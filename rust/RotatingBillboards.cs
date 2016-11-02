using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("RotatingBillboards", "k1lly0u", "0.1.0", ResourceId = 0)]
    class RotatingBillboards : RustPlugin
    {
        #region Fields
        StoredData storedData;
        private DynamicConfigFile data;

        private List<Rotator> billBoards;
        static RotatingBillboards instance;

        private Vector3 eyesAdjust;
        private FieldInfo serverinput;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            billBoards = new List<Rotator>();
            data = Interface.Oxide.DataFileSystem.GetFile("billboard_data");
            eyesAdjust = new Vector3(0f, 1.5f, 0f);
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            lang.RegisterMessages(Messages, this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            instance = this;
            FindAllEntities();
        }
        void OnEntityKill(BaseNetworkable netEntity)
        {
            if (netEntity?.net?.ID == null) return;
            if (storedData.data.Contains(netEntity.net.ID))
            {
                storedData.data.Remove(netEntity.net.ID);
                return;
            }
        }
        void Unload()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<Rotator>();
            if (objects != null)
            {
                foreach(var obj in objects)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
        }
        #endregion

        #region Class
        class Rotator : Signage
        {
            private Signage entity;
            private float secsToTake;
            private float secsTaken;

            private Vector3 initialRot;
            private Vector3 startRot;
            private Vector3 endRot;

            private bool isRotating;

            void Awake()
            {
                entity = GetComponent<Signage>();                
                initialRot = entity.transform.eulerAngles;
                secsToTake = instance.configData.RotationSpeed;
                startRot = new Vector3(entity.transform.eulerAngles.x, 0.01f, entity.transform.eulerAngles.z);
                endRot = new Vector3(entity.transform.eulerAngles.x, 359.99f, entity.transform.eulerAngles.z);
                isRotating = false;
            }            
            void Destroy()
            {
                entity.transform.eulerAngles = initialRot;
                entity.transform.hasChanged = true;
                entity.SendNetworkUpdateImmediate();
                Destroy(this);
            }
            void FixedUpdate()
            {
                if (!isRotating) return;
                secsTaken = secsTaken + UnityEngine.Time.deltaTime;
                float single = Mathf.InverseLerp(0f, secsToTake, secsTaken);
                entity.transform.eulerAngles = Vector3.Lerp(startRot, endRot, single);
                if (single >= 1)
                {
                    entity.transform.eulerAngles = startRot;
                    secsTaken = 0;
                }
                entity.transform.hasChanged = true;
                entity.SendNetworkUpdateImmediate();
            }
            public void ToggleRotation()
            {
                if (isRotating)
                    isRotating = false;
                else isRotating = true;
            }
            public bool IsRotating() => isRotating;
        }
        #endregion

        #region Functions
        void FindAllEntities()
        {
            var signs = UnityEngine.Object.FindObjectsOfType<Signage>();
            foreach(var sign in signs)
            {
                if (sign == null) continue;
                if (storedData.data.Contains(sign.net.ID))
                {
                    if (!sign.GetComponent<Rotator>())
                    {
                        var rotator = sign.gameObject.AddComponent<Rotator>();
                        rotator.enabled = true;
                        billBoards.Add(rotator);
                        rotator.ToggleRotation();
                    }
                }
            }
        }
        object FindEntityFromRay(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            Ray ray = new Ray(player.eyes.position, Quaternion.Euler(input.current.aimAngles) * Vector3.forward);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 20))
                return null;

            var hitEnt = hit.collider.GetComponentInParent<Signage>();
            if (hitEnt != null)
                return hitEnt;
            return null;
        }
        #endregion

        #region Commands
        [ChatCommand("rot")]
        void cmdRot(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            if (args.Length == 0)
            {
                SendReply(player, msg("/rot add - Adds a rotator to the sign you are looking at", player.UserIDString));
                SendReply(player, msg("/rot remove - Removes a rotator from the sign you are looking at", player.UserIDString));
                SendReply(player, msg("/rot remove all - Removes all rotators and wipes data", player.UserIDString));
                SendReply(player, msg("/rot start - Starts the rotation of the sign you are looking at", player.UserIDString));
                SendReply(player, msg("/rot stop - Stops the rotation of the sign you are looking at", player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "add":
                    {
                        var entity = FindEntityFromRay(player);
                        if (entity != null)
                        {
                            Signage sign = entity as Signage;
                            if (!storedData.data.Contains(sign.net.ID))
                            {
                                storedData.data.Add(sign.net.ID);
                                var rotator = sign.gameObject.AddComponent<Rotator>();
                                rotator.enabled = true;
                                billBoards.Add(rotator);
                                rotator.ToggleRotation();
                                SaveData();
                                SendReply(player, msg("You have successfully created a rotating billboard"));
                            }
                            else SendReply(player, msg("This sign already has a rotator attached to it"));
                        }
                        else SendReply(player, msg("Unable to find a valid sign"));
                    }
                    return;
                case "remove":
                    if (args.Length == 2 && args[1].ToLower() == "all")
                    {
                        foreach (var rotator in billBoards)                        
                            UnityEngine.Object.Destroy(rotator);                        
                        billBoards.Clear();
                        storedData.data.Clear();
                        SaveData();
                        SendReply(player, msg("Removed all rotating billboards"));
                    }
                    else
                    {
                        var entity = FindEntityFromRay(player);
                        if (entity != null)
                        {
                            BaseEntity sign = (entity as BaseEntity);
                            if (sign.GetComponent<Rotator>())
                            {
                                billBoards.Remove(sign.GetComponent<Rotator>());
                                UnityEngine.Object.Destroy(sign.GetComponent<Rotator>());
                                if (storedData.data.Contains(sign.net.ID))                                
                                    storedData.data.Remove(sign.net.ID);
                                SaveData();
                                SendReply(player, msg("You have successfully removed this rotating billboard"));
                            }
                            else SendReply(player, msg("This sign does not have a rotator attached to it"));
                        }
                        else SendReply(player, msg("Unable to find a valid sign"));
                    }
                    return;
                case "start":
                    {
                        var entity = FindEntityFromRay(player);
                        if (entity != null)
                        {
                            Rotator sign = (entity as BaseEntity).GetComponent<Rotator>();
                            if (sign != null)
                            {
                                if (sign.IsRotating())
                                    SendReply(player, msg("This sign is already rotating"));
                                else
                                {
                                    sign.ToggleRotation();
                                    SendReply(player, msg("Rotation started"));
                                }
                                return;
                            }
                            else SendReply(player, msg("This sign does not have a rotator attached to it"));
                        }
                        else SendReply(player, msg("Unable to find a valid sign"));
                    }
                    return;
                case "stop":
                    {
                        var entity = FindEntityFromRay(player);
                        if (entity != null)
                        {
                            Rotator sign = (entity as BaseEntity).GetComponent<Rotator>();
                            if (sign != null)
                            {
                                if (!sign.IsRotating())
                                    SendReply(player, msg("This sign is already stopped"));
                                else
                                {
                                    sign.ToggleRotation();
                                    SendReply(player, msg("Rotation stopped"));
                                }
                                return;
                            }
                            else SendReply(player, msg("This sign does not have a rotator attached to it"));
                        }
                        else SendReply(player, msg("Unable to find a valid sign"));
                    }
                    return;
                default:
                    break;
            }
        }

        string msg(string key, string userId = null) => lang.GetMessage(key, this, userId);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"Unable to find a valid sign", "Unable to find a valid sign" },
            {"This sign does not have a rotator attached to it","This sign does not have a rotator attached to it" },
            {"Rotation stopped","Rotation stopped" },
            {"This sign is already stopped","This sign is already stopped" },
            {"Rotation started","Rotation started" },
            {"This sign is already rotating","This sign is already rotating" },
            {"You have successfully removed this rotating billboard","You have successfully removed this rotating billboard" },
            {"Removed all rotating billboards","Removed all rotating billboards" },
            {"You have successfully created a rotating billboard","You have successfully created a rotating billboard" },
            {"/rot stop - Stops the rotation of the sign you are looking at","/rot stop - Stops the rotation of the sign you are looking at" },
            {"/rot start - Starts the rotation of the sign you are looking at","/rot start - Starts the rotation of the sign you are looking at" },
            {"/rot remove all - Removes all rotators and wipes data","/rot remove all - Removes all rotators and wipes data" },
            {"/rot remove - Removes a rotator from the sign you are looking at","/rot remove - Removes a rotator from the sign you are looking at" },
            {"/rot add - Adds a rotator to the sign you are looking at","/rot add - Adds a rotator to the sign you are looking at" }
        };
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public float RotationSpeed { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                RotationSpeed = 5f
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData() => data.WriteObject(storedData);
        void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }
        class StoredData
        {
            public List<uint> data = new List<uint>();
        }
        #endregion
    }
}
