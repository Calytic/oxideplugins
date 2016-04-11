class ScreenshotRequest:
    def __init__(self):
        self.Title = "Screenshot Request"
        self.Description = "Displays a message and enables branding on a selected player."
        self.Author = "Spicy"
        self.Version = V(1, 1, 2)
        self.Url = "http://oxidemod.org/plugins/681/"
        self.ResourceId = 681
       
    def Init(self):
        command.AddChatCommand("sr",  self.Plugin, "cmdScreenshotRequest")
        permission.RegisterPermission("screenshotrequest", self.Plugin)

    def LoadDefaultConfig(self):
        self.Config = {
            "MESSAGE": "Take a screenshot with F12 and upload it to Steam."
        }

    def cmdScreenshotRequest(self, netuser, command, args):
        steamid = rust.UserIDFromPlayer(netuser)
        if permission.UserHasPermission(steamid, "screenshotrequest"):
            target = rust.FindPlayer(args[0])
            rust.Notice(netuser, "%s is now being requested to take a screenshot." % target.displayName, "â", 4)
            rust.RunClientCommand(target, "gui.show_branding")
            rust.RunClientCommand(target, "deathscreen.reason \"%s\"" % self.Config["MESSAGE"])
            rust.RunClientCommand(target, "deathscreen.show")
        else:
            rust.Notice(netuser, "You do not have permission to use this command!", "â", 4)