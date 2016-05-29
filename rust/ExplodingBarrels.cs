using System.Collections.Generic;
using System;
using Rust;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("ExplodingBarrels", "ignignokt84", "0.1.1", ResourceId = 1902)]
	class ExplodingBarrels : RustPlugin
	{
		// list of barrels containing explosives
		private Dictionary<uint,ExplosiveType> barrels;
		// random number generator
		private System.Random random;
		// explosive types for effects
		private enum ExplosiveType { Rocket, IncendRocket };
		// damage types list
		private List<DamageTypeEntry> damage;
		
		// debug toggle
		private bool debug = false;
		// configuration data
		private Dictionary<Option,object> data = new Dictionary<Option,object>();
		// usage string
		public string usageString;
		// configuration changed flag
		private bool hasConfigChanged;
		// enum of valid commands
		private enum Command { usage, set, get, desc, list, version, def, count, debug };
		// enum of valid options
		private enum Option { chance, amount, minradius, radius, delay, killinv };
		// default values
		private object[] def = { 5f, 95f, 2f, 5f, 1f, true };
		
		// load default messages to Lang
		void LoadDefaultMessages()
		{
			var messages = new Dictionary<string, string>
			{
				{"ConsoleCommand", "xbar"},
				{"VersionString", "ExplodingBarrels v. {0}"},
				
				{"UsageHeader", "---- ExplodingBarrels usage ----"},
				{"CmdUsageSet", "Set value of specified option"},
				{"CmdUsageGet", "Get value of specified option"},
				{"CmdUsageDesc", "Describe specified option"},
				{"CmdUsageList", "Lists available options"},
				{"CmdUsageDef", "Loads default configuration"},
				{"CmdUsageCount", "Count and display locations of explodable barrels"},
				{"CmdUsageVersion", "Prints version information"},
				{"CmdUsageOptionString", "[option]"},
				{"CmdUsageValueString", "[value]"},
				
				{"Desc_chance", "Percent chance that a barrel containing an explosive will explode on hit"},
				{"Desc_amount", "Max amount of damage to be dealt by explosion"},
				{"Desc_minradius", "Minimum radius to apply max damage"},
				{"Desc_radius", "Max explosion damage radius"},
				{"Desc_delay", "Delay between hit and explosion"},
				{"Desc_killinv", "Whether to destroy inventory on explosion"},
				
				{"AvailOptions", "Available Options: {0}"},
				{"SetSuccess", "Successfully set \"{0}\" to \"{1}\""},
				{"DefConfigLoad", "Loaded default configuration"},
				
				{"OptionDescHeader", "---- Option: \"{0}\" ----\n"},
				{"CountHeader", "---- Explosive Barrels: {0} ----\n"},
				{"InvalidParameter", "Invalid parameter: {0}"},
				{"InvalidParamForCmd", "Invalid parameters for command \"{0}\""},
				{"NoPermission", "Cannot execute command: No permission"},
				{"ToggleDebug", "Debugging has been set to {0}"}
			};
			lang.RegisterMessages(messages, this);
		}
		
        // get message from Lang
        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
		
		// Initialize plugin
		void Init()
		{
			barrels = new Dictionary<uint,ExplosiveType>();
			damage = new List<DamageTypeEntry>();
			random = new System.Random();
		}
		
		// Loaded
		void Loaded()
		{
			LoadDefaultMessages();
			string baseCommand = GetMessage("ConsoleCommand");
			foreach(Command command in Enum.GetValues(typeof(Command)))
				cmd.AddConsoleCommand((baseCommand + "." + command.ToString()), this, "ccmdDelegator");
			LoadConfig();
			buildDamageTypes();
			// build usage string
			usageString = wrapSize(14, wrapColor("orange", GetMessage("UsageHeader"))) + "\n" +
						  wrapSize(12, wrapColor("cyan", baseCommand + "." + Command.set.ToString() + " " + GetMessage("CmdUsageOptionString") + " " + GetMessage("CmdUsageValueString")) + " - " + GetMessage("CmdUsageSet") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.get.ToString() + " " + GetMessage("CmdUsageOptionString")) + " - " + GetMessage("CmdUsageGet") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.desc.ToString() + " " + GetMessage("CmdUsageOptionString")) + " - " + GetMessage("CmdUsageDesc") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.list.ToString()) + " - " + GetMessage("CmdUsageList") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.count.ToString()) + " - " + GetMessage("CmdUsageCount") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.def.ToString()) + " - " + GetMessage("CmdUsageDef") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.version.ToString()) + " - " + GetMessage("CmdUsageVersion"));
			checkAllBarrels();
		}
		
		// Set up damage types list
		void buildDamageTypes()
		{
			damage.Clear();
			DamageTypeEntry explosionDamage = new DamageTypeEntry();
			explosionDamage.type = DamageType.Explosion;
			explosionDamage.amount = getFloat(Option.amount);
			damage.Add(explosionDamage);
		}
		
		// Load default configuration
		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadConfig();
		}
		
		// Load configuration
		private void LoadConfig()
		{
			foreach(Option opt in Enum.GetValues(typeof(Option)))
			{
				if(opt != Option.killinv)
					data[opt] = Convert.ToSingle(GetConfig(opt, def[(int)opt]));
				else
					data[opt] = Convert.ToBoolean(GetConfig(opt, def[(int)opt]));
			}
			
			if (!hasConfigChanged) return;
			SaveConfig();
			hasConfigChanged = false;
		}
		
		// Get config options, or set to default value if not found
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
		
		// Save updated entry to config
		private void SaveEntry(Option opt, object value)
		{
			string optstr = opt.ToString();
			data[opt] = value;
			Config[optstr] = value;
			SaveConfig();
		}
		
		// Check all existing barrels for rockets
		void checkAllBarrels()
		{
			foreach (LootContainer container in BaseNetworkable.serverEntities.entityList.Values.OfType<LootContainer>())
				OnEntitySpawned(container);
		}
		
		// Check newly spawned entity to check if entity is barrel containing rocket
		void OnEntitySpawned(BaseNetworkable entity)
		{
			if(entity == null) return;
			if(entity is LootContainer && entity.LookupShortPrefabName().Contains("loot-barrel"))
			{
				ExplosiveType type = ExplosiveType.Rocket;
				Item item = ((LootContainer)entity).inventory.FindItemByItemID(1578894260); // rocket
				if(debug && item != null) Puts("found rocket at " + entity.transform.position);
				if(item == null)
				{
					item = ((LootContainer)entity).inventory.FindItemByItemID(542276424); // hv rocket
					if(debug && item != null) Puts("found hv rocket at " + entity.transform.position);
				}
				if(item == null)
				{
					item = ((LootContainer)entity).inventory.FindItemByItemID(1436532208); // incendiary rocket
					type = ExplosiveType.IncendRocket;
					if(debug && item != null) Puts("found incendiary rocket at " + entity.transform.position);
				}
				// rocket found, ignore blueprints
				if(item != null && !item.IsBlueprint())
				{
					// rocket found
					barrels[entity.net.ID] = type;
				}
			}
		}
		
		// Handle entity death
		void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
		{
			barrels.Remove(entity.net.ID);
		}
		
		// Handle damage
		void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			// if entity is in barrel list, attempt to explode
			if(barrels.ContainsKey(entity.net.ID))
			{
				if(random.Next(0,100) < getFloat(Option.chance))
					explodeBarrel((LootContainer)entity, entity.transform.position);
			}
		}
		
		// Explode barrel
		void explodeBarrel(LootContainer entity, Vector3 position)
		{
			ExplosiveType type = barrels[entity.net.ID];
			if(getBool(Option.killinv))
				entity.inventory.Kill(); // destroy inventory
			barrels.Remove(entity.net.ID);
			Effect.server.Run("assets/bundled/Prefabs/fx/weapons/landmine/landmine_trigger.prefab", position); // landmine trigger click
			
			timer.Once(getFloat(Option.delay), delegate() {
				if(type == null || type == ExplosiveType.Rocket) // normal rocket explosion
				{
					Effect.server.Run("assets/prefabs/weapons/rocketLauncher/effects/rocket_explosion.prefab", position);
				}
				else if(type == ExplosiveType.IncendRocket) // incendiary rocket explosion
				{
					Effect.server.Run("Assets/Prefabs/Weapons/RocketLauncher/Effects/Rocket_Explosion_Incendiary.prefab", position);
					Effect.server.Run("assets/bundled/prefabs/fx/gas_explosion_small.prefab", position);
				}
				// damage nearby entities
				DamageUtil.RadiusDamage(entity, null, position, getFloat(Option.minradius), getFloat(Option.radius), damage, -1, false);
			});
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
				if(opt != Option.killinv)
					value = Convert.ToSingle(arg.Args[1]);
				else
					value = Convert.ToBoolean(arg.Args[1]);
			} catch(FormatException e) {
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), arg.Args[1]))));
				return false;
			}
			
			SaveEntry(opt,value);
			// if amount changed, rebuild damage types list
			if(opt == Option.amount)
				buildDamageTypes();
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
			showDesc(arg, opt, GetMessage("Desc_" + opt.ToString()));
			
			return true;
		}
		
		// prints the description of an Option
		private void showDesc(ConsoleSystem.Arg arg, Option opt, string str)
		{
			SendReply(arg, wrapSize(14, wrapColor("orange", String.Format(GetMessage("OptionDescHeader"), opt.ToString()))) + wrapSize(12, wrapColor("cyan", str)));
		}
		
		// prints the number and locations of barrels with rockets
		private void showCount(ConsoleSystem.Arg arg)
		{
			string str = "";
			foreach(uint id in barrels.Keys)
			{
				str += BaseNetworkable.serverEntities.Find(id).transform.position + "\n";
			}
			SendReply(arg, wrapSize(14, wrapColor("orange", String.Format(GetMessage("CountHeader"), barrels.Count()))) + wrapSize(12, wrapColor("cyan", str)));
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
		
		// toggles debug messages
		void toggleDebug(ConsoleSystem.Arg arg)
		{
			debug = !debug;
			SendReply(arg, wrapSize(12, wrapColor("orange", String.Format(GetMessage("ToggleDebug"), debug))));
		}
		
		// console command delegator
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
					case Command.count:
						showCount(arg);
						return;
					case Command.usage:
						showUsage(arg);
						return;
					case Command.debug:
						toggleDebug(arg);
						return;
				}
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParamForCmd"), arg.cmd.namefull))));
			}
			showUsage(arg);
		}
	}
}