Simple plugin to allow all the players on the server to vote to skip night - automatically when nightfall hits.


When the game time hits the hour defined as "sunsetHour" in the configs, a vote will open up in the chat for the time defined as "pollTimer".  Players will enter the
**/voteday**

command during this time to vote on skipping night.  If enough players vote to reach above the "requiredVotesPercentage" config setting, the plugin will skip the night and set it to the "sunriseHour" config setting.  If the vote failed, the plugin will try again in "pollRetryTime" minutes.


Config settings in SkipNightVote.json

````
{

  "Messages": {

    "noPollOpen": "No poll is open at this time.",

    "alreadyVoted": "You have already voted once.",

    "voteProgress": "Vote progress: {0} / {1} ({2}%/{3}%)",

    "voteOpenTime": "Night time skip vote is now open for {0} minute(s).",

    "voteSuccessful": "Vote was successful, it will be daytime soon.",

    "voteFailed": "Vote failed, not enough players voted to skip night time.",

    "voteReOpenTime": "Vote will re-open in {0} minute(s).",

    "voteNow": "Type <color=#FF2211>/voteday</color> now to skip night time."

  },

  "Settings": {

    "displayVoteProgress": true,

    "pollRetryTime": 5.0,

    "pollTimer": 1.0,

    "requiredVotesPercentage": 0.5,

    "sunriseHour": 8,

    "sunsetHour": 18

  },

  "Version": "1.1.0"

}
````