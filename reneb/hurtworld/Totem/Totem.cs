using System.Collections.Generic;
using System;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("Select Totems", "Reneb", "1.0.1")]
    [Description("Set a totem to respawn at.")]

    class Totem : HurtworldPlugin
    {
        Dictionary<ulong, List<OwnershipStakeServer>> cashedTotems = new Dictionary<ulong, List<OwnershipStakeServer>>();
        Hash<ulong, float> lastRefresh = new Hash<ulong, float>();
        Hash<ulong, float> lastSelection = new Hash<ulong, float>();
        float floatTotemSelectCooldown = 300f;
        float floatTotemRefresh = 120f;


        int totemRefresh = 120;
        string permissionTotem = "totem.use";
        int totemSelectCooldown = 300;
        

        protected override void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<int>("Totem list Refresh timer", ref totemRefresh);
            CheckCfg<int>("Totem select cooldown", ref totemSelectCooldown);
            CheckCfg<string>("Permission", ref permissionTotem);

            floatTotemRefresh = float.Parse(totemRefresh.ToString());
            floatTotemSelectCooldown = float.Parse(totemSelectCooldown.ToString());

            SaveConfig();
        }

        static MethodInfo invalidate;

        void Loaded()
        {
            permission.RegisterPermission(permissionTotem, this);

            var messages = new Dictionary<string, string>
            {
                {"Not Allowed", "You are not allowed to use this command"},
                {"Select a totem", "Select a totem to spawn at with:<color=red> /totem IDNUM</color>. Next refresh in {refresh}s" },
                {"No Totems Found","<color=yellow>No totems where found</color>" },
                {"Use /totem", "Use /totem to get the full list of your totems" },
                {"/totem TOTEMID","/totem TOTEMID" },
                {"Totem doesnt exist","This totem doesn't exist" },
                {"No longer authorized","You are no longer authorized to spawn on this totem" },
                {"Last Selection Cooldown","You must wait {cooldown}s before being able to select a new totem to respawn at" },
                {"Set Spawn","You've now set spawn on the territory named: {totem}" }
            };

            lang.RegisterMessages(messages, this);
        }
        void OnServerInitialized()
        {
            invalidate = typeof(OwnershipStakeServer).GetMethod("InvalidateServer", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }

        [ChatCommand("totem")]
        private void cmdTotem(PlayerSession player, string command, string[] args)
        {
            if(!permission.UserHasPermission(player.SteamId.ToString(), permissionTotem))
            {
                hurt.SendChatMessage(player, GetMessage("Not Allowed", player.SteamId.ToString()));
                return;
            }
            PlayerIdentity identity = player.Identity;
            ulong steamID = player.SteamId.m_SteamID;
            
            if (args == null || args.Length == 0)
            { 
                if (UnityEngine.Time.realtimeSinceStartup - lastRefresh[steamID] > floatTotemRefresh)
                {
                    RefreshTotem(identity);
                }
                int o = 0;
                hurt.SendChatMessage(player, GetMessage("Select a totem", player.SteamId.ToString()).Replace("{refresh}",UnityEngine.Mathf.Ceil(floatTotemRefresh - (UnityEngine.Time.realtimeSinceStartup - lastRefresh[steamID])).ToString()));
                foreach (OwnershipStakeServer stake in cashedTotems[steamID])
                {
                    if (stake.AuthorizedPlayers.Contains(identity))
                    {
                        hurt.SendChatMessage(player, string.Format("{0} - {1}", o.ToString(), stake.TerritoryName));
                        o++;
                    }
                }
                if(o==0)
                {
                    hurt.SendChatMessage(player, GetMessage("No Totems Found", player.SteamId.ToString()));
                }
            }
            else
            {
                if (UnityEngine.Time.realtimeSinceStartup - lastSelection[steamID] < floatTotemSelectCooldown)
                {
                    hurt.SendChatMessage(player, GetMessage("Last Selection Cooldown", player.SteamId.ToString()).Replace("{cooldown}", UnityEngine.Mathf.Ceil(floatTotemSelectCooldown - (UnityEngine.Time.realtimeSinceStartup - lastSelection[steamID])).ToString()));
                    return;
                }
                int arg = 0;
                if(!int.TryParse(args[0], out arg))
                {
                    hurt.SendChatMessage(player, GetMessage("/totem TOTEMID", player.SteamId.ToString()));
                    return;
                }
                if(!cashedTotems.ContainsKey(steamID))
                {
                    hurt.SendChatMessage(player, GetMessage("Use /totem", player.SteamId.ToString()));
                    return;
                }
                if(cashedTotems[steamID][arg] == null)
                {
                    hurt.SendChatMessage(player, GetMessage("Totem doesnt exist", player.SteamId.ToString()));
                    return;
                }
                if (!cashedTotems[steamID][arg].AuthorizedPlayers.Contains(identity))
                {
                    hurt.SendChatMessage(player, GetMessage("No longer authorized", player.SteamId.ToString()));
                    return;
                }
                List<OwnershipStakeServer> all = RefTrackedBehavior<OwnershipStakeServer>.GetAll();
                for (int p = 0; p < all.Count; p++)
                {
                    OwnershipStakeServer staked = all[p];
                    staked.SpawnPlayers.Remove(identity);
                    invalidate.Invoke(staked, null);
                }
                cashedTotems[steamID][arg].SpawnPlayers.Add(identity);
                invalidate.Invoke(cashedTotems[steamID][arg], null);
                hurt.SendChatMessage(player, GetMessage("Set Spawn", player.SteamId.ToString()).Replace("{totem}", cashedTotems[steamID][arg].TerritoryName));
                lastSelection[steamID] = UnityEngine.Time.realtimeSinceStartup;
            }
        }

        void RefreshTotem(PlayerIdentity identity)
        {
            ulong steamID = identity.SteamId.m_SteamID;
            if (!cashedTotems.ContainsKey(steamID))
                cashedTotems.Add(steamID, new List<OwnershipStakeServer>());
            cashedTotems[steamID].Clear();
            OwnershipStakeServer[] stakeArray = UnityEngine.Object.FindObjectsOfType<OwnershipStakeServer>();
            for (int i = 0; i < stakeArray.Length; i++)
            {
                if (stakeArray[i].AuthorizedPlayers.Contains(identity))
                {
                    cashedTotems[steamID].Add(stakeArray[i]);
                }
            }
            lastRefresh[steamID] = UnityEngine.Time.realtimeSinceStartup;
        }
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
    }
}
