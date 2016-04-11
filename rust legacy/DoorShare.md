REQUIRES: **[Share Database](http://oxidemod.org/plugins/share-database.935/) 1.0.0+**


Share Doors with friends

Check the Share Database to see how to share/unshare.



For Plugin Devs

You can access Door shares by using this hook inside your plugin:

(this is from the Share Database, but if you want to share doors via a group plugin, clan plugin, etc you can)

````
bool isSharing(string userid, string targetid)

        {

            if (Data[userid] == null) return false;

            return (Data[userid] as Dictionary<string, object>).ContainsKey(targetid);

        }
````