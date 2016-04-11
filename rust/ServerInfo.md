**===============================================


Introduction**


This plugin allows you to create customizable UI with multiple tabs.


If you like my plugin and want to support - buy me a beer or two 
Post bugs and features here: [Trello](https://trello.com/b/Uorfyb5d/gui-help-features-requests)

**===============================================


Commands**

/info - only chat command plugin provides - it shows info.

**===============================================

Text Formatting**


Plugins creates panel with tabs that you define in a config file.

Tab consists of a header (tab name), page, and background image(s). One text line takes from button list to right edge of panel.
**"\n" does not work, create new textline instead.

"\t" works.

Any UTF8 symbol should work. You just need to put /uXXXX, where XXXX is UTF8 symbol code.**

To diplay double quote (")  in text use two single quotes (').

Formatting examples:


* <color=red>yourtext</color> - makes your text red
* <b>yourtext</b> - bold formatting
* <i>yourtext</i> - italic formatting
* <size=40>yourtext</size> - will draw your text with 40 font size

For text formatting in tab name/text lines (color, bold, italic, font size) refer to: [Unity - Manual: Rich Text](http://docs.unity3d.com/Manual/StyledText.html)


Plugin supports image in pages, check example config.
  =**===============================================

Configuration**

You may configure a plugin to show info on server join using ShowInfoOnPlayerInit option in config.


Config file is in file: oxide/config/ServerInfo.json. Previous versions of the plugin that user server_info_text.json will be upgraded automatically by the plugin. If you experience any issues with this - please post at plugin's thread.


You may trigger upgrade by changing a config option "UpgradeConfig" to true.

**To validate your config use: [JSON Formatter & Validator](https://jsonformatter.curiousconcept.com/)**

Configuration is pretty straightforward: Tabs, text, show on join.

**Global settings
**
Note about colors: all colors are formatted in RRGGBBAA format. [Wikipedia](https://en.wikipedia.org/wiki/RGBA_color_space) RRGGBB is for the color hex representation and AA is for alpha channel. Use any color picker of your choice.


* "TabToOpenByDefault" : 0 - zero based index of tab to open by default
* "ShowInfoOnPlayerInit" : true - show info window when player joins server
* "Position": {

    "MinX": 0.15,

    "MaxX": 0.9,

    "MinY": 0.2,

    "MaxY": 0.9

  }  - Position of window
* "BackgroundImage": {

    "Enabled": false,

    "Position": {

      "MinX": 0.0,

      "MaxX": 1.0,

      "MinY": 0.0,

      "MaxY": 1.0

    },

    "Url": "[http://7-themes.com/data_images/out/35/6889756-black-backgrounds.jpg](http://7-themes.com/data_images/out/35/6889756-black-backgrounds.jpg)",

    "TransparencyInPercent": 100

  } - position, url and transparency settings for background image.
* "ActiveButtonColor": "#00FFFFFF" - #RRGGBBAA color for active button
* "BackgroundColor": "#000000FF"- #RRGGBBAA color for window background
* "PrevPageButtonColor": "#7F7F7FFF",- #RRGGBBAA color for prev page button
* "NextPageButtonColor": "#7F7F7FFF",- #RRGGBBAA color for next page button
* "CloseButtonColor": "#7F7F7FFF",- #RRGGBBAA color for close button
* "InactiveButtonColor": "#7F7F7FFF",- #RRGGBBAA color for inactive button
* "HelpButton": { -- settings for UI button

      "IsEnabled": true,

      "Text": "Help",

      "Position": {

        "MinX": 0.26,

        "MaxX": 0.32,

        "MinY": 0.1,

        "MaxY": 0.14

      },

      "Color": "#7F7F7FFF",

      "FontSize": 18

  }

**per Tab settings**


* "ButtonText": "First Tab" - tab button text
* "HeaderText": "First Tab" - tab header text
* "TabButtonAnchor": 4 - alignment of text of tab button. ( values from 0 to 9)
* "OxideGroup": "" - empty string for access to everyone. Otherwise write here oxide group names like "admin,moderator,owner,vip,whatever".
* "TextAnchor": 3, - alignment of text lines of all pages in this tab.
* "TextFontSize": 16, - Default font size for tab text
* "HeaderFontSize": 32, - Header font size
* "HeaderAnchor": 0, - alignment of text of header.
* "TabButtonFontSize": 16, - Font size for tab button
* **"Pages": **- contains pages that may contain text and images****


**per Page settings**


* TextLines** - list of **lines of text that are displayed in this page. Each line - 1 row.
* ImageSettings - list of images that appear on the background of page.


Example config:

````

{

  "Tabs": [

    {

      "ButtonText": "First Tab",

      "HeaderText": "First Tab",

      "Pages": [

        {

          "TextLines": [

            "This is first tab,  \t\t 1 \u20AC \n first page.",

            "Add some text here by adding more lines.",

            "You should replace all default text lines with whatever you feel up to",

            "type <color=red> /info </color> to open this window",

            "Press next page to check second page.",

            "You may add more pages in config file."

          ],

          "ImageSettings": [

            {

              "Position": {

                "MinX": 0.0,

                "MaxX": 0.5,

                "MinY": 0.0,

                "MaxY": 0.5

              },

              "Url": "http://th04.deviantart.net/fs70/PRE/f/2012/223/4/4/rust_logo_by_furrypigdog-d5aqi3r.png",

              "TransparencyInPercent": 100

            },

            {

              "Position": {

                "MinX": 0.5,

                "MaxX": 1.0,

                "MinY": 0.0,

                "MaxY": 0.5

              },

              "Url": "http://files.enjin.com/176331/IMGS/LOGO_RUST1.fw.png",

              "TransparencyInPercent": 100

            },

            {

              "Position": {

                "MinX": 0.0,

                "MaxX": 0.5,

                "MinY": 0.5,

                "MaxY": 1.0

              },

              "Url": "http://files.enjin.com/176331/IMGS/LOGO_RUST1.fw.png",

              "TransparencyInPercent": 100

            },

            {

              "Position": {

                "MinX": 0.5,

                "MaxX": 1.0,

                "MinY": 0.5,

                "MaxY": 1.0

              },

              "Url": "http://th04.deviantart.net/fs70/PRE/f/2012/223/4/4/rust_logo_by_furrypigdog-d5aqi3r.png",

              "TransparencyInPercent": 100

            }

          ]

        },

        {

          "TextLines": [

            "This is first tab, second page",

            "Add some text here by adding more lines.",

            "You should replace all default text lines with whatever you feel up to",

            "type <color=red> /info </color> to open this window",

            "Press next page to check third page.",

            "Press prev page to go back to first page.",

            "You may add more pages in config file."

          ],

          "ImageSettings": []

        },

        {

          "TextLines": [

            "This is first tab, third page",

            "Add some text here by adding more lines.",

            "You should replace all default text lines with whatever you feel up to",

            "type <color=red> /info </color> to open this window",

            "Press prev page to go back to second page."

          ],

          "ImageSettings": []

        }

      ],

      "TabButtonAnchor": 4,

      "TabButtonFontSize": 16,

      "HeaderAnchor": 0,

      "HeaderFontSize": 32,

      "TextFontSize": 16,

      "TextAnchor": 3,

      "OxideGroup": ""

    },

    {

      "ButtonText": "Second Tab",

      "HeaderText": "Second Tab",

      "Pages": [

        {

          "TextLines": [

            "This is second tab, first page.",

            "Add some text here by adding more lines.",

            "You should replace all default text lines with whatever you feel up to",

            "type <color=red> /info </color> to open this window",

            "You may add more pages in config file."

          ],

          "ImageSettings": []

        }

      ],

      "TabButtonAnchor": 4,

      "TabButtonFontSize": 16,

      "HeaderAnchor": 0,

      "HeaderFontSize": 32,

      "TextFontSize": 16,

      "TextAnchor": 3,

      "OxideGroup": ""

    },

    {

      "ButtonText": "Third Tab",

      "HeaderText": "Third Tab",

      "Pages": [

        {

          "TextLines": [

            "This is third tab, first page.",

            "Add some text here by adding more lines.",

            "You should replace all default text lines with whatever you feel up to",

            "type <color=red> /info </color> to open this window",

            "You may add more pages in config file."

          ],

          "ImageSettings": []

        }

      ],

      "TabButtonAnchor": 4,

      "TabButtonFontSize": 16,

      "HeaderAnchor": 0,

      "HeaderFontSize": 32,

      "TextFontSize": 16,

      "TextAnchor": 3,

      "OxideGroup": ""

    }

  ],

  "ShowInfoOnPlayerInit": true,

  "TabToOpenByDefault": 0,

  "Position": {

    "MinX": 0.15,

    "MaxX": 0.9,

    "MinY": 0.2,

    "MaxY": 0.9

  },

  "BackgroundImage": {

    "Enabled": true,

    "Position": {

      "MinX": 0.0,

      "MaxX": 1.0,

      "MinY": 0.0,

      "MaxY": 1.0

    },

    "Url": "http://7-themes.com/data_images/out/35/6889756-black-backgrounds.jpg",

    "TransparencyInPercent": 100

  },

  "ActiveButtonColor": "#00FFFFFF",

  "InactiveButtonColor": "#7F7F7FFF",

  "CloseButtonColor": "#7F7F7FFF",

  "NextPageButtonColor": "#7F7F7FFF",

  "PrevPageButtonColor": "#7F7F7FFF",

  "BackgroundColor": "#000000FF"

}

 
````


**===============================================**
Examples in game:


Video from my friend, plugin appears at 3:41