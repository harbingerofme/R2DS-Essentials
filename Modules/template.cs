

namespace R2DSEssentials.Modules
{
    //[Module(ModuleName, ModuleDescription, DefaultEnabled)]
    class template : R2DSEModule
    {
        public const string ModuleName = nameof(template);
        public const string ModuleDescription = "don't forget to uncomment the attribute!";
        public const bool DefaultEnabled = true;

        public template(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
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