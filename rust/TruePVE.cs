
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("TruePVE", "ignignokt84", "0.4.0")]
	class TruePVE : RustPlugin
	{
		private TruePVEData data = new TruePVEData();
		private Hurtable global;
		
		static FieldInfo serverinput;
		
		// usage information string with formatting
		public string usageString;
		// valid commands
		private enum Command { usage, set, get, list, version, def };
		// valid options
		public enum Option { allowSuicide, authDamage, corpseLooting, handleDamage, handleLooting, heliDamage, heliDamageLocked, immortalLocks, sleeperAdminDamage, sleeperLooting };
		// default values array
		private bool[] def = { true, false, false, true, true, true, false, true, false, false };
		// layer mask for finding authorization
        private readonly int triggerMask = LayerMask.GetMask("Trigger");
        
		// load default messages to Lang
		void LoadDefaultMessages()
		{
			var messages = new Dictionary<string, string>
			{
				{"ConsoleCommand", "tpve"},
				
				{"Header_Usage", "---- TruePVE usage ----"},
				{"Cmd_Usage_set", "Set value of specified option"},
				{"Cmd_Usage_get", "Get value of specified option"},
				{"Cmd_Usage_list", "Lists available options"},
				{"Cmd_Usage_def", "Loads default configuration and/or mapping data"},
				{"Cmd_Usage_version", "Prints version information"},
				{"Cmd_Usage_prod", "Show the prefab name and type of the entity being looked at"},
				{"Cmd_Usage_OptionString", "[option]"},
				{"Cmd_Usage_ValueString", "[value]"},
				{"Cmd_Usage_DefaultsOptions", "[all|config|data]"},
				
				{"Warning_PveMode", "Server is set to PVE mode!  TruePVE is designed for PVP mode, and may cause unexpected behavior in PVE mode."},
				
				{"Error_InvalidParameter", "Invalid parameter: {0}"},
				{"Error_InvalidParamForCmd", "Invalid parameters for command \"{0}\""},
				{"Error_NoPermission", "Cannot execute command: No permission"},
				{"Error_NoSuicide", "You are not allowed to commit suicide"},
				{"Error_NoLootCorpse", "You are not allowed to loot another player's corpse"},
				{"Error_NoLootSleeper", "You are not allowed to loot sleeping players"},
				
				{"Notify_Version", "TruePVE v. {0}"},
				{"Notify_AvailOptions", "Available Options: {0}"},
				{"Notify_SetSuccess", "Successfully set \"{0}\" to \"{1}\""},
				{"Notify_DefConfigLoad", "Loaded default configuration"},
				{"Notify_DefDataLoad", "Loaded default mapping data"},
				{"Notify_ProdResult", "Prod results: type={0}, prefab={1}"},
				
				{"Format_Wrapper", "<size={0}><color=\"{1}\">{2}</color></size>"},
				{"Format_NotifyColor", "#00FFFF"}, // cyan
				{"Format_NotifySize", "12"},
				{"Format_HeaderColor", "#FFA500"}, // orange
				{"Format_HeaderSize", "14"},
				{"Format_ErrorColor", "#FF0000"}, // red
				{"Format_ErrorSize", "12"},
				{"Format_ColorWrapper", "<color=\"{0}\">{1}</color>"},
				{"Format_SizeWrapper", "<size={0}>{1}</size>"}
			};
			lang.RegisterMessages(messages, this);
        }
        
        // get message from Lang
        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
        
        void warnPve() => PrintWarning(GetMessage("Warning_PveMode"));
		
		void Loaded()
		{
			LoadDefaultMessages();
			
			string baseCommand = GetMessage("ConsoleCommand");
			// register console commands
			foreach(Command command in Enum.GetValues(typeof(Command)))
				cmd.AddConsoleCommand((baseCommand + "." + command.ToString()), this, "ccmdDelegator");
			// register chat commands
			cmd.AddChatCommand(baseCommand + "_prod", this, "handleProd");
			// check for server pve setting
			if(ConVar.Server.pve)
				warnPve();
			// load configuration
			LoadConfig();
			// build usage string
			usageString = wrapSize(14, wrapColor("orange", GetMessage("Header_Usage"))) + "\n" +
						  wrapSize(12, wrapColor("cyan", baseCommand + "." + Command.set.ToString() + " " + GetMessage("Cmd_Usage_OptionString") + " " + GetMessage("Cmd_Usage_ValueString")) + " - " + GetMessage("Cmd_Usage_set") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.get.ToString() + " " + GetMessage("Cmd_Usage_OptionString")) + " - " + GetMessage("Cmd_Usage_get") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.def.ToString() + " " + GetMessage("Cmd_Usage_DefaultsOptions")) + " - " + GetMessage("Cmd_Usage_def") + "\n" +
									   wrapColor("cyan", "/" + baseCommand + "_prod") + " - " + GetMessage("Cmd_Usage_prod") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.list.ToString()) + " - " + GetMessage("Cmd_Usage_list") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.version.ToString()) + " - " + GetMessage("Cmd_Usage_version"));
			
			serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
		}
		// delegation method for console commands
		//[ConsoleCommand("tpve")]
		void ccmdDelegator(ConsoleSystem.Arg arg)
		{
			// return if user doesn't have access to run console command
			if(!hasAccess(arg)) return;
			
			string cmd = arg.cmd.namefull.Split('.')[1];
			if(!Enum.IsDefined(typeof(Command), cmd))
			{
				// shouldn't hit this
				SendMessage(arg, "Error_InvalidParameter");
			}
			else
			{
				switch((Command) Enum.Parse(typeof(Command), cmd))
				{
					case Command.version:
						SendMessage(arg, "Notify_Version", new string[] { this.Version.ToString()});
						return;
					case Command.def:
						if(handleDef(arg)) return; // set defaults
						break;
					case Command.list:
						handleList(arg); // display list options
						return;
					case Command.set:
						if(handleSet(arg)) return;
						break;
					case Command.get:
						if(handleGet(arg)) return;
						break;
					case Command.usage:
						showUsage(arg);
						return;
				}
				SendMessage(arg, "Error_InvalidParamForCmd");
			}
			showUsage(arg);
		}
		
		// show usage information
		void showUsage(ConsoleSystem.Arg arg)
		{
			SendReply(arg, usageString);
		}
		
		// check user access
		bool hasAccess(ConsoleSystem.Arg arg)
		{
			if (arg.connection != null)
			{
				if (arg.connection.authLevel < 1)
				{
					SendMessage(arg, "Error_NoPermission");
					return false;
				}
			}
			return true;
		}
		
		// handle setting defaults
		private bool handleDef(ConsoleSystem.Arg arg)
		{
			bool success = false;
			if(arg.Args == null || arg.Args[0] == null)
			{
				SendMessage(arg, "Error_InvalidParameter", new object[] {"null"});
				return false;
			}
			string flag = arg.Args[0];
			if(flag == "all" || flag == "config")
			{
				LoadDefaultConfig();
				SendMessage(arg, "Notify_DefConfigLoad");
				success = true;
			}
			if(flag == "all" || flag == "data")
			{
				LoadDefaultData();
				SaveData();
				SendMessage(arg, "Notify_DefDataLoad");
				success = true;
			}
			return success;
		}
		
		// handle prod command (raycast to determine what player is looking at)
		private void handleProd(BasePlayer player, string command, string[] args)
		{
			if(!isAdmin(player))
				SendMessage(player, "Error_NoPermission");
			
			object entity;
			if(!GetRaycastTarget(player, out entity) || entity == null)
			{
				SendReply(player, wrapSize(12, wrapColor("red", GetMessage("Error_NoEntityFound"))));
				return;
			}
			SendMessage(player, "Notify_ProdResult", new object[] { entity.GetType(), (entity as BaseEntity).ShortPrefabName });
		}
		
		// handle raycast from player
		bool GetRaycastTarget(BasePlayer player, out object closestEntity)
		{
			closestEntity = false;
			var input = serverinput.GetValue(player) as InputState;
			if (input == null || input.current == null || input.current.aimAngles == Vector3.zero)
				return false;
			
			Vector3 sourceEye = player.transform.position + new Vector3(0f, 1.6f, 0f);
			Ray ray = new Ray(sourceEye, Quaternion.Euler(input.current.aimAngles) * Vector3.forward);
			
			var hits = Physics.RaycastAll(ray);
			float closestdist = 100f;
			foreach (var hit in hits)
			{
				if (hit.collider.isTrigger)
					continue;
				if (hit.distance < closestdist)
				{
					closestdist = hit.distance;
					closestEntity = hit.GetEntity();
				}
			}
			if (closestEntity is bool)
				return false;
			return true;
		}
		
		// handle list command
		private void handleList(ConsoleSystem.Arg arg)
		{
			string str = "";//wrapColor("orange", GetMessage("AvailOptions"));
			foreach(Option opt in Enum.GetValues(typeof(Option)))
			{
				str += opt.ToString() + ", ";
			}
			str = str.Trim(new char[] {',',' '});
			SendMessage(arg, "Notify_AvailOptions" , new object[] {str});
		}
		
		// handle set command
		private bool handleSet(ConsoleSystem.Arg arg)
		{
			if(!Enum.IsDefined(typeof(Option), arg.Args[0]))
			{
				SendMessage(arg, "Error_InvalidParameter", new object[] {arg.Args[0]});
				return false;
			}
			
			Option opt = (Option) Enum.Parse(typeof(Option), arg.Args[0]);
			bool value;
			try {
				value = Convert.ToBoolean(arg.Args[1]);
			} catch(FormatException e) {
				SendMessage(arg, "Error_InvalidParameter", new object[] {arg.Args[1]});
				return false;
			}
			
			SaveEntry(opt,value);
			SendMessage(arg, "Notify_SetSuccess", new object[] {opt, value});
			return true;
		}
		
		// save updated entry to config
		private void SaveEntry(Option opt, bool value)
		{
			string optstr = opt.ToString();
			data.config[opt] = value;
			SaveConfig();
		}
		
		// handle get command
		private bool handleGet(ConsoleSystem.Arg arg)
		{
			if(arg.Args[0] == "all")
			{
				foreach(Option option in Enum.GetValues(typeof(Option)))
				{
					printValue(arg, option);
				}
				return true;
			}
			if(!Enum.IsDefined(typeof(Option), arg.Args[0]))
			{
				SendMessage(arg, "Error_InvalidParameter", new object[] {arg.Args[0]});
				return false;
			}
			Option opt = (Option) Enum.Parse(typeof(Option), arg.Args[0]);
			printValue(arg, opt);
			return true;
		}
		
		// prints the value of an Option
		private void printValue(ConsoleSystem.Arg arg, Option opt)
		{
			SendReply(arg, wrapSize(GetMessage("Format_NotifySize"), wrapColor(GetMessage("Format_NotifyColor"), opt + ": ") + data.config[opt]));
		}
		
		// load config
		void LoadConfig()
		{
			Config.Settings.NullValueHandling = NullValueHandling.Include;
			try {
				data = Config.ReadObject<TruePVEData>();
			} catch (Exception e) {
				data = new TruePVEData();
			}
			Config.Settings.NullValueHandling = NullValueHandling.Include;
			bool dirty = CheckConfig();
			if(data.data == null || data.data.Count == 0)
				dirty = LoadDefaultData();
			
			global = data.LookupByName("global");
			if(global == null)
				dirty = CreateDefaultGlobal();
			if(dirty)
				SaveData();
		}
		
		// save data
		void SaveData()
		{
			Config.WriteObject(data);
		}
		
		// verify/update configuration
		bool CheckConfig()
		{
			bool dirty = false;
			foreach(Option option in Enum.GetValues(typeof(Option)))
				if(!data.config.ContainsKey(option))
				{
					data.config[option] = def[(int)option];
					dirty = true;
				}
			return dirty;
		}
		
		// loads default configuration entries
		bool LoadDefaultConfig()
		{
			foreach(Option option in Enum.GetValues(typeof(Option)))
				data.config[option] = def[(int)option];
			return true;
		}
		
		// loads default data entries
		bool LoadDefaultData()
		{
			data.data.Clear();
			CreateDefaultGlobal();
			
			Hurtable napalm = data.CreateHurtable("napalm");
			napalm.description = "Heli Napalm";
			napalm.prefabs.Add("napalm");
			napalm.links[global.name] = true; // napalm hurts anything
			
			Hurtable player = data.CreateHurtable("player");
			player.description = "Players";
			player.types.Add(typeof(BasePlayer).Name);
			player.links[player.name] = false; // no player-vs-player damage
			global.links[player.name] = true; // anything hurts player
			
			//Hurtable fire = data.CreateHurtable("fire");
			//fire.type = typeof(FireBall).Name;
			//fire.prefabs.Add("campfire");
			//fire.links[global.name] = false; // no fire damage
			
			Hurtable traps = data.CreateHurtable("traps");
			traps.description = "Traps, landmines, and spikes";
			traps.types.Add(typeof(BearTrap).Name);
			traps.types.Add(typeof(Landmine).Name);
			traps.prefabs.Add("spikes.floor");
			//traps.prefabs.Add("beartrap");
			//traps.prefabs.Add("landmine");
			player.links[traps.name] = true; // players can damage traps
			traps.links[player.name] = false; // traps don't damage players
			
			Hurtable barricades = data.CreateHurtable("barricades");
			barricades.description = "Barricades";
			barricades.types.Add(typeof(Barricade).Name);
			player.links[barricades.name] = true; // players can damage barricade
			barricades.links[player.name] = false; // barricades cannot hurt players
			
			Hurtable highwalls = data.CreateHurtable("highwalls");
			highwalls.description = "High external walls";
			highwalls.prefabs.Add("wall.external.high.stone");
			highwalls.prefabs.Add("wall.external.high.wood");
			highwalls.links[player.name] = false; // high external walls cannot hurt players
			
			Hurtable heli = data.CreateHurtable("heli");
			heli.description = "Heli";
			heli.types.Add(typeof(BaseHelicopter).Name);
			global.links[heli.name] = true; // heli can take damage
			
			return true;
		}
		
		// creates default "global" container
		bool CreateDefaultGlobal() {
			global = data.CreateHurtable("global");
			global.description = "Global damage handling";
			global.links[global.name] = false; // map global to itself - default no damage
			return true;
		}
		
		// handle damage - if another mod must override TruePVE damages or take priority,
		// comment out this method and reference HandleDamage from the other mod(s)
		private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
		{
			if(!data.config[Option.handleDamage])
				return;
			HandleDamage(entity, hitinfo);
		}
		
		// handle damage
		[HookMethod("HandleDamage")]
		private void HandleDamage(BaseCombatEntity entity, HitInfo hitinfo)
		{
			if(!AllowDamage(entity, hitinfo))
				CancelDamage(hitinfo);
		}
		
		// determines if an entity is "allowed" to take damage
		[HookMethod("AllowDamage")]
		private bool AllowDamage(BaseCombatEntity entity, HitInfo hitinfo)
		{
			if(!global.enabled)
				return true;
			
			if (entity == null || hitinfo == null || hitinfo.Initiator == null) return true;
			
			// allow resource gathering
			if(entity.GetComponent<ResourceDispenser>() != null)
				return true;
			
			// allow decay
			if(hitinfo.damageTypes.Get(DamageType.Decay) > 0)
				return true;
			
			// check heli
			object heli = CheckHeliInitiator(hitinfo);
			if(heli != null)
				return (bool)heli;
			
			// allow NPC damage
			if (entity is BaseNPC || hitinfo.Initiator is BaseNPC)
				return true;
			
			// allow damage to door barricades and covers
			if(entity is Barricade && (entity.ShortPrefabName.Contains("door_barricade") || entity.ShortPrefabName.Contains("cover")))
				return true;
			
			// if entity is a barrel, trash can, or giftbox, allow damage
			if(entity.ShortPrefabName.Contains("barrel") ||
			   entity.ShortPrefabName == "loot_trash" ||
			   entity.ShortPrefabName == "giftbox_loot")
				return true;
			
			// handle suicide
			if(hitinfo.damageTypes.Get(DamageType.Suicide) > 0)
			{
				if(data.config[Option.allowSuicide])
					return true;
				else
				{
					SendMessage(entity as BasePlayer, "Error_NoSuicide");
					return false;
				}
			}
			
			// Check storage containers and doors for locks
			if((entity is StorageContainer || entity is Door) && data.config[Option.immortalLocks])
			{
				// check for lock
				object hurt = CheckLock(entity,hitinfo);
				if(hurt != null)
					return (bool) hurt;
			}
			
			// ignore checks if authorized damage enabled (except for players)
			if(data.config[Option.authDamage] && !(entity is BasePlayer) && CheckAuthDamage(entity, hitinfo.Initiator as BasePlayer))
				return true;
			
			// allow sleeper damage by admins if configured
			if(data.config[Option.sleeperAdminDamage] && entity is BasePlayer && hitinfo.Initiator is BasePlayer)
				if((entity as BasePlayer).IsSleeping() && isAdmin(hitinfo.Initiator as BasePlayer))
					return true;
			
			// handle rules
			List<Hurtable> hurtableList = new List<Hurtable>();
			hurtableList = data.Lookup(hitinfo.Initiator);
			List<Hurtable> otherList = new List<Hurtable>();
			otherList = data.Lookup(entity);
			if(hurtableList != null && hurtableList.Count > 0 && otherList != null && otherList.Count > 0)
			{
				// check direct assignment (hurtable mapped to hurtable)
				foreach(Hurtable h1 in hurtableList)
					foreach(Hurtable h2 in otherList)
					{
						object r = h1.CanHurt(h2);
						if(r != null)
							return (bool)r;
					}
			}
			if(hurtableList != null && hurtableList.Count > 0)
			{
				// check if initiator can hurt anything (hurtable mapped to global)
				foreach(Hurtable h1 in hurtableList)
				{
					object r = h1.CanHurt(global);
					if(r != null)
						return (bool)r;
				}
			}
			if(otherList != null && otherList.Count > 0)
			{
				// check if anything can hurt entity (global mapped to hurtable)
				foreach(Hurtable h2 in otherList)
				{
					object r = global.CanHurt(h2);
					if(r != null)
						return (bool)r;
				}
			}
			
			// handle global damage (global mapped to global)
			return global.links[global.name];
		}
		
		// checks for a lock
		object CheckLock(BaseCombatEntity entity, HitInfo hitinfo)
		{
			// exclude deployed items in storage container lock check (since they can't have locks)
			if(entity.ShortPrefabName == "lantern.deployed" ||
			   entity.ShortPrefabName == "ceilinglight.deployed" ||
			   entity.ShortPrefabName == "furnace.large" ||
			   entity.ShortPrefabName == "campfire" ||
			   entity.ShortPrefabName == "furnace" ||
			   entity.ShortPrefabName == "refinery_small_deployed" ||
			   entity.ShortPrefabName == "waterbarrel" ||
			   entity.ShortPrefabName == "jackolantern.angry" ||
			   entity.ShortPrefabName == "jackolantern.happy" ||
			   entity.ShortPrefabName == "repairbench_deployed" ||
			   entity.ShortPrefabName == "researchtable_deployed")
				return null;
			
			// if unlocked damage allowed - check for lock
			BaseLock alock = entity.GetSlot(BaseEntity.Slot.Lock) as BaseLock; // get lock
			if (alock == null) return true; // no lock, allow damage

			if (alock.IsLocked()) // is locked, cancel damage except heli
			{
				// if heliDamageLocked option is false or heliDamage is false, all damage is cancelled
				if(!data.config[Option.heliDamageLocked] || !data.config[Option.heliDamage]) return false;
				object heli = CheckHeliInitiator(hitinfo);
				if(heli != null)
					return (bool) heli;
				return false;
			}
			return true;
		}
		
		// check for heli
		object CheckHeliInitiator(HitInfo hitinfo)
		{
			// Check for heli initiator
			if(hitinfo.Initiator is BaseHelicopter ||
			   hitinfo.Initiator is HelicopterTurret)
				return data.config[Option.heliDamage];
			else if(hitinfo.WeaponPrefab != null) // prevent null spam
			{
				if(hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli" ||
				   hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli_napalm")
					return data.config[Option.heliDamage];
			}
			return null;
		}
		
		// checks if the player is authorized to damage the entity
		bool CheckAuthDamage(BaseCombatEntity entity, BasePlayer player)
		{
			// check if the player is the owner of the entity
			if(player.userID == entity.OwnerID)
				return true; // player is the owner, allow damage
			
			// assume no authorization by default
			bool authed = false;
			// check for cupboards which overlap the entity
			var hit = Physics.OverlapBox(entity.transform.position, entity.bounds.extents/2f, entity.transform.rotation, triggerMask);
			// loop through cupboards
			foreach (var ent in hit)
			{
				// get cupboard BuildingPrivilidge
				BuildingPrivlidge privs = ent.GetComponentInParent<BuildingPrivlidge>();
				// check if the player is authorized on the cupboard
				if (privs != null)
					if (!privs.IsAuthed(player))
						return false; // return false if not authorized on any single cupboard which overlaps this entity
					else
						authed = true; // set authed to true to indicate player is authorized on at least one cupboard
			}
            return authed;
		}
		
		// cancel damage
		private static void CancelDamage(HitInfo hitinfo)
		{
			hitinfo.damageTypes = new DamageTypeList();
            hitinfo.DoHitEffects = false;
			hitinfo.HitMaterial = 0;
		}
		
		// handle looting - if another mod must override TruePVE looting behavior,
		// comment out this method and reference AllowLoot from the other mod(s)
		private object CanLootPlayer(BasePlayer target, BasePlayer player)
		{
			if(!data.config[Option.handleLooting]) // let other mods handle looting and hook to HandleLoot if necessary
				return null;
			if(!HandleLoot(player, target))
				return (object) false; // non-null allows looting
			return null;
		}
		
		// handle looting players
        private void OnLootPlayer(BasePlayer player, BasePlayer target)
        {
			if(!data.config[Option.handleLooting]) // let other mods handle looting and hook to HandleLoot if necessary
				return;
			HandleLoot(player, target);
		}
		
		// handle looting entities
		private void OnLootEntity(BasePlayer player, BaseEntity target)
		{
			if(!data.config[Option.handleLooting]) // let other mods handle looting and hook to HandleLoot if necessary
				return;
			HandleLoot(player, target);
		}
		
		// handle looting players
		[HookMethod("HandleLoot")]
		public bool HandleLoot(BasePlayer player, BaseEntity target)
		{
			if(target == null)
				return true;
			if(!AllowLoot(player, target))
			{
				CancelLooting(player, target);
				return false;
			}
			return true;
		}
		
		// determine whether to allow looting sleepers and other players' corpses
		[HookMethod("AllowLoot")]
		private bool AllowLoot(BasePlayer player, BaseEntity target)
		{
			if(isAdmin(player))
				return true;
			else if(target is BasePlayer && (target as BasePlayer).IsSleeping())
				return data.config[Option.sleeperLooting];
			else if(target is LootableCorpse && (Convert.ToString(player.userID) != Convert.ToString((target as LootableCorpse).playerSteamID)))
				return data.config[Option.corpseLooting];
			return true;
		}
		
		// cancel looting and send a message to the player
		void CancelLooting(BasePlayer player, BaseEntity target)
		{
			string message = "";
			if(target is LootableCorpse)
				message = "Error_NoLootCorpse";
			else if(target is BasePlayer)
				message = "Error_NoLootSleeper";
			
			NextTick(() =>
			{
				player.EndLooting();
				SendMessage(player, message);
			});
		}
		
		// send message to player
		void SendMessage(BasePlayer player, string key, object[] options = null)
		{
			SendReply(player, BuildMessage(key, options));
		}
		
		// send message to player
		void SendMessage(ConsoleSystem.Arg arg, string key, object[] options = null)
		{
			SendReply(arg, BuildMessage(key, options));
		}
		
		string BuildMessage(string key, object[] options = null)
		{
			string message = GetMessage(key);
			if(options != null && options.Length > 0)
				message = String.Format(message, options);
			string type = key.Split('_')[0];
			string size = GetMessage("Format_"+type+"Size");
			string color = GetMessage("Format_"+type+"Color");
			return String.Format(GetMessage("Format_Wrapper"), new object[] {size, color, message});
		}
		
		string wrapSize(string size, string input)
		{
			int i = 0;
			if(Int32.TryParse(size, out i))
				return wrapSize(i, input);
			return input;
		}
		
		// wrap a string in a <size> tag with the passed size
		string wrapSize(int size, string input)
		{
			if(input == null || input == "")
				return input;
			return String.Format(GetMessage("Format_SizeWrapper"), new object[] {size, input});
		}
		
		// wrap a string in a <color> tag with the passed color
		string wrapColor(string color, string input)
		{
			if(input == null || input == "" || color == null || color == "")
				return input;
			return String.Format(GetMessage("Format_ColorWrapper"), new object[] {color, input});
		}
		
		// is admin
        private bool isAdmin(BasePlayer player)
        {
        	if (player == null) return false;
            if (player?.net?.connection == null) return true;
            return player.net.connection.authLevel > 0;
        }
		
		// configuration and data storage container
		private class TruePVEData
		{
			public Dictionary<Option,bool> config = new Dictionary<Option,bool>();
			public Dictionary<string,Hurtable> data = new Dictionary<string,Hurtable>();
			
			public Hurtable LookupByName(string name) {
				return data.ContainsKey(name) ? data[name] : null;
			}
			
			public List<Hurtable> Lookup(BaseEntity entity) {
				return data.Values.ToList().Where(h => (h.prefabs != null && h.prefabs.Contains(entity.ShortPrefabName)) || (h.types != null && h.types.Contains(entity.GetType().Name))).ToList();
			}
			
			public Hurtable CreateHurtable(string name)
			{
				Hurtable h = new Hurtable();
				h.name = name;
				data[h.name] = h;
				return h;
			}
		}
		
		// container for mapping entities
		private class Hurtable
		{
			public string name;
			public string description;
			public List<string> prefabs = new List<string>();
			public List<string> types = new List<string>();
			public Dictionary<string,bool> links = new Dictionary<string,bool>();
			public bool enabled = true;
			
			public object CanHurt(Hurtable other) {
				if(!enabled || !other.enabled)
					return null;
				if(links != null && links.Count > 0 && links.ContainsKey(other.name))
					return links[other.name];
				return null;
			}
		}
	}
}