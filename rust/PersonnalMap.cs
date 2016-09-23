
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Personnal Map", "Reneb", "1.0.0")]
    public class PersonnalMap : RustPlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;

        #region Fields
        ItemDefinition papermapdef = null;

        static string cfg_map_user_permission = "personnalmap.user";
        static string cfg_map_admin_permission = "personnalmap.admin";

        static int cfg_map_user_max = 1;
        static bool cfg_map_user_auto = true;


        static bool cfg_auto_message = true;
       
        static bool cfg_auto_default_map = true;
        static string cfg_default_map = string.Empty;
        PaperMap default_map = null;

        #endregion

        string GetMsg(string key, object steamid = null)
        {
            return lang.GetMessage(key, this, steamid == null ? null : steamid.ToString());
        }
        void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"You may use /pmap to save and load personnal saved maps.","You may use /pmap to save and load personnal saved maps."},
                {"Couldn't find the Paper Map as a Held Entity.","Couldn't find the Paper Map as a Held Entity." },
                {"Couldn't find the map inside the Paper Map.","Couldn't find the map inside the Paper Map." },
                {"You are not allowed to use this command.","You are not allowed to use this command." },
                {"UserHelp","/pmap list => to show your current map list\n\r/pmap save mapname => to save your current map\n\r/pmap load mapname => to load a specific map saved\r\n/pmap remove mapname => to remove a specific map.\n\ryou may use \"auto\" to automatically load a map when you craft a new paper map.\n\r" },
                {"AdminHelp", "/pmap admin reset => to reset all data related to this plugin.\n\r/pmap admin default optional:null => to set or remove a default map." },
                {"List of saved maps:\n","List of saved maps:\n" },
                { "usage: /pmap remove mapname.", "usage: /pmap remove mapname." },
                {"You don't have any map named like that.","You don't have any map named like that." },
                {"You have successfully removed your map: {0}.","You have successfully removed your map: {0}." },
                {"Couldn't find your inventory.","Couldn't find your inventory." },
                {"You need to place the Paper Map in your Belt.","You need to place the Paper Map in your Belt." },
                {"usage: /pmap save mapname.","usage: /pmap save mapname." },
                {"Your map has been saved to: {0}","Your map has been saved to: {0}" },
                { "usage: /pmap load mapname.", "usage: /pmap load mapname." },
                {"You have successfully loaded your map: {0}","You have successfully loaded your map: {0}" }
            }, this);
        }


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
            CheckCfg<string>("User - Permission", ref cfg_map_user_permission);
            CheckCfg<string>("Admin - Permission", ref cfg_map_admin_permission);

            CheckCfg<int>("User - Max saveable map per user", ref cfg_map_user_max);
            CheckCfg<bool>("User - Allow to set an auto map", ref cfg_map_user_auto);

            CheckCfg<bool>("Announce message when crafting a Paper map", ref cfg_auto_message);

            CheckCfg<bool>("Default - Use default map", ref cfg_auto_default_map);
            CheckCfg<string>("Default - Map (DO NOT EDIT HERE)", ref cfg_default_map);

            if(cfg_auto_default_map)
                default_map = cfg_default_map == string.Empty ? null : JsonConvert.DeserializeObject<PaperMap>(cfg_default_map);

            SaveConfig();

            InitializeLang();

            
        }

        void OnServerInitialized()
        {
            permission.RegisterPermission(cfg_map_user_permission, this);
            permission.RegisterPermission(cfg_map_admin_permission, this);
            try
            {
                papermapdef = (ItemManager.itemList.Where(x => x.displayName.english == "Paper Map").ToList())[0];
            }
            catch { }

            if (papermapdef == null)
            {
                Interface.Oxide.LogWarning("PersonnalMap didn't find the Paper Map item definition. Unloading the plugin now");
                timer.Once(0.01f, () => Interface.Oxide.UnloadPlugin("PersonnalMap"));
                return;
            }

            if (PlayerDatabase == null)
            {
                Interface.Oxide.LogWarning("PlayerDatabase was not found. PersonnalMap will now Unload.");
                timer.Once(0.01f, () => Interface.Oxide.UnloadPlugin("PersonnalMap"));
                return;
            }

            if (!cfg_auto_default_map && !cfg_auto_message && !cfg_map_user_auto)
            {
                Unsubscribe(nameof(OnItemCraftFinished));
            }
        }

        bool hasPermission(BasePlayer player, string permissionName)
        {
            if (player.net.connection.authLevel == 2) return true;
            return permission.UserHasPermission(player.userID.ToString(), permissionName);
        }

        public class PaperMap
        {
            public uint[] ImageFog;
            public uint[] ImagePaint;

            public PaperMap() { }
            public PaperMap(uint[] ImageFog, uint[] ImagePaint)
            {
                this.ImageFog = ImageFog;
                this.ImagePaint = ImagePaint;
            }
        }

        void OnItemCraftFinished(ItemCraftTask task,Item item)
        {
            if (item.info.itemid != papermapdef.itemid) return;

            var player = task.owner;
            if (player == null) return;

            var steamid = player.userID.ToString();

            if(cfg_auto_message)
            {
                SendReply(player, GetMsg("You may use /pmap to save and load personnal saved maps.", steamid));
            }

            string reason = string.Empty;
            MapEntity map = null;

            reason = GetMapEntity(item, out map);
            if (reason != string.Empty)
            {
                SendReply(player, GetMsg(reason, steamid));
                return;
            }

            if (cfg_map_user_auto)
            {
                var PlayerData = new Dictionary<string, PaperMap>();

                var success = PlayerDatabase.Call("GetPlayerDataRaw", player.userID.ToString(), "PersonnalMap");
                if (success is string)
                {
                    PlayerData = JsonConvert.DeserializeObject<Dictionary<string, PaperMap>>((string)success);
                }
                if(PlayerData.ContainsKey("auto"))
                {
                    map.fogImages = PlayerData["auto"].ImageFog;
                    map.paintImages = PlayerData["auto"].ImagePaint;
                    return;
                }
            }

            if(cfg_auto_default_map && default_map != null)
            {
                map.fogImages = default_map.ImageFog;
                map.paintImages = default_map.ImagePaint;
            }
        }

        string GetMapEntity(Item item, out MapEntity ent)
        {
            ent = null;
            var held = item.GetHeldEntity();
            if (held == null)
            {
                return "Couldn't find the Paper Map as a Held Entity.";
            }

            var ment = held.GetComponent<MapEntity>();
            if (ment == null)
            {
                return "Couldn't find the map inside the Paper Map.";
            }

            ent = ment;

            return string.Empty;
        }

        string GetHelp(BasePlayer player)
        {
            var ret = string.Empty;
            var steamid = player.userID.ToString();
            if(hasPermission(player, cfg_map_user_permission))
            {
                ret += GetMsg("UserHelp", steamid);
            }
            if(hasPermission(player, cfg_map_admin_permission))
            {
                ret += GetMsg("AdminHelp", steamid);
            }
            return ret;
        }

        [ChatCommand("pmap")]
        void cmdPersonnalMap(BasePlayer player, string command, string[] args)
        {
            using (TimeWarning.New("Personnal Map Command", 0.01f))
            {
                string reason = string.Empty;
                MapEntity map = null;
                var steamid = player.userID.ToString();

                if (!hasPermission(player, cfg_map_user_permission))
                {
                    SendReply(player, GetMsg("You are not allowed to use this command.",steamid));
                    return;
                }

                if (args.Length == 0)
                {
                    SendReply(player, GetHelp(player));
                    return;
                }

                var name = args.Length > 1 ? args[1].ToLower() : string.Empty;

                var PlayerData = new Dictionary<string, PaperMap>();

                var success = PlayerDatabase.Call("GetPlayerDataRaw", steamid, "PersonnalMap");
                if (success is string)
                {
                    PlayerData = JsonConvert.DeserializeObject<Dictionary<string, PaperMap>>((string)success);
                }

                switch (args[0].ToLower())
                {
                    case "admin":
                        if (!hasPermission(player, cfg_map_admin_permission))
                        {
                            SendReply(player, GetMsg("You are not allowed to use this command.", steamid));
                            return;
                        }
                        if (args.Length == 1)
                        {
                            SendReply(player, GetHelp(player));
                            return;
                        }
                        switch (args[1].ToLower())
                        {
                            case "reset":
                                var knownPlayers = (HashSet<string>)PlayerDatabase.Call("GetAllKnownPlayers");
                                if (knownPlayers == null)
                                {
                                    SendReply(player, "Couldn't get all known players out of PlayerDatabase."); return;
                                }
                                foreach (var userid in knownPlayers)
                                {
                                    PlayerDatabase?.Call("SetPlayerData", userid, "PersonnalMap", new Dictionary<string, PaperMap>());
                                }
                                cfg_default_map = string.Empty;
                                Config["Default - Map (DO NOT EDIT HERE)"] = cfg_default_map;
                                default_map = null;
                                SaveConfig();
                                SendReply(player, "All personnal map data has been reset.");
                                return;
                                break;
                            case "default":
                                if (args.Length > 2 && (args[2].ToLower() == "false" || args[2].ToLower() == "null"))
                                {
                                    cfg_default_map = string.Empty;
                                    Config["Default - Map (DO NOT EDIT HERE)"] = cfg_default_map;
                                    default_map = cfg_default_map == string.Empty ? null : JsonConvert.DeserializeObject<PaperMap>(cfg_default_map);
                                    SaveConfig();
                                    SendReply(player, "You've removed the default map.");
                                    return;
                                }
                                
                                break;
                            default:
                                SendReply(player,GetHelp(player));
                                return;
                                break;
                        }
                        break;
                    case "save":
                    case "load":
                       
                        break;
                    case "list":
                        string l = GetMsg("List of saved maps:\n", steamid);
                        foreach (var n in PlayerData.Keys)
                        {
                            l += n + "\n";
                        }
                        SendReply(player, l);
                        return;
                        break;
                    case "remove":
                        if (name == string.Empty)
                        {
                            SendReply(player, GetMsg("usage: /pmap remove mapname.",steamid));
                            return;
                        }
                        if (!PlayerData.ContainsKey(name))
                        {
                            SendReply(player, GetMsg("You don't have any map named like that.",steamid));
                            return;
                        }

                        PlayerData.Remove(name);
                        PlayerDatabase?.Call("SetPlayerData", steamid, "PersonnalMap", PlayerData);
                        SendReply(player, string.Format(GetMsg("You have successfully removed your map: {0}.",steamid), name));
                        return;
                        break;
                    default:
                        SendReply(player, GetHelp(player));
                        return;
                        break;
                }

                var inv = player.inventory;
                if (inv == null)
                {
                    SendReply(player, GetMsg("Couldn't find your inventory.",steamid));
                    return;
                }

                var target = inv.containerBelt.FindItemByItemID(papermapdef.itemid);

                if (target == null)
                {
                    SendReply(player, GetMsg("You need to place the Paper Map in your Belt.",steamid));
                    return;
                }

                reason = GetMapEntity(target, out map);
                if(reason != string.Empty)
                {
                    SendReply(player, reason);
                    return;
                }
                
                
                switch (args[0].ToLower())
                {
                    case "admin":
                        
                        switch(args[1].ToLower())
                        {
                            case "default":
                                var d = new PaperMap(map.fogImages, map.paintImages);
                                cfg_default_map = JsonConvert.SerializeObject(d);
                                Config["Default - Map (DO NOT EDIT HERE)"] = cfg_default_map;
                                default_map = cfg_default_map == string.Empty ? null : JsonConvert.DeserializeObject<PaperMap>(cfg_default_map);
                                SaveConfig();
                                SendReply(player, "You have set your current map as the default crafting map.");
                                return;
                                break;
                            default:
                                break;
                        }
                        break;
                    case "save":
                        if(name == string.Empty)
                        {
                            SendReply(player, GetMsg("usage: /pmap save mapname.",steamid));
                            return;
                        }
                        if (!PlayerData.ContainsKey(name) && PlayerData.Count >= cfg_map_user_max)
                        {
                            SendReply(player, "You have reached the limit of maps your can save.");
                            return;
                        }
                        if (PlayerData.ContainsKey(name)) PlayerData.Remove(name);
                        PlayerData.Add(name, new PaperMap(map.fogImages, map.paintImages));

                        PlayerDatabase?.Call("SetPlayerData", steamid, "PersonnalMap", PlayerData);
                        SendReply(player, string.Format(GetMsg("Your map has been saved to: {0}",steamid), name));
                        return;
                        break;

                    case "load":
                        if (name == string.Empty)
                        {
                            SendReply(player, GetMsg("usage: /pmap load mapname.",steamid));
                            return;
                        }
                        if (!PlayerData.ContainsKey(name))
                        {
                            SendReply(player, GetMsg("You don't have any map named like that.",steamid));
                            return;
                        }

                        map.fogImages = PlayerData[name].ImageFog;
                        map.paintImages = PlayerData[name].ImagePaint;

                        target.RemoveFromContainer();
                        target.MoveToContainer(inv.containerBelt, -1, false);
                        SendReply(player, string.Format(GetMsg("You have successfully loaded your map: {0}",steamid), name));
                        return;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
