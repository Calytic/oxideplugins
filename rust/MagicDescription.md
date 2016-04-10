This plugin hijacks the "server.description" command and adds data onto the end of it, etc plugin list.



**What does it currently show?**

* Dynamic Plugin List


To-do:

* Custom variables

**Console Commands:**

````
plugins.refresh
````

 (Wipes the plugins list and regenerates it (So if you've removed a plugin in the data file, it will be re-added).

**What if I don't want to show a specific plugin?**

All plugins are grabbed upon first load (As in VERY first), and stored in MagicDescription.json in /Data/. If you don't wish to display a particular plugin, delete it from the data file and reload.


Default Configuration:

````
{

  "Description": "[Change Me]",

  "Refresh": true

}
````