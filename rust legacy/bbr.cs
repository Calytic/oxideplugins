// Reference: Newtonsoft.Json
// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("bbr - Banned Base Recker", "copper", "1.0.0")]
    class Bbr : RustLegacyPlugin
	{
		Vector3 cachedPos;
        RaycastHit cachedRaycastt;
        Vector3 vectorup = new Vector3(0f, 1f, 0f);
        int terrainLayer;
		string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
		RustServerManagement management;

		private Core.Configuration.DynamicConfigFile Data;
		private Core.Configuration.DynamicConfigFile Info;
		
		void LoadData() { Data = Interface.GetMod().DataFileSystem.GetDatafile("Bbr(pl)"); Info = Interface.GetMod().DataFileSystem.GetDatafile("Bbr(House.pl)"); }
		void SaveData() { Interface.GetMod().DataFileSystem.SaveDatafile("Bbr(pl)"); Interface.GetMod().DataFileSystem.SaveDatafile("Bbr(House.pl)"); }
		void Unload() { SaveData(); }
		private static FieldInfo structureComponents;

        static Hash<string, string> deployableCloneToGood = new Hash<string, string>();
        static Hash<string,string> structureCloneToGood = new Hash<string, string>();
        private Dictionary<string, ItemDataBlock> displaynameToDataBlock = new Dictionary<string, ItemDataBlock>();
        float cachedSeconds;
        string cachedType;
        StructureComponent cachedStructurebase;
        DeployableObject cachedDeployablebase;
        StructureMaster cachedMasterbase;
        List<object> cachedListObjectbase;
		[PluginReference]
        Plugin PlayerDatabase;
		
		private static FieldInfo accessUsers;

        RaycastHit cachedRaycast;
        Character cachedCharacter;
        bool cachedBoolean;
        Collider cachedCollider;
        StructureComponent cachedStructure;
        DeployableObject cachedDeployable;
        StructureMaster cachedMaster;
		 List<object> cachedListObject;
        Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;


		void Loaded()
        {			
			LoadData();
			LoadDefaultMessages();
			if (!permission.PermissionExists("Rbb.checkbase")) permission.RegisterPermission("Rbb.checkbase", this);
			if(shouldremoveallbannedbasesusingtimer)
			timer.Once(Timertoremovebannedbase, () => RemoveAllbannedbasestimerfunction());
        }
		public static bool removeall = false;
		public static bool shouldsenddefaultmsg = true;
		public static bool shouldsendcustommsg = false;
		public static bool shouldsendtelemsgtouser = true;
		public static bool shouldremoveallbannedbasesusingtimer = false;
		public static bool shouldsendtotalremovedbannedbasestochat = true;
		public float Timertoremovebannedbase = 86400.0f;

		void LoadDefaultConfig() { }
		
		private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
		void OnServerInitialized()
        {
            management = RustServerManagement.Get();
        }
		
		 void Init()
        {
			structureComponents = typeof(StructureMaster).GetField("_structureComponents", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
			CheckCfg<bool>("Bool: should send default message", ref shouldsenddefaultmsg);
			CheckCfg<bool>("Bool: should send auto matic removed objects to chat", ref shouldsendtotalremovedbannedbasestochat);
			CheckCfg<bool>("Bool: should send custom msg", ref shouldsendcustommsg);
			CheckCfg<bool>("Bool: send teleportation message to user", ref shouldsendtelemsgtouser);
			CheckCfg<bool>("Bool: Timer to remove all banned bases?", ref shouldremoveallbannedbasesusingtimer);
			CheckCfg<bool>("Bool: should remove banned bases if base is banned and cb command typed?", ref removeall);
			CheckCfg<float>("Bool: Timer to remove all banned owner bases(default is one day)", ref Timertoremovebannedbase);
			SaveConfig();
        }
		 void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"tpmsg", "for security reasons you have been teleported to  {0} {1} {2}"},
				{"custom message", "[color red]This command is blocked."},
				{"no one except the owner", "no one except the owner"},
				{"access to door", "[color red]Players with access to this door."},
				{"no bases in log", "there was no bases found in the log with that number plz do /cbl to see available tp's/numbers."},
				{"not allowed to use command", "[color red]You are not allowed to use this command."},
				{"cant tell what you are looking at", "Cant prod what you are looking at."},
				{"systemname", "DTG(derpteamgames)"},
				{"isinbanllist", "This player is banned."},
				{"isnotinbanllist", "This player is banned."},
				{"cantprod what you are looking at phase 2", "Can't prod what you are looking at: {0}"},
				{"objects destroyed", "Banned user objects destroyed."},
				{"tpbb wrong syntax", "wrong syntax: /tpbb 'playername < must be precise"},
				{"starting form index 1", "Banned user bases starting from index 1."},
            };
            lang.RegisterMessages(messages, this);
        }
		bool hasAccess(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
            if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "Rbb.checkbase")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }

        Dictionary<string, object> GetPlayerdata(string userid)
        {
            if (Data[userid] == null)
                Data[userid] = new Dictionary<string, object>();
            return Data[userid] as Dictionary<string, object>;
        }

        Dictionary<string, object> GetPlayerinfo(string userid)
        {
            if (Info[userid] == null)
                Info[userid] = new Dictionary<string, object>();
            return Info[userid] as Dictionary<string, object>;
        }
		void OnPlayerConnected(NetUser netuser)
		{
			var pl = GetPlayerdata(netuser.playerClient.userID.ToString());
			if(!pl.ContainsKey("StaticNickname"))
			{
				pl.Add("StaticNickname", netuser.displayName);
			}
			if(pl.ContainsKey("LastSeenNickname"))
				pl.Remove("LastSeenNickname");
			pl.Add("LastSeenNickname", netuser.displayName);
			return;
		}
		void checkbaselocation(NetUser netuser, int tostartfrom)
        {
			int count = 0;
			var newcount = (tostartfrom + 20);
			SendReply(netuser, GetMessage("starting form index 1"));
            foreach(StructureComponent comp in UnityEngine.Resources.FindObjectsOfTypeAll<StructureComponent>())
            {
				var cachedStructure1 = comp.GetComponent<StructureComponent>();
				var cachedMasterbannedbase = cachedStructure1._master;
				if(cachedMasterbannedbase == null)
					continue;
				var cachedmaster = cachedMasterbannedbase.ownerID;
				if(BanList.Contains(Convert.ToUInt64(cachedmaster)))
				{
					count++;
					if(count < tostartfrom)
						continue;
					int count3 = count;
					if(count >= newcount)
						break;
					if(count >= newcount)
						return;
					var locationx = cachedStructure1.transform.position.x.ToString();
					var locationy = cachedStructure1.transform.position.y.ToString();
					var locationz = cachedStructure1.transform.position.z.ToString();
					var baseinfo = GetPlayerinfo(count.ToString());
					var owner2 = GetPlayerdata(cachedMasterbannedbase.ownerID.ToString());
					if(baseinfo.ContainsKey("locationx"))
					{
						baseinfo.Remove("locationx");
						baseinfo.Remove("locationy");
						baseinfo.Remove("locationz");
					}
					baseinfo.Add("locationx", locationx);
					baseinfo.Add("locationy", locationy);
					baseinfo.Add("locationz", locationz);
					if(count > newcount)
						Debug.Log("count is greater????");
					if(!owner2.ContainsKey("LastSeenNickname"))
					{
						SendReply(netuser, count.ToString() + " (" + locationx + " " + locationy + " " + locationz + ")");
						continue;
					}
					else
					{
						var name = owner2["LastSeenNickname"].ToString();
						SendReply(netuser, count.ToString() + " (" + locationx + " " + locationy + " " + locationz + ") " + name);
					}
				}
				
            }
		}
		void Dorepeat()
		{
			timer.Once(Timertoremovebannedbase, () => RemoveAllbannedbasestimerfunction());
		}
		void RemoveAllbannedbasestimerfunction()
        {
			int count = 0;
			int count2 = 0;
            foreach(StructureComponent comp in UnityEngine.Resources.FindObjectsOfTypeAll<StructureComponent>())
            {
				var cachedStructure1 = comp.GetComponent<StructureComponent>();
				var cachedMasterbannedbase = cachedStructure1._master;
				if(cachedMasterbannedbase == null)
					continue;
				var cachedmaster = cachedMasterbannedbase.ownerID;
				if(BanList.Contains(Convert.ToUInt64(cachedmaster)))
				{
					TakeDamage.KillSelf(comp.GetComponent<IDMain>());
					count++;
				}
            }
			foreach(DeployableObject comp2 in UnityEngine.Resources.FindObjectsOfTypeAll<DeployableObject>())
            {
				var cachedStructure2 = comp2.GetComponent<DeployableObject>();
				var cachedMasterbannedbase2 = cachedStructure2._carrier;
				if(cachedMasterbannedbase2 != null)
					continue;
				var cachedmaster = cachedStructure2.ownerID;
				if(BanList.Contains(Convert.ToUInt64(cachedmaster)))
				{
					TakeDamage.KillSelf(comp2.GetComponent<IDMain>());
					count2++;
				}
            }
			if(!shouldsendtotalremovedbannedbasestochat)
				return;
			var total = (count + count2);
			foreach (PlayerClient player in PlayerClient.All)
            {
				var message = (total.ToString() + " " + GetMessage("objects destroyed"));
                ConsoleNetworker.SendClientCommand(player.netPlayer, "chat.add AntiCheat \"" + message + "\"");
            }
			Dorepeat();
        }
		void RemoveAllbannedbases(NetUser netuser)
        {
			int count = 0;
			int count2 = 0;
            foreach(StructureComponent comp in UnityEngine.Resources.FindObjectsOfTypeAll<StructureComponent>())
            {
				var cachedStructure1 = comp.GetComponent<StructureComponent>();
				var cachedMasterbannedbase = cachedStructure1._master;
				if(cachedMasterbannedbase == null)
					continue;
				var cachedmaster = cachedMasterbannedbase.ownerID;
				if(BanList.Contains(Convert.ToUInt64(cachedmaster)))
				{
					TakeDamage.KillSelf(comp.GetComponent<IDMain>());
					count++;
				}
            }
			foreach(DeployableObject comp2 in UnityEngine.Resources.FindObjectsOfTypeAll<DeployableObject>())
            {
				var cachedStructure2 = comp2.GetComponent<DeployableObject>();
				var cachedMasterbannedbase2 = cachedStructure2._carrier;
				if(cachedMasterbannedbase2 != null)
					continue;
				var cachedmaster = cachedStructure2.ownerID;
				if(BanList.Contains(Convert.ToUInt64(cachedmaster)))
				{
					TakeDamage.KillSelf(comp2.GetComponent<IDMain>());
					count2++;
				}
            }
			var total = (count + count2);
			SendReply(netuser, total.ToString() + " " + GetMessage("objects destroyed"));
        }
		void TeleportToPos(NetUser source, float x, float y, float z)
        {
            if (Physics.Raycast(new Vector3(x, -1000f, z), vectorup, out cachedRaycastt, Mathf.Infinity, terrainLayer))
            {
                if (cachedRaycast.point.y > y) y = cachedRaycast.point.y;
            }
			management.TeleportPlayerToWorld(source.playerClient.netPlayer, new Vector3(x, y +6.000f, z));
			if(shouldsendtelemsgtouser)
			{
				if(shouldsendcustommsg)
				{
					rust.SendChatMessage(source, GetMessage("systemname"), GetMessage("custom message"));
				}
				if(shouldsenddefaultmsg)
				{
					SendReply(source, string.Format(GetMessage("tpmsg"), x.ToString(), y.ToString(), z.ToString()));
				}
			}
        }
		[ChatCommand("rbb")]
		void cmdchatrbannedbasese(NetUser netuser, string command, string[] args)
		{
			if (!hasAccess(netuser, "Rbb.checkbase")) { SendReply(netuser, GetMessage("not allowed to use command")); return; }
			RemoveAllbannedbases(netuser);
		}
		[ChatCommand("tpbb")]
		void cmdchattpbase(NetUser netuser, string command, string[] args)
		{
			if (!hasAccess(netuser, "Rbb.checkbase")) { SendReply(netuser, GetMessage("not allowed to use command")); return; }
			if (args.Length == 0)
			{
				rust.SendChatMessage(netuser, GetMessage("tpbb wrong syntax"));
				return;
			}
			int b = 1;
			if (args.Length > 0) int.TryParse(args[0], out b);
			var baseinfo = GetPlayerinfo(b.ToString());
			if(!baseinfo.ContainsKey("locationx"))
			{
				rust.SendChatMessage(netuser, GetMessage("no bases in log"));
				return;
			}
			TeleportToPos(netuser, Convert.ToSingle(baseinfo["locationx"]), Convert.ToSingle(baseinfo["locationy"]), Convert.ToSingle(baseinfo["locationz"]));
			
		}
		[ChatCommand("cbl")]
		void cmdchatgetbaselocation(NetUser netuser, string command, string[] args)
		{
			if (!hasAccess(netuser, "Rbb.checkbase")) { SendReply(netuser, GetMessage("not allowed to use command")); return; }
			int bl = 1;
			if (args.Length > 0) int.TryParse(args[0], out bl);
			checkbaselocation(netuser, bl);
		}
		[ChatCommand("cb")]
        void cmdchatbancheck(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "Rbb.checkbase")) { SendReply(netuser, GetMessage("not allowed to use command")); return; }
            cachedCharacter = netuser.playerClient.rootControllable.idMain.GetComponent<Character>();
            if (!MeshBatchPhysics.Raycast(cachedCharacter.eyesRay, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { SendReply(netuser, "Are you looking at the sky?"); return; }
            //Debug.Log(cachedRaycast.collider.ToString());
            //Debug.Log(LayerMask.LayerToName(cachedRaycast.collider.gameObject.layer));
            //Debug.Log(cachedRaycast.collider.transform.position.y.ToString());
            //Debug.Log(cachedCharacter.origin.y.ToString());

            //GameObject.Destroy(cachedRaycast.collider.GetComponent<UnityEngine.MeshCollider>());
            /*
            
            var components = cachedRaycast.collider.GetComponents<UnityEngine.Component>();
            foreach (var comp in components)
            {
                Debug.Log(comp.ToString());
            }
            Debug.Log("============= COMPONENTS IN PARENT =============");
            components = cachedRaycast.collider.GetComponentsInParent<UnityEngine.Component>();
            foreach (var comp in components)
            {
                Debug.Log(comp.ToString());
            }
            Debug.Log("============= COMPONENTS IN CHILDREN =============");
            components = cachedRaycast.collider.GetComponentsInChildren<UnityEngine.Component>();
            foreach (var comp in components)
            {
                Debug.Log(comp.ToString());
            }*/
            if (cachedhitInstance != null)  
            {
                cachedCollider = cachedhitInstance.physicalColliderReferenceOnly;
                if (cachedCollider == null) { SendReply(netuser,  GetMessage("cant tell what you are looking at")); return; }
                cachedStructure = cachedCollider.GetComponent<StructureComponent>();
                if (cachedStructure != null && cachedStructure._master != null)
                {
                    cachedMaster = cachedStructure._master;
					var owner = GetPlayerdata(cachedMaster.ownerID.ToString());
					var gg = owner["StaticNickname"].ToString();
					var q = owner["LastSeenNickname"].ToString();
                    var name = (q);
					rust.SendChatMessage(netuser, GetMessage("systemname"), "StaticNickName " + gg);
                    rust.SendChatMessage(netuser, GetMessage("systemname"), string.Format("{0} - {1} - {2}", cachedStructure.gameObject.name, cachedMaster.ownerID.ToString(), name == null ? "Unknown" : name.ToString()));
					if(BanList.Contains(Convert.ToUInt64(cachedMaster.ownerID.ToString())))
					{
						rust.SendChatMessage(netuser, GetMessage("systemname"),  GetMessage("isinbanllist"));
						if(removeall)
						{
							foreach(StructureComponent comp in (HashSet<StructureComponent>)structureComponents.GetValue(cachedMaster))
                            {
								TakeDamage.KillSelf(comp.GetComponent<IDMain>());
                            }
						}
						return;
					}
					
					if(!BanList.Contains(Convert.ToUInt64(cachedMaster.ownerID.ToString())))
					{
						rust.SendChatMessage(netuser, GetMessage("systemname"),  GetMessage("isnotinbanllist"));
						return;
					}
                }
            }
		    else 
            {

                cachedDeployable = cachedRaycast.collider.GetComponent<DeployableObject>();
                if (cachedDeployable != null)
                {
                    var owner2 = GetPlayerdata(cachedDeployable.ownerID.ToString());
					var gg2 = owner2["StaticNickname"].ToString();
					var q2 = owner2["LastSeenNickname"].ToString();
					var name = (q2);
					rust.SendChatMessage(netuser, GetMessage("systemname"), "StaticNickName " + gg2);
                    rust.SendChatMessage(netuser, GetMessage("systemname"), string.Format("{0} - {1} - {2}", cachedDeployable.gameObject.name, cachedDeployable.ownerID.ToString(), name == null ? "Unknown" : name.ToString()));
                    if(cachedDeployable.GetComponent<PasswordLockableObject>())
                    {
                        SendReply(netuser, GetMessage("access to door"));
                        int count = 0;
                        foreach(ulong userid in (HashSet<ulong>)accessUsers.GetValue(cachedDeployable.GetComponent<PasswordLockableObject>()))
                        {
                            count++;
                            SendReply(netuser, string.Format("{0} - {1}", userid.ToString(), name == null ? "Unknown" : name.ToString()));
                        }
                        if(count == 0) SendReply(netuser, GetMessage("no one except the owner"));
                    }
					if(BanList.Contains(Convert.ToUInt64(cachedDeployable.ownerID.ToString())))
					{
						var cachedcarrier = cachedDeployable._carrier;
						rust.SendChatMessage(netuser, GetMessage("systemname"),  GetMessage("isinbanllist"));
						if(removeall)
						{
							foreach(DeployableObject comp in (HashSet<DeployableObject>)structureComponents.GetValue(cachedcarrier))
							{
								TakeDamage.KillSelf(comp.GetComponent<IDMain>());
							}
						}
					}
					rust.SendChatMessage(netuser, GetMessage("systemname"),  GetMessage("isnotinbanllist"));
                    return;
                }
            }
            SendReply(netuser, string.Format(GetMessage("cantprod what you are looking at phase 2")),cachedRaycast.collider.gameObject.name);
        }

	}
}