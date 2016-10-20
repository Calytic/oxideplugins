using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NoWaterBuild", "Jakkee", "0.1", ResourceId = 000)]
    class NoWaterBuild : RustPlugin
    {

        #region Variables
        
        private string PermBypass = "nowaterbuild.bypass";
        private double Depth = 0.5;
        private bool Refund = true;

        #endregion

        #region Initialization

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["Give Refund"] = true;
            Config["Entity Depth"] = 0.5;
            Config.Save();
        }

        void Loaded()
        {
            CheckConfig();
            Depth = GetConfig("Entity Depth", 0.5);
            Refund = GetConfig("Give Refund", true);
            permission.RegisterPermission(PermBypass, this);
            lang.RegisterMessages(messages, this);
        }

        void CheckConfig()
        {
            if (Config["VERSION"] == null)
            {
                ReloadConfig();
            }
            else if (GetConfig<string>("VERSION", "") != Version.ToString())
            {
                ReloadConfig();
            }
        }

        protected void ReloadConfig()
        {
            Config["VERSION"] = Version.ToString();
            SaveConfig();
        }

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"Placement: Water", "You are not allowed to build in water"},
        };

        #endregion

        #region Oxide Hooks / Core

        void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            BasePlayer player = planner.GetOwnerPlayer();
            if (!HasPermission(player.UserIDString, PermBypass))
            {
                BaseEntity entity = UnityEngine.GameObjectEx.ToBaseEntity(gameobject);
                if (entity.WaterFactor() >= Depth)
                {
                    if (Refund)
                    {
                        var buildingBlock = entity.GetComponent<BuildingBlock>();
                        if (buildingBlock != null)
                        {
                            foreach (ItemAmount item in buildingBlock.blockDefinition.grades[(int)buildingBlock.grade].costToBuild)
                            {
                                player.inventory.GiveItem(ItemManager.CreateByItemID(item.itemid, (int)item.amount));
                            }
                        }
                    }
                    entity.Kill();
                    SendReply(player, Lang("Placement: Water"), player);
                }
            }
        }

        #endregion

        #region Helper Methods

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                Config[name] = defaultValue;
                Config.Save();
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        #endregion
    }
}
