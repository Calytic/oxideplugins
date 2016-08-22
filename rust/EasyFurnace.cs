using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("EasyFurnace", "oskar3123", "1.1.3", ResourceId = 1191)]
    class EasyFurnace : RustPlugin
    {
        class Cfg
        {
            public static double
                burntime_wood,
                cooktime_hqmetal,
                cooktime_metal,
                cooktime_sulfur;
            public static int
                furnaceMetalOres,
                furnaceMetalWood,
                furnaceMetalOutput,
                furnaceSulfurOres,
                furnaceSulfurWood,
                furnaceSulfurOutput,
                furnaceHQMetalOres,
                furnaceHQMetalWood,
                furnaceHQMetalOutput,
                largeFurnaceMetalOres,
                largeFurnaceMetalWood,
                largeFurnaceMetalOutput,
                largeFurnaceSulfurOres,
                largeFurnaceSulfurWood,
                largeFurnaceSulfurOutput,
                largeFurnaceHQMetalOres,
                largeFurnaceHQMetalWood,
                largeFurnaceHQMetalOutput;
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            Config.Clear();
            Config["Furnace", "Metal", "Ores"] = 4;
            Config["Furnace", "Metal", "Wood"] = 1;
            Config["Furnace", "Metal", "Output"] = 1;
            Config["Furnace", "Sulfur", "Ores"] = 4;
            Config["Furnace", "Sulfur", "Wood"] = 1;
            Config["Furnace", "Sulfur", "Output"] = 1;
            Config["Furnace", "HQMetal", "Ores"] = 3;
            Config["Furnace", "HQMetal", "Wood"] = 2;
            Config["Furnace", "HQMetal", "Output"] = 1;
            Config["LargeFurnace", "Metal", "Ores"] = 12;
            Config["LargeFurnace", "Metal", "Wood"] = 5;
            Config["LargeFurnace", "Metal", "Output"] = 1;
            Config["LargeFurnace", "Sulfur", "Ores"] = 12;
            Config["LargeFurnace", "Sulfur", "Wood"] = 3;
            Config["LargeFurnace", "Sulfur", "Output"] = 3;
            Config["LargeFurnace", "HQMetal", "Ores"] = 7;
            Config["LargeFurnace", "HQMetal", "Wood"] = 10;
            Config["LargeFurnace", "HQMetal", "Output"] = 1;
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Cfg.furnaceMetalOres = (int)Config["Furnace", "Metal", "Ores"];
            Cfg.furnaceMetalWood = (int)Config["Furnace", "Metal", "Wood"];
            Cfg.furnaceMetalOutput = (int)Config["Furnace", "Metal", "Output"];
            Cfg.furnaceSulfurOres = (int)Config["Furnace", "Sulfur", "Ores"];
            Cfg.furnaceSulfurWood = (int)Config["Furnace", "Sulfur", "Wood"];
            Cfg.furnaceSulfurOutput = (int)Config["Furnace", "Sulfur", "Output"];
            Cfg.furnaceHQMetalOres = (int)Config["Furnace", "HQMetal", "Ores"];
            Cfg.furnaceHQMetalWood = (int)Config["Furnace", "HQMetal", "Wood"];
            Cfg.furnaceHQMetalOutput = (int)Config["Furnace", "HQMetal", "Output"];
            Cfg.largeFurnaceMetalOres = (int)Config["LargeFurnace", "Metal", "Ores"];
            Cfg.largeFurnaceMetalWood = (int)Config["LargeFurnace", "Metal", "Wood"];
            Cfg.largeFurnaceMetalOutput = (int)Config["LargeFurnace", "Metal", "Output"];
            Cfg.largeFurnaceSulfurOres = (int)Config["LargeFurnace", "Sulfur", "Ores"];
            Cfg.largeFurnaceSulfurWood = (int)Config["LargeFurnace", "Sulfur", "Wood"];
            Cfg.largeFurnaceSulfurOutput = (int)Config["LargeFurnace", "Sulfur", "Output"];
            Cfg.largeFurnaceHQMetalOres = (int)Config["LargeFurnace", "HQMetal", "Ores"];
            Cfg.largeFurnaceHQMetalWood = (int)Config["LargeFurnace", "HQMetal", "Wood"];
            Cfg.largeFurnaceHQMetalOutput = (int)Config["LargeFurnace", "HQMetal", "Output"];

            Cfg.burntime_wood = GetBurntime("wood");
            Cfg.cooktime_hqmetal = GetCooktime("hq.metal.ore");
            Cfg.cooktime_metal = GetCooktime("metal.ore");
            Cfg.cooktime_sulfur = GetCooktime("sulfur.ore");
        }

        double GetBurntime(ItemDefinition item)
        {
            foreach (ItemMod mod in item.itemMods)
            {
                if (!(mod is ItemModBurnable)) continue;
                ItemModBurnable burnable = mod as ItemModBurnable;
                return burnable.fuelAmount / 5;
            }
            return 0D;
        }
        double GetBurntime(string shortname)
        {
            return GetBurntime(ItemManager.FindItemDefinition(shortname));
        }

        double GetCooktime(ItemDefinition item)
        {
            foreach (ItemMod mod in item.itemMods)
            {
                if (!(mod is ItemModCookable)) continue;
                ItemModCookable cookable = mod as ItemModCookable;
                return cookable.cookTime;
            }
            return 0D;
        }
        double GetCooktime(string shortname)
        {
            return GetCooktime(ItemManager.FindItemDefinition(shortname));
        }

        int GetStackSize(string shortname) { return ItemManager.FindItemDefinition(shortname).stackable; }
        int GetStackSize(ItemDefinition item) { return item.stackable; }
        
        Dictionary<BaseOven, BasePlayer> furnaceCache = new Dictionary<BaseOven, BasePlayer>();

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            BaseOven furnace = entity as BaseOven;
            if (!furnace) return;

            furnaceCache[furnace] = player;
        }

        int RemoveItemsFromInventory(BasePlayer player, ItemDefinition itemToRemove, int amount)
        {
            List<Item> foundItems = player.inventory.FindItemIDs(itemToRemove.itemid);
            int numberFound = foundItems == null ? 0 : foundItems.Sum(item => item.amount);
            if (numberFound < amount) amount = numberFound;
            int numberRemoved = player.inventory.Take(foundItems, itemToRemove.itemid, amount);
            return numberRemoved;
        }
        int RemoveItemsFromInventory(BasePlayer player, string shortname, int amount)
        {
            return RemoveItemsFromInventory(player, ItemManager.FindItemDefinition(shortname), amount);
        }

        void GivePlayerItems(BasePlayer player, ItemDefinition item, int amount)
        {
            int stacksize = GetStackSize(item);
            int fullstacks = (int)Math.Floor((double)amount / stacksize);
            int remainder = amount - fullstacks * stacksize;
            for (int i = 0; i < fullstacks; i++)
                ItemManager.Create(item, stacksize).MoveToContainer(player.inventory.containerMain);
            if (remainder != 0)
                ItemManager.Create(item, remainder).MoveToContainer(player.inventory.containerMain);
        }
        void GivePlayerItems(BasePlayer player, string shortname, int amount)
        {
            GivePlayerItems(player, ItemManager.FindItemDefinition(shortname), amount);
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (item.info.shortname != "metal.ore" && item.info.shortname != "sulfur.ore" && item.info.shortname != "hq.metal.ore") return;
            
            if (container.itemList.Count() > 1) return;

            int cap = container.capacity;
            if (cap != 6 && cap != 18) return;
            
            if (item.amount < cap) return;

            int oresize, woodsize, outputsize;
            if (cap == 6)
            {
                oresize = Cfg.furnaceHQMetalOres;
                woodsize = Cfg.furnaceHQMetalWood;
                outputsize = Cfg.furnaceHQMetalOutput;
                if (item.info.shortname == "metal.ore")
                {
                    oresize = Cfg.furnaceMetalOres;
                    woodsize = Cfg.furnaceMetalWood;
                    outputsize = Cfg.furnaceMetalOutput;
                }
                else if (item.info.shortname == "sulfur.ore")
                {
                    oresize = Cfg.furnaceSulfurOres;
                    woodsize = Cfg.furnaceSulfurWood;
                    outputsize = Cfg.furnaceSulfurOutput;
                }
            }
            else
            {
                oresize = Cfg.largeFurnaceHQMetalOres;
                woodsize = Cfg.largeFurnaceHQMetalWood;
                outputsize = Cfg.largeFurnaceHQMetalOutput;
                if (item.info.shortname == "metal.ore")
                {
                    oresize = Cfg.largeFurnaceMetalOres;
                    woodsize = Cfg.largeFurnaceMetalWood;
                    outputsize = Cfg.largeFurnaceMetalOutput;
                }
                else if (item.info.shortname == "sulfur.ore")
                {
                    oresize = Cfg.largeFurnaceSulfurOres;
                    woodsize = Cfg.largeFurnaceSulfurWood;
                    outputsize = Cfg.largeFurnaceSulfurOutput;
                }
            }
            double woodfactor = Cfg.cooktime_hqmetal / Cfg.burntime_wood;
            string outputname = "metal.refined";
            if (item.info.shortname == "metal.ore")
            {
                woodfactor = Cfg.cooktime_metal / Cfg.burntime_wood;
                outputname = "metal.fragments";
            }
            else if (item.info.shortname == "sulfur.ore")
            {
                woodfactor = Cfg.cooktime_sulfur / Cfg.burntime_wood;
                outputname = "sulfur";
            }
            
            if (oresize + woodsize + outputsize > cap) return;

            BaseOven furnace = null;
            foreach (BaseOven key in furnaceCache.Keys)
                if (key.inventory == container)
                {
                    furnace = key;
                    break;
                }
            if (!furnace) return;

            BasePlayer player;
            if (!furnaceCache.TryGetValue(furnace, out player) || !player) return;

            int orecount = 0;
            Item[] items = player.inventory.AllItems();
            foreach (Item itm in items)
                if (itm.info.shortname == item.info.shortname)
                    orecount += itm.amount;
            orecount += item.amount;
            if (orecount > oresize * GetStackSize(outputname))
                orecount = oresize * GetStackSize(outputname);


            ItemDefinition wooddefinition = ItemManager.FindItemDefinition("wood");
            int woodToRetain = (int)(Math.Ceiling((double)orecount / oresize) * woodfactor);
            Puts(woodToRetain.ToString());
            int woodMaxStack = GetStackSize(wooddefinition);
            if (woodToRetain > woodMaxStack * woodsize)
                woodToRetain = woodMaxStack * woodsize;
            Puts(woodToRetain.ToString());

            int retainedWood = RemoveItemsFromInventory(player, wooddefinition, woodToRetain);
            if (retainedWood < woodsize)
            {
                GivePlayerItems(player, wooddefinition, retainedWood);
                return;
            }

            if (woodToRetain > retainedWood)
            {
                orecount = (int)(Math.Floor(retainedWood / woodfactor) * oresize);
                int oldretained = retainedWood;
                retainedWood = (int)(orecount / oresize * woodfactor);
                GivePlayerItems(player, wooddefinition, oldretained - retainedWood);
            }

            int retainedAmount;
            retainedAmount = RemoveItemsFromInventory(player, outputname, outputsize);
            if (retainedAmount < outputsize)
            {
                GivePlayerItems(player, wooddefinition, retainedWood);
                return;
            }

            item.MoveToContainer(player.inventory.containerMain, -1, false);

            int extraWood = retainedWood % woodsize;
            Puts(extraWood.ToString());
            int perstack = (int)Math.Floor((double)retainedWood / woodsize);
            Puts(perstack.ToString());
            for (int i = 0; i < woodsize; i++)
            {
                ItemManager.Create(wooddefinition, perstack + (extraWood > 0 ? 1 : 0)).MoveToContainer(container, -1, false);
                extraWood--;
            }

            for (int i = 0; i < outputsize; i++)
                ItemManager.Create(ItemManager.FindItemDefinition(outputname), 1).MoveToContainer(container, -1, false);

            RemoveItemsFromInventory(player, item.info.shortname, orecount);

            int amountPerStack = orecount / oresize;
            Item[] oresToAdd = new Item[oresize];
            int extras = orecount % oresize;
            for (int i = 0; i < oresize; i++)
            {
                int tmpCnt = 0;
                if (extras > 0)
                    tmpCnt++;
                tmpCnt += amountPerStack;
                extras--;
                oresToAdd[i] = ItemManager.Create(ItemManager.FindItemDefinition(item.info.shortname), tmpCnt);
            }

            foreach (Item oreToAdd in oresToAdd)
                oreToAdd.MoveToContainer(container, -1, false);

            furnace.Invoke("StartCooking", 0);
        }
    }
}