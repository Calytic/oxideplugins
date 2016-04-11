/*
References:
 * System
 * Assembly-CSharp
 * Oxide.Core
 * Oxide.Ext.CSharp
 * OXide.Game.Rust
 * Oxide.Ext.Lua
 * NLua
*/
using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Ext.Lua;
using NLua;

namespace Oxide.Plugins
{
    [Info("Unwound", "mk_sky", "1.0.8", ResourceId = 1352)]
    [Description("The sky presents the newest technology in calling the MEDIC!")]
    class Unwound : RustPlugin
    {
        #region vars
        ListDictionary<string, string> localization;

        List<ulong> called = new List<ulong>();

        ListDictionary<uint, uint> ecoSettings;

        List<ulong> inUnwoundZone = new List<ulong>();

        uint waitTillMedic = 10;

        bool popupsEnabled = false;

        uint chanceTheMedicSavesYou = 100;

        bool canCallMedicOncePerWounded = true;

        bool economicsEnabled = false;
        
        [PluginReference]
        Plugin PopupNotifications;

        [PluginReference]
        Plugin Economics;

        [PluginReference]
        Plugin ZoneManager;
        #endregion

        void OnServerInitialized()
        {
            ConfigLoader();

            if (!permission.PermissionExists("canuseunwound"))
                permission.RegisterPermission("canuseunwound", this);

            if (!permission.GroupHasPermission("admin", "canuseunwound"))
                permission.GrantGroupPermission("admin", "canuseunwound", this);
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();

            Config["Version"] = this.Version.ToString();

            #region localization
            Config["Localization", "PermissionMissing"] = "You have no permission to use this command, if you're wounded right now it means you're probably screwed!";

            Config["Localization", "TheMedicIsComing"] = "The medic is coming for you ... that means if you can survive another {0} seconds.";

            Config["Localization", "NotWounded"] = "You're not wounded, get your extra shots somewhere else!";

            Config["Localization", "Survived"] = "The claws of death failed to claim you this time!";

            Config["Localization", "DontTrollTheMedic"] = "How dare you call the medic and then don't wait for him before staying up again!";

            Config["Localization", "AboutToDie"] = "You are about to die, use /aid to call for a medic.";

            Config["Localization", "MedicToLate"] = "Seems like your medic found some free beer on the way and won't come in time now ... I think we have to cut his salary!";

            Config["Localization", "MedicIncompetent"] = "This incompetent troll of a medic is just to stupid to get you back up, we will get rid of him!";

            Config["Localization", "MedicAlreadyCalled"] = "You already called for a medic, just wait for him.";

            Config["Localization", "NotEnoughMoney"] = "You don't have enough money, how horrible ... You have {0} and you would need {1} so just wait the full {2} seconds for the medic.";
            #endregion
            #region settings
            Config["Settings", "WaitTillMedic"] = 10;

            Config["Settings", "ChanceTheMedicSavesYou"] = 100;

            Config["Settings", "CanCallMedicOncePerWounded"] = true;

            Config["Settings", "EnablePopups"] = false;

            Config["Settings", "EnableEconomics"] = false;

            Config["EcoSettings", "500"] = 0;

            Config["EcoSettings", "250"] = 5;
            #endregion

            SaveConfig();

            PrintWarning("Unwound created new config.");
        }

        void ConfigLoader()
        {
            base.LoadConfig();

            if (Config.Exists() &&
                Config["Version"].ToString() != this.Version.ToString())
                ConfigUpdater();

            #region localization
            localization = new ListDictionary<string, string>();

            localization.Add("PermissionMissing", Config["Localization", "PermissionMissing"].ToString());

            localization.Add("TheMedicIsComing", Config["Localization", "TheMedicIsComing"].ToString());

            localization.Add("NotWounded", Config["Localization", "NotWounded"].ToString());

            localization.Add("Survived", Config["Localization", "Survived"].ToString());

            localization.Add("DontTrollTheMedic", Config["Localization", "DontTrollTheMedic"].ToString());

            localization.Add("AboutToDie", Config["Localization", "AboutToDie"].ToString());

            localization.Add("MedicToLate", Config["Localization", "MedicToLate"].ToString());

            localization.Add("MedicIncompetent", Config["Localization", "MedicIncompetent"].ToString());

            localization.Add("MedicAlreadyCalled", Config["Localization", "MedicAlreadyCalled"].ToString());

            localization.Add("NotEnoughMoney", Config["Localization", "NotEnoughMoney"].ToString());
            #endregion
            #region settings
            waitTillMedic = Convert.ToUInt32(Config["Settings", "WaitTillMedic"]);

            chanceTheMedicSavesYou = Convert.ToUInt32(Config["Settings", "ChanceTheMedicSavesYou"]) > 100 ? 100 : Convert.ToUInt32(Config["Settings", "ChanceTheMedicSavesYou"]);

            if (chanceTheMedicSavesYou == 0)
            {
                PrintError("The ChanceTheMedicSavesYou can't be 0, Plugin will run it with 1."); //still almost 0 but not 0

                chanceTheMedicSavesYou = 1;
            }

            canCallMedicOncePerWounded = Convert.ToBoolean(Config["Settings", "CanCallMedicOncePerWounded"]);

            if (PopupNotifications == null &&
                Convert.ToBoolean(Config["Settings", "EnablePopups"]))
                PrintError("PopupNotifications-Plugin missing, can't enable pop-ups. Get the plugin first: http://oxidemod.org/plugins/popup-notifications.1252/");
            else if (PopupNotifications != null &&
                     Convert.ToBoolean(Config["Settings", "EnablePopups"]))
                popupsEnabled = true;

            if (Convert.ToBoolean(Config["Settings", "EnableEconomics"]) &&
                Economics != null)
            {
                economicsEnabled = true;

                ecoSettings = new ListDictionary<uint, uint>();

                Dictionary<string, string> temp = Config.Get<Dictionary<string, string>>("EcoSettings");

                foreach (KeyValuePair<string, string> s in temp)
                    if (Convert.ToUInt32(s.Value) >= 0)
                        ecoSettings.Add(Convert.ToUInt32(s.Key), Convert.ToUInt32(s.Value));
            }
            else if (Convert.ToBoolean(Config["Settings", "EnableEconomics"]))
                PrintError("Economics-Plugin missing, can't enable economics. Get the plugin first: http://oxidemod.org/plugins/economics.717/");
            #endregion

            Puts("Unwound loaded config.");
        }

        void ConfigUpdater()
        {
            PrintWarning(String.Format("Unwoud updates config from v{0} to v{1}.", Config["Version"].ToString(), this.Version.ToString()));

            while (Config["Version"].ToString() != this.Version.ToString())
                switch (Config["Version"].ToString())
                {
                    #region 1.0.0 => 1.0.1
                    case "1.0.0":
                        Config["Localization", "AboutToDie"] = "You are about to die, use /aid to call for a medic.";

                        Config["Localization", "MedicToLate"] = "Seems like your medic found some free beer on the way and won't come in time now ... I think we have to cut his salary!";

                        Config["Localization", "MedicIncompetent"] = "This incompetent troll of a medic is just to stupid to get you back up, we will get rid of him!";

                        Config["Localization", "MedicAlreadyCalled"] = "You already called for a medic, just wait for him.";

                        Config["Localization", "NotEnoughMoney"] = "You don't have enough money, how horrible ... You have {0} and you would need {1} so just wait the full {2} seconds for the medic.";

                        Config["Settings", "ChanceTheMedicSavesYou"] = 100;

                        Config["Settings", "CanCallMedicOncePerWounded"] = true;

                        Config["Settings", "EnablePopups"] = false;

                        Config["Settings", "EnableEconomics"] = false;

                        Config["EcoSettings", "500"] = 0;

                        Config["EcoSettings", "250"] = 5;

                        Config["Version"] = "1.0.1";
                        break;
                    #endregion
                    #region 1.0.1 || 1.0.2 || 1.0.3 || 1.0.4 || 1.0.5 || 1.0.6 || 1.0.7 => 1.0.8
                    case "1.0.1":
                    case "1.0.2":
                    case "1.0.3":
                    case "1.0.4":
                    case "1.0.5":
                    case "1.0.6":
                    case "1.0.7":
                        Config["Version"] = "1.0.8";
                    break;
                    #endregion
                }

            SaveConfig();
        }

        [ConsoleCommand("unwound.recreate")]
        void ConsoleCommandConfigRecreate()
        {
            LoadDefaultConfig();

            ConfigLoader();
        }

        [ConsoleCommand("unwound.load")]
        void ConsoleCommandConfigLoad()
        {
            ConfigLoader();
        }

        [ConsoleCommand("unwound.set")]
        void ConsoleCommandConfigSet(ConsoleSystem.Arg arg)
        {
            if (IsUInt(arg.GetString(2)))
                Config[arg.GetString(0), arg.GetString(1)] = arg.GetUInt(2);
            else if (arg.GetString(2) == "true" ||
                     arg.GetString(2) == "false")
                Config[arg.GetString(0), arg.GetString(1)] = arg.GetBool(2);
            else
                Config[arg.GetString(0), arg.GetString(1)] = arg.GetString(2);

            SaveConfig();

            ConfigLoader();
        }

        [ChatCommand("aid")]
        void ChatCommandAid(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), "canuseunwound"))
            {
                if (!popupsEnabled)
                    SendReply(player, localization["PermissionMissing"]);
                else
                    PopupNotifications.Call("CreatePopupNotification", localization["PermissionMissing"], player);

                return;
            }
            else if (!player.IsWounded())
            {
                if (!popupsEnabled)
                    SendReply(player, localization["NotWounded"]);
                else
                    PopupNotifications.Call("CreatePopupNotification", localization["NotWounded"], player);
            
                return;
            }
            else if (canCallMedicOncePerWounded &&
                     waitTillMedic > 0 &&
                     !CheckCanCall(player))
                return;

            if (args.Length > 0 &&
                args[0] == "0" ||
                args.Length > 0 &&
                IsUInt(args[0]) &&
                !ecoSettings.Contains(Convert.ToUInt32(args[0])))
                args = new string[0];

            if (waitTillMedic > 0)
            {
                if (Economics != null &&
                    economicsEnabled &&
                    args.Length >= 1 &&
                    IsUInt(args[0]))
                {
                    double playerMoney = (double)Economics.Call("GetPlayerMoney", player.userID);

                    if (playerMoney >= Convert.ToDouble(args[0]))
                    {
                        Economics.Call("Withdraw", player.userID, Convert.ToDouble(args[0]));

                        if (ecoSettings[Convert.ToUInt32(args[0])] > 0)
                        {
                            if (!popupsEnabled)
                                SendReply(player, String.Format(localization["TheMedicIsComing"], ecoSettings[Convert.ToUInt32(args[0])].ToString()));
                            else
                                PopupNotifications.Call("CreatePopupNotification", String.Format(localization["TheMedicIsComing"], ecoSettings[Convert.ToUInt32(args[0])].ToString()), player);

                            Action timed = new Action(() => TimedMedic(player.userID));

                            timer.In(ecoSettings[Convert.ToUInt32(args[0])], timed);
                        }
                        else
                        {
                            called.Remove(player.userID);

                            if (!MedicGetsYouUp(player))
                            {
                                switch (Oxide.Core.Random.Range(0, 1))
                                {
                                    case 0:
                                        if (!popupsEnabled)
                                            SendReply(player, localization["MedicToLate"]);
                                        else
                                            PopupNotifications.Call("CreatePopupNotification", localization["MedicToLate"], player);
                                        break;
                                    case 1:
                                        if (!popupsEnabled)
                                            SendReply(player, localization["MedicIncompetent"]);
                                        else
                                            PopupNotifications.Call("CreatePopupNotification", localization["MedicIncompetent"], player);
                                        break;
                                }

                                return;
                            }

                            player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);

                            player.CancelInvoke("WoundingEnd");

                            player.ChangeHealth(player.StartHealth());

                            player.metabolism.bleeding.value = 0f;

                            if (!popupsEnabled)
                                SendReply(player, localization["Survived"]);
                            else
                                PopupNotifications.Call("CreatePopupNotification", localization["Survived"], player);
                        }
                    }
                    else
                    {
                        if (!popupsEnabled)
                            SendReply(player, String.Format(localization["NotEnoughMoney"], playerMoney.ToString("0"), args[0], waitTillMedic.ToString()));
                        else
                            PopupNotifications.Call("CreatePopupNotification", String.Format(localization["NotEnoughMoney"], playerMoney.ToString("0"), args[0], waitTillMedic.ToString()), player);
                    }
                }
                else
                {
                    if (!popupsEnabled)
                        SendReply(player, String.Format(localization["TheMedicIsComing"], waitTillMedic.ToString()));
                    else
                        PopupNotifications.Call("CreatePopupNotification", String.Format(localization["TheMedicIsComing"], waitTillMedic.ToString()), player);

                    Action timed = new Action(() => TimedMedic(player.userID));

                    timer.In(waitTillMedic, timed);
                }
            }
            else
            {
                if (!MedicGetsYouUp(player))
                {
                    switch (Oxide.Core.Random.Range(0, 1))
                    {
                        case 0:
                            if (!popupsEnabled)
                                SendReply(player, localization["MedicToLate"]);
                            else
                                PopupNotifications.Call("CreatePopupNotification", localization["MedicToLate"], player);
                            break;
                        case 1:
                            if (!popupsEnabled)
                                SendReply(player, localization["MedicIncompetent"]);
                            else
                                PopupNotifications.Call("CreatePopupNotification", localization["MedicIncompetent"], player);
                            break;
                    }

                    return;
                }

                player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);

                player.CancelInvoke("WoundingEnd");

                player.ChangeHealth(player.StartHealth());

                player.metabolism.bleeding.value = 0f;

                if (!popupsEnabled)
                    SendReply(player, localization["Survived"]);
                else
                    PopupNotifications.Call("CreatePopupNotification", localization["Survived"], player);
            }
        }

        void TimedMedic(ulong playerID)
        {
            BasePlayer player = BasePlayer.FindByID(playerID);

            if (player == null)
            {
                PrintWarning(String.Format("Unwound reports that the medic has arrived, but player \"{0}\" does not exist ...", playerID.ToString()));

                return;
            }
            else if (!player.IsConnected())
                return; //lol ragequit ftw
            else if (player.IsDead())
            {
                //TODO: code to enqueue a message for the player 
                return;
            }
            else if (!player.IsWounded())
            {
                if (!popupsEnabled)
                    SendReply(player, localization["DontTrollTheMedic"]);
                else
                    PopupNotifications.Call("CreatePopupNotification", localization["DontTrollTheMedic"], player);

                return;
            }
            else if (!MedicGetsYouUp(player))
            {
                called.Remove(player.userID);

                switch (Oxide.Core.Random.Range(0, 1))
                {
                    case 0:
                        if (!popupsEnabled)
                            SendReply(player, localization["MedicToLate"]);
                        else
                            PopupNotifications.Call("CreatePopupNotification", localization["MedicToLate"], player);
                        break;
                    case 1:
                        if (!popupsEnabled)
                            SendReply(player, localization["MedicIncompetent"]);
                        else
                            PopupNotifications.Call("CreatePopupNotification", localization["MedicIncompetent"], player);
                        break;
                }

                return;
            }

            player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);

            player.CancelInvoke("WoundingEnd");

            player.ChangeHealth(player.StartHealth());

            player.metabolism.bleeding.value = 0f;

            if (!popupsEnabled)
                SendReply(player, localization["Survived"]);
            else
                PopupNotifications.Call("CreatePopupNotification", localization["Survived"], player);
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (hitInfo != null &&
                hitInfo.Initiator != null &&
                hitInfo.Initiator.ToPlayer() != null)
            {
                BasePlayer player = hitInfo.Initiator.ToPlayer();

                if (ZoneManager != null)
                {
                    object canBeWounded = ZoneManager.Call("CanBeWounded", player, hitInfo);

                    if (canBeWounded == null)
                        return;
                }

                TimedExplosive explosion = null;

                if (hitInfo.WeaponPrefab != null &&
                    hitInfo.WeaponPrefab.ToString().Contains("explosive"))
                    explosion = (hitInfo.WeaponPrefab.GetEntity() as TimedExplosive);
                
                if (entity.LookupShortPrefabName().ToString().Contains("player") &&
                    (
                     explosion == null ||
                     explosion.explosionRadius >= player.Distance(entity.GetEntity())
                    )
                   )
                {
                    float totalDamage = (float)0.0;

                    foreach (Rust.DamageType damageType in Enum.GetValues(typeof(Rust.DamageType))) //calculate the damage the player would take
                        try
                        {
                            if (hitInfo.damageTypes.Get(damageType) > (float)0.0)
                                if (hitInfo.damageTypes.Get(damageType) > player.baseProtection.Get(damageType)) //damage of attack surpass player protection
                                    totalDamage += hitInfo.damageTypes.Get(damageType) - player.baseProtection.Get(damageType);
                        }
                        catch
                        { }

                    if (player.health <= totalDamage)
                        if (!popupsEnabled)
                            SendReply(player, localization["AboutToDie"]);
                        else
                            PopupNotifications.Call("CreatePopupNotification", localization["AboutToDie"], player);
                }
            }
        }

        bool CheckCanCall(BasePlayer player)
        {
            if (called.Contains(player.userID))
            {
                if (!popupsEnabled)
                    SendReply(player, localization["MedicAlreadyCalled"]);
                else
                    PopupNotifications.Call("CreatePopupNotification", localization["MedicAlreadyCalled"], player);

                return false;
            }
            else
            {
                called.Add(player.userID);

                return true;
            }
        }

        bool MedicGetsYouUp(BasePlayer player)
        {
            if (chanceTheMedicSavesYou == 100)
                return true;

            uint success = 0;

            //with 100k-test-rounds this seemed pretty accurate ... well 99% chance is as failsafe as 100%, so not perfect but good enough
            
            for (int i = 0; i <= 100; i++)
                success += (uint)Oxide.Core.Random.Range(1, 100);

            success = success % 100;

            return success <= chanceTheMedicSavesYou;
        }

        static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            TimeSpan diff = date.ToUniversalTime() - origin;

            return Math.Floor(diff.TotalSeconds);
        }

        static bool IsUInt(string s)
        {
            try
            {
                Convert.ToUInt32(s);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
