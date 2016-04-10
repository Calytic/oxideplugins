PLUGIN.Title = "Email API"
PLUGIN.Version = V(0, 3, 2)
PLUGIN.Description = "API for sending email messages via supported transactional email services."
PLUGIN.Author = "Wulf / Luke Spragg"
PLUGIN.Url = "http://oxidemod.org/plugins/1172/"
PLUGIN.ResourceId = 1172

local debug = false

--[[ Do NOT edit the config here, instead edit EmailAPI.json in oxide/config ! ]]

local api, settings, messages
function PLUGIN:LoadDefaultConfig()
    self.Config.Api = self.Config.Api or {}
    api = self.Config.Api
    api.Domain = api.Domain or ""
    api.PrivateKey = api.PrivateKey or api.KeyPrivate or ""
    api.Service = api.Service or api.Provider or ""
    api.PublicKey = api.PublicKey or api.KeyPublic or ""
    api.Username = api.Username or ""

    self.Config.Settings = self.Config.Settings or {}
    settings = self.Config.Settings
    settings.FromEmail = settings.FromEmail or "change@me.tld"
    settings.FromName = settings.FromName or "Change Me"
    settings.ToEmail = settings.ToEmail or "change@me.tld"
    settings.ToName = settings.ToName or "Change Me"

    self.Config.Messages = self.Config.Messages or {}
    messages = self.Config.Messages
    messages.InvalidService = messages.InvalidService or "Configured email service is not valid!"
    messages.MessageRequired = messages.MessageRequired or "Message not given! Please enter one and try again"
    messages.SendFailed = messages.SendFailed or "Email failed to send!"
    messages.SendSuccess = messages.SendSuccess or "Email successfully sent!"
    messages.SetDomain = messages.SetDomain or "Domain not set! Please set it and try again."
    messages.SetPrivateKey = messages.SetPrivateKey or "Private key not set! Please set it and try again."
    messages.SetPublicKey = messages.SetPublicKey or "Public key not set! Please set it and try again."
    messages.SetUsername = messages.SetUsername or "Username not set! Please set it and try again."
    messages.SubjectRequired = messages.SubjectRequired or "Subject not given! Please enter one and try again"

    settings.ApiKeyPrivate = nil -- Removed in 0.3.0
    settings.ApiKeyPublic = nil -- Removed in 0.3.0
    settings.ApiProvider = nil -- Removed in 0.3.0
    messages.InvalidProvider = nil -- Removed in 0.3.0
    messages.SetApiKey = nil -- Removed in 0.3.0

    self:SaveConfig()
end

function PLUGIN:Init() self:LoadDefaultConfig() end

local function Print(self, message) print("[" .. self.Title .. "] " .. message) end

local function Encode(message)
    if message then
        message = message:gsub("\n", "\r\n")
        message = message:gsub("([^%w %-%_%.%~])",
            function (c) return string.format("%%%02X", string.byte(c)) end)
        message = message:gsub(" ", "+")
    end
    return message
end

local function Base64(hash)
    if type(hash) ~= "userdata" then hash = System.Text.Encoding.get_ASCII():GetBytes(hash) end
    return System.Convert.ToBase64String.methodarray[0]:Invoke(nil, util.TableToArray({ hash }))
end

local function HmacSha256(key, sig)
    local hasher = new(System.Security.Cryptography.HMACSHA256._type, nil)
    hasher:set_Key(System.Text.Encoding.get_ASCII():GetBytes(key))
    return hasher:ComputeHash(System.Text.Encoding.get_ASCII():GetBytes(sig))
end

local function WebRequest(self, url, data)
    webrequests.EnqueuePost(url, data, function(code, response)
        if debug then
            Print(self, "POST URL: " .. url)
            Print(self, "POST data: " .. data)
            Print(self, "HTTP code: " .. code)
        end

        if code ~= 200 and code ~= 250 then
            Print(self, messages.SendFailed)
        else
            Print(self, messages.SendSuccess)
        end

        if debug then Print(self, response) end
    end, self.Plugin, headers)
end

function PLUGIN:EmailMessage(subject, message)
    if subject == "" then Print(self, messages.SubjectRequired) return end
    if message == "" then Print(self, messages.MessageRequired) return end

    local provider = string.lower(api.Service)
    local headers = new(System.Collections.Generic.Dictionary[{ System.String, System.String }], null)
    local url, data

    if provider == "amazon" or provider == "ses" or provider == "amazonses" then
        if api.PrivateKey == "" then Print(self, messages.SetPrivateKey) return end
        if api.PublicKey == "" then Print(self, messages.SetPublicKey) return end

        url = "https://email.us-east-1.amazonaws.com"
        data =
            "Source="                               .. Encode(settings.FromName .. "<" .. settings.FromEmail .. ">")
            .. "&Destination.ToAddresses.member.1=" .. Encode(settings.ToName .. "<" .. settings.ToEmail .. ">")
            .. "&Message.Subject.Data="             .. Encode(subject)
            .. "&Message.Body.Text.Data="           .. Encode(message)
            .. "&Action=SendEmail"

        local timestamp = time.GetCurrentTime():ToString("ddd, dd MMM yyyy HH:mm:s") .. " GMT"
        local signature = Base64(HmacSha256(api.PrivateKey, timestamp))

        headers["X-Amzn-Authorization"] = "AWS3-HTTPS AWSAccessKeyId=" .. api.PublicKey
            .. ", Algorithm=HmacSHA256, Signature=" .. signature
        headers["X-Amz-Date"] = timestamp

    elseif provider == "elastic" or provider == "elasticemail" then
        if api.PrivateKey == "" then Print(self, messages.SetApiKey) return end
        if api.Username == "" then Print(self, messages.SetUsername) return end

        url = "https://api.elasticemail.com/mailer/send"
        data =
            "api_key="       .. api.PrivateKey
            .. "&username="  .. api.Username
            .. "&from="      .. Encode(settings.FromEmail)
            .. "&from_name=" .. Encode(settings.FromName)
            .. "&to="        .. Encode(settings.ToName .. "<" .. settings.ToEmail .. ">")
            .. "&subject="   .. Encode(subject)
            .. "&body_html=" .. Encode(message)

    elseif provider == "madmimi" then
        if api.PrivateKey == "" then
            Print(self, messages.SetApiKey)
            return
        end

        url = "https://api.madmimi.com/mailer"
        data =
            "api_key="            .. api.PrivateKey
            .. "&username="       .. api.Username
            .. "&from="           .. Encode(settings.FromName .. "<" .. settings.FromEmail .. ">")
            .. "&recipient="      .. Encode(settings.ToName .. "<" .. settings.ToEmail .. ">")
            .. "&promotion_name=" .. Encode(subject)
            .. "&subject="        .. Encode(subject)
            .. "&raw_html="       .. Encode(message)

    elseif provider == "mailgun" then
        if api.PrivateKey == "" then Print(self, messages.SetPrivateKey) return end
        if api.Domain == "" then Print(self, messages.SetDomain) return end

        url = "https://api.mailgun.net/v3/" .. api.Domain .. "/messages"
        data =
            "from="        .. Encode(settings.FromName .. "<" .. settings.FromEmail .. ">")
            .. "&to="      .. Encode(settings.ToName .. "<" .. settings.ToEmail .. ">")
            .. "&subject=" .. Encode(subject)
            .. "&text="    .. Encode(message)

        headers["Authorization"] = "Basic " .. Base64("api:" .. api.PrivateKey)

    elseif provider == "mailjet" then
        if api.PrivateKey == "" then Print(self, messages.SetPrivateKey) return end
        if api.PublicKey == "" then Print(self, messages.SetPublicKey) return end

        url = "https://api.mailjet.com/v3/send/message"
        data =
            "from="        .. Encode(settings.FromName .. "<" .. settings.FromEmail .. ">")
            .. "&to="      .. Encode(settings.ToName .. "<" .. settings.ToEmail .. ">")
            .. "&subject=" .. Encode(subject)
            .. "&html="    .. Encode(message)

        headers["Authorization"] = "Basic " .. Base64(api.PublicKey .. ":" .. api.PrivateKey)

    elseif provider == "mandrill" or provider == "mandrillapp" then
        if api.PrivateKey == "" then Print(self, messages.SetPrivateKey) return end

        url = "https://mandrillapp.com/api/1.0/messages/send.json"
        data =
            '{\"key\": \"'          .. api.PrivateKey .. '\",'
            .. '\"message\": {'
            .. '\"from_email\": \"' .. settings.FromEmail .. '\",'
            .. '\"from_name\": \"'  .. settings.FromName .. '\",'
            .. '\"to\": [{'
            .. '\"email\": \"'      .. settings.ToEmail .. '\",'
            .. '\"name\": \"'       .. settings.ToName .. '\"}],'
            .. '\"subject\": \"'    .. subject .. '\",'
            .. '\"html\": \"'       .. message .. '\"}}'

    elseif provider == "postageapp" or provider == "postage" then
        if api.PrivateKey == "" then Print(self, messages.SetPrivateKey) return end

        url = "https://api.postageapp.com/v.1.0/send_message.json"
        data =
            '{\"api_key\": \"'      .. api.PrivateKey .. '\",'
            .. '\"arguments\": {'
            .. '\"recipients\": \"' .. settings.ToName .. "<" .. settings.ToEmail .. ">" .. '\",'
            .. '\"headers\": {'
            .. '\"subject\": \"'    .. subject .. '\",'
            .. '\"from\": \"'       .. settings.FromName .. "<" .. settings.FromEmail .. ">" .. '\"},'
            .. '\"content\": \"'    .. message .. '\"}}'

        headers["Content-Type"] = "application/json"

    elseif provider == "postmark" or provider == "postmarkapp" then
        if api.PrivateKey == "" then Print(self, messages.SetPrivateKey) return end

        url = "https://api.postmarkapp.com/email"
        data =
        '{\"From\": \"'       .. settings.FromName .. "<" .. settings.FromEmail .. ">" .. '\",'
        .. '\"To\": \"'       .. settings.ToName .. "<" .. settings.ToEmail .. ">" .. '\",'
        .. '\"Subject\": \"'  .. subject .. '\",'
        .. '\"HtmlBody\": \"' .. message .. '\"}'

        headers["Accept"] = "application/json"
        headers["Content-Type"] = "application/json"
        headers["X-Postmark-Server-Token"] = api.PrivateKey

    elseif provider == "sendgrid" then
        if api.PrivateKey == "" then Print(self, messages.SetPrivateKey) return end
        if api.Username == "" then Print(self, messages.SetUsername) return end

        url = "https://api.sendgrid.com/api/mail.send.json"
        data =
        "api_key="      .. api.PrivateKey
        .. "&api_user=" .. api.Username
        .. "&from="     .. Encode(settings.FromEmail)
        .. "&fromname=" .. Encode(settings.FromName)
        .. "&to="       .. Encode(settings.ToEmail)
        .. "&toname="   .. Encode(settings.ToName)
        .. "&subject="  .. Encode(subject)
        .. "&html="     .. Encode(message)

    elseif provider == "sendinblue" then
        if api.PrivateKey == "" then Print(self, messages.SetPrivateKey) return end

        url = "https://api.sendinblue.com/v2.0/email"
        data =
        '{ \"to\": { \"'     .. settings.ToEmail
        .. '\": \"'          .. settings.ToName .. '\" },'
        .. '\"from\": [ \"'  .. settings.FromEmail
        .. '\", \"'          .. settings.FromName .. '\"],'
        .. '\"subject\": \"' .. subject .. '\",'
        .. '\"html\": \"'    .. message .. '\"}'

        headers["api-key"] = api.PrivateKey

    else
        Print(self, messages.InvalidService)
        return
    end

    WebRequest(self, url, data)
end
