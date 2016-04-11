var MageMotd = {
    Title : "Mage MOTD",
    Author : "Risin",
    Version : V(1, 0, 0),
    HasConfig : true,
    Init : function () {
        print("Mage MOTD: Starting...");
    },
    OnServerInitialized : function () {
        command.AddConsoleCommand("motd.test", this.Plugin, "showMotd");
        command.AddChatCommand("motd.change", this.Plugin, "changeMotd");
    },
    LoadDefaultConfig : function () {
        this.Config.authLevel = 1;
        this.Config = {"motd" : "Some Random MOTD, you should probably change me."};
    },
    OnPluginLoaded : function () {
        print("Mage MOTD: Loaded!");
    },
    OnPlayerInit: function(player) {
        rust.SendChatMessage(player, this.title, this.Config.motd, 0)
    },
    changeMotd : function (player, cmd, args) {
        if (player.net.connection.authLevel >= 2) {
            var new_motd = "";
            args.forEach(function(value) {
                new_motd += value + " ";
            });
            
            this.Config.motd = new_motd;
            this.SaveConfig();
            rust.SendChatMessage(player,  this.Title, "<color=#A347FF>MOTD Changed to: </color>" + new_motd, 0);
            rust.BroadcastChat(this.Title, this.Config.motd, 0);
            return;
        } else {
            rust.SendChatMessage(player, this.Title, "You don't have the required permission to do that!", 0);
        }
    },
    showMotd : function () {
        var motd = this.Config.motd;
        print(motd);
    },
}
