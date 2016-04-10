using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Kits", "Reneb", "1.0.4", ResourceId = 1494)]
    [Description("Create kits of items for players to use.")]

    class Kits : HurtworldPlugin
    {
        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Plugin initialization
        //////////////////////////////////////////////////////////////////////////////////////////

        void Loaded()
        {
            LoadData();
            try
            {
                kitsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, KitData>>>("Kits_Data");
            }
            catch
            {
                kitsData = new Dictionary<ulong, Dictionary<string, KitData>>();
            }
        }

        void OnServerInitialized() => InitializePermissions();

        void InitializePermissions()
        {
            foreach (var kit in storedData.Kits.Values)
            {
                if (string.IsNullOrEmpty(kit.permission)) continue;
                permission.RegisterPermission(kit.permission, this);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Configuration
        //////////////////////////////////////////////////////////////////////////////////////////

        void OnPlayerRespawn(PlayerSession session)
        {
            if (!storedData.Kits.ContainsKey("autokit")) return;
            var thereturn = Interface.Oxide.CallHook("canRedeemKit", session);
            if (thereturn == null)
            {
                var playerinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                for(var j = 0; j < playerinv.Capacity; j++)
                {
                    if (playerinv.Items[j] == null) continue;
                    if (playerinv.Items[j].Item == null) continue;
                    Singleton<ClassInstancePool>.Instance.ReleaseInstanceExplicit(playerinv.Items[j]);
                    playerinv.Items[j] = null;
                }
                GiveKit(session, "autokit");
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Kit Creator
        //////////////////////////////////////////////////////////////////////////////////////////

        List<KitItem> GetPlayerItems(PlayerSession session)
        {
            var playerinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();

            var kititems = new List<KitItem>();
            for(var i = 0; i < playerinv.Capacity; i++)
            {
                var item = playerinv.Items[i];
                if (item?.Item == null) continue;
                kititems.Add(new KitItem
                {
                    itemid = item.Item.ItemId,
                    amount = item.StackSize,
                    slot = i
                });
            }
            return kititems;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Kit Redeemer
        //////////////////////////////////////////////////////////////////////////////////////////

        private void TryGiveKit(PlayerSession session, string kitname)
        {
            var success = CanRedeemKit(session, kitname) as string;
            if (success != null)
            {
                hurt.SendChatMessage(session, success);
                return;
            }
            success = GiveKit(session, kitname) as string;
            if (success != null)
            {
                hurt.SendChatMessage(session, success);
                return;
            }
            hurt.SendChatMessage(session, "Kit redeemed");

            ProccessKitGiven(session, kitname);
        }

        void ProccessKitGiven(PlayerSession session, string kitname)
        {
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return;

            var kitData = GetKitData(session.SteamId.m_SteamID, kitname);
            if (kit.max > 0) kitData.max += 1;

            if (kit.cooldown > 0) kitData.cooldown = CurrentTime() + kit.cooldown;
        }

        object GiveKit(PlayerSession session, string kitname)
        {
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return $"The kit '{kitname}' doesn't exist";

            var playerinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            var amanager = Singleton<AlertManager>.Instance;
            var itemmanager = Singleton<GlobalItemManager>.Instance;
            foreach (var kitem in kit.items)
            {

                if (playerinv.Items[kitem.slot] == null)
                {
                    var item = itemmanager.GetItem(kitem.itemid);
                    var iitem = new ItemInstance(item, kitem.amount);
                    playerinv.Items[kitem.slot] = iitem;
                    amanager.ItemReceivedServer(iitem.Item, iitem.StackSize, session.Player);
                    playerinv.Invalidate(false);
                }
                else
                    itemmanager.GiveItem(session.Player, itemmanager.GetItem(kitem.itemid), kitem.amount);
            }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Check Kits
        //////////////////////////////////////////////////////////////////////////////////////////

        bool isKit(string kitname) => !string.IsNullOrEmpty(kitname) && storedData.Kits.ContainsKey(kitname);

        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        bool CanSeeKit(PlayerSession session, string kitname, out string reason)
        {
            reason = string.Empty;
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return false;
            if (kit.hide) return false;
            if (kit.authlevel > 0)
                if (!session.IsAdmin) return false;
            if (!string.IsNullOrEmpty(kit.permission))
                if (!permission.UserHasPermission(session.SteamId.ToString(), kit.permission)) return false;
            if (kit.max > 0)
            {
                var left = GetKitData(session.SteamId.m_SteamID, kitname).max;
                if (left >= kit.max)
                {
                    reason += "- 0 left";
                    return false;
                }
                reason += $"- {(kit.max - left)} left";
            }
            if (kit.cooldown > 0)
            {
                var cd = GetKitData(session.SteamId.m_SteamID, kitname).cooldown;
                var ct = CurrentTime();
                if (cd > ct && cd != 0.0)
                {
                    reason += $"- {Math.Abs(Math.Ceiling(cd - ct))} seconds";
                    return false;
                }
            }
            return true;
        }

        object CanRedeemKit(PlayerSession session, string kitname)
        {
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return $"The kit '{kitname}' doesn't exist";

            var thereturn = Interface.Oxide.CallHook("canRedeemKit", session);
            if (thereturn != null)
            {
                if (thereturn is string) return thereturn;
                return "You are not allowed to redeem a kit at the moment";
            }

            if (kit.authlevel > 0)
                if (!session.IsAdmin) return "You don't have the level to use this kit";

            if (!string.IsNullOrEmpty(kit.permission))
                if (!permission.UserHasPermission(session.SteamId.ToString(), kit.permission))
                    return "You don't have the permissions to use this kit";

            var kitData = GetKitData(session.SteamId.m_SteamID, kitname);
            if (kit.max > 0)
                if (kitData.max >= kit.max) return "You already redeemed all of those kits";

            if (kit.cooldown > 0)
            {
                var ct = CurrentTime();
                if (kitData.cooldown > ct && kitData.cooldown != 0.0)
                    return $"You need to wait {Math.Abs(Math.Ceiling(kitData.cooldown - ct))} seconds to use this kit";
            }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Kit Class
        //////////////////////////////////////////////////////////////////////////////////////

        class KitItem
        {
            public int itemid;
            public int amount;
            public int slot;
        }

        class Kit
        {
            public string name;
            public string description;
            public int max;
            public double cooldown;
            public int authlevel;
            public bool hide;
            public string permission;
            public List<KitItem> items = new List<KitItem>();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Data Manager
        //////////////////////////////////////////////////////////////////////////////////////

        void SaveKitsData() => Interface.Oxide.DataFileSystem.WriteObject("Kits_Data", kitsData);

        StoredData storedData;
        Dictionary<ulong, Dictionary<string, KitData>> kitsData;

        class StoredData
        {
            public Dictionary<string, Kit> Kits = new Dictionary<string, Kit>();
        }
        class KitData
        {
            public int max;
            public double cooldown;
        }
        void ResetData()
        {
            kitsData.Clear();
            SaveKitsData();
        }

        void Unload() => SaveKitsData();
        void OnServerSave() => SaveKitsData();

        void SaveKits() => Interface.Oxide.DataFileSystem.WriteObject("Kits", storedData);

        void LoadData()
        {
            var kits = Interface.Oxide.DataFileSystem.GetFile("Kits");
            try
            {
                kits.Settings.NullValueHandling = NullValueHandling.Ignore;
                storedData = kits.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
            kits.Settings.NullValueHandling = NullValueHandling.Include;
        }

        KitData GetKitData(ulong userID, string kitname)
        {
            Dictionary<string, KitData> kitDatas;
            if (!kitsData.TryGetValue(userID, out kitDatas)) kitsData[userID] = kitDatas = new Dictionary<string, KitData>();
            KitData kitData;
            if (!kitDatas.TryGetValue(kitname, out kitData)) kitDatas[kitname] = kitData = new KitData();
            return kitData;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Kit Editor
        //////////////////////////////////////////////////////////////////////////////////////

        readonly Dictionary<ulong, string> kitEditor = new Dictionary<ulong, string>();

        //////////////////////////////////////////////////////////////////////////////////////
        // Console Command
        //////////////////////////////////////////////////////////////////////////////////////

        List<PlayerSession> FindPlayer(string arg)
        {
            var listPlayers = new List<PlayerSession>();

            ulong steamid;
            ulong.TryParse(arg, out steamid);
            var lowerarg = arg.ToLower();

            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var session = pair.Value;
                if (!session.IsLoaded) continue;
                if (steamid != 0L)
                    if (session.SteamId.m_SteamID == steamid)
                    {
                        listPlayers.Clear();
                        listPlayers.Add(session);
                        return listPlayers;
                    }
                var lowername = session.Name.ToLower();
                if (lowername.Contains(lowerarg)) listPlayers.Add(session);
            }
            return listPlayers;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Command
        //////////////////////////////////////////////////////////////////////////////////////

        bool HasAccess(PlayerSession session) => session.IsAdmin;

        void SendListKitEdition(PlayerSession session)
        {
            hurt.SendChatMessage(session, "permission \"permission name\" => set the permission needed to get this kit");
            hurt.SendChatMessage(session, "description \"description text here\" => set a description for this kit");
            hurt.SendChatMessage(session, "authlevel XXX");
            hurt.SendChatMessage(session, "cooldown XXX");
            hurt.SendChatMessage(session, "max XXX");
            hurt.SendChatMessage(session, "items => set new items for your kit (will copy your inventory)");
            hurt.SendChatMessage(session, "hide TRUE/FALSE => dont show this kit in lists (EVER)");
        }

        [ChatCommand("kit")]
        void cmdKit(PlayerSession session, string command, string[] args)
        {
            if (args.Length == 0)
            {
                var reason = string.Empty;
                foreach (var pair in storedData.Kits)
                {
                    var cansee = CanSeeKit(session, pair.Key, out reason);
                    if (!cansee && string.IsNullOrEmpty(reason)) continue;
                    hurt.SendChatMessage(session, $"{pair.Value.name} - {pair.Value.description} {reason}");
                }
                return;
            }
            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "help":
                        hurt.SendChatMessage(session, "====== Player Commands ======");
                        hurt.SendChatMessage(session, "/kit => to get the list of kits");
                        hurt.SendChatMessage(session, "/kit KITNAME => to redeem the kit");
                        if (!HasAccess(session)) return;
                        hurt.SendChatMessage(session, "====== Admin Commands ======");
                        hurt.SendChatMessage(session, "/kit add KITNAME => add a kit");
                        hurt.SendChatMessage(session, "/kit remove KITNAME => remove a kit");
                        hurt.SendChatMessage(session, "/kit edit KITNAME => edit a kit");
                        hurt.SendChatMessage(session, "/kit list => get a raw list of kits (the real full list)");
                        hurt.SendChatMessage(session, "/kit give PLAYER/STEAMID KITNAME => give a kit to a player");
                        hurt.SendChatMessage(session, "/kit resetkits => deletes all kits");
                        hurt.SendChatMessage(session, "/kit resetdata => reset player data");
                        break;
                    case "add":
                    case "remove":
                    case "edit":
                        if (!HasAccess(session)) { hurt.SendChatMessage(session, "You don't have access to this command"); return; }
                        hurt.SendChatMessage(session,  $"/kit {args[0]} KITNAME");
                        break;
                    case "give":
                        if (!HasAccess(session)) { hurt.SendChatMessage(session, "You don't have access to this command"); return; }
                        hurt.SendChatMessage(session, "/kit give PLAYER/STEAMID KITNAME");
                        break;
                    case "list":
                        if (!HasAccess(session)) { hurt.SendChatMessage(session, "You don't have access to this command"); return; }
                        foreach (var kit in storedData.Kits.Values) hurt.SendChatMessage(session, $"{kit.name} - {kit.description}");
                        break;
                    case "items":
                        break;
                    case "resetkits":
                        if (!HasAccess(session)) { hurt.SendChatMessage(session, "You don't have access to this command"); return; }
                        storedData.Kits.Clear();
                        kitEditor.Clear();
                        ResetData();
                        SaveKits();
                        hurt.SendChatMessage(session, "Resetted all kits and player data");
                        break;
                    case "resetdata":
                        if (!HasAccess(session)) { hurt.SendChatMessage(session, "You don't have access to this command"); return; }
                        ResetData();
                        hurt.SendChatMessage(session, "Resetted all player data");
                        break;
                    default:
                        TryGiveKit(session, args[0].ToLower());
                        break;
                }
                if (args[0] != "items") return;

            }
            if (!HasAccess(session)) { hurt.SendChatMessage(session, "You don't have access to this command"); return; }

            string kitname;
            switch (args[0])
            {
                case "add":
                    kitname = args[1].ToLower();
                    if (storedData.Kits.ContainsKey(kitname))
                    {
                        hurt.SendChatMessage(session, "This kit already exists.");
                        return;
                    }
                    storedData.Kits[kitname] = new Kit { name = args[1] };
                    kitEditor[session.SteamId.m_SteamID] = kitname;
                    hurt.SendChatMessage(session, "You've created a new kit: " + args[1]);
                    SendListKitEdition(session);
                    break;
                case "give":
                    if (args.Length < 3)
                    {
                        hurt.SendChatMessage(session, "/kit give PLAYER/STEAMID KITNAME");
                        return;
                    }
                    kitname = args[2].ToLower();
                    if (!storedData.Kits.ContainsKey(kitname))
                    {
                        hurt.SendChatMessage(session, "This kit doesn't seem to exist.");
                        return;
                    }
                    var findPlayers = FindPlayer(args[1]);
                    if (findPlayers.Count == 0)
                    {
                        hurt.SendChatMessage(session, "No players found.");
                        return;
                    }
                    if (findPlayers.Count > 1)
                    {
                        hurt.SendChatMessage(session, "Multiple players found.");
                        return;
                    }
                    GiveKit(findPlayers[0], kitname);
                    hurt.SendChatMessage(session, $"You gave {findPlayers[0].Name} the kit: {storedData.Kits[kitname].name}");
                    hurt.SendChatMessage(findPlayers[0], string.Format("You've received the kit {1} from {0}", session.Name, storedData.Kits[kitname].name));
                    break;
                case "edit":
                    kitname = args[1].ToLower();
                    if (!storedData.Kits.ContainsKey(kitname))
                    {
                        hurt.SendChatMessage(session, "This kit doesn't seem to exist");
                        return;
                    }
                    kitEditor[session.SteamId.m_SteamID] = kitname;
                    hurt.SendChatMessage(session, $"You are now editing the kit: {kitname}");
                    SendListKitEdition(session);
                    break;
                case "remove":
                    kitname = args[1].ToLower();
                    if (!storedData.Kits.Remove(kitname))
                    {
                        hurt.SendChatMessage(session, "This kit doesn't seem to exist");
                        return;
                    }
                    hurt.SendChatMessage(session, $"{kitname} was removed");
                    if (kitEditor[session.SteamId.m_SteamID] == kitname) kitEditor.Remove(session.SteamId.m_SteamID);
                    break;
                default:
                    if (!kitEditor.TryGetValue(session.SteamId.m_SteamID, out kitname))
                    {
                        hurt.SendChatMessage(session, "You are not creating or editing a kit");
                        return;
                    }
                    Kit kit;
                    if (!storedData.Kits.TryGetValue(kitname, out kit))
                    {
                        hurt.SendChatMessage(session, "There was an error while getting this kit, was it changed while you were editing it?");
                        return;
                    }
                    for (var i = 0; i < args.Length; i++)
                    {
                        object editvalue;
                        var key = args[i].ToLower();
                        switch (key)
                        {
                            case "items":
                                kit.items = GetPlayerItems(session);
                                hurt.SendChatMessage(session, "The items were copied from your inventory");
                                continue;
                            case "name":
                                continue;
                            case "description":
                                editvalue = kit.description = args[++i];
                                break;
                            case "max":
                                editvalue = kit.max = int.Parse(args[++i]);
                                break;
                            case "cooldown":
                                editvalue = kit.cooldown = double.Parse(args[++i]);
                                break;
                            case "authlevel":
                                editvalue = kit.authlevel = int.Parse(args[++i]);
                                break;
                            case "hide":
                                editvalue = kit.hide = bool.Parse(args[++i]);
                                break;
                            case "permission":
                                editvalue = kit.permission = args[++i];
                                InitializePermissions();
                                break;
                            default:
                                hurt.SendChatMessage(session, $"{args[i]} is not a valid argument");
                                    continue;
                        }
                        hurt.SendChatMessage(session, $"{key} set to {editvalue ?? "null"}");
                    }
                    break;
            }
            SaveKits();
        }
    }
}
