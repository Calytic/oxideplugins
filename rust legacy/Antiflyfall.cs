// Reference: Facepunch.ID
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


using Google.ProtocolBuffers;
using Google.ProtocolBuffers.Collections;
using Google.ProtocolBuffers.Descriptors;
using Google.ProtocolBuffers.FieldAccess;
using RustProto.Helpers;
using System.Collections.Generic;
using System.IO;

namespace Oxide.Plugins
{
    [Info("Antiflyfall - devcheck v1", "copper", "3.1.0")]
    class Antiflyfall : RustLegacyPlugin
    {
		[PluginReference] Plugin Banip;
        /////////////////////////////
        // FIELDS
        /////////////////////////////
        NetUser cachedUser;
        string cachedSteamid;
		public bool finishedcheck;
        string cachedReason;
        string cachedName;
        Vector3 cachedPos;
        RaycastHit cachedRaycast;
		public static  RaycastHit cachedRaycasttt;
        Vector3 vectorup = new Vector3(0f, 1f, 0f);
		public static Vector3 vectorup2 = new Vector3(0f, 1f, 0f);
        int terrainLayer;
        public static Dictionary<NetUser, Vector3> teleportBack = new Dictionary<NetUser, Vector3>();
        private Core.Configuration.DynamicConfigFile Data;
		private Core.Configuration.DynamicConfigFile Info;
		private Core.Configuration.DynamicConfigFile Pldata;
		private Core.Configuration.DynamicConfigFile Ipban;
		void Unload() { SaveData(); }
		static int terrainLayerr;

        Vector3 VectorUp = new Vector3(0f, 1f, 0f);
        Vector3 VectorDown = new Vector3(0f, -0.4f, 0f);
		Vector3 VectorDownn = new Vector3(0f, -0.1f, 0f);
		
        public static Vector3 Vector3Down = new Vector3(0f,-1f,0f);
		public static Vector3 Vector3Down2 = new Vector3(0f,-3f,0f);
        public static Vector3 Vector3Up = new Vector3(0f, 1f, 0f);
        public static Vector3 UnderPlayerAdjustement = new Vector3(0f, -1.15f, 0f);
		public static Vector3 UnderPlayerAdjustement2 = new Vector3(0f, -1.16f, 0f);
        public static float distanceDown = 10f;
		
        Collider[] cachedColliders;

        RustServerManagement management;
		
		public static RustServerManagement management2;

        /////////////////////////////
        // Data Management
        /////////////////////////////

        void LoadData()
        {
			Ipban = Interface.GetMod().DataFileSystem.GetDatafile("Blacklist(ip)");
            Data = Interface.GetMod().DataFileSystem.GetDatafile("Antiflyfall(ac.pl)");
			Info = Interface.GetMod().DataFileSystem.GetDatafile("Antiflyfall(pl)");
			Pldata = Interface.GetMod().DataFileSystem.GetDatafile("Antiflyfall(name's)");
        }
        void SaveData()
        {
			
            Interface.GetMod().DataFileSystem.SaveDatafile("Antiflyfall(ac.pl)");
			Interface.GetMod().DataFileSystem.SaveDatafile("Antiflyfall(pl)");
			Interface.GetMod().DataFileSystem.SaveDatafile("Antiflyfall(name's)");
        }
		void ceilingglitchcheck(NetUser netuser)
		{
			Collider cachedCollider;
			bool cachedBoolean;
			Vector3 cachedvector3;
			RaycastHit cachedRaycast;
			Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
		}
		
		bool ifOnGround(NetUser netusery) {
			
            PlayerClient playerclient = netusery.playerClient;
			Vector3 lastPosition = playerclient.lastKnownPosition;
			
			
			Collider cachedCollider;
			bool cachedBoolean;
			Vector3 cachedvector3;
			RaycastHit cachedRaycast;
			Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
			

			if (lastPosition == default(Vector3)) return true;
			if (!MeshBatchPhysics.Raycast(lastPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { return true; }
			if (cachedhitInstance == null) {  return true; }
			if (cachedhitInstance.graphicalModel.ToString() == null) {
				//Put(cachedhitInstance.graphicalModel.ToString());
				return true;
			}
			
			return false;
		}


        /////////////////////////////
        // Config Management
        /////////////////////////////

        public static string systemname = "Derpteamgames";
		public static string isbannedd = "has been banned from the server for (glitch raid)";
		public static int timertocheck = 1;
		public static double timertochecksecond = 0.1;
		public static double nofalltimer = 4.95;
		public static double beguincheck = 2.0;
		public static double Antiflyhacktimer = 0.5;
		public static double timertocheckceiling = 0.45;
		public static string tpmsg = "you are beign teleported for security reasons";
		public static string cachedreason = "Antflyfall(autoban location : 0.0 0.0 0.0)";
		public static string notAllowed = "You are not allowed to use this command.";
		public static bool shouldsendtelemsgtouser = false;
		public static string cachedreasonflyhack = "AntiFyfall(Autoban FlyHack)";
		public static string cachedreasonceiling = "AntiFyfall(Autoban ceiling hack/glitch)";
		public static bool shouldtpback = true;
		public static bool shouldbanforceilinghack = false;
		public static bool shouldsendcustommsg = false;
		public static bool shouldanticeilinghack = true;
		public static bool shouldsenddefaultmsg = true;
		public static bool shouldlogallnames = false;
		public static bool shouldban = false;
		public static bool shouldpunishwasteland = true;
		public static bool shouldbanflyhack = true;
		public static bool shouldsaveonserversave = false;
		public static bool shouldbroadcasttryconneect = true;
		public static bool autocorrect = false;
		public static bool Antiflyhack = true;
		public static bool Antinofall = true;
		public static bool secondchecknofall = true;
		public static bool shouldpreventpsleepglitch = true;
		public static bool shouldcheckenhacnednofall = true;
		public static bool shouldchecknorm = true;
		public static bool monoantinofall = false;
		
		public static bool shoulldcheckfalldamgaefourth = true;
		
		public static bool shouldcheckkthrdfalldamage = true;
		public static string bannedlocation = "0";
		public static string cachedreasonnofall = "AntiFlyfall(Nofall damage)";
		public static string reportto = ";http://oxidemod.org/threads/antiflyfall.12867/)";
		public static float pingexcepton = 500;
		

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
			CheckCfg<string>("Messages: Open Beta this is a early release of v 3.1.0 of the plugin mono meathod has been disabled for further testing and may be enabled in futer update there may also be bugs if so plz report the bugs to ", ref reportto);
			CheckCfg<bool>("Messages: send teleportation message to user", ref shouldsendtelemsgtouser);
			CheckCfg<bool>("Messages: should check nofall damage a fourth time", ref shoulldcheckfalldamgaefourth);
			CheckCfg<bool>("Messages: should check nofall damage a thrd time", ref shouldcheckkthrdfalldamage);
			CheckCfg<bool>("Messages: should check nofall normal method this si for servers with fall damage enabled", ref shouldchecknorm);
			CheckCfg<bool>("Messages: should check enhanced nofall will make nofall and fly hack dettections more effective(will make super jump dettections less effecient still works but less effective)", ref shouldcheckenhacnednofall);
			CheckCfg<bool>("Messages: check antinofall mono method", ref monoantinofall);
			CheckCfg<float>("Messages: ping exception limit", ref pingexcepton);
			CheckCfg<bool>("Messages: shoud prevent sleep glitch (not needed if you dont have sleepers enabled)", ref shouldpreventpsleepglitch);
			CheckCfg<bool>("Messages: should check second nofalldamage (makes sure they are using it)", ref secondchecknofall);
			CheckCfg<bool>("Messages: should ban for ceiling hack", ref shouldbanforceilinghack);
			CheckCfg<bool>("Messages: check nofalldamage ", ref Antinofall);
			CheckCfg<bool>("Messages: shouldban for flyhack ", ref shouldbanflyhack);
			CheckCfg<bool>("Antiflyhack: check for flyhack on connect", ref Antiflyhack);
			CheckCfg<bool>("Messages: should Anticeiling hack ", ref shouldanticeilinghack);
			CheckCfg<bool>("Messages: should broadcast ban", ref shouldbroadcasttryconneect);
			CheckCfg<bool>("Messages: should punish by tp player to hacker valley", ref shouldpunishwasteland);
			CheckCfg<bool>("Messages: should log all names", ref shouldlogallnames);
			CheckCfg<bool>("Messages: should ban(WARNING ENABLE AT YOUR OWN RISK innocent players tend to get dettected by this)", ref shouldban);
			CheckCfg<int>("Messages: timer to check", ref timertocheck);
			CheckCfg<double>("Messages: timer to check no fall damage (0.65 should be good if you ant it to be real fast try it out and see what works for you)", ref nofalltimer);
			CheckCfg<double>("Messages: Antiflyhack timer", ref Antiflyhacktimer);
			CheckCfg<double>("Messages: timer to check second", ref timertochecksecond);
			CheckCfg<double>("Messages: timer to start check", ref beguincheck);
			CheckCfg<double>("Messages: timer to check for ceiling glitch/hack", ref timertocheckceiling);
			CheckCfg<string>("Messages: bannedlocation", ref bannedlocation);
			CheckCfg<string>("Messages: flyhack ban reason", ref cachedreasonflyhack);
			CheckCfg<string>("Messages: addded ban broadcast chat", ref isbannedd);
			CheckCfg<bool>("Messages: should send default message", ref shouldsenddefaultmsg);
			CheckCfg<string>("Messages: custom message", ref tpmsg);
			CheckCfg<string>("Messages: nofall dmg autoban", ref cachedreasonnofall);
			CheckCfg<string>("Messages: ban reason", ref cachedreason);
			CheckCfg<string>("Messages: ban reason ceiling hack", ref cachedreasonceiling);
			CheckCfg<bool>("Messages: should send custom msg", ref shouldsendcustommsg);
			CheckCfg<string>("Messages: systemname", ref systemname);
            CheckCfg<string>("Messages: Not Allowed", ref notAllowed);
			CheckCfg<bool>("Messages: should save data on server save (not needed)", ref shouldsaveonserversave);
			CheckCfg<bool>("Messages: autocorrect systemcheck(makes sure everything is running smooth)", ref autocorrect);
			CheckCfg<bool>("Messages: should tp to hacker valley", ref shouldtpback);
            SaveConfig();
        }


        /////////////////////////////
        // Oxide Hooks
        /////////////////////////////

        void Loaded()
        {
            if (!permission.PermissionExists("cantpslper")) permission.RegisterPermission("cantpslper", this);
            terrainLayer = LayerMask.GetMask(new string[] { "Terrain" });
			terrainLayerr = LayerMask.GetMask(new string[] { "Static" });
            LoadData();
        }
        void OnServerSave()
		{
			if(shouldsaveonserversave)
			{
				SaveData();
			}
		}
        void OnServerInitialized()
        {
            management = RustServerManagement.Get();
			management2 = RustServerManagement.Get();
        }


		bool hasAccess(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
            if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "cantpslper")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }
		void SendHelpText(NetUser netuser)
        {
            if (hasAccess(netuser, "cantpslpr")) SendReply(netuser, "Antiflyfall tpslpr: /tpslpr PLAYERNAME");
        }


        /////////////////////////////
        // Teleportation Functions
        /////////////////////////////
		
		public class PlayerHandler : MonoBehaviour
		{
			public Vector3 point;
			public bool firsttime;
			public float timeleft;
            public float lastTick;
            public float currentTick;
            public float deltaTime;
			public float firstx;
			public float firsty;
			public int count;
			public float firstz;
			public float component2distance;
			public Component componenthit2;
            public Vector3 lastPosition;
			public Vector3 headlocation2;
			public float headlocation3;
			public float headlocationangle2;
			public float headlocationangle1;
			public float checkrotaionx;
			public float totaleularanglesplayerhandler;
			public Vector3 lastPosition2;
            public PlayerClient playerclient;
            public Character character;
            public Inventory inventory;
            public string userid;
			public float currenthealth;
            public float distance3D;
			public float distance3D2;
            public float distanceHeight;
			public bool hascurrenthealth = false;
			public bool startedhealthcheck;

            public float currentFloorHeight;
            public bool hasSearchedForFloor = false;

            public float lastSpeed = Time.realtimeSinceStartup;
            public int speednum = 0;


            public float lastWalkSpeed = Time.realtimeSinceStartup;
            public int walkspeednum = 0;
            public bool lastSprint = false;

            public float lastJump = Time.realtimeSinceStartup;
            public int jumpnum = 0;


            public float lastFly = Time.realtimeSinceStartup;
            public int flynum = 0;

            public int noRecoilDetections = 0;
            public int noRecoilKills = 0;

            public float lastWoodCount = 0;

            void Awake()
            {
                lastTick = Time.realtimeSinceStartup;
                enabled = false;
            }
            public void StartCheck()
            {
                this.playerclient = GetComponent<PlayerClient>();
                this.userid = this.playerclient.userID.ToString();
                if (playerclient.controllable == null) return;
                this.character = playerclient.controllable.GetComponent<Character>();
                this.lastPosition = this.playerclient.lastKnownPosition;
                enabled = true;
				FixedUpdate();
            }
            void FixedUpdate()
            {
                if (Time.realtimeSinceStartup - lastTick >= 1)
                {
                    currentTick = Time.realtimeSinceStartup;
                    deltaTime = currentTick - lastTick;
                    distance3D = Vector3.Distance(playerclient.lastKnownPosition, lastPosition) / deltaTime;
                    distanceHeight = (playerclient.lastKnownPosition.y - lastPosition.y) / deltaTime;
                    checknofall(this);
                    lastPosition = playerclient.lastKnownPosition;
                    lastTick = currentTick;
                    this.hasSearchedForFloor = false;
                }
            }
		}
		public static void checknofallnorm(PlayerHandler player)
		{
			return;
			if(player.playerclient.controllable == null)
			{
				player.hascurrenthealth = false;
				return;
			}
			FallDamage falldamage = player.playerclient.rootControllable.GetComponent<FallDamage>();
			if (PlayerHandlerHasGround(player))
			{
				if(player.startedhealthcheck)
				{
					player.startedhealthcheck = false;
				}
				return;
			}
			player.startedhealthcheck = true;
			NetUser netuser = player.playerclient.netUser;
			if(!falldamage.enabled)
				return;
			if(shouldchecknorm)
			{
				if(shouldchecknorm)
				{
					if(!player.hascurrenthealth)
					{
						player.currenthealth = player.playerclient.controllable.health;
						player.hascurrenthealth = true;
					}
						
				}
			}
		}
		public static void checknofall(PlayerHandler player)
		{
			return;
			NetUser netuser = player.playerclient.netUser;
			var enhancedcheck = player.lastPosition.y;
			var newcheckdistance = (enhancedcheck - player.playerclient.lastKnownPosition.y);
			var ulongcheck2 = (Math.Abs(newcheckdistance));
			var ulongcheck = (Math.Abs(player.distanceHeight));
			return;
			if (PlayerHandlerHasGround(player)) return;
			if (IsOnSupport(player)) return;
			var distanceenhanced2 = (Math.Abs(newcheckdistance));
			if(distanceenhanced2 < 20)
			{
				player.count++;
			}
			if(!shouldcheckenhacnednofall) return;
			if (PlayerHandlerHasGround(player)) return;
			var time = Time.realtimeSinceStartup;
			
			var distanceenhanced = (Math.Abs(player.distanceHeight));

			if(ulongcheck <= 15)
			{
				return;
			}
			if(ulongcheck < 20)
			{
				checknofall2(player);
				player.count++;
				return;
			}
			if(ulongcheck >= 50)
			{
				
				Debug.Log("is greater");
			}
			if(player.firsttime == true && ulongcheck <= 90)
			{
				if(player.count >= 2){
				Debug.Log("count is greater");}
				if(player.count <= 4){
					Debug.Log("count is less than 4");
					Debug.Log(player.count);
				}
				player.count = 0;
				Debug.Log("player.firstime is not false");
				player.firsttime = false;
				TeleportToPos2(netuser, player.firstx, player.firsty, player.firstz);
			}
			 Debug.Log("doesnot has ground");
			var thisposition = player.playerclient.lastKnownPosition.y;
			if(ulongcheck < 20 && ulongcheck > 8)
				Debug.Log("nofall damage hack dettected");
			Debug.Log(ulongcheck);
			 foreach (PlayerClient playerr in PlayerClient.All)
			 {
				if(player != playerr)
				{
					var client = playerr.controllable;
					player.playerclient.controllable.RelativeControlTo(client);
					
				}
			 }
			 return;
			 var datatest = player.playerclient.instantiationTimeStamp;
			 var character = player.playerclient.controllable.CreateCCMotor();
			 Debug.Log(datatest);
			 return;
			 player.playerclient.controllable.ccmotor.minTimeBetweenJumps = 0.55f;
			 var character3 = player.playerclient.controllable.ccmotor.minTimeBetweenJumps;
			 Debug.Log(character3);
			 return;
			 Debug.Log(character);
			 if (PlayerHandlerHasGround(player)) return;
		}
		static void checknofall2(PlayerHandler player)
		{
			var time = Time.realtimeSinceStartup;
			if(player.firsttime != true)
			{
				var firstx = player.playerclient.lastKnownPosition.x;
				var firsty = player.playerclient.lastKnownPosition.y;
				var firstz = player.playerclient.lastKnownPosition.z;
				player.firstx = firstx;
				player.firsty = firsty;
				player.firstz = firstz;
				player.firsttime = true;
			}
			NetUser netuser = player.playerclient.netUser;
			TeleportToPos2(netuser, 0.0f, 60000f, 0.0f);
		}
		static bool PlayerHandlerHasGround(PlayerHandler player)
        {
        if (!player.hasSearchedForFloor)
         {
			 if (Physics.Raycast(player.playerclient.lastKnownPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycasttt, distanceDown))
				 player.currentFloorHeight = cachedRaycasttt.distance;
             else
				 player.currentFloorHeight = 10f;
         }
         player.hasSearchedForFloor = true;
         if (player.currentFloorHeight < 4f) return true;
        return false;
        }
        static bool IsOnSupport(PlayerHandler player)
        {
            foreach( Collider collider in Physics.OverlapSphere(player.playerclient.lastKnownPosition, 5f))
            {
                if (collider.GetComponent<UnityEngine.MeshCollider>())
                    return true;
            }
            return false;
        }

        void DoTeleportToPlayer(NetUser source, NetUser target)
        {
            management.TeleportPlayerToPlayer(source.playerClient.netPlayer, target.playerClient.netPlayer);
            SendReply(source, string.Format("You teleported to {0}", target.playerClient.userName));
        }
		static void TeleportToPos2(NetUser source, float x, float y, float z)
        {
            if (Physics.Raycast(new Vector3(x, -1000f, z), vectorup2, out cachedRaycasttt, Mathf.Infinity, terrainLayerr))
            {
                if (cachedRaycasttt.point.y > y) y = cachedRaycasttt.point.y;
            }
            management2.TeleportPlayerToWorld(source.playerClient.netPlayer, new Vector3(x, y, z));
			if(shouldsendtelemsgtouser)
			{
				if(shouldsendcustommsg)
				{
				}
				if(shouldsenddefaultmsg)
				{
				}
			}
        }
        void TeleportToPos(NetUser source, float x, float y, float z)
        {
            if (Physics.Raycast(new Vector3(x, -1000f, z), vectorup, out cachedRaycast, Mathf.Infinity, terrainLayer))
            {
                if (cachedRaycast.point.y > y) y = cachedRaycast.point.y;
            }
            management.TeleportPlayerToWorld(source.playerClient.netPlayer, new Vector3(x, y, z));
			if(shouldsendtelemsgtouser)
			{
				if(shouldsendcustommsg)
				{
					rust.SendChatMessage(source, systemname,  tpmsg);
				}
				if(shouldsenddefaultmsg)
				{
					SendReply(source, string.Format("for security reasons you have been teleported to  {0} {1} {2}", x.ToString(), y.ToString(), z.ToString()));
				}
			}
        }

        /////////////////////////////
        // Random Functions
        /////////////////////////////

		Dictionary<string, object> Getactivepl(string userid)
		{
			if (Data[userid] == null)
				Data[userid] = new Dictionary<string, object>();
			return Data[userid] as Dictionary<string, object>;
		}


		Dictionary<string, object> GetPlayer(string userid)
		{
			if (Info[userid] == null)
				Info[userid] = new Dictionary<string, object>();
			return Info[userid] as Dictionary<string, object>;
		}
		Dictionary<string, object> GetPlayername(string userid)
		{
			if (Pldata[userid] == null)
				Pldata[userid] = new Dictionary<string, object>();
			return Pldata[userid] as Dictionary<string, object>;
		}
		Dictionary<string, object> Getipban(string userid)
		{
			if (Ipban[userid] == null)
				Ipban[userid] = new Dictionary<string, object>();
			return Ipban[userid] as Dictionary<string, object>;
		}
		void ipban(NetUser source, string cachedreason)
		{
			var ip = source.networkPlayer.externalIP;
			var name = source.displayName;
			var GetPlayerdata = Getipban("Blacklist(ip)");
			if(GetPlayerdata.ContainsKey(ip))
			{
				return;
			}
			GetPlayerdata.Add(ip, name + " " + cachedreason);
			
		}
		void OnPlayerDisconnected(uLink.NetworkPlayer netplayer)
		{
			
			var netcheck = netplayer.internalIP;
			if(netplayer == null)
				return;
			PlayerClient player = ((NetUser)netplayer.GetLocalData()).playerClient;
			if(player.controllable == null)
			{
				var addeception = Getactivepl("addeception");
				if(!addeception.ContainsKey(player.userID.ToString()))
				addeception.Add(player.userID.ToString(), true);
				return;
			}
			if(player.controllable.GetComponent<Character>() == null)
				return;
			var test1 = (player.controllable.GetComponent<Character>());
			var iq = player.lastKnownPosition.ToString();
			var x = player.lastKnownPosition.x.ToString();
            var y = player.lastKnownPosition.y.ToString();
            var z = player.lastKnownPosition.z.ToString();
			var pl = GetPlayer(player.userID.ToString());
			var gg = player.lastKnownPosition.ToString();
			var netUser = netplayer.GetLocalData<NetUser>();
			var ip = netUser.networkPlayer.externalIP;
			var name = netUser.displayName;
			
			Vector3 lastPosition = player.lastKnownPosition;
			
			
			Collider cachedCollider;
			bool cachedBoolean;
			bool cachedBoolean2;
			Vector3 cachedvector3;
			RaycastHit cachedRaycast;
			RaycastHit cachedRaycastt;
			Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
			Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance2;

			if (!MeshBatchPhysics.Raycast(lastPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { return; }
			var raycastid = cachedRaycast.collider.gameObject.transform.position;
			if (!MeshBatchPhysics.Raycast(raycastid + UnderPlayerAdjustement, Vector3Down, out cachedRaycastt, out cachedBoolean2, out cachedhitInstance2)) {  }
			if(cachedhitInstance2 != null)
			{
			
			var cachedCollider2 = cachedhitInstance2.physicalColliderReferenceOnly;
			var cachedstructure2 = cachedCollider2.GetComponent<StructureComponent>();
			var cachedstructureobjectid2 = cachedstructure2._master.ownerID;
			if(shouldanticeilinghack)
			{
				if(cachedstructure2 != null)
				{
					if(!pl.ContainsKey("CeilingCheck"))
					{
						pl.Add("CeilingCheck", true);
					}
					if(pl.ContainsKey("cachedobjectlocation"))
					{
						var checkpl = pl["cachedobjectlocation"].ToString();
						if(checkpl == cachedstructure2.gameObject.transform.position.ToString())
						{
						}
						else
						{
							pl.Remove("cachedobjectlocation");
						}
						if(pl.ContainsKey("OwnerId"))
						{
							if(pl["OwnerId"].ToString() == cachedstructureobjectid2.ToString())
							{
								
							}
							else
							{
								pl.Remove("OwnerId");
							}
						}
					}
					if(!pl.ContainsKey("OwnerId"))
					{
						pl.Add("OwnerId", cachedstructureobjectid2.ToString());
					}
					if(!pl.ContainsKey("cachedobjectlocation"))
					{
						pl.Add("cachedobjectlocation", cachedstructure2.gameObject.transform.position.ToString());
					}
				}
			}}
			if(cachedhitInstance != null)
			{
			var cachedCollider3 = cachedhitInstance.physicalColliderReferenceOnly;
			var cachedstructure = cachedCollider3.GetComponent<StructureComponent>();
			var objectowner = cachedstructure._master.ownerID;
			if(shouldanticeilinghack)
			{
				if(!ifOnGround(netUser))
				{
					if(!pl.ContainsKey("CeilingCheck"))
					{
						pl.Add("CeilingCheck", true);
					}
					if(pl.ContainsKey("cachedobjectlocation"))
					{
						var checkpl = pl["cachedobjectlocation"].ToString();
						if(checkpl == cachedstructure.gameObject.transform.position.ToString())
						{
						}
						else
						{
							pl.Remove("cachedobjectlocation");
						}
						if(pl.ContainsKey("OwnerId"))
						{
							if(pl["OwnerId"].ToString() == objectowner.ToString())
							{
								
							}
							else
							{
								pl.Remove("OwnerId");
							}
						}
					}
					if(!pl.ContainsKey("OwnerId"))
					{
						pl.Add("OwnerId", objectowner.ToString());
					}
					if(!pl.ContainsKey("cachedobjectlocation"))
					{
						pl.Add("cachedobjectlocation", cachedstructure.gameObject.transform.position.ToString());
					}
				}
			}}
			if(!shouldpunishwasteland)
			{
				if(x == bannedlocation)
				{
					if(y == bannedlocation)
					{
						if(z == bannedlocation)
						{
							return;
						}
					}
				}
			}
			if(pl.ContainsKey("x"))
			{
				pl.Remove("x");
				pl.Remove("y");
				pl.Remove("z");
				pl.Remove("total");
				pl.Remove("name");
				pl.Remove("ip");
			}
			pl.Add("x", x);
			pl.Add("y", y);
			pl.Add("z", z);
			pl.Add("total", x + " " + y + " " + z);
			pl.Add("name", name);
			pl.Add("ip", ip);
			if(shouldlogallnames)
			{
				var getname = GetPlayername(name);
				if(getname.ContainsKey("x"))
				{
					getname.Remove("total");
					getname.Remove("id");
					getname.Remove("x");
					getname.Remove("y");
					getname.Remove("z");
					getname.Remove("ip");
					getname.Remove("name");
				}
				getname.Add("x", x);
				getname.Add("y", y);
				getname.Add("z", z);
				getname.Add("total", x + " " + y + " " + z);
				getname.Add("id", player.userID.ToString());
				getname.Add("ip", ip);
				getname.Add("name", player.userName);
			}
		}
		void Broadcast(string message)
        {
            ConsoleNetworker.Broadcast("chat.add " + systemname + " "+ Facepunch.Utility.String.QuoteSafe(message));
        }
		void OnPlayerConnected(NetUser netuser)
		{
			var ac = Getactivepl("Active(pl)");
			var id = netuser.playerClient.userID.ToString();
			var name = netuser.displayName;
			if(!ac.ContainsKey(id))
			{
				ac.Add(id, name);
			}
			if(shouldban)
			{
				
				var pl = GetPlayer(netuser.playerClient.userID.ToString());
				if(!pl.ContainsKey("x")) return;
				var x = pl["x"].ToString();
				var y = pl["y"].ToString();
				var z = pl["z"].ToString();
				if(x == bannedlocation)
				{
					if(y == bannedlocation)
					{
						if(z == bannedlocation)
						{
							Interface.CallHook("cmdBan", id.ToString(), name, cachedreason);
							pl.Remove("x");
							pl.Remove("y");
							pl.Remove("z");
							if(shouldbroadcasttryconneect)
							{
								Broadcast(string.Format(name + " " + isbannedd));
							}
							return;
						}
					}
				}
			}
		}
		void doteleport(NetUser netuser, string targetid2)
		{
			var pl = GetPlayername(targetid2.ToString());
			if(pl.ContainsKey("name"))
			{
				if(pl.ContainsKey("x")){
				var x = pl["x"].ToString();
				var y = pl["y"].ToString();
				var z = pl["z"].ToString();
				rust.SendChatMessage(netuser, systemname, "you have teleported to " + targetid2);
				TeleportToPos(netuser, Convert.ToSingle(pl["x"]), Convert.ToSingle(pl["y"]), Convert.ToSingle(pl["z"]));
				return;
			    }
			}
		}
		[ChatCommand("tpslp")]
		void cmdChatAddofflinebanip(NetUser netuser, string command, string[] args)
		{
			if(!shouldlogallnames)
			{
				rust.SendChatMessage(netuser, systemname, "should logallnames is disabled plz enable it to use this command");
				return;
			}
			if (!hasAccess(netuser, "cantpslper")) { rust.SendChatMessage(netuser, systemname, "you are not allowed to use this command"); return; }
			if (args.Length != 1)
			{
				rust.SendChatMessage(netuser, systemname, "wrong syntax: /tpslp 'playername < must be precise");
				return;
			}
			var pl = GetPlayername(args[0]);
			int count = 0;
			foreach (KeyValuePair<string, object> pair in Pldata)
            {
				if(count >= 1)
				{
					break;
					return;
					continue;
				}
				 var currenttable = pair.Value as Dictionary<string, object>;
				 if (currenttable.ContainsKey("name"))
                    {
                        if (currenttable["name"].ToString().ToLower().Contains(args[0]) || currenttable["name"].ToString() == args[0] || currenttable["name"].ToString().ToLower() == args[0])
                        {
							count++;
							doteleport(netuser, currenttable["name"].ToString());
                        }
                    }
               }
			
		}
		void newfunction(NetUser source)
		{
		    return;
			if (source == null || source.playerClient == null) return;
			if (source == null || source.playerClient.controllable == null) return;
			var ply =  source.playerClient.lastKnownPosition.y;
			var pl = GetPlayer(source.userID.ToString());
			var y = pl["y"];
			var q = Convert.ToSingle(pl["y"]);
			if(ply == q)
			{
				return;
			}
			if(ply < q)
			{
				rust.SendChatMessage(source, systemname, "our system's have dettected a glitch raid on your account");
				rust.SendChatMessage(source, systemname, ply.ToString());
				rust.SendChatMessage(source, systemname, q.ToString());
				return;
			}
			if(ply > q)
			{
				rust.SendChatMessage(source, systemname, "your hight is higher than usual");
			}
			rust.SendChatMessage(source, systemname, "you are not a glitch raider");
		}
		bool ceilingglitchfixcheck2(NetUser source)
		{
			return true;
		}
		void newfunctionphaseceiling(NetUser source)
		{
			if (source == null || source.playerClient.controllable == null) return;
			if (source == null || source.playerClient == null) return;
			if(ifOnGround(source))
			{
				return;
			}
			var ply =  source.playerClient.lastKnownPosition.y;
			var pl = GetPlayer(source.userID.ToString());
			var y = pl["y"];
			var q = Convert.ToSingle(pl["y"]);
			var fix = (q - 0.01f);
			var fixfly = (q + 0.01f);
			var checkceiling = pl["cachedobjectlocation"].ToString();
			Vector3 lastPosition = source.playerClient.lastKnownPosition;
			
			
			Collider cachedCollider;
			bool cachedBoolean;
			bool cachedBoolean2;
			Vector3 cachedvector3;
			RaycastHit cachedRaycast;
			RaycastHit cachedRaycastt;
			Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
			Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance2;
			

			if (!MeshBatchPhysics.Raycast(lastPosition + UnderPlayerAdjustement, Vector3Up, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { return; }
			var raycastid = cachedRaycast.collider.gameObject.transform.position;
			if (!MeshBatchPhysics.Raycast(raycastid + UnderPlayerAdjustement, Vector3Down, out cachedRaycastt, out cachedBoolean2, out cachedhitInstance2)) {  }
			if(cachedhitInstance2 != null)
			{
				var cachedCollider2 = cachedhitInstance2.physicalColliderReferenceOnly;
				var cachedstructure2 = cachedCollider2.GetComponent<StructureComponent>();
				var cachedstructureobjectid2 = cachedstructure2._master.ownerID;
				var newy2 = Convert.ToSingle(pl["y"]);
				var tot2 = (ply - newy2);
				var totmath2 = (Math.Abs(tot2));
				var objectownerid2 = pl["OwnerId"].ToString();
				var objectlocation2 = cachedstructure2.gameObject.transform.position;
				if(tot2 < -4.021912 && objectownerid2 == cachedstructureobjectid2.ToString());
				{
					if(totmath2 < 2){ }
				    else{
					if(shouldbanforceilinghack)
				    {
					ipban(source, cachedreasonceiling);
					Interface.CallHook("cmdBan", source.userID.ToString(), source.displayName, cachedreasonceiling);
					var plnew = GetPlayer(source.playerClient.userID.ToString());
					plnew.Remove("CeilingCheck");
					return;
				    }
				    if(shouldtpback)
				    {
					var plnewsafe = GetPlayer(source.playerClient.userID.ToString());
					TeleportToPos(source, 0.0f, 0.0f, 0.0f);
					plnewsafe.Remove("CeilingCheck");
					return;
				    }}
				}
				if(objectlocation2.ToString() == checkceiling)
			{
				
				if(shouldbanforceilinghack)
				{
					ipban(source, cachedreasonceiling);
					Debug.Log("called ceilinghack ban");
					Interface.CallHook("cmdBan", source.userID.ToString(), source.displayName, cachedreasonceiling);
					var plnew = GetPlayer(source.playerClient.userID.ToString());
					plnew.Remove("CeilingCheck");
					return;
				}
				if(shouldtpback)
				{
					var plnewsafe = GetPlayer(source.playerClient.userID.ToString());
					TeleportToPos(source, 0.0f, 0.0f, 0.0f);
					plnewsafe.Remove("CeilingCheck");
				}
			}
			}
			if(cachedhitInstance == null)
			{
				return;
			}
			var cachedCollider3 = cachedhitInstance.physicalColliderReferenceOnly;
			var cachedstructure = cachedCollider3.GetComponent<StructureComponent>();
			var objectlocation = cachedstructure.gameObject.transform.position;
			if(ply == q)
			{
				return;
			}
			var newy = Convert.ToSingle(pl["y"]);
			var ownerID = cachedstructure._master.ownerID;
			var objectownerid = pl["OwnerId"].ToString();
			var tot = (ply - newy);
			var totmath = (Math.Abs(tot));
			var totconversion = (tot * -1f);
			if(totmath > 5 && totmath < 2 && objectownerid == ownerID.ToString());
			{
				if(totmath < 2){ }
				else
				{
				if(shouldbanforceilinghack)
				{
					ipban(source, cachedreasonceiling);
					Interface.CallHook("cmdBan", source.userID.ToString(), source.displayName, cachedreasonceiling);
					var plnew = GetPlayer(source.playerClient.userID.ToString());
					plnew.Remove("CeilingCheck");
					return;
				}
				if(shouldtpback)
				{
					var plnewsafe = GetPlayer(source.playerClient.userID.ToString());
					TeleportToPos(source, 0.0f, 0.0f, 0.0f);
					plnewsafe.Remove("CeilingCheck");
					return;
				}}
			}
			var ceiling = cachedstructure.gameObject.transform.position.y;
			if(objectlocation.ToString() == checkceiling)
			{
				if(shouldbanforceilinghack)
				{
					ipban(source, cachedreasonceiling);
					Debug.Log("called flyhack ban");
					Interface.CallHook("cmdBan", source.userID.ToString(), source.displayName, cachedreasonceiling);
					var plnew = GetPlayer(source.playerClient.userID.ToString());
					plnew.Remove("CeilingCheck");
					return;
				}
				if(shouldtpback)
				{
					var plnewsafe = GetPlayer(source.playerClient.userID.ToString());
					TeleportToPos(source, 0.0f, 0.0f, 0.0f);
					plnewsafe.Remove("CeilingCheck");
				}
			}
		}
		void thrdtptimer(NetUser source)
		{
			if (source == null || source.playerClient == null) return;
			if (source == null || source.playerClient.controllable == null) return;
			if(shouldanticeilinghack)
			{
				var pl = GetPlayer(source.playerClient.userID.ToString());
				if(!pl.ContainsKey("CeilingCheck"))
				{
					return;
				}
				newfunctionphaseceiling(source);
				return;
				timer.Once((float)timertocheckceiling, () => newfunctionphaseceiling(source));
			}
		}
		void newplayerFlyhackcheck(NetUser source, float y)
		{
			return;
			if (source == null || source.playerClient == null) return;
			if (source == null || source.playerClient.controllable == null) return;
			var ply =  source.playerClient.lastKnownPosition.y;
			var fixedply = (y + 1f);
			{
				if(ply > fixedply)
				{
					var ping = source.networkPlayer.averagePing;
					if(ping >= pingexcepton)
						return;
					if (source == null || source.playerClient == null) return;
					ipban(source, cachedreasonflyhack);
					Debug.Log("called flyhack ban");
					Interface.CallHook("cmdBan", source.userID.ToString(), source.displayName, cachedreasonflyhack);
					return;
				}
			}
		}
		void Flyhackcheck(NetUser source)
		{
			if (source == null || source.playerClient == null) return;
			if (source == null || source.playerClient.controllable == null) return;
			var ply =  source.playerClient.lastKnownPosition.y;
			timer.Once((float)0.69 , () => newplayerFlyhackcheck(source, ply));
		}
		void undofixplayerhp(NetUser source)
		{
			if (source == null || source.playerClient == null) return;
			if (source == null || source.playerClient.controllable == null) return;
			FallDamage falldamage = source.playerClient.rootControllable.GetComponent<FallDamage>();
			var testc = source.playerClient.rootControllable.GetComponent<HumanBodyTakeDamage>();
			if(testc == null)
			{
			}
			testc.Bandage( 1000.0f );
			falldamage.ClearInjury();
			source.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(false);
			thrdtptimer(source);
			
		}
		void checksourcefalldamagethrd(NetUser source, float x, float y, float z )
		{
			if (source == null || source.playerClient == null)
				return;
			if (source == null || source.playerClient == null) return;
			if (source == null || source.playerClient.controllable == null) return;
			var y3 = source.playerClient.lastKnownPosition.y;
			var location = source.playerClient.lastKnownPosition.ToString();
			var total = (60000 - y3);
			if(total < 30)
			{
				var ping = source.networkPlayer.averagePing;
				if(ping >= pingexcepton)
				{
					TeleportToPos(source, x, y, z);
					timer.Once((float)0.20 , () => TeleportToPos(source, x, y, z));
					timer.Once((float)0.69 , () => TeleportToPos(source, x, y, z));
					timer.Once((float)3.0 , () => undofixplayerhp(source));
					return;
				}
				if(shoulldcheckfalldamgaefourth)
				{
					var adjustment = 60000f;
					TeleportToPos(source, 0.0f, 0.0f + adjustment, 0.0f);
					timer.Once((float)1.69 , () => checksourcefalldamagefourth(source, x, y, z));
				return;}
		
				ipban(source, cachedreasonnofall);
				Interface.CallHook("cmdBan", source.userID.ToString(), source.displayName, cachedreasonnofall);
				Broadcast(string.Format(source.displayName + " " + isbannedd + " (nofalldamage)"));
				return;
			}
			Debug.Log("thrd check called");
			rust.SendChatMessage(source, systemname, "you have been cleared of flyhack");
			source.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(true);
			TeleportToPos(source, x, y, z);
			timer.Once((float)0.20 , () => TeleportToPos(source, x, y, z));
			timer.Once((float)0.69 , () => TeleportToPos(source, x, y, z));
			timer.Once((float)3.0 , () => undofixplayerhp(source));
		}
		void checksourcefalldamagefourth(NetUser source, float x, float y, float z )
		{
			if (source == null || source.playerClient == null)
				return;
			if (source == null || source.playerClient == null) return;
			if (source == null || source.playerClient.controllable == null) return;
			var y3 = source.playerClient.lastKnownPosition.y;
			var location = source.playerClient.lastKnownPosition.ToString();
			var total = (60000 - y3);
			if(total < 30)
			{
				var ping = source.networkPlayer.averagePing;
				if(ping >= pingexcepton)
				{
					TeleportToPos(source, x, y, z);
					timer.Once((float)0.20 , () => TeleportToPos(source, x, y, z));
					timer.Once((float)0.69 , () => TeleportToPos(source, x, y, z));
					timer.Once((float)3.0 , () => undofixplayerhp(source));
					return;
				}
				ipban(source, cachedreasonnofall);
				Interface.CallHook("cmdBan", source.userID.ToString(), source.displayName, cachedreasonnofall);
				Broadcast(string.Format(source.displayName + " " + isbannedd + " (nofalldamage)"));
				return;
			}
			rust.SendChatMessage(source, systemname, "you have been cleared of flyhack");
			source.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(true);
			TeleportToPos(source, x, y, z);
			timer.Once((float)0.20 , () => TeleportToPos(source, x, y, z));
			timer.Once((float)0.69 , () => TeleportToPos(source, x, y, z));
			timer.Once((float)3.0 , () => undofixplayerhp(source));
		}
		void checksourcefalldamagesecond(NetUser source, float x, float y, float z )
		{
			if (source == null || source.playerClient == null)
				return;
			if (source == null || source.playerClient.controllable == null)
			{
				return;
			}
			var y3 = source.playerClient.lastKnownPosition.y;
			var location = source.playerClient.lastKnownPosition.ToString();
			var total = (60000 - y3);
			if(total < 30)
			{
				
				var ping = source.networkPlayer.averagePing;
				if(ping >= pingexcepton)
				{
					TeleportToPos(source, x, y, z);
					timer.Once((float)0.20 , () => TeleportToPos(source, x, y, z));
					timer.Once((float)0.69 , () => TeleportToPos(source, x, y, z));
					timer.Once((float)3.0 , () => undofixplayerhp(source));
					return;
				}
				if(shouldcheckkthrdfalldamage)
				{
					var adjustment = 60000f;
					TeleportToPos(source, 0.0f, 0.0f + adjustment, 0.0f);
					timer.Once((float)1.69 , () => checksourcefalldamagethrd(source, x, y, z));
					return;
				}
				ipban(source, cachedreasonnofall);
				Interface.CallHook("cmdBan", source.userID.ToString(), source.displayName, cachedreasonnofall);
				Broadcast(string.Format(source.displayName + " " + isbannedd + " (nofalldamage)"));
				return;
			}
			rust.SendChatMessage(source, systemname, "you have been cleared of flyhack");
			source.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(true);
			TeleportToPos(source, x, y, z);
			timer.Once((float)0.20 , () => TeleportToPos(source, x, y, z));
			timer.Once((float)0.69 , () => TeleportToPos(source, x, y, z));
			timer.Once((float)3.0 , () => undofixplayerhp(source));
		}
		void finishcheck(NetUser netuser, float x, float y, float z)
		{
			if (netuser == null || netuser.playerClient == null) return;
			var ac = Getactivepl("Active(pl2)");
			if(ac.ContainsKey(netuser.playerClient.userID.ToString()))
			ac.Remove(netuser.playerClient.userID.ToString());
		}
		void checksourcefalldamage(NetUser source, float x, float y, float z )
		{
			if (source == null || source.playerClient == null)
				return;
			if (source == null || source.playerClient.controllable == null)
			{
				return;
			}
			var location = source.playerClient.lastKnownPosition.ToString();
			var y2 = source.playerClient.lastKnownPosition.y;
			
			var total = (60000 - y2);
			if(total < 30)
			{
				if(secondchecknofall)
				{
				   if (source == null || source.playerClient == null)
				    return;
					var adjustment = 60000f;
					TeleportToPos(source, 0.0f, 0.0f + adjustment, 0.0f);
					timer.Once((float)1.69 , () => checksourcefalldamagesecond(source, x, y, z));
					return;
					
				}
				var ping = source.networkPlayer.averagePing;
				if(ping >= pingexcepton)
				{
					if (source == null || source.playerClient == null)
				    return;
					TeleportToPos(source, x, y, z);
					timer.Once((float)0.20 , () => TeleportToPos(source, x, y, z));
					timer.Once((float)0.69 , () => TeleportToPos(source, x, y, z));
					timer.Once((float)3.0 , () => undofixplayerhp(source));
					
					return;
				}
				ipban(source, cachedreasonnofall);
				Interface.CallHook("cmdBan", source.userID.ToString(), source.displayName, cachedreasonnofall);
				Broadcast(string.Format(source.displayName + " " + isbannedd + " (nofalldamage)"));
				return;
			}
			rust.SendChatMessage(source, systemname, "you have been cleared of flyhack");
			source.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(true);
			TeleportToPos(source, x, y, z);
			timer.Once((float)0.20 , () => TeleportToPos(source, x, y, z));
			timer.Once((float)0.69 , () => TeleportToPos(source, x, y, z));
			timer.Once((float)1.35 , () => undofixplayerhp(source));
			
			
		}
		void checkfalldamage(NetUser source)
		{
			if (source == null || source.playerClient.controllable == null) return;
			var adjustment = 60000f;
			TeleportToPos(source, 0.0f, 0.0f + adjustment, 0.0f);
			var x = source.playerClient.lastKnownPosition.x;
			var y = source.playerClient.lastKnownPosition.y;
			var z = source.playerClient.lastKnownPosition.z;
			var y2 = source.playerClient.lastKnownPosition.y;
			var location = source.playerClient.lastKnownPosition;
			timer.Once((float)1.69 , () => checksourcefalldamage(source, x, y, z));
		}
		void checksystem(NetUser source)
		{
			if (source == null || source.playerClient.controllable == null) return;
			if(Antiflyhack)
			{
				timer.Once((float)Antiflyhacktimer , () => Flyhackcheck(source));
			}
			if(shouldanticeilinghack)
			{
				if(autocorrect)
				{
					if(timertocheckceiling < Antiflyhacktimer)
					{
						var correctceiling = (timertocheckceiling + 0.50f);
						timer.Once((float)correctceiling , () => thrdtptimer(source));
						
					}
				}
				else
				{
					timer.Once((float)timertocheckceiling , () => thrdtptimer(source));
				}
				
			}
			if(Antinofall)
			{
				if(autocorrect)
				{
					var correctceiling = (timertocheckceiling + 1.2f);
					var GG = (correctceiling + 2.0f);
					timer.Once((float)GG , () => checkfalldamage(source));
					return;
				}
				timer.Once((float)nofalltimer , () => checkfalldamage(source));
			}
			
		}
		
		void secondcheck(NetUser source)
		{
			if (source == null || source.playerClient.controllable == null) return;
			var pl = GetPlayer(source.playerClient.userID.ToString());
			timer.Once((float)timertochecksecond, () => TeleportToPos(source, Convert.ToSingle(pl["x"]), Convert.ToSingle(pl["y"]), Convert.ToSingle(pl["z"])));
			if(autocorrect)
			{
				var correct = (timertochecksecond + 3.0f);
				timer.Once((float)correct , () => checksystem(source));
				return;
				
			}
			timer.Once((float)beguincheck , () => checksystem(source));
		}
		bool hassleeper(NetUser netuser)
		{
			if (netuser == null || netuser.playerClient == null) return false;
			if(sleepers.on)
			{
				foreach(DeployableObject comp2 in UnityEngine.Resources.FindObjectsOfTypeAll<DeployableObject>())
				{
				var cachedStructure2 = comp2.GetComponent<DeployableObject>();
				var cachedMasterbannedbase2 = cachedStructure2._carrier;
				if(cachedMasterbannedbase2 != null)
					continue;
				int count2 = 0;
				var cachedmaster = cachedStructure2.ownerID;
				if(netuser.playerClient.userID != cachedmaster)
					continue;
				if(cachedStructure2.gameObject.name.ToString() == "MaleSleeper(Clone)")
					return true;
				return false;
			}}
			return false;
		}
		bool nullcharactercheck(NetUser netuser)
		{
			var getexception = Getactivepl("addeception");
			if(getexception.ContainsKey(netuser.playerClient.userID.ToString()))
			{
				getexception.Remove(netuser.playerClient.userID.ToString());
				return true;
			}
			return false;
		}
		void OnPlayerSpawn(PlayerClient player, bool useCamp, RustProto.Avatar avatar)
		{
			NetUser netuser = player.netUser;
			if (netuser == null || netuser.playerClient == null) return;
			if(monoantinofall)
			{
				return;
				PlayerHandler phandler = player.GetComponent<PlayerHandler>();
				if (phandler == null) { phandler = player.gameObject.AddComponent<PlayerHandler>(); }
				timer.Once(0.1f, () => phandler.StartCheck());
			}
			var character = player.controllable;
			if(nullcharactercheck(netuser))
				return;
			if(shouldpreventpsleepglitch)
			if(sleepers.on)
			{
				if(!hassleeper(netuser)){
					return;
					}
			}
			var ply = player.lastKnownPosition.y.ToString();
			var pl = GetPlayer(player.userID.ToString());
			var ac = Getactivepl("Active(pl)");
			var id = player.userID.ToString();
			if(!ac.ContainsKey(id)) return;
			if(!pl.ContainsKey("x")) return;
			var name = netuser.displayName;
			var x = pl["x"].ToString();
		    var y = pl["y"].ToString();
			var z = pl["z"].ToString();
			ac.Remove(player.userID.ToString());
			var timeradd = (nofalltimer + 0.99);
			if(Antinofall){
			timer.Once((float)nofalltimer , () => checkfalldamage(netuser));
			return;
			}
		    if(shouldanticeilinghack){
			timer.Once((float)2.5 , () => thrdtptimer(netuser));
			timer.Once((float)0.65, () => TeleportToPos(netuser, Convert.ToSingle(pl["x"]), Convert.ToSingle(pl["y"]), Convert.ToSingle(pl["z"])));
			timer.Once((float)1.15, () => TeleportToPos(netuser, Convert.ToSingle(pl["x"]), Convert.ToSingle(pl["y"]), Convert.ToSingle(pl["z"])));
			}
			return;
			if(Antinofall)
			{
				timer.Once((float)timertocheck , () => checkfalldamage(netuser));
				return;
			}
			timer.Once((float)timeradd, () => TeleportToPos(netuser, Convert.ToSingle(pl["x"]), Convert.ToSingle(pl["y"]), Convert.ToSingle(pl["z"])));
			secondcheck(netuser);
		}
    }
}