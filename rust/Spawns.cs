using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Spawns", "Reneb / k1lly0u", "2.0.22", ResourceId = 720)]
    class Spawns : RustPlugin
    {
        #region Fields
        SpawnsData spawnsData;
        private DynamicConfigFile data;

        Dictionary<ulong, List<Vector3>> SpawnCreation;
        Dictionary<string, List<Vector3>> LoadedSpawnfiles;

        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("SpawnsDatabase/spawns_data");
            SpawnCreation = new Dictionary<ulong, List<Vector3>>();
            LoadedSpawnfiles = new Dictionary<string, List<Vector3>>();
        }
        void OnServerInitialized()
        {
            LoadData();
            VerifySpawnfiles();
        }
        #endregion

        #region Functions
        void VerifySpawnfiles()
        {
            bool hasChanged = false;
            for (int i = 0; i < spawnsData.Spawnfiles.Count; i++)
            {
                var name = spawnsData.Spawnfiles[i];
                if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"SpawnsDatabase/{name}"))
                {
                    spawnsData.Spawnfiles.Remove(name);
                    hasChanged = true;
                } 
                else 
                {                    
                    if (LoadSpawns(name) != null)
                    {
                        spawnsData.Spawnfiles.Remove(name);
                        hasChanged = true;
                    }
                    else if (LoadedSpawnfiles[name].Count == 0)
                    {
                        spawnsData.Spawnfiles.Remove(name);
                        hasChanged = true;
                    }
                }               
            }
            if (hasChanged) SaveData();
        }
        int GetRandomNumber(int min, int max) => UnityEngine.Random.Range(min, max);
        
        object LoadSpawns(string name)
        {
            if (string.IsNullOrEmpty(name))
                return MSG("noFile");

            if (!LoadedSpawnfiles.ContainsKey(name))
            {                
                object success = LoadSpawnFile(name);
                if (success == null)                
                    return MSG("noFile");
                
                else LoadedSpawnfiles.Add(name, (List<Vector3>)success);
            }
            return null;
        }
        #endregion

        #region Hooks

        object GetSpawnsCount(string filename)
        {
            object success = LoadSpawns(filename);
            if (success != null) return (string)success;
            return LoadedSpawnfiles[filename].Count;
        }
        object GetRandomSpawn(string filename)
        {
            object success = LoadSpawns(filename);
            if (success != null) return (string)success;
            return LoadedSpawnfiles[filename][GetRandomNumber(0, LoadedSpawnfiles[filename].Count - 1)];
        }
        object GetRandomSpawnRange(string filename, int min, int max)
        {
            object success = LoadSpawns(filename);
            if (success != null) return (string)success;
            if (min < 0) min = 0;
            if (max > LoadedSpawnfiles[filename].Count - 1) max = LoadedSpawnfiles[filename].Count - 1;
            return LoadedSpawnfiles[filename][GetRandomNumber(min, max)];
        }
        object GetSpawn(string filename, int number)
        {
            object success = LoadSpawns(filename);
            if (success != null) return (string)success;
            if (number < 0) number = 0;
            if (number > LoadedSpawnfiles[filename].Count - 1) number = LoadedSpawnfiles[filename].Count - 1;
            return LoadedSpawnfiles[filename][number];
        }
        string[] GetSpawnfileNames() => spawnsData.Spawnfiles.ToArray();
        #endregion

        #region Chat Commands
        [ChatCommand("spawns")]
        void cmdSpawns(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (args == null || args.Length == 0)
            {
                SendHelp(player);
                return;
            }
            if (args.Length >= 1)
            {
                switch (args[0].ToLower())
                {
                    case "new":
                        if (isCreating(player))
                        {
                            SendReply(player, MSG("alreadyCreating", player.UserIDString));
                            return;
                        }
                        SpawnCreation.Add(player.userID, new List<Vector3>());
                        SendReply(player, MSG("newCreating", player.UserIDString));
                        return;

                    case "open":
                        if (args.Length >= 2)
                        {
                            if (isCreating(player))
                            {
                                SendReply(player, MSG("isCreating", player.UserIDString));
                                return;
                            }
                            var spawns = LoadSpawnFile(args[1]);
                            if (spawns != null)
                            {
                                SpawnCreation.Add(player.userID, (List<Vector3>)spawns);
                                SendReply(player, string.Format(MSG("opened", player.UserIDString), SpawnCreation[player.userID].Count));
                                return;
                            }
                            else
                            {
                                SendReply(player, MSG("invalidFile", player.UserIDString));
                                return;
                            }
                        }
                        else SendReply(player, MSG("fileName", player.UserIDString));
                        return;

                    case "add":
                        if (!isCreating(player))
                        {
                            SendReply(player, MSG("notCreating", player.UserIDString));
                            return;
                        }
                        else
                        {                            
                            SpawnCreation[player.userID].Add(player.transform.position);
                            var number = SpawnCreation[player.userID].Count;
                            ShowSpawnPoint(player, SpawnCreation[player.userID][number - 1], number.ToString());
                            SendReply(player, string.Format("Added Spawn nÂ°{0}", SpawnCreation[player.userID].Count));
                        }
                        return;

                    case "remove":
                        if (args.Length >= 2)
                        {
                            if (!isCreating(player))
                            {
                                SendReply(player, MSG("notCreating", player.UserIDString));
                                return;
                            }
                            if (SpawnCreation[player.userID].Count > 0)
                            {
                                int number;
                                if (int.TryParse(args[1], out number))
                                {
                                    number = int.Parse(args[1]);
                                    if (number < SpawnCreation[player.userID].Count)
                                    {
                                        SpawnCreation[player.userID].RemoveAt(number - 1);
                                        SendReply(player, string.Format(MSG("remSuccess", player.UserIDString), number));
                                        return;
                                    }
                                    SendReply(player, MSG("nexistNum", player.UserIDString));
                                    return;
                                }
                                SendReply(player, MSG("noNum", player.UserIDString));
                                return;
                            }
                            SendReply(player, MSG("noSpawnpoints", player.UserIDString));
                            return;
                        }
                        else SendReply(player, "/spawns remove <number>");
                        return;

                    case "save":
                        if (args.Length >= 2)
                        {
                            if (!isCreating(player))
                            {
                                SendReply(player, MSG("noCreate", player.UserIDString));
                                return;
                            }
                            if (SpawnCreation[player.userID].Count > 0)
                            {
                                if (!spawnsData.Spawnfiles.Contains(args[1]) && !LoadedSpawnfiles.ContainsKey(args[1]))
                                {
                                    SaveSpawnFile(player, args[1]);
                                    return;                                    
                                }
                                SendReply(player, MSG("spawnfileExists", player.UserIDString));
                                return;
                            }
                            SendReply(player, MSG("noSpawnpoints", player.UserIDString));
                            return;
                        }
                        else SendReply(player, "/spawns save <filename>");
                        return;

                    case "close":
                        if (!isCreating(player))
                        {
                            SendReply(player, MSG("noCreate", player.UserIDString));
                            return;
                        }
                        SpawnCreation.Remove(player.userID);
                        SendReply(player, MSG("noSave", player.UserIDString));
                        return;

                    case "show":
                        if (!isCreating(player))
                        {
                            SendReply(player, MSG("notCreating", player.UserIDString));
                            return;
                        }
                        if (SpawnCreation[player.userID].Count > 0)
                        {
                            int i = 1;
                            foreach (var point in SpawnCreation[player.userID])
                            {
                                ShowSpawnPoint(player, point, i.ToString());
                                i++;
                            }
                            return;
                        }
                        else SendReply(player, MSG("noSp", player.UserIDString));
                        return;

                    default:
                        SendHelp(player);
                        break;
                }
            }
        }
        void ShowSpawnPoint(BasePlayer player, Vector3 point, string name, float time = 10f)
        {
            player.SendConsoleCommand("ddraw.text", 10f, Color.green, point + new Vector3(0, 1.5f, 0), $"<size=40>{name}</size>");
            player.SendConsoleCommand("ddraw.box", 10f, Color.green, point, 1f);
        }
        void SendHelp(BasePlayer player)
        {
            SendReply(player, MSG("newSyn", player.UserIDString));
            SendReply(player, MSG("openSyn", player.UserIDString));
            SendReply(player, MSG("addSyn", player.UserIDString));
            SendReply(player, MSG("remSyn", player.UserIDString));
            SendReply(player, MSG("saveSyn", player.UserIDString));
            SendReply(player, MSG("closeSyn", player.UserIDString));
            SendReply(player, MSG("showSyn", player.UserIDString));
        }
        bool isCreating(BasePlayer player)
        {
            if (SpawnCreation.ContainsKey(player.userID)) return true;
            return false;
        }
        bool hasAccess(BasePlayer player)
        {
            if (player.net.connection.authLevel < 1)
            {
                SendReply(player, MSG("noAccess", player.UserIDString));
                return false;
            }
            return true;
        }
        #endregion

        #region Data Management
        void SaveData() => data.WriteObject(spawnsData);
        void LoadData()
        {
            try
            {
                spawnsData = data.ReadObject<SpawnsData>();
            }
            catch
            {
                spawnsData = new SpawnsData();
            }
        }
        void SaveSpawnFile(BasePlayer player, string name)
        {
            var NewSpawnFile = Interface.Oxide.DataFileSystem.GetFile($"SpawnsDatabase/{name}");
            NewSpawnFile.Clear();
            NewSpawnFile.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter() };
            var spawnFile = new Spawnfile();

            int i = 1;
            foreach (Vector3 spawnpoint in SpawnCreation[player.userID])
            {
                spawnFile.spawnPoints.Add(i.ToString(), spawnpoint);
                i++;
            }
            NewSpawnFile.WriteObject(spawnFile);

            if (!spawnsData.Spawnfiles.Contains(name))
                spawnsData.Spawnfiles.Add(name);
            if (!LoadedSpawnfiles.ContainsKey(name))
                LoadedSpawnfiles.Add(name, SpawnCreation[player.userID]);
            SaveData();

            SendReply(player, string.Format(MSG("saved", player.UserIDString), SpawnCreation[player.userID].Count, name));
            SpawnCreation.Remove(player.userID);
        }
        object LoadSpawnFile(string name)
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"SpawnsDatabase/{name}"))
                return null;
            var sfile = Interface.GetMod().DataFileSystem.GetDatafile($"SpawnsDatabase/{name}");
            sfile.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter() };

            var spawnFile = new Spawnfile();
            spawnFile = sfile.ReadObject<Spawnfile>();
            var spawnList = spawnFile.spawnPoints.Values.ToList();
            if (spawnList.Count < 1)
                return null;
            return spawnList;
        }
        class SpawnsData
        {
            public List<string> Spawnfiles = new List<string>();
        }
        class Spawnfile
        {
            public Dictionary<string, Vector3> spawnPoints = new Dictionary<string, Vector3>();
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
        }
        #endregion

        #region Messaging
        private string MSG(string key, string ID = null) => lang.GetMessage(key, this, ID);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"noFile", "This file doesn't exist" },
            {"alreadyCreating", "You are already creating a spawn file" },
            {"newCreating", "You now creating a new spawn file" },
            {"isCreating", "You must save/close your current spawn file first. Type /spawns for more information" },
            {"opened", "Opened spawnfile with {0} spawns" },
            {"invalidFile", "This spawnfile is empty or not valid" },
            {"fileName", "You must enter a filename" },
            {"notCreating", "You must create/open a new Spawn file first /spawns for more information" },
            {"remSuccess", "Successfully removed spawn nÂ°{0}" },
            {"nexistNum", "This spawn number doesn't exist" },
            {"noNum", "You must enter a spawn point number" },
            {"noSpawnpoints", "You haven't set any spawn points yet" },
            {"noCreate", "You must create a new Spawn file first. Type /spawns for more information" },
            {"noSave", "Spawn file closed without saving" },
            {"noSp", "You must add spawnpoints first" },
            {"newSyn", "/spawns new - Create a new spawn file" },
            {"openSyn", "/spawns open - Open a existing spawn file for editing" },
            {"addSyn", "/spawns add - Add a new spawn point" },
            {"remSyn", "/spawns remove <number> - Remove a spawn point" },
            {"saveSyn", "/spawns save <filename> - Saves your spawn file" },
            {"closeSyn", "/spawns close - Cancel spawn file creation" },
            {"showSyn", "/spawns show - Display a box at each spawnpoint" },
            {"noAccess", "You are not allowed to use this command" },
            {"saved", "{0} spawnpoints saved into {1}" },
            {"spawnfileExists", "A spawn file with that name already exists" }
        };
        #endregion
    }
}
