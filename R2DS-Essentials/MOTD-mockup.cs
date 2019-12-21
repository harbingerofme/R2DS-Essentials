using BepInEx;
using RoR2;
using UnityEngine.Networking;
using MonoMod.Cil;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using BepInEx.Configuration;
using R2API;

namespace MotD
{
    [BepInPlugin(GUID,NAME,VERSION)]
    public class MotDPlugin : BaseUnityPlugin
    {
        public const string
            NAME = "SimpleMotD",
            GUID = "com.harbingerofme." + NAME,
            VERSION = "0.0.1";

        private string modList = "";

        ConfigEntry<string> motd;

        public void Awake()
        {
            On.RoR2.RoR2Application.UnitySystemConsoleRedirector.Redirect += orig => { };
            IL.RoR2.Networking.GameNetworkManager.OnServerAddPlayerInternal += GameNetworkManager_OnServerAddPlayerInternal1;
            motd = Config.Bind<string>(new ConfigDefinition("", ""), "This server runs: %MODLIST%", new ConfigDescription("You can use the following tokens: %STEAM%, %MODLIST%."));
        }

        private string GetModList()
        {
            if (modList == "")
            {
                List<string> nameList = new List<string>();
                foreach (KeyValuePair<string, PluginInfo> entry in BepInEx.Bootstrap.Chainloader.PluginInfos)
                {
                    nameList.Add($"[{entry.Value.Metadata.Name}]");
                }
                modList = string.Join(",", nameList);
            }
            return modList;
        }

        private void GameNetworkManager_OnServerAddPlayerInternal1(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(UnityEngine.Debug), "LogFormat"));
            c.GotoNext(MoveType.Before,
                x => x.MatchRet());
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<NetworkConnection>>((conn) =>
            {
               
                var message = new Chat.SimpleChatMessage() { baseToken = "{0}", paramTokens = new[] { GenerateMotDFormatted(conn) } };
                SendPrivateMessage(message, conn);
            });
        }

        private string GenerateMotDFormatted(NetworkConnection conn)
        {
            string message = motd.Value;
            if (message.Contains("%STEAM%"))
            {
                var steam = RoR2.Networking.ServerAuthManager.FindAuthData(conn).steamId;
                message = message.Replace("%STEAM%", steam.ToString());
            }
            if (message.Contains("%MODLIST%"))
            {
                message = message.Replace("%MODLIST%", GetModList());
            }
            return message;
        }

        private static void SendPrivateMessage(Chat.ChatMessageBase message, NetworkConnection connection)
        {
            NetworkWriter writer = new NetworkWriter();
            writer.StartMessage((short)59);
            writer.Write(message.GetTypeIndex());
            writer.Write((MessageBase)message);
            writer.FinishMessage();
            connection.SendWriter(writer, RoR2.Networking.QosChannelIndex.chat.intVal);
        }
    }
}
