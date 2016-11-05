using System.Collections.Generic;
using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using UnityEngine;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("Jail", "Reneb / k1lly0u", "3.0.2", ResourceId = 794)]
    class Jail : RustPlugin
    {
        [PluginReference] Plugin ZoneManager;
        [PluginReference] Plugin Spawns;
        [PluginReference] Plugin Kits;

        private bool Changed;
        private bool Started = false;

        JailDataStorage jailData;
        private DynamicConfigFile JailData;

        private Dictionary<ulong, int> jailTimerList = new Dictionary<ulong, int>();
        private List<string> prisonIDs = new List<string>();

        private static LayerMask GROUND_MASKS = LayerMask.GetMask("Terrain", "World", "Construction");

        #region UI
        class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = "Overlay",
                        panelName
                    }
                };
                return NewElement;
            }            
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }            
        }
        private void CreateJailTimer(BasePlayer player)
        {
            if (player != null)
            {
                CuiHelper.DestroyUi(player, "jailTimer");
                if (jailData.Prisoners.ContainsKey(player.userID) && jailTimerList.ContainsKey(player.userID))
                {
                    TimeSpan dateDifference = TimeSpan.FromSeconds(jailTimerList[player.userID]);
                    string clock = string.Format("{0:D2}:{1:D2}:{2:D2}", dateDifference.Hours, dateDifference.Minutes, dateDifference.Seconds);
                    if (jailData.Prisoners[player.userID].expireTime == -99999)
                        clock = "~";

                    var container = UI.CreateElementContainer("jailTimer", "0.3 0.3 0.3 0.6", "0.42 0.965", "0.58 0.995", false);
                    UI.CreateLabel(ref container, "jailTimer", "", $"Remaining: {clock}", 18, "0 0", "1 1");
                    CuiHelper.AddUi(player, container);

                    jailTimerList[player.userID]--;
                    if (jailTimerList[player.userID] > 0)
                        timer.Once(1, () => CreateJailTimer(player));
                    else timer.Once(2, () => CheckPlayerExpireTime(player));
                }
            }
        }
        #endregion

        #region Oxide Hooks        
        void Loaded()
        {
            permission.RegisterPermission("jail.admin", this);

            lang.RegisterMessages(messages, this);

            JailData = Interface.Oxide.DataFileSystem.GetFile("jail_data");
            JailData.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter(), };            
        }
        void OnServerInitialized()
        {
            if (!CheckDependencies()) return;
            LoadData();
            LoadVariables();
            foreach(var player in BasePlayer.activePlayerList)
                CheckPlayer(player);
        }
        private bool CheckDependencies()
        {
            if (ZoneManager == null)
            {
                PrintWarning($"ZoneManager could not be found!");
                return false;
            }

            if (Spawns == null)
            {
               PrintWarning($"Spawns Database could not be found!");
                return false;
            }
            Started = true;
            return true;
        } 
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            if (Started)
            {                
                jailTimerList.Clear();
                foreach (var player in BasePlayer.activePlayerList)
                {
                    foreach(var jail in jailData.prisons)
                        ZoneManager.Call("RemovePlayerFromZoneKeepinlist", jail.Value.zoneID, player);
                    CuiHelper.DestroyUi(player, "jailTimer");
                }
                SaveData();
            }
        }
        void OnPlayerInit(BasePlayer player) => CheckPlayer(player);
        void OnPlayerSleepEnded(BasePlayer player) => CheckPlayer(player);
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (Started)
            {
                try
                {
                    if (entity is BasePlayer && hitinfo.Initiator is BasePlayer)
                    {
                        var victim = entity.ToPlayer();
                        var attacker = hitinfo.Initiator.ToPlayer();
                        if (disableDamage)
                        {
                            if (victim.userID != attacker.userID)
                                if (jailData.Prisoners.ContainsKey(victim.userID) && jailData.Prisoners.ContainsKey(attacker.userID))
                                {
                                    hitinfo.damageTypes.ScaleAll(0);
                                    SendMsg(attacker, lang.GetMessage("ff", this, attacker.UserIDString));
                                }
                        }
                        else if (!jailData.Prisoners.ContainsKey(victim.userID) && jailData.Prisoners.ContainsKey(attacker.userID)) hitinfo.damageTypes.ScaleAll(0);
                        else if (jailData.Prisoners.ContainsKey(victim.userID) && !jailData.Prisoners.ContainsKey(attacker.userID)) hitinfo.damageTypes.ScaleAll(0);
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
        void OnPlayerRespawned(BasePlayer player) => CheckPlayer(player);
        void CheckPlayer(BasePlayer player)
        {
            if (Started)
                if (jailData.Prisoners.ContainsKey(player.userID))
                    CheckInmate(player);
        }
        void CheckInmate(BasePlayer player)
        {
            if (!CheckPlayerExpireTime(player))
            {
                string prisonName = jailData.Prisoners[player.userID].prisonName;
                string zoneID = jailData.prisons[prisonName].zoneID;
                if (!isInZone(player, zoneID))
                {
                    int cellNum = jailData.Prisoners[player.userID].cellNumber;
                    object cellPos = FindSpawnPoint(prisonName, cellNum);
                    if (cellPos == null) { Puts("null cell pos"); return; }
                    ZoneManager.Call("AddPlayerToZoneKeepinlist", zoneID, player);
                    TeleportPlayerPosition(player, (Vector3)cellPos);                    
                }
                if (!jailTimerList.ContainsKey(player.userID))
                    jailTimerList.Add(player.userID, (int)(jailData.Prisoners[player.userID].expireTime - GrabCurrentTime()));
                CreateJailTimer(player);
            }
        }
                
        #endregion

        #region Main Functions        
        private object FindPlayer(string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (steamid != 0L)
                    if (p.userID == steamid)
                    {
                        foundPlayers.Clear();
                        foundPlayers.Add(p);
                        return foundPlayers[0];
                    }
                string lowername = p.displayName.ToLower();
                if (lowername.Contains(lowerarg))
                {
                    foundPlayers.Add(p);
                }
            }
            if (foundPlayers.Count == 0) return lang.GetMessage("noPlayers", this);
            if (foundPlayers.Count > 1) return lang.GetMessage("multiPlayers", this);

            return foundPlayers[0];
        }
        private BasePlayer FindPlayerByID(ulong steamid)
        {
            BasePlayer targetplayer = BasePlayer.FindByID(steamid);
            if (targetplayer != null)
            {
                return targetplayer;
            }
            targetplayer = BasePlayer.FindSleeping(steamid);
            if (targetplayer != null)
            {
                return targetplayer;
            }
            return null;
        }
        private void TPPlayer(BasePlayer player, Vector3 pos)
        {
            player.MovePosition(pos);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", pos);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            player.SendFullSnapshot();
        }
        private object CheckSpawns(string name)
        {
            object success = Spawns.Call("GetSpawnsCount", new object[] { name });
            if (success is string) return null;
            return success;
        }
        private void AddJail(BasePlayer player, string[] args)
        {
            string name = args[1].ToLower();

            Prison data = new Prison();
            if (CheckSpawns(args[3]) != null)
            {
                object zoneid = ZoneManager.Call("CheckZoneID", args[2] );
                if (zoneid is string && (string)zoneid != "")
                {
                    data.zoneID = (string)zoneid;

                    object location = ZoneManager?.Call("GetZoneLocation", (string)zoneid );
                    if (location != null && location is Vector3)
                        data.location = (Vector3)location;
                    else
                    {
                        SendMsg(player, lang.GetMessage("invalidZPos", this, player.UserIDString));
                        return;
                    }

                    object radius = ZoneManager?.Call("GetZoneRadius", (string)zoneid );
                    if (radius != null && radius is float)
                        data.zoneRadius = float.Parse(radius.ToString());
                    else
                    {
                        SendMsg(player, lang.GetMessage("invalidZRad", this, player.UserIDString));
                        return;
                    }

                    data.spawnFile = args[3];

                    int cellCount = (int)CheckSpawns(args[3]);
                    for (int i = 0; i < cellCount; i++)
                        data.freeCells.Add(i, false);
                }
                else
                {
                    SendMsg(player, lang.GetMessage("invalidZID", this, player.UserIDString) + args[2]);
                    return;
                }

                jailData.prisons.Add(args[1].ToLower(), data);
                SendMsg(player, lang.GetMessage("newJailAdd", this, player.UserIDString));
                SaveData();
                return;
            }
            SendMsg(player, lang.GetMessage("invalidSF", this, player.UserIDString) + args[3]);
        }
        private void RemoveJail(BasePlayer player, string arg)
        {
            if (jailData.prisons.ContainsKey(arg.ToLower()))
            {
                EraseJailZone(arg.ToLower());
                jailData.prisons.Remove(arg.ToLower());
                SendMsg(player, lang.GetMessage("remJail", this, player.UserIDString) + arg);
                SaveData();
            }
            else SendMsg(player, lang.GetMessage("noJail", this, player.UserIDString));
        }
        private void EraseJailZone(string zoneID)
        {
            ZoneManager.Call("EraseZone", zoneID);
            Puts("Jail Zone " + zoneID + " removed.");
        }
        private object SendPlayerToJail(BasePlayer player, string prisonName, int time)
        {
            object cellNum = FindEmptyCell(prisonName);
            if (cellNum != null)
            {
                string zoneID = jailData.prisons[prisonName].zoneID;
                object cellPos = FindSpawnPoint(prisonName, (int)cellNum);
                if (cellPos == null) return "noPos";
                jailData.prisons[prisonName].freeCells[(int)cellNum] = true;

                double jailTime = -99999;

                if (time != -99999)
                    jailTime = time + GrabCurrentTime();

                Inmate inmate = new Inmate() { initialPos = player.transform.position, prisonName = prisonName, cellNumber = (int)cellNum, expireTime = jailTime};
                jailData.Prisoners.Add(player.userID, inmate);

                SendMsg(player, lang.GetMessage("sentPrison", this, player.UserIDString));
                timer.Once(5, ()=>
                {
                    SaveInventory(player);
                    ZoneManager.Call("AddPlayerToZoneKeepinlist", zoneID, player);
                    player.inventory.Strip();
                    TeleportPlayerPosition(player, (Vector3)cellPos);

                    jailTimerList.Add(player.userID, time);
                    CreateJailTimer(player);

                    SendMsg(player, lang.GetMessage("checkTime", this, player.UserIDString));

                    if (giveKit)
                    {
                        if (kitName == null || kitName == "") return;
                        Kits?.Call("GiveKit", player, kitName);
                    }
                });
                SaveData();
                return true;
            }
            return "noCells";
        }       
        private void FreeFromJail(BasePlayer player)
        {
            string prisonName = jailData.Prisoners[player.userID].prisonName;
            int cellNum = jailData.Prisoners[player.userID].cellNumber;
            string zoneID = jailData.prisons[prisonName].zoneID;
            CuiHelper.DestroyUi(player, "jailTimer");
            if (jailTimerList.ContainsKey(player.userID))
                jailTimerList.Remove(player.userID);

            jailData.prisons[prisonName].freeCells[cellNum] = false;
            RestoreInventory(player);

            SendMsg(player, lang.GetMessage("relPrison", this, player.UserIDString));

            Vector3 freePos = CalculateFreePos(prisonName);
            if (useInitialSpawns) freePos = jailData.Prisoners[player.userID].initialPos;

            jailData.Prisoners.Remove(player.userID);
            ZoneManager.Call("RemovePlayerFromZoneKeepinlist", zoneID, player);
            TeleportPlayerPosition(player, freePos);
            SaveData();
        }
        private object FindSpawnPoint(string prisonName, int cellNumber)
        {
            object cellPos = Spawns?.Call("GetSpawn", new object[] { jailData.prisons[prisonName].spawnFile, cellNumber });
            if (cellPos is string)
            {
                Puts((string)cellPos);
                return null;
            }
            return (Vector3)cellPos;
        }
        private Vector3 CalculateFreePos(string prisonName)
        {
            Vector3 zonePos = jailData.prisons[prisonName].location;
            float zoneRadius = jailData.prisons[prisonName].zoneRadius;
            Vector3 calcPos = zonePos + new Vector3(zoneRadius + 10, 0, 0);
            Vector3 finalPos = CalculateGroundPos(calcPos);
            return finalPos;
        }
        static Vector3 CalculateGroundPos(Vector3 sourcePos) // credit Wulf & Nogrod
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, GROUND_MASKS))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }
        private bool CheckPlayerExpireTime(BasePlayer player)
        {
            if (!player.IsConnected()) return false;
            if (player.IsDead()) return false;

            double time = jailData.Prisoners[player.userID].expireTime;
            if (time == -99999)
                return false;
            double timeLeft = time - GrabCurrentTime();
            if (timeLeft <= 0)
            {                
                jailTimerList.Remove(player.userID);
                FreeFromJail(player);
                return true;
            }
            return false;
        }
        void TeleportPlayerPosition(BasePlayer player, Vector3 destination)
        {
            player.MovePosition(destination);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            player.SendFullSnapshot();
        }
        private string FindEmptyPrison()
        {
            List<string> emptyPrisons = new List<string>();
            int num = 0;
            foreach (var prison in jailData.prisons)
            {
                foreach (var cell in prison.Value.freeCells)
                    if (cell.Value == false)
                        num++;
                if (num > 0) return prison.Key;
            }            
            return null;
        }
        private object FindEmptyCell(string prisonName)
        {
            int emptyCell = -1;
            foreach (var cell in jailData.prisons[prisonName].freeCells)
            {
                if (cell.Value == false)
                {
                    emptyCell = cell.Key;
                    break;
                }                               
            }
            if (emptyCell == -1) return null;
            return emptyCell;
        }

        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        private void SendMsg(BasePlayer player, string msg)
        {
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + msgColor + msg + "</color>");
        }
        private bool IsPrisoner(BasePlayer player)
        {
            if (jailData.Prisoners.ContainsKey(player.userID)) return true;
            return false;
        }
        #endregion

        #region ZoneManager Hooks       
        bool isInZone(BasePlayer player, string zoneID)
        {
            if (ZoneManager == null) return false;
            return (bool)ZoneManager.Call("isPlayerInZone", zoneID, player);
        }
        void OnEnterZone(string ZoneID, BasePlayer player)
        {
            if (Started)
                if (prisonIDs.Contains(ZoneID))
            {
                if (hasPermission(player)) { SendMsg(player, string.Format(lang.GetMessage("welcomeJail", this, player.UserIDString), player.displayName)); return; }
                else if (!jailData.Prisoners.ContainsKey(player.userID)) { SendMsg(player, lang.GetMessage("keepOut", this, player.UserIDString)); }
            }
        }
        void OnExitZone(string ZoneID, BasePlayer player)
        {
            if (Started)
                if (prisonIDs.Contains(ZoneID))
                if (jailData.Prisoners.ContainsKey(player.userID)) { SendMsg(player, lang.GetMessage("keepIn", this, player.UserIDString)); }
        }
        #endregion

        #region Permissions
        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
                if (player.net.connection.authLevel != 2) return false;
            return true;
        }
        bool hasPermission(BasePlayer player)
        {
            if (isAuth(player)) return true;
            else if (permission.UserHasPermission(player.userID.ToString(), "jail.admin")) return true;
            return false;
        }
        #endregion

        #region Chat/Console Commands        
        [ChatCommand("jail")]
        private void cmdJail(BasePlayer player, string command, string[] args)
        {
            if (Started)
            if (jailData.Prisoners.ContainsKey(player.userID) && !hasPermission(player))
            {
                double currentTime = GrabCurrentTime();
                double expTime = jailData.Prisoners[player.userID].expireTime;
                if (expTime == -99999)
                {
                    SendMsg(player, lang.GetMessage("noRelease", this, player.UserIDString));
                    return;
                }
                double timeLeft = expTime - currentTime;
                if (timeLeft > 0)
                {
                    string msg = lang.GetMessage("mins", this, player.UserIDString);

                    if (timeLeft <= 60) msg = lang.GetMessage("secs", this, player.UserIDString);
                    else timeLeft = timeLeft / 60;

                    SendMsg(player, string.Format(lang.GetMessage("remainJail", this, player.UserIDString), timeLeft, msg));
                    return;
                }
                FreeFromJail(player);
            }
            if (!hasPermission(player)) return;
            if (args == null || args.Length == 0)
            {
                SendMsg(player, lang.GetMessage("synSend1", this, player.UserIDString));
                SendMsg(player, lang.GetMessage("synFree", this, player.UserIDString));
                SendMsg(player, lang.GetMessage("synAdd", this, player.UserIDString));
                SendMsg(player, lang.GetMessage("synRem", this, player.UserIDString));
                SendMsg(player, lang.GetMessage("synList", this, player.UserIDString));
                SendMsg(player, lang.GetMessage("synPurge", this, player.UserIDString));
                SendMsg(player, lang.GetMessage("synZone", this, player.UserIDString));
                SendMsg(player, lang.GetMessage("synWipe", this, player.UserIDString));
                return;
            }
            if (!hasPermission(player)) return;
            switch (args[0].ToLower())
            {
                case "send":
                    if (args.Length >= 2)
                    {
                        object addPlayer = FindPlayer(args[1]);

                        if (addPlayer is string) { SendMsg(player, (string)addPlayer); return; }

                        int time = -99999;
                        string prison = FindEmptyPrison();
                        if (args.Length >= 5)
                            if (jailData.prisons.ContainsKey(args[4].ToLower())) prison = args[4].ToLower();
                        if (string.IsNullOrEmpty(prison)) { SendMsg(player, lang.GetMessage("noPrisons", this, player.UserIDString)); return; }

                        BasePlayer target = (BasePlayer)addPlayer;
                        if (jailData.Prisoners.ContainsKey(target.userID)) return;

                        if (args.Length >= 3)
                            int.TryParse(args[2], out time);

                        object success = SendPlayerToJail(target, prison, time);

                        if (success is bool)
                        {
                            if ((bool)success)
                            {
                                SendMsg(player, string.Format(lang.GetMessage("sentTo", this, player.UserIDString), target.displayName, prison));
                                if (args.Length >= 4 && broadcastImprisonment)
                                    PrintToChat(string.Format(lang.GetMessage("sentToBroadcast", this, player.UserIDString), target.displayName, args[3]));
                            }                          
                        }
                        else if (success is string)
                        {
                            if ((string)success == "noPos") SendMsg(player, string.Format(lang.GetMessage("noPos", this), prison));
                            else if ((string)success == "noCells") SendMsg(player, string.Format(lang.GetMessage("noCells", this), prison));
                            return;
                        }
                    }
                    else SendMsg(player, lang.GetMessage("synSend1", this, player.UserIDString));
                    return;

                case "free":
                    if (args.Length == 2)
                    {
                        object freePlayer = FindPlayer(args[1]);

                        if (freePlayer is string) { SendMsg(player, (string)freePlayer); return; }
                        BasePlayer freetarget = (BasePlayer)freePlayer;

                        if (jailData.Prisoners.ContainsKey(freetarget.userID))
                        {
                            FreeFromJail(freetarget);
                            SendMsg(player, string.Format(lang.GetMessage("relFrom", this, player.UserIDString), freetarget.displayName));
                        }
                    }
                    else SendMsg(player, lang.GetMessage("synFree", this, player.UserIDString));
                    return;
                case "add":
                    if (args.Length == 4)
                        AddJail(player, args);
                    else SendMsg(player, lang.GetMessage("synAdd", this, player.UserIDString));                  
                    return;
                case "remove":
                    if (args.Length == 2)
                        RemoveJail(player, args[1]);
                    else SendMsg(player, lang.GetMessage("synRem", this, player.UserIDString));
                    return;
                case "zone":
                    int radius = 30;
                    if (args.Length >= 2)
                        int.TryParse(args[1], out radius);
                    string zoneID = "Jail" + (jailData.prisons.Count + 1);
                    string[] zoneargs = new string[] { "eject", "true", "radius", radius.ToString(), "sleepgod", "true", "undestr", "true", "nobuild", "true", "notp", "true", "nokits", "true", "nodeploy", "true", "nosuicide", "true" };
                    ZoneManager?.Call("CreateOrUpdateZone", zoneID, zoneargs, player.transform.position);
                    SendMsg(player, lang.GetMessage("createJail", this, player.UserIDString));
                    SendMsg(player, string.Format(lang.GetMessage("jailID", this, player.UserIDString), zoneID));
                    return;
                case "list":
                    foreach (var entry in jailData.prisons)
                        SendReply(player, "Name: " + entry.Key + ", Location: " + entry.Value.location.ToString());
                    return;
                case "purge":
                    List<ulong> RemoveList = new List<ulong>();
                    List<BasePlayer> FreeList = new List<BasePlayer>();
                    foreach(var entry in jailData.Prisoners)
                    {
                        if (entry.Value.expireTime - GrabCurrentTime() < 0)
                        {
                            BasePlayer target = FindPlayerByID(entry.Key);
                            if (target != null)
                                FreeList.Add(target);
                            else RemoveList.Add(entry.Key);
                        }
                    }
                    foreach (var entry in RemoveList)
                        jailData.Prisoners.Remove(entry);
                    foreach (var entry in FreeList)
                        FreeFromJail(entry);
                    return;
                case "wipe":
                    foreach(var entry in jailData.prisons)
                        EraseJailZone(entry.Key);
                    foreach (var entry in jailData.Prisoners)
                    {
                        BasePlayer inmate = FindPlayerByID(entry.Key);
                        if (inmate != null)
                            FreeFromJail(inmate);
                    }
                    jailData.Prisoners.Clear();
                    jailData.prisons.Clear();
                    SaveData();
                    return;
            }
        }
        [ConsoleCommand("jail.free")]
        private void ccmdJailFree(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
                if (arg.connection.authLevel < 1) return;
            if (arg.Args.Length != 0)
            {
                object freePlayer = FindPlayer(arg.Args[0]);
                if (freePlayer is string) { SendReply(arg, (string)freePlayer); return; }
                BasePlayer freetarget = (BasePlayer)freePlayer;
                if (jailData.Prisoners.ContainsKey(freetarget.userID))
                {
                    FreeFromJail(freetarget);
                    SendReply(arg, string.Format(lang.GetMessage("relFrom", this), freetarget.displayName));
                }
            }            
            else SendReply(arg, lang.GetMessage("synFree", this));
        }
        [ConsoleCommand("jail.send")]
        private void ccmdjailSend(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
                if (arg.connection.authLevel < 1) return;
            if (arg.Args.Length >= 1)
            {
                object addPlayer = FindPlayer(arg.Args[0]);

                if (addPlayer is string) { SendReply(arg, (string)addPlayer); return; }

                int time = -99999;
                string prison = FindEmptyPrison();
                if (arg.Args.Length >= 4)
                    if (jailData.prisons.ContainsKey(arg.Args[3].ToLower())) prison = arg.Args[3].ToLower();
                if (string.IsNullOrEmpty(prison)) { lang.GetMessage("noPrisons", this); return; }

                BasePlayer target = (BasePlayer)addPlayer;
                if (jailData.Prisoners.ContainsKey(target.userID)) return;

                if (arg.Args.Length >= 2)
                    int.TryParse(arg.Args[1], out time);

                object success = SendPlayerToJail(target, prison, time);
                if (success is bool)
                {
                    if ((bool)success)
                    {
                        SendReply(arg, lang.GetMessage("sentTo", this), target.displayName, prison);
                        if (arg.Args.Length >= 3 && broadcastImprisonment)
                            PrintToChat(string.Format(lang.GetMessage("sentToBroadcast", this), target.displayName, arg.Args[2]));
                    }
                }
                else if (success is string)
                {
                    if ((string)success == "noPos") SendReply(arg, string.Format(lang.GetMessage("noPos", this), prison));
                    else if ((string)success == "noCell") SendReply(arg, string.Format(lang.GetMessage("noCell", this), prison));
                    return;
                }
            }
            else SendReply(arg, lang.GetMessage("synSend1", this));
            return;
        }


        #endregion
        
        #region Config        
        static bool useInitialSpawns = true;
        static bool disableDamage = false;
        static bool giveKit = true;
        static bool broadcastImprisonment = true;
        static string kitName = "default";
        static string msgColor = "<color=#d3d3d3>";

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Inmates - Release - Return to initial position when released", ref useInitialSpawns);
            CheckCfg("Inmates - Disable damage inside the Jail", ref disableDamage);
            CheckCfg("Inmates - Kits - Give kit to Inmates", ref giveKit);
            CheckCfg("Inmates - Kits - Kitname", ref kitName);
            CheckCfg("Messages - Message color", ref msgColor);
            CheckCfg("Messages - Broadcast imprisonment", ref broadcastImprisonment);
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
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }
        #endregion        

        #region Classes and Data
        class JailDataStorage
        {
            public Dictionary<ulong, Inmate> Prisoners = new Dictionary<ulong, Inmate>();
            public Dictionary<string, Prison> prisons = new Dictionary<string, Prison>();
        }
        void SaveData()
        {
            JailData.WriteObject(jailData);
        }
        void LoadData()
        {
            try
            {
                jailData = Interface.GetMod().DataFileSystem.ReadObject<JailDataStorage>("jail_data");
                foreach (var entry in jailData.prisons)
                    prisonIDs.Add(entry.Value.zoneID);
            }
            catch
            {
                jailData = new JailDataStorage();
            }            
        }
        class Inmate
        {
            public Vector3 initialPos;
            public string prisonName;
            public int cellNumber;
            public double expireTime;
            public List<InvItem> savedInventory = new List<InvItem>();            
        }
        public class Prison
        {
            public string zoneID;
            public float zoneRadius;
            public string spawnFile;
            public Dictionary<int, bool> freeCells = new Dictionary<int, bool>();
            public Vector3 location;
        }
        class InvItem
        {
            public int itemid;
            public ulong skinid;
            public string container;
            public int amount;
            public bool weapon;
            public int ammo;
            public string ammotype;
            public List<int> mods;
            public float condition;

            public InvItem()
            {
            }
        }
        class JailTimer
        {
            public Timer time;
            public int timeLeft;
        }
        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        } // borrowed from ZoneManager
        #endregion

        #region Inventory Saving and Restoration
        public void SaveInventory(BasePlayer player)
        {
            jailData.Prisoners[player.userID].savedInventory.Clear();
            List<InvItem> kititems = new List<InvItem>();
            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (item != null)
                {
                    var iteminfo = AddItemToSave(item, "wear");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item != null)
                {
                    var iteminfo = AddItemToSave(item, "main");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item != null)
                {
                    var iteminfo = AddItemToSave(item, "belt");
                    kititems.Add(iteminfo);
                }
            }
            jailData.Prisoners[player.userID].savedInventory = kititems;            
        }
        private InvItem AddItemToSave(Item item, string container)
        {
            InvItem iItem = new InvItem();
            iItem.ammo = 0;
            iItem.amount = item.amount;
            iItem.mods = new List<int>();
            iItem.skinid = item.skin;
            iItem.container = container;
            iItem.condition = item.condition;
            iItem.itemid = item.info.itemid;
            iItem.weapon = false;

            if (item.info.category.ToString() == "Weapon")
            {
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (weapon.primaryMagazine != null)
                    {
                        iItem.weapon = true;
                        iItem.ammo = weapon.primaryMagazine.contents;
                        if (item.contents != null)
                            foreach (var mod in item.contents.itemList)
                            {
                                if (mod.info.itemid != 0)
                                    iItem.mods.Add(mod.info.itemid);
                            }
                    }
                }
            }
            return iItem;
        }
        public void RestoreInventory(BasePlayer player)
        {
            player.inventory.Strip();
            foreach (InvItem kitem in jailData.Prisoners[player.userID].savedInventory)
            {
                if (kitem.weapon)
                    player.inventory.GiveItem(BuildWeapon(kitem.itemid, kitem.ammo, kitem.skinid, kitem.mods, kitem.condition), kitem.container == "belt" ? player.inventory.containerBelt : kitem.container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);
                else player.inventory.GiveItem(BuildItem(kitem.itemid, kitem.amount, kitem.skinid, kitem.condition), kitem.container == "belt" ? player.inventory.containerBelt : kitem.container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);
            }
        }
        private Item BuildItem(int itemid, int amount, ulong skin, float cond)
        {
            if (amount < 1) amount = 1;
            Item item = ItemManager.CreateByItemID(itemid, amount, skin);
            item.conditionNormalized = cond;
            return item;
        }
        private Item BuildWeapon(int id, int ammo, ulong skin, List<int> mods, float cond)
        {
            Item item = ItemManager.CreateByItemID(id, 1, skin);
            item.conditionNormalized = cond;
            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = ammo;
            }
            if (mods != null)
                foreach (var mod in mods)
                {
                    item.contents.AddItem(BuildItem(mod, 1, 0, cond).info, 1);
                }

            return item;
        }
        #endregion

        #region Localization
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "<color=#afff00>Jail</color> : " },
            {"noPlayers", "No players found." },
            {"multiPlayers", "Multiple players found." },
            {"invalidZID", "Invalid zone ID : " },
            {"invalidZPos", "Unable to retrieve the zone location" },
            {"invalidZRad", "Unable to retrieve the zone radius" },
            {"newJailAdd", "New Jail added!" },
            {"invalidSF", "Invalid spawnfile : " },
            {"remJail", "Removed Jail : " },
            {"noJail", "No prison found with that name" },
            {"sentPrison", "You are being sent to prison!" },
            {"checkTime", "Use /jail to check how much time you have left to serve" },
            {"relPrison", "You are being released from prison!" },
            {"welcomeJail", "Welcome to prison {0}" },
            {"keepOut", "Keep Out! No visitors allowed in prison" },
            {"keepIn", "You are not allowed to leave the prison" },
            {"noRelease", "You are stuck in prison until a admin releases you" },
            {"mins", "Minutes" },
            {"secs", "Seconds" },
            {"remainJail", "You have {0} {1} remaining of your prison sentence" },
            {"synSend1", "/jail send <playername> <opt:time> <opt:reason> <opt:prisonname> - Send a player to Jail, time and prison name are optional" },
            {"synFree", "/jail free <playername> - Free a player from Jail" },
            {"synAdd", "/jail add <prisonname> <zoneID> <spawnfile> - Create a new prison" },
            {"synRem", "/jail remove <prisonname> - Remove a prison" },
            {"synZone", "/jail zone <radius> - Create a prison zone with required flags" },
            {"synList", "/jail list - Lists all prison's" },
            {"synWipe", "/jail wipe - Wipe's all prison data" },
            {"synPurge", "/jail purge - Remove inactive players from prison" },
            {"noPrisons", "The are no prisons available!" },
            {"sentTo", "{0} has been sent to {1}" },
            {"sentToBroadcast", "{0} has been sent to jail for: {1}" },
            {"noCells", "The are no free cells available at {0}" },
            {"noPos", "Unable to find a valid spawn point at {0}" },
            {"relFrom", "{0} has been released from prison" },
            {"ff", "You can not hurt other inmates" },
            {"createJail", "You have successfully created a new prison zone, you can edit this zone with /zone_edit" },
            {"jailID", "Zone ID: {0}" }
        };
        #endregion
    }
}
