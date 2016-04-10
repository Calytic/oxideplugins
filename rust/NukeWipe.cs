using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Facepunch;
using System;

namespace Oxide.Plugins
{
    [Info("NukeWipe", "LaserHydra", "1.0.0", ResourceId = 0)]
    [Description("Wipe with style - and propably lag lol")]
    class NukeWipe : RustPlugin
    {
        #region Global Declaration

        Type[] DestroyableTypes = new Type[]
        {
            typeof(BuildingBlock),
            typeof(Barricade),
            typeof(BaseOven),
            typeof(Door),
            typeof(StorageContainer),
            typeof(BuildingPrivlidge),
            typeof(SimpleBuildingBlock),
            typeof(Signage),
            typeof(RepairBench),
            typeof(ResearchTable),
            typeof(DroppedItem),
            typeof(MiningQuarry),
            typeof(WaterCatcher),
            typeof(AutoTurret),
            typeof(BaseCombatEntity)
        };

        #endregion

        #region Plugin General

        ////////////////////////////////////////
        ///     Plugin Related Hooks
        ////////////////////////////////////////

        void Loaded()
        {
#if !RUST
            throw new NotSupportedException("This plugin or the version of this plugin does not support this game!");
#endif

            RegisterPerm("use");

            LoadMessages();
            LoadConfig();
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Settings", "Time Until Explosion", 20f);

            SaveConfig();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Nuke About To Happen", "<color=red>A NUCLEAR EXPLOSION IS ABOUT TO HAPPEN!</color>"},
                {"Nuke Done", "<color=red>A NUCLEAR EXPLOSION DESTROYED EVERYTHING AND KILLED EVERYBODY!</color>"}
            }, this);
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");
        #endregion

        #region Commands

        [ChatCommand("nuke")]
        void cmdNuke(BasePlayer player)
        {
            if(!HasPerm(player.userID, "use"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.userID));
                return;
            }

            BroadcastChat(GetMsg("Nuke About To Happen"));

            timer.Once(GetConfig(20f, "Settings", "Time Until Explosion"), () => Nuke());
        }

        #endregion

        #region Subject Related

        void Nuke()
        {
            //  Make all Players explode
            foreach (var player in CopyList(BasePlayer.activePlayerList))
                Explode(player);

            //  Make all Sleepers explode
            foreach (var player in CopyList(BasePlayer.sleepingPlayerList))
                Explode(player);

            //  Get all Entities
            var entities = GetEntities<BaseEntity>(new Vector3(0, 0, 0), Convert.ToInt32(ConVar.Server.worldsize * 0.8f));
            
            //  Kill all Entities which should be wiped
            foreach (var building in (from entity in entities where ShouldBeDestroyed(entity) select entity))
                building.Kill(BaseNetworkable.DestroyMode.None);

            Pool.FreeList(ref entities);

            //  Remove Corpses
            timer.Once(5.01f, () =>
            {
                //  Get all Corpses
                List<BaseCorpse> corpses = GetEntities<BaseCorpse>(Vector3.zero, Convert.ToInt32(ConVar.Server.worldsize * 0.8f));

                //  Kill all Corpses
                foreach (var corpse in corpses)
                    corpse.Kill(BaseNetworkable.DestroyMode.None);

                Pool.FreeList(ref corpses);

                BroadcastChat(GetMsg("Nuke Done"));
            });
        }

        void Explode(BasePlayer player)
        {
            //  Initialize DamageTypeEntry - Explosion
            Rust.DamageTypeEntry dmg = new Rust.DamageTypeEntry();
            dmg.amount = 100;
            dmg.type = Rust.DamageType.Generic;

            //  Fire Effects
            Effect.server.Run("assets/bundled/prefabs/fx/fire/fire_v2.prefab", player.transform.position, Vector3.up);
            Effect.server.Run("assets/bundled/prefabs/fx/fire/fire_v2.prefab", player.transform.position + new Vector3(1, 0, 1), Vector3.up);
            Effect.server.Run("assets/bundled/prefabs/fx/fire/fire_v2.prefab", player.transform.position + new Vector3(1, 0, 0), Vector3.up);
            Effect.server.Run("assets/bundled/prefabs/fx/fire/fire_v2.prefab", player.transform.position + new Vector3(0, 0, 1), Vector3.up);

            //  Explosion Effect
            Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", player.transform.position, Vector3.up, null, true);

            //  Initialize HitInfo
            HitInfo hitInfo = new HitInfo()
            {
                Initiator = null,
                WeaponPrefab = null
            };

            //  Add DamageTypeEntry to HitInfo
            hitInfo.damageTypes.Add(new List<Rust.DamageTypeEntry> { dmg });

            //  Hurt Player
            DamageUtil.RadiusDamage(null, null, player.transform.position, 5, 10, new List<Rust.DamageTypeEntry> { dmg }, 133376, true);

            //  Hurt Player
            //  player.Hurt(hitInfo, true);

            timer.Once(5f, () => 
            {
                //  Kill player if still wounded
                if(player.IsWounded())
                    player.DieInstantly();
            });
        }

        bool ShouldBeDestroyed(BaseEntity entity) => DestroyableTypes.Contains(entity.GetType());

#endregion

        #region General Methods

        ////////////////////////////////////////
        ///     Game Related
        ////////////////////////////////////////

        List<T> GetEntities<T>(Vector3 position, int radius)
        where T : BaseEntity
        {
            List<T> list = Pool.GetList<T>();
            Vis.Entities(position, radius, list, LayerMask.GetMask("Construction", "Deployed", "Default", "Ragdoll"));

            return list;
        }

        ////////////////////////////////////////
        ///     Converting etc.
        ////////////////////////////////////////

        string ListToString<T>(List<T> list, int first, string seperator) => string.Join(seperator, (from item in list select item.ToString()).Skip(first).ToArray());

        List<T> CopyList<T>(List<T> list)
        {
            T[] copy = new T[list.Count];
            list.CopyTo(copy);

            return copy.ToList();
        }

        ////////////////////////////////////////
        ///     Config Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        ////////////////////////////////////////
        ///     Data Related
        ////////////////////////////////////////

        void LoadData<T>(ref T data, string filename = "?") => data = Core.Interface.Oxide.DataFileSystem.ReadObject<T>(filename == "?" ? this.Title : filename);

        void SaveData<T>(ref T data, string filename = "?") => Core.Interface.Oxide.DataFileSystem.WriteObject(filename == "?" ? this.Title : filename, data);

        ////////////////////////////////////////
        ///     Message Related
        ////////////////////////////////////////

        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());

        ////////////////////////////////////////
        ///     Permission Related
        ////////////////////////////////////////

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        bool HasPerm(object uid, params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        string PermissionPrefix
        {
            get
            {
                return this.Title.Replace(" ", "").ToLower();
            }
        }

        ////////////////////////////////////////
        ///     Chat Related
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        #endregion

        #region Dev / Debug

        /*
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.USE))
                Puts(GetViewEntity(player)?.GetType()?.ToString() ?? "unknown");
        }


        BaseEntity GetViewEntity(BasePlayer player)
        {
            RaycastHit hit;

            if (Physics.Raycast(player.eyes.HeadRay(), out hit))
                return hit.GetEntity();

            return null;
        }*/

        #endregion
    }
}
