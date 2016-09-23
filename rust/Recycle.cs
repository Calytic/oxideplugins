using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;
using System.Text;

namespace Oxide.Plugins
{
    [Info("Recycle", "Calytic", "2.0.1")]
    [Description("Recycle crafted items to base resources")]
    class Recycle : RustPlugin
    {
        #region Configuration

        private float cooldownMinutes;
        private float refundRatio;
        private string box;

        #endregion

        #region State

        private Dictionary<string, DateTime> recycleCooldowns = new Dictionary<string, DateTime>();

        class OnlinePlayer
        {
            public BasePlayer Player;
            public BasePlayer Target;
            public StorageContainer View;
            public List<BasePlayer> Matches;

            public OnlinePlayer(BasePlayer player)
            {
            }
        }

        public Dictionary<ItemContainer, ulong> containers = new Dictionary<ItemContainer,ulong>();

        [OnlinePlayers]
        Hash<BasePlayer, OnlinePlayer> onlinePlayers = new Hash<BasePlayer, OnlinePlayer>();

        #endregion

        #region Initialization

        protected override void LoadDefaultConfig()
        {
            Config["Settings","box"] = "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab";
            Config["Settings","cooldownMinutes"] = 5;
            Config["Settings","refundRatio"] = 0.5f;
            Config["VERSION"] = Version.ToString();
        }

        void Unloaded()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnlinePlayer onlinePlayer;
                if (onlinePlayers.TryGetValue(player, out onlinePlayer) && onlinePlayer.View != null)
                {
                    CloseBoxView(player, onlinePlayer.View);
                }
            }
        }

        void Loaded()
        {
            permission.RegisterPermission("recycle.use", this);
            LoadMessages();
            CheckConfig();

            cooldownMinutes = GetConfig("Settings","cooldownMinutes", 5f);
            box = GetConfig("Settings","box", "assets/prefabs/deployable/woodenbox/box_wooden.item.prefab");
            refundRatio = GetConfig("Settings", "refundRatio", 0.5f);
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

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Recycle: Complete", "Recycling <color=lime>{0}</color> to {1}% base materials:"},
                {"Recycle: Item", "    <color=lime>{0}</color> X <color=yellow>{1}</color>"},
                {"Recycle: Invalid", "Cannot recycle that!"},
                {"Denied: Permission", "You lack permission to do that"},
                {"Denied: Privilege", "You lack permission to do that"},
                {"Denied: Swimming", "You cannot do that while swimming"},
                {"Denied: Falling", "You cannot do that while falling"},
                {"Denied: Wounded", "You cannot do that while wounded"},
                {"Denied: Generic", "You cannot do that right now"},
                {"Cooldown: Seconds", "You are doing that too often, try again in a {0} seconds(s)."},
                {"Cooldown: Minutes", "You are doing that too often, try again in a {0} minute(s)."},
            }, this);
        }

        #endregion

        #region Oxide Hooks

        void OnPlayerInit(BasePlayer player)
        {
            onlinePlayers[player].View = null;
            onlinePlayers[player].Target = null;
            onlinePlayers[player].Matches = null;
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (onlinePlayers[player].View != null) {
                CloseBoxView(player, onlinePlayers[player].View);
            }
        }

        void OnPlayerLootEnd(PlayerLoot inventory) {
            BasePlayer player;
            if ((player = inventory.GetComponent<BasePlayer>()) == null)
                return;

            OnlinePlayer onlinePlayer;
            if (onlinePlayers.TryGetValue(player, out onlinePlayer) && onlinePlayer.View != null)
            {
                if (onlinePlayer.View == inventory.entitySource)
                {
                    CloseBoxView(player, (StorageContainer)inventory.entitySource);
                }
            }
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container.playerOwner is BasePlayer)
            {
                if (onlinePlayers.ContainsKey(container.playerOwner))
                {
                    BasePlayer owner = container.playerOwner;
                    if (containers.ContainsKey(container))
                    {
                        if (SalvageItem(owner, item))
                        {
                            item.Remove(0f);
                            item.RemoveFromContainer();
                        }
                        else
                        {
                            ShowNotification(owner, GetMsg("Recycle: Invalid", owner));
                            item.MoveToContainer(owner.inventory.containerMain);
                        }
                    }
                }
            }
        }

        #endregion

        #region Commands

        [ConsoleCommand("rec")]
        void ccRec(ConsoleSystem.Arg arg)
        {
            cmdRec(arg.connection.player as BasePlayer, arg.cmd.name, arg.Args);
        }

        [ChatCommand("rec")]
        void cmdRec(BasePlayer player, string command, string[] args)
        {
            string playerID = player.userID.ToString();

            if(!CanPlayerRecycle(player))
                return;

            if(cooldownMinutes > 0 && !player.IsAdmin()) {
                DateTime startTime;

                if(recycleCooldowns.TryGetValue(playerID, out startTime)) {
                    DateTime endTime = DateTime.Now;
                
                    TimeSpan span = endTime.Subtract(startTime);
                    if(span.TotalMinutes > 0 && span.TotalMinutes < Convert.ToDouble(cooldownMinutes)) {
                        double timeleft = System.Math.Round(Convert.ToDouble(cooldownMinutes) - span.TotalMinutes, 2);
                        if(span.TotalSeconds < 0) {
                            recycleCooldowns.Remove(playerID);
                        } 

                        if(timeleft < 1) {
                            double timelefts = System.Math.Round((Convert.ToDouble(cooldownMinutes)*60) - span.TotalSeconds);
                            SendReply(player, string.Format(GetMsg("Cooldown: Seconds", player), timelefts.ToString()));
                            return;
                        } else {
                            SendReply(player, string.Format(GetMsg("Cooldown: Minutes", player), System.Math.Round(timeleft).ToString()));
                            return;
                        }
                    } else {
                        recycleCooldowns.Remove(playerID);
                    }
                }
            }

            ShowBox(player, player);
        }

        #endregion

        #region Core Methods

        void ShowBox(BasePlayer player, BaseEntity target)
        {
            if(!recycleCooldowns.ContainsKey(player.userID.ToString())) {
                recycleCooldowns.Add(player.userID.ToString(), DateTime.Now);
            }
            var ply = onlinePlayers[player];
            if (ply.View == null)
            {
                OpenBoxView(player, target);
                return;
            }

            CloseBoxView(player, ply.View);
            timer.In(1f, () => OpenBoxView(player, target));
        }

        void HideBox(BasePlayer player)
        {
            player.EndLooting();
            var ply = onlinePlayers[player];
            if (ply.View == null)
            {
                return;
            }

            CloseBoxView(player, ply.View);
        }

        void OpenBoxView(BasePlayer player, BaseEntity targArg)
        {
            var pos = new Vector3(player.transform.position.x, player.transform.position.y-0.6f, player.transform.position.z);
            int slots = 1;
            var view = GameManager.server.CreateEntity(box,pos) as StorageContainer;
            view.transform.position = pos;


            if (!view) return;

            player.EndLooting();
            if(targArg is BasePlayer) {
                BasePlayer target = targArg as BasePlayer;
                ItemContainer container = new ItemContainer();
                container.playerOwner = player;
                container.ServerInitialize((Item) null, slots);
                if ((int) container.uid == 0)
                    container.GiveUID();
                

                if(!this.containers.ContainsKey(container)) {
                    this.containers.Add(container, player.userID);
                }

                view.enableSaving = false;
                view.Spawn();
                view.inventory = container;
                view.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                onlinePlayers[player].View = view;
                onlinePlayers[player].Target = target;
                timer.Once(0.1f, delegate() {
                    view.PlayerOpenLoot(player);
                });
                
            }
        }

        void CloseBoxView(BasePlayer player, StorageContainer view)
        {
            OnlinePlayer onlinePlayer;
            if (!onlinePlayers.TryGetValue(player, out onlinePlayer)) return;
            if (onlinePlayer.View == null) return;

            if(containers.ContainsKey(view.inventory)) {
                containers.Remove(view.inventory);
            }

            player.inventory.loot.containers = new List<ItemContainer>();
            view.inventory = new ItemContainer();

            if (player.inventory.loot.IsLooting()) {
                player.SendConsoleCommand("inventory.endloot", null);
            }


            onlinePlayer.View = null;
            onlinePlayer.Target = null;

            view.KillMessage();
        }

        bool SalvageItem(BasePlayer player, Item item)
        {
            var sb = new StringBuilder();

            var ratio = item.hasCondition ? (item.condition / item.maxCondition) : 1;

            sb.Append(string.Format(GetMsg("Recycle: Complete", player), item.info.displayName.english, (refundRatio * 100)));

            if(item.info.Blueprint == null) {
                return false;
            }

            foreach (var ingredient in item.info.Blueprint.ingredients)
            {
                var refundAmount = (double)ingredient.amount / item.info.Blueprint.amountToCreate;
                refundAmount *= item.amount;
                refundAmount *= ratio;
                refundAmount *= refundRatio;
                refundAmount = System.Math.Ceiling(refundAmount);
                if (refundAmount < 1) refundAmount = 1;

                var newItem = ItemManager.Create(ingredient.itemDef, (int)refundAmount);

                ItemBlueprint ingredientBp = ingredient.itemDef.Blueprint;
                if (item.hasCondition) newItem.condition = (float)System.Math.Ceiling(newItem.maxCondition * ratio);

                player.GiveItem(newItem);
                sb.AppendLine();
                sb.Append(string.Format(GetMsg("Recycle: Item", player), newItem.info.displayName.english, newItem.amount));
            }

            ShowNotification(player, sb.ToString());

            return true;
        }

        bool CanPlayerRecycle(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "recycle.use"))
            {
                SendReply(player, GetMsg("Denied: Permission", player));
                return false;
            }

            if (!player.CanBuild())
            {
                SendReply(player, GetMsg("Denied: Privilege", player));
                return false;
            }
            if (player.IsSwimming())
            {
                SendReply(player, GetMsg("Denied: Swimming", player));
                return false;
            }
            if (!player.IsOnGround())
            {
                SendReply(player, GetMsg("Denied: Falling", player));
                return false;
            }
            if (player.IsFlying())
            {
                SendReply(player, GetMsg("Denied: Falling", player));
                return false;
            }
            if (player.IsWounded())
            {
                SendReply(player, GetMsg("Denied: Wounded", player));
                return false;
            }

            var canRecycle = Interface.Call("CanRecycle", player);
            if (canRecycle != null)
            {
                if (canRecycle is string)
                {
                    SendReply(player, Convert.ToString(canRecycle));
                }
                else
                {
                    SendReply(player, GetMsg("Denied: Generic", player));
                }
                return false;
            }

            return true;
        }

        #endregion

        #region GUI

        public string jsonNotify = @"[{""name"":""NotifyMsg"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""color"":""0 0 0 0.89""},{""type"":""RectTransform"",""anchormax"":""0.99 0.94"",""anchormin"":""0.69 0.77""}]},{""name"":""MassText"",""parent"":""NotifyMsg"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{msg}"",""fontSize"":16,""align"":""UpperLeft""},{""type"":""RectTransform"",""anchormax"":""0.98 0.99"",""anchormin"":""0.01 0.02""}]},{""name"":""CloseButton{1}"",""parent"":""NotifyMsg"",""components"":[{""type"":""UnityEngine.UI.Button"",""color"":""0.95 0 0 0.68"",""close"":""NotifyMsg"",""imagetype"":""Tiled""},{""type"":""RectTransform"",""anchormax"":""0.99 1"",""anchormin"":""0.91 0.86""}]},{""name"":""CloseButtonLabel"",""parent"":""CloseButton{1}"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""X"",""fontSize"":5,""align"":""MiddleCenter""},{""type"":""RectTransform"",""anchormax"":""1 1"",""anchormin"":""0 0""}]}]";

        public void ShowNotification(BasePlayer player, string msg)
        {
            this.HideNotification(player);
            string send = jsonNotify.Replace("{msg}", msg);

            var obj = new Facepunch.ObjectList?(new Facepunch.ObjectList(send));
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", obj);
            timer.Once(3f, delegate()
            {
                this.HideNotification(player);
            });
        }

        public void HideNotification(BasePlayer player)
        {
            var obj = new Facepunch.ObjectList?(new Facepunch.ObjectList("NotifyMsg"));
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", obj);
        }

        #endregion

        #region HelpText
        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder()
               .Append("Recycle by <color=#ce422b>http://rustservers.io</color>\n")
               .Append("  ").Append("<color=\"#ffd479\">/rec</color> - Open recycle box").Append("\n");
            player.ChatMessage(sb.ToString());
        }
        #endregion

        #region Helper methods

        string GetMsg(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player == null ? null : player.UserIDString);
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

        #endregion
    }
}