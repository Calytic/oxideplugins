using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Dead Players List", "Mughisi", "2.0.5", ResourceId = 696)]
    class DeadPlayersList : RustPlugin
    {

        class LocationInfo
        {
            public string x;
            public string y;
            public string z;
            Vector3 position;

            public LocationInfo(Vector3 position)
            {
                x = position.x.ToString();
                y = position.y.ToString();
                z = position.z.ToString();
                this.position = position;
            }

            public Vector3 GetPosition()
            {
                if (position == Vector3.zero)
                    position = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return position;
            }
        }

        class DeadPlayerInfo
        {
            public string UserId;
            public string Name;
            public string Reason;
            public LocationInfo Position;

            public DeadPlayerInfo()
            {
            }

            public DeadPlayerInfo(BasePlayer victim, HitInfo info)
            {
                UserId = victim.userID.ToString();
                Name = victim.displayName;
                Reason = victim.lastDamage.ToString();
                if (info != null)
                {
                    var attacker = info.Initiator as BasePlayer;
                    if (info.Initiator)
                    {
                        Reason = info.Initiator.LookupPrefabName();
                        if (attacker && attacker != victim) Reason = attacker.displayName;
                    }
                }
                Position = new LocationInfo(victim.transform.position);
            }

            public ulong GetUserId()
            {
                ulong user_id;
                if (!ulong.TryParse(UserId, out user_id)) return 0;
                return user_id;
            }
        }

        class StoredData
        {
            public HashSet<DeadPlayerInfo> DeadPlayers = new HashSet<DeadPlayerInfo>();

            public StoredData()
            {
            }
        }

        StoredData storedData;

        Hash<ulong, DeadPlayerInfo> deadPlayers = new Hash<ulong, DeadPlayerInfo>();

        bool dataChanged;

        void Loaded() => LoadData();

        void Unloaded() => SaveData();

        void LoadData()
        {
            deadPlayers.Clear();
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("DeadPlayersList");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var dead_player in storedData.DeadPlayers)
                deadPlayers[dead_player.GetUserId()] = dead_player;
        }

        void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("DeadPlayersList", storedData);

        void OnPlayerInit(BasePlayer player)
        {
            if (deadPlayers.Remove(player.userID) && storedData.DeadPlayers.RemoveWhere(info => info.GetUserId() == player.userID) > 0)
                dataChanged = true;
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            if (deadPlayers.Remove(player.userID) && storedData.DeadPlayers.RemoveWhere(info => info.GetUserId() == player.userID) > 0)
                dataChanged = true;
        }

        void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            var player = entity as BasePlayer;
            if (!player) return;

            var dead_player = deadPlayers[player.userID];
            if (dead_player != null) storedData.DeadPlayers.Remove(dead_player);
            dead_player = new DeadPlayerInfo(player, info);
            deadPlayers[player.userID] = dead_player;
            storedData.DeadPlayers.Add(dead_player);

            dataChanged = true;
        }

        void OnServerSave()
        {
            if (!dataChanged) return;
            SaveData();
            dataChanged = false;
        }

        Dictionary<string, string> GetPlayerList()
        {
            Dictionary<string, string> deadPlayerList = new Dictionary<string, string>();
            foreach (var deadPlayer in deadPlayers)
                deadPlayerList.Add(deadPlayer.Key.ToString(), deadPlayer.Value.Name);

            return deadPlayerList;
        }

        string GetPlayerName(object userID) => deadPlayers[Convert.ToUInt64(userID)]?.Name;

        string GetPlayerDeathReason(object userID) => deadPlayers[Convert.ToUInt64(userID)]?.Reason;

        Vector3 GetPlayerDeathPosition(object userID) => deadPlayers[Convert.ToUInt64(userID)]?.Position.GetPosition() ?? Vector3.zero;
    }
}