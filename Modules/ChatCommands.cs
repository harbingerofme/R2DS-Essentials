

using Mono.Cecil.Cil;
using MonoMod.Cil;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;

namespace R2DSEssentials.Modules
{

    //Disabled!


    //[Module(ModuleName, ModuleDescription, DefaultEnabled)]
    class ChatCommands : R2DSEModule
    {
        public const string ModuleName = nameof(ChatCommands);
        public const string ModuleDescription = "Capture messages beginning with '/' and consider them as though a user send them as a command. WIP";
        public const bool DefaultEnabled = false;

        public ChatCommands(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
        }

        protected override void Hook()
        {
            IL.RoR2.Chat.CCSay += Chat_CCSay;
        }

        private void Chat_CCSay(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(x => x.MatchLdarga(out _));
            c.Emit(OpCodes.Ldarga_S,0);
            c.EmitDelegate<Func<RoR2.ConCommandArgs,bool>>((args) =>
            {
                if (args[0].StartsWith("/"))
                {
                    string command = args[0].Substring(1);
                    Logger.LogMessage("HAI!");
                    RoR2.Console.instance.SubmitCmd(args.sender, command);
                    return true;
                }
                return false;
            });
            c.Emit(OpCodes.Brtrue,19);
            /*
            if (args[0].StartsWith("/"))
            {
                string command = args[0].Substring(1);
                Logger.LogInfo($"Intercepted: {command}");
                RoR2.Console.instance.SubmitCmd(args.sender, command);
            }
            else
            {
                orig(args);
            }*/
        }

        protected override void MakeConfig()
        {
           //can be empty.
        }

        protected override void UnHook()
        {
            IL.RoR2.Chat.CCSay -= Chat_CCSay;
        }
    }
}