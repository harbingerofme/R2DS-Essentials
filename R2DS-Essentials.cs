using System.Text.RegularExpressions;
using BepInEx;
using Facepunch.Steamworks;

namespace R2DSEssentials
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class R2DSEssentials : BaseUnityPlugin
    {
        private const string ModVer = "0.0.1";
        private const string ModName = "R2DSEssentials";
        public const string ModGuid = "com.iDeathHD." + ModName;

        public void Awake()
        {
            On.RoR2.NetworkPlayerName.GetResolvedName += NetworkPlayerNameOnGetResolvedName;
        }

        private string NetworkPlayerNameOnGetResolvedName(On.RoR2.NetworkPlayerName.orig_GetResolvedName orig, ref RoR2.NetworkPlayerName self)
        {
            return Server.Instance != null ? GetPersonaNameWebAPI(self.steamId.value) : orig(ref self);
        }

        private string GetPersonaNameWebAPI(ulong steamId)
        {
            const string regexForLookUp = "<dd class=\"value\"><a href=\"(.*?)\"";
            const string regexForPersonaName = "\"personaname\":\"(.*?)\"";
            const string unkString = "???";

            if (steamId.ToString().Length != 17)
                return unkString;

            var wc = new System.Net.WebClient();

            var steamidIo = "https://steamid.io/lookup/" + steamId;
            byte[] raw = wc.DownloadData(steamidIo);

            var webData = System.Text.Encoding.UTF8.GetString(raw);
            var rx = new Regex(regexForLookUp,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var steamProfileUrl = rx.Match(webData).Groups[1].ToString();
            raw = wc.DownloadData(steamProfileUrl);

            webData = System.Text.Encoding.UTF8.GetString(raw);
            rx = new Regex(regexForPersonaName,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var personaName = rx.Match(webData).Groups[1].ToString();

            return !personaName.Equals("") ? personaName : unkString;
        }
    }
}