using System;
using System.Collections.Generic;
using Oxide.Core;
using System.Text.RegularExpressions;
namespace Oxide.Plugins
{
    [Info("MagicChat", "Norn", 0.2, ResourceId = 1595)]
    [Description("An alternative chat system for HurtWorld.")]
    public class MagicChat : HurtworldPlugin
    {
        string DEFAULT_COLOR = "#81DAF5";

        class StoredData
        {
            public Dictionary<string, UserInfo> Users = new Dictionary<string, UserInfo>();
            public StoredData()
            {
            }
        }

        class UserInfo
        {
            public string tUserId; // Steam ID
            public string tLastName; // Display Name
            public bool uVoiceMuted; // Voice Chat
            public bool uPublicMuted; // Public Chat (OOC)
            public bool uLocalMuted; // Local Chat
            public bool uCanColor; // Name Color
            public string tColor; // Color hex
            public bool uColorEnabled;
            public bool uCanCustomTag; // Can Use Custom Tag In Chat
            public bool uCustomTagEnabled; // Enabled Or Not
            public string tCustomTag; // The custom tag string
            public int iMessagesSent; // Message count
            public int iInitTimestamp; // First Init
            public int iLastSeenTimestamp;
            public bool uShowPublicChat;
            public UserInfo()
            {
            }
        }

        StoredData MCData;
        private void Loaded()
        {
            LoadMessages();
            MCData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
        }
        void OnServerInitialized()
        {
            
        }
        void Unload()
        {
            SaveData();
        }
        private bool ContainsHTML(string CheckString)
        {
            return Regex.IsMatch(CheckString, "<(.|\n)*?>");
        }
        string GetUserTag(PlayerSession player)
        {
            UserInfo p; string tag = "None";
            if (MCData.Users.TryGetValue(player.SteamId.ToString().ToString(), out p))
            {
                if (p.tCustomTag.Length >= 1) tag = "<color=" + Config["General", "UserTagColor"].ToString() + ">" + p.tCustomTag + "</color>";
            }
            return tag;
        }
        bool UserUpdateColor(PlayerSession player, string color)
        {
            UserInfo p;
            if (MCData.Users.TryGetValue(player.SteamId.ToString().ToString(), out p))
            {
                p.tColor = color;
                return true;
            }
            return false;
        }
        bool UserUpdateTag(PlayerSession player, string tag)
        {
            UserInfo p;
            if (MCData.Users.TryGetValue(player.SteamId.ToString().ToString(), out p))
            {
                p.tCustomTag = tag;
                return true;
            }
            return false;
        }

        [ChatCommand("chat")]
        private void ChatCommand(PlayerSession player, string command, string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                string default_command = null;
                if (CanUserCustomColor(player))
                { default_command += " | color "; }
                if (CanUserCustomTag(player))
                { default_command += " | tag "; }
                hurt.SendChatMessage(player, "USAGE: /chat <" + default_command + " | public>");
                if (player.IsAdmin)
                {
                    hurt.SendChatMessage(player, msg("AdminCmd", player.SteamId.ToString()));
                }
                if (UserDataExists(player))
                {
                    string parsed_config = msg("PlayerInfo", player.SteamId.ToString());
                    parsed_config = parsed_config.Replace("{name}", player.Name);
                    parsed_config = parsed_config.Replace("{tag}", GetUserTag(player));
                    parsed_config = parsed_config.Replace("{messages_sent}", MCData.Users[player.SteamId.ToString()].iMessagesSent.ToString());
                    hurt.SendChatMessage(player, parsed_config);
                }
            }
            else if (args[0] == "public")
            {
                if (UserDataExists(player))
                {
                    if (MCData.Users[player.SteamId.ToString()].uShowPublicChat) { MCData.Users[player.SteamId.ToString()].uShowPublicChat = false; } else { MCData.Users[player.SteamId.ToString()].uShowPublicChat = true; }
                    if (MCData.Users[player.SteamId.ToString()].uShowPublicChat) { hurt.SendChatMessage(player, msg("PublicVisible", player.SteamId.ToString())); } else { hurt.SendChatMessage(player, msg("PublicNotVisible", player.SteamId.ToString())); }
                }
            }
            else if (args[0] == "color")
            {
                if (CanUserCustomColor(player))
                {
                    if (args.Length == 2)
                    {
                        if (args[1].Length >= 1 && args.Length <= Convert.ToInt32(Config["General", "MaxColorLength"]))
                        {
                            if (args[1] == "red" && !player.IsAdmin) { hurt.SendChatMessage(player, msg("ReservedColor", player.SteamId.ToString())); return; }
                            if (UserUpdateColor(player, args[1]))
                            {
                                string parsed_config = msg("ColorUpdated", player.SteamId.ToString());
                                parsed_config = parsed_config.Replace("{c1}", args[1]);
                                parsed_config = parsed_config.Replace("{c2}", args[1]);
                                hurt.SendChatMessage(player, parsed_config);
                            }
                        }
                        else
                        {
                            hurt.SendChatMessage(player, msg("InvalidColor", player.SteamId.ToString()));
                        }

                    }
                    else
                    {
                        if (UserColorToggle(player)) { hurt.SendChatMessage(player, msg("ColorEnabled", player.SteamId.ToString())); } else { hurt.SendChatMessage(player, msg("ColorDisabled", player.SteamId.ToString())); }
                        hurt.SendChatMessage(player, msg("ChatColorUsage", player.SteamId.ToString()));
                    }

                }
                else
                {
                    hurt.SendChatMessage(player, msg("NoPermission", player.SteamId.ToString()));
                }
            }
            else if (args[0] == "tag")
            {
                if (CanUserCustomTag(player))
                {
                    if (args.Length == 2)
                    {
                        if (args[1].Length >= 1 && args.Length <= Convert.ToInt32(Config["General", "MaxTagLength"]))
                        {
                            if (UserUpdateTag(player, args[1]))
                            {
                                string parsed_config = msg("TagUpdated", player.SteamId.ToString());
                                parsed_config = parsed_config.Replace("{tagcolor}", Config["General", "UserTagColor"].ToString());
                                parsed_config = parsed_config.Replace("{tag}", GetUserTag(player));
                                hurt.SendChatMessage(player, parsed_config);
                            }
                        }
                        else
                        {
                            hurt.SendChatMessage(player, msg("InvalidColor", player.SteamId.ToString()));
                        }

                    }
                    else
                    {
                        if (UserTagToggle(player)) { hurt.SendChatMessage(player, msg("TitleEnabled", player.SteamId.ToString())); } else { hurt.SendChatMessage(player, msg("TitleDisabled", player.SteamId.ToString())); }
                        hurt.SendChatMessage(player, msg("ChatTagUsage", player.SteamId.ToString()));
                    }

                }
                else
                {
                    hurt.SendChatMessage(player, msg("NoTag", player.SteamId.ToString()));
                }
            }
            else if (args[0] == "clear")
            {
                if (player.IsAdmin)
                {
                    MCData.Users.Clear();
                    SaveData();
                    hurt.SendChatMessage(player, msg("DBCleared", player.SteamId.ToString()));
                }
                else
                {
                    hurt.SendChatMessage(player, msg("NotAdmin", player.SteamId.ToString()));
                }
            }
        }
        void OnPlayerRespawn(PlayerSession session)
        {
            if (!UserDataExists(session)) InitUserData(session);
        }
        public static Int32 UnixTimeStampUTC()
        {
            Int32 unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }
        object OnPlayerChat(PlayerSession player, string text)
        {
            if (text.StartsWith("/"))
                return false;
            if (ContainsHTML(text))
                return false;
            if (text.StartsWith(Config["General", "PublicPrefix"].ToString()))
            {
                if (Convert.ToBoolean(Config["Public", "Enabled"]))
                {
                    if (MCData.Users[player.SteamId.ToString()].uShowPublicChat)
                    {
                        string final_text = text.Remove(0, Config["General", "PublicPrefix"].ToString().Length);
                        if (final_text.Length >= 1 && player != null) UserTextPublic(player, final_text);
                    }
                    else
                    {
                        hurt.SendChatMessage(player, msg("NoPublic", player.SteamId.ToString()));
                    }
                }
                else
                {
                    hurt.SendChatMessage(player, msg("PublicDisabled", player.SteamId.ToString()));
                }
            }
            else
            {
                if (Convert.ToBoolean(Config["Local", "Enabled"]))
                {
                    if (text.Length >= 1 && player.IsLoaded) UserTextRadius(player, Convert.ToDouble(Config["Local", "Radius"]), text, UserNameColor(player));
                }
                else return null;
            }
            return false;
        }
        private void UserTextPublic(PlayerSession player, string text)
        {
            string end_result = null;
            if (player != null && player.IsLoaded)
            {
                if (!UserDataExists(player)) { InitUserData(player); }
                if (Convert.ToBoolean(Config["Public", "Enabled"]))
                {
                    UserInfo user = null;
                    if (MCData.Users.TryGetValue(player.SteamId.ToString().ToString(), out user))
                    {
                        if (user.uPublicMuted)
                        {
                            string parsed_config = msg("Muted", player.SteamId.ToString());
                            parsed_config = parsed_config.Replace("{room}", Config["Public", "ChatPrefex"].ToString().ToLower());
                            hurt.SendChatMessage(player, parsed_config);
                            return;
                        }
                        if (user.uCanCustomTag && user.uCustomTagEnabled && user.tCustomTag.Length >= 1)
                        {
                            end_result = "[<color=" + Config["Public", "PrefixColor"].ToString() + ">" + Config["Public", "ChatPrefex"].ToString() + "</color>] [<color=" + Config["General", "UserTagColor"].ToString() + ">" + user.tCustomTag + "</color>] <color=" + UserNameColor(player) + ">" + player.Name + "</color>: " + text;
                        }
                        else
                        {
                            end_result = "[<color=" + Config["Public", "PrefixColor"].ToString() + ">" + Config["Public", "ChatPrefex"].ToString() + "</color>] <color=" + UserNameColor(player) + ">" + player.Name + "</color>: " + text;
                        }
                        user.iMessagesSent++;
                        Puts("[" + Config["Public", "ChatPrefex"].ToString() + "] " + player.Name + ": " + text);
                        foreach (var pair in GameManager.Instance.GetSessions().Values)
                        {
                            if (!pair.IsLoaded) continue;
                            if (!UserDataExists(pair)) InitUserData(pair);
                            if (MCData.Users[player.SteamId.ToString()].uShowPublicChat)
                            {
                                hurt.SendChatMessage(pair, end_result);
                            }
                        }
                    }
                }
            }
        }
        private string UserNameColor(PlayerSession i)
        {
            string color = DEFAULT_COLOR;
            if (i != null && i.IsLoaded)
            {
                UserInfo user = null;
                if (MCData.Users.TryGetValue(i.SteamId.ToString(), out user))
                {
                    if (user.uCanColor)
                    {
                        if (user.uColorEnabled)
                        {
                            if (user.tColor.Length >= 1)
                            { color = user.tColor; }
                            else
                            { user.tColor = color; }
                        }
                    }
                }
            }
            return color;
        }
        private void UserTextRadius(PlayerSession player, double radius, string text, string name_color = "")
        {
            if (!UserDataExists(player)) { InitUserData(player); }
            if (player.IsLoaded)
            {
                float posx;
                float posy;
                float posz;
                float oldposx = 0.0f;  float oldposy = 0.0f; float oldposz = 0.0f; float tempposx = 0.0f; float tempposy = 0.0f; float tempposz = 0.0f;
                oldposx = player.WorldPlayerEntity.transform.position.x;
                oldposy = player.WorldPlayerEntity.transform.position.y;
                oldposz = player.WorldPlayerEntity.transform.position.z;
                string gradient1 = Config["FadeGradient", "1"].ToString();
                string gradient2 = Config["FadeGradient", "2"].ToString();
                string gradient3 = Config["FadeGradient", "3"].ToString();
                string gradient4 = Config["FadeGradient", "4"].ToString();
                string gradient5 = Config["FadeGradient", "5"].ToString();
                foreach(PlayerSession i in GameManager.Instance.GetSessions().Values)
                {
                    if (i.IsLoaded)
                    {
                        if (!UserDataExists(i)) { InitUserData(i); }
                        posx = i.WorldPlayerEntity.transform.position.x;
                        posy = i.WorldPlayerEntity.transform.position.y;
                        posz = i.WorldPlayerEntity.transform.position.z;
                        tempposx = (oldposx - posx);
                        tempposy = (oldposy - posy);
                        tempposz = (oldposz - posz);
                        string end_color = null;
                        if (((tempposx < radius / 16) && (tempposx > -radius / 16)) && ((tempposy < radius / 16) && (tempposy > -radius / 16)) && ((tempposz < radius / 16) && (tempposz > -radius / 16)))
                        {
                            end_color = gradient1;
                        }
                        else if (((tempposx < radius / 8) && (tempposx > -radius / 8)) && ((tempposy < radius / 8) && (tempposy > -radius / 8)) && ((tempposz < radius / 8) && (tempposz > -radius / 8)))
                        {
                            end_color = gradient2;
                        }
                        else if (((tempposx < radius / 4) && (tempposx > -radius / 4)) && ((tempposy < radius / 4) && (tempposy > -radius / 4)) && ((tempposz < radius / 4) && (tempposz > -radius / 4)))
                        {
                            end_color = gradient3;
                        }
                        else if (((tempposx < radius / 2) && (tempposx > -radius / 2)) && ((tempposy < radius / 2) && (tempposy > -radius / 2)) && ((tempposz < radius / 2) && (tempposz > -radius / 2)))
                        {
                            end_color = gradient4;
                        }
                        else if (((tempposx < radius) && (tempposx > -radius)) && ((tempposy < radius) && (tempposy > -radius)) && ((tempposz < radius) && (tempposz > -radius)))
                        {
                            end_color = gradient5;
                        }
                        if (end_color != null)
                        {
                            string return_string = null;
                            UserInfo user = null;
                            if (MCData.Users.TryGetValue(player.SteamId.ToString().ToString(), out user))
                            {
                                if (user.uLocalMuted)
                                {
                                    string parsed_config = msg("Muted", player.SteamId.ToString());
                                    parsed_config = parsed_config.Replace("{room}", Config["Public", "ChatPrefex"].ToString().ToLower());
                                    hurt.SendChatMessage(player, parsed_config);
                                    return;
                                }
                                if (Convert.ToBoolean(Config["Local", "ShowPlayerTags"]))
                                {
                                    if (user.uCanCustomTag && user.uCustomTagEnabled)
                                    {
                                        return_string = "[<color=" + Config["Local", "PrefixColor"].ToString() + ">" + Config["Local", "ChatPrefex"].ToString() + "</color>] [<color=" + Config["General", "UserTagColor"].ToString() + ">" + user.tCustomTag + "</color>]<color=" + name_color + "> " + player.Name + "</color>: <color=" + end_color + ">" + text + "</color>";
                                    }
                                    else
                                    {
                                        return_string = "[<color=" + Config["Local", "PrefixColor"].ToString() + ">" + Config["Local", "ChatPrefex"].ToString() + "</color>] <color=" + name_color + "> " + player.Name + "</color>: <color=" + end_color + ">" + text + "</color>";
                                    }
                                    user.iMessagesSent++;
                                    Puts("[" + Config["Local", "ChatPrefex"].ToString() + "] " + player.Name + ": " + text);
                                }

                            }
                            if (return_string.Length >= 1)
                            {

                                if (user.uColorEnabled && !user.uCanColor) { user.uColorEnabled = false; }
                                if (user.uCustomTagEnabled && !user.uCanCustomTag) { user.uCustomTagEnabled = false; }
                                hurt.SendChatMessage(i, return_string);
                            }
                        }
                    }
                }
            }
        }
        void OnPlayerDisconnected(PlayerSession player, string reason)
        {
            if (UserDataExists(player)) { MCData.Users[player.SteamId.ToString()].tLastName = player.Name; MCData.Users[player.SteamId.ToString()].iLastSeenTimestamp = UnixTimeStampUTC(); }
        }
        private bool InitUserData(PlayerSession player, bool debug = false)
        {
            if (!UserDataExists(player))
            {
                UserInfo z = new UserInfo();
                z.iLastSeenTimestamp = UnixTimeStampUTC();
                z.iMessagesSent = 0;
                z.iInitTimestamp = UnixTimeStampUTC();
                z.tUserId = player.SteamId.ToString().ToString();
                z.tColor = Config["UserSettings", "DefaultColor"].ToString();
                z.tCustomTag = Config["UserSettings", "DefaultTag"].ToString();
                z.uCanColor = Convert.ToBoolean(Config["UserSettings", "DefaultCanColor"]);
                z.uCanCustomTag = Convert.ToBoolean(Config["UserSettings", "DefaultCanTag"]);
                z.uLocalMuted = false;
                z.uPublicMuted = false;
                z.tLastName = player.Name;
                if (Convert.ToBoolean(Config["General", "VoipEnabled"])) { z.uVoiceMuted = false; } else { z.uVoiceMuted = true; }
                z.uColorEnabled = false;
                z.uCustomTagEnabled = false;
                z.uShowPublicChat = Convert.ToBoolean(Config["UserSettings", "DefaultPublicChat"]);
                MCData.Users.Add(z.tUserId, z);
                Puts("Adding " + z.tLastName + " to the database. [VOIP Muted: " + z.uVoiceMuted.ToString() + "]");
                return true;
            }
            else
            {
                if (debug) { Puts("DEBUG: Resetting " + player.Name + "'s MagicChat entry."); MCData.Users.Remove(player.SteamId.ToString()); InitUserData(player); }
            }
            return false;
        }
        private bool CanUserCustomColor(PlayerSession player)
        {
            if (player.IsLoaded && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.SteamId.ToString().ToString(), out item))
                {
                    return item.uCanColor;
                }
            }
            return false;
        }
        private bool UserColorToggle(PlayerSession player)
        {
            bool return_b = false;
            if (player.IsLoaded && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.SteamId.ToString().ToString(), out item))
                {
                    if (item.uColorEnabled)
                    {
                        item.uColorEnabled = false;
                    }
                    else
                    {
                        item.uColorEnabled = true;
                    }
                    return_b = item.uColorEnabled;
                }
            }
            return return_b;
        }
        private bool UserTagToggle(PlayerSession player)
        {
            bool return_b = false;
            if (player.IsLoaded && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.SteamId.ToString().ToString(), out item))
                {
                    if (item.uCustomTagEnabled)
                    {
                        item.uCustomTagEnabled = false;
                    }
                    else
                    {
                        item.uCustomTagEnabled = true;
                    }
                    return_b = item.uCustomTagEnabled;
                }
            }
            return return_b;
        }
        private bool UserTagEnabled(PlayerSession player)
        {
            if (player.IsLoaded && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.SteamId.ToString().ToString(), out item))
                {
                    return item.uCustomTagEnabled;
                }
            }
            return false;
        }
        private bool CanUserCustomTag(PlayerSession player)
        {
            if (player.IsLoaded && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.SteamId.ToString().ToString(), out item))
                {
                    return item.uCanCustomTag;
                }
            }
            return false;
        }
        private bool UserDataExists(PlayerSession player)
        {
            UserInfo item = null;
            if (MCData.Users.TryGetValue(player.SteamId.ToString(), out item))
            {
                return true;
            }
            return false;
        }
        void SaveData()
        {
            Puts("Saving database...");
            Interface.Oxide.DataFileSystem.WriteObject(this.Title, MCData);
        }
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"PublicDisabled", "Public chat is currently <color=red>disabled</color>."},
                {"DBCleared", "You have <color=green>successfully</color> cleared the " + this.Title + " database."},
                {"NotAdmin", "You are <color=red>not</color> an administrator."},
                {"AdminCmd", "<color=yellow>ADMIN: /chat <clear></color>"},
                {"PlayerInfo", "[ <color=brown>{name}</color> ] Tag: {tag} [Messages Sent: <color=yellow>{messages_sent}</color>]." },
                {"PublicVisible", "You will <color=green>now</color> see public messages." },
                {"PublicNotVisible", "You will <color=red>no-longer</color> see public messages." },
                {"ReservedColor", "That color is <color=red>reserved.</color>." },
                {"ColorUpdated", "You have <color=green>successfully</color> updated your custom user color. (<color={c1}>{c2}</color>)." },
                {"InvalidColor", "Please enter a <color=red>valid</color> color." },
                {"ColorEnabled", "You have <color=green>enabled</color> your custom user color." },
                {"ColorDisabled", "You have <color=red>disabled</color> your custom user color." },
                {"ChatColorUsage", "<color=yellow>USAGE:</color> /chat color <new>." },
                {"TagUpdated", "You have <color=green>successfully</color> updated your custom user tag. (<color={tagcolor}>{tag}</color>)." },
                {"NoPublic", "You <color=red>can't</color> use public chat when you can't even see it. (/chat public)" },
                {"TitleEnabled", "You have <color=green>enabled</color> your custom title." },
                {"NoTag", "You <color=red>don't</color> have permission to set a custom user tag." },
                {"NoPermission", "You <color=red>don't</color> have permission to do that." },
                {"ChatTagUsage", "<color=yellow>USAGE:</color> /chat tag <new>" },
                {"Muted", "You are currently <color=red>muted</color> from <color=yellow>{room}</color> chat." },
                {"TitleDisabled", "You have <color=red>disabled</color> your custom title." }
            }, this);
        }
        string msg(string key, string userID = null)
        {
            return lang.GetMessage(key, this, userID);
        }
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();

            Config["AdminColors", "1"] = "#b4da73";

            // --- [ GENERAL SETTINGS ] ---

            Config["General", "MaxTagLength"] = 15;
            Config["General", "MaxColorLength"] = 10;
            Config["General", "VoipEnabled"] = true;
            Config["General", "UserTagColor"] = "#00FFFF";
            Config["General", "PublicPrefix"] = "@";

            // --- [ NOTIFICATIONS SETTINGS ] ---

            Config["Notifications", "TimerInterval"] = 60;
            Config["Notifications", "Enabled"] = true;


            // --- [ USER SETTINGS ] ---

            Config["UserSettings", "DefaultCanTag"] = true;
            Config["UserSettings", "DefaultCanColor"] = true;
            Config["UserSettings", "DefaultColor"] = DEFAULT_COLOR;
            Config["UserSettings", "AdminColor"] = "#b4da73";
            Config["UserSettings", "DefaultTag"] = "O.G.";
            Config["UserSettings", "DefaultPublicChat"] = true;

            // --- [ LOCAL SETTINGS ] ---

            Config["Local", "Radius"] = 60.00;
            Config["Local", "FadeColors"] = true;
            Config["Local", "ChatPrefex"] = "Local";
            Config["Local", "PrefixColor"] = "#F5A9F2";
            Config["Local", "PrefixEnabled"] = true;
            Config["Local", "ShowPlayerTags"] = true;
            Config["Local", "Enabled"] = true;

            // --- [ PUBLIC SETTINGS ] ---

            Config["Public", "ChatPrefex"] = "Public";
            Config["Public", "PrefixColor"] = "#82FA58";
            Config["Public", "PrefixEnabled"] = true;
            Config["Public", "ShowPlayerTags"] = true;
            Config["Public", "Enabled"] = true;

            // --- [ FADE COLORS ] ---

            Config["FadeGradient", "1"] = "#E6E6E6";
            Config["FadeGradient", "2"] = "#C8C8C8";
            Config["FadeGradient", "3"] = "#AAAAAA";
            Config["FadeGradient", "4"] = "#8C8C8C";
            Config["FadeGradient", "5"] = "#6E6E6E";
            SaveConfig();
        }
    }
}