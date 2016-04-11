var titles = {
    Title: "Titles",
    Author: "Killparadise",
    Version: V(0, 1, 1),
    HasConfig: true,
    Init: function() {
        this.loadTitleData();
    },
    OnServerInitialized: function() {
        print(this.Title + " Is now loading, please wait...");
        //command.AddChatCommand("show", this.Plugin, "cmdShowPlayer");
    },

    LoadDefaultConfig: function() {
        this.Config.authLevel = 2;
        this.Config.Settings = {
            "showPlayer": true,
            "groupsSet": false,
            "displayLvl": true
        };
        this.Config.Titles = {
            "Admin": "Admin",
            "Moderator": "Mod",
            "Player": "Player"
        };
    },
    cmdShowPlayer: function(player, cmd, args) {
        if (authLevel >= this.Config.authLevel) {
            if (!showPlayer) {
                showPlayer = true;
                for (var i = 0; i < authConfig.PlayerData.length; i++) {
                    if (authConfig.PlayerData[i].Title === this.Config.Titles.Player) {
                        player.displayName = authConfig.PlayerData[i].RealName + " " + "[" + authConfig.PlayerData[i].Title + "]";
                        rust.SendChatMessage(player, "TITLES", "Successfully turned on titles for Players", "0");
                        print("Returned Users with 'Player' " + authConfig.PlayerData[i]);
                    } else {
                        print("Returned No Users with 'Player' " + authConfig.PlayerData[i]);
                        return false;
                    }
                }
            } else {
                showPlayer = false;
                for (var ii = 0; ii < authConfig.PlayerData.length; ii++) {
                    if (authConfig.PlayerData[ii].Title === this.Config.Titles.Player) {
                        player.displayName = authConfig.PlayerData[ii].RealName;
                        rust.SendChatMessage(player, "TITLES", "Successfully turned off titles for Player", "0");
                        print("Returned Users with 'Player' " + authConfig.PlayerData[ii].Title);
                    } else {
                        print("Returned No Users with 'Player' " + authConfig.PlayerData[ii].Title);
                        return false;
                    }
                }
            }
        } else {
            rust.SendChatMessage(player, "TITLES", "You do not have permission to use this command!", "0");
        }
    },


    loadTitleData: function() {
        authConfig = data.GetData('Titles');
        authConfig = authConfig || {};
        authConfig.PlayerData = authConfig.PlayerData || {};
    },


    checkPlayerData: function(player) {
        // pID = rust.UserIDFromPlayer( player );
        authConfig.PlayerData[pID] = authConfig.PlayerData[pID] || {};
        authConfig.PlayerData[pID].RealName = authConfig.PlayerData[pID].RealName || player.displayName || "";
        authConfig.PlayerData[pID].Title = authConfig.PlayerData[pID].Title || "";
        authConfig.PlayerData[pID].authLvl = authConfig.PlayerData[pID].authLvl || authLevel || "";
        showPlayer = this.Config.Settings.showPlayer;
        //groupsSet = this.checkForGroups(player, pID) || false;
        this.setTitle(player, showPlayer, pID);
    },


    saveTitleData: function() {
        data.SaveData('Titles');
    },

    setTitle: function(player, showPlayer, pID) {
        //Get our title and name for our data
        authName = authConfig.PlayerData[pID].Title;
        realName = authConfig.PlayerData[pID].RealName;

        switch (authLevel) {
            case 0:
                authConfig.PlayerData[pID].Title = this.Config.Titles.Player;
                if (showPlayer) {
                    player.displayName = authConfig.PlayerData[pID].RealName + " " + "[" + authConfig.PlayerData[pID].Title + "]";
                } else {
                    player.displayName = authConfig.PlayerData[pID].RealName;
                    return false;
                }
                break;
            case 1:
                authConfig.PlayerData[pID].Title = this.Config.Titles.Moderator;
                player.displayName = authConfig.PlayerData[pID].RealName + " " + "[" + authConfig.PlayerData[pID].Title + "]";
                if (this.Config.Settings.displayLvl) rust.SendChatMessage(player, "Titles", "Your title is: " +this.Config.Titles.Moderator, "0");
                break;
            case 2:
                authConfig.PlayerData[pID].Title = this.Config.Titles.Admin;
                player.displayName = authConfig.PlayerData[pID].RealName + " " + "[" + authConfig.PlayerData[pID].Title + "]";
                if (this.Config.Settings.displayLvl) rust.SendChatMessage(player, "Titles", "Your title is: " +this.Config.Titles.Admin, "0");
                break;
            default:
                authConfig.PlayerData[pID].Title = "Not Found";
                player.displayName = authConfig.PlayerData[pID].RealName;
                break;
        }
        this.saveTitleData();
    },

    checkForGroups: function(player, pID) {
        var GroupsDataExists = data.GetData("Groups") ? true : false;

        if (GroupsDataExists) {
            GroupsData = data.GetData("Groups");
            pData = GroupsData.PlayerData[pID];
            authConfig.PlayerData[pID].RealName = pData.RealName;
            groupsDisplayName = player.displayName;
            return true;
        } else {
            print("Groups Data not found, no need to grab anything.");
            return false;
        }
    },

    OnPlayerInit: function(player) {
        pID = rust.UserIDFromPlayer(player);
        authLevel = player.net.connection.authLevel;
        this.checkPlayerData(player);
    }

}
