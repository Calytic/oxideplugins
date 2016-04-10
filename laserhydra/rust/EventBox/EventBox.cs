using System.Collections.Generic;
using System.Linq;
using System;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EventBox API", "LaserHydra", "1.0.0", ResourceId = 0)]
    [Description("allows you to set up spots for boxes which other plugins can use.")]
    class EventBox : RustPlugin
    {
        class Category : Dictionary<string, object>
        {
            public Category(int MinimalAmount, int MaximalAmount, bool Enabled)
            {
                this.Add("Minimal Amount", MinimalAmount);
                this.Add("Maximal Amount", MaximalAmount);
                this.Add("Enabled", Enabled);
            }
        }

        class Location
        {
            public float x;
            public float y;
            public float z;

            public Location()
            {
            }

            internal Location(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            internal Location(Dictionary<string, float> dic)
            {
                this.x = dic["x"];
                this.y = dic["y"];
                this.z = dic["z"];
            }

            internal Location(Vector3 vector)
            {
                this.x = vector.x;
                this.y = vector.y;
                this.z = vector.z;
            }

            internal Vector3 Vector
            {
                get
                {
                    return new Vector3(this.x, this.y, this.z);
                }
            }

        }

        class Data
        {
            public Dictionary<string, List<Location>> locations = new Dictionary<string, List<Location>>();
        }

        Data data;

        Dictionary<string, List<BaseEntity>> boxes = new Dictionary<string, List<BaseEntity>>();

        ////////////////////////////////////////
        ///     Plugin Related
        ////////////////////////////////////////

        void Loaded()
        {
            permission.RegisterPermission("eventbox.use", this);

            LoadConfig();
            LoadData();
        }

        void Unloaded()
        {
            foreach(string name in names)
            {
                DestroyBoxes(name);
            }
        }

        ////////////////////////////////////////
        ///     Config Handling
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Categories", "Weapon", new Category(1, 2, true));
            SetConfig("Categories", "Construction", new Category(1, 5, true));
            SetConfig("Categories", "Items", new Category(1, 5, true));
            SetConfig("Categories", "Resources", new Category(500, 10000, false));
            SetConfig("Categories", "Attire", new Category(1, 2, true));
            SetConfig("Categories", "Tool", new Category(1, 2, true));
            SetConfig("Categories", "Medical", new Category(1, 5, true));
            SetConfig("Categories", "Food", new Category(5, 10, false));
            SetConfig("Categories", "Ammunition", new Category(5, 64, true));
            SetConfig("Categories", "Traps", new Category(1, 3, true));
            SetConfig("Categories", "Misc", new Category(1, 5, false));

            SetConfig("Settings", "Min Items", 1);
            SetConfig("Settings", "Max Items", 8);

            SetConfig("Settings", "Item Blacklist", new List<string> { "autoturret", "mining.quarry", "mining.pumpjack", "cctv.camera", "targeting.computer" });

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating new config file...");
        }

        ////////////////////////////////////////
        ///     Data Handling
        ////////////////////////////////////////

        void LoadData()
        {
            data = Interface.GetMod().DataFileSystem.ReadObject<Data>("EventBox_Data");
        }

        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("EventBox_Data", data);
        }

        ////////////////////////////////////////
        ///     Commands
        ////////////////////////////////////////

        [ChatCommand("eventbox")]
        void cmdEventBox(BasePlayer player, string cmd, string[] args)
        {
            if (!HasPermission(player))
            {
                SendChatMessage(player, "You have no permission to use this command.");
                return;
            }

            if(args.Length < 1)
            {
                SendChatMessage(player, "Syntax: /eventbox <add|build|destroy>");
                return;
            }

            switch(args[0])
            {
                case "add":
                    if(args.Length < 2)
                    {
                        SendChatMessage(player, "Syntax: /eventbox add <name>");
                        return;
                    }
                    string addname = args[1];

                    AddBox(addname, player.transform.position);
                    SendChatMessage(player, "Box has been added.");
                    break;

                case "build":
                    if (args.Length < 2)
                    {
                        SendChatMessage(player, "Syntax: /eventbox build <name>");
                        return;
                    }
                    string buildname = args[1];
                    
                    SendChatMessage(player, BuildBoxes(buildname));
                    break;

                case "destroy":
                    if (args.Length < 2)
                    {
                        SendChatMessage(player, "Syntax: /eventbox destroy <name>");
                        return;
                    }
                    string destroyname = args[1];

                    SendChatMessage(player, DestroyBoxes(destroyname));
                    break;
                default:
                    break;
            }
        }

        ////////////////////////////////////////
        ///     EventBox Related
        ////////////////////////////////////////

        List<string> names
        {
            get
            {
                return (from name in data.locations.Keys
                        select name).ToList();
            }
        }

        void AddBox(string name, Vector3 vector)
        {
            if (!data.locations.ContainsKey(name))
                data.locations.Add(name, new List<Location> { new Location(vector) });
            else
                data.locations[name].Add(new Location(vector));

            SaveData();
        }

        BaseEntity BuildBox(string name, Location location, bool items)
        {
            Vector3 vector = location.Vector;

            BaseEntity box = GameManager.server.CreateEntity("assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab", vector);
            box.Spawn(true);

            if (!boxes.ContainsKey(name))
                boxes.Add(name, new List<BaseEntity> { box });
            else
                boxes[name].Add(box);

            if(items) AddItems(box);

            return box;
        }

        void AddItems(BaseEntity box)
        {
            int itemCount = UnityEngine.Random.Range((int) Config["Settings", "Min Items"], (int) Config["Settings", "Max Items"]);

            StorageContainer container = box.GetComponent<StorageContainer>();

            if (container == null)
            {
                return;
            }

            for (int i = 1; i <= itemCount; i++)
            {
                Item item = GetRandomItem();

                if (item == null)
                {
                    i--;
                    continue;
                }

                item.MoveToContainer(container.inventory);
            }
        }

        string BuildBoxes(string name)
        {
            if (data.locations.ContainsKey(name))
            {
                foreach (Location location in data.locations[name])
                {
                    BuildBox(name, location, true);
                }

                return "Boxes have been built!";
            }
            else
                return "FAILED: Name does not exist!";
        }

        string DestroyBoxes(string name)
        {
            if (boxes.ContainsKey(name))
            {
                foreach (BaseEntity box in boxes[name])
                {
                    box.Kill(BaseNetworkable.DestroyMode.None);
                }

                boxes.Remove(name);
                return "Boxes have been destroyed!";
            }
            else
                return "FAILED: Boxes are not built!";
        }

        Item GetRandomItem()
        {
            Item item = null;
            switch("")
            {
                default:
                    
                    ItemDefinition info = ItemManager.itemList[UnityEngine.Random.Range(0, ItemManager.itemList.Count - 1)];
                    Dictionary<string, object> settings = Config["Categories", info.category.ToString()] as Dictionary<string, object>;

                    List<object> blacklist = Config["Settings", "Item Blacklist"] as List<object>;

                    if (settings == null)
                        return null;

                    if (!(bool)settings["Enabled"] || blacklist.Contains(info.shortname))
                        goto default;

                    int amount = (int) GetRandomAmount(info.category.ToString());

                    item = GetItem(info.itemid, amount);

                    break;
            }

            return item;
        }

        object GetRandomAmount(string category)
        {
            if (Config["Categories", category] == null)
                return null;

            Dictionary<string, object> settings = Config["Categories", category] as Dictionary<string, object>;

            if (settings == null)
                return null;

            int minAmount = Convert.ToInt32(settings["Minimal Amount"]);
            int maxAmount = Convert.ToInt32(settings["Maximal Amount"]) + 1;

            return UnityEngine.Random.Range(minAmount, maxAmount);
        }

        Item GetItem(int id, int amount)
        {
            Item item = ItemManager.CreateByItemID(id);
            item.amount = amount;

            return item;
        }

        ////////////////////////////////////////
        ///     Permission
        ////////////////////////////////////////

        bool HasPermission(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "eventbox.use"))
                return true;
            return false;
        }

        ////////////////////////////////////////
        ///     Console Command Handling
        ////////////////////////////////////////

        void RunAsChatCommand(ConsoleSystem.Arg arg, Action<BasePlayer, string, string[]> command)
        {
            if (arg == null) return;

            BasePlayer player = null;
            string cmd = string.Empty;
            string[] args = new string[0];

            if (arg.HasArgs()) args = arg.Args;
            if (arg.connection.player == null) return;

            player = arg.connection.player as BasePlayer;
            cmd = arg.cmd?.name ?? "unknown";

            command(player, cmd, args);
        }

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        ////////////////////////////////////////
        ///     Config Setup
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        ////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => PrintToChat(msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);
    }
}
