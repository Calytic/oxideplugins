using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Private Messages", "PaiN", "0.3.1", ResourceId = 2046)]
    class PrivateMessages : CovalencePlugin
    {
        [PluginReference]
        Plugin AntiAds;
        // Plugin BetterChat;

        ConfigFile Cfg = new ConfigFile();

        class ConfigFile
        {
            public bool Anti_Ads_PM_BlockAdverts;
            public bool ConsoleLogPM;
            //public bool BetterChat_PM_BlockIgnored;
        }

        public Dictionary<string, string> LastPerson = new Dictionary<string, string>();

        void Loaded() { Cfg = Config.ReadObject<ConfigFile>(); LoadMessages(); }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PM_SYNTAX"] = "/pm <Player> <Text>",
                ["PM_LOG"] = "*NEW* Sender: {0} || Receiver: {1} || Message {3}",
                ["PMR_SYNTAX"] = "/r <Text>",
                ["EMPTY_MSG"] = "You can not send empty messages!",
                ["NO_CONVERSATIONS"] = "You do not have any previous conversations!",
                ["NO_ADVERTS"] = "Advertisments are not allowed!",
                ["PLAYER_NOT_FOUND"] = "Player not found!",
                ["FROM_MSG"] = "FROM: {0} => {1}",
                ["TO_MSG"] = "TO: {0} => {1}",
                ["IS_IGNORING"] = "This player has ignored you."

            }, this);

        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file ...");
            Config.WriteObject(Cfg, true);
        }

        [Command("pm"), Permission("privatemessages.use")]
        void cmdPM(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                player.Message(LangMsg("PM_SYNTAX", player.Id));
                return;
            }

            string msg = string.Join(" ", args.Skip(1).ToArray());

            if (string.IsNullOrEmpty(msg))
            {
                player.Message(LangMsg("EMPTY_MSG", player.Id));
                return;
            }

            if (Cfg.Anti_Ads_PM_BlockAdverts)
            {
                bool isAdvert = (bool)AntiAds?.Call("IsAdvertisement", msg);

                if (isAdvert)
                {
                    player.Message(LangMsg("NO_ADVERTS", player.Id));
                    return;
                }
            }

            IPlayer target = players.FindPlayer(args[0]) ?? null;

            if (target == null)
            {
                player.Message(LangMsg("PLAYER_NOT_FOUND", player.Id));
                return;
            }

            /* if (Cfg.BetterChat_PM_BlockIgnored)
             {
                 bool IsIgnoring = (bool)BetterChat?.CallHook("API_PlayerIgnores", player.Id.ToString(), target.BasePlayer.Id.ToString());

                 if(IsIgnoring)
                 {
                     player.ConnectedPlayer.Message(LangMsg("IS_IGNORING", player.Id));
                     return;
                 }
             }*/

            target.Message(string.Format(LangMsg("FROM_MSG", player.Id), player.Name, msg));
            player.Message(string.Format(LangMsg("TO_MSG", player.Id), target.Name, msg));

            if (Cfg.ConsoleLogPM)
                Puts(string.Format(LangMsg("PM_LOG"), player.Name, target.Name, msg));

            if (!LastPerson.ContainsKey(player.Id))
                LastPerson.Add(player.Id, target.Id);
            else
                LastPerson[player.Id] = target.Id;

        }

        [Command("r"), Permission("privatemessages.use")]
        void cmdReply(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                player.Message(LangMsg("PMR_SYNTAX", player.Id));
                return;
            }

            if (!LastPerson.ContainsValue(player.Id))
            {
                player.Message(LangMsg("NO_CONVERSATIONS", player.Id));
                return;
            }

            string msg = string.Join(" ", args);

            if (Cfg.Anti_Ads_PM_BlockAdverts)
            {
                bool isAdvert = (bool)AntiAds?.Call("IsAdvertisement", msg);

                if (isAdvert)
                {
                    player.Message(LangMsg("NO_ADVERTS", player.Id));
                    return;
                }
            }

            IPlayer LastSender = players.FindPlayer(LastPerson.FirstOrDefault(x => x.Value == player.Id).Key);

            if (LastSender == null)
            {
                player.Message(LangMsg("PLAYER_NOT_FOUND", player.Id));
                return;
            }

            /*if (Cfg.BetterChat_PM_BlockIgnored)
            {
                bool IsIgnoring = (bool)BetterChat?.Call("API_PlayerIgnores", player.Id.ToString(), LastSender?.BasePlayer.Id.ToString());

                if (IsIgnoring)
                {
                    player.ConnectedPlayer.Message(LangMsg("IS_IGNORING", player.Id));
                    return;
                }
            }*/

            LastSender.Message(string.Format(LangMsg("FROM_MSG", player.Id), player.Name, msg));
            player.Message(string.Format(LangMsg("TO_MSG", player.Id), LastSender.Name, msg));

            if (Cfg.ConsoleLogPM)
                Puts(string.Format(LangMsg("PM_LOG"), player.Name, LastSender.Name, msg));

            if (!LastPerson.ContainsKey(player.Id))
                LastPerson.Add(player.Id, LastSender.Id);
            else
                LastPerson[player.Id] = LastSender.Id;
        }

        string LangMsg(string msg, string uid = null) => lang.GetMessage(msg, this, uid);
    }
}
