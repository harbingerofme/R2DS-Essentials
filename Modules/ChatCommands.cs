using MonoMod.RuntimeDetour;
using RoR2;
using System.Reflection;

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
    class ChatCommands : R2DSEModule
    {
        public const string ModuleName = nameof(ChatCommands);
        public const string ModuleDescription = "Capture messages beginning with '/' and consider them as though a user send them as a command. WIP";
        public const bool DefaultEnabled = true;

        private static HookConfig _hookConfig;
        private static Hook _runCmdHook;
        private static On.RoR2.Console.orig_RunCmd _origRunCmd;

        public ChatCommands(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
        }

        protected override void Hook()
        {
            // Higher priority to make sure this hook executed before the one from DebugToolkit if it exists
            // It'll make sure correct permissions are applied if needed.

            _hookConfig = new HookConfig { ManualApply = true, Priority = 2};

            _runCmdHook = new Hook(typeof(Console).GetMethod("RunCmd",BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(ChatCommands).GetMethod(nameof(Console_RunCmd), BindingFlags.Instance | BindingFlags.NonPublic), _hookConfig);
            _origRunCmd = _runCmdHook.GenerateTrampoline<On.RoR2.Console.orig_RunCmd>();

            _runCmdHook.Apply();
        }

        private void Console_RunCmd(On.RoR2.Console.orig_RunCmd _, Console self, Console.CmdSender sender, string concommandName, System.Collections.Generic.List<string> userArgs)
        {
            if (concommandName == "say" && userArgs != null && userArgs.Count>=1 && userArgs[0].StartsWith("/"))
            {
                var oldArgs = userArgs[0].Split(' ');
                concommandName = oldArgs[0].Substring(1);
                if (oldArgs.Length > 1)
                {
                    userArgs[0] = string.Join(" ", oldArgs, 1, oldArgs.Length - 1);
                }
                else
                {
                    userArgs[0] = "";
                }
            }

            _origRunCmd(self, sender, concommandName, userArgs);
        }

        protected override void MakeConfig()
        {
           //can be empty.
        }

        protected override void UnHook()
        {
            _runCmdHook.Dispose();
        }
    }
}