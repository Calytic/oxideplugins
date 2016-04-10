[](http://forum.rustoxide.com/plugins/678/rate)
**Natives**  is a plugin that allows only players from the server's country to join. This is useful if you want to keep your playerbase somewhat localized.

**Configuration** 

You can configure the chat name, messages, and other settings in the natives.json file under the oxide/config directory.

**Default Configuration** 

````
{

  "Settings": {

    "BroadcastKick": "true"

  },

  "Messages": {

    "PlayerKicked": "{player} kicked for not being from its native country, {country}!",

    "PlayerRejected": "Sorry, this server only allows players from its native country, {country}!"

  }

}
````

The configuration file will update automatically if there are new options available. I'll do my best to preserve any existing settings and message strings with each new version.