using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Facepunch;
using System;

namespace Oxide.Plugins
{
    [Info("SmartHomes", "k1lly0u & DylanSMR", "0.1.3", ResourceId = 2051)]
    class SmartHomes : RustPlugin
    {
        // / ////// / //   
        // / Fields / //
        // / ////// / //    
        static MethodInfo updatelayer;

        List<ulong> setupUI = new List<ulong>();
        List<ulong> barUI = new List<ulong>();
        List<ulong> isEditing = new List<ulong>();
        Dictionary<ulong, newData> newD = new Dictionary<ulong, newData>();
        class newData
        {
            public uint entKey;
            public string entName;
            public bool entStatus;
            public string entType;
            public Vector3 entLocation;

            public newData(){ }
        }

        // / ////// / //   
        // / OxideH / //
        // / ////// / //    
        void Loaded()
        {
            homeData = Interface.GetMod().DataFileSystem.ReadObject<HomeData>(this.Title);
            lang.RegisterMessages(messages, this);
        }
        void Unload()
        {
            foreach(var player in BasePlayer.activePlayerList){
            CuiHelper.DestroyUi(player, PublicSideBar);
            CuiHelper.DestroyUi(player, PublicSetupName);
            CuiHelper.DestroyUi(player, PublicObjectSetup);
            CuiHelper.DestroyUi(player, PublicControlSetup);
            CuiHelper.DestroyUi(player, PublicControlSetupTurret);
            CuiHelper.DestroyUi(player, PublicControlSetupDoor);
            CuiHelper.DestroyUi(player, PublicControlSetupLight);
            barUI.Remove(player.userID);
            setupUI.Remove(player.userID);}
            SaveData();
        }
        void OnServerSave()
        {
            SaveData();
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (Homes.Find(player) == null){
                var info = new Homes()
                {
                    locx = 0.0f,
                    locy = 0.0f,
                    locz = 0.0f,
                    playerID = player.userID
                };
                homeData.homeD.Add(player.userID, info);
                SaveData();  
            }        
        }
        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(this.Title, homeData);
        void OnServerInitialized()
        {
            updatelayer = typeof(BuildingBlock).GetMethod("UpdateLayer", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            LoadVariables();
        }   
        // / ////// / //   
        // / data s / //
        // / ////// / // 

        static HomeData homeData;

        class HomeData
        {
            public Dictionary<ulong, Homes> homeD = new Dictionary<ulong, Homes>();
        }

        class Homes
        {
            public float locx;
            public float locy;
            public float locz;

            public ulong playerID;

            public Dictionary<string, TurretData> tData = new Dictionary<string, TurretData>();
            public Dictionary<string, LightData> lData = new Dictionary<string, LightData>();
            public Dictionary<string, DoorData> dData = new Dictionary<string, DoorData>();

            public List<float> objectX = new List<float>();
            public Homes(){}
            internal static Homes Find(BasePlayer player)
            {
                return homeData.homeD.Values.ToList().Find((d) => d.playerID == player.userID);
            }
        }       
        public class TurretData
        {   
            public float locx;
            public float locy;
            public float locz;
            public uint key;
            public bool status;
            public string name;
            public TurretData(){}
        }
        public class LightData
        {
            public float locx;
            public float locy;
            public float locz;
            public uint key;
            public bool status;
            public string name;
            public LightData(){}
        }
        public class DoorData
        {
            public float locx;
            public float locy;
            public float locz;
            public uint key;
            public bool status;
            public string name;
            public DoorData(){}
        }

        // / ////// / //   
        // / Config / //
        // / ////// / //       
        private ConfigData configData;
        class Options
        {
            public float ActivationDistance { get; set; }
        }
        class ConfigData
        {
            public Options Options { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                Options = new Options
                {
                    ActivationDistance = 30
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        // / ////// / //   
        // / GUIMai / //
        // / ////// / //    

        [ConsoleCommand("CloseCUIMain")]
        private void destroyUI(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CuiHelper.DestroyUi(player, PublicSideBar);
            CuiHelper.DestroyUi(player, PublicSetupName);
            CuiHelper.DestroyUi(player, PublicObjectSetup);
            CuiHelper.DestroyUi(player, PublicControlSetup);
            CuiHelper.DestroyUi(player, PublicControlSetupTurret);
            CuiHelper.DestroyUi(player, PublicControlSetupDoor);
            CuiHelper.DestroyUi(player, PublicControlSetupLight);
            barUI.Remove(player.userID);
            setupUI.Remove(player.userID);
        }

        void destroyUIN(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;          
            CuiHelper.DestroyUi(player, PublicSetupName);
            CuiHelper.DestroyUi(player, PublicObjectSetup);
            CuiHelper.DestroyUi(player, PublicControlSetup);
            CuiHelper.DestroyUi(player, PublicControlSetupTurret);
            CuiHelper.DestroyUi(player, PublicControlSetupDoor);
            CuiHelper.DestroyUi(player, PublicControlSetupLight);
            barUI.Remove(player.userID);
            setupUI.Remove(player.userID);  
        }

        [ChatCommand("rem")]
        void openUI(BasePlayer player)
        {
            barUI.Add(player.userID);
            RemoteBar(player);
        }   

        // / ////// / //   
        // / GUI Co / //
        // / ////// / //    

        [ConsoleCommand("CUI_ControlMenu")]
        void ControlMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
                destroyUIN(arg);

            var element = UI.CreateElementContainer(PublicControlSetup, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicControlSetup, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicControlSetup, UIColors["header"], lang.GetMessage("CtrlMenu", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicControlSetup, UIColors["dark"], lang.GetMessage("SelectMenu", this, player.UserIDString), 20, "0 .9", "1 1");
            UI.CreatePanel(ref element, PublicControlSetup, UIColors["dark"], "0.14 0.05", "0.38 0.15", true);
            UI.CreateButton(ref element, PublicControlSetup, UIColors["buttongreen"], lang.GetMessage("OpenTurret", this, player.UserIDString), 20, "0.15 0.06", "0.37 0.14", $"CUI_ControlOpen Turret");
            UI.CreatePanel(ref element, PublicControlSetup, UIColors["dark"], "0.39 0.05", "0.63 0.15", true);
            UI.CreateButton(ref element, PublicControlSetup, UIColors["buttongreen"], lang.GetMessage("OpenLight", this, player.UserIDString), 20, "0.40 0.06", "0.62 0.14", $"CUI_ControlOpen Light");
            UI.CreatePanel(ref element, PublicControlSetup, UIColors["dark"], "0.64 0.05", "0.88 0.15", true);
            UI.CreateButton(ref element, PublicControlSetup, UIColors["buttongreen"], lang.GetMessage("OpenDoor", this, player.UserIDString), 20, "0.65 0.06", "0.87 0.14", $"CUI_ControlOpen Door");
            CuiHelper.AddUi(player, element);    
        } 

        [ConsoleCommand("CUI_ControlOpen")]
        void ControlOpen(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;  
            switch(arg.Args[0])
            {
                case "Turret":
                    destroyUIN(arg);
                    player.SendConsoleCommand("CUI_OpenTurret");
                break;
                case "Light":
                    destroyUIN(arg);
                    player.SendConsoleCommand("CUI_OpenLight");
                break;
                case "Door":
                    destroyUIN(arg);
                    player.SendConsoleCommand("CUI_OpenDoor");
                break;
            }
        } 

        [ConsoleCommand("CUI_ChangeElement")]
        private void ChangeElement(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var panelName = arg.GetString(0);
            switch (panelName)
            {
                case "listpage":
                    {
                        if(arg.GetString(1) == "turret"){
                            var pageNumber = arg.GetString(2);
                            ControlTurret(arg, int.Parse(pageNumber));
                        }
                        if(arg.GetString(1) == "light"){
                            var pageNumber = arg.GetString(2);
                            ControlLight(arg, int.Parse(pageNumber));
                        }
                        if(arg.GetString(1) == "door"){
                            var pageNumber = arg.GetString(2);
                            ControlDoor(arg, int.Parse(pageNumber));
                        }
                    }
                    return;
            }
        }

        [ConsoleCommand("CUI_OpenTurret")]
        void ControlTurret(ConsoleSystem.Arg arg, int page = 0)
        {
            destroyUIN(arg);
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;   

            var i = homeData.homeD[player.userID].tData.Count;
            var element = UI.CreateElementContainer(PublicControlSetupTurret, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicControlSetupTurret, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicControlSetupTurret, UIColors["header"], lang.GetMessage("TList", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            if(i >= 18){ 
                var maxpages = (i - 1) / 18 + 1;
                if (page < maxpages - 1){
                    UI.CreatePanel(ref element, PublicControlSetupTurret, UIColors["dark"], "0.64 0.05", "0.88 0.15", true); 
                    UI.CreateButton(ref element, PublicControlSetupTurret, UIColors["buttongreen"], lang.GetMessage("Next", this, player.UserIDString), 20, "0.65 0.06", "0.87 0.14", $"CUI_ChangeElement listpage turret {page + 1}"); 
                }
                if(page > 0){
                    UI.CreatePanel(ref element, PublicControlSetupTurret, UIColors["dark"], "0.14 0.05", "0.38 0.15", true);
                    UI.CreateButton(ref element, PublicControlSetupTurret, UIColors["buttongreen"], lang.GetMessage("Back", this, player.UserIDString), 20, "0.15 0.06", "0.37 0.14", $"CUI_ChangeElement listpage turret {page - 1}");
                }
            }

            int maxentries = (18 * (page + 1));
            if (maxentries > i)
                maxentries = i;

            int rewardcount = 18 * page;

            var k = 0;
            var entries = homeData.homeD[player.userID].tData;

            List <string> questNames = new List<string>();
            foreach (var entry in homeData.homeD[player.userID].tData)
                questNames.Add(entry.Key);

            for (int n = rewardcount; n < maxentries; n++)
            {                
                CreateTurretButton(ref element, PublicControlSetupTurret, entries[questNames[n]], player, k); k++;
            }
            CuiHelper.AddUi(player, element);
        } 

        private void CreateTurretButton(ref CuiElementContainer container, string panelName, TurretData data, BasePlayer player, int num)
        {

            string name = homeData.homeD[player.userID].tData[data.name].name;
            var status = "<color='#818884'>Disabled</color>";
            if(homeData.homeD[player.userID].tData[name].status) status = "<color='#818884'>Enabled</color>";
            else status = "<color='#818884'>Disabled</color>";
            var color = "";
            if(homeData.homeD[player.userID].tData[data.name].status) color = "0.06 0.47 0.39 1.0";
            else color = "0.91 0.0 0.0 1.0";
            string cmd = $"CUI_ToggleTurret {homeData.homeD[player.userID].tData[data.name].name}";
            var pos = CalcButtonPos(num);
            UI.CreatePanel(ref container, panelName, UIColors["dark"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", true);
            UI.CreateButton(ref container, panelName, color, $"<color='#818884'>{name}</color>\n{status}", 13, $"{pos[0] + 0.01} {pos[1] + 0.01}", $"{pos[2] - 0.01} {pos[3] - 0.01}", cmd);
        }

        [ConsoleCommand("CUI_ToggleTurret")]
        void toggle(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            var name = arg.Args[0];
            if (player == null)
                return;   
            if(homeData.homeD[player.userID].tData[name].status)
            {
                var newLocation = new Vector3(homeData.homeD[player.userID].tData[name].locx, homeData.homeD[player.userID].tData[name].locy, homeData.homeD[player.userID].tData[name].locz);
                homeData.homeD[player.userID].tData[name].status = false;
                    List<BaseEntity> turretnear= new List<BaseEntity>();
                    Vis.Entities(newLocation, 0.2f, turretnear);
                var i = 0;
                foreach(var turret in turretnear)
                {
                    if(turret.ToString().Contains("turret")){
                        if (turret is AutoTurret) turret.GetComponent<AutoTurret>().target = null;
                        i++;
                        turret.SetFlag(BaseEntity.Flags.On, false);
                        turret.SendNetworkUpdateImmediate();
                    }else{}
                }
                if(i == 0)
                {
                    homeData.homeD[player.userID].objectX.Remove(homeData.homeD[player.userID].tData[name].locx);
                    homeData.homeD[player.userID].tData.Remove(name);
                    SaveData();
                    SendReply(player, lang.GetMessage("LostObject", this, player.UserIDString).Replace("0", name).Replace("1", "World"));
                }
            }
            else
            {
                var newLocation = new Vector3(homeData.homeD[player.userID].tData[name].locx, homeData.homeD[player.userID].tData[name].locy, homeData.homeD[player.userID].tData[name].locz);
                homeData.homeD[player.userID].tData[name].status = true;
                    List<BaseEntity> turretnear= new List<BaseEntity>();
                    Vis.Entities(newLocation, 0.2f, turretnear);
                var i = 0;
                foreach(var turret in turretnear)
                {
                    if(turret.ToString().Contains("turret")){
                        if (turret is AutoTurret) turret.GetComponent<AutoTurret>().target = null;
                        i++;
                        turret.SetFlag(BaseEntity.Flags.On, true);
                        turret.SendNetworkUpdateImmediate();
                    }else{}
                }
                if(i == 0)
                {
                    homeData.homeD[player.userID].objectX.Remove(homeData.homeD[player.userID].tData[name].locx);
                    homeData.homeD[player.userID].tData.Remove(name);
                    SaveData();
                    SendReply(player, lang.GetMessage("LostObject", this, player.UserIDString).Replace("0", name).Replace("1", "World"));
                }       
            }
            ControlTurret(arg);
            SaveData();
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if(entity is BasePlayer) return;
            if(entity.OwnerID == null) return;
            if(entity.ToString().Contains("autoturret"))
            {
                foreach(var entry in homeData.homeD[entity.OwnerID].tData)
                {
                    if(homeData.homeD[entity.OwnerID].tData[entry.Key] == null) return;
                    if(homeData.homeD[entity.OwnerID].tData[entry.Key].locx == entity.transform.position.x)
                    {
                        var namen = homeData.homeD[entity.OwnerID].tData[entry.Key].name;
                        homeData.homeD[entity.OwnerID].objectX.Remove(entity.transform.position.x);
                        homeData.homeD[entity.OwnerID].tData.Remove(homeData.homeD[entity.OwnerID].tData[entry.Key].name);
                        SaveData();
                        SendReply(BasePlayer.FindByID(entity.OwnerID), lang.GetMessage("LostObject", this, BasePlayer.FindByID(entity.OwnerID).UserIDString).Replace("0", namen).Replace("1", info.Initiator.ToPlayer().displayName));
                        return;
                    }
                    else continue;
                }
            }
            else if(entity.ToString().Contains("hinged"))
            {
                foreach(var entry2 in homeData.homeD[entity.OwnerID].dData)
                {
                    if(homeData.homeD[entity.OwnerID].dData[entry2.Key] == null) return;
                    if(homeData.homeD[entity.OwnerID].dData[entry2.Key].locx == entity.transform.position.x)
                    {
                        var namenew = homeData.homeD[entity.OwnerID].dData[entry2.Key].name;
                        homeData.homeD[entity.OwnerID].objectX.Remove(entity.transform.position.x);
                        homeData.homeD[entity.OwnerID].dData.Remove(homeData.homeD[entity.OwnerID].dData[entry2.Key].name);
                        SaveData();
                        SendReply(BasePlayer.FindByID(entity.OwnerID), lang.GetMessage("LostObject", this, BasePlayer.FindByID(entity.OwnerID).UserIDString).Replace("0", namenew).Replace("1", info.Initiator.ToPlayer().displayName));
                        return;
                    }
                    else continue;
                }
            }
            else if(entity.ToString().Contains("lantern") || entity.ToString().Contains("ceilinglight"))
            {
                foreach(var entry3 in homeData.homeD[entity.OwnerID].lData)
                {
                    if(homeData.homeD[entity.OwnerID].lData[entry3.Key] == null) return;
                    if(homeData.homeD[entity.OwnerID].lData[entry3.Key].locx == entity.transform.position.x)
                    {
                        var namenew = homeData.homeD[entity.OwnerID].lData[entry3.Key].name;
                        homeData.homeD[entity.OwnerID].objectX.Remove(entity.transform.position.x);
                        homeData.homeD[entity.OwnerID].lData.Remove(homeData.homeD[entity.OwnerID].lData[entry3.Key].name);
                        SaveData();
                        SendReply(BasePlayer.FindByID(entity.OwnerID), lang.GetMessage("LostObject", this, BasePlayer.FindByID(entity.OwnerID).UserIDString).Replace("0", namenew).Replace("1", info.Initiator.ToPlayer().displayName));
                        return;
                    }
                    else continue;
                }
            }
            else return;
        }

        // / ////// / //   
        // / Light / //
        // / ////// / //   

        [ConsoleCommand("CUI_OpenLight")]
        void ControlLight(ConsoleSystem.Arg arg, int page = 0)
        {
            destroyUIN(arg);
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;   

            var i = homeData.homeD[player.userID].lData.Count;
            var element = UI.CreateElementContainer(PublicControlSetupLight, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicControlSetupLight, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicControlSetupLight, UIColors["header"], lang.GetMessage("LList", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            if(i >= 18){ 
                var maxpages = (i - 1) / 18 + 1;
                if (page < maxpages - 1){
                    UI.CreatePanel(ref element, PublicControlSetupLight, UIColors["dark"], "0.64 0.05", "0.88 0.15", true); 
                    UI.CreateButton(ref element, PublicControlSetupLight, UIColors["buttongreen"], lang.GetMessage("Next", this, player.UserIDString), 20, "0.65 0.06", "0.87 0.14", $"CUI_ChangeElement listpage light {page + 1}"); 
                }
                if(page > 0){
                    UI.CreatePanel(ref element, PublicControlSetupLight, UIColors["dark"], "0.14 0.05", "0.38 0.15", true);
                    UI.CreateButton(ref element, PublicControlSetupLight, UIColors["buttongreen"], lang.GetMessage("Back", this, player.UserIDString), 20, "0.15 0.06", "0.37 0.14", $"CUI_ChangeElement listpage light {page - 1}");
                }
            }

            int maxentries = (18 * (page + 1));
            if (maxentries > i)
                maxentries = i;

            int rewardcount = 18 * page;

            var k = 0;
            var entries2 = homeData.homeD[player.userID].lData;

            List <string> questNames = new List<string>();
            foreach (var entry in homeData.homeD[player.userID].lData)
                questNames.Add(entry.Key);

            for (int n = rewardcount; n < maxentries; n++)
            {                
                CreateLightButton(ref element, PublicControlSetupLight, entries2[questNames[n]], player, k); k++;
            }
            CuiHelper.AddUi(player, element);
        } 

        private void CreateLightButton(ref CuiElementContainer container, string panelName, LightData light, BasePlayer player, int num)
        {
            string name = light.name;
            var status = "<color='#818884'>Disabled</color>";
            if(homeData.homeD[player.userID].lData[name].status) status = "<color='#818884'>Enabled</color>";
            else status = "<color='#818884'>Disabled</color>";
            var color = "";
            if(homeData.homeD[player.userID].lData[light.name].status) color = "0.06 0.47 0.39 1.0";
            else color = "0.91 0.0 0.0 1.0";
            string cmd = $"CUI_ToggleLight {homeData.homeD[player.userID].lData[light.name].name}";
            var pos = CalcButtonPos(num);
            UI.CreatePanel(ref container, panelName, UIColors["dark"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", true);
            UI.CreateButton(ref container, panelName, color, $"<color='#818884'>{name}</color>\n{status}", 13, $"{pos[0] + 0.01} {pos[1] + 0.01}", $"{pos[2] - 0.01} {pos[3] - 0.01}", cmd);          
        }

        [ConsoleCommand("CUI_ToggleLight")]
        void toggleLight(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            var name = arg.Args[0];
            if (player == null)
                return;   
            if(homeData.homeD[player.userID].lData[name].status)
            {
                var newLocation = new Vector3(homeData.homeD[player.userID].lData[name].locx, homeData.homeD[player.userID].lData[name].locy, homeData.homeD[player.userID].lData[name].locz);
                homeData.homeD[player.userID].lData[name].status = false;
                    List<BaseEntity> lightnear = new List<BaseEntity>();
                    Vis.Entities(newLocation, 0.2f, lightnear);
                var i = 0;
                foreach(var light in lightnear)
                {
                    if(light.ToString().Contains("lantern") || light.ToString().Contains("ceilinglight")){
                        i++;
                        light.SetFlag(BaseEntity.Flags.On, false);
                        light.SendNetworkUpdateImmediate();}
                    else{}
                }
                if(i == 0)
                {
                    homeData.homeD[player.userID].objectX.Remove(homeData.homeD[player.userID].lData[name].locx);
                    homeData.homeD[player.userID].lData.Remove(name);
                    SaveData();
                    SendReply(player, lang.GetMessage("LostObject", this, player.UserIDString).Replace("0", name).Replace("1", "World"));
                }  
            }
            else
            {
                var newLocation = new Vector3(homeData.homeD[player.userID].lData[name].locx, homeData.homeD[player.userID].lData[name].locy, homeData.homeD[player.userID].lData[name].locz);
                homeData.homeD[player.userID].lData[name].status = true;
                    List<BaseEntity> lightnear = new List<BaseEntity>();
                    Vis.Entities(newLocation, 0.2f, lightnear);
                var i = 0;
                foreach(var light in lightnear)
                {
                    if(light.ToString().Contains("lantern") || light.ToString().Contains("ceilinglight")){
                        i++;
                        light.SetFlag(BaseEntity.Flags.On, true);
                        light.SendNetworkUpdateImmediate();}
                    else{}
                }
                if(i == 0)
                {
                    homeData.homeD[player.userID].objectX.Remove(homeData.homeD[player.userID].lData[name].locx);
                    homeData.homeD[player.userID].lData.Remove(name);
                    SaveData();
                    SendReply(player, lang.GetMessage("LostObject", this, player.UserIDString).Replace("0", name).Replace("1", "World"));
                }
            }
            ControlLight(arg);
            SaveData();
        }

        // / ////// / //   
        // / Doors / //
        // / ////// / //   

        [ConsoleCommand("CUI_OpenDoor")]
        void ControlDoor(ConsoleSystem.Arg arg, int page = 0)
        {
            destroyUIN(arg);
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;   

            var i = homeData.homeD[player.userID].dData.Count;
            var element = UI.CreateElementContainer(PublicControlSetupDoor, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicControlSetupDoor, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicControlSetupDoor, UIColors["header"], lang.GetMessage("LList", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            if(i >= 18){ 
                var maxpages = (i - 1) / 18 + 1;
                if (page < maxpages - 1){
                    UI.CreatePanel(ref element, PublicControlSetupDoor, UIColors["dark"], "0.64 0.05", "0.88 0.15", true); 
                    UI.CreateButton(ref element, PublicControlSetupDoor, UIColors["buttongreen"], lang.GetMessage("Next", this, player.UserIDString), 20, "0.65 0.06", "0.87 0.14", $"CUI_ChangeElement listpage door{page + 1}"); 
                }
                if(page > 0){
                    UI.CreatePanel(ref element, PublicControlSetupDoor, UIColors["dark"], "0.14 0.05", "0.38 0.15", true);
                    UI.CreateButton(ref element, PublicControlSetupDoor, UIColors["buttongreen"], lang.GetMessage("Back", this, player.UserIDString), 20, "0.15 0.06", "0.37 0.14", $"CUI_ChangeElement listpage door {page - 1}");
                }
            }

            int maxentries = (18 * (page + 1));
            if (maxentries > i)
                maxentries = i;

            int rewardcount = 18 * page;

            var k = 0;
            var entries3 = homeData.homeD[player.userID].dData;

            List <string> questNames = new List<string>();
            foreach (var entry in homeData.homeD[player.userID].dData)
                questNames.Add(entry.Key);

            for (int n = rewardcount; n < maxentries; n++)
            {                
                CreateDoorButton(ref element, PublicControlSetupDoor, entries3[questNames[n]], player, k); k++;
            }
            CuiHelper.AddUi(player, element);
        } 

        private void CreateDoorButton(ref CuiElementContainer container, string panelName, DoorData door, BasePlayer player, int num)
        {
            string name = door.name;
            var status = "<color='#818884'>Disabled</color>";
            if(homeData.homeD[player.userID].dData[name].status) status = "<color='#818884'>Enabled</color>";
            else status = "<color='#818884'>Disabled</color>";
            var color = "";
            if(homeData.homeD[player.userID].dData[door.name].status) color = "0.06 0.47 0.39 1.0";
            else color = "0.91 0.0 0.0 1.0";
            string cmd = $"CUI_ToggleDoor {homeData.homeD[player.userID].dData[door.name].name}";
            var pos = CalcButtonPos(num);
            UI.CreatePanel(ref container, panelName, UIColors["dark"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", true);
            UI.CreateButton(ref container, panelName, color, $"<color='#818884'>{name}</color>\n{status}", 13, $"{pos[0] + 0.01} {pos[1] + 0.01}", $"{pos[2] - 0.01} {pos[3] - 0.01}", cmd);
        }

        [ConsoleCommand("CUI_ToggleDoor")]
        void toggleDoor(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            var name = arg.Args[0];
            if (player == null)
                return;   
            if(homeData.homeD[player.userID].dData[name].status)
            {
                var newLocation = new Vector3(homeData.homeD[player.userID].dData[name].locx, homeData.homeD[player.userID].dData[name].locy, homeData.homeD[player.userID].dData[name].locz);
                homeData.homeD[player.userID].dData[name].status = false;
                    List<BaseEntity> doornear = new List<BaseEntity>();
                    Vis.Entities(newLocation, 1.0f, doornear);
                var i = 0;
                foreach(var door in doornear)
                {
                    if (door.ToString().Contains("hinged")){
                    i++;
                    door.SetFlag(BaseEntity.Flags.Open, false);
                    door.SendNetworkUpdateImmediate();}
                    else{}
                }
                if(i == 0)
                {
                    homeData.homeD[player.userID].objectX.Remove(homeData.homeD[player.userID].dData[name].locx);
                    homeData.homeD[player.userID].dData.Remove(name);
                    SaveData();
                    SendReply(player, lang.GetMessage("LostObject", this, player.UserIDString).Replace("0", name).Replace("1", "World"));
                }
            }
            else
            {
                var newLocation = new Vector3(homeData.homeD[player.userID].dData[name].locx, homeData.homeD[player.userID].dData[name].locy, homeData.homeD[player.userID].dData[name].locz);
                homeData.homeD[player.userID].dData[name].status = true;
                    List<BaseEntity> doornear = new List<BaseEntity>();
                    Vis.Entities(newLocation, 1.0f, doornear);
                var i = 0;
                foreach(var door in doornear)
                {
                    if (door.ToString().Contains("hinged")){
                    i++;
                    door.SetFlag(BaseEntity.Flags.Open, true);
                    door.SendNetworkUpdateImmediate();}
                    else{}
                }
                if(i == 0)
                {
                    homeData.homeD[player.userID].objectX.Remove(homeData.homeD[player.userID].dData[name].locx);
                    homeData.homeD[player.userID].dData.Remove(name);
                    SaveData();
                    SendReply(player, lang.GetMessage("LostObject", this, player.UserIDString).Replace("0", name).Replace("1", "World"));
                }
            }
            ControlDoor(arg);
            SaveData();
        }

        // / ////// / //   
        // / calcbt / //
        // / ////// / //   

        private float[] CalcButtonPos(int number)
        {
            Vector2 position = new Vector2(0.05f, 0.8f);
            Vector2 dimensions = new Vector2(0.125f, 0.125f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 6)
            {
                offsetX = (0.03f + dimensions.x) * number;
            }
            if (number > 5 && number < 12)
            {
                offsetX = (0.03f + dimensions.x) * (number - 6);
                offsetY = (-0.05f - dimensions.y) * 1;
            }
            if (number > 11 && number < 18)
            {
                offsetX = (0.03f + dimensions.x) * (number - 12);
                offsetY = (-0.05f - dimensions.y) * 2;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        // / ////// / //   
        // / GUIOth / //
        // / ////// / //    

        [ConsoleCommand("CUI_Homes")]
        void SetUP(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (!player.CanBuild()){
                SendReply(player, lang.GetMessage("BuildingAuth", this, player.UserIDString));
                return;
            }
            if(setupUI.Contains(player.userID)){
                destroyUIN(arg);
                setupUI.Remove(player.userID);}
            setupUI.Add(player.userID);

            var element = UI.CreateElementContainer(PublicSetupName, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicSetupName, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicSetupName, UIColors["header"], lang.GetMessage("SHomes", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicSetupName, UIColors["dark"], lang.GetMessage("WalkThrough", this, player.UserIDString), 20, "0 .9", "1 1");
            UI.CreatePanel(ref element, PublicSetupName, UIColors["dark"], "0.39 0.05", "0.63 0.15", true);
            UI.CreateButton(ref element, PublicSetupName, UIColors["buttongreen"], lang.GetMessage("BeginProcess", this, player.UserIDString), 20, "0.40 0.06", "0.62 0.14", $"CUI_Homesetup2");
            CuiHelper.AddUi(player, element);    
        } 

        [ConsoleCommand("CUI_HomesNew")]
        void SetUPB(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (!player.CanBuild()){
                SendReply(player, lang.GetMessage("BuildingAuth", this, player.UserIDString).Replace("0", configData.Options.ActivationDistance.ToString()));
                return;
            }
            var i = 0;
            List<BaseEntity> nearby = new List<BaseEntity>();
            Vis.Entities(player.transform.position, configData.Options.ActivationDistance, nearby);
            foreach(BaseEntity entity in nearby)
            {
                if (entity is BuildingPrivlidge)
                { 
                    List<string> authedPlayers = new List<string>();
                    var tc = entity.GetComponent<BuildingPrivlidge>();       
                    if (tc != null)
                    {
                        if(tc.IsAuthed(player)) break;
                        else
                        {
                            SendReply(player, lang.GetMessage("BuildingAuth", this, player.UserIDString).Replace("0", configData.Options.ActivationDistance.ToString()));
                            return;       
                        }
                    }
                }
            }
            if(setupUI.Contains(player.userID)){
                destroyUIN(arg);
                setupUI.Remove(player.userID);}
            setupUI.Add(player.userID);

            var element = UI.CreateElementContainer(PublicSetupName, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicSetupName, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicSetupName, UIColors["header"], lang.GetMessage("SHomes", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicSetupName, UIColors["dark"], lang.GetMessage("WalkThroughNew", this, player.UserIDString), 20, "0 .9", "1 1");
            UI.CreatePanel(ref element, PublicSetupName, UIColors["dark"], "0.39 0.05", "0.63 0.15", true);
            UI.CreateButton(ref element, PublicSetupName, UIColors["buttongreen"], lang.GetMessage("BeginProcessNew", this, player.UserIDString), 20, "0.40 0.06", "0.62 0.14", $"CUI_HomeNew2");
            CuiHelper.AddUi(player, element);    
        } 

        [ConsoleCommand("CUI_HomeNew2")]
        void SetUP2B(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
                destroyUIN(arg);

            var element = UI.CreateElementContainer(PublicSetupName, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicSetupName, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicSetupName, UIColors["header"], lang.GetMessage("SHomes", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicSetupName, UIColors["dark"], lang.GetMessage("StandWhereNew", this, player.UserIDString), 20, "0 .9", "1 1");
            UI.CreatePanel(ref element, PublicSetupName, UIColors["dark"], "0.39 0.05", "0.63 0.15", true);
            UI.CreateButton(ref element, PublicSetupName, UIColors["buttongreen"], lang.GetMessage("NextRe", this, player.UserIDString), 20, "0.40 0.06", "0.62 0.14", $"CUI_HomeNew3");
            CuiHelper.AddUi(player, element);    
        } 

        [ConsoleCommand("CUI_Homesetup2")]
        void SetUP2(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
                destroyUIN(arg);

            var element = UI.CreateElementContainer(PublicSetupName, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicSetupName, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicSetupName, UIColors["header"], lang.GetMessage("SHomes", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicSetupName, UIColors["dark"], lang.GetMessage("StandWhere", this, player.UserIDString), 20, "0 .9", "1 1");
            UI.CreatePanel(ref element, PublicSetupName, UIColors["dark"], "0.39 0.05", "0.63 0.15", true);
            UI.CreateButton(ref element, PublicSetupName, UIColors["buttongreen"], lang.GetMessage("NextNext", this, player.UserIDString), 20, "0.40 0.06", "0.62 0.14", $"CUI_Homesetup3");
            CuiHelper.AddUi(player, element);    
        } 

        [ConsoleCommand("CUI_HomeNew3")]
        void SetUP3B(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
                destroyUIN(arg);
            Homes playerData = Homes.Find(player);
            playerData.locx = player.transform.position.x;
            playerData.locy = player.transform.position.y;
            playerData.locz = player.transform.position.z;

            playerData.tData.Clear();
            playerData.dData.Clear();
            playerData.lData.Clear();
            playerData.objectX.Clear();

            SaveData();

            var element = UI.CreateElementContainer(PublicSetupName, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicSetupName, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicSetupName, UIColors["header"], lang.GetMessage("SHomes", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicSetupName, UIColors["dark"], lang.GetMessage("SmartHomeNew", this, player.UserIDString).Replace("0", configData.Options.ActivationDistance.ToString()), 20, "0 .9", "1 1");

            CuiHelper.DestroyUi(player, PublicSideBar);
            barUI.Remove(player.userID);
            barUI.Add(player.userID);
            RemoteBar(player);

            CuiHelper.AddUi(player, element);    
        } 

        [ConsoleCommand("CUI_Homesetup3")]
        void SetUP3(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
                destroyUIN(arg);
            Homes playerData = Homes.Find(player);
            playerData.locx = player.transform.position.x;
            playerData.locy = player.transform.position.y;
            playerData.locz = player.transform.position.z;
            SaveData();

            var element = UI.CreateElementContainer(PublicSetupName, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicSetupName, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicSetupName, UIColors["header"], "Smart Homes", 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            CuiHelper.DestroyUi(player, PublicSideBar);
            barUI.Remove(player.userID);
            barUI.Add(player.userID);
            RemoteBar(player);

            UI.CreateLabel(ref element, PublicSetupName, UIColors["dark"], lang.GetMessage("SmartHome", this, player.UserIDString).Replace("0", configData.Options.ActivationDistance.ToString()), 20, "0 .9", "1 1");
            CuiHelper.AddUi(player, element);    
        } 
        [ConsoleCommand("CUI_ObjectSetup")]
        void ObjectSetup(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (!player.CanBuild()){
                SendReply(player, lang.GetMessage("BuildingAuth", this, player.UserIDString));
                return;
            }
                destroyUIN(arg);

            var element = UI.CreateElementContainer(PublicObjectSetup, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicObjectSetup, UIColors["header"], lang.GetMessage("ObjConfig", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicObjectSetup, UIColors["dark"], lang.GetMessage("InstructAdd", this, player.UserIDString), 20, "0 .9", "1 1");
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["dark"], $"0.14 0.05", $"0.38 0.15", true);
            UI.CreateButton(ref element, PublicObjectSetup, UIColors["buttongreen"], "<color='#818884'>Add</color>", 20, "0.15 0.06", "0.37 0.14", $"CUI_ObjectSetup2");
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["dark"], $"0.64 0.05", $"0.88 0.15", true);
            UI.CreateButton(ref element, PublicObjectSetup, UIColors["buttongreen"], "<color='#818884'>Remove</color>", 20, "0.65 0.06", "0.87 0.14", $"CUI_ObjectRemove1");
            CuiHelper.AddUi(player, element);    
        } 

        [ConsoleCommand("CUI_ObjectRemove1")]
        void ObjectRemove(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
                destroyUIN(arg);

            var element = UI.CreateElementContainer(PublicObjectSetup, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicObjectSetup, UIColors["header"], lang.GetMessage("ObjConfig", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicObjectSetup, UIColors["dark"], lang.GetMessage("OnceNear", this, player.UserIDString), 20, "0 .9", "1 1");
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["dark"], "0.14 0.05", "0.38 0.15", true);
            UI.CreateButton(ref element, PublicObjectSetup, UIColors["buttongreen"], "Remove Turret", 20, "0.15 0.06", "0.37 0.14", $"CUI_ObjectRemove2 Turret");
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["dark"], "0.39 0.05", "0.63 0.15", true);
            UI.CreateButton(ref element, PublicObjectSetup, UIColors["buttongreen"], "Remove Light", 20, "0.40 0.06", "0.62 0.14", $"CUI_ObjectRemove2 Light");
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["dark"], "0.64 0.05", "0.88 0.15", true);
            UI.CreateButton(ref element, PublicObjectSetup, UIColors["buttongreen"], "Remove Door", 20, "0.65 0.06", "0.87 0.14", $"CUI_ObjectRemove2 Door");
            CuiHelper.AddUi(player, element);    
        } 

        [ConsoleCommand("CUI_ObjectRemove2")]
        void ObjectRemove2(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
                destroyUIN(arg);
            switch(arg.Args[0])
            {
                case "Turret":
                    if(homeData.homeD[player.userID].tData.Count == 0)
                    {
                        SendReply(player, "You have no turrets.");
                        return;
                    }
                    List<BaseEntity> turretnear = new List<BaseEntity>();
                    Vis.Entities(player.transform.position, 1.5f, turretnear);
                    var locationT = new Vector3();
                    foreach(var turret in turretnear)
                    {
                        if (turret.ToString().Contains("auto"))
                        {
                            locationT = turret.transform.position;
                            break;
                        }
                    }
                    if(locationT == new Vector3())
                    {
                        SendReply(player, lang.GetMessage("NoTurretInRange", this, player.UserIDString));
                        return;
                    }
                    var i = 0;
                    var ent2 = "";
                    foreach(var entry in homeData.homeD[player.userID].tData)
                    {
                        if(homeData.homeD[player.userID].tData[entry.Key].locx == locationT.x)
                        {
                            i++;
                            player.SendConsoleCommand("CUI_ObjectRemove3");
                            SendReply(player, lang.GetMessage("DeletedTurret", this, player.UserIDString).Replace("0", entry.Key));
                            homeData.homeD[player.userID].objectX.Remove(homeData.homeD[player.userID].tData[entry.Key].locx);
                            ent2 = entry.Key;
                            CuiHelper.DestroyUi(player, PublicSideBar);
                            CuiHelper.DestroyUi(player, PublicSetupName);
                            CuiHelper.DestroyUi(player, PublicObjectSetup);
                            isEditing.Remove(player.userID);
                            barUI.Remove(player.userID);
                            setupUI.Remove(player.userID);
                            openUI(player);
                        }
                    }
                    if(i == 0)
                    {
                        SendReply(player, lang.GetMessage("NoTurretInRange", this, player.UserIDString));
                        return;              
                    }
                    if(i != 0)
                    {
                        homeData.homeD[player.userID].tData.Remove(homeData.homeD[player.userID].tData[ent2].name);
                        SaveData();
                    }
                break;
                case "Light":       
                    if(homeData.homeD[player.userID].lData.Count == 0)
                    {
                        SendReply(player, "You have no lights.");
                        return;
                    }
                    List<BaseEntity> lightnear = new List<BaseEntity>();
                    Vis.Entities(player.transform.position, 1.5f, lightnear);
                    var locationL = new Vector3();
                    foreach(var light in lightnear)
                    {
                        if (light.ToString().Contains("lantern") || light.ToString().Contains("ceilinglight"))
                        {
                            locationL = light.transform.position;
                            break;
                        }
                    }
                    if(locationL == new Vector3())
                    {
                        SendReply(player, lang.GetMessage("NoLightInRange", this, player.UserIDString));
                        return;
                    }
                    var n = 0;
                    var ent4 = "";
                    foreach(var entry2 in homeData.homeD[player.userID].lData)
                    {
                        if(homeData.homeD[player.userID].lData[entry2.Key].locx == locationL.x)
                        {
                            n++;
                            player.SendConsoleCommand("CUI_ObjectRemove3");
                            SendReply(player, lang.GetMessage("DeletedLight", this, player.UserIDString).Replace("0", entry2.Key));
                            homeData.homeD[player.userID].objectX.Remove(homeData.homeD[player.userID].lData[entry2.Key].locx);
                            ent4 = entry2.Key;
                            CuiHelper.DestroyUi(player, PublicSideBar);
                            CuiHelper.DestroyUi(player, PublicSetupName);
                            CuiHelper.DestroyUi(player, PublicObjectSetup);
                            isEditing.Remove(player.userID);
                            barUI.Remove(player.userID);
                            setupUI.Remove(player.userID);
                            openUI(player);
                        }
                    }
                    if(n == 0)
                    {
                        SendReply(player, lang.GetMessage("NoLightInRange", this, player.UserIDString));
                        return;              
                    }
                    if(n != 0)
                    {
                        homeData.homeD[player.userID].lData.Remove(homeData.homeD[player.userID].lData[ent4].name);
                        SaveData();
                    }
                break;
                case "Door":         
                    if(homeData.homeD[player.userID].dData.Count == 0)
                    {
                        SendReply(player, "You have no doors.");
                        return;
                    }
                    List<BaseEntity> doornear = new List<BaseEntity>();
                    Vis.Entities(player.transform.position, 1.5f, doornear);
                    var locationD = new Vector3();
                    foreach(var door in doornear)
                    {
                        if (door.ToString().Contains("hinged"))
                        {
                            locationD = door.transform.position;
                            break;
                        }
                    }
                    if(locationD == new Vector3())
                    {
                        SendReply(player, lang.GetMessage("NoDoorInRange", this, player.UserIDString));
                        return;
                    }
                    var k = 0;
                    var ent3 = "";
                    foreach(var entry1 in homeData.homeD[player.userID].dData)
                    {
                        if(homeData.homeD[player.userID].dData[entry1.Key].locx == locationD.x)
                        {
                            k++;
                            player.SendConsoleCommand("CUI_ObjectRemove3");
                            SendReply(player, lang.GetMessage("DeletedDoor", this, player.UserIDString).Replace("0", entry1.Key));
                            homeData.homeD[player.userID].objectX.Remove(homeData.homeD[player.userID].dData[entry1.Key].locx);
                            ent3 = entry1.Key;
                            CuiHelper.DestroyUi(player, PublicSideBar);
                            CuiHelper.DestroyUi(player, PublicSetupName);
                            CuiHelper.DestroyUi(player, PublicObjectSetup);
                            isEditing.Remove(player.userID);
                            barUI.Remove(player.userID);
                            setupUI.Remove(player.userID);
                            openUI(player);
                        }
                    }
                    if(k == 0)
                    {
                        SendReply(player, lang.GetMessage("NoTurretInRange", this, player.UserIDString));
                        return;              
                    }
                    if(k != 0)
                    {
                        homeData.homeD[player.userID].dData.Remove(homeData.homeD[player.userID].dData[ent3].name);
                        SaveData();
                    }
                break;
            }
        } 

        [ConsoleCommand("CUI_ObjectSetup2")]
        void ObjectSetup2(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
                destroyUIN(arg);

            var element = UI.CreateElementContainer(PublicObjectSetup, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicObjectSetup, UIColors["header"], lang.GetMessage("ObjConfig", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicObjectSetup, UIColors["dark"], lang.GetMessage("OnceNear", this, player.UserIDString), 20, "0 .9", "1 1");
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["dark"], "0.14 0.05", "0.38 0.15", true);
            UI.CreateButton(ref element, PublicObjectSetup, UIColors["buttongreen"], lang.GetMessage("AddTurret", this, player.UserIDString), 20, "0.15 0.06", "0.37 0.14", $"CUI_ObjectSetup3 Turret");
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["dark"], "0.39 0.05", "0.63 0.15", true);
            UI.CreateButton(ref element, PublicObjectSetup, UIColors["buttongreen"], lang.GetMessage("AddLight", this, player.UserIDString), 20, "0.40 0.06", "0.62 0.14", $"CUI_ObjectSetup3 Light");
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["dark"], "0.64 0.05", "0.88 0.15", true);
            UI.CreateButton(ref element, PublicObjectSetup, UIColors["buttongreen"], lang.GetMessage("AddDoor", this, player.UserIDString), 20, "0.65 0.06", "0.87 0.14", $"CUI_ObjectSetup3 Door");
            CuiHelper.AddUi(player, element);    
        } 

        List<T> FindEntities<T>(Vector3 position, float distance) where T : BaseEntity
        {
            var list = Pool.GetList<T>();
            Vis.Entities(position, distance, list, LayerMask.GetMask("Construction", "Construction Trigger", "Trigger", "Deployed"));
            return list;
        }

        [ConsoleCommand("CUI_ObjectSetup3")]
        void ObjectSetup3(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
                destroyUIN(arg);
            switch(arg.Args[0])
            {
                case "Turret":
                    if(homeData.homeD[player.userID].tData.Count >= 19)
                    {
                        SendReply(player, lang.GetMessage("You may only have 18 of each object.", this, player.UserIDString));
                        return;
                    }
                    List<BaseEntity> turretnear = new List<BaseEntity>();
                    Vis.Entities(player.transform.position, 1.5f, turretnear);
                    var newKeyT = (uint)(5);
                    var locationT = new Vector3();
                    foreach(var turret in turretnear)
                    {
                        if (turret.ToString().Contains("auto"))
                        {
                            newKeyT = turret.net.ID;
                            locationT = turret.transform.position;
                            break;
                        }
                    }
                    if(locationT == new Vector3())
                    {
                        SendReply(player, lang.GetMessage("NoTurretInRange", this, player.UserIDString));
                        return;
                    }
                    if(homeData.homeD[player.userID].objectX.Contains(locationT.x))
                    {
                        SendReply(player, lang.GetMessage("AlreadyAddedTurret", this, player.UserIDString));
                        return;
                    }  
                    isEditing.Add(player.userID);
                    var tinfo = new newData()
                    {
                        entKey = newKeyT,
                        entName = "",
                        entStatus = false,
                        entType = "Turret",
                        entLocation = locationT,
                    };
                    newD.Add(player.userID, tinfo);
                break;
                case "Light":
                    if(homeData.homeD[player.userID].lData.Count >= 18)
                    {
                        SendReply(player, lang.GetMessage("You may only have 18 of each object.", this, player.UserIDString));
                    }             
                    var newKeyL = (uint)(5);
                    var locationL = new Vector3();
                    List<BaseEntity> nearby = new List<BaseEntity>();
                    Vis.Entities(player.transform.position, 4.5f, nearby);
                    foreach (var lantern in nearby)
                    {
                        if (lantern.ToString().Contains("lantern") || lantern.ToString().Contains("ceilinglight"))
                        {
                            newKeyL = lantern.net.ID;
                            locationL = lantern.transform.position;
                            break;
                        }
                    }
                    if(locationL == new Vector3())
                    {
                        SendReply(player, lang.GetMessage("NoLightInRange", this, player.UserIDString));
                        return;
                    }
                    if(homeData.homeD[player.userID].objectX.Contains(locationL.x))
                    {
                        SendReply(player, lang.GetMessage("AlreadyAddedLight", this, player.UserIDString));
                        return;
                    }      
                    isEditing.Add(player.userID);
                    var linfo = new newData()
                    {
                        entKey = newKeyL,
                        entType = "Light",
                        entName = "",
                        entStatus = false,
                        entLocation = locationL,
                    };
                    newD.Add(player.userID, linfo);
                break;
                case "Door":
                    if(homeData.homeD[player.userID].dData.Count >= 18)
                    {
                        SendReply(player, lang.GetMessage("You may only have 18 of each object.", this, player.UserIDString));
                        return;
                    }           
                    var newKeyD = (uint)(5);
                    var locationD = new Vector3();
                    List<BaseEntity> nearby2 = new List<BaseEntity>();
                    Vis.Entities(player.transform.position, 1.5f, nearby2);
                    foreach (var door in nearby2)
                    {
                        if (door.ToString().Contains("hinged"))
                        {
                            newKeyD = door.net.ID;
                            locationD = door.transform.position;
                            break;
                        }
                    }
                    if(locationD == new Vector3())
                    {
                        SendReply(player, lang.GetMessage("NoDoorInRange", this, player.UserIDString));
                        return;
                    }
                    if(homeData.homeD[player.userID].objectX.Contains(locationD.x))
                    {
                        SendReply(player, lang.GetMessage("AlreadyAddedDoor", this, player.UserIDString));
                        return;
                    }
                    isEditing.Add(player.userID);
                    var Dinfo = new newData()
                    {
                        entKey = newKeyD,
                        entType = "Door",
                        entName = "",
                        entStatus = false,
                        entLocation = locationD,
                    };
                    newD.Add(player.userID, Dinfo);
                break;
            }

            var element = UI.CreateElementContainer(PublicSetupName, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicSetupName, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicSetupName, UIColors["header"], lang.GetMessage("ObjConfig", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicSetupName, UIColors["dark"], lang.GetMessage("NameObje", this), 20, "0 .9", "1 1");
            UI.CreatePanel(ref element, PublicSetupName, UIColors["dark"], "0.39 0.05", "0.63 0.15", true);
            UI.CreateButton(ref element, PublicSetupName, UIColors["buttongreen"], lang.GetMessage("Exit", this, player.UserIDString), 20, "0.40 0.06", "0.62 0.14", $"CloseCUIMain");
            CuiHelper.AddUi(player, element);    
        } 

        [ConsoleCommand("CUI_ObjectSetup4")]
        void ObjectSetup4(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
                destroyUIN(arg);

            var element = UI.CreateElementContainer(PublicObjectSetup, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PublicObjectSetup, UIColors["header"], lang.GetMessage("ObjConfig", this, player.UserIDString), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreateLabel(ref element, PublicObjectSetup, UIColors["dark"], lang.GetMessage("Completed", this, player.UserIDString), 20, "0 .9", "1 1");
            UI.CreatePanel(ref element, PublicObjectSetup, UIColors["dark"], "0.39 0.05", "0.63 0.15", true);
            UI.CreateButton(ref element, PublicObjectSetup, UIColors["buttongreen"], lang.GetMessage("Save", this, player.UserIDString), 20, "0.40 0.06", "0.62 0.14", $"CUI_SaveNewObject");
            CuiHelper.AddUi(player, element);    
        }        

        [ConsoleCommand("CUI_SaveNewObject")]
        void ObjectSetup5(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            SaveData();
            CuiHelper.DestroyUi(player, PublicSideBar);
            CuiHelper.DestroyUi(player, PublicSetupName);
            CuiHelper.DestroyUi(player, PublicObjectSetup);
            isEditing.Remove(player.userID);
            barUI.Remove(player.userID);
            setupUI.Remove(player.userID);
            openUI(player);
        }        

        void OnPlayerChat(ConsoleSystem.Arg arg)
        {
            try{
                var player = arg.connection.player as BasePlayer;
                if (player == null)
                    return;
                    if(isEditing.Contains(player.userID))
                    {
                        if (Homes.Find(player) == null)
                            OnPlayerInit(player);
                            Homes playerData = Homes.Find(player);
                            newD[player.userID].entName = arg.Args[0];

                            switch(newD[player.userID].entType)
                            {
                                case "Turret":
                                   foreach(var entry in homeData.homeD[player.userID].tData){
                                        if(entry.Key.ToString() == arg.Args[0]){
                                            isEditing.Remove(player.userID);
                                            newD.Remove(player.userID);
                                            SendReply(player, lang.GetMessage("AlreadyName", this, player.UserIDString).Replace("0", arg.Args[0]));
                                            return;
                                        }
                                    }
                                    var infoT = new TurretData()
                                    {
                                        locx = newD[player.userID].entLocation.x,
                                        locy = newD[player.userID].entLocation.y,
                                        locz = newD[player.userID].entLocation.z,
                                        key = newD[player.userID].entKey,
                                        status = newD[player.userID].entStatus,
                                        name = newD[player.userID].entName,
                                    };
                                    homeData.homeD[player.userID].objectX.Add(newD[player.userID].entLocation.x);
                                    playerData.tData.Add(newD[player.userID].entName, infoT);
                                    player.SendConsoleCommand("CUI_ObjectSetup4");
                                    newD.Remove(player.userID);
                                break;
                                case "Light":
                                   foreach(var entry in homeData.homeD[player.userID].lData){
                                        if(entry.Key.ToString() == arg.Args[0]){
                                            isEditing.Remove(player.userID);
                                            newD.Remove(player.userID);
                                            SendReply(player, lang.GetMessage("AlreadyName", this, player.UserIDString).Replace("0", arg.Args[0]));
                                            return;
                                        }
                                    }
                                    var infoL = new LightData()
                                    {
                                        locx = newD[player.userID].entLocation.x,
                                        locy = newD[player.userID].entLocation.y,
                                        locz = newD[player.userID].entLocation.z,
                                        key = newD[player.userID].entKey,
                                        status = newD[player.userID].entStatus,
                                        name = newD[player.userID].entName,

                                    };
                                    homeData.homeD[player.userID].objectX.Add(newD[player.userID].entLocation.x);
                                    playerData.lData.Add(newD[player.userID].entName, infoL);
                                    player.SendConsoleCommand("CUI_ObjectSetup4");
                                    newD.Remove(player.userID);
                                break;
                                case "Door":
                                    foreach(var entry in homeData.homeD[player.userID].dData){
                                        if(entry.Key.ToString() == arg.Args[0]){
                                            isEditing.Remove(player.userID);
                                            newD.Remove(player.userID);
                                            SendReply(player, lang.GetMessage("AlreadyName", this, player.UserIDString).Replace("0", arg.Args[0]));
                                            return;
                                        }
                                    }
                                    var infoD = new DoorData()
                                    {
                                        locx = newD[player.userID].entLocation.x,
                                        locy = newD[player.userID].entLocation.y,
                                        locz = newD[player.userID].entLocation.z,
                                        key = newD[player.userID].entKey,
                                        status = newD[player.userID].entStatus,
                                        name = newD[player.userID].entName,

                                    };
                                    homeData.homeD[player.userID].objectX.Add(newD[player.userID].entLocation.x);
                                    playerData.dData.Add(newD[player.userID].entName, infoD);
                                    player.SendConsoleCommand("CUI_ObjectSetup4");
                                    newD.Remove(player.userID);
                                break;
                            }
                    }  
            }
            catch(System.Exception)
            {
                return;
            }
        }


        // / ////// / //   
        // / OtherC / //
        // / ////// / //    

        [ConsoleCommand("CUI_RemoteBar")]
        void RemoteBar(BasePlayer player)
        {
            if (Homes.Find(player) == null)
                    OnPlayerInit(player);
            Homes playerData = Homes.Find(player);
            var location = new Vector3(playerData.locx, playerData.locy, playerData.locz);
            var element = UI.CreateElementContainer(PublicSideBar, UIColors["dark"], "0.1 0.1", "0.205 0.5", true);
            UI.CreatePanel(ref element, PublicSideBar, UIColors["light"], "0.05 0.03", "0.95 0.97", true);
            UI.CreateButton(ref element, PublicSideBar, UIColors["blue"], lang.GetMessage("HomeMenu", this, player.UserIDString), 20, "0.1 0.86", "0.9 0.96", $"");

            if(playerData.locx == 0.0f){
            UI.CreateButton(ref element, PublicSideBar, UIColors["green"], lang.GetMessage("HomeSetup", this, player.UserIDString), 16, "0.1 0.73", "0.9 0.83", "CUI_Homes");
            } else { UI.CreateButton(ref element, PublicSideBar, UIColors["green"], lang.GetMessage("HomeSetup", this, player.UserIDString), 16, "0.1 0.73", "0.9 0.83", "CUI_HomesNew"); }

            List<BaseEntity> nearby = new List<BaseEntity>();
            Vis.Entities(location, configData.Options.ActivationDistance, nearby);
            var dotrue = false;
            foreach(var ent in nearby)
            {
                if(ent is BasePlayer)
                {
                    BasePlayer newplayer = ent.ToPlayer();
                    if(newplayer != player) break;
                    else
                    {
                        dotrue = true;
                        break;
                    }
                }
                else dotrue = false;
            }
            if(dotrue) UI.CreateButton(ref element, PublicSideBar, UIColors["orange"], lang.GetMessage("ObjectSet", this, player.UserIDString), 16, "0.1 0.59", "0.9 0.69", "CUI_ObjectSetup");
            else UI.CreateButton(ref element, PublicSideBar, UIColors["orange"], lang.GetMessage("ObjectSet", this, player.UserIDString), 16, "0.1 0.59", "0.9 0.69", "");
            UI.CreateButton(ref element, PublicSideBar, UIColors["lightblue"], lang.GetMessage("CtrlMenu", this, player.UserIDString), 16, "0.1 0.45", "0.9 0.55", "CUI_ControlMenu");
            UI.CreateButton(ref element, PublicSideBar, UIColors["buttonred"], lang.GetMessage("Close", this, player.UserIDString), 16, "0.1 0.04", "0.9 0.14", "CloseCUIMain");
            CuiHelper.AddUi(player, element);
        }

        // / ////// / //   
        // / CUIMai / //
        // / ////// / //    

        static string PublicSetupName = "PublicSetupName";
        static string PublicSideBar = "PublicSideBar";
        static string PublicObjectSetup = "PublicObjectSetup";
        static string PublicControlSetup = "PublicControlSetup";
        static string PublicControlSetupTurret = "PublicControlSetupTurret";
        static string PublicControlSetupLight = "PublicControlSetupLight";
        static string PublicControlSetupDoor = "PublicControlSetupDoor";

        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false)
            {
                var NewElement = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = color},
                        RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                        CursorEnabled = cursor
                    },
                    new CuiElement().Parent,
                    panelName
                }
            };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = 1.0f, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);
            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
        }

        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"dark", "0.1 0.1 0.1 0.98" },
            {"header", "0 0 0 0.6" },
            {"light", ".85 .85 .85 1.0" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"brown", "0.3 0.16 0.0 1.0" },
            {"yellow", "0.9 0.9 0.0 1.0" },
            {"orange", "1.0 0.65 0.0 1.0" },
            {"blue", "0.2 0.6 1.0 1.0" },
            {"red", "1.0 0.1 0.1 1.0" },
            {"green", "0.28 0.82 0.28 1.0" },
            {"grey", "0.85 0.85 0.85 1.0" },
            {"lightblue", "0.6 0.86 1.0 1.0" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttongreen", "0.133 0.965 0.133 0.9" },
            {"buttonred", "0.964 0.133 0.133 0.9" },
            {"buttongrey", "0.8 0.8 0.8 0.9" }
        }; 

        // / ////// / //   
        // / Langua / //
        // / ////// / // 

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"SelectMenu", "<color='#818884'>Select a menu you wish to open. With these menu's you can edit any registered objects.</color>"},
            {"OpenTurret", "<color='#818884'>Open Turret Menu</color>"},
            {"OpenLight", "<color='#818884'>Open Light Menu</color>"},
            {"OpenDoor", "<color='#818884'>Open Door Menu</color>"},
            {"TList", "<color='#818884'>Turret List</color>"},
            {"LList", "<color='#818884'>Lights List</color>"},
            {"DList", "<color='#818884'>Door List</color>"},
            {"SHomes", "<color='#818884'>Smart Homes</color>"},
            {"WalkThrough", "<color='#818884'>Smart Home will walk you through how to setup your home!</color>"},
            {"BeginProcess", "<color='#818884'>Begin Processing Home</color>"},
            {"WalkThroughNew", "<color='#818884'>Smart Home will walk you through how to setup your new home!</color>"},
            {"BeginProcessNew", "<color='#818884'>Begin Reprocessing Home!</color>"},
            {"StandWhereNew", "<color='#818884'>Go ahead and stand where you wish your new home to be and hit next(Suggestion is in middle of base)(WARNING-THIS WILL REMOVE ALL OBJECT DATA!!!!)!</color>"},
            {"NextRe", "<color='#818884'>Begin Next Reprocessing Stage</color>"},
            {"StandWhere", "<color='#818884'>Go ahead and stand where you wish your home to be and hit next(Suggestion is in middle of base)!</color>"},
            {"NextNext", "<color='#818884'>Next Processing Stage</color>"},
            {"SmartHomeNew", "<color='#818884'>Smart Home has just finished your new home! You may now add objects within 0m!</color>"},
            {"SmartHome", "<color='#818884'>Smart Home has just finished your home! You may now add objects within 0m!</color>"},
            {"ObjConfig", "<color='#818884'>Object Configuration</color>"},
            {"InstructAdd", "<color='#818884'>Follow the instructions to add/remove a object. Press Remove or Add to start!</color>"},
            {"OnceNear", "<color='#818884'>One near the correct object. Click the appropriate selection below!</color>"},
            {"AddTurret", "<color='#818884'>Add Turret</color>"},
            {"AddLight", "<color='#818884'>Add Light</color>"},
            {"AddDoor", "<color='#818884'>Add Door</color>"},
            {"NameObje", "<color='#818884'>You must now name your object. Type your new name in chat(without any command)!</color>"},
            {"Exit", "<color='#818884'>Exit</color>"},
            {"Completed", "<color='#818884'>You have completed the process and created a new object! Click Save New Object to finish!</color>"},
            {"CompletedRemove", "<color='#818884'>You have completed the process and removed a object! Click Save New Object to finish!</color>"},
            {"Save", "<color='#818884'>Save New Object</color>"},
            {"HomeMenu", "<color='#818884'>*Home Menu*</color>"},
            {"HomeSetup", "<color='#818884'>Home Setup</color>"},
            {"ObjectSet", "<color='#818884'>Object Setup</color>"},
            {"Close", "<color='#818884'>Close</color>"},
            {"AlreadyName", "There is already a object with the name of 0!"},
            {"BuildingAuth", "You must have all cupboard access within 0m."},
            {"LostObject", "You have lost your object: 0 to player/object 1."},
            {"DeletedTurret", "You have deleted the turret with the name of 0."},
            {"DeletedLight", "You have deleted the light with the name of 0."},
            {"DeletedDoor", "You have deleted the door with the name of 0."},
            {"NoDoorInRange", "There is no door within 1m of you!"},
            {"NoLightInRange", "There is no light within 5m of you!"},
            {"NoTurretInRange", "There is no turret within 1m of you!"},
            {"AlreadyAddedDoor", "You have already added this door!"},
            {"AlreadyAddedTurret", "You have already added this turret!"},
            {"AlreadyAddedLight", "You have already added this light!"},
            {"CtrlMenu", "<color='#818884'>Control Menu</color>"},
            {"Next", "Next"},
            {"Back", "Back"},
        };
    }
}