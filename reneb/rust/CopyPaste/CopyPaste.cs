using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using Oxide.Core;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("Copy Paste", "Reneb & VVoid & Alex", "2.2.15")]
    class CopyPaste : RustPlugin
    {
        private MethodInfo inventoryClear = typeof(ItemContainer).GetMethod("Clear", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo serverinput = typeof(BasePlayer).GetField("serverInput", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo keycode = typeof(KeyLock).GetField("keyCode", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo codelock = typeof(CodeLock).GetField("code", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo firstKeyCreated = typeof(KeyLock).GetField("firstKeyCreated", BindingFlags.NonPublic | BindingFlags.Instance);
        private Dictionary<string, string> deployedToItem = new Dictionary<string, string>();
        private int layerMasks = LayerMask.GetMask("Construction", "Construction Trigger", "Trigger", "Deployed", "Tree", "AI");

        /// CACHED VARIABLES

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
         
        void OnServerInitialized()
        {
            var allItemsDef = Resources.FindObjectsOfTypeAll<ItemDefinition>();
            foreach (ItemDefinition itemDef in allItemsDef)
            {
                if (itemDef.GetComponent<ItemModDeployable>() != null)
                {
                    deployedToItem.Add(itemDef.GetComponent<ItemModDeployable>().entityPrefab.Get().gameObject.name.ToString(), itemDef.shortname.ToString());
                }
            }
        }

        bool TryGetClosestRayPoint(Vector3 sourcePos, Quaternion sourceDir, out object closestEnt, out Vector3 closestHitpoint)
        {
            Vector3 sourceEye = sourcePos + new Vector3(0f, 1.5f, 0f);
            Ray ray = new Ray(sourceEye, sourceDir * Vector3.forward);

            var hits = Physics.RaycastAll(ray);
            float closestdist = 999999f;
            closestHitpoint = sourcePos;
            closestEnt = false;
            foreach (var hit in hits)
            {
                if (hit.collider.isTrigger)
                    continue;
                if (hit.distance < closestdist)
                {
                    closestdist = hit.distance;
                    closestEnt = hit.GetEntity();
                    closestHitpoint = hit.point;
                }
            }
            if (closestEnt is bool)
                return false;
            return true;
        }

        bool hasAccess(BasePlayer player)
        {
            if (player.net.connection.authLevel < 1)
            {
                SendReply(player, "You are not allowed to use this command");
                return false;
            }
            return true;
        }

        bool TryGetPlayerView(BasePlayer player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);
            var input = serverinput.GetValue(player) as InputState;
            if (input == null || input.current == null || input.current.aimAngles == Vector3.zero)
                return false;

            viewAngle = Quaternion.Euler(input.current.aimAngles);
            return true;
        }

        Vector3 GenerateGoodPos(Vector3 InitialPos, Vector3 CurrentPos, float diffRot)
        {
            transformedPos = CurrentPos - InitialPos;
            newX = (transformedPos.x * (float)System.Math.Cos(-diffRot)) + (transformedPos.z * (float)System.Math.Sin(-diffRot));
            newZ = (transformedPos.z * (float)System.Math.Cos(-diffRot)) - (transformedPos.x * (float)System.Math.Sin(-diffRot));
            transformedPos.x = newX;
            transformedPos.z = newZ;
            return transformedPos;
        }

        bool GetStructureClean(BuildingBlock initialBlock, float playerRot, BuildingBlock currentBlock, out Dictionary<string, object> data)
        {
            data = new Dictionary<string, object>();
            posCleanData = new Dictionary<string, object>();
            rotCleanData = new Dictionary<string, object>();

            normedPos = GenerateGoodPos(initialBlock.transform.position, currentBlock.transform.position, playerRot);
            normedYRot = currentBlock.transform.rotation.ToEulerAngles().y - playerRot;

            data.Add("prefabname", currentBlock.blockDefinition.fullName);
            data.Add("grade", currentBlock.grade);

            posCleanData.Add("x", normedPos.x.ToString());
            posCleanData.Add("y", normedPos.y.ToString());
            posCleanData.Add("z", normedPos.z.ToString());
            data.Add("pos", posCleanData);

            rotCleanData.Add("x", currentBlock.transform.rotation.ToEulerAngles().x.ToString());
            rotCleanData.Add("y", normedYRot.ToString());
            rotCleanData.Add("z", currentBlock.transform.rotation.ToEulerAngles().z.ToString());
            data.Add("rot", rotCleanData);
            return true;
        }

        bool GetDeployableClean(BuildingBlock initialBlock, float playerRot, Deployable currentBlock, out Dictionary<string, object> data)
        {
            data = new Dictionary<string, object>();
            posCleanData = new Dictionary<string, object>();
            rotCleanData = new Dictionary<string, object>();

            normedPos = GenerateGoodPos(initialBlock.transform.position, currentBlock.transform.position, playerRot);
            normedYRot = currentBlock.transform.rotation.ToEulerAngles().y - playerRot;
            data.Add("prefabname", StringPool.Get(currentBlock.prefabID).ToString());

            posCleanData.Add("x", normedPos.x.ToString());
            posCleanData.Add("y", normedPos.y.ToString());
            posCleanData.Add("z", normedPos.z.ToString());
            data.Add("pos", posCleanData);

            rotCleanData.Add("x", currentBlock.transform.rotation.ToEulerAngles().x.ToString());
            rotCleanData.Add("y", normedYRot.ToString());
            rotCleanData.Add("z", currentBlock.transform.rotation.ToEulerAngles().z.ToString());
            data.Add("rot", rotCleanData);
            return true;
        }

        bool GetSpawnableClean(BuildingBlock initialBlock, float playerRot, Spawnable currentSpawn, out Dictionary<string, object> data)
        {
            data = new Dictionary<string, object>();
            posCleanData = new Dictionary<string, object>();
            rotCleanData = new Dictionary<string, object>();

            normedPos = GenerateGoodPos(initialBlock.transform.position, currentSpawn.transform.position, playerRot);
            normedYRot = currentSpawn.transform.rotation.ToEulerAngles().y - playerRot;
            data.Add("prefabname", currentSpawn.GetComponent<BaseNetworkable>().LookupPrefabName().ToString());

            posCleanData.Add("x", normedPos.x.ToString());
            posCleanData.Add("y", normedPos.y.ToString());
            posCleanData.Add("z", normedPos.z.ToString());
            data.Add("pos", posCleanData);

            rotCleanData.Add("x", currentSpawn.transform.rotation.ToEulerAngles().x.ToString());
            rotCleanData.Add("y", normedYRot.ToString());
            rotCleanData.Add("z", currentSpawn.transform.rotation.ToEulerAngles().z.ToString());
            data.Add("rot", rotCleanData);
            return true;
        }

        object CopyBuilding(Vector3 playerPos, float playerRot, BuildingBlock initialBlock, out List<object> rawStructure, out List<object> rawDeployables, out List<object> rawSpawnables)
        {
            rawStructure = new List<object>();
            rawDeployables = new List<object>();
            rawSpawnables = new List<object>();
            List<object> houseList = new List<object>();
            List<Vector3> checkFrom = new List<Vector3>();
            BuildingBlock fbuildingblock;
            Deployable fdeployable;
            Spawnable fspawnable;

            houseList.Add(initialBlock);
            checkFrom.Add(initialBlock.transform.position);

            Dictionary<string, object> housedata;
            if (!GetStructureClean(initialBlock, playerRot, initialBlock, out housedata))
            {
                return "Couldn\'t get a clean initial block";
            }
            if (initialBlock.HasSlot(BaseEntity.Slot.Lock)) // initial block could be a door.
                TryCopyLock(initialBlock, housedata);
            rawStructure.Add(housedata);

            int current = 0;
            while (true)
            {
                current++;
                if (current > checkFrom.Count)
                    break;
                List<BaseEntity> list = Pool.GetList<BaseEntity>();
                Vis.Entities<BaseEntity>(checkFrom[current - 1], 3f, list, layerMasks);
                for (int i = 0; i < list.Count; i++)
                {
                    BaseEntity hit = list[i];
                    if (hit.GetComponentInParent<BuildingBlock>() != null)
                    {
                        fbuildingblock = hit.GetComponentInParent<BuildingBlock>();
                        if (!(houseList.Contains(fbuildingblock)))
                        {
                            houseList.Add(fbuildingblock);
                            checkFrom.Add(fbuildingblock.transform.position);
                            if (GetStructureClean(initialBlock, playerRot, fbuildingblock, out housedata))
                            {

                                if (fbuildingblock.HasSlot(BaseEntity.Slot.Lock))
                                    TryCopyLock(fbuildingblock, housedata);
                                rawStructure.Add(housedata);
                            }
                        }
                    }
                    else if (hit.GetComponentInParent<Deployable>() != null)
                    {
                        fdeployable = hit.GetComponentInParent<Deployable>();
                        if (!(houseList.Contains(fdeployable)))
                        {
                            houseList.Add(fdeployable);
                            checkFrom.Add(fdeployable.transform.position);
                            if (GetDeployableClean(initialBlock, playerRot, fdeployable, out housedata))
                            {
                                if (fdeployable.GetComponent<StorageContainer>())
                                {
                                    var box = fdeployable.GetComponent<StorageContainer>();
                                    var itemlist = new List<object>();
                                    foreach (Item item in box.inventory.itemList)
                                    {
                                        var newitem = new Dictionary<string, object>();
                                        newitem.Add("blueprint", item.IsBlueprint().ToString());
                                        newitem.Add("id", item.info.itemid.ToString());
                                        newitem.Add("amount", item.amount.ToString());
                                        itemlist.Add(newitem);
                                    }
                                    housedata.Add("items", itemlist);

                                    if (box.HasSlot(BaseEntity.Slot.Lock))
                                        TryCopyLock(box, housedata);
                                }
                                else if (fdeployable.GetComponent<Signage>())
                                {
                                    var signage = fdeployable.GetComponent<Signage>();
                                    var sign = new Dictionary<string, object>();
									var get = FileStorage.server.Get(signage.textureID, FileStorage.Type.png, signage.net.ID);
                                    if (signage.textureID > 0 && get!=null)
                                        sign.Add("texture", Convert.ToBase64String(get));
                                    sign.Add("locked", signage.IsLocked());
                                    housedata.Add("sign", sign);
                                }
                                rawDeployables.Add(housedata);
                            }
                        }
                    }
                    else if (hit.GetComponentInParent<Spawnable>() != null)
                    {
                        fspawnable = hit.GetComponentInParent<Spawnable>();
                        if (!(houseList.Contains(fspawnable)))
                        {
                            houseList.Add(fspawnable);
                            checkFrom.Add(fspawnable.transform.position);
                            if (GetSpawnableClean(initialBlock, playerRot, fspawnable, out housedata))
                            {
                                rawSpawnables.Add(housedata);
                            }
                        }
                    }
                }
            }
            return true;
        }

        [ChatCommand("copy")]
        void cmdChatCopy(BasePlayer player, string command, string[] args)
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
                SendReply(player, "Couldn\'t find your eyes");
                return;
            }

            // Get what the player is looking at
            if (!TryGetClosestRayPoint(player.transform.position, currentRot, out closestEnt, out closestHitpoint))
            {
                SendReply(player, "Couldn\'t find any Entity");
                return;
            }

            // Check if what the player is looking at is a collider
            var baseentity = closestEnt as BaseEntity;
            if (baseentity == null)
            {
                SendReply(player, "You are not looking at a Structure, or something is blocking the view.");
                return;
            }

            // Check if what the player is looking at is a BuildingBlock (like a wall or something like that)
            var buildingblock = baseentity.GetComponentInParent<BuildingBlock>();
            if (buildingblock == null)
            {
                SendReply(player, "You are not looking at a Structure, or something is blocking the view.");
                return;
            }

            var returncopy = CopyBuilding(player.transform.position, currentRot.ToEulerAngles().y, buildingblock, out rawStructure, out rawDeployables, out rawSpawnables);
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
            defaultPos.Add("x", buildingblock.transform.position.x.ToString());
            defaultPos.Add("y", buildingblock.transform.position.y.ToString());
            defaultPos.Add("z", buildingblock.transform.position.z.ToString());
            defaultValues.Add("position", defaultPos);
            defaultValues.Add("yrotation", buildingblock.transform.rotation.ToEulerAngles().y.ToString());

            filename = string.Format("copypaste-{0}", args[0].ToString());
            Core.Configuration.DynamicConfigFile CopyData = Interface.GetMod().DataFileSystem.GetDatafile(filename);
            CopyData.Clear();
            CopyData["structure"] = rawStructure;
            CopyData["deployables"] = rawDeployables;
            CopyData["spawnables"] = rawSpawnables;
            CopyData["default"] = defaultValues;


            Interface.GetMod().DataFileSystem.SaveDatafile(filename);

            SendReply(player, string.Format("The house {0} was successfully saved", args[0].ToString()));
            SendReply(player, string.Format("{0} building parts detected", rawStructure.Count.ToString()));
            SendReply(player, string.Format("{0} deployables detected", rawDeployables.Count.ToString()));
            SendReply(player, string.Format("{0} spawnables detected", rawSpawnables.Count.ToString()));
        }

        [ChatCommand("paste")]
        void cmdChatPaste(BasePlayer player, string command, string[] args)
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
                SendReply(player, "Couldn\'t find your eyes");
                return;
            }

            // Get what the player is looking at
            if (!TryGetClosestRayPoint(player.transform.position, currentRot, out closestEnt, out closestHitpoint))
            {
                SendReply(player, "Couldn\'t find any Entity");
                return;
            }

            closestHitpoint.y = closestHitpoint.y + heightAdjustment;

            filename = string.Format("copypaste-{0}", args[0].ToString());
            Core.Configuration.DynamicConfigFile PasteData = Interface.GetMod().DataFileSystem.GetDatafile(filename);
            if (PasteData["structure"] == null || PasteData["default"] == null)
            {
                SendReply(player, "This is not a correct copypaste file, or it\'s empty.");
                return;
            }
            List<object> structureData = PasteData["structure"] as List<object>;
            List<object> deployablesData = PasteData["deployables"] as List<object>;
            List<object> spawnablesData = PasteData["spawnables"] as List<object>;

            PasteBuilding(structureData, closestHitpoint, currentRot.ToEulerAngles().y, heightAdjustment);
            PasteDeployables(deployablesData, closestHitpoint, currentRot.ToEulerAngles().y, heightAdjustment, player);
            PasteSpawnables(spawnablesData, closestHitpoint, currentRot.ToEulerAngles().y, heightAdjustment, player);
        }

        [ChatCommand("placeback")]
        void cmdChatPlaceback(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 1)
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
                SendReply(player, "This is not a correct copypaste file, or it\'s empty.");
                return;
            }
            Dictionary<string, object> defaultData = PasteData["default"] as Dictionary<string, object>;
            Dictionary<string, object> defaultPos = defaultData["position"] as Dictionary<string, object>;
            Vector3 defaultposition = new Vector3(Convert.ToSingle(defaultPos["x"]), Convert.ToSingle(defaultPos["y"]), Convert.ToSingle(defaultPos["z"]));
            List<object> structureData = PasteData["structure"] as List<object>;
            List<object> deployablesData = PasteData["deployables"] as List<object>;

            PasteBuilding(structureData, defaultposition, Convert.ToSingle(defaultData["yrotation"]), heightAdjustment);
            PasteDeployables(deployablesData, defaultposition, Convert.ToSingle(defaultData["yrotation"]), heightAdjustment, player);
        }

        BuildingBlock SpawnStructure(GameObject prefab, Vector3 pos, Quaternion angles, BuildingGrade.Enum grade)
        {
            BuildingBlock block = prefab.GetComponent<BuildingBlock>();
            if (block == null) return null;
            block.transform.position = pos;
            block.transform.rotation = angles;
            block.gameObject.SetActive(true);
            block.blockDefinition = PrefabAttribute.server.Find<Construction>(block.prefabID);
            block.Spawn(true);
            block.SetGrade(grade);
            block.health = block.MaxHealth();
            return block;
        }

        void SpawnDeployable(Item newitem, Vector3 pos, Quaternion angles, BasePlayer player)
        {
            if (newitem.info.GetComponent<ItemModDeployable>() == null)
            {
                return;
            }
            var deployable = newitem.info.GetComponent<ItemModDeployable>().entityPrefab.resourcePath;
            if (deployable == null)
            {
                return;
            }
            var newBaseEntity = GameManager.server.CreateEntity(deployable, pos, angles);
            if (newBaseEntity == null)
            {
                return;
            }
            newBaseEntity.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
            newBaseEntity.SendMessage("InitializeItem", newitem, SendMessageOptions.DontRequireReceiver);
            newBaseEntity.Spawn(true);
        }

        void PasteBuilding(List<object> structureData, Vector3 targetPoint, float targetRot, float heightAdjustment)
        {
            Vector3 OriginRotation = new Vector3(0f, targetRot, 0f);
            Quaternion OriginRot = Quaternion.EulerRotation(OriginRotation);
            foreach (Dictionary<string, object> structure in structureData)
            {

                Dictionary<string, object> structPos = structure["pos"] as Dictionary<string, object>;
                Dictionary<string, object> structRot = structure["rot"] as Dictionary<string, object>;
                string prefabname = (string)structure["prefabname"];
				if (!prefabname.Contains(".prefab")) prefabname = "assets/bundled/prefabs/"+prefabname+".prefab";
                BuildingGrade.Enum grade = (BuildingGrade.Enum)structure["grade"];
                Quaternion newAngles = Quaternion.EulerRotation((new Vector3(Convert.ToSingle(structRot["x"]), Convert.ToSingle(structRot["y"]), Convert.ToSingle(structRot["z"]))) + OriginRotation);
                Vector3 TempPos = OriginRot * (new Vector3(Convert.ToSingle(structPos["x"]), Convert.ToSingle(structPos["y"]), Convert.ToSingle(structPos["z"])));
                Vector3 NewPos = TempPos + targetPoint;
                GameObject newPrefab = GameManager.server.CreatePrefab(prefabname, NewPos, newAngles, true);
                if (newPrefab != null)
                {
                    var block = SpawnStructure(newPrefab, NewPos, newAngles, grade);
                    if (block && block.HasSlot(BaseEntity.Slot.Lock))
                    {
                        TryPasteLock(block, structure);
                    }
                }
            }
        }

        void TryCopyLock(BaseCombatEntity lockableEntity, IDictionary<string, object> housedata)
        {
            var slotentity = lockableEntity.GetSlot(BaseEntity.Slot.Lock);
            if (slotentity != null)
            {
                if (slotentity.GetComponent<CodeLock>())
                {
                    housedata.Add("codelock", codelock.GetValue(slotentity.GetComponent<CodeLock>()).ToString());
                }
                else if (slotentity.GetComponent<KeyLock>())
                {
                    var code = (int)keycode.GetValue(slotentity.GetComponent<KeyLock>());
                    if ((bool)firstKeyCreated.GetValue(slotentity.GetComponent<KeyLock>()))
                        code |= 0x80;
                    housedata.Add("keycode", code.ToString());
                }
            }
        }

        void TryPasteLock(BaseCombatEntity lockableEntity, IDictionary<string, object> structure)
        {
            BaseEntity lockentity = null;
            if (structure.ContainsKey("codelock"))
            {
                lockentity = GameManager.server.CreateEntity("assets/bundled/prefabs/build/locks/lock.code.prefab", Vector3.zero, new Quaternion());
                lockentity.OnDeployed(lockableEntity);
                var code = (string)structure["codelock"];
                if (!string.IsNullOrEmpty(code))
                {
                    var @lock = lockentity.GetComponent<CodeLock>();
                    codelock.SetValue(@lock, (string)structure["codelock"]);
                    @lock.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else if (structure.ContainsKey("keycode"))
            {
                lockentity = GameManager.server.CreateEntity("assets/bundled/prefabs/build/locks/lock.key.prefab", Vector3.zero, new Quaternion());
                lockentity.OnDeployed(lockableEntity);
                var code = Convert.ToInt32(structure["keycode"]);
                var @lock = lockentity.GetComponent<KeyLock>();
                if ((code & 0x80) != 0)
                {
                    // Set the keycode only if that lock had keys before. Otherwise let it be random.
                    keycode.SetValue(@lock, (code & 0x7F));
                    firstKeyCreated.SetValue(@lock, true);
                    @lock.SetFlag(BaseEntity.Flags.Locked, true);
                }

            }

            if (lockentity)
            {
                lockentity.gameObject.Identity();
                lockentity.SetParent(lockableEntity, "lock");
                lockentity.Spawn(true);
                lockableEntity.SetSlot(BaseEntity.Slot.Lock, lockentity);
            }
        }

        void PasteDeployables(List<object> deployablesData, Vector3 targetPoint, float targetRot, float heightAdjustment, BasePlayer player)
        {
            Vector3 OriginRotation = new Vector3(0f, targetRot, 0f);
            Quaternion OriginRot = Quaternion.EulerRotation(OriginRotation);
            foreach (Dictionary<string, object> deployable in deployablesData)
            {

                Dictionary<string, object> structPos = deployable["pos"] as Dictionary<string, object>;
                Dictionary<string, object> structRot = deployable["rot"] as Dictionary<string, object>;
                string prefabname = (string)deployable["prefabname"];
                if (!prefabname.Contains(".prefab")) prefabname = "assets/bundled/prefabs/" + prefabname + ".prefab";
                Quaternion newAngles = Quaternion.EulerRotation((new Vector3(Convert.ToSingle(structRot["x"]), Convert.ToSingle(structRot["y"]), Convert.ToSingle(structRot["z"]))) + OriginRotation);
                Vector3 TempPos = OriginRot * (new Vector3(Convert.ToSingle(structPos["x"]), Convert.ToSingle(structPos["y"]), Convert.ToSingle(structPos["z"])));
                Vector3 NewPos = TempPos + targetPoint;
                BaseEntity entity = GameManager.server.CreateEntity(prefabname, NewPos, newAngles, true);
                if (entity == null) return;
                entity.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
                entity.Spawn(true);
                if (entity.GetComponent<StorageContainer>())
                {
                    var box = entity.GetComponent<StorageContainer>();
                    inventoryClear.Invoke(box.inventory, null);
                    var items = deployable["items"] as List<object>;
                    foreach (var itemDef in items)
                    {
                        var item = itemDef as Dictionary<string, object>;
                        var i = ItemManager.CreateByItemID(Convert.ToInt32(item["id"]), Convert.ToInt32(item["amount"]), Convert.ToBoolean(item["blueprint"]));
                        i?.MoveToContainer(box.inventory);
                    }

                    if (box.HasSlot(BaseEntity.Slot.Lock))
                        TryPasteLock(box, deployable);
                }
                else if (entity.GetComponent<Signage>())
                {
                    var sign = entity.GetComponent<Signage>();
                    var signData = deployable["sign"] as Dictionary<string, object>;
                    if (signData.ContainsKey("texture"))
                        sign.textureID = FileStorage.server.Store(Convert.FromBase64String(signData["texture"].ToString()), FileStorage.Type.png, sign.net.ID);
                    if (Convert.ToBoolean(signData["locked"]))
                        sign.SetFlag(BaseEntity.Flags.Locked, true);
                    sign.SendNetworkUpdate();
                }

            }
        }

        void PasteSpawnables(List<object> spawnablesData, Vector3 targetPoint, float targetRot, float heightAdjustment, BasePlayer player)
        {
            if (spawnablesData == null) return;

            Vector3 OriginRotation = new Vector3(0f, targetRot, 0f);
            Quaternion OriginRot = Quaternion.EulerRotation(OriginRotation);
            foreach (Dictionary<string, object> spawnable in spawnablesData)
            {
                Dictionary<string, object> structPos = spawnable["pos"] as Dictionary<string, object>;
                Dictionary<string, object> structRot = spawnable["rot"] as Dictionary<string, object>;
                string prefabname = (string)spawnable["prefabname"];
				if (!prefabname.Contains(".prefab")) prefabname = "assets/bundled/prefabs/"+prefabname+".prefab";
                Quaternion newAngles = Quaternion.EulerRotation((new Vector3(Convert.ToSingle(structRot["x"]), Convert.ToSingle(structRot["y"]), Convert.ToSingle(structRot["z"]))) + OriginRotation);
                Vector3 TempPos = OriginRot * (new Vector3(Convert.ToSingle(structPos["x"]), Convert.ToSingle(structPos["y"]), Convert.ToSingle(structPos["z"])));
                Vector3 NewPos = TempPos + targetPoint;
                BaseEntity entity = GameManager.server.CreateEntity(prefabname, NewPos, newAngles, true);
                if (entity == null) return;
                entity.Spawn(true);
            }

        }
    }
}