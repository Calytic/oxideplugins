using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("RemoverTool", "Reneb", "4.0.9", ResourceId = 651)]
    class RemoverTool : RustPlugin
    {
        [PluginReference]
        Plugin Friends;

        static RemoverTool rt = new RemoverTool();

        #region Fields

        static FieldInfo serverInput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        static FieldInfo buildingPrivilege = typeof(BasePlayer).GetField("buildingPrivilege", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        static int colliderRemovable = LayerMask.GetMask("Construction", "Deployed", "Default");
        static int colliderBuilding = LayerMask.GetMask("Construction");
        static int colliderPlayer = LayerMask.GetMask("Player (Server)");

        bool RemoveOverride = false;

        static string permissionNormal = "removertool.remove";
        static string permissionOverride = "removertool.override";
        static string permissionAdmin = "removertool.admin";
        static string permissionAll = "removertool.all";
        static string permissionTarget = "removertool.target";

        static int authTarget = 1;
        static int authNormal = 0;
        static int authAdmin = 2;
        static int authAll = 2;
        static int authOverride = 1;

        static int removeDistanceNormal = 2;
        static int removeDistanceAdmin = 20;
        static int removeDistanceAll = 100;

        static bool removeGibsNormal = true;
        static bool removeGibsAdmin = true;
        static bool removeGibsAll = false;

        static int RemoveDefaultTime = 30;
        static int RemoveMaxTime = 300;

        static bool RemoveWithToolCupboards = false;
        static bool RemoveWithEntityOwners = true;
        static bool RemoveWithBuildingOwners = true;
        static bool RemoveWithRustIO = true;
        static bool RemoveWithFriends = true;

        static bool RaidBlocker = true;
        static bool RaidBlockerBlockBuildingID = true;
        static bool RaidBlockerBlockSurroundingPlayers = true;
        static int RaidBlockerRadius = 120;
        static int RaidBlockerTime = 300;

        static Dictionary<string, object> Price = new Dictionary<string, object>();
        static Dictionary<string, object> Refund = new Dictionary<string, object>();
        static Dictionary<string, object> ValidEntities = new Dictionary<string, object>();

        static string GUIRemoverToolBackgroundColor = "0.1 0.1 0.1 0";
        static string GUIRemoverToolAnchorMin = "0.1 0.65";
        static string GUIRemoverToolAnchorMax = "0.4 0.95";

        static string GUIRemoveBackgroundColor = "0.1 0.1 0.1 0.98";
        static string GUIRemoveAnchorMin = "0 0.9";
        static string GUIRemoveAnchorMax = "0.55 1";

        static string GUIRemoveTextColor = "1 0.1 0.1 0.98";
        static int GUIRemoveTextSize = 16;
        static string GUIRemoveTextAnchorMin = "0.1 0";
        static string GUIRemoveTextAnchorMax = "1 1";

        static string GUITimeLeftBackgroundColor = "0.1 0.1 0.1 0.98";
        static string GUITimeLeftAnchorMin = "0.55 0.9";
        static string GUITimeLeftAnchorMax = "1 1";

        static string GUITimeLeftTextColor = "1 1 1 0.98";
        static int GUITimeLeftTextSize = 16;
        static string GUITimeLeftTextAnchorMin = "0 0";
        static string GUITimeLeftTextAnchorMax = "0.9 1";

        static string GUIEntityBackgroundColor = "0.1 0.1 0.1 0.98";
        static string GUIEntityAnchorMin = "0 0.8";
        static string GUIEntityAnchorMax = "1 0.9";

        static string GUIEntityTextColor = "1 1 1 0.98";
        static int GUIEntityTextSize = 16;
        static string GUIEntityTextAnchorMin = "0.05 0";
        static string GUIEntityTextAnchorMax = "1 1";

        static bool GUIAuthorizations = true;
        static string GUIAllowedBackgroundColor = "0.1 1 0.1 0.3";
        static string GUIRefusedBackgroundColor = "1 0.1 0.1 0.3";
        static string GUIAuthorizationsAnchorMin = "0 0.8";
        static string GUIAuthorizationsAnchorMax = "1 0.9";

        static string GUIPriceBackgroundColor = "0.1 0.1 0.1 0.98";
        static string GUIPriceAnchorMin = "0 0.60";
        static string GUIPriceAnchorMax = "1 0.80";

        static bool GUIPrices = true;
        static string GUIPriceTextColor = "1 1 1 0.98";
        static int GUIPriceTextSize = 16;
        static string GUIPriceTextAnchorMin = "0.05 0";
        static string GUIPriceTextAnchorMax = "0.3 1";

        static string GUIPrice2TextColor = "1 1 1 0.98";
        static int GUIPrice2TextSize = 16;
        static string GUIPrice2TextAnchorMin = "0.35 0";
        static string GUIPrice2TextAnchorMax = "1 1";

        static string GUIRefundBackgroundColor = "0.1 0.1 0.1 0.98";
        static string GUIRefundAnchorMin = "0 0.40";
        static string GUIRefundAnchorMax = "1 0.60";

        static bool GUIRefund = true;
        static string GUIRefundTextColor = "1 1 1 0.98";
        static int GUIRefundTextSize = 16;
        static string GUIRefundTextAnchorMin = "0.05 0";
        static string GUIRefundTextAnchorMax = "0.3 1";

        static string GUIRefund2TextColor = "1 1 1 0.98";
        static int GUIRefund2TextSize = 16;
        static string GUIRefund2TextAnchorMin = "0.35 0";
        static string GUIRefund2TextAnchorMax = "1 1";


        static Dictionary<string, string> PrefabNameToDeployable = new Dictionary<string, string>();
        static Dictionary<string, string> PrefabNameToStructure = new Dictionary<string, string>();
        static Dictionary<string, int> ItemNameToItemID = new Dictionary<string, int>();
        static Hash<uint, float> LastAttackedBuildings = new Hash<uint, float>();
        static Hash<ulong, float> LastBlockedPlayers = new Hash<ulong, float>();

        public enum RemoveType
        {
            All,
            Structure,
            Admin,
            Normal
        }

        #endregion

        #region Config
        protected override void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        #endregion

        #region Oxide Hooks
        void LoadConfigs()
        {
            CheckCfg<string>("Remove - Access - Oxide Permissions - Normal", ref permissionNormal);
            CheckCfg<string>("Remove - Access - Oxide Permissions - Override", ref permissionOverride);
            CheckCfg<string>("Remove - Access - Oxide Permissions - Admin", ref permissionAdmin);
            CheckCfg<string>("Remove - Access - Oxide Permissions - All", ref permissionAll);
            CheckCfg<string>("Remove - Access - Oxide Permissions - Target", ref permissionTarget);

            CheckCfg<int>("Remove - Access - AuthLevel - Normal", ref authNormal);
            CheckCfg<int>("Remove - Access - AuthLevel - Override", ref authOverride);
            CheckCfg<int>("Remove - Access - AuthLevel - Admin", ref authAdmin);
            CheckCfg<int>("Remove - Access - AuthLevel - All", ref authAll);
            CheckCfg<int>("Remove - Access - AuthLevel - Target", ref authTarget);

            CheckCfg<int>("Remove - Distance - Normal", ref removeDistanceNormal);
            CheckCfg<int>("Remove - Distance - Admin", ref removeDistanceAdmin);
            CheckCfg<int>("Remove - Distance - All/Structure", ref removeDistanceAll);

            CheckCfg<bool>("Remove - Gibs - Normal", ref removeGibsNormal);
            CheckCfg<bool>("Remove - Gibs - Admin", ref removeGibsAdmin);
            CheckCfg<bool>("Remove - Gibs - All", ref removeGibsAll);

            CheckCfg<int>("Remove - Time - Default", ref RemoveDefaultTime);
            CheckCfg<int>("Remove - Time - Max", ref RemoveMaxTime);

            CheckCfg<bool>("Remove - Normal - Use Tool Cupboards (strongly unrecommended)", ref RemoveWithToolCupboards);
            CheckCfg<bool>("Remove - Normal - Use Entity Owners", ref RemoveWithEntityOwners);
            CheckCfg<bool>("Remove - Normal - Use Building Owners (You will need Building Owners plugin)", ref RemoveWithBuildingOwners);
            CheckCfg<bool>("Remove - Normal - Use Friends (RustIO)", ref RemoveWithRustIO);
            CheckCfg<bool>("Remove - Normal - Use Friends (Friends)", ref RemoveWithFriends);

            CheckCfg<bool>("Remove - Normal - RaidBlocker", ref RaidBlocker);
            CheckCfg<bool>("Remove - Normal - RaidBlocker - By Buildings", ref RaidBlockerBlockBuildingID);
            CheckCfg<bool>("Remove - Normal - RaidBlocker - By Surrounding Players", ref RaidBlockerBlockSurroundingPlayers);
            CheckCfg<int>("Remove - Normal - RaidBlocker - By Surrounding Players - Radius", ref RaidBlockerRadius);
            CheckCfg<int>("Remove - Normal - RaidBlocker - Time", ref RaidBlockerTime);

            ValidEntities = DefaultEntities();
            Price = DefaultPay();
            Refund = DefaultRefund();
            CheckCfg<Dictionary<string, object>>("Remove - Normal - Allowed Entities", ref ValidEntities);
            CheckCfg<Dictionary<string, object>>("Remove - Normal - Price", ref Price);
            CheckCfg<Dictionary<string, object>>("Remove - Normal - Refund", ref Refund);

            CheckCfg<string>("Remove - GUI - Main Box - Min Anchor (in Rust Window)", ref GUIRemoverToolAnchorMin);
            CheckCfg<string>("Remove - GUI - Main Box - Max Anchor (in Rust Window)", ref GUIRemoverToolAnchorMax);
            CheckCfg<string>("Remove - GUI - Main Box - Background Color", ref GUIRemoverToolBackgroundColor);

            CheckCfg<string>("Remove - GUI - Remove - Box - Min Anchor (in Main Box)", ref GUIRemoveAnchorMin);
            CheckCfg<string>("Remove - GUI - Remove - Box - Max Anchor (in Main Box)", ref GUIRemoveAnchorMax);
            CheckCfg<string>("Remove - GUI - Remove - Box - Background Color", ref GUIRemoveBackgroundColor);

            CheckCfg<string>("Remove - GUI - Remove - Text - Min Anchor (in Remove Box)", ref GUIRemoveTextAnchorMin);
            CheckCfg<string>("Remove - GUI - Remove - Text - Max Anchor (in Remove Box)", ref GUIRemoveTextAnchorMax);
            CheckCfg<string>("Remove - GUI - Remove - Text - Text Color", ref GUIRemoveTextColor);
            CheckCfg<int>("Remove - GUI - Remove - Text - Text Size", ref GUIRemoveTextSize);

            CheckCfg<string>("Remove - GUI - Timeleft - Box - Min Anchor (in Main Box)", ref GUITimeLeftAnchorMin);
            CheckCfg<string>("Remove - GUI - Timeleft - Box - Max Anchor (in Main Box)", ref GUITimeLeftAnchorMax);
            CheckCfg<string>("Remove - GUI - Timeleft - Box - Background Color", ref GUITimeLeftBackgroundColor);

            CheckCfg<string>("Remove - GUI - Timeleft - Text - Min Anchor (in Timeleft Box)", ref GUITimeLeftTextAnchorMin);
            CheckCfg<string>("Remove - GUI - Timeleft - Text - Max Anchor (in Timeleft Box)", ref GUITimeLeftTextAnchorMax);
            CheckCfg<string>("Remove - GUI - Timeleft - Text - Text Color", ref GUITimeLeftTextColor);
            CheckCfg<int>("Remove - GUI - Timeleft - Text - Text Size", ref GUITimeLeftTextSize);

            CheckCfg<string>("Remove - GUI - Entity - Box - Min Anchor (in Main Box)", ref GUIEntityAnchorMin);
            CheckCfg<string>("Remove - GUI - Entity - Box - Max Anchor (in Main Box)", ref GUIEntityAnchorMax);
            CheckCfg<string>("Remove - GUI - Entity - Box - Background Color", ref GUIEntityBackgroundColor);

            CheckCfg<string>("Remove - GUI - Entity - Text - Min Anchor (in Entity Box)", ref GUIEntityTextAnchorMin);
            CheckCfg<string>("Remove - GUI - Entity - Text - Max Anchor (in Entity Box)", ref GUIEntityTextAnchorMax);
            CheckCfg<string>("Remove - GUI - Entity - Text - Text Color", ref GUIEntityTextColor);
            CheckCfg<int>("Remove - GUI - Entity - Text - Text Size", ref GUIEntityTextSize);

            CheckCfg<bool>("Remove - GUI - Authorization Check Hightlighting Box", ref GUIAuthorizations);
            CheckCfg<string>("Remove - GUI - Authorization Check Hightlighting Box - Min Anchor (in Main Box)", ref GUIAuthorizationsAnchorMin);
            CheckCfg<string>("Remove - GUI - Authorization Check Hightlighting Box - Max Anchor (in Main Box)", ref GUIAuthorizationsAnchorMax);
            CheckCfg<string>("Remove - GUI - Authorization Check Hightlighting Box - Allowed Background", ref GUIAllowedBackgroundColor);
            CheckCfg<string>("Remove - GUI - Authorization Check Hightlighting Box - Refused Background", ref GUIRefusedBackgroundColor);

            CheckCfg<bool>("Remove - GUI - Price", ref GUIPrices);
            CheckCfg<string>("Remove - GUI - Price - Box - Min Anchor (in Main Box)", ref GUIPriceAnchorMin);
            CheckCfg<string>("Remove - GUI - Price - Box - Max Anchor (in Main Box)", ref GUIPriceAnchorMax);
            CheckCfg<string>("Remove - GUI - Price - Box - Background Color", ref GUIPriceBackgroundColor);

            CheckCfg<string>("Remove - GUI - Price - Text - Min Anchor (in Price Box)", ref GUIPriceTextAnchorMin);
            CheckCfg<string>("Remove - GUI - Price - Text - Max Anchor (in Price Box)", ref GUIPriceTextAnchorMax);
            CheckCfg<string>("Remove - GUI - Price - Text - Text Color", ref GUIPriceTextColor);
            CheckCfg<int>("Remove - GUI - Price - Text - Text Size", ref GUIPriceTextSize);

            CheckCfg<string>("Remove - GUI - Price - Text2 - Min Anchor (in Price Box)", ref GUIPrice2TextAnchorMin);
            CheckCfg<string>("Remove - GUI - Price - Text2 - Max Anchor (in Price Box)", ref GUIPrice2TextAnchorMax);
            CheckCfg<string>("Remove - GUI - Price - Text2 - Text Color", ref GUIPrice2TextColor);
            CheckCfg<int>("Remove - GUI - Price - Text2 - Text Size", ref GUIPrice2TextSize);

            CheckCfg<bool>("Remove - GUI - Refund", ref GUIRefund);
            CheckCfg<string>("Remove - GUI - Refund - Box - Min Anchor (in Main Box)", ref GUIRefundAnchorMin);
            CheckCfg<string>("Remove - GUI - Refund - Box - Max Anchor (in Main Box)", ref GUIRefundAnchorMax);
            CheckCfg<string>("Remove - GUI - Refund - Box - Background Color", ref GUIRefundBackgroundColor);

            CheckCfg<string>("Remove - GUI - Refund - Text - Min Anchor (in Refund Box)", ref GUIRefundTextAnchorMin);
            CheckCfg<string>("Remove - GUI - Refund - Text - Max Anchor (in Refund Box)", ref GUIRefundTextAnchorMax);
            CheckCfg<string>("Remove - GUI - Refund - Text - Text Color", ref GUIRefundTextColor);
            CheckCfg<int>("Remove - GUI - Refund - Text - Text Size", ref GUIRefundTextSize);

            CheckCfg<string>("Remove - GUI - Refund - Text2 - Min Anchor (in Refund Box)", ref GUIRefund2TextAnchorMin);
            CheckCfg<string>("Remove - GUI - Refund - Text2 - Max Anchor (in Refund Box)", ref GUIRefund2TextAnchorMax);
            CheckCfg<string>("Remove - GUI - Refund - Text2 - Text Color", ref GUIRefund2TextColor);
            CheckCfg<int>("Remove - GUI - Refund - Text2 - Text Size", ref GUIRefund2TextSize);

            SaveConfig();
        }

        void OnServerInitialized()
        {
            InitializeRustIO();
            InitializeItems();
            InitializeConstruction();

            LoadConfigs();

            permission.RegisterPermission(permissionNormal, this);
            permission.RegisterPermission(permissionAdmin, this);
            permission.RegisterPermission(permissionTarget, this);
            permission.RegisterPermission(permissionAll, this);

            rt = this;
        }

        void Loaded()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "You don't have access to this command.", "You don't have access to this command."},
                {"{0} {1} now has remover tool activated for {2} seconds ({3})","{0} {1} now has remover tool activated for {2} seconds ({3})" },
                {"Couldn't use the RemoverTool: You don't have enough resources.","Couldn't use the RemoverTool: You don't have enough resources."},
                {"RemoverTool from your target has been deactivated.","RemoverTool from your target has been deactivated."},
                {"Couldn't use the RemoverTool: Admin has restricted this entity from being removed.","Couldn't use the RemoverTool: Admin has restricted this entity from being removed." },
                {"Couldn't use the RemoverTool: An external plugin blocked the usage","Couldn't use the RemoverTool: An external plugin blocked the usage" },
                {"Couldn't use the RemoverTool: No valid entity targeted","Couldn't use the RemoverTool: No valid entity targeted" },
                {"Couldn't use the RemoverTool: Paying system crashed! Contact an administrator with the time and date to help him understand what happened.","Couldn't use the RemoverTool: Paying system crashed! Contact an administrator with the time and date to help him understand what happened." },
                {"Couldn't use the RemoverTool: No valid entity targeted, or entity is too far.","Couldn't use the RemoverTool: No valid entity targeted, or entity is too far." },
                {"Refund:","Refund:" },
                {"Nothing","Nothing" },
                { "Price:","Price:"},
                {"Free","Free" },
                {"Timeleft: {0}secs","Timeleft: {0}secs" },
                {"Remover Tool {0}","Remover Tool {0}" },
                {"RemoverTool is currently disabled.\n","RemoverTool is currently disabled.\n" },
                {"You are not allowed to use this command option.\n","You are not allowed to use this command option.\n" },
                {"You are not allowed to use this command.\n","You are not allowed to use this command.\n" },
                {"Couldn't use the RemoverTool: The Remover Tool is blocked for another {0} seconds.","Couldn't use the RemoverTool: The Remover Tool is blocked for another {0} seconds." },
                {"Couldn't find player. Multiple players match: {0}.\n","Couldn't find player. Multiple players match: {0}.\n" },
                {"Couldn't find player. No players match this name: {0}.\n","Couldn't find player. No players match this name: {0}.\n" },
                {"Couldn't use the RemoverTool: You don't have any rights to remove this.","Couldn't use the RemoverTool: You don't have any rights to remove this." },
                {"<size=18>Remover Tool</size> by <color=#ce422b>Reneb</color>\n<color=\"#ffd479\">/remove optional:TimerInSeconds</color> - Activate/Deactivate the Remover Tool, You will need to have no highlighted items in your belt bar.","<size=18>Remover Tool</size> by <color=#ce422b>Reneb</color>\n<color=\"#ffd479\">/remove optional:TimerInSeconds</color> - Activate/Deactivate the Remover Tool, You will need to have no highlighted items in your belt bar." }
            }, this);
        }

        void Unload()
        {
            foreach (ToolRemover toolremover in Resources.FindObjectsOfTypeAll<ToolRemover>())
            {
                toolremover.Destroy();
            }
        }

        #endregion


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// RustIO Inclusion
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static Library RustIO;
        private static MethodInfo isInstalled;
        private static MethodInfo hasFriend;

        private static bool RustIOIsInstalled()
        {
            if (RustIO == null) return false;
            return (bool)isInstalled.Invoke(RustIO, new object[] { });
        }
        private void InitializeRustIO()
        {
            if (!RemoveWithRustIO)
            {
                RustIO = null;
                return;
            }
            RustIO = Interface.GetMod().GetLibrary<Library>("RustIO");
            if (RustIO == null || (isInstalled = RustIO.GetFunction("IsInstalled")) == null || (hasFriend = RustIO.GetFunction("HasFriend")) == null)
            {
                RustIO = null;
                Puts("{0}: {1}", Title, "Rust:IO is not present. You need to install Rust:IO first in order to use the RustIO option!");
            }
        }
        private static bool HasFriend(string playerId, string friendId)
        {
            if (RustIO == null) return false;
            return (bool)hasFriend.Invoke(RustIO, new object[] { playerId, friendId });
        }

        #region Initializing

        void InitializeItems()
        {
            foreach (var item in ItemManager.GetItemDefinitions())
            {
                if (!ItemNameToItemID.ContainsKey(item.displayName.english.ToLower())) ItemNameToItemID.Add(item.displayName.english.ToLower(), item.itemid);

                var itemdeployable = item?.GetComponent<ItemModDeployable>();
                if (itemdeployable == null) continue;

                if (!PrefabNameToDeployable.ContainsKey(itemdeployable.entityPrefab.resourcePath)) PrefabNameToDeployable.Add(itemdeployable.entityPrefab.resourcePath, item.displayName.english);
            }
        }
        void InitializeConstruction()
        {
            foreach (var construction in PrefabAttribute.server.GetAll<Construction>())
            {
                if (construction.deployable == null && construction.info.name.english != string.Empty)
                    if (!PrefabNameToStructure.ContainsKey(construction.fullName)) PrefabNameToStructure.Add(construction.fullName, construction.info.name.english);
            }
        }

        Dictionary<string, object> DefaultPay()
        {
            var d = new Dictionary<string, object>
            {
                {"Twigs", new Dictionary<string,object>
                    {
                        { "wood", 1 }
                    }
                },
                {"Wood", new Dictionary<string,object>
                    {
                        { "wood", 10 }
                    }
                },
                {"Stone", new Dictionary<string,object>
                    {
                        { "stones", 50 },
                        { "wood", 10 }
                    }
                },
                {"Metal", new Dictionary<string,object>
                    {
                        { "metal fragments", 75 }
                    }
                },
                {"TopTier", new Dictionary<string,object>
                    {
                         { "high quality metal", 5 }
                    }
                },
            };
            foreach (var itemname in PrefabNameToDeployable.Values)
            {
                d.Add(itemname, new Dictionary<string, object> {
                    { "wood", 75 },
                    {"metal fragments", 10 }
                });
            }

            return d;
        }

        Dictionary<string, object> DefaultEntities()
        {
            var d = new Dictionary<string, object>
            {
                {"Twigs", true},
                {"Wood", true},
                {"Stone", true},
                {"Metal", true},
                {"TopTier", true}
            };
            foreach (var itemname in PrefabNameToStructure.Values)
            {
                d.Add(itemname, true);
            }
            foreach (var itemname in PrefabNameToDeployable.Values)
            {
                d.Add(itemname, true);
            }
            return d;
        }

        Dictionary<string, object> DefaultRefund()
        {
            var d = new Dictionary<string, object>
            {
                {"Twigs", new Dictionary<string,object>
                    {
                        { "wood", 1 }
                    }
                },
                {"Wood", new Dictionary<string,object>
                    {
                        { "wood", 10 }
                    }
                },
                {"Stone", 50},
                {"Metal", new Dictionary<string,object>
                    {
                        { "metal fragments", 50 }
                    }
                },
                {"TopTier", new Dictionary<string,object>
                    {
                        { "high quality metal", 2 }
                    }
                },
            };
            foreach (var itemname in PrefabNameToDeployable.Values)
            {
                d.Add(itemname, new Dictionary<string, object> {
                    { itemname.ToLower(), 1 }
                });
            }

            return d;
        }

        #endregion

        #region Methods
        static string GetMsg(string key, BasePlayer source = null) { return rt.lang.GetMessage(key, rt, source == null ? null : source.UserIDString); }

        bool hasPermission(BasePlayer player, string perm, int authlevel)
        {
            if (player == null) return true;
            if (player.net.connection.authLevel >= authlevel) return true;
            return permission.UserHasPermission(player.userID.ToString(), perm);
        }

        string ListPlayersToString(List<IPlayer> players)
        {
            var returnstring = string.Empty;
            foreach (var player in players)
            {
                returnstring += string.Format("{0} {1}\n", player.Id, player.Name);
            }
            return returnstring;
        }

        bool GetParameters(BasePlayer player, string[] args, out RemoveType RemoveType, out BasePlayer Target, out int Time, out string Reason)
        {
            Reason = string.Empty;
            Target = player;
            RemoveType = RemoveType.Normal;
            Time = RemoveDefaultTime;

            if (args != null)
            {
                foreach (var arg in args)
                {
                    switch (arg.ToLower())
                    {
                        case "normal":
                            RemoveType = RemoveType.Normal;
                            break;
                        case "admin":
                            RemoveType = RemoveType.Admin;
                            break;
                        case "all":
                            RemoveType = RemoveType.All;
                            break;
                        case "structure":
                            RemoveType = RemoveType.Structure;
                            break;
                        default:
                            ulong userid = 0L;
                            int temptime = 0;
                            if (arg.Length == 17 && ulong.TryParse(arg, out userid)) { Target = BasePlayer.Find(arg); }
                            else if (int.TryParse(arg, out temptime)) { Time = temptime; }
                            else
                            {
                                var players = covalence.Players.FindPlayers(arg).Where(x => x.IsConnected).ToList();
                                if (players.Count == 0) { Reason += string.Format(GetMsg("Couldn't find player. No players match this name: {0}.\n", player), arg); }
                                else if (players.Count > 1) { Reason += string.Format(GetMsg("Couldn't find player. Multiple players match: {0}.\n", player), ListPlayersToString(players)); }
                                else { Target = (BasePlayer)players[0]?.Object; }
                            }
                            break;
                    }
                }
            }
            if (Target != player && !hasPermission(player, permissionTarget, authTarget)) Reason += string.Format(GetMsg("You are not allowed to use this command option.\n", player));
            if (RemoveType == RemoveType.Normal && !hasPermission(player, permissionNormal, authNormal)) Reason += string.Format(GetMsg("You are not allowed to use this command.\n", player));
            if ((RemoveType == RemoveType.All || RemoveType == RemoveType.Structure) && !hasPermission(player, permissionAll, authAll)) Reason += string.Format(GetMsg("You are not allowed to use this command option.\n", player));
            if (RemoveType == RemoveType.Admin && !hasPermission(player, permissionAdmin, authAdmin)) Reason += string.Format(GetMsg("You are not allowed to use this command option.\n", player));
            if (RemoveOverride && !hasPermission(player, permissionOverride, authOverride)) Reason += string.Format(GetMsg("RemoverTool is currently disabled.\n", player));
            if (Time > RemoveMaxTime) Time = RemoveMaxTime;

            return (Reason == string.Empty);
        }

        static void DoRemove(BaseEntity Entity, bool gibs = true)
        {
            if (Entity != null)
            {
                Interface.Oxide.CallHook("OnRemovedEntity", Entity);
                if (!Entity.isDestroyed)
                    Entity.Kill(gibs ? BaseNetworkable.DestroyMode.Gib : BaseNetworkable.DestroyMode.None);
            }
        }

        #endregion

        #region UI
        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string parent, string panelName, string color, string aMin, string aMax, bool useCursor)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panelName
                    }
                };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
        }
        public static string GetName(string prefabname)
        {
            if (PrefabNameToStructure.ContainsKey(prefabname)) return PrefabNameToStructure[prefabname];
            else if (PrefabNameToDeployable.ContainsKey(prefabname)) return PrefabNameToDeployable[prefabname];
            return string.Empty;
        }

        public static void CreateGUI(BasePlayer player, RemoveType removeType)
        {
            var panelName = "RemoverTool";
            CuiHelper.DestroyUi(player, panelName);

            var Class_Element = UI.CreateElementContainer("Overlay", panelName, GUIRemoverToolBackgroundColor, GUIRemoverToolAnchorMin, GUIRemoverToolAnchorMax, false);
            CuiHelper.AddUi(player, Class_Element);

            var panelName2 = "Remove";
            CuiHelper.DestroyUi(player, panelName2);
            var Class_Element2 = UI.CreateElementContainer(panelName, panelName2, GUIRemoveBackgroundColor, GUIRemoveAnchorMin, GUIRemoveAnchorMax, false);
            UI.CreateLabel(ref Class_Element2, panelName2, GUIRemoveTextColor, string.Format(GetMsg("Remover Tool {0}", player), removeType == RemoveType.Normal ? string.Empty : string.Format("({0})", removeType.ToString())), GUIRemoveTextSize, GUIRemoveTextAnchorMin, GUIRemoveTextAnchorMax, TextAnchor.MiddleLeft);

            CuiHelper.AddUi(player, Class_Element2);
        }
        public static void GUITimeLeftUpdate(BasePlayer player, int timeleft)
        {
            var panelName = "RemoverToolTimeLeft";
            CuiHelper.DestroyUi(player, panelName);

            var Class_Element = UI.CreateElementContainer("RemoverTool", panelName, GUITimeLeftBackgroundColor, GUITimeLeftAnchorMin, GUITimeLeftAnchorMax, false);
            UI.CreateLabel(ref Class_Element, panelName, GUITimeLeftTextColor, string.Format(GetMsg("Timeleft: {0}secs", player), timeleft.ToString()), GUITimeLeftTextSize, GUITimeLeftTextAnchorMin, GUITimeLeftTextAnchorMax, TextAnchor.MiddleLeft);

            CuiHelper.AddUi(player, Class_Element);
        }
        public static void GUIEntityUpdate(BasePlayer player, BaseEntity TargetEntity)
        {
            var panelName = "RemoverToolEntity";
            CuiHelper.DestroyUi(player, panelName);
            if (TargetEntity == null) return;
            var Name = GetName(TargetEntity.PrefabName);
            var Class_Element = UI.CreateElementContainer("RemoverTool", panelName, GUIEntityBackgroundColor, GUIEntityAnchorMin, GUIEntityAnchorMax, false);
            UI.CreateLabel(ref Class_Element, panelName, GUIEntityTextColor, Name, GUIEntityTextSize, GUIEntityTextAnchorMin, GUIEntityTextAnchorMax, TextAnchor.MiddleLeft);

            CuiHelper.AddUi(player, Class_Element);
        }

        public static void GUIPricesUpdate(BasePlayer player, bool usePrice, BaseEntity TargetEntity)
        {
            var panelName = "RemoverToolPrice";
            CuiHelper.DestroyUi(player, panelName);
            if (TargetEntity == null) return;
            Dictionary<string, object> price = new Dictionary<string, object>();
            if (usePrice)
            {
                price = GetPrice(TargetEntity);
            }
            string cost = string.Empty;
            if (price.Count == 0) cost = GetMsg("Free", player);
            else
            {
                foreach (KeyValuePair<string, object> p in price)
                {
                    cost += string.Format("{2}{0} x{1}", p.Key, p.Value.ToString(), cost != string.Empty ? "\n" : string.Empty);
                }
            }
            var Class_Element = UI.CreateElementContainer("RemoverTool", panelName, GUIPriceBackgroundColor, GUIPriceAnchorMin, GUIPriceAnchorMax, false);
            UI.CreateLabel(ref Class_Element, panelName, GUIPriceTextColor, GetMsg("Price:", player), GUIPriceTextSize, GUIPriceTextAnchorMin, GUIPriceTextAnchorMax, TextAnchor.MiddleLeft);
            UI.CreateLabel(ref Class_Element, panelName, GUIPrice2TextColor, cost, GUIPrice2TextSize, GUIPrice2TextAnchorMin, GUIPrice2TextAnchorMax, TextAnchor.MiddleLeft);
            CuiHelper.AddUi(player, Class_Element);
        }
        public static void GUIRefundUpdate(BasePlayer player, bool useRefund, BaseEntity TargetEntity)
        {
            var panelName = "RemoverToolRefund";
            CuiHelper.DestroyUi(player, panelName);
            if (TargetEntity == null) return;
            Dictionary<string, object> refund = new Dictionary<string, object>();
            if (useRefund)
            {
                refund = GetRefund(TargetEntity);
            }
            string r = string.Empty;
            if (refund.Count == 0) r = GetMsg("Nothing", player);
            else
            {
                foreach (KeyValuePair<string, object> p in refund)
                {
                    r += string.Format("{2}{0} x{1}", p.Key, p.Value.ToString(), r != string.Empty ? "\n" : string.Empty);
                }
            }
            var Class_Element = UI.CreateElementContainer("RemoverTool", panelName, GUIRefundBackgroundColor, GUIRefundAnchorMin, GUIRefundAnchorMax, false);
            UI.CreateLabel(ref Class_Element, panelName, GUIRefundTextColor, GetMsg("Refund:", player), GUIRefundTextSize, GUIRefundTextAnchorMin, GUIRefundTextAnchorMax, TextAnchor.MiddleLeft);
            UI.CreateLabel(ref Class_Element, panelName, GUIRefund2TextColor, r, GUIRefund2TextSize, GUIRefund2TextAnchorMin, GUIRefund2TextAnchorMax, TextAnchor.MiddleLeft);
            CuiHelper.AddUi(player, Class_Element);
        }

        public static void GUIAuthorizationUpdate(BasePlayer player, RemoveType removeType, BaseEntity TargetEntity, bool shouldPay)
        {
            var panelName = "RemoverToolAuth";
            CuiHelper.DestroyUi(player, panelName);
            if (TargetEntity == null) return;

            string Reason = string.Empty;
            string GUIColor = CanRemoveEntity(player, removeType, TargetEntity, shouldPay, out Reason) ? GUIAllowedBackgroundColor : GUIRefusedBackgroundColor;
            var Class_Element = UI.CreateElementContainer("RemoverTool", panelName, GUIColor, GUIAuthorizationsAnchorMin, GUIAuthorizationsAnchorMax, false);

            CuiHelper.AddUi(player, Class_Element);
        }
        public static void DestroyGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "RemoverTool");
        }
        #endregion

        #region ToolRemover Class

        class ToolRemover : MonoBehaviour
        {
            public BasePlayer player { get; set; }
            public BasePlayer source { get; set; }
            public int timeLeft { get; set; }
            public float distance { get; set; }
            public RemoveType removetype { get; set; }

            public bool Pay { get; set; }
            public bool Refund { get; set; }

            public BaseEntity TargetEntity { get; set; }
            RaycastHit RayHit;

            InputState state;

            float lastUpdate { get; set; }
            float lastRemove { get; set; }

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                lastUpdate = UnityEngine.Time.realtimeSinceStartup;
                lastRemove = UnityEngine.Time.realtimeSinceStartup;
            }

            public void Start()
            {
                state = (InputState)serverInput.GetValue(player);
                CreateGUI(player, removetype);
                CancelInvoke("RemoveUpdate");
                InvokeRepeating("RemoveUpdate", 0f, 1f);
            }

            void RemoveUpdate()
            {
                timeLeft--;
                if (timeLeft <= 0) { Destroy(); return; }
                GUITimeLeftUpdate(player, timeLeft);
                GUIEntityUpdate(player, TargetEntity);
                if (removetype == RemoveType.Normal && GUIAuthorizations) GUIAuthorizationUpdate(player, removetype, TargetEntity, Pay);
                if (removetype == RemoveType.Normal && GUIPrices) GUIPricesUpdate(player, Pay, TargetEntity);
                if (removetype == RemoveType.Normal && GUIRefund) GUIRefundUpdate(player, Refund, TargetEntity);
            }

            void FixedUpdate()
            {
                if (player.IsSleeping() || !player.IsConnected()) { Destroy(); return; }

                float currentTime = UnityEngine.Time.realtimeSinceStartup;
                if (currentTime - lastUpdate >= 0.5f)
                {
                    bool flag1 = Physics.Raycast(player.eyes.HeadRay(), out RayHit, distance, colliderRemovable);
                    TargetEntity = flag1 ? RayHit.GetEntity() : null;
                    lastUpdate = currentTime;
                }

                if (state.IsDown(BUTTON.FIRE_PRIMARY))
                {
                    if (currentTime - lastRemove >= 0.5f)
                    {
                        var returnmsg = TryRemove(player, removetype, distance, Pay, Refund);
                        if (returnmsg != string.Empty) player.ChatMessage(returnmsg);
                        lastRemove = currentTime;
                    }
                }
            }

            public void Destroy()
            {
                CancelInvoke("RemoveUpdate");
                DestroyGUI(player);
                GameObject.Destroy(this);
            }
        }
        #endregion

        #region Pay
        static bool Pay(BasePlayer player, BaseEntity TargetEntity)
        {
            var cost = GetPrice(TargetEntity);
            try
            {
                List<Item> collect = new List<Item>();
                foreach (KeyValuePair<string, object> p in cost)
                {
                    var priceName = p.Key.ToLower();
                    var amount = (int)p.Value;
                    if (ItemNameToItemID.ContainsKey(priceName))
                    {
                        var itemid = ItemNameToItemID[priceName];
                        player.inventory.Take(collect, itemid, amount);
                        player.Command("note.inv", itemid, -amount);
                    }
                    else if (priceName == "withdraw")
                    {
                        var w = Interface.Oxide.CallHook("Withdraw", player.userID, (double)amount);
                        if (w == null || !(bool)w) return false;
                    }
                }
                foreach (Item item in collect)
                {
                    item.Remove(0f);
                }
            }
            catch (Exception e) { Interface.Oxide.LogWarning(string.Format("{0} {1} couldn't pay to remove entity: {2}", player.UserIDString, player.displayName, e.Message)); return false; }

            return true;
        }
        static Dictionary<string, object> GetPrice(BaseEntity TargetEntity)
        {
            var cost = new Dictionary<string, object>();
            var buildingblock = TargetEntity.GetComponent<BuildingBlock>();
            if (buildingblock != null)
            {
                var grade = buildingblock.grade.ToString();
                if (Price.ContainsKey(grade)) cost = Price[grade] as Dictionary<string, object>;
            }
            else
            {
                var prefabname = TargetEntity.PrefabName;
                if (PrefabNameToDeployable.ContainsKey(prefabname))
                {
                    var deployablename = PrefabNameToDeployable[prefabname];
                    if (Price.ContainsKey(deployablename))
                    {
                        cost = Price[deployablename] as Dictionary<string, object>;
                    }
                }
            }
            return cost;
        }

        static bool CanPay(BasePlayer player, BaseEntity TargetEntity)
        {
            var prefabname = TargetEntity.PrefabName;

            var cost = GetPrice(TargetEntity);
            if (cost.Count == 0) return true;

            foreach (KeyValuePair<string, object> p in cost)
            {
                var priceName = p.Key.ToLower();
                var amount = (int)p.Value;
                if (ItemNameToItemID.ContainsKey(priceName))
                {
                    int c = player.inventory.GetAmount(ItemNameToItemID[priceName]);
                    if (c < amount) return false;
                }
                else if (priceName == "withdraw")
                {
                    var b = Interface.Oxide.CallHook("GetPlayerMoney", player.userID);
                    if (b == null) return false;
                    var balance = (double)b;
                    if (balance <= amount) return false;
                }
            }
            return true;
        }
        #endregion

        #region Refund
        static void GiveRefund(BasePlayer player, BaseEntity TargetEntity)
        {
            var refund = GetRefund(TargetEntity);
            foreach (KeyValuePair<string, object> p in refund)
            {
                var itemname = p.Key.ToLower();
                if (ItemNameToItemID.ContainsKey(itemname))
                {
                    var itemid = ItemNameToItemID[itemname];
                    var itemamount = (int)p.Value;
                    var item = ItemManager.CreateByItemID(itemid, itemamount);
                    player.inventory.GiveItem(item, null);
                    player.Command("note.inv", itemid, itemamount);
                }
                else { Interface.Oxide.LogWarning(string.Format("{0} {1} didn't receive refund because {2} doesn't seem to be a valid item name", player.UserIDString, player.displayName, itemname)); }
            }
        }

        static Dictionary<string, object> GetRefund(BaseEntity TargetEntity)
        {
            var refund = new Dictionary<string, object>();
            var buildingblock = TargetEntity.GetComponent<BuildingBlock>();
            if (buildingblock != null)
            {
                var grade = buildingblock.grade.ToString();
                if (Refund.ContainsKey(grade))
                {
                    if (Refund[grade] is Dictionary<string, object>)
                        refund = Refund[grade] as Dictionary<string, object>;
                    else if (Refund[grade] is int)
                    {
                        var p = (int)Refund[grade] / 100f;
                        var @enum = buildingblock.grade;
                        var c = buildingblock.blockDefinition.grades[(int)@enum];
                        foreach (var ia in c.costToBuild)
                        {
                            var a = ia.amount * p;
                            if (Mathf.Floor(a) < 1) continue;
                            refund.Add(ia.itemDef.displayName.english.ToLower(), (int)a);
                        }
                    }
                }
            }
            else
            {
                var Name = GetName(TargetEntity.PrefabName);
                if (Refund.ContainsKey(Name)) refund = Refund[Name] as Dictionary<string, object>;
            }
            return refund;
        }

        #endregion

        #region RaidBlocker
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!RaidBlocker) return;
            if (info == null) return;
            BuildingBlock block = entity?.GetComponent<BuildingBlock>();
            if (block == null) return;

            var attacker = info.InitiatorPlayer;
            if (attacker != null)
            {
                if (HasAccess(attacker, entity.GetComponent<BaseEntity>())) return;
            }

            BlockRemove(entity);
        }

        void BlockRemove(BaseCombatEntity entity)
        {
            if (RaidBlockerBlockBuildingID)
            {
                var buildingid = entity.GetComponent<BuildingBlock>()?.buildingID;
                if (buildingid == null) return;
                LastAttackedBuildings[(uint)buildingid] = UnityEngine.Time.realtimeSinceStartup;
            }

            if (RaidBlockerBlockSurroundingPlayers)
            {
                foreach (var collider in UnityEngine.Physics.OverlapSphere(entity.transform.position, (float)RaidBlockerRadius, colliderPlayer))
                {
                    var player = collider.GetComponent<BasePlayer>();
                    LastBlockedPlayers[player.userID] = UnityEngine.Time.realtimeSinceStartup;
                }
            }
        }
        static bool IsBlocked(BasePlayer player, BaseEntity TargetEntity, out float timeLeft)
        {
            timeLeft = 0f;
            if (RaidBlockerBlockBuildingID)
            {
                var buildingid = TargetEntity.GetComponent<BuildingBlock>()?.buildingID;
                if (buildingid != null)
                {
                    timeLeft = (float)RaidBlockerTime - (UnityEngine.Time.realtimeSinceStartup - LastAttackedBuildings[(uint)buildingid]);
                    if (timeLeft > 0f)
                    {
                        return true;
                    }
                }
            }
            if (RaidBlockerBlockSurroundingPlayers)
            {
                timeLeft = (float)RaidBlockerTime - (UnityEngine.Time.realtimeSinceStartup - LastBlockedPlayers[player.userID]);
                if (timeLeft > 0f)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region TryRemove
        static string TryRemove(BasePlayer player, RemoveType removeType, float distance, bool shouldPay, bool shouldRefund)
        {
            RaycastHit RayHit;
            bool flag1 = Physics.Raycast(player.eyes.HeadRay(), out RayHit, distance, colliderRemovable);
            var TargetEntity = flag1 ? RayHit.GetEntity() : null;

            if (TargetEntity == null) return GetMsg("Couldn't use the RemoverTool: No valid entity targeted, or entity is too far.", player);

            string Reason = string.Empty;
            if (!CanRemoveEntity(player, removeType, TargetEntity, shouldPay, out Reason))
            {
                return Reason;
            }

            if (removeType == RemoveType.All) { RemoveAll(TargetEntity); return string.Empty; }
            if (removeType == RemoveType.Structure) { RemoveStructure(TargetEntity); return string.Empty; }

            if (shouldPay)
            {
                bool flag2 = Pay(player, TargetEntity);
                if (!flag2)
                {
                    return GetMsg("Couldn't use the RemoverTool: Paying system crashed! Contact an administrator with the time and date to help him understand what happened.", player);
                }
            }

            if (shouldRefund)
            {
                GiveRefund(player, TargetEntity);
            }

            DoRemove(TargetEntity, removeType == RemoveType.Normal ? removeGibsNormal : removeGibsAdmin);

            return string.Empty;
        }

        #endregion

        #region Remove Conditions
        static bool CanRemoveEntity(BasePlayer player, RemoveType removeType, BaseEntity TargetEntity, bool shouldPay, out string Reason)
        {
            Reason = string.Empty;
            float timeLeft = 0f;

            if (!IsRemovableEntity(TargetEntity))
            {
                Reason = GetMsg("Couldn't use the RemoverTool: No valid entity targeted", player);
                return false;
            }

            if (removeType != RemoveType.Normal) return true;

            var externalPlugins = Interface.CallHook("canRemove", player);
            if (externalPlugins != null)
            {
                Reason = externalPlugins is string ? (string)externalPlugins : GetMsg("Couldn't use the RemoverTool: An external plugin blocked the usage", player);
                return false;
            }

            if (!IsValidEntity(TargetEntity))
            {
                Reason = GetMsg("Couldn't use the RemoverTool: Admin has restricted this entity from being removed.", player);
                return false;
            }
            if (IsBlocked(player, TargetEntity, out timeLeft))
            {
                Reason = string.Format(GetMsg("Couldn't use the RemoverTool: The Remover Tool is blocked for another {0} seconds.", player), timeLeft.ToString());
                return false;
            }

            if (shouldPay && removeType == RemoveType.Normal && !CanPay(player, TargetEntity))
            {
                Reason = GetMsg("Couldn't use the RemoverTool: You don't have enough resources.", player);
                return false;
            }

            if (HasAccess(player, TargetEntity)) return true;

            Reason = GetMsg("Couldn't use the RemoverTool: You don't have any rights to remove this.", player);

            return false;
        }
        bool AreFriends(string steamid, string friend)
        {
            if (RemoveWithRustIO && RustIOIsInstalled())
            {
                if (HasFriend(steamid, friend)) return true;
            }
            if (RemoveWithFriends && Friends != null)
            {
                var r = Friends.CallHook("HasFriend", steamid, friend);
                if (r != null && (bool)r) return true;
            }
            return false;
        }
        static bool HasAccess(BasePlayer player, BaseEntity TargetEntity)
        {
            if (RemoveWithEntityOwners)
            {
                if (TargetEntity.OwnerID == player.userID) return true;
                if (rt.AreFriends(TargetEntity.OwnerID.ToString(), player.userID.ToString())) return true;
            }
            if (RemoveWithBuildingOwners)
            {
                BuildingBlock BuildingRef = TargetEntity.GetComponent<BuildingBlock>();
                if (BuildingRef == null)
                {
                    RaycastHit supportHit;
                    if (Physics.Raycast(TargetEntity.transform.position + new Vector3(0f, 0.1f, 0f), new Vector3(0f, -1f, 0f), out supportHit, 3f, colliderBuilding))
                    {
                        BaseEntity supportEnt = supportHit.GetEntity();
                        if (supportEnt != null)
                        {
                            BuildingRef = supportEnt.GetComponent<BuildingBlock>();
                        }
                    }
                }
                if (BuildingRef != null)
                {
                    var returnhook = Interface.GetMod().CallHook("FindBlockData", new object[] { BuildingRef });
                    if (returnhook is string)
                    {
                        string ownerid = (string)returnhook;
                        if (player.userID.ToString() == ownerid) return true;
                        if (rt.AreFriends(ownerid, player.userID.ToString())) return true;
                    }
                }
            }
            if (RemoveWithToolCupboards && hasTotalAccess(player))
            {
                return true;
            }
            return false;
        }

        static bool IsRemovableEntity(BaseEntity entity)
        {
            var Name = GetName(entity.PrefabName);
            return (!(Name == string.Empty));
        }
        static bool IsValidEntity(BaseEntity entity)
        {
            var Name = GetName(entity.PrefabName);

            if (ValidEntities.ContainsKey(Name) && !(bool)ValidEntities[Name]) return false;

            var buildingblock = entity.GetComponent<BuildingBlock>();
            if (buildingblock != null)
            {
                if (ValidEntities.ContainsKey(buildingblock.grade.ToString()) && !(bool)ValidEntities[buildingblock.grade.ToString()]) return false;
            }

            return true;
        }
        static bool hasTotalAccess(BasePlayer player)
        {
            List<BuildingPrivlidge> playerpriv = buildingPrivilege.GetValue(player) as List<BuildingPrivlidge>;
            if (playerpriv.Count == 0)
            {
                return false;
            }
            foreach (BuildingPrivlidge priv in playerpriv.ToArray())
            {
                List<ProtoBuf.PlayerNameID> authorized = priv.authorizedPlayers;
                bool flag1 = false;
                foreach (ProtoBuf.PlayerNameID pni in authorized.ToArray())
                {
                    if (pni.userid == player.userID)
                        flag1 = true;
                }
                if (!flag1)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Remove All
        static void RemoveAll(BaseEntity sourceEntity)
        {
            var current = 0;
            var checkFrom = new List<Vector3> { sourceEntity.transform.position };
            var removeList = new List<BaseEntity>();

            while (true)
            {
                if (current >= checkFrom.Count) break;

                List<BaseEntity> list = Pool.GetList<BaseEntity>();
                Vis.Entities<BaseEntity>(checkFrom[current], 3f, list, colliderRemovable);

                for (int i = 0; i < list.Count; i++)
                {
                    var entity = list[i];

                    if (removeList.Contains(entity)) continue;
                    removeList.Add(entity);

                    if (!checkFrom.Contains(entity.transform.position)) checkFrom.Add(entity.transform.position);

                }
                current++;
            }

            ServerMgr.Instance.StartCoroutine(DelayRemove(removeList));
        }

        static bool RemoveStructure(BaseEntity sourceEntity)
        {
            var buildingBlock = sourceEntity.GetComponent<BuildingBlock>();
            if (buildingBlock == null) return false;
            var buildingId = buildingBlock.buildingID;

            var removeList = UnityEngine.GameObject.FindObjectsOfType<BuildingBlock>().Where(x => x.buildingID == buildingId).ToList();

            ServerMgr.Instance.StartCoroutine(DelayRemove(removeList));
            return true;
        }

        public static IEnumerator DelayRemove(List<BuildingBlock> entities)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                DoRemove(entities[i], false);
                yield return new WaitWhile(new Func<bool>(() => (!entities[i].isDestroyed)));
            }
        }
        public static IEnumerator DelayRemove(List<BaseEntity> entities)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                DoRemove(entities[i], false);
                yield return new WaitWhile(new Func<bool>(() => (!entities[i].isDestroyed)));
            }
        }
        #endregion

        #region Toggle Remove
        string ToggleRemove(BasePlayer player, string[] args)
        {
            RemoveType RemoveType = RemoveType.Normal;
            BasePlayer Target = player;
            int Time = RemoveDefaultTime;
            string Reason = string.Empty;

            if (args == null || args.Length == 0)
            {
                var SourceRemover = player.GetComponent<ToolRemover>();
                if (SourceRemover != null) { SourceRemover.Destroy(); return string.Empty; }
            }

            bool flag1 = GetParameters(player, args, out RemoveType, out Target, out Time, out Reason);
            if (!flag1)
            {
                return Reason;
            }

            if (player != Target && (args != null && args.Length == 1))
            {
                var TargetRemover = Target.GetComponent<ToolRemover>();
                if (TargetRemover != null) { TargetRemover.Destroy(); return GetMsg("RemoverTool from your target has been deactivated.", player); }
            }

            var RemoverTool = Target.GetComponent<ToolRemover>();
            if (RemoverTool == null) RemoverTool = Target.gameObject.AddComponent<ToolRemover>();

            RemoverTool.source = player;
            RemoverTool.timeLeft = Time;
            RemoverTool.removetype = RemoveType;
            RemoverTool.Pay = (RemoveType == RemoveType.Normal);
            RemoverTool.Refund = (RemoveType == RemoveType.Normal);
            RemoverTool.distance = RemoveType == RemoveType.Normal ? (float)removeDistanceNormal : RemoveType == RemoveType.Admin ? (float)removeDistanceAdmin : (RemoveType == RemoveType.All || RemoveType == RemoveType.Structure) ? (float)removeDistanceAll : (float)removeDistanceNormal;
            RemoverTool.Start();

            return string.Format(GetMsg("{0} {1} now has remover tool activated for {2} seconds ({3})", player), Target.UserIDString, Target.displayName, Time.ToString(), RemoveType.ToString());
        }
        #endregion

        #region Commands
        [ChatCommand("remove")]
        void cmdChatRemove(BasePlayer player, string command, string[] args)
        {
            var success = ToggleRemove(player, args);
            SendReply(player, success);
        }

        [ConsoleCommand("remove.toggle")]
        void ccmdRemoveToggle(ConsoleSystem.Arg arg)
        {
            var success = ToggleRemove(arg.Player(), arg.Args);
            arg.ReplyWith(success);
        }

        [ConsoleCommand("remove")]
        void ccmdRemove(ConsoleSystem.Arg arg)
        {

        }

        [ConsoleCommand("remove.allow")]
        void ccmdRemoveAllow(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "remove.allow true/false");
                return;
            }
            if (arg.connection != null)
            {
                if (!hasPermission(arg.Player(), permissionOverride, authOverride)) return;
                {
                    arg.ReplyWith(GetMsg("You don't have access to this command.", arg.Player()));
                    return;
                }
            }
            switch (arg.Args[0].ToLower())
            {
                case "true":
                case "1":
                    RemoveOverride = false;
                    SendReply(arg, "Remove is now allowed depending on your settings.");
                    break;
                case "false":
                case "0":
                    RemoveOverride = true;
                    SendReply(arg, "Remove is now restricted for all players (exept admins)");
                    foreach (ToolRemover toolremover in Resources.FindObjectsOfTypeAll<ToolRemover>())
                    {
                        if (toolremover.removetype == RemoveType.Normal && toolremover.source == toolremover.player)
                        {
                            SendReply(toolremover.player, "The Remover Tool has been disabled by the admin");
                            timer.Once(0.01f, () => GameObject.Destroy(toolremover));
                        }
                    }
                    break;
                default:
                    SendReply(arg, "This is not a valid argument");
                    break;
            }
        }
        #endregion

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            SendReply(player, GetMsg("<size=18>Remover Tool</size> by <color=#ce422b>Reneb</color>\n<color=\"#ffd479\">/remove optional:TimerInSeconds</color> - Activate/Deactivate the Remover Tool, You will need to have no highlighted items in your belt bar.", player));
        }
    }
}
