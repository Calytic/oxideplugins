// Reference: Newtonsoft.Json

using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Oxide.Plugins
{
    [Info("ObjectRemover", "Wolfs Darker", "2.1.7", ResourceId = 1213)]
    class ObjectRemover : RustPlugin
    {
        #region Variables
        /**
         * Configuration instance, handles all the data that will be written or loaded.
         */ 
        Configuration config = new Configuration();

        /**
         * Handles the permission instance.
         */
        private Permission permission = GetLibrary<Permission>();

        /**
         * Message: Called when removing objects ( Console )
         */ 
        static String removing_objects = "Removing Objects...";
        /**
         * Message: Called when the player has no authorization to that command ( In game / Console )
         */ 
        static String no_access = "You don't have access to that command.";
        /**
         * Cupboard's deployed name.
         */
        static String cupboard = "cupboard.tool";

        /**
         * Configuration class, where all data is stored.
         */ 
        public class Configuration
        {
            /**
             * List of building grades.
             */ 
            public Dictionary<BuildingGrade.Enum, bool> building_list;
            /**
             * List of deployables.
             */ 
            public Dictionary<String, String> deployables;

            /**
             * List of distances for each object.
             */ 
            public Dictionary<String, float> distances;

            /**
             * Permissions for commands.
             */ 
            public Dictionary<string, string> permissions = new Dictionary<string, string>();

            /**
             * Message displayed when the admin is counting the objects.
             */ 
            public string count_message;

            /**
             * Message displayed when the admin is removing the objects.
             */ 
            public string admin_removing;

            /**
             * Message displayed when the admin removed the objects.
             */
            public string admin_removed;

            /**
             * Message displayed when there is a timer in the remove action.
             */ 
            public string timer_removing;

            /**
            * Message's prefix.
            */
            public String prefix = "[<color=#ffbf00>Object Remover</color>]";

            public Configuration()
            {
                building_list = new Dictionary<BuildingGrade.Enum, bool>();
                deployables = new Dictionary<String, String>();
                distances = new Dictionary<String, float>();
            }
        }

        #endregion

        #region Configuration

        protected override void LoadDefaultConfig()
        {
            foreach (BuildingGrade.Enum grade in Enum.GetValues(typeof(BuildingGrade.Enum)))
            {
                if (grade != BuildingGrade.Enum.Count)
                    config.building_list.Add(grade, grade != BuildingGrade.Enum.TopTier ? true : false);
            }

            config.deployables.Add("furnace", "furnace_deployed");
            config.deployables.Add("largefurnace", "large_furnace_deployed");

            config.deployables.Add("chest", "woodbox_deployed");
            config.deployables.Add("largechest", "large_woodbox_deployed");

            config.deployables.Add("lantern", "lantern_deployed");
            config.deployables.Add("sleepingbag", "sleepingbag_leather_deployed");
            config.deployables.Add("campfire", "campfire_deployed");

            config.deployables.Add("researchtable", "researchtable_deployed");
            config.deployables.Add("repairbench", "repairbench_deployed");

            config.deployables.Add("wallexternal", "wall.external.high");
            config.deployables.Add("ladder", "ladder.wooden.wall.");

            config.deployables.Add("windturbine", "generator.wind.scrap");
            config.deployables.Add("quarry", "mining_quarry");
            config.deployables.Add("refinery", "refinery_small_deployed");
            config.deployables.Add("pumpjack", "pumpjack");

            config.deployables.Add("smallsign", "sign.small.wood");
            config.deployables.Add("mediumsign", "sign.medium.wood");
            config.deployables.Add("largesign", "sign.large.wood");
            config.deployables.Add("hugesign", "sign.huge.wood");

            config.deployables.Add("smallcatcher", "water_catcher_small");
            config.deployables.Add("largecatcher", "water_catcher_large");

            config.deployables.Add("corn", "corn.entity");
            config.deployables.Add("pumpkin", "pumpkin.entity");

            config.deployables.Add("building", "assets/bundled/prefabs/build/");

            config.deployables.Add("barricade", "/barricades/barricade.");

            config.deployables.Add("beartrap", "beartrap.prefab");
            config.deployables.Add("floorspike", "floor_spikes.prefab");
            config.deployables.Add("landmine", "landmine.prefab");

            config.timer_removing = "The admin will remove all {entity}s outside cupboard area in {time} seconds.";
            config.admin_removed = "Admin has removed {count} {entity}s from the map.";
            config.admin_removing = "Admin is removing all {entity} outside of cupboard range.";
            config.count_message = "There are {count} {entity}s outside of cupboard range.";

            config.permissions.Add("removecommand", "object.remove");
            config.permissions.Add("countcommand", "object.count");
            config.permissions.Add("deployablelist", "object.deployable");

            foreach (String s in config.deployables.Keys)
                config.distances.Add(s, 60f);

            Config["configuration"] = config;
            SaveConfig();
        }

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            try
            {
                LoadConfig();
                config = JsonConvert.DeserializeObject<Configuration>(JsonConvert.SerializeObject(Config["configuration"]).ToString());
            }
            catch (Exception ex)
            {
                Puts("OnServerInitialized failed: " + ex.Message);
            }
        }

        #endregion

        #region Commands

        /**
         * Command: Handles the action of counting/removing objects outside cupboard area(In game).
         */ 
        [ChatCommand("object")]
        void cmdObject(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 2)
            {
                SendReply(player, config.prefix + "Object command usage:");
                SendReply(player, "/object entity action optional:time");
                SendReply(player, "entity - 'all' or /deployable_list for 'deployable'");
                SendReply(player, "action - count/remove");
                SendReply(player, "time - time(in seconds) for the action to happen(Default: 1 second)");
                return;
            }

            String entity = args[0];
            bool count = args[1].Equals("count");
            int time = args.Length > 2 && !count ? int.Parse(args[2]) : 1;

            if (hasPermission(player, count ? "countcommand" : "removecommand"))
            {
                SendReply(player, no_access);
                return;
            }

            float distance = getDistance(entity);

            if (distance <= 0)
            {
                SendReply(player, "" + entity + "'s distance is invalid(Distance: " + distance + ")!");
                return;
            }

            if (!count && time > 1 && config.timer_removing.Length > 0)
            {
                PrintToChat(config.prefix + config.timer_removing.Replace("{entity}", entity).Replace("{time}", time.ToString()));
            }

            string message = config.prefix + config.count_message.Replace("{entity}", entity.Equals("all") ? "object" : entity);
            int amount = 0;
            string deployable;

            timer.Once(time, () =>
            {
                if (entity.Equals("all"))
                {
                    foreach (string key in config.deployables.Keys)
                    {
                        deployable = config.deployables[key];
                        distance = getDistance(key);
                        amount += handleObjects<BaseEntity>(key, distance, !count, deployable, true);
                    }
                }
                else
                {
                    if (config.deployables.TryGetValue(entity, out deployable))                    
                        amount = handleObjects<BaseEntity>(entity, distance, !count, deployable);                    
                    else
                        SendReply(player, config.prefix + "There is no support for " + entity + " yet!");
                }

                if (count)
                    SendReply(player, message.Replace("{count}", amount.ToString()));
            });
        }

        /**
         * Command: displays the deployables supported by the configuration's deployables list.
         */ 
        [ChatCommand("deployable_list")]
        void cmdDeploytList(BasePlayer player, string command, string[] args)
        {
            if (hasPermission(player, "deployablelist"))
            {
                SendReply(player, no_access);
                return;
            }

            String temp = "";

            foreach (String s in config.deployables.Keys)
                temp += s + "   ";

            SendReply(player, config.prefix + "Deployables avaliable: ");
            SendReply(player, temp);
        }

        /**
         * Command: displays the deployables supported by the configuration's deployables list.
         */ 
        [ConsoleCommand("deployable_list")]
        void consoleDeploytList(ConsoleSystem.Arg arg)
        {

            if (arg.Player() && hasPermission(arg.Player(), "deployablelist"))
            {
                SendReply(arg.Player(), no_access);
                return;
            }

            String temp = "";

            foreach (String s in config.deployables.Keys)
                temp += s + "   ";

            Puts("Deployables avaliable:");
            Puts(temp);
        }

        /**
         * Command: Handles the action of counting/removing objects outside cupboard area(Console).
         */
        [ConsoleCommand("object")]
        void consoleCmdObject(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length < 2)
            {
                Puts("Object command usage:");
                Puts("/object entity action optional:time");
                Puts("entity - 'all' or /deployable_list for 'deployable'");
                Puts("action - count/remove");
                Puts("time - time(in seconds) for the action to happen(Default: 1 second)");
                return;
            }

            String entity = arg.Args[0];
            bool count = arg.Args[1].Equals("count");
            int time = arg.Args.Length > 2 && !count ? int.Parse(arg.Args[2]) : 1;

            if (arg.Player() && hasPermission(arg.Player(), count ? "countcommand" : "removecommand"))
            {
                SendReply(arg.Player(), no_access);
                return;
            }

            float distance = getDistance(entity);

            if (distance <= 0)
            {
                Puts("" + entity + "'s distance is invalid(Distance: " + distance + ")!");
                return;
            }

            if (!count && time > 1 && config.timer_removing.Length > 0)
            {
                PrintToChat(config.prefix + config.timer_removing.Replace("{entity}", entity).Replace("{time}", time.ToString()));
            }

            string message = config.count_message.Replace("{entity}", entity.Equals("all") ? "object" : entity);
            int amount = 0;
            string deployable;

            timer.Once(time, () =>
            {
                if (entity.Equals("all"))
                {
                    foreach (string key in config.deployables.Keys)
                    {
                        deployable = config.deployables[key];
                        distance = getDistance(key);
                        amount += handleObjects<BaseEntity>(key, distance, !count, deployable, true);
                    }
                }
                else
                {
                    if (config.deployables.TryGetValue(entity, out deployable))                   
                        amount = handleObjects<BaseEntity>(entity, distance, !count, deployable);                   
                    else
                        Puts("There is no support for " + entity + " yet!");
                }

                if (count)
                    Puts(message.Replace("{count}", amount.ToString()));
            });
        }

        #endregion

        #region Main Methods

        /**
         * Removes every object outside a cupboard area.
         */
        int handleObjects<T>(String entity, float distance = 60f, bool remove = false, String name = "", bool all = false) where T : BaseEntity
        {
            if (remove)
            {
                PrintToChat(config.prefix + config.admin_removing.Replace("{entity}", entity));
                Puts(removing_objects);
            }
            var started_at = UnityEngine.Time.realtimeSinceStartup;
            var objects = getFilteredObjects<T>(entity, distance, name, all);

            if (remove)
            {
                foreach (var block in objects)
                    block.Kill();
                Puts(("Removed " + objects.Count() + " " + entity + "s in " + (int)(UnityEngine.Time.realtimeSinceStartup - started_at) + " seconds."));
                PrintToChat((config.prefix + config.admin_removed.Replace("{count}", objects.Count.ToString()).Replace("{entity}", entity)));
            }

            return objects.Count;
        }

        /*
         * Filters all the objects from all cupboards.
         */
        HashSet<T> getFilteredObjects<T>(string e, float distance = 60f, String name = "", bool all = false) where T : BaseEntity
        {
            var cupboards = UnityEngine.Object.FindObjectsOfType<BaseCombatEntity>().Where(entity => nameFilter(entity, cupboard)).ToArray();

            if (cupboards.Length == 0)
            {
                Puts("There is no cupboards in game.");
                return new HashSet<T>();
            }

            var blocks = new HashSet<T>(UnityEngine.Object.FindObjectsOfType<T>().Where(entity => buildingBlockTier(entity as BuildingBlock) && nameFilter(entity, name)));

            if (blocks.Count == 0)
            {
                Puts("There is no " + name + "s in game.");
                return new HashSet<T>();
            }

            Puts("Cupboards: " + cupboards.Length + ", " + e + "s: " + blocks.Count);

            foreach (var cup in cupboards)
            {
                foreach (var collider in Physics.OverlapSphere(cup.transform.position, distance))
                {
                    var building_block = collider.GetComponentInParent<T>();
                    if (building_block != null)
                    {
                        blocks.Remove(building_block);
                    }
                }
            }

            if(all)
                Puts(e + "s without cupboard: " + blocks.Count);

            return blocks;
        }

        #endregion

        #region Assist methods

        /**
         * Checks if the name of the entity matches the provided name.
         */
        bool nameFilter(BaseEntity entity, string name)
        {
            return name.Length == 0 || (entity.name.Contains(name) && !entity.name.Contains("/locks/"));
        }

        /**
         * Checks if the block has a removeable grade.
         */ 
        bool buildingBlockTier(BuildingBlock block)
        {
            return block == null || (config.building_list[block.grade] && block.grade != BuildingGrade.Enum.Count);
        }

        /**
         * Gets the distance from the object's name.
         */
        float getDistance(string name)
        {
            float temp = 0f;
            if (config.distances.TryGetValue(name, out temp))
                return temp;
            return 60f;
        }

        /**
         * Checks if the player has certain permission
         */
        bool hasPermission(BasePlayer player, string key)
        {
            string value = "";
            if (config.permissions.TryGetValue(key, out value))
                return permission.UserHasPermission(player.userID.ToString(), value);
            return false;
        }

        #endregion
    }
}