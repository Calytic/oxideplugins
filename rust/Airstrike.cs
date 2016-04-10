using System;
using System.Collections.Generic;
using UnityEngine;
using Rust;
using System.Reflection;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Airstrike", "k1lly0u", "0.2.2", ResourceId = 1489)]
    class Airstrike : RustPlugin
    {

        #region fields
        [PluginReference]
        Plugin Economics;

        private bool changed;
        private bool strikeCalled;        
        private float rocketDrop = 0f;

        private FieldInfo strikePlanesecondsToTake;
        private FieldInfo strikePlanestartPos;
        private FieldInfo strikePlaneendPos;
        private FieldInfo strikePlanedropPosition;
        
        public Dictionary<ulong, bool> toggleList = new Dictionary<ulong, bool>();
        private List<CargoPlane> strikePlanesInAir = new List<CargoPlane>();
        private List<CargoPlane> massStrikePlanesInAir = new List<CargoPlane>();

        PlayerCooldown pcdData;
        private DynamicConfigFile PCDDATA;
        #endregion
        #region oxide hooks
        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            broadcastStrikeAll = true;
            strikeCalled = false;
            permission.RegisterPermission("airstrike.admin", this);
            permission.RegisterPermission("airstrike.canuse", this);
            permission.RegisterPermission("airstrike.buystrike", this);
            permission.RegisterPermission("airstrike.mass", this);
            lang.RegisterMessages(messages, this);

            strikePlanestartPos = typeof(CargoPlane).GetField("startPos", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            strikePlaneendPos = typeof(CargoPlane).GetField("endPos", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            strikePlanesecondsToTake = typeof(CargoPlane).GetField("secondsToTake", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            strikePlanedropPosition = typeof(CargoPlane).GetField("dropPosition", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

            LoadVariables();
            LoadData();
            PCDDATA = Interface.Oxide.DataFileSystem.GetFile("airstrike_data");
        }
        void OnServerInitialized()
        {           
            if (Economics == null)
            {
                if (useEconomics)
                {
                    PrintWarning($"Economics could not be found! Disabling money feature");
                    useEconomics = false;
                }
            }
        }
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            SaveData();
            foreach (CargoPlane plane in strikePlanesInAir)
                plane.KillMessage();
            foreach (CargoPlane plane in massStrikePlanesInAir)
                plane.KillMessage();
            strikePlanesInAir.Clear();
            massStrikePlanesInAir.Clear();      
        }
        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if ((useSignalStrike) && (canSmokeStrike(player)))
            {
                if (toggleList.ContainsKey(player.userID) && (toggleList[player.userID] == true))
                {
                    if (entity.name.Contains(lang.GetMessage("Only change this is your supply signal name is in a differant language", this)))
                    {
                        if (useCooldown)
                            if (!CheckPlayerData(player)) return;

                        timer.Once(3, () => strikeOnSmoke(player, entity));
                        return;
                    }
                }                
            }
        }
        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity == null) return;
            if (entity is SupplyDrop)
            {
                var drop = entity as SupplyDrop;
                               
                checkStrikeDrop(drop);
            }
            if (strikeCalled == true)
            {
                if (entity is CargoPlane)
                {
                    var plane = entity.GetComponent<CargoPlane>();
                    if (plane == null) return;
                    if (!strikePlanesInAir.Contains(plane))
                    {
                        plane.KillMessage();
                    }
                }                
            }
        }
        #endregion
        #region airstrike functions
        //////////////////////////////////////////////////////////////////////////////////////
        // Airstrike /////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        public const string cargoPlanePrefab = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";    
        
        private void removePlaneFromList(CargoPlane plane)
        {
            strikePlanesInAir.Remove(plane);
            if (massStrikePlanesInAir.Contains(plane))
                massStrikePlanesInAir.Remove(plane);           
        }        
        public static CargoPlane createStrikePlane()
        {
            var newPlane = (CargoPlane)GameManager.server.CreateEntity(cargoPlanePrefab, new Vector3(), new Quaternion(), true);
            return newPlane;
        }
        private void checkPlaneDistance(CargoPlane strikePlane, Vector3 targetPos)
        {
            var currentPos = strikePlane.transform.position;
            if (Vector3.Distance(currentPos, targetPos) < (currentPos.y + planeDistance))
            {
                fireRockets(strikePlane, targetPos);
                return;
            }
            timer.Once(3, () => checkPlaneDistance(strikePlane, targetPos));
        }
        
        private void fireRockets(CargoPlane strikePlane, Vector3 targetPos)
        {
            if (!strikeCalled) strikeCalled = true;
            timer.Repeat(rocketInterval, rocketAmount, () => spreadRockets(strikePlane, targetPos));
            timer.Once(10, () => strikeCalled = false);           
        }
        private void spreadRockets(CargoPlane strikePlane, Vector3 targetPos)
        {
            targetPos = Quaternion.Euler(UnityEngine.Random.Range((float)(-rocketSpread * 0.2), rocketSpread * 0.2f), UnityEngine.Random.Range((float)(-rocketSpread * 0.2), rocketSpread * 0.2f), UnityEngine.Random.Range((float)(-rocketSpread * 0.2), rocketSpread * 0.2f)) * targetPos;
            createRocket(strikePlane, targetPos);
        }
        private BaseEntity createRocket(CargoPlane strikePlane, Vector3 targetPos)
        {
            var rocket = rocketType;
            if (useMixedRockets)
            {

                int rand = UnityEngine.Random.Range(1, fireChance);
                if (rand == 1)
                    rocket = fireRocket;
            }
            var launchPos = strikePlane.transform.position;

            ItemDefinition projectileItem = ItemManager.FindItemDefinition(rocket);
            ItemModProjectile component = projectileItem.GetComponent<ItemModProjectile>();

            BaseEntity entity = GameManager.server.CreateEntity(component.projectileObject.resourcePath, launchPos, new Quaternion(), true);                            
            
            TimedExplosive rocketExplosion = entity.GetComponent<TimedExplosive>();
            ServerProjectile rocketProjectile = entity.GetComponent<ServerProjectile>();            
                       
            rocketProjectile.speed = rocketSpeed;
            rocketProjectile.gravityModifier = rocketDrop;
            rocketExplosion.timerAmountMin = 60;
            rocketExplosion.timerAmountMax = 60;

            Vector3 newDirection = (targetPos - launchPos);
            scaleRocketDamage(rocketExplosion.damageTypes, damageModifier);

            entity.SendMessage("InitializeVelocity", (newDirection));
            entity.Spawn(true);

            return null;
        }
        private void scaleRocketDamage(List<DamageTypeEntry> damageTypes, float scale)
        {
            for (int i = 0; i < damageTypes.Count; i++)
            {
                damageTypes[i].amount *= scale;
            }
        }
        public bool checkStrikeDrop(SupplyDrop drop)
        {
            bool istrue = false;
            var supplyDrop = drop.GetComponent<LootContainer>();
            Vector3 dropPosition = supplyDrop.transform.position;            
            foreach (var plane in strikePlanesInAir)
            {
                float dropDistance = ((Vector3.Distance(plane.transform.position, dropPosition)));
                if (dropDistance < 100) supplyDrop.KillMessage();
                istrue = true;
            }
            return istrue;
        }
        private void massSet(CargoPlane plane, Vector3 position, Vector3 offset, int speed = -1)
        {
            if (speed == -1) speed = planeSpeed;
            CargoPlane strikePlane = plane.GetComponent<CargoPlane>();
            var spawnPos = calculateSpawnPos(position, offset);
            Vector3 endPos = calculateEndPos(position, offset);

            strikePlane.Spawn();

            strikePlane.InitDropPosition(position + offset);
            strikePlanestartPos.SetValue(strikePlane, spawnPos);
            strikePlaneendPos.SetValue(strikePlane, endPos);
            strikePlane.transform.position = spawnPos;
            strikePlane.transform.rotation = new Quaternion(0, 180, 0, 0);
            strikePlanesecondsToTake.SetValue(plane, Vector3.Distance((Vector3)strikePlanestartPos.GetValue(plane), (Vector3)strikePlaneendPos.GetValue(plane)) / speed);

            checkPlaneDistance(plane, position);
            float removePlane = ((Vector3.Distance((Vector3)strikePlanestartPos.GetValue(plane), (Vector3)strikePlanedropPosition.GetValue(plane)) / speed) + 10);
            timer.Once(removePlane, () => removePlaneFromList(plane));
            timer.Once(10, () => strikeCalled = false);
        }
        private Vector3 calculateSpawnPos(Vector3 pos, Vector3 offset)
        {
            float mapSize = (TerrainMeta.Size.x / 2) + 150f;
            Vector3 spawnPos = new Vector3();
            spawnPos.x = pos.x + offset.x;
            spawnPos.z = mapSize + offset.z;
            spawnPos.y = 150;
            return spawnPos;
        }
        private Vector3 calculateEndPos(Vector3 pos, Vector3 offset)
        {
            float mapSize = (TerrainMeta.Size.x / 2);
            Vector3 endPos = new Vector3();
            endPos.x = pos.x + offset.x;
            endPos.z = -mapSize;
            endPos.y = 150;
            return endPos;
        }
        private void strikeOnPayment(BasePlayer player, string type)
        {
            int playerHQ = player.inventory.GetAmount(374890416);
            int playerFlare = player.inventory.GetAmount(97513422);
            int playerTC = player.inventory.GetAmount(1490499512);

            if (type == "computer")
            {
                if ((playerFlare >= buyFlare) && (playerTC >= buyTarget))
                {
                    player.inventory.Take(null, 97513422, buyFlare);
                    player.inventory.Take(null, 1490499512, buyTarget);
                    callPaymentStrike(player);
                    return;
                }
            }
            else if (type == "metal")
            {
                if ((playerHQ >= buyMetal))
                {
                    player.inventory.Take(null, 374890416, buyMetal);
                    callPaymentStrike(player);
                    return;
                }
            }
            else if (type == "money")
            {
                callPaymentStrike(player);
                return;
            }
            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("buyCost", this, player.UserIDString));
            SendReply(player, string.Format(lang.GetMessage("buyFlare", this, player.UserIDString), buyTarget.ToString(), buyFlare.ToString()));
            SendReply(player, string.Format(lang.GetMessage("buyMetal", this, player.UserIDString), buyMetal.ToString()));
        }
        private void squadStrikeOnPayment(BasePlayer player, bool type)
        {
            int playerHQ = player.inventory.GetAmount(374890416);
            int playerFlare = player.inventory.GetAmount(97513422);
            int playerTC = player.inventory.GetAmount(1490499512);

            if (type == false)
            {
                if ((playerFlare >= buyFlare) && (playerTC >= buyTarget))
                {
                    player.inventory.Take(null, 97513422, buyFlare * 3);
                    player.inventory.Take(null, 1490499512, buyTarget * 3);
                    callPaymentSquadStrike(player);
                    return;
                }
            }
            else if (type == true)
            {
                if ((playerHQ >= buyMetal))
                {
                    player.inventory.Take(null, 374890416, buyMetal * 3);
                    callPaymentSquadStrike(player);
                    return;
                }
            }
            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("squadCost", this, player.UserIDString));
            SendReply(player, string.Format(lang.GetMessage("buyFlare", this, player.UserIDString), (buyTarget * 3).ToString(), (buyFlare * 3).ToString()));
            SendReply(player, string.Format(lang.GetMessage("buyMetal", this, player.UserIDString), (buyMetal * 3).ToString()));
        }
        private bool CheckPlayerMoney(BasePlayer player, int amount)
        {
            if (useEconomics)
            {
                double money = (double)Economics?.CallHook("GetPlayerMoney", player.userID);
                if (money >= amount)
                {
                    money = money - amount;
                    Economics?.CallHook("Set", player.userID, money);
                    return true;
                }
                return false;
            }
            return false;
        }

        private bool CheckPlayerData(BasePlayer player)
        {
            if (noAdminCooldown)
            {
                if (player.net.connection.authLevel >= auth) return true;
            }
            var d = pcdData.pCooldown;
            ulong ID = player.userID;
            double timeStamp = GrabCurrentTime();
            
            if (!d.ContainsKey(ID))
            {
                d.Add(ID, new PCDInfo((long)timeStamp + cooldownTime));
                SaveData();
                return true;
            }
            else
            {
                long time = d[ID].Cooldown;
                if (time > timeStamp && time != 0.0)
                {
                    SendReply(player, string.Format(lang.GetMessage("title", this) + lang.GetMessage("cdTime", this, player.UserIDString), (int)(time - timeStamp) / 60));
                    return false;
                }
                else
                {
                    d[ID].Cooldown = (long)timeStamp + cooldownTime;
                    SaveData();
                    return true;
                }
            }
        }
        static double GrabCurrentTime()
        {
            return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        #endregion
        #region callstrike functions
        //////////////////////////////////////////////////////////////////////////////////////
        // Call Strike Funtions  /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private void strikeOnPlayer(BasePlayer player)
        {
            callStrike(player.transform.position);
            SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("strikeConfirmed", this, player.UserIDString), player.transform.position.ToString()));
            Puts(player.displayName.ToString() + lang.GetMessage("calledStrike", this, player.UserIDString) + player.transform.position.ToString());
        }
        private void squadStrikeOnPlayer(BasePlayer player)
        {
            massStrike(player.transform.position);
            SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("strikeConfirmed", this, player.UserIDString), player.transform.position.ToString()));
            Puts(player.displayName.ToString() + lang.GetMessage("calledMassStrike", this, player.UserIDString) + player.transform.position.ToString());
        }
        private void strikeOnSmoke(BasePlayer player, BaseEntity entity)
        {
            Vector3 strikePos = entity.GetEstimatedWorldPosition();
            if (changeSupplyStrike)
                massStrike(strikePos);
            else callStrike(strikePos);
            SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("strikeConfirmed", this, player.UserIDString), strikePos.ToString()));
            Puts(player.displayName.ToString() + lang.GetMessage("calledStrike", this, player.UserIDString) + strikePos.ToString());
        }
        private void callStrike(Vector3 position, int speed = -1)
        {
            if (speed == -1) speed = planeSpeed;
            if (broadcastStrikeAll)
                PrintToChat(lang.GetMessage("strikeInbound", this));

            Puts(lang.GetMessage("calledTo", this), position.ToString());

            strikeCalled = true;

            var plane = createStrikePlane();
            strikePlanesInAir.Add(plane);

            CargoPlane strikePlane = plane.GetComponent<CargoPlane>();

            strikePlane.InitDropPosition(position);
            strikePlane.Spawn();
            strikePlanesecondsToTake.SetValue(plane, Vector3.Distance((Vector3)strikePlanestartPos.GetValue(plane), (Vector3)strikePlaneendPos.GetValue(plane)) / speed);

            checkPlaneDistance(plane, position);
            float removePlane = ((Vector3.Distance((Vector3)strikePlanestartPos.GetValue(plane), (Vector3)strikePlanedropPosition.GetValue(plane)) / speed) + 10);            
            timer.Once(removePlane, () => removePlaneFromList(plane));
            timer.Once(10, () => strikeCalled = false);            
        }
        private void massStrike(Vector3 position)
        {
            if (broadcastStrikeAll)
                PrintToChat(lang.GetMessage("strikeInbound", this));

            Puts(lang.GetMessage("calledTo", this), position.ToString());
                        
            Vector3 offsetLeft = new Vector3(-70, 0, 80);
            Vector3 offsetRight = new Vector3(70, 0, 80);
            strikeCalled = true;
            var plane = createStrikePlane();
            strikePlanesInAir.Add(plane);
            massStrikePlanesInAir.Add(plane);
            massSet(plane, position, new Vector3());

            var planeLeft = createStrikePlane();
            strikePlanesInAir.Add(planeLeft);
            massStrikePlanesInAir.Add(planeLeft);
            massSet(planeLeft, position, offsetLeft);

            var planeRight = createStrikePlane();
            strikePlanesInAir.Add(planeRight);
            massStrikePlanesInAir.Add(planeRight);
            massSet(planeRight, position, offsetRight);


        }       
        private void callPaymentStrike(BasePlayer player)
        {
            var pos = player.transform.position;
            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", pos);
            callStrike(pos);
            SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("strikeConfirmed", this, player.UserIDString), pos.ToString()));
            Puts(player.displayName.ToString() + lang.GetMessage("calledStrike", this, player.UserIDString) + pos.ToString());
            return;
        }
        private void callPaymentSquadStrike(BasePlayer player)
        {
            var pos = player.transform.position;
            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", pos);
            massStrike(pos);
            SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("squadConfirmed", this, player.UserIDString), pos.ToString()));
            Puts(player.displayName.ToString() + lang.GetMessage("calledMassStrike", this, player.UserIDString) + pos.ToString());
            return;
        }
        private void callRandomStrike(bool single)
        {
            float mapSize = (TerrainMeta.Size.x / 2) - 600f;

            float randomX = UnityEngine.Random.Range(-mapSize, mapSize);
            float randomY = UnityEngine.Random.Range(-mapSize, mapSize);

            Vector3 pos = new Vector3(randomX, 0f, randomY);

            if (single)callStrike(pos);
            if (!single) massStrike(pos);            
        }      

        //////////////////////////////////////////////////////////////////////////////////////
        // Find players by name //////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        List<BasePlayer> FindPlayer(string arg)
        {
            var foundPlayers = new List<BasePlayer>();

            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (steamid != 0L)
                    if (player.userID == steamid)
                    {
                        foundPlayers.Clear();
                        foundPlayers.Add(player);
                        return foundPlayers;
                    }
                string lowername = player.displayName.ToLower();
                if (lowername.Contains(lowerarg))
                {
                    foundPlayers.Add(player);
                }
            }
            return foundPlayers;
        }
        public bool isStrikePlane(CargoPlane plane)
        {
            Puts("called");
            if (massStrikePlanesInAir.Contains(plane)) return true;
            else if (strikePlanesInAir.Contains(plane)) return true;
            else return false;
        }

        #endregion
        #region chat/console commands and auth/perms
        //////////////////////////////////////////////////////////////////////////////////////
        // Permission/Auth Check /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        bool canChatStrike(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "airstrike.admin")) return true;
            else if (player.net.connection.authLevel >= auth) return true;
            return false;
        }
        bool canSquadStrike(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "airstrike.mass")) return true;
            else if (player.net.connection.authLevel >= auth) return true;
            return false;
        }
        bool canSmokeStrike(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "airstrike.canuse")) return true;
            else if (player.net.connection.authLevel >= auth) return true;
            return false;
        }
        bool canBuyStrike(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "airstrike.buystrike")) return true;
            else if (player.net.connection.authLevel >= auth) return true;
            return false;
        }
        bool isAuth(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < auth)
                {
                    SendReply(arg, lang.GetMessage("noPerms", this));
                    return false;
                }
            }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat/Console Commands /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("callstrike")]
        private void chatStrike(BasePlayer player, string command, string[] args)
        {
            if (!canChatStrike(player)) return;

            if (useCooldown)
                if (!CheckPlayerData(player)) return;

            if (args.Length == 0)
            {
                strikeOnPlayer(player);                
                return;
            }           
            if (args.Length > 1)
            {
                SendReply(player, lang.GetMessage("badSyntax", this, player.UserIDString));
                SendReply(player, lang.GetMessage("callStrike", this, player.UserIDString));
                SendReply(player, lang.GetMessage("callStrikeName", this, player.UserIDString));
                return;
            }
            var fplayer = FindPlayer(args[0]);
            if (fplayer.Count == 0)
            {
                SendReply(player, lang.GetMessage("noPlayers", this, player.UserIDString));
                return;
            }
            if (fplayer.Count > 1)
            {
                SendReply(player, lang.GetMessage("multiplePlayers", this, player.UserIDString));
                return;
            }
            foreach (BasePlayer targetPlayer in fplayer)
            {
                strikeOnPlayer(targetPlayer);
            }
        }
        [ChatCommand("squadstrike")]
        private void chatsquadStrike(BasePlayer player, string command, string[] args)
        {
            if (!canChatStrike(player)) return;
            if (args.Length == 0)
            {
                squadStrikeOnPlayer(player);
                return;
            }
            if (args.Length > 1)
            {
                SendReply(player, lang.GetMessage("badSyntax", this, player.UserIDString));
                SendReply(player, lang.GetMessage("callStrike", this, player.UserIDString));
                SendReply(player, lang.GetMessage("callStrikeName", this, player.UserIDString));
                return;
            }
            var fplayer = FindPlayer(args[0]);
            if (fplayer.Count == 0)
            {
                SendReply(player, lang.GetMessage("noPlayers", this, player.UserIDString));
                return;
            }
            if (fplayer.Count > 1)
            {
                SendReply(player, lang.GetMessage("multiplePlayers", this, player.UserIDString));
                return;
            }
            foreach (BasePlayer targetPlayer in fplayer)
            {
                squadStrikeOnPlayer(targetPlayer);
            }
        }
        [ChatCommand("buystrike")]
        private void chatBuyStrike(BasePlayer player, string command, string[] args)
        {
            if (usePaymentStrike == true)
            {
                if (!canBuyStrike(player)) return;

                if (useCooldown)
                    if (!CheckPlayerData(player)) return;

                if (args.Length == 0)
                {
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("selectPayment", this, player.UserIDString));
                    SendReply(player, lang.GetMessage("selectOptions", this, player.UserIDString));
                    if (Economics && useEconomics)
                    {
                        SendReply(player, lang.GetMessage("selectMoney", this, player.UserIDString));
                    }
                    return;
                }               

                if (args.Length == 1)
                {                   
                    if (args[0].ToLower() == "money")
                    {
                        if (CheckPlayerMoney(player, buyMoney))
                        {
                            strikeOnPayment(player, "money");
                                return;
                        }
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noMoney", this, player.UserIDString));
                        return;                        
                    }
                    if (args[0].ToLower() == "squad")
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("squadSyntax", this, player.UserIDString));
                        return;
                    }
                    var paymentType = args[0].ToUpper();
                    if (paymentType != "METAL" && paymentType != "COMPUTER")
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("validType", this, player.UserIDString));
                        return;
                    }
                    if (paymentType == "METAL")
                    {
                        strikeOnPayment(player, "metal");
                        return;
                    }
                    else if (paymentType == "COMPUTER")
                    {
                        strikeOnPayment(player, "computer");
                        return;
                    }
                }
                else if (args.Length == 2)
                {
                    if (args[0].ToLower() != "squad")
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("squadSyntax", this, player.UserIDString));
                        return;
                    }
                    if (usePaymentSquad)
                    {
                        var paymentType = args[1].ToUpper();
                        if (paymentType != "METAL" && paymentType != "COMPUTER")
                        {
                            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("validType", this, player.UserIDString));
                            return;
                        }
                        if (paymentType == "METAL")
                        {
                            squadStrikeOnPayment(player, true);
                            return;
                        }
                        else if (paymentType == "COMPUTER")
                        {
                            squadStrikeOnPayment(player, false);
                            return;
                        }
                    }
                }
                else
                {
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("badSyntax", this, player.UserIDString));
                    SendReply(player, lang.GetMessage("selectOptions", this, player.UserIDString));
                    if (Economics && useEconomics)
                    {
                        SendReply(player, lang.GetMessage("selectMoney", this, player.UserIDString));
                    }
                    return;
                }
            }
        }
        [ChatCommand("togglestrike")]
        private void chatToggleStrike(BasePlayer player, string command, string[] args)
        {
            if (!canSmokeStrike(player)) return;
            if (!toggleList.ContainsKey(player.userID)) toggleList.Add(player.userID, false);

            string reply = "";
            if (toggleList[player.userID] == true) reply = "ON";
            else reply = "OFF";

            if (args.Length == 0)
            {
                SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("onOff", this, player.UserIDString), reply));
                return;
            }            
            else if (args.Length == 1)
            {
                var toggleString = args[0].ToUpper();
                if (toggleString != "ON")
                {
                    reply = "OFF";
                    toggleList[player.userID] = false;
                    SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("onOff", this, player.UserIDString), reply));
                    return;
                }
                if (toggleString == "ON")
                {
                    toggleList[player.userID] = true;
                    SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("onOff", this, player.UserIDString), toggleString));
                    return;
                }                
            }
            else if (args.Length > 1)
            {
                SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("badSyntax", this, player.UserIDString));
                SendReply(player, lang.GetMessage("toggleOn", this, player.UserIDString));
                return;
            }

        }
        [ConsoleCommand("airstrike")]
        void ccmdAirstrike(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                callRandomStrike(true);
                return;
            }

            if (arg.Args.Length == 1)
            {
                var fplayer = FindPlayer(arg.Args[0]);
                if (fplayer.Count == 0)
                {
                    SendReply(arg, lang.GetMessage("noPlayers", this));
                    return;
                }
                if (fplayer.Count > 1)
                {
                    SendReply(arg, lang.GetMessage("multiplePlayers", this));
                    return;
                }
                foreach (BasePlayer targetPlayer in fplayer)
                {
                    strikeOnPlayer(targetPlayer);
                    return;
                }
            }

            if (arg.Args.Length == 3)
            {
                float x;
                float y;
                float z;
                try
                {
                    x = Convert.ToSingle(arg.Args[0]);
                    y = Convert.ToSingle(arg.Args[1]);
                    z = Convert.ToSingle(arg.Args[2]);
                }
                catch (FormatException ex)
                {
                    SendReply(arg, lang.GetMessage("coordNum", this));
                    return;
                }
                Vector3 pos = new Vector3(x, y, z);
                callStrike(pos);
                return;
            }

            if (arg.Args.Length == 2 || arg.Args.Length >= 4)
            {
                SendReply(arg, lang.GetMessage("badSyntax", this));
                SendReply(arg, lang.GetMessage("airStrike", this));
                SendReply(arg, lang.GetMessage("airStrikeName", this));
                SendReply(arg, lang.GetMessage("airStrikeCoords", this));
                return;
            }
            
        }
        [ConsoleCommand("squadstrike")]
        void ccmdsquadstrike(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                callRandomStrike(false);
                return;
            }

            if (arg.Args.Length == 1)
            {
                var fplayer = FindPlayer(arg.Args[0]);
                if (fplayer.Count == 0)
                {
                    SendReply(arg, lang.GetMessage("noPlayers", this));
                    return;
                }
                if (fplayer.Count > 1)
                {
                    SendReply(arg, lang.GetMessage("multiplePlayers", this));
                    return;
                }
                foreach (BasePlayer targetPlayer in fplayer)
                {
                    massStrike(targetPlayer.transform.position);
                    return;
                }
            }

            if (arg.Args.Length == 3)
            {
                float x;
                float y;
                float z;
                try
                {
                    x = Convert.ToSingle(arg.Args[0]);
                    y = Convert.ToSingle(arg.Args[1]);
                    z = Convert.ToSingle(arg.Args[2]);
                }
                catch (FormatException ex)
                {
                    SendReply(arg, lang.GetMessage("coordNum", this));
                    return;
                }
                Vector3 pos = new Vector3(x, y, z);
                massStrike(pos);
                return;
            }

            if (arg.Args.Length == 2 || arg.Args.Length >= 4)
            {
                SendReply(arg, lang.GetMessage("badSyntax", this));
                SendReply(arg, lang.GetMessage("airStrike", this));
                SendReply(arg, lang.GetMessage("airStrikeName", this));
                SendReply(arg, lang.GetMessage("airStrikeCoords", this));
                return;
            }

        }
        #endregion
        #region class and datamanagement
        //////////////////////////////////////////////////////////////////////////////////////
        // Player class and data management //////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        class PlayerCooldown
        {
            public Dictionary<ulong, PCDInfo> pCooldown = new Dictionary<ulong, PCDInfo>();
            public PlayerCooldown() { }
        }
        class PCDInfo
        {
            public long Cooldown;
            public PCDInfo() { }
            public PCDInfo(long cd)
            {
                Cooldown = cd;
            }
        }
        void SaveData()
        {
            PCDDATA.WriteObject(pcdData);
        }
        void LoadData()
        {
            try
            {
                pcdData = Interface.GetMod().DataFileSystem.ReadObject<PlayerCooldown>("airstrike_data");
            }
            catch
            {
                Puts("Couldn't load Airstrike data, creating new datafile");
                pcdData = new PlayerCooldown();
            }
        }
        #endregion
        #region config
        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        private bool broadcastStrikeAll = true;
        private bool useSignalStrike = true;
        private bool usePaymentStrike = true;
        private bool usePaymentSquad = true;
        private bool useEconomics = true;
        private bool useCooldown = false;
        private bool noAdminCooldown = true;
        private bool useMixedRockets = false;
        private bool changeSupplyStrike = false;

        private float rocketSpeed = 110f;
        private float rocketInterval = 0.6f;
        private float damageModifier = 1.0f;
        private float rocketSpread = 1.5f;

        private int rocketAmount = 15;
        private int planeSpeed = 105;
        private int planeDistance = 900;
        private int buyMetal = 1000;
        private int buyFlare = 2;
        private int buyTarget = 1;
        private int buyMoney = 500;
        private int cooldownTime = 3600;
        private int auth = 1;
        private int fireChance = 4;

        private string normalRocket = "ammo.rocket.basic";
        private string fireRocket = "ammo.rocket.fire";
        private string rocketType = "ammo.rocket.basic";

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Messages - Broadcast strike to all players", ref broadcastStrikeAll);
            CheckCfg("Rockets - Use both rocket types", ref useMixedRockets);
            CheckCfg("Rockets - Chance of fire rocket - 1 in... ", ref fireChance);
            CheckCfg("Supply Signals - Change the supply strike to call a Squadstrike", ref changeSupplyStrike);
            CheckCfg("Supply Signals - Use supply signals to call strike", ref useSignalStrike);
            CheckCfg("Buy - Can purchase airstrike", ref usePaymentStrike);
            CheckCfg("Buy - Can purchase squadstrike", ref usePaymentSquad);
            CheckCfg("Buy - Use Economics", ref useEconomics);
            CheckCfg("Cooldown - Use Cooldown", ref useCooldown);
            CheckCfg("Cooldown - Admin exempt from cooldown", ref noAdminCooldown);
            CheckCfg("Options - Minimum Authlevel", ref auth);

            CheckCfgFloat("Rockets - Speed of rockets", ref rocketSpeed);
            CheckCfgFloat("Rockets - Interval between rockets (seconds)", ref rocketInterval);
            CheckCfgFloat("Rockets - Damage Modifier", ref damageModifier);
            CheckCfgFloat("Rockets - Accuracy of rockets", ref rocketSpread);

            CheckCfg("Rockets - Amount of rockets to fire", ref rocketAmount);
            CheckCfg("Plane - Plane speed", ref planeSpeed);
            CheckCfg("Plane - Plane distance before firing", ref planeDistance);
            CheckCfg("Buy - Buy strike cost - HQ Metal", ref buyMetal);
            CheckCfg("Buy - Buy strike cost - Flare", ref buyFlare);
            CheckCfg("Buy - Buy strike cost - Targeting Computer", ref buyTarget);
            CheckCfg("Buy - Buy strike cost - Economics", ref buyMoney);
            CheckCfg("Rockets - Default rocket type", ref rocketType);
            CheckCfg("Cooldown - Cooldown timer", ref cooldownTime);
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
        //////////////////////////////////////////////////////////////////////////////////////
        // Messages //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "<color=orange>Airstrike</color> : "},
            {"strikeConfirmed", "Airstrike confirmed at co-ords: {0}!"},
            {"squadConfirmed", "Squadstrike confirmed at co-ords: {0}!"},
            {"strikeInbound", "Airstrike Inbound!"},
            {"buyCost", "To purchase a airstrike you need either;"},
            {"squadCost", "To purchase a squadstrike you need either;"},
            {"buyFlare", "{0} Targeting Computer(s) and {1} Flare(s)"},
            {"noMoney", "You do not have enough money to buy a strike" },
            {"buyMetal", "-or- {0} High Quality Metal"},
            {"noPerms", "You dont not have permission to use this command."},
            {"badSyntax", "Incorrect syntax:"},
            {"callStrike", "\"callstrike\" will call a strike on your location."},
            {"callStrikeName", "callstrike \"PLAYERNAME\" will call a strike on a player"},
            {"noPlayers", "No players found."},
            {"multiplePlayers", "Multiple players found"},
            {"selectPayment", "You must select a payment type"},
            {"selectOptions", "/buystrike metal -or- /buystrike computer"},
            {"selectMoney", "-or- /buystrike money" },
            {"validType", "Enter a valid payment type"},
            {"squadSyntax", "/buystrike squad metal -or- /buystrike squad computer"},
            {"onOff", "Signal airstrike is {0}!"},
            {"toggleOn", "\"airstrike\" \"on\" -or- \"off\""},
            {"coordNum", "Co-ordinates must be numbers!"},
            {"airStrike", "\"airstrike\" will call a random strike."},
            {"airStrikeName", "airstrike \"PLAYERNAME\" will call a strike on a player"},
            {"airStrikeCoords", "airstrike \"x y z\" will call a strike on a co-ordinates"},
            {"calledStrike", " has called a airstrike at co-ords: "},
            {"calledMassStrike", " has called a squadron airstrike at co-ords: "},
            {"calledTo", "Airstrike called to {0}" },
            {"cdTime", "You must wait another {0} minutes before using this command again" },
            {"Only change this is your supply signal name is in a differant language", "supply" }
        };
        #endregion


    }
}
