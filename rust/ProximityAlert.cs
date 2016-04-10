using System.Collections.Generic;
using Oxide.Core;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ProximityAlert", "k1lly0u", "0.1.21", ResourceId = 1801)]
    class ProximityAlert : RustPlugin
    {
        #region Fields
        [PluginReference]
        Plugin Clans;
        [PluginReference]
        Plugin Friends;

        private static int playerLayer = UnityEngine.LayerMask.GetMask("Player (Server)");
        List<ProximityPlayer> playerList = new List<ProximityPlayer>();
        private Vector2 guiPos;
        private Vector2 guiDim;

        
        #endregion

        #region Functions
        void OnServerInitialized() => InitializePlugin();
        void Unload()
        {
            var objects = UnityEngine.Object.FindObjectsOfType(typeof(ProximityPlayer));
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
            playerList.Clear();
        }
        void OnPlayerInit(BasePlayer player) => InitializePlayer(player);
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (player.GetComponent<ProximityPlayer>())
                DestroyPlayer(player);
        }
        private void DestroyPlayer(BasePlayer player)
        {
            playerList.Remove(player.GetComponent<ProximityPlayer>());
            UnityEngine.Object.Destroy(player.GetComponent<ProximityPlayer>());
        }
        private void InitializePlugin()
        {
            RegisterMessages();
            permission.RegisterPermission("proximityalert.use", this);
            LoadVariables();
            guiPos = new Vector2(configData.GUI_X_Pos, configData.GUI_Y_Pos);
            guiDim = new Vector2(configData.GUI_X_Dim, configData.GUI_Y_Dim);
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }
        private void InitializePlayer(BasePlayer player)
        {
            if (player.GetComponent<ProximityPlayer>())
                DestroyPlayer(player);
            if (!permission.UserHasPermission(player.UserIDString, "proximityalert.use")) return;
            GetPlayer(player);
        }
        private void CheckDependencies()
        {
            if (Friends == null) PrintWarning($"FriendsAPI could not be found! Disabling friends feature");
            if (Clans == null) PrintWarning($"Clans could not be found! Disabling clans feature");
        }
        private void ProxCollisionEnter(BasePlayer player) => SendUI(player, lang.GetMessage("warning", this, player.UserIDString));
        private void ProxCollisionLeave(BasePlayer player) => SendUI(player, lang.GetMessage("clear", this, player.UserIDString));
        private bool PA_IsClanmate(ulong playerId, ulong friendId)
        {
            if (!Clans) return false;
            object playerTag = Clans?.Call("GetClanOf", playerId);
            object friendTag = Clans?.Call("GetClanOf", friendId);
            if (playerTag is string && friendTag is string)
                if (playerTag == friendTag) return true;
            return false;
        }
        private bool PA_IsFriend(ulong playerID, ulong friendID)
        {
            if (!Friends) return false;
            bool isFriend = (bool)Friends?.Call("IsFriend", playerID, friendID);
            return isFriend;
        }
        private void SendUI(BasePlayer player, string msg)
        {
            if (!GetPlayer(player).GUIDestroyed)
                timer.Once(3, () => SendUI(player, msg));
            else GetPlayer(player).UseUI(msg, guiPos, guiDim);
        }
        private ProximityPlayer GetPlayer(BasePlayer player)
        {
            ProximityPlayer p = player.GetComponent<ProximityPlayer>();
            if (p == null)
            {
                playerList.Add(player.gameObject.AddComponent<ProximityPlayer>());
                player.GetComponent<ProximityPlayer>().SetRadius(configData.TriggerRadius);
            }
            return player.GetComponent<ProximityPlayer>();
        }
        #endregion

        #region Chat Command
        [ChatCommand("prox")]
        private void cmdProx(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "proximityalert.use")) return;
            if (GetPlayer(player).Activated)
            {
                GetPlayer(player).Activated = false;
                SendReply(player, lang.GetMessage("deactive", this, player.UserIDString));
                return;
            }
            else
            {
                GetPlayer(player).Activated = true;
                SendReply(player, lang.GetMessage("active", this, player.UserIDString));                
            }
        }
        #endregion

        #region Player Class
        class ProximityPlayer : MonoBehaviour
        {
            private BasePlayer player;
            private List<ulong> inProximity = new List<ulong>();
            private float collisionRadius;
            public bool GUIDestroyed = true;
            public bool Activated = true;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                InvokeRepeating("UpdateTrigger", 2f, 2f);
            }
            public void SetRadius(float radius) => collisionRadius = radius;
            private void OnDestroy() => CancelInvoke("UpdateTrigger");
            private void UpdateTrigger()
            {
                if (!Activated) return;
                var colliderArray = Physics.OverlapSphere(player.transform.position, collisionRadius, playerLayer);
                var collidePlayers = new List<ulong>();
                var outProximity = new List<ulong>();

                var existingCount = inProximity.Count();

                foreach (Collider collider in colliderArray)
                {
                    var col = collider.GetComponentInParent<BasePlayer>();
                    if (col == null || col == player) continue;
                    if (IsClanmate(col) || IsFriend(col) || col.IsSleeping() || col.IsAdmin() || !col.IsAlive()) break;

                    collidePlayers.Add(col.userID);

                    if (!inProximity.Contains(col.userID))
                        inProximity.Add(col.userID);
                }

                if (inProximity.Count > existingCount)
                    EnterTrigger();

                foreach (var entry in inProximity)
                    if (!collidePlayers.Contains(entry))
                        outProximity.Add(entry);

                foreach (var entry in outProximity)
                {
                    inProximity.Remove(entry);
                    if (inProximity.Count == 0)
                        LeaveTrigger();
                }
            }
            private bool IsClanmate(BasePlayer target)
            {
                object confirmed = Interface.CallHook("PA_IsClanmate", player.userID, target.userID);
                if (confirmed is bool)
                    if ((bool)confirmed)
                        return true;
                return false;
            }
            private bool IsFriend(BasePlayer target)
            {
                object confirmed = Interface.CallHook("PA_IsFriend", player.userID, target.userID);
                if (confirmed is bool)
                    if ((bool)confirmed)
                        return true;
                return false;
            }
            void EnterTrigger() => Interface.CallHook("ProxCollisionEnter", player);
            void LeaveTrigger() => Interface.CallHook("ProxCollisionLeave", player);            
            public void UseUI(string msg, Vector2 pos, Vector2 dim, int size = 20)
            {                
                GUIDestroyed = false;        
                Vector2 posMin = pos;
                Vector2 posMax = posMin + dim;

                var elements = new CuiElementContainer();
                CuiElement textElement = new CuiElement
                {
                    Name = "ProxWarn",
                    Parent = "HUD/Overlay",
                    FadeOut = 0.3f,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = msg,
                            FontSize = size,
                            Align = TextAnchor.MiddleCenter,
                            FadeIn = 0.3f
                        },
                        new CuiOutlineComponent
                        {
                            Distance = "1.0 1.0",
                            Color = "0.0 0.0 0.0 1.0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = posMin.x + " " + posMin.y,
                            AnchorMax = posMax.x + " " + posMax.y
                        }
                    }
                };
                elements.Add(textElement);
                CuiHelper.AddUi(player, elements);
                Invoke("DestroyNotification", 5f);
            }
            private void DestroyNotification()
            {
                CuiHelper.DestroyUi(player, "ProxWarn");
                GUIDestroyed = true;
            }
        }
        #endregion
        
        #region Config
        private ConfigData configData;
        class ConfigData
        {
            public float GUI_X_Pos { get; set; }
            public float GUI_X_Dim { get; set; }
            public float GUI_Y_Pos { get; set; }
            public float GUI_Y_Dim { get; set; }
            public float TriggerRadius { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                GUI_X_Pos = 0.2f,
                GUI_X_Dim = 0.6f,
                GUI_Y_Pos = 0.1f,
                GUI_Y_Dim = 0.2f,
                TriggerRadius = 50f
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        private void RegisterMessages() => lang.RegisterMessages(messages, this);
        #endregion

        #region Localization
        Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"warning", "<color=#cc0000>Caution!</color> There are players nearby!" },
            {"clear", "<color=#ffdb19>Clear!</color>" },
            {"active", "You have activated ProximityAlert" },
            {"deactive", "You have deactivated ProximityAlert" }
        };
        #endregion
    }
}
