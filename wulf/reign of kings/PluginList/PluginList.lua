PLUGIN.Title = "Plugin List"
PLUGIN.Version = V(0, 1, 1)
PLUGIN.Description = "Shows installed plugins and plugin information."
PLUGIN.Author = "Wulf / Luke Spragg"
PLUGIN.Url = "http://oxidemod.org/plugins/1168/"
PLUGIN.ResourceId = 1168

local game = "rok"

--[[ Do NOT edit the config here, instead edit PluginList.json in oxide/config ! ]]

local messages
function PLUGIN:LoadDefaultConfig()
    self.Config.Messages = self.Config.Messages or {}
    messages = self.Config.Messages
    messages.InstalledPlugins = messages.InstalledPlugins or "Installed plugin(s):"
    messages.NoPluginFound = messages.NoPluginFound or "No plugin found with that name!"
    messages.PluginDesc = messages.PluginDesc or ", {description}"
    messages.PluginTitle = messages.PluginTitle or "{title} v{version}"

    self:SaveConfig()
end

local function SendChatMessage(player, message)
    if game == "rust" then rust.SendChatMessage(player, message) return end
    if game == "legacy" then rust.SendChatMessage(player, message) return end
    if game == "rok" then rok.SendChatMessage(player, message) return end
    --if game == "7dtd" then sdtd.SendChatMessage(player, message) return end
end

function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand("plugin", self.Plugin, "ListPlugins")
    command.AddChatCommand("plugins", self.Plugin, "ListPlugins")
end

function PLUGIN:ListPlugins(player, cmd, args)
    local pluginList, pluginTable = plugins.GetAll(), {}
    local title, version, description, resourceId

    if args.Length ~= 1 then
        for i = 2, pluginList.Length - 1 do
            title = pluginList[i].Title
            version = pluginList[i].Version:ToString()
            description = pluginList[i].Description
            resourceId = pluginList[i].Object.ResourceId
            pluginTable[i - 1] = title
        end

        local message = table.concat(pluginTable, ", ")
        SendChatMessage(player, messages.InstalledPlugins .. " " .. message)
    end

    if args.Length == 1 then
        local plugin = plugins.Find(args[0])

        if not plugin then SendChatMessage(player, messages.NoPluginFound) return end

        local message = messages.PluginTitle:gsub("{title}", plugin.Title):gsub("{version}", plugin.Version:ToString())
        if plugin.Description then message = message .. messages.PluginDesc:gsub("{description}", plugin.Description) end
        if plugin.Object.ResourceId and plugin.Object.ResourceId ~= "" and tonumber(plugin.Object.ResourceId) ~= 0 then
            message = message .. " http://oxidemod.org/plugins/" .. plugin.Object.ResourceId .. "/"
        end

        SendChatMessage(player, message)
    end
end
