using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.IO;
using Rust;

namespace Oxide.Plugins
{
    [Info("NukeWeapons", "k1lly0u", "0.1.5", ResourceId = 2044)]
    class NukeWeapons : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin LustyMap;

        NukeData nukeData;
        ItemNames itemNames;
        private DynamicConfigFile data;        
        private DynamicConfigFile Item_Names;

        static GameObject webObject;
        static UnityWeb uWeb;
        static MethodInfo getFileData = typeof(FileStorage).GetMethod("StorageGet", (BindingFlags.Instance | BindingFlags.NonPublic));

        private static readonly int playerLayer = LayerMask.GetMask("Player (Server)");
        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic))?.GetValue(null);

        private List<ZoneList> RadiationZones = new List<ZoneList>();

        private Dictionary<ulong, NukeType> activeUsers = new Dictionary<ulong, NukeType>();
        private Dictionary<ulong, Dictionary<NukeType, int>> cachedAmmo = new Dictionary<ulong, Dictionary<NukeType, int>>();
        private Dictionary<ulong, Dictionary<NukeType, double>> craftingTimers = new Dictionary<ulong, Dictionary<NukeType, double>>();

        private List<Timer> nwTimers = new List<Timer>();

        private Dictionary<string, ItemDefinition> ItemDefs;
        private Dictionary<string, string> DisplayNames = new Dictionary<string, string>();

        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("NukeWeapons/nukeweapon_data");
            Item_Names = Interface.Oxide.DataFileSystem.GetFile("NukeWeapons/itemnames");
            Interface.Oxide.DataFileSystem.SaveDatafile("NukeWeapons/Icons/foldercreator");
            lang.RegisterMessages(Messages, this);
            webObject = new GameObject("WebObject");
            uWeb = webObject.AddComponent<UnityWeb>();
            InitializePlugin();
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            ItemDefs = ItemManager.itemList.ToDictionary(i => i.shortname);
            if (itemNames.DisplayNames == null || itemNames.DisplayNames.Count < 1)
            {
                foreach (var item in ItemDefs)
                {
                    if (!DisplayNames.ContainsKey(item.Key))
                        DisplayNames.Add(item.Key, item.Value.displayName.translated);
                }
                SaveDisplayNames();
            }
            else DisplayNames = itemNames.DisplayNames;
            
            FindAllMines();
        }
        void Unload()
        {
            for (int i = 0; i < RadiationZones.Count; i++)
            {
                RadiationZones[i].time.Destroy();
                UnityEngine.Object.Destroy(RadiationZones[i].zone);
            }
            RadiationZones.Clear();
            foreach(var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, UIMain);
                CuiHelper.DestroyUi(player, UIPanel);
                DestroyIconUI(player);
                DestroyCraftUI(player);
            }
            foreach (var time in nwTimers)
                time.Destroy();
            SaveData();         
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (activeUsers.ContainsKey(player.userID))
                activeUsers.Remove(player.userID);
            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.DestroyUi(player, UIPanel);
            DestroyIconUI(player);
            DestroyCraftUI(player);
        }
        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            if (entity is BasePlayer)
            {
                var player = entity.ToPlayer();
                if (activeUsers.ContainsKey(player.userID))
                    activeUsers.Remove(player.userID);
                CuiHelper.DestroyUi(player, UIMain);
                CuiHelper.DestroyUi(player, UIPanel);
                DestroyIconUI(player);
                //DestroyCraftUI(player);
            }            
        }
        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (activeUsers.ContainsKey(player.userID) && activeUsers[player.userID] == NukeType.Rocket)
            {
                if (hasUnlimited(player) || HasAmmo(player.userID, NukeType.Rocket))
                {
                    if (!hasUnlimited(player))
                    {
                        string itemname = "ammo.rocket.basic";
                        switch (entity.ShortPrefabName)
                        {
                            case "calledrocket_hv":
                                itemname = "ammo.rocket.hv";
                                break;
                            case "calledrocket_fire":
                                itemname = "ammo.rocket.fire";
                                break;
                            default:
                                break;
                        }
                        player.inventory.containerMain.AddItem(ItemDefs[itemname], 1);
                        cachedAmmo[player.userID][NukeType.Rocket]--;
                    }
                    entity.gameObject.AddComponent<Nuke>().InitializeComponent(this, NukeType.Rocket, configData.Rockets.RadiationProperties);
                }
                else
                {
                    activeUsers.Remove(player.userID);
                    SendMSG(player, $"{MSG("OOA", player.UserIDString)} {MSG("Rockets", player.UserIDString)}");
                }
                CreateAmmoIcons(player);
            }
        }
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is Landmine)
            {
                var mine = entity as Landmine;
                if (activeUsers.ContainsKey(mine.OwnerID) && activeUsers[mine.OwnerID] == NukeType.Mine)
                {
                    var player = BasePlayer.FindByID(mine.OwnerID);
                    if (player != null)
                    {
                        if (hasUnlimited(player) || HasAmmo(player.userID, NukeType.Mine))
                        {
                            if (!hasUnlimited(player))
                            {
                                player.inventory.containerMain.AddItem(ItemDefs["trap.landmine"], 1);
                                cachedAmmo[player.userID][NukeType.Mine]--;
                            }
                            mine.gameObject.AddComponent<Nuke>().InitializeComponent(this, NukeType.Mine, configData.Mines.RadiationProperties);
                            nukeData.Mines.Add(entity.net.ID);
                        }
                        else
                        {
                            activeUsers.Remove(player.userID);
                            SendMSG(player, $"{MSG("OOA", player.UserIDString)} {MSG("Mines", player.UserIDString)}");
                        }
                        CreateAmmoIcons(player);
                    }                 
                }
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is Landmine)
            {                
                if (nukeData.Mines.Contains(entity.net.ID))
                {
                    nukeData.Mines.Remove(entity.net.ID);
                }
            }
        }
        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (activeUsers.ContainsKey(attacker.userID) && activeUsers[attacker.userID] == NukeType.Bullet)
            {
                if (!string.IsNullOrEmpty(info?.Weapon?.GetEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine?.ammoType?.shortname))
                {
                    var ammo = info?.Weapon?.GetEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine?.ammoType?.shortname;
                    if (!string.IsNullOrEmpty(ammo) && ammo.Contains("ammo.rifle"))
                    {
                        var hitPos = info.HitPositionWorld;
                        if (hitPos != null)
                        {
                            var radVar = configData.Bullets.RadiationProperties;
                            if (hasUnlimited(attacker) || HasAmmo(attacker.userID, NukeType.Bullet))
                            {
                                if (!hasUnlimited(attacker))
                                {
                                    attacker.inventory.containerMain.AddItem(ItemDefs[ammo], 1);
                                    cachedAmmo[attacker.userID][NukeType.Bullet]--;
                                }
                                InitializeZone(hitPos, radVar.Intensity, radVar.Duration, radVar.Radius, false);
                            }
                            else
                            {
                                activeUsers.Remove(attacker.userID);
                                SendMSG(attacker, $"{MSG("OOA", attacker.UserIDString)} {MSG("Bullets", attacker.UserIDString)}");
                            }
                            CreateAmmoIcons(attacker);
                        }
                    }
                }                
            }
        }
        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (entity.ShortPrefabName.Contains("explosive.timed"))
            {
                if (activeUsers.ContainsKey(player.userID) && activeUsers[player.userID] == NukeType.Explosive)
                {
                    if (hasUnlimited(player) || HasAmmo(player.userID, NukeType.Explosive))
                    {
                        if (!hasUnlimited(player))
                        {
                            player.inventory.containerMain.AddItem(ItemDefs["explosive.timed"], 1);
                            cachedAmmo[player.userID][NukeType.Explosive]--;
                        }
                        entity.gameObject.AddComponent<Nuke>().InitializeComponent(this, NukeType.Explosive, configData.Explosives.RadiationProperties);                        
                    }
                    else
                    {
                        activeUsers.Remove(player.userID);
                        SendMSG(player, $"{MSG("OOA", player.UserIDString)} {MSG("Explosives", player.UserIDString)}");
                    }
                    CreateAmmoIcons(player);
                }
            }
            if (entity.ShortPrefabName.Contains("grenade.f1"))
            {
                if (activeUsers.ContainsKey(player.userID) && activeUsers[player.userID] == NukeType.Grenade)
                {
                    if (hasUnlimited(player) || HasAmmo(player.userID, NukeType.Grenade))
                    {
                        if (!hasUnlimited(player))
                        {
                            player.inventory.containerMain.AddItem(ItemDefs["grenade.f1"], 1);
                            cachedAmmo[player.userID][NukeType.Grenade]--;
                        }
                        entity.gameObject.AddComponent<Nuke>().InitializeComponent(this, NukeType.Explosive, configData.Grenades.RadiationProperties);                        
                    }
                    else
                    {
                        activeUsers.Remove(player.userID);
                        SendMSG(player, $"{MSG("OOA", player.UserIDString)} {MSG("Grenades", player.UserIDString)}");
                    }
                    CreateAmmoIcons(player);
                }
            }
        }
        #endregion

        #region Helpers
        private bool HasEnoughRes(BasePlayer player, int itemid, int amount) => player.inventory.GetAmount(itemid) >= amount;
        private void TakeResources(BasePlayer player, int itemid, int amount) => player.inventory.Take(null, itemid, amount);
        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        private bool IsType(BasePlayer player, NukeType type) => activeUsers.ContainsKey(player.userID) && activeUsers[player.userID] == type;
        #endregion

        #region Functions
        private void FindAllMines()
        {
            var mineList = new Dictionary<uint, Landmine>();
            var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var gobject in allobjects)
            {
                if (gobject.GetComponent<Landmine>())
                {
                    var mine = gobject.GetComponent<Landmine>();
                    if (!mineList.ContainsKey(mine.net.ID))
                        mineList.Add(mine.net.ID, mine);
                }
            }
            foreach (var entry in nukeData.Mines)
            {                
                if (mineList.ContainsKey(entry))
                {
                    mineList[entry].gameObject.AddComponent<Nuke>().InitializeComponent(this, NukeType.Mine, configData.Mines.RadiationProperties);
                }                
            }            
        }
        private bool CanCraft(BasePlayer player, NukeType type)
        {
            var ingredients = GetCraftingComponents(type);
            foreach (var item in ingredients)
            {
                if (HasEnoughRes(player, ItemDefs[item.Key].itemid, item.Value))
                    continue;
                else return false;
            }
            return true;
        }
        private bool AlreadyCrafting(BasePlayer player, NukeType type)
        {
            if (craftingTimers.ContainsKey(player.userID))
            {
                if (craftingTimers[player.userID].ContainsKey(type))
                {
                    if (craftingTimers[player.userID][type] > GrabCurrentTime())
                        return true;
                }
            }
            return false;
        }
        private string CraftTimeClock(BasePlayer player, NukeType type)
        {
            if (player == null) return null;
            TimeSpan dateDifference = TimeSpan.FromSeconds(craftingTimers[player.userID][type] - GrabCurrentTime());            
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            return string.Format("{0:00}:{1:00}", mins, secs);
        }
        private void StartCrafting(BasePlayer player, NukeType type)
        {
            var config = GetConfigFromType(type);
            var ingredients = GetCraftingComponents(type);
            foreach (var ing in ingredients)            
                TakeResources(player, ItemDefs[ing.Key].itemid, ing.Value);

            bool finished = FinishedCrafting(player);
            craftingTimers[player.userID][type] = GrabCurrentTime() + config.CraftTime;            
            CraftingElement(player, type);
            if (finished)
                CreateCraftTimer(player);
        }
        private void FinishCraftingItems(BasePlayer player, NukeType type)
        {
             var config = GetConfigFromType(type);  
                     
            cachedAmmo[player.userID][type] += config.CraftAmount;

            if (activeUsers.ContainsKey(player.userID))
                CreateAmmoIcons(player);
        }
        private bool FinishedCrafting(BasePlayer player)
        {
            if (!craftingTimers.ContainsKey(player.userID))
            {
                CheckPlayerEntry(player);
                return true;
            }
            bool finished = true;
            foreach (var craft in craftingTimers[player.userID])
            {
                if (craft.Value != -1)
                {
                    finished = false;
                    break;
                }
            }
            return finished;
        }
        #endregion

        #region External Calls        
        private void CloseMap(BasePlayer player)
        {
            if (LustyMap)
            {
                LustyMap.Call("DisableMaps", player);
            }
        }
        private void OpenMap(BasePlayer player)
        {
            if (LustyMap)
            {
                LustyMap.Call("EnableMaps", player);
            }
        }        
        #endregion

        #region UI Creation
        class NWUI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false, string parent = "Overlay")
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
                panel);
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0f)
            {
                
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0f)
            {
               
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = fadein },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
            static public void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }            
            public static string CreateTextOverlay(ref CuiElementContainer container, string panelName, string textcolor, string text, int size, string distance, string olcolor, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                string name = CuiHelper.GetGuid();
                container.Add(new CuiElement
                {
                    Name = name,
                    Parent = panelName,
                    Components =
                        {
                            new CuiTextComponent { Color = textcolor, Text = text, FontSize = size, Align = align},
                            new CuiOutlineComponent { Distance = distance, Color = olcolor },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = aMin,
                                AnchorMax = aMax
                            }
                        }
                });
                return name;
            }
        }

        #region Colors
        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"dark", "0.1 0.1 0.1 0.98" },
            {"light", "0.7 0.7 0.7 0.3" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttonopen", "0.2 0.8 0.2 0.9" },
            {"buttoncompleted", "0 0.5 0.1 0.9" },
            {"buttonred", "0.85 0 0.35 0.9" },
            {"buttongrey", "0.8 0.8 0.8 0.9" },
            {"grey8", "0.8 0.8 0.8 1.0" }
        };
        #endregion
        #endregion

        #region NW UI
        static string UIMain = "NWUIMain";
        static string UIPanel = "NWUIPanel";
        static string UIEntry = "NWUIEntry";
        static string UIIcon = "NWUIIcon";
        
        private void OpenCraftingMenu(BasePlayer player)
        {
            CloseMap(player);
            var Selector = NWUI.CreateElementContainer(UIMain, UIColors["dark"], "0 0.92", "1 1");
            NWUI.CreatePanel(ref Selector, UIMain, UIColors["light"], "0.01 0.05", "0.99 0.95", true);
            NWUI.CreateLabel(ref Selector, UIMain, "", $"{configData.Options.MSG_MainColor}{Title}</color>", 30, "0.05 0", "0.2 1");

            int number = 0;
            if (configData.Bullets.Enabled && canBullet(player)) { CreateMenuButton(ref Selector, UIMain, MSG("Bullets", player.UserIDString), "NWUI_ChangeElement bullets", number); number++; }
            if (configData.Explosives.Enabled && canExplosive(player)) { CreateMenuButton(ref Selector, UIMain, MSG("Explosives", player.UserIDString), "NWUI_ChangeElement explosives", number); number++; }
            if (configData.Grenades.Enabled && canGrenade(player)) { CreateMenuButton(ref Selector, UIMain, MSG("Grenades", player.UserIDString), "NWUI_ChangeElement grenades", number); number++; }
            if (configData.Mines.Enabled && canMine(player)) { CreateMenuButton(ref Selector, UIMain, MSG("Mines", player.UserIDString), "NWUI_ChangeElement mines", number); number++; }
            if (configData.Rockets.Enabled && canRocket(player)) { CreateMenuButton(ref Selector, UIMain, MSG("Rockets", player.UserIDString), "NWUI_ChangeElement rockets", number); number++; }
            CreateMenuButton(ref Selector, UIMain, MSG("Close", player.UserIDString), "NWUI_DestroyAll", number);
            CuiHelper.AddUi(player, Selector);
        }
        private void CraftingElement(BasePlayer player, NukeType type)
        {            
            var Main = NWUI.CreateElementContainer(UIPanel, UIColors["dark"], "0 0", "1 0.92");
            NWUI.CreatePanel(ref Main, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            if (nukeData.ImageIDs.ContainsKey("Background"))
            NWUI.LoadImage(ref Main, UIPanel, nukeData.ImageIDs["Background"].ToString(), "0.01 0.02", "0.99 0.98");         

            NWUI.CreateLabel(ref Main, UIPanel, "", $"{configData.Options.MSG_MainColor}{MSG("Required Ingredients", player.UserIDString)}</color>", 20, "0.1 0.85", "0.55 0.95");
            NWUI.CreateLabel(ref Main, UIPanel, "", MSG("Item Name", player.UserIDString), 16, "0.1 0.75", "0.3 0.85", TextAnchor.MiddleLeft);
            NWUI.CreateLabel(ref Main, UIPanel, "", MSG("Required Amount", player.UserIDString), 16, "0.3 0.75", "0.42 0.85");
            NWUI.CreateLabel(ref Main, UIPanel, "", MSG("Your Supply", player.UserIDString), 16, "0.42 0.75", "0.54 0.85");

            
            var ingredients = GetCraftingComponents(type);
            int i = 0;
            foreach(var item in ingredients)
            {
                var itemInfo = ItemDefs[item.Key];
                var plyrAmount = player.inventory.GetAmount(itemInfo.itemid);                
                CreateIngredientEntry(ref Main, UIPanel, DisplayNames[itemInfo.shortname], item.Value, plyrAmount, i);
                i++;
            }
            var config = GetConfigFromType(type);
            string command = null;            
            string text = $"{MSG("Craft", player.UserIDString)} {config.CraftAmount}x";
            if (CanCraft(player, type)) command = $"NWUI_Craft {type.ToString()}";
            if (cachedAmmo[player.userID][type] >= config.MaxAllowed)
            {
                text = MSG("Limit Reached", player.UserIDString);
                command = null;
            }
            if (AlreadyCrafting(player, type))
            {
                text = MSG("Crafting...", player.UserIDString);
                command = null;                
            }
            if (hasUnlimited(player))
            {
                text = MSG("Unlimited", player.UserIDString);
                command = null;
            }

            NWUI.CreateLabel(ref Main, UIPanel, "", $"{configData.Options.MSG_MainColor}{MSG("Inventory Amount", player.UserIDString)}</color>", 20, "0.6 0.85", "0.9 0.95");
            if (hasUnlimited(player))
                NWUI.CreateLabel(ref Main, UIPanel, "", $"~ / {config.MaxAllowed}", 16, "0.6 0.75", "0.9 0.85");
            else NWUI.CreateLabel(ref Main, UIPanel, "", $"{cachedAmmo[player.userID][type]} / {config.MaxAllowed}", 16, "0.6 0.75", "0.9 0.85");
            NWUI.CreateButton(ref Main, UIPanel, UIColors["buttonbg"], text, 16, $"0.6 0.65", $"0.74 0.72", command);
            if (cachedAmmo[player.userID][type] > 0 || hasUnlimited(player))
            {
                if (IsType(player, type))
                    NWUI.CreateButton(ref Main, UIPanel, UIColors["buttonbg"], MSG("Disarm", player.UserIDString), 16, $"0.76 0.65", $"0.9 0.72", $"NWUI_DeactivateMenu {type.ToString()}");
                else NWUI.CreateButton(ref Main, UIPanel, UIColors["buttonbg"], MSG("Arm", player.UserIDString), 16, $"0.76 0.65", $"0.9 0.72", $"NWUI_Activate {type.ToString()}");
            }
            CuiHelper.DestroyUi(player, UIPanel);
            CuiHelper.AddUi(player, Main);
        }
        private void CreateCraftTimer(BasePlayer player)
        {               
            var Main = NWUI.CreateElementContainer(UIEntry, "0 0 0 0", "0.2 0.11", "0.8 0.15");
            var CraftingMessage = "";
            var FinishedTypes = new List<NukeType>();

            foreach(var craft in craftingTimers[player.userID])
            {
                if (craft.Value == -1)
                    continue;                
                else if (craft.Value <= GrabCurrentTime())                
                    FinishedTypes.Add(craft.Key); 
                else                
                    CraftingMessage += $"{craft.Key.ToString()}: {configData.Options.MSG_MainColor}{CraftTimeClock(player, craft.Key)}</color>     ";                              
            }

            foreach(var type in FinishedTypes)
            {
                craftingTimers[player.userID][type] = -1;
                FinishCraftingItems(player, type);
            }

            if (string.IsNullOrEmpty(CraftingMessage))
            {
                DestroyCraftUI(player);
                return;
            }
            else CraftingMessage = $"{configData.Options.MSG_MainColor}{MSG("Crafting", player.UserIDString)} ::: </color> " + CraftingMessage;

            NWUI.CreateLabel(ref Main, UIEntry, "", CraftingMessage, 16, $"0 0", $"1 1", TextAnchor.MiddleRight, 0f);
            CuiHelper.DestroyUi(player, UIEntry);
            CuiHelper.AddUi(player, Main);
            timer.Once(1, () => CreateCraftTimer(player));
        }
        private void CreateIngredientEntry(ref CuiElementContainer container, string panel, string name, int amountreq, int plyrhas, int number)
        {
            Vector2 position = new Vector2(0.1f, 0.68f);
            Vector2 dimensions = new Vector2(0.4f, 0.06f);
            float offsetY = (0.004f + dimensions.y) * number;
            Vector2 offset = new Vector2(0, offsetY);
            Vector2 posMin = position - offset;
            Vector2 posMax = posMin + dimensions;
            string color;
            if (amountreq > plyrhas)
                color = "<color=red>";
            else color = configData.Options.MSG_MainColor;

            NWUI.CreateLabel(ref container, panel, "", $"{configData.Options.MSG_MainColor}{name}</color>", 16, $"{posMin.x} {posMin.y}", $"{posMin.x + 0.2f} {posMax.y}", TextAnchor.MiddleLeft);
            NWUI.CreateLabel(ref container, panel, "", $"{amountreq}", 16, $"{posMin.x + 0.2f} {posMin.y}", $"{posMin.x + 0.32f} {posMax.y}");
            NWUI.CreateLabel(ref container, panel, "", $"{color}{plyrhas}</color>", 16, $"{posMin.x + 0.32f} {posMin.y}", $"{posMin.x + 0.44f} {posMax.y}");
                       
        }
        private void CreateAmmoIcons(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.DestroyUi(player, UIPanel);

            if (cachedAmmo.ContainsKey(player.userID))
            {
                DestroyIconUI(player);
                int i = 0;
                if (canBullet(player))
                {
                    if (cachedAmmo[player.userID][NukeType.Bullet] > 0 || hasUnlimited(player))
                    {
                        AmmoIcon(player, NukeType.Bullet, i); i++;
                    }
                }
                if (canExplosive(player))
                {
                    if (cachedAmmo[player.userID][NukeType.Explosive] > 0 || hasUnlimited(player))
                    {
                        AmmoIcon(player, NukeType.Explosive, i); i++;
                    }
                }
                if (canGrenade(player))
                {
                    if (cachedAmmo[player.userID][NukeType.Grenade] > 0 || hasUnlimited(player))
                    {
                        AmmoIcon(player, NukeType.Grenade, i); i++;
                    }
                }
                if (canMine(player))
                {
                    if (cachedAmmo[player.userID][NukeType.Mine] > 0 || hasUnlimited(player))
                    {
                        AmmoIcon(player, NukeType.Mine, i); i++;
                    }
                }
                if (canRocket(player))
                {
                    if (cachedAmmo[player.userID][NukeType.Rocket] > 0 || hasUnlimited(player))
                    {
                        AmmoIcon(player, NukeType.Rocket, i); i++;
                    }
                }
                AddButtons(player, i);
            }
        }
        private void AmmoIcon(BasePlayer player, NukeType type, int number)
        {      
            Vector2 position = new Vector2(0.92f, 0.2f);
            Vector2 dimensions = new Vector2(0.07f, 0.12f);
            Vector2 offset = new Vector2(0, (0.01f + dimensions.y) * number);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;

            string panelName = UIIcon + type.ToString();
            
            var Main = NWUI.CreateElementContainer(panelName, "0 0 0 0", $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", false, "Hud");

            var image = nukeData.ImageIDs[$"{type.ToString()}"].ToString();
            if (IsType(player, type))
                image = nukeData.ImageIDs[$"{type.ToString()}Active"].ToString();
            NWUI.LoadImage(ref Main, panelName, image, "0 0", "1 1");

            string amount;
            if (hasUnlimited(player))
                amount = "~";
            else amount = cachedAmmo[player.userID][type].ToString();
            NWUI.CreateTextOverlay(ref Main, panelName, "", $"{amount}", 30, "2 2", "0 0 0 1", "0 0", "1 1", TextAnchor.LowerCenter);

            if (IsType(player, type))
                NWUI.CreateButton(ref Main, panelName, "0 0 0 0", "", 20, "0 0", "1 1", "NWUI_DeactivateButton");
            else NWUI.CreateButton(ref Main, panelName, "0 0 0 0", "", 20, "0 0", "1 1", $"NWUI_Activate {type.ToString()}");
            
            CuiHelper.AddUi(player, Main);
        }  
        
        private void AddButtons(BasePlayer player, int number)
        {
            Vector2 position = new Vector2(0.92f, 0.2f);
            Vector2 dimensions = new Vector2(0.07f, 0.12f);
            Vector2 offset = new Vector2(0, (0.01f + dimensions.y) * number);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            var Main = NWUI.CreateElementContainer(UIIcon, "0 0 0 0", $"{posMin.x} {posMin.y}", $"{posMax.x} {posMin.y + 0.1}", false, "Hud");
            NWUI.CreateButton(ref Main, UIIcon, UIColors["buttonbg"], MSG("Menu",player.UserIDString), 16, "0 0.55", "1 1", "NWUI_OpenMenu");
            NWUI.CreateButton(ref Main, UIIcon, UIColors["buttonbg"], MSG("Deactivate", player.UserIDString), 16, "0 0", "1 0.45", "NWUI_DeactivateIcons");
            CuiHelper.DestroyUi(player, UIIcon);
            CuiHelper.AddUi(player, Main);
        }   
        
        #region UI Functions
        private void CreateMenuButton(ref CuiElementContainer container, string panelName, string buttonname, string command, int number)
        {
            Vector2 dimensions = new Vector2(0.1f, 0.6f);
            Vector2 origin = new Vector2(0.25f, 0.2f);
            Vector2 offset = new Vector2((0.01f + dimensions.x) * number, 0);

            Vector2 posMin = origin + offset;
            Vector2 posMax = posMin + dimensions;

            NWUI.CreateButton(ref container, panelName, UIColors["buttonbg"], buttonname, 16, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", command);
        }                
        #endregion
        #region UI Commands
        [ConsoleCommand("NWUI_Craft")]
        private void cmdNWCraft(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var nukeType = arg.GetString(0);            
            switch (nukeType.ToLower())
            {
                case "bullet":
                    StartCrafting(player, NukeType.Bullet);
                    return;
                case "explosive":
                    StartCrafting(player, NukeType.Explosive);
                    return;
                case "grenade":
                    StartCrafting(player, NukeType.Grenade);
                    return;
                case "mine":
                    StartCrafting(player, NukeType.Mine);
                    return;
                case "rocket":
                    StartCrafting(player, NukeType.Rocket);
                    return;
            }
        }
        [ConsoleCommand("NWUI_DeactivateMenu")]
        private void cmdNWDeActivate(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            activeUsers.Remove(player.userID);
            var nukeType = arg.GetString(0);            
            switch (nukeType.ToLower())
            {
                case "bullet":
                    CraftingElement(player, NukeType.Bullet);
                    return;
                case "explosive":
                    CraftingElement(player, NukeType.Explosive);
                    return;
                case "grenade":
                    CraftingElement(player, NukeType.Grenade);
                    return;
                case "mine":
                    CraftingElement(player, NukeType.Mine);
                    return;
                case "rocket":
                    CraftingElement(player, NukeType.Rocket);
                    return;
            }
        }
        [ConsoleCommand("NWUI_DeactivateButton")]
        private void cmdNWDeActivateButton(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            activeUsers.Remove(player.userID);
            CreateAmmoIcons(player);       
        }
        [ConsoleCommand("NWUI_DeactivateIcons")]
        private void cmdNWDeactivateIcons(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            activeUsers.Remove(player.userID);
            DestroyIconUI(player);
        }
        [ConsoleCommand("NWUI_OpenMenu")]
        private void cmdNWOpenMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;           
            
            if (canRocket(player) || canBullet(player) || canMine(player) || canGrenade(player) || canExplosive(player) || canAll(player))
            {
                CloseMap(player);
                CheckPlayerEntry(player);
                OpenCraftingMenu(player);
            }
        }
        [ConsoleCommand("NWUI_Activate")]
        private void cmdNWActivate(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var nukeType = arg.GetString(0);
            if (!activeUsers.ContainsKey(player.userID))
                activeUsers.Add(player.userID, NukeType.Bullet);
            SendMSG(player, MSG("activated", player.UserIDString).Replace("<type>", nukeType.ToString()));
            switch (nukeType.ToLower())
            {
                case "bullet":
                    activeUsers[player.userID] = NukeType.Bullet;
                    break;
                case "explosive":
                    activeUsers[player.userID] = NukeType.Explosive;
                    break;
                case "grenade":
                    activeUsers[player.userID] = NukeType.Grenade;
                    break;
                case "mine":
                    activeUsers[player.userID] = NukeType.Mine;
                    break;
                case "rocket":
                    activeUsers[player.userID] = NukeType.Rocket;
                    break;
            }
            CreateAmmoIcons(player);
        }
        [ConsoleCommand("NWUI_ChangeElement")]
        private void cmdNWChangeElement(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var panelName = arg.GetString(0);
            switch (panelName)
            {
                case "bullets":
                    CraftingElement(player, NukeType.Bullet);
                    return;
                case "explosives":
                    CraftingElement(player, NukeType.Explosive);
                    return;
                case "grenades":
                    CraftingElement(player, NukeType.Grenade);
                    return;
                case "mines":
                    CraftingElement(player, NukeType.Mine);
                    return;
                case "rockets":
                    CraftingElement(player, NukeType.Rocket);
                    return;
            }
        }

        [ConsoleCommand("NWUI_DestroyAll")]
        private void cmdNWDestroyAll(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            OpenMap(player);
            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.DestroyUi(player, UIPanel);            
        }
        void DestroyCraftUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIEntry);            
        }
        void DestroyIconUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIIcon + "Bullet");
            CuiHelper.DestroyUi(player, UIIcon + "Rocket");
            CuiHelper.DestroyUi(player, UIIcon + "Explosive");
            CuiHelper.DestroyUi(player, UIIcon + "Grenade");
            CuiHelper.DestroyUi(player, UIIcon + "Mine");
            CuiHelper.DestroyUi(player, UIIcon);
        }
        #endregion
        #endregion

        #region Functions
        private void InitializePlugin()
        {
            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission("nukeweapons.rocket", this);
            permission.RegisterPermission("nukeweapons.bullet", this);
            permission.RegisterPermission("nukeweapons.mine", this);
            permission.RegisterPermission("nukeweapons.explosive", this);
            permission.RegisterPermission("nukeweapons.grenade", this);
            permission.RegisterPermission("nukeweapons.all", this);
            permission.RegisterPermission("nukeweapons.unlimited", this);
        }
        private bool HasAmmo(ulong player, NukeType type)
        {            
            if (cachedAmmo.ContainsKey(player))
            {
                if (cachedAmmo[player][type] > 0)
                    return true;
            }
            return false;
        }
        private Dictionary<string, int> GetCraftingComponents(NukeType type)
        {
            switch (type)
            {
                case NukeType.Mine:
                    return configData.Mines.CraftingCosts;
                case NukeType.Rocket:
                    return configData.Rockets.CraftingCosts;
                case NukeType.Bullet:
                    return configData.Bullets.CraftingCosts;
                case NukeType.Explosive:
                    return configData.Explosives.CraftingCosts;
                case NukeType.Grenade:
                    return configData.Grenades.CraftingCosts;
                default:
                    return null;
            }
        }
        private NWType GetConfigFromType(NukeType type)
        {
            switch (type)
            {
                case NukeType.Mine:
                    return configData.Mines;
                case NukeType.Rocket:
                    return configData.Rockets;
                case NukeType.Bullet:
                    return configData.Bullets;
                case NukeType.Explosive:
                    return configData.Explosives;
                case NukeType.Grenade:
                    return configData.Grenades;
                default:
                    return null;
            }
        }
        private void CheckPlayerEntry(BasePlayer player)
        {
            if (!cachedAmmo.ContainsKey(player.userID))
            {
                cachedAmmo.Add(player.userID, new Dictionary<NukeType, int>
                {
                    {NukeType.Bullet, 0 },
                    {NukeType.Explosive, 0 },
                    {NukeType.Grenade, 0 },
                    {NukeType.Mine, 0 },
                    {NukeType.Rocket, 0 },
                });
            }
            if (!craftingTimers.ContainsKey(player.userID))
                craftingTimers.Add(player.userID, new Dictionary<NukeType, double>
                {
                    {NukeType.Bullet, -1 },
                    {NukeType.Explosive, -1 },
                    {NukeType.Grenade, -1 },
                    {NukeType.Mine, -1 },
                    {NukeType.Rocket, -1 },
                });
        }
        #endregion

        #region Radiation Control
        private void InitializeZone(Vector3 Location, float intensity, float duration, float radius, bool explosionType = false)
        {
            if (!ConVar.Server.radiation)
                ConVar.Server.radiation = true;
            if (explosionType) Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", Location);
            else Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/rocket_explosion.prefab", Location);

            var newZone = new GameObject().AddComponent<RadZones>();
            newZone.Activate(Location, radius, intensity);

            var listEntry = new ZoneList { zone = newZone };
            listEntry.time = timer.Once(duration, () => DestroyZone(listEntry));

            RadiationZones.Add(listEntry);
        }
        private void DestroyZone(ZoneList zone)
        {
            if (RadiationZones.Contains(zone))
            {
                var index = RadiationZones.FindIndex(a => a.zone == zone.zone);
                RadiationZones[index].time.Destroy();
                UnityEngine.Object.Destroy(RadiationZones[index].zone);
                RadiationZones.Remove(zone);
            }            
        }
        class Nuke : MonoBehaviour
        {
            public NukeWeapons instance;
            public NukeType type;
            public RadiationStats stats;

            private void OnDestroy()
            {
                bool useExplosion = false;
                switch (type)
                {
                    case NukeType.Mine:
                        useExplosion = true;
                        break;
                    case NukeType.Rocket:
                        break;
                    case NukeType.Bullet:
                        break;
                    case NukeType.Explosive:
                        useExplosion = true;
                        break;
                    case NukeType.Grenade:
                        break;
                    default:
                        break;
                }
                instance.InitializeZone(transform.position, 30, 10, 20, useExplosion);
            }
            public void InitializeComponent(NukeWeapons ins, NukeType typ, RadiationStats sta)
            {
                instance = ins;
                type = typ;
                stats = sta;
            }
        }
        public class ZoneList
        {
            public RadZones zone;
            public Timer time;
        }
        public class RadZones : MonoBehaviour
        {
            private int ID;
            private Vector3 Position;
            private float ZoneRadius;
            private float RadiationAmount;

            private List<BasePlayer> InZone;

            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "NukeZone";

                var rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
            }
            public void Activate(Vector3 pos, float radius, float amount)
            {
                ID = UnityEngine.Random.Range(0, 999999999);
                Position = pos;
                ZoneRadius = radius;
                RadiationAmount = amount;

                gameObject.name = $"RadZone {ID}";
                transform.position = Position;
                transform.rotation = new Quaternion();
                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;

                var Rads = gameObject.GetComponent<TriggerRadiation>();
                Rads = Rads ?? gameObject.AddComponent<TriggerRadiation>();
                Rads.RadiationAmountOverride = RadiationAmount;
                Rads.radiationSize = ZoneRadius;
                Rads.interestLayers = playerLayer;
                Rads.enabled = true;

                if (IsInvoking("UpdateTrigger")) CancelInvoke("UpdateTrigger");
                InvokeRepeating("UpdateTrigger", 5f, 5f);
            }
            private void OnDestroy()
            {
                CancelInvoke("UpdateTrigger");
                Destroy(gameObject);
            }
            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>();
                {
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    sphereCollider.radius = ZoneRadius;
                }
            }
            private void UpdateTrigger()
            {
                InZone = new List<BasePlayer>();
                int entities = Physics.OverlapSphereNonAlloc(Position, ZoneRadius, colBuffer, playerLayer);
                for (var i = 0; i < entities; i++)
                {
                    var player = colBuffer[i].GetComponentInParent<BasePlayer>();
                    if (player != null)
                        InZone.Add(player);
                }
            }
        }

        #endregion
       
        #region Chat Commands
        [ChatCommand("nw")]
        private void cmdNukes(BasePlayer player, string command, string[] args)
        {
            if (canRocket(player) || canBullet(player) || canMine(player) || canGrenade(player) || canExplosive(player) || canAll(player))
            {
                CheckPlayerEntry(player);
                OpenCraftingMenu(player);
            }       
        }
        #endregion

        #region Permissions
        private bool canRocket(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "nukeweapons.rocket") || canAll(player);
        private bool canBullet(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "nukeweapons.bullet") || canAll(player);
        private bool canMine(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "nukeweapons.mine") || canAll(player);
        private bool canExplosive(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "nukeweapons.explosive") || canAll(player);
        private bool canGrenade(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "nukeweapons.grenade") || canAll(player);
        private bool canAll(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "nukeweapons.all") || player.IsAdmin();
        private bool hasUnlimited(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "nukeweapons.unlimited");
        #endregion

        #region Config        
        private ConfigData configData;
        
        class NWType
        {
            public bool Enabled { get; set; }
            public int MaxAllowed { get; set; }
            public int CraftTime { get; set; }
            public int CraftAmount { get; set; }
            public Dictionary<string, int> CraftingCosts { get; set; }
            public RadiationStats RadiationProperties { get; set; }
        }
        class RadiationStats
        {
            public float Intensity { get; set; }
            public float Duration { get; set; }
            public float Radius { get; set; }
        }
        
        class Options
        {
            public string MSG_MainColor { get; set; }
            public string MSG_SecondaryColor { get; set; }
        }
        class ConfigData
        {            
            public NWType Mines { get; set; }
            public NWType Rockets { get; set; }
            public NWType Bullets { get; set; }
            public NWType Grenades { get; set; }
            public NWType Explosives { get; set; }
            public Options Options { get; set; }
            public Dictionary<string, string> URL_IconList { get; set; }
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
                Bullets = new NWType
                {
                    CraftAmount = 5,
                    CraftTime = 30,
                    CraftingCosts = new Dictionary<string, int>
                    {
                        {"ammo.rifle.explosive", 5 },
                        {"sulfur", 10 },
                        {"lowgradefuel", 10 }
                    },
                    Enabled = true,
                    MaxAllowed = 100,
                    RadiationProperties = new RadiationStats
                    {
                        Intensity = 15,
                        Duration = 3,
                        Radius = 5
                    }
                },
                Explosives = new NWType
                {
                    CraftAmount = 1,
                    CraftTime = 90,
                    CraftingCosts = new Dictionary<string, int>
                    {
                        {"explosive.timed", 1 },
                        {"sulfur", 150 },
                        {"lowgradefuel", 200 }
                    },
                    Enabled = true,
                    MaxAllowed = 3,
                    RadiationProperties = new RadiationStats
                    {
                        Intensity = 60,
                        Duration = 30,
                        Radius = 25
                    }
                },
                Grenades = new NWType
                {
                    CraftAmount = 1,
                    CraftTime = 45,
                    CraftingCosts = new Dictionary<string, int>
                    {
                        {"grenade.f1", 1 },
                        {"sulfur", 100 },
                        {"lowgradefuel", 100 }
                    },
                    Enabled = true,
                    MaxAllowed = 3,
                    RadiationProperties = new RadiationStats
                    {
                        Intensity = 35,
                        Duration = 15,
                        Radius = 15
                    }
                },
                Mines = new NWType
                {
                    CraftAmount = 1,
                    CraftTime = 60,
                    CraftingCosts = new Dictionary<string, int>
                    {
                        {"trap.landmine", 1 },
                        {"sulfur", 100 },
                        {"lowgradefuel", 150 }
                    },
                    Enabled = true,
                    MaxAllowed = 5,
                    RadiationProperties = new RadiationStats
                    {
                        Intensity = 70,
                        Duration = 25,
                        Radius = 20
                    }
                },
                Rockets = new NWType
                {
                    CraftAmount = 1,
                    CraftTime = 60,
                    CraftingCosts = new Dictionary<string, int>
                    {
                        {"ammo.rocket.basic", 1 },
                        {"sulfur", 150 },
                        {"lowgradefuel", 150 }
                    },
                    Enabled = true,
                    MaxAllowed = 3,
                    RadiationProperties = new RadiationStats
                    {
                        Intensity = 45,
                        Duration = 15,
                        Radius = 10
                    }
                },
                Options = new Options
                {
                    MSG_MainColor = "<color=#00CC00>",
                    MSG_SecondaryColor = "<color=#939393>"                    
                },
                URL_IconList = new Dictionary<string, string>
                {
                    {"BulletActive", "bulletactive.png" },
                    {"ExplosiveActive", "explosiveactive.png" },
                    {"GrenadeActive", "grenadeactive.png" },
                    {"MineActive", "landmineactive.png" },
                    {"RocketActive", "rocketactive.png" },
                    {"Bullet", "bullet.png" },
                    {"Explosive", "explosive.png" },
                    {"Grenade", "grenade.png" },
                    {"Mine", "landmine.png" },
                    {"Rocket", "rocket.png" },
                    {"Background", "background.png" }
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
            nukeData.ammo = cachedAmmo;
            data.WriteObject(nukeData);
        }
        void SaveDisplayNames()
        {
            itemNames.DisplayNames = DisplayNames;
            Item_Names.WriteObject(itemNames);
        }
        void LoadData()
        {
            try
            {
                nukeData = data.ReadObject<NukeData>();
                cachedAmmo = nukeData.ammo;
            }
            catch
            {
                nukeData = new NukeData();
            }
            try
            {
                itemNames = Item_Names.ReadObject<ItemNames>();
            }
            catch
            {
                Puts("Couldn't load item display name data, creating new datafile");
                itemNames = new ItemNames();
            }
        }
        class NukeData
        {
            public Dictionary<ulong, Dictionary<NukeType, int>> ammo = new Dictionary<ulong, Dictionary<NukeType, int>>();
            public List<uint> Mines = new List<uint>();
            public Dictionary<string, uint> ImageIDs = new Dictionary<string, uint>();
        }
        class ItemNames
        {
            public Dictionary<string, string> DisplayNames = new Dictionary<string, string>();
        }
        class PlayerAmmo
        {
            public int Rockets;
            public int Mines;
            public int Bullets;
            public int Explosives;
            public int Grenades;
        }
        enum NukeType
        {
            Mine,
            Rocket,
            Bullet,
            Explosive,
            Grenade
        }
        #endregion

        #region Unity WWW
        class QueueItem
        {
            public string url;
            public string imagename;

            public QueueItem(string ur, string na)
            {
                url = ur;
                imagename = na;               
            }
        }
        class UnityWeb : MonoBehaviour
        {
            NukeWeapons filehandler;
            const int MaxActiveLoads = 3;
            private Queue<QueueItem> QueueList = new Queue<QueueItem>();
            static byte activeLoads;
            private MemoryStream stream = new MemoryStream();

            private void Awake()
            {
                filehandler = (NukeWeapons)Interface.Oxide.RootPluginManager.GetPlugin(nameof(NukeWeapons));
            }
            private void OnDestroy()
            {
                QueueList.Clear();
                filehandler = null;
            }
            public void Add(string url, string imagename)
            {
                QueueList.Enqueue(new QueueItem(url, imagename));
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
                        if (!filehandler.nukeData.ImageIDs.ContainsKey(info.imagename))
                            filehandler.nukeData.ImageIDs.Add(info.imagename, 0);            
                        ClearStream();
                        stream.Write(www.bytes, 0, www.bytes.Length);
                        uint textureID = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue);
                        ClearStream();
                        filehandler.nukeData.ImageIDs[info.imagename] = textureID;
                    }
                    activeLoads--;
                    if (QueueList.Count > 0) Next();
                    else filehandler.SaveData();
                }
            }
        }
        [ConsoleCommand("nukeicons")]
        private void cmdNukeIcons(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                string dir = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "NukeWeapons" + Path.DirectorySeparatorChar + "Icons" + Path.DirectorySeparatorChar;
                foreach (var image in configData.URL_IconList)
                    uWeb.Add(dir + image.Value, image.Key);
            }
        }
        
        
        #endregion

        #region Messaging
        private void SendMSG(BasePlayer player, string message, string message2 = "") => SendReply(player, $"{configData.Options.MSG_MainColor}{message}</color>{configData.Options.MSG_SecondaryColor}{message2}</color>");
        private string MSG(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"Bullet", "Bullet" },
            {"Explosive", "Explosive" },
            {"Grenade", "Grenade" },
            {"Rocket", "Rocket" },
            {"Mine", "Mine" },
            {"Bullets", "Bullets" },
            {"Explosives", "Explosives" },
            {"Grenades", "Grenades" },
            {"Rockets", "Rockets" },
            {"Mines", "Mines" },
            {"activated", "You have activated Nuke <type>s" },
            {"Menu", "Menu" },
            {"Deactivate", "Deactivate" },
            {"Disarm", "Disarm" },
            {"Arm", "Arm" },
            {"Inventory Amount", "Inventory Amount" },
            {"Unlimited", "Unlimited" },
            {"Crafting...", "Crafting..." },
            {"Limit Reached", "Limit Reached" },
            {"Craft", "Craft" },
            {"Item Name", "Item Name" },
            {"Required Amount", "Required Amount" },
            {"Your Supply", "Your Supply" },
            {"Required Ingredients", "Required Ingredients" },
            {"Close", "Close" },
            {"OOA", "You have run out of Nuke" },
            {"Crafting", "Crafting" }
        };
        #endregion
    }
}
