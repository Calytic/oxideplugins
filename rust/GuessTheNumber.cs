using Random=System.Random;
using System;
using Rust.Xp;
using System.Collections.Generic;
using Oxide.Core.Plugins;

namespace Oxide.Plugins {
  
  [Info("Guess The Number", "Dora", "1.2.1", ResourceId = 2023)]
  [Description("Rewards the user with XP when they say the correct number.")]
  
  class GuessTheNumber : RustPlugin {

    Random rng = new Random();
    int number = 0;
    bool hasEconomics = false;
    bool hasServerRewards = false;
    Timer endEventTimer = null;
    Timer autoRepeatTimer = null;

    [PluginReference] Plugin Economics;
    [PluginReference] Plugin ServerRewards;

    public class LimitTries {
      public int attemptedTries = 1;
    }

    public static Dictionary<ulong, LimitTries> playerInfo = new Dictionary<ulong, LimitTries>();

    private void OnServerInitialized() {

      permission.RegisterPermission("GuessTheNumber.startEvent", this);
      LoadVariables();
      LoadDefaultMessages();

      if(configData.autoEventEnabler == true) {
        repeatNumberEvent();
      }

      if(Economics == null) {
        hasEconomics = false;
      } else {
        hasEconomics = true;
      }

      if(ServerRewards == null) {
        hasServerRewards = false;
      } else {
        hasServerRewards = true;
      }

    }

    private void repeatNumberEvent() {
      autoRepeatTimer = timer.Repeat(configData.autoEventInterval, 0, () => GuessNumberEvent(configData.minNumber, configData.maxNumber));
    }

    private void GuessNumberEvent(int minNumber, int maxNumber) {
      if(number == 0) {
        number = rng.Next(minNumber, maxNumber);
        broadcastChat(Lang("pluginPrefix", null), Lang("numberNotice", null, minNumber, (maxNumber - 1)));
        if(configData.autoEndEventEnabler == true) {
          endEventTimer = timer.Once(configData.autoEndEventTimer, () => endEvent());     
        }
      }
    }

    [ChatCommand("startNumber")]
    private void startGuessNumberEvent(BasePlayer player, string cmd, string[] args) {
      int minNumber = 0;
      int maxNumber = 0;

      if(!hasPermission(player, "GuessTheNumber.startEvent")) {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("noPermission", player.UserIDString));
        return;
      }

      if(number != 0) {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("eventStarted", player.UserIDString, number));
        return;
      }

      if(args.Length == 2) {
        Int32.TryParse(args[0], out minNumber);
        Int32.TryParse(args[1], out maxNumber);
        if(minNumber != 0 && maxNumber != 0) {
          number = rng.Next(minNumber, maxNumber);
          broadcastChat(Lang("pluginPrefix", player.UserIDString), Lang("numberNotice", player.UserIDString, minNumber, (maxNumber - 1)));
        } else {
          sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("invalidParam2", player.UserIDString));
          return;
        }
      } else {
        number = rng.Next(configData.minNumber, configData.maxNumber);
        broadcastChat(Lang("pluginPrefix", player.UserIDString), Lang("numberNotice", player.UserIDString, configData.minNumber, (configData.maxNumber - 1)));
      }
 
      sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("winNumber", player.UserIDString, number));
      if(configData.autoEndEventEnabler == true) {
        endEventTimer = timer.Once(configData.autoEndEventTimer, () => endEvent());     
      }
    }

    [ChatCommand("endNumber")]
    private void stopGuessNumberEvent(BasePlayer player) {
      if(!hasPermission(player, "GuessTheNumber.startEvent")) {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("noPermission", player.UserIDString));
        return;
      }

      if(number == 0) {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("eventNotStarted", player.UserIDString));
        return;
      }

      broadcastChat(Lang("pluginPrefix", player.UserIDString), Lang("eventForcedEnd", player.UserIDString, player.displayName, number));
      number = 0;
      playerInfo.Clear();
      if(endEventTimer != null && !endEventTimer.Destroyed) {
        endEventTimer.Destroy();
      }
    }

    [ChatCommand("number")]
    private void numberReply(BasePlayer player, string cmd, string[] args) {
      if(number == 0) {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("eventNotStarted", player.UserIDString));
        return;
      }

      if(args.Length == 1) {
        
        int playerNum;
        Int32.TryParse(args[0], out playerNum);

        if(configData.maxAttemptsEnabler == true) {
          if(playerInfo.ContainsKey(player.userID)) {
            if(playerInfo[player.userID].attemptedTries == configData.maxAttempts) {
              sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("maxAttempts", player.UserIDString));
              return;
            } else {
              playerInfo[player.userID].attemptedTries += 1;
            }    
          } else {
            playerInfo.Add(player.userID, new LimitTries());
          }
        }
        
        if(playerNum == number) {
          
          float xpToGive;
          bool showEconomics = false;
          bool showServerRewards = false;
          bool showXP = false;
          
          if(configData.xpPercentEnabler == true) {
            xpToGive = player.xp.UnspentXp * (float) configData.xpPercentToGive;
          } else {
            xpToGive = configData.xpToGive;
          }
          
          if(configData.xpEnabler == true) {
            player.xp.Add(Definitions.Cheat, xpToGive); 
            showXP = true;
          } 

          if(hasEconomics == true) {
            if(configData.economicsEnabler == true) {
              Economics.CallHook("Deposit", player.userID, configData.economicsWinReward);
              showEconomics = true;
            }
          }

          if(hasServerRewards == true) {
            if(configData.serverRewardsEnabler == true) {
              ServerRewards?.Call("AddPoints", new object[] {player.userID, configData.serverRewardsPoints});
              showServerRewards = true;
            }
          }

          string xpMsg = showXP ? Lang("eventWonExperiences", player.UserIDString, xpToGive) : "";
          string economicsMsg = showEconomics ? Lang("eventWonEconomics", player.UserIDString, configData.economicsWinReward) : "";
          string serverRewardsMsg = showServerRewards ? Lang("eventWonServerRewards", player.UserIDString, configData.serverRewardsPoints) : "";
          broadcastChat(Lang("pluginPrefix", player.UserIDString), Lang("eventWon", player.UserIDString, player.displayName, number) + xpMsg + economicsMsg + serverRewardsMsg);

          number = 0;
          playerInfo.Clear();
          if(endEventTimer != null && !endEventTimer.Destroyed) {
            endEventTimer.Destroy();
          }
        
        } else {
          sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("wrongNumber", player.UserIDString));
        }

      } else {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("invalidParam", player.UserIDString));
        return;
      }
    }

    private void endEvent() {
      broadcastChat(Lang("pluginPrefix", null), Lang("autoEventEnd", null, number));
      number = 0;
      playerInfo.Clear();
      if(endEventTimer != null && !endEventTimer.Destroyed) {
        endEventTimer.Destroy();
      } 
    }

    private void sendChatMessage(BasePlayer player, string prefix, string msg) {
      SendReply(player, prefix + ": " + msg);
    }

    private void broadcastChat(string prefix, string msg) {
      PrintToChat(prefix + ": " + msg);
    }

    private bool hasPermission(BasePlayer player, string perm) {
      if(player.net.connection.authLevel > 1) {
        return true;
      }
      return permission.UserHasPermission(player.userID.ToString(), perm);
    }

    private ConfigData configData;
    class ConfigData {
      public bool autoEventEnabler { get; set; }
      public int autoEventInterval { get; set; }

      public int minNumber { get; set; }
      public int maxNumber { get; set; }

      public bool xpEnabler { get; set; }
      public bool xpPercentEnabler { get; set; }
      public float xpPercentToGive { get; set; }
      public int xpToGive { get; set; }
      
      public bool economicsEnabler { get; set; }
      public int economicsWinReward { get; set; }
      
      public bool serverRewardsEnabler { get; set; }
      public int serverRewardsPoints { get; set; }

      public bool maxAttemptsEnabler { get; set; }
      public int maxAttempts { get; set; }
      
      public bool autoEndEventEnabler { get; set; }
      public int autoEndEventTimer { get; set; }

    }

    private void LoadVariables() {
      LoadConfigVariables();
      SaveConfig();
    }

    protected override void LoadDefaultConfig() {
      Config.Clear();
      Config["autoEventEnabler"] = true;
      Config["autoEventInterval"] = 1800;

      Config["minNumber"] = 1;
      Config["maxNumber"] = 101;

      Config["xpEnabler"] = true;
      Config["xpPercentEnabler"] = false;
      Config["xpPercentToGive"] = 0.10;
      Config["xpToGive"] = 10;

      Config["economicsEnabler"] = false;
      Config["economicsWinReward"] = 100;

      Config["serverRewardsEnabler"] = false;
      Config["serverRewardsPoints"] = 5;

      Config["maxAttemptsEnabler"] = true;
      Config["maxAttempts"] = 10;

      Config["autoEndEventEnabler"] = true;
      Config["autoEndEventTimer"] = 300;
      SaveConfig();
    }

    void LoadDefaultMessages() {
      lang.RegisterMessages(new Dictionary<string, string> {
        ["pluginPrefix"] = "<color=orange>Guess The Number</color>",
        ["wrongNumber"] = "Ops, that is not the correct number!",
        ["invalidParam"] = "Invalid Parameter - /number <number>",
        ["invalidParam2"] = "Invalid Parameters - /startNumber <minNumber> <maxNumber>",
        ["noPermission"] = "You do not have the permission to use this.",
        ["winNumber"] = "Winning number: <color=orange>{0}</color>",
        ["numberNotice"] = "Event has started, guess a number! ({0} - {1}). Reply using /number <number>",
        ["maxAttempts"] = "You have reached the maximum attempts to guess a number.",
        ["autoEventEnd"] = "Event has auto-ended due to time limit. The winning number was <color=orange>{0}</color>.",
        ["eventWon"] = "<color=orange>{0}</color> won!\nThe winning number was <color=orange>{1}</color>.",
        ["eventNotStarted"] = "Event has not started.",
        ["eventForcedEnd"] = "Event has been ended by <color=orange>{0}</color>. The winning number was <color=orange>{1}</color>.",
        ["eventStarted"] = "Someone has already started the event.\nWinning number: <color=orange>{0}</color>\n/endNumber - To end the event forcefully.",
        ["eventWonEconomics"] = "\n<color=orange>${0}</color> to has been added to his balance.",
        ["eventWonServerRewards"] = "\n<color=orange>{0}</color> server points has been added.",
        ["eventWonExperiences"] = "\n<color=orange>{0}</color> experiences has been awarded."
      }, this);
    }

    string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

    private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
    void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

  }
}
