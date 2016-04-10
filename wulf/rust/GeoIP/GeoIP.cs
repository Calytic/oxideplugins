using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("GeoIP", "Wulf / Luke Spragg", "0.1.3", ResourceId = 1365)]
    [Description("Provides an API to obtain IP address information from a local database.")]
    class GeoIP : RustPlugin
    {
        bool debug = false; // For development
        string auth = "?client_id=&client_secret="; // For development

        JsonSerializerSettings jsonSettings;
        DynamicConfigFile locationConfig;
        LocationData locationData;
        DynamicConfigFile blockConfig;
        BlockData blockData;
        Dictionary<string, BlockEntry> octetBlocks = new Dictionary<string, BlockEntry>();

        class LocationData
        {
            public string hash { get; set; }
            public List<LocationEntry> locations { get; set; } = new List<LocationEntry>();
        }

        class LocationEntry
        {
            public int geoname_id { get; set; }
            public string locale_code { get; set; }
            public string continent_code { get; set; }
            public string continent_name { get; set; }
            public string country_iso_code { get; set; }
            public string country_name { get; set; }
        }

        class BlockData
        {
            public string hash { get; set; } = string.Empty;
            public List<BlockEntry> ipv4 { get; set; } = new List<BlockEntry>();
        }

        class BlockEntry
        {
            public string network { get; set; }
            public int geoname_id { get; set; }
            //public int registered_country_geoname_id { get; set; }
            //public int represented_country_geoname_id { get; set; }
            //public int is_anonymous_proxy { get; set; }
            //public int is_satellite_provider { get; set; }
        }

        void Init()
        {
            jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new KeyValuesConverter());

            LoadSavedData();
            UpdateLocationData();
            UpdateBlockData();
        }

        void LoadSavedData()
        {
            locationConfig = Interface.Oxide.DataFileSystem.GetFile("GeoIP-Locations");
            locationData = locationConfig.ReadObject<LocationData>();
            blockConfig = Interface.Oxide.DataFileSystem.GetFile("GeoIP-Blocks");
            blockData = blockConfig.ReadObject<BlockData>();
        }

        void UpdateLocationData()
        {
            var check_url = "https://api.github.com/repos/lukespragg/geoip-json/contents/geoip-locations-en.json" + auth;
            var download_url = "https://raw.githubusercontent.com/lukespragg/geoip-json/master/geoip-locations-en.json";
            var headers = new Dictionary<string, string> {["User-Agent"] = "Oxide-Awesomesauce" };

            Interface.Oxide.GetLibrary<WebRequests>("WebRequests").EnqueueGet(check_url, (api_code, api_response) =>
            {
                if (api_code != 200 || api_response == null)
                {
                    Puts("Checking for locations EN update failed! (" + api_code + ")");
                    if (debug) Puts(api_response);
                    return;
                }

                var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(api_response, jsonSettings);
                string latest_sha = (string)json["sha"];
                string current_sha = locationData.hash;

                if (latest_sha == current_sha)
                {
                    Puts("Using latest locations EN data, commit: " + current_sha.Substring(0, 7));
                    return;
                }

                Puts("Updating locations EN data to commit " + latest_sha.Substring(0, 7) + "...");
                locationData.hash = latest_sha;

                Interface.Oxide.GetLibrary<WebRequests>("WebRequests").EnqueueGet(download_url, (code, response) =>
                {
                    if (code != 200 || response == null)
                    {
                        timer.Once(30f, UpdateLocationData);
                        return;
                    }

                    locationData.locations = JsonConvert.DeserializeObject<List<LocationEntry>>(response);
                    locationConfig.WriteObject(locationData);

                    Puts("Locations EN data updated successfully!");
                }, this, null, 300f);
            }, this, headers);
        }

        void CacheBlockData()
        {
            foreach (var block in blockData.ipv4)
            {
                var octets = block.network.Split('.');
                octetBlocks[octets[0] + "." + octets[1]] = block;
            }
        }

        void UpdateBlockData()
        {
            var check_url = "https://api.github.com/repos/lukespragg/geoip-json/git/trees/master" + auth;
            var download_url = "https://raw.githubusercontent.com/lukespragg/geoip-json/master/geoip-blocks-ipv4.json";
            var headers = new Dictionary<string, string> {["User-Agent"] = "Oxide-Awesomesauce" };

            Interface.Oxide.GetLibrary<WebRequests>("WebRequests").EnqueueGet(check_url, (api_code, api_response) =>
            {
                if (api_code != 200 || api_response == null)
                {
                    Puts("Checking for blocks IPv4 update failed! (" + api_code + ")");
                    if (debug) Puts(api_response);
                    return;
                }

                var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(api_response, jsonSettings);
                string latest_sha = string.Empty;
                string current_sha = blockData.hash;

                foreach (Dictionary<string, object> file in json["tree"] as List<object>)
                    if ((string)file["path"] == "geoip-blocks-ipv4.json")
                        latest_sha = file["sha"].ToString();

                if (latest_sha == current_sha)
                {
                    CacheBlockData();
                    Puts("Using latest blocks IPv4 data, commit: " + current_sha.Substring(0, 7));
                    return;
                }

                Puts("Updating blocks IPv4 data to commit " + latest_sha.Substring(0, 7) + "...");
                blockData.hash = latest_sha;

                Interface.Oxide.GetLibrary<WebRequests>("WebRequests").EnqueueGet(download_url, (code, response) =>
                {
                    if (code != 200 || response == null) timer.Once(30f, UpdateBlockData);

                    blockData.ipv4 = JsonConvert.DeserializeObject<List<BlockEntry>>(response);
                    blockConfig.WriteObject(blockData);
                    CacheBlockData();

                    Puts("Blocks IPv4 data updated successfully!");
                }, this, headers, 300f);
            }, this, headers);
        }

        LocationEntry GetLocationData(string ip)
        {
            string[] values = ip.Split('.');

            BlockEntry block;
            if (octetBlocks.TryGetValue(values[0] + "." + values[1], out block))
            {
                if (block == null)
                {
                    Puts("Blocks data is not loaded yet!");
                    return null;
                }

                foreach (var location in locationData.locations)
                    if (location.geoname_id == block.geoname_id)
                        return location;
            }
            return null;
        }

        string GetCountry(string ip)
        {
            var location = GetLocationData(ip);
            if (location != null)
            {
                string country_name = location.country_name;
                if (debug) Puts("Country name for IP " + ip + " is " + country_name);
                return country_name;
            }
            return "Unknown";
        }

        string GetCountryCode(string ip)
        {
            var location = GetLocationData(ip);
            if (location != null)
            {
                string country_iso_code = location.country_iso_code;
                if (debug) Puts("Country code for IP " + ip + " is " + country_iso_code);
                return country_iso_code;
            }
            return "Unknown";
        }

        string GetContinent(string ip)
        {
            var location = GetLocationData(ip);
            if (location != null)
            {
                string continent_name = location.continent_name;
                if (debug) Puts("Continent name for IP " + ip + " is " + continent_name);
                return continent_name;
            }
            return "Unknown";
        }

        string GetContinentCode(string ip)
        {
            var location = GetLocationData(ip);
            if (location != null)
            {
                string continent_code = location.continent_code;
                if (debug) Puts("Continent code for IP " + ip + " is " + continent_code);
                return continent_code;
            }
            return "Unknown";
        }

        string GetLocale(string ip)
        {
            var location = GetLocationData(ip);
            if (location != null)
            {
                string locale_code = location.locale_code;
                if (debug) Puts("Locale for IP " + ip + " is " + locale_code);
                return locale_code;
            }
            return "Unknown";
        }
    }
}
