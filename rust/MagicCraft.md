**What is Magic Craft?**

Magic Craft is an alternative crafting system (or lack thereof), it's a way to bypass the traditional system of crafting on specific items.

**What's the point of that you ask?**

Well for a start, gunpowder can actually be insta and none of the ui lag that I've seen people complaining about for example. That's just one example.

**Can I use this at the same time as other crafting plugins such as "Crafting Controller"?**

Yes, indeed you can.



**Permissions:**

````

MagicCraft.able

grant group player "MagicCraft.able"

grant user "Norn" "MagicCraft.able"

 
````


**Default Configuration:**

````
{

  "Protocol": 1320,

  "UsePopupNotifications": false,

  "MessagesEnabled": true

}
````


**How to use:**

Navigate to /data/MagicCraft.json and search for whatever item you are modifying the crafting/bulk crafting on.


Here is what an item in the data file will look like, in this case I'm using gunpowder as an example.


````
"gunpowder": {

      "MaxBulkCraft": 999,

      "MinBulkCraft": 1,

      "displayName": "Gun Powder",

      "shortName": "gunpowder",

      "description": "Made from Sulphur and Charcoal.",

      "Enabled": false,

      "Cooldown": 0

    },
````


**MaxBulkCraft = **If amount crafted is over this, then MagicCraft will not take effect.
**MinBulkCraft = **If amount crafted is below this, then MagicCraft will not take effect.
**Enabled = **true/false (false by default)