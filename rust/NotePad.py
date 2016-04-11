import re

DEV = False
LATEST_CFG = 0.2
LINE = "-"*50

class NotePad:

    def __init__(self):

        self.Title = "NotePad"
        self.Author = "OMNI-Hollow"
        self.Version = V 0.0.2
        self.ResourceId = 1154

    # -------------------------------------------------------------------------
    def LoadDefaultConfig(self):
        ''' Hook called when there is no configuration file '''

        self.Config = {
            "CONFIG_VERSION": LATEST_CFG,
            "SETTINGS": {
                "PREFIX": "NotePad",
                "BROADCAST TO CONSOLE": True
            },
            "MESSAGES": {
                "NOTE NOT EXIST": "There are no notes with the name <orange>{note}<end>.",
                "YOUR NOTES": "Your Notes:",
                "ADMIN NOTES": "Admin Notes:",
                "NO PLAYER NOTES": "You do not have any notes saved.",
                "NOTE ADDED": "<cyan>{note}<end> added to your notes.",
                "NOTE DELETED": "<cyan>{note}<end> has been deleted.",
                "NOTES CLEARED": "All notes have been cleared.",
                "NO PERMISSION": "You do not have permission to use this command",
                "NO USER NOTES": "You do not have any saved notes",
                "NO GLOBAL NOTES": "There aren't any any notes saved yet"
            },
            "COLORS": {
                "PREFIX": "#0000FF",
                "SYSTEM": "#3399FF"
            }
        }

    # -------------------------------------------------------------------------
    def UpdateConfig(self):
        ''' Function to update the configuration file on plugin Init '''

        if self.Config["CONFIG_VERSION"] <= LATEST_CFG - 0.2 or DEV:

            self.Config.clear()

            self.LoadDefaultConfig()

        else:

            self.Config["CONFIG_VERSION"] = LATEST_CFG

            self.Config["MESSAGES"]["NOTES CLEARED"] = "All notes have been cleared."

        self.SaveConfig()

    # -------------------------------------------------------------------------
    def Save(self):
        ''' Function to save the plugin database '''

        data.SaveData(self.dbname)

        self.console("Saving Database")

    # -------------------------------------------------------------------------
    # - MESSAGE SYSTEM
    def console(self, text, force=False):
        ''' Function to send a server console message '''

        if self.Config["SETTINGS"]["BROADCAST TO CONSOLE"] or force:

            print("[%s v%s] :: %s" % (self.Title, str(self.Version), self._format(text, True)))

    # -------------------------------------------------------------------------
    def say(self, text, color="white", force=True, userid=0):
        ''' Function to send a message to all players '''

        if self.prefix and force:

            rust.BroadcastChat(self._format("%s <%s>%s<end>" % (self.prefix, color, text)), None, str(userid))

        else:

            rust.BroadcastChat(self._format("<%s>%s<end>" % (color, text)), None, str(userid))

        self.console(self._format(text, True))

    # -------------------------------------------------------------------------
    def tell(self, player, text, color="white", force=True, userid=0):
        ''' Function to send a message to a player '''

        if self.prefix and force:

            rust.SendChatMessage(player, self._format("%s <%s>%s<end>" % (self.prefix, color, text)), None, str(userid))

        else:

            rust.SendChatMessage(player, self._format("<%s>%s<end>" % (color, text)), None, str(userid))

    # -------------------------------------------------------------------------
    def _format(self, text, con=False):
        '''
            * Notifier"s color format system
            ---
            Replaces color names and RGB hex code into HTML format code
        '''

        colors = (
            "red", "blue", "green", "yellow", "white", "black", "cyan",
            "lightblue", "lime", "purple", "darkblue", "magenta", "brown",
            "orange", "olive", "gray", "grey", "silver", "maroon"
        )

        name = r"\<(\w+)\>"
        hexcode = r"\<(#\w+)\>"
        end = "<end>"

        if con:
            for x in (end, name, hexcode):
                if x.startswith("#") or x in colors:
                    text = re.sub(x, "", text)
        else:
            text = text.replace(end, "</color>")
            for f in (name, hexcode):
                for c in re.findall(f, text):
                    if c.startswith("#") or c in colors:
                        text = text.replace("<%s>" % c, "<color=%s>" % c)
        return text

    # -------------------------------------------------------------------------
    # - SERVER HOOKS
    def Init(self):
        ''' Hook called on plugin initialized '''

        # Configuration Update
        if self.Config["CONFIG_VERSION"] < LATEST_CFG or DEV:

            self.UpdateConfig()

        # Plugin Specific
        global MSG, PLUGIN, COLOR
        MSG, COLOR, PLUGIN = [self.Config[x] for x in ("MESSAGES","COLORS","SETTINGS")]

        if PLUGIN["PREFIX"]:
            self.prefix = "<%s>%s<end> :" % (COLOR["PREFIX"], PLUGIN["PREFIX"])
        else:
            self.prefix = None
        
        self.uinv = "/notes <add|show|del|clear> <notename> [message]"
        self.ginv = "/global <add|show|del|clear> <notename> [message]"
        self.error = "white"

        # Setup Database
        self.dbname = "notepad_db"
        self.db = data.GetData(self.dbname)

        if not self.db:
            self.db["USERS"] = {}
            self.db["GLOBAL"] = {}

        # Commands
        command.AddChatCommand("notes", self.Plugin, "handle_CMD")
        command.AddChatCommand("global", self.Plugin, "handle_CMD")

    # -------------------------------------------------------------------------
    def Unload(self):
        ''' Hook called on plugin unload '''

        self.Save()

    # -------------------------------------------------------------------------
    def OnServerSave(self):
        ''' Hook called on server save '''

        self.Save()

    # -------------------------------------------------------------------------
    # - COMMAND FUNTIONS
    def handle_CMD(self, player, cmd, args):

        users = self.db["USERS"]
        admin = self.db["GLOBAL"]

        if args:

            if cmd == "notes":

                uid = self.playerid(player)

                # Is Player in DB?
                if uid not in users:

                    users[uid] = {}

                if args[0] == "add" and len(args) >= 3:

                    # Add a Note

                    users[uid][args[1].lower()] = " ".join(args[2:])

                    self.tell(player, MSG["NOTE ADDED"].format(note=args[1].title()), COLOR["SYSTEM"])

                elif users[uid]:

                    if args[0] == "del" and len(args) == 2:

                        # Delete a Note by name

                        note = args[1]

                        if note.lower() in users[uid]:

                            del users[uid][note]

                            self.tell(player, MSG["NOTE DELETED"].format(note=note.title()), COLOR["SYSTEM"])

                        else:

                            self.tell(player, MSG["NOTE NOT EXIST"].format(note=note), COLOR["SYSTEM"])

                    elif args[0] == "show" and len(args) == 2:

                        # Shows specific note

                        note = args[1]

                        if note.lower() in users[uid]:

                            self.tell(player, LINE)
                            self.tell(player, "<%s>Title:<end> %s" % (COLOR["SYSTEM"], args[1].title()))
                            self.tell(player, "<%s>Message:<end> %s" % (COLOR["SYSTEM"], users[uid][note]))
                            self.tell(player, LINE)

                        else:

                            self.tell(player, MSG["NOTE NOT EXIST"].format(note=note), COLOR["SYSTEM"])

                    elif args[0] == "clear":

                        # Clear all player notes

                        users[uid].clear()

                        self.tell(player, MSG["NOTES CLEARED"], COLOR["SYSTEM"])

                    else:

                        self.tell(player, "Invalid Syntax: %s" % self.uinv, self.error)

                else:

                    self.tell(player, MSG["NO PLAYER NOTES"], COLOR["SYSTEM"])

                # Update Database with any changes
                self.db["USERS"].update(users)

            if cmd == "global":

                if player.IsAdmin():

                    if args[0] == "add" and len(args) >= 2:

                        # Add a Note

                        admin[args[1].lower()] = " ".join(args[2:])

                        self.tell(player, MSG["NOTE ADDED"].format(note=args[1].title()), COLOR["SYSTEM"])

                    elif admin:

                        if args[0] == "del" and len(args) == 2:

                            # Delete a Note by name

                            note = args[1]

                            if note.lower() in admin:

                                del admin[note]

                                self.tell(player, MSG["NOTE DELETED"].format(note=note.title()), COLOR["SYSTEM"])

                            else:

                                self.tell(player, MSG["NOTE NOT EXIST"].format(note=note), COLOR["SYSTEM"])

                        elif args[0] == "show" and len(args) == 2:

                            # Shows specific note

                            note = args[1]

                            if note.lower() in admin:

                                self.tell(player, LINE)
                                self.tell(player, "<%s>Title:<end> %s" % (COLOR["SYSTEM"], args[1].title()))
                                self.tell(player, "<%s>Message:<end> %s" % (COLOR["SYSTEM"], admin[note]))
                                self.tell(player, LINE)

                            else:

                                self.tell(player, MSG["NOTE NOT EXIST"].format(note=note), COLOR["SYSTEM"])

                        elif args[0] == "clear":

                            # Clear all player notes

                            admin.clear()

                            self.tell(player, MSG["NOTES CLEARED"], COLOR["SYSTEM"])

                        else:

                            self.tell(player, "Invalid Syntax: %s" % self.ginv, self.error)

                    else:

                        self.tell(player, MSG["NO PLAYER NOTES"], COLOR["SYSTEM"])

                else:

                    self.tell(player, MSG["NO PERMISSION"], COLOR["SYSTEM"])

                # Update Database with any changes
                self.db["GLOBAL"].update(admin)

        else:

            # Commands with no arguments will print a list of all user/admins notes

            if cmd == "notes":

                uid = self.playerid(player)

                if uid in users and users[uid]:

                    self.tell(player, MSG["YOUR NOTES"], COLOR['SYSTEM'])
                    self.tell(player, LINE)

                    for n, i in enumerate(users[uid]):

                        self.tell(player, "<%s>%s.<end> %s" % (COLOR["SYSTEM"], n + 1, i.title()))

                    self.tell(player, LINE)
                    self.tell(player, "Syntax: %s" % self.uinv, self.error)

                else:

                    self.tell(player, MSG["NO USER NOTES"], COLOR["SYSTEM"])

            elif cmd == "global" and player.IsAdmin():

                if admin:

                    self.tell(player, MSG["ADMIN NOTES"], COLOR["SYSTEM"])
                    self.tell(player, LINE)

                    for n, i in enumerate(admin):

                        self.tell(player, "<%s>%s.<end> %s" % (COLOR["SYSTEM"], n + 1, i.title()))

                    self.tell(player, LINE)
                    self.tell(player, "Syntax: %s" % self.ginv, self.error)

                else:

                    self.tell(player, MSG["NO GLOBAL NOTES"], COLOR["SYSTEM"])

            else:

                self.tell(player, MSG["NO PERMISSION"], COLOR["SYSTEM"])

    # -------------------------------------------------------------------------
    # - FUNCTIONS
    def playerid(self, player):
        ''' Returns UID of the player '''

        return rust.UserIDFromPlayer(player)