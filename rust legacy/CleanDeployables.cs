using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CleanDeployables", "Reneb", "1.0.1", ResourceId = 948)]
    class CleanDeployables : RustLegacyPlugin
    {
        string cachedName;
        float cachedRadius;
        bool hasAccess(NetUser netuser)
        { 
            if (netuser.CanAdmin()) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "canclean");
        }
        object ValidDeploy(string name)
        {
            var objects = Resources.FindObjectsOfTypeAll(typeof(DeployableItemDataBlock));
            if (objects != null)
                foreach (DeployableItemDataBlock gameObj in objects)
                {
                    if(gameObj.name.ToLower() == name)
                    {
                        return gameObj.ObjectToPlace.gameObject.name + "(Clone)";
                    }

                }
            return null; 
        }
        int CleanAllDeployables(string name, bool all)
        {
            int destroyed = 0;
            bool shoulddestroy = true;
            foreach (DeployableObject deployable in UnityEngine.Resources.FindObjectsOfTypeAll<DeployableObject>())
            {
                if (deployable.gameObject.name == name)
                {
                    if (!all && deployable._carrier != null) continue;
                    if (shouldDestroy(deployable.transform.position, all))
                    {
                        NetCull.Destroy(deployable.gameObject);
                        destroyed++;
                    }
                }
            }
            return destroyed;
        }
        int CleanAllSacks(bool all)
        {
            int destroyed = 0;
            foreach (LootableObject lootable in UnityEngine.Resources.FindObjectsOfTypeAll<LootableObject>())
            {
                if (lootable.gameObject.name == "LootSack(Clone)")
                {
                    if (shouldDestroy(lootable.transform.position,all))
                    {
                        NetCull.Destroy(lootable.gameObject);
                        destroyed++;
                    }
                }
            }
            return destroyed;
        }
        void Loaded()
        {

            /*foreach (CharacterDeathDropPrefabTrait deployable in UnityEngine.Resources.FindObjectsOfTypeAll<CharacterDeathDropPrefabTrait>())
            {
                var gameobj = NetCull.InstantiateStatic(deployable.instantiateString, default(Vector3), default(UnityEngine.Quaternion));
                var components = gameobj.GetComponents<UnityEngine.Component>();
                foreach (var comp in components)
                {
                    Debug.Log(comp.ToString());
                }
                Debug.Log( "============= COMPONENTS IN PARENT =============");
                components = gameobj.GetComponentsInParent<UnityEngine.Component>();
                foreach (var comp in components)
                {
                    Debug.Log( comp.ToString());
                }
                Debug.Log( "============= COMPONENTS IN CHILDREN =============");
                components = gameobj.GetComponentsInChildren<UnityEngine.Component>();
                foreach (var comp in components)
                {
                    Debug.Log( comp.ToString());
                }
            }*/
        }
        [ChatCommand("clean")]
        void cmdChatClean(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser)) { SendReply(netuser, "You dont have access to this command."); return; }
            if(args.Length == 0)
            {
                SendReply(netuser, "You must enter deployed item name that you want to clean.");
                SendReply(netuser, "Not using the option: \"all\" will only clean items that are not on a structure.");
                SendReply(netuser, "using the option: \"all\" will only clean all the items.");
                SendReply(netuser, "/clean DEPLOYNAME optional:all");
                return;
            }
            cachedName = args[0].ToLower();
            int totalCleaned = 0;
            bool all = (args.Length > 1 && args[1] == "all") ? true : false;
            switch (args[0].ToLower())
            {
                case "lootsack":
                case "bag":
                case "lootsacks":
                case "bags":
                    totalCleaned = CleanAllSacks(all);
                break;
                case "help":
                    SendReply(netuser, "/clean DEPLOYNAME optional:all");
                    SendReply(netuser, "You must enter deployed item name that you want to clean.");
                    SendReply(netuser, "Not using the option: \"all\" will only clean items that are not on a structure.");
                    SendReply(netuser, "using the option: \"all\" will only clean all the items.");
                    
                    SendReply(netuser, "/cleanradius RADIUS optional:DEPLOYNAME optional:all");
                    SendReply(netuser, "You must enter a radius.");
                    SendReply(netuser, "You may or may not say what kind of deployements you want to be cleaned (default will be any).");
                    SendReply(netuser, "Not using the option: \"all\" will only clean items that are not on a structure.");
                    SendReply(netuser, "using the option: \"all\" will only clean all the items.");
                    return;
                    break;
                default:
                    var getvalidname = ValidDeploy(cachedName);
                    if (getvalidname == null)
                    {
                        SendReply(netuser, string.Format("{0} is not a valid deploy name.", args[0]));
                        return;
                    }
                    cachedName = (string)getvalidname;
                    totalCleaned = CleanAllDeployables(cachedName, all);
                break;
            }
            SendReply(netuser, string.Format("You've successfully cleaned {0} {1}.", totalCleaned.ToString(), args[0]));
        }
        
        bool shouldDestroy(Vector3 position, bool all)
        {
            if (all) return true;
            foreach (Collider collider in Physics.OverlapSphere(position, 4f))
            {
                if (collider.GetComponent<DeployableObject>() != null) continue;
                if (collider.gameObject.layer == 0)
                {
                    return false;
                }
            }
            return true;
        }

        int CleanRadius(Vector3 position, float radius, string name, bool all)
        {
            int destroyed = 0;
            foreach (Collider collider in Physics.OverlapSphere(position, radius))
            {
                if(name == string.Empty && collider.GetComponent<DeployableObject>())
                {
                    if (!all && collider.GetComponent<DeployableObject>()._carrier != null) continue;
                    if (shouldDestroy(collider.transform.position, all))
                    {
                        NetCull.Destroy(collider.gameObject);
                        destroyed++;
                    }
                }
                else if(collider.gameObject.name == name)
                {
                    if (shouldDestroy(collider.transform.position, all))
                    {
                        NetCull.Destroy(collider.gameObject);
                        destroyed++;
                    }
                }
            }
            return destroyed;
        }
        [ChatCommand("cleanradius")]
        void cmdChatCleanRadius(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser)) { SendReply(netuser, "You dont have access to this command."); return; }
            if (args.Length == 0)
            {
                SendReply(netuser, "You must enter a radius.");
                SendReply(netuser, "You may or may not say what kind of deployements you want to be cleaned (default will be any).");
                SendReply(netuser, "Not using the option: \"all\" will only clean items that are not on a structure.");
                SendReply(netuser, "using the option: \"all\" will only clean all the items.");
                SendReply(netuser, "/cleanradius RADIUS optional:DEPLOYNAME optional:all");
                return;
            }
            if(!float.TryParse(args[0], out cachedRadius))
            {
                SendReply(netuser, "Wrong arguments, you were supposed to write a radius first");
                return;
            }
            cachedName = "";
            int totalCleaned = 0;
            bool all = (args.Length > 1 && string.Concat(args).Contains("all")) ? true : false;
            if (args.Length == 2 && !all || args.Length > 2) cachedName = args[1].ToLower();
            switch (cachedName)
            {
                case "":
                    totalCleaned = CleanRadius(netuser.playerClient.lastKnownPosition, cachedRadius, string.Empty, all);
                    break;
                case "lootsack":
                case "bag":
                case "lootsacks":
                case "bags":
                    totalCleaned = CleanRadius(netuser.playerClient.lastKnownPosition, cachedRadius, "LootStack(Clone)", all);
                    break;
                default:
                    var getvalidname = ValidDeploy(cachedName);
                    if (getvalidname == null)
                    {
                        SendReply(netuser, string.Format("{0} is not a valid deploy name.", cachedName));
                        return;
                    }
                    cachedName = (string)getvalidname;
                    totalCleaned = CleanRadius(netuser.playerClient.lastKnownPosition, cachedRadius, cachedName, all);
                    break;
            }
            if (cachedName == "") cachedName = "Deployables";
            SendReply(netuser, string.Format("You've successfully cleaned {0} {1}.", totalCleaned.ToString(), cachedName));
        }

        void SendHelpText(NetUser netuser)
        {
            if (!hasAccess(netuser)) return;
            SendReply(netuser, "Clean Commands: /clean help");
        }
    }
}
 