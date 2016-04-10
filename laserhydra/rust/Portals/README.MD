**Portals** gives you the opportunity to place portals in your world.
**Permissions (Oxide permissions):**


* 
**portals.admin **- for admin commands
* 
**portals.use** - standard permission to use any portal

**How it works:**


* Go to the place where the entrance should be
* write **/portal set Portal1 entrance** (Portal1 is an example name for a Portal)
* go to the place where the exit should be
* write **/portal set Portal1 exit**
* Go into the Config and change other details of the Portal, for example the radius, in which it teleports people.


**Chat Commands:**


* 
**/portal **(Shows available commands)
* 
**/portal list **(Shows active portals)
* 
**/portal set <PortalName> <entrance|exit|oneway> **(Set portal positions & toggle OneWay functionality)
* 
**/portal wipe **(Wipe all Portals & Backup those in Oxide/Data/PortalsBackup.json)

****Console Commands:****


* 
**portals.wipe (Wipe all Portals & Backup those in **Oxide/Data/PortalsBackup.json)


**

Config file:**


* {
  "Effects": {
    "Enabled": "true",
    "Height": "5",
    "Spacing": "0.2"
  },
  "PresetPortal": {
    "EntranceX": 0,
    "EntranceY": 0,
    "EntranceZ": 0,
    "ExitX": 0,
    "ExitY": 10,
    "ExitZ": 0,
    "OneWay": "false",
    "Radius": 5
  },
  "TeleportTimer": 15
}
 


**Future updates:**

- your suggestions