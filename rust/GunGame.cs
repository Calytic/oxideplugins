// Requires: EventManager

using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using UnityEngine;

using Rust;

namespace Oxide.Plugins
{
    [Info("Gun Game", "k1lly0u", "0.3.51", ResourceId = 1485)]
    class GunGame : RustPlugin
    {
        [PluginReference]
        EventManager EventManager;

        private bool useThisEventGG;
        private bool GGStarted;

        private List<GunGamePlayer> GunGamePlayers = new List<GunGamePlayer>();
        private ConfigData configData;
        private Dictionary<string, ItemDefinition> _itemsDict;

        class ConfigData
        {
            public string EventName { get; set; }
            public string SpawnFile { get; set; }
            public string ZoneName { get; set; }
            public string ArmourType { get; set; }
            public float StartHealth { get; set; }
            public bool UseMachete { get; set; }
            public bool UseArmour { get; set; }
            public bool UseMeds { get; set; }
            public bool CloseEventAtStart { get; set; }
            public int RankLimit { get; set; }
            public int TokensPerKill { get; set; }
            public int TokensOnWin { get; set; }
            public Gear DowngradeWeapon { get; set; }
            public List<Gear> Meds { get; set; }
            public List<Gear> PlayerGear { get; set; }
            public Dictionary<int, RankItem> Weapons { get; set; }
        }


        class GunGamePlayer : MonoBehaviour
        {
            public BasePlayer player;
            public int kills;
            public int level;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;
                kills = 0;
                level = 1;
            }
        }

        internal class RankItem
        {
            public string name;
            public string shortname;
            public int skin;
            public string container;
            public int amount;
            public int ammo;
            public string ammoType;
            public string[] contents = new string[0];
        }
        class Gear
        {
            public string name;
            public string shortname;
            public int skin;
            public int amount;
            public string container;
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void OnServerInitialized()
        {
            useThisEventGG = false;
            GGStarted = false;
            if (EventManager == null)
            {
                Puts("Event plugin doesn't exist");
                return;
            }
            LoadVariables();
            RegisterGame();
        }
        void RegisterGame()
        {
            var success = EventManager.RegisterEventGame(configData.EventName);
            if (success == null)
            {
                Puts("Event plugin doesn't exist");
                return;
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Event GunGame: Creating a new config file");
            var config = new ConfigData
            {
                EventName = "GunGame",
                ZoneName = "GunGame",
                SpawnFile = "ggspawnfile",
                ArmourType = "metal.plate.torso",
                StartHealth = 100,
                UseMachete = true,
                UseArmour = true,
                UseMeds = true,
                CloseEventAtStart = true,
                RankLimit = 15,
                TokensPerKill = 1,
                TokensOnWin = 5,
                DowngradeWeapon = new Gear
                {
                    name = "Machete",
                    shortname = "machete",
                    amount = 1,
                    container = "belt",
                    skin = 0
                },
                Meds = new List<Gear>
                {
                    {
                        new Gear
                        {
                            name = "Medical Syringe",
                            shortname = "syringe.medical",
                            amount = 2,
                            container = "belt"
                        }
                    },
                    {
                        new Gear
                        {
                            name = "Bandage",
                            shortname = "bandage",
                            amount = 1,
                            container = "belt"
                        }
                    }
                },
                PlayerGear = new List<Gear>
                {
                    {
                        new Gear
                        {
                            name = "Boots",
                            shortname = "shoes.boots",
                            container = "wear",
                            skin = 0,
                            amount = 1
                        }
                    },
                    {
                        new Gear
                        {
                            name = "Hide Pants",
                            shortname = "attire.hide.pants",
                            container = "wear",
                            skin = 0,
                            amount = 1
                        }
                    },
                    {
                        new Gear
                        {
                            name = "Bone Armour Pants",
                            shortname = "bone.armor.pants",
                            container = "wear",
                            skin = 0,
                            amount = 1
                        }
                    },
                    {
                        new Gear
                        {
                            name = "Riot Helmet",
                            shortname = "riot.helmet",
                            container = "wear",
                            skin = 0,
                            amount = 1
                        }
                    }
                },
                Weapons = new Dictionary<int, RankItem>
                {
                    {
                        1, new RankItem
                        {
                            name = "AssaultRifle",
                            shortname = "rifle.ak",
                            container = "belt",
                            ammoType = "ammo.rifle",
                            ammo = 120,
                            amount = 1,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        2, new RankItem
                        {
                            name = "Thompson",
                            shortname = "smg.thompson",
                            container = "belt",
                            ammoType = "ammo.pistol",
                            amount = 1,
                            ammo = 120,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        3, new RankItem
                        {
                            name = "PumpShotgun",
                            shortname = "shotgun.pump",
                            container = "belt",
                            ammoType = "ammo.shotgun",
                            amount = 1,
                            ammo = 60,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        4, new RankItem
                        {
                            name = "SMG",
                            shortname = "smg.2",
                            container = "belt",
                            ammoType = "ammo.pistol",
                            amount = 1,
                            ammo = 120,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        5, new RankItem
                        {
                            name = "BoltAction",
                            shortname = "rifle.bolt",
                            container = "belt",
                            amount = 1,
                            ammoType = "ammo.rifle",
                            ammo = 120,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        6, new RankItem
                        {
                            name = "SemiAutoRifle",
                            shortname = "rifle.semiauto",
                            container = "belt",
                            ammoType = "ammo.rifle",
                            amount = 1,
                            ammo = 120,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        7, new RankItem
                        {
                            name = "SemiAutoPistol",
                            shortname = "pistol.semiauto",
                            container = "belt",
                            ammoType = "ammo.pistol",
                            amount = 1,
                            ammo = 120,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        8, new RankItem
                        {
                            name = "Revolver",
                            shortname = "pistol.revolver",
                            container = "belt",
                            amount = 1,
                            ammoType = "ammo.pistol",
                            ammo = 120,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        9, new RankItem
                        {
                            name = "WaterpipeShotgun",
                            shortname = "shotgun.waterpipe",
                            container = "belt",
                            amount = 1,
                            ammoType = "ammo.handmade.shell",
                            ammo = 40,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        10, new RankItem
                        {
                            name = "HuntingBow",
                            shortname = "bow.hunting",
                            container = "belt",
                            amount = 1,
                            ammoType = "arrow.hv",
                            ammo = 40
                        }
                    },
                    {
                        11, new RankItem
                        {
                            name = "EokaPistol",
                            shortname = "pistol.eoka",
                            container = "belt",
                            amount = 1,
                            ammoType = "ammo.handmade.shell",
                            ammo = 40
                        }
                    },
                    {
                        12, new RankItem
                        {
                            name = "StoneSpear",
                            shortname = "spear.stone",
                            container = "belt",
                            amount = 2
                        }
                    },
                    {
                        13, new RankItem
                        {
                            name = "SalvagedCleaver",
                            shortname = "salvaged.cleaver",
                            container = "belt",
                            amount = 2
                        }
                    },
                    {
                        14, new RankItem
                        {
                            name = "Mace",
                            shortname = "mace",
                            container = "belt",
                            amount = 2
                        }
                    },
                    {
                        15, new RankItem
                        {
                            name = "BoneClub",
                            shortname = "bone.club",
                            container = "belt",
                            amount = 2
                        }
                    },
                    {
                        16, new RankItem
                        {
                            name = "BoneKnife",
                            shortname = "knife.bone",
                            container = "belt",
                            amount = 1
                        }
                    },
                    {
                        17, new RankItem
                        {
                            name = "LongSword",
                            shortname = "longsword",
                            container = "belt",
                            amount = 1
                        }
                    },
                    {
                        18, new RankItem
                        {
                            name = "SalvagedSword",
                            shortname = "salvaged.sword",
                            container = "belt",
                            amount = 1
                        }
                    },
                    {
                        19, new RankItem
                        {
                            name = "SalvagedIcepick",
                            shortname = "icepick.salvaged",
                            container = "belt",
                            amount = 1
                        }
                    },
                    {
                        20, new RankItem
                        {
                            name = "SalvagedAxe",
                            shortname = "axe.salvaged",
                            container = "belt",
                            amount = 1
                        }
                    },
                    {
                        21, new RankItem
                        {
                            name = "Pickaxe",
                            shortname = "pickaxe",
                            container = "belt",
                            amount = 1
                        }
                    },
                    {
                        22, new RankItem
                        {
                            name = "Hatchet",
                            shortname = "hatchet",
                            container = "belt",
                            amount = 1
                        }
                    },
                    {
                        23, new RankItem
                        {
                            name = "Rock",
                            shortname = "rock",
                            container = "belt",
                            amount = 1
                        }
                    },
                    {
                        24, new RankItem
                        {
                            name = "Torch",
                            shortname = "torch",
                            container = "belt",
                            amount = 1
                        }
                    },
                    {
                        25, new RankItem
                        {
                            name = "Crossbow",
                            shortname = "crossbow",
                            container = "belt",
                            amount = 1,
                            ammoType = "arrow.hv",
                            ammo = 40,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        26, new RankItem
                        {
                            name = "M249",
                            shortname = "lmg.m249",
                            container = "belt",
                            amount = 1,
                            ammoType = "ammo.rifle",
                            ammo = 120,
                            contents = new [] {"weapon.mod.holosight"}
                        }
                    },
                    {
                        27, new RankItem
                        {
                            name = "TimedExplosive",
                            shortname = "explosive.timed",
                            container = "belt",
                            amount = 20
                        }
                    },
                    {
                        28, new RankItem
                        {
                            name = "SurveyCharge",
                            shortname = "surveycharge",
                            container = "belt",
                            amount = 20
                        }
                    },
                    {
                        29, new RankItem
                        {
                            name = "F1Grenade",
                            shortname = "grenade.f1",
                            container = "belt",
                            amount = 20
                        }
                    },
                    {
                        30, new RankItem
                        {
                            name = "RocketLauncher",
                            shortname = "rocket.launcher",
                            container = "belt",
                            amount = 1,
                            ammoType = "ammo.rocket.basic",
                            ammo = 20
                        }
                    }
                }
            };
            SaveConfig(config);
        }
        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        void Unload()
        {           
            foreach (var player in BasePlayer.activePlayerList) DestroyUI(player);               
            if (useThisEventGG && GGStarted)            
                EventManager.EndEvent();
            DestroyEvent();              
            
            var objects = UnityEngine.Object.FindObjectsOfType<GunGamePlayer>();
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
        }
        private void DestroyEvent()
        {
            GGStarted = false;
            foreach (var player in GunGamePlayers)            
                UnityEngine.Object.Destroy(player);            
            GunGamePlayers.Clear();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, object> EventZoneConfig;

        string EventMessageWon = "<color=orange>Gungame</color> : {0} WON THE GUNGAME";
        string EventMessageNoMorePlayers = "<color=orange>Gungame</color> : The Gun Game Arena has no more players, auto-closing.";
        string GGMessageKill = "<color=orange>Gungame</color> : {3} was killed by {0}, who is now rank {2} with {1} kill(s)";
        string EventMessageOpenBroadcast = "<color=orange>Gungame</color> : In GunGame, every player you kill will advance you 1 rank, each rank has a new weapon. But beware, if you are killed by a downgrade weapon you will lose a rank!";

        private void LoadVariables()
        {
            _itemsDict = ItemManager.itemList.ToDictionary(i => i.shortname);
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
        }
       
        class LeaderBoard
        {
            public string Name;
            public int Kills;
        }
        #region UI Scoreboard
        private List<GunGamePlayer> SortScores()
        {
            List<GunGamePlayer> sortedScores;

            if (EventManager.EventMode == EventManager.GameMode.Battlefield)
                sortedScores = GunGamePlayers.OrderByDescending(pair => pair.kills).ToList();
            else sortedScores = GunGamePlayers.OrderByDescending(pair => pair.level).ToList();               
            
            return sortedScores;
        }
        private string PlayerMsg(int key, GunGamePlayer player)
        {
            var score = player.level;
            if (EventManager.EventMode == EventManager.GameMode.Battlefield)
                score = player.kills;

            return $"|  <color=#FF8C00>{key}</color>.  <color=#FF8C00>{player.player.displayName}</color> <color=#939393>--</color> <color=#FF8C00>{score}</color>  |";
        }
        private CuiElementContainer CreateScoreboard(BasePlayer player)
        {
            DestroyUI(player);
            string panelName = "GGScoreBoard";
            var element = EventManager.UI.CreateElementContainer(panelName, "0.3 0.3 0.3 0.6", "0.1 0.95", "0.9 1", false);

            var scores = SortScores();
            var index = scores.FindIndex(a => a.player == player);

            var scoreMessage = PlayerMsg(index + 1, scores[index]);
            int amount = 3;
            for (int i = 0; i < amount; i++)
            {
                if (scores.Count >= i + 1)
                {
                    if (scores[i].player == player)
                    {
                        amount++;
                        continue;
                    }
                    scoreMessage = scoreMessage + PlayerMsg(i + 1, scores[i]);
                }
            }
            EventManager.UI.CreateLabel(ref element, panelName, "", scoreMessage, 18, "0 0", "1 1");
            return element;
        }
        private void RefreshSB()
        {
            foreach (var entry in GunGamePlayers)
            {
                DestroyUI(entry.player);
                AddUI(entry.player);
            }
        }
        private void AddUI(BasePlayer player) => CuiHelper.AddUi(player, CreateScoreboard(player));
        private void DestroyUI(BasePlayer player) => CuiHelper.DestroyUi(player, "GGScoreBoard");
        #endregion
        //////////////////////////////////////////////////////////////////////////////////////
        // Event Manager Hooks ///////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void OnSelectEventGamePost(string name)
        {
            if (configData.EventName == name)
            {
                useThisEventGG = true;
                if (!string.IsNullOrEmpty(configData.SpawnFile))
                    EventManager.SelectSpawnfile(configData.SpawnFile);                
            }
            else
                useThisEventGG = false;
        }
        void OnEventPlayerSpawn(BasePlayer player)
        {
            if (useThisEventGG && GGStarted)
            {
                player.health = configData.StartHealth;
                if (!player.GetComponent<GunGamePlayer>()) GunGamePlayers.Add(player.gameObject.AddComponent<GunGamePlayer>());
                stripGive(player);
                AddUI(player);
            }
        }
        object OnSelectSpawnFile(string name)
        {
            if (useThisEventGG)
            {
                configData.SpawnFile = name;
                return true;
            }
            return null;
        }
        void OnSelectEventZone(MonoBehaviour monoplayer, string radius)
        {
            if (useThisEventGG)
            {
                return;
            }
        }
        void OnPostZoneCreate(string name)
        {
            if (name == configData.EventName)
            {
                return;
            }
        }
        object CanEventOpen()
        {
            if (useThisEventGG)
            {

            }
            return null;
        }
        object CanEventStart()
        {
            return null;
        }
        object OnEventOpenPost()
        {
            if (useThisEventGG)
            {
                EventManager.BroadcastEvent(EventMessageOpenBroadcast);
                EventManager.UseClassSelection = false;
                if (configData.RankLimit > configData.Weapons.Count)
                {
                    configData.RankLimit = configData.Weapons.Count;
                    SaveConfig(configData);
                }
            }
            return null;
        }
        object OnEventCancel()
        {
            CheckScores(null, false, true);
            return null;
        }
        object OnEventClosePost()
        {
            return null;
        }
        object OnEventEndPre()
        {
            if (useThisEventGG)
            {
                CheckScores(null, false, true);
                DestroyEvent();
            }
            return null;
        }
        object OnEventEndPost()
        {
            var objPlayers = UnityEngine.Object.FindObjectsOfType<GunGamePlayer>();
            if (objPlayers != null)
                foreach (var gameObj in objPlayers)
                    UnityEngine.Object.Destroy(gameObj);
            return null;
        }
        object OnEventStartPre()
        {
            if (useThisEventGG)
            {
                GGStarted = true;
                if (configData.CloseEventAtStart)
                    EventManager.CloseEvent();
            }
            return null;
        }
        object OnEventStartPost()
        {
            return null;
        }
        object CanEventJoin()
        {
            return null;
        }
        object OnSelectKit(string kitname)
        {
            if (useThisEventGG)
            {
                Puts("No Kits required for this gamemode!");
            }
            return null;
        }
        object OnEventJoinPost(BasePlayer player)
        {
            if (useThisEventGG)
            {
                if (player.GetComponent<GunGamePlayer>())
                    UnityEngine.Object.Destroy(player.GetComponent<GunGamePlayer>());
                GunGamePlayers.Add(player.gameObject.AddComponent<GunGamePlayer>());
                player.GetComponent<GunGamePlayer>().level = 1;
                if (GGStarted) AddUI(player);
                
            }
            return null;
        }
        object OnEventLeavePost(BasePlayer player)
        {
            if (useThisEventGG)
            {
                var gunGamePlayer = player.GetComponent<GunGamePlayer>();
                if (gunGamePlayer)
                {
                    GunGamePlayers.Remove(gunGamePlayer);
                    UnityEngine.Object.Destroy(gunGamePlayer);
                    CheckScores(null);
                }
            }
            return null;
        }
        void OnEventPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            if (useThisEventGG && !(hitinfo.HitEntity is BasePlayer))
            {
                hitinfo.damageTypes = new DamageTypeList();
                hitinfo.DoHitEffects = false;
            }
        }

        void OnEventPlayerDeath(BasePlayer victim, HitInfo hitinfo)
        {
            if ((useThisEventGG) && (GGStarted))
            {
                DestroyUI(victim);
                BasePlayer attacker = hitinfo?.Initiator?.ToPlayer();
                if (attacker != null && attacker != victim)
                {
                    if (configData.UseMachete)
                    {
                        if (hitinfo.WeaponPrefab != null && hitinfo.WeaponPrefab.name.Contains(configData.DowngradeWeapon.shortname))
                        {
                            var vicplayerLevel = victim.GetComponent<GunGamePlayer>().level;
                            if (vicplayerLevel == 1)
                            {
                                SendReply(attacker, string.Format("You killed <color=orange>{0}</color> with a <color=orange>{1}</color> but they were already the lowest rank.", victim.displayName, configData.DowngradeWeapon.name));
                                return;
                            }
                            if (vicplayerLevel >= 2)
                            {
                                victim.GetComponent<GunGamePlayer>().level = (vicplayerLevel - 1);
                                SendReply(attacker, string.Format("You killed <color=orange>{0}</color> with a <color=orange>{1}</color> and they have lost a rank!", victim.displayName, configData.DowngradeWeapon.name));
                                SendReply(victim, string.Format("You were killed with a <color=orange>{0}</color> and have lost a rank!", configData.DowngradeWeapon.name));
                                return;
                            }
                        }
                    }
                    AddKill(attacker, victim, GetWeapon(hitinfo));
                }
            }
            return;
        }
        object EventChooseSpawn(BasePlayer player, Vector3 destination)
        {
            return null;
        }
        object OnRequestZoneName()
        {
            if (useThisEventGG)
                if (!string.IsNullOrEmpty(configData.ZoneName))
                    return configData.ZoneName;            
            return null;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Gungame ///////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private void notifyMachete(BasePlayer player)
        {
            if (!GGStarted) return;
            if (player.GetComponent<GunGamePlayer>())
            {
                SendReply(player, string.Format("The downgrade weapon is enabled. Kills with a <color=orange>{0}</color> will lower the victims rank!", configData.DowngradeWeapon.name));
                timer.Once(120, () => notifyMachete(player));
            }
        }
        private void stripGive(BasePlayer player)
        {
            player.inventory.Strip();
            GiveRankKit(player, player.GetComponent<GunGamePlayer>().level);
            if (configData.UseMachete)
                GiveItem(player, configData.DowngradeWeapon.shortname, "belt");
            if (configData.UseMeds)
                foreach (var entry in configData.Meds)
                    GiveItem(player, entry.shortname, entry.container, entry.amount);
            if (configData.UseArmour)
                GiveItem(player, configData.ArmourType, "wear");
            foreach (var entry in configData.PlayerGear)
                GiveItem(player, entry.shortname, entry.container, entry.amount, entry.skin);
        }
        public void GiveRankKit(BasePlayer player, int rank)
        {
            RankItem rankItem;
            if (configData.Weapons.TryGetValue(rank, out rankItem))
            {
                for (var i = 0; i < rankItem.amount; i++)
                    GiveItem(player, rankItem);
                SendReply(player, string.Format("You are Rank <color=orange>{0}</color> ({1})", rank, rankItem.name));
                return;
            }
            Puts("Kit not found, Check your config for errors!");
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Give //////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private ItemDefinition FindItemDefinition(string shortname)
        {
            ItemDefinition itemDefinition;
            return _itemsDict.TryGetValue(shortname, out itemDefinition) ? itemDefinition : null;
        }

        private Item BuildItem(string shortname)
        {
            var definition = FindItemDefinition(shortname);
            if (definition != null)
            {
                var item = ItemManager.Create(definition);
                if (item != null)
                    return item;
            }
            Puts("Error making item: " + shortname);
            return null;
        }

        public void GiveItem(BasePlayer player, RankItem rankItem)
        {
            var definition = FindItemDefinition(rankItem.shortname);
            if (definition == null)
            {
                Puts("Error making item: " + rankItem.shortname);
                return;
            }
            var stack = definition.stackable;
            if (stack < 1) stack = 1;
            for (var i = rankItem.amount; i > 0; i = i - stack)
            {
                var giveamount = i >= stack ? stack : i;
                if (giveamount < 1) return;
                var item = ItemManager.Create(definition, giveamount, false, rankItem.skin);
                if (item == null)
                {
                    Puts("Error making item: " + rankItem.shortname);
                    return;
                }
                var weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (!string.IsNullOrEmpty(rankItem.ammoType))
                    {
                        var ammoType = FindItemDefinition(rankItem.ammoType);
                        if (ammoType != null)
                            weapon.primaryMagazine.ammoType = ammoType;
                    }
                    var ammo = rankItem.ammo - weapon.primaryMagazine.capacity;
                    if (ammo <= 0)
                        weapon.primaryMagazine.contents = rankItem.ammo;
                    else
                    {
                        weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;
                        GiveItem(player, weapon.primaryMagazine.ammoType.shortname, "main", ammo);
                    }
                }
                if (rankItem.contents != null)
                    foreach (var content in rankItem.contents)
                        BuildItem(content)?.MoveToContainer(item.contents);
                ItemContainer cont;
                switch (rankItem.container)
                {
                    case "wear":
                        cont = player.inventory.containerWear;
                        break;
                    case "belt":
                        cont = player.inventory.containerBelt;
                        break;
                    default:
                        cont = player.inventory.containerMain;
                        break;
                }
                player.inventory.GiveItem(item, cont);
            }
        }

        public void GiveItem(BasePlayer player, string shortname, string container, int amount = 1, int skin = 0)
        {
            var definition = FindItemDefinition(shortname);
            if (definition == null)
            {
                Puts("Error making item: " + shortname);
                return;
            }
            var stack = definition.stackable;
            if (stack < 1) stack = 1;
            for (var i = amount; i > 0; i = i - stack)
            {
                var giveamount = i >= stack ? stack : i;
                if (giveamount < 1) return;
                var item = ItemManager.Create(definition, giveamount, false, skin);
                if (item == null)
                {
                    Puts("Error making item: " + shortname);
                    return;
                }
                ItemContainer cont;
                switch (container)
                {
                    case "wear":
                        cont = player.inventory.containerWear;
                        break;
                    case "belt":
                        cont = player.inventory.containerBelt;
                        break;
                    default:
                        cont = player.inventory.containerMain;
                        break;
                }
                player.inventory.GiveItem(item, cont);
            }
        }

        private string GetWeapon(HitInfo hitInfo, string def = "")
        {
            var item = hitInfo.Weapon?.GetItem();
            if (item == null && hitInfo.WeaponPrefab == null) return def;
            var shortname = item?.info.shortname ?? hitInfo.WeaponPrefab.name;
            shortname = shortname.Replace(".prefab", string.Empty);
            shortname = shortname.Replace(".entity", string.Empty);
            shortname = shortname.Replace("_", ".");
            switch (shortname)
            {
                case "rocket.basic":
                case "rocket.fire":
                case "rocket.hv":
                case "rocket.smoke":
                    shortname = "rocket.launcher";
                    break;
            }
            return shortname;
        }

        ////////////////////////////////////////////////////////////
        // Scoring /////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////

        void AddKill(BasePlayer player, BasePlayer victim, string shortname)
        {
            var gunGamePlayer = player.GetComponent<GunGamePlayer>();
            if (gunGamePlayer == null)
                return;

            var leveled = false;
            gunGamePlayer.kills++;
            RankItem rankItem;
            if (configData.Weapons.TryGetValue(gunGamePlayer.level, out rankItem) && rankItem.shortname.Equals(shortname))
            {
                leveled = true;
                gunGamePlayer.level++;
                if (EventManager.EventMode == EventManager.GameMode.Battlefield)
                {
                    if (gunGamePlayer.level >= (configData.RankLimit + 1))
                        gunGamePlayer.level = 1;
                }
            }
            EventManager.AddTokens(player.UserIDString, configData.TokensPerKill);
            EventManager.BroadcastEvent(string.Format(GGMessageKill, player.displayName, gunGamePlayer.kills, gunGamePlayer.level, victim.displayName));
			CheckScores(player, leveled);
            RefreshSB();
        }
        void CheckScores(BasePlayer player, bool leveled = false, bool timelimitreached = false)
        {
            if (GunGamePlayers.Count <= 1)
            {
                EventManager.BroadcastEvent(EventMessageNoMorePlayers);
                EventManager.CloseEvent();
                EventManager.EndEvent();
                return;
            }
            BasePlayer winner = null;
            int topscore = 0;
            bool finished = false;            
            foreach (GunGamePlayer gungameplayer in GunGamePlayers)
            {
                if (gungameplayer == null) continue;
                if (EventManager.EventMode == EventManager.GameMode.Normal)
                {
                    if (gungameplayer.level >= (configData.RankLimit + 1))
                    {
                        winner = gungameplayer.player;
                        finished = true;
                        break;
                    }
                }
                if (timelimitreached)
                {                    
                    if (gungameplayer.kills > topscore)
                    {
                        winner = gungameplayer.player;
                        topscore = gungameplayer.kills;
                        finished = true;
                    }
                }
            }
           
            if (winner != null)
            {
                Winner(winner);
                return;
            }

            if (player != null && !finished && leveled)
                stripGive(player);
        }
        void Winner(BasePlayer player)
        {
            EventManager.AddTokens(player.UserIDString, configData.TokensOnWin);
            EventManager.BroadcastEvent(string.Format(EventMessageWon, player.displayName));
            EventManager.CloseEvent();
            EventManager.EndEvent();
        }

        ////////////////////////////////////////////////////////////
        // Rank Setup //////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        [ChatCommand("gg")]
        private void cmdGunGame(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            if (args == null || args.Length == 0)
            {
                SendReply(player, "<color=orange>Gungame rank setup:</color>");
                SendReply(player, "To add a weapon to a Gungame rank, you must first put the weapon in your hands");
                SendReply(player, "Then type <color=orange>'/gg rank <rank##> <opt:ammo>'</color>");
                SendReply(player, "<color=orange><rank##></color> is the rank you want to assign the weapon");
                SendReply(player, "<color=orange><opt:ammo></color> is the amount of ammo you want to supply with the weapon");
                SendReply(player, "To add a new kit you must set your inventory");
                SendReply(player, "Then type <color=orange>'/gg kit'</color> and it will copy your inventory");
                SendReply(player, "You can only add clothing and medical items to the kit");
                return;
            }
            switch (args[0].ToLower())
            {
                case "rank":
                    {
                        if (args.Length >= 2)
                        {
                            int rank;
                            int.TryParse(args[1], out rank);
                            if (rank >= 1)
                            {
                                int ammo = 1;
                                if (args.Length == 3) int.TryParse(args[2], out ammo);
                                SaveWeapon(player, rank, ammo);
                                return;
                            }
                            SendReply(player, "<color=orange>You must enter a rank number</color>");
                            return;
                        }
                        else SendReply(player, "<color=orange>/gg rank <rank##> <opt:ammo></color> - You must select a rank number");
                    }
                    return;
                case "kit":
                    SetPlayerKit(player);
                    return;
            }
        }
        private bool isAuth(BasePlayer player)
        {
            if (player.net.connection.authLevel >= 1) return true;
            return false;
        }
        private void SaveWeapon(BasePlayer player, int rank, int ammo = 1)
        {
            RankItem weaponEntry = new RankItem();
            Item item = player.GetActiveItem();
            if (item != null)
                if (item.info.category.ToString() == "Weapon")
                {
                    BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                        if (weapon.primaryMagazine != null)
                        {
                            List<string> mods = new List<string>();
                            if (item.contents != null)
                                foreach (var mod in item.contents.itemList)
                                    if (mod.info.itemid != 0) mods.Add(mod.info.shortname);
                            if (mods != null) weaponEntry.contents = mods.ToArray();

                            weaponEntry.ammoType = weapon.primaryMagazine.ammoType.shortname;
                            weaponEntry.ammo = ammo;
                        }

                    weaponEntry.amount = item.amount;
                    weaponEntry.container = "belt";
                    weaponEntry.name = item.info.displayName.english;
                    weaponEntry.shortname = item.info.shortname;
                    weaponEntry.skin = item.skin;

                    if (rank > configData.Weapons.Count) rank = configData.Weapons.Count + 1;
                    if (!configData.Weapons.ContainsKey(rank))
                        configData.Weapons.Add(rank, weaponEntry);
                    else configData.Weapons[rank] = weaponEntry;
                    SaveConfig(configData);
                    SendReply(player, string.Format("You have successfully added <color=orange>{0}</color> as the weapon for Rank <color=orange>{1}</color>", weaponEntry.name, rank));
                    return;
                }
            SendReply(player, "<color=orange>Unable to save item.</color> You must put a weapon in your hands");
        }
        private void SetPlayerKit(BasePlayer player)
        {
            configData.PlayerGear.Clear();
            configData.Meds.Clear();

            foreach (var item in player.inventory.containerWear.itemList)
                SaveItem(item, "wear", true);

            foreach (var item in player.inventory.containerMain.itemList)
            {
                if (item.info.category.ToString() == "Medical")
                    SaveItem(item, "main", false);
                else if (item.info.category.ToString() == "Attire")
                    SaveItem(item, "main", true);
                else SendReply(player, string.Format("Did not save <color=orange>{0}</color>, you may only save clothing and meds to the gungame kit", item.info.displayName.translated));
            }

            foreach (var item in player.inventory.containerBelt.itemList)
            {
                if (item.info.category.ToString() == "Medical")
                    SaveItem(item, "belt", false);
                else if (item.info.category.ToString() == "Attire")
                    SaveItem(item, "belt", true);
                else SendReply(player, string.Format("Did not save <color=orange>{0}</color>, you may only save clothing and meds to the gungame kit", item.info.displayName.translated));
            }

            SaveConfig(configData);
            SendReply(player, "<color=orange>You have successfully saved a new player kit for Gungame</color>");
        }
        private void SaveItem(Item item, string cont, bool data)
        {
            Gear gear = new Gear
            {
                name = item.info.displayName.english,
                amount = item.amount,
                container = cont,
                shortname = item.info.shortname,
                skin = item.skin
            };
            if (data) configData.PlayerGear.Add(gear);
            else configData.Meds.Add(gear);
        }
    }
}

