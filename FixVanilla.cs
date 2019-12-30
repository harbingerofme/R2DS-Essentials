﻿using Facepunch.Steamworks;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace R2DSEssentials
{
    internal static class FixVanilla
    {
        internal static void Init()
        {
            NativeWrapper.InjectRemoveGarbage();

            On.RoR2.Chat.CCSay += ServerSay;

            On.LeTai.Asset.TranslucentImage.TranslucentImage.Start += FixCameraErrorSpam;
            On.LeTai.Asset.TranslucentImage.TranslucentImage.LateUpdate += FixCameraErrorSpamPartTwo;

            On.RoR2.Networking.GameNetworkManager.OnServerDisconnect += EndAuthOnClientDisconnect;
        }

        private static void ServerSay(On.RoR2.Chat.orig_CCSay orig, ConCommandArgs args)
        {
            if (args.sender == null)
            {
                args.CheckArgumentCount(1);
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=red>Server:</color> {args[0]}"
                });
            }
            else
            {
                orig(args);
            }
        }

        private static void FixCameraErrorSpam(On.LeTai.Asset.TranslucentImage.TranslucentImage.orig_Start orig, LeTai.Asset.TranslucentImage.TranslucentImage self)
        {
            if (!Application.isBatchMode)
            {
                orig(self);
            }
        }

        private static void FixCameraErrorSpamPartTwo(On.LeTai.Asset.TranslucentImage.TranslucentImage.orig_LateUpdate orig, LeTai.Asset.TranslucentImage.TranslucentImage self)
        {
            if (!Application.isBatchMode)
            {
                orig(self);
            }
        }

        private static void EndAuthOnClientDisconnect(On.RoR2.Networking.GameNetworkManager.orig_OnServerDisconnect orig, GameNetworkManager self, NetworkConnection conn)
        {
            var nu = Util.Networking.FindNetworkUserForConnectionServer(conn);

            if (nu != null)
            {
                var steamId = nu.GetNetworkPlayerName().steamId.value;

                if (steamId != 0)
                {
                    PluginEntry.Log.LogInfo($"Ending AuthSession with : {nu.userName} ({steamId})");
                    Server.Instance.Auth.EndSession(steamId);
                }
            }
            
            orig(self, conn);
        }
    }
}
