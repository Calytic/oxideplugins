using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;
namespace Oxide.Plugins
{
    [Info("AdminList", "BDM", "1.3.0")]
    [Description("Lists all the server admins/staff.")]
	public class AdminList : RustLegacyPlugin
	{
        public static bool hasOwner = true;
        public static bool hasCoowner = true;
        public static bool hasHeadadmin = true;
        public static string serverOwner = "Owner has yet to input this!";
        public static string serverCoOwner = "Owner has yet to input this!";
        public static string headAdmin = "Owner has yet to input this!";
        public static string admin1 = "";
        public static string admin2 = "";
        public static string admin3 = "";
        public static string admin4 = "";
        public static string admin5 = "";
        public static string admin6 = "";
        public static string admin7 = "";
        public static string admin8 = "";
        public static string admin9 = "";
        public static string admin10 = "";
        public static bool admincheck1 = false;
        public static bool admincheck2 = false;
        public static bool admincheck3 = false;
        public static bool admincheck4 = false;
        public static bool admincheck5 = false;
        public static bool admincheck6 = false;
        public static bool admincheck7 = false;
        public static bool admincheck8 = false;
        public static bool admincheck9 = false;
        public static bool admincheck10 = false;
        public static string mod1 = "";
        public static string mod2 = "";
        public static string mod3 = "";
        public static string mod4 = "";
        public static string mod5 = "";
        public static string mod6 = "";
        public static string mod7 = "";
        public static string mod8 = "";
        public static string mod9 = "";
        public static string mod10 = "";
        public static int numberofadmins = 0;
        public static int numberofmods = 0;
        void LoadDefaultConfig() { }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        void Init()
        {
            CheckCfg<bool>("Settings: Server has an admin", ref hasOwner);
            CheckCfg<bool>("Settings: Server has a coowner", ref hasCoowner);
            CheckCfg<bool>("Settings: Server has a head admin", ref hasHeadadmin);
            CheckCfg<int>("Settings: Number of Admins", ref numberofadmins);
            CheckCfg<int>("Settings: Number of Moderators", ref numberofmods);
            CheckCfg<string>("Owner: The owner of the server", ref serverOwner);
            CheckCfg<string>("CoOwner: The Coowner of the server", ref serverCoOwner);
            CheckCfg<string>("Head Admin: The Head Admin of the server", ref headAdmin);
            CheckCfg<string>("Admin1: An Admin of the server", ref admin1);
            CheckCfg<string>("Admin2: An Admin of the server", ref admin2);
            CheckCfg<string>("Admin3: An Admin of the server", ref admin3);
            CheckCfg<string>("Admin4: An Admin of the server", ref admin4);
            CheckCfg<string>("Admin5: An Admin of the server", ref admin5);
            CheckCfg<string>("Admin6: An Admin of the server", ref admin6);
            CheckCfg<string>("Admin7: An Admin of the server", ref admin7);
            CheckCfg<string>("Admin8: An Admin of the server", ref admin8);
            CheckCfg<string>("Admin9: An Admin of the server", ref admin9);
            CheckCfg<string>("Admin10: An Admin of the server", ref admin10);
            CheckCfg<bool>("Admin1: Used", ref admincheck1);
            CheckCfg<bool>("Admin2: Used", ref admincheck2);
            CheckCfg<bool>("Admin3: Used", ref admincheck3);
            CheckCfg<bool>("Admin4: Used", ref admincheck4);
            CheckCfg<bool>("Admin5: Used", ref admincheck5);
            CheckCfg<bool>("Admin6: Used", ref admincheck6);
            CheckCfg<bool>("Admin7: Used", ref admincheck7);
            CheckCfg<bool>("Admin8: Used", ref admincheck8);
            CheckCfg<bool>("Admin9: Used", ref admincheck9);
            CheckCfg<bool>("Admin10: Used", ref admincheck10);
            CheckCfg<string>("Mod1: A Mod of the server", ref mod1);
            CheckCfg<string>("Mod2: A Mod of the server", ref mod2);
            CheckCfg<string>("Mod3: A Mod of the server", ref mod3);
            CheckCfg<string>("Mod4: A Mod of the server", ref mod4);
            CheckCfg<string>("Mod5: A Mod of the server", ref mod5);
            CheckCfg<string>("Mod6: A Mod of the server", ref mod6);
            CheckCfg<string>("Mod7: A Mod of the server", ref mod7);
            CheckCfg<string>("Mod8: A Mod of the server", ref mod8);
            CheckCfg<string>("Mod9: A Mod of the server", ref mod9);
            CheckCfg<string>("Mod10: A Mod of the server", ref mod10);
            SaveConfig();
        }
        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"Loaded", "AdminList by BDM (v 1.0.0) has loaded, enjoy and please support future development!"},
                {"NoPerm", "You dont have permission to use the command: [color orange]/setadmin"},
                {"Prefix", "AdminList"},
                {"SyntaxSetAdmin", "Use /setadmin 'username'"},
                {"KnownStaff", "The following is the currently listed staff:"},
                {"Owner", "Server Owner: [color cyan]"},
                {"Coowner", "Server Co-Owner: [color green]"},
                {"HeadAdmin", "Server Head Admin: [color red]"},
                {"Admin", "Server Admin: [color orange]"},
                {"Mod", "Server Mod: [color yellow]"},
            };
            lang.RegisterMessages(messages, this);
        }
        void Loaded()
        {
            permission.RegisterPermission("adminlist.allowed", this);
            LoadDefaultMessages();
            Puts(GetMessage("Loaded"));
        }
        [ChatCommand("admins")]
        void cmdAdmins(NetUser netuser, string command)
        {
            bool admincheck1 = Convert.ToBoolean(Config["Admin1: Used"]);
            bool admincheck2 = Convert.ToBoolean(Config["Admin2: Used"]);
            bool admincheck3 = Convert.ToBoolean(Config["Admin3: Used"]);
            bool admincheck4 = Convert.ToBoolean(Config["Admin4: Used"]);
            bool admincheck5 = Convert.ToBoolean(Config["Admin5: Used"]);
            bool admincheck6 = Convert.ToBoolean(Config["Admin6: Used"]);
            bool admincheck7 = Convert.ToBoolean(Config["Admin7: Used"]);
            bool admincheck8 = Convert.ToBoolean(Config["Admin8: Used"]);
            bool admincheck9 = Convert.ToBoolean(Config["Admin9: Used"]);
            bool admincheck10 = Convert.ToBoolean(Config["Admin10: Used"]);
            if (!admincheck1)
            {
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("KnownStaff", netuser.userID.ToString()));
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Owner", netuser.userID.ToString()) + serverOwner.ToString());
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Coowner", netuser.userID.ToString()) + serverCoOwner.ToString());
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("HeadAdmin", netuser.userID.ToString()) + headAdmin.ToString());
                return;
            }
            else
            {
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("KnownStaff", netuser.userID.ToString()));
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Owner", netuser.userID.ToString()) + serverOwner.ToString());
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Coowner", netuser.userID.ToString()) + serverCoOwner.ToString());
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("HeadAdmin", netuser.userID.ToString()) + headAdmin.ToString());
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Admin", netuser.userID.ToString()) + admin1.ToString());
                if (!admincheck2)
                {
                    return;
                }
                else
                {
                    rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Admin", netuser.userID.ToString()) + admin2.ToString());
                    if (!admincheck3)
                    {
                        return;
                    }
                    else
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Admin", netuser.userID.ToString()) + admin3.ToString());
                        if (!admincheck4)
                        {
                            return;
                        }
                        else
                        {
                            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Admin", netuser.userID.ToString()) + admin4.ToString());
                            if (!admincheck5)
                            {
                                return;
                            }
                            else
                            {
                                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Admin", netuser.userID.ToString()) + admin5.ToString());
                                if (!admincheck6)
                                {
                                    return;
                                }
                                else
                                {
                                    rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Admin", netuser.userID.ToString()) + admin6.ToString());
                                    if (!admincheck7)
                                    {
                                        return;
                                    }
                                    else
                                    {
                                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Admin", netuser.userID.ToString()) + admin7.ToString());
                                        if (!admincheck8)
                                        {
                                            return;
                                        }
                                        else
                                        {
                                            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Admin", netuser.userID.ToString()) + admin8.ToString());
                                            if (!admincheck9)
                                            {
                                                return;
                                            }
                                            else
                                            {
                                                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Admin", netuser.userID.ToString()) + admin9.ToString());
                                                if (admincheck10)
                                                {
                                                    return;
                                                }
                                                else
                                                {
                                                    rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Admin", netuser.userID.ToString()) + admin10.ToString());
                                                    return;
                                                }
                                                return;
                                            }
                                            return;
                                        }
                                        return;
                                    }
                                    return;
                                }
                                return;
                            }
                            return;
                        }
                        return;
                    }
                    return;
                }
                return;
            }
            //rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Mod", netuser.userID.ToString()) + mod1.ToString());
            //rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Mod", netuser.userID.ToString()) + mod2.ToString());
            //rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Mod", netuser.userID.ToString()) + mod3.ToString());
            //rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Mod", netuser.userID.ToString()) + mod4.ToString());
           // rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Mod", netuser.userID.ToString()) + mod5.ToString());
           // rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Mod", netuser.userID.ToString()) + mod6.ToString());
            //rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Mod", netuser.userID.ToString()) + mod7.ToString());
           // rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Mod", netuser.userID.ToString()) + mod8.ToString());
           // rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Mod", netuser.userID.ToString()) + mod9.ToString());
           // rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("Mod", netuser.userID.ToString()) + mod10.ToString());
            return;
        }
        [ChatCommand("setadmin")]
        void cmdsetadmin(NetUser netuser, string command, string[] args)
        {
            bool admincheck1 = Convert.ToBoolean(Config["Admin1: Used"]);
            bool admincheck2 = Convert.ToBoolean(Config["Admin2: Used"]);
            bool admincheck3 = Convert.ToBoolean(Config["Admin3: Used"]);
            bool admincheck4 = Convert.ToBoolean(Config["Admin4: Used"]);
            bool admincheck5 = Convert.ToBoolean(Config["Admin5: Used"]);
            bool admincheck6 = Convert.ToBoolean(Config["Admin6: Used"]);
            bool admincheck7 = Convert.ToBoolean(Config["Admin7: Used"]);
            bool admincheck8 = Convert.ToBoolean(Config["Admin8: Used"]);
            bool admincheck9 = Convert.ToBoolean(Config["Admin9: Used"]);
            bool admincheck10 = Convert.ToBoolean(Config["Admin10: Used"]);
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "adminlist.allowed"))
            {
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("NoPerm", netuser.userID.ToString()));
                return;
            }
            else if (args.Length != 1)
            {
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("SyntaxSetAdmin", netuser.userID.ToString()));
                return;
            }
            else
            {
                if (!admincheck1)
                {
                    admin1 = args[0];
                    admincheck1 = true;
                    Config["Admin1: An Admin of the server"] = args[0];
                    Config["Admin1: Used"] = true;
                }
                else
                {
                    if (!admincheck2)
                    {
                        admin2 = args[0];
                        admincheck2 = true;
                        Config["Admin2: An Admin of the server"] = args[0];
                        Config["Admin2: Used"] = true;
                    }
                    else
                    {
                        if (!admincheck3)
                        {
                            admin3 = args[0];
                            admincheck3 = true;
                            Config["Admin3: An Admin of the server"] = args[0];
                            Config["Admin3: Used"] = true;
                        }
                        else
                        {
                            if (!admincheck4)
                            {
                                admin4 = args[0];
                                admincheck4 = true;
                                Config["Admin4: An Admin of the server"] = args[0];
                                Config["Admin4: Used"] = true;
                            }
                            else
                            {
                                if (!admincheck5)
                                {
                                    admin5 = args[0];
                                    admincheck5 = true;
                                    Config["Admin5: An Admin of the server"] = args[0];
                                    Config["Admin5: Used"] = true;
                                }
                                else
                                {
                                    if (!admincheck6)
                                    {
                                        admin6 = args[0];
                                        admincheck6 = true;
                                        Config["Admin6 An Admin of the server"] = args[0];
                                        Config["Admin6 Used"] = true;
                                    }
                                    else
                                    {
                                        if (!admincheck7)
                                        {
                                            admin7 = args[0];
                                            admincheck7 = true;
                                            Config["Admin7: An Admin of the server"] = args[0];
                                            Config["Admin7: Used"] = true;
                                        }
                                        else
                                        {
                                            if (!admincheck8)
                                            {
                                                admin8 = args[0];
                                                admincheck8 = true;
                                                Config["Admin8: An Admin of the server"] = args[0];
                                                Config["Admin8: Used"] = true;
                                            }
                                            else
                                            {
                                                if (!admincheck9)
                                                {
                                                    admin9 = args[0];
                                                    admincheck9 = true;
                                                    Config["Admin9: An Admin of the server"] = args[0];
                                                    Config["Admin9: Used"] = true;
                                                }
                                                else
                                                {
                                                    if (!admincheck10)
                                                    {
                                                        admin10 = args[0];
                                                        admincheck10 = true;
                                                        Config["Admin10: An Admin of the server"] = args[0];
                                                        Config["Admin10: Used"] = true;
                                                    }
                                                    else
                                                    {

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
    }
}