**HideAndSeek** is work-in-progress re-make of the Prop Hunt / Hide and Seek game mode for games such as Garry's Mod, and other games. It's currently unfinished, but there's still fun to be had!


To hide as an item, you'd first need the permission that is documented below. Once you have that, you can use the chat commands or simply use the Left and Right mouse buttons. Enjoy!

**Note: **I would recommend running this standalone, with no other plugins that change game mechanics. There is still a good bit that is hard-coded, but that'll be moved to a configuration soon.

**Features**


* Players can hide as any deployable object and be interacted with.
* Props are not damaged by attackers, but damage amount is passed to the hidden player.
* Attacking non-props will hurt the attacking player, and eventually kill them.
* Corpses are automatically removed to keep things clean.


**Known Issues**


* No actual event yet, just the hiding and seeking.
* No configuration, but it doesn't really need one yet.
* No cooldown for taunts, so expect sound spam.
* No whitelist/blacklist of things not to use as props.
* Players may glitch if they try to hide as a non-deployable.
* Players sometimes get stuck as a prop if the server isn't restarted properly.
* Holding an item when disguising as a prop will make the prop have that item.

* The taunt GUI button overlaps the VoIP, but it's better anyways...


**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**hideandseek.allowed** (allows player to use /vanish and go invisible)
**Ex.** grant user Wulf hideandseek.allowed
**Ex.** revoke user Wulf hideandseek.allowed
**Ex.** grant group moderator hideandseek.allowed


**Chat Commands**


* 
**/hide**
Disguises the player as the prop they are targeting.


* 
**/unhide**
Restores the player to their normal self.


**Credits**


* 
**LaserHydra**, for helping remind me of this idea and helping test a lot.
* Other Slack goers for the oohs and awes, <3 signs, and testing.