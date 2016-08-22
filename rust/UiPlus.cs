//
//  By
//      Ron Dekker (www.RonDekker.nl, @RedKenrok)
//  
//  Distribution
//      http://www.GitHub.com/RedKenrok/OxidePlugins
//
//  License
//      GNU GENERAL PUBLIC LICENSE (Version 3, 29 June 2007)
//
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

namespace Oxide.Plugins {
    [Info("UiPlus", "RedKenrok", "1.0.0")]
    [Description("Adds user elements to the user interface containing; the active players count, maximum player slots, sleeping players, and ingame time.")]
    internal class UiPlus : RustPlugin {

        #region Enums
        /// <summary>The different panels that the plugin can display.</summary>
        private enum PanelTypes { Active = 0, Sleeping = 1, Clock = 2 };
        /// <summary>The amount of different panels.</summary>
        private static readonly int panelCount = Enum.GetValues(typeof(PanelTypes)).Length;
        /// <summary>The different container types.</summary>
        private enum ContainerTypes { Static = 0, Dynamic = 1 };
        /// <summary>The amount of different container types.</summary>
        private static readonly int containerTypesCount = Enum.GetValues(typeof(ContainerTypes)).Length;
        /// <summary>The different fields that the plugin can display per panel.</summary>
        private enum FieldTypes { PlayersActive, PlayerMax, PlayersSleeping, Time };
        #endregion

        #region Classes
        /// <summary>A struct that can contain all the component data for one panel.</summary>
        private struct PanelData {
            /// <summary>The panel type to which this data belongs.</summary>
            public PanelTypes panelType;

            /// <summary>The data for the CuiRectTransformComponent for the background of the panel.</summary>
            public Dictionary<string, object> backgroundRect;
            /// <summary>The data for the CuiImageComponent for the background of the panel.</summary>
            public Dictionary<string, object> backgroundImage;
            /// <summary>The data for the CuiRectTransformComponent for the icon of the panel.</summary>
            public Dictionary<string, object> iconRect;
            /// <summary>The data for the CuiImageComponent for the icon of the panel.</summary>
            public Dictionary<string, object> iconImage;
            /// <summary>The data for the CuiRectTransformComponent for the text of the panel.</summary>
            public Dictionary<string, object> textRect;
            /// <summary>The data for the CuiTextComponent for the text of the panel.</summary>
            public Dictionary<string, object> textText;

            /// <summary>Constructor call for the PanelData struct.</summary>
            /// <param name="backgroundRect">The data for the CuiRectTransformComponent for the background of the panel.</param>
            /// <param name="backgroundImage">The data for the CuiImageComponent for the background of the panel.</param>
            /// <param name="iconRect">The data for the CuiRectTransformComponent for the icon of the panel.</param>
            /// <param name="iconImage">The data for the CuiImageComponent for the icon of the panel.</param>
            /// <param name="textRect">The data for the CuiRectTransformComponent for the text of the panel.</param>
            /// <param name="textText">The data for the CuiTextComponent for the text of the panel.</param>
            public PanelData(PanelTypes panelType, Dictionary<string, object> backgroundRect, Dictionary<string, object> backgroundImage, Dictionary<string, object> iconRect, Dictionary<string, object> iconImage, Dictionary<string, object> textRect, Dictionary<string, object> textText) {
                this.panelType = panelType;

                this.backgroundRect = backgroundRect;
                this.backgroundImage = backgroundImage;
                this.iconRect = iconRect;
                this.iconImage = iconImage;
                this.textRect = textRect;
                this.textText = textText;
            }
        }

        /// <summary>The properties that make up a rect transform component.</summary>
        private class RectProperties {
            public static readonly string anchorMin = "Anchor Min";
            public static readonly string anchorMax = "Anchor Max";
            public static readonly string offset = "Offset";
        }

        /// <summary>The properties that make up an image component.</summary>
        private class ImageProperties {
            public static readonly string color = "Color";
            public static readonly string uri = "Uri";
        }

        /// <summary>The properties that make up a text component.</summary>
        private class TextProperties {
            public static readonly string align = "Alignment";
            public static readonly string color = "Color";
            public static readonly string font = "Font";
            public static readonly string fontSize = "Font size";
            public static readonly string text = "Format";
        }
        #endregion

        #region Variables
        /// <summary>The name of the plugin.</summary>
        private static readonly string pluginName = "UiPlus";

        /// <summary>This instance of the plugin.</summary>
        private static UiPlus instance = null;

        /// <summary>Please use gameObject instead of this.</summary>
        /// <seealso cref="gameObject"/>
        private static GameObject _gameObject = null;
        /// <summary>The game object belonging to this plugin.</summary>
        private static GameObject gameObject {
            get {
                if (_gameObject == null) {
                    _gameObject = new GameObject(pluginName + "Object");
                }
                return _gameObject;
            }
        }
        // Don't use the following. For some reason that breaks it...
        //private static GameObject gameObject = new GameObject(pluginName + "Object");

        /// <summary>Default rect transform component to retrieve default values from for the dictionaries.</summary>
        private static readonly CuiRectTransformComponent defaultRectComponent = new CuiRectTransformComponent();
        /// <summary>Default image component to retrieve default values from for the dictionaries.</summary>
        private static readonly CuiImageComponent defaultImageComponent = new CuiImageComponent();
        /// <summary>Default text component to retrieve default values from for the dictionaries.</summary>
        private static readonly CuiTextComponent defaultTextComponent = new CuiTextComponent();

        /// <summary>The name of the parent of the ui elements's container.</summary>
        private static readonly string ContainerParent = "Overlay";
        /// <summary>The static containers in the form of an array ordered according to the panels enumerator.</summary>
        private CuiElementContainer[] staticContainers;

        /// <summary>The character used in between values of the Json.</summary>
        private static readonly char valueSeperator = ' ';

        /// <summary>The character put before the display field value in order to signal it has to be replaced.</summary>
        private static readonly char replacementPrefix = '{';
        /// <summary>The character put after the display field value in order to signal it has to be replaced.</summary>
        private static readonly char replacementSufix = '}';

        /// <summary>Directly retrieves time from the game.</summary>
        private string TIMERAW {
            get {
                if (clock24Hour) {
                    if (clockShowSeconds) {
                        return TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm:ss");
                    }
                    else {
                        return TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm");
                    }
                }
                else {
                    if (clockShowSeconds) {
                        return TOD_Sky.Instance.Cycle.DateTime.ToString("h:mm:ss tt");
                    }
                    else {
                        return TOD_Sky.Instance.Cycle.DateTime.ToString("h:mm tt");
                    }
                }
            }
        }

        /// <summary>Holds the time as a string value.</summary>
        private string TIME = "";

        /// <summary>The amount of times elapsed since loading before the reinitialzation of the static containers.</summary>
        private int iconWaitTimerCount = 0;
        /// <summary>The maximum amount of times the reinitialzation loop will run before giving up.</summary>
        private int iconWaitTimerMax = 60;
        /// <summary>The amount of icons loaded on the previous timer itteration.</summary>
        private bool[] iconsPresentPrevious = new bool[3] { false, false, false };
        #endregion

        #region Variables panel specifics
        /// <summary>Which panels to add ordered like the PanelTypes enum.</summary>
        private bool[] addPanels = new bool[3] {
            true,
            true,
            true
        };

        /// <summary>An array of data from each panel ordered in the same as the PanelTypes enum.</summary>
        private PanelData[] panelsData = new PanelData[3] {
            // Active
            new PanelData(
                // PanelType
                PanelTypes.Active,
                // Background Rect
                new Dictionary<string, object> {
                    { RectProperties.anchorMin, "0 0" },
                    { RectProperties.anchorMax, "0.048 0.036" },
                    { RectProperties.offset, "81 72" }
                },
                // Background Image
                new Dictionary<string, object> {
                    { ImageProperties.color, "1 0.95 0.875 0.025" }
                },
                // Icon Rect
                new Dictionary<string, object> {
                    { RectProperties.anchorMin, "0 0" },
                    { RectProperties.anchorMax, "0.325 0.75" },
                    { RectProperties.offset, "2 3" }
                },
                // Icon Icon
                new Dictionary<string, object> {
                    { ImageProperties.color, "0.7 0.7 0.7 1" },
                    { ImageProperties.uri, "http://i.imgur.com/UY0y5ZI.png" }
                },
                // Text Rect
                new Dictionary<string, object> {
                    { RectProperties.anchorMin, "0 0" },
                    { RectProperties.anchorMax, "1 1" },
                    { RectProperties.offset, "24 0" }
                },
                // Text Text
                new Dictionary<string, object> {
                    { TextProperties.align, "MiddleLeft" },
                    { TextProperties.color, "1 1 1 0.5" },
                    { TextProperties.font, defaultTextComponent.Font },
                    { TextProperties.fontSize, 14 },
                    { TextProperties.text, replacementPrefix + FieldTypes.PlayersActive.ToString().ToUpper() + replacementSufix + "/" + replacementPrefix + FieldTypes.PlayerMax.ToString().ToUpper() + replacementSufix }
                }
            ),
            // Sleeping
            new PanelData(
                // PanelType
                PanelTypes.Sleeping,
                // Background Rect
                new Dictionary<string, object> {
                    { RectProperties.anchorMin, "0 0" },
                    { RectProperties.anchorMax, "0.049 0.036" },
                    { RectProperties.offset, "145 72" }
                },
                // Background Image
                new Dictionary<string, object> {
                    { ImageProperties.color, "1 0.95 0.875 0.025" }
                },
                // Icon Rect
                new Dictionary<string, object> {
                    { RectProperties.anchorMin, "0 0" },
                    { RectProperties.anchorMax, "0.325 0.75" },
                    { RectProperties.offset, "2 3" }
                },
                // Icon Icon
                new Dictionary<string, object> {
                    { ImageProperties.color, "0.7 0.7 0.7 1" },
                    { ImageProperties.uri, "http://i.imgur.com/mvUBBOB.png" }
                },
                // Text Rect
                new Dictionary<string, object> {
                    { RectProperties.anchorMin, "0 0" },
                    { RectProperties.anchorMax, "1 1" },
                    { RectProperties.offset, "24 0" }
                },
                // Text Text
                new Dictionary<string, object> {
                    { TextProperties.align, "MiddleLeft" },
                    { TextProperties.color, "1 1 1 0.5" },
                    { TextProperties.font, defaultTextComponent.Font },
                    { TextProperties.fontSize, 14 },
                    { TextProperties.text, replacementPrefix + FieldTypes.PlayersSleeping.ToString().ToUpper() + replacementSufix }
                }
            ),
            // Clock
            new PanelData(
                // PanelType
                PanelTypes.Clock,
                // Background Rect
                new Dictionary<string, object> {
                    { RectProperties.anchorMin, "0 0" },
                    { RectProperties.anchorMax, "0.049 0.036" },
                    { RectProperties.offset, "16 72" }
                },
                // Background Image
                new Dictionary<string, object> {
                    { ImageProperties.color, "1 0.95 0.875 0.025" }
                },
                // Icon Rect
                new Dictionary<string, object> {
                    { RectProperties.anchorMin, "0 0" },
                    { RectProperties.anchorMax, "0.325 0.75" },
                    { RectProperties.offset, "2 3" }
                },
                // Icon Icon
                new Dictionary<string, object> {
                    { ImageProperties.color, "0.7 0.7 0.7 1" },
                    { ImageProperties.uri, "http://i.imgur.com/CycsoyW.png" }
                },
                // Text Rect
                new Dictionary<string, object> {
                    { RectProperties.anchorMin, "0 0" },
                    { RectProperties.anchorMax, "1 1" },
                    { RectProperties.offset, "24 0" }
                },
                // Text Text
                new Dictionary<string, object> {
                    { TextProperties.align, "MiddleLeft" },
                    { TextProperties.color, "1 1 1 0.5" },
                    { TextProperties.font, defaultTextComponent.Font },
                    { TextProperties.fontSize, 14 },
                    { TextProperties.text, replacementPrefix + FieldTypes.Time.ToString().ToUpper() + replacementSufix }
                }
            )
        };

        /// <summary>Whether the clock displays in a 24 or 12 hour format.</summary>
        private bool clock24Hour = true;
        /// <summary>Whether the clock also displays the seconds.</summary>
        private bool clockShowSeconds = false;
        /// <summary>The interval between clock panel updates in milliseconds.</summary>
        private int clockUpdateInterval = 2000;
        #endregion

        #region Configuration
        /// <summary>Default configuration loading function is overridden and has no functionality.</summary>
        protected override void LoadDefaultConfig() { }

        /// <summary>Checks the configuration file if the data is already present, if not it adds a default value.</summary>
        /// <typeparam name="T">The type of data.</typeparam>
        /// <param name="configName">The name of the data packet.</param>
        /// <param name="defaultValue">The default data to add if none is present.</param>
        /// <param name="defaultAddedToConfig">Wether or not a new field is added to the config.</param>
        /// <returns>Returns the data currently in the configuration file.</returns>
        private T CheckConfigFile<T>(string configName, T defaultValue, ref bool defaultApplied) {
            if (Config[configName] != null) {
                return (T)Config[configName];
            }
            else {
                Config[configName] = defaultValue;
                defaultApplied = true;
                return defaultValue;
            }
        }

        /// <summary>Initializes the data for the given panel.</summary>
        /// <param name="panelData">The panel data to be initialized.</param>
        /// <param name="defaultAddedToConfig">Wether or not a new field is added to the config.</param>
        private void InitializePanelData(ref PanelData panelData, ref bool defaultAddedToConfig) {
            panelData.backgroundRect = CheckConfigFile(panelData.panelType.ToString() + " backgroundRect", panelData.backgroundRect, ref defaultAddedToConfig);
            panelData.backgroundImage = CheckConfigFile(panelData.panelType.ToString() + " backgroundImage", panelData.backgroundImage, ref defaultAddedToConfig);
            panelData.iconRect = CheckConfigFile(panelData.panelType.ToString() + " iconRect", panelData.iconRect, ref defaultAddedToConfig);
            panelData.iconImage = CheckConfigFile(panelData.panelType.ToString() + " iconImage", panelData.iconImage, ref defaultAddedToConfig);
            panelData.textRect = CheckConfigFile(panelData.panelType.ToString() + " textRect", panelData.textRect, ref defaultAddedToConfig);
            panelData.textText = CheckConfigFile(panelData.panelType.ToString() + " textText", panelData.textText, ref defaultAddedToConfig);
        }

        /// <summary>Retrieves all the data from the configuration file and adds default data if it is not present.</summary>
        private void InitializeConfiguration() {
            bool defaultApplied = false;

            for (int i = 0; i < panelCount; i++) {
                addPanels[i] = CheckConfigFile("__Create " + ((PanelTypes) i).ToString() + " panel", addPanels[i], ref defaultApplied);
                InitializePanelData(ref panelsData[i], ref defaultApplied);
            }

            clock24Hour = CheckConfigFile("_Clock 24 hour format", clock24Hour, ref defaultApplied);
            clockShowSeconds = CheckConfigFile("_Clock show seconds", clockShowSeconds, ref defaultApplied);
            clockUpdateInterval = CheckConfigFile("_Clock update frequency in milliseconds", clockUpdateInterval, ref defaultApplied);

            SaveConfig();
            
            if (defaultApplied) {
                PrintWarning("New field(s) added to the configuration file please view and edit if necessary.");
            }
        }
        #endregion

        #region Hooks
        [HookMethod("Loaded")]
        private void Loaded() {
            // Adds singleton pattern.
            instance = this;

            // Initializes the configuration.
            //PrintWarning("Config read and write disabled.");
            InitializeConfiguration();

            // Takes the background rect of the panel and applies the text rect of the panel to calculate the result that will be used.
            for (int i = 0; i < panelCount; i++) {
                panelsData[i].textRect[RectProperties.anchorMin] = StringUtilities.ToString(VectorUtilities.Multiply(VectorUtilities.ToVector2(panelsData[i].backgroundRect[RectProperties.anchorMin].ToString(), valueSeperator), VectorUtilities.ToVector2(panelsData[i].textRect[RectProperties.anchorMin].ToString(), valueSeperator)));
                panelsData[i].textRect[RectProperties.anchorMax] = StringUtilities.ToString(VectorUtilities.Multiply(VectorUtilities.ToVector2(panelsData[i].backgroundRect[RectProperties.anchorMax].ToString(), valueSeperator), VectorUtilities.ToVector2(panelsData[i].textRect[RectProperties.anchorMax].ToString(), valueSeperator)));
                panelsData[i].textRect[RectProperties.offset] = StringUtilities.ToString(VectorUtilities.ToVector2(panelsData[i].backgroundRect[RectProperties.offset].ToString(), valueSeperator) + VectorUtilities.ToVector2(panelsData[i].textRect[RectProperties.offset].ToString(), valueSeperator));
            }
        }

        /// <summary>Called after the server startup has been completed and is awaiting connections.</summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized() {
            // Initializes the static containers for each panel.
            staticContainers = new CuiElementContainer[panelCount];
            for (int i = 0; i < panelCount; i++) {
                if (addPanels[i]) {
                    InitializeStaticContainer(panelsData[i]);
                }
            }

            // Adds and updates the panels for each active player.
            for (int i = 0; i < BasePlayer.activePlayerList.Count * panelCount; i++) {
                if (addPanels[i % panelCount]) {
                    CuiHelper.AddUi(BasePlayer.activePlayerList[i / panelCount], staticContainers[i % panelCount]);
                    UpdateField(BasePlayer.activePlayerList[i / panelCount], (PanelTypes)(i % panelCount));
                }
            }

            if (addPanels[(int)PanelTypes.Clock]) {
                StartRepeatingFieldUpdate(PanelTypes.Clock, clockUpdateInterval);
            }

            // Starts retrieving the icons.
            for (int i = 0; i < panelCount; i++) {
                FileManager.InitializeFile(((PanelTypes)i).ToString(), panelsData[i].iconImage[ImageProperties.uri].ToString());
            }

            WaitForIconsDownloaded();
        }

        /// <summary>Called when a plugin is being unloaded.</summary>
        [HookMethod("Unload")]
        private void Unload() {
            // Removes all panels for the players.
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++) {
                for (int j = 0; j < panelCount * containerTypesCount; j++) {
                    if (addPanels[j % panelCount]) {
                        CuiHelper.DestroyUi(BasePlayer.activePlayerList[i], pluginName + ((PanelTypes)(j % panelCount)).ToString() + ((ContainerTypes)(j / panelCount)).ToString());
                    }
                }
            }
        }

        /// <summary>Called when the player awakes.</summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerSleepEnded")]
        private void OnPlayerSleepEnded(BasePlayer player) {
            for (int i = 0; i < panelCount; i++) {
                if (addPanels[i]) {
                    CuiHelper.DestroyUi(player, pluginName + ((PanelTypes)i).ToString() + ContainerTypes.Static.ToString());
                    CuiHelper.AddUi(player, staticContainers[i]);
                }
            }

            if (addPanels[(int)PanelTypes.Active]) {
                UpdateField(player, PanelTypes.Active);
            }
            if (addPanels[(int)PanelTypes.Clock]) {
                UpdateField(player, PanelTypes.Clock);
            }

            if (addPanels[(int)PanelTypes.Sleeping]) {
                for (int i = 0; i < BasePlayer.activePlayerList.Count; i++) {
                    UpdateFieldDelayed(BasePlayer.activePlayerList[i], PanelTypes.Sleeping);
                }
            }
        }

        /// <summary>Called when the player is attempting to respawn.</summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerRespawn")]
        private void OnPlayerRespawn(BasePlayer player) {
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++) {
                if (addPanels[(int)PanelTypes.Active]) {
                    UpdateField(BasePlayer.activePlayerList[i], PanelTypes.Active);
                }
                if (addPanels[(int)PanelTypes.Sleeping]) {
                    UpdateField(BasePlayer.activePlayerList[i], PanelTypes.Sleeping);
                }
            }
        }

        /// <summary>Called after the player has disconnected from the server.</summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(BasePlayer player, string reason) {
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++) {
                if (addPanels[(int)PanelTypes.Active]) {
                    UpdateField(BasePlayer.activePlayerList[i], PanelTypes.Active);
                }
                if (addPanels[(int)PanelTypes.Sleeping]) {
                    UpdateField(BasePlayer.activePlayerList[i], PanelTypes.Sleeping);
                }
            }
        }
        #endregion
        
        #region InitializeUi
        /// <summary>Initializes the panel data into the static container variable.</summary>
        /// <param name="panelData">The panel data that should be initialized.</param>
        private void InitializeStaticContainer(PanelData panelData) {
            staticContainers[(int)panelData.panelType] = new CuiElementContainer();
            
            staticContainers[(int)panelData.panelType].Add(new CuiPanel {
                RectTransform = {
                    AnchorMin = panelData.backgroundRect[RectProperties.anchorMin].ToString(),
                    AnchorMax = panelData.backgroundRect[RectProperties.anchorMax].ToString(),
                    OffsetMin = panelData.backgroundRect[RectProperties.offset].ToString(),
                    OffsetMax = panelData.backgroundRect[RectProperties.offset].ToString()
                },
                Image = {
                    Color = panelData.backgroundImage[ImageProperties.color].ToString()
                },
                CursorEnabled = false
            }, ContainerParent, pluginName + panelData.panelType.ToString() + ContainerTypes.Static.ToString());

            string iconId = default(string);
            FileManager.fileDictionary.TryGetValue(panelData.panelType.ToString(), out iconId);

            staticContainers[(int)panelData.panelType].Add(new CuiPanel {
                RectTransform = {
                    AnchorMin = panelData.iconRect[RectProperties.anchorMin].ToString(),
                    AnchorMax = panelData.iconRect[RectProperties.anchorMax].ToString(),
                    OffsetMin = panelData.iconRect[RectProperties.offset].ToString(),
                    OffsetMax = panelData.iconRect[RectProperties.offset].ToString()
                },
                Image = {
                    Color = panelData.iconImage[ImageProperties.color].ToString(),
                    Png = iconId,
                    Sprite = "assets/content/textures/generic/fulltransparent.tga"
                },
                CursorEnabled = false
            }, pluginName + panelData.panelType.ToString() + ContainerTypes.Static.ToString(), pluginName + panelData.panelType.ToString() + ContainerTypes.Static.ToString() + "Icon");
        }

        /// <summary>Reinitializes all the static panels once their icons are downloaded.</summary>
        private void WaitForIconsDownloaded() {
            for (int i = 0; i < panelCount; i++) {
                if (!iconsPresentPrevious[i]) {
                    if (FileManager.fileDictionary.ContainsKey(((PanelTypes)i).ToString())) {
                        for (int j = 0; j < BasePlayer.activePlayerList.Count; j++) {
                            CuiHelper.DestroyUi(BasePlayer.activePlayerList[j], pluginName + ((PanelTypes)i).ToString() + ContainerTypes.Static.ToString());
                        }

                        InitializeStaticContainer(panelsData[i]);

                        for (int j = 0; j < BasePlayer.activePlayerList.Count; j++) {
                            CuiHelper.AddUi(BasePlayer.activePlayerList[j], staticContainers[i]);
                            UpdateField(BasePlayer.activePlayerList[j], (PanelTypes)i);
                        }

                        iconsPresentPrevious[i] = true;
                    }
                }
            }

            if (iconWaitTimerCount <= iconWaitTimerMax && FileManager.fileDictionary.Count < panelCount) {
                iconWaitTimerCount++;
                timer.Once(0.5f, () => {
                    WaitForIconsDownloaded();
                });
            }
        }
        #endregion

        #region UpdateUi
        /// <summary>Updates the player's panel.</summary>
        /// <param name="player">The player for who the panel should be updated.</param>
        /// <param name="panelData">The data of the panel that will be updated.</param>
        /// <param name="replacements">The text elements that will be replaced from the normal format.</param>
        /// <seealso cref="UpdateField"/>
        private void UpdateField(BasePlayer player, PanelData panelData, params StringUtilities.ReplacementData[] replacements) {
            CuiHelper.DestroyUi(player, pluginName + panelData.panelType.ToString() + ContainerTypes.Dynamic.ToString());

            CuiElementContainer container = new CuiElementContainer();

            // Tries to parse the font size 
            int fontSize = defaultTextComponent.FontSize;
            if (!int.TryParse(panelData.textText[TextProperties.fontSize].ToString(), out fontSize)) {
                PrintWarning("Could not succesfully parse " + panelData.textText[TextProperties.fontSize].ToString() + ", a value retrieved from the configuration file, as a font size. Returned a default value of " + fontSize + " instead.");
            }

            // Adds the text to the container.
            container.Add(new CuiLabel {
                RectTransform = {
                    AnchorMin = panelData.textRect[RectProperties.anchorMin].ToString(),
                    AnchorMax = panelData.textRect[RectProperties.anchorMax].ToString(),
                    OffsetMin = panelData.textRect[RectProperties.offset].ToString(),
                    OffsetMax = panelData.textRect[RectProperties.offset].ToString()
                },
                Text = {
                    Align = StringUtilities.ToTextAnchor(panelData.textText[TextProperties.align].ToString()),
                    Color = panelData.textText[TextProperties.color].ToString(),
                    Font = panelData.textText[TextProperties.font].ToString(),
                    FontSize = fontSize,
                    Text = StringUtilities.Replace(panelData.textText[TextProperties.text].ToString(), replacements)
                }
            }, ContainerParent, pluginName + panelData.panelType.ToString() + ContainerTypes.Dynamic.ToString());
            
            CuiHelper.AddUi(player, container);
        }

        /// <summary>Updates the player's panel.</summary>
        /// <param name="player">The player for who the panel should be updated.</param>
        /// <param name="panel">The panel type that should be updated.</param>
        private void UpdateField(BasePlayer player, PanelTypes panel) {
            List<StringUtilities.ReplacementData> replacements = new List<StringUtilities.ReplacementData>();
            switch (panel) {
                case PanelTypes.Active:
                    replacements.Add(new StringUtilities.ReplacementData(replacementPrefix + FieldTypes.PlayersActive.ToString().ToUpper() + replacementSufix, BasePlayer.activePlayerList.Count.ToString()));
                    replacements.Add(new StringUtilities.ReplacementData(replacementPrefix + FieldTypes.PlayerMax.ToString().ToUpper() + replacementSufix, ConVar.Server.maxplayers.ToString()));
                    break;
                case PanelTypes.Sleeping:
                    replacements.Add(new StringUtilities.ReplacementData(replacementPrefix + FieldTypes.PlayersSleeping.ToString().ToUpper() + replacementSufix, BasePlayer.sleepingPlayerList.Count.ToString()));
                    break;
                case PanelTypes.Clock:
                    replacements.Add(new StringUtilities.ReplacementData(replacementPrefix + FieldTypes.Time.ToString().ToUpper() + replacementSufix, TIME));
                    break;
            }
            UpdateField(player, panelsData[(int)panel], replacements.ToArray());
        }

        /// <summary>Updates the player's panel with a delay of one frame.</summary>
        /// <param name="player">The player for who the panel should be updated.</param>
        /// <param name="panel">The panel type that should be updated.</param>
        private void UpdateFieldDelayed(BasePlayer player, PanelTypes panelType) {
            NextFrame(() => {
                UpdateField(player, panelType);
            });
        }

        /// <summary>Start a function that repeats updating a panel for each active player.</summary>
        /// <param name="panelType">The panel that should be updated.</param>
        /// <param name="updateInterval">The time in between updating the panel in milliseconds.</param>
        private void StartRepeatingFieldUpdate(PanelTypes panelType, float updateInterval) {
            // Updates the time if the repeating panel is the clock.
            if (panelType == PanelTypes.Clock) {
                TIME = TIMERAW;
            }

            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++) {
                if (!BasePlayer.activePlayerList[i].IsSleeping()) {
                    UpdateField(BasePlayer.activePlayerList[i], panelType);
                }
            }

            timer.Once(updateInterval / 1000f, () => {
                StartRepeatingFieldUpdate(panelType, updateInterval);
            });
        }
        #endregion

        #region FileStorage
        /// <summary>Unity script component for adding a file storage system readable by clients of the server.</summary>
        private class FileManager : MonoBehaviour {
            /// <summary>Please use instance instead of this.</summary>
            /// <seealso cref="instance"/>
            private static FileManager _instance = null;
            /// <summary>The instance of this script, on first call it will create the component attached to the plugins own game object.</summary>
            public static FileManager instance {
                get {
                    if (_instance == null) {
                        _instance = UiPlus.gameObject.AddComponent<FileManager>();
                    }
                    return _instance;
                }
            }
            // Don't use the following. For some reason that breaks it...
            //public static FileManager instance = UiPlus.gameObject.AddComponent<FileManager>();

            /// <summary>The path leading to the data directory of the plugin.</summary>
            private static readonly string dataDirectoryPath = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + pluginName + Path.DirectorySeparatorChar;
            
            /// <summary>Dictionary containing the files by key.</summary>
            public static Dictionary<string, string> fileDictionary = new Dictionary<string, string>();

            /// <summary>Intializes the file into the file dictionary.</summary>
            /// <param name="key">The key by wich you will be able to request the file from the file dictionary.</param>
            /// <param name="uri">The path to the file.</param>
            /// <seealso cref="fileDictionary"/>
            public static void InitializeFile(string key, string uri) {
                StringBuilder uriBuilder = new StringBuilder();
                if (!uri.StartsWith("file:///") && !uri.StartsWith(("http://"))) {
                    uriBuilder.Append(dataDirectoryPath);
                }
                uriBuilder.Append(uri);
                instance.StartCoroutine(WaitForRequest(key, uriBuilder.ToString()));
            }
            
            /// <summary></summary>
            /// <param name="key"></param>
            /// <param name="uri"></param>
            /// <returns></returns>
            private static IEnumerator WaitForRequest(string key, string uri) {
                WWW www = new WWW(uri);
                yield return www;

                if (string.IsNullOrEmpty(www.error)) {
                    MemoryStream stream = new MemoryStream();
                    stream.Write(www.bytes, 0, www.bytes.Length);
                    if (!fileDictionary.ContainsKey(key)) {
                        fileDictionary.Add(key, "");
                    }
                    fileDictionary[key] = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue).ToString();
                }
            }
        }
        #endregion

        #region Utilities
        /// <summary>A helper class for vectors.</summary>
        private class VectorUtilities {
            /// <summary>Turns a string in any number of float values.</summary>
            /// <param name="s">The string that will be read.</param>
            /// <param name="seperator">By which character the values are seperated</param>
            /// <returns>An array filled with the split values.</returns>
            public static float[] ToFloatArray(string s, params char[] seperator) {
                string[] stringArray = s.Split(seperator);

                float[] floatArray = new float[stringArray.Length];
                for (int i = 0; i < stringArray.Length; i++) {
                    floatArray[i] = 0;
                    if (!float.TryParse(stringArray[i], out floatArray[i])) {
                        instance.PrintWarning("Could not succesfully parse " + stringArray[i] + ", a value retrieved from the configuration file, as a float value. Returned a default value of " + 0.ToString() + " instead.");
                    }
                }
                return floatArray;
            }

            /// <summary>Turns a string into a Vector2</summary>
            /// <param name="s">The string that will be read.</param>
            /// <param name="seperator">By which character the values are seperated</param>
            /// <returns>A Vector2 with the split values.</returns>
            public static Vector2 ToVector2(string s, params char[] seperator) {
                float[] vectors = ToFloatArray(s, seperator);
                return new Vector2(vectors[0], vectors[1]);
            }

            /// <summary>Multiplies each value of a vector with each other. x*x, y*y.</summary>
            /// <param name="a">Vector A</param>
            /// <param name="b">Vector B</param>
            /// <returns>The input vectors multiplied with eachother.</returns>
            public static Vector2 Multiply(Vector2 a, Vector2 b) {
                return new Vector2(a.x * b.x, a.y * b.y);
            }
        }

        /// <summary>A helper class for the string type.</summary>
        private class StringUtilities {
            /// <summary>A struct able to container data for replacing parts of a string with one another.</summary>
            public struct ReplacementData {
                /// <summary>The part of the string that will be replaced.</summary>
                public readonly string from;
                /// <summary>The part of the string that will be added.</summary>
                public readonly string to;

                /// <summary>Constructor call for the ReplacementData struct</summary>
                /// <param name="from">The part of the string that will be replaced.</param>
                /// <param name="to">The part of the string that will be added.</param>
                public ReplacementData(string from, string to) {
                    this.from = from;
                    this.to = to;
                }
            }

            /// <summary>Reverses the characters in the string.</summary>
            /// <param name="s">The string to be reversed.</param>
            /// <returns>The newly reversed string.</returns>
            public static string Reverse(string s) {
                char[] charArray = s.ToCharArray();
                Array.Reverse(charArray);
                return new string(charArray);
            }

            /// <summary>Makes sure the number is transformed into a string having the given amount of characters.</summary>
            /// <param name="s">The string to fill or trim.</param>
            /// <param name="targetCharCount">The target amount of characters that make up the string.</param>
            /// <returns>The newly edited string.</returns>
            public static string FillTrim(string s, int targetCharCount, char fillChar = '0') {
                if (s.Length > 0 && s.Length > targetCharCount) {
                    Reverse(Reverse(s).Remove(s.Length - targetCharCount));
                }
                else if (s.Length < targetCharCount) {
                    for (int i = 0; s.Length < targetCharCount; i++) {
                        s.Insert(0, fillChar.ToString());
                    }
                }
                return s;
            }

            /// <summary>Replaces the first string in the array with the second string in the array.</summary>
            /// <param name="s">The text in which you want to replace characters.</param>
            /// <param name="replacements">A string array with which part [0] will be replaced by part [1] of the text property.</param>
            /// <returns>The new string after applying the replacements.</returns>
            public static string Replace(string s, params ReplacementData[] replacements) {
                for (int i = 0; i < replacements.Length; i++) {
                    if (s.Contains(replacements[i].from)) {
                        s = s.Replace(replacements[i].from, replacements[i].to);
                    }
                }
                return s;
            }

            /// <summary>Simple function for turning a Vector2 into a string readable in Json.</summary>
            /// <param name="vector2">The vector to be transformed.</param>
            /// <param name="seperator">The character to devide the values by.</param>
            /// <returns>The string based off the parsed vectors.</returns>
            public static string ToString(Vector2 vector2, char seperator = ' ') {
                return vector2.x.ToString() + seperator + vector2.y.ToString(); 
            }

            /// <summary>Converts a string into the correct ImageType.</summary>
            /// <param name="imageTypeString">The ImageType as a string.</param>
            /// <returns>The ImageType corresponding to the input string. Default value is Simple.</returns>
            public static Image.Type ToImageType(string imageTypeString) {
                switch (imageTypeString.ToLower()) {
                    default:
                        instance.PrintWarning("Unable to convert string, " + imageTypeString + ", from the configuration file into an ImageType. Returned Simple as a default instead.");
                        return Image.Type.Simple;
                    case "filled":
                        return Image.Type.Filled;
                    case "simple":
                        return Image.Type.Simple;
                    case "sliced":
                        return Image.Type.Sliced;
                    case "tiled":
                        return Image.Type.Tiled;
                }
            }

            /// <summary>Converts a string into the correct TextAnchor.</summary>
            /// <param name="textAnchorString">The TextAnchor as a string.</param>
            /// <returns>The TextAnchor corresponding to the input string. Default value is MiddleCenter.</returns>
            public static TextAnchor ToTextAnchor(string textAnchorString) {
                switch (textAnchorString.ToLower()) {
                    default:
                        instance.PrintWarning("Unable to convert string, " + textAnchorString + ", from the configuration file into a TextAnchor. Returned MiddleCenter as a default instead.");
                        return TextAnchor.MiddleCenter;
                    case "lowercenter":
                        return TextAnchor.LowerCenter;
                    case "lowerleft":
                        return TextAnchor.LowerLeft;
                    case "lowerright":
                        return TextAnchor.LowerRight;
                    case "middlecenter":
                        return TextAnchor.MiddleCenter;
                    case "middleleft":
                        return TextAnchor.MiddleLeft;
                    case "middleright":
                        return TextAnchor.MiddleRight;
                    case "uppercenter":
                        return TextAnchor.UpperCenter;
                    case "upperleft":
                        return TextAnchor.UpperLeft;
                    case "upperright":
                        return TextAnchor.UpperRight;
                }
            }
        }
        #endregion
    }
}