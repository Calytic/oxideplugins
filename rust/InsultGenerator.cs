using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("InsultGenerator", "Spicy", 1.0)]
    [Description("Grabs an insult from the web and pastes it to a player on command.")]

    class InsultGenerator : RustPlugin
    {
        void Init()
        {
            LoadLang();
            RegisterPermission("insultgenerator.use");
        }

        void LoadLang()
        {
            var messages = new Dictionary<string, string>
            {
                {"ChatPrefix", "InsultGenerator"},
                {"InvalidSyntax", "Syntax: /insult [me | playername]"},
                {"SelfInsult", "You insulted yourself."},
                {"OtherInsult", "You insulted: "},
                {"GetInsulted", " insulted you!"},
                {"NoUsersFound", "No users found with that name."},
                {"NoPermission", "You don't have permission to use this command!"},
                {"WebRequestFailed", "Webrequest for InsultGenerator failed!"}
            };
            lang.RegisterMessages(messages, this);
        }

        string GetLangMessage(string key)
        {
            return lang.GetMessage(key, this);
        }

        void RegisterPermission(string permission_name)
        {
            permission.RegisterPermission(permission_name, this);
        }

        bool HasPermission(string steamid, string permission_name)
        {
            if (permission.UserHasPermission(steamid, permission_name))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void SendMessage(BasePlayer player, string message)
        {
            rust.SendChatMessage(player, GetLangMessage("ChatPrefix"), message);
        }

        BasePlayer FindPlayer(string args)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString == args)
                    return activePlayer;
                if (activePlayer.displayName.ToLower().Contains(args.ToLower()))
                    return activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == args)
                    return activePlayer;
            }
            return null;
        }

        [ChatCommand("insult")]
        void cmdInsult(BasePlayer player, string command, string[] args)
        {
            string steamid = rust.UserIDFromPlayer(player).ToString();
            if(HasPermission(steamid, "insultgenerator.use"))
            {
                if (args == null || args.Length == 0)
                {
                    SendMessage(player, GetLangMessage("InvalidSyntax"));
                }
                else if (args[0] == "me")
                {
                    Request("http://insultgenerator.org", player, player);
                }
                else
                {
                    if (FindPlayer(args[0]) != null)
                    {
                        var target = FindPlayer(args[0]);
                        
                        Request("http://insultgenerator.org", target, player);
                    }
                    else
                    {
                        SendMessage(player, GetLangMessage("NoUsersFound"));
                    }
                }
            }
            else
            {
                SendMessage(player, GetLangMessage("NoPermission"));
            }
        }

        void Request(string page, BasePlayer playertoinsult, BasePlayer player)
        {
            webrequest.EnqueueGet(page, (code, response) => GetCallback(code, response, page, playertoinsult, player), this);
        }

        void GetCallback(int code, string response, string page, BasePlayer playertoinsult, BasePlayer player)
        {
            if (response == null || code != 200)
            {
                Puts(GetLangMessage("WebRequestFailed"));
            }
            else
            {
                string insult = Insult(response);
                if (playertoinsult == player)
                {
                    SendMessage(player, GetLangMessage("SelfInsult"));
                    SendMessage(playertoinsult, insult);
                }
                else
                {
                    SendMessage(player, GetLangMessage("OtherInsult") + playertoinsult.displayName + " with insult:\n" + insult);
                    SendMessage(playertoinsult, player.displayName + GetLangMessage("GetInsulted"));
                    SendMessage(playertoinsult, insult);
                }
            }
        }

        string Insult(string response)
        {
            int start = response.IndexOf("<br><br>");
            int end = response.IndexOf("</div>\n<center>");
            string insult = response.Substring(start + 8, end - start - 8);

            return insult;
        }
    }
}