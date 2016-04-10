import re
import ChatManager

DEV = False

class rules:

    def __init__(self):

        self.Title = 'Server Rules'
        self.Version = V(1, 0, 1)
        self.Author = 'SkinN'
        self.Description = 'Displays customized rules for your server'
        self.ResourceId = 1514
        self.Default = {
            'SETTINGS': {
                'PREFIX': '<white>[<end> <cyan>SERVER RULES<end> <white>]<end>',
                'COMMAND TRIGGER': 'rules',
                'DEFAULT LANGUAGE': 'en',
                'ENUMERATE RULES': True,
                'REMOVE CHAT COLORS': False,
            },
            'MESSAGES': {
                'TITLE PREFIX': '<orange>â¢<end> <silver>SERVER RULES<end>:',
                'AVAILABLE LANGS': 'Available languages',
                'LANG NOT EXIST': 'Could not find <red>{lang}<end> language, type <lime>/rules list<end> for the full list of languages.'
            },
            'RULES': {
                'EN': (
                    'Cheating is strictly prohibited.',
                    'Respect all players',
                    'Avoid spam in chat.',
                    'Play fair and don\'t abuse of bugs/exploits.'
                ),
                'PT': (
                    'Usar cheats e totalmente proibido.',
                    'Respeita todos os jogadores.',
                    'Evita spam no chat.',
                    'Nao abuses de bugs ou exploits.'
                ),
                'FR': (
                    'Tricher est strictement interdit.',
                    'Respectez tous les joueurs.',
                    'Ãvitez le spam dans le chat.',
                    'Jouer juste et ne pas abuser des bugs / exploits.'
                ),
                'ES': (
                    'Los trucos estÃ¡n terminantemente prohibidos.',
                    'Respeta a todos los jugadores.',
                    'Evita el Spam en el chat.',
                    'Juega limpio y no abuses de bugs/exploits.'
                ),
                'DE': (
                    'Cheaten ist verboten!',
                    'Respektiere alle Spieler',
                    'Spam im Chat zu vermeiden.',
                    'Spiel fair und missbrauche keine Bugs oder Exploits.'
                ),
                'TR': (
                    'Hile kesinlikle yasaktÄ±r.',
                    'TÃ¼m oyuncular SaygÄ±.',
                    'Sohbet Spam kaÃ§Ä±nÄ±n.',
                    'Adil oynayÄ±n ve bÃ¶cek / aÃ§Ä±klarÄ± kÃ¶tÃ¼ye yok.'
                ),
                'IT': (
                    'Cheating Ã¨ severamente proibito.',
                    'Rispettare tutti i giocatori.',
                    'Evitare lo spam in chat.',
                    'Fair Play e non abusare di bug / exploit.'
                ),
                'DK': (
                    'Snyd er strengt forbudt.',
                    'Respekter alle spillere.',
                    'UndgÃ¥ spam i chatten.',
                    'Spil fair og misbrug ikke bugs / exploits.'
                ),
                'RU': (
                    'ÐÐ°Ð¿ÑÐµÑÐµÐ½Ð¾ Ð¸ÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°ÑÑ ÑÐ¸ÑÑ.',
                    'ÐÐ°Ð¿ÑÐµÑÐµÐ½Ð¾ ÑÐ¿Ð°Ð¼Ð¸ÑÑ Ð¸ Ð¼Ð°ÑÐµÑÐ¸ÑÑÑÑ.',
                    'Ð£Ð²Ð°Ð¶Ð°Ð¹ÑÐµ Ð´ÑÑÐ³Ð¸Ñ Ð¸Ð³ÑÐ¾ÐºÐ¾Ð².',
                    'ÐÐ³ÑÐ°Ð¹ÑÐµ ÑÐµÑÑÐ½Ð¾ Ð¸ Ð½Ðµ Ð¸ÑÐ¿Ð¾Ð»ÑÐ·ÑÐ¹ÑÐµ Ð±Ð°Ð³Ð¸ Ð¸ Ð»Ð°Ð·ÐµÐ¹ÐºÐ¸.'
                ),
                'NL': (
                    'Vals spelen is ten strengste verboden.',
                    'Respecteer alle spelers',
                    'Vermijd spam in de chat.',
                    'Speel eerlijk en maak geen misbruik van bugs / exploits.'
                ),
                'UA': (
                    'ÐÐ±Ð¼Ð°Ð½ ÑÑÐ²Ð¾ÑÐ¾ Ð·Ð°Ð±Ð¾ÑÐ¾Ð½ÐµÐ½Ð¾.',
                    'ÐÐ¾Ð²Ð°Ð¶Ð°Ð¹ÑÐµ Ð²ÑÑÑ Ð³ÑÐ°Ð²ÑÑÐ²',
                    'Ð©Ð¾Ð± ÑÐ½Ð¸ÐºÐ½ÑÑÐ¸ ÑÐ¿Ð°Ð¼Ñ Ð² ÑÐ°ÑÑ.',
                    'ÐÑÐ°ÑÐ¸ ÑÐµÑÐ½Ð¾ Ñ Ð½Ðµ Ð·Ð»Ð¾Ð²Ð¶Ð¸Ð²Ð°ÑÐ¸ Ð¿Ð¾Ð¼Ð¸Ð»ÐºÐ¸ / Ð¿Ð¾Ð´Ð²Ð¸Ð³Ð¸.'
                ),
                'RO': (
                    'Cheaturile sunt strict interzise!',
                    'RespectaÈi toÈi jucÄtorii!',
                    'EvitaÈi spamul Ã®n chat!',
                    'JucaÈi corect Èi nu abuzaÈi de bug-uri/exploituri!'
                ),
                'HU': (
                    'CsalÃ¡s szigorÃºan tilos.',
                    'Tiszteld minden jÃ¡tÃ©kostÃ¡rsad.',
                    'KerÃ¼ld a spammolÃ¡st a chaten.',
                    'JÃ¡tssz tisztessÃ©gesen Ã©s nem Ã©lj vissza a hibÃ¡kkal.'
                )
            }
        }

    # -------------------------------------------------------------------------
    # - CONFIGURATION / DATABASE SYSTEM
    def LoadDefaultConfig(self):
        '''Hook called when there is no configuration file '''

        self.Config.Clear()
        self.Config = self.Default
        self.SaveConfig()

    # -------------------------------------------------------------------------
    def UpdateConfig(self):
        '''Function to update the configuration file on plugin Init '''

        # Override config in developer mode is enabled
        if DEV: self.LoadDefaultConfig(); return

        # Start configuration checks
        for section in self.Default:

            # Is section in the configuration file
            if section not in self.Config:

                # Add section to config
                self.Config[section] = self.Default[section]

            elif isinstance(self.Default[section], dict):

                # Check for sub-section
                for sub in self.Default[section]:

                    if sub not in self.Config[section]:

                        self.Config[section][sub] = self.Default[section][sub]

        self.SaveConfig()

    # -------------------------------------------------------------------------
    # - PLUGIN HOOKS
    def Init(self):
        '''Hook called when the plugin initializes '''

        # Update System
        self.UpdateConfig()

        # Global and Plugin variables
        global MSG, PLUGIN, RULES
        MSG, PLUGIN, RULES = [self.Config[x] for x in ('MESSAGES', 'SETTINGS', 'RULES')]

        self.prefix = PLUGIN['PREFIX'] if PLUGIN['PREFIX'] else ''

        # Command trigger
        command.AddChatCommand(PLUGIN['COMMAND TRIGGER'], self.Plugin, 'rules_CMD')

        # Plugin Command
        command.AddChatCommand('serverrules', self.Plugin, 'plugin_CMD')

    # -------------------------------------------------------------------------
    # - COMMAND FUNCTIONS
    def rules_CMD(self, player, command, args):
        '''Rules command function '''

        key = PLUGIN['DEFAULT LANGUAGE'].upper()

        if args:

            # Get arguments as a string
            arg = ' '.join(args).upper()

            if arg.lower() == 'list':

                self.tell(player, '%s %s: <lightblue>%s<end>' % (self.prefix, MSG['AVAILABLE LANGS'], ', '.join(RULES.keys())))

                return

            # Check if argument is a valid key
            elif arg in RULES:

                key = arg

            else:

                self.tell(player, '%s %s' % (self.prefix, MSG['LANG NOT EXIST'].replace('{lang}', 'arg')))

                return

        # Get list of rules
        lis = RULES[key]

        self.tell(player, MSG['TITLE PREFIX'])

        if PLUGIN['ENUMERATE RULES']:

            for n, i in enumerate(lis):

                self.tell(player, '<orange>%s.<end> <lightblue>%s<end>' % (n+1, i))

        else:

            for i in lis:

                self.tell(player, '<lightblue>%s<end>' % i)

    # -------------------------------------------------------------------------
    def plugin_CMD(self, player, command, args):
        '''Plugin command function '''

        self.tell(player, '<lightblue><size=18>%s</size> <grey>v%s<end><end>' % (self.Title, self.Version))
        self.tell(player, '<silver><orange><size=20>â¢</size><end> %s<end>' % self.Description)
        self.tell(player, '<silver><orange><size=20>â¢</size><end> Plugin developed by <#9810FF>SkinN<end>, powered by <orange>Oxide 2<end>.<end>')

    # -------------------------------------------------------------------------
    # - PLUGIN FUNCTIONS / HOOKS
    def playerid(self, player):
        '''Function to return the player UID '''

        return str(player.SteamId)

    # -------------------------------------------------------------------------
    # - MESSAGE SYSTEM
    def tell(self, player, text):
        '''Function to send a message to a player '''

        ChatManager.Instance.AppendChatboxServerSingle(self.scs(text, self.Config['SETTINGS']['REMOVE CHAT COLORS']), player.Player)

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