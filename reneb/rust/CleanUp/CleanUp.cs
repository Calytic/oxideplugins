using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("CleanUp", "Reneb & SPooCK", "2.0.2")]
    public class CleanUp : RustPlugin
    {
        private int constructionColl;

        void Loaded()
        {
            if (!permission.PermissionExists("canclean")) permission.RegisterPermission("canclean", this);
            constructionColl = LayerMask.GetMask(new string[] { "Construction" });
        }
        void OnServerInitialized()
        {
            InitializeTable();
        }

        Dictionary<string, string> displaynameToShortname = new Dictionary<string, string>();

        private void InitializeTable()
        {
            displaynameToShortname.Clear();
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions() as List<ItemDefinition>;
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                 if(itemdef.GetComponent< ItemModDeployable>() != null)
                      displaynameToShortname.Add(itemdef.displayName.english.ToString().ToLower(), itemdef.shortname.ToString());
            }
        }
        bool shouldRemove(Deployable deployable, bool forceRemove, float eraseRadius = 0.5f )
        {
            if (forceRemove) return true;
            foreach( Collider collider in UnityEngine.Physics.OverlapSphere(deployable.transform.position, eraseRadius, constructionColl) )
            {
                return false;
            }
            return true;
        }

        bool hasAccess(BasePlayer player)
        {
            if (player == null) return false;
            if (player.net.connection.authLevel > 0) return true;
            return permission.UserHasPermission(player.userID.ToString(), "canclean");
        }
		
		[ConsoleCommand("cc.clean")]
        void cmdConsoleClean(ConsoleSystem.Arg arg)
        {
            if (arg.Player() && !arg.Player().IsAdmin()) { SendReply(arg, "You need to be admin to use that command"); return; }
			if (arg.Args == null || arg.Args.Length < 2) { SendReply(arg, "cc.clean \"Deployable Item Name\" all => all the deployable items"); SendReply(arg, "cc.clean \"Deployable Item Name\" world optional:XX => all the items that are not connected to a construction in XX radius (default is 3 meters)"); return; }
            if (arg.Args[1] != "world" && arg.Args[1] != "all") { SendReply(arg, "cc.clean \"Deployable Item Name\" all => all the deployable items"); SendReply(arg, "cc.clean \"Deployable Item Name\" world optional:XX => all the items that are not connected to a construction in XX radius (default is 3 meters)"); return; }
            switch (arg.Args[0].ToLower())
            {
                default:
                    string shortname = arg.Args[0].ToLower();
                    if (displaynameToShortname.ContainsKey(shortname))
                        shortname = displaynameToShortname[shortname];
                    else if (!displaynameToShortname.ContainsValue(shortname))
                    {
                        SendReply(arg, string.Format("{0} is not a valid item name", arg.Args[0]));
                        return;
                    }
                    Item newItem = ItemManager.CreateByName(shortname, 1);
                    if (newItem == null)
                    {
                        SendReply(arg, "Couldn't find this item, this shouldnt show ever ...");
                        return;
                    }
                    if (newItem.info.GetComponent<ItemModDeployable>() == null)
                    {
                        SendReply(arg, "This is not a item mod deployable item, this shouldnt show ever ...");
                        return;
                    }
                    Deployable deployable = newItem.info.GetComponent<ItemModDeployable>().entityPrefab.Get().GetComponent<Deployable>();
                    if (deployable == null)
                    {
                        SendReply(arg, "This is not a deployable item, this shouldnt show ever ...");
                        return;
                    }
                    string deployablename = deployable.gameObject.name + ".prefab";
                    bool shouldForce = (arg.Args[1] == "all") ? true : false;
                    float eraseRadius = 0.5f;
                    if (arg.Args.Length > 2) float.TryParse(arg.Args[2], out eraseRadius);
                    int cleared = 0;
                    int total = 0;
                    foreach (Deployable deployed in UnityEngine.Resources.FindObjectsOfTypeAll<Deployable>())
                    {
                        var realEntity = deployed.GetComponent<BaseNetworkable>().net;
                        if (realEntity == null) continue;
                        if(deployed.gameObject.name.EndsWith(deployablename))
                        {
                            total++;
                            if (shouldRemove(deployed, shouldForce, eraseRadius))
                            {
                                deployed.GetComponent<BaseEntity>().KillMessage();
                                cleared++;
                            }
                        }
                    }
                    SendReply(arg, string.Format("Cleared {0} entities out of {1} found", cleared.ToString(), total.ToString()));
                    break;
            }			
		}
		[ConsoleCommand("cc.count")]
        void cmdConsoleCount(ConsoleSystem.Arg arg)
        {
            if (arg.Player() && !arg.Player().IsAdmin()) { SendReply(arg, "You need to be admin to use that command"); return; }
			if (arg.Args == null || arg.Args.Length < 2) { SendReply(arg, "cc.count \"Deployable Item Name\" all => all the deployable items"); SendReply(arg, "cc.count \"Deployable Item Name\" world optional:XX => all the items that are not connected to a construction in XX radius (default is 3 meters)"); return; }
            if (arg.Args[1] != "world" && arg.Args[1] != "all") { SendReply(arg, "cc.count \"Deployable Item Name\" all => all the deployable items"); SendReply(arg, "cc.count \"Deployable Item Name\" world optional:XX => all the items that are not connected to a construction in XX radius (default is 3 meters)"); return; }
            switch (arg.Args[0].ToLower())
            {
                default:
                    string shortname = arg.Args[0].ToLower();
                    if (displaynameToShortname.ContainsKey(shortname))
                        shortname = displaynameToShortname[shortname];
                    else if (!displaynameToShortname.ContainsValue(shortname))
                    {
                        SendReply(arg, string.Format("{0} is not a valid item name", arg.Args[0]));
                        return;
                    }
                    Item newItem = ItemManager.CreateByName(shortname, 1);
                    if (newItem == null)
                    {
                        SendReply(arg, "Couldn't find this item, this shouldnt show ever ...");
                        return;
                    }
                    if (newItem.info.GetComponent<ItemModDeployable>() == null)
                    {
                        SendReply(arg, "This is not a item mod deployable item, this shouldnt show ever ...");
                        return;
                    }
                    Deployable deployable = newItem.info.GetComponent<ItemModDeployable>().entityPrefab.Get().GetComponent<Deployable>();
                    if (deployable == null)
                    {
                        SendReply(arg, "This is not a deployable item, this shouldnt show ever ...");
                        return;
                    }
                    string deployablename = deployable.gameObject.name + ".prefab";
                    bool shouldForce = (arg.Args[1] == "all") ? true : false;
                    float eraseRadius = 0.5f;
                    if (arg.Args.Length > 2) float.TryParse(arg.Args[2], out eraseRadius);
                    int cleared = 0;
                    foreach (Deployable deployed in UnityEngine.Resources.FindObjectsOfTypeAll<Deployable>())
                    {
                        var realEntity = deployed.GetComponent<BaseNetworkable>().net;
                        if (realEntity == null) continue;
                        if (deployed.gameObject.name.EndsWith(deployablename))
                        {
                            if (shouldRemove(deployed, shouldForce, eraseRadius))
                            {
                                cleared++;
                            }
                        }
                    }
                    SendReply(arg, string.Format("{1}: Found {0} entities that matchs your search", cleared.ToString(), shortname));
                    break;
            }
		}
        [ChatCommand("clean")]
        void cmdChatClean(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, "You dont have access to this command"); return; }
            if (args.Length < 2) { SendReply(player, "/clean \"Deployable Item Name\" all => all the deployable items"); SendReply(player, "/clean \"Deployable Item Name\" world optional:XX => all the items that are not connected to a construction in XX radius (default is 3 meters)"); return; }
            if (args[1] != "world" && args[1] != "all") { SendReply(player, "/clean \"Deployable Item Name\" all => all the deployable items"); SendReply(player, "/clean \"Deployable Item Name\" world optional:XX => all the items that are not connected to a construction in XX radius (default is 3 meters)"); return; }
            switch (args[0].ToLower())
            {
                default:
                    string shortname = args[0].ToLower();
                    if (displaynameToShortname.ContainsKey(shortname))
                        shortname = displaynameToShortname[shortname];
                    else if (!displaynameToShortname.ContainsValue(shortname))
                    {
                        SendReply(player, string.Format("{0} is not a valid item name", args[0]));
                        return;
                    }
                    Item newItem = ItemManager.CreateByName(shortname, 1);
                    if (newItem == null)
                    {
                        SendReply(player, "Couldn't find this item, this shouldnt show ever ...");
                        return;
                    }
                    if (newItem.info.GetComponent<ItemModDeployable>() == null)
                    {
                        SendReply(player, "This is not a item mod deployable item, this shouldnt show ever ...");
                        return;
                    }
                    Deployable deployable = newItem.info.GetComponent<ItemModDeployable>().entityPrefab.Get().GetComponent<Deployable>();
                    if (deployable == null)
                    {
                        SendReply(player, "This is not a deployable item, this shouldnt show ever ...");
                        return;
                    }
                    string deployablename = deployable.gameObject.name + ".prefab";
                    bool shouldForce = (args[1] == "all") ? true : false;
                    float eraseRadius = 0.5f;
                    if (args.Length > 2) float.TryParse(args[2], out eraseRadius);
                    int cleared = 0;
                    int total = 0;
                    foreach (Deployable deployed in UnityEngine.Resources.FindObjectsOfTypeAll<Deployable>())
                    {
                        var realEntity = deployed.GetComponent<BaseNetworkable>().net;
                        if (realEntity == null) continue;
                        if(deployed.gameObject.name.EndsWith(deployablename))
                        {
                            total++;
                            if (shouldRemove(deployed, shouldForce, eraseRadius))
                            {
                                deployed.GetComponent<BaseEntity>().KillMessage();
                                cleared++;
                            }
                        }
                    }
                    SendReply(player, string.Format("Cleared {0} entities out of {1} found", cleared.ToString(), total.ToString()));
                    break;
            }
        }
        [ChatCommand("count")]
        void cmdChatCount(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, "You dont have access to this command"); return; }
            if (args.Length < 2) { SendReply(player, "/count \"Deployable Item Name\" all => all the deployable items"); SendReply(player, "/count \"Deployable Item Name\" world optional:XX => all the items that are not connected to a construction in XX radius (default is 3 meters)"); return; }
            if (args[1] != "world" && args[1] != "all") { SendReply(player, "/count \"Deployable Item Name\" all => all the deployable items"); SendReply(player, "/count \"Deployable Item Name\" world optional:XX => all the items that are not connected to a construction in XX radius (default is 3 meters)"); return; }
            switch (args[0].ToLower())
            {
                default:
                    string shortname = args[0].ToLower();
                    if (displaynameToShortname.ContainsKey(shortname))
                        shortname = displaynameToShortname[shortname];
                    else if (!displaynameToShortname.ContainsValue(shortname))
                    {
                        SendReply(player, string.Format("{0} is not a valid item name", args[0]));
                        return;
                    }
                    Item newItem = ItemManager.CreateByName(shortname, 1);
                    if (newItem == null)
                    {
                        SendReply(player, "Couldn't find this item, this shouldnt show ever ...");
                        return;
                    }
                    if (newItem.info.GetComponent<ItemModDeployable>() == null)
                    {
                        SendReply(player, "This is not a item mod deployable item, this shouldnt show ever ...");
                        return;
                    }
                    Deployable deployable = newItem.info.GetComponent<ItemModDeployable>().entityPrefab.Get().GetComponent<Deployable>();					
                    if (deployable == null)
                    {
                        SendReply(player, "This is not a deployable item, this shouldnt show ever ...");
                        return;
                    }
                    string deployablename = deployable.gameObject.name + ".prefab";					
                    bool shouldForce = (args[1] == "all") ? true : false;
                    float eraseRadius = 0.5f;
                    if (args.Length > 2) float.TryParse(args[2], out eraseRadius);
                    int cleared = 0;
                    foreach (Deployable deployed in UnityEngine.Resources.FindObjectsOfTypeAll<Deployable>())
                    {
                        var realEntity = deployed.GetComponent<BaseNetworkable>().net;
                        if (realEntity == null) continue;
                        if (deployed.gameObject.name.EndsWith(deployablename))
                        {
                            if (shouldRemove(deployed, shouldForce, eraseRadius))
                            {
                                cleared++;
                            }
                        }
                    }
                    SendReply(player, string.Format("{1}: Found {0} entities that matchs your search", cleared.ToString(), shortname));
                    break;
            }
        }
    }
}
