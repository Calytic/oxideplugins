**Description**

Tiny and simple plugin to show players their coordinates.

**Usage**

/location or /loc

**Default Config**

````
{

  "Settings": {

    "ChatCommands": [

      "loc",

      "location"

    ],

    "Precision": "0"

  },

  "Messages": {

    "HelpText": "use /location or /loc to see your current location",

    "Location": "Current location x: {x} y: {y} z: {z}"

  }

}
````

ChatCommands - config the chat commands used

Precision - decimal places displayed in coordinates. "0" means 0 digits after comma, "1" means 1 digit after comma, "2" 2 digits after comma ...


Location message - you can use {x}, {y} and {z} as wildcards for the coordinates