// Reference: System.Drawing
using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Collections;
using System.Drawing;

namespace Oxide.Plugins
{
    [Info("LustyMap", "Kayzor / k1lly0u", "2.0.6", ResourceId = 1333)]
    class LustyMap : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin EventManager;
        [PluginReference] Plugin Friends;
        [PluginReference] Plugin Clans;

        static GameObject webObject;
        static ImageAssets assets;
        static GameObject mapObject;
        static MapSplitter mapSplitter;

        static LustyMap instance;
        static float mapSize;
        static string mapSeed;
        static string worldSize;
        static string level;

        private bool activated;
        private bool isNewSave;
        private bool isRustFriends;
        
        ImageStore storedImages;
        private DynamicConfigFile imageData;

        MarkerData storedMarkers;
        private DynamicConfigFile markerData;

        private Dictionary<string, MapUser> mapUsers;

        private List<MapMarker> staticMarkers;
        private Dictionary<string, MapMarker> customMarkers;
        private Dictionary<uint, ActiveEntity> temporaryMarkers;

        private Dictionary<string, List<string>> clanData;

        static string dataDirectory = $"file://{Interface.Oxide.DataDirectory}{Path.DirectorySeparatorChar}LustyMap{Path.DirectorySeparatorChar}";
        #endregion
        
        #region User Class  
        class MapUser : MonoBehaviour
        {
            private Dictionary<string, List<string>> friends;
            public HashSet<string> friendList;

            public BasePlayer player;
            public MapMode mode;
            public MapMode lastMode;

            private MapMarker marker;

            private int mapX;
            private int mapZ;

            private int currentX;
            private int currentZ;
            private int mapZoom;

            private bool mapOpen;
            private bool inEvent;
            private bool adminMode;

            private int changeCount;
            private double lastChange;
            private bool isBlocked;

            private SpamOptions spam;
                    
            void Awake()
            {                
                player = GetComponent<BasePlayer>();
                friends = new Dictionary<string, List<string>>
                {
                    {"Clans", new List<string>() },
                    {"FriendsAPI", new List<string>() }
                };
                friendList = new HashSet<string>();
                spam = instance.configData.SpamOptions;             
                inEvent = false;
                mapOpen = false;
                mode = MapMode.None;
                lastMode = MapMode.None;
                adminMode = false;
                InvokeRepeating("UpdateMarker", 0.1f, 1f);
            }
            void OnDestroy()
            {
                CancelInvoke();
                DestroyUI();
            }
            public void InitializeComponent()
            {
                if (MapSettings.friends)
                {
                    FillFriendList();
                }
                if (MapSettings.forcedzoom && MapSettings.complexmap)
                {
                    if (!instance.configData.MapOptions.StartOpen)
                        ToggleMapType(MapMode.None);
                    else
                    {
                        mapZoom = MapSettings.zoomlevel;
                        ToggleMapType(MapMode.Complex);
                    }
                }
                else if (MapSettings.minimap)
                {
                    if (!instance.configData.MapOptions.StartOpen)
                        ToggleMapType(MapMode.None);
                    else
                    {
                        mode = MapMode.Minimap;
                        ToggleMapType(mode);
                    }
                }                
            }

            #region Friends
            private void FillFriendList()
            {                
                if (instance.configData.FriendOptions.UseClans)
                {
                    var clanTag = instance.GetClan(player.userID);
                    if (!string.IsNullOrEmpty(clanTag) && instance.clanData.ContainsKey(clanTag))                    
                        friends["Clans"] = instance.clanData[clanTag];
                }
                    
                if (instance.configData.FriendOptions.UseFriends)
                    friends["FriendsAPI"] = instance.GetFriends(player.userID);
                UpdateMembers();
            }
            private void UpdateMembers()
            {
                friendList.Clear();
                foreach (var list in friends)
                {
                    foreach (var member in list.Value)
                        friendList.Add(member);
                }
            }
            #endregion

            #region Maps
            public float Rotation() => GetDirection(player.transform.rotation.eulerAngles.y);
            public int Position(bool x) => x ? mapX : mapZ;            
            public void Position(bool x, int pos)
            {
                if (x) mapX = pos;
                else mapZ = pos;
            } 
                        
            public void ToggleMapType(MapMode mapMode)
            {
                if (isBlocked || IsSpam()) return;

                DestroyUI();               

                if (mapMode == MapMode.None)
                {
                    CancelInvoke("UpdateMap");                   
                    mode = MapMode.None;
                    mapOpen = false;

                    if (MapSettings.minimap)                    
                        instance.CreateShrunkUI(player);                    
                }
                else
                {               
                    mapOpen = true;
                    switch (mapMode)
                    {
                        case MapMode.Main:
                            mode = MapMode.Main;
                            instance.OpenMainMap(player);
                            break;
                        case MapMode.Complex:
                            mode = MapMode.Complex;                            
                            instance.OpenComplexMap(player);
                            break;
                        case MapMode.Minimap:
                            mode = MapMode.Minimap;
                            instance.OpenMiniMap(player);
                            break;
                    }
                    if (!IsInvoking("UpdateMap"))                    
                        InvokeRepeating("UpdateMap", 0.1f, instance.configData.MapOptions.UpdateSpeed);                    
                }     
            }                      
            public void UpdateMap()
            {                
                switch (mode)
                {
                    case MapMode.None:
                        break;
                    case MapMode.Main:
                        instance.UpdateOverlay(player, LustyUI.MainOverlay, LustyUI.MainMin, LustyUI.MainMax, 0.01f);
                        break;
                    case MapMode.Complex:
                        CheckForChange();
                        instance.UpdateCompOverlay(player);
                        break;
                    case MapMode.Minimap:
                        instance.UpdateOverlay(player, LustyUI.MiniOverlay, LustyUI.MiniMin, LustyUI.MiniMax, 0.03f);
                        break;
                }
            }
            #endregion

            #region Complex
            public bool HasMapOpen() => (mapOpen && mode == MapMode.Main);
            public int Zoom() => mapZoom;
            public void Zoom(bool zoomIn)
            {
                var zoom = mapZoom;
                if (zoomIn)
                {
                    if (zoom < 3)
                        zoom++;
                    else return;
                }
                else
                {
                    if (zoom > 0)
                        zoom--;
                    else return;
                }
                if (IsInvoking("UpdateMap"))
                    CancelInvoke("UpdateMap");
                SwitchZoom(zoom);
            }
            private void SwitchZoom(int zoom)
            {
                if (zoom == 0 && MapSettings.minimap)
                {                    
                    mapZoom = zoom;
                    ToggleMapType(MapMode.Minimap);
                }
                else
                {
                    if (zoom == 0 && !MapSettings.minimap)
                        zoom = 1;
                    
                    mapZoom = zoom;
                    currentX = 0;
                    currentZ = 0;
                    ToggleMapType(MapMode.Complex);
                }
            }
            public int Current(bool x) => x ? currentX : currentZ;
            public void Current(bool x, int num)
            {
                if (x) currentX = num;
                else currentZ = num;
            }           
            private void CheckForChange()
            {
                var mapSlices = ZoomToCount(mapZoom);
                float x = player.transform.position.x + mapSize / 2f;
                float z = player.transform.position.z + mapSize / 2f;
                var mapres = mapSize / mapSlices;

                var newX = Convert.ToInt32(Math.Ceiling(x / mapres)) - 1;
                var newZ = mapSlices - Convert.ToInt32(Math.Ceiling(z / mapres));

                if (currentX != newX || currentZ != newZ)
                {
                    DestroyUI();
                    currentX = newX;
                    currentZ = newZ;
                    var container = LustyUI.StaticComplex[mapZoom][newX, newZ];
                    instance.OpenComplexMap(player);
                }
            }
            #endregion

            #region Spam Checking
            private bool IsSpam()
            {
                if (!spam.Enabled) return false;

                changeCount++;
                var current = GrabCurrentTime();
                if (current - lastChange < spam.TimeBetweenAttempts)
                {
                    lastChange = current;
                    if (changeCount > spam.WarningAttempts && changeCount < spam.DisableAttempts)
                    {
                        instance.SendReply(player, instance.msg("spamWarning", player.UserIDString));
                        return false;
                    }
                    if (changeCount >= spam.DisableAttempts)
                    {
                        instance.SendReply(player, string.Format(instance.msg("spamDisable", player.UserIDString), spam.DisableSeconds));
                        Block();
                        Invoke("Unblock", spam.DisableSeconds);
                        return true;
                    }
                }
                else
                {
                    lastChange = current;
                    changeCount = 0;
                }
                return false;
            }
            private void Block()
            {                
                isBlocked = true;
                OnDestroy();
            }
            private void Unblock()
            {
                isBlocked = false;
                ToggleMapType(lastMode);
                instance.SendReply(player, instance.msg("spamEnable", player.UserIDString));
            }
            #endregion

            #region Other
            public bool InEvent() => inEvent;
            public bool IsAdmin() => adminMode;
            public void ToggleEvent(bool isPlaying) => inEvent = isPlaying;
            public void ToggleAdmin(bool enabled) => adminMode = enabled;
            public void DestroyUI() => LustyUI.DestroyUI(player);
            private void UpdateMarker() => marker = new MapMarker { name = RemoveSpecialCharacters(player.displayName), r = GetDirection(player.eyes.rotation.eulerAngles.y), x = GetPosition(transform.position.x), z = GetPosition(transform.position.z) };
            public MapMarker GetMarker() => marker;
            
            public void ToggleMain()
            {
                if (HasMapOpen())
                {                    
                    ToggleMapType(lastMode);
                }
                else
                {
                    lastMode = mode;
                    CuiHelper.DestroyUi(player, LustyUI.Buttons);
                    ToggleMapType(MapMode.Main);
                }
            }
            public void DisableUser()
            {
                if (!mapOpen) return;
                lastMode = mode;
                CancelInvoke("UpdateMap");
                CuiHelper.DestroyUi(player, LustyUI.Buttons);
                if (mode != MapMode.None)
                    LustyUI.DestroyUI(player);
                mode = MapMode.None;
                mapOpen = false;
            }
            public void EnableUser()
            {
                if (mapOpen) return;
                ToggleMapType(lastMode);
            }
            public void EnterEvent() => inEvent = true;
            public void ExitEvent() => inEvent = false;

            #region Friends
            public bool HasFriendList(string name) => friends.ContainsKey(name);
            public void AddFriendList(string name, List<string> friendlist) { friends.Add(name, friendlist); UpdateMembers(); }
            public void RemoveFriendList(string name) { friends.Remove(name); UpdateMembers(); }
            public void UpdateFriendList(string name, List<string> friendlist) { friends[name] = friendlist; UpdateMembers(); }

            public bool HasFriend(string name, string friendId) => friends[name].Contains(friendId);
            public void AddFriend(string name, string friendId) { friends[name].Add(friendId); UpdateMembers(); }
            public void RemoveFriend(string name, string friendId) { friends[name].Remove(friendId); UpdateMembers(); }
            #endregion
            #endregion
        }

        MapUser GetUser(BasePlayer player) => player.GetComponent<MapUser>() ?? null;
        MapUser GetUserByID(string playerId) => mapUsers.ContainsKey(playerId) ? mapUsers[playerId] : null;
        #endregion

        #region Markers
        class ActiveEntity : MonoBehaviour
        {
            public BaseEntity entity;
            private MapMarker marker;
            private AEType type;
            private string icon;

            void Awake()
            {
                entity = GetComponent<BaseEntity>();
                marker = new MapMarker();              
            }
            void OnDestroy()
            {
                CancelInvoke("UpdatePosition");
            }
            public void SetType(AEType type)
            {
                this.type = type;
                switch (type)
                {
                    case AEType.None:
                        break;
                    case AEType.Plane:
                        icon = "plane";
                        marker.name = instance.msg("Plane");
                        break;
                    case AEType.SupplyDrop:
                        icon = "supply";
                        marker.name = instance.msg("Supply Drop");
                        break;
                    case AEType.Helicopter:
                        icon = "heli";
                        marker.name = instance.msg("Helicopter");
                        break;
                    case AEType.Debris:
                        icon = "debris";
                        marker.name = instance.msg("Debris");
                        break;
                }
                InvokeRepeating("UpdatePosition", 0.1f, 1f);
            }
            public MapMarker GetMarker() => marker;
            void UpdatePosition()
            {                
                if (type == AEType.Helicopter || type == AEType.Plane)
                {
                    marker.r = GetDirection(entity.transform.rotation.eulerAngles.y);
                    marker.icon = $"{icon}{marker.r}";
                }
                else marker.icon = $"{icon}";
                marker.x = GetPosition(entity.transform.position.x);
                marker.z = GetPosition(entity.transform.position.z);
            }
        }
        class MapMarker
        {
            public string name { get; set; }
            public float x { get; set; }
            public float z { get; set; }
            public float r { get; set; }
            public string icon { get; set; }
        }
        static class MapSettings
        {
            static public bool minimap, complexmap, monuments, names, compass, caves, plane, heli, supply, debris, player, allplayers, friends, forcedzoom;
            static public int zoomlevel;       
        }
        public enum MapMode
        {
            None,
            Main,
            Complex,
            Minimap
        }
        enum AEType
        {
            None,
            Plane,
            SupplyDrop,
            Helicopter,
            Debris            
        }
        #endregion

        #region UI
        class LMUI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false, string parent = "Hud")
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
                    new CuiElement().Parent = parent,
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
                panel, CuiHelper.GetGuid());
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {                
                container.Add(new CuiLabel
                {                    
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = 0, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel, CuiHelper.GetGuid());

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {                
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0 },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel, CuiHelper.GetGuid());
            }
            static public void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }            
        }        
        #endregion

        #region Oxide Hooks
        void OnNewSave(string filename)
        {
            isNewSave = true;
        }
        void Loaded()
        {
            imageData = Interface.Oxide.DataFileSystem.GetFile($"LustyMap{Path.DirectorySeparatorChar}ImageData");
            markerData = Interface.Oxide.DataFileSystem.GetFile($"LustyMap{Path.DirectorySeparatorChar}CustomData");

            mapUsers = new Dictionary<string, MapUser>();
            staticMarkers = new List<MapMarker>();
            customMarkers = new Dictionary<string, MapMarker>();
            temporaryMarkers = new Dictionary<uint, ActiveEntity>();
            clanData = new Dictionary<string, List<string>>();

            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission("lustymap.admin", this);
        }
        void OnServerInitialized()
        {
            instance = this;

            worldSize = ConsoleSystem.ConVar.GetString("worldsize");
            mapSeed = ConsoleSystem.ConVar.GetString("seed");
            level = ConVar.Server.level;
            mapSize = TerrainMeta.Size.x;

            webObject = new GameObject("WebObject");
            assets = webObject.AddComponent<ImageAssets>();

            mapObject = new GameObject("MapGenObject");
            mapSplitter = mapObject.AddComponent<MapSplitter>();

            LoadVariables();
            LoadData();            
            LoadSettings();

            FindStaticMarkers();

            ValidateImages();

            CheckFriends();
            GetClans();
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (player == null) return;
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot) || player.IsSleeping())
            {
                timer.In(2, () => OnPlayerInit(player));
                return;
            }
            if (activated)
            {
                if (!string.IsNullOrEmpty(configData.MapOptions.MapKeybind))                
                    player.Command("bind " + configData.MapOptions.MapKeybind + " LMUI_Control map");                              

                var user = GetUser(player);
                if (user != null)
                {
                    UnityEngine.Object.DestroyImmediate(user);
                    if (mapUsers.ContainsKey(player.UserIDString))
                        mapUsers.Remove(player.UserIDString);
                }

                var mapUser = player.gameObject.AddComponent<MapUser>();
                if (!mapUsers.ContainsKey(player.UserIDString))
                    mapUsers.Add(player.UserIDString, mapUser);
                mapUser.InitializeComponent();
            }
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (player == null) return;
            if (activated)
            {
                if (!string.IsNullOrEmpty(configData.MapOptions.MapKeybind))
                    player.Command("bind " + configData.MapOptions.MapKeybind + " \"\"");
            }
            if (mapUsers.ContainsKey(player.UserIDString))
                mapUsers.Remove(player.UserIDString);

            if (player.GetComponent<MapUser>())
                UnityEngine.Object.Destroy(player.GetComponent<MapUser>());

            LustyUI.DestroyUI(player);
        }
        void OnEntitySpawned(BaseEntity entity)
        {
            if (!activated) return;
            if (entity == null) return;
            if (entity is CargoPlane || entity is SupplyDrop || entity is BaseHelicopter || entity is HelicopterDebris)
                AddTemporaryMarker(entity);
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            var activeEntity = entity.GetComponent<ActiveEntity>();
            if (activeEntity == null) return;
            if (entity?.net?.ID == null) return;
            if (temporaryMarkers.ContainsKey(entity.net.ID))
                temporaryMarkers.Remove(entity.net.ID);
            UnityEngine.Object.Destroy(activeEntity);
        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerDisconnected(player);

            var components = UnityEngine.Object.FindObjectsOfType<MapUser>();
            if (components != null)
                foreach (var component in components)
                    UnityEngine.Object.DestroyImmediate(component);
        }
        #endregion

        #region Static UI Generation
        static class LustyUI
        {
            public static string Main = "LMUI_MapMain";
            public static string Mini = "LMUI_MapMini";
            public static string Complex = "LMUI_Complex";
            public static string MainOverlay = "LMUI_MainOverlay";
            public static string MiniOverlay = "LMUI_MiniOverlay";
            public static string ComplexOverlay = "LMUI_ComplexOverlay";
            public static string Buttons = "LMUI_Buttons";

            public static string MainMin;
            public static string MainMax;
            public static string MiniMin;
            public static string MiniMax;

            public static CuiElementContainer StaticMain;
            public static CuiElementContainer StaticMini;
            public static Dictionary<int, CuiElementContainer[,]> StaticComplex = new Dictionary<int, CuiElementContainer[,]>();

            private static Dictionary<ulong, List<string>> OpenUI = new Dictionary<ulong, List<string>>();

            public static void RenameComponents()
            {
                if (StaticMain != null)
                {
                    foreach (var element in StaticMain)
                    {
                        if (element.Name == "AddUI CreatedPanel")
                            element.Name = CuiHelper.GetGuid();
                    }
                }
                if (StaticMini != null)
                {
                    foreach (var element in StaticMini)
                    {
                        if (element.Name == "AddUI CreatedPanel")
                            element.Name = CuiHelper.GetGuid();
                    }
                }
                if (StaticComplex != null)
                {
                    foreach (var size in StaticComplex)
                    {
                        foreach (var piece in size.Value)
                        {
                            foreach (var element in piece)
                            {
                                if (element.Name == "AddUI CreatedPanel")
                                    element.Name = CuiHelper.GetGuid();
                            }
                        }
                    }
                }
                instance.activated = true;
                if (instance.configData.MapOptions.StartOpen)
                    instance.ActivateMaps();
            }
            public static void AddBaseUI(BasePlayer player, MapMode type)
            {
                var user = instance.GetUser(player);
                if (user == null) return;

                DestroyUI(player);
                CuiElementContainer element = null;
                switch (type)
                {
                    case MapMode.None:
                        return;
                    case MapMode.Main:
                        element = StaticMain;
                        CuiHelper.AddUi(player, StaticMain);
                        AddElementIds(player, ref element);
                        return;
                    case MapMode.Complex:
                        element = StaticComplex[user.Zoom()][user.Current(true), user.Current(false)];
                        instance.AddMapButtons(player);
                        CuiHelper.AddUi(player, element);
                        AddElementIds(player, ref element);
                        return;
                    case MapMode.Minimap:
                        element = StaticMini;
                        instance.AddMapButtons(player);
                        CuiHelper.AddUi(player, element);
                        AddElementIds(player, ref element);
                        return;                    
                }
            }
            private static void AddElementIds(BasePlayer player, ref CuiElementContainer container)
            {
                if (!OpenUI.ContainsKey(player.userID))
                    OpenUI.Add(player.userID, new List<string>());
                foreach (var piece in container)
                    OpenUI[player.userID].Add(piece.Name);               
            }
            public static void DestroyUI(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, Buttons);
                CuiHelper.DestroyUi(player, Main);
                CuiHelper.DestroyUi(player, MainOverlay);
                CuiHelper.DestroyUi(player, Mini);
                CuiHelper.DestroyUi(player, MiniOverlay);
                CuiHelper.DestroyUi(player, Complex);
                CuiHelper.DestroyUi(player, ComplexOverlay);
                if (!OpenUI.ContainsKey(player.userID)) return;
                foreach (var piece in OpenUI[player.userID])
                    CuiHelper.DestroyUi(player, piece);
            }
            /* public static void DestroyUI(BasePlayer player, MapMode type)
             {
                 CuiHelper.DestroyUi(player, Buttons);
                 CuiElementContainer element = null;

                 switch (type)
                 {
                     case MapMode.Main:
                         element = StaticMain;
                         CuiHelper.DestroyUi(player, Main);
                         CuiHelper.DestroyUi(player, MainOverlay);
                         break;
                     case MapMode.Minimap:
                         element = StaticMini;
                         CuiHelper.DestroyUi(player, Mini);
                         CuiHelper.DestroyUi(player, MiniOverlay);
                         break;
                     case MapMode.Complex:
                         {
                             var user = instance.GetUser(player);
                             if (user != null)
                             {
                                 int index = user.Zoom();
                                 int row = user.Position(false);
                                 int column = user.Position(true);
                                 foreach (var e in StaticComplex[index][column, row])
                                     CuiHelper.DestroyUi(player, e.Name);
                             }
                             CuiHelper.DestroyUi(player, Complex);
                             CuiHelper.DestroyUi(player, ComplexOverlay);
                         }
                         return;
                     case MapMode.None:
                         return;
                 }

                 if (element != null)
                     foreach (var piece in element)
                         CuiHelper.DestroyUi(player, piece.Name);
             }     
             public static void DestroyAllUI(BasePlayer player) // Avoid if possible
             {
                 if (player == null) return;

                 if (StaticMain != null)
                     foreach (var piece in StaticMain)
                         CuiHelper.DestroyUi(player, piece.Name);

                 if (StaticMini != null)
                     foreach (var piece in StaticMini)
                         CuiHelper.DestroyUi(player, piece.Name);

                 if (StaticComplex != null)
                     foreach (var piece in StaticComplex)
                         foreach(var e in piece.Value)
                             foreach (var c in e)
                                 CuiHelper.DestroyUi(player, c.Name);

                 CuiHelper.DestroyUi(player, Buttons);
                 CuiHelper.DestroyUi(player, Main);
                 CuiHelper.DestroyUi(player, MainOverlay);               
                 CuiHelper.DestroyUi(player, Mini);
                 CuiHelper.DestroyUi(player, MiniOverlay);                
                 CuiHelper.DestroyUi(player, Complex);
                 CuiHelper.DestroyUi(player, ComplexOverlay);
             }            */
            public static string Color(string hexColor, float alpha)
            {
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }

        void GenerateMaps(bool main, bool mini, bool complex)
        {
            if (main) CreateStaticMain();
            SetMinimapSize();
            if (mini) CreateStaticMini();
            if (complex) CreateStaticComplex();
        }
        void SetMinimapSize()
        {
            float startx = 0f + configData.MapOptions.MinimapOptions.OffsetSide;
            float endx = startx + (0.13f * configData.MapOptions.MinimapOptions.HorizontalScale);
            float endy = 1f - configData.MapOptions.MinimapOptions.OffsetTop;
            float starty = endy - (0.2301f * configData.MapOptions.MinimapOptions.VerticalScale);
            if (!configData.MapOptions.MinimapOptions.OnLeftSide)
            {
                endx = 1 - configData.MapOptions.MinimapOptions.OffsetSide;
                startx = endx - (0.13f * configData.MapOptions.MinimapOptions.HorizontalScale);
            }
            LustyUI.MiniMin = $"{startx} {starty}";
            LustyUI.MiniMax = $"{endx} {endy}";
        }
        void CreateStaticMain()
        {
            PrintWarning("Generating the main map");
            var mapimage = GetImage("mapimage");
            if (string.IsNullOrEmpty(mapimage))
            {
                PrintError("Unable to load the map image from file storage. This may be caused by slow processing of the images being uploaded to your server. If this problem persists unload the plugin and delete your ImageData.json data file");
                activated = false;
                return;
            }
            float iconsize = 0.01f;
            LustyUI.MainMin = "0.2271875 0.015";
            LustyUI.MainMax = "0.7728125 0.985";

            var mapContainer = LMUI.CreateElementContainer(LustyUI.Main, "0 0 0 1", LustyUI.MainMin, LustyUI.MainMax, true);
            LMUI.LoadImage(ref mapContainer, LustyUI.Main, mapimage, "0 0", "1 1");
            LMUI.CreatePanel(ref mapContainer, LustyUI.Main, LustyUI.Color("2b627a", 1), "0 0.96", "1 1", true);
            LMUI.CreateLabel(ref mapContainer, LustyUI.Main, "", $"{Title}  v{Version}", 14, "0.01 0.96", "0.99 1");            

            foreach(var marker in staticMarkers)
            {
                var image = GetImage(marker.icon);
                if (string.IsNullOrEmpty(image)) continue;
                LMUI.LoadImage(ref mapContainer, LustyUI.Main, image, $"{marker.x - iconsize} {marker.z - iconsize}", $"{marker.x + iconsize} {marker.z + iconsize}");
                if (MapSettings.names)
                    LMUI.CreateLabel(ref mapContainer, LustyUI.Main, "", marker.name, 10, $"{marker.x - 0.1} {marker.z - iconsize - 0.03}", $"{marker.x + 0.1} {marker.z - iconsize}");
            }
            LustyUI.StaticMain = mapContainer;
            PrintWarning("Main map generated successfully!");
            if (!MapSettings.minimap)            
                LustyUI.RenameComponents();
        }
        void CreateStaticMini()
        {
            PrintWarning("Generating the mini-map");
            var mapimage = GetImage("mapimage");
            if (string.IsNullOrEmpty(mapimage))
            {
                PrintError("Unable to load the map image from file storage. This may be caused by slow processing of the images being uploaded to your server. If this problem persists unload the plugin and delete your ImageData.json data file");
                activated = false;
                return;
            }
            float iconsize = 0.03f;
            
            var mapContainer = LMUI.CreateElementContainer(LustyUI.Mini, "0 0 0 1", LustyUI.MiniMin, LustyUI.MiniMax);
            LMUI.LoadImage(ref mapContainer, LustyUI.Mini, mapimage, "0 0", "1 1");

            foreach (var marker in staticMarkers)
            {
                var image = GetImage(marker.icon);
                if (string.IsNullOrEmpty(image)) continue;
                LMUI.LoadImage(ref mapContainer, LustyUI.Mini, image, $"{marker.x - iconsize} {marker.z - iconsize}", $"{marker.x + iconsize} {marker.z + iconsize}");                
            }
            LustyUI.StaticMini = mapContainer;
            PrintWarning("Mini map generated successfully!");
            if (!MapSettings.complexmap)            
                LustyUI.RenameComponents();            
        }       
        void CreateStaticComplex()
        {
            PrintWarning("Generating the complex map. This may take a few moments, please wait!");            
            foreach (var mapslices in new List<int> { 6, 12, 26 })//, 32 })
            {
                for (int number = 0; number < (mapslices * mapslices); number++)
                {
                    int rowNum = 0;
                    int colNum = 0;
                    if (number > mapslices - 1)
                    {
                        colNum = Convert.ToInt32(Math.Floor((float)number / (float)mapslices));
                        rowNum = number - (colNum * mapslices);
                    }
                    else rowNum = number;
                    
                    var mapContainer = LMUI.CreateElementContainer(LustyUI.Complex, "0 0 0 1", LustyUI.MiniMin, LustyUI.MiniMax);

                    string imageId = GetImage($"map-{mapslices}-{rowNum}-{colNum}");
                    if (!string.IsNullOrEmpty(imageId))
                        LMUI.LoadImage(ref mapContainer, LustyUI.Complex, imageId, $"0 0", $"1 1");
                    else
                    {
                        PrintError($"Missing map piece (Slices: {mapslices}, Column: {colNum}, Row: {rowNum}). When the plugin is splitting the map you must wait for it to finish otherwise the split will not complete and this error will occur");
                        PrintError($"The plugin will now clear all image data and reload itself to re-initialize the map splitter.");
                        storedImages.data.Clear();
                        SaveData();
                        rust.RunServerCommand("oxide.reload", new object[] { "LustyMap" });
                        return;
                    }

                    double width = ((double)1 / (double)mapslices);
                    float iconsize = 0.03f;

                    var column = colNum;
                    var row = rowNum;
                    if (column < 1) column = 1;
                    if (column > mapslices - 2) column = mapslices - 2;
                    if (row < 1) row = 1;
                    if (row > mapslices - 2) row = mapslices - 2;                    

                    double colStart = (width * column) - width;
                    double colEnd = colStart + (width * 3);

                    double rowStart = 1 - ((width * row) - width);
                    double rowEnd = (rowStart - (width * 3));                   

                    foreach (var marker in staticMarkers)
                    {
                        string markerId = GetImage(marker.icon);
                        if (string.IsNullOrEmpty(markerId)) continue;

                        float x = marker.x;
                        float z = marker.z;
                        if ((x > colStart && x < colEnd) && (z > rowEnd && z < rowStart))
                        {
                            var average = 1 / (colEnd - colStart);
                            double posX = (x - colStart) * average;
                            double posZ = (z - rowEnd) * average;
                            LMUI.LoadImage(ref mapContainer, LustyUI.Complex, markerId, $"{posX - iconsize} {posZ - iconsize}", $"{posX + iconsize} {posZ + iconsize}");
                        }
                    }
                    int zoom = CountToZoom(mapslices);

                    if (!LustyUI.StaticComplex.ContainsKey(zoom))
                        LustyUI.StaticComplex.Add(zoom, new CuiElementContainer[mapslices, mapslices]);
                    LustyUI.StaticComplex[zoom][colNum, rowNum] = mapContainer;
                }
            }
            PrintWarning("Complex map generated successfully!");
            LustyUI.RenameComponents();            
        }
        
        static int ZoomToCount(int zoom)
        {
            switch (zoom)
            {
                case 1:
                    return 6;
                case 2:
                    return 12;
                case 3:
                    return 26;
                case 4:
                    return 32;
                default:
                    return 0;
            }
        }
        static int CountToZoom(int count)
        {
            switch (count)
            {
                case 6:
                    return 1;
                case 12:
                    return 2;
                case 26:
                    return 3;
                case 32:
                    return 4;
                default:
                    return 0;
            }
        }
        #endregion

        #region Maps
        void ActivateMaps()
        {
            foreach (var player in BasePlayer.activePlayerList)            
                OnPlayerInit(player);            
        }
        void CreateCompass(BasePlayer player, ref CuiElementContainer mapContainer, string panel, int fontsize, string offsetMin, string offsetMax)
        {
            string direction = null;
            float lookRotation = player.eyes.rotation.eulerAngles.y;
            int playerdirection = (Convert.ToInt16((lookRotation - 5) / 10 + 0.5) * 10);
            if (lookRotation >= 355) playerdirection = 0;
            if (lookRotation > 337.5 || lookRotation < 22.5) { direction = msg("cpsN"); }
            else if (lookRotation > 22.5 && lookRotation < 67.5) { direction = msg("cpsNE"); }
            else if (lookRotation > 67.5 && lookRotation < 112.5) { direction = msg("cpsE"); }
            else if (lookRotation > 112.5 && lookRotation < 157.5) { direction = msg("cpsSE"); }
            else if (lookRotation > 157.5 && lookRotation < 202.5) { direction = msg("cpsS"); }
            else if (lookRotation > 202.5 && lookRotation < 247.5) { direction = msg("cpsSW"); }
            else if (lookRotation > 247.5 && lookRotation < 292.5) { direction = msg("cpsW"); }
            else if (lookRotation > 292.5 && lookRotation < 337.5) { direction = msg("cpsNW"); }
            LMUI.CreateLabel(ref mapContainer, panel, "", $"<size={fontsize + 4}>{direction}</size> \n{player.transform.position}", fontsize, offsetMin, offsetMax, TextAnchor.UpperCenter);
        }
        void AddMapButtons(BasePlayer player)
        {
            float startx = 0f + configData.MapOptions.MinimapOptions.OffsetSide;
            float endx = startx + (0.13f * configData.MapOptions.MinimapOptions.HorizontalScale);
            float endy = 1f - configData.MapOptions.MinimapOptions.OffsetTop;
            float starty = endy - (0.2301f * configData.MapOptions.MinimapOptions.VerticalScale);
            string b_text = "<<<";
            var container = LMUI.CreateElementContainer(LustyUI.Buttons, "0 0 0 0", $"{endx + 0.001f} {starty}", $"{endx + 0.02f} {endy}");

            if (!configData.MapOptions.MinimapOptions.OnLeftSide)
            {
                endx = 1 - configData.MapOptions.MinimapOptions.OffsetSide;
                startx = endx - (0.13f * configData.MapOptions.MinimapOptions.HorizontalScale);
                b_text = ">>>";
                container = LMUI.CreateElementContainer(LustyUI.Buttons, "0 0 0 0", $"{startx - 0.02f} {starty}", $"{startx - 0.001f} {endy}");
            }
           
            LMUI.CreateButton(ref container, LustyUI.Buttons, LustyUI.Color("696969", 0.6f), b_text, 12, $"0 0.9", $"1 1", "LMUI_Control shrink");
            if (MapSettings.complexmap && !MapSettings.forcedzoom)
            {
                LMUI.CreateButton(ref container, LustyUI.Buttons, LustyUI.Color("696969", 0.6f), "+", 14, $"0 0.79", $"1 0.89", "LMUI_Control zoomin");
                LMUI.CreateButton(ref container, LustyUI.Buttons, LustyUI.Color("696969", 0.6f), "-", 14, $"0 0.68", $"1 0.78", "LMUI_Control zoomout");
            }
            CuiHelper.DestroyUi(player, LustyUI.Buttons);
            CuiHelper.AddUi(player, container);
        }
        void CreateShrunkUI(BasePlayer player)
        {
            var user = GetUser(player);
            if (user == null) return;

            float b_endy = 0.999f - configData.MapOptions.MinimapOptions.OffsetTop;
            float b_startx = 0.001f + configData.MapOptions.MinimapOptions.OffsetSide;
            float b_endx = b_startx + 0.02f;
            string b_text = ">>>";

            if (!configData.MapOptions.MinimapOptions.OnLeftSide)
            {                
                b_endx = 0.999f - configData.MapOptions.MinimapOptions.OffsetSide;
                b_startx = b_endx - 0.02f;
                b_text = "<<<";
            }                       
            var container = LMUI.CreateElementContainer(LustyUI.Buttons, "0 0 0 0", $"{b_startx} {b_endy - 0.025f}", $"{b_endx} {b_endy}");
            LMUI.CreateButton(ref container, LustyUI.Buttons, LustyUI.Color("696969", 0.6f), b_text, 12, "0 0", "1 1", "LMUI_Control expand");
            CuiHelper.DestroyUi(player, LustyUI.Buttons);
            CuiHelper.AddUi(player, container);
        }

        #region Standard Maps
        void OpenMainMap(BasePlayer player) => LustyUI.AddBaseUI(player, MapMode.Main);
        void OpenMiniMap(BasePlayer player) => LustyUI.AddBaseUI(player, MapMode.Minimap);
        void UpdateOverlay(BasePlayer player, string panel, string posMin, string posMax, float iconsize)
        {
            var mapContainer = LMUI.CreateElementContainer(panel, "0 0 0 0", posMin, posMax);

            var user = GetUser(player);
            if (user == null) return;            
            foreach (var marker in customMarkers)
            {
                var image = GetImage(marker.Value.name);
                if (string.IsNullOrEmpty(image)) continue;
                AddIconToMap(ref mapContainer, panel, image, marker.Value.name, iconsize * 1.25f, marker.Value.x, marker.Value.z);
            }
            foreach (var entity in temporaryMarkers)
            {
                var marker = entity.Value.GetMarker();
                if (marker == null) continue;
                var image = GetImage(marker.icon);
                if (string.IsNullOrEmpty(image)) continue;
                AddIconToMap(ref mapContainer, panel, image, "", iconsize * 1.4f, marker.x, marker.z);
            }            
            if (user.IsAdmin() || MapSettings.allplayers)
            {
                foreach (var mapuser in mapUsers)
                {
                    if (mapuser.Key == player.UserIDString) continue;

                    var marker = mapuser.Value.GetMarker();
                    if (marker == null) continue;
                    var image = GetImage($"other{marker.r}");
                    if (string.IsNullOrEmpty(image)) continue;
                    AddIconToMap(ref mapContainer, panel, image, marker.name, iconsize * 1.25f, marker.x, marker.z);                    
                }
            }
            else if (MapSettings.friends)
            {
                foreach (var friendId in user.friendList)
                {
                    if (friendId == player.UserIDString) continue;

                    if (mapUsers.ContainsKey(friendId))
                    {
                        var friend = mapUsers[friendId];
                        if (friend.InEvent() && configData.MapOptions.HideEventPlayers) continue;
                        var marker = friend.GetMarker();
                        if (marker == null) continue;
                        var image = GetImage($"friend{marker.r}");
                        if (string.IsNullOrEmpty(image)) continue;
                        AddIconToMap(ref mapContainer, panel, image, marker.name, iconsize * 1.25f, marker.x, marker.z);
                    }
                }
            }
            if (MapSettings.player)
            {
                var selfMarker = user.GetMarker();
                if (selfMarker != null)
                {
                    var selfImage = GetImage($"self{selfMarker.r}");
                    AddIconToMap(ref mapContainer, panel, selfImage, "", iconsize * 1.25f, selfMarker.x, selfMarker.z);
                }
            }

            if (panel == LustyUI.MainOverlay)
            {
                LMUI.CreateButton(ref mapContainer, panel, LustyUI.Color("88a8b6", 1), "X", 14, "0.95 0.961", "0.999 0.999", "LMUI_Control closeui");
                if (MapSettings.compass)
                    CreateCompass(player, ref mapContainer, panel, 14, "0.75 0.88", "1 0.95");
            }

            if (panel == LustyUI.MiniOverlay)
            {                
                if (MapSettings.compass)                
                    CreateCompass(player, ref mapContainer, panel, 10, "0 -0.25", "1 -0.02");                
            }

            CuiHelper.DestroyUi(player, panel);
            CuiHelper.AddUi(player, mapContainer);
        }
        void AddIconToMap(ref CuiElementContainer mapContainer, string panel, string image, string name, float iconsize, float posX, float posZ)
        {
            if (posX < iconsize || posX > 1 - iconsize || posZ < iconsize || posZ > 1 - iconsize) return;
            LMUI.LoadImage(ref mapContainer, panel, image, $"{posX - iconsize} {posZ - iconsize}", $"{posX + iconsize} {posZ + iconsize}");
            if (MapSettings.names)
                LMUI.CreateLabel(ref mapContainer, panel, "", name, 10, $"{posX - 0.1} {posZ - iconsize - 0.025}", $"{posX + 0.1} {posZ - iconsize}");
        }        
        #endregion

        #region Complex Maps
        void OpenComplexMap(BasePlayer player) => LustyUI.AddBaseUI(player, MapMode.Complex);
        void UpdateCompOverlay(BasePlayer player)
        {
            var mapContainer = LMUI.CreateElementContainer(LustyUI.ComplexOverlay, "0 0 0 0", LustyUI.MiniMin, LustyUI.MiniMax);

            var user = GetUser(player);
            if (user == null) return;

            var colNum = user.Current(true);
            var rowNum = user.Current(false);

            var mapslices = ZoomToCount(user.Zoom());
            double width = ((double)1 / (double)mapslices);
            float iconsize = 0.04f;

            var column = colNum;
            var row = rowNum;
            if (column < 1) column = 1;
            if (column > mapslices - 2) column = mapslices - 2;
            if (row < 1) row = 1;
            if (row > mapslices - 2) row = mapslices - 2;

            double colStart = (width * column) - width;
            double colEnd = colStart + (width * 3);

            double rowStart = 1 - ((width * row) - width);
            double rowEnd = (rowStart - (width * 3));

            foreach (var marker in customMarkers)
            {
                var image = GetImage(marker.Value.icon);
                if (string.IsNullOrEmpty(image)) continue;
                AddComplexIcon(ref mapContainer, LustyUI.ComplexOverlay, image, "", iconsize * 1.5f, marker.Value.x, marker.Value.z, colStart, colEnd, rowStart, rowEnd);
            }
            foreach (var entity in temporaryMarkers)
            {
                var marker = entity.Value.GetMarker();
                if (marker == null) continue;
                var image = GetImage(marker.icon);
                if (string.IsNullOrEmpty(image)) continue;
                AddComplexIcon(ref mapContainer, LustyUI.ComplexOverlay, image, "", iconsize * 1.6f, marker.x, marker.z, colStart, colEnd, rowStart, rowEnd);
            }
            if (user.IsAdmin() || MapSettings.allplayers)
            {
                foreach (var mapuser in mapUsers)
                {
                    if (mapuser.Key == player.UserIDString) continue;

                    var marker = mapuser.Value.GetMarker();
                    if (marker == null) continue;
                    var image = GetImage($"other{marker.r}");
                    if (string.IsNullOrEmpty(image)) continue;
                    AddComplexIcon(ref mapContainer, LustyUI.ComplexOverlay, image, "", iconsize * 1.25f, marker.x, marker.z, colStart, colEnd, rowStart, rowEnd);
                }
            }
            else if (MapSettings.friends)
            {
                foreach (var friendId in user.friendList)
                {
                    if (friendId == player.UserIDString) continue;

                    if (mapUsers.ContainsKey(friendId))
                    {
                        var friend = mapUsers[friendId];
                        if (friend.InEvent() && configData.MapOptions.HideEventPlayers) continue;
                        var marker = friend.GetMarker();
                        if (marker == null) continue;
                        var image = GetImage($"friend{marker.r}");
                        if (string.IsNullOrEmpty(image)) continue;
                        AddComplexIcon(ref mapContainer, LustyUI.ComplexOverlay, image, "", iconsize * 1.25f, marker.x, marker.z, colStart, colEnd, rowStart, rowEnd);
                    }
                }
            }            
            if (MapSettings.player)
            {
                var selfMarker = user.GetMarker();
                if (selfMarker != null)
                {
                    var selfImage = GetImage($"self{selfMarker.r}");
                    if (!string.IsNullOrEmpty(selfImage))
                        AddComplexIcon(ref mapContainer, LustyUI.ComplexOverlay, selfImage, "", iconsize * 1.25f, selfMarker.x, selfMarker.z, colStart, colEnd, rowStart, rowEnd);
                }
            }
            if (MapSettings.compass)
                CreateCompass(player, ref mapContainer, LustyUI.ComplexOverlay, 10, "0 -0.25", "1 -0.02");
            
            CuiHelper.DestroyUi(player, LustyUI.ComplexOverlay);
            CuiHelper.AddUi(player, mapContainer);
        }
        void AddComplexIcon(ref CuiElementContainer mapContainer, string panel, string image, string name, float iconsize, float x, float z, double colStart, double colEnd, double rowStart, double rowEnd)
        {
            if ((x > colStart && x < colEnd) && (z > rowEnd && z < rowStart))
            {
                var average = 1 / (colEnd - colStart);
                double posX = (x - colStart) * average;
                double posZ = (z - rowEnd) * average;

                if (posX < 0 + iconsize || posX > 1 - iconsize || posZ < 0 + iconsize || posZ > 1 - iconsize) return;
                LMUI.LoadImage(ref mapContainer, panel, image, $"{posX - iconsize} {posZ - iconsize}", $"{posX + iconsize} {posZ + iconsize}");
                if (MapSettings.names)
                    LMUI.CreateLabel(ref mapContainer, panel, "", name, 10, $"{posX - 0.1} {posZ - iconsize - 0.025}", $"{posX + 0.1} {posZ - iconsize}");
            }
        }
        #endregion
        #endregion

        #region Commands
        [ConsoleCommand("LMUI_Control")]
        private void cmdLustyControl(ConsoleSystem.Arg arg)
        {
            if (!activated) return;
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var user = GetUser(player);
            if (user == null) return;
            switch (arg.Args[0].ToLower())
            {
                case "map":                    
                    user.ToggleMain();
                    return;
                case "closeui":
                    if (MapSettings.minimap)
                    {
                        if (user.Zoom() > 0)
                            user.ToggleMapType(MapMode.Complex);
                        else user.ToggleMapType(MapMode.Minimap);
                    }
                    else user.ToggleMapType(MapMode.None);
                    return;
                case "shrink":
                    user.ToggleMapType(MapMode.None);
                    return;
                case "expand":
                    if (user.Zoom() > 0)
                        user.ToggleMapType(MapMode.Complex);
                    else user.ToggleMapType(MapMode.Minimap);
                    return;
                case "zoomin":
                    user.Zoom(true);
                    break;
                case "zoomout":
                    user.Zoom(false);
                    return;
                default:
                    return;
            }
        }
        [ConsoleCommand("resetmap")]
        void ccmdResetmap(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null) return;
            SendReply(arg, "ResetMap Confirmed! Removing stored images and reloading the plugin to re-download your map image");
            storedImages.data.Clear();
            SaveData();
            rust.RunServerCommand("oxide.reload", new object[] { "LustyMap " });
        }
        [ChatCommand("map")]
        void cmdOpenMap(BasePlayer player, string command, string[] args)
        {
            if (!activated)
            {
                SendReply(player, "LustyMap is not activated");
                return;
            }
            var user = GetUser(player);
            if (user == null)
            {
                user = player.gameObject.AddComponent<MapUser>();
                mapUsers.Add(player.UserIDString, user);
                user.InitializeComponent();
            }
            if (args.Length == 0)
                user.ToggleMapType(MapMode.Main);
            else
            {
                if (args[0].ToLower() == "mini")
                    user.ToggleMapType(MapMode.Minimap);
                if (args[0].ToLower() == "admin")
                {
                    if (!player.IsAdmin() && !permission.UserHasPermission(player.UserIDString, "lustymap.admin")) return;
                    if (user.IsAdmin())
                    {
                        user.ToggleAdmin(false);
                        SendReply(player, "Admin mode disabled");
                    }
                    else
                    {
                        user.ToggleAdmin(true);
                        SendReply(player, "Admin mode enabled");
                    }
                }
            }
        }

        [ChatCommand("marker")]
        void cmdMarker(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin() && !permission.UserHasPermission(player.UserIDString, "lustymap.admin")) return;
            if (args.Length == 0)
            {
                SendReply(player, "Add and remove markers / custom icons to the map. See the overview for information regarding using custom icons");
                SendReply(player, "/marker add <name> <opt:iconname> - Adds a new markers with the name specified at your location.");
                SendReply(player, "/marker remove <name> - Removes the marker with the name specified");
                return;
            }
            if (args.Length < 2)
            {
                SendReply(player, "You must enter a marker name!");
                return;
            }
            var name = args[1];
            switch (args[0].ToLower())
            {
                case "add":
                    string icon = "special";
                    if (args.Length > 2)
                        icon = args[2];
                    if (AddMarker(player.transform.position.x, player.transform.position.z, name, icon))
                        SendReply(player, $"You have successfully added a new map marker with the name: {name}");  
                    else SendReply(player, $"A map marker with the name \"{name}\" already exists");
                    return;
                case "remove":
                    if (RemoveMarker(name))
                        SendReply(player, $"You have successfully removed the map marker with the name: {name}");
                    else SendReply(player, $"No map marker with the name \"{name}\" exists");
                    return;
                default:
                    SendReply(player, "Incorrect syntax used. type \"/marker\" for more information");
                    break;
            }
        }
        #endregion

        #region Functions    
        private void CheckFriends()
        {
            if (Friends)
            {
                if (Friends.ResourceId == 686)               
                    isRustFriends = true;
            }
        }
        private void AddTemporaryMarker(BaseEntity entity)
        {
            if (entity == null) return;
            if (entity?.net?.ID == null) return;
            AEType type = AEType.None;
            if (entity is CargoPlane)
            {
                if (!configData.MapMarkers.ShowPlanes) return;
                type = AEType.Plane;                
            }
            else if (entity is BaseHelicopter)
            {
                if (!configData.MapMarkers.ShowHelicopters) return;
                type = AEType.Helicopter;               
            }
            else if (entity is SupplyDrop)
            {
                if (!configData.MapMarkers.ShowSupplyDrops) return;
                type = AEType.SupplyDrop;                
            }
            else if (entity is HelicopterDebris)
            {
                if (!configData.MapMarkers.ShowDebris) return;
                type = AEType.Debris;                
            }           
            var actEnt = entity.gameObject.AddComponent<ActiveEntity>();
            actEnt.SetType(type);

            temporaryMarkers.Add(entity.net.ID, actEnt);
        }
        private void LoadSettings()
        {
            MapSettings.caves = configData.MapMarkers.ShowCaves;
            MapSettings.compass = configData.MapOptions.ShowCompass;
            MapSettings.debris = configData.MapMarkers.ShowDebris;
            MapSettings.heli = configData.MapMarkers.ShowHelicopters;
            MapSettings.monuments = configData.MapMarkers.ShowMonuments;
            MapSettings.plane = configData.MapMarkers.ShowPlanes;
            MapSettings.player = configData.MapMarkers.ShowPlayer;
            MapSettings.allplayers = configData.MapMarkers.ShowAllPlayers;
            MapSettings.supply = configData.MapMarkers.ShowSupplyDrops;
            MapSettings.friends = configData.MapMarkers.ShowFriends;
            MapSettings.names = configData.MapMarkers.ShowMarkerNames;
            MapSettings.minimap = configData.MapOptions.MinimapOptions.UseMinimap;
            MapSettings.complexmap = configData.MapOptions.MinimapOptions.ComplexOptions.UseComplexMap;
            MapSettings.forcedzoom = configData.MapOptions.MinimapOptions.ComplexOptions.ForceMapZoom;
            MapSettings.zoomlevel = configData.MapOptions.MinimapOptions.ComplexOptions.ForcedZoomLevel;

            if (MapSettings.zoomlevel < 1)
                MapSettings.zoomlevel = 1;
            if (MapSettings.zoomlevel > 3)
                MapSettings.zoomlevel = 3;
        }        
        private void FindStaticMarkers()
        {
            if (MapSettings.monuments)
            { 
                var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
                foreach (var monument in monuments)
                {
                    MapMarker mon = new MapMarker
                    {
                        x = GetPosition(monument.transform.position.x),
                        z = GetPosition(monument.transform.position.z)
                    };

                    if (monument.name.Contains("lighthouse"))
                    {
                        mon.name = msg("lighthouse");
                        mon.icon = "lighthouse";
                        staticMarkers.Add(mon);
                        continue;
                    }
                    if (monument.Type == MonumentType.Cave && MapSettings.caves)
                    {
                        mon.name = msg("cave");
                        mon.icon = "cave";
                        staticMarkers.Add(mon);
                        continue;
                    }
                    if (monument.name.Contains("powerplant_1"))
                    {
                        mon.name = msg("powerplant");
                        mon.icon = "special";
                        staticMarkers.Add(mon);
                        continue;
                    }

                    if (monument.name.Contains("military_tunnel_1"))
                    {
                        mon.name = msg("militarytunnel");
                        mon.icon = "special";
                        staticMarkers.Add(mon);
                        continue;
                    }

                    if (monument.name.Contains("airfield_1"))
                    {
                        mon.name = msg("airfield");
                        mon.icon = "special";
                        staticMarkers.Add(mon);
                        continue;
                    }

                    if (monument.name.Contains("trainyard_1"))
                    {
                        mon.name = msg("trainyard");
                        mon.icon = "special";
                        staticMarkers.Add(mon);
                        continue;
                    }

                    if (monument.name.Contains("water_treatment_plant_1"))
                    {
                        mon.name = msg("waterplant");
                        mon.icon = "special";
                        staticMarkers.Add(mon);
                        continue;
                    }

                    if (monument.name.Contains("warehouse"))
                    {
                        mon.name = msg("warehouse");
                        mon.icon = "warehouse";
                        staticMarkers.Add(mon);
                        continue;
                    }

                    if (monument.name.Contains("satellite_dish"))
                    {

                        mon.name = msg("dish");
                        mon.icon = "dish";
                        staticMarkers.Add(mon);
                        continue;
                    }

                    if (monument.name.Contains("sphere_tank"))
                    {
                        mon.name = msg("spheretank");
                        mon.icon = "spheretank";
                        staticMarkers.Add(mon);
                        continue;
                    }

                    if (monument.name.Contains("radtown_small_3"))
                    {
                        mon.name = msg("radtown");
                        mon.icon = "radtown";
                        staticMarkers.Add(mon);
                        continue;
                    }
                }
            }                      
        }
        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        #endregion

        #region Helpers
        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= 'Ð' && c <= 'Ð¯') || (c >= 'Ð°' && c <= 'Ñ') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        static float GetPosition(float pos) => (pos + mapSize / 2f) / mapSize;  
        static int GetDirection(float rotation) => (int)((rotation - 5) / 10 + 0.5) * 10;
        #endregion

        #region API
        void EnableMaps(BasePlayer player)
        {
            var user = GetUser(player);
            if (user != null)
                user.EnableUser();
        }
        void DisableMaps(BasePlayer player)
        {
            var user = GetUser(player);
            if (user != null)
                user.DisableUser();
        }

        #region Markers
        bool AddMarker(float x, float z, string name, string icon = "special", float r = 0)
        {
            if (customMarkers.ContainsKey(name)) return false;
            MapMarker marker = new MapMarker
            {
                icon = icon,
                name = name,
                x = GetPosition(x),
                z = GetPosition(z),
                r = r
            };
            if (r > 0) marker.r = GetDirection(r);

            customMarkers.Add(name, marker);
            if (!string.IsNullOrEmpty(icon) && icon != "special")
                assets.Add(name, $"{dataDirectory}custom{Path.DirectorySeparatorChar}{icon}");            
            SaveMarkers();
            return true;
        }
        void UpdateMarker(float x, float z, string name, string icon = "special", float r = 0)
        {
            if (!customMarkers.ContainsKey(name)) return;
            MapMarker marker = new MapMarker
            {
                icon = icon,
                name = name,
                x = GetPosition(x),
                z = GetPosition(z),
                r = r
            };
            if (r > 0) marker.r = GetDirection(r);
            customMarkers[name] = marker;

            if (!string.IsNullOrEmpty(icon) && icon != "special")
                assets.Add(name, $"{dataDirectory}custom{Path.DirectorySeparatorChar}{icon}");
            SaveMarkers();
        }
        bool RemoveMarker(string name)
        {
            if (!customMarkers.ContainsKey(name)) return false;
            customMarkers.Remove(name);
            SaveMarkers();
            return true;
        }
        #endregion

        #region Friends
        bool AddFriendList(string playerId, string name, List<string> list, bool bypass = false)
        {
            if (!bypass && !configData.FriendOptions.AllowCustomLists) return false;
            var user = GetUserByID(playerId);
            if (user == null) return false;
            if (user.HasFriendList(name))
                return false;

            user.AddFriendList(name, list);
            return true;
        }
        bool RemoveFriendList(string playerId, string name, bool bypass = false)
        {
            if (!bypass && !configData.FriendOptions.AllowCustomLists) return false;
            var user = GetUserByID(playerId);
            if (user == null) return false;
            if (!user.HasFriendList(name))
                return false;

            user.RemoveFriendList(name);
            return true;
        }
        bool UpdateFriendList(string playerId, string name, List<string> list, bool bypass = false)
        {
            if (!bypass && !configData.FriendOptions.AllowCustomLists) return false;
            var user = GetUserByID(playerId);
            if (user == null) return false;
            if (!user.HasFriendList(name))
                return false;

            user.UpdateFriendList(name, list);
            return true;
        }
        bool AddFriend(string playerId, string name, string friendId, bool bypass = false)
        {
            if (!bypass && !configData.FriendOptions.AllowCustomLists) return false;
            var user = GetUserByID(playerId);
            if (user == null) return false;
            if (!user.HasFriendList(name))
                user.AddFriendList(name, new List<string>());
            if (user.HasFriend(name, friendId))
                return true;
            user.AddFriend(name, friendId);
            return true;
        }
        bool RemoveFriend(string playerId, string name, string friendId, bool bypass = false)
        {
            if (!bypass && !configData.FriendOptions.AllowCustomLists) return false;
            var user = GetUserByID(playerId);
            if (user == null) return false;
            if (!user.HasFriendList(name))
                return false;
            if (!user.HasFriend(name, friendId))
                return true;
            user.RemoveFriend(name, friendId);
            return true;
        }
        #endregion

        #endregion

        #region External API  
        void JoinedEvent(BasePlayer player)
        {
            var user = GetUser(player);
            if (user != null)
                user.EnterEvent();
        }
        void LeftEvent(BasePlayer player)
        {
            var user = GetUser(player);
            if (user != null)
                user.ExitEvent();
        }

        #region Friends
        List<string> GetFriends(ulong playerId)
        {
            if (Friends != null)
            {
                if (isRustFriends)
                    return GetRustFriends(playerId);
                return GetUniversalFriends(playerId);
            }
            return new List<string>();
        }
        List<string> GetRustFriends(ulong playerId)
        {
            var list = new List<string>();
            var success = Friends?.Call("IsFriendOfS", playerId.ToString());
            if (success is string[])
            {
                return (success as string[]).ToList();
            }
            return list;
        }
        List<string> GetUniversalFriends(ulong playerId)
        {
            var success = Friends?.Call("GetFriendsReverse", playerId.ToString());
            if (success is string[])
            {
                return (success as string[]).ToList();
            }
            return new List<string>();
        }
        void OnFriendAdded(object playerId, object friendId)
        {
            AddFriend(friendId.ToString(), "FriendsAPI", playerId.ToString(), true);
        }
        void OnFriendRemoved(object playerId, object friendId)
        {
            RemoveFriend(friendId.ToString(), "FriendsAPI", playerId.ToString(), true);
        }
        #endregion

        #region Clans
        void GetClans()
        {
            if (Clans)
            {
                var allClans = Clans?.Call("GetAllClans");
                if (allClans != null && allClans is JArray)
                {
                    foreach(var clan in (JArray)allClans)
                    {
                        var name = clan.ToString();
                        List<string> members = GetClanMembers(name);
                        if (!clanData.ContainsKey(name))
                            clanData.Add(name, new List<string>());
                        clanData[name] = members;
                    }
                }
            }
        }
        string GetClan(ulong playerId)
        {
            string clanName = Clans?.Call<string>("GetClanOf", playerId);
            if (!string.IsNullOrEmpty(clanName))
            {
                if (!clanData.ContainsKey(clanName))
                    clanData.Add(clanName, GetClanMembers(clanName));
            }
            return clanName;
        }
        List<string> GetClanMembers(string clanTag)
        {
            var newList = new List<string>();
            var clan = instance.Clans?.Call("GetClan", clanTag);
            if (clan != null && clan is JObject)
            {                
                var members = (clan as JObject).GetValue("members");
                if (members != null && members is JArray)
                {
                    foreach (var member in (JArray)members)
                    {
                        newList.Add(member.ToString());
                    }
                }
            }
            return newList;
        }
        void OnClanCreate(string tag)
        {
            if (!clanData.ContainsKey(tag))
                clanData.Add(tag, GetClanMembers(tag));
        }
        void OnClanUpdate(string tag)
        {
            var members = GetClanMembers(tag);
            if (!clanData.ContainsKey(tag))                            
                clanData.Add(tag, members);            
            else
            {
                foreach (var member in clanData[tag])                                    
                    RemoveFriendList(member, "Clans", true); 
                foreach(var member in members)
                    AddFriendList(member, "Clans", members, true);
                clanData[tag] = members;
            }            
        }
        void OnClanDestroy(string tag)
        {            
            if (clanData.ContainsKey(tag))
            {
                foreach(var member in clanData[tag])                                    
                    RemoveFriendList(member, "Clans", true);                
                clanData.Remove(tag);
            }
        }
        #endregion
        #endregion

        #region Config        
        private ConfigData configData;
        class FriendLists
        {
            public bool AllowCustomLists { get; set; }
            public bool UseClans { get; set; }
            public bool UseFriends { get; set; }            
        }
        class MapMarkers
        {
            public bool ShowAllPlayers { get; set; }
            public bool ShowCaves { get; set; }
            public bool ShowDebris { get; set; }
            public bool ShowFriends { get; set; }
            public bool ShowHelicopters { get; set; }
            public bool ShowMarkerNames { get; set; }
            public bool ShowMonuments { get; set; }
            public bool ShowPlanes { get; set; }
            public bool ShowPlayer { get; set; }
            public bool ShowSupplyDrops { get; set; }
        }
        class MapOptions
        {
            public bool HideEventPlayers { get; set; }
            public string MapKeybind { get; set; }
            public bool StartOpen { get; set; }
            public bool ShowCompass { get; set; }
            public MapImages MapImage { get; set; }
            public Minimap MinimapOptions { get; set; } 
            public float UpdateSpeed { get; set; }           
        }
        class MapImages
        {
            public string APIKey { get; set; }
            public bool CustomMap_Use { get; set; }
            public string CustomMap_Filename { get; set; }
        }
        class Minimap
        {            
            public bool UseMinimap { get; set; }
            public float HorizontalScale { get; set; }
            public float VerticalScale { get; set; }
            public bool OnLeftSide { get; set; }
            public float OffsetSide { get; set; }
            public float OffsetTop { get; set; }
            public ComplexMap ComplexOptions { get; set; }
        }
        class ComplexMap
        {
            public bool UseComplexMap { get; set; }
            public bool ForceMapZoom { get; set; }
            public int ForcedZoomLevel { get; set; }
        }
        class SpamOptions
        {
            public int TimeBetweenAttempts { get; set; }
            public int WarningAttempts { get; set; }
            public int DisableAttempts { get; set; }
            public int DisableSeconds { get; set; }
            public bool Enabled { get; set; }
        }
        class ConfigData
        {
            public FriendLists FriendOptions { get; set; }
            public MapMarkers MapMarkers { get; set; }
            public MapOptions MapOptions { get; set; }
            public SpamOptions SpamOptions { get; set; }
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
                FriendOptions = new FriendLists
                {
                    AllowCustomLists = true,
                    UseClans = true,
                    UseFriends = true,
                },
                MapMarkers = new MapMarkers
                {
                    ShowAllPlayers = false,
                    ShowCaves = false,
                    ShowDebris = false,
                    ShowFriends = true,
                    ShowHelicopters = true,
                    ShowMarkerNames = true,
                    ShowMonuments = true,
                    ShowPlanes = true,
                    ShowPlayer = true,
                    ShowSupplyDrops = true
                },
                MapOptions = new MapOptions
                {                    
                    HideEventPlayers = true,
                    MapKeybind = "m",
                    ShowCompass = true,
                    StartOpen = true,
                    MapImage = new MapImages
                    {
                        APIKey = "",
                        CustomMap_Filename = "",
                        CustomMap_Use = false
                    },
                    MinimapOptions = new Minimap
                    {
                        ComplexOptions = new ComplexMap
                        {
                            ForcedZoomLevel = 1,
                            ForceMapZoom = false,
                            UseComplexMap = true
                        },
                        HorizontalScale = 1.0f,
                        VerticalScale = 1.0f,
                        OnLeftSide = true,
                        OffsetSide = 0,
                        OffsetTop = 0,
                        UseMinimap = true
                    },
                    UpdateSpeed = 1f
                },
                SpamOptions = new SpamOptions
                {
                    DisableAttempts = 10,
                    DisableSeconds = 120,
                    Enabled = true,
                    TimeBetweenAttempts = 3,
                    WarningAttempts = 5
                }
                             
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData()
        {
            imageData.WriteObject(storedImages);            
        }
        void SaveMarkers()
        {
            markerData.WriteObject(storedMarkers);
            SaveData();
        }
        void LoadData()
        {
            try
            {
                storedImages = imageData.ReadObject<ImageStore>();
                
            }
            catch
            {
                storedImages = new ImageStore();
            }
            try
            {
                storedMarkers = markerData.ReadObject<MarkerData>();
                customMarkers = storedMarkers.data;
            }
            catch
            {
                storedMarkers = new MarkerData();
            }
        }
        class ImageStore
        {
            public Dictionary<string, uint> data = new Dictionary<string, uint>();
        }
        class MarkerData
        {
            public Dictionary<string, MapMarker> data = new Dictionary<string, MapMarker>();
        }
        #endregion

        #region Image Storage
        private string GetImage(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (storedImages.data.ContainsKey(name))
                return storedImages.data[name].ToString();            
            else return null;
        }
        class ImageAssets : MonoBehaviour
        {
            LustyMap filehandler;
            private Queue<QueueItem> QueueList = new Queue<QueueItem>();
            private MemoryStream stream = new MemoryStream();
            const int MaxActiveLoads = 3;            
            static byte activeLoads;            

            private void Awake() => filehandler = (LustyMap)Interface.Oxide.RootPluginManager.GetPlugin(nameof(LustyMap));                        
            private void OnDestroy()
            {
                QueueList.Clear();
                filehandler = null;
            }
            public void Add(string name, string url)
            {
                QueueList.Enqueue(new QueueItem(url, name));
                if (activeLoads < MaxActiveLoads) Next();
            }
            void Next()
            {
                if (QueueList.Count <= 0) return;
                activeLoads++;
                StartCoroutine(WaitForRequest(QueueList.Dequeue()));
            }
            private void ClearStream()
            {
                stream.Position = 0;
                stream.SetLength(0);
            }

            IEnumerator WaitForRequest(QueueItem info)
            {
                using (var www = new WWW(info.url))
                {
                    yield return www;
                    if (filehandler == null) yield break;
                    if (www.error != null)
                    {
                        print(string.Format("Image loading fail! Error: {0}", www.error));
                    }
                    else
                    {
                        ClearStream();
                        stream.Write(www.bytes, 0, www.bytes.Length);
                        uint textureID = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue);                        
                        ClearStream();
                        if (!filehandler.storedImages.data.ContainsKey(info.name))
                            filehandler.storedImages.data.Add(info.name, textureID);
                        else
                            filehandler.storedImages.data[info.name] = textureID;
                    }
                    activeLoads--;
                    if (QueueList.Count > 0) Next();
                    else if (QueueList.Count <= 0) filehandler.SaveData();
                }
            }
            internal class QueueItem
            {
                public string url;
                public string name;
                public QueueItem(string url, string name)
                {
                    this.url = url;
                    this.name = name;
                }
            }
        }
        void ValidateImages()
        {
            PrintWarning("Validating imagery");
            if (isNewSave || storedImages.data.Count < 194)
                LoadImages();
            else
            {
                PrintWarning("Images and icons found in server storage");
            }
            if (string.IsNullOrEmpty(GetImage("mapimage")))
            {
                LoadMapImage();
            }
            else GenerateMaps(true, MapSettings.minimap, MapSettings.complexmap);
        }
        private void LoadImages()
        {
            PrintWarning("Icon images have not been found. Uploading images to file storage");

            string[] files = new string[] { "self", "friend", "other", "heli", "plane" };
            string path = $"{dataDirectory}icons{Path.DirectorySeparatorChar}";

            foreach (string file in files)
            {                
                string ext = ".png";
                for (int i = 0; i <= 360; i = i + 10)                
                    assets.Add(file + i, path + file + i + ext);                
            }
            
            assets.Add("lighthouse", $"{path}lighthouse.png");
            assets.Add("radtown", $"{path}radtown.png");
            assets.Add("cave", $"{path}cave.png");
            assets.Add("warehouse", $"{path}warehouse.png");
            assets.Add("dish", $"{path}dish.png");
            assets.Add("spheretank", $"{path}spheretank.png");
            assets.Add("special", $"{path}special.png");
            assets.Add("supply", $"{path}supply.png");
            assets.Add("debris", $"{path}debris.png");

            foreach (var image in customMarkers)
            {
                if (image.Value.icon == "special")                    
                    assets.Add(image.Value.name, dataDirectory + "icons" + Path.DirectorySeparatorChar + image.Value.icon + ".png");
                else assets.Add(image.Value.name, dataDirectory + "custom" + Path.DirectorySeparatorChar + image.Value.icon);
            }          
        } 
        private void LoadMapImage()
        {
            if (configData.MapOptions.MapImage.CustomMap_Use)
            {
                PrintWarning("Downloading map image to file storage. Please wait!"); 
                assets.Add("mapimage", dataDirectory + configData.MapOptions.MapImage.CustomMap_Filename);
                if (MapSettings.complexmap)
                {
                    PrintWarning("Attempting to split and store the complex mini-map. This may take a few moments! Failure to wait for this process to finish WILL result in error!");
                    AttemptSplit();
                }
                else GenerateMaps(true, MapSettings.minimap, false);
            }
            else DownloadMapImage();
        }       
        #endregion

        #region Map Generation - Credits to Calytic, Nogrod, kraz and beancan.io for the awesome looking map images and API to make this possible!
        void DownloadMapImage()
        {
            if (string.IsNullOrEmpty(configData.MapOptions.MapImage.APIKey))
            {
                PrintError("You must supply a valid API key to utilize the auto-download feature!\nVisit 'beancan.io' and register your server to retrieve your API key!");
                activated = false;
                return;
            }
            PrintWarning("Attempting to contact beancan.io to download your map image!");
            GetQueueID();            
        }
        void GetQueueID()
        {
            var url = $"http://beancan.io/map-queue-generate?level={level}&seed={mapSeed}&size={mapSize}&key={configData.MapOptions.MapImage.APIKey}";
            webrequest.EnqueueGet(url, (code, response) =>
            {
                if (string.IsNullOrEmpty(response))
                {
                    if (code == 403)
                        PrintError($"Error: {code} - Invalid API key. Unable to download map image");
                    else PrintWarning($"Error: {code} - Couldn't get an answer from beancan.io. Unable to download map image. Please try again in a few minutes");                    
                }
                else CheckAvailability(response);
            }, this);
        }
        void CheckAvailability(string queueId)
        {
            webrequest.EnqueueGet($"http://beancan.io/map-queue/{queueId}", (code, response) =>
            {
                if (string.IsNullOrEmpty(response))
                {
                    PrintWarning($"Error: {code} - Couldn't get an answer from beancan.io");
                }
                else ProcessResponse(queueId, response);
            }, this);
        }
        void ProcessResponse(string queueId, string response)
        {
            switch (response)
            {
                case "-1":
                    PrintWarning("Your map is still in the queue to be generated. Checking again in 10 seconds");
                    break;
                case "0":
                    PrintWarning("Your map is still being generated. Checking again in 10 seconds");
                    break;
                case "1":
                    GetMapURL(queueId);
                    return;
                default:
                    PrintWarning($"Error retrieving map: Invalid response from beancan.io : Response code {response}");
                    return;
            }
            timer.Once(10, () => CheckAvailability(queueId));
        }
        void GetMapURL(string queueId)
        {
            var url = $"http://beancan.io/map-queue-image/{queueId}";
            webrequest.EnqueueGet(url, (code, response) =>
            {
                if (string.IsNullOrEmpty(response))
                {
                    PrintWarning($"Error: {code} - Couldn't get an answer from beancan.io");
                }
                else DownloadMap(response);
            }, this);
        }
        void DownloadMap(string url)
        {
            PrintWarning("Map generation successful! Downloading map image to file storage. Please wait!");
            assets.Add("mapimage", url);
            if (MapSettings.complexmap)
            {
                PrintWarning("Attempting to split and store the complex mini-map. This may take a while, please wait!");
                AttemptSplit();
            }
            else GenerateMaps(true, MapSettings.minimap, false);            
        }
        #endregion

        #region Map Splitter
        void AttemptSplit(int attempts = 0)
        {
            if (attempts == 5)
            {
                PrintError("The plugin has timed out trying to find the map image to split! Complex map has been disabled");
                MapSettings.complexmap = false;                
                return;
            }
            if (storedImages.data.ContainsKey("mapimage"))
            {
                var imageId = storedImages.data["mapimage"];
                if (mapSplitter.SplitMap(imageId))
                {
                    PrintWarning("Map split was successful!");
                    GenerateMaps(true, MapSettings.minimap, true);
                }
                else
                {
                    MapSettings.complexmap = false;
                    GenerateMaps(true, MapSettings.minimap, MapSettings.complexmap);
                }
            }
            else
            {
                PrintWarning($"Map image not found in file store. Waiting for 10 seconds and trying again (Attempt: {attempts + 1} / 5)");
                timer.Once(10, () => AttemptSplit(attempts + 1));
            }
        }        
        class MapSplitter : MonoBehaviour
        {
            LustyMap filehandler;
            private Queue<QueueItem> QueueList = new Queue<QueueItem>();
            private MemoryStream stream = new MemoryStream();
            const int MaxActiveLoads = 3;
            static byte activeLoads;

            internal class QueueItem
            {
                public byte[] bmp;
                public string name;
                public QueueItem(byte[] bmp, string name)
                {
                    this.bmp = bmp;
                    this.name = name;
                }
            }
            private void Awake() => filehandler = (LustyMap)Interface.Oxide.RootPluginManager.GetPlugin(nameof(LustyMap));
            private void OnDestroy()
            {
                QueueList.Clear();
                filehandler = null;
            }

            public bool SplitMap(uint imageId)
            {
                System.Drawing.Image img = ImageFromStorage(imageId);
                if (img == null)
                {
                    instance.PrintError("There was a error retrieving the map image from file storage. This may be caused by slow processing of the images being uploaded to your server. Please wait a few minutes and try again");
                    return false;
                }
                foreach (var amount in new List<int> { 6, 12, 26 })//, 32 })
                {
                    int width = (int)(img.Width / (double)amount);
                    int height = (int)(img.Height / (double)amount);

                    int rowCount = 0;
                    int colCount = 0;
                    for (int r = 0; r < amount; r++)
                    {
                        colCount = 0;
                        for (int c = 0; c < amount; c++)
                        {
                            var column = colCount;
                            var row = rowCount;
                            if (column < 1) column = 1;
                            if (column > amount - 2) column = amount - 2;
                            if (row < 1) row = 1;
                            if (row > amount - 2) row = amount - 2;
                            
                            Bitmap cutPiece = new Bitmap(width * 3, height * 3);
                            System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(cutPiece);
                            graphic.DrawImage(img, new Rectangle(0, 0, width * 3, height * 3), new Rectangle((width * column) - width, (height * row) - height, width * 3, height * 3), GraphicsUnit.Pixel);
                            graphic.Dispose();
                            colCount++;

                            StoreImagePiece(cutPiece, $"map-{amount}-{r}-{c}");                            
                        }
                        rowCount++;
                    }
                }                
                return true;
            }            
            private System.Drawing.Image ImageFromStorage(uint imageId)
            {
                byte[] imageData = FileStorage.server.Get(imageId, FileStorage.Type.png, 0U);
                System.Drawing.Image img = null;
                try
                {
                    img = (System.Drawing.Bitmap)((new System.Drawing.ImageConverter()).ConvertFrom(imageData));
                }
                catch (Exception ex)
                {
                    instance.PrintError($"Error whilst retrieving the map image from file storage: {ex.Message}\nIf you are running linux you must install LibGDIPlus using the following line: \"sudo apt install libgdiplus\", then restart your system for the changes to take affect");
                }
                return img;
            }         
            internal void StoreImagePiece(System.Drawing.Bitmap bmp, string name)
            {
                System.Drawing.ImageConverter converter = new System.Drawing.ImageConverter();
                byte[] array = (byte[])converter.ConvertTo(bmp, typeof(byte[]));
                Add(name, array);
            }

            internal void Add(string name, byte[] bmp)
            {
                QueueList.Enqueue(new QueueItem(bmp, name));
                if (activeLoads < MaxActiveLoads) Next();
            }
            internal void Next()
            {
                if (QueueList.Count <= 0) return;
                activeLoads++;
                StartCoroutine(StoreNextSplit(QueueList.Dequeue()));
            }
            internal void ClearStream()
            {
                stream.Position = 0;
                stream.SetLength(0);
            }            
            
            IEnumerator StoreNextSplit(QueueItem info)
            {
                if (filehandler == null) yield break;
                if (info.bmp == null)
                {
                    instance.PrintError($"Error whilst storing map piece to file storage : {info.name}");                    
                }
                else
                {
                    ClearStream();
                    stream.Write(info.bmp, 0, info.bmp.Length);
                    uint textureID = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue);
                    ClearStream();

                    if (!filehandler.storedImages.data.ContainsKey(info.name))
                        filehandler.storedImages.data.Add(info.name, textureID);
                    else filehandler.storedImages.data[info.name] = textureID;
                }
                activeLoads--;
                if (QueueList.Count > 0) Next();
                else if (QueueList.Count <= 0) filehandler.SaveData();

            }            
        }
        #endregion

        #region Localization
        string msg(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"cpsN", "North" },
            {"cpsNE", "North-East" },
            {"cpsE", "East" },
            {"cpsSE", "South-East" },
            {"cpsS", "South" },
            {"cpsSW", "South-West" },
            {"cpsW", "West" },
            {"cpsNW", "North-West" },
            {"Plane", "Plane" },
            {"Supply Drop", "Supply Drop" },
            {"Helicopter", "Helicopter" },
            {"Debris", "Debris" },
            {"lighthouse", "lighthouse" },
            {"radtown", "radtown" },
            {"spheretank", "spheretank" },
            {"dish","dish" },
            {"warehouse","warehouse" },
            {"waterplant", "waterplant" },
            {"trainyard", "trainyard" },
            {"airfield", "airfield" },
            {"militarytunnel", "militarytunnel" },
            {"powerplant", "powerplant" },
            {"cave", "cave" },
            {"spamWarning", "Please do not spam the map. If you continue to do so your map will be temporarily disabled" },
            {"spamDisable", "Your map has been disabled for {0} seconds" },
            {"spamEnable", "Your map has been enabled" }
        };
        #endregion
    }
}
