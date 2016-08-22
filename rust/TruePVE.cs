using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
	[Info("TruePVE", "ignignokt84", "0.3.3", ResourceId = 1789)]
	class TruePVE : RustPlugin
	{
		/*
		
		This plugin is meant to better represent PVE damage and looting in place of using the
		server.pve = true setting.  This plugin is designed to be used with server.pve = false
		because there's no reflection to worry about.
		
		- Prevents player damage which originates from other players
		- Prevents players from looting sleepers or another player's corpse
		- Makes most other objects unbreakable by players
		- Beds and sleeping bags take no damage
		- Barricades will still take damage
		- Locked doors and boxes take no damage, however unlocked doors and boxes still take damage and can break
		- Heli can damage aforementioned "unbreakable" entities
		
		*/
		
		// load default messages to Lang
		void LoadDefaultMessages()
		{
			var messages = new Dictionary<string, string>
			{
				{"ConsoleCommand", "tpve"},
				{"VersionString", "TruePVE v. {0}"},
				
				{"UsageHeader", "---- TruePVE usage ----"},
				{"CmdUsageSet", "Set value of specified option"},
				{"CmdUsageGet", "Get value of specified option"},
				{"CmdUsageDesc", "Describe specified option"},
				{"CmdUsageList", "Lists available options"},
				{"CmdUsageDef", "Loads default configuration"},
				{"CmdUsageVersion", "Prints version information"},
				{"CmdUsageOptionString", "[option]"},
				{"CmdUsageValueString", "[value]"},
				
				{"InvalidParameter", "Invalid parameter: {0}"},
				{"InvalidParamForCmd", "Invalid parameters for command \"{0}\""},
				{"PveWarning", "Server is set to PVE mode!  TruePVE is designed for PVP mode, and may cause unexpected behavior in PVE mode."},
				
				{"NoPermission", "Cannot execute command: No permission"},
				{"NoSuicide", "You are not allowed to commit suicide"},
				{"NoLootCorpse", "You are not allowed to loot another player's corpse"},
				{"NoLootSleeper", "You are not allowed to loot sleeping players"},
				
				{"AvailOptions", "Available Options: {0}"},
				{"SetSuccess", "Successfully set \"{0}\" to \"{1}\""},
				{"DefConfigLoad", "Loaded default configuration"},
				
				{"OptionDescHeader", "---- Option: \"{0}\" ----\n"},
				
				{"Desc_barricade", "Whether or not barricades should be damageable\n" +
								  " true: Barricades will take damage and can be destroyed\n" +
								  " false: Barricades will NOT take damage and cannot be destroyed"},
				
				{"Desc_unlocked", "Whether or not unlocked boxes and doors should be damageable\n" +
								 " true: Unlocked doors and boxes will take damage and can be destroyed\n" + 
								 " false: Unlocked doors and boxes will NOT take damage and cannot be destroyed"},
				
				{"Desc_sleepingbag", "Whether or not sleeping bags and beds should be damageable\n" +
									" true: Sleeping bags and beds will take damage and can be destroyed\n" +
									" false: Sleeping bags and beds will NOT take damage and cannot be destroyed"},
				
				{"Desc_heli", "Whether heli can damage entities which have been deemed indestructible\n" +
							 " true: Heli can damage entities (normal behavior)\n" +
							 " false: Heli cannot damage entities"},
				
				{"Desc_sleeper", "Whether sleepers can be looted\n" +
								" true: Anyone can loot sleepers\n" +
								" false: No one can loot sleepers"},
				
				{"Desc_corpse", "Whether other players' corpses can be looted\n" +
							   " true: Players can loot any corpse\n" +
							   " false: Players can only loot their own corpse(s)"},
				
				{"Desc_suicide", "Whether players can commit suicide via F1 > \"kill\" command\n" +
								" true: Players can commit suicide\n" +
								" false: Players cannot commit suicide"},
				
				{"Desc_hookdamage", "Whether to enable processing of OnEntityTakeDamage hook\n" +
								   " true: Enable normal TruePVE handling of entity damage\n" +
								   " false: Disable TruePVE handling of entity damage - external plugin can call HandleDamage hook"},
				
				{"Desc_hookloot", "Whether to enable processing of OnLoot* hooks\n" +
								 " true: Enable normal TruePVE handling of looting\n" +
								 " false: Disable TruePVE handling of looting - external plugin can call HandleLooting hook"},
				
				{"Desc_turret", "Whether to allow turrets to damage players\n" +
							   " true: Turrets can hurt players\n" +
							   " false: Turrets cannot hurt players"},
				
				{"Desc_fire", "Whether to enable fire damage\n" +
							   " true: Fire can damage anything\n" +
							   " false: Fire cannot damage anything"},
				
				{"Desc_helilocked", "Whether locked boxes/doors take damage from heli\n" +
								   " true: Heli can damage/destroy locked doors/boxes\n" +
								   " false: Heli cannot damage locked doors/boxes"},
				
				{"Desc_killsleepers", "Whether admins can kill sleepers\n" +
									 " true: Admins can kill sleepers\n" +
									 " false: Admins cannot kill sleepers"},
				
				{"Desc_authdamage", "Whether players can damage entities where building authorized\n" +
									" true: Players can damage entities where build authorized\n" +
									" false: Players cannot damage entities"},
				
				{"Desc_napalm", "Whether heli napalm can damage entities\n" +
								" true: Napalm can damage entities\n" +
								" false: Napalm cannot damage entities"}
			};
			lang.RegisterMessages(messages, this);
        }
		
		// option values
		private Dictionary<Option,object> data = new Dictionary<Option,object>();
		// has config changed?
		private bool hasConfigChanged;
		// usage information string with formatting
		public string usageString;
		// command enum
		private enum Command { usage, set, get, desc, list, version, def };
		// option enum
		private enum Option { barricade, unlocked, sleepingbag, heli, sleeper, corpse, suicide, hookdamage, hookloot, turret, fire, helilocked, killsleepers, authdamage, napalm};
		// default values array
		private object[] def = { true, true, false, true, false, false, true, true, true, false, false, false, false, false, true };
		// layer mask for finding authorization
        private readonly int triggerMask = LayerMask.GetMask("Trigger");
		
		// load
		void Loaded()
		{
			LoadDefaultMessages();
			// build commands based on enum values
			string baseCommand = GetMessage("ConsoleCommand");
			foreach(Command command in Enum.GetValues(typeof(Command)))
				cmd.AddConsoleCommand((baseCommand + "." + command.ToString()), this, "ccmdDelegator");
			if(ConVar.Server.pve)
				warnPve();
			LoadConfig();
			// build usage string
			usageString = wrapSize(14, wrapColor("orange", GetMessage("UsageHeader"))) + "\n" +
						  wrapSize(12, wrapColor("cyan", baseCommand + "." + Command.set.ToString() + " " + GetMessage("CmdUsageOptionString") + " " + GetMessage("CmdUsageValueString")) + " - " + GetMessage("CmdUsageSet") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.get.ToString() + " " + GetMessage("CmdUsageOptionString")) + " - " + GetMessage("CmdUsageGet") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.desc.ToString() + " " + GetMessage("CmdUsageOptionString")) + " - " + GetMessage("CmdUsageDesc") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.list.ToString()) + " - " + GetMessage("CmdUsageList") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.def.ToString()) + " - " + GetMessage("CmdUsageDef") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.version.ToString()) + " - " + GetMessage("CmdUsageVersion"));
		}
        
        // get message from Lang
        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
		
		// delegation method for console commands
		//[ConsoleCommand("tpve")]
		void ccmdDelegator(ConsoleSystem.Arg arg)
		{
			// user doesn't have access to run console command
			if(!hasAccess(arg)) return;
			
			string cmd = arg.cmd.namefull.Split('.')[1];
			if(!Enum.IsDefined(typeof(Command), cmd))
			{
				// shouldn't hit
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), cmd))));
			}
			else
			{
				switch((Command) Enum.Parse(typeof(Command), cmd))
				{
					case Command.version:
						SendReply(arg, wrapSize(14, wrapColor("orange", String.Format(GetMessage("VersionString"), this.Version.ToString()))));
						return;
					case Command.def:
						LoadDefaultConfig();
						SendReply(arg, wrapSize(12, wrapColor("green", GetMessage("DefConfigLoad"))));
						return;
					case Command.list:
						handleList(arg); // display list options
						return;
					case Command.set:
						if(handleSet(arg)) return;
						break;
					case Command.get:
						if(handleGet(arg)) return;
						break;
					case Command.desc:
						if(handleDesc(arg)) return;
						break;
					case Command.usage:
						showUsage(arg);
						return;
				}
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParamForCmd"), arg.cmd.namefull))));
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
					SendReply(arg, GetMessage("NoPermission"));
					return false;
				}
			}
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
			SendReply(arg, wrapSize(12, wrapColor("orange", String.Format(GetMessage("AvailOptions"),str))));
		}
		
		// handle set command
		private bool handleSet(ConsoleSystem.Arg arg)
		{
			if(!Enum.IsDefined(typeof(Option), arg.Args[0]))
			{
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), arg.Args[0]))));
				return false;
			}
			
			Option opt = (Option) Enum.Parse(typeof(Option), arg.Args[0]);
			object value;
			try {
				value = Convert.ToBoolean(arg.Args[1]);
			} catch(FormatException e) {
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), arg.Args[1]))));
				return false;
			}
			
			SaveEntry(opt,value);
			SendReply(arg, wrapSize(12, wrapColor("cyan", String.Format(GetMessage("SetSuccess"), new object[] {opt, value}))));
			return true;
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
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), arg.Args[0]))));
				return false;
			}
			Option opt = (Option) Enum.Parse(typeof(Option), arg.Args[0]);
			printValue(arg, opt);
			return true;
		}
		
		// prints the value of an Option
		private void printValue(ConsoleSystem.Arg arg, Option opt)
		{
			SendReply(arg, wrapSize(12, wrapColor("cyan", opt + ": ") + data[opt]));
		}
		
		// handle desc command
		private bool handleDesc(ConsoleSystem.Arg arg)
		{
			if(!Enum.IsDefined(typeof(Option), arg.Args[0]))
			{
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), arg.Args[0]))));
				return false;
			}
			Option opt = (Option) Enum.Parse(typeof(Option), arg.Args[0]);
			showDesc(arg, opt);
			
			return true;
		}
		
		// prints the description of an Option
		private void showDesc(ConsoleSystem.Arg arg, Option opt)
		{
			SendReply(arg, wrapSize(14, wrapColor("orange", String.Format(GetMessage("OptionDescHeader"), opt.ToString()))) + wrapSize(12, wrapColor("cyan", GetMessage("Desc_" + opt.ToString()))));
		}
		
		// warn that server is in pve mode
		private void warnPve()
		{
			PrintWarning(GetMessage("PveWarning"));
		}
		
		// loads default configuration
		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadConfig();
		}
		
		// loads config from file
		private void LoadConfig()
		{
			foreach(Option opt in Enum.GetValues(typeof(Option)))
				data[opt] = Convert.ToBoolean(GetConfig(opt, def[(int)opt]));
			
			if (!hasConfigChanged) return;
			SaveConfig();
			hasConfigChanged = false;
		}
		
		// get config options, or set to default value if not found
		private object GetConfig(object opt, object defaultValue)
		{
			string optstr = opt.ToString();
			object value = Config[optstr];
			if (value == null)
			{
				value = defaultValue;
				Config[optstr] = value;
				hasConfigChanged = true;
			}
			return value;
		}
		
		// save updated entry to config
		private void SaveEntry(Option opt, object value)
		{
			string optstr = opt.ToString();
			data[opt] = value;
			Config[optstr] = value;
			SaveConfig();
		}
		
		// handle damage - if another mod must override TruePVE damages or take priority,
		// comment out this method and reference HandleDamage from the other mod(s)
		private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
		{
			if(!getBool(Option.hookdamage)) // let other mods handle damage and hook to HandleDamage if necessary
				return;
			HandleDamage(entity, hitinfo);
		}
		
		// handle damage
		// exposed as hook method for other mods to use
		[HookMethod("HandleDamage")]
		private void HandleDamage(BaseCombatEntity entity, HitInfo hitinfo)
		{
			if(!AllowDamage(entity, hitinfo))
				CancelDamage(hitinfo);
		}
		
		// determines if an entity is "allowed" to take damage
		// exposed as hook method for other mods to use
		[HookMethod("AllowDamage")]
		private bool AllowDamage(BaseCombatEntity entity, HitInfo hitinfo)
		{
			if (entity == null) return true;
			var resource = entity.GetComponent<ResourceDispenser>();
			if (resource != null)
			{
				// allow resource gathering
			}
			else if (entity is BasePlayer)
			{
				// allow environment damage
				if(hitinfo.Initiator == null)
					return true;
				// check for fireball
				if (hitinfo.Initiator is FireBall || hitinfo.Initiator.ShortPrefabName == "campfire")
					return getBool(Option.fire);
				// allow heli napalm damage
				if(hitinfo.Initiator.ShortPrefabName == "napalm")
					return getBool(Option.napalm);
				// ignore external wall damage
				if(hitinfo.Initiator.ShortPrefabName == "wall.external.high.stone" ||
				   hitinfo.Initiator.ShortPrefabName == "wall.external.high.wood")
					return false;
				// allow suicide
				if(hitinfo.damageTypes.Get(DamageType.Suicide) > 0)
					if(getBool(Option.suicide))
						return true;
					else
					{
						sendMessage(entity as BasePlayer, wrapSize(12, wrapColor("red", GetMessage("NoSuicide"))));
						return false;
					}
				// prevent pvp damage
				if(hitinfo.Initiator is BasePlayer)
				{
					// allow admins to kill sleepers if option is toggled on
					if(((BasePlayer)entity).IsSleeping() && isAdmin((BasePlayer)hitinfo.Initiator))
						return getBool(Option.killsleepers);
					return false;
				}
				// ignore trap damage
				if(hitinfo.Initiator is BaseTrap)
					return false;
				// ignore barricade damage
				if(hitinfo.Initiator is Barricade)
					return false;
				// ignore spike trap, campfire, and external wall damage
				if(hitinfo.Initiator.ShortPrefabName == "spikes.floor")
					return false;
				// handle turret damage
				if(hitinfo.Initiator is AutoTurret)
					return getBool(Option.turret);
			}
			else if (entity is BaseNPC)
			{
				// allow NPC damage
				return true;
			}
			else if (entity is Barricade)
			{
				// exclude door barricades and covers
				if(entity.ShortPrefabName.Contains("door_barricade") || entity.ShortPrefabName.Contains("cover"))
					return true;
				// allow heli napalm damage
				if(hitinfo.Initiator != null && hitinfo.Initiator.ShortPrefabName == "napalm")
					return getBool(Option.napalm);
				// check for fireball
				if (hitinfo.Initiator != null && hitinfo.Initiator is FireBall)
					return getBool(Option.fire);
				// Check for build authorization permission
				if(getBool(Option.authdamage) && hitinfo.Initiator != null && hitinfo.Initiator is BasePlayer)
					return checkAuthDamage(entity, hitinfo.Initiator as BasePlayer);
				// Check for heli initiator
				if(hitinfo.Initiator is BaseHelicopter ||
				   hitinfo.Initiator is HelicopterTurret)
					return getBool(Option.heli);
				else if(hitinfo.WeaponPrefab != null) // prevent null spam
				{
					if(hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli" ||
					   hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli_napalm")
						return getBool(Option.heli);
				}
				// if damage not allowed, cancel damage
				return getBool(Option.barricade);
			}
			else if (entity is SleepingBag)
			{
				// allow heli napalm damage
				if(hitinfo.Initiator != null && hitinfo.Initiator.ShortPrefabName == "napalm")
					return getBool(Option.napalm);
				// check for fireball
				if (hitinfo.Initiator != null && hitinfo.Initiator is FireBall)
					return getBool(Option.fire);
				// Check for build authorization permission
				if(getBool(Option.authdamage) && hitinfo.Initiator != null && hitinfo.Initiator is BasePlayer)
					return checkAuthDamage(entity, hitinfo.Initiator as BasePlayer);
				// Check for heli initiator
				if(hitinfo.Initiator is BaseHelicopter ||
				   hitinfo.Initiator is HelicopterTurret)
					return getBool(Option.heli);
				else if(hitinfo.WeaponPrefab != null) // prevent null spam
				{
					if(hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli" ||
					   hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli_napalm")
						return getBool(Option.heli);
				}
				// if damage not allowed, cancel damage
				return getBool(Option.sleepingbag);
			}
			else if(entity is StorageContainer || entity is Door)
			{
				// if entity is a barrel or giftbox, allow damage
				if(entity.ShortPrefabName.Contains("barrel") || entity.ShortPrefabName == "giftbox_loot")
					return true;
				// if entity is a trash can, allow damage
				if(entity.ShortPrefabName == "loot_trash")
					return true;
				// allow heli napalm damage
				if(hitinfo.Initiator != null && hitinfo.Initiator.ShortPrefabName == "napalm")
					return getBool(Option.napalm);
				// Check for build authorization permission
				if(getBool(Option.authdamage) && hitinfo.Initiator != null && hitinfo.Initiator is BasePlayer)
					return checkAuthDamage(entity, hitinfo.Initiator as BasePlayer);
				// check misc deployables and prevent damage
				if(entity.ShortPrefabName == "lantern.deployed" ||
				   entity.ShortPrefabName == "ceilinglight.deployed" ||
				   entity.ShortPrefabName == "furnace.large" ||
				   entity.ShortPrefabName == "furnace" ||
				   entity.ShortPrefabName == "refinery_small_deployed" ||
				   entity.ShortPrefabName == "waterbarrel" ||
				   entity.ShortPrefabName == "jackolantern.angry" ||
				   entity.ShortPrefabName == "jackolantern.happy" ||
				   entity.ShortPrefabName == "repairbench_deployed" ||
				   entity.ShortPrefabName == "researchtable_deployed")
				{
					// Check for heli initiator
					if(hitinfo.Initiator is BaseHelicopter ||
					   hitinfo.Initiator is HelicopterTurret)
						return getBool(Option.heli);
					else if(hitinfo.WeaponPrefab != null) // prevent null spam
					{
						if(hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli" ||
						   hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli_napalm")
							return getBool(Option.heli);
					}
					// don't allow damage
					return false;
				}
				
				// check campfire/gates
				if(entity.ShortPrefabName == "campfire" ||
				   entity.ShortPrefabName == "gates.external.high.stone" ||
				   entity.ShortPrefabName == "gates.external.high.wood")
				{
					// allow campfire/gate decay
					if(hitinfo.damageTypes.Get(DamageType.Decay) > 0)
						return true;
					// Check for heli initiator
					if(hitinfo.Initiator is BaseHelicopter ||
					   hitinfo.Initiator is HelicopterTurret)
						return getBool(Option.heli);
					else if(hitinfo.WeaponPrefab != null) // prevent null spam
					{
						if(hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli" ||
						   hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli_napalm")
							return getBool(Option.heli);
					}
					return false;
				}
				// check for fireball
				if (hitinfo.Initiator != null && hitinfo.Initiator is FireBall)
					return getBool(Option.fire);
				
				// if damage not allowed, cancel damage
				if(!getBool(Option.unlocked))
				{
					// Check for heli initiator
					if(hitinfo.Initiator is BaseHelicopter ||
					   hitinfo.Initiator is HelicopterTurret)
						return getBool(Option.heli);
					else if(hitinfo.WeaponPrefab != null) // prevent null spam
					{
						if(hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli" ||
						   hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli_napalm")
							return getBool(Option.heli);
					}
					return false;
				}
				
				// if unlocked damage allowed - check for lock
				BaseLock alock = entity.GetSlot(BaseEntity.Slot.Lock) as BaseLock; // get lock
				if (alock == null) return true; // no lock, allow damage

				if (alock.IsLocked()) // is locked, cancel damage except heli
				{
					// if helilocked option is false or heli damage is false, all damage is cancelled
					if(!getBool(Option.helilocked) || !getBool(Option.heli)) return false;
					// Check for heli initiator
					if(hitinfo.Initiator is BaseHelicopter ||
					   hitinfo.Initiator is HelicopterTurret)
						return getBool(Option.heli);
					else if(hitinfo.WeaponPrefab != null) // prevent null spam
					{
						if(hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli" ||
						   hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli_napalm")
							return getBool(Option.heli);
					}
					return false;
				}
			}
			else if (!(entity is LootContainer) &&
					 !(entity is BaseHelicopter) &&
					 !(entity is AutoTurret) &&
					 !(entity is StorageContainer) &&
					 !(entity is Door) &&
					 !(entity is BaseTrap))
			{
				// allow heli napalm damage
				if(hitinfo.Initiator != null && hitinfo.Initiator.ShortPrefabName == "napalm")
					return getBool(Option.napalm);
				// check for fireball
				if (hitinfo.Initiator != null && hitinfo.Initiator is FireBall)
					return getBool(Option.fire);
				// Check for build authorization permission
				if(getBool(Option.authdamage) && hitinfo.Initiator != null && hitinfo.Initiator is BasePlayer)
					return checkAuthDamage(entity, hitinfo.Initiator as BasePlayer);
				// prevent damage except for decay and (maybe) heli
				if(hitinfo.damageTypes.Get(DamageType.Decay) > 0)
					return true;
				if(hitinfo.Initiator is BaseHelicopter ||
				   hitinfo.Initiator is HelicopterTurret)
					return getBool(Option.heli);
				else if(hitinfo.WeaponPrefab != null) // prevent null spam
				{
					if(hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli" ||
					   hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli_napalm")
						return getBool(Option.heli);
				}
				
				return false;
			}
			else
			{
				// allow heli napalm damage
				if(hitinfo.Initiator != null && hitinfo.Initiator.ShortPrefabName == "napalm")
					return getBool(Option.napalm);
				// check for fireball
				if (hitinfo.Initiator != null && hitinfo.Initiator is FireBall)
					return getBool(Option.fire);
			}
			return true;
		}
		
		// cancel damage
		private static void CancelDamage(HitInfo hitinfo)
		{
			hitinfo.damageTypes = new DamageTypeList();
            hitinfo.DoHitEffects = false;
			hitinfo.HitMaterial = 0;
		}
		
		// checks if the player is authorized on all cupboards intersecting the entity
		private bool checkAuthDamage(BaseCombatEntity entity, BasePlayer player)
		{
			// check if the player is the owner of the entity
			if(player.userID == entity.OwnerID)
				return true; // player is the owner, allow damage
			
			// assume no authorization by default
			bool authed = false;
			// check for cupboards which overlap the entity
			var hit = Physics.OverlapBox(entity.transform.position, entity.bounds.extents/2f, entity.transform.rotation, triggerMask);
			//var hit = Physics.OverlapSphere(entity.transform.position, 1.0f, triggerMask);
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
		
		// wrap a string in a <size> tag with the passed size
		static string wrapSize(int size, string input)
		{
			if(input == null || input == "")
				return input;
			return "<size=" + size + ">" + input + "</size>";
		}
		
		// wrap a string in a <color> tag with the passed color
		static string wrapColor(string color, string input)
		{
			if(input == null || input == "" || color == null || color == "")
				return input;
			return "<color=" + color + ">" + input + "</color>";
		}
		
		// convert Option value to bool
		private bool getBool(Option opt)
		{
			return Convert.ToBoolean(data[opt]);
		}
		
		// convert Option value to float
		private float getFloat(Option opt)
		{
			return Convert.ToSingle(data[opt]);
		}
		
		// handle looting - if another mod must override TruePVE looting behavior,
		// comment out this method and reference AllowLoot from the other mod(s)
		private object CanLootPlayer(BasePlayer target, BasePlayer player)
		{
			if(!getBool(Option.hookloot)) // let other mods handle looting and hook to HandleLoot if necessary
				return null;
			if(!HandleLoot(player, target, false))
				return (object) false; // non-null allows looting
			return null;
		}
		
		// handle looting players
        private void OnLootPlayer(BasePlayer player, BasePlayer target)
        {
			if(!getBool(Option.hookloot)) // let other mods handle looting and hook to HandleLoot if necessary
				return;
			HandleLoot(player, target);
		}
		
		// handle looting entities
		private void OnLootEntity(BasePlayer player, BaseEntity target)
		{
			if(!getBool(Option.hookloot)) // let other mods handle looting and hook to HandleLoot if necessary
				return;
			HandleLoot(player, target);
		}
		
		// handle looting players
		[HookMethod("HandleLoot")]
		public bool HandleLoot(BasePlayer player, BaseEntity target, bool cancel = true)
		{
			if(target == null)
				return true;
			if(!AllowLoot(player, target))
			{
				if(cancel)
					CancelLooting(player, target);
				return false;
			}
			return true;
		}
		
		// determine whether to allow looting sleepers and other players' corpses
		// exposed as hook method for other mods to use
		[HookMethod("AllowLoot")]
		private bool AllowLoot(BasePlayer player, BaseEntity target)
		{
			if(isAdmin(player))
				return true;
			else if(target is BasePlayer && (target as BasePlayer).IsSleeping())
				return getBool(Option.sleeper);
			else if(target is LootableCorpse && (Convert.ToString(player.userID) != Convert.ToString((target as LootableCorpse).playerSteamID)))
				return getBool(Option.corpse);
			return true;
		}
		
		// cancel looting and send a message to the player
		void CancelLooting(BasePlayer player, BaseEntity target)
		{
			string message = "";
			if(target is LootableCorpse)
				message = GetMessage("NoLootCorpse");
			else if(target is BasePlayer)
				message = GetMessage("NoLootSleeper");
			
			NextTick(() =>
			{
				player.EndLooting();
				sendMessage(player, wrapSize(12, wrapColor("red", message)));
			});
		}
		
		// send message to player
		void sendMessage(BasePlayer player, string message)
		{
			//if(checkPopup())
			//	PopupNotifications.Call("CreatePopupNotification", message, player);
			//else
				SendReply(player, message);
		}
		
		// is admin
        private bool isAdmin(BasePlayer player)
        {
        	if (player == null) return false;
            if (player?.net?.connection == null) return false;
            return player.net.connection.authLevel > 0;
        }
	}
}