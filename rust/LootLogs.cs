using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("LootLogs", "k1lly0u", "0.1.3", ResourceId = 2065)]
    class LootLogs : RustPlugin
    {
        #region Fields
        Dictionary<uint, StorageType> itemTracker = new Dictionary<uint, StorageType>();
        private FieldInfo serverinput;
        private bool isInit = false;
        #endregion

        #region Oxide Hooks
        void Loaded() => lang.RegisterMessages(Messages, this);
        void OnServerInitialized()
        {
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            isInit = true;
        }

        private void OnEntityDeath(BaseEntity entity, HitInfo hitInfo)
        {
            try
            {
                if (isInit)
                {
                    if (entity != null)
                    {
                        if (entity is StorageContainer || entity is BaseOven || entity is StashContainer)
                        {
                            var killer = "";
                            if (hitInfo?.InitiatorPlayer != null)
                                killer = hitInfo.InitiatorPlayer.displayName;
                            DeathLog(entity.GetType().ToString(), entity.PrefabName, entity.net.ID.ToString(), entity.transform.position.ToString(), killer);
                        }
                    }
                }
            }
            catch { }
        }
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (isInit)
            {
                if (item == null) return;
                if (item.uid == 0) return;
                if (container.playerOwner != null)
                {
                    if (itemTracker.ContainsKey(item.uid))
                    {
                        var player = container.playerOwner;
                        var data = itemTracker[item.uid];
                        if (string.IsNullOrEmpty(data.type) || data.type == "BasePlayer") return;
                        Log(player.displayName, $"{data.itemAmount}x {data.itemName}", data.type, data.entityID, data.entityName, true);
                        itemTracker.Remove(item.uid);
                    }
                }
                else if (container.entityOwner != null)
                {
                    if (itemTracker.ContainsKey(item.uid))
                    {
                        var data = itemTracker[item.uid];
                        string type = "";
                        if (container.entityOwner is StorageContainer)
                            type = "StorageContainer";
                        if (container.entityOwner.GetComponentInParent<BaseOven>())
                            type = "BaseOven";
                        if (container.entityOwner is StashContainer)
                            type = "StashContainer";
                        if (string.IsNullOrEmpty(type) || type == "BasePlayer") return;

                        Log(data.entityName, $"{data.itemAmount}x {data.itemName}", type, container.entityOwner.net.ID.ToString(), container.entityOwner.ShortPrefabName, false);
                        itemTracker.Remove(item.uid);
                    }
                }
            }
        }
        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (isInit)
            {
                if (item == null) return;
                if (item.uid == 0) return;
                if (container.entityOwner != null)
                {
                    var entity = container.entityOwner;
                    var storageData = new StorageType
                    {
                        entityName = entity.ShortPrefabName,
                        entityID = entity.net.ID.ToString(),
                        itemAmount = item.amount,
                        itemName = item.info.displayName.english
                    };

                    if (entity is StorageContainer)
                        storageData.type = "StorageContainer";
                    if (entity.GetComponentInParent<BaseOven>())
                        storageData.type = "BaseOven";
                    if (entity is StashContainer)
                        storageData.type = "StashContainer";

                    if (string.IsNullOrEmpty(storageData.type)) return;

                    if (!itemTracker.ContainsKey(item.uid))
                    {
                        itemTracker.Add(item.uid, storageData);

                        timer.Once(5, () =>
                        {
                            if (itemTracker.ContainsKey(item.uid))
                                itemTracker.Remove(item.uid);
                        });
                    }
                }
                else if (container.playerOwner != null)
                {
                    var entity = container.playerOwner;
                    var storageData = new StorageType
                    {
                        entityName = entity.displayName,
                        entityID = entity.net.ID.ToString(),
                        itemAmount = item.amount,
                        itemName = item.info.displayName.english,
                        type = "BasePlayer"
                    };
                    if (!itemTracker.ContainsKey(item.uid))
                    {
                        itemTracker.Add(item.uid, storageData);

                        timer.Once(5, () =>
                        {
                            if (itemTracker.ContainsKey(item.uid))
                                itemTracker.Remove(item.uid);
                        });
                    }
                }
            }
        }
        class StorageType
        {
            public string entityName;
            public string entityID;
            public string itemName;
            public int itemAmount;
            public string type;            
        }
        #endregion

        #region Functions
        private BaseEntity FindEntity(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles) * Vector3.forward;
            Vector3 eyesAdjust = new Vector3(0f, 1.5f, 0f);

            var rayResult = Ray(player.transform.position + eyesAdjust, currentRot);
            if (rayResult is BaseEntity)
            {
                var target = rayResult as BaseEntity;
                return target;
            }
            return null;
        }
        private object Ray(Vector3 Pos, Vector3 Aim)
        {
            var hits = Physics.RaycastAll(Pos, Aim);
            float distance = 100f;
            object target = null;

            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<BaseEntity>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<BaseEntity>();
                    }
                }
            }
            return target;
        }
        void Log(string playername, string item, string type, string id, string entityname, bool take)
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"LootLogs/{type}/foldercreator"))
                Interface.Oxide.DataFileSystem.SaveDatafile($"LootLogs/{type}/foldercreator");
            var dateTime = DateTime.Now.ToString("yyyy-MM-dd");            
            var taketype = "looted";
            if (!take) taketype = "deposited";
            ConVar.Server.Log($"oxide/data/LootLogs/{type}/{entityname}_{id}_{dateTime}.txt", $"{playername} {taketype} {item}");
        }
        void DeathLog(string type, string entityname, string id, string position, string killer)
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"LootLogs/DestroyedContainers/{type}/foldercreator"))
                Interface.Oxide.DataFileSystem.SaveDatafile($"LootLogs/DestroyedContainers/{type}/foldercreator");
            var dateTime = DateTime.Now.ToString("yyyy-MM-dd");
            ConVar.Server.Log($"oxide/data/LootLogs/DestroyedContainers/{type}/DeathLog_{dateTime}.txt", $"Name:{entityname} | BoxID:{id} | Position:{position} | Killer: {killer} | LogFile: oxide/data/LootLogs/{type}/{entityname}_{id}_xxxxxxxx.txt");
            ConVar.Server.Log($"oxide/data/LootLogs/DestroyedContainers/{type}/DeathLog_{dateTime}.txt", "-------------------------------------------------------------------------");
        }
        #endregion

        #region Chat Commands
        [ChatCommand("findid")]
        void cmdFindID(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            var entity = FindEntity(player);
            if (entity == null || ( !(entity is StorageContainer) && !(entity is StashContainer) && !(entity is BaseOven)))
            {
                SendReply(player, $"<color=orange>{msg("noEntity", player.UserIDString)}</color>");
                return;
            }
            else
            {
                SendReply(player, string.Format(msg("foundBox", player.UserIDString), entity.GetType(), entity.net.ID, entity.ShortPrefabName, "<color=#939393>", "<color=orange>"));
                return;
            }
        }
        #endregion

        #region Messaging
        string msg(string key, string id = null) => lang.GetMessage(key, this, id);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"noEntity", "You are either not looking at at a entity or you are not looking at the correct type of entity" },
            {"foundBox", "{3}The box you are looking at is of the type:</color>{4} {0}</color>{3} with the ID:</color>{4} {1}.</color>{3} You can find the log for this box in </color>{4}'oxide/data/LootLogs/{0}/{2}_{1}_xxdatexx.txt'</color>" }
        };
        #endregion
    }
}
