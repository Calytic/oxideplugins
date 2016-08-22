using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Death Notes", "LaserHydra", "5.2.8", ResourceId = 819)]
    [Description("Broadcast deaths with many details")]
    class DeathNotes : RustPlugin
    {
        #region Global Declaration

        bool debug = false;
        bool killReproducing = false;

        Dictionary<ulong, HitInfo> LastWounded = new Dictionary<ulong, HitInfo>();

        Dictionary<string, string> reproduceableKills = new Dictionary<string, string>();

        Dictionary<BasePlayer, Timer> timers = new Dictionary<BasePlayer, Timer>();

        Dictionary<ulong, PlayerSettings> playerSettings = new Dictionary<ulong, PlayerSettings>();

        Plugin PopupNotifications;
        static DeathNotes dn;

        #region Cached Variables

        UIColor deathNoticeShadowColor = new UIColor(0.1, 0.1, 0.1, 0.8);
        UIColor deathNoticeColor = new UIColor(0.85, 0.85, 0.85, 0.1);

        List<string> selfInflictedDeaths = new List<string> { "Cold", "Drowned", "Heat", "Suicide", "Generic", "Posion", "Radiation", "Thirst", "Hunger", "Fall" };

        List<DeathReason> SleepingDeaths = new List<DeathReason>
        {
            DeathReason.Animal,
            DeathReason.Blunt,
            DeathReason.Bullet,
            DeathReason.Explosion,
            DeathReason.Generic,
            DeathReason.Helicopter,
            DeathReason.Slash,
            DeathReason.Stab,
            DeathReason.Unknown
        };

        List<Regex> regexTags = new List<Regex>
        {
            new Regex(@"<color=.+?>", RegexOptions.Compiled),
            new Regex(@"<size=.+?>", RegexOptions.Compiled)
        };

        List<string> tags = new List<string>
        {
            "</color>",
            "</size>",
            "<i>",
            "</i>",
            "<b>",
            "</b>"
        };

        // ------------------->  Config Values

            // ------->   General

                //  Needs Permission to see messages?
                    bool NeedsPermission;

                //  Chat Icon (Steam Profile - SteamID)
                    string ChatIcon;

                //  Message Radius
                    bool MessageRadiusEnabled;
                    float MessageRadius;

                //  Where Should the message appear?
                    bool LogToFile;
                    bool WriteToConsole;
                    bool WriteToChat;
                    bool UsePopupNotifications;
                    bool UseSimpleUI;

                //  Attachments
                    string AttachmentSplit;
                    string AttachmentFormatting;

                //  Other
                    string ChatTitle;
                    string ChatFormatting;
                    string ConsoleFormatting;

            // ------->   Colors
            
                    string TitleColor;
                    string VictimColor;
                    string AttackerColor;
                    string WeaponColor;
                    string AttachmentColor;
                    string DistanceColor;
                    string BodypartColor;
                    string MessageColor;
                    string HealthColor;

            // ------->   Localization

                    Dictionary<string, object> Names;
                    Dictionary<string, object> Bodyparts;
                    Dictionary<string, object> Weapons;
                    Dictionary<string, object> Attachments;

            // ------->   Messages

                    Dictionary<string, List<string>> Messages;

            // ------->   Simple UI

                //  Other
                    bool SimpleUI_StripColors;

                //  Scaling & Positioning
                    int SimpleUI_FontSize;

                    float SimpleUI_Top;
                    float SimpleUI_Left;
                    float SimpleUI_MaxWidth;
                    float SimpleUI_MaxHeight;

                //  Timer
                    float SimpleUI_HideTimer;
        
        // ----------------------------------------------------

        #endregion

        #endregion
        
        #region Classes

        class UIColor
        {
            string color;

            public UIColor(double red, double green, double blue, double alpha)
            {
                color = $"{red} {green} {blue} {alpha}";
            }

            public override string ToString() => color;
        }

        class UIObject
        {
            List<object> ui = new List<object>();
            List<string> objectList = new List<string>();

            public UIObject()
            {
            }

            string RandomString()
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                List<char> charList = chars.ToList();

                string random = "";

                for (int i = 0; i <= UnityEngine.Random.Range(5, 10); i++)
                    random = random + charList[UnityEngine.Random.Range(0, charList.Count - 1)];

                return random;
            }

            public void Draw(BasePlayer player)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(JsonConvert.SerializeObject(ui).Replace("{NEWLINE}", Environment.NewLine)));
            }

            public void Destroy(BasePlayer player)
            {
                foreach (string uiName in objectList)
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList(uiName));
            }

            public string AddText(string name, double left, double top, double width, double height, UIColor color, string text, int textsize = 15, string parent = "Hud.Under", int alignmode = 0, float fadeIn = 0f, float fadeOut = 0f)
            {
                //name = name + RandomString();
                text = text.Replace("\n", "{NEWLINE}");
                string align = "";

                switch (alignmode)
                {
                    case 0: { align = "LowerCenter"; break; };
                    case 1: { align = "LowerLeft"; break; };
                    case 2: { align = "LowerRight"; break; };
                    case 3: { align = "MiddleCenter"; break; };
                    case 4: { align = "MiddleLeft"; break; };
                    case 5: { align = "MiddleRight"; break; };
                    case 6: { align = "UpperCenter"; break; };
                    case 7: { align = "UpperLeft"; break; };
                    case 8: { align = "UpperRight"; break; };
                }

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"fadeOut", fadeOut.ToString()},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Text"},
                                {"text", text},
                                {"fontSize", textsize.ToString()},
                                {"color", color.ToString()},
                                {"align", align},
                                {"fadeIn", fadeIn.ToString()}
                            },
                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left} {((1 - top) - height)}"},
                                {"anchormax", $"{(left + width)} {(1 - top)}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }
        }

        class PlayerSettings
        {
            public bool ui = false;
            public bool chat = true;

            public PlayerSettings()
            {
            }

            internal PlayerSettings(DeathNotes deathnotes)
            {
                ui = dn.UseSimpleUI;
                chat = dn.WriteToChat;
            }
        }

        class Attacker
        {
            public string name = string.Empty;
            [JsonIgnore]
            public BaseCombatEntity entity;
            public AttackerType type = AttackerType.Invalid;
            public float healthLeft;

            public string TryGetName()
            {
                if (entity == null)
                    return "No Attacker";

                if (type == AttackerType.Player)
                    return entity.ToPlayer().displayName;
                if (type == AttackerType.Helicopter)
                    return "Patrol Helicopter";
                if (type == AttackerType.Turret)
                    return "Auto Turret";
                if (type == AttackerType.Self)
                    return "himself";
                if (type == AttackerType.Animal)
                {
                    if (entity.name.Contains("boar"))
                        return "Boar";
                    if (entity.name.Contains("horse"))
                        return "Horse";
                    if (entity.name.Contains("wolf"))
                        return "Wolf";
                    if (entity.name.Contains("stag"))
                        return "Stag";
                    if (entity.name.Contains("chicken"))
                        return "Chicken";
                    if (entity.name.Contains("bear"))
                        return "Bear";
                }
                else if (type == AttackerType.Structure)
                {
                    if (entity.name.Contains("barricade.wood.prefab"))
                        return "Wooden Barricade";
                    if (entity.name.Contains("barricade.woodwire.prefab"))
                        return "Barbed Wooden Barricade";
                    if (entity.name.Contains("barricade.metal.prefab"))
                        return "Metal Barricade";
                    if (entity.name.Contains("wall.external.high.wood.prefab"))
                        return "High External Wooden Wall";
                    if (entity.name.Contains("wall.external.high.stone.prefab"))
                        return "High External Stone Wall";
                    if (entity.name.Contains("gates.external.high.wood.prefab"))
                        return "High External Wooden Gate";
                    if (entity.name.Contains("gates.external.high.wood.prefab"))
                        return "High External Stone Gate";
                }
                else if (type == AttackerType.Trap)
                {
                    if (entity.name.Contains("beartrap.prefab"))
                        return "Snap Trap";
                    if (entity.name.Contains("landmine.prefab"))
                        return "Land Mine";
                    if (entity.name.Contains("spikes.floor.prefab"))
                        return "Wooden Floor Spikes";
                }

                return "No Attacker";
            }

            public AttackerType TryGetType()
            {
                if (entity == null)
                    return AttackerType.Invalid;
                if (entity.ToPlayer() != null)
                    return AttackerType.Player;
                if (entity is BaseHelicopter)// entity.name.Contains("patrolhelicopter.prefab") && !entity.name.Contains("gibs"))
                    return AttackerType.Helicopter;
                if (entity.name.Contains("animals/"))
                    return AttackerType.Animal;
                if (entity.name.Contains("barricades/") || entity.name.Contains("wall.external.high"))
                    return AttackerType.Structure;
                if (entity.name.Contains("beartrap.prefab") || entity.name.Contains("landmine.prefab") || entity.name.Contains("spikes.floor.prefab"))
                    return AttackerType.Trap;
                if (entity.name.Contains("autoturret_deployed.prefab"))
                    return AttackerType.Turret;

                return AttackerType.Invalid;
            }
        }

        class Victim
        {
            public string name = string.Empty;
            [JsonIgnore]
            public BaseCombatEntity entity;
            public VictimType type = VictimType.Invalid;

            public string TryGetName()
            {
                if (type == VictimType.Player)
                    return entity.ToPlayer().displayName;
                if (type == VictimType.Helicopter)
                    return "Patrol Helicopter";
                if (type == VictimType.Animal)
                {
                    if (entity.name.Contains("boar"))
                        return "Boar";
                    if (entity.name.Contains("horse"))
                        return "Horse";
                    if (entity.name.Contains("wolf"))
                        return "Wolf";
                    if (entity.name.Contains("stag"))
                        return "Stag";
                    if (entity.name.Contains("chicken"))
                        return "Chicken";
                    if (entity.name.Contains("bear"))
                        return "Bear";
                }

                return "No Victim";
            }

            public VictimType TryGetType()
            {
                if (entity == null)
                    return VictimType.Invalid;
                if (entity.ToPlayer() != null)
                    return VictimType.Player;
                if (entity.name.Contains("patrolhelicopter.prefab") && entity.name.Contains("gibs"))
                    return VictimType.Helicopter;
                if ((bool)entity?.name?.Contains("animals/"))
                    return VictimType.Animal;

                return VictimType.Invalid;
            }
        }

        class DeathData
        {
            public Victim victim = new Victim();
            public Attacker attacker = new Attacker();
            public DeathReason reason = DeathReason.Unknown;
            public string damageType = string.Empty;
            public string weapon = string.Empty;
            public List<string> attachments = new List<string>();
            public string bodypart = string.Empty;
            internal float _distance = -1f;

            public float distance
            {
                get
                {
                    try
                    {
                        if (_distance != -1)
                            return _distance;

                        foreach (string death in dn.selfInflictedDeaths)
                        {
                            if (reason == GetDeathReason(death))
                                attacker.entity = victim.entity;
                        }

                        return victim.entity.Distance(attacker.entity.transform.position);
                    }
                    catch(Exception)
                    {
                        return 0f;
                    }
                }
            }

            public DeathReason TryGetReason()
            {
                if (victim.type == VictimType.Helicopter)
                    return DeathReason.HelicopterDeath;
                else if (attacker.type == AttackerType.Helicopter)
                    return DeathReason.Helicopter;
                else if (attacker.type == AttackerType.Turret)
                    return DeathReason.Turret;
                else if (attacker.type == AttackerType.Trap)
                    return DeathReason.Trap;
                else if (attacker.type == AttackerType.Structure)
                    return DeathReason.Structure;
                else if (attacker.type == AttackerType.Animal)
                    return DeathReason.Animal;
                else if (victim.type == VictimType.Animal)
                    return DeathReason.AnimalDeath;
                else if (weapon == "F1 Grenade" || weapon == "Survey Charge")
                    return DeathReason.Explosion;
                else if (weapon == "Flamethrower")
                    return DeathReason.Flamethrower;
                else if (victim.type == VictimType.Player)
                    return GetDeathReason(damageType);

                return DeathReason.Unknown;
            }

            public DeathReason GetDeathReason(string damage)
            {
                List<DeathReason> Reason = (from DeathReason current in Enum.GetValues(typeof(DeathReason)) where current.ToString() == damage select current).ToList();

                if (Reason.Count == 0)
                    return DeathReason.Unknown;

                return Reason[0];
            }

            [JsonIgnore]
            internal string JSON
            {
                get
                {
                    return JsonConvert.SerializeObject(this, Formatting.Indented);
                }
            }
            
            internal static DeathData Get(object obj)
            {
                JObject jobj = (JObject) obj;
                DeathData data = new DeathData();

                data.bodypart = jobj["bodypart"].ToString();
                data.weapon = jobj["weapon"].ToString();
                data.attachments = (from attachment in jobj["attachments"] select attachment.ToString()).ToList();
                data._distance = Convert.ToSingle(jobj["distance"]);

                /// Victim
                data.victim.name = jobj["victim"]["name"].ToString();

                List<VictimType> victypes = (from VictimType current in Enum.GetValues(typeof(VictimType)) where current.GetHashCode().ToString() == jobj["victim"]["type"].ToString() select current).ToList();

                if (victypes.Count != 0)
                    data.victim.type = victypes[0];

                /// Attacker
                data.attacker.name = jobj["attacker"]["name"].ToString();

                List<AttackerType> attackertypes = (from AttackerType current in Enum.GetValues(typeof(AttackerType)) where current.GetHashCode().ToString() == jobj["attacker"]["type"].ToString() select current).ToList();

                if (attackertypes.Count != 0)
                    data.attacker.type = attackertypes[0];
                
                /// Reason
                List<DeathReason> reasons = (from DeathReason current in Enum.GetValues(typeof(DeathReason)) where current.GetHashCode().ToString() == jobj["reason"].ToString() select current).ToList();
                if (reasons.Count != 0)
                    data.reason = reasons[0];

                return data;
            }
        }

        #endregion
        
        #region Enums / Types

        enum VictimType
        {
            Player,
            Helicopter,
            Animal,
            Invalid
        }

        enum AttackerType
        {
            Player,
            Helicopter,
            Animal,
            Turret,
            Structure,
            Trap,
            Self,
            Invalid
        }

        enum DeathReason
        {
            Turret,
            Helicopter,
            HelicopterDeath,
            Structure,
            Trap,
            Animal,
            AnimalDeath,
            Generic,
            Hunger,
            Thirst,
            Cold,
            Drowned,
            Heat,
            Bleeding,
            Poison,
            Suicide,
            Bullet,
            Arrow,
            Flamethrower,
            Slash,
            Blunt,
            Fall,
            Radiation,
            Stab,
            Explosion,
            Unknown
        }

        #endregion

        #region Player Settings

        List<string> playerSettingFields
        {
            get
            {
                return (from field in typeof(PlayerSettings).GetFields() select field.Name).ToList();
            }
        }

        List<string> GetSettingValues(BasePlayer player) => (from field in typeof(PlayerSettings).GetFields() select $"{field.Name} : {field.GetValue(playerSettings[player.userID]).ToString().ToLower()}").ToList();

        void SetSettingField<T>(BasePlayer player, string field, T value)
        {
            foreach(var curr in typeof(PlayerSettings).GetFields())
            {
                if (curr.Name == field)
                    curr.SetValue(playerSettings[player.userID], value);
            }
        }

        #endregion

        #region General Plugin Hooks

        void Loaded()
        {
#if !RUST
            throw new NotSupportedException("This plugin or the version of this plugin does not support this game!");
#endif

            dn = this;

            if (killReproducing)
                RegisterPerm("reproduce");

            RegisterPerm("customize");
            RegisterPerm("see");

            LoadConfig();
            LoadData();
            LoadMessages();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (!playerSettings.ContainsKey(player.userID))
                {
                    playerSettings.Add(player.userID, new PlayerSettings(this));

                    SaveData();
                }

            PopupNotifications = (Plugin)plugins.Find("PopupNotifications");

            if (PopupNotifications == null && UsePopupNotifications)
                PrintWarning("You have set 'Use Popup Notifications' to true, but the Popup Notifications plugin is not installed. Popups will not work without it. Get it here: http://oxidemod.org/plugins/1252/");
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating new config file...");
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (!playerSettings.ContainsKey(player.userID))
            {
                playerSettings.Add(player.userID, new PlayerSettings(this));
                SaveData();
            }
        }

        void OnPluginLoaded(object plugin)
        {
            if (plugin is Plugin && ((Plugin)plugin).Title == "Popup Notifications")
                PopupNotifications = (Plugin)plugin;
        }

        #endregion

        #region Loading

        void LoadData()
        {
            //canRead = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>("DeathNotes");

            playerSettings = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerSettings>>("DeathNotes/PlayerSettings");

            if (killReproducing)
                reproduceableKills = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>("DeathNotes/KillReproducing");
        }

        void SaveData()
        {
            //Interface.Oxide.DataFileSystem.WriteObject("DeathNotes", canRead);

            Interface.Oxide.DataFileSystem.WriteObject("DeathNotes/PlayerSettings", playerSettings);

            if (killReproducing)
                Interface.Oxide.DataFileSystem.WriteObject("DeathNotes_KillReproducing", reproduceableKills);
        }

        void LoadConfig()
        {
            SetConfig("Settings", "Chat Icon (SteamID)", "76561198077847390");

            SetConfig("Settings", "Message Radius Enabled", false);
            SetConfig("Settings", "Message Radius", 300f);

            SetConfig("Settings", "Log to File", false);
            SetConfig("Settings", "Write to Console", true);
            SetConfig("Settings", "Write to Chat", true);
            SetConfig("Settings", "Use Popup Notifications", false);
            SetConfig("Settings", "Use Simple UI", false);
            SetConfig("Settings", "Strip Colors from Simple UI", false);
            SetConfig("Settings", "Simple UI - Font Size", 20);
            SetConfig("Settings", "Simple UI - Top", 0.1f);
            SetConfig("Settings", "Simple UI - Left", 0.1f);
            SetConfig("Settings", "Simple UI - Max Width", 0.8f);
            SetConfig("Settings", "Simple UI - Max Height", 0.05f);

            SetConfig("Settings", "Simple UI Hide Timer", 5f);

            SetConfig("Settings", "Needs Permission", false);

            SetConfig("Settings", "Title", "Death Notes");
            SetConfig("Settings", "Formatting", "[{Title}]: {Message}");
            SetConfig("Settings", "Console Formatting", "{Message}");

            SetConfig("Settings", "Attachments Split", " | ");
            SetConfig("Settings", "Attachments Formatting", " ({attachments})");

            SetConfig("Settings", "Title Color", "#80D000");
            SetConfig("Settings", "Victim Color", "#C4FF00");
            SetConfig("Settings", "Attacker Color", "#C4FF00");
            SetConfig("Settings", "Weapon Color", "#C4FF00");
            SetConfig("Settings", "Attachments Color", "#C4FF00");
            SetConfig("Settings", "Distance Color", "#C4FF00");
            SetConfig("Settings", "Bodypart Color", "#C4FF00");
            SetConfig("Settings", "Message Color", "#696969");
            SetConfig("Settings", "Health Color", "#C4FF00");

            SetConfig("Names", new Dictionary<string, object> { });
            SetConfig("Bodyparts", new Dictionary<string, object> { });
            SetConfig("Weapons", new Dictionary<string, object> { });
            SetConfig("Attachments", new Dictionary<string, object> { });

            SetConfig("Messages", "Bleeding", new List<object> { "{victim} bled out." });
            SetConfig("Messages", "Blunt", new List<object> { "{attacker} used a {weapon} to knock {victim} out." });
            SetConfig("Messages", "Bullet", new List<object> { "{victim} was shot in the {bodypart} by {attacker} with a {weapon}{attachments} from {distance}m." });
            SetConfig("Messages", "Flamethrower", new List<object> { "{victim} was burned to ashes by {attacker} using a {weapon}." });
            SetConfig("Messages", "Cold", new List<object> { "{victim} became an iceblock." });
            SetConfig("Messages", "Drowned", new List<object> { "{victim} tried to swim." });
            SetConfig("Messages", "Explosion", new List<object> { "{victim} was shredded by {attacker}'s {weapon}" });
            SetConfig("Messages", "Fall", new List<object> { "{victim} did a header into the ground." });
            SetConfig("Messages", "Generic", new List<object> { "The death took {victim} with him." });
            SetConfig("Messages", "Heat", new List<object> { "{victim} burned to ashes." });
            SetConfig("Messages", "Helicopter", new List<object> { "{victim} was shot to pieces by a {attacker}." });
            SetConfig("Messages", "HelicopterDeath", new List<object> { "The {victim} was taken down." });
            SetConfig("Messages", "Animal", new List<object> { "A {attacker} followed {victim} until it finally caught him." });
            SetConfig("Messages", "AnimalDeath", new List<object> { "{attacker} killed a {victim} with a {weapon}{attachments} from {distance}m." });
            SetConfig("Messages", "Hunger", new List<object> { "{victim} forgot to eat." });
            SetConfig("Messages", "Poison", new List<object> { "{victim} died after being poisoned." });
            SetConfig("Messages", "Radiation", new List<object> { "{victim} became a bit too radioactive." });
            SetConfig("Messages", "Slash", new List<object> { "{attacker} slashed {victim} in half." });
            SetConfig("Messages", "Stab", new List<object> { "{victim} was stabbed to death by {attacker} using a {weapon}." });
            SetConfig("Messages", "Structure", new List<object> { "A {attacker} impaled {victim}." });
            SetConfig("Messages", "Suicide", new List<object> { "{victim} had enough of life." });
            SetConfig("Messages", "Thirst", new List<object> { "{victim} dried internally." });
            SetConfig("Messages", "Trap", new List<object> { "{victim} ran into a {attacker}" });
            SetConfig("Messages", "Turret", new List<object> { "A {attacker} defended its home against {victim}." });
            SetConfig("Messages", "Unknown", new List<object> { "{victim} died. Nobody knows why, it just happened." });
            
            SetConfig("Messages", "Blunt Sleeping", new List<object> { "{attacker} used a {weapon} to turn {victim}'s dream into a nightmare." });
            SetConfig("Messages", "Bullet Sleeping", new List<object> { "Sleeping {victim} was shot in the {bodypart} by {attacker} with a {weapon}{attachments} from {distance}m." });
            SetConfig("Messages", "Flamethrower Sleeping", new List<object> { "{victim} was burned to ashes by sleeping by {attacker} using a {weapon}." });
            SetConfig("Messages", "Explosion Sleeping", new List<object> { "{victim} was shredded by {attacker}'s {weapon} while sleeping." });
            SetConfig("Messages", "Generic Sleeping", new List<object> { "The death took sleeping {victim} with him." });
            SetConfig("Messages", "Helicopter Sleeping", new List<object> { "{victim} was sleeping when he was shot to pieces by a {attacker}." });
            SetConfig("Messages", "Animal Sleeping", new List<object> { "{victim} was killed by a {attacker} while having a sleep." });
            SetConfig("Messages", "Slash Sleeping", new List<object> { "{attacker} slashed sleeping {victim} in half." });
            SetConfig("Messages", "Stab Sleeping", new List<object> { "{victim} was stabbed to death by {attacker} using a {weapon} before he could even awake." });
            SetConfig("Messages", "Unknown Sleeping", new List<object> { "{victim} was sleeping when he died. Nobody knows why, it just happened." });

            SaveConfig();

            //  Cache Config Variables
            ChatIcon = GetConfig("76561198077847390", "Settings", "Chat Icon (SteamID)");

            MessageRadiusEnabled = GetConfig(false, "Settings", "Message Radius Enabled");
            MessageRadius = GetConfig(300f, "Settings", "Message Radius");

            LogToFile = GetConfig(false, "Settings", "Log to File");
            WriteToConsole = GetConfig(true, "Settings", "Write to Console");
            WriteToChat = GetConfig(true, "Settings", "Write to Chat");
            UsePopupNotifications = GetConfig(false, "Settings", "Use Popup Notifications");
            UseSimpleUI = GetConfig(false, "Settings", "Use Simple UI");
            SimpleUI_StripColors = GetConfig(false, "Settings", "Strip Colors from Simple UI");
            SimpleUI_FontSize = GetConfig(20, "Settings", "Simple UI - Font Size");
            SimpleUI_Top = GetConfig(0.1f, "Settings", "Simple UI - Top");
            SimpleUI_Left = GetConfig(0.1f, "Settings", "Simple UI - Left");
            SimpleUI_MaxWidth = GetConfig(0.8f, "Settings", "Simple UI - Max Width");
            SimpleUI_MaxHeight = GetConfig(0.05f, "Settings", "Simple UI - Max Height");

            SimpleUI_HideTimer = GetConfig(5f, "Settings", "Simple UI Hide Timer");

            NeedsPermission = GetConfig(false, "Settings", "Needs Permission");

            ChatTitle = GetConfig("Death Notes", "Settings", "Title");
            ChatFormatting = GetConfig("[{Title}]: {Message}", "Settings", "Formatting");
            ConsoleFormatting = GetConfig("{Message}", "Settings", "Console Formatting");

            AttachmentSplit = GetConfig(" | ", "Settings", "Attachments Split");
            AttachmentFormatting = GetConfig(" ({attachments})", "Settings", "Attachments Formatting");

            TitleColor = GetConfig("#80D000", "Settings", "Title Color");
            VictimColor = GetConfig("#C4FF00", "Settings", "Victim Color");
            AttackerColor = GetConfig("#C4FF00", "Settings", "Attacker Color");
            WeaponColor = GetConfig("#C4FF00", "Settings", "Weapon Color");
            AttachmentColor = GetConfig("#C4FF00", "Settings", "Attachments Color");
            DistanceColor = GetConfig("#C4FF00", "Settings", "Distance Color");
            BodypartColor = GetConfig("#C4FF00", "Settings", "Bodypart Color");
            MessageColor = GetConfig("#696969", "Settings", "Message Color");

            Names = GetConfig(new Dictionary<string, object> { }, "Names");
            Bodyparts = GetConfig(new Dictionary<string, object> { }, "Bodyparts");
            Weapons = GetConfig(new Dictionary<string, object> { }, "Weapons");
            Attachments = GetConfig(new Dictionary<string, object> { }, "Attachments");

            Messages = GetConfig(new Dictionary<string, object>
            {
                //  Normal
                { "Bleeding", new List<object> { "{victim} bled out." }},
                { "Blunt", new List<object> { "{attacker} used a {weapon} to knock {victim} out." }},
                { "Bullet", new List<object> { "{victim} was shot in the {bodypart} by {attacker} with a {weapon}{attachments} from {distance}m." }},
                { "Flamethrower", new List<object> { "{victim} was burned to ashes by {attacker} using a {weapon}." }},
                { "Cold", new List<object> { "{victim} became an iceblock." }},
                { "Drowned", new List<object> { "{victim} tried to swim." }},
                { "Explosion", new List<object> { "{victim} was shredded by {attacker}'s {weapon}" }},
                { "Fall", new List<object> { "{victim} did a header into the ground." }},
                { "Generic", new List<object> { "The death took {victim} with him." }},
                { "Heat", new List<object> { "{victim} burned to ashes." }},
                { "Helicopter", new List<object> { "{victim} was shot to pieces by a {attacker}." }},
                { "HelicopterDeath", new List<object> { "The {victim} was taken down." }},
                { "Animal", new List<object> { "A {attacker} followed {victim} until it finally caught him." }},
                { "AnimalDeath", new List<object> { "{attacker} killed a {victim} with a {weapon}{attachments} from {distance}m." }},
                { "Hunger", new List<object> { "{victim} forgot to eat." }},
                { "Poison", new List<object> { "{victim} died after being poisoned." }},
                { "Radiation", new List<object> { "{victim} became a bit too radioactive." }},
                { "Slash", new List<object> { "{attacker} slashed {victim} in half." }},
                { "Stab", new List<object> { "{victim} was stabbed to death by {attacker} using a {weapon}." }},
                { "Structure", new List<object> { "A {attacker} impaled {victim}." }},
                { "Suicide", new List<object> { "{victim} had enough of life." }},
                { "Thirst", new List<object> { "{victim} dried internally." }},
                { "Trap", new List<object> { "{victim} ran into a {attacker}" }},
                { "Turret", new List<object> { "A {attacker} defended its home against {victim}." }},
                { "Unknown", new List<object> { "{victim} died. Nobody knows why, it just happened." }},

                //  Sleeping
                { "Blunt Sleeping", new List<object> { "{attacker} used a {weapon} to turn {victim}'s dream into a nightmare." }},
                { "Bullet Sleeping", new List<object> { "Sleeping {victim} was shot in the {bodypart} by {attacker} with a {weapon}{attachments} from {distance}m." }},
                { "Flamethrower Sleeping", new List<object> { "{victim} was burned to ashes while sleeping by {attacker} using a {weapon}." }},
                { "Explosion Sleeping", new List<object> { "{victim} was shredded by {attacker}'s {weapon} while sleeping." }},
                { "Generic Sleeping", new List<object> { "The death took sleeping {victim} with him." }},
                { "Helicopter Sleeping", new List<object> { "{victim} was sleeping when he was shot to pieces by a {attacker}." }},
                { "Animal Sleeping", new List<object> { "{victim} was killed by a {attacker} while having a sleep." }},
                { "Slash Sleeping", new List<object> { "{attacker} slashed sleeping {victim} in half." }},
                { "Stab Sleeping", new List<object> { "{victim} was stabbed to death by {attacker} using a {weapon} before he could even awake." }},
                { "Unknown Sleeping", new List<object> { "{victim} was sleeping when he died. Nobody knows why, it just happened." }}
            }, "Messages").ToDictionary(l => l.Key, l => ((List<object>)l.Value).ConvertAll(m => m.ToString()));
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Hidden", "You do no longer see death messages."},
                {"Unhidden", "You will now see death messages."},
                {"Field Not Found", "The field could not be found!"},
                {"True Or False", "{arg} must be 'true' or 'false'!"},
                {"Field Set", "Field '{field}' set to '{value}'"}
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("deaths")]
        void cmdDeaths(BasePlayer player, string cmd, string[] args)
        {
            if(!HasPerm(player.userID, "customize"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.userID));
                return;
            }

            if (args.Length == 0)
            {
                SendChatMessage(player, "/deaths set <field> <value> - set a value");
                SendChatMessage(player, "Fields", Environment.NewLine + ListToString(GetSettingValues(player), 0, Environment.NewLine));

                return;
            }

            switch(args[0].ToLower())
            {
                case "set":
                    if(args.Length != 3)
                    {
                        SendChatMessage(player, "Syntax: /deaths set <field> <value>");
                        return;
                    }

                    if(!playerSettingFields.Contains(args[1].ToLower()))
                    {
                        SendChatMessage(player, GetMsg("Field Not Found", player.userID));
                        return;
                    }
                    
                    bool value = false;

                    try
                    {
                        value = Convert.ToBoolean(args[2]);
                    }
                    catch(FormatException)
                    {
                        SendChatMessage(player, GetMsg("True Or False", player.userID).Replace("{arg}", "<value>"));
                        return;
                    }

                    SetSettingField(player, args[1].ToLower(), value);

                    SendChatMessage(player, GetMsg("Field Set", player.userID).Replace("{value}", value.ToString().ToLower()).Replace("{field}", args[1].ToLower()));

                    SaveData();

                    break;

                default:
                    SendChatMessage(player, "/deaths set <field> <value> - set a value");
                    SendChatMessage(player, "Fields", Environment.NewLine + ListToString(GetSettingValues(player), 0, Environment.NewLine));
                    break;
            }
        }

        [ChatCommand("deathnotes")]
        void cmdGetInfo(BasePlayer player) => GetInfo(player);

        [ConsoleCommand("reproducekill")]
        void ccmdReproduceKill(ConsoleSystem.Arg arg)
        {
            bool hasPerm = false;

            if (arg?.connection == null)
                hasPerm = true;
            else
            {
                if((BasePlayer)arg.connection.player != null)
                {
                    if (HasPerm(arg.connection.userid, "reproduce"))
                        hasPerm = true;
                }
            }
            
            if (hasPerm)
            {
                if (arg.Args == null || arg.Args.Length != 1)
                {
                    arg.ReplyWith("Syntax: reproducekill <datetime>");
                    return;
                }
                
                if(reproduceableKills.ContainsKey(arg.Args[0]))
                {
                    DeathData data = DeathData.Get(JsonConvert.DeserializeObject(reproduceableKills[arg.Args[0]]));
                    PrintWarning("Reproduced Kill: " + Environment.NewLine + data.JSON);

                    if (data == null)
                        return;

                    NoticeDeath(data, true);
                    arg.ReplyWith("Death reproduced!");
                }
                else
                    arg.ReplyWith("No saved kill at that time found!");
            }
        }

        #endregion

        #region DeathNotes Information

        void GetInfo(BasePlayer player)
        {
            webrequest.EnqueueGet("http://oxidemod.org/plugins/819/", (code, response) => {
                if(code != 200)
                {
                    PrintWarning("Failed to get information!");
                    return;
                }

                string version_published = "0.0.0";
                string version_installed = this.Version.ToString();

                Match version = new Regex(@"<h3>Version (\d{1,2}(\.\d{1,2})+?)<\/h3>").Match(response);
                if(version.Success)
                {
                    version_published = version.Groups[1].ToString();
                }

                SendChatMessage(player, $"<size=25><color=#C4FF00>DeathNotes</color></size> <size=20><color=#696969>by LaserHydra</color>{Environment.NewLine}<color=#696969>Latest <color=#C4FF00>{version_published}</color>{Environment.NewLine}Installed <color=#C4FF00>{version_installed}</color></color></size>");
            }, this);
        }

        #endregion

        #region Death Related

        HitInfo TryGetLastWounded(ulong uid, HitInfo info)
        {
            if (LastWounded.ContainsKey(uid))
            {
                HitInfo output = LastWounded[uid];
                LastWounded.Remove(uid);
                return output;
            }

            return info;
        }

        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            if(victim?.ToPlayer() != null && info?.Initiator?.ToPlayer() != null)
            {
                NextTick(() => 
                {
                    if (victim.ToPlayer().IsWounded())
                        LastWounded[victim.ToPlayer().userID] = info;
                });
            }
        }

        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            if (victim == null)
                return;

            if(victim.ToPlayer() != null)
            {
                if (victim.ToPlayer().IsWounded())
                    info = TryGetLastWounded(victim.ToPlayer().userID, info);
            }

            if (info?.Initiator?.ToPlayer() == null && (victim?.name?.Contains("autospawn") ?? false))
                return;

            DeathData data = new DeathData();
            data.victim.entity = victim;
            data.victim.type = data.victim.TryGetType();

            if (data.victim.type == VictimType.Invalid)
                return;

            data.victim.name = data.victim.TryGetName();

            if (info?.Initiator != null)
            {
                data.attacker.entity = info.Initiator as BaseCombatEntity;
                data.attacker.healthLeft = info.Initiator.Health();
            }
            else
                data.attacker.entity = victim.lastAttacker as BaseCombatEntity;

            data.attacker.type = data.attacker.TryGetType();
            data.attacker.name = StripTags(data.attacker.TryGetName());
            data.weapon = info?.Weapon?.GetItem()?.info?.displayName?.english ?? FormatThrownWeapon(info?.WeaponPrefab?.name ?? "No Weapon");
            data.attachments = GetAttachments(info);
            data.damageType = FirstUpper(victim.lastDamage.ToString());

            if(data.weapon == "Heli Rocket")
            {
                data.attacker.name = "Patrol Helicopter";
                data.reason = DeathReason.Helicopter;
            }

            if (info?.HitBone != null)
                data.bodypart = FirstUpper(GetBoneName(victim, info.HitBone) ?? string.Empty);
            else
                data.bodypart = FirstUpper("Body") ?? string.Empty;

            data.reason = data.TryGetReason();

            if (!(bool)(plugins.CallHook("OnDeathNotice", JObject.FromObject(data)) ?? true))
                return;

            NoticeDeath(data);
        }

        void NoticeDeath(DeathData data, bool reproduced = false)
        {
            DeathData newData = UpdateData(data);

            if (string.IsNullOrEmpty(GetDeathMessage(newData, false)))
                return;

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (InRadius(player, data.attacker.entity))
                {
                    if (CanSee(player, "chat"))
                        SendChatMessage(player, GetDeathMessage(newData, false), null, ChatIcon);

                    if (CanSee(player, "ui"))
                        UIMessage(player, SimpleUI_StripColors ? StripTags(GetDeathMessage(newData, true)) : GetDeathMessage(newData, true));
                }
            }

            if (WriteToConsole)
                Puts(StripTags(GetDeathMessage(newData, true)));

            if (LogToFile)
                ConVar.Server.Log("oxide/logs/Kills.txt", StripTags(GetDeathMessage(newData, true)));

            if (UsePopupNotifications)
                PopupMessage(GetDeathMessage(newData, false));

            if (debug)
            {
                PrintWarning("DATA: " + Environment.NewLine + data.JSON);
                PrintWarning("UPDATED DATA: " + Environment.NewLine + newData.JSON);
            }

            if (killReproducing && !reproduced)
            {
                reproduceableKills[DateTime.Now.ToString()] = data.JSON.Replace(Environment.NewLine, "");
                SaveData();
            }
        }

        #endregion

        #region Formatting

        string FormatThrownWeapon(string unformatted)
        {
            if (unformatted == string.Empty)
                return string.Empty;

            string formatted = FirstUpper(unformatted.Split('/').Last().Replace(".prefab", "").Replace(".entity", "").Replace(".weapon", "").Replace(".deployed", "").Replace("_", " ").Replace(".", ""));

            if (formatted == "Stonehatchet")
                formatted = "Stone Hatchet";
            else if (formatted == "Knife Bone")
                formatted = "Bone Knife";
            else if (formatted == "Spear Wooden")
                formatted = "Wooden Spear";
            else if (formatted == "Spear Stone")
                formatted = "Stone Spear";
            else if (formatted == "Icepick Salvaged")
                formatted = "Salvaged Icepick";
            else if (formatted == "Axe Salvaged")
                formatted = "Salvaged Axe";
            else if (formatted == "Hammer Salvaged")
                formatted = "Salvaged Hammer";
            else if (formatted == "Grenadef1")
                formatted = "F1 Grenade";
            else if (formatted == "Grenadebeancan")
                formatted = "Beancan Grenade";
            else if (formatted == "Explosivetimed")
                formatted = "Timed Explosive";

            return formatted;
        }

        string StripTags(string original)
        {
            foreach (string tag in tags)
                original = original.Replace(tag, "");

            foreach (Regex regexTag in regexTags)
                original = regexTag.Replace(original, "");

            return original;
        }

        string FirstUpper(string original)
        {
            if (original == string.Empty)
                return string.Empty;

            List<string> output = new List<string>();
            foreach (string word in original.Split(' '))
                output.Add(word.Substring(0, 1).ToUpper() + word.Substring(1, word.Length - 1));

            return ListToString(output, 0, " ");
        }

        #endregion

        #region Death Variables Methods

        List<string> GetMessages(string reason) => Messages.ContainsKey(reason) ? Messages[reason] : new List<string>();
        
        List<string> GetAttachments(HitInfo info)
        {
            List<string> attachments = new List<string>();

            if (info?.Weapon?.GetItem()?.contents?.itemList != null)
            {
                foreach (var content in info.Weapon.GetItem().contents.itemList)
                {
                    attachments.Add(content?.info?.displayName?.english);
                }
            }

            return attachments;
        }

        string GetBoneName(BaseCombatEntity entity, uint boneId) => entity?.skeletonProperties?.FindBone(boneId)?.name?.english ?? "Body";

        bool InRadius(BasePlayer player, BaseCombatEntity attacker)
        {
            if (MessageRadiusEnabled)
            {
                try
                {
                    if (player.Distance(attacker) <= MessageRadius)
                        return true;
                    else
                        return false;
                }
                catch(Exception)
                {
                    return false;
                }
            }

            return true;
        }

        string GetDeathMessage(DeathData data, bool console)
        {
            string message = string.Empty;
            string reason = string.Empty;
            List<string> messages = new List<string>();

            if (data.victim.type == VictimType.Player && data.victim.entity?.ToPlayer() != null && data.victim.entity.ToPlayer().IsSleeping())
            {
                if(SleepingDeaths.Contains(data.reason))
                {
                    reason = data.reason + " Sleeping";
                }
                else
                    reason = data.reason.ToString();
            }
            else
                reason = data.reason.ToString();

            try
            {
                messages = GetMessages(reason);
            }
            catch (InvalidCastException)
            {
            }

            if (messages.Count == 0)
                return message;

            string attachmentsString = data.attachments.Count == 0 ? string.Empty : AttachmentFormatting.Replace("{attachments}", ListToString(data.attachments, 0, AttachmentSplit));

            if (console)
                message = ConsoleFormatting.Replace("{Title}", $"<color={TitleColor}>{ChatTitle}</color>").Replace("{Message}", $"<color={MessageColor}>{messages.GetRandom()}</color>");
            else
                message = ChatFormatting.Replace("{Title}", $"<color={TitleColor}>{ChatTitle}</color>").Replace("{Message}", $"<color={MessageColor}>{messages.GetRandom()}</color>");

            message = message.Replace("{health}", $"<color={HealthColor}>{Math.Round(data.attacker.healthLeft, 1)}</color>");
            message = message.Replace("{attacker}", $"<color={AttackerColor}>{data.attacker.name}</color>");
            message = message.Replace("{victim}", $"<color={VictimColor}>{data.victim.name}</color>");
            message = message.Replace("{distance}", $"<color={DistanceColor}>{Math.Round(data.distance, 2)}</color>");
            message = message.Replace("{weapon}", $"<color={WeaponColor}>{data.weapon}</color>");
            message = message.Replace("{bodypart}", $"<color={BodypartColor}>{data.bodypart}</color>");
            message = message.Replace("{attachments}", $"<color={AttachmentColor}>{attachmentsString}</color>");

            return message;
        }

        DeathData UpdateData(DeathData data)
        {
            bool configUpdated = false;

            if (data.victim.type != VictimType.Player)
            {
                if (Config.Get("Names", data.victim.name) == null)
                {
                    SetConfig("Names", data.victim.name, data.victim.name);
                    configUpdated = true;
                }
                else
                    data.victim.name = GetConfig(data.victim.name, "Names", data.victim.name);
            }

            if (data.attacker.type != AttackerType.Player)
            {
                if (Config.Get("Names", data.attacker.name) == null)
                {
                    SetConfig("Names", data.attacker.name, data.attacker.name);
                    configUpdated = true;
                }
                else
                    data.attacker.name = GetConfig(data.attacker.name, "Names", data.attacker.name);
            }

            if (Config.Get("Bodyparts", data.bodypart) == null)
            {
                SetConfig("Bodyparts", data.bodypart, data.bodypart);
                configUpdated = true;
            }
            else
                data.bodypart = GetConfig(data.bodypart, "Bodyparts", data.bodypart);

            if (Config.Get("Weapons", data.weapon) == null)
            {
                SetConfig("Weapons", data.weapon, data.weapon);
                configUpdated = true;
            }
            else
                data.weapon = GetConfig(data.weapon, "Weapons", data.weapon);

            string[] attachmentsCopy = new string[data.attachments.Count];
            data.attachments.CopyTo(attachmentsCopy);

            foreach (string attachment in attachmentsCopy)
            {
                if (Config.Get("Attachments", attachment) == null)
                {
                    SetConfig("Attachments", attachment, attachment);
                    configUpdated = true;
                }
                else
                {
                    data.attachments.Remove(attachment);
                    data.attachments.Add(GetConfig(attachment, "Attachments", attachment));
                }
            }

            if (configUpdated)
                SaveConfig();

            return data;
        }

        bool CanSee(BasePlayer player, string type)
        {
            if (!NeedsPermission)
            {
                if (type == "ui")
                {
                    if (HasPerm(player.userID, "customize"))
                        return playerSettings.ContainsKey(player.userID) ? playerSettings[player.userID].ui : true;
                    else
                        return UseSimpleUI;
                }

                if (HasPerm(player.userID, "customize"))
                    return playerSettings.ContainsKey(player.userID) ? playerSettings[player.userID].chat : true;
                else
                    return WriteToChat;
            }
            else
            {
                if(HasPerm(player.userID, "see"))
                {
                    if (type == "ui")
                    {
                        if (HasPerm(player.userID, "customize"))
                            return playerSettings.ContainsKey(player.userID) ? playerSettings[player.userID].ui : true;
                        else
                            return UseSimpleUI;
                    }

                    if (HasPerm(player.userID, "customize"))
                        return playerSettings.ContainsKey(player.userID) ? playerSettings[player.userID].chat : true;
                    else
                        return WriteToChat;
                }
            }

            return false;
        }

        #endregion

        #region Converting

        string ListToString(List<string> list, int first, string seperator) => string.Join(seperator, list.Skip(first).ToArray());

        #endregion

        #region Config and Message Handling

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID.ToString());
        }

        #endregion

        #region Permission Handling

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        bool HasPerm(object uid, params string[] permArray)
        {
            uid = uid.ToString();
            string perm = ListToString(permArray.ToList(), 0, ".");

            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        string PermissionPrefix
        {
            get
            {
                return this.Title.Replace(" ", "").ToLower();
            }
        }

        #endregion

        #region Messages

        void BroadcastChat(string prefix, string msg = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null, object uid = null) => rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg, null, uid?.ToString() ?? "0");

        void PopupMessage(string message) => PopupNotifications?.Call("CreatePopupNotification", message);

        void UIMessage(BasePlayer player, string message)
        {
            bool replaced = false;
            float fadeIn = 0.2f;

            Timer playerTimer;

            timers.TryGetValue(player, out playerTimer);

            if (playerTimer != null && !playerTimer.Destroyed)
            {
                playerTimer.Destroy();
                fadeIn = 0.1f;

                replaced = true;
            }

            UIObject ui = new UIObject();

            ui.AddText("DeathNotice_DropShadow", SimpleUI_Left + 0.001, SimpleUI_Top + 0.001, SimpleUI_MaxWidth, SimpleUI_MaxHeight, deathNoticeShadowColor, StripTags(message), SimpleUI_FontSize, "Hud.Under", 3, fadeIn, 0.2f);
            ui.AddText("DeathNotice", SimpleUI_Left, SimpleUI_Top, SimpleUI_MaxWidth, SimpleUI_MaxHeight, deathNoticeColor, message, SimpleUI_FontSize, "Hud.Under", 3, fadeIn, 0.2f);

            ui.Destroy(player);

            if(replaced)
            {
                timer.Once(0.1f, () =>
                {
                    ui.Draw(player);

                    timers[player] = timer.Once(SimpleUI_HideTimer, () => ui.Destroy(player));
                });
            }
            else
            {
                ui.Draw(player);

                timers[player] = timer.Once(SimpleUI_HideTimer, () => ui.Destroy(player));
            }
        }

        #endregion
    }
}
