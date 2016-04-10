All credit for this plugin goes to [@4seti](http://oxidemod.org/members/23924/) , If you like his work consider donating to him [HERE](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=843Z3T75ZZWVG)
**
Why you may need this plugin:**

Investigation of "stolen" goods cases 
Player tell you want goods was stolen, you come in his house, point at box where goods was, and just type **/**box****, you will see list of looters for this box, if there is anyone who doesn't live in this house (just ask player about) - this is your cheater.

**Config**

````
{

  "Options - Amount of hours before removing an entry": 48,

  "Options - Data save timer (seconds)": 600,

  "Options - Detect radius": 15.0

}
````


**Commands:

/box** - Check the StorageContainer you are looking at.
**/**boxc**lear** - Force cleanup attempt of data
**/**box**save** - Force data save.
**/**box**rad** - Find a StorageConatiner in the radius set in the config.
**/boxpname "playername"** - Show looting information about 'playername'

**Permissions:**
boxlooters.checkbox - Grants player access to /box, /boxpname, /boxrad chat commands