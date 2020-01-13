using System;
using R2API;

namespace R2DSEssentials.Modules.ModSyncHelper
{
    //Code by https://github.com/ReinMasamune
    public class PrefEntry
    {
        public string guid { get; private set; }
        public bool enforceConfig { get; private set; }
        public Version minVersion { get; private set; }
        public Version maxVersion { get; private set; }

        public bool useMinVersion
        {
            get { return minVersion != null; }
        }

        public bool useMaxVersion
        {
            get { return maxVersion != null; }
        }

        public bool Check(ModListAPI.ModInfo mod)
        {
            if (mod == null) return false;
            if (guid != mod.guid.ToLower()) return false;
            if (useMinVersion && mod.version < minVersion) return false;
            if (useMaxVersion && mod.version > maxVersion) return false;
            return true;
        }

        internal PrefEntry(string guid, string enforceConfig, string minVersion, string maxVersion)
        {
            this.guid = guid;
            this.enforceConfig = (enforceConfig == "true");


            Version.TryParse(minVersion, out Version minVer);
            Version.TryParse(maxVersion, out Version maxVer);

            this.minVersion = minVer;
            this.maxVersion = maxVer;
        }
    }
}