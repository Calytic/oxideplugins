using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BuildingRestriction", "Jakkee", "1.1.3", ResourceId = 2124)]
    class BuildingRestriction : RustPlugin
    {

        #region Variables

        private List<string> AllowedBuildingBlocks = new List<string> { "assets/prefabs/building core/wall.low/wall.low.prefab",
            "assets/prefabs/building core/floor/floor.prefab",
            "assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
            "assets/prefabs/building core/floor.frame/floor.frame.prefab",
            "assets/prefabs/building core/roof/roof.prefab" };
        private float MaxHeight = 15;
        private int MaxTFoundations = 24;
        private int MaxFoundations = 16;
        private string PermBypass = "buildingrestriction.bypass";
        private string TriangleFoundation = "assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab";
        private string Foundation = "assets/prefabs/building core/foundation/foundation.prefab";
        Dictionary<uint, List<BuildingBlock>> buildingids = new Dictionary<uint, List<BuildingBlock>>();

        #endregion

        #region Initialization

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["Max build height"] = 5;
            Config["Max triangle foundations"] = 24;
            Config["Max foundations"] = 16;
            Config.Save();
        }

        void Loaded()
        {
            CheckConfig();
            MaxTFoundations = GetConfig("Max triangle foundations", 24);
            MaxFoundations = GetConfig("Max foundations", 16);
            MaxHeight = GetConfig("Max build height", 5) * 3;
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

        void OnServerInitialized()
        {
            UpdateDictionary();
        }

        protected void ReloadConfig()
        {
            Config["VERSION"] = Version.ToString();
            SaveConfig();
        }

        void UpdateDictionary()
        {
            Puts("Searching for structures, This may awhile...");
            buildingids.Clear();
            var FoundationBlocks = Resources.FindObjectsOfTypeAll<BuildingBlock>().Where(x => x.name == Foundation || x.name == TriangleFoundation).ToList();
            foreach (BuildingBlock Block in FoundationBlocks)
            {
                if (!buildingids.ContainsKey(Block.buildingID))
                {
                    var structure = UnityEngine.GameObject.FindObjectsOfType<BuildingBlock>().Where(x => x.buildingID == Block.buildingID && x.name == Foundation || x.name == TriangleFoundation).ToList();
                    buildingids[Block.buildingID] = structure;
                }
            }
            Puts("Completed! Found " + buildingids.Count.ToString() + " structures");
        }

        #endregion

        #region Oxide Hooks / Core

        void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            BasePlayer player = planner.GetOwnerPlayer();
            var hasperm = HasPermission(player.UserIDString, PermBypass);
            BaseEntity entity = UnityEngine.GameObjectEx.ToBaseEntity(gameobject);
            var buildingBlock = entity?.GetComponent<BuildingBlock>()?? null;
            if(buildingBlock != null || !buildingBlock.Equals(null))
            {
                var buildingId = buildingBlock.buildingID;
                if (buildingids.ContainsKey(buildingId))
                {
                    var ConnectingStructure = buildingids[buildingBlock.buildingID];
                    if (buildingBlock.name == Foundation || buildingBlock.name == TriangleFoundation)
                    {
                        var trifcount = GetCountOf(ConnectingStructure, TriangleFoundation);
                        var fcount = GetCountOf(ConnectingStructure, Foundation);
                        if (buildingBlock.name == Foundation && fcount >= MaxFoundations)
                        {
                            if (!hasperm)
                            {
                                buildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                                SendReply(player, Lang("Limit: Foundations", player.UserIDString, MaxFoundations.ToString()), player);
                            }
                        }
                        else if (buildingBlock.name == TriangleFoundation && trifcount >= MaxTFoundations)
                        {
                            if (!hasperm)
                            {
                                buildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                                SendReply(player, Lang("Limit: Triangle Foundations", player.UserIDString, MaxTFoundations.ToString()), player);
                            }
                        }
                        else
                        {
                            var structure = new List<BuildingBlock>(ConnectingStructure);
                            structure.Add(buildingBlock);
                            buildingids[buildingId] = structure;
                        }
                    }
                    else
                    {
                        if (!AllowedBuildingBlocks.Contains(buildingBlock.name))
                        {
                            BuildingBlock firstfoundation = null;
                            foreach (BuildingBlock block in ConnectingStructure)
                            {
                                if (block.name.Contains(TriangleFoundation) || block.name.Contains(Foundation))
                                {
                                    firstfoundation = block;
                                    break;
                                }
                            }
                            if (firstfoundation != null)
                            {
                                float height = (float)Math.Round(buildingBlock.transform.position.y - firstfoundation.transform.position.y, 0, MidpointRounding.AwayFromZero);
                                if (MaxHeight <= height)
                                {
                                    if (!hasperm)
                                    {
                                        buildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                                        SendReply(player, Lang("Limit: Height", player.UserIDString, (MaxHeight / 3).ToString()), player);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    var structure = new List<BuildingBlock>();
                    structure.Add(buildingBlock);
                    buildingids[buildingId] = structure;
                }
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            var buildingBlock = entity?.GetComponent<BuildingBlock>()?? null;
            if (buildingBlock == null || buildingBlock.Equals(null))
            {
                return;
            } 
            else
            {
                if (buildingBlock.name == Foundation || buildingBlock.name == TriangleFoundation)
                {
                    if (buildingids.ContainsKey(buildingBlock.buildingID))
                    {
                        foreach (BuildingBlock Block in buildingids[buildingBlock.buildingID])
                        {
                            if (buildingBlock == Block)
                            {
                                buildingids[buildingBlock.buildingID].Remove(buildingBlock);
                                break;
                            }
                        }
                    }
                }
            }
        }

        void OnStructureDemolish(BaseCombatEntity entity, BasePlayer player)
        {
            var buildingBlock = entity?.GetComponent<BuildingBlock>() ?? null;
            if (buildingBlock == null || buildingBlock.Equals(null))
            {
                return;
            }
            else
            {
                if (buildingBlock.name == Foundation || buildingBlock.name == TriangleFoundation)
                {
                    if (buildingids.ContainsKey(buildingBlock.buildingID))
                    {
                        foreach (BuildingBlock Block in buildingids[buildingBlock.buildingID])
                        {
                            if (buildingBlock == Block)
                            {
                                buildingids[buildingBlock.buildingID].Remove(buildingBlock);
                                break;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        private int GetCountOf(List<BuildingBlock> ConnectingStructure, string buildingobject)
        {
            int count = 0;
            var templist = ConnectingStructure.ToList();
            foreach (BuildingBlock block in templist)
            {
                if (block == null || block.Equals(null))
                {
                    ConnectingStructure.Remove(block);
                }
                else
                {
                    if (block.name == buildingobject)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

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
