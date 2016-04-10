using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Stack Size Controller", "Waizujin", 1.9, ResourceId = 1185)]
    [Description("Allows you to set the max stack size of every item.")]
    public class StackSizeController : RustPlugin
    {
		protected override void LoadDefaultConfig()
        {
			PrintWarning("Creating a new configuration file.");

			var gameObjectArray = FileSystem.LoadAll<GameObject>("Assets/", ".item");
			var itemList = gameObjectArray.Select(x => x.GetComponent<ItemDefinition>()).Where(x => x != null).ToList();

			foreach (var item in itemList)
			{
				if (item.condition.enabled && item.condition.max > 0) { continue; }

				Config[item.displayName.english] = item.stackable;
			}
		}

        void OnServerInitialized()
        {
            permission.RegisterPermission("stacksizecontroller.canChangeStackSize", this);

			var dirty = false;
			var itemList = ItemManager.itemList;

			foreach (var item in itemList)
			{
				if (item.condition.enabled && item.condition.max > 0) { continue; }

				if (Config[item.displayName.english] == null)
				{
					Config[item.displayName.english] = item.stackable;
					dirty = true;
				}

				item.stackable = (int)Config[item.displayName.english];
			}

			if (dirty == false) { return; }

			PrintWarning("Updating configuration file with new values.");
			SaveConfig();
		}

        [ChatCommand("stack")]
        private void StackCommand(BasePlayer player, string command, string[] args)
        {
            int stackAmount = 0;

            if (!hasPermission(player, "stacksizecontroller.canChangeStackSize"))
			{
				SendReply(player, "You don't have permission to use this command.");

				return;
			}

			if (args.Length <= 1)
			{
                SendReply(player, "Syntax Error: Requires 2 arguments. Syntax Example: /stack ammo_rocket_hv 64 (Use shortname)");

				return;
			}

            if (int.TryParse(args[1], out stackAmount) == false)
            {
                SendReply(player, "Syntax Error: Stack Amount is not a number. Syntax Example: /stack ammo_rocket_hv 64 (Use shortname)");

                return;
            }

            List<ItemDefinition> items = ItemManager.itemList.FindAll(x => x.shortname.Equals(args[0]));

            if (items[0].shortname == args[0])
            {
                if (items[0].condition.enabled && items[0].condition.max > 0) { return; }

                Config[items[0].displayName.english] = Convert.ToInt32(stackAmount);
                items[0].stackable = Convert.ToInt32(stackAmount);

                SaveConfig();

                SendReply(player, "Updated Stack Size for " + items[0].displayName.english + " (" + items[0].shortname + ") to " + stackAmount + ".");
            }
            else
            {
                SendReply(player, "That is an incorrect item name. Please use a valid shortname.");
            }
        }

        [ChatCommand("stackall")]
        private void StackAllCommand(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, "stacksizecontroller.canChangeStackSize"))
			{
				SendReply(player, "You don't have permission to use this command.");

				return;
			}

			if (args.Length == 0)
			{
                SendReply(player, "Syntax Error: Requires 1 argument. Syntax Example: /stackall 65000");

				return;
			}

            var itemList = ItemManager.itemList;

			foreach (var item in itemList)
			{
                if (item.displayName.english.ToString() == "Salt Water" ||
                item.displayName.english.ToString() == "Water") { continue; }

				Config[item.displayName.english] = Convert.ToInt32(args[0]);
				item.stackable = Convert.ToInt32(args[0]);
			}

            SaveConfig();

            SendReply(player, "The Stack Size of all stackable items has been set to " + args[0]);
        }

        [ConsoleCommand("stack")]
        private void StackConsoleCommand(ConsoleSystem.Arg arg)
        {
            int stackAmount = 0;

            if(arg.isAdmin != true) { return; }

            if (arg.Args.Length <= 1)
            {
                Puts("Syntax Error: Requires 2 arguments. Syntax Example: stack ammo_rocket_hv 64 (Use shortname)");

                return;
            }

            if (int.TryParse(arg.Args[1], out stackAmount) == false)
            {
                Puts("Syntax Error: Stack Amount is not a number. Syntax Example: stack ammo_rocket_hv 64 (Use shortname)");

                return;
            }

            List<ItemDefinition> items = ItemManager.itemList.FindAll(x => x.shortname.Equals(arg.Args[0]));

            if (items[0].shortname == arg.Args[0])
            {
                if (items[0].condition.enabled && items[0].condition.max > 0) { return; }

                Config[items[0].displayName.english] = Convert.ToInt32(stackAmount);
                items[0].stackable = Convert.ToInt32(stackAmount);

                SaveConfig();

                Puts("Updated Stack Size for " + items[0].displayName.english + " (" + items[0].shortname + ") to " + stackAmount + ".");
            }
            else
            {
                Puts("That is an incorrect item name. Please use a valid shortname.");
            }
        }

        [ConsoleCommand("stackall")]
        private void StackAllConsoleCommand(ConsoleSystem.Arg arg)
        {
            if(arg.isAdmin != true) { return; }

            if (arg.Args.Length == 0)
			{
                Puts("Syntax Error: Requires 1 argument. Syntax Example: stackall 65000");

				return;
			}

            var itemList = ItemManager.itemList;

			foreach (var item in itemList)
			{
                if (item.displayName.english.ToString() == "Salt Water" ||
                item.displayName.english.ToString() == "Water") { continue; }

				Config[item.displayName.english] = Convert.ToInt32(arg.Args[0]);
				item.stackable = Convert.ToInt32(arg.Args[0]);
			}

            SaveConfig();

            Puts("The Stack Size of all stackable items has been set to " + arg.Args[0]);
        }

        bool hasPermission(BasePlayer player, string perm)
        {
            if (player.net.connection.authLevel > 1)
            {
                return true;
            }

            return permission.UserHasPermission(player.userID.ToString(), perm);
        }
    }
}
