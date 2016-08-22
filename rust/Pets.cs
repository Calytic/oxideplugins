// Reference: RustBuild
// Reference: Behave.Unity.Runtime

using System.Collections.Generic;
using System;
using System.Reflection;

using Oxide.Core;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Pets", "Bombardir", "0.5.5", ResourceId = 851)]
    class Pets : RustPlugin
    {
        static Pets PluginInstance;
        static BUTTON MainButton;
        static BUTTON SecondButton;
        static Dictionary<ulong, PetInfo> SaveNpcList;
        public enum Act { Move, Attack, Eat, Follow, Sleep, None }

        #region NPC Controller Class

        public class NpcControl : MonoBehaviour
        {
            private readonly FieldInfo serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Instance | BindingFlags.NonPublic));
            private float ButtonReload = 0.2f;
            private float DrawReload = 0.05f;
            internal static float LootDistance = 1f;
            internal static float ReloadControl = 60f;
            internal static float MaxControlDistance = 10f;

            internal bool DrawEnabled;
            InputState input;
            float NextTimeToPress;
            float NextTimeToControl;
            float NextTimeToDraw;

            public NpcAI npc;
            public BasePlayer owner;

            void Awake()
            {
                owner = GetComponent<BasePlayer>();
                input = serverinput.GetValue(owner) as InputState;
                enabled = false;
                NextTimeToPress = 0f;
                NextTimeToControl = 0f;
                NextTimeToDraw = 0f;
                DrawEnabled = GlobalDraw;
            }

            void OnAttacked(HitInfo info)
            {
                if (npc && info.Initiator && npc.action != Act.Attack)
                    npc.Attack(info.Initiator.GetComponent<BaseCombatEntity>());
            }

            void FixedUpdate()
            {
                var time = Time.realtimeSinceStartup;
                if (input.WasJustPressed(MainButton) && NextTimeToPress < time)
                {
                    NextTimeToPress = time + ButtonReload;
                    UpdateAction();
                }
                if (DrawEnabled && npc != null && npc.action < Act.Follow && NextTimeToDraw < time)
                {
                    NextTimeToDraw = time + DrawReload;
                    UpdateDraw();
                }
            }

            void UpdateDraw()
            {
                var drawpos = (npc.action == Act.Move ? npc.targetpoint : (npc.targetentity == null ? Vector3.zero : transform.position));
                if (drawpos != Vector3.zero)
                    owner.SendConsoleCommand("ddraw.arrow", DrawReload + 0.02f, npc.action == Act.Move ? Color.cyan : npc.action == Act.Attack ? Color.red : Color.yellow, drawpos + new Vector3(0, 5f, 0), drawpos, 1.5f);
            }

            void UpdateAction()
            {
                if (npc != null && input.IsDown(SecondButton))
                {
                    ChangeFollowAction();
                    return;
                }

                RaycastHit hit;
                if (!Physics.SphereCast(owner.eyes.position, 0.5f, Quaternion.Euler(input.current.aimAngles) * Vector3.forward, out hit) || hit.transform == transform)
                    return;

                if (npc == null)
                {
                    BaseNPC npcPet = hit.transform.GetComponent<BaseNPC>();
                    if (npcPet == null)
                        return;

                    if (hit.distance >= MaxControlDistance)
                    {
                        owner.ChatMessage(CloserMsg);
                        return;
                    }

                    TryGetNewPet(npcPet);
                    return;
                }

                BaseCombatEntity targetentity = hit.transform.GetComponent<BaseCombatEntity>();
                if (targetentity == null)
                {
                    npc.targetpoint = hit.point;
                    npc.action = Act.Move;
                    return;
                }

                if (targetentity == npc.Base)
                {
                    if (hit.distance <= LootDistance)
                        OpenPetInventory();
                }
                else if (targetentity is BaseCorpse)
                {
                    owner.ChatMessage(EatMsg);
                    npc.Attack(targetentity, Act.Eat);
                }
                else
                {
                    owner.ChatMessage(AttackMsg);
                    npc.Attack(targetentity);
                }
            }

            void OpenPetInventory()
            {
                var loot = owner.inventory.loot;
                loot.StartLootingEntity(npc.Base, true);
                loot.AddContainer(npc.inventory);
                loot.SendImmediate();
                owner.ClientRPCPlayer(owner.net.connection, owner, "RPC_OpenLootPanel", "smallwoodbox");
                owner.ChatMessage(OpenInvMsg);
            }

            void ChangeFollowAction()
            {
                if (npc.action == Act.Follow)
                {
                    owner.ChatMessage(UnFollowMsg);
                    npc.action = Act.None;
                }
                else
                {
                    owner.ChatMessage(FollowMsg);
                    npc.Attack(owner.GetComponent<BaseCombatEntity>(), Act.Follow);
                }
            }

            void TryGetNewPet(BaseNPC npcPet)
            {
                var OwnedNpc = npcPet.GetComponent<NpcAI>();
                if (OwnedNpc != null && OwnedNpc.owner != this)
                {
                    owner.ChatMessage(NoOwn);
                    return;
                }

                if (NextTimeToControl >= Time.realtimeSinceStartup)
                {
                    owner.ChatMessage(ReloadMsg);
                    return;
                }

                if (UsePermission && !PluginInstance.HasPermission(owner, "can" + npcPet.mdlPrefab.Get().name.Replace("_skin", "")))
                {
                    owner.ChatMessage(NoPermPetMsg);
                    return;
                }

                NextTimeToControl = Time.realtimeSinceStartup + ReloadControl;

                npc = npcPet.gameObject.AddComponent<NpcAI>();
                npc.owner = this;

                owner.ChatMessage(NewPetMsg);
            }
        }

        #endregion
        #region NPC AI Class

        public class NpcAI : MonoBehaviour
        {
            private readonly MethodInfo SetDeltaTimeMethod = typeof(NPCAI).GetProperty("deltaTime", (BindingFlags.Public | BindingFlags.Instance)).GetSetMethod(true);
            internal static float IgnoreTargetDistance = 70f;
            internal static float HealthModificator = 1.5f;
            internal static float AttackModificator = 2f;
            internal static float SpeedModificator = 1f;

            private float PointMoveDistance = 1f;
            private float TargetMoveDistance = 3f;

            float lastTick;
            float hungerLose;
            float thristyLose;
            float sleepLose;
            double attackrange;

            internal Act action;
            internal Vector3 targetpoint;
            internal BaseCombatEntity targetentity;

            public NpcControl owner;
            public ItemContainer inventory;
            public BaseNPC Base;
            public NPCAI RustAI;
            public NPCMetabolism RustMetabolism;

            void Awake()
            {
               RustAI = GetComponent<NPCAI>();
               RustAI.ServerDestroy();
               RustMetabolism = GetComponent<NPCMetabolism>();
               Base = GetComponent<BaseNPC>();

               lastTick = Time.time;
               targetpoint = Vector3.zero;
               action = Act.None;

               hungerLose = RustMetabolism.calories.max*2 / 12000;
               thristyLose = RustMetabolism.hydration.max*3 / 12000;
               sleepLose = RustMetabolism.sleep.max / 12000;

               inventory = new ItemContainer();
               inventory.ServerInitialize(null, 6);

               Base.enableSaving = false;
               BaseEntity.saveList.Remove(Base);
               Base.InitializeHealth(Base.health * HealthModificator, Base.MaxHealth() * HealthModificator);
               Base.locomotion.gallopSpeed *= SpeedModificator;
               Base.locomotion.trotSpeed *= SpeedModificator;
               Base.locomotion.acceleration *= SpeedModificator;
            }

            void OnDestroy()
            {
                DropUtil.DropItems(inventory, transform.position);
                SaveNpcList.Remove(owner.owner.userID);
                RustAI.ServerInit();

                if (Base.health <= 0)
                    return;

                Base.enableSaving = true;
                BaseEntity.saveList.Add(Base);

                Base.InitializeHealth(Base.health / HealthModificator, Base.MaxHealth() / HealthModificator);
                Base.locomotion.gallopSpeed /= SpeedModificator;
                Base.locomotion.trotSpeed /= SpeedModificator;
                Base.locomotion.acceleration /= SpeedModificator;
            }

            internal void OnAttacked(HitInfo info)
            {
                if (info.Initiator && info.Initiator != owner.owner && action != Act.Attack)
                    Attack(info.Initiator.GetComponent<BaseCombatEntity>());
            }

            void FixedUpdate()
            {
                SetDeltaTimeMethod.Invoke( RustAI, new object[] { Time.time - lastTick });
                if (RustAI.deltaTime < ConVar.Server.TickDelta()) return;
                lastTick = Time.time;
                if (Base.IsStunned()) return;
                Base.Tick();

                if (action != Act.Sleep)
                {
                    RustMetabolism.sleep.MoveTowards(0.0f, RustAI.deltaTime * sleepLose);
                    RustMetabolism.hydration.MoveTowards(0.0f, RustAI.deltaTime * thristyLose);
                    RustMetabolism.calories.MoveTowards(0.0f, RustAI.deltaTime * hungerLose);
                }

                if (action == Act.None)
                    return;

                if (action == Act.Move)
                    if (Vector3.Distance(transform.position, targetpoint) < PointMoveDistance)
                        action = Act.None;
                    else
                        Move(targetpoint);
                else if (action == Act.Sleep)
                    Sleep();
                else if (targetentity == null)
                {
                    action = Act.None;
                    Base.state = BaseNPC.State.Normal;
                }
                else
                {
                    var distance = Vector3.Distance(transform.position, targetentity.transform.position);
                    if (distance >= IgnoreTargetDistance)
                    {
                        action = Act.None;
                        return;
                    }

                    if (action != Act.Follow && distance <= attackrange)
                    {
                        var normalized = (targetentity.transform.position - transform.position).XZ3D().normalized;
                        if (action == Act.Eat)
                        {
                            if (Base.diet.Eat(targetentity))
                            {
                                Base.Heal(Base.MaxHealth() * 0.01f);
                                RustMetabolism.calories.Add(RustMetabolism.calories.max * 0.03f);
                                RustMetabolism.hydration.Add(RustMetabolism.hydration.max * 0.03f);
                            }
                        }
                        else if (Base.attack.Hit(targetentity, AttackModificator, false))
                            transform.rotation = Quaternion.LookRotation(normalized);
                        Base.steering.Face(normalized);
                    }
                    else if (action != Act.Follow || distance > TargetMoveDistance && distance > attackrange)
                        Move(targetentity.transform.position);
                }
            }

            void Sleep()
            {
                Base.state = BaseNPC.State.Sleeping;
                Base.sleep.Recover(2f);
                RustMetabolism.stamina.Run(4f);
                Base.StartCooldown(2f, true);
            }

            void Move(Vector3 point)
            {
                Base.state = BaseNPC.State.Normal;
                RustAI.sense.Think();
                Base.steering.Move((point - transform.position).XZ3D().normalized, point, (int) BLRust.ContextType.Gallop);
            }

            internal void Attack(BaseCombatEntity ent, Act act = Act.Attack)
            {
                targetentity = ent;
                action = act;
                attackrange = Math.Pow(Base._collider.bounds.XZ3D().extents.Max() + Base.attack.range + ent._collider.bounds.XZ3D().extents.Max(), 2);
            }
        }
        #endregion
        #region PetInfo Object to Save
        public class PetInfo
        {
            public uint prefabID;
            public float x, y, z;
            public byte[] inventory;
            internal bool NeedToSpawn;

            public PetInfo()
            {
                NeedToSpawn = true;
            }

            public PetInfo(NpcAI pet)
            {
                x = pet.transform.position.x;
                y = pet.transform.position.y;
                z = pet.transform.position.z;
                prefabID = pet.Base.prefabID;
                inventory = pet.inventory.Save().ToProtoBytes();
                NeedToSpawn = false;
            }
        }
        #endregion

        #region Config & Initialisation

        static bool UsePermission = true;
        static bool GlobalDraw = true;
        static string CfgButton = "USE";
        static string CfgSecButton = "RELOAD";
        static string OpenInvMsg = "Now open your inventory if you want loot pet!";
        static string ReloadMsg = "You can not tame so often! Wait!";
        static string NewPetMsg = "Now you have a new pet!";
        static string CloserMsg = "You need to get closer!";
        static string NoPermPetMsg = "You don't have permission to take this NPC!";
        static string FollowMsg = "Follow command!";
        static string UnFollowMsg = "UnFollow command!";
        static string SleepMsg = "Sleep command!";
        static string AttackMsg = "Attack!";
        static string NoPermMsg = "No Permission!";
        static string ActivatedMsg = "NPC Mode activated!";
        static string DeactivatedMsg = "NPC Mode deactivated!";
        static string NotNpc = "You don't have a pet!";
        static string NpcFree = "Now your per is free!";
        static string NoOwn = "This Npc is already tamed by other player!";
        static string EatMsg = "Time to eat!";
        static string DrawEn = "Draw enabled!";
        static string DrawDis = "Draw disabled!";
        static string DrawSysDis = "Draw system was disabled by administrator!";
        static string InfoMsg = "<color=red>Health: {health}%</color>, <color=orange>Hunger: {hunger}%</color>, <color=cyan>Thirst: {thirst}%</color>, <color=teal>Sleepiness: {sleep}%</color>, <color=lightblue>Stamina: {stamina}%</color>";

        protected override void LoadDefaultConfig() { }

        void Init()
        {
            CheckCfg("Use permissions", ref UsePermission);
            CheckCfg("Enable draw system", ref GlobalDraw);
            CheckCfg("Main button to controll pet", ref CfgButton);
            CheckCfg("Second button to use follow|unfollow", ref CfgSecButton);
            CheckCfg("Reload time to take new Npc", ref NpcControl.ReloadControl);
            CheckCfg("Max distance to take Npc", ref NpcControl.MaxControlDistance);
            CheckCfg("Distance to loot Npc", ref NpcControl.LootDistance);
            CheckCfg("Distance when target will be ignored by NPC", ref NpcAI.IgnoreTargetDistance);
            CheckCfg("Pet's Health Modificator", ref NpcAI.HealthModificator);
            CheckCfg("Pet's Attack Modificator", ref NpcAI.AttackModificator);
            CheckCfg("Pet's Speed Modificator", ref NpcAI.SpeedModificator);
            CheckCfg("New pet msg", ref NewPetMsg);
            CheckCfg("Closer msg", ref CloserMsg);
            CheckCfg("No take perm msg", ref NoPermPetMsg);
            CheckCfg("Follow msg", ref FollowMsg);
            CheckCfg("UnFollow msg", ref UnFollowMsg);
            CheckCfg("Sleep msg", ref SleepMsg);
            CheckCfg("Attack msg", ref AttackMsg);
            CheckCfg("No command perm msg", ref NoPermMsg);
            CheckCfg("Activated msg", ref ActivatedMsg);
            CheckCfg("Deactivated msg", ref DeactivatedMsg);
            CheckCfg("Reload msg", ref ReloadMsg);
            CheckCfg("No pet msg", ref NotNpc);
            CheckCfg("Free pet msg", ref NpcFree);
            CheckCfg("Already tamed msg", ref NoOwn);
            CheckCfg("Eat msg", ref EatMsg);
            CheckCfg("Draw enabled msg", ref DrawEn);
            CheckCfg("Draw disabled msg", ref DrawDis);
            CheckCfg("Draw system disabled msg", ref DrawSysDis);
            CheckCfg("Info msg", ref InfoMsg);
            CheckCfg("Open Inventory msg", ref OpenInvMsg);
            SaveConfig();

            InfoMsg= InfoMsg
                .Replace("{health}", "{0}")
                .Replace("{hunger}", "{1}")
                .Replace("{thirst}", "{2}")
                .Replace("{sleep}", "{3}")
                .Replace("{stamina}", "{4}");

            MainButton = ConvertStringToButton(CfgButton);
            SecondButton = ConvertStringToButton(CfgSecButton);
            PluginInstance = this;

            if (UsePermission)
            {
                permission.RegisterPermission("cannpc", this);
                permission.RegisterPermission("canstag", this);
                permission.RegisterPermission("canbear", this);
                permission.RegisterPermission("canwolf", this);
                permission.RegisterPermission("canchicken", this);
                permission.RegisterPermission("canboar", this);
                permission.RegisterPermission("canhorse", this);
            }

            try { SaveNpcList = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PetInfo>>("Pets"); } catch { }
            if (SaveNpcList == null) SaveNpcList = new Dictionary<ulong, PetInfo>();
        }

        #endregion

        #region Unload Hook (destroy all plugin's objects)

        void Unload()
        {
            OnServerSave();
            DestroyAll<NpcControl>();
            DestroyAll<NpcAI>();
            PluginInstance = null;
        }

        #endregion

        #region Hook OnAttacked for NpcAI

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BaseNPC)
                entity.GetComponent<NpcAI>()?.OnAttacked(hitInfo);
        }

        #endregion

        #region Hook OnPlayerInit (load player's pet)

        void OnPlayerInit(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerInit(player));
                return;
            }
            PetInfo info;
            if (!SaveNpcList.TryGetValue(player.userID, out info) || !info.NeedToSpawn) return;
            Puts("Loading pet...");
            var pet = GameManager.server.CreateEntity(StringPool.Get(info.prefabID), new Vector3(info.x, info.y, info.z));
            if (pet == null) return;
            var comp = player.gameObject.AddComponent<NpcControl>();
            pet.Spawn();
            comp.npc = pet.gameObject.AddComponent<NpcAI>();
            comp.npc.owner = comp;
            comp.npc.inventory.Load(ProtoBuf.ItemContainer.Deserialize(info.inventory));
            info.NeedToSpawn = false;
        }

        #endregion

        #region Hook OnServerSave (save all pets)

        void OnServerSave()
        {
            var pets = UnityEngine.Object.FindObjectsOfType<NpcAI>();
            if (pets == null) return;
            foreach (var pet in pets)
                SaveNpcList[pet.owner.owner.userID] = new PetInfo(pet);
            Interface.Oxide.DataFileSystem.WriteObject("Pets", SaveNpcList);
        }

        #endregion

        #region PET Command (activate/deactivate Npc mode)

        [ChatCommand("pet")]
        void pet(BasePlayer player, string command, string[] args)
        {
            var comp = player.GetComponent<NpcControl>() ?? player.gameObject.AddComponent<NpcControl>();
            if (args.Length == 0)
            {
                player.ChatMessage(comp.enabled ? DeactivatedMsg : ActivatedMsg);
                comp.enabled = !comp.enabled;
                return;
            }

            if (args[0] == "draw")
            {
                if (GlobalDraw)
                    if (comp.DrawEnabled)
                    {
                        comp.DrawEnabled = false;
                        player.ChatMessage(DrawDis);
                    }
                    else
                    {
                        comp.DrawEnabled = true;
                        player.ChatMessage(DrawEn);
                    }
                else
                    player.ChatMessage(DrawSysDis);
                return;
            }

            if (comp.npc)
            {
                switch (args[0])
                {
                    case "free":
                        UnityEngine.Object.Destroy(comp.npc);
                        player.ChatMessage(NpcFree);
                        break;
                    case "sleep":
                        player.ChatMessage(SleepMsg);
                        comp.npc.action = Act.Sleep;
                        break;
                    case "info":
                        var meta = comp.npc.RustMetabolism;
                        player.ChatMessage(string.Format(InfoMsg,
                            Math.Round(comp.npc.Base.health*100/comp.npc.Base.MaxHealth()),
                            Math.Round(meta.hydration.value*100/meta.hydration.max),
                            Math.Round(meta.calories.value*100/meta.calories.max),
                            Math.Round(meta.sleep.value*100/meta.sleep.max),
                            Math.Round(meta.stamina.value*100/meta.stamina.max)));
                        break;
                }
            }
            else
                player.ChatMessage(NotNpc);
        }

        #endregion

        #region Some other plugin methods

        bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        static void DestroyAll<T>()
        {
            var objects = UnityEngine.Object.FindObjectsOfType(typeof(T));
            if (objects == null) return;
            foreach (var gameObj in objects)
                UnityEngine.Object.Destroy(gameObj);
        }

        void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] == null)
                Config[Key] = var;
            else
                try { var = (T) Convert.ChangeType(Config[Key], typeof(T)); }
                catch { Config[Key] = var; }
        }

        static BUTTON ConvertStringToButton(string button)
        {
            try
            {
                return (BUTTON) Enum.Parse(typeof (BUTTON), button);
            }
            catch (Exception)
            {
                return BUTTON.USE;
            }
        }

        #endregion
    }
}
