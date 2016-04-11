using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("LustyMap", "Kayzor", "1.0.24", ResourceId = 1333)]
    [Description("In-game map and minimap GUI")]
    public class LustyMap : RustPlugin
    {
        // Plugin variables
        string lustyPlugin = null;
        string lustyAuthor = null;
        string lustyVersion = null;
        string lustyDescription = null;

        // System variables
        int mapslices = 32;
        bool debug = false;
        bool mapmode = false;
        bool minimap = true;
        bool left = true;
        bool compass = true;
        bool startopen = true;
        bool mapbutton = false;
        string mapurl = null;
        string mapcomplex = null;
        List<PlayerMap> MinimapPlayers = new List<PlayerMap>();
        List<BasePlayer> MinimapReopen = new List<BasePlayer>();
        List<ulong> startupList = new List<ulong>();
        List<lustyMonuments> Monuments = new List<lustyMonuments>();        

        // Text Strings
        string txtInvalid = null;
        string txtUnknown = null;
        string txtCmdComplex = null;
        string txtCmdMinimap = null;
        string txtCmdMode = null;
        string txtCmdUrl = null;
        string txtCmdCompass = null;
        string txtCmdStart = null;
        string txtCmtMode = null;
        string txtCmtComplex = null;
        string txtCmtUrl = null;
        string txtCmtMinimap = null;
        string txtCmtAlign = null;
        string txtCmtCompass = null;
        string txtCmtOpen = null;
        string txtCpsHead = null;
        string txtCpsN = null;
        string txtCpsNE = null;
        string txtCpsE = null;
        string txtCpsSE = null;
        string txtCpsS = null;
        string txtCpsSW = null;
        string txtCpsW = null;
        string txtCpsNW = null;
        string txtBtnClose = null;
        string txtBtnMap = null;

        // Plugin setup
        void Init()
        {
            object[] getAttributes = this.GetType().GetCustomAttributes(false);

            foreach (Attribute a in getAttributes)
            {
                if (a.ToString() == "Oxide.Plugins.DescriptionAttribute")
                {
                    lustyDescription = (a as DescriptionAttribute).Description;
                }
                else if (a.ToString() == "Oxide.Plugins.InfoAttribute")
                {
                    lustyPlugin = (a as InfoAttribute).Title;
                    lustyAuthor = (a as InfoAttribute).Author;
                    lustyVersion = (a as InfoAttribute).Version.ToString();
                }
            }
        }

        void Loaded()
        {
            // Default config vlaues
            set("LustyMap", "MapURL", "http://185.38.151.245:28015/map.jpg", false);
            set("LustyMap", "MapComplexURL", "http://lustyrust.co.uk/img/lustyplugins/map32x32/", false);
            set("LustyMap", "MapMode", false, false);
            set("LustyMap", "Minimap", true, false);
            set("LustyMap", "Left", true, false);
            set("LustyMap", "Compass", true, false);
            set("LustyMap", "StartOpen", true, false);
            set("LustyMap", "Debug", false, false);
            set("LustyMap", "MapButton", false, false);

            // Default/English text strings
            set("TextStrings", "InvalidSyntex", "Invalid syntex! usage: ", false);
            set("TextStrings", "UnknownCommand", "Unknown command, type <color=#00ff00ff>/map help</color> for a list of commands.", false);
            set("TextStrings", "ComplexCommand", "<color=#00ff00ff>/map complex <url to map parts></color> - Sets the located to the map parts for complex mode.", false);
            set("TextStrings", "MinimapCommand", "<color=#00ff00ff>/map minimap <true|false|left|right></color> - Enables, disables or sets the default alignment for the minimap.", false);
            set("TextStrings", "ModeCommand", "<color=#00ff00ff>/map mode <true|false></color> - Enables or disables complex mode.", false);
            set("TextStrings", "UrlCommand", "<color=#00ff00ff>/map url <url to map image></color> - Sets the map image, used as the background image.", false);
            set("TextStrings", "CompassCommand", "<color=#00ff00ff>/map compass <true|false></color> - Enables or disables the minimap compass.", false);
            set("TextStrings", "StartCommand", "<color=#00ff00ff>/map startopen <true|false></color> - Sets the default state for the minimap.", false);

            set("TextStrings", "ModeCommit", "Complex mode has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "ComplexCommit", "Map Complex URL has been set to: <color=#00ff00ff>{0}</color>", false);
            set("TextStrings", "UrlCommit", "Map URL has been set to: <color=#00ff00ff>{0}</color>", false);
            set("TextStrings", "MinimapCommit", "Minimap has been <color=#00ff00ff>{0}</color> for all players.", false);
            set("TextStrings", "AlignCommit", "Minimap has been set to the <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "CompassCommit", "Minimap compass has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "OpenCommit", "Minimap will be <color=#00ff00ff>{0}</color> by default.", false);

            set("TextStrings", "CloseButton", "Close Map", false);
            set("TextStrings", "MapButton", "Map", false);

            set("CompassStrings", "Head", "Heading:", false);
            set("CompassStrings", "N", "North", false);
            set("CompassStrings", "NE", "North East", false);
            set("CompassStrings", "E", "East", false);
            set("CompassStrings", "SE", "South East", false);
            set("CompassStrings", "S", "South", false);
            set("CompassStrings", "SW", "South West", false);
            set("CompassStrings", "W", "West", false);
            set("CompassStrings", "NW", "North West", false);            

            // Load config values
            mapurl = Convert.ToString(get("LustyMap", "MapURL"));
            mapcomplex = Convert.ToString(get("LustyMap", "MapComplexURL"));
            mapmode = Convert.ToBoolean(get("LustyMap", "MapMode"));
            minimap = Convert.ToBoolean(get("LustyMap", "Minimap"));
            left = Convert.ToBoolean(get("LustyMap", "Left"));
            compass = Convert.ToBoolean(get("LustyMap", "Compass"));
            startopen = Convert.ToBoolean(get("LustyMap", "StartOpen"));
            debug = Convert.ToBoolean(get("LustyMap", "Debug"));
            mapbutton = Convert.ToBoolean(get("LustyMap", "MapButton"));

            // Text strings
            txtInvalid = (string)get("TextStrings", "InvalidSyntex");
            txtUnknown = (string)get("TextStrings", "UnknownCommand");
            txtCmdComplex = (string)get("TextStrings", "ComplexCommand");
            txtCmdMinimap = (string)get("TextStrings", "MinimapCommand");
            txtCmdMode = (string)get("TextStrings", "ModeCommand");
            txtCmdUrl = (string)get("TextStrings", "UrlCommand");
            txtCmdCompass = (string)get("TextStrings", "CompassCommand");
            txtCmdStart = (string)get("TextStrings", "StartCommand");

            txtCmtMode = (string)get("TextStrings", "ModeCommit");
            txtCmtComplex = (string)get("TextStrings", "ComplexCommit");
            txtCmtUrl = (string)get("TextStrings", "UrlCommit");
            txtCmtMinimap = (string)get("TextStrings", "MinimapCommit");
            txtCmtAlign = (string)get("TextStrings", "AlignCommit");
            txtCmtCompass = (string)get("TextStrings", "CompassCommit");
            txtCmtOpen = (string)get("TextStrings", "OpenCommit");

            txtBtnClose = (string)get("TextStrings", "CloseButton");
            txtBtnMap = (string)get("TextStrings", "MapButton");

            txtCpsHead = (string)get("CompassStrings", "Head");
            txtCpsN = (string)get("CompassStrings", "N");
            txtCpsNE = (string)get("CompassStrings", "NE");
            txtCpsE = (string)get("CompassStrings", "E");
            txtCpsSE = (string)get("CompassStrings", "SE");
            txtCpsS = (string)get("CompassStrings", "S");
            txtCpsSW = (string)get("CompassStrings", "SW");
            txtCpsW = (string)get("CompassStrings", "W");
            txtCpsNW = (string)get("CompassStrings", "NW");
            
            // Start map timer
            timer.Repeat(1f, 0, () => lustyTimer());

            if (BasePlayer.activePlayerList.Count > 0)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    startupList.Add(player.userID);
                    if (minimap)
                    {
                        minimapAdd(player);
                    }
                    player.Command("bind m \"LustyMap map\"");
                }
            }
        }

        void Unloaded()
        {
            if (BasePlayer.activePlayerList.Count > 0)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("MinimapBG"));
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Minimap"));
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("MinimapHUD"));
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("MapGUI"));
                }
            }
        }

        private void lustyTimer()
        {
            if (minimap)
            {
                minimapUpdate();
            }
        }



        // Monuments
        void OnServerInitialized()
        {
            var gameobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            Puts($"Found {gameobjects.Length} gameobjects on the map.");
            foreach (var go in gameobjects)
            {
                if (go.name.ToLower().Contains("monument"))
                {
                    lustyMonuments monument = new lustyMonuments();
                    monument.position = go.transform.position;
                    // Convert location into percent for the map gui
                    float x = Convert.ToSingle(go.transform.position.x);
                    float y = Convert.ToSingle(go.transform.position.y);
                    float z = Convert.ToSingle(go.transform.position.z);
                    int mapsize = Convert.ToInt32(TerrainMeta.Size.x);
                    x = Convert.ToSingle(x + (mapsize / 2));
                    z = Convert.ToSingle(z + (mapsize / 2));
                    monument.percentX = Convert.ToSingle(x / mapsize);
                    monument.percentZ = Convert.ToSingle(z / mapsize);
                    // Work out which minimap chunk the monument is in                    
                    monument.row = (Convert.ToInt16(Math.Floor(mapslices * monument.percentX)));
                    monument.column = ((mapslices - 1) - Convert.ToInt16(Math.Floor(mapslices * monument.percentZ)));                    

                    if (go.name.ToLower().Contains("lighthouse"))
                    {
                        monument.name = "Lighthouse";
                        monument.icon = "http://map.playrust.io/img/lighthouse.png";
                        Monuments.Add(monument);
                    }
                    else if (go.name.ToLower().Contains("radtown"))
                    {
                        monument.name = "Radtown";
                        monument.icon = "http://map.playrust.io/img/radtown.png";
                        Monuments.Add(monument);
                    }
                    else if (go.name.ToLower().Contains("cave"))
                    {
                        monument.name = "Cave";
                        monument.icon = "http://map.playrust.io/img/cave.png";
                        Monuments.Add(monument);
                    }
                    else if (go.name.ToLower().Contains("warehouse"))
                    {
                        monument.name = "Warehouse";
                        monument.icon = "http://map.playrust.io/img/warehouse.png";
                        Monuments.Add(monument);
                    }
                    else if (go.name.ToLower().Contains("satellite"))
                    {
                        monument.name = "Satellite Dish";
                        monument.icon = "http://map.playrust.io/img/dish.png";
                        Monuments.Add(monument);
                    }
                    else if (go.name.ToLower().Contains("sphere"))
                    {
                        monument.name = "Sphere Tank";
                        monument.icon = "http://map.playrust.io/img/spheretank.png";
                        Monuments.Add(monument);
                    }
                    else if (go.name.ToLower().Contains("powerplant"))
                    {
                        monument.name = "Powerplant";
                        monument.icon = "http://map.playrust.io/img/special.png";
                        Monuments.Add(monument);
                    }
                    else if (go.name.ToLower().Contains("trainyard"))
                    {
                        monument.name = "Trainyard";
                        monument.icon = "http://map.playrust.io/img/special.png";
                        Monuments.Add(monument);
                    }
                    else if (go.name.ToLower().Contains("airfield"))
                    {
                        monument.name = "Airfield";
                        monument.icon = "http://map.playrust.io/img/special.png";
                        Monuments.Add(monument);
                    }
                    else if (go.name.ToLower().Contains("tunnel"))
                    {
                        monument.name = "Military Tunnel";
                        monument.icon = "http://map.playrust.io/img/special.png";
                        Monuments.Add(monument);
                    }
                    else if (go.name.ToLower().Contains("treatment"))
                    {
                        monument.name = "Water Treatment Plant";
                        monument.icon = "http://map.playrust.io/img/special.png";
                        Monuments.Add(monument);
                    }
                    else
                    {
                        // Missed one!
                        Puts("Missed monument " + go.name.ToLower());
                    }
                }
            }
        }

        private class lustyMonuments
        {
            public string name { get; set; }
            public Vector3 position { get; set; }
            public float percentX { get; set; }
            public float percentZ { get; set; }
            public int row { get; set; }
            public int column { get; set; }
            public string icon { get; set; }
        }

        
        // Chat commands
        [ChatCommand("map")]
        private void chatCmd(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                minimapRemove(player, true);
                mapGUI(player);
            }
            else
            {
                if (isAdmin(player))
                {
                    if (args[0].ToLower() == "mode")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                mapmode = Convert.ToBoolean(args[1]);
                                set("LustyMap", "MapMode", mapmode);

                                if (MinimapPlayers.Count > 0)
                                {
                                    foreach (PlayerMap map in MinimapPlayers)
                                    {
                                        map.refresh = true;
                                        map.mapx = 0;
                                        map.mapz = 0;
                                    }
                                }

                                string disabled = "Disabled";
                                if (mapmode) { disabled = "Enabled"; }
                                playerMsg(player, string.Format(txtCmtMode, disabled));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdMode);
                    }
                    else if (args[0].ToLower() == "compass")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                compass = Convert.ToBoolean(args[1]);
                                set("LustyMap", "Compass", compass);

                                string disabled = "Disabled";
                                if (compass) { disabled = "Enabled"; }
                                playerMsg(player, string.Format(txtCmtCompass, disabled));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdCompass);
                    }
                    else if (args[0].ToLower() == "startopen")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                startopen = Convert.ToBoolean(args[1]);
                                set("LustyMap", "StartOpen", startopen);

                                string open = "Closed";
                                if (startopen) { open = "Open"; }
                                playerMsg(player, string.Format(txtCmtOpen, open));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdStart);
                    }
                    else if (args[0].ToLower() == "minimap")
                    {
                        if (args.Length > 1)
                        {
                            if (args[1].ToLower() == "right")
                            {
                                left = false;
                                set("LustyMap", "Left", left);

                                if (MinimapPlayers.Count > 0)
                                {
                                    foreach (PlayerMap map in MinimapPlayers)
                                    {
                                        map.refresh = true;
                                        map.mapx = 0;
                                        map.mapz = 0;
                                    }
                                }

                                if (BasePlayer.activePlayerList.Count > 0)
                                {
                                    foreach (BasePlayer activeplayer in BasePlayer.activePlayerList)
                                    {
                                        minimapAdd(activeplayer);
                                    }
                                }

                                string align = "Right";
                                playerMsg(player, string.Format(txtCmtAlign, align));
                                return;
                            }
                            else if (args[1].ToLower() == "left")
                            {
                                left = true;
                                set("LustyMap", "Left", left);

                                if (MinimapPlayers.Count > 0)
                                {
                                    foreach (PlayerMap map in MinimapPlayers)
                                    {
                                        map.refresh = true;
                                        map.mapx = 0;
                                        map.mapz = 0;
                                    }
                                }

                                if (BasePlayer.activePlayerList.Count > 0)
                                {
                                    foreach (BasePlayer activeplayer in BasePlayer.activePlayerList)
                                    {
                                        minimapAdd(activeplayer);
                                    }
                                }

                                string align = "Left";
                                playerMsg(player, string.Format(txtCmtAlign, align));
                                return;
                            }
                            else if (args[1].ToLower() == "false")
                            {
                                minimap = false;
                                set("LustyMap", "Minimap", minimap);

                                if (BasePlayer.activePlayerList.Count > 0)
                                {
                                    foreach (BasePlayer activeplayer in BasePlayer.activePlayerList)
                                    {
                                        minimapRemove(activeplayer);
                                    }
                                }

                                string disabled = "Disabled";
                                playerMsg(player, string.Format(txtCmtMinimap, disabled));
                                return;
                            }
                            else if (args[1].ToLower() == "true")
                            {
                                minimap = true;
                                set("LustyMap", "Minimap", minimap);

                                if (MinimapPlayers.Count > 0)
                                {
                                    foreach (PlayerMap map in MinimapPlayers)
                                    {
                                        map.refresh = true;
                                        map.mapx = 0;
                                        map.mapz = 0;
                                    }
                                }

                                if (BasePlayer.activePlayerList.Count > 0)
                                {
                                    foreach (BasePlayer activeplayer in BasePlayer.activePlayerList)
                                    {
                                        minimapAdd(activeplayer);
                                    }
                                }

                                string disabled = "Enabled";
                                playerMsg(player, string.Format(txtCmtMinimap, disabled));
                                return;
                            }
                            else
                            {
                                playerMsg(player, txtInvalid + txtCmdMinimap);
                            }
                        }
                    }
                    else if (args[0].ToLower() == "url")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                mapurl = args[1];
                                set("LustyMap", "MapURL", mapurl);
                                playerMsg(player, string.Format(txtCmtUrl, mapurl));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdUrl);
                    }
                    else if (args[0].ToLower() == "complex")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                mapcomplex = args[1];
                                set("LustyMap", "MapComplexURL", mapcomplex);
                                playerMsg(player, string.Format(txtCmtComplex, mapcomplex));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdComplex);
                    }
                    else if (args[0].ToLower() == "help")
                    {
                        playerMsg(player, "<color=#00ff00ff>" + lustyPlugin + "</color> v<color=#00ff00ff>" + lustyVersion + "</color>");
                        playerMsg(player, txtCmdUrl);
                        playerMsg(player, txtCmdMode);
                        playerMsg(player, txtCmdMinimap);
                        playerMsg(player, txtCmdComplex);
                        playerMsg(player, txtCmdCompass);
                        playerMsg(player, txtCmdStart);                        
                    }
                    else
                    {
                        playerMsg(player, txtUnknown);
                    }
                }
                else
                {
                    minimapRemove(player);
                    mapGUI(player);
                }
            }
        }

        [ChatCommand("zmap")]
        private void chatCmdZ(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                minimapRemove(player, true);
                fullscreenmapGUI(player, 2);
            }
        }

        List<ulong> MapOpen = new List<ulong>();

        // Console commands
        [ConsoleCommand("LustyMap")]
        private void lustyConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = (arg.Player() as BasePlayer);

            if (arg.Args == null || arg.Args.Length == 0)
            {
                PrintToConsole(player, lustyPlugin + " v" + lustyVersion);
            }
            else
            {
                if (arg.Args[0].ToLower() == "close")
                {
                    minimapRemove(player);
                }
                else if (arg.Args[0].ToLower() == "open")
                {
                    minimapAdd(player);
                }
                else if (arg.Args[0].ToLower() == "map")
                {
                    if (MinimapPlayers.Count > 0)
                    {
                        PlayerMap search = MinimapPlayers.Find(r => r.userid == player.userID);
                        if (search != null)
                        {
                            minimapRemove(player, true);
                        }
                    }


                    if (MapOpen.Find(r => r == player.userID) != player.userID)
                    {
                        mapGUI(player);
                        MapOpen.Add(player.userID);
                    }
                    else
                    {
                        minimapReopen(player);
                        catchMouse(player, false);
                        destroyUI(player, "MapGUI");
                        MapOpen.Remove(player.userID);
                    }
                }
                else if (arg.Args[0].ToLower() == "zoom")
                {
                    if (arg.Args.Length == 2)
                    {
                        int zoom = 2;
                        try { zoom = Convert.ToInt16(arg.Args[1]); }
                        catch { PrintToConsole(player, "Invlaid zoom scale detected"); }

                        if (zoom < 2)
                        {
                            mapGUI(player);
                        }
                        else
                        {
                            fullscreenmapGUI(player, zoom);
                        }
                    }
                    else
                    {
                        catchMouse(player, false);
                    }
                }
                else if (arg.Args[0].ToLower() == "return")
                {
                    minimapReopen(player);
                    catchMouse(player, false);
                    MapOpen.Remove(player.userID);
                }
            }
        }

        // Player map settings
        private class PlayerMap
        {
            public ulong userid { get; set; }
            public BasePlayer player { get; set; }
            public int mapx { get; set; }
            public int mapz { get; set; }
            public bool refresh { get; set; }
        }

        private void minimapAdd(BasePlayer player)
        {
            if (minimap)
            {
                if (MinimapPlayers.Count > 0)
                {
                    PlayerMap search = MinimapPlayers.Find(r => r.userid == player.userID);
                    if (search != null)
                    {
                        MinimapPlayers.Remove(search);
                    }
                }

                PlayerMap map = new PlayerMap();
                map.userid = player.userID;
                map.player = player;
                map.refresh = true;
                MinimapPlayers.Add(map);

                minimapBackground(map);
                minimapGUI(player);
            }
        }

        public void minimapReopen(BasePlayer player)
        {
            if (MinimapReopen.Count > 0)
            {
                BasePlayer search = MinimapReopen.Find(r => r.userID == player.userID);
                if (search != null)
                {
                    minimapAdd(player);
                    MinimapReopen.Remove(player);
                }
            }
        }

        private void minimapRemove(BasePlayer player, bool reopen = false)
        {
            if (MinimapPlayers.Count > 0)
            {
                PlayerMap search = MinimapPlayers.Find(r => r.userid == player.userID);
                if (search != null)
                {
                    MinimapPlayers.Remove(search);
                    if (reopen)
                    {
                        MinimapReopen.Add(player);
                    }
                }
            }
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("MinimapBG"));
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Minimap"));

            if (minimap)
            {
                minimapHUD(player);
            }
            else
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("MinimapHUD")); 
            }
        }

        private void minimapUpdate()
        {
            if (MinimapPlayers.Count > 0)
            {
                List<PlayerMap> remove = new List<PlayerMap>();   
                foreach (PlayerMap player in MinimapPlayers)
                {
                    try
                    {
                        minimapBackground(player);
                        minimapGUI(player.player);
                    }
                    catch
                    {
                        remove.Add(player);
                        if (debug)
                        {
                            Puts("Error Updating Minimap for " + player.player.displayName);
                        }
                    }
                }
                if (remove.Count > 0)
                {
                    foreach (PlayerMap player in remove)
                    {
                        MinimapPlayers.Remove(player);
                    }
                }
            }
        }

        // Remove player from Minimap list
        void OnPlayerDisconnected(BasePlayer player)
        {
            minimapRemove(player);
            if (startupList.Find(x => x == player.userID) != null)
            {
                startupList.Remove(player.userID);
            }
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {            
            if (startupList.Find(x => x == player.userID) != player.userID)
            {
                startupList.Add(player.userID);
                if (minimap)
                {
                    if (startopen)
                    {
                        minimapAdd(player);
                    }
                    else
                    {
                        minimapHUD(player);
                    }
                }
                player.Command("bind m \"LustyMap map\"");
            }
        }

        // Map Zoomed GUI
        private void fullscreenmapGUI(BasePlayer player, int zoom)
        {
            if (zoom < 1)
            {
                zoom = 1;
            }
            else if (zoom > 10)
            {
                zoom = 10;
            }
            
            // Setup map dimentions
            float offset = Convert.ToSingle((zoom - 1) / 2);
            float startx = 0 * zoom;
            float startz = 0 * zoom;
            float endx = 1 * zoom;
            float endz = 1 * zoom;

            // Get player pos 
            float x = Convert.ToSingle(player.transform.position.x);
            float y = Convert.ToSingle(player.transform.position.y);
            float z = Convert.ToSingle(player.transform.position.z);
            // work out percent values
            int mapsize = Convert.ToInt32(TerrainMeta.Size.x); // Thanks miRror for the code to get the mapsize
            x = Convert.ToSingle(x + (mapsize / 2));
            z = Convert.ToSingle(z + (mapsize / 2));
            float playX = Convert.ToSingle(x / mapsize);
            float playZ = Convert.ToSingle(z / mapsize);

            // Scale to zoom
            float offsetx = playX * zoom;
            float offsetz = playZ * zoom;

            // Calculate differance to center map on player
            float diffx = offsetx - 0.5f;
            float diffz = offsetz - 0.5f;

            // Shift all points relative to diff
            offsetx = offsetx - diffx;
            offsetz = offsetz - diffz;
            startx = startx - diffx;
            startz = startz - diffz;
            endx = endx - diffx;
            endz = endz - diffz;

            lustyGUI gui = new lustyGUI();
            gui.add("FullscreenMap", true, "0 0", "1 1", "0.16862 0.29803 0.33333 1");
            gui.url("FullscreenMap", "Map", mapurl, startx.ToString() + " " + startz.ToString(), endx.ToString() + " " + endz.ToString());

            gui.text("FullscreenMap", "Title", "UpperLeft", "<size=20><color=#00ff00ff>" + lustyPlugin + "</color></size>", "0.01 0.95", "0.2 0.99", "1 1 1 0.8");
            //gui.box("MapGUI", "Player", offsetx.ToString() + " " + offsetz.ToString(), (offsetx + 0.01f).ToString() + " " + (offsetz + 0.01f).ToString(), "0.8 0 0.8 1");

            gui.button("FullscreenMap", "ZoomIn", "MiddleCenter", "<size=20>+</size>", true, "LustyMap zoom " + (zoom + 1).ToString(), true, "FullscreenMap", "0.96 0.95", "0.99 0.99", "0 0 0 0.8");
            gui.button("FullscreenMap", "ZoomOut", "MiddleCenter", "<size=20>-</size>", true, "LustyMap zoom " + (zoom - 1).ToString(), true, "FullscreenMap", "0.96 0.90", "0.99 0.94", "0 0 0 0.8");

            gui.box("FullscreenMap", "Hor", "0.499 0", "0.501 1", "0 0 0 0.8");
            gui.box("FullscreenMap", "Vir", "0 0.498", "1 0.502", "0 0 0 0.8");

            gui.button("FullscreenMap", "Close", "MiddleCenter", txtBtnClose, true, "LustyMap return", true, "FullscreenMap", "0.8 0.01", "0.99 0.04", "0 0 0.5 0.9");
            catchMouse(player, true);
            gui.send(player);
        }

        // Map GUI
        private void mapGUI(BasePlayer player)
        {
            string direction = null;
            double lookRotation = player.eyes.rotation.eulerAngles.y;
            if (lookRotation > 337.5 || lookRotation < 22.5) { direction = txtCpsN; }
            else if (lookRotation > 22.5 && lookRotation < 67.5) { direction = txtCpsNE; }
            else if (lookRotation > 67.5 && lookRotation < 112.5) { direction = txtCpsE; }
            else if (lookRotation > 112.5 && lookRotation < 157.5) { direction = txtCpsSE; }
            else if (lookRotation > 157.5 && lookRotation < 202.5) { direction = txtCpsS; }
            else if (lookRotation > 202.5 && lookRotation < 247.5) { direction = txtCpsSW; }
            else if (lookRotation > 247.5 && lookRotation < 292.5) { direction = txtCpsW; }
            else if (lookRotation > 292.5 && lookRotation < 337.5) { direction = txtCpsNW; }
            
            float x = Convert.ToSingle(player.transform.position.x);
            float y = Convert.ToSingle(player.transform.position.y);
            float z = Convert.ToSingle(player.transform.position.z);

            int mapsize = Convert.ToInt32(TerrainMeta.Size.x); // Thanks miRror for the code to get the mapsize
            x = Convert.ToSingle(x + (mapsize / 2));
            z = Convert.ToSingle(z + (mapsize / 2));
            float mapX = Convert.ToSingle(x / mapsize);
            float mapZ = Convert.ToSingle(z / mapsize);

            lustyGUI gui = new lustyGUI();
            gui.add("MapGUI", true, "0.25 0.13", "0.75 0.95", "0 0 0 1");
            gui.box("MapGUI", "TitleBox", "0 1", "0.999999999 1.05", "0 0.3 0.3 1");
            gui.text("MapGUI", "Title", "MiddleLeft", "<size=20>" + lustyPlugin + " v" + lustyVersion + "</size>", "0.01 1", "0.999999999 1.05", "0 0 0 0");
            gui.button("MapGUI", "Close", "MiddleCenter", "X", true, "LustyMap return", true, "MapGUI", "0.9 1", "0.999999999 1.05", "0.4 0.1 0.1 1");
            gui.url("MapGUI", "Map", mapurl, "0 0", "1 1");

            float iconsize = 0.01f;
            foreach (lustyMonuments monument in Monuments)
            {
                gui.url("MapGUI", "Mon" + DateTime.Now.Ticks.ToString(), monument.icon, (monument.percentX - iconsize).ToString() + " " + (monument.percentZ - iconsize).ToString(), (monument.percentX + iconsize).ToString() + " " + (monument.percentZ + iconsize).ToString());
                gui.text("MapGUI", "TxT" + DateTime.Now.Ticks.ToString(), "UpperCenter", "<size=10>" + monument.name + "</size>", (monument.percentX - 0.1).ToString() + " " + (monument.percentZ - iconsize - 0.05).ToString(), (monument.percentX + 0.1).ToString() + " " + (monument.percentZ).ToString(), "1 1 1 0.8");
            }

            //gui.box("MapGUI", "PlayerX", (mapX - 0.001f).ToString() + " 0", (mapX + 0.001f).ToString() + " 1", "0 0 0 1");
            //gui.box("MapGUI", "PlayerY", "0 " + (mapZ - 0.001f).ToString(), "1 " + (mapZ + 0.001f).ToString(), "0 0 0 1");
            gui.box("MapGUI", "PlayerX", (mapX - 0.001f).ToString() + " " + (mapZ - 0.0125f).ToString(), (mapX + 0.001f).ToString() + " " + (mapZ + 0.0125f).ToString(), "0 0.8 0.2 1");
            gui.box("MapGUI", "PlayerY", (mapX - 0.0125f).ToString() + " " + (mapZ - 0.001f).ToString(), (mapX + 0.0125f).ToString() + " " + (mapZ + 0.001f).ToString(), "0 0.8 0.2 1");
            //gui.url("MapGUI", "Self", "http://map.playrust.io/img/self.png", (mapX - 0.01f).ToString() + " " + (mapZ - 0.01f).ToString(), (mapX + 0.01f).ToString() + " " + (mapZ + 0.01f).ToString());   
            
            gui.text("MapGUI", "Direction", "UpperRight", "<size=16>" + txtCpsHead + " " + direction + "</size>\n<size=12>" + player.transform.position.ToString() + "</size>", "0.6 0.9", "0.99 0.99", "1 1 1 0.8");

            gui.send(player);
        }

        // Minimap Menu
        private void minimapHUD(BasePlayer player, bool close = false)
        {
            // Map alignment
            float offset = 0f;
            if (!left)
            {
                offset = 0.87f;
            }

            // Draw GUI
            lustyGUI gui = new lustyGUI();
            gui.add("MinimapHUD", false, offset.ToString() + " 0.97", (0.13 + offset).ToString() + " 1", "0 0 0 0");

            if (left)
            {
                if (close)
                {
                    gui.button("MinimapHUD", "MinimapClose", "MiddleCenter", "<size=12><<<</size>", true, "LustyMap close", true, "MinimapHUD", "1 0", "1.15 1", "0 0 0 0.6");
                    if (mapbutton)
                    {
                        gui.button("MinimapHUD", "MapOpen", "MiddleCenter", "<size=12>" + txtBtnMap + "</size>", true, "LustyMap map", true, "MinimapHUD", "1 -1.1", "1.15 -0.1", "0 0 0 0.6");
                    }
                }
                else
                {
                    gui.button("MinimapHUD", "MinimapOpen", "MiddleCenter", "<size=12>>>></size>", true, "LustyMap open", true, "MinimapHUD", "0 0", "0.15 1", "0 0 0 0.6");
                }
            }
            else
            {
                if (close)
                {
                    gui.button("MinimapHUD", "MinimapClose", "MiddleCenter", "<size=12>>>></size>", true, "LustyMap close", true, "MinimapHUD", "-0.15 0", "0 1", "0 0 0 0.6");
                    gui.button("MinimapHUD", "MapOpen", "MiddleCenter", "<size=12>" + txtBtnMap + "</size>", true, "LustyMap map", true, "MinimapHUD", "-0.15 -1.1", "0 -0.1", "0 0 0 0.6");
                }
                else
                {
                    gui.button("MinimapHUD", "MinimapOpen", "MiddleCenter", "<size=12><<<</size>", true, "LustyMap open", true, "MinimapHUD", "0.85 0", "1 1", "0 0 0 0.6");
                }
            }
            gui.send(player);
        }

        // Minimap
        private void minimapGUI(BasePlayer player)
        {
            // Map alignment
            float offset = 0f;
            if (!left)
            {
                offset = 0.87f;
            }

            // Complex mode
            if (mapmode)
            {
                string direction = null;
                double lookRotation = player.eyes.rotation.eulerAngles.y;
                if (lookRotation > 337.5 || lookRotation < 22.5) { direction = txtCpsN; }
                else if (lookRotation > 22.5 && lookRotation < 67.5) { direction = txtCpsNE; }
                else if (lookRotation > 67.5 && lookRotation < 112.5) { direction = txtCpsE; }
                else if (lookRotation > 112.5 && lookRotation < 157.5) { direction = txtCpsSE; }
                else if (lookRotation > 157.5 && lookRotation < 202.5) { direction = txtCpsS; }
                else if (lookRotation > 202.5 && lookRotation < 247.5) { direction = txtCpsSW; }
                else if (lookRotation > 247.5 && lookRotation < 292.5) { direction = txtCpsW; }
                else if (lookRotation > 292.5 && lookRotation < 337.5) { direction = txtCpsNW; }

                float x = Convert.ToSingle(player.transform.position.x);
                float y = Convert.ToSingle(player.transform.position.y);
                float z = Convert.ToSingle(player.transform.position.z);

                int mapsize = Convert.ToInt32(TerrainMeta.Size.x); // Thanks miRror for the code to get the mapsize
                x = Convert.ToSingle(x + (mapsize / 2));
                z = Convert.ToSingle(z + (mapsize / 2));

                int mapres = mapsize / mapslices;
                double mapX = Convert.ToSingle(x / mapres);
                double mapZ = Convert.ToSingle(z / mapres);

                mapX = mapX - Math.Truncate(mapX);
                mapZ = mapZ - Math.Truncate(mapZ);

                int row = 3;
                int col = 3;

                mapX = (mapX / col) + (1f / col);
                mapZ = (mapZ / row) + (1f / row);

                mapX = Math.Round(mapX, 4);
                mapZ = Math.Round(mapZ, 4);

                // Draw GUI
                lustyGUI gui = new lustyGUI();
                gui.add("Minimap", false, offset.ToString() + " 0.7699", (0.13 + offset).ToString() + " 1", "0 0 0 0");

                //gui.box("Minimap", "PlayerX", (mapX - 0.001f).ToString() + " 0", (mapX + 0.001f).ToString() + " 1", "0 0 0 1");
                //gui.box("Minimap", "PlayerY", "0 " + (mapZ - 0.001f).ToString(), "1 " + (mapZ + 0.001f).ToString(), "0 0 0 1");
                gui.box("Minimap", "PlayerX", (mapX - 0.001f).ToString() + " " + (mapZ - 0.06f).ToString(), (mapX + 0.001f).ToString() + " " + (mapZ + 0.06f).ToString(), "0 0.8 0.2 1");
                gui.box("Minimap", "PlayerY", (mapX - 0.06f).ToString() + " " + (mapZ - 0.001f).ToString(), (mapX + 0.06f).ToString() + " " + (mapZ + 0.001f).ToString(), "0 0.8 0.2 1");

                if (compass)
                {
                    gui.text("Minimap", "Location", "UpperCenter", "<size=14>" + txtCpsHead + " " + direction + "</size>\n<size=12>" + player.transform.position.ToString() + "</size>", "0 -0.3", "1 0", "1 1 1 0.8");
                }

                gui.send(player);
            }
            // Simple mode
            else
            {
                string direction = null;
                double lookRotation = player.eyes.rotation.eulerAngles.y;
                if (lookRotation > 337.5 || lookRotation < 22.5) { direction = txtCpsN; }
                else if (lookRotation > 22.5 && lookRotation < 67.5) { direction = txtCpsNE; }
                else if (lookRotation > 67.5 && lookRotation < 112.5) { direction = txtCpsE; }
                else if (lookRotation > 112.5 && lookRotation < 157.5) { direction = txtCpsSE; }
                else if (lookRotation > 157.5 && lookRotation < 202.5) { direction = txtCpsS; }
                else if (lookRotation > 202.5 && lookRotation < 247.5) { direction = txtCpsSW; }
                else if (lookRotation > 247.5 && lookRotation < 292.5) { direction = txtCpsW; }
                else if (lookRotation > 292.5 && lookRotation < 337.5) { direction = txtCpsNW; }

                float x = Convert.ToSingle(player.transform.position.x);
                float y = Convert.ToSingle(player.transform.position.y);
                float z = Convert.ToSingle(player.transform.position.z);

                int mapsize = Convert.ToInt32(TerrainMeta.Size.x); // Thanks miRror for the code to get the mapsize
                x = Convert.ToSingle(x + (mapsize / 2));
                z = Convert.ToSingle(z + (mapsize / 2));
                float mapX = Convert.ToSingle(x / mapsize);
                float mapZ = Convert.ToSingle(z / mapsize);

                lustyGUI gui = new lustyGUI();
                gui.add("Minimap", false, offset.ToString() + " 0.7699", (0.13 + offset).ToString() + " 1", "0 0 0 0");

                //gui.box("Minimap", "PlayerX", (mapX - 0.001f).ToString() + " 0", (mapX + 0.001f).ToString() + " 1", "0 0 0 1");
                //gui.box("Minimap", "PlayerY", "0 " + (mapZ - 0.001f).ToString(), "1 " + (mapZ + 0.001f).ToString(), "0 0 0 1");
                gui.box("Minimap", "PlayerX", (mapX - 0.001f).ToString() + " " + (mapZ - 0.06f).ToString(), (mapX + 0.001f).ToString() + " " + (mapZ + 0.06f).ToString(), "0 0.8 0.2 1");
                gui.box("Minimap", "PlayerY", (mapX - 0.06f).ToString() + " " + (mapZ - 0.001f).ToString(), (mapX + 0.06f).ToString() + " " + (mapZ + 0.001f).ToString(), "0 0.8 0.2 1");

                if (compass)
                {
                    gui.text("Minimap", "Location", "MiddleCenter", "<size=12>" + txtCpsHead + " " + direction + "\n" + player.transform.position.ToString() + "</size>", "0 -0.3", "1 0", "1 1 1 0.8");
                }
                gui.send(player);
            }
        }

        // Minimap background
        private void minimapBackground(PlayerMap player)
        {
            // Map alignment
            float offset = 0f;
            if (!left)
            {
                offset = 0.87f;
            }

            // Complex
            if (mapmode)
            {
                // Get center map part 
                float x = Convert.ToSingle(player.player.transform.position.x);
                float z = Convert.ToSingle(player.player.transform.position.z);
                int mapsize = Convert.ToInt32(TerrainMeta.Size.x); // Thanks miRror for the code to get the mapsize
                x = Convert.ToSingle(x + (mapsize / 2));
                z = Convert.ToSingle(z + (mapsize / 2));

                int mapres = mapsize / mapslices;
                int currentx = Convert.ToInt32(Math.Ceiling(x / mapres)) - 2;
                int currentz = mapslices - Convert.ToInt32(Math.Ceiling(z / mapres)) - 1;

                // Check if it has changed
                if (player.mapx != currentx || player.mapz != currentz)
                {
                    player.mapx = currentx;
                    player.mapz = currentz;

                    // Start creating GUI
                    lustyGUI gui = new lustyGUI();
                    gui.add("MinimapBG", false, offset.ToString() + " 0.7699", (0.13 + offset).ToString() + " 1", "0 0 0 0");

                    // Map parts
                    int row = 3;
                    int col = 3;
                    for (int r = 0; r < row; r++)
                    {
                        for (int c = 0; c < col; c++)
                        {
                            string maplink = mapcomplex + "map-" + (currentz + r).ToString() + "-" + (currentx + c).ToString() + ".jpeg";
                            string sx = Convert.ToSingle(c * (1f / col)).ToString();
                            string sy = Convert.ToSingle(1 - ((1f / row) * (r + 1))).ToString();
                            string ex = Convert.ToSingle(((c + 1) * (1f / col)) - 0.005f).ToString();
                            string ey = Convert.ToSingle((1 - ((1f / row) * (r + 1)) + (1f / row)) - 0.004f).ToString();
                            gui.url("MinimapBG", "Map", maplink, sx + " " + sy, ex + " " + ey, "0.9");

                            float iconsize = 0.05f;
                            foreach (lustyMonuments monument in Monuments)
                            {
                                if (monument.column == (currentz + r) && monument.row == (currentx + c))
                                {
                                    float _sx = Convert.ToSingle(c * (1f / col));
                                    float _sy = Convert.ToSingle(1 - ((1f / row) * (r + 1)));
                                    float _ex = Convert.ToSingle(((c + 1) * (1f / col)) - 0.005f);
                                    float _ey = Convert.ToSingle((1 - ((1f / row) * (r + 1)) + (1f / row)) - 0.004f);
                                    double mapX = (monument.percentX * mapslices) - monument.row;
                                    double mapZ = ((1 - monument.percentZ) * mapslices) - monument.column;
                                    float _xd = _ex - _sx;
                                    mapX = (mapX * _xd) + _sx;
                                    float _yd = _ey - _sy;
                                    mapZ = _ey - (mapZ * _yd);

                                    gui.url("MinimapBG", DateTime.Now.Ticks.ToString(), monument.icon, (mapX - iconsize).ToString() + " " + (mapZ - iconsize).ToString(), (mapX + iconsize).ToString() + " " + (mapZ + iconsize).ToString(), "1");
                                    //Puts(monument.name + " " + monument.percentX + " (" + monument.row + ") " + " (" + mapX + ") " + monument.percentZ + " (" + monument.column + ")" + " (" + mapZ + ") ");
                                    //Puts(maplink + " " + (currentz + r) + " " + (currentx + c));
                                }
                            }
                        }
                    }

                    gui.send(player.player);
                    //Puts(player.player.displayName + " " + player.mapx + " " + player.mapz);

                    // Display close button
                    minimapHUD(player.player, true);
                }
            }
            // Simple
            else
            {
                if (player.refresh)
                {
                    lustyGUI gui = new lustyGUI();
                    gui.add("MinimapBG", false, offset.ToString() + " 0.7699", (0.13 + offset).ToString() + " 1", "0 0 0 1");
                    gui.url("MinimapBG", "Map", mapurl, "0 0", "1 1");

                    float iconsize = 0.02f;
                    foreach (lustyMonuments monument in Monuments)
                    {
                        gui.url("MinimapBG", "Mon" + DateTime.Now.Ticks.ToString(), monument.icon, (monument.percentX - iconsize).ToString() + " " + (monument.percentZ - iconsize).ToString(), (monument.percentX + iconsize).ToString() + " " + (monument.percentZ + iconsize).ToString());
                    }

                    gui.send(player.player);

                    // Display close button
                    minimapHUD(player.player, true);

                    // Finished updating the map background
                    player.refresh = false;
                }
            }
        }

        // Map Users
        private class lustyUsers
        {
            public ulong userid { get; set; }
            public List<lustyMarkers> markers { get; set; }
            public bool minimap { get; set; }
            public bool minmapleft { get; set; }

        }

        // Map Markers
        private class lustyMarkers
        {
            public int id { get; set; }
            public string name { get; set; }
            public float x { get; set; }
            public float y { get; set; }
        }
        
        // GUI Class
        private class lustyGUIv3a
        {
            private string gui = null;
            private string guiname = null;
            public string rand = null;
            private string startblock = @"{""name"": ""{name}{rand}"",""parent"": ""{blockparent}{rand}"",""components"":[";
            private string imageblock = @"{""type"":""UnityEngine.UI.Image"", ""color"":""{color}""}";
            private string textblock = @"{""type"":""UnityEngine.UI.Text"",""text"":""{text}"",""fontSize"":20,""align"": ""{align}""}";
            private string anchorblock = @"{""type"":""RectTransform"",""anchormin"": ""{start}"",""anchormax"": ""{end}""}";
            private string buttonblock = @"{""type"":""UnityEngine.UI.Button"",{closeblock}{commandblock}""color"": ""{color}"",""imagetype"": ""Tiled""}";
            private string buttonclose = @"""close"":""{parent}"",";
            private string buttoncmd = @"""command"":""{command}"",";
            private string buttontext = @"{""parent"": ""{name}{rand}"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{text}"",""fontSize"":16,""align"": ""{align}""},{""type"":""RectTransform"",""anchormin"": ""0 0"",""anchormax"": ""1 1""}]}";
            private string urlblock = @"{""type"":""UnityEngine.UI.RawImage"",""imagetype"": ""Tiled"",""url"": ""{url}"",""color"": ""1 1 1 {alpha}""}";
            private string cursorblock = @"{""type"":""NeedsCursor""}";
            private string endblock = "]}";

            public void add(string name, bool mouse, string start, string end, string color)
            {
                guiname = name;
                gui = "[" + startblock + imageblock + "," + anchorblock;
                if (mouse) { gui = gui + "," + cursorblock; }
                gui = gui + endblock;


                gui = gui.Replace("{blockparent}{rand}", "HUD/Overlay");
                gui = gui.Replace("{name}{rand}", "{parent}");
                gui = gui.Replace("{color}", color);
                gui = gui.Replace("{start}", start);
                gui = gui.Replace("{end}", end);
            }

            public void box(string parent, string name, string start, string end, string color)
            {
                gui = gui + "," + startblock + imageblock + "," + anchorblock + endblock;

                if (parent == "{parent}")
                {
                    gui = gui.Replace("{blockparent}{rand}", "{parent}");
                }
                else
                {
                    gui = gui.Replace("{blockparent}", parent);
                }
                gui = gui.Replace("{name}", name);
                gui = gui.Replace("{color}", color);
                gui = gui.Replace("{start}", start);
                gui = gui.Replace("{end}", end);
            }

            public void url(string parent, string name, string url, string start, string end, float alpha = 1f)
            {
                gui = gui + "," + startblock + urlblock + "," + anchorblock + endblock;

                if (parent == "{parent}")
                {
                    gui = gui.Replace("{blockparent}{rand}", "{parent}");
                }
                else
                {
                    gui = gui.Replace("{blockparent}", parent);
                }
                gui = gui.Replace("{name}", name);
                gui = gui.Replace("{url}", url);
                gui = gui.Replace("{start}", start);
                gui = gui.Replace("{end}", end);
                gui = gui.Replace("{alpha}", alpha.ToString());
            }

            public void text(string parent, string name, string align, string text, string start, string end)
            {
                gui = gui + "," + startblock + textblock + "," + anchorblock + endblock;

                if (parent == "{parent}")
                {
                    gui = gui.Replace("{blockparent}{rand}", "{parent}");
                }
                else
                {
                    gui = gui.Replace("{blockparent}", parent);
                }
                gui = gui.Replace("{name}", name);
                gui = gui.Replace("{align}", align);
                gui = gui.Replace("{text}", text);
                gui = gui.Replace("{start}", start);
                gui = gui.Replace("{end}", end);
            }

            public void button(string parent, string name, string align, string text, bool cmd, string command, bool cls, string start, string end, string color)
            {
                gui = gui + "," + startblock + buttonblock + "," + anchorblock + endblock + "," + buttontext;

                if (cls)
                {
                    gui = gui.Replace("{closeblock}", buttonclose);
                }
                else
                {
                    gui = gui.Replace("{closeblock}", "");
                }
                if (cmd)
                {
                    gui = gui.Replace("{commandblock}", buttoncmd);
                    gui = gui.Replace("{command}", command);
                }
                else
                {
                    gui = gui.Replace("{commandblock}", "");
                }

                if (parent == "{parent}")
                {
                    gui = gui.Replace("{blockparent}{rand}", "{parent}");
                }
                else
                {
                    gui = gui.Replace("{blockparent}", parent);
                }
                gui = gui.Replace("{name}", name);
                gui = gui.Replace("{color}", color);
                gui = gui.Replace("{align}", align);
                gui = gui.Replace("{text}", text);
                gui = gui.Replace("{start}", start);
                gui = gui.Replace("{end}", end);
            }

            public string json()
            {
                string json = gui.Replace("{rand}", this.rand).Replace("{parent}", this.guiname) + "]";
                return json;
            }

            public void send(BasePlayer player)
            {
                if (guiname != null)
                {
                    this.rand = DateTime.Now.Ticks.ToString();
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList(guiname));
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(gui.Replace("{rand}", this.rand).Replace("{parent}", this.guiname) + "]"));
                }
            }
        }

        // GUI Class - v2
        private class lustyGUI
        {
            private string gui = null;
            private string guiname = null;
            private string startblock = @"{""name"": ""{name}{rand}"",""parent"": ""{parent}{rand}"",""components"":[";
            private string imageblock = @"{""type"":""UnityEngine.UI.Image"", ""color"":""{color}""}";
            private string textblock = @"{""type"":""UnityEngine.UI.Text"",""text"":""{text}"",""fontSize"":20,""align"": ""{align}""}";
            private string anchorblock = @"{""type"":""RectTransform"",""anchormin"": ""{start}"",""anchormax"": ""{end}""}";
            private string buttonblock = @"{""type"":""UnityEngine.UI.Button"",{closeblock}{commandblock}""color"": ""{color}"",""imagetype"": ""Tiled""}";
            private string buttonclose = @"""close"":""{topparent}{rand}"",";
            private string buttoncmd = @"""command"":""{command}"",";
            private string buttontext = @"{""parent"": ""{name}{rand}"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{text}"",""fontSize"":16,""align"": ""{align}""},{""type"":""RectTransform"",""anchormin"": ""0 0"",""anchormax"": ""1 1""}]}";
            private string urlblock = @"{""type"":""UnityEngine.UI.RawImage"",""imagetype"": ""Tiled"",""sprite"": ""assets/content/textures/generic/fulltransparent.tga"",""color"": ""0.7 0.7 0.7 {alpha}"",""url"": ""{url}""}";
            private string cursorblock = @"{""type"":""NeedsCursor""}";
            private string endblock = "]}";

            public string returngui()
            {
                gui = gui + "]";
                gui = gui.Replace("{rand}", "");
                return gui;
            }

            public string returnguirand(string rand)
            {
                gui = gui + "]";
                gui = gui.Replace("{rand}", rand);
                return gui;
            }

            public void add(string name, bool mouse, string start, string end, string color)
            {
                guiname = name;
                gui = "[" + startblock + imageblock + "," + anchorblock;
                if (mouse) { gui = gui + "," + cursorblock; }
                gui = gui + endblock;

                gui = gui.Replace("{parent}{rand}", "HUD/Overlay");
                gui = gui.Replace("{name}", name);
                gui = gui.Replace("{color}", color);
                gui = gui.Replace("{start}", start);
                gui = gui.Replace("{end}", end);
            }

            public void box(string parent, string name, string start, string end, string color)
            {
                gui = gui + "," + startblock + imageblock + "," + anchorblock + endblock;

                gui = gui.Replace("{parent}", parent);
                gui = gui.Replace("{name}", name);
                gui = gui.Replace("{color}", color);
                gui = gui.Replace("{start}", start);
                gui = gui.Replace("{end}", end);
            }

            public void url(string parent, string name, string url, string start, string end, string alpha = "1")
            {
                gui = gui + "," + startblock + urlblock + "," + anchorblock + endblock;

                gui = gui.Replace("{parent}", parent);
                gui = gui.Replace("{name}", name);
                gui = gui.Replace("{url}", url);
                gui = gui.Replace("{start}", start);
                gui = gui.Replace("{end}", end);
                gui = gui.Replace("{alpha}", alpha);
            }

            public void text(string parent, string name, string align, string text, string start, string end, string color)
            {
                gui = gui + "," + startblock + textblock + "," + anchorblock + endblock;

                gui = gui.Replace("{parent}", parent);
                gui = gui.Replace("{name}", name);
                gui = gui.Replace("{align}", align);
                gui = gui.Replace("{text}", text);
                gui = gui.Replace("{start}", start);
                gui = gui.Replace("{end}", end);
            }

            public void button(string parent, string name, string align, string text, bool cmd, string command, bool cls, string topparent, string start, string end, string color)
            {
                gui = gui + "," + startblock + buttonblock + "," + anchorblock + endblock + "," + buttontext;

                if (cls)
                {
                    gui = gui.Replace("{closeblock}", buttonclose);
                    gui = gui.Replace("{topparent}", topparent);
                }
                else
                {
                    gui = gui.Replace("{closeblock}", "");
                }
                if (cmd)
                {
                    gui = gui.Replace("{commandblock}", buttoncmd);
                    gui = gui.Replace("{command}", command);
                }
                else
                {
                    gui = gui.Replace("{commandblock}", "");
                }

                gui = gui.Replace("{parent}", parent);
                gui = gui.Replace("{name}", name + DateTime.Now.Ticks.ToString());
                gui = gui.Replace("{color}", color);
                gui = gui.Replace("{align}", align);
                gui = gui.Replace("{text}", text);
                gui = gui.Replace("{start}", start);
                gui = gui.Replace("{end}", end);
            }

            public void send(BasePlayer player)
            {
                if (guiname != null)
                {                    
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList(guiname));
                    string final = gui.Replace("{rand}", "") + "]";
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(final));
                }
            }
        }

        private void destroyUI(BasePlayer player, string name)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList(name));
        }
        private void addUI(BasePlayer player, string gui)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(gui));
        }

        private void catchMouse(BasePlayer player, bool state)
        {
            if (state)
            {
                lustyGUIv3a mouse = new lustyGUIv3a();
                mouse.add("CatchMouse", true, "0 0", "0 0", "0 0 0 0");
                addUI(player, mouse.json());
            }
            else
            {
                destroyUI(player, "CatchMouse");
            }
        }


        // Player Messages
        private void playerMsg(BasePlayer player, string msg)
        {
            SendReply(player, String.Format("<color=#008080ff>Lusty Map</color>: {0}", msg));
        }

        private void globalMsg(string msg)
        {
            PrintToChat(String.Format("<color=#008080ff>Lusty Map</color>: {0}", msg));
        }

        // Permissions Check
        private bool isAdmin(BasePlayer player)
        {
            if (player.net.connection.authLevel >= 1)
            {
                return true;
            }
            return false;
        }
        
        // Config stuff
        private void LoadDefaultConfig() { }

        // Get config item
        private object get(string item, string subitem)
        {
            try
            {
                if (Config[item, subitem] != null)
                {
                    return Config[item, subitem];
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        // Set config item
        private void set(string item, string subitem, object data, bool overwrite = true)
        {
            try
            {
                if (!overwrite & Config[item, subitem] == null)
                {
                    Config[item, subitem] = data;
                }
                else if (overwrite)
                {
                    Config[item, subitem] = data;
                }
                SaveConfig();
            }
            catch
            {

            }
        }

        // Clear config item
        private void clear(string item, string subitem)
        {
            try
            {
                Config[item, subitem] = null;
                SaveConfig();
            }
            catch
            {

            }
        }
    }
}