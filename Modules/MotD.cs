using BepInEx;
using RoR2;
using UnityEngine.Networking;
using MonoMod.Cil;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
    [ModuleDependency(nameof(RetrieveUsername), ModuleDependency.DependencyType.Soft)]
    internal sealed class MotD : R2DSEModule
    {
        public const string ModuleName = nameof(MotD);
        public const string ModuleDescription = "Sends a configurable message to clients upon joining";
        public const bool   DefaultEnabled = true;

        private string modList = "";

        ConfigEntry<string> motd;

        public MotD(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
        }


        protected override void MakeConfig()
        {
            motd = AddConfig<string>("Message","This server runs: %MODLIST%", "You can use the following tokens: %STEAM%, %MODLIST%.");
        }

        protected override void Hook()
        {
            IL.RoR2.Networking.GameNetworkManager.OnServerAddPlayerInternal += GameNetworkManager_OnServerAddPlayerInternal1;
        }

        protected override void UnHook()
        {
            IL.RoR2.Networking.GameNetworkManager.OnServerAddPlayerInternal -= GameNetworkManager_OnServerAddPlayerInternal1;
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
