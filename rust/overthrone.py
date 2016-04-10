import re
import time
import BasePlayer

DEV = False
LATEST_CFG = 0.3
LINE = '-'*50
PROFILE = '76561198203401038'

class overthrone:

    def __init__(self):

        self.Title = 'OverThrone'
        self.Author = 'SkinN & OMNI-Hollow'
        self.Description = 'Reign Of Kings king system re-imagined on Rust'
        self.Version = V(0, 0, 4)
        self.ResourceId = 1169

    # -------------------------------------------------------------------------
    # - CONFIGURATION / DATABASE SYSTEM
    def LoadDefaultConfig(self):
        ''' Hook called when there is no configuration file '''

        self.Config = {
            'CONFIG_VERSION': LATEST_CFG,
            'SETTINGS': {
                'BROADCAST TO CONSOLE': True,
                'PREFIX': '<#9B7E7A>Over<end><#CA1F0C>Throne<end>',
                'KING PREFIX': '<#CECECE>[ <#A435E4>KING<end> ]<end>',
                'ENABLE KING CMD': True,
                'ENABLE RESET KING CMD': True,
                'ENABLE KING PREFIX': True,
                'ENABLE PLUGIN ICON': True
            },
            'MESSAGES': {
                'FIRST KING': 'This land had no King until today, <#A435E4>{king}<end> is now the King of the land!',
                'ANNOUNCE NEW KING': 'Sir <red>{attacker}<end> killed King <cyan>{lastking}<end> and is now our new King, all hail the new King!',
                'TELL KINGSHIP LOST': 'You were killed by <red>{attacker}<end> and lost your throne. You are no longer King of the land!',
                'NOTIFY KINGSHIP LOSS': 'While away you were killed by <red>{attacker}<end> and lost the throne. The current king is <#A435E4>{king}<end>.',
                'YOU ARE KING': 'You are the King of the land, since <lime>{time}H<end> ago.',
                'WHO IS KING': 'The land is ruled by King <#A435E4>{king}<end>, since <lime>{time}H<end> ago.',
                'NO KING YET': 'No one is ruling the land yet. Kill someone to be the first to claim the land!',
                'KING DESC': '<orange>/king<end> <grey>-<end> Tells who is the current King and for how long.',
                'RESET KING DESC': '<orange>/resetking<end> <grey>-<end> Resets the King system, living no one as King. ( <cyan>Admins Only<end> )',
                'NO ACCESS': 'You are not allowed to use this command.',
                'KING RESETED': 'An <cyan>Admin<end> has reset the <orange>King System<end>, the land is now without a King!'
            },
            'COMMANDS': {
                'KING': 'king',
                'RESET KING': 'resetking'
            },
            'COLORS': {
                'PREFIX': '#CECECE',
                'SYSTEM': '#CECECE',
                'KING CHAT': 'white',
                'ADMIN NAME': '#ADFF64',
                'PLAYER NAME': '#6496E1'
            }
        }

        self.con('* Setting up default configuration file')

    # -------------------------------------------------------------------------
    def UpdateConfig(self):
        ''' Function to update the configuration file on plugin Init '''

        if self.Config['CONFIG_VERSION'] <= LATEST_CFG - 0.2 or DEV:

            self.con('* Configuration version is too old, resetting to default')

            self.Config.clear()

            self.LoadDefaultConfig()

        else:

            self.Config['SETTINGS']['ENABLE PLUGIN ICON'] = True
            self.Config['SETTINGS']['ENABLE RESET KING CMD'] = True

            self.Config['COLORS']['ADMIN NAME'] = '#ADFF64'
            self.Config['COLORS']['PLAYER NAME'] = '#6496E1'

            self.Config['COMMANDS']['RESET KING'] = 'resetking'

            self.Config['MESSAGES']['NO ACCESS'] = 'You are not allowed to use this command.'
            self.Config['MESSAGES']['KING RESETED'] = 'An <cyan>Admin<end> has reset the <orange>King System<end>, the land is now without a King!'
            self.Config['MESSAGES']['KING DESC'] = '<orange>/king<end> <grey>-<end> Tells who is the current King and for how long.'
            self.Config['MESSAGES']['RESET KING DESC'] = '<orange>/resetking<end> <grey>-<end> Resets the King system, living no one as King. ( <cyan>Admins Only<end> )'

            self.Config['CONFIG_VERSION'] = LATEST_CFG

        self.SaveConfig()

    # -------------------------------------------------------------------------
    def Save(self):
        ''' Function to save the plugin database '''

        data.SaveData(self.dbname)

        self.con('Saving Database')

    # -------------------------------------------------------------------------
    # - MESSAGE SYSTEM
    def con(self, text, f=False):
        ''' Function to send a server console message '''

        if self.Config['SETTINGS']['BROADCAST TO CONSOLE'] or f:

            print('[%s v%s] :: %s' % (self.Title, str(self.Version), self.format(text, True)))

    # -------------------------------------------------------------------------
    def say(self, text, color='white', profile=False, f=True, con=True):
        ''' Function to send a message to all players '''

        if self.prefix and f:

            rust.BroadcastChat(self.format('[ %s ] <%s>%s<end>' % (self.prefix, color, text)), None, PROFILE if not profile else profile)

        else:

            rust.BroadcastChat(self.format('<%s>%s<end>' % (color, text)), None, PROFILE if not profile else profile)

        if con:

            self.con(text)

    # -------------------------------------------------------------------------
    def tell(self, player, text, color='white', f=True, profile=False):
        ''' Function to send a message to a player '''

        if self.prefix and f:

            rust.SendChatMessage(player, self.format('[ %s ] <%s>%s<end>' % (self.prefix, color, text)), None, PROFILE if not profile else profile)

        else:

            rust.SendChatMessage(player, self.format('<%s>%s<end>' % (color, text)), None, PROFILE if not profile else profile)

    # -------------------------------------------------------------------------
    def format(self, text, con=False):
        '''
            * Notifier's color format system
            ---
            Replaces color names and RGB hex code into HTML format code
        '''

        colors = (
            'red', 'blue', 'green', 'yellow', 'white', 'black', 'cyan',
            'lightblue', 'lime', 'purple', 'darkblue', 'magenta', 'brown',
            'orange', 'olive', 'gray', 'grey', 'silver', 'maroon'
        )

        name = r'\<(\w+)\>'
        hexcode = r'\<(#\w+)\>'
        end = '<end>'

        if con:
            for x in (end, name, hexcode):
                for c in re.findall(x, text):
                    if c.startswith('#') or c in colors:
                        text = re.sub(x, '', text)
        else:
            text = text.replace(end, '</color>')
            for f in (name, hexcode):
                for c in re.findall(f, text):
                    if c.startswith('#') or c in colors: text = text.replace('<%s>' % c, '<color=%s>' % c)
        return text

    # -------------------------------------------------------------------------
    # - SERVER / PLUGIN HOOKS
    def Unload(self):
        ''' Hook called on plugin unload '''

        self.Save()

    # -------------------------------------------------------------------------
    def OnServerSave(self):
        ''' Hook called when server data is saved '''

        self.Save()

    # -------------------------------------------------------------------------
    def Init(self):
        ''' Hook called when the plugin initializes '''

        self.con(LINE)

        # Update Config File
        if self.Config['CONFIG_VERSION'] < LATEST_CFG or DEV:

            self.UpdateConfig()

        else:

            self.con('* Configuration file is up to date')

        # Global / Class Variables
        global MSG, PLUGIN, COLOR, CMDS
        MSG, COLOR, PLUGIN, CMDS = [self.Config[x] for x in ('MESSAGES', 'COLORS', 'SETTINGS', 'COMMANDS')]

        self.prefix = '<%s>%s<end>' % (COLOR['PREFIX'], PLUGIN['PREFIX']) if PLUGIN['PREFIX'] else None
        self.dbname = 'overthrone_db'

        # Use OverThrone icon?
        if not PLUGIN['ENABLE PLUGIN ICON']:

            global PROFILE

            PROFILE = '0'

        # Load Databas
        self.db = data.GetData(self.dbname)

        if not self.db:

            # Default Database
            self.db['KING'] = False
            self.db['KING_NAME'] = False
            self.db['KINGSMEN'] = {}
            self.db['QUEUE'] = {}
            self.db['SINCE'] = 0.0

            self.con('* Creating Database')

        else:

            self.con('* Loading Database')

            # Changes To Database Keys
            if 'KING_NAME' not in self.db:

                self.db['KING_NAME'] = 'Unknown'

            # Update King
            if self.db['KING']:

                for player in self.playerslist():

                    uid = self.playerid(player)

                    if uid == self.db['KING_NAME']:

                        self.db['KING_NAME'] = player.displayName

                self.con('* Updating King data')

        # Create Plugin Commands
        for cmd in CMDS:

            if PLUGIN['ENABLE %s CMD' % cmd]:

                command.AddChatCommand(CMDS[cmd], self.Plugin, '%s_CMD' % cmd.replace(' ','_').lower())

            else:

                CMDS.remove(cmd)

        self.con('* Enabling commands:')

        if CMDS:

            for cmd in CMDS:

                self.con('  - /%s (%s)' % (CMDS[cmd], cmd.title()))

        else: self.con('  - There are no commands enabled')

        command.AddConsoleCommand('overthrone.resetking', self.Plugin, 'plugin_CMD')

        command.AddChatCommand('overthrone', self.Plugin, 'plugin_CMD')

        self.con(LINE)

    # -------------------------------------------------------------------------
    # - ENTITY HOOKS
    def OnEntityDeath(self, vic, hitinfo):
        ''' Hook called whenever an entity dies '''

        # Is victim a player and attacker?
        if not 'corpse' in str(vic) and vic and 'player' in str(vic) and (hitinfo and hitinfo.Initiator and 'player' in str(hitinfo.Initiator)):

            att = hitinfo.Initiator
            vic_id = self.playerid(vic)
            att_id = self.playerid(att)

            # Check if Victim or Attacker are NPCs
            if any(i < 17 for i in (vic_id, att_id)): return

            # Is there any King?
            if self.db['KING']:

                # Check if victim is not the attacker and is the actual King
                if vic_id != att_id and vic_id == self.db['KING']:

                    # Last King timestamp
                    since = self.db['SINCE']

                    # Replace King
                    self.db['KING'] = att_id
                    self.db['KING_NAME'] = att.displayName
                    self.db['SINCE'] = time.time()
                    self.db['KINGSMEN'].clear()

                    # Announce new King to the server
                    self.say(MSG['ANNOUNCE NEW KING'].format(attacker=att.displayName, lastking=vic.displayName), COLOR['SYSTEM'])

                    # Is victim connected?
                    if vic.IsConnected():

                        self.tell(vic, MSG['TELL KINGSHIP LOST'].format(attacker=att.displayName), COLOR['SYSTEM'])

                    # Otherwise add victim to queue for later notification
                    else:

                        self.db['QUEUE'][vic_id] = att.displayName

            # Otherwise name the as new King
            else:

                self.db['KING'] = att_id
                self.db['KING_NAME'] = att.displayName
                self.db['KINGSMEN'].clear()
                self.db['SINCE'] = time.time()

                self.tell(att, MSG['FIRST KING'].format(king=att.displayName), COLOR['SYSTEM'])

    # -------------------------------------------------------------------------
    # - PLAYER HOOKS
    def OnPlayerInit(self, player):
        ''' Hook called when the player has fully connected '''

        # If Kingship has been taken from the player while he was offline
        # inform the player of the event telling the attacker who took the place
        uid = self.playerid(player)

        if uid in self.db['QUEUE']:

            att = self.db['QUEUE'][uid]

            self.tell(player, MSG['NOTIFY KINGSHIP LOSS'].format(attacker=att, king=self.db['KING_NAME']), COLOR['SYSTEM'])

            del self.db['QUEUE'][uid]

        elif uid == self.db['KING']:

            self.db['KING_NAME'] = player.displayName

    # -------------------------------------------------------------------------
    def OnPlayerChat(self, args):

        text = args.GetString(0, 'text')
        player = args.connection.player
        uid = self.playerid(player)

        # Is Player the actual King?
        if uid == self.db['KING']:

            prefix = PLUGIN['KING PREFIX']

            name_color = COLOR['ADMIN NAME'] if player.IsAdmin() else COLOR['PLAYER NAME']

            name = '<%s>%s<end>' % (name_color, player.displayName)

            text = '%s %s: <%s>%s<end>' % (prefix, name, COLOR['KING CHAT'], text)

            self.say(text, COLOR['KING CHAT'], uid, False, False)

            rust.RunServerCommand('global.echo', self.format(text, True))

            return False

    #--------------------------------------------------------------------------
    # - PLUGIN FUNTIONS / HOOKS
    def get_time(self, stamp):
        ''' Returns exact hours, minutes and seconds from a timestamp '''

        m, s = divmod(time.time() - stamp, 60)
        h, m = divmod(m, 60)

        return '%02d:%02d' % (h, m)

    # -------------------------------------------------------------------------
    def playerid(self, player):
        ''' Function to return the player UserID '''

        return rust.UserIDFromPlayer(player)

    # -------------------------------------------------------------------------
    def playerslist(self):
        ''' Returns list of active players in the server '''

        return BasePlayer.activePlayerList

    # -------------------------------------------------------------------------
    def SendHelpText(self, player):
        ''' Hook called from HelpText plugin '''

        self.tell(player, 'For all <#9B7E7A>Over<end><#CA1F0C>Throne<end>\'s commands type <orange>/overthrone help<end>', f=False)

    #--------------------------------------------------------------------------
    # - COMMAND BLOCKS
    def king_CMD(self, player, cmd, args):
        ''' Command that tells who is the current King '''

        uid = self.playerid(player)

        if self.db['KING']:

            since = self.get_time(self.db['SINCE'])

            if uid == self.db['KING']:

                self.tell(player, MSG['YOU ARE KING'].format(time=since), COLOR['SYSTEM'])

            else:

                self.tell(player, MSG['WHO IS KING'].format(time=since, king=self.db['KING_NAME']), COLOR['SYSTEM'])

        else:

            self.tell(player, MSG['NO KING YET'], COLOR['SYSTEM'])

    # -------------------------------------------------------------------------
    def reset_king_CMD(self, player=None, cmd=None, args=None):
        ''' Resets the king and restarts the King system '''

        if player and not player.IsAdmin():

            self.tell(player, MSG['NO ACCESS'], COLOR['SYSTEM'])

            return

        if self.db['KING']:

            self.db['KING'] = False
            self.db['KING_NAME'] = False
            self.db['KINGSMEN'].clear()
            self.db['SINCE'] = 0.0

            self.say(MSG['KING RESETED'], COLOR['SYSTEM'])

    # -------------------------------------------------------------------------
    def plugin_CMD(self, player, cmd, args):
        '''
            - Plugin command - Information of the plugin
            - Help Option - Description of all enabled commands
        '''

        if args and args[0] == 'help':

            self.tell(player, '%s | Commands Description:' % self.prefix, f=False)
            self.tell(player, LINE, f=False)

            for cmd in CMDS:

                i = '%s DESC' % cmd

                if i in MSG: self.tell(player, MSG[i], f=False)

        else:

            self.tell(player, '<size=18><#9B7E7A>Over<end><#CA1F0C>Throne<end></size> <grey>v%s<end>' % self.Version, 'silver', False, '76561198203401038')
            self.tell(player, self.Description, 'silver', False, '76561198203401038')
            self.tell(player, 'Plugin powered by <orange>Oxide 2<end> and developed by <#9810FF>SkinN<end>', 'silver', False, '76561197999302614')

    # -------------------------------------------------------------------------