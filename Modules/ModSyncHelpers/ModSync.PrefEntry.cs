using System;
using R2API;

namespace R2DSEssentials.Modules.ModSyncHelper
{
    //Code by https://github.com/ReinMasamune
    public class PrefEntry
    {
        public string Guid { get; private set; }
        public bool EnforceConfig { get; private set; }
        public Version MinVersion { get; private set; }
        public Version MaxVersion { get; private set; }

        public bool UseMinVersion => MinVersion != null;

        public bool UseMaxVersion => MaxVersion != null;

        public bool Check(ModListAPI.ModInfo mod)
        {
            if (mod == null) return false;
            if (Guid != mod.Guid.ToLower()) return false;
            if (UseMinVersion && mod.Version < MinVersion) return false;
            if (UseMaxVersion && mod.Version > MaxVersion) return false;
            return true;
        }

        internal PrefEntry(string guid, string enforceConfig, string minVersion, string maxVersion)
        {
            Guid = guid;
            EnforceConfig = (enforceConfig == "true");


            Version.TryParse(minVersion, out Version minVer);
            Version.TryParse(maxVersion, out Version maxVer);

            MinVersion = minVer;
            MaxVersion = maxVer;
        }
    }
}