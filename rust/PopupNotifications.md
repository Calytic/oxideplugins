[**Donate here**](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=NQYRAPDV676MY)


This plugin adds customizable UI notifications. Admins and other plugins can create notifications.


**

Admin commands**
/popupmsg "message" --- Creates a notification for everyone on the server.
/popupmsg "player name" "message" --- Creates a notification for a player.

**Console commands**
popupmsg.global "message" duration --- Creates a notification for everyone on the server. Duration is optional.
popupmsg.toplayer "message" "player name" duration --- Creates a notification for a player. Duration is optional.


The old commands still work.

**Config**

````
{

  "DefaultShowDuration": 8.0, //how long the notifications stay (seconds) by default

  "FadeTime": 1.0, //how long does it take (seconds) for the notification to disappear when destroyed, 0 = instant

  "Height": 0.1, //notification panel height

  "MaxShownMessages": 5, //max amount of panels on screen at a time

  "PositionX": 0.8, //x position of the first panel's lower left corner

  "PositionY": 0.78, //y position of the first panel's lower left corner

  "ScrollDown": true, //set to false if you want new panels to appear over the first one

  "Spacing": 0.01, //space between panels

  "Transparency": 0.7, //notification transparency, 0 = invisible, 1 = fully visible

  "Width": 0.19 //notification panel width

}
````


**For plugin developers**

To call the functions from this API your plugin needs to get the plugin instance.
Code (C):
````
[PluginReference]

Plugin PopupNotifications;
````

Code (Lua):
````
local popupApi = plugins.Find("PopupNotifications")
````

You can then use this to create notifications using the CreatePopupNotification method.
Code (C):
````
PopupNotifications?.Call("CreatePopupNotification", "Test message");
````

Code (Lua):
````
popupApi:CallHook("CreatePopupNotification", "Test message")
````


**Example**
Code (C):
````
using Oxide.Core.Plugins;


namespace Oxide.Plugins
{

    [Info("Popup Test", "emu", 0.0.1)]

    class PopupTest : RustPlugin

    {

        [PluginReference]

        Plugin PopupNotifications;


        void Loaded()

        {

            if (!PopupNotifications)

            {

                Puts("PopupNotifications is not loaded! http://oxidemod.org/plugins/popup-notifications.1252/");

                return;

            }

            PopupNotifications?.Call("CreatePopupNotification", "Test message");

        }

    }
}
````

Code (Lua):
````
PLUGIN.Title = "Popup Test"

PLUGIN.Version = V(0, 0, 1)

PLUGIN.Author = "emu"

function PLUGIN:Init()

    local popupApi = plugins.Find("PopupNotifications")

    if not popupApi then print("PopupNotifications is not loaded! http://oxidemod.org/plugins/popup-notifications.1252/") return end

    popupApi:CallHook("CreatePopupNotification", "Test message")
end
````

Code (Python):
````
class popuptest:


    def __init__(self):


        self.Title = "Popup Test"

        self.Author = "emu"

        self.Version = V(0, 0, 1)


    def Init(self):


        PopupAPI = plugins.Find("PopupNotifications")


        if not PopupAPI:

            print("PopupNotifications is not loaded! http://oxidemod.org/plugins/popup-notifications.1252/")

            return


        PopupAPI.CallHook("CreatePopupNotification", "Test Message")
````