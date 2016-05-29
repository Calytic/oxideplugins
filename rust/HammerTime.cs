using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Hammer Time", "Shady", "1.0.6", ResourceId = 1711)]
    [Description("Tweak settings for building blocks like demolish time, and rotate time.")]
    class HammerTime : RustPlugin
    {
        bool configWasChanged = false;

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

        void OnServerInitialized()
        {
            var doDemo = (bool)Config["AllowDemolishAfterServerRestart"];
            var doRotate = (bool)Config["AllowRotateAfterServerRestart"];
            if (!doDemo && !doRotate) return;
            var blocks = GameObject.FindObjectsOfType<BuildingBlock>();
            foreach(var block in blocks)
            {
                var name = block?.LookupShortPrefabName() ?? string.Empty;
                if (string.IsNullOrEmpty(name)) continue;
                var grade = block.grade.ToString();
                if (grade.ToLower().Contains("twig")) continue; //ignore twigs (performance)
                if (block.Health() <= block.MaxHealth() / 2.75f) continue; //ignore blocks that are weak (performance)
                   
                if (name.Contains("foundation") || name.Contains("pillar") || name.Contains("roof") || name.Contains("floor")) doRotate = false;
                DoInvokes(block, doDemo, doRotate, false);
            }
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
            TryParseFloat(Config["DemolishTime"].ToString(), ref demoTime);
            TryParseFloat(Config["RotateTime"].ToString(), ref rotateTime);
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
        }
        
    
        private void OnEntityBuilt(Planner plan, GameObject objectBlock)
        {
            var GetTypeString = objectBlock?.ToBaseEntity()?.GetType()?.ToString();
            var isBuildingBlock = GetTypeString == "BuildingBlock";
            if (!isBuildingBlock) return;
            var block = (BuildingBlock)objectBlock.ToBaseEntity();
            if (block == null) return;
            var name = block?.LookupShortPrefabName() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) return;
            var doRotate = true;
            if (name.Contains("foundation") || name.Contains("pillar") || name.Contains("floor") || name.Contains("roof")) doRotate = false;
            DoInvokes(block, true, doRotate, true);
        }

        private void OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (block == null) return;
            var name = block?.LookupShortPrefabName() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) return;
            var doRotate = true;
            if (name.Contains("foundation") || name.Contains("pillar") || name.Contains("floor") || name.Contains("roof")) doRotate = false;   
            DoInvokes(block, false, doRotate, false);
        }

       object OnStructureRepair(BaseCombatEntity block, BasePlayer player)
        {
            if (block == null || player == null) return null;
            var cooldown = 8f;
            TryParseFloat(Config["RepairDamageCooldown"].ToString(), ref cooldown);
            if (cooldown < 1f) cooldown = 0f;
            if (cooldown == 8f) return null;
            if (block.TimeSinceAttacked() < cooldown) return false;
            return null;
        }

        object OnHammerHit(BasePlayer player, HitInfo hitInfo)
        {
            var entity = hitInfo?.HitEntity?.GetComponent<BaseCombatEntity>() ?? null;
            if (entity == null) return null;
            var cooldown = 8f;
            TryParseFloat(Config["RepairDamageCooldown"].ToString(), ref cooldown);
            if (cooldown < 1f) cooldown = 0f;
            if (cooldown == 8f) return null;
            if (entity.TimeSinceAttacked() < cooldown) return false;
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

        public bool TryParseFloat(string text, ref float value)
        {
            float tmp;
            if (float.TryParse(text, out tmp))
            {
                value = tmp;
                return true;
            }
            else return false;
        }

        public bool TryParseInt(string text, ref int value)
        {
            int tmp;
            if (int.TryParse(text, out tmp))
            {
                value = tmp;
                return true;
            }
            else return false;
        }


    }
}