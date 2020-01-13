namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
    class ChatCommands : R2DSEModule
    {
        public const string ModuleName = nameof(ChatCommands);
        public const string ModuleDescription = "Capture messages beginning with '/' and consider them as though a user send them as a command. WIP";
        public const bool DefaultEnabled = true;

        public ChatCommands(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
        }

        protected override void Hook()
        {
            On.RoR2.Console.RunCmd += Console_RunCmd;
        }

        private void Console_RunCmd(On.RoR2.Console.orig_RunCmd orig, RoR2.Console self, RoR2.NetworkUser sender, string concommandName, System.Collections.Generic.List<string> userArgs)
        {
            if(concommandName == "say" && userArgs != null && userArgs.Count>=1 && userArgs[0].StartsWith("/"))
            {
                var oldArgs = userArgs[0].Split(' ');
                concommandName = oldArgs[0].Substring(1);
                if (oldArgs.Length > 1)
                {
                    userArgs[0] = string.Join(" ", oldArgs, 1, oldArgs.Length - 1);
                }
                else
                    userArgs[0] = "";
            }
            orig(self, sender, concommandName, userArgs);
        }

        protected override void MakeConfig()
        {
           //can be empty.
        }

        protected override void UnHook()
        {
            On.RoR2.Console.RunCmd -= Console_RunCmd;
        }
    }
}