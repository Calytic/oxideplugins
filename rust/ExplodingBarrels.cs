using System.Collections.Generic;
using System;
using Rust;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("ExplodingBarrels", "ignignokt84", "0.2.3", ResourceId = 1902)]
	class ExplodingBarrels : RustPlugin
	{
		// list of barrels containing explosives
		private Dictionary<uint,ExplosiveType> barrels;
		// random number generator
		private System.Random random;
		// explosive types for effects
		private enum ExplosiveType { Rocket, IncendiaryRocket, F1Grenade, BeancanGrenade, Landmine };
		
		// debug toggle
		private bool debug = false;
		// configuration data
		private Dictionary<object,object> data = new Dictionary<object,object>();
		// usage string
		public string usageString;
		// configuration changed flag
		private bool hasConfigChanged;
		// enum of valid commands
		private enum Command { usage, set, get, desc, list, version, def, count, debug, show };
		// enum of valid options
		private enum Option { killinv };
		// default values
		private object[] def = { true };
		// default highlight time = 10 seconds
		private float defaultHighlightTime = 10f;
		
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
				{"CmdUsageShow", "Highlights explosive barrels for the specified number of seconds"},
				{"CmdUsageDef", "Loads default configuration"},
				{"CmdUsageCount", "Count and display locations of explodable barrels"},
				{"CmdUsageVersion", "Prints version information"},
				{"CmdUsageOptionString", "[option]"},
				{"CmdUsageValueString", "[value]"},
				{"CmdUsageClosestString", "[\"closest\"]"},
				{"CmdUsageTimeString", "[time]"},
				
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
			// build usage string
			usageString = wrapSize(14, wrapColor("orange", GetMessage("UsageHeader"))) + "\n" +
						  wrapSize(12, wrapColor("cyan", baseCommand + "." + Command.set.ToString() + " " + GetMessage("CmdUsageOptionString") + " " + GetMessage("CmdUsageValueString")) + " - " + GetMessage("CmdUsageSet") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.get.ToString() + " " + GetMessage("CmdUsageOptionString")) + " - " + GetMessage("CmdUsageGet") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.desc.ToString() + " " + GetMessage("CmdUsageOptionString")) + " - " + GetMessage("CmdUsageDesc") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.list.ToString()) + " - " + GetMessage("CmdUsageList") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.count.ToString()) + " - " + GetMessage("CmdUsageCount") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.show.ToString()) + " " + GetMessage("CmdUsageClosestString") + " " + GetMessage("CmdUsageTimeString") + " - " + GetMessage("CmdUsageShow") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.def.ToString()) + " - " + GetMessage("CmdUsageDef") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.version.ToString()) + " - " + GetMessage("CmdUsageVersion"));
			checkAllBarrels();
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
			// Beancan Grenade config
			data["BeancanGrenade_BluntDamage"] = GetConfigValue("BeancanGrenade", "BluntDamage", 50f);
			data["BeancanGrenade_ExplosiveDamage"] = GetConfigValue("BeancanGrenade", "ExplosiveDamage", 15f);
			data["BeancanGrenade_StabDamage"] = GetConfigValue("BeancanGrenade", "StabDamage", 50f);
			data["BeancanGrenade_MinRadius"] = GetConfigValue("BeancanGrenade", "MinRadius", 1.5f);
			data["BeancanGrenade_Radius"] = GetConfigValue("BeancanGrenade", "Radius", 4.5f);
			data["BeancanGrenade_Delay"] = GetConfigValue("BeancanGrenade", "Delay", 3.5f);
			data["BeancanGrenade_Scale"] = GetConfigValue("BeancanGrenade", "Scale", 1f);
			data["BeancanGrenade_Chance"] = GetConfigValue("BeancanGrenade", "Chance", 50f);
			data["BeancanGrenade_Effects"] = GetConfigValue("BeancanGrenade", "Effects", new List<object>() {"assets/prefabs/weapons/beancan grenade/effects/beancan_grenade_explosion.prefab"});
			data["BeancanGrenade_Trigger"] = GetConfigValue("BeancanGrenade", "Trigger", "assets/prefabs/weapons/f1 grenade/effects/pullpin.prefab");
			
			// F1 Grenade config
			data["F1Grenade_BluntDamage"] = GetConfigValue("F1Grenade", "BluntDamage", 50f);
			data["F1Grenade_ExplosiveDamage"] = GetConfigValue("F1Grenade", "ExplosiveDamage", 40f);
			data["F1Grenade_StabDamage"] = GetConfigValue("F1Grenade", "StabDamage", 50f);
			data["F1Grenade_MinRadius"] = GetConfigValue("F1Grenade", "MinRadius", 1.5f);
			data["F1Grenade_Radius"] = GetConfigValue("F1Grenade", "Radius", 4.5f);
			data["F1Grenade_Delay"] = GetConfigValue("F1Grenade", "Delay", 3.5f);
			data["F1Grenade_Scale"] = GetConfigValue("F1Grenade", "Scale", 1f);
			data["F1Grenade_Chance"] = GetConfigValue("F1Grenade", "Chance", 50f);
			data["F1Grenade_Effects"] = GetConfigValue("F1Grenade", "Effects", new List<object>() {"assets/prefabs/weapons/f1 grenade/effects/f1grenade_explosion.prefab"});
			data["F1Grenade_Trigger"] = GetConfigValue("F1Grenade", "Trigger", "assets/prefabs/weapons/f1 grenade/effects/pullpin.prefab");
			
			// Rocket config
			data["Rocket_BluntDamage"] = GetConfigValue("Rocket", "BluntDamage", 75f);
			data["Rocket_ExplosiveDamage"] = GetConfigValue("Rocket", "ExplosiveDamage", 275f);
			data["Rocket_MinRadius"] = GetConfigValue("Rocket", "MinRadius", 1f);
			data["Rocket_Radius"] = GetConfigValue("Rocket", "Radius", 3.8f);
			data["Rocket_Delay"] = GetConfigValue("Rocket", "Delay", 8f);
			data["Rocket_Scale"] = GetConfigValue("Rocket", "Scale", 1f);
			data["Rocket_Chance"] = GetConfigValue("Rocket", "Chance", 50f);
			data["Rocket_Effects"] = GetConfigValue("Rocket", "Effects", new List<object>() {"assets/prefabs/weapons/rocketLauncher/effects/rocket_explosion.prefab"});
			data["Rocket_Trigger"] = GetConfigValue("Rocket", "Trigger", "assets/bundled/Prefabs/fx/weapons/landmine/landmine_trigger.prefab");
			
			// Incendiary Rocket config
			data["IncendiaryRocket_HeatDamage"] = GetConfigValue("IncendiaryRocket", "HeatDamage", 25f);
			data["IncendiaryRocket_MinRadius"] = GetConfigValue("IncendiaryRocket", "MinRadius", 5f);
			data["IncendiaryRocket_Radius"] = GetConfigValue("IncendiaryRocket", "Radius", 5f);
			data["IncendiaryRocket_Delay"] = GetConfigValue("IncendiaryRocket", "Delay", 8f);
			data["IncendiaryRocket_Scale"] = GetConfigValue("IncendiaryRocket", "Scale", 1f);
			data["IncendiaryRocket_Chance"] = GetConfigValue("IncendiaryRocket", "Chance", 50f);
			data["IncendiaryRocket_Effects"] = GetConfigValue("IncendiaryRocket", "Effects", new List<object>()
				{"assets/prefabs/weapons/rocketlauncher/effects/rocket_explosion_incendiary.prefab", "assets/bundled/prefabs/fx/gas_explosion_small.prefab"});
			data["IncendiaryRocket_Trigger"] = GetConfigValue("IncendiaryRocket", "Trigger", "assets/bundled/Prefabs/fx/weapons/landmine/landmine_trigger.prefab");
			
			// HV Rocket config
			data["HVRocket_BluntDamage"] = GetConfigValue("HVRocket", "BluntDamage", 75f);
			data["HVRocket_ExplosiveDamage"] = GetConfigValue("HVRocket", "ExplosiveDamage", 150f);
			data["HVRocket_MinRadius"] = GetConfigValue("HVRocket", "MinRadius", 0f);
			data["HVRocket_Radius"] = GetConfigValue("HVRocket", "Radius", 3f);
			data["HVRocket_Delay"] = GetConfigValue("HVRocket", "Delay", 8f);
			data["HVRocket_Scale"] = GetConfigValue("HVRocket", "Scale", 1f);
			data["HVRocket_Chance"] = GetConfigValue("HVRocket", "Chance", 50f);
			data["HVRocket_Effects"] = GetConfigValue("HVRocket", "Effects", new List<object>() {"assets/prefabs/weapons/rocketLauncher/effects/rocket_explosion.prefab"});
			data["HVRocket_Trigger"] = GetConfigValue("HVRocket", "Trigger", "assets/bundled/Prefabs/fx/weapons/landmine/landmine_trigger.prefab");
			
			// Landmine config
			data["Landmine_BluntDamage"] = GetConfigValue("Landmine", "BluntDamage", 100f);
			data["Landmine_ExplosiveDamage"] = GetConfigValue("Landmine", "ExplosiveDamage", 100f);
			data["Landmine_MinRadius"] = GetConfigValue("Landmine", "MinRadius", 0f);
			data["Landmine_Radius"] = GetConfigValue("Landmine", "Radius", 3f);
			data["Landmine_Delay"] = GetConfigValue("Landmine", "Delay", 2f);
			data["Landmine_Scale"] = GetConfigValue("Landmine", "Scale", 1f);
			data["Landmine_Chance"] = GetConfigValue("Landmine", "Chance", 50f);
			data["Landmine_Effects"] = GetConfigValue("Landmine", "Effects", new List<object>() {"assets/bundled/prefabs/fx/weapons/landmine/landmine_explosion.prefab"});
			data["Landmine_Trigger"] = GetConfigValue("Landmine", "Trigger", "assets/bundled/Prefabs/fx/weapons/landmine/landmine_trigger.prefab");
			
			foreach(Option opt in Enum.GetValues(typeof(Option)))
			{
				if(opt == Option.killinv)
					data[opt.ToString()] = Convert.ToBoolean(GetConfigValue("Options", opt.ToString(), def[(int)opt]));
				else
					data[opt.ToString()] = Convert.ToSingle(GetConfigValue("Options", opt.ToString(), def[(int)opt]));
			}
			
			if (!hasConfigChanged) return;
			SaveConfig();
			hasConfigChanged = false;
		}
		
		// Get Configuration value
        private T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            var cfg = Config[category] as Dictionary<string, object>;
            object value;
            if (cfg == null)
            {
                cfg = new Dictionary<string, object>();
                Config[category] = cfg;
                hasConfigChanged = true;
            }
            if (cfg.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            cfg[setting] = value;
            hasConfigChanged = true;
            return (T)Convert.ChangeType(value, typeof(T));
        }
		
		// Set Configuration value
        private void SetConfigValue<T>(string category, string setting, T newValue)
        {
            var cfg = Config[category] as Dictionary<string, object>;
            object value;
            if (cfg != null && cfg.TryGetValue(setting, out value))
            {
                value = newValue;
                cfg[setting] = value;
                hasConfigChanged = true;
            }
            SaveConfig();
        }
		
		// Save updated entry to config
		private void SaveEntry(object opt, object value)
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
			if(entity is LootContainer && entity.ShortPrefabName.Contains("loot-barrel"))
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
					type = ExplosiveType.IncendiaryRocket;
					if(debug && item != null) Puts("found incendiary rocket at " + entity.transform.position);
				}
				if(item == null)
				{
					item = ((LootContainer)entity).inventory.FindItemByItemID(-1308622549); // f1 grenade
					type = ExplosiveType.F1Grenade;
					if(debug && item != null) Puts("found f1 grenade at " + entity.transform.position);
				}
				if(item == null)
				{
					item = ((LootContainer)entity).inventory.FindItemByItemID(384204160); // beancan grenade
					type = ExplosiveType.BeancanGrenade;
					if(debug && item != null) Puts("found beancan grenade at " + entity.transform.position);
				}
				if(item == null)
				{
					item = ((LootContainer)entity).inventory.FindItemByItemID(255101535); // landmine
					type = ExplosiveType.Landmine;
					if(debug && item != null) Puts("found landmine at " + entity.transform.position);
				}
				// condition for testing
				if(debug && item == null)
				{
					// test items
					item = ((LootContainer)entity).inventory.FindItemByItemID(1351589500); // blueprint fragment
					type = ExplosiveType.IncendiaryRocket;
					if(debug && item != null) Puts("found test entity at " + entity.transform.position);
				}
				// rocket found
				if(item != null)
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
				if(random.Next(0,100) < getFloat(barrels[entity.net.ID].ToString() + "_Chance"))
					explodeBarrel((LootContainer)entity, entity.transform.position);
			}
		}
		
		// Explode barrel
		void explodeBarrel(LootContainer entity, Vector3 position)
		{
			ExplosiveType type = barrels[entity.net.ID];
			if(getBool(Option.killinv.ToString()))
				entity.inventory.Kill(); // destroy inventory
			barrels.Remove(entity.net.ID);
			Effect.server.Run(getString(type.ToString() + "_Trigger"), position);
			//Effect.server.Run("assets/prefabs/weapons/f1 grenade/effects/pullpin.prefab", position);
			
			timer.Once(getFloat(type.ToString() + "_Delay"), delegate() {
				// run effects
				foreach(object effect in getList(type.ToString() + "_Effects"))
					Effect.server.Run(effect.ToString(), position);
				// damage nearby entities
				doDamage(entity, position, type);
			});
		}
		
		void doDamage(BaseCombatEntity entity, Vector3 position, ExplosiveType type)
		{
			List<DamageTypeEntry> damage = new List<DamageTypeEntry>();
			DamageTypeEntry bluntDamage = new DamageTypeEntry();
			bluntDamage.type = DamageType.Blunt;
			DamageTypeEntry explosionDamage = new DamageTypeEntry();
			explosionDamage.type = DamageType.Explosion;
			DamageTypeEntry stabDamage = new DamageTypeEntry();
			stabDamage.type = DamageType.Stab;
			DamageTypeEntry heatDamage = new DamageTypeEntry();
			heatDamage.type = DamageType.Heat;
			
			float scale = getFloat(type.ToString() + "_Scale");
			float blunt = getFloatOrZero(type.ToString() + "_BluntDamage");
			float explosion = getFloatOrZero(type.ToString() + "_ExplosionDamage");
			float stab = getFloatOrZero(type.ToString() + "_StabDamage");
			float heat = getFloatOrZero(type.ToString() + "_HeatDamage");
			
			bluntDamage.amount = blunt * scale;
			explosionDamage.amount = explosion * scale;
			stabDamage.amount = stab * scale;
			heatDamage.amount = heat * scale;
			
			damage.Add(bluntDamage);
			damage.Add(explosionDamage);
			damage.Add(stabDamage);
			damage.Add(heatDamage);
			
			DamageUtil.RadiusDamage(entity, null, position, getFloat(type.ToString() + "_MinRadius"), getFloat(type.ToString() + "_Radius"), damage, -1, false);
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
				if(opt == Option.killinv)
					value = Convert.ToBoolean(arg.Args[1]);
				else
					value = Convert.ToSingle(arg.Args[1]);
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
			SendReply(arg, wrapSize(12, wrapColor("cyan", opt + ": ") + data[opt.ToString()]));
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
				str += BaseNetworkable.serverEntities.Find(id).transform.position + ": " + barrels[id].ToString() + "\n";
			}
			SendReply(arg, wrapSize(14, wrapColor("orange", String.Format(GetMessage("CountHeader"), barrels.Count()))) + wrapSize(12, wrapColor("cyan", str)));
		}
		
		// find and highlight barrels
		private void highlightBarrels(ConsoleSystem.Arg arg)
		{
			bool closest = false;
			float time = defaultHighlightTime;
			int i = 0;
			if(arg.Args != null)
			{
				if(arg.Args[i] != null && arg.Args[i++] == "closest")
					closest = true;
				
				if(arg.Args.Count() > i && arg.Args[i] != null)
					try {
						time = Convert.ToSingle(arg.Args[i]);
					} catch (FormatException) {
						SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), arg.Args[i]))));
						return;
					}
			}
			
			BasePlayer player = BasePlayer.Find(arg.connection.username);
			Vector3 closestPos = Vector3.zero;
			float distance = -1f;
			
			foreach(uint id in barrels.Keys)
			{
				Vector3 pos = BaseNetworkable.serverEntities.Find(id).transform.position;
				if(closest)
				{
					float dist = Vector3.Distance(pos, player.transform.position);
					if(distance == -1f || dist < distance)
					{
						closestPos = pos;
						distance = dist;
					}
				}
				else
					highlightPath(player, pos, time, Vector3.Distance(pos, player.transform.position).ToString("#.0"));
			}
			if(closest)
				highlightPath(player, closestPos, time, distance.ToString("#.0"));
		}
		
		// draw line from player to target
		private void highlightPath(BasePlayer player, Vector3 to, float duration, string text)
		{
			Vector3 from = player.transform.position;
			player.SendConsoleCommand("ddraw.line", duration, Color.red, from, to);
			// if text string is not null or empty, calculate text position as midpoint of line + 0.1y (to place text above line)
			if(text != null && text != "")
			{
				Vector3 textPosition = ((from + to)/2f); // midpoint of line
				textPosition = textPosition + new Vector3(0,0.1f,0); // shift text 0.1y
				player.SendConsoleCommand("ddraw.text", duration, Color.red, textPosition, text);
			}
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
		private bool getBool(string str)
		{
			return Convert.ToBoolean(data[str]);
		}
		
		// convert Option value to float
		private float getFloat(string str)
		{
			return Convert.ToSingle(data[str]);
		}
		
		private float getFloatOrZero(string str)
		{
			if(data.ContainsKey(str))
				return getFloat(str);
			return 0f;
		}
		
		private string getString(string str)
		{
			return (string) data[str];
		}
		
		private T getValue<T>(string str)
		{
			return (T)Convert.ChangeType(data[str], typeof(T));
		}
		
		private List<object> getList(string str)
		{
			return (List<object>) data[str];
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
					case Command.show:
						highlightBarrels(arg);
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