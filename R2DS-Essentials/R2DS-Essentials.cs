using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using Facepunch.Steamworks;
using Path = System.IO.Path;

namespace DS_UsernameFix
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class R2DS-Essentials : BaseUnityPlugin
    {
        private const string ModVer = "0.0.1";
        private const string ModName = "R2DS-Essentials";
        public const string ModGuid = "com.iDeathHD.R2DS-Essentials";

        private static string SteamWebAPIKey;
        private const string SteamWebAPIKeyFileName = "SteamWebAPI.key";

        public R2DS-Essentials()
        {
            if (!ReadSteamWebAPIKeyFromFile(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName + $"/{SteamWebAPIKeyFileName}", out SteamWebAPIKey))
            {
                return;
            }
            
            On.RoR2.NetworkPlayerName.GetResolvedName += NetworkPlayerNameOnGetResolvedName;
        }

        private string NetworkPlayerNameOnGetResolvedName(On.RoR2.NetworkPlayerName.orig_GetResolvedName orig, ref RoR2.NetworkPlayerName self)
        {
            return Server.Instance != null ? GetPersonaNameWebAPI(self.steamId.value) : orig(ref self);
        }

        private string GetPersonaNameWebAPI(ulong steamId)
        {
            const string unkString = "???";

            if (steamId.ToString().Length != 17)
                return unkString;

            var wc = new System.Net.WebClient();
            var address = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + SteamWebAPIKey + "&steamids=" + steamId;
            byte[] raw = wc.DownloadData(address);

            var webData = System.Text.Encoding.UTF8.GetString(raw);
            const string regexPattern = "\"personaname\":\"(.*?)\"";
            var rx = new Regex(regexPattern,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var personaName = rx.Match(webData).Groups[1].ToString();

            return !personaName.Equals("") ? personaName : unkString;
        }

        private bool ReadSteamWebAPIKeyFromFile(string fileName, out string key)
        {
            key = null;
            if (!File.Exists(fileName))
            {
                File.CreateText(fileName);
                Path.ChangeExtension(fileName, ".key");

                return false;
            }

            using (var sr = File.OpenText(fileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Length == 32)
                    {
                        key = line;
                        return true;
                    }

                    Logger.LogError($"Could not find a correct key in the file {SteamWebAPIKeyFileName}. Please put a correct key in it. See https://steamcommunity.com/dev/apikey for more information.");
                }
            }

            Logger.LogError($"Could not find the file {SteamWebAPIKeyFileName} that contains the Steam Web API Key. Please put a key in it. See https://steamcommunity.com/dev/apikey for more information.");
            return false;
        }
    }
}