using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.IO;
using System.Reflection;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("LustyMap", "Kayzor", "1.1.10", ResourceId = 1333)]
    [Description("In-game Map and Minimap GUI")]
    public class LustyMap : RustPlugin
    {
        // System variables
        string mapurl = null;
        bool run = false;
        static bool debug = false;
        bool mapmode = false;
        bool minimap = true;
        bool left = true;
        bool compass = true;
        bool startopen = true;
        bool mapbutton = false;
        bool showmonuments = true;
        bool showcaves = true;
        bool showheli = true;
        bool showplane = true;
        bool showsupply = true;
        bool showdebris = true;
        bool useurl = false;
        bool rustiofriends = false;
        bool rustio = false;
        bool friendapi = false;
        bool friendsapifriends = false;

        // Lists
        List<MapLocation> mapCustom = new List<MapLocation>();
        List<MapLocation> mapMonuments = new List<MapLocation>();
        List<ActiveEntity> activeEntities = new List<ActiveEntity>();
        List<string> customIamges = new List<string>();

        // Text Strings
        string txtInvalid = null;
        string txtUnknown = null;
        string txtCmdMinimap = null;
        string txtCmdMode = null;
        string txtCmdCompass = null;
        string txtCmdStart = null;
        string txtCmdCaves = null;
        string txtCmdHeli = null;
        string txtCmdPlane = null;
        string txtCmdSupply = null;
        string txtCmdDebris = null;
        string txtCmdMonuments = null;
        string txtCmdImages = null;
        string txtCmdUrl = null;
        string txtCmdMap = null;
        string txtCmdIOFriends = null;
        string txtCmdAPIFriends = null;
        string txtCmdAdmin = null;

        string txtCmtMode = null;
        string txtCmdLocation = null;
        string txtCmdImage = null;
        string txtCmtMinimap = null;
        string txtCmtAlign = null;
        string txtCmtCompass = null;
        string txtCmtOpen = null;
        string txtCmtCaves = null;
        string txtCmtHeli = null;
        string txtCmtPlane = null;
        string txtCmtSupply = null;
        string txtCmtDebris = null;
        string txtCmtMonuments = null;
        string txtCmtImages = null;
        string txtCmtUrl = null;
        string txtCmtLocation = null;
        string txtCmtLocationFail = null;
        string txtCmtImage = null;
        string txtCmtImageFail = null;
        string txtCmtIOFriends = null;
        string txtCmtAPIFriends = null;
        string txtCmtAdmin = null;

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

        private float mapSize;

        // RustIO Support
        private Library RustIOLib;
        private MethodInfo isRustIOInstalled;
        private MethodInfo hasRustIOFriend;

        private void InitializeRustIO()
        {
            RustIOLib = Interface.GetMod().GetLibrary<Library>("RustIO");
            if (RustIOLib == null || (isRustIOInstalled = RustIOLib.GetFunction("IsInstalled")) == null || (hasRustIOFriend = RustIOLib.GetFunction("HasFriend")) == null)
            {
                RustIOLib = null;
            }

            if (!IsRustIOInstalled())
            {
                rustio = false;
                if (debug) { Puts("Rust:IO not detected..."); }
            }
            else
            {
                rustio = true;
            }
        }

        private bool IsRustIOInstalled()
        {
            if (RustIOLib == null) return false;
            return (bool)isRustIOInstalled.Invoke(RustIOLib, new object[] { });
        }

        private bool HasRustIOFriend(string playerId, string friendId)
        {
            if (RustIOLib == null) return false;
            return (bool)hasRustIOFriend.Invoke(RustIOLib, new object[] { playerId, friendId });
        }

        // Friends API Support
        [PluginReference]
        private Plugin Friends;

        private void InitializeFriendsAPI()
        {
            if (Friends != null)
            {
                friendapi = true;
            }
            else
            {
                friendapi = false;
                if (debug) { Puts("FriendsAPI not detected..."); }
            }
        }

        private bool HasFriendsAPIFriend(string playerId, string friendId)
        {
            try
            {
                bool result = (bool)Friends?.CallHook("HasFriend", playerId, friendId);
                return result;
            }
            catch
            {
                return false;
            }
        }

        // Plugin Setup
        void Loaded()
        {
            // Default config values
            set("LustyMap", "MapURL", "http://185.38.151.245:28015/map.jpg", false);
            set("LustyMap", "MapMode", false, false);
            set("LustyMap", "Minimap", true, false);
            set("LustyMap", "Left", true, false);
            set("LustyMap", "Compass", true, false);
            set("LustyMap", "StartOpen", true, false);
            set("LustyMap", "Debug", false, false);
            set("LustyMap", "MapButton", false, false);
            set("LustyMap", "ShowMonuments", true, false);
            set("LustyMap", "ShowCaves", true, false);
            set("LustyMap", "ShowHeli", true, false);
            set("LustyMap", "ShowPlane", true, false);
            set("LustyMap", "ShowSupply", true, false);
            set("LustyMap", "ShowDebris", true, false);
            set("LustyMap", "RustIOFriends", false, false);
            set("LustyMap", "FriendsAPIFriends", false, false);
            set("LustyMap", "UseURL", true, false);

            // Default/English text strings
            set("TextStrings", "InvalidSyntex", "Invalid syntex! usage: ", false);
            set("TextStrings", "UnknownCommand", "Unknown command, type <color=#00ff00ff>/map help</color> for a list of commands.", false);
            set("TextStrings", "MinimapCommand", "<color=#00ff00ff>/map minimap <true|false|left|right></color> - Enables, disables or sets the default alignment for the minimap.", false);
            set("TextStrings", "ModeCommand", "<color=#00ff00ff>/map mode <true|false></color> - Enables or disables complex mode.", false);
            set("TextStrings", "CompassCommand", "<color=#00ff00ff>/map compass <true|false></color> - Enables or disables the minimap compass.", false);
            set("TextStrings", "StartCommand", "<color=#00ff00ff>/map startopen <true|false></color> - Sets the default state for the minimap.", false);
            set("TextStrings", "CavesCommand", "<color=#00ff00ff>/map showcaves <true|false></color> - Enables or disables Caves from showing on the map.", false);
            set("TextStrings", "MonumentsCommand", "<color=#00ff00ff>/map showmonuments <true|false></color> - Enables or disables Monuments from showing on the map.", false);
            set("TextStrings", "ImagesCommand", "<color=#00ff00ff>/map images</color> - Reloads the Image cache.", false);
            set("TextStrings", "UrlCommand", "<color=#00ff00ff>/map url <url to map image></color> - Sets the map image, used as the background image.", false);
            set("TextStrings", "PlaneCommand", "<color=#00ff00ff>/map showplane <true|false></color> - Enables or disables Airplanes from showing on the map.", false);
            set("TextStrings", "HeliCommand", "<color=#00ff00ff>/map showheli <true|false></color> - Enables or disables Helicopters from showing on the map.", false);
            set("TextStrings", "SupplyCommand", "<color=#00ff00ff>/map showsupply <true|false></color> - Enables or disables Supply Drops from showing on the map.", false);
            set("TextStrings", "DebrisCommand", "<color=#00ff00ff>/map showdebris <true|false></color> - Enables or disables Helicopter Debris from showing on the map.", false);
            set("TextStrings", "LocationCommand", "<color=#00ff00ff>/map <add|remove> <name> (optional)<image name></color> - Adds|Removes a location from the map, (optional)using a custom image.", false);
            set("TextStrings", "ImageCommand", "<color=#00ff00ff>/map <addimage|removeimage> <name></color> - Adds a custom image which can be used with a custom map location.", false);
            set("TextStrings", "MapCommand", "<color=#00ff00ff>/map</color> has been <color=#00ff00ff>removed</color>, Please use keybind <color=#00ff00ff>M</color> to toggle the map instead.", false);
            set("TextStrings", "RustIOFriendsCommand", "<color=#00ff00ff>/map rustiofriends <true|false></color> - Enables or disables RustIO Friends displaying on the map and minimap for players.", false);
            set("TextStrings", "FriendsAPIFriendsCommand", "<color=#00ff00ff>/map friendsapifriends <true|false></color> - Enables or disables FriendsAPI Friends displaying on the map and minimap for players.", false);
            set("TextStrings", "AdminViewCommand", "<color=#00ff00ff>/map adminview</color> - Toggles Admin View for the map and minimap.", false);

            set("TextStrings", "ModeCommit", "Complex mode has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "MinimapCommit", "Minimap has been <color=#00ff00ff>{0}</color> for all players.", false);
            set("TextStrings", "AlignCommit", "Minimap has been set to the <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "CompassCommit", "Minimap compass has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "OpenCommit", "Minimap will be <color=#00ff00ff>{0}</color> by default.", false);
            set("TextStrings", "CavesCommit", "Showing Caves has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "PlaneCommit", "Showing Airplanes has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "HeliCommit", "Showing Helicopters has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "SupplyCommit", "Showing Supply Drops has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "DebrisCommit", "Showing Helicopter Debris has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "MonumentsCommit", "Showing Monuments has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "ImagesCommit", "Reloading the Image cache.", false);
            set("TextStrings", "UrlCommit", "Map URL has been set to: <color=#00ff00ff>{0}</color>", false);
            set("TextStrings", "RustIOFriendsCommit", "RustIO Friends has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "FriendsAPIFriendsCommit", "FriendsAPI Friends has been <color=#00ff00ff>{0}</color>.", false);
            set("TextStrings", "AdminViewCommit", "Admin View has been <color=#00ff00ff>{0}</color>.", false);

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
            mapmode = Convert.ToBoolean(get("LustyMap", "MapMode"));
            minimap = Convert.ToBoolean(get("LustyMap", "Minimap"));
            left = Convert.ToBoolean(get("LustyMap", "Left"));
            compass = Convert.ToBoolean(get("LustyMap", "Compass"));
            startopen = Convert.ToBoolean(get("LustyMap", "StartOpen"));
            debug = Convert.ToBoolean(get("LustyMap", "Debug"));
            mapbutton = Convert.ToBoolean(get("LustyMap", "MapButton"));
            showmonuments = Convert.ToBoolean(get("LustyMap", "ShowMonuments"));
            showcaves = Convert.ToBoolean(get("LustyMap", "ShowCaves"));
            showheli = Convert.ToBoolean(get("LustyMap", "ShowHeli"));
            showplane = Convert.ToBoolean(get("LustyMap", "ShowPlane"));
            showsupply = Convert.ToBoolean(get("LustyMap", "ShowSupply"));
            showdebris = Convert.ToBoolean(get("LustyMap", "ShowDebris"));
            useurl = Convert.ToBoolean(get("LustyMap", "UseURL"));
            mapurl = Convert.ToString(get("LustyMap", "MapURL"));
            rustiofriends = Convert.ToBoolean(get("LustyMap", "RustIOFriends"));
            friendsapifriends = Convert.ToBoolean(get("LustyMap", "FriendsAPIFriends"));
            

            // Text strings
            txtInvalid = (string)get("TextStrings", "InvalidSyntex");
            txtUnknown = (string)get("TextStrings", "UnknownCommand");
            txtCmdMinimap = (string)get("TextStrings", "MinimapCommand");
            txtCmdMode = (string)get("TextStrings", "ModeCommand");
            txtCmdCompass = (string)get("TextStrings", "CompassCommand");
            txtCmdStart = (string)get("TextStrings", "StartCommand");
            txtCmdCaves = (string)get("TextStrings", "CavesCommand");
            txtCmdPlane = (string)get("TextStrings", "PlaneCommand");
            txtCmdHeli = (string)get("TextStrings", "HeliCommand");
            txtCmdSupply = (string)get("TextStrings", "SupplyCommand");
            txtCmdDebris = (string)get("TextStrings", "DebrisCommand");
            txtCmdMonuments = (string)get("TextStrings", "MonumentsCommand");
            txtCmdImages = (string)get("TextStrings", "ImagesCommand");
            txtCmdImage = (string)get("TextStrings", "ImageCommand");
            txtCmdLocation = (string)get("TextStrings", "LocationCommand");
            txtCmdUrl = (string)get("TextStrings", "UrlCommand");
            txtCmdMap = (string)get("TextStrings", "MapCommand");
            txtCmdIOFriends = (string)get("TextStrings", "RustIOFriendsCommand");
            txtCmdAPIFriends = (string)get("TextStrings", "FriendsAPIFriendsCommand");
            txtCmdAdmin = (string)get("TextStrings", "AdminViewCommand");

            txtCmtMode = (string)get("TextStrings", "ModeCommit");
            txtCmtMinimap = (string)get("TextStrings", "MinimapCommit");
            txtCmtAlign = (string)get("TextStrings", "AlignCommit");
            txtCmtCompass = (string)get("TextStrings", "CompassCommit");
            txtCmtOpen = (string)get("TextStrings", "OpenCommit");
            txtCmtCaves = (string)get("TextStrings", "CavesCommit");
            txtCmtPlane = (string)get("TextStrings", "PlaneCommit");
            txtCmtHeli = (string)get("TextStrings", "HeliCommit");
            txtCmtSupply = (string)get("TextStrings", "SupplyCommit");
            txtCmtDebris = (string)get("TextStrings", "DebrisCommit");
            txtCmtMonuments = (string)get("TextStrings", "MonumentsCommit");
            txtCmtImages = (string)get("TextStrings", "ImagesCommit");
            txtCmtUrl = (string)get("TextStrings", "UrlCommit");
            txtCmtIOFriends = (string)get("TextStrings", "RustIOFriendsCommit");
            txtCmtAPIFriends = (string)get("TextStrings", "FriendsAPIFriendsCommit");
            txtCmtAdmin = (string)get("TextStrings", "AdminViewCommit");

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

            // Load custom lists
            customIamges = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("LustyMapImages");
            mapCustom = Interface.Oxide.DataFileSystem.ReadObject<List<MapLocation>>("LustyMapLocations");
        }

        // Monuments
        private class MapLocation
        {
            public string name { get; set; }
            public double percentX { get; set; }
            public double percentZ { get; set; }
            public string icon { get; set; }
        }

        void OnServerInitialized()
        {
            mapSize = TerrainMeta.Size.x;
            var monumentInfos = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
            if (debug) { Puts($"Found {monumentInfos.Length} monuments on the map."); }
            foreach (var monumentInfo in monumentInfos)
            {
                MapLocation monument = new MapLocation
                {
                    percentX = GetMapPos(monumentInfo.transform.position.x),
                    percentZ = GetMapPos(monumentInfo.transform.position.z)
                };

                if (monumentInfo.Type == MonumentType.Lighthouse)
                {
                    monument.name = "Lighthouse";
                    monument.icon = "lighthouse";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.Type == MonumentType.Cave)
                {
                    monument.name = "Cave";
                    monument.icon = "cave";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.name.ToLower().Contains("warehouse"))
                {
                    monument.name = "Warehouse";
                    monument.icon = "warehouse";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.name.ToLower().Contains("satellite"))
                {
                    monument.name = "Satellite Dish";
                    monument.icon = "dish";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.name.ToLower().Contains("sphere"))
                {
                    monument.name = "Sphere Tank";
                    monument.icon = "spheretank";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.name.ToLower().Contains("powerplant"))
                {
                    monument.name = "Powerplant";
                    monument.icon = "special";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.name.ToLower().Contains("trainyard"))
                {
                    monument.name = "Trainyard";
                    monument.icon = "special";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.name.ToLower().Contains("airfield"))
                {
                    monument.name = "Airfield";
                    monument.icon = "special";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.name.ToLower().Contains("tunnel"))
                {
                    monument.name = "Military Tunnel";
                    monument.icon = "special";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.name.ToLower().Contains("treatment"))
                {
                    monument.name = "Water Treatment Plant";
                    monument.icon = "special";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.Type == MonumentType.Radtown)
                {
                    monument.name = "Radtown";
                    monument.icon = "radtown";
                    mapMonuments.Add(monument);
                }
                else if (monumentInfo.name.ToLower().Contains("monuments"))
                {
                }
                else
                {
                    // Missed one!
                    if (debug) { Puts("Missed monument " + monumentInfo.name.ToLower()); }
                }
            }

            CargoPlane[] planes = UnityEngine.Object.FindObjectsOfType<CargoPlane>();
            if (planes.Length > 0)
            {
                foreach (CargoPlane entity in planes)
                {
                    addActive(entity);
                }
            }
            BaseHelicopter[] heli = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>();
            if (heli.Length > 0)
            {
                foreach (BaseHelicopter entity in heli)
                {
                    addActive(entity);
                }
            }
            SupplyDrop[] supply = UnityEngine.Object.FindObjectsOfType<SupplyDrop>();
            if (supply.Length > 0)
            {
                foreach (SupplyDrop entity in supply)
                {
                    addActive(entity);
                }
            }
            HelicopterDebris[] debris = UnityEngine.Object.FindObjectsOfType<HelicopterDebris>();
            if (debris.Length > 0)
            {
                foreach (HelicopterDebris entity in debris)
                {
                    addActive(entity);
                }
            }

            // Check for RustIO
            InitializeRustIO();

            // Check for FriendsAPI
            InitializeFriendsAPI();

            // Download Images
            cacheImages();
        }

        private double GetMapPos(float pos)
        {
            return (pos + mapSize / 2.0) / mapSize;
        }

        // Custom map locations
        bool addLocation(BasePlayer player, string name, string icon = "special")
        {
            if (mapCustom.Find(r => string.Equals(r.name, name, StringComparison.CurrentCultureIgnoreCase)) == null)
            {
                MapLocation location = new MapLocation
                {
                    name = name,
                    icon = icon,
                    percentX = GetMapPos(player.transform.position.x),
                    percentZ = GetMapPos(player.transform.position.z)
                };

                // Add to list
                mapCustom.Add(location);
                Interface.Oxide.DataFileSystem.WriteObject("LustyMapLocations", mapCustom);
                return true;
            }
            return false;
        }

        bool removeLocation(string name)
        {
            var loc = mapCustom.Find(r => string.Equals(r.name, name, StringComparison.CurrentCultureIgnoreCase));
            if (loc != null)
            {
                mapCustom.Remove(loc);
                Interface.Oxide.DataFileSystem.WriteObject("LustyMapLocations", mapCustom);
                return true;
            }
            return false;
        }


        // Planes and Helicopters!
        class ActiveEntity
        {
            public long ID { get; set; }
            public bool isplane { get; set; }
            public bool isheli { get; set; }
            public bool issupply { get; set; }
            public bool isdebris { get; set; }
            public BaseEntity entity { get; set; }

            public string name { get; set; }
            public Vector3 position { get; set; }
            public double percentX { get; set; }
            public double percentZ { get; set; }
            public int row { get; set; }
            public int column { get; set; }
            public string icon { get; set; }
        }

        void OnEntitySpawned(BaseEntity entity)
        {
            addActive(entity);
        }

        void addActive(BaseEntity entity)
        {
            if (!(entity is CargoPlane) && !(entity is SupplyDrop) && !(entity is BaseHelicopter) && !(entity is HelicopterDebris)) return;

            ActiveEntity activeEntity = new ActiveEntity
            {
                ID = DateTime.Now.Ticks,
                isplane = false,
                isheli = false,
                issupply = false,
                isdebris = false,
                entity = entity
            };

            if (entity is CargoPlane)
            {
                activeEntity.isplane = true;
                activeEntity.name = "Plane";
                locationActive(activeEntity);
                activeEntities.Add(activeEntity);
            }
            else if (entity is BaseHelicopter)
            {
                activeEntity.isheli = true;
                activeEntity.name = "Helicopter";
                locationActive(activeEntity);
                activeEntities.Add(activeEntity);
            }
            else if (entity is SupplyDrop)
            {
                activeEntity.issupply = true;
                activeEntity.name = "Supply Drop";
                locationActive(activeEntity);
                activeEntities.Add(activeEntity);
            }
            else if (entity is HelicopterDebris)
            {
                activeEntity.isdebris = true;
                activeEntity.name = "Helicopter Debris";
                locationActive(activeEntity);
                activeEntities.Add(activeEntity);
            }
        }

        int directionEntity(double rotation)
        {
            return (int)((rotation - 5) / 10 + 0.5) * 10;
        }

        void locationActive(ActiveEntity activeEntity)
        {
            try
            {
                activeEntity.position = activeEntity.entity.transform.position;
                activeEntity.percentX = GetMapPos(activeEntity.position.x);
                activeEntity.percentZ = GetMapPos(activeEntity.position.z);

                if (activeEntity.isplane)
                {
                    activeEntity.icon = "plane" + directionEntity(activeEntity.entity.transform.rotation.eulerAngles.y);
                }
                else if (activeEntity.isheli)
                {
                    activeEntity.icon = "heli" + directionEntity(activeEntity.entity.transform.rotation.eulerAngles.y);
                }
                else if (activeEntity.issupply)
                {
                    activeEntity.icon = "supply";
                }
                else if (activeEntity.isdebris)
                {
                    activeEntity.icon = "debris";
                }
            }
            catch
            {
                if (debug) { Puts("Removing Entity: " + activeEntity.name); }
                activeEntities.Remove(activeEntity);
            }
        }

        void checkActive()
        {
            if (activeEntities.Count > 0)
            {
                for (int i = activeEntities.Count - 1; i >= 0; i--)
                {
                    locationActive(activeEntities[i]);
                }
            }
        }

        // Download Images
        ImageCache ImageAssets;
        GameObject LustyObject;

        private void cacheImages()
        {
            // Disable map updates while downloading images...
            run = false;

            // Initialize image cache
            LustyObject = new GameObject();
            ImageAssets = LustyObject.AddComponent<ImageCache>();
            ImageAssets.imageFiles.Clear();

            // Icons
            if (debug) { Puts("Downloading images..."); }
            string dataDirectory = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "LustyMap" + Path.DirectorySeparatorChar;

            List<string> files = new List<string>()
            {
                "self",
                "friend",
                "other",
                "heli",
                "plane"
            };

            foreach (string file in files)
            {
                string path = dataDirectory + "icons" + Path.DirectorySeparatorChar;
                string ext = ".png";

                for (int i = 0; i <= 360; i = i + 10)
                {
                    ImageAssets.getImage(file + i, path + file + i + ext);
                }
            }

            ImageAssets.getImage("lighthouse", dataDirectory + "icons" + Path.DirectorySeparatorChar + "lighthouse.png");
            ImageAssets.getImage("radtown", dataDirectory + "icons" + Path.DirectorySeparatorChar + "radtown.png");
            ImageAssets.getImage("cave", dataDirectory  + "icons" + Path.DirectorySeparatorChar + "cave.png");
            ImageAssets.getImage("warehouse", dataDirectory + "icons" + Path.DirectorySeparatorChar + "warehouse.png");
            ImageAssets.getImage("dish", dataDirectory + "icons" + Path.DirectorySeparatorChar + "dish.png");
            ImageAssets.getImage("spheretank", dataDirectory + "icons" + Path.DirectorySeparatorChar + "spheretank.png");
            ImageAssets.getImage("special", dataDirectory + "icons" + Path.DirectorySeparatorChar + "special.png");
            ImageAssets.getImage("supply", dataDirectory + "icons" + Path.DirectorySeparatorChar + "supply.png");
            ImageAssets.getImage("debris", dataDirectory + "icons" + Path.DirectorySeparatorChar + "debris.png");

            // Other
            ImageAssets.getImage("mapbg", dataDirectory + "other" + Path.DirectorySeparatorChar + "mapbg.jpg");

            // Map - TODO: Add option to auto detect RustIO address and download map
            if (useurl)
            {
                //ImageAssets.getImage("mapimage", mapurl);
            }

            ImageAssets.getImage("mapimage", dataDirectory + "map.jpg");

            if (mapmode)
            {
                List<int> minmaps = new List<int>() { 32, 26, 12, 6 };

                foreach (int minisize in minmaps)
                {
                    for (int i = 0; i < minisize; i++)
                    {
                        for (int j = 0; j < minisize; j++)
                        {
                            ImageAssets.getImage("map-" + minisize + "-" + i + "-" + j, dataDirectory + "map" + minisize + "x" + minisize + Path.DirectorySeparatorChar + "map-" + i + "-" + j + ".jpeg");
                        }
                    }
                }
            }

            foreach (string image in customIamges)
            {
                ImageAssets.getImage(image, dataDirectory + "custom" + Path.DirectorySeparatorChar + image + ".png");
            }

            // Wait for downloads to complete...
            download();
        }


        // Image cache class
        public class ImageCache : MonoBehaviour
        {
            public Dictionary<string, string> imageFiles = new Dictionary<string, string>();

            public int downloading = 0;
            public List<Queue> queued = new List<Queue>();

            public class Queue
            {
                public string url { get; set; }
                public string name { get; set; }
            }

            private void OnDestroy()
            {
                foreach (var value in imageFiles.Values)
                {
                    FileStorage.server.RemoveEntityNum(uint.MaxValue, Convert.ToUInt32(value));
                }
            }

            public void getImage(string name, string url)
            {
                if (imageFiles.ContainsKey(name))
                {
                    if (LustyMap.debug)
                    {
                        Debug.Log("Error, duplicate image: " + name);
                    }
                    return;
                }
                    // Queue download (too many connections at once causes errors), call the process function to initiate download...
                queued.Add(new Queue
                {
                    url = url,
                    name = name
                });
            }

            IEnumerator WaitForRequest(Queue queue)
            {
                using (var www = new WWW(queue.url))
                {
                    yield return www;
                    // check for errors
                    if (string.IsNullOrEmpty(www.error))
                    {
                        imageFiles.Add(queue.name, FileStorage.server.Store(www.bytes, FileStorage.Type.png, uint.MaxValue).ToString());
                        downloading--;
                    }
                    else
                    {
                        if (LustyMap.debug)
                        {
                            Debug.Log("Error downloading: " + queue.name + " - " + www.error);
                        }
                        downloading--;
                    }
                }
            }

            public void process()
            {
                // Limit the number of simultaneous downloads...
                if (downloading < 100)
                {
                    if (queued.Count > 0)
                    {
                        downloading++;
                        StartCoroutine(WaitForRequest(queued[0]));
                        queued.RemoveAt(0);
                    }
                }
            }
        }

        public string fetchImage(string name)
        {
            string result;
            if (ImageAssets.imageFiles.TryGetValue(name, out result))
                return result;
            if (debug) { Puts("[fetchImage]: error: " + name); }
            return string.Empty;
        }

        // Called after cacheImages
        int wait = 0;
        void download()
        {
            // Keep processing downloads until complete...
            if (ImageAssets.queued.Count > 0 || ImageAssets.downloading > 0)
            {
                for (int i = 0; i < 150; i++)
                {
                    ImageAssets.process();
                }
                timer.Once(0.1f, download);

                wait++;
                if (wait > 100) { if (debug) { Puts("[ImageAsset]: " + ImageAssets.queued.Count + " Queued, " + ImageAssets.downloading + " Downloading..."); wait = 0; } }
            }
            else
            {
                if (debug) { Puts("Downloaded " + ImageAssets.imageFiles.Count + " images."); }
                StartUp();
            }
        }

        // Custom images
        bool addCustom(string imagename)
        {
            if (customIamges.Find(r => r == imagename) == null)
            {
                customIamges.Add(imagename);
                Interface.Oxide.DataFileSystem.WriteObject("LustyMapImages", customIamges);
                return true;
            }
            return false;
        }

        bool removeCustom(string imagename)
        {
            if (customIamges.Find(r => r == imagename) != null)
            {
                customIamges.Remove(imagename);
                Interface.Oxide.DataFileSystem.WriteObject("LustyMapImages", customIamges);
                return true;
            }
            return false;
        }

        // Ready to start!
        void StartUp()
        {
            if (BasePlayer.activePlayerList.Count > 0)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    InitUser(player);
                }
            }

            run = true;
            lustyTimer();
        }

        // Work work
        private void lustyTimer()
        {
            // Check / Update Plane / Heli
            checkActive();

            // Refresh friends
            updateFriends();

            // Refresh maps
            if (BasePlayer.activePlayerList.Count > 0)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    try
                    {
                        minimapGUI(player);
                        mapGUI(player);
                    }
                    catch
                    {
                        // Error updating map...
                    }
                }
            }

            // Renew timer
            if (run) { timer.Once(1f, lustyTimer); }
        }

        // Cleanup on unload
        void Unloaded()
        {
            if (BasePlayer.activePlayerList.Count > 0)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player,"MinimapBG");
                    CuiHelper.DestroyUi(player,"Minimap");
                    CuiHelper.DestroyUi(player,"MinimapHUD");
                    CuiHelper.DestroyUi(player,"MapGUI");
                    CuiHelper.DestroyUi(player,"MapGUIBG");
                }
            }
            UnityEngine.Object.Destroy(LustyObject);
        }

        // Friends
        void updateFriends()
        {
            if (BasePlayer.activePlayerList.Count > 0)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    MapUser user = getUser(player);
                    user.friends.Clear();

                    foreach (BasePlayer wannabe in BasePlayer.activePlayerList)
                    {
                        // Skip self
                        if (wannabe.userID == player.userID) { continue; }

                        // Check if already friends
                        if (user.friends.Find(r => r.name == wannabe.displayName) == null)
                        {
                            bool result = false;
                            // Admin view
                            if (user.adminView)
                            {
                                user.friends.Add(createFriend(wannabe, "other"));
                                result = true;
                            }

                            // RustIO Friend?
                            if (!result && rustio && rustiofriends)
                            {
                                if (HasRustIOFriend(wannabe.userID.ToString(), player.userID.ToString()))
                                {
                                    user.friends.Add(createFriend(wannabe, "friend"));
                                    result = true;
                                }
                            }
                            
                            // Friend API
                            if (!result && friendapi && friendsapifriends)
                            {
                                if (HasFriendsAPIFriend(wannabe.userID.ToString(), player.userID.ToString()))
                                {
                                    user.friends.Add(createFriend(wannabe, "friend"));
                                    result = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        MapLocation createFriend(BasePlayer friend, string icon = "friend")
        {
            return new MapLocation
            {
                name = friend.displayName,
                icon = icon + ((int)(((double)friend.eyes.rotation.eulerAngles.y - 5) / 10 + 0.5) * 10),
                percentX = GetMapPos(friend.transform.position.x),
                percentZ = GetMapPos(friend.transform.position.z)
            };
        }

        // Chat commands
        [ChatCommand("map")]
        private void chatCmd(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                playerMsg(player, txtCmdMap);
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

                                if (BasePlayer.activePlayerList.Count > 0)
                                {
                                    foreach (BasePlayer activeplayer in BasePlayer.activePlayerList)
                                    {
                                        MapUser user = getUser(activeplayer);
                                        user.minimapRefresh = true;
                                    }
                                }

                                // Reload the image cache
                                cacheImages();

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

                                if (BasePlayer.activePlayerList.Count > 0)
                                {
                                    foreach (BasePlayer activeplayer in BasePlayer.activePlayerList)
                                    {
                                        MapUser user = getUser(activeplayer);
                                        user.minimapRefresh = true;
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

                                if (BasePlayer.activePlayerList.Count > 0)
                                {
                                    foreach (BasePlayer activeplayer in BasePlayer.activePlayerList)
                                    {
                                        MapUser user = getUser(activeplayer);
                                        user.minimapRefresh = true;
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
                                        CuiHelper.DestroyUi(activeplayer,"Minimap");
                                        CuiHelper.DestroyUi(activeplayer,"MinimapBG");
                                        CuiHelper.DestroyUi(activeplayer,"MinimapHUD");
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

                                if (BasePlayer.activePlayerList.Count > 0)
                                {
                                    foreach (BasePlayer activeplayer in BasePlayer.activePlayerList)
                                    {
                                        MapUser user = getUser(activeplayer);
                                        user.minimap = true;
                                        user.minimapRefresh = true;
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
                    //else if (args[0].ToLower() == "url")
                    //{
                    //    if (args.Length > 1)
                    //    {
                    //        try
                    //        {
                    //            mapurl = args[1];
                    //            set("LustyMap", "MapURL", mapurl);
                    //            playerMsg(player, string.Format(txtCmtUrl, mapurl));
                    //            return;
                    //        }
                    //        catch
                    //        {

                    //        }
                    //    }
                    //    playerMsg(player, txtInvalid + txtCmdUrl);
                    //}
                    else if (args[0].ToLower() == "showcaves")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                showcaves = Convert.ToBoolean(args[1]);
                                set("LustyMap", "ShowCaves", showcaves);
                                string disabled = "Disabled";
                                if (showcaves) { disabled = "Enabled"; }
                                playerMsg(player, string.Format(txtCmtCaves, disabled));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdCaves);
                    }
                    else if (args[0].ToLower() == "showmonuments")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                showmonuments = Convert.ToBoolean(args[1]);
                                set("LustyMap", "ShowMonuments", showmonuments);
                                string disabled = "Disabled";
                                if (showmonuments) { disabled = "Enabled"; }
                                playerMsg(player, string.Format(txtCmtMonuments, disabled));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdMonuments);
                    }
                    else if (args[0].ToLower() == "showplane")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                showplane = Convert.ToBoolean(args[1]);
                                set("LustyMap", "ShowPlane", showplane);
                                string disabled = "Disabled";
                                if (showplane) { disabled = "Enabled"; }
                                playerMsg(player, string.Format(txtCmtPlane, disabled));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdPlane);
                    }
                    else if (args[0].ToLower() == "showheli")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                showheli = Convert.ToBoolean(args[1]);
                                set("LustyMap", "ShowHeli", showheli);
                                string disabled = "Disabled";
                                if (showheli) { disabled = "Enabled"; }
                                playerMsg(player, string.Format(txtCmtHeli, disabled));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdHeli);
                    }
                    else if (args[0].ToLower() == "showsupply")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                showsupply = Convert.ToBoolean(args[1]);
                                set("LustyMap", "ShowSupply", showsupply);
                                string disabled = "Disabled";
                                if (showsupply) { disabled = "Enabled"; }
                                playerMsg(player, string.Format(txtCmtSupply, disabled));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdSupply);
                    }
                    else if (args[0].ToLower() == "showdebris")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                showdebris = Convert.ToBoolean(args[1]);
                                set("LustyMap", "ShowDebris", showdebris);
                                string disabled = "Disabled";
                                if (showdebris) { disabled = "Enabled"; }
                                playerMsg(player, string.Format(txtCmtDebris, disabled));
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdDebris);
                    }
                    else if (args[0].ToLower() == "debug")
                    {
                        if (args.Length > 1)
                        {
                            try
                            {
                                debug = Convert.ToBoolean(args[1]);
                                set("LustyMap", "Debug", debug);
                                playerMsg(player, "Debug: " + debug);
                                return;
                            }
                            catch
                            {

                            }
                        }
                        playerMsg(player, txtInvalid + txtCmdMonuments);
                    }
                    else if (args[0].ToLower() == "admin")
                    {
                        MapUser user = getUser(player);
                        if (user.adminView)
                        {
                            user.adminView = false;
                            playerMsg(player, string.Format(txtCmtAdmin, "Disabled"));
                            return;
                        }
                        else
                        {
                            user.adminView = true;
                            playerMsg(player, string.Format(txtCmtAdmin, "Enabled"));
                            return;
                        }
                    }
                    else if (args[0].ToLower() == "rustiofriends")
                    {
                        if (rustio)
                        {
                            if (args.Length > 1)
                            {
                                try
                                {
                                    rustiofriends = Convert.ToBoolean(args[1]);
                                    set("LustyMap", "RustIOFriends", rustiofriends);
                                    string disabled = "Disabled";
                                    if (rustiofriends) { disabled = "Enabled"; }
                                    playerMsg(player, string.Format(txtCmtIOFriends, disabled));
                                    return;
                                }
                                catch
                                {

                                }
                            }
                            playerMsg(player, txtInvalid + txtCmdIOFriends);
                        }
                        else
                        {
                            rustiofriends = false;
                            playerMsg(player, "RustIO not detected...");
                        }
                    }
                    else if (args[0].ToLower() == "friendsapifriends")
                    {
                        if (friendapi)
                        {
                            if (args.Length > 1)
                            {
                                try
                                {
                                    friendsapifriends = Convert.ToBoolean(args[1]);
                                    set("LustyMap", "FriendAPIFriends", friendsapifriends);
                                    string disabled = "Disabled";
                                    if (friendsapifriends) { disabled = "Enabled"; }
                                    playerMsg(player, string.Format(txtCmtAPIFriends, disabled));
                                    return;
                                }
                                catch
                                {

                                }
                            }
                            playerMsg(player, txtInvalid + txtCmdAPIFriends);
                        }
                        else
                        {
                            friendsapifriends = false;
                            playerMsg(player, "FriendsAPI not detected...");
                        }
                    }
                    else if (args[0].ToLower() == "add")
                    {
                        if (args.Length > 2)
                        {
                            if (addLocation(player, args[1], args[2].ToLower()))
                            {
                                playerMsg(player, "location added");
                            }
                            else
                            {
                                playerMsg(player, "location already in list");
                            }
                        }
                        else if (args.Length > 1)
                        {
                            if (addLocation(player, args[1]))
                            {
                                playerMsg(player, "location added");
                            }
                            else
                            {
                                playerMsg(player, "location already in list");
                            }
                        }
                        else
                        {
                            playerMsg(player, txtInvalid + txtCmdLocation);
                        }
                    }
                    else if (args[0].ToLower() == "remove")
                    {
                        if (args.Length > 1)
                        {
                            if (removeLocation(args[1].ToLower()))
                            {
                                playerMsg(player, "location removed");
                            }
                            else
                            {
                                playerMsg(player, "location not in list");
                            }
                        }
                        else
                        {
                            playerMsg(player, txtInvalid + txtCmdLocation);
                        }
                    }
                    else if (args[0].ToLower() == "images")
                    {
                        playerMsg(player, "Reloading image cache...");
                        cacheImages();
                    }
                    else if (args[0].ToLower() == "addimage")
                    {
                        if (args.Length > 1)
                        {
                            if (addCustom(args[1].ToLower()))
                            {
                                playerMsg(player, "Image added");
                                cacheImages();
                            }
                            else
                            {
                                playerMsg(player, "Image already in list");
                            }
                        }
                        else
                        {
                            playerMsg(player, txtInvalid + txtCmdImage);
                        }
                    }
                    else if (args[0].ToLower() == "removeimage")
                    {
                        if (args.Length > 1)
                        {
                            if (removeCustom(args[1].ToLower()))
                            {
                                playerMsg(player, "Image removed");
                            }
                            else
                            {
                                playerMsg(player, "Image not in list");
                            }
                        }
                        else
                        {
                            playerMsg(player, txtInvalid + txtCmdImage);
                        }
                    }
                    else if (args[0].ToLower() == "help")
                    {
                        playerMsg(player, "<color=#00ff00ff>" + Title + "</color> v<color=#00ff00ff>" + Version + "</color>");
                        playerMsg(player, txtCmdMode);
                        playerMsg(player, txtCmdMinimap);
                        playerMsg(player, txtCmdCompass);
                        playerMsg(player, txtCmdStart);
                        playerMsg(player, txtCmdMonuments);
                        playerMsg(player, txtCmdCaves);
                        playerMsg(player, txtCmdPlane);
                        playerMsg(player, txtCmdHeli);
                        playerMsg(player, txtCmdSupply);
                        playerMsg(player, txtCmdDebris);
                        playerMsg(player, txtCmdImages);
                        playerMsg(player, txtCmdImage);
                        playerMsg(player, txtCmdLocation);
                        playerMsg(player, txtCmdIOFriends);
                        playerMsg(player, txtCmdAPIFriends);
                        playerMsg(player, txtCmdAdmin);
                    }
                    else
                    {
                        playerMsg(player, txtUnknown);
                    }
                }
            }
        }

        // Console commands
        [ConsoleCommand("LustyMap")]
        private void lustyConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (arg.Args == null || arg.Args.Length == 0)
            {
                PrintToConsole(player, Title + " v" + Version);
            }
            else
            {
                if (arg.Args[0].ToLower() == "close")
                {
                    MapUser user = getUser(player);
                    user.minimapReOpen = false;
                    user.minimap = false;
                    user.minimapRefresh = true;
                    CuiHelper.DestroyUi(player,"Minimap");
                    CuiHelper.DestroyUi(player,"MinimapBG");
                    CuiHelper.DestroyUi(player,"MinimapHUD");
                    minimapGUI(player);
                }
                else if (arg.Args[0].ToLower() == "open")
                {
                    MapUser user = getUser(player);
                    user.minimapReOpen = true;
                    user.minimap = true;
                    user.minimapRefresh = true;
                    minimapGUI(player);
                }
                else if (arg.Args[0].ToLower() == "map")
                {
                    mapToggle(player);
                }
                else if (arg.Args[0].ToLower() == "return")
                {
                    mapClose(player);
                }
                else if (arg.Args[0].ToLower() == "zoomin")
                {
                    MapUser user = getUser(player);
                    // Check if at max zoom
                    if (user.minimapZoom > 1)
                    {
                        user.minimapZoom--;
                        user.minimapRefresh = true;
                    }
                }
                else if (arg.Args[0].ToLower() == "zoomout")
                {
                    MapUser user = getUser(player);
                    // Check if at min zoom
                    if (user.minimapZoom < 4)
                    {
                        user.minimapZoom++;
                        user.minimapRefresh = true;
                    }
                }
            }
        }

        void mapToggle(BasePlayer player)
        {
            MapUser user = getUser(player);
            if (user.map)
            {
                mapClose(player);
            }
            else
            {
                mapOpen(player);
            }
        }

        void mapOpen(BasePlayer player)
        {
            MapUser user = getUser(player);
            user.minimap = false;
            CuiHelper.DestroyUi(player,"Minimap");
            CuiHelper.DestroyUi(player,"MinimapBG");
            CuiHelper.DestroyUi(player,"MinimapHUD");
            user.map = true;
            user.minimapRefresh = true;
            user.mapRefresh = true;
            minimapGUI(player);
            mapGUI(player);
        }

        void mapClose(BasePlayer player)
        {
            MapUser user = getUser(player);
            if (user.map)
            {
                user.map = false;
                user.minimapRefresh = true;
                user.mapRefresh = false;
                CuiHelper.DestroyUi(player,"MapGUI");
                CuiHelper.DestroyUi(player,"MapGUIBG");
                if (user.minimapReOpen)
                {
                    user.minimap = true;
                    minimapGUI(player);
                }
            }
        }

        // User settings
        private class MapUser
        {
            public ulong userid { get; set; }
            public bool minimap { get; set; }
            public bool minimapStart { get; set; }
            public bool minimapReOpen { get; set; }
            public bool minimapLeft { get; set; }
            public bool minimapRefresh { get; set; }
            public int minimapZoom { get; set; }
            public bool compass { get; set; }
            public bool map { get; set; }
            public bool mapGrid { get; set; }
            public bool mapCustom { get; set; }
            public bool mapMonuments { get; set; }
            public bool mapCaves { get; set; }
            public bool mapHeli { get; set; }
            public bool mapPlane { get; set; }
            public bool mapSupply { get; set; }
            public bool mapDebris { get; set; }
            public bool mapRefresh { get; set; }
            public bool mapMode { get; set; }
            public bool adminView { get; set; }
            public int mapx { get; set; }
            public int mapz { get; set; }
            public List<MapLocation> friends { get; set; }
        }

        Dictionary<ulong, MapUser> mapUsers = new Dictionary<ulong, MapUser>();

        private MapUser getUser(BasePlayer player) => getUser(player.userID);
        private MapUser getUser(ulong userid)
        {
            // Find player...
            MapUser user;
            return mapUsers.TryGetValue(userid, out user) ? user : newUser(userid);
        }

        private MapUser newUser(ulong userid)
        {
            MapUser user = new MapUser
            {
                userid = userid,
                minimap = startopen,
                minimapReOpen = startopen,
                minimapLeft = left,
                compass = compass,
                map = false,
                mapGrid = true,
                mapCustom = true,
                mapMonuments = showmonuments,
                mapCaves = showcaves,
                mapHeli = showheli,
                mapPlane = showplane,
                mapSupply = showsupply,
                mapDebris = showdebris,
                mapx = 0,
                mapz = 0,
                minimapRefresh = true,
                minimapZoom = 1,
                friends = new List<MapLocation>(),
                mapMode = mapmode,
                adminView = false
            };
            mapUsers.Add(userid, user);
            return user;
        }

        // Player Join
        [HookMethod("OnPlayerInit")]
        void OnPlayerInit(BasePlayer player)
        {
            OnPlayerReady(player);
        }

        private void OnPlayerReady(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => OnPlayerReady(player));
            }
            else
            {
                InitUser(player);

            }
        }

        void InitUser(BasePlayer player)
        {
            // Keybinds
            player.Command("bind m \"LustyMap map\"");

            // Variables
            MapUser user = getUser(player);
            user.minimapRefresh = true;

            // Make sure no exiting map gui exists (in case of disconnect with map open)
            mapClose(player);
        }

        // Map GUI
        private void mapGUI(BasePlayer player)
        {
            MapUser user = getUser(player);
            if (user.map)
            {
                if (user.mapRefresh)
                {
                    GUIv4 gui = new GUIv4();
                    gui.add("MapGUIBG", false, "0.2271875 0.015", "0.7728125 0.985", "0 0 0 1");
                    gui.png("{parent}", "Map", fetchImage("mapimage"), "0 0", "1 1");
                    gui.text("{parent}", "Ver", TextAnchor.LowerRight, "<size=10>" + Title + " v" + Version + "</size>", "0.8 0.01", "0.99 0.1");

                    bool grid = false;

                    if (grid)
                    {
                        int rows = 25;
                        for (int i = 1; i < 25; i++)
                        {
                            double s = ((1f / rows) * i) - 0.000001f;
                            double e = ((1f / rows) * i) + 0.000001f;

                            gui.box("{parent}", "X" + i, s + " 0", e + " 1", "0.2 0.2 0.2 8");
                            gui.box("{parent}", "Y" + i, "0 " + s, "1 " + e, "0.2 0.2 0.2 8");
                        }
                    }

                    double iconsize = 0.01f;
                    if (showmonuments && user.mapMonuments)
                    {
                        foreach (MapLocation location in mapMonuments)
                        {
                            if (location.name == "Cave" && !showcaves) { continue; }
                            if (location.name == "Cave" && !user.mapCaves) { continue; }
                            gui.png("{parent}", "Mon" + DateTime.Now.Ticks, fetchImage(location.icon), (location.percentX - iconsize) + " " + (location.percentZ - iconsize), (location.percentX + iconsize) + " " + (location.percentZ + iconsize));
                            gui.text("{parent}", "TxT" + DateTime.Now.Ticks, TextAnchor.UpperCenter, "<size=10>" + location.name + "</size>", (location.percentX - 0.1) + " " + (location.percentZ - iconsize - 0.05), (location.percentX + 0.1) + " " + (location.percentZ - iconsize));
                        }
                    }
                    if (user.mapCustom)
                    {
                        foreach (MapLocation location in mapCustom)
                        {
                            gui.png("{parent}", "Cus" + DateTime.Now.Ticks, fetchImage(location.icon), (location.percentX - iconsize) + " " + (location.percentZ - iconsize), (location.percentX + iconsize) + " " + (location.percentZ + iconsize));
                            gui.text("{parent}", "TxT" + DateTime.Now.Ticks, TextAnchor.UpperCenter, "<size=10>" + location.name + "</size>", (location.percentX - 0.1) + " " + (location.percentZ - iconsize - 0.05), (location.percentX + 0.1) + " " + (location.percentZ - iconsize));
                        }
                    }
                    gui.send(player);
                    user.mapRefresh = false;
                }
            }
            // Live Map
            if (user.map)
            {
                // Player Direction
                string direction = null;
                double lookRotation = player.eyes.rotation.eulerAngles.y;
                int playerdirection = (Convert.ToInt16((lookRotation - 5) / 10 + 0.5) * 10);
                if (lookRotation >= 355) playerdirection = 0;
                if (lookRotation > 337.5 || lookRotation < 22.5) { direction = txtCpsN; }
                else if (lookRotation > 22.5 && lookRotation < 67.5) { direction = txtCpsNE; }
                else if (lookRotation > 67.5 && lookRotation < 112.5) { direction = txtCpsE; }
                else if (lookRotation > 112.5 && lookRotation < 157.5) { direction = txtCpsSE; }
                else if (lookRotation > 157.5 && lookRotation < 202.5) { direction = txtCpsS; }
                else if (lookRotation > 202.5 && lookRotation < 247.5) { direction = txtCpsSW; }
                else if (lookRotation > 247.5 && lookRotation < 292.5) { direction = txtCpsW; }
                else if (lookRotation > 292.5 && lookRotation < 337.5) { direction = txtCpsNW; }

                double mapX = GetMapPos(player.transform.position.x);
                double mapZ = GetMapPos(player.transform.position.z);

                GUIv4 gui = new GUIv4();
                gui.add("MapGUI", false, "0.2271875 0.015", "0.7728125 0.985", "0 0 0 0");

                bool grid = false;

                double iconsize = 0.02f;
                if (activeEntities.Count > 0)
                {
                    foreach (ActiveEntity entity in activeEntities)
                    {
                        if ((entity.isplane && showplane && user.mapPlane) || (entity.isheli && showheli && user.mapHeli) || (entity.issupply && showsupply && user.mapSupply) || (entity.issupply && showsupply && user.mapSupply) || (entity.isdebris && showdebris && user.mapDebris))
                        {
                            if (entity.percentX >= 0 && entity.percentX <= 1 && entity.percentZ >= 0 && entity.percentZ <= 1)
                            {
                                gui.png("{parent}", "Ent" + DateTime.Now.Ticks.ToString(), fetchImage(entity.icon), (entity.percentX - iconsize) + " " + (entity.percentZ - iconsize), (entity.percentX + iconsize) + " " + (entity.percentZ + iconsize));
                            }
                        }
                    }
                }

                if (user.friends.Count > 0)
                {
                    foreach (MapLocation friendEntity in user.friends)
                    {
                        if (friendEntity.percentX >= 0 && friendEntity.percentX <= 1 && friendEntity.percentZ >= 0 && friendEntity.percentZ <= 1)
                        {
                            gui.png("{parent}", "Fnd" + DateTime.Now.Ticks, fetchImage(friendEntity.icon), (friendEntity.percentX - (iconsize / 1.5)) + " " + (friendEntity.percentZ - (iconsize / 1.5)), (friendEntity.percentX + (iconsize / 1.5)) + " " + (friendEntity.percentZ + (iconsize / 1.5)));
                        }
                    }
                }
                gui.png("{parent}", "Player", fetchImage("self" + playerdirection), (mapX - (iconsize / 1.5)) + " " + (mapZ - (iconsize / 1.5)), (mapX + (iconsize / 1.5)) + " " + (mapZ + (iconsize / 1.5)));
                gui.text("{parent}", "Direction", TextAnchor.UpperRight, "<size=16>" + txtCpsHead + " " + direction + "</size>\n<size=12>" + player.transform.position + "</size>", "0.6 0.9", "0.99 0.99");
                gui.send(player);
            }
        }

        // Minimap Menu
        private void minimapGUI(BasePlayer player)
        {
            MapUser user = getUser(player);
            int mapslices = 32;

            // Minimap open / allowed?
            if (minimap)
            {
                // Map alignment
                double offset = 0f;
                if (!user.minimapLeft)
                {
                    offset = 0.87f;
                }

                // Minimap Hud
                if (user.minimapRefresh)
                {
                    GUIv4 gui = new GUIv4();
                    gui.add("MinimapHUD", false, offset + " 0.98", (0.13 + offset) + " 1", "0 0 0 0");

                    if (user.minimapLeft)
                    {
                        if (user.minimap)
                        {
                            gui.button("{parent}", "MinimapClose", TextAnchor.MiddleCenter, "<size=12><<<</size>", true, "LustyMap close", "1 0", "1.15 1", "0 0 0 0.6");
                            if (mapmode)
                            {
                                gui.button("{parent}", "MinimapIn", TextAnchor.MiddleCenter, "<size=12>+</size>", false, "LustyMap zoomin", "1 -1.1", "1.1 -0.1", "0 0 0 0.6");
                                gui.button("{parent}", "MinimapOut", TextAnchor.MiddleCenter, "<size=12>-</size>", false, "LustyMap zoomout", "1 -2.2", "1.1 -1.2", "0 0 0 0.6");
                            }
                        }
                        else
                        {
                            gui.button("{parent}", "MinimapOpen", TextAnchor.MiddleCenter, "<size=12>>>></size>", true, "LustyMap open", "0 0", "0.15 1", "0 0 0 0.6");
                            if (mapmode)
                            {
                                gui.button("{parent}", "MinimapIn", TextAnchor.MiddleCenter, "<size=12>+</size>", false, "LustyMap zoomin", "0 -1.5", "0.075 -0.5", "0 0 0 0.6");
                                gui.button("{parent}", "MinimapOut", TextAnchor.MiddleCenter, "<size=12>-</size>", false, "LustyMap zoomout", "0 -3", "0.075 -2", "0 0 0 0.6");
                            }
                        }
                    }
                    else
                    {
                        if (user.minimap)
                        {
                            gui.button("{parent}", "MinimapClose", TextAnchor.MiddleCenter, "<size=12>>>></size>", true, "LustyMap close", "-0.15 0", "0 1", "0 0 0 0.6");
                            if (mapmode)
                            {
                                gui.button("{parent}", "MinimapIn", TextAnchor.MiddleCenter, "<size=12>+</size>", false, "LustyMap zoomin", "-0.15 -1.1", "0 -0.1", "0 0 0 0.6");
                                gui.button("{parent}", "MinimapOut", TextAnchor.MiddleCenter, "<size=12>-</size>", false, "LustyMap zoomout", "-0.15 -2.2", "0 -1.2", "0 0 0 0.6");
                            }
                        }
                        else
                        {
                            gui.button("{parent}", "MinimapOpen", TextAnchor.MiddleCenter, "<size=12><<<</size>", true, "LustyMap open", "0.85 0", "1 1", "0 0 0 0.6");
                            if (mapmode)
                            {
                                gui.button("{parent}", "MinimapIn", TextAnchor.MiddleCenter, "<size=12>+</size>", false, "LustyMap zoomin", "0.85 -1.1", "1 -0.1", "0 0 0 0.6");
                                gui.button("{parent}", "MinimapOut", TextAnchor.MiddleCenter, "<size=12>-</size>", false, "LustyMap zoomout", "0.85 -2.2", "1 -1.2", "0 0 0 0.6");
                            }
                        }
                    }
                    gui.send(player);
                }

                // Minimap Simple Mode - Background
                if (user.minimapRefresh && user.minimap)
                {
                    if (!mapmode)
                    {
                        GUIv4 gui = new GUIv4();
                        gui.add("MinimapBG", false, offset + " 0.7699", (0.13 + offset) + " 1", "0 0 0 1");
                        gui.png("{parent}", "Map", fetchImage("mapimage"), "0 0", "1 1");

                        if (showmonuments && user.mapMonuments)
                        {
                            double iconsize = 0.02f;
                            foreach (MapLocation location in mapMonuments)
                            {
                                if (location.name == "Cave" && !showcaves) { continue; }
                                if (location.name == "Cave" && !user.mapCaves) { continue; }
                                gui.png("{parent}", "Mon" + DateTime.Now.Ticks, fetchImage(location.icon), (location.percentX - iconsize) + " " + (location.percentZ - iconsize), (location.percentX + iconsize) + " " + (location.percentZ + iconsize));
                            }
                        }
                        if (user.mapCustom)
                        {
                            double iconsize = 0.02f;
                            foreach (MapLocation location in mapCustom)
                            {
                                gui.png("{parent}", "Cus" + DateTime.Now.Ticks, fetchImage(location.icon), (location.percentX - iconsize) + " " + (location.percentZ - iconsize), (location.percentX + iconsize) + " " + (location.percentZ + iconsize));
                            }
                        }

                        gui.send(player);
                    }
                }

                // Minimap Complex Mode - Background Refresh
                if (mapmode && user.minimap)
                {
                    // Get zoom level for user
                    if (user.minimapZoom == 4)
                    {
                        mapslices = 6;
                    }
                    else if (user.minimapZoom == 3)
                    {
                        mapslices = 12;
                    }
                    else if (user.minimapZoom == 2)
                    {
                        mapslices = 26;
                    }

                    // Get center map part
                    double x = player.transform.position.x + mapSize / 2f;
                    double z = player.transform.position.z + mapSize / 2f;
                    var mapres = mapSize / mapslices;
                    int currentx = Convert.ToInt32(Math.Ceiling(x / mapres)) - 2;
                    int currentz = mapslices - Convert.ToInt32(Math.Ceiling(z / mapres)) - 1;

                    // Check if it has changed
                    if (user.mapx != currentx || user.mapz != currentz || user.minimapRefresh)
                    {
                        user.mapx = currentx;
                        user.mapz = currentz;

                        // Start creating GUI
                        GUIv4 gui = new GUIv4();
                        gui.add("MinimapBG", false, offset + " 0.7699", (0.13 + offset) + " 1", "0 0 0 0");

                        // Map parts
                        int row = 3;
                        int col = 3;
                        for (int r = 0; r < row; r++)
                        {
                            for (int c = 0; c < col; c++)
                            {
                                string maplink = "map-" + mapslices + "-" + (currentz + r) + "-" + (currentx + c);
                                string sx = Convert.ToSingle(c * (1f / col)).ToString();
                                string sy = Convert.ToSingle(1 - ((1f / row) * (r + 1))).ToString();
                                string ex = Convert.ToSingle(((c + 1) * (1f / col)) - 0.0005f).ToString();
                                string ey = Convert.ToSingle((1 - ((1f / row) * (r + 1)) + (1f / row)) - 0.0004f).ToString();

                                if ((currentz + r) >= 0 && (currentz + r) < mapslices && (currentx + c) >= 0 && (currentx + c) < mapslices)
                                {
                                    gui.png("{parent}", "Map" + DateTime.Now.Ticks, fetchImage(maplink), sx + " " + sy, ex + " " + ey, "0.9");
                                }
                                else
                                {
                                    gui.png("{parent}", "Map" + DateTime.Now.Ticks, fetchImage("mapbg"), sx + " " + sy, ex + " " + ey, "0.9");
                                }
                                if (showmonuments && user.mapMonuments)
                                {
                                    double iconsize = 0.05f;
                                    foreach (MapLocation location in mapMonuments)
                                    {
                                        if (location.name == "Cave" && !showcaves) { continue; }
                                        if (location.name == "Cave" && !user.mapCaves) { continue; }

                                        int lrow = (Convert.ToInt16(Math.Floor(mapslices * location.percentX)));
                                        int lcolumn = ((mapslices - 1) - Convert.ToInt16(Math.Floor(mapslices * location.percentZ)));

                                        if (lcolumn == (currentz + r) && lrow == (currentx + c))
                                        {
                                            double _sx = Convert.ToSingle(c * (1f / col));
                                            double _sy = Convert.ToSingle(1 - ((1f / row) * (r + 1)));
                                            double _ex = Convert.ToSingle(((c + 1) * (1f / col)) - 0.005f);
                                            double _ey = Convert.ToSingle((1 - ((1f / row) * (r + 1)) + (1f / row)) - 0.004f);
                                            double mapX = (location.percentX * mapslices) - lrow;
                                            double mapZ = ((1 - location.percentZ) * mapslices) - lcolumn;
                                            double _xd = _ex - _sx;
                                            mapX = (mapX * _xd) + _sx;
                                            double _yd = _ey - _sy;
                                            mapZ = _ey - (mapZ * _yd);

                                            gui.png("{parent}", "Mon" + DateTime.Now.Ticks, fetchImage(location.icon), (mapX - iconsize) + " " + (mapZ - iconsize), (mapX + iconsize) + " " + (mapZ + iconsize), "1");
                                        }
                                    }
                                }
                                if (user.mapCustom)
                                {
                                    double iconsize = 0.05f;
                                    foreach (MapLocation location in mapCustom)
                                    {
                                        int lrow = (Convert.ToInt16(Math.Floor(mapslices * location.percentX)));
                                        int lcolumn = ((mapslices - 1) - Convert.ToInt16(Math.Floor(mapslices * location.percentZ)));

                                        if (lcolumn == (currentz + r) && lrow == (currentx + c))
                                        {
                                            double _sx = Convert.ToSingle(c * (1f / col));
                                            double _sy = Convert.ToSingle(1 - ((1f / row) * (r + 1)));
                                            double _ex = Convert.ToSingle(((c + 1) * (1f / col)) - 0.005f);
                                            double _ey = Convert.ToSingle((1 - ((1f / row) * (r + 1)) + (1f / row)) - 0.004f);
                                            double mapX = (location.percentX * mapslices) - lrow;
                                            double mapZ = ((1 - location.percentZ) * mapslices) - lcolumn;
                                            double _xd = _ex - _sx;
                                            mapX = (mapX * _xd) + _sx;
                                            double _yd = _ey - _sy;
                                            mapZ = _ey - (mapZ * _yd);

                                            gui.png("{parent}", "Cus" + DateTime.Now.Ticks, fetchImage(location.icon), (mapX - iconsize) + " " + (mapZ - iconsize), (mapX + iconsize) + " " + (mapZ + iconsize), "1");
                                        }
                                    }
                                }
                            }
                        }
                        gui.send(player);
                    }
                }

                // Static GUI done
                if (user.minimapRefresh) { user.minimapRefresh = false; }

                // Minimap Player / Entity Locations
                if (user.minimap)
                {
                    // Player Direction
                    string direction = null;
                    double lookRotation = player.eyes.rotation.eulerAngles.y;
                    int playerdirection = (Convert.ToInt16((lookRotation - 5) / 10 + 0.5) * 10);
                    if (lookRotation > 337.5 || lookRotation < 22.5) { direction = txtCpsN; }
                    else if (lookRotation > 22.5 && lookRotation < 67.5) { direction = txtCpsNE; }
                    else if (lookRotation > 67.5 && lookRotation < 112.5) { direction = txtCpsE; }
                    else if (lookRotation > 112.5 && lookRotation < 157.5) { direction = txtCpsSE; }
                    else if (lookRotation > 157.5 && lookRotation < 202.5) { direction = txtCpsS; }
                    else if (lookRotation > 202.5 && lookRotation < 247.5) { direction = txtCpsSW; }
                    else if (lookRotation > 247.5 && lookRotation < 292.5) { direction = txtCpsW; }
                    else if (lookRotation > 292.5 && lookRotation < 337.5) { direction = txtCpsNW; }

                    // Player Location
                    double x = player.transform.position.x + mapSize / 2f;
                    double z = player.transform.position.z + mapSize / 2f;

                    // Player location in percent
                    double mapX = GetMapPos(player.transform.position.x);
                    double mapZ = GetMapPos(player.transform.position.z);

                    // GUI
                    GUIv4 gui = new GUIv4();
                    gui.add("Minimap", false, offset + " 0.7699", (0.13 + offset) + " 1", "0 0 0 0");

                    double iconsize = 0.05f;
                    if (mapmode)
                    {
                        var mapres = mapSize / mapslices;
                        int currentx = Convert.ToInt32(Math.Ceiling(x / mapres)) - 2;
                        int currentz = mapslices - Convert.ToInt32(Math.Ceiling(z / mapres)) - 1;

                        // Map parts
                        int row = 3;
                        int col = 3;
                        for (int r = 0; r < row; r++)
                        {
                            for (int c = 0; c < col; c++)
                            {
                                // Planes / Helis / Supply Drops Etc...
                                if (activeEntities.Count > 0)
                                {
                                    foreach (ActiveEntity entity in activeEntities)
                                    {
                                        if ((entity.isplane && showplane && user.mapPlane) || (entity.isheli && showheli && user.mapHeli) || (entity.issupply && showsupply && user.mapSupply) || (entity.isdebris && showdebris && user.mapDebris))
                                        {
                                            int erow = (Convert.ToInt16(Math.Floor(mapslices * entity.percentX)));
                                            int ecolumn = ((mapslices - 1) - Convert.ToInt16(Math.Floor(mapslices * entity.percentZ)));

                                            if (ecolumn == (currentz + r) && erow == (currentx + c))
                                            {
                                                double _sx = Convert.ToSingle(c * (1f / col));
                                                double _sy = Convert.ToSingle(1 - ((1f / row) * (r + 1)));
                                                double _ex = Convert.ToSingle(((c + 1) * (1f / col)) - 0.005f);
                                                double _ey = Convert.ToSingle((1 - ((1f / row) * (r + 1)) + (1f / row)) - 0.004f);
                                                double mapXX = (entity.percentX * mapslices) - erow;
                                                double mapZZ = ((1 - entity.percentZ) * mapslices) - ecolumn;
                                                double _xd = _ex - _sx;
                                                mapXX = (mapXX * _xd) + _sx;
                                                double _yd = _ey - _sy;
                                                mapZZ = _ey - (mapZZ * _yd);

                                                gui.png("{parent}", "Ent" + DateTime.Now.Ticks, fetchImage(entity.icon), (mapXX - iconsize) + " " + (mapZZ - iconsize), (mapXX + iconsize) + " " + (mapZZ + iconsize), "1");
                                            }
                                        }
                                    }
                                }

                                // Friends
                                if (user.friends.Count > 0)
                                {
                                    foreach (MapLocation friendEntity in user.friends)
                                    {
                                        int erow = (Convert.ToInt16(Math.Floor(mapslices * friendEntity.percentX)));
                                        int ecolumn = ((mapslices - 1) - Convert.ToInt16(Math.Floor(mapslices * friendEntity.percentZ)));

                                        if (ecolumn == (currentz + r) && erow == (currentx + c))
                                        {
                                            double _sx = Convert.ToSingle(c * (1f / col));
                                            double _sy = Convert.ToSingle(1 - ((1f / row) * (r + 1)));
                                            double _ex = Convert.ToSingle(((c + 1) * (1f / col)) - 0.005f);
                                            double _ey = Convert.ToSingle((1 - ((1f / row) * (r + 1)) + (1f / row)) - 0.004f);
                                            double mapXX = (friendEntity.percentX * mapslices) - erow;
                                            double mapZZ = ((1 - friendEntity.percentZ) * mapslices) - ecolumn;
                                            double _xd = _ex - _sx;
                                            mapXX = (mapXX * _xd) + _sx;
                                            double _yd = _ey - _sy;
                                            mapZZ = _ey - (mapZZ * _yd);

                                            gui.png("{parent}", "Fnd" + DateTime.Now.Ticks, fetchImage(friendEntity.icon), (mapXX - iconsize) + " " + (mapZZ - iconsize), (mapXX + iconsize) + " " + (mapZZ + iconsize), "1");
                                        }
                                    }
                                }

                                // Player
                                int prow = (Convert.ToInt16(Math.Floor(mapslices * mapX)));
                                int pcolumn = ((mapslices - 1) - Convert.ToInt16(Math.Floor(mapslices * mapZ)));

                                if (pcolumn == (currentz + r) && prow == (currentx + c))
                                {
                                    double _sx = Convert.ToSingle(c * (1f / col));
                                    double _sy = Convert.ToSingle(1 - ((1f / row) * (r + 1)));
                                    double _ex = Convert.ToSingle(((c + 1) * (1f / col)) - 0.005f);
                                    double _ey = Convert.ToSingle((1 - ((1f / row) * (r + 1)) + (1f / row)) - 0.004f);
                                    double mapXX = (mapX * mapslices) - prow;
                                    double mapZZ = ((1 - mapZ) * mapslices) - pcolumn;
                                    double _xd = _ex - _sx;
                                    mapXX = (mapXX * _xd) + _sx;
                                    double _yd = _ey - _sy;
                                    mapZZ = _ey - (mapZZ * _yd);

                                    gui.png("{parent}", "Player" + DateTime.Now.Ticks, fetchImage("self" + playerdirection), (mapXX - iconsize) + " " + (mapZZ - iconsize), (mapXX + iconsize) + " " + (mapZZ + iconsize));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (activeEntities.Count > 0)
                        {
                            foreach (ActiveEntity entity in activeEntities)
                            {
                                if ((entity.isplane && showplane && user.mapPlane) || (entity.isheli && showheli && user.mapHeli) || (entity.issupply && showsupply && user.mapSupply) || (entity.isdebris && showdebris && user.mapDebris))
                                {
                                    if (entity.percentX >= 0 && entity.percentX <= 1 && entity.percentZ >= 0 && entity.percentZ <= 1)
                                    {
                                        gui.png("{parent}", "Ent" + DateTime.Now.Ticks, fetchImage(entity.icon), (entity.percentX - iconsize) + " " + (entity.percentZ - iconsize), (entity.percentX + iconsize) + " " + (entity.percentZ + iconsize));
                                    }
                                }
                            }
                        }
                        if (user.friends.Count > 0)
                        {
                            foreach (MapLocation friendEntity in user.friends)
                            {
                                if (friendEntity.percentX >= 0 && friendEntity.percentX <= 1 && friendEntity.percentZ >= 0 && friendEntity.percentZ <= 1)
                                {
                                    gui.png("{parent}", "Fnd" + DateTime.Now.Ticks, fetchImage(friendEntity.icon), (friendEntity.percentX - iconsize) + " " + (friendEntity.percentZ - iconsize), (friendEntity.percentX + iconsize) + " " + (friendEntity.percentZ + iconsize));
                                }
                            }
                        }
                        gui.png("{parent}", "Player" + DateTime.Now.Ticks, fetchImage("self" + playerdirection), (mapX - iconsize) + " " + (mapZ - iconsize), (mapX + iconsize) + " " + (mapZ + iconsize));
                    }
                    if (user.compass)
                    {
                        gui.text("{parent}", "Location", TextAnchor.UpperCenter, "<size=12>" + txtCpsHead + " " + direction + "\n" + player.transform.position + "</size>", "0 -0.2", "1 0");
                    }
                    gui.send(player);
                }
            }
        }

        // GUI Class
        private class GUIv4
        {
            string guiname { get; set; }
            CuiElementContainer container = new CuiElementContainer();

            public void add(string uiname, bool mouse, string start, string end, string colour)
            {
                guiname = uiname;
                if (mouse)
                {
                    CuiElement element = new CuiElement
                    {
                        Name = guiname,
                        Parent = "HUD/Overlay",
                        FadeOut = 0.0f,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = colour
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = start,
                                AnchorMax = end
                            },
                            new CuiNeedsCursorComponent()
                        }
                    };
                    container.Add(element);
                }
                else
                {
                    CuiElement element = new CuiElement
                    {
                        Name = guiname,
                        Parent = "HUD/Overlay",
                        FadeOut = 0.0f,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = colour
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = start,
                                AnchorMax = end
                            }
                        }
                    };
                    container.Add(element);
                }
            }

            public void box(string uiparent, string uiname, string start, string end, string colour)
            {
                if (uiparent == "{parent}") { uiparent = guiname; } else { uiparent += "{rand}"; }

                CuiElement element = new CuiElement
                {
                    Name = uiname + "{rand}",
                    Parent = uiparent,
                    FadeOut = 0.0f,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = colour
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = start,
                            AnchorMax = end
                        }
                    }
                };
                container.Add(element);
            }

            public void text(string uiparent, string uiname, UnityEngine.TextAnchor textalign, string uitext, string start, string end)
            {
                if (uiparent == "{parent}") { uiparent = guiname; } else { uiparent += "{rand}"; }

                CuiElement element = new CuiElement
                {
                    Name = uiname + "{rand}",
                    Parent = uiparent,
                    FadeOut = 0.0f,
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = uitext,
                                FontSize = 12,
                                Align = textalign,
                                FadeIn = 0.0f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = start,
                                AnchorMax = end
                            }
                        }
                };
                container.Add(element);
            }

            public void png(string uiparent, string uiname, string image, string start, string end, string colour = "1 1 1 1")
            {
                if (string.IsNullOrEmpty(image)) return;
                if (uiparent == "{parent}") { uiparent = guiname; } else { uiparent += "{rand}"; }

                CuiElement element = new CuiElement
                {
                    Name = uiname + "{rand}",
                    Parent = uiparent,
                    FadeOut = 0.0f,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Sprite = "assets/content/textures/generic/fulltransparent.tga",
                            Png = image,
                            FadeIn = 0.0f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = start,
                            AnchorMax = end
                        }
                    }
                };
                container.Add(element);
            }

            public void button(string uiparent, string uiname, UnityEngine.TextAnchor textalign, string uitext, bool closeui, string cmd, string start, string end, string colour)
            {
                box(uiparent, uiname + "BoX", start, end, colour);
                text(uiparent, uiname + "TxT", textalign, uitext, start, end);

                if (uiparent == "{parent}") { uiparent = guiname; } else { uiparent += "{rand}"; }
                string closegui = null;
                if (closeui) { closegui = guiname; }

                CuiElement element = new CuiElement
                {
                    Name = uiname + "{rand}",
                    Parent = uiparent,
                    FadeOut = 0.0f,
                    Components =
                        {
                            new CuiButtonComponent
                            {
                                Command = cmd,
                                Close = closegui,
                                Color = "0 0 0 0"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = start,
                                AnchorMax = end
                            }
                        }
                };
                container.Add(element);
            }

            public void send(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, guiname);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(container.ToJson().Replace("{rand}", DateTime.Now.Ticks.ToString())));
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
