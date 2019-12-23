using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using RoR2.ConVar;
using Console = System.Console;
using System.Collections;

namespace R2DSEssentials
{
    [BepInDependency(R2API.R2API.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    class PluginEntry : BaseUnityPlugin
    {
        private const string ModVer = "0.0.1";
        private const string ModName = "R2DSE";
        public const string ModGuid = "com.HarbAndDeath." + ModName;
        public static ManualLogSource Log;
        public static ConfigFile Configuration;
        private static ConfigEntry<bool> DisableWhenGraphicDetected;
        public static Dictionary<string, R2DSEModule> Modules;

        private Queue<ModuleAndAttribute>[] ModulesToLoad;
        private readonly Type[] constructorParameters = new Type[] { typeof(string), typeof(string), typeof(bool) };
        private readonly object[] constuctorArgumentArray = new object[3];
        internal static Queue<BaseConVar> ConvarsToAdd;

        internal static PluginEntry Instance;

        private static StringBuilder _consoleCommand = new StringBuilder();

        private PluginEntry()
        {
            Instance = this;
            Log = Logger;
            Configuration = Config;
            Modules = new Dictionary<string, R2DSEModule>();
            ConvarsToAdd = new Queue<BaseConVar>();
            ModulesToLoad = new Queue<ModuleAndAttribute>[2];
            ModulesToLoad[0] = new Queue<ModuleAndAttribute>();
            ModulesToLoad[1] = new Queue<ModuleAndAttribute>();

            DisableWhenGraphicDetected = Configuration.Bind("_R2DSE", "Disable When Graphics Detected", true, "Disable the plugin when game graphics are detected.");

            if (!Application.isBatchMode && DisableWhenGraphicDetected.Value)
            {
                Logger.LogWarning("Detected graphics. Plugin disabled. If you want to use R2DSE in this mode please change the plugin config.");
                return;
            }

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Awake is called right after the constructor.")]
        private void Awake()
        {
            while (ModulesToLoad[0].Count > 0)
            {
                ModuleAndAttribute temp = ModulesToLoad[0].Dequeue();
                EnableModule(temp);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Start is called right after all catalogs have been initialized.")]
        private void Start()
        {
            while (ModulesToLoad[1].Count > 0)
            {
                ModuleAndAttribute temp = ModulesToLoad[1].Dequeue();
                EnableModule(temp);
            }

            var convarAddMethod = typeof(RoR2.Console).GetMethod("RegisterConVarInternal", BindingFlags.NonPublic | BindingFlags.Instance);
            while (ConvarsToAdd.Count > 0)
            {
                BaseConVar convar = ConvarsToAdd.Dequeue();
                convarAddMethod.Invoke(RoR2.Console.instance, new object[] { convar });
            }

            var CCcatalog = (IDictionary)typeof(RoR2.Console).GetField("concommandCatalog", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(RoR2.Console.instance);
            var CCtype = typeof(RoR2.Console).GetNestedType("ConCommand", BindingFlags.NonPublic);
            foreach (MethodInfo methodInfo in typeof(ConCommands).GetMethods())
            {
                var attr = methodInfo.GetCustomAttribute<RoR2.ConCommandAttribute>();
                var conCommand = Activator.CreateInstance(CCtype);
                foreach (FieldInfo field in conCommand.GetType().GetFields())
                {
                    switch (field.Name)
                    {
                        case "flags":
                            field.SetValue(conCommand, attr.flags);
                            break;
                        case "helpText":
                            field.SetValue(conCommand, attr.helpText);
                            break;
                        case "action":
                            field.SetValue(conCommand, (RoR2.Console.ConCommandDelegate)Delegate.CreateDelegate(typeof(RoR2.Console.ConCommandDelegate), methodInfo));
                            break;
                        default:
                            break;
                    }
                }
                CCcatalog[attr.commandName.ToLower()] = conCommand;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Update is called by Unity.")]
        private void Update()
        {
            if (!Application.isBatchMode)
                return;

            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey();
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    RoR2.Console.instance.SubmitCmd(null, _consoleCommand.ToString());
                    _consoleCommand.Clear();
                }
                else
                {
                    Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop);
                    Console.Write(keyInfo.KeyChar);
                    _consoleCommand.Append(keyInfo.KeyChar);
                }
            }
        }

        private void EnableModule(ModuleAndAttribute module)
        {
            Logger.LogInfo($"Enabling module: {module.attribute.Name}");

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
                Logger.LogError($"Couldn't load module: {constuctorArgumentArray[0]}");
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

        public bool IsEnabled {
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

        protected BaseConVar BindConfig<T>(string convarName, ConfigEntry<T> configEntry, Type convarType)
        {
            if (!typeof(BaseConVar).IsAssignableFrom(convarType))
            {
                Debug.LogErrorFormat("Cannot bind {0}! baseconvar is not assignable from it.");
                return null;
            }
            var ctorParamTypes = new Type[] { typeof(string), typeof(RoR2.ConVarFlags), typeof(string), typeof(string) };
            var ctor = convarType.GetConstructor(ctorParamTypes);
            var ctorParamFields = new object[] { convarName, RoR2.ConVarFlags.None, configEntry.DefaultValue.ToString(), configEntry.Description };
            BaseConVar convar = (BaseConVar) ctor.Invoke(ctorParamFields);
            configEntry.SettingChanged += (obj, args) => { convar.AttemptSetString(((SettingChangedEventArgs)args).ChangedSetting.BoxedValue.ToString());};
            PluginEntry.ConvarsToAdd.Enqueue(convar);
            return convar;
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
