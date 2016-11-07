using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;
using Oxide.Plugins;
using System;

namespace Oxide.Plugins
{
    [Info("WeaponsShownOnBack", "Jake_Rich", 0.11)]
    [Description("Shows player's best two weapons holstered on their back")]

    public class WeaponsOnBack : RustPlugin
    {
        public static WeaponsOnBack thisPlugin;

        public static int displayMode = 1; //0 = Only from player's belt
                                           //1 = From player's belt and inventory
        #region Loading / Unloading
        void Loaded()
        {
            thisPlugin = this;
            weaponData.Clear();
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                if (!weaponData.ContainsKey(player))
                {
                    weaponData.Add(player, new WeaponData(player));
                }
            }
            displayMode = (int)Config["displayMode"];

            //testing
            //ShowAllGuns(BasePlayer.activePlayerList[0], null, null);
            //SpawnPlayersTest(BasePlayer.activePlayerList[0], null, null);

        }

        void Unload()
        {
            foreach (WeaponData data in weaponData.Values)
            {
                data.Destroy();
            }
            foreach (BaseNetworkable item in fakeEntities)
            {
                if (!item.isDestroyed)
                {
                    item.Kill();
                }
            }
            foreach(BasePlayer item in fakePlayers)
            {
                if (!item.isDestroyed)
                {
                    item.Kill();
                }
            }
            foreach(BaseCorpse corpse in GameObject.FindObjectsOfType<BaseCorpse>())
            {
                if (!corpse.isDestroyed)
                {
                    corpse.Kill();
                }
            }

        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            Config.Clear();
            Config["displayMode"] = 1;
            SaveConfig();
        }
        #endregion
            
        #region Writing
        public void Output(params object[] text)
        {
            string str = "";
            for (int i = 0; i < text.Length; i++)
            {
                str += text[i].ToString() + " ";
            }
            Puts(str);
        }

        public static void Write(params object[] text)
        {
            thisPlugin.Output(text);
        }

        public static void Write(object text)
        {
            thisPlugin.Output(text);
        }

        #endregion

        public static Dictionary<BasePlayer, WeaponData> weaponData = new Dictionary<BasePlayer, WeaponData>();
        public static Dictionary<BaseEntity, BasePlayer> weaponNetworking = new Dictionary<BaseEntity, BasePlayer>(); //Stores player that owns each gun, so gun's arent networked to the player
        public static List<BaseNetworkable> fakeEntities = new List<BaseNetworkable>();
        public static List<BasePlayer> fakePlayers = new List<BasePlayer>();
        

        [ChatCommand("weapondisplay")]
       void ConfigureWeaponsSettings(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "1")
                {
                    Config["displayMode"] = 0;
                    SaveConfig();
                }
                if (args[0] == "2")
                {
                    Config["displayMode"] = 1;
                    SaveConfig();
                }
            }
        }

        //Test commands
        /*
        #region Testing Commands
        [ChatCommand("player")]
        void SpawnPlayerTest(BasePlayer player, string command, string[] args)
        {
            string prefab = "assets/prefabs/player/player.prefab";
            BasePlayer newPlayer = (BasePlayer)GameManager.server.CreateEntity(prefab, player.transform.position + new Vector3(0,0,1));
            newPlayer.Spawn();
            //newPlayer.InitializeHealth(1000, 1000); 
            newPlayer.Heal(100);
            for (int i = 0; i < player.inventory.containerBelt.itemList.Count; i++)
            {
                newPlayer.inventory.containerBelt.AddItem(player.inventory.containerBelt.itemList[i].info,1);
            }
            for (int i = 0; i < player.inventory.containerMain.itemList.Count; i++)
            {
                newPlayer.inventory.containerMain.AddItem(player.inventory.containerMain.itemList[i].info, 1);
            }
            for (int i = 0; i < player.inventory.containerWear.itemList.Count; i++)
            {
                newPlayer.inventory.containerWear.AddItem(player.inventory.containerWear.itemList[i].info, 1);
            }

            fakePlayers.Add(newPlayer);
            weaponData.Add(newPlayer, new WeaponData(newPlayer));
        }

        [ChatCommand("showall")]
        void ShowAllGuns(BasePlayer player, string command, string[] args)
        {
            int Count = 0;
            foreach(KeyValuePair<string,GunConfig> data in WeaponData.gunSettings_main)
            {
                string prefab = "assets/prefabs/player/player.prefab";
                BasePlayer newPlayer = (BasePlayer)GameManager.server.CreateEntity(prefab, player.transform.position + new Vector3(Count, 0, 1));
                newPlayer.Spawn();
                //newPlayer.InitializeHealth(1000, 1000); 
                newPlayer.Heal(100);
                newPlayer.inventory.containerBelt.AddItem(ItemManager.FindItemDefinition(data.Key), 1);
                for (int i = 0; i < player.inventory.containerWear.itemList.Count; i++)
                {
                    newPlayer.inventory.containerWear.AddItem(player.inventory.containerWear.itemList[i].info, 1);
                }
                fakePlayers.Add(newPlayer);
                weaponData.Add(newPlayer, new WeaponData(newPlayer));
                Count++;
            }
        }
        #endregion
        */

        #region Hooks

        object CanNetworkTo(BaseNetworkable entity, BasePlayer player)
        {
            if (weaponNetworking.ContainsKey((BaseEntity)entity)) //If it crashes, change it to net.ID
            {
                if (weaponNetworking[(BaseEntity)entity] == player)
                {
                    return false;
                }
            }
            return null;
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (!weaponData.ContainsKey(player))
            {
                weaponData.Add(player, new WeaponData(player));
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (weaponData.ContainsKey(player))
            {
                weaponData[player].Destroy();
                weaponData.Remove(player);
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity.GetType() == typeof(BasePlayer))
            {
                if (weaponData.ContainsKey((BasePlayer)entity))
                {
                    weaponData[(BasePlayer)entity].Destroy();
                }
            }
        } //Maybe add items to corpse instead? Could lag even worse if lots of bodies

        int playersPerTick = 1;
        int lastPlayerIndex = 0;
        DateTime lastTimeCompleted = new DateTime(); //Shouldnt update more often then once every 2 seconds
        void OnTick()
        {
            playersPerTick = (BasePlayer.activePlayerList.Count / 20) + 1;
            if (BasePlayer.activePlayerList.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                weaponData[BasePlayer.activePlayerList[i]].QuickUpdate();
            }

            for (int i = 0; i < playersPerTick; i++)
            {
                if (lastPlayerIndex >= BasePlayer.activePlayerList.Count)
                {
                    if (lastTimeCompleted.AddSeconds(2) > DateTime.Now) //Don't update players too often
                    {
                        return;
                    }
                    lastTimeCompleted = DateTime.Now.AddSeconds(2);
                    lastPlayerIndex = 0;
                }
                if (!weaponData.ContainsKey(BasePlayer.activePlayerList[lastPlayerIndex]))
                {
                    if (BasePlayer.activePlayerList[lastPlayerIndex] != null)
                    {
                        weaponData.Add(BasePlayer.activePlayerList[lastPlayerIndex], new WeaponData(BasePlayer.activePlayerList[lastPlayerIndex]));
                    }
                    else
                    {
                        continue;
                    }
                }
                weaponData[BasePlayer.activePlayerList[lastPlayerIndex]].Update();
                lastPlayerIndex++;
            }
    
        }

        /*
        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                Puts(item.info.shortname.ToString());
            }
            foreach (Item item in player.inventory.containerWear.itemList)
            {
                Puts(item.info.shortname.ToString());
            }
        }
        */
        #endregion

        public class WeaponData
        {
            #region Weapon Positions

            public static Dictionary<string, GunConfig> gunSettings_main = new Dictionary<string, GunConfig>()
    {
        //{"lr300.item", new GunConfig(new Vector3(0.0f,0.0f,0.0f),new Vector3(0,0,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/lr300/lr300.entity.prefab") },
        {"rocket.launcher", new GunConfig(new Vector3(-0.35f,-0.25f,0f),new Vector3(80,260,180), new Vector3(-0.35f,-0.25f,-0.2f), new Vector3(100,80,25), "assets/prefabs/weapons/rocketlauncher/rocket_launcher.entity.prefab",150) },
        {"rifle.lr300", new GunConfig(new Vector3(0.0f,-0.05f,0.0f),new Vector3(0,30,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/lr300/lr300.entity.prefab",110) },
        {"rifle.ak", new GunConfig(new Vector3(0.0f,-0.05f,0.0f),new Vector3(0,30,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/ak47u/ak47u.entity.prefab",100) },
        {"rifle.bolt", new GunConfig(new Vector3(0.0f,-0.06f,-0.1f),new Vector3(0,30,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/bolt rifle/bolt_rifle.entity.prefab",90) },
        {"rifle.semiauto", new GunConfig(new Vector3(0.0f,-0.08f,-0.05f),new Vector3(0,30,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/semi auto rifle/semi_auto_rifle.entity.prefab",60) },
        {"lmg.m249", new GunConfig(new Vector3(0.0f,-0.05f,-0.05f),new Vector3(0,30,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/m249/m249.entity.prefab",120) },
        {"smg.thompson", new GunConfig(new Vector3(0.0f,-0.075f,-0.05f),new Vector3(0,30,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/thompson/thompson.entity.prefab",80) },
        {"smg.mp5", new GunConfig(new Vector3(-0.1f,-0.07f,-0.03f),new Vector3(0,30,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/mp5/mp5.entity.prefab",88 ) },
        {"shotgun.pump", new GunConfig(new Vector3(0.0f,-0.085f,-0.05f),new Vector3(0,30,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/sawnoff_shotgun/shotgun_pump.entity.prefab",70) },
        {"shotgun.double", new GunConfig(new Vector3(0.0f,-0.08f,-0.05f),new Vector3(0,30,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/doubleshotgun/double_shotgun.entity.prefab",50) },
        {"crossbow", new GunConfig(new Vector3(-0.50f,-0.05f,0.1f),new Vector3(280,290,270), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/crossbow/crossbow.entity.prefab",30) },
        {"shotgun.waterpipe", new GunConfig(new Vector3(0.0f,-0.065f,0.0f),new Vector3(0,30,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/pipe shotgun/shotgun_waterpipe.entity.prefab",40) },
        {"spear.wooden", new GunConfig(new Vector3(-0.5f,-0.08f,0.0f),new Vector3(0,110,0), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/wooden spear/spear_wooden.entity.prefab",  10) },
        {"spear.stone", new GunConfig(new Vector3(-0.5f,-0.055f,0.0f),new Vector3(0,110,90), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/stone spear/spear_stone.entity.prefab",20) },
        {"bow.hunting", new GunConfig(new Vector3(-0.1f,-0f,-0.05f),new Vector3(351,65,135), new Vector3(0.0f,0.0f,0.0f), new Vector3(0,0,0), "assets/prefabs/weapons/bow/bow_hunting.entity.prefab") },
        
        
    };

            public static Dictionary<string, GunConfig> gunSettings_holster = new Dictionary<string, GunConfig>()
    {
        {"pistol.semiauto", new GunConfig(new Vector3(0f,0.05f,0.15f),new Vector3(60,180,0), new Vector3(0f,-0.05f,-0.2f), new Vector3(180,0,-10), "assets/prefabs/weapons/semi auto pistol/pistol_semiauto.entity.prefab", 20) }, //Holster
        {"pistol.revolver", new GunConfig(new Vector3(0f,0.05f,0.18f),new Vector3(80,180,0), new Vector3(0f,-0.05f,-0.2f), new Vector3(180,0,-10), "assets/prefabs/weapons/revolver/pistol_revolver.entity.prefab",10) }, //Holster as well  
    };

            public static Dictionary<string, Vector3> shirtOffsetValues = new Dictionary<string, Vector3>()
    {
        {"tshirt", new Vector3(0,-0.02f,0) },
        {"tshirt.long", new Vector3(0,-0.02f,0) },
        {"hoodie", new Vector3(0,-0.03f,0f) },
        {"burlap.shirt", new Vector3(0,-0.01f,0) },
        {"hazmat.jacket", new Vector3(0,-0.02f,0) },
        {"attire.hide.vest", new Vector3(0,-0.02f,0) },
    };

            public static Dictionary<string, Vector3> armourOffsetValues = new Dictionary<string, Vector3>()
    {
        {"roadsign.jacket", new Vector3(0,-0.035f,0) },
        {"metal.plate.torso", new Vector3(0,-0.071f,0) },
        {"attire.hide.poncho", new Vector3(0,0.04f,0) },
        {"wood.armor.jacket", new Vector3(0,0.035f,0) },
        {"jacket", new Vector3(0,-0.05f,0) },
        {"jacket.snow", new Vector3(0,-0.03f,0) },
        {"bone.armor.suit", new Vector3(0,-0.022f,0) },

    };


            #endregion

            public BasePlayer player;
            public BaseEntity mainWeapon;
            public BaseEntity sideWeapon;
            public string mainGun { get; set; }
            public string holsterGun { get; set; }
            public Item activeItem { get { return player.GetActiveItem(); } }
            public Item oldActiveItem { get; set; }

            public BaseEntity SpawnEntity(string prefab, Vector3 pos, Quaternion rotation, BaseEntity parent, ulong skin = 0)
            {
                BaseEntity entity = GameManager.server.CreateEntity(prefab, pos, rotation);
                WeaponsOnBack.weaponNetworking.Add(entity, player);
                entity.skinID = skin;
                entity.Spawn();
                entity.SetParent(parent, "spine1");
                entity.SendNetworkUpdateImmediate();
                return entity;
            }

            public WeaponData(BasePlayer player)
            {
                this.player = player;
                UpdateGuns();
            }

            public void Destroy()
            {
                DestroyWeapons();
            }

            public void DestroyWeapons()
            {
                DestroyWeapon(mainWeapon);
                DestroyWeapon(sideWeapon);
            }

            public void DestroyWeapon(BaseEntity gun)
            {
                if (gun != null)
                {
                    if (!gun.isDestroyed)
                    {
                        WeaponsOnBack.weaponNetworking.Remove(gun);
                        gun.Kill();
                    }
                }
            }

            public void NetworkUpdate()
            {

            }

            public void UpdateGuns()
            {
                if (player == null)
                {
                    return;
                }
                int mainGunValue = -1;
                Item mainGun = null;
                int holsterGunValue = -1;
                Item holsterGun = null;
                Vector3 offset = Vector3.zero;

                //Gets offset based on items worn
                foreach (Item item in player.inventory.containerWear.itemList)
                {
                    if (armourOffsetValues.ContainsKey(item.info.shortname))
                    {
                        offset = armourOffsetValues[item.info.shortname];
                        break;
                    }
                    if (shirtOffsetValues.ContainsKey(item.info.shortname))
                    {
                        offset = shirtOffsetValues[item.info.shortname];
                    }
                }

                //Checks hotbar for weapons
                foreach (Item item in player.inventory.containerBelt.itemList)
                {
                    if (/*item.info.shortname != this.mainGun*/ item.info.shortname != activeItem?.info.shortname)
                    {
                        if (gunSettings_main.ContainsKey(item.info.shortname))
                        {
                            if (mainGunValue < gunSettings_main[item.info.shortname].priority)
                            {
                                mainGunValue = gunSettings_main[item.info.shortname].priority;
                                mainGun = item;
                            }
                        }
                    }
                    if (/*item.info.shortname != this.holsterGun*/ item.info.shortname != activeItem?.info.shortname)
                    {
                        if (gunSettings_holster.ContainsKey(item.info.shortname))
                        {
                            if (holsterGunValue < gunSettings_holster[item.info.shortname].priority)
                            {
                                holsterGunValue = gunSettings_holster[item.info.shortname].priority;
                                holsterGun = item;
                            }
                        }
                    }
                }

                //Make sure same function as above, Checks inventory
                if (WeaponsOnBack.displayMode == 1)
                {
                    foreach (Item item in player.inventory.containerMain.itemList)
                    {
                        if (/*item.info.shortname != this.mainGun*/ item.info.shortname != activeItem?.info.shortname)
                        {
                            if (gunSettings_main.ContainsKey(item.info.shortname))
                            {
                                if (mainGunValue < gunSettings_main[item.info.shortname].priority)
                                {
                                    mainGunValue = gunSettings_main[item.info.shortname].priority;
                                    mainGun = item;
                                }
                            }
                        }
                        if (/*item.info.shortname != this.holsterGun*/ item.info.shortname != activeItem?.info.shortname)
                        {
                            if (gunSettings_holster.ContainsKey(item.info.shortname))
                            {
                                if (holsterGunValue < gunSettings_holster[item.info.shortname].priority)
                                {
                                    holsterGunValue = gunSettings_holster[item.info.shortname].priority;
                                    holsterGun = item;
                                }
                            }
                        }
                    }
                }

                //Spawns guns on back of player
                if (mainGun != null)
                {
                    if (mainGun.info.shortname != this.mainGun)
                    {
                        GunConfig settings = gunSettings_main[mainGun.info.shortname];
                        this.mainGun = mainGun.info.shortname;
                        DestroyWeapon(mainWeapon);
                        mainWeapon = SpawnEntity(settings.prefabName, settings.localPosition_slot1 + offset, settings.localRotation_slot1, player, mainGun.skin);
                    }
                }
                else
                {
                    DestroyWeapon(mainWeapon);
                    this.mainGun = "";
                }

                if (holsterGun != null)
                {
                    if (holsterGun.info.shortname != this.holsterGun)
                    {
                        GunConfig settings = gunSettings_holster[holsterGun.info.shortname];
                        this.holsterGun = holsterGun.info.shortname;
                        if (sideWeapon != null)
                        DestroyWeapon(sideWeapon);
                        sideWeapon = SpawnEntity(settings.prefabName, settings.localPosition_slot1, settings.localRotation_slot1, player, holsterGun.skin);
                    }
                }
                else
                {
                    DestroyWeapon(sideWeapon);
                    this.holsterGun = "";
                }
            }

            public void QuickUpdate()
            {
                if (player.GetActiveItem() != oldActiveItem)
                {
                    oldActiveItem = player.GetActiveItem();
                    UpdateGuns();
                    return;
                }
            }

            public void Update()
            {

                    /*if (mainWeapon != null)
                    {
                        //mainWeapon.transform.localRotation *= Quaternion.Euler(0f, 0f, -1f);
                        mainWeapon.SendNetworkUpdateImmediate();
                        //Write("Rotating", mainWeapon.transform.localRotation.eulerAngles);
                    }*/
                    UpdateGuns();
            }
        }

        public class GunConfig
        {
            public Vector3 localPosition_slot1 { get; set; }
            public Quaternion localRotation_slot1 { get; set; }
            public Vector3 localPosition_slot2 { get; set; }
            public Quaternion localRotation_slot2 { get; set; }
            public int priority { get; set; }
            public string prefabName { get; set; }

            public GunConfig(Vector3 pos, Vector3 rotation, Vector3 slot2pos, Vector3 slot2rot, string prefab, int priority = 0)
            {
                localPosition_slot1 = pos;
                localRotation_slot1 = Quaternion.Euler(rotation);
                localPosition_slot2 = slot2pos;
                localRotation_slot2 = Quaternion.Euler(slot2rot);
                prefabName = prefab;
                this.priority = priority;
            }
        }


    }
}

