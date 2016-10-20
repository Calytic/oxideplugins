using System;

namespace Oxide.Plugins
{
    [Info("Everlight", ".legaCypowers", "1.0.0")]
    [Description("Disable the consumption of fuel on light items like Lantern")]

    class Everlight : RustPlugin
    {

        void Init()
        {
            Puts("Everlight by .legaCypowers");
        }

        void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
		{
			if(oven.panelName == "Lantern"){
				fuel.amount++;
				
			}
		}

    }
}