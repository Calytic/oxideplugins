using System;
using System.Collections.Generic;
using System.Linq;
using Rust;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Text.RegularExpressions;
namespace Oxide.Plugins
{
    [Info("MailingList", "Norn", 0.2, ResourceId = 1388)]
    [Description("Get virtual rewards for giving a complete stranger your personal email address.")]
    public class MailingList : RustPlugin
    {
        [PluginReference]
        Plugin PopupNotifications;
        class StoredData
        {
            public Dictionary<ulong, EmailData> EmailData = new Dictionary<ulong, EmailData>();
            public StoredData()
            {
            }
        }

        class EmailData
        {
            public string tEmailAddress;
            public ulong uUserId;
            public string tRegistrationName;
            public int iDateSet;
            public bool bSet;
            public EmailData()
            {
            }
        }
        Dictionary<string, int> RewardItems = new Dictionary<string, int>()
        {
            {"sign.hanging.banner.large", 1},
            {"sign.hanging.ornate", 1},
            {"sign.pictureframe.landscape", 1},
            {"sign.pictureframe.portrait", 1},
            {"sign.pictureframe.tall", 1},
            {"sign.pictureframe.xl", 1},
            {"sign.pictureframe.xxl", 1},
            {"sign.pole.banner.large", 1},
            {"sign.post.double", 1},
            {"sign.post.single", 1},
            {"sign.post.town", 1},
            {"sign.post.town.roof", 1},
            {"sign.wooden.huge", 2},
            {"sign.wooden.large", 2},
            {"sign.wooden.medium", 3},
            {"sign.wooden.small", 4},
        };
        StoredData storedData;
        private void Loaded()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
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
        void Unload()
        {
            SaveData();
        }
        private string GiveItem(BasePlayer player, string shortname, int amount, ItemContainer pref)
        {
            shortname = shortname.ToLower();
            bool isBP = false;
            string end_result = "null";
            if (shortname.EndsWith(" bp"))
            {
                isBP = true;
                shortname = shortname.Substring(0, shortname.Length - 3);
            }
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition == null)
                return end_result;
            int stack = definition.stackable;
            if (isBP)
                stack = 1;
            if (stack < 1) stack = 1;
            for (var i = amount; i > 0; i = i - stack)
            {
                var giveamount = i >= stack ? stack : i;
                if (giveamount < 1) return end_result;
                player.inventory.GiveItem(ItemManager.CreateByItemID(definition.itemid, giveamount, isBP), pref);
                end_result = definition.displayName.english;
            }
            return end_result;
        }
		System.Random rnd = new System.Random();
        protected int GetRandomInt(int min, int max)
        {
            return rnd.Next(min, max);
        }

        bool RewardPlayer(BasePlayer player)
        {
            if (Convert.ToBoolean(Config["bRewardPlayer"]))
            {
                int max_items = Convert.ToInt32(Config["iMaxRewardItems"]);
                for (int i = 1; i <= max_items; i++)
                {
                    System.Random rand = new System.Random();
                    int size = RewardItems.Count;
                    int randNum = rand.Next(0, size);
                    KeyValuePair<string, int> item = RewardItems.ElementAt(randNum);
                    string display_name = GiveItem(player, item.Key, item.Value, player.inventory.containerMain);
                    if (display_name.Length >= 1 && display_name != "null")
                    {
                        PrintToChatEx(player, "You have been rewarded x" + item.Value + " " + display_name + ".");
                    }
                    else
                    {
                        PrintToChatEx(player, "You have been rewarded x" + item.Value + " " + item.Key + ".");
                    }
                }
                return true;
            }
            return false;
        }
        void OnServerInitialized()
        {
            if (Config["iMaxRewardItems"] == null) { Puts("Resetting configuration file (out of date)..."); LoadDefaultConfig(); }
        }
        bool IsEmailSet(ulong steamid)
        {
            EmailData p = null;
            if (storedData.EmailData.TryGetValue(steamid, out p))
            {
                return true;
            }
            return false;
        }
        string ReturnEmail(BasePlayer player)
        {
            string email = "null";
            EmailData p = null;
            if (storedData.EmailData.TryGetValue(player.userID, out p))
            {
                email = p.tEmailAddress;
            }
            return email;
        }
        bool RegisterEmail(BasePlayer player, string email_address)
        {
            EmailData p;
            if (storedData.EmailData.TryGetValue(player.userID, out p))
            {
                storedData.EmailData.Remove(player.userID);
                RegisterEmail(player, email_address);
            }
            else
            {
                EmailData eData = new EmailData();
                eData.uUserId = player.userID;
                eData.tRegistrationName = player.displayName;
                eData.tEmailAddress = email_address;
                eData.iDateSet = UnixTimeStampUTC();
                eData.bSet = true;
                storedData.EmailData.Add(eData.uUserId, eData);
                Interface.GetMod().DataFileSystem.WriteObject(this.Title, storedData);
                Puts("(" + eData.tRegistrationName + ") " + eData.tEmailAddress + " has been added to the mailing list database.");
                return true;
            }
            return false;
        }
        bool UpdateEmail(BasePlayer player, string email_address)
        {
            EmailData p;
            if (storedData.EmailData.TryGetValue(player.userID, out p))
            {
                storedData.EmailData.Remove(player.userID);
                return RegisterEmail(player, email_address);
            }
            return false;
        }
        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!IsEmailSet(player.userID)) { PrintToChatEx(player, "Enter your email and receive updates, items and more! /mail set (Your e-mail address)."); }
        }
        public static bool IsValidEmail(string email)
        {
            string reg = "\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*";
            if (Regex.IsMatch(email, reg))
            {
                if (Regex.Replace(email, reg, string.Empty).Length == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        [ChatCommand("mail")]
        private void MailCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                PrintToChatEx(player, "USAGE: /mail <set | update>");
                if (player.net.connection.authLevel >= Convert.ToInt32(Config["iAuthLevel"]))
                {
                    PrintToChatEx(player, "<color=yellow>ADMIN: /mail <dump | cleardb></color>");
                }
                if (IsEmailSet(player.userID))
                {
                    PrintToChatEx(player, "Your current email address: <color=yellow>" + ReturnEmail(player) + "</color>.");
                }
            }
            else if (args[0] == "update")
            {
                if (IsEmailSet(player.userID))
                {
                    if (args.Length == 2)
                    {
                        if (IsValidEmail(args[1]))
                        {
                            if (UpdateEmail(player, args[1]))
                            {
                                PrintToChatEx(player, "You have <color=green>successfully</color> updated your email address. (" + args[1].ToString() + ").");
                            }
                        }
                        else
                        {
                            PrintToChatEx(player, "<color=red> Please enter a valid email address.</color>");
                        }

                    }
                    else
                    {
                        PrintToChatEx(player, "USAGE: /mail update <email address>");
                    }

                }
                else
                {
                    PrintToChatEx(player, "You have already set your email address, use /mail update <Your e-mail address> instead.");
                }
            }
            else if (args[0] == "set")
            {
                if (!IsEmailSet(player.userID))
                {
                    if (args.Length == 2)
                    {
                        if (IsValidEmail(args[1]))
                        {
                            if (RegisterEmail(player, args[1]))
                            {
                                PrintToChatEx(player, "You have <color=green>successfully</color> added " + args[1].ToString() + " to our mailing list.");
                                RewardPlayer(player);
                            }
                        }
                        else
                        {
                            PrintToChatEx(player, "<color=red>Please enter a valid email address.</color>");
                        }
                    }
                    else
                    {
                        PrintToChatEx(player, "USAGE: /mail set <email address>");
                    }

                }
                else
                {
                    PrintToChatEx(player, "You have already set your email address, use /mail update <e-mail address> instead.");
                }
            }
            else if (args[0] == "dump")
            {
                if (player.net.connection.authLevel >= Convert.ToInt32(Config["iAuthLevel"]))
                {
                    EmailData item = null;
                    int count = 0;
                    Dictionary<string, string> result = new Dictionary<string, string>();
                    new List<ulong>(storedData.EmailData.Keys).ForEach(u =>
                    {
                        if (storedData.EmailData.TryGetValue(u, out item))
                        {
                            string key = item.tEmailAddress.ToString();
                            count++;
                            result.Add(count.ToString(), key);
                        }
                    });
                    if (count != 0)
                    {
                        string file = this.Title + "_" + UnixTimeStampUTC().ToString();
                        Interface.GetMod().DataFileSystem.WriteObject(file, result);
                        Puts("Dumped " + count.ToString() + " email address(es) to " + file + ".json");
                        PrintToChatEx(player, "<color=yellow>Dumped " + count.ToString() + " email address(es) to " + file + ".json</color>");
                    }
                    else
                    {
                        PrintToChatEx(player, "<color=red>No email addresses have been set.</color>");
                    }
                }
                else
                {
                    PrintToChatEx(player, Config["tNoAuthLevel"].ToString());
                }
            }
            else if (args[0] == "cleardb")
            {
                if (player.net.connection.authLevel >= Convert.ToInt32(Config["iAuthLevel"]))
                {
                    storedData.EmailData.Clear();
                    SaveData();
                    PrintToChatEx(player, Config["tDBCleared"].ToString());
                }
                else
                {
                    PrintToChatEx(player, Config["tNoAuthLevel"].ToString());
                }
            }
        }
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(this.Title, storedData);
        }
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ GENERAL ] ---

            Config["iProtocol"] = Protocol.network;
            Config["bUsePopupNotifications"] = false;
            Config["bShowPluginName"] = true;
            Config["iAuthLevel"] = 2;

            // --- [ MESSAGES ] ---

            Config["tNotification"] = "Enter your email and receive updates, items and more! /mail set <Your e-mail address>.";
            Config["tDBCleared"] = "You have <color=#FF3300>cleared</color> the Mailing List Rewards database.";
            Config["tNoAuthLevel"] = "You <color=#FF3300>do not</color> have access to this command.";

            // --- [ OTHER ] ----
            Config["iMaxRewardItems"] = 1;
            Config["bRewardPlayer"] = true;
            Config["dRewardItems"] = RewardItems;
            SaveConfig();
        }
        private void PrintToChatEx(BasePlayer player, string result, string tcolour = "#66FF66")
        {
            if (!Convert.ToBoolean(Config["bUsePopupNotifications"]))
            {
                if (Convert.ToBoolean(Config["bShowPluginName"]))
                {
                    PrintToChat(player, "<color=\"" + tcolour + "\">[" + this.Title.ToString() + "]</color> " + result);
                }
                else
                {
                    PrintToChat(player, result);
                }
            }
            else
            {
                if(PopupNotifications)
                {
                    if (Convert.ToBoolean(Config["bShowPluginName"]))
                    {
                        PopupNotifications?.Call("CreatePopupNotification", "<color=" + tcolour + ">" + this.Title.ToString() + "</color>\n" + result, player);
                    }
                    else
                    {
                        PopupNotifications?.Call("CreatePopupNotification", result, player);
                    }
                }
                
            }
        }
    }
}