This plugin allow to automatically update building parts to any grade just after you build it (using building plan). By default works only for admins, but you can change that via oxide permissions.

**Command usage:**

/bgrade - help in-game, with display current mode.

/bgrade 1 - auto update to wood

/bgrade 2 - auto update to stone

/bgrade 3 - auto update to metal

/bgrade 4 - auto update to armored

/bgrade 0 - disable auto update

**Oxide permissions:**

You can change who will access command and what grades they will can use via oxide permissions.


For example if you need allow auto update for wood&stone with consume resources for players, you need type in server console:

**oxide.grant group player autogrades.1

oxide.grant group player **autogrades.2****


If you want to remove no-resource consumption for admins, then type:
**oxide.revoke group admin **autogrades.nores****


Also if you are admin, but still don't have permissions to plugin, then try add youself to admin group using:
**oxide.usergroup add username admin


Available permissions:**

autogrades.all - access to all grades (by default for admins)

autogrades.1 - access to wood update

autogrades.2 - access to stone update

autogrades.3 - access to metal update

autogrades.4 - access to armored update

autogrades.nores - disable resouces consumption for upgrade (by default for admins) **note** - you still need wood for create twig building part

**Available config options:**

Block Construct and Refund - block construct and refund if not enough resources for upgrade to needed grade (by default enabled).

Command - chat command name, you can change it if you want

**Available plugin hooks (for developers):**

CanAutoGrade - allow block auto upgrade if needed. Must return int or nothing.
**Arguments:** BasePlayer player, int grade, BuildingBlock buildingBlock, Planner planner
**Possible return values:**

Return -1 - Block upgrade, but create twig part

Return 0 - Obey plugin settings (block on construct if enabled or not)

Return 1 - Block upgrade and block build

Return nothing - allow upgrade


Also this plugin has multi-language support so you can translate or customize messages in **oxide/lang/AutoGrades.en.json** file.

By default plugin comes with English and Russian language.


ps this is my first public released rust plugin, and sorry for my english.