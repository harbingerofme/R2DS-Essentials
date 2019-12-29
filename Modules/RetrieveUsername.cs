using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RoR2;
using Facepunch.Steamworks;
using UnityEngine.Networking;

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
    internal sealed class RetrieveUsername : R2DSEModule
    {
        public const string ModuleName = nameof(RetrieveUsername);
        public const string ModuleDescription = "Retrieve player usernames through third party website. Don't need a steam api key.";
        public const bool   DefaultEnabled = true;

        internal static event Action OnUsernameUpdated;

        internal static readonly Dictionary<ulong, string> UsernamesCache = new Dictionary<ulong, string>();
        private readonly List<ulong> _requestCache = new List<ulong>();

        public RetrieveUsername(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
        }


        protected override void Hook()
        {
            On.RoR2.NetworkPlayerName.GetResolvedName += OnGetResolvedName;
            Run.OnServerGameOver += EmptyCachesOnGameOver;
        }

        protected override void UnHook()
        {
            On.RoR2.NetworkPlayerName.GetResolvedName -= OnGetResolvedName;
            Run.OnServerGameOver -= EmptyCachesOnGameOver;
        }

        protected override void MakeConfig()
        {
        }

        private string OnGetResolvedName(On.RoR2.NetworkPlayerName.orig_GetResolvedName orig, ref NetworkPlayerName self)
        {
            if (Server.Instance != null)
            {
                return UsernamesCache.TryGetValue(self.steamId.value, out var name) ? name : GetPersonaNameWebAPI(self.steamId.value);
            }

            return orig(ref self);
        }

        private void EmptyCachesOnGameOver(Run self, GameResultType gameResult)
        {
            UsernamesCache.Clear();
            _requestCache.Clear();
        }

        // ReSharper disable once InconsistentNaming
        private string GetPersonaNameWebAPI(ulong steamId)
        {
            const string unkString = "???";

            if (steamId.ToString().Length != 17)
                return unkString;

            if (!_requestCache.Contains(steamId))
            {
                _requestCache.Add(steamId);
                PluginEntry.Instance.StartCoroutine(WebRequestCoroutine(steamId));
            }
            
            return unkString;
        }

        private IEnumerator WebRequestCoroutine(ulong steamId)
        {
            const string regexForLookUp = "<dd class=\"value\"><a href=\"(.*?)\"";
            const string regexForPersonaName = "\"personaname\":\"(.*?)\"";

            var ioUrlRequest = "https://steamid.io/lookup/" + steamId;

            var webRequest = UnityWebRequest.Get(ioUrlRequest);
            yield return webRequest.SendWebRequest();

            if (!webRequest.isNetworkError)
            {
                var rx = new Regex(regexForLookUp,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

                var steamProfileUrl = rx.Match(webRequest.downloadHandler.text).Groups[1].ToString();

                webRequest = UnityWebRequest.Get(steamProfileUrl);

                yield return webRequest.SendWebRequest();

                if (!webRequest.isNetworkError)
                {
                    rx = new Regex(regexForPersonaName,
                        RegexOptions.Compiled | RegexOptions.IgnoreCase);

                    var nameFromRegex = rx.Match(webRequest.downloadHandler.text).Groups[1].ToString();

                    if (!nameFromRegex.Equals(""))
                    {
                        if (!UsernamesCache.ContainsKey(steamId))
                        {
                            UsernamesCache.Add(steamId, nameFromRegex);
                            foreach (var networkUser in NetworkUser.readOnlyInstancesList)
                            {
                                if (networkUser.GetNetworkPlayerName().steamId.value == steamId)
                                {
                                    Logger.LogInfo($"New player : {nameFromRegex} connected. (STEAM:{steamId})");
                                    networkUser.userName = nameFromRegex;

                                    // Sync with other players by forcing dirty syncVar ?
                                    var tmp = networkUser.Network_id;
                                    networkUser.Network_id = new NetworkUserId();
                                    networkUser.Network_id = tmp;

                                    OnUsernameUpdated?.Invoke();
                                    OnUsernameUpdated = null;
                                    break;
                                }
                            }
                        }    
                    }
                }
            }

            webRequest.Dispose();
        }
    }
}
