using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;

using Rust;
using UnityEngine;

using IEnumerator = System.Collections.IEnumerator;

namespace Oxide.Plugins
{
    [Info("TrapFloors", "Cheeze", "1.2", ResourceId = 2038)]
    [Description("Allows players to make trap floors that collapse on non cupboard authorised players")]

    class TrapFloors : RustPlugin
    {
        static readonly DynamicConfigFile DataFile = Interface.Oxide.DataFileSystem.GetFile("TrapFloors");
        static List<Floor> trapFloors = new List<Floor>();
        const string permAdmin = "trapfloors.admin";
        BaseEntity newTrapFloor;

        class Floor
        {
            public uint Id;
            public string Location;
            public ulong PlayerId;
            internal static Floor GetTrap(uint id) => trapFloors.First(x => x.Id == id);
        }

        protected override void LoadDefaultConfig()
        {
            Config["MaxFloors"] = 1;
            Config["EnableFoundationTraps"] = true;
            SaveConfig();
        }

        void Init()
        {
            LoadDefaultMessages();
            trapFloors = DataFile.ReadObject<List<Floor>>();
            permission.RegisterPermission(permAdmin, this);
        }

        void OnServerInitialized()
        {
            foreach (var ent in UnityEngine.Object.FindObjectsOfType<BaseEntity>())
            {
                if (trapFloors.All(x => x.Id != ent.net.ID)) continue;
                newTrapFloor.gameObject.AddComponent<FloorTrap>();
                ent.gameObject.AddComponent<FloorTrap>();
            }
        }

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                ["ADMIN_BAD_SYNTAX"] = "Invalid, use /trapfloor add/remove/list/wipe",
                ["ADMIN_ONLY"] = "That command is for admins only",
                ["BAD_SYNTAX"] = "Incorrect Syntax, /trapfloor add",
                ["END_LIST"] = "End of Trap Floor List",
                ["FLOOR_EXISTS"] = "There is already a floor stored with the same ID",
                ["FLOOR_LIST"] = "Floor ID: {0}, Floor Location: {1}, Player ID: {2}",
                ["FLOOR_SET"] = "Trap Floor set! You have set {0} out of {1} available trap floors",
                ["INSTRUCTIONS"] = "Use: /trapfloor remove <id> || Trap List: /trapfloor list ",
                ["LIST"] = "Trap Floor List",
                ["MAX_REACHED"] = "Cannot create TrapFloor, you have reached the maximum of {0} TrapFloors",
                ["NOT_FLOOR"] = "This is not a floor",
                ["NO_BUILD"] = "You are not authorised here",
                ["NO_FLOOR"] = "No floor detected",
                ["NO_PERMISSION"] = "Sorry, you are not authorised to use this",
                ["REMOVED"] = "Trap floor removed with floor ID: {0}",
                ["WIPED"] = "Trap floors wiped!",
                ["NO_FOUNDATION"] = "You cannot set foundations as traps"
            };
            lang.RegisterMessages(messages, this);
        }

        void OnServerSave() => SaveData();

        void Unloaded()
        {
            foreach (var floor in UnityEngine.Object.FindObjectsOfType<FloorTrap>())
                UnityEngine.Object.Destroy(floor);
        }

        string pName = "<color=orange>TrapFloors:</color> ";

        #region ColliderCheck

        class FloorTrap : MonoBehaviour
        {
            void Awake()
            {
                gameObject.name = "FloorTrap";
                gameObject.layer = (int)Layer.Reserved1;

                var rigidbody = gameObject.GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();

                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                UpdateCollider();
            }

            void UpdateCollider()
            {
                var collider = gameObject.GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(1.5f, 0.25f, 1.5f);
                collider.isTrigger = true;
                collider.enabled = true;
            }

            void OnTriggerEnter(Collider col)
            {
                if (!(col.gameObject.ToBaseEntity() is BasePlayer)) return;
                var player = (BasePlayer)col.gameObject.ToBaseEntity();

                if (player.CanBuild()) return;

                StartCoroutine(KillIt(gameObject.ToBaseEntity()));
                trapFloors.Remove(Floor.GetTrap(gameObject.ToBaseEntity().net.ID));
                SaveData();

                Effect.server.Run("assets/bundled/prefabs/fx/build/repair_failed.prefab", player.transform.position, Vector3.zero, null, true);
            }

            IEnumerator KillIt(BaseEntity ent)
            {
                yield return new WaitForSeconds(0.4f);

                if (!ent.isDestroyed)
                    ent.Kill(BaseNetworkable.DestroyMode.Gib);
            }
        }

        #endregion Collider Check

        void OnEntityDeath(BaseCombatEntity entity)
        {
            if (!(entity is BuildingBlock)) return;
            if ((!entity.name.Contains("floor/floor") || (!entity.name.Contains("foundation")) && (trapFloors.All(x => x.Id != entity.net.ID)))) return;

            trapFloors.Remove(Floor.GetTrap(Convert.ToUInt32(entity.net.ID)));
            SaveData();
        }

        [ChatCommand("trapfloor")]
        void cmdAddTrapFloor(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                PrintToChat(player, pName + LangMsg("BAD_SYNTAX", player.UserIDString));
                return;
            }

            if ((args[0] == "add") || (args[0] != "remove") || (args[0] != "wipe") || (args[0] != "list"))
            {
                switch (args[0])
                {
                    case "add":
                        if (!player.CanBuild())
                        {
                            PrintToChat(player, pName + LangMsg("NO_BUILD", player.UserIDString));
                            return;
                        }

                        int amount = trapFloors.Count(x => x.PlayerId == player.userID);
                        Int32 max = Convert.ToInt32(Config["MaxFloors"]);
                        bool useFoundations = Convert.ToBoolean(Config["EnableFoundationTraps"]); 
                        if (amount < max)
                        {
                            RaycastHit hit;

                            if (Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity)) newTrapFloor = hit.GetTransform().gameObject.ToBaseEntity();

                            if (newTrapFloor == null)
                            {
                                PrintToChat(player, pName + LangMsg("NO_FLOOR", player.UserIDString));
                                return;
                            }

                            if (trapFloors.Any(x => x.Id == newTrapFloor.net.ID))
                            {
                                PrintToChat(player, pName + LangMsg("FLOOR_EXISTS", player.UserIDString));
                                return;
                            }

                            if ((!newTrapFloor.name.Contains("floor")) && (!newTrapFloor.name.Contains("foundation")))
                            {
                                PrintToChat(player, pName + LangMsg("NOT_FLOOR", player.UserIDString));
                                return;
                            }

                            if (newTrapFloor.name.Contains("foundation") && !useFoundations)
                            {
                                PrintToChat(player, pName + LangMsg("NO_FOUNDATION", player.UserIDString));
                                return;
                            }

                            var info = new Floor()
                            {
                                Id = newTrapFloor.net.ID,
                                Location = newTrapFloor.transform.position.ToString(),
                                PlayerId = player.userID
                            };

                            trapFloors.Add(info);
                            amount = trapFloors.Count(x => x.PlayerId == player.userID);
                            PrintToChat(player, pName + LangMsg("FLOOR_SET", player.UserIDString, amount, max));
                            newTrapFloor.gameObject.AddComponent<FloorTrap>();
                        }
                        else
                        {
                            PrintToChat(player, pName + LangMsg("MAX_REACHED", player.UserIDString, max));
                        }
                        break;

                    case "remove":
                        if (!HasPermission(player, permAdmin))
                        {
                            PrintToChat(player, pName + LangMsg("ADMIN_ONLY", player.UserIDString));
                            return;
                        }
                        if (args.Length < 2)
                        {
                            PrintToChat(player, pName + LangMsg("INSTRUCTIONS", player.UserIDString));
                            return;
                        }
                        if (Floor.GetTrap(Convert.ToUInt32(args[1])) != null)
                            trapFloors.Remove(Floor.GetTrap(Convert.ToUInt32(args[1])));
                        player.ChatMessage(LangMsg("REMOVED", player.UserIDString, args[1]));
                        break;

                    case "list":
                        if (!HasPermission(player, permAdmin))
                        {
                            PrintToChat(player, pName + LangMsg("ADMIN_ONLY", player.UserIDString));
                            return;
                        }
                        PrintToChat(player, pName + LangMsg("LIST", player.UserIDString));
                        foreach (var floor in trapFloors)
                            player.ChatMessage(LangMsg("FLOOR_LIST", player.UserIDString, floor.Id, floor.Location, floor.PlayerId));
                        PrintToChat(player, pName + LangMsg("END_LIST", player.UserIDString));
                        break;

                    case "wipe":
                        if (!HasPermission(player, permAdmin))
                        {
                            PrintToChat(player, pName + LangMsg("ADMIN_ONLY", player.UserIDString));
                            return;
                        }
                        trapFloors.Clear();
                        PrintToChat(player, pName + LangMsg("WIPED", player.UserIDString));
                        break;

                    case "default":
                        PrintToChat(player, pName + LangMsg("BAD_SYNTAX", player.UserIDString));
                        break;

                }
                SaveData();
            }
            else
            {
                PrintToChat(player, LangMsg("ADMIN_BAD_SYNTAX", player.UserIDString));
            }
        }

        void Remove(string args, BasePlayer player)
        {
            if (Floor.GetTrap(Convert.ToUInt32(args[1])) != null)
                trapFloors.Remove(Floor.GetTrap(Convert.ToUInt32(args[1])));
            player.ChatMessage(LangMsg("REMOVED", player.UserIDString, args[1]));
        }

        bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        string LangMsg(string key, string uid = null, params object[] args) => string.Format(lang.GetMessage(key, this, uid), args);

        static void SaveData() => DataFile.WriteObject(trapFloors);
    }
}
