using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System.Text;
using System.Collections;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("L.I.M.E. Bench", "Deicide666ra", "1.1.2", ResourceId = 1155)]
    class LimeBench : RustPlugin
    {
        //*********************************************
        // Config values
        //*********************************************
        float c_craftingMultiplier;
        float c_gunpowderMultiplier;
        float c_benchMultiplier;

        string[] c_craftingMultiplierBlacklist;
        string[] c_benchMultiplierBlacklist;
        string[] c_bulkCraftBlacklist;

        int c_craftingMultiplierAuthLevel;
        int c_benchMultiplierAuthLevel;

        string g_bulkCraftPermissionName= "limeBenchBulk";
        string g_bulkCraftNoCupboardPermissionName = "limeBenchBulkNoCup";


        //*********************************************
        // Rollback and reference values
        //*********************************************
        Dictionary<int, float> r_blueprintTimes = new Dictionary<int, float>();


        //*********************************************
        // Global Workset
        //*********************************************
        private bool configChanged = false;
        List<ItemBlueprint> blueprintDefinitions = new List<ItemBlueprint>();
        private FieldInfo buildingPrivlidges;


        //*********************************************
        // Init / Config functions
        //*********************************************
        void Loaded() => LoadConfigValues();
        void Unloaded() => Rollback();
        protected override void LoadDefaultConfig() => Puts("New configuration file created.");


        void LoadConfigValues()
        {
            c_craftingMultiplier = Convert.ToSingle(GetConfigValue("Crafting", "craftingMultiplier", 0.75f));
            c_gunpowderMultiplier = Convert.ToSingle(GetConfigValue("Crafting", "gunpowderMultiplier", 0.4f));
            c_benchMultiplier = Convert.ToSingle(GetConfigValue("Crafting", "benchMultiplier", 0.5f));

            c_craftingMultiplierBlacklist = ((IEnumerable)GetConfigValue("Blacklists", "craftingMultiplierBlacklist", new string[] { })).Cast<object>().Select(x => x.ToString()).ToArray();
            c_benchMultiplierBlacklist = ((IEnumerable)GetConfigValue("Blacklists", "benchMultiplierBlacklist", new string[] { })).Cast<object>().Select(x => x.ToString()).ToArray();
            c_bulkCraftBlacklist = ((IEnumerable)GetConfigValue("Blacklists", "bulkCraftBlacklist", new string[] { })).Cast<object>().Select(x => x.ToString()).ToArray();

            c_craftingMultiplierAuthLevel = Convert.ToInt32(GetConfigValue("Authorizations", "craftingMultiplierAuthLevel", 0));
            c_benchMultiplierAuthLevel = Convert.ToInt32(GetConfigValue("Authorizations", "benchMultiplierAuthLevel", 0));
            
            if (configChanged)
            {
                Puts("Configuration file updated.");
                SaveConfig();
            }
        }

        
        object GetConfigValue(string category, string setting, object defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }

            if (data.TryGetValue(setting, out value)) return value;
            value = defaultValue;
            data[setting] = value;
            configChanged = true;
            return value;
        }


        void Rollback()
        {
            // Rollback crafting times
            foreach (var bp in blueprintDefinitions)
                bp.time = r_blueprintTimes[bp.targetItem.itemid];
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<color=yellow>LimeBench 1.1.2</color> Â· Crafting speed controler");
            sb.AppendLine($"  Â· <color=lime>Global craft time multiplier</color> is <color=yellow>{c_craftingMultiplier}</color>");
            sb.AppendLine($"  Â· <color=lime>Gunpowder craft time</color> is <color=yellow>{c_gunpowderMultiplier}</color>");
            sb.AppendLine($"  Â· <color=lime>Any authorized cupboard in range</color> gives extra craft <color=yellow>{c_benchMultiplier}</color>");
            player.ChatMessage(sb.ToString());
        }


        //*********************************************
        // Events/Hooks
        //*********************************************
        [ChatCommand("limebench")]
        void cmdLimebench(BasePlayer player, string cmd, string[] args)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<color=lime>[LIME] Bench</color> 1.1.2 by Deicide666ra (aka The Big Bad Wolf)");
            sb.AppendLine($"  Â· <color=lime>Global craft time multiplier</color> is <color=yellow>{c_craftingMultiplier}</color>");
            sb.AppendLine($"  Â· <color=lime>Gunpowder craft time</color> is <color=yellow>{c_gunpowderMultiplier}</color>");
            sb.AppendLine($"  Â· <color=lime>Any authorized cupboard in range</color> further affects crafting time by <color=yellow>{c_benchMultiplier}</color>");
            SendReply(player, sb.ToString());
        }

        void OnServerInitialized()
        {
            buildingPrivlidges = typeof(BasePlayer).GetField("buildingPrivlidges", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            blueprintDefinitions.Clear();
            var gameObjectArray = FileSystem.LoadAll<GameObject>("Assets/Items/");

            blueprintDefinitions = ItemManager.bpList.ToList<ItemBlueprint>();
            foreach (var bp in blueprintDefinitions)
                r_blueprintTimes.Add(bp.targetItem.itemid, bp.time);

            // Create the bulk permission
            var exists= permission.PermissionExists(g_bulkCraftPermissionName);
            if (!exists)
            {
                permission.RegisterPermission(g_bulkCraftPermissionName, this);
                Puts($"Registered permission [{g_bulkCraftPermissionName}].");
            }

            // Create the bulk permission for no cupboard
            exists = permission.PermissionExists(g_bulkCraftNoCupboardPermissionName);
            if (!exists)
            {
                permission.RegisterPermission(g_bulkCraftNoCupboardPermissionName, this);
                Puts($"Registered permission [{g_bulkCraftNoCupboardPermissionName}].");
            }            
        }

        void BroadcastToChat(string msg)
        {
            ConsoleSystem.Broadcast("chat.add \"SERVER\" " + msg + " 1.0", new object[0]);
        }
        
        bool UserIsAuthorizedOnAnyCupboard(BasePlayer player)
        {
            List<BuildingPrivlidge> playerpriv = buildingPrivlidges.GetValue(player) as List<BuildingPrivlidge>;
            if (playerpriv.Count == 0) return false;
            foreach (BuildingPrivlidge priv in playerpriv.ToArray())
            {
                List<ProtoBuf.PlayerNameID> authorized = priv.authorizedPlayers;
                foreach (ProtoBuf.PlayerNameID pni in authorized.ToArray())
                    if (pni.userid == player.userID) return true;
            }
            return false;
        }


        void OnItemCraft(ItemCraftTask task)
        {
            AdjustCraftingTime(task.owner, task.blueprint, task);
        }

        int GetStackSize(string shortname)
        {
            var item= ItemManager.itemList.FirstOrDefault(i => i.shortname == shortname);
            if (item == null) throw new Exception("failed to get stack size for " + shortname);
            return item.stackable;
        }

        void GiveItemsToPlayer(BasePlayer player, string shortname, int skinID, int amount)
        {
			var initialAmount= amount;
            var stackSize = 1;
            try { stackSize = GetStackSize(shortname); } catch { }

			int loops= 0;
            while (amount > 0)
            {
                var giving = amount > stackSize ? stackSize : amount;
                var item = ItemManager.CreateByName(shortname, giving);
                item.skin = skinID;
                player.GiveItem(item);
                amount -= giving;
				loops++;
				
				if (loops > 40)
				{
					Puts($"Infinite loop detected for {shortname} X {initialAmount} (giving {giving} and {amount} left)");
					break;
				}
            }
        }

        bool CanBulk(BasePlayer player)
        {
            // Check if player has the bulkcraft permission
            if (!permission.UserHasPermission(player.UserIDString,
                g_bulkCraftPermissionName))
                return false;

            // If the player has no cupboard access, make sure he has the nocup permission
            if (!UserIsAuthorizedOnAnyCupboard(player) &&
                !permission.UserHasPermission(player.UserIDString,
                g_bulkCraftNoCupboardPermissionName))
                return false;

            return true;
        }

        void AdjustCraftingTime(BasePlayer player, ItemBlueprint bp, ItemCraftTask task)
        {
            var multipler = 1.0f;

            if (!c_craftingMultiplierBlacklist.Contains(bp.targetItem.shortname) &&
                player.net.connection.authLevel >= c_craftingMultiplierAuthLevel)
                multipler = bp.targetItem.shortname == "gunpowder" ? c_gunpowderMultiplier : c_craftingMultiplier;

            if (UserIsAuthorizedOnAnyCupboard(player) && 
                !c_benchMultiplierBlacklist.Contains(bp.targetItem.shortname) &&
                player.net.connection.authLevel >= c_benchMultiplierAuthLevel)
                multipler *= c_benchMultiplier;

            var crafter = player.inventory.crafting;

            if (CanBulk(player) && !c_bulkCraftBlacklist.Contains(bp.targetItem.shortname))
            {
                int amount = task.blueprint.amountToCreate * task.amount;
                int stackable = 1;
                try { stackable= GetStackSize(task.blueprint.targetItem.shortname); } catch { }
                    
                if (amount / stackable > 30)
                {
                    player.ChatMessage($"Could not bulkcraft {task.blueprint.targetItem.displayName.translated} X{amount}, try a smaller amount.");
                    return;
                }

                var tick = DateTime.Now;
                GiveItemsToPlayer(player, task.blueprint.targetItem.shortname, task.skinID, amount);
                var elapsed = (DateTime.Now - tick).TotalMilliseconds;
                if (elapsed > 10) Puts($"Warning: Bulkcraft took {elapsed} ms");

                crafter.CancelTask(task.taskUID, false);
                task.cancelled = true;
                
                return;
            }

            float stockTime = 0;
            var ret= r_blueprintTimes.TryGetValue(bp.targetItem.itemid, out stockTime);
            if (ret) bp.time = stockTime * multipler;
            else
            {
                Puts($"Dictionary access error trying to get stock crafting time for <{bp.targetItem.shortname}>");
            }
        }

        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            BasePlayer player = task.owner;
            var crafter = player.inventory.crafting;
            if (crafter.queue.Count == 0) return;
            AdjustCraftingTime(player, crafter.queue.First().blueprint, task);
        }
    }
}