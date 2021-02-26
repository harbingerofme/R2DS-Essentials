using UnityEngine;

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
    class HideIP : R2DSEModule
    {
        public const string ModuleName = nameof(HideIP);
        public const string ModuleDescription = "Hides the IP from the console. This is useful if you're writing guides on a local machine and don't want your local ip adress to leak.";
        public const bool DefaultEnabled = false;

        public HideIP(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
        }

        protected override void Hook()
        {
            On.RoR2.SteamworksServerManager.OnAddressDiscovered += SteamworksServerManager_OnAddressDiscovered;
        }

        private void SteamworksServerManager_OnAddressDiscovered(On.RoR2.SteamworksServerManager.orig_OnAddressDiscovered orig, object self)
        {
            Debug.Log("Steamworks Server IP discovered: [HIDDEN]");
        }

        protected override void MakeConfig()
        {
            //can be empty.
        }

        protected override void UnHook()
        {
            On.RoR2.SteamworksServerManager.OnAddressDiscovered -= SteamworksServerManager_OnAddressDiscovered;
        }
    }
}