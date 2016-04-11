Plugin API for counting Rust days like real-life days.

**Usage**
Code (C#):
````
[PluginReference] Plugin DateCore;

Action<int> onday  =  (int day) =>
{

      Puts("A day passed!");
};

DateCore.Call("addonday", onday, "HOOKNAMEHERE");

DateCore.Call("removeonday","HOOKNAMEHERE");
````

hooks: addonday, addonweek, addonmonth, addonyear;

to remove hooks: removeonday, removeonweek, removeonmonth, removeonyear;

**Beta testings:**

Betas are avaible on: [||JMTeams Betas||](http://jmnet.servegame.com/rust/beta/)
Report errors in beta trough email not on this site!

email: [jmgamerzzz@gmail.com](mailto:jmgamerzzz@gmail.com)