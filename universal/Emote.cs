using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Emote", "Hirsty", "1.0.8", ResourceId = 1353)]
    [Description("This will allow players to express their feelings!")]
    class Emote : CovalencePlugin
    {

        public static string version = "1.0.8";
        public string template = "";
        public string EnableEmotes = "false";
        #region Config Data
        protected override void LoadDefaultConfig()
        {

            PrintWarning("Whoops! No config file, lets create a new one!"); // Runs when no configuration file has been found
            Config.Clear();
            Config["Plugin", "Version"] = version;
            Config["Config", "Text"] = "<color=#f0f0f0><i><b>{Player}</b> {Message}</i></color>";
            Config["Config", "EnableEmotes"] = "false";
            Config["Config", "BetterChat"] = "false";
            Config["Emotes", ":)"] = "smiles";
            Config["Emotes", ":-)"] = "smiles";
            Config["Emotes", ":=)"] = "smiles";
            Config["Emotes", ":("] = "sulks";
            Config["Emotes", ":-("] = "sulks";
            Config["Emotes", ":=("] = "sulks";
            Config["Emotes", ":D"] = "grins";
            Config["Emotes", ":-D"] = "grins";
            Config["Emotes", ":=D"] = "grins";
            Config["Emotes", ":d"] = "grins";
            Config["Emotes", ":-d"] = "grins";
            Config["Emotes", ":=d"] = "grins";
            Config["Emotes", ":|"] = "is speechless";
            Config["Emotes", ":-|"] = "is speechless";
            Config["Emotes", ":=|"] = "is speechless";
            Config["Emotes", ":p"] = "sticks out a tongue";
            Config["Emotes", ":-p"] = "sticks out a tongue";
            Config["Emotes", ":=p"] = "sticks out a tongue";
            Config["Emotes", ":P"] = "sticks out a tongue";
            Config["Emotes", ":-P"] = "sticks out a tongue";
            Config["Emotes", ":=P"] = "sticks out a tongue";
            Config["Emotes", ":$"] = "blushes";
            Config["Emotes", ":-$"] = "blushes";
            Config["Emotes", ":=$"] = "blushes";
            Config["Emotes", "]:)"] = "gives an evil grin";
            Config["Emotes", ">:)"] = "gives an evil grin";
            Config["Emotes", ":*"] = "blows a kiss";
            Config["Emotes", ":-*"] = "blows a kiss";
            Config["Emotes", ":=*"] = "blows a kiss";
            Config["Emotes", ":@"] = "looks angry";
            Config["Emotes", ":-@"] = "looks angry";
            Config["Emotes", ":=@"] = "looks angry";
            Config["Emotes", "x("] = "looks angry";
            Config["Emotes", "x-("] = "looks angry";
            Config["Emotes", "x=("] = "looks angry";
            Config["Emotes", "X("] = "looks angry";
            Config["Emotes", "X-("] = "looks angry";
            Config["Emotes", "X=("] = "looks angry";
            Config["Emotes", "o/"] = "waves";
            Config["Emotes", "\\o"] = "waves back";
            SaveConfig();
        }
        private void Loaded() => LoadConfigData(); // What to do when plugin loaded
        [PluginReference] Plugin BetterChat;
        private void LoadConfigData()
        {
            if(BetterChat){
                Puts("We found BetterChat! Enabling Support.");
                Config["Config", "BetterChat"] = "true";
            } else {
                
                Puts("No BetterChat found! Disabling Support.");
                Config["Config", "BetterChat"] = "false";
            }
            if (Config["Plugin", "Version"].ToString() != version)
            {
                Puts("Uh oh! Not up to date! No Worries, lets update you!");
                switch (version)
                {
                    case "1.0.1":
                        Config["Config", "EnableEmotes"] = "false";
                        Config["Emotes", ":)"] = "smiles";
                        Config["Emotes", ":-)"] = "smiles";
                        Config["Emotes", ":=)"] = "smiles";
                        Config["Emotes", ":("] = "sulks";
                        Config["Emotes", ":-("] = "sulks";
                        Config["Emotes", ":=("] = "sulks";
                        Config["Emotes", ":D"] = "grins";
                        Config["Emotes", ":-D"] = "grins";
                        Config["Emotes", ":=D"] = "grins";
                        Config["Emotes", ":d"] = "grins";
                        Config["Emotes", ":-d"] = "grins";
                        Config["Emotes", ":=d"] = "grins";
                        Config["Emotes", ":|"] = "is speechless";
                        Config["Emotes", ":-|"] = "is speechless";
                        Config["Emotes", ":=|"] = "is speechless";
                        Config["Emotes", ":p"] = "sticks out a tongue";
                        Config["Emotes", ":-p"] = "sticks out a tongue";
                        Config["Emotes", ":=p"] = "sticks out a tongue";
                        Config["Emotes", ":P"] = "sticks out a tongue";
                        Config["Emotes", ":-P"] = "sticks out a tongue";
                        Config["Emotes", ":=P"] = "sticks out a tongue";
                        Config["Emotes", ":$"] = "blushes";
                        Config["Emotes", ":-$"] = "blushes";
                        Config["Emotes", ":=$"] = "blushes";
                        Config["Emotes", "]:)"] = "gives an evil grin";
                        Config["Emotes", ">:)"] = "gives an evil grin";
                        Config["Emotes", ":*"] = "blows a kiss";
                        Config["Emotes", ":-*"] = "blows a kiss";
                        Config["Emotes", ":=*"] = "blows a kiss";
                        Config["Emotes", ":@"] = "looks angry";
                        Config["Emotes", ":-@"] = "looks angry";
                        Config["Emotes", ":=@"] = "looks angry";
                        Config["Emotes", "x("] = "looks angry";
                        Config["Emotes", "x-("] = "looks angry";
                        Config["Emotes", "x=("] = "looks angry";
                        Config["Emotes", "X("] = "looks angry";
                        Config["Emotes", "X-("] = "looks angry";
                        Config["Emotes", "X=("] = "looks angry";
                        Config["Emotes", "o/"] = "waves";
                        Config["Emotes", "\\o"] = "waves back";
                        break;
                    case "1.0.6":
                        Config["Config", "BetterChat"] = "false";
                        break;

                }
                Config["Plugin", "Version"] = version;
                Config.Save();
            }
            template = Config["Config", "Text"].ToString();
            EnableEmotes = Config["Config", "EnableEmotes"].ToString();

        }
        #endregion
        #region Hooks
        [HookMethod("CheckForEmotes")]
        public string EmoteCheck(IPlayer player, string checkmsg)
        {
            if (Config["Emotes", checkmsg] != null && EnableEmotes == "true")
            {
                string emote = Config["Emotes", checkmsg].ToString();

                string build = template;
                build = build.Replace("{Player}", player.Name);
                build = build.Replace("{Message}",  emote);
                return build;
            }
            else
            {
                return checkmsg;
            }
        }
        #endregion
        #region Chat Commands
        [Command("me"), Permission("emote.use")] // Whatever cammand you want the player to type
        object MeCommand(IPlayer player, string command, string[] args)
        {
            if (player.LastCommand == CommandType.Console) return null;
            string uid = player.Id.ToString();
            if(BetterChat){
                if ((bool)BetterChat.Call("API_IsPlayerMuted",player.Id)){
                    return false;
                }
            }
            string full = string.Join(" ", args);
            SendChatMessage(player, full);
            return null;
        
        }
        #endregion
        object OnUserChat(IPlayer player, string message)
        {
            string uid = player.Id;

            if(BetterChat){
                return null;
            }
           if (Config["Emotes", message] != null && EnableEmotes == "true" && player.HasPermission("emote.canemote"))
           {
               SendChatMessage(player, Config["Emotes", message].ToString());
               return false;
               
            }
            return null;
        }
        object OnBetterChat(IPlayer player, string message)
        {
            
            if(player==null){
                return false;
            }
            string uid = player.Id;
           if (Config["Emotes", message] != null && EnableEmotes == "true" && player.HasPermission("emote.canemote"))
           {
               SendChatMessage(player, Config["Emotes", message].ToString());
               return false;
            }
            return null;
        }

        object SendChatMessage(IPlayer player, string msg)
        {
            string build = template;
            build = build.Replace("{Player}", player.Name);
            build = build.Replace("{Message}", msg);
            server.Broadcast(build);
            Puts(player.Name + " emoted: " + msg);
            return false;
        }
    }
}
