// Reference: Newtonsoft.Json
// Reference: Oxide.Ext.Rust

// #define DEBUG

using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

using UnityEngine;

using Random = UnityEngine.Random;

// Oxide requires your "main plugin" class to reside in Oxide.Plugins. It will not be able to be loaded otherwise. (Will compile fine, as it should, but the actual loader will fail
// because it can't find the plugin class) https://github.com/OxideMod/Oxide/blob/master/Oxide.Ext.CSharp/CompilablePlugin.cs#L102 is the offending code.
// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
	[Info("World Quarry", "ApocDev", "1.0.0")]
	public class WorldQuarry : RustPlugin
	{
		internal readonly List<Quarry> Quarries = new List<Quarry>();
		public DynamicConfigFile QuarryData { get; set; }

		private void Loaded()
		{
			QuarryData = Interface.GetMod().DataFileSystem.GetDatafile("WorldQuarryInstances");
		}

		[HookMethod("OnServerInitialized")]
		private void OnServerInitialized()
		{
			LoadQuarries();
		}

		[ChatCommand("quarry")]
		private void HandleQuarryCommand(BasePlayer player, string command, string[] args)
		{
			if (args.Length == 0)
			{
				player.ChatMessage("To create a quarry, place a 3x3 of boxes on the ground, without a building nearby and smack the middle box with a torch!");
				return;
			}

			switch (args[0].ToLowerInvariant())
			{
				case "clearall":
					if (player.IsAdmin())
					{
						var count = Quarries.Count;
						Quarries.Clear();
						SaveQuarries();
						player.ChatMessage(count + " quarr" + (count > 1 ? "ies" : "y") + " cleared!");
					}
					else
					{
						player.ChatMessage("You do not have permission for that command!");
					}
					break;
			}
		} // 185, 170, 134

		#region Nested type: Quarry

		internal class Quarry : IEquatable<Quarry>
		{
			[JsonIgnore]
			private static uint TickUpdateCounter = 1;

			/// <summary>
			///     Simply helps track how many ticks have passed since we updated this quarry.
			///     Default value has this spread across multiple ticks to avoid having all quarries ticked at once
			///     on server startup.
			/// </summary>
			[JsonIgnore]
			internal uint TicksSinceUpdate = unchecked(TickUpdateCounter++) % NumTicksToThrottle;

			/// <summary>
			///     Initializes a new instance of the <see cref="T:System.Object" /> class.
			/// </summary>
			public Quarry(Guid id, uint mainContainerId, List<uint> entityIds)
			{
				Id = id;
				EntityIds = entityIds;
				QuarryContainerId = mainContainerId;
			}

			public Quarry(uint mainContainerId, List<uint> entityIds)
				: this(Guid.NewGuid(), mainContainerId, entityIds)
			{
				TicksSinceUpdate = 0;
			}

			// Paramless ctor for json object creation.
			public Quarry()
			{
				EntityIds = new List<uint>();
			}

			public uint QuarryContainerId { get; set; }
			public List<uint> EntityIds { get; set; }
			public Guid Id { get; set; }

			#region IEquatable<Quarry> Members

			/// <summary>
			///     Indicates whether the current object is equal to another object of the same type.
			/// </summary>
			/// <returns>
			///     true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
			/// </returns>
			/// <param name="other">An object to compare with this object.</param>
			public bool Equals(Quarry other)
			{
				if (ReferenceEquals(null, other))
				{
					return false;
				}
				if (ReferenceEquals(this, other))
				{
					return true;
				}
				return Id.Equals(other.Id);
			}

			#endregion

			/// <summary>
			///     Returns a string that represents the current object.
			/// </summary>
			/// <returns>
			///     A string that represents the current object.
			/// </returns>
			public override string ToString()
			{
				return string.Format("Id: {0}, QuarryContainerId: {1}, EntityIds: {2}, TicksSinceUpdate: {3}",
					Id,
					QuarryContainerId,
					string.Join(", ", EntityIds.Select(i => i.ToString()).ToArray()),
					TicksSinceUpdate);
			}

			public IEnumerable<BaseEntity> GetEntities()
			{
				return EntityIds.Select(entityId => BaseNetworkable.serverEntities.Find(entityId)).OfType<BaseEntity>();
			}

			public BaseEntity GetQuarryEntity()
			{
				return BaseNetworkable.serverEntities.Find(QuarryContainerId) as BaseEntity;
			}

			/// <summary>
			///     Determines whether the specified <see cref="T:System.Object" /> is equal to the current
			///     <see cref="T:System.Object" />.
			/// </summary>
			/// <returns>
			///     true if the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />;
			///     otherwise, false.
			/// </returns>
			/// <param name="obj">The <see cref="T:System.Object" /> to compare with the current <see cref="T:System.Object" />. </param>
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
				{
					return false;
				}
				if (ReferenceEquals(this, obj))
				{
					return true;
				}
				if (obj.GetType() != GetType())
				{
					return false;
				}
				return Equals((Quarry) obj);
			}

			/// <summary>
			///     Serves as a hash function for a particular type.
			/// </summary>
			/// <returns>
			///     A hash code for the current <see cref="T:System.Object" />.
			/// </returns>
			public override int GetHashCode()
			{
				return Id.GetHashCode();
			}

			public static bool operator ==(Quarry left, Quarry right)
			{
				return Equals(left, right);
			}

			public static bool operator !=(Quarry left, Quarry right)
			{
				return !Equals(left, right);
			}
		}

		#endregion

		#region Quarry Persistence

		private void LoadQuarries()
		{
			// Note: This doesn't use Oxide's default "DynamicData/ConfigFile" API, because it's pretty restrictive on datatypes allowed
			// And causes us to cast things all over the place.
			// Instead, we'll shove some json serialized strings into the data file that we can deserialize later
			// using Json.Net's awesome serialization.

			var quarryData = QuarryData.Get<string>("Quarries");

			if (string.IsNullOrEmpty(quarryData))
			{
				Quarries.Clear();
				return;
			}
			var settings = new JsonSerializerSettings
			{
				Formatting = Formatting.None,
				DefaultValueHandling = DefaultValueHandling.Populate,
				TypeNameHandling = TypeNameHandling.Auto,
				ObjectCreationHandling = ObjectCreationHandling.Replace
			};

			// Populate the actual quarry list now please.
			JsonConvert.PopulateObject(quarryData, Quarries, settings);
		}

		private void SaveQuarries()
		{
			QuarryData.Clear();
			var settings = new JsonSerializerSettings
			{
				Formatting = Formatting.None,
				DefaultValueHandling = DefaultValueHandling.Populate,
				TypeNameHandling = TypeNameHandling.Auto
			};

			string json = JsonConvert.SerializeObject(Quarries, settings);
			QuarryData["Quarries"] = json;
			Interface.GetMod().DataFileSystem.SaveDatafile("WorldQuarryInstances");
		}

		#endregion

		#region Config

		protected override void LoadDefaultConfig()
		{
			AmountToCreatePerTick = 2;
			TicksToInclude = 0.25f;
			StoneChance = 0.25f;
			MetalChance = 0.1f;
			SulfurChance = 0.05f;
		}

		/// <summary>
		///     The minimum number of ticks before a quarry can be updated. Raise this value to alleviate some server load.
		///     Lower values will cause client-side lag while players are looking inside quarry boxes. Suggested: 10+, Default: 10
		/// </summary>
		private const byte NumTicksToThrottle = 10;

		/// <summary>
		///     The base amount of each item to create per tick. Default: 2
		/// </summary>
		private int AmountToCreatePerTick { get { return Config.Get<int>("AmountToCreatePerTick"); } set { Config["AmountToCreatePerTick"] = value; } }

		/// <summary>
		///     How many of the total ticks should be included when updating the quarry. (0.0-1.0 [0%-100%]) Default: 0.25
		/// </summary>
		private float TicksToInclude { get { return Config.Get<float>("TicksToInclude"); } set { Config["TicksToInclude"] = value; } }

		// These are just the chances of each item to be created each tick.
		private float StoneChance { get { return Config.Get<float>("StoneChance"); } set { Config["StoneChance"] = value; } }
		private float SulfurChance { get { return Config.Get<float>("SulfurChance"); } set { Config["SulfurChance"] = value; } }
		private float MetalChance { get { return Config.Get<float>("MetalChance"); } set { Config["MetalChance"] = value; } }

		#endregion

		#region Updating Quarries

		[HookMethod("OnTick")]
		// ReSharper disable once UnusedMember.Local
		private void OnTick()
		{
			// Note: This spreads the updates across multiple ticks.
			// We don't want to hammer the server with tons of quarry updates
			// when it isn't necessary.
			for (int index = Quarries.Count - 1; index >= 0; index--)
			{
				var quarry = Quarries[index];
				
				// Is this quarry ready for an update?
				// Make sure we increase TicksSinceUpdate so we're not forgetting to actually update this quarry.
				if (quarry.TicksSinceUpdate++ < NumTicksToThrottle)
				{
					continue;
				}

				// Ensure the quarry is still in a valid state.
				// TODO: Check for nearby building blocks (avoids sneaky people building the quarry, then immediately dropping floors on top of it)
				if (quarry.EntityIds == null || quarry.EntityIds.Count != 9 || quarry.GetEntities().Any(e => !e.IsValid()))
				{
					Quarries.RemoveAt(index);
					Puts("Removing quarry [" + quarry + "] because it is missing required nearby entities.");
					SaveQuarries();
					continue;
				}

				if (quarry.GetQuarryEntity() != null)
				{
					var quarryBox = quarry.GetQuarryEntity();
					var container = quarryBox.GetComponent<StorageContainer>();

					if (container != null)
					{
						int numStones = 0;
						int numMetal = 0;
						int numSulfur = 0;

						// Figure out the totals for each tick
						// The only reason we're throttling ticks is because of the inventory update actually lagging
						// the client-side of things when the inventory is open, and being updated.
						for (int i = 0; i < (int) Math.Floor(quarry.TicksSinceUpdate * TicksToInclude); i++)
						{
							float createChance = Random.Range(0f, 1f);

							if (createChance < StoneChance)
							{
								numStones += AmountToCreatePerTick;
							}
							if (createChance < MetalChance)
							{
								numMetal += AmountToCreatePerTick;
							}
							if (createChance < SulfurChance)
							{
								numSulfur += AmountToCreatePerTick;
							}
						}

						if (numStones > 0)
						{
							var stones = ItemManager.CreateByName("stones", numStones);
							InsertIntoInventory(container.inventory, stones);
						}
						if (numMetal > 0)
						{
							var metal = ItemManager.CreateByName("metal_ore", numMetal);
							InsertIntoInventory(container.inventory, metal);
						}
						if (numSulfur > 0)
						{
							var sulfur = ItemManager.CreateByName("sulfur_ore", numSulfur);
							InsertIntoInventory(container.inventory, sulfur);
						}
					}
				}

				quarry.TicksSinceUpdate = 0;
			}
		}

		private void InsertIntoInventory(ItemContainer container, Item item)
		{
			if (item != null && container != null && item.IsValid())
			{
				if (!item.MoveToContainer(container))
				{
					item.Drop(container.dropPosition, container.dropVelocity);
				}
			}
		}

		#endregion

		#region Add/Remove Quarries

		[HookMethod("OnPlayerAttack")]
		// ReSharper disable once UnusedMember.Local
		private object OnPlayerAttack(BasePlayer player, HitInfo hitInfo)
		{
			if (hitInfo.HitEntity != null)
			{
				if (hitInfo.Weapon.GetItem().info.shortname == "torch")
				{
					TryUpdateQuarry(player, hitInfo);
				}
			}

			return null;
		}

		private Quarry GetQuarryForPart(BaseEntity ent)
		{
			var id = ent.net.ID;
			foreach (var quarry in Quarries)
			{
				if (quarry.EntityIds.Contains(id))
				{
					return quarry;
				}
			}
			return null;
		}

		private void TryUpdateQuarry(BasePlayer player, HitInfo hitInfo)
		{
			var hitEntity = hitInfo.HitEntity;

			if (hitEntity.GetComponentInParent<BaseEntity>() == null || hitEntity.GetComponentInParent<BaseEntity>().GetComponent<StorageContainer>() == null)
			{
				return;
			}

			var owningQuarry = GetQuarryForPart(hitEntity);
			if (owningQuarry != null)
			{
				Quarries.Remove(owningQuarry);
				SaveQuarries();
				player.ChatMessage("Quarry Removed!");
				return;
			}

			List<BaseEntity> containers;
			bool nearBuilding;
			if (!HasValidQuarryConstruction(hitEntity.transform.position, out containers, out nearBuilding))
			{
				if (nearBuilding)
				{
					player.ChatMessage("Cannot build quarry near buildings!");
				}
				else
				{
					player.ChatMessage("Invalid quarry construction! There must be a 3x3 area of wooden boxes (small or large). Smack the middle with a torch!");
				}
				return;
			}

			HashSet<uint> containerIds = new HashSet<uint>(containers.Select(c => c.net.ID));

			for (int i = Quarries.Count - 1; i >= 0; i--)
			{
				var q = Quarries[i];
				if (q.EntityIds.Any(id => containerIds.Contains(id)))
				{
					// Quarry already exists.
					// There's nothing to do here.
					Quarries.RemoveAt(i);
					SaveQuarries();
					player.ChatMessage("Quarry Removed!");
					return;
				}
			}

			var quarry = new Quarry(hitEntity.net.ID, containers.Select(c => c.net.ID).ToList());
			Quarries.Add(quarry);
			SaveQuarries();

#if DEBUG
			player.ChatMessage("Quarry Created! ID: " + quarry.Id + ", Entities: " + string.Join(", ", quarry.EntityIds.Select(i => i.ToString()).ToArray()));
#else
			player.ChatMessage("Quarry Created!");
#endif
		}

		private bool HasValidQuarryConstruction(Vector3 position, out List<BaseEntity> storageContainers, out bool isNearBuilding)
		{
			isNearBuilding = false;
			storageContainers = new List<BaseEntity>();
			var hits = Physics.OverlapSphere(position, 2f);
			var collisions = new List<Collider>();
			foreach (var hit in hits)
			{
				if (collisions.Contains(hit))
				{
					continue;
				}

				collisions.Add(hit);

				var storage = hit.GetComponentInParent<BaseEntity>();

				if (storage == null)
				{
					continue;
				}

				if (storageContainers.Contains(storage))
				{
					continue;
				}

				// Make sure we're not building on a... building
				// We can only quarry into the ground. (This makes quarries a tiny bit harder to protect)
				if (storage.GetComponent<BuildingBlock>() != null)
				{
					isNearBuilding = true;
					return false;
				}

				if (hit.GetComponentInParent<BasePlayer>() == null)
				{
					var prefab = storage.LookupShortPrefabName();
					// Matches both small and large boxes!
					// TODO: Make this check for something other than a 3x3 of boxes.
					// Maybe something cooler looking? Not sure what yet.
					if (prefab.EndsWith("woodbox_deployed"))
					{
						storageContainers.Add(storage);
					}
				}
			}

			return storageContainers.Count == 9;
		}

		#endregion
	}
}