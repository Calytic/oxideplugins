// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("AntiGlitch", "Reneb", "2.0.6", ResourceId = 627)]
    class AntiGlitch : RustLegacyPlugin
    {
        public static Vector3 Vector3ABitUp = new Vector3(0f, 0.1f, 0f);
        public static Vector3 Vector3Down = new Vector3(0f, -1f, 0f);
        public static Vector3 Vector3Up = new Vector3(0f, 1f, 0f);
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
        public static int doorLayer;
        public static int terrainLayer;
        private static FieldInfo getweight;
        private static FieldInfo doorstate;
        private static FieldInfo getlooters;
        static Hash<Inventory, NetUser> inventoryLooter = new Hash<Inventory, NetUser>();
        static Hash<NetUser, float> isWallLooting = new Hash<NetUser, float>();
        public static Vector3 Vector3ABitLeft = new Vector3(-0.03f, 0f, -0.03f);
        public static Vector3 Vector3ABitRight = new Vector3(0.03f, 0f, 0.03f);
        public static Vector3 Vector3NoChange = new Vector3(0f, 0f, 0f);
        /////////////////////////
        // Config Management
        /////////////////////////
        public static bool antiPillarStash = true;
        public static bool antiPillarBarricade = true;
        public static bool antiRampStack = true;
        public static float rampstackMax = 2f;
        public static bool antiRampGlitch = true;
        public static bool antiWolfBearGlitch = true;
        public static bool antiFoundationGlitch = true;

        public static bool antiWallloot = true;
        public static bool walllootPunishByKick = true;
        public static bool walllootPunishByBan = true;

        public static bool antiWoodDoorGlitch = true;

        public static bool antiRockGlitch = true;
        public static bool rockGlitchDeath = true;
        public static bool rockGlitchDestroySleepingBag = true;

        public static string playerGlitchDetectionBroadcast = "[color #FFD630] {0} [color red]tried to glitch on this server!";

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
            CheckCfg<bool>("anti Pillar-Stash: activated", ref antiPillarStash);
            CheckCfg<bool>("anti Wall-Loot: activated", ref antiWallloot);
            CheckCfg<bool>("anti Wall-Loot: Punish By Kick", ref walllootPunishByKick);
            CheckCfg<bool>("anti Wall-Loot: Punish By Ban", ref walllootPunishByBan);
            CheckCfg<bool>("anti Pillar-Barricade: activated", ref antiPillarBarricade);
            CheckCfg<bool>("anti RampStack: activated", ref antiRampStack);
            CheckCfg<float>("anti RampStack: max allowed", ref rampstackMax);
            CheckCfg<bool>("anti RampGlitch: activated", ref antiRampGlitch);
            CheckCfg<bool>("anti FoundationGlitch: activated", ref antiFoundationGlitch);
            CheckCfg<bool>("anti StorageBox-Door Glitch: activated", ref antiWoodDoorGlitch);
            CheckCfg<bool>("anti RockGlitch: activated", ref antiRockGlitch);
            CheckCfg<bool>("anti RockGlitch: Kill Player", ref rockGlitchDeath);
            CheckCfg<bool>("anti RockGlitch: Destroy Sleeping Bag", ref rockGlitchDestroySleepingBag);
            CheckCfg<string>("Messages: Glitch Broadcast", ref playerGlitchDetectionBroadcast);
            SaveConfig();
        }
        void Loaded()
        {
            getweight = typeof(StructureMaster).GetField("_weightOnMe", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            terrainLayer = LayerMask.GetMask(new string[] { "Static", "Terrain" });
            doorLayer = LayerMask.GetMask(new string[] { "Mechanical" });
            getlooters = typeof(Inventory).GetField("_netListeners", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            doorstate = typeof(BasicDoor).GetField("state", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        } 
        void Unload()
        {
            var objects = GameObject.FindObjectsOfType(typeof(StructureCheck));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        } 

        public class StructureCheck : MonoBehaviour
        {
            public float radius;
            public Vector3 position;
            public StructureComponent structuremaster;
            public Character owner;
            public bool shouldDestroy = false;

            void Awake()
            {
                this.position = this.gameObject.transform.position;
                structuremaster = GetComponent<StructureComponent>();
            }
            public void CheckCollision()
            { 
                foreach ( Collider collider in Physics.OverlapSphere(this.position, this.radius))
                {
                    if(collider.GetComponent<DeployableObject>() != null)
                    {
                        if (structuremaster.IsPillar())
                        {
                            if (antiPillarStash && collider.GetComponent<DeployableObject>().name == "SmallStash(Clone)")
                                shouldDestroy = true;
                            else if (antiPillarBarricade && collider.GetComponent<DeployableObject>().name == "Barricade_Fence_Deployable(Clone)")
                                shouldDestroy = true;
                        }
                        else if (antiFoundationGlitch && (structuremaster.type == StructureComponent.StructureComponentType.Foundation))
                            shouldDestroy = true;
                        else if (antiRampGlitch && (structuremaster.type == StructureComponent.StructureComponentType.Ramp))
                            shouldDestroy = true;
                    }
                    else if(collider.GetComponent<Character>())
                    {
                        if (antiRampGlitch && (structuremaster.type == StructureComponent.StructureComponentType.Ramp))
                            shouldDestroy = true;
                    }

                    if (shouldDestroy)
                    {
                        if (owner != null && owner.playerClient != null)
                            ConsoleNetworker.SendClientCommand(owner.playerClient.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(string.Format("{0} is blocking the way", collider.gameObject.name)));
                        TakeDamage.KillSelf(structuremaster.GetComponent<IDMain>());
                        return;
                    }
                }
                if(this != null) GameObject.Destroy(this);
            }
        }
        void OnPlayerSpawn(PlayerClient player, bool useCamp, RustProto.Avatar avatar)
        {
            if (!useCamp) return;
            if (!antiRockGlitch) return;
            timer.Once(0.1f, () => CheckPlayerSpawn(player));
        }
        void CheckPlayerSpawn(PlayerClient player)
        {
            cachedPosition = player.lastKnownPosition;
            cachedPosition.y += 100f;
            if (Physics.Raycast(player.lastKnownPosition, Vector3Up, out cachedRaycast, terrainLayer)) {
                cachedPosition = cachedRaycast.point;
            }
            if (!Physics.Raycast(cachedPosition, Vector3Down, out cachedRaycast, terrainLayer)) return;
            if (cachedRaycast.collider.gameObject.name != "") return;
            if (cachedRaycast.point.y < player.lastKnownPosition.y) return;
            AntiGlitchBroadcastAdmins(string.Format("{0} is trying to rock glitch @ {1}",player.userName, player.lastKnownPosition.ToString()));
            Debug.Log(string.Format("{0} is trying to rock glitch @ {1}", player.userName, player.lastKnownPosition.ToString()));
            if (rockGlitchDestroySleepingBag)
            {
                foreach (Collider collider in Physics.OverlapSphere(player.lastKnownPosition, 3f))
                {
                    if(collider.gameObject.name == "SleepingBagA(Clone)")
                        TakeDamage.KillSelf(collider.GetComponent<IDMain>());
                }
            }
            if (rockGlitchDeath)
            {
                ConsoleNetworker.SendClientCommand(player.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe("You have been detected Rock Glitching! Die!"));
                TakeDamage.KillSelf(player.controllable.GetComponent<Character>());
            }
            
        }
        static void AntiGlitchBroadcastAdmins(string message)
        {
            foreach (PlayerClient player in PlayerClient.All)
            {
                if (player.netUser.CanAdmin())
                    ConsoleNetworker.SendClientCommand(player.netPlayer, "chat.add AntiGlitch \"" + message + "\"");
            }
        }
        void OnItemRemoved(Inventory inventory, int slot, IInventoryItem item)
        {
            if (antiWallloot && (inventory.name == "WoodBoxLarge(Clone)" || inventory.name == "WoodBox(Clone)" || inventory.name == "Furnace(Clone)")) { CheckWallLoot(inventory); return; }
        }
        void CheckWallLoot(Inventory inventory)
        {
            NetUser looter = GetLooter(inventory);
            if (looter == null) return;
            if (looter.playerClient == null) return;
            if (inventoryLooter[inventory] != looter) CheckIfWallLooting(inventory, looter);
            if (isWallLooting[looter] == null || isWallLooting[looter] == 0) return;
            if (isWallLooting[looter] > 1)
            {
                Puts(string.Format("{0} - WallLoot @ {1}", looter.playerClient.userName, looter.playerClient.lastKnownPosition.ToString()));
                AntiGlitchBroadcastAdmins(string.Format("{0} - WallLoot @ {1}", looter.playerClient.userName, looter.playerClient.lastKnownPosition.ToString()));
                if (walllootPunishByBan || walllootPunishByKick) Punish(looter.playerClient, "rWallLoot", walllootPunishByKick, walllootPunishByBan);
            }
        }
        static void Punish(PlayerClient player, string reason, bool kick, bool ban)
        {
            if (ban)
            {
                BanList.Add(player.userID, player.userName, reason);
                BanList.Save();
                Interface.CallHook("cmdBan", false, new string[] { player.netPlayer.externalIP.ToString(), reason });
                Debug.Log(string.Format("{0} {1} was auto banned for {2}", player.userID.ToString(), player.userName.ToString(), reason));
            }
            AntiGlitchBroadcastAdmins(string.Format(playerGlitchDetectionBroadcast, player.userName.ToString()));
            if (kick || ban)
            {
                player.netUser.Kick(NetError.Facepunch_Kick_Violation, true);
                Debug.Log(string.Format("{0} {1} was auto kicked for {2}", player.userID.ToString(), player.userName.ToString(), reason));
            }
        }
        bool TraceEyes(Vector3 origin, Ray ray, Vector3 directiondelta, out string objectname, out string modelname, out float distance)
        {
            modelname = string.Empty;
            objectname = string.Empty;
            distance = 0f;
            ray.direction += directiondelta;
            if (!MeshBatchPhysics.Raycast(ray, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) return false;
            if (cachedhitInstance != null) modelname = cachedhitInstance.graphicalModel.ToString();
            distance = cachedRaycast.distance;
            objectname = cachedRaycast.collider.gameObject.name;
            return true;
        }
        void CheckIfWallLooting(Inventory inventory, NetUser netuser)
        {
            isWallLooting.Remove(netuser);
            inventoryLooter[inventory] = netuser;

            var character = netuser.playerClient.controllable.GetComponent<Character>();
            if (!TraceEyes(character.eyesOrigin, character.eyesRay, Vector3NoChange, out cachedObjectname, out cachedModelname, out cachedDistance)) return;
            if (inventory.name != cachedObjectname) return;
            float distance = cachedDistance;
            if (TraceEyes(character.eyesOrigin, character.eyesRay, Vector3ABitLeft, out cachedObjectname, out cachedModelname, out cachedDistance))
            {
                if (cachedDistance < distance)
                    if (cachedModelname.Contains("pillar") || cachedModelname.Contains("doorframe") || cachedModelname.Contains("wall"))
                        isWallLooting[netuser]++;
            }
            if (TraceEyes(character.eyesOrigin, character.eyesRay, Vector3ABitRight, out cachedObjectname, out cachedModelname, out cachedDistance))
            {
                if (cachedDistance < distance)
                    if (cachedModelname.Contains("pillar") || cachedModelname.Contains("doorframe") || cachedModelname.Contains("wall"))
                        isWallLooting[netuser]++;
            }
            return;
        }
        NetUser GetLooter(Inventory inventory)
        {
            var looters = getlooters.GetValue(inventory);
            if (looters == null) return null;
            foreach (uLink.NetworkPlayer netplayer in (HashSet<uLink.NetworkPlayer>)looters)
            {
                return (NetUser)netplayer.GetLocalData();
            }
            return null;
        }
        [HookMethod("OnItemDeployed")]
        void OnItemDeployed(DeployableObject component, NetUser netuser)
        {
            if (!antiWoodDoorGlitch) return;
            if (component.gameObject.name == "WoodBoxLarge(Clone)")
            {
                foreach (Collider collider in Physics.OverlapSphere(component.transform.position + Vector3Up, 1.2f, doorLayer))
                {
                    if (collider.gameObject.name == "MetalDoor(Clone)")
                    {
                        if (doorstate.GetValue(collider.GetComponent<BasicDoor>()).ToString() == "Opened" || doorstate.GetValue(collider.GetComponent<BasicDoor>()).ToString() == "Opening")
                        {
                            ConsoleNetworker.SendClientCommand(netuser.playerClient.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(string.Format("{0} should be closed before trying to build here", collider.gameObject.name.ToString().Replace("(Clone)", ""))));
                            //item.character.GetComponent<Inventory>().AddItemAmount(item.datablock, 1);
                            timer.Once(0.01f, () => NetCull.Destroy(component.gameObject));
                            return;
                        }
                    }
                }
                foreach (Collider collider in Physics.OverlapSphere(component.transform.position + Vector3Up, 0.65f, doorLayer))
                {
                    if (collider.gameObject.name == "MetalDoor(Clone)")
                    {
                        ConsoleNetworker.SendClientCommand(netuser.playerClient.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(string.Format("{0} is blocking the way", collider.gameObject.name.ToString().Replace("(Clone)", ""))));
                        //item.character.GetComponent<Inventory>().AddItemAmount(item.datablock, 1);
                        timer.Once(0.01f, () => NetCull.Destroy(component.gameObject));
                        return;
                    }
                }
            }
            else if (component.gameObject.name == "MetalDoor(Clone)")
            {
                foreach (Collider collider in Physics.OverlapSphere(component.transform.position + Vector3Up, 0.65f))
                {
                    if (collider.gameObject.name == "WoodBoxLarge(Clone)")
                    {

                        ConsoleNetworker.SendClientCommand(netuser.playerClient.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(string.Format("{0} is blocking the way", collider.gameObject.name.ToString().Replace("(Clone)", ""))));
                        //item.character.GetComponent<Inventory>().AddItemAmount(item.datablock, 1);
                        timer.Once(0.01f, () => NetCull.Destroy(component.gameObject));
                        return;
                    }
                }
            }
            
        }
        [HookMethod("OnStructureBuilt")]
        void OnStructureBuilt(StructureComponent component, NetUser netuser)
        {
            var structurecheck = component.gameObject.AddComponent<StructureCheck>();
            structurecheck.owner = netuser.playerClient.controllable.GetComponent<Character>();
            structurecheck.radius = 0f;
            if ((antiPillarStash || antiPillarBarricade) && component.IsPillar())
            {
                structurecheck.radius = 0.2f;
            }
            else if (antiFoundationGlitch && (component.type == StructureComponent.StructureComponentType.Foundation))
            {
                structurecheck.radius = 3.0f;
                structurecheck.position.y += 2f;
            }
            else if (component.type == StructureComponent.StructureComponentType.Ramp) 
            {
                if (antiRampStack)
                {
                    if (MeshBatchPhysics.Raycast(structurecheck.position + Vector3ABitUp, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance))
                    {
                        if (cachedhitInstance != null)
                        {
                            cachedComponent = cachedhitInstance.physicalColliderReferenceOnly.GetComponent<StructureComponent>();
                            if (cachedComponent.type == StructureComponent.StructureComponentType.Foundation || cachedComponent.type == StructureComponent.StructureComponentType.Ceiling)
                            {
                                var weight = getweight.GetValue(cachedComponent._master) as Dictionary<StructureComponent, HashSet<StructureComponent>>;
                                int ramps = 0;
                                if(weight.ContainsKey(cachedComponent))
                                {
                                    foreach(StructureComponent structure in weight[cachedComponent])
                                    {
                                        if(structure.type == StructureComponent.StructureComponentType.Ramp)
                                        {
                                            ramps++;
                                        }
                                    }
                                }
                                if(ramps > rampstackMax)
                                {
                                    TakeDamage.KillSelf(component.GetComponent<IDMain>());
                                    if (structurecheck.owner != null && structurecheck.owner.playerClient != null)
                                        ConsoleNetworker.SendClientCommand(structurecheck.owner.playerClient.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(string.Format("You are not allowed to stack more than {0} ramps", rampstackMax.ToString())));
                                    timer.Once(0.01f, () => GameObject.Destroy(structurecheck));
                                    return;
                                }
                            }
                        }
                    }
                }
                if (antiRampGlitch)
                {
                    structurecheck.radius = 3.0f;
                    structurecheck.position.y += 2f;
                }
            }
            timer.Once(0.05f, () => { if (structurecheck != null) structurecheck.CheckCollision(); });
        }
    }
}
 