// Reference: RustBuild

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;


namespace Oxide.Plugins
{
    [Info("Waypoints", "Reneb", "1.1.1")]
    class Waypoints : RustPlugin
    {

        void Loaded()
        {
            LoadData();
        }

        private DynamicConfigFile data;
        private Dictionary<string, Waypoint> waypoints;
        void SaveData()
        {
            data.WriteObject(waypoints);
        }
        void LoadData()
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.GetFile(nameof(Waypoints));
                data.Settings.Converters = new JsonConverter[] {new UnityVector3Converter()};
                waypoints = data.ReadObject<Dictionary<string, Waypoint>>();
                waypoints = waypoints.ToDictionary(w => w.Key.ToLower(), w => w.Value);
            }
            catch
            {
                waypoints = new Dictionary<string, Waypoint>();
            }
        }

        class WaypointInfo
        {
            [JsonProperty("p")]
            public Vector3 Position;
            [JsonProperty("s")]
            public float Speed;

            public WaypointInfo(Vector3 position, float speed)
            {
                Position = position;
                Speed = speed;
            }
        }

        class Waypoint
        {
            public string Name;
            public List<WaypointInfo> Waypoints;

            public Waypoint()
            {
                Waypoints = new List<WaypointInfo>();
            }
            public void AddWaypoint(Vector3 position, float speed)
            {
                Waypoints.Add(new WaypointInfo(position, speed));
            }
        }


        class WaypointEditor : MonoBehaviour
        {
            public Waypoint targetWaypoint;

            void Awake()
            {
            }
        }
        [HookMethod("GetWaypointsList")]
        object GetWaypointsList(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Waypoint waypoint;
            if (!waypoints.TryGetValue(name.ToLower(), out waypoint)) return null;
            var returndic = new List<object>();

            foreach(var wp in waypoint.Waypoints)
            {
                returndic.Add(new Dictionary<Vector3, float> { { wp.Position, wp.Speed } });
            }
            return returndic;
        }

        bool hasAccess(BasePlayer player)
        {
            if (player.net.connection.authLevel < 1) { SendReply(player, "You don't have access to this command"); return false; }
            return true;
        }
         bool isEditingWP(BasePlayer player, int ttype)
        {
            if (player.GetComponent<WaypointEditor>() != null)
            {
                if (ttype == 0) SendReply(player, string.Format("You are already editing {0}", player.GetComponent<WaypointEditor>().targetWaypoint.Name));
                return true;
            }
            else
            {
                if (ttype == 1) SendReply(player, "You are not editing any waypoints, say /waypoints_new or /waypoints_edit NAME");
                return false;
            }
        }
        //////////////////////////////////////////////////////
        // Waypoints manager
        //////////////////////////////////////////////////////

        [ChatCommand("waypoints_new")]
        void cmdWaypointsNew(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (isEditingWP(player, 0)) return;

            var newWaypointEditor = player.gameObject.AddComponent<WaypointEditor>();
            newWaypointEditor.targetWaypoint = new Waypoint();
            SendReply(player, "Waypoints: New WaypointList created, you may now add waypoints.");
        }
        [ChatCommand("waypoints_add")]
        void cmdWaypointsAdd(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (!isEditingWP(player, 1)) return;
            var WaypointEditor = player.GetComponent<WaypointEditor>();
            if (WaypointEditor.targetWaypoint == null)
            {
                SendReply(player, "Waypoints: Something went wrong while getting your WaypointList");
                return;
            }
            float speed = 3f;
            if (args.Length > 0) float.TryParse(args[0], out speed);
            WaypointEditor.targetWaypoint.AddWaypoint(player.transform.position, speed);

            SendReply(player, string.Format("Waypoint Added: {0} {1} {2} - Speed: {3}", player.transform.position.x, player.transform.position.y, player.transform.position.z, speed));
        }
        [ChatCommand("waypoints_list")]
        void cmdWaypointsList(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (waypoints.Count == 0)
            {
                SendReply(player, "No waypoints created yet");
                return;
            }
            SendReply(player, "==== Waypoints ====");
            foreach (var pair in waypoints)
            {
                SendReply(player, pair.Key);
            }
        }
        [ChatCommand("waypoints_remove")]
        void cmdWaypointsRemove(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (waypoints.Count == 0)
            {
                SendReply(player, "No waypoints created yet");
                return;
            }
            if(args.Length == 0)
            {
                SendReply(player, "/waypoints_list to get the list of waypoints");
                return;
            }
            if (!waypoints.Remove(args[0]))
            {
                SendReply(player, "Waypoint "+ args[0]+ " doesn't exist");
                return;
            }
            SaveData();
            SendReply(player, string.Format("Waypoints: {0} was removed",args[0]));
        }
        [ChatCommand("waypoints_save")]
        void cmdWaypointsSave(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (!isEditingWP(player, 1)) return;
            if (args.Length == 0)
            {
                SendReply(player, "Waypoints: /waypoints_save NAMEOFWAYPOINT");
                return;
            }
            var WaypointEditor = player.GetComponent<WaypointEditor>();
            if (WaypointEditor.targetWaypoint == null)
            {
                SendReply(player, "Waypoints: Something went wrong while getting your WaypointList");
                return;
            }

            var name = args[0];
            WaypointEditor.targetWaypoint.Name = name;
            waypoints[name.ToLower()] = WaypointEditor.targetWaypoint;
            SendReply(player, string.Format("Waypoints: New waypoint saved with: {0} with {1} waypoints stored", WaypointEditor.targetWaypoint.Name, WaypointEditor.targetWaypoint.Waypoints.Count));
            UnityEngine.Object.Destroy(player.GetComponent<WaypointEditor>());
            SaveData();
        }
        [ChatCommand("waypoints_close")]
        void cmdWaypointsClose(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (!isEditingWP(player, 1)) return;
            SendReply(player, "Waypoints: Closed without saving");
            UnityEngine.Object.Destroy(player.GetComponent<WaypointEditor>());
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
    }
}
