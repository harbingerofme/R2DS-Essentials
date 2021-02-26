using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static R2DSEssentials.Util.Networking;

namespace R2DSEssentials.Modules
{
    [Module(ModuleName, ModuleDescription, DefaultEnabled)]
    [ModuleDependency(nameof(RetrieveUsername), ModuleDependency.DependencyType.Soft)]
    internal sealed class MotD : R2DSEModule
    {
        public const string ModuleName = nameof(MotD);
        public const string ModuleDescription = "Sends a configurable message to clients upon joining";
        public const bool DefaultEnabled = true;

        private const string _defaultMOTDValue = "<style=cIsDamage>Welcome</style> <style=cIsUtility>%USER%</style> (<color=yellow>%STEAM%</color>) - Time : <color=green>%TIME%</color>. This server runs: %MODLIST%";
        private const string _MOTDHelp = "You can use the following tokens: %STEAM%, %MODLIST%, %USER%, %TIME%. You can also use Unity Rich Text.";

        private const string _defaultMOTRValue = "<style=cIsHealing>This is a round message!</style>.";
        private const string _MOTRHelp = _MOTDHelp;//Sue me.

        private const int _defaultMOTRRoundsValue = 0;
        private const string _MOTRRoundsHelp = "When this is set to any whole positive number, the MOTR message will dispaly when the rounds are divided by this number.";//find a better way to explain modulo.

        private const string _defaultMOTHValue = "<style=cDeath>It's been 4 hours!</style>";
        private const string _MOTHHelp = _MOTDHelp;

        private const int _defaultMOTHTimeValue = 4 * 60;
        private const string _MOTHTimeHelp = "The amount of minutes between MOTH messages.";

        private const string _MOTSHelp = @"Replace with """" to ignore this.";

        private string modList = "";

        private static readonly ConfigConVar<string> MotdConVar = new ConfigConVar<string>("motd", ConVarFlags.None, _defaultMOTDValue, _MOTDHelp);
        private static readonly ConfigConVar<string> MotrConVar = new ConfigConVar<string>("motr", ConVarFlags.None, _defaultMOTRValue, _MOTRHelp);
        private static readonly ConfigConVar<string> MothConVar = new ConfigConVar<string>("moth", ConVarFlags.None, _defaultMOTHValue, _MOTHHelp);
        private static readonly ConfigConVar<int> MotrValConVar = new ConfigConVar<int>("motr_value", ConVarFlags.None, "0", _MOTRRoundsHelp);
        private static readonly ConfigConVar<int> MothValConVar = new ConfigConVar<int>("moth_value", ConVarFlags.None, "0", _MOTHTimeHelp);

        private ConfigEntry<string> motdConfig;
        private ConfigEntry<string> motrConfig;
        private ConfigEntry<string> mothConfig;
        private ConfigEntry<int> motrValConfig;
        private ConfigEntry<int> mothValConfig;

        private int lastStageCount;
        private DateTime time;

        private static readonly Dictionary<string, ConfigEntry<string>> stageMessages = new Dictionary<string, ConfigEntry<string>>();

        public MotD(string name, string description, bool defaultEnabled) : base(name, description, defaultEnabled)
        {
            lastStageCount = -1;
        }

        protected override void MakeConfig()
        {
            motdConfig = AddConfigConvar<string>("Message of the day", _defaultMOTDValue, _MOTDHelp, MotdConVar);
            motrConfig = AddConfigConvar<string>("Message of the round", _defaultMOTRValue, _MOTRHelp, MotrConVar);
            mothConfig = AddConfigConvar<string>("Message of the hour", _defaultMOTHValue, _MOTHHelp, MothConVar);
            motrValConfig = AddConfigConvar<int>("Rounds per message", _defaultMOTRRoundsValue, _MOTRRoundsHelp, MotrValConVar);
            mothValConfig = AddConfigConvar<int>("Rounds per message", _defaultMOTHTimeValue, _MOTHTimeHelp, MothValConVar);
        }

        private ConfigEntry<t> AddConfigConvar<t>(string key, t defaultValue, string description, ConfigConVar<t> conVar)
        {
            var entry = AddConfig(key, defaultValue, description);
            conVar.defaultValue = entry.GetSerializedValue();
            conVar.config = entry;
            return entry;
        }

        [ConCommand(commandName = "mots", flags = ConVarFlags.None, helpText = "mots <stage> <message>. " + _MOTDHelp)]
        private static void cc_mots(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);
            if (!PluginEntry.Modules[ModuleName].IsEnabled)
            {
                Debug.LogWarning("The Motd module is not enabled.");
            }
            var MOTD = (MotD)PluginEntry.Modules[ModuleName];
            string stage = args.GetArgString(0).ToLower();
            if (args.Count < 2)
            {
                string mots = MOTD.GetMotS(args.GetArgString(0));
                if (mots == "")
                {
                    Debug.LogFormat("Stage '{0}' does not have any message set.", stage);
                }
                else
                {
                    Debug.Log(mots);
                }
                return;
            }
            string newMessage = Util.Console.MergeArgs(args, 2);
            MOTD.SetMotS(stage, newMessage);
            Debug.Log("Something something mots set.");
        }

        public string GetMotS(string stageName)
        {
            string name = stageName.ToLower();
            if (stageMessages.ContainsKey(name))
                return stageMessages[name].Value;
            var configDef = new ConfigDefinition(ModuleName, name);
            if (PluginEntry.Configuration.ContainsKey(configDef))
            {
                stageMessages.Add(name, PluginEntry.Configuration.Bind<string>(configDef, ""));
                return stageMessages[name].Value;
            }
            return "";
        }

        public void SetMotS(string stageName, string newArgs)
        {
            string name = stageName.ToLower();
            if (stageMessages.ContainsKey(name))
            {
                stageMessages[name].Value = newArgs;
            }
            else
            {
                var configDef = new ConfigDefinition(ModuleName, name);
                if (PluginEntry.Configuration.ContainsKey(configDef))
                {
                    stageMessages.Add(name, PluginEntry.Configuration.Bind<string>(configDef, ""));
                    stageMessages[name].Value = newArgs;
                }
                else
                {
                    ConfigEntry<string> entry = PluginEntry.Configuration.Bind<string>(configDef, "", new ConfigDescription(_MOTSHelp));
                    entry.Value = newArgs;
                    stageMessages.Add(name, entry);
                }
            }
        }

        protected override void Hook()
        {
            time = DateTime.Now.AddMinutes(mothValConfig.Value);

            IL.RoR2.Networking.GameNetworkManager.OnServerAddPlayerInternal += MessageOnPlayerJoin;
            Stage.onServerStageBegin += MotrAndMots;
            Run.onRunStartGlobal += ResetStageCount;
            RoR2Application.onFixedUpdate += RoR2Application_onFixedUpdate;
        }

        private void RoR2Application_onFixedUpdate()
        {
            if (mothValConfig.Value > 0 && mothConfig.Value != "" && time < DateTime.Now)
            {
                time = time.AddMinutes(mothValConfig.Value);
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "{0}", paramTokens = new[] { mothConfig.Value } });
            }
        }

        private void ResetStageCount(Run obj)
        {
            lastStageCount = -1;
        }

        private void MotrAndMots(Stage obj)
        {
            if (motrConfig.Value != "" && motrValConfig.Value > 0 && Run.instance && Run.instance.stageClearCount != lastStageCount)
            {
                lastStageCount = Run.instance.stageClearCount;
                if (lastStageCount % motrValConfig.Value == 0)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "{0}", paramTokens = new[] { motrConfig.Value } });
                }
            }
            string mots = GetMotS(obj.sceneDef.baseSceneName);
            if (mots != "")
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "{0}", paramTokens = new[] { mots } });
            }
        }

        protected override void UnHook()
        {
            IL.RoR2.Networking.GameNetworkManager.OnServerAddPlayerInternal -= MessageOnPlayerJoin;
            Stage.onServerStageBegin -= MotrAndMots;
            Run.onRunStartGlobal -= ResetStageCount;
            RoR2Application.onFixedUpdate -= RoR2Application_onFixedUpdate;
        }

        private string GetModList()
        {
            if (modList == "")
            {
                List<string> nameList = new List<string>();
                foreach (KeyValuePair<string, PluginInfo> entry in BepInEx.Bootstrap.Chainloader.PluginInfos)
                {
                    nameList.Add($"[{entry.Value.Metadata.Name}]");
                }
                modList = string.Join(", ", nameList);
            }
            return modList;
        }

        private void MessageOnPlayerJoin(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(UnityEngine.Debug), "LogFormat"));
            c.GotoNext(MoveType.Before,
                x => x.MatchRet());
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<RuntimeILReferenceBag.FastDelegateInvokers.Action<NetworkConnection>>((conn) =>
            {
                if (PluginEntry.Modules.ContainsKey(nameof(RetrieveUsername)) && PluginEntry.Modules[nameof(RetrieveUsername)].IsEnabled)
                {
                    var authData = ServerAuthManager.FindAuthData(conn);
                    if (authData == null)
                    {
                        MakeAndSendMotd(conn);
                        return;
                    }

                    var steamId = ServerAuthManager.FindAuthData(conn).steamId.value;
                    if (RetrieveUsername.UsernamesCache.ContainsKey(steamId))
                    {
                        MakeAndSendMotd(conn);
                    }
                    else
                    {
                        void del()
                        {
                            MakeAndSendMotd(conn);
                            RetrieveUsername.OnUsernameUpdated -= del;
                        };
                        RetrieveUsername.OnUsernameUpdated += del;
                    }
                }
                else
                {
                    MakeAndSendMotd(conn);
                }
            });
        }

        private void MakeAndSendMotd(NetworkConnection connection)
        {
            var message = new Chat.SimpleChatMessage { baseToken = "{0}", paramTokens = new[] { GenerateMotDFormatted(connection) } };
            SendPrivateMessage(message, connection);
        }

        private string GenerateMotDFormatted(NetworkConnection conn)
        {
            string message = motdConfig.Value;
            if (message.Contains("%STEAM%"))
            {
                var authData = ServerAuthManager.FindAuthData(conn);
                if (authData != null)
                {
                    string steamId = ServerAuthManager.FindAuthData(conn).steamId.ToString();
                    message = message.Replace("%STEAM%", steamId);
                }
                else
                {
                    message = message.Replace("%STEAM%", "No Auth Data");
                }
            }

            if (message.Contains("%MODLIST%"))
            {
                message = message.Replace("%MODLIST%", GetModList());
            }

            if (message.Contains("%USER%"))
            {
                if (PluginEntry.Modules.ContainsKey(nameof(RetrieveUsername)) && PluginEntry.Modules[nameof(RetrieveUsername)].IsEnabled)
                {
                    NetworkUser networkUser = Util.Networking.FindNetworkUserForConnectionServer(conn);
                    message = message.Replace("%USER%", networkUser.userName);
                }
                else
                {
                    Logger.LogWarning($"MOTD: Can't replace %USER% as module `{nameof(RetrieveUsername)}` is not enabled.");
                }
            }

            if (message.Contains("%TIME%"))
            {
                message = message.Replace("%TIME%", DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss"));
            }

            return message;
        }
    }
}