using System;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Collider Count", "Cheeze www.ukwasteland.co.uk", "0.0.3", ResourceId = 1306)]
    class ColliderCount : RustPlugin
    {

        private const string ChatPrefix = "Server Status";
        private const string ChatPrefixColor = "#ffa500ff";

        private readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private DateTime wipeDate;

        IFormatProvider culture = new System.Globalization.CultureInfo("en-GB", true);

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["Settings", "LastWipe"] = "17.8.2015";
            Config["Settings", "MaxColliders"] = 270000;
            Config["Settings", "Color1"] = "<color=#ff0000ff>";
            Config["Settings", "Color2"] = "<color=#008000ff>";
            SaveConfig();
        }
        void Loaded()
        {
            var date = Config["Settings", "LastWipe"].ToString();
            wipeDate = DateTime.Parse(date, culture, System.Globalization.DateTimeStyles.AssumeLocal);
        }

        [ChatCommand("wipeinfo")]
        private void WipeInfoChat(BasePlayer player, string command, string[] args)
        {
            SendMessage(player, $"{GetColor1()}There are currently</color>{GetColor2()} {GetColliderCount()}</color> {GetColor1()}colliders on the server out of a max of</color>{GetColor2()} {GetMaxColliders()}</color>");
            SendMessage(player, GetTimeToWipe());
        }

        [ConsoleCommand("wipeinfo")]
        private void WipeInfoConsole(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                string NotAllowed = null;
                arg.ReplyWith(NotAllowed);
                return;
            }
            PrintToChat($"{GetColor1()}There are currently</color>{GetColor2()} {GetColliderCount()}</color> {GetColor1()}colliders on the server out of a max of</color>{GetColor2()} {GetMaxColliders()}</color>");
            PrintToChat(GetTimeToWipe());
        }

        private int GetColliderCount()
        {
            var colliders = UnityEngine.Object.FindObjectsOfType<Collider>().Count(x => x.enabled);
            return colliders;
        }

        
        private int GetMaxColliders()
        {
            var maxColliders = int.Parse(Config["Settings", "MaxColliders"].ToString());
            return maxColliders;
        }

        private string GetColor1()
        {   
           string color1 = (Config["Settings", "Color1"].ToString());
           return color1;        
        }

        private string GetColor2()
        {
            string color2 = (Config["Settings", "Color2"].ToString());
            return color2;
        }

        private string GetTimeToWipe()
        {
            var days = (int)Math.Floor((DateTime.UtcNow - wipeDate).TotalDays);
            if (days > 0)
            {
                var mapSize = TerrainMeta.Size.x;
                var initialColliders = (int)(((mapSize * mapSize) / 1000000) * 1500);
                var colliders = UnityEngine.Object.FindObjectsOfType<Collider>().Count(x => x.enabled);
                var postEnts = (colliders - initialColliders);
                var entsDaily = (postEnts / days);
                var maxColliders = int.Parse(Config["Settings", "MaxColliders"].ToString());
                var timetowipe = (maxColliders - colliders) / entsDaily;
                var wipeDays = $"{GetColor1()}We estimate needing to wipe in </color>{GetColor2()}" + timetowipe + $"</color> {GetColor1()}days!</color>";
                return wipeDays;
            }

            var nodays = $"{GetColor1()}We only just recently wiped and cannot estimate next wipe yet!</color>";
            return nodays;
        }

        private static void SendMessage(BasePlayer player, string message, params object[] args) => player?.SendConsoleCommand("chat.add", -1, string.Format($"<color={ChatPrefixColor}>{ChatPrefix}</color>: {message}", args), 1.0);

        private long GetTimestamp(DateTime date) => Convert.ToInt64(date.Subtract(epoch).TotalSeconds);
    }
}