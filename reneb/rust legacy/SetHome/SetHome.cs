// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;

namespace Oxide.Plugins
{
    [Info("SetHome", "Reneb", "1.0.0")]
    class SetHome : RustLegacyPlugin
    {
        [PluginReference]
        Plugin Share;
        /////////////////////////////
        // FIELDS
        /////////////////////////////

        private DateTime epoch;

        RustServerManagement management;

        Dictionary<NetUser, Oxide.Plugins.Timer> timersList = new Dictionary<NetUser, Oxide.Plugins.Timer>();
        Dictionary<NetUser, double> nextHome = new Dictionary<NetUser, double>();

        Vector3 Vector3Down = new Vector3(0f, -1f, 0f);
        Vector3 UnderPlayerAdjustement = new Vector3(0f, -1.15f, 0f);
         
        NetUser cachedUser;
        string cachedString;
        string cachedUserid;
        RaycastHit cachedRaycast;
        RaycastHit2 cachedRaycast2;
        PlayerClient cachedPlayer;
        string cachedModelname;
        string cachedObjectname;
        float cachedDistance;
        bool cachedBoolean;
        Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;

        /////////////////////////////
        // Data Management
        /////////////////////////////
        StoredData storedData;
        Hash<string, SetHomeData> sethomedatas = new Hash<string, SetHomeData>();

        class StoredData
        {
            public HashSet<SetHomeData> SetHomeDatas = new HashSet<SetHomeData>();

            public StoredData()
            {
            }
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("SetHome", storedData);
        }
        void LoadData()
        {
            sethomedatas.Clear();
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("SetHome");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var thehomedata in storedData.SetHomeDatas)
                sethomedatas[thehomedata.userid] = thehomedata;
        }

        public class HomeLocations
        {
            public string name;
            public string x;
            public string y;
            public string z;

            Vector3 position;

            public HomeLocations()
            {

            }
            public HomeLocations(string name, Vector3 position)
            {
                this.x = position.x.ToString();
                this.y = position.y.ToString();
                this.z = position.z.ToString();
                this.name = name;
            }
            public Vector3 GetPosition()
            {
                if (position == default(Vector3))
                    position = new Vector3(float.Parse(this.x), float.Parse(this.y), float.Parse(this.z));
                return position;
            }

        }


        public class SetHomeData
        {
            public string userid;
            public List<HomeLocations> savedhomes;

            Dictionary<string, HomeLocations> homes;

            public SetHomeData()
            {
            }

            public SetHomeData(PlayerClient player)
            {
                userid = player.userID.ToString();
                savedhomes = new List<HomeLocations>();
                homes = new Dictionary<string, HomeLocations>();
            }

            public void SetHomes()
            {
                homes = new Dictionary<string, HomeLocations>();
                foreach (HomeLocations homeloc in savedhomes)
                {
                    homes.Add(homeloc.name.ToString().ToLower(), homeloc);
                }
            }
            public void AddHome(string name, Vector3 position)
            {
                savedhomes.Add(new HomeLocations(name, position));
                SetHomes();
            }

            public void RemoveHome(string name)
            {
                if (homes == null || homes.Count == 0)
                    SetHomes();
                if (homes.ContainsKey(name))
                {
                    savedhomes.Remove((homes[name]));
                }
                SetHomes();
            }
            public object FindHome(string name)
            {
                if (homes == null || homes.Count == 0)
                    SetHomes();
                if (!homes.ContainsKey(name)) return null;
                return homes[name].GetPosition();
            }
        }


        /////////////////////////////
        // Config Management
        /////////////////////////////

        static string notAllowed = "You are not allowed to use this command.";
        static bool cancelOnHurt = true;
        static int maxAllowed = 3;
        static int timerTeleport = 30;
        static int timeinterval = 60;

        double timeInterval;

        static bool sethomeOnlyBuildings = true;
        static bool sethomeOnlyFoundation = true;
        static bool sethomeOnlySelf = true;
        static bool useShare = true;


        static string teleportCancelled = "Teleportation was cancelled";
        static string teleportRestricted = "You are not allowed to teleport from where you are.";
        static string onlyBuildingsMessage = "You are only allowed to set home on buildings on this server.";
        static string onlyFoundationsMessage = "On buildings, you are only allowed to sethome on foundations.";
        static string onlySelfOrFriend = "On buildings, you are only allowed to sethome on your home or shared homes.";
        static string onlySelfMessage = "On buildings, you are only allowed to sethome on your home.";
        static string nohomesSet = "You dont have any homes set.";
        static string homeDoesntExist = "This home doesn't exist.";
        static string notAllowedHere = "You are not allowed to sethome here.";
        static string alreadyWaiting = "You are already waiting for a home teleportation.";
        static string cooldownMessage = "You must wait {0} seconds before requesting another home teleportation.";
        static string teleportationMessage = "You will be teleported in {0} seconds.";
        static string homelistMessage = "Homes list:";
        static string newhome = "You've set a new home named {0} @ {1}";
        static string maxhome = "You've reached the maximum homes allowed";
        static string homeErased = "{0} home point was erased";
        static string sethomeHelp1 = "/sethome XXX => to set home where you stand";
        static string sethomeHelp2 = "/sethome remove XXX => to remove a home";

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<string>("Messages: Restricted command", ref notAllowed);
            CheckCfg<bool>("Home: Cancel teleport when hurt", ref cancelOnHurt);
            CheckCfg<int>("Sethome: Max Allowed Homes", ref maxAllowed);
            CheckCfg<int>("Home: Time to teleport", ref timerTeleport);
            CheckCfg<int>("Home: Teleportations Cooldown", ref timeinterval);

            CheckCfg<bool>("Sethome: Only on buildings", ref sethomeOnlyBuildings);
            CheckCfg<bool>("Sethome: If on building, only on foundations", ref sethomeOnlyFoundation);
            CheckCfg<bool>("Sethome: If on building, only on own house (or shared house)", ref sethomeOnlySelf);
            CheckCfg<bool>("Sethome: allow Share Plugin", ref useShare);


            CheckCfg<string>("Messages: Teleportation cancelled", ref teleportCancelled);
            CheckCfg<string>("Messages: Teleportation Restricted", ref teleportRestricted);
            CheckCfg<string>("Messages: Only Buildings", ref onlyBuildingsMessage);
            CheckCfg<string>("Messages: Only Foundations", ref onlyFoundationsMessage);
            CheckCfg<string>("Messages: Only Self Or Friends Buildings", ref onlySelfOrFriend);
            CheckCfg<string>("Messages: Only Self Buildings", ref onlySelfMessage);
            CheckCfg<string>("Messages: No Homes Set", ref nohomesSet);
            CheckCfg<string>("Messages: Home Doesn't exist", ref homeDoesntExist);
            CheckCfg<string>("Messages: Restricted Location", ref notAllowedHere);
            CheckCfg<string>("Messages: Teleportation Pending", ref alreadyWaiting);
            CheckCfg<string>("Messages: Teleportation Cooldown", ref cooldownMessage);
            CheckCfg<string>("Messages: Teleportation Accepted", ref teleportationMessage);
            CheckCfg<string>("Messages: Homes List", ref homelistMessage);
            CheckCfg<string>("Messages: New home", ref newhome);
            CheckCfg<string>("Messages: Max Home Reached", ref maxhome);
            CheckCfg<string>("Messages: Home Erased", ref homeErased);
            CheckCfg<string>("Messages: Sethome Help 1", ref sethomeHelp1);
            CheckCfg<string>("Messages: Sethome Help 2", ref sethomeHelp2);

            SaveConfig();
            timeInterval = (double)timeinterval;
        }


        /////////////////////////////
        // Oxide Hooks
        /////////////////////////////

        void Loaded()
        {
            epoch = new System.DateTime(1970, 1, 1);
            LoadData();
        }
        void OnServerSave()
        {
            SaveData();
        }
        void OnServerInitialized()
        {
            management = RustServerManagement.Get();
        }
        void Unload()
        {
            foreach (KeyValuePair<NetUser, Oxide.Plugins.Timer> pair in timersList)
            {
                pair.Value.Destroy();
            }
            timersList.Clear();
            SaveData();
        }
        void OnPlayerDisconnect(uLink.NetworkPlayer netplayer)
        {
            NetUser netuser = (NetUser)netplayer.GetLocalData();
            ResetRequest(netuser);
        }

        /////////////////////////////
        // Teleportation Functions
        /////////////////////////////

        void DoTeleportToPos(NetUser source, Vector3 position)
        {
            if (source == null || source.playerClient == null)
                return;
            management.TeleportPlayerToWorld(source.playerClient.netPlayer, position);
        }
        void ResetRequest(NetUser netuser)
        {
            if (timersList.ContainsKey(netuser))
            {
                timersList[netuser].Destroy();
                timersList.Remove(netuser);
            }
            if (netuser.playerClient != null)
            {
                SendReply(netuser, teleportCancelled);
            }
        }

        void DoTeleportation(NetUser netuser, Vector3 position)
        {
            if (netuser == null || netuser.playerClient == null) return;

            if (timersList.ContainsKey(netuser))
            {
                timersList[netuser].Destroy();
                timersList.Remove(netuser);
            }

            object thereturn = Interface.GetMod().CallHook("canTeleport", new object[] { netuser });
            if (thereturn != null)
            {
                SendReply(netuser, teleportRestricted);
                return;
            }

            if (nextHome.ContainsKey(netuser)) nextHome.Remove(netuser);
            nextHome.Add(netuser, CurrentTime() + timeInterval);

            BreakLegs(netuser.playerClient);
            DoTeleportToPos(netuser, position);
            timer.Repeat( 2, 2, () => DoTeleportToPos(netuser, position));
            timer.Once(4, () => UnbreakLegs(netuser.playerClient));
        }
        void OnHurt(TakeDamage takedamage, DamageEvent damage)
        {
            if (!cancelOnHurt) return;
            if (damage.victim.client == null) return;
            if (damage.attacker.client == null) return;
            if (damage.amount < 5f) return;
            NetUser netuser = damage.victim.client.netUser;
            if (timersList.ContainsKey(netuser)) ResetRequest(netuser);
        }
        void OnKilled(TakeDamage takedamage, DamageEvent damage)
        {
            if (damage.victim.client == null) return;
            NetUser netuser = damage.victim.client.netUser;
            if (timersList.ContainsKey(netuser)) ResetRequest(netuser);
        }
        double CurrentTime()
        {
            return System.DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }
        void BreakLegs(PlayerClient player)
        {
            if (player == null) return;
            if (player.controllable == null) return;
            player.controllable.GetComponent<FallDamage>().AddLegInjury(1);

        }
        void UnbreakLegs(PlayerClient player)
        {
            if (player == null) return;
            if (player.controllable == null) return;
            player.controllable.GetComponent<FallDamage>().ClearInjury();
        }
        bool AllowedSetHome(NetUser netuser)
        {
            MeshBatchPhysics.Raycast(netuser.playerClient.lastKnownPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance);
            if(sethomeOnlyBuildings && cachedhitInstance == null)
            {
                SendReply(netuser, onlyBuildingsMessage);
                return false;
            }
            if(sethomeOnlyFoundation && cachedhitInstance != null)
            {
                if(!cachedhitInstance.physicalColliderReferenceOnly.gameObject.name.Contains("Foundation"))
                {
                    SendReply(netuser, onlyFoundationsMessage);
                    return false;
                }
            }
            if(sethomeOnlySelf && cachedhitInstance != null)
            {
                string ownerid = cachedhitInstance.physicalColliderReferenceOnly.GetComponent<StructureComponent>()._master.ownerID.ToString();
                if(ownerid != netuser.playerClient.userID.ToString())
                {
                    if(useShare && Share != null)
                    {
                        if(!(bool)Share.Call("isSharing", ownerid, netuser.playerClient.userID.ToString()))
                        {
                            SendReply(netuser, onlySelfOrFriend);
                            return false;
                        }
                    }
                    else
                    {
                        SendReply(netuser, onlySelfMessage);
                        return false;
                    }
                }
            }
            return true;
        }
        [ChatCommand("sethome")]
        void cmdChatSetHome(NetUser netuser, string command, string[] args)
        {
            cachedUserid = netuser.playerClient.userID.ToString();
            if (args.Length == 0) {
                SendReply(netuser, sethomeHelp1);
                SendReply(netuser, sethomeHelp2);
                return;
            }

            SetHomeData newdata = sethomedatas[cachedUserid];
            if (newdata == null)
                newdata = new SetHomeData(netuser.playerClient);


            if (args.Length == 2 && args[0] == "remove")
            {
                var findhome = newdata.FindHome(args[1].ToString().ToLower());
                if (findhome == null)
                {
                    SendReply(netuser, homeDoesntExist);
                    return;
                }
                newdata.RemoveHome(args[1].ToString().ToLower());
                SendReply(netuser, string.Format(homeErased, args[1]));
            }
            else
            {
                var thereturn = Interface.GetMod().CallHook("canTeleport", new object[] { netuser });
                if (thereturn != null)
                {
                    SendReply(netuser, notAllowedHere);
                    return;
                }
                if (!AllowedSetHome(netuser)) return;

                var oldhome = newdata.FindHome(args[0].ToString().ToLower());
                if (oldhome is Vector3)
                {
                    newdata.RemoveHome(args[0].ToString().ToLower());
                    SendReply(netuser, string.Format(homeErased, args[0]));
                }
                if (newdata.savedhomes.Count >= maxAllowed)
                {
                    SendReply(netuser, maxhome);
                    return;
                }
                newdata.AddHome(args[0], netuser.playerClient.lastKnownPosition);
                SendReply(netuser, string.Format(newhome, args[0], netuser.playerClient.lastKnownPosition.ToString()));
            }
            if(sethomedatas[cachedUserid] != null) storedData.SetHomeDatas.Remove(sethomedatas[cachedUserid]);
            sethomedatas[cachedUserid] = newdata;
            storedData.SetHomeDatas.Add(sethomedatas[cachedUserid]);
        }
        [ChatCommand("home")]
        void cmdChatTeleportAccept(NetUser netuser, string command, string[] args)
        {
            cachedUserid = netuser.playerClient.userID.ToString();

            if (sethomedatas[cachedUserid] == null)
            {
                SendReply(netuser, nohomesSet);
                return;
            }
            if ((sethomedatas[cachedUserid]).savedhomes.Count == 0)
            {
                SendReply(netuser, nohomesSet);
                return;
            }
            if (args.Length == 0)
            {
                SendReply(netuser, homelistMessage);
                foreach (HomeLocations homeloc in (sethomedatas[cachedUserid]).savedhomes)
                {
                    SendReply(netuser,string.Format("{0} - {1}",homeloc.name, homeloc.GetPosition().ToString()));
                }
                return;
            }
            if (timersList.ContainsKey(netuser))
            {
                SendReply(netuser, alreadyWaiting);
                return;
            } 
            if(nextHome.ContainsKey(netuser))
            {
                if(nextHome[netuser] > CurrentTime())
                {
                    SendReply(netuser, string.Format(cooldownMessage, Math.Ceiling( (nextHome[netuser] - CurrentTime() ) ).ToString()));
                    return;
                }
                nextHome.Remove(netuser);
            }
            var thereturn = Interface.GetMod().CallHook("canTeleport", new object[] { netuser });
            if (thereturn != null)
            {
                SendReply(netuser, teleportRestricted);
                return;
            }
            var findhome = (sethomedatas[cachedUserid]).FindHome(args[0]);
            if (findhome == null)
            {
                SendReply(netuser, homeDoesntExist);
                return;
            }
            timersList.Add(netuser, timer.Once(timerTeleport, () => DoTeleportation(netuser, (Vector3)findhome)));
            SendReply(netuser, string.Format(teleportationMessage, timerTeleport.ToString()));
        }
    }
}