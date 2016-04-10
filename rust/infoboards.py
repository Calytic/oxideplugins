import re
import time
import BasePlayer
import TOD_Sky
import ConVar.Server as sv

from System import Action

DEV = False
LATEST_CFG = 2.0
LINE = '-' * 50

class infoboards:

    def __init__(self):

        self.Title = 'Info Boards'
        self.Version = V(2, 0, 0)
        self.Author = 'SkinN'
        self.Description = 'Custom informational boards system'
        self.ResourceId = 1024
        self.Default = {
            'CONFIG_VERSION': LATEST_CFG,
            'SETTINGS': {
                'PREFIX': '<white>[ <lightblue>INFO BOARDS<end> ]<end>',
                'BROADCAST TO CONSOLE': True,
                'SHOW BOARD IN CHAT': True,
                'SHOW BOARD IN CONSOLE': False,
                'ENABLE BOARDS': True
            },
            'MESSAGES': {
                'AVAILABLE BOARDS': 'AVAILABLE BOARDS',
                'NO BOARDS AVAILABLE': '<#E85858>There are\'t any boards available<end>',
                'NO BOARDS FOUND': '<#E85858>No boards found with the name \'{args}\'<end>',
                'MULTIPLE BOARDS FOUND': '<#E85858>Multiple boards found with close to \'{args}\'<end>',
                'BOARDS DESC': '<orange>/info [board name]<end> <grey>-<end> Displays the list of available boards, if given a name then it will display the desired board'
            },
            'COLORS': {
                'PREFIX': '#00EEEE',
                'SYSTEM': 'white'
            },
            'COMMANDS': {
                'BOARDS': ('info', 'boards')
            },
            'BOARDS': {
                'Board Example Title': {
                    'DESC': 'Board Example Description',
                    'LINES': (
                        'Line # 1',
                        'Line # 2',
                        'Line # 3'
                    )
                },
                'Server Info': {
                    'DESC': 'Displays server detailed information',
                    'LINES': (
                        '<green>HOSTNAME: <end><silver>{server.hostname}<end>',
                        '<green>DESCRIPTION: <end><silver>{server.description}<end>',
                        '<green>IP: <end><silver>{server.ip}:{server.port}<end>',
                        '<green>MAP: <end><silver>{server.level}<end>',
                        '<green>LOCAL TIME & DATE: <end><silver>{localtime} {localdate}<end>',
                        '<green>GAME TIME & DATE: <end><silver>{gametime} {gamedate}<end>',
                        '<green>PLAYERS: <silver>{players} / {server.maxplayers} ({sleepers} sleepers)<end> SEED: <silver>{server.seed}<end> WORLD SIZE: <silver>{server.worldsize}<end><end>'
                    )
                }
            }
        }

    # -------------------------------------------------------------------------
    # - CONFIGURATION / DATABASE SYSTEM
    def LoadDefaultConfig(self):
        '''Hook called when there is no configuration file '''

        self.Config = self.Default

        self.con('* Loading default configuration file')

    # -------------------------------------------------------------------------
    def UpdateConfig(self):
        '''Function to update the configuration file on plugin Init '''

        if (self.Config['CONFIG_VERSION'] <= LATEST_CFG - 0.2) or DEV:

            self.Config.clear()

            self.LoadDefaultConfig()

            if not DEV: self.con('* Configuration file forced to reset')

        else:

            self.Config['CONFIG_VERSION'] = LATEST_CFG

            self.con('* Applying new changes to the configuration file')

        self.SaveConfig()

    # -------------------------------------------------------------------------
    # - MESSAGE SYSTEM
    def con(self, text, f=False):
        '''Function to send a server con message '''

        if self.Config['SETTINGS']['BROADCAST TO CONSOLE'] or f:

            print('[%s v%s] %s' % (self.Title, str(self.Version), self.scs(text, True)))

    # -------------------------------------------------------------------------
    def pcon(self, player, text, color='silver'):
        '''Function to send a message to a player console '''

        player.SendConsoleCommand(self.scs('echo <%s>%s<end>' % (color, text)))

    # -------------------------------------------------------------------------
    def tell(self, player, text, color='silver', f=True, profile='0'):
        '''Function to send a message to a player '''

        if len(self.prefix) and f:

            msg = self.scs('%s <%s>%s<end>' % (self.prefix, color, text))

        else:

            msg = self.scs('<%s>%s<end>' % (color, text))

        rust.SendChatMessage(player, msg, None, profile)

    # -------------------------------------------------------------------------
    # - PLUGIN HOOKS
    def Init(self):
        '''Hook called when the plugin initializes '''

        self.con(LINE)

        # Update System
        if self.Config['CONFIG_VERSION'] < LATEST_CFG or DEV:

            self.UpdateConfig()

        else:

            self.con('* Configuration file is up to date')

        global MSG, PLUGIN, COLORS, CMDS, BOARDS
        MSG, COLORS, PLUGIN, CMDS, BOARDS = [self.Config[x] for x in \
        ('MESSAGES', 'COLORS', 'SETTINGS', 'COMMANDS', 'BOARDS')]

        self.prefix = '<%s>%s<end>' % (COLORS['PREFIX'], PLUGIN['PREFIX']) if PLUGIN['PREFIX'] else ''

        # Commands System
        n = 0

        self.con('* Enabling commands:')

        for cmd in CMDS:

            if PLUGIN['ENABLE %s' % cmd]:

                n += 1

                if isinstance(CMDS[cmd], tuple):

                    for i in CMDS[cmd]:

                        command.AddChatCommand(i, self.Plugin, '%s_CMD' % cmd.replace(' ','_').lower())

                    self.con('  - %s (/%s)' % (cmd.title(), ', /'.join(CMDS[cmd])))

                else:

                    command.AddChatCommand(CMDS[cmd], self.Plugin, '%s_CMD' % cmd.replace(' ','_').lower())

                    self.con('  - %s (/%s)' % (cmd.title(), CMDS[cmd]))

        if not n:

            self.con('  - No commands are enabled')

        # Plugin Command
        command.AddChatCommand('infoboards', self.Plugin, 'plugin_CMD')

        self.con(LINE)

    # -------------------------------------------------------------------------
    # - COMMAND FUNCTIONS
    def boards_CMD(self, player, cmd, args):
        '''Boards command function '''

        if args:

            args = ' '.join(args)

            # Search for boards close to the given arguments
            found = [i for i in BOARDS if args.lower() in i.lower()]

            # No boards found?
            if len(found) == 0:

                self.tell(player, MSG['NO BOARDS FOUND'].replace('{args}', args), COLORS['SYSTEM'])

                return

            # Multiple boards found?
            elif len(found) > 1:

                self.tell(player, MSG['MULTIPLE BOARDS FOUND'].replace('{args}', args), COLORS['SYSTEM'])

                return

            # Display board to player
            else:

                board = BOARDS[found[0]]

                if PLUGIN['SHOW BOARD IN CHAT']:

                    self.tell(player, '%s <white>%s<end>:' % (self.prefix, found[0].upper()), f=False)
                    self.tell(player, LINE, f=False)

                if PLUGIN['SHOW BOARD IN CONSOLE']:

                    self.pcon(player, LINE)
                    self.pcon(player, '%s <white>%s<end>:' % (self.prefix, found[0].upper()))
                    self.pcon(player, LINE)

                for line in board['LINES']:

                    line = self.name_formats(line)

                    if PLUGIN['SHOW BOARD IN CHAT']:

                        self.tell(player, line, 'white', f=False)

                    if PLUGIN['SHOW BOARD IN CONSOLE']:

                        self.pcon(player, line, 'white')

        else:

            # Is there any boards?
            if BOARDS:

                # Print available boards
                self.tell(player, '%s %s:' % (self.prefix, MSG['AVAILABLE BOARDS']), f=False)

                for i in BOARDS:

                    self.tell(player, '<orange>%s<end> - <white>%s<end>' % (i, BOARDS[i]['DESC']), f=False)

            else:

                self.tell(player, MSG['NO BOARDS AVAILABLE'], COLORS['SYSTEM'])

    # -------------------------------------------------------------------------
    def plugin_CMD(self, player, cmd, args):
        '''Plugin command function '''

        if args:

            if args[0] == 'help':

                self.tell(player, '%sCOMMANDS DESCRIPTION:' % ('%s ' % self.prefix), f=False)
                self.tell(player, LINE, f=False)

                for cmd in CMDS:

                    i = '%s DESC' % cmd

                    if i in MSG: self.tell(player, MSG[i], f=False)

            elif player.IsAdmin():

                pass

        else:

            self.tell(player, '<lightblue><size=18>%s</size><end> <grey>v%s<end> - <white>%s<end>' % (self.Title, self.Version, self.Description), f=False)
            self.tell(player, 'Plugin developed by <#9810FF>SkinN<end>, powered by <orange>Oxide 2<end>.', profile='76561197999302614', f=False)

    # -------------------------------------------------------------------------
    # - PLUGIN FUNCTIONS / HOOKS
    def name_formats(self, msg):
        ''' Function to format name formats on advert messages '''

        return msg.format(
            players=len(BasePlayer.activePlayerList),
            sleepers=len(BasePlayer.sleepingPlayerList),
            localtime=time.strftime('%H:%M'),
            localdate=time.strftime('%m/%d/%Y'),
            gametime=' '.join(str(TOD_Sky.Instance.Cycle.DateTime).split()[1:]),
            gamedate=str(TOD_Sky.Instance.Cycle.DateTime).split()[0],
            server=sv
        )

    # -------------------------------------------------------------------------
    def scs(self, text, con=False):
        '''
            Replaces color names and RGB hex code into HTML code
        '''

        colors = (
            'red', 'blue', 'green', 'yellow', 'white', 'black', 'cyan',
            'lightblue', 'lime', 'purple', 'darkblue', 'magenta', 'brown',
            'orange', 'olive', 'gray', 'grey', 'silver', 'maroon'
        )

        name = r'\<(\w+)\>'
        hexcode = r'\<(#\w+)\>'
        end = 'end'

        if con:
            for x in (end, name, hexcode):
                for c in re.findall(x, text):
                    if c.startswith('#') or c in colors or x == end:
                        text = text.replace('<%s>' % c, '')
        else:
            text = text.replace('<%s>' % end, '</color>')
            for f in (name, hexcode):
                for c in re.findall(f, text):
                    if c.startswith('#') or c in colors:
                        text = text.replace('<%s>' % c, '<color=%s>' % c)
        return text

    # -------------------------------------------------------------------------
    def SendHelpText(self, player):
        '''Hook called from HelpText plugin when /help is triggered '''

        self.tell(player, 'For all <lightblue>Info Boards\'s<end> commands type <orange>/template help<end>', f=False)