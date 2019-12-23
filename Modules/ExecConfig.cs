using BepInEx.Configuration;
using Console = RoR2.Console;

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled, ModuleAttribute.StartupTarget.Awake)]
    internal sealed class ExecConfig : R2DSEModule
    {
        public const string ModuleName = nameof(ExecConfig);
        public const string ModuleDescription = "(Re) Execute the server config file of your choice located in Risk of Rain 2_Data/Config/ (server.cfg by default).";
        public const bool   DefaultEnabled = true;

        private ConfigEntry<string> _configFileName;

        public ExecConfig(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
        }


        protected override void Hook()
        {
            PluginEntry.Instance.OnFinishLoading += Execute;
        }

        protected override void UnHook()
        {
            PluginEntry.Instance.OnFinishLoading -= Execute;
        }

        protected override void MakeConfig()
        {
            _configFileName = AddConfig("Server CFG Filename", "server", "Name of the CFG File to load at the startup of the server. Don't include the extension. Example : server");
        }

        private void Execute()
        {
            if (IsEnabled)
                Console.instance.SubmitCmd(null, $"exec {_configFileName.Value}");
            else
                Logger.LogWarning("ExecConfig: Execute() called while not enabled!");
        }
    }
}
