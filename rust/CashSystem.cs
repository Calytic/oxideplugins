// Reference: Oxide.Ext.Rust
// Reference: NLua

using System.Collections.Generic;
using System;








namespace Oxide.Plugins
{
    [Info("CashSystem", "igor1150", 1.0)]
    class CashSystem : RustPlugin
    {
        private string menuPreco = "precos";
        private string menuQuantia = "quantia";
        private string menuNome = "nome";
        void OnPlayerInit(BasePlayer player){
            LoadConfig();
            if (Config[menuPreco] == null)
                GerarListaPrecos();
            if(Config[menuQuantia] == null)
                GerarListaQuantia();
            if (Config[menuNome] == null)
                GerarListaNome();
        }
        [ChatCommand("buy")]
        void chatCompra(BasePlayer player, string command, string[] args)
        {
            if (Convert.ToInt32(args[1].ToString()) == 0 || Convert.ToInt32(args[1].ToString()) == null)
            {
                SendReply(player, "To buy the amount must be greater than 0 (zero)");
                SendReply(player, "How to use: /buy \"ITEMNAME\" \"AMOUNT\"");
            }
            else
            {
                if (!VenderItem(player, args[0].ToString(), Convert.ToInt32(args[1].ToString())))
                {
                    SendReply(player, "Make sure you typed the item name correctly");
                    SendReply(player, "How to use: /buy \"ITEMNAME\" \"AMOUNT\"");
                }
            }
        }
        [ChatCommand("cash")]
        void chatCash(BasePlayer player, string command, string[] args)
        {
            SendReply(player, String.Concat("Your current cash and: ", Convert.ToString(ObterCash(player))));
        }
        [ChatCommand("addcash")]
        void chataddcash(BasePlayer player, string command, string[] args)
        {
            try
            {
                if (args[0].ToString() == "" || args[0] == null || args[0].ToString().Length <= 0)
                {
                    SendReply(player, "Enter a nickname");
                }
                if (args[1].ToString() == Convert.ToString(0) || args[1] == null)
                {
                    SendReply(player, "Enter a value");
                }
                else
                {
                    if (!AddCash(player, args[0].ToString(), Convert.ToInt32(args[1].ToString())))
                    {
                        SendReply(player, "How to use: /addcash \"PLAYERNOME\" \"AMOUNT\"");
                    }
                }
            }
            catch (Exception ex)
            {
                SendReply(player, "How to use: /addcash \"PLAYERNOME\" \"AMOUNT\"");
            }
        }
        [ChatCommand("help")]
        void chatajuda(BasePlayer player, string command, string[] args)
        {
            if (Administrador(player))
            {
                SendReply(player, "How to use: /addcash \"PLAYERNAME\" \"AMOUNT\"");
                SendReply(player, "How to use: /cash -show cash");
                SendReply(player, "How to use: /buy \"ITEMNAME\" \"AMOUNT\"");
                SendReply(player, "How to use: /generatepricelists -generates a new price list [CAUTION]");
                SendReply(player, "How to use: /generateamountlist -generates a new list of the amount of items [CAUTION]");
                SendReply(player, "How to use: /generatenamelist -generates a new list of names of items in English");
                SendReply(player, "How to use: /getprice \"ITEMNAME\"");
                SendReply(player, "How to use: /setprice \"ITEMNAME\" \"VALOR\"");
                SendReply(player, "How to use: /delitem \"ITEMNAME\"");                
                SendReply(player, "How to use: /setamount \"ITEMNAME\" \"AMOUNT\"");
                SendReply(player, "How to use: /getamount \"ITEMNAME\"");
            }
            else
            {
                SendReply(player, "How to use: /cash -show cash");
                SendReply(player, "How to use: /getprice \"ITEMNAME\"");
                if (ObterCash(player) > 0)
                {
                    SendReply(player, "How to use: /buy \"ITEMNAME\" \"AMOUNT\"");
                }
            }
        }
        [ChatCommand("generatepricelist")]
        void chatgerarlistaprecos(BasePlayer player, string command, string[] args)
        {
            if (!GerarListaPrecos())
                SendReply(player, "Error generating the list");
            else
                SendReply(player, "Success to generate the list");
        }
        [ChatCommand("getprice")]
        void chatobterpreco(BasePlayer player, string command, string[] args)
        {
            if (args[0].ToString() == "" || args[0] == null)
                SendReply(player, "enter a name of an item");
            else
            {
                int preco = ObterPreco(args[0].ToString());
                if (preco == 0 || preco < 0)
                    SendReply(player, "The item does not exist or is not for sale make sure you typed the name correctly type / help for more information");
                else
                    SendReply(player, String.Concat("The value of the item and: ", preco));

            }

        }
        [ChatCommand("setprice")]
        void chatdefinirpreco(BasePlayer player, string command, string[] args)
        {
            if (!Administrador(player))
            {
                SendReply(player, "The command does not exist");
            }
            else
            {
                if (args[0].ToString() == "" || args[0] == null || Convert.ToInt32(args[1]) == 0 || args[1] == null)
                    SendReply(player, "Enter a name of an item and price");
                else
                {
                    if (ObterPreco(args[0].ToString())>0)
                        SendReply(player, "Error setting the price of the item make sure you typed the name correctly");
                    else
                    {
                        DefinirPreco(args[0].ToString(), Convert.ToInt32(args[1]));
                        SendReply(player, String.Concat("Price change item with new price and success: ", Convert.ToString(args[1])));
                    }
                }
            }
        }
        [ChatCommand("delitem")]
        void chatremoveritem(BasePlayer player, string command, string[] args)
        {
            if (!Administrador(player))
            {
                SendReply(player, "The command does not exist");
            }
            else
            {
                if (args[0].ToString() == "" || args[0] == null)
                    SendReply(player, "Eter a name of an item");
                else
                {
                    if (ObterPreco(args[0].ToString())==0)
                        SendReply(player, "Error deleting the item make sure you typed the name correctly");
                    else
                    {
                        RemoverItem(args[0].ToString());
                        SendReply(player, "Item successfully deleted!");
                    }
                }
            }
        }
        [ChatCommand("generateamountlist")]
        void chatgerarlistaquantia(BasePlayer player, string command, string[] args)
        {
            if (!GerarListaQuantia())
                SendReply(player, "Error generating the list");
            else
                SendReply(player, "Success to generate the list");
        }
        [ChatCommand("getamount")]
        void chatobterquantia(BasePlayer player, string command, string[] args)
        {
            if (args[0].ToString() == "" || args[0] == null)
                SendReply(player, "enter a name of an item");
            else
            {
                int quantia = ObterQuantia(args[0].ToString());
                if (quantia == 0 || quantia < 0)
                    SendReply(player, "The item does not exist or is not for sale make sure you typed the name correctly type / help for more information");
                else
                    SendReply(player, String.Concat("The amount of the item and: ", quantia));

            }
        }
        [ChatCommand("setamount")]
        void chatdefinirquantia(BasePlayer player, string command, string[] args)
        {
            if (!Administrador(player))
            {
                SendReply(player, "The command does not exist");
            }
            else
            {
                if (args[0].ToString() == "" || args[0] == null || Convert.ToInt32(args[1]) == 0 || args[1] == null)
                    SendReply(player, "enter a name of an item e depois a quantia");
                else
                {
                    if (ObterPreco(args[0].ToString()) == 0)
                        SendReply(player, "Error setting the amount of the item make sure you typed the name correctly");
                    else
                    {
                        DefinirQuantia(args[0].ToString(), Convert.ToInt32(args[1]));
                        SendReply(player, String.Concat("Amount successfully changed item new amount and: ", Convert.ToString(args[1])));
                    }
                }
            }
        }

        [ChatCommand("generatenamelist")]
        void chatgerarlistanome(BasePlayer player, string command, string[] args)
        {
            if (!GerarListaNome())
                SendReply(player, "Error generating the list");
            else
                SendReply(player, "Success to generate the list");
        }


        bool VenderItem(BasePlayer player, string nome, int quantia)
        {
            try
            {
                LoadConfig();
                string ID641 = player.userID.ToString();
                object value1;
                var menu = (Config[menuNome]) as Dictionary<string, object>;
                if (menu != null)
                {
                    menu.TryGetValue(nome, out value1);
                    if (value1 != null || value1.ToString() != "")
                        nome = value1.ToString();
                }
                 int cash = ObterCash(player);
                 int preco = ObterPreco(nome);
                 if (cash < preco)
                 {
                     SendReply(player, "Your current cash and not enough to buy");
                     return false;
                 }
                 if (preco == 0)
                 {
                     return false;
                 }
                var item = ItemManager.FindItemDefinition(nome);
                if (item == null)
                    return false;            
                else{
                    string ID64 = player.userID.ToString();
                    cash -= preco * quantia;
                    SendReply(player, String.Concat("Purchased Item successfully! price: ", Convert.ToString(preco * quantia)));
                    Dictionary<string, object> subMenu = new Dictionary<string, object>();
                    subMenu.Add("cash", Convert.ToString(cash));
                    Config[ID64] = subMenu;
                    SaveConfig();
                    quantia = quantia * ObterQuantia(nome);
                    player.inventory.GiveItem(ItemManager.CreateByItemID((int)item.itemid, quantia, false), (ItemContainer)((BasePlayer)player).inventory.containerMain);              
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return false;
        }
        int ObterCash(BasePlayer player)
        {
            LoadConfig();
            string ID64 = player.userID.ToString();
            var menu = (Config[ID64]) as Dictionary<string, object>;
            if (menu != null)
            {
                object value;
                menu.TryGetValue("cash", out value);
                int cash = Convert.ToInt32(value);
                if (cash > 0)
                {
                    return cash;
                }
                else
                {
                    RemoveCash((BasePlayer)player);
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
        bool RemoveCash(BasePlayer player)
        {
            LoadConfig();
            string ID64 = player.userID.ToString();
            var newMenu = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> str in Config)
            {
                if (str.Key.ToString() != ID64.ToString() && str.Value != null)//testar com o str.Value eu nao testei ainda
                {
                    newMenu.Add(str.Key.ToString(), str.Value);
                }
            }
            Config.Clear();
            foreach (KeyValuePair<string, object> str in newMenu)
            {
                Config[str.Key] = str.Value;
            }
            SaveConfig();
            return true;
        }
        bool AddCash(BasePlayer player, string nick, int cash)
        {
            if (!Administrador(player))
            {
                SendReply(player, "The command does not exist");
            }
            else
            {
                var add = BasePlayer.Find(nick);
                if (add == null)
                {
                    SendReply(player, "Player not found");
                    return false;
                }
                LoadConfig();
                string ID64 = add.userID.ToString();
                if (ID64 == "" || ID64 == null)
                {
                    SendReply(player, "Error looking the ID64Steam");
                    return false;
                }
                Dictionary<string, object> subMenu = new Dictionary<string, object>();
                cash += ObterCash(add);
                subMenu.Add("cash", Convert.ToString(cash));
                Config[ID64] = subMenu;
                SaveConfig();
                SendReply(player, String.Concat("Cash added value with new success: ", Convert.ToString(cash)));
            }
            return true;
        }
        bool Administrador(BasePlayer player)
        {
            if (player.net.connection.authLevel >= 2)
                return true;
            return false;
        }
        bool GerarListaPrecos()
        {
            LoadConfig();
            var items = ItemManager.GetItemDefinitions();
            if (items == null)
                return false;
            var newMenu = new Dictionary<string, int>();
            foreach (var item in items)
            {
                newMenu.Add(Convert.ToString(item.shortname), 2);
            }
            if (newMenu == null)
                return false;
            Config[menuPreco] = newMenu;
            SaveConfig();
            return true;
        }
        int ObterPreco(string nomeItem)
        {
            LoadConfig();
            Dictionary<string, object> menu = new Dictionary<string, object>();
            menu = Config[menuPreco] as Dictionary<string, object>;    
            if (menu != null)
            {
                object value;
                menu.TryGetValue(nomeItem, out value);
                if (Convert.ToInt32(value) > 0)
                {
                    return Convert.ToInt32(value);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
        void DefinirPreco(string nomeItem, int Preco)
        {
            LoadConfig();
            var newMenu = Config[menuPreco] as Dictionary<string, object>;
            newMenu[nomeItem] = Preco;
            Config[menuPreco] = newMenu;
            SaveConfig();
        }
        void RemoverItem(string nomeItem)
        {
            LoadConfig();
            var Menu = Config[menuPreco] as Dictionary<string, object>;
            var newMenu = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> str in Menu)
            {
                if (str.Key.ToString() != nomeItem)
                {
                    newMenu.Add(str.Key.ToString(), str.Value);
                } 
            }
            Config[menuPreco] = newMenu;
            Menu = new Dictionary<string, object>();
            Menu = Config[menuQuantia] as Dictionary<string, object>;
            newMenu = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> str in Menu)
            {
                if (str.Key.ToString() != nomeItem)
                {
                    newMenu.Add(str.Key.ToString(), str.Value);
                }
            }
            Config[menuQuantia] = newMenu;
            SaveConfig();
        }
        bool GerarListaQuantia()
        {
            LoadConfig();
            var items = ItemManager.GetItemDefinitions();
            if (items == null)
                return false;
            var newMenu = new Dictionary<string, int>();
            foreach (var item in items)
            {
                newMenu.Add(Convert.ToString(item.shortname), 1);
            }
            if (newMenu == null)
                return false;
            Config[menuQuantia] = newMenu;
            SaveConfig();
            return true;
        }        
        int ObterQuantia(string nomeItem)
        {
            LoadConfig();
            Dictionary<string, object> menu = new Dictionary<string, object>();
            menu = Config[menuQuantia] as Dictionary<string, object>;
            if (menu != null)
            {
                object value;
                menu.TryGetValue(nomeItem, out value);
                if (Convert.ToInt32(value) > 0)
                {
                    return Convert.ToInt32(value);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
        void DefinirQuantia(string nomeItem, int quantia)
        {
            LoadConfig();
            var newMenu = Config[menuQuantia] as Dictionary<string, object>;
            newMenu[nomeItem] = quantia;
            Config[menuQuantia] = newMenu;
            SaveConfig();
        }

        bool GerarListaNome()
        {
            LoadConfig();
            var items = ItemManager.GetItemDefinitions();
            if (items == null)
                return false;
            var newMenu = new Dictionary<string, string>();
            foreach (var item in items)
            {
                newMenu.Add(Convert.ToString(item.displayName.english), Convert.ToString(item.shortname));
            }
            if (newMenu == null)
                return false;
            Config[menuNome] = newMenu;
            SaveConfig();
            return true;
        }        
    }
}