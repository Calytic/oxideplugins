// Reference: Oxide.Ext.MySql
// Reference: Oxide.Ext.SQLite
using UnityEngine;
using System.Collections.Generic;
using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using Rust;
using System.Text;
using Oxide.Core.Database;

namespace Oxide.Plugins
{
    [Info("Zeiser Levels REMASTERED", "Zeiser/Visagalis", "1.6.1", ResourceId = 1453)]
    [Description("Lets players level up as they harvest different resources and when crafting.")]
    public class ZLevelsRemastered : RustPlugin
    {
        #region SQL Things
        private readonly Ext.MySql.Libraries.MySql _mySql = new Ext.MySql.Libraries.MySql();
        private readonly Ext.SQLite.Libraries.SQLite _sqLite = new Ext.SQLite.Libraries.SQLite();
        private Connection _mySqlConnection = null;
        private Connection _sqLiteConnection = null;
        public Dictionary<ulong, Dictionary<string, long>> playerList = new Dictionary<ulong, Dictionary<string, long>>();
        private readonly string sqLiteDBFile = "ZLevelsRemastered.db";

        private void StartConnection()
        {
            if (usingMySQL() && _mySqlConnection == null)
            {
                _mySqlConnection = _mySql.OpenDb(dbConnection["Host"].ToString(), Convert.ToInt32(dbConnection["Port"]),
                    dbConnection["Database"].ToString(), dbConnection["Username"].ToString(),
                    dbConnection["Password"].ToString(), this);
                Puts("Connection opened.(MySQL)");
            }
            else
            {
                _sqLiteConnection = _sqLite.OpenDb(sqLiteDBFile, this);
                CheckConnection();
            }
        }

        private void CheckConnection()
        {
            Dictionary <string, string> tableStucture = new Dictionary<string, string>();
            tableStucture.Add("UserID", "INTEGER\tNOT NULL");
            tableStucture.Add("Name", "TEXT\tNOT NULL");
            tableStucture.Add("WCLevel", "INTEGER");
            tableStucture.Add("WCPoints", "INTEGER");
            tableStucture.Add("MLevel", "INTEGER");
            tableStucture.Add("MPoints", "INTEGER");
            tableStucture.Add("SLevel", "INTEGER");
            tableStucture.Add("SPoints", "INTEGER");
            tableStucture.Add("CLevel", "INTEGER");
            tableStucture.Add("CPoints", "INTEGER");
            tableStucture.Add("LastDeath", "INTEGER");
            tableStucture.Add("LastLoginDate", "INTEGER");
            tableStucture.Add("XPMultiplier", "INTEGER\tNOT NULL\tDEFAULT 100");

            string queryText = "CREATE TABLE IF NOT EXISTS \"RPG_User\" (";
            foreach (var structItem in tableStucture)
            {
                queryText += "`" + structItem.Key + "` " + structItem.Value + ", ";
            }
            queryText += "PRIMARY KEY(UserID))";
            var sql = new Sql(queryText);
            _sqLite.Query(sql, _sqLiteConnection, list =>
            {
                //CheckTableIntegrity(tableStucture);
            });
        }

        /* TODO: Will finish this one day!
        public void CheckTableIntegrity(Dictionary<string, string> tableStucture)
        {
            var sql = new Sql("PRAGMA table_info(RPG_User)");
            _sqLite.Query(sql, _sqLiteConnection, list =>
            {
                if (list.Count > 0) // Save to DB failed.
                    foreach (var listItem in list)
                    {
                        string currColumn = listItem["type"] +
                                            (listItem["notnull"].ToString() == "1" ? "\tNOT NULL" : "") +
                                            (listItem["dflt_value"].ToString() != string.Empty ? "\tDEFAULT " + listItem["dflt_value"].ToString() : "");

                        if (tableStucture[listItem["name"].ToString()] != currColumn)
                            alterTable(currColumn, tableStucture[listItem["name"].ToString()]);
                    }

            });
        }

        public void alterTable(string currColumn, string fixedColumn)
        {
            Puts("Altering table from: [" + currColumn + "] to [" + fixedColumn + "]");
            var sql = new Sql("BEGIN TRANSACTION;PRAGMA schema_version;");
            _sqLite.Query(sql, _sqLiteConnection, list =>
            {
                if (list.Count > 0)
                {
                    Puts("1");
                    int schemaVersion = Convert.ToInt32(list[0]["schema_version"]);
                    var sql2 = new Sql("PRAGMA writable_schema=ON;");
                    _sqLite.Query(sql2, _sqLiteConnection, list2 =>
                    {
                        Puts("2");
                        var sql3 = new Sql("SELECT * FROM sqlite_master WHERE type='table' and name='RPG_User';");
                        _sqLite.Query(sql3, _sqLiteConnection, list3 =>
                        {
                            if (list3.Count > 0)
                            {
                                Puts("3");
                                string modifiedSql = list3[0]["sql"].ToString();
                                Puts("Before: " + modifiedSql);
                                modifiedSql = modifiedSql.Replace(currColumn, fixedColumn);
                                Puts("After: " + modifiedSql);
                                var sql4 =
                                    new Sql("UPDATE sqlite_master SET sql=@0 WHERE type='table' and name='RPG_User';",
                                        modifiedSql);
                                _sqLite.Query(sql4, _sqLiteConnection, list4 =>
                                {
                                    if (list4.Count > 0)
                                    {
                                        Puts("4");
                                        var sql5 =
                                            new Sql("PRAGMA schema_version=" + ++schemaVersion +
                                                    ";PRAGMA writable_schema=OFF;END TRANSACTION");
                                        _sqLite.Query(sql5, _sqLiteConnection, list5 =>
                                        {
                                            Puts("5");
                                            var sql6 = new Sql("PRAGMA integrity_check");
                                            _sqLite.Query(sql5, _sqLiteConnection, list6 =>
                                            {
                                                if (list6.Count > 0)
                                                {
                                                    Puts("6");
                                                    Puts(list6[0]["integrity_check"].ToString());
                                                }
                                            });
                                        });
                                    }
                                });
                            }
                        });
                    });
                }
            });
        }
        */

        public void setPointsAndLevel(ulong userID, string skill, long points, long level)
        {
            if (!playerList.ContainsKey(userID))
                playerList.Add(userID, new Dictionary<string, long>());

            setPlayerData(userID, skill + "Points", points == 0 ? getLevelPoints(level) : points);
            setPlayerData(userID, skill + "Level", level);
        }

        public void loadUser(BasePlayer player)
        {
            Dictionary<string, long> statsInit = new Dictionary<string, long>();
            foreach (string skill in Skills.ALL)
            {
                statsInit.Add(skill + "Level", 1);
                statsInit.Add(skill + "Points", 10);
            }
            long currTime = ToEpochTime(DateTime.UtcNow);
            statsInit.Add("LastDeath", currTime);
            statsInit.Add("LastLoginDate", currTime);
            statsInit.Add("XPMultiplier", 100);
            var sql = Sql.Builder.Append("SELECT * FROM RPG_User WHERE UserID = @0", player.userID);


            if (usingMySQL())
            {
                _mySql.Query(sql, _mySqlConnection, list =>
                {
                    initPlayer(player, statsInit, list);
                });
            }
            else
            {
                _sqLite.Query(sql, _sqLiteConnection, list =>
                {
                    initPlayer(player, statsInit, list);
                });
            }
        }

        void initPlayer(BasePlayer player, Dictionary<string, long> statsInit, List<Dictionary<string, object>> sqlData)
        {

            bool needToSave = true;
            Dictionary<string, long> tempElement = new Dictionary<string, long>();
            if (sqlData.Count > 0)
            {
                foreach (string key in statsInit.Keys)
                {
                    if(sqlData[0][key] != DBNull.Value)
                        tempElement.Add(key, Convert.ToInt64(sqlData[0][key]));
                }

                needToSave = false;
            }

            foreach (var tempItem in tempElement)
                statsInit[tempItem.Key] = tempItem.Value;

            initPlayerData(player, statsInit);
            if (needToSave)
                saveUser(player);

            RenderUI(player);
        }

        void setPlayerData(ulong userID, string key, long value)
        {
            if (playerList[userID].ContainsKey(key))
                playerList[userID][key] = value;
            else
                playerList[userID].Add(key, value);
        }

        void initPlayerData(BasePlayer player, Dictionary<string, long> playerData)
        {
            foreach (string skill in Skills.ALL)
                setPointsAndLevel(player.userID, skill, playerData[skill + "Points"], playerData[skill + "Level"]);

            foreach (var dataItem in playerData)
            {
                if (dataItem.Key.EndsWith("Level") || dataItem.Key.EndsWith("Points"))
                    continue;
                setPlayerData(player.userID, dataItem.Key, dataItem.Value);
            }
        }

        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            BasePlayer player = inventory.GetComponent<BasePlayer>();
            if(player != null && inPlayerList(player.userID))
            {
                RenderUI(player);
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (guioff.Contains(player.userID))
                guioff.Remove(player.userID);
            else
                CuiHelper.DestroyUi(player, "StatsUI");

            if (inPlayerList(player.userID))
            {
                saveUser(player);

                if (playerList.ContainsKey(player.userID))
                    playerList.Remove(player.userID);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            long multiplier = 100;
            string[] playerPermissions = permission.GetUserPermissions(player.UserIDString);
            if (playerPermissions.Any(x => x.ToLower().StartsWith("zlvlboost")))
            {
                string permission = playerPermissions.First(x => x.ToLower().StartsWith("zlvlboost"));

                if (!long.TryParse(permission.ToLower().Replace("zlvlboost", ""), out multiplier))
                    multiplier = 100;
            }
            editMultiplierForPlayer(multiplier, player.userID);


            loadUser(player);
        }

        public void SaveUsers()
        {
            foreach (var user in BasePlayer.activePlayerList)
            {
                saveUser(user);
            }
        }

        static string EncodeNonAsciiCharacters(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    string encodedValue = "";
                    sb.Append(encodedValue);
                }
                else {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public void saveUser(BasePlayer player)
        {
            if (!playerList.ContainsKey(player.userID))
            {
                Puts("Trying to save player, who haven't been loaded yet? Player name: " + player.displayName);
                return;
            }

            Dictionary<string, long> statsInit = getConnectedPlayerDetailsData(player.userID);

            string name = EncodeNonAsciiCharacters(player.displayName);
            string sqlText =
                "REPLACE INTO RPG_User (UserID, Name, WCLevel, WCPoints, MLevel, MPoints, SLevel, SPoints, CLevel, CPoints, LastDeath, LastLoginDate, XPMultiplier) " +
                "VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12)";
            var sql = Sql.Builder.Append(sqlText,
                player.userID, //0
                name, //1
                statsInit["WCLevel"], //2 
                statsInit["WCPoints"], //3
                statsInit["MLevel"], //4
                statsInit["MPoints"], //5
                statsInit["SLevel"], //6
                statsInit["SPoints"], //7
                statsInit["CLevel"], //8
                statsInit["CPoints"], //9
                statsInit["LastDeath"], //10
                statsInit["LastLoginDate"], //11
                statsInit["XPMultiplier"]); //12
            if (usingMySQL())
            {
                _mySql.Insert(sql, _mySqlConnection, list =>
                {
                    if (list == 0) // Save to DB failed.
                        Puts("OMG WE DIDN'T SAVED IT!: " + sql.SQL);
                });
            }
            else
            {
                _sqLite.Insert(sql, _sqLiteConnection, list =>
                {
                    if (list == 0) // Save to DB failed.
                        Puts("OMG WE DIDN'T SAVED IT!: " + sql.SQL);
                });
            }
        }

        public Dictionary<string, long> getConnectedPlayerDetailsData(ulong userID)
        {
            if (!playerList.ContainsKey(userID))
                return null;

            Dictionary<string, long> statsInit = new Dictionary<string, long>();
            foreach (string skill in Skills.ALL)
            {
                statsInit.Add(skill + "Level", getLevel(userID, skill));
                statsInit.Add(skill + "Points", getPoints(userID, skill));
            }
            statsInit.Add("LastDeath", playerList[userID]["LastDeath"]);
            statsInit.Add("LastLoginDate", playerList[userID]["LastLoginDate"]);
            statsInit.Add("XPMultiplier", playerList[userID]["XPMultiplier"]);
            return statsInit;
        }

        #endregion
        public static class Skills
        {
            public static string CRAFTING = "C";
            public static string WOODCUTTING = "WC";
            public static string SKINNING = "S";
            public static string MINING = "M";
            public static string[] ALL = new[] {WOODCUTTING, MINING, SKINNING, CRAFTING};
        }
        System.Collections.Generic.List<ulong> guioff = new System.Collections.Generic.List<ulong>();

        private Dictionary<string, string> colors = new Dictionary<string, string>()
        {
            {Skills.WOODCUTTING, "#FFDDAA"},
            {Skills.MINING, "#DDDDDD"},
            {Skills.SKINNING, "#FFDDDD"},
            {Skills.CRAFTING, "#CCFF99"}
        };

        class CraftData
        {
            public Dictionary<string, CraftInfo> CraftList = new Dictionary<string, CraftInfo>();
            public CraftData() { }
        }

        CraftData _craftData;

        #region Stats
        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            string text = "/stats - Displays your stats.\n/statsui - Displays/hides stats UI.\n/statinfo [statsname] - Displays information about stat.\n" +
                          "/topskills - Display max levels reached so far.";
            player.ChatMessage(text);
        }

        [ChatCommand("topskills")]
        private void StatsTopCommand(BasePlayer player, string command, string[] args)
        {
            PrintToChat(player, "Max stats on server so far:");
            foreach (string skill in Skills.ALL)
            {
                if (!IsSkillDisabled(skill))
                    printMaxSkillDetails(player, skill);
            }
        }

        private void printMaxSkillDetails(BasePlayer player, string skill)
        {
            var sql =
                Sql.Builder.Append("SELECT * FROM RPG_User ORDER BY " + skill + "Level DESC," + skill +
                                   "Points DESC LIMIT 1;");
            if (usingMySQL())
            {
                _mySql.Query(sql, _mySqlConnection, list =>
                {
                    if (list.Count > 0)
                        printMaxSkillDetails(player, skill, list);
                });
            }
            else
            {
                _sqLite.Query(sql, _sqLiteConnection, list =>
                {
                    if (list.Count > 0)
                        printMaxSkillDetails(player, skill, list);
                });
            }
        }

        void printMaxSkillDetails(BasePlayer player, string skill, List<Dictionary<string, object>> sqlData)
        {
            PrintToChat(player,
                            "<color=" + colors[skill] + ">" + messages[skill + "Skill"] + ": " +
                            sqlData[0][skill + "Level"] + " (XP: " + sqlData[0][skill + "Points"] + ")</color> <- " +
                            sqlData[0]["Name"]);
        }

        [ConsoleCommand("zinfo")]
        private void InfoCommand(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
                return;

            if (arg.Args == null || arg.Args.Length != 1)
            {
                Puts("Syntax is: zinfo name/steamid");
                Puts("Example: zinfo visagalis");
                return;
            }
            string playerName = arg.Args[0];
            BasePlayer player = rust.FindPlayer(playerName);

            if (player != null)
            {

                var playerData = getConnectedPlayerDetailsData(player.userID);
                if(playerData == null)
                    Puts("PlayerData IS NULL!!!");

                Puts("Stats for player: [" + player.displayName + "]");
                Puts("Woodcutting: " + playerData["WCLevel"] + " XP: [" + playerData["WCPoints"] + "]");
                Puts("Mining: " + playerData["MLevel"] + " XP: [" + playerData["MPoints"] + "]");
                Puts("Skinning: " + playerData["SLevel"] + " XP: [" + playerData["SPoints"] + "]");
                Puts("Crafting: " + playerData["CLevel"] + " XP: [" + playerData["CPoints"] + "]");
                Puts("XP Multiplier: " + playerData["XPMultiplier"] + " % ");
            }
        }

        [ConsoleCommand("zlvl")]
        private void ZlvlCommand(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
                return;

            if (arg.Args == null || arg.Args.Length != 3)
            {
                Puts("Syntax is: zlvl name/steamid skill [OPERATOR]NUMBER");
                Puts("Example: zlvl Visagalis WC /2 -- visagalis gets his WC level divided by 2.");
                Puts("Example: zlvl * * +3 -- Everyone currently playing in the server gets +3 for all skills.");
                Puts("Example: zlvl ** * /2 -- Everyone (including offline players) gets their level divided by 2.");
                Puts("Instead of names you can use wildcard(*): * - affects online players, ** - affects all players");
                Puts("Possible operators: *(XP Modified %), +(Adds level), -(Removes level), /(Divides level)");
                return;
            }
            string playerName = arg.Args[0];
            BasePlayer p = rust.FindPlayer(playerName);

            if (p != null || (playerName == "*" || playerName == "**"))
            {
                int playerMode = 0; // Exact player
                if (playerName == "*")
                    playerMode = 1; // Online players
                else if (playerName == "**")
                    playerMode = 2; // All players
                string skill = arg.Args[1].ToUpper();
                if (skill == Skills.WOODCUTTING || skill == Skills.MINING || skill == Skills.SKINNING ||
                    skill == Skills.CRAFTING || skill == "*")
                {
                    bool allSkills = skill == "*";
                    int mode = 0; // 0 = SET, 1 = ADD, 2 = SUBTRACT, 3 = multiplier, 4 = divide
                    int value;
                    bool correct = false;
                    if (arg.Args[2][0] == '+')
                    {
                        mode = 1;
                        correct = int.TryParse(arg.Args[2].Replace("+", ""), out value);
                    } else if (arg.Args[2][0] == '-')
                    {
                        mode = 2;
                        correct = int.TryParse(arg.Args[2].Replace("-", ""), out value);
                    }
                    else if (arg.Args[2][0] == '*')
                    {
                        mode = 3;
                        correct = int.TryParse(arg.Args[2].Replace("*", ""), out value);
                    }
                    else if (arg.Args[2][0] == '/')
                    {
                        mode = 4;
                        correct = int.TryParse(arg.Args[2].Replace("/", ""), out value);
                    }
                    else
                    {
                        correct = int.TryParse(arg.Args[2], out value);
                    }
                    if (correct)
                    {
                        if (mode == 3) // Change XP Multiplier.
                        {
                            if (!allSkills)
                            {
                                Puts("XPMultiplier is changeable for all skills! Use * instead of " + skill + ".");
                                return;
                            }
                            if (playerMode == 1)
                            {
                                foreach (var currPlayer in BasePlayer.activePlayerList)
                                    editMultiplierForPlayer(value, currPlayer.userID);
                            }
                            else if (playerMode == 2)
                                editMultiplierForPlayer(value);
                            else if(p != null)
                                editMultiplierForPlayer(value, p.userID);

                            Puts("XP rates has changed to "+ value +"% of normal XP for " + (playerMode == 1 ? "ALL ONLINE PLAYERS" : (playerMode == 2 ? "ALL PLAYERS" : p.displayName)));
                            return;
                        }

                        if (playerMode == 1)
                        {
                            foreach (var currPlayer in BasePlayer.activePlayerList)
                                adminModifyPlayerStats(skill, value, mode, currPlayer);
                        }
                        else if (playerMode == 2)
                            adminModifyPlayerStats(skill, value, mode);
                        else
                            adminModifyPlayerStats(skill, value, mode, p);

                    }
                }
                else
                {
                    Puts("Incorrect skill. Possible skills are: WC, M, S, C, *(All skills).");
                }
            }
            else
            {
                Puts("Player with name: "+ arg.Args[0] +" haven't been found online.");
            }

        }

        private void adminModifyPlayerStats(string skill, long level, int mode, BasePlayer p = null)
        {
            if (skill == "*")
            {
                foreach (var currSkill in Skills.ALL)
                {
                    if (p == null)
                    {
                        string action = "";
                        switch (mode)
                        {
                            case 1:
                                action = "+";
                                break;
                            case 2:
                                action = "-";
                                break;
                            case 4:
                                action = "/";
                                break;
                            default:
                                break;
                        }
                        if (string.IsNullOrEmpty(action))
                        {
                            Puts("You can't just SET everyone's level, use + - or / operator.");
                            return;
                        }
                        string sqlText = "UPDATE RPG_User SET ";
                        string skillLevel = currSkill + "Level";
                        sqlText += skillLevel + "=" + skillLevel + action + level + ", ";
                        sqlText += currSkill + "Points=0;" +
                                   (levelCaps[currSkill].ToString() != "0" ? ("UPDATE RPG_User SET " + skillLevel + "=" + levelCaps[currSkill] + " WHERE " + skillLevel + ">" + levelCaps[currSkill] + ";"): "") +
                                   "UPDATE RPG_User SET " + skillLevel + "=1 WHERE " + skillLevel + "< 1;";
                        var sql = Sql.Builder.Append(sqlText);
                        if (usingMySQL())
                            _mySql.ExecuteNonQuery(sql, _mySqlConnection);
                        else
                            _sqLite.ExecuteNonQuery(sql, _sqLiteConnection);

                        foreach (var onlinePlayer in BasePlayer.activePlayerList)
                            loadUser(onlinePlayer);
                    }
                    else
                    {
                        long modifiedLevel = getLevel(p.userID, currSkill);
                        if (mode == 0) // SET
                            modifiedLevel = level;
                        else if (mode == 1) // ADD
                            modifiedLevel += level;
                        else if (mode == 2) // SUBTRACT
                            modifiedLevel -= level;
                        else if (mode == 4) // DIVIDE
                            modifiedLevel /= level;
                        if (modifiedLevel < 1)
                            modifiedLevel = 1;
                        if (modifiedLevel > Convert.ToInt32(levelCaps[currSkill]) &&
                            Convert.ToInt32(levelCaps[currSkill]) != 0)
                        {
                            modifiedLevel = Convert.ToInt32(levelCaps[currSkill]);
                                // Don't allow to ADD levels above limits.
                            Puts(
                                "Warning! You tried to level up player above levelCaps, use SET if you want to have player level over levelCaps.");
                        }

                        setPointsAndLevel(p.userID, currSkill, getLevelPoints(modifiedLevel), modifiedLevel);
                        RenderUI(p);
                        Puts(messages[currSkill + "Skill"] + " Level for [" + p.displayName + "] has been set to: [" +
                             modifiedLevel +
                             "]");
                        SendReply(p,
                            "Admin has set your " + messages[currSkill + "Skill"] + " level to: [" + modifiedLevel +
                            "] ");
                    }
                }
            }
            else
            {
                if (p == null)
                {
                    string action = "";
                    switch (mode)
                    {
                        case 1:
                            action = "+";
                            break;
                        case 2:
                            action = "-";
                            break;
                        case 4:
                            action = "/";
                            break;
                        default:
                            break;
                    }
                    if (string.IsNullOrEmpty(action))
                    {
                        Puts("You can't just SET everyone's level, use + - or / operator.");
                        return;
                    }
                    string sqlText = "UPDATE RPG_User SET ";
                    string skillLevel = skill + "Level";
                    sqlText += skillLevel + "=" + skillLevel + action + level + ", ";
                    sqlText += skill + "Points=0;" +
                               (levelCaps[skill].ToString() != "0" ? ("UPDATE RPG_User SET " + skillLevel + "=" + levelCaps[skill] + " WHERE " + skillLevel + ">" + levelCaps[skill] + ";") : "") +
                               "UPDATE RPG_User SET " + skillLevel + "=1 WHERE " + skillLevel + "< 1;";
                    var sql = Sql.Builder.Append(sqlText);
                    if (usingMySQL())
                        _mySql.ExecuteNonQuery(sql, _mySqlConnection);
                    else
                        _sqLite.ExecuteNonQuery(sql, _sqLiteConnection);

                    foreach (var onlinePlayer in BasePlayer.activePlayerList)
                        loadUser(onlinePlayer);
                    return;
                }
                long modifiedLevel = getLevel(p.userID, skill);
                if (mode == 0) // SET
                    modifiedLevel = level;
                else if (mode == 1) // ADD
                    modifiedLevel += level;
                else if (mode == 2) // SUBTRACT
                    modifiedLevel -= level;
                else if (mode == 4) // DIVIDE
                    modifiedLevel /= level;
                if (modifiedLevel < 1)
                    modifiedLevel = 1;
                if (modifiedLevel > Convert.ToInt32(levelCaps[skill]) && Convert.ToInt32(levelCaps[skill]) != 0)
                {
                    modifiedLevel = Convert.ToInt32(levelCaps[skill]); // Don't allow to ADD levels above limits.
                    Puts("Warning! You tried to level up player above levelCaps, use SET if you want to have player level over levelCaps.");
                }

                setPointsAndLevel(p.userID, skill, getLevelPoints(modifiedLevel), modifiedLevel);
                RenderUI(p);
                Puts(messages[skill + "Skill"] + " Level for [" + p.displayName + "] has been set to: [" + modifiedLevel + "]");
                SendReply(p, "Admin has set your " + messages[skill + "Skill"] + " level to: [" + modifiedLevel + "] ");
            } 
            
        }

        private void editMultiplierForPlayer(long multiplier, ulong userID = ulong.MinValue)
        {
            string sqlText = "UPDATE RPG_User SET XPMultiplier = @0";
            if (userID != ulong.MinValue)
                sqlText += " WHERE UserID = @1";

            if (userID == ulong.MinValue)
            {
                foreach (var playerDetails in playerList)
                {
                    playerDetails.Value["XPMultiplier"] = multiplier;
                }
            }
            else
            {
                if(playerList.ContainsKey(userID))
                    playerList[userID]["XPMultiplier"] = multiplier;
            }
            var sql = Sql.Builder.Append(sqlText, multiplier, userID);

            if (usingMySQL())
                _mySql.ExecuteNonQuery(sql, _mySqlConnection);
            else
                _sqLite.ExecuteNonQuery(sql, _sqLiteConnection);
        }

        [ChatCommand("stats")]
        private void StatsCommand(BasePlayer player, string command, string[] args)
        {
            string text = "<color=blue>ZLevels Remastered [" + Version + "] by Visagalis</color>\n" + "<color=yellow>" +
                          (string) messages["StatsHeadline"] + "</color>\n";


            foreach (string skill in Skills.ALL)
            {
                text += getStatPrint(player, skill);
            }

            rust.SendChatMessage(player, text, null, "76561198002115162");

            Dictionary<string, long> details = playerList[player.userID];
            if (details.ContainsKey("LastDeath"))
            {
                DateTime currentTime = DateTime.UtcNow;
                DateTime lastDeath = ToDateTimeFromEpoch(details["LastDeath"]);
                TimeSpan timeAlive = currentTime - lastDeath;
                PrintToChat(player, "Time alive: " + ReadableTimeSpan(timeAlive));
                if(details["XPMultiplier"].ToString() != "100")
                    PrintToChat(player, "XP rates for you are " + details["XPMultiplier"] + "%");
            }

            RenderUI(player);
        }

        public static string ReadableTimeSpan(TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}{4}",
                (span.Days / 7) > 0 ? string.Format("{0:0} weeks, ", span.Days / 7) : string.Empty,
                span.Days % 7 > 0 ? string.Format("{0:0} days, ", span.Days % 7) : string.Empty,
                span.Hours > 0 ? string.Format("{0:0} hours, ", span.Hours) : string.Empty,
                span.Minutes > 0 ? string.Format("{0:0} minutes, ", span.Minutes) : string.Empty,
                span.Seconds > 0 ? string.Format("{0:0} seconds, ", span.Seconds) : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            return formatted;
        }

        [ChatCommand("statinfo")]
        private void StatInfoCommand(BasePlayer player, string command, string[] args)
        {
            string messagesText = "";
            long xpMultiplier = 100;
            if (inPlayerList(player.userID))
                xpMultiplier = playerList[player.userID]["XPMultiplier"];

            if (args.Length == 1)
            {
                string statname = args[0].ToLower();
                switch (statname)
                {
                    case "mining":
                        messagesText = "<color=" + colors[Skills.MINING] + ">Mining</color>" + (IsSkillDisabled(Skills.MINING) ? "(DISABLED)" : "") + "\n";
                        messagesText += "XP per hit: <color=" + colors[Skills.MINING] + ">" + ((int)pointsPerHit[Skills.MINING] * (xpMultiplier / 100f)) + "</color>\n";
                        messagesText += "Bonus materials per level: <color=" + colors[Skills.MINING] + ">" + ((getGathMult(2, Skills.MINING) - 1) * 100).ToString("0.##") + "%</color>\n";
                        break;
                    case "woodcutting":
                        messagesText = "<color=" + colors[Skills.WOODCUTTING] + ">Woodcutting</color>" + (IsSkillDisabled(Skills.WOODCUTTING) ? "(DISABLED)" : "") + "\n";
                        messagesText += "XP per hit: <color=" + colors[Skills.WOODCUTTING] + ">" + ((int)pointsPerHit[Skills.WOODCUTTING] * (xpMultiplier / 100f)) + "</color>\n";
                        messagesText += "Bonus materials per level: <color=" + colors[Skills.WOODCUTTING] + ">" + ((getGathMult(2, Skills.WOODCUTTING) - 1) * 100).ToString("0.##") + "%</color>\n";
                        break;
                    case "skinning":
                        messagesText = "<color=" + colors[Skills.SKINNING] + '>' + "Skinning" + "</color>" + (IsSkillDisabled(Skills.SKINNING) ? "(DISABLED)" : "") + "\n";
                        messagesText += "XP per hit: <color=" + colors[Skills.SKINNING] + ">" + ((int)pointsPerHit[Skills.SKINNING] * (xpMultiplier / 100f)) + "</color>\n";
                        messagesText += "Bonus materials per level: <color=" + colors[Skills.SKINNING] + ">" + ((getGathMult(2, Skills.SKINNING) - 1) * 100).ToString("0.##") + "%</color>\n";
                        break;
                    case "crafting":
                        messagesText = "<color=" + colors[Skills.CRAFTING] + '>' + "Crafting" + "</color>" + (IsSkillDisabled(Skills.CRAFTING) ? "(DISABLED)" : "") + "\n";
                        messagesText += "XP gain: <color=" + colors[Skills.SKINNING] + ">You get "+ craftingDetails["XPPerTimeSpent"] + " XP per "+ craftingDetails["TimeSpent"] + "s spent crafting.</color>\n";
                        messagesText += "Bonus: <color=" + colors[Skills.SKINNING] + ">Crafting time is decreased by "+ craftingDetails["PercentFasterPerLevel"]  +"% per every level.</color>\n";
                        break;
                    default:
                        messagesText = "No such stat: " + args[0];
                        messagesText += "\nYou must choose from these stats: <color=" + colors[Skills.MINING] + ">Mining</color>, <color=" + colors[Skills.SKINNING] + ">Skinning</color>, <color=" + colors[Skills.WOODCUTTING] + ">Woodcutting</color>, <color=" + colors[Skills.CRAFTING] + ">Crafting</color>";
                        break;
                }
            }
            else
            {
                messagesText = "You must choose from these stats: <color=" + colors[Skills.MINING] + ">Mining</color>, <color=" + colors[Skills.SKINNING] + ">Skinning</color>, <color=" + colors[Skills.WOODCUTTING] + ">Woodcutting</color>, <color=" + colors[Skills.CRAFTING] + ">Crafting</color>";
            }
            PrintToChat(player, messagesText);
        }

        [ChatCommand("statsui")]
        private void StatsUICommand(BasePlayer player, string command, string[] args)
        {
            if (guioff.Contains(player.userID))
            {
                guioff.Remove(player.userID);
                RenderUI(player);
            }
            else
            {
                guioff.Add(player.userID);
                CuiHelper.DestroyUi(player, "StatsUI"); ;
            }
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (inPlayerList(player.userID))
                RenderUI(player);
        }

        void OnLootEntity(BasePlayer looter, BaseEntity target)
        {
            if (!guioff.Contains(looter.userID))
            {
                CuiHelper.DestroyUi(looter, "StatsUI");
            }
        }

        void OnLootPlayer(BasePlayer looter, BasePlayer beingLooter)
        {
            OnLootEntity(looter, null);
        }

        void OnLootItem(BasePlayer looter, Item lootedItem)
        {
            OnLootEntity(looter, null);
        }

        private void FillElements(ref CuiElementContainer elements, string mainPanel, int rowNumber, int maxRows, long level, int percent, string skillName, string progressColor, int fontSize, string xpBarAnchorMin, string xpBarAnchorMax)
        {
            float value = 1 / (float)maxRows;
            float positionMin = 1 - (value * rowNumber);
            float positionMax = 2 - (1 - (value * (1 - rowNumber)));
            var xpBarPlaceholder1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = mainPanel,
                Components =
                        {
                            new CuiImageComponent { Color = "0.4 0.4 0.4 0.2" },
                            new CuiRectTransformComponent{ AnchorMin = "0 " + positionMin.ToString("0.####"), AnchorMax = $"1 "+ positionMax.ToString("0.####") }
                        }
            };
            elements.Add(xpBarPlaceholder1);

            var innerXPBar1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = xpBarPlaceholder1.Name,
                Components =
                        {
                            new CuiImageComponent { Color = "0 0 0 0.8"},
                            new CuiRectTransformComponent{ AnchorMin = xpBarAnchorMin, AnchorMax = xpBarAnchorMax }
                        }
            };
            elements.Add(innerXPBar1);

            var innerXPBarProgress1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = innerXPBar1.Name,
                Components =
                        {
                            new CuiImageComponent() { Color = progressColor},
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = (percent / 100.0).ToString() + " 1" }
                        }
            };
            elements.Add(innerXPBarProgress1);

            var innerXPBarText1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = innerXPBar1.Name,
                Components =
                        {
                            new CuiTextComponent { Color = "1 1 1 1", Text = skillName, FontSize = fontSize, Align = TextAnchor.MiddleCenter},
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1" }
                        }
            };
            elements.Add(innerXPBarText1);

            var xpText1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = xpBarPlaceholder1.Name,
                Components =
                        {
                            new CuiTextComponent { Text = percent + "%", FontSize = fontSize, Align = TextAnchor.MiddleRight, Color = "0.749019608 0.760784314 0.780392157 1" },
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0.98 1" }
                        }
            };
            elements.Add(xpText1);

            var lvText1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = xpBarPlaceholder1.Name,
                Components =
                        {
                            new CuiTextComponent { Text = "Lv." + level, FontSize = fontSize, Align = TextAnchor.MiddleLeft, Color = "0.749019608 0.760784314 0.780392157 1" },
                            new CuiRectTransformComponent{ AnchorMin = "0.01 0", AnchorMax = $"0.5 1" }
                        }
            };
            elements.Add(lvText1);
        }

        private void RenderUI(BasePlayer player)
        {
            if (guioff.Contains(player.userID))
                return;
            Dictionary<string, string> skillColors = new Dictionary<string, string>();
            skillColors.Add("WC", "0.8 0.4 0 1");
            skillColors.Add("M", "0.1 0.5 0.8 0.6");
            skillColors.Add("S", "0.8 0.1 0 0.6");
            skillColors.Add("C", "0.2 0.72 0.5 0.8");
            int enabledSkillCount = 0;
            foreach (string skill in Skills.ALL)
            {
                if (!IsSkillDisabled(skill))
                    enabledSkillCount++;
            }


            CuiHelper.DestroyUi(player, "StatsUI");

            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.1 0.1 0.1 0.7"
                },
                RectTransform =
                {
                    AnchorMin = "0.67 0.04525",
                    AnchorMax = "0.819 0.1625"
                }
            }, "HUD/Overlay", "StatsUI");

            int fontSize = 12;
            string xpBarAnchorMin = "0.16 0.1";
            string xpBarAnchorMax = "0.88 0.9";
            int currentSKillIndex = 1;


            foreach (string skill in Skills.ALL)
            {
                if (!IsSkillDisabled(skill))
                {
                    FillElements(ref elements, mainName, currentSKillIndex, enabledSkillCount, getLevel(player.userID, skill), getExperiencePercentInt(player,
                        skill), messages[skill + "Skill"].ToString(), skillColors[skill], fontSize, xpBarAnchorMin, xpBarAnchorMax);
                    currentSKillIndex++;
                }
            }

            CuiHelper.AddUi(player, elements);
        }

        private string getStatPrint(BasePlayer player, string skill)
        {
            if (IsSkillDisabled(skill))
                return "";

            bool skillMaxed = (int) levelCaps[skill] != 0 && getLevel(player.userID, skill) == (int) levelCaps[skill];
            string bonusText = "";
            if (skill == Skills.CRAFTING)
                bonusText =
                    (getLevel(player.userID, skill)*(int) craftingDetails["PercentFasterPerLevel"]).ToString("0.##");
            else
                bonusText = ((getGathMult(getLevel(player.userID, skill), skill) - 1)*100).ToString("0.##");

            return string.Format("<color=" + colors[skill] + '>' + (string) messages["StatsText"] + "</color>\n",
                (string) messages[skill + "Skill"],
                getLevel(player.userID, skill) + (Convert.ToInt32(levelCaps[skill]) > 0 ? ("/" + levelCaps[skill]) :""),
                getPoints(player.userID, skill),
                skillMaxed ? "â" : getLevelPoints(getLevel(player.userID, skill) + 1).ToString(),
                bonusText,
                getExperiencePercent(player, skill),
                getPenaltyPercent(player, skill) + "%");

        }

        #endregion

        #region Main/Other

        /// <summary>
        /// Converts the given date value to epoch time.
        /// </summary>
        long ToEpochTime(DateTime dateTime)
        {
            var date = dateTime.ToUniversalTime();
            var ticks = date.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, 0).Ticks;
            var ts = ticks / TimeSpan.TicksPerSecond;
            return ts;
        }

        /// <summary>
        /// Converts the given epoch time to a <see cref="DateTime"/> with <see cref="DateTimeKind.Utc"/> kind.
        /// </summary>
        DateTime ToDateTimeFromEpoch(long intDate)
        {
            var timeInTicks = intDate * TimeSpan.TicksPerSecond;
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddTicks(timeInTicks);
        }

        private void Loaded()
        {
            StartConnection();

            if ((_craftData = Interface.GetMod().DataFileSystem.ReadObject<CraftData>("ZLevelsCraftDetails")) == null)
            {
                _craftData = new CraftData();
            }
            
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                loadUser(player);
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BasePlayer)
            {
                BasePlayer player = (BasePlayer)entity;
                if (!inPlayerList(player.userID)) return;

                string penaltyText ="<color=#FF0000>You have lost XP for dying:";
                bool penaltyExist = false;
                foreach (string skill in Skills.ALL)
                {
                    if (!IsSkillDisabled(skill))
                    {
                        int penalty = GetPenalty(player, skill);
                        if(penalty > 0)
                        {
                            penaltyText += "\n* -" + penalty + " " + messages[skill + "Skill"] + " XP.";
                            removePoints(player.userID, skill, penalty);
                            penaltyExist = true;
                        }
                    }
                }
                penaltyText += "</color>";

                if(penaltyExist)
                    PrintToChat(player,penaltyText);
                SetPlayerLastDeathDate(player.userID);
                RenderUI(player);
            }

        }

        void SetPlayerLastDeathDate(ulong userID)
        {
            setPlayerData(userID, "LastDeath", ToEpochTime(DateTime.UtcNow));
        }


        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity as BasePlayer;
            if (player == null) return;

            if (!IsSkillDisabled(Skills.WOODCUTTING))
                if ((int)dispenser.gatherType == 0) levelHandler(player, item, Skills.WOODCUTTING);
            if (!IsSkillDisabled(Skills.MINING))
                if ((int)dispenser.gatherType == 1) levelHandler(player, item, Skills.MINING);
            if (!IsSkillDisabled(Skills.SKINNING))
                if ((int)dispenser.gatherType == 2) levelHandler(player, item, Skills.SKINNING);
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            string skillName = string.Empty;
            switch (item.info.shortname.ToLower())
            {
                case "wood":
                    skillName = Skills.WOODCUTTING;
                    break;
                case "cloth":
                case "mushroom":
				case "corn":
				case "pumpkin":
				case "seed.pumpkin":
				case "seed.corn":
                    skillName = Skills.SKINNING;
                    break;
                case "metal.ore":
                case "sulfur.ore":
                case "stones":
                    skillName = Skills.MINING;
                    break;
            }

            if(!string.IsNullOrEmpty(skillName))
                levelHandler(player, item, skillName);
            else
                Puts("Developer missed this item, which can be picked up: [" + item.info.shortname + "]. Let him know on Oxide forums!");
        }

        void levelHandler(BasePlayer player, Item item, string skill)
        {
            string xpPercentBefore = getExperiencePercent(player, skill);
            long Level = getLevel(player.userID, skill);
            long Points = getPoints(player.userID, skill);
            item.amount = (int)(item.amount * getGathMult(Level, skill));

            int pointsToGet = (int)pointsPerHit[skill];
            long xpMultiplier = Convert.ToInt64(playerList[player.userID]["XPMultiplier"]);
            Points += Convert.ToInt64(pointsToGet * (xpMultiplier/100f));
            getPointsLevel(Points, skill);
            try
            {
                if (Points >= getLevelPoints(Level + 1))
                {
                    bool maxLevel = (int)levelCaps[skill] > 0 && Level + 1 > (int)levelCaps[skill];
                    if (!maxLevel)
                    {
                        Level = getPointsLevel(Points, skill);
                        PrintToChat(player, string.Format("<color=" + colors[skill] + '>' + (string)messages["LevelUpText"] + "</color>",
                            (string)messages[skill + "Skill"],
                            Level,
                            Points,
                            getLevelPoints(Level + 1),
                            ((getGathMult(Level, skill) - 1) * 100).ToString("0.##")
                            )
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Puts(ex.Message);
            }

            setPointsAndLevel(player.userID, skill, Points, Level);

            string xpPercentAfter = getExperiencePercent(player, skill);
            if (!xpPercentAfter.Equals(xpPercentBefore))
                RenderUI(player);
        }
        #endregion

        #region Utility
        private long getLevelPoints(long level)
        {
            return 110 * level * level - 100 * level;
        }

        private long getPointsLevel(long points, string skill)
        {
            int a = 110;
            int b = 100;
            long c = -points;
            double x1 = (-b - Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
            if ((int)levelCaps[skill] == 0 || (int)-x1 <= (int)levelCaps[skill])
                return (int)-x1;
            else
                return (int)levelCaps[skill];
        }

        double getGathMult(long skillLevel, string skill)
        {
            return 1 + Convert.ToDouble(resourceMultipliers[skill]) * 0.1 * (skillLevel - 1);
        }

        private bool inPlayerList(UInt64 userID)
        {
            return playerList.ContainsKey(userID);

        }
        #endregion

        #region Saving
        void OnServerSave()
        {
            SaveUsers();
        }

        private void Unload()
        {
            SaveUsers();
            if (_mySqlConnection != null)
                _mySqlConnection = null;
            else
            {
                _sqLiteConnection = null; 
            }

            foreach (var player in BasePlayer.activePlayerList)  // destroy UI when unloading.
            {
                if (guioff.Contains(player.userID))
                    return;
                CuiHelper.DestroyUi(player, "StatsUI");
            }
        }
        #endregion

        #region Config
        private Dictionary<string, object> resourceMultipliers;
        private Dictionary<string, object> levelCaps;
        private Dictionary<string, object> pointsPerHit;
        private Dictionary<string, object> craftingDetails;
        private Dictionary<string, object> percentLostOnDeath;
        private Dictionary<string, object> messages;
        private Dictionary<string, object> dbConnection;

        protected override void LoadDefaultConfig() { }

        void Init()
        {
            resourceMultipliers = checkCfg<Dictionary<string, object>>("ResourcePerLevelMultiplier", new Dictionary<string, object>{
                {Skills.WOODCUTTING, 2.0d},
                {Skills.MINING, 2.0d},
                {Skills.SKINNING, 2.0d}
            });
            levelCaps = checkCfg<Dictionary<string, object>>("LevelCaps", new Dictionary<string, object>{
                {Skills.WOODCUTTING, 0},
                {Skills.MINING, 0},
                {Skills.SKINNING, 0},
                {Skills.CRAFTING, 20}
            });
            pointsPerHit = checkCfg<Dictionary<string, object>>("PointsPerHit", new Dictionary<string, object>{
                {Skills.WOODCUTTING, 30},
                {Skills.MINING, 30},
                {Skills.SKINNING, 30}
            });
            craftingDetails = checkCfg<Dictionary<string, object>>("CraftingDetails", new Dictionary<string, object>{
                { "TimeSpent", 1},
                { "XPPerTimeSpent", 3},
                { "PercentFasterPerLevel", 5 }
            });
            percentLostOnDeath = checkCfg<Dictionary<string, object>>("PercentLostOnDeath", new Dictionary<string, object>{
                {Skills.WOODCUTTING, 100},
                {Skills.MINING, 100},
                {Skills.SKINNING, 50},
                {Skills.CRAFTING, 50}
            });

            dbConnection = checkCfg<Dictionary<string, object>>("dbConnection", new Dictionary<string, object>{
                {"UseMySQL", false },
                {"Host", "127.0.0.1"},
                {"Port", 3306 },
                {"Username", "user" },
                {"Password", "password" },
                {"Database", "db" },
                {"GameProtocol", Protocol.network }
            });

            messages = checkCfg<Dictionary<string, object>>("Messages", new Dictionary<string, object>{
                {"StatsHeadline", "Level stats (/statinfo [statname] - To get more information about skill)"},
                {"StatsText",   "-{0}"+
                            "\nLevel: {1} (+{4}% bonus) \nXP: {2}/{3} [{5}].\n<color=red>-{6} XP loose on death.</color>"},
                {"LevelUpText", "{0} Level up"+
                            "\nLevel: {1} (+{4}% bonus) \nXP: {2}/{3}"},
                {"WCSkill", "Woodcutting"},
                {"MSkill", "Mining"},
                {"SSkill", "Skinning"},
                {"CSkill", "Crafting" }
            });
            SaveConfig();
        }

        private T checkCfg<T>(string conf, T def)
        {
            if (Config[conf] != null)
            {
                return (T)Config[conf];
            }
            else
            {
                Config[conf] = def;
                return def;
            }
        }
        #endregion

        #region Adds&Removse

        private void removePoints(UInt64 userID, string skill, long points)
        {
            if (playerList[userID][skill + "Points"] - 10 > points)
                playerList[userID][skill + "Points"] -= points;
            else
                playerList[userID][skill + "Points"] = 10;

            setLevel(userID, skill, getPointsLevel(playerList[userID][skill + "Points"], skill));
        }

        #endregion
        #region Gets&Sets

        private long getLevel(UInt64 userID, string skill)
        {
            if(!playerList.ContainsKey(userID))
                Puts("Trying to get [" + messages[skill + "Skill"].ToString() + "]. For player who's SteamID: [" + userID + "]. He is not on a user list yet?");
            if (!playerList[userID].ContainsKey(skill + "Level"))
                playerList[userID].Add(skill + "Level", 1);

            return playerList[userID][skill + "Level"];
        }

        private long getPoints(UInt64 userID, string skill)
        {
            if (!playerList[userID].ContainsKey(skill + "Points"))
                playerList[userID].Add(skill + "Points", 11);

            return playerList[userID][skill + "Points"];
        }

        private void setLevel(UInt64 userID, string skill, long level)
        {
            setPlayerData(userID, skill + "Level", level);
        }

        #endregion


        #region New stuff

        bool IsSkillDisabled(string skill)
        {
            return levelCaps[skill].ToString() == "-1";
        }

        bool usingMySQL()
        {
            return Convert.ToBoolean(dbConnection["UseMySQL"]);
        }

        int GetPenalty(BasePlayer player, string skill)
        {
            int penalty = 0;
            int penaltyPercent = getPenaltyPercent(player, skill);
            penalty = Convert.ToInt32(getPercentAmount(playerList[player.userID][skill + "Level"], penaltyPercent));
            return penalty;
        }

        int getPenaltyPercent(BasePlayer player, string skill)
        {
            int penaltyPercent = 0;
            Dictionary<string, long> details = playerList[player.userID];

            if (details.ContainsKey("LastDeath"))
            {
                DateTime currentTime = DateTime.UtcNow;
                DateTime lastDeath = ToDateTimeFromEpoch(details["LastDeath"]);
                TimeSpan timeAlive = currentTime - lastDeath;
                if (timeAlive.TotalMinutes > 10)
                {
                    penaltyPercent = ((int)percentLostOnDeath[skill] - ((int)timeAlive.TotalHours * (int)percentLostOnDeath[skill] / 10));
                    if (penaltyPercent < 0)
                        penaltyPercent = 0;
                }
            }
            return penaltyPercent;
        }

        [HookMethod("OnItemCraftFinished")]
        object OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (IsSkillDisabled(Skills.CRAFTING))
                return null;

            BasePlayer crafter = task.owner;
            string xpPercentBefore = getExperiencePercent(crafter, Skills.CRAFTING);
            if (task.blueprint == null)
            {
                Puts("There is problem obtaining task.blueprint on 'OnItemCraftFinished' hook! This is usually caused by some incompatable plugins.");
                return null;
            }
            int experienceGain = Convert.ToInt32(Math.Floor((task.blueprint.time + 0.99f) / (int)craftingDetails["TimeSpent"]));//(int)task.blueprint.time / 10;
            if (experienceGain == 0)
                return null;

            long Level = 0;
            long Points = 0;
            try
            {
                Level = getLevel(crafter.userID, Skills.CRAFTING);
                Points = getPoints(crafter.userID, Skills.CRAFTING);
            }
            catch (Exception ex)
            {
                Puts("Problem when getting level/points for player. Error:" + ex.StackTrace);
            }
            Points += experienceGain * (int)craftingDetails["XPPerTimeSpent"];
            if (Points >= getLevelPoints(Level + 1))
            {
                bool maxLevel = (int)levelCaps[Skills.CRAFTING] > 0 && Level + 1 > (int)levelCaps[Skills.CRAFTING];
                if (!maxLevel)
                {
                    Level = getPointsLevel(Points, Skills.CRAFTING);
                    PrintToChat(crafter, string.Format("<color=" + colors[Skills.CRAFTING] + '>' + (string)messages["LevelUpText"] + "</color>",
                        (string)messages["CSkill"],
                        Level,
                        Points,
                        getLevelPoints(Level + 1),
                        (getLevel(crafter.userID, Skills.CRAFTING) * Convert.ToDouble(craftingDetails["PercentFasterPerLevel"])).ToString()
                        )
                    );
                }
            }
            try
            {
                if (item.info.shortname != "lantern_a" && item.info.shortname != "lantern_b")
                {
                    setPointsAndLevel(crafter.userID, Skills.CRAFTING, Points, Level);
                }
            }
            catch (Exception ex)
            {
                Puts("Problem when setting crafting xp/level for player. Error information:" + ex.StackTrace);
            }

            try {
                string xpPercentAfter = getExperiencePercent(crafter, Skills.CRAFTING);
                if (!xpPercentAfter.Equals(xpPercentBefore))
                    RenderUI(crafter);
            }catch(Exception ex)
            {
                Puts("Problem when checking if we should RenderUI: " + ex.StackTrace);
            }


            if (task.amount > 0) return null;
            if (task.blueprint != null && task.blueprint.name.Contains("(Clone)"))
            {
                var behaviours = task.blueprint.GetComponents<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour.name.Contains("(Clone)")) UnityEngine.Object.Destroy(behaviour);
                }
                task.blueprint = null;
            }
            return null;
        }

        private object OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        {
            if (IsSkillDisabled(Skills.CRAFTING))
                return null;

            long Level = getLevel(crafter.userID, Skills.CRAFTING);

            var craftingTime = task.blueprint.time;
            var amountToReduce = task.blueprint.time * ((float)(Level * (int)craftingDetails["PercentFasterPerLevel"]) / 100);
            craftingTime -= amountToReduce;
            if (craftingTime < 0)
                craftingTime = 0;
            if (craftingTime == 0)
            {
                try {
                    foreach (var entry in _craftData.CraftList)
                    {
                        var itemname = task.blueprint.targetItem.shortname.ToString();
                        if (entry.Value.shortName == itemname && entry.Value.Enabled)
                        {
                            int amount = task.amount;
                            if (amount >= entry.Value.MinBulkCraft && amount <= entry.Value.MaxBulkCraft)
                            {
                                ItemDefinition item = GetItem(itemname);
                                int final_amount = task.blueprint.amountToCreate * amount;
                                var newItem = ItemManager.CreateByItemID(item.itemid, (int)final_amount);
                                crafter.inventory.GiveItem(newItem);

                                string returnstring = "You have crafted <color=#66FF66>" + amount.ToString() + "</color> <color=#66FFFF>" + item.displayName.english.ToString() + "</color>\n[Batch Amount: <color=#66FF66>" + final_amount.ToString() + "</color>]";
                                PrintToChat(crafter, returnstring);
                                return false;
                            }
                        }
                    }
                }
                catch
                {
                    // Generate only when someone reached instant craft level.
                    GenerateItems(false);
                }
            }

            if (!task.blueprint.name.Contains("(Clone)"))
                task.blueprint = UnityEngine.Object.Instantiate(task.blueprint);
            task.blueprint.time = craftingTime;
            return null;
        }

        int MaxB = 999;
        int MinB = 10;
        int Cooldown = 0;

        /*
            Thanks Norn for this piece of code!
            It was borrowed from his plugin:
            http://oxidemod.org/threads/magic-craft.11784/
        */
        void GenerateItems(bool reset = false)
        {
            if (!reset)
            {
                string config_protocol = dbConnection["GameProtocol"].ToString();
                if (config_protocol != Protocol.network.ToString())
                {
                    dbConnection["GameProtocol"] = Protocol.network.ToString();
                    Puts("Updating item list from protocol " + config_protocol.ToString() + " to protocol " + dbConnection["GameProtocol"] + ".");
                    GenerateItems(true);
                    SaveConfig();
                    return;
                }
            }

            if (reset)
            {
                Interface.GetMod().DataFileSystem.WriteObject("ZLevelsCraftDetails.old", _craftData);
                _craftData.CraftList.Clear();
                Puts("Generating new item list...");
            }
            mcITEMS = ItemManager.itemList.ToDictionary(i => i.shortname);
            int loaded = 0, enabled = 0;
            foreach (var definition in mcITEMS)
            {
                if (definition.Value.shortname.Length >= 1)
                {
                    CraftInfo p = null;
                    if (_craftData.CraftList.TryGetValue(definition.Value.shortname, out p))
                    {
                        if (p.Enabled) { enabled++; }
                        loaded++;
                    }
                    else
                    {
                        CraftInfo z = new CraftInfo();
                        z.shortName = definition.Value.shortname.ToString();
                        z.MaxBulkCraft = MaxB;
                        z.MinBulkCraft = MinB;
                        z.Enabled = true;
                        _craftData.CraftList.Add(definition.Value.shortname.ToString(), z);
                        loaded++;
                    }
                }
            }
            int inactive = loaded - enabled;
            Puts("Loaded " + loaded.ToString() + " items. (Enabled: " + enabled.ToString() + " | Inactive: " + inactive.ToString() + ").");
            Interface.GetMod().DataFileSystem.WriteObject("ZLevelsCraftDetails", _craftData);
        }

        class CraftInfo
        {
            public int MaxBulkCraft;
            public int MinBulkCraft;
            public string shortName;
            public bool Enabled;
            public CraftInfo()
            {
            }
        }

        private Dictionary<string, ItemDefinition> mcITEMS;

        private ItemDefinition GetItem(string shortname)
        {
            if (string.IsNullOrEmpty(shortname) || mcITEMS == null) return null;
            ItemDefinition item;
            if (mcITEMS.TryGetValue(shortname, out item)) return item;
            return null;
        }

        long getPointsNeededForNextLevel(long level)
        {
            long startingPoints = getLevelPoints(level);
            long nextLevelPoints = getLevelPoints(level + 1);
            long pointsNeeded = nextLevelPoints - startingPoints;
            return pointsNeeded;
        }

        long getPercentAmount(long level, int percent)
        {
            long points = getPointsNeededForNextLevel(level);
            long percentPoints = (points * percent) / 100;
            return percentPoints;
        }

        int getExperiencePercentInt(BasePlayer player, string skill)
        {
            long Level = getLevel(player.userID, skill);
            long startingPoints = getLevelPoints(Level);
            long nextLevelPoints = getLevelPoints(Level + 1) - startingPoints;
            long Points = getPoints(player.userID, skill) - startingPoints;
            int experienceProc = Convert.ToInt32((Points / (double)nextLevelPoints) * 100);
            if (experienceProc >= 100)
                experienceProc = 99;
            else if (experienceProc == 0)
                experienceProc = 1;
            return experienceProc;
        }

        string getExperiencePercent(BasePlayer player, string skill)
        {
            string percent = getExperiencePercentInt(player, skill).ToString() + "%";
            return percent;
        }


        #endregion
    }
}