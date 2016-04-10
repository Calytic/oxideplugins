using System;
using System.Collections.Generic;
using Rust;
using Oxide.Core;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("MagicChat", "Norn", 0.1, ResourceId = 1437)]
    [Description("An alternative chat system.")]
    public class MagicChat : RustPlugin
    {
        string DEFAULT_COLOR = "#81DAF5";

        [PluginReference]
        Plugin PopupNotifications;

        class StoredData
        {
            public Dictionary<ulong, UserInfo> Users = new Dictionary<ulong, UserInfo>();
            public StoredData()
            {
            }
        }

        class UserInfo
        {
            public ulong iUserId; // Steam ID
            public string tLastName; // Display Name
            public int iWorld; // World (0 for now)
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
            public bool uShowIcon; // Hide/Show Icon In Chat
            public bool uIconStatus; // Hide/Show Icon In Chat
            public int iLastSeenTimestamp;
            public bool uShowPublicChat;
            public UserInfo()
            {
            }
        }

        StoredData MCData;
        private void Loaded()
        {
            MCData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
            if (!PopupNotifications && Convert.ToBoolean(Config["Dependencies", "PopupNotifications"])) { Config["Dependencies", "PopupNotifications"] = false; Puts("PopupNotifications [1252] has not been found. [Resetting to false]"); }
            int config_protocol = Convert.ToInt32(Config["General", "Protocol"]); if (Config["General", "Protocol"] == null)
            { Config["General", "Protocol"] = Protocol.network; }
            else if (Convert.ToInt32(Config["General", "Protocol"]) != Protocol.network)
            { Config["General", "Protocol"] = Protocol.network; }
        }
        void OnServerInitialized()
        {

        }
        void Unload()
        {
            SaveData();
        }
        string GetUserTag(BasePlayer player)
        {
            UserInfo p; string tag = "None";
            if (MCData.Users.TryGetValue(player.userID, out p))
            {
                if(p.tCustomTag.Length >= 1) tag = "<color="+ Config["General", "UserTagColor"].ToString()+">"+p.tCustomTag+"</color>";
            }
            return tag;
        }
        bool UserUpdateColor(BasePlayer player, string color)
        {
            UserInfo p;
            if (MCData.Users.TryGetValue(player.userID, out p))
            {
                p.tColor = color;
                return true;
            }
            return false;
        }
        bool UserUpdateTag(BasePlayer player, string tag)
        {
            UserInfo p;
            if (MCData.Users.TryGetValue(player.userID, out p))
            {
                p.tCustomTag = tag;
                return true;
            }
            return false;
        }
        [ChatCommand("chat")]
        private void ChatCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                string default_command = null;
                if (CanUserToggleSteamIcon(player))
                { default_command += "icon "; }
                if (CanUserCustomColor(player))
                { default_command += " | color "; }
                if (CanUserCustomTag(player))
                { default_command += " | tag "; }
                PrintToChat(player, "USAGE: /chat <"+default_command+ " | public>");
                if (player.net.connection.authLevel >= Convert.ToInt32(Config["Admin", "MinLevel"]))
                {
                    PrintToChat(player, "<color=yellow>ADMIN: /chat <clear></color>");
                }
                if (UserDataExists(player))
                {
                    PrintToChat(player, "[ <color=brown>"+player.displayName +"</color> ] Tag: "+ GetUserTag(player) + " [Messages Sent: <color=yellow>"+ MCData.Users[player.userID].iMessagesSent.ToString()+"</color>].");
                }
            }
            else if (args[0] == "public")
            {
               if (UserDataExists(player))
               {
                    if (MCData.Users[player.userID].uShowPublicChat) { MCData.Users[player.userID].uShowPublicChat = false; } else { MCData.Users[player.userID].uShowPublicChat = true; }
                    if (MCData.Users[player.userID].uShowPublicChat) { PrintToChat(player, "You will <color=green>now</color> see public messages."); } else { PrintToChat(player, "You will <color=red>no-longer</color> see public messages."); }
               }
            }
            else if (args[0] == "icon")
            {
                if (CanUserToggleSteamIcon(player))
                {
                    if(MCData.Users[player.userID].uShowIcon) { MCData.Users[player.userID].uShowIcon = false; } else { MCData.Users[player.userID].uShowIcon = true; }
                    if(MCData.Users[player.userID].uShowIcon){PrintToChat(player, "You will now <color=green>display</color> your steam icon.");}else{PrintToChat(player, "You have <color=red>hidden</color> your steam icon.");}
                }
                else
                {
                    PrintToChat(player, "You <color=red>don't</color> have permission to set a custom user color.");
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
                            if(args[1] == "red" && player.net.connection.authLevel != Convert.ToInt32(Config["Admin", "MaxLevel"])) { PrintToChat("That color is <color=red>reserved.</color>."); return; }
                            if (UserUpdateColor(player, args[1]))
                            {
                                PrintToChat(player, "You have <color=green>successfully</color> updated your custom user color. (<color=" + args[1].ToString() + ">" + args[1].ToString() + "</color>).");
                            }
                        }
                        else
                        {
                            PrintToChat(player, "Please enter a <color=red>valid</color> color.");
                        }

                    }
                    else
                    {
                        if (UserColorToggle(player)) { PrintToChat(player, "You have <color=green>enabled</color> your custom user color."); } else { PrintToChat(player, "You have <color=red>disabled</color> your custom user color."); }
                        PrintToChat(player, "<color=yellow>USAGE:</color> /chat color <new>.");
                    }

                }
                else
                {
                    PrintToChat(player, "You <color=red>don't</color> have permission to set a custom user color.");
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
                                PrintToChat(player, "You have <color=green>successfully</color> updated your custom user tag. (<color="+ Config["General", "UserTagColor"].ToString()+">" + args[1].ToString() + "</color>).");
                            }
                        }
                        else
                        {
                            PrintToChat(player, "Please enter a <color=red>valid</color> tag.");
                        }

                    }
                    else
                    {
                        if(UserTagToggle(player)) { PrintToChat(player, "You have <color=green>enabled</color> your custom title."); } else { PrintToChat(player, "You have <color=red>disabled</color> your custom title."); }
                        PrintToChat(player, "<color=yellow>USAGE:</color> /chat tag <new>");
                    }

                }
                else
                {
                    PrintToChat(player, "You <color=red>don't</color> have permission to set a custom user tag.");
                }
            }
            else if (args[0] == "clear")
            {
                if (player.net.connection.authLevel >= Convert.ToInt32(Config["Admin", "MaxLevel"]))
                {
                    MCData.Users.Clear();
                    SaveData();
                    PrintToChat(player, Config["Messages", "DBCleared"].ToString());
                }
                else
                {
                    PrintToChat(player, Config["Messages", "AuthLevel"].ToString());
                }
            }
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            //InitUserData(player, true);
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (!UserDataExists(player)) InitUserData(player);
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
        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            string text = arg.Args[0];
            if (text.StartsWith(Config["General", "PublicPrefix"].ToString()))
            {
                if (Convert.ToBoolean(Config["Public", "Enabled"]))
                {
                    if (MCData.Users[arg.connection.userid].uShowPublicChat)
                    {
                        BasePlayer player = BasePlayer.FindByID(arg.connection.userid);
                        string final_text = text.Remove(0, Config["General", "PublicPrefix"].ToString().Length);
                        if (final_text.Length >= 1 && player != null) UserTextPublic(player, final_text);
                    }
                    else
                    {
                        PrintToChat(BasePlayer.FindByID(arg.connection.userid), "You <color=red>can't</color> use public chat when you can't even see it. (/chat public)");
                    }
                }
                else
                {
                    PrintToChat(BasePlayer.FindByID(arg.connection.userid), Config["Messages", "PublicDisabled"].ToString());
                }
            }
            else
            {
                if (Convert.ToBoolean(Config["Local", "Enabled"]))
                {
                    BasePlayer player = BasePlayer.FindByID(arg.connection.userid);
                    if(text.Length >= 1 && player != null) UserTextRadius(player, Convert.ToDouble(Config["Local", "Radius"]), text, UserNameColor(player));
                }
                else return null;
            }
            return false;
        }
        private void UserTextPublic(BasePlayer player, string text)
        {
            string end_result = null;
            if (player != null && player.IsConnected())
            {
                if (!UserDataExists(player)) { InitUserData(player); }
                if (Convert.ToBoolean(Config["Public", "Enabled"]))
                {
                    UserInfo user = null;
                    if (MCData.Users.TryGetValue(player.userID, out user))
                    {
                        if(user.uPublicMuted)
                        {
                            PrintToChat(player, "You are currently <color=red>muted</color> from <color=yellow>" + Config["Public", "ChatPrefex"].ToString().ToLower() + "</color> chat.");
                            return;
                        }
                        if(user.uCanCustomTag && user.uCustomTagEnabled && user.tCustomTag.Length >= 1)
                        {
                            end_result = "[<color=" + Config["Public", "PrefixColor"].ToString() + ">" + Config["Public", "ChatPrefex"].ToString() + "</color>] [<color=" + Config["General", "UserTagColor"].ToString() + ">" + user.tCustomTag + "</color>] <color=" + UserNameColor(player) + ">" + player.displayName + "</color>: " + text;
                        }
                        else
                        {
                            end_result = "[<color=" + Config["Public", "PrefixColor"].ToString() + ">" + Config["Public", "ChatPrefex"].ToString() + "</color>] <color=" + UserNameColor(player) + ">" + player.displayName + "</color>: " + text;
                        }
                        user.iMessagesSent++;
                        Puts("[" + Config["Public", "ChatPrefex"].ToString() + "] " + player.displayName + ": " + text);
                        foreach (BasePlayer target in BasePlayer.activePlayerList)
                        {
                            if(target != null && target.IsConnected())
                            {
                                if (!UserDataExists(target)) InitUserData(target);
                                if (MCData.Users[target.userID].uShowPublicChat) { if (user.uShowIcon) { rust.SendChatMessage(target, end_result, null, player.userID.ToString()); } else { rust.SendChatMessage(target, end_result, null, Config["General", "IconDisabled"].ToString()); } }
                            }
                        } 
                    }
                }
            }
        }
        private string UserNameColor(BasePlayer i)
        {
            string color = DEFAULT_COLOR;
            if (i != null && i.IsConnected())
            {
                UserInfo user = null;
                if (MCData.Users.TryGetValue(i.userID, out user))
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
                        else
                        {
                            if (i.net.connection.authLevel >= Convert.ToInt32(Config["Admin", "MinLevel"]))
                            {
                                if (Config["AdminColors", i.net.connection.authLevel.ToString()] != null)
                                { color = Config["AdminColors", i.net.connection.authLevel.ToString()].ToString(); }
                                else
                                {
                                    if (Config["AdminColors", "1"] == null)
                                    { Config["AdminColors", "1"] = DEFAULT_COLOR; color = Config["AdminColors", "1"].ToString(); }
                                    else
                                    { color = Config["AdminColors", "1"].ToString(); }
                                }
                            }
                        }
                    }
                }
            }
            return color;
        }
        private void UserTextRadius(BasePlayer player, double radius, string text, string name_color = "")
        {
            if (!UserDataExists(player))
            {
                InitUserData(player);
            }
            if (player.IsConnected())
            {
                float posx;
                float posy;
                float posz;
                float oldposx = 0.0f, oldposy = 0.0f, oldposz = 0.0f, tempposx = 0.0f, tempposy = 0.0f, tempposz = 0.0f;
                oldposx = player.transform.position.x;
                oldposy = player.transform.position.y;
                oldposz = player.transform.position.z;
                string gradient1 = Config["FadeGradient", "1"].ToString();
                string gradient2 = Config["FadeGradient", "2"].ToString();
                string gradient3 = Config["FadeGradient", "3"].ToString();
                string gradient4 = Config["FadeGradient", "4"].ToString();
                string gradient5 = Config["FadeGradient", "5"].ToString();
                foreach (BasePlayer i in BasePlayer.activePlayerList)
                {
                    if (i.IsConnected())
                    {
                        posx = i.transform.position.x;
                        posy = i.transform.position.y;
                        posz = i.transform.position.z;
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
                            if (MCData.Users.TryGetValue(player.userID, out user))
                            {
                                if (user.uLocalMuted)
                                {
                                    PrintToChat(player, "You are currently <color=red>muted</color> from <color=yellow>" + Config["Local", "ChatPrefex"].ToString().ToLower() + "</color> chat.");
                                    return;
                                }
                                if (Convert.ToBoolean(Config["Local", "ShowPlayerTags"]))
                                {
                                    if (user.uCanCustomTag && user.uCustomTagEnabled)
                                    {
                                        return_string = "[<color=" + Config["Local", "PrefixColor"].ToString() + ">" + Config["Local", "ChatPrefex"].ToString() + "</color>] [<color=" + Config["General", "UserTagColor"].ToString() + ">" + user.tCustomTag + "</color>]<color=" + name_color + "> " + player.displayName + "</color>: <color=" + end_color + ">" + text + "</color>";
                                    }
                                    else
                                    {
                                        return_string = "[<color=" + Config["Local", "PrefixColor"].ToString() + ">" + Config["Local", "ChatPrefex"].ToString() + "</color>] <color=" + name_color + "> " + player.displayName + "</color>: <color=" + end_color + ">" + text + "</color>";
                                    }
                                    user.iMessagesSent++;
                                    Puts("[" + Config["Local", "ChatPrefex"].ToString() + "] " + player.displayName + ": " + text);
                                    //Puts(i.displayName + " has sent " + user.iMessagesSent.ToString() + " messages.");
                                }

                            }
                            if (return_string.Length >= 1)
                            {

                                if (!user.uShowIcon && !user.uIconStatus) { user.uShowIcon = true; }
                                if (user.uColorEnabled && !user.uCanColor) { user.uColorEnabled = false; }
                                if (user.uCustomTagEnabled && !user.uCanCustomTag) { user.uCustomTagEnabled = false; }
                                if (user.uShowIcon)
                                {
                                    rust.SendChatMessage(i, return_string, null, player.userID.ToString());
                                }
                                else
                                {
                                    if (user.uIconStatus) rust.SendChatMessage(i, return_string, null, Config["General", "IconDisabled"].ToString());
                                }
                            }
                        }
                    }
                }
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
           if(UserDataExists(player)) { MCData.Users[player.userID].tLastName = player.displayName; MCData.Users[player.userID].iLastSeenTimestamp = UnixTimeStampUTC(); }
        }
        private bool InitUserData(BasePlayer player, bool debug = false)
        {
            if (!UserDataExists(player))
            {
                UserInfo z = new UserInfo();
                z.iLastSeenTimestamp = UnixTimeStampUTC();
                z.iMessagesSent = 0;
                z.iInitTimestamp = UnixTimeStampUTC();
                z.iUserId = player.userID;
                z.iWorld = Convert.ToInt32(Config["UserSettings", "DefaultWorld"]);
                z.tColor = Config["UserSettings", "DefaultColor"].ToString();
                z.tCustomTag = Config["UserSettings", "DefaultTag"].ToString();
                z.uCanColor = Convert.ToBoolean(Config["UserSettings", "DefaultCanColor"]);
                z.uCanCustomTag = Convert.ToBoolean(Config["UserSettings", "DefaultCanTag"]);
                z.uLocalMuted = false;
                z.uPublicMuted = false;
                z.tLastName = player.displayName;
                if(Convert.ToBoolean(Config["General", "VoipEnabled"])) { z.uVoiceMuted = false; } else { z.uVoiceMuted = true; }
                z.uShowIcon = Convert.ToBoolean(Config["General", "ShowUserIcons"]);
                z.uColorEnabled = false;
                z.uIconStatus = Convert.ToBoolean(Config["UserSettings", "AllowIconHide"]);
                z.uCustomTagEnabled = false;
                z.uShowPublicChat = Convert.ToBoolean(Config["UserSettings", "DefaultPublicChat"]);
                MCData.Users.Add(z.iUserId, z);
                Puts("Adding " + z.tLastName + " to the database. [VOIP Muted: " + z.uVoiceMuted.ToString() + "]");
                return true;
            }
            else
            {
                if(debug) { Puts("DEBUG: Resetting " + player.displayName + "'s MagicChat entry."); MCData.Users.Remove(player.userID); InitUserData(player); }
            }
            return false;
        }
        private bool CanUserToggleSteamIcon(BasePlayer player)
        {
            if (player.isConnected && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.userID, out item))
                {
                    return item.uIconStatus;
                }
            }
            return false;
        }
        private bool CanUserCustomColor(BasePlayer player)
        {
            if (player.isConnected && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.userID, out item))
                {
                    return item.uCanColor;
                }
            }
            return false;
        }
        private bool UserColorToggle(BasePlayer player)
        {
            bool return_b = false;
            if (player.isConnected && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.userID, out item))
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
        private bool UserTagToggle(BasePlayer player)
        {
            bool return_b = false;
            if (player.isConnected && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.userID, out item))
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
        private bool UserTagEnabled(BasePlayer player)
        {
            if (player.isConnected && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.userID, out item))
                {
                    return item.uCustomTagEnabled;
                }
            }
            return false;
        }
        private bool CanUserCustomTag(BasePlayer player)
        {
            if (player.isConnected && player != null)
            {
                UserInfo item = null;
                if (MCData.Users.TryGetValue(player.userID, out item))
                {
                    return item.uCanCustomTag;
                }
            }
            return false;
        }
        private bool UserDataExists(BasePlayer player)
        {
            UserInfo item = null;
            if (MCData.Users.TryGetValue(player.userID, out item))
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
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ ADMIN ] ---

            Config["Admin", "MinLevel"] = 1;
            Config["Admin", "MaxLevel"] = 2;

            Config["AdminColors", "1"] = "#b4da73";
            
            // --- [ GENERAL SETTINGS ] ---

            Config["General", "Protocol"] = Protocol.network;
            Config["General", "MaxTagLength"] = 15;
            Config["General", "MaxColorLength"] = 10;
            Config["General", "VoipEnabled"] = true;
            Config["General", "ShowUserIcons"] = true;
            Config["General", "UserTagColor"] = "#00FFFF";
            Config["General", "PublicPrefix"] = "@";
            Config["General", "IconDisabled"] = "76561197967728661";

            // --- [ NOTIFICATIONS SETTINGS ] ---

            Config["Notifications", "TimerInterval"] = 60;
            Config["Notifications", "Enabled"] = true;

            // --- [ DEPENDENCIES ] ---

            Config["Dependencies", "PopupNotifications"] = false;
 

            // --- [ USER SETTINGS ] ---

            Config["UserSettings", "DefaultCanTag"] = true;
            Config["UserSettings", "DefaultCanColor"] = true;
            Config["UserSettings", "DefaultWorld"] = 0;
            Config["UserSettings", "DefaultColor"] = DEFAULT_COLOR;
            Config["UserSettings", "AdminColor"] = "#b4da73";
            Config["UserSettings", "DefaultTag"] = "O.G.";
            Config["UserSettings", "AllowIconHide"] = false;
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

            // --- [ MESSAGES ] ---

            Config["Messages", "PublicDisabled"] = "Public chat is currently <color=red>disabled</color>.";
            Config["Messages", "DBCleared"] = "You have <color=green>successfully</color> cleared the " + this.Title + " database.";
            Config["Messages", "AuthLevel"] = "You <color=red>don't</color> have the required auth level.";
            SaveConfig();
        }
    }
}