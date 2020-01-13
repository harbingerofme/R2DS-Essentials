using System.Collections.Generic;

namespace R2DSEssentials.Modules.ModSyncHelper
{
    //Code by https://github.com/ReinMasamune
    public class ModPrefs
    {
        public Header header;
        public List<PrefEntry> requiredMods;
        public List<PrefEntry> bannedMods;
        public List<PrefEntry> approvedMods;

        public class Header
        {
            public bool vanillaAllowed;
            public bool moddedAllowed;
            public bool enforceRequiredMods;
            public bool enforceBannedMods;
            public bool enforceApprovedMods;
        }
    }
}