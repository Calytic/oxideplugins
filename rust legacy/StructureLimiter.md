**Features:**

- Limit structure elements by USERS

- Set custom elements for some users

- Faster then ever 
- Warn players when they dont have a lot of elements allowed left

**Player Commands:**

- /structurelimiter => checks how many elements you've used, and how much you have left
**Admin Commands:**

- /structurelimiter PLAYER/STEAMID => checks how many elements a user used, and how much he has left

- /structurelimiter PLAYER/STEAMID NUMBER => sets a new max elements allowed for this user.
**For plugin devs:**

Get the number of allowed entities for a user

````
int GetAllowedEntities(string userid)
````

Set the number of allowed entities for a user

````
GetAllowedEntities(string userid, int newnumber)
````

Add allowed entities for a user

````
GetAllowedEntities(string userid, int addnumber)
````