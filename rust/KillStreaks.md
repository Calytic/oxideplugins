Keep track of player kill streaks, broadcast custom killstreak messages and have the option to reward or punish them with multiple perk types. With UI notifications and a leaderboard


Support for FriendsAPI and Clans, to stop friends killing each other

Support for EventManager, to stop player kill streaks whilst in the arena

**Streaks currently available** - I am looking for more ideas, feel free to leave any suggestions in the support thread

````
0 - None

---- Punishments ----

1 - Airstrike // Requires the Airstrike plugin

2 - Squadstrike // Requires the Airstrike plugin

3 - Artillery // Drops a artillery strike on the player

4 - Helicopter // Sends a helicopter to the player

--- Rewards ----

5 - Supplydrop // Drops a supply drop to the player

6 - Airstrike signal // Give the player a supply signal to call the perk

7 - Squadstrike signal // ^^

8 - Artillery signal // ^^

9 - Helicopter signal // ^^

10 - Martyrdom // When the player dies the will drop either a grenade, beancan grenade or explosive charge as a final stand

11 - TurretDrop // Give the player a supply signal that spawns a loaded Auto Turret
````


**Chat Commands**
/ks top <opt:##> - Display top kill streaks
/ks pb - Displays the users personal best
/ks list - List all currently setup streaks
/ks remove <killnumber> - Remove the streak for killnumber
/ks wipe - Wipes all killstreak data
/ks show - Shows available streak types and their corresponding ID number
/ks add - Displays the format required for adding a new kill streak


--- Perk Signal activation commands ---
/ks strike - Activates the airstrike signal
/ks squad - Activates the squadstrike signal
/ks art - Activates a artillery signal
/ks heli - Activates the helicopter signal
/ks turret - Activates the TurretDrop signal

**Adding a new KillStreak**

Having a perk/punishment is optional, you can just display messages if thats what you wish

/ks add <killnumber> <message> <opt:type> <opt:amount>

- killnumber is the amount of kills needed to activate the streak

- message is the message that will displayed upon streak activation

- opt:type is the streak type (see above)

- opt:amount is the amount of time that streak type will be called


ex. /ks add 10 " is on a rampage!" 3 2 ===> This will activate at 10 kills, display "k1lly0u is on a rampage!" and will call a artillery strike on my position 2 times

** NOTE: The players name will be displayed before the message!

**Perk types and activation instructions**

When a player receives a perk, if it requires activation the player will be given instructions how to do so.
**Signals** - Players must activate the perk with a chat command. Once activated all they have to do it throw the supplied supply signal to activate the perk

-- Types


* Airstrike - Calls a airstrike on the signals position
* Squadstrike - Calls a squadstrike on the signals position
* Artillery - Calls a artillery strike on the signals position
* Helicopter - Calls a helicopter to the signals position
* Turret Drop - Spawns a loaded auto turret on the signals position. This will automatically authorize to the player and any of his nearby friends/clan mates. The turret spawns active

**Martyrdom** - Martyrdom is activated automatically, upon death the player will drop a random explosive as a final stand against the attacker

**Supply Drops** - Supply drops are activated instantly and fall to the ground rapidly for fast paced looting

**Config**

````

{

  "Artillery - Rocket amount": 20,

  "Artillery - Rocket interval": 0.5,

  "Artillery - Rocket spread": 6.0,

  "Helicopter - Accuracy": 6.0,

  "Helicopter - Bullet damage": 3.0,

  "Helicopter - Health": 4000.0,

  "Helicopter - Mail rotor health": 400.0,

  "Helicopter - Spawn distance (away from player)": 500.0,

  "Helicopter - Speed": 30.0,

  "Helicopter - Tail rotor health": 250.0,

  "Messages - Broadcast streak message": true,

  "Messages - Main color": "<color=orange>",

  "Messages - Message color": "<color=#939393>",

  "Options - Data save timer": 10,

  "Options - Use Clans": true,

  "Options - Use FriendsAPI": true


}

 
````