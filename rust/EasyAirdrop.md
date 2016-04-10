**Easy Airdrop** allows you to call airdrops very easily.

**Chat Commands:**


* 
**/airdrop** - Calls airdrop to random location
* 
**/airdrop player <player> **- Calls airdrop to player
* 
**/airdrop pos <x> <z> **- Calls airdrop to position
* 
**/massdrop <count> **- Calls multiple airdrops to random locations



**Console Commands:**


* 
**airdrop** - Calls airdrop to random location
* 
**airdrop player <player> **- Calls airdrop to player
* 
**airdrop pos <x> <z> **- Calls airdrop to position
* 
**massdrop <count> **- Calls multiple airdrops to random locations



**Permissions:**


* 
**airdrop.call** - needed for /airdrop
* 
**airdrop.call.player** - needed for /airdrop player
* 
**airdrop.call.position** - needed for /airdrop pos
* 
**airdrop.call.mass** - needed for /massdrop



**Known issues:**

- none

**Config File:**


* 
````
{

  "Messages": {

    "Chat Message": "<color=red>{player}</color> has called an airdrop.",

    "Console Message": "{player} has called an airdrop. {location}",

    "Massdrop Chat Message": "<color=red>{player}</color> has called <color=red>{amount}</color> airdrops.",

    "Massdrop Console Message": "{player} has called {amount} airdrops."

  },

  "Settings": {

    "Broadcast to Chat": true,

    "Send to Console": true

  }
}
````




**Future Updates:**

- information for the reciever of an playerdrop

- your suggestions


Thanks to: Wulf, Domestos and Mughisi for the first version of this plugin