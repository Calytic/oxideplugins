Magic Signs scrapes a dynamic list of images using web requests and adds it to a list, when someone with the permission places a sign it'll load a random image from the list into the sign and then remove the URL from the list. When the list runs out, it'll scrape more.

**Commands:**

* Administrator:


/ms wipe (Clears EVERY sign from the map)

/ms tags <tags> (e.g. "Guns", type "Random" to return to the random pool) (Just typing /ms tags will show you the current tags.)

/ms aspect <aspect> (e.g. "1920x177" or "all" to show images of all aspects)

/ms sfw <sfw> (Safe for work, "All" for every image to be shown regardless)

**Permission:**

"can.ms"

````
oxide.grant user "Norn" can.ms
````


**Default Configuration:**

````

{

  "General": {

    "AuthLevel": 2

  },

  "Image": {

    "Aspect": "",

    "SafeForWork": "",

    "Tags": ""

  },

  "Messages": {

    "NoAuth": "You <color=red>don't</color> have the required authorization level to use this command.",

    "NoSigns": "There are <color=red>no</color> signs to wipe."

  }

}

 
````