**DISCONTINUED**
**REPLACED BY: **[http://oxidemod.org/plugins/moegicbox.1296/](http://oxidemod.org/plugins/moegicbox.1296/)


If you are a plugin developer and would would like to continue supporting this plugin, contact me or Wulf. MoegicBox has roughly the same functionality (minus the recursiveness) and I will be supporting MoegicBox in the future and not working on this version anymore.




This plugin allows players to convert a **Repair Bench** into a **Salvage Bench **and use it to bring items back to their **base materials**.

**What you need to know:**

· **/salvager **gives usage and price information

· **/salvager buy **turns "buy mode" on, simply open a Repair Bench to complete the operation

· **/salvager remove **turns "remove mode" on, simply open a Salvage Bench to complete the operation

· The remove operation simply converts the **Salvage Bench **back to a normal **Repair Bench**

· There is currently **no authority or cupboard checks in place** (next version will) so you might want to keep the price free for now (to be clear, any player can buy/remove any repair bench regardless of who placed it or whether he has cupboard access or not)

· The config contains a field for the price, **format needs to be respected**, see default config for the proper format (an empty string should make it free but **I honestly didn't test**)

· The config contains a refund ratio (default 0.5 for 50% of mats)

· Items with **zero condition** **will not be recycled **and an error message will be shown. **Item is left in the bench, allowing repair**.

· If an ingredient is also salvageable, **the plugin will salvage recursively**

· The condition ratio of the item affects the materials returned (also applies to ingredients that have condition)

· The plugin **cannot salvage processed items** (cannot turn metal fragments into metal ore for instance)

**Default config:**

````
{

  "Salvager": {

    "Price": "1000 wood + 5000 metal_fragments",

    "Refund Ratio": 0.5,

    "Salvagers": {}

  }

}
````

Special thanks to **@[HBros]Moe** for suggesting this plugin and supporting its development