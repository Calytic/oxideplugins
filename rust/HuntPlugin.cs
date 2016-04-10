using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using UnityEngine;

using Hunt.RPG;
using Hunt.RPG.Keys;

using Random = UnityEngine.Random;
using Time = UnityEngine.Time;
using Timer = Oxide.Plugins.Timer;

namespace Oxide.Plugins
{

    [Info("Hunt RPG", "PedraozauM / SW / Nogrod", "1.5.5", ResourceId = 841)]
    public class HuntPlugin : RustPlugin
    {
        [PluginReference]
        private Plugin Pets;
        [PluginReference]
        private Plugin EventManager;
        private bool initialized;
        private bool updateConfig;
        private bool updatePlayerData;
        private readonly DynamicConfigFile huntDataFile;
        private VersionNumber DataVersion;

        private HuntData Data;
        private string ChatPrefix;
        private ulong[] Trainer;
        private Dictionary<HRK, Skill> SkillTable;
        private Dictionary<ResourceDispenser.GatherType, float> ExpRateTable;
        private Dictionary<int, string> TameTable;
        private Dictionary<string, ItemInfo> ItemTable;
        private Dictionary<string, int> ResearchTable;
        private Dictionary<BuildingGrade.Enum, float> UpgradeBuildingTable;
        private string[] AllowedEntites;
        private bool AdminReset;
        private bool ShowHud;
        private bool ShowProfile;
        private uint DefaultHud;
        private float NightXP;
        private float DeleteProfileAfter;
        private float DeathReducer;
        private Dictionary<string, string> itemShortname;

        private readonly Dictionary<ulong, float> PlayerLastPercentChange;
        private readonly Dictionary<ulong, Dictionary<HRK, float>> SkillsCooldowns;
        private readonly Dictionary<ulong, GUIInfo> GUIInfo;
        //private readonly Random randomGenerator;
        private readonly int playersMask = LayerMask.GetMask("Player (Server)");
        private readonly int triggerMask = LayerMask.GetMask("Trigger");

        public HuntPlugin()
        {
            DataVersion = new VersionNumber(0,9,3);

            GUIInfo = new Dictionary<ulong, GUIInfo>();
            PlayerLastPercentChange = new Dictionary<ulong, float>();
            SkillsCooldowns = new Dictionary<ulong, Dictionary<HRK, float>>();
            huntDataFile = Interface.Oxide.DataFileSystem.GetFile(HK.DataFileName);
        }

        #region Hooks

        void OnServerInitialized()
        {
            if (!initialized) OnTerrainInitialized();
            if (ItemTable == null)
            {
                DefaultItems();
                ItemTable = ReadFromConfig<Dictionary<string, ItemInfo>>(HK.ItemTable);
            }
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                GUIInfo guiInfo;
                if (!GUIInfo.TryGetValue(player.userID, out guiInfo)) continue;
                DestroyUi(player, guiInfo.LastHud);
                DestroyUi(player, guiInfo.LastMain);
            }
            SaveRpg();
            RPGInfo.OnUnload();
        }

        void OnTerrainInitialized()
        {
            initialized = true;
            var configVersion = new VersionNumber();
            if (Config[HK.ConfigVersion] != null)
                configVersion = ReadFromConfig<VersionNumber>(HK.ConfigVersion);
            var dataVersion = new VersionNumber();
            if (Config[HK.DataVersion] != null)
                dataVersion = ReadFromConfig<VersionNumber>(HK.DataVersion);
            var needDataUpdate = !DataVersion.Equals(dataVersion);
            var needConfigUpdate = !Version.Equals(configVersion);
            if (needConfigUpdate)
            {
                Puts("Your config needs updating...");
                DefaultConfig();
            }
            UpdateLang();
            LoadRpg(dataVersion);
            if (!needDataUpdate)
            {
                PrintToChat(_(HMK.Loaded));
                return;
            }
            updatePlayerData = true;
            UpdateData();
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (updatePlayerData) ChatMessage(player, HMK.DataUpdated);
            var rpgInfo = FindRpgInfo(player);
            if (rpgInfo.Preferences.ShowProfile) ChatMessage(player, Profile(rpgInfo, player));
            var steamId = player.userID;
            if (!PlayerLastPercentChange.ContainsKey(steamId))
                PlayerLastPercentChange.Add(steamId, CurrentPercent(FindRpgInfo(player)));
            UpdateEffectsPlayer(player, rpgInfo);
            if (!ShowHud) return;
            timer.Once(1, () => GuiInit(player));
        }

        object OnEntityTakeDamage(MonoBehaviour entity, HitInfo hitInfo)
        {
            var player = entity as BasePlayer;
            if (player == null) return null;
            if (!OnAttackedInternal(player, hitInfo)) return null;
            hitInfo = new HitInfo();
            return hitInfo;
        }

        object OnPlayerAttack(BasePlayer player, HitInfo hitInfo)
        {
            if (player == null || hitInfo?.Weapon?.GetItem() == null || !player.CanBuild()) return null;
            Skill skill;
            if (SkillTable.TryGetValue(HRK.Blinkarrow, out skill) && !skill.Enabled)
                return null;
            if (!hitInfo.Weapon.GetOwnerItemDefinition().shortname.Equals("bow.hunting"))
                return null;
            var rpgInfo = FindRpgInfo(player);
            int skillPoints;
            if (!rpgInfo.Skills.TryGetValue(HRK.Blinkarrow, out skillPoints))
            {
                ChatMessage(player, HMK.NotLearnedSkill);
                return null;
            }
            var playerCooldowns = PlayerCooldowns(player.userID);
            float availableAt = 0;
            var time = Time.realtimeSinceStartup;
            var isReady = /*player.IsAdmin() || */IsSkillReady(playerCooldowns, ref availableAt, time, HRK.Blinkarrow);
            if (isReady)
            {
                if (rpgInfo.Preferences.AutoToggleBlinkArrow)
                    rpgInfo.Preferences.UseBlinkArrow = true;
                if (!rpgInfo.Preferences.UseBlinkArrow) return null;
                var newPos = GetGround(hitInfo.HitPositionWorld);
                if (!IsBuildingAllowed(newPos, player))
                {
                    ChatMessage(player, HMK.CantBlinkOther);
                    return null;
                }
                TeleportPlayerTo(player, newPos);
                SetCooldown(skillPoints, time, playerCooldowns, HRK.Blinkarrow);
                return true;
            }
            if (!rpgInfo.Preferences.UseBlinkArrow) return null;
            ChatMessage(player, HMK.BlinkedRecently, TimeLeft(availableAt, time));
            if (rpgInfo.Preferences.AutoToggleBlinkArrow)
                rpgInfo.Preferences.UseBlinkArrow = false;
            return null;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            var oven = entity as BaseOven;
            if (oven != null) Data.Furnaces.Remove(EntityId(oven));
            var quarry = entity as MiningQuarry;
            if (quarry != null) Data.Quarries.Remove(EntityId(quarry));
            var player = entity as BasePlayer;
            if (player == null) return;
            if (EventManager != null && (bool)EventManager.CallHook("isPlaying", player)) return;
            FindRpgInfo(player).Died(DeathReducer);
            ChatMessage(player, HMK.Died, DeathReducer);
        }

        object OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        {
            if (!ItemTable.ContainsKey(task.blueprint.targetItem.shortname))
                return null;

            var rpgInfo = FindRpgInfo(crafter);
            var craftingTime = task.blueprint.time;
            var amountToReduce = craftingTime * rpgInfo.GetCraftingReducer();
            craftingTime -= amountToReduce;
            if (!task.blueprint.name.Contains("(Clone)"))
                task.blueprint = UnityEngine.Object.Instantiate(task.blueprint);
            task.blueprint.time = craftingTime;
            if (rpgInfo.Preferences.ShowCraftMessage)
                ChatMessage(crafter, HMK.CraftingEnd, craftingTime, amountToReduce);
            return null;
        }

        object OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (task.amount > 0) return null;
            if (task.blueprint != null && task.blueprint.name.Contains("(Clone)"))
            {
                var behaviours = task.blueprint.GetComponents<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour.name.Contains("(Clone)")) UnityEngine.Object.Destroy(behaviour);
                }
                task.blueprint = null;
            }
            /*var blueprints = UnityEngine.Object.FindObjectsOfType<ItemBlueprint>();
            if (blueprints.Length > 0)
            {
                foreach (var blueprint in blueprints)
                {
                    var behaviours = blueprint.GetComponents<MonoBehaviour>();
                    foreach (var behaviour in behaviours)
                    {
                        if (behaviour.name.Contains("(Clone)")) UnityEngine.Object.Destroy(behaviour);
                    }
                }
            }
            var items = UnityEngine.Object.FindObjectsOfType<ItemDefinition>();
            if (items.Length > 0)
            {
                foreach (var itemDef in items)
                {
                    var behaviours = itemDef.GetComponents<MonoBehaviour>();
                    foreach (var behaviour in behaviours)
                    {
                        if (behaviour.name.Contains("(Clone)")) UnityEngine.Object.Destroy(behaviour);
                    }
                }
            }*/
            return null;
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var player = entity.ToPlayer();
            if (player == null) return;
            var rpgInfo = FindRpgInfo(player);
            if (rpgInfo == null) return;
            var gatherType = dispenser.gatherType;
            HRK skillType;
            switch (gatherType)
            {
                case ResourceDispenser.GatherType.Tree:
                    skillType = HRK.Lumberjack;
                    break;
                case ResourceDispenser.GatherType.Ore:
                    skillType = HRK.Miner;
                    break;
                case ResourceDispenser.GatherType.Flesh:
                    skillType = HRK.Hunter;
                    break;
                default:
                    ExpGain(rpgInfo, item.amount, player);
                    return;
            }
            int skillPoints;
            if (rpgInfo.Skills.TryGetValue(skillType, out skillPoints))
                item.amount = GatherModifierInt(skillPoints, skillType, item.amount);
            ExpGain(rpgInfo, (int)Math.Ceiling(item.amount * ExpRateTable[gatherType]), player);
        }

        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (quarry == null) return;
            var instanceId = EntityId(quarry);
            ulong playerQuarry;
            if (!Data.Quarries.TryGetValue(instanceId, out playerQuarry))
                return;
            var player = FindPlayer(playerQuarry);
            if (player == null) return;
            var rpgInfo = FindRpgInfo(player);
            if (rpgInfo == null)
                return;
            int skillPoints;
            if (rpgInfo.Skills.TryGetValue(HRK.Miner, out skillPoints))
                item.amount = GatherModifierInt(skillPoints, HRK.Miner, item.amount);
            ExpGain(rpgInfo, (int)Math.Ceiling(item.amount * ExpRateTable[ResourceDispenser.GatherType.Ore]), player);
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            var rpgInfo = FindRpgInfo(player);
            if (rpgInfo == null)
                return;
            int skillPoints;
            if (rpgInfo.Skills.TryGetValue(HRK.Gatherer, out skillPoints))
                item.amount = GatherModifierInt(skillPoints, HRK.Gatherer, item.amount);
            ExpGain(rpgInfo, (int)Math.Ceiling(item.amount * ExpRateTable[ResourceDispenser.GatherType.Ore] * 2), player);
        }

        void OnItemDeployed(Deployer deployer, BaseEntity baseEntity)
        {
            OnEntityDeployedInternal(deployer.ownerPlayer, baseEntity as BaseOven, Data.Furnaces);
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container.playerOwner == null) return;
            var rpgInfo = FindRpgInfo(container.playerOwner);
            if (rpgInfo == null) return;
            UpdateMagazin(item, rpgInfo);
            UpdateGather(item, rpgInfo);
        }

        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            var entity = gameObject.GetComponent<BaseEntity>();
            OnEntityDeployedInternal(planner.ownerPlayer, entity as BaseOven, Data.Furnaces);
            OnEntityDeployedInternal(planner.ownerPlayer, entity as MiningQuarry, Data.Quarries);
            var buildingBlock = entity as BuildingBlock;
            if (buildingBlock != null) OnStructureUpgrade(buildingBlock, planner.ownerPlayer, buildingBlock.grade);
        }

        void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven == null) return;
            var instanceId = EntityId(oven);
            ulong playerFurnace;
            if (!Data.Furnaces.TryGetValue(instanceId, out playerFurnace))
                return;
            var player = FindPlayer(playerFurnace);
            var rpgInfo = player == null ? FindRpgInfo(playerFurnace) : FindRpgInfo(player);
            if (rpgInfo == null)
                return;
            int skillLevel;
            if (!rpgInfo.Skills.TryGetValue(HRK.Blacksmith, out skillLevel))
                return;
            var skill = SkillTable[HRK.Blacksmith];
            var skillChance = skillLevel * skill.Modifiers[HRK.Chance].Args[0];
            if (Random.Range(0f, 1f) > skillChance)
                return;
            var rate = skillLevel / (float)skill.MaxLevel * skill.Modifiers[HRK.RessRate].Args[0];
            var items = oven.inventory.itemList.ToArray();
            foreach (var item in items)
            {
                var itemModCookable = item.info.GetComponent<ItemModCookable>();
                if (itemModCookable?.becomeOnCooked == null || item.temperature < itemModCookable.lowTemp || item.temperature > itemModCookable.highTemp || itemModCookable.cookTime < 0) continue;
                if (oven.inventory.Take(null, item.info.itemid, 1) != 1) continue;
                var itemToGive = ItemManager.Create(itemModCookable.becomeOnCooked, (int)Math.Ceiling(itemModCookable.amountOfBecome * rate));
                if (!itemToGive.MoveToContainer(oven.inventory))
                    itemToGive.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity);
            }
        }

        object OnStructureUpgrade(BuildingBlock buildingBlock, BasePlayer player, BuildingGrade.Enum grade)
        {
            NextTick(() =>
            {
                if (buildingBlock.grade != grade) return;
                var items = buildingBlock.blockDefinition.grades[(int) grade].costToBuild;
                var total = 0;
                foreach (var item in items)
                    total += (int) item.amount;
                var experience = (int) Math.Ceiling(UpgradeBuildingTable[grade]*total);
                ExpGain(FindRpgInfo(player), experience, player);
            });
            return null;
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            GUIInfo.Remove(player.userID);
        }

        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (!Trainer.Contains(npc.userID)) return;
            NpcGui(player);
        }

        void OnServerSave()
        {
            SaveRpg();
        }
        #region Internal
        private bool OnAttackedInternal(BasePlayer player, HitInfo hitInfo)
        {
            var basePlayer = hitInfo.Initiator as BasePlayer;
            if (!(hitInfo.Initiator is BaseNPC || basePlayer != null && player.userID != basePlayer.userID)) return false;
            var rpgInfo = FindRpgInfo(player);
            if (Random.Range(0f, 1f) <= rpgInfo.GetEvasion())
            {
                ChatMessage(player, HMK.Dodged);
                return true;
            }
            hitInfo.damageTypes.ScaleAll(1 - rpgInfo.GetBlock());
            return false;
        }

        private void OnEntityDeployedInternal(BasePlayer player, BaseEntity entity, IDictionary<string, ulong> data)
        {
            if (player == null || entity == null) return;
            if (!AllowedEntites.Contains(EntityName(entity), StringComparer.OrdinalIgnoreCase)) return;
            var instanceId = EntityId(entity);
            if (data.ContainsKey(instanceId))
            {
                ChatMessage(player, HMK.IdAlreadyExists, instanceId);
                return;
            }
            data.Add(instanceId, player.userID);
        }
        #endregion
        #endregion

        #region Commands
        [ChatCommand("h")]
        void cmdHuntShortcut(BasePlayer player, string command, string[] args)
        {
            HandleChatCommand(player, args);
        }

        [ChatCommand("hunt")]
        void cmdHunt(BasePlayer player, string command, string[] args)
        {
            HandleChatCommand(player, args);
        }

        [ChatCommand("hgui")]
        void cmdHuntGui(BasePlayer player, string command, string[] args)
        {
            if (Trainer.Length > 0 && !player.IsAdmin()) return;
            ProfileGui(player);
        }

        [ConsoleCommand("hunt.cmd")]
        private void cmdCmd(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            GUIInfo guiInfo;
            if (player == null || !GUIInfo.TryGetValue(player.userID, out guiInfo))
                return;
            if (Trainer.Length > 0 && !IsNPCInRange(player.transform.position) && !IsAdmin(arg))
            {
                DestroyUi(player, guiInfo.LastMain);
                return;
            }
            HandleChatCommand(player, arg.Args, true);
            if (Trainer.Length > 0) NpcGui(player, true);
            else ProfileGui(player, true);
        }

        [ConsoleCommand("hunt.saverpg")]
        private void cmdSaveRPG(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;
            SaveRpg();
        }

        [ConsoleCommand("hunt.resetrpg")]
        private void cmdResetRPG(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;
            foreach (var rpgInfoPair in Data.Profiles)
                rpgInfoPair.Value.ResetSkills();
            Data.Profiles.Clear();
            Data.Furnaces.Clear();
            Data.Quarries.Clear();
            SaveRpg();
        }

        [ConsoleCommand("hunt.lvlup")]
        private void cmdLevelUp(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg) || !arg.HasArgs()) return;
            var player = arg.Player();
            int desiredLevel;
            if (arg.HasArgs(2)) {
                player = FindPlayer(arg.GetString(0));
                desiredLevel = arg.GetInt(1);
            } else
                desiredLevel = arg.GetInt(0);
            if (player == null)
            {
                arg.ReplyWith(_(HMK.NotFoundPlayer, arg.Player()));
                return;
            }
            var rpgInfo = FindRpgInfo(player);
            if (desiredLevel == 0 || desiredLevel <= rpgInfo.Level) return;
            rpgInfo.LevelUp(desiredLevel);
            NotifyLevelUp(player, rpgInfo);
            arg.ReplyWith(_(HMK.PlayerLevelUp, arg.Player(), player.displayName, desiredLevel));
        }

        [ConsoleCommand("hunt.lvlreset")]
        private void cmdLvlReset(ConsoleSystem.Arg arg)
        {
            if (AdminReset && !IsAdmin(arg)) return;
            var player = arg.Player();
            if (arg.HasArgs() && IsAdmin(arg))
                player = FindPlayer(arg.GetString(0));
            var rpgInfo = player == null ? FindRpgInfo(Convert.ToUInt64(arg.GetString(0))) : FindRpgInfo(player);
            if (rpgInfo == null)
            {
                arg.ReplyWith(_(HMK.NotFoundPlayer, arg.Player()));
                return;
            }
            rpgInfo.Reset();
            if (player != null) UpdateEffectsPlayer(player, rpgInfo);
            arg.ReplyWith(_(HMK.PlayerLevelUp, arg.Player(), player?.displayName, 0));
        }

        [ConsoleCommand("hunt.statreset")]
        private void cmdStatReset(ConsoleSystem.Arg arg)
        {
            if (AdminReset && !IsAdmin(arg)) return;
            var player = arg.Player();
            if (arg.HasArgs() && IsAdmin(arg))
            {
                var target = arg.GetString(0);
                if (target.Equals("*"))
                {
                    foreach (var rpgInfoPair in Data.Profiles)
                    {
                        rpgInfoPair.Value.ResetStats();
                        player = FindPlayer(rpgInfoPair.Key);
                        if (player != null) UpdateEffectsPlayer(player, rpgInfoPair.Value);
                    }
                    SaveRpg();
                    arg.ReplyWith(_(HMK.StatResetPlayer, arg.Player(), "All players"));
                    return;
                }
                player = FindPlayer(target);
            }
            var rpgInfo = player == null ? FindRpgInfo(Convert.ToUInt64(arg.GetString(0))) : FindRpgInfo(player);
            if (rpgInfo == null)
            {
                arg.ReplyWith(_(HMK.NotFoundPlayer, arg.Player()));
                return;
            }
            rpgInfo.ResetStats();
            if (player != null) UpdateEffectsPlayer(player, rpgInfo);
            arg.ReplyWith(player == arg.Player() ? _(HMK.StatReset, arg.Player()) : _(HMK.StatResetPlayer, arg.Player(), player?.displayName));
        }

        [ConsoleCommand("hunt.skillreset")]
        private void cmdSkillReset(ConsoleSystem.Arg arg)
        {
            if (AdminReset && !IsAdmin(arg)) return;
            var player = arg.Player();
            if (arg.HasArgs() && IsAdmin(arg))
            {
                var target = arg.GetString(0);
                if (target.Equals("*"))
                {
                    foreach (var rpgInfoPair in Data.Profiles)
                    {
                        rpgInfoPair.Value.ResetSkills();
                        player = FindPlayer(rpgInfoPair.Key);
                        if (player != null) UpdateGatherPlayer(player, rpgInfoPair.Value);
                    }
                    SaveRpg();
                    arg.ReplyWith(_(HMK.SkillResetPlayer, arg.Player(), "All players"));
                    return;
                }
                player = FindPlayer(target);
            }
            var rpgInfo = player == null ? FindRpgInfo(Convert.ToUInt64(arg.GetString(0))) : FindRpgInfo(player);
            if (rpgInfo == null)
            {
                arg.ReplyWith(_(HMK.NotFoundPlayer, arg.Player()));
                return;
            }
            rpgInfo.ResetSkills();
            if (player != null) UpdateGatherPlayer(player, rpgInfo);
            arg.ReplyWith(player == arg.Player() ? _(HMK.SkillReset, arg.Player()) : _(HMK.SkillResetPlayer, arg.Player(), player?.displayName));
        }

        [ConsoleCommand("hunt.genxptable")]
        private void cmdGenerateXPTable(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;
            arg.ReplyWith(_(HMK.GenerateXp, arg.Player()));
            var baseXP = arg.HasArgs() ? arg.GetInt(0) : HKD.BaseXP;
            var levelMultiplier = arg.HasArgs(2) ? arg.GetFloat(1) : HKD.LevelMultiplier;
            var levelModule = arg.HasArgs(3) ? arg.GetInt(2) : HKD.LevelModule;
            var moduleReducer = arg.HasArgs(4) ? arg.GetFloat(3) : HKD.ModuleReducer;
            //TODO merge old levels to new?
            var xpTable = HuntTablesGenerator.GenerateXPTable(HKD.MaxLevel, baseXP, levelMultiplier, levelModule, moduleReducer);
            Config[HK.XPTable] = xpTable;
            RPGInfo.XPTable = xpTable.Values.ToArray();
            SaveConfig();
        }

        private void HandleChatCommand(BasePlayer player, string[] args, bool npc = false)
        {
            if (args.Length == 0)
            {
                ChatMessage(player, HMK.Help);
                return;
            }
            var rpgInfo = FindRpgInfo(player);
            var cmdArg = args[0].ToLower();
            switch (cmdArg)
            {
                case "about":
                    ChatMessage(player, HMK.About);
                    return;
                case "shortcuts":
                    ChatMessage(player, HMK.Shortcuts);
                    return;
                case "p":
                case "profile":
                    ChatMessage(player, Profile(rpgInfo, player));
                    return;
                case "pp":
                case "profilepreferences":
                    ChatMessage(player, HMK.ProfilePreferences);
                    return;
                case "skill":
                    DisplaySkillCommand(player, args);
                    return;
                case "skilllist":
                    ListSkills(player);
                    return;
                case "lvlup":
                    LevelUpChatHandler(player, args, rpgInfo);
                    return;
                case "research":
                    ResearchItemHandler(player, args, rpgInfo);
                    return;
                case "xp":
                    ChatMessage(player, XPProgression(player, rpgInfo));
                    return;
                case "xp%":
                    ChangePlayerXPMessagePreference(player, args, rpgInfo);
                    return;
                case "craftmsg":
                    ToggleCraftMessage(player, rpgInfo);
                    return;
                case "ba":
                    ToggleBlinkArrow(player, rpgInfo);
                    return;
                case "aba":
                    ToggleAutoBlinkArrow(player, rpgInfo);
                    return;
                case "sp":
                    ToggleShowProfile(player, rpgInfo);
                    return;
                case "sh":
                    ToggleShowHud(player, rpgInfo);
                    return;
                case "top":
                    ShowTop(player);
                    return;
            }
            if (Trainer.Length > 0 && !IsNPCInRange(player.transform.position) && !player.IsAdmin())
            {
                ChatMessage(player, HMK.NeedNpc);
                return;
            }
            switch (cmdArg)
            {
                case "lvlreset":
                    if (AdminReset && !player.IsAdmin())
                    {
                        ChatMessage(player, HMK.NotAnAdmin);
                        return;
                    }
                    rpgInfo.Reset();
                    UpdateEffectsPlayer(player, rpgInfo);
                    return;
                case "sts":
                case "statset":
                    SetStatsCommand(player, args, rpgInfo, npc);
                    return;
                case "statreset":
                    if (AdminReset && !player.IsAdmin())
                    {
                        ChatMessage(player, HMK.NotAnAdmin);
                        return;
                    }
                    rpgInfo.ResetStats();
                    UpdateEffectsPlayer(player, rpgInfo);
                    return;
                case "sks":
                case "skillset":
                    SetSkillsCommand(player, args, rpgInfo, npc);
                    return;
                case "skillreset":
                    if (AdminReset && !player.IsAdmin())
                    {
                        ChatMessage(player, HMK.NotAnAdmin);
                        return;
                    }
                    rpgInfo.ResetSkills();
                    UpdateGatherPlayer(player, rpgInfo);
                    return;
                default:
                    ChatMessage(player, HMK.InvalidCommand, args[0]);
                    return;
            }
        }

        private void ListSkills(BasePlayer player)
        {
            var sb = new StringBuilder();
            foreach (var skill in SkillTable.Values)
            {
                if (!skill.Enabled) continue;
                sb.Clear();
                SkillInfo(player, sb, skill, 100);
                ChatMessage(player, sb.ToString());
            }
        }

        private void SkillInfo(BasePlayer player, StringBuilder sb, Skill skill, int cut = 0)
        {
            sb.AppendLine(_(HMK.SkillInfoHeader, player, skill.Name, skill.RequiredLevel));
            for (var i = 0; i < skill.RequiredSkills.Count; i++)
                sb.AppendLine($"Lvl {i + 1}: " + string.Join(" | ", skill.RequiredSkills[i].Select(s => $"{s.Key} Lvl {s.Value}").ToArray()));
            for (var i = 0; i < skill.RequiredStats.Count; i++)
                sb.AppendLine($"Lvl {i + 1}: " + string.Join(" | ", skill.RequiredStats[i].Select(s => $"{s.Key}: {s.Value}").ToArray()));
            var cost = FindRpgInfo(player).GetSkillPointsCostNext(skill);
            if (cost > 1)
                sb.AppendLine(_(HMK.SkillCost, player, cost));

            var description = skill.Description != HMK.None ? _(skill.Description, player) : string.Empty;
            if (cut > 0)
                sb.Append(description.Length > cut ? $"{description.Substring(0, cut)}..." : description);
            else
                sb.Append(description);
            if (cut <= 0 && skill.Usage != HMK.None)
            {
                sb.AppendLine();
                sb.Append(_(HMK.Usage, player, _(skill.Usage, player)));
            }
        }

        private void ShowTop(BasePlayer player)
        {
            var players = Data.Profiles.Values.ToArray();
            Array.Sort(players, (a, b) =>
            {
                if (a.Level != b.Level) return a.Level > b.Level ? -1 : 1;
                if (a.Experience == b.Experience) return 0;
                return a.Experience > b.Experience ? -1 : 1;
            });
            for (var i = 0; i < 10; i++)
                ChatMessage(player, HMK.TopPlayer, i + 1, players[i].SteamName, players[i].Level);
        }

        private void ToggleAutoBlinkArrow(BasePlayer player, RPGInfo rpgInfo)
        {
            rpgInfo.Preferences.AutoToggleBlinkArrow = !rpgInfo.Preferences.AutoToggleBlinkArrow;
            var toggleBlinkArrowStatus = rpgInfo.Preferences.AutoToggleBlinkArrow ? _(HMK.On, player) : _(HMK.Off, player);
            ChatMessage(player, HMK.BlinkToggle, toggleBlinkArrowStatus);
        }

        private void ToggleBlinkArrow(BasePlayer player, RPGInfo rpgInfo)
        {
            rpgInfo.Preferences.UseBlinkArrow = !rpgInfo.Preferences.UseBlinkArrow;
            var blinkArrowStatus = rpgInfo.Preferences.UseBlinkArrow ? _(HMK.On, player) : _(HMK.Off, player);
            ChatMessage(player, HMK.BlinkStatus, blinkArrowStatus);
        }

        private void ToggleCraftMessage(BasePlayer player, RPGInfo rpgInfo)
        {
            rpgInfo.Preferences.ShowCraftMessage = !rpgInfo.Preferences.ShowCraftMessage;
            var craftMessageStatus = rpgInfo.Preferences.ShowCraftMessage ? _(HMK.On, player) : _(HMK.Off, player);
            ChatMessage(player, HMK.CraftMessage, craftMessageStatus);
        }

        private void ToggleShowProfile(BasePlayer player, RPGInfo rpgInfo)
        {
            rpgInfo.Preferences.ShowProfile = !rpgInfo.Preferences.ShowProfile;
            var showProfileStatus = rpgInfo.Preferences.ShowProfile ? _(HMK.On, player) : _(HMK.Off, player);
            ChatMessage(player, HMK.ProfileMessage, showProfileStatus);
        }

        private void ToggleShowHud(BasePlayer player, RPGInfo rpgInfo)
        {
            rpgInfo.Preferences.ShowHud++;
            if (rpgInfo.Preferences.ShowHud > 2)
                rpgInfo.Preferences.ShowHud = 0;
            if (rpgInfo.Preferences.ShowHud > 0)
                UpdateHud(player);
            else
            {
                GUIInfo guiInfo;
                if (GUIInfo.TryGetValue(player.userID, out guiInfo))
                    DestroyUi(player, guiInfo.LastHud);
            }
        }

        private void DisplaySkillCommand(BasePlayer player, string[] args)
        {
            var commandArgs = args?.Length - 1 ?? 0;
            if (commandArgs != 1)
            {
                ChatMessage(player, HMK.InvalidCommand, args[0]);
                return;
            }
            HRK skillType;
            try
            {
                skillType = (HRK)Enum.Parse(typeof(HRK), args[1], true);
            }
            catch (Exception)
            {
                ChatMessage(player, HMK.InvalidSkillName);
                return;
            }
            Skill skill;
            if (!SkillTable.TryGetValue(skillType, out skill) || !skill.Enabled)
            {
                ChatMessage(player, HMK.InvalidSkillName);
                return;
            }
            var sb = new StringBuilder();
            SkillInfo(player, sb, skill);
            ChatMessage(player, sb.ToString());
        }

        private void SetSkillsCommand(BasePlayer player, string[] args, RPGInfo rpgInfo, bool npc = false)
        {
            var commandArgs = args?.Length - 1 ?? 0;
            if (args == null || commandArgs < 2 || commandArgs % 2 != 0)
            {
                ChatMessage(player, HMK.InvalidCommand, args[0]);
                return;
            }
            var pairs = commandArgs / 2 + 1;
            for (var i = 1; i < pairs; i++)
            {
                var index = i * 2 - 1;
                HRK skillType;
                try
                {
                    skillType = (HRK)Enum.Parse(typeof(HRK), args[index], true);
                }
                catch
                {
                    ChatMessage(player, HMK.InvalidCommand, args[0]);
                    continue;
                }
                int level;
                if (!int.TryParse(args[index + 1], out level))
                {
                    ChatMessage(player, HMK.InvalidCommand, args[0]);
                    continue;
                }

                Skill skill;
                if (SkillTable.TryGetValue(skillType, out skill))
                {
                    if (!skill.Enabled)
                    {
                        ChatMessage(player, HMK.SkillDisabled);
                        continue;
                    }
                    HMK reason;
                    var levelsAdded = rpgInfo.AddSkill(skill, level, out reason, Pets);
                    if (levelsAdded > 0)
                    {
                        if (!npc) ChatMessage(player, HMK.SkillUp, skill.Name, levelsAdded);
                        switch (skill.Type)
                        {
                            case HRK.Lumberjack:
                            case HRK.Miner:
                            case HRK.Hunter:
                                UpdateGatherPlayer(player, rpgInfo);
                                break;
                        }
                    }
                    else
                    {
                        if (reason == HMK.NotEnoughPoints)
                            ChatMessage(player, reason, rpgInfo.GetSkillPointsCostNext(skill, level));
                        else
                            ChatMessage(player, reason);
                        ChatMessage(player, HMK.SkillInfo);
                    }
                }
                else
                    ChatMessage(player, HMK.InvalidSkillName);
            }
            UpdateHud(player, true);
        }

        private void SetStatsCommand(BasePlayer player, string[] args, RPGInfo rpgInfo, bool npc = false)
        {
            var commandArgs = args?.Length - 1 ?? 0;
            if (commandArgs < 2 || commandArgs % 2 != 0)
            {
                ChatMessage(player, HMK.InvalidCommand, args[0]);
                return;
            }
            var pairs = commandArgs / 2 + 1;
            for (var i = 1; i < pairs; i++)
            {
                var index = i * 2 - 1;
                int points;
                if (!int.TryParse(args[index + 1], out points))
                {
                    ChatMessage(player, HMK.InvalidCommand, args[index + 1]);
                    continue;
                }
                HRK statType;
                try
                {
                    statType = (HRK)Enum.Parse(typeof(HRK), args[index], true);
                }
                catch
                {
                    ChatMessage(player, HMK.InvalidCommand, args[index]);
                    continue;
                }

                switch (statType)
                {
                    case HRK.Agi:
                    case HRK.Int:
                    case HRK.Str:
                        if (rpgInfo.AddStat(statType, points))
                        {
                            if (!npc) ChatMessage(player, (HMK)Enum.Parse(typeof(HMK), statType+"Color", true), $"+{points}");
                        }
                        else
                            ChatMessage(player, HMK.NotEnoughPoints, rpgInfo.GetStatPointsCost(statType, points));
                        break;
                    default:
                        ChatMessage(player, HMK.InvalidCommand, args[index]);
                        break;
                }
            }
            UpdateHud(player, true);
        }

        private void LevelUpChatHandler(BasePlayer player, string[] args, RPGInfo rpgInfo)
        {
            if (!player.IsAdmin()) return;
            var commandArgs = args?.Length - 1 ?? 0;
            if (args == null || commandArgs > 2 || commandArgs < 1)
            {
                ChatMessage(player, HMK.InvalidCommand, args[0]);
                return;
            }
            var callerPlayer = player;
            var levelIndex = 1;
            if (commandArgs == 2)
            {
                levelIndex = 2;
                player = FindPlayer(args[1].ToLower());
                if (player == null)
                {
                    ChatMessage(callerPlayer, HMK.NotFoundPlayer);
                    return;
                }
                rpgInfo = FindRpgInfo(player);
            }
            int desiredLevel;
            if (!int.TryParse(args[levelIndex], out desiredLevel))
            {
                ChatMessage(callerPlayer, HMK.InvalidCommand, args[0]);
                return;
            }
            if (desiredLevel <= rpgInfo.Level) return;
            rpgInfo.LevelUp(desiredLevel);
            NotifyLevelUp(player, rpgInfo);
            if (callerPlayer != player)
                ChatMessage(callerPlayer, HMK.PlayerLevelUp, player.displayName, desiredLevel);
        }

        private bool ResearchItemHandler(BasePlayer player, string[] args, RPGInfo rpgInfo)
        {
            var commandArgs = args?.Length - 1 ?? 0;
            if (commandArgs != 1)
            {
                ChatMessage(player, HMK.InvalidCommand, args[0]);
                return false;
            }
            int skillPoints;
            if (!rpgInfo.Skills.TryGetValue(HRK.Researcher, out skillPoints))
            {
                ChatMessage(player, HMK.NotLearnedSkill);
                return false;
            }
            var argItem = args[1].ToLower();
            string itemname;
            if (!itemShortname.TryGetValue(argItem, out itemname))
                itemname = argItem;
            ItemInfo itemInfo;
            if (!ItemTable.TryGetValue(itemname, out itemInfo))
            {
                ChatMessage(player, HMK.NotFoundItem, argItem);
                return false;
            }
            if (!itemInfo.CanResearch)
            {
                ChatMessage(player, HMK.ResearchBlocked, itemInfo.DisplayName);
                return false;
            }
            var definition = ItemManager.FindItemDefinition(itemname);
            if (definition == null)
            {
                ChatMessage(player, HMK.NotFoundItem, itemInfo.DisplayName);
                return false;
            }
            var hasItem = player.inventory.FindItemID(itemname);
            if (hasItem == null)
            {
                ChatMessage(player, HMK.ResearchItem);
                return false;
            }
            int requiredSkillPoints;
            if (!ResearchTable.TryGetValue(itemInfo.ItemCategory, out requiredSkillPoints))
            {
                ChatMessage(player, HMK.ResearchType);
                return false;
            }
            if (skillPoints < requiredSkillPoints)
            {
                ChatMessage(player, HMK.ResearchSkill, requiredSkillPoints);
                return false;
            }

            var time = Time.realtimeSinceStartup;
            var playerCooldowns = PlayerCooldowns(player.userID);
            float availableAt = 0;
            if (IsSkillReady(playerCooldowns, ref availableAt, time, HRK.Researcher))
            {
                if (Random.Range(0f, 1f) > SkillTable[HRK.Researcher].Modifiers[HRK.Chance].Args[0])
                {
                    ChatMessage(player, HMK.ResearchSuccess, itemInfo.DisplayName);
                    player.inventory.GiveItem(ItemManager.Create(definition, 1, true), player.inventory.containerMain);
                    NoticeArea.ItemPickUp(definition, 1, true);
                }
                else
                {
                    ChatMessage(player, HMK.ResearchFail, itemInfo.DisplayName);
                    player.inventory.Take(new List<Item> { player.inventory.FindItemID(definition.itemid) }, definition.itemid, 1);
                }
                SetCooldown(skillPoints, time, playerCooldowns, HRK.Researcher);
            }
            else
            {
                ChatMessage(player, HMK.ResearchReuse, TimeLeft(availableAt, time));
            }
            return true;
        }

        private void ChangePlayerXPMessagePreference(BasePlayer player, string[] args, RPGInfo rpgInfo)
        {
            var commandArgs = args.Length - 1;
            if (commandArgs != 1)
            {
                ChatMessage(player, HMK.InvalidCommand, args[0]);
                return;
            }
            float xpPercent;
            if (!float.TryParse(args[1], out xpPercent))
            {
                ChatMessage(player, HMK.InvalidCommand, args[0]);
                return;
            }
            rpgInfo.Preferences.ShowXPMessagePercent = xpPercent / 100;
            ChatMessage(player, HMK.XpMessage, rpgInfo.Preferences.ShowXPMessagePercent);
        }

        private bool IsAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null || arg.Player().IsAdmin()) return true;
            arg.ReplyWith(_(HMK.NotAnAdmin, arg.Player()));
            return false;
        }
        #endregion

        #region Config
        protected override void LoadDefaultConfig() => DefaultConfig();

        private void DefaultConfig()
        {
            //Config.Clear();
            //this will only be called if there is not a config file, or it needs updating
            Config[HK.ConfigVersion] = Version;
            Config[HK.DataVersion] = DataVersion;
            Config[HK.AdminReset] = GetConfig(HK.AdminReset, true);
            Config[HK.ShowHud] = GetConfig(HK.ShowHud, true);
            Config[HK.ShowProfile] = GetConfig(HK.ShowProfile, true);
            Config[HK.DefaultHud] = GetConfig(HK.DefaultHud, 2);
            Config[HK.NightXP] = GetConfig(HK.NightXP, 2);
            Config[HK.DeleteProfileAfterOfflineDays] = GetConfig(HK.DeleteProfileAfterOfflineDays, 0);
            Config[HK.Trainer] = GetConfig(HK.Trainer, new ulong[0]);
            Config[HK.DeathReducerK] = GetConfig(HK.DeathReducerK, HKD.DeathReducer);
            Config[HK.Defaults] = HuntTablesGenerator.GenerateDefaults();
            Config[HK.XPTable] = HuntTablesGenerator.GenerateXPTable(HKD.MaxLevel, HKD.BaseXP, HKD.LevelMultiplier, HKD.LevelModule, HKD.ModuleReducer).OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value);
            Config[HK.ExpRateTable] = HuntTablesGenerator.GenerateExpRateTable();
            Config[HK.MaxStatsTable] = HuntTablesGenerator.GenerateMaxStatsTable();
            Config[HK.SkillTable] = HuntTablesGenerator.GenerateSkillTable();
            Config[HK.ResearchSkillTable] = HuntTablesGenerator.GenerateResearchTable();
            Config[HK.UpgradeBuildTable] = HuntTablesGenerator.GenerateUpgradeBuildingTable();
            Config[HK.ChatPrefix] = GetConfig(HK.ChatPrefix, "<color=lightblue>Hunt</color>: {0}");
            Config[HK.TameTable] = HuntTablesGenerator.GenerateTameTable();
            Config[HK.AllowedEntities] = HuntTablesGenerator.GenerateAllowedEntites();
            SaveConfig();
        }

        private void DefaultItems()
        {
            Config[HK.ItemTable] = HuntTablesGenerator.GenerateItemTable();
            SaveConfig();
        }

        private void UpdateLang()
        {
            var messagesConfig = new Dictionary<HMK, string>
            {
                {
                    HMK.Help, string.Join("\n", new[]
                    {
                        "To get an overview about the Hunt RPG, type \"/hunt about\"",
                        "To see you available shortcuts commdands, type \"/hunt shortcuts\"",
                        "To see you player profile, type \"/hunt profile\"",
                        "To see you current xp, type \"/hunt xp\"",
                        "To see how to change you profile preferences, type \"/hunt profilepreferences\"",
                        "To see you current health, type \"/hunt health\"",
                        "To see the skill list type \"/hunt skilllist\"",
                        "To see info about a specific skill type \"/hunt skill <skillname>\"",
                        "To spend your available stats points, type \"/hunt statset <stats> <points> \". Ex: /hunt statset agi 3",
                        "To spend your available skill points, type \"/hunt skillset <skillname> <points> \". Ex: /hunt skillset lumberjack 1",
                        "To reset your stat points, type \"/hunt statreset\". This will also reset your skills.",
                        "To reset your skill points, type \"/hunt skillreset\""
                    })
                },
                {
                    HMK.Shortcuts, string.Join("\n", new[]
                    {
                        "\"/hunt\" = \"/h\"",
                        "\"/hunt profile\" = \"/h p\"",
                        "\"/hunt profilepreferences\" = \"/h pp\"",
                        "\"/hunt statset\" = \"/h sts\".",
                        "You can set multiple stats at a time like this \"/h sts agi 30 str 45\".",
                        "\"/hunt skillset\" = \"/h sks\"",
                        "You can set multiple skillpoints at a time like this \"/h sks lumberjack 3 miner 2\".",
                        "\"/hunt health\" = \"/h h\""
                    })
                },
                {
                    HMK.ProfilePreferences, string.Join("\n", new[]
                    {
                        "To see change the % changed need to show the xp message, type \"/hunt xp% <percentnumber>\"",
                        "To toggle crafting message type \"/hunt craftmsg\"",
                        "To toggle blink arrow skill type \"/hunt ba\"",
                        "To toggle blink arrow skill auto toggle type \"/hunt aba\"",
                        "To toggle auto show profile \"/hunt sp\""
                    })
                },
                {
                    HMK.About, string.Join("\n", new[]
                    {
                        "=================================================",
                        "The Hunt RPG system in development.",
                        "It is consisted of levels, stats atributes, skills and later on specializations.",
                        "Currently there are 3 attributes, each of then give you and specific enhancement.",
                        "Strenght gives you more health, it will not be displayed in the Health Bar, but it is considered for healing and getting hurt.",
                        "Agillity gives you dodge change",
                        "Intelligence decreases your items crafting time",
                        "Right now you can level up by gathering resources.",
                        "Each level gives you 1 point in each attribute. And 3 more to distribute.",
                        "Each level gives you 1 skill point to distribute",
                        "Each skill have its required level, and later on it will require specific stats.",
                        "To see the all the available skills and its description type \"/hunt skilllist\"",
                        "To learn more about Hunt RPG go to the plugin page at <link>",
                        "================================================="
                    })
                },
                {
                    HMK.DataUpdated, string.Join("\n", new[]
                    {
                        "<color=yellow>Plugin was updated to new version!</color>",
                        "<color=orange>Your profile needed to be reset, but your level was saved. You just need to redistribute.</color>",
                        "<color=red>Furnaces were not saved though, so build new ones for the blacksmith skill to be applied (If you have, or when you get it)!</color>"
                    })
                },
                {HMK.InvalidCommand, "You ran the \"{0}\" command incorrectly. Type \"/hunt\" to get help"},
                {HMK.SkillInfo, "Type \"/hunt skill <skillname>\" to see the skill info"},
                {HMK.NotEnoughPoints, "<color=orange>You don't have enought points to set! Cost: {0}</color>"},
                {HMK.NotEnoughLevels, "<color=orange>You dont have the minimum level to learn this skill!</color>"},
                {HMK.NotEnoughStrength, "<color=orange>You dont have enough strength to learn this skill!</color>"},
                {HMK.NotEnoughAgility, "<color=orange>You dont have enough agility to learn this skill!</color>"},
                {HMK.NotEnoughIntelligence, "<color=orange>You dont have enough intelligence to learn this skill!</color>"},
                {HMK.NotEnoughSkill, "<color=orange>You dont have the required skill to learn this skill!</color>"},
                {HMK.InvalidSkillName, "<color=orange>There is no such skill! Type \"/hunt skilllist\" to see the available skills</color>"},
                {HMK.SkillDisabled, "<color=orange>This skill is blocked in this server.</color>"},
                {HMK.NotFoundItem, "<color=orange>Item {0} not found.</color>"},
                {HMK.ResearchBlocked, "<color=orange>Item {0} research is blocked by in this server.</color>"},
                {HMK.NotLearnedSkill, "<color=orange>You havent learned this skill yet.</color>"},
                {HMK.AlreadyAtMaxLevel, "<color=orange>You have mastered this skill already!</color>"},
                {HMK.IdAlreadyExists, "<color=yellow>Entity id already exists for {0}!</color>"},
                {HMK.PetsPlugin, "Pets plugin was not found, disabling taming skill"},
                {HMK.BuildingOwnersPlugin, "Building Owners plugin was not found, disabling blink to arrow skill"},
                {HMK.Died, "Oh no man! You just died! You lost {0:P} of XP because of this..."},
                {HMK.CantBlink, "Can't blink there!"},
                {HMK.CantBlinkOther, "Can't blink to other player house!"},
                {HMK.BlinkedRecently, "Blinked recently! You might get dizzy, give it a rest. Time left to blink again: {0}"},
                {HMK.BlinkToggle, "Auto Toggle Blink Arrow is now: {0}"},
                {HMK.BlinkStatus, "Blink Arrow is now: {0}"},
                {HMK.CraftingEnd, "Crafting will end in {0:F} seconds. Reduced in {1:F} seconds"},
                {HMK.CraftMessage, "Craft message is now: {0}"},
                {HMK.NotFoundPlayer, "Player not found."},
                {HMK.NotAnAdmin, "You are not an admin."},
                {HMK.On, "On"},
                {HMK.Off, "Off"},
                {HMK.XpMessage, "XP will be shown at every {0:P} change"},
                {HMK.AvailableSkills, "Available Skills:"},
                {HMK.Dodged, "Dodged!"},
                {HMK.LevelUp, "<color=yellow>Level Up! You are now level {0}</color>"},
                {HMK.CurrentXp, "Current XP: {0:P}{1}"},
                {HMK.NightXp, " Bonus Night Exp On"},
                {HMK.Level, "Level: {0}"},
                {HMK.LevelShort, "Lvl: {0}"},
                {HMK.DamageBlock, "Damage Block: {0:P}"},
                {HMK.EvasionChance, "Evasion Chance: {0:P}"},
                {HMK.CraftingReducer, "Crafting Reducer: {0:P}"},
                {HMK.StatPoints, "Stat points: {0}"},
                {HMK.SkillPoints, "Skill points: {0}"},
                {HMK.ResearchItem, "In order to research an item you must have it on your inventory"},
                {HMK.ResearchType, "You can't research items of this type"},
                {HMK.ResearchSkill, "Your research skills are not high enough. Required {0}"},
                {HMK.ResearchSuccess, "You managed to reverse enginier the {0}. The blueprint its on your inventory"},
                {HMK.ResearchFail, "OPS! While you were trying to research the {0} you accidently broke it."},
                {HMK.ResearchReuse, "You have tried this moments ago, give it a rest. Time left to research again: {0}"},
                {HMK.PlayerLevelUp, "Player {0} lvlup to {1}"},
                {HMK.AgiColor, "<color=green>Agi: {0}</color>"},
                {HMK.StrColor, "<color=red>Str: {0}</color>"},
                {HMK.IntColor, "<color=blue>Int: {0}</color>"},
                {HMK.Agi, "Agi: {0}"},
                {HMK.Str, "Str: {0}"},
                {HMK.Int, "Int: {0}"},
                {HMK.StatReset, "You reset your stats."},
                {HMK.StatResetPlayer, "{0} stats reset."},
                {HMK.SkillReset, "You reset your skills."},
                {HMK.SkillResetPlayer, "{0} skills reset."},
                {HMK.ProfileMessage, "Auto show profile is now: {0}"},
                {HMK.NeedNpc, "You cannot teach yourself. Go to the next trainer!"},
                {HMK.GenerateXp, "Generate XP table..."},
                {HMK.SkillsHeader, "========<color=purple>Skills</color>========"},
                {HMK.ProfileHeader, "========{0}========"},
                {
                    HMK.Loaded, string.Join("\n", new[]
                    {
                        "<color=lightblue>Hunt</color>: RPG Loaded!",
                        "<color=lightblue>Hunt</color>: To see the Hunt RPG help type \"/hunt\" or \"/h\""
                    })
                },
                {HMK.StatusLoad, "{0} profiles, {1} furnaces, {2} quarries loaded"},
                {HMK.StatusSave, "{0} profiles, {1} furnaces, {2} quarries saved"},
                {HMK.TopPlayer, "{0}. {1} Lvl {2}"},
                {HMK.SkillUp, "<color=purple>{0}: +{1}</color>"},
                {HMK.SkillCost, "Each skill level costs {0} skillpoints."},
                {HMK.SkillInfoHeader, "<color=lightblue>{0}</color> - Required: Lvl: {1}"},
                {HMK.Usage, "<color=teal>Usage:</color> {0}"},
                {HMK.LumberjackDesc, "This skill allows you to gather wood faster. Each point gives you more wood per hit."},
                {HMK.MinerDesc, "This skill allows you to gather stones faster. Each point gives you more stones per hit."},
                {HMK.HunterDesc, "This skill allows you to gather resources faster from animals. Each point gives you more resources per hit."},
                {HMK.GathererDesc, "This skill allows you to gather more resources from pickup. Each point gives you more resources."},
                {HMK.ResearcherDesc, "This skill allows you to research items you have. Each level enables a type of type to be researched and decreases 2 minutes of cooldown. Table: Level 1 - Tools (10 min); Level 2 - Clothes (8 min); Level 3 - Construction and Resources (6 min); Level 4 - Ammunition and Medic (4 min); Level 5 - Weapons (2 min)"},
                {HMK.BlacksmithDesc, "This skill allows your furnaces to melt more resources each time. Each level gives increase the productivity by 1."},
                {HMK.BlinkarrowDesc, "This skill allows you to blink to your arrow destination from time to time. Each level deacreases the cooldown in 2 minutes."},
                {HMK.TamerDesc, "This skill allows you to tame a animal as your pet. Level 1 allows chicken, level 2 allows boar, level 3 allows stag, level 4 allows wolf, level 5 allows bear, level 6 allows horse."},
                {HMK.BlinkarrowUsage, "Just shoot an Arrow at desired blink location. To toogle this skill type \"/h ba\" . To change the auto toggle for this skill type \"/h aba\""},
                {HMK.ResearcherUsage, "To research an item type \"/research \"Item Name\"\". In order to research an item, you must have it on your invetory, and have the required skill level for that item tier."},
                {HMK.TamerUsage, "Type \"/pet\" to toggle taming. To tame get close to the animal and press your USE button(E). After tamed press USE looking at something, if its terrain he will move, if its a player or other animal it he will attack. If looking at him it will start following you. To set the pet free type \"/pet free\"."},
                {HMK.ButtonClose, "Close"},
                {HMK.ButtonResetSkills, "Reset Skills"},
                {HMK.ButtonResetStats, "Reset Stats"}
            };
            lang.RegisterMessages(messagesConfig.ToDictionary(m => m.Key.ToString(), m => m.Value), this);
        }

        private void UpdateData()
        {
            if (!updatePlayerData) return;
            // this will only be called if this version requires a data wipe and the config is outdated.
            Puts("This version needs a wipe to data file.");
            Puts("Dont worry levels will be kept! =]");
            var keys = Data.Profiles.Keys.ToArray();
            foreach (var key in keys)
            {
                var value = Data.Profiles[key];
                var displayName = FindPlayer(key)?.displayName ?? value.SteamName;
                var rpgInfo = new RPGInfo(displayName, DefaultHud, ShowProfile);
                rpgInfo.LevelUp(value.Level);
                Data.Profiles[key] = rpgInfo;
            }
            Puts("Data file updated.");
            Config[HK.DataVersion] = DataVersion;
            SaveConfig();
            SaveRpg();
            updatePlayerData = false;
        }

        private void LoadRpg(VersionNumber dataVersion)
        {
            RPGInfo.Perm = permission;
            var defaults = ReadFromConfig<HuntDefaults>(HK.Defaults);
            RPGInfo.XPTable = ReadFromConfig<Dictionary<int, long>>(HK.XPTable).OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value).Values.ToArray();
            RPGInfo.MaxStatsTable = ReadFromConfig<Dictionary<HRK, Modifier>>(HK.MaxStatsTable);
            RPGInfo.SkillPointsGain = defaults.SkillPointsGain;
            RPGInfo.SkillPointsPerLevel = defaults.SkillPointsPerLevel;
            RPGInfo.StatPointsGain = defaults.StatPointsGain;
            RPGInfo.StatPointsPerLevel = defaults.StatPointsPerLevel;
            RPGInfo.TameTable = ReadFromConfig<Dictionary<int, string>>(HK.TameTable);
            var newStructure = dataVersion < new VersionNumber(0, 9, 3);
            if (newStructure)
                huntDataFile.Settings.ContractResolver = new IgnoreJsonPropertyResolver();
            Data = huntDataFile.ReadObject<HuntData>();
            if (newStructure)
                huntDataFile.Settings.ContractResolver = new DefaultContractResolver();
            ChatPrefix = ReadFromConfig<string>(HK.ChatPrefix);
            ExpRateTable = ReadFromConfig<Dictionary<ResourceDispenser.GatherType, float>>(HK.ExpRateTable);
            SkillTable = ReadFromConfig<Dictionary<HRK, Skill>>(HK.SkillTable);
            ItemTable = ReadFromConfig<Dictionary<string, ItemInfo>>(HK.ItemTable);
            ResearchTable = ReadFromConfig<Dictionary<string, int>>(HK.ResearchSkillTable);
            UpgradeBuildingTable = ReadFromConfig<Dictionary<BuildingGrade.Enum, float>>(HK.UpgradeBuildTable);
            AllowedEntites = ReadFromConfig<string[]>(HK.AllowedEntities);
            AdminReset = ReadFromConfig<bool>(HK.AdminReset);
            ShowHud = ReadFromConfig<bool>(HK.ShowHud);
            ShowProfile = ReadFromConfig<bool>(HK.ShowProfile);
            DefaultHud = ReadFromConfig<uint>(HK.DefaultHud);
            NightXP = ReadFromConfig<float>(HK.NightXP);
            DeleteProfileAfter = ReadFromConfig<int>(HK.DeleteProfileAfterOfflineDays);
            Trainer = ReadFromConfig<ulong[]>(HK.Trainer);
            DeathReducer = ReadFromConfig<float>(HK.DeathReducerK);
            itemShortname = ItemManager.itemList.ToDictionary(definition => definition.displayName.translated.ToLower(), definition => definition.shortname);

            Puts(_(HMK.StatusLoad, Data.Profiles.Count, Data.Furnaces.Count, Data.Quarries.Count));

            if (Pets == null)
            {
                Puts(_(HMK.PetsPlugin));
                SkillTable[HRK.Tamer].Enabled = false;
            }
            if (DeleteProfileAfter <= 0) return;
            var now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            var delTime = now - 86400 * DeleteProfileAfter;
            var toRemove = new List<ulong>();
            foreach (var profile in Data.Profiles)
            {
                if (profile.Value.LastSeen == 0)
                {
                    profile.Value.LastSeen = now;
                    continue;
                }
                if (profile.Value.LastSeen < delTime)
                {
                    toRemove.Add(profile.Key);
                    var data = Data.Furnaces.Where(pair => pair.Value == profile.Key).Select(pair => pair.Key).ToArray();
                    foreach (var key in data)
                        Data.Furnaces.Remove(key);
                    data = Data.Quarries.Where(pair => pair.Value == profile.Key).Select(pair => pair.Key).ToArray();
                    foreach (var key in data)
                        Data.Quarries.Remove(key);
                }
            }
            foreach (var userId in toRemove)
                Data.Profiles.Remove(userId);
        }

        private T ReadFromConfig<T>(string configKey)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(Config[configKey]));
        }

        private object GetConfig(string key, object defaultValue)
        {
            var value = Config[key];
            if (value == null)
                Config[key] = value = defaultValue;
            return value;
        }

        private void SaveRpg(bool showMsgs = true)
        {
            if (Data == null) return;
            //if (showMsgs)
            //    Puts("Data being saved...");
            huntDataFile.WriteObject(Data);
            if (!showMsgs) return;
            Puts(_(HMK.StatusSave, Data.Profiles.Count, Data.Furnaces.Count, Data.Quarries.Count));
        }
        #endregion

        #region CUI
        private void GuiInit(BasePlayer player)
        {
            if (player == null) return;

            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
                timer.Once(1, () => GuiInit(player));
            else
                UpdateHud(player);
        }

        private static CuiLabel CreateLabel(string text, int i, float rowHeight, TextAnchor align = TextAnchor.MiddleLeft, int fontSize = 15, string xMin = "0", string xMax = "1", string color = "1.0 1.0 1.0 1.0")
        {
            return new CuiLabel
            {
                Text =
                {
                    Text = text,
                    FontSize = fontSize,
                    Align = align,
                    Color = color
                },
                RectTransform =
                {
                    AnchorMin = $"{xMin} {1 - rowHeight*i + i * .002f}",
                    AnchorMax = $"{xMax} {1 - rowHeight*(i-1) + i * .002f}"
                }
            };
        }

        private static CuiButton CreateButton(string command, int i, float rowHeight, int fontSize = 15, string content = "+", string xMin = "0", string xMax = "1")
        {
            return new CuiButton
            {
                Button =
                {
                    Command = command,
                    Color = "0.8 0.8 0.8 0.2"
                },
                RectTransform =
                {
                    AnchorMin = $"{xMin} {1 - rowHeight*i + i * .002f}",
                    AnchorMax = $"{xMax} {1 - rowHeight*(i-1) + i * .002f}"
                },
                Text =
                {
                    Text = content,
                    FontSize = fontSize,
                    Align = TextAnchor.MiddleCenter
                }
            };
        }

        private static CuiPanel CreatePanel(string anchorMin, string anchorMax, string color = "0 0 0 0")
        {
            return new CuiPanel
            {
                Image =
                {
                    Color = color
                },
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax
                }
            };
        }

        private void NpcGui(BasePlayer player, bool repaint = false)
        {
            if (player == null) return;
            var rpgInfo = FindRpgInfo(player);
            GUIInfo guiInfo;
            if (!GUIInfo.TryGetValue(player.userID, out guiInfo))
                GUIInfo[player.userID] = guiInfo = new GUIInfo();
            else
            {
                DestroyUi(player, guiInfo.LastInfo);
                DestroyUi(player, guiInfo.LastStats);
                DestroyUi(player, guiInfo.LastSkills);
            }

            const float height = 1 / (6f * 1.5f);
            var heightS = 1f / (Math.Min(6, SkillTable.Count(skill => skill.Value.Enabled)) * 1.75f);
            var elements = new CuiElementContainer();
            if (!repaint || string.IsNullOrEmpty(guiInfo.LastMain))
            {
                guiInfo.LastMain = elements.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0.1 0.1 0.1 0.8"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                    CursorEnabled = true
                });
                elements.Add(new CuiButton
                {
                    Button =
                {
                    Close = guiInfo.LastMain,
                    Color = "0.8 0.8 0.8 0.2"
                },
                    RectTransform =
                {
                    AnchorMin = "0.45 0.92",
                    AnchorMax = "0.55 0.98"
                },
                    Text =
                {
                    Text = _(HMK.ButtonClose, player),
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                }
                }, guiInfo.LastMain);
                elements.Add(CreateLabel(rpgInfo.SteamName, 3, .06f, TextAnchor.MiddleCenter, 30, "0.3", "0.7"), guiInfo.LastMain);
                var statsButtons = elements.Add(CreatePanel("0.4 0.1", "0.45 0.5"), guiInfo.LastMain);
                elements.Add(CreateButton("hunt.cmd statset agi 1", 3, heightS, 18), statsButtons);
                elements.Add(CreateButton("hunt.cmd statset str 1", 4, heightS, 18), statsButtons);
                elements.Add(CreateButton("hunt.cmd statset int 1", 5, heightS, 18), statsButtons);
                var skillsButtons = elements.Add(CreatePanel("0.8 0.1", "0.85 0.5"), guiInfo.LastMain);
                var i = 3;
                foreach (var skill in SkillTable.Where(skill => skill.Value.Enabled))
                {
                    int level;
                    rpgInfo.Skills.TryGetValue(skill.Key, out level);
                    elements.Add(CreateButton($"hunt.cmd skillset {skill.Key} 1", i++, heightS, 18), skillsButtons);
                }
                if (!AdminReset || player.IsAdmin())
                {
                    elements.Add(CreateButton("hunt.cmd statreset", 25, .04f, 18, _(HMK.ButtonResetStats, player), "0.2", "0.45"), guiInfo.LastMain);
                    elements.Add(CreateButton("hunt.cmd skillreset", 25, .04f, 18, _(HMK.ButtonResetSkills, player), "0.6", "0.85"), guiInfo.LastMain);
                }
            }

            var info = guiInfo.LastInfo = elements.Add(CreatePanel("0.3 0.5", "0.7 0.8"), guiInfo.LastMain);
            elements.Add(CreateLabel(_(HMK.Level, player, rpgInfo.Level), 1, height, TextAnchor.MiddleCenter, 20), info);
            elements.Add(CreateLabel(XPProgression(player, rpgInfo), 2, height, TextAnchor.MiddleCenter, 20), info);
            elements.Add(CreateLabel(_(HMK.DamageBlock, player, rpgInfo.GetBlock()), 4, height, TextAnchor.MiddleCenter, 20), info);
            elements.Add(CreateLabel(_(HMK.EvasionChance, player, rpgInfo.GetEvasion()), 5, height, TextAnchor.MiddleCenter, 20), info);
            elements.Add(CreateLabel(_(HMK.CraftingReducer, player, rpgInfo.GetCraftingReducer()), 6, height, TextAnchor.MiddleCenter, 20), info);

            var stats = guiInfo.LastStats = elements.Add(CreatePanel("0.2 0.1", "0.39 0.5"), guiInfo.LastMain);
            elements.Add(CreateLabel(_(HMK.StatPoints, player, rpgInfo.StatsPoints), 1, heightS, TextAnchor.MiddleLeft, 20), stats);
            elements.Add(CreateLabel(_(HMK.Agi, player, $"{rpgInfo.Agility} ({rpgInfo.GetStatPointsCost(HRK.Agi)})"), 3, heightS, TextAnchor.MiddleLeft, 18), stats);
            elements.Add(CreateLabel(_(HMK.Str, player, $"{rpgInfo.Strength} ({rpgInfo.GetStatPointsCost(HRK.Str)})"), 4, heightS, TextAnchor.MiddleLeft, 18), stats);
            elements.Add(CreateLabel(_(HMK.Int, player, $"{rpgInfo.Intelligence} ({rpgInfo.GetStatPointsCost(HRK.Int)})"), 5, heightS, TextAnchor.MiddleLeft, 18), stats);

            var skills = guiInfo.LastSkills = elements.Add(CreatePanel("0.6 0.1", "0.79 0.5"), guiInfo.LastMain);
            elements.Add(CreateLabel(_(HMK.SkillPoints, player, rpgInfo.SkillPoints), 1, heightS, TextAnchor.MiddleLeft, 20), skills);
            var j = 3;
            foreach (var skill in SkillTable.Where(skill => skill.Value.Enabled))
            {
                int level;
                rpgInfo.Skills.TryGetValue(skill.Key, out level);
                elements.Add(CreateLabel($"{skill.Value.Name}: {level}/{skill.Value.MaxLevel} ({rpgInfo.GetSkillPointsCostNext(skill.Value)})", j++, heightS, TextAnchor.MiddleLeft, 18), skills);
            }
            CuiHelper.AddUi(player, elements);
        }

        private void ProfileGui(BasePlayer player, bool repaint = false)
        {
            if (player == null) return;
            var rpgInfo = FindRpgInfo(player);
            GUIInfo guiInfo;
            if (!GUIInfo.TryGetValue(player.userID, out guiInfo))
                GUIInfo[player.userID] = guiInfo = new GUIInfo();
            else
                DestroyUi(player, guiInfo.LastContent);

            var skills = SkillTable.Where(skill => skill.Value.Enabled).ToArray();
            var height = 1f / (9.5f + skills.Length) - .002f;

            var elements = new CuiElementContainer();
            if (!repaint || string.IsNullOrEmpty(guiInfo.LastMain))
            {
                guiInfo.LastMain = elements.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0.1 0.1 0.1 0.8"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.8 0.25",
                        AnchorMax = "0.995 0.845"
                    },
                    CursorEnabled = true
                });
                elements.Add(new CuiButton
                {
                    Button =
                {
                    Close = guiInfo.LastMain,
                    Color = "0.8 0.8 0.8 0.2"
                },
                    RectTransform =
                {
                    AnchorMin = "0.86 0.93",
                    AnchorMax = "0.97 0.99"
                },
                    Text =
                {
                    Text = "X",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                }
                }, guiInfo.LastMain);
                elements.Add(CreateLabel(rpgInfo.SteamName, 1, height, TextAnchor.MiddleLeft, 18, "0.06", "0.85"), guiInfo.LastMain);
                var buttonsName = elements.Add(CreatePanel("0.86 0", "0.97 0.925"), guiInfo.LastMain);
                elements.Add(CreateButton("hunt.cmd statset agi 1", 6, height), buttonsName);
                elements.Add(CreateButton("hunt.cmd statset str 1", 7, height), buttonsName);
                elements.Add(CreateButton("hunt.cmd statset int 1", 8, height), buttonsName);
                var i = 9;
                foreach (var skill in skills)
                {
                    int level;
                    rpgInfo.Skills.TryGetValue(skill.Key, out level);
                    elements.Add(CreateButton($"hunt.cmd skillset {skill.Key} 1", i++, height), buttonsName);
                }
                if (!AdminReset || player.IsAdmin())
                {
                    elements.Add(CreateButton("hunt.cmd statreset", i++, height, 15, "R"), buttonsName);
                    elements.Add(CreateButton("hunt.cmd skillreset", i, height, 15, "R"), buttonsName);
                }
            }
            var contentName = guiInfo.LastContent = elements.Add(CreatePanel("0.06 0", "0.85 0.925"), guiInfo.LastMain);
            elements.Add(CreateLabel(_(HMK.Level, player, rpgInfo.Level), 1, height), contentName);
            elements.Add(CreateLabel(_(HMK.DamageBlock, player, rpgInfo.GetBlock()), 2, height), contentName);
            elements.Add(CreateLabel(_(HMK.EvasionChance, player, rpgInfo.GetEvasion()), 3, height), contentName);
            elements.Add(CreateLabel(_(HMK.CraftingReducer, player, rpgInfo.GetCraftingReducer()), 4, height), contentName);
            elements.Add(CreateLabel(XPProgression(player, rpgInfo), 5, height), contentName);
            elements.Add(CreateLabel(_(HMK.Agi, player, $"{rpgInfo.Agility} ({rpgInfo.GetStatPointsCost(HRK.Agi)})"), 6, height), contentName);
            elements.Add(CreateLabel(_(HMK.Str, player, $"{rpgInfo.Strength} ({rpgInfo.GetStatPointsCost(HRK.Str)})"), 7, height), contentName);
            elements.Add(CreateLabel(_(HMK.Int, player, $"{rpgInfo.Intelligence} ({rpgInfo.GetStatPointsCost(HRK.Int)})"), 8, height), contentName);
            var j = 9;
            foreach (var skill in skills)
            {
                int level;
                rpgInfo.Skills.TryGetValue(skill.Key, out level);
                elements.Add(CreateLabel($"{skill.Value.Name}: {level}/{skill.Value.MaxLevel} ({rpgInfo.GetSkillPointsCostNext(skill.Value)})", j++, height), contentName);
            }
            elements.Add(CreateLabel(_(HMK.StatPoints, player, rpgInfo.StatsPoints), j++, height), contentName);
            elements.Add(CreateLabel(_(HMK.SkillPoints, player, rpgInfo.SkillPoints), j, height), contentName);
            CuiHelper.AddUi(player, elements);
        }

        private void UpdateHud(BasePlayer player, bool repaint = false, bool points = true)
        {
            if (player == null || !ShowHud) return;
            var rpgInfo = FindRpgInfo(player);
            if (rpgInfo == null || rpgInfo.Preferences.ShowHud == 0) return;
            GUIInfo guiInfo;
            if (!GUIInfo.TryGetValue(player.userID, out guiInfo))
                GUIInfo[player.userID] = guiInfo = new GUIInfo();
            else
            {
                if (guiInfo.LastHudTime > Interface.Oxide.Now)
                {
                    if (guiInfo.LastHudTimer == null) guiInfo.LastHudTimer = timer.Once(1, () => UpdateHud(player, repaint, points));
                    return;
                }
                guiInfo.LastHudTimer?.Destroy();
                guiInfo.LastHudTimer = null;
                guiInfo.LastHudTime = Interface.Oxide.Now + 1;
            }

            var elements = new CuiElementContainer();
            if (!repaint || string.IsNullOrEmpty(guiInfo.LastHud))
            {
                DestroyUi(player, guiInfo.LastHud);
                //elements.Add(CreateLabel($"{_(HMK.Level, player, rpgInfo.Level)}/{XPTable.Length}", 2, 1 / 3f), guiInfo.LastHud);
                //AnchorMin = "0.822 0.045",
                //AnchorMax = "0.9734 0.117"
                if (rpgInfo.Preferences.ShowHud == 1)
                    guiInfo.LastHud = elements.Add(CreatePanel("0.0266 0.045", "0.178 0.117"));
                else
                    guiInfo.LastHud = elements.Add(CreatePanel("0 0", "1 0.03"));
            }

            if (points) DestroyUi(player, guiInfo.LastHudFirst);
            DestroyUi(player, guiInfo.LastHudSecond);

            if (rpgInfo.Preferences.ShowHud == 1)
            {
                if (points)
                {
                    guiInfo.LastHudFirst = elements.Add(CreatePanel("0 0.51", "1 1", "0.16 0.16 0.16 0.8"), guiInfo.LastHud);
                    elements.Add(CreateLabel($"{_(HMK.StatPoints, player, rpgInfo.StatsPoints)} {_(HMK.SkillPoints, player, rpgInfo.SkillPoints)}", 1, 1, TextAnchor.MiddleCenter, 15, "0", "1", "1.0 1.0 1.0 0.3"), guiInfo.LastHudFirst);
                }
                guiInfo.LastHudSecond = elements.Add(CreatePanel("0 0", "1 0.45", "0.16 0.16 0.16 0.8"), guiInfo.LastHud);
                elements.Add(CreateLabel($"{rpgInfo.Level}", 1, 1, TextAnchor.MiddleCenter, 15, "0", "0.19", "1.0 1.0 1.0 0.3"), guiInfo.LastHudSecond);
                elements.Add(CreatePanel("0.2 0.1", $"{.2f + .77f*CurrentPercent(rpgInfo)} 0.88", "0.8392156862745098 0.6823529411764706 0 0.5"), guiInfo.LastHudSecond);
                elements.Add(CreateLabel($"{rpgInfo.Experience}/{rpgInfo.RequiredExperience()}", 1, 1, TextAnchor.MiddleCenter, 15, "0.2", "1", "1.0 1.0 1.0 0.3"), guiInfo.LastHudSecond);
            }
            else
            {
                if (points)
                {
                    guiInfo.LastHudFirst = elements.Add(CreatePanel("0.45 0.2", "0.55 1", "0.13 0.13 0.13 0"), guiInfo.LastHud);
                    elements.Add(CreateLabel($"{_(HMK.Level, player, rpgInfo.Level)}", 1, 1, TextAnchor.MiddleCenter, 15, "0", "0.95", "1.0 0.6470588235294118 0.0 0.6"), guiInfo.LastHudFirst);
                }
                guiInfo.LastHudSecond = elements.Add(CreatePanel("0 0", "1 0.2", "0.13 0.13 0.13 0.75"), guiInfo.LastHud);
                elements.Add(CreatePanel("0.0015 0.3", $"{.9985f * CurrentPercent(rpgInfo)} 0.7", "1.0 0.6470588235294118 0.0 0.6"), guiInfo.LastHudSecond);
            }

            //Puts(CuiHelper.ToJson(elements));
            CuiHelper.AddUi(player, elements);
        }
        #endregion

        private void ExpGain(RPGInfo rpgInfo, int experience, BasePlayer player)
        {
            var steamId = player.userID;
            if (IsNight())
                experience = (int)(experience * NightXP);
            if (rpgInfo.AddExperience(experience))
            {
                NotifyLevelUp(player, rpgInfo);
                PlayerLastPercentChange[steamId] = 0;
            }
            else if (!ShowHud)
            {
                var currentPercent = CurrentPercent(rpgInfo);
                float lastPercent;
                if (!PlayerLastPercentChange.TryGetValue(steamId, out lastPercent))
                    PlayerLastPercentChange.Add(steamId, lastPercent = currentPercent);
                var percentChange = currentPercent - lastPercent;
                if (percentChange >= rpgInfo.Preferences.ShowXPMessagePercent)
                {
                    ChatMessage(player, XPProgression(player, rpgInfo));
                    PlayerLastPercentChange[steamId] = currentPercent;
                }
            }
            UpdateHud(player, true, false);
        }

        private void NotifyLevelUp(BasePlayer player, RPGInfo rpgInfo)
        {
            ChatMessage(player, HMK.LevelUp, rpgInfo.Level);
            if (rpgInfo.Preferences.ShowProfile) ChatMessage(player, Profile(rpgInfo, player));
            UpdateHud(player, true);
            //SaveRPG(false);
        }

        private string Profile(RPGInfo rpgInfo, BasePlayer player)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(_(HMK.ProfileHeader, player, rpgInfo.SteamName));
            sb.AppendLine(_(HMK.Level, player, rpgInfo.Level));
            sb.AppendLine(_(HMK.DamageBlock, player, rpgInfo.GetBlock()));
            sb.AppendLine(_(HMK.EvasionChance, player, rpgInfo.GetEvasion()));
            sb.AppendLine(_(HMK.CraftingReducer, player, rpgInfo.GetCraftingReducer()));
            sb.AppendLine(XPProgression(player, rpgInfo));
            var Agi = _(HMK.AgiColor, player, $"{rpgInfo.Agility} ({rpgInfo.GetStatPointsCost(HRK.Agi)})");
            var Str = _(HMK.StrColor, player, $"{rpgInfo.Strength} ({rpgInfo.GetStatPointsCost(HRK.Str)})");
            var Int = _(HMK.IntColor, player, $"{rpgInfo.Intelligence} ({rpgInfo.GetStatPointsCost(HRK.Int)})");
            sb.AppendLine($"{Agi} | {Str} | {Int}");
            sb.AppendLine(_(HMK.StatPoints, player, rpgInfo.StatsPoints));
            sb.AppendLine(_(HMK.SkillPoints, player, rpgInfo.SkillPoints));
            sb.AppendLine(_(HMK.SkillsHeader, player));
            foreach (var skill in SkillTable.Where(skill => skill.Value.Enabled))
            {
                int level;
                rpgInfo.Skills.TryGetValue(skill.Key, out level);
                sb.AppendLine($"{skill.Value.Name}: {level}/{skill.Value.MaxLevel} ({rpgInfo.GetSkillPointsCostNext(skill.Value)})");
            }
            sb.AppendLine("====================");
            return sb.ToString();
        }

        #region Skill
        private void UpdateGatherPlayer(BasePlayer player, RPGInfo rpgInfo)
        {
            foreach (var item in player.inventory.AllItems())
                UpdateGather(item, rpgInfo);
        }

        private void UpdateMagazinPlayer(BasePlayer player, RPGInfo rpgInfo)
        {
            foreach (var item in player.inventory.AllItems())
                UpdateMagazin(item, rpgInfo);
        }

        private void UpdateEffectsPlayer(BasePlayer player, RPGInfo rpgInfo)
        {
            foreach (var item in player.inventory.AllItems())
            {
                UpdateGather(item, rpgInfo);
                UpdateMagazin(item, rpgInfo);
            }
        }

        private void UpdateGather(Item item, RPGInfo rpgInfo)
        {
            var melee = item.GetHeldEntity() as BaseMelee;
            if (melee == null) return;
            var defaultMelee = GameManager.server.FindPrefab(melee.LookupPrefabName()).GetComponent<BaseMelee>();
            UpdateGatherPropertyEntry(melee.gathering.Tree, defaultMelee.gathering.Tree, rpgInfo, HRK.Lumberjack);
            //SendReply(item.GetOwnerPlayer(), "Item: {0} G: {1:0.00} C: {2:0.00} D: {3:0.00}", item.info.shortname, defaultMelee.gathering.Tree.gatherDamage, defaultMelee.gathering.Tree.conditionLost, defaultMelee.gathering.Tree.destroyFraction);
            //SendReply(item.GetOwnerPlayer(), "Item: {0} G: {1:0.00} C: {2:0.00} D: {3:0.00}", item.info.shortname, melee.gathering.Tree.gatherDamage, melee.gathering.Tree.conditionLost, melee.gathering.Tree.destroyFraction);
            UpdateGatherPropertyEntry(melee.gathering.Ore, defaultMelee.gathering.Ore, rpgInfo, HRK.Miner);
            UpdateGatherPropertyEntry(melee.gathering.Flesh, defaultMelee.gathering.Flesh, rpgInfo, HRK.Hunter);
        }

        private void UpdateGatherPropertyEntry(ResourceDispenser.GatherPropertyEntry entry, ResourceDispenser.GatherPropertyEntry defaultEntry, RPGInfo rpgInfo, HRK skillType)
        {
            int skillPoints;
            float modifier;
            float reducer;
            if (rpgInfo.Skills.TryGetValue(skillType, out skillPoints))
            {
                modifier = GatherModifier(skillPoints, skillType);
                reducer = Mathf.Lerp(.5f, 1, 1 - modifier / GatherModifier(SkillTable[skillType].MaxLevel, skillType));
            }
            else
            {
                modifier = 1;
                reducer = 1;
            }
            entry.gatherDamage = modifier * defaultEntry.gatherDamage;
            entry.conditionLost = reducer * defaultEntry.conditionLost;
            entry.destroyFraction = reducer * defaultEntry.destroyFraction;
        }

        private void UpdateMagazin(Item item, RPGInfo rpgInfo)
        {
            var projectile = item.GetHeldEntity() as BaseProjectile;
            if (projectile == null) return;
            var capacity = (int)(projectile.primaryMagazine.definition.builtInSize * (1f + rpgInfo.Strength / (RPGInfo.XPTable.Length * 4f)));
            if (projectile.primaryMagazine.capacity == capacity) return;
            projectile.primaryMagazine.contents += capacity - projectile.primaryMagazine.capacity;
            projectile.primaryMagazine.capacity = capacity;
            projectile.SendNetworkUpdateImmediate();
            item.GetOwnerPlayer()?.inventory.ServerUpdate(0f);
        }

        private void SetCooldown(int skillPoints, float time, Dictionary<HRK, float> playerCooldowns, HRK skillKey)
        {
            playerCooldowns[skillKey] = CooldownModifier(skillPoints, skillKey, time);
        }

        private Dictionary<HRK, float> PlayerCooldowns(ulong steamId)
        {
            Dictionary<HRK, float> playerCooldowns;
            if (!SkillsCooldowns.TryGetValue(steamId, out playerCooldowns))
                SkillsCooldowns.Add(steamId, playerCooldowns = new Dictionary<HRK, float>());
            return playerCooldowns;
        }

        private float GatherModifier(int skillpoints, HRK skillType)
        {
            //TODO cache values?
            return (float)Math.Pow(SkillTable[skillType].Modifiers[HRK.Gather].Args[0], skillpoints);
            //var skill = SkillTable[skillType];
            //return 1f + skillpoints / (float)skill.MaxLevel * skill.Modifiers[HRK.Gather].Args[0];
        }

        private int GatherModifierInt(int skillpoints, HRK skillType, int value)
        {
            return (int) Math.Ceiling(GatherModifier(skillpoints, skillType) * value);
        }

        private float CooldownModifier(int skillpoints, HRK skillKey, float currenttime)
        {
            var modifier = SkillTable[skillKey].Modifiers[HRK.Cooldown];
            var baseCooldown = modifier.Args[0]*60f;
            var timeToReduce = (skillpoints - 1f)*modifier.Args[1]*60f;
            var finalCooldown = baseCooldown - timeToReduce;
            return finalCooldown + currenttime;
        }
        #endregion

        #region Message
        private string _(HMK key, params object[] args)
        {
            return _(key, (string) null, args);
        }

        private string _(HMK key, BasePlayer player, params object[] args)
        {
            return _(key, player?.UserIDString, args);
        }

        private string _(HMK key, string userid = null, params object[] args)
        {
            var message = lang.GetMessage(key.ToString(), this, userid);
            return message != null ? args.Length > 0 ? string.Format(message, args) : message : string.Empty;
        }

        private void ChatMessage(BasePlayer player, string message)
        {
            if (player?.net == null) return;
            player.ChatMessage(string.Format(ChatPrefix, message));
        }

        private void ChatMessage(BasePlayer player, HMK key, params object[] args)
        {
            ChatMessage(player, _(key, player.UserIDString, args));
        }
        #endregion

        #region Util
        private RPGInfo FindRpgInfo(BasePlayer player)
        {
            if (Data == null) return null;
            var userId = player.userID;
            RPGInfo config;
            if (Data.Profiles.TryGetValue(userId, out config))
            {
                config.SetUserId(userId);
                config.SteamName = player.displayName;
                config.LastSeen = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                return config;
            }
            Data.Profiles[userId] = config = new RPGInfo(player.displayName, DefaultHud, ShowProfile);
            config.SetUserId(userId);
            //SaveRPG();
            return config;
        }

        private RPGInfo FindRpgInfo(ulong userId)
        {
            RPGInfo config;
            Data.Profiles.TryGetValue(userId, out config);
            config?.SetUserId(userId);
            return config;
        }

        private bool IsBuildingAllowed(Vector3 position, BasePlayer player)
        {
            var hits = Physics.OverlapSphere(position, 2f, triggerMask);
            foreach (var collider in hits)
            {
                var buildingPrivlidge = collider.GetComponentInParent<BuildingPrivlidge>();
                if (buildingPrivlidge == null) continue;
                if (!buildingPrivlidge.IsAuthed(player)) return false;
            }
            return true;
        }

        private string XPProgression(BasePlayer player, RPGInfo rpgInfo)
        {
            var percent = CurrentPercent(rpgInfo);
            var nightBonus = string.Empty;
            if (IsNight())
                nightBonus = _(HMK.NightXp, player);
            return _(HMK.CurrentXp, player, percent, nightBonus);
        }

        private float CurrentPercent(RPGInfo rpgInfo)
        {
            var requiredXp = rpgInfo.RequiredExperience();
            return requiredXp <= 0 ? 1 : Mathf.Clamp01(rpgInfo.Experience / (float)requiredXp);
        }

        private bool IsNPCInRange(Vector3 pos)
        {
            return Physics.OverlapSphere(pos, 3, playersMask).Select(col => col.GetComponentInParent<BasePlayer>()).Any(player => player != null && Trainer.Contains(player.userID));
        }

        private static void TeleportPlayerTo(BasePlayer player, Vector3 position)
        {
            player.MovePosition(position);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            player.TransformChanged();
            //TODO replace later
            //ForcePlayerPosition(player, position);
        }

        private static Vector3 GetGround(Vector3 position)
        {
            var height = TerrainMeta.HeightMap.GetHeight(position);
            position.y = Math.Max(position.y, height);
            return position;
            /*var raycastHits = Physics.RaycastAll(position, Vector3.forward, 25f).GetEnumerator();
            var nearestDistance = 9999f;
            var nearestPoint = Vector3.zero;
            while (raycastHits.MoveNext())
            {
                if (raycastHits.Current == null) continue;
                var raycastHit = (RaycastHit)raycastHits.Current;
                if (raycastHit.distance < nearestDistance)
                {
                    nearestDistance = raycastHit.distance;
                    nearestPoint = raycastHit.point;
                }
            }
            return nearestPoint;*/
        }

        private static bool IsNight()
        {
            var dateTime = TOD_Sky.Instance.Cycle.DateTime;
            return dateTime.Hour >= 19 || dateTime.Hour <= 5;
        }

        private static bool IsSkillReady(Dictionary<HRK, float> playerCooldowns, ref float availableAt, float time, HRK skillKey)
        {
            bool isReady;
            if (playerCooldowns.TryGetValue(skillKey, out availableAt))
            {
                isReady = time >= availableAt;
            }
            else
            {
                isReady = true;
                playerCooldowns.Add(skillKey, availableAt);
            }
            return isReady;
        }

        private static BasePlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString == nameOrIdOrIp)
                    return sleepingPlayer;
                if (sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return sleepingPlayer;
            }
            return null;
        }

        private static BasePlayer FindPlayer(ulong userId)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.userID == userId)
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.userID == userId)
                    return sleepingPlayer;
            }
            return null;
        }

        private static string TimeLeft(float availableAt, float time)
        {
            var timeLeft = availableAt - time;
            var formatableTime = new DateTime(TimeSpan.FromSeconds(timeLeft).Ticks);
            return $"{formatableTime:mm\\:ss}";
        }

        private static string EntityId(BaseEntity entity)
        {
            if (entity == null) return "XYZ";
            var position = entity.transform.position;
            return $"X{position.x}Y{position.y}Z{position.z}";
        }

        private static string EntityName(BaseEntity entity)
        {
            var name = entity.LookupShortPrefabName();
            var pos = name.LastIndexOf(".", StringComparison.Ordinal);
            if (pos >= 0) name = name.Substring(0, pos);
            return name;
        }

        private void DestroyUi(BasePlayer player, string name)
        {
            if (!string.IsNullOrEmpty(name)) CuiHelper.DestroyUi(player, name);
        }
        #endregion

        public class IgnoreJsonPropertyResolver : DefaultContractResolver
        {
            private Dictionary<string, string> PropertyMappings { get; set; }

            public IgnoreJsonPropertyResolver()
            {
                PropertyMappings = new Dictionary<string, string>();
                var types = new[] {typeof (RPGInfo), typeof (ProfilePreferences)};
                foreach (var type in types)
                {
                    var fields = type.GetFields();
                    foreach (var fieldInfo in fields)
                    {
                        var jsonProperty = fieldInfo.GetCustomAttributes(typeof(JsonPropertyAttribute), false);
                        if (jsonProperty.Length > 0)
                        {
                            PropertyMappings.Add(((JsonPropertyAttribute)jsonProperty[0]).PropertyName, fieldInfo.Name);
                        }
                    }
                    var properties = type.GetProperties();
                    foreach (var propertyInfo in properties)
                    {
                        var jsonProperty = propertyInfo.GetCustomAttributes(typeof(JsonPropertyAttribute), false);
                        if (jsonProperty.Length > 0)
                        {
                            PropertyMappings.Add(((JsonPropertyAttribute)jsonProperty[0]).PropertyName, propertyInfo.Name);
                        }
                    }
                }
            }

            protected override string ResolvePropertyName(string propertyName)
            {
                string resolvedName;
                return PropertyMappings.TryGetValue(propertyName, out resolvedName) ? resolvedName : base.ResolvePropertyName(propertyName);
            }
        }
    }
}

namespace Hunt.RPG
{
    public static class HuntTablesGenerator
    {
        public static Dictionary<int, long> GenerateXPTable(int maxLevel, int baseExp, float levelMultiplier, int levelModule, float moduleReducer)
        {
            var xpTable = new Dictionary<int, long>();
            long currentLevel = baseExp;
            xpTable.Add(0, baseExp);
            for (var i = 1; i < maxLevel; i++)
            {
                if (i%levelModule == 0)
                    levelMultiplier -= moduleReducer;
                if (levelMultiplier < 1.01f) levelMultiplier = 1.01f;
                currentLevel = (long) (currentLevel*levelMultiplier);
                xpTable.Add(i, currentLevel);
            }
            return xpTable;
        }

        public static Dictionary<HRK, Skill> GenerateSkillTable()
        {
            var skillTable = new Dictionary<HRK, Skill>();
            var lumberJack = new Skill(HRK.Lumberjack, HMK.LumberjackDesc, 0, 20);
            var woodAndFleshModifier = new Modifier( /*HRK.Gather, */new[] { 1.035265f });
            lumberJack.AddModifier(HRK.Gather, woodAndFleshModifier);
            skillTable.Add(HRK.Lumberjack, lumberJack);
            var miner = new Skill(HRK.Miner, HMK.MinerDesc, 0, 20);
            miner.AddModifier(HRK.Gather, new Modifier( /*HRK.Gather, */new[] { 1.02048f }));
            skillTable.Add(HRK.Miner, miner);
            var hunter = new Skill(HRK.Hunter, HMK.HunterDesc, 0, 20);
            hunter.AddModifier(HRK.Gather, woodAndFleshModifier);
            skillTable.Add(HRK.Hunter, hunter);
            var gatherer = new Skill(HRK.Gatherer, HMK.GathererDesc, 0, 20);
            gatherer.AddModifier(HRK.Gather, woodAndFleshModifier);
            skillTable.Add(HRK.Gatherer, gatherer);
            var researcher = new Skill(HRK.Researcher, HMK.ResearcherDesc, 30, 5)
            {
                SkillPointsPerLevel = 7, Usage = HMK.ResearcherUsage
            };
            researcher.AddRequiredStat("int", researcher.RequiredLevel*3);
            researcher.AddModifier(HRK.Cooldown, new Modifier( /*HRK.Cooldown, */new[] {10f, 2f}));
            researcher.AddModifier(HRK.Chance, new Modifier( /*HRK.Chance, */new[] {.6f}));
            skillTable.Add(HRK.Researcher, researcher);
            var blacksmith = new Skill(HRK.Blacksmith, HMK.BlacksmithDesc, 30, 5)
            {
                SkillPointsPerLevel = 7
            };
            blacksmith.AddRequiredStat("str", (int) Math.Floor(blacksmith.RequiredLevel*2.5f));
            blacksmith.AddModifier(HRK.Chance, new Modifier( /*HRK.Chance, */new[] {.15f}));
            blacksmith.AddModifier(HRK.RessRate, new Modifier( /*HRK.RessRate, */new[] {.5f}));
            skillTable.Add(HRK.Blacksmith, blacksmith);
            var blinkarrow = new Skill(HRK.Blinkarrow, HMK.BlinkarrowDesc, 150, 5)
            {
                Usage = HMK.BlinkarrowUsage, SkillPointsPerLevel = 10, Enabled = false
            };
            blinkarrow.AddModifier(HRK.Cooldown, new Modifier( /*HRK.Cooldown, */new[] {9f, 2f}));
            blinkarrow.AddRequiredStat("agi", blinkarrow.RequiredLevel*2);
            skillTable.Add(HRK.Blinkarrow, blinkarrow);
            var tamer = new Skill(HRK.Tamer, HMK.TamerDesc, 50, 6)
            {
                SkillPointsPerLevel = 5, Usage = HMK.TamerUsage
            };
            skillTable.Add(HRK.Tamer, tamer);
            return skillTable;
        }

        public static Dictionary<string, ItemInfo> GenerateItemTable(Dictionary<string, ItemInfo> itemDict = null)
        {
            if (itemDict == null) itemDict = new Dictionary<string, ItemInfo>();
            var definitions = ItemManager.itemList;
            foreach (var definition in definitions)
            {
                var key = definition.shortname;
                itemDict.Add(key, new ItemInfo
                {
                    DisplayName = definition.displayName.translated,
                    CanResearch = true,
                    ItemCategory = definition.category.ToString()
                });
            }
            return itemDict;
        }

        public static Dictionary<string, int> GenerateResearchTable()
        {
            return new Dictionary<string, int>
            {
                {"Tool", 1},
                {"Attire", 2},
                {"Construction", 3},
                {"Resources", 3},
                {"Medical", 4},
                {"Ammunition", 4},
                {"Weapon", 5}
            };
        }

        public static Dictionary<int, string> GenerateTameTable()
        {
            return new Dictionary<int, string>
            {
                {1, HPK.CanTameChicken},
                {2, HPK.CanTameBoar},
                {3, HPK.CanTameStag},
                {4, HPK.CanTameWolf},
                {5, HPK.CanTameBear},
                {6, HPK.CanTameHorse}
            };
        }

        public static Dictionary<BuildingGrade.Enum, float> GenerateUpgradeBuildingTable()
        {
            return new Dictionary<BuildingGrade.Enum, float>
            {
                {BuildingGrade.Enum.Twigs, 1f},
                {BuildingGrade.Enum.Wood, 1.5f},
                {BuildingGrade.Enum.Stone, 3f},
                {BuildingGrade.Enum.Metal, 10f},
                {BuildingGrade.Enum.TopTier, 3f}
            };
        }

        public static Dictionary<string, object> GenerateDefaults()
        {
            return new Dictionary<string, object>
            {
                {HK.SkillPointsGain, HKD.SkillPointsGain},
                {HK.SkillPointsPerLevel, HKD.SkillPointsPerLevel},
                {HK.StatPointsGain, HKD.StatPointsGain},
                {HK.StatPointsPerLevel, HKD.StatPointsPerLevel}
            };
        }

        public static Dictionary<HRK, Modifier> GenerateMaxStatsTable()
        {
            return new Dictionary<HRK, Modifier>
            {
                {HRK.str_block_percent_gain, new Modifier(new[] {1.00095f, .5f})},
                {HRK.agi_evasion_percent_gain, new Modifier(new[] {1.000625f, .5f})},
                {HRK.int_crafting_reducer_percent, new Modifier(new[] {1.001f, .5f})}
            };
        }

        public static Dictionary<ResourceDispenser.GatherType, float> GenerateExpRateTable()
        {
            return new Dictionary<ResourceDispenser.GatherType, float>
            {
                {ResourceDispenser.GatherType.Tree, .5f},
                {ResourceDispenser.GatherType.Ore, .33f},
                {ResourceDispenser.GatherType.Flesh, 5f}
            };
        }

        public static List<string> GenerateAllowedEntites()
        {
            return new List<string>
            {
                "furnace",
                "furnace.large",
                "refinery_small_deployed",
                "mining.pumpjack",
                "mining_quarry"
            };
        }
    }


    public struct ItemInfo
    {
        public string DisplayName;
        public bool CanResearch;
        public string ItemCategory;
    }

    public class ProfilePreferences
    {
        [JsonProperty("sxpmp")]
        public float ShowXPMessagePercent;
        [JsonProperty("sp")]
        public bool ShowProfile;
        [JsonProperty("scm")]
        public bool ShowCraftMessage;
        [JsonProperty("sh")]
        public uint ShowHud;
        [JsonProperty("uba")]
        public bool UseBlinkArrow;
        [JsonProperty("atba")]
        public bool AutoToggleBlinkArrow;

        public ProfilePreferences(uint defaultHud, bool showProfile)
        {
            ShowXPMessagePercent = .25f;
            ShowProfile = showProfile;
            ShowCraftMessage = true;
            ShowHud = defaultHud;
            UseBlinkArrow = true;
            AutoToggleBlinkArrow = true;
        }
    }

    public class GUIInfo
    {
        public string LastMain;
        public string LastContent;
        public string LastInfo;
        public string LastStats;
        public string LastSkills;
        public string LastHud;
        public string LastHudFirst;
        public string LastHudSecond;
        public float LastHudTime;
        public Timer LastHudTimer;
    }

    public class RPGInfo
    {
        private float evasionCache;
        private int agility;
        [JsonProperty("agi")]
        public int Agility
        {
            get { return agility; }
            set
            {
                agility = value;
                evasionCache = -1f;
            }
        }

        private float blockCache;
        private int strength;
        [JsonProperty("str")]
        public int Strength
        {
            get { return strength; }
            set
            {
                strength = value;
                blockCache = -1f;
            }
        }

        private float craftCache;
        private int intelligence;
        [JsonProperty("int")]
        public int Intelligence
        {
            get { return intelligence; }
            set
            {
                intelligence = value;
                craftCache = -1f;
            }
        }

        [JsonProperty("sn")]
        public string SteamName;
        [JsonProperty("l")]
        public int Level;
        [JsonProperty("xp")]
        public long Experience;
        [JsonProperty("statp")]
        public int StatsPoints;
        [JsonProperty("skillp")]
        public int SkillPoints;
        [JsonProperty("s")]
        public Dictionary<HRK, int> Skills;
        [JsonProperty("p")]
        public ProfilePreferences Preferences;
        [JsonProperty("ls")]
        public long LastSeen;
        private ulong userId;
        public static long[] XPTable = new long[0];
        public static Dictionary<HRK, Modifier> MaxStatsTable = new Dictionary<HRK, Modifier>();
        public static Dictionary<int, string> TameTable = new Dictionary<int, string>();
        public static float SkillPointsGain = HKD.SkillPointsGain;
        public static float StatPointsGain = HKD.StatPointsGain;
        public static int SkillPointsPerLevel = HKD.SkillPointsPerLevel;
        public static int StatPointsPerLevel = HKD.StatPointsPerLevel;
        public static Permission Perm;
        private static int[] skillCostCache;
        private static int[] statCostCache;

        public RPGInfo(string steamName, uint defaultHud, bool showProfile)
        {
            SteamName = steamName;
            Level = 0;
            Skills = new Dictionary<HRK, int>();
            Preferences = new ProfilePreferences(defaultHud, showProfile);
        }

        public void SetUserId(ulong userId)
        {
            this.userId = userId;
        }

        public bool AddExperience(long xp)
        {
            if (Level >= XPTable.Length) return false;
            Experience += xp;
            var requiredXp = XPTable[Level];
            if (Experience < requiredXp) return false;
            Experience -= requiredXp;
            LevelUp();
            return true;
        }

        public void LevelUp(int desiredLevel)
        {
            var levelsToUp = desiredLevel - Level;
            for (var i = 0; i < levelsToUp; i++)
                AddExperience(RequiredExperience());
        }

        public long RequiredExperience()
        {
            return Level >= XPTable.Length ? 0 : XPTable[Level];
        }

        public void Died(float percent)
        {
            Experience -= (long) (Experience*percent);
            if (Experience < 0)
                Experience = 0;
        }

        private void LevelUp()
        {
            Level++;
            Agility++;
            Strength++;
            Intelligence++;
            StatsPoints += StatPointsPerLevel;
            SkillPoints += SkillPointsPerLevel;
            if (Level >= XPTable.Length) Experience = 0;
        }

        public bool AddStat(HRK stat, int points)
        {
            if (stat != HRK.Agi && stat != HRK.Int && stat != HRK.Str) return false;
            points = Math.Abs(points);
            if (StatsPoints < points) return false;
            var pointsCost = GetStatPointsCost(stat, points);
            if (StatsPoints < pointsCost) return false;
            switch (stat)
            {
                case HRK.Agi:
                    Agility += points;
                    break;
                case HRK.Int:
                    Intelligence += points;
                    break;
                case HRK.Str:
                    Strength += points;
                    break;
                default:
                    return false;
            }
            StatsPoints -= pointsCost;
            return true;
        }

        public void Reset()
        {
            Level = 0;
            ResetStats();
        }

        public void ResetStats()
        {
            ResetSkills();
            StatsPoints = Level*StatPointsPerLevel;
            Agility = Intelligence = Strength = Level;
        }

        public void ResetSkills()
        {
            int skillLevel;
            if (Skills.TryGetValue(HRK.Tamer, out skillLevel))
            {
                Perm.RevokeUserPermission(userId.ToString(), HPK.CanTame);
                for (var j = 1; j <= skillLevel; j++)
                    Perm.RevokeUserPermission(userId.ToString(), TameTable[j]);
            }
            Skills.Clear();
            SkillPoints = Level*SkillPointsPerLevel;
        }

        public int AddSkill(Skill skill, int level, out HMK reason, Plugin pets = null)
        {
            int existingLevel;
            Skills.TryGetValue(skill.Type, out existingLevel);
            if (existingLevel >= skill.MaxLevel)
            {
                reason = HMK.AlreadyAtMaxLevel;
                return 0;
            }
            var levelsToAdd = Math.Abs(level);
            if (levelsToAdd + existingLevel > skill.MaxLevel) levelsToAdd = skill.MaxLevel - existingLevel;
            if (levelsToAdd <= 0)
            {
                reason = HMK.Empty;
                return 0;
            }
            var requiredPoints = GetSkillPointsCost(skill, existingLevel, levelsToAdd);
            if (SkillPoints < requiredPoints)
            {
                reason = HMK.NotEnoughPoints;
                return 0;
            }
            if (Level < skill.RequiredLevel)
            {
                reason = HMK.NotEnoughLevels;
                return 0;
            }
            if (skill.RequiredSkills.Count > existingLevel)
                foreach (var requiredSkill in skill.RequiredSkills[existingLevel])
                {
                    int tempLevel;
                    if (!Skills.TryGetValue(requiredSkill.Key, out tempLevel) || tempLevel < requiredSkill.Value)
                    {
                        reason = HMK.NotEnoughSkill;
                        return 0;
                    }
                }
            if (skill.RequiredStats.Count > existingLevel)
                foreach (var requiredStat in skill.RequiredStats[existingLevel])
                {
                    switch (requiredStat.Key.ToLower())
                    {
                        case "str":
                            if (Strength < requiredStat.Value)
                            {
                                reason = HMK.NotEnoughStrength;
                                return 0;
                            }
                            break;
                        case "agi":
                            if (Agility < requiredStat.Value)
                            {
                                reason = HMK.NotEnoughAgility;
                                return 0;
                            }
                            break;
                        case "int":
                            if (Intelligence < requiredStat.Value)
                            {
                                reason = HMK.NotEnoughIntelligence;
                                return 0;
                            }
                            break;
                        default:
                            reason = HMK.InvalidCommand;
                            return 0;
                    }
                }
            if (existingLevel > 0)
            {
                if (levelsToAdd > 0)
                    Skills[skill.Type] += levelsToAdd;
            }
            else
            {
                Skills.Add(skill.Type, levelsToAdd);
            }
            if (skill.Type == HRK.Tamer && pets != null)
            {
                var skillLevel = Skills[HRK.Tamer];
                Perm.GrantUserPermission(userId.ToString(), HPK.CanTame, pets);
                for (var j = 1; j <= skillLevel; j++)
                    Perm.GrantUserPermission(userId.ToString(), TameTable[j], pets);
            }
            reason = HMK.Empty;
            SkillPoints -= requiredPoints;
            return levelsToAdd;
        }

        public float GetEvasion()
        {
            if (evasionCache >= 0) return evasionCache;
            var args = MaxStatsTable[HRK.agi_evasion_percent_gain].Args;
            evasionCache = Mathf.Clamp((float) (Math.Pow(args[0], Agility) - 1), 0, args[1]);
            return evasionCache;
        }

        public float GetBlock()
        {
            if (blockCache >= 0) return blockCache;
            var args = MaxStatsTable[HRK.str_block_percent_gain].Args;
            blockCache = Mathf.Clamp((float) (Math.Pow(args[0], Strength) - 1), 0, args[1]);
            return blockCache;
        }

        public float GetCraftingReducer()
        {
            if (craftCache >= 0) return craftCache;
            var args = MaxStatsTable[HRK.int_crafting_reducer_percent].Args;
            craftCache = Mathf.Clamp((float) (Math.Pow(args[0], Intelligence) - 1), 0, args[1]);
            return craftCache;
        }

        public int GetSkillPointsCostNext(Skill skill, int levelsToAdd = 1)
        {
            int existingLevel;
            Skills.TryGetValue(skill.Type, out existingLevel);
            levelsToAdd = Math.Abs(levelsToAdd);
            if (levelsToAdd + existingLevel > skill.MaxLevel) levelsToAdd = skill.MaxLevel - existingLevel;
            if (levelsToAdd < 0) levelsToAdd = 1;
            return GetSkillPointsCost(skill, existingLevel, levelsToAdd);
        }

        private static int GetSkillPointsCost(Skill skill, int level, int add = 1)
        {
            if (SkillPointsGain <= 1) return skill.SkillPointsPerLevel * add;
            if (skillCostCache == null || skillCostCache.Length < XPTable.Length)
            {
                skillCostCache = new int[XPTable.Length];
                for (var i = 0; i < XPTable.Length; i++)
                    skillCostCache[i] = Math.Max((int)Math.Ceiling(Math.Pow(SkillPointsGain, i) - 1), 1);
            }
            var target = level + add;
            if (target > skillCostCache.Length)
            {
                var size = skillCostCache.Length - 1;
                Array.Resize(ref skillCostCache, target);
                for (var i = size; i < target; i++)
                    skillCostCache[i] = Math.Max((int)Math.Ceiling(Math.Pow(SkillPointsGain, i) - 1), 1);
            }
            var points = 0;
            for (var i = level; i < target; i++)
                points += skill.SkillPointsPerLevel + skillCostCache[i];
            return points;
        }

        public int GetStatPointsCost(HRK stat, int add = 1)
        {
            if (StatPointsGain <= 1) return add;
            var level = 0;
            switch (stat)
            {
                case HRK.Agi:
                    level = Agility;
                    break;
                case HRK.Int:
                    level = Intelligence;
                    break;
                case HRK.Str:
                    level = Strength;
                    break;
            }
            level -= Level;
            if (level < 0) return 1;
            if (statCostCache == null || statCostCache.Length < XPTable.Length)
            {
                statCostCache = new int[XPTable.Length];
                for (var i = 0; i < XPTable.Length; i++)
                    statCostCache[i] = Math.Max((int)Math.Ceiling(Math.Pow(StatPointsGain, i) - 1), 1);
            }
            var target = level + add;
            if (target > statCostCache.Length)
            {
                var size = statCostCache.Length - 1;
                Array.Resize(ref statCostCache, target);
                for (var i = size; i < target; i++)
                    statCostCache[i] = Math.Max((int)Math.Ceiling(Math.Pow(StatPointsGain, i) - 1), 1);
            }
            var points = 0;
            for (var i = level; i < target; i++)
                points += statCostCache[i];
            return points;
        }

        public static void OnUnload()
        {
            MaxStatsTable = null;
            TameTable = null;
            XPTable = null;
            skillCostCache = null;
            statCostCache = null;
            Perm = null;
        }
    }

    public class Skill
    {
        public string Name;
        public HRK Type;
        public bool Enabled;
        public HMK Description;
        public HMK Usage;
        public int RequiredLevel;
        public int MaxLevel;
        public List<Dictionary<HRK, int>> RequiredSkills;
        public Dictionary<HRK, Modifier> Modifiers;
        public List<Dictionary<string, int>> RequiredStats;
        public int SkillPointsPerLevel;

        public Skill(HRK type, HMK description, int requiredLevel, int maxLevel)
        {
            Name = type.ToString();
            Type = type;
            Enabled = true;
            Description = description;
            RequiredLevel = requiredLevel;
            MaxLevel = maxLevel;
            RequiredSkills = new List<Dictionary<HRK, int>>();
            Modifiers = new Dictionary<HRK, Modifier>();
            RequiredStats = new List<Dictionary<string, int>>();
            SkillPointsPerLevel = 1;
        }

        public void AddRequiredStat(string stat, int points, int level = 1)
        {
            while (RequiredStats.Count < level)
                RequiredStats.Add(new Dictionary<string, int>());
            if (!RequiredStats[level - 1].ContainsKey(stat))
                RequiredStats[level - 1].Add(stat, points);
        }

        public void AddRequiredSkill(HRK skillName, int pointsNeeded, int level = 1)
        {
            while (RequiredSkills.Count < level)
                RequiredSkills.Add(new Dictionary<HRK, int>());
            if (!RequiredSkills[level - 1].ContainsKey(skillName))
                RequiredSkills[level - 1].Add(skillName, pointsNeeded);
        }

        public void AddModifier(HRK modifier, Modifier handler)
        {
            if (!Modifiers.ContainsKey(modifier))
                Modifiers.Add(modifier, handler);
        }
    }

    public struct Modifier
    {
        public Modifier( /*HRK identifier, */ float[] args)
        {
            //Identifier = identifier;
            Args = args;
        }

        //public HRK Identifier;

        public float[] Args;
    }

    public class HuntData
    {
        [JsonProperty(HK.Furnaces)]
        public Dictionary<string, ulong> Furnaces { get; } = new Dictionary<string, ulong>();

        [JsonProperty(HK.Quarries)]
        public Dictionary<string, ulong> Quarries { get; } = new Dictionary<string, ulong>();

        [JsonProperty(HK.Profile)]
        public Dictionary<ulong, RPGInfo> Profiles { get; } = new Dictionary<ulong, RPGInfo>();
    }

    public class HuntDefaults
    {
        public float SkillPointsGain;
        public float StatPointsGain;
        public int SkillPointsPerLevel;
        public int StatPointsPerLevel;
        public HuntDefaults()
        {
            SkillPointsGain = HKD.SkillPointsGain;
            StatPointsGain = HKD.StatPointsGain;
            SkillPointsPerLevel = HKD.SkillPointsPerLevel;
            StatPointsPerLevel = HKD.StatPointsPerLevel;
        }
    }
}

namespace Hunt.RPG.Keys
{
    //config keys
    static class HK
    {
        public const string Defaults = "Defaults";
        public const string SkillPointsGain = "SkillPointsGain";
        public const string StatPointsGain = "StatPointsGain";
        public const string DeleteProfileAfterOfflineDays = "DeleteProfileAfterOfflineDays";
        public const string ShowHud = "SHOWHUD";
        public const string ShowProfile = "SHOWPROFILE";
        public const string DefaultHud = "DEFAULTHUD";
        public const string ConfigVersion = "VERSION";
        public const string DataVersion = "DATA_VERSION";
        public const string DataFileName = "Hunt_Data";
        public const string Profile = "PROFILE";
        public const string Furnaces = "FURNACES";
        public const string Quarries = "QUARRIES";
        public const string ChatPrefix = "CHATPREFIX";
        public const string XPTable = "XPTABLE";
        public const string ExpRateTable = "EXPRATETABLE";
        public const string MaxStatsTable = "MAXSTATSTABLE";
        public const string SkillTable = "SKILLTABLE";
        public const string ItemTable = "ITEMTABLE";
        public const string ResearchSkillTable = "RESEARCHSKILLTABLE";
        public const string TameTable = "TAMETABLE";
        public const string UpgradeBuildTable = "UPGRADEBUILDTABLE";
        public const string AllowedEntities = "ALLOWEDENTITIES";
        public const string AdminReset = "ADMINRESET";
        public const string NightXP = "NIGHTXP";
        public const string Trainer = "TRAINER";
        public const string DeathReducerK = "DEATHREDUCER";
        public const string SkillPointsPerLevel = "SkillPointsPerLevel";
        public const string StatPointsPerLevel = "StatPointsPerLevel";

    }

    //defaults
    static class HKD
    {
        public const int MaxLevel = 200;
        public const float SkillPointsGain = 1.0975f;
        public const float StatPointsGain = 1.015f;
        public const int SkillPointsPerLevel = 2;
        public const int StatPointsPerLevel = 3;
        public const int BaseXP = 383;
        public const float LevelMultiplier = 1.105f;
        public const int LevelModule = 10;
        public const float ModuleReducer = .0055f;
        public const float DeathReducer = .05f;
    }

    [JsonConverter(typeof (StringEnumConverter))]
    public enum HMK
    {
        None,
        About,
        Agi,
        AgiColor,
        AlreadyAtMaxLevel,
        AvailableSkills,
        BlinkStatus,
        BlinkToggle,
        BlinkedRecently,
        BuildingOwnersPlugin,
        ButtonClose,
        ButtonResetSkills,
        ButtonResetStats,
        CantBlink,
        CantBlinkOther,
        CraftMessage,
        CraftingEnd,
        CraftingReducer,
        CurrentXp,
        DamageBlock,
        DataUpdated,
        Died,
        Dodged,
        Empty,
        EvasionChance,
        GenerateXp,
        Help,
        IdAlreadyExists,
        Int,
        IntColor,
        InvalidCommand,
        InvalidSkillName,
        NotFoundItem,
        Level,
        LevelUp,
        NeedNpc,
        NightXp,
        NotAnAdmin,
        NotEnoughAgility,
        NotEnoughIntelligence,
        NotEnoughLevels,
        NotEnoughPoints,
        NotEnoughStrength,
        Off,
        On,
        PetsPlugin,
        PlayerLevelUp,
        NotFoundPlayer,
        ProfilePreferences,
        ProfileMessage,
        ResearchBlocked,
        ResearchFail,
        ResearchItem,
        ResearchReuse,
        ResearchSkill,
        ResearchSuccess,
        ResearchType,
        Shortcuts,
        SkillDisabled,
        SkillInfo,
        NotLearnedSkill,
        SkillPoints,
        SkillReset,
        SkillResetPlayer,
        StatPoints,
        StatReset,
        StatResetPlayer,
        Str,
        StrColor,
        XpMessage,
        NotEnoughSkill,
        SkillsHeader,
        Loaded,
        StatusLoad,
        StatusSave,
        TopPlayer,
        ProfileHeader,
        SkillUp,
        SkillInfoHeader,
        Usage,
        LevelShort,
        SkillCost,
        LumberjackDesc,
        MinerDesc,
        HunterDesc,
        GathererDesc,
        BlacksmithDesc,
        ResearcherDesc,
        TamerDesc,
        BlinkarrowDesc,
        ResearcherUsage,
        TamerUsage,
        BlinkarrowUsage
    }

    //permission keys
    public static class HPK
    {
        public const string CanTame = "cannpc";
        public const string CanTameChicken = "canchicken";
        public const string CanTameBoar = "canboar";
        public const string CanTameStag = "canstag";
        public const string CanTameWolf = "canwolf";
        public const string CanTameBear = "canbear";
        public const string CanTameHorse = "canhorse";
    }

    [JsonConverter(typeof (StringEnumConverter))]
    public enum HRK
    {
        Tamer,
        Blinkarrow,
        Blacksmith,
        Researcher,
        Lumberjack,
        Miner,
        Hunter,
        Gatherer,
        Gather,
        Cooldown,
        int_crafting_reducer_percent,
        agi_evasion_percent_gain,
        str_block_percent_gain,
        Agi,
        Int,
        Str,
        Chance,
        RessRate
    }
}
