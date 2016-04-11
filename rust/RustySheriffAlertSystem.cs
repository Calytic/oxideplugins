using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using System.IO;
using Oxide.Core.Libraries;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("RaidAlert", "By JV", "1.0.4")]
    class RustySheriffAlertSystem : RustPlugin
    {
        List<BasePlayer> allBasePlayer;
        List<Auth> authList = new List<Auth>();
        List<DrawListEntry> drawList = new List<DrawListEntry>();
        List<IgnoreListEntry> ignoreList = new List<IgnoreListEntry>();
        List<ulong> muteList = new List<ulong>();
        List<Trespasser> newTrespassers = new List<Trespasser>();
        List<Trespasser> oldTrespassers = new List<Trespasser>();
        List<Perimeter> perimeters = new List<Perimeter>();
        List<TestAlert> testAlerts = new List<TestAlert>();
        List<ValReq> valReqs = new List<ValReq>();
        List<ulong> validated = new List<ulong>();

        Dictionary<ulong, int> authDict = new Dictionary<ulong, int>();
        Dictionary<ulong, int> ignoreDict = new Dictionary<ulong, int>();
        Dictionary<ulong, int> perimeterDict = new Dictionary<ulong, int>();
        
        private static DateTime epoch;

        string url = "http://jvetech.co.uk";

        double drawCheck = 0;
        double nextCheck = 0;
        double playerUpdateCheck = 0;

        int maxPerimeterDelta = 75;
        int maxPerimeterPoints = 50;
        int maxPerimetersPerPlayer = 4;
        int playerCheckIndex = 0;
        int timeBetweenChecks = 30;

        bool alertsVisibleInGame = true;
        bool alertTriggeredOnlyWhenSleepingInPerimeter = false;
        bool automaticCheckingEnabled = true;
        bool enabledForAuthorisedUsersOnly = false;
        bool sendAnon = false;
        bool syncWithRustySheriffServer = true;
        bool updatedOldData = false;
        bool useCupboards = false;
        bool adminsAuthed = false;
        
        private FieldInfo buildingPrivlidges;

        float perimeterHeightDelta = 1.5f;
        System.Random rnd = new System.Random(DateTime.Now.Millisecond);

        [ChatCommand("rs")]
        void cmdChatRS(BasePlayer player, string command, string[] args)
        {
            if (getAuthLevel(player.userID) >= 0 || !enabledForAuthorisedUsersOnly)
            {
                if (args == null || args.Length == 0)
                {
                    showMainMenu(player);
                    return;
                }
                else
                {
                    if (args[0] == "set") { set(player, args); return; }
                    if (args[0] == "view") { viewPerimeters(player, args); return; }
                    if (args[0] == "start") { startPerimeter(player, args); return; }
                    if (args[0] == "add") { addPerimeterEntry(player, false); return; }
                    if (args[0] == "stop") { stopPerimeter(player); return; }
                    if (args[0] == "cancel") { cancelPerimeter(player); return; }
                    if (args[0] == "clear") { clearPerimeters(player); return; }
                    if (args[0] == "delete") { deletePerimeter(player, args); return; }
                    if (args[0] == "validate") { validatePlayer(player, false); return; }
                    if (args[0] == "validatenew") { validatePlayer(player, true); return; }
                    if (args[0] == "ignore") { ignorePlayer(player, args); return; }
                    if (args[0] == "unignore") { unignorePlayer(player, args); return; }
                    if (args[0] == "clearignores") { clearIgnoreList(player); return; }
                    if (args[0] == "ignores") { viewIgnoreList(player); return; }
                    if (args[0] == "ignoredetect") { ignoreDetect(player); return; }
                    if (args[0] == "mute") { muteIngameAlerts(player, true); return; }
                    if (args[0] == "unmute") { muteIngameAlerts(player, false); return; }
                    if (args[0] == "help") { showHelpMenu(player); return; }
                    if (args[0] == "undo") { undoLastPoint(player); return; }
                    if (args[0] == "test") { testPerimeter(player); return; }
                    if (args[0] == "adv") { showAdvancedMenu(player); return; }

                    if (getAuthLevel(player.userID) == 2)
                    {
                        if (args[0] == "time") { setTimeBetweenChecks(player, args); return; }
                        if (args[0] == "adduser") { addUser(player, args); return; }
                        if (args[0] == "deluser") { delUser(player, args); return; }
                        if (args[0] == "enable") { toggleAutoCheck(player, true); return; }
                        if (args[0] == "disable") { toggleAutoCheck(player, false); return; }
                        if (args[0] == "save") { saveAllData(player); return; }
                        if (args[0] == "auths") { showAuthorisedUsers(player); return; }
                        if (args[0] == "secure") { toggleSecureMode(player); return; }
                        if (args[0] == "chatmute") { toggleChatMute(player); return; }
                        if (args[0] == "sleepers") { toggleSleepers(player); return; }
                        if (args[0] == "maxperim") { setMaxPerimeters(player, args); return; }
                        if (args[0] == "maxsize") { setMaxSize(player, args); return; }
                        if (args[0] == "maxpoints") { setMaxPoints(player, args); return; }
                        if (args[0] == "checkinterval") { setCheckInterval(player, args); return; }
                        if (args[0] == "sync") { toggleSync(player); return; }
                        if (args[0] == "status") { showStatus(player); return; }
                        if (args[0] == "anon") { toggleAnon(player); return; }
                        if (args[0] == "cupboard") { toggleUseCupboard(player); return; }
                        if (args[0] == "admin") { showAdminMenu(player);
                        Puts("|{}+*#");
                        Puts("CLEAN - " + cleanString("|{}+*#"));
                        return; }
                        if (args[0] == "auth") { showAuthMenu(player); return; }
                    }

                    SendReply(player, "Please enter a valid command.  Do /rs for a list.");
                }
            }
        }

        [ConsoleCommand("rs.adduser")]
        void cmdRSAddUser(ConsoleSystem.Arg arg)
        {
            addUser(null, arg.Args);
        }
        [ConsoleCommand("rs.auths")]
        void cmdRSAuths(ConsoleSystem.Arg arg)
        {
            showAuthorisedUsers(null);
        }
        [ConsoleCommand("rs.deluser")]
        void cmdRSDelUser(ConsoleSystem.Arg arg)
        {
            delUser(null, arg.Args);
        }
        [ConsoleCommand("rs.save")]
        void cmdRSSave(ConsoleSystem.Arg arg)
        {
            saveAllData(null);
            Puts("Config and data saved.");
        }

        void addPerimeter(BasePlayer player, string perimeterName)
        {
            if (isInProgress(player.userID))
            {
                SendReply(player, "You already have a perimeter in progress.  Do /rs cancel to remove it.");
                return;
            }

            perimeterName = cleanString(perimeterName);

            int perimeterIndex = getPerimeterIndex(player.userID);
            if (perimeterIndex == -1)
            {
                Perimeter per = new Perimeter();
                per.steamid = player.userID;
                perimeters.Add(per);
                perimeterDict.Add(player.userID, perimeters.Count - 1);
                perimeterIndex = perimeters.Count - 1;
            }

            if (!isPerimeterNameInUse(player.userID, perimeterName))
            {
                PlayerPerimeter pl = new PlayerPerimeter();
                pl.name = perimeterName;

                Vector3 v = new Vector3();
                v.x = player.transform.position.x;
                v.y = player.transform.position.y;// +perimeterHeightDelta;
                v.z = player.transform.position.z;

                pl.coords.Add(v);
                pl.finished = false;
                pl.id = (uint)rnd.Next(1000, 999999999);

                perimeters[perimeterIndex].playerPerimeters.Add(pl);

                SendReply(player, "Started perimeter entry for perimeter " + perimeterName + ".  Do /rs add to enter, and /rs stop for the last waypoint.");

                //*** ENSURE ANY PLAYER TIMERS ARE DESTROYED HERE ***//
                plotPerimeter(player, perimeterIndex, perimeters[perimeterIndex].playerPerimeters.Count - 1);

                DrawListEntry d = new DrawListEntry();
                d.basePlayer = player;
                d.perimeterIndex = perimeterIndex;
                d.playerPerimeterIndex = perimeters[perimeterIndex].playerPerimeters.Count - 1;

                drawList.Add(d);
            }
            else
            {
                SendReply(player, "You already have a perimeter called " + perimeterName + ". Do /rs delete <index> to remove it, or choose another name.");
            }
        }
        int addPerimeterEntry(BasePlayer player, bool stop)
        {
            PerimeterPosition pPos = getInProgress(player.userID);

            if (pPos.perimeterIndex == -1)
            {
                SendReply(player, "You need to start a perimeter first.  Do /rs start <name>, or /rs set for auto-creation.");
                return -1;
            }

            string perimeterName = perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].name;
            int perimeterCount = perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords.Count;
            int authLevel = getAuthLevel(player.userID);

            if (!stop)
            {
                if ((perimeterCount > maxPerimeterPoints - 1) && authLevel < 1)
                {
                    SendReply(player, "You cannot enter any more waypoints for perimeter " + perimeterName + ".  Do /rs stop to finalise this perimeter or /rs cancel discard it.");
                    return -1;
                }
            }

            Vector3 ePoint = player.transform.position;
            //ePoint.y += perimeterHeightDelta;

            perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords.Add(ePoint);

            if (!findTolerances(pPos) && authLevel < 1)
            {
                SendReply(player, "You cannot add this waypoint as the resulting perimeter will be larger than permitted. (" + maxPerimeterDelta.ToString() + " x " + maxPerimeterDelta.ToString() + ")");
                perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords.RemoveAt(perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords.Count - 1);
                return -1;
            }

            if (authLevel < 1)
            {
                SendReply(player, "Added current location to perimeter " + perimeterName + ". - " + (perimeterCount + 1).ToString() + "/" + maxPerimeterPoints.ToString() + ".");
            }
            else
            {
                SendReply(player, "Added current location to perimeter " + perimeterName + ".");
            }

            plotPerimeter(player, pPos.perimeterIndex, pPos.playerPerimeterIndex);
            return perimeterCount + 1;
        }
        void addUser(BasePlayer initPlayer, string[] args)
        {
            int argIndexDelta = 0;

            if (initPlayer != null)
            {
                //called from ingame
                argIndexDelta = 1;
            }

            string name = "";

            if ((args == null && initPlayer == null) || (args.Length == 1 && initPlayer != null))
            {
                if (args != null)
                {
                    SendReply(initPlayer, "Do /rs adduser followed by either <steamid> <authlevel> or <name> <authlevel> if the player's online.");
                    SendReply(initPlayer, "Or /rs <steamid> <name> <authLevel> if they're offline.");
                    SendReply(initPlayer, "0 = Normal User, 1 = Privileged user, 2 = Admin.");
                    return;
                }
                else
                {
                    Puts("Do /rs adduser followed by either <steamid> <authlevel> or <name> <authlevel> if the player's online.");
                    Puts("Or /rs <steamid> <name> <authLevel> if they're offline.");
                    Puts("0 = Normal User, 1 = Privileged user, 2 = Admin.");
                    return;
                }
            }

            if (args.Length < 2)
            {
                if (initPlayer != null)
                {
                    SendReply(initPlayer, "Do /rs adduser followed by either <steamid> <authlevel> or <name> <authlevel> if the player's online.  Or /rs <steamid> <name> <authLevel> if they're offline.  0 = Normal User, 1 = Privileged user, 2 = Admin.");
                    return;
                }
                else
                {
                    Puts("Do /rs adduser followed by either <steamid> <authlevel> or <name> <authlevel> if the player's online.");
                    Puts("Or /rs <steamid> <name> <authLevel> if they're offline.");
                    Puts("0 = Normal User, 1 = Privileged user, 2 = Admin.");
                    return;
                }
            }

            //check authLevel
            int argIndex = -1;

            if (args.Length == 2 + argIndexDelta)
            {
                argIndex = 1 + argIndexDelta;
            }
            else
            {
                argIndex = 2 + argIndexDelta;
            }

            int authLevel;

            try
            {
                authLevel = Convert.ToInt32(args[argIndex]);
            }
            catch (Exception e)
            {
                if (initPlayer != null)
                {
                    SendReply(initPlayer, "Invalid auth level entered.  0 = Normal User, 1 = Privileged User, 2 = Admin.");
                }
                else
                {
                    Puts("Invalid auth level entered.  0 = Normal User, 1 = Privileged User, 2 = Admin.");
                }
                return;
            }

            ulong steamid = 0;

            if (args.Length == 2 + argIndexDelta)
            {
                //can be steamid or playername + authlevel
                //check if steamid
                bool match = false;

                if (args[argIndexDelta].Length == 17)
                {
                    match = false;
                    name = "";

                    if (args[argIndexDelta].Substring(0, 6) == "765611")
                    {
                        try
                        {
                            steamid = Convert.ToUInt64(args[argIndexDelta]);
                        }
                        catch (Exception e)
                        {
                            if (initPlayer != null)
                            {
                                SendReply(initPlayer, "Invalid SteamID entered.");
                            }
                            else
                            {
                                Puts("Invalid SteamID entered.");
                            }
                            return;
                        }

                        for (int i = 0; i < allBasePlayer.Count; i++)
                        {
                            if (allBasePlayer[i].userID == (ulong)steamid)
                            {
                                name = allBasePlayer[i].displayName;
                                match = true;
                                break;
                            }
                        }
                    }
                }

                if (!match)
                {
                    //check for playername instead
                    int r = checkDupes(args[argIndexDelta]);

                    if (r >= 0)
                    {
                        for (int i = 0; i < allBasePlayer.Count; i++)
                        {
                            if (allBasePlayer[i].displayName.Length >= args[argIndexDelta].Length)
                            {
                                if (allBasePlayer[i].displayName.Substring(0, args[argIndexDelta].Length) == args[argIndexDelta])
                                {
                                    steamid = allBasePlayer[i].userID;
                                    name = allBasePlayer[i].displayName;
                                    match = true;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (initPlayer != null)
                        {
                            if (r == -2)
                            {
                                SendReply(initPlayer, "There is more than 1 player with that name.");
                            }
                            else
                            {
                                SendReply(initPlayer, "There are no players with that name.");
                            }
                        }
                        else
                        {
                            if (r == -2)
                            {
                                Puts("There is more than 1 player with that name.");
                            }
                            else
                            {
                                Puts("There are no players with that name.");
                            }
                        }
                        return;
                    }
                }

                if (match)
                {
                    int aIndex = -1;

                    if (getPlayerIndex(steamid) != -1)
                    {
                        setAuthLevel(steamid, authLevel, name, initPlayer,false);
                    }
                }
                else
                {
                    if (initPlayer != null)
                    {
                        SendReply(initPlayer, "No players with that name or SteamID are currently online.  Try /rs adduser <steamid> <name> <authlevl> to add an offline player.");
                    }
                    else
                    {
                        Puts("No players with that name or SteamID are currently online.  Try /rs adduser <steamid> <name> <authlevl> to add an offline player.");
                    }
                }
            }

            if (args.Length == 3 + argIndexDelta)
            {
                //<steamid> <name> <level>
                try
                {
                    steamid = Convert.ToUInt64(args[argIndexDelta]);
                }
                catch (Exception e)
                {
                    if (initPlayer != null)
                    {
                        SendReply(initPlayer, "Invalid SteamID entered.");
                    }
                    else
                    {
                        Puts("Invalid SteamID entered.");
                    }
                    return;
                }

                name = args[1 + argIndexDelta];
                setAuthLevel(steamid, authLevel, name, initPlayer,false);
            }
        }
        void addValidationRequest(ulong steamid, bool newCode)
        {
            for (int i = 0; i < valReqs.Count; i++)
            {
                if (valReqs[i].steamid == steamid)
                {
                    valReqs.RemoveAt(i);
                    break;
                }
            }

            ValReq v = new ValReq();
            v.steamid = steamid;
            v.newCode = newCode;

            valReqs.Add(v);
        }
        void cancelPerimeter(BasePlayer player)
        {
            stopPerimeter(player, true);
        }
        void checkAdminsAuthed()
        {
            for (int i = 0; i < allBasePlayer.Count; i++)
            {
                if (isServerModerator(allBasePlayer[i]))
                {
                    setAuthLevel(allBasePlayer[i].userID, 2, allBasePlayer[i].displayName, null, true);
                }
            }
        }
        int checkDupes(string playerName)
        {
            int count = 0;
            int index = 0;

            for (int i = 0; i < allBasePlayer.Count; i++)
            {
                if (allBasePlayer[i].displayName.Length >= playerName.Length)
                {
                    if (allBasePlayer[i].displayName.Substring(0, playerName.Length) == playerName)
                    {
                        count++;
                        index = i;
                    }
                }
            }

            if (count == 0)
            {
                return -1;
            }

            if (count == 1)
            {
                return index;
            }

            //count must be > 1 so
            return -2;
        }
        void checkTrespass()
        {
            if (allBasePlayer.Count > playerCheckIndex)
            {
                BasePlayer serverPlayer = allBasePlayer[playerCheckIndex];
                int authLevel = getAuthLevel(serverPlayer.userID);
                if (authLevel < 2)
                {
                    string serverPlayerName = serverPlayer.displayName;
                    serverPlayerName = cleanString(serverPlayerName);
                    Vector3 serverPlayerPos = serverPlayer.transform.position;

                    for (int i = 0; i < perimeters.Count; i++)
                    {
                        for (int p = 0; p < perimeters[i].playerPerimeters.Count; p++)
                        {
                            bool doCheck = false;

                            if (enabledForAuthorisedUsersOnly)
                            {
                                if (authLevel == 0)
                                {
                                    if (alertTriggeredOnlyWhenSleepingInPerimeter)
                                    {
                                        if (perimeters[i].playerPerimeters[p].sleeperPresent)
                                        {
                                            doCheck = true;
                                        }
                                    }
                                    else
                                    {
                                        doCheck = true;
                                    }
                                }

                                if (authLevel > 0)
                                {
                                    doCheck = true;
                                }
                            }
                            else
                            {
                                if (alertTriggeredOnlyWhenSleepingInPerimeter)
                                {
                                    if (perimeters[i].playerPerimeters[p].sleeperPresent)
                                    {
                                        doCheck = true;
                                    }
                                }
                                else
                                {
                                    doCheck = true;
                                }
                            }

                            if (doCheck)
                            {
                                doCheck = false;

                                if (perimeters[i].playerPerimeters[p].finished)
                                {
                                    int wn = 0;
                                    if (serverPlayerPos.x >= perimeters[i].playerPerimeters[p].minX && serverPlayerPos.x <= perimeters[i].playerPerimeters[p].maxX)
                                    {
                                        if (serverPlayerPos.z >= perimeters[i].playerPerimeters[p].minY && serverPlayerPos.z <= perimeters[i].playerPerimeters[p].maxY)
                                        {
                                            PerimeterPosition pPos = new PerimeterPosition();
                                            pPos.perimeterIndex = i;
                                            pPos.playerPerimeterIndex = p;

                                            wn = windingPoly(serverPlayerPos, pPos);
                                        }
                                    }

                                    if (wn != 0)
                                    {
                                        if (syncWithRustySheriffServer && isValidated(serverPlayer.userID))
                                        {
                                            if (!isPlayerIgnored(perimeters[i].steamid, serverPlayer.userID))
                                            {
                                                Trespasser t = new Trespasser();
                                                t.id = perimeters[i].playerPerimeters[p].id;
                                                t.steamid = perimeters[i].steamid;
                                                t.name = cleanString(perimeters[i].playerPerimeters[p].name);
                                                t.trespasserSteamID = serverPlayer.userID;
                                                t.alertText = "|" + perimeters[i].playerPerimeters[p].id + "|" + cleanString(serverPlayerName) + "|" + serverPlayerPos.x.ToString() + "|" + serverPlayerPos.z.ToString();
                                                newTrespassers.Add(t);
                                            }
                                        }

                                        if (alertsVisibleInGame)
                                        {
                                            doCheck = true;
                                        }
                                        else
                                        {
                                            if (authLevel > 0)
                                            {
                                                doCheck = true;
                                            }
                                        }

                                        if (doCheck)
                                        {
                                            for (int t = 0; t < allBasePlayer.Count; t++)
                                            {
                                                if (perimeters[i].steamid == allBasePlayer[t].userID)
                                                {
                                                    if (!muteList.Contains(perimeters[i].steamid))
                                                    {
                                                        if (!isPlayerIgnored(perimeters[i].steamid, serverPlayer.userID))
                                                        {
                                                            bool isValid = true;

                                                            //if (authLevel == 2 && perimeters[i].steamid != serverPlayer.userID)
                                                            //{
                                                            //    isValid = false;
                                                            //}

                                                            //if (isValid)
                                                            //{
                                                            if (!sendAnon || getAuthLevel(perimeters[i].steamid) == 2)
                                                            {
                                                                SendReply(allBasePlayer[t], serverPlayerName + " " + serverPlayer.userID.ToString() + " has breached perimeter " + perimeters[i].playerPerimeters[p].name + ".");
                                                            }
                                                            else
                                                            {
                                                                SendReply(allBasePlayer[t], "Perimeter " + perimeters[i].playerPerimeters[p].name + " has been breached.");
                                                            }
                                                            // }

                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        string cleanString(string msg)
        {
            string newMsg = "";

            for (int i = 0; i < msg.Length; i++)
            {
                if (msg.Substring(i, 1) != "|")
                {
                    if (msg.Substring(i, 1) != "{")
                    {
                        if (msg.Substring(i, 1) != "}")
                        {
                            if (msg.Substring(i, 1) != "+")
                            {
                                if (msg.Substring(i, 1) != "*")
                                {
                                    if (msg.Substring(i, 1) != "#")
                                    {
                                        newMsg += msg.Substring(i, 1);
                                    }
                                }
                            }
                        }
                    }

                }
            }

            return newMsg;
        }
        void clearIgnoreList(BasePlayer player)
        {
            int count = 0;
            int ignoreListIndex = getIgnoreListIndex(player.userID);

            if (ignoreListIndex != -1)
            {
                count = ignoreList[ignoreListIndex].ignores.Count;
                ignoreList[ignoreListIndex].ignores.Clear();
            }
            else
            {
                SendReply(player, "Your ignore list is already empty.");
            }

            if (count == 0)
            {
                SendReply(player, "Your ignore list is already empty.");
            }
            else
            {
                SendReply(player, "Removed " + count.ToString() + " players from your ignore list.");
            }
        }
        void clearPerimeters(BasePlayer player)
        {
            int perimeterIndex = getPerimeterIndex(player.userID);

            if (perimeterIndex == -1)
            {
                SendReply(player, "You have no perimeters to remove.  Do /rs set <name> to create one.");
            }
            else
            {
                if (perimeters[perimeterIndex].playerPerimeters.Count == 0)
                {
                    SendReply(player, "You have no perimeters to remove.  Do /rs set <name> to create one.");
                    return;
                }

                if (isInProgress(player.userID))
                {
                    stopDrawing(player);
                }
                SendReply(player, "Removed " + perimeters[perimeterIndex].playerPerimeters.Count.ToString() + " perimeters on this server.");
                perimeters[perimeterIndex].playerPerimeters.Clear();
            }
        }
        void convertOldAlertsObj(OldAlertsObjectDict oldAlertsObj)
        {
            //Puts("Converting perimeters");
            //key is steamid to which perimeter belongs
            var keyArray = oldAlertsObj.alerts.Keys.ToArray();
            //for each steamid
            for (int i = 0; i < keyArray.Length; i++)
            {
                //Puts(keyArray[i]);
                int pIndex = getPerimeterIndex(Convert.ToUInt64(keyArray[i]));
                //key is alert name
                var pKeyArray = oldAlertsObj.alerts[keyArray[i]].Keys.ToArray();

                //for each alert name
                for (int p = 0; p < pKeyArray.Length; p++)
                {
                    //Puts(pKeyArray[p]);

                    int count = oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].count;
                    bool finished = oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].finished;
                    uint id = oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].id;
                    double maxx = oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].maxx;
                    double maxy = oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].maxy;
                    double minx = oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].minx;
                    double miny = oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].miny;
                    bool sleeperPresent = oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].SleeperPresent;

                    PlayerPerimeter pp = new PlayerPerimeter();
                    pp.name = pKeyArray[p];
                    pp.finished = finished;
                    pp.id = id;
                    pp.minX = minx;
                    pp.minY = miny;
                    pp.maxX = maxx;
                    pp.maxY = maxy;
                    pp.sleeperPresent = sleeperPresent;

                    Vector3 vec;
                    for (int c = 0; c < count; c++)
                    {
                        vec = new Vector3();
                        vec.x = (float)oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].coords[c.ToString()]["0"];
                        vec.y = (float)oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].coords[c.ToString()]["2"];
                        vec.z = (float)oldAlertsObj.alerts[keyArray[i]][pKeyArray[p]].coords[c.ToString()]["1"];
                        pp.coords.Add(vec);
                    }

                    if (pIndex == -1)
                    {
                        Perimeter per = new Perimeter();
                        per.steamid = Convert.ToUInt64(keyArray[i]);
                        per.playerPerimeters.Add(pp);
                        perimeters.Add(per);
                        pIndex = perimeters.Count - 1;
                        perimeterDict.Add(perimeters[pIndex].steamid, pIndex);
                    }
                    else
                    {
                        perimeters[pIndex].playerPerimeters.Add(pp);
                    }
                }
            }
        }
        void convertOldAuthObjs(OldAuthObjectDict oldAuthObj)
        {
            //Puts("Converting auths");
            var steamIDs = oldAuthObj.authorised.Keys.ToArray();

            for (int i = 0; i < steamIDs.Length; i++)
            {
                //Puts(steamIDs[i]);
                Auth a = new Auth();
                a.steamid = Convert.ToUInt64(steamIDs[i]);
                a.name = oldAuthObj.authNames[steamIDs[i]];
                a.level = Convert.ToInt32(oldAuthObj.authorised[steamIDs[i]]);
                authList.Add(a);
                authDict.Add(a.steamid, authList.Count - 1);
            }
        }
        void convertOldIgnoresObjs(OldIgnoresObjectDict oldIgnoresObj)
        {
            //Puts("Converting ignores");
            var keyArray = oldIgnoresObj.ignores.Keys.ToArray();

            for (int i = 0; i < keyArray.Length; i++)
            {
                IgnoreListEntry ie = new IgnoreListEntry();
                ie.steamid = Convert.ToUInt64(keyArray[i]);

                var pKeyArray = oldIgnoresObj.ignores[keyArray[i]].Keys.ToArray();

                for (int p = 0; p < pKeyArray.Length; p++)
                {
                    IgnoredPlayer ip = new IgnoredPlayer();
                    ip.steamid = Convert.ToUInt64(pKeyArray[p]);
                    ip.name = oldIgnoresObj.ignores[keyArray[i]][pKeyArray[p]];
                    ie.ignores.Add(ip);
                }

                ignoreList.Add(ie);
                ignoreDict.Add(ie.steamid, ignoreList.Count - 1);
            }
        }
        void convertOldMuteList(OldMuteList oldMuteList)
        {
            var steamIDs = oldMuteList.muted.Keys.ToArray();

            //Puts("Converting mute list");
            for (int i = 0; i < oldMuteList.muted.Count; i++)
            {
                muteList.Add(Convert.ToUInt64(steamIDs[i]));
            }
        }
        void convertOldValObjs(OldValidatedObjectDict oldValObj)
        {
            //Puts("Converting validations");
            var steamIDs = oldValObj.validated.Keys.ToArray();

            for (int i = 0; i < steamIDs.Length; i++)
            {
                validated.Add(Convert.ToUInt64(steamIDs[i]));
            }
        }
        int countPerimeters(ulong steamid)
        {
            int cnt = 0;

            if (perimeterDict.ContainsKey(steamid))
            {
                return perimeters[perimeterDict[steamid]].playerPerimeters.Count;
            }

            return cnt;
        }
        double currentTime()
        {
            return System.DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }
        void deletePerimeter(BasePlayer player, string[] args)
        {
            if (args.Length > 1)
            {
                int perimeterIndex = getPerimeterIndex(player.userID);

                if (perimeterIndex == -1)
                {
                    SendReply(player, "You have no perimeters to delete.  Do /rs set <name> to create one.");
                }

                if (perimeters[perimeterIndex].playerPerimeters.Count == 0)
                {
                    SendReply(player, "You have no perimeters to delete.  Do /rs set <name> to create one.");
                }
                else
                {
                    int index;

                    try
                    {
                        index = Convert.ToInt32(args[1]);
                    }
                    catch (Exception e)
                    {
                        SendReply(player, "Invalid index.  Do /rs view to get a valid index.");
                        return;
                    }

                    bool inProgress = false;

                    if (isInProgress(player.userID))
                    {
                        inProgress = true;

                        for (int i = 0; i < drawList.Count; i++)
                        {
                            if (drawList[i].basePlayer.userID == player.userID)
                            {
                                if (drawList[i].playerPerimeterIndex != index - 1)
                                {
                                    SendReply(player, "You have a perimeter in progress.  Complete or cancel it first.");
                                    return;
                                }
                            }
                        }
                    }

                    if (index > perimeters[perimeterIndex].playerPerimeters.Count || index < 0)
                    {
                        SendReply(player, "Invalid index.  Do /rs view to get a valid index.");
                        return;
                    }

                    SendReply(player, "Removed " + perimeters[perimeterIndex].playerPerimeters[index - 1].name + " from your perimeter list.");

                    if (inProgress) { stopDrawing(player); }
                    perimeters[perimeterIndex].playerPerimeters.RemoveAt(index - 1);
                }
            }
            else
            {
                SendReply(player, "Enter a perimeter index to delete.");
            }
        }
        void delUser(BasePlayer player, string[] args)
        {
            int argIndexDelta = 0;

            if (player != null)
            {
                argIndexDelta = 1;
            }

            int index = -1;

            if (args.Length > argIndexDelta)
            {
                try
                {
                    index = Convert.ToInt32(args[argIndexDelta]);
                }
                catch (Exception e)
                {
                    if (player != null)
                    {
                        SendReply(player, "Invalid index entered.  Do /rs auths for a list of authorised users, and their indices.");
                    }
                    else
                    {
                        Puts("Invalid index entered.  Do rs.auths for a list of authorised users, and their indices.");
                    }
                    return;
                }
            }

            if (index < 1 || index > authList.Count)
            {
                if (player != null)
                {
                    SendReply(player, "Invalid index entered.  Do /rs auths for a list of authorised users, and their indices.");
                }
                else
                {
                    Puts("Invalid index entered.  Do rs.auths for a list of authorised users, and their indices.");
                }
                return;
            }

            string name = authList[index - 1].name;
            ulong steamid = authList[index - 1].steamid;
            int level = authList[index - 1].level;

            string levelText = "";

            if (level == 0)
            {
                levelText = "Basic User ";
            }

            if (level == 1)
            {
                levelText = "Privileged User ";
            }

            if (level == 2)
            {
                levelText = "Raid Alert Admin ";
            }

            if (player != null)
            {
                if (steamid == player.userID)
                {
                    SendReply(player, "You cannot remove yourself.");
                    return;
                }
            }

            authList.RemoveAt(index - 1);
            hashAuths();

            for (int i = 0; i < allBasePlayer.Count; i++)
            {
                if (allBasePlayer[i].userID == steamid)
                {
                    SendReply(allBasePlayer[i], "You have been removed from the authorised users list.");
                }
            }

            if (player != null)
            {
                SendReply(player, levelText + name + " " + steamid.ToString() + " has been removed from the authorised users list.");
            }
            else
            {
                Puts(levelText + name + " " + steamid.ToString() + " has been removed from the authorised users list.");
            }
        }
        Point doFacesJoinTogether(List<Point> perimeterPoints, int facesIndex2, List<Face> faces)
        {
            double delta = 0.5;
            Point nextPnt = new Point();
            int cnt = perimeterPoints.Count - 1;

            if (Math.Abs(perimeterPoints[cnt].x - faces[facesIndex2].sx) < delta)
            {
                if (Math.Abs(perimeterPoints[cnt].y - faces[facesIndex2].sy) < delta)
                {
                    nextPnt.x = faces[facesIndex2].ex;
                    nextPnt.y = faces[facesIndex2].ey;
                    return nextPnt;
                }
            }

            if (Math.Abs(perimeterPoints[cnt].x - faces[facesIndex2].ex) < delta)
            {
                if (Math.Abs(perimeterPoints[cnt].y - faces[facesIndex2].ey) < delta)
                {
                    nextPnt.x = faces[facesIndex2].sx;
                    nextPnt.y = faces[facesIndex2].sy;
                    return nextPnt;
                }
            }

            //if (Math.Abs(faces[facesIndex].ex - faces[facesIndex2].sx) < 0.1)
            //{
            //    if (Math.Abs(faces[facesIndex].ey - faces[facesIndex2].sy) < 0.1)
            //    {
            //        nextPnt.x = faces[facesIndex2].ex;
            //        nextPnt.y = faces[facesIndex2].ey;
            //        return nextPnt;
            //    }
            //}

            //if (Math.Abs(faces[facesIndex].ex - faces[facesIndex2].ex) < 0.1)
            //{
            //    if (Math.Abs(faces[facesIndex].ey - faces[facesIndex2].ey) < 0.1)
            //    {
            //        nextPnt.x = faces[facesIndex2].sx;
            //        nextPnt.y = faces[facesIndex2].sy;
            //        return nextPnt;
            //    }
            //}

            nextPnt.x = -65535;
            nextPnt.y = -65535;

            return nextPnt;
        }
        bool doFacesOverlap(int facesIndex, int facesIndex2, List<Face> faces)
        {
            double pntX, pntY, pntX2, pntY2;
            double pnt2X, pnt2Y, pnt2X2, pnt2Y2;

            pntX = faces[facesIndex].sx;
            pntY = faces[facesIndex].sy;
            pntX2 = faces[facesIndex].ex;
            pntY2 = faces[facesIndex].ey;

            pnt2X = faces[facesIndex2].sx;
            pnt2Y = faces[facesIndex2].sy;
            pnt2X2 = faces[facesIndex2].ex;
            pnt2Y2 = faces[facesIndex2].ey;

            //Puts(pntX.ToString() + "," + pntY.ToString()+ "," + pntX2.ToString() + "," + pntY2.ToString() + "," + pnt2X.ToString() + "," + pnt2Y.ToString() + "," + pnt2X2.ToString() + "," + pnt2Y2.ToString());

            if (Math.Abs(pntX - pnt2X) < 0.1)
            {
                if (Math.Abs(pntY - pnt2Y) < 0.1)
                {
                    if (Math.Abs(pntX2 - pnt2X2) < 0.1)
                    {
                        if (Math.Abs(pntY2 - pnt2Y2) < 0.1)
                        {
                            return true;
                        }
                    }
                }
            }

            if (Math.Abs(pntX - pnt2X2) < 0.1)
            {
                if (Math.Abs(pntY - pnt2Y2) < 0.1)
                {
                    if (Math.Abs(pntX2 - pnt2X) < 0.1)
                    {
                        if (Math.Abs(pntY2 - pnt2Y) < 0.1)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        void drawFaces(BasePlayer player, List<Face> faces, double height)
        {
            //Puts("Drawing " + faces.Count.ToString() + " faces!");
            int persistSeconds = 10;
            Vector3 lastPos;
            Vector3 thisPos;

            height += 1;
            int triCount = 1;

            float delta = 0;

            for (int f = 0; f < faces.Count; f++)
            {
                delta += .025f;
                lastPos = new Vector3();
                thisPos = new Vector3();

                lastPos.x = (float)faces[f].sx;
                lastPos.z = (float)faces[f].sy;
                thisPos.x = (float)faces[f].ex;
                thisPos.z = (float)faces[f].ey;

                UnityEngine.Color col;

                lastPos.y = (float)height + delta;
                thisPos.y = (float)height + delta;
                col = UnityEngine.Color.red;

                player.SendConsoleCommand("ddraw.arrow", persistSeconds, col, lastPos, thisPos, 0.25);
            }
        }
        void drawPerimeterPoints(BasePlayer player, List<Point> perimeterPoints, double height)
        {
            //Puts("Drawing perimeter points!");
            int persistSeconds = 10;
            Vector3 lastPos;
            Vector3 thisPos;

            height += perimeterHeightDelta;

            for (int p = 0; p < perimeterPoints.Count - 1; p++)
            {
                lastPos = new Vector3();
                thisPos = new Vector3();

                lastPos.x = (float)perimeterPoints[p].x;
                lastPos.y = (float)height;
                lastPos.z = (float)perimeterPoints[p].y;

                thisPos.x = (float)perimeterPoints[p + 1].x;
                thisPos.y = (float)height;
                thisPos.z = (float)perimeterPoints[p + 1].y;

                player.SendConsoleCommand("ddraw.arrow", persistSeconds, UnityEngine.Color.red, lastPos, thisPos, 0.25);
            }

            if (perimeterPoints.Count > 2)
            {
                lastPos = new Vector3();
                thisPos = new Vector3();

                lastPos.x = (float)perimeterPoints[perimeterPoints.Count - 1].x;
                lastPos.y = (float)height;
                lastPos.z = (float)perimeterPoints[perimeterPoints.Count - 1].y;

                thisPos.x = (float)perimeterPoints[0].x;
                thisPos.y = (float)height;
                thisPos.z = (float)perimeterPoints[0].y;

                player.SendConsoleCommand("ddraw.arrow", persistSeconds, UnityEngine.Color.green, lastPos, thisPos, 0.25);
            }
        }
        void FindAllFrom(BasePlayer player, string perimeterName)
        {
            if (getPerimeterIndex(player.userID, perimeterName).perimeterIndex != -1)
            {
                SendReply(player, "You already have a perimeter called " + perimeterName + ". Do /rs delete <index> to remove it, or choose another name.");
                return;
            }

            int pCount = countPerimeters(player.userID);

            if (pCount >= maxPerimetersPerPlayer && getAuthLevel(player.userID) < 1)
            {
                SendReply(player, "You already have the maximum of " + maxPerimetersPerPlayer.ToString() + " perimeters on this server.  Do /rs delete <index>.");
                return;
            }

            if (useCupboards)
            {
                if (!hasTotalAccess(player) && getAuthLevel(player.userID) < 1)
                {
                    SendReply(player, "You must be authorised on a nearby cupboard to create a perimeter.");
                    return;
                }
            }

            Point nextPnt;
            List<Point> perimeterPoints = new List<Point>();
            List<Face> faces = new List<Face>();
            List<Vector3> positions = new List<Vector3>();
            List<Quaternion> rotations = new List<Quaternion>();
            List<string> foundationNames = new List<string>();
            List<UnityEngine.Collider> wasProcessed = new List<UnityEngine.Collider>();

            positions.Add(player.transform.position);

            int entryCount = 0;
            int currentPos = 0;

            while (true)
            {
                //need to add height checks and perimeter size checks here
                currentPos++;
                if (currentPos > positions.Count)
                    break;
                var objects = UnityEngine.Physics.OverlapSphere(positions[currentPos - 1], 3f);

                foreach (var obj in objects)
                {
                    if (!(wasProcessed.Contains(obj)))
                    {
                        wasProcessed.Add(obj);
                        if (obj.GetComponentInParent<BuildingBlock>() != null)
                        {
                            BuildingBlock fBuildingBlock = obj.GetComponentInParent<BuildingBlock>();
                            //Puts(fBuildingBlock.blockDefinition.info.name.english);

                            string name = fBuildingBlock.blockDefinition.info.name.english;

                            if (name == "Floor") { name = "Foundation"; }
                            if (name == "Floor Triangle") { name = "Triangle Foundation"; }

                            if (name == "Foundation" || name == "Triangle Foundation")
                            {
                                if (Math.Abs(positions[0].y - fBuildingBlock.transform.position.y) < 0.2)
                                {
                                    positions.Add(fBuildingBlock.transform.position);
                                    rotations.Add(fBuildingBlock.transform.rotation);
                                    foundationNames.Add(name);
                                }
                            }
                        }
                    }
                }
            }

            //remove our startpos as it's not a foundation
            if (positions.Count > 1)
            {
                positions.RemoveAt(0);
            }
            else
            {
                SendReply(player, "No foundations within range. Try setting a perimeter manually with /rs start <name>.");
                return;
            }

            //Puts("Faces construction starting.");
            //we now have our lists of foundations info
            double height = positions[0].y;

            for (int i = 0; i < positions.Count; i++)
            {
                Point f = new Point();
                f.x = positions[i].x;
                f.y = positions[i].z;

                Quat q = new Quat();
                q.w = rotations[i].w;
                q.x = rotations[i].x;
                q.y = rotations[i].y;
                q.z = rotations[i].z;

                double rectWidth = 3f;
                Euler e = GetEulerAngles(q);
                double angle = -e.z;

                double minX = 65535;
                double minY = 65535;
                double maxX = -65535;
                double maxY = -65535;

                if (foundationNames[i].Equals("Foundation"))
                {
                    Rect r = new Rect();
                    r.points[0].x = f.x;
                    r.points[0].y = f.y;

                    //1
                    r.points[1].x = Math.Cos(angle) * rectWidth;
                    r.points[1].x += r.points[0].x;
                    r.points[1].y = Math.Sin(angle) * rectWidth;
                    r.points[1].y += r.points[0].y;

                    Point vec = new Point();
                    vec.x = r.points[1].x - r.points[0].x;
                    vec.y = r.points[1].y - r.points[0].y;

                    double tmpX;
                    tmpX = vec.x;
                    vec.x = -vec.y;
                    vec.y = tmpX;

                    //2
                    r.points[2].x = r.points[1].x + vec.x;
                    r.points[2].y = r.points[1].y + vec.y;

                    tmpX = vec.x;
                    vec.x = -vec.y;
                    vec.y = tmpX;

                    //3
                    r.points[3].x = r.points[2].x + vec.x;
                    r.points[3].y = r.points[2].y + vec.y;

                    for (int a = 0; a < 4; a++)
                    {
                        if (r.points[a].x < minX) { minX = r.points[a].x; }
                        if (r.points[a].y < minY) { minY = r.points[a].y; }
                        if (r.points[a].x > maxX) { maxX = r.points[a].x; }
                        if (r.points[a].y > maxY) { maxY = r.points[a].y; }
                    }

                    double cx = minX + ((maxX - minX) / 2);
                    double cy = minY + ((maxY - minY) / 2);

                    double dx = cx - f.x;
                    double dy = cy - f.y;

                    for (int a = 0; a < 4; a++)
                    {
                        r.points[a].x -= dx;
                        r.points[a].y -= dy;
                    }

                    for (int a = 0; a < 4; a++)
                    {
                        Face fce = new Face();
                        fce.sx = r.points[a].x;
                        fce.sy = r.points[a].y;

                        if (a != 3)
                        {
                            fce.ex = r.points[a + 1].x;
                            fce.ey = r.points[a + 1].y;
                        }
                        else
                        {
                            fce.ex = r.points[0].x;
                            fce.ey = r.points[0].y;
                        }

                        fce.name = "Foundation";
                        faces.Add(fce);
                    }
                }
                else
                {
                    Rect t = new Rect();
                    t.points[0].x = f.x;
                    t.points[0].y = f.y;

                    //1
                    t.points[1].x = Math.Cos(angle) * rectWidth;
                    t.points[1].x += t.points[0].x;
                    t.points[1].y = Math.Sin(angle) * rectWidth;
                    t.points[1].y += t.points[0].y;

                    //2
                    double deg60rad = 0.33333333 * Math.PI;
                    angle += deg60rad;

                    t.points[2].x = Math.Cos(angle) * rectWidth;
                    t.points[2].x += t.points[0].x;
                    t.points[2].y = Math.Sin(angle) * rectWidth;
                    t.points[2].y += t.points[0].y;

                    for (int a = 0; a < 2; a++)
                    {
                        if (t.points[a].x < minX) { minX = t.points[a].x; }
                        if (t.points[a].y < minY) { minY = t.points[a].y; }
                        if (t.points[a].x > maxX) { maxX = t.points[a].x; }
                        if (t.points[a].y > maxY) { maxY = t.points[a].y; }
                    }

                    double cx = minX + ((maxX - minX) / 2);
                    double cy = minY + ((maxY - minY) / 2);

                    double dx = cx - f.x;
                    double dy = cy - f.y;

                    for (int a = 0; a < 3; a++)
                    {
                        t.points[a].x -= dx;
                        t.points[a].y -= dy;
                    }

                    for (int a = 0; a < 3; a++)
                    {
                        Face fce = new Face();
                        fce.sx = t.points[a].x;
                        fce.sy = t.points[a].y;

                        if (a != 2)
                        {
                            fce.ex = t.points[a + 1].x;
                            fce.ey = t.points[a + 1].y;
                        }
                        else
                        {
                            fce.ex = t.points[0].x;
                            fce.ey = t.points[0].y;
                        }

                        fce.name = "Triangle Foundation";
                        faces.Add(fce);
                        //Puts("Added triangle face.");
                    }
                }
            }

            //Puts("Constructed " + faces.Count.ToString() + " faces.");
            //Puts("Starting removal of overlapping faces - " + faces.Count.ToString());

            //all foundations added and faces created
            //remove overlapping faces
            int facesIndex = 0;
            int facesIndex2 = 0;

            while (facesIndex < faces.Count)
            {
                facesIndex2 = facesIndex + 1;

                while (facesIndex2 < faces.Count)
                {
                    if (doFacesOverlap(facesIndex, facesIndex2, faces))
                    {
                        faces.RemoveAt(facesIndex2);
                        faces.RemoveAt(facesIndex);
                        facesIndex--;
                        break;
                    }

                    facesIndex2++;
                }

                facesIndex++;
            }

            //Puts("Finished removing overlapping faces." + faces.Count.ToString());

            int startIndex = -1;
            bool doPerimeter = true;

            if (doPerimeter)
            {
                startIndex = 0;
                Point pt = new Point();
                pt.x = faces[startIndex].sx;
                pt.y = faces[startIndex].sy;
                perimeterPoints.Add(pt);

                pt = new Point();
                pt.x = faces[startIndex].ex;
                pt.y = faces[startIndex].ey;
                perimeterPoints.Add(pt);

                faces.RemoveAt(startIndex);

                int loopCnt = 0;
                bool foundNext = false;
                int maxLoops = faces.Count;

                while (true)
                {
                    foundNext = false;
                    if (loopCnt > maxLoops)
                    {
                        SendReply(player, faces.Count.ToString() + " - Error auto creating perimeter.  Try creating one manually using /rs start <name>.");
                        return;
                    }
                    loopCnt++;

                    for (int i = 0; i < faces.Count; i++)
                    {
                        nextPnt = doFacesJoinTogether(perimeterPoints, i, faces);

                        if (nextPnt.x != -65535)
                        {
                            foundNext = true;
                            perimeterPoints.Add(nextPnt);
                            faces.RemoveAt(i);
                            break;
                        }
                    }

                    if (!foundNext) { break; }
                }

                perimeterPoints = removePointlessPoints(perimeterPoints);

                if (perimeterPoints.Count > maxPerimeterPoints && getAuthLevel(player.userID) < 1)
                {
                    SendReply(player, "Perimeter exceeds the maximum number of points. (" + maxPerimeterPoints.ToString() + ").");
                    return;
                }

                int perimeterIndex = getPerimeterIndex(player.userID);

                if (perimeterIndex == -1)
                {
                    Perimeter per = new Perimeter();
                    per.steamid = player.userID;

                    perimeters.Add(per);
                    perimeterDict.Add(player.userID, perimeters.Count - 1);
                    perimeterIndex = perimeters.Count - 1;
                }

                PlayerPerimeter pPer = new PlayerPerimeter();
                pPer.name = perimeterName;
                pPer.id = (uint)rnd.Next(1000, 999999999);
                pPer.finished = true;

                //exclude the last perimeter point as it'll be equal to our first
                for (int i = 0; i < perimeterPoints.Count - 1; i++)
                {
                    Vector3 v = new Vector3();
                    v.x = (float)perimeterPoints[i].x;
                    v.y = (float)height;
                    v.z = (float)perimeterPoints[i].y;

                    pPer.coords.Add(v);
                }

                perimeters[perimeterIndex].playerPerimeters.Add(pPer);
                PerimeterPosition pPos = new PerimeterPosition();
                pPos.perimeterIndex = perimeterIndex;
                pPos.playerPerimeterIndex = perimeters[perimeterIndex].playerPerimeters.Count - 1;

                if (!findTolerances(pPos) && getAuthLevel(player.userID) < 1)
                {
                    SendReply(player, "Perimeter exceeds maximum allowed size of " + maxPerimeterDelta.ToString() + " x " + maxPerimeterDelta.ToString() + ".");
                    perimeters[perimeterIndex].playerPerimeters.RemoveAt(perimeters[perimeterIndex].playerPerimeters.Count - 1);
                    return;
                }
                else
                {
                    plotPerimeter(player, perimeterIndex, perimeters[perimeterIndex].playerPerimeters.Count - 1);
                    sortPlayerPerimeters(perimeterIndex);
                    addValidationRequest(player.userID, false);
                    sendTestAlert(player);
                    ignorePlayer(player.userID, player.userID, player.displayName);
                    SendReply(player, "Perimeter entry successful.");
                }
                //drawPerimeterPoints(player, perimeterPoints, height);
            }
            else
            {
                drawFaces(player, faces, height);
            }
        }
        bool findTolerances(PerimeterPosition pPos)
        {
            //max and min and report if over max
            double minX = 65535;
            double maxX = -65535;
            double minY = 65535;
            double maxY = -65535;
            double x = 0;
            double y = 0;

            for (int i = 0; i < perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords.Count; i++)
            {
                x = perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords[i].x;
                y = perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords[i].z;

                if (x < minX) { minX = x; }
                if (x > maxX) { maxX = x; }
                if (y < minY) { minY = y; }
                if (y > maxY) { maxY = y; }
            }

            if (maxX - minX > maxPerimeterDelta || maxY - minY > maxPerimeterDelta)
            {
                return false;
            }

            perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].minX = minX;
            perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].minY = minY;
            perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].maxX = maxX;
            perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].maxY = maxY;
            return true;
        }
        int getAuthIndex(ulong steamid)
        {
            if (authDict.ContainsKey(steamid))
            {
                return authDict[steamid];
            }

            return -1;
        }
        int getAuthLevel(ulong steamid)
        {
            if (authDict.ContainsKey(steamid))
            {
                return authList[authDict[steamid]].level;
            }

            return -1;
        }
        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
            }
            return value;
        }
        Euler GetEulerAngles(Quat q)
        {
            double yaw, pitch, roll;
            double w2 = q.w * q.w;
            double x2 = q.x * q.x;
            double y2 = q.y * q.y;
            double z2 = q.z * q.z;
            double unitLength = w2 + x2 + y2 + z2;
            double abcd = q.w * q.x + q.y * q.z;
            double eps = 0.0000001;
            //double pi = Math.PI;

            if (abcd > (0.5 - eps) * unitLength)
            {
                //yaw = 2 * Math.Atan2(q.y, q.w);
                //pitch = pi;
                roll = 0;
            }
            else
            {
                if (abcd < (-0.5 + eps) * unitLength)
                {
                    //yaw = -2 * Math.Atan2(q.y, q.w);
                    //pitch = -pi;
                    roll = 0;
                }
                else
                {
                    //double adbc = q.w * q.z - q.x * q.y;
                    double acbd = q.w * q.y - q.x * q.z;
                    //yaw = Math.Atan2(2 * adbc, 1 - 2 * (z2 + x2));
                    //pitch = Math.Asin(2 * abcd / unitLength);
                    roll = Math.Atan2(2 * acbd, 1 - 2 * (y2 + x2));
                }
            }

            Euler e = new Euler();
            e.x = 0;//yaw;
            e.y = 0;// pitch;
            e.z = roll;

            return e;
        }
        int getIgnoreListIndex(ulong steamid)
        {
            if (ignoreDict.ContainsKey(steamid))
            {
                return (ignoreDict[steamid]);
            }

            return -1;
        }
        PerimeterPosition getInProgress(ulong steamid)
        {
            PerimeterPosition pPos = new PerimeterPosition();
            int i = getPerimeterIndex(steamid);

            if (i != -1)
            {
                for (int p = 0; p < perimeters[i].playerPerimeters.Count; p++)
                {
                    if (!perimeters[i].playerPerimeters[p].finished)
                    {
                        pPos.perimeterIndex = i;
                        pPos.playerPerimeterIndex = p;
                        return pPos;
                    }
                }
            }

            pPos.perimeterIndex = -1;
            pPos.playerPerimeterIndex = -1;
            return pPos;
        }
        int getPerimeterIndex(ulong steamid)
        {
            if (perimeterDict.ContainsKey(steamid))
            {
                return perimeterDict[steamid];
            }

            return -1;
        }
        PerimeterPosition getPerimeterIndex(ulong steamid, string name)
        {
            PerimeterPosition pPos = new PerimeterPosition();
            int i = -1;

            if (perimeterDict.ContainsKey(steamid))
            {
                i = perimeterDict[steamid];
            }

            if (i != -1)
            {
                for (int p = 0; p < perimeters[i].playerPerimeters.Count; p++)
                {
                    if (perimeters[i].playerPerimeters[p].name == name)
                    {
                        pPos.perimeterIndex = i;
                        pPos.playerPerimeterIndex = p;
                        return pPos;
                    }
                }
            }

            pPos.perimeterIndex = -1;
            pPos.playerPerimeterIndex = -1;
            return pPos;
        }
        int getPlayerIndex(ulong steamid)
        {
            for (int i = 0; i < allBasePlayer.Count; i++)
            {
                if (allBasePlayer[i].userID == steamid)
                {
                    return i;
                }
            }

            return -1;
        }
        void hashAuths()
        {
            authDict.Clear();

            for (int i = 0; i < authList.Count; i++)
            {
                authDict.Add(authList[i].steamid, i);
            }
        }
        void hashIgnores()
        {
            ignoreDict.Clear();

            for (int i = 0; i < ignoreList.Count; i++)
            {
                ignoreDict.Add(ignoreList[i].steamid, i);
            }
        }
        void hashPerimeters()
        {
            perimeterDict.Clear();

            for (int i = 0; i < perimeters.Count; i++)
            {
                perimeterDict.Add(perimeters[i].steamid, i);
            }
        }
        bool hasTotalAccess(BasePlayer player)
        {
            List<BuildingPrivlidge> playerpriv = buildingPrivlidges.GetValue(player) as List<BuildingPrivlidge>;
            if (playerpriv.Count == 0)
            {
                return false;
            }
            foreach (BuildingPrivlidge priv in playerpriv.ToArray())
            {
                List<ProtoBuf.PlayerNameID> authorized = priv.authorizedPlayers;
                bool foundplayer = false;
                foreach (ProtoBuf.PlayerNameID pni in authorized.ToArray())
                {
                    if (pni.userid == player.userID)
                        foundplayer = true;
                }
                if (!foundplayer)
                {
                    return false;
                }
            }
            return true;
        }
        void ignoreDetect(BasePlayer player)
        {
            int perimeterIndex = getPerimeterIndex(player.userID);

            if (perimeterIndex == -1)
            {
                SendReply(player, "You have no perimeters to check.  Do /rs set <name>.");
            }
            else
            {

                int ignoredCount = 0;

                for (int i = 0; i < allBasePlayer.Count; i++)
                {
                    for (int p = 0; p < perimeters[perimeterIndex].playerPerimeters.Count; p++)
                    {
                        PerimeterPosition pPos = new PerimeterPosition();
                        pPos.perimeterIndex = perimeterIndex;
                        pPos.playerPerimeterIndex = p;

                        int wn = windingPoly(allBasePlayer[i].transform.position, pPos);

                        if (wn != 0)
                        {
                            bool match = false;
                            int ignoreListIndex = getIgnoreListIndex(player.userID);

                            if (ignoreListIndex != -1)
                            {
                                bool playerMatch = false;

                                for (int pl = 0; pl < ignoreList[ignoreListIndex].ignores.Count; pl++)
                                {
                                    if (ignoreList[ignoreListIndex].ignores[pl].steamid == allBasePlayer[i].userID)
                                    {
                                        playerMatch = true;
                                        if (ignoreList[ignoreListIndex].ignores[pl].name != allBasePlayer[i].displayName)
                                        {
                                            ignoreList[ignoreListIndex].ignores[pl].name = allBasePlayer[i].displayName;
                                            SendReply(player, "Added " + allBasePlayer[i].displayName + " " + allBasePlayer[i].userID.ToString() + " to your ignore list.");
                                            ignoredCount++;
                                            break;
                                        }
                                    }
                                }

                                if (!playerMatch)
                                {
                                    IgnoredPlayer ip = new IgnoredPlayer();
                                    ip.steamid = allBasePlayer[i].userID;
                                    ip.name = allBasePlayer[i].displayName;

                                    ignoreList[ignoreListIndex].ignores.Add(ip);
                                    sortIgnores(ignoreListIndex);
                                    SendReply(player, "Added " + allBasePlayer[i].displayName + " " + allBasePlayer[i].userID.ToString() + " to your ignore list.");
                                    ignoredCount++;
                                }
                            }
                            else
                            {
                                IgnoreListEntry ie = new IgnoreListEntry();
                                ie.steamid = player.userID;

                                IgnoredPlayer ip = new IgnoredPlayer();
                                ip.steamid = allBasePlayer[i].userID;
                                ip.name = allBasePlayer[i].displayName;

                                ie.ignores.Add(ip);
                                ignoreList.Add(ie);
                                ignoreDict.Add(player.userID, ignoreList.Count - 1);
                                SendReply(player, "Added " + allBasePlayer[i].displayName + " " + allBasePlayer[i].userID.ToString() + " to your ignore list.");
                                ignoredCount++;
                            }
                        }
                    }
                }

                if (ignoredCount == 0)
                {
                    SendReply(player, "No players were found within your perimeters to add to the ignore list.");
                }
            }
        }
        void ignorePlayer(BasePlayer player, string[] args)
        {
            if (args.Length > 2)
            {
                if (args[1].Length == 17)
                {
                    if (args[1].Substring(0, 6) == "765611")
                    {
                        ulong steamid;

                        try
                        {
                            steamid = Convert.ToUInt64(args[1]);
                        }
                        catch (Exception e)
                        {
                            SendReply(player, "Please enter a valid Steam ID.");
                            return;
                        }

                        if (args[2] == "")
                        {
                            SendReply(player, "Please enter a valid player name after the Steam ID.");
                            return;
                        }

                        IgnoredPlayer plr;
                        IgnoreListEntry ie;

                        int ignoreListIndex = getIgnoreListIndex(player.userID);

                        if (ignoreListIndex != -1)
                        {
                            for (int p = 0; p < ignoreList[ignoreListIndex].ignores.Count; p++)
                            {
                                if (ignoreList[ignoreListIndex].ignores[p].steamid == steamid)
                                {
                                    if (ignoreList[ignoreListIndex].ignores[p].name != args[2])
                                    {
                                        ignoreList[ignoreListIndex].ignores[p].name = args[2];
                                        SendReply(player, "Updated ignore list entry.");
                                    }
                                    else
                                    {
                                        SendReply(player, "Player is already ignored.");
                                    }
                                    return;
                                }
                            }
                        }

                        if (ignoreListIndex == -1)
                        {
                            ie = new IgnoreListEntry();
                            ie.steamid = player.userID;

                            plr = new IgnoredPlayer();
                            plr.steamid = steamid;
                            plr.name = args[2];

                            ie.ignores.Add(plr);
                            ignoreList.Add(ie);
                            ignoreDict.Add(player.userID, ignoreList.Count - 1);

                            SendReply(player, "Added " + args[2] + " - " + args[1] + " to your ignore list.");
                            return;
                        }
                        else
                        {
                            plr = new IgnoredPlayer();
                            plr.steamid = steamid;
                            plr.name = args[2];

                            ignoreList[ignoreListIndex].ignores.Add(plr);
                            sortIgnores(ignoreListIndex);
                            SendReply(player, "Added " + args[2] + " - " + args[1] + " to your ignore list.");
                            return;
                        }
                    }
                }

                SendReply(player, "Please enter a SteamID and a name to add to your ignore list on this server.");
            }
            else
            {
                if (args.Length == 2)
                {
                    if (args[1].Length == 17)
                    {
                        if (args[1].Substring(0, 6) == "765611")
                        {
                            ulong steamid;

                            try
                            {
                                steamid = Convert.ToUInt64(args[1]);
                            }
                            catch (Exception e)
                            {
                                SendReply(player, "Please enter a valid Steam ID.");
                                return;
                            }

                            for (int a = 0; a < allBasePlayer.Count; a++)
                            {
                                if (allBasePlayer[a].userID == steamid)
                                {
                                    IgnoredPlayer plr = new IgnoredPlayer();
                                    int ignoreListIndex = getIgnoreListIndex(player.userID);

                                    plr.steamid = steamid;
                                    plr.name = allBasePlayer[a].displayName;

                                    if (ignoreListIndex == -1)
                                    {
                                        IgnoreListEntry ie = new IgnoreListEntry();
                                        ie.steamid = player.userID;
                                        ie.ignores.Add(plr);
                                        ignoreList.Add(ie);
                                        ignoreDict.Add(player.userID, ignoreList.Count - 1);
                                        SendReply(player, "Added " + plr.name + " - " + plr.steamid.ToString() + " to your ignore list.");
                                        return;
                                    }
                                    else
                                    {
                                        for (int p = 0; p < ignoreList[ignoreListIndex].ignores.Count; p++)
                                        {
                                            if (ignoreList[ignoreListIndex].ignores[p].steamid == steamid)
                                            {
                                                if (ignoreList[ignoreListIndex].ignores[p].name != plr.name)
                                                {
                                                    ignoreList[ignoreListIndex].ignores[p].name = plr.name;
                                                    SendReply(player, "Updated ignore list entry.");
                                                }
                                                else
                                                {
                                                    SendReply(player, "Player is already ignored.");
                                                }
                                                return;
                                            }
                                        }

                                        ignoreList[ignoreListIndex].ignores.Add(plr);
                                        SendReply(player, "Added " + plr.name + " - " + plr.steamid.ToString() + " to your ignore list.");
                                        return;
                                    }
                                }
                            }
                            SendReply(player, "No players with that SteamID are currently online.");
                        }
                    }
                    else
                    {
                        for (int a = 0; a < allBasePlayer.Count; a++)
                        {
                            if (allBasePlayer[a].displayName.Length >= args[1].Length)
                            {
                                if (allBasePlayer[a].displayName.Substring(0, args[1].Length) == args[1])
                                {
                                    int r = checkDupes(args[1]);

                                    if (r >= 0)
                                    {
                                        ulong steamid = allBasePlayer[a].userID;
                                        int ignoreListIndex = getIgnoreListIndex(player.userID);

                                        IgnoredPlayer plr = new IgnoredPlayer();
                                        plr.steamid = steamid;
                                        plr.name = allBasePlayer[a].displayName;

                                        if (ignoreListIndex != -1)
                                        {
                                            for (int p = 0; p < ignoreList[ignoreListIndex].ignores.Count; p++)
                                            {
                                                if (ignoreList[ignoreListIndex].ignores[p].steamid == steamid)
                                                {
                                                    if (ignoreList[ignoreListIndex].ignores[p].name != plr.name)
                                                    {
                                                        ignoreList[ignoreListIndex].ignores[p].name = plr.name;
                                                        SendReply(player, "Updated ignore list entry.");
                                                    }
                                                    else
                                                    {
                                                        SendReply(player, "Player is already ignored.");
                                                    }
                                                    return;
                                                }
                                            }

                                            ignoreList[ignoreListIndex].ignores.Add(plr);
                                            SendReply(player, "Added " + plr.name + " - " + steamid.ToString() + " to your ignore list.");
                                            return;
                                        }
                                        else
                                        {
                                            IgnoreListEntry ie = new IgnoreListEntry();
                                            ie.steamid = player.userID;
                                            ie.ignores.Add(plr);
                                            ignoreList.Add(ie);
                                            ignoreDict.Add(player.userID, ignoreList.Count - 1);
                                            SendReply(player, "Added " + plr.name + " - " + steamid.ToString() + " to your ignore list.");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        if (r == -2)
                                        {
                                            SendReply(player, "There is more than 1 player with that name.");
                                        }
                                        else
                                        {
                                            SendReply(player, "There are no players with that name.");
                                        }
                                        return;
                                    }
                                }
                            }
                        }
                        SendReply(player, "Please enter a the name or the SteamID of an online player.");
                    }
                }
                else
                {
                    SendReply(player, "Please enter a SteamID and a name to add to your ignore list on this server.");
                }
            }
        }
        bool ignorePlayer(ulong baseSteamID, ulong otherPlayerSteamID, string otherPlayerName)
        {
            bool baseMatch = false;

            int i = getIgnoreListIndex(baseSteamID);

            if (i != -1)
            {
                baseMatch = true;

                for (int a = 0; a < ignoreList[i].ignores.Count; a++)
                {
                    if (ignoreList[i].ignores[a].steamid == otherPlayerSteamID)
                    {
                        return false;
                    }
                }

                IgnoredPlayer p = new IgnoredPlayer();
                p.steamid = otherPlayerSteamID;
                p.name = otherPlayerName;
                ignoreList[i].ignores.Add(p);
                sortIgnores(i);
                return true;
            }

            if (!baseMatch)
            {
                IgnoreListEntry il = new IgnoreListEntry();
                il.steamid = baseSteamID;

                IgnoredPlayer p = new IgnoredPlayer();
                p.steamid = otherPlayerSteamID;
                p.name = otherPlayerName;

                il.ignores.Add(p);
                ignoreList.Add(il);
                ignoreDict.Add(baseSteamID, ignoreList.Count - 1);
                return true;
            }

            return false;
        }
        bool isInProgress(ulong steamid)
        {
            int i = getPerimeterIndex(steamid);

            if (i != -1)
            {
                for (int p = 0; p < perimeters[i].playerPerimeters.Count; p++)
                {
                    if (!perimeters[i].playerPerimeters[p].finished)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        bool isPerimeterNameInUse(ulong steamid, string name)
        {
            int i = getPerimeterIndex(steamid);

            if (i != -1)
            {
                for (int p = 0; p < perimeters[i].playerPerimeters.Count; p++)
                {
                    if (perimeters[i].playerPerimeters[p].name == name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        bool isPlayerIgnored(ulong steamid, ulong tpsteamid)
        {
            int index = getIgnoreListIndex(steamid);

            if (index != -1)
            {
                for (int i = 0; i < ignoreList[index].ignores.Count; i++)
                {
                    if (ignoreList[index].ignores[i].steamid == tpsteamid)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        bool isServerModerator(BasePlayer player)
        {
            if (player.net.connection != null)
            {
                if (player.net.connection.authLevel >= 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        bool isValidated(ulong steamid)
        {
            if (validated.Contains(steamid))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        double isLeft(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            return ((p1.x - p0.x) * (p2.z - p0.z) - (p2.x - p0.x) * (p1.z - p0.z));
        }
        void LoadDataFiles()
        {
            if (!updatedOldData)
            {
                try
                {
                    OldValidatedObjectDict oldValObj = Interface.Oxide.DataFileSystem.ReadObject<OldValidatedObjectDict>("RaidAlert");
                    convertOldValObjs(oldValObj);
                }
                catch (Exception e)
                {
                    //Puts(e.ToString());
                    //Puts("No validations to migrate");
                }

                try
                {
                    OldAuthObjectDict oldAuthObj = Interface.Oxide.DataFileSystem.ReadObject<OldAuthObjectDict>("RaidAlert");
                    convertOldAuthObjs(oldAuthObj);
                }
                catch (Exception e)
                {
                    //Puts("No auths to migrate");
                }

                try
                {
                    OldMuteList oldMuteList = Interface.Oxide.DataFileSystem.ReadObject<OldMuteList>("RaidAlert");
                    convertOldMuteList(oldMuteList);
                }
                catch (Exception e)
                {
                    //Puts("No muted list to migrate");
                }

                try
                {
                    OldIgnoresObjectDict oldIgnoresObj = Interface.Oxide.DataFileSystem.ReadObject<OldIgnoresObjectDict>("RaidAlert");
                    convertOldIgnoresObjs(oldIgnoresObj);
                }
                catch (Exception e)
                {
                    //Puts("No ignores to migrate");
                    //Puts(e.ToString());
                }

                try
                {
                    OldAlertsObjectDict oldAlertsObj = Interface.Oxide.DataFileSystem.ReadObject<OldAlertsObjectDict>("RaidAlert");
                    convertOldAlertsObj(oldAlertsObj);
                }
                catch (Exception e)
                {
                    //Puts("No perimeters to migrate");
                    //Puts(e.ToString());
                }

                updatedOldData = true;
                saveDataFiles();
                saveConfiguration();
                return;
            }

            //try
            //{
            //oldSaveObj = Interface.Oxide.DataFileSystem.ReadObject<OldSaveObject>("RaidAlert");
            //Puts("Converting old save.");
            //convertOldSave(oldSaveObj);
            //return;
            //}
            //catch (Exception e)
            //{
            //    Puts();
            //    //old save doesn't exist
            //}

            //Load our perimeter/user data here
            SaveObject saveObj = null;

            try
            {
                saveObj = Interface.Oxide.DataFileSystem.ReadObject<SaveObject>("RaidAlert v1");
            }
            catch (Exception e)
            {
                //data file missing, so skip
                return;
            }

            if (saveObj.validated == null) { return; }
            validated = saveObj.validated;
            muteList = saveObj.muteList;
            authList = saveObj.authList;
            ignoreList = saveObj.ignoreList;

            List<SavePerimeterEntry> savePerimeters = Interface.Oxide.DataFileSystem.ReadObject<List<SavePerimeterEntry>>("RaidAlert - Perimeters - delete on server wipe");

            for (int i = 0; i < savePerimeters.Count; i++)
            {
                int baseIndex = getPerimeterIndex(savePerimeters[i].steamid);

                if (baseIndex == -1)
                {
                    //create new base entry
                    Perimeter p = new Perimeter();
                    p.steamid = savePerimeters[i].steamid;
                    perimeters.Add(p);
                    perimeterDict.Add(p.steamid, perimeters.Count - 1);
                    baseIndex = perimeters.Count - 1;
                }

                //add new player perimeter to base entry
                PlayerPerimeter pl = new PlayerPerimeter();
                pl.name = savePerimeters[i].name;
                pl.finished = savePerimeters[i].finished;
                pl.id = savePerimeters[i].id;
                pl.minX = savePerimeters[i].minX;
                pl.minY = savePerimeters[i].minY;
                pl.maxX = savePerimeters[i].maxX;
                pl.maxY = savePerimeters[i].maxY;
                pl.sleeperPresent = savePerimeters[i].sleeperPresent;

                for (int i2 = 0; i2 < savePerimeters[i].coordsX.Count; i2++)
                {
                    Vector3 v = new Vector3();
                    v.x = (float)savePerimeters[i].coordsX[i2];
                    v.y = (float)savePerimeters[i].coordsY[i2];
                    v.z = (float)savePerimeters[i].coordsZ[i2];
                    pl.coords.Add(v);
                }
                perimeters[baseIndex].playerPerimeters.Add(pl);
            }

            hashAuths();
            hashIgnores();
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Rusty Sheriff Raid Alert: Creating new config file");
            Config.Clear();
            saveConfiguration();
            LoadVariables();
        }
        void Loaded()
        {
            epoch = new System.DateTime(1970, 1, 1);
            LoadVariables();
            LoadDataFiles();

            for (int i = 0; i < 999; i++)
            {
                rnd.Next(0, 999999999);
            }

            buildingPrivlidges = typeof(BasePlayer).GetField("buildingPrivlidges", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }
        void loadOldConfig()
        {
            try
            {
                alertsVisibleInGame = Convert.ToBoolean(Config["alertsVisiblePlayers"]);
                enabledForAuthorisedUsersOnly = Convert.ToBoolean(Config["enabledForAuthorisedUsersOnly"]);
                maxPerimetersPerPlayer = (int)Config["maxAlertsPerPlayer"];
                maxPerimeterDelta = (int)Config["maxPerimeterDelta"];
                maxPerimeterPoints = (int)Config["maxPerimeterPoints"];
                syncWithRustySheriffServer = Convert.ToBoolean(Config["syncWithRustySheriffServer"]);
                timeBetweenChecks = (int)Config["timeBetweenChecks"];
                alertTriggeredOnlyWhenSleepingInPerimeter = Convert.ToBoolean(Config["alertTriggeredOnlyWhenSleepingInPerimeter"]);
                sendAnon = Convert.ToBoolean(Config["sendAnon"]);

                Puts("Config file updated successfully.");
            }
            catch (Exception e)
            {
                Puts("You may have upgraded from a version lower than 0.9.9.  Check your setttings in-game with /rs status, to make sure your options are as expected. Thanks");
            }

            saveConfiguration();
        }
        void LoadVariables()
        {
            bool configFound = false;

            var a = Config["OPTIONS", "Show alerts in-game"];

            if (a == null)
            {
                Puts("New version of config file not found.");
            }
            else
            {
                //Puts("New config file found.");
                configFound = true;
            }

            if (!configFound)
            {
                var a2 = Convert.ToBoolean(Config["alertsVisiblePlayers"]);
                if (a2 != null)
                {
                    Puts("Old config found.  Converting...");
                    loadOldConfig();
                    return;
                }
                else
                {
                    Puts("Old config not detected.");
                }
            }

            if (!configFound)
            {
                saveConfiguration();
                return;
            }

            updatedOldData = Convert.ToBoolean(GetConfig("SETTINGS", "Converted", false));
            maxPerimetersPerPlayer = Convert.ToInt32(GetConfig("OPTIONS", "Max perimeters per player", 4));
            timeBetweenChecks = Convert.ToInt32(GetConfig("OPTIONS", "Time between updates", 10));
            maxPerimeterDelta = Convert.ToInt32(GetConfig("OPTIONS", "Max perimeter size", 75));
            maxPerimeterPoints = Convert.ToInt32(GetConfig("OPTIONS", "Max points per perimeter", 50));
            maxPerimetersPerPlayer = Convert.ToInt32(GetConfig("OPTIONS", "Max perimeters per player", 4));

            sendAnon = Convert.ToBoolean(GetConfig("OPTIONS", "Hide names and SteamIDs from alerts", false));
            automaticCheckingEnabled = Convert.ToBoolean(GetConfig("OPTIONS", "Enable automatic perimeter checking", true));
            syncWithRustySheriffServer = Convert.ToBoolean(GetConfig("OPTIONS", "Sync alerts with the Raid Alert server", true));
            alertTriggeredOnlyWhenSleepingInPerimeter = Convert.ToBoolean(GetConfig("OPTIONS", "Alerts only triggered if player sleeping in their perimeter", false));
            alertsVisibleInGame = Convert.ToBoolean(GetConfig("OPTIONS", "Show alerts in-game", true));
            useCupboards = Convert.ToBoolean(GetConfig("OPTIONS", "Use cupboard authorisation when creating perimeters", false));
            enabledForAuthorisedUsersOnly = Convert.ToBoolean(GetConfig("OPTIONS", "Enabled for authorised users only", false));
        }
        void muteIngameAlerts(BasePlayer player, bool mute)
        {
            if (muteList.Contains(player.userID))
            {
                if (mute)
                {
                    SendReply(player, "Alerts are already muted in-game.");
                }
                else
                {
                    SendReply(player, "Alerts will be displayed in-game.");
                    muteList.Remove(player.userID);
                }
            }
            else
            {
                if (mute)
                {
                    SendReply(player, "Alerts will be muted in-game.");
                    muteList.Add(player.userID);
                }
                else
                {
                    SendReply(player, "Alerts are already unmuted.");
                }
            }
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (isInProgress(player.userID))
            {
                stopDrawing(player);
            }

            for (int i = 0; i < allBasePlayer.Count; i++)
            {
                if (allBasePlayer[i].userID == player.userID)
                {
                    int perimeterIndex = getPerimeterIndex(player.userID);

                    if (perimeterIndex == -1)
                    {
                        return;
                    }

                    for (int p = 0; p < perimeters[perimeterIndex].playerPerimeters.Count; p++)
                    {
                        PerimeterPosition pPos = new PerimeterPosition();
                        pPos.perimeterIndex = perimeterIndex;
                        pPos.playerPerimeterIndex = p;
                        int wn = windingPoly(player.transform.position, pPos);

                        if (wn != 0)
                        {
                            perimeters[perimeterIndex].playerPerimeters[p].sleeperPresent = true;
                            if (alertTriggeredOnlyWhenSleepingInPerimeter)
                            {
                                Puts("Player " + allBasePlayer[i].displayName + " disconnected within his perimeter, " + perimeters[perimeterIndex].playerPerimeters[p].name);
                            }
                        }
                    }
                }
            }
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (isServerModerator(player))
            {
                setAuthLevel(player.userID, 2, player.displayName, null, true);
            }

            int perimeterIndex = getPerimeterIndex(player.userID);

            if (perimeterIndex != -1)
            {
                for (int p = 0; p < perimeters[perimeterIndex].playerPerimeters.Count; p++)
                {
                    perimeters[perimeterIndex].playerPerimeters[p].sleeperPresent = false;
                }
            }
        }
        void OnServerInitialized()
        {
        }
        void OnServerSave()
        {
            saveConfiguration();
            saveDataFiles();
        }
        void OnTick()
        {
            if (automaticCheckingEnabled)
            {
                if (currentTime() >= playerUpdateCheck)
                {
                    try
                    {
                        allBasePlayer = BasePlayer.activePlayerList;
                    }
                    catch (Exception e)
                    {

                    }
                    playerUpdateCheck = currentTime() + 30;
                }

                if (currentTime() >= nextCheck)
                {
                    int divisor = 1;

                    try
                    {
                        if (!adminsAuthed) { checkAdminsAuthed(); adminsAuthed = true; }
                        checkTrespass();
                        playerCheckIndex++;

                        if (playerCheckIndex >= allBasePlayer.Count)
                        {
                            updateRSSServer();
                            playerCheckIndex = 0;
                        }

                        if (allBasePlayer.Count > 1) { divisor = allBasePlayer.Count; }
                    }
                    catch (Exception e)
                    {
                    }

                    double delay = timeBetweenChecks / divisor;
                    nextCheck = currentTime() + delay;
                }
            }

            if (currentTime() >= drawCheck)
            {
                try
                {
                    for (int i = 0; i < drawList.Count; i++)
                    {
                        plotPerimeter(drawList[i].basePlayer, drawList[i].perimeterIndex, drawList[i].playerPerimeterIndex);
                    }
                }
                catch (Exception e)
                {
                }

                drawCheck = currentTime() + 4.9;
            }
        }
        void parseWebReply(int code, string response)
        {
            if (response.Length >= 2)
            {
                if (response.Substring(0, 2) == "OK")
                {
                    oldTrespassers = newTrespassers;
                    //oldTrespassers.Clear();

                    //for (int i = 0; i < newTrespassers.Count; i++)
                    //{
                    //    Trespasser t = new Trespasser();
                    //    t.steamid = newTrespassers[i].steamid;
                    //    t.name = newTrespassers[i].name;
                    //    t.trespasserSteamID = newTrespassers[i].trespasserSteamID;
                    //    t.alertText = newTrespassers[i].alertText;

                    //    oldTrespassers.Add(t);
                    //}

                    newTrespassers = new List<Trespasser>();

                    if (response.Length > 2)
                    {
                        respondToValRequests(response.Substring(2));
                    }
                }
                else
                {
                    newTrespassers.Clear();
                    oldTrespassers.Clear();
                }
            }
            else
            {
                newTrespassers.Clear();
                oldTrespassers.Clear();
            }
        }
        void plotPerimeter(BasePlayer player, int perimeterIndex, int playerPerimeterIndex)
        {
            int persistSeconds = 5;
            string name = perimeters[perimeterIndex].playerPerimeters[playerPerimeterIndex].name;

            if (perimeters[perimeterIndex].playerPerimeters[playerPerimeterIndex].finished) { persistSeconds = 20; }

            for (int p = 0; p < perimeters[perimeterIndex].playerPerimeters[playerPerimeterIndex].coords.Count; p++)
            {
                player.SendConsoleCommand("ddraw.text", persistSeconds, UnityEngine.Color.green, perimeters[perimeterIndex].playerPerimeters[playerPerimeterIndex].coords[p], name + " " + (p + 1).ToString());

                if (p > 0) { player.SendConsoleCommand("ddraw.arrow", persistSeconds, UnityEngine.Color.red, perimeters[perimeterIndex].playerPerimeters[playerPerimeterIndex].coords[p - 1], perimeters[perimeterIndex].playerPerimeters[playerPerimeterIndex].coords[p], 0.25); }
            }

            if (perimeters[perimeterIndex].playerPerimeters[playerPerimeterIndex].finished)
            {
                player.SendConsoleCommand("ddraw.arrow", persistSeconds, UnityEngine.Color.red, perimeters[perimeterIndex].playerPerimeters[playerPerimeterIndex].coords[perimeters[perimeterIndex].playerPerimeters[playerPerimeterIndex].coords.Count - 1], perimeters[perimeterIndex].playerPerimeters[playerPerimeterIndex].coords[0], 0.25);
            }
        }
        List<Point> removePointlessPoints(List<Point> perimeterPoints)
        {
            bool done;
            int perimeterIndex = 0;
            List<int> removalList = new List<int>();
            Point inVec = new Point();
            Point outVec = new Point();

            while (perimeterIndex < perimeterPoints.Count)
            {
                done = false;

                if (perimeterIndex == 0)
                {
                    done = true;

                    inVec.x = perimeterPoints[perimeterPoints.Count - 1].x;
                    inVec.y = perimeterPoints[perimeterPoints.Count - 1].y;

                    outVec.x = perimeterPoints[perimeterIndex + 1].x - perimeterPoints[perimeterIndex].x;
                    outVec.y = perimeterPoints[perimeterIndex + 1].y - perimeterPoints[perimeterIndex].y;
                }

                if (perimeterIndex == perimeterPoints.Count - 1 && !done)
                {
                    done = true;

                    inVec.x = perimeterPoints[perimeterIndex].x - perimeterPoints[perimeterIndex - 1].x;
                    inVec.y = perimeterPoints[perimeterIndex].y - perimeterPoints[perimeterIndex - 1].y;

                    outVec.x = perimeterPoints[0].x - perimeterPoints[perimeterIndex].x;
                    outVec.y = perimeterPoints[0].y - perimeterPoints[perimeterIndex].y;
                }

                if (!done)
                {
                    inVec.x = perimeterPoints[perimeterIndex].x - perimeterPoints[perimeterIndex - 1].x;
                    inVec.y = perimeterPoints[perimeterIndex].y - perimeterPoints[perimeterIndex - 1].y;

                    outVec.x = perimeterPoints[perimeterIndex + 1].x - perimeterPoints[perimeterIndex].x;
                    outVec.y = perimeterPoints[perimeterIndex + 1].y - perimeterPoints[perimeterIndex].y;
                }

                if (Math.Abs(inVec.x - outVec.x) < 0.1)
                {
                    if (Math.Abs(inVec.y - outVec.y) < 0.1)
                    {
                        removalList.Add(perimeterIndex);
                    }
                }

                perimeterIndex++;
            }

            for (int i = removalList.Count - 1; i >= 0; i--)
            {
                perimeterPoints.RemoveAt(removalList[i]);
            }

            return perimeterPoints;
        }
        void respondToValRequests(string response)
        {
            string[] responses = response.Split('+');
            List<Validation> validations = new List<Validation>();

            for (int i = 0; i < responses.Length; i++)
            {
                string[] valDetail = responses[i].Split('|');

                Validation v = new Validation();
                v.steamid = Convert.ToUInt64(valDetail[0]);
                v.details = valDetail[1];
                validations.Add(v);
            }

            if (validations.Count > 0)
            {
                for (int i = 0; i < allBasePlayer.Count; i++)
                {
                    for (int val = 0; val < validations.Count; val++)
                    {
                        if (allBasePlayer[i].userID == validations[val].steamid)
                        {
                            if (!validated.Contains(allBasePlayer[i].userID))
                            {
                                validated.Add(allBasePlayer[i].userID);
                            }
                            SendReply(allBasePlayer[i], "Your validation code is " + validations[val].details + " and your SteamID is " + validations[val].steamid.ToString() + ".");
                        }
                    }
                }
            }

            valReqs.Clear();
        }
        void saveAllData(BasePlayer player)
        {
            saveConfiguration();
            saveDataFiles();
            if (player != null) { SendReply(player, "Config and data saved."); }
        }
        void saveConfiguration()
        {
            Config.Clear();
            GetConfig("SETTINGS", "Converted", updatedOldData);
            GetConfig("OPTIONS", "Time between updates", timeBetweenChecks);
            GetConfig("OPTIONS", "Max perimeter size", maxPerimeterDelta);
            GetConfig("OPTIONS", "Max points per perimeter", maxPerimeterPoints);
            GetConfig("OPTIONS", "Max perimeters per player", maxPerimetersPerPlayer);

            GetConfig("OPTIONS", "Hide names and SteamIDs from alerts", sendAnon);
            GetConfig("OPTIONS", "Enable automatic perimeter checking", automaticCheckingEnabled);
            GetConfig("OPTIONS", "Sync alerts with the Raid Alert server", syncWithRustySheriffServer);
            GetConfig("OPTIONS", "Alerts only triggered if player sleeping in their perimeter", alertTriggeredOnlyWhenSleepingInPerimeter);
            GetConfig("OPTIONS", "Show alerts in-game", alertsVisibleInGame);
            GetConfig("OPTIONS", "Use cupboard authorisation when creating perimeters", useCupboards);
            GetConfig("OPTIONS", "Enabled for authorised users only", enabledForAuthorisedUsersOnly);
            SaveConfig();
        }
        void saveDataFiles()
        {
            List<SavePerimeterEntry> savePerimeters = new List<SavePerimeterEntry>();
            //flatten perimeters
            for (int i = 0; i < perimeters.Count; i++)
            {
                for (int i2 = 0; i2 < perimeters[i].playerPerimeters.Count; i2++)
                {
                    SavePerimeterEntry s = new SavePerimeterEntry();
                    s.steamid = perimeters[i].steamid;
                    s.name = perimeters[i].playerPerimeters[i2].name;
                    s.finished = perimeters[i].playerPerimeters[i2].finished;
                    s.id = perimeters[i].playerPerimeters[i2].id;


                    for (int v = 0; v < perimeters[i].playerPerimeters[i2].coords.Count; v++)
                    {
                        s.coordsX.Add(perimeters[i].playerPerimeters[i2].coords[v].x);
                        s.coordsY.Add(perimeters[i].playerPerimeters[i2].coords[v].y);
                        s.coordsZ.Add(perimeters[i].playerPerimeters[i2].coords[v].z);
                    }

                    s.minX = perimeters[i].playerPerimeters[i2].minX;
                    s.minY = perimeters[i].playerPerimeters[i2].minY;
                    s.maxX = perimeters[i].playerPerimeters[i2].maxX;
                    s.maxY = perimeters[i].playerPerimeters[i2].maxY;
                    s.sleeperPresent = perimeters[i].playerPerimeters[i2].sleeperPresent;
                    savePerimeters.Add(s);
                }
            }

            SaveObject saveObj = new SaveObject();
            saveObj.validated = validated;
            saveObj.authList = authList;
            saveObj.muteList = muteList;
            saveObj.ignoreList = ignoreList;
            //saveObj.savePerimeters = savePerimeters;

            Interface.Oxide.DataFileSystem.WriteObject<SaveObject>("RaidAlert v1", saveObj);
            Interface.Oxide.DataFileSystem.WriteObject<List<SavePerimeterEntry>>("RaidAlert - Perimeters - delete on server wipe", savePerimeters);
        }
        void sendTestAlert(BasePlayer player)
        {
            TestAlert t = new TestAlert();
            t.steamid = player.userID;
            t.name = player.displayName;

            testAlerts.Add(t);
            SendReply(player, "A test alert has been queued for external delivery.");
        }
        void set(BasePlayer player, string[] args)
        {
            if (args.Length > 1)
            {
                FindAllFrom(player, args[1]);
            }
            else
            {
                SendReply(player, "Enter a name for this perimeter.");
            }
        }
        void setAuthLevel(ulong steamid, int level, string name, BasePlayer initPlayer, bool quiet)
        {
            if (initPlayer != null)
            {
                if (initPlayer.userID == steamid)
                {
                    SendReply(initPlayer, "You cannot modify your own authorisation level.");
                    return;
                }
            }

            bool match = false;
            int index = getAuthIndex(steamid);

            BasePlayer player = null;
            int playerIndex = getPlayerIndex(steamid);

            if (playerIndex != -1)
            {
                player = allBasePlayer[playerIndex];
            }

            //check for -1 after all getPerimeterIndex etc

            if (index == -1)
            {
                Auth a = new Auth();
                a.steamid = steamid;
                a.name = name;
                a.level = level;
                authList.Add(a);
                sortAuths();
                hashAuths();


                if (level == 0)
                {
                    if (player != null) { SendReply(player, "You've been added as a Raid Alert user."); }
                    if (initPlayer != null && initPlayer != player) { SendReply(initPlayer, "You've been added as a Raid Alert user."); }
                    else
                    {
                        Puts(name + " " + steamid.ToString() + " added as a Raid Alert user.");
                    }
                }

                if (level == 1)
                {
                    if (player != null) { SendReply(player, "You've been added as a Raid Alert privileged user."); }
                    if (initPlayer != null && initPlayer != player) { SendReply(initPlayer, "You've been added as a Raid Alert privileged user."); }
                    else
                    {
                        Puts(name + " " + steamid.ToString() + " added as a Raid Alert privileged user.");
                    }
                }

                if (level == 2)
                {
                    if (player != null) { SendReply(player, "You've been added as a Raid Alert admin."); }
                    if (initPlayer != null && initPlayer != player) { SendReply(initPlayer, "You've been added as a Raid Alert admin."); }
                    else
                    {
                        Puts(name + " " + steamid.ToString() + " added as a Raid Alert admin.");
                    }
                }
            }
            else
            {
                if (authList[index].level == level)
                {
                    if (name == authList[index].name)
                    {
                        if (initPlayer != null)
                        {
                            SendReply(initPlayer, name + " already has that auth level.");
                        }
                        else
                        {
                            if (!quiet)
                            {
                                Puts(name + " already has that auth level.");
                            }
                        }
                        return;
                    }
                    else
                    {
                        if (initPlayer != null)
                        {
                            SendReply(initPlayer, "Updated player name in auth list.");
                        }
                        else
                        {
                            Puts("Updated player name in auth list.");
                        }
                        authList[index].name = name;
                    }
                }

                string newLevel = "";
                string grade = "";

                if (level == 0) { newLevel = "Raid Alert user"; }

                if (level == 1) { newLevel = "Raid Alert privileged user"; }

                if (level == 2) { newLevel = "Raid Alert admin"; }

                if (level < authList[index].level)
                {
                    grade = "downgraded";
                }
                else
                {
                    if (level == authList[index].level)
                    {
                        authList[index].level = level;
                        return;
                    }
                    else
                    {
                        grade = "upgraded";
                    }
                }

                authList[index].level = level;
                if (player != null) { SendReply(player, "You have been " + grade + " to a " + newLevel + "."); }
                if (initPlayer != null && initPlayer != player) { SendReply(initPlayer, name + " has been " + grade + " to a " + newLevel + "."); }
                else
                {
                    Puts(name + " has been " + grade + " to a " + newLevel + ".");
                }
            }
        }
        void setCheckInterval(BasePlayer player, string[] args)
        {
            if (args.Length > 1)
            {
                int val;

                try
                {
                    val = Convert.ToInt32(args[1]);

                    if (val >= 0)
                    {
                        SendReply(player, "Each player and perimeter will be checked over a period of " + val.ToString() + " seconds.");
                    }
                    else
                    {
                        SendReply(player, "Please enter a valid number for the check duration.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    SendReply(player, "Please enter a valid number for the check duration.");
                    return;
                }

                timeBetweenChecks = val;
            }
            else
            {
                SendReply(player, "Please enter a valid number for the check duration.");
            }
        }
        void setMaxPerimeters(BasePlayer player, string[] args)
        {
            if (args.Length > 1)
            {
                int val;

                try
                {
                    val = Convert.ToInt32(args[1]);

                    if (val >= 0)
                    {
                        SendReply(player, "The maximum number of perimeters has been set to " + val.ToString());
                    }
                    else
                    {
                        SendReply(player, "Enter a valid number for the maximum number of perimeters per player.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    SendReply(player, "Enter a valid number for the maximum number of perimeters per player.");
                    return;
                }

                maxPerimetersPerPlayer = val;
            }
            else
            {
                SendReply(player, "Enter a valid number for the maximum number of perimeters per player.");
            }
        }
        void setMaxPoints(BasePlayer player, string[] args)
        {
            if (args.Length > 1)
            {
                int val;

                try
                {
                    val = Convert.ToInt32(args[1]);

                    if (val >= 0)
                    {
                        SendReply(player, "The maximum number of points per perimeter has been set to " + val.ToString());
                    }
                    else
                    {
                        SendReply(player, "Enter a valid number for the maximum number of points per perimiter.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    SendReply(player, "Enter a valid number for the maximum number of points per perimiter.");
                    return;
                }

                maxPerimeterPoints = val;
            }
            else
            {
                SendReply(player, "Enter a valid number for the maximum number of points per perimiter.");
            }
        }
        void setMaxSize(BasePlayer player, string[] args)
        {
            if (args.Length > 1)
            {
                int val;

                try
                {
                    val = Convert.ToInt32(args[1]);

                    if (val >= 0)
                    {
                        SendReply(player, "The maximum perimeter size has been set to " + val.ToString() + " x " + val.ToString());
                    }
                    else
                    {
                        SendReply(player, "Enter a valid number for the maximum perimeter size.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    SendReply(player, "Enter a valid number for the maximum perimeter size.");
                    return;
                }

                maxPerimeterDelta = val;
            }
            else
            {
                SendReply(player, "Enter a valid number for the maximum perimeter size.");
            }
        }
        void setTimeBetweenChecks(BasePlayer player, string[] args)
        {
            if (args.Length == 1)
            {
                SendReply(player, "Please enter the time in seconds over which to check players and perimeters.");
                return;
            }

            int val;

            try
            {
                val = Convert.ToInt32(args[1]);
            }
            catch (Exception e)
            {
                SendReply(player, "Please enter the time in seconds over which to check players and perimeters.");
                return;
            }

            if (val < 5)
            {
                SendReply(player, "The number of seconds you have chosen is too short.");
                return;
            }

            timeBetweenChecks = val;
            SendReply(player, "Players and perimeters will now be checked over an interval of " + timeBetweenChecks.ToString() + " seconds.");
        }
        void showAdminMenu(BasePlayer player)
        {
            SendReply(player, "<color=#00FF00>Rusty Sheriff Raid Alert Admin Commands</color>");
            SendReply(player, "enable/disable - enable/disable perimeter checking");
            SendReply(player, "status - displays the values of the options below");
            SendReply(player, "secure - toggles whether the alert system is available to unauthorised users");
            SendReply(player, "cupboard - toggle whether players can create perimeters if not cupboard authorised");
            SendReply(player, "chatmute - toggle whether player's alerts show in-game");
            SendReply(player, "time <number> - sets the interval over which perimeter checks are made.");
            SendReply(player, "sleepers - toggle whether an alert is triggered only if the player is sleeping within their perimeter");
            SendReply(player, "sync - toggle synchronisation with the Raid Alert server");
            SendReply(player, "maxperim <number> - set the maximum number of perimeters a player can set");
            SendReply(player, "maxsize <number> - set the maximum area a perimeter can span to <number> x <number> metres");
            SendReply(player, "maxpoints <number> - set the maxmimum number of points a player's perimeter can contain");
            SendReply(player, "anon - toggles whether or not alerts display the player's name and SteamID");
            SendReply(player, "auth - shows authorisation commands");
        }
        void showAdvancedMenu(BasePlayer player)
        {
            SendReply(player, "<color=#00FF00>Rusty Sheriff Advanced Player Commands</color>");
            SendReply(player, "start <perimeter name> - to start manually recording a new perimeter");
            SendReply(player, "add - add a waypoint to a perimeter");
            SendReply(player, "undo - remove the previous waypoint");
            SendReply(player, "cancel - cancel logging waypoints");
            SendReply(player, "stop  - finalise a perimeter");
            SendReply(player, "ignore <player name> - add an online player to your ignore list");
            SendReply(player, "ignore <steamid> - add an online player to your ignore list");
            SendReply(player, "ignore <steamid> <player name> - add an offline player to your ignore list");
            SendReply(player, "validate/validatenew - get or create a new validation code for use in the PC/Android app.");
        }
        void showAuthMenu(BasePlayer player)
        {
            SendReply(player, "<color=#00FF00>Rusty Sheriff Raid Alert Auth Commands</color>");
            SendReply(player, "auths - view a list of authorised user for this server");
            SendReply(player, "adduser <steamid>/<playername> <authLevel> - add an online player to the authorised user list.");
            SendReply(player, "adduser <steamid> <playername> <authLevel> - add an offline player to the authorised user list.");
            SendReply(player, "deluser <index> - delete a user from the authorised user list");
            SendReply(player, "save - saves all Raid Alert data and configuration settings");
            SendReply(player, "Console Commands");
            SendReply(player, "rs.auths - as above");
            SendReply(player, "rs.adduser - as above");
        }
        void showAuthorisedUsers(BasePlayer player)
        {
            if (authList.Count > 0)
            {
                if (player != null)
                {
                    SendReply(player, "<color=#00FF00>Raid Alert Authorised Users List</color>");
                }
                else
                {
                    Puts("Raid Alert Authorised Users List");
                }

                for (int i = 0; i < authList.Count; i++)
                {
                    if (authList[i].level == 0)
                    {
                        if (player != null)
                        {

                            SendReply(player, (i + 1).ToString() + " - " + authList[i].name + " " + authList[i].steamid.ToString() + " - Basic User.");
                        }
                        else
                        {
                            Puts((i + 1).ToString() + " - " + authList[i].name + " " + authList[i].steamid.ToString() + " - Basic User.");
                        }
                    }

                    if (authList[i].level == 1)
                    {
                        if (player != null)
                        {
                            SendReply(player, (i + 1).ToString() + " - " + authList[i].name + " " + authList[i].steamid.ToString() + " - Priviledged User.");
                        }
                        else
                        {
                            Puts((i + 1).ToString() + " - " + authList[i].name + " " + authList[i].steamid.ToString() + " - Priviledged User.");
                        }
                    }

                    if (authList[i].level == 2)
                    {
                        if (player != null)
                        {
                            SendReply(player, (i + 1).ToString() + " - " + authList[i].name + " " + authList[i].steamid.ToString() + " - Raid Alert Admin.");
                        }
                        else
                        {
                            Puts((i + 1).ToString() + " - " + authList[i].name + " " + authList[i].steamid.ToString() + " - Raid Alert Admin.");
                        }
                    }
                }
            }
            else
            {
                if (player != null)
                {
                    SendReply(player, "Authorised users list is empty.");
                }
                else
                {
                    Puts("Authorised users list is empty.");
                }
            }

        }
        void showHelpMenu(BasePlayer player)
        {
            SendReply(player, "<color=#00FF00>Rusty Sheriff Raid Alert Instructions</color>");
            SendReply(player, "Use /rs set <perimeter name> to auto create a perimeter around your foundation/floor.");

            if (alertsVisibleInGame)
            {
                SendReply(player, "You will now be alerted in-game if another player breaches your perimeter.");
            }
            else
            {
                SendReply(player, "Your admin has disabled in-game alerts, so you won't be alerted in-game of any perimeter breaches.");
            }

            if (syncWithRustySheriffServer)
            {
                SendReply(player, "Within " + timeBetweenChecks.ToString() + " seconds, you will receive a validation code.");
                SendReply(player, "Enter the code along with your SteamID in the Raid Alert app if you wish to receive alerts externally.");
                SendReply(player, "A test alert will be automatically sent to your device.  To send another do /rs test.");
                SendReply(player, "Within a minute or so, you'll receive a test alert on your PC/Android device.");
                SendReply(player, "To get a copy of the PC/Android App search for \"Rusty Sheriff Raid Alert\" on Google Play.  The PC App is linked in the description.");
            }
            else
            {
                SendReply(player, "Your admin has disabled the option to send alerts to your PC/Android device.");
            }

            SendReply(player, "If you need further assistance or wish to report a bug, contact JV at djgamingservices@gmail.com.");
        }
        void showMainMenu(BasePlayer player)
        {
            SendReply(player, "<color=#00FF00>Welcome to the Rusty Sheriff Raid Alert System</color>");
            SendReply(player, "Use /rs followed by");
            SendReply(player, "set <perimeter name> - automatically create a perimeter around your foundation/floor");
            SendReply(player, "view - show a list of perimeters and draw them in-game");
            SendReply(player, "ignores - show your ignore list");
            SendReply(player, "delete/unignore <index> - remove a perimeter/ignored player");
            SendReply(player, "clear/clearignores - remove all perimeters/ignored players");
            SendReply(player, "ignoredetect - add all players within your perimeters to your ignore list");
            SendReply(player, "mute/unmute - enable/disable in-game alerts");
            SendReply(player, "test - test your perimeter and send a test alert");
            SendReply(player, "adv - advanced commands");
            SendReply(player, "help - show intructions");

            if (getAuthLevel(player.userID) == 2)
            {
                SendReply(player, "admin - show the admin commands menu");
            }
        }
        void showStatus(BasePlayer player)
        {
            SendReply(player, "<color=#00FF00>Rusty Sheriff Raid Alert Status</color>");

            SendReply(player, "Players can have a maximum of " + maxPerimetersPerPlayer.ToString() + " perimeters.");
            SendReply(player, "Time between perimeter checks is " + timeBetweenChecks.ToString() + " seconds.");
            SendReply(player, "The maximum perimeter size is " + maxPerimeterDelta.ToString() + " x " + maxPerimeterDelta.ToString() + ".");
            SendReply(player, "The maximum number of points per perimeter is " + maxPerimeterPoints.ToString() + ".");

            if (alertsVisibleInGame)
            {
                SendReply(player, "Alerts are displayed to players in-game.");
            }
            else
            {
                SendReply(player, "Alerts are not displayed to players in-game.");
            }

            if (alertTriggeredOnlyWhenSleepingInPerimeter)
            {
                SendReply(player, "Alerts are only triggered when the player is sleeping within their perimeter.");
            }
            else
            {
                SendReply(player, "Alerts are sent regardless of the player's online status or sleeper location.");
            }

            if (syncWithRustySheriffServer)
            {
                SendReply(player, "Synchronisation with the Raid Alert server is enabled.");
            }
            else
            {
                SendReply(player, "Synchronisation with the Raid Alert server is DISABLED.");
            }

            if (enabledForAuthorisedUsersOnly)
            {
                SendReply(player, "Raid Alert is only available to authorised users.");
            }
            else
            {
                SendReply(player, "Raid Alert is available to all users.");
            }

            if (sendAnon)
            {
                SendReply(player, "Trespasser names and Steam IDs are not visible to players in-game nor externally.");
            }
            else
            {
                SendReply(player, "Trespasser names and Steam IDs are visible to players in-game and externally.");
            }

            if (automaticCheckingEnabled)
            {
                SendReply(player, "Automatic checking of players and perimeters is enabled.");
            }
            else
            {
                SendReply(player, "Automatic checking of players and perimeters is DISABLED.");
            }

            if (useCupboards)
            {
                SendReply(player, "Players must have tool cupboard authorisation to create a perimeter.");
            }
            else
            {
                SendReply(player, "Players do not need tool cupboard authorisation to create a perimeter.");
            }

            int perimeterCount = 0;
            int players = 0;

            for (int i = 0; i < perimeters.Count; i++)
            {
                perimeterCount += perimeters[i].playerPerimeters.Count;

                if (perimeters[i].playerPerimeters.Count > 0)
                {
                    players++;
                }
            }

            SendReply(player, "There are " + perimeterCount.ToString() + " perimeters defined, by " + players.ToString() + " players.");
        }
        void sortAuths()
        {
            List<string> nameList = new List<string>();

            for (int i = 0; i < authList.Count; i++)
            {
                nameList.Add(authList[i].name);
            }

            nameList.Sort();

            List<Auth> ath = new List<Auth>();

            for (int a = 0; a < nameList.Count; a++)
            {
                for (int b = 0; b < authList.Count; b++)
                {
                    if (nameList[a] == authList[b].name)
                    {
                        ath.Add(authList[b]);
                        authList.RemoveAt(b);
                        break;
                    }
                }
            }

            authList = ath;
        }
        void sortIgnores(int ignoreListIndex)
        {
            if (ignoreList[ignoreListIndex].ignores.Count < 2) { return; }

            List<string> nameList = new List<string>();

            for (int i = 0; i < ignoreList[ignoreListIndex].ignores.Count; i++)
            {
                nameList.Add(ignoreList[ignoreListIndex].ignores[i].name);
            }

            nameList.Sort();

            List<IgnoredPlayer> newIgnores = new List<IgnoredPlayer>();

            for (int n = 0; n < nameList.Count; n++)
            {
                for (int p = 0; p < ignoreList[ignoreListIndex].ignores.Count; p++)
                {
                    if (nameList[n] == ignoreList[ignoreListIndex].ignores[p].name)
                    {
                        newIgnores.Add(ignoreList[ignoreListIndex].ignores[p]);
                        ignoreList[ignoreListIndex].ignores.RemoveAt(p);
                        break;
                    }
                }
            }

            ignoreList[ignoreListIndex].ignores = newIgnores;
        }
        void sortPlayerPerimeters(int perimeterIndex)
        {
            if (perimeters[perimeterIndex].playerPerimeters.Count < 2) { return; }

            List<string> nameList = new List<string>();

            for (int i = 0; i < perimeters[perimeterIndex].playerPerimeters.Count; i++)
            {
                nameList.Add(perimeters[perimeterIndex].playerPerimeters[i].name);
            }

            nameList.Sort();

            List<PlayerPerimeter> newPP = new List<PlayerPerimeter>();

            for (int n = 0; n < nameList.Count; n++)
            {
                for (int p = 0; p < perimeters[perimeterIndex].playerPerimeters.Count; p++)
                {
                    if (nameList[n] == perimeters[perimeterIndex].playerPerimeters[p].name)
                    {
                        newPP.Add(perimeters[perimeterIndex].playerPerimeters[p]);
                        perimeters[perimeterIndex].playerPerimeters.RemoveAt(p);
                        break;
                    }
                }
            }

            perimeters[perimeterIndex].playerPerimeters = newPP;
        }
        void startDrawing(BasePlayer player, int perimeterIndex, int playerPerimeterIndex)
        {
            DrawListEntry d = new DrawListEntry();
            d.basePlayer = player;
            d.perimeterIndex = perimeterIndex;
            d.playerPerimeterIndex = playerPerimeterIndex;

            drawList.Add(d);
        }
        void startPerimeter(BasePlayer player, string[] args)
        {
            ulong steamid = player.userID;

            if (!isInProgress(steamid))
            {
                if (args.Length > 1)
                {
                    int pCount = countPerimeters(steamid);

                    if (pCount >= maxPerimetersPerPlayer && getAuthLevel(player.userID) < 1)
                    {
                        SendReply(player, "You already have the maximum of " + maxPerimetersPerPlayer.ToString() + " perimeters on this server.  Do /rs delete <index>.");
                        return;
                    }

                    if (useCupboards)
                    {
                        if (!hasTotalAccess(player) && getAuthLevel(player.userID) < 1)
                        {
                            SendReply(player, "You must be authorised on a nearby cupboard to create a perimeter here.");
                            return;
                        }
                    }
                    addPerimeter(player, args[1]);
                }
                else
                {
                    SendReply(player, "Please enter a name for this perimeter.  Do /rs start <name>");
                }
            }
            else
            {
                SendReply(player, "You are already logging waypoints for perimeter " + getInProgress(steamid) + ".  Do /rs cancel.");
            }
        }
        void stopDrawing(BasePlayer player)
        {
            for (int i = 0; i < drawList.Count; i++)
            {
                if (drawList[i].basePlayer.userID == player.userID)
                {
                    drawList.RemoveAt(i);
                    return;
                }
            }
        }
        void stopPerimeter(BasePlayer player)
        {
            if (isInProgress(player.userID))
            {
                int r = addPerimeterEntry(player, true);
                if (r == -1)
                {
                    return;
                }
                else
                {
                    if (r < 3)
                    {
                        SendReply(player, "You need at least 3 points to create a perimeter.  Do /rs add to enter more waypoints.");
                        stopPerimeter(player, true);
                        return;
                    }
                }

                stopDrawing(player);
                stopPerimeter(player, false);
            }
            else
            {
                SendReply(player, "You have no perimeters in progress.  Do /rs set <name> to create one.");
            }
        }
        void stopPerimeter(BasePlayer player, bool cancel)
        {
            string name = "";
            int inProgressIndex = -1;

            PerimeterPosition pPos = getInProgress(player.userID);

            if (pPos.perimeterIndex == -1)
            {
                SendReply(player, "You have no perimeters in progress.");
                return;
            }
            else
            {
                name = perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].name;
            }

            stopDrawing(player);

            if (cancel)
            {
                perimeters[pPos.perimeterIndex].playerPerimeters.RemoveAt(pPos.playerPerimeterIndex);
                SendReply(player, "Perimeter entry for " + name + " cancelled.");
            }
            else
            {
                ignorePlayer(player.userID, player.userID, player.displayName);
                perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].finished = true;
                addValidationRequest(player.userID, false);
                sendTestAlert(player);
                SendReply(player, "Perimeter entry successful.");
                plotPerimeter(player, pPos.perimeterIndex, pPos.playerPerimeterIndex);
            }
        }
        void testPerimeter(BasePlayer player)
        {
            int perimeterIndex = getPerimeterIndex(player.userID);

            if (perimeterIndex == -1)
            {
                SendReply(player, "You haven't set any perimeters.  Do /rs set <name>.");
                return;
            }

            bool match = false;

            for (int i = 0; i < perimeters[perimeterIndex].playerPerimeters.Count; i++)
            {
                PerimeterPosition pPos = new PerimeterPosition();
                pPos.perimeterIndex = perimeterIndex;
                pPos.playerPerimeterIndex = i;

                int wn = windingPoly(player.transform.position, pPos);

                if (wn != 0)
                {
                    SendReply(player, "You're currently within the bounds of location " + perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].name + ".");
                    match = true;

                    if (validated.Contains(player.userID))
                    {
                        if (syncWithRustySheriffServer)
                        {
                            TestAlert t = new TestAlert();
                            t.steamid = player.userID;
                            t.name = player.displayName;

                            testAlerts.Add(t);
                            SendReply(player, "A test alert has been queued for external delivery.");
                        }
                        else
                        {
                            SendReply(player, "External alerts have been disabled by the admin.");
                        }
                    }
                }
            }

            if (!match)
            {
                SendReply(player, "You're not currently within any of your perimeters.");
            }
        }
        void toggleAnon(BasePlayer player)
        {
            if (sendAnon)
            {
                sendAnon = false;
                saveConfiguration();
                SendReply(player, "Trespasser names and Steam IDs will be visible to players in=game and externally.");
            }
            else
            {
                sendAnon = true;
                saveConfiguration();
                SendReply(player, "Trespasser names and SteamIDs will be hidden from player in-game and externally.");
            }
        }
        void toggleAutoCheck(BasePlayer player, bool enable)
        {
            if (enable)
            {
                if (automaticCheckingEnabled)
                {
                    SendReply(player, "Automatic perimeter checking is already enabled.");
                }
                else
                {
                    SendReply(player, "Automatic perimeter checking enabled.");
                    automaticCheckingEnabled = true;
                    Puts("Automatic perimeter checking enabled by " + player.userID.ToString());
                }
            }
            else
            {
                if (automaticCheckingEnabled)
                {
                    SendReply(player, "Automatic perimeter checking disabled.");
                    automaticCheckingEnabled = false;
                    Puts("Automatic perimeter checking disabled by " + player.userID.ToString());
                }
                else
                {
                    SendReply(player, "Automatic perimeter checking is already disabled.");
                }
            }
        }
        void toggleChatMute(BasePlayer player)
        {
            if (alertsVisibleInGame)
            {
                alertsVisibleInGame = false;
                saveConfiguration();
                SendReply(player, "Alerts will no longer be displayed to players in-game.");
            }
            else
            {
                alertsVisibleInGame = true;
                saveConfiguration();
                SendReply(player, "Alerts will be displayed to players in-game.");
            }
        }
        void toggleSecureMode(BasePlayer player)
        {
            if (enabledForAuthorisedUsersOnly)
            {
                enabledForAuthorisedUsersOnly = false;
                saveConfiguration();
                SendReply(player, "Raid Alert is now available to all players.");
                Puts("Raid Alert auth mode disabled by player " + player.userID.ToString());
            }
            else
            {
                enabledForAuthorisedUsersOnly = true;
                saveConfiguration();
                SendReply(player, "Raid Alert is only available for authorised players.");
                Puts("Raid Alert auth mode enabled by player " + player.userID.ToString());
            }
        }
        void toggleSleepers(BasePlayer player)
        {
            if (alertTriggeredOnlyWhenSleepingInPerimeter)
            {
                alertTriggeredOnlyWhenSleepingInPerimeter = false;
                saveConfiguration();
                SendReply(player, "Alerts will be sent regardless of the player's online status or sleeper location.");
            }
            else
            {
                alertTriggeredOnlyWhenSleepingInPerimeter = true;
                saveConfiguration();
                SendReply(player, "Alerts will only be triggered if the player logs out within their perimeter.");
            }
        }
        void toggleSync(BasePlayer player)
        {
            if (syncWithRustySheriffServer)
            {
                syncWithRustySheriffServer = false;
                saveConfiguration();
                SendReply(player, "Synchronisation with the Raid Alert server has been disabled.");
            }
            else
            {
                syncWithRustySheriffServer = true;
                saveConfiguration();
                SendReply(player, "Synchronisation with the Raid Alert server has been enabled.");
            }
        }
        void toggleUseCupboard(BasePlayer player)
        {
            if (useCupboards)
            {
                useCupboards = false;
                saveConfiguration();
                SendReply(player, "Players can create perimeters without tool cupboard authorisation.");
            }
            else
            {
                useCupboards = true;
                saveConfiguration();
                SendReply(player, "Players must have tool cupboard authorisation before creating a perimeter.");
            }
        }
        void validatePlayer(BasePlayer player, bool newCode)
        {
            if (syncWithRustySheriffServer)
            {
                SendReply(player, "Please wait up to " + timeBetweenChecks.ToString() + " seconds for your validation code.");
                addValidationRequest(player.userID, newCode);
            }
            else
            {
                SendReply(player, "External alerts are currently disabled by the admin.");
            }
        }
        void undoLastPoint(BasePlayer player)
        {
            if (!isInProgress(player.userID))
            {
                SendReply(player, "You are not currently recording a perimeter.");
            }
            else
            {
                PerimeterPosition pPos = getInProgress(player.userID);
                string perimeterName = perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].name;

                if (perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords.Count > 0)
                {
                    int cnt = perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords.Count;
                    perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords.RemoveAt(cnt - 1);
                    SendReply(player, "Removed your last waypoint on perimeter " + perimeterName + ".");
                }
                else
                {
                    SendReply(player, "You have no waypoints left to remove from perimeter " + perimeterName + ".");
                }
            }
        }
        void unignorePlayer(BasePlayer player, string[] args)
        {
            if (args.Length > 1)
            {
                int index;

                try
                {
                    index = Convert.ToInt32(args[1]);
                }
                catch (Exception e)
                {
                    SendReply(player, "Please enter a valid index.  Do /rs ignores for a list.");
                    return;
                }

                int i = getIgnoreListIndex(player.userID);

                if (i != -1)
                {
                    if (index < 1 || index > ignoreList[i].ignores.Count)
                    {
                        SendReply(player, "Please enter a valid index.  Do /rs ignores for a list.");
                        return;
                    }

                    SendReply(player, "Removed " + ignoreList[i].ignores[index - 1].name + " from your ignore list.");
                    ignoreList[i].ignores.RemoveAt(index - 1);
                    return;
                }

                SendReply(player, "Your ignore list is empty.");
            }
            else
            {
                SendReply(player, "Please enter a valid index to remove from your ignore list.");
            }
        }
        void updateRSSServer()
        {
            if (syncWithRustySheriffServer)
            {
                List<Trespasser> alertsList = new List<Trespasser>();
                List<Trespasser> cancelsList = new List<Trespasser>();

                for (int i = 0; i < testAlerts.Count; i++)
                {
                    Trespasser t = new Trespasser();
                    t.steamid = testAlerts[i].steamid;
                    t.name = "Test Alert";
                    t.trespasserSteamID = t.steamid;
                    t.alertText = "|" + "1234567" + "|" + cleanString(testAlerts[i].name) + "|" + "0" + "|" + "0";

                    //Puts("nt = " + newTrespassers.Count.ToString());
                    //Puts("ot = " + oldTrespassers.Count.ToString());

                    //Puts("Added to newTrespasssers");
                    newTrespassers.Add(t);

                    //Puts("nt2 = " + newTrespassers.Count.ToString());
                    //Puts("ot2 = " + oldTrespassers.Count.ToString());
                }

                testAlerts.Clear();

                bool match = false;

                for (int i = 0; i < newTrespassers.Count; i++)
                {
                    match = false;
                    for (int j = 0; j < oldTrespassers.Count; j++)
                    {
                        if (newTrespassers[i].steamid == oldTrespassers[j].steamid)
                        {
                            if (newTrespassers[i].name == oldTrespassers[j].name)
                            {
                                if (newTrespassers[i].trespasserSteamID == oldTrespassers[j].trespasserSteamID)
                                {
                                    if (newTrespassers[i].id == oldTrespassers[j].id)
                                    {
                                    //Puts("NEW MATCHES OLD");
                                    match = true;
                                    }
                                }
                            }
                        }
                    }

                    if (!match)
                    {
                        //Puts("ADDED ALERT");
                        alertsList.Add(newTrespassers[i]);
                    }
                }

                for (int i = 0; i < oldTrespassers.Count; i++)
                {
                    match = false;
                    for (int j = 0; j < newTrespassers.Count; j++)
                    {
                        if (oldTrespassers[i].steamid == newTrespassers[j].steamid)
                        {
                            if (oldTrespassers[i].name == newTrespassers[j].name)
                            {
                                if (oldTrespassers[i].trespasserSteamID == newTrespassers[j].trespasserSteamID)
                                {
                                    if (oldTrespassers[i].id == newTrespassers[j].id)
                                    {
                                    //Puts("OLD MATCHES NEW");
                                    match = true;
                                    }
                                }
                            }
                        }
                    }

                    if (!match)
                    {
                        //Puts("ADDED CANCEL");
                        cancelsList.Add(oldTrespassers[i]);
                    }
                }

                string trespassText = "";

                for (int i = 0; i < valReqs.Count; i++)
                {
                    if (valReqs[i].newCode)
                    {
                        trespassText += "+VLN|" + valReqs[i].steamid.ToString();
                    }
                    else
                    {
                        trespassText += "+VLD|" + valReqs[i].steamid.ToString();
                    }
                }

                for (int i = 0; i < cancelsList.Count; i++)
                {
                    if (!sendAnon || getAuthLevel(cancelsList[i].steamid) > 0)
                    {
                        //Puts("C");
                        trespassText += "+CAN|" + cancelsList[i].steamid.ToString() + "|" + cancelsList[i].name + "|" + cancelsList[i].trespasserSteamID.ToString() + cancelsList[i].alertText;
                    }
                    else
                    {
                        trespassText += "+CAN|" + cancelsList[i].steamid.ToString() + "|" + cancelsList[i].name + "|" + cancelsList[i].trespasserSteamID.ToString() + cancelsList[i].alertText + "|HIDDEN";
                    }
                }

                for (int i = 0; i < alertsList.Count; i++)
                {
                    if (!sendAnon || getAuthLevel(alertsList[i].steamid) > 0)
                    {
                        //Puts("A");
                        trespassText += "+ALR|" + alertsList[i].steamid.ToString() + "|" + alertsList[i].name + "|" + alertsList[i].trespasserSteamID.ToString() + alertsList[i].alertText;
                    }
                    else
                    {
                        trespassText += "+ALR|" + alertsList[i].steamid.ToString() + "|" + alertsList[i].name + "|" + alertsList[i].trespasserSteamID.ToString() + alertsList[i].alertText + "|HIDDEN";
                    }
                }

                if (trespassText.Length > 0)
                {
                    string hostname = cleanString(ConVar.Server.hostname);
                    trespassText = "|" + hostname + "|" + ConVar.Server.port.ToString() + trespassText + "#";
                    trespassText = trespassText.Length.ToString() + trespassText;
                    //Puts("Sending " + trespassText);
                    Interface.GetMod().GetLibrary<WebRequests>("WebRequests").EnqueuePost(url, trespassText, (code, response) => parseWebReply(code, response), this);
                }
                else
                {
                    newTrespassers.Clear();
                }
            }
        }
        void viewIgnoreList(BasePlayer player)
        {
            List<string> ignoreEntries = new List<string>();

            int i = getIgnoreListIndex(player.userID);

            if (i != -1)
            {
                for (int p = 0; p < ignoreList[i].ignores.Count; p++)
                {
                    ignoreEntries.Add(ignoreList[i].ignores[p].name + " - " + ignoreList[i].ignores[p].steamid.ToString());
                }

                if (ignoreEntries.Count > 0)
                {
                    SendReply(player, "You are ignoring the following players:");
                    for (int c = 0; c < ignoreEntries.Count; c++)
                    {
                        SendReply(player, (c + 1).ToString() + " - " + ignoreEntries[c]);
                    }
                    return;
                }
                else
                {
                    SendReply(player, "Your ignore list is empty.");
                    return;
                }
            }

            SendReply(player, "Your ignore list is empty.");
        }
        void viewPerimeters(BasePlayer player, string[] args)
        {
            int maxViewDistance = 500;
            int perimeterIndex = getPerimeterIndex(player.userID);

            if (perimeterIndex == -1)
            {
                SendReply(player, "You have no perimeters defined.  Create one with /rs set <name>.");
                return;
            }
            else
            {
                if (perimeters[perimeterIndex].playerPerimeters.Count == 0)
                {
                    SendReply(player, "You have no perimeters defined.  Create one with /rs set <name>.");
                    return;
                }

                SendReply(player, "Your current perimeters are - ");

                for (int i = 0; i < perimeters[perimeterIndex].playerPerimeters.Count; i++)
                {
                    //Puts("Plotting perimeter " + i.ToString());
                    if (Math.Abs(player.transform.position.x - perimeters[perimeterIndex].playerPerimeters[i].coords[0].x) <= 500)
                    {
                        if (Math.Abs(player.transform.position.z - perimeters[perimeterIndex].playerPerimeters[i].coords[0].z) <= 500)
                        {
                            plotPerimeter(player, perimeterIndex, i);
                        }
                    }
                    SendReply(player, (i + 1).ToString() + " - " + perimeters[perimeterIndex].playerPerimeters[i].name);
                }
            }
        }
        int windingPoly(Vector3 position, PerimeterPosition pPos)
        {
            int wn = 0;

            for (int i = 0; i < perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords.Count; i++)
            {
                int tmp;

                if (i == perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords.Count - 1)
                {
                    tmp = 0;
                }
                else
                {
                    tmp = i + 1;
                }

                if (perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords[i].z <= position.z)
                {
                    if (perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords[tmp].z > position.z)
                    {
                        if (isLeft(perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords[i], perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords[tmp], position) > 0)
                        {
                            wn++;
                        }
                    }
                }
                else
                {
                    if (perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords[tmp].z <= position.z)
                    {
                        if (isLeft(perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords[i], perimeters[pPos.perimeterIndex].playerPerimeters[pPos.playerPerimeterIndex].coords[tmp], position) < 0)
                        {
                            wn--;
                        }
                    }
                }
            }

            return wn;
        }

        //***********************//
        //structures/classes here//
        //***********************//
        public class Auth
        {
            public ulong steamid;
            public string name;
            public int level;
        }
        class DrawListEntry
        {
            public BasePlayer basePlayer;
            public int perimeterIndex;
            public int playerPerimeterIndex;
        }
        class Euler
        {
            public double x, y, z;
        }
        class Face
        {
            public double sx, sy, ex, ey;
            public string name;
        }
        class IgnoredPlayer
        {
            public ulong steamid;
            public string name;
        }
        class IgnoreListEntry
        {
            public ulong steamid;
            public List<IgnoredPlayer> ignores = new List<IgnoredPlayer>();
        }
        class name
        {
            public string foundationName;
        }
        class OldAlert
        {
            public Dictionary<string, OldAlertDetails> alertName;
        }
        class OldAlertDetails
        {
            public Dictionary<string, Dictionary<string, double>> coords;
            public int count;
            public bool finished;
            public uint id;
            public double maxx;
            public double maxy;
            public double minx;
            public double miny;
            public bool SleeperPresent;
        }
        class OldAlertsObjectDict
        {
            public Dictionary<string, Dictionary<string, OldAlertDetails>> alerts;
        }
        class OldAuthObjectDict
        {
            public Dictionary<string, string> authNames;
            public Dictionary<string, string> authorised;
        }
        class OldIgnoresObjectDict
        {
            public Dictionary<string, Dictionary<string, string>> ignores;
        }
        class OldIgnoredPlayers
        {
            public Dictionary<string, string> ignoredPlayers;
        }
        class OldMuteList
        {
            public Dictionary<string, string> muted;
        }
        class OldValidatedObjectDict
        {
            public Dictionary<string, string> validated;
        }
        public class Perimeter
        {
            public ulong steamid;
            public List<PlayerPerimeter> playerPerimeters = new List<PlayerPerimeter>();
        }
        class PerimeterPosition
        {
            public int perimeterIndex;
            public int playerPerimeterIndex;
        }
        public class PlayerPerimeter
        {
            public string name;
            public bool finished;
            public uint id;
            public List<Vector3> coords = new List<Vector3>();
            public double minX, minY, maxX, maxY;
            public bool sleeperPresent;
        }
        struct Point
        {
            public double x;
            public double y;
        }
        struct Quat
        {
            public double w;
            public double x;
            public double y;
            public double z;
        }
        class Rect
        {
            public Point[] points = new Point[4];
        }
        class SaveObject
        {
            public List<ulong> validated;
            public List<Auth> authList;
            public List<ulong> muteList;
            public List<IgnoreListEntry> ignoreList;
            //public List<SavePerimeterEntry> savePerimeters;
        }
        public class SavePerimeterEntry
        {
            public ulong steamid;
            public string name;
            public bool finished;
            public uint id;
            public List<double> coordsX = new List<double>();
            public List<double> coordsY = new List<double>();
            public List<double> coordsZ = new List<double>();
            public double minX, minY, maxX, maxY;
            public bool sleeperPresent;
        }
        public class TestAlert
        {
            public ulong steamid;
            public string name;
        }
        struct Trespasser
        {
            public ulong steamid;
            public string name;
            public ulong trespasserSteamID;
            public string alertText;
            public uint id;
        }
        class ValReq
        {
            public ulong steamid;
            public bool newCode;
        }
        class Validation
        {
            public ulong steamid;
            public string details;
        }
    }
}