This plugin will allow you to set Titles to players when certain criteria have been met or when they are a leader of something.


At current there are 3 different categories.


* Kills - Person with the most kills gets a title
* Head shots - Person with the most Head shots gets a title
* Animal Kills = Person with the most animal kills gets a title

And I am currently expanding this list, as well as adding in general criteria allowing multiple people to be assigned a Title.

**BetterChat**

This integrates in with Betterchat if you use it, when the config is set to use it.


This works on a group basis so the user gets assigned to the Oxide Group, so please ensure you have given the BetterChat permission to the relevant group otherwise this wont work.

**Config**

Brief explanation of the config file.

````
{

  "0IsAnimalKillChallangeActive": true, -- Is the Animal Challange Active

  "0IsHeadshotChallangeActive": true, -- Is the Headshot Challenge Active

  "0IsKillChallangeActive": true, -- Is the Kill Challenge Active

  "1BCaUseBetterChat": true, -- Use Better Chat

  "1IgnoreSleepers": false, -- Ignore Sleeper Deaths

  "1IgnoreAdmins": false, --Ignores Admin Kills

  "1AnnounceNewLeader": true, -- Announce to the server if theres a new leader

  "2BCAnimalGroup": "Better Chat Group", -- Oxide Group for Animal Kills For Better Chat

  "2BCheadshotGroup": "Better Chat Group", -- Oxide Group for Headshots For Better Chat

  "2BCKillsGroup": "Better Chat Group", -- Oxide Group for player Kills For Better Chat

  "3animalKillTitle": "[Hunter]", -- Non Better Chat Title(animal kills)

  "3headshotTitle": "[Scalper]",  -- Non Better Chat Title(Headshots)

  "3KillTitle": "[TopGun]"  -- Non Better Chat Title(Player Kills)

}
````