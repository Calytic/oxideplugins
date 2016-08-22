using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("PaiN AFK", "PaiN", "0.1", ResourceId = 0)]
    class PaiNAFK : RustPlugin
    {
        class Data { public List<ulong> AfkPlayers = new List<ulong>(); }
        Data data;

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"AFK Enabled", "You are now afk!"},
                {"AFK Disabled", "You are no longer afk!" }
            }, this);
        }

        void Loaded() { ReadData(); LoadMessages(); }

        void Unloaded()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (IsAfk(player.userID))
                {
                    data.AfkPlayers.Remove(player.userID);
                    player.EndSleeping();
                }
            SaveData();
        }

        [ChatCommand("afk")]
        void cmdAfk(BasePlayer player, string cmd, string[] args)
        {
            if (IsAfk(player.userID))
                StartAFK(player);
            else
                StartAFK(player, true);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (IsAfk(player.userID))
                StartAFK(player, false);
        }

        void OnServerSave() => SaveData();

        bool IsAfk(ulong userid)
        {
            if (data.AfkPlayers.Contains(userid))
                return true;
            else return false;
        }

        private void StartAFK(BasePlayer player, bool enable = true)
        {
            if(enable == true)
            {
                SendReply(player, GetLang("AFK Enabled", player.userID.ToString()));
                data.AfkPlayers.Add(player.userID);
                player.StartSleeping();
            }
            else
            {
                SendReply(player, GetLang("AFK Disabled", player.userID.ToString()));
                data.AfkPlayers.Remove(player.userID);           
            }
        }

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("AfkPlayers", data);
        void ReadData() => data = Interface.Oxide.DataFileSystem.ReadObject<Data>("AfkPlayers");
        string GetLang(string msg, string userID) => lang.GetMessage(msg, this, userID);
    }
}
