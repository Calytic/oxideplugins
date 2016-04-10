PLUGIN.Title = "Push API"
PLUGIN.Version = V(0, 2, 0)
PLUGIN.Description = "API for sending messages via mobile notification services."
PLUGIN.Author = "Wulf / Luke Spragg"
PLUGIN.Url = "http://oxidemod.org/plugins/1164/"
PLUGIN.ResourceId = 1164

local debug = false

--[[ Do NOT edit the config here, instead edit PushAPI.json in oxide/config ! ]]

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.Service = self.Config.Settings.Service or "pushover"

    self.Config.Pushalot = self.Config.Pushalot or {}
    self.Config.Pushalot.AuthToken = self.Config.Pushalot.AuthToken or ""

    self.Config.Pushover = self.Config.Pushover or {}
    self.Config.Pushover.ApiToken = self.Config.Pushover.ApiToken or ""
    self.Config.Pushover.UserKey = self.Config.Pushover.UserKey or ""

    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.InvalidService = self.Config.Messages.InvalidService or "Configured push service is not valid!"
    self.Config.Messages.TitleRequired = self.Config.Messages.TitleRequired or "Title not given! Please enter one and try again"
    self.Config.Messages.MessageRequired = self.Config.Messages.MessageRequired or "Message not given! Please enter one and try again"
    self.Config.Messages.SendFailed = self.Config.Messages.SendFailed or "Notification failed to send!"
    self.Config.Messages.SendSuccess = self.Config.Messages.SendSuccess or "Notification successfully sent!"
    self.Config.Messages.SetApiToken = self.Config.Messages.SetApiToken or "API token not set! Please set it and try again."
    self.Config.Messages.SetApiToken = self.Config.Messages.SetAuthToken or "Auth token not set! Please set it and try again."
    self.Config.Messages.SetUserKey = self.Config.Messages.SetUserKey or "User key not set! Please set it and try again."

    self:SaveConfig()
end

function PLUGIN:Init()
    self:LoadDefaultConfig()
end

local function Print(self, message) print("[" .. self.Title .. "] " .. message) end

local function Debug(self, url, data, code)
    if debug then
        Print(self, "POST URL: " .. url)
        Print(self, "POST data: " .. data)
        Print(self, "HTTP code: " .. code)
    end
end

function PLUGIN:PushMessage(title, message, priority, sound)
    if title == "" then
        Print(self, self.Config.Messages.TitleRequired)
        return
    end

    if message == "" then
        Print(self, self.Config.Messages.MessageRequired)
        return
    end

    local url, data

    if string.lower(self.Config.Settings.Service) == "pushover" then
        if self.Config.Pushover.ApiToken == "" then
            Print(self, self.Config.Messages.SetApiToken)
            return
        end

        if self.Config.Pushover.UserKey == "" then
            Print(self, self.Config.Messages.SetUserKey)
            return
        end

        if not priority or priority == "high" then priority = "1"
        elseif priority == "low" then priority = "0"
        elseif priority == "quiet" then priority = "-1" end

        local sound = sound or "gamelan"

        url = "https://api.pushover.net/1/messages.json"
        data = "token=" .. self.Config.Pushover.ApiToken
        .. "&user=" .. self.Config.Pushover.UserKey
        .. "&title=" .. title
        .. "&message=" .. message
        .. "&priority=" .. priority
        .. "&sound=" .. sound
        .. "&html=1"

    elseif string.lower(self.Config.Settings.Service) == "pushalot" then
        if self.Config.Pushalot.AuthToken == "" then
            Print(self, self.Config.Messages.SetAuthToken)
            return
        end

        if not priority or priority == "high" then priority = "IsImportant=true"
        elseif priority == "low" then priority = "IsImportant=false"
        elseif priority == "quiet" then priority = "IsQuiet=true" end

        url = "https://pushalot.com/api/sendmessage"
        data = "AuthorizationToken=" .. self.Config.Pushalot.AuthToken
        .. "&Title=" .. title
        .. "&Body=" .. message
        .. "&" .. priority

    else
        Print(self, self.Config.Messages.InvalidService)
        return
    end

    webrequests.EnqueuePost(url, data, function(code, response)
        Debug(self, url, data, code)
        if code ~= 200 then
            Print(self, self.Config.Messages.SendFailed)
        else
            Print(self, self.Config.Messages.SendSuccess)
        end
    end, self.Plugin)
end
