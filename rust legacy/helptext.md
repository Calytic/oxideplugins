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

````
function PLUGIN:SendHelpText(netuser)

    rust.SendChatMessage(netuser, "helptext")

end
````

If you want specific helptexts only shown to admins

````
function PLUGIN:SendHelpText(netuser)

    if netuser:CanAdmin() then

        rust.SendChatMessage(netuser, "admin helptext")

    end

    rust.SendChatMessage(netuser, "normal helptext")

end
````