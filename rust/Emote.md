**[WARNING: **Will currently bug out with any other chat plugins using OnPlayerChat - trying to work with author of BetterChat on the issue and will upload to GitHub for any other chat plugin authors**]**


Quite a simple one this, basically use "/me slaps himself silly" to produce the following:


Permissions

**emote.canemote** - Allows the person or group to use emotes



Default Config:


````

{

  "Config": {

  "EnableEmotes": "false",

  "Text": "<color=#f0f0f0><i><b>{Player}</b> {Message}</i></color>"

  },

  "Emotes": {

  ":$": "blushes",

  ":-$": "blushes",

  ":(": "sulks",

  ":-(": "sulks",

  ":)": "smiles",

  ":-)": "smiles",

  ":*": "blows a kiss",

  ":-*": "blows a kiss",

  ":@": "looks angry",

  ":-@": "looks angry",

  ":|": "is speechless",

  ":-|": "is speechless",

  ":=$": "blushes",

  ":=(": "sulks",

  ":=)": "smiles",

  ":=*": "blows a kiss",

  ":=@": "looks angry",

  ":=|": "is speechless",

  ":=d": "grins",

  ":=D": "grins",

  ":=p": "sticks out a tongue",

  ":=P": "sticks out a tongue",

  ":d": "grins",

  ":-d": "grins",

  ":D": "grins",

  ":-D": "grins",

  ":p": "sticks out a tongue",

  ":-p": "sticks out a tongue",

  ":P": "sticks out a tongue",

  ":-P": "sticks out a tongue",

  "\\o": "waves back",

  "]:)": "gives an evil grin",

  ">:)": "gives an evil grin",

  "o/": "waves",

  "x(": "looks angry",

  "x-(": "looks angry",

  "X(": "looks angry",

  "X-(": "looks angry",

  "x=(": "looks angry",

  "X=(": "looks angry"

  },

  "Plugin": {

  "Version": "1.0.1"

  }

}

 
````


**Available Hooks:**

CheckForEmotes[BasePlayer player, string message]:


If an emote rule is matched this will return the string it would otherwise send to the chat. Otherwise, it will return your original message