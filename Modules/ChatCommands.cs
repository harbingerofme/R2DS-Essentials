

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
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
        }

        protected override void MakeConfig()
        {
           //can be empty.
        }

        protected override void UnHook()
        {
        }
    }
}