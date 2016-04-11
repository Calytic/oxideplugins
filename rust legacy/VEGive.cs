using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;

/* BROUGHT TO YOU BY        
,.   ,.         .  .        `.---     .              . 
`|  / ,-. ,-. . |  |  ,-.    |__  . , |- ,-. ,-. ,-. |-
 | /  ,-| | | | |  |  ,-|   ,|     X  |  |   ,-| |   | 
 `'   `-^ ' ' ' `' `' `-^   `^--- ' ` `' '   `-^ `-' `'
 ~PrincessRadPants and Swuave
*/
namespace Oxide.Plugins
{


    [Info("VEGive", "PrincessRadPants and Swuave", "1.0.3")]
    public class VEGive : RustLegacyPlugin
    {
        [PluginReference]
        Plugin Kits;

        void Loaded()
        {
            if (!permission.PermissionExists("cangive")) permission.RegisterPermission("cangive", this);
            if (!permission.PermissionExists("all")) permission.RegisterPermission("all", this);
        }



        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin()) { return true; }
            else if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "cangive"))
            {
                return true;
            }
            else if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "all"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [ChatCommand("give")]
        void cmdGive(NetUser netuser, string command, string[] args)
        {
            int? notReceived = null;
            string item = String.Empty;
            int amount = 1;
            NetUser targetuser = null;
            int argsnum = args.Length;

            //check if user has permision
            if (!hasAccess(netuser))
            {
                SendReply(netuser, "You do not have permission to use this command"); return;
            }

            //check for args
            if ((args == null) || (argsnum < 1))
            {
                SendReply(netuser, "Syntax: /give [optional:Playername] <item name> <amount>"); return;
            }

            switch (argsnum)
            {
                case (1): //single item to self
                    item = args[0];
                    ItemDataBlock datablock = (GetItemDataBlock(item));
                    if (datablock != null)
                    {
                        targetuser = netuser;
                        notReceived = Give(targetuser, datablock, amount);
                    }
                    else
                    {
                        Rust.Notice.Popup(netuser.networkPlayer, "â", "Item not found."); return;
                    }
                    break;

                case (2): // either (user, item) or (item, amount)

                    //check to see if first arg is a player
                    if ((rust.FindPlayer(args[0]) != null))
                    {
                        //check to see if second arg is an item
                        if ((GetItemDataBlock(args[1])) != null)
                        {
                            item = (args[1]);
                            targetuser = rust.FindPlayer(args[0]);
                            datablock = (GetItemDataBlock(item));
                            notReceived = Give(targetuser, datablock, amount);
                        }

                       //second arg is not an item so check to see if the first arg is an item
                        else if ((GetItemDataBlock(args[0])) != null)
                        {
                            targetuser = netuser;
                            item = (args[0]);
                            datablock = (GetItemDataBlock(item));
                            amount = Convert.ToInt32(args[1]);
                            notReceived = Give(targetuser, datablock, amount);
                        }

                        //something is wrong
                        else
                        {
                            SendReply(netuser, "Syntax: /give [optional:Playername] <item name> <amount>");
                        }
                    }
                    //first arg is not a player so check if it is an item
                    else if ((GetItemDataBlock(args[0])) != null)
                    {
                        targetuser = netuser;
                        item = args[0];
                        datablock = (GetItemDataBlock(item));
                        amount = Convert.ToInt32(args[1]);
                        notReceived = Give(targetuser, datablock, amount);
                    }
                    //something is wrong
                    else
                    {
                        SendReply(netuser, "Syntax: /give [optional:Playername] <item name> <amount>");
                    }
                    break;

                case (3):  //(user, item, amount)

                    targetuser = rust.FindPlayer(args[0]);
                    if (targetuser != null)
                    {
                        item = args[1];
                        datablock = (GetItemDataBlock(item));
                        if (datablock != null)
                        {
                            amount = Convert.ToInt32(args[2]);
                            notReceived = Give(targetuser, datablock, amount);
                        }
                        else
                        {
                            SendReply(netuser, "Item datablock not found");
                        }
                    }
                    else
                    {
                        SendReply(netuser, "Player not found");
                    }
                    break;
            }

            GiveCheck(netuser, targetuser, item, amount, notReceived);



        }

        [ChatCommand("GiveKit")]
        void cmdGiveKit(NetUser netuser, string command, string[] args)
        {
            int argsnum = args.Length;
            NetUser targetuser = null;
            string kitname = String.Empty;


            //check if user has permision
            if (!hasAccess(netuser))
            {
                SendReply(netuser, "You do not have permission to use this command"); return;
            }

            //check for args
            if ((args == null) || (args.Length < 1) || (argsnum > 2))
            {
                SendReply(netuser, "Syntax: /givekit [optional:Playername] <kit name>"); return;
            }

            if (argsnum == 1)
            {
                kitname = args[0];
                targetuser = netuser;
            }
            else
            {
                targetuser = rust.FindPlayer(args[0]);
                if (targetuser != null)
                {
                    kitname = args[1];
                }
                else
                {
                    SendReply(netuser, "Player not found");
                }
            }
            Kits.Call("GiveKit", targetuser, kitname);
        }

        [ChatCommand("GiveAll")]
        void cmdGiveAll(NetUser netuser, string command, string[] args)
        {
            int amount = 1;
            int argsnum = args.Length;
            int? notReceived = null;
            string item = args[0];

            //check if user has permision
            if (!hasAccess(netuser))
            {
                SendReply(netuser, "You do not have permission to use this command"); return;
            }

            //check for args
            if ((args == null) || (argsnum < 1) || (argsnum > 2))
            {
                SendReply(netuser, "Syntax: /giveall <item name> <amount>"); return;
            }
            if (argsnum == 2)
            {

                if (item == "kit")
                {
                    string kitname = args[1];
                    foreach (PlayerClient player in PlayerClient.All)
                    {
                        Kits.Call("GiveKit", player.netUser, kitname);
                        return;
                    }
                }
                else { amount = Convert.ToInt32(args[1]); }

            }


            ItemDataBlock datablock = (GetItemDataBlock(item));

            if (datablock != null)
            {
                foreach (PlayerClient player in PlayerClient.All)
                {
                    NetUser targetuser = player.netUser;
                    notReceived = Give(targetuser, datablock, amount);
                    GiveCheck(netuser, targetuser, item, amount, notReceived);
                }

            }


        }

        ItemDataBlock GetItemDataBlock(string item)
        {
            ItemDataBlock datablock = DatablockDictionary.GetByName(item);
            return datablock;
        }


        int Give(NetUser targetuser, ItemDataBlock datablock, int num)
        {
            Inventory inv = targetuser.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            return inv.AddItemAmount(datablock, num);
        }

        void GiveCheck(NetUser netuser, NetUser targetuser, string item, int amount, int? notReceived)
        {
            int amountGiven = (amount - (int)notReceived);
            if (notReceived != 0) // Some inventory was not received
            {

                //send reply to netuser
                Rust.Notice.Popup(netuser.networkPlayer, "â", "Only " + amountGiven.ToString() + " of item " + item + " given.");

                //send inv notice to targetuser
                Rust.Notice.Inventory(targetuser.networkPlayer, "+" + amountGiven.ToString() + " " + item);
            }
            else // Everything went as planned.
            {

                //send reply to netuser
                Rust.Notice.Popup(netuser.networkPlayer, "â", amountGiven.ToString() + " of item " + item + " given successfully!");

                //send inv notice to targetuser
                Rust.Notice.Inventory(targetuser.networkPlayer, "+" + amountGiven.ToString() + " " + item);
            }
        }

    }
}