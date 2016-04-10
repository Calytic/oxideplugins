// Reference: Oxide.Ext.RustLegacy
// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers


using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Copy Paste", "Reneb", "1.0.0")]
    class CopyPaste : RustLegacyPlugin
    {

        PropertyInfo collection;
        FieldInfo structurecomponents;
        /// CACHED VARIABLES
        public static RaycastHit cachedRaycast;
        public static Vector3 cachedPosition;
        public static StructureMaster cachedMaster;
        public static Collider cachedCollider;
        public static string cachedModelname;
        public static string cachedObjectname;
        public static float cachedDistance;
        public static StructureComponent cachedComponent;
        public static Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
        public static bool cachedBoolean;



        private Vector3 transformedPos;
        private Vector3 normedPos;
        private Quaternion currentRot;
        private float normedYRot;
        private float newX;
        private float newZ;
        private Dictionary<string, object> posCleanData;
        private Dictionary<string, object> rotCleanData;
        private List<object> rawStructure;
        private List<object> rawDeployables;
        private List<object> rawSpawnables;
        private float heightAdjustment;
        private string filename;
        private object closestEnt;
        private Vector3 closestHitpoint;
        private string cleanDeployedName;


        private Dictionary<string, string> GameObjectToPrefab = new Dictionary<string, string>();
        private Dictionary<string, ItemDataBlock> displaynameToDataBlock = new Dictionary<string, ItemDataBlock>();

        void OnServerInitialized()
        {
              structurecomponents = typeof(StructureMaster).GetField("_structureComponents", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
            InitializeTable();
            GameObjectToPrefab.Add("WoodFoundation(Clone)", ";struct_wood_foundation");
            GameObjectToPrefab.Add("WoodDoorFrame(Clone)", ";struct_wood_doorway");
            GameObjectToPrefab.Add("WoodWall(Clone)", ";struct_wood_wall");
            GameObjectToPrefab.Add("WoodCeiling(Clone)", ";struct_wood_ceiling");
            GameObjectToPrefab.Add("WoodWindowFrame(Clone)", ";struct_wood_windowframe");
            GameObjectToPrefab.Add("WoodRamp(Clone)", ";struct_wood_ramp");
            GameObjectToPrefab.Add("WoodStairs(Clone)", ";struct_wood_stairs");
            GameObjectToPrefab.Add("WoodPillar(Clone)", ";struct_wood_pillar");
            GameObjectToPrefab.Add("MetalFoundation(Clone)", ";struct_metal_foundation");
            GameObjectToPrefab.Add("MetalWall(Clone)", ";struct_metal_wall");
            GameObjectToPrefab.Add("MetalDoorFrame(Clone)", ";struct_metal_doorframe");
            GameObjectToPrefab.Add("MetalCeiling(Clone)", ";struct_metal_ceiling");
            GameObjectToPrefab.Add("MetalStairs(Clone)", ";struct_metal_stairs");
            GameObjectToPrefab.Add("MetalWindowFrame(Clone)", ";struct_metal_windowframe");
            GameObjectToPrefab.Add("MetalRamp(Clone)", ";struct_metal_ramp");
            GameObjectToPrefab.Add("MetalPillar(Clone)", ";struct_metal_pillar");

            GameObjectToPrefab.Add("WoodBoxLarge(Clone)", ";deploy_wood_storage_large");
            GameObjectToPrefab.Add("WoodBox(Clone)", ";deploy_wood_box");
            GameObjectToPrefab.Add("SmallStash(Clone)", ";deploy_small_stash");
            GameObjectToPrefab.Add("Wood_Shelter(Clone)", ";deploy_wood_shelter");
            GameObjectToPrefab.Add("Campfire(Clone)", ";deploy_camp_bonfire");
            GameObjectToPrefab.Add("Furnace(Clone)", ";deploy_furnace");
            GameObjectToPrefab.Add("Workbench(Clone)", ";deploy_workbench");
            GameObjectToPrefab.Add("SleepingBagA(Clone)", ";deploy_camp_sleepingbag");
            GameObjectToPrefab.Add("SingleBed(Clone)", ";deploy_singlebed");
            GameObjectToPrefab.Add("RepairBench(Clone)", ";deploy_repairbench");
            GameObjectToPrefab.Add("LargeWoodSpikeWall(Clone)", ";deploy_largewoodspikewall");
            GameObjectToPrefab.Add("WoodSpikeWall(Clone)", ";deploy_woodspikewall");
            GameObjectToPrefab.Add("Barricade_Fence_Deployable(Clone)", ";deploy_wood_barricade");
            GameObjectToPrefab.Add("WoodGateway(Clone)", ";deploy_woodgateway");
            GameObjectToPrefab.Add("WoodGate(Clone)", ";deploy_woodgate");
            GameObjectToPrefab.Add("WoodenDoor(Clone)", ";deploy_wood_door");
            GameObjectToPrefab.Add("MetalDoor(Clone)", ";deploy_metal_door");
            GameObjectToPrefab.Add("MetalBarsWindow(Clone)", ";deploy_metalwindowbars");

        }
        private void InitializeTable()
        {
            displaynameToDataBlock.Clear();
            foreach (ItemDataBlock itemdef in DatablockDictionary.All)
            {
                displaynameToDataBlock.Add(itemdef.name.ToString(), itemdef);
            }
        }
        bool TryGetClosestRayPoint(Vector3 sourcePos, Quaternion sourceDir, out object closestEnt, out Vector3 closestHitpoint)
        {
            closestHitpoint = default(Vector3);
            closestEnt = null;
            Ray ray = new Ray(sourcePos, sourceDir * Vector3.forward);

            if (!MeshBatchPhysics.Raycast(ray, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) return false;
            if (cachedhitInstance == null) return false;
            closestHitpoint = cachedRaycast.point;
            closestEnt = cachedhitInstance.physicalColliderReferenceOnly;
            return true;
        }
        bool TryGetClosestPoint(Vector3 sourcePos, Quaternion sourceDir, out object closestEnt, out Vector3 closestHitpoint)
        {
            closestHitpoint = default(Vector3);
            closestEnt = null;
            Ray ray = new Ray(sourcePos, sourceDir * Vector3.forward);

            if (!MeshBatchPhysics.Raycast(ray, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) return false;
            closestHitpoint = cachedRaycast.point;
            closestEnt = cachedRaycast.collider;
            return true;
        }

        bool hasAccess(NetUser player)
        {
            if (!player.CanAdmin())
            {
                SendReply(player, "You are not allowed to use this command");
                return false;
            }
            return true;
        }

        bool TryGetPlayerView(NetUser player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);
            if (player.playerClient.rootControllable == null) return false;
            viewAngle = player.playerClient.rootControllable.GetComponent<Character>().eyesRotation;
            return true;
        }

        Vector3 GenerateGoodPos(Vector3 InitialPos, Vector3 CurrentPos, float diffRot)
        {
            transformedPos = CurrentPos - InitialPos;
            newX = (transformedPos.x * (float)Math.Cos(-diffRot)) + (transformedPos.z * (float)Math.Sin(-diffRot));
            newZ = (transformedPos.z * (float)Math.Cos(-diffRot)) - (transformedPos.x * (float)Math.Sin(-diffRot));
            transformedPos.x = newX;
            transformedPos.z = newZ;
            return transformedPos;
        }

        bool GetStructureClean(StructureComponent initialBlock, float playerRot, StructureComponent currentBlock, out Dictionary<string, object> data)
        {
            
            data = new Dictionary<string, object>();
            posCleanData = new Dictionary<string, object>(); 
            rotCleanData = new Dictionary<string, object>();
            if (!GameObjectToPrefab.ContainsKey(currentBlock.gameObject.name)) return false;
            normedPos = GenerateGoodPos(initialBlock.transform.position, currentBlock.transform.position, playerRot);
            normedYRot = currentBlock.transform.rotation.ToEulerAngles().y - playerRot;

            data.Add("prefabname", GameObjectToPrefab[currentBlock.gameObject.name]);

            posCleanData.Add("x", normedPos.x);
            posCleanData.Add("y", normedPos.y);
            posCleanData.Add("z", normedPos.z);
            data.Add("pos", posCleanData);

            rotCleanData.Add("x", currentBlock.transform.rotation.ToEulerAngles().x);
            rotCleanData.Add("y", normedYRot);
            rotCleanData.Add("z", currentBlock.transform.rotation.ToEulerAngles().z);
            data.Add("rot", rotCleanData);
            return true;
        }

        bool GetDeployableClean(StructureComponent initialBlock, float playerRot, DeployableObject currentBlock, out Dictionary<string, object> data)
        {

            data = new Dictionary<string, object>();
            posCleanData = new Dictionary<string, object>();
            rotCleanData = new Dictionary<string, object>();

            normedPos = GenerateGoodPos(initialBlock.transform.position, currentBlock.transform.position, playerRot);
            normedYRot = currentBlock.transform.rotation.ToEulerAngles().y - playerRot;
            data.Add("prefabname", GameObjectToPrefab[currentBlock.gameObject.name]);

            posCleanData.Add("x", normedPos.x);
            posCleanData.Add("y", normedPos.y);
            posCleanData.Add("z", normedPos.z);
            data.Add("pos", posCleanData);

            rotCleanData.Add("x", currentBlock.transform.rotation.ToEulerAngles().x);
            rotCleanData.Add("y", normedYRot);
            rotCleanData.Add("z", currentBlock.transform.rotation.ToEulerAngles().z);
            data.Add("rot", rotCleanData);
            return true;
        }

        object CopyBuilding(Vector3 playerPos, float playerRot, StructureComponent initialBlock, out List<object> rawStructure, out List<object> rawDeployables)
        {
            rawStructure = new List<object>();
            rawDeployables = new List<object>();
            rawSpawnables = new List<object>();
            List<object> houseList = new List<object>();
            List<Vector3> checkFrom = new List<Vector3>();
            StructureComponent fbuildingblock;
            DeployableObject fdeployable;
            IInventoryItem item;

            houseList.Add(initialBlock);
            checkFrom.Add(initialBlock.transform.position);

            Dictionary<string, object> housedata;
            if (!GetStructureClean(initialBlock, playerRot, initialBlock, out housedata))
            {
                return "Couldn't get a clean initial block";
            }
            rawStructure.Add(housedata);

            int current = 0;
            while (true)
            {
                current++;
                if (current > checkFrom.Count)
                    break;
                foreach (var hit in MeshBatchPhysics.OverlapSphere(checkFrom[current - 1], 5f))
                {
                    if (hit.GetComponentInParent<StructureComponent>() != null)
                    {
                        fbuildingblock = hit.GetComponentInParent<StructureComponent>();
                        if (!(houseList.Contains(fbuildingblock)))
                        {
                            houseList.Add(fbuildingblock);
                            checkFrom.Add(fbuildingblock.transform.position);
                            if (GetStructureClean(initialBlock, playerRot, fbuildingblock, out housedata))
                            {
                                rawStructure.Add(housedata);
                            }
                        }
                    }
                    else if (hit.GetComponentInParent<DeployableObject>() != null)
                    {
                        fdeployable = hit.GetComponentInParent<DeployableObject>();
                        if (!(houseList.Contains(fdeployable)))
                        {
                            houseList.Add(fdeployable);
                            checkFrom.Add(fdeployable.transform.position);
                            if (GetDeployableClean(initialBlock, playerRot, fdeployable, out housedata))
                            {
                                if (fdeployable.GetComponent<LootableObject>())
                                {
                                    var box = fdeployable.GetComponent<LootableObject>();
                                    var itemlist = new List<object>();
                                    for (int i = 0; i < box._inventory.slotCount; i++)
                                    {
                                        if (box._inventory.GetItem(i, out item))
                                        {
                                            var newitem = new Dictionary<string, object>();
                                            newitem.Add("name", item.datablock.name);
                                            newitem.Add("amount", item.uses.ToString());
                                            itemlist.Add(newitem);
                                        }
                                    }
                                    housedata.Add("items", itemlist);
                                }
                                rawDeployables.Add(housedata);
                            }
                        }
                    }
                }
            }
            return true;
        }

        [ChatCommand("copy")]
        void cmdChatCopy(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) return;

            if (args == null || args.Length == 0)
            {
                SendReply(player, "You need to set the name of the copy file: /copy NAME");
                return;
            }

            // Get player camera view directly from the player
            if (!TryGetPlayerView(player, out currentRot))
            {
                SendReply(player, "Couldn't find your eyes");
                return;
            }

            // Get what the player is looking at
            if (!TryGetClosestRayPoint(player.playerClient.rootControllable.GetComponent<Character>().eyesOrigin, currentRot, out closestEnt, out closestHitpoint))
            {
                SendReply(player, "Couldn't find any Entity");
                return;
            }

            // Check if what the player is looking at is a collider
            var baseentity = closestEnt as Collider;

            // Check if what the player is looking at is a BuildingBlock (like a wall or something like that)
            var buildingblock = baseentity.GetComponentInParent<StructureComponent>();
            if (buildingblock == null)
            {
                SendReply(player, "You are not looking at a Structure, or something is blocking the view.");
                return;
            }

            var returncopy = CopyBuilding(player.playerClient.lastKnownPosition, currentRot.ToEulerAngles().y, buildingblock, out rawStructure, out rawDeployables);
            if (returncopy is string)
            {
                SendReply(player, (string)returncopy);
                return;
            }

            if (rawStructure.Count == 0)
            {
                SendReply(player, "Something went wrong, house is empty?");
                return;
            }

            Dictionary<string, object> defaultValues = new Dictionary<string, object>();

            Dictionary<string, object> defaultPos = new Dictionary<string, object>();
            defaultPos.Add("x", buildingblock.transform.position.x);
            defaultPos.Add("y", buildingblock.transform.position.y);
            defaultPos.Add("z", buildingblock.transform.position.z);
            defaultValues.Add("position", defaultPos);
            defaultValues.Add("yrotation", buildingblock.transform.rotation.ToEulerAngles().y);

            filename = string.Format("copypaste-{0}", args[0].ToString());
            Core.Configuration.DynamicConfigFile CopyData = Interface.GetMod().DataFileSystem.GetDatafile(filename);
            CopyData.Clear();
            CopyData["structure"] = rawStructure;
            CopyData["deployables"] = rawDeployables;
            CopyData["default"] = defaultValues;


            Interface.GetMod().DataFileSystem.SaveDatafile(filename);

            SendReply(player, string.Format("The house {0} was successfully saved", args[0].ToString()));
            SendReply(player, string.Format("{0} building parts detected", rawStructure.Count.ToString()));
            SendReply(player, string.Format("{0} deployables detected", rawDeployables.Count.ToString()));
        }
        
        [ChatCommand("paste")]
        void cmdChatPaste(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (args == null || args.Length == 0)
            {
                SendReply(player, "You need to set the name of the copy file: /paste NAME optional:HeightAdjustment");
                return;
            }

            // Adjust height so you don't automatically paste in the ground
            heightAdjustment = 0.5f;
            if (args.Length > 1)
            { 
                float.TryParse(args[1].ToString(), out heightAdjustment);
            }

            // Get player camera view directly from the player
            if (!TryGetPlayerView(player, out currentRot))
            {
                SendReply(player, "Couldn't find your eyes");
                return;
            }

            // Get what the player is looking at
            if (!TryGetClosestPoint(player.playerClient.rootControllable.GetComponent<Character>().eyesOrigin, currentRot, out closestEnt, out closestHitpoint))
            {
                SendReply(player, "Couldn't find any Entity");
                return;
            }

            closestHitpoint.y = closestHitpoint.y -4f + heightAdjustment;

            filename = string.Format("copypaste-{0}", args[0].ToString());
            Core.Configuration.DynamicConfigFile PasteData = Interface.GetMod().DataFileSystem.GetDatafile(filename);
            if (PasteData["structure"] == null || PasteData["default"] == null)
            {
                SendReply(player, "This is not a correct copypaste file, or it's empty.");
                return;
            }
            List<object> structureData = PasteData["structure"] as List<object>;
            List<object> deployablesData = PasteData["deployables"] as List<object>;

            PasteBuilding(structureData, closestHitpoint, currentRot.ToEulerAngles().y, heightAdjustment, player);
            PasteDeployables(deployablesData, closestHitpoint, currentRot.ToEulerAngles().y, heightAdjustment, player);
        }
        
        [ChatCommand("placeback")]
        void cmdChatPlaceback(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player))
            {
                SendReply(player, "You are not allowed to use this command");
                return;
            }
            if (args == null || args.Length == 0)
            {
                SendReply(player, "You need to set the name of the copy file: /placeback NAME");
                return;
            }
            heightAdjustment = 0;
            filename = string.Format("copypaste-{0}", args[0].ToString());

            Core.Configuration.DynamicConfigFile PasteData = Interface.GetMod().DataFileSystem.GetDatafile(filename);
            if (PasteData["structure"] == null || PasteData["default"] == null)
            {
                SendReply(player, "This is not a correct copypaste file, or it's empty.");
                return;
            }
            Dictionary<string, object> defaultData = PasteData["default"] as Dictionary<string, object>;
            Dictionary<string, object> defaultPos = defaultData["position"] as Dictionary<string, object>;
            Vector3 defaultposition = new Vector3(Convert.ToSingle(defaultPos["x"]), Convert.ToSingle(defaultPos["y"]), Convert.ToSingle(defaultPos["z"]));
            List<object> structureData = PasteData["structure"] as List<object>;
            List<object> deployablesData = PasteData["deployables"] as List<object>;

            PasteBuilding(structureData, defaultposition, Convert.ToSingle(defaultData["yrotation"]), heightAdjustment, player);
            PasteDeployables(deployablesData, defaultposition, Convert.ToSingle(defaultData["yrotation"]), heightAdjustment, player);
        }
        
        StructureComponent SpawnStructure(string prefab, Vector3 pos, Quaternion angle)
        {
            StructureComponent build = NetCull.InstantiateStatic(prefab,pos, angle).GetComponent<StructureComponent>();
            if(build == null) return null;
            return build;
        }

        DeployableObject SpawnDeployable(string prefab, Vector3 pos, Quaternion angles)
        {
            DeployableObject build = NetCull.InstantiateStatic(prefab, pos, angles).GetComponent<DeployableObject>();
            if (build == null) return null;
            return build;
        }

        void PasteBuilding(List<object> structureData, Vector3 targetPoint, float targetRot, float heightAdjustment, NetUser netuser)
        {
            Vector3 OriginRotation = new Vector3(0f, targetRot, 0f);
            Quaternion OriginRot = Quaternion.EulerRotation(OriginRotation);
            StructureMaster themaster = null;
            foreach (Dictionary<string, object> structure in structureData)
            {
                
                Dictionary<string, object> structPos = structure["pos"] as Dictionary<string, object>;
                Dictionary<string, object> structRot = structure["rot"] as Dictionary<string, object>;
                string prefabname = (string)structure["prefabname"];
                Quaternion newAngles = Quaternion.EulerRotation((new Vector3(Convert.ToSingle(structRot["x"]), Convert.ToSingle(structRot["y"]), Convert.ToSingle(structRot["z"]))) + OriginRotation);
                Vector3 TempPos = OriginRot * (new Vector3(Convert.ToSingle(structPos["x"]), Convert.ToSingle(structPos["y"]), Convert.ToSingle(structPos["z"])));
                Vector3 NewPos = TempPos + targetPoint;
                if (themaster == null)
                {
                    themaster = NetCull.InstantiateClassic<StructureMaster>(Facepunch.Bundling.Load<StructureMaster>("content/structures/StructureMasterPrefab"), NewPos, newAngles,0);
                    themaster.SetupCreator(netuser.playerClient.controllable);
                }
                StructureComponent block = SpawnStructure(prefabname, NewPos, newAngles);
                if(block != null)
                {
                    themaster.AddStructureComponent(block);
                }
            }
        }

        void PasteDeployables(List<object> deployablesData, Vector3 targetPoint, float targetRot, float heightAdjustment, NetUser player)
        {
            Vector3 OriginRotation = new Vector3(0f, targetRot, 0f);
            Quaternion OriginRot = Quaternion.EulerRotation(OriginRotation);
            foreach (Dictionary<string, object> deployable in deployablesData)
            {

                Dictionary<string, object> structPos = deployable["pos"] as Dictionary<string, object>;
                Dictionary<string, object> structRot = deployable["rot"] as Dictionary<string, object>;
                string prefabname = (string)deployable["prefabname"];

                Quaternion newAngles = Quaternion.EulerRotation((new Vector3(Convert.ToSingle(structRot["x"]), Convert.ToSingle(structRot["y"]), Convert.ToSingle(structRot["z"]))) + OriginRotation);
                Vector3 TempPos = OriginRot * (new Vector3(Convert.ToSingle(structPos["x"]), Convert.ToSingle(structPos["y"]), Convert.ToSingle(structPos["z"])));
                Vector3 NewPos = TempPos + targetPoint;
                DeployableObject block = SpawnDeployable(prefabname, NewPos, newAngles);
                if (block != null)
                {
                    block.SetupCreator(player.playerClient.controllable);
                    block.GrabCarrier();
                    LootableObject lootobject = block.GetComponent<LootableObject>();
                    if (lootobject == null) continue;
                    List<object> itemlist = deployable["items"] as List<object>;
                    if (itemlist == null || itemlist.Count == 0) continue;
                    foreach (Dictionary<string, object> item in itemlist)
                    {
                        lootobject._inventory.AddItemAmount(displaynameToDataBlock[item["name"].ToString()], (displaynameToDataBlock[item["name"].ToString()])._splittable? Convert.ToInt32(item["amount"]) : 1);
                    }
                }
            }
        }
    }
}