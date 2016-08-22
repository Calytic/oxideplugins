
using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Inventory Viewer", "Mughisi", "2.0.2", ResourceId = 871)]
    public class InventoryViewer : RustPlugin
    {

        private string prefix = "Inspector";

        private string prefixColor = "#008000ff";

        private const string Permission = "inventoryviewer.allowed";

        private static InventoryViewer instance;

        private readonly Dictionary<BasePlayer, List<BasePlayer>> activeMatches = new Dictionary<BasePlayer, List<BasePlayer>>();

        public class Inspector : MonoBehaviour
        {
            private BasePlayer player;

            private BasePlayer target;

            private LootableCorpse view;

            private int ticks;

            private readonly MethodInfo markDirty = typeof(PlayerLoot).GetMethod("MarkDirty", BindingFlags.NonPublic | BindingFlags.Instance);

            private readonly FieldInfo positionChecks = typeof(PlayerLoot).GetField("PositionChecks", BindingFlags.NonPublic | BindingFlags.Instance);

            public void StartInspecting(BasePlayer p, BasePlayer t)
            {
                player = p;
                target = t;

                var corpse = GameManager.server.CreateEntity("assets/prefabs/player/player_corpse.prefab") as BaseCorpse;
                if (corpse == null) return;
                corpse.parentEnt = null;
                corpse.transform.position = new Vector3(player.transform.position.x, -100, player.transform.position.z);
                corpse.CancelInvoke("RemoveCorpse");

                view = corpse as LootableCorpse;

                if (view == null) return;

                var source = new[] { target.inventory.containerMain, target.inventory.containerWear, target.inventory.containerBelt };
                view.containers = new ItemContainer[source.Length];
                for (var i = 0; i < source.Length; i++)
                {
                    view.containers[i] = source[i];
                    view.containers[i].playerOwner = target;
                }

                view.playerName = $"Inspecting {target.displayName}";
                view.playerSteamID = 0;
                view.enableSaving = false;
                view.Spawn();
                view.GetComponentInChildren<Rigidbody>().useGravity = false;

                BeginLooting();

                InvokeRepeating("Inspect", 0f, 0.1f);
            }

            private void Inspect()
            {
                ticks++;
                if (!player.inventory.loot.IsLooting()) BeginLooting();
                if (target.IsDead())
                {
                    instance.SendChatMessage(player, instance.GetTranslation("TargetDied", player.UserIDString));
                    StopInspecting();
                }
                if (!player.isConnected) return;
                player.inventory.loot.SendImmediate();
                player.SendNetworkUpdate();
            }

            public void StopInspecting()
            {
                if (ticks < 5 && !target.IsDead()) return;
                CancelInvoke("Inspect");
                StopLooting();
                for (var i = 0; i < view.containers.Length; i++) view.containers[i] = new ItemContainer();
                view.Kill();
                Remove();
            }

            private void BeginLooting()
            {
                if (target.IsDead()) return;
                player.inventory.loot.Clear();
                positionChecks.SetValue(player.inventory.loot, false);
                player.inventory.loot.entitySource = view;
                player.inventory.loot.itemSource = null;
                markDirty.Invoke(player.inventory.loot, null);

                view.SetFlag(BaseEntity.Flags.Open, true);

                foreach (var container in view.containers)
                    player.inventory.loot.containers.Add(container);

                player.inventory.loot.SendImmediate();
                view.ClientRPCPlayer(null, player, "RPC_ClientLootCorpse");
                player.SendNetworkUpdate();

                ticks = 0;
            }

            private void StopLooting()
            {
                markDirty.Invoke(player.inventory.loot, null);

                if (player.inventory.loot.entitySource)
                    player.inventory.loot.entitySource.SendMessage("PlayerStoppedLooting", player, SendMessageOptions.DontRequireReceiver);

                foreach (var container in player.inventory.loot.containers)
                    if (container != null)
                        container.onDirty -= (Action)Delegate.CreateDelegate(typeof(Action), player.inventory.loot, "MarkDirty");

                player.inventory.loot.containers.Clear();
                player.inventory.loot.entitySource = null;
                player.inventory.loot.itemSource = null;
            }

            public void Remove()
            {
                Destroy(this);
            }
        }

        private void Loaded()
        {
            instance = this;
            LoadConfigValues();
            LoadDefaultMessages();
            permission.RegisterPermission(Permission, this);
        }

        protected override void LoadDefaultConfig() => Puts("New configuration file generated.");

        private void LoadConfigValues()
        {
            prefix = GetConfig("Prefix", prefix);
            prefixColor = GetConfig("PrefixColor", prefixColor);
        }

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "NotAllowed", "You are not allowed to use this command." },
                { "NoPlayersFound", "Couldn't find any players matching that name." },
                { "MultiplePlayersFound", "Multiple players found with that name, please select one of these players by using '/viewinv list <number>':" },
                { "InvalidSelection", "Invalid number, use the number in front of the player's name. Use '/viewinv list' to check the list of players again." },
                { "InvalidArguments", "Invalid argument(s) supplied! Use '/viewinv <name>' or '/viewinv list <number>'." },
                { "NoListAvailable", "You do not have a players list available, use '/viewinv <name>' instead." },
                { "TargetDied", "The player you were looting died." }
            }, this);
        }

        private void Unload()
        {
            var inspectors = UnityEngine.Object.FindObjectsOfType<Inspector>();
            foreach (var inspector in inspectors)
                inspector.StopInspecting();
        }

        private void OnPlayerLootEnd(PlayerLoot looter)
        {
            looter.GetComponentInParent<BasePlayer>()?.GetComponent<Inspector>()?.StopInspecting();
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var view = entity as LootableCorpse;
            if (view == null) return;
            if (!view.playerName.StartsWith("Inspecting")) return;
            info.damageTypes.ScaleAll(0f);
        }

        [ChatCommand("viewinv")]
        private void ViewInventoryCommand(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowed(player.UserIDString))
            {
                SendChatMessage(player, GetTranslation("NotAllowed", player.UserIDString));
                return;
            }

            if (args.Length < 1)
            {
                SendChatMessage(player, GetTranslation("InvalidArguments", player.UserIDString));
                return;
            }

            if (args[0] == "list")
            {
                int num;
                if (args.Length == 1) ShowMatches(player);
                else if (int.TryParse(args[1], out num)) ShowMatch(player, num);
            }
            else
            {
                var name = string.Join(" ", args);
                var players = FindPlayersByNameOrId(name);

                switch (players.Count)
                {
                    case 0:
                        SendChatMessage(player, GetTranslation("NoPlayersFound", player.UserIDString));
                        break;
                    case 1:
                        ViewInventory(player, players[0]);
                        break;
                    default:
                        SendChatMessage(player, GetTranslation("MultiplePlayersFound", player.UserIDString));
                        if (!activeMatches.ContainsKey(player)) activeMatches.Add(player, null);
                        activeMatches[player] = players;
                        ShowMatches(player);
                        break;

                }
            }
        }

        private void ViewInventory(BasePlayer player, BasePlayer target)
        {
            var inspector = player.gameObject.GetComponent<Inspector>();
            inspector?.StopInspecting();
            inspector = player.gameObject.AddComponent<Inspector>();
            inspector.StartInspecting(player, target);
        }

        private List<BasePlayer> FindPlayersByNameOrId(string nameOrId)
        {
            var matches = new List<BasePlayer>();

            foreach (var ply in BasePlayer.activePlayerList)
            {
                if (ply.displayName.ToLower().Contains(nameOrId.ToLower()) || ply.UserIDString == nameOrId)
                    matches.Add(ply);
            }

            foreach (var ply in BasePlayer.sleepingPlayerList)
            {
                if (ply.displayName.ToLower().Contains(nameOrId.ToLower()) || ply.UserIDString == nameOrId)
                    matches.Add(ply);
            }

            return matches;
        }

        private void ShowMatches(BasePlayer player)
        {
            if (!activeMatches.ContainsKey(player) || activeMatches[player] == null)
            {
                SendChatMessage(player, GetTranslation("NoListAvailable", player.UserIDString));
                return;
            }

            for (var i = 0; i < activeMatches[player].Count; i++)
                SendReply(player, $"{i + 1}. {activeMatches[player][i].displayName}");
        }

        private void ShowMatch(BasePlayer player, int num)
        {
            if (!activeMatches.ContainsKey(player) || activeMatches[player] == null)
            {
                SendChatMessage(player, GetTranslation("NoListAvailable", player.UserIDString));
                return;
            }

            if (num > activeMatches[player].Count)
            {
                SendChatMessage(player, GetTranslation("InvalidSelection", player.UserIDString));
                ShowMatches(player);
                return;
            }

            ViewInventory(player, activeMatches[player][num - 1]);
        }

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) Config[name] = defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        private string GetTranslation(string key, string id = null)
        {
            return lang.GetMessage(key, this, id);
        }

        private bool IsAllowed(string id)
        {
            return permission.UserHasPermission(id, Permission);
        }

        private void SendChatMessage(BasePlayer player, string message, string arguments = null)
        {
            PrintToChat(player, $"<color={prefixColor}>{prefix}</color>: {message}");
        }

    }
}
