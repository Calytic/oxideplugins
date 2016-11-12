using System.Collections.Generic;
using System;
using Rust;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
	[Info("ExplodingBarrels", "ignignokt84", "0.3.0", ResourceId = 1902)]
	class ExplodingBarrels : RustPlugin
	{
		// list of barrels containing explosives
		private Dictionary<uint,string> barrels;
		// random number generator
		private System.Random random;
		// data
		private ExplodingBarrelsData data = new ExplodingBarrelsData();
		// usage string
		public string usageString;
		// enum of valid commands
		private enum Command { usage, version, def, count, show };
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
				{"CmdUsageShow", "Highlights explosive barrels for the specified number of seconds"},
				{"CmdUsageDef", "Loads default configuration"},
				{"CmdUsageCount", "Count and display locations of explodable barrels"},
				{"CmdUsageVersion", "Prints version information"},
				{"CmdUsageValueString", "[value]"},
				{"CmdUsageClosestString", "[\"closest\"]"},
				{"CmdUsageTimeString", "[time]"},
				
				{"DefConfigLoad", "Loaded default configuration"},
				
				{"CountHeader", "---- Explosive Barrels: {0} ----\n"},
				{"InvalidParameter", "Invalid parameter: {0}"},
				{"InvalidParamForCmd", "Invalid parameters for command \"{0}\""},
				{"NoPermission", "Cannot execute command: No permission"},
				{"Warning_MissingEffect", "Unable to find configuration entry for effect \"{0}\""},
				{"Warning_InvalidDamageType", "No DamageType exists for \"{0}\""},
				{"Warning_InvalidDamageValue", "Invalid damage value for \"{0}\": {1}"}
			};
			lang.RegisterMessages(messages, this);
		}
		
        // get message from Lang
        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
		
		// Initialize plugin
		void Init()
		{
			barrels = new Dictionary<uint,string>();
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
						  wrapSize(12, wrapColor("cyan", baseCommand + "." + Command.count.ToString()) + " - " + GetMessage("CmdUsageCount") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.show.ToString()) + " " + GetMessage("CmdUsageClosestString") + " " + GetMessage("CmdUsageTimeString") + " - " + GetMessage("CmdUsageShow") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.def.ToString()) + " - " + GetMessage("CmdUsageDef") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.version.ToString()) + " - " + GetMessage("CmdUsageVersion"));
			checkAllBarrels();
		}
		
		// Load default configuration
		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			DefaultItemMappings();
			DefaultEffects();
			SaveData();
		}
		
		void SaveData()
		{
			Config.WriteObject(data);
		}
		
		// Load configuration
		private void LoadConfig()
		{
			Config.Settings.NullValueHandling = NullValueHandling.Include;
			try {
				data = Config.ReadObject<ExplodingBarrelsData>();
			} catch (Exception e) {
				data = new ExplodingBarrelsData();
			};
			
			bool dirty = false;
			
			if(data.itemMappings == null || data.itemMappings.Count == 0)
				dirty = DefaultItemMappings();
			if(data.barrelEffects == null || data.barrelEffects.Count == 0)
				dirty = DefaultEffects();
			
			if(dirty)
				SaveData();
			
			foreach(string effectName in data.itemMappings.Values)
				if(!data.barrelEffects.ContainsKey(effectName))
					PrintWarning(String.Format(GetMessage("Warning_MissingEffect"), effectName));
		}
		
		bool DefaultItemMappings()
		{
			data.itemMappings.Clear();
			data.itemMappings[1974032895] = "PropaneExplosion"; // propane tank id
			return true;
		}
		
		bool DefaultEffects()
		{
			BarrelEffect eff = new BarrelEffect();
			eff.name = "PropaneExplosion";
			eff.damageMap["Blunt"] = 50f;
			eff.damageMap["Heat"] = 25f;
			eff.damageMap["Explosion"] = 25f;
			eff.minRadius = 0f;
			eff.maxRadius = 10f;
			eff.delay = 3.5f;
			eff.chance = 15f;
			eff.effects.Add("assets/prefabs/weapons/rocketlauncher/effects/rocket_explosion_incendiary.prefab");
			eff.effects.Add("assets/bundled/prefabs/fx/gas_explosion_small.prefab");
			eff.triggerEffect = "assets/bundled/Prefabs/fx/weapons/landmine/landmine_trigger.prefab";
			eff.killInventory = true;
			data.barrelEffects[eff.name] = eff;
			
			return true;
		}
		
		// Check all existing barrels for rockets
		void checkAllBarrels()
		{
			foreach (LootContainer container in GameObject.FindObjectsOfType<LootContainer>())
				OnEntitySpawned(container);
		}
		
		// Check newly spawned entity to check if entity is barrel containing rocket
		void OnEntitySpawned(BaseNetworkable entity)
		{
			if(entity == null) return;
			if(entity is LootContainer && entity.ShortPrefabName.Contains("loot-barrel"))
			{
				foreach(int id in data.itemMappings.Keys)
				{
					Item item = ((LootContainer)entity).inventory.FindItemByItemID(id);

					// explosive item found
					if(item != null)
					{
						barrels[entity.net.ID] = data.itemMappings[item.info.itemid];
						break;
					}
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
				if(random.Next(0,100) < data.barrelEffects[barrels[entity.net.ID]].chance)
					explodeBarrel((LootContainer)entity, entity.transform.position);
			}
		}
		
		// Explode barrel
		void explodeBarrel(LootContainer entity, Vector3 position)
		{
			BarrelEffect effect = data.barrelEffects[barrels[entity.net.ID]];
			if(effect.killInventory)
				entity.inventory.Kill(); // destroy inventory
			barrels.Remove(entity.net.ID);
			Effect.server.Run(effect.triggerEffect, position);
			
			timer.Once(effect.delay, delegate() {
				// run effects
				foreach(object eff in effect.effects)
					Effect.server.Run(eff.ToString(), position);
				// damage nearby entities
				doDamage(entity, position, effect);
			});
		}
		
		void doDamage(BaseCombatEntity entity, Vector3 position, BarrelEffect effect)
		{
			List<DamageTypeEntry> damage = new List<DamageTypeEntry>();
			foreach(KeyValuePair<string,float> entry in effect.damageMap)
			{
				if(!Enum.IsDefined(typeof(DamageType), entry.Key))
				{
					PrintWarning(String.Format(GetMessage("Warning_InvalidDamageType"), entry.Key));
					continue;
				}
				if(entry.Value == null || entry.Value < 0.0f)
				{
					PrintWarning(String.Format(GetMessage("Warning_InvalidDamageValue"), new object[] {entry.Key, entry.Value}));
					continue;
				}
				DamageTypeEntry d = new DamageTypeEntry();
				d.type = (DamageType) Enum.Parse(typeof(DamageType), entry.Key);
				d.amount = entry.Value;
				damage.Add(d);
			}
			
			DamageUtil.RadiusDamage(entity, null, position, effect.minRadius, effect.maxRadius, damage, -1, false);
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
					case Command.count:
						showCount(arg);
						return;
					case Command.show:
						highlightBarrels(arg);
						return;
					case Command.usage:
						showUsage(arg);
						return;
				}
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParamForCmd"), arg.cmd.namefull))));
			}
			showUsage(arg);
		}
		
		// configuration data
		private class ExplodingBarrelsData {
			// item id to string mapping
			public Dictionary<int,string> itemMappings = new Dictionary<int,string>();
			// effects data
			public Dictionary<string,BarrelEffect> barrelEffects = new Dictionary<string,BarrelEffect>();
		}
		
		// effect container
		public class BarrelEffect {
			public string name;
			public Dictionary<string,float> damageMap = new Dictionary<string,float>();
			public float minRadius;
			public float maxRadius;
			public float delay;
			public float chance;
			public List<object> effects = new List<object>();
			public string triggerEffect;
			public bool killInventory = false;
		}
	}
}