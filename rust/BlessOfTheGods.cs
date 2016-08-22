using System;
using UnityEngine;  
using Random=System.Random;
using Rust;
using Rust.Xp;
using System.Collections.Generic;

namespace Oxide.Plugins {
  
  [Info("Blessing of the Gods", "Dora", "1.0.4", ResourceId = 2022)]
  [Description("Player get blessed by the gods.")]
  
  class BlessOfTheGods : RustPlugin {

  	Random rng = new Random();
  	int number;

    void LoadDefaultMessages() {
      lang.RegisterMessages(new Dictionary<string, string> {
        ["blessNotice"] = ": It is time for the blessing!",
        ["noPermission"] = ": You do not have the permission to use this.",
        ["blessHelp"] = "\n<color=green>God of Life</color> - Recover 100% HP\n<color=red>God of Damnation</color> - Take away 25% of your current HP\n<color=orange>God of Gluttony</color> - Recover 100% Food & Water\n<color=grey>God of Poverty</color> - Does nothing\n<color=black>God of Death</color> - Instant KO\n<color=purple>God of Plague</color> - Poison you slowly\n<color=yellow>God of War</color> - Give 10% extra XP based on current XP",
        ["blessList"] = ": Invalid blessing.\n<color=green>God of Life</color> - /bless <name> hp\n<color=red>God of Damnation</color> - /bless <name> damn\n<color=orange>God of Gluttony</color> - /bless <name> food\n<color=grey>God of Poverty</color> - /bless <name> poverty\n<color=black>God of Death</color> - /bless <name> death\n<color=purple>God of Plague</color> - /bless <name> poison\n<color=yellow>God of War</color> - /bless <name> xp",
        ["playerNotFound"] = ": Player not found.",
        ["invalidParams"] = ": Invalid parameters. /bless <name> <blessing>",
        ["godOfLife"] = "<color=green>God of Life</color> has blessed you with full health recovery.",
        ["godOfDamn"] = "<color=red>God of Damnation</color> has damned you by taking away 25% of your health.",
        ["godOfGluttony"] = "<color=orange>God of Gluttony</color> has filled your belly with great food & wine.",
        ["godOfPoverty"] = "<color=grey>God of Poverty</color> has looked at you and decided there is nothing he can give you.",
        ["godOfDeath"] = "<color=black>God of Death</color> has claimed {0}'s life...",
        ["godOfPlague"] = "<color=purple>God of Plague</color> has defiled you with slow poison.",
        ["godOfWar"] = "<color=yellow>God of War</color> has bestowed {0} with war experiences.",
        ["pluginPrefix"] = "<color=orange>Blessing of The Gods</color>"
      }, this);
    }

    private ConfigData configData;
    class ConfigData {
      public int repeatInterval { get; set; }
      public bool autoBless { get; set; }
      public int godOfGluttonyFood { get; set; }
      public int godOfGluttonyWater { get; set; }
      public float godOfDamnHPDeduction { get; set; }
      public int godOfPlaguePoison { get; set; }
      public bool godOfWarPercentEnabler { get; set; }
      public float godOfWarPercentXP { get; set; }
      public int godOfWarFixedXP { get; set; }
      public bool enableGodOfLife { get; set; }
      public bool enableGodOfDamn { get; set; }
      public bool enableGodOfGluttony { get; set; }
      public bool enableGodOfPlague { get; set; }
      public bool enableGodOfWar { get; set; }
      public bool enableGodOfDeath { get; set; }
    }

    private void LoadVariables() {
      LoadConfigVariables();
      SaveConfig();
    }

    protected override void LoadDefaultConfig() {
      Config.Clear();
      Config["repeatInterval"] = 900;
      Config["autoBless"] = true;
      Config["godOfDamnHPDeduction"] = 0.75;
      Config["godOfGluttonyWater"] = 500;
      Config["godOfGluttonyFood"] = 500;
      Config["godOfPlaguePoison"] = 20;
      Config["godOfWarPercentEnabler"] = true;
      Config["godOfWarPercentXP"] = 0.10;
      Config["godOfWarFixedXP"] = 10;
      Config["enableGodOfLife"] = true;
      Config["enableGodOfDamn"] = true;
      Config["enableGodOfGluttony"] = true;
      Config["enableGodOfWar"] = true;
      Config["enableGodOfDeath"] = true;
      SaveConfig();
    }

    private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
    private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

		private void OnServerInitialized() {
      permission.RegisterPermission("blessofthegods.bless", this);
      permission.RegisterPermission("blessofthegods.rebless", this);
      LoadDefaultMessages();
      LoadVariables();

      if(configData.autoBless == true) {
        repeatBless();
      }
		}

		private void repeatBless() {
			timer.Repeat(configData.repeatInterval, 0, () => blessPlayers());
		}

		private void blessPlayers() {
      broadcastChat(Lang("pluginPrefix", null), Lang("blessNotice", null));
      foreach (BasePlayer current in BasePlayer.activePlayerList) {
      	number = rng.Next(1,101);

      	if(number >= 86) {
          if(configData.enableGodOfLife) {
            godOfLife(current);
          } else {
            godOfPoverty(current);
          }
      	} else if(number >= 76 && number <= 85) {
          if(configData.enableGodOfDamn) {
            godOfDamn(current);
          } else {
            godOfPoverty(current);
          }
      	} else if(number >= 61  && number <= 75) {
          if(configData.enableGodOfGluttony) {
            godOfGluttony(current);
          } else {
            godOfPoverty(current);
          }
      	} else if(number >= 51 && number <= 60) {
          if(configData.enableGodOfPlague) {
            godOfPlague(current);
          } else {
            godOfPoverty(current);
          }
      	} else if(number >= 46 && number <= 50) {
          if(configData.enableGodOfWar) {
            godOfWar(current);
          } else {
            godOfPoverty(current);
          }
      	} else if(number >= 2 && number <= 45) {
      		godOfPoverty(current);
      	} else if(number == 1) {
          if(configData.enableGodOfDeath) {
            godOfDeath(current);
          } else {
            godOfPoverty(current);
          }
      	}
      }
		}

		private void godOfLife(BasePlayer player) {
			player.InitializeHealth(100,100);
      sendChatMessage(player, null, Lang("godOfLife", player.UserIDString));
		}

		private void godOfDamn(BasePlayer player) {
      float deductedHP = player.health * (float) configData.godOfDamnHPDeduction;
      player.InitializeHealth(deductedHP, 100);
      sendChatMessage(player, null, Lang("godOfDamn", player.UserIDString));
		}

		private void godOfGluttony(BasePlayer player) {
			player.metabolism.hydration.value = configData.godOfGluttonyWater;
      player.metabolism.calories.value = configData.godOfGluttonyFood;
      sendChatMessage(player, null, Lang("godOfGluttony", player.UserIDString));
		}

		private void godOfPoverty(BasePlayer player) {
      sendChatMessage(player, null, Lang("godOfPoverty", player.UserIDString)); 
		}

		private void godOfDeath(BasePlayer player) {
    	player.Die();
      broadcastChat(null, Lang("godOfDeath", player.UserIDString, player.displayName));	
		}

		private void godOfPlague(BasePlayer player) {
			player.metabolism.poison.value = configData.godOfPlaguePoison;
      sendChatMessage(player, null, Lang("godOfPlague", player.UserIDString));
		}

		private void godOfWar(BasePlayer player) {
      float amtToAdd;
      if(configData.godOfWarPercentEnabler == true) {
          amtToAdd = player.xp.UnspentXp * (float) configData.godOfWarPercentXP;
        } else {
          amtToAdd = configData.godOfWarFixedXP;
        }
      player.xp.Add(Definitions.Cheat, amtToAdd);
      broadcastChat(null, Lang("godOfWar", player.UserIDString, player.displayName));
		}
    
    [ChatCommand("rebless")]
    private void blessPlayer(BasePlayer player) {      
    	if(!hasPermission(player, "blessofthegods.rebless")) {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("noPermission", player.UserIDString));
    		return;
    	}
    	blessPlayers();
    }

    [ChatCommand("blesshelp")]
    private void blessHelp(BasePlayer player) {
    	sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("blessHelp", player.UserIDString));
    }

    [ChatCommand("bless")]
    private void blessing(BasePlayer player, string command, string[] args) {
      if(!hasPermission(player, "blessofthegods.bless")) {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("noPermission", player.UserIDString));
        return;
      }

      if(args.Length < 2) {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("invalidParams", player.UserIDString));
        return;
      }

      BasePlayer targetPlayer = getPlayerName(args[0]);
      if(targetPlayer == null) {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("playerNotFound", player.UserIDString));
        return;
      }

      if(args[1] == "hp") {
        godOfLife(targetPlayer);
      } else if(args[1] == "damn") {
        godOfDamn(targetPlayer);
      } else if(args[1] == "food") {
        godOfGluttony(targetPlayer);
      } else if(args[1] == "death") {
        godOfDeath(targetPlayer);
      } else if(args[1] == "xp") {
        godOfWar(targetPlayer);
      } else if(args[1] == "poison") {
        godOfPlague(targetPlayer);
      } else if(args[1] == "poverty") {
        godOfPoverty(targetPlayer);
      } else {
        sendChatMessage(player, Lang("pluginPrefix", player.UserIDString), Lang("blessList", player.UserIDString));
      }
    }

    private void sendChatMessage(BasePlayer player, string prefix, string msg) {
      SendReply(player, prefix + msg);
    }

    private void broadcastChat(string prefix, string msg) {
      PrintToChat(prefix +  msg);
    }

    private void broadcastSuccess(BasePlayer player, String godName, String action, String msg, string color) {
    	PrintToChat("<color=" + color + ">" + godName + "</color> " + action + " <color=orange>" + player.displayName + "</color> " + msg);
    }

    private void sendToPlayer(BasePlayer player, String godName, String action, String msg, string color) {
    	SendReply(player, "<color=" + color + ">" + godName + "</color> " + action + " " + msg);
    }

    string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

    private BasePlayer getPlayerName(string name) {
      string currentName;
      string lastName;
      BasePlayer foundPlayer = null;
      name = name.ToLower();
    
      foreach(BasePlayer player in BasePlayer.activePlayerList) {
        currentName = player.displayName.ToLower();
        
        if(currentName.Contains(name)) {
          if(foundPlayer != null) {
            lastName = foundPlayer.displayName;  
            if(currentName.Replace(name, "").Length < lastName.Replace(name, "").Length) {
              foundPlayer = player;
            }
          }  
          foundPlayer = player;
        }
      }
      return foundPlayer;
    }

    private bool hasPermission(BasePlayer player, string perm) {
      if(player.net.connection.authLevel > 1) {
        return true;
      }
      return permission.UserHasPermission(player.userID.ToString(), perm);
    }

  }
}
