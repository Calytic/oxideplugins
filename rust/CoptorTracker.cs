using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
//using Oxide.Core;
//using Oxide.Core.Libraries;
//using Oxide.Core.Plugins;
//using Assembly-CSharp;

namespace Oxide.Plugins
{
    [Info("Chopper Tracker", "Smoosher", "1.6.4")]
    [Description("Tracking Of The Coptor")]



    class CoptorTracker : RustPlugin
    {

        DateTime TimerStart;
        int ChopperSpawnTime;
        float ChopperLifeTimeOriginal;
        float ChopperLifeTimeCurrent;
        DateTime TimerSpawn;
        DateTime ChopperSpawned;
        bool SpawnedHeli = false;

        #region Config
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["CoptorRespawnTimeInSeconds"] = 3600;
            Config["CoptorLifetimeInMins"] = 7.5;
        }

        private void SetConfig()
        {
            ChopperSpawnTime = Convert.ToInt32(Config["CoptorRespawnTimeInSeconds"]);
            ChopperLifeTimeOriginal = Convert.ToInt32(Config["CoptorLifetimeInMins"]);
        }


        private bool TrueorFalse(string input)
        {
            bool output;
            input = input.ToLower();
            switch (input)
            {
                case "true":
                    output = true;
                    return output;
                    break;

                case "false":
                    output = false;
                    return output;
                    break;

                default:
                    output = false;
                    return output;
                    break;
            }
        }
        #endregion

        #region OnLoad
        void Loaded()
        {
            SetConfig();
            if (!permission.PermissionExists("canCoptorControl")) permission.RegisterPermission("canCoptorControl", this);
            SetChopperLifetimeMins();
            StartChopperSpawnFreq();
        }
        #endregion

        #region ChatCommands

        [ChatCommand("Nextheli")]
        private void NextCoptor(BasePlayer player, string command, string[] args)
        {
            var TimeNow = DateTime.Now;

            TimeSpan t = TimerSpawn.Subtract(TimeNow);

            string TimeLeft = string.Format(string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
        t.Hours,
                    t.Minutes,
                    t.Seconds));

            SendReply(player, "Next Helicoptor will spawn in " + TimeLeft + "", "");
            int count = 0;
            string UpOrDown = "";
            BaseHelicopter[] allHelicopters = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>();
            foreach (BaseHelicopter helicopter in allHelicopters)
            {
                count++;
            }
            if (count > 0)
            {
                UpOrDown = "Spawned And Hunting";
            }
            else
            {
                UpOrDown = "Not Spawned";
            }
            SendReply(player, "The Helicoptor is currently " + UpOrDown + "", "");
            if (UpOrDown == "Spawned And Hunting")
            {
                var ChopLT = -ChopperLifeTimeCurrent;
                DateTime Duration = ChopperSpawned.AddMinutes(-ChopLT);
                TimeSpan l = Duration.Subtract(TimeNow);
                string DurationLeft = string.Format(string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
l.Hours,
        l.Minutes,
        l.Seconds));
                SendReply(player, "The Helicoptor Will Leave In " + DurationLeft + "", "");
            }
        }

        [ChatCommand("KillAllHelis")]
        private void KillHelis(BasePlayer player, string command, string[] args)
        {
            var perm = new Oxide.Core.Libraries.Permission();
            if (perm.UserHasPermission(player.userID.ToString(), "canCoptorControl"))
            {
                KillCoptor();
            }
            else
            {
                SendReply(player, "You Dont Have Permissions To Do This, Attempt Has Been Logged", "");
            }

        }

        [ChatCommand("SpawnHeli")]
        private void SpawnHeli(BasePlayer player, string command, string[] args)
        {
            var perm = new Oxide.Core.Libraries.Permission();
            if (perm.UserHasPermission(player.userID.ToString(), "canCoptorControl"))
            {
                SpawnChopper();
            }
            else
            {
                SendReply(player, "You Dont Have Permissions To Do This, Attempt Has Been Logged", "");
            }

        }

        #endregion

        #region ConsoleCommands

        #endregion

        #region functions

        private void StartChopperSpawnFreq()
        {
            TimerStart = DateTime.Now;
            TimerSpawn = TimerStart.AddSeconds(ChopperSpawnTime);
            timer.In(ChopperSpawnTime, () => SpawnChopper());
        }

        private void SetChopperLifetimeMins()
        {
            ConsoleSystem.Run.Server.Normal("heli.lifetimeminutes", new String[] { ChopperLifeTimeOriginal.ToString() });
        }

        private void SpawnChopper()
        {
            SpawnedHeli = true;
            SetChopperLifetimeMins();
            ChopperLifeTimeCurrent = ChopperLifeTimeOriginal;
            BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
            if (!(bool)((UnityEngine.Object)entity))
                return;

            ChopperSpawned = DateTime.Now;
            entity.Spawn(true);

            StartChopperSpawnFreq();
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {

            if (entity.name.Contains("patrolhelicopter.prefab"))
            {
                string thing = entity.LookupShortPrefabName();
                switch (thing)
                {
                    case "patrolhelicopter.prefab":
                        if (SpawnedHeli == true)
                        {
                            BaseHelicopter Chopper = (BaseHelicopter)entity;
                            ConsoleSystem.Broadcast("chat.add", 0, "<color=Red> [Coptor Tracker]</color>  Patrol Helicopter Has Spawned Look Out!!", 1);
                            SpawnedHeli = false;
                        }
                        else
                        {
                            KillCoptor(entity);
                        }
                        break;
                }

            }

        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            string Victim = "";
            if (entity.LookupShortPrefabName().ToString().Equals("patrolhelicopter.prefab"))
                Victim = "PatrolHeli";

            switch (Victim)
            {
                case "PatrolHeli":
                    var TimeNow = DateTime.Now;
                    var ChopLT = ChopperLifeTimeCurrent;
                    DateTime Duration = ChopperSpawned.AddMinutes(ChopLT);
                    TimeSpan l = Duration.Subtract(TimeNow);
                    if (l.Minutes <= 2)
                    {
                        ChopperLifeTimeCurrent = ChopperLifeTimeCurrent + 5;
                        ConsoleSystem.Run.Server.Normal("heli.lifetimeminutes", new String[] { ChopperLifeTimeCurrent.ToString() });
                        ConsoleSystem.Broadcast("chat.add", 0, "<color=Red> [Coptor Tracker]</color>  Helicopter Lifetime has been extended as has been engaged", 1);
                    }

                    break;
            }
        }

        private void KillCoptor(BaseNetworkable entity)
        {
            //ConsoleSystem.Broadcast("chat.add", 0, "<color=Red> [Coptor Tracker]</color>  Patrol Coptor Has been removed due to lack of something", 1);
            entity.Kill();
        }

        private void KillCoptor()
        {
            int coptors = 0;
            BaseHelicopter[] allHelicopters = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>();
            foreach (BaseHelicopter helicopter in allHelicopters)
            {
                helicopter.maxCratesToSpawn = 0;
                coptors++;
                helicopter.DieInstantly();
            }
            if (coptors > 0)
            {
                ConsoleSystem.Broadcast("chat.add", 0, "<color=Red> [Coptor Tracker]</color>  "+coptors.ToString()+" Helicopters Have Been Removed", 1);
            }
        }
    }
        #endregion

    }

