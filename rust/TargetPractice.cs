using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Configuration;
using Oxide.Core;
using System.Linq;
using Oxide.Game.Rust.Cui;
using System.Reflection;



namespace Oxide.Plugins
{
    [Info("TargetPractice", "k1lly0u", "0.1.51", ResourceId = 1731)]
    class TargetPractice : RustPlugin
    {
        TargetData shotData;
        private DynamicConfigFile ShotData;

        private static Vector2 position = new Vector2(0.75f, 0.2f);
        private static Vector2 dimension = new Vector2(0.25f, 0.07f);
        private Dictionary<ulong, PlayerMSG> currentHits = new Dictionary<ulong, PlayerMSG>();

        FieldInfo knockdownMaxValue = typeof(ReactiveTarget).GetField("knockdownHealth", (BindingFlags.Instance | BindingFlags.NonPublic));

        #region oxide hooks
        void Loaded()
        {     
            lang.RegisterMessages(messages, this);
            ShotData = Interface.Oxide.DataFileSystem.GetFile("targetpractice_scores");
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            LoadTargets();
            timer.Once(saveTimer * 60, () => SaveLoop());
        }
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload() => SaveData();        
               
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            try
            {                
                if (entity is ReactiveTarget && hitinfo.Initiator is BasePlayer)
                {
                              
                    var target = (ReactiveTarget)entity;
                    var attacker = (BasePlayer)hitinfo.Initiator;
                    if (entity != null && attacker != null)
                    {
                        float distance = GetPlayerDistance(entity.transform.position, attacker.transform.position); 
                        string hit = lang.GetMessage("hit", this, attacker.UserIDString);
                                                
                        CheckPlayerData(attacker);

                        var data = shotData.longShot;
                        var time = data[attacker.userID].PopupTime;

                        string weapon = FormatWeapon(attacker.GetActiveItem());

                        if (hitinfo.HitBone == StringPool.Get("target_collider_bullseye"))
                        {
                            hit = lang.GetMessage("bullseye", this, attacker.UserIDString);
                            if (data[attacker.userID].Bullseye < distance) { data[attacker.userID].Bullseye = distance; data[attacker.userID].Weapon = weapon; }
                        }
                        else if (data[attacker.userID].Range < distance) { data[attacker.userID].Range = distance; data[attacker.userID].Weapon = weapon; }

                            if (distance > shotData.bestHit.Range)
                        {
                            shotData.bestHit.Name = attacker.displayName;
                            shotData.bestHit.Range = distance;
                            shotData.bestHit.Weapon = attacker.GetActiveItem().info.displayName.english;
                            if (broadcastNewScore)
                                BroadcastToAll(attacker.displayName, distance.ToString(), weapon);
                        }
                        if (target.IsKnockedDown())
                            timer.Once(time, () => target.SetFlag(BaseEntity.Flags.On, true));

                        if (!currentHits.ContainsKey(attacker.userID)) currentHits.Add(attacker.userID, new PlayerMSG());
                        currentHits[attacker.userID].msg = hit;
                        currentHits[attacker.userID].distance = distance;
                        OverwriteDuplicate(attacker);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        private string FormatWeapon(Item weapon)
        {
            string weaponstring = weapon.info.displayName.english;
            string formattedWeapon = "";
            List<string> mods = new List<string>();
            if (weapon.contents != null)
                foreach (var mod in weapon.contents.itemList)
                    mods.Add(mod.info.shortname);
            if (mods.Count > 0)
            {
                if (mods.Contains("weapon.mod.silencer"))
                    formattedWeapon = "Silenced ";
                if (mods.Contains("weapon.mod.holosight"))
                    formattedWeapon = formattedWeapon + "Sighted ";
                if (mods.Contains("weapon.mod.lasersight"))
                    if (!formattedWeapon.Contains("Sighted"))
                        formattedWeapon = formattedWeapon + "Laser Sighted ";
                if (mods.Contains("weapon.mod.small.scope"))
                    formattedWeapon = formattedWeapon + "Scoped ";
            }
            formattedWeapon = formattedWeapon + weaponstring;
            return formattedWeapon;
        }
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity != null)
                if (entity.GetComponent<ReactiveTarget>())            
                    ModifyTargetStats(entity);            
        }
        private void ModifyTargetStats(BaseNetworkable entity)
        {
            if (entity != null)
            {
                knockdownMaxValue.SetValue((ReactiveTarget)entity, maxKnockdown);
                entity.SendNetworkUpdateImmediate(false);
            }
        }
        private void OverwriteDuplicate(BasePlayer player)
        {
            if (!currentHits[player.userID].time)
            {
                currentHits[player.userID].time = true;
                timer.Once(0.05f, () => GetMessage(player));
            }
        }
        private void GetMessage(BasePlayer player)
        {
            string hit = currentHits[player.userID].msg;
            float distance = currentHits[player.userID].distance;
            TPUI.GetPlayer(player).UseUI(fontColor2 + hit + "</color> " + fontColor1 + distance + "M</color>");
            currentHits[player.userID].time = false;
        }
        private void CheckPlayerData(BasePlayer player)
        {
            var data = shotData.longShot;
            if (!data.ContainsKey(player.userID)) data.Add(player.userID, new TargetInfo() { Name = player.displayName, Range = 0, Bullseye = 0 });
        }
        #endregion

        #region functions
        protected void LoadTargets()
        {
            ReactiveTarget[] targets = GameObject.FindObjectsOfType<ReactiveTarget>();

            if (targets.Length > 0)
            {
                foreach (ReactiveTarget target in targets.ToList())
                    ModifyTargetStats(target);
            }
        }
        private float GetPlayerDistance(Vector3 targetPos, Vector3 attackerPos)
        {
            var distance = Vector3.Distance(targetPos, attackerPos);
            var rounded = Mathf.Round(distance * 100f) / 100f;
            return rounded;
        }
        private void BroadcastToAll(string name, string range, string weapon) => PrintToChat(fontColor1 + "Target Practice: " + name + " </color>" + fontColor2 + "just set a new high score of " + "</color>" + fontColor1 + range + "M</color> " + fontColor2 + "using a</color> " + fontColor1 + weapon + "</color>");
        class TPUI : MonoBehaviour
        {
            public List<int> slots = new List<int>();
            int i;

            private BasePlayer player;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                i = 0;
            }

            public static TPUI GetPlayer(BasePlayer player)
            {
                TPUI p = player.GetComponent<TPUI>();
                if (p == null) p = player.gameObject.AddComponent<TPUI>();
                return p;
            }

            private int FindSlot()
            {
                for (int i = 0; i < maxUIMsg; i++)
                    if (!slots.Contains(i)) return i;
                return -1;
            }
            public void UseUI(string msg)
            {
                i++;
                string uiNum = i.ToString();
                
                int slot = FindSlot();
                if (slot == -1) return;

                Vector2 offset = (new Vector2(0, dimension.y) + new Vector2(0, UIspacing)) * slot;

                Vector2 posMin = position + offset;
                Vector2 posMax = posMin + dimension;

                var elements = new CuiElementContainer();
                CuiElement textElement = new CuiElement
                {
                    Name = uiNum,
                    Parent = "HUD/Overlay",
                    FadeOut = 0.1f,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = msg,
                            FontSize = fontSize,
                            Align = TextAnchor.MiddleCenter,
                            FadeIn = 0.1f
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
                slots.Add(slot);
                Interface.GetMod().CallHook("DestroyHitMsg", new object[] { player, uiNum, slot, msgDuration });
            }
        }
        private void DestroyNotification(BasePlayer player, string msgNum, int slot)
        {
            bool t = CuiHelper.DestroyUi(player, msgNum);
            if (!t) DestroyNotification(player, msgNum, slot);
            TPUI.GetPlayer(player).slots.Remove(slot);
        }
        private void DestroyHitMsg(BasePlayer player, string msgNum, int slot, float duration)
        {
            timer.Once(duration, () => DestroyNotification(player, msgNum, slot));
        }
        #endregion

        #region chat commands
        [ChatCommand("target")]
        void cmdTarget(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                SendReply(player, fontColor1 + lang.GetMessage("tp", this, player.UserIDString));
                SendReply(player, fontColor1 + lang.GetMessage("hit1", this, player.UserIDString) + fontColor2 + lang.GetMessage("hit2", this, player.UserIDString));
                SendReply(player, fontColor1 + lang.GetMessage("be1", this, player.UserIDString) + fontColor2 + lang.GetMessage("be2", this, player.UserIDString));
                SendReply(player, fontColor1 + lang.GetMessage("time1", this, player.UserIDString) + fontColor2 + lang.GetMessage("time2", this, player.UserIDString));
                SendReply(player, fontColor1 + lang.GetMessage("pb1", this, player.UserIDString) + fontColor2 + lang.GetMessage("pb2", this, player.UserIDString));
                if (isAuth(player))
                    SendReply(player, fontColor1 + lang.GetMessage("wipe1", this, player.UserIDString) + fontColor2 + lang.GetMessage("wipe2", this, player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "top":
                    if (args.Length >= 2)
                    {
                        if (args[1].ToLower() == "hit")
                        {
                            int amount = 5;
                            if (args.Length >= 3) int.TryParse(args[2], out amount);

                            Dictionary<ulong, TargetInfo> top5 = shotData.longShot.OrderByDescending(pair => pair.Value.Range).Take(amount).ToDictionary(pair => pair.Key, pair => pair.Value);

                            if (top5.Count > 0)
                            {
                                SendReply(player, fontColor1 + lang.GetMessage("title", this, player.UserIDString) + "</color>" + fontColor2 + lang.GetMessage("bestHits", this, player.UserIDString) + "</color>");
                                foreach (var name in top5)
                                    SendReply(player, string.Format(fontColor2 + lang.GetMessage("topList1", this, player.UserIDString) + "</color>", name.Value.Name, name.Value.Range, name.Value.Weapon));
                            }
                            return;
                        }
                        else if (args[1].ToLower() == "bullseye")
                        {
                            int amount = 5;
                            if (args.Length >= 4) int.TryParse(args[3], out amount);


                            Dictionary<ulong, TargetInfo> top5 = shotData.longShot.OrderByDescending(pair => pair.Value.Bullseye).Take(amount).ToDictionary(pair => pair.Key, pair => pair.Value);
                            if (top5.Count > 0)
                            {
                                SendReply(player, fontColor1 + lang.GetMessage("title", this, player.UserIDString) + "</color>" + fontColor2 + lang.GetMessage("bestBullseye", this, player.UserIDString) + "</color>");
                                foreach (var name in top5)
                                    SendReply(player, string.Format(fontColor2 + lang.GetMessage("topList1", this, player.UserIDString) + "</color>", name.Value.Name, name.Value.Bullseye, name.Value.Weapon));
                            }
                            return;
                        }
                    }
                    return;
                case "wipe":
                    if (isAuth(player))
                    {
                        shotData.longShot.Clear();
                        SendReply(player, lang.GetMessage("wipe", this, player.UserIDString));
                    }
                    return;
                case "time":
                    if (args.Length >= 2)
                    {
                        CheckPlayerData(player);
                        int time = popupTime;
                        int.TryParse(args[1], out time);
                        if (time != popupTime)
                            shotData.longShot[player.userID].PopupTime = time;
                        SendReply(player, string.Format(fontColor1 + lang.GetMessage("title", this, player.UserIDString) + "</color>" + fontColor2 + lang.GetMessage("changeTime", this, player.UserIDString) + "</color>", time));
                    }
                    return;
                case "pb":
                    if (shotData.longShot.ContainsKey(player.userID))
                    {
                        SendReply(player, fontColor1 + lang.GetMessage("title", this, player.UserIDString) + "</color>" + fontColor2 + lang.GetMessage("pb3", this, player.UserIDString));
                        SendReply(player, fontColor2 + lang.GetMessage("hit", this, player.UserIDString) + "</color> " + fontColor1 + shotData.longShot[player.userID].Range + lang.GetMessage("m", this, player.UserIDString));
                        SendReply(player, fontColor2 + lang.GetMessage("bullseye", this, player.UserIDString) + "</color> " + fontColor1 + shotData.longShot[player.userID].Bullseye + lang.GetMessage("m", this, player.UserIDString));
                    }
                    return;
            }
        }
        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)            
                if (player.net.connection.authLevel < 1)
                    return false; 
            return true;
        }
        #endregion

        #region data
        class TargetData
        {
            public Dictionary<ulong, TargetInfo> longShot = new Dictionary<ulong, TargetInfo>();
            public BestHit bestHit = new BestHit();
            public TargetData() { }
        }
        class BestHit
        {
            public string Name = "";
            public float Range = 0;
            public string Weapon = "";
            public bool isBullseye = false;
        }
        class TargetInfo
        {
            public string Name;
            public float Range;
            public float Bullseye;
            public string Weapon;
            public bool UseUI = true;
            public int PopupTime = popupTime;           
        }
        class PlayerMSG
        {
            public string msg;
            public float distance;
            public string weapon;
            public bool time = false;
        }
        void SaveLoop()
        {
            SaveData();
            timer.Once(saveTimer * 60, () => SaveLoop());
        }
        void SaveData()
        {
            ShotData.WriteObject(shotData);
        }
        void LoadData()
        {
            try
            {
                shotData = Interface.GetMod().DataFileSystem.ReadObject<TargetData>("targetpractice_scores");
            }
            catch
            {
                Puts("Couldn't load TargetPractice data, creating new datafile");
                shotData = new TargetData();
            }
        }
        #endregion

        #region config
        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private bool changed;

        private static int popupTime = 5;
        private static int msgDuration = 5;
        private static int fontSize = 20;
        private static int saveTimer = 10;
        private static int maxUIMsg = 11;

        private static bool broadcastNewScore = true;
        private static string fontColor1 = "<color=orange>";
        private static string fontColor2 = "<color=#939393>";

        private static float maxKnockdown = 100f;
        private static float UIspacing = 0.01f;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Target - Default time to reset target (seconds)", ref popupTime);
            CheckCfg("Messages - Duration (seconds)", ref popupTime);
            CheckCfg("Messages - Font size", ref fontSize);
            CheckCfg("Messages - Message color", ref fontColor2);
            CheckCfg("Messages - Main color", ref fontColor1);
            CheckCfg("Messages - Broadcast new high scores to all", ref broadcastNewScore);
            CheckCfg("Data - Save timer (minutes)", ref saveTimer);
            CheckCfgFloat("Target - Knockdown health", ref maxKnockdown);
        }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }
        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                changed = true;
            }
            return value;
        }
        #endregion

        #region messages
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "TargetPractice: "},
            {"hit", "Hit" },
            {"bullseye", "Bullseye" },
            {"wipe", "You have wiped all TargetPractice hit data!" },
            {"bestBullseye", "--- Best Bullseye hits ---" },
            {"bestHits", "--- Best Hits ---" },
            {"topList1", "Name:</color><color=orange> {0}</color><color=#939393>, Distance:</color><color=orange> {1}</color><color=#939393>, Weapon:</color><color=orange> {2}" },
            {"changeTime", "You have changed the reset time to {0}" },
            {"tp", "Target Practice</color>" },
            {"hit1", "/target top hit <opt:##></color>" },
            {"hit2", " - Displays top 5 hit distances, optional number to change amount shown</color>" },
            {"be2", " - Displays top 5 bullseye hit distances, optional number to change amount shown</color>" },
            {"be1", "/target top bullseye <opt:##></color>" },
            {"time1", "/target time <##></color>" },
            {"time2", " - Change the target reset time</color>" },
            {"wipe1", "/target wipe</color>" },
            {"wipe2", " - Clear all hit data</color>" },
            {"pb1", "/target pb</color>" },
            {"pb2", " - Shows your best hits</color>" },
            {"pb3", "--- Personal Best ---</color>" },
            {"m", "M</color>" }
        };
        #endregion
    }
}
