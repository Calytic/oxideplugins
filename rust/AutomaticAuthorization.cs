using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("AutomaticAuthorization", "k1lly0u", "0.1.6", ResourceId = 2063)]
    class AutomaticAuthorization : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Clans;
        [PluginReference] Plugin Friends;

        private FieldInfo serverinput;
        #endregion

        #region Oxide Hooks        
        void OnServerInitialized()
        {
            permission.RegisterPermission("automaticauthorization.use", this);
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            lang.RegisterMessages(Messages, this);
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
        void RegisterClanmates(BasePlayer player, BaseEntity entity)
        {
            List<ulong> authList = new List<ulong>();
            var clanName = Clans?.Call("GetClanOf", player.userID);            
            if (clanName != null)
            {
                var clan = GetClan((string)clanName);
                if (clan != null && clan is JObject)
                {
                    var members = (clan as JObject).GetValue("members");
                    if (members != null && members is JArray)
                    {
                        foreach (var member in (JArray)members)
                        {
                            ulong ID;
                            if (!ulong.TryParse(member.ToString(), out ID))                            
                                continue;                            
                            authList.Add(ID);
                        }
                        SortAuthList(player, entity, authList);
                    }
                    else
                    {
                        SendReply(player, msg("noClanMembers"));
                        return;
                    }
                }
                else
                {
                    SendReply(player, msg("noClanMembers"));
                    return;
                }
            }
            else
            {
                SendReply(player, msg("noClan"));
                return;
            }            
        }
        void RegisterFriends(BasePlayer player, BaseEntity entity)
        {
            List<ulong> authList = new List<ulong>();
            var friends = GetFriends(player.userID);
            if (friends is ulong[])
            {
                authList.Add(player.userID);
                foreach (var member in (ulong[])friends)                                   
                    authList.Add(member);                
                SortAuthList(player, entity, authList);
            }
            else
            {
                SendReply(player, msg("noFriendsList"));
                return;
            }            
        }
        void SortAuthList(BasePlayer player,  BaseEntity entity, List<ulong> authList)
        {
            Dictionary<ulong, string> friendData = new Dictionary<ulong, string>();
            for (int i = 0; i < authList.Count; i++)
            {
                var foundPlayer = BasePlayer.FindByID(authList[i]);
                if (foundPlayer == null)
                    foundPlayer = BasePlayer.FindSleeping(authList[i]);                
                if (foundPlayer != null)
                    friendData.Add(foundPlayer.userID, foundPlayer.displayName);                
                else
                    friendData.Add(authList[i], "");
                
            }
            if (entity is BuildingPrivlidge)
                AuthToCupboard(player, entity as BuildingPrivlidge, friendData);
            else AuthToTurret(player, entity as AutoTurret, friendData);
        }
        void AuthToCupboard(BasePlayer player, BuildingPrivlidge cupboard, Dictionary<ulong, string> authList)
        {
            cupboard.authorizedPlayers.Clear();
            foreach (var friend in authList)
            {
                cupboard.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
                {
                    userid = friend.Key,
                    username = friend.Value,
                    ShouldPool = true
                });
            }           
            cupboard.SendNetworkUpdateImmediate();
            player.SendNetworkUpdateImmediate();
            SendReply(player, string.Format(msg("cupboardSuccess"), authList.Count));
            return;
        }
        void AuthToTurret(BasePlayer player, AutoTurret turret, Dictionary<ulong, string> authList)
        {
            turret.authorizedPlayers.Clear();
            foreach (var friend in authList)
            {
                turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
                {
                    userid = friend.Key,
                    username = friend.Value
                });
            }
            turret.SendNetworkUpdateImmediate();
            player.SendNetworkUpdateImmediate();
            SendReply(player, string.Format(msg("turretSuccess"), authList.Count));
            return;
        }
        #endregion

        #region Chat Commands
        [ChatCommand("autoauth")]
        void cmdAuth(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin() || permission.UserHasPermission(player.UserIDString, "automaticauthorization.use"))
            {
                if (args == null || args.Length == 0)
                {
                    if (Clans) SendReply(player, msg("clanSyn1"));
                    if (Friends) SendReply(player, msg("friendSyn1"));
                    if (!Clans && !Friends) return;
                    SendReply(player, msg("options"));
                    return;
                }
                var entity = FindEntity(player);                
                if (entity == null || (!entity.GetComponent<AutoTurret>() && !entity.GetComponent<BuildingPrivlidge>()))
                {
                    SendReply(player, msg("noEntity"));
                    return;
                }
                if (entity.OwnerID != player.userID)
                {
                    SendReply(player, msg("noOwner"));
                    return;
                }
                switch (args[0].ToLower())
                {
                    case "clan":
                        if (Clans)
                            RegisterClanmates(player, entity);
                        else SendReply(player, msg("noClanPlugin", player.UserIDString));
                        return;
                    case "friends":
                        if (Friends)
                            RegisterFriends(player, entity);
                        else SendReply(player, msg("noFriendPlugin", player.UserIDString));
                        return;
                    default:
                        break;
                }                                                
            }          
        }

        #endregion

        #region Helpers
        private object GetClan(string name) => Clans?.Call("GetClan", name);
        private object GetFriends(ulong playerID) => Friends?.Call("IsFriendOf", playerID);
        #endregion

        #region Messaging
        string msg(string key, string id = null) => lang.GetMessage(key, this, id);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"noEntity", "You need to look at either a Autoturret or a Tool Cupboard" },
            {"turretSuccess", "Successfully added {0} friends/clan members to the turret auth list" },
            {"cupboardSuccess", "Successfully added {0} friends/clan members to the cupboard auth list" },
            {"noOwner", "You can not authorize on something you do not own" },
            { "noFriendsList", "Unable to find your friends list" },
            {"noClan", "Unable to find your clan" },
            {"noClanMembers", "Unable to find your clan members" },
            {"clanSyn1", "/autoauth clan - Authorizes your clan mates to the object your looking at (RustIO Clans)" },
            {"friendSyn1", "/autoauth friends - Authorizes your friends to the object your looking at (Friends API)" },
            {"options", "This works for Tool Cupboards and Autoturrets\nTo use look at the cupboard/turret you want to authorize your friends on. You must be the owner of the cupboard/turret!" },
            {"noClanPlugin", "Unable to find the Clans plugin" },
            {"noFriendPlugin", "Unable to find the Friends plugin" }
        };
        #endregion
    }
}
