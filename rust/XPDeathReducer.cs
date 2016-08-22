
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Rust.Xp;

namespace Oxide.Plugins
{
    [Info("XP Death Reducer", "k1lly0u", "0.1.2", ResourceId = 2007)]
    class XPDeathReducer : RustPlugin
    {
        #region Fields
        [PluginReference]
        Plugin Clans;
        [PluginReference]
        Plugin Friends;
        [PluginReference]
        Plugin EventManager;

        #endregion

        #region Oxide Hooks  
        void Loaded() => lang.RegisterMessages(Messages, this);      
        void OnServerInitialized() => LoadVariables();
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                var attacker = info?.InitiatorPlayer;
                var victim = entity.ToPlayer();
                if (victim != null)
                {
                    if (attacker != null)
                    {
                        if (configData.Exemptions.FriendsExempt && IsFriend(attacker.userID, entity.ToPlayer().userID)) return;
                        if (configData.Exemptions.ClansExempt && IsClanmate(attacker.userID, entity.ToPlayer().userID)) return;
                    }
                    if (configData.Exemptions.AdminExempt && victim.IsAdmin()) return;
                    if (IsPlaying(victim)) return;
                    DeductPlayerXP(victim, attacker);
                }
            }
            catch { }
        }
        #endregion

        #region Functions
        private void DeductPlayerXP(BasePlayer victim, BasePlayer attacker)
        {
            var totalXP = victim.xp.SpentXp + victim.xp.UnspentXp;            
            var unspent = victim.xp.UnspentXp;
            var spent = victim.xp.SpentXp;
            float amount = 0;     

            if (configData.Deductions.UsePercentageDeduction)
                amount = (totalXP / 100) * configData.Deductions.XPDeductionPercentage;
            else amount = configData.Deductions.XPDeductionStatic;

            if (victim.xp.UnspentXp > 1)
            {
                victim.xp.Reset();
                victim.xp.Add(Definitions.Cheat, totalXP - amount);
                victim.xp.SpendXp((int)spent, null);
                var deathmessage = MSG("lossMessage", attacker.UserIDString)
                        .Replace("{color}", configData.Messaging.MSG_Color)
                        .Replace("{amount}", $"</color>{configData.Messaging.MSG_MainColor}{(int)amount}</color>{configData.Messaging.MSG_Color}")
                        .Replace("{endcolor}", "</color>");
                MessagePlayer(victim, deathmessage);
            }

            if (attacker != null && configData.Options.GiveDeductedXPToKiller)
            {
                attacker.xp.Add(Definitions.Cheat, amount);
                var killmessage = MSG("gainMessage", attacker.UserIDString)
                    .Replace("{color}", configData.Messaging.MSG_Color)
                    .Replace("{amount}", $"</color>{configData.Messaging.MSG_MainColor}{(int)amount}</color>{configData.Messaging.MSG_Color}")
                    .Replace("{victimname}", $"</color>{configData.Messaging.MSG_MainColor}{victim.displayName}</color>");
                MessagePlayer(attacker, killmessage);
            }
        }
        private void MessagePlayer(BasePlayer player, string message)
        {
            if (player.IsSleeping() || player.IsDead())
            {
                timer.Once(3, () => MessagePlayer(player, message));
                return;
            }
            else SendReply(player, message);
        }
        #endregion

        #region External Calls
        private bool IsClanmate(ulong playerId, ulong friendId)
        {
            object playerTag = Clans?.Call("GetClanOf", playerId);
            object friendTag = Clans?.Call("GetClanOf", friendId);
            if (playerTag is string && friendTag is string)
                if (playerTag == friendTag) return true;
            return false;
        }
        private bool IsFriend(ulong playerId, ulong friendId)
        {
            bool isFriend = (bool)Friends?.Call("IsFriend", playerId, friendId);
            return isFriend;
        }
        private bool IsPlaying(BasePlayer player)
        {
            if (EventManager)
            {
                object isPlaying = EventManager?.Call("isPlaying", new object[] { player });
                if (isPlaying is bool)
                {
                    if ((bool)isPlaying) return true;
                }                
            }
            return false;
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class Exempt
        {
            public bool AdminExempt;
            public bool FriendsExempt;
            public bool ClansExempt;
        }
        class Deductions
        {
            public bool UsePercentageDeduction;
            public float XPDeductionPercentage;
            public float XPDeductionStatic;
        }
        class Options
        {
            public bool GiveDeductedXPToKiller;
        }
        class Messaging
        {
            public string MSG_MainColor;
            public string MSG_Color;
        }
        class ConfigData
        {
            public Exempt Exemptions { get; set; }
            public Deductions Deductions { get; set; }
            public Options Options { get; set; }
            public Messaging Messaging { get; set; }
        }
        
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                Deductions = new Deductions
                {
                    UsePercentageDeduction = true,
                    XPDeductionPercentage = 5,
                    XPDeductionStatic = 50
                },
                Exemptions = new Exempt
                {
                    AdminExempt = false,
                    ClansExempt = false,
                    FriendsExempt = false,
                },
                Options = new Options
                {
                    GiveDeductedXPToKiller = false,
                },
                Messaging = new Messaging
                {
                    MSG_MainColor = "<color=orange>",
                    MSG_Color = "<color=#939393>"
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messaging
        private string MSG(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"lossMessage", "{color}Your have lost {amount} XP for dieing{endcolor}" },
            {"gainMessage", "{color}Your have gained {amount} XP for killing {victimname}" }           
        };
        #endregion
    }
}
