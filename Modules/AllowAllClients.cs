using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using UnityEngine.Networking;

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
    class AllowAllClients : R2DSEModule
    {
        public const string ModuleName = nameof(template);
        public const string ModuleDescription = "Allows any kind of client to join.";
        public const bool DefaultEnabled = true;

        private ConfigEntry<bool> ShouldSendMismatchMessage;

        public AllowAllClients(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {

        }

        protected override void Hook()
        {
            IL.RoR2.Networking.ServerAuthManager.HandleSetClientAuth += ServerAuthManager_HandleSetClientAuth;
        }

        private void ServerAuthManager_HandleSetClientAuth(MonoMod.Cil.ILContext il)
        {
               ILCursor c = new ILCursor(il);
            c.GotoNext(
               inst => inst.MatchLdstr("Mod mismatch."));
            c.GotoNext(MoveType.Before,
                inst => inst.MatchStloc(1));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<RoR2.Networking.GameNetworkManager.ModMismatchKickReason,NetworkMessage>>((kickReason, message)=> {
                if (ShouldSendMismatchMessage.Value)
                {
                    var myMessage = new Chat.SimpleChatMessage { baseToken = "{0}: {2}: {1}", paramTokens = new string[] { "Server", "Your connection was accepted, but the following mod mismatches exist", String.Join(" | ", kickReason.serverModList) } };
                    Util.Networking.SendPrivateMessage(myMessage, message.conn);
                }
            });
            c.Emit(OpCodes.Ldloc_1);
        }

        protected override void MakeConfig()
        {
           ShouldSendMismatchMessage = AddConfig("SendMisMatchMessage",true, "Send mismatched message to clients?");
        }

        protected override void UnHook()
        {
            IL.RoR2.Networking.ServerAuthManager.HandleSetClientAuth += ServerAuthManager_HandleSetClientAuth;
        }
    }
}