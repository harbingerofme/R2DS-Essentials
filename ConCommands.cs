using RoR2;
using UnityEngine;

namespace R2DSEssentials
{
    internal static class ConCommands
    {
        [ConCommand(commandName = "r2dse_submodule", flags = ConVarFlags.None, helpText = "Enable or disable modules. r2dse_submodule <name> 0/1")]
        public static void CCSubmodule(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);
            string module = args.GetArgString(0);
            if (PluginEntry.Modules.ContainsKey(module))
            {
                if (args.Count == 1)
                {
                    Debug.LogFormat($"Module {module} is {(PluginEntry.Modules[module].IsEnabled ? "enabled" : "disabled")}.");
                    return;
                }
                bool? value = args.TryGetArgBool(1);
                if (value.HasValue)
                {
                    PluginEntry.Modules[module].IsEnabled = value.Value;
                }
                else
                {
                    Debug.Log("Cannot parse second argument.");
                }
            }
            else
            {
                Debug.LogFormat("Module not found. Available modules: {0}", string.Join(", ", PluginEntry.Modules.Keys));
            }
        }
    }
}