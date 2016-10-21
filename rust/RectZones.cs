using System;
using System.Collections.Generic;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;

using UnityEngine;

using Newtonsoft.Json;

namespace Oxide.Plugins
{
	[Info("RectZones", "deer_SWAG", "0.0.25", ResourceId = 000)]
	[Description("Creates polygonal zones")]
	public class RectZones : RustPlugin
	{
		const int MaxPoints = 254;
		const int MinPoints = 6;
		const string PermissionName = "rectzones.use";
		const string GameObjectPrefix = "zone-";

		class StoredData
		{
			[JsonProperty("zones")]
			public HashSet<ZoneDefinition> Zones = new HashSet<ZoneDefinition>();

			public StoredData() { }
		}

		public class ZoneDefinition
		{
			public string Id = string.Empty;
			[JsonProperty("n")]
			public string Name;
			[JsonProperty("v")]
			public List<JVector3> Vertices = new List<JVector3>();
			[JsonProperty("o")]
			public Dictionary<string, string> Options = new Dictionary<string, string>();

			public ZoneDefinition() { }
		}

		StoredData data;

		HashSet<GameObject> currentZones = new HashSet<GameObject>();

		class TemporaryStorage
		{
			public BasePlayer Player;
			public ZoneDefinition Zone;
			public Timer Timer;
			public bool Fixed;
			public float Height;
		}

		bool isEditing;
		List<TemporaryStorage> tempPlayers = new List<TemporaryStorage>();

		Dictionary<string, string> availableOptions = new Dictionary<string, string>()
		{
			{ "entermsg", "Shows message when a player enters a zone" },
			{ "exitmsg", "Shows message when a player exits a zone" },
			{ "nobuild", "Players can't build in zone" },
			{ "nobuildex", "Players can't build in zone. All buildings in a zone will be demolished" },
			{ "nostability", "Removes stability from buildings" },
			{ "nodestroy", "Players will be unable to destroy buildings and deployables" },
			{ "nopvp", "Players won't get hurt by another player" }
		};

		void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>()
			{
				{ "HelpText", "Rect Zones:\n/rect add <height> [fixed] [name]\n/rect done\n/rect undo\n/rect remove <zone id>\n/rect list\n/rect options\n/rect show [zone id]\n/rect clear" },
				{ "AlreadyEditing", "You are editing a zone. Type /rect done to finish editing" },
				{ "AddHelp", "Now you can start adding points by pressing \"USE\" button (default is \"E\"). They will be added where your crosshair is pointing. To add options type /options <name> = [value]. To finish type /rect done" },
				{ "Done", "Zone <color=#ffff00ff>{name}</color> with id <color=#ffff00ff>{id}</color> was added" },
				{ "DonePointsCount", "Points count should be more than 2 and less than 128" },
				{ "RemoveHelp", "To remove a zone type /rect remove <id/name>" },
				{ "Removed", "Zone was removed" },
				{ "ZoneNotFound", "Zone was not found" },
				{ "Empty", "Empty. To add a zone type /rect add <height> [fixed]" },
				{ "OptionsHelp", "/rect options <zoneID>" },
				{ "NoOptions", "This zone has no options" },
				{ "Cleared", "All zones were removed" },
				{ "ShowEmpty", "Nothing to show" },
				{ "CurrentZone", "Currently you are in zone with ID {id}" },
				{ "CurrentZoneNoZone", "You are not in any zone" },
				{ "InvalidAddCommand", "/rect add <height> [fixed] [name]" },
				{ "HeightMustBeNumber", "Argument \"height\" must be a number. If you want to use floating point number then write it in the quotes" },
				{ "UndoComplete", "Last point was removed" },
				{ "List", "Zones:\n" }
			}, this);
		}

		void Loaded()
		{
			LoadData();
			LoadDefaultMessages();

			if (data == null)
			{
				RaiseError("Unable to load data file");
				rust.RunServerCommand("oxide.unload " + Title);
			}

			permission.RegisterPermission(PermissionName, this);
		}

		void Unload()
		{
			foreach(GameObject zone in currentZones)
			{
				GameObject.Destroy(zone);
			}

			currentZones.Clear();
		}

		void OnServerInitialized()
		{
			foreach (ZoneDefinition definition in data.Zones)
			{
				CreateZoneByDefinition(definition);
			}
		}

		// -------------- Adding the points --------------
		// -----------------------------------------------

		void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			foreach(GameObject zone in currentZones)
			{
				RectZone zoneComponent = zone.GetComponent<RectZone>();
				zoneComponent.Players.Remove(player);
			}

			if (!isEditing)
				return;

			tempPlayers.RemoveAll(x => x.Player.userID == player.userID);
		}

		void OnPlayerInput(BasePlayer player, InputState input)
		{
			if (!isEditing)
				return;

			for (int i = 0; i < tempPlayers.Count; i++)
			{
				TemporaryStorage storage = tempPlayers[i];

				if (storage.Player.userID == player.userID)
				{
					if (input.WasJustPressed(BUTTON.USE))
					{
						Ray ray = player.eyes.HeadRay();
						RaycastHit hit;

						if(Physics.Raycast(ray, out hit, 10))
						{
							JVector3 bottomPoint = new JVector3(hit.point);
							JVector3 topPoint;

							if(storage.Fixed)
							{
								topPoint = new JVector3(hit.point.x, storage.Height, hit.point.z);
							}
							else
							{
								topPoint = new JVector3(hit.point.x, hit.point.y + storage.Height, hit.point.z);
							}

							storage.Zone.Vertices.Add(bottomPoint);
							storage.Zone.Vertices.Add(topPoint);

							ShowPoint(player, bottomPoint.ToUnity(), topPoint.ToUnity(), 2f);
						}
					}
				}
			}
		}

		// -------------- Chat and commands --------------
		// -----------------------------------------------

		CommandHelper chatHelper = new CommandHelper();

		[ChatCommand("rect")]
		void cmdChat(BasePlayer player, string command, string[] args)
		{
			if (!IsPlayerPermitted(player, PermissionName))
				return;

			int length = args.Length;

			chatHelper.Run(args, () =>
			{
				player.ChatMessage(Lang("HelpText", player));
			});

			chatHelper.OnCommand("add", () =>
			{
				if (CheckIsEditing(player) != null)
				{
					player.ChatMessage(Lang("AlreadyEditing", player));
					return;
				}

				if (chatHelper.NextExists())
				{
					float height = 0.0f;

					if(float.TryParse(chatHelper.Next(), out height))
					{
						if(chatHelper.NextExists())
						{
							if(chatHelper.Next().Equals("fixed", StringComparison.CurrentCultureIgnoreCase))
							{
								if(chatHelper.NextExists()) // with height, "fixed" and name
								{
									CommandAdd(player, height, chatHelper.Next(), true);
								}
								else // with height and "fixed"
								{
									CommandAdd(player, height, null, true);
								}
							}
							else // with height and name
							{
								CommandAdd(player, height, chatHelper.Current());
							}
						}
						else // with only height
						{
							CommandAdd(player, height);
						}
					}
					else
					{
						player.ChatMessage(Lang("HeightMustBeNumber", player));
					}
				}
				else
				{
					player.ChatMessage(Lang("InvalidAddCommand", player));
				}
			});

			chatHelper.OnCommand("done", () =>
			{
				TemporaryStorage storage = CheckIsEditing(player);

				if (storage != null)
				{
					if (storage.Zone.Vertices.Count < MinPoints || storage.Zone.Vertices.Count > MaxPoints)
					{
						player.ChatMessage(Lang("DonePointsCount", player));
						return;
					}

					CommandDone(player);
					player.ChatMessage(Lang("Done", player).Replace("{id}", storage.Zone.Id).Replace("{name}", storage.Zone.Name));
				}
			});

			chatHelper.OnCommand("undo", () =>
			{
				TemporaryStorage storage = CheckIsEditing(player);

				if (storage != null)
				{
					if(storage.Zone.Vertices.Count != 0)
					{
						storage.Zone.Vertices.RemoveRange(storage.Zone.Vertices.Count - 2, 2);
						player.ChatMessage(Lang("UndoComplete", player));
					}
				}
			});

			chatHelper.OnCommand("remove", () =>
			{
				if (CheckIsEditing(player) != null)
				{
					player.ChatMessage(Lang("AlreadyEditing", player));
					return;
				}

				if (chatHelper.NextExists())
				{
					if (IsDigitsOnly(chatHelper.Next()))
					{
						int removed = data.Zones.RemoveWhere(x => x.Id == chatHelper.Current());

						if (removed <= 0)
						{
							player.ChatMessage(Lang("ZoneNotFound", player));
							return;
						}

						currentZones.RemoveWhere((GameObject go) =>
						{
							RectZone rz = go.GetComponent<RectZone>();

							if (rz.Definition.Id == chatHelper.Current())
							{
								GameObject.Destroy(go);
								return true;
							}

							return false;
						});

						SaveData();

						player.ChatMessage(Lang("Removed", player));
					}
					else
					{
						int removed = data.Zones.RemoveWhere((ZoneDefinition x) =>
						{
							if (x.Name == null)
								return false;

							return x.Name.Equals(chatHelper.Current(), StringComparison.CurrentCultureIgnoreCase);
						});

						if (removed <= 0)
						{
							player.ChatMessage(Lang("ZoneNotFound", player));
							return;
						}

						currentZones.RemoveWhere((GameObject go) =>
						{
							RectZone rz = go.GetComponent<RectZone>();

							if (rz.Definition.Name.Equals(chatHelper.Current(), StringComparison.CurrentCultureIgnoreCase))
							{
								GameObject.Destroy(go);
								return true;
							}

							return false;
						});

						SaveData();

						player.ChatMessage(Lang("Removed", player));
					}
				}
				else
				{
					player.ChatMessage(Lang("RemoveHelp", player));
				}
			});

			chatHelper.OnCommand("list", () =>
			{
				if (data.Zones.Count == 0)
				{
					player.ChatMessage(Lang("Empty", player));
					return;
				}

				string result = Lang("List", player);
				foreach (ZoneDefinition zd in data.Zones)
				{
					result += zd.Id + (zd.Name != null ? (" (" + zd.Name + ")") : "") + ", ";
				}

				player.ChatMessage(result.Substring(0, result.Length - 2));
			});

			chatHelper.OnCommand("edit", () =>
			{
				if (CheckIsEditing(player) != null)
				{
					player.ChatMessage(Lang("AlreadyEditing", player));
					return;
				}

				if (args.Length > 1 && IsDigitsOnly(args[1]))
				{
					// TODO: editing
				}
			});

			chatHelper.OnCommand("options", () =>
			{
				TemporaryStorage storage = CheckIsEditing(player);

				if (storage == null)
				{
					if (chatHelper.NextExists())
					{
						string id = chatHelper.Next();

						if (IsDigitsOnly(id))
						{
							ZoneDefinition definition = null;

							foreach (ZoneDefinition d in data.Zones)
							{
								if (d.Id == id)
								{
									definition = d;
									break;
								}
							}

							if (definition == null)
							{
								player.ChatMessage(Lang("ZoneNotFound", player));
								return;
							}

							if (definition.Options.Count == 0)
							{
								player.ChatMessage(Lang("NoOptions", player));
								return;
							}

							string result = string.Empty;
							foreach (KeyValuePair<string, string> option in definition.Options)
							{
								result += option.Key + (option.Value != null ? (" = " + option.Value) : "") + ", ";
							}

							player.ChatMessage(result.Substring(0, result.Length - 2));
						}
						else
						{
							player.ChatMessage(Lang("OptionsHelp", player));
						}
					}
					else
					{
						string result = string.Empty;

						foreach (KeyValuePair<string, string> option in availableOptions)
						{
							result += "<color=#ffa500ff>" + option.Key + "</color> - " + (option.Value ?? "No description") + "\n";
						}

						player.ChatMessage(result);
					}
				}
				else
				{
					if (chatHelper.NextExists())
					{
						if (chatHelper.Next() == "list")
						{
							string result = string.Empty;

							foreach (KeyValuePair<string, string> option in availableOptions)
							{
								result += "<color=#ffa500ff>" + option.Key + "</color> - " + (option.Value ?? "No description") + "\n";
							}

							player.ChatMessage(result);

							return;
						}

						for (int i = 1; i < args.Length; i++)
						{
							string[] option = args[i].Split(new char[] { '=' }, 2);

							storage.Zone.Options.Add(option[0], (option.Length > 1 ? option[1] : null));
						}
					}
					else
					{
						if (storage.Zone.Options.Count != 0)
						{
							string result = string.Empty;
							foreach (KeyValuePair<string, string> option in storage.Zone.Options)
							{
								result += option.Key + " = " + option.Value + ", \n";
							}

							player.ChatMessage(result.Substring(0, result.Length - 2));
						}
						else
						{
							player.ChatMessage(Lang("NoOptions", player));
						}
					}
				}
			});

			chatHelper.OnCommand("show", () =>
			{
				if (chatHelper.NextExists() && IsDigitsOnly(chatHelper.Next()))
				{
					foreach (GameObject zone in currentZones)
					{
						RectZone component = zone.GetComponent<RectZone>();

						if (component.Definition.Id == chatHelper.Current())
						{
							component.ShowZone(player, 10f);
							return;
						}
					}

					player.ChatMessage(Lang("ZoneNotFound", player));
				}
				else if (chatHelper.Current().Equals("current", StringComparison.CurrentCultureIgnoreCase))
				{
					RectZone zone = GetCurrentZoneForPlayer(player);
					zone.ShowZone(player, 10f);
				}
				else
				{
					if (currentZones.Count == 0)
					{
						player.ChatMessage(Lang("ShowEmpty", player));
						return;
					}

					foreach (GameObject zone in currentZones)
					{
						zone.GetComponent<RectZone>().ShowZone(player, 10f);
					}
				}
			});

			chatHelper.OnCommand("current", () =>
			{
				RectZone zone = GetCurrentZoneForPlayer(player);

				if (zone != null)
				{
					player.ChatMessage(Lang("CurrentZone", player).Replace("{id}", zone.Definition.Id));
				}
				else
				{
					player.ChatMessage(Lang("CurrentZoneNoZone", player));
				}
			});

			chatHelper.OnCommand("clear", () =>
			{
				foreach (GameObject zone in currentZones)
				{
					GameObject.Destroy(zone);
				}

				currentZones.Clear();
				data.Zones.Clear();

				SaveData();

				player.ChatMessage(Lang("Cleared", player));
			});
		}

		TemporaryStorage CheckIsEditing(BasePlayer player)
		{
			if (tempPlayers.Count == 0)
				return null;

			return tempPlayers.Find(x => x.Player.userID == player.userID);
		}

		void CommandAdd(BasePlayer player, float height, string name = null, bool fixedHeight = false)
		{
			player.ChatMessage(Lang("AddHelp", player));

			isEditing = true;

			TemporaryStorage storage = new TemporaryStorage();
			storage.Player = player;
			storage.Zone = new ZoneDefinition
			{
				Id = GenerateId()
			};
			storage.Height = height;
			storage.Fixed = fixedHeight;
			storage.Zone.Name = name;

			storage.Timer = timer.Repeat(5f, 0, () =>
			{
				JVector3 prevVector = null;

				foreach(JVector3 vector in storage.Zone.Vertices)
				{
					if(prevVector == null)
					{
						prevVector = vector;
						continue;
					}

					ShowPoint(player, prevVector.ToUnity(), vector.ToUnity(), 5.5f);

					prevVector = null;
				}
			});

			tempPlayers.Add(storage);
		}

		void CommandDone(BasePlayer player)
		{
			tempPlayers.RemoveAll((TemporaryStorage storage) =>
			{
				if(storage.Player.userID == player.userID)
				{
					storage.Timer.Destroy();
					storage.Timer = null;
					storage.Player = null;

					data.Zones.Add(storage.Zone);

					CreateZoneByDefinition(storage.Zone);

					return true;
				}

				return false;
			});

			SaveData();

			if(tempPlayers.Count == 0)
				isEditing = false;
		}

		void CreateZoneByDefinition(ZoneDefinition definition)
		{
			GameObject zoneObject = new GameObject(GameObjectPrefix + definition.Id);
			RectZone zoneComponent = zoneObject.AddComponent<RectZone>();
			zoneComponent.SetZone(definition);
			currentZones.Add(zoneObject);
		}

		RectZone GetCurrentZoneForPlayer(BasePlayer player)
		{
			foreach (GameObject zone in currentZones)
			{
				RectZone zoneComponent = zone.GetComponent<RectZone>();

				if (zoneComponent.Players.Contains(player))
				{
					return zoneComponent;
				}
			}

			return null;
		}

		// For HelpText plugin

		void SendHelpText(BasePlayer player)
		{
			if(IsPlayerPermitted(player, PermissionName))
				player.ChatMessage(Lang("HelpText", player));
		}

		// ----------------- For options -----------------
		// -----------------------------------------------

		void OnEntityBuilt(Planner plan, GameObject go)
		{
			foreach (GameObject zone in currentZones)
			{
				RectZone zoneComponent = zone.GetComponent<RectZone>();

				if (zoneComponent.Players.Count == 0)
					continue;

				if (zoneComponent.Definition.Options.ContainsKey("nobuild"))
				{
					if (zoneComponent.GetComponent<Collider>().bounds.Contains(go.transform.position))
					{
						go.GetComponentInParent<BaseCombatEntity>().Kill(BaseNetworkable.DestroyMode.Gib);

						if (zoneComponent.Definition.Options["nobuild"] != null)
							plan.GetOwnerPlayer().ChatMessage(zoneComponent.Definition.Options["nobuild"]);

						break;
					}
				}
				else if(zoneComponent.Definition.Options.ContainsKey("nobuildex"))
				{
					if (zoneComponent.GetComponent<Collider>().bounds.Contains(go.transform.position))
					{
						go.GetComponentInParent<BaseCombatEntity>().Kill(BaseNetworkable.DestroyMode.Gib);

						if (zoneComponent.Definition.Options["nobuildex"] != null)
							plan.GetOwnerPlayer().ChatMessage(zoneComponent.Definition.Options["nobuildex"]);

						break;
					}
				}
			}
		}

		void OnEntitySpawned(BaseNetworkable entity)
		{
			if (!(entity is BuildingBlock))
				return;

			BuildingBlock block = (BuildingBlock)entity;

			foreach(GameObject zone in currentZones)
			{
				RectZone zoneComponent = zone.GetComponent<RectZone>();

				if (zoneComponent.Definition.Options.ContainsKey("nostability"))
				{
					if (zoneComponent.GetComponent<Collider>().bounds.Contains(block.transform.position))
					{
						block.grounded = true;
					}
				}
			}
		}

		void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
		{
			bool blockDamage = false;

			foreach (GameObject zone in currentZones)
			{
				RectZone zoneComponent = zone.GetComponent<RectZone>();

				if((entity is BuildingBlock && zoneComponent.Definition.Options.ContainsKey("nodestroy")) ||
				   (entity is BasePlayer && (hitinfo.Initiator is BasePlayer || hitinfo.Initiator is FireBall) && zoneComponent.Definition.Options.ContainsKey("nopvp")))
				{
					if(zoneComponent.GetComponent<Collider>().bounds.Contains(entity.transform.position))
					{
						blockDamage = true;
						break;
					}
				}
			}

			if(blockDamage)
			{
				hitinfo.damageTypes = new Rust.DamageTypeList();
				hitinfo.DoHitEffects = false;
				hitinfo.HitMaterial = 0;
			}
		}

		// --------------- FOR EXTENSIONS AND OTHER PLUGINS ----------------
		// -----------------------------------------------------------------

		[HookMethod("RegisterOption")]
		public bool RegisterOption(string name, string description = null)
		{
			if(availableOptions.Count > 0)
			{
				if (availableOptions.ContainsKey(name))
				{
					PrintWarning("There is already an option with name \"" + name + "\"");
					return false;
				}
			}

			availableOptions.Add(name, description);

			return true;
		}

		[HookMethod("UnregisterOption")]
		public void UnregisterOption(string name)
		{
			availableOptions.Remove(name);
		}

		// Hooks
		// void OnEnterZone(string id, BasePlayer player)
		// void OnExitZone(string id, BasePlayer player)
		// void OnEnterZoneWithOptions(string id, BasePlayer player, Dictionary<string, string> options)
		// void OnExitZoneWithOptions(string id, BasePlayer player, Dictionary<string, string> options)

		// ----------------------------- UTILS -----------------------------
		// -----------------------------------------------------------------

		void LoadData()
		{
			data = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Title);
		}

		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject(Title, data);
		}

		static void ShowPoint(BasePlayer player, Vector3 from, Vector3 to, float duration = 5f)
		{
			DrawDebugLine(player, from, to, 4.8f);
			DrawDebugSphere(player, from, 0.1f, 4.8f);
			DrawDebugSphere(player, to, 0.1f, 4.8f);
		}

		static void DrawDebugLine(BasePlayer player, Vector3 from, Vector3 to, float duration = 1f)
		{
			player.SendConsoleCommand("ddraw.line", duration, Color.yellow, from, to);
		}

		static void DrawDebugSphere(BasePlayer player, Vector3 position, float radius = 0.5f, float duration = 1f)
		{
			player.SendConsoleCommand("ddraw.sphere", duration, Color.green, position, radius);
		}

		string GenerateId()
		{
			byte[] bytes = Guid.NewGuid().ToByteArray();
			int number = Math.Abs(BitConverter.ToInt32(bytes, 0));
			return number.ToString();
		}

		string Lang(string key, BasePlayer player = null) => lang.GetMessage(key, this, player?.UserIDString);

		bool IsDigitsOnly(string str)
		{
			foreach (char c in str)
				if (c < '0' || c > '9')
					return false;
			return true;
		}

		bool IsPlayerPermitted(BasePlayer player, string permissionName) => (player.IsAdmin() || permission.UserHasPermission(player.UserIDString, permissionName));

		// --------------------------- HELPERS -----------------------------
		// -----------------------------------------------------------------

		public class CommandHelper
		{
			private string[] args;
			private int index;
			private bool isEmpty;

			public void Run(string[] commands, Action onEmpty)
			{
				if (commands.Length == 0 || string.IsNullOrEmpty(commands[0]))
				{
					onEmpty.Invoke();
					isEmpty = true;
					return;
				}

				isEmpty = false;
				args = commands;
				index = 0;
			}

			public void OnCommand(string command, Action action)
			{
				if (isEmpty)
					return;

				if (command.Equals(args[0], StringComparison.CurrentCultureIgnoreCase))
					action.Invoke();
			}

			public string Current()
			{
				return args[index];
			}

			public string Next()
			{
				index++;
				return args[index];
			}

			public string Prev()
			{
				index--;
				return args[index];
			}

			public bool NextExists(int num = 1)
			{
				if (args.Length >= (index + num + 1))
					return true;
				return false;
			}
		}

		public class JVector3
		{
			public float x;
			public float y;
			public float z;

			public JVector3() { }

			public JVector3(float x, float y, float z)
			{
				this.x = x;
				this.y = y;
				this.z = z;
			}

			public JVector3(Vector3 vector)
			{
				x = vector.x;
				y = vector.y;
				z = vector.z;
			}

			public Vector3 ToUnity()
			{
				return new Vector3(x, y, z);
			}
		}

		public class RectZone : MonoBehaviour
		{
			public ZoneDefinition Definition;
			public HashSet<BasePlayer> Players = new HashSet<BasePlayer>();

			MeshCollider collider;

			void Awake()
			{
				gameObject.layer = (int)Rust.Layer.Reserved1;

				Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
				rigidbody.isKinematic = true;
				rigidbody.useGravity = false;
				rigidbody.detectCollisions = true;

				collider = gameObject.AddComponent<MeshCollider>();
				collider.convex = true;
				collider.isTrigger = true;
			}

			public void SetZone(ZoneDefinition definition)
			{
				Definition = definition;

				MakeMesh(definition.Vertices);
			}

			public void ShowZone(BasePlayer player, float duration)
			{
				Vector3? prevVertex = null;

				foreach (Vector3 vertex in collider.sharedMesh.vertices)
				{
					if (prevVertex == null)
					{
						prevVertex = vertex;
						continue;
					}

					ShowPoint(player, prevVertex.Value, vertex, duration);

					prevVertex = null;
				}
			}

			void MakeMesh(List<JVector3> vertices)
			{
				Mesh mesh = new Mesh();

				List<Vector3> tempVertices = new List<Vector3>();
				List<int> tempIndices = new List<int>();

				foreach(JVector3 vertex in vertices)
				{
					tempVertices.Add(vertex.ToUnity());
				}

				// Bottom cap

				/*int count = 1;

				for (int i = 0; i < tempVertices.Count - 4; i += 2)
				{
					if (count % 2 == 0)
					{
						tempIndices.Add(i + 2);
						tempIndices.Add(i + 4);
						tempIndices.Add(0);
					}
					else
					{
						tempIndices.Add(0);
						tempIndices.Add(i + 2);
						tempIndices.Add(i + 4);
					}

					count++;
				}*/

				// Side

				tempIndices.Add(0);
				tempIndices.Add(1);

				for (int q = 2; q < tempVertices.Count; q++)
				{
					tempIndices.Add(q);

					if (tempIndices.Count % 3 == 0)
					{
						tempIndices.Add(q - 1);
						tempIndices.Add(q + 1);
						tempIndices.Add(q);
						tempIndices.Add(q);
					}
				}

				tempIndices.Add(0);
				tempIndices.Add(tempVertices.Count - 1);
				tempIndices.Add(1);
				tempIndices.Add(0);

				// Top cap

				/*count = 1;

				for (int i = 1; i < tempVertices.Count - 4; i += 2)
				{
					if (count % 2 == 0)
					{
						tempIndices.Add(tempVertices.Count - (i + 2));
						tempIndices.Add(tempVertices.Count - (i + 4));
						tempIndices.Add(tempVertices.Count - 1);
					}
					else
					{
						tempIndices.Add(tempVertices.Count - 1);
						tempIndices.Add(tempVertices.Count - (i + 2));
						tempIndices.Add(tempVertices.Count - (i + 4));
					}

					count++;
				}*/

				mesh.SetVertices(tempVertices);
				mesh.SetIndices(tempIndices.ToArray(), MeshTopology.Triangles, 0);

				mesh.RecalculateBounds();
				mesh.RecalculateNormals();

				collider.sharedMesh = mesh;
			}

			void OnTriggerEnter(Collider collider)
			{
				BasePlayer player = collider.GetComponentInParent<BasePlayer>();

				if (player != null)
				{
					Players.Add(player);

					if (Definition.Options.ContainsKey("entermsg") && Definition.Options["entermsg"] != null)
						player.ChatMessage(Definition.Options["entermsg"]);

					Interface.Oxide.CallHook("OnEnterZone", Definition.Id, player);

					if(Definition.Options.Count != 0)
						Interface.Oxide.CallHook("OnEnterZoneWithOptions", Definition.Id, player, Definition.Options);
				}
				else
				{
					if (Definition.Options.ContainsKey("nobuildex"))
					{
						MeshColliderBatch batch = collider.GetComponent<MeshColliderBatch>();

						if(batch != null)
						{
							FieldInfo info = batch.GetType().GetField("instances", BindingFlags.NonPublic | BindingFlags.Instance);
							var batchColliders = (ListDictionary<Component, ColliderCombineInstance>)info.GetValue(batch);

							List<ColliderCombineInstance> batchCollidersList = batchColliders.Values;

							for(int i = 0; i < batchCollidersList.Count; i++)
							{
								ColliderCombineInstance instance = batchCollidersList[i];

								if(this.collider.bounds.Intersects(instance.bounds.ToBounds()))
								{
									instance.collider.GetComponentInParent<BaseCombatEntity>()?.Kill(BaseNetworkable.DestroyMode.Gib);
								}
							}
						}
					}
				}

				//Debug.Log("OnTriggerEnter: " + player?.userID);
			}

			void OnTriggerExit(Collider collider)
			{
				BasePlayer player = collider.GetComponentInParent<BasePlayer>();

				if (player != null)
				{
					Players.Remove(player);

					if (Definition.Options.ContainsKey("exitmsg") && Definition.Options["exitmsg"] != null)
						player.ChatMessage(Definition.Options["exitmsg"]);

					Interface.Oxide.CallHook("OnExitZone", Definition.Id, player);

					if(Definition.Options.Count != 0)
						Interface.Oxide.CallHook("OnExitZoneWithOptions", Definition.Id, player, Definition.Options);
				}

				//Debug.Log("OnTriggerExit: " + player?.displayName);
			}
		}

	}
}
