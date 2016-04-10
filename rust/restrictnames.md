This plugin will reject connections of undesired names and characters.
**Default Config:**

````
{

  "RestrictedNames": [

    "SERVER CONSOLE",

    "SERVER",

    "Oxide"

  ],

  "IgnoreModerators": true,

  "useRestrictName": true,

  "useRestrictCharacters": true,

  "AllowedCharacters": "abcdefghijklmnopqrstuvwxyz1234567890 [](){}!@#$%^&*_-=+.|"

}
````

IgnoreModerators => Will ignore all moderators that are level 1 (moderator) or 2 (owner)

useRestrictName => activate the name restrictor

useRestrictCharacters => activate the character restrictor

AllowedCharacters => place in one line all the allowed characters that you want on your server.

RestrictedNames => place here all the names that you want to restrict
**Hints:**

Validate your json config file here => [http://jsonlint.com](http://jsonlint.com)