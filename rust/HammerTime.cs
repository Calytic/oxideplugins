using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Hammer Time", "Shady", "1.0.4", ResourceId = 1711)]
    [Description("Tweak settings for building blocks like demolish time, and rotate time.")]
    class HammerTime : RustPlugin
    {
        bool configWasChanged = false;
        private List<BuildingBlock> entityBuiltList = new List<BuildingBlock>();


        /*--------------------------------------------------------------//
		//			Load up the default config on first use				//
		//--------------------------------------------------------------*/
        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["DemolishTime"] = 600f;
            Config["RotateTime"] = 600f;
            Config["MustOwnToDemolish"] = false;
            Config["MustOwnToRotate"] = false;
            Config["AllowDemolishAfterServerRestart"] = false;
            Config["AllowRotateAfterServerRestart"] = true;
            Config["AuthLevelOverrideDemolish"] = true;
            Config["RepairDamageCooldown"] = 8f;
            SaveConfig();
        }

        void CheckConfigEntry<T>(string key, T value)
        {
            if (Config[key] == null)
            {
                Config[key] = value;
                configWasChanged = true;
            }
        }
        private void Init()
        {
            LoadDefaultMessages();
            CheckConfigEntry("DemolishTime", 600f);
            CheckConfigEntry("RotateTime", 600f);
            CheckConfigEntry("MustOwnToDemolish", false);
            CheckConfigEntry("MustOwnToRotate", false);
            CheckConfigEntry("AllowDemolishAfterServerRestart", false);
            CheckConfigEntry("AllowRotateAfterServerRestart", true);
            CheckConfigEntry("AuthLevelOverrideDemolish", true);
            CheckConfigEntry("RepairDamageCooldown", 8f);
            if (configWasChanged) SaveConfig();
        }

        /*--------------------------------------------------------------//
        //			Localization Stuff			                        //
        //--------------------------------------------------------------*/

        private void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                //DO NOT EDIT LANGUAGE FILES HERE! Navigate to oxide\lang\HammerTime.en.json
                {"doesNotOwnDemo", "You can only demolish objects you own!"},
                {"doesNotOwnRotate", "You can only rotate objects you own!" }
            };
            lang.RegisterMessages(messages, this);
        }

        private string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);

        void DoInvokes(BuildingBlock block, bool demo, bool rotate, bool justCreated)
        {
            var demoTime = 600f;
            var rotateTime = 600f;
            float.TryParse(Config["DemolishTime"].ToString(), out demoTime);
            float.TryParse(Config["RotateTime"].ToString(), out rotateTime);
            if (demo)
            {
                if (demoTime < 0)
                {
                    block.CancelInvoke("StopBeingDemolishable");
                    block.SetFlag(BaseEntity.Flags.Reserved2, true); //reserved2 is demolishable
                    block.SendNetworkUpdateImmediate(justCreated);
                }
                if (demoTime == 0) block.Invoke("StopBeingDemolishable", 0.01f);
                if (demoTime >= 1 && demoTime != 600) //if time is = to 600, then it's default, and there's no point in changing anything
                {
                    block.CancelInvoke("StopBeingDemolishable");
                    block.SetFlag(BaseEntity.Flags.Reserved2, true); //reserved2 is demolishable
                    block.Invoke("StopBeingDemolishable", demoTime);
                    block.SendNetworkUpdateImmediate(justCreated);
                }
            }
            if (rotate)
            {
                if (rotateTime < 0)
                {
                    block.CancelInvoke("StopBeingRotatable");
                    block.SetFlag(BaseEntity.Flags.Reserved1, true); //reserved1 is rotatable
                    block.SendNetworkUpdateImmediate(justCreated);
                }
                    if (rotateTime == 0) block.Invoke("StopBeingRotatable", 0.01f);
                if (rotateTime >= 1 && rotateTime != 600) //if time is = to 600, then it's default, and there's no point in changing anything
                {
                    block.CancelInvoke("StopBeingRotatable");
                    block.SetFlag(BaseEntity.Flags.Reserved1, true); //reserved1 is rotatable
                    block.Invoke("StopBeingRotatable", rotateTime);
                    block.SendNetworkUpdateImmediate(justCreated);
                }
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            var getType = entity?.GetType()?.ToString() ?? string.Empty;
            if (getType != "BuildingBlock") return;
            var block = entity?.GetComponent<BuildingBlock>() ?? null;
            if (block == null) return;
            entityBuiltList.Remove(block);
        }

            void OnEntitySpawned(BaseNetworkable entity)
        {
            //Use next tick to make sure it's partially delayed in case list is slow at updating (unknown)
            NextTick(() =>
            {
                if (entity == null) return;
                var doDemolish = false;
                var doRotate = true;
                var getType = entity?.GetType()?.ToString() ?? string.Empty;
                if (!getType.Contains("BuildingB")) return;
                var block = entity?.GetComponent<BuildingBlock>() ?? null;
                if (block == null) return;
                if (entityBuiltList.Contains(block)) return;
                if ((bool)Config["AllowDemolishAfterServerRestart"]) doDemolish = true;
                if (!(bool)Config["AllowRotateAfterServerRestart"]) doRotate = false;
                timer.Once(7f, () =>
                {
                    if (block == null) return;
                    DoInvokes(block, doDemolish, doRotate, true);
                });
            });      
        }

        private void OnEntityBuilt(Planner plan, GameObject objectBlock)
        {
            var GetTypeString = objectBlock?.ToBaseEntity()?.GetType()?.ToString();
            var isBuildingBlock = GetTypeString == "BuildingBlock";
            if (!isBuildingBlock) return;
            var block = (BuildingBlock)objectBlock.ToBaseEntity();
            if (block == null) return;
            DoInvokes(block, true, true, true);
            entityBuiltList.Add(block);
        }

        private void OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (block == null) return;
            DoInvokes(block, false, true, false);
        }

       object OnStructureRepair(BuildingBlock block, BasePlayer player)
        {
            var cooldown = 0f;
            float.TryParse(Config["RepairDamageCooldown"].ToString(), out cooldown);
            if (cooldown == 0f || cooldown == 8f) return null;
            if (block.TimeSinceAttacked() <= cooldown) return false;
            return null;
        }

        object OnStructureDemolish(BuildingBlock block, BasePlayer player)
        {
            if (!(bool)Config["MustOwnToDemolish"]) return null;
            if ((bool)Config["AuthLevelOverrideDemolish"] && player.IsAdmin()) return null;
            if (permission.UserHasPermission(player.userID.ToString(), "hammertime.allowdemo")) return null;
            if (block.OwnerID == 0 || player.userID == 0) return null;
            if (block.OwnerID != player.userID)
            {
                SendReply(player, GetMessage("doesNotOwnDemo"));
                return true;
            }
            if (entityBuiltList.Contains(block)) entityBuiltList.Remove(block);
            return null;
        }

        object OnStructureRotate(BuildingBlock block, BasePlayer player)
        {
            if (!(bool)Config["MustOwnToRotate"]) return null;
            if (block.OwnerID == 0 || player.userID == 0) return null;
            if (block.OwnerID != player.userID)
            {
                SendReply(player, GetMessage("doesNotOwnRotate"));
                return true;
            }
                
            return null;
        }

    }
}