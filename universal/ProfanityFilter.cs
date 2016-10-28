using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("ProfanityFilter", "Spicy", "1.0.0")]
    [Description("Filters profanity.")]

    class ProfanityFilter : CovalencePlugin
    {
        #region Config

        protected override void LoadDefaultConfig()
        {
            Config["BannedWords"] = new List<string>
            {
                "fuck",
                "shit",
                "cunt"
            };
        }

        #endregion

        #region Lang

        private void InitialiseLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BannedWord"] = "That's a banned word."
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BannedWord"] = "Ce mot est interdit."
            }, this, "fr");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BannedWord"] = "Esta palabra es prohibida."
            }, this, "es");
        }

        #endregion

        #region Hooks

        private void OnServerInitialized() => InitialiseLang();

        private object OnUserChat(IPlayer player, string message)
        {
            foreach (string bannedWord in Config.Get<List<string>>("BannedWords"))
            {
                if (message.Contains(bannedWord))
                {
                    player.Reply(lang.GetMessage("BannedWord", this, player.Id));
                    return true;
                }
                return null;
            }
            return null;
        }

        #endregion
    }
}
