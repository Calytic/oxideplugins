PLUGIN.Title = "Online Log"
PLUGIN.Version = V(0, 1, 1)
PLUGIN.Description = "Creates a JSON formatted log of all online players for use with external scripts."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/plugins/690/"
PLUGIN.ResourceId = 690
PLUGIN.HasConfig = false

function PLUGIN:Init()
    self.datatable = datafile.GetDataTable("onlinelog")
end

function PLUGIN:OnPlayerConnected(packet)
    if not packet then return end
    if not packet.connection then return end
    local steamId = rust.UserIDFromConnection(packet.connection)
    self.datatable.Online = self.datatable.Online or {}
    self.datatable.Online.Count = global.BasePlayer.activePlayerList.Count + 1
    self.datatable.Online.Players = self.datatable.Online.Players or {}
    self.datatable.Online.Players[steamId] = packet.connection.username
    datafile.SaveDataTable("onlinelog")
end

function PLUGIN:OnPlayerDisconnected(player)
    if not player then return end
    local steamId = rust.UserIDFromPlayer(player)
    self.datatable.Online.Count = global.BasePlayer.activePlayerList.Count - 1
    self.datatable.Online.Players[steamId] = nil
    datafile.SaveDataTable("onlinelog")
end
