using Oxide.Game.Rust.Cui;
using Oxide.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Enhanced Hammer", "Visagalis", "0.4.9", ResourceId = 1439)]
    public class EnhancedHammer : RustPlugin
    {
        public class PlayerDetails
        {
            public PlayerFlags flags = PlayerFlags.MESSAGES_DISABLED;
            public BuildingGrade.Enum upgradeInfo = BuildingGrade.Enum.Count; // HAMMER
            public int backToDefaultTimer = 20;
        }

        private string pluginPrefix = "[Enhanced Hammer] ";
        public enum PlayerFlags
        {
            NONE = 0,
            ICONS_DISABLED = 2,
            PLUGIN_DISABLED = 4,
            MESSAGES_DISABLED = 8
        }

        public static Dictionary<ulong, PlayerDetails> playersInfo = new Dictionary<ulong, PlayerDetails>();
        public static Dictionary<ulong, Timer> playersTimers = new Dictionary<ulong, Timer>();

        void OnStructureRepair(BuildingBlock block, BasePlayer player)
        {
            if (PlayerHasFlag(player.userID, PlayerFlags.PLUGIN_DISABLED))
                return;

            


            if (playersInfo[player.userID].upgradeInfo == BuildingGrade.Enum.Count
                || playersInfo[player.userID].upgradeInfo <= block.currentGrade.gradeBase.type
                || !player.CanBuild())
            {
                if (playersInfo[player.userID].upgradeInfo != BuildingGrade.Enum.Count && playersInfo[player.userID].upgradeInfo <= block.currentGrade.gradeBase.type)
                {
                    if(!PlayerHasFlag(player.userID, PlayerFlags.MESSAGES_DISABLED))
                        SendReply(player, pluginPrefix + "You are now in REPAIR mode.");
                    playersInfo[player.userID].upgradeInfo = BuildingGrade.Enum.Count;
                    RenderMode(player, true);
                }
                else if (!player.CanBuild())
                {
                    SendReply(player, pluginPrefix + "Building is blocked!");
                }
            }
            else
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo dynMethod = block.GetType().GetMethod("CanChangeToGrade", flags);
                bool canChangeGrade = (bool)dynMethod.Invoke(block, new object[] { playersInfo[player.userID].upgradeInfo, player });

                if (!canChangeGrade)
                {
                    SendReply(player, pluginPrefix + "You can't upgrade it, something is blocking it's way.");
                    return;
                }

                if (block.name.ToLower().Contains("wall.external"))
                {
                    SendReply(player, pluginPrefix + "Can't upgrade walls! Switching to REPAIR mode.");
                    playersInfo[player.userID].upgradeInfo = BuildingGrade.Enum.Count;
                    return;
                }
                float currentHealth = block.health;
                var currentGradeType = block.currentGrade.gradeBase.type;
                block.SetGrade(playersInfo[player.userID].upgradeInfo);
                var TwigsDecay = plugins.Find("TwigsDecay");
                TwigsDecay?.Call("OnStructureUpgrade", block, player, playersInfo[player.userID].upgradeInfo);
				block.UpdateSkin(false);
                var cost = block.currentGrade.gradeBase.baseCost;
                int hasEnough = 0;
                foreach (var itemCost in cost)
                {
                    int itemCostAmount = Convert.ToInt32((float)itemCost.amount*block.blockDefinition.costMultiplier);
                    var foundItems = player.inventory.FindItemIDs(itemCost.itemid);
                    var amountFound = foundItems?.Sum(item => item.amount) ?? 0;
                    if (amountFound >= itemCostAmount)
                        hasEnough++;
                }
                if (hasEnough >= cost.Count)
                {
                    foreach (var itemCost in cost)
                    {
                        int itemCostAmount = Convert.ToInt32((float)itemCost.amount * block.blockDefinition.costMultiplier);
                        var foundItems = player.inventory.FindItemIDs(itemCost.itemid);
                        player.inventory.Take(foundItems, itemCost.itemid, itemCostAmount);
                    }
                    block.SetHealthToMax();
                    block.SetFlag(BaseEntity.Flags.Reserved1, true); // refresh rotation
                    block.Invoke("StopBeingRotatable", 600f);
                    Effect.server.Run("assets/bundled/prefabs/fx/build/promote_" + playersInfo[player.userID].upgradeInfo.ToString().ToLower() + ".prefab", block, 0u, Vector3.zero, Vector3.zero, null, false);
                }
                else
                {
                    block.SetGrade(currentGradeType);
                    TwigsDecay?.Call("OnStructureUpgrade", block, player, currentGradeType);
                    block.UpdateSkin(false);
                    block.health = currentHealth;
                    SendReply(player, pluginPrefix + "Can't afford to upgrade!");
                }
            }

            RefreshTimer(player);
        }

        void RefreshTimer(BasePlayer player)
        {
            if (playersInfo[player.userID].backToDefaultTimer == 0)
                return;

            if (playersTimers.ContainsKey(player.userID))
            {
                playersTimers[player.userID].Destroy();
                playersTimers.Remove(player.userID);
            }

            var timerIn = timer.Once(playersInfo[player.userID].backToDefaultTimer, () => SetBackToDefault(player));
            playersTimers.Add(player.userID, timerIn);
        }

        void OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (PlayerHasFlag(player.userID, PlayerFlags.PLUGIN_DISABLED))
                return;

            if (playersInfo[player.userID].upgradeInfo != grade)
            {
                playersInfo[player.userID].upgradeInfo = grade;
                RenderMode(player, false);
                if (!PlayerHasFlag(player.userID, PlayerFlags.MESSAGES_DISABLED))
                    SendReply(player, pluginPrefix + "You are now in UPGRADE mode. [" + grade.ToString() + "]");
            }

            RefreshTimer(player);
        }

        void RenderMode(BasePlayer player, bool repair = false)
        {
            CuiHelper.DestroyUi(player, "EnhancedHammerUI");
            if (PlayerHasFlag(player.userID, PlayerFlags.PLUGIN_DISABLED) || 
                PlayerHasFlag(player.userID, PlayerFlags.ICONS_DISABLED) || 
                (!repair && playersInfo[player.userID].upgradeInfo == BuildingGrade.Enum.Count))
                return;

            CuiElementContainer panel = new CuiElementContainer();
            string icon = "http://i.imgur.com/Nq6DNSX.png";
            if (!repair)
            {
                switch (playersInfo[player.userID].upgradeInfo)
                {
                    case BuildingGrade.Enum.Wood:
                        icon = "http://i.imgur.com/F4XBBhY.png";
                        break;
                    case BuildingGrade.Enum.Stone:
                        icon = "http://i.imgur.com/S7Sl9oh.png";
                        break;
                    case BuildingGrade.Enum.Metal:
                        icon = "http://i.imgur.com/fVjzbag.png";
                        break;
                    case BuildingGrade.Enum.TopTier:
                        icon = "http://i.imgur.com/f0WklR3.png";
                        break;
                }

            }
            CuiElement ehUI = new CuiElement { Name = "EnhancedHammerUI", Parent = "HUD/Overlay", FadeOut = 0.5f };
            CuiRawImageComponent ehUI_IMG = new CuiRawImageComponent { FadeIn = 0.5f, Url = icon };
            CuiRectTransformComponent ehUI_RECT = new CuiRectTransformComponent
            {
                AnchorMin = "0.32 0.09",
                AnchorMax = "0.34 0.13"
            };
            ehUI.Components.Add(ehUI_IMG);
            ehUI.Components.Add(ehUI_RECT);
            panel.Add(ehUI);
            CuiHelper.AddUi(player, panel);
        }

        void SetBackToDefault(BasePlayer player)
        {
            if(playersTimers.ContainsKey(player.userID))
                playersTimers.Remove(player.userID);
			if(playersInfo.ContainsKey(player.userID))
				playersInfo[player.userID].upgradeInfo = BuildingGrade.Enum.Count;
            RemoveUI(player);
            if (!PlayerHasFlag(player.userID, PlayerFlags.MESSAGES_DISABLED))
                SendReply(player, pluginPrefix + "You are now in REPAIR mode.");
        }

        void RemoveUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "EnhancedHammerUI");
        }

        void OnPlayerInit(BasePlayer player)
        {
            playersInfo.Add(player.userID, new PlayerDetails());
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (playersInfo.ContainsKey(player.userID))
                playersInfo.Remove(player.userID);
        }

        public PlayerFlags GetPlayerFlags(ulong userID)
        {
            if (playersInfo.ContainsKey(userID))
                    return playersInfo[userID].flags;

            return PlayerFlags.NONE;
        }

        void Init()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if(!playersInfo.ContainsKey(player.userID))
                    playersInfo.Add(player.userID, new PlayerDetails());
            }
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                RemoveUI(player);
                if (playersInfo.ContainsKey(player.userID))
                    playersInfo.Remove(player.userID);
            }
        }

        [ChatCommand("eh")]
        private void OnEhCommand(BasePlayer player, string command, string[] arg)
        {
            bool incorrectUsage = arg.Length == 0;
            bool ADD = true;
            bool REMOVE = false;
            if (arg.Length == 1)
            {
                switch (arg[0].ToLower())
                {
                    case "enable":
                        ModifyPlayerFlags(player, REMOVE, PlayerFlags.PLUGIN_DISABLED);
                        break;
                    case "disable":
                        ModifyPlayerFlags(player, ADD, PlayerFlags.PLUGIN_DISABLED);
                        break;
                    case "show":
                        ModifyPlayerFlags(player, REMOVE, PlayerFlags.ICONS_DISABLED);
                        break;
                    case "hide":
                        ModifyPlayerFlags(player, ADD, PlayerFlags.ICONS_DISABLED);
                        break;
                    default:
                        incorrectUsage = true;
                        break;
                }
                if (!incorrectUsage)
                    RenderMode(player);
            }
            else if (arg.Length == 2)
            {
                if (arg[0].ToLower() == "timer")
                {
                    int seconds;
                    if (int.TryParse(arg[1], out seconds) && seconds >= 0)
                    {
                        playersInfo[player.userID].backToDefaultTimer = seconds;
                        string msg = "";
                        if (seconds > 0)
                            msg += " Timer has been set to " + seconds + " seconds.";
                        else
                            msg += " Timer will never end.";
                        SendReply(player, pluginPrefix + msg);
                        incorrectUsage = false;
                    }
                }
                else if (arg[0].ToLower() == "msgs")
                {
                    if (arg[1].ToLower() == "show")
                        ModifyPlayerFlags(player, false, PlayerFlags.MESSAGES_DISABLED);
                    else if (arg[1].ToLower() == "hide")
                        ModifyPlayerFlags(player, true, PlayerFlags.MESSAGES_DISABLED);
                    else
                        incorrectUsage = true;
                }
            }

            if (incorrectUsage)
            {
                SendReply(player, "Command usage:");
                SendReply(player, "/eh [enable/disable] - Enables or disabled plugin functionality.");
                SendReply(player, "/eh [show/hide] - Shows or hides plugin icons.");
                SendReply(player, "/eh timer [0/seconds] - Time in which hammer goes back to default mode.");
                SendReply(player, "/eh msgs [show/hide] - Show messages in chat about hammer state.");
            }
        }

        private bool PlayerHasFlag(ulong userID, PlayerFlags flag)
        {
            return (GetPlayerFlags(userID) & flag) == flag;
        }

        private void ModifyPlayerFlags(BasePlayer player, bool addFlag, PlayerFlags flag)
        {
            bool actionCompleted = false;
            if (addFlag)
            {
                if ((playersInfo[player.userID].flags & flag) != flag)
                {
                    playersInfo[player.userID].flags |= flag;
                    actionCompleted = true;
                }
            }
            else
            {
                if ((playersInfo[player.userID].flags & flag) == flag)
                {
                    playersInfo[player.userID].flags &= ~flag;
                    actionCompleted = true;
                }
            }

            if (actionCompleted)
            {
                string msg = "";
                switch (flag)
                {
                    case PlayerFlags.ICONS_DISABLED:
                        msg += "ICONS";
                        break;
                    case PlayerFlags.PLUGIN_DISABLED:
                        msg += "PLUGIN";
                        break;
                    case PlayerFlags.MESSAGES_DISABLED:
                        msg += "MESSAGES";
                        break;
                }
                SendReply(player, pluginPrefix + msg + " has been " + (!addFlag? "ENABLED" : "DISABLED") + ".");
            }
        }
    }
}