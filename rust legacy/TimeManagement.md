Time Management:
**Commands:**

- /time => display your current time options

- /time freeze => activate / deactivate the time freeze

- /time daylength XXX => day length in seconds

- /time nightlength XXX => night length in seconds

- /time day XX => day starts at XX (default is 7 (7am))

- /time night XX => night starts at XX (defailt is 19 (7pm))

- /time start XX => the time that your server should start with (or when the plugin gets reloaded)

- /time XXX => set the current time
**Permissions:

rcon.login**

oxide permission: **cantime**

oxide permission: **all**
**Examples:**
NO NIGHT  with DAY CYCLE:

/time day 7

/time night 19

/time nightlength 1

/time daylength 3600
Time Frozen to 11am:

/time start 11

/time freeze

/time 11

You may also directly edit from the config file then reload the plugin:
**TimeManagement.json**

````
{

  "Messages: Not Allowed": "You are not allowed to use this command.",

  "Settings: Day length in seconds": 2700,

  "Settings: Day Starts At": 7,

  "Settings: Freeze Time": false,

  "Settings: Night length in seconds": 900,

  "Settings: Night Stats At": 19,

  "Settings: Start Time": 12

}
````