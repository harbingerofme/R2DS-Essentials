using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using R2API;
using UnityEngine.Networking;
using RoR2.Networking;
using R2DSEssentials.Modules.ModSyncHelper;

namespace R2DSEssentials.Modules
{
    //Code by https://github.com/ReinMasamune
    
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
    [PluginDependency(GUID = R2API.R2API.PluginGUID)]
    public class ModSync : R2DSEModule
    {
        public const string ModuleName = nameof(ModSync);
        public const string ModuleDescription = "Enforce mods white/blacklists on clients. (requires R2API)";
        public const bool DefaultEnabled = false;

        private ConfigEntry<bool> cfgAllowVanillaPlayers;
        private ConfigEntry<bool> cfgAllowModdedPlayers;
        private ConfigEntry<bool> cfgEnforceRequiredMods;
        private ConfigEntry<bool> cfgEnforceBannedMods;
        private ConfigEntry<bool> cfgEnforceApprovedMods;

        public ModSync(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {

        }

        protected override void Hook()
        {
            ModListAPI.modlistRecievedFromClient += ModListAPI_modlistRecievedFromClient;
            ModListAPI.modlistRecievedFromServer += ModListAPI_modlistRecievedFromServer;
        }

        protected override void UnHook()
        {
            ModListAPI.modlistRecievedFromClient += ModListAPI_modlistRecievedFromClient;
            ModListAPI.modlistRecievedFromServer += ModListAPI_modlistRecievedFromServer;
        }

        private void ModListAPI_modlistRecievedFromServer(NetworkConnection connection, ModListAPI.ModList list)
        {
            Logger.LogWarning("Modlist recieved from server");
            foreach (ModListAPI.ModInfo mod in list.mods)
            {
                Logger.LogWarning(mod.guid + " : " + mod.version);
            }
        }

        private void ModListAPI_modlistRecievedFromClient(NetworkConnection connection, ModListAPI.ModList list,
            CSteamID steamID)
        {
            Logger.LogWarning("Modlist recieved from client with steamID: " + steamID.value);

            foreach (ModListAPI.ModInfo mod in list.mods)
            {
                Logger.LogWarning(mod.guid + " : " + mod.version);
            }

            if (!CheckList(list, GetModPrefs()))
            {
                GameNetworkManager.singleton.ServerKickClient(connection, GameNetworkManager.KickReason.BadVersion);
            }
        }

        private bool CheckList(ModListAPI.ModList clientList, ModPrefs modPrefs)
        {
            //Check vanilla
            bool vanillaCheck = true;
            if (!modPrefs.header.vanillaAllowed)
            {
                if (clientList.isVanilla)
                {
                    Logger.LogWarning("Kicking player due to disallowed vanilla players");
                    vanillaCheck = false;
                }
            }

            //Check modded
            bool moddedCheck = true;
            if (!modPrefs.header.moddedAllowed)
            {
                if (!clientList.isVanilla)
                {
                    Logger.LogWarning("Kicking player due to disallowed modded players");
                    moddedCheck = false;
                }
            }

            //Check required mods
            bool requiredCheck = true;
            if (modPrefs.header.enforceRequiredMods)
            {
                foreach (PrefEntry entry in modPrefs.requiredMods)
                {
                    bool tempCheck = false;
                    foreach (ModListAPI.ModInfo mod in clientList.mods)
                    {
                        if (entry.Check(mod))
                        {
                            tempCheck = true;
                        }
                    }

                    if (!tempCheck)
                    {
                        Logger.LogWarning("Kicking player due to missing required mod: " + entry.Guid);
                        requiredCheck = false;
                    }
                }
            }

            //Check banned
            bool bannedCheck = true;
            if (modPrefs.header.enforceBannedMods)
            {
                foreach (ModListAPI.ModInfo mod in clientList.mods)
                {
                    if (!modPrefs.bannedMods.TrueForAll((x) => !x.Check(mod)))
                    {
                        Logger.LogWarning("Kicking player due to banned mod: " + mod.guid);
                        bannedCheck = false;
                    }
                }
            }

            //Check approved
            bool allowedCheck = true;
            if (modPrefs.header.enforceApprovedMods)
            {
                foreach (ModListAPI.ModInfo mod in clientList.mods)
                {
                    bool tempCheck = false;
                    foreach (PrefEntry entry in modPrefs.approvedMods)
                    {
                        if (entry.Check(mod))
                        {
                            tempCheck = true;
                        }
                    }

                    if (modPrefs.header.enforceApprovedMods)
                    {
                        foreach (PrefEntry entry in modPrefs.requiredMods)
                        {
                            if (entry.Check(mod))
                            {
                                tempCheck = true;
                            }
                        }
                    }

                    if (!tempCheck)
                    {
                        Logger.LogWarning("Kicking player due to unapproved mod: " + mod.guid);
                    }
                }
            }

            return vanillaCheck && moddedCheck && requiredCheck && bannedCheck && allowedCheck;
        }

        private readonly string basePath = Path.PathSeparator + "ModSyncLists";
        private readonly string requiredPath = Path.PathSeparator + "RequiredMods.txt";
        private readonly string bannedPath = Path.PathSeparator + "BannedMods.txt";
        private readonly string approvedPath = Path.PathSeparator + "ApprovedMods.txt";

        const string infoEnd = "----------";

        private readonly string[] requiredModsInfo = new string[]
        {
            "Required Mods",
            "If enabled in the standard config, any connecting players must have all mods on this list installed.",
            "Format is GUID | Enforce Config | Min Version | Max Version",
            "Example: com.testMods.RandomMod | false | 0.0.0 | 2.1.0",
            "GUID = the name of the mod (Might not match thunderstore, need to ask the developer or decompile and check",
            "Enforce Config = does nothing at the moment, true or false",
            "Min Version = The lowest version number accepted. Leave blank for no minimum.",
            "Max Version = The highest version number accepted. Leave blank for no maximum.",
            "Recommended settings for min and max are minimum set to version installed on server, and max left blank.",
            infoEnd
        };

        private readonly string[] bannedModsInfo = new string[]
        {
            "Banned Mods",
            "If enabled in the standard config, players will be unable to connect with any of the mods on this list installed.",
            "Format is GUID | Enforce Config | Min Version | Max Version",
            "Example: com.testMods.RandomMod | false | 0.0.0 | 2.1.0",
            "GUID = the name of the mod (Might not match thunderstore, need to ask the developer or decompile and check",
            "Enforce Config = does nothing at the moment, true or false",
            "Min Version = The lowest version number accepted. Leave blank for no minimum.",
            "Max Version = The highest version number accepted. Leave blank for no maximum.",
            infoEnd
        };

        private readonly string[] approvedModsInfo = new string[]
        {
            "Approved Mods",
            "If enabled in the standard config, any players with mods that are not on this list (Or required list) will be unable to connect.",
            "Format is GUID | Enforce Config | Min Version | Max Version",
            "Example: com.testMods.RandomMod | false | 0.0.0 | 2.1.0",
            "GUID = the name of the mod (Might not match thunderstore, need to ask the developer or decompile and check",
            "Enforce Config = does nothing at the moment, true or false",
            "Min Version = The lowest version number accepted. Leave blank for no minimum.",
            "Max Version = The highest version number accepted. Leave blank for no maximum.",
            infoEnd
        };

        private readonly List<ModEntry> requiredEntries = new List<ModEntry>();
        private readonly List<ModEntry> bannedEntries = new List<ModEntry>();
        private readonly List<ModEntry> approvedEntries = new List<ModEntry>();

        private string BaseDirectoryPath =>
            Path.GetDirectoryName(PluginEntry.Configuration.ConfigFilePath) + basePath;

        private string FullRequiredPath => BaseDirectoryPath + requiredPath;
        private string FullBannedPath => BaseDirectoryPath + bannedPath;
        private string FullApprovedPath => BaseDirectoryPath + approvedPath;

        protected override void MakeConfig()
        {
            cfgAllowModdedPlayers = AddConfig("Allow Modded Players", true,
                "Should players with mods be allowed to connect?");
            cfgAllowVanillaPlayers = AddConfig("Allow Vanilla Players", false,
                "Should players without mods be allowed to connect?");
            cfgEnforceRequiredMods = AddConfig("Enforce Required Mods", false,
                "Should a list of required mods be enforced for connecting players?");
            cfgEnforceBannedMods = AddConfig("Enforce Banned Mods", false,
                "Should a list of banned mods be enforced for connecting players? (Blacklist)");
            cfgEnforceApprovedMods = AddConfig("Enforce Approved Mods", false,
                "Should a list of allowed mods be enforced for connecting players? (Whitelist)");

            if (!Directory.Exists(BaseDirectoryPath))
            {
                Directory.CreateDirectory(BaseDirectoryPath);
            }

            if (!File.Exists(FullRequiredPath))
            {
                Logger.LogInfo("Required Mods file not found, creating new.");

                File.AppendAllLines(FullRequiredPath, requiredModsInfo);
            }
            else
            {
                List<string> infoList = new List<string>();
                List<string> modsList = new List<string>();

                bool infoDone = false;
                foreach (string s in File.ReadAllLines(FullRequiredPath))
                {
                    if (infoDone)
                    {
                        modsList.Add(s);
                    }
                    else
                    {
                        if (s == infoEnd)
                        {
                            infoDone = true;
                        }
                        else
                        {
                            infoList.Add(s);
                        }
                    }
                }

                bool writeNewInfo = false;
                if (infoList.Count != requiredModsInfo.Length)
                {
                    writeNewInfo = true;
                }
                else
                {
                    for (Int32 i = 0; i < infoList.Count; ++i)
                    {
                        if (infoList[i] != requiredModsInfo[i]) writeNewInfo = true;
                    }
                }

                if (writeNewInfo)
                {
                    File.WriteAllLines(FullRequiredPath, requiredModsInfo);
                    File.AppendAllLines(FullRequiredPath, modsList);
                }

                foreach (string s in modsList)
                {
                    if (s == "")
                    {
                        continue;
                    }

                    try
                    {
                        ModEntry en = new ModEntry(s);
                        requiredEntries.Add(en);
                    }
                    catch
                    {
                        Logger.LogError("Invalid Line: " + s + " in required mods list, skipping.");
                    }
                }
            }

            if (!File.Exists(FullBannedPath))
            {
                Logger.LogInfo("Banned Mods file not found, creating new.");

                File.AppendAllLines(FullBannedPath, bannedModsInfo);
            }
            else
            {
                List<string> infoList = new List<string>();
                List<string> modsList = new List<string>();

                bool infoDone = false;
                foreach (string s in File.ReadAllLines(FullBannedPath))
                {
                    if (infoDone)
                    {
                        modsList.Add(s);
                    }
                    else
                    {
                        if (s == infoEnd)
                        {
                            infoDone = true;
                        }
                        else
                        {
                            infoList.Add(s);
                        }
                    }
                }

                bool writeNewInfo = false;
                if (infoList.Count != bannedModsInfo.Length)
                {
                    writeNewInfo = true;
                }
                else
                {
                    for (Int32 i = 0; i < infoList.Count; ++i)
                    {
                        if (infoList[i] != bannedModsInfo[i]) writeNewInfo = true;
                    }
                }

                if (writeNewInfo)
                {
                    File.WriteAllLines(FullBannedPath, bannedModsInfo);
                    File.AppendAllLines(FullBannedPath, modsList);
                }

                foreach (string s in modsList)
                {
                    if (s == "")
                    {
                        continue;
                    }

                    try
                    {
                        ModEntry en = new ModEntry(s);
                        bannedEntries.Add(en);
                    }
                    catch
                    {
                        Logger.LogError("Invalid Line: " + s + " in banned mods list, skipping.");
                    }
                }
            }

            if (!File.Exists(FullApprovedPath))
            {
                Logger.LogInfo("Approved Mods file not found, creating new.");

                File.AppendAllLines(FullApprovedPath, approvedModsInfo);
            }
            else
            {
                List<string> infoList = new List<string>();
                List<string> modsList = new List<string>();

                bool infoDone = false;
                foreach (string s in File.ReadAllLines(FullApprovedPath))
                {
                    if (infoDone)
                    {
                        modsList.Add(s);
                    }
                    else
                    {
                        if (s == infoEnd)
                        {
                            infoDone = true;
                        }
                        else
                        {
                            infoList.Add(s);
                        }
                    }
                }

                bool writeNewInfo = false;
                if (infoList.Count != approvedModsInfo.Length)
                {
                    writeNewInfo = true;
                }
                else
                {
                    for (Int32 i = 0; i < infoList.Count; ++i)
                    {
                        if (infoList[i] != approvedModsInfo[i]) writeNewInfo = true;
                    }
                }

                if (writeNewInfo)
                {
                    File.WriteAllLines(FullApprovedPath, approvedModsInfo);
                    File.AppendAllLines(FullApprovedPath, modsList);
                }

                foreach (string s in modsList)
                {
                    if (s == "")
                    {
                        continue;
                    }

                    try
                    {
                        ModEntry en = new ModEntry(s);
                        approvedEntries.Add(en);
                    }
                    catch
                    {
                        Logger.LogError("Invalid Line: " + s + " in approved mods list, skipping.");
                    }
                }
            }

        }

        private static ModPrefs prefs;

        private ModPrefs GetModPrefs()
        {
            if (prefs == null)
            {
                BuildModPrefs();
            }

            return prefs;
        }

        private void BuildModPrefs()
        {
            ModPrefs tempPrefs = new ModPrefs
            {
                header = new ModPrefs.Header
                {
                    vanillaAllowed = cfgAllowVanillaPlayers.Value,
                    moddedAllowed = cfgAllowModdedPlayers.Value,
                    enforceRequiredMods = cfgEnforceRequiredMods.Value,
                    enforceBannedMods = cfgEnforceBannedMods.Value,
                    enforceApprovedMods = cfgEnforceApprovedMods.Value
                },
                requiredMods = new List<PrefEntry>()
            };

            foreach (ModEntry entry in requiredEntries)
            {
                tempPrefs.requiredMods.Add(entry.PrefEntry);
            }

            tempPrefs.bannedMods = new List<PrefEntry>();
            foreach (ModEntry entry in bannedEntries)
            {
                tempPrefs.bannedMods.Add(entry.PrefEntry);
            }

            tempPrefs.approvedMods = new List<PrefEntry>();
            foreach (ModEntry entry in approvedEntries)
            {
                tempPrefs.approvedMods.Add(entry.PrefEntry);
            }

            prefs = tempPrefs;
        }
    }
}

