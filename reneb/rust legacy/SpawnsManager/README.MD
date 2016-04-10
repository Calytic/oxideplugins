Allows modification of the spawnpoints table (this will hard code inside rust itself, so no more teleportations needed)

**Commands:**

- /spawnsmanager FILENAME => load the filename with all the spawns
**How to use**

* Once the plugin is installed, launch or restart the server.
* Inside your server's oxide config directory (see Installing plugins on your Legacy server | Oxide), the file "SpawnsManager.json" should have been created. Download it.
* This file contains the default spawns tables included within rust, in JSON format.** Keep a copy of this file somewhere**, it's a helpful reference!
* Install the Spawns Database Plugin 
Spawns Database 
* Follow the instructions inside Spawns Database on how to make new spawns, remember to do /spawns_save FILENAME
* Load the new spawns via /spawnsmanager FILENAME
**Notes**


* If you have an error in your loot tables file, **the plugin may not display an error and simply just load the default spawns tables.** Always check your customised loot tables in a validator (JSONLint - The JSON Validator.) before uploading!


**via Spawns Database:**


**Manually:

Positions:**

positions are in default Vector3 positions (so normal coordinates)

You may use any map to help you get the locations (ex: [Rust Map Marks - Interactive Map Tool for Rust](http://rustmapmarks.com/) ), but you will need to teleport to the points to know the HEIGHT (y value) so you don't set spawn points under the map

I recommend the Location plugin : [Location for Rust Legacy | Oxide](http://oxidemod.org/plugins/location.937/)

**Rotations:**

They are Quaternion rotations, if you don't know what you are doing just use w: 1 x:0 y:0 z:0

This is a good exemple of how it works: [Maths - Euler to Quaternion Examples 90 degree steps - Martin Baker](http://www.euclideanspace.com/maths/geometry/rotations/conversions/eulerToQuaternion/steps/index.htm)


So yes it's a lot of work, but you just have to do it once 

**AND REMEMBER KEEP A COPY OF YOUR ORIGINAL SPAWN POINTS (or use the ones from here)**

**
````
{

  "Spawns List": [

    {

      "pos_x": "5531.182",

      "pos_y": "411.2128",

      "pos_z": "-3684.792",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5837.283",

      "pos_y": "392.8771",

      "pos_z": "-3494.182",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "4905.67",

      "pos_y": "393.4428",

      "pos_z": "-4769.813",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6775.949",

      "pos_y": "352.3508",

      "pos_z": "-3682.972",

      "rot_w": "0.5671433",

      "rot_x": "0",

      "rot_y": "-0.8236192",

      "rot_z": "0"

    },

    {

      "pos_x": "5976.265",

      "pos_y": "382.7106",

      "pos_z": "-3596.637",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6790.05",

      "pos_y": "327.3807",

      "pos_z": "-4452.243",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5740.514",

      "pos_y": "297.3926",

      "pos_z": "-5257.481",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6850.896",

      "pos_y": "366.9601",

      "pos_z": "-2994.972",

      "rot_w": "0.7150749",

      "rot_x": "0",

      "rot_y": "0.6990479",

      "rot_z": "0"

    },

    {

      "pos_x": "4671.583",

      "pos_y": "440.1155",

      "pos_z": "-3826.446",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6562.437",

      "pos_y": "356.3081",

      "pos_z": "-3883.619",

      "rot_w": "0.7150747",

      "rot_x": "0",

      "rot_y": "0.699048",

      "rot_z": "0"

    },

    {

      "pos_x": "6315.311",

      "pos_y": "358.7458",

      "pos_z": "-4646.003",

      "rot_w": "-0.006586382",

      "rot_x": "0",

      "rot_y": "0.9999783",

      "rot_z": "0"

    },

    {

      "pos_x": "6500.149",

      "pos_y": "363.6516",

      "pos_z": "-4247.492",

      "rot_w": "0.3462204",

      "rot_x": "0",

      "rot_y": "0.9381533",

      "rot_z": "0"

    },

    {

      "pos_x": "5565.772",

      "pos_y": "338.5708",

      "pos_z": "-5054.916",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "4671.034",

      "pos_y": "439.2345",

      "pos_z": "-4560.056",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6057.834",

      "pos_y": "386.8134",

      "pos_z": "-4297.3",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6735.126",

      "pos_y": "363.2726",

      "pos_z": "-3325.363",

      "rot_w": "0.7150749",

      "rot_x": "0",

      "rot_y": "0.6990479",

      "rot_z": "0"

    },

    {

      "pos_x": "4681.396",

      "pos_y": "439.5459",

      "pos_z": "-4045.794",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "7005.118",

      "pos_y": "339.8353",

      "pos_z": "-3994.495",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5266.553",

      "pos_y": "340.9123",

      "pos_z": "-5298.712",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5261.042",

      "pos_y": "403.7686",

      "pos_z": "-3968.376",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5549.888",

      "pos_y": "328.6223",

      "pos_z": "-5418.906",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6131.337",

      "pos_y": "390.9005",

      "pos_z": "-3859.77",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6884.638",

      "pos_y": "340.5172",

      "pos_z": "-4036.02",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6678.809",

      "pos_y": "372.3412",

      "pos_z": "-4587.396",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5584.592",

      "pos_y": "384.4573",

      "pos_z": "-4404.155",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6915.186",

      "pos_y": "330.3829",

      "pos_z": "-4121.668",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6196.602",

      "pos_y": "345.5115",

      "pos_z": "-4754.339",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6402.392",

      "pos_y": "357.4748",

      "pos_z": "-4434.293",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6455.994",

      "pos_y": "371.1599",

      "pos_z": "-4127.518",

      "rot_w": "0.5309135",

      "rot_x": "0",

      "rot_y": "0.8474261",

      "rot_z": "0"

    },

    {

      "pos_x": "6472.923",

      "pos_y": "359.9763",

      "pos_z": "-4699.128",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6470.615",

      "pos_y": "372.7591",

      "pos_z": "-3450.401",

      "rot_w": "0.7150749",

      "rot_x": "0",

      "rot_y": "0.6990479",

      "rot_z": "0"

    },

    {

      "pos_x": "5447.94",

      "pos_y": "347.6385",

      "pos_z": "-4941.132",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "4983.095",

      "pos_y": "370.3271",

      "pos_z": "-5061.488",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "4577.002",

      "pos_y": "429.5138",

      "pos_z": "-4756.197",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5276.897",

      "pos_y": "401.2099",

      "pos_z": "-4133.221",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "4644.853",

      "pos_y": "378.2797",

      "pos_z": "-5142.231",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5548.247",

      "pos_y": "365.5267",

      "pos_z": "-4714.467",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5441.314",

      "pos_y": "391.442",

      "pos_z": "-4241.093",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6002.927",

      "pos_y": "398.1774",

      "pos_z": "-3854.451",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6963.688",

      "pos_y": "351.7164",

      "pos_z": "-3677.03",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6242.193",

      "pos_y": "377.7453",

      "pos_z": "-3436.96",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6960.122",

      "pos_y": "348.0456",

      "pos_z": "-3801.577",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6912.684",

      "pos_y": "360.6867",

      "pos_z": "-3185.461",

      "rot_w": "0.7150749",

      "rot_x": "0",

      "rot_y": "0.6990479",

      "rot_z": "0"

    },

    {

      "pos_x": "6153.295",

      "pos_y": "375.7242",

      "pos_z": "-4093.817",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6271.871",

      "pos_y": "358.0141",

      "pos_z": "-4642.436",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "4713.295",

      "pos_y": "400.9944",

      "pos_z": "-4876.156",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5087.345",

      "pos_y": "414.0234",

      "pos_z": "-3886.334",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6065.788",

      "pos_y": "385.3451",

      "pos_z": "-3800.052",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5848.806",

      "pos_y": "384.2297",

      "pos_z": "-3661.133",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6212.962",

      "pos_y": "353.2497",

      "pos_z": "-4684.625",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6686.377",

      "pos_y": "352.2229",

      "pos_z": "-4225.295",

      "rot_w": "0.9278761",

      "rot_x": "0",

      "rot_y": "-0.372889",

      "rot_z": "0"

    },

    {

      "pos_x": "4929.744",

      "pos_y": "415.0445",

      "pos_z": "-3800.212",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6521.322",

      "pos_y": "358.9024",

      "pos_z": "-3632.243",

      "rot_w": "0.7150749",

      "rot_x": "0",

      "rot_y": "0.6990479",

      "rot_z": "0"

    },

    {

      "pos_x": "6490.945",

      "pos_y": "365.7235",

      "pos_z": "-4376.725",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5885.447",

      "pos_y": "388.6733",

      "pos_z": "-4062.263",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "4800.936",

      "pos_y": "360.8081",

      "pos_z": "-5218.493",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6221.51",

      "pos_y": "401.7562",

      "pos_z": "-4292.377",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6898.551",

      "pos_y": "352.411",

      "pos_z": "-3883.529",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6208.185",

      "pos_y": "352.5298",

      "pos_z": "-4478.465",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "4345.299",

      "pos_y": "497.813",

      "pos_z": "-3913.293",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "6609.746",

      "pos_y": "406.7775",

      "pos_z": "-4735.205",

      "rot_w": "0.1534805",

      "rot_x": "0",

      "rot_y": "0.9881517",

      "rot_z": "0"

    },

    {

      "pos_x": "6913.629",

      "pos_y": "322.515",

      "pos_z": "-4355.763",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    },

    {

      "pos_x": "5579.97",

      "pos_y": "411.3439",

      "pos_z": "-3448.924",

      "rot_w": "1",

      "rot_x": "0",

      "rot_y": "0",

      "rot_z": "0"

    }

  ]

}
````

**