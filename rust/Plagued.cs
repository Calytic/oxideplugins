using System.Reflection;
using System.Collections.Generic;
using System;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Ext.SQLite;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Plagued", "Wernesgruner", "0.3.2")]
    [Description("Everyone is infected.")]

    class Plagued : RustPlugin
    {
        private static int plagueRange = 20;
        private static int plagueIncreaseRate = 5;
        private static int plagueDecreaseRate = 1;
        private static int plagueMinAffinity = 6000;
        private static int affinityIncRate = 10;
        private static int affinityDecRate = 1;
        private static int maxKin = 2;
        private static int maxKinChanges = 3;
        private static int playerLayer;

        private readonly FieldInfo serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

        // Get the buffer size from the Vis class using relfection. It should always be 8ko, but it might change in the future
        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic)).GetValue(null);

        //
        private Dictionary<ulong, PlayerState> playerStates;

        #region Hooks
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file (Plagued Mod)");
            Config.Clear();
            Config["plagueRange"] = 20;
            Config["plagueIncreaseRate"] = 5;
            Config["plagueDecreaseRate"] = 1;
            Config["plagueMinAffinity"] = 6000;
            Config["affinityIncRate"] = 10;
            Config["affinityDecRate"] = 1;
            Config["maxKin"] = 2;
            Config["maxKinChanges"] = 3;

            SaveConfig();
        }

        void Unload()
        {
            PlayerState.closeDatabase();
        }

        void OnServerInitialized()
        {
            PlayerState.setupDatabase(this);
            // Set the layer that will be used in the radius search. We only want human players in this case
            playerLayer = LayerMask.GetMask("Player (Server)");

            // Reload the player states
            playerStates = new Dictionary<ulong, PlayerState>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                playerStates.Add(player.userID, new PlayerState(player, null));
            }
            
            plagueRange = (int) Config["plagueRange"];
            plagueIncreaseRate = (int) Config["plagueIncreaseRate"];
            plagueDecreaseRate = (int) Config["plagueDecreaseRate"];
            plagueMinAffinity = (int) Config["plagueMinAffinity"];
            affinityIncRate = (int) Config["affinityIncRate"];
            affinityDecRate = (int) Config["affinityDecRate"];
            maxKin = (int) Config["maxKin"];
            maxKinChanges = (int) Config["maxKinChanges"];
        }

        void OnPlayerInit(BasePlayer player)
        {
            // Add the player to the player state list
            if (!playerStates.ContainsKey(player.userID))
            {
                PlayerState state = new PlayerState(player, stateRef => {
                    // The player was loaded in the current game session
                    playerStates.Add(player.userID, stateRef);
                    SendReply(player, "Welcome to plagued mod. Try the <color=#81F781>/plagued</color> command for more information.");
                    Puts(player.displayName + " has been plagued!");

                    // Add the proximity detector to the player
                    player.gameObject.AddComponent<ProximityDetector>();

                    return true;
                });
            } else
            {
                // Add the proximity detector to the player
                player.gameObject.AddComponent<ProximityDetector>();
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            ProximityDetector proximityDetector = player.gameObject.GetComponent<ProximityDetector>();
            proximityDetector.disableProximityCheck();
            //Puts(player.displayName + " is no longer watched!");
        }

        void OnRunPlayerMetabolism(PlayerMetabolism metabolism)
        {
            // 0 - 1000 -> Decreased Health Regen
            // 1000 - 2000 -> Increased hunger
            // 2000 - 3000 -> Increased thirst
            // 3000 - 4000 -> No Health Regen
            // 4000 - 5000 -> No comfort
            // 5000 - 6000 -> Increased Hunger 2
            // 6000 - 7000 -> Increased Thirst 2
            // 7000 - 8000 -> Cold
            // 8000 - 9000 -> Bleeding
            // 9000+ -> Poison

            /*
             * -- ----------------------------
             * -- Rust default rates
             * -- ----------------------------
             * -- healthgain = 0.03
             * -- caloriesloss = 0 - 0.05
             * -- hydrationloss = 0 - 0.025
             * -- ----------------------------
             */
            BasePlayer player = metabolism.GetComponent<BasePlayer>();
            PlayerState state = playerStates[player.userID];
            int plagueLevel = state.getPlagueLevel();
            float defaultHealthGain = 0.03f;
            float defaultCaloriesLoss = 0.05f;
            float defaultHydrationLoss = 0.025f;


            //Interface.Oxide.LogInfo("Infection stage " + (plagueLevel / 1000).ToString());

            if (plagueLevel == 0) return;

            if (plagueLevel <= 1) return;
            //Interface.Oxide.LogInfo("Infection stage 1 " + player.displayName + " " + player.userID);
            metabolism.pending_health.value = metabolism.pending_health.value + (defaultHealthGain / 2f);

            if (plagueLevel <= 1000) return;
            //Interface.Oxide.LogInfo("Infection stage 2");
            metabolism.calories.value = metabolism.calories.value - ((defaultCaloriesLoss * 3f) + (metabolism.heartrate.value / 10f));

            if (plagueLevel <= 2000) return;
            //Interface.Oxide.LogInfo("Infection stage 3");
            metabolism.hydration.value = metabolism.hydration.value - ((defaultHydrationLoss * 3f) + (metabolism.heartrate.value / 10f));

            if (plagueLevel <= 3000) return;
            metabolism.pending_health.value = metabolism.pending_health.value - (defaultHealthGain / 2f);

            if (plagueLevel <= 4000) return;
            //Interface.Oxide.LogInfo("Infection stage 5");
            metabolism.comfort.value = -1;

            if (plagueLevel <= 5000) return;
            //Interface.Oxide.LogInfo("Infection stage 6");
            metabolism.calories.value = metabolism.calories.value - ((defaultCaloriesLoss * 5f) + (metabolism.heartrate.value / 10f));

            if (plagueLevel <= 6000) return;
            //Interface.Oxide.LogInfo("Infection stage 7");
            metabolism.hydration.value = metabolism.hydration.value - ((defaultHydrationLoss * 5f) + (metabolism.heartrate.value / 10f));

            if (plagueLevel <= 7000) return;
            ///Interface.Oxide.LogInfo("Infection stage 8");
            metabolism.temperature.value = metabolism.temperature.value - 0.05f;

            if (plagueLevel <= 8000) return;
            //Interface.Oxide.LogInfo("Infection stage 9");
            metabolism.bleeding.value = metabolism.bleeding.value + 0.2f;

            if (plagueLevel < 10000) return;
            //Interface.Oxide.LogInfo("Infection stage 10");
            metabolism.poison.value = 2;
        }

        void OnPlayerProximity(BasePlayer player, BasePlayer[] players)
        {
            if (playerStates.ContainsKey(player.userID))
            {
                playerStates[player.userID].increasePlaguePenalty(players);
                //Puts(player.displayName + " is close to " + (players.Length - 1).ToString() + " other players!");
            }
        }

        void OnPlayerAlone(BasePlayer player)
        {
            //Puts("OnPlayerAlone: "+ player.userID);
            if (playerStates.ContainsKey(player.userID))
            {
                playerStates[player.userID].decreasePlaguePenalty();
            }
        }
        #endregion

        #region Commands
        [ChatCommand("plagued")]
        void cmdPlagued(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                SendReply(player, "<color=#81F781>/plagued addkin</color> => <color=#D8D8D8> Add the player you are looking at to your kin list.</color>");
                SendReply(player, "<color=#81F781>/plagued delkin</color> => <color=#D8D8D8> Remove the player you are looking at from your kin list.</color>");
                SendReply(player, "<color=#81F781>/plagued delkin</color> <color=#F2F5A9> number </color> => <color=#D8D8D8> Remove a player from your kin list by kin number.</color>");
                SendReply(player, "<color=#81F781>/plagued lskin</color> => <color=#D8D8D8> Display your kin list.</color>");
                SendReply(player, "<color=#81F781>/plagued lsassociates</color> => <color=#D8D8D8> Display your associates list.</color>");
                SendReply(player, "<color=#81F781>/plagued info</color> => <color=#D8D8D8> Display information about the workings of this mod.</color>");

                return;
            }

            if (args.Length >= 1)
            {
                switch (args[0])
                {
                    case "addkin":
                        cmdAddKin(player);
                        break;
                    case "delkin":
                        if (args.Length == 2)
                        {
                            int position;
                            if (int.TryParse(args[1], out position))
                            {
                                cmdDelKin(player, position);
                            }
                            else
                            {
                                SendReply(player, "Kin position must be a valid number!");
                            }
                        } else
                        {
                            cmdDelKin(player);
                        }
                        break;
                    case "lskin":
                        cmdListKin(player);
                        break;
                    case "lsassociates":
                        cmdListAssociates(player);
                        break;
                    case "info":
                        cmdInfo(player);
                        break;
                    default:
                        SendReply(player, "Invalid Plagued mod command.");
                        break;
                }
            }
        }

        private void cmdAddKin(BasePlayer player)
        {
            BasePlayer targetPlayer;

            if (getPlayerLookedAt(player, out targetPlayer))
            {
                PlayerState state = playerStates[player.userID];
                PlayerState targetPlayerState = playerStates[targetPlayer.userID];

                if (state.isKinByUserID(targetPlayer.userID))
                {
                    SendReply(player, targetPlayer.displayName + " is already your kin!");
                    return;
                }

                if (state.hasKinRequest(targetPlayer.userID))
                {
                    state.addKin(targetPlayer.userID);
                    targetPlayerState.addKin(player.userID);
                    SendReply(player, "You are now kin with " + targetPlayer.displayName + "!");
                    SendReply(targetPlayer, "You are now kin with " + player.displayName + "!");

                    return;
                } else
                {
                    targetPlayerState.addKinRequest(player.userID);
                    SendReply(player, "You have requested to be " + targetPlayer.displayName + "'s kin!");
                    SendReply(targetPlayer, player.displayName + " has requested to be your kin. Add him back to become kin!");

                    return;
                }

                SendReply(player, targetPlayer.displayName + " could not be added to kin!");
            }

        }

        private bool cmdDelKin(BasePlayer player)
        {
            BasePlayer targetPlayer;

            if (getPlayerLookedAt(player, out targetPlayer))
            {
                PlayerState state = playerStates[player.userID];
                PlayerState targetPlayerState = playerStates[targetPlayer.userID];

                if (!state.isKinByUserID(targetPlayer.userID))
                {
                    SendReply(player, targetPlayer.displayName + " is not your kin!");

                    return false;
                }

                if (state.removeKin(targetPlayer.userID) && targetPlayerState.forceRemoveKin(player.userID))
                {
                    SendReply(player, targetPlayer.displayName + " was removed from you kin list!");
                    SendReply(targetPlayer, player.displayName + " was removed from you kin list!");

                    return true;
                }

                SendReply(player, targetPlayer.displayName + " could not be removed from kin list (Exceeded max kin changes per restart)!");
            }

            return false;
        }

        private bool cmdDelKin(BasePlayer player, int id)
        {
            PlayerState state = playerStates[player.userID];

            if (state.removeKinById(id))
            {
                foreach(var item in playerStates)
                {
                    if (item.Value.getId() == id)
                    {
                        item.Value.forceRemoveKin(player.userID);
                    }
                }
                SendReply(player, "Successfully removed kin.");
            } else
            {
                SendReply(player, "Could not remove kin.");
            }
            
            return false;
        }

        private void cmdListKin(BasePlayer player)
        {
            List<string> kinList = playerStates[player.userID].getKinList();

            displayList(player, "Kin", kinList);
        }

        private void cmdListAssociates(BasePlayer player)
        {
            List<string> associatesList = playerStates[player.userID].getAssociatesList();
            displayList(player, "Associates", associatesList);
        }

        private bool cmdInfo(BasePlayer player)
        {
            SendReply(player, " ===== Plagued mod ======");
            SendReply(player, "An unknown airborne pathogen has decimated most of the population. You find yourself on a deserted island, lucky to be among the few survivors. But the biological apocalypse is far from being over. It seems that the virus starts to express itself when certain hormonal changes are triggered by highly social behaviors. It has been noted that small groups of survivor seems to be relatively unaffected, but there isn't one single town or clan that wasn't decimated.");
            SendReply(player, "Workings: \n The longer you hang around others, the sicker you'll get. However, your kin are unaffected, add your friends as kin and you will be able to collaborate. Choose your kin wisely, there are no big families in this world.");
            SendReply(player, "Settings: \n > Max kin : " + maxKin.ToString() + "\n" + " > Max kin changes / Restart : " + maxKinChanges.ToString());

            return false;
        }

        #endregion

        #region Helpers
        public static void MsgPlayer(BasePlayer player, string format, params object[] args)
        {
            if (player?.net != null) player.SendConsoleCommand("chat.add", 0, args.Length > 0 ? string.Format(format, args) : format, 1f);
        }

        public void displayList(BasePlayer player, string listName, List<string> stringList)
        {
            if (stringList.Count == 0)
            {
                SendReply(player, "You have no "+ listName.ToLower()+".");
                return;
            }

            string answerMsg = listName + " list: \n";

            foreach (string text in stringList)
            {
                answerMsg += "> " + text + "\n";
            }

            SendReply(player, answerMsg);
        }

        #endregion

        #region Geometry

        private bool getPlayerLookedAt(BasePlayer player, out BasePlayer targetPlayer)
        {
            targetPlayer = null;

            Quaternion currentRot;
            if (!TryGetPlayerView(player, out currentRot))
            {
                SendReply(player, "Couldn't get player rotation");
                return false;
            }

            object closestEnt;
            Vector3 closestHitpoint;
            if (!TryGetClosestRayPoint(player.transform.position, currentRot, out closestEnt, out closestHitpoint)) return false;
            targetPlayer = ((Collider)closestEnt).GetComponentInParent<BasePlayer>();

            if (targetPlayer == null)
            {
                SendReply(player, "You aren't looking at a player");
                return false;
            }

            return true;
        }

        private bool TryGetClosestRayPoint(Vector3 sourcePos, Quaternion sourceDir, out object closestEnt, out Vector3 closestHitpoint)
        {
            /**
             * Credit: Nogrod (HumanNPC)
             */
            Vector3 sourceEye = sourcePos + new Vector3(0f, 1.5f, 0f);
            Ray ray = new Ray(sourceEye, sourceDir * Vector3.forward);

            var hits = Physics.RaycastAll(ray);
            float closestdist = 999999f;
            closestHitpoint = sourcePos;
            closestEnt = false;
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider.GetComponentInParent<TriggerBase>() == null && hit.distance < closestdist)
                {
                    closestdist = hit.distance;
                    closestEnt = hit.collider;
                    closestHitpoint = hit.point;
                }
            }

            if (closestEnt is bool) return false;
            return true;
        }

        private bool TryGetPlayerView(BasePlayer player, out Quaternion viewAngle)
        {
            /**
             * Credit: Nogrod (HumanNPC)
             */
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);
            var input = serverinput.GetValue(player) as InputState;
            if (input?.current == null) return false;
            viewAngle = Quaternion.Euler(input.current.aimAngles);
            return true;
        }

        #endregion

        #region Data
        /**
         * This class handles the in-memory state of a player.
         */
        public class PlayerState
        {
            private static readonly Oxide.Ext.SQLite.Libraries.SQLite sqlite = Interface.GetMod().GetLibrary<Ext.SQLite.Libraries.SQLite>();
            private static Core.Database.Connection sqlConnection;
            private BasePlayer player;
            private int id;
            private int plagueLevel;
            private int kinChangesCount;
            private bool pristine;
            private Dictionary<ulong, Association> associations;
            private Dictionary<ulong, Kin> kins;
            private List<ulong> kinRequests;

            private const string UpdateAssociation = "UPDATE associations SET level=@0 WHERE associations.id = @1;";
            private const string InsertAssociation = "INSERT INTO associations (player_id,associate_id,level) VALUES (@0,@1,@2);";
            private const string CheckAssociationExists = "SELECT id FROM associations WHERE player_id == @0 AND associate_id == @1;";
            private const string DeleteAssociation = "DELETE FROM associations WHERE id=@0";
            private const string InsertPlayer = "INSERT OR IGNORE INTO players (user_id, name, plague_level, kin_changes_count, pristine) VALUES (@0, @1,0,0,1);";
            private const string SelectPlayer = "SELECT * FROM players WHERE players.user_id == @0;";
            private const string UpdatePlayerPlagueLevel = "UPDATE players SET plague_level=@0,pristine=@1 WHERE players.user_id == @2;";
            private const string SelectAssociations = @"
                SELECT associations.id, associations.player_id, associations.associate_id, associations.level, players.user_id, players.name
                FROM associations
                JOIN players ON associations.associate_id = players.id
                WHERE associations.player_id = @0
            ";
            private const string SelectKinList = @"
                SELECT kin.self_id, kin.kin_id, players.name as kin_name, players.user_id as kin_user_id
                FROM kin
                JOIN players ON kin.kin_id = players.id
                WHERE kin.self_id = @0
            ";
            private const string InsertKin = "INSERT INTO kin (self_id,kin_id) VALUES (@0,@1);";
            private const string DeleteKin = "DELETE FROM kin WHERE self_id=@0 AND kin_id=@1";
            private const string SelectKinRequestList = @"";

            /**
             * Retrieves a player from database and restore its store or creates a new database entry
             */
            public PlayerState(BasePlayer newPlayer, Func<PlayerState,bool> callback)
            {
                player = newPlayer;
                Interface.Oxide.LogInfo("Loading player: " + player.displayName);

                var sql = new Oxide.Core.Database.Sql();
                sql.Append(InsertPlayer, player.userID, player.displayName);
                sqlite.Insert(sql, sqlConnection, create_results =>
                {
                    if (create_results == 1) Interface.Oxide.LogInfo("New user created!");

                    sql = new Oxide.Core.Database.Sql();
                    sql.Append(SelectPlayer, player.userID);

                    sqlite.Query(sql, sqlConnection, results =>
                    {
                        if (results == null) return;

                        if (results.Count > 0)
                        {
                            foreach (var entry in results)
                            {
                                id = Convert.ToInt32(entry["id"]);
                                plagueLevel = Convert.ToInt32(entry["plague_level"]);
                                kinChangesCount = Convert.ToInt32(entry["kin_changes_count"]);
                                pristine = Convert.ToBoolean(entry["pristine"]);
                                break;
                            }
                        }
                        else
                        {
                            Interface.Oxide.LogInfo("Something wrong has happened: Could not find the player with the given user_id!");
                        }

                        associations = new Dictionary<ulong, Association>();
                        kins = new Dictionary<ulong, Kin>();
                        kinRequests = new List<ulong>();

                        loadAssociations();
                        loadKinList();
                        //loadKinRequestList();
                        callback?.Invoke(this);
                    });
                });
            }

            public static void setupDatabase(RustPlugin plugin)
            {
                sqlConnection = sqlite.OpenDb($"Plagued.db", plugin);

                var sql = new Oxide.Core.Database.Sql();

                sql.Append(@"CREATE TABLE IF NOT EXISTS players (
                                 id INTEGER PRIMARY KEY   AUTOINCREMENT,
                                 user_id TEXT UNIQUE NOT NULL,
                                 name TEXT,
                                 plague_level INTEGER,
                                 kin_changes_count INTEGER,
                                 pristine INTEGER
                               );");

                sql.Append(@"CREATE TABLE IF NOT EXISTS associations (
                                id INTEGER PRIMARY KEY   AUTOINCREMENT,
                                player_id integer NOT NULL,
                                associate_id integer NOT NULL,
                                level INTEGER,
                                FOREIGN KEY (player_id) REFERENCES players(id),
                                FOREIGN KEY (associate_id) REFERENCES players(id)
                            );");

                sql.Append(@"CREATE TABLE IF NOT EXISTS kin (
                                self_id integer NOT NULL,
                                kin_id integer NOT NULL,
                                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                                FOREIGN KEY (self_id) REFERENCES players(id),
                                FOREIGN KEY (kin_id) REFERENCES players(id),
                                PRIMARY KEY (self_id,kin_id)
                            );");

                sql.Append(@"CREATE TABLE IF NOT EXISTS kin_request (
                                requester_id integer NOT NULL,
                                target_id integer NOT NULL,
                                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                                FOREIGN KEY (requester_id) REFERENCES players(id),
                                FOREIGN KEY (target_id) REFERENCES players(id),
                                PRIMARY KEY (requester_id,target_id)
                            );");


                sqlite.Insert(sql, sqlConnection);
            }

            public static void closeDatabase()
            {
                sqlite.CloseDb(sqlConnection);
            }

            /**
             * Increases the affinity of an associate and returns his new affinity
             */
            private Association increaseAssociateAffinity(BasePlayer associate)
            {
                if (associate == null) return null;
                if (player.userID == associate.userID) return null;

                Association association = null;

                if (associations.ContainsKey(associate.userID))
                {
                    association = associations[associate.userID];
                    if ((association.level + affinityIncRate) < int.MaxValue) association.level += affinityIncRate;
                }
                else
                {
                    createAssociation(associate.userID, associationRef => {
                        if (associationRef != null)
                        {
                            association = associationRef;
                            associations.Add(associate.userID, associationRef);
                        }

                        return true;
                    });
                }

                //Interface.Oxide.LogInfo(player.displayName + " -> " + associate.displayName + " = " + associates[associate.userID].ToString());

                return association;
            }

            /**
             * Increases the affinity of all the associations in the list and increases the plague penalty if some associations are over the plague threshold
             * It also decreases the plague treshold if all the associates are kin or under the threshold
             */
            public void increasePlaguePenalty(BasePlayer[] associates)
            {
                int contagionVectorsCount = 0;
                var sql = new Oxide.Core.Database.Sql();

                foreach (BasePlayer associate in associates)
                {
                    if (isKinByUserID(associate.userID)) continue;

                    Association association = increaseAssociateAffinity(associate);

                    if (association == null) continue;
                    
                    sql.Append(UpdateAssociation, association.level, association.id);

                    if (association.level >= plagueMinAffinity)
                    {
                        contagionVectorsCount++;
                    }
                }

                sqlite.Update(sql, sqlConnection);


                if (contagionVectorsCount > 0)
                {
                    increasePlagueLevel(contagionVectorsCount);
                } else
                {
                    decreasePlagueLevel();
                }

                //Interface.Oxide.LogInfo(player.displayName + " -> " + plagueLevel);
            }

            /**
             * Decreases the affinity of all associations and decreases the plague level.
             */
            public void decreasePlaguePenalty()
            {
                decreaseAssociationsLevel();

                if (!pristine)
                {
                    decreasePlagueLevel();
                }
            }

            public void increasePlagueLevel(int contagionVectorCount)
            {
                if ((plagueLevel + (contagionVectorCount * plagueIncreaseRate)) <= 10000) {
                    plagueLevel += contagionVectorCount * plagueIncreaseRate;

                    if (pristine == true)
                    {
                        pristine = false;
                        MsgPlayer(player, "I don't feel so good.");
                        //Interface.Oxide.LogInfo(player.displayName + " is now sick.");
                    }

                    syncPlagueLevel();
                }

                //Interface.Oxide.LogInfo(player.displayName + "'s new plague level: " + plagueLevel.ToString());
            }

            public void decreasePlagueLevel()
            {
                if ((plagueLevel - plagueDecreaseRate) >= 0)
                {
                    plagueLevel -= plagueDecreaseRate;

                    if (plagueLevel == 0)
                    {
                        pristine = true;
                        MsgPlayer(player, "I feel a bit better now.");
                        //Interface.Oxide.LogInfo(player.displayName + " is now cured.");
                    }

                    syncPlagueLevel();
                }
            }

            public void decreaseAssociationsLevel()
            {
                if (associations.Count == 0) return;

                List<ulong> to_remove = new List<ulong>();
                var sql = new Oxide.Core.Database.Sql();

                foreach (ulong key in associations.Keys)
                {
                    Association association = associations[key];
                    int new_affinity = association.level - affinityDecRate;
                    if (new_affinity >= 1)
                    {
                        association.level = association.level - affinityDecRate;
                        sql.Append(UpdateAssociation, association.level, association.id);
                    } else if (new_affinity <= 0)
                    {
                        sql.Append(DeleteAssociation, association.id);
                        to_remove.Add(key);
                    }
                }

                foreach(ulong keyToRemove in to_remove)
                {
                    associations.Remove(keyToRemove);
                }

                sqlite.ExecuteNonQuery(sql, sqlConnection);
            }


            public bool isKinByUserID(ulong userID)
            {
                foreach(var item in kins)
                {
                    if (item.Value.kin_user_id == userID)
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool hasKinRequest(ulong kinID)
            {
                return kinRequests.Contains(kinID);
            }

            public bool addKinRequest(ulong kinID)
            {
                if (!kinRequests.Contains(kinID))
                {
                    kinRequests.Add(kinID);

                    return true;
                }

                return false;
            }

            public bool addKin(ulong kinUserID) {
                if (kins.Count + 1 <= maxKin && !isKinByUserID(kinUserID))
                {
                    if (kinRequests.Contains(kinUserID)) kinRequests.Remove(kinUserID);
                    Kin newKin = createKin(kinUserID);
                    newKin.kin_user_id = kinUserID;
                    kins.Add(kinUserID, newKin);

                    return true;
                }

                return false;
            }

            public bool removeKinById(int id)
            {
                if ((kinChangesCount + 1) <= maxKinChanges)
                {
                    foreach(Kin kin in kins.Values)
                    {
                        if (kin.kin_id == id)
                        {
                            return forceRemoveKin(kin.kin_user_id);
                        }
                    }
                }

                return false;
            }

            public bool removeKin(ulong kinUserID)
            {
                if ((kinChangesCount + 1) <= maxKinChanges)
                {
                    return forceRemoveKin(kinUserID);
                }

                return false;
            }

            public bool forceRemoveKin(ulong kinUserID)
            {
                if (isKinByUserID(kinUserID))
                {
                    kinChangesCount++;
                    Kin kin = kins[kinUserID];

                    var sql = new Oxide.Core.Database.Sql();
                    sql.Append(DeleteKin, kin.self_id, kin.kin_id);
                    sqlite.ExecuteNonQuery(sql, sqlConnection);

                    kins.Remove(kinUserID);

                    return true;
                }

                return false;
            }

            public List<string> getKinList()
            {
                List<string> kinList = new List<string>();

                foreach (Kin kin in kins.Values)
                {
                    kinList.Add(String.Format("{0} (Id: {1})", kin.kin_name, kin.kin_id));
                }

                return kinList;
            }
            
            public List<string> getAssociatesList()
            {
                List<string> associatesList = new List<string>();

                foreach (Association association in associations.Values)
                {
                    associatesList.Add(String.Format("{0} (Id: {1} | Level: {2})", association.associate_name, association.associate_id, association.getAffinityLabel()));
                }

                return associatesList;
            }

            public int getPlagueLevel()
            {
                return plagueLevel;
            }

            public int getId()
            {
                return id;
            }

            public bool getPristine()
            {
                return pristine;
            }

            private Kin createKin(ulong kinUserId)
            {
                Kin kin = new Kin(id);

                var sql = new Oxide.Core.Database.Sql();
                sql.Append(SelectPlayer, kinUserId);

                sqlite.Query(sql, sqlConnection, list => {
                    if (list == null) return;

                    foreach (var user in list)
                    {
                        kin.kin_id = Convert.ToInt32(user["id"]);
                        kin.kin_name = Convert.ToString(user["name"]);
                        kin.kin_user_id = kinUserId;
                        break;
                    }

                    kin.create();
                });

                return kin;
            }

            private void createAssociation(ulong associate_user_id, Func<Association, bool> callback)
            {
                Association association = new Association();

                var sql = new Oxide.Core.Database.Sql();
                sql.Append(SelectPlayer, associate_user_id);
                sqlite.Query(sql, sqlConnection, list => {
                    if (list == null) return;
                    if (list.Count == 0) {
                        callback(null);
                        return;
                    };

                    foreach (var user in list)
                    {
                        association.player_id = id;
                        association.associate_id = Convert.ToInt32(user["id"]);
                        association.associate_user_id = associate_user_id;
                        association.associate_name = Convert.ToString(user["name"]);
                        association.level = 0;
                        break;
                    }

                    association.create();
                    callback(association);
                });
            }

            private void syncPlagueLevel()
            {
                var sql = new Oxide.Core.Database.Sql();
                sql.Append(UpdatePlayerPlagueLevel, plagueLevel, (pristine ? 1 : 0), player.userID);
                sqlite.Update(sql, sqlConnection);
            }

            private void loadAssociations()
            {
                var sql = new Oxide.Core.Database.Sql();
                sql.Append(SelectAssociations, id);
                sqlite.Query(sql, sqlConnection, results => {
                    if (results == null) return;

                    foreach (var association_result in results) {
                        Association association =  new Association();
                        association.load(association_result);
                        associations[association.associate_user_id] = association;
                    }
                });
            }

            private void loadKinList()
            {
                var sql = new Oxide.Core.Database.Sql();
                sql.Append(SelectKinList, id);
                sqlite.Query(sql, sqlConnection, results => {
                    if (results == null) return;

                    foreach (var kinResult in results)
                    {
                        Kin kin = new Kin(id);
                        kin.load(kinResult);
                        kins[kin.kin_user_id] = kin;
                    }
                });
            }

            private void loadKinRequestList()
            {
                var sql = new Oxide.Core.Database.Sql();
                sql.Append(SelectKinRequestList, id);
                sqlite.Query(sql, sqlConnection, results => {
                    if (results == null) return;

                    foreach (var kinRequest in results)
                    {
                        kinRequests.Add((ulong)Convert.ToInt64(kinRequest["user_id"]));
                    }
                });
            }

            private class Association
            {
                public int id;
                public int player_id;
                public int associate_id;
                public ulong associate_user_id;
                public string associate_name;
                public int level;

                public void create()
                {
                    var sql = new Oxide.Core.Database.Sql();
                    sql.Append(CheckAssociationExists, player_id, associate_id);

                    // Check if the relationship exists before creating it
                    sqlite.Query(sql, sqlConnection, check_results =>
                    {
                        if (check_results.Count > 0) return;

                        sql = new Oxide.Core.Database.Sql();

                        sql.Append(InsertAssociation, player_id, associate_id, level);
                        sqlite.Insert(sql, sqlConnection, result =>
                        {
                            if (result == null) return;
                            id = (int)sqlConnection.LastInsertRowId;
                        });
                    });
                }

                public void load(Dictionary<string, object> association)
                {
                    id = Convert.ToInt32(association["id"]);
                    associate_name = Convert.ToString(association["name"]);
                    associate_user_id = (ulong) Convert.ToInt64(association["user_id"]);
                    associate_id = Convert.ToInt32(association["associate_id"]);
                    player_id = Convert.ToInt32(association["player_id"]);
                    level = Convert.ToInt32(association["level"]);
                }

                public string getAffinityLabel()
                {
                    if (level >= plagueMinAffinity)
                    {
                        return "Associate";
                    } else
                    {
                        return "Acquaintance";
                    }
                }
            }

            private class Kin
            {
                public int self_id;
                public int kin_id;
                public ulong kin_user_id;
                public string kin_name;
                public int player_one_id;
                public int player_two_id;

                private Kin()
                {

                }

                public Kin(int p_self_id)
                {
                    self_id = p_self_id;
                }

                public void create()
                {
                    var sql = new Oxide.Core.Database.Sql();
                    sql.Append(InsertKin, self_id, kin_id);
                    sqlite.Insert(sql, sqlConnection);
                }

                public void load(Dictionary<string, object> kin)
                {
                    self_id = Convert.ToInt32(kin["self_id"]);
                    kin_id = Convert.ToInt32(kin["kin_id"]);
                    kin_name = Convert.ToString(kin["kin_name"]);
                    kin_user_id = (ulong)Convert.ToInt64(kin["kin_user_id"]);
                }
            }
        }

        #endregion

        #region Unity Components

        /**
         * This component adds a timers and collects all players colliders in a given radius. It then triggers custom hooks to reflect the situation of a given player
         */
        public class ProximityDetector : MonoBehaviour
        {
            public BasePlayer player;

            public void disableProximityCheck()
            {
                CancelInvoke("CheckProximity");
            }

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                InvokeRepeating("CheckProximity", 0, 2.5f);
            }

            void OnDestroy()
            {
                disableProximityCheck();
            }

            void CheckProximity()
            {
                var count = Physics.OverlapSphereNonAlloc(player.transform.position, plagueRange, colBuffer, playerLayer);

                if (count > 1)
                {
                    BasePlayer[] playersNear = new BasePlayer[count];
                    for (int i = 0; i < count; i++)
                    {
                        var collider = colBuffer[i];
                        colBuffer[i] = null;
                        var collidingPlayer = collider.GetComponentInParent<BasePlayer>();
                        playersNear[i] = collidingPlayer;
                    }
                    notifyPlayerProximity(playersNear);
                } else
                {
                    notifyPlayerAlone();
                }
            }

            void notifyPlayerProximity(BasePlayer[] players)
            {
                Interface.Oxide.CallHook("OnPlayerProximity", player, players);
            }

            void notifyPlayerAlone()
            {
                Interface.Oxide.CallHook("OnPlayerAlone", player);
            }
        }
        #endregion
    }
}
