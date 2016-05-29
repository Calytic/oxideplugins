using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("DeathKick", "k1lly0u", "0.1.2", ResourceId = 1779)]
    public class DeathKick : RustPlugin
    {        
        private Dictionary<ulong, double> deadPlayers = new Dictionary<ulong, double>();
        private List<Timer> Timers = new List<Timer>();
        private Dictionary<ulong, int> deathCounts = new Dictionary<ulong, int>();

        #region oxide hooks
        void Loaded() => lang.RegisterMessages(messages, this);
        void OnServerInitialized()
        {
            LoadVariables();
            permission.RegisterPermission("deathkick.exempt", this);
        }
        void Unload() => ClearData();
        #endregion

        #region functions
        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            try
            {
                if (entity != null)
                    if (entity is BasePlayer)
                        ProcessDeath((BasePlayer)entity, hitinfo); 
            }
            catch (Exception ex)
            {
            }
        }
        private void ProcessDeath(BasePlayer player, HitInfo info, bool isBounty = false)
        {            
            if (!GetDeathType(player, info)) return;
            if (player.IsAdmin() || permission.UserHasPermission(player.UserIDString, "deathkick.exempt")) return;
            if (useBounty && !isBounty) return;

            if (!deathCounts.ContainsKey(player.userID))
                deathCounts.Add(player.userID, 0);
            deathCounts[player.userID]++;

            if (deathCounts[player.userID] >= deathLimit)
            {
                deadPlayers.Add(player.userID, GrabCurrentTime() + (cooldownTime * 60));
                Timers.Add(timer.Once(cooldownTime * 60, () => deadPlayers.Remove(player.userID)));
                Network.Net.sv.Kick(player.net.connection, string.Format(lang.GetMessage("died", this, player.UserIDString), cooldownTime, lang.GetMessage("minutes", this, player.UserIDString)));
                deathCounts.Remove(player.userID);
            }
        }
        public bool GetDeathType(BasePlayer player, HitInfo info)
        {
            if (info == null && useFall) return true;

            BaseEntity entity = info.Initiator;
            if (entity == null) return false;
            else if (entity.ToPlayer() != null)
            {
                if (info.damageTypes.GetMajorityDamageType().ToString() == "Suicide" && useSuicide) return true;
                if (usePlayers) return true;
            }
            else if (entity.name.Contains("patrolhelicopter.pr") && useHeli) return true;
            else if (entity.name.Contains("animals/") && useAnimals) return true;
            else if (entity.name.Contains("beartrap.prefab") && useBeartrap) return true;
            else if (entity.name.Contains("landmine.prefab") && useLandmine) return true;
            else if (entity.name.Contains("spikes.floor.prefab") && useFloorspikes) return true;
            else if (entity.name.Contains("autoturret_deployed.prefab") && useAutoturret) return true;
            else if ((entity.name.Contains("deployable/barricades") || entity.name.Contains("wall.external.high")) && useBarricades) return true;
            return false;
        }
        object CanClientLogin(Network.Connection connection)
        {
            if (deadPlayers.ContainsKey(connection.userid))
            {
                int remaining = (int)deadPlayers[connection.userid] - (int)GrabCurrentTime();                
                int time = remaining / 60;
                string timeMsg = lang.GetMessage("minutes", this, connection.userid.ToString());

                if (remaining <= 90)
                {
                    time = remaining;
                    timeMsg = lang.GetMessage("seconds", this, connection.userid.ToString());
                }

                return string.Format(lang.GetMessage("wait", this, connection.userid.ToString()), time, timeMsg);
            }
            return null;
        }
        void ClearData()
        {
            foreach (var entry in Timers)
                entry.Destroy();
            deadPlayers.Clear();
        }
        static double GrabCurrentTime()
        {
            return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }
        #endregion

        #region config
        static int cooldownTime = 30;
        static int deathLimit = 1;
        static bool usePlayers = true;
        static bool useHeli = true;
        static bool useAnimals = true;
        static bool useBeartrap = true;
        static bool useLandmine = true;
        static bool useFloorspikes = true;
        static bool useBarricades = true;
        static bool useAutoturret = true;
        static bool useSuicide = true;
        static bool useFall = true;
        static bool useBounty = false;

        private bool changed;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Death types - Players", ref usePlayers);
            CheckCfg("Death types - Helicopters", ref useHeli);
            CheckCfg("Death types - Animals", ref useAnimals);
            CheckCfg("Death types - Beartraps", ref useBeartrap);
            CheckCfg("Death types - Landmines", ref useLandmine);
            CheckCfg("Death types - Floorspikes", ref useFloorspikes);
            CheckCfg("Death types - Barricades", ref useBarricades);
            CheckCfg("Death types - Autoturrets", ref useAutoturret);
            CheckCfg("Death types - Suicide", ref useSuicide);
            CheckCfg("Death types - Fall", ref useFall);
            CheckCfg("Death Limit", ref deathLimit);
            CheckCfg("Bounty kills only", ref useBounty);
            CheckCfg("Timer - Amount of time a player is kicked for (minutes)", ref cooldownTime);            
        }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }
        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                changed = true;
            }
            return value;
        }
        #endregion

        #region messages
        Dictionary<string, string> messages = new Dictionary<string, string>
        {
            { "died", "You died and must wait {0} {1} before reconnecting" },
            { "wait", "You must wait another {0} {1} before you can reconnect" },
            { "minutes", "minutes" },
            { "seconds", "seconds" }
        };
        #endregion
    }
}
