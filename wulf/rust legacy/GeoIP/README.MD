**GeoIP**  provides an API to obtain IP address information from a local database. The plugin downloads and uses a [JSON-formatted MaxMind GeoLite2 database](https://github.com/lukespragg/geoip-json) locally, so no web requests are needed beyond the database download and updates.


Keep in mind that alone this does nothing, as it's an API meant for plugins to utilize. To use this with a supported plugin, simply install like you would any other plugin!


GeoLite2 databases are free IP geolocation databases comparable to, but less accurate than, MaxMind’s GeoIP2 databases. GeoLite2 databases are updated on the first Tuesday of each month. The GeoLite2 databases are distributed under the [Creative Commons Attribution-ShareAlike 3.0 Unported License](http://creativecommons.org/licenses/by-sa/3.0/).

**Plugin Developers** 

To call the functions from this API, your plugin needs to get the plugin instance.
Code (C):
````
[PluginReference]

Plugin GeoIP;
````

Code (Lua):
````
local geoIp = plugins.Find("GeoIP")
````

You can then use the function you'd like to get information for the specified IP.
Code (C):
````
GeoIP?.Call("GetCountry", "8.8.8.8");
````

Code (Lua):
````
geoIp:CallHook("GetCountry", "8.8.8.8")
````

**Functions:**  GetCountry(string ip), GetCountryCode(string ip), GetContinent(string ip), GetContinentCode(string ip), GetLocale(string ip).

**Example Plugins** 
Code (C):
````
using Oxide.Core.Plugins;


namespace Oxide.Plugins
{

    [Info("GeoIP Test", "Wulf / Luke Spragg", 0.1)]

    [Description("GeoIP test plugin.")]

    class GeoIPTest : RustLegacyPlugin

    {

        [PluginReference]

        Plugin GeoIP;


        void Loaded()

        {

            if (!GeoIP)

            {

                Puts("GeoIP is not loaded! http://oxidemod.org/plugins/1366/");

                return;

            }

            var country = GeoIP.Call("GetCountry", "8.8.8.8");

            Puts("Country name for IP 8.8.8.8 is " + country);

        }

    }
}
````

Code (Lua):
````
PLUGIN.Title = "GeoIP Test"

PLUGIN.Version = V(0, 1, 0)

PLUGIN.Description = "GeoIP test plugin."

PLUGIN.Author = "Wulf / Luke Spragg"

function PLUGIN:Init()

    local geoIp = plugins.Find("GeoIP")

    if not geoIp then print("GeoIP is not loaded! http://oxidemod.org/plugins/1366/") return end

    local country = geoIp:CallHook("GetCountry", "8.8.8.8")

    print("Country name for IP 8.8.8.8 is " .. country)
end
````

This product includes GeoLite2 data created by MaxMind, available from [http://www.maxmind.com](http://www.maxmind.com/).