This plugin is designed to work with Economics, it shows the survivor their current balance in the lower-left corner of the interface.  The position of the GUI element is now configurable, see below for details.

**How does it work?**

The server checks for balance changes, and when a balance differs from that of the previous, ie a transaction has been made, the interface is updated.

**How do I install this?**

Place the script in your Oxide plugins directory, then restart your server for it to take effect.  There is currently no config file to alter the position of the interface element, however I'm working on this at the moment.

**Is the configuration easy?**

Yes, see the following example. The AnchorMin and AnchorMax are also nicely explained in the second attached screenshot.

````

{

  "GUIAnchorMax": "0.175 0.08",

  "GUIAnchorMin": "0.024 0.04",

  "GUIBalanceColor": "1.0 1.0 1.0 1.0", // Color of the players balance.

  "GUIBalanceSize": "12", // Font size of the players balance.

  "GUIColor": "0.1 0.1 0.1 0.75", // Color of the GUI background.

  "GUICurrency": "M", // Letter used to define the currency.

  "GUICurrencyColor": "1.0 1.0 1.0 1.0", // Color of the currency letter.

  "GUICurrencySize": "12" // Font size of the currency letter.

}

 
````


**Are there any known issues?**

None, last issue has been resolved.