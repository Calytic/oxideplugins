**Thanks for over 20.000 downloads!**
**

Permissions:**


* 
****deathnotes.see ****needed to see the messages **(if enabled in the config file)**
* 
**deathnotes.customize** needed for /deaths


**

How to grant permissions:**

Use following CONSOLE commands to grant permissions


* 
**grant user <player> <permission>** grant permission to player
* 
**grant group <group> <permission>** grant permission to group


**

Commands:**


* 
**/deathnotes **show infos about the plugin
* 
**/deaths **show settings
* 
**/deaths set <field> <value>** change a setting


**

Features:**


* All types of deaths
* UI Notifications
* Customizeable random messages
* Various customizeable death informations (Attacker, Victim and animal names, distance, weapon used and 
hitbone)
* Message radius (Recomended for high-populated servers)
* Custom coloring


**

Disabling specific deaths:**

To disable specific kill messages, just make the messages list of the death type you do not want to see empty.  Example:

"AnimalDeath": [],
**

Configfile (yourserver/oxide/config/DeathNotes.json):**

````
{

  "Attachments": {},

  "Bodyparts": {},

  "Messages": {

    "Animal": [

      "A {attacker} followed {victim} until it finally caught him."

    ],

    "Animal Sleeping": [

      "{victim} was killed by a {attacker} while having a sleep."

    ],

    "AnimalDeath": [

      "{attacker} killed a {victim} with a {weapon}{attachments} from {distance}m."

    ],

    "Bleeding": [

      "{victim} bled out."

    ],

    "Blunt": [

      "{attacker} used a {weapon} to knock {victim} out."

    ],

    "Blunt Sleeping": [

      "{attacker} used a {weapon} to turn {victim}'s dream into a nightmare."

    ],

    "Bullet": [

      "{victim} was shot in the {bodypart} by {attacker} with a {weapon}{attachments} from {distance}m."

    ],

    "Bullet Sleeping": [

      "Sleeping {victim} was shot in the {bodypart} by {attacker} with a {weapon}{attachments} from {distance}m."

    ],

    "Cold": [

      "{victim} became an iceblock."

    ],

    "Drowned": [

      "{victim} tried to swim."

    ],

    "Explosion": [

      "{victim} was shredded by {attacker}'s {weapon}"

    ],

    "Explosion Sleeping": [

      "{victim} was shredded by {attacker}'s {weapon} while sleeping."

    ],

    "Fall": [

      "{victim} did a header into the ground."

    ],

    "Generic": [

      "The death took {victim} with him."

    ],

    "Generic Sleeping": [

      "The death took sleeping {victim} with him."

    ],

    "Heat": [

      "{victim} burned to ashes."

    ],

    "Helicopter": [

      "{victim} was shot to pieces by a {attacker}."

    ],

    "Helicopter Sleeping": [

      "{victim} was sleeping when he was shot to pieces by a {attacker}."

    ],

    "HelicopterDeath": [

      "The {victim} was taken down."

    ],

    "Hunger": [

      "{victim} forgot to eat."

    ],

    "Poison": [

      "{victim} died after being poisoned."

    ],

    "Radiation": [

      "{victim} became a bit too radioactive."

    ],

    "Slash": [

      "{attacker} slashed {victim} in half."

    ],

    "Slash Sleeping": [

      "{attacker} slashed sleeping {victim} in half."

    ],

    "Stab": [

      "{victim} was stabbed to death by {attacker} using a {weapon}."

    ],

    "Stab Sleeping": [

      "{victim} was stabbed to death by {attacker} using a {weapon} before he could even awake."

    ],

    "Structure": [

      "A {attacker} impaled {victim}."

    ],

    "Suicide": [

      "{victim} had enough of life."

    ],

    "Thirst": [

      "{victim} dried internally."

    ],

    "Trap": [

      "{victim} ran into a {attacker}"

    ],

    "Turret": [

      "A {attacker} defended its home against {victim}."

    ],

    "Unknown": [

      "{victim} died. Nobody knows why, it just happened."

    ],

    "Unknown Sleeping": [

      "{victim} was sleeping when he died. Nobody knows why, it just happened."

    ]

  },

  "Names": {},

  "Settings": {

    "Attachments Color": "#C4FF00",

    "Attachments Formatting": " ({attachments})",

    "Attachments Split": " | ",

    "Attacker Color": "#C4FF00",

    "Bodypart Color": "#C4FF00",

    "Chat Icon (SteamID)": "76561198077847390",

    "Console Formatting": "{Message}",

    "Distance Color": "#C4FF00",

    "Enable Showdeaths Command": true,

    "Formatting": "[{Title}]: {Message}",

    "Log to File": false,

    "Message Color": "#696969",

    "Message Radius": 300.0,

    "Message Radius Enabled": false,

    "Needs Permission": false,

    "Simple UI Hide Timer": 5.0,

    "Strip Colors from Simple UI": false,

    "Title": "Death Notes",

    "Title Color": "#80D000",

    "Use Popup Notifications": false,

    "Use Simple UI": false,

    "Victim Color": "#C4FF00",

    "Weapon Color": "#C4FF00",

    "Write to Console": true

  },

  "Weapons": {}
}
````


**

Messagefile (yourserver/oxide/lang/en.DeathNotes.json):**

````
{

  "No Permission": "You don't have permission to use this command.",

  "Hidden": "You do no longer see death messages.",

  "Unhidden": "You will now see death messages."
}
````


**

Usage notes:**


* In order for any Config File changes to take effect in game you must reload the plugin. Simply type **oxide.reload 
DeathNotes **in your server's console.
* When editing the config- or messagefile, make sure you respect the Config File's quotation marks, commas, braces, square brackets and line indentation, or else you 
may cause plugin malfunctions. You can check that **> here <**