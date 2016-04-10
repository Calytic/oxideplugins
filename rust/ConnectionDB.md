**What's this for?:**

Plugin devs to check specific details about players, etc get timestamp of when a player last connected.


Obviously it's only going to have data for when the plugin was first loaded, so preferably a fresh server is the best scenario for this but it'll work well regardless. Inactive players just won't exist.

**Player Hooks:**

ConnectionDataExists(BasePlayer player) [If User Exists] [Returns bool format]

ConnectionDataExistsFromID(ulong steamid) [If User Exists] [Returns bool format]

FirstSeen(BasePlayer player) [Init Timestamp] [Returns DateTime format]

FirstSeenFromID(ulong steamid) [Init Timestamp] [Returns DateTime format]

LastSeen(BasePlayer player) [Last Timestamp] [Returns DateTime format]

LastSeenFromID(ulong steamid) [Last Timestamp] [Returns DateTime format]

Connections(BasePlayer player) [Player Connection Count] [Returns int format]

ConnectionsFromID(ulong steamid) [Player Connection Count] [Returns int format]

SecondsPlayed(BasePlayer player) [Seconds Connected] [Returns int format]

SecondsPlayedFromID(ulong steamid) [Seconds Connected] [Returns int format]

FirstIP(BasePlayer player) [Init IP] [Returns string format]

FirstIPFromID(ulong steamid) [Init IP] [Returns string format]

LastIP(BasePlayer player) [Last IP] [Returns string format]

LastIPFromID(ulong steamid) [Last IP] [Returns string format]

FirstName(BasePlayer player) [First Connection Name] [Returns string format]

FirstNameFromID(ulong steamid) [Last Connection Name] [Returns string format]

LastName(BasePlayer player) [First Connection Name] [Returns string format]

LastNameFromID(ulong steamid) [Last Connection Name] [Returns string format]

DisconnectReason(BasePlayer player) [Last Disconnect Reason] [Returns string format]

DisconnectReasonFromID(ulong steamid) [Last Disconnect Reason] [Returns string format]

IsPlayerAliveFromID(ulong steamid) [Is the player alive?] [Returns bool format]

**General Hooks:**

UniqueConnections() [Server Unique Connections] [Returns int format]

ConnectionPlayerCount() [Database Entry Count] [Returns int format]

**Example Usage (Prints First IP Address to console):**
Code (C#):
````

        [PluginReference]

        Plugin ConnectionDB;


        private void OnPlayerInit(BasePlayer player)

        {

            Puts(Convert.ToString(ConnectionDB.CallHook("FirstIP", player)));

        }

 
````


**Default Configuration:**

````
{

  "Admin": {

    "AuthLevel": 2

  },

  "DB": {

    "UniqueConnections": 0

  },

  "General": {

    "ConfigInit": 1447514418,

    "Debug": true

  },

  "Timers": {

    "SecondsInterval": 10

  }

}
````