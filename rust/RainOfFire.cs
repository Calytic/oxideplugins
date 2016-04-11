using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;

using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("Rain of Fire", "emu", "0.1.3", ResourceId = 1249)]
    public class RainOfFire : RustPlugin
    {
		[PluginReference]
		Plugin PopupNotifications;
	
		const string s_Incoming = "Meteor shower incoming";
	
		#region Config
		private Setting extreme = new Setting(100f, 20000f, 20f);
		private Setting optimal = new Setting(300f, 180000f, 120f);
		private Setting mild = new Setting(500f, 290000f, 240f);

		private ItemDrop[] drops = new ItemDrop[]
		{
			new ItemDrop("metal.fragments", 25, 50),
			new ItemDrop("stones", 80, 120)
		};
		
		private Timer randomTimer = null;
		private float eventIntervals = 1800f;
		private float safeIntervals = 240f;
		private bool notifyEvent = false;
		
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["EventIntervals"] = eventIntervals;
            Config["ItemDropMultiplier"] = ItemDrop.dropMultiplier;
            Config["DamageMultiplier"] = damageModifier;
            Config["NotifyEvent"] = notifyEvent;
            SaveConfig();
        }
		
		private void LoadConfig()
		{
			eventIntervals = GetConfig<float>("EventIntervals", eventIntervals);
			ItemDrop.dropMultiplier = GetConfig<float>("ItemDropMultiplier", ItemDrop.dropMultiplier);
			damageModifier = GetConfig<float>("DamageMultiplier", damageModifier);
			notifyEvent = GetConfig<bool>("NotifyEvent", notifyEvent);
		}
		
        T GetConfig<T>(string key, T defaultValue) {
            try {
                var val = Config[key];
                if (val == null)
                    return defaultValue;
                if (val is List<object>) {
                    var t = typeof(T).GetGenericArguments()[0];
                    if (t == typeof(String)) {
                        var cval = new List<string>();
                        foreach (var v in val as List<object>)
                            cval.Add((string)v);
                        val = cval;
                    } else if (t == typeof(int)) {
                        var cval = new List<int>();
                        foreach (var v in val as List<object>)
                            cval.Add(Convert.ToInt32(v));
                        val = cval;
                    }
                } else if (val is Dictionary<string, object>) {
                    var t = typeof(T).GetGenericArguments()[1];
                    if (t == typeof(int)) {
                        var cval = new Dictionary<string,int>();
                        foreach (var v in val as Dictionary<string, object>)
                            cval.Add(Convert.ToString(v.Key), Convert.ToInt32(v.Value));
                        val = cval;
                    }
                }
                return (T)Convert.ChangeType(val, typeof(T));
            } catch (Exception ex) {
                return defaultValue;
            }
        }
		#endregion
		
		private float barrageDelay = 0.33f;
		private float barrageSpread = 16f;
		
		private float launchHeight = 200f;
		private float fireRocketChance = 0.05f;
		private float launchStraightness = 2.0f;
	
		private float projectileSpeed = 20f;
		private float gravityModifier = 0f;
		private float damageModifier = 0.2f;
		private float detonationTime = 20f;
	
		private void StartRandomOnMap()
		{
			float mapsize = MapSize() - 600f;
		
			float randomX = UnityEngine.Random.Range(-mapsize, mapsize);
			float randomY = UnityEngine.Random.Range(-mapsize, mapsize);
			
			Vector3 callAt = new Vector3(randomX, 0f, randomY);
			
			StartRainOfFire(callAt, optimal);
		}
		
        [ConsoleCommand("rof.random")]
        private void ConEventRandom(ConsoleSystem.Arg arg)
        {
			if(!arg.isAdmin)
				return;
			
			StartRandomOnMap();
			Puts("Random event started");
        }
		
        [ConsoleCommand("rof.onposition")]
        private void ConEventOnPosition(ConsoleSystem.Arg arg)
        {
			if(!arg.isAdmin)
				return;
			
			float x, y ,z;
			
			if(arg.Args.Length == 3 && float.TryParse(arg.Args[0], out x) && float.TryParse(arg.Args[1], out y) && float.TryParse(arg.Args[2], out z))
			{
				StartRainOfFire(new Vector3(x, y, z), optimal);
				Puts("Random event started on position (" + x + ", " + y + ", " + z + ")");
			}
			else
				Puts("Usage: rof.onposition x y z");
        }
		
		private bool StartOnPlayer(string playerName, Setting setting)
		{
			BasePlayer player = GetPlayerByName(playerName);
			
			if(player == null)
				return false;
			
			StartRainOfFire(player.transform.position, setting);
			return true;
		}
		
		private BasePlayer GetPlayerByName(string name)
		{
			string currentName;
			string lastName;
			BasePlayer foundPlayer = null;
			name = name.ToLower();
		
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			{
				currentName = player.displayName.ToLower();
				
				if(currentName.Contains(name))
				{
					if(foundPlayer != null)
					{
						lastName = foundPlayer.displayName;
						if(currentName.Replace(name, "").Length < lastName.Replace(name, "").Length)
						{
							foundPlayer = player;
						}
					}
					
					foundPlayer = player;
				}
			}
		
			return foundPlayer;
		}
		
        [ChatCommand("rof")]
        private void AdminEvent(BasePlayer player, string command, string[] args)
        {
			if(!player.IsAdmin() || args.Length == 0)
				return;
				
			switch(args[0])
			{
				case "onplayer":
					if(args.Length == 2)
					{
						if(StartOnPlayer(args[1], optimal))
							player.ChatMessage("Event called on " + args[1] + "'s position");
						else
							player.ChatMessage("No player found with that name");
					}
					else
					{
						StartRainOfFire(player.transform.position, optimal);
						player.ChatMessage("Event called on your position");
					}
					break;
					
				case "onplayer_extreme":
					if(args.Length == 2)
					{
						if(StartOnPlayer(args[1], extreme))
							player.ChatMessage("Extreme event called on " + args[1] + "'s position");
						else
							player.ChatMessage("No player found with that name");
					}
					else
					{
						StartRainOfFire(player.transform.position, extreme);
						player.ChatMessage("Extreme event called on your position");
					}
					break;
					
				case "onplayer_mild":
					if(args.Length == 2)
					{
						if(StartOnPlayer(args[1], mild))
							player.ChatMessage("Mild event called on " + args[1] + "'s position");
						else
							player.ChatMessage("No player found with that name");
					}
					else
					{
						StartRainOfFire(player.transform.position, mild);
						player.ChatMessage("Mild event called on your position");
					}
					break;
					
				case "barrage":
					StartBarrage(player.eyes.position + player.eyes.HeadForward() * 1f, player.eyes.HeadForward(), 20);
					break;
				
				case "random":
					StartRandomOnMap();
					player.ChatMessage("Event called on random position");
					break;
					
				case "intervals":
					if(args.Length > 1)
					{
						float newIntervals;
						bool isValid;
						isValid = float.TryParse(args[1], out newIntervals);
						
						if(isValid)
						{
							if(newIntervals >= safeIntervals || newIntervals == 0f)
							{
								SetRandomIntervals(newIntervals);
								player.ChatMessage("Event intervals set to " + newIntervals);
								StopRandomTimer();
								StartRandomTimer();
							}
							else
							{
								player.ChatMessage("Event intervals under " + safeIntervals + " are not allowed");
							}
						}
						else
						{
							player.ChatMessage("Invalid parameter '" + args[1] + "'");
						}
					}
					break;
					
				case "droprate":
					if(args.Length > 1)
					{
						float newDropMultiplier;
						bool isValid;
						isValid = float.TryParse(args[1], out newDropMultiplier);
						
						
						if(isValid)
						{
							SetDropRate(newDropMultiplier);
							player.ChatMessage("Drop multiplier set to " + newDropMultiplier);
						}
						else
						{
							player.ChatMessage("Invalid parameter '" + args[1] + "'");
						}
					}
					break;
					
				case "damagescale":
					if(args.Length > 1)
					{
						float newDamageMultiplier;
						bool isValid;
						isValid = float.TryParse(args[1], out newDamageMultiplier);
						
						
						if(isValid)
						{
							SetDamageMult(newDamageMultiplier);
							player.ChatMessage("Damage scale set to " + newDamageMultiplier);
						}
						else
						{
							player.ChatMessage("Invalid parameter '" + args[1] + "'");
						}
					}
					break;
					
				case "togglemsg":
					SetNotifyEvent(!notifyEvent);
					player.ChatMessage("Event notification set to " + notifyEvent);
					break;
				
				default:
					player.ChatMessage("Unknown parameter '" + args[0] + "'");
					break;
			}
        }
		
		private void SetRandomIntervals(float intervals)
		{
			StopRandomTimer();
			
			eventIntervals = intervals;
            Config["EventIntervals"] = eventIntervals;
            SaveConfig();
			LoadConfig();
			
			StartRandomTimer();
		}
		
		private void SetDropRate(float rate)
		{
			ItemDrop.dropMultiplier = rate;
            Config["ItemDropMultiplier"] = ItemDrop.dropMultiplier;
            SaveConfig();
			LoadConfig();
		}
		
		private void SetDamageMult(float scale)
		{
			damageModifier = scale;
            Config["DamageMultiplier"] = damageModifier;
            SaveConfig();
			LoadConfig();
		}
		
		private void SetNotifyEvent(bool notify)
		{
			notifyEvent = notify;
            Config["NotifyEvent"] = notifyEvent;
            SaveConfig();
			LoadConfig();
		}
		
		private void StartRandomTimer()
		{
			if(eventIntervals > 0f)
				randomTimer = timer.Repeat(eventIntervals, 0, () => StartRandomOnMap());
		}
		
		private void StopRandomTimer()
		{
			if(randomTimer != null)
				randomTimer.Destroy();
			randomTimer = null;
		}
		
		
		private void OnServerInitialized()
		{
			LoadConfig();
			StartRandomTimer();
		}
		
		private float MapSize()
		{
			return TerrainMeta.Size.x / 2;
		}
		
		private float CircleArea(float radius)
		{
			return radius * radius * 3.14f;
		}
		
		private void StartBarrage(Vector3 origin, Vector3 direction, int numberOfRockets)
		{
			timer.Repeat(barrageDelay, numberOfRockets, () => SpreadRocket(origin, direction));
		}
		
		private void StartRainOfFire(Vector3 origin, float radius, float rocketDensity, float duration, bool dropsItems)
		{
			int numberOfRockets = (int)(CircleArea(radius) * duration / rocketDensity);
			float intervals = duration / numberOfRockets;
			
			if(notifyEvent)
			{
				if(PopupNotifications)
					PopupNotifications.Call("CreatePopupNotification", s_Incoming);
				else
					PrintToChat(s_Incoming);
			}
			
			timer.Repeat(intervals, numberOfRockets, () => RandomRocket(origin, radius, dropsItems));
		}
		
		private void StartRainOfFire(Vector3 origin, Setting setting)
		{
			float radius = setting.Radius;
			float rocketDensity = setting.RocketDensity;
			float duration = setting.Duration;
			bool dropsItems = setting.DropsItems;
			
			StartRainOfFire(origin, radius, rocketDensity, duration, dropsItems);
		}
		
		private void RandomRocket(Vector3 origin, float radius, bool dropsItems = true)
		{
			bool isFireRocket = false;
			Vector2 rand = UnityEngine.Random.insideUnitCircle;
			Vector3 offset = new Vector3(rand.x * radius, 0, rand.y * radius);
		
			Vector3 direction = (Vector3.up * -launchStraightness + Vector3.right).normalized;
			Vector3 launchPos = origin + offset - direction * launchHeight;
			
			if(UnityEngine.Random.value < fireRocketChance)
				isFireRocket = true;
			
			BaseEntity rocket = CreateRocket(launchPos, direction, isFireRocket);
			if(dropsItems)
				rocket.gameObject.AddComponent<ItemCarrier>().SetCarriedItems(drops);
		}
		
		private void SpreadRocket(Vector3 origin, Vector3 direction)
		{
			direction = Quaternion.Euler(UnityEngine.Random.Range((float) (-(double) barrageSpread * 0.5), barrageSpread * 0.5f), UnityEngine.Random.Range((float) (-(double) barrageSpread * 0.5), barrageSpread * 0.5f), UnityEngine.Random.Range((float) (-(double) barrageSpread * 0.5), barrageSpread * 0.5f)) * direction;
			CreateRocket(origin, direction, false);
		}
		
		private BaseEntity CreateRocket(Vector3 startPoint, Vector3 direction, bool isFireRocket)
		{
			ItemDefinition projectileItem;
			
			if(isFireRocket)
				projectileItem = GetFireRocket();
			else
				projectileItem = GetRocket();
		
			ItemModProjectile component = projectileItem.GetComponent<ItemModProjectile>();
			BaseEntity entity = GameManager.server.CreateEntity(component.projectileObject.resourcePath, startPoint, new Quaternion(), true);
			
			TimedExplosive timedExplosive = entity.GetComponent<TimedExplosive>();
			ServerProjectile serverProjectile = entity.GetComponent<ServerProjectile>();
			
			serverProjectile.gravityModifier = gravityModifier;
			serverProjectile.speed = projectileSpeed;
			timedExplosive.timerAmountMin = detonationTime;
			timedExplosive.timerAmountMax = detonationTime;
			ScaleAllDamage(timedExplosive.damageTypes, damageModifier); 
			
			entity.SendMessage("InitializeVelocity", (object) (direction * 1f));
			entity.Spawn(true);
			
			return entity;
		}
		
		private void ScaleAllDamage(List<DamageTypeEntry> damageTypes, float scale)
		{
			for(int i = 0; i < damageTypes.Count; i++)
			{
				damageTypes[i].amount *= scale;
			}
		}
		
		private ItemDefinition GetRocket()
		{
			return ItemManager.FindItemDefinition("ammo.rocket.basic");
		}
		
		private ItemDefinition GetFireRocket()
		{
			return ItemManager.FindItemDefinition("ammo.rocket.fire");
		}
		
		class Setting
		{
			private float radius;
			private float rocketDensity;
			private float duration;
			private bool dropsItems;
			
			public Setting(float radius, float rocketDensity, float duration, bool dropsItems = true)
			{
				this.radius = radius;
				this.rocketDensity = rocketDensity;
				this.duration = duration;
				this.dropsItems = dropsItems;
			}
			
			public float Radius
			{
				get { return radius; }
			}
			public float RocketDensity
			{
				get { return rocketDensity; }
			}
			public float Duration
			{
				get { return duration; }
			}
			public bool DropsItems
			{
				get { return dropsItems; }
			}
		}
		
		class ItemCarrier : MonoBehaviour
		{
			private ItemDrop[] carriedItems = null;
		
			public void SetCarriedItems(ItemDrop[] carriedItems)
			{
				this.carriedItems = carriedItems;
			}
			
			private void OnDestroy()
			{
				DropItems();
			}
			
			private void DropItems()
			{
				if(carriedItems == null)
					return;
					
				int amount;
					
				for(int i = 0; i < carriedItems.Length; i++)
				{
					if((amount = carriedItems[i].GetRandomAmount()) > 0)
						ItemManager.CreateByName(carriedItems[i].GetItemName(), amount).Drop(gameObject.transform.position, Vector3.up);
				}
			}
		}
		
		class ItemDrop
		{
			public static float dropMultiplier = 1f;
			
			private string itemName;
			private int minAmount;
			private int maxAmount;
			
			public ItemDrop(string itemName, int minAmount, int maxAmount)
			{
				this.itemName = itemName;
				this.minAmount = minAmount;
				this.maxAmount = maxAmount;
			}
			
			public string GetItemName()
			{
				return itemName;
			}
			
			public int GetRandomAmount()
			{
				return (int)(UnityEngine.Random.Range(minAmount, maxAmount) * dropMultiplier);
			}
		}
    }
}
