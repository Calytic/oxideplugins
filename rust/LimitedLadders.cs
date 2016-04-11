using System.Linq;
using System.Text.RegularExpressions;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LimitedLadders", "DefaultPlayer(VVoid)", "1.0.2", ResourceId = 1051)]
    public class LimitedLadders : RustPlugin
    {
        private const string LadderPrefabs = "assets/bundled/prefabs/items/ladders/";
        private readonly int LayerMasks = LayerMask.GetMask("Construction");
        private bool DisableOnlyOnConstructions;
        private string BuildingBlockedMsg;
        private bool AllowOnExternalWalls; // TODO

        private void Cfg<T>(string key, ref T var)
        {
            if (Config[key] is T)
                var = (T) Config[key];
            else
            {
                Config[key] = var;
                SaveConfig();
            }
        }

        private void Init()
        {
            Cfg("DisableOnlyOnConstructions", ref DisableOnlyOnConstructions);
            Cfg("BuildingBlockedMsg", ref BuildingBlockedMsg);
            //Cfg("AllowOnExternalWalls", ref AllowOnExternalWalls);
        }

        protected override void LoadDefaultConfig()
        {
            Config["DisableOnlyOnConstructions"] = false;
            Config["BuildingBlockedMsg"] = "Building is blocked!";
            //Config["AllowOnExternalWalls"] = false;
        }

        private void OnServerInitialized()
        {
            PrefabAttribute.server.GetAll<Construction>().Where(pref => pref.fullName.StartsWith(LadderPrefabs))
                .ToList().ForEach(ladder => ladder.canBypassBuildingPermission = DisableOnlyOnConstructions);
        }

        [HookMethod("OnEntityBuilt")]
        private void OnEntityBuilt(HeldEntity heldentity, GameObject gameobject)
        {
            if (!DisableOnlyOnConstructions)
                return;
            var player = heldentity.ownerPlayer;
            if (player.CanBuild())
                return;
            var entity = gameobject.GetComponent<BaseCombatEntity>();
            if (!entity || !entity.LookupPrefabName().StartsWith(LadderPrefabs))
                return;
            if (Physics.CheckSphere(entity.transform.position, 1.2f, LayerMasks))
            {
                entity.Kill(BaseNetworkable.DestroyMode.Gib);
                player.ChatMessage(BuildingBlockedMsg);
                TryReturnLadder(player, entity);
            }
        }

        private static void TryReturnLadder(BasePlayer player, BaseCombatEntity entity)
        {
            var item = ItemManager.CreateByName(Regex.Replace(entity.LookupShortPrefabName(), @"\.prefab$", string.Empty));
            if(item != null)
                player.GiveItem(item);
        }
    }
}