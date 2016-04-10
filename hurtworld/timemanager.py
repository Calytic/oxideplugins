import re
import TimeManager

DEV = False
LINE = '-'*50

class timemanager:

    def __init__(self):

        self.Title = 'Time Manager'
        self.Version = V(0, 0, 4)
        self.Author = 'SkinN'
        self.Description = 'Controls the day/night timing of the server'
        self.ResourceId = 1530
        self.Settings = {
            'SETTINGS': {
                'PREFIX': '<white>[ <lightblue>TimeManager<end> ]<end>',
                'BROADCAST TO CONSOLE': True,
                'DAY LENGTH (mins)': 15,
                'NIGHT LENGTH (mins)': 7,
                'REMOVE CHAT COLORS': False
            },
            'MESSAGES': {
                'THE TIME': 'It\'s now {time} on day {day}'
            }
        }

    # -------------------------------------------------------------------------
    # - CONFIGURATION / DATABASE SYSTEM
    def LoadDefaultConfig(self):
        '''Hook called when there is no configuration file '''

        self.Config.clear()
        self.Config = self.Settings
        self.SaveConfig()

        self.con('* Loading default configuration file')

    # -------------------------------------------------------------------------
    def UpdateConfig(self):
        '''Function to update the configuration file on plugin Init '''

        # Override config in developer mode is enabled
        if DEV: self.LoadDefaultConfig(); return

        # Start configuration checks
        for section in self.Settings:

            # Is section in the configuration file
            if section not in self.Config:

                # Add section to config
                self.Config[section] = self.Settings[section]

            elif isinstance(self.Settings[section], dict):

                # Check for sub-section
                for sub in self.Settings[section]:

                    if sub not in self.Config[section]:

                        self.Config[section][sub] = self.Settings[section][sub]

        self.SaveConfig()

    # -------------------------------------------------------------------------
    # - MESSAGE SYSTEM
    def con(self, text, f=False):
        '''Function to send a server con message '''

        if self.Config['SETTINGS']['BROADCAST TO CONSOLE'] or f:

            print('[%s] %s' % (self.Title, self.scs(text, True)))

    # -------------------------------------------------------------------------
    def tell(self, player, text, color='silver', f=True):
        '''Function to send a message to a player '''

        if len(self.prefix) and f:

            msg = self.scs('%s <%s>%s<end>' % (self.prefix, color, text))

        else:

            msg = self.scs('<%s>%s<end>' % (color, text))

        hurt.SendChatMessage(player, self.scs(msg))

    # -------------------------------------------------------------------------
    # - PLUGIN HOOKS
    def Init(self):
        '''Hook called when the plugin initializes '''

        self.con(LINE)

        # Update System
        self.UpdateConfig()

        # Global and class variables
        global PLUGIN, MSG
        PLUGIN, MSG = (self.Config[i] for i in ('SETTINGS', 'MESSAGES'))

        self.prefix = PLUGIN['PREFIX'] if PLUGIN['PREFIX'] else ''

        self.con('* Time settings:')
        self.SetDayLength(PLUGIN['DAY LENGTH (mins)'], False)
        self.SetNightLength(PLUGIN['NIGHT LENGTH (mins)'], False)

        # Plugin Command
        command.AddChatCommand('timemanager', self.Plugin, 'plugin_CMD')
        command.AddChatCommand('time', self.Plugin, 'time_CMD')

        self.con(LINE)

    # -------------------------------------------------------------------------
    # - COMMAND FUNCTIONS
    def time_CMD(self, player, command, args):
        '''Time command function '''

        time = str(TimeManager.Instance.GetCurrentGameTime()).split(' on day ')

        self.tell(player, MSG['THE TIME']\
            .replace('{time}', time[0])\
            .replace('{day}', time[1])
        )

    # -------------------------------------------------------------------------
    def plugin_CMD(self, player, command, args):
        '''Plugin command function '''

        self.tell(player, '<lightblue><size=18>%s</size> <grey>v%s<end><end>' % (self.Title, self.Version), f=False)
        self.tell(player, '<silver><orange><size=20>â¢</size><end> %s<end>' % self.Description, f=False)
        self.tell(player, '<silver><orange><size=20>â¢</size><end> Plugin developed by <#9810FF>SkinN<end>, powered by <orange>Oxide 2<end>.<end>', f=False)

    # -------------------------------------------------------------------------
    # - PLUGIN FUNCTIONS / HOOKS
    def SetDayLength(self, length, set_cfg=True):
        '''Changes the day length (in minutes)'''

        # Check if length is an integer
        if isinstance(length, int):

            # Check if length is not 0 otherwise make it the default
            self.daylen = length if length else 15

            # Set the day LENGTH
            TimeManager.Instance.DayLength = float(self.daylen * 60)

            self.con('Day length set to %s minute/s' % self.daylen)

            # Change config values
            if set_cfg:

                PLUGIN['DAY LENGTH (mins)'] = self.daylen
                self.Config['SETTINGS']['DAY LENGTH (mins)'] = self.daylen
                self.SaveConfig()

        else:

            self.con('Warning, the length must be an integer!')

    # -------------------------------------------------------------------------
    def SetNightLength(self, length, set_cfg=True):
        '''Changes the night length (in minutes)'''

        # Check if length is an integer
        if isinstance(length, int):

            # Check if length is not 0 otherwise make it the default
            self.nightlen = length if length else 7

            # Set the night length
            TimeManager.Instance.NightLength = float(self.nightlen * 60)

            self.con('Night length set to %s minute/s' % self.nightlen)

            # Change config values
            if set_cfg:

                PLUGIN['NIGHT LENGTH (mins)'] = self.nightlen
                self.Config['SETTINGS']['NIGHT LENGTH (mins)'] = self.nightlen
                self.SaveConfig()

        else:

            self.con('Warning, the length must be an integer!')

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