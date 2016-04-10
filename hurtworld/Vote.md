**Permission:**

Vote.admin

**How to Add Permission:**

/grant user UserName vote.admin

or

/grant group admin vote.admin (If you have an admin group)

**Commands:**

/vote (what you want to vote for).

**How to vote:**

Simply type "yes" or "no" in the chat. It will count towards the vote if there's one active. You can only vote once.

You can change the yes/no to whatever you want by changing the config file "VoteAgainst" and "VoteFor" settings.

**Configuration File:**

````

{

  "AutoResult": true,

  "Daytime": 0.4,

  "Nighttime": 0.9,

  "SecondsChangeLootModeBack": 3600.0,

  "VoteAgainst": "No",

  "VoteFor": "Yes",

  "VoteTimeSeconds": 30

}

 
````


**

AutoResult Voting:**

Auto Result votes are votes that you start for kicking, banning, or changing the game time to night/day.

To start a kick/ban vote simply use: /vote kick/ban Player (optional message).

For Example: /vote kick Noviets for killing newspawns?


To start a day/night vote, simply have the word "day" or "night" in the vote.


The Daytime and Nighttime config are a percentage of the time in decimal. So 0.5 would be the middle of the day. The default config for day and night are just before midnight and midday (0.9 and 0.4).

**LootModes:**

Useage: /vote Lootmode (Mode)

**Modes are**:
**Full Loot**: everything or full
**BackPack Only**: backpack
**Backpack with infamy**: infamy or default
**None**: none or off


So if you want to vote for full loot, you can use:

/vote lootmode and the word  "full" or "everything".