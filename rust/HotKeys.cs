using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("HotKeys", "Calytic", "0.0.2", ResourceId = 2135)]
    class HotKeys : RustPlugin
    {
        Dictionary<string, object> keys;

        Dictionary<string, string> defaultRustBinds = new Dictionary<string, string>()
        {
            {"f1", "consoletoggle"},
            {"backquote", "consoletoggle"},
            {"f7", "bugreporter"},
            {"w", "+forward"},
            {"s", "+backward"},
            {"a", "+left"},
            {"d", "+right"},
            {"mouse0", "+attack"},
            {"mouse1", "+attack2"},
            {"mouse2", "+attack3"},
            {"1", "+slot1"},
            {"2", "+slot2"},
            {"3", "+slot3"},
            {"4", "+slot4"},
            {"5", "+slot5"},
            {"6", "+slot6"},
            {"7", "+slot7"},
            {"8", "+slot8"},
            {"leftshift", "+sprint"},
            {"rightshift", "+sprint"},
            {"leftalt", "+altlook"},
            {"r", "+reload"},
            {"space", "+jump"},
            {"leftcontrol", "+duck"},
            {"e", "+use"},
            {"v", "+voice"},
            {"t", "chat.open"},
            {"return", "chat.open"},
            {"mousewheelup", "+invnext"},
            {"mousewheeldown", "+invprev"},
            {"tab", "inventory.toggle "},
        };

        void Loaded()
        {
            CheckConfig();
            keys = GetConfig("Settings", "Keys", GetDefaultKeys());

            BindAll();
        }

        void OnPlayerInit(BasePlayer player)
        {
            BindKeys(player);
        }

        [ConsoleCommand("hotkey.bind")]
        private void ccHotKeyBind(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null && arg.connection.authLevel < 1)
            {
                return;
            }

            if (arg.Args.Length == 1)
            {
                string keyCombo = arg.Args[0].Trim();
                if(keys.ContainsKey(keyCombo)) {
                    SendReply(arg, keyCombo + ": " + keys[keyCombo].ToString());
                    SaveBinds();
                    BindAll();
                } else {
                    SendReply(arg, "[HotKeys] No such binding");
                }
            } else if(arg.Args.Length == 2) {
                string keyCombo = arg.Args[0].Trim();
                string bind = arg.Args[1].Trim();

                if (keys.ContainsKey(keyCombo))
                {
                    SendReply(arg, "[HotKeys] Replaced " + keyCombo + ": " + bind);
                    keys[keyCombo] = bind;
                }
                else
                {
                    SendReply(arg, "[HotKeys] Bound " + keyCombo + ": " + bind);
                    keys.Add(keyCombo, bind);
                }

                SaveBinds();
                BindAll();
            }
            else
            {
                SendReply(arg, "[HotKeys] Invalid Syntax. hotkey.bind \"keyCombo\" [bind]");
            }
        }

        [ConsoleCommand("hotkey.unbind")]
        private void ccHotKeyUnbind(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null && arg.connection.authLevel < 1)
            {
                return;
            }

            if (arg.Args.Length == 1)
            {
                string keyCombo = arg.Args[0].Trim();

                if (keys.ContainsKey(keyCombo))
                {
                    string bind = keys[keyCombo].ToString();
                    keys.Remove(keyCombo);
                    if (defaultRustBinds.ContainsKey(keyCombo))
                    {
                        SendReply(arg, "[HotKeys] Reverted " + keyCombo + ": " + defaultRustBinds[keyCombo]);
                    }
                    else
                    {
                        SendReply(arg, "[HotKeys] Unbound " + keyCombo + ": " + bind);
                    }
                    
                    SaveBinds();
                    UnbindAll(keyCombo);
                }
            }
            else
            {
                SendReply(arg, "[HotKeys] Invalid Syntax. hotkey.unbind \"keyCombo\"");
            }
        }

        void BindAll()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                BindKeys(player);
            }
        }

        void UnbindAll(string keyCombo)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                UnbindKey(player, keyCombo);
            }
        }

        void BindKeys(BasePlayer player)
        {
            foreach (KeyValuePair<string, object> kvp in keys)
            {
                player.SendConsoleCommand("bind " + kvp.Key + " " + kvp.Value.ToString());
            }
        }

        void UnbindKey(BasePlayer player, string keyCombo)
        {
            string defaultRustBind = "";
            if (defaultRustBinds.ContainsKey(keyCombo))
            {
                defaultRustBind = defaultRustBinds[keyCombo];
            }
            player.SendConsoleCommand("bind " + keyCombo + " \"" + defaultRustBind + "\"");
        }

        void SaveBinds()
        {
            Config["Settings", "Keys"] = keys;
            Config.Save();
        }

        void LoadDefaultConfig()
        {
            Config["Settings", "Keys"] = GetDefaultKeys();

            Config["VERSION"] = Version.ToString();
        }

        void CheckConfig()
        {
            if (Config["VERSION"] == null)
            {
                // FOR COMPATIBILITY WITH INITIAL VERSIONS WITHOUT VERSIONED CONFIG
                ReloadConfig();
            }
            else if (GetConfig<string>("VERSION", "") != Version.ToString())
            {
                // ADDS NEW, IF ANY, CONFIGURATION OPTIONS
                ReloadConfig();
            }
        }

        protected void ReloadConfig()
        {
            Config["VERSION"] = Version.ToString();

            // NEW CONFIGURATION OPTIONS HERE
            // END NEW CONFIGURATION OPTIONS

            PrintToConsole("Upgrading configuration file");
            SaveConfig();
        }

        Dictionary<string, object> GetDefaultKeys()
        {
            return new Dictionary<string, object>()
            {
                {"i", "inventory.toggle"},
                {"c", "duck"},
                {"z", "+attack;+duck"},
                {"f", "forward;sprint"},
            };
        }

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        private T GetConfig<T>(string name, string name2, T defaultValue)
        {
            if (Config[name, name2] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name, name2], typeof(T));
        }
    }
}
