using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("RepairTool", "k1lly0u", "0.1.1", ResourceId = 1883)]
    class RepairTool : RustPlugin
    {
        #region Fields
        private static FieldInfo serverinput;
        private string panelName = "RTUI";
        private List<Repairer> activeRepairers = new List<Repairer>();
        #endregion

        #region Oxide Hooks
        void OnServerInitialized() => InitPlugin();
        void Unload() => DestroyAllPlayers();
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player.GetComponent<Repairer>())
                if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                {
                    if (!player.IsConnected() || player.IsDead()) { player.GetComponent<Repairer>().DestroyComponent(); return; }

                    if (player.GetActiveItem() != null)
                    {
                        MSG(player, GetMSG("HandsFull"));
                        return;
                    }
                    player.GetComponent<Repairer>().FindTarget();
                }
        }
        #endregion

        #region UI
        private void CreateUI(BasePlayer player, int time, string name)
        {
            var element = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = "0.2 0.2 0.2 0.7"},
                        RectTransform = {AnchorMin = "0.005 0.89", AnchorMax = "0.2 0.99"},
                    },
                    new CuiElement().Parent = "Overlay",
                    panelName
                }
            };
            element.Add(new CuiLabel
            {
                Text = { FontSize = 20, Align = TextAnchor.MiddleCenter, Text = $"Repair time remaining: {time}" },
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "1 1" }
            },
            panelName);
            element.Add(new CuiLabel
            {
                Text = { FontSize = 16, Align = TextAnchor.MiddleCenter, Text = name },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.5" }
            },
            panelName);
            CuiHelper.AddUi(player, element);
        }
        private void DestroyUI(BasePlayer player) => CuiHelper.DestroyUi(player, panelName);
        #endregion

        #region Functions
        private void InitPlugin()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "Repaired", "Repaired " },
                { "RepairNum", "You have repaired {0} entities across the map"},
                { "NoRepair", "Unable to find any repairable entities" },
                { "RepairRad", "You have repaired {0} entities in a {1} radius" },
                { "RepairMap", "You have repaired {0} entities across the map" },
                { "NoRad", "You must enter a radius amount" },
                { "ChatAll", "/rt all - Repairs everything on the map" },
                { "ChatRad", "/rt radius <radius> - Repairs all items found in <radius>" },
                { "ChatRepair", "/rt repair <opt:time> - Activates the repair tool for <time>" },
                { "Chat", "- Chat Commands" },
                { "Deactive", "Repair tool de-activated" },
                { "MaxHealth", "The target already has max health" },
                { "HandsFull", "You cannot use the repair tool with a item in your hands" }
            }, this);
            permission.RegisterPermission("repairtool.use", this);
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }
        private object RepairEntity(BaseEntity entity)
        {
            var r = entity.GetComponent<ResourceDispenser>();
            var e = entity.GetComponent<BaseCombatEntity>();
            if (e != null)
            {
                if (e.health != e.MaxHealth())
                {
                    if (r != null)
                        r.fractionRemaining = 1f;
                    e.health = e.MaxHealth();
                    e.SendNetworkUpdate();
                    return true;
                }
                return GetMSG("MaxHealth");
            }            
            return false;
        }
        private BaseEntity FindEntity(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles) * Vector3.forward; 
            var rayResult = Ray(player, currentRot);
            if (rayResult is BaseEntity)
            {
                var ent = rayResult as BaseEntity;
                return ent;
            }
            else
            {
                var ent = FindBuildingBlock(new Ray(player.eyes.position, Quaternion.Euler(input.current.aimAngles) * Vector3.forward), 50);
                if (ent == null) return null;
                return ent;
            }           
            
        }
        private object Ray(BasePlayer player, Vector3 Aim)
        {            
            var hits = Physics.RaycastAll(player.transform.position + new Vector3(0f, 1.5f, 0f), Aim);
            float distance = 50f;
            object target = null;

            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<BaseEntity>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<BaseEntity>();
                    }
                }               
            }
            return target;
        }
        private BaseEntity FindBuildingBlock(Ray ray, float distance)
        {
            RaycastHit hit;
            if (!UnityEngine.Physics.Raycast(ray, out hit, distance, LayerMask.GetMask(new string[] { "Construction", "Deployable", "Prevent Building", "Deployed" })))
                return null;
            return hit.GetEntity();
        }
        private void MSG(BasePlayer player, string msg, bool title = true)
        {            
            msg = "<color=#939393>" + msg + "</color>";
            if (title) msg = "<color=#FF8C00>Repair Tool:</color> " + msg;
            SendReply(player, msg);
        }
        private string GetMSG(string key) => lang.GetMessage(key, this);
        private void DestroyAllPlayers()
        {
            for (int i = 0; i < activeRepairers.Count - 1; i++)                
                if (activeRepairers[i] != null)
                    activeRepairers[i].DestroyComponent();
        }
        #endregion

        #region Chat Commands
        [ChatCommand("rt")]
        private void cmdRA(BasePlayer player, string command, string[] args)
        {
            if (HasPerm(player))
            {
                if (player.GetComponent<Repairer>())
                {
                    player.GetComponent<Repairer>().DestroyComponent();
                    MSG(player, GetMSG("Deactive"));
                    return;
                }
                if (args == null || args.Length == 0)
                {
                    MSG(player, GetMSG("Chat"));
                    MSG(player, GetMSG("ChatRepair"), false);
                    MSG(player, GetMSG("ChatRad"), false);
                    MSG(player, GetMSG("ChatAll"), false);
                    return;
                }
                else switch (args[0].ToLower())
                    {
                        case "repair":
                            {
                                int time = 30;
                                if (args.Length > 1)
                                    int.TryParse(args[1], out time);
                                activeRepairers.Add(player.gameObject.AddComponent<Repairer>());
                                player.GetComponent<Repairer>().InitPlayer(player, time, this);
                            }
                            return;
                        case "radius":
                            if (args.Length > 1)
                            {
                                int radius;
                                if (!int.TryParse(args[1], out radius))
                                {
                                    MSG(player, GetMSG("NoRad"));
                                    return;
                                }
                                List<BaseEntity> foundEntities = new List<BaseEntity>();
                                Vis.Entities(player.transform.position, radius, foundEntities);
                                if (foundEntities == null)
                                {
                                    MSG(player, GetMSG("NoRepair"));
                                    return;
                                }
                                int i = 0;
                                foreach (var entity in foundEntities)
                                {
                                    var success = RepairEntity(entity);
                                    if (success is string) continue;
                                    if (success is bool)
                                        if ((bool) success)
                                            i++;
                                }
                                MSG(player, string.Format(GetMSG("RepairRad"), i, radius));
                            }
                            return;
                        case "all":
                            {
                                var foundEntities = UnityEngine.Object.FindObjectsOfType<BaseEntity>();                                
                                if (foundEntities == null)
                                {
                                    MSG(player, GetMSG("NoRepair"));
                                    return;
                                }
                                int i = 0;
                                foreach (var entity in foundEntities)
                                {
                                    var success = RepairEntity(entity);
                                    if (success is string) continue;
                                    if (success is bool)
                                        if ((bool)success)
                                            i++;
                                }
                                MSG(player, string.Format(GetMSG("RepairMap"), i));
                            }
                            return;
                    }
            }
        }
        private bool HasPerm(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "repairtool.use")) return true;
            else if (player.IsAdmin()) return true;
            return false;
        }
        #endregion

        #region Player Class
        class Repairer : MonoBehaviour
        {
            public BasePlayer player;
            public int TimeRemaining;
            public InputState inputState;
            RepairTool ra;

            public void InitPlayer(BasePlayer p, int time, RepairTool repairtool)
            {
                player = p;
                TimeRemaining = time;
                ra = repairtool;
                ra.activeRepairers.Add(this);
                InvokeRepeating("RefreshGUI", 0.01f, 1);
            }
            public void FindTarget()
            {                
                var entity = ra.FindEntity(player);
                if (entity == null) return;
                var success = ra.RepairEntity(entity);
                if (success is string)
                {
                    ra.MSG(player, (string)success);
                    return;
                }
                else if (success is bool)
                    if ((bool)success)
                    {
                        string name = entity.ShortPrefabName.Replace(".prefab", "").Replace("_deployed", "").Replace(".deployed", "").Replace("_", " ").Replace(".", " ");
                        ra.MSG(player, ra.GetMSG("Repaired") + name);
                    }
            }
            private void RefreshGUI()
            {
                DestroyGUI();
                if (TimeRemaining > 0)
                {
                    string name = "---";
                    var entity = ra.FindEntity(player);
                    if (entity != null) name = entity.ShortPrefabName.Replace(".prefab", "").Replace("_deployed", "").Replace(".deployed", "").Replace("_", " ").Replace(".", " ");
                    ra.CreateUI(player, TimeRemaining, name);
                }
                else DestroyComponent();
                TimeRemaining--;
            }
            void DestroyGUI() => ra.DestroyUI(player);
            public void DestroyComponent()
            {
                CancelInvoke("RefreshGUI");
                ra.activeRepairers.Remove(this);
                DestroyGUI();
                UnityEngine.Object.Destroy(this);
            }
        }   
        #endregion
    }
}
