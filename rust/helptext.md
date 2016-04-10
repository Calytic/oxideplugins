This tiny plugin provides a /help command that hooks into all other plugins and send their helptexts.

**Default config**

````
{

  "CustomHelpText": [

    "custom helptext",

    "custom helptext"

  ],

  "Settings": {

    "UseCustomHelpText": "false",

    "AllowHelpTextFromOtherPlugins": "true"

  }

}
````

UseCustomHelpText - Adds your CustomHelpText messages if set to true

AllowHelpTextFromOtherPlugin - if set to false it will block all helptext from other plugins

**Plugin Devs**

Same syntax like Oxide 1.18

````
function PLUGIN:SendHelpText(player)

    player:ChatMessage("helptext")

end
````

If you want specific helptexts only shown to admins

````
function PLUGIN:SendHelpText(player)

    if player:IsAdmin() then

        player:ChatMessage("admin helptext")

    end

    player:ChatMessage("normal helptext")

end
````