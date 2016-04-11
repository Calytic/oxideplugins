using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{

    [Info("Tracker", "Wolfs Darker", "1.0.1", ResourceId = 1278)]
    class Tracker : RustPlugin
    {
        #region Variables
        /**
         * Invalid name message.
         */
        static String invalid_name = "Invalid Name! Try again!";
        /**
         * Multiple players found message.
         */
        static String multiple_players = "Multiple players found with that name! Try again!";
 
        /**
         * Handles the tracked item data.
         */ 
        public class TrackedItem
        {
            public String container_name;
            public String location;
            public int count;
        }

        #endregion

        #region Item filters

        /**
         * Returns a list of all players that has the item.
         */ 
        List<TrackedItem> findPlayerItems(String item, int min_amount)
        {
            var players = UnityEngine.Object.FindObjectsOfType<BasePlayer>();
            var items_found = new List<TrackedItem>();

            if (players.Length == 0)
            {
                return items_found;
            }

            List<Item> items = new List<Item>();
            TrackedItem t;

            foreach (BasePlayer player in players)
            {
                if (player != null)
                {
                    items.Clear();

                    items.AddRange(player.inventory.containerMain.itemList);
                    items.AddRange(player.inventory.containerBelt.itemList);
                    items.AddRange(player.inventory.containerWear.itemList);
                    t = new TrackedItem();

                    if (items.ToArray().Length > 0)
                    {
                        foreach (Item i in items)
                        {
                            if (i != null && i.info.displayName.english.ToLower().Equals(item.ToLower()))
                            {
                                t.container_name = player.displayName + (player.IsSleeping() ? "(Sleeping)" : "(Online)");
                                t.location = (int)player.transform.position.x + " " + (int)player.transform.position.y + " " + (int)player.transform.position.z;
                                t.count += i.amount;
                            }
                        }
                    }

                    if (t != null && t.count > 0 && t.count >= min_amount)
                    {
                        items_found.Add(t);
                    }
                }
            }

            return items_found;
        }

        /**
         * Returns a list of chests that has the item.
         */ 
        List<TrackedItem> findChestItems(String item, int min_amount)
        {
            var containers = UnityEngine.Object.FindObjectsOfType<StorageContainer>();
            var items_found = new List<TrackedItem>();

            if (containers.Length == 0)
            {
                Puts("There is no containers in game.");
                return items_found;
            }

            TrackedItem t;

            foreach (StorageContainer container in containers)
            {
                if (container != null && container.inventory != null && container.name.Contains("woodbox"))
                {
                    t = new TrackedItem();

                    if (container.inventory.itemList.ToArray().Length > 0)
                    {
                        foreach (Item i in container.inventory.itemList)
                        {
                            if (i != null && i.info.displayName.english.ToLower().Equals(item.ToLower()))
                            {
                                t.container_name = "A Chest";
                                t.location = (int)container.transform.position.x + " " + (int)container.transform.position.y + " " + (int)container.transform.position.z;
                                t.count += i.amount;
                            }
                        }
                    }

                    if (t != null && t.count > 0 && t.count >= min_amount)
                    {
                        items_found.Add(t);
                    }
                }
            }

            return items_found;
        }

        #endregion

        #region Hooks
        /**
         * Displays the total items count in game.
         */
        [ConsoleCommand("trackitemcount")]
        void cmdTrackItemCount(ConsoleSystem.Arg arg)
        {
            if (arg.Player() && !arg.Player().IsAdmin())
            {
                SendReply(arg, "You don't have permission to use this command.");
                return;
            }

            if (arg.Args.Length == 0)
            {
                Puts("Track Count items usage:");
                Puts("/trackitemcount item - Shows the amount of certain item in game.");
                return;
            }

            var item = arg.Args.Length > 0 ? arg.Args[0].ToLower() : "";

            if (!itemExist(item))
            {
                Puts("Looks like there is no such a item named '" + item + "'.");
                return;
            }

            List<TrackedItem> items_list = new List<TrackedItem>();

            items_list.AddRange(findChestItems(item, 0));
            items_list.AddRange(findPlayerItems(item, 0));

            int count = 0;

            foreach (TrackedItem t in items_list)
            {
                if(t != null)
                    count += t.count;
            }

            Puts("There is " + String.Format("{0:N0}", count) + " '" + item + "' in game.");
        }

        /**
         * Displays a list of all containers/players that has certain item.
         */ 
        [ConsoleCommand("trackitem")]
        void consoleTrackItem(ConsoleSystem.Arg arg)
        {

            if (arg.Player() && !arg.Player().IsAdmin())
            {
                SendReply(arg, "You don't have permission to use this command.");
                return;
            }

            if (arg.Args.Length == 0)
            {
                Puts("Track items usage:");
                Puts("/trackitem item optional:place optional:minamount");
                Puts("Places: chests / players / all ( Default: all )");
                return;
            }

            var item = arg.Args.Length > 0 ? arg.Args[0].ToLower() : "";
            var location = arg.Args.Length > 1 ? arg.Args[1].ToLower() : "all";
            int min_amount = 0;
            try
            {
                min_amount = arg.Args.Length > 2 ? int.Parse(arg.Args[2]) : 0;
            }
            catch (Exception e)
            {
                Puts("Invalind minimun amount! Try again!");
                return;
            }

            if (item.Length == 0 || location.Length == 0 || min_amount < 0 || (!location.Equals("all") && !location.Equals("players") && !location.Equals("chests")))
            {
                Puts("Wrong syntax! Try again!");
                return;
            }

            if (!itemExist(item))
            {
                Puts("Looks like there is no such a item named '" + item + "'.");
                return;
            }

            List<TrackedItem> items_list = new List<TrackedItem>();

            if (location.Equals("all"))
            {
                items_list.AddRange(findChestItems(item, min_amount));
                items_list.AddRange(findPlayerItems(item, min_amount));
            }
            else if (location.Equals("chests"))
                items_list.AddRange(findChestItems(item, min_amount));
            else
                items_list.AddRange(findPlayerItems(item, min_amount));

            if (items_list.ToArray().Length == 0)
            {
                Puts("There is no such item in game(No player or chest has it).");
                return;
            }

            TrackedItem t;

            Puts("'" + item + "'s found:");

            for (int i = 0; i < items_list.Count; i++)
            {
                t = items_list[i];
                if (t != null)
                {
                    Puts(t.container_name + " has " + String.Format("{0:N0}", t.count) + " at position: " + t.location);
                }
            }
        }

        #endregion

        #region Support methods

        /**
         * Checks if the item exist.
         */ 
        bool itemExist(String item)
        {
            foreach (ItemDefinition i in ItemManager.GetItemDefinitions())
            {
                if (i != null && i.displayName.english.ToLower().Equals(item))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

    }
}