**Easy Chat** is a  plugin that allows you to modify your chat.

**What if player has two permission (owner_chat and vip_chat for example):**

So the plugin check which one of the two permissions is at the top in the config file.. so if  VIP is second then first one is the OWNER then the "Owner" one counts.

**Default Config:**

````

{

  "Moderator": {

    "MessageColor": "[color yellow]",

    "Permission": "mod_chat",

    "Prefix": "[MOD]"

  },

  "Owner": {

    "MessageColor": "[color red]",

    "Permission": "owner_chat",

    "Prefix": "[Owner]"

  },

  "VIP": {

    "MessageColor": "[color aqua]",

    "Permission": "vip_chat",

    "Prefix": "[VIP]"

  }

}

 
````


**How to make your own chat group and how to use the plugin:** [http://oxidemod.org/plugins/easy-chat.1115/field?field=faq](http://oxidemod.org/plugins/easy-chat.1115/field?field=faq)