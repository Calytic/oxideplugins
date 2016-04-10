**PLEASE CONSIDER DONATING TO SUPPORT DEVELOPMENT**

The store is still in early stages. All recommendation will greatly be appreciated to make it better. Sponsored by senselessgaming.com!


Since hurtworld has no way of drawing a GUI, and that hurtworlds chat window does not support scrolling, it's difficult to create a store that's both easy to use and works as flawlessly. I opted for simplicity and threw away all the hectic commands needed. Thus Items are "Categorized".
**-------------------------------------------------------------------------------
You can either use the itemid or itemname


To view the shop use**

/shop

**To view a category in a shop use**

/shop catname

**To buy an item use**

/buy item qty

**To sell an item use**

/sell item qty

**To clear store stock**

/shopclear

**To add a new item to stock**

/additem categorie item price

**To disable or enable the ability to sell**

/shop config true/false

**To change selling percentage**

/shop config sellpercent

  -To change the selling percentage. Default is 20 (20%). So if an item is $100 it will sell for 20% less at $80.

**To change pricing method**

/shop config pricing <normal/dynamic>

- Normal = Pricing will be the standard prices set.

- Dynamic = Pricing will change based upon supply and demand.

**To change pricing dilution**

/shop config dynamic <dilution> - This will make the changing of prices less abrupt, default is 50, this can be set to any number. The higher the slower prices changes.
Suggested value: 10000

**How dynamic pricing works**

(Demand / Supply) * Price

So lets say we have a Owrong for sale for $10. At the start it will sell for $10 because there are no transactions. Now someone comes along and buys 2, the price will be adjusted to (2/0)*10 = $20. (Zero will always be 1 in the rule) With that effect the selling price also moves up to make it more attractive to sell (since demand is higher than supply), so now someone comes along and sells 10, the price will be adjusted to (2/10)*10 = $2. So now again selling has adjust along as well and doesn't make sense to sell it anymore buy does make sense to buy again (Since the supply is now more than demand).


This in effect puts in effect a sort of a mini game as well, to trade commodities in the shop. Sell when price is high and buy when price is low.

**SPECIAL THANKS**

@[kpl35m](http://oxidemod.org/members/kpl35mm-nl.93043/)m-NL

For helping to beta test the plugin. You assistance is much appreciated.