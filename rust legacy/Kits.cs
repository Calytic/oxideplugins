// Reference: Oxide.Ext.RustLegacy
// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;

namespace Oxide.Plugins
{
    [Info("Kits", "Reneb", "1.0.8", ResourceId = 925)]
    class Kits : RustLegacyPlugin
    {
        static string noAccess = "You are not allowed to use this command";
        static List<object> permissionsList = GetDefaultPermList();
        static string itemNotFound = "Item not found: ";
        static string cantUseKit = "You are not allowed to use this kit";
        static string maxKitReached = "You've used all your tokens for this kit";
        static string unknownKit = "This kit doesn't exist";
        static string kitredeemed = "You've redeemed a kit";
        static string kitsreset = "All kits data from players were deleted";
        static string kithelp = "/kit => get the full list of kits";
        private bool shouldstrip = true;

        private DateTime epoch;
        private Core.Configuration.DynamicConfigFile KitsConfig;
        private Core.Configuration.DynamicConfigFile KitsData;
        private Dictionary<string, ItemDataBlock> displaynameToDataBlock = new Dictionary<string, ItemDataBlock>();
		private List<string> permNames = new List<string>();

        void Loaded()
        {
            epoch = new System.DateTime(1970, 1, 1);
            foreach (var perm in permissionsList)
            {
                if (!permission.PermissionExists(perm.ToString())) permission.RegisterPermission(perm.ToString(), this);
                if(!permNames.Contains(perm.ToString())) permNames.Add(perm.ToString());
            }
            InitializeKits();
        }
        void OnServerInitialized()
        {
            InitializeTable();
        }
        double CurrentTime()
        {
            return System.DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }
        private void InitializeKits()
        {
            KitsConfig = Interface.GetMod().DataFileSystem.GetDatafile("Kits_List");
            KitsData = Interface.GetMod().DataFileSystem.GetDatafile("Kits_Data");
        }
        private void SaveKits()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("Kits_List");
        }
        private void SaveKitsData()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("Kits_Data");
        }
        private void InitializeTable()
        {
            displaynameToDataBlock.Clear();
            foreach (ItemDataBlock itemdef in DatablockDictionary.All)
            {
                displaynameToDataBlock.Add(itemdef.name.ToString().ToLower(), itemdef);
            }
        }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        
        void Init()
        {
            CheckCfg<List<object>>("Settings: Permissions List", ref permissionsList);
            CheckCfg<string>("Messages: noAccess", ref noAccess);
            CheckCfg<string>("Messages: itemNotFound", ref itemNotFound);
            CheckCfg<string>("Messages: cantUseKit", ref cantUseKit);
            CheckCfg<string>("Messages: maxKitReached", ref maxKitReached);
            CheckCfg<string>("Messages: unknownKit", ref unknownKit);
            CheckCfg<string>("Messages: kitredeemed", ref kitredeemed);
            CheckCfg<string>("Messages: kitsreset", ref kitsreset);
            CheckCfg<string>("Messages: kithelp", ref kithelp);
            CheckCfg<bool>("Settings: RemoveDefaultKit", ref shouldstrip);
            SaveConfig();

        }
        
        
        void LoadDefaultConfig()
        {
        }
        
        static List<object> GetDefaultPermList()
        {
            var newobject = new List<object>();
            newobject.Add("vip");
            newobject.Add("donator");
            return newobject;
        }
        
        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin())
                return true;
            return false;
        }
        bool hasVip(NetUser netuser, string vipname)
        {
            if (netuser.CanAdmin()) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), vipname);
        }
        public object GiveItem(Inventory inventory, string itemname, int amount, Inventory.Slot.Preference pref)
        {
            itemname = itemname.ToLower();
            if (!displaynameToDataBlock.ContainsKey(itemname)) return false;
            ItemDataBlock datablock = displaynameToDataBlock[itemname];
            inventory.AddItemAmount(displaynameToDataBlock[itemname], amount, pref);
            return true;
        }
        void SendList(NetUser netuser)
        {
            var kitEnum = KitsConfig.GetEnumerator();
            bool isadmin = hasAccess(netuser);
			bool shouldShow = true;
            while (kitEnum.MoveNext())
            {
                string kitdescription = string.Empty;
                string options = string.Empty;
                string kitname = string.Empty;
                options = string.Empty;
                kitname = kitEnum.Current.Key.ToString();
                shouldShow = true;
                var kitdata = kitEnum.Current.Value as Dictionary<string, object>;
                if (kitdata.ContainsKey("description"))
                    kitdescription = kitdata["description"].ToString();
                if (kitdata.ContainsKey("max"))
                {
                    options = string.Format("{0} - {1} max", options, kitdata["max"].ToString());
                }
                if (kitdata.ContainsKey("cooldown"))
                {
                    options = string.Format("{0} - {1}s cooldown", options, kitdata["cooldown"].ToString());
                }
                if (kitdata.ContainsKey("admin"))
                {
                    options = string.Format("{0} - {1}", options, "admin");
                    if (!isadmin) shouldShow = false;
                }
                foreach (string name in permNames)
                {
                    if (kitdata.ContainsKey(name))
                    {
                        options = string.Format("{0} - {1}", options, name);
                        if (!hasVip(netuser, name)) shouldShow = false;
                    }
                }
                
                if(shouldShow)
                	SendReply(netuser, string.Format("{0} - {1} {2}", kitname, kitdescription, options));
            }
        }

        void cmdAddKit(NetUser netuser, string[] args)
        {
            if (args.Length < 3)
            {
                SendReply(netuser, "/kit add \"KITNAME\" \"DESCRIPTION\" -option1 -option2 etc, Everything you have in your inventory will be used in the kit");
                SendReply(netuser, "Options avaible:");
                SendReply(netuser, "-maxXX => max times someone can use this kit. Default is infinite.");
                SendReply(netuser, "-cooldownXX => cooldown of the kit. Default is none.");
                SendReply(netuser, "-admin => Allow to give this kit only to admins (set this for the autokit!!!!)");
                foreach( string name in permNames)
                {
                    SendReply(netuser, string.Format("-{0} => Allow to give this kit only to {0}s", name));
                }
                return;
            }
            string kitname = args[1].ToString();
            string description = args[2].ToString();
            var vip = new List<string>();
            bool admin = false;
            int max = -1;
            double cooldown = 0.0;
            if (KitsConfig[kitname] != null)
            {
                SendReply(netuser, string.Format("The kit {0} already exists. Delete it first or change the name.", kitname));
                return;
            }
            if (args.Length > 3)
            {
                object validoptions = VerifyOptions(args, out admin, out vip, out max, out cooldown);
                if (validoptions is string)
                {
                    SendReply(netuser, (string)validoptions);
                    return;
                }
            }
            Dictionary<string, object> kitsitems = GetNewKitFromPlayer(netuser);
            Dictionary<string, object> newkit = new Dictionary<string, object>();
            newkit.Add("items", kitsitems);
            if (admin)
                newkit.Add("admin", true);
            foreach (string name in vip)
            {
                newkit.Add(name, true);
            }
            if (max >= 0)
                newkit.Add("max", max);
            if (cooldown > 0.0)
                newkit.Add("cooldown", cooldown);
            newkit.Add("description", description);
            KitsConfig[kitname] = newkit;
            SaveKits();
        }
        Dictionary<string, object> GetNewKitFromPlayer(NetUser netuser)
        {
            Dictionary<string, object> kitsitems = new Dictionary<string, object>();
            List<object> wearList = new List<object>();
            List<object> mainList = new List<object>();
            List<object> beltList = new List<object>();

            IInventoryItem item;
            var inv = netuser.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            for (int i = 0; i < 40; i++)
            {
                if(inv.GetItem(i, out item))
                {
                    Dictionary<string, object> newObject = new Dictionary<string, object>();
                    newObject.Add(item.datablock.name.ToString().ToLower(), item.datablock._splittable?(int)item.uses :1);
                    if (i>=0 && i<30)
                        mainList.Add(newObject);
                    else if(i>=30 && i < 36)
                        beltList.Add(newObject);
                    else
                        wearList.Add(newObject);
                }
            }
            inv.Clear();
            kitsitems.Add("wear", wearList);
            kitsitems.Add("main", mainList);
            kitsitems.Add("belt", beltList);
            return kitsitems;
        }
        object VerifyOptions(string[] args, out bool admin, out List<string> vip, out int max, out double cooldown)
        {
            max = -1;
            admin = false;
            vip = new List<string>();
            cooldown = 0.0;
            bool error = true;
            for (int i = 3; i < args.Length; i++)
            {
                int substring = 0;
                if (args[i].StartsWith("-max"))
                {
                    substring = 4;
                    if (!(int.TryParse(args[i].Substring(substring), out max)))
                        return string.Format("Wrong Number Value for : {0}", args[i].ToString());
                }
                else if (args[i].StartsWith("-cooldown"))
                {
                    substring = 9;
                    if (!(double.TryParse(args[i].Substring(substring), out cooldown)))
                        return string.Format("Wrong Number Value for : {0}", args[i].ToString());
                }
                else if (args[i].StartsWith("-admin"))
                {
                    admin = true;
                }
                else
                {
                    error = true;
                    foreach (string name in permNames)
                    {
                        if(args[i].StartsWith("-"+name))
                        {
                            if (!vip.Contains(name)) vip.Add(name);
                            error = false;
                        }
                    }
                    if(error)
                        return string.Format("Wrong Options: {0}", args[i].ToString()); 
                }
            }
            return true;
        }
        void cmdResetKits(NetUser netuser, string[] args)
        {
            KitsData.Clear();
            SendReply(netuser, "All kits data from players were deleted");
            SaveKitsData();
        }
        void OnPlayerSpawn(PlayerClient player, bool useCamp, RustProto.Avatar avatar)
        {
            if (KitsConfig["autokit"] == null) return;
            if (avatar != null && avatar.HasPos && avatar.HasAng) return;
            object thereturn = Interface.GetMod().CallHook("canRedeemKit", new object[] { player.netUser });
            if (thereturn == null)
            {
                timer.Once(0.01f, () => StripAndGiveKit(player.netUser, "autokit"));
            }
        } 
        void StripAndGiveKit(NetUser netuser, string kitname)
        {
            if(shouldstrip) netuser.playerClient.rootControllable.idMain.GetComponent<Inventory>().Clear();
            GiveKit(netuser, kitname);
        }
        void cmdRemoveKit(NetUser netuser, string[] args)
        {
            if (args.Length < 2)
            {
                SendReply(netuser, "Kit must specify the name of the kit that you want to remove");
                return;
            }
            int kitlvl = 0;
            string kitname = args[1].ToString();
            if (KitsConfig[kitname] == null)
            {
                SendReply(netuser, string.Format("The kit {0} doesn't exist", kitname));
                return;
            }
            var kitdata = (KitsConfig[kitname]) as Dictionary<string, object>;
            var newKits = new Dictionary<string, object>();
            var enumkits = KitsConfig.GetEnumerator();
            while (enumkits.MoveNext())
            {
                if (enumkits.Current.Key.ToString() != kitname && enumkits.Current.Value != null)
                {
                    newKits.Add(enumkits.Current.Key.ToString(), enumkits.Current.Value);
                }
            }
            KitsConfig.Clear();
            foreach (KeyValuePair<string, object> pair in newKits)
            {
                KitsConfig[pair.Key] = pair.Value;
            }
            SaveKits();
            SendReply(netuser, string.Format("The kit {0} was successfully removed", kitname));
        }
        int GetKitLeft(NetUser netuser, string kitname, int max)
        {
            if (KitsData[netuser.playerClient.userID.ToString()] == null) return max;
            var data = KitsData[netuser.playerClient.userID.ToString()] as Dictionary<string, object>;
            if (!(data.ContainsKey(kitname))) return max;
            var currentkit = data[kitname] as Dictionary<string, object>;
            if (!(currentkit.ContainsKey("used"))) return max;
            return (max - (int)currentkit["used"]);
        }
        double GetKitTimeleft(NetUser netuser, string kitname, double max)
        {
            if (KitsData[netuser.playerClient.userID.ToString()] == null) return 0.0;
            var data = KitsData[netuser.playerClient.userID.ToString()] as Dictionary<string, object>;
            if (!(data.ContainsKey(kitname))) return 0.0;
            var currentkit = data[kitname] as Dictionary<string, object>;
            if (!(currentkit.ContainsKey("cooldown"))) return 0.0;
            return ((double)currentkit["cooldown"] - CurrentTime());
        }
        void TryGiveKit(NetUser netuser, string kitname)
        {
            if (KitsConfig[kitname] == null)
            {
                SendReply(netuser, unknownKit);
                return;
            }
            object thereturn = Interface.GetMod().CallHook("canRedeemKit", netuser );
            if (thereturn != null)
            {
                if (thereturn is string)
                {
                    SendReply(netuser, (string)thereturn);
                }
                return;
            }

            Dictionary<string, object> kitdata = (KitsConfig[kitname]) as Dictionary<string, object>;
            double cooldown = 0.0;
            int kitleft = 1;
            if (kitdata.ContainsKey("max"))
                kitleft = GetKitLeft(netuser, kitname, (int)(kitdata["max"]));
            if (kitdata.ContainsKey("admin"))
                if(!hasAccess(netuser))
                {
                    SendReply(netuser, cantUseKit);
                    return;
                }
            foreach (string name in permNames)
            {
                if(kitdata.ContainsKey(name))
                {
                    if (!hasVip(netuser, name))
                    {
                        SendReply(netuser, cantUseKit);
                        return;
                    }
                }
            }
            
            if (kitleft <= 0)
            {
                SendReply(netuser, maxKitReached);
                return;
            }
            if (kitdata.ContainsKey("cooldown"))
                cooldown = GetKitTimeleft(netuser, kitname, (double)(kitdata["cooldown"]));
            if (cooldown > 0.0)
            {
                SendReply(netuser, string.Format("You must wait {0}s before using this kit again", cooldown.ToString()));
                return;
            }
            object wasGiven = GiveKit(netuser, kitname);
            if ((wasGiven is bool) && !((bool)wasGiven))
            {
                Puts(string.Format("An error occurred while giving the kit {0} to {1}", kitname, netuser.playerClient.userName.ToString()));
                return;
            }
            proccessKitGiven(netuser, kitname, kitdata, kitleft);
        }

        void proccessKitGiven(NetUser netuser, string kitname, Dictionary<string, object> kitdata, int kitleft)
        {
            string userid = netuser.playerClient.userID.ToString();
            if (KitsData[userid] == null)
            {
                (KitsData[userid]) = new Dictionary<string, object>();
            }
            var playerData = (KitsData[userid]) as Dictionary<string, object>;
            var currentKitData = new Dictionary<string, object>();
            bool write = false;
            if (kitdata.ContainsKey("max"))
            {
                currentKitData.Add("used", (((int)kitdata["max"] - kitleft) + 1));
                write = true;
            }
            if (kitdata.ContainsKey("cooldown"))
            {
                currentKitData.Add("cooldown", ((double)kitdata["cooldown"] + CurrentTime()));
                write = true;
            }
            if (write)
            {
                if (playerData.ContainsKey(kitname))
                    playerData[kitname] = currentKitData;
                else
                    playerData.Add(kitname, currentKitData);
                KitsData[userid] = playerData;
                
            }
        }
        void OnServerSave()
        {
            SaveKitsData();
        }
        void Unload()
        {
            SaveKitsData();
        }
        object GiveKit(NetUser netuser, string kitname)
        {
            if (KitsConfig[kitname] == null)
            {
                SendReply(netuser, unknownKit);
                return false;
            }
            
            if (netuser.playerClient == null || netuser.playerClient.rootControllable == null) return false;

            var inv = netuser.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            var kitdata = (KitsConfig[kitname]) as Dictionary<string, object>;
            var kitsitems = kitdata["items"] as Dictionary<string, object>;
            List<object> wearList = kitsitems["wear"] as List<object>;
            List<object> mainList = kitsitems["main"] as List<object>;
            List<object> beltList = kitsitems["belt"] as List<object>;
            Inventory.Slot.Preference pref = Inventory.Slot.Preference.Define(Inventory.Slot.Kind.Armor,false,Inventory.Slot.KindFlags.Belt);

            if (wearList.Count > 0)
            {
                pref = Inventory.Slot.Preference.Define(Inventory.Slot.Kind.Armor, false, Inventory.Slot.KindFlags.Belt);
                foreach (object items in wearList)
                {
                    foreach (KeyValuePair<string, object> pair in items as Dictionary<string, object>)
                    {
                        GiveItem(inv, (string)pair.Key, (int)pair.Value, pref);
                    }
                }
            }

            if (mainList.Count > 0)
            {
                pref = Inventory.Slot.Preference.Define(Inventory.Slot.Kind.Default, false, Inventory.Slot.KindFlags.Belt);
                foreach (object items in mainList)
                {
                    foreach (KeyValuePair<string, object> pair in items as Dictionary<string, object>)
                    {
                        GiveItem(inv, (string)pair.Key, (int)pair.Value, pref);
                    }
                }
            }
            if (beltList.Count > 0)
            {
                pref = Inventory.Slot.Preference.Define(Inventory.Slot.Kind.Belt, false, Inventory.Slot.KindFlags.Belt);
                foreach (object items in beltList)
                {
                    foreach (KeyValuePair<string, object> pair in items as Dictionary<string, object>)
                    {
                        GiveItem(inv, (string)pair.Key, (int)pair.Value, pref);
                    }
                }
            }
            SendReply(netuser, kitredeemed);
            return true;
        }
        [ChatCommand("kit")]
        void cmdChatKits(NetUser player, string command, string[] args)
        {
            if (args.Length > 0 && (args[0].ToString() == "add" || args[0].ToString() == "reset" || args[0].ToString() == "remove" || args[0].ToString() == "help"))
            {
                if (!hasAccess(player))
                {
                    SendReply(player, noAccess);
                    return;
                }
                if (args[0].ToString() == "add")
                    cmdAddKit(player, args);
                else if (args[0].ToString() == "reset")
                    cmdResetKits(player, args);
                else if (args[0].ToString() == "remove")
                    cmdRemoveKit(player, args);
                else if (args[0].ToString() == "help")
                {
                    SendReply(player, "Add a Kit: /kit add name \"description\" -option1 -option2 -option3 etc");
                    SendReply(player, "Options are: -vip -vip+ -vip++ -admin -maxXX -cooldownXXXX");
                    SendReply(player, "Remove a Kit: /kit remove NAME");
                    SendReply(player, "Reset players data: /kit reset");
                }
                return;
            }
            if (args.Length == 0)
            {
                SendList(player);
                return;
            }
            TryGiveKit(player, args[0]);
        }
        void SendHelpText(NetUser netuser)
        {
            if (hasAccess(netuser)) SendReply(netuser, "Kit Commands: /kit help");
            SendReply(netuser, kithelp);
        }
    }
}