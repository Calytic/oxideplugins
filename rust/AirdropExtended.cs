using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AirdropExtended;
using AirdropExtended.Airdrop.Services;
using AirdropExtended.Airdrop.Settings;
using AirdropExtended.Airdrop.Settings.Generate;
using AirdropExtended.Behaviors;
using AirdropExtended.Commands;
using AirdropExtended.Diagnostics;
using AirdropExtended.Permissions;
using AirdropExtended.Rust.Extensions;
using AirdropExtended.WeightedSearch;
using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using AirdropExtended.PluginSettings;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Libraries;
using Oxide.Plugins;
using Rust;
using UnityEngine;
using Constants = AirdropExtended.Constants;
using LogType = Oxide.Core.Logging.LogType;
using Timer = Oxide.Core.Libraries.Timer;

// ReSharper disable once CheckNamespace

namespace Oxide.Plugins
{
	[Info(Constants.PluginName, "baton", "1.0.5", ResourceId = 1210)]
	[Description("Customizable airdrop")]
	public class AirdropExtended : RustPlugin
	{
		private readonly SettingsContext _settingsContext;
		private readonly AirdropController _airdropController;
		private PluginSettingsRepository _pluginSettingsRepository;
		private Dictionary<string, AirdropExtendedCommand> _commands;

		public AirdropExtended()
		{
			_settingsContext = new SettingsContext();
			_airdropController = new AirdropController(_settingsContext);
		}

		private void OnServerInitialized()
		{
			LoadConfig();
			Bootstrap();
			Save();
		}

		private void Bootstrap()
		{
			_pluginSettingsRepository = new PluginSettingsRepository(Config, SaveConfig);
			LoadAireSettings();

			var commands = CommandFactory.Create(_settingsContext, _pluginSettingsRepository, _airdropController);
			PermissionService.RegisterPermissions(this, commands);

			_commands = commands.ToDictionary(c => c.Name, c => c);
			var consoleSystem = Interface.Oxide.GetLibrary<Command>();
			foreach (var command in commands.Where(c => !c.IsChatOnly))
			{
				var command1 = command;
				consoleSystem.AddConsoleCommand(command.Name, this, arg =>
				{
					command1.Execute(arg, arg.Player());
					return true;
				});
			}
		}

		private void LoadAireSettings()
		{
			_settingsContext.SettingsName = _pluginSettingsRepository.LoadSettingsName();
			Diagnostics.MessageToServer("Loaded settings:{0}", _settingsContext.SettingsName);
			_settingsContext.Settings = AidropSettingsRepository.LoadFrom(_settingsContext.SettingsName);
			_airdropController.ApplySettings();
		}

		private void Save()
		{
			_pluginSettingsRepository.SaveSettingsName(_settingsContext.SettingsName);
			AidropSettingsRepository.SaveTo(_settingsContext.SettingsName, _settingsContext.Settings);

			SaveConfig();
		}

		private void Unload()
		{
			_airdropController.Cleanup();
		}

		void OnPlayerLoot(PlayerLoot lootInventory, BaseEntity targetEntity)
		{
			var supplyDrop = targetEntity as SupplyDrop;
			if (lootInventory == null || targetEntity == null || supplyDrop == null)
				return;

			var settings = _settingsContext.Settings;

			Diagnostics.MessageTo(
				settings.Localization.NotifyOnPlayerLootingStartedMessage,
				settings.CommonSettings.NotifyOnPlayerLootingStarted,
				lootInventory.GetComponent<BasePlayer>().displayName);
		}

		private void OnEntitySpawned(BaseEntity entity)
		{
			if (_airdropController.IsInitialized())
				_airdropController.OnEntitySpawned(entity);
		}

		private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
		{
			if (entity is SupplySignal)
				_airdropController.OnSupplySignal(player, entity);
		}

		#region command handlers

		[ChatCommand("aire.load")]
		private void LoadSettingsChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.load"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.save")]
		private void SaveSettingsChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.save"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.generate")]
		private void GenerateSettingsChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.generate"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.reload")]
		private void ReloadSettingsChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.reload"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.players")]
		private void SetPlayersChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.players"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.event")]
		private void SetEventEnabledChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.event"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.supply")]
		private void SetSupplyEnabledChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.supply"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.timer")]
		private void SetTimerEnabledChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.timer"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.despawntime")]
		private void SetDespawnTimeChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.despawntime"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.minfreq")]
		private void SetMinFrequencyChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.minfreq"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.maxfreq")]
		private void SetMaxFrequencyChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.maxfreq"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.setitem")]
		private void SetItemChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.setitem"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.setitemgroup")]
		private void SetItemGroupChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.setitemgroup"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.capacity")]
		private void SetDropCapacityChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.capacity"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.enableplanelimit")]
		private void EnablePlaneLimitChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.enableplanelimit"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.planelimit")]
		private void SetPlaneLimitChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.planelimit"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.planespeed")]
		private void SetPlaneSpeedChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.planespeed"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.crates")]
		private void SetCratesChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.crates"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.onelocation")]
		private void SetDropToOneLocationChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.onelocation"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.drop")]
		private void CallRandomDropChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.drop"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.massdrop")]
		private void CallMassDropChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.massdrop"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.topos")]
		private void CallDropToPosChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.topos"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.toplayer")]
		private void CallDropToPlayerChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.toplayer"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.tome")]
		private void CallDropToMeChatCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.tome"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.localize")]
		private void CallLocalizeCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.localize"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.notify")]
		private void CallNotifyCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.notify"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.customloot")]
		private void CallSetCustomLootCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.customloot"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.pick")]
		private void CallSetPickStrategyCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.pick"].ExecuteFromChat(player, command, args);
		}

		[ChatCommand("aire.test")]
		private void CallTestLootCommand(BasePlayer player, string command, string[] args)
		{
			_commands["aire.test"].ExecuteFromChat(player, command, args);
		}

		#endregion
	}
}

namespace AirdropExtended
{
	public sealed class Constants
	{
		public const string PluginName = "AirdropExtended";
		public const string CargoPlanePrefab = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
	}

	public sealed class SettingsContext
	{
		public string SettingsName { get; set; }
		public AirdropSettings Settings { get; set; }
	}
}

namespace AirdropExtended.Rust.Extensions
{
	public static class LocalizationExtensions
	{
		public static string GetDirectionsFromAngle(float angle, DirectionLocalizationSettings settings)
		{
			if (settings == null) throw new ArgumentNullException("settings");

			if (angle > 337.5 || angle < 22.5)
				return settings.North;
			if (angle > 22.5 && angle < 67.5)
				return settings.NorthEast;
			if (angle > 67.5 && angle < 112.5)
				return settings.East;
			if (angle > 112.5 && angle < 157.5)
				return settings.SouthEast;
			if (angle > 157.5 && angle < 202.5)
				return settings.South;
			if (angle > 202.5 && angle < 247.5)
				return settings.SouthWest;
			if (angle > 247.5 && angle < 292.5)
				return settings.West;
			if (angle > 292.5 && angle < 337.5)
				return settings.NorthWest;
			return string.Empty;
		}
	}

	public static class CargoPlaneExtensions
	{
		public static void SetPlaneEndPosition(
			this CargoPlane plane,
			Vector3 startPosition,
			Vector3 endPosition,
			int planeSpeedInSeconds)
		{
			if (plane == null) throw new ArgumentNullException("plane");
			CargoPlaneFields.StartPositionField.SetValue(plane, startPosition);
			CargoPlaneFields.EndPositionField.SetValue(plane, endPosition);

			plane.SetSpeed(startPosition, endPosition, planeSpeedInSeconds);
		}

		public static void SetSpeed(this CargoPlane plane, Vector3 startPosition, Vector3 endPosition, int planeSpeedInSeconds)
		{
			if (plane == null) throw new ArgumentNullException("plane");
			var speed = Vector3.Distance(endPosition, startPosition) / planeSpeedInSeconds;
			plane.transform.rotation = Quaternion.LookRotation(endPosition - startPosition);

			CargoPlaneFields.SecondsTakenField.SetValue(plane, 0);
			CargoPlaneFields.SecondsToTakeField.SetValue(plane, speed);
		}

		public static void SetSpeed(this CargoPlane plane, int planeSpeedInSeconds)
		{
			if (plane == null) throw new ArgumentNullException("plane");
			var startPosition = (Vector3)CargoPlaneFields.StartPositionField.GetValue(plane);
			var endPosition = (Vector3)CargoPlaneFields.EndPositionField.GetValue(plane);

			plane.SetSpeed(startPosition, endPosition, planeSpeedInSeconds);
		}

		public static void NotifyNextDropPosition(this CargoPlane plane, Vector3 dropPos, AirdropSettings settings)
		{
			Diagnostics.Diagnostics.MessageTo(
				settings.Localization.NotifyOnNextDropPositionMessage,
				settings.CommonSettings.NotifyOnNextDropPosition,
				dropPos.x,
				dropPos.y,
				dropPos.z);
		}
	}
}

namespace AirdropExtended.Behaviors
{
	public sealed class SupplyDropLandedBehavior : MonoBehaviour
	{
		private const string DefaultNotifyOnCollisionMessage = "Supply drop has landed at {0:F0},{1:F0},{2:F0}";

		private bool _isTriggered;

		public string NotifyOnLandedMessage { get; set; }
		public bool NotifyOnLanded { get; set; }

		public bool NotifyAboutPlayersAroundOnDropLand { get; set; }
		public string NotifyAboutPlayersAroundOnDropLandMessage { get; set; }

		public bool NotifyAboutDirectionAroundOnDropLand { get; set; }
		public string NotifyAboutDirectionAroundOnDropLandMessage { get; set; }

		public int DropNotifyMaxDistance { get; set; }

		public DirectionLocalizationSettings Settings { get; set; }

		public SupplyDropLandedBehavior()
		{
			NotifyOnLandedMessage = DefaultNotifyOnCollisionMessage;
			NotifyOnLanded = true;
		}

		void OnCollisionEnter(Collision col)
		{
			if (_isTriggered || col.gameObject.GetComponent<CargoPlane>() != null)
				return;

			_isTriggered = true;

			var baseEntity = GetComponent<BaseEntity>();

			var dropPosition = baseEntity.transform.position;
			var landedX = dropPosition.x;
			var landedY = dropPosition.y;
			var landedZ = dropPosition.z;

			Diagnostics.Diagnostics.MessageTo(NotifyOnLandedMessage, NotifyOnLanded, landedX, landedY, landedZ);

			if (NotifyAboutDirectionAroundOnDropLand || NotifyAboutPlayersAroundOnDropLand)
				NotifyPlayersAround(dropPosition, baseEntity);

			var despawnBehavior = baseEntity.GetComponent<SupplyDropDespawnBehavior>();
			if (despawnBehavior != null)
				despawnBehavior.Despawn();
		}

		private void NotifyPlayersAround(Vector3 dropPosition, BaseEntity baseEntity)
		{
			var players = BasePlayer.activePlayerList;
			var nearbyPlayers = players
				.Where(p => Vector3.Distance(p.transform.position, dropPosition) < DropNotifyMaxDistance)
				.ToList();

			foreach (var player in nearbyPlayers)
			{
				var distance = Vector3.Distance(player.transform.position, dropPosition);
				var dropVector = (baseEntity.transform.position - player.eyes.position).normalized;
				var rotation = Quaternion.LookRotation(dropVector);

				var compassDirection = LocalizationExtensions.GetDirectionsFromAngle(rotation.eulerAngles.y, Settings);

				if (NotifyAboutDirectionAroundOnDropLand)
					Diagnostics.Diagnostics.MessageToPlayer(player, NotifyAboutDirectionAroundOnDropLandMessage, distance, compassDirection);
				if (NotifyAboutPlayersAroundOnDropLand)
					Diagnostics.Diagnostics.MessageToPlayer(player, NotifyAboutPlayersAroundOnDropLandMessage, nearbyPlayers.Count);
			}
		}

		public static void AddTo(
			BaseEntity entity,
			AirdropSettings settings)
		{
			if (entity == null) throw new ArgumentNullException("entity");
			if (settings == null) throw new ArgumentNullException("settings");

			entity.gameObject.AddComponent<SupplyDropLandedBehavior>();

			var dropCollisionCheck = entity.gameObject.GetComponent<SupplyDropLandedBehavior>();

			dropCollisionCheck.NotifyOnLanded = settings.CommonSettings.NotifyOnCollision;
			dropCollisionCheck.NotifyOnLandedMessage = settings.Localization.NotifyOnCollisionMessage;

			dropCollisionCheck.DropNotifyMaxDistance = settings.CommonSettings.DropNotifyMaxDistance;

			dropCollisionCheck.NotifyAboutDirectionAroundOnDropLand = settings.CommonSettings.NotifyAboutDirectionAroundOnDropLand;
			dropCollisionCheck.NotifyAboutDirectionAroundOnDropLandMessage = settings.Localization.NotifyAboutDirectionAroundOnDropLandMessage;

			dropCollisionCheck.NotifyAboutPlayersAroundOnDropLand = settings.CommonSettings.NotifyAboutPlayersAroundOnDropLand;
			dropCollisionCheck.NotifyAboutPlayersAroundOnDropLandMessage = settings.Localization.NotifyAboutPlayersAroundOnDropLandMessage;

			dropCollisionCheck.Settings = settings.Localization.Directions;
		}

	}

	public sealed class SupplyDropDespawnBehavior : MonoBehaviour
	{
		private const string DefaultNotifyOnDespawnMessage = "Supply drop has been despawned at <color=red>{0:F0},{1:F0},{2:F0}</color>";
		private const float DefaultSupplyStayTime = 300.0f;

		private bool _isTriggered;
		private Timer.TimerInstance _timerInstance;
		private BaseEntity _baseEntity;

		public float TimeoutInSeconds { get; set; }

		public string NotifyOnDespawnMessage { get; set; }
		public bool NotifyOnDespawn { get; set; }

		public SupplyDropDespawnBehavior()
		{
			TimeoutInSeconds = DefaultSupplyStayTime;
			NotifyOnDespawnMessage = DefaultNotifyOnDespawnMessage;
			NotifyOnDespawn = false;
		}

		public void Despawn()
		{
			if (_isTriggered)
				return;

			_baseEntity = gameObject.GetComponent<BaseEntity>();
			_isTriggered = true;

			_timerInstance = Interface.Oxide.GetLibrary<Timer>("Timer").Once(TimeoutInSeconds, DespawnCallback);
		}

		private void DespawnCallback()
		{
			var x = _baseEntity.transform.position.x;
			var y = _baseEntity.transform.position.y;
			var z = _baseEntity.transform.position.z;

			Diagnostics.Diagnostics.MessageTo(NotifyOnDespawnMessage, NotifyOnDespawn, x, y, z);
			_baseEntity.KillMessage();

			OnDestroy();
		}

		void OnDestroy()
		{
			_baseEntity = null;

			if (_timerInstance == null || _timerInstance.Destroyed)
				return;

			_timerInstance.Destroy();
			_timerInstance = null;
			_isTriggered = false;
		}

		public static void AddTo(BaseEntity supplyDrop, AirdropSettings settings)
		{
			if (supplyDrop == null) throw new ArgumentNullException("supplyDrop");
			if (settings == null) throw new ArgumentNullException("settings");

			var supplyCrateDespawnTime = settings.CommonSettings.SupplyCrateDespawnTime;
			if (supplyCrateDespawnTime <= TimeSpan.Zero)
				return;

			var timeoutInSeconds = Convert.ToSingle(supplyCrateDespawnTime.TotalSeconds);

			supplyDrop.gameObject.AddComponent<SupplyDropDespawnBehavior>();
			var despawnBehavior = supplyDrop.gameObject.GetComponent<SupplyDropDespawnBehavior>();

			despawnBehavior.TimeoutInSeconds = timeoutInSeconds;
			despawnBehavior.NotifyOnDespawn = settings.CommonSettings.NotifyOnDespawn;
			despawnBehavior.NotifyOnDespawnMessage = settings.Localization.NotifyOnDespawnMessage;
		}
	}

	public sealed class PlaneNotifyOnDestroyBehavior : MonoBehaviour
	{
		public Action<CargoPlane> Callback { get; set; }

		void OnDestroy()
		{
			Diagnostics.Diagnostics.MessageToServer("On destroy plane");
			if (Callback == null)
				return;

			var cargoPlane = GetComponent<CargoPlane>();
			if (cargoPlane != null)
			{
				Callback(cargoPlane);
				Diagnostics.Diagnostics.MessageToServer("cargoPlane [id:{0}] was destroyed", cargoPlane.GetInstanceID());
			}

			Callback = null;
		}
	}

	public sealed class MultipleCratesBehavior : MonoBehaviour
	{
		public int TotalCratesToDrop { get; set; }
		public Vector3 InitialEndPosition { get; set; }
		public bool DropToOneLocation { get; set; }
		public int DroppedCrates { get; set; }

		private Vector3 IntersectionPoint { get; set; }

		public void OnPlaneDroppedCrate(CargoPlane plane, AirdropSettings settings, Vector3 position)
		{
			DroppedCrates++;
			var planeSpeedInSeconds = settings.CommonSettings.PlaneSpeedInSeconds;
			if (DroppedCrates >= TotalCratesToDrop)
			{
				plane.SetPlaneEndPosition(position, InitialEndPosition, planeSpeedInSeconds);
				return;
			}

			if (IntersectionPoint == Vector3.zero)
				IntersectionPoint = GetIntersectionPoint(settings.DropLocation, position, InitialEndPosition);

			CargoPlaneFields.DroppedField.SetValue(plane, false);

			Vector3 currentEndPos;
			Vector3 nextDropPosition;
			if (DropToOneLocation)
			{
				currentEndPos = Vector3.MoveTowards(position, InitialEndPosition, 50f);
				nextDropPosition = Vector3.MoveTowards(position, InitialEndPosition,
					25f);
			}
			else
			{
				var cratesToDrop = (TotalCratesToDrop - DroppedCrates);
				var distanceToNextEndPos = Vector3.Distance(position, IntersectionPoint) / cratesToDrop;
				var distanceToNextDrop = distanceToNextEndPos / 2;
				currentEndPos = Vector3.MoveTowards(position, IntersectionPoint, distanceToNextEndPos);
				nextDropPosition = Vector3.MoveTowards(position, IntersectionPoint, distanceToNextDrop);
			}
			plane.NotifyNextDropPosition(nextDropPosition, settings);
			CargoPlaneFields.DropPositionField.SetValue(plane, nextDropPosition);
			plane.SetPlaneEndPosition(position, currentEndPos, planeSpeedInSeconds);
		}

		public Vector3 GetIntersectionPoint(DropLocationSettings settings, Vector3 position, Vector3 endpos)
		{
			var lines = new List<Vector2[]>
			{
				new[] {new Vector2(settings.MinX, settings.MinZ), new Vector2(settings.MinX, settings.MaxZ)},
				new[] {new Vector2(settings.MinX, settings.MaxZ), new Vector2(settings.MaxX, settings.MaxZ)},
				new[] {new Vector2(settings.MaxX, settings.MaxZ), new Vector2(settings.MaxX, settings.MinZ)},
				new[] {new Vector2(settings.MaxX, settings.MinZ), new Vector2(settings.MinX, settings.MinZ)},
			};

			var lineStart = new Vector2(position.x, position.z);
			var lineEnd = new Vector2(endpos.x, endpos.z);

			foreach (var line in lines)
			{
				var intersectionPoint = LineIntersectionPoint(line[0], line[1], lineStart, lineEnd);
				if (intersectionPoint != Vector2.zero)
					return new Vector3(intersectionPoint.x, position.y, intersectionPoint.y);
			}

			return Vector3.zero;
		}

		private Vector2 LineIntersectionPoint(Vector2 lineOneStart, Vector2 lineOneEnd1, Vector2 lineTwoStart, Vector2 lineTwoEnd)
		{
			var d = (lineOneStart.x - lineOneEnd1.x) * (lineTwoStart.y - lineTwoEnd.y) - (lineOneStart.y - lineOneEnd1.y) * (lineTwoStart.x - lineTwoEnd.x);
			if (Math.Abs(d) < 0.01)
				return Vector2.zero;

			var xi = ((lineTwoStart.x - lineTwoEnd.x) * (lineOneStart.x * lineOneEnd1.y - lineOneStart.y * lineOneEnd1.x) - (lineOneStart.x - lineOneEnd1.x) * (lineTwoStart.x * lineTwoEnd.y - lineTwoStart.y * lineTwoEnd.x)) / d;
			var yi = ((lineTwoStart.y - lineTwoEnd.y) * (lineOneStart.x * lineOneEnd1.y - lineOneStart.y * lineOneEnd1.x) - (lineOneStart.y - lineOneEnd1.y) * (lineTwoStart.x * lineTwoEnd.y - lineTwoStart.y * lineTwoEnd.x)) / d;

			var p = new Vector2(xi, yi);
			if (xi < Math.Min(lineOneStart.x, lineOneEnd1.x) || xi > Math.Max(lineOneStart.x, lineOneEnd1.x))
				return Vector2.zero;

			if (xi < Math.Min(lineTwoStart.x, lineTwoEnd.x) || xi > Math.Max(lineTwoStart.x, lineTwoEnd.x))
				return Vector2.zero;

			return p;
		}
	}

	public static class SupplyDropBehaviorService
	{
		public static void AttachCustomBehaviorsToSupplyDrops(AirdropSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			var supplyDrops = UnityEngine.Object.FindObjectsOfType<SupplyDrop>();

			foreach (var supplyDrop in supplyDrops)
			{
				var despawnBehavior = supplyDrop.GetComponent<SupplyDropDespawnBehavior>();
				if (Equals(despawnBehavior, null))
					SupplyDropDespawnBehavior.AddTo(supplyDrop, settings);

				despawnBehavior = supplyDrop.GetComponent<SupplyDropDespawnBehavior>();
				var hasParachute = HasParachute(supplyDrop);

				var collisionBehavior = supplyDrop.GetComponent<SupplyDropLandedBehavior>();
				if (hasParachute && Equals(collisionBehavior, null))
					SupplyDropLandedBehavior.AddTo(supplyDrop, settings);

				if (hasParachute)
					continue;

				despawnBehavior.Despawn();
			}
		}

		private static bool HasParachute(SupplyDrop supplyDrop)
		{
			var parachuteField = typeof(SupplyDrop).GetField("parachute",
				Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Rust>().PrivateBindingFlag());
			if (parachuteField == null)
				return false;

			return parachuteField.GetValue(supplyDrop) != null;
		}

		public static void RemoveCustomBehaviorsFromSupplyDrops()
		{
			var despawnBehaviors = UnityEngine.Object.FindObjectsOfType<SupplyDropDespawnBehavior>();
			if (despawnBehaviors != null && despawnBehaviors.Any())
			{
				foreach (var despawnBehavior in despawnBehaviors)
					UnityEngine.Object.Destroy(despawnBehavior);
			}

			var landBehaviors = UnityEngine.Object.FindObjectsOfType<SupplyDropLandedBehavior>();
			if (landBehaviors != null && landBehaviors.Any())
			{
				foreach (var landBehavior in landBehaviors)
					UnityEngine.Object.Destroy(landBehavior);
			}
		}
	}
}

namespace AirdropExtended.Diagnostics
{
	public static class Diagnostics
	{
		public static string Prefix = "aire:";
		public static string Color = "orange";
		private const string Format = "<color={0}>{1}</color>";

		public static void MessageTo(string message, bool sendToAll, params object[] args)
		{
			if (sendToAll)
				MessageToAll(message, args);
			MessageToServer(message, args);
		}

		public static void MessageToPlayer(BasePlayer player, string message, params object[] args)
		{
			player.SendConsoleCommand("chat.add", new object[] { 0, string.Format(Format, Color, Prefix) + string.Format(message, args), 1f });
		}

		public static void MessageToAll(string message, params object[] args)
		{
			var msg = string.Format(Format, Color, Prefix) + string.Format(message, args);
			ConsoleSystem.Broadcast("chat.add \"SERVER\" " + msg.Quote() + " 1.0", new object[0]);
		}

		public static void MessageToServer(string message, params object[] args)
		{
			Interface.GetMod().RootLogger.Write(LogType.Info, string.Format("{0}{1}", Prefix, message), args);
		}

		public static void MessageToServerAndPlayer(BasePlayer player, string message, params object[] args)
		{
			if (player != null)
				MessageToPlayer(player, message, args);
			MessageToServer(message, args);
		}
	}
}

namespace AirdropExtended.Permissions
{
	public static class PermissionService
	{
		public static Permission Permission = Interface.GetMod().GetLibrary<Permission>();

		public static bool HasPermission(BasePlayer player, string permissionName)
		{
			if (player == null || string.IsNullOrEmpty(permissionName))
				return false;

			var uid = Convert.ToString(player.userID);
			if (Permission.UserHasPermission(uid, permissionName))
				return true;

			return false;
		}

		public static void RegisterPermissions(Plugin owner, List<AirdropExtendedCommand> commands)
		{
			if (owner == null) throw new ArgumentNullException("owner");
			if (commands == null) throw new ArgumentNullException("commands");

			foreach (var permissionName in commands.Select(c => c.PermissionName))
			{
				if (!Permission.PermissionExists(permissionName))
					Permission.RegisterPermission(permissionName, owner);
			}
		}
	}
}

namespace AirdropExtended.Commands
{
	public abstract class AirdropExtendedCommand
	{
		public string Name { get; private set; }
		public string PermissionName { get; private set; }
		public bool IsChatOnly { get; private set; }

		protected AirdropExtendedCommand(string name, string permissionName = "", bool chatOnly = false)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

			Name = name;
			PermissionName = permissionName;
			IsChatOnly = chatOnly;
		}

		public virtual void ExecuteFromChat(BasePlayer player, string command, string[] args)
		{
			if (player != null && !PermissionService.HasPermission(player, PermissionName) && !player.IsAdmin())
			{
				Diagnostics.Diagnostics.MessageToPlayer(player, "You are not admin. You are required to have permission \"{0}\" to run command: {1}", PermissionName, Name);
				return;
			}

			var commandString = args.Aggregate(command, (s, s1) => s + " " + s1.Quote());
			Diagnostics.Diagnostics.MessageToServer("'{0}' called by {1}", commandString, player.displayName);
			var commandArgs = new ConsoleSystem.Arg(commandString);
			ExecuteInternal(commandArgs, player);
		}

		public virtual void Execute(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (player != null && !PermissionService.HasPermission(player, PermissionName) && !player.IsAdmin())
			{
				Diagnostics.Diagnostics.MessageToPlayer(player, "You are not admin. You are required to have permission \"{0}\" to run command: {1}", PermissionName, Name);
				return;
			}

			ExecuteInternal(arg, player);
		}

		protected abstract void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player);

		protected void PrintUsage(BasePlayer player)
		{
			var message = GetUsageString();
			if (player != null)
				Diagnostics.Diagnostics.MessageToPlayer(player, message);
			Diagnostics.Diagnostics.MessageToServer(message);
		}

		protected virtual string GetUsageString()
		{
			return GetDefaultUsageString();
		}

		protected string GetDefaultUsageString(params string[] parameters)
		{
			var parameterString = string.Join(" ", parameters);
			return string.Format("Command use {0} {1}", Name, parameterString);
		}
	}

	public class LoadSettingsCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;
		private readonly PluginSettingsRepository _repository;

		public LoadSettingsCommand(
			SettingsContext context,
			PluginSettingsRepository repository,
			AirdropController controller)
			: base("aire.load", "aire.canLoad")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (repository == null) throw new ArgumentNullException("repository");
			if (controller == null) throw new ArgumentNullException("controller");

			_context = context;
			_repository = repository;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var settingsName = arg.GetString(0);
			if (string.IsNullOrEmpty(settingsName))
			{
				PrintUsage(player);
				return;
			}

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Loading settings: {0}", settingsName);

			_context.SettingsName = settingsName;
			_context.Settings = AidropSettingsRepository.LoadFrom(settingsName);
			_repository.SaveSettingsName(_context.SettingsName);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("settingsName");
		}
	}

	public class ReloadSettingsCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly PluginSettingsRepository _repository;
		private readonly AirdropController _controller;

		public ReloadSettingsCommand(
			SettingsContext context,
			PluginSettingsRepository repository,
			AirdropController controller)
			: base("aire.reload", "aire.canReload")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (repository == null) throw new ArgumentNullException("repository");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_repository = repository;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Reloading plugin");

			_context.SettingsName = _repository.LoadSettingsName();
			_context.Settings = AidropSettingsRepository.LoadFrom(_context.SettingsName);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("settingsName");
		}
	}

	public class SaveSettingsCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly PluginSettingsRepository _pluginSettingsRepository;

		public SaveSettingsCommand(SettingsContext context, PluginSettingsRepository pluginSettingsRepository)
			: base("aire.save", "aire.canSave")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (pluginSettingsRepository == null) throw new ArgumentNullException("pluginSettingsRepository");
			_context = context;
			_pluginSettingsRepository = pluginSettingsRepository;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			var settingsName = arg.HasArgs()
				? arg.GetString(0)
				: _pluginSettingsRepository.LoadSettingsName();

			if (string.IsNullOrEmpty(settingsName))
			{
				PrintUsage(player);
				return;
			}

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Saving settings to: {0}", settingsName);

			_pluginSettingsRepository.SaveSettingsName(settingsName);
			AidropSettingsRepository.SaveTo(settingsName, _context.Settings);
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("settingsName");
		}
	}

	public class GenerateDefaultSettingsAndSaveCommand : AirdropExtendedCommand
	{
		public GenerateDefaultSettingsAndSaveCommand()
			: base("aire.generate", "aire.canGenerate")
		{ }

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var settingsName = arg.GetString(0);
			if (string.IsNullOrEmpty(settingsName))
			{
				PrintUsage(player);
				return;
			}

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Generating default settings to {0}", settingsName);

			var settings = AirdropSettingsFactory.CreateDefault();
			AidropSettingsRepository.SaveTo(settingsName, settings);
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("settingsName");
		}
	}

	public class SetDropMinFrequencyCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetDropMinFrequencyCommand(SettingsContext context, AirdropController controller)
			: base("aire.minfreq", "aire.canMinFreq")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var frequency = arg.GetInt(0);
			frequency = frequency < 0 ? 0 : frequency;
			_context.Settings.CommonSettings.MinDropFrequency = TimeSpan.FromSeconds(frequency);

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting min frequency to {0}", frequency);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("3600");
		}
	}

	public class SetDropMaxFrequencyCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetDropMaxFrequencyCommand(SettingsContext context, AirdropController controller)
			: base("aire.maxfreq", "aire.canMaxFreq")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");

			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var frequency = arg.GetInt(0);
			frequency = frequency < 0 ? 0 : frequency;
			_context.Settings.CommonSettings.MaxDropFrequency = TimeSpan.FromSeconds(frequency);

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting max frequency to {0}", frequency);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("3600");
		}
	}

	public class SetPlayersCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetPlayersCommand(SettingsContext context, AirdropController controller)
			: base("aire.players", "aire.canPlayers")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var players = arg.GetInt(0);
			players = players < 0 ? 0 : players;
			_context.Settings.CommonSettings.MinimumPlayerCount = players;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting min players to {0}", players);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("25");
		}
	}

	public class SetTimerEnabledCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetTimerEnabledCommand(SettingsContext context, AirdropController controller)
			: base("aire.timer", "aire.canTimer")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var pluginAirdropTimerEnabled = arg.GetBool(0);

			_context.Settings.CommonSettings.PluginAirdropTimerEnabled = pluginAirdropTimerEnabled;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting plugin timer enabled to {0}", pluginAirdropTimerEnabled);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("true");
		}
	}

	public class SetEventEnabledCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetEventEnabledCommand(SettingsContext context, AirdropController controller)
			: base("aire.event", "aire.canEvent")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var builtInAirdropEnabled = arg.GetBool(0);

			_context.Settings.CommonSettings.BuiltInAirdropEnabled = builtInAirdropEnabled;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting built in airdrop enabled to {0}", builtInAirdropEnabled);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("true");
		}
	}

	public class SetSupplyDropEnabledCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetSupplyDropEnabledCommand(SettingsContext context, AirdropController controller)
			: base("aire.supply", "aire.canSupply")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var supplyDropTimerEnabled = arg.GetBool(0);

			_context.Settings.CommonSettings.SupplySignalsEnabled = supplyDropTimerEnabled;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting supply signals enabled to {0}", supplyDropTimerEnabled);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("true");
		}
	}

	public class SetPlaneSpeedCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetPlaneSpeedCommand(SettingsContext context, AirdropController controller)
			: base("aire.planespeed", "aire.canPlaneSpeed")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var planeSpeedInSeconds = arg.GetInt(0);
			_context.Settings.CommonSettings.PlaneSpeedInSeconds = planeSpeedInSeconds;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting plane speed in seconds to {0}", planeSpeedInSeconds);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("300");
		}
	}

	public class SetCrateCountCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetCrateCountCommand(SettingsContext context, AirdropController controller)
			: base("aire.crates", "aire.canCrates")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var minCrates = arg.GetInt(0);
			var maxCrates = arg.HasArgs(2) ? arg.GetInt(1) : minCrates;
			_context.Settings.CommonSettings.MinCrates = minCrates;
			_context.Settings.CommonSettings.MaxCrates = maxCrates;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting min/max crates to {0}-{1}", minCrates, maxCrates);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("300");
		}
	}

	public class SetDropToOneLocationCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetDropToOneLocationCommand(SettingsContext context, AirdropController controller)
			: base("aire.onelocation", "aire.canOneLocation")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var oneLocation = arg.GetBool(0);
			_context.Settings.CommonSettings.DropToOneLocation = oneLocation;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting drop to one location to {0}", oneLocation);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("true");
		}
	}

	public class SetPlaneLimitEnabledCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetPlaneLimitEnabledCommand(SettingsContext context, AirdropController controller)
			: base("aire.enableplanelimit", "aire.canEnablePlaneLimit")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var maximumPlaneLimitEnabled = arg.GetBool(0);

			_context.Settings.CommonSettings.MaximumPlaneLimitEnabled = maximumPlaneLimitEnabled;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting plane limit enabled to {0}", maximumPlaneLimitEnabled);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("true");
		}
	}

	public class SetPlaneLimitCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetPlaneLimitCommand(SettingsContext context, AirdropController controller)
			: base("aire.planelimit", "aire.canPlaneLimit")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var planeLimit = arg.GetInt(0);
			planeLimit = planeLimit < 0 ? 0 : planeLimit;
			_context.Settings.CommonSettings.MaximumNumberOfPlanesInTheAir = planeLimit;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting plane in air limit to {0}", planeLimit);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("10");
		}
	}

	public class SetDespawnTimeCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetDespawnTimeCommand(SettingsContext context, AirdropController controller)
			: base("aire.despawntime", "aire.canDespawnTime")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var despawnTimeInSeconds = arg.GetInt(0);
			despawnTimeInSeconds = despawnTimeInSeconds < 0 ? 0 : despawnTimeInSeconds;
			_context.Settings.CommonSettings.SupplyCrateDespawnTime = TimeSpan.FromSeconds(despawnTimeInSeconds);

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Set SupplyCrateDespawnTime to {0}", _context.Settings.CommonSettings.SupplyCrateDespawnTime);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("300");
		}
	}

	public class SetItemSettingsCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;
		private readonly string _usageString;

		public SetItemSettingsCommand(SettingsContext context, AirdropController controller)
			: base("aire.setitem", "aire.canSetItem")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;

			var usageStrings = new[]
			{
				GetDefaultUsageString("item_name [category] [chance] [min] [max] [is_blueprint]"),
				string.Format("Example: {0} Weapon rocket_launcher 15 1 1 false.", Name),
				"default chance=0, min=0, max=0, is_blueprint=false."
			};

			_usageString = string.Join(Environment.NewLine, usageStrings);
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			string categoryName;
			string itemName;
			float chance;
			int minAmount;
			int maxAmount;
			bool isBlueprint;

			try
			{
				categoryName = arg.GetString(0);
				itemName = arg.GetString(1);
				chance = arg.GetFloat(2);
				minAmount = arg.GetInt(3);
				maxAmount = arg.GetInt(4);
				isBlueprint = arg.GetBool(5);
			}
			catch (Exception)
			{
				PrintUsage(player);
				return;
			}

			var item = _context.Settings.FindItem(categoryName, itemName, isBlueprint);

			item.ChanceInPercent = chance;
			item.MinAmount = minAmount;
			item.MaxAmount = maxAmount;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player,
			"Set settings to item:{0}, chance:{1}, min_amount:{2}, max_amount:{3}, is blueprint:{4}",
			itemName,
			chance,
			minAmount,
			maxAmount,
			isBlueprint);

			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return _usageString;
		}
	}

	public class SetItemGroupSettingsCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;
		private readonly string _usageString;

		public SetItemGroupSettingsCommand(SettingsContext context, AirdropController controller)
			: base("aire.setitemgroup", "aire.canSetItemGroup")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;

			var usageStrings = new[]
			{
				GetDefaultUsageString("group_name 2"),
				string.Format("Example: {0} Attire 2", Name)
			};

			_usageString = string.Join(Environment.NewLine, usageStrings);
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			string groupName;
			int maxAmount;

			try
			{
				groupName = arg.GetString(0);
				if (string.IsNullOrEmpty(groupName))
				{
					PrintUsage(player);
					return;
				}

				maxAmount = arg.GetInt(1);
			}
			catch (Exception)
			{
				PrintUsage(player);
				return;
			}

			var airdropItemGroup = _context.Settings.ItemGroups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
			if (airdropItemGroup == null)
			{
				Diagnostics.Diagnostics.MessageToServerAndPlayer(player, " command {0} error - group not found", Name);
				return;
			}

			airdropItemGroup.MaximumAmountInLoot = maxAmount;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player,
				"Set item group {0}, max_amount:{1}",
				groupName,
				maxAmount);

			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return _usageString;
		}
	}

	public class SetAirdropCapacityCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetAirdropCapacityCommand(SettingsContext context, AirdropController controller)
			: base("aire.capacity", "aire.canCapacity")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var capacity = arg.GetInt(0);
			capacity = capacity < 0 ? 0 : capacity;
			_context.Settings.Capacity = capacity;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting airdrop capacity to {0}", capacity);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("18");
		}
	}

	public class CallRandomDropCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;

		public CallRandomDropCommand(SettingsContext context)
			: base("aire.drop", "aire.canDrop")
		{
			if (context == null) throw new ArgumentNullException("context");
			_context = context;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Calling random drop");
			AirdropService.CallRandomDrop(_context.Settings.DropLocation);
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString();
		}
	}

	public class CallMassDropCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;

		public CallMassDropCommand(SettingsContext context)
			: base("aire.massdrop", "aire.canMassDrop")
		{
			if (context == null) throw new ArgumentNullException("context");
			_context = context;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			var planeCount = arg.GetInt(0, 3);

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Calling mass drop, number of planes:{0}", planeCount);
			for (int i = 0; i < planeCount; i++)
				AirdropService.CallRandomDrop(_context.Settings.DropLocation);
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("3");
		}
	}

	public class CallToPosCommand : AirdropExtendedCommand
	{
		private static readonly string[] UsageStrings = { "aire.topos x z", "aire.topos x;z", "aire.topos x,z" };
		private static readonly char[] Separators = { ';', ',' };

		public CallToPosCommand()
			: base("aire.topos", "aire.canDropToPos")
		{ }

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			int x, z;
			if (!arg.HasArgs(2))
			{
				var argz = arg.GetString(0);
				var coordinates = argz.Split(Separators);
				if (coordinates.Length < 2)
				{
					PrintUsage(player);
					return;
				}

				x = int.Parse(coordinates[0]);
				z = int.Parse(coordinates[1]);
			}
			else
			{
				x = arg.GetInt(0);
				z = arg.GetInt(1);
			}

			AirdropService.CallToPos(new Vector3(x, 300.0f, z));
		}

		protected override string GetUsageString()
		{
			return string.Join(Environment.NewLine, UsageStrings);
		}
	}

	public class CallToPlayerCommand : AirdropExtendedCommand
	{
		private static readonly string[] UsageStrings = { "aire.toplayer steamId", "aire.toplayer nickname" };

		public CallToPlayerCommand()
			: base("aire.toplayer", "aire.canDropToPlayer")
		{ }

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			ulong steamId;
			var parameter = arg.GetString(0);
			var activePlayerList = BasePlayer.activePlayerList;

			var playerToDropTo = ulong.TryParse(parameter, out steamId)
				? activePlayerList.FirstOrDefault(p => p.userID.Equals(steamId))
				: activePlayerList.FirstOrDefault(p => p.displayName.Equals(parameter, StringComparison.OrdinalIgnoreCase));

			if (playerToDropTo == null)
			{
				Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Player to drop to was not found!");
				return;
			}
			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Dropping to player:{0}!", playerToDropTo.displayName);
			AirdropService.CallToPos(playerToDropTo.transform.position);
		}

		protected override string GetUsageString()
		{
			return string.Join(Environment.NewLine, UsageStrings);
		}
	}

	public class CallToMeCommand : AirdropExtendedCommand
	{
		public CallToMeCommand()
			: base("aire.tome", "aire.canDropToMe", true)
		{ }

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (player == null)
				return;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Dropping to player:{0}!", player.displayName);
			AirdropService.CallToPos(player.transform.position);
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString();
		}
	}

	public class LocalizeCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		private static readonly Type LocalizationType = typeof(LocalizationSettings);

		public LocalizeCommand(SettingsContext context, AirdropController controller)
			: base("aire.localize", "aire.canLocalize")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs(2))
			{
				PrintUsage(player);
				return;
			}

			var message = arg.GetString(0);
			var text = arg.GetString(1);

			var messagePropertyInfo = LocalizationType.GetProperty(message);
			if (messagePropertyInfo == null)
			{
				Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Couldn't find message setting: {0}", message);
				return;
			}
			if (messagePropertyInfo.PropertyType != typeof(String))
			{
				Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Property is not of string type: {0}", message);
				return;
			}
			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting message {0} to {1}", message, text);
			messagePropertyInfo.SetValue(_context.Settings.Localization, text, null);

			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("18");
		}
	}

	public class SetNotifyEnabledCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		private static readonly Type LocalizationType = typeof(CommonSettings);

		public SetNotifyEnabledCommand(SettingsContext context, AirdropController controller)
			: base("aire.notify", "aire.canNotify")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs(2))
			{
				PrintUsage(player);
				return;
			}

			var switchName = arg.GetString(0);
			var value = arg.GetBool(1);

			var notifySwitchPropertyInfo = LocalizationType.GetProperty(switchName);
			if (notifySwitchPropertyInfo == null)
			{
				Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Couldn't find notify setting: {0}", switchName);
				return;
			}

			if (notifySwitchPropertyInfo.PropertyType != typeof(bool))
			{
				Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Property is not of bool type: {0}", switchName);
				return;
			}

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting {0} to {1}", switchName, value);
			notifySwitchPropertyInfo.SetValue(_context.Settings.CommonSettings, value, null);

			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("18");
		}
	}

	public class SetCustomLootEnabledCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetCustomLootEnabledCommand(SettingsContext context, AirdropController controller)
			: base("aire.customloot", "aire.canSetCustomLoot")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var customLoot = arg.GetBool(0);

			_context.Settings.CustomLootEnabled = customLoot;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting custom loot enabled to {0}", customLoot);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("false");
		}
	}

	public class SetPickStrategyCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;
		private readonly AirdropController _controller;

		public SetPickStrategyCommand(SettingsContext context, AirdropController controller)
			: base("aire.pick", "aire.canSetPickStrategy")
		{
			if (context == null) throw new ArgumentNullException("context");
			if (controller == null) throw new ArgumentNullException("controller");
			_context = context;
			_controller = controller;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			if (!arg.HasArgs())
			{
				PrintUsage(player);
				return;
			}

			var pickStrategy = arg.GetInt(0);
			if (!(Enum.IsDefined(typeof(PickStrategy), pickStrategy)))
			{
				Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Value {0} is not applicable.", pickStrategy);
				return;
			}

			_context.Settings.PickStrategy = (PickStrategy)pickStrategy;

			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Setting Pick Strategy to {0}", pickStrategy);
			_controller.ApplySettings();
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("true");
		}
	}

	public class PrintTestDropContentsCommand : AirdropExtendedCommand
	{
		private readonly SettingsContext _context;

		public PrintTestDropContentsCommand(SettingsContext context)
			: base("aire.test", "aire.canTest")
		{
			if (context == null) throw new ArgumentNullException("context");
			_context = context;
		}

		protected override void ExecuteInternal(ConsoleSystem.Arg arg, BasePlayer player)
		{
			var itemList = _context.Settings.CreateItemList();
			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "Test airdrop crate contents:");
			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "================================================");
			foreach (var item in itemList)
			{
				var itemName = item.info.displayName.english;
				Diagnostics.Diagnostics.MessageToServerAndPlayer(
					player,
					"Item: |{0,20}|, bp: {1}, count: {2}",
					itemName.Substring(0, Math.Min(itemName.Length, 18)),
					item.HasFlag(Item.Flag.Blueprint),
					item.amount);
			}
			Diagnostics.Diagnostics.MessageToServerAndPlayer(player, "================================================");
		}

		protected override string GetUsageString()
		{
			return GetDefaultUsageString("true");
		}
	}

	public static class CommandFactory
	{
		public static List<AirdropExtendedCommand> Create(
			SettingsContext context,
			PluginSettingsRepository settingsRepository,
			AirdropController controller)
		{
			if (context == null) throw new ArgumentNullException("context");
			if (settingsRepository == null) throw new ArgumentNullException("settingsRepository");
			if (controller == null) throw new ArgumentNullException("controller");

			return new List<AirdropExtendedCommand>
				{
					new LoadSettingsCommand(context, settingsRepository, controller),
					new ReloadSettingsCommand(context, settingsRepository, controller),
					new SaveSettingsCommand(context, settingsRepository),
					new GenerateDefaultSettingsAndSaveCommand(),
					new SetDropMinFrequencyCommand(context, controller),
					new SetDropMaxFrequencyCommand(context, controller),
					new SetPlayersCommand(context, controller),
					new SetEventEnabledCommand(context, controller),
					new SetTimerEnabledCommand(context, controller),
					new SetSupplyDropEnabledCommand(context, controller),
					new SetDespawnTimeCommand(context, controller),
					new SetPlaneLimitCommand(context, controller),
					new SetPlaneLimitEnabledCommand(context, controller),
					new SetPlaneSpeedCommand(context, controller),
					new SetCrateCountCommand(context, controller),
					new SetDropToOneLocationCommand(context, controller),
					new SetItemSettingsCommand(context, controller),
					new SetItemGroupSettingsCommand(context, controller),
					new SetAirdropCapacityCommand(context, controller),
					new CallRandomDropCommand(context),
					new CallMassDropCommand(context),
					new CallToPosCommand(),
					new CallToPlayerCommand(),
					new CallToMeCommand(),
					new LocalizeCommand(context, controller),
					new SetNotifyEnabledCommand(context, controller),
					new SetCustomLootEnabledCommand(context, controller),
					new SetPickStrategyCommand(context, controller),
					new PrintTestDropContentsCommand(context)
				};
		}
	}
}

namespace AirdropExtended.PluginSettings
{
	public sealed class PluginSettingsRepository
	{
		private readonly DynamicConfigFile _config;
		private readonly Action _saveConfigDelegate;

		public PluginSettingsRepository(DynamicConfigFile config, Action saveConfigDelegate)
		{
			if (config == null) throw new ArgumentNullException("config");
			if (saveConfigDelegate == null) throw new ArgumentNullException("saveConfigDelegate");
			_config = config;
			_saveConfigDelegate = saveConfigDelegate;
		}

		private const string DefaultSettingsName = "defaultSettings";

		public string LoadSettingsName(string defaultName = DefaultSettingsName)
		{
			string settingsName;
			try
			{
				settingsName = (string)_config["settingsName"];
			}
			catch (Exception)
			{
				settingsName = string.Empty;
			}

			settingsName = string.IsNullOrEmpty(settingsName) ? defaultName : settingsName;
			return settingsName;
		}

		public void SaveSettingsName(string settingsName)
		{
			if (string.IsNullOrEmpty(settingsName))
				settingsName = DefaultSettingsName;

			_config["settingsName"] = settingsName;
			_saveConfigDelegate();
		}
	}
}

namespace AirdropExtended.WeightedSearch
{
	public static class Algorithms
	{
		public static int BinarySearchClosestIndex<T>(List<T> inputArray, Func<T, float> selector, float number)
		{
			if (inputArray.Count == 0)
				return -1;

			var left = 0;
			var right = inputArray.Count - 1;
			//find the closest range
			while ((right - left) > 1)
			{
				var mid = left + (right - left) / 2;

				if (selector(inputArray[mid]) > number)
					right = mid;
				else
					left = mid;
			}

			var diffWithLeft = number - selector(inputArray[left]);
			var diffWithRight = selector(inputArray[right]) - number;

			//closest is the one with the lesser difference
			var result = diffWithLeft < diffWithRight
				? left
				: right;

			return result;
		}
	}

	public struct Weighted<T>
	{
		public T Value { get; set; }
		public float Weight { get; set; }
	}
}

namespace AirdropExtended.Airdrop.Services
{
	internal static class AirdropService
	{
		public static void CallRandomDrop(DropLocationSettings locationSettings)
		{
			if (locationSettings == null) throw new ArgumentNullException("locationSettings");

			var plane = CargoPlaneFactory.CreatePlane(locationSettings.GetRandomPosition());
			plane.Spawn();
		}

		public static void CallToPos(Vector3 position)
		{
			var plane = CargoPlaneFactory.CreatePlane(position);
			plane.Spawn();
		}
	}

	internal sealed class CargoPlaneData
	{
		private readonly Vector3 _targetPos;
		private readonly float _secondsToTake;

		public CargoPlaneData(Vector3 targetPos, float secondsToTake)
		{
			_targetPos = targetPos;
			_secondsToTake = secondsToTake;
		}

		public Vector3 TargetPos
		{
			get { return _targetPos; }
		}

		public float SecondsToTake
		{
			get { return _secondsToTake; }
		}
	}

	internal static class CargoPlaneFields
	{
		public static readonly FieldInfo DropPositionField = typeof(CargoPlane).GetField("dropPosition", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
		public static readonly FieldInfo SecondsToTakeField = typeof(CargoPlane).GetField("secondsToTake", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
		public static readonly FieldInfo SecondsTakenField = typeof(CargoPlane).GetField("secondsTaken", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
		public static readonly FieldInfo DroppedField = typeof(CargoPlane).GetField("dropped", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
		public static readonly FieldInfo EndPositionField = typeof(CargoPlane).GetField("endPos", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
		public static readonly FieldInfo StartPositionField = typeof(CargoPlane).GetField("startPos", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
	}

	internal sealed class CargoPlaneFactory
	{
		public static CargoPlane CreatePlane(Vector3 position)
		{
			var plane = (CargoPlane)GameManager.server.CreateEntity(
				Constants.CargoPlanePrefab,
				new Vector3(),
				new Quaternion());

			plane.InitDropPosition(position);
			return plane;
		}

		public static CargoPlane CreatePlane(CargoPlaneData data)
		{
			var plane = (CargoPlane)GameManager.server.CreateEntity(
				Constants.CargoPlanePrefab,
				new Vector3(),
				new Quaternion());
			if (plane == null)
				return null;

			CargoPlaneFields.DropPositionField.SetValue(plane, data.TargetPos);

			return plane;
		}

		public static CargoPlaneData CreateData(CargoPlane plane)
		{
			var dropPositionObject = CargoPlaneFields.DropPositionField.GetValue(plane);
			var secondsToTakeObject = CargoPlaneFields.SecondsToTakeField.GetValue(plane);

			return new CargoPlaneData((Vector3)dropPositionObject, (float)secondsToTakeObject);
		}
	}

	public sealed class AirdropTimerService
	{
		public static TimeSpan DefaultTimerInterval = TimeSpan.FromHours(1);

		private Timer.TimerInstance _aidropTimer;
		private AirdropSettings _settings;

		public void StartAirdropTimer(AirdropSettings settings)
		{
			if (settings == null) throw new ArgumentNullException("settings");
			_settings = settings;

			if (!settings.CommonSettings.PluginAirdropTimerEnabled)
				return;

			SetupNextTick();
		}

		private void SetupNextTick()
		{
			StopAirdropTimer();

			var minDropFreq = Convert.ToSingle(_settings.CommonSettings.MinDropFrequency.TotalSeconds);
			var maxDropFreq = Convert.ToSingle(_settings.CommonSettings.MaxDropFrequency.TotalSeconds);

			var delay = Oxide.Core.Random.Range(minDropFreq, maxDropFreq);

			Diagnostics.Diagnostics.MessageToServer("next airdrop in:{0}", delay);
			_aidropTimer = Interface.GetMod().GetLibrary<Timer>().Once(delay, Tick);
		}

		private void Tick()
		{
			var playerCount = BasePlayer.activePlayerList.Count;
			if (playerCount >= _settings.CommonSettings.MinimumPlayerCount)
			{
				Diagnostics.Diagnostics.MessageToServer("running timed airdrop");

				AirdropService.CallRandomDrop(_settings.DropLocation);
			}
			else
			{
				Diagnostics.Diagnostics.MessageTo(
					_settings.Localization.NotifyOnPlaneRemovedMessage,
					_settings.CommonSettings.NotifyOnPlaneRemoved,
					playerCount);
			}

			SetupNextTick();
		}

		public void StopAirdropTimer()
		{
			if (_aidropTimer == null || _aidropTimer.Destroyed)
				return;

			_aidropTimer.Destroy();
			_aidropTimer = null;
		}
	}

	public sealed class CargoPlaneLaunchedEventArgs : EventArgs
	{
		public CargoPlaneLaunchedEventArgs(CargoPlane plane)
		{
			Plane = plane;
		}

		public CargoPlane Plane { get; set; }
	}

	public sealed class CargoPlaneQueueService
	{
		private const int DelayBetweenPlaneTryLaunch = 5000;
		private readonly List<CargoPlane> _planesInAir = new List<CargoPlane>();
		private readonly Queue<CargoPlaneData> _planesInQueue = new Queue<CargoPlaneData>();

		private int _maximumNumberOfPlanesInTheAir = CommonSettings.DefaultMaximumNumberOfPlanesInTheAir;

		public EventHandler<CargoPlaneLaunchedEventArgs> OnPlaneLaunched = (sender, args) => { };
		private bool _isEnabled;

		public void UpdateQueue(CommonSettings settings)
		{
			if (settings == null) throw new ArgumentNullException("settings");

			_isEnabled = settings.MaximumPlaneLimitEnabled;
			Diagnostics.Diagnostics.MessageToServer("Plane limit enabled:{0}", _isEnabled);
			if (!settings.MaximumPlaneLimitEnabled)
			{
				ClearQueueAndSetDefault();
				return;
			}

			var currentSettingValue = settings.MaximumNumberOfPlanesInTheAir;
			AdjustNumberOfPlanesInAir(currentSettingValue);

			_maximumNumberOfPlanesInTheAir = currentSettingValue;
		}

		private void ClearQueueAndSetDefault()
		{
			var count = _planesInQueue.Count;
			if (count > 0)
			{
				for (int i = 0; i < count; i++)
					LaunchPlaneFromQueue();
			}

			_planesInAir.Clear();
			_planesInQueue.Clear();
			_maximumNumberOfPlanesInTheAir = 0;
		}

		private void AdjustNumberOfPlanesInAir(int currentSettingValue)
		{

			var currentPlanesInAir = UnityEngine.Object.FindObjectsOfType<CargoPlane>() ?? Enumerable.Empty<CargoPlane>().ToArray();

			var numberOfAvailablePlanesToLaunch = currentSettingValue - currentPlanesInAir.Length;
			if (numberOfAvailablePlanesToLaunch > 0)
			{
				for (int i = 0; i < numberOfAvailablePlanesToLaunch; i++)
					LaunchPlaneFromQueue();
			}

			currentPlanesInAir = UnityEngine.Object.FindObjectsOfType<CargoPlane>() ?? Enumerable.Empty<CargoPlane>().ToArray();
			_planesInAir.Clear();
			_planesInAir.AddRange(currentPlanesInAir);

			foreach (var cargoPlane in currentPlanesInAir)
			{
				if (cargoPlane.GetComponent<PlaneNotifyOnDestroyBehavior>() == null)
					AddDestroyBehavior(cargoPlane);
			}
		}

		public void Enqueue(CargoPlane plane)
		{
			if (plane == null) throw new ArgumentNullException("plane");

			if (_isEnabled)
			{
				if (_planesInAir.Count >= _maximumNumberOfPlanesInTheAir)
				{
					var planeData = CargoPlaneFactory.CreateData(plane);
					_planesInQueue.Enqueue(planeData);
					plane.KillMessage();
					return;
				}

				AddDestroyBehavior(plane);
				_planesInAir.Add(plane);
			}
			RaiseCargoPlaneLaunchedEvent(plane);
		}

		private void AddDestroyBehavior(CargoPlane plane)
		{
			plane.gameObject.AddComponent<PlaneNotifyOnDestroyBehavior>();
			plane.GetComponent<PlaneNotifyOnDestroyBehavior>().Callback = OnPlaneDestroyed;
		}

		private void RaiseCargoPlaneLaunchedEvent(CargoPlane plane)
		{
			if (OnPlaneLaunched != null)
				OnPlaneLaunched(this, new CargoPlaneLaunchedEventArgs(plane));
		}

		private void OnPlaneDestroyed(CargoPlane plane)
		{
			if (plane == null) throw new ArgumentNullException("plane");

			_planesInAir.Remove(plane);
			LaunchPlaneFromQueue();
		}

		private void LaunchPlaneFromQueue()
		{
			if (_planesInQueue.Count <= 0)
				return;

			var cargoPlaneData = _planesInQueue.Peek();
			if (cargoPlaneData == null)
				return;

			var plane = CargoPlaneFactory.CreatePlane(cargoPlaneData);
			if (plane == null)
			{
				Interface.GetMod().GetLibrary<Timer>().Once(DelayBetweenPlaneTryLaunch, LaunchPlaneFromQueue);
			}
			else
			{
				_planesInQueue.Dequeue();
				plane.Spawn();
			}
		}
	}

	public sealed class AirdropController
	{
		private readonly SettingsContext _context;
		private readonly AirdropTimerService _timerService = new AirdropTimerService();
		private readonly CargoPlaneQueueService _cargoPlaneQueueService = new CargoPlaneQueueService();

		public AirdropController(SettingsContext context)
		{
			if (context == null) throw new ArgumentNullException("context");
			_context = context;
		}

		public bool IsInitialized()
		{
			return _context != null && _context.Settings != null;
		}

		public void ApplySettings()
		{
			var settings = _context.Settings ?? AirdropSettingsFactory.CreateDefault();
			AirdropSettingsValidator.Validate(settings);
			AidropSettingsRepository.SaveTo(_context.SettingsName, settings);

			Diagnostics.Diagnostics.Color = settings.Localization.Color;
			Diagnostics.Diagnostics.Prefix = settings.Localization.Prefix;

			CleanupServices();
			_context.Settings = settings;
			InitializeServices(settings);
		}

		private void CleanupServices()
		{
			_timerService.StopAirdropTimer();
			SupplyDropBehaviorService.RemoveCustomBehaviorsFromSupplyDrops();
			_cargoPlaneQueueService.OnPlaneLaunched -= OnPlaneLaunched;
		}

		private void OnPlaneLaunched(object sender, CargoPlaneLaunchedEventArgs cargoPlaneLaunchedEventArgs)
		{
			var plane = cargoPlaneLaunchedEventArgs.Plane;
			AddMultipleCratesBehavior(plane);
			plane.SetSpeed(_context.Settings.CommonSettings.PlaneSpeedInSeconds);
			Diagnostics.Diagnostics.MessageTo(
				_context.Settings.Localization.NotifyOnPlaneSpawnedMessage,
				_context.Settings.CommonSettings.NotifyOnPlaneSpawned);

			var position = (Vector3)CargoPlaneFields.DropPositionField.GetValue(plane);
			plane.NotifyNextDropPosition(position, _context.Settings);
		}

		private void AddMultipleCratesBehavior(CargoPlane plane)
		{
			plane.gameObject.AddComponent<MultipleCratesBehavior>();
			var cratesBehavior = plane.GetComponent<MultipleCratesBehavior>();

			cratesBehavior.TotalCratesToDrop = Oxide.Core.Random.Range(_context.Settings.CommonSettings.MinCrates, _context.Settings.CommonSettings.MaxCrates + 1);
			cratesBehavior.DropToOneLocation = _context.Settings.CommonSettings.DropToOneLocation;
			cratesBehavior.InitialEndPosition = (Vector3)CargoPlaneFields.EndPositionField.GetValue(plane);
		}

		private void InitializeServices(AirdropSettings settings)
		{
			SetupBuiltInAirdrop();
			SupplyDropBehaviorService.AttachCustomBehaviorsToSupplyDrops(settings);
			_timerService.StartAirdropTimer(settings);
			_cargoPlaneQueueService.OnPlaneLaunched += OnPlaneLaunched;
			_cargoPlaneQueueService.UpdateQueue(settings.CommonSettings);
		}

		public void OnEntitySpawned(BaseEntity entity)
		{
			if (entity == null)
				return;

			var supplyDrop = entity as SupplyDrop;
			if (supplyDrop != null)
				HandleSupplyDrop(supplyDrop);

			var cargoPlane = entity as CargoPlane;
			if (cargoPlane != null)
				_cargoPlaneQueueService.Enqueue(cargoPlane);
		}

		private void HandleSupplyDrop(SupplyDrop entity)
		{
			var supplyDrop = entity.GetComponent<LootContainer>();
			if (supplyDrop == null)
				return;

			var itemContainer = supplyDrop.inventory;
			if (itemContainer == null || itemContainer.itemList == null)
				return;

			if (_context.Settings.CustomLootEnabled)
				FillWithCustomLoot(itemContainer);

			var x = entity.transform.position.x;
			var y = entity.transform.position.y;
			var z = entity.transform.position.z;

			Diagnostics.Diagnostics.MessageTo(_context.Settings.Localization.NotifyOnDropStartedMessage, _context.Settings.CommonSettings.NotifyOnDropStarted, x, y, z);

			SupplyDropLandedBehavior.AddTo(supplyDrop, _context.Settings);
			SupplyDropDespawnBehavior.AddTo(entity, _context.Settings);

			var droppedPlane =
				UnityEngine.Object.FindObjectsOfType<CargoPlane>()
					.FirstOrDefault(p => (bool)CargoPlaneFields.DroppedField.GetValue(p));
			if (droppedPlane == null)
				return;

			var cratesBehavior = droppedPlane.GetComponent<MultipleCratesBehavior>();
			if (cratesBehavior == null)
				return;

			cratesBehavior.OnPlaneDroppedCrate(droppedPlane, _context.Settings, entity.transform.position);
		}

		private void FillWithCustomLoot(ItemContainer itemContainer)
		{
			if (_context == null || _context.Settings == null)
				return;
			itemContainer.itemList.Clear();
			itemContainer.capacity = _context.Settings.Capacity;

			var itemList = _context.Settings.CreateItemList();
			for (var index = 0; index < itemList.Count; index++)
			{
				var item = itemList[index];
				item.MoveToContainer(itemContainer, index);
			}
		}

		private void SetupBuiltInAirdrop()
		{
			var triggeredEvents = UnityEngine.Object.FindObjectsOfType<TriggeredEventPrefab>();
			var planePrefab = triggeredEvents.Where(e => e.targetPrefab != null && e.targetPrefab.guid.Equals("8429b072581d64747bfe17eab7852b42")).ToList();
			foreach (var prefab in planePrefab)
			{
				UnityEngine.Object.Destroy(prefab);
			}

			if (!_context.Settings.CommonSettings.BuiltInAirdropEnabled)
				return;

			var schedule = UnityEngine.Object.FindObjectsOfType<EventSchedule>().First();
			schedule.gameObject.AddComponent<TriggeredEventPrefab>();
			var eventPrefab = schedule.gameObject.GetComponent<TriggeredEventPrefab>();
			eventPrefab.targetPrefab = new GameObjectRef { guid = "8429b072581d64747bfe17eab7852b42" };

			//var schedules = UnityEngine.Object.FindObjectsOfType<EventSchedule>();
			//foreach (var schedule in schedules)
			//{
			//	Diagnostics.Diagnostics.MessageToServer("Disable event schedule#{0}", schedule.GetInstanceID());
			//	schedule.enabled = false;

			//	schedule.CancelInvoke("RunSchedule");
			//	if (_context.Settings.CommonSettings.BuiltInAirdropEnabled)
			//		schedule.InvokeRepeating("RunSchedule", 1f, 1f);
			//}
		}

		public void Cleanup()
		{
			_timerService.StopAirdropTimer();
			SupplyDropBehaviorService.RemoveCustomBehaviorsFromSupplyDrops();
		}

		public void OnSupplySignal(BasePlayer player, BaseEntity entity)
		{
			if (player == null || entity == null)
				return;

			var airdropSettings = _context.Settings;
			var commonSettings = airdropSettings.CommonSettings;

			if (commonSettings.NotifyAboutDirectionAroundOnSupplyThrown || commonSettings.NotifyAboutPlayersAroundOnSupplyThrown)
				NotifyPlayersOnSupplySignal(player, entity, airdropSettings, commonSettings);

			if (commonSettings.SupplySignalsEnabled)
				return;

			entity.KillMessage();

			var signalItem = ItemManager.CreateByName("supply.signal");
			var playerBeltHasEnoughSpace = player.inventory.containerBelt.itemList.Count != player.inventory.containerBelt.capacity;
			var containerToAddItemTo = playerBeltHasEnoughSpace
				? player.inventory.containerBelt
				: player.inventory.containerMain;
			signalItem.MoveToContainer(containerToAddItemTo, -1, false);

			if (commonSettings.NotifyOnSupplySingalDisabled)
				Diagnostics.Diagnostics.MessageToPlayer(player, airdropSettings.Localization.NotifyOnSupplySingalDisabledMessage);
		}

		private static void NotifyPlayersOnSupplySignal(
			BasePlayer player,
			BaseEntity entity,
			AirdropSettings airdropSettings,
			CommonSettings commonSettings)
		{
			var players = BasePlayer.activePlayerList;
			var supplyPosition = entity.transform.position;
			var nearbyPlayers = players.Count(p => Vector3.Distance(p.transform.position, supplyPosition) < airdropSettings.CommonSettings.SupplySignalNotifyMaxDistance);

			foreach (var otherPlayer in players)
			{
				var distance = Vector3.Distance(otherPlayer.transform.position, supplyPosition);
				var dropVector = (supplyPosition - otherPlayer.eyes.position);
				var rotation = Quaternion.LookRotation(dropVector);

				var compassDirection = LocalizationExtensions.GetDirectionsFromAngle(rotation.eulerAngles.y,
					airdropSettings.Localization.Directions);
				if (commonSettings.SupplySignalNotifyMaxDistance < distance)
					continue;

				if (commonSettings.NotifyAboutDirectionAroundOnSupplyThrown && player.userID != otherPlayer.userID)
					Diagnostics.Diagnostics.MessageToPlayer(
						otherPlayer,
						airdropSettings.Localization.NotifyAboutDirectionAroundOnSupplyThrownMessage,
						distance,
						compassDirection);

				if (commonSettings.NotifyAboutPlayersAroundOnSupplyThrown)
					Diagnostics.Diagnostics.MessageToPlayer(
						otherPlayer,
						airdropSettings.Localization.NotifyAboutPlayersAroundOnSupplyThrownMessage,
						nearbyPlayers);
			}
		}
	}
}

namespace AirdropExtended.Airdrop.Settings
{
	public enum PickStrategy
	{
		Capacity,
		GroupSize
	}

	public sealed class AirdropSettings
	{
		public const int MaxCapacity = 18;
		public const int DefaultCapacity = 6;

		private int _capacity = DefaultCapacity;
		private bool _customLootEnabled = true;

		public AirdropSettings()
		{
			Capacity = DefaultCapacity;
			PickStrategy = PickStrategy.Capacity;
			DropLocation = DropLocationSettings.CreateDefault();
			Localization = new LocalizationSettings();
		}

		public int Capacity
		{
			get { return _capacity; }
			set
			{
				_capacity = value > MaxCapacity || value < 0
					? MaxCapacity
					: value;
			}
		}

		public bool CustomLootEnabled
		{
			get { return _customLootEnabled; }
			set { _customLootEnabled = value; }
		}

		public PickStrategy PickStrategy { get; set; }

		public CommonSettings CommonSettings { get; set; }

		public LocalizationSettings Localization { get; set; }

		public DropLocationSettings DropLocation { get; set; }

		public List<AirdropItemGroup> ItemGroups { get; set; }

		public List<Item> CreateItemList()
		{
			var groups = ItemGroups.Where(g => g.CanDrop()).ToList();
			List<Item> items;
			switch (PickStrategy)
			{
				case PickStrategy.Capacity:
					items = CapacityWeightedPick(groups);
					break;
				case PickStrategy.GroupSize:
					items = GroupSizeWeightedPick(groups);
					break;
				default:
					items = CapacityWeightedPick(groups);
					break;
			}
			return items;
		}

		private List<Item> CapacityWeightedPick(IEnumerable<AirdropItemGroup> groups)
		{
			var items = new List<Item>(Capacity);

			var weightedGroups = groups
				.OrderBy(i => i.MaximumAmountInLoot)
				.ToList();

			var groupWeightAccumulator = 0.0f;
			var fractionCapacity = (float)Capacity;
			var groupWeightArray = weightedGroups
				.Aggregate(new List<Weighted<AirdropItemGroup>>(), (list, @group) =>
				{
					groupWeightAccumulator += @group.MaximumAmountInLoot / fractionCapacity;
					list.Add(new Weighted<AirdropItemGroup> { Value = @group, Weight = groupWeightAccumulator });
					return list;
				})
				.ToList();

			for (var pickIteration = 0; items.Count < Capacity; pickIteration++)
			{
				var groupRandomValue = (float)Oxide.Core.Random.Range(0.0d, 1.0d) * groupWeightAccumulator;
				var indexOfGroupToPick = Algorithms.BinarySearchClosestIndex(groupWeightArray, g => g.Weight, groupRandomValue);
				var weightedGroup = weightedGroups[indexOfGroupToPick];

				Item item;
				do
				{
					item = PickItemWeightedOrDefault(weightedGroup);
				} while (item == null);

				items.Add(item);
			}
			return items;
		}

		private static Item PickItemWeightedOrDefault(AirdropItemGroup weightedGroup)
		{
			var itemWeightedArray = weightedGroup.ItemWeightedArray;
			var itemRandomValue = (float)Oxide.Core.Random.Range(0.0d, 1.0d) * weightedGroup.ItemWeightAccumulator;
			var indexOfItemToPick = Algorithms.BinarySearchClosestIndex(weightedGroup.ItemWeightedArray, setting => setting.Weight, itemRandomValue);
			var item = itemWeightedArray[indexOfItemToPick].Value;
			var amount = Oxide.Core.Random.Range(item.MinAmount, item.MaxAmount);
			if (amount == 0)
				return null;

			var i1 = ItemManager.CreateByName(item.Name, amount);
			if (i1 == null)
				return null;

			if (item.IsBlueprint)
				i1.SetFlag(Item.Flag.Blueprint, true);
			return i1;
		}

		private List<Item> GroupSizeWeightedPick(IEnumerable<AirdropItemGroup> groups)
		{
			var list = new List<Item>(Capacity);
			foreach (var group in groups)
			{
				var items = new List<Item>();
				for (var i = 0; items.Count < group.MaximumAmountInLoot; i++)
				{
					Item item;
					do
					{
						item = PickItemWeightedOrDefault(group);
					} while (item == null);

					items.Add(item);
				}

				list.AddRange(items);
			}
			return list;
		}

		public AirdropItem FindItem(string categoryName, string itemName, bool isBlueprint)
		{
			if (categoryName == null) throw new ArgumentNullException("categoryName");
			if (itemName == null) throw new ArgumentNullException("itemName");

			return ItemGroups.Select(@group =>
				@group.ItemSettings.FirstOrDefault(f =>
					f.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) &&
					f.IsBlueprint == isBlueprint))
						.FirstOrDefault(item => item != null);
		}
	}

	public sealed class AirdropItemGroup
	{
		private int _maximumAmountInLoot;
		private bool _canDrop;
		private List<AirdropItem> _itemSettings;
		private List<Weighted<AirdropItem>> _itemWeightedArray;
		private float _itemWeightAccumulator;

		public string Name { get; set; }

		public int MaximumAmountInLoot
		{
			get { return _maximumAmountInLoot; }
			set
			{
				_maximumAmountInLoot = value < 0
					? 0
					: value;
				RefreshCanDrop();
			}
		}

		private void RefreshCanDrop()
		{
			_canDrop = _maximumAmountInLoot > 0 && _itemSettings != null && _itemSettings.Any(i => i.CanDrop());
		}

		public bool CanDrop()
		{
			return _canDrop;
		}

		public List<AirdropItem> ItemSettings
		{
			get { return _itemSettings; }
			set
			{
				_itemSettings = (value ?? new List<AirdropItem>())
					.OrderByDescending(i => i.MaxAmount)
					.ThenByDescending(i => i.ChanceInPercent).ToList();
				RefreshCanDrop();
				RefreshWeightedArray();
			}
		}

		[JsonIgnore]
		public List<Weighted<AirdropItem>> ItemWeightedArray
		{
			get { return _itemWeightedArray; }
		}

		[JsonIgnore]
		public float ItemWeightAccumulator
		{
			get { return _itemWeightAccumulator; }
		}

		private void RefreshWeightedArray()
		{
			_itemWeightAccumulator = 0.0f;
			_itemWeightedArray = ItemSettings
				.OrderByDescending(i => i.ChanceInPercent)
				.Aggregate(new List<Weighted<AirdropItem>>(), (list, itm) =>
				{
					_itemWeightAccumulator = ItemWeightAccumulator + itm.ChanceInPercent;
					list.Add(new Weighted<AirdropItem> { Value = itm, Weight = ItemWeightAccumulator });
					return list;
				})
				.ToList();
		}
	}

	public sealed class AirdropItem
	{
		private float _chanceInPercent;
		private string _name;
		private int _minAmount;
		private int _maxAmount;

		public string Name
		{
			get { return _name; }
			set
			{
				if (string.IsNullOrEmpty(value))
					throw new ArgumentException("Item name should not be null", "value");
				_name = value;
			}
		}

		public float ChanceInPercent
		{
			get { return _chanceInPercent; }
			set { _chanceInPercent = value < 0 ? 0 : value; }
		}

		public int MinAmount
		{
			get { return _minAmount; }
			set { _minAmount = value < 0 ? 0 : value; }
		}

		public int MaxAmount
		{
			get { return _maxAmount; }
			set { _maxAmount = value < 0 ? 0 : value; }
		}

		public bool IsBlueprint { get; set; }

		public bool CanDrop()
		{
			return ChanceInPercent > 0.0f && MaxAmount > 0;
		}
	}

	public sealed class CommonSettings
	{
		public static readonly TimeSpan DefaultDropFrequency = TimeSpan.FromHours(1);
		public const int DefaultMaximumNumberOfPlanesInTheAir = 10;
		public const int DefaultDropNotifyMaxDistance = 300;
		public const int DefaultSupplySignalNotifyMaxDistance = 300;

		private int _maximumNumberOfPlanesInTheAir;
		private int _minCrates = 1;
		private int _maxCrates = 1;

		public Boolean SupplySignalsEnabled { get; set; }
		public Boolean BuiltInAirdropEnabled { get; set; }
		public Boolean PluginAirdropTimerEnabled { get; set; }

		public TimeSpan MinDropFrequency { get; set; }
		public TimeSpan MaxDropFrequency { get; set; }

		public int MinimumPlayerCount { get; set; }
		public TimeSpan SupplyCrateDespawnTime { get; set; }

		public int MinCrates
		{
			get { return _minCrates; }
			set { _minCrates = value < 1 ? 1 : value; }
		}
		public int MaxCrates
		{
			get { return _maxCrates; }
			set { _maxCrates = value < 1 ? 1 : value; }
		}

		public bool DropToOneLocation { get; set; }

		public Boolean NotifyOnPlaneSpawned { get; set; }
		public Boolean NotifyOnNextDropPosition { get; set; }
		public Boolean NotifyOnPlaneRemoved { get; set; }
		public Boolean NotifyOnDropStarted { get; set; }
		public Boolean NotifyOnPlayerLootingStarted { get; set; }
		public Boolean NotifyOnCollision { get; set; }
		public Boolean NotifyOnSupplySingalDisabled { get; set; }
		public Boolean NotifyOnDespawn { get; set; }
		public Boolean MaximumPlaneLimitEnabled { get; set; }
		public Boolean NotifyAboutPlayersAroundOnDropLand { get; set; }
		public Boolean NotifyAboutDirectionAroundOnDropLand { get; set; }

		public Boolean NotifyAboutPlayersAroundOnSupplyThrown { get; set; }
		public Boolean NotifyAboutDirectionAroundOnSupplyThrown { get; set; }

		public int PlaneSpeedInSeconds { get; set; }

		public int MaximumNumberOfPlanesInTheAir
		{
			get { return _maximumNumberOfPlanesInTheAir; }
			set
			{
				_maximumNumberOfPlanesInTheAir = value <= 0
					? DefaultMaximumNumberOfPlanesInTheAir
					: value;
			}
		}

		public int DropNotifyMaxDistance { get; set; }
		public int SupplySignalNotifyMaxDistance { get; set; }

		public CommonSettings()
		{
			NotifyOnPlaneSpawned = false;
			NotifyOnPlaneRemoved = false;
			NotifyOnDropStarted = false;
			NotifyOnPlayerLootingStarted = false;
			NotifyOnCollision = false;
			NotifyOnDespawn = false;
			NotifyOnSupplySingalDisabled = true;
			NotifyAboutDirectionAroundOnDropLand = false;
			NotifyAboutPlayersAroundOnDropLand = false;
			SupplySignalsEnabled = true;
			BuiltInAirdropEnabled = false;
			PluginAirdropTimerEnabled = true;
			MaximumPlaneLimitEnabled = false;

			NotifyAboutPlayersAroundOnSupplyThrown = false;
			NotifyAboutDirectionAroundOnSupplyThrown = false;

			PlaneSpeedInSeconds = 50;

			DropNotifyMaxDistance = DefaultDropNotifyMaxDistance;
			SupplySignalNotifyMaxDistance = DefaultSupplySignalNotifyMaxDistance;

			MinCrates = 1;
			MaxCrates = 1;
			DropToOneLocation = true;
		}

		public static CommonSettings CreateDefault()
		{
			return new CommonSettings
			{
				MinDropFrequency = DefaultDropFrequency,
				MaxDropFrequency = DefaultDropFrequency,
				MinimumPlayerCount = 25,
				SupplyCrateDespawnTime = TimeSpan.FromMinutes(5),

				MaximumNumberOfPlanesInTheAir = DefaultMaximumNumberOfPlanesInTheAir,
				MaximumPlaneLimitEnabled = false,

				NotifyOnPlaneSpawned = false,
				NotifyOnPlaneRemoved = false,
				NotifyOnDropStarted = false,
				NotifyOnPlayerLootingStarted = false,
				NotifyOnCollision = false,
				NotifyOnDespawn = false,
				NotifyOnSupplySingalDisabled = true,
				NotifyAboutDirectionAroundOnDropLand = false,
				NotifyAboutPlayersAroundOnDropLand = false,
				NotifyAboutPlayersAroundOnSupplyThrown = false,
				NotifyAboutDirectionAroundOnSupplyThrown = false,
				SupplySignalsEnabled = true,
				BuiltInAirdropEnabled = false,
				PluginAirdropTimerEnabled = true,
				PlaneSpeedInSeconds = 50,
				DropToOneLocation = true,
				MinCrates = 1,
				MaxCrates = 1,

				DropNotifyMaxDistance = DefaultDropNotifyMaxDistance,
				SupplySignalNotifyMaxDistance = DefaultSupplySignalNotifyMaxDistance,
			};
		}
	}

	public sealed class DropLocationSettings
	{
		public int MinX { get; set; }
		public int MaxX { get; set; }

		public int MinY { get; set; }
		public int MaxY { get; set; }

		public int MinZ { get; set; }
		public int MaxZ { get; set; }

		public static DropLocationSettings CreateDefault()
		{
			var halfOfWorldSize = Convert.ToInt32(World.Size / 2);
			return new DropLocationSettings
			{
				MinX = -halfOfWorldSize + 500,
				MaxX = halfOfWorldSize - 500,
				MinZ = -halfOfWorldSize + 500,
				MaxZ = halfOfWorldSize - 500,
				MinY = 200,
				MaxY = 300
			};
		}

		[JsonIgnore]
		public float PlaneWidth
		{
			get { return (Math.Abs(MinX) + Math.Abs(MaxX)) / 2.0f; }
		}

		[JsonIgnore]
		public float PlaneHeight
		{
			get { return (Math.Abs(MinZ) + Math.Abs(MaxZ)) / 2.0f; }
		}

		public Vector3 GetRandomPosition()
		{
			var x = Oxide.Core.Random.Range(MinX, MaxX + 1) + 1;
			var y = Oxide.Core.Random.Range(MinY, MaxY + 1);
			var z = Oxide.Core.Random.Range(MinZ, MaxZ + 1) + 1;

			return new Vector3(x, y, z);
		}
	}

	public sealed class LocalizationSettings
	{
		public const string DefaultNotifyOnNextDropPositionMessage = "Plane is dropping at: <color=red>{0:F0},{1:F0},{2:F0}</color>.";
		public const string DefaultNotifyOnPlaneSpawnedMessage = "Cargo Plane has been spawned.";
		public const string DefaultNotifyOnPlaneRemovedMessage = "Cargo Plane has been removed, due to insufficient player count: <color=yellow>{0}</color>.";
		public const string DefaultNotifyOnDropStartedMessage = "Supply Drop has been spawned at <color=red>{0:F0},{1:F0},{2:F0}</color>.";
		public const string DefaultNotifyOnPlayerLootingStartedMessage = "<color=green>{0}</color> started looting the Supply Drop.";
		public const string DefaultNotifyOnCollisionMessage = "Supply drop has landed at <color=red>{0:F0},{1:F0},{2:F0}</color>";
		public const string DefaultNotifyOnDespawnMessage = "Supply drop has been despawned at <color=red>{0:F0},{1:F0},{2:F0}</color>";
		public const string DefaultNotifyOnSupplySingalDisabledMessage = "Supply signals are disabled by server. An item has been added to your inventory/belt.";
		public const string DefaultNotifyAboutPlayersAroundOnDropLandMessage = "There are <color=green>{0}</color> players near drop, including you!";
		public const string DefaultNotifyAboutDirectionAroundOnDropLandMessage = "Airdrop is <color=green>{0:F0}</color> meters away from you! Direction: <color=yellow>{1}</color>";

		public const string DefaultNotifyAboutPlayersAroundOnSupplyThrownMessage = "There are <color=green>{0}</color> players around supply signal, including you.";
		public const string DefaultNotifyAboutDirectionAroundOnSupplyThrownMessage = "Someone launched supply signal <color=green>{0:F0}</color> meters from you. Direction:<color=yellow>{1}</color>";

		private const string DefaultPrefix = "[aire]: ";
		private const string DefaultColor = "orange";

		public string NotifyOnPlaneSpawnedMessage { get; set; }
		public string NotifyOnNextDropPositionMessage { get; set; }
		public string NotifyOnPlaneRemovedMessage { get; set; }
		public string NotifyOnDropStartedMessage { get; set; }
		public string NotifyOnPlayerLootingStartedMessage { get; set; }
		public string NotifyOnCollisionMessage { get; set; }
		public string NotifyOnSupplySingalDisabledMessage { get; set; }
		public string NotifyOnDespawnMessage { get; set; }
		public string NotifyAboutPlayersAroundOnDropLandMessage { get; set; }
		public string NotifyAboutDirectionAroundOnDropLandMessage { get; set; }
		public string NotifyAboutPlayersAroundOnSupplyThrownMessage { get; set; }
		public string NotifyAboutDirectionAroundOnSupplyThrownMessage { get; set; }

		public DirectionLocalizationSettings Directions { get; set; }

		public string Prefix { get; set; }
		public string Color { get; set; }

		public LocalizationSettings()
		{
			NotifyOnPlaneSpawnedMessage = DefaultNotifyOnPlaneSpawnedMessage;
			NotifyOnNextDropPositionMessage = DefaultNotifyOnNextDropPositionMessage;
			NotifyOnPlaneRemovedMessage = DefaultNotifyOnPlaneRemovedMessage;
			NotifyOnDropStartedMessage = DefaultNotifyOnDropStartedMessage;
			NotifyOnPlayerLootingStartedMessage = DefaultNotifyOnPlayerLootingStartedMessage;
			NotifyOnCollisionMessage = DefaultNotifyOnCollisionMessage;
			NotifyOnDespawnMessage = DefaultNotifyOnDespawnMessage;
			NotifyOnSupplySingalDisabledMessage = DefaultNotifyOnSupplySingalDisabledMessage;
			NotifyAboutDirectionAroundOnDropLandMessage = DefaultNotifyAboutDirectionAroundOnDropLandMessage;
			NotifyAboutPlayersAroundOnDropLandMessage = DefaultNotifyAboutPlayersAroundOnDropLandMessage;
			NotifyAboutPlayersAroundOnSupplyThrownMessage = DefaultNotifyAboutPlayersAroundOnSupplyThrownMessage;
			NotifyAboutDirectionAroundOnSupplyThrownMessage = DefaultNotifyAboutDirectionAroundOnSupplyThrownMessage;

			Directions = new DirectionLocalizationSettings();

			Prefix = DefaultPrefix;
			Color = DefaultColor;
		}

		public static LocalizationSettings CreateDefault()
		{
			return new LocalizationSettings();
		}
	}

	public sealed class DirectionLocalizationSettings
	{
		public string North { get; set; }
		public string NorthEast { get; set; }
		public string NorthWest { get; set; }
		public string East { get; set; }
		public string West { get; set; }
		public string South { get; set; }
		public string SouthWest { get; set; }
		public string SouthEast { get; set; }

		public DirectionLocalizationSettings()
		{
			North = "North";
			NorthEast = "NorthEast";
			NorthWest = "NorthWest";
			East = "East";
			West = "West";
			South = "South";
			SouthWest = "SouthWest";
			SouthEast = "SouthEast";
		}

		public static DropLocationSettings CreateDefault()
		{
			return new DropLocationSettings();
		}
	}

	public sealed class AirdropSettingsValidator
	{
		public static void Validate(AirdropSettings settings)
		{
			if (settings.ItemGroups == null)
				settings.ItemGroups = AirdropSettingsFactory.CreateDefault().ItemGroups;

			var countOfItems = settings.ItemGroups.Sum(g => g.MaximumAmountInLoot);
			var diff = countOfItems - AirdropSettings.MaxCapacity;
			if (diff > 0 && settings.PickStrategy == PickStrategy.GroupSize)
				AdjustGroupMaxAmount(settings.ItemGroups, diff);

			ValidateFrequency(settings);
			ValidateCrates(settings);

			ValidateMessages(settings);
		}

		private static void AdjustGroupMaxAmount(List<AirdropItemGroup> value, int diff)
		{
			Diagnostics.Diagnostics.MessageToServer("adjusting groups amount: substracting {0} from total", diff);
			var groupsOrderedByDescending = value.OrderByDescending(g => g.MaximumAmountInLoot);
			for (var i = diff; i > 0; i--)
			{
				var airdropItemGroup = groupsOrderedByDescending.Skip(diff - i).Take(1).First();
				value.First(g => g.Name == airdropItemGroup.Name).MaximumAmountInLoot--;

				foreach (var item in airdropItemGroup.ItemSettings.Where(item => item.MinAmount > item.MaxAmount))
					item.MinAmount = item.MaxAmount;
			}
		}

		private static void ValidateFrequency(AirdropSettings settings)
		{
			if (settings.CommonSettings.MinDropFrequency <= TimeSpan.Zero)
				settings.CommonSettings.MinDropFrequency = CommonSettings.DefaultDropFrequency;
			if (settings.CommonSettings.MaxDropFrequency <= TimeSpan.Zero)
				settings.CommonSettings.MaxDropFrequency = CommonSettings.DefaultDropFrequency;

			if (settings.CommonSettings.MinDropFrequency > settings.CommonSettings.MaxDropFrequency)
			{
				settings.CommonSettings.MinDropFrequency = settings.CommonSettings.MaxDropFrequency;
				Diagnostics.Diagnostics.MessageToServer("adjusting minfreq to :{0}", settings.CommonSettings.MinDropFrequency.TotalSeconds);
			}
			else if (settings.CommonSettings.MaxDropFrequency < settings.CommonSettings.MinDropFrequency)
			{
				settings.CommonSettings.MaxDropFrequency = settings.CommonSettings.MinDropFrequency;
				Diagnostics.Diagnostics.MessageToServer("adjusting maxfreq to :{0}", settings.CommonSettings.MaxDropFrequency.TotalSeconds);
			}
		}

		private static void ValidateCrates(AirdropSettings settings)
		{
			if (settings.CommonSettings.MinCrates > settings.CommonSettings.MaxCrates)
				settings.CommonSettings.MaxCrates = settings.CommonSettings.MinCrates;
		}

		private static void ValidateMessages(AirdropSettings settings)
		{
			ValidateLocalizationSetting(
				settings.Localization.NotifyAboutDirectionAroundOnDropLandMessage,
				LocalizationSettings.DefaultNotifyAboutDirectionAroundOnDropLandMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyAboutDirectionAroundOnSupplyThrownMessage,
				LocalizationSettings.DefaultNotifyAboutDirectionAroundOnSupplyThrownMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyAboutPlayersAroundOnDropLandMessage,
				LocalizationSettings.DefaultNotifyAboutPlayersAroundOnDropLandMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyAboutPlayersAroundOnSupplyThrownMessage,
				LocalizationSettings.DefaultNotifyAboutPlayersAroundOnSupplyThrownMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyOnCollisionMessage,
				LocalizationSettings.DefaultNotifyOnCollisionMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyOnDespawnMessage,
				LocalizationSettings.DefaultNotifyOnDespawnMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyOnDropStartedMessage,
				LocalizationSettings.DefaultNotifyOnDropStartedMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyOnNextDropPositionMessage,
				LocalizationSettings.DefaultNotifyOnNextDropPositionMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyOnPlaneRemovedMessage,
				LocalizationSettings.DefaultNotifyOnPlaneRemovedMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyOnPlaneSpawnedMessage,
				LocalizationSettings.DefaultNotifyOnPlaneSpawnedMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyOnPlayerLootingStartedMessage,
				LocalizationSettings.DefaultNotifyOnPlayerLootingStartedMessage);
			ValidateLocalizationSetting(
				settings.Localization.NotifyOnSupplySingalDisabledMessage,
				LocalizationSettings.DefaultNotifyOnSupplySingalDisabledMessage);
		}

		private static void ValidateLocalizationSetting(string settings, string defaultMessage)
		{
			const string pattern = @"{(.*?)}";
			var settingMatches = Regex.Matches(settings, pattern);
			var settingMatchCount = settingMatches.Count;

			var defaultMatches = Regex.Matches(defaultMessage, pattern);
			var defaultMatchCount = defaultMatches.Count;

			if (settingMatchCount > defaultMatchCount)
				Diagnostics.Diagnostics.MessageToServer(
				"\nYour localization string is incorrect:{0}.\nNumber of format parameters is insufficient.\nRemove {1} format parameters from your localization message.\nDefault message is:{2}",
				settings,
				settingMatchCount - defaultMatchCount,
				defaultMessage);
		}
	}

	public static class AidropSettingsRepository
	{
		public static AirdropSettings LoadFrom(string settingsName)
		{
			AirdropSettings settings;
			try
			{
				var fileName = "airdropExtended_" + settingsName;
				settings = Interface.GetMod().DataFileSystem.ReadObject<AirdropSettings>(fileName);

				var oxideGeneratedDefaultSettingsFile = string.IsNullOrEmpty(settingsName) || settings == null || settings.CommonSettings == null || settings.ItemGroups == null;
				if (oxideGeneratedDefaultSettingsFile)
				{
					Diagnostics.Diagnostics.MessageToServer("Not found settings in:{0}, generating default", settingsName);
					settings = AirdropSettingsFactory.CreateDefault();
					SaveTo(settingsName, settings);
				}
			}
			catch (Exception ex)
			{
				Diagnostics.Diagnostics.MessageToServer("exception during read:{0}", ex);
				Diagnostics.Diagnostics.MessageToServer("error. Creating default settings.");
				settings = AirdropSettingsFactory.CreateDefault();
			}

			return settings;
		}

		public static void SaveTo(string settingsName, AirdropSettings airdropSettings)
		{
			if (airdropSettings == null) throw new ArgumentNullException("airdropSettings");
			if (string.IsNullOrEmpty(settingsName)) throw new ArgumentException("Should not be blank", "settingsName");

			var fileName = "airdropExtended_" + settingsName;
			Diagnostics.Diagnostics.MessageToServer("Saving settings to:{0}", settingsName);
			Interface.GetMod().DataFileSystem.WriteObject(fileName, airdropSettings);
		}
	}
}

namespace AirdropExtended.Airdrop.Settings.Generate
{
	public static class AirdropSettingsFactory
	{
		public static List<string> DefaultExcludedItems = new List<string>
		{
			//Construction
			"generator.wind.scrap",
			"lock.key",
			
			//Food
			"wolfmeat.spoiled",
			"wolfmeat.burned",
			"chicken.spoiled",
			"chicken.burned",
			"apple.spoiled",
			"humanmeat.spoiled",
			"humanmeat.burned",
			"battery.small",
			
			//Misc
			"book.accident",
			"note",
			//Resources
			"salt.water",
			"skull.human",
			"skull.wolf",
			"water",
			//Tools
			"lock.key",
			"tool.camera",
			"rock",
			"torch"
		};

		private static readonly Dictionary<string, Func<ItemDefinition, int[]>> DefaultAmountByCategoryMapping = new Dictionary
			<string, Func<ItemDefinition, int[]>>
			{
				{"Food", GenerateAmountMappingForFood},
				{"Attire", def => new[] {1, 1}},
				{"Items", GenerateAmountMappingForItems},
				{"Ammunition", GenerateAmountMappingForAmmunition},
				{"Misc", GenerateAmountMappingForMisc},
				{"Construction", GenerateAmountMappingForConstruction},
				{"Medical", GenerateMappingForMedical},
				{"Tool", GenerateMappingForTool},
				{"Traps", GenerateMappingForTraps},
				{"Weapon", def => new[] {1, 1}},
				{"Resources", GenerateMappingForResource},
				{"Blueprint", def => new[] {1, 1}}
			};

		private static int[] GenerateMappingForTraps(ItemDefinition itemDefinition)
		{
			if (itemDefinition.shortname.Equals("autoturret", StringComparison.OrdinalIgnoreCase))
				return new[] { 0, 0 };

			return new[] { 1, 1 };
		}

		private static int[] GenerateAmountMappingForFood(ItemDefinition itemDefinition)
		{
			var excludedItems = new[] { "bearmeat", "humanmeat.cooked", "humanmeat.raw", "mushroom", "chicken.raw", "wolfmeat.raw" };
			var singleStackItems = new[] { "smallwaterbottle" };
			if (singleStackItems.Contains(itemDefinition.shortname))
				return new[] { 1, 1 };

			if (excludedItems.Contains(itemDefinition.shortname))
				return new[] { 0, 0 };

			return new[] { 5, 10 };
		}

		private static int[] GenerateAmountMappingForItems(ItemDefinition itemDefinition)
		{
			var excludedItems = new[] { "box.wooden", "furnace", "stash.small", "botabag", "campfire", "wolfmeat.raw" };
			if (excludedItems.Contains(itemDefinition.shortname))
				return new[] { 0, 0 };

			return new[] { 1, 1 };
		}

		private static int[] GenerateAmountMappingForAmmunition(ItemDefinition itemDefinition)
		{
			var excludedItems = new[] { "arrow.wooden", "ammo.rocket.smoke" };
			if (excludedItems.Contains(itemDefinition.shortname))
				return new[] { 0, 0 };

			return itemDefinition.shortname.Contains("rocket", CompareOptions.OrdinalIgnoreCase)
				? new[] { 1, 3 }
				: new[] { 32, 64 };
		}

		private static int[] GenerateAmountMappingForMisc(ItemDefinition itemDefinition)
		{
			var excludedItems = new[] { "door.key" };
			if (excludedItems.Contains(itemDefinition.shortname))
				return new[] { 0, 0 };

			return itemDefinition.shortname.Contains("blueprint", CompareOptions.OrdinalIgnoreCase)
				? new[] { 1, 3 }
				: new[] { 1, 1 };
		}

		private static int[] GenerateAmountMappingForConstruction(ItemDefinition itemDefinition)
		{
			var blueprint = ItemManager.FindBlueprint(itemDefinition);
			if (blueprint == null)
				return new[] { 0, 0 };

			if (itemDefinition.shortname.Equals("lock.key", StringComparison.OrdinalIgnoreCase))
				return new[] { 0, 0 };

			return new[] { 1, 1 };
		}

		private static int[] GenerateMappingForMedical(ItemDefinition itemDefinition)
		{
			var excludedItems = new[] { "blood" };
			if (excludedItems.Contains(itemDefinition.shortname))
				return new[] { 0, 0 };

			var largeStackItems = new[] { "antiradpills" };
			return largeStackItems.Contains(itemDefinition.shortname)
				? new[] { 5, 10 }
				: new[] { 1, 1 };
		}

		private static int[] GenerateMappingForTool(ItemDefinition itemDefinition)
		{
			var excludedItems = new[] { "flare", "hammer", "stonehatchet", "torch", "stone.pickaxe" };
			if (excludedItems.Contains(itemDefinition.shortname))
				return new[] { 0, 0 };

			var largeAmountItems = new[] { "explosive.timed" };
			if (largeAmountItems.Contains(itemDefinition.shortname))
				return new[] { 3, 5 };

			return new[] { 1, 1 };
		}

		private static int[] GenerateMappingForResource(ItemDefinition itemDefinition)
		{
			var largeStackItems = new[] { "wood", "sulfur_ore", "sulfur", "stones", "metal_ore", "metal_fragments", "fat_animal", "cloth", "gunpowder", "lowgradefuel", "leather" };
			var smallStackItems = new[] { "paper", "lowgradefuel", "explosives" };
			var zeroStackItems = new[] { "skull_wolf", "skull_human", "water", "salt_water", "charcoal", "targeting.computer", "can.beans.empty", "can.tuna.empty", "bone.fragments", "battery_small", "charcoal", "cctv.camera" };

			if (largeStackItems.Contains(itemDefinition.shortname))
				return new[] { 750, 1000 };
			if (smallStackItems.Contains(itemDefinition.shortname))
				return new[] { 25, 50 };
			if (zeroStackItems.Contains(itemDefinition.shortname))
				return new[] { 0, 0 };
			return new[] { 750, 1000 };
		}

		private static readonly Dictionary<string, int> DefaultCategoryAmountMapping = new Dictionary<string, int>
			{
				{"Food", 3},
				{"Attire", 2},
				{"Items", 1},
				{"Ammunition", 6},
				{"Construction", 2},
				{"Medical", 2},
				{"Tool", 1},
				{"Traps", 1},
				{"Misc", 1},
				{"Weapon", 6},
				{"Resources", 2},
				{"Blueprint", 1}
			};

		public const string TemplatePath = "";

		public static AirdropSettings CreateDefault()
		{
			var itemGroups = GenerateDefaultItemGroups();
			return new AirdropSettings
			{
				ItemGroups = itemGroups,
				Capacity = AirdropSettings.DefaultCapacity,
				CommonSettings = CommonSettings.CreateDefault()
			};
		}

		private static List<AirdropItemGroup> GenerateDefaultItemGroups()
		{
			var itemGroups = ItemManager
				.GetItemDefinitions()
				.GroupBy(i => i.category)
				.Select(group =>
				{
					var categoryName = group.Key.ToString();
					int defaultAmount;
					DefaultCategoryAmountMapping.TryGetValue(categoryName, out defaultAmount);
					return new AirdropItemGroup
					{
						Name = categoryName,
						ItemSettings = group
							.Where(i => !DefaultExcludedItems.Contains(i.shortname, StringComparer.OrdinalIgnoreCase))
							.Select(itemDefinition =>
							{
								Func<ItemDefinition, int[]> amountFunc;
								DefaultAmountByCategoryMapping.TryGetValue(categoryName, out amountFunc);
								var amountMappingArray = amountFunc == null
									? new[] { 0, 0 }
									: amountFunc(itemDefinition);
								var chanceInPercent = CalculateChanceByRarity(itemDefinition.rarity);

								return new AirdropItem
								{
									ChanceInPercent = chanceInPercent,
									Name = itemDefinition.shortname,
									MinAmount = amountMappingArray[0],
									MaxAmount = amountMappingArray[1]
								};
							})
							.ToList(),
						MaximumAmountInLoot = defaultAmount
					};
				})
				.ToList();

			var blueprintItemGroup = GenerateBlueprintItemGroup();
			itemGroups.Add(blueprintItemGroup);
			return itemGroups;
		}

		private static float CalculateChanceByRarity(Rarity rarity)
		{
			return 100 - ((int)rarity + 1) * 16f;
		}

		private static AirdropItemGroup GenerateBlueprintItemGroup()
		{
			var excludedBlueprints = DefaultExcludedItems.Concat(new[] { "ammo.rocket.smoke", "lantern_a", "lantern_b", "spear.stone" });
			var notDefaultBlueprints = ItemManager.bpList
				.Where(bp =>
					!bp.defaultBlueprint &&
					bp.userCraftable &&
					bp.isResearchable &&
					!excludedBlueprints.Contains(bp.targetItem.shortname))
				.ToList();
			var bpItems = notDefaultBlueprints.Select(b => b.targetItem).ToList();

			return new AirdropItemGroup
			{
				ItemSettings = bpItems.Select(itemDef => new AirdropItem
				{
					Name = itemDef.shortname,
					MinAmount = 1,
					MaxAmount = 1,
					ChanceInPercent = CalculateChanceByRarity(itemDef.rarity),
					IsBlueprint = true
				}).ToList(),
				MaximumAmountInLoot = 2,
				Name = "Blueprint"
			};
		}
	}
}
