using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("InsultGenerator", "Spicy", 1.0)]
    [Description("Grabs an insult from the web and pastes it to a player on command.")]

    class InsultGenerator : RustLegacyPlugin
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

        [ChatCommand("insult")]
        void cmdInsult(NetUser netuser, string command, string[] args)
        {
            string steamid = rust.UserIDFromPlayer(netuser).ToString();
            if(HasPermission(steamid, "insultgenerator.use"))
            {
                if (args == null || args.Length == 0)
                {
                    rust.Notice(netuser, GetLangMessage("InvalidSyntax"), "â", 4);
                }
                else if (args[0] == "me")
                {
                    Request("http://insultgenerator.org", netuser);
                    rust.Notice(netuser, GetLangMessage("SelfInsult"), "â", 4);
                }
                else
                {
                    if (rust.FindPlayer(args[0]) != null)
                    {
                        NetUser target = rust.FindPlayer(args[0]);
                        Request("http://insultgenerator.org", target);
                        rust.Notice(netuser, GetLangMessage("OtherInsult") + target.displayName + ".", "â", 4);
                    }
                    else
                    {
                        rust.Notice(netuser, GetLangMessage("NoUsersFound"), "â", 4);
                    }
                }
            }
            else
            {
                rust.Notice(netuser, GetLangMessage("NoPermission"), "â", 4);
            }
        }

        void Request(string page, NetUser playertoinsult)
        {
            webrequest.EnqueueGet(page, (code, response) => GetCallback(code, response, page, playertoinsult), this);
        }

        void GetCallback(int code, string response, string page, NetUser playertoinsult)
        {
            if (response == null || code != 200)
            {
                Puts(GetLangMessage("WebRequestFailed"));
            }
            else
            {
                rust.SendChatMessage(playertoinsult, GetLangMessage("ChatPrefix"), Insult(response));
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