using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Block Remover", "bawNg / Nogrod", "0.4.1")]
    class BlockRemover : RustPlugin
    {
        private ConfigData configData;
        private readonly FieldInfo entityListField = typeof(BaseNetworkable.EntityRealm).GetField("entityList", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo instancesField = typeof(MeshColliderBatch).GetField("instances", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic)).GetValue(null);
        private const string PermCount = "blockremover.count";
        private const string PermRemove = "blockremover.remove";

        class ConfigData
        {
            public float CupboardDistance { get; set; }
            public VersionNumber Version { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new ConfigData
            {
                CupboardDistance = 30f,
                Version = Version
            }, true);
        }

        void Loaded()
        {
            configData = Config.ReadObject<ConfigData>();
            if (configData.Version != Version)
            {
                configData.Version = Version;
                Config.WriteObject(configData, true);
            }
            permission.RegisterPermission(PermCount, this);
            permission.RegisterPermission(PermRemove, this);
        }

        [ConsoleCommand("block.countall")]
        void cmdCountBlockAll(ConsoleSystem.Arg arg)
        {
            if (!CheckAccess(arg, PermCount)) return;
            var stabilityEntities = FindAllCupboardlessStabilityEntities();
            SendReply(arg, $"There are {stabilityEntities.Count} blocks outside of cupboard range");
        }

        [ConsoleCommand("block.count")]
        void cmdCountBlock(ConsoleSystem.Arg arg)
        {
            if (!CheckAccess(arg, PermCount)) return;
            BuildingGrade.Enum grade;
            if (!ParseGrade(arg, out grade)) return;
            var blocks = FindAllCupboardlessBlocks(grade);
            SendReply(arg, $"There are {blocks.Count} {grade} blocks outside of cupboard range");
        }

        [ConsoleCommand("block.remove")]
        void cmdRemoveBlock(ConsoleSystem.Arg arg)
        {
            if (!CheckAccess(arg, PermRemove)) return;
            BuildingGrade.Enum grade;
            if (!ParseGrade(arg, out grade)) return;
            PrintToChat($"<color=red>Admin is removing all {grade} blocks outside of cupboard range...</color>");
            var blocks = FindAllCupboardlessBlocks(grade);
            var started_at = Time.realtimeSinceStartup;
            foreach (var building_block in blocks)
                building_block.Kill();
            Puts($"Destroyed {blocks.Count} {grade} blocks in {Time.realtimeSinceStartup - started_at:0.000} seconds");
            PrintToChat($"<color=yellow>Admin has removed {blocks.Count} {grade} blocks from the map</color>");
        }

        [ConsoleCommand("block.removeall")]
        void cmdRemoveBlockAll(ConsoleSystem.Arg arg)
        {
            if (!CheckAccess(arg, PermRemove)) return;
            PrintToChat("<color=red>Admin is removing all blocks outside of cupboard range...</color>");
            var stabilityEntities = FindAllCupboardlessStabilityEntities();
            var started_at = Time.realtimeSinceStartup;
            foreach (var building_block in stabilityEntities)
                building_block.Kill();
            Puts($"Destroyed {stabilityEntities.Count} blocks in {Time.realtimeSinceStartup - started_at:0.000} seconds");
            PrintToChat($"<color=yellow>Admin has removed {stabilityEntities.Count} blocks from the map</color>");
        }

        HashSet<BuildingBlock> FindAllCupboardlessBlocks(BuildingGrade.Enum grade)
        {
            var blocks = FindAllBuildingBlocks(grade);
            FilterAllCupboardless(blocks);
            return blocks;
        }

        HashSet<StabilityEntity> FindAllCupboardlessStabilityEntities()
        {
            var stabilityEntities = FindAllStabilityEntities();
            FilterAllCupboardless(stabilityEntities);
            return stabilityEntities;
        }

        void FilterAllCupboardless<T>(HashSet<T> blocks) where T : StabilityEntity
        {
            var toolCupboards = FindAllToolCupboards();
            float squaredDist = configData.CupboardDistance * configData.CupboardDistance;
            var started_at = Time.realtimeSinceStartup;
            foreach (var cupboard in toolCupboards)
            {
                var count = Physics.OverlapSphereNonAlloc(cupboard.transform.position, configData.CupboardDistance, colBuffer, 270532864);
                for (var i = 0; i < count; i ++)
                {
                    var collider = colBuffer[i];
                    colBuffer[i] = null;
                    if (!collider.transform.CompareTag("MeshColliderBatch"))
                    {
                        var buildingBlock = collider.GetComponentInParent<T>();
                        if (buildingBlock) blocks.Remove(buildingBlock);
                    }
                    else
                    {
                        var batch = collider.transform.GetComponent<MeshColliderBatch>();
                        var instances = (ListDictionary<Component, ColliderCombineInstance>)instancesField.GetValue(batch);
                        foreach (var item in instances.Values)
                        {
                            if ((item.bounds.ClosestPoint(cupboard.transform.position) - cupboard.transform.position).sqrMagnitude <= squaredDist)
                            {
                                var buildingBlock = item.collider?.GetComponentInParent<T>();
                                if (buildingBlock) blocks.Remove(buildingBlock);
                            }
                        }
                    }
                }
            }
            Puts($"Finding {blocks.Count} cupboardless blocks took {Time.realtimeSinceStartup - started_at:0.000} seconds");
        }

        HashSet<BuildingBlock> FindAllBuildingBlocks(BuildingGrade.Enum grade)
        {
            var started_at = Time.realtimeSinceStartup;
            var blocks = new HashSet<BuildingBlock>(((ListDictionary<uint, BaseNetworkable>)entityListField.GetValue(BaseNetworkable.serverEntities)).Values.OfType<BuildingBlock>().Where(block => block.grade == grade));
            Puts($"Finding {blocks.Count} {grade} blocks took {Time.realtimeSinceStartup - started_at:0.000} seconds");
            return blocks;
        }

        HashSet<StabilityEntity> FindAllStabilityEntities()
        {
            var started_at = Time.realtimeSinceStartup;
            var stabilityEntities = new HashSet<StabilityEntity>(((ListDictionary<uint, BaseNetworkable>)entityListField.GetValue(BaseNetworkable.serverEntities)).Values.OfType<StabilityEntity>());
            Puts($"Finding {stabilityEntities.Count} blocks took {Time.realtimeSinceStartup - started_at:0.000} seconds");
            return stabilityEntities;
        }

        BuildingPrivlidge[] FindAllToolCupboards()
        {
            var started_at = Time.realtimeSinceStartup;
            var toolCupboards = UnityEngine.Object.FindObjectsOfType<BuildingPrivlidge>();
            Puts($"Finding {toolCupboards.Length} tool cupboards took {Time.realtimeSinceStartup - started_at:0.000} seconds");
            return toolCupboards;
        }

        bool CheckAccess(ConsoleSystem.Arg arg, string perm)
        {
            if (arg != null && arg.connection == null || arg.Player() != null && (arg.Player().IsAdmin() || permission.UserHasPermission(arg.Player().UserIDString, perm)))
                return true;
            SendReply(arg, "You need to be admin to use that command");
            return false;
        }

        bool ParseGrade(ConsoleSystem.Arg arg, out BuildingGrade.Enum grade)
        {
            grade = BuildingGrade.Enum.Twigs;
            if (arg.HasArgs())
            {
                try
                {
                    grade = (BuildingGrade.Enum)Enum.Parse(typeof(BuildingGrade.Enum), arg.GetString(0), true);
                }
                catch (Exception)
                {
                    SendReply(arg, $"Unknown grade '{arg.GetString(0)}'");
                    return false;
                }
            }
            return true;
        }
    }
}
