// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json
// Reference: RustBuild
// Reference: UnityEngine

/*
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015 Looking For Gamers, Inc. <support@lfgame.rs>
 * 
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
using System.IO;
using System.Collections.Generic;
using System.Timers;

// Rust Unity Namespaces
using Rust;
using UnityEngine;

//Oxide NameSpaces
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

//External NameSpaces
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using LFG;

namespace LFG
{
    public class SpawnPoint
    {
        public float x { set; get; }
        public float y { set; get; }
        public float z { set; get; }

        public SpawnPoint(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public bool isNotSet()
        {
            return this.x == 0.0 && this.y == 0.0 && this.z == 0.0;
        }

        public override string ToString()
        {
            return "x: " + this.x + "; y: " + this.y + "; z: " + this.z + ";";
        }
    }
}

namespace Oxide.Plugins
{
    [Info("Spawn", "Looking For Gamers <support@lfgame.rs>", "1.2.1", ResourceId = 818)]
    public class Spawn : RustPlugin
    {
        #region Other Classes
        public class ConfigObj
        {
            public int countdown { set; get; }
            public int safeDistance { set; get; }
            public string chatPrefix { set; get; }
            public string chatPrefixColor { set; get; }
            public Dictionary<string, List<string>> messages { set; get; }

            public ConfigObj()
            {
                this.messages = new Dictionary<string, List<string>> { };
                this.chatPrefix = "Spawn";
                this.chatPrefixColor = "blue";
            }

            public void addMessage(string key, List<string> message)
            {
                this.messages.Add(key, message);
            }

            public List<string> getMessage(string key, string[] args)
            {
                List<string> strings = new List<string>();
                List<string> messageList;

                if (this.messages.ContainsKey(key))
                {
                    messageList = (List<string>)this.messages[key];
                    foreach (string message in messageList)
                    {
                        strings.Add(string.Format(message, args));
                    }
                }

                return strings;
            }

        }
        #endregion

        public ConfigObj config;
        private string configPath;
        private bool loaded = false;
        private SpawnPoint spawnPoint;
        private string dataFile = "LFG-spawn";
        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();

        void SetupConfig()
        {
            if (this.loaded)
            {
                return;
            }

            LoadConfig();
            this.configPath = Manager.ConfigPath + string.Format("\\{0}.json", Name);
            this.config = JsonConvert.DeserializeObject<ConfigObj>((string)JsonConvert.SerializeObject(Config["Config"]).ToString());

            // This all seems 
            try
            {
                this.spawnPoint = Interface.GetMod().DataFileSystem.ReadObject<SpawnPoint>(this.dataFile);
            }
            catch (Exception e)
            {
                this.spawnPoint = new SpawnPoint(0.0F, 0.0F, 0.0F);
                this.SaveData();
            }

            if (this.spawnPoint == null)
            {
                this.spawnPoint = new SpawnPoint(0.0F, 0.0F, 0.0F);
            }

            this.loaded = true;
        }

        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject(this.dataFile, this.spawnPoint);
        }

        #region hook methods
        void Loaded()
        {
            this.SetupConfig();
            Print("Spawn by Looking For Gamers, has been started");
        }

        [HookMethod("LoadDefaultConfig")]
        void CreateDefaultConfig()
        {
            ConfigObj localConfig = new ConfigObj();

            localConfig.countdown = 10;
            localConfig.safeDistance = 0;

            localConfig.addMessage("welcome", new List<string>() { "Welcome to spawn!" });
            localConfig.addMessage("countdown", new List<string>() { "You will teleport to spawn in {0} seconds." });
            localConfig.addMessage("spawn", new List<string>() { "You have teleported to spawn." });
            localConfig.addMessage("setSpawn", new List<string>() { "You have set the server spawn point." });
            localConfig.addMessage("noSpawn", new List<string>() { "You have not set the server spawn point yet.", "Type /setspawn to set a spawn." });

            localConfig.addMessage("pendingTeleport", new List<string>() { "You already have a pending teleport." });
            localConfig.addMessage("canceledFromDamage", new List<string>() { "You've been hit! Your teleport has been canceled." });

            localConfig.addMessage("helpSpawn", new List<string>() { "Use /spawn to teleport to spawn." });
            localConfig.addMessage("helpSetSpawn", new List<string>() { "Use /setspawn to set the server spawn point." });
            
            this.config = localConfig;
            Config["Config"] = this.config;
            SaveConfig();

            this.SetupConfig();
        }

        void OnPlayerSpawn(BasePlayer player)
        {
            if (this.spawnPoint.isNotSet())
            {
                if (playerIsAdmin(player))
                {
                    this.chatMessage(player, config.getMessage("noSpawn", new string[] { }));
                }

                return;
            }

            if(playerSpawnedAtSleepingBag(player))
            {
                return;
            }

            this.doTeleport(null, null, player);
            this.chatMessage(player, config.getMessage("welcome", new string[] { }));
        }

        [HookMethod("OnEntityAttacked")]
        object OnEntityAttacked(MonoBehaviour entity, HitInfo hitinfo)
        {
            BasePlayer player = entity as BasePlayer;
            if (player != null)
            {
                if (closeToSpawn(player))
                {
                    return false;
                }

                if (this.timers.ContainsKey(player.userID.ToString()))
                {
                    this.timers[player.userID.ToString()].Stop();
                    this.timers.Remove(player.userID.ToString());
                    this.chatMessage(player, config.getMessage("canceledFromDamage", new string[] { }));
                }
            }

            return null;
        }

        void SendHelpText(BasePlayer player)
        {
            this.chatMessage(player, config.getMessage("helpSpawn", new string[] { }));
            if (playerIsAdmin(player))
            {
                this.chatMessage(player, config.getMessage("helpSetSpawn", new string[] { }));
            }
        }
        #endregion

        #region chat commands
        [ChatCommand("spawn")]
        void commandSpawn(BasePlayer player, string command, string[] args)
        {
            this.teleportToSpawn(player);
        }

        [ChatCommand("setspawn")]
        void commandSetSpawn(BasePlayer player, string command, string[] args)
        {
            if (!playerIsAdmin(player))
            {
                return;
            }

            Vector3 location = player.transform.position;
            this.spawnPoint = new SpawnPoint(location.x, location.y, location.z);
            this.SaveData();
            this.chatMessage(player, config.getMessage("setSpawn", new string[] { }));
        }

        #endregion

        #region console commands
        [ConsoleCommand("spawn.reload")]
        void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            this.loaded = false;
            this.SetupConfig();
            this.Print("Spawn Reloaded");
        }

        [ConsoleCommand("spawn.reset")]
        void cmdConsoleReset(ConsoleSystem.Arg arg)
        {
            this.loaded = false;
            this.CreateDefaultConfig();
            this.SetupConfig();
            this.Print("Spawn Reset");
        }

        [ConsoleCommand("Spawn.version")]
        void cmdConsoleVersion(ConsoleSystem.Arg arg)
        {
            this.Print(Version.ToString());
        }
        #endregion

        #region private helpers
        private void Print(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("{0}: {1}", Title, msg);
        }

        private void chatMessage(BasePlayer player, List<string> messages)
        {
            foreach (string message in messages)
            {
                player.ChatMessage(string.Format("<color={0}>{1}</color>: " + message, config.chatPrefixColor, config.chatPrefix));
            }
        }

        private bool playerIsAdmin(BasePlayer player)
        {
            return player.net.connection.authLevel == 2;
        }

        private bool closeToSpawn(BasePlayer player)
        {
            Vector3 location = player.transform.position;

            if (Math.Abs(location.x - this.spawnPoint.x) > this.config.safeDistance)
            {
                return false;
            }
            if (Math.Abs(location.y - this.spawnPoint.y) > this.config.safeDistance)
            {
                return false;
            }
            if (Math.Abs(location.z - this.spawnPoint.z) > this.config.safeDistance)
            {
                return false;
            }

            return true;
        }

        private bool playerSpawnedAtSleepingBag(BasePlayer player)
        {
            SleepingBag[] sleepingBags = SleepingBag.FindForPlayer(player.userID, false);
            foreach (SleepingBag sleepingBag in sleepingBags)
            {
                if (sleepingBag.transform.position.Equals(player.transform.position))
                {
                    return true;
                }
            }

            return false;
        }

        private void teleportToSpawn(BasePlayer player)
        {
            if (this.timers.ContainsKey(player.userID.ToString()))
            {
                this.chatMessage(player, config.getMessage("pendingTeleport", new string[] { }));
                return;
            }

            this.chatMessage(player, config.getMessage("countdown", new string[] { this.config.countdown.ToString() }));
            Timer timer = new Timer();
            timer.Interval = this.config.countdown * 1000;
            timer.Elapsed += (timerSender, timerEvent) => doTeleport(timerSender, timerEvent, player);
            timer.Start();
            this.timers[player.userID.ToString()] = timer;
        }

        public void doTeleport(object source, ElapsedEventArgs e, BasePlayer player)
        {
            player.StartSleeping();

            player.transform.position = new UnityEngine.Vector3(this.spawnPoint.x, this.spawnPoint.y, this.spawnPoint.z);
            player.ClientRPC(null, player, "ForcePositionTo", new object[] { player.transform.position });
            player.TransformChanged();
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendFullSnapshot();

            if (this.timers.ContainsKey(player.userID.ToString()))
            {
                this.timers[player.userID.ToString()].Stop();
                this.chatMessage(player, config.getMessage("spawn", new string[] { this.config.countdown.ToString() }));
            }
        }
        #endregion
    }
}
