using BepInEx.Configuration;
using RoR2;

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled, ModuleAttribute.StartupTarget.Start)]
    internal sealed class ExecConfig : R2DSEModule
    {
        public const string ModuleName = nameof(ExecConfig);
        public const string ModuleDescription = "Execute the server config file of your choice located in Risk of Rain 2_Data/Config/ (server.cfg by default).";
        public const bool   DefaultEnabled = true;

        private ConfigEntry<string> _configFileName;

        public ExecConfig(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
            Console.instance.SubmitCmd(null, $"exec {_configFileName.Value}");
        }


        protected override void Hook()
        {

        }

        protected override void UnHook()
        {

        }

        protected override void MakeConfig()
        {
            _configFileName = AddConfig("Server CFG on Startup", "server", "Name of the CFG File to load at the startup of the server. Don't include the extension. Example : server");
        }
    }
}
