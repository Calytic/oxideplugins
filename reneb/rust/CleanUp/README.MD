Clean up your server.


**Commands:**
- /clean "DEPLOYABLE NAME" all => destroy all deployables that have this name

- /clean "DEPLOYABLE NAME" world optional:RADIUS => destroy all deployables that are not in a XX meters radius of a building element (default is 3)
- /count "DEPLOYABLE NAME" all => count all deployables that have this name

- /count "DEPLOYABLE NAME" world optional:RADIUS => count all deployables that are not in a XX meters radius of a building element (default is 3)


- /clean deployables all => destroy ALL deployables !!!

- /clean deployables world XX => destroy all deployables that are not in a XX meters of a building

(works also with /count)

**Permissions:**

- moderatorid & ownerid (In configs: authLevel: 1 = moderators + owners, 2 = owners only)

- Oxide permission: "canclean"


Don't have time to do more, but this should help a lot with all the barricade spams and stuff like that


I'll try to add radius, and other features later on.


Notes:

- via tool cupboards