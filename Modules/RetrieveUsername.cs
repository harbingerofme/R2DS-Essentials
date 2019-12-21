using System.Net;
using System.Text.RegularExpressions;
using RoR2;
using Facepunch.Steamworks;

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
    internal sealed class RetrieveUsername : R2DSEModule
    {
        public const string ModuleName = nameof(RetrieveUsername);
        public const string ModuleDescription = "Retrieve player usernames through third party website. Don't need a steam api key.";
        public const bool   DefaultEnabled = true;

        private readonly WebClient _webClient = new WebClient();

        public RetrieveUsername(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
        }


        protected override void Hook()
        {
            On.RoR2.NetworkPlayerName.GetResolvedName += OnGetResolvedName;
        }

        protected override void UnHook()
        {
            On.RoR2.NetworkPlayerName.GetResolvedName -= OnGetResolvedName;
        }

        protected override void MakeConfig()
        {
        }

        private string OnGetResolvedName(On.RoR2.NetworkPlayerName.orig_GetResolvedName orig, ref NetworkPlayerName self)
        {
            return Server.Instance != null ? GetPersonaNameWebAPI(self.steamId.value) : orig(ref self);
        }

        // ReSharper disable once InconsistentNaming
        private string GetPersonaNameWebAPI(ulong steamId)
        {
            const string regexForLookUp = "<dd class=\"value\"><a href=\"(.*?)\"";
            const string regexForPersonaName = "\"personaname\":\"(.*?)\"";
            const string unkString = "???";

            if (steamId.ToString().Length != 17)
                return unkString;

            var ioUrlRequest = "https://steamid.io/lookup/" + steamId;
            byte[] raw = _webClient.DownloadData(ioUrlRequest);

            var webData = System.Text.Encoding.UTF8.GetString(raw);
            var rx = new Regex(regexForLookUp,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var steamProfileUrl = rx.Match(webData).Groups[1].ToString();
            raw = _webClient.DownloadData(steamProfileUrl);

            webData = System.Text.Encoding.UTF8.GetString(raw);
            rx = new Regex(regexForPersonaName,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var personaName = rx.Match(webData).Groups[1].ToString();

            return !personaName.Equals("") ? personaName : unkString;
        }
    }
}
