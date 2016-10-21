using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Finder", "Reneb", "3.0.2", ResourceId = 692)]
    class Finder : RustPlugin
    {
        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Plugin References
        //////////////////////////////////////////////////////////////////////////////////////////
        [PluginReference]
        Plugin PlayerDatabase;

        Dictionary<ulong, PlayerFinder> cachedFinder = new Dictionary<ulong, PlayerFinder>();

        static string findPermission = "finder.find";
        static int findAuthlevel = 1;


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
            CheckCfg<int>("auth level permission", ref findAuthlevel);
            CheckCfg<string>("oxide Permission", ref findPermission);

            SaveConfig();
        }

        void Loaded()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"You don't have the permission to use this command","You don't have the permission to use this command" },
                {"ListCommands","/find player PLAYERNAME/STEAMID\n\r/find cupboard PLAYERNAME/STEAMID\n\r/find bag PLAYERNAME/STEAMID\n\r/find building PLAYERNAME/STEAMID\n\r/find item ITEMNAME MINAMOUNT\n\r/find tp FINDID" },
                {"Multiple players found:\n\r","Multiple players found:\n\r" },
                {"No matching players found.","No matching players found." },
                {"You need to select a target player.","You need to select a target player." },
                {"This player doesn't have a position","This player doesn't have a position" },
                {"This player doesn't have any cupboard privileges","This player doesn't have any cupboard privileges" },
                {"You didn't find anything yet","You didn't find anything yet" },
                {"You need to select a target findid.","You need to select a target findid." },
                {"This id is out of range.","This id is out of range." },
                {"You are using the console, you can't tp!","You are using the console, you can't tp!" },
                {"This player hasn't built anything yet","This player hasn't built anything yet" },
                {"usage: /find item ITEMNAME MINAMOUNT.","usage: /find item ITEMNAME MINAMOUNT." },
                {"You didn't use a valid item name.","You didn't use a valid item name." }
            }, this);
        }

        bool hasPermission(BasePlayer player, string perm, int authLevel)
        {
            var lvl = player?.net?.connection?.authLevel;
            if (lvl == null || lvl >= authLevel) { return true; }

            return permission.UserHasPermission(player.userID.ToString(), perm);
        }

        string GetMsg(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player == null ? null : player.userID.ToString());
        }

        class FindData
        {
            string Name;
            public Vector3 Pos;
            string TypeName;

            public FindData(string TypeName, Vector3 Pos, string Name)
            {
                this.Name = Name;
                this.TypeName = TypeName;
                this.Pos = Pos;
            }

            public override string ToString()
            {
                return string.Format("{0} - {1}{2}", TypeName, Pos.ToString(), Name == string.Empty ? string.Empty : (" - " + Name));
            }

        }

        class PlayerFinder
        {
            string Name;
            string Id;
            bool Online;

            public List<FindData> Data = new List<FindData>();

            public PlayerFinder(string Name, string Id, bool Online)
            {
                this.Name = Name;
                this.Id = Id;
                this.Online = Online;
            }

            public void AddFind(string TypeName, Vector3 Pos, string Name)
            {
                Data.Add(new FindData(TypeName, Pos, Name));
            }

            public override string ToString()
            {
                return string.Format("{0} {1} - {2}", Id, Name, Online ? "Connected" : "Offline");
            }
        }

        PlayerFinder GetPlayerInfo(ulong userID)
        {
            var steamid = userID.ToString();
            var player = covalence.Players.FindPlayer(steamid);
            if(player != null)
            {
                return new PlayerFinder(player.Name, player.Id, player.IsConnected);
            }

            if(PlayerDatabase != null)
            {
                var name = (string)PlayerDatabase?.Call("GetPlayerData", steamid, "name");
                if(name != null)
                {
                    return new PlayerFinder(name, steamid, false);
                }
            }

            return new PlayerFinder("Unknown", steamid, false);
        }

        private object FindPosition(ulong userID)
        {
            var p = BasePlayer.activePlayerList.Find((BasePlayer x) => x.userID == userID);
            if(p == null)
            {
                p = BasePlayer.sleepingPlayerList.Find((BasePlayer x) => x.userID == userID);
                if (p == null)
                    return null;
            }
            return p.transform.position;
        }


        private object FindPlayerID(string arg, BasePlayer source = null)
        {
            ulong userID = 0L;
            if (arg.Length == 17 && ulong.TryParse(arg, out userID))
                return userID;

            var players = covalence.Players.FindPlayers(arg).ToList();
            if(players.Count > 1)
            {
                var returnstring = GetMsg("Multiple players found:\n\r", source);
                foreach(var p in players)
                {
                    returnstring += string.Format("{0} - {1}\n\r", p.Id, p.Name);
                }
                return returnstring;
            }
            if(players.Count == 1)
            {
                return ulong.Parse(players[0].Id);
            }

            if (PlayerDatabase != null)
            {
                string success = PlayerDatabase.Call("FindPlayer", arg) as string;
                if (success.Length == 17 && ulong.TryParse(success, out userID))
                {
                    return userID;
                }
                else
                    return success;
            }

            return GetMsg("No matching players found.", source);
        }

        string Find(BasePlayer player, string[] args)
        {
            string returnstring = string.Empty;

            if(!hasPermission(player, findPermission, findAuthlevel))
            {
                return GetMsg("You don't have the permission to use this command.", player);
            }

            if(args == null || args.Length == 0)
            {
                return GetMsg("ListCommands", player);
            }
            var puserid = player == null ? 0L : player.userID;
            switch(args[0].ToLower())
            {
                case "player":
                case "bag":
                case "cupboard":
                case "building":
                    if(args.Length == 1)
                    {
                        return GetMsg("You need to select a target player.", player);
                    }
                    var f = FindPlayerID(args[1], player);
                    if(!(f is ulong))
                    {
                        return f.ToString();
                    }
                    ulong targetID = (ulong)f;
                    var d = GetPlayerInfo(targetID);
                    returnstring = d.ToString() + ":\n\r";
                    switch (args[0].ToLower())
                    {
                        case "player":
                            var p = FindPosition(targetID);
                            if(p == null)
                            {
                                returnstring += GetMsg("This player doesn't have a position", player);
                            }
                            else 
                                d.AddFind("Position", (Vector3)p, string.Empty);
                            break;
                        case "bag":
                            var bs = SleepingBag.FindForPlayer(targetID, true).ToList();
                            if (bs.Count == 0)
                            {
                                returnstring += GetMsg("This player doesn't have any bags", player);
                            }
                            foreach(var b in bs)
                            {
                                d.AddFind(b.ShortPrefabName, b.transform.position, b.niceName);
                            }
                            break;
                        case "cupboard":
                            var cs = Resources.FindObjectsOfTypeAll<BuildingPrivlidge>().Where(x => x.authorizedPlayers.Any((ProtoBuf.PlayerNameID z) => z.userid == targetID)).ToList();
                            if(cs.Count== 0)
                            {
                                returnstring += GetMsg("This player doesn't have any cupboard privileges", player);
                            }
                            foreach(var c in cs)
                            {
                                d.AddFind("Tool Cupboard", c.transform.position, string.Empty);
                            }
                            break;
                        case "building":
                            var bb = Resources.FindObjectsOfTypeAll<BuildingBlock>().Where(x => x.OwnerID == targetID).ToList();
                            if (bb.Count == 0)
                            {
                                returnstring += GetMsg("This player hasn't built anything yet", player);
                            }
                            var dic = new Dictionary<uint, Dictionary<string, object>>();
                            foreach(var b in bb)
                            {
                                if(!dic.ContainsKey(b.buildingID))
                                {
                                    dic.Add(b.buildingID, new Dictionary<string, object>
                                    {
                                        {"pos", b.transform.position },
                                        {"num", 0 }
                                    });
                                }
                                dic[b.buildingID]["num"] = (int)dic[b.buildingID]["num"] + 1;
                            }
                            foreach (var c in dic)
                            {
                                d.AddFind("Building", (Vector3)c.Value["pos"], c.Value["num"].ToString());
                            }
                            break;
                        default:
                            break;
                    }
                    for (int i = 0; i < d.Data.Count; i++)
                    {
                        returnstring += i.ToString() + " - " + d.Data[i].ToString() + "\n\r";
                    }
                    if (cachedFinder.ContainsKey(puserid))
                    {
                        cachedFinder[puserid].Data.Clear();
                        cachedFinder[puserid] = null;
                        cachedFinder.Remove(puserid);
                    }
                    cachedFinder.Add(puserid, d);
                    break;
                case "item":
                    if(args.Length < 3)
                    {
                        return GetMsg("usage: /find item ITEMNAME MINAMOUNT optional:STEAMID.", player);
                    }
                    var pu = GetPlayerInfo(puserid);
                    var itemname = args[1].ToLower();
                    ulong ownerid = 0L;
                    if (args.Length > 3)
                        ulong.TryParse(args[3], out ownerid);
                    var itemamount = 0;
                    if(!(int.TryParse(args[2], out itemamount)))
                    {
                        return GetMsg("usage: /find item ITEMNAME MINAMOUNT optional:STEAMID.", player);
                    }
                    ItemDefinition item = null;
                    for(int i = 0; i < ItemManager.itemList.Count; i++)
                    {
                        if(ItemManager.itemList[i].displayName.english.ToLower() == itemname)
                        {
                            item = ItemManager.itemList[i];
                            break;
                        }
                    }
                    if(item == null)
                    {
                        return GetMsg("You didn't use a valid item name.", player);
                    }
                    foreach (StorageContainer sc in Resources.FindObjectsOfTypeAll<StorageContainer>())
                    {
                        ItemContainer inventory = sc.inventory;
                        if (inventory == null) continue;
                        List<Item> list = inventory.itemList.FindAll((Item x) => x.info.itemid == item.itemid);
                        int amount = 0;
                        foreach (Item current in list)
                        {
                            if(ownerid == 0L || IsOwned(current, ownerid))
                                amount += current.amount;
                        }
                        if (amount < itemamount) continue;
                        pu.AddFind("Box", sc.transform.position, amount.ToString());
                    }
                    foreach (BasePlayer bp in Resources.FindObjectsOfTypeAll<BasePlayer>())
                    {
                        PlayerInventory inventory = player.inventory;
                        if (inventory == null) continue;
                        int amount = inventory.GetAmount(item.itemid);
                        if (amount < itemamount) continue;
                        Dictionary<string, object> scdata = new Dictionary<string, object>();
                        pu.AddFind(string.Format("{0} {1}", player.userID.ToString(), player.displayName), bp.transform.position, amount.ToString());
                    }
                    for (int i = 0; i < pu.Data.Count; i++)
                    {
                        returnstring += i.ToString() + " - " + pu.Data[i].ToString() + "\n\r";
                    }
                    if (cachedFinder.ContainsKey(puserid))
                    {
                        cachedFinder[puserid].Data.Clear();
                        cachedFinder[puserid] = null;
                        cachedFinder.Remove(puserid);
                    }
                    cachedFinder.Add(puserid, pu);
                    break;
                case "tp":
                    if(player == null)
                    {
                        return GetMsg("You are using the console, you can't tp!", player);
                    }
                    if (!cachedFinder.ContainsKey(puserid))
                    {
                        return GetMsg("You didn't find anything yet", player);
                    }
                    if (args.Length == 1)
                    {
                        return GetMsg("You need to select a target findid.", player);
                    }
                    var fp = cachedFinder[puserid];
                    var id = 0;
                    int.TryParse(args[1], out id);
                    if(id >= fp.Data.Count)
                    {
                        return GetMsg("This id is out of range.", player);
                    }

                    var data = cachedFinder[puserid].Data[id];
                    player.MovePosition(data.Pos);
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", data.Pos);
                    returnstring += data.ToString();
                    break;
                default:
                    returnstring += GetMsg("ListCommands", player);
                    break;
            }
           


            return returnstring;
        }

        bool IsOwned(Item item, ulong userid)
        {
            var owner = item.owners.Where(x => x.userid == userid).ToList();
            if (owner == null || owner.Count == 0) return false;
            return true;
        }

        [ChatCommand("find")]
        void cmdChatFind(BasePlayer player, string command, string[] args)
        {
            SendReply(player, Find(player, args));
        }

        [ConsoleCommand("finder.find")]
        void cmdConsoleFind(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            arg.ReplyWith(Find(player, arg.Args));
        }
    }
}
