**Optional
[DeadPlayerList](http://oxidemod.org/resources/deadplayerlist.696/)


Features:**

- **Find** all **players** with partial names

- Get the **current state **of players (Connected/Disconnect, Spectating/Alive/Sleeping/Dead)

- Find all **sleeping bags **owned by a specific player

- Find all **sleepers** with partial names

- Find all **Building Privileges (Tool Cupboard) **by specific player

- Find players that own certain items, with an optional minimum number

- **Teleport** to any of your results

(You may request to me to add other elements to find)

**Commands:**
- /find players NAME/STEAMID => returns all players AND sleepers that partially or fully match the name, or returns the player that has the specific steamid (or partial steamid)
- /find bag NAME/STEAMID => returns all sleeping bags owned by a specific player
- /find privilege NAME/STEAMID => returns all cupboards where the target player is whitelisted.
- /find tp FINDID => teleport to any of your previous results.

- /find info FINDID => get full informations of a found data.
- /find  item "Full name or Shortname" "Minimum amount" => this will look everywhere on your server to find the items that match the name and the amount.

**Exemple usage:**

/find item "Metal Fragments" 50000 => will show me who owns 50k+ of metal fragments (needs to be in the same box)

**Note:**

Using: **[DeadPlayerList](http://oxidemod.org/resources/deadplayerlist.696/) **will bring you more results as this plugin on it's own only search for sleepers and alive players. DeadPlayerList increases the search to dead & disconnected players.