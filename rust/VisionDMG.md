VisionDMG control defines who can damage what and where.

**Current Features**


* Control Damage for Players Vs Entities
* Control Damage for Players Vs Players


**Upcoming**


* Enable/Disabled based on PVP/PVE mode
* Plugin Integration (for VisionPVP)
* AuthLevel Permissions
* Console Commands
* API
* Configuration Upgrading


**Players Vs. Entities

Damage On**

Toggle all player damage on or off. This applies to everything including animals. Damage can still be done to the player from other sources such as starvation or animal attacks. Players cannot hurt the animals back if this is off.

````
{

"damage_on": true

}
````


**Damage All Buildings**

Toggles damage from players to any building. If this setting is disabled and the Damage Own Buildings setting is enabled, then damage cannot be done to buildings where building privileges do not exist. The Damage Locked Items setting overrides this.

````
{

"players_can_damage_all_buildings": true

}
````


**Damage Own Buildings**

Toggles damage from players to own buildings. This is determined by building privileges so it does not apply if there is no tool cupboard. This setting when enabled, overrides the Damage All Buildings setting and allows players to damage only their own buildings. The Damage Locked Items setting overrides this.

````
{

"players_can_damage_own_buildings": true

}
````


**Damage Loot Containers (Storage Containers)**

Toggles damage from players to loot containers. When disabled, it prevents loot containers from being damaged. It does not matter if you have building privileges or not, they are in GOD mode. This overrides the Damage Locked Items setting.

````
{

"players_can_damage_loot_containers": true

}
````


**Damage Locked Items**

Toggles damage for locked items. Applies to anything that can have a lock or code-lock put on it. If this is disabled then anything locked cannot be destroyed such as doors or storage containers. If you can unlock the lock, then damage can be applied. Again, building privileges do not apply here.

````
{

"players_can_damage_locked_items": true

}
````


**Players Vs. Players**

Toggle damage for Player Vs Player attacks in different situations. Setting any of these settings will allow damage to the victim for that circumstance.

````
{

"contested": true,

"private": false,

"trespassing": true,

"friendly": true

}
````

Allow damage to victim?

**Contested PVP**

This is an area where no building privileges exist one way or another. This applies to a majority of the map.

**Private PVP**

Any area of the map where the attacker does not have building privileges, but the victim does.

**Trespassing PVP**

Any area of the map where building is blocked for both the attacker and the victim. Effectively on someone else's property.

**Friendly PVP**

Both the attacker and the victim have building privileges.