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

        private readonly Queue<ModuleAndAttribute>[] ModulesToLoad;
        private readonly Type[] constructorParameters = new Type[] { typeof(string), typeof(string), typeof(bool) };
        private readonly object[] constuctorArgumentArray = new object[3];
        private readonly Queue<BaseConVar> ConvarsToAdd;
        private readonly Queue<MethodInfo> ConCommandsToAdd;

        internal static PluginEntry Instance;

        private static readonly StringBuilder _consoleCommand = new StringBuilder();

        private PluginEntry()
        {
            Instance = this;
            Log = Logger;
            Configuration = Config;
            Modules = new Dictionary<string, R2DSEModule>();
            ConvarsToAdd = new Queue<BaseConVar>();
            ConCommandsToAdd = new Queue<MethodInfo>();
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
                    foreach(FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (field.FieldType.IsSubclassOf(typeof(BaseConVar)))
                        {
                            ConvarsToAdd.Enqueue((BaseConVar) (field.GetValue(null)));
                        }
                    }
                    foreach(MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        var CCattr = method.GetCustomAttribute<RoR2.ConCommandAttribute>();
                        if (CCattr != null)
                        {
                            ConCommandsToAdd.Enqueue(method);
                        }
                    }
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
            if(ConvarsToAdd.Count>0)
                Logger.LogInfo($"Registering {ConvarsToAdd.Count} ConVars");
            var convarAddMethod = typeof(RoR2.Console).GetMethod("RegisterConVarInternal", BindingFlags.NonPublic | BindingFlags.Instance);
            while (ConvarsToAdd.Count > 0)
            {
                BaseConVar convar = ConvarsToAdd.Dequeue();
                convarAddMethod.Invoke(RoR2.Console.instance, new object[] { convar });
            }

            foreach (MethodInfo methodInfo in typeof(ConCommands).GetMethods())
            {
                var attr = methodInfo.GetCustomAttribute<RoR2.ConCommandAttribute>();
                if (attr == null)
                    continue;
                ConCommandsToAdd.Enqueue(methodInfo);
            }
            if (ConCommandsToAdd.Count > 0)
                Logger.LogInfo($"Registering {ConCommandsToAdd.Count} ConCommands");
            var CCcatalog = (IDictionary)typeof(RoR2.Console).GetField("concommandCatalog", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(RoR2.Console.instance);
            var CCtype = typeof(RoR2.Console).GetNestedType("ConCommand", BindingFlags.NonPublic);
            while (ConCommandsToAdd.Count > 0) {
                MethodInfo methodInfo = ConCommandsToAdd.Dequeue();
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
            catch (Exception e)
            {
                Logger.LogError($"Couldn't load module: {constuctorArgumentArray[0]}");
                Logger.LogError(e);
            }
        }

        private sealed class ModuleAndAttribute
        {
            public Type Module;
            public ModuleAttribute attribute;
        }
    }
}
