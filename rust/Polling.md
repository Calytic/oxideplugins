Poll System V 1.1.0


Rewritten version of the old poll system with using oxide's built in permission and language systems.This version also uses GUI based interface that users can easily interact with.


This version does not have backward compatibility with older versions.Your config file and data file will be deleted and replaced with the new one.

Updates
V 1.2.1 - 17/03/2016


* If you use Poll Schedules you need to update, for scheduled poll GUI, [CronLibrary V:1.0.2](http://oxidemod.org/plugins/cron-library.1754/)
* Added a GUI to show scheduled polls.Under Help Menu
* Added a GUI to show custom polls.Under Help Menu
* [](http://oxidemod.org/attachments/help-predefined-polls-png.19320/)[](http://oxidemod.org/attachments/help-custom-polls-png.19319/)[](http://oxidemod.org/attachments/help-scheduled-polls-png.19321/)

V 1.2.0 - 12/03/2016


* This update will replace config file.
* Added Poll Schuldes using [Cron Library for Rust](http://oxidemod.org/plugins/cron-library.1754/).

* Added Custom polls with console commands.
* Custom polls will add "Polling.Create.PollCommand" permission to create polls.
* Currently, Custom Polls don't count non-voters.
* Added some examples at the bottom of plugin overview.
* Added Re-Run Poll button to poll results screen.
* Added End Vote button to end poll before time.
* Polls max duration increased to 3600 seconds.
* Added ChatProfile settings for changing chat icon.You need to write SteamId to work
* [](http://oxidemod.org/attachments/current-poll-not-voted-end-poll-png.19139/)
 End poll button

* [](http://oxidemod.org/attachments/poll-history-results-rerun-png.19140/)
 Re-Run poll button

Permissions
"Polling.Can.CleanHistory" : /poll "Clean History" to clean history file.

"Polling.Can.EndPolls" : to end polls before their timers end.

Build-in Polls and Permissions


Airdrop -> "Polling.Create.Airdrop"

Ban-> "Polling.Create.Ban"

Custom-> "Polling.Create.Custom"

Heli-> "Polling.Create.Heli"

Kick-> "Polling.Create.Kick"

Time-> "Polling.Create.Time"


Airdrop: To call an airdrop @ random location.

Ban: To ban an User from server.

Custom: To ask custom questions to players.(How are you today ? )

Heli: To call an patrol helicopter @ random location.

Kick: To kick an User from server.

Time: To change time to Day or Night

Soon


* Adding support for custom polls to run console commands.1.2.0
* Increasing the upper time limit for polls.1.2.0
* Adding a command to stop polls before its ending time.1.2.0
* Adding a GUI to show schulded polls.1.2.1
* Adding a GUI to show custom polls.1.2.1
* Nothing in my mind, but I'm open for ideas 

Data & Config Files


Config file : Polling.json @ oxide\config

Language file : Polling.en.json @ oxide\lang

Data file : Polling.json @ oxide\data


Config

Main Settings ->


* ChatCommand : To change chat command /poll

* ChatTag : Announcements will use this as name

* ConfigVersion: Do not edit this settings

* CountNonVoters: If this set to true , active players who does not voted will be counted as No - This only apply for predefined Yes/No type of polls.

* ReminderTimer: Every given second a reminder will remind active players who haven't voted yet to vote.

Plugin Settings->


* AutoSkipNights 


* Enabled: If this set to true every given in-game time there will be a skip night poll.

* TimeOfSkip: The in-game time for starting a skip night poll.

* CargoPlane


* Enabled : If you have another plugin to manage Cargo Planes set this to true and fill PluginConsoleCommand.

* PluginConsoleCommand: If this option enabled poll system will use this console command to call a Cargo Plane .

* PatrolHelicopter


* Enabled: If you have another plugin to manage Patrol Helicopter set this to true and fill PluginConsoleCommand.

* PluginConsoleCommand: If this option enabled poll system will use this console command to call a Patrol Helicopter.Default Config & English Language File

Config file:

````
{

  "Settings": {

    "Main Settings": {

      "ChatCommand": "poll",

      "ChatProfile": "76561198286931766",

      "ChatTag": "Polling",

      "ConfigVersion": "1.2.2",

      "CountNonVoters": true,

      "ReminderTimer": 20

    },

    "Plugin Settings": {

      "AutoSkipNights": {

        "Enabled": true,

        "TimeOfSkip": "18:00"

      },

      "CargoPlane": {

        "Enabled": false,

        "PluginConsoleCommand": ""

      },

      "PatrolHelicopter": {

        "Enabled": false,

        "PluginConsoleCommand": ""

      }

    },

    "Poll Schuldes": [],

    "Polls": {}

  }

}
````

English Language File

````

{

  "AlreadyVoted": "You have already <color=#ce422b>voted</color> for current poll!\n\nCheck back later or keep an eye on chat for an announcement for new poll.",

  "AutoRefresh": "This menu refresh automatically every <color=#ce422b>10</color> seconds.",

  "BanReason": "You have been banned by poll results.",

  "ButtonClose": "Close",

  "ButtonCurrent": "Current Poll",

  "ButtonEndVote": "End Poll",

  "ButtonHelp": "Help",

  "ButtonHistory": "Poll History",

  "ButtonNextPage": "Next Page",

  "ButtonPreviousPage": "Previous Page",

  "ButtonReRunPoll": "Re-run Poll",

  "ButtonResults": "Results",

  "CanCreatePoll": "<color=#ce422b>Admin</color> Menu\n   You have permission to create given types of polls.\n\n",

  "CantCreatePoll": "You <color=#ce422b>don't</color> have permission to create polls.",

  "CurrenOwner": "<color=#ce422b>Current</color> Poll By",

  "CurrenQuestion": "<color=#ce422b>Current</color> Poll Question",

  "CurrenStarted": "<color=#ce422b>Current</color> Poll Started",

  "CurrenTimeleft": "<color=#ce422b>Current</color> Poll Will End in {0} Second(s)",

  "CustomPollCount": "Currently there is/are {0} custom polls",

  "Day": "Day",

  "ErrorAlreadyPoll": "There is already a poll running.",

  "ErrorEndVote": "You <color=#ce422b>don't</color> have permission to end polls.",

  "ErrorNoActivePoll": "There is <color=#ce422b>no</color> active poll right now!\n\nCheck back later or keep an eye on chat for an announcement.",

  "ErrorPermission": "You <color=#ce422b>don't</color> have permission to create {0} polls.",

  "ErrorSyntax": "There has been an syntax error in command.\nCorrect form is\n",

  "ErrorTime": "You have entered an invalid time.Time limits Min:30 Max:3600",

  "HelpAirdropPoll": "<color=#ce422b> * </color>You can use following command to create a poll to call an airdrop.",

  "HelpAirdropPollUsage": "<color=#ce422b>     /{0}</color> 'Airdrop' 'Timer'",

  "HelpCustomPoll": "<color=#ce422b> * </color>You can use following command to create a custom poll to ask anything.",

  "HelpCustomPollUsage": "<color=#ce422b>     /{0}</color> 'Custom' 'Timer' 'Question' 'Choice1' 'Choice2' 'Choice3' ... 'ChoiceN'",

  "HelpHeliPoll": "<color=#ce422b> * </color>You can use following command to create a poll to call an patrol helicopter.",

  "HelpHeliPollUsage": "<color=#ce422b>     /{0}</color> 'Heli' 'Timer'",

  "HelpTimePoll": "<color=#ce422b> * </color>You can use following command to create a poll to call to change time.",

  "HelpTimePollUsage": "<color=#ce422b>     /{0}</color> 'Time' 'Timer' 'Day|Night'",

  "HelpUserBanPoll": "<color=#ce422b> * </color>You can use following command to create a poll to ban a player.",

  "HelpUserBanPollUsage": "<color=#ce422b>     /{0}</color> 'Ban' 'Timer' 'Name|SteamID'",

  "HelpUserKickPoll": "<color=#ce422b> * </color>You can use following command to create a poll to kick a player.",

  "HelpUserKickPollUsage": "<color=#ce422b>     /{0}</color> 'Kick' 'Timer' 'Name|SteamID'",

  "HistoryCount": "Currently there is/are <color=#ce422b>{0}</color> poll(s) in history.",

  "HowToVote": "You can use /{0} to vote.",

  "KickReason": "You have been kicked by poll results.",

  "Night": "Night",

  "No": "No",

  "NoPoll": "There is <color=#ce422b>no</color> active poll right now!",

  "NoUser": "There is no user found with search with Name/SteamID ({0})",

  "Page": "Page {0}",

  "PollNotVoted": "You haven't voted.Please vote now!\n     <color=red>*</color> {0}",

  "PollStarted": "Poll Started\n     <color=red>*</color> {0}",

  "QuestionAirdrop": "Do you want to call an Airdrop ?",

  "QuestionBan": "Do you want to ban {0} ?",

  "QuestionCustom": "Don't edit this",

  "QuestionHeli": "Do you want to call a Patrol Helicopter ?",

  "QuestionKick": "Do you want to kick {0} ?",

  "QuestionTime": "Do you want to change time to {0} ?",

  "Voted": "You have voted <color=#ce422b>{0}</color> for {1}.",

  "VoteFinish": "Vote Finished\n     <color=red>*</color> {0}",

  "VoteResult": "{0} people gave vote for {1}",

  "Yes": "Yes"

}

 
````

Poll Schedules & Custom Polls

You can add custom polls and poll schedules via editing Config file (Polling.json @ oxide\config)

You can add Poll Schedules like given example.This one will run a poll every hour, on the hour for calling Airdrop for 120 seconds.

````
    "Poll Schedules": [

      {

        "Cron": "0 * * * *",

        "Duration": 120,

        "PollType": "Airdrop"

      }

    ]
````

You can add custom polls like given example.This one will add a custom poll that ask players to restart server, and if players say Yes poll will run "save,quit" commands in order.

````
"Polls": {

      "Restart": {

        "AskQuestion": "Do you want to restart server now?",

        "Description": "You can use following command to create a poll to restart server.",

        "PollChoices": {

          "0": {

            "ChoiceConsoleCommand": "save,quit",

            "ChoiceText": "Yes"

          },

          "1": {

            "ChoiceConsoleCommand": "",

            "ChoiceText": "No"

          }

        }

      }

    }
````