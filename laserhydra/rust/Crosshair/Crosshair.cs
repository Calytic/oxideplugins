using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Crosshair", "LaserHydra", "2.1.1", ResourceId = 1236)]
    [Description("Adds a customizable crosshair to your screen")]
    class Crosshair : RustPlugin
    {
        List<ulong> enabled = new List<ulong>();
        List<BasePlayer> inventory = new List<BasePlayer>();

        #region Classes
        class UIColor
        {
            double red;
            double green;
            double blue;
            double alpha;

            public UIColor(double red, double green, double blue, double alpha)
            {
                this.red = red;
                this.green = green;
                this.blue = blue;
                this.alpha = alpha;
            }

            public override string ToString()
            {
                return $"{red.ToString()} {green.ToString()} {blue.ToString()} {alpha.ToString()}";
            }
        }

        class UIObject
        {
            List<object> ui = new List<object>();
            List<string> objectList = new List<string>();

            public void Draw(BasePlayer player) => CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(JsonConvert.SerializeObject(ui).Replace("{NEWLINE}", Environment.NewLine)));

            public void Destroy(BasePlayer player)
            {
                foreach (string uiName in objectList)
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList(uiName));
            }
            
            public string AddText(string name, double left, double top, double width, double height, UIColor color, string text, int textsize = 15, string parent = "HUD/Overlay", int alignmode = 0)
            {
                //name = name + RandomString();
                text = text.Replace("\n", "{NEWLINE}");
                string align = "";

                switch (alignmode)
                {
                    case 0: { align = "LowerCenter"; break; };
                    case 1: { align = "LowerLeft"; break; };
                    case 2: { align = "LowerRight"; break; };
                    case 3: { align = "MiddleCenter"; break; };
                    case 4: { align = "MiddleLeft"; break; };
                    case 5: { align = "MiddleRight"; break; };
                    case 6: { align = "UpperCenter"; break; };
                    case 7: { align = "UpperLeft"; break; };
                    case 8: { align = "UpperRight"; break; };
                }

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Text"},
                                {"text", text},
                                {"fontSize", textsize.ToString()},
                                {"color", color.ToString()},
                                {"align", align}
                            },
                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }
        }
        #endregion

        #region Plugin General

        ////////////////////////////////////////
        ///     Plugin Related Hooks
        ////////////////////////////////////////

        void Loaded()
        {
#if !RUST
            throw new NotSupportedException("This plugin or the version of this plugin does not support this game!");
#endif

            RegisterPerm("use");

            LoadData(ref enabled);
            LoadConfig();
            LoadMessages();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                player.SendConsoleCommand("bind tab inventory.toggle;toggle.crosshair");
                player.SendConsoleCommand("bind escape toggle.crosshair");
                
                if (HasEnabled(player))
                {
                    DestroyCrosshair(player);
                    DrawCrosshair(player);
                }
            }
        }

        void Unloaded()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                player.SendConsoleCommand("bind tab inventory.toggle");
                player.SendConsoleCommand("bind escape \"\"");

                DestroyCrosshair(player);
            }
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Crosshair", "Size", 20);
            SetConfig("Crosshair", "Symbol", "+");
            SetConfig("Color", "Red", 1f);
            SetConfig("Color", "Green", 0f);
            SetConfig("Color", "Blue", 0f);
            SetConfig("Color", "Alpha", 1f);

            SaveConfig();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Enabled", "You have enabled the crosshair."},
                {"Disabled", "You have disabled the crosshair."}
            }, this);
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");
        #endregion

        #region Commands

        [ChatCommand("crosshair")]
        void cmdCrosshair(BasePlayer player)
        {
            if(!HasPerm(player.userID, "use"))
            {
                SendChatMessage(player, GetMsg("No Permission"));
                return;
            }

            if (HasEnabled(player))
                Disable(player);
            else
                Enable(player);
        }

        [ConsoleCommand("toggle.crosshair")]
        void ToggleCrosshair(ConsoleSystem.Arg arg)
        {
            if (arg != null && arg?.connection != null && arg?.connection?.player != null)
            {
                BasePlayer player = (BasePlayer)arg.connection.player;

                if (inventory.Contains(player))
                {
                    DrawCrosshair(player);
                    inventory.Remove(player);
                }
                else
                {
                    DestroyCrosshair(player);
                    inventory.Add(player);
                }
            }
        }

        #endregion

        #region Subject Related

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (HasEnabled(player))
            {
                DestroyCrosshair(player);
                DrawCrosshair(player);
            }
        }

        /*void OnPlayerLootEnd(PlayerLoot loot)
        {
            if(loot.GetComponent<BasePlayer>() != null && HasEnabled(loot.GetComponent<BasePlayer>()))
            {
                DestroyCrosshair(loot.GetComponent<BasePlayer>());
                DrawCrosshair(loot.GetComponent<BasePlayer>());

                if (inventory.Contains(loot.GetComponent<BasePlayer>()))
                    inventory.Remove(loot.GetComponent<BasePlayer>());
            }
        }*/

        void OnLootPlayer(BasePlayer player)
        {
            if (HasEnabled(player))
            {
                if (!inventory.Contains(player))
                    inventory.Add(player);

                DestroyCrosshair(player);
            }
        }

        void OnLootItem(BasePlayer player)
        {
            if (HasEnabled(player))
            {
                if (!inventory.Contains(player))
                    inventory.Add(player);

                DestroyCrosshair(player);
            }
        }

        void OnLootEntity(BasePlayer player)
        {
            if (HasEnabled(player))
            {
                if (!inventory.Contains(player))
                    inventory.Add(player);

                DestroyCrosshair(player);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (HasEnabled(player))
                DrawCrosshair(player);

            player.SendConsoleCommand("bind tab inventory.toggle;toggle.crosshair");
            player.SendConsoleCommand("bind escape toggle.crosshair");
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (inventory.Contains(player))
                inventory.Remove(player);

            player.SendConsoleCommand("bind tab inventory.toggle");
            player.SendConsoleCommand("bind escape \"\"");
        }

        void DrawCrosshair(BasePlayer player)
        {
            string crosshair = GetConfig("+", "Crosshair", "Symbol");

            int size = GetConfig(20, "Crosshair", "Size");

            float red = GetConfig(1f, "Color", "Red");
            float green = GetConfig(0f, "Color", "Green");
            float blue = GetConfig(0f, "Color", "Blue");
            float alpha = GetConfig(1f, "Color", "Alpha");

            UIObject ui = new UIObject();

            ui.AddText("Crosshair", 0.475, 0.475, 0.05, 0.05, new UIColor(red, green, blue, alpha), crosshair, size, "HUD/Overlay", 3);

            ui.Draw(player);
        }

        void DestroyCrosshair(BasePlayer player)
        {
            UIObject ui = new UIObject();
            
            ui.AddText("Crosshair", 0, 0, 0, 0, new UIColor(0, 0, 0, 0), "", 0, "HUD/Overlay", 4);

            ui.Destroy(player);
        }

        bool HasEnabled(BasePlayer player) => enabled.Contains(player.userID);

        void Enable(BasePlayer player)
        {
            if (!enabled.Contains(player.userID))
            {
                enabled.Add(player.userID);
                DrawCrosshair(player);

                SaveData(ref enabled);

                SendChatMessage(player, GetMsg("Enabled"));
            }
        }

        void Disable(BasePlayer player)
        {
            if (enabled.Contains(player.userID))
            {
                enabled.Remove(player.userID);
                DestroyCrosshair(player);

                SaveData(ref enabled);

                SendChatMessage(player, GetMsg("Disabled"));
            }
        }

        #endregion

        #region General Methods

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString<T>(List<T> list, int first, string seperator) => string.Join(seperator, (from item in list select item.ToString()).Skip(first).ToArray());

        ////////////////////////////////////////
        ///     Config Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        ////////////////////////////////////////
        ///     Data Related
        ////////////////////////////////////////

        void LoadData<T>(ref T data, string filename = "?") => data = Core.Interface.Oxide.DataFileSystem.ReadObject<T>(filename == "?" ? this.Title : filename);

        void SaveData<T>(ref T data, string filename = "?") => Core.Interface.Oxide.DataFileSystem.WriteObject(filename == "?" ? this.Title : filename, data);

        ////////////////////////////////////////
        ///     Message Related
        ////////////////////////////////////////

        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());

        ////////////////////////////////////////
        ///     Permission Related
        ////////////////////////////////////////

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        bool HasPerm(object uid, params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        string PermissionPrefix
        {
            get
            {
                return this.Title.Replace(" ", "").ToLower();
            }
        }

        ////////////////////////////////////////
        ///     Chat Related
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        #endregion
    }
}
