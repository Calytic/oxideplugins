using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("TrollTax", "Absolut", "1.0.0", ResourceId = 000000)]

    class TrollTax : RustPlugin
    {
        #region Fields

        [PluginReference]
        Plugin LustyMap;

        string TitleColor = "<color=orange>";
        string MsgColor = "<color=#A9A9A9>";

        TrollTaxData ttData;
        private DynamicConfigFile TTData;

        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        private Dictionary<ulong, Coords> BoxPrep = new Dictionary<ulong, Coords>();

        #endregion

        #region Server Hooks

        void Loaded()
        {
            TTData = Interface.Oxide.DataFileSystem.GetFile("TrollTax_Data");
            lang.RegisterMessages(messages, this);
        }

        void Unload()
        {
            BoxPrep.Clear();
            foreach (var entry in timers)
                entry.Value.Destroy();
            timers.Clear();
            SaveData();
        }

        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            timers.Add("info", timer.Once(900, () => InfoLoop()));
            timers.Add("save", timer.Once(600, () => SaveLoop()));
            SaveData();
        }

        #endregion

        #region Player Hooks

        void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            if (planner == null) return;
            if (gameobject.GetComponent<BaseEntity>() != null)
            {
                BaseEntity container = gameobject.GetComponent<BaseEntity>();
                var entityowner = gameobject.GetComponent<BaseEntity>().OwnerID;
                if (container.PrefabName == "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab" || container.PrefabName == "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab")
                {
                    if (BoxPrep.ContainsKey(entityowner)) BoxPrep.Remove(entityowner);
                    BoxPrep.Add(entityowner, new Coords { x = gameobject.transform.position.x, y = gameobject.transform.position.y, z = gameobject.transform.position.z });
                    BasePlayer player = BasePlayer.FindByID(entityowner);
                    TaxBoxConfirmation(player);
                }
            }
        }

        private void OnEntityDeath(BaseEntity entity, HitInfo hitInfo)
        {
            if (entity is StorageContainer)
            {
                Vector3 ContPosition = entity.transform.position;
                if (ttData.TaxBox.ContainsKey(entity.OwnerID))
                {
                    if (ContPosition == new Vector3 ( ttData.TaxBox[entity.OwnerID].x, ttData.TaxBox[entity.OwnerID].y, ttData.TaxBox[entity.OwnerID].z))
                    {
                        ttData.TaxBox.Remove(entity.OwnerID);
                        BasePlayer owner = BasePlayer.FindByID(entity.OwnerID);
                        if (BasePlayer.activePlayerList.Contains(owner))
                            GetSendMSG(owner, "TaxBoxDestroyed");
                    }
                    SaveData();
                }
                return;
            }
            if (entity is BasePlayer)
            {
                var victim = entity.ToPlayer();
                if (ttData.TaxCollector.ContainsKey(victim.userID))
                {
                    ttData.TaxCollector.Remove(victim.userID);
                    SaveData();
                }
                if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
                {
                    var attacker = hitInfo.Initiator.ToPlayer() as BasePlayer;
                    if (entity as BasePlayer == null || hitInfo == null) return;
                    if (victim.userID != attacker.userID)
                    {
                        if (!ttData.TaxCollector.ContainsKey(attacker.userID))
                            ttData.TaxCollector.Add(attacker.userID, new List<ulong>());
                        ttData.TaxCollector[attacker.userID].Add(victim.userID);
                        SaveData();
                    }
                }
            }
        }

        void OnPlantGather(PlantEntity Plant, Item item, BasePlayer player)
        {
            if (!isPayor(player.userID)) return;
            var taxrate = configData.TaxRate;
            List<StorageContainer> TaxContainers = GetTaxContainer(player.userID);
            if (TaxContainers == null) return;
            int taxcollectors = 0;
            foreach (var entry in ttData.TaxCollector.Where(kvp => kvp.Value.Contains(player.userID)))
                taxcollectors++;
            var maxtaxors = Math.Floor(100 / taxrate);
            if (maxtaxors < taxcollectors)
                taxrate = 90 / taxcollectors;

            int Tax = Convert.ToInt32(Math.Ceiling((item.amount * taxrate) / 100));
            item.amount = item.amount - (Tax * taxcollectors);
            foreach (StorageContainer cont in TaxContainers)
            {
                if (!cont.inventory.IsFull())
                {
                    ItemDefinition ToAdd = ItemManager.FindItemDefinition(item.info.itemid);
                    if (ToAdd != null)
                    {
                        cont.inventory.AddItem(ToAdd, Tax);
                    }
                }
                else if (BasePlayer.activePlayerList.Contains(BasePlayer.FindByID(cont.OwnerID)))
                    if (timers.ContainsKey(cont.OwnerID.ToString()))
                    {
                        GetSendMSG(player, "TaxBoxFull");
                        SetBoxFullNotification(cont.OwnerID.ToString());
                        return;
                    }
            }
        }


        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (!isPayor(player.userID)) return;
            var taxrate = configData.TaxRate;
            List<StorageContainer> TaxContainers = GetTaxContainer(player.userID);
            if (TaxContainers == null) return;
            int taxcollectors = 0;
            foreach (var entry in ttData.TaxCollector.Where(kvp => kvp.Value.Contains(player.userID)))
                taxcollectors++;
            var maxtaxors = Math.Floor(100 / taxrate);
            if (maxtaxors < taxcollectors)
                taxrate = 90 / taxcollectors;

            int Tax = Convert.ToInt32(Math.Ceiling((item.amount * taxrate) / 100));
            item.amount = item.amount - (Tax * taxcollectors);
            foreach (StorageContainer cont in TaxContainers)
            {
                if (!cont.inventory.IsFull())
                {
                    ItemDefinition ToAdd = ItemManager.FindItemDefinition(item.info.itemid);
                    if (ToAdd != null)
                    {
                        cont.inventory.AddItem(ToAdd, Tax);
                    }
                }
                else if (BasePlayer.activePlayerList.Contains(BasePlayer.FindByID(cont.OwnerID)))
                    if (timers.ContainsKey(cont.OwnerID.ToString()))
                    {
                        GetSendMSG(player, "TaxBoxFull");
                        SetBoxFullNotification(cont.OwnerID.ToString());
                        return;
                    }
            }
        }

        void OnDispenserGather(ResourceDispenser Dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (!isPayor(entity.ToPlayer().userID)) return;
            var taxrate = configData.TaxRate;
            List<StorageContainer> TaxContainers = GetTaxContainer(player.userID);
            if (TaxContainers == null) return;
            int taxcollectors = 0;
            foreach (var entry in ttData.TaxCollector.Where(kvp => kvp.Value.Contains(player.userID)))
                taxcollectors++; 
            var maxtaxors = Math.Floor(100 / taxrate);
            if (maxtaxors < taxcollectors)
                taxrate = 90 / taxcollectors;
            int Tax = Convert.ToInt32(Math.Ceiling((item.amount * taxrate) / 100));
            item.amount = item.amount - (Tax * taxcollectors);
            foreach (StorageContainer cont in TaxContainers)
            {
                if (!cont.inventory.IsFull())
                {
                    ItemDefinition ToAdd = ItemManager.FindItemDefinition(item.info.itemid);
                    if (ToAdd != null)
                    {
                        cont.inventory.AddItem(ToAdd, Tax);
                    }
                }
                else if (BasePlayer.activePlayerList.Contains(BasePlayer.FindByID(cont.OwnerID)))
                    if (timers.ContainsKey(cont.OwnerID.ToString()))
                    {
                        GetSendMSG(player, "TaxBoxFull");
                        SetBoxFullNotification(cont.OwnerID.ToString());
                        return;
                    }
            }
        }


        #endregion

        #region Functions
        public bool isPayor(ulong ID)
        {
            foreach (var entry in ttData.TaxCollector)
            {
                if (entry.Value.Contains(ID))
                {
                    return true;
                }
                else continue;
            }
            return false;
        }

        private List<StorageContainer> GetTaxContainer(ulong Payor)
        {
            List<StorageContainer> Containers = new List<StorageContainer>();
            foreach (var entry in ttData.TaxCollector.Where(kvp => kvp.Value.Contains(Payor)))
            {
                if (ttData.TaxBox.ContainsKey(entry.Key))
                {
                    Vector3 containerPos = new Vector3 (ttData.TaxBox[entry.Key].x, ttData.TaxBox[entry.Key].y, ttData.TaxBox[entry.Key].z );
                    foreach (StorageContainer Cont in StorageContainer.FindObjectsOfType<StorageContainer>())
                    {
                        Vector3 ContPosition = Cont.transform.position;
                        if (ContPosition == containerPos)
                            Containers.Add(Cont);
                    }
                }
            }
            if (Containers.Count > 0)
                return Containers;
            else return null;
        }

        private string GetLang(string msg)
        {
            if (messages.ContainsKey(msg))
                return lang.GetMessage(msg, this);
            else return msg;
        }

        private void GetSendMSG(BasePlayer player, string message, string arg1 = "", string arg2 = "", string arg3 = "")
        {
            string msg = string.Format(lang.GetMessage(message, this), arg1, arg2, arg3);
            SendReply(player, TitleColor + lang.GetMessage("title", this, player.UserIDString) + "</color>" + MsgColor + msg + "</color>");
        }

        private string GetMSG(string message, string arg1 = "", string arg2 = "", string arg3 = "")
        {
            string msg = string.Format(lang.GetMessage(message, this), arg1, arg2, arg3);
            return msg;
        }

        public void DestroyTaxPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelTax);
        }
        #endregion

        #region UI Creation

        private string PanelTax = "Tax";

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

            static public void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void CreateTextOverlay(ref CuiElementContainer container, string panel, string text, string color, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                //if (configdata.DisableUI_FadeIn)
                //    fadein = 0;
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
        }

        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"black", "0 0 0 1.0" },
            {"dark", "0.1 0.1 0.1 0.98" },
            {"header", "1 1 1 0.3" },
            {"light", ".564 .564 .564 1.0" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"brown", "0.3 0.16 0.0 1.0" },
            {"yellow", "0.9 0.9 0.0 1.0" },
            {"orange", "1.0 0.65 0.0 1.0" },
            {"blue", "0.2 0.6 1.0 1.0" },
            {"red", "1.0 0.1 0.1 1.0" },
            {"white", "1 1 1 1" },
            {"green", "0.28 0.82 0.28 1.0" },
            {"grey", "0.85 0.85 0.85 1.0" },
            {"lightblue", "0.6 0.86 1.0 1.0" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttongreen", "0.133 0.965 0.133 0.9" },
            {"buttonred", "0.964 0.133 0.133 0.9" },
            {"buttongrey", "0.8 0.8 0.8 0.9" },
            {"CSorange", "1.0 0.64 0.10 1.0" }
        };
        #endregion

        #region UI Panels

        private void TaxBoxConfirmation(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelTax);
            var element = UI.CreateElementContainer(PanelTax, UIColors["dark"], "0.425 0.45", "0.575 0.55", true);
            UI.CreatePanel(ref element, PanelTax, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, PanelTax, MsgColor, GetLang("TaxBoxCreation"), 14, "0.05 0.5", "0.95 0.9");
            UI.CreateButton(ref element, PanelTax, UIColors["buttongreen"], GetLang("Yes"), 14, "0.05 0.1", "0.475 0.4", $"UI_SaveTaxBox");
            UI.CreateButton(ref element, PanelTax, UIColors["buttonred"], GetLang("No"), 14, "0.525 0.1", "0.95 0.4", $"UI_DestroyTaxPanel");
            CuiHelper.AddUi(player, element);
        }
        #endregion

        #region UI Commands

        [ConsoleCommand("UI_SaveTaxBox")]
        private void cmdUI_SaveTaxBox(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyTaxPanel(player);
            if (BoxPrep.ContainsKey(player.userID))
            {
                if (ttData.TaxBox.ContainsKey(player.userID)) ttData.TaxBox.Remove(player.userID);
                ttData.TaxBox.Add(player.userID, new Coords { x = BoxPrep[player.userID].x, y = BoxPrep[player.userID].y, z = BoxPrep[player.userID].z });
            }
            else GetSendMSG(player, "NoBoxPrepped");
            SaveData();
        }

        [ConsoleCommand("UI_DestroyTaxPanel")]
        private void cmdUI_DestroyTaxPanel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyTaxPanel(player);
        }
        #endregion

        #region Timers

        private void SaveLoop()
        {
            if (timers.ContainsKey("save"))
            {
                timers["save"].Destroy();
                timers.Remove("save");
            }
            SaveData();
            timers.Add("save", timer.Once(600, () => SaveLoop()));
        }

        private void InfoLoop()
        {
            if (timers.ContainsKey("info"))
            {
                timers["info"].Destroy();
                timers.Remove("info");
            }
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                GetSendMSG(p, "TrollTaxInfo");
            }
            timers.Add("info", timer.Once(900, () => InfoLoop()));
        }

        private void SetBoxFullNotification(string ID)
        {
            timers.Add(ID, timer.Once(5 * 60, () => timers.Remove(ID)));
        }

        #endregion

        #region Classes
        class TrollTaxData
        {
            public Dictionary<ulong, List<ulong>> TaxCollector = new Dictionary<ulong, List<ulong>>();
            public Dictionary<ulong, Coords> TaxBox = new Dictionary<ulong, Coords>();
        }

        class Coords
        {
            public float x;
            public float y;
            public float z;
        }
        #endregion

        #region Data Management

        void SaveData()
        {
            TTData.WriteObject(ttData);
        }

        void LoadData()
        {
            try
            {
                ttData = TTData.ReadObject<TrollTaxData>();
            }
            catch
            {
                Puts("Couldn't load TrollTax data, creating new datafile");
                ttData = new TrollTaxData();
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            //--------//General Settings//--------//
            public double TaxRate { get; set; }
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
                TaxRate = 5,
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messages
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "TrollTax: " },
            {"TrollTaxInfo", "This server is running TrollTax. You will become a tax collector for each player you kill until you die. To create a tax box simply place a box on the ground."},
            {"NoBoxPrepped", "Error finding target tax box!" },
            {"TaxBoxDestroyed", "Your tax box has been destroyed!" },
            {"TaxBoxFull", "Your tax box is full! Clear room to generate taxes." },
            {"TaxBoxCreation", "Would like to make this your tax box?" },
            {"Yes", "Yes?" },
            {"No", "No?" }
        };
        #endregion
    }
}
