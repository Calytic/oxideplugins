**Chat Commands**


* 
**/sell <item> **-- Shows the sales price
* 
**/sell <item> <amount> **-- Sell item
* 
**/buy <item> **-- Shows the purchase price
* 
**/buy <item> <amount> **-- Buy item
* 
**/buy <item>_bp <amount> **-- Buy item blueprint ( 'hachet_bp' ) (Blueprint indicator ('_bp') can be changed in cfg)
* 
**/bsl [<page>] **-- Shows a list of the prices of a specific page

P.S. All chat commands can be changed\deleted from config!

**Config**

````

    "List_Items_Per_Page": 10, -- How many items per page price list

    "Generate_Ingredients": false, -- Generate ingredients of items? It is not used anywhere, just information.

    "New_Price_List": false, -- Generate a price list when server starts?

    "Sell_Modificator": 0.5 -- How many percent of the purchase price contained in the sale price. (1 - 100%, 0.5 - 50%, 0 - 0%)

    "Blueprint_Modificator": 2 -- How many percent of the purchase price contained in the blueprint price. (2 - 200%)


    "Buy": "buy", -- Chat commands, u can change their.

    "Sell": "sell",  -- To remove a command, leave it "" (for example "Sell": "")


    "ChatFormat": "<color=#af5>[Shop]:</color> %s"  -- format for plugin's message

    "ChatPlayerIcon": true -- format for plugin's message icon (if true then it will be player's avatar, else it will be Rust icon)
````



**How to calculate a price list?**

* Reload/load the plugin and wait for the message in the console: 'Price List Generated!'
* Open your config
* Edit the basic prices => **[HOW TO](http://savepic.ru/6355345.jpg)**
* In the config you need to set the value "New_Price_List": true
* Repeat the first action
* PROFIT! Prices are calculated on the basis of the reference price.For example you put the price of a wood: 10. And if something is composed of 100 wood, the plugin will set price: 1000.
**How to edit a price list?**

* Edit the prices => **[HOW TO](http://savepic.ru/6636653.png)**
* Reload the plugin
* PROFIT!P.S. If you change the regular price in the store you do not need set "New_Price_List" to true, only when u change basic price.