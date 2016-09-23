using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Entity Limit", "PaiN", 0.5, ResourceId = 1947)]
    class EntityLimit : RustPlugin
    {
        static EntityLimit Plugin;
        static Data data;
        static ConfigFile Cfg = new ConfigFile();

        class ConfigFile
        {
            public Dictionary<string, int> MaxLimits = new Dictionary<string, int>
            {
                ["wall.external.high.stone"] = 10,
                ["gates.external.high.stone"] = 2,
                ["gates.external.high.wood"] = 2
            };
        }

        class Data { public List<PlayerLimit> Limits = new List<PlayerLimit>(); }

        class PlayerLimit
        {
            public ulong Id;
            public List<Entities> limit;

            public static void Create(BasePlayer player, BaseEntity entity)
            {
                if (!data.Limits.Any(x => x.Id == player.userID))
                {
                    data.Limits.Add(new PlayerLimit()
                    {
                        Id = player.userID,
                        limit = new List<Entities>()
                        {
                            new Entities()
                            { Name = entity.ShortPrefabName, Count = 1}
                        }
                    });
                }
            }

            public static void Modify(BasePlayer player, BaseEntity entity)
            {
                if (data.Limits.Any(x => x.Id == player.userID))
                {
                    PlayerLimit info = data.Limits.Find(x => x.Id == player.userID) ?? null;
                    if (!info.limit.Any(x => x.Name == entity.ShortPrefabName))
                    {
                        info.limit.Add(new Entities()
                        { Count = 1, Name = entity.ShortPrefabName });

                    }
                    else
                    {
                        Entities playerEnt = info.limit.Find(x => x.Name == entity.ShortPrefabName) ?? null;
                        if (Cfg.MaxLimits.Any(x => x.Key == playerEnt.Name))
                        {
                            if (playerEnt.Count == Cfg.MaxLimits[playerEnt.Name])
                            {
                                player.ChatMessage(LangMsg("MAX_ENTITIES"));
                                var item = ItemManager.CreateByName(entity.ShortPrefabName.Replace("_", "."), 1);
                                player.inventory.GiveItem(item, player.inventory.containerBelt);
                                player.Command(string.Concat(new object[4]
                                {
                                    (object) "note.inv ",
                                    (object) item.info.itemid,
                                    (object) " ",
                                    (object) "1"
                                }));
                                entity.KillMessage();
                                return;
                            }
                            playerEnt.Count += 1;
                            Plugin.Puts(playerEnt.Count.ToString());
                            return;
                        }
                    }
                }
            }
        }

        class Entities
        {
            public string Name;
            public int Count;
        }

        void Loaded()
        {
            permission.RegisterPermission("entitylimit.admin", this);
            Cfg = Config.ReadObject<ConfigFile>();
            data = Interface.Oxide.DataFileSystem.ReadObject<Data>("EntityLimit");
            LoadMessages();
            Plugin = this;
        }

        void Unloaded()
        {
            SaveData();
        }
        void OnServerSave() => SaveData();

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file...");
            Config.WriteObject(Cfg, true);
        }

        void LoadMessages()
        {
            Dictionary<string, string> msg = new Dictionary<string, string>
            {
                ["NO_PERMISSION"] = "You do not have permission to use this command!",
                ["MAX_ENTITIES"] = "You have reached the max allowed placed amount of this entity!",
                //["CLAN_MAX_ENTITIES"] = "Your previous clan has already reached the max limit for this item!",
                ["REMOVED_LIMITS_PLAYER"] = "You have removed all the player limits of {0}",
                ["REMOVED_ALL_LIMITS"] = "You have remove all the saved limits",
                ["PLAYER_NOT_FOUND"] = "Player not found.",
                ["NOT_VALID_ENTITY"] = "This is not a valid entity!",
                ["CMD_WIPE_SYNTAX"] = "Syntax: /limitswipe <playerName/all>"
            };
            lang.RegisterMessages(msg, this);
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null) return;
            if (!entity is BaseEntity) return;

            BaseEntity ent = (BaseEntity)entity;
            BasePlayer player = BasePlayer.FindByID(ent.OwnerID);
            if (ent == null || player == null) return;

            if (Cfg.MaxLimits.Any(x => x.Key == entity.ShortPrefabName))
            {
                if (!data.Limits.Any(x => x.Id == player.userID))
                    PlayerLimit.Create(player, ent);
                else
                    PlayerLimit.Modify(player, ent);
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null) return;
            EntityDestroyed(entity);
        }

        void OnRemovedEntity(BaseEntity entity)
        {
            if (entity == null) return;
            EntityDestroyed(entity);
        }

        void EntityDestroyed(BaseEntity entity)
        {
            if (entity == null) return;

            if (Cfg.MaxLimits.Any(x => x.Key == entity.ShortPrefabName))
            {
                PlayerLimit info = data.Limits.Find(x => x.Id == entity.OwnerID) ?? null;
                Entities PlayerEnts = info.limit.Find(x => x.Name == entity.ShortPrefabName) ?? null;

                if (info == null || PlayerEnts == null || PlayerEnts.Count == 0) return;

                PlayerEnts.Count -= 1;
            }
        }

        [ChatCommand("shortname")]
        void cmdShortName(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "entitylimit.admin"))
            {
                player.ChatMessage(LangMsg("NO_PERMISSION", player.UserIDString));
                return;
            }

            RaycastHit hit;

            Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity);
            BaseEntity entity = hit.GetTransform()?.gameObject.ToBaseEntity();

            if (entity == null)
            {
                player.ChatMessage(LangMsg("NOT_VALID_ENTITY", player.UserIDString));
                return;
            }

            player.ChatMessage(string.Format("Shortname: {0}", entity.ShortPrefabName));
        }

        [ChatCommand("limitswipe")]
        void cmdWipe(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "entitylimit.admin"))
            {
                player.ChatMessage(LangMsg("NO_PERMISSION", player.UserIDString));
                return;
            }

            if (args.Length == 0)
            {
                player.ChatMessage(LangMsg("CMD_WIPE_SYNTAX", player.UserIDString));
                return;
            }

            if (args[0] == "all")
            {
                data.Limits.Clear();
                SaveData();
                player.ChatMessage(LangMsg("REMOVED_ALL_LIMITS", player.UserIDString));
            }
            else
            {
                BasePlayer target = BasePlayer.Find(args[0]);
                if (target == null)
                {
                    player.ChatMessage(LangMsg("PLAYER_NOT_FOUND", player.UserIDString));
                    return;
                }
                PlayerLimit info = data.Limits.Find(x => x.Id == target.userID);

                if (info == null)
                {
                    player.ChatMessage("This player doesn't have any saved entities");
                    return;
                }

                data.Limits.Remove(info);
                player.ChatMessage(string.Format(LangMsg("REMOVED_LIMITS_PLAYER", player.UserIDString), target.displayName));
                SaveData();
            }

        }

        static void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("EntityLimit", data);
        static string LangMsg(string msg, string uid = null) => Plugin.lang.GetMessage(msg, Plugin, uid);
    }
}
