using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core;

using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
namespace Oxide.Plugins
{
    [Info("BlueprintBlocker", "DylanSMR", "1.0.5")]
    [Description("Blocks certain blueprint items.")]
    class BlueprintBlocker : RustPlugin
    {      
        //////////////////////////////////////////////////////////////////////////////////////
        // Local Variables
        //////////////////////////////////////////////////////////////////////////////////////   
        
        private List<string> blueprintBlacklist = new List<string>(); 
        private uint FragAmount;
        private uint EFragAmount;
        private uint PlayerTotalFrags;
        private uint EntityTotalFrags;
        
        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration API
        //////////////////////////////////////////////////////////////////////////////////////   
        
        void LoadDefaultConfig() {
            Config.Clear();
                Config["blueprintBlacklist"] = blueprintBlacklist;
                Config["NoBlueprintsAllowed"] = false;
                Config["DropNoBPFrags"] = false;
                Config["DropNoBPLibraries"] = false;
                Config["DropNoBPPages"] = false;
                Config["DropNoBPBooks"] = false;
                Config["NoUpgradeToPage"] = false;
                Config["NoUpgradeToBook"] = false;
                Config["NoUpgradeToLibrary"] = false;
                Config["SpawnedRemoverTest"] = false;
                Config["GiveItemsBack"] = true;
            Config.Save();
        }     
        
        //////////////////////////////////////////////////////////////////////////////////////
        // GetConfig(Params)
        //////////////////////////////////////////////////////////////////////////////////////   
        
        T GetConfig<T>(string key, T defaultValue) {
            try {
                var val = Config[key];
                if (val == null)
                    return defaultValue;
                if (val is List<object>) {
                    var t = typeof(T).GetGenericArguments()[0];
                    if (t == typeof(String)) {
                        var cval = new List<string>();
                        foreach (var v in val as List<object>)
                            cval.Add((string)v);
                        val = cval;
                    } else if (t == typeof(int)) {
                        var cval = new List<int>();
                        foreach (var v in val as List<object>)
                            cval.Add(Convert.ToInt32(v));
                        val = cval;
                    }
                } else if (val is Dictionary<string, object>) {
                    var t = typeof(T).GetGenericArguments()[1];
                    if (t == typeof(int)) {
                        var cval = new Dictionary<string,int>();
                        foreach (var v in val as Dictionary<string, object>)
                            cval.Add(Convert.ToString(v.Key), Convert.ToInt32(v.Value));
                        val = cval;
                    }
                }
                return (T)Convert.ChangeType(val, typeof(T));
            } catch (Exception ex) {
                return defaultValue;
            }
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // Loaded(On Plugin Loaded)
        //////////////////////////////////////////////////////////////////////////////////////   
        
        void Loaded() {
            LoadLangaugeAPI();
            blueprintBlacklist = GetConfig("blueprintBlacklist", new List<string>());    
        }        
          
        //////////////////////////////////////////////////////////////////////////////////////
        // LoadLangaugeAPI
        //////////////////////////////////////////////////////////////////////////////////////   
          
        void LoadLangaugeAPI() {
			lang.RegisterMessages(new Dictionary<string,string>{
				["BP_NOLEARN"] = "<color='#DD0000'>You may not learn {0} as it is blacklisted!</color>",
                ["BP_NOPERMS"] = "<color='#DD0000'>You do not have auth level 1/2 so you cannot preform this command!</color>",
                ["BP_NOFRAGSFOUND"] = "No player or container had any blueprint fragments!", 
                ["BP_REMOVEDFRAGS"] = "Removed {0} frag(s) from {1} different player(s) and removed {2} frags from {3} container(s).", 
                ["BP_ADDEDTOCONFIG"] = "{0} was added to the blocked blueprints file.", 
                ["BP_ALREADYINCONFIG"] = "{0} is already in the config file.", 
                ["BP_NOINCONFIG"] = "{0} is not in the blueprint blacklist file.", 
                ["BP_REMOVEDFROMCONFIG"] = "{0} was removed from the blueprint blacklist file.", 
                ["BP_MAYNOTUSE"] = "<color='#DD0000'>You may not use {0} as it is blacklisted!</color>",
                ["BP_NOBP"] = "<color='#DD0000'>You may not reveal this blueprint as it is blacklisted.</color>",
			}, this);
        }   
          
        //////////////////////////////////////////////////////////////////////////////////////
        // GetMessage(LangaugeAPI)
        //////////////////////////////////////////////////////////////////////////////////////   
          
        private string GetMessage(string name, string sid = null) {
			return lang.GetMessage(name, this, sid);
		} 
                 
        //////////////////////////////////////////////////////////////////////////////////////
        // OnConsumableUse()
        //////////////////////////////////////////////////////////////////////////////////////             
        
        void OnConsumableUse(Item item) {
            try 
            {
                BasePlayer player = item.GetOwnerPlayer();   
                if(player == null)
                {
                    return;
                }       
                var playerInfo = SingletonComponent<ServerMgr>.Instance.persistance.GetPlayerInfo(player.userID);
                if(item == null)
                {
                    return;
                }
                if (playerInfo != null)
                {      
                    if(Convert.ToBoolean(Config["NoBlueprintsAllowed"]) && item.HasFlag(Item.Flag.Blueprint))
                    {
                        SendReply(player, string.Format(GetMessage("BP_NOLEARN", player.UserIDString), item.info.shortname));
                        playerInfo.blueprints.complete.Remove(item.info.itemid);
                        SingletonComponent<ServerMgr>.Instance.persistance.SetPlayerInfo(player.userID, playerInfo);
                        player.SendNetworkUpdateImmediate();    
                        if(Convert.ToBoolean(Config["GiveItemsBack"]))
                        {
                            player.inventory.GiveItem(ItemManager.CreateByName(item.info.shortname, item.amount), player.inventory.containerMain);
                        }         
                        return;
                    }     
                    if(item.info.shortname == "blueprint_fragment" && Convert.ToBoolean(Config["NoUpgradeToPage"]))
                    {
                        SendReply(player, string.Format(GetMessage("BP_MAYNOTUSE", player.UserIDString), item.info.shortname));
                        if(item.HasFlag(Item.Flag.Blueprint))
                        {
                            return;
                        }
                        else
                        {
                            player.inventory.GiveItem(ItemManager.CreateByName(item.info.shortname, 60), player.inventory.containerMain);    
                            timer.Once(0.1f, () => player.inventory.Take(null, 1625167035, item.amount));
                        } 
                    }
                    else if(item.info.shortname == "blueprint_book" && Convert.ToBoolean(Config["NoUpgradeToLibrary"]))
                    {
                        SendReply(player, string.Format(GetMessage("BP_MAYNOTUSE", player.UserIDString), item.info.shortname));
                        if(item.HasFlag(Item.Flag.Blueprint))
                        {
                            return;
                        }
                        else
                        {
                            player.inventory.GiveItem(ItemManager.CreateByName(item.info.shortname, 4), player.inventory.containerMain);    
                            timer.Once(0.1f, () => player.inventory.Take(null, -845335793, item.amount));
                        } 
                    }
                    else if(item.info.shortname == "blueprint_page" && Convert.ToBoolean(Config["NoUpgradeToBook"]))
                    {
                        SendReply(player, string.Format(GetMessage("BP_MAYNOTUSE", player.UserIDString), item.info.shortname));
                        if(item.HasFlag(Item.Flag.Blueprint))
                        {
                            return;
                        }
                        else
                        {
                            player.inventory.GiveItem(ItemManager.CreateByName(item.info.shortname, 5), player.inventory.containerMain);    
                            timer.Once(0.1f, () => player.inventory.Take(null, 1624763669, item.amount));
                        } 
                    } 
                    else if (item.HasFlag(Item.Flag.Blueprint) && blueprintBlacklist.Contains(item.info.shortname))
                    {
                        SendReply(player, string.Format(GetMessage("BP_NOLEARN", player.UserIDString), item.info.shortname));
                        playerInfo.blueprints.complete.Remove(item.info.itemid);
                        SingletonComponent<ServerMgr>.Instance.persistance.SetPlayerInfo(player.userID, playerInfo);
                        player.SendNetworkUpdateImmediate();
                        if(Convert.ToBoolean(Config["GiveItemsBack"]))
                        {
                            player.inventory.GiveItem(ItemManager.CreateByName(item.info.shortname, item.amount), player.inventory.containerMain);
                        }     
                    }   
                    else
                    {
                        return;
                    }
                }   
            }
            catch(System.Exception)
            {
                return;
            }
        } 
        
        //////////////////////////////////////////////////////////////////////////////////////
        // OnEntitySpawned()
        ////////////////////////////////////////////////////////////////////////////////////// 
        
        private void OnEntitySpawned(BaseNetworkable entity) {
            if(Convert.ToBoolean(Config["SpawnedRemoverTest"]) == false)
            {
                return;
            }
            if(entity is LootContainer)
            {            
                var container = entity as LootContainer;
                var inv = container.inventory.itemList.ToArray();             
                if(container == null) return;
                if (container.inventory == null || container.inventory.itemList == null) return;
                foreach(var item in inv)
                {
                    if(item.HasFlag(Item.Flag.Blueprint) && blueprintBlacklist.Contains(item.info.shortname))
                    {
                        item.RemoveFromContainer();
                        item.Remove(1f);  
                    }
                    else if(item.info.shortname == "blueprint_fragment" && Convert.ToBoolean(Config["DropNoBPFrags"]))
                    {
                        item.RemoveFromContainer();
                        item.Remove(1f);  
                    }
                    else if(item.info.shortname == "blueprint_book" && Convert.ToBoolean(Config["DropNoBPBooks"]))
                    {
                        item.RemoveFromContainer();
                        item.Remove(1f);  
                    }
                    else if(item.info.shortname == "blueprint_page" && Convert.ToBoolean(Config["DropNoBPPages"]))
                    {
                        item.RemoveFromContainer();
                        item.Remove(1f);  
                    }
                    else if(item.info.shortname == "blueprint_library" && Convert.ToBoolean(Config["DropNoBPLibraries"]))
                    {
                        item.RemoveFromContainer();
                        item.Remove(1f);  
                    } 
                    else
                    {
                        return;
                    } 
                }           
            }
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // DeleteAllBPFrags
        ////////////////////////////////////////////////////////////////////////////////////// 
        
        [ConsoleCommand("deletefrags")]
        void DeleteAllFrags(ConsoleSystem.Arg arg) {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                 SendReply(arg, lang.GetMessage("BP_NOPERMS", this));
                 return;
            } 
            else
            {
                foreach(var active in BasePlayer.activePlayerList)
                {
                    var frags = active.inventory.GetAmount(1351589500);
                    if(frags >= 1)
                    {
                        active.inventory.Take(null, 1351589500, frags);
                        FragAmount++;
                        PlayerTotalFrags++;
                    }    
                    else
                    {
                    }
                }   
                foreach(var sleeper in BasePlayer.sleepingPlayerList)
                {
                    var frags = sleeper.inventory.GetAmount(1351589500);
                    if(frags >= 1)
                    {
                        sleeper.inventory.Take(null, 1351589500, frags);
                        FragAmount++;
                        PlayerTotalFrags++;
                    }    
                    else
                    {
                    }   
                } 
                var containers = UnityEngine.Object.FindObjectsOfType<LootContainer>();
                foreach(var entity in containers)
                {
                    if(entity is LootContainer)
                    {            
                        var container = entity as LootContainer;
                        var inv = container.inventory.itemList.ToArray();    
                        foreach(var item in inv)
                        {
                            if(item.info.shortname == "blueprint_fragment")
                            {
                                item.RemoveFromContainer();
                                item.Remove(1f);  
                                EFragAmount++;
                            }
                        }
                    }                  
                }
                if(FragAmount == 0 && EFragAmount == 0)
                {
                    Puts(lang.GetMessage("BP_NOFRAGSFOUND", this));
                }
                else
                {
                    Puts(string.Format(lang.GetMessage("BP_REMOVEDFRAGS", this), FragAmount, PlayerTotalFrags, EFragAmount, "?"));    
                    FragAmount = 0;
                    PlayerTotalFrags = 0;
                    EFragAmount = 0;
                    EntityTotalFrags = 0;
                }   
            }
        }
        
        [ConsoleCommand("AddConfig")]
        void AddConfig(ConsoleSystem.Arg arg) {    
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                 SendReply(arg, lang.GetMessage("BP_NOPERMS", this));
                 return;
            } 
            else
            {
                if(blueprintBlacklist.Contains(arg.Args[0].ToString()))
                {
                    Puts(string.Format(lang.GetMessage("BP_ALREADYINCONFIG", this), arg.Args[0].ToString()));          
                }
                else
                {
                    blueprintBlacklist.Add(arg.Args[0].ToString());
                    Config.Save();
                    Puts(string.Format(lang.GetMessage("BP_ADDEDTOCONFIG", this), arg.Args[0].ToString()));       
                }
            }
        }
        
        [ConsoleCommand("RemoveConfig")]
        void RemoveConfig(ConsoleSystem.Arg arg) {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                 SendReply(arg, lang.GetMessage("BP_NOPERMS", this));
                 return;
            } 
            else
            { 
                if(!blueprintBlacklist.Contains(arg.Args[0].ToString()))
                {
                    Puts(string.Format(lang.GetMessage("BP_NOINCONFIG", this), arg.Args[0].ToString()));          
                }
                else
                {
                    blueprintBlacklist.Remove(arg.Args[0].ToString());
                    Config.Save();
                    Puts(string.Format(lang.GetMessage("BP_REMOVEDFROMCONFIG", this), arg.Args[0].ToString()));       
                }   
            }   
        }
    }
}