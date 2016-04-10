using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("AirdropControl", "Reneb", "1.1.6")]
    class AirdropControl : RustPlugin
    {
        private FieldInfo CPstartPos;
        private FieldInfo CPendPos;
        private FieldInfo CPdropped;
        private FieldInfo CPsecondsToTake;
        private FieldInfo CPsecondsTaken;
        private FieldInfo dropPosition;
        private MethodInfo CreateEntity;
        private Vector3 centerPos;
        private float secondsToTake;
        private BaseEntity cargoplane;
        private Dictionary<CargoPlane, Vector3> dropPoint;
        private Dictionary<CargoPlane, int> dropNumber;
        private Dictionary<CargoPlane, double> nextDrop;
        private static readonly DateTime epoch = new DateTime(1970, 1, 1);
        private float dropMinX;
        private float dropMaxX;
        private float dropMinY;
        private float dropMaxY;
        private float dropMinZ;
        private float dropMaxZ;
        private string dropMessage;
        private int dropMinCrates;
        private int dropMaxCrates;
        private float airdropSpeed;
        private bool showDropLocation;
        private bool Changed;
        private System.Random getrandom;
        private object syncLock;
        private int minDropCratesInterval;
        private int maxDropCratesInterval;
        private double nextCheck;
        private List<CargoPlane> RemoveListND;
        private Dictionary<CargoPlane, int> RemoveListNUM;
        private Dictionary<CargoPlane,double> AddDrop;
        private Quaternion defaultRot = new Quaternion(1f,0f,0f,0f);

        void Loaded()
        {
            RemoveListND = new List<CargoPlane>();
            RemoveListNUM = new Dictionary<CargoPlane, int>();
            AddDrop = new Dictionary<CargoPlane, double>();
            getrandom = new System.Random();
            syncLock = new object();
            centerPos = new UnityEngine.Vector3(0f, 0f, 0f);
            dropPoint = new Dictionary<CargoPlane, Vector3>();
            dropNumber = new Dictionary<CargoPlane, int>();
            nextDrop = new Dictionary<CargoPlane, double>();
            dropPosition = typeof(CargoPlane).GetField("dropPosition", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            CPstartPos = typeof(CargoPlane).GetField("startPos", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            CPendPos = typeof(CargoPlane).GetField("endPos", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            CPdropped = typeof(CargoPlane).GetField("dropped", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            CPsecondsToTake = typeof(CargoPlane).GetField("secondsToTake", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            CPsecondsTaken = typeof(CargoPlane).GetField("secondsTaken", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            LoadVariables();
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
        void LoadVariables()
        {
            dropMinX = Convert.ToSingle(GetConfig("Drop", "MinX", -((World.Size/2) - 500)));
            dropMaxX = Convert.ToSingle(GetConfig("Drop", "MaxX", ((World.Size / 2) - 500)));
            dropMinZ = Convert.ToSingle(GetConfig("Drop", "MinZ", -((World.Size / 2) - 500)));
            dropMaxZ = Convert.ToSingle(GetConfig("Drop", "MaxZ", ((World.Size / 2) - 500)));
            dropMinY = Convert.ToSingle(GetConfig("Drop", "MinY", 200f));
            dropMaxY = Convert.ToSingle(GetConfig("Drop", "MaxY", 300f));
            dropMinCrates = Convert.ToInt32(GetConfig("Drop", "MinCrates", 1));
            dropMaxCrates = Convert.ToInt32(GetConfig("Drop", "MaxCrates", 3));
            minDropCratesInterval = Convert.ToInt32(GetConfig("Drop", "MinDropCratesInterval", 3));
            maxDropCratesInterval = Convert.ToInt32(GetConfig("Drop", "MaxDropCratesInterval", 10));
            showDropLocation = Convert.ToBoolean(GetConfig("Drop", "ShowDropLocation", true));
            airdropSpeed = Convert.ToSingle(GetConfig("Airdrop", "Speed", 40f));
            dropMessage = Convert.ToString(GetConfig("Messages","Inbound","Airdrop incoming! Dropping at {0} {1} {2}"));
            if (Changed)
            {
                SaveConfig();
                Changed = false;
            }
        }
        void LoadDefaultConfig()
        {
            Puts("Airdrop Control: Creating a new config file");
            Config.Clear(); // force clean new config
            LoadVariables();
        }
        int GetRandomNumber(int min, int max)
        {
            return getrandom.Next(min, max);
        }
        double CurrentTime()
        {
            return System.DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }
        Vector3 FindDropPoint(CargoPlane cargoplane)
        {
            return (Vector3)dropPosition.GetValue(cargoplane);
        }
        int RandomCrateDrop(CargoPlane cargoplane)
        {
            return GetRandomNumber(dropMinCrates, dropMaxCrates+1);
        }
        double RandomDropInterval()
        {
            return Convert.ToDouble(GetRandomNumber(minDropCratesInterval, maxDropCratesInterval + 1));
        }
        Vector3 RandomDropPoint()
        {
            var RandomX = Convert.ToSingle(GetRandomNumber((int)dropMinX, (int)dropMaxX+1));
            var RandomY = Convert.ToSingle(GetRandomNumber((int)dropMinY, (int)dropMaxY+1));
            var RandomZ = Convert.ToSingle(GetRandomNumber((int)dropMinZ, (int)dropMaxZ+1));
            if (RandomX == 0f)
                RandomX = 1f;
            if (RandomZ == 0f)
                RandomZ = 1f;
            return new UnityEngine.Vector3(RandomX, RandomY, RandomZ);
        }
        void FindStartAndEndPos(Vector3 target, out Vector3 startpos, out Vector3 endpos, out float distance)
        {
            var directionFromCenter = (target - centerPos).normalized;
            var directionAngles = Quaternion.LookRotation( directionFromCenter );
            var toRight = directionAngles * Vector3.right;
            var toLeft = directionAngles * Vector3.left;
            startpos = target;
            var multiplier = 1000f;
            var i = 0f;
            for(int o=0;o<50;o++)
            {
                var temPos = startpos + toRight * i * multiplier;
                if (((float)Math.Abs(temPos.x + multiplier) > (World.Size / 2)) || ((float)Math.Abs(temPos.x - multiplier) > (World.Size / 2)) || ((float)Math.Abs(temPos.z - multiplier) > (World.Size / 2)) || ((float)Math.Abs(temPos.z + multiplier) > (World.Size / 2)))
                {
                    multiplier = multiplier / 10f;
                    i = 0f;
                }
                else
                {
                    temPos.y = startpos.y;
                    startpos = temPos;
                    i = i + 1f;
                }
                if (multiplier < 1f)
                {
                    break;
                }
            }
            distance = Vector3.Distance(startpos, target);
            endpos = target - toRight * distance;
        }
        void BroadcastToChat(string msg)
        {
            ConsoleSystem.Broadcast("chat.add \"SERVER\" " + msg.QuoteSafe() + " 1.0", new object[0]);
        }
        void OnTick()
        {
            if (CurrentTime() >= nextCheck)
            {
                var currentTime = CurrentTime();
                if (nextDrop.Count > 0)
                {
                    foreach (KeyValuePair<CargoPlane, double> entry in nextDrop)
                    {
                        if (entry.Value >= currentTime)
                        {
                            CPdropped.SetValue(entry.Key, false);
                            RemoveListND.Add(entry.Key as CargoPlane);
                        }
                    }
                    foreach (CargoPlane cp in RemoveListND)
                    {
                        nextDrop.Remove(cp);
                    }
                    RemoveListND.Clear();
                }
                nextCheck = currentTime + 1;
            }
        }
        void CheckAirdropDrops()
        {
            foreach(KeyValuePair<CargoPlane, int> entry in dropNumber)
            {
                if((bool)CPdropped.GetValue(entry.Key))
                {
                    if(entry.Value > 1)
                    {
                        if (!(nextDrop.ContainsKey(entry.Key)))
                        {
                            AddDrop.Add(entry.Key, RandomDropInterval() + CurrentTime());
                            RemoveListNUM.Add(entry.Key as CargoPlane, entry.Value - 1);
                        }
                    }
                }
            }
            foreach (KeyValuePair<CargoPlane, double> entry in AddDrop)
            {
                nextDrop.Add(entry.Key, entry.Value);
            }
            AddDrop.Clear();
            foreach (KeyValuePair<CargoPlane, int> entry in RemoveListNUM)
            {
                if (entry.Value <= 0)
                    dropNumber.Remove(entry.Key);
                else
                    dropNumber[entry.Key] = entry.Value;
            }
            RemoveListNUM.Clear();
        }
        void OnEntitySpawned(BaseEntity entity)
        {
            if(entity != null)
            {
                if (entity is CargoPlane)
                {
                    var cargoplane = entity as CargoPlane;
                    Vector3 startPos;
                    Vector3 endPos;
                    float distance;
                    
                    var dropTarget = FindDropPoint(cargoplane);
                    if (showDropLocation)
                    {
                        BroadcastToChat(string.Format(dropMessage, dropTarget.x.ToString(), dropTarget.y.ToString(), dropTarget.z.ToString()));
                    }
                    Puts("Airdrop setting to drop at : " + dropTarget.ToString());
                    
                    
                    
                    dropNumber.Add(cargoplane, RandomCrateDrop(cargoplane));
                }
                else if(entity is SupplyDrop)
                {
                    CheckAirdropDrops();
                }
            }
        }
        void AllowNextDrop()
        {
            Interface.GetMod().CallHook("AllowDrop", new object[0] {});
        }
        [ConsoleCommand("airdrop.toplayer")]
        void cmdConsoleAirdropToPlayer(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, "You are not allowed to use this command");
                    return;
                }
            }
            if (arg.Args == null ||  arg.Args.Length < 1)
            {
                SendReply(arg, "You must select a player to check");
                return;
            }
            var target = BasePlayer.Find(arg.Args[0].ToString());
            if (target == null || target.net == null || target.net.connection == null)
            {
                SendReply(arg, "Target player not found");
            }
            else
            {
                AllowNextDrop();
                BaseEntity entity = GameManager.server.CreateEntity("assets/bundled/prefabs/events/cargo_plane.prefab", new Vector3(), defaultRot);
                if (entity != null)
                {
                    var targetPos = target.transform.position;
                    targetPos.y = Convert.ToSingle(GetRandomNumber((int)dropMinY, (int)dropMaxY + 1));
                    CargoPlane plane = entity.GetComponent<CargoPlane>();
                    plane.InitDropPosition(targetPos);
                    entity.Spawn(true);
                    
                    CPsecondsToTake.SetValue(plane, Vector3.Distance( (Vector3)CPendPos.GetValue(plane), (Vector3)CPstartPos.GetValue(plane) ) / airdropSpeed );
                }
            }
        }
        [ConsoleCommand("airdrop.topos")]
        void cmdConsoleAirdropToPos(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, "You are not allowed to use this command");
                    return;
                }
            }
            if (arg.Args == null || arg.Args.Length < 3)
            {
                SendReply(arg, "You must give coordinates of destination ex: airdrop.topos 124 200 -453");
                return;
            }
            AllowNextDrop();
            BaseEntity entity = GameManager.server.CreateEntity("assets/bundled/prefabs/events/cargo_plane.prefab", new Vector3(), defaultRot);
            if (entity != null)
            {
                var targetPos = new Vector3();
                targetPos.x = Convert.ToSingle(arg.Args[0]);
                targetPos.y = Convert.ToSingle(arg.Args[1]);
                targetPos.z = Convert.ToSingle(arg.Args[2]);
                CargoPlane plane = entity.GetComponent<CargoPlane>();
                plane.InitDropPosition(targetPos);
                entity.Spawn(true);
                
                CPsecondsToTake.SetValue(plane, Vector3.Distance( (Vector3)CPendPos.GetValue(plane), (Vector3)CPstartPos.GetValue(plane) ) / airdropSpeed );
            }
        }
        [ConsoleCommand("airdrop.massdrop")]
        void cmdConsoleAirdropMassDrop(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, "You are not allowed to use this command");
                    return;
                }
            }
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You must select the number of airdrops that you want");
                return;
            }
            for (int i = 0; i < Convert.ToInt32(arg.Args[0]); i++)
            {
                AllowNextDrop();
                Vector3 dropposition = RandomDropPoint();
                BaseEntity entity = GameManager.server.CreateEntity("assets/bundled/prefabs/events/cargo_plane.prefab", new Vector3(), defaultRot);
                if (entity != null)
                {
                    CargoPlane plane = entity.GetComponent<CargoPlane>();
                    plane.InitDropPosition( dropposition );
                    entity.Spawn(true);
                    CPsecondsToTake.SetValue(plane, Vector3.Distance( (Vector3)CPendPos.GetValue(plane), (Vector3)CPstartPos.GetValue(plane) ) / airdropSpeed );
                }
            }
        }
    }
}
