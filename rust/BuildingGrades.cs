using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Facepunch;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Building Grades", "bawNg / Nogrod", "0.3.6", ResourceId = 865)]
    class BuildingGrades : RustPlugin
    {
        private readonly FieldInfo serverInputField = typeof(BasePlayer).GetField("serverInput", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo instancesField = typeof(MeshColliderBatch).GetField("instances", BindingFlags.Instance | BindingFlags.NonPublic);
        private const string Perm = "buildinggrades.cangrade";
        private const string PermNoCost = "buildinggrades.nocost";
        private const string PermOwner = "buildinggrades.owner";
        private const float Distance = 3f;
        private readonly HashSet<ulong> runningPlayers = new HashSet<ulong>();
        private ConfigData configData;
        private readonly Dictionary<string, HashSet<uint>> categories = new Dictionary<string, HashSet<uint>>();

        class ConfigData
        {
            public int BatchSize { get; set; }
            public Dictionary<string, HashSet<string>> Categories { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                BatchSize = 500,
                Categories = new Dictionary<string, HashSet<string>>
                {
                    {
                        "foundation", new HashSet<string>
                        {
                            "assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab",
                            "assets/prefabs/building core/foundation.steps/foundation.steps.prefab",
                            "assets/prefabs/building core/foundation/foundation.prefab"
                        }
                    },
                    {
                        "wall", new HashSet<string>
                        {
                            "assets/prefabs/building core/wall.frame/wall.frame.prefab",
                            "assets/prefabs/building core/wall.window/wall.window.prefab",
                            "assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
                            "assets/prefabs/building core/wall/wall.prefab"
                        }
                    },
                    {
                        "floor", new HashSet<string>
                        {
                            "assets/prefabs/building core/floor.frame/floor.frame.prefab",
                            "assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
                            "assets/prefabs/building core/floor/floor.prefab"
                        }
                    },
                    {
                        "other", new HashSet<string>
                        {
                            "assets/prefabs/building core/roof/roof.prefab",
                            "assets/prefabs/building core/stairs.l/block.stair.lshape.prefab",
                            "assets/prefabs/building core/pillar/pillar.prefab",
                            "assets/prefabs/building core/stairs.u/block.stair.ushape.prefab"
                        }
                    }
                }
            };
            Config.WriteObject(config, true);
        }

        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NotEnoughItems", "Not enough {0}! You need {1} but you only have {2}"},
                {"NotAllowed", "<color=red>You are not allowed to use that command!</color>"},
                {"UnknownGrade", "<color=red>Unknown grade!</color>"},
                {"UnknownCategory", "<color=red>Unknown category!</color>"},
                {"NotLookingAt", "<color=red>You are not looking at a building block!</color>"},
                {"FinishedUp", "Finished upgrading!"},
                {"FinishedDown", "Finished downgrading!"},
                {"AlreadyRunning", "Already running, please wait!"},
                {"AnotherProcess", "Another process already running, please try again in a few seconds!"}
            }, this);
            configData = Config.ReadObject<ConfigData>();
            foreach (var category in configData.Categories)
            {
                var data = new HashSet<uint>();
                foreach (var prefab in category.Value)
                {
                    var prefabId = StringPool.Get(prefab);
                    if (prefabId <= 0) continue;
                    data.Add(prefabId);
                }
                categories.Add(category.Key, data);
            }
        }

        void OnServerInitialized()
        {
            permission.RegisterPermission(Perm, this);
            permission.RegisterPermission(PermNoCost, this);
            permission.RegisterPermission(PermOwner, this);
        }

        [ChatCommand("up4")]
        void UpCommand4(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, args.Length > 0 ? new[] { "4", args[0] } : new[] { "4" }, true);
        }

        [ChatCommand("up3")]
        void UpCommand3(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, args.Length > 0 ? new[] { "3", args[0] } : new[] { "3" }, true);
        }

        [ChatCommand("up2")]
        void UpCommand2(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, args.Length > 0 ? new[] { "2", args[0] } : new[] { "2" }, true);
        }

        [ChatCommand("up1")]
        void UpCommand1(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, args.Length > 0 ? new[] { "1", args[0] } : new[] { "1" }, true);
        }

        [ChatCommand("up")]
        void UpCommand(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, args, true);
        }

        [ChatCommand("down3")]
        void DownCommand3(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, args.Length > 0 ? new[] { "3", args[0] } : new[] { "3" }, false);
        }

        [ChatCommand("down2")]
        void DownCommand2(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, args.Length > 0 ? new[] { "2", args[0] } : new[] { "2" }, false);
        }

        [ChatCommand("down1")]
        void DownCommand1(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, args.Length > 0 ? new[] { "1", args[0] } : new[] { "1" }, false);
        }

        [ChatCommand("down0")]
        void DownCommand0(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, args.Length > 0 ? new[] { "0", args[0] } : new []{"0"}, false);
        }

        [ChatCommand("down")]
        void DownCommand(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, args, false);
        }

        void ChangeBuildingGrade(BasePlayer player, string[] args, bool increment)
        {
            if (!IsAllowed(player))
            {
                PrintMessage(player, "NotAllowed");
                return;
            }

            if (runningPlayers.Contains(player.userID))
            {
                PrintMessage(player, "AlreadyRunning");
                return;
            }

            if (!player.IsAdmin() && runningPlayers.Count > 0)
            {
                PrintMessage(player, "AnotherProcess");
                return;
            }

            var targetGrade = -1;
            var filter = false;
            HashSet<uint> prefabs = null;
            if (args.Length > 0)
            {
                try
                {
                    targetGrade = (int)(BuildingGrade.Enum)Enum.Parse(typeof(BuildingGrade.Enum), args[0], true);
                }
                catch (Exception)
                {
                    PrintMessage(player, "UnknownGrade");
                    return;
                }
                if (args.Length > 1)
                {
                    if (!categories.TryGetValue(args[1], out prefabs))
                    {
                        PrintMessage(player, "UnknownCategory");
                        return;
                    }
                    filter = prefabs.Count > 0;
                }
            }

            var stack = GetTargetBuildingBlock(player);
            if (stack == null || stack.Count == 0)
            {
                PrintMessage(player, "NotLookingAt");
                return;
            }

            var all_blocks = new HashSet<BuildingBlock>();

            //var started = Interface.Oxide.Now;
            //var done = 0;
            while (stack.Count > 0)
            {
                var building_block = stack.Pop();
                var position = new OBB(building_block.transform, building_block.bounds).ToBounds().center;
                var blocks = Pool.GetList<BuildingBlock>();
                Vis.Entities(position, Distance, blocks, 270532864);
                foreach (var block in blocks)
                {
                    if (!all_blocks.Add(block)) continue;
                    stack.Push(block);
                }
                Pool.FreeList(ref blocks);
                //done++;
            }
            var allowed = player.IsAdmin() || permission.UserHasPermission(player.UserIDString, PermOwner);
            all_blocks.RemoveWhere(b => !allowed && b.OwnerID != player.userID || filter && !prefabs.Contains(b.prefabID));
            //Puts("Time: {0} Size: {1} Done: {2}", Interface.Oxide.Now - started, all_blocks.Count, done);

            if (increment && !player.IsAdmin() && !permission.UserHasPermission(player.UserIDString, PermNoCost))
            {
                var costs = GetCosts(all_blocks, targetGrade);
                if (!CanAffordUpgrade(costs, player)) return;
                PayForUpgrade(costs, player);
            }

            runningPlayers.Add(player.userID);

            NextTick(() => DoUpgrade(all_blocks, targetGrade, increment, player));

            /*foreach (var building_block in all_blocks)
            {
                var target_grade = NextBlockGrade(building_block, grade, increment ? 1 : -1);
                if (!CanUpgrade(building_block, (BuildingGrade.Enum) target_grade)) continue;

                building_block.SetGrade((BuildingGrade.Enum)target_grade);
                building_block.SetHealthToMax();
                building_block.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                building_block.UpdateSkin();
            }
            PrintMessage(player, increment ? "FinishedUp" : "FinishedDown");
            runningPlayers.Remove(player.userID);
            */
        }

        private void DoUpgrade(HashSet<BuildingBlock> all_blocks, int targetGrade, bool increment, BasePlayer player)
        {
            var todo = all_blocks.Take(configData.BatchSize).ToArray();
            foreach (var building_block in todo)
            {
                all_blocks.Remove(building_block);
                var target_grade = NextBlockGrade(building_block, targetGrade, increment ? 1 : -1);
                if (!CanUpgrade(building_block, (BuildingGrade.Enum)target_grade)) continue;

                building_block.SetGrade((BuildingGrade.Enum)target_grade);
                building_block.SetHealthToMax();
                building_block.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                building_block.UpdateSkin();
            }
            if (all_blocks.Count > 0)
                NextTick(() => DoUpgrade(all_blocks, targetGrade, increment, player));
            else
            {
                PrintMessage(player, increment ? "FinishedUp" : "FinishedDown");
                runningPlayers.Remove(player.userID);
            }
        }

        private Dictionary<int, float> GetCosts(HashSet<BuildingBlock> blocks, int targetGrade)
        {
            Dictionary<int, float> costs = new Dictionary<int, float>();
            var toRemove = new HashSet<BuildingBlock>();
            foreach (var block in blocks)
            {
                var grade = NextBlockGrade(block, targetGrade, 1);
                if (!CanUpgrade(block, (BuildingGrade.Enum) grade))
                {
                    toRemove.Add(block);
                    continue;
                }
                var costToBuild = block.blockDefinition.grades[grade].costToBuild;
                foreach (var itemAmount in costToBuild)
                {
                    if (!costs.ContainsKey(itemAmount.itemid))
                        costs[itemAmount.itemid] = itemAmount.amount;
                    else
                        costs[itemAmount.itemid] += itemAmount.amount;
                }
            }
            foreach (var block in toRemove)
                blocks.Remove(block);
            return costs;
        }

        private bool CanAffordUpgrade(Dictionary<int, float> costs, BasePlayer player)
        {
            foreach (var current in costs)
            {
                var amount = player.inventory.GetAmount(current.Key);
                if (amount >= current.Value)
                    continue;
                PrintMessage(player, "NotEnoughItems", ItemManager.FindItemDefinition(current.Key).displayName.english, (int)current.Value, amount);
                return false;
            }
            return true;
        }

        private void PayForUpgrade(Dictionary<int, float> costs, BasePlayer player)
        {
            var items = new List<Item>();
            foreach (var current in costs)
            {
                player.inventory.Take(items, current.Key, (int)current.Value);
                player.Command(string.Concat("note.inv ", current.Key, " ", current.Value * -1f));
            }
            foreach (var item in items)
                item.Remove(0f);
        }

        static bool CanUpgrade(BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (block.isDestroyed || grade == block.grade)
                return false;
            if ((int) grade > block.blockDefinition.grades.Length)
                return false;
            if (grade < BuildingGrade.Enum.Twigs)
                return false;
            return true;
        }

        static int NextBlockGrade(BuildingBlock building_block, int targetGrade, int offset)
        {
            var grade = (int)building_block.grade;

            var grades = building_block.blockDefinition.grades;
            if (grades == null) return grade;

            if (offset > 0 && targetGrade >= 0 && targetGrade < grades.Length && grades[targetGrade] != null)
                return grade >= targetGrade ? grade : targetGrade;
            if (offset < 0 && targetGrade >= 0 && targetGrade < grades.Length && grades[targetGrade] != null)
                return grade <= targetGrade ? grade : targetGrade;

            targetGrade = grade + offset;
            while (targetGrade >= 0 && targetGrade < grades.Length)
            {
                if (grades[targetGrade] != null) return targetGrade;
                targetGrade += offset;
            }

            return grade;
        }

        Stack<BuildingBlock> GetTargetBuildingBlock(BasePlayer player)
        {
            var input = serverInputField?.GetValue(player) as InputState;
            if (input == null) return null;
            var direction = Quaternion.Euler(input.current.aimAngles);
            var stack = new Stack<BuildingBlock>();
            RaycastHit initial_hit;
            if (!Physics.Raycast(new Ray(player.transform.position + new Vector3(0f, 1.5f, 0f), direction * Vector3.forward), out initial_hit, 150f) || initial_hit.collider is TerrainCollider)
                return stack;
            var entity = initial_hit.collider.GetComponentInParent<BuildingBlock>();
            if (entity != null) stack.Push(entity);
            else
            {
                var batch = initial_hit.collider?.GetComponent<MeshColliderBatch>();
                if (batch == null) return stack;
                var colliders = (ListDictionary<Component, ColliderCombineInstance>)instancesField.GetValue(batch);
                if (colliders == null) return stack;
                foreach (var instance in colliders.Values)
                {
                    entity = instance.collider?.GetComponentInParent<BuildingBlock>();
                    if (entity == null) continue;
                    stack.Push(entity);
                }
            }
            return stack;
        }

        bool IsAllowed(BasePlayer player)
        {
            return player != null && (player.IsAdmin() || permission.UserHasPermission(player.UserIDString, Perm));
        }

        private void PrintMessage(BasePlayer player, string msgId, params object[] args)
        {
            PrintToChat(player, lang.GetMessage(msgId, this, player.UserIDString), args);
        }
    }
}
