using CodeHatch.Blocks.Networking.Events;
using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("WarTime", "D-Kay", "1.1.0")]
    public class WarTime : ReignOfKingsPlugin {

        #region Configuration Data
        private bool adminSiegeException => GetConfig("adminSiegeException", false);
        private int banTime => GetConfig("BanTime", 1);
        private int Peacetime => GetConfig("Peacetime", 23);
        private string punish => GetConfig("Punish", "ban");
        private bool usingRealTime => GetConfig("UsingRealtime", false);
        private int Wartime => GetConfig("Wartime", 9);
        private bool warOn = false;
        #endregion

        #region Config save/load
        private void Loaded()
        {
            warOn = GetConfig("WarOn", false);
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission("WarTime.Toggle", this);
            permission.RegisterPermission("WarTime.Exception", this);
            if (usingRealTime) timer.Repeat(1, 0, CheckTime);
        }

        protected override void LoadDefaultConfig()
        {
            Config["adminSiegeException"] = adminSiegeException;
            Config["BanTime"] = banTime;
            Config["Peacetime"] = Peacetime;
            Config["Punish"] = punish;
            Config["UsingRealtime"] = usingRealTime;
            Config["Wartime"] = Wartime;
            Config["WarOn"] = warOn;
            SaveConfig();
        }

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "ChatPrefix", "[950415]WarTime[ffffff]: " },
                { "PeaceTime", "It is now a time of Peace! Do not siege!" },
                { "WarTime", "It is now a time of War! You may now siege!" },
                { "SiegeUsed", "A player used a siege weapon during Peace Times! The player was kicked from the server!" },
                { "BaseSieged", "A base was sieged during Peace Time. The attacker was kicked from the server!" },
                { "PunishReason", "Sieging during Peace Times!" },
                { "ToggleWhileUsingRealTime", "Toggle is disabled while RealTime is being used." }
            }, this);
        }
        #endregion

        #region Commands
        [ChatCommand("wartime")]
        private void WarTimeCommand(Player player)
        {
			if (player.HasPermission("WarTime.Toggle")) {
                if (usingRealTime) { PrintToChat(player, GetMessage("ChatPrefix", player.Id.ToString()) + GetMessage("ToggleWhileUsingRealTime", player.Id.ToString())); return; }
                if (warOn) { PrintToChat(GetMessage("ChatPrefix", player.Id.ToString()) + GetMessage("PeaceTime", player.Id.ToString())); warOn = false; }
                else { PrintToChat(GetMessage("ChatPrefix", player.Id.ToString()) + GetMessage("WarTime", player.Id.ToString())); warOn = true; }
                UpdateConfig();
			}
        }
		
		[ChatCommand("checkwartime")]
        private void CheckWarTimeCommand(Player player)
        {
			if (warOn) PrintToChat(player, GetMessage("ChatPrefix", player.Id.ToString()) + GetMessage("WarTime", player.Id.ToString())); 
            else PrintToChat(player, GetMessage("ChatPrefix", player.Id.ToString()) + GetMessage("PeaceTime", player.Id.ToString()));
        }
        #endregion

        #region Hooks
        private void OnCubeTakeDamage(CubeDamageEvent e)
        {
            string damageSource = e.Damage.Damager.name.ToString();
            if (!warOn)
            {
                if (e.Damage.DamageSource.Owner is Player)
                {
                    Player player = e.Damage.DamageSource.Owner;
                    if (adminSiegeException)
                        if (player.HasPermission("WarTime.Exception")) return;
                    if (damageSource.Contains("Trebuchet") || damageSource.Contains("Ballista"))
                    {
                        e.Cancel(GetMessage("PunishReason"));
                        e.Damage.Amount = 0f;
                        PrintToChat(GetMessage("ChatPrefix") + GetMessage("BaseSieged"));
                        if (punish == "kick") Server.Kick(player, GetMessage("PunishReason"));
                        if (punish == "ban") Server.Ban(player, banTime, GetMessage("PunishReason"));
                    }
                }
                return;
            }
        }

        private void OnEntityHealthChange(EntityDamageEvent e)
        {
            string damageSource = e.Damage.Damager.name.ToString();
            if (!warOn)
            {
                if (e.Damage.DamageSource.Owner is Player && !(e.Entity is Player))
                {
                        Player player = e.Damage.DamageSource.Owner;
                    if (adminSiegeException)
                        if (player.HasPermission("WarTime.Exception")) return;
                    if (damageSource.Contains("Trebuchet") || damageSource.Contains("Ballista"))
                    {
                        e.Cancel(GetMessage("PunishReason"));
                        e.Damage.Amount = 0f;
                        PrintToChat(GetMessage("ChatPrefix") + GetMessage("SiegeUsed"));
                        if (punish == "kick") Server.Kick(player, GetMessage("PunishReason"));
                        if (punish == "ban") Server.Ban(player, banTime, GetMessage("PunishReason"));
                    }
                }
                return;
            }
        }
        #endregion

        #region Functions
        private void UpdateConfig()
        {
            Config["WarOn"] = warOn;
            SaveConfig();
        }

        private void CheckTime()
        {
            bool Check = warOn;
            if (DateTime.Now.Hour >= Peacetime || DateTime.Now.Hour < Wartime)
            {
                warOn = false;
                if (warOn != Check) PrintToChat(GetMessage("PeaceTime"));
            }
            else
            {
                warOn = true;
                if (warOn != Check) PrintToChat(GetMessage("WarTime"));
            }
            UpdateConfig();
        }

        private void Warning(string msg) => PrintWarning($"{Title} : {msg}");
        #endregion

        #region Helpers
        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
        #endregion
    }
}
