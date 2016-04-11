// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;

namespace Oxide.Plugins
{
    [Info("TPR", "Reneb Fix by Copper, Flapstik,DraB", "1.0.3", ResourceId = 941)]
    class TPR : RustLegacyPlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;
        /////////////////////////////
        // FIELDS
        /////////////////////////////
        private DateTime epoch;

        RustServerManagement management;

        Dictionary<NetUser, float> lastRequest = new Dictionary<NetUser, float>();
        Dictionary<NetUser, NetUser> TPRequest = new Dictionary<NetUser, NetUser>();
        Dictionary<NetUser, NetUser> TPIncoming = new Dictionary<NetUser, NetUser>();
        Dictionary<NetUser, Oxide.Plugins.Timer> timersList = new Dictionary<NetUser, Oxide.Plugins.Timer>();

        int terrainLayer;

        float TPRStructureDistance = 7f;

        Vector3 VectorUp = new Vector3(0f, 1f, 0f);
        Vector3 VectorDown = new Vector3(0f, -0.4f, 0f);
        Vector3 VectorDownn = new Vector3(0f, -0.1f, 0f);

        public static Vector3 Vector3Down = new Vector3(0f, -1f, 0f);
        public static Vector3 Vector3Up = new Vector3(0f, 1f, 0f);
        public static Vector3 UnderPlayerAdjustement = new Vector3(0f, -1.15f, 0f);
        public static float distanceDown = 10f;

        RaycastHit cachedRaycast;
        Collider[] cachedColliders;
        /////////////////////////////
        // Config Management
        /////////////////////////////

        bool ifOnGround(NetUser netusery)
        {

            PlayerClient playerclient = netusery.playerClient;
            Vector3 lastPosition = playerclient.lastKnownPosition;


            Collider cachedCollider;
            bool cachedBoolean;
            Vector3 cachedvector3;
            RaycastHit cachedRaycast;
            Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;


            if (lastPosition == default(Vector3)) return true;
            if (!MeshBatchPhysics.Raycast(lastPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { return true; }
            if (cachedhitInstance == null) { return true; }
            if (cachedhitInstance.graphicalModel.ToString() == null) {
                //Put(cachedhitInstance.graphicalModel.ToString());
                return true;
            }

            return false;
        }

        bool ifOnDeployable(NetUser userx) {

            PlayerClient playerclient = userx.playerClient;
            Vector3 lastPosition = playerclient.lastKnownPosition;


            Collider cachedCollider;
            bool cachedBoolean;
            Vector3 cachedvector3;
            RaycastHit cachedRaycast;
            Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
            DeployableObject cachedDeployable;

            if (lastPosition == default(Vector3)) return false;
            if (!MeshBatchPhysics.Raycast(lastPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { return false; }
            if (cachedhitInstance == null)
            {
                cachedDeployable = cachedRaycast.collider.GetComponent<DeployableObject>();
                if (cachedDeployable != null)
                {
                    return true;
                }
                return false;
            }
            if (cachedhitInstance.graphicalModel.ToString() == null) {
                //Put(cachedhitInstance.graphicalModel.ToString());
                return false;
            }

            return false;
        }
        bool ifOnlootDeployable(NetUser userx) {

            PlayerClient playerclient = userx.playerClient;
            Vector3 lastPosition = playerclient.lastKnownPosition;


            Collider cachedCollider;
            bool cachedBoolean;
            Vector3 cachedvector3;
            RaycastHit cachedRaycast;
            Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
            DeployableObject cachedDeployable;

            if (lastPosition == default(Vector3)) return false;
            if (!MeshBatchPhysics.Raycast(lastPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { return false; }
            if (cachedhitInstance == null)
            {
                var cachedLootableObject = cachedRaycast.collider.GetComponent<LootableObject>();
                if (cachedLootableObject != null)
                {
                    return true;
                }
                return false;
            }
            if (cachedhitInstance.graphicalModel.ToString() == null) {
                //Put(cachedhitInstance.graphicalModel.ToString());
                return false;
            }

            return false;
        }
        bool ifOnlootsackDeployable(NetUser userx) {

            PlayerClient playerclient = userx.playerClient;
            Vector3 lastPosition = playerclient.lastKnownPosition;

            Collider cachedCollider;
            bool cachedBoolean;
            Vector3 cachedvector3;
            RaycastHit cachedRaycast;
            Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
            DeployableObject cachedDeployable;

            if (lastPosition == default(Vector3)) return false;
            if (!MeshBatchPhysics.Raycast(lastPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { return false; }
            if (cachedhitInstance == null)
            {
                var cachedsack = "GenericItemPickup(Clone)";
                var cachedLootableObject = cachedRaycast.collider.gameObject.name;
                if (cachedLootableObject == cachedsack)
                {
                    return true;
                }
                return false;
            }
            var cachedsack2 = "GenericItemPickup(clone)";
            if (cachedhitInstance.graphicalModel.ToString() == cachedsack2)
                return true;
            if (cachedhitInstance.graphicalModel.ToString().Contains(cachedsack2)) return true;
            if (cachedhitInstance.graphicalModel.ToString() == null) {
                Debug.Log(cachedhitInstance.graphicalModel.ToString());
                return false;
            }

            return false;
        }

        StructureComponent GetClosestStructure(UnityEngine.Object[] structObjs, Vector3 pos)
        {
            StructureComponent theComponent = null;
            float minDistance = Mathf.Infinity;
			
			for(int i = 0; i < structObjs.Length; i++)
            {
				StructureComponent component = (StructureComponent)structObjs[i];
				
                float distance = Vector3.Distance(component.transform.position, pos);
                if (distance < minDistance)
                {
                    theComponent = component;
                    minDistance = distance;
                }
            }
            return theComponent;
        }

        bool IfNearStructure(NetUser userx)
        {
            PlayerClient playerclient = userx.playerClient;
            Vector3 lastPosition = playerclient.lastKnownPosition;
            UnityEngine.Object[] structObjs = Resources.FindObjectsOfTypeAll(typeof(StructureComponent));

            if (Vector3.Distance(GetClosestStructure(structObjs, lastPosition).transform.position, lastPosition) < TPRStructureDistance)
            {
                return true;
            }
            return false;
        }

        bool Ifinshack(NetUser userx)
		{
            PlayerClient playerclient = userx.playerClient;
			Vector3 lastPosition = playerclient.lastKnownPosition;
			Collider cachedCollider;
			bool cachedBoolean;
			Vector3 cachedvector3;
			RaycastHit cachedRaycast;
			Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
			DeployableObject cachedDeployable;
			if (lastPosition == default(Vector3)) return false;
			if (!MeshBatchPhysics.Raycast(lastPosition + UnderPlayerAdjustement, Vector3Up, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { return false; }
			if (cachedhitInstance == null) 
			{
				var cachedsack = "Wood_Shelter(Clone)";
				var cachedLootableObject = cachedRaycast.collider.gameObject.name;
				if (cachedLootableObject == cachedsack)
				{
					return true;
				}
				return false;
			}
			var cachedsack2 = "Wood_Shelter(Clone)";
			if(cachedhitInstance.graphicalModel.ToString() == cachedsack2)
				return true;
			if (cachedhitInstance.graphicalModel.ToString().Contains(cachedsack2)) return true;
			if (cachedhitInstance.graphicalModel.ToString() == null)
			{
				Debug.Log(cachedhitInstance.graphicalModel.ToString());
				return false;
			}
			return false;
		}
        static string notAllowed = "You are not allowed to use this command.";
        static bool cancelOnHurt = true;
        static bool useTokens = true;
        static int givefreetokensmax = 3;
        static int starttokens = 3;
        static bool givefreetokens = true;
        static double givefreetokensevery = 600.0;
        static int tprCooldown = 60;
        static int tprtime = 10;

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key]; 
            else
                Config[Key] = var;
        } 

        void Init()
        {
            CheckCfg<bool>("Settings: Cancel teleportation if player is hurt", ref cancelOnHurt);
            CheckCfg<int>("Settings: Cooldown before being able to use TPR again", ref tprCooldown);
            CheckCfg<int>("Settings: TPR Teleportation delay", ref tprtime);
            CheckCfg<bool>("Tokens: activated", ref useTokens);
            CheckCfg<bool>("Tokens: give free tokens", ref givefreetokens);
            CheckCfg<double>("Tokens: give free token every", ref givefreetokensevery);
            CheckCfg<int>("Tokens: give free tokens max", ref givefreetokensmax);
            CheckCfg<int>("Tokens: start tokens", ref starttokens);
            SaveConfig();
        }


        /////////////////////////////
        // Oxide Hooks
        /////////////////////////////

        void Loaded()
        {
            epoch = new System.DateTime(1970, 1, 1);
            terrainLayer = LayerMask.GetMask(new string[] { "Static" });
        }
        void OnServerInitialized()
        {
            management = RustServerManagement.Get();
            if(useTokens && PlayerDatabase==null)
            {
                Debug.Log("WARNING from TPR: You are trying to use the tokens without PlayerDatabase installed, tokens will not work.");
                useTokens = false;
            }
        }
        void Unload()
        {
            foreach (KeyValuePair<NetUser, Oxide.Plugins.Timer> pair in timersList)
            {
                pair.Value.Destroy();
            }
            timersList.Clear();
        }  
        void OnPlayerDisconnect(uLink.NetworkPlayer netplayer)
        {
            NetUser netuser = (NetUser)netplayer.GetLocalData();
            ResetRequest(netuser);
        }
        /////////////////////////////
        // External Plugin Functions
        /////////////////////////////
        object AddTeleportTokens(string userid, int number = 1)
        {
            if(useTokens)
            {
                var tokensLeft = GetPlayerTokensByID(userid) + number;
                PlayerDatabase.Call("SetPlayerData", userid.ToString(), "tokens", tokensLeft);
                return tokensLeft;
            }
            return null;
        }
        object RemoveTeleportTokens(string userid, int number = 1)
        {
            if (useTokens)
            {
                var tokensLeft = GetPlayerTokensByID(userid) - number;
                PlayerDatabase.Call("SetPlayerData", userid.ToString(), "tokens", tokensLeft);
                return tokensLeft;
            }
            return null;
        }
        object GetTeleportTokens(string userid)
        {
            if (useTokens)
            {
                return GetPlayerTokensByID(userid);
            }
            return null;
        }
        object SetTeleportTokens(string userid, int number)
        {
            if (useTokens)
            {
                var tokensLeft = GetPlayerTokensByID(userid);
                PlayerDatabase.Call("SetPlayerData", userid.ToString(), "tokens", number);
                return tokensLeft;
            }
            return null;
        }

        /////////////////////////////
        // Teleportation Functions
        /////////////////////////////

        void DoTeleportToPlayer(NetUser source, Vector3 target, NetUser targetuser)
        {
            if (source == null || source.playerClient == null)
                return;

            management.TeleportPlayerToWorld(source.playerClient.netPlayer, target);
            SendReply(source, string.Format("You teleported to {0}", targetuser.playerClient.userName));
        }
        void ResetRequest(NetUser netuser)
        {
            if (timersList.ContainsKey(netuser))
            {
                timersList[netuser].Destroy();
                timersList.Remove(netuser);
            }
            if (TPRequest.ContainsKey(netuser))
            {
                var targetuser = TPRequest[netuser];
                if (TPIncoming.ContainsKey(targetuser))
                {
                    TPIncoming.Remove(targetuser);
                    SendReply(netuser, "[color red]Target user hasn't responded to your request.");
                }
                TPRequest.Remove(netuser);
            }
            foreach (KeyValuePair<NetUser, NetUser> pair in TPRequest)
            {
                if (pair.Value == netuser)
                {
                    var sourceuser = pair.Key;
                    if (TPRequest.ContainsKey(sourceuser))
                    {
                        var targetuser = TPRequest[sourceuser];
                        if (TPIncoming.ContainsKey(targetuser))
                            TPIncoming.Remove(targetuser);
                        timer.Once(0.01f, () => TPRequest.Remove(sourceuser));
                        if (timersList.ContainsKey(sourceuser))
                        {
                            timersList[sourceuser].Destroy();
                            timersList.Remove(sourceuser);
                        }
                        if(sourceuser.playerClient)
                        {
                            SendReply(sourceuser, "[color red]Teleportation was not a success");
                        }
                        if (targetuser.playerClient)
                        {
                            SendReply(targetuser, "[color red]Teleportation was not a success");
                        }
                    }
                }
            }
        }
        void AcceptRequest(NetUser netuser)
        {
            if (timersList.ContainsKey(netuser))
            {
                timersList[netuser].Destroy();
                timersList.Remove(netuser);
            }
            if (TPRequest.ContainsKey(netuser))
            {
                var targetuser = TPRequest[netuser];
                if (TPIncoming.ContainsKey(targetuser))
                    TPIncoming.Remove(targetuser);
                timersList.Add(netuser, timer.Once((float)tprtime, () => DoTeleportation(netuser)));
            }
        }
        void DoTeleportation(NetUser netuser)
        {
            if (netuser == null || netuser.playerClient == null)
			{
				return;
			}
			FallDamage falldamage = netuser.playerClient.rootControllable.GetComponent<FallDamage>();
            if(!TPRequest.ContainsKey(netuser)) { SendReply(netuser, "Something went wrong, you dont have a target"); ClearLegInjury(falldamage); return; }
            var targetuser = TPRequest[netuser];
            if (targetuser == null || targetuser.playerClient == null) { SendReply(netuser, "The target player that you were supposed to teleport to doesn't seem to be connected."); TPRequest.Remove(netuser); ClearLegInjury(falldamage); return; }

            object thereturn = Interface.GetMod().CallHook("canTeleport", new object[] { netuser });
            if (thereturn != null)
            {
                SendReply(netuser, "You are not allowed to teleport from where you are.");
                TPRequest.Remove(netuser);
                return;
            } 
            thereturn = Interface.GetMod().CallHook("canTeleport", new object[] { targetuser });
            if (thereturn != null)
            {
                SendReply(netuser, "You are not allowed to teleport to where the target is.");
                TPRequest.Remove(netuser);
                return;
            }
            foreach(Collider collider in UnityEngine.Physics.OverlapSphere(targetuser.playerClient.lastKnownPosition, 0.5f, terrainLayer))
            {
                if(Physics.Raycast(targetuser.playerClient.lastKnownPosition, VectorDown, out cachedRaycast, 1f, terrainLayer))
                {
                    if (cachedRaycast.collider == collider)
                    {
                        break;
                    }
                }
                SendReply(netuser, "[color red]The target seems to be under a rock, can't teleport you there.");
                TPRequest.Remove(netuser);
                return;
            }
			if(Ifinshack(targetuser)) {
				SendReply(netuser, string.Format("[color cyan]{0} [color red]is in a shelter so you can't teleport.", targetuser.displayName));
				SendReply(targetuser, string.Format("[color cyan]You [color red]are in a shelter so [color cyan]{0} [color red]couldn't teleport to you.", netuser.displayName));
				return;
			}
            if (IfNearStructure(targetuser))
            {
                SendReply(netuser, string.Format("[color cyan]{0} [color red]is standing to close to a wall. Unable to teleport.", targetuser.displayName));
                SendReply(targetuser, string.Format("[color cyan]You [color red]are standing to close to a wall so [color cyan]{0} [color red]couldn't teleport to you.", netuser.displayName));
                TPRequest.Remove(netuser);
                return;
            }
            if (!ifOnGround(targetuser)) {
				SendReply(netuser, string.Format("{0} [color red]is on a building so you can't teleport.", targetuser.displayName));
				SendReply(targetuser, string.Format("You are on a building so {0} couldn't teleport to you.", netuser.displayName));
				TPRequest.Remove(netuser);
				return;
			}
			if(ifOnDeployable(targetuser)) {
				SendReply(netuser, string.Format("{0} [color red]is on an object so you can't teleport.", targetuser.displayName));
				SendReply(targetuser, string.Format("You are on an object so {0} couldn't teleport to you.", netuser.displayName));
				TPRequest.Remove(netuser);
				return;
			}
			if(ifOnlootDeployable(targetuser)){
				SendReply(netuser, string.Format("{0} [color red]is on a lootable object so you can't teleport.", targetuser.displayName));
				SendReply(targetuser, string.Format("You are on a lotable object so {0} couldn't teleport to you.", netuser.displayName));
				TPRequest.Remove(netuser);
				return;
			}
			if(ifOnlootsackDeployable(targetuser)){
				SendReply(netuser, string.Format("{0} [color red]is on a lootable object so you can't teleport.", targetuser.displayName));
				SendReply(targetuser, string.Format("You are on a lotable object so {0} couldn't teleport to you.", netuser.displayName));
				TPRequest.Remove(netuser);
				return;
			}
			if(ifOnlootsackDeployable(targetuser)){
				SendReply(netuser, string.Format("{0} [color red]is on a lootable object so you can't teleport.", targetuser.displayName));
				SendReply(targetuser, string.Format("You are on a lotable object so {0} couldn't teleport to you.", netuser.displayName));
				TPRequest.Remove(netuser);
				return;
			}
            if (lastRequest.ContainsKey(netuser)) lastRequest.Remove(netuser);
            lastRequest.Add(netuser, Time.realtimeSinceStartup);
            if(useTokens)
            {
                var tokensLeft = GetPlayerTokens(netuser) - 1;
                PlayerDatabase.Call("SetPlayerData", netuser.playerClient.userID.ToString(), "tokens", tokensLeft);
            }
            var fixedpos = targetuser.playerClient.lastKnownPosition;
            DoTeleportToPlayer(netuser, fixedpos, targetuser);
			if (timersList.ContainsKey(netuser))
            {
                timersList[netuser].Destroy();
                timersList.Remove(netuser);
            }
            TPRequest.Remove(netuser);
        }
        void ClearLegInjury(FallDamage falldamage)
        {
            if (falldamage == null) return;
            falldamage.ClearInjury();
        }
        void OnKilled(TakeDamage takedamage, DamageEvent damage)
        {
            if (damage.victim.client == null) return;
            NetUser netuser = damage.victim.client.netUser;
            if (TPRequest.ContainsKey(netuser))
            {
                SendReply(netuser, "Teleportation cancelled.");
            }
            ResetRequest(netuser);
        }
        double CurrentTime()
        {
            return System.DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }
        int GetPlayerTokensByID(string userid)
        {
            var datatokens = PlayerDatabase.Call("GetPlayerData", userid.ToString(), "tokens");
            if (datatokens == null)
            {
                
                PlayerDatabase.Call("SetPlayerData", userid.ToString(), "tokens", starttokens);
                PlayerDatabase.Call("SetPlayerData", userid.ToString(), "lasttokens", CurrentTime());
                return starttokens;
            }
            int realtokens = Convert.ToInt32(datatokens);
            if (realtokens < givefreetokensmax)
            {
                var lasttokens = Convert.ToDouble(PlayerDatabase.Call("GetPlayerData", userid.ToString(), "lasttokens"));
                var tokenstogive = (CurrentTime() - lasttokens) / givefreetokensevery;
                if (tokenstogive >= 1)
                {
                    realtokens += Convert.ToInt32(tokenstogive);
                    if (realtokens > givefreetokensmax)
                        realtokens = givefreetokensmax;
                    PlayerDatabase.Call("SetPlayerData", userid.ToString(), "tokens", realtokens);
                    PlayerDatabase.Call("SetPlayerData", userid.ToString(), "lasttokens", CurrentTime());
                }
            }
            return realtokens;
        }
        int GetPlayerTokens(NetUser netuser)
        {
            return GetPlayerTokensByID(netuser.playerClient.userID.ToString());
        }

        [ChatCommand("tpr")]
        void cmdChatTeleportRequest(NetUser netuser, string command, string[] args)
        {
            if (args.Length == 0) {
                SendReply(netuser, "/tpr PLAYER => To request a teleportation to a player");
                if(useTokens)
                {
                    var tokensLeft = GetPlayerTokens(netuser);
                    SendReply(netuser, string.Format("You have {0} tokens left",tokensLeft.ToString()));
                }
                return;
            }
            if (TPIncoming.ContainsKey(netuser)) { SendReply(netuser, "[color red]You have an incoming teleportation request, you must wait before using this command."); return; }
            if (TPRequest.ContainsKey(netuser)) { SendReply(netuser, "[color red]You have already requested a teleportation, you must wait before using this command."); return; }

            if (lastRequest.ContainsKey(netuser))
            {
                if(Time.realtimeSinceStartup - lastRequest[netuser] < (float)tprCooldown)
                {
                    SendReply(netuser, string.Format("[color cyan]You [color red]must wait [color cyan]{0}s [color red]before requesting another teleportation.", ((float)tprCooldown - (Time.realtimeSinceStartup - lastRequest[netuser])).ToString()));
                    return;
                }
            }
			if(Ifinshack(netuser)) {
				SendReply(netuser, " [color cyan]You [color red]are not allowed to teleport from shelter's");
				return;
			}
            if (IfNearStructure(netuser))
            {
                SendReply(netuser, " [color cyan]You [color red]are not allowed to teleport close to building's.");
                return;
            }
            if (!ifOnGround(netuser)) {
				SendReply(netuser, " [color cyan]You [color red]are not allowed to teleport on building's");
				return;
			}
			if(ifOnDeployable(netuser)) {
				SendReply(netuser, " [color cyan]You [color red]are on a object so you can't teleport.");
				return;
			}
			if(ifOnlootDeployable(netuser)){
				SendReply(netuser, " [color cyan]You [color red]are on a lootable object so you can't teleport.");
				return;
			}
			if(ifOnlootsackDeployable(netuser)){
				SendReply(netuser, " [color cyan]You [color red]are on a lootable object so you can't teleport.");
				return;
			}
			
            if(useTokens)
            {
                var tokensLeft = GetPlayerTokens(netuser);
                if(tokensLeft < 1)
                {
                    SendReply(netuser, " You don't have any more tokens left to teleport");
                    return;
                }
            }
           
            NetUser targetPlayer = rust.FindPlayer(args[0]);
            if(targetPlayer == null) { SendReply(netuser, "Target player doesn't exist"); return; }
            if(TPIncoming.ContainsKey(targetPlayer)) { SendReply(netuser, "[color red]Target player already has a pending request."); return; }

            object thereturn = Interface.GetMod().CallHook("canTeleport", new object[] { netuser });
            if (thereturn != null)
            {
                SendReply(netuser, "[color red]You are not allowed to teleport from where you are.");
                return;
            }
            thereturn = Interface.GetMod().CallHook("canTeleport", new object[] { targetPlayer });
            if (thereturn != null)
            {
                SendReply(netuser, "[color red]You are not allowed to teleport to where the target is.");
                return;
            }
			if (targetPlayer == netuser) {
				SendReply(netuser, "[color red]You can't teleport to yourself.");
				return;
			}
			if(!ifOnGround(targetPlayer)) {
				SendReply(netuser, string.Format("[color cyan]{0} [color red]is on a building so you can't teleport.", targetPlayer.displayName));
				SendReply(targetPlayer, string.Format("[color cyan]You [color red]are on a building so [color cyan]{0} [color red]couldn't teleport to you.", netuser.displayName));
				return;
			}
            if (IfNearStructure(targetPlayer))
            {
                SendReply(netuser, string.Format("[color cyan]{0} [color red]is standing to close to a wall. Unable to teleport to him.", targetPlayer.displayName));
                SendReply(targetPlayer, string.Format("[color cyan]You [color red]are standing to close to a building [color cyan]{0} [color red]couldn't teleport to you.", netuser.displayName));
                return;
            }
            if (ifOnDeployable(targetPlayer)) {
				SendReply(netuser, string.Format("[color cyan]{0} [color red]is on an object so you can't teleport.", targetPlayer.displayName));
				SendReply(targetPlayer, string.Format("[color cyan]You [color red]are on an object so [color cyan]{0} [color red]couldn't teleport to you.", netuser.displayName));
				return;
			}
			if(ifOnlootsackDeployable(targetPlayer)){
				SendReply(netuser, string.Format("[color cyan]{0} [color red]is on a lootsack so you can't teleport.", targetPlayer.displayName));
				SendReply(targetPlayer, string.Format("[color cyan]You [color red]are on an object so [color cyan]{0} [color red]couldn't teleport to you.", netuser.displayName));
				return;
			}
			if(ifOnlootDeployable(targetPlayer)){
				SendReply(netuser, string.Format("[color cyan]{0} [color red]is on a loot bag so you can't teleport.", targetPlayer.displayName));
				SendReply(targetPlayer, string.Format("[color cyan]You [color red]are on an object so [color cyan]{0} [color red]couldn't teleport to you.", netuser.displayName));
				return;
			}
			
            TPRequest.Add(netuser, targetPlayer);
            TPIncoming.Add(targetPlayer, netuser);
            timersList.Add(netuser, timer.Once(10f, () => ResetRequest(netuser)));
            SendReply(netuser, string.Format("[color cyan]You [color orange]have sent a request to [color cyan]{0}.",targetPlayer.displayName));
            SendReply(targetPlayer, string.Format("[color cyan]You've received a teleportation request from [color cyan]{0}. [color orange]/tpa to accept, /tpc to reject.", netuser.displayName));
        }
        [ChatCommand("tpa")]
        void cmdChatTeleportAccept(NetUser netuser, string command, string[] args)
        {
            if(!TPIncoming.ContainsKey(netuser)) { SendReply(netuser, "[color red]You don't have any incoming request."); return; }
            var targetuser = TPIncoming[netuser];
			
			if (targetuser == netuser) {
				SendReply(netuser, "[color cyan]You [color red]can't teleport to yourself.");
				return;
			}
            AcceptRequest(TPIncoming[netuser]);
            SendReply(netuser, string.Format( "[color cyan]You have accepted the teleportation request from [color orange]{0}.", targetuser.displayName));
            SendReply(targetuser, string.Format("[color cyan]{0} [color orange]has accepted your teleportation request.", netuser.displayName));
        }
        [ChatCommand("tpc")]
        void cmdChatTeleportCancel(NetUser netuser, string command, string[] args)
        {
			FallDamage falldamage = netuser.playerClient.rootControllable.GetComponent<FallDamage>();
            if (TPIncoming.ContainsKey(netuser))
            {
				ClearLegInjury(falldamage);
                var targetplayer = TPIncoming[netuser];
                SendReply(netuser, string.Format("[color cyan]You have rejected [color orange]{0}'s request.", targetplayer.displayName));
                SendReply(targetplayer, string.Format("{0} has rejected your request.", netuser.displayName));
                TPIncoming.Remove(netuser);
                if (timersList.ContainsKey(targetplayer))
                {
                    timersList[targetplayer].Destroy();
                    timersList.Remove(targetplayer);
                }
                TPRequest.Remove(targetplayer);
                return;
            }
			ClearLegInjury(falldamage);
            ResetRequest(netuser);
            SendReply(netuser, "[color orange]You have cancelled all current teleportations.");
        }
        void SendHelpText(NetUser netuser)
        {
            SendReply(netuser, "Teleportation Requests: /tpr PLAYERNAME");
            SendReply(netuser, "Teleportation Cancel: /tpc");
        }
    }
}