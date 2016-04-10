using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Facepunch;
using Oxide.Core;

namespace Oxide.Plugins
{ 
    [Info("Building Owners", "Reneb", "2.1.4")]
    class BuildingOwners : RustPlugin
    {
    	private static DateTime epoch;
        private Core.Configuration.DynamicConfigFile ReverseData;
        private Dictionary<float,string> OwnersData;
        private FieldInfo keyvalues;
        private static bool serverInitialized;
        void Loaded() 
        {
            serverInitialized = false;
            OwnersData = new Dictionary<float, string>(); 
        	epoch = new System.DateTime(1970, 1, 1);
            keyvalues = typeof(Oxide.Core.Configuration.DynamicConfigFile).GetField("keyvalues", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }
        void OnServerInitialized()
        {
            ReverseData = Interface.GetMod().DataFileSystem.GetDatafile("BuildingOwners");
            serverInitialized = true;
            ReverseTable();
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("BuildingOwners");
        }
        void ReverseTable()
        {
            //var Table = keyvalues.GetValue(ReverseData) as Dictionary<string, object>;

            foreach (KeyValuePair<string, object> pair in ReverseData)
            {
                var list = pair.Value as List<object>;
                foreach (object heights in list)
                {
                    if (!(OwnersData.ContainsKey(Convert.ToSingle(heights))))
                    {
                        OwnersData.Add(Convert.ToSingle(heights), pair.Key.ToString());
                    }
                }
            }
        }
        double CurrentTime()
        {
            return System.DateTime.UtcNow.Subtract(epoch).TotalMilliseconds;
        }
        object FindBuilding(BasePlayer player)
        {
            var hits = UnityEngine.Physics.RaycastAll(player.transform.position, new Vector3(0f, -1f, 0f));
            foreach (var hit in hits)
            {
                BaseEntity ent = hit.GetEntity();
                if (ent != null)
                {
                    if(ent.GetComponent<BuildingBlock>() != null)
                        return ent.GetComponent<BuildingBlock>();
                }
            }
            return false;
        }
        void OnEntityBuilt(HeldEntity heldentity, GameObject gameobject)
        {
            if (serverInitialized)
            {
                var buildingblock = gameobject.GetComponent<BuildingBlock>();
                if (buildingblock == null) return;
                float posy = buildingblock.transform.position.y;
                if (!(OwnersData.ContainsKey(posy)))
                {
                    string userid = heldentity.ownerPlayer.userID.ToString();
                    OwnersData.Add(posy, userid);
                    if (ReverseData[userid] == null)
                        ReverseData[userid] = new List<object>();
                    var list = ReverseData[userid] as List<object>;
                    list.Add(posy);
                    ReverseData[userid] = list;
                }
            }
        }
        object FindBlockData(BuildingBlock block)
        {
            float posy = block.transform.position.y;
            if (OwnersData.ContainsKey(posy))
                return OwnersData[posy];
            return false;
        }
        [ChatCommand("changeowner")]
        void cmdChatchangeowner(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 1)
            {
                SendReply(player, "You are not allowed to use this command");
                return; 
            }
            if (args == null || args.Length < 1)
            {
                SendReply(player, "You need to give the name of the new owner");
                return;
            }
            var target = BasePlayer.Find(args[0].ToString());
            if (target == null || target.net == null || target.net.connection == null)
            {
                SendReply(player, "Target player not found");
            }
            else
            {
                object block = FindBuilding(player);
                if ( block is bool )
                {
                    SendReply(player, "No Building found.");
                }
                else
                {
                    var userid = target.userID.ToString();
                    BuildingBlock buildingblock = (BuildingBlock)block;
                    List<BuildingBlock> houseList = new List<BuildingBlock>();
                    List<Vector3> checkFrom = new List<Vector3>();
                    houseList.Add(buildingblock);
                    checkFrom.Add(buildingblock.transform.position);
                    var current = 0;
                    while (true)
                    {
                        current++;
                        if (current > checkFrom.Count)
                            break;
                        List<BaseEntity> list = Pool.GetList<BaseEntity>();
                        Vis.Entities<BaseEntity>(checkFrom[current-1], 3f, list);
                        for (int i = 0; i < list.Count; i++)
                        {
                            BaseEntity ent = list[i];
                            BuildingBlock fbuildingblock = ent.GetComponent<BuildingBlock>();
                            if (fbuildingblock != null)
                            {
                                if (!(houseList.Contains(fbuildingblock)))
                                {
                                    houseList.Add(fbuildingblock);
                                    checkFrom.Add(fbuildingblock.transform.position);
                                    if (!(OwnersData.ContainsKey(fbuildingblock.transform.position.y)))
                                    {
                                        OwnersData.Add(fbuildingblock.transform.position.y, userid);
                                    }
                                    else
                                    {
                                        OwnersData[fbuildingblock.transform.position.y] = userid;
                                    }
                                    if (ReverseData[userid] == null)
                                        ReverseData[userid] = new List<object>();
                                    var listt = ReverseData[userid] as List<object>;
                                    if (!(listt.Contains(fbuildingblock.transform.position.y)))
                                    {
                                        listt.Add(fbuildingblock.transform.position.y);
                                        ReverseData[userid] = listt;
                                    }
                                }
                            }
                        }
                    }
                    SendReply(player, string.Format("New owner of this house is: {0}",target.displayName));
                    SendReply(target, "An admin gave you the ownership of this house");
                }
            }
        }
        void OnServerSave()
        {
            SaveData();
        }
        void OnServerQuit()
        {
            SaveData();
        }
    }
}