using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RemoveAAA", "Tuntenfisch", "0.4.3", ResourceId = 1645)]
    [Description("Removes admin abuse announcements!")]
    class RemoveAAA : RustPlugin
    {
        #region Fields
        List<string> itemBlackList;
        #endregion

        #region Hooks
        /// <summary>
        /// Effectively, the entry point for this plugin.
        /// </summary>
        void OnServerInitialized()
        {
            LoadConfig();

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "missing permission", "You are missing the necessary permission to do that!" },
                { "invalid item", "Invalid item!" },
                { "couldn't give item", "Couldn't give item!" },
                { "black listed item", "Item is black listed!" },
                { "couldn't find player", "Couldn't find player!" }
            }, this);

            RegisterPermissions("give", "giveall", "givearm", "givebp", "givebpall", "giveid", "giveto");
        }

        /// <summary>
        /// Overrides the command handling system if a command has been entered that would issue an admin abuse announcement.
        /// </summary>
        /// <param name="arg"> The console argument containing information about the command.</param>
        /// <returns> An object that determines whether the command handling system should be overriden or not.</returns>
        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null) return null;

            string command = arg.cmd.name;

            // give
            if (command.Equals("give"))
            {
                if (!HasPermission(arg, "give"))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("missing permission", this));
                    }
                    return true;
                }
                BasePlayer player = arg.Player();
                if (!player) return true;

                Item item = ItemManager.CreateByPartialName(arg.GetString(0), 1);
                if (item == null)
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("invalid item", this));
                    }
                    return true;
                }
                if (itemBlackList.Contains(item.info.shortname))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("black listed item", this));
                    }
                    return true;
                }
                item.amount = arg.GetInt(1, 1);
                if (!player.inventory.GiveItem(item, null))
                {
                    item.Remove(0f);
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("couldn't give item", this));
                    }
                    return true;
                }
                player.Command("note.inv", new object[] { item.info.itemid, item.amount });
                Debug.Log(string.Concat(new object[] { "[admin] giving ", player.displayName, " ", item.amount, " x ", item.info.displayName.english }));
                return true;
            }

            // giveall
            else if (command.Equals("giveall"))
            {
                if (!HasPermission(arg, "giveall"))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("missing permission", this));
                    }
                    return true;
                }
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    Item item = ItemManager.CreateByPartialName(arg.GetString(0), 1);
                    if (item != null)
                    {
                        if (!itemBlackList.Contains(item.info.shortname))
                        {
                            item.amount = arg.GetInt(1, 1);
                            if (player.inventory.GiveItem(item, null))
                            {
                                player.Command("note.inv", new object[] { item.info.itemid, item.amount });
                                Debug.Log(string.Concat(new object[] { "[admin] giving ", player.displayName, " ", item.amount, " x ", item.info.displayName.english }));
                                return true;
                            }
                            else
                            {
                                item.Remove(0f);
                                if (arg.Player())
                                {
                                    arg.Player().ChatMessage(lang.GetMessage("couldn't give item", this));
                                }
                                return true;
                            }
                        }
                        else
                        {
                            if (arg.Player())
                            {
                                arg.Player().ChatMessage(lang.GetMessage("black listed item", this));
                            }
                            return true;
                        }
                    }
                    else
                    {
                        if (arg.Player())
                        {
                            arg.Player().ChatMessage(lang.GetMessage("invalid item", this));
                        }
                        return true;
                    }
                }
            }

            // givearm
            else if (command.Equals("givearm"))
            {
                if (!HasPermission(arg, "givearm"))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("missing permission", this));
                    }
                    return true;
                }
                BasePlayer player = arg.Player();
                if (!player) return true;

                Item item = ItemManager.CreateByItemID(arg.GetInt(0), 1, 0);
                if (item == null)
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("invalid item", this));
                    }
                    return true;
                }
                if (itemBlackList.Contains(item.info.shortname))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("black listed item", this));
                    }
                    return true;
                }
                item.amount = arg.GetInt(1, 1);

                if (!player.inventory.GiveItem(item, player.inventory.containerBelt))
                {
                    item.Remove(0f);
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("couldn't give item", this));
                    }
                    return true;
                }
                player.Command("note.inv", new object[] { item.info.itemid, item.amount });
                Debug.Log(string.Concat(new object[] { "[admin] giving ", player.displayName, " ", item.amount, " x ", item.info.displayName.english }));
                return true;
            }

            // givebp
            else if (command.Equals("givebp"))
            {
                if (!HasPermission(arg, "givebp"))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("missing permission", this));
                    }
                    return true;
                }
                BasePlayer player = arg.Player();
                if (!player) return true;

                ItemDefinition definition = ItemManager.FindItemDefinition(arg.GetInt(0, 0));
                if (definition == null)
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("invalid item", this));
                    }
                    return true;
                }
                if (itemBlackList.Contains(definition.shortname))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("black listed item", this));
                    }
                    return true;
                }
                player.blueprints.Unlock(definition);
                Debug.Log(string.Concat(new object[] { "[admin] ", player.displayName, " learning blueprint ", definition.displayName.english }));
                return true;
            }

            // givebpall
            else if (command.Equals("givebpall"))
            {
                if (!HasPermission(arg, "givebpall"))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("missing permission", this));
                    }
                    return true;
                }
                ItemDefinition definition = ItemManager.FindItemDefinition(arg.GetString(0));
                if (definition == null)
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("invalid item", this));
                    }
                    return true;
                }
                if (itemBlackList.Contains(definition.shortname))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("black listed item", this));
                    }
                    return true;
                }
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    player.blueprints.Unlock(definition);
                    Debug.Log(string.Concat(new string[] { "[admin] teaching ", player.displayName, " ", definition.displayName.english, " blueprint" }));
                }
                return true;
            }

            // giveid
            else if (command.Equals("giveid"))
            {
                if (!HasPermission(arg, "giveid"))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("missing permission", this));
                    }
                    return true;
                }
                BasePlayer player = arg.Player();
                if (!player) return true;

                Item item = ItemManager.CreateByItemID(arg.GetInt(0), 1, 0);
                if (item == null)
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("invalid item", this));
                    }
                    return true;
                }
                if (itemBlackList.Contains(item.info.shortname))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("black listed item", this));
                    }
                    return true;
                }
                item.amount = arg.GetInt(1, 1);

                if (!player.inventory.GiveItem(item, null))
                {
                    item.Remove(0f);
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("couldn't give item", this));
                    }
                    return true;
                }
                player.Command("note.inv", new object[] { item.info.itemid, item.amount });
                Debug.Log(string.Concat(new object[] { "[admin] giving ", player.displayName, " ", item.amount, " x ", item.info.displayName.english }));
                return true;
            }

            // giveto
            else if (command.Equals("giveto"))
            {
                if (!HasPermission(arg, "giveto"))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("missing permission", this));
                    }
                    return true;
                }
                BasePlayer player = BasePlayer.Find(arg.GetString(0));
                if (player == null)
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("couldn't find player", this));
                    }
                    return true;
                }

                Item item = ItemManager.CreateByPartialName(arg.GetString(1), 1);
                if (item == null)
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("invalid item", this));
                    }
                    return true;
                }
                if (itemBlackList.Contains(item.info.shortname))
                {
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("black listed item", this));
                    }
                    return true;
                }
                item.amount = arg.GetInt(2, 1);

                if (!player.inventory.GiveItem(item, null))
                {
                    item.Remove(0f);
                    if (arg.Player())
                    {
                        arg.Player().ChatMessage(lang.GetMessage("couldn't give item", this));
                    }
                    return true;
                }
                player.Command("note.inv", new object[] { item.info.itemid, item.amount });
                Debug.Log(string.Concat(new object[] { "[admin] giving ", player.displayName, " ", item.amount, " x ", item.info.displayName.english }));
                return true;
            }
            return null;
        }
        #endregion

        #region Config
        /// <summary>
        /// Provides the default values for each configuration file value for easy access.
        /// </summary>
        static class DefaultConfig
        {
            public static readonly Dictionary<string, ConfigValue> values = new Dictionary<string, ConfigValue>()
            {
                { "ItemBlackList",              new ConfigValue(GetItemBlackList(),                                                         "1. Data", "1.1 Item Black List") },
            };

            private static List<string> GetItemBlackList()
            {
                List<string> items = new List<string>()
                {
                    "flare",
                    "generator.wind.scrap"
                };
                return items;
            }
        }

        /// <summary>
        /// Wrapper for a config value.
        /// </summary>
        /// <remarks>
        /// Contains both, the config value and the corresponding path.
        /// </remarks>
        class ConfigValue
        {
            public object value { private set; get; }
            public string[] path { private set; get; }

            public ConfigValue(object value, params string[] path)
            {
                this.value = value;
                this.path = path;
            }
        }

        /// <summary>
        /// Responsible for getting a value from the configuration file.
        /// </summary>
        /// <typeparam name="T"> The type that should be returned.</typeparam>
        /// <param name="saveConfig"> Indicates whether the configuration file should be saved.</param>
        /// <param name="defaultValue"> The defaultValue that will be returned if the actual value cannot be found.</param>
        /// <param name="keys"> The keys pointing to the value which should be returned.</param>
        /// <returns> Returns either the defaultValue or the value of the configuration file associated with the provided keys.</returns>
        T GetConfig<T>(ref bool saveConfig, T defaultValue, params string[] keys)
        {
            object value = Config.Get(keys);

            // get the value associated with the provided keys and check if the value is valid
            if (!IsValueValid(value, defaultValue))
            {
                object[] objArray = new object[keys.Length + 1];
                for (int i = 0; i < keys.Length; i++)
                {
                    objArray[i] = keys[i];
                }
                objArray[keys.Length] = defaultValue;

                Config.Set(objArray);

                saveConfig = true;
                return defaultValue;
            }
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }

        /// <summary>
        /// Overload function for getting a value from the configuration file.
        /// </summary>
        /// <typeparam name="T"> The type that should be returned.</typeparam>
        /// <param name="saveConfig"> Indicates whether the configuration file should be saved.</param>
        /// <param name="configValue"> Holds information both about the actual config value and the corresponding path.</param>
        /// <returns> Returns either the defaultValue or the value of the configuration file associated with the provided keys.</returns>
        T GetConfig<T>(ref bool saveConfig, ConfigValue configValue)
        {
            string[] keys = configValue.path;
            object defaultValue = configValue.value;

            return GetConfig(ref saveConfig, (T)defaultValue, keys);
        }

        /// <summary>
        /// Checks if a value is valid by comparing the types of two objects and checking if both objects are of the same type or not.
        /// </summary>
        /// <param name="value"> The value that needs to be checked for validity.</param>
        /// <param name="defaultValue"> The defaultValue which is used for checking if the value is valid.</param>
        /// <returns> A bool indicating whether the value is valid.</returns>
        bool IsValueValid(object value, object defaultValue)
        {
            if (value == null) return false;

            Type type = value.GetType();
            Type defaultType = defaultValue.GetType();

            if (type != defaultType && type != typeof(double) && defaultType != typeof(float))
            {
                if (type.IsGenericType && defaultType.IsGenericType)
                {
                    if (type.GetGenericTypeDefinition() != defaultType.GetGenericTypeDefinition())
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks whether the configuration file contains unused keys and removes them.
        /// </summary>
        void CleanupConfig()
        {
            List<string[]> paths = new List<string[]>();

            // iterate over the current configuration file to find all existing paths
            IEnumerator enumerator = Config.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    KeyValuePair<string, object> pair = (KeyValuePair<string, object>)enumerator.Current;

                    if (pair.Value.GetType().IsGenericType && pair.Value.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>) && ((IDictionary)pair.Value).Count != 0)
                    {
                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                        foreach (DictionaryEntry entry in (IDictionary)pair.Value)
                        {
                            dictionary.Add((string)entry.Key, entry.Value);
                        }

                        foreach (KeyValuePair<string, object> pair2 in dictionary)
                        {
                            if (pair2.Value.GetType().IsGenericType && pair2.Value.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>) && ((IDictionary)pair.Value).Count != 0)
                            {
                                Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
                                foreach (DictionaryEntry entry in (IDictionary)pair2.Value)
                                {
                                    dictionary2.Add((string)entry.Key, entry.Value);
                                }

                                foreach (KeyValuePair<string, object> pair3 in dictionary2)
                                {
                                    if (char.IsDigit(pair3.Key[0]) && pair3.Key[1].Equals('.') && char.IsDigit(pair3.Key[2]) && pair3.Key[3].Equals('.') && char.IsDigit(pair3.Key[4]))
                                    {
                                        paths.Add(new string[3] { pair.Key, pair2.Key, pair3.Key });
                                    }
                                    else
                                    {
                                        paths.Add(new string[2] { pair.Key, pair2.Key });
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                paths.Add(new string[2] { pair.Key, pair2.Key });
                            }
                        }
                    }
                    else
                    {
                        paths.Add(new string[1] { pair.Key });
                    }
                }
            }
            finally
            {
                ((IDisposable)enumerator).Dispose();
            }

            // iterate over the found paths and determine whether they are part of the default configuration
            foreach (string[] path in paths)
            {
                int index = -1;

                foreach (ConfigValue value in DefaultConfig.values.Values)
                {
                    if (path.Length != value.path.Length) continue;

                    for (int j = 0; j < path.Length; j++)
                    {
                        if (path[j].Equals(value.path[j]))
                        {
                            index = j > index ? j : index;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                // if a path is not part of the default configuration remove it
                if (index < path.Length - 1)
                {
                    if (index == -1)
                    {
                        Config.Remove(path[index + 1]);
                    }
                    else
                    {
                        string[] strArray = new string[index + 1];
                        for (int j = 0; j < strArray.Length; j++)
                        {
                            strArray[j] = path[j];
                        }
                        ((Dictionary<string, object>)Config.Get(strArray)).Remove(path[index + 1]);
                    }
                }
            }
        }

        /// <summary>
        /// Responsible for creating a configuration file and populating it with the required default values.
        /// </summary>
        /// <remarks>
        /// This function is called automatically by oxide if no configuration file could be found.
        /// </remarks>
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating configuration file!");

            Config.Clear();

            foreach (ConfigValue value in DefaultConfig.values.Values)
            {
                object[] objArray = new object[value.path.Length + 1];
                for (int i = 0; i < value.path.Length; i++)
                {
                    objArray[i] = value.path[i];
                }
                objArray[value.path.Length] = value.value;

                Config.Set(objArray);
            }
            SaveConfig();
        }

        /// <summary>
        /// Responsible for loading the configuration file.
        /// </summary>
        /// <seealso cref="GetConfig{T}(T, string, string, string)"/>
        new void LoadConfig()
        {
            Puts("Loading configuration file!");

            bool saveConfig = false;

            itemBlackList = GetConfig<List<string>>(ref saveConfig, DefaultConfig.values["ItemBlackList"]);

            if (saveConfig)
            {
                CleanupConfig();

                PrintWarning("Updating configuration file!");

                SaveConfig();
            }
        }
        #endregion

        #region Permissions
        /// <summary>
        /// Responsible for registering multiple permissions at once.
        /// </summary>
        /// <param name="permissions"></param>
        void RegisterPermissions(params string[] permissions)
        {
            foreach (string permission in permissions)
            {
                this.permission.RegisterPermission(Title.ToLower() + "." + permission, this);
            }
        }

        /// <summary>
        /// Checks whether a user has a permission-
        /// </summary>
        /// <param name="arg"> The console argument that needs to be checked for permissions.</param>
        /// <param name="permission"> The permission that needs to be checked.</param>
        /// <returns> A bool specifying whether the user has the given permission or not.</returns>
        bool HasPermission(ConsoleSystem.Arg arg, string permission)
        {
            if (arg.cmd.isAdmin && arg.connection == null) return true;
            if (arg.connection != null) return this.permission.UserHasPermission(arg.connection.userid.ToString(), Title.ToLower() + "." + permission);
            return false;
        }
        #endregion
    }
}