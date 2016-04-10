**Introduction**

This is my own take on skipping night cycles.  Includes several new options and configurations.

**Description**

Allows automatic or player voting to skip night cycles.

**Notes**:


* AutoSkip - Automatically skip night cycle bypassing player voting.  Will follow "AllowAutoNights" setting.

* AllowVoteChange - If true, players may change their vote as many times as they wish.

* AllowVoteNights and AllowAutoNights - How often a night cycle is skipped or day vote is called.  Setting these values, for example, to 1 will call a vote or automatically skip the night cycle every night. Setting to 2 will result in every other night and so on. Setting these values, for example, to +1 will call a vote or automatically skip the night cycle for 1 night before skipping the next. Setting to +2 will result in two votes or skips before skipping the next and so on.

* ShowProgress - Show progress automatically during a vote for the configured interval.
* ShowTimeChanges - Display configured message to players indicating an administrator changed the current time.

* VoteRequired - Setting this value, for example, to 1 will require yes votes to outnumber no votes by one. Setting to 2 will result in two yes votes required. Setting this value, for example, to 50% will require 50% of the current server population to vote yes.
* If a manual day vote is called, "AllowVoteNights" and "AllowAutoNights" will restart and a day vote will be called on the next night cycle or night cycle skipped.

* Setting "UsePermissions" applies to "dayvote.use" only.
* "AdminExempt" - when true, players with permission "dayvote.admin" will be excluded from player count and voting. This will allow admins to idle without interfering with day vote counts and percentages for players who are voting.
* "ShowPlayerVotes" - when true, a message is shown to all players when a player casts a vote, along with what they voted.
* "ShowDisconnectVotes" - when true, a message is shown to all players when a player that has voted and disconnects has had their vote removed.
* "RoundPercent" - when true, the percentage of players required to pass a vote is rounded to a whole number. For example, if 11.2%, 11.8%, etc. yes votes is required, this will turn it to 11% required. Therefore, 11% will be required instead of 12% to pass.
* "AirdropWait" - when true, no airdrops will be called until at least one time change is made from this plugin. This will allow the default airdrops to call until this is needed. (should keep false until a full server restart if time has already been changed, if desired)



**Permissions**

This plugin uses oxide permissions.


dayvote.use - Allows players to vote on skipping night cycles

dayvote.admin - Allow players to use administrator commands

````
oxide.grant <group|user> <name|id> dayvote.use

oxide.revoke <group|user> <name|id> dayvote.use
````


**Usage for players**


/dayvote - View help

/dayvote limits [revote] - View configuration limits

/dayvote progress - View current day vote progress

/dayvote yes - Vote yes to skip current night cycle

/dayvote no - Vote no to skip current night cycle

**Usage for administrators**


/dayvote toggle - Enable or disable day vote system

/dayvote start [req] - Manually start a day vote with optional requirement

/dayvote stop - Manually stop a day vote

/dayvote set <0-23> - Manually set current time

**Configuration file**

````
{

  "Airdrop": {

  "AirdropWait": "false",

  "Drops": "1",

  "Enabled": "false",

  "Interval": [

  "2400",

  "3000",

  "3600"

  ],

  "MinPlayers": "1"

  },

  "Messages": {

  "AdminExempt": "Day vote administrators a currently exempt from voting.",

  "AdminTimeSet": "Administrator set current time to <color=#cd422b>{set}</color> from <color=#cd422b>{current}</color>.",

  "AlreadyVoted": "You have already voted <color=#cd422b>{vote}</color> for this day vote.",

  "AutoSkip": "Night cycle started and has automatically been skipped.",

  "AutoSkipError": "Day vote is <color=#cd422b>disabled</color>.  Night cycles are automatically skipped every <color=#cd422b>{allowed}</color> nights.",

  "ChangedStatus": "Day vote <color=#cd422b>{status}</color>.",

  "Connected": "A day vote is currently in progress.  To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes/no votes required is <color=#cd422b>{required}</color>.  Current progress: <color=#cd422b>{yes}</color> voted yes, <color=#cd422b>{no}</color> voted no.",

  "DisconnectVotes": "<color=#cd422b>{player}'s</color> vote of <color=#cd422b>{vote}</color> has been removed for disconnecting.",

  "ManualAutoSkipError": "You cannot manually start or stop a day vote when night cycles are automatically skipped.",

  "ManualPercentProgress": "Current progress: <color=#cd422b>{yes}%</color> (<color=#cd422b>{required}%</color> required) of <color=#ffd479>{players}</color> online players voted yes, <color=#cd422b>{no}%</color> voted no.  Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.",

  "ManualPercentVoteOpen": "Administrator started a skip night cycle vote.  To skip, type <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes votes required is <color=#cd422b>{required}%</color> of <color=#ffd479>{players}</color> online players.  Voting ends in <color=#cd422b>{seconds} seconds</color>.",

  "ManualProgress": "Current progress: <color=#ffd479>{yes} yes</color>, <color=#cd422b>{no} no</color>.  Yes/no votes required is <color=#cd422b>{required}</color>.  Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.",

  "ManualRevoteClose": "A day vote is not currently open.  However, a pending <color=#f9169f>revote</color> has been aborted.",

  "ManualVoteClose": "Administrator aborted current day vote.  <color=#cd422b>Night cycle will continue.</color>",

  "ManualVoteOpen": "Administrator started a skip night cycle vote.  To skip, type <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes/no votes required is <color=#cd422b>{required}</color>.  Voting ends in <color=#cd422b>{seconds} seconds</color>.",

  "NoPermission": "You do not have permission to use this command.",

  "NotEnabled": "Day vote is <color=#cd422b>disabled</color>.",

  "NotNumber": "Time must be a number between <color=#cd422b>0</color> and <color=#cd422b>23</color>.",

  "NoVote": "No day vote currently in progress.  Wait until the night cycle has started.",

  "NoVoteChange": "You have already voted <color=#cd422b>{vote}</color> for this day vote.  Permission to change your vote is disabled.",

  "PercentProgress": "Current progress: <color=#cd422b>{yes}%</color> (<color=#cd422b>{required}%</color> required) of <color=#ffd479>{players}</color> online players voted yes, <color=#cd422b>{no}%</color> voted no.  Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Voting ends in <color=#cd422b>{seconds} seconds</color>.",

  "PercentVoteFailed": "<color=#cd422b>Voting is closed.</color>  Day vote failed, <color=#cd422b>{yes}%</color> of <color=#ffd479>{players}</color> online players voted yes, <color=#cd422b>{no}%</color> voted no.  Yes votes required is <color=#cd422b>{required}%</color>.  Night cycle will continue.",

  "PercentVoteOpen": "Night cycle has started.  To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes votes required is <color=#cd422b>{required}%</color> of <color=#ffd479>{players}</color> online players.  Voting ends in <color=#cd422b>{seconds} seconds</color>.",

  "PercentVotePassed": "<color=#cd422b>Voting is closed.</color>  Day vote passed, <color=#cd422b>{required}%</color> yes vote requirement of <color=#ffd479>{players}</color> online players reached, <color=#cd422b>{no}%</color> voted no.  Night cycle will end.",

  "PlayerVotes": "<color=#cd422b>{player}</color> has voted <color=#ffd479>{vote}</color>.",

  "PlusAutoSkipError": "Day vote is <color=#cd422b>disabled</color>.  Night cycles are automatically skipped.  Night cycles are forced every <color=#cd422b>{allowed}</color> nights.",

  "PlusVoteSkip": "Night cycle has started.  Night cycle is forced every <color=#cd422b>{allowed}</color> nights.  Night cycle will continue.",

  "PrecentConnected": "A day vote is currently in progress.  To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes votes required is <color=#cd422b>{required}%</color> of <color=#ffd479>{players}</color> online players.  Current progress: <color=#cd422b>{yes}%</color> voted yes, <color=#cd422b>{no}%</color> voted no.",

  "Progress": "Current progress: <color=#ffd479>{yes} yes</color>, <color=#cd422b>{no} no</color>.  Yes/no votes required is <color=#cd422b>{required}</color>.  Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Voting ends in <color=#cd422b>{seconds} seconds</color>.",

  "Revote": "A one time only day revote will begin in <color=#cd422b>{seconds} seconds</color>.",

  "RevoteWait": "A pending <color=#f9169f>revote</color> is already scheduled to start in less than <color=#cd422b>{seconds} seconds</color>.",

  "SetRevoteClose": "A pending <color=#f9169f>revote</color> has been aborted.",

  "SetToNightHour": "Current time set to configured night hour.  No day vote will be started or night cycle automatically skipped.",

  "SetVoteClose": "Administrator aborted current day vote by manually setting new time.  <color=#cd422b>New time will now take effect.</color>",

  "SkipAutoSkip": "Night cycle has started.  Automatic skipping allowed every <color=#cd422b>{allowed}</color> nights.  Current night cycle is <color=#cd422b>{current}</color>.  Night cycle will continue.",

  "SkipPlusAutoSkip": "Night cycle has started.  Night cycle is forced every <color=#cd422b>{allowed}</color> nights.  Night cycle will continue.",

  "TimeSet": "Current time set to <color=#cd422b>{set}</color> from <color=#cd422b>{current}</color>.",

  "VoteAlreadyOpen": "A day vote is currently in progress.",

  "Voted": "You have successfully voted <color=#cd422b>{vote}</color> for this day vote.",

  "VoteFailed": "<color=#cd422b>Voting is closed.</color>  Day vote failed, <color=#cd422b>{no} no</color> to <color=#ffd479>{yes} yes</color> votes.  Yes/no votes required is <color=#cd422b>{required}</color>.  Night cycle will continue.",

  "VoteFailedNoVote": "<color=#cd422b>Voting is closed.</color>  Day vote failed, no votes were cast.  Night cycle will continue.",

  "VoteMinPlayers": "You cannot manually start a day vote, minimum online players of <color=#cd422b>{minimum}</color> was not reached.  Current online players is <color=#cd422b>{current}</color>.",

  "VoteNotNight": "You cannot manually start a day vote, current time of <color=#cd422b>{current}</color> cannot be between configured day and night hours of <color=#cd422b>{day}</color> and <color=#cd422b>{night}</color>.",

  "VoteNotOpen": "A day vote is not currently in progress.",

  "VoteOpen": "Night cycle has started.  To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes/no votes required is <color=#cd422b>{required}</color>.  Voting ends in <color=#cd422b>{seconds} seconds</color>.",

  "VotePassed": "<color=#cd422b>Voting is closed.</color>  Day vote passed, <color=#ffd479>{yes} yes</color> to <color=#cd422b>{no} no</color> votes.  Night cycle will end.",

  "VoteSkip": "Night cycle has started.  Day votes allowed every <color=#cd422b>{allowed}</color> nights.  Current night cycle is <color=#cd422b>{current}</color>.  Night cycle will continue.",

  "WrongArgs": "Syntax error.  Use <color=#cd422b>/dayvote</color> for help."

  },

  "Revote": {

  "Announce": "true",

  "Enabled": "false",

  "Prefix": "[<color=#f9169f> Revote </color>]",

  "ProgressInterval": "15",

  "ShowProgress": "true",

  "VoteDuration": "60",

  "VoteRequired": "40%",

  "WaitDuration": "30"

  },

  "Settings": {

  "AdminExempt": "false",

  "AllowAutoNights": "1",

  "AllowVoteChange": "false",

  "AllowVoteNights": "1",

  "AutoSkip": "false",

  "DayHour": "6",

  "Enabled": "true",

  "NightHour": "18",

  "Prefix": "[<color=#cd422b> Day Vote </color>]",

  "ProgressInterval": "30",

  "RoundPercent": "true",

  "ShowDisconnectVotes": "true",

  "ShowPlayerVotes": "false",

  "ShowProgress": "true",

  "ShowTimeChanges": "true",

  "UsePermissions": "true",

  "VoteDuration": "120",

  "VoteMinPlayers": "1",

  "VoteRequired": "50%"

  }

}
````

Configuration file will be created and updated automatically.

**More to come**


* Your suggestions


**Known Issues**


* Rust issue: Changing time interrupts default air drops