namespace R2DSEssentials.Modules.ModSyncHelper
{
    //Code by https://github.com/ReinMasamune
    public struct ModEntry
    {
        public string guid;
        public string enforceConfig;
        public string minVersion;
        public string maxVersion;


        //FORMAT:
        //GUID|Enforce Config|Min Version|Max Version
        //com.example.thing | false | 1.0.0 | 2.0.0
        public ModEntry(string text)
        {
            string[] splits = text.Split('|');

            guid = splits[0].Trim().ToLower();
            enforceConfig = splits[1].Trim().ToLower();
            minVersion = splits[2].Trim().ToLower();
            maxVersion = splits[3].Trim().ToLower();
        }

        public PrefEntry prefEntry => new PrefEntry(guid, enforceConfig, minVersion, maxVersion);
    }
}