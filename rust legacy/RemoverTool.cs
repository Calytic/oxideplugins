using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("RemoverTool", "Reneb", "1.0.7")]
    class RemoverTool : RustLegacyPlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;
        [PluginReference]
        Plugin Share;
         
        private static FieldInfo structureComponents;

        static Hash<string, string> deployableCloneToGood = new Hash<string, string>();
        static Hash<string,string> structureCloneToGood = new Hash<string, string>();
        private Dictionary<string, ItemDataBlock> displaynameToDataBlock = new Dictionary<string, ItemDataBlock>();
        float cachedSeconds;
        string cachedType;
        RemoveHandler cachedRemoveHandler;
        StructureComponent cachedStructure;
        DeployableObject cachedDeployable;
        StructureMaster cachedMaster;
        List<object> cachedListObject;
        DeployableObject lastRemovedDeployable;
        StructureComponent lastRemovedStructure;

        public class RemoveHandler : MonoBehaviour
        {
            public PlayerClient playerclient;
            public Inventory inventory;
            public string removeType;
            public float deactivateTime;
            public string userid;

            void Awake()
            {
                playerclient = GetComponent<PlayerClient>();
                enabled = false;
                userid = playerclient.userID.ToString();
            }
            public void Activate()
            {
                enabled = true;
                inventory = playerclient.rootControllable.idMain.GetComponent<Inventory>();
                ConsoleNetworker.SendClientCommand(playerclient.netUser.networkPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(string.Format(removeActivated,removeType,(deactivateTime - Time.realtimeSinceStartup).ToString())));
            }
            void OnDestroy()
            {
                if(playerclient != null && playerclient.netUser != null)
                    ConsoleNetworker.SendClientCommand(playerclient.netUser.networkPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(removeDeactivated));
            }
            void FixedUpdate()
            {
                if (Time.realtimeSinceStartup > deactivateTime) DeactivateRemover(playerclient.netUser);
            }
        }

        string noAccess = "You don't have access to this command";
        static string removeDeactivated = "RemoverTool deactivated";
        static string removeActivated = "RemoverTool ({0}) activated for {1} seconds";
        string noRemoveAccess = "You are not allowed to remove this";
        string wrongArguments = "Wrong arguments";
        string carryingWeight = "The structure is carrying something on him";
        string noTargetPlayer = "Target Player doesn't exist";
        float autoDeactivate = 30f;
        float maxAutoDeactivate = 120f;

        bool canRefund = true;
        bool playerCanRemove = true;
        bool antiFloat = true;
        bool useShare = true;

        static Dictionary<string, object> deployableList = new Dictionary<string, object>();
        static Dictionary<string, object> structureList = new Dictionary<string, object>();
        static Dictionary<string, object> refundList = new Dictionary<string, object>();

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void OnServerInitialized()
        {
        	
        	
        	if (!permission.PermissionExists("canremove"))
                permission.RegisterPermission("canremove", this);
            structureComponents = typeof(StructureMaster).GetField("_structureComponents", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            
            
            InitializeTable();
            deployableList = GetDeployableList();
        	structureList = GetStructureList();
        	refundList = GetBlueprints();
            
        
            CheckCfg<bool>("Remove: For Players", ref playerCanRemove);
            CheckCfg<string>("Messages: No Access", ref noAccess);
            CheckCfg<string>("Messages: Wrong Arguments", ref wrongArguments);
            CheckCfg<string>("Messages: Remove Activated {0} is the type, {1} the seconds", ref removeActivated);
            CheckCfg<string>("Messages: Remove Deactivated", ref removeDeactivated);
            CheckCfg<string>("Messages: Not Allowed to remove the object", ref noRemoveAccess);
            CheckCfg<string>("Messages: structure carrying weight", ref carryingWeight);
            CheckCfg<string>("Messages: No Target player", ref noTargetPlayer);
            CheckCfg<float>("Remove: Auto Deactivate time", ref autoDeactivate);
            CheckCfg<float>("Remove: Max Allowed Deactivate time", ref maxAutoDeactivate);
            CheckCfg<bool>("Remove: Anti Float", ref antiFloat);
            CheckCfg<Dictionary<string,object>>("Remove: Deployable Remove Allowed", ref deployableList);
            CheckCfg<Dictionary<string, object>>("Remove: Structure Remove Allowed", ref structureList);
            CheckCfg<Dictionary<string, object>>("Refund: Values", ref refundList);
            CheckCfg<bool>("Refund: allowed", ref canRefund);
            CheckCfg<bool>("Settings: use Share", ref useShare);
            SaveConfig();
        }

        void ActivateDeactivateRemover(NetUser netuser, string ttype, float secs, int length)
        {
            cachedRemoveHandler = netuser.playerClient.GetComponent<RemoveHandler>();
            if (cachedRemoveHandler != null && length == 0) { DeactivateRemover(netuser); return; }
            if (cachedRemoveHandler == null) cachedRemoveHandler = netuser.playerClient.gameObject.AddComponent<RemoveHandler>();
            cachedRemoveHandler.deactivateTime = Time.realtimeSinceStartup + secs;
            cachedRemoveHandler.removeType = ttype;
            cachedRemoveHandler.Activate();
        } 
        static void DeactivateRemover(NetUser netuser)
        {
            GameObject.Destroy(netuser.playerClient.GetComponent<RemoveHandler>());
        }

        void OnHurt(TakeDamage takedamage, DamageEvent damage)
        {
            if (damage.attacker.client == null) return;
            if (!(damage.extraData is WeaponImpact)) return;
            if (damage.attacker.client.GetComponent<RemoveHandler>() == null) return;
            TryToRemove(takedamage, damage.attacker.client.GetComponent<RemoveHandler>());
        }
        void TryToRemove(TakeDamage takedamage, RemoveHandler rplayer)
        {

            cachedStructure = takedamage.GetComponent<StructureComponent>();
            cachedDeployable = takedamage.GetComponent<DeployableObject>();
            if(cachedStructure != null && cachedStructure._master != null)
            {
                cachedMaster = cachedStructure._master;
                if(!canRemove(rplayer,cachedMaster.ownerID.ToString())) { SendReply(rplayer.playerclient.netUser, noRemoveAccess); return; }
                if (rplayer.removeType == "all") RemoveAll(cachedMaster, rplayer);
                else SimpleRemove(cachedStructure, rplayer);
            }
            else if(cachedDeployable != null)
            {
                if (!canRemove(rplayer, cachedDeployable.ownerID.ToString())) { SendReply(rplayer.playerclient.netUser, noRemoveAccess); return; }
                DeployableRemove(cachedDeployable, rplayer);
            }
        }
        void DeployableRemove(DeployableObject deployable, RemoveHandler rplayer)
        {
            if(!canRemoveDeployable(deployable, rplayer.removeType)) { SendReply(rplayer.playerclient.netUser, noRemoveAccess); return; }
            if (canRefund) TryRefund(deployableCloneToGood[deployable.gameObject.name], rplayer);
            lastRemovedDeployable = deployable;
            TakeDamage.KillSelf(deployable.GetComponent<IDMain>());
        }
        void SimpleRemove(StructureComponent structurecomponent, RemoveHandler rplayer)
        {
            object canremove = canRemoveStructure(structurecomponent, rplayer.removeType);
            if(canremove is string) { SendReply(rplayer.playerclient.netUser, canremove as string); return; }
            if(!(bool)canremove) return;
            if (canRefund) TryRefund(structureCloneToGood[structurecomponent.gameObject.name], rplayer);
            lastRemovedStructure = structurecomponent;
            TakeDamage.KillSelf(structurecomponent.GetComponent<IDMain>());
        } 
        void TryRefund(string gameobjectname, RemoveHandler rplayer)
        {
            if (!refundList.ContainsKey(gameobjectname)) return;
            foreach(object ingredients in (List<object>)refundList[gameobjectname])
            {
                cachedListObject = ingredients as List<object>;
                if (cachedListObject == null || cachedListObject.Count == 0) continue;
                if (displaynameToDataBlock.ContainsKey(cachedListObject[0].ToString().ToLower()))
                {
                    rplayer.inventory.AddItemAmount(displaynameToDataBlock[cachedListObject[0].ToString().ToLower()], Convert.ToInt32(cachedListObject[1]));
                    Rust.Notice.Inventory(rplayer.playerclient.netPlayer, string.Format("{0} x {1}", cachedListObject[0].ToString(), cachedListObject[1].ToString()));
                }
            }
        }
        void RemoveAll(StructureMaster master, RemoveHandler rplayer)
        {
            foreach(StructureComponent comp in (HashSet<StructureComponent>)structureComponents.GetValue(master))
            {
                TakeDamage.KillSelf(comp.GetComponent<IDMain>());
            }
        }
        bool canRemoveDeployable(DeployableObject deployable, string ttype)
        {
            if (lastRemovedDeployable == deployable) return false;
            if (ttype == "admin" || ttype == "all") return true;
            if (deployableCloneToGood[deployable.gameObject.name] == null) return false;
            return (bool)deployableList[deployableCloneToGood[deployable.gameObject.name]];
        }
        object canRemoveStructure(StructureComponent structurecomponent, string ttype)
        {
            if (lastRemovedStructure == structurecomponent) return false;
            if (ttype == "admin" || ttype == "all") return true;
            if (antiFloat && structurecomponent._master.ComponentCarryingWeight(structurecomponent)) return carryingWeight;
            if (structureCloneToGood[structurecomponent.gameObject.name] == null) return noRemoveAccess;
            if (!(bool)structureList[structureCloneToGood[structurecomponent.gameObject.name]]) return noRemoveAccess;
            return true;
        }
        bool canRemove(RemoveHandler rplayer, string ownerid)
        {
            if (rplayer.removeType == "admin" || rplayer.removeType == "all") return true;
            else if (rplayer.removeType == "normal" && rplayer.userid == ownerid) return true;
            else if (useShare)
            {
                var share = Share?.Call("isSharing", ownerid, rplayer.userid);
                if (share is bool) return (bool)share;
            } 
            return false;
        }  
        static Dictionary<string, object> GetDeployableList()
        {
            var deployables = new Dictionary<string, object>();
            deployableCloneToGood.Clear();
            var objects = Resources.FindObjectsOfTypeAll(typeof(DeployableItemDataBlock));
            if (objects != null)
                foreach (DeployableItemDataBlock gameObj in objects)
                {
                    deployableCloneToGood[gameObj.ObjectToPlace.gameObject.name + "(Clone)"] = gameObj.name;
                    if(!deployables.ContainsKey(gameObj.name))
                        deployables.Add(gameObj.name, true);
                } 
             
            return deployables;
        }
        static Dictionary<string, object>  GetStructureList()
        {
            var structures = new Dictionary<string, object>();
            structureCloneToGood.Clear();
            var objects = Resources.FindObjectsOfTypeAll(typeof(StructureComponentDataBlock));
            if (objects != null)
                foreach (StructureComponentDataBlock gameObj in objects)
                {
                    structureCloneToGood[gameObj.structureToPlacePrefab.gameObject.name + "(Clone)"] = gameObj.name;
                    if (!structures.ContainsKey(gameObj.name))
                        structures.Add(gameObj.name, true);
                }
                
            return structures;
        }
        static Dictionary<string,object> GetBlueprints()
        {
            var bps = new Dictionary<string, object>();
            var objects = Resources.FindObjectsOfTypeAll(typeof(BlueprintDataBlock));
            if (objects != null)
                foreach (BlueprintDataBlock gameObj in objects)
                {
                    if (!(deployableList.ContainsKey(gameObj.resultItem.name) || structureList.ContainsKey(gameObj.resultItem.name))) continue;
                    var newingredients = new List<object>();
                    foreach(BlueprintDataBlock.IngredientEntry entry in gameObj.ingredients)
                    {
                        var newingredient = new List<object>();
                        newingredient.Add(entry.Ingredient.name);
                        newingredient.Add(entry.amount.ToString());
                        newingredients.Add(newingredient);
                    }
                    bps.Add(gameObj.resultItem.name, newingredients);
                } 
            return bps;
        }
        bool hasAccess(NetUser netuser, string ttype)
        {
            if (netuser.CanAdmin()) return true;
            else if (ttype == "normal" && playerCanRemove) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "canremove");
        }
        void InitializeTable()
        {
            displaynameToDataBlock.Clear();
            foreach (ItemDataBlock itemdef in DatablockDictionary.All)
            {
                displaynameToDataBlock.Add(itemdef.name.ToString().ToLower(), itemdef);
            }
        }
        [ChatCommand("remove")]
        void cmdChatRemove(NetUser netuser, string command, string[] args)
        {
            cachedSeconds = autoDeactivate;
            cachedType = string.Empty;
            if (args.Length == 0 || (args.Length == 1 && float.TryParse(args[0],out cachedSeconds)) )
            {
                if (cachedSeconds == 0f) cachedSeconds = autoDeactivate;
                if (!hasAccess(netuser, "normal")) { SendReply(netuser, noAccess); return; }
                cachedType = "normal";
            }
            else if(args.Length == 1 || (args.Length == 2 && float.TryParse(args[1],out cachedSeconds)))
            {
                if(cachedSeconds == 0f) cachedSeconds = autoDeactivate;
                if (!hasAccess(netuser, "admin")) { SendReply(netuser, noAccess); return; }
                cachedType = args[0];
            }
            else { SendReply(netuser, wrongArguments); return; }
            switch (cachedType)
            {
                case "normal":
                case "all":
                case "admin":
                    ActivateDeactivateRemover(netuser, cachedType, cachedSeconds, args.Length);
                    break;
                default:
                    var targetplayer = FindPlayer(args[0]);
                    if(targetplayer == null) { SendReply(netuser, noTargetPlayer); return; }
                    Debug.Log(Vector3.Distance(targetplayer.playerClient.lastKnownPosition, netuser.playerClient.lastKnownPosition).ToString());
                    ActivateDeactivateRemover(targetplayer, "normal", cachedSeconds, 1);
                    break;
            }
        }
        NetUser FindPlayer(string name)
        {
            foreach (PlayerClient player in PlayerClient.All)
            {
                if (player.userName == name || player.userID.ToString() == name) return player.netUser;
            }
            return null;
        }
        void SendHelpText(NetUser netuser)
        {

        }
    }
}
