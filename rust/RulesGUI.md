This plugin displays all over the screen the set in config rules on connect. With the posibility to display the rules one more time by just typing /rule
**

Commads:**

/rule **=>** Displays the rules on the screen.

/rulesto** <player> => **Displays the rules on the target's screen.**[NEEDS PERMISSION]
**
**Permissions:**
**"canuserulesto" = > For the command "/rulesto <player>


Default Configuration:**

````

{

  "Backround": {

    "Enabled": false,

    "ImageURL": "https://i.ytimg.com/vi/yaqe1qesQ8c/maxresdefault.jpg"

  },

  "Messages": {

    "KICK_MESSAGE": "You disagreed with the rules!",

    "RULES_MESSAGE": [

      "<color=cyan>Welcome!</color> <color=red>The following in-game activities are prohibited in the Game:</color>",

      "<color=yellow>1.</color> Use of bots, use of third-party software, bugs.",

      "<color=yellow>2.</color> Pretending to be a member of Administration.",

      "<color=yellow>3.</color> Fraud, other dishonest actions.",

      "<color=yellow>4.</color> Flooding, flaming, spam, printing in capital letters (CAPS LOCK).",

      "<color=yellow>5.</color> Creating obstructions for other users.",

      "<color=yellow>6.</color> Advertisement, political propaganda."

    ]

  },

  "Settings": {

    "DisplayOnEveryConnect": false

  }

}

 
````