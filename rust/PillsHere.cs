using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Pills Here", "e.70033", "2.0", ResourceId = 1723)]
    [Description("Take pills, heal up")]
    class PillsHere : RustPlugin
    {

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["PillsHealAmount"] = 20;
            Config["PillsHungerAmount"] = 0;
            Config["PillsThirstAmount"] = 0;
            SaveConfig();
        }

        private float healAmount => Config.Get<float>("PillsHealAmount");
        private float hungerAmount => Config.Get<float>("PillsHungerAmount");
        private float thirstAmount => Config.Get<float>("PillsThirstAmount");

        void OnConsumableUse(Item item)
        {

            BasePlayer player = item.GetOwnerPlayer();
            if (item.info.itemid == 1685058759)
            {
                player.Heal(healAmount);
                player.metabolism.calories.value += hungerAmount;
                player.metabolism.hydration.value += thirstAmount;
            }
        }

        [ConsoleCommand("pills")]
        void SetValueCommand(ConsoleSystem.Arg arg)
        {
            var args = arg.Args;
            var h = "heal";
            string hu = "hunger";
            string t = "thirst";
            float tmpVal;
            if (args.Length == 2)
            {
                if (float.TryParse(args[1], out tmpVal))
                {
                    if (args[0].Equals(h))//works, just set the value.
                    {
                        Config["PillsHealAmount"] = tmpVal;
                        SaveConfig();
                        Puts("Healing amount had been set to: " + tmpVal + "\n\tSaving config...\tSaved!");
                    }
                    else if (args[0].Equals(hu))
                    {
                        Config["PillsHungerAmount"] = tmpVal;
                        SaveConfig();
                        Puts("Hunger amount had been set to: " + tmpVal + "\n\tSaving config...\tSaved!");
                    }
                    else if (args[0].Equals(t))
                    {
                        Config["PillsThirstAmount"] = tmpVal;
                        SaveConfig();
                        Puts("Thirst amount had been set to: " + tmpVal + "\n\tSaving config...\tSaved!");
                    }
                }
            }
            else
            {
                Puts("\nTo change the effect value of anti-rad pills, use the follow console command:\n\tpills <TYPE> <ammount>\t\tType: " + h + " | " + hu + " | " + t +"\n\tCurrent values are:\tHeal: "+healAmount+" | Hunger: " +hungerAmount+" | Thirst: "+thirstAmount);
            }
        }
    }
}
