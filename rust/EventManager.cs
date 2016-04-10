using System.Reflection;
using System;
using System.Data;
using System.Collections.Generic;
using UnityEngine;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
    [Info("Event Manager", "Reneb", "1.2.20", ResourceId = 740)]
    class EventManager : RustPlugin
    {
        ////////////////////////////////////////////////////////////
        // Setting all fields //////////////////////////////////////
        ////////////////////////////////////////////////////////////
        [PluginReference]
        Plugin Spawns;

        [PluginReference]
        Plugin Kits;
         
        [PluginReference]
        Plugin ZoneManager; 



        [PluginReference]
        Plugin DeadPlayersList;

        [PluginReference]
        Plugin FriendlyFire;

        private string EventSpawnFile;
        private string EventGameName;
        private string itemname;





        private bool EventOpen;
        private bool EventStarted;
        private bool EventEnded;
        private bool EventPending;
        private int EventMaxPlayers = 0;
        private int EventMinPlayers = 0;
        private int EventAutoNum = -1;
        private bool isBP;
        private bool Changed;

        private List<string> EventGames;
        private List<EventPlayer> EventPlayers;
        private List<BasePlayer> Godmode;

        private ItemDefinition itemdefinition;

        private int stackable;
        private int giveamount;


        public List<Oxide.Plugins.Timer> AutoArenaTimers = new List<Oxide.Plugins.Timer>();
        public float LastAnnounce;
        public bool AutoEventLaunched = false;


        ////////////////////////////////////////////////////////////
        // EventPlayer class to store informations /////////////////
        ////////////////////////////////////////////////////////////
        class EventInvItem
        {

            public int itemid;

            public bool bp;

            public int skinid;

            public string container;

            public int amount;

            public bool weapon;

            public int ammo;

            public string ammotype;

            public List<int> mods;

            public float condition;



            public EventInvItem()

            {

            }           
        }
        class EventPlayer : MonoBehaviour
        {
            public BasePlayer player;

            public bool inEvent;
            public bool savedInventory;
            public bool savedHome;
            public float preHealth;
            public float calories;
            public float hydration;
            public string zone;
            public List<EventInvItem> InvItems = new List<EventInvItem>();

            public Vector3 Home;

            void Awake()
            {
                inEvent = true;
                savedInventory = false;
                savedHome = false;
                preHealth = 0;
                player = GetComponent<BasePlayer>();
            }
            public void SaveHealth()

            {

                preHealth = player.health;

                calories = player.metabolism.calories.value;

                hydration = player.metabolism.hydration.value;

            }
            public void SaveHome()
            {
                if (!savedHome)
                    Home = player.transform.position;
                savedHome = true;
            }
            public void TeleportHome()
            {
                if (!savedHome)
                    return;
                ForcePlayerPosition(player, Home);
                savedHome = false;
            }

            public void SaveInventory()
            {
                if (savedInventory)
                    return;

                InvItems.Clear();
                foreach (Item item in player.inventory.containerWear.itemList)

                {

                    if (item != null)

                        AddItemToSave(item, "wear");

                }

                foreach (Item item in player.inventory.containerMain.itemList)

                {

                    if (item != null)

                        AddItemToSave(item, "main");

                }

                foreach (Item item in player.inventory.containerBelt.itemList)

                {

                    if (item != null)

                        AddItemToSave(item, "belt");

                }

                savedInventory = true;
            }
            private void AddItemToSave(Item item, string container)

            {

                EventInvItem iItem = new EventInvItem();

                iItem.ammo = 0;

                iItem.amount = item.amount; 

                iItem.mods = new List<int>(); 

                iItem.skinid = item.skin; 

                iItem.container = container; 

                iItem.bp = item.IsBlueprint();

                iItem.condition = item.condition;

                iItem.itemid = item.info.itemid;

                iItem.weapon = false;



                if (item.info.category.ToString() == "Weapon")

                {

                    BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;

                    if (weapon != null)

                    {

                        if (weapon.primaryMagazine != null)

                        {

                            iItem.weapon = true;

                            iItem.ammo = weapon.primaryMagazine.contents;

                            if (item.contents != null)                                

                                foreach (var mod in item.contents.itemList)

                                {

                                    if (mod.info.itemid != 0)

                                        iItem.mods.Add(mod.info.itemid);

                                }

                        }

                    }

                }

                InvItems.Add(iItem);

            }
            public void RestoreInventory()
            {

                foreach (EventInvItem kitem in InvItems)

                {

                    if (kitem.weapon)

                        player.inventory.GiveItem(BuildWeapon(kitem.itemid, kitem.ammo, kitem.bp, kitem.skinid, kitem.mods, kitem.condition), kitem.container == "belt" ? player.inventory.containerBelt : kitem.container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);

                    else player.inventory.GiveItem(BuildItem(kitem.itemid, kitem.amount, kitem.bp, kitem.skinid, kitem.condition), kitem.container == "belt" ? player.inventory.containerBelt : kitem.container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);



                }
                savedInventory = false;
            }
            private Item BuildItem(int itemid, int amount, bool isBP, int skin, float cond)

            {

                if (amount < 1) amount = 1;

                Item item = ItemManager.CreateByItemID(itemid, amount, isBP, skin);

                item.conditionNormalized = cond;

                return item;

            }

            private Item BuildWeapon(int id, int ammo, bool isBP, int skin, List<int> mods, float cond)

            {

                Item item = ItemManager.CreateByItemID(id, 1, isBP, skin);

                item.conditionNormalized = cond;

                var weapon = item.GetHeldEntity() as BaseProjectile;

                if (weapon != null)

                {

                    (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = ammo;

                }

                if (mods != null)

                    foreach (var mod in mods)

                    {

                        item.contents.AddItem(BuildItem(mod, 1, false, 0, cond).info, 1);

                    }



                return item;

            }
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Some Static methods that can be called from the EventPlayer Class /////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        static void PutToSleep(BasePlayer player)
        {
            if (!player.IsSleeping())
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
                if (!BasePlayer.sleepingPlayerList.Contains(player))
                {
                    BasePlayer.sleepingPlayerList.Add(player);
                }
                player.CancelInvoke("InventoryUpdate");
                player.inventory.crafting.CancelAll(true);
            }
        }

        static void ForcePlayerPosition(BasePlayer player, Vector3 destination)
        {
            PutToSleep(player);
            player.MovePosition(destination);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            player.metabolism.Reset();

            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading", null,null,null,null,null);
            player.SendFullSnapshot();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            Changed = false;
            EventGames = new List<string>();
            EventPlayers = new List<EventPlayer>();
            LoadData();
        }
        void OnServerInitialized()
        {
            EventOpen = false;
            EventStarted = false;
            EventEnded = true;
            EventPending = false;
            EventGameName = defaultGame;
            InitializeTable();
            timer.Once(0.1f, () => InitializeZones());
            timer.Once(0.2f, () => InitializeGames());
        }
        void InitializeGames()
        {
            Interface.CallHook("RegisterGame");
            SelectSpawnfile(defaultSpawnfile);
        }
        void Unload()
        {
            EndEvent();
            var objects = GameObject.FindObjectsOfType(typeof(EventPlayer));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            if (!(player.GetComponent<EventPlayer>())) return;
            if (player.GetComponent<EventPlayer>().inEvent)
            {
                if (!EventStarted) return;
                Interface.CallHook("OnEventPlayerSpawn", new object[] { player });
            }
            else
            {
                RedeemInventory(player);
                TeleportPlayerHome(player);
                TryErasePlayer(player);
            }
        }

        void OnPlayerAttack(BasePlayer player, HitInfo hitinfo)
        {
            if (!EventStarted) return;
            if (player.GetComponent<EventPlayer>() == null || !(player.GetComponent<EventPlayer>().inEvent))
            {
                return;
            }
            else if (hitinfo.HitEntity != null)
            {
                Interface.CallHook("OnEventPlayerAttack", new object[] { player, hitinfo });
            }
            return;
        }
        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            if (!EventStarted) return;
            if (!(entity is BasePlayer)) return;
            if ((entity as BasePlayer).GetComponent<EventPlayer>() == null) return;
            Interface.CallHook("OnEventPlayerDeath", new object[] { (entity as BasePlayer), hitinfo });
            return;
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (player.GetComponent<EventPlayer>() != null)
            {
                LeaveEvent(player);
            }
        }
        void OnEntityTakeDamage(BaseEntity entity, HitInfo info)

        {

            var player = entity as BasePlayer;

            var attacker = info.Initiator as BasePlayer;



            if (!player) return;

            if (Godmode == null) return;

            else if (Godmode.Contains(player))

            {

                info.damageTypes = new DamageTypeList();

                info.HitMaterial = 0;

                info.PointStart = Vector3.zero;

            }

        }
        bool hasEventStarted()

        {

            return EventStarted;

        }
        bool isPlaying(BasePlayer player)

        {

            EventPlayer eplayer = player.GetComponent<EventPlayer>();

            if (eplayer == null) return false;

            if (!eplayer.inEvent) return false;

            return true;

        }
        ////////////////////////////////////////////////////////////
        // Zone Management
        ////////////////////////////////////////////////////////////
        void InitializeZones()
        {
            foreach (KeyValuePair<string, EventZone> pair in zonelogs)
            {
                InitializeZone(pair.Key);
            }
        }
        void InitializeZone(string name)
        {
            if (zonelogs[name] == null) return;
            ZoneManager?.Call("CreateOrUpdateZone", name, new string[] { "radius", zonelogs[name].radius }, zonelogs[name].GetPosition());
            if (EventGames.Contains(name))
                Interface.CallHook("OnPostZoneCreate", name);
        }
        void UpdateZone(string name, string[] args)
        {
            ZoneManager?.Call("CreateOrUpdateZone", name, args);
        }
        public class EventZone
        {
            public string name;
            public string x;
            public string y;
            public string z;
            public string radius;
            Vector3 position;

            public EventZone(string name, Vector3 position, float radius)
            {
                this.name = name;
                this.x = position.x.ToString();
                this.y = position.y.ToString();
                this.z = position.z.ToString();
                this.radius = radius.ToString();
            }
            public Vector3 GetPosition()
            {
                if (position == default(Vector3))
                    position = new Vector3(float.Parse(this.x), float.Parse(this.y), float.Parse(this.z));
                return position;
            }

        }

        static StoredData storedData;
        static Hash<string, EventZone> zonelogs = new Hash<string, EventZone>();
        static Hash<string, Reward> rewards = new Hash<string, Reward>();

        class StoredData
        {
            public HashSet<EventZone> ZoneLogs = new HashSet<EventZone>();
            public Hash<string, string> Tokens = new Hash<string, string>();
            public HashSet<Reward> Rewards = new HashSet<Reward>();

            public StoredData()
            {
            }
        }

        void OnServerSave()
        {
            SaveData();
        }

        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("EventManager", storedData);
        }

        void LoadData()
        {
            zonelogs.Clear();
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("EventManager");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var thelog in storedData.ZoneLogs)
            {
                zonelogs[thelog.name] = thelog;
            }
            foreach (var thelog in storedData.Rewards)
            {
                rewards[thelog.name] = thelog;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Tokens Manager
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> displaynameToShortname = new Dictionary<string, string>();

        private void InitializeTable()
        {
            displaynameToShortname.Clear();
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions() as List<ItemDefinition>;
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                displaynameToShortname.Add(itemdef.displayName.english.ToString().ToLower(), itemdef.shortname.ToString());
            }
        }
        object GiveReward(BasePlayer player, string rewardname, int amount)
        {
            if (rewards[rewardname] == null) return "This reward doesn't exist";
            amount = amount * (rewards[rewardname]).GetAmount();
            if (rewards[rewardname].IsKit())
            {
                if (Kits == null) return "Kits plugin couldn't be found";
                if (!(bool)Kits.Call("isKit", rewards[rewardname].item)) return "The kit doesn't exist anymore";
                for (int i = 1; i < amount; i++)
                {
                    Kits.Call("GiveKit", player, rewards[rewardname].item);
                }
                return (bool)Kits.Call("GiveKit", player, rewards[rewardname].item);
            }
            var definition = ItemManager.FindItemDefinition(rewards[rewardname].item);
            if (definition == null)
                return string.Format("Item not found {0}", rewards[rewardname].item);
            if (definition.stackable > 1)
                player.inventory.GiveItem(ItemManager.CreateByItemID((int)definition.itemid, amount, false), player.inventory.containerMain);
            else
            {
                for (int i = 0; i < amount; i++)
                {
                    player.inventory.GiveItem(ItemManager.CreateByItemID((int)definition.itemid, 1, false), player.inventory.containerMain);
                }
            }
            return true;
        }

        public class Reward
        {
            public string name;
            public string cost;
            public string kit;
            public string item;
            public string amount;

            public Reward()
            {

            }
            public Reward(string name, int cost, bool kit, string item, int amount)
            {
                this.name = name;
                this.cost = cost.ToString();
                this.kit = kit.ToString();
                this.item = item;
                this.amount = amount.ToString();
            }
            public int GetCost()
            {
                return int.Parse(cost);
            }
            public int GetAmount()
            {
                return int.Parse(amount);
            }
            public bool IsKit()
            {
                return Convert.ToBoolean(kit);
            }
        }
        void AddTokens(string userid, int amount)
        {
            storedData.Tokens[userid] = (GetTokens(userid) + amount).ToString();
        }

        int GetTokens(string userid)
        {
            if (storedData.Tokens[userid] == null)
                return 0;
            return int.Parse(storedData.Tokens[userid]);
        }

        void RemoveTokens(string userid, int amount)
        {
            storedData.Tokens[userid] = (GetTokens(userid) - amount).ToString();
        }

        void SetTokens(string userid, int amount)
        {
            storedData.Tokens[userid] = amount.ToString();
        }


        public string tokenoverlay = @"[  
		                { 
							""name"": ""EventManagerOverlay"",
                            ""parent"": ""HUD/Overlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 1"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                },
                                {
                                    ""type"":""NeedsCursor"",
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Event Manager"",
                                    ""fontSize"":30,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.3 0.80"",
                                    ""anchormax"": ""0.7 0.90""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{msg}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.3 0.70"",
                                    ""anchormax"": ""0.7 0.79""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Reward"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleLeft"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.2 0.60"",
                                    ""anchormax"": ""0.4 0.65""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Amount"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleLeft"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.49 0.60"",
                                    ""anchormax"": ""0.54 0.65""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Cost"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleLeft"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.55 0.60"",
                                    ""anchormax"": ""0.6 0.65""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Claim"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleLeft"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.65 0.60"",
                                    ""anchormax"": ""0.75 0.65""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Close"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.5 0.20"",
                                    ""anchormax"": ""0.7 0.25""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""close"":""EventManagerOverlay"",
                                    ""color"": ""0.5 0.5 0.5 0.2"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.5 0.20"",
                                    ""anchormax"": ""0.7 0.25""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""<<"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.2 0.20"",
                                    ""anchormax"": ""0.3 0.25""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""color"": ""0.5 0.5 0.5 0.2"",
                                    ""command"":""reward.show {rewardpageminus}"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.2 0.20"",
                                    ""anchormax"": ""0.3 0.25""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":"">>"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.35 0.20"",
                                    ""anchormax"": ""0.45 0.25""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""color"": ""0.5 0.5 0.5 0.2"",
                                    ""command"":""reward.show {rewardpageplus}"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.35 0.20"",
                                    ""anchormax"": ""0.45 0.25""
                                }
                            ]
                        },
                        
                    ]
                    ";
        string tokenjson = @"[
        				{
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{rewardname}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleLeft"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.2 {ymin}"",
                                    ""anchormax"": ""0.4 {ymax}""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{rewardamount}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleLeft"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.49 {ymin}"",
                                    ""anchormax"": ""0.54 {ymax}""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{rewardcost}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleLeft"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.55 {ymin}"",
                                    ""anchormax"": ""0.6 {ymax}""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EventManagerOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""close"":""EventManagerOverlay"",
                                    ""command"":""reward.claim {rewardcmd}"",
                                    ""color"": ""{color}"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.65 {ymin}"",
                                    ""anchormax"": ""0.69 {ymax}""
                                }
                            ]
                        }
                    ]
                    ";

        void ShowRewards(BasePlayer player, double from)
        {
            if (from < 0) return;
            if (from >= rewards.Count) return;
            Oxide.Game.Rust.Cui.CuiHelper.DestroyUi(player, "EventManagerOverlay");
            int currenttokens = GetTokens(player.userID.ToString());
            var ctoverlay = tokenoverlay.Replace("{msg}", string.Format(OverlayGUIMsg, currenttokens.ToString())).Replace("{rewardpageplus}", (from + 6).ToString()).Replace("{rewardpageminus}", (from - 6).ToString());
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(ctoverlay, null, null, null, null));
            double current = 0;
            foreach (KeyValuePair<string, Reward> pair in rewards)
            {
                if (current >= from && current < from + 6)
                {
                    string color = (pair.Value.GetCost() <= currenttokens) ? "0 0.6 0 0.2" : "1 0 0 0.2";
                    double pos = 0.55 - 0.05 * (current - from);
                    var tokenline = tokenjson.Replace("{ymin}", pos.ToString()).Replace("{ymax}", (pos + 0.05).ToString()).Replace("{color}", color).Replace("{rewardname}", pair.Key).Replace("{rewardcmd}", string.Format("'{0}' {1}", pair.Key, from.ToString())).Replace("{rewardcost}", pair.Value.cost).Replace("{rewardamount}", pair.Value.amount);
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(tokenline, null, null, null, null));
                }
                current++;
            }
        }
        [ConsoleCommand("reward.show")]
        void ccmdRewardShow(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length == 0) return;
            if (arg.connection == null) return;
            BasePlayer player = arg.connection.player as BasePlayer;
            if (player == null) return;
            string rewardpage = arg.Args[0].Replace("'", "");
            double rewardp = Convert.ToDouble(rewardpage);
            ShowRewards(player, rewardp);
        }
        [ConsoleCommand("reward.claim")]
        void ccmdRewardClaim(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 2) return;
            if (arg.connection == null) return;
            BasePlayer player = arg.connection.player as BasePlayer;
            if (player == null) return;
            string rewardname = arg.Args[0].Replace("'", "");
            if (rewards[rewardname] == null)
            {
                SendReply(player, MessageRewardWrong);
                return;
            }
            int currenttokens = GetTokens(player.userID.ToString());
            int amount = 1;
            if (rewards[rewardname].GetCost() * amount > currenttokens)
            {
                SendReply(player, string.Format(MessageRewardNotEnoughTokens, rewardname, amount.ToString()));
                return;
            }
            var success = GiveReward(player, rewardname, amount);
            if (success is string)
            {
                SendReply(player, success.ToString());
                return;
            }
            if (success is bool && (bool)success)
                RemoveTokens(player.userID.ToString(), rewards[rewardname].GetCost() * amount);
            ShowRewards(player, Convert.ToDouble(arg.Args[1]));
        }
        [ChatCommand("reward")]
        void cmdEventReward(BasePlayer player, string command, string[] args)
        {
            ShowRewards(player, 0.0);
        }


        //////////////////////////////////////////////////////////////////////////////////////
        // Configs Manager ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        private static string MessagesPermissionsNotAllowed = "You are not allowed to use this command";
        private static string MessagesEventNotSet = "An Event game must first be chosen.";
        private static string MessagesErrorSpawnfileIsNull = "The spawnfile can't be set to null";
        private static string MessagesEventNoSpawnFile = "A spawn file must first be loaded.";
        private static string MessagesEventAlreadyOpened = "The Event is already open.";
        private static string MessagesEventAlreadyClosed = "The Event is already closed.";
        private static string MessagesEventAlreadyStarted = "An Event game has already started.";


        private static string MessagesEventOpen = "The Event is now open for : {0} !  Type /event_join to join!";
        private static string MessagesEventClose = "The Event entrance is now closed!";
        private static string MessagesEventCancel = "The Event was cancelled!";
        private static string MessagesEventNoGamePlaying = "An Event game is not underway.";
        private static string MessagesEventEnd = "All players respawned, {0} has ended!";

        private static string MessagesEventPreEnd = "Event: {0} is now over, waiting for players to respawn before sending home!";

        private static string MessagesEventAlreadyJoined = "You are already in the Event.";
        private static string MessagesEventJoined = "{0} has joined the Event!  (Total Players: {1})";
        private static string MessagesEventLeft = "{0} has left the Event! (Total Players: {1})";
        private static string MessagesEventBegin = "Event: {0} is about to begin!";
        private static string MessagesEventNotInEvent = "You are not currently in the Event.";
        private static string MessagesEventNotAnEvent = "This Game {0} isn't registered, did you reload the game after loading Event - Core?";
        private static string MessagesEventCloseAndEnd = "The Event needs to be closed and ended before using this command.";


        private static string MessagesEventStatusOpen = "The Event {0} is currently opened for registration: /event_join";
        private static string MessagesEventStatusOpenStarted = "The Event {0} has started, but is still opened: /event_join";
        private static string MessagesEventStatusClosedEnd = "There is currently no event";
        private static string MessagesEventStatusClosedStarted = "The Event {0} has already started, it's too late to join.";

        private static string MessagesEventMaxPlayers = "The Event {0} has reached max players. You may not join for the moment";
        private static string MessagesEventMinPlayers = "The Event {0} has reached min players and will start in {1} seconds";
        private static bool EventAutoEvents = false;
        private static int EventAutoInterval = 600;
        private static int EventAnnounceDuringInterval = 60;
        private static bool EventAutoAnnounceDuring = false;
        private static int EventAutoCancelTimer = 600;
        private static int EventAutoAnnounceInterval = 60;
        private static Dictionary<string, object> EventAutoConfig = CreateDefaultAutoConfig();

        private static string MessageRewardCurrentReward = "You currently have {0} for the /reward shop";
        private static string MessageRewardCurrent = "You have {0} tokens";
        private static string MessageRewardHelp = "/reward \"RewardName\" Amount";
        private static string MessageRewardItem = "Reward Name: {0} - Cost: <color={4}>{1}</color> - {2} - Amount: {3}";
        private static string MessageRewardWrong = "This reward doesn't exist";
        private static string MessageRewardNegative = "The amount to buy can't be 0 or negative.";
        private static string MessageRewardNotEnoughTokens = "You don't have enough tokens to buy {1} of {0}.";

        private static string noPlayerFound = "No players found";
        private static string multipleNames = "Multiple players found";

        private static string defaultGame = "Deathmatch";
        private static string defaultSpawnfile = "deathmatchspawns";
        private static int eventAuth = 1;

        public string OverlayGUIMsg = "You currently have <color=green>{0}</color> tokens.";

        void Init()
        {
            CheckCfg<int>("Settings - authLevel", ref eventAuth);

            CheckCfg<string>("Default - Game", ref defaultGame);
            CheckCfg<string>("Default - Spawnfile", ref defaultSpawnfile);

            CheckCfg<bool>("AutoEvents - Activate", ref EventAutoEvents);
            CheckCfg<int>("AutoEvents - Interval between 2 events", ref EventAutoInterval);
            CheckCfg<int>("AutoEvents - Announce Open Interval", ref EventAutoAnnounceInterval);
            CheckCfg<int>("AutoEvents - Event cancel timer", ref EventAutoCancelTimer);
            CheckCfg<bool>("Broadcast - Broadcast join message during a round", ref EventAutoAnnounceDuring);
            CheckCfg("Broadcast - Join message interval", ref EventAnnounceDuringInterval);
            CheckCfg<Dictionary<string, object>>("AutoEvents - Config", ref EventAutoConfig);

            CheckCfg<string>("Messages - Permissions - Not Allowed", ref MessagesPermissionsNotAllowed);
            CheckCfg<string>("Messages - Event Error - Not Set", ref MessagesEventNotSet);
            CheckCfg<string>("Messages - Event Error - No SpawnFile", ref MessagesEventNoSpawnFile);
            CheckCfg<string>("Messages - Event Error - SpawnFile Is Null", ref MessagesErrorSpawnfileIsNull);
            CheckCfg<string>("Messages - Event Error - Already Opened", ref MessagesEventAlreadyOpened);
            CheckCfg<string>("Messages - Event Error - Already Closed", ref MessagesEventAlreadyClosed);
            CheckCfg<string>("Messages - Event Error - No Games Undergoing", ref MessagesEventNoGamePlaying);
            CheckCfg<string>("Messages - Event Error - Already Joined", ref MessagesEventAlreadyJoined);
            CheckCfg<string>("Messages - Event Error - Already Started", ref MessagesEventAlreadyStarted);
            CheckCfg<string>("Messages - Event Error - Not In Event", ref MessagesEventNotInEvent);
            CheckCfg<string>("Messages - Event Error - Not Registered Event", ref MessagesEventNotAnEvent);
            CheckCfg<string>("Messages - Event Error - Close&End", ref MessagesEventCloseAndEnd);



            CheckCfg<string>("Messages - Error - No players found", ref noPlayerFound);
            CheckCfg<string>("Messages - Error - Multiple players found", ref multipleNames);

            CheckCfg<string>("Messages - Status - Closed & End", ref MessagesEventStatusClosedEnd);
            CheckCfg<string>("Messages - Status - Closed & Started", ref MessagesEventStatusClosedStarted);
            CheckCfg<string>("Messages - Status - Open", ref MessagesEventStatusOpen);
            CheckCfg<string>("Messages - Status - Open & Started", ref MessagesEventStatusOpenStarted);

            CheckCfg<string>("Messages - Event - Opened", ref MessagesEventOpen);
            CheckCfg<string>("Messages - Event - Closed", ref MessagesEventClose);
            CheckCfg<string>("Messages - Event - Cancelled", ref MessagesEventCancel);
            CheckCfg<string>("Messages - Event - End", ref MessagesEventEnd);
            CheckCfg<string>("Messages - Event - Pre-End", ref MessagesEventPreEnd);
            CheckCfg<string>("Messages - Event - Join", ref MessagesEventJoined);
            CheckCfg<string>("Messages - Event - Begin", ref MessagesEventBegin);
            CheckCfg<string>("Messages - Event - Left", ref MessagesEventLeft);

            CheckCfg<string>("Messages - Event - MaxPlayersReached", ref MessagesEventMaxPlayers);
            CheckCfg<string>("Messages - Event - MinPlayersReached", ref MessagesEventMinPlayers);

            CheckCfg<string>("Messages - Reward - Message", ref MessageRewardCurrentReward);
            CheckCfg<string>("Messages - Reward - GUI Message", ref OverlayGUIMsg);
            CheckCfg<string>("Messages - Reward - Current", ref MessageRewardCurrent);
            CheckCfg<string>("Messages - Reward - Help", ref MessageRewardHelp);
            CheckCfg<string>("Messages - Reward - Reward Description", ref MessageRewardItem);
            CheckCfg<string>("Messages - Reward - Doesnt Exist", ref MessageRewardWrong);
            CheckCfg<string>("Messages - Reward - Negative Amount", ref MessageRewardNegative);
            CheckCfg<string>("Messages - Reward - Not Enough Tokens", ref MessageRewardNotEnoughTokens);

            SaveConfig();
        }

        static Dictionary<string, object> CreateDefaultAutoConfig()
        {
            var newautoconfiglist = new Dictionary<string, object>();
            var AutoDM = new Dictionary<string, object>();
            AutoDM.Add("gametype", "Deathmatch");
            AutoDM.Add("spawnfile", "deathmatchspawnfile");
            AutoDM.Add("closeonstart", "false");
            AutoDM.Add("timetojoin", "30");
            AutoDM.Add("minplayers", "1");
            AutoDM.Add("maxplayers", "10");
            AutoDM.Add("timelimit", "1800");

            var AutoBF = new Dictionary<string, object>();
            AutoBF.Add("gametype", "Battlefield");
            AutoBF.Add("spawnfile", "battlefieldspawnfile");
            AutoBF.Add("closeonstart", "false");
            AutoBF.Add("timetojoin", "0");
            AutoBF.Add("timelimit", null);
            AutoBF.Add("minplayers", "0");
            AutoBF.Add("maxplayers", "30");

            newautoconfiglist.Add("0", AutoDM);
            newautoconfiglist.Add("1", AutoBF);

            return newautoconfiglist;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Some global methods ///////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        // hasAccess /////////////////////////////////////////////////////////////////////////
        bool hasAccess(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, MessagesPermissionsNotAllowed);
                    return false;
                }
            }
            return true;
        }

        // Broadcast To The General Chat /////////////////////////////////////////////////////
        void BroadcastToChat(string msg)
        {
            Debug.Log(msg);
            ConsoleSystem.Broadcast("chat.add", new object[] { 0, "<color=orange>Event:</color> " + msg });
        }

        // Broadcast To Players in Event /////////////////////////////////////////////////////
        void BroadcastEvent(string msg)
        {
            foreach (EventPlayer eventplayer in EventPlayers)
            {
                SendReply(eventplayer.player, msg.QuoteSafe());
            }
        }

        void TeleportAllPlayersToEvent()
        {
            foreach (EventPlayer eventplayer in EventPlayers)
            {
                Interface.CallHook("OnEventPlayerSpawn", new object[] { eventplayer.player });
            }
        }
        void OnEventPlayerSpawn(BasePlayer player)
        {
            TeleportPlayerToEvent(player);
        }
        void TeleportPlayerToEvent(BasePlayer player)
        {
            if (!(player.GetComponent<EventPlayer>())) return;
            var targetpos = Spawns.Call("GetRandomSpawn", new object[] { EventSpawnFile });
            if (targetpos is string)
                return;
            var newpos = Interface.Call("EventChooseSpawn", new object[] { player, targetpos });
            if (newpos is Vector3)
                targetpos = newpos;

            var zonen = Interface.Call("OnRequestZoneName");
            string zonename = zonen is string ? (string)zonen : EventGameName;
            ZoneManager?.Call("AddPlayerToZoneKeepinlist", zonename, player);
            player.GetComponent<EventPlayer>().zone = zonename;

            ForcePlayerPosition(player, (Vector3)targetpos);
        }

        void SaveAllInventories()
        {
            foreach (EventPlayer player in EventPlayers)
            {
                if (player != null)
                    player.SaveInventory();
            }
        }
        void SaveAllPlayerStats()
        {
            foreach (EventPlayer player in EventPlayers)
            {
                if (player != null)
                    player.SaveHealth();
            }
        }
        void SaveAllHomeLocations()
        {
            foreach (EventPlayer player in EventPlayers)
            {
                player.SaveHome();
            }
        }
        void SaveInventory(BasePlayer player)
        {
            var eventplayer = player.GetComponent<EventPlayer>();
            if (eventplayer == null) return;
            eventplayer.SaveInventory();
        }
        void SaveHomeLocation(BasePlayer player)
        {
            EventPlayer eventplayer = player.GetComponent<EventPlayer>();
            if (eventplayer == null) return;
            eventplayer.SaveHome();
        }
        void SavePlayerHealth(BasePlayer player)
        {
            EventPlayer eventplayer = player.GetComponent<EventPlayer>();
            if (eventplayer == null) return;
            eventplayer.SaveHealth();
        }
        void RedeemInventory(BasePlayer player)
        {
            EventPlayer eventplayer = player.GetComponent<EventPlayer>();
            if (eventplayer == null) return;
            if (player.IsDead() || player.health < 1)
                return;
            if (eventplayer.savedInventory)
            {
                eventplayer.player.inventory.Strip();
                eventplayer.RestoreInventory();
            }
        }
        void TeleportPlayerHome(BasePlayer player)
        {
            EventPlayer eventplayer = player.GetComponent<EventPlayer>();
            if (eventplayer == null) return;
            if (player.IsDead() || player.health < 1)
                return;
            if (eventplayer.savedHome)
            {
                eventplayer.TeleportHome();
            }
        }
        void TryErasePlayer(BasePlayer player)
        {
            var eventplayer = player.GetComponent<EventPlayer>();
            if (eventplayer == null) return;
            if (!(eventplayer.inEvent) && !(eventplayer.savedHome) && !(eventplayer.savedInventory))
                GameObject.Destroy(eventplayer);
        }
        void GivePlayerKit(BasePlayer player, string GiveKit)
        {
            Kits.Call("GiveKit", player, GiveKit);
        }
        void EjectPlayer(BasePlayer player)
        {
            if (player.IsAlive())
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
                player.CancelInvoke("WoundingEnd");
                player.metabolism.bleeding.value = 0f;
            }
            SendReply(player, string.Format(MessageRewardCurrentReward, GetTokens(player.userID.ToString()).ToString()));
        }

        void EjectAllPlayers()
        {
            foreach (EventPlayer eventplayer in EventPlayers)
            {
                EjectPlayer(eventplayer.player);
                if (eventplayer.zone != null)
                    ZoneManager?.Call("RemovePlayerFromZoneKeepinlist", eventplayer.zone, eventplayer.player);
                eventplayer.inEvent = false;
            }
        }
        void SendPlayersHome()
        {
            foreach (EventPlayer eventplayer in EventPlayers)
            {
                TeleportPlayerHome(eventplayer.player);
            }
        }
        void RestorePlayerHealth(BasePlayer player)

        {

            EventPlayer eventplayer = player.GetComponent<EventPlayer>();            
            if (eventplayer == null) return;
            player.health = eventplayer.preHealth;

            player.metabolism.calories.value = eventplayer.calories;

            player.metabolism.hydration.value = eventplayer.hydration;

            player.metabolism.bleeding.value = 0;

            player.metabolism.SendChangesToClient();

        }
        void RedeemPlayersInventory()
        {
            foreach (EventPlayer eventplayer in EventPlayers)
            {
                RedeemInventory(eventplayer.player);

            }
        }
       void TryEraseAllPlayers()
        {
            foreach (EventPlayer eventplayer in EventPlayers)
            {
                TryErasePlayer(eventplayer.player);
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Methods to Change the Arena Status ////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        object OpenEvent()
        {
            var success = Interface.CallHook("CanEventOpen", new object[] { });
            if (success is string)
            {
                return (string)success;
            }
            EventOpen = true;
            EventPlayers.Clear();
            BroadcastToChat(string.Format(MessagesEventOpen, EventGameName));
            Interface.CallHook("OnEventOpenPost", new object[] { });
            return true;
        }
        void OpenTimer()

        {

            OpenEvent();

            

        }
        void OnEventOpenPost()
        {
            OnEventOpenPostAutoEvent();
        }
        void OnEventOpenPostAutoEvent()
        {
            if (!AutoEventLaunched) return;

            DestroyTimers();
            var evencfg = EventAutoConfig[EventAutoNum.ToString()] as Dictionary<string, object>;
            if (evencfg["timelimit"] != null && evencfg["timelimit"] != "0")
                AutoArenaTimers.Add(timer.Once(Convert.ToSingle(evencfg["timelimit"]), () => CancelEvent("Not enough players")));
            AutoArenaTimers.Add(timer.Repeat(EventAutoAnnounceInterval, 0, () => AnnounceEvent()));
        }
        object CanEventOpen()
        {
            if (EventGameName == null) return MessagesEventNotSet;
            else if (EventSpawnFile == null) return MessagesEventNoSpawnFile;
            else if (EventOpen) return MessagesEventAlreadyOpened;

            object success = Spawns.Call("GetSpawnsCount", new object[] { EventSpawnFile });
            if (success is string)
            {
                return (string)success;
            }

            return null;
        }
        object CloseEvent()
        {
            if (!EventOpen) return MessagesEventAlreadyClosed;
            EventOpen = false;
            Interface.CallHook("OnEventClosePost", new object[] { });
            if (EventStarted)
                BroadcastToChat(MessagesEventClose);
            else
                BroadcastToChat(MessagesEventCancel);
            return true;
        }
        object AutoEventNext()
        {
            if (EventAutoConfig.Count == 0)
            {
                AutoEventLaunched = false;
                return "No Automatic Events Configured";
            }
            bool successed = false;
            for (int i = 0; i < EventAutoConfig.Count; i++)
            {
                EventAutoNum++;
                if (EventAutoNum >= EventAutoConfig.Count) EventAutoNum = 0;

                var evencfg = EventAutoConfig[EventAutoNum.ToString()] as Dictionary<string, object>;

                object success = SelectEvent((string)evencfg["gametype"]);
                if (success is string) { continue; }

                success = SelectSpawnfile((string)evencfg["spawnfile"]);
                if (success is string) { continue; }

                success = SelectMinplayers((string)evencfg["minplayers"]);
                if (success is string) { continue; }

                success = SelectMaxplayers((string)evencfg["maxplayers"]);
                if (success is string) { continue; }

                success = Interface.CallHook("CanEventOpen", new object[] { });
                if (success is string) { continue; }

                successed = true;
                break;
            }
            if (!successed)
            {
                return "No Events were successfully initialized, check that your events are correctly configured in AutoEvents - Config";
            }

            AutoArenaTimers.Add(timer.Once(EventAutoInterval, () => OpenTimer()));
            return null;
        }
        void OnEventStartPost()
        {
            DestroyTimers();
            OnEventStartPostAutoEvent();
            if (EventAutoAnnounceDuring)
                AutoArenaTimers.Add(timer.Repeat(EventAnnounceDuringInterval, 0, () => AnnounceDuringEvent()));
        }
        void OnEventStartPostAutoEvent()
        {
            if (!AutoEventLaunched) return;

            DestroyTimers();
            AutoArenaTimers.Add(timer.Once(600f, () => CancelEvent("Time limit reached")));
        }
        void DestroyTimers()
        {
            foreach (Oxide.Plugins.Timer eventimer in AutoArenaTimers)
            {
                eventimer.Destroy();
            }
            AutoArenaTimers.Clear();
        }
        void CancelEvent(string reason)
        {
            var message = "Event {0} was cancelled for {1}";
            object success = Interface.CallHook("OnEventCancel", new object[] { });
            if (success != null)
            {
                if (success is string)
                    message = (string)success;
                else
                    return;
            }
            BroadcastToChat(string.Format(message, EventGameName));
            DestroyTimers();
            EndEvent();
        }
        void AnnounceEvent()
        {
            var message = "Event {0} in now opened, you join it by saying /event_join";
            object success = Interface.CallHook("OnEventAnnounce", new object[] { });
            if (success is string)
            {
                message = (string)success;
            }            
            BroadcastToChat(string.Format(message, EventGameName));
        }
        void AnnounceDuringEvent()

        {

            if (EventAutoAnnounceDuring)

            {

                if (EventOpen && EventStarted)

                {

                    var message = "Event {0} is still open, you join it by saying /event_join";

                    foreach (BasePlayer player in BasePlayer.activePlayerList)

                    {

                        if (!player.GetComponent<EventPlayer>())

                            SendReply(player, string.Format("<color=orange>Event:</color> " + message, EventGameName));

                    }

                }

            }

        }
        object LaunchEvent()
        {
            // just activate it and take over from where it is currently.
            AutoEventLaunched = true;

            if (!EventStarted)
            {
                if (!EventOpen)
                {
                    object success = AutoEventNext();
                    if (success is string)
                    {
                        return (string)success;
                    }
                    success = OpenEvent();
                    if (success is string)
                    {
                        return (string)success;
                    }
                }
                else
                {
                    OnEventOpenPostAutoEvent();
                    // start laiunch timer if min players reached
                }
            }
            else
            {
                OnEventStartPostAutoEvent();
            }
            return null;
        }
        object EndEvent()
        {
            if (EventEnded) return MessagesEventNoGamePlaying;            
            BroadcastToChat(string.Format(MessagesEventPreEnd, EventGameName));
            EventOpen = false;
            EventStarted = false;
            EventPending = false;

            EnableGod();

            timer.Once(5, ()=> ProcessPlayers());           
            return true;
        }
        void EnableGod()

        {

            Godmode = new List<BasePlayer>();

            foreach (EventPlayer player in EventPlayers)

            {

                Godmode.Add(player.player);

                player.player.metabolism.bleeding.value = 0;

                player.player.metabolism.SendChangesToClient();

            }

        }
        void DisableGod()

        {

            foreach (BasePlayer player in Godmode) RestorePlayerHealth(player);

            Godmode.Clear();

        }

        void ProcessPlayers()

        {

            if (!CheckForDead())

            {

                timer.Once(5, () => ProcessPlayers());

                return;

            }

            EventEnded = true;

            BroadcastToChat(string.Format(MessagesEventEnd, EventGameName));

            Interface.CallHook("OnEventEndPre", new object[] { });                        

            DisableGod();

            RedeemPlayersInventory();

            SendPlayersHome();

            TryEraseAllPlayers();

            EjectAllPlayers();

            EventPlayers.Clear();

            Interface.CallHook("OnEventEndPost", new object[] { });

        }
        bool CheckForDead()

        {

            int i = 0;

            

            foreach (EventPlayer p in EventPlayers)

            {

                if (p.player.IsDead() || !p.player.IsAlive())

                {

                    var pos = Spawns.Call("GetRandomSpawn", new object[] { EventSpawnFile });

                    if (pos is Vector3) p.player.RespawnAt((Vector3)pos, new Quaternion());

                    else p.player.Respawn();

                    i++;

                }

                else if (p.player.IsWounded() || p.player.health < 1) { p.player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false); RestorePlayerHealth(p.player); i++; }

                else if (p.player.IsSleeping()) { p.player.EndSleeping(); i++; }



                }

            if (i != 0) return false;

            return true;

        }
        object CanEventStart()
        {
            if (EventGameName == null) return MessagesEventNotSet;
            else if (EventSpawnFile == null) return MessagesEventNoSpawnFile;
            else if (EventStarted) return MessagesEventAlreadyStarted;
            return null;
        }
        object StartEvent()
        {
            object success = Interface.CallHook("CanEventStart", new object[] { });
            if (success is string)
            {
                return (string)success;
            }
            Interface.CallHook("OnEventStartPre", new object[] { });
            BroadcastToChat(string.Format(MessagesEventBegin, EventGameName));
            EventStarted = true;
            EventEnded = false;
            DestroyTimers();
            SaveAllInventories();
            SaveAllHomeLocations();
            SaveAllPlayerStats();
            TeleportAllPlayersToEvent();
            Interface.CallHook("OnEventStartPost", new object[] { });
            return true;
        }
        object JoinEvent(BasePlayer player)
        {
            if (player.GetComponent<EventPlayer>())
            {
                if (EventPlayers.Contains(player.GetComponent<EventPlayer>()))
                    return MessagesEventAlreadyJoined;
            }

            object success = Interface.CallHook("CanEventJoin", new object[] { player });
            if (success is string)
            {
                return (string)success;
            }
            EventPlayer event_player = player.GetComponent<EventPlayer>();
            if (event_player == null) event_player = player.gameObject.AddComponent<EventPlayer>();
            event_player.inEvent = true;
            event_player.enabled = true;
            EventPlayers.Add(event_player);
            FriendlyFire?.Call("EnableBypass", player.userID);
            if (EventStarted)
            {

                SaveHomeLocation(player);
                SavePlayerHealth(player);
                SaveInventory(player);
                Interface.CallHook("OnEventPlayerSpawn", new object[] { player });
            }
            BroadcastToChat(string.Format(MessagesEventJoined, player.displayName.ToString(), EventPlayers.Count.ToString()));

            Interface.CallHook("OnEventJoinPost", new object[] { player });
            return true;
        }
        object CanEventJoin(BasePlayer player)
        {
            if (!EventOpen)
                return "The Event is currently closed.";

            if (EventMaxPlayers != 0 && EventPlayers.Count >= EventMaxPlayers)
            {
                return string.Format(MessagesEventMaxPlayers, EventGameName);
            }
            return null;
        }
        object OnEventJoinPost(BasePlayer player)
        {
            if (!AutoEventLaunched) return null;
            if (EventPlayers.Count >= EventMinPlayers && !EventStarted && EventEnded && !EventPending)
            {
                var evencfg = EventAutoConfig[EventAutoNum.ToString()] as Dictionary<string, object>;
                float timerStart = evencfg["timetojoin"] != null ? Convert.ToSingle(evencfg["timetojoin"]) : 30f;
                BroadcastToChat(string.Format(MessagesEventMinPlayers, EventGameName, timerStart.ToString()));

                EventPending = true;
                DestroyTimers();
                AutoArenaTimers.Add(timer.Once(timerStart, () => StartEvent()));
            }
            return null;
        }
        void OnEventEndPost()
        {
            if (!AutoEventLaunched) return;
            DestroyTimers();
            AutoEventNext();
        }
        object LeaveEvent(BasePlayer player)
        {
            if (player.GetComponent<EventPlayer>() == null)
            {
                return "You are not currently in the Event.";
            }
            if (!EventPlayers.Contains(player.GetComponent<EventPlayer>()))
            {
                return "You are not currently in the Event.";
            }
            Interface.CallHook("OnEventLeavePre", new object[] { player });
            FriendlyFire?.Call("DisableBypass", player.userID);
            player.GetComponent<EventPlayer>().inEvent = false;
            if (!EventEnded || !EventStarted)
            {
                BroadcastToChat(string.Format(MessagesEventLeft, player.displayName.ToString(), (EventPlayers.Count - 1).ToString()));
            }
            if (player.GetComponent<EventPlayer>().zone != null)
                ZoneManager?.Call("RemovePlayerFromZoneKeepinlist", player.GetComponent<EventPlayer>().zone, player);
            if (EventStarted)
            {
                player.inventory.Strip();
                RedeemInventory(player);                
                TeleportPlayerHome(player);
                RestorePlayerHealth(player);
                EventPlayers.Remove(player.GetComponent<EventPlayer>());
                EjectPlayer(player);
                TryErasePlayer(player);
                Interface.CallHook("OnEventLeavePost", new object[] { player });
            }
            else
            {
                EventPlayers.Remove(player.GetComponent<EventPlayer>());
                GameObject.Destroy(player.GetComponent<EventPlayer>());
            }
            return true;
        }

        object SelectEvent(string name)
        {
            if (!(EventGames.Contains(name))) return string.Format(MessagesEventNotAnEvent, name);
            if (EventStarted || EventOpen) return MessagesEventCloseAndEnd;
            EventGameName = name;
            Interface.CallHook("OnSelectEventGamePost", new object[] { name });
            return true;
        }
        object SelectSpawnfile(string name)
        {
            if (name == null) return MessagesErrorSpawnfileIsNull;
            if (EventGameName == null || EventGameName == "") return MessagesEventNotSet;
            if (!(EventGames.Contains(EventGameName))) return string.Format(MessagesEventNotAnEvent, EventGameName.ToString());

            object success = Interface.CallHook("OnSelectSpawnFile", new object[] { name });
            if (success == null)
            {
                return string.Format(MessagesEventNotAnEvent, EventGameName.ToString());
            }

            EventSpawnFile = name;
            success = Spawns.Call("GetSpawnsCount", new object[] { EventSpawnFile });

            if (success is string)
            {
                EventSpawnFile = null;
                return (string)success;
            }

            return true;
        }
        object SelectKit(string kitname)
        {
            if (kitname == null) return "You can't have a null kitname";
            if (EventGameName == null || EventGameName == "") return MessagesEventNotSet;
            if (!(EventGames.Contains(EventGameName))) return string.Format(MessagesEventNotAnEvent, EventGameName.ToString());

            object success = Kits.Call("isKit", kitname);
            if (!(success is bool))
            {
                return "Do you have the kits plugin?";
            }
            if (!(bool)success)
            {
                return string.Format("The kit {0} doesn't exist", kitname);
            }

            success = Interface.CallHook("OnSelectKit", new object[] { kitname });
            if (success == null)
            {
                return "The Current Event doesn't let you select a Kit";
            }
            return true;
        }
        object SelectMaxplayers(string num)
        {
            int mplayer = 0;
            if (EventGameName == null || EventGameName == "") return MessagesEventNotSet;
            if (!(EventGames.Contains(EventGameName))) return string.Format(MessagesEventNotAnEvent, EventGameName.ToString());

            if (!int.TryParse(num, out mplayer))
            {
                return string.Format("{0} is not a number", num);
            }

            EventMaxPlayers = mplayer;

            Interface.CallHook("OnPostSelectMaxPlayers", EventMaxPlayers);

            return true;
        }
        object SelectMinplayers(string num)
        {
            int mplayer = 0;
            if (EventGameName == null || EventGameName == "") return MessagesEventNotSet;
            if (!(EventGames.Contains(EventGameName))) return string.Format(MessagesEventNotAnEvent, EventGameName.ToString());

            if (!int.TryParse(num, out mplayer))
            {
                return string.Format("{0} is not a number", num);
            }

            EventMinPlayers = mplayer;

            Interface.CallHook("OnPostSelectMinPlayers", EventMinPlayers);

            return true;
        }
        object SelectNewZone(MonoBehaviour monoplayer, string radius)
        {
            if (EventGameName == null || EventGameName == "") return MessagesEventNotSet;
            if (!(EventGames.Contains(EventGameName))) return string.Format(MessagesEventNotAnEvent, EventGameName.ToString());
            if (EventStarted || EventOpen) return MessagesEventCloseAndEnd;
            Interface.CallHook("OnSelectEventZone", new object[] { monoplayer, radius });
            if (zonelogs[EventGameName] != null) storedData.ZoneLogs.Remove(zonelogs[EventGameName]);
            zonelogs[EventGameName] = new EventZone(EventGameName, monoplayer.transform.position, Convert.ToSingle(radius));
            storedData.ZoneLogs.Add(zonelogs[EventGameName]);
            InitializeZone(EventGameName);
            return true;
        }
        object RegisterEventGame(string name)
        {
            if (!(EventGames.Contains(name)))
                EventGames.Add(name);
            Puts(string.Format("Registered event game: {0}", name));
            Interface.CallHook("OnSelectEventGamePost", new object[] { EventGameName });

            if (EventGameName == name)
            {
                object success = SelectEvent(EventGameName);
                if (success is string)
                {
                    Puts((string)success);
                }
            }
            if (zonelogs[name] != null)
                timer.Once(0.5f, () => InitializeZone(name));
            return true;
        }

        object canRedeemKit(BasePlayer player)
        {
            TryErasePlayer(player);
            EventPlayer eplayer = player.GetComponent<EventPlayer>();
            if (eplayer == null) return null;
            return false;
        }
        object canShop(BasePlayer player)
        {
            if (!EventStarted) return null;
            EventPlayer eplayer = player.GetComponent<EventPlayer>();
            if (eplayer == null) return null;
            return "You are not allowed to shop while in an Event";
        }

        Dictionary<string, string> deadPlayers = new Dictionary<string, string>();
        bool FindPlayer(string name, out string targetid, out string targetname)
        {
            ulong userid;
            targetid = string.Empty;
            targetname = string.Empty;
            if (name.Length == 17 && ulong.TryParse(name, out userid))
            {
                targetid = name;
                return true;
            }

            foreach (BasePlayer player in Resources.FindObjectsOfTypeAll(typeof(BasePlayer)))
            {
                if (player.displayName == name)
                {
                    targetid = player.userID.ToString();
                    targetname = player.displayName;
                    return true;
                }
                if (player.displayName.Contains(name))
                {
                    if (targetid == string.Empty)
                    {
                        targetid = player.userID.ToString();
                        targetname = player.displayName;
                    }
                    else
                    {
                        targetid = multipleNames;
                    }
                }
            }
            if (targetid == multipleNames)
                return false;
            if (targetid != string.Empty)
                return true;
            targetid = noPlayerFound;
            if (DeadPlayersList == null)
                return false;
            deadPlayers = DeadPlayersList.Call("GetPlayerList", null) as Dictionary<string, string>;
            if (deadPlayers == null)
                return false;

            foreach (KeyValuePair<string, string> pair in deadPlayers)
            {
                if (pair.Value == name)
                {
                    targetid = pair.Key;
                    targetname = pair.Value;
                    return true;
                }
                if (pair.Value.Contains(name))
                {
                    if (targetid == noPlayerFound)
                    {
                        targetid = pair.Key;
                        targetname = pair.Value;
                    }
                    else
                    {
                        targetid = multipleNames;
                    }
                }
            }
            if (targetid == multipleNames)
                return false;
            if (targetid != noPlayerFound)
                return true;
            return false;
        }       
        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Commands /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        [ChatCommand("event_leave")]
        void cmdEventLeave(BasePlayer player, string command, string[] args)
        {
            object success = LeaveEvent(player);
            if (success is string)
            {
                SendReply(player, (string)success);
                return;
            }
        }
        [ChatCommand("event_join")]
        void cmdEventJoin(BasePlayer player, string command, string[] args)
        {
            object success = JoinEvent(player);
            if (success is string)
            {
                SendReply(player, (string)success);
                return;
            }
        }

        [ChatCommand("event")]
        void cmdEvent(BasePlayer player, string command, string[] args)
        {
            string message = string.Empty;
            if (!EventOpen && !EventStarted) message = MessagesEventStatusClosedEnd;
            else if (EventOpen && !EventStarted) message = MessagesEventStatusOpen;
            else if (EventOpen && EventStarted) message = MessagesEventStatusOpenStarted;
            else message = MessagesEventStatusClosedStarted;
            SendReply(player, string.Format(message, EventGameName));
        }



        //////////////////////////////////////////////////////////////////////////////////////
        // Console Commands //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("event.launch")]
        void ccmdEventLaunch(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            object success = LaunchEvent();
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            SendReply(arg, string.Format("Event \"{0}\" is now launched.", EventGameName));
        }
        [ConsoleCommand("event.open")]
        void ccmdEventOpen(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            object success = OpenEvent();
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            SendReply(arg, string.Format("Event \"{0}\" is now opened.", EventGameName));
        }
        [ConsoleCommand("event.start")]
        void ccmdEventStart(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            object success = StartEvent();
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            SendReply(arg, string.Format("Event \"{0}\" is now started.", EventGameName));
        }
        [ConsoleCommand("event.close")]
        void ccmdEventClose(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            object success = CloseEvent();
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            SendReply(arg, string.Format("Event \"{0}\" is now closed for entries.", EventGameName));
        }
        [ConsoleCommand("event.end")]
        void ccmdEventEnd(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            object success = EndEvent();
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            SendReply(arg, string.Format("Event \"{0}\" has ended.", EventGameName));
        }
        [ConsoleCommand("event.game")]
        void ccmdEventGame(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "event.game \"Game Name\"");
                return;
            }
            object success = SelectEvent((string)arg.Args[0]);
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            defaultGame = EventGameName;
            SaveConfig();
            SendReply(arg, string.Format("{0} is now the next Event game.", arg.Args[0].ToString()));
        }
        [ConsoleCommand("event.minplayers")]
        void ccmdEventminPlayers(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "event.minplayers XX");
                return;
            }
            object success = SelectMinplayers((string)arg.Args[0]);
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            SendReply(arg, string.Format("Minimum Players for {0} is now {1} (this is only usefull for auto events).", arg.Args[0].ToString(), EventSpawnFile.ToString()));
        }
        [ConsoleCommand("event.maxplayers")]
        void ccmdEventMaxPlayers(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "event.maxplayers XX");
                return;
            }
            object success = SelectMaxplayers((string)arg.Args[0]);
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            SendReply(arg, string.Format("Maximum Players for {0} is now {1}.", arg.Args[0].ToString(), EventSpawnFile.ToString()));
        }
        [ConsoleCommand("event.spawnfile")]
        void ccmdEventSpawnfile(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "event.spawnfile \"filename\"");
                return;
            }
            object success = SelectSpawnfile((string)arg.Args[0]);
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            defaultSpawnfile = arg.Args[0];
            SaveConfig();
            SendReply(arg, string.Format("Spawnfile for {0} is now {1} .", EventGameName.ToString(), EventSpawnFile.ToString()));
        }

        [ConsoleCommand("event.kit")]
        void ccmdEventKit(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "event.kit \"kitname\"");
                return;
            }
            object success = SelectKit((string)arg.Args[0]);
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            SendReply(arg, string.Format("The new Kit for {0} is now {1}", EventGameName.ToString(), arg.Args[0]));
        }
        /*
        [ConsoleCommand("event.zone")]
        void ccmdEventZone(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            if (arg.connection == null)
            {
                SendReply(arg, "To set the zone position & radius you must be connected");
                return;
            }
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "event.zone RADIUS");
                return;
            }
            object success = SelectNewZone(arg.connection.player, arg.Args[0]);
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }

            SendReply(arg, string.Format("New Zone Created for {0}: @ {1} {2} {3} with {4}m radius .", EventGameName.ToString(), arg.connection.player.transform.position.x.ToString(), arg.connection.player.transform.position.y.ToString(), arg.connection.player.transform.position.z.ToString(), arg.Args[0]));
        }*/
        [ConsoleCommand("event.reward")]
        void ccmdEventReward(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "Reward Related: event.reward add/list/remove");
                SendReply(arg, "Players Related: event.reward set/clear/give/take/check");
                return;
            }
            string targetid = string.Empty;
            string targetname = string.Empty;
            int amount = 0;
            bool foundtarget;
            switch (arg.Args[0])
            {
                case "check":
                    if (arg.Args.Length < 2)
                    {
                        SendReply(arg, "event.reward check PLAYERNAME/STEAMID");
                        return;
                    }

                    foundtarget = FindPlayer(arg.Args[1], out targetid, out targetname);
                    if (!(bool)foundtarget)
                    {
                        SendReply(arg, targetid.ToString());
                        return;
                    }
                    SendReply(arg, string.Format("{0} {1} has {2} tokens", targetid, targetname, GetTokens(targetid).ToString()));
                    break;
                case "clear":
                    if (arg.Args.Length < 2)
                    {
                        SendReply(arg, "You must confirm by saying: event.reward clear yes");
                        return;
                    }
                    if (arg.Args[1] != "yes")
                    {
                        SendReply(arg, "You must confirm clearing the players token list by added: yes, at the end.");
                        return;
                    }
                    storedData.Tokens.Clear();
                    SendReply(arg, "Cleared all player tokens!!!!!");
                    break;
                case "set":
                    if (arg.Args.Length < 3)
                    {
                        SendReply(arg, "event.reward set PLAYERNAME/STEAMID AMOUNT");
                        return;
                    }

                    if (!int.TryParse(arg.Args[2], out amount))
                    {
                        SendReply(arg, "the amount needs to be a number");
                        return;
                    }
                    foundtarget = FindPlayer(arg.Args[1], out targetid, out targetname);
                    if (!(bool)foundtarget)
                    {
                        SendReply(arg, targetid.ToString());
                        return;
                    }
                    SetTokens(targetid, amount);
                    SendReply(arg, string.Format("{0} {1} now has {2} tokens", targetid, targetname, GetTokens(targetid).ToString()));
                    break;
                case "give":
                    if (arg.Args.Length < 3)
                    {
                        SendReply(arg, "event.reward give PLAYERNAME/STEAMID AMOUNT");
                        return;
                    }

                    if (!int.TryParse(arg.Args[2], out amount))
                    {
                        SendReply(arg, "the amount needs to be a number");
                        return;
                    }
                    foundtarget = FindPlayer(arg.Args[1], out targetid, out targetname);
                    if (!(bool)foundtarget)
                    {
                        SendReply(arg, targetid.ToString());
                        return;
                    }
                    AddTokens(targetid, amount);
                    SendReply(arg, string.Format("{0} {1} now has {2} tokens", targetid, targetname, GetTokens(targetid).ToString()));
                    break;
                case "take":
                    if (arg.Args.Length < 3)
                    {
                        SendReply(arg, "event.reward take PLAYERNAME/STEAMID AMOUNT");
                        return;
                    }

                    if (!int.TryParse(arg.Args[2], out amount))
                    {
                        SendReply(arg, "the amount needs to be a number");
                        return;
                    }
                    foundtarget = FindPlayer(arg.Args[1], out targetid, out targetname);
                    if (!(bool)foundtarget)
                    {
                        SendReply(arg, targetid.ToString());
                        return;
                    }
                    RemoveTokens(targetid, amount);
                    SendReply(arg, string.Format("{0} {1} now has {2} tokens", targetid, targetname, GetTokens(targetid).ToString()));
                    break;
                case "add":
                    if (arg.Args.Length < 5)
                    {
                        SendReply(arg, "event.reward add NAME COST ITEM/KIT AMOUNT");
                        return;
                    }
                    string rewardname = arg.Args[1];
                    int cost = 0;
                    if (!int.TryParse(arg.Args[2], out cost))
                    {
                        SendReply(arg, "The cost needs to be a number");
                        return;
                    }
                    if (cost < 1)
                    {
                        SendReply(arg, "The cost needs to be higher then 0");
                        return;
                    }

                    if (!int.TryParse(arg.Args[4], out amount))
                    {
                        SendReply(arg, "The amount needs to be a number");
                        return;
                    }
                    if (amount < 1)
                    {
                        SendReply(arg, "The amount needs to be higher then 0");
                        return;
                    }

                    bool kit = false;
                    string itemname = arg.Args[3].ToLower();
                    if (displaynameToShortname.ContainsKey(itemname))
                        itemname = displaynameToShortname[itemname];
                    var definition = ItemManager.FindItemDefinition(itemname);
                    if (definition == null)
                    {
                        kit = true;
                        if (Kits == null)
                        {
                            SendReply(arg, "This item doesn't exist and it seems like you don't have the kits plugin");
                            return;
                        }
                        var iskit = Kits.Call("isKit", itemname);
                        if (!(iskit is bool))
                        {
                            SendReply(arg, "Seems like you have an out dated Kits plugin");
                            return;
                        }
                        if (!(bool)iskit)
                        {
                            SendReply(arg, "This item doesn't exist and no kits match this name neither.");
                            return;
                        }
                    }
                    Reward reward = new Reward(rewardname, cost, kit, itemname, amount);
                    if (rewards[reward.name] != null) storedData.Rewards.Remove(rewards[reward.name]);
                    rewards[reward.name] = reward;
                    storedData.Rewards.Add(rewards[reward.name]);
                    SaveData();
                    SendReply(arg, string.Format("Reward Name: {0} - Cost: {1} - Name: {2} - Amount: {3}", reward.name, reward.cost, (Convert.ToBoolean(reward.kit) ? "Kit " : string.Empty) + reward.item, reward.amount));
                    break;

                case "list":
                    if (rewards.Count == 0)
                    {
                        SendReply(arg, "You dont have any rewards set yet.");
                        return;
                    }
                    foreach (KeyValuePair<string, Reward> pair in rewards)
                    {
                        SendReply(arg, string.Format("Reward Name: {0} - Cost: {1} - Name: {2} - Amount: {3}", pair.Value.name, pair.Value.cost, (Convert.ToBoolean(pair.Value.kit) ? "Kit " : string.Empty) + pair.Value.item, pair.Value.amount));
                    }
                    break;

                case "remove":
                    if (arg.Args.Length < 2)
                    {
                        SendReply(arg, "event.reward remove REWARDNAME");
                        return;
                    }
                    if (rewards[arg.Args[1]] == null)
                    {
                        SendReply(arg, "This reward doesn't exist");
                        return;
                    }
                    storedData.Rewards.Remove(rewards[arg.Args[1]]);
                    rewards[arg.Args[1]] = null;
                    SendReply(arg, "You've successfully removed this reward");
                    break;

            }
        }
    }
}
