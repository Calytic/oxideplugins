using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BuildingRestriction", "Jakkee", "0.1.2")]
    class BuildingRestriction : RustPlugin
    {

        #region Variables

        private List<string> AllowedBuildingBlocks = new List<string> { "wall.low.prefab", "floor.prefab", "floor.triangle.prefab", "floor.frame.prefab", "roof.prefab" };
        private float MaxHeight = 15;
        private int MaxTFoundations = 24;
        private int MaxFoundations = 16;
        private string PermBypass = "buildingrestriction.bypass";

        #endregion

        #region Initialization

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["Max build height"] = 5;
            Config["Max triangle foundations"] = 24;
            Config["Max foundations"] = 16;
            Config["Permission"] = "buildingrestriction.bypass";
            Config.Save();
        }

        void Loaded()
        {
            CheckConfig();
            MaxTFoundations = GetConfig("Max triangle foundations", 24);
            MaxFoundations = GetConfig("Max foundations", 16);
            MaxHeight = GetConfig("Max build height", 5) * 3;
            PermBypass = GetConfig("Permission", "buildingrestriction.bypass");
            permission.RegisterPermission(PermBypass, this);
            lang.RegisterMessages(messages, this);
        }

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"Limit: Height", "You have reached the max building height! ({0} BuildingBlocks)"},
            {"Limit: Foundations", "You have reached the max foundations allowed! ({0} Foundations)"},
            {"Limit: Triangle Foundations", "You have reached the max triangle foundations allowed! ({0} Foundations)"},
        };

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

        #endregion

        #region Oxide Hooks / Core

        void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            BasePlayer player = planner.GetOwnerPlayer();
            if (HasPermission(player.UserIDString, PermBypass)) return;

            BaseEntity entity = UnityEngine.GameObjectEx.ToBaseEntity(gameobject);
            var buildingBlock = entity.GetComponent<BuildingBlock>();
            if (buildingBlock != null)
            {
                var buildingId = buildingBlock.buildingID;
                var removeList = UnityEngine.GameObject.FindObjectsOfType<BuildingBlock>().Where(x => x.buildingID == buildingId).ToList();
                bool first = false;
                BuildingBlock firstfoundation = null;
                var fcount = 0;
                var trifcount = 0;
                for (int i = 0; i < removeList.Count; i++)
                {
                    if (removeList[i].name.Contains("foundation"))
                    {
                        if (removeList[i].name.Contains("foundation.prefab"))
                        {
                            fcount++;
                        }
                        else
                        {
                            trifcount++;
                        }
                        if (!first)
                        {
                            firstfoundation = removeList[i];
                        }
                    }
                }
                if (firstfoundation != null)
                {
                    if (!buildingBlock.name.Contains("foundation"))
                    {
                        float height = buildingBlock.transform.position.y - firstfoundation.transform.position.y;
                        if (MaxHeight <= height)
                        {
                            foreach (string block in AllowedBuildingBlocks)
                            {
                                if (buildingBlock.name.Contains(block))
                                {
                                    return;
                                }
                            }
                            buildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                            SendReply(player, Lang("Limit: Height", player.UserIDString, (MaxHeight / 3).ToString()), player);
                        }
                    }
                    else
                    {
                        if (buildingBlock.name.Contains("foundation.prefab"))
                        {
                            if (fcount > MaxFoundations)
                            {
                                buildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                                SendReply(player, Lang("Limit: Foundations", player.UserIDString, MaxFoundations.ToString()), player);
                            }
                        }
                        else if (trifcount > MaxTFoundations)
                        {
                            buildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                            SendReply(player, Lang("Limit: Triangle Foundations", player.UserIDString, MaxTFoundations.ToString()), player);
                        }
                    }
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
