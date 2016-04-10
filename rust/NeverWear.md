As requested by [@Rebel 2](http://oxidemod.org/members/39166/)


A simple plugin to stop item wear. There are 3 config options, each will disable item wear for that category of item.

**Config**

````

{

  "useAttire": false, // Stops wear on every item in the category 'Attire'

  "useTools": true, // Stops wear on every item in the category 'Tools'

  "useWeapons": false, // Stops wear on every item in the category 'Weapons'

  "useWhiteList": false, // Use the whitelisted items instead of the whole category

  "WhitelistedItems": [

    "hatchet",

    "pickaxe",

    "rifle.bolt",

    "rifle.ak"

  ]

}

 
````


**Permission**
'neverwear.use' - Required to use the whitelist
'neverwear.attire' - Required to use the category attire
'neverwear.tools' - Required to use the category tools
'neverwear.weapons' - Required to use the category weapons