using System;

using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
     	[Info("VisualCupboard", "Colon Blow", "1.0.6", ResourceId = 2030)]
    	class VisualCupboard : RustPlugin
     	{
		void OnServerInitialized() { serverInitialized = true; }

        	void Loaded()
        	{
			LoadVariables();
			serverInitialized = true;
			lang.RegisterMessages(messages, this);
			permission.RegisterPermission("visualcupboard.allowed", this);
			permission.RegisterPermission("visualcupboard.admin", this);
		}

        	void LoadDefaultConfig()
        	{
            		Puts("Creating a new config file");
            		Config.Clear();
            		LoadVariables();
        	}

       	 	Dictionary<string, string> messages = new Dictionary<string, string>()
        	{
			{"notallowed", "You are not allowed to access that command." }
        	};

	////////////////////////////////////////////////////////////////////////////////////////////
	//	Configuration File
	////////////////////////////////////////////////////////////////////////////////////////////

		bool Changed;

		private static float UseCupboardRadius = 25f;

		bool ShowOnlyOwnCupboards = false;
		bool ShowRadiusWhenDeploying = false;

		float DurationToShowRadius = 60f;
		float ShowCupboardsWithinRangeOf = 50f;

		bool AdminShowOwnerID = false;

		private static bool serverInitialized = false;

        	private void LoadConfigVariables()
        	{
        		CheckCfgFloat("My Cupboard Radius is (25 is default)", ref UseCupboardRadius);
			
        		CheckCfg("Show Visuals On OWN Cupboards Only", ref ShowOnlyOwnCupboards);
			CheckCfg("Show Visuals When Placing Cupboard", ref ShowRadiusWhenDeploying);

        		CheckCfgFloat("Show Visuals On Cupboards Withing Range Of", ref ShowCupboardsWithinRangeOf);
			CheckCfgFloat("Show Visuals For This Long", ref DurationToShowRadius);

			CheckCfg("Admin : Show Cupboard Owners ID", ref AdminShowOwnerID);
        	}

        	private void LoadVariables()
        	{
            		LoadConfigVariables();
            		SaveConfig();
        	}

        	private void CheckCfg<T>(string Key, ref T var)
        	{
            	if (Config[Key] is T)
              		var = (T)Config[Key];
           	else
                	Config[Key] = var;
        	}

        	private void CheckCfgFloat(string Key, ref float var)
        	{

            	if (Config[Key] != null)
                	var = Convert.ToSingle(Config[Key]);
            	else
                	Config[Key] = var;
        	}

        	object GetConfig(string menu, string datavalue, object defaultValue)
        	{
            		var data = Config[menu] as Dictionary<string, object>;
            		if (data == null)
            		{
                		data = new Dictionary<string, object>();
                		Config[menu] = data;
                		Changed = true;
           		}

            		object value;
            		if (!data.TryGetValue(datavalue, out value))
            		{
                		value = defaultValue;
                		data[datavalue] = value;
                		Changed = true;
            		}
            		return value;
       		}

	////////////////////////////////////////////////////////////////////////////////////////////
	//	Sphere entity used for Visual Cupboard Radius
	////////////////////////////////////////////////////////////////////////////////////////////

	class ToolCupboardSphere : MonoBehaviour
		{

			
			BaseEntity sphere;
			BaseEntity entity;

			Vector3 pos = new Vector3(0, 0, 0);
			Quaternion rot = new Quaternion();
			string strPrefab = "assets/prefabs/visualization/sphere.prefab";

			void Awake()
			{
				entity = GetComponent<BaseEntity>();
				sphere = GameManager.server.CreateEntity(strPrefab, pos, rot, true);
				SphereEntity ball = sphere.GetComponent<SphereEntity>();
				ball.currentRadius = 1f;
				ball.lerpRadius = 2.0f*UseCupboardRadius;
				ball.lerpSpeed = 100f;

				sphere.SetParent(entity, "");
				sphere?.Spawn();
			}

           	 	void OnDestroy()
            		{
				if (sphere == null) return;
               			sphere.Kill(BaseNetworkable.DestroyMode.None);
            		}

		}

	////////////////////////////////////////////////////////////////////////////////////////////
	//	When player places a cupbaord, a Visual cupboard radius will pop up
	////////////////////////////////////////////////////////////////////////////////////////////

		void OnEntitySpawned(BaseEntity entity, UnityEngine.GameObject gameObject)
		{
			if (!serverInitialized) return;
			if (!ShowRadiusWhenDeploying) return;
			if (entity == null) return;

			if (ShowRadiusWhenDeploying) 
			{
				if (entity.name.Contains("cupboard.tool"))
				{
					var player = BasePlayer.FindByID(entity.OwnerID);
					if (player != null)
					{
						if (!isAllowed(player, "visualcupboard.allowed")) return;
            					var sphereobj = entity.gameObject.AddComponent<ToolCupboardSphere>();
						GameManager.Destroy(sphereobj, DurationToShowRadius);	
						return;
					}
				}
			}
			else return;
		}

	////////////////////////////////////////////////////////////////////////////////////////////
	//	When player runs chat command, shows Cupboard Radius of nearby Tool Cupboards
	////////////////////////////////////////////////////////////////////////////////////////////
		
		[ChatCommand("showsphere")]
        	void cmdChatShowSphere(BasePlayer player, string command)
		{	
			if (isAllowed(player, "visualcupboard.allowed"))
			{
				bool ShowAdmin = false;
				if (isAllowed(player, "visualcupboard.admin")) { ShowAdmin = true; }

				List<BaseCombatEntity> cblist = new List<BaseCombatEntity>();
				Vis.Entities<BaseCombatEntity>(player.transform.position, ShowCupboardsWithinRangeOf, cblist);
			
				foreach (BaseCombatEntity bp in cblist)
				{
					if (bp is BuildingPrivlidge)
					{
						if (bp.GetComponent<ToolCupboardSphere>() == null)
						{
							Vector3 pos = bp.transform.position;
							
							if (!ShowAdmin)
							{
								if ((ShowOnlyOwnCupboards) && (player.userID != bp.OwnerID)) return;
								var sphereobj = bp.gameObject.AddComponent<ToolCupboardSphere>();
								GameManager.Destroy(sphereobj, DurationToShowRadius);
							}
							if (ShowAdmin)
							{
								var sphereobj = bp.gameObject.AddComponent<ToolCupboardSphere>();
								GameManager.Destroy(sphereobj, DurationToShowRadius);
								if (AdminShowOwnerID)
								{
									string tcradius = "Radius: " + UseCupboardRadius;							
									player.SendConsoleCommand("ddraw.text", 10, UnityEngine.Color.red, pos+Vector3.up, FindPlayerName(bp.OwnerID));
									PrintWarning("Tool Cupboard Owner " + bp.OwnerID + " : " + FindPlayerName(bp.OwnerID));
								}
							}	
						}
					}
				}
			}
			if (!isAllowed(player, "visualcupboard.allowed"))
			{
				SendReply(player, lang.GetMessage("notallowed", this));
			 	return;	
			}
			else return;
		}

		[ChatCommand("killsphere")]
        	void cmdChatDestroySphere(BasePlayer player, string command)
		{
			if (isAllowed(player, "visualcupboard.admin"))
			{
				DestroyAll<ToolCupboardSphere>();
				return;
			}
			else if (!isAllowed(player, "visualcupboard.admin"))
			{
				SendReply(player, lang.GetMessage("notallowed", this));
			 	return;	
			}
		}

	////////////////////////////////////////////////////////////////////////////////////////////

        	private string FindPlayerName(ulong userId)
        	{
           	 BasePlayer player = BasePlayer.FindByID(userId);
           	 if (player)
                return player.displayName;

            	player = BasePlayer.FindSleeping(userId);
           	 if (player)
                return player.displayName;

           	 var iplayer = covalence.Players.GetPlayer(userId.ToString());
            	if (iplayer != null)
                return iplayer.Name;

            	return "Unknown Entity Owner";
       		}

        	void Unload()
        	{
            		DestroyAll<ToolCupboardSphere>();
        	}
		
        	static void DestroyAll<T>()
        	{
            		var objects = GameObject.FindObjectsOfType(typeof(T));
            		if (objects != null)
                		foreach (var gameObj in objects)
                    		GameObject.Destroy(gameObj);
       		 }

		bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
	}
}