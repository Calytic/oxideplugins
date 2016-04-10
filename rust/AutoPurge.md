Removes entities created by inactive players using EntityOwner & ConnectionDB. Obviously this won't perform well if you're installing either EntityOwner or ConnectionDB at the same time as AutoPurge. They need time to cache data.


If you already have EntityOwner and ConnectionDB installed this will work straight away.

**Console Commands:**

autopurge.run (Bypass timer and call hook directly)

**Permissions:**

autopurge.run


(By default inactive after 2 days, timer runs every 6 hours).
**Default Configuration:**

````
{

  "General": {

    "InactiveAfter": 172800,

    "MainTimer": 21600,

    "Messages": true

  }

}
````