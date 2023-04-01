﻿using BepInEx.Configuration;
using Facepunch.Steamworks;
using RoR2;
using RoR2.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Console = RoR2.Console;

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
    public sealed class RetrieveUsername : R2DSEModule
    {
        public const string ModuleName = nameof(RetrieveUsername);
        public const string ModuleDescription = "Retrieve player usernames through third party website. Don't need a steam api key.";
        public const bool DefaultEnabled = true;

        private ConfigEntry<bool> _enableBlackListRichNames;
        private ConfigEntry<string> _blackListRichNames;

        public static event Action OnUsernameUpdated;

        internal static readonly Dictionary<ulong, string> UsernamesCache = new Dictionary<ulong, string>();
        private static readonly List<ulong> RequestCache = new List<ulong>();

        public RetrieveUsername(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
        }


        protected override void Hook()
        {
            On.RoR2.NetworkPlayerName.GetResolvedName += OnGetResolvedName;
            Run.onServerGameOver += EmptyCachesOnGameOver;
            On.RoR2.Networking.NetworkManagerSystem.OnServerDisconnect += RemoveCacheOnPlayerDisconnect;
        }


        protected override void UnHook()
        {
            On.RoR2.NetworkPlayerName.GetResolvedName -= OnGetResolvedName;
            Run.onServerGameOver -= EmptyCachesOnGameOver;
            On.RoR2.Networking.NetworkManagerSystem.OnServerDisconnect -= RemoveCacheOnPlayerDisconnect;
        }

        protected override void MakeConfig()
        {
            _enableBlackListRichNames = AddConfig("Enable Auto-kick Rich Tag", true,
                "Should the auto-kicker be enabled for people with rich name like oversized names / names with annoying tag");

            _blackListRichNames = AddConfig("Rich Tag Blacklist", "size, style",
                "Blacklist thats used for banning specific tags, only input the tag name in this. Example : size, style, color");
        }

        private string OnGetResolvedName(On.RoR2.NetworkPlayerName.orig_GetResolvedName orig, ref NetworkPlayerName self)
        {
            if (Server.Instance != null)
            {
                return UsernamesCache.TryGetValue(self.steamId.steamValue, out var name) ? name : GetPersonaNameWebAPI(self.steamId.steamValue);
            }

            return orig(ref self);
        }

        private static void EmptyCachesOnGameOver(Run _, GameEndingDef __)
        {
            UsernamesCache.Clear();
            RequestCache.Clear();
        }

        private static void RemoveCacheOnPlayerDisconnect(On.RoR2.Networking.NetworkManagerSystem.orig_OnServerDisconnect orig, NetworkManagerSystem self, NetworkConnection conn)
        {
            var nu = Util.Networking.FindNetworkUserForConnectionServer(conn);

            if (nu != null)
            {
                var steamId = nu.GetNetworkPlayerName().steamId.steamValue;

                if (steamId != 0)
                {
                    UsernamesCache.Remove(steamId);
                    RequestCache.Remove(steamId);
                }
            }

            orig(self, conn);
        }

        // ReSharper disable once InconsistentNaming
        private string GetPersonaNameWebAPI(ulong steamId)
        {
            const string unkString = "???";

            if (steamId.ToString().Length != 17)
                return unkString;

            if (!RequestCache.Contains(steamId))
            {
                RequestCache.Add(steamId);
                PluginEntry.Instance.StartCoroutine(WebRequestCoroutine(steamId));
            }

            return unkString;
        }

        private IEnumerator WebRequestCoroutine(ulong steamId)
        {
            const string regexForLookUp = "<th scope=\"row\">name</th>\\s*<td><code>(.*)<\\/code></td>";
             
            var ioUrlRequest = "https://steamidfinder.com/lookup/" + steamId;

            var webRequest = UnityWebRequest.Get(ioUrlRequest);
            yield return webRequest.SendWebRequest();

            if (!webRequest.isNetworkError)
            {
                var rx = new Regex(regexForLookUp,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

                var nameFromRegex = rx.Match(webRequest.downloadHandler.text).Groups[1].ToString();

                if (!nameFromRegex.Equals(""))
                {
                    var gotBlackListed = false;

                    if (_enableBlackListRichNames.Value)
                    {
                        var blackList = _blackListRichNames.Value.Split(',');

                        foreach (var tag in blackList)
                        {
                            var bannedTag = "&lt;" + tag + "=";
                            if (nameFromRegex.Contains(bannedTag))
                            {
                                var userToKick = Util.Networking.GetNetworkUserFromSteamId(steamId);
                                var playerId = Util.Networking.GetPlayerIndexFromNetworkUser(userToKick);

                                Console.instance.SubmitCmd(null, $"kick {playerId}");
                                gotBlackListed = true;
                            }
                        }
                    }

                    if (!UsernamesCache.ContainsKey(steamId) && !gotBlackListed)
                    {
                        UsernamesCache.Add(steamId, nameFromRegex);

                        var networkUser = Util.Networking.GetNetworkUserFromSteamId(steamId);
                        if (networkUser != null)
                        {
                            Logger.LogInfo($"New player : {nameFromRegex} connected. (STEAM:{steamId})");
                            networkUser.userName = nameFromRegex;

                            // Sync with other players by forcing dirty syncVar ?
                            SyncNetworkUserVarTest(networkUser);

                            OnUsernameUpdated?.Invoke();
                        }
                    }
                }
            }

            webRequest.Dispose();
        }

        private static void SyncNetworkUserVarTest(NetworkUser currentNetworkUser)
        {
            var tmp = currentNetworkUser.Network_id;
            var nid = NetworkUserId.FromIp("000.000.000.1", 255);
            currentNetworkUser.Network_id = nid;
            currentNetworkUser.SetDirtyBit(1u);
            PluginEntry.Instance.StartCoroutine(UpdateUsernameDelayed(currentNetworkUser, tmp));
        }

        private static IEnumerator UpdateUsernameDelayed(NetworkUser userToUpdate, NetworkUserId realId)
        {
            yield return new WaitForSeconds(1);

            userToUpdate.Network_id = realId;
            userToUpdate.SetDirtyBit(1u);
        }
    }
}