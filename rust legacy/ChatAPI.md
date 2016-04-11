ChatAPI is a plugin that gives ability for plugins to control player prefix, display name ,suffix and chat color.


You seen what code parts should be changed? You want to push some update? Here [Oxide2-plugins/Legacy at master · PreFix/Oxide2-plugins · GitHub](https://github.com/PreFix/Oxide2-plugins/tree/master/Legacy) when you make Pull Request contact me.


I mainly made this plugin to add suffix for clans plugin, so please use prefix for your [Donator] ranks and stuff for now. (I'll make ability to choose what to use prefix or suffix for clan short name in short future  )

**Future plans:**


* Suggestions?


**Configuration file:**


* Chat: Player tag > "{0} ({1}) {2}" // suffix, display name, prefix 

* Chat: Player message > "{0}{1}" // chat color, message


**Now about plugin API:**


Everything should be called on hook onChatApiPlayerLoad if u want to set up it for long term usage.


If API plugin was reloaded you must to apply prefix, suffix again.

**C# example**

````
using System;

using Oxide.Core;

using Oxide.Core.Plugins;


namespace Oxide.Plugins

{

   [Info("Example", "Prefix", "0.1.0")]

   public class Example: RustLegacyPlugin

   {

     [PluginReference]

     Plugin ChatAPI;


     void Init() {

       if(ChatAPI == null) {

         Puts("Chat API not running, you won't see any prefix http://oxidemod.org/plugins/chatapi.1768/");

       }

     }


     void onChatApiPlayerLoad(NetUser netuser)

     {

       object prefix = ChatAPI?.Call("setPrefix", netuser, "Player");

       if(prefix is bool) {

         bool prefixk = (bool)prefix;

         if(prefixk) {

         Puts("Ok prefix Player was set");

        }

       }

     }

   }

}
````


**Lua example**


If return value is object use if(type(variable) == "boolean") then or if(type(variable) == "string") to find out what it is.

````
PLUGIN.Title = "ChatAPI example"

PLUGIN.Version = V(0, 1, 0)

PLUGIN.Description = "Let's test ChatAPI plugin!"

PLUGIN.Author = "Prefix"

PLUGIN.Url = "http://oxidemod.org/plugins/chatapi.1768"


local ChatAPI


function PLUGIN:Init()

  ChatAPI = plugins.Find("ChatAPI")

  if not ChatAPI then print("ChatAPI is not loaded! http://oxidemod.org/plugins/1798/") return end

end


function PLUGIN:onChatApiPlayerLoad( netuser )

   if (not(ChatAPI == nil)) then

     ChatAPI:CallHook("setPrefix", netuser, "Player")

   end

end
````


**All HOOKS:
**

````
/*

* Hooked when player types a message to chat.

* Arguments:

* NetUser

* string - message what user typed to chat

* Return values:

* string - edit current message

* bool(false) - prevent message to be shown

*/

object ChatAPIPlayerChat(NetUser netuser, string message)

/*

* Hooked when player types a message to chat.

* Arguments:

* NetUser

* Return values:

* none (void)

*/

void onChatApiPlayerLoad(NetUser netuser)
````



**All API functions:**

````
/**

** Get user's prefix

** Arguments:

** NetUser

** Return:

** if success string, on failure false

**/

object getPrefix(NetUser netuser)

/**

** Set user's prefix

** Arguments:

** NetUser

** String (prefix you want to set to)

** Optional int (Priority to set prefix, if less than current returns false)

** Return:

** if success true, on failure false

**/

bool setPrefix(NetUser netuser, string prefix, int priority = 0)

/**

** Reset user's prefix

** Arguments:

** NetUser

** Return:

** if success true, on failure false

**/

bool resetPrefix(NetUser netuser)

object getSuffix(NetUser netuser)

bool setSuffix(NetUser netuser, string suffix, int priority = 0)

bool resetSuffix(NetUser netuser)

object getDisplayName(NetUser netuser)

bool setDisplayName(NetUser netuser, string DisplayName)

bool resetDisplayName(NetUser netuser)

object getChatColor(NetUser netuser)

bool setChatColor(NetUser netuser, string chatcolor, int priority = 0)

bool resetChatColor(NetUser netuser)

bool setCustomTag(NetUser netuser, string tag)

bool resetCustomTag(NetUser netuser)

bool setListenersList(NetUser netuser, List<NetUser> list, Plugin plugin)

bool resetListenersList(NetUser netuser, Plugin plugin)

 
````