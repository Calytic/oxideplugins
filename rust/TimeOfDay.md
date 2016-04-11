This plugin allows you to change the current time as well as alter how long day and night should last.

**Features:**


* Getting information about the current time
* Changing the current time to any hour between 0 and 24
* Altering the daylength and nightlength to specified time


**Available Commands:**

````
/tod - Shows current Time Of Day.

/tod set <hour> - Sets the time to the given hour.

/tod daylength <length> - Sets the daylength in minutes.

/tod nightlength <length> - Sets the nightlength in minutes.

/tod info - Shows the current settings.

/tod help - Shows all available commands.
````


**Default Configuration:**

````
{

  "ConfigVersion": "1.0.0",

  "Settings":

  {

    "Daylength": 30,

    "Nightlength": 30

  }

}
````


**Note:** This plugin is licensed under the zlib/libpng License. By downloading you agree to these license terms. ([The zlib/libpng License (Zlib) | Open Source Initiative](https://opensource.org/licenses/Zlib)) Why? Read FAQ for information.