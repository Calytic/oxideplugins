// Reference: Oxide.Ext.RustLegacy
// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;


namespace Oxide.Plugins
{
    [Info("StructureLimiter", "Reneb", "1.0.1")]
    class StructureLimiter : RustLegacyPlugin
    {
        void Loaded()
        {
            LoadData();
            structureComponents = typeof(StructureMaster).GetField("_structureComponents", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }
        void LoadData() { Data = Interface.GetMod().DataFileSystem.GetDatafile("StructureLimiter"); }
        void SaveData() { Interface.GetMod().DataFileSystem.SaveDatafile("StructureLimiter"); }
        void OnServerSave() { SaveData(); }
        void Unload() { SaveData(); }

        int DefaultAllowedEntities = 1000;
        int DefaultWarningEntities = 20;

        private static FieldInfo structureComponents;

        Core.Configuration.DynamicConfigFile Data;


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
            CheckCfg<int>("Settings: Defaut Allowed Entities Per User", ref DefaultAllowedEntities);
            CheckCfg<int>("Settings: Warn Players when Only X Entities are left", ref DefaultWarningEntities);

            SaveConfig();
        }


        Hash<string, List<StructureMaster>> UserStructures = new Hash<string, List<StructureMaster>>();
        Hash<StructureMaster, int> StructureEntityCount = new Hash<StructureMaster, int>();

        Vector3 Vector3Down = new Vector3(0f, -1f, 0f);
        Vector3 Vector3ABitUp = new Vector3(0f, 0.1f, 0f);

        RaycastHit cachedRaycast;
        bool cachedBoolean;
        Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
        StructureMaster cachedMaster;
        StructureComponent cachedComponent;
        int cachedCount;
        List<StructureMaster> cachedMasterlist;

        int GetAllowedEntities( string userid )
        {
            if (Data[userid] == null)
                return DefaultAllowedEntities;
            return (int)Data[userid];
        }
        void SetAllowedEntities(string userid, int newnumber)
        {
            Data[userid] = newnumber;
        }
        void AddAllowedEntities(string userid, int newnumber)
        {
            if (Data[userid] == null)
                Data[userid] = DefaultAllowedEntities;
            Data[userid] = (int)Data[userid] + newnumber;
        }
        void OnServerInitialized()
        {
            foreach(StructureMaster master in StructureMaster.AllStructures)
            {
                StructureEntityCount[master] = ((HashSet<StructureComponent>)structureComponents.GetValue(master)).Count;
                AddStructureMaster(master);
            }
        }
        void AddStructureMaster(StructureMaster master)
        {
            if (UserStructures[master.ownerID.ToString()] == null)
                UserStructures[master.ownerID.ToString()] = new List<StructureMaster>();
            if(!UserStructures[master.ownerID.ToString()].Contains(master))
                UserStructures[master.ownerID.ToString()].Add(master);
        } 
        List<StructureMaster> GetStructureMasters(string userid)
        {
            if (UserStructures[userid] == null)
                UserStructures[userid] = new List<StructureMaster>();
            return UserStructures[userid];
        }
        void CheckIfCanPlace(PlayerClient player, string userid, StructureComponent component)
        {
            cachedMasterlist = GetStructureMasters(userid);
            cachedCount = 0;
            foreach(StructureMaster master in cachedMasterlist)
            { 
                if (StructureEntityCount[master] == null)
                    StructureEntityCount[master] = ((HashSet<StructureComponent>)structureComponents.GetValue(master)).Count;
                cachedCount += StructureEntityCount[master];
            }
            if(cachedCount > GetAllowedEntities(userid))
            {
                timer.Once(0.05f, () => DestroyEntity(component.GetComponent<IDBase>()));
                ConsoleNetworker.SendClientCommand(player.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe("The owner has no more entities allowed on his structures"));
                return;
            }
            else if(cachedCount > (GetAllowedEntities(userid) - DefaultWarningEntities))
            {
                ConsoleNetworker.SendClientCommand(player.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(string.Format("{0} elements left to place", (GetAllowedEntities(userid) - cachedCount).ToString())));
            }
            timer.Once(0.1f, () => AddToComponentMasterOneElement(component));
        }
        void AddToComponentMasterOneElement(StructureComponent component)
        {
            if (component == null) return;
            if (component.gameObject == null) return;
            StructureEntityCount[component._master] += 1;
            AddStructureMaster(component._master);
        }
        void DestroyEntity(IDBase idbase)
        {
            if (idbase != null)
                TakeDamage.KillSelf(idbase);
        }
        void OnStructurePlaced(StructureComponent component, IStructureComponentItem structureComponentItem)
        {
            var currettime = Time.realtimeSinceStartup;
            if (MeshBatchPhysics.Raycast(component.transform.position + Vector3ABitUp, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance))
            {
                if(cachedhitInstance!=null)
                {
                    cachedComponent = cachedhitInstance.physicalColliderReferenceOnly.GetComponent<StructureComponent>();
                    if(cachedComponent != null)
                    {
                        cachedMaster = cachedComponent._master;
                        CheckIfCanPlace(structureComponentItem.character.playerClient, cachedMaster.ownerID.ToString(), component);
                    }
                }
            }
            else
            {
                CheckIfCanPlace(structureComponentItem.character.playerClient, structureComponentItem.character.playerClient.userID.ToString(), component);
            }
            SendReply(structureComponentItem.character.playerClient.netUser, "Took: " + (currettime - Time.realtimeSinceStartup).ToString());
        }

        [ChatCommand("structurelimiter")]
        void cmdChatStructureLimiter(NetUser netuser, string command, string[] args)
        {
            if(args.Length == 0)
            {
                cachedMasterlist = GetStructureMasters(netuser.playerClient.userID.ToString());
                cachedCount = 0;
                foreach (StructureMaster master in cachedMasterlist)
                {
                    if (StructureEntityCount[master] == null)
                        StructureEntityCount[master] = ((HashSet<StructureComponent>)structureComponents.GetValue(master)).Count;
                    cachedCount += StructureEntityCount[master];
                } 
                SendReply(netuser, string.Format("You have used {0}/{1} elements", cachedCount.ToString(), GetAllowedEntities(netuser.playerClient.userID.ToString()).ToString()));
                return;
            }
            if (!netuser.CanAdmin())
            {
                SendReply(netuser, "You are not allowed to use this command.");
                return;
            }
            string steamid = string.Empty;
            string name = string.Empty;
            ulong userid;
            if (args[0].Length == 17 && ulong.TryParse(args[0], out userid))
            {
                steamid = args[0];
            }
            else
            {
                var targetplayer = rust.FindPlayer(args[0]);
                if(targetplayer == null)
                {
                    SendReply(netuser, "No players found");
                    return;
                }
                steamid = ((NetUser)targetplayer).playerClient.userID.ToString();
                name = ((NetUser)targetplayer).displayName;
            }
            if(args.Length == 1)
            {
                cachedMasterlist = GetStructureMasters(steamid);
                cachedCount = 0;
                foreach (StructureMaster master in cachedMasterlist)
                { 
                    if (StructureEntityCount[master] == null)
                        StructureEntityCount[master] = ((HashSet<StructureComponent>)structureComponents.GetValue(master)).Count;
                    cachedCount += StructureEntityCount[master];
                }
                SendReply(netuser, string.Format("{0} used {1}/{2} elements", (name == string.Empty) ? steamid : name, cachedCount.ToString(), GetAllowedEntities(steamid).ToString()));
                return;
            }
            int newelements = 0;
            if(!int.TryParse(args[1], out newelements))
            {
                SendReply(netuser, "/structurelimiter PLAYER NEWELEMENTS");
                return;
            }
            SetAllowedEntities(steamid, newelements);
            SendReply(netuser, string.Format("{0} has now {1} elements allowed", (name == string.Empty) ? steamid : name, newelements.ToString()));
        }
    }
}
