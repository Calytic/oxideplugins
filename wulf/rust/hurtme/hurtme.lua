PLUGIN.Title = "Hurt Me"
PLUGIN.Version = V(0, 2, 0)
PLUGIN.Description = "Hurts target player on command, with optional amount."
PLUGIN.Author = "Luke Spragg - Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/resources/657/"
PLUGIN.ResourceID = "657"
PLUGIN.HasConfig = true

-- TODO:
---- Add authLevel check and permission option for moderators

-- Plugin initialization
function PLUGIN:Init()
    -- Add default command
    command.AddChatCommand("hurt", self.Object, "cmdHurt")
end

-- Hurt chat command
function PLUGIN:cmdHurt(player, cmd, args)
    -- Check if player has permission
    if (not player:IsAdmin()) then
        -- Send no permission message to player via chat
        player:SendConsoleCommand("chat.add \"" .. self.Config.Settings.ChatName .. "\" \"" .. self.Config.Messages.NoPermission .. "\"")
        return
    else
        -- Check for proper command usage
        if (args.Length < 1) then
            -- Send correct usage message to player via chat
            player:SendConsoleCommand("chat.add \"" .. self.Config.Settings.ChatName .. "\" \"" .. self.Config.Messages.HelpText .. "\"")
            return
        else
            -- Check if target is given
            local targetplayer = global.BasePlayer.Find(args[0])

            -- Check if player name is a real boy
            if (targetplayer == nil) then
                -- We tried, but the player doesn't seem to exist!
                player:SendConsoleCommand("chat.add \"" .. self.Config.Settings.ChatName .. "\" \"" .. self.Config.Messages.InvalidTarget .. "\"")
                return
            else
                -- Damage the player by given amount
                local amount
                if (args.Length == 2) then amount = args[1] else amount = 100 end
                targetplayer:TakeDamage(amount)

                -- Send message to target player via chat
                local targethurt = string.gsub(self.Config.Messages.TargetHurt, "{amount}", tostring(amount))
                --targetplayer:SendConsoleCommand("chat.add \"" .. self.Config.Settings.ChatName .. "\" \"" .. targethurt .. "\"")

                -- Send message to command user via chat
                local playerhurt = string.gsub(self.Config.Messages.AdminHurt, "{player}", targetplayer.displayName)
                local playerhurt = string.gsub(playerhurt, "{amount}", tostring(amount))
                --player:SendConsoleCommand("chat.add \"" .. self.Config.Settings.ChatName .. "\" \"" .. playerhurt .. "\"")
            end
        end
    end
end

-- Load default configuration
function PLUGIN:LoadDefaultConfig()
    -- General settings
    self.Config.Settings = {}
    self.Config.Settings.ChatName = "HURT"
    self.Config.Settings.ChatCommand = "hurt"

    -- Message strings
    self.Config.Messages = {}
    self.Config.Messages.NoPermission = "You do not have permission to use this command!"
    self.Config.Messages.HelpText = "Use /hurt player amount (amount being optional, default is 100)"
    self.Config.Messages.InvalidTarget = "Invalid target! Please try again"
    self.Config.Messages.TargetHurt = "You have been hurt {amount} by admin"
    self.Config.Messages.AdminHurt = "{player} has been hurt {amount}"
end
