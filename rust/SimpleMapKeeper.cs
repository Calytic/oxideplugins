using Oxide.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{

    [Info("SimpleMapKeeper", "CARNY666", "1.1.0", ResourceId = 1579)]
    class SimpleMapKeeper : RustPlugin
    {
        private Dictionary<BasePlayer, Item> mapStorage = new Dictionary<BasePlayer, Item>();

        #region localization/messages
        private void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {                
                {"mapSaved", "You have just saved your current map."},
                {"mapAutoSaved", "Your map will be automatically saved."},
                {"noMapInBelt", "You must have a map in your belt to save it."},
                {"mapRestored", "Your map has just been restored."},
                {"mapNotRestored", "Map was not restored."},
                {"informMapSave", "Your map is automatically saved, so long as it is/was equipped on your belt."}
            };
            lang.RegisterMessages(messages, this);
        }

        private string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
        #endregion

        #region events
        private void Init()
        {
            LoadDefaultMessages();
            timer.Repeat(10, 0, () => SaveAllPlayerMaps());
            //PrintToChat($"SimpleMapKeeper {Version.ToString()} initialized.."); ~ Removed as per suggestion by Wulf. 
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!playerAlreadyHasMap(player))
                if (restoreMap(player))
                    PrintToChat(player, GetMessage("mapRestored", player.UserIDString));
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container.playerOwner != null)
                if (playerAlreadyHasMap(container.playerOwner))
                    if (saveMap(container.playerOwner))
                        if (item.info.displayName.english == "Paper Map")
                            PrintToChat(container.playerOwner, GetMessage("mapSaved", container.playerOwner.UserIDString));
        }

        private void Loaded()
        {
            LoadDefaultMessages();
        }
        #endregion

        [ChatCommand("simplemap")]
        private void simplemap(BasePlayer player, string command, string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "save")
                {
                    if (saveMap(player))
                        PrintToChat(player, GetMessage("mapSaved", player.UserIDString));
                    else
                        PrintToChat(player, GetMessage("noMapInBelt", player.UserIDString));
                }

                if (args[0] == "restore")
                {
                    if (restoreMap(player))
                        PrintToChat(player, GetMessage("mapRestored", player.UserIDString));
                    else
                        PrintToChat(player, GetMessage("mapNotRestored", player.UserIDString));
                }
            }
            PrintToChat(player, GetMessage("informMapSave", player.UserIDString));
        }

        /// <summary>
        /// Iterates through all active players, tests for a map, removes the existsing reference and saves new reference. 
        /// Called from the timer.
        /// </summary>
        private void SaveAllPlayerMaps()
        {
            foreach(BasePlayer b in BasePlayer.activePlayerList)
            {
                try {
                    if (!playerAlreadyHasMap(b)) break;

                    var map = b.inventory.containerBelt.itemList.Where(x => x.info.displayName.english == "Paper Map").First();
                    if (map != null)
                    {
                        if (mapStorage.ContainsKey(b))
                            mapStorage.Remove(b);
                        else
                            PrintToChat(b, GetMessage("mapAutoSaved", b.UserIDString));

                        saveMap(b);                            
                    }
                } catch(System.Exception e)
                {
                    PrintToConsole($"SaveAllPlayerMaps Error: SAVING {b.displayName} {e.Message}");
                }
            }
        }

        /// <summary>
        /// Tests to see is passed BasePlayer's containerBelt contains a map.
        /// </summary>
        /// <param name="player">BasePlayer of whom to save map.</param>
        /// <returns>True if BasePlayer's container.</returns>
        private bool playerAlreadyHasMap(BasePlayer player)
        {
            try {
                if (player.inventory.containerBelt.itemList.Where(x => x.info.displayName.english == "Paper Map").Count() > 0)
                    return true;    
                return false;  // no map to save
            } catch (System.Exception e)
            {
                PrintToConsole($"playerAlreadyHasMap error: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves a reference to the passed BasePlayer's equipped map into dictionary object.
        /// </summary>
        /// <param name="player">BasePlayer of whom to save map.</param>
        /// <returns>True if success.</returns>
        private bool saveMap(BasePlayer player)
        {
            // test that player has map equipped
            if (player.inventory.containerBelt.itemList.Where(x => x.info.displayName.english == "Paper Map").Count() == 0) return false;
            
            // get the players first equipped map
            var map = player.inventory.containerBelt.itemList.Where(x => x.info.displayName.english == "Paper Map").First();

            // better not be null..
            if (map == null) return false;  // no map to save

            // if a maps been saved, remove it
            if (mapStorage.ContainsKey(player))
                mapStorage.Remove(player);

            // add the map
            mapStorage.Add(player, map);

            return mapStorage.ContainsKey(player);
        }

        /// <summary>
        /// Restores reference to BasePlayer's saved map in containerBelt.
        /// </summary>
        /// <param name="player">BasePlayer of whom to save map.</param>
        /// <returns>True if success.</returns>
        private bool restoreMap(BasePlayer player)
        {
            if (!mapStorage.ContainsKey(player)) return false;

            if (mapStorage[player] == null) return false;
            mapStorage[player].MoveToContainer(player.inventory.containerBelt);
            return true;
        }
    }
}