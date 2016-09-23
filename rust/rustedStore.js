var rustedStore = {
    Title : "rusted.store payment system",
    Author : "rusted.store",
    Version : V(1, 0, 1),
    HasConfig : true,
    Init : function () {
        this.LoadDefaultConfig()
        this.LoadDefaultMessages();
        command.AddChatCommand(this.Config.command, this.Plugin, "cmdBuy" )
        String.format = function(format) {
            var args = Array.prototype.slice.call(arguments, 1);
            return format.replace(/{(\d+)}/g, function(match, number) { 
              return typeof args[number] != 'undefined'
                ? args[number] 
                : '<color=#cc2010>undefined</color>'
              ;
            });
        };
    },
    LoadDefaultConfig : function () {
        this.Config.command = this.Config.command || 'donate'
        this.Config.username = this.Config.username || 'USERNAME_NOT_SET'
        this.Config.secret = this.Config.secret || 'SECRET_NOT_SET'
        this.SaveConfig();
    },
    LoadDefaultMessages: function() {
        var dict_type = System.Collections.Generic.Dictionary(System.String, System.String);
        var lang_dict = new dict_type();
        lang_dict.Add('name', 'rusted.store')
        lang_dict.Add('buy_prompt', '\nThis is a list of items available for purchase.  \nTo view more details, type <color=#ffa500>/{0} </color><color=#40bb40>#</color>.\nFor example, type <color=#ffa500> /{0} </color><color=#40bb40>4</color> for details on package 4')
        lang_dict.Add('not_found', 'An unclaimed donation for your account could not be found')
        lang_dict.Add('thank', 'Thank you for your donation!')
        lang_dict.Add('package_not_found', 'The package ID you entered could not be found.')
        lang_dict.Add('purchase_prompt', 'To purchase this package, visit')
        var current_config = "\n" + 'Current Config: \nSecret: {0}\nBuy Username: {1}'
        current_config += "\n\nTo configure type \n/{2} config secret YOUR_SECRET_HERE\n/{2} config username YOUR_BUY_USERNAME_HERE"
        lang_dict.Add('current_config', current_config)
        lang_dict.Add('configure_config', 'To configure that plugin, type /{0} config.')
        lang_dict.Add('incomplete_config', 'This plugin has not been configured.  Please contact the server admin for support.')
        lang_dict.Add('incorrect_config', 'This plugin has been incorrectly configured, please contact the server admin for support.')
        lang.RegisterMessages(lang_dict, this.Plugin)
    },
    Lang: function(key, id) {
        return lang.GetMessage(key, this.Plugin, id);
    },
    cmdBuy : function(player, cmd, args) {

        var authLvl = player.net.connection.authLevel
        var baseUrl = "https://rusted.store"
        var that = this;

        //admin config stuff
        if (authLvl >= 2 && args.length > 0 && args[0] == 'config'){
            if (args.length == 3 && args[1] == 'secret'){
                that.Config.secret = args[2]
            } else if (args[1] == 'username'){
                that.Config.username = args[2]
            } else {
                rust.SendChatMessage(player, that.Lang('name', player.Id) , String.format(that.Lang('current_config', player.Id), that.Config.secret, that.Config.username, that.Config.command))
                return;
            }
            that.SaveConfig();
            rust.SendChatMessage(player, that.Lang('name', player.Id) , String.format(that.Lang('current_config', player.Id), that.Config.secret, that.Config.username, that.Config.command))
            return
        }

        //plugin not set up
        if (!that.Config.secret || !that.Config.username){
            if (authLvl >= 2)
                rust.SendChatMessage(player, that.Lang('name', player.Id) , String.format(that.Lang('incomplete_config', player.Id), that.Config.command))
            else
                rust.SendChatMessage(player, that.Lang('name', player.Id) , that.Lang('incomplete_config', player.Id))
            return;
        }

        //show list
        if (args.length == 0){
            var url = baseUrl+"/shops/"+that.Config.username+"?secret="+that.Config.secret
            var textLines = [String.format(that.Lang('buy_prompt', player.Id), that.Config.command)]
            webrequests.EnqueueGet(url, function(code, response) { 
                if (code == 400){
                    rust.SendChatMessage(player, that.Lang('name', player.Id) , that.Lang('incorrect_config', player.Id))
                    return
                }
                var items = JSON.parse(response)
                for (var i = 0; i< items.length; i++){
                    var item = items[i]
                    textLines.push('<color=#40bb40>' + item.localId + '</color> - '+ item.name + ' - $' + item.price)
                }

                rust.SendChatMessage(player, that.Lang('name', player.Id) , textLines.join('\n'), "0")
            }.bind(that),  that.Plugin);


        } else if (args.length == 1){
            if (args[0] == 'claim'){
                var params = {
                    steamId: player.UserIDString
                }
                var url = baseUrl+"/shops/"+that.Config.username+"/payments/claim"+"?secret="+that.Config.secret
                webrequests.EnqueuePost(url, "body="+JSON.stringify(params), function(code, response){
                    if (code == 400){
                        rust.SendChatMessage(player, that.Lang('name', player.Id) , that.Lang('not_found', player.Id))
                        return
                    }
                    var json_obj = JSON.parse(response)
                    var item = json_obj['item']
                    var ticket = json_obj['ticket']
                    var cmds = item.commands
                    print("------Claimed Payment-----")
                    print("Player: " + ticket.steamName + '/' + ticket.steamId)
                    print("Identifier: " + ticket.easyId)
                    for (var i = 0; i < cmds.length; i++){
                        rust.RunServerCommand(cmds[i].command)
                        print("Command: " + cmds[i].command)
                    }
                    print("------Claimed Payment-----")
                    rust.SendChatMessage(player, that.Lang('name', player.Id) , that.Lang('thank', player.Id))
                }, that.Plugin)
            } else {
                var url = baseUrl+"/shops/"+that.Config.username+"/items/"+args[0]+'?secret='+that.Config.secret
                var params = {steamId: player.UserIDString,
                            steamName: player.displayName}
                var textLines = ['']

                webrequests.EnqueuePost(url, "body="+JSON.stringify(params), function(code, response){
                    if (code == 400){
                        rust.SendChatMessage(player, that.Lang('name', player.Id) , that.Lang('package_not_found', player.Id))
                        return
                    }
                    var item = JSON.parse(response)
                    textLines.push(item.name + ' - $' + item.price)
                    textLines.push(item.description)
                    textLines.push('')
                    textLines.push(that.Lang('purchase_prompt', player.Id) +' <color=#0088dd>\n'+baseUrl+'/payments/' + item.easyId + '</color>')
                    rust.SendChatMessage(player, that.Lang('name', player.Id) , textLines.join('\n'), "0")
                }, that.Plugin)
            }
        }
    }
}