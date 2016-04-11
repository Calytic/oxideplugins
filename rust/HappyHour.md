You want to give players some items in specific times?

This plugin just for it 
"Time": "600" means how many second should a Happy Hour run.Even new players will get items when they log in that time span.

"ChatTag": "Happy Hour" Chat messages will be send under that name


Times must be set with UTC time "22:00:00"
[You can check here UTC time.](http://www.timeanddate.com/worldclock/timezone/utc)


"Message": "Its time to be happy..." will be broadcast @ start of event.


Use the item names like "Stone Hatchet"


"Type": "Belt" //For belt

"Type": "Wear" // For equip

"Type": "Main" //For inventory



````
{

  "Time": "600",

  "ChatTag": "Happy Hour",

  "HappyHours": {

    "22:00:00": {

      "Message": "Its time to be happy...",

      "Items": {

        "Stone Hatchet": {

          "Amount": 1,

          "Type": "Belt"

        },

        "Building Plan": {

          "Amount": 1,

          "Type": "Belt"

        }

      }

    },

    "23:00:00": {

      "Message": "Its time to be happy...",

      "Items": {

        "Stone Hatchet": {

          "Amount": 1,

          "Type": "Belt"

        },

        "Building Plan": {

          "Amount": 1,

          "Type": "Belt"

        }

      }

    }

  }

}
````