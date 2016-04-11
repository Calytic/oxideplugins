using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Tear Up BluePrints", "DaBludger", 1.91, ResourceId = 1300)]
    [Description("This will allow players to 'Tear up' Blueprints for BP Fragments")]
    public class TearUpBP : RustPlugin
    {
        private static Dictionary<int, ItemDefinition> ALLITEMS = ItemManager.itemDictionary;
        private static Dictionary<string, object> CUSTOME_ITME_NAMES = new Dictionary<string, object>();
        // this is multiplyed by the total number of frags to get the MIN amount of frags for a BP
        private static double MIN_FRAGS_MULTIPLYER = 0.25;
        // this is multiplyed by the total number of frags to get the MAX amount of frags for a BP
        private static double MAX_FRAGS_MULTIPLYER = 0.50;
        private static bool CAN_PLAYER_USE = true;
        private static bool USE_PERMISSIONS = true;
        //Uncommon is a page, Rare is a Book, VeryRare is a Library
        //The min and max are multiplyed by the rarity number to get the range that a random number is chosen from 
        //that range for the number of BP fragments the player gets
        private static readonly Dictionary<string, int> bprarety2frags = new Dictionary<string, int> { { "Common", 20 }, { "Uncommon", 60 }, { "Rare", 300 }, { "VeryRare", 1200 } };
        private static readonly string UNKNOWN_ITEM = "Unknown item! use chat command /Tearup lookup [name contains]";
        private static readonly string HELP_TEXT = "Use the Tearup command like this '/tearup [bp name]'   or   '/tearup lookup [name contains]' to look up a BP name.";
        private static readonly string NO_PERMISSION = "You do not have the correct permissions to access this plugin.";

        void Init()
        {
            permission.RegisterPermission("tearup.use",this);
            permission.RegisterPermission("tearup.admin", this);
            CheckCfg("CUSTOMENAMES", ref CUSTOME_ITME_NAMES);
            Puts("Number of BPs: " + CUSTOME_ITME_NAMES.Keys.Count);
            CheckCfg("TEARUP_MIN_FRAGS_MULTIPLYER", ref MIN_FRAGS_MULTIPLYER);
            CheckCfg("TEARUP_MAX_FRAGS_MULTIPLYER", ref MAX_FRAGS_MULTIPLYER);
            CheckCfg("USE_PERMISSIONS", ref USE_PERMISSIONS);
            CheckCfg("CAN_PLAYER_USE", ref CAN_PLAYER_USE);
            if (MIN_FRAGS_MULTIPLYER <= 0)
            {
                setMinFrags(0.25);
            }
            if (MAX_FRAGS_MULTIPLYER < MIN_FRAGS_MULTIPLYER)
            {
                setMaxFrag(MIN_FRAGS_MULTIPLYER);
            }
            Puts("Tear IT UP!");
        }
        /**
        This has to be done after the server loads up to have the list of BPs to get the names.
        **/
        void OnServerInitialized()
        {
            ALLITEMS = ItemManager.itemDictionary;
            for (int i = 0; i < ItemManager.bpList.Count; i++)
            {
                if (!CUSTOME_ITME_NAMES.ContainsKey(ItemManager.bpList[i].name))
                {
                    Puts("Adding item name: " + ItemManager.bpList[i].name);
                    CUSTOME_ITME_NAMES.Add(ItemManager.bpList[i].name, null);
                }
            }
            Puts("Number of BPs: " + CUSTOME_ITME_NAMES.Keys.Count);
            SaveConfig();
            Puts("Custome names loaded.");
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void LoadDefaultConfig() { }

        private void setMinFrags(double min)
        {
            MIN_FRAGS_MULTIPLYER = min;
        }

        private void setMaxFrag(double max)
        {
            MAX_FRAGS_MULTIPLYER = max;
        }

        [ChatCommand("tearup")]
        private void TearupCommand(BasePlayer player, string command, string[] args)
        {
            if (USE_PERMISSIONS)
            {
                if (!permission.UserHasPermission(player.userID + "", "terup.admin") && !CAN_PLAYER_USE)
                {
                    sendChant2Player(player, NO_PERMISSION);
                    return;
                }
            }
            if (args.Length == 0)
            {
                sendChant2Player(player, HELP_TEXT);
                return;
            }
            else if (args.Length == 1)
            {
                if ("lookup".Equals(args[0].Trim().ToLower()))
                {
                    sendChant2Player(player, HELP_TEXT);
                    return;
                }
                if (args[0].Trim().Length > 0)
                {
                    string bpname = (args[0].Trim() + ".item").ToUpper();
                    string realname = getRealBPItemName(args[0].Trim());
                    ItemBlueprint ib = null;
                    if (realname == null)
                    {
                        realname = bpname;
                        ib = getBPItem(bpname);
                    }
                    else
                    {
                        ib = getBPItem(realname);
                    }
                    if (ib != null)
                    {
                        int min = (int)(bprarety2frags[ib.rarity.ToString()] * MIN_FRAGS_MULTIPLYER);
                        int max = (int)((bprarety2frags[ib.rarity.ToString()] * MAX_FRAGS_MULTIPLYER) + 1);
                        if (removeBP(player, getItemId(ib.name)))
                        {
                            givePlayerFragments(player, Random.Range(min, max));
                        }
                        else
                        {
                            sendChant2Player(player, realname + " is not in your inventory!");
                        }
                        return;
                    }
                    else
                    {
                        sendChant2Player(player, realname + " is not a valid Blueprint name, doing /tearup lookup  for you");
                        string[] newArgs = new string[2] { "lookup", args[0] };
                        TearupCommand(player, command, newArgs);
                    }
                }
            }
            else if (args.Length == 2 && "lookup".Equals(args[0].Trim().ToLower()) && args[1].Trim().Length > 0)
            {
                string wildcard = args[1].Trim().ToUpper();
                string result = "";
                List<string> matchingItems = new List<string>();
                string[] cValues = new string[CUSTOME_ITME_NAMES.Values.Count];
                CUSTOME_ITME_NAMES.Values.CopyTo(cValues, 0);
                string[] cKeys = new string[CUSTOME_ITME_NAMES.Keys.Count];
                CUSTOME_ITME_NAMES.Keys.CopyTo(cKeys, 0);
                for (int k = 0; k < cValues.Length; k++)
                {
                    if (cValues[k] != null)
                    {
                        if (cValues[k].Trim().ToUpper().Contains(wildcard))
                        {
                            matchingItems.Add(cKeys[k]);
                        }
                    }
                }

                for (int i = 0; i < ItemManager.bpList.Count; i++)
                {
                    if (ItemManager.bpList[i].name.ToUpper().Contains(wildcard))
                    {
                        matchingItems.Add(ItemManager.bpList[i].ToString());
                        //result = prityPrintItemNames(ItemManager.bpList[i]);
                    }
                }
                foreach (string name in matchingItems)
                {
                    string nameClean = name;
                    if (name.Contains(" "))
                    {
                        nameClean = name.Substring(0, name.IndexOf(" "));
                    }
                    if (CUSTOME_ITME_NAMES.ContainsKey(nameClean) && CUSTOME_ITME_NAMES[nameClean] != null)
                    {
                        result += CUSTOME_ITME_NAMES[nameClean] + ", ";
                    }
                    else
                    {
                        result += prityPrintItemNames(name) + ", ";
                    }
                }
                if (result.Length == 0)
                {
                    sendChant2Player(player, "No Item Names were found containing " + wildcard + ".");
                }
                else
                {
                    sendChant2Player(player, result);
                }
                return;
            }
            else
            {
                sendChant2Player(player, HELP_TEXT);
                return;
            }
        }

        private void sendChant2Player(BasePlayer player, string msg)
        {
            PrintToChat(player, msg, new object[0]);
        }

        private void givePlayerFragments(BasePlayer player, int frags2give)
        {
            player.GiveItem(ItemManager.CreateByItemID(1351589500, frags2give));
        }

        private bool isNameOfBP(string bpname)
        {
            foreach (ItemBlueprint ib in ItemManager.bpList)
            {
                if (ib.name.ToUpper().Equals(bpname))
                {
                    return true;
                }
            }
            return false;
        }

        private string getRealBPItemName(string cName)
        {
            object[] values = new object[CUSTOME_ITME_NAMES.Values.Count];
            CUSTOME_ITME_NAMES.Values.CopyTo(values, 0);
            string[] keys = new string[CUSTOME_ITME_NAMES.Keys.Count];
            CUSTOME_ITME_NAMES.Keys.CopyTo(keys, 0);

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                {
                    if (cName.ToLower().Trim() == (values[i] as string).ToLower().Trim())
                    {
                        return keys[i].ToLower().Trim();
                    }
                }
            }
            return null;
        }

        private ItemBlueprint getBPItem(string bpname)
        {
            foreach (ItemBlueprint ib in ItemManager.bpList)
            {
                if (ib.name.ToUpper().Equals(bpname.ToUpper()))
                {
                    return ib;
                }
            }
            return null;
        }

        private bool removeBP(BasePlayer player, int bpid)
        {
            if (bpid != -1)
            {
                foreach(Item i in player.inventory.AllItems())
                {
                    if(i.IsBlueprint() && i.info.itemid == bpid)
                    {
                        i.RemoveFromContainer();
                        return true;
                    }
                }
            }
            return false;
        }

        private int getItemId(string name)
        {
            if (name != null && name.Length > 0)
            {
                foreach (ItemDefinition itemdef in ALLITEMS.Values)
                {
                    if (itemdef.name.ToUpper().Equals(name.ToUpper()))
                    {
                        return itemdef.itemid;
                    }
                }
            }

            return -1;
        }

        private string prityPrintItemNames(ItemBlueprint iBP)
        {
            string temp = "" + iBP;
            return temp.Substring(0, temp.IndexOf(".item"));
        }

        private string prityPrintItemNames(string iBPname)
        {
            return iBPname.Substring(0, iBPname.IndexOf(".item"));
        }
    }
}
