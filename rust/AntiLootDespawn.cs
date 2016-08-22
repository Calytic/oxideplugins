using UnityEngine;
using System;
using System.Linq;
namespace Oxide.Plugins
{
    [Info("AntiLootDespawn", "Bamabo", "1.1.0")]
    [Description("Change loot despawn time in cupboard radius")]
    public class AntiLootDespawn : RustPlugin
    {
        public float despawnMultiplier = 2.0f;
        public bool enabled = true;

        void Init()
        {
            permission.RegisterPermission("antilootdespawn", this);
            permission.RegisterPermission("antilootdespawn.multiplier", this);
            permission.RegisterPermission("antilootdespawn.enabled", this);
            despawnMultiplier = GetConfigEntry<float>("multiplier", 2.0f);
            enabled = GetConfigEntry<bool>("enabled", true);
        }

        void Unloaded()
        {
            foreach(var item in Resources.FindObjectsOfTypeAll<DroppedItem>().Where(c => c.isActiveAndEnabled))
            {
                item.CancelInvoke("IdleDestroy");
                item.Invoke("IdleDestroy", item.GetDespawnDuration());
            }
        }

        void OnEntitySpawned(BaseEntity entity) => SetDespawnTime(entity as DroppedItem);
        void SetDespawnTime(DroppedItem item)
        {
            if (!enabled)
                return;
            if (item == null)
                return;

            var entityRadius = Physics.OverlapSphere(item.transform.position, 0.5f, LayerMask.GetMask("Trigger"));

            foreach (var cupboard in entityRadius)
            {
                if (cupboard.GetComponentInParent<BuildingPrivlidge>() != null)
                {
                    item.CancelInvoke("IdleDestroy");
                    item.Invoke("IdleDestroy", despawnMultiplier * item.GetDespawnDuration());
                }
            }
        }

        [ConsoleCommand("antilootdespawn.multiplier")]
        void cmdMultiplier(ConsoleSystem.Arg args)
        {
            if(args.Player() != null)
            {
                if (!args.Player().IsAdmin() && !permission.UserHasPermission(args.Player().UserIDString, "antilootdespawn.multiplier"))
                    return;
            }else
            {
                if (!args.CheckPermissions())
                    return;
            }

            if (args.HasArgs())
            {
                despawnMultiplier = Convert.ToSingle(args.Args[0]);
                Config["multiplier"] = despawnMultiplier;
                SaveConfig();
            }
            args.ReplyWith($"antilootdespawn.multiplier = {despawnMultiplier}");


        }
        [ConsoleCommand("antilootdespawn.enabled")]
        void cmdEnabled(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
            {
                if (!args.Player().IsAdmin() && !permission.UserHasPermission(args.Player().UserIDString, "antilootdespawn.enabled"))
                    return;
            }
            else
            {
                if (!args.CheckPermissions())
                    return;
            }

            if (args.HasArgs())
            {
                enabled = (args.Args[0] == "true" ? true : args.Args[0] == "false" ? false : args.Args[0] == "1" ? true : args.Args[0] == "0" ? false : true);
                Config["enabled"] = enabled;
                SaveConfig();
            }
            args.ReplyWith($"antilootdespawn.enabled = {enabled}");
        }

       [ConsoleCommand("antilootdespawn")]
        void cmdList(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
            {
                if (!args.Player().IsAdmin() && !permission.UserHasPermission(args.Player().UserIDString, "antilootdespawn"))
                    return;
            }
            else
            {
                if (!args.CheckPermissions())
                    return;
            }

            args.ReplyWith($"antilootdespawn.enabled = {enabled}\nantilootdespawn.multiplier = {despawnMultiplier}");
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file for AntiLootDespawn");
            Config.Clear();
            Config["multiplier"] = 2.0f;
            Config["enabled"] = true;
            SaveConfig();
        }

        T GetConfigEntry<T>(string configEntry, T defaultValue)
        {
            if (Config[configEntry] == null)
            {
                Config[configEntry] = defaultValue;
                SaveConfig();
            }
            return (T)Convert.ChangeType(Config[configEntry], typeof(T));
        }
    }
}
