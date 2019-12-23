using BepInEx.Configuration;
using RoR2.ConVar;
using RoR2;

namespace R2DSEssentials
{
    public class ConfigConVar<T> : BaseConVar
    {
        public ConfigConVar(string name, ConVarFlags flags, string defaultValue, string helpText) : base(name, flags, defaultValue, helpText)
        {
        }

        public ConfigEntry<T> config;

        public override string GetString()
        {
            return config.GetSerializedValue();
        }

        public override void SetString(string newValue)
        {
            config.SetSerializedValue(newValue);
        }
    }
}
