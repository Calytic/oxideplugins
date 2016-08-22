using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("PrivateZones", "k1lly0u", "0.1.3", ResourceId = 1703)]
    class PrivateZones : RustPlugin
    {
        [PluginReference]
        Plugin ZoneManager;
		
		[PluginReference]
		Plugin PopupNotifications;

        private bool Changed;

        ZoneDataStorage data;
        private DynamicConfigFile ZoneData;

        private static LayerMask GROUND_MASKS = LayerMask.GetMask("Terrain", "World", "Construction");

        #region oxide hooks
        void Loaded()
        {
            permission.RegisterPermission("privatezones.admin", this);
            lang.RegisterMessages(messages, this);
            ZoneData = Interface.Oxide.DataFileSystem.GetFile("privatezone_data");
        }
        void OnServerInitialized()
        {
            LoadData();
            InitPerms();
        }
        void InitPerms()
        {
            foreach (var entry in data.zones)
            {
                permission.RegisterPermission(entry.Value, this);
            }
        }
        void Unload()
        {
            SaveData();
        }
        #endregion

        #region functions
        private void TPPlayer(BasePlayer player, Vector3 pos)
        {
            player.MovePosition(pos);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
            player.TransformChanged();
            player.SendNetworkUpdateImmediate();
        }
        private Vector3 CalculateOutsidePos(BasePlayer player, string zoneID)
        {
            float distance = 0;
			Vector3 zonePos = (Vector3) ZoneManager?.Call("GetZoneLocation", new object[] { zoneID });
			object zoneRadius = ZoneManager?.Call("GetZoneRadius", new object[] { zoneID });
            Vector3 zoneSize = (Vector3) ZoneManager?.Call("GetZoneSize", new object[] { zoneID });
			var playerPos = player.transform.position;
            var cachedDirection = playerPos - zonePos;
			if (zoneSize != Vector3.zero)
                distance = zoneSize.x > zoneSize.z ? zoneSize.x : zoneSize.z;
            else
				distance = (float)zoneRadius;
			
			var newPos = zonePos + (cachedDirection / cachedDirection.magnitude * (distance + 2f));
            newPos.y = TerrainMeta.HeightMap.GetHeight(newPos);
            return newPos;
        }
        static Vector3 CalculateGroundPos(Vector3 sourcePos) // credit Wulf & Nogrod
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, GROUND_MASKS))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }

        #endregion
        #region zonemanager hooks
        //////////////////////////////////////////////////////////////////////////////////////
        // ZoneManager Hooks /////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
               
        void OnEnterZone(string ZoneID, BasePlayer player)
        {            
            if (player == null || string.IsNullOrEmpty(ZoneID)) return;
            if (player.IsSleeping()) return; 
            if (data.zones.ContainsKey(ZoneID))
            {
                string perm = data.zones[ZoneID];
                if (permission.UserHasPermission(player.userID.ToString(), perm) || isAuth(player)) return;                
				if (PopupNotifications)
                    PopupNotifications?.Call("CreatePopupNotification", lang.GetMessage("noPerms", this, player.UserIDString), player);
                else SendMsg(player, lang.GetMessage("noPerms", this, player.UserIDString));
                Vector3 newPos = CalculateOutsidePos(player, ZoneID);
                TPPlayer(player, newPos);
            }
        }
        #endregion

        #region chat commands
        [ChatCommand("pz")]
        private void cmdPZ(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player)) return;
            if (args == null || args.Length == 0)
            {
                SendReply(player, lang.GetMessage("synAdd", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synRem", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synList", this, player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "add":
                    if (args.Length == 3)
                    {
                        object zoneid = ZoneManager.Call("CheckZoneID", new object[] { args[1] });

                        if (zoneid is string && (string)zoneid != "")
                        {
                            string perm = args[2].ToLower();
                            if (!perm.StartsWith("privatezones."))
                                perm = "privatezones." + perm;
                            Puts(perm);

                            data.zones.Add((string)zoneid, perm);

                            SendMsg(player, string.Format(lang.GetMessage("newZone", this, player.UserIDString), (string)zoneid, perm));
                            permission.RegisterPermission(perm, this);
                            SaveData();
                            return;
                        }
                        SendMsg(player, lang.GetMessage("invID", this, player.UserIDString));
                        return;
                    }
                    SendMsg(player, lang.GetMessage("synError", this, player.UserIDString));
                    return;
                case "remove":
                    if (args.Length == 2)
                    {
                        if (data.zones.ContainsKey(args[1].ToLower()))
                        {
                            data.zones.Remove(args[1].ToLower());
                            SendMsg(player, string.Format(lang.GetMessage("remZone", this, player.UserIDString), args[1]));
                            SaveData();
                            return;
                        }
                        SendMsg(player, lang.GetMessage("invID", this, player.UserIDString));
                        return;
                    }
                    SendMsg(player, lang.GetMessage("synError", this, player.UserIDString));
                    return;
                case "list":
                    foreach(var entry in data.zones)
                    {
                        SendReply(player, string.Format(lang.GetMessage("list", this, player.UserIDString), entry.Key, entry.Value));
                    }
                    return;
            }
        }
        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
                if (player.net.connection.authLevel != 2) return false;
            return true;
        }
        bool hasPermission(BasePlayer player)
        {
            if (isAuth(player)) return true;
            else if (permission.UserHasPermission(player.userID.ToString(), "privatezones.admin")) return true;
            return false;
        }
        #endregion

        #region data
        //////////////////////////////////////////////////////////////////////////////////////
        // Data Management ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void SaveData()
        {
            ZoneData.WriteObject(data);
        }
        void LoadData()
        {
            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<ZoneDataStorage>("privatezone_data");
            }
            catch
            {
                data = new ZoneDataStorage();
            }
        }
        class ZoneDataStorage
        {
            public Dictionary<string, string> zones = new Dictionary<string, string>();
            public ZoneDataStorage() { }
        }
        #endregion
        #region messages
        private void SendMsg(BasePlayer player, string msg)
        {
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("MsgColor", this, player.UserIDString) + msg + "</color>");
        }
        private Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "<color=#afff00>PrivateZones:</color> " },
            {"list", "ZoneID: {0}, Permission: {1}" },
            {"synError", "Syntax Error" },
            {"invID", "Invalid ZoneID" },
            {"remZone", "Removed Zone: {0}" },
            {"newZone", "Created new private zone for ZoneID: {0}, using permission: {1}" },
            {"synAdd", "/pz add <zoneid> <permission>" },
            {"synRem", "/pz remove <zoneid>" },
            {"synList", "/pz list" },
            {"noPerms", "You don't have permission to enter this zone, bought a HL Pass or become a VIP." },
            {"MsgColor", "<color=#d3d3d3>" }
        };
        #endregion

    }
}
