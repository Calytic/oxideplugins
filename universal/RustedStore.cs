using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
//using System.Web.Script.Serialization;
using System;
namespace Oxide.Plugins
{
    [Info("rusted.store payment system", "Vitrify", "1.0.2", ResourceId = 2134)]
    [Description("Players can claim rewards for automatic PayPal donations")]

    class RustedStore: CovalencePlugin
    {
        public class DonateItem
        {
            [JsonProperty("price")]
            public string price {get; set;}

            [JsonProperty("description")]
            public string description {get; set;}

            [JsonProperty("name")]
            public string name {get; set;}

            [JsonProperty("localId")]
            public string localId {get; set;}

            [JsonProperty("easyId")]
            public string easyId {get; set;}

            [JsonProperty("commands")]
            public List<ItemCommands> commands {get; set;}
        }
        public class Claim
        {
            [JsonProperty("item")]
            public DonateItem item {get; set;}

            [JsonProperty("ticket")]
            public ClaimTicket ticket {get; set;}
        }
        public class ItemCommands
        {
            [JsonProperty("item")]
            public string item {get; set;}

            [JsonProperty("quantity")]
            public string quantity {get; set;}

            [JsonProperty("command")]
            public string command {get; set;}

        }  
        public class ClaimTicket
        {
            [JsonProperty("steamName")]
            public string steamName {get; set;}
            [JsonProperty("easyId")]
            public string easyId {get; set;}
            [JsonProperty("steamId")]
            public string steamId {get; set;}
        }

        void Init()
        {
            LoadDefaultMessages();
            LoadConfigData();
        }

        string secret = "YOUR.SECRET.HERE";
        string username = "YOUR.USERNAME.HERE";
        string command = "donate";
        string baseUrl = "https://rusted.store";
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a configuration file.");
            Config.Clear();
            SetConfig("secret", secret);
            SetConfig("username", username);
            //SetConfig("command", command);
            SaveConfig();
        }

        void LoadConfigData()
        {
            secret = (string)ReadConfig("secret");
            username = (string)ReadConfig("username");
            //command = (string)ReadConfig("command");
        }

        #region Localization
        void LoadDefaultMessages()
        {
            string current_config = "\n" + "Current Config: \nSecret: {0}\nBuy Username: {1}";
            current_config += "\n\nTo configure type \n/{2} config secret YOUR_SECRET_HERE\n/{2} config username YOUR_BUY_USERNAME_HERE";
        
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ItemList"] = "<color=#40bb40>{0}</color> - {1} - ${2}",
                ["buy_prompt"] = "\nThis is a list of items available for purchase.  \nTo view more details, type <color=#ffa500>/{0} </color><color=#40bb40>#</color>.\nFor example, type <color=#ffa500> /{0} </color><color=#40bb40>4</color> for details on package 4",
                ["not_found"] = "An unclaimed donation for your account could not be found",
                ["thank"] = "Thank you for your donation!",
                ["package_not_found"] = "The package ID you entered could not be found.",
                ["purchase_prompt"] = "To purchase this package, visit",
                //["configure_config"] = "To configure that plugin, type /{0} config.",
                ["incomplete_config"] = "This plugin has not been configured.  Please contact the server admin for support.",
                ["incorrect_config"] = "This plugin has been incorrectly configured, please contact the server admin for support.",
                ["current_config"] = current_config,
            }, this);
        }
        #endregion

        [Command("donate")]
        void ChatCommand(IPlayer player, string command, string[] args)
        {

            //admin config
            if (IsAdmin(player.Id) && args.Length > 0 && args[0] == "config"){
                if (args.Length == 3){
                    if (args[1] == "secret"){
                        SetConfig("secret", args[2]);
                        secret = args[2];
                        SaveConfig();
                    } else if (args[1] == "username"){
                        SetConfig("username", args[2]);
                        username = args[2];
                        SaveConfig();
                    }
                    Reply(player, Lang("current_config", player.Id, secret, username, command));
                    return;
                } else {
                    Reply(player, Lang("current_config", player.Id, secret, username, command));
                    return;
                };
            }

            //plugin not set up
            if (secret == "YOUR.SECRET.HERE" || username == "YOUR.USERNAME.HERE"){
                if (IsAdmin(player.Id))
                    Reply(player, Lang("current_config", player.Id, secret, username, command));
                else
                    Reply(player, Lang("incomplete_config", player.Id));
                return;
            }

            //show list
            if (args.Length == 0){

                string url = baseUrl+"/shops/"+username+"?secret="+secret;
                List<string> textLines = new List<string>{"hello123"};
                webrequest.EnqueueGet(url, (code, response) => ListCallback(code, response, player), this);

            } else if (args.Length == 1){
                if (args[0] == "claim"){
                    //claim
                    string url = baseUrl+"/shops/"+username+"/payments/claim?secret="+secret;
                    string params2 = "body={\"steamId\": \""+player.Id+"\"}";
                    webrequest.EnqueuePost(url, params2, (code, response) => ClaimCallback(code, response, player), this);

                } else {
                    //item details
                    string url = baseUrl+"/shops/"+username+"/items/"+args[0]+"?secret="+secret;
                    string params2 = "body={\"steamId\": \""+ player.Id +"\", \"steamName\": \""+ player.Name +"\"}";
                    webrequest.EnqueuePost(url, params2, (code, response) => ItemDetailCallback(code, response, player), this);
                }
            }



        }
        void ClaimCallback(int code, string response, IPlayer player)
        {
            if (code == 400){
                Reply(player, Lang("not_found", player.Id));
                return;
            }
            var r = JsonConvert.DeserializeObject<Claim>(response);
            Puts("------Claimed Payment-----");
            Puts("Player: " + r.ticket.steamName + " / " + r.ticket.steamId);
            Puts("Identifier: " + r.ticket.easyId);
            foreach (var command in r.item.commands){
                server.Command(command.command);
                Puts("Command: " + command.command);
            }
            Puts("-----/Claimed Payment-----");
            Reply(player, Lang("thank", player.Id));
        }
        void ItemDetailCallback(int code, string response, IPlayer player)
        {
            if (code == 400){
                Reply(player, Lang("package_not_found", player.Id));
                return;
            }
            var item = JsonConvert.DeserializeObject<DonateItem>(response);
            var textLines = new string[4];
            var i = 0;
            textLines[0] = item.name + " - $" + item.price;
            textLines[1] = item.description;
            textLines[2] = "";
            textLines[3] = Lang("purchase_prompt", player.Id) + " <color=#0088dd>\n"+baseUrl+"/payments/" + item.easyId + "</color>";
            Reply(player, String.Join("\n", textLines));

        }
        void ListCallback(int code, string response, IPlayer player)
        {
            if (code == 400){
                Reply(player, Lang("incorrect_config", player.Id));
                return;
            }
            var items = JsonConvert.DeserializeObject<List<DonateItem>>(response);
            var stringList = new string[items.Count+1];
            stringList[0] = Lang("buy_prompt", player.Id, "donate");
            var i = 1;
            foreach (var item in items){
                stringList[i] = Lang("ItemList", player.Id, item.localId, item.name, item.price);
                i++;
            }
            Reply(player, String.Join("\n", stringList));

        }

        #region Utility
        void SetConfig(string name, object data)
        {
            Config[name] = data;
            SaveConfig();
        }

        object ReadConfig(string name)
        {
            if (Config[name] != null)
            {
                return Config[name];
            }

            return null;
        }
        #endregion

        bool IsAdmin(string id) => permission.UserHasGroup(id, "admin");
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        static void Reply(IPlayer player, string message, params object[] args) => player.Reply(string.Format(message, args));
    }
}