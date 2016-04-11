var rules = {
        Title: "Rules",
        Author: "Killparadise",
        Version: V(1, 5, 0),
        HasConfig: true,
        Init: function() {
            notifier = plugins.Find("notifier");
                if (notifier) {
                    print('Loaded Rules from Config: ' + this.Config.setRules.length);
                    notifierFound = true;
                } else {
                    print('Loaded Rules from Config: ' + this.Config.setRules.length);
                    command.AddChatCommand("rules", this.Plugin, "switchRules");
                    notifierFound = false;
                }

                if (this.Config.settings.repeatDisplay) {
                    var rules = this.Config.setRules;
                    this.rulesDelay = timer.Repeat(this.Config.settings.interval, 0, function() {
                        rust.BroadcastChat('', "---------Server Rules--------", 0);
                        for (var i = 0; i < rules.length; i++) {
                            rust.BroadcastChat('RULES', rules[i], 0)
                        }
                    }, this.Plugin);
                }
        },

        LoadDefaultConfig: function() {
            print("Config Changes with new update, Updating Config file.");
            this.Config.setRules = [
                "1. No Cheating!",
                "2. No Abusing broken mechanics!",
                "3. Respect thy fellow players"
            ];

            this.Config.RulesFr = [
                "1. place french translations here.",
            ];

            this.Config.RulesSp = [
                "1. place Spanish translations here."
            ];

            this.Config.settings = {
                "authLvl": 2,
                "repeatDisplay": true,
                "welcomeMsg": true,
                "interval": 200
            }

            this.Config.Messages = {
                "AddBadSyntax": ['Incorrect Syntax, please use: /rules add "rule here" Example: /rules add "Example Rule Here"'],
                "DelBadSyntax": ["Incorrect Syntax, please use:", "/rules del ruleNumber", "Example: /rules del 1"],
                "Default": ["No Permissions to use this command."],
                "welcome": "Welcome {player} to our server!",
                "badSyntaxGen": "Incorrect Syntax please try again.",
                "title": "---------Server Rules--------"
            }
        },

        OnPlayerInit: function(player) {
            if (!notifierFound) {
                try {
                    if (this.Config.settings.welcomeMsg) {
                        var msg = this.Config.Messages.welcome.toString();
                        msg = msg.replace("{player}", player.displayName);
                        rust.SendChatMessage(player, "RULES", msg, "0");
                        this.cmdShowRules(player);
                    }
                } catch (e) {
                    print(e.message.toString());
                }
            }
        },



        /*-----------------------------------------------------------------
                         Commands for Rules
        ------------------------------------------------------------------*/

        switchRules: function(player, cmd, args) {
            if (args.length >= 0) {
                switch (args[0]) {
                    case "add":
                        this.cmdAddRule(player, cmd, args);
                        break;
                    case "del":
                        this.cmdDelRule(player, cmd, args);
                        break;
                    case "fr":
                        this.cmdShowRules(player, "fr");
                        break;
                    case "sp":
                        this.cmdShowRules(player, "sp");
                        break;
                    default:
                        var rules = this.Config.setRules;
                        rust.SendChatMessage(player, '', this.Config.Messages.title, 0);
                        for (var i = 0; i < rules.length; i++) {
                            rust.SendChatMessage(player, "RULES", rules[i], "0");
                        }
                        break;
                }
            } else {
                rust.SendChatMessage(player, "RULES", this.Config.Messages.badSyntaxGen, "0");
            }
          },

            cmdShowRules: function(player, lang) {
                      if (lang === "fr") {
                        var rules = this.Config.RulesFr;
                        rust.SendChatMessage(player, '', this.Config.Messages.title, 0);
                        for (var i = 0; i < rules.length; i++) {
                            rust.SendChatMessage(player, "RULES", rules[i], "0");
                        }
                      } else if (lang === "sp") {
                        var rules = this.Config.RulesSp;
                        rust.SendChatMessage(player, '', this.Config.Messages.title, 0);
                        for (var i = 0; i < rules.length; i++) {
                            rust.SendChatMessage(player, "RULES", rules[i], "0");
                        }
                      }
                },

                cmdAddRule: function(player, cmd, args) {
                    var authLvl = player.net.connection.authLevel;
                    var rules = this.Config.setRules;
                    var rulLen = rules.length + 1;

                    if (args.length < 2) {
                        for (var j = 0; j < this.Config.Messages.AddBadSyntax.length; j++) {
                            rust.SendChatMessage(player, "RULES", this.Config.Messages.AddBadSyntax[j], "0");
                            return;
                        }
                    } else {

                        if (authLvl >= 2 && args[1].length) {
                            rules.push(rulLen + "." + " " + args[1]);
                            rust.SendChatMessage(player, "RULES", "Rule added successfully.", "0")
                            this.SaveConfig();
                        } else if (authLvl <= 1) {
                            rust.SendChatMessage(player, "RULES", "No Permissions to use this command.", "0");
                        }
                    }
                },

                cmdDelRule: function(player, cmd, args) {
                    var authLvl = player.net.connection.authLevel;
                    var rules = this.Config.setRules;
                    var tempSave = [];
                    if (authLvl >= 2 && args.length < 2) {
                        for (var j = 0; j < this.Config.Messages.DelBadSyntax.length; j++) {
                            rust.SendChatMessage(player, "RULES", this.Config.Messages.DelBadSyntax[j], "0");
                            return;
                        }
                    } else if (authLvl >= 2 && args.length >= 2) {
                        for (var i = 0; i < rules.length; i++) {
                            try {
                                if (rules.indexOf(rules[i]) != (args[1] - 1)) {
                                    tempSave.push(rules[i]);
                                } else {
                                    continue;
                                }
                            } catch (e) {
                                print(e.message.toString());
                            }
                        }
                        this.Config.setRules = [];
                        for (var ii = 0; ii < tempSave.length; ii++) {
                            this.Config.setRules.push(tempSave[ii]);
                        }
                        rust.SendChatMessage(player, "RULES", "Rule Deleted Successfully", "0");
                        this.SaveConfig();
                        return;
                    } else {
                        rust.SendChatMessage(player, "RULES", "No Permissions to use this command.", "0");
                    }
                },

                SendHelpText: function(player) {
                    var authLvl = player.net.connection.authLevel;
                    rust.SendChatMessage(player, "RULES", "/rules - Show the list of server rules", "0");
                    if (authLvl >= 2) {
                        rust.SendChatMessage(player, "RULES", '/rules add "Rule in quotes here" - Add a new rule to the list', "0");
                        rust.SendChatMessage(player, "RULES", "/rules del # - removes the rule listed with given number", "0");
                    }
                }
        };
