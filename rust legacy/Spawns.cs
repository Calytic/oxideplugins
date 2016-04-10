using System.Collections.Generic;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;


namespace Oxide.Plugins
{
    [Info("Spawns Database", "Reneb", 1.0)]
    class Spawns : RustLegacyPlugin
    {
        Dictionary<NetUser, object> SpawnsData;
        Dictionary<string, object> SpawnsfileData;
        private int currentrandom;
        private System.Random getrandom;
        void Loaded()
        {
            SpawnsData = new Dictionary<NetUser, object>();
            getrandom = new System.Random();
            SpawnsfileData = new Dictionary<string, object>();
        }
        void OnServerInitialized()
        {
        }
        bool hasAccess(NetUser player)
        {
            if (!player.CanAdmin())
            {
                SendReply(player, "You are not allowed to use this command");
                return false;
            }
            return true;
        }
        object GetSpawnsCount(string filename)
        {
            if (!(SpawnsfileData.ContainsKey(filename)))
            {
                object success = loadSpawnfile(filename);
                if (success is string)
                {
                    return (string)success;
                }
            }
            return ((List<Vector3>)SpawnsfileData[filename]).Count;
        }
        int GetRandomNumber(int min, int max)
        {
            return getrandom.Next(min, max);
        }
        object GetRandomSpawn(string filename)
        {
            if (!(SpawnsfileData.ContainsKey(filename)))
            {
                object success = loadSpawnfile(filename);
                if (success is string)
                {
                    return (string)success;
                }
            }
            return ((List<Vector3>)SpawnsfileData[filename])[GetRandomNumber(0, ((List<Vector3>)SpawnsfileData[filename]).Count)];
        }

        object GetRandomSpawnRange(string filename, int min, int max)
        {
            if (!(SpawnsfileData.ContainsKey(filename)))
            {
                object success = loadSpawnfile(filename);
                if (success is string)
                {
                    return (string)success;
                }
            }
            if (min < 0) min = 0;
            if (max > ((List<Vector3>)SpawnsfileData[filename]).Count) max = ((List<Vector3>)SpawnsfileData[filename]).Count;
            return ((List<Vector3>)SpawnsfileData[filename])[GetRandomNumber(min, max)];
        }
        object GetSpawn(string filename, int number)
        {
            if (!(SpawnsfileData.ContainsKey(filename)))
            {
                object success = loadSpawnfile(filename);
                if (success is string)
                {
                    return (string)success;
                }
            }
            if (number < 0) number = 0;
            if (number > ((List<Vector3>)SpawnsfileData[filename]).Count) number = ((List<Vector3>)SpawnsfileData[filename]).Count;
            return ((List<Vector3>)SpawnsfileData[filename])[number];
        }
        object loadSpawnfile(string filename)
        {
            if (SpawnsfileData.ContainsKey(filename))
                SpawnsfileData.Remove(filename);
            var loadFile = Interface.GetMod().DataFileSystem.GetDatafile(filename);
            if (loadFile["1"] == null)
            {
                return "This file doesn't exist";
            }
            SpawnsfileData.Add(filename, new List<Vector3>());
            foreach (KeyValuePair<string, object> pair in loadFile)
            {
                var currentvalue = pair.Value as Dictionary<string, object>;
                ((List<Vector3>)SpawnsfileData[filename]).Add(new Vector3(Convert.ToInt32(currentvalue["x"]), Convert.ToInt32(currentvalue["y"]), Convert.ToInt32(currentvalue["z"])));
            }
            return true;
        }
        [ChatCommand("spawns_new")]
        void cmdSpawnsNew(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (SpawnsData.ContainsKey(player))
            {
                SendReply(player, "You are already creating a spawn file");
                return;
            }
            SpawnsData.Add(player, new List<Vector3>());
            SendReply(player, "You now creating a new spawn file");
        }
        [ChatCommand("spawns_open")]
        void cmdSpawnOpen(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (SpawnsData.ContainsKey(player))
            {
                SendReply(player, "You must save/close your current spawns first. /spawns_help for more informations");
                return;
            }
            if (args == null || args.Length == 0)
            {
                SendReply(player, "/spawns_remove SPAWN_NUMBER");
                return;
            }
            var NewSpawnFile = Interface.GetMod().DataFileSystem.GetDatafile(args[0].ToString());
            if (NewSpawnFile["1"] == null)
            {
                SendReply(player, "This spawnfile is empty or not valid");
                return;
            }
            SpawnsData.Add(player, new List<Vector3>());
            foreach (KeyValuePair<string, object> pair in NewSpawnFile)
            {
                var currentvalue = pair.Value as Dictionary<string, object>;
                ((List<Vector3>)SpawnsData[player]).Add(new Vector3(Convert.ToInt32(currentvalue["x"]), Convert.ToInt32(currentvalue["y"]), Convert.ToInt32(currentvalue["z"])));
            }
            SendReply(player, string.Format("Opened spawnfile with {0} spawns", ((List<Vector3>)SpawnsData[player]).Count.ToString()));
        }
        [ChatCommand("spawns_add")]
        void cmdSpawnAdd(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (!(SpawnsData.ContainsKey(player)))
            {
                SendReply(player, "You must create/open a new Spawn file first /spawns_help for more informations");
                return;
            }
            ((List<Vector3>)SpawnsData[player]).Add(player.playerClient.lastKnownPosition);
            SendReply(player, string.Format("Added Spawn n°{0}", ((List<Vector3>)SpawnsData[player]).Count));
        }
        [ChatCommand("spawns_remove")]
        void cmdSpawnsRemove(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (!(SpawnsData.ContainsKey(player)))
            {
                SendReply(player, "You must create/open a new Spawn file first /spawns_help for more informations");
                return;
            }
            if (args == null || args.Length == 0)
            {
                SendReply(player, "/spawns_remove SPAWN_NUMBER");
                return;
            }
            int result;
            if (!int.TryParse(args[0], out result))
            {
                SendReply(player, "/spawns_remove SPAWN_NUMBER");
                return;
            }
            result = Convert.ToInt32(args[0]);
            if (((List<Vector3>)SpawnsData[player]).Count == 0)
            {
                SendReply(player, "You didn't set any spawns yet");
                return;
            }
            if (result > ((List<Vector3>)SpawnsData[player]).Count)
            {
                SendReply(player, "This spawns number doesn't exist");
                return;
            }
            ((List<Vector3>)SpawnsData[player]).RemoveAt(result);
            SendReply(player, string.Format("Successfully removed Spawn n°{0}", result.ToString()));
        }
        [ChatCommand("spawns_save")]
        void cmdSpawnsSave(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (!(SpawnsData.ContainsKey(player)))
            {
                SendReply(player, "You must create a new Spawn file first /spawns_help for more informations");
                return;
            }
            if (args == null || args.Length == 0)
            {
                SendReply(player, "/spawns_save FILENAME");
                return;
            }
            if (((List<Vector3>)SpawnsData[player]).Count == 0)
            {
                SendReply(player, "You didn't set any spawns yet");
                return;
            }
            var NewSpawnFile = Interface.GetMod().DataFileSystem.GetDatafile(args[0].ToString());
            NewSpawnFile.Clear();
            int i = 1;
            foreach (Vector3 spawnpoint in (List<Vector3>)SpawnsData[player])
            {
                var spawnpointadd = new Dictionary<string, object>();
                spawnpointadd.Add("x", Math.Round(spawnpoint.x * 100) / 100);
                spawnpointadd.Add("y", Math.Round(spawnpoint.y * 100) / 100);
                spawnpointadd.Add("z", Math.Round(spawnpoint.z * 100) / 100);
                NewSpawnFile[i.ToString()] = spawnpointadd;
                i++;
            }
            Interface.GetMod().DataFileSystem.SaveDatafile(args[0].ToString());
            SendReply(player, string.Format("{0} spawnpoints saved into {1}", ((List<Vector3>)SpawnsData[player]).Count.ToString(), args[0].ToString()));
            SpawnsData.Remove(player);
        }
        [ChatCommand("spawns_close")]
        void cmdSpawnsClose(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (!(SpawnsData.ContainsKey(player)))
            {
                SendReply(player, "You must create a new Spawn file first /spawns_help for more informations");
                return;
            }
            SpawnsData.Remove(player);
            SendReply(player, "Spawns file closed without saving");
        }
        [ChatCommand("spawns_help")]
        void cmdSpawnshelp(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            SendReply(player, "Start by making a new data with: /spawns_new");
            SendReply(player, "Add new spawn points where you are standing with /spawns_add");
            SendReply(player, "Remove a spawn point that you didn't like with /spawns_remove NUMBER");
            SendReply(player, "Save the spawn points into a file with: /spawns_save FILENAME");
            SendReply(player, "Use /spawns_open later on to open it back and edit it");
            SendReply(player, "Use /spawns_close to stop setting points without saving");
        }
    }
}
