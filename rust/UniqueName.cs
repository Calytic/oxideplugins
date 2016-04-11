// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json

/*
 * The MIT License (MIT)
 * Copyright (c) 2015 Feramor
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

//Microsoft NameSpaces
using System;
using System.Collections.Generic;

//Oxide NameSpaces
using Oxide.Core;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

//External NameSpaces
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Unique Name Plugin", "Feramor", "1.0.1")]
    public class UniqueName : RustPlugin
    {
        private static Logger logger = Interface.GetMod().RootLogger;
        public Core.Configuration.DynamicConfigFile mySave;

        Dictionary<string, object> UserNames = new Dictionary<string, object>();
        Dictionary<string, object> Users = new Dictionary<string, object>();
        Dictionary<string, object> KickList = new Dictionary<string, object>();
        void Init()
        {
            LoadConfig();
            mySave = Interface.GetMod().DataFileSystem.GetDatafile("UniqueName");

            if (mySave["UserNames"] != null)
                if (((Dictionary<string, object>)mySave["UserNames"]).Count != 0)
                    UserNames = (Dictionary<string, object>) mySave["UserNames"];
            if (mySave["Users"] != null)
                if (((Dictionary<string, object>)mySave["Users"]).Count != 0)
                    Users = (Dictionary<string, object>)mySave["Users"];

            foreach (BasePlayer CurrentPlayer in BasePlayer.activePlayerList)
            {
                CheckPlayer(CurrentPlayer);
            }
            mySaveData();
        }


        void CheckPlayer(BasePlayer Player)
        {
            if (UserNames.ContainsKey(Player.displayName.ToString()))
            {
                if (Player.userID.ToString() != UserNames[Player.displayName.ToString()].ToString())
                {
                    long CurrentTimer = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                    Dictionary<string, object> User = (Dictionary<string, object>) Users[UserNames[Player.displayName.ToString()].ToString()];

                    if (CurrentTimer >= Convert.ToInt64(User["TIMER"].ToString()))
                    {
                        Users.Remove(UserNames[Player.displayName.ToString()].ToString());
                        UserNames.Remove(Player.displayName.ToString());

                        Dictionary<string, object> NewUser = new Dictionary<string, object>();
                        NewUser.Add("USERNAME", Player.displayName.ToString());
                        NewUser.Add("TIMER", (CurrentTimer + Convert.ToInt64((string)(((Dictionary<string, object>)Config["Settings"])["DeletionTime"]))).ToString());

                        if (Users.ContainsKey(Player.userID.ToString()))
                        {
                            Dictionary<string, object> OldUserName = (Dictionary<string, object>)Users[Player.userID.ToString()];
                            UserNames.Remove(OldUserName["USERNAME"].ToString());
                            Users.Remove(Player.userID.ToString());
                        }

                        Users.Add(Player.userID.ToString(), NewUser);
                        UserNames.Add(Player.displayName.ToString(), Player.userID.ToString());

                        logger.Write(LogType.Info, "UniqueName : User ({0}) replaced SteamID after {1} seconds.", Player.displayName.ToString(), Convert.ToInt64((string)(((Dictionary<string, object>)Config["Settings"])["DeletionTime"])).ToString());
                    }
                    else
                    {
                        try
                        {
                            KickList.Add(Player.userID.ToString(), (CurrentTimer + 30).ToString());
                        }
                        catch { }
                        logger.Write(LogType.Info, "UniqueName : User ({0}) registered under another SteamID kicking.", Player.displayName.ToString());
                    }
                }
                else
                {
                    long CurrentTimer = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                    Dictionary<string, object> User = (Dictionary<string, object>) Users[UserNames[Player.displayName.ToString()].ToString()];
                    User.Remove("TIMER");
                    User.Add("TIMER",(CurrentTimer + Convert.ToInt64((string)(((Dictionary<string, object>)Config["Settings"])["DeletionTime"]))).ToString());

                    logger.Write(LogType.Info, "UniqueName : User ({0}) extended.", Player.displayName.ToString());
                }
            }
            else
            {
                long CurrentTimer = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                Dictionary<string, object> NewUser = new Dictionary<string, object>();
                NewUser.Add("USERNAME", Player.displayName.ToString());
                NewUser.Add("TIMER", (CurrentTimer + Convert.ToInt64((string)(((Dictionary<string, object>)Config["Settings"])["DeletionTime"]))).ToString());
                if (Users.ContainsKey(Player.userID.ToString()))
                {
                    Dictionary<string, object> OldUserName = (Dictionary<string, object>)Users[Player.userID.ToString()];
                    UserNames.Remove(OldUserName["USERNAME"].ToString());
                    Users.Remove(Player.userID.ToString());
                }
                Users.Add(Player.userID.ToString(), NewUser);
                UserNames.Add(Player.displayName.ToString(), Player.userID.ToString());

                logger.Write(LogType.Info, "UniqueName : User ({0}) added.", Player.displayName.ToString());
            }
            mySaveData();
        }

        void mySaveData()
        {
            mySave["UserNames"] = UserNames;
            mySave["Users"] = Users;

            Interface.GetMod().DataFileSystem.SaveDatafile("UniqueName");
        }
        void OnTick()
        {
            if (KickList.Count > 0)
            {
                long CurrentTimer = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                List<string> DeleteFromKickList = new List<string>();
                foreach (var CurrentPlayer in KickList)
                {
                    if (CurrentTimer > Convert.ToInt64(CurrentPlayer.Value.ToString()))
                    {
                        BasePlayer CurrentPlayerObj = BasePlayer.FindByID(Convert.ToUInt64((String)CurrentPlayer.Key.ToString()));
                        if (CurrentPlayerObj != null)
                        {
                            logger.Write(Oxide.Core.Logging.LogType.Info, "UniqueName : {0} has been kicked.", CurrentPlayerObj.displayName.ToString());
                            Network.Net.sv.Kick(CurrentPlayerObj.net.connection, "This name reserved on this server.Please change your name to enter this server.");
                        }
                        DeleteFromKickList.Add(CurrentPlayer.Key.ToString());
                    }
                }
                foreach(string Current in DeleteFromKickList)
                {
                    KickList.Remove(Current);
                }
            }
        }
        [HookMethod("LoadDefaultConfig")]
        void myLoadDefaultConfig()
        {
            Dictionary<string, object> NewConfig = new Dictionary<string, object>();
            NewConfig.Add("DeletionTime", "604800");
            Config["Settings"] = NewConfig;
            logger.Write(Oxide.Core.Logging.LogType.Info, "UniqueName : Default Config loaded.");
        }
        [HookMethod("OnPlayerInit")]
        void OnPlayerInit(BasePlayer new_player)
        {
            try
            {
                KickList.Remove(new_player.userID.ToString());
            }
            catch { }
            CheckPlayer(new_player);
        }
    }
}
