**Foreword:**

This plugin was requested, conceptualized and supported financially by @**[HBros]Moe**. He asked of me to publish it here under my name, even though he owns this code and it is thus a gift from him to the community.


Moe, thanks for your contribution!

**Overview: **


This plugin allows admins to setup trade boxes to automate player-to-server trades based on configurable price-reward lists. The plugin also allows players to setup recycling boxes.

**Commands:**

**/moe** displays available commands and help
**/mlists **lists all existing tradelists (admin only)
**/mshow **xxx displays the contents of the xxxx list (admin only)
**/mlink xxxx **links tradelist xxxx to the next box the player opens (admin only)
**/mulink **converts the next opened box to a normal box
**/mrec **converts the next opened box to a recycling box (must have price)

**Config:**


This plugin uses a different method to create/load it's config file, consequently the config is created in the data folder as opposed to the standard config folder. Load the plugin a first time to create the default config, edit it and then use oxide.reload MoegicBox to activate your configuration. Sorry to multiplay/clanforge hosted admins, sucks to be you.

**Other:**


I recommend using ZonesManager or some other plugin to protect the whole area around trade boxes if that matters to your situation.


Also note that there currently is no ownership check on the /unlinkbox command, allowing any player to convert a recycle box back to a normal box even if he did not setup the box or have building privs.

**Important notes on recycling:**


There is an unintended side effect that I purposely left that allows players to recycle blueprints. For some reason, this results in giving the mats of the actual item to the player (put an AK bp in the box, AK mats appear in your inventory). Didn't plan this, but hey, OK I guess 
The recycler uses a % to give back mats and rounds up the amounts. In some cases, this may result in more than the actual % being given back. Recycling a medkit for example will give you back at least one syringe even if you set your % very low.