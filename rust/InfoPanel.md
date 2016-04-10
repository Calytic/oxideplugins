This is a small information panel.
Features:

- In-Game / Server Time.

- Online Player counter

- Sleeper Counter

- Message box

- Airdrop alert

- Helicopter alert

- Radiation alert

- Coordinates

- Full Customization

- Custom Panels: Simple text and Icon

- Api
Chat commands:


* /ipanel - Show the available commands

* /ipanel hide - Hide the InfoPanel

* /ipanel show - Show the InfoPanel

* /ipanel clock game - The clock will show the in-game time.

* /ipanel clock server <+/-hours> - The clock will show the RL time. You can add or remove hours.

* /ipanel timeformat - Show the available time formats.

* /ipanel timeformat <number> - Select your favorite time format from the list.

Configuration:


* Available: (Default: true), With this option u can turn on or off a panel.

* Dock: (Default: BottomPanel) , With this option u can choose the dock panel.

* Order:  With this option u can set the order of the panels. (Panels with the same dock and AnchorX)
* AnchorX: (Default: Left), You can pull the panel to the left or right side of the dock. (Left/Right)

* AnchorY: (Default: Bottom), You can pull the panel to the top or bottom of the dock/screen. (Top/Bottom)

* Width: Panel width. (0-1)
* Height: Panel height. (0-1)
* Margin: (Default: 0 0 0 0.005)  Panel margin (Top,Right,Bottom,Left)

* Autoload: (Default: true) If u turn this off (false). The panel not will be displayed automatically. But other plugins can trigger it.

API:

private bool PanelRegister( string PluginName, string PanelName, string json )


This function will load your panel. For the first time it will create a new entry into the InfoPanel config file under the ThirdPartyPanels section.

After that the InfoPanel will load the panel config from there.

If you use the Text or Image in your config. They will be automatically  named. (PluginName + Text or PluginName + Image)

For example: MyPanelName -> MyPanelNameText or MyPanelNameImage
private bool ShowPanel(string PluginName, string PanelName,  string PlayerId = null )


Show the selected panel to everyone or certain player.
private bool HidePanel(string PluginName, string PanelName,  string PlayerId = null )


Hide the selected panel to everyone or certain player.
private bool RefreshPanel( string PluginName, string PanelName,  string PlayerId = null )


Refresh the panel to everyone or certain player.
private void SetPanelAttribute( string PluginName, string PanelName, string Attribute, string Value, string PlayerId = null )


Change a panel attribute for everyone or or certain player.
private bool SendPanelInfo( string PluginName, List<string> Panels )


You can send a list of your panel names to the InfoPanel. The differences between this list and the config file will be removed from the config file to keep it clean.
private bool IsPlayerGUILoaded( string PlayerId )


You can check the player GUI status.
API Example:

````
using System;

using System.Collections.Generic;

using Oxide.Core.Plugins;

namespace Oxide.Plugins

{

    [Info("ApiTest", "Ghosst", "1.0.0")]

    [Description("OnlinePlayers Counter.")]

    public class ApiTest : RustPlugin

    {

        Timer CounterTimer;

        Timer BlinkTimer;

        Timer RandPTimer;

        int Count = 0;

        bool IsActive = true;

        string RandomPlayerID;

        bool RandomPlayername = false;

        bool Blinker = false;

        bool CounterP = false;

        List<string> Panels = new List<string> { "BlinkPanel", "CounterPanel", "RandomPlayernamePanel" };

        [PluginReference]

        Plugin InfoPanel;

        void Loaded()

        {

            if(InfoPanel)

            {

                InfoPanelInit();

            }

        }

        void OnPluginLoaded(Plugin InfoPanel)

        {

            if (InfoPanel.Title == "InfoPanel")

            {

                InfoPanelInit();

            }

        }

        public void InfoPanelInit()

        {

            //Send Panel names to the infopanel.

            InfoPanel.Call("SendPanelInfo", "ApiTest", Panels);

            AddRandomPlayerNamePanel();

            AddBlinkPanel();

            AddCounterPanel();

            if (CounterTimer == null & CounterP)

            {

                CounterTimer = timer.Repeat(5, 0, () => Counter());

            }

            if (BlinkTimer == null & Blinker)

            {

                BlinkTimer = timer.Repeat(5, 0, () => Blink());

            }

            if (RandPTimer == null & RandomPlayername)

            {

                RandPTimer = timer.Repeat(5, 0, () => RandomPlayer());

            }

     

        }

        /// <summary>

        /// Load the panel

        /// </summary>

        public void AddRandomPlayerNamePanel()

        {

            RandomPlayername = (bool)InfoPanel.Call("PanelRegister", "ApiTest", "RandomPlayernamePanel", RndPlayerNameCfg);

        }

        /// <summary>

        /// Load the panel.

        /// Show the panel everyone.

        /// </summary>

        public void AddBlinkPanel()

        {

            Blinker = (bool)InfoPanel.Call("PanelRegister", "ApiTest", "BlinkPanel", BlinkPCfg);

            InfoPanel.Call("ShowPanel", "ApiTest", "BlinkPanel");

        }

        /// <summary>

        /// Load the panel.

        /// Show the panel everyone.

        /// </summary>

        public void AddCounterPanel()

        {

            CounterP = (bool)InfoPanel.Call("PanelRegister", "ApiTest", "CounterPanel", CounterPCfg);

            InfoPanel.Call("ShowPanel", "ApiTest", "CounterPanel");

        }

        /// <summary>

        /// Hide or show the panel

        /// </summary>

        public void Blink()

        {

            if(IsActive)

            {

                IsActive = false;

                InfoPanel.Call("HidePanel", "ApiTest", "BlinkPanel");

            }

            else

            {

                IsActive = true;

                InfoPanel.Call("ShowPanel", "ApiTest", "BlinkPanel");

            }

        }

        /// <summary>

        /// Refresh the counter to every active player.

        /// </summary>

        public void Counter()

        {

            Count += 5;

            if (InfoPanel && CounterP)

            {

                InfoPanel.Call("SetPanelAttribute", "ApiTest", "CounterPanelText", "Content", Count.ToString());

                InfoPanel.Call("SetPanelAttribute", "ApiTest", "CounterPanelText", "FontColor", "0.6 0.1 0.1 1");

                InfoPanel.Call("RefreshPanel", "ApiTest", "CounterPanel");

            }

        }

        /// <summary>

        /// Show his name to a random player. But just only for him.

        /// </summary>

        public void RandomPlayer()

        {

            if(RandomPlayerID != null)

                InfoPanel.Call("HidePanel", "ApiTest", "RandomPlayernamePanel", RandomPlayerID);

            if(BasePlayer.activePlayerList.Count > 0)

            {

                var rand = new System.Random();

                BasePlayer player = BasePlayer.activePlayerList[rand.Next(BasePlayer.activePlayerList.Count)];

                RandomPlayerID = player.UserIDString;

                if (InfoPanel && RandomPlayername)

                {

                    InfoPanel.Call("SetPanelAttribute", "ApiTest", "RandomPlayernamePanelText", "Content", player.displayName, RandomPlayerID);

                    InfoPanel.Call("SetPanelAttribute", "ApiTest", "RandomPlayernamePanelText", "FontColor", "0.2 0.3 0.5 1", RandomPlayerID);

                    InfoPanel.Call("ShowPanel", "ApiTest", "RandomPlayernamePanel", RandomPlayerID);

                }

            }

     

        }

        /*

            Example Configs. Theres is no required option.

        */

        string RndPlayerNameCfg = @"

        {

            ""Autoload"": false,

            ""AnchorX"": ""Left"",

            ""AnchorY"": ""Bottom"",

            ""Available"": true,

            ""BackgroundColor"": ""0.1 0.1 0.1 0.4"",

            ""Dock"": ""BottomPanel"",

            ""Width"": 0.07,

            ""Height"": 0.95,

            ""Margin"": ""0 0 0 0.005"",

            ""Order"": 0,

            ""Image"": {

              ""AnchorX"": ""Left"",

              ""AnchorY"": ""Bottom"",

              ""Available"": true,

              ""BackgroundColor"": ""0.1 0.1 0.1 0.3"",

              ""Dock"": ""BottomPanel"",

              ""Height"": 0.8,

              ""Margin"": ""0 0.05 0.1 0.05"",

              ""Order"": 1,

              ""Url"": ""http://i.imgur.com/dble6vf.png"",

              ""Width"": 0.22

            },     

            ""Text"": {

              ""Align"": ""MiddleCenter"",

              ""AnchorX"": ""Left"",

              ""AnchorY"": ""Bottom"",

              ""Available"": true,

              ""BackgroundColor"": ""0.1 0.1 0.1 0.3"",

              ""Dock"": ""BottomPanel"",

              ""FontColor"": ""1 1 1 1"",

              ""FontSize"": 14,

              ""Content"": ""APITest Bottom"",

              ""Height"": 1.0,

              ""Margin"": ""0 0 0 0"",

              ""Order"": 2,

              ""Width"": 0.63

            },

        }";

        string BlinkPCfg = @"{}";

        string CounterPCfg = @"

        {

            ""Dock"": ""TopPanel"",

            ""Text"": {

                ""Content"": ""APITest Top""

            }

        }";

    }

}
````

Soon™:
0.8.x:


* AnchorX: center
* Fixed position