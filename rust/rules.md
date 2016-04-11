**Note:** Development for rules is slowed, as no new features are coming to mind, and with notifier now supporting rules and multi-language this no longer has a real demand for attention so unless requested development won't continue much on Server Rules.


Welcome to Rules! This plugin gives Admins the ability to add rules to a list, and remove them as they please.


You can edit rules via the config files "rules.json"

**Example**:

Default:

````
"setRules": [

    "1. No Cheating!",

    "2. No Abusing broken mechanics!",

    "3. Respect thy fellow players"

  ]
````

Updated:

````
"setRules": [

    "1. No Cheating!",

    "2. No Abusing broken mechanics!",

    "3. Respect thy fellow players",

    "4. Test Rule",

    "5. No Eating"

  ]
````


**Translations**

The Translation arrays look like so:

````
this.Config.RulesFr = [

                "1. place french translations here.",

            ];


            this.Config.RulesSp = [

                "1. place Spanish translations here."

            ];


 
````

Add rules to these just as you would the regular set rules, like so:

````
this.Config.RulesFr = [

                "1. place french translations here.",

                "2. Rule in french"

            ];


            this.Config.RulesSp = [

                "1. place Spanish translations here."

                "2. Rule in Spanish"

            ];

 
````

Please note: The plugin does not translate for you, the rules will have to be placed translated already.
**Current Commands:**


* /rules - Basic command shows the list of server rules
* /rules add "Add new Rule" - Adds a rule that you place in the  " " to the list
* /rules del # - Deletes the rule with the entered number


**Example:**

/rules add "No Abuse of Broken Mechanics" - this adds the rule "No Abuse of Broken Mechanics" to the list of rules


/rules del 3 - this will remove rule 3 from your list automatically.

**Please Note**: if and ONLY IF you use the command to add a rule you DO NOT need to add a number in front of it, the plugin will do this for you automatically (this is going to contribute to the delete command once it's finished).
**However:** If you modify the config file to add rules than you will need to add the numbers yourself as shown in the above example at the top. Or you can not use the command all together and choose how you want it to be displayed through the config. The plugin won't care.

**Current Features:**


* Plugged into Help for help text
* Supports in game rule list additions in game
* Admin Only access
* Supports Del Command
* Editable Welcome Message
* Auto display of rules set on a timer
* Now plays well with notifier (however /rules will no longer work if you use notifier with this plugin)

**New Config Features**

With the new config features I figured I should place a little explanation here for everyone.

````

    "authLvl": 2.0, - auth level required to use add and del commands

    "repeatDisplay": true, - sets to display rules on a timer

    "welcomeMsg": true, - sets to display the welcome message on user login

    "interval": 200.0 - the rate at which you want messages to display (seconds)

 
````

NOTE: Welcome message is automatically disabled if notifier the notifier plugin is enabled. I may change this if users do not wish that to happen.

**Future Features:**


* Command that will allow you to delete Rules in game (Right now I am almost done but waiting on a oxide patch) **DONE**
* Maybe more support for notifier


I will update this plugin as needed, and maybe add some other features if I can think of any. If you would like to see a feature added let me know. Thanks!