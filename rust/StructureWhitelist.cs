using System.Collections.Generic;
using System;
namespace Oxide.Plugins
{
    [Info("StructureWhitelist", "Norn", 0.2, ResourceId = 1511)]
    [Description("Choose which grades players can build.")]
    public class StructureWhitelist : RustPlugin
    {
        void OnServerInitialized()
        {
            Puts("Wood: " + Config["Grade", "Wood"].ToString() + " | Stone: " + Config["Grade", "Stone"].ToString() + " | Metal: " + Config["Grade", "Metal"].ToString() + " | TopTier: " + Config["Grade", "TopTier"].ToString());
        }
        void Loaded()
        {
            LoadDefaultMessages();
        }

        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating..."); Config.Clear();
            Config["Grade", "Wood"] = true;
            Config["Grade", "Stone"] = true;
            Config["Grade", "Metal"] = true;
            Config["Grade", "TopTier"] = true;
        }

        #region Localization

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"NotAllowed", "Structure grade: <color=yellow>{grade}</color> has been <color=red>disabled</color> on this server."},
            };
            lang.RegisterMessages(messages, this);
        }
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);

        #endregion

        object OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            string gradestring = grade.ToString();
            switch (gradestring)
            {
                case "Wood":
                    {
                        if (!Convert.ToBoolean(Config["Grade", "Wood"])) { PrintToChat(player, GetMessage("NotAllowed", player.UserIDString).Replace("{grade}", gradestring)); return false; }
                        break;
                    }
                case "Stone":
                    {
                        if (!Convert.ToBoolean(Config["Grade", "Stone"])) { PrintToChat(player, GetMessage("NotAllowed", player.UserIDString).Replace("{grade}", gradestring)); return false; }
                        break;
                    }
                case "Metal":
                    {
                        if (!Convert.ToBoolean(Config["Grade", "Metal"])) { PrintToChat(player, GetMessage("NotAllowed", player.UserIDString).Replace("{grade}", gradestring)); return false; }
                        break;
                    }
                case "TopTier":
                    {
                        if (!Convert.ToBoolean(Config["Grade", "TopTier"])) { PrintToChat(player, GetMessage("NotAllowed", player.UserIDString).Replace("{grade}", gradestring)); return false; }
                        break;
                    }
            }
            return null;
        }
    }
}