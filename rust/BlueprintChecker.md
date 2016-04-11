A simple tool to allow authorised users to check how many blueprints an online player has currently learnt; for the rare situation that knowing such a thing actually becomes important.


What blueprints a player actually knows can also be displayed, either in chat or the server console. You may also check a specific blueprint as well.

Usage:


/bpcheck help : Display Version and Help Information

/bpcheck 'username' : Count Known BPs

/bpcheck 'username' knows 'itemname' : Confirm player has specific BP

/bpcheck 'username' listinchat : Display known BPs in Chat

/bpcheck 'username' listincon  : Display known BPs in Server Console

For example:

````
/bpcheck "Dablin"


'Dablin' knows 44 blueprints of 119


/bpcheck "Dablin" knows "Hunting Bow"


YES, 'Dablin' does know the Hunting Bow BP


/bpcheck "Dablin" knows "Assault Rifle"


NO, 'Dablin' does NOT know the Assault Rifle BP
````

Note: Items within Rust have both a long name (with spaces) and short name (no spaces)


ie. Hunting Bow = bow.hunting


Either can be used as they are treated by this plugin equally.

I hope someone finds this useful. If you have any suggestions on how to improve this or would like to see an additional feature please feel free to let me know.

**Default Configuration Parameters:**

````
{

  "ARG_Help": "help",

  "ARG_Knows": "knows",

  "ARG_ListInChat": "listinchat",

  "ARG_ListInScon": "listincon",

  "CFG_AuthorityLevelRequired": 2,

  "CFG_UpdatePlayerOnConnect": true,

  "CFG_UpdatePlayerOnStudy": true,

  "CFG_UpdatePlayerVerbose": true,

  "MSG_AccessDenied": "You are not allowed to use this command!",

  "MSG_CommandLine": "\nType /bpcheck 'username or steamid' or\n\t     /bpcheck help for more info",

  "MSG_Help": "/bpcheck 'username' : Count Known BPs\n/bpcheck 'username' knows 'itemname' : Confirm player has specific BP\n/bpcheck 'username' listinchat : Display known BPs in Chat\n/bpcheck 'username' listincon : Display known BPs in Server Console",

  "MSG_Knows": "Knows",

  "MSG_KnowsCount": "'{0}' knows {1} of {2} BPs",

  "MSG_KnowsNot": "NO, '{0}' does NOT know the {1} BP",

  "MSG_KnowsThis": "YES, '{0}' does know the {1} BP",

  "MSG_LogNoArg": "{0} used /bpcheck",

  "MSG_LogWithArg": "{0} used /bpcheck {1}",

  "MSG_MultiplePlayersFound": "Multiple Players Found - Please be more specific",

  "MSG_PlayerNotFound": "Player '{0}' not online or found in database",

  "MSG_What": "Um, what the hell is a '{0}'?"

}
````


**Coming Features:**


* An optional GUI System instead of using the chat or console system to display player blueprint information
* Direct offline player data access (without cache) - if I ever figure out how
* The ability to add/remove specific blueprints to player(s) on demand or on player connect.
* The ability to prevent specific blueprints from being learnable - will be automatically revoked if they try.


**Known Issues:**


* The use of double quotes around user names and items is only required if a space exists within the name. Not using double quotes within such names can confuse the argument check with which there will be no response taken by this plugin. I'll fix this with an invalid argument error display.
* In the unlikely event a player joins with the name "help", his information will not be able to be checked unless his/her steam ID is used in place of their name. The help info will override the check.


**Credits:
**

I wish to thank 4seti, ApocDev, Cheeze and Nogrod for developing some really fine plugins, which I shameless perused and stole some coding ideas from for this plugin - yeah I know I suck! - seriously though, just relatively new at programming for Oxide and was clueless about how to go about some things.


Also wish to thank [Rizzok](http://oxidemod.org/members/rizzok.59010/) and [Galgenjunge](http://oxidemod.org/members/galgenjunge.88499/) for providing help trying to fix this plugin when Rust updates inevitably broke it and I wasn't around to do anything about.