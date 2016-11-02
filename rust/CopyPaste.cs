using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Oxide.Core;
using Facepunch;
using System.IO;

namespace Oxide.Plugins
{
    [Info("Copy Paste", "Reneb", "3.0.17", ResourceId = 5981)]
    class CopyPaste : RustPlugin
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Static Fields
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
         
        string copyPermission = "copypaste.copy";
        string pastePermission = "copypaste.paste";
        string undoPermission = "copypaste.undo";
        string subDirectory = "copypaste/";

        int rayLayer = LayerMask.GetMask(new string[] { "Construction", "Deployed", "Tree", "Terrain", "Resource", "World", "Water", "Default", "Prevent Building" });
        int copyLayer = LayerMask.GetMask("Construction", "Construction Trigger", "Trigger", "Deployed", "Tree", "AI");
        int collisionLayer = LayerMask.GetMask("Construction", "Construction Trigger", "Trigger", "Deployed", "Default");
        int terrainLayer = LayerMask.GetMask(new string[] { "Terrain", "World", "Water", "Default" });
        int groundLayer = LayerMask.GetMask(new string[] { "Terrain", "Default" });

        private FieldInfo codelock = typeof(CodeLock).GetField("code", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo keycode = typeof(KeyLock).GetField("keyCode", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo firstKeyCreated = typeof(KeyLock).GetField("firstKeyCreated", BindingFlags.NonPublic | BindingFlags.Instance);
        private MethodInfo inventoryClear = typeof(ItemContainer).GetMethod("Clear", BindingFlags.NonPublic | BindingFlags.Instance);
        private MethodInfo newbuildingid = typeof(BuildingBlock).GetMethod("NewBuildingID", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        DataFileSystem datafile = Interface.GetMod().DataFileSystem;

        Dictionary<string, List<BaseEntity>> lastPastes = new Dictionary<string, List<BaseEntity>>();

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // General Methods
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        string GetMsg(string key, object steamid = null) { return lang.GetMessage(key, this, steamid == null ? null : steamid.ToString()); }
        bool hasAccess(BasePlayer player, string permissionName) { if (player.net.connection.authLevel > 1) return true; return permission.UserHasPermission(player.userID.ToString(), permissionName); }

        bool FindRayEntity(Vector3 sourcePos, Vector3 sourceDir, out Vector3 point, out BaseEntity entity)
        {
            RaycastHit hitinfo;
            entity = null;
            point = default(Vector3);

            if (!Physics.Raycast(sourcePos, sourceDir, out hitinfo, 1000f, rayLayer)) { return false; }

            point = hitinfo.point;
            entity = hitinfo.GetEntity();
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void Loaded()
        {
            permission.RegisterPermission(copyPermission, this);
            permission.RegisterPermission(pastePermission, this);
            permission.RegisterPermission(undoPermission, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "This file is empty.", "This file is empty."},
                {"You don't have the permissions to use this command.","You don't have the permissions to use this command." },
                {"Syntax: /paste TARGETFILENAME options values\r\nheight XX - Adjust the height\r\nblockcollision XX - blocks the entire paste if something the new building collides with something","Syntax: /paste TARGETFILENAME options values\r\nheight XX - Adjust the height\r\nblockcollision XX - blocks the entire paste if something the new building collides with something" },
                {"Syntax: /placeback TARGETFILENAME options values\r\nheight XX - Adjust the height\r\ncheckplaced true/false - checks if parts of the house are already placed or not, if they are already placed, the building part will be removed","Syntax: /placeback TARGETFILENAME options values\r\nheight XX - Adjust the height\r\ncheckplaced true/false - checks if parts of the house are already placed or not, if they are already placed, the building part will be removed" },
                {"Syntax: /paste or /placeback TARGETFILENAME options values\r\nheight XX - Adjust the height\r\nautoheight true/false - sets best height, carefull of the steep\r\ncheckplaced true/false - checks if parts of the house are already placed or not, if they are already placed, the building part will be removed \r\nblockcollision XX - blocks the entire paste if something the new building collides with something\r\ndeployables true/false - false to remove deployables\r\ninventories true/false - false to ignore inventories","Syntax: /paste or /placeback TARGETFILENAME options values\r\nheight XX - Adjust the height\r\nautoheight true/false - sets best height, carefull of the steep\r\ncheckplaced true/false - checks if parts of the house are already placed or not, if they are already placed, the building part will be removed \r\nblockcollision XX - blocks the entire paste if something the new building collides with something\r\ndeployables true/false - false to remove deployables\r\ninventories true/false - false to ignore inventories" },
                { "You've successfully placed back the structure.","You've successfully placed back the structure." },
                {"You've successfully pasted the structure.","You've successfully pasted the structure." },
                {"Syntax: /copy TARGETFILENAME options values\r\n radius XX (default 3)\r\n mechanics proximity/building (default building)\r\nbuilding true/false (saves structures or not)\r\ndeployables true/false (saves deployables or not)\r\ninventories true/false (saves inventories or not)","Syntax: /copy TARGETFILENAME options values\r\n radius XX (default 3)\r\n mechanics proximity/building (default building)\r\nbuilding true/false (saves structures or not)\r\ndeployables true/false (saves deployables or not)\r\ninventories true/false (saves inventories or not)" },
                {"Couldn't ray something valid in front of you.","Couldn't ray something valid in front of you." },
                {"The structure was successfully copied as {0}","The structure was successfully copied as {0}" },
                {"You must paste something before undoing it.","You must paste something before undoing it." },
                {"You've successfully undid what you pasted.","You've successfully undid what you pasted." }
            }, this);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Copy
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        bool isValid(BaseEntity entity) { return entity.GetComponentInParent<BuildingBlock>() || entity.GetComponentInParent<BaseCombatEntity>() || entity.GetComponentInParent<Spawnable>(); }

        void TryCopyLock(BaseEntity lockableEntity, IDictionary<string, object> housedata)
        {
            var slotentity = lockableEntity.GetSlot(BaseEntity.Slot.Lock);
            if (slotentity != null)
            {
                var codedata = new Dictionary<string, object>
                {
                    {"prefabname", slotentity.PrefabName}
                };

                if (slotentity.GetComponent<CodeLock>())
                {
                    codedata.Add("code", codelock.GetValue(slotentity.GetComponent<CodeLock>()).ToString());
                }
                else if (slotentity.GetComponent<KeyLock>())
                {
                    var code = (int)keycode.GetValue(slotentity.GetComponent<KeyLock>());
                    if ((bool)firstKeyCreated.GetValue(slotentity.GetComponent<KeyLock>()))
                        code |= 0x80;
                    codedata.Add("code", code.ToString());
                }
                housedata.Add("lock",codedata);
            }
        }
        object CopyByBuilding(Vector3 sourcePos, Vector3 sourceRot, float RotationCorrection, float range, bool saveBuildings, bool saveDeployables, bool saveInventories)
        {
            var rawData = new List<object>();
            var houseList = new List<BaseEntity>();
            var checkFrom = new List<Vector3> { sourcePos };
            uint buildingid = 0;
            int current = 0;
            try
            {
                while (true)
                {
                    if (current >= checkFrom.Count) break;

                    List<BaseEntity> list = Pool.GetList<BaseEntity>();
                    Vis.Entities<BaseEntity>(checkFrom[current], range, list, copyLayer);

                    for (int i = 0; i < list.Count; i++)
                    {
                        var entity = list[i];
                        if (isValid(entity) && !houseList.Contains(entity))
                        {
                            houseList.Add(entity);
                            var buildingblock = entity.GetComponentInParent<BuildingBlock>();
                            if (buildingblock)
                            {
                                if (buildingid == 0) buildingid = buildingblock.buildingID;
                                else if(buildingid != buildingblock.buildingID) continue;
                            }
                            if (!checkFrom.Contains(entity.transform.position)) checkFrom.Add(entity.transform.position);

                            if (!saveBuildings && entity.GetComponentInParent<BuildingBlock>() != null) continue;
                            if (!saveDeployables && (entity.GetComponentInParent<BuildingBlock>() == null && entity.GetComponent<BaseCombatEntity>() != null)) continue;
                            rawData.Add(EntityData(entity, sourcePos, sourceRot, entity.transform.position, entity.transform.rotation.ToEulerAngles(), RotationCorrection, saveInventories));
                        }
                    }
                    current++;
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return rawData;
        }
        object CopyByProximity(Vector3 sourcePos, Vector3 sourceRot, float RotationCorrection, float range, bool saveBuildings, bool saveDeployables, bool saveInventories)
        {
            var rawData = new List<object>();
            var houseList = new List<BaseEntity>();
            var checkFrom = new List<Vector3> { sourcePos };
            int current = 0;
            try
            {
                while (true)
                {
                    if (current >= checkFrom.Count) break;

                    List<BaseEntity> list = Pool.GetList<BaseEntity>();
                    Vis.Entities<BaseEntity>(checkFrom[current], range, list, copyLayer);

                    for (int i = 0; i < list.Count; i++)
                    {
                        var entity = list[i];
                        if (isValid(entity) && !houseList.Contains(entity))
                        {
                            houseList.Add(entity);
                            if (!checkFrom.Contains(entity.transform.position)) checkFrom.Add(entity.transform.position);

                            if (!saveBuildings && entity.GetComponentInParent<BuildingBlock>() != null) continue;
                            if (!saveDeployables && (entity.GetComponentInParent<BuildingBlock>() == null && entity.GetComponent<BaseCombatEntity>() != null)) continue;

                            rawData.Add(EntityData(entity, sourcePos, sourceRot, entity.transform.position, entity.transform.rotation.ToEulerAngles(), RotationCorrection, saveInventories));
                        }
                    }
                    current++;
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return rawData;
        }
        enum CopyMechanics
        {
            Building,
            Proximity
        }


        object TryCopyFromSteamID(string steamid, string filename, string[] args)
        {
            ulong userid;
            if (!ulong.TryParse(steamid, out userid)) { return "First argument isn't a steamid"; }
            var player = BasePlayer.FindByID(userid);
            if (player == null) return "Couldn't find the player";
            if (!player.IsConnected()) return "Player is not connected?";

            return TryCopyFromPlayer(player, filename, args);
        }

        object TryCopyFromPlayer(BasePlayer player, string filename, string[] args)
        {
            if (player == null) return "Player is null?";
            if (!player.IsConnected()) return "Player is not connected?";

            var ViewAngles = Quaternion.Euler(player.GetNetworkRotation());
            BaseEntity sourceEntity;
            Vector3 sourcePoint;

            if (!FindRayEntity(player.eyes.position, ViewAngles * Vector3.forward, out sourcePoint, out sourceEntity))
            {
                return GetMsg("Couldn't ray something valid in front of you.", player.userID.ToString());
            }

            return TryCopy(sourcePoint, sourceEntity.transform.rotation.ToEulerAngles(), filename, ViewAngles.ToEulerAngles().y, args);
        }

        object TryCopy(Vector3 sourcePos, Vector3 sourceRot, string filename, float RotationCorrection, string[] args)
        {
            CopyMechanics copyMechanics = CopyMechanics.Building;
            float radius = 3f;
            bool saveInventories = true;
            bool saveDeployables = true;
            bool saveBuilding = true;
            for(int i = 0; ; i = i + 2)
            {
                if (i >= args.Length) break;
                if(i+1 >= args.Length)
                {
                    return GetMsg("Syntax: /copy TARGETFILENAME options values\r\n radius XX (default 3)\r\n mechanics proximity/building (default building)\r\nbuilding true/false (saves structures or not)\r\ndeployables true/false (saves deployables or not)\r\ninventories true/false (saves inventories or not)", null);
                }
                switch(args[i].ToLower())
                {
                    case "r":
                    case "rad":
                    case "radius":
                        if(!float.TryParse(args[i+1], out radius))
                        {
                            return "radius must be a number";
                        }
                        break;
                    case "mechanics":
                    case "m":
                    case "mecha":
                        switch(args[i+1].ToLower())
                        {
                            case "building":
                            case "build":
                            case "b":
                                copyMechanics = CopyMechanics.Building;
                                break;
                            case "proximity":
                            case "prox":
                            case "p":
                                copyMechanics = CopyMechanics.Proximity;
                                break;
                        }
                        break;
                    case "i":
                    case "inventories":
                    case "inv":
                        if(!bool.TryParse(args[i + 1], out saveInventories))
                        {
                            return "save inventories needs to be true or false";
                        }
                        break;
                    case "b":
                    case "building":
                    case "structure":
                        if (!bool.TryParse(args[i + 1], out saveBuilding))
                        {
                            return "save buildings needs to be true or false";
                        }
                        break;
                    case "d":
                    case "deployables":
                        if (!bool.TryParse(args[i + 1], out saveDeployables))
                        {
                            return "save deployables needs to be true or false";
                        }
                        break;

                    default:
                        return GetMsg("Syntax: /copy TARGETFILENAME options values\r\n radius XX (default 3)\r\n mechanics proximity/building (default building)\r\nbuilding true/false (saves structures or not)\r\ndeployables true/false (saves deployables or not)\r\ninventories true/false (saves inventories or not)", null);
                        break;
                }

            }

            return Copy(sourcePos, sourceRot, filename, RotationCorrection, copyMechanics, radius, saveBuilding, saveDeployables, saveInventories);
        }
        object Copy(Vector3 sourcePos, Vector3 sourceRot, string filename, float RotationCorrection, CopyMechanics copyMechanics, float range, bool saveBuildings, bool saveDeployables, bool saveInventories )
        {
            var rawData = new List<object>();
            var copy = copyMechanics == CopyMechanics.Proximity ? CopyByProximity(sourcePos, sourceRot, RotationCorrection, range, saveBuildings, saveDeployables, saveInventories) : CopyByBuilding(sourcePos, sourceRot, RotationCorrection, range, saveBuildings, saveDeployables, saveInventories); ;
            if (copy is string) return copy;
            rawData = copy as List<object>;

            var defaultData = new Dictionary<string, object>
            {
                {"position", new Dictionary<string, object>
                    {
                        {"x", sourcePos.x.ToString()  },
                        {"y", sourcePos.y.ToString() },
                        {"z", sourcePos.z.ToString() }
                    }
                },
                {"rotationy", sourceRot.y.ToString() },
                {"rotationdiff", RotationCorrection.ToString() }
            };

            string path = subDirectory + filename;
            var CopyData = Interface.GetMod().DataFileSystem.GetDatafile(path);
            CopyData.Clear();
            CopyData["default"] = defaultData;
            CopyData["entities"] = rawData;
            Interface.GetMod().DataFileSystem.SaveDatafile(path);
            return true;
        }

        Dictionary<string,object> EntityData(BaseEntity entity, Vector3 sourcePos, Vector3 sourceRot, Vector3 entPos, Vector3 entRot, float diffRot, bool saveInventories)
        {
            var normalizedPos = NormalizePosition(sourcePos, entPos, diffRot);
            var normalizedRot = entRot.y - diffRot;

            var data = new Dictionary<string, object>
            {
                { "prefabname", entity.PrefabName },
                { "skinid", entity.skinID },
                { "pos", new Dictionary<string,object>
                    {
                        { "x", normalizedPos.x.ToString() },
                        { "y", normalizedPos.y.ToString() },
                        { "z", normalizedPos.z.ToString() }
                    }
                },
                { "rot", new Dictionary<string,object>
                    {
                        { "x", entRot.x.ToString() },
                        { "y", normalizedRot.ToString() },
                        { "z", entRot.z.ToString() },
                    }
                }
            };

            if (entity.HasSlot(BaseEntity.Slot.Lock))
                TryCopyLock(entity, data);

            var buildingblock = entity.GetComponentInParent<BuildingBlock>();
            if (buildingblock != null )
            {
                data.Add("grade", buildingblock.grade);
            }

            var box = entity.GetComponentInParent<StorageContainer>();
            if (box != null)
            {
                var itemlist = new List<object>();
                if (saveInventories)
                {
                    foreach (Item item in box.inventory.itemList)
                    {
                        var itemdata = new Dictionary<string, object>
                        {
                            {"condition", item.condition.ToString() },
                            {"id", item.info.itemid },
                            {"amount", item.amount },
                            {"skinid", item.skin },
                        };
                        var heldEnt = item.GetHeldEntity();
                        if(heldEnt != null)
                        {
                            var projectiles = heldEnt.GetComponent<BaseProjectile>();
                            if(projectiles != null)
                            {
                                var magazine = projectiles.primaryMagazine;
                                if(magazine != null)
                                {
                                    itemdata.Add("magazine", new Dictionary<string, object> { { magazine.ammoType.itemid.ToString(), magazine.contents } });
                                }
                            }
                        }
                        
                        if(item.contents != null)
                        {
                            if(item.contents.itemList != null)
                            {
                                var contents = new List<object>();
                                foreach (Item item2 in item.contents.itemList)
                                {
                                    contents.Add(new Dictionary<string, object>
                                    {
                                        {"condition", item.condition.ToString() },
                                        {"id", item.info.itemid },
                                        {"amount", item.amount },
                                        {"skinid", item.skin },
                                        {"items", new List<object>() }
                                    });
                                }
                                itemdata["items"] = contents;
                            }
                        }

                        itemlist.Add(itemdata);
                    }
                }
                data.Add("items", itemlist);
            }

            var sign = entity.GetComponentInParent<Signage>();
            if(sign != null)
            {
                var get = FileStorage.server.Get(sign.textureID, FileStorage.Type.png, sign.net.ID);
                data.Add("sign", new Dictionary<string, object>
                {
                    {"locked", sign.IsLocked() }
                });
                if (sign.textureID > 0 && get != null) ((Dictionary<string, object>)data["sign"]).Add("texture", Convert.ToBase64String(get));
            }

            return data;
        }
        Vector3 NormalizePosition(Vector3 InitialPos, Vector3 CurrentPos, float diffRot)
        {
            var transformedPos = CurrentPos - InitialPos;
            var newX = (transformedPos.x * (float)System.Math.Cos(-diffRot)) + (transformedPos.z * (float)System.Math.Sin(-diffRot));
            var newZ = (transformedPos.z * (float)System.Math.Cos(-diffRot)) - (transformedPos.x * (float)System.Math.Sin(-diffRot));
            transformedPos.x = newX;
            transformedPos.z = newZ;
            return transformedPos;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Placeback
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        object TryPlaceback(string filename, BasePlayer player, string[] args)
        {
            string path = subDirectory + filename;

            if (datafile.ExistsDatafile(path)) { }

            var data = Interface.GetMod().DataFileSystem.GetDatafile(path);
            if (data["default"] == null || data["entities"] == null)
            {
                return GetMsg("This file is empty.", player.userID.ToString());
            }

            var defaultdata = data["default"] as Dictionary<string, object>;
            var pos = defaultdata["position"] as Dictionary<string, object>;
            var startPos = new Vector3(Convert.ToSingle(pos["x"]), Convert.ToSingle(pos["y"]), Convert.ToSingle(pos["z"]));
            var RotationCorrection = Convert.ToSingle(defaultdata["rotationdiff"]);

            return TryPaste(startPos, filename, player, RotationCorrection, args);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Paste
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        object TryPasteFromVector3(Vector3 startPos, Vector3 direction, string filename, string[] args)
        {
            return TryPaste(startPos, filename, null, direction.y, args);
        }

        object TryPasteFromSteamID(string steamid, string filename, string[] args)
        {
            ulong userid;
            if (!ulong.TryParse(steamid, out userid)) { return "First argument isn't a steamid"; }
            var player = BasePlayer.FindByID(userid);
            if(player == null) return "Couldn't find the player";
            if (!player.IsConnected()) return "Player is not connected?";

            return TryPasteFromPlayer(player, filename, args);
        }

        object TryPasteFromPlayer(BasePlayer player, string filename, string[] args)
        {
            if (player == null) return "Player is null?";
            if (!player.IsConnected()) return "Player is not connected?";

            var ViewAngles = Quaternion.Euler(player.GetNetworkRotation());
            BaseEntity sourceEntity;
            Vector3 sourcePoint;

            if (!FindRayEntity(player.eyes.position, ViewAngles * Vector3.forward, out sourcePoint, out sourceEntity)) {
                return GetMsg("Couldn't ray something valid in front of you.", player.userID.ToString());
            }

            return TryPaste(sourcePoint, filename, player, ViewAngles.ToEulerAngles().y, args);
        }

        object TryPaste(Vector3 startPos, string filename, BasePlayer player, float RotationCorrection, string[] args)
        {
            var steamid = player == null ? null : player.userID.ToString();

            string path = subDirectory + filename;

            if (datafile.ExistsDatafile(path)) { }

            var data = Interface.GetMod().DataFileSystem.GetDatafile(path);
            if(data["default"] == null || data["entities"] == null)
            {
                return GetMsg("This file is empty.", steamid);
            }

            float heightAdj = 0f;
            float blockCollision = 0f;
            bool checkPlaced = false;
            bool autoHeight = false;
            bool inventories = true;
            bool deployables = true;

            for (int i = 0; ; i = i + 2)
            {
                if (i >= args.Length) break;
                if (i + 1 >= args.Length)
                {
                    return GetMsg("Syntax: /paste or /placeback TARGETFILENAME options values\r\nheight XX - Adjust the height\r\nautoheight true/false - sets best height, carefull of the steep\r\ncheckplaced true/false - checks if parts of the house are already placed or not, if they are already placed, the building part will be removed \r\nblockcollision XX - blocks the entire paste if something the new building collides with something\r\ndeployables true/false - false to remove deployables\r\ninventories true/false - false to ignore inventories", steamid);
                }
                switch (args[i].ToLower())
                {
                    case "autoheight":
                        if (!bool.TryParse(args[i + 1], out autoHeight))
                        {
                            return "autoheight must be true or false";
                        }
                        break;
                    case "height":
                        if (!float.TryParse(args[i + 1], out heightAdj))
                        {
                            return "height must be a number";
                        }
                        break;
                    case "checkplaced":
                        if (!bool.TryParse(args[i + 1], out checkPlaced))
                        {
                            return "checkplaced must be true or false";
                        }
                        
                        break;
                    case "blockcollision":
                        if (!float.TryParse(args[i + 1], out blockCollision))
                        {
                            return "blockcollision must be a number, 0 will deactivate the option";
                        }
                        break;
                    case "deployables":
                        if (!bool.TryParse(args[i + 1], out deployables))
                        {
                            return "deployables must be true or false";
                        }

                        break;
                    case "inventories":
                        if (!bool.TryParse(args[i + 1], out inventories))
                        {
                            return "inventories must be true or false";
                        }
                        break;
                    default:
                        return GetMsg("Syntax: /paste or /placeback TARGETFILENAME options values\r\nheight XX - Adjust the height\r\nautoheight true/false - sets best height, carefull of the steep\r\ncheckplaced true/false - checks if parts of the house are already placed or not, if they are already placed, the building part will be removed \r\nblockcollision XX - blocks the entire paste if something the new building collides with something\r\ndeployables true/false - false to remove deployables\r\ninventories true/false - false to ignore inventories", steamid);
                        break;
                }

            }

            startPos.y += heightAdj;

            var preloadData = PreLoadData(data["entities"] as List<object>, startPos, RotationCorrection, deployables, inventories);
            if(autoHeight)
            {
                var bestHeight = FindBestHeight(preloadData, startPos);
                if (bestHeight is string)
                {
                    return bestHeight;
                }
                heightAdj = (float)bestHeight - startPos.y;
                FixPreloadData(preloadData, heightAdj);
            }

            if (blockCollision > 0f)
            {
                var collision = CheckCollision(preloadData, startPos,blockCollision);
                if(collision is string)
                {
                    return collision;
                }
            }

            return Paste(preloadData, startPos, player, checkPlaced);
        }
        object GetGround(Vector3 pos)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(pos, Vector3.up, out hitInfo, groundLayer))
            {
                return hitInfo.point;
            }
            if (Physics.Raycast(pos, Vector3.down, out hitInfo, groundLayer))
            {
                return hitInfo.point;
            }
            return null;
        }
        object FindBestHeight(List<Dictionary<string,object>> entities, Vector3 startPos)
        {
            float minHeight = 0f;
            float maxHeight = 0f;
            foreach(var entity in entities)
            {
                if(((string)entity["prefabname"]).Contains("/foundation/"))
                {
                    var foundHeight = GetGround((Vector3)entity["position"]);
                    if(foundHeight != null)
                    {
                        var height = (Vector3)foundHeight;
                        if (height.y > maxHeight) maxHeight = height.y;
                        if (height.y < minHeight) minHeight = height.y;
                    }
                }
            }
            if (maxHeight - minHeight > 3f) return "The ground is too steep";
            return maxHeight;
        }
        void FixPreloadData(IList<Dictionary<string,object>> entities, float heightAdj)
        {
            foreach(var entity in entities)
            {
                var pos = ((Vector3)entity["position"]);
                pos.y += heightAdj;
                entity["position"] = pos;
            }
        }
        List<Dictionary<string, object>> PreLoadData(List<object> entities, Vector3 startPos, float RotationCorrection, bool deployables, bool inventories)
        {
            var eulerRotation = new Vector3(0f, RotationCorrection, 0f);
            var quaternionRotation = Quaternion.EulerRotation(eulerRotation);
            var preloaddata = new List<Dictionary<string, object>>();
            foreach(var entity in entities)
            {
                var data = entity as Dictionary<string, object>;
                if (!deployables && !data.ContainsKey("grade")) continue;
                var pos = (Dictionary<string, object>)data["pos"];
                var rot = (Dictionary<string, object>)data["rot"];
                var fixedRotation = Quaternion.EulerRotation(eulerRotation + new Vector3(Convert.ToSingle(rot["x"]), Convert.ToSingle(rot["y"]), Convert.ToSingle(rot["z"])));
                var tempPos = quaternionRotation * (new Vector3(Convert.ToSingle(pos["x"]), Convert.ToSingle(pos["y"]), Convert.ToSingle(pos["z"])));
                Vector3 newPos = tempPos + startPos;
                data.Add("position", newPos);
                data.Add("rotation", fixedRotation);
                if (!inventories && data.ContainsKey("items")) data["items"] = new List<object>();
                preloaddata.Add(data);
            }
            return preloaddata;
        }
        void TryPasteLock(BaseEntity lockableEntity, Dictionary<string, object> structure)
        {
            BaseEntity lockentity = null;
            if (structure.ContainsKey("lock"))
            {
                var lockdata = structure["lock"] as Dictionary<string, object>;
                lockentity = GameManager.server.CreateEntity((string)lockdata["prefabname"], Vector3.zero, new Quaternion(), true);
                if (lockentity != null)
                {
                    lockentity.gameObject.Identity();
                    lockentity.SetParent(lockableEntity, "lock");
                    lockentity.OnDeployed(lockableEntity);
                    lockentity.Spawn();
                    lockableEntity.SetSlot(BaseEntity.Slot.Lock, lockentity);
                    if (lockentity.GetComponent<CodeLock>())
                    {
                        var code = (string)lockdata["code"];
                        if (!string.IsNullOrEmpty(code))
                        {
                            var @lock = lockentity.GetComponent<CodeLock>();
                            codelock.SetValue(@lock, code);
                            @lock.SetFlag(BaseEntity.Flags.Locked, true);
                        }
                    }
                    else if (lockentity.GetComponent<KeyLock>())
                    {
                        var code = Convert.ToInt32(lockdata["code"]);
                        var @lock = lockentity.GetComponent<KeyLock>();
                        if ((code & 0x80) != 0)
                        {
                            keycode.SetValue(@lock, (code & 0x7F));
                            firstKeyCreated.SetValue(@lock, true);
                            @lock.SetFlag(BaseEntity.Flags.Locked, true);
                        }
                    }
                }
            }
        }
        object CheckCollision(List<Dictionary<string,object>> entities, Vector3 startPos, float radius)
        {
            foreach (var entityobj in entities)
            {
                var pos = (Vector3)entityobj["position"];
                var rot = (Quaternion)entityobj["rotation"];

                foreach(var collider in Physics.OverlapSphere(pos, radius, collisionLayer))
                {
                    return string.Format("Something is blocking the paste ({0})",collider.gameObject.name.ToString());
                }
                
            }
            return true;
        }

        List<BaseEntity> Paste(List<Dictionary<string,object>> entities, Vector3 startPos, BasePlayer player, bool checkPlaced)
        {
            bool unassignid = true;
            uint buildingid = 0;
            var pastedEntities = new List<BaseEntity>();
            foreach(var data in entities)
            {
                try
                {
                    var prefabname = (string)data["prefabname"];
                    var skinid = (int)data["skinid"];
                    var pos = (Vector3)data["position"];
                    var rot = (Quaternion)data["rotation"];

                    bool isplaced = false;
                    if (checkPlaced)
                    {
                        foreach (var col in Physics.OverlapSphere(pos, 1f))
                        {
                            var ent = col.GetComponentInParent<BaseEntity>();
                            if (ent != null)
                            {
                                if (ent.PrefabName == prefabname && ent.transform.position == pos && ent.transform.rotation == rot)
                                {
                                    isplaced = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (isplaced) continue;

                    var entity = GameManager.server.CreateEntity(prefabname, pos, rot, true);
                    if (entity != null)
                    {
                        entity.transform.position = pos;
                        entity.transform.rotation = rot;
                        entity.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);

                        var buildingblock = entity.GetComponentInParent<BuildingBlock>();
                        if (buildingblock != null)
                        {
                            buildingblock.blockDefinition = PrefabAttribute.server.Find<Construction>(buildingblock.prefabID);
                            buildingblock.SetGrade((BuildingGrade.Enum)data["grade"]);
                            if (unassignid)
                            {
                                buildingid = (uint)newbuildingid.Invoke(buildingblock, null);
                                unassignid = false;
                            }
                            buildingblock.buildingID = buildingid;
                        }
                        entity.skinID = skinid;
                        entity.Spawn();

                        bool killed = false;

                        if (killed) continue;

                        var basecombat = entity.GetComponentInParent<BaseCombatEntity>();
                        if (basecombat != null)
                        {
                            basecombat.ChangeHealth(basecombat.MaxHealth());
                        }

                        if (entity.HasSlot(BaseEntity.Slot.Lock))
                        {
                            TryPasteLock(entity, data);
                        }

                        var box = entity.GetComponentInParent<StorageContainer>();
                        if (box != null)
                        {
                            inventoryClear.Invoke(box.inventory, null);
                            var items = data["items"] as List<object>;
                            var itemlist = new List<ItemAmount>();
                            foreach (var itemDef in items)
                            {
                                var item = itemDef as Dictionary<string, object>;
                                var itemid = Convert.ToInt32(item["id"]);
                                var itemamount = Convert.ToInt32(item["amount"]);
                                var itemskin = Convert.ToInt32(item["skinid"]);
                                var itemcondition = Convert.ToSingle(item["condition"]);

                                var i = ItemManager.CreateByItemID(itemid, itemamount, itemskin);
                                if (i != null)
                                {
                                    i.condition = itemcondition;

                                    if (item.ContainsKey("magazine"))
                                    {
                                        var magazine = item["magazine"] as Dictionary<string, object>;
                                        var ammotype = int.Parse(magazine.Keys.ToArray()[0]);
                                        var ammoamount = int.Parse(magazine[ammotype.ToString()].ToString());
                                        var heldent = i.GetHeldEntity();
                                        if (heldent != null)
                                        {
                                            var projectiles = heldent.GetComponent<BaseProjectile>();
                                            if (projectiles != null)
                                            {
                                                projectiles.primaryMagazine.ammoType = ItemManager.FindItemDefinition(ammotype);
                                                projectiles.primaryMagazine.contents = ammoamount;
                                            }
                                        }
                                    }
                                    i?.MoveToContainer(box.inventory).ToString();
                                }
                            };
                        }

                        var sign = entity.GetComponentInParent<Signage>();
                        if (sign != null)
                        {
                            var signData = data["sign"] as Dictionary<string, object>;
                            if (signData.ContainsKey("texture"))
                            {
                                var stream = new MemoryStream();
                                var stringSign = Convert.FromBase64String(signData["texture"].ToString());
                                stream.Write(stringSign, 0, stringSign.Length);
                                sign.textureID = FileStorage.server.Store(stream, FileStorage.Type.png, sign.net.ID);
                                stream.Position = 0;
                                stream.SetLength(0);
                            }
                            if (Convert.ToBoolean(signData["locked"]))
                                sign.SetFlag(BaseEntity.Flags.Locked, true);
                            sign.SendNetworkUpdate();
                        }

                        pastedEntities.Add(entity);
                    }
                }
                catch(Exception e)
                {
                    PrintError(string.Format("Trying to paste {0} send this error: {1}", data["prefabname"].ToString(), e.Message));
                }
            }
            return pastedEntities;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Undo
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        object TryUndo(BasePlayer player)
        {
            return TryUndo(player.userID.ToString());
        }
        object TryUndo(string steamid)
        {
            if (!lastPastes.ContainsKey(steamid)) { return GetMsg("You must paste something before undoing it.", steamid); }

            var success = Undo(lastPastes[steamid]);
            lastPastes.Remove(steamid);

            return success;
        }

        object Undo(List<BaseEntity> entities)
        {
            foreach(var entity in entities)
            {
                if (entity == null) continue;
                if (entity.isDestroyed) continue;
                entity.KillMessage();
            }
            return true;
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Chat commands
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("paste")]
        void cmdChatPaste(BasePlayer player, string command, string[] args)
        {
            var steamid = player.userID.ToString();
            if (!hasAccess(player, pastePermission)) { SendReply(player, GetMsg("You don't have the permissions to use this command.", steamid)); return; }
            if (args == null || args.Length == 0) { SendReply(player, GetMsg("Syntax: /paste or /placeback TARGETFILENAME options values\r\nheight XX - Adjust the height\r\nautoheight true/false - sets best height, carefull of the steep\r\ncheckplaced true/false - checks if parts of the house are already placed or not, if they are already placed, the building part will be removed \r\nblockcollision XX - blocks the entire paste if something the new building collides with something", steamid)); return; }

            var loadname = args[0];

            var success = TryPasteFromPlayer(player, loadname, args.Skip(1).ToArray());

            if (success is string)
            {
                SendReply(player, (string)success);
                return;
            }

            if (lastPastes.ContainsKey(steamid)) lastPastes.Remove(steamid);
            lastPastes.Add(steamid,(List<BaseEntity>)success);

            SendReply(player, GetMsg("You've successfully pasted the structure.", steamid));
        }

        [ChatCommand("undo")]
        void cmdChatUndo(BasePlayer player, string command, string[] args)
        {
            var steamid = player.userID.ToString();
            if (!hasAccess(player, undoPermission)) { SendReply(player, GetMsg("You don't have the permissions to use this command.", steamid)); return; }

            var success = TryUndo(player);

            if (success is string)
            {
                SendReply(player, (string)success);
                return;
            }

            SendReply(player, GetMsg("You've successfully undid what you pasted.", steamid));
        }

        [ChatCommand("placeback")]
        void cmdChatPlaceback(BasePlayer player, string command, string[] args)
        {
            var steamid = player.userID.ToString();
            if (!hasAccess(player, pastePermission)) { SendReply(player, GetMsg("You don't have the permissions to use this command.", steamid)); return; }
            if (args == null || args.Length == 0) { SendReply(player, GetMsg("Syntax: /placeback TARGETFILENAME options values\r\nheight XX - Adjust the height\r\ncheckplaced true/false - checks if parts of the house are already placed or not, if they are already placed, the building part will be removed", steamid)); return; }

            var loadname = args[0];

            var success = TryPlaceback(loadname, player, args.Skip(1).ToArray());

            if (success is string)
            {
                SendReply(player, (string)success);
                return;
            }

            if (lastPastes.ContainsKey(steamid)) lastPastes.Remove(steamid);
            lastPastes.Add(steamid, (List<BaseEntity>)success);

            SendReply(player, GetMsg("You've successfully placed back the structure.", steamid));
        }

        [ChatCommand("copy")]
        void cmdChatCopy(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player, copyPermission)) { SendReply(player, GetMsg("You don't have the permissions to use this command.", player.userID.ToString())); return; }
            if (args == null || args.Length == 0) { SendReply(player, GetMsg("Syntax: /copy TARGETFILENAME options values\r\n radius XX (default 3)\r\n mechanics proximity/building (default building)\r\nbuilding true/false (saves structures or not)\r\ndeployables true/false (saves deployables or not)\r\ninventories true/false (saves inventories or not)", player.userID.ToString())); return; }
            var savename = args[0];

            
            var success = TryCopyFromPlayer(player, savename, args.Skip(1).ToArray());

            if(success is string) {
                SendReply(player, (string)success);
                return;
            }

            SendReply(player, string.Format(GetMsg("The structure was successfully copied as {0}", player.userID.ToString()), savename));
        }
    }
}