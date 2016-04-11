The plugin that blocks configured console commands sent by a client to the server. This is useful for preventing users from suiciding, and more.

**Configuration**

Most of the client to server console commands for Rust Legacy should work fine, just add them in the commandblock.json file under the oxide/config directory.


Do not include any value, only the base command itself. Keep in mind that this will only work with commands that are sent to the server. Client specific commands will not be blocked, as it is never sent to the server being as it is a purely a client-side command.

**Default Config**

````
{

  "Blocked commands (needs to be lowercase)": [

    "suicide",

    "status",

    "unbanall"

  ],

  "Chat Prefix": "Oxide"

}
````