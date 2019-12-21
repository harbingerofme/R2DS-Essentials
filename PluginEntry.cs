using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;

namespace R2DSEssentials
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    class PluginEntry : BaseUnityPlugin
    {

        private const string ModVer = "0.0.1";
        private const string ModName = "R2DSE";
        public const string ModGuid = "com.iDeathHD&Harb." + ModName;
        public static ManualLogSource Log;
        public static ConfigFile Configuration;
        public static Dictionary<string, R2DSEModule> Modules;
        //private 
        private PluginEntry()
        {
            Log = Logger;
            Configuration = Config;
            Modules = new Dictionary<string, R2DSEModule>();
        }

        private void Awake()
        {

        }

        private void Start()
        {

        }

    }

    abstract class R2DSEModule
    {
        public readonly string Name;
        public readonly string Description;

        protected ManualLogSource Logger;

        protected bool PreviouslyEnabled = false;
        protected readonly ConfigEntry<bool> Enabled;

        public R2DSEModule(string name, string description, bool defaultEnabled)
        {
            Name = name;
            Description = description;
            Enabled = AddConfig("_Enabled", defaultEnabled, Description);
            MakeConfig();
            Logger = PluginEntry.Log;
        }

        public void ReloadHooks(object _ = null, System.EventArgs __ = null)
        {
            if (PreviouslyEnabled)
            {
                UnHook();
                PreviouslyEnabled = false;
            }
            if (Enabled.Value)
            {
                Hook();
                PreviouslyEnabled = true;
            }
        }

        protected abstract void UnHook();
        protected abstract void Hook();

        protected abstract void MakeConfig();


        protected ConfigEntry<T> AddConfig<T>(string settingShortDescr, T value, string settingLongDescr)
        {
            return AddConfig(settingShortDescr, value, new ConfigDescription(settingLongDescr));
        }

        protected ConfigEntry<T> AddConfig<T>(string settingShortDescr, T value, ConfigDescription configDescription)
        {
            ConfigDescription orderedConfigDescription = new ConfigDescription(configDescription.Description, configDescription.AcceptableValues);
            ConfigEntry<T> entry = PluginEntry.Configuration.Bind(Name, settingShortDescr, value, orderedConfigDescription);
            entry.SettingChanged += ReloadHooks;
            return entry;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal class ModuleAttribute : Attribute
    {
        public readonly string Name;
        public readonly bool DefaultEnabled;
        public readonly string Description;
        public StartupTarget target;
        public ModuleAttribute(string name, string description, bool defaultEnabled, StartupTarget target = StartupTarget.Awake)
        {
            Name = name;
            DefaultEnabled = defaultEnabled;
            Description = description;
            this.target = target;
        }

        public enum StartupTarget
        {
            Awake,
            Start
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal class ModuleDependency : Attribute
    {
        public readonly string Dependency;
        public readonly DependencyType Type;
        public ModuleDependency(string dependency, DependencyType type = DependencyType.Hard)
        {
            Dependency = dependency;
            Type = type;
        }

        public enum DependencyType
        {
            Hard,
            Soft
        }
    }

}
}
