Ths plugin simply adds XP to the game and levels for each amount of XP.

Currently the plugin just adds bonus damage for each level of a player.


All options like the amount of bonus damage and the needed XP per level can be configured in the config file and the lang config file.


For those who want I have edited the existing [Kits for Reign of Kings](http://oxidemod.org/plugins/kits.1025/) by Mughisi to have the level as a parameter. Pm me about it if you want it.

If you want to let a plugin work with the level system please let me know and I'll help as much as possible.

**Commands:**
/xp - Shows your current amount of xp, your current level and the amount of xp you need to reach the next level.
/levellist - Shows from all online players their current level.
/topplayers - Shows a numerical list of players ordered on their current level starting with the player with the highest level.
**Admin commands:**
/givexp (amount) (optional: target player) - Gives the amount of xp  (optional: to the target player).
/removexp (amount) (optional: target player) - Removes the amount of xp  (optional: to the target player).
/clearxp - Removes all xp data.
/pvpxp - Toggle if players can get xp from pvp.
/pvexp - Toggle if players can get xp from pve.

**Permissions:**
level.toggle - Permission to use the two toggle commands.
level.modify - Permission to use the givexp, removexp and clearxp commands.

**Configuration:** (plugin will give a random number between each min and max)
monsterKillMinXp - The minimal amount of xp a player can get for killing a dangerous animal (wolf, werewolf, bear, villager).
monsterKillMaxXp - The maximal amount of xp a player can get for killing a dangerous animal (wolf, werewolf, bear, villager).
animalKillMinXp - The minimal amount of xp a player can get for killing a normal animal (rabbit, sheep, etc.).
animalKillMaxXp - The maximal amount of xp a player can get for killing a normal animal (rabbit, sheep, etc.).
pvpGetMinXp - The minimal amount of xp a player can get for killing another player.
pvpGetMaxXp - The maximal amount of xp a player can get for killing another player.
pvpLoseMinXp - The minimal amount of xp a player will lose when killed by another player.
pvpLoseMaxXp - The maximal amount of xp a player will lose when killed by another player.
pvpXpLoss - The amount in percent the gained player kill xp will reduce for each level the killed player is below the killers level.
playerDamageBonusPercentage - The bonus damage against other players in percent the player will get for each level he is.
monsterDamageBonusPercentage - The bonus damage against all animals (dangerous and normal) in percent the player will get for each level he is.
siegeDamageBonusPercentage - The bonus damage with siege weapons (ballista and trebuchet) the player will get for each level he is.
blockDamageBonusPercentage - The bonus damage against blocks (with normal weapon) in percent the player will get for each level he is.
maxTopPlayersList - The number of players shown in the top players list.
xpNeededPerLevel - The list of xp needed for the next level (start with zero to give players level 1 from the start).


I'll keep working on this system to improve it.

If you find any bugs or if you have a good idea for the system let me know and I'll see what I can do.