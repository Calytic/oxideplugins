This plugin modifies the built-in RoK /help command to also include entries added by Oxide plugins. Keep in mind that this not instantly show up commands for plugins, plugin developers will need to implement the help messages in their plugins.

**Plugin Devs**

If you want your commands to show up when a user uses the command /help you would use the hook SendHelpText(Player player).


Example:
Code (C):
````
private void SendHelpText(Player player)
{

    PrintToChat(player, "Some help message for custom command /something");
}
````