using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Rust.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Beginner Protection", "Kation", "0.1.2", ResourceId = 910)]
    public class BeginnerProtection : RustPlugin
    {
        private Dictionary<string, string> message = new Dictionary<string, string>()
        {
            {"DoNotAttack", "He is a beginner, he has no guns, don't attack the beginner."},
            {"CanNotAttack", "You are a beginner, you can not attack anyone until you have a gun."},
            {"DoNotAttackSleeped", "Do not attack someone when he was slept."},
            {"DoNotLoot", "You can not loot anything from a sleeping player."},
            {"Enabled", "Beginner Protection is enabled."},
            {"Disabled", "Beginner Protection is disabled."},
            {"ConfigMissing", "Config missing."},
            {"Reloaded", "Beginner gift reloaded."},
            {"Usage", "Usage:"},
            {"ItemName", "item name"},
            {"Amount", "amount"},
            {"ItemNotExist", "Item does not exist."},
            {"AmountInvalid", "Amount value invalid."},
            {"GiftSetSuccess", "Set gift success."}
        };
        private Dictionary<string, object> setting = new Dictionary<string, object>()
        {
            {"IsEnabled", true},
            {"SleepAttack", false},
            {"SleepLoot", false}
        };
        private Dictionary<string, int> gift = new Dictionary<string, int>()
        {
            {"camp fire", 1},
            {"cooked wolf meat", 2},
            {"small water bottle", 1},
            {"stone hatchet", 1},
            {"wood", 200}
        };
        private Dictionary<string, string> nameTable;

        [ChatCommand("bp")]
        private void bpCommand(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                if ((bool)setting["IsEnabled"])
                    player.ChatMessage(message["Enabled"]);
                else
                    player.ChatMessage(message["Disabled"]);
            }
            else
            {
                if (player.net.connection.authLevel == 0)
                    return;
                if (args[0].ToLower() == "true")
                {
                    setting["IsEnabled"] = true;
                    Config["Settings"] = setting;
                    SaveConfig();
                    player.ChatMessage(message["Enabled"]);
                }
                else if (args[0].ToLower() == "false")
                {
                    setting["IsEnabled"] = false;
                    Config["Settings"] = setting;
                    SaveConfig();
                    player.ChatMessage(message["Disabled"]);
                }
            }
        }

        [ChatCommand("bp.gift.reload")]
        private void giftReloadCommand(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0)
                return;
            LoadConfig();
            Dictionary<string, object> gc = Config["Gifts"] as Dictionary<string, object>;
            if (gc != null)
            {
                gift = gc.Where(t => t.Value is int &&
                    (int)t.Value > 0 &&
                    nameTable.ContainsKey(t.Key.ToLower()) &&
                    ItemManager.FindItemDefinition(nameTable[t.Key.ToLower()]) != null).ToDictionary(t => t.Key.ToLower(), t => (int)t.Value);
            }
            else
            {
                player.ChatMessage(message["ConfigMissing"]);
                return;
            }
            player.ChatMessage(message["Reloaded"]);
        }

        [ChatCommand("bp.gift.set")]
        private void giftAddCommand(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length != 2)
            {
                player.ChatMessage(message["Usage"]);
                player.ChatMessage("/bp.gift.set \"[" + message["ItemName"] + "]\" [" + message["Amount"] + "]");
                return;
            }
            var itemname = args[0].ToLower();
            if (!nameTable.ContainsKey(itemname))
            {
                player.ChatMessage(message["ItemNotExist"]);
                return;
            }
            int amount;
            if (!int.TryParse(args[1], out amount))
            {
                player.ChatMessage(message["AmountInvalid"]);
                return;
            }
            if (amount < 0)
            {
                if (gift.ContainsKey(itemname))
                    gift.Remove(itemname);
            }
            else
            {
                if (gift.ContainsKey(itemname))
                    gift[itemname] = amount;
                else
                    gift.Add(itemname, amount);
            }
            Config["Gifts"] = gift;
            SaveConfig();
            player.ChatMessage(message["GiftSetSuccess"]);
        }

        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            LoadConfig();

            Dictionary<string, object> mc = Config["Messages"] as Dictionary<string, object>;
            if (mc != null)
                foreach (var key in message.Keys.ToArray())
                {
                    if (mc.ContainsKey(key))
                        message[key] = mc[key].ToString();
                }
            Dictionary<string, object> sc = Config["Settings"] as Dictionary<string, object>;
            if (sc != null)
                foreach (var key in setting.Keys.ToArray())
                {
                    if (sc.ContainsKey(key))
                    {
                        object value = sc[key];
                        value = Convert.ChangeType(value, setting[key].GetType());
                        setting[key] = value;
                    }
                }
            nameTable = ItemManager.GetItemDefinitions().ToDictionary(t => t.displayName.english.ToLower(), t => t.shortname.ToLower());

            Dictionary<string, object> gc = Config["Gifts"] as Dictionary<string, object>;
            if (gc != null)
                gift = gc.Where(t => t.Value is int &&
                    (int)t.Value > 0 &&
                    nameTable.ContainsKey(t.Key.ToLower()) &&
                    ItemManager.FindItemDefinition(nameTable[t.Key.ToLower()]) != null).ToDictionary(t => t.Key.ToLower(), t => (int)t.Value);

            Config.Clear();
            Config["Settings"] = setting;
            Config["Messages"] = message;
            Config["Gifts"] = gift;
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["Settings"] = setting;
            Config["Messages"] = message;
            Config["Gifts"] = gift;
            SaveConfig();
        }

        [HookMethod("OnPlayerAttack")]
        private object OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            if ((bool)setting["IsEnabled"])
            {
                if (hitInfo.HitEntity != null && hitInfo.HitEntity is BasePlayer)
                {
                    BasePlayer target = (BasePlayer)hitInfo.HitEntity;
                    if (!(bool)setting["SleepAttack"] && target.HasFlag(BaseEntity.Flags.Locked))
                    {
                        SendReply(attacker, message["DoNotAttackSleeped"]);
                        return true;
                    }
                    if (!target.inventory.AllItems().Any(t => t.GetHeldEntity() is BaseProjectile))
                    {
                        SendReply(attacker, message["DoNotAttack"]);
                        return true;
                    }
                    target = attacker;
                    if (!target.inventory.AllItems().Any(t => t.GetHeldEntity() is BaseProjectile))
                    {
                        SendReply(attacker, message["CanNotAttack"]);
                        return true;
                    }
                }
            }
            return null;
        }

        [HookMethod("OnPlayerRespawned")]
        private void OnPlayerRespawned(BasePlayer player)
        {
            foreach (var item in gift)
            {
                var definition = ItemManager.FindItemDefinition(nameTable[item.Key]);
                player.inventory.GiveItem(ItemManager.CreateByItemID((int)definition.itemid, item.Value, false), player.inventory.containerMain);
            }
        }

        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(BasePlayer player)
        {
            PluginTimers timer = new PluginTimers(this);
            timer.Once(10, () =>
            {
                player.SetFlag(BaseEntity.Flags.Locked, true);
            });
        }

        [HookMethod("OnPlayerSleepEnded")]
        private void OnPlayerSleepEnded(BasePlayer player)
        {
            player.SetFlag(BaseEntity.Flags.Locked, false);
        }

        [HookMethod("OnPlayerLoot")]
        private void OnPlayerLoot(PlayerLoot lootInventory, BaseEntity targetEntity)
        {
            if ((bool)setting["IsEnabled"])
            {
                if (targetEntity is BasePlayer)
                {
                    BasePlayer targetPlayer = (BasePlayer)targetEntity;
                    if (!(bool)setting["SleepLoot"] && targetPlayer.HasFlag(BaseEntity.Flags.Locked))
                    {
                        BasePlayer player = lootInventory.GetComponent<BasePlayer>();
                        player.ChatMessage(message["DoNotLoot"]);
                        NextTick(() =>
                        {
                            player.EndLooting();
                        });
                    }
                }
            }
        }
    }
}
