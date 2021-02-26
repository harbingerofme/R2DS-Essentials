using BepInEx.Configuration;
using BepInEx.Logging;

namespace R2DSEssentials
{
    public abstract class R2DSEModule
    {
        public readonly string Name;
        public readonly string Description;

        protected ManualLogSource Logger;

        public bool IsEnabled
        {
            get { return PreviouslyEnabled; }
            set
            {
                Enabled.Value = value;
            }
        }

        protected bool PreviouslyEnabled = false;
        protected readonly ConfigEntry<bool> Enabled;

        public R2DSEModule(string name, string description, bool defaultEnabled)
        {
            Name = name;
            Description = description;
            Logger = PluginEntry.Log;
            Enabled = AddConfig("_Enabled", defaultEnabled, Description);
            // ReSharper disable once VirtualMemberCallInConstructor  Justification=The consctructor of an abstract class shouldn't be called anyway.
            MakeConfig();
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

        protected ConfigEntry<T> AddConfig<T>(string key, T defaultValue, string description)
        {
            return AddConfig(key, defaultValue, new ConfigDescription(description));
        }

        protected ConfigEntry<T> AddConfig<T>(string key, T defaultValue, ConfigDescription configDescription)
        {
            ConfigDescription orderedConfigDescription = new ConfigDescription(configDescription.Description, configDescription.AcceptableValues);
            ConfigEntry<T> entry = PluginEntry.Configuration.Bind(Name, key, defaultValue, orderedConfigDescription);
            entry.SettingChanged += ReloadHooks;
            return entry;
        }

        protected void LogModuleMessage(object message)
        {
            Logger.LogMessage($"[{Name}] {message}");
        }

        protected void LogModuleInfo(object message)
        {
            Logger.LogInfo($"[{Name}] {message}");
        }

        protected void LogModuleWarning(object message)
        {
            Logger.LogWarning($"[{Name}] {message}");
        }

        protected void LogModuleError(object message)
        {
            Logger.LogError($"[{Name}] {message}");
        }
    }
}