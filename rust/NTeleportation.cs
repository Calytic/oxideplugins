using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Facepunch;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NTeleportation", "Nogrod", "1.0.11", ResourceId = 1832)]
    class NTeleportation : RustPlugin
    {
        private const string NewLine = "\n";
        private const string ConfigDefaultPermVip = "nteleportation.vip";
        private const string PermDeleteHome = "nteleportation.deletehome";
        private const string PermHomeHomes = "nteleportation.homehomes";
        private const string PermImportHomes = "nteleportation.importhomes";
        private const string PermRadiusHome = "nteleportation.radiushome";
        private const string PermTp = "nteleportation.tp";
        private const string PermTpB = "nteleportation.tpb";
        private const string PermTpConsole = "nteleportation.tpconsole";
        private const string PermTpHome = "nteleportation.tphome";
        private const string PermTpN = "nteleportation.tpn";
        private const string PermTpL = "nteleportation.tpl";
        private const string PermTpRemove = "nteleportation.tpremove";
        private const string PermTpSave = "nteleportation.tpsave";
        private const string PermWipeHomes = "nteleportation.wipehomes";
        private const string PermCraftHome = "nteleportation.crafthome";
        private const string PermCraftTown = "nteleportation.crafttown";
        private const string PermCraftTpR = "nteleportation.crafttpr";
        private DynamicConfigFile dataAdmin;
        private DynamicConfigFile dataHome;
        private DynamicConfigFile dataTPR;
        private DynamicConfigFile dataTown;
        private Dictionary<ulong, AdminData> Admin;
        private Dictionary<ulong, HomeData> Home;
        private Dictionary<ulong, TeleportData> TPR;
        private Dictionary<ulong, TeleportData> Town;
        private bool changedAdmin;
        private bool changedHome;
        private bool changedTPR;
        private bool changedTown;
        private ConfigData configData;
        private float boundary;
        private readonly int groundLayer = LayerMask.GetMask("Terrain", "World");
        private readonly int buildingLayer = LayerMask.GetMask("Terrain", "World", "Construction", "Deployed");
        private readonly int blockLayer = LayerMask.GetMask("Construction");
        private readonly Dictionary<ulong, TeleportTimer> TeleportTimers = new Dictionary<ulong, TeleportTimer>();
        private readonly Dictionary<ulong, Timer> PendingRequests = new Dictionary<ulong, Timer>();
        private readonly Dictionary<ulong, BasePlayer> PlayersRequests = new Dictionary<ulong, BasePlayer>();
        private readonly Dictionary<int, string> ReverseBlockedItems = new Dictionary<int, string>();
        private readonly HashSet<ulong> teleporting = new HashSet<ulong>();

        [PluginReference]
        private Plugin Friends;
        [PluginReference]
        private Plugin RustIO;
        [PluginReference]
        private Plugin Clans;

        class ConfigData
        {
            public SettingsData Settings { get; set; }
            public AdminSettingsData Admin { get; set; }
            public HomesSettingsData Home { get; set; }
            public TPRData TPR { get; set; }
            public TownData Town { get; set; }
            public VersionNumber Version { get; set; }
        }

        class SettingsData
        {
            public string ChatName { get; set; }
            public bool HomesEnabled { get; set; }
            public bool TPREnabled { get; set; }
            public bool TownEnabled { get; set; }
            public bool InterruptTPOnHurt { get; set; }
            public Dictionary<string, string> BlockedItems { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        class AdminSettingsData
        {
            public bool AnnounceTeleportToTarget { get; set; }
            public bool UseableByModerators { get; set; }
            public int LocationRadius { get; set; }
            public int TeleportNearDefaultDistance { get; set; }
        }

        class HomesSettingsData
        {
            public int HomesLimit { get; set; }
            public Dictionary<string, int> VIPHomesLimits { get; set; }
            public int Cooldown { get; set; }
            public int Countdown { get; set; }
            public int DailyLimit { get; set; }
            public Dictionary<string, int> VIPDailyLimits { get; set; }
            public Dictionary<string, int> VIPCooldowns { get; set; }
            public Dictionary<string, int> VIPCountdowns { get; set; }
            public int LocationRadius { get; set; }
            public bool ForceOnTopOfFoundation { get; set; }
            public bool CheckFoundationForOwner { get; set; }
            public bool UseFriends { get; set; }
            public bool UsableOutOfBuildingBlocked { get; set; }
            public bool AllowIceberg { get; set; }
            public bool AllowCave { get; set; }
            public bool AllowCraft { get; set; }
            public bool AllowAboveFoundation { get; set; }
        }

        class TPRData
        {
            public int Cooldown { get; set; }
            public int Countdown { get; set; }
            public int DailyLimit { get; set; }
            public Dictionary<string, int> VIPDailyLimits { get; set; }
            public Dictionary<string, int> VIPCooldowns { get; set; }
            public Dictionary<string, int> VIPCountdowns { get; set; }
            public int RequestDuration { get; set; }
            public bool BlockTPAOnCeiling { get; set; }
            public bool UsableOutOfBuildingBlocked { get; set; }
            public bool AllowCraft { get; set; }
        }

        class TownData
        {
            public int Cooldown { get; set; }
            public int Countdown { get; set; }
            public int DailyLimit { get; set; }
            public Dictionary<string, int> VIPDailyLimits { get; set; }
            public Dictionary<string, int> VIPCooldowns { get; set; }
            public Dictionary<string, int> VIPCountdowns { get; set; }
            public Vector3 Location { get; set; }
            public bool UsableOutOfBuildingBlocked { get; set; }
            public bool AllowCraft { get; set; }
        }

        class AdminData
        {
            [JsonProperty("pl")]
            public Vector3 PreviousLocation { get; set; }
            [JsonProperty("l")]
            public Dictionary<string, Vector3> Locations { get; set; } = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
        }

        class HomeData
        {
            [JsonProperty("l")]
            public Dictionary<string, Vector3> Locations { get; set; } = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
            [JsonProperty("t")]
            public TeleportData Teleports { get; set; } = new TeleportData();
        }

        class TeleportData
        {
            [JsonProperty("a")]
            public int Amount { get; set; }
            [JsonProperty("d")]
            public string Date { get; set; }
            [JsonProperty("t")]
            public int Timestamp { get; set; }
        }

        class TeleportTimer
        {
            public Timer Timer { get; set; }
            public BasePlayer OriginPlayer { get; set; }
            public BasePlayer TargetPlayer { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            Config.Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Config.Settings.Converters = new JsonConverter[] {new UnityVector3Converter()};
            Config.WriteObject(new ConfigData
            {
                Settings = new SettingsData
                {
                    ChatName = "<color=red>Teleportation</color>: ",
                    HomesEnabled = true,
                    TPREnabled = true,
                    TownEnabled = true,
                    InterruptTPOnHurt = true
                },
                Admin = new AdminSettingsData
                {
                    AnnounceTeleportToTarget = false,
                    UseableByModerators = true,
                    LocationRadius = 25,
                    TeleportNearDefaultDistance = 30
                },
                Home = new HomesSettingsData
                {
                    HomesLimit = 2,
                    VIPHomesLimits = new Dictionary<string, int> {{ConfigDefaultPermVip, 5}},
                    Cooldown = 600,
                    Countdown = 15,
                    DailyLimit = 5,
                    VIPDailyLimits = new Dictionary<string, int> {{ConfigDefaultPermVip, 5}},
                    VIPCooldowns = new Dictionary<string, int> {{ConfigDefaultPermVip, 5}},
                    VIPCountdowns = new Dictionary<string, int> {{ConfigDefaultPermVip, 5}},
                    LocationRadius = 25,
                    ForceOnTopOfFoundation = true,
                    CheckFoundationForOwner = true,
                    UseFriends = true,
                    AllowAboveFoundation = true
                },
                TPR = new TPRData
                {
                    Cooldown = 600,
                    Countdown = 15,
                    DailyLimit = 5,
                    VIPDailyLimits = new Dictionary<string, int> {{ConfigDefaultPermVip, 5}},
                    VIPCooldowns = new Dictionary<string, int> {{ConfigDefaultPermVip, 5}},
                    VIPCountdowns = new Dictionary<string, int> {{ConfigDefaultPermVip, 5}},
                    RequestDuration = 30,
                    BlockTPAOnCeiling = true
                },
                Town = new TownData
                {
                    Cooldown = 600,
                    Countdown = 15,
                    DailyLimit = 5,
                    VIPDailyLimits = new Dictionary<string, int> {{ConfigDefaultPermVip, 5}},
                    VIPCooldowns = new Dictionary<string, int> {{ConfigDefaultPermVip, 5}},
                    VIPCountdowns = new Dictionary<string, int> {{ConfigDefaultPermVip, 5}}
                },
                Version = Version
            }, true);
        }

        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"AdminTP", "You teleported to {0}!"},
                {"AdminTPTarget", "{0} teleported to you!"},
                {"AdminTPPlayers", "You teleported {0} to {1}!"},
                {"AdminTPPlayer", "{0} teleported you to {1}!"},
                {"AdminTPPlayerTarget", "{0} teleported {1} to you!"},
                {"AdminTPCoordinates", "You teleported to {0}!"},
                {"AdminTPTargetCoordinates", "You teleported {0} to {1}!"},
                {"AdminTPOutOfBounds", "You tried to teleport to a set of coordinates outside the map boundaries!"},
                {"AdminTPBoundaries", "X and Z values need to be between -{0} and {0} while the Y value needs to be between -100 and 2000!"},
                {"AdminTPLocation", "You teleported to {0}!"},
                {"AdminTPLocationSave", "You have saved the current location!"},
                {"AdminTPLocationRemove", "You have removed the location {0}!"},
                {"AdminLocationList", "The following locations are available:"},
                {"AdminLocationListEmpty", "You haven't saved any locations!"},
                {"AdminTPBack", "You've teleported back to your previous location!"},
                {"AdminTPBackSave", "Your previous location has been saved, use /tpb to teleport back!"},
                {"AdminTPTargetCoordinatesTarget", "{0} teleported you to {1}!"},
                {"AdminTPConsoleTP", "You were teleported to {0}"},
                {"AdminTPConsoleTPPlayer", "You were teleported to {0}"},
                {"AdminTPConsoleTPPlayerTarget", "{0} was teleported to you!"},
                {"HomeTP", "You teleported to your home '{0}'!"},
                {"HomeAdminTP", "You teleported to {0}'s home '{1}'!"},
                {"HomeSave", "You have saved the current location as your home!"},
                {"HomeNoFoundation", "You can only use a home location on a foundation!"},
                {"HomeFoundationNotOwned", "You can't use home on someone else's house."},
                {"HomeFoundationNotFriendsOwned", "You or a friend need to own the house to use home!"},
                {"HomeRemovedInvalid", "Your home '{0}' was removed because not on a foundation or not owned!"},
                {"HomeRemove", "You have removed your home {0}!"},
                {"HomeDelete", "You have removed {0}'s home '{1}'!"},
                {"HomeList", "The following homes are available:"},
                {"HomeListEmpty", "You haven't saved any homes!"},
                {"HomeMaxLocations", "Unable to set your home here, you have reached the maximum of {0} homes!"},
                {"HomeQuota", "You have set {0} of the maximum {1} homes!"},
                {"HomeTPStarted", "Teleporting to your home {0} in {1} seconds!"},
                {"HomeTPCooldown", "Your teleport is currently on cooldown. You'll have to wait {0} for your next teleport."},
                {"HomeTPLimitReached", "You have reached the daily limit of {0} teleports today!"},
                {"HomeTPAmount", "You have {0} home teleports left today!"},
                {"HomesListWiped", "You have wiped all the saved home locations!"},
                {"HomeTPBuildingBlocked", "You can't set your home if you are not allowed to build in this zone!"},
                {"HomeTPSwimming", "You can't set your home while swimming!"},
                {"HomeTPCrafting", "You can't set your home while crafting!"},
                {"Request", "You've requested a teleport to {0}!"},
                {"RequestTarget", "{0} requested to be teleported to you! Use '/tpa' to accept!"},
                {"PendingRequest", "You already have a request pending, cancel that request or wait until it gets accepted or times out!"},
                {"PendingRequestTarget", "The player you wish to teleport to already has a pending request, try again later!"},
                {"NoPendingRequest", "You have no pending teleport request!"},
                {"AcceptOnRoof", "You can't accept a teleport while you're on a ceiling, get to ground level!"},
                {"Accept", "{0} has accepted your teleport request! Teleporting in {1} seconds!"},
                {"AcceptTarget", "You've accepted the teleport request of {0}!"},
                {"NotAllowed", "You are not allowed to use this command!"},
                {"Success", "You teleported to {0}!"},
                {"SuccessTarget", "{0} teleported to you!"},
                {"Cancelled", "Your teleport request to {0} was cancelled!"},
                {"CancelledTarget", "{0} teleport request was cancelled!"},
                {"TPCancelled", "Your teleport was cancelled!"},
                {"TPCancelledTarget", "{0} cancelled teleport!"},
                {"TPYouCancelledTarget", "You cancelled {0} teleport!"},
                {"TimedOut", "{0} did not answer your request in time!"},
                {"TimedOutTarget", "You did not answer {0}'s teleport request in time!"},
                {"TargetDisconnected", "{0} has disconnected, your teleport was cancelled!"},
                {"TPRCooldown", "Your teleport requests are currently on cooldown. You'll have to wait {0} to send your next teleport request."},
                {"TPRLimitReached", "You have reached the daily limit of {0} teleport requests today!"},
                {"TPRAmount", "You have {0} teleport requests left today!"},
                {"TPRTarget", "Your target is currently not available!"},
                {"TPDead", "You can't teleport while being dead!"},
                {"TPBuildingBlocked", "You can't teleport while in a building blocked zone!"},
                {"TPSwimming", "You can't teleport while swimming!"},
                {"TPCrafting", "You can't teleport while crafting!"},
                {"TPBlockedItem", "You can't teleport while carrying: {0}!"},
                {"TownTP", "You teleported to town!"},
                {"TownTPNotSet", "Town is currently not set!"},
                {"TownTPLocation", "You have set the town location set to {0}!"},
                {"TownTPStarted", "Teleporting to town in {0} seconds!"},
                {"TownTPCooldown", "Your teleport is currently on cooldown. You'll have to wait {0} for your next teleport."},
                {"TownTPLimitReached", "You have reached the daily limit of {0} teleports today!"},
                {"TownTPAmount", "You have {0} town teleports left today!"},
                {"Interrupted", "Your teleport was interrupted!"},
                {"InterruptedTarget", "{0}'s teleport was interrupted!"},
                {"Unlimited", "Unlimited"},
                {
                    "TPInfoGeneral", string.Join(NewLine, new[]
                    {
                        "Please specify the module you want to view the info of.",
                        "The available modules are: ",
                    })
                },
                {
                    "TPHelpGeneral", string.Join(NewLine, new[]
                    {
                        "/tpinfo - Shows limits and cooldowns.",
                        "Please specify the module you want to view the help of.",
                        "The available modules are: ",
                    })
                },
                {
                    "TPHelpadmintp", string.Join(NewLine, new[]
                    {
                        "As an admin you have access to the following commands:",
                        "/tp \"targetplayer\" - Teleports yourself to the target player.",
                        "/tp \"player\" \"targetplayer\" - Teleports the player to the target player.",
                        "/tp x y z - Teleports you to the set of coordinates.",
                        "/tpl - Shows a list of saved locations.",
                        "/tpl \"location name\" - Teleports you to a saved location.",
                        "/tpsave \"location name\" - Saves your current position as the location name.",
                        "/tpremove \"location name\" - Removes the location from your saved list.",
                        "/tpb - Teleports you back to the place where you were before teleporting.",
                        "/home radius \"radius\" - Find all homes in radius.",
                        "/home delete \"player name|id\" \"home name\" - Remove a home from a player.",
                        "/home tp \"player name|id\" \"name\" - Teleports you to the home location with the name 'name' from the player.",
                        "/home homes \"player name|id\" - Shows you a list of all homes from the player."
                    })
                },
                {
                    "TPHelphome", string.Join(NewLine, new[]
                    {
                        "With the following commands you can set your home location to teleport back to:",
                        "/home add \"name\" - Saves your current position as the location name.",
                        "/home list - Shows you a list of all the locations you have saved.",
                        "/home remove \"name\" - Removes the location of your saved homes.",
                        "/home \"name\" - Teleports you to the home location."
                    })
                },
                {
                    "TPHelptpr", string.Join(NewLine, new[]
                    {
                        "With these commands you can request to be teleported to a player or accept someone else's request:",
                        "/tpr \"player name\" - Sends a teleport request to the player.",
                        "/tpa - Accepts an incoming teleport request.",
                        "/tpc - Cancel teleport or request."
                    })
                },
                {
                    "TPSettingsGeneral", string.Join(NewLine, new[]
                    {
                        "Please specify the module you want to view the settings of. ",
                        "The available modules are:",
                    })
                },
                {
                    "TPSettingshome", string.Join(NewLine, new[]
                    {
                        "Home System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}",
                        "Amount of saved Home locations: {2}"
                    })
                },
                {
                    "TPSettingstpr", string.Join(NewLine, new[]
                    {
                        "TPR System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}"
                    })
                },
                {
                    "TPSettingstown", string.Join(NewLine, new[]
                    {
                        "Town System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}"
                    })
                },
                {"PlayerNotFound", "The specified player couldn't be found please try again!"},
                {"MultiplePlayers", "Found multiple players: {0}"},
                {"CantTeleportToSelf", "You can't teleport to yourself!"},
                {"CantTeleportPlayerToSelf", "You can't teleport a player to himself!"},
                {"TeleportPending", "You can't initiate another teleport while you have a teleport pending!"},
                {"TeleportPendingTarget", "You can't request a teleport to someone who's about to teleport!"},
                {"LocationExists", "A location with this name already exists at {0}!"},
                {"LocationExistsNearby", "A location with the name {0} already exists near this position!"},
                {"LocationNotFound", "Couldn't find a location with that name!"},
                {"NoPreviousLocationSaved", "No previous location saved!"},
                {"HomeExists", "You have already saved a home location by this name!"},
                {"HomeExistsNearby", "A home location with the name {0} already exists near this position!"},
                {"HomeNotFound", "Couldn't find your home with that name!"},
                {"InvalidCoordinates", "The coordinates you've entered are invalid!"},
                {"InvalidHelpModule", "Invalid module supplied!"},
                {"InvalidCharacter", "You have used an invalid character, please limit yourself to the letters a to z and numbers."},
                {
                    "SyntaxCommandTP", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tp command as follows:",
                        "/tp \"targetplayer\" - Teleports yourself to the target player.",
                        "/tp \"player\" \"targetplayer\" - Teleports the player to the target player.",
                        "/tp x y z - Teleports you to the set of coordinates.",
                        "/tp \"player\" x y z - Teleports the player to the set of coordinates."
                    })
                },
                {
                    "SyntaxCommandTPL", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpl command as follows:",
                        "/tpl - Shows a list of saved locations.",
                        "/tpl \"location name\" - Teleports you to a saved location."
                    })
                },
                {
                    "SyntaxCommandTPSave", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpsave command as follows:",
                        "/tpsave \"location name\" - Saves your current position as 'location name'."
                    })
                },
                {
                    "SyntaxCommandTPRemove", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpremove command as follows:",
                        "/tpremove \"location name\" - Removes the location with the name 'location name'."
                    })
                },
                {
                    "SyntaxCommandTPN", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpn command as follows:",
                        "/tpn \"targetplayer\" - Teleports yourself the default distance behind the target player.",
                        "/tpn \"targetplayer\" \"distance\" - Teleports you the specified distance behind the target player."
                    })
                },
                {
                    "SyntaxCommandSetHome", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home add command as follows:",
                        "/home add \"name\" - Saves the current location as your home with the name 'name'."
                    })
                },
                {
                    "SyntaxCommandRemoveHome", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home remove command as follows:",
                        "/home remove \"name\" - Removes the home location with the name 'name'."
                    })
                },
                {
                    "SyntaxCommandHome", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home command as follows:",
                        "/home \"name\" - Teleports yourself to your home with the name 'name'.",
                        "/home add \"name\" - Saves the current location as your home with the name 'name'.",
                        "/home list - Shows you a list of all your saved home locations.",
                        "/home remove \"name\" - Removes the home location with the name 'name'."
                    })
                },
                {
                    "SyntaxCommandHomeAdmin", string.Join(NewLine, new[]
                    {
                        "/home radius \"radius\" - Shows you a list of all homes in radius(10).",
                        "/home delete \"player name|id\" \"name\" - Removes the home location with the name 'name' from the player.",
                        "/home tp \"player name|id\" \"name\" - Teleports you to the home location with the name 'name' from the player.",
                        "/home homes \"player name|id\" - Shows you a list of all homes from the player."
                    })
                },
                {
                    "SyntaxCommandTown", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /town command as follows:",
                        "/town - Teleports yourself to town."
                    })
                },
                {
                    "SyntaxCommandTownAdmin", string.Join(NewLine, new[]
                    {
                        "/town set - Saves the current location as town.",
                    })
                },
                {
                    "SyntaxCommandHomeDelete", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home delete command as follows:",
                        "/home delete \"player name|id\" \"name\" - Removes the home location with the name 'name' from the player."
                    })
                },
                {
                    "SyntaxCommandHomeAdminTP", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home tp command as follows:",
                        "/home tp \"player name|id\" \"name\" - Teleports you to the home location with the name 'name' from the player."
                    })
                },
                {
                    "SyntaxCommandHomeHomes", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home homes command as follows:",
                        "/home homes \"player name|id\" - Shows you a list of all homes from the player."
                    })
                },
                {
                    "SyntaxCommandListHomes", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home list command as follows:",
                        "/home list - Shows you a list of all your saved home locations."
                    })
                },
                {
                    "SyntaxCommandTPR", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpr command as follows:",
                        "/tpr \"player name\" - Sends out a teleport request to 'player name'."
                    })
                },
                {
                    "SyntaxCommandTPA", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpa command as follows:",
                        "/tpa - Accepts an incoming teleport request."
                    })
                },
                {
                    "SyntaxCommandTPC", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpc command as follows:",
                        "/tpc - Cancels an teleport request."
                    })
                },
                {
                    "SyntaxConsoleCommandToPos", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the teleport.topos console command as follows:",
                        " > teleport.topos \"player\" x y z"
                    })
                },
                {
                    "SyntaxConsoleCommandToPlayer", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the teleport.toplayer console command as follows:",
                        " > teleport.toplayer \"player\" \"target player\""
                    })
                },
                {"LogTeleport", "{0} teleported to {1}."},
                {"LogTeleportPlayer", "{0} teleported {1} to {2}."},
                {"LogTeleportBack", "{0} teleported back to previous location."}
            }, this);
            Config.Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Config.Settings.Converters = new JsonConverter[] { new UnityVector3Converter() };
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch (Exception)
            {
                Puts("Corrupt config, loading default...");
                LoadDefaultConfig();
            }
            if (!(configData.Version == Version))
            {
                if (configData.Home.VIPHomesLimits == null)
                {
                    configData.Home.VIPHomesLimits = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };
                    configData.Home.VIPDailyLimits = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };
                    configData.Home.VIPCooldowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };
                    configData.TPR.VIPDailyLimits = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };
                    configData.TPR.VIPCooldowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };
                    configData.Town.VIPDailyLimits = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };
                    configData.Town.VIPCooldowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };
                }
                if (configData.Home.VIPCountdowns == null)
                {
                    configData.Home.VIPCountdowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };
                    configData.TPR.VIPCountdowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };
                    configData.Town.VIPCountdowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };
                }
                if (configData.Version <= new VersionNumber(1, 0, 4))
                    configData.Home.AllowAboveFoundation = true;
                configData.Version = Version;
                Config.WriteObject(configData, true);
            }
            dataAdmin = GetFile(nameof(NTeleportation) + "Admin");
            Admin = dataAdmin.ReadObject<Dictionary<ulong, AdminData>>();
            dataHome = GetFile(nameof(NTeleportation) + "Home");
            Home = dataHome.ReadObject<Dictionary<ulong, HomeData>>();
            dataTPR = GetFile(nameof(NTeleportation) + "TPR");
            TPR = dataTPR.ReadObject<Dictionary<ulong, TeleportData>>();
            dataTown = GetFile(nameof(NTeleportation) + "Town");
            Town = dataTown.ReadObject<Dictionary<ulong, TeleportData>>();
            cmd.AddConsoleCommand("teleport.toplayer", this, ccmdTeleport);
            cmd.AddConsoleCommand("teleport.topos", this, ccmdTeleport);
            permission.RegisterPermission(PermDeleteHome, this);
            permission.RegisterPermission(PermHomeHomes, this);
            permission.RegisterPermission(PermImportHomes, this);
            permission.RegisterPermission(PermRadiusHome, this);
            permission.RegisterPermission(PermTp, this);
            permission.RegisterPermission(PermTpB, this);
            permission.RegisterPermission(PermTpConsole, this);
            permission.RegisterPermission(PermTpHome, this);
            permission.RegisterPermission(PermTpN, this);
            permission.RegisterPermission(PermTpL, this);
            permission.RegisterPermission(PermTpRemove, this);
            permission.RegisterPermission(PermTpSave, this);
            permission.RegisterPermission(PermWipeHomes, this);
            permission.RegisterPermission(PermCraftHome, this);
            permission.RegisterPermission(PermCraftTown, this);
            permission.RegisterPermission(PermCraftTpR, this);
        }
        private DynamicConfigFile GetFile(string name)
        {
            var file = Interface.Oxide.DataFileSystem.GetFile(name);
            file.Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            file.Settings.Converters = new JsonConverter[] { new UnityVector3Converter(), new CustomComparerDictionaryCreationConverter<string>(StringComparer.OrdinalIgnoreCase) };
            return file;
        }

        void OnServerInitialized()
        {
            boundary = TerrainMeta.Size.x / 2;
            CheckPerms(configData.Home.VIPHomesLimits);
            CheckPerms(configData.Home.VIPDailyLimits);
            CheckPerms(configData.Home.VIPCooldowns);
            CheckPerms(configData.TPR.VIPDailyLimits);
            CheckPerms(configData.TPR.VIPCooldowns);
            CheckPerms(configData.Town.VIPDailyLimits);
            CheckPerms(configData.Town.VIPCooldowns);
            foreach (var item in configData.Settings.BlockedItems)
            {
                var definition = ItemManager.FindItemDefinition(item.Key);
                if (definition == null)
                {
                    Puts("Blocked item not found: {0}", item.Key);
                    continue;
                }
                ReverseBlockedItems[definition.itemid] = item.Value;
            }
        }

        void OnServerSave()
        {
            SaveTeleportsAdmin();
            SaveTeleportsHome();
            SaveTeleportsTPR();
            SaveTeleportsTown();
        }

        void OnServerShutdown() => OnServerSave();

        void Unload() => OnServerSave();

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            var player = entity.ToPlayer();
            if (player == null || hitinfo == null) return;
            if (hitinfo.damageTypes.Has(DamageType.Fall) && teleporting.Contains(player.userID))
            {
                hitinfo.damageTypes = new DamageTypeList();
                teleporting.Remove(player.userID);
            }
            TeleportTimer teleportTimer;
            if (!TeleportTimers.TryGetValue(player.userID, out teleportTimer)) return;
            NextTick(() => {
                if (hitinfo.damageTypes.Total() <= 0) return;
                PrintMsgL(teleportTimer.OriginPlayer, "Interrupted");
                if (teleportTimer.TargetPlayer != null)
                    PrintMsgL(teleportTimer.TargetPlayer, "InterruptedTarget", teleportTimer.OriginPlayer.displayName);
                teleportTimer.Timer.Destroy();
                TeleportTimers.Remove(player.userID);
            });
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (teleporting.Contains(player.userID))
                timer.Once(3, () => { teleporting.Remove(player.userID); });
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            Timer reqTimer;
            if (PendingRequests.TryGetValue(player.userID, out reqTimer))
            {
                var originPlayer = PlayersRequests[player.userID];
                PrintMsgL(originPlayer, "RequestTargetOff");
                reqTimer.Destroy();
                PendingRequests.Remove(player.userID);
                PlayersRequests.Remove(player.userID);
                PlayersRequests.Remove(originPlayer.userID);
            }
            TeleportTimer teleportTimer;
            if (TeleportTimers.TryGetValue(player.userID, out teleportTimer))
            {
                teleportTimer.Timer.Destroy();
                TeleportTimers.Remove(player.userID);
            }
            teleporting.Remove(player.userID);
        }

        private void SaveTeleportsAdmin()
        {
            if (Admin == null || !changedAdmin) return;
            dataAdmin.WriteObject(Admin);
            changedAdmin = false;
        }

        private void SaveTeleportsHome()
        {
            if (Home == null || !changedHome) return;
            dataHome.WriteObject(Home);
            changedHome = false;
        }

        private void SaveTeleportsTPR()
        {
            if (TPR == null || !changedTPR) return;
            dataTPR.WriteObject(TPR);
            changedTPR = false;
        }

        private void SaveTeleportsTown()
        {
            if (Town == null || !changedTown) return;
            dataTown.WriteObject(Town);
            changedTown = false;
        }

        private void SaveLocation(BasePlayer player)
        {
            if (!IsAllowed(player, PermTpB)) return;
            AdminData adminData;
            if (!Admin.TryGetValue(player.userID, out adminData))
                Admin[player.userID] = adminData = new AdminData();
            adminData.PreviousLocation = player.transform.position;
            changedAdmin = true;
            PrintMsgL(player, "AdminTPBackSave");
        }

        [ChatCommand("tp")]
        private void cmdChatTeleport(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermTp)) return;
            BasePlayer target;
            float x, y, z;
            switch (args.Length)
            {
                case 1:
                    target = FindPlayersSingle(args[0], player);
                    if (target == null) return;
                    if (target == player)
                    {
                        PrintMsgL(player, "CantTeleportToSelf");
                        return;
                    }
                    TeleportToPlayer(player, target);
                    PrintMsgL(player, "AdminTP", target.displayName);
                    Puts(_("LogTeleport", null, player.displayName, target.displayName));
                    if (configData.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(target, "AdminTPTarget", player.displayName);
                    break;
                case 2:
                    var origin = FindPlayersSingle(args[0], player);
                    if (origin == null) return;
                    target = FindPlayersSingle(args[1], player);
                    if (target == null) return;
                    if (target == origin)
                    {
                        PrintMsgL(player, "CantTeleportPlayerToSelf");
                        return;
                    }
                    TeleportToPlayer(origin, target);
                    PrintMsgL(player, "AdminTPPlayers", origin.displayName, target.displayName);
                    PrintMsgL(origin, "AdminTPPlayer", player.displayName, target.displayName);
                    PrintMsgL(target, "AdminTPPlayerTarget", player.displayName, origin.displayName);
                    Puts(_("LogTeleportPlayer", null, player.displayName, origin.displayName, target.displayName));
                    break;
                case 3:
                    if (!float.TryParse(args[0], out x) || !float.TryParse(args[1], out y) || !float.TryParse(args[2], out z))
                    {
                        PrintMsgL(player, "InvalidCoordinates");
                        return;
                    }
                    if (!CheckBoundaries(x, y, z))
                    {
                        PrintMsgL(player, "AdminTPOutOfBounds");
                        PrintMsgL(player, "AdminTPBoundaries", boundary);
                        return;
                    }
                    TeleportToPosition(player, x, y, z);
                    PrintMsgL(player, "AdminTPCoordinates", player.transform.position);
                    Puts(_("LogTeleport", null, player.displayName, player.transform.position));
                    break;
                case 4:
                    target = FindPlayersSingle(args[0], player);
                    if (target == null) return;
                    if (!float.TryParse(args[0], out x) || !float.TryParse(args[1], out y) || !float.TryParse(args[2], out z))
                    {
                        PrintMsgL(player, "InvalidCoordinates");
                        return;
                    }
                    if (!CheckBoundaries(x, y, z))
                    {
                        PrintMsgL(player, "AdminTPOutOfBounds");
                        PrintMsgL(player, "AdminTPBoundaries", boundary);
                        return;
                    }
                    TeleportToPosition(target, x, y, z);
                    if (player == target)
                    {
                        PrintMsgL(player, "AdminTPCoordinates", player.transform.position);
                        Puts(_("LogTeleport", null, player.displayName, player.transform.position));
                    }
                    else
                    {
                        PrintMsgL(player, "AdminTPTargetCoordinates", target.displayName, player.transform.position);
                        PrintMsgL(target, "AdminTPTargetCoordinatesTarget", player.displayName, player.transform.position);
                        Puts(_("LogTeleportPlayer", null, player.displayName, target.displayName, player.transform.position));
                    }
                    break;
                default:
                    PrintMsgL(player, "SyntaxCommandTP");
                    break;
            }
        }

        [ChatCommand("tpn")]
        private void cmdChatTeleportNear(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermTpN)) return;
            switch (args.Length)
            {
                case 1:
                case 2:
                    var target = FindPlayersSingle(args[0], player);
                    if (target == null) return;
                    if (target == player)
                    {
                        PrintMsgL(player, "CantTeleportToSelf");
                        return;
                    }
                    int distance;
                    if (args.Length != 2 || !int.TryParse(args[1], out distance))
                        distance = configData.Admin.TeleportNearDefaultDistance;
                    float x = UnityEngine.Random.Range(-distance, distance);
                    var z = (float) System.Math.Sqrt(System.Math.Pow(distance, 2) - System.Math.Pow(x, 2));
                    var destination = target.transform.position;
                    destination.x = destination.x - x;
                    destination.z = destination.z - z;
                    Teleport(player, GetGroundBuilding(destination));
                    PrintMsgL(player, "AdminTP", target.displayName);
                    Puts(_("LogTeleport", null, player.displayName, target.displayName));
                    if (configData.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(target, "AdminTPTarget", player.displayName);
                    break;
                default:
                    PrintMsgL(player, "SyntaxCommandTPN");
                    break;
            }
        }

        [ChatCommand("tpl")]
        private void cmdChatTeleportLocation(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermTpL)) return;
            AdminData adminData;
            if (!Admin.TryGetValue(player.userID, out adminData) || adminData.Locations.Count <= 0)
            {
                PrintMsgL(player, "AdminLocationListEmpty");
                return;
            }
            switch (args.Length)
            {
                case 0:
                    PrintMsgL(player, "AdminLocationList");
                    foreach (var location in adminData.Locations)
                        PrintMsgL(player, $"{location.Key} {location.Value}");
                    break;
                case 1:
                    Vector3 loc;
                    if (!adminData.Locations.TryGetValue(args[0], out loc))
                    {
                        PrintMsgL(player, "LocationNotFound");
                        return;
                    }
                    Teleport(player, loc);
                    PrintMsgL(player, "AdminTPLocation", args[0]);
                    break;
                default:
                    PrintMsgL(player, "SyntaxCommandTPL");
                    break;
            }
        }

        [ChatCommand("tpsave")]
        private void cmdChatSaveTeleportLocation(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermTpSave)) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandTPSave");
                return;
            }
            AdminData adminData;
            if (!Admin.TryGetValue(player.userID, out adminData))
                Admin[player.userID] = adminData = new AdminData();
            Vector3 location;
            if (adminData.Locations.TryGetValue(args[0], out location))
            {
                PrintMsgL(player, "LocationExists", location);
                return;
            }
            var positionCoordinates = player.transform.position;
            foreach (var loc in adminData.Locations)
            {
                if (Vector3.Distance(positionCoordinates, loc.Value) < configData.Admin.LocationRadius)
                {
                    PrintMsgL(player, "LocationExistsNearby", loc.Key);
                    return;
                }
            }
            adminData.Locations[args[0]] = positionCoordinates;
            PrintMsgL(player, "AdminTPLocationSave");
            changedAdmin = true;
        }

        [ChatCommand("tpremove")]
        private void cmdChatRemoveTeleportLocation(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermTpRemove)) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandTPRemove");
                return;
            }
            AdminData adminData;
            if (!Admin.TryGetValue(player.userID, out adminData) || adminData.Locations.Count <= 0)
            {
                PrintMsgL(player, "AdminLocationListEmpty");
                return;
            }
            if (adminData.Locations.Remove(args[0]))
            {
                PrintMsgL(player, "AdminTPLocationRemove", args[0]);
                changedAdmin = true;
                return;
            }
            PrintMsgL(player, "LocationNotFound");
        }

        [ChatCommand("tpb")]
        private void cmdChatTeleportBack(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermTpB)) return;
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandTPB");
                return;
            }
            AdminData adminData;
            if (!Admin.TryGetValue(player.userID, out adminData) || adminData.PreviousLocation == default(Vector3))
            {
                PrintMsgL(player, "NoPreviousLocationSaved");
                return;
            }
            Teleport(player, adminData.PreviousLocation);
            adminData.PreviousLocation = default(Vector3);
            changedAdmin = true;
            PrintMsgL(player, "AdminTPBack");
            Puts(_("LogTeleportBack", null, player.displayName));
        }

        [ChatCommand("sethome")]
        private void cmdChatSetHome(BasePlayer player, string command, string[] args)
        {
            if (!configData.Settings.HomesEnabled) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandSetHome");
                return;
            }
            var err = CheckPlayer(player, false, CanCraftHome(player));
            if (err != null)
            {
                PrintMsgL(player, $"Home{err}");
                return;
            }
            if (!args[0].All(char.IsLetterOrDigit))
            {
                PrintMsgL(player, "InvalidCharacter");
                return;
            }
            HomeData homeData;
            if (!Home.TryGetValue(player.userID, out homeData))
                Home[player.userID] = homeData = new HomeData();
            var limit = GetHigher(player, configData.Home.VIPHomesLimits, configData.Home.HomesLimit);
            if (homeData.Locations.Count >= limit)
            {
                PrintMsgL(player, "HomeMaxLocations", limit);
                return;
            }
            Vector3 location;
            if (homeData.Locations.TryGetValue(args[0], out location))
            {
                PrintMsgL(player, "HomeExists", location);
                return;
            }
            var positionCoordinates = player.transform.position;
            foreach (var loc in homeData.Locations)
            {
                if (Vector3.Distance(positionCoordinates, loc.Value) < configData.Home.LocationRadius)
                {
                    PrintMsgL(player, "HomeExistsNearby", loc.Key);
                    return;
                }
            }
            err = CanPlayerTeleport(player);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }

            if (player.IsAdmin())
                player.SendConsoleCommand("ddraw.sphere", 60f, Color.blue, GetGround(positionCoordinates), 2.5f);

            err = CheckFoundation(player.userID, positionCoordinates);
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            homeData.Locations[args[0]] = positionCoordinates;
            changedHome = true;
            PrintMsgL(player, "HomeSave");
            PrintMsgL(player, "HomeQuota", homeData.Locations.Count, limit);
        }

        [ChatCommand("removehome")]
        private void cmdChatRemoveHome(BasePlayer player, string command, string[] args)
        {
            if (!configData.Settings.HomesEnabled) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandRemoveHome");
                return;
            }
            HomeData homeData;
            if (!Home.TryGetValue(player.userID, out homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            if (homeData.Locations.Remove(args[0]))
            {
                changedHome = true;
                PrintMsgL(player, "HomeRemove", args[0]);
            } else
                PrintMsgL(player, "HomeNotFound");
        }

        [ChatCommand("home")]
        private void cmdChatHome(BasePlayer player, string command, string[] args)
        {
            if (!configData.Settings.HomesEnabled) return;
            if (args.Length == 0)
            {
                PrintMsgL(player, "SyntaxCommandHome");
                if (IsAllowed(player)) PrintMsgL(player, "SyntaxCommandHomeAdmin");
                return;
            }
            switch (args[0].ToLower())
            {
                case "add":
                    cmdChatSetHome(player, command, args.Skip(1).ToArray());
                    break;
                case "list":
                    cmdChatListHome(player, command, args.Skip(1).ToArray());
                    break;
                case "remove":
                    cmdChatRemoveHome(player, command, args.Skip(1).ToArray());
                    break;
                case "radius":
                    cmdChatHomeRadius(player, command, args.Skip(1).ToArray());
                    break;
                case "delete":
                    cmdChatHomeDelete(player, command, args.Skip(1).ToArray());
                    break;
                case "tp":
                    cmdChatHomeAdminTP(player, command, args.Skip(1).ToArray());
                    break;
                case "homes":
                    cmdChatHomeHomes(player, command, args.Skip(1).ToArray());
                    break;
                case "wipe":
                    cmdChatWipeHomes(player, command, args.Skip(1).ToArray());
                    break;
                default:
                    cmdChatHomeTP(player, command, args);
                    break;
            }
        }

        [ChatCommand("radiushome")]
        private void cmdChatHomeRadius(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermRadiusHome)) return;
            float radius;
            if (args.Length != 1 || !float.TryParse(args[0], out radius)) radius = 10;
            var found = false;
            foreach (var homeData in Home)
            {
                var toRemove = new List<string>();
                var target = RustCore.FindPlayerById(homeData.Key)?.displayName ?? homeData.Key.ToString();
                foreach (var location in homeData.Value.Locations)
                {
                    if (Vector3.Distance(player.transform.position, location.Value) <= radius)
                    {
                        if (CheckFoundation(homeData.Key, location.Value) != null)
                        {
                            toRemove.Add(location.Key);
                            continue;
                        }
                        var entity = GetFoundationOwned(location.Value, homeData.Key);
                        if (entity == null) continue;
                        player.SendConsoleCommand("ddraw.text", 30f, Color.blue, entity.CenterPoint() + new Vector3(0, .5f), $"<size=20>{target} - {location.Key} {location.Value}</size>");
                        DrawBox(player, entity.CenterPoint(), entity.GetEstimatedWorldRotation(), entity.bounds.size);
                        PrintMsg(player, $"{target} - {location.Key} {location.Value}");
                        found = true;
                    }
                }
                foreach (var loc in toRemove)
                {
                    homeData.Value.Locations.Remove(loc);
                    changedHome = true;
                }
            }
            if (!found)
                PrintMsgL(player, "HomeNoFound");
        }

        [ChatCommand("deletehome")]
        private void cmdChatHomeDelete(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermDeleteHome)) return;
            if (args.Length != 2)
            {
                PrintMsgL(player, "SyntaxCommandHomeDelete");
                return;
            }
            var userId = FindPlayersSingleId(args[0], player);
            if (userId <= 0) return;
            HomeData targetHome;
            if (!Home.TryGetValue(userId, out targetHome) || !targetHome.Locations.Remove(args[1]))
            {
                PrintMsgL(player, "HomeNotFound");
                return;
            }
            changedHome = true;
            PrintMsgL(player, "HomeDelete", args[0], args[1]);
        }

        [ChatCommand("tphome")]
        private void cmdChatHomeAdminTP(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermTpHome)) return;
            if (args.Length != 2)
            {
                PrintMsgL(player, "SyntaxCommandHomeAdminTP");
                return;
            }
            var userId = FindPlayersSingleId(args[0], player);
            if (userId <= 0) return;
            HomeData targetHome;
            Vector3 location;
            if (!Home.TryGetValue(userId, out targetHome) || !targetHome.Locations.TryGetValue(args[1], out location))
            {
                PrintMsgL(player, "HomeNotFound");
                return;
            }
            Teleport(player, location);
            PrintMsgL(player, "HomeAdminTP", args[0], args[1]);
        }

        private void cmdChatHomeTP(BasePlayer player, string command, string[] args)
        {
            if (!configData.Settings.HomesEnabled) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandHome");
                return;
            }
            var err = CheckPlayer(player, configData.Home.UsableOutOfBuildingBlocked, CanCraftHome(player));
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            HomeData homeData;
            if (!Home.TryGetValue(player.userID, out homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            Vector3 location;
            if (!homeData.Locations.TryGetValue(args[0], out location))
            {
                PrintMsgL(player, "HomeNotFound");
                return;
            }
            err = CheckFoundation(player.userID, location);
            if (err != null)
            {
                PrintMsgL(player, "HomeRemovedInvalid", args[0]);
                homeData.Locations.Remove(args[0]);
                changedHome = true;
                return;
            }
            var timestamp = Facepunch.Math.Epoch.Current;
            var currentDate = DateTime.Now.ToString("d");
            if (homeData.Teleports.Date != currentDate)
            {
                homeData.Teleports.Amount = 0;
                homeData.Teleports.Date = currentDate;
            }
            var cooldown = GetLower(player, configData.Home.VIPCooldowns, configData.Home.Cooldown);
            if (cooldown > 0 && timestamp - homeData.Teleports.Timestamp < cooldown)
            {
                var remain = cooldown - (timestamp - homeData.Teleports.Timestamp);
                PrintMsgL(player, "HomeTPCooldown", FormatTime(remain));
                return;
            }
            var limit = GetHigher(player, configData.Home.VIPDailyLimits, configData.Home.DailyLimit);
            if (limit > 0 && homeData.Teleports.Amount >= limit)
            {
                PrintMsgL(player, "HomeTPLimitReached", limit);
                return;
            }
            if (TeleportTimers.ContainsKey(player.userID))
            {
                PrintMsgL(player, "TeleportPending");
                return;
            }
            err = CanPlayerTeleport(player);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }
            err = CheckItems(player);
            if (err != null)
            {
                PrintMsgL(player, "TPBlockedItem", err);
                return;
            }
            var countdown = GetLower(player, configData.Home.VIPCountdowns, configData.Home.Countdown);
            TeleportTimers[player.userID] = new TeleportTimer
            {
                OriginPlayer = player,
                Timer = timer.Once(countdown, () =>
                {
                    err = CheckPlayer(player, configData.Home.UsableOutOfBuildingBlocked, CanCraftHome(player));
                    if (err != null)
                    {
                        PrintMsgL(player, "Interrupted");
                        PrintMsgL(player, err);
                        TeleportTimers.Remove(player.userID);
                        return;
                    }
                    err = CanPlayerTeleport(player);
                    if (err != null)
                    {
                        PrintMsgL(player, "Interrupted");
                        SendReply(player, err);
                        TeleportTimers.Remove(player.userID);
                        return;
                    }
                    err = CheckItems(player);
                    if (err != null)
                    {
                        PrintMsgL(player, "Interrupted");
                        PrintMsgL(player, "TPBlockedItem", err);
                        TeleportTimers.Remove(player.userID);
                        return;
                    }
                    Teleport(player, location);
                    homeData.Teleports.Amount++;
                    homeData.Teleports.Timestamp = timestamp;
                    changedHome = true;
                    PrintMsgL(player, "HomeTP", args[0]);
                    TeleportTimers.Remove(player.userID);
                })
            };
            PrintMsgL(player, "HomeTPStarted", args[0], countdown);
        }

        [ChatCommand("listhomes")]
        private void cmdChatListHome(BasePlayer player, string command, string[] args)
        {
            if (!configData.Settings.HomesEnabled) return;
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandListHomes");
                return;
            }
            HomeData homeData;
            if (!Home.TryGetValue(player.userID, out homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            PrintMsgL(player, "HomeList");
            var toRemove = new List<string>();
            foreach (var location in homeData.Locations)
            {
                var err = CheckFoundation(player.userID, location.Value);
                if (err != null)
                {
                    toRemove.Add(location.Key);
                    continue;
                }
                PrintMsgL(player, $"{location.Key} {location.Value}");
            }
            foreach (var loc in toRemove)
            {
                PrintMsgL(player, "HomeRemovedInvalid", loc);
                homeData.Locations.Remove(loc);
                changedHome = true;
            }
        }

        [ChatCommand("homehomes")]
        private void cmdChatHomeHomes(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermHomeHomes)) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandHomeHomes");
                return;
            }
            var userId = FindPlayersSingleId(args[0], player);
            if (userId <= 0) return;
            HomeData homeData;
            if (!Home.TryGetValue(userId, out homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            PrintMsgL(player, "HomeList");
            var toRemove = new List<string>();
            foreach (var location in homeData.Locations)
            {
                var err = CheckFoundation(userId, location.Value);
                if (err != null)
                {
                    toRemove.Add(location.Key);
                    continue;
                }
                PrintMsgL(player, $"{location.Key} {location.Value}");
            }
            foreach (var loc in toRemove)
            {
                PrintMsgL(player, "HomeRemovedInvalid", loc);
                homeData.Locations.Remove(loc);
                changedHome = true;
            }
        }

        [ChatCommand("tpr")]
        private void cmdChatTeleportRequest(BasePlayer player, string command, string[] args)
        {
            if (!configData.Settings.TPREnabled) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandTPR");
                return;
            }
            var targets = FindPlayersOnline(args[0]);
            if (targets.Count <= 0)
            {
                PrintMsgL(player, "PlayerNotFound");
                return;
            }
            if (targets.Count > 1)
            {
                PrintMsgL(player, "MultiplePlayers", string.Join(", ", targets.ConvertAll(p => p.displayName).ToArray()));
                return;
            }
            var target = targets[0];
            if (target == player)
            {
                PrintMsgL(player, "CantTeleportToSelf");
                return;
            }
            var err = CheckPlayer(player, configData.TPR.UsableOutOfBuildingBlocked, CanCraftTPR(player));
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            var timestamp = Facepunch.Math.Epoch.Current;
            var currentDate = DateTime.Now.ToString("d");
            TeleportData tprData;
            if (!TPR.TryGetValue(player.userID, out tprData))
                TPR[player.userID] = tprData = new TeleportData();
            if (tprData.Date != currentDate)
            {
                tprData.Amount = 0;
                tprData.Date = currentDate;
            }
            var cooldown = GetLower(player, configData.TPR.VIPCooldowns, configData.TPR.Cooldown);
            if (cooldown > 0 && timestamp - tprData.Timestamp < cooldown)
            {
                var remain = cooldown - (timestamp - tprData.Timestamp);
                PrintMsgL(player, "TPRCooldown", FormatTime(remain));
                return;
            }
            var limit = GetHigher(player, configData.TPR.VIPDailyLimits, configData.TPR.DailyLimit);
            if (limit > 0 && tprData.Amount >= limit)
            {
                PrintMsgL(player, "TPRLimitReached", limit);
                return;
            }
            if (TeleportTimers.ContainsKey(player.userID))
            {
                PrintMsgL(player, "TeleportPending");
                return;
            }
            if (TeleportTimers.ContainsKey(target.userID))
            {
                PrintMsgL(player, "TeleportPendingTarget");
                return;
            }
            if (PlayersRequests.ContainsKey(player.userID))
            {
                PrintMsgL(player, "PendingRequest");
                return;
            }
            if (PlayersRequests.ContainsKey(target.userID))
            {
                PrintMsgL(player, "PendingRequestTarget");
                return;
            }
            err = CanPlayerTeleport(player);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }
            err = CanPlayerTeleport(target);
            if (err != null)
            {
                PrintMsgL(player, "TPRTarget");
                return;
            }
            err = CheckItems(player);
            if (err != null)
            {
                PrintMsgL(player, "TPBlockedItem", err);
                return;
            }
            PlayersRequests[player.userID] = target;
            PlayersRequests[target.userID] = player;
            PendingRequests[target.userID] = timer.Once(configData.TPR.RequestDuration, () => {
                RequestTimedOut(player, target);
            });
            PrintMsgL(player, "Request", target.displayName);
            PrintMsgL(target, "RequestTarget", player.displayName);
        }

        [ChatCommand("tpa")]
        private void cmdChatTeleportAccept(BasePlayer player, string command, string[] args)
        {
            if (!configData.Settings.TPREnabled) return;
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandTPA");
                return;
            }
            Timer reqTimer;
            if (!PendingRequests.TryGetValue(player.userID, out reqTimer))
            {
                PrintMsgL(player, "NoPendingRequest");
                return;
            }
            var err = CheckPlayer(player, false, CanCraftTPR(player));
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            err = CanPlayerTeleport(player);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }
            var originPlayer = PlayersRequests[player.userID];
            if (configData.TPR.BlockTPAOnCeiling)
            {
                var position = GetGround(player.transform.position);
                if (Vector3.Distance(position, player.transform.position) > 2)
                {
                    RaycastHit hitInfo;
                    BaseEntity entity = null;
                    if (Physics.SphereCast(player.transform.position, .5f, Vector3.down, out hitInfo, 5, blockLayer))
                        entity = hitInfo.GetEntity();
                    if (entity != null && !entity.PrefabName.Contains("foundation"))
                    {
                        PrintMsgL(player, "AcceptOnRoof");
                        return;
                    }
                }
            }
            var countdown = GetLower(originPlayer, configData.TPR.VIPCountdowns, configData.TPR.Countdown);
            PrintMsgL(originPlayer, "Accept", player.displayName, countdown);
            PrintMsgL(player, "AcceptTarget", originPlayer.displayName);
            var timestamp = Facepunch.Math.Epoch.Current;
            TeleportTimers[originPlayer.userID] = new TeleportTimer
            {
                OriginPlayer = originPlayer,
                TargetPlayer = player,
                Timer = timer.Once(countdown, () =>
                {
                    err = CheckPlayer(originPlayer, configData.TPR.UsableOutOfBuildingBlocked, CanCraftTPR(originPlayer)) ?? CheckPlayer(player, false, CanCraftTPR(player));
                    if (err != null)
                    {
                        PrintMsgL(player, "InterruptedTarget", originPlayer.displayName);
                        PrintMsgL(originPlayer, "Interrupted");
                        PrintMsgL(originPlayer, err);
                        TeleportTimers.Remove(originPlayer.userID);
                        return;
                    }
                    err = CanPlayerTeleport(originPlayer) ?? CanPlayerTeleport(player);
                    if (err != null)
                    {
                        SendReply(player, err);
                        PrintMsgL(originPlayer, "Interrupted");
                        SendReply(originPlayer, err);
                        TeleportTimers.Remove(originPlayer.userID);
                        return;
                    }
                    err = CheckItems(originPlayer);
                    if (err != null)
                    {
                        PrintMsgL(player, "InterruptedTarget", originPlayer.displayName);
                        PrintMsgL(originPlayer, "Interrupted");
                        PrintMsgL(originPlayer, "TPBlockedItem", err);
                        TeleportTimers.Remove(originPlayer.userID);
                        return;
                    }
                    Teleport(originPlayer, CheckPosition(player.transform.position));
                    var tprData = TPR[originPlayer.userID];
                    tprData.Amount++;
                    tprData.Timestamp = timestamp;
                    changedTPR = true;
                    PrintMsgL(player, "SuccessTarget", originPlayer.displayName);
                    PrintMsgL(originPlayer, "Success", player.displayName);
                    var limit = GetHigher(player, configData.TPR.VIPDailyLimits, configData.TPR.DailyLimit);
                    if (limit > 0) PrintMsgL(originPlayer, "TPRAmount", limit - tprData.Amount);
                    TeleportTimers.Remove(originPlayer.userID);
                })
            };
            reqTimer.Destroy();
            PendingRequests.Remove(player.userID);
            PlayersRequests.Remove(player.userID);
            PlayersRequests.Remove(originPlayer.userID);
        }

        [ChatCommand("wipehomes")]
        private void cmdChatWipeHomes(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowedMsg(player, PermWipeHomes)) return;
            Home.Clear();
            changedHome = true;
            PrintMsgL(player, "HomesListWiped");
        }

        [ChatCommand("tphelp")]
        private void cmdChatTeleportHelp(BasePlayer player, string command, string[] args)
        {
            if (!configData.Settings.HomesEnabled && !configData.Settings.TPREnabled && !IsAllowedMsg(player)) return;
            if (args.Length == 1)
            {
                var key = $"TPHelp{args[0].ToLower()}";
                var msg = _(key, player);
                if (key.Equals(msg))
                    PrintMsgL(player, "InvalidHelpModule");
                else
                    PrintMsg(player, msg);
            }
            else
            {
                var msg = _("TPHelpGeneral", player);
                if (IsAllowed(player))
                    msg += NewLine + "/tphelp AdminTP";
                if (configData.Settings.HomesEnabled)
                    msg += NewLine + "/tphelp Home";
                if (configData.Settings.TPREnabled)
                    msg += NewLine + "/tphelp TPR";
                PrintMsg(player, msg);
            }
        }

        [ChatCommand("tpinfo")]
        private void cmdChatTeleportInfo(BasePlayer player, string command, string[] args)
        {
            if (!configData.Settings.HomesEnabled && !configData.Settings.TPREnabled && !configData.Settings.TownEnabled) return;
            if (args.Length == 1)
            {
                var module = args[0].ToLower();
                var msg = _($"TPSettings{module}", player);
                var timestamp = Facepunch.Math.Epoch.Current;
                var currentDate = DateTime.Now.ToString("d");
                TeleportData teleportData;
                int limit;
                int cooldown;
                switch (module)
                {
                    case "home":
                        limit = GetHigher(player, configData.Home.VIPDailyLimits, configData.Home.DailyLimit);
                        cooldown = GetLower(player, configData.Home.VIPCooldowns, configData.Home.Cooldown);
                        PrintMsg(player, string.Format(msg, FormatTime(cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player), GetHigher(player, configData.Home.VIPHomesLimits, configData.Home.HomesLimit)));
                        HomeData homeData;
                        if (!Home.TryGetValue(player.userID, out homeData))
                            Home[player.userID] = homeData = new HomeData();
                        if (homeData.Teleports.Date != currentDate)
                        {
                            homeData.Teleports.Amount = 0;
                            homeData.Teleports.Date = currentDate;
                        }
                        if (limit > 0) PrintMsgL(player, "HomeTPAmount", limit - homeData.Teleports.Amount);
                        if (cooldown > 0 && timestamp - homeData.Teleports.Timestamp < cooldown)
                        {
                            var remain = cooldown - (timestamp - homeData.Teleports.Timestamp);
                            PrintMsgL(player, "HomeTPCooldown", FormatTime(remain));
                        }
                        break;
                    case "tpr":
                        limit = GetHigher(player, configData.TPR.VIPDailyLimits, configData.TPR.DailyLimit);
                        cooldown = GetLower(player, configData.TPR.VIPCooldowns, configData.TPR.Cooldown);
                        PrintMsg(player, string.Format(msg, FormatTime(cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player)));
                        if (!TPR.TryGetValue(player.userID, out teleportData))
                            TPR[player.userID] = teleportData = new TeleportData();
                        if (teleportData.Date != currentDate)
                        {
                            teleportData.Amount = 0;
                            teleportData.Date = currentDate;
                        }
                        if (limit > 0) PrintMsgL(player, "TPRAmount", limit - teleportData.Amount);
                        if (cooldown > 0 && timestamp - teleportData.Timestamp < cooldown)
                        {
                            var remain = cooldown - (timestamp - teleportData.Timestamp);
                            PrintMsgL(player, "TPRCooldown", FormatTime(remain));
                        }
                        break;
                    case "town":
                        limit = GetHigher(player, configData.Town.VIPDailyLimits, configData.Town.DailyLimit);
                        cooldown = GetLower(player, configData.Town.VIPCooldowns, configData.Town.Cooldown);
                        PrintMsg(player, string.Format(msg, FormatTime(cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player)));
                        if (!Town.TryGetValue(player.userID, out teleportData))
                            Town[player.userID] = teleportData = new TeleportData();
                        if (teleportData.Date != currentDate)
                        {
                            teleportData.Amount = 0;
                            teleportData.Date = currentDate;
                        }
                        if (limit > 0) PrintMsgL(player, "TownTPAmount", limit - teleportData.Amount);
                        if (cooldown > 0 && timestamp - teleportData.Timestamp < cooldown)
                        {
                            var remain = cooldown - (timestamp - teleportData.Timestamp);
                            PrintMsgL(player, "TownTPCooldown", FormatTime(remain));
                        }
                        break;
                    default:
                        PrintMsgL(player, "InvalidHelpModule");
                        break;
                }
            }
            else
            {
                var msg = _("TPInfoGeneral", player);
                if (configData.Settings.HomesEnabled)
                    msg += NewLine + "/tpinfo Home";
                if (configData.Settings.TPREnabled)
                    msg += NewLine + "/tpinfo TPR";
                if (configData.Settings.TownEnabled)
                    msg += NewLine + "/tpinfo Town";
                PrintMsgL(player, msg);
            }
        }

        [ChatCommand("tpc")]
        private void cmdChatTeleportCancel(BasePlayer player, string command, string[] args)
        {
            if (!configData.Settings.TPREnabled) return;
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandTPC");
                return;
            }
            TeleportTimer teleportTimer;
            if (TeleportTimers.TryGetValue(player.userID, out teleportTimer))
            {
                teleportTimer.Timer?.Destroy();
                PrintMsgL(player, "TPCancelled");
                PrintMsgL(teleportTimer.TargetPlayer, "TPCancelledTarget", player.displayName);
                TeleportTimers.Remove(player.userID);
                return;
            }
            foreach (var keyValuePair in TeleportTimers)
            {
                if (keyValuePair.Value.TargetPlayer != player) continue;
                keyValuePair.Value.Timer?.Destroy();
                PrintMsgL(keyValuePair.Value.OriginPlayer, "TPCancelledTarget", player.displayName);
                PrintMsgL(player, "TPYouCancelledTarget", keyValuePair.Value.OriginPlayer.displayName);
                TeleportTimers.Remove(keyValuePair.Key);
                return;
            }
            BasePlayer target;
            if (!PlayersRequests.TryGetValue(player.userID, out target))
            {
                PrintMsgL(player, "NoPendingRequest");
                return;
            }
            Timer reqTimer;
            if (PendingRequests.TryGetValue(player.userID, out reqTimer))
            {
                reqTimer.Destroy();
                PendingRequests.Remove(player.userID);
            }
            else if (PendingRequests.TryGetValue(target.userID, out reqTimer))
            {
                reqTimer.Destroy();
                PendingRequests.Remove(target.userID);
                var temp = player;
                player = target;
                target = temp;
            }
            PlayersRequests.Remove(target.userID);
            PlayersRequests.Remove(player.userID);
            PrintMsgL(player, "Cancelled", target.displayName);
            PrintMsgL(target, "CancelledTarget", player.displayName);
        }

        [ChatCommand("town")]
        private void cmdChatTown(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 1 && IsAllowed(player) && args[0].ToLower().Equals("set"))
            {
                configData.Town.Location = player.transform.position;
                Config.WriteObject(configData, true);
                PrintMsgL(player, "TownTPLocation", configData.Town.Location);
                return;
            }
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandTown");
                if (IsAllowed(player)) PrintMsgL(player, "SyntaxCommandTownAdmin");
                return;
            }
            if (configData.Town.Location == default(Vector3))
            {
                PrintMsgL(player, "TownTPNotSet");
                return;
            }
            var err = CheckPlayer(player, configData.Town.UsableOutOfBuildingBlocked, CanCraftTown(player));
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            TeleportData teleportData;
            if (!Town.TryGetValue(player.userID, out teleportData))
                Town[player.userID] = teleportData = new TeleportData();
            var timestamp = Facepunch.Math.Epoch.Current;
            var currentDate = DateTime.Now.ToString("d");
            if (teleportData.Date != currentDate)
            {
                teleportData.Amount = 0;
                teleportData.Date = currentDate;
            }
            var cooldown = GetLower(player, configData.Town.VIPCooldowns, configData.Town.Cooldown);
            if (cooldown > 0 && timestamp - teleportData.Timestamp < cooldown)
            {
                var remain = cooldown - (timestamp - teleportData.Timestamp);
                PrintMsgL(player, "TownTPCooldown", FormatTime(remain));
                return;
            }
            var limit = GetHigher(player, configData.Town.VIPDailyLimits, configData.Town.DailyLimit);
            if (limit > 0 && teleportData.Amount >= limit)
            {
                PrintMsgL(player, "TownTPLimitReached", limit);
                return;
            }
            if (TeleportTimers.ContainsKey(player.userID))
            {
                PrintMsgL(player, "TeleportPending");
                return;
            }
            err = CanPlayerTeleport(player);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }
            err = CheckItems(player);
            if (err != null)
            {
                PrintMsgL(player, "TPBlockedItem", err);
                return;
            }
            var countdown = GetLower(player, configData.Town.VIPCountdowns, configData.Town.Countdown);
            TeleportTimers[player.userID] = new TeleportTimer
            {
                OriginPlayer = player,
                Timer = timer.Once(countdown, () =>
                {
                    err = CheckPlayer(player, configData.Town.UsableOutOfBuildingBlocked, CanCraftTown(player));
                    if (err != null)
                    {
                        PrintMsgL(player, "Interrupted");
                        PrintMsgL(player, err);
                        TeleportTimers.Remove(player.userID);
                        return;
                    }
                    err = CanPlayerTeleport(player);
                    if (err != null)
                    {
                        PrintMsgL(player, "Interrupted");
                        SendReply(player, err);
                        TeleportTimers.Remove(player.userID);
                        return;
                    }
                    err = CheckItems(player);
                    if (err != null)
                    {
                        PrintMsgL(player, "Interrupted");
                        PrintMsgL(player, "TPBlockedItem", err);
                        TeleportTimers.Remove(player.userID);
                        return;
                    }
                    Teleport(player, configData.Town.Location);
                    teleportData.Amount++;
                    teleportData.Timestamp = timestamp;
                    changedTown = true;
                    PrintMsgL(player, "TownTP");
                    TeleportTimers.Remove(player.userID);
                })
            };
            PrintMsgL(player, "TownTPStarted", countdown);
        }

        private bool ccmdTeleport(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !IsAllowedMsg(arg.Player(), PermTpConsole)) return false;
            HashSet<BasePlayer> players;
            switch (arg.cmd.namefull)
            {
                case "teleport.topos":
                    if (!arg.HasArgs(4))
                    {
                        arg.ReplyWith(_("SyntaxConsoleCommandToPos", arg.Player()));
                        return false;
                    }
                    players = FindPlayers(arg.GetString(0));
                    if (players.Count <= 0)
                    {
                        arg.ReplyWith(_("PlayerNotFound", arg.Player()));
                        return false;
                    }
                    if (players.Count > 1)
                    {
                        arg.ReplyWith(_("MultiplePlayers", arg.Player(), string.Join(", ", players.Select(p => p.displayName).ToArray())));
                        return false;
                    }
                    var targetPlayer = players.First();
                    var x = arg.GetFloat(1, -10000);
                    var y = arg.GetFloat(2, -10000);
                    var z = arg.GetFloat(3, -10000);
                    if (!CheckBoundaries(x, y, z))
                    {
                        arg.ReplyWith(_("AdminTPOutOfBounds", arg.Player()) + Environment.NewLine + _("AdminTPBoundaries", arg.Player(), boundary));
                        return false;
                    }
                    TeleportToPosition(targetPlayer, x, y, z);
                    PrintMsgL(targetPlayer, "AdminTPConsoleTP", targetPlayer.transform.position);
                    arg.ReplyWith(_("AdminTPTargetCoordinates", arg.Player(), targetPlayer.displayName, targetPlayer.transform.position));
                    Puts(_("LogTeleportPlayer", null, arg.Player()?.displayName, targetPlayer.displayName, targetPlayer.transform.position));
                    break;
                case "teleport.toplayer":
                    if (!arg.HasArgs(2))
                    {
                        arg.ReplyWith(_("SyntaxConsoleCommandToPlayer", arg.Player()));
                        return false;
                    }
                    players = FindPlayers(arg.GetString(0));
                    if (players.Count <= 0)
                    {
                        arg.ReplyWith(_("PlayerNotFound", arg.Player()));
                        return false;
                    }
                    if (players.Count > 1)
                    {
                        arg.ReplyWith(_("MultiplePlayers", arg.Player(), string.Join(", ", players.Select(p => p.displayName).ToArray())));
                        return false;
                    }
                    var originPlayer = players.First();
                    players = FindPlayers(arg.GetString(1));
                    if (players.Count <= 0)
                    {
                        arg.ReplyWith(_("PlayerNotFound", arg.Player()));
                        return false;
                    }
                    if (players.Count > 1)
                    {
                        arg.ReplyWith(_("MultiplePlayers", arg.Player(), string.Join(", ", players.Select(p => p.displayName).ToArray())));
                        return false;
                    }
                    targetPlayer = players.First();
                    if (targetPlayer == originPlayer)
                    {
                        arg.ReplyWith(_("CantTeleportPlayerToSelf", arg.Player()));
                        return false;
                    }
                    TeleportToPlayer(originPlayer, targetPlayer);
                    arg.ReplyWith(_("AdminTPPlayers", arg.Player(), originPlayer.displayName, targetPlayer.displayName));
                    PrintMsgL(originPlayer, "AdminTPConsoleTPPlayer", targetPlayer.displayName);
                    PrintMsgL(targetPlayer, "AdminTPConsoleTPPlayerTarget", originPlayer.displayName);
                    Puts(_("LogTeleportPlayer", null, arg.Player()?.displayName, originPlayer.displayName, targetPlayer.displayName));
                    break;
            }
            return false;
        }

        [ConsoleCommand("teleport.importhomes")]
        private bool ccmdImportHomes(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !IsAllowedMsg(arg.Player(), PermImportHomes))
            {
                arg.ReplyWith("Not allowed.");
                return false;
            }
            var datafile = Interface.Oxide.DataFileSystem.GetFile("m-Teleportation");
            if (!datafile.Exists())
            {
                arg.ReplyWith("No m-Teleportation.json exists.");
                return false;
            }
            datafile.Load();
            var allHomeData = datafile["HomeData"] as Dictionary<string, object>;
            if (allHomeData == null)
            {
                arg.ReplyWith("Empty HomeData.");
                return false;
            }
            var count = 0;
            foreach (var kvp in allHomeData)
            {
                var homeDataOld = kvp.Value as Dictionary<string, object>;
                if (homeDataOld == null) continue;
                if (!homeDataOld.ContainsKey("HomeLocations")) continue;
                var homeList = homeDataOld["HomeLocations"] as Dictionary<string, object>;
                if (homeList == null) continue;
                var userId = Convert.ToUInt64(kvp.Key);
                HomeData homeData;
                if (!Home.TryGetValue(userId, out homeData))
                    Home[userId] = homeData = new HomeData();
                foreach (var kvp2 in homeList)
                {
                    var positionData = kvp2.Value as Dictionary<string, object>;
                    if (positionData == null) continue;
                    if (!positionData.ContainsKey("x") || !positionData.ContainsKey("y") || !positionData.ContainsKey("z")) continue;
                    var position = new Vector3(Convert.ToSingle(positionData["x"]), Convert.ToSingle(positionData["y"]), Convert.ToSingle(positionData["z"]));
                    homeData.Locations[kvp2.Key] = position;
                    changedHome = true;
                    count++;
                }
            }
            arg.ReplyWith(string.Format("Imported {0} homes.", count));
            return false;
        }

        private void RequestTimedOut(BasePlayer player, BasePlayer target)
        {
            PlayersRequests.Remove(player.userID);
            PlayersRequests.Remove(target.userID);
            PendingRequests.Remove(target.userID);
            PrintMsgL(player, "TimedOut", target.displayName);
            PrintMsgL(target, "TimedOutTarget", player.displayName);
        }

        #region Util

        private string FormatTime(long seconds)
        {
            var timespan = TimeSpan.FromSeconds(seconds);
            return string.Format(timespan.TotalHours >= 1 ? "{2:00}:{0:00}:{1:00}" : "{0:00}:{1:00}", timespan.Minutes, timespan.Seconds, System.Math.Floor(timespan.TotalHours));
        }

        private double ConvertToRadians(double angle)
        {
            return System.Math.PI / 180 * angle;
        }

        #region Teleport

        public void TeleportToPlayer(BasePlayer player, BasePlayer target) => Teleport(player, target.transform.position);

        public void TeleportToPosition(BasePlayer player, float x, float y, float z) => Teleport(player, new Vector3(x, y, z));

        public void Teleport(BasePlayer player, Vector3 position)
        {
            SaveLocation(player);
            teleporting.Add(player.userID);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            StartSleeping(player);
            player.MovePosition(position);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            player.TransformChanged();
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            //player.UpdatePlayerCollider(true, false);
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            //TODO temporary for potential rust bug
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }

        private void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
            //player.inventory.crafting.CancelAll(true);
            //player.UpdatePlayerCollider(true, false);
        }

        #endregion

        #region Checks

        private Vector3 CheckPosition(Vector3 position)
        {
            var hits = Physics.OverlapSphere(position, 2, blockLayer);
            var distance = 5f;
            BuildingBlock buildingBlock = null;
            for (var i = 0; i < hits.Length; i++)
            {
                var block = hits[i].GetComponentInParent<BuildingBlock>();
                if (block == null) continue;
                var prefab = block.PrefabName;
                if (!prefab.Contains("foundation", CompareOptions.OrdinalIgnoreCase) && !prefab.Contains("floor", CompareOptions.OrdinalIgnoreCase) && !prefab.Contains("pillar", CompareOptions.OrdinalIgnoreCase)) continue;
                if (!(Vector3.Distance(block.transform.position, position) < distance)) continue;
                buildingBlock = block;
                distance = Vector3.Distance(block.transform.position, position);
            }
            if (buildingBlock == null) return position;
            var blockRotation = buildingBlock.transform.rotation.eulerAngles.y;
            var angles = new[] { 360 - blockRotation, 180 - blockRotation };
            var location = default(Vector3);
            const double r = 1.9;
            var locationDistance = 100f;
            for (var i = 0; i < angles.Length; i++)
            {
                var radians = ConvertToRadians(angles[i]);
                var newX = r * System.Math.Cos(radians);
                var newZ = r * System.Math.Sin(radians);
                var newLoc = new Vector3((float)(buildingBlock.transform.position.x + newX), buildingBlock.transform.position.y + .2f, (float)(buildingBlock.transform.position.z + newZ));
                if (Vector3.Distance(position, newLoc) < locationDistance)
                {
                    location = newLoc;
                    locationDistance = Vector3.Distance(position, newLoc);
                }
            }
            return location;
        }

        private string CanPlayerTeleport(BasePlayer player)
        {
            return Interface.Oxide.CallHook("CanTeleport", player) as string;
        }

        private bool CanCraftHome(BasePlayer player)
        {
            return configData.Home.AllowCraft || permission.UserHasPermission(player.UserIDString, PermCraftHome);
        }

        private bool CanCraftTown(BasePlayer player)
        {
            return configData.Town.AllowCraft || permission.UserHasPermission(player.UserIDString, PermCraftTown);
        }

        private bool CanCraftTPR(BasePlayer player)
        {
            return configData.TPR.AllowCraft || permission.UserHasPermission(player.UserIDString, PermCraftTpR);
        }

        private string CheckPlayer(BasePlayer player, bool build = false, bool craft = false)
        {
            if (!player.IsAlive())
                return "TPDead";
            if (!build && !player.CanBuild())
                return "TPBuildingBlocked";
            if (player.IsSwimming())
                return "TPSwimming";
            if (!craft && player.inventory.crafting.queue.Count > 0)
                return "TPCrafting";
            return null;
        }

        private string CheckItems(BasePlayer player)
        {
            foreach (var blockedItem in ReverseBlockedItems)
            {
                if (player.inventory.containerMain.GetAmount(blockedItem.Key, true) > 0)
                    return blockedItem.Value;
                if (player.inventory.containerBelt.GetAmount(blockedItem.Key, true) > 0)
                    return blockedItem.Value;
                if (player.inventory.containerWear.GetAmount(blockedItem.Key, true) > 0)
                    return blockedItem.Value;
            }
            return null;
        }

        private string CheckFoundation(ulong userID, Vector3 position)
        {
            if (!configData.Home.ForceOnTopOfFoundation) return null;
            var entities = GetFoundation(position);
            if (entities.Count == 0)
                return "HomeNoFoundation";
            if (!configData.Home.CheckFoundationForOwner) return null;
            for (var i = 0; i < entities.Count; i++)
                if (entities[i].OwnerID == userID) return null;
            if (!configData.Home.UseFriends)
                return "HomeFoundationNotOwned";
            var moderator = (bool)(Clans?.CallHook("IsModerator", userID) ?? false);
            var userIdString = userID.ToString();
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if ((bool) (Friends?.CallHook("HasFriend", entity.OwnerID, userID) ?? false) || (bool) (Clans?.CallHook("HasFriend", entity.OwnerID, userID) ?? false) && moderator || (bool) (RustIO?.CallHook("HasFriend", entity.OwnerID.ToString(), userIdString) ?? false))
                    return null;
            }
            return "HomeFoundationNotFriendsOwned";
        }

        private BuildingBlock GetFoundationOwned(Vector3 position, ulong userID)
        {
            var entities = GetFoundation(position);
            if (entities.Count == 0)
                return null;
            if (!configData.Home.CheckFoundationForOwner) return entities[0];
            for (var i = 0; i < entities.Count; i++)
                if (entities[i].OwnerID == userID) return entities[i];
            if (!configData.Home.UseFriends)
                return null;
            var moderator = (bool)(Clans?.CallHook("IsModerator", userID) ?? false);
            var userIdString = userID.ToString();
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if ((bool)(Friends?.CallHook("HasFriend", entity.OwnerID, userID) ?? false) || (bool)(Clans?.CallHook("HasFriend", entity.OwnerID, userID) ?? false) && moderator || (bool)(RustIO?.CallHook("HasFriend", entity.OwnerID.ToString(), userIdString) ?? false))
                    return entity;
            }
            return null;
        }

        private List<BuildingBlock> GetFoundation(Vector3 positionCoordinates)
        {
            var position = GetGround(positionCoordinates);
            var entities = new List<BuildingBlock>();
            var hits = Pool.GetList<BuildingBlock>();
            Vis.Entities(position, 2.5f, hits, buildingLayer);
            for (var i = 0; i < hits.Count; i++)
            {
                var entity = hits[i];
                if (!entity.PrefabName.Contains("foundation") || positionCoordinates.y < entity.WorldSpaceBounds().ToBounds().max.y) continue;
                entities.Add(entity);
            }
            Pool.FreeList(ref hits);
            return entities;
        }

        private bool CheckBoundaries(float x, float y, float z)
        {
            return x <= boundary && x >= -boundary && y < 2000 && y >= -100 && z <= boundary && z >= -boundary;
        }

        private Vector3 GetGround(Vector3 sourcePos)
        {
            if (!configData.Home.AllowAboveFoundation) return sourcePos;
            var oldPos = sourcePos;
            sourcePos.y = TerrainMeta.HeightMap.GetHeight(sourcePos);
            RaycastHit hitinfo;
            if (configData.Home.AllowCave && Physics.SphereCast(oldPos, .1f, Vector3.down, out hitinfo, groundLayer) && hitinfo.collider.name.Contains("rock_"))
                sourcePos.y = hitinfo.point.y;
            if (configData.Home.AllowIceberg && Physics.SphereCast(sourcePos, .1f, Vector3.up, out hitinfo, groundLayer) && hitinfo.collider.name.Contains("iceberg"))
            {
                sourcePos.y = hitinfo.collider.bounds.max.y;
                if (Physics.SphereCast(sourcePos + Vector3.up, .1f, Vector3.down, out hitinfo, groundLayer))
                    sourcePos.y = hitinfo.point.y - 1.25f;
            }
            return sourcePos;
        }

        private Vector3 GetGroundBuilding(Vector3 sourcePos)
        {
            sourcePos.y = TerrainMeta.HeightMap.GetHeight(sourcePos);
            RaycastHit hitinfo;
            if (Physics.Raycast(sourcePos, Vector3.down, out hitinfo, buildingLayer))
            {
                sourcePos.y = System.Math.Max(hitinfo.point.y, sourcePos.y);
                return sourcePos;
            }
            if (Physics.Raycast(sourcePos, Vector3.up, out hitinfo, buildingLayer))
                sourcePos.y = System.Math.Max(hitinfo.point.y, sourcePos.y);
            return sourcePos;
        }

        private bool IsAllowed(BasePlayer player, string perm = null)
        {
            var playerAuthLevel = player.net?.connection?.authLevel;
            var requiredAuthLevel = configData.Admin.UseableByModerators ? 1 : 2;
            if (playerAuthLevel >= requiredAuthLevel) return true;
            return !string.IsNullOrEmpty(perm) && permission.UserHasPermission(player.UserIDString, perm);
        }

        private bool IsAllowedMsg(BasePlayer player, string perm = null)
        {
            if (IsAllowed(player, perm)) return true;
            PrintMsg(player, "NotAllowed");
            return false;
        }

        private int GetHigher(BasePlayer player, Dictionary<string, int> limits, int limit)
        {
            foreach (var l in limits)
            {
                if (permission.UserHasPermission(player.UserIDString, l.Key) && l.Value > limit)
                    limit = l.Value;
            }
            return limit;
        }

        private int GetLower(BasePlayer player, Dictionary<string, int> times, int time)
        {
            foreach (var l in times)
            {
                if (permission.UserHasPermission(player.UserIDString, l.Key) && l.Value < time)
                    time = l.Value;
            }
            return time;
        }

        private void CheckPerms(Dictionary<string, int> limits)
        {
            foreach (var limit in limits)
            {
                if (!permission.PermissionExists(limit.Key))
                    permission.RegisterPermission(limit.Key, this);
            }
        }

        #endregion

        #region Message

        private string _(string msgId, BasePlayer player, params object[] args)
        {
            var msg = lang.GetMessage(msgId, this, player?.UserIDString);
            return args.Length > 0 ? string.Format(msg, args) : msg;
        }

        private void PrintMsgL(BasePlayer player, string msgId, params object[] args)
        {
            if (player == null) return;
            PrintMsg(player, _(msgId, player, args));
        }

        private void PrintMsg(BasePlayer player, string msg)
        {
            if (player == null) return;
            SendReply(player, $"{configData.Settings.ChatName}{msg}");
        }

        #endregion

        #region DrawBox

        private static void DrawBox(BasePlayer player, Vector3 center, Quaternion rotation, Vector3 size)
        {
            size = size / 2;
            var point1 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y + size.y, center.z + size.z), center, rotation);
            var point2 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y - size.y, center.z + size.z), center, rotation);
            var point3 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y + size.y, center.z - size.z), center, rotation);
            var point4 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y - size.y, center.z - size.z), center, rotation);
            var point5 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y + size.y, center.z + size.z), center, rotation);
            var point6 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y - size.y, center.z + size.z), center, rotation);
            var point7 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y + size.y, center.z - size.z), center, rotation);
            var point8 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y - size.y, center.z - size.z), center, rotation);

            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point1, point2);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point1, point3);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point1, point5);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point4, point2);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point4, point3);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point4, point8);

            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point5, point6);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point5, point7);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point6, point2);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point8, point6);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point8, point7);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point7, point3);
        }

        private static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            return rotation * (point - pivot) + pivot;
        }

        #endregion

        #region FindPlayer

        private ulong FindPlayersSingleId(string nameOrIdOrIp, BasePlayer player)
        {
            var targets = FindPlayers(nameOrIdOrIp);
            if (targets.Count > 1)
            {
                PrintMsgL(player, "MultiplePlayers", string.Join(", ", targets.Select(p => p.displayName).ToArray()));
                return 0;
            }
            ulong userId;
            if (targets.Count <= 0)
            {
                if (!ulong.TryParse(nameOrIdOrIp, out userId))
                {
                    PrintMsgL(player, "PlayerNotFound");
                    return 0;
                }
            }
            else
                userId = targets.First().userID;
            return userId;
        }

        private BasePlayer FindPlayersSingle(string nameOrIdOrIp, BasePlayer player)
        {
            var targets = FindPlayers(nameOrIdOrIp);
            if (targets.Count <= 0)
            {
                PrintMsgL(player, "PlayerNotFound");
                return null;
            }
            if (targets.Count > 1)
            {
                PrintMsgL(player, "MultiplePlayers", string.Join(", ", targets.Select(p => p.displayName).ToArray()));
                return null;
            }
            return targets.First();
        }

        private static HashSet<BasePlayer> FindPlayers(string nameOrIdOrIp)
        {
            var players = new HashSet<BasePlayer>();
            if (string.IsNullOrEmpty(nameOrIdOrIp)) return players;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(sleepingPlayer);
                else if (sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(sleepingPlayer);
            }
            return players;
        }

        private static List<BasePlayer> FindPlayersOnline(string nameOrIdOrIp)
        {
            var players = new List<BasePlayer>();
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            return players;
        }

        #endregion

        #endregion

        #region API

        private Dictionary<string, Vector3> GetHomes(object playerObj)
        {
            if (playerObj == null) return null;
            if (playerObj is string)
                playerObj = Convert.ToUInt64(playerObj);
            if (!(playerObj is ulong))
                throw new ArgumentException("playerObj");
            var playerId = (ulong) playerObj;
            HomeData homeData;
            if (!Home.TryGetValue(playerId, out homeData) || homeData.Locations.Count == 0)
                return null;
            return homeData.Locations;
        }

        private int GetLimitRemaining(BasePlayer player, string type)
        {
            if (player == null || string.IsNullOrEmpty(type)) return 0;
            var currentDate = DateTime.Now.ToString("d");
            int limit;
            var remaining = -1;
            switch (type.ToLower())
            {
                case "home":
                    limit = GetHigher(player, configData.Home.VIPDailyLimits, configData.Home.DailyLimit);
                    HomeData homeData;
                    if (!Home.TryGetValue(player.userID, out homeData))
                        Home[player.userID] = homeData = new HomeData();
                    if (homeData.Teleports.Date != currentDate)
                    {
                        homeData.Teleports.Amount = 0;
                        homeData.Teleports.Date = currentDate;
                    }
                    if (limit > 0)
                        remaining = limit - homeData.Teleports.Amount;
                    break;
                case "town":
                    limit = GetHigher(player, configData.Town.VIPDailyLimits, configData.Town.DailyLimit);
                    TeleportData townData;
                    if (!Town.TryGetValue(player.userID, out townData))
                        Town[player.userID] = townData = new TeleportData();
                    if (townData.Date != currentDate)
                    {
                        townData.Amount = 0;
                        townData.Date = currentDate;
                    }
                    if (limit > 0)
                        remaining = limit - townData.Amount;
                    break;
                case "tpr":
                    limit = GetHigher(player, configData.TPR.VIPDailyLimits, configData.TPR.DailyLimit);
                    TeleportData tprData;
                    if (!TPR.TryGetValue(player.userID, out tprData))
                        TPR[player.userID] = tprData = new TeleportData();
                    if (tprData.Date != currentDate)
                    {
                        tprData.Amount = 0;
                        tprData.Date = currentDate;
                    }
                    if (limit > 0)
                        remaining = limit - tprData.Amount;
                    break;
            }
            return remaining;
        }

        private int GetCooldownRemaining(BasePlayer player, string type)
        {
            if (player == null || string.IsNullOrEmpty(type)) return 0;
            var currentDate = DateTime.Now.ToString("d");
            var timestamp = Facepunch.Math.Epoch.Current;
            int cooldown;
            var remaining = -1;
            switch (type.ToLower())
            {
                case "home":
                    cooldown = GetLower(player, configData.Home.VIPCooldowns, configData.Home.Cooldown);
                    HomeData homeData;
                    if (!Home.TryGetValue(player.userID, out homeData))
                        Home[player.userID] = homeData = new HomeData();
                    if (homeData.Teleports.Date != currentDate)
                    {
                        homeData.Teleports.Amount = 0;
                        homeData.Teleports.Date = currentDate;
                    }
                    if (cooldown > 0 && timestamp - homeData.Teleports.Timestamp < cooldown)
                        remaining = cooldown - (timestamp - homeData.Teleports.Timestamp);
                    break;
                case "town":
                    cooldown = GetLower(player, configData.Town.VIPCooldowns, configData.Town.Cooldown);
                    TeleportData townData;
                    if (!Town.TryGetValue(player.userID, out townData))
                        Town[player.userID] = townData = new TeleportData();
                    if (townData.Date != currentDate)
                    {
                        townData.Amount = 0;
                        townData.Date = currentDate;
                    }
                    if (cooldown > 0 && timestamp - townData.Timestamp < cooldown)
                        remaining = cooldown - (timestamp - townData.Timestamp);
                    break;
                case "tpr":
                    cooldown = GetLower(player, configData.TPR.VIPCooldowns, configData.TPR.Cooldown);
                    TeleportData tprData;
                    if (!TPR.TryGetValue(player.userID, out tprData))
                        TPR[player.userID] = tprData = new TeleportData();
                    if (tprData.Date != currentDate)
                    {
                        tprData.Amount = 0;
                        tprData.Date = currentDate;
                    }
                    if (cooldown > 0 && timestamp - tprData.Timestamp < cooldown)
                        remaining = cooldown - (timestamp - tprData.Timestamp);
                    break;
            }
            return remaining;
        }

        #endregion

        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }

        private class CustomComparerDictionaryCreationConverter<T> : CustomCreationConverter<IDictionary>
        {
            private readonly IEqualityComparer<T> comparer;

            public CustomComparerDictionaryCreationConverter(IEqualityComparer<T> comparer)
            {
                if (comparer == null)
                    throw new ArgumentNullException(nameof(comparer));
                this.comparer = comparer;
            }

            public override bool CanConvert(Type objectType)
            {
                return HasCompatibleInterface(objectType) && HasCompatibleConstructor(objectType);
            }

            private static bool HasCompatibleInterface(Type objectType)
            {
                return objectType.GetInterfaces().Where(i => HasGenericTypeDefinition(i, typeof(IDictionary<,>))).Any(i => typeof(T).IsAssignableFrom(i.GetGenericArguments().First()));
            }

            private static bool HasGenericTypeDefinition(Type objectType, Type typeDefinition)
            {
                return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeDefinition;
            }

            private static bool HasCompatibleConstructor(Type objectType)
            {
                return objectType.GetConstructor(new[] { typeof(IEqualityComparer<T>) }) != null;
            }

            public override IDictionary Create(Type objectType)
            {
                return Activator.CreateInstance(objectType, comparer) as IDictionary;
            }
        }

    }
}
