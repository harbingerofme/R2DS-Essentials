using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace R2DSEssentials
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    class PluginEntry : BaseUnityPlugin
    {
        private const string ModVer = "0.0.1";
        private const string ModName = "R2DSE";
        public const string ModGuid = "com.HarbAndDeath." + ModName;
        public static ManualLogSource Log;
        public static ConfigFile Configuration;
        public static Dictionary<string, R2DSEModule> Modules;


        private Queue<ModuleAndAttribute>[] ModulesToLoad;
        private readonly Type[] constructorParameters = new Type[] { typeof(string), typeof(string), typeof(bool) };
        private readonly object[] constuctorArgumentArray = new object[3];

        private PluginEntry()
        {
            Log = Logger;
            Configuration = Config;
            Modules = new Dictionary<string, R2DSEModule>();
            ModulesToLoad = new Queue<ModuleAndAttribute>[2];
            ModulesToLoad[0] = new Queue<ModuleAndAttribute>();
            ModulesToLoad[1] = new Queue<ModuleAndAttribute>();

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                ModuleAttribute customAttr = (ModuleAttribute)type.GetCustomAttributes(typeof(ModuleAttribute), false).FirstOrDefault();
                if (customAttr != null)
                {
                    if (customAttr.target == ModuleAttribute.StartupTarget.Awake)
                    {
                        ModulesToLoad[0].Enqueue(new ModuleAndAttribute() { Module = type, attribute = customAttr});
                    }
                    else
                    {
                        ModulesToLoad[1].Enqueue(new ModuleAndAttribute() { Module = type, attribute = customAttr });
                    }
                }
            }
        }

        private void Awake()
        {
            while (ModulesToLoad[0].Count > 0)
            {
                ModuleAndAttribute temp = ModulesToLoad[0].Dequeue();
                EnableModule(temp);
            }
        }

        private void Start()
        {
            while (ModulesToLoad[0].Count > 0)
            {
                ModuleAndAttribute temp = ModulesToLoad[1].Dequeue();
                EnableModule(temp);
            }
        }

        private void EnableModule(ModuleAndAttribute module)
        {
            Logger.LogError($"Enabling module: {module.attribute.Name}");

            ModuleAttribute customAttr = module.attribute;
            Type type = module.Module;
            constuctorArgumentArray[0] = customAttr.Name;
            constuctorArgumentArray[1] = customAttr.Description;
            constuctorArgumentArray[2] = customAttr.DefaultEnabled;
            try
            {
                var ctor = type.GetConstructor(constructorParameters);
                R2DSEModule loadedModule = (R2DSEModule)ctor.Invoke(constuctorArgumentArray);
                loadedModule.ReloadHooks();
                Modules.Add(customAttr.Name, loadedModule);
            }
            catch
            {
                Logger.LogError($"Couldn't load module: {constuctorArgumentArray[1]}");
            }
        }

        private sealed class ModuleAndAttribute
        {
            public Type Module;
            public ModuleAttribute attribute;
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
