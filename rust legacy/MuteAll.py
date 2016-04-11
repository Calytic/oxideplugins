class MuteAll:
    def __init__(self):
        self.Title = 'MuteAll'
        self.Description = 'MuteAll'
        self.Author = 'Spicy'
        self.Version = V(1, 0, 0)
        self.Url = 'http://oxidemod.org/plugins/681/'
        self.ResourceId = 681

    def Init(self):
        global global_mute
        global_mute = 0
	command.AddChatCommand('muteall', self.Plugin, 'cmdMuteAll')
	permission.RegisterPermission('canmuteall', self.Plugin)

    def cmdMuteAll(self, netuser, command, args):
	netusersteamid = rust.UserIDFromPlayer(netuser)
	if permission.UserHasPermission(netusersteamid, 'canmuteall'):
            global global_mute
            if global_mute == 0:
                rust.Notice(netuser, 'Chat has been globally muted.', 'â', 4)
                rust.BroadcastChat('Server', '[color red]Chat has been globally muted by \'%s\'!' % netuser.displayName)
                global_mute = 1
            else:
                rust.Notice(netuser, 'Chat has been globally unmuted.', 'â', 4)
                rust.BroadcastChat('Server', '[color red]Chat has been globally unmuted by \'%s\'!' % netuser.displayName)
                global_mute = 0
	else:
	    rust.Notice(netuser, 'You do not have permission to use this command!', 'â', 4)
	    
    def OnPlayerChat(self, netuser, command):
	if global_mute == 1:
            rust.Notice(netuser, 'Chat is globally muted!', 'â', 4)
            return False
