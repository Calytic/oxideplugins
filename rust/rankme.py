import re
import BasePlayer
import UnityEngine.Random as random

from UnityEngine import Vector3
from System import Action

DEV = False
LINE = '-' * 50
DBNAME = 'rankme-db'

class rankme:

    def __init__(self):

        self.Title = 'Rank-ME'
        self.Version = V(2, 6, 0)
        self.Author = 'SkinN'
        self.Description = 'Complex ranking system based on player statistics'
        self.ResourceId = 1074
        self.Default = {
            'SETTINGS': {
                'PREFIX': '<white>[ <orange>RANK-ME<end> ]<end>',
                'BROADCAST TO CONSOLE': True,
                'LISTS MAX PLAYERS': 5,
                'AUTO-SAVE INTERVAL': 30,
                'TOP3 ADVERT INTERVAL': 15,
                'SHOW TOP IN CHAT': True,
                'SHOW TOP IN CONSOLE': False,
                'SHOW RANK IN CHAT': True,
                'SHOW RANK IN CONSOLE': False,
                'ANNOUNCE DATABASE RESET': True,
                'INFORM PLAYER ENTER/EXIT ZONE': True,
                'ENABLE RANK WHITELIST': False,
                'ENABLE AUTO-SAVE': True,
                'ENABLE TOP3 ADVERT': True,
                'ENABLE RESET DATABASE': True,
                'ENABLE PLAYER RESET': True,
                'ENABLE RANK': True,
                'ENABLE TOP': True,
                'USE SEPARATOR LINES': False,
                'ENABLE ZONE MANAGER SUPPORT': False,
                'RESTRICTED TO ADMINS': (),
                'RESTRICTED TO PLAYERS': ('sdr','barrels'),
                'RANK WHITELIST': ('pvpkills', 'deaths', 'kdr', 'bullets'),
                'ZONES WHITELIST': ()
            },
            'MESSAGES': {
                'TOP DESC': '<orange>/top [list name]<end> <grey>-<end> Shows the Top PVP Kills, or any other list like Deaths, KDR, etc.',
                'RANK DESC': '<orange>/rank<end> - Shows player stats information',
                'PLAYER RESET DESC': '<orange>/resetme<end> <grey>-<end> Allows player to reset own data',
                'ADMIN TELL RESET': '{player} data has been reset.',
                'DATA SAVED': 'Database has been saved.',
                'DATA RESET': 'Database has been reset.',
                'PLAYER DATA RESET': 'Your stats have been reset.',
                'MULTI PLAYERS FOUND': 'Found multiple players with that name.',
                'NO PLAYERS FOUND': 'No Players found with that name.',
                'RANK INFO': 'Your Personal Stats',
                'CHECK CONSOLE NOTE': 'Check the console (press F1) for more info.',
                'TOP TITLE': 'Top {list}',
                'LIST NOT FOUND': 'No list found with that name',
                'NO PLAYERS TO LIST': 'There are no valid players to list on Top <lime>{list}<end>',
                'TOP3 ADVERT': '<lime>{list}<end> Top 3: <lightblue>{top}<end>',
                'LIST RESTRICTED': '<lime>{list}<end> is restricted by the Admins!',
                'AVAILABLE LISTS': 'Available Lists (<lime>{total}<end>)',
                'MULTIPLE LISTS FOUND': 'Found multiple lists close to the given name.',
                'ON ZONE ENTER': 'You are now in a Ranked zone',
                'ON ZONE EXIT': 'You are no longer in a Ranked zone'
            },
            'COLORS': {
                'PREFIX': '#00EEEE',
                'SYSTEM': 'white'
            },
            'LISTS': {
                'PVPKILLS': 'PVP Kills',
                'NPCKILLS': 'Human NPC Kills',
                'DEATHS': 'Deaths',
                'KDR': 'Kill/Death Ratio',
                'SUICIDES': 'Suicides',
                'SDR': 'Suicides/Kills Ratio',
                'SLEEPERS': 'Sleepers',
                'ANIMALS': 'Animal Kills',
                'PVPDISTANCE': 'PVP Distance (In Meters)',
                'PVEDISTANCE': 'PVE Distance (In Meters)',
                'RESOURCES': 'Resources Gathered',
                'WOUNDED': 'Times Wounded',
                'ROCKETS': 'Rockets Fired',
                'BULLETS': 'Bullets Fired',
                'ARROWS': 'Arrows Fired',
                'EXPLOSIVES': 'Explosives Used',
                'HELIS': 'Helicopters Destroyed',
                'BARRELS': 'Barrels Destroyed',
                'HEALED': 'Times Healed',
                'CRAFTED': 'Items Crafted',
                'DEMOLISHED': 'Structures Demolished',
                'BUILT': 'Structures Built',
                'REPAIRED': 'Structures Repaired',
                'UPGRADED': 'Structures Upgraded',
                'TURRETS': 'Auto-Turrets Kills',
                'BPREVEALED': 'Blueprints Revealed'
            },
            'STRINGS': {
                'RANK': 'Rank Position',
                'NAME': 'Name'
            },
            'COMMANDS': {
                'RANK': 'rank',
                'TOP': 'top',
                'PLAYER RESET': 'resetme'
            }
        }

    # -------------------------------------------------------------------------
    # - CONFIGURATION / DATABASE SYSTEM
    def LoadDefaultConfig(self):
        '''Hook called when there is no configuration file '''

        self.Config.clear()
        self.Config = self.Default
        self.SaveConfig()

    # -------------------------------------------------------------------------
    def UpdateConfig(self):
        '''Function to update the configuration file on plugin Init '''

        # Override config in developer mode is enabled
        if DEV: self.LoadDefaultConfig(); return

        # Remove config versioning
        if 'CONFIG_VERSION' in self.Config:

            del self.Config['CONFIG_VERSION']

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
    def save_data(self, args=None):
        '''Function to save the plugin database '''

        data.SaveData(DBNAME)

        self.con('Saving database')

    # -------------------------------------------------------------------------
    def reset_data(self):
        '''Function to reset the plugin database '''

        # Full database reset
        self.db.clear()

        self.con('Reseting database')

        # Re-add all active players to database
        for i in BasePlayer.activePlayerList:

            self.store_player(i)

        self.save_data()

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
    def say(self, text, color='silver', f=True, profile='0'):
        '''Function to send a message to all players '''

        if len(self.prefix) and f:

            msg = self.scs('%s <%s>%s<end>' % (self.prefix, color, text))

        else:

            msg = self.scs('<%s>%s<end>' % (color, text))

        rust.BroadcastChat(msg, None, profile)

        self.con(text)

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
        self.UpdateConfig()

        global MSG, PLUGIN, COLORS, CMDS, LISTS, STRINGS
        MSG, COLORS, PLUGIN, CMDS, LISTS, STRINGS = [self.Config[x] for x in \
        ('MESSAGES', 'COLORS', 'SETTINGS', 'COMMANDS', 'LISTS', 'STRINGS')]

        self.prefix = '<%s>%s<end>' % (COLORS['PREFIX'], PLUGIN['PREFIX']) if PLUGIN['PREFIX'] else ''
        self.db = data.GetData(DBNAME)
        self.autosave_loop = False
        self.helis_cache = {}
        self.timers = {}
        self.keys = {
            'pvpkills': 0,
            'npckills': 0,
            'deaths': 0,
            'kdr': 0.0,
            'suicides': 0,
            'sdr': 0.0,
            'sleepers': 0,
            'animals': 0,
            'pvpdistance': 0.0,
            'pvedistance': 0.0,
            'resources': 0,
            'wounded': 0,
            'rockets': 0,
            'arrows': 0,
            'bullets': 0,
            'helis': 0,
            'explosives': 0,
            'barrels': 0,
            'healed': 0,
            'crafted': 0,
            'demolished': 0,
            'built': 0,
            'repaired': 0,
            'upgraded': 0,
            'turrets': 0,
            'bprevealed': 0
        }

        # Zones Manager zones whitelist
        self.zones = PLUGIN['ZONES WHITELIST']
        if isinstance(self.zones, str):
            self.zones = [self.zones]
        self.zones = { i : [] for i in self.zones }

        # Initiate Database
        for i in BasePlayer.activePlayerList:

            self.store_player(i)

        # Start Timers
        self.con('* Starting timers:')

        for i in (
                    ('AUTO-SAVE', self.save_data),
                    ('TOP3 ADVERT', self.top_advert)
                ):

            name, func = i

            # Is System Enabled?
            if PLUGIN['ENABLE ' + name]:

                a = name + ' INTERVAL'

                if a in PLUGIN:

                    mins = PLUGIN[a]
                    secs = mins * 60 if mins else 60

                    self.timers[name] = timer.Repeat(secs, 0, Action(func), self.Plugin)

                    self.con('  - Started %s timer, set to %s minute/s' % (name.title(), mins))

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

        if not n: self.con('  - No commands are enabled')

        # Plugin Command
        command.AddChatCommand('rankme', self.Plugin, 'plugin_CMD')

        self.con(LINE)

    # -------------------------------------------------------------------------
    def Unload(self):
        '''Hook called on plugin unload '''

        # Destroy timers
        for i in self.timers:

            self.timers[i].Destroy()

        # Save database
        self.save_data()

    # -------------------------------------------------------------------------
    # - PLAYER HOOKS
    def OnPlayerInit(self, player):
        '''Hook called when a player initiates '''

        self.store_player(player)

    # -------------------------------------------------------------------------
    def OnPlayerDisconnect(self, player):
        '''Hook called when a player leaves the server'''

        self._RemovePlayerFromZones(player)

    # -------------------------------------------------------------------------
    def OnBlueprintReveal(self, item, revealed, player):
        '''Called when a player attempts to reveal a blueprint'''

        uid = self.playerid(player)

        if uid in self.db and self._IsPlayerInZone(player):

            self.db[uid]['bprevealed'] += 1

    # -------------------------------------------------------------------------
    def OnDispenserGather(self, dis, ent, item):
        '''Called before the player is given items from a resource '''

        if ent.ToPlayer():

            uid = self.playerid(ent)

            if uid in self.db and self._IsPlayerInZone(ent):

                self.db[uid]['resources'] += item.amount

    # -------------------------------------------------------------------------
    def OnCollectiblePickup(self, item, player):
        '''
            Called when a player collects an item
        '''

        uid = self.playerid(player)

        if uid in self.db and self._IsPlayerInZone(player):

            self.db[uid]['resources'] += item.amount

    # -------------------------------------------------------------------------
    def OnItemCraftFinished(self, task, item):
        '''Called right after an item has been crafted'''

        uid = self.playerid(task.owner)

        if uid in self.db and self._IsPlayerInZone(task.owner):

            self.db[uid]['crafted'] += 1

    # -------------------------------------------------------------------------
    def OnHealingItemUse(self, item, target):
        '''Called right before a Syringe or Medkit item is used'''

        uid = self.playerid(target)

        if uid in self.db and self._IsPlayerInZone(target):

            self.db[uid]['healed'] += 1

    # -------------------------------------------------------------------------
    def CanBeWounded(self, player, info):
        '''Called when a player dies '''

        uid = self.playerid(player)

        if uid in self.db and self._IsPlayerInZone(player):

            self.db[uid]['wounded'] += 1

    # -------------------------------------------------------------------------
    def OnWeaponFired(self, projectile, player, ammo, projectiles):
        '''Called when a player fires a weapon '''

        uid = self.playerid(player)

        if uid in self.db and self._IsPlayerInZone(player):

            if 'arrow' in str(ammo):

                self.db[uid]['arrows'] += 1

            else:

                self.db[uid]['bullets'] += 1

    # -------------------------------------------------------------------------
    def OnRocketLaunched(self, player, entity):
        '''Called when a player launches a rocket '''

        uid = self.playerid(player)

        if uid in self.db and self._IsPlayerInZone(player):

            self.db[uid]['rockets'] += 1

    # -------------------------------------------------------------------------
    def OnExplosiveThrown(self, player, entity):
        '''Called when a player throws an explosive '''

        uid = self.playerid(player)

        if uid in self.db and self._IsPlayerInZone(player):

            if any(i in str(entity) for i in ('explosive', 'grenade')) and 'smoke' not in str(entity):

                self.db[uid]['explosives'] += 1

    # -------------------------------------------------------------------------
    # - STRUCTURE HOOKS
    def OnEntityBuilt(self, planner, component):
        '''Called when any structure is built (walls, ceilings, stairs, etc.)'''

        # Check for a valid player
        if planner and planner.ownerPlayer:

            uid = self.playerid(planner.ownerPlayer)

            if uid in self.db and self._IsPlayerInZone(planner.ownerPlayer):

                self.db[uid]['built'] += 1

    # -------------------------------------------------------------------------
    def OnStructureDemolish(self, block, player, unknown):
        '''
            Called when a player selects DemolishImmediate
            from the BuildingBlock menu
        '''
        
        uid = self.playerid(player)

        if uid in self.db and self._IsPlayerInZone(player):

            self.db[uid]['demolished'] += 1

    # -------------------------------------------------------------------------
    def OnStructureRepair(self, block, player):
        '''Called when a player repairs a BuildingBlock'''
        
        uid = self.playerid(player)

        if uid in self.db and self._IsPlayerInZone(player):

            self.db[uid]['repaired'] += 1

    # -------------------------------------------------------------------------
    def OnStructureUpgrade(self, block, player, grade):
        '''Called when a player upgrades the grade of a BuildingBlock'''
        
        uid = self.playerid(player)

        if uid in self.db and self._IsPlayerInZone(player):

            self.db[uid]['upgraded'] += 1

    # -------------------------------------------------------------------------
    # - SERVER HOOKS
    def OnEntityDeath(self, ent, info):
        '''Called when an entity dies'''

        # Death info
        ini = info.Initiator if info and info.Initiator else False

        # Is entity a Player?
        if ent.ToPlayer():

            vic_uid = self.playerid(ent)

            # Is Player not a NPC or has it data?
            if len(vic_uid) == 17 and vic_uid in self.db and self._IsPlayerInZone(ent):

                # Victim info
                vic = self.db[vic_uid]
                dmg = str(ent.lastDamage)

                vic['deaths'] += 1

                if vic['deaths']:
                    
                    vic['kdr'] = self._float(float(vic['pvpkills']) / vic['deaths'])

                # Was it a suicide?
                if dmg == 'Suicide':

                    vic['suicides'] += 1

                    if vic['deaths']:

                        vic['sdr'] = self._float(float(vic['suicides']) / vic['deaths'])

                self._RemovePlayerFromZones(ent)

        # Check for attacker
        if ini:

            # Is attacker a player and not the victim?
            if ini.ToPlayer() and ini != ent and self._IsPlayerInZone(ini):

                att_uid = self.playerid(ini)

                # Is Player not a NPC or has it data?
                if len(att_uid) == 17 and att_uid in self.db:

                    att = self.db[att_uid]

                    # Check if victim is a Player
                    if ent.ToPlayer() and self._IsPlayerInZone(ent):

                        # Victim UID
                        vic_uid = self.playerid(ent)

                        if len(vic_uid) == 17:

                            att['pvpkills'] += 1

                            if att['deaths']:

                                att['kdr'] = self._float(float(att['pvpkills']) / att['deaths'])

                            # Is victim sleeping?
                            if ent.IsSleeping():

                                att['sleepers'] += 1

                            # Check for distance
                            dis = Vector3.Distance(ini.transform.position, ent.transform.position)

                            if dis > att['pvpdistance']:

                                att['pvpdistance'] = dis

                        # Else is an Human NPC
                        else:

                            att['npckills'] += 1

                        self._RemovePlayerFromZones(ent)

                    # Is victim an Animal?
                    if 'animal' in str(ent):

                        att['animals'] += 1

                        # Check for distance
                        dis = Vector3.Distance(ini.transform.position, ent.transform.position)

                        if dis > att['pvedistance']:

                            att['pvedistance'] = dis

                    # Is victim a Barrel?
                    if 'barrel' in str(ent):

                        att['barrels'] += 1

                    if 'autoturret' in str(ent):

                        att['turrets'] += 1

                    self._RemovePlayerFromZones(ini)

        # Is victim an Helicopter?
        if '/patrolhelicopter.prefab' in str(ent) and str(ent) in self.helis_cache:

            uid = self.helis_cache[str(ent)]

            if len(uid) == 17 and uid in self.db:

                self.db[uid]['helis'] += 1

            # De-cache helicopter
            del self.helis_cache[str(ent)]

    # -------------------------------------------------------------------------
    def OnEntityTakeDamage(self, ent, info):
        '''Called when an entity takes damage'''

        # Is victim an Helicopter?
        if '/patrolhelicopter.prefab' in str(ent):

            # Death info
            ini = info.Initiator if info and info.Initiator else False

            # Is Attacker a valid player?
            if ini and ini.ToPlayer() and self._IsPlayerInZone(ini):
                
                # Cache heli and its attacker
                self.helis_cache[str(ent)] = self.playerid(ini)

    # -------------------------------------------------------------------------
    # - COMMAND FUNCTIONS
    def rank_CMD(self, player, cmd, args):
        '''Reset Database command function '''

        uid = self.playerid(player)

        # Is Player in database?
        if uid in self.db:

            # Player Data
            ply = self.db[uid]

            # Make list of lines
            l = ['%s %s:' % (self.prefix, MSG['RANK INFO'])]

            if PLUGIN['USE SEPARATOR LINES']: l.append(LINE)

            # Check keys before printing
            k = []

            for i in ply:

                if i not in ('name', 'admin'):

                    # Is whitelist enabled?
                    if PLUGIN['ENABLE RANK WHITELIST']:

                        # Is key in whitelist?
                        if i in PLUGIN['RANK WHITELIST']:

                            k.append('<lightblue>%s<end>: <white>%s<end>' % (LISTS[i.upper()], ply[i]))

                    else:

                        k.append('<lightblue>%s<end>: <white>%s<end>' % (LISTS[i.upper()], ply[i]))

            # Slipt keys into groups of 2
            k = [k[i:i+2] for i in xrange(0, len(k), 2)]

            for i in k:

                l.append(' | '.join(i))

            if PLUGIN['USE SEPARATOR LINES']: l.append(LINE)

            # Send messages to chat / console
            for i in l:

                if PLUGIN['SHOW RANK IN CHAT']:

                    self.tell(player, i, f=False)

                if PLUGIN['SHOW RANK IN CONSOLE']:

                    self.pcon(player, i)

            if PLUGIN['SHOW RANK IN CONSOLE']:

                self.tell(player, LINE, f=False)
                self.tell(player, MSG['CHECK CONSOLE NOTE'], COLORS['SYSTEM'], f=False)

    # -------------------------------------------------------------------------
    def top_CMD(self, player, cmd, args):
        '''Top List command function'''

        uid = self.playerid(player)
        ply = self.db[uid]
        key = 'pvpkills'

        # Check if any list was requested
        if args:

            args = ' '.join(args).lower()

            # Check if argument is lists to print all the available lists
            if args == 'lists':

                adm = self.db[uid]['admin']
                lis = []

                # Gather all the available lists by checking if keys
                # are valid lists for the player
                for i in self.keys:

                    if i not in PLUGIN['RESTRICTED TO PLAYERS']:

                        if not adm or adm and i not in PLUGIN['RESTRICTED TO ADMINS']:

                            lis.append(LISTS[i.upper()])

                total = str(len(lis))

                if lis:

                    lis = [lis[n:n+2] for n in xrange(0, len(lis), 2)]

                    self.tell(player, '%s %s:' % (self.prefix, MSG['AVAILABLE LISTS'].replace('{total}', total)), f=False)

                    for i in lis:

                        self.tell(player, '<lightblue>%s<end>' % '<silver>,<end> '.join(i), f=False)

                return

            else:

                key = False

                if args in self.keys:

                    key = args

                else:

                    # Check whether the argument is close to any of the official keys
                    check = [i for i in self.keys if args in i]

                    if len(check) == 1: # Found the key

                        key = check[0]

                    elif len(check) == 2: # Found multiple keys close to the argument

                        self.tell(player, MSG['MULTIPLE LISTS FOUND'], COLORS['SYSTEM'])

                        return

                    else:

                        # Try to find close key by full name
                        check = [i.lower() for i in LISTS if args in LISTS[i].lower()]

                        if len(check) == 1: # Found the key

                            key = check[0]

                        elif len(check) == 2: # Found multiple keys close to the argument

                            self.tell(player, MSG['MULTIPLE LISTS FOUND'], COLORS['SYSTEM'])

                            return

                        else:

                            self.tell(player, MSG['LIST NOT FOUND'], COLORS['SYSTEM'])

                            return

        # Key name string
        name = LISTS[key.upper()]

        # Is key restricted to all players?
        if key not in PLUGIN['RESTRICTED TO PLAYERS']:

            # Start row of lines to display
            row = ['%s %s:' % (self.prefix, MSG['TOP TITLE'].replace('{list}', name))]

            if PLUGIN['USE SEPARATOR LINES']: row.append(LINE)

            # Sort players stats and enumerate them respectively
            for n, p in enumerate(self._sort(key)):

                t = self.db[p]

                row.append(('<orange>%s.<end> <lightblue>%s (<white>%s<end>)<end>' % (n+1, t['name'], t[key] if key in t else self.keys[key]), p))

            if PLUGIN['USE SEPARATOR LINES']: row.append(LINE)

            if PLUGIN['SHOW TOP IN CHAT']:

                if PLUGIN['SHOW TOP IN CONSOLE']:

                    row.append('<%s>%s<end>' % (COLORS['SYSTEM'], MSG['CHECK CONSOLE NOTE']))

                for i in row:

                    if isinstance(i, tuple):

                        a, b = i

                        self.tell(player, a, 'silver', False, b)

                    else:

                        self.tell(player, i, 'silver', False)

            # Send message to chat / console
            if PLUGIN['SHOW TOP IN CONSOLE']:

                for i in row:

                    self.pcon(player, i[0] if isinstance(i, tuple) else i)

        else:

            self.tell(player, MSG['LIST RESTRICTED'].replace('{list}', name), COLORS['SYSTEM'])

    # -------------------------------------------------------------------------
    def player_reset_CMD(self, player, cmd, args):
        '''Reset Database command function '''

        self.store_player(player, reset=True)

    # -------------------------------------------------------------------------
    def reset_database_CMD(self, player, cmd, args):
        '''Reset Database command function '''

        if player.IsAdmin():

            self.reset_data()

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

                if args[0] == 'save':

                    self.save_data()

                    self.tell(player, MSG['DATA SAVED'], COLORS['SYSTEM'])

                elif args[0] == 'reset' and PLUGIN['ENABLE RESET DATABASE']:

                    self.reset_data()

                    if PLUGIN['ANNOUNCE DATABASE RESET']:
                        
                        self.say(MSG['DATA RESET'], COLORS['SYSTEM'])

                    else:

                        self.tell(player, MSG['DATA RESET'], COLORS['SYSTEM'])

                elif args[0] == 'del':

                    players = [i for i in BasePlayer.sleepingPlayerList] + [i for i in BasePlayer.activePlayerList]
                    players = [i for i in players if args[1].lower() in i.displayName.lower()]

                    if not players:

                        self.tell(player, MSG['NO PLAYERS FOUND'], COLORS['SYSTEM'])

                    elif len(players) > 1:

                        self.tell(player, MSG['MULTI PLAYERS FOUND'], COLORS['SYSTEM'])

                    else:

                        self.store_player(players[0], True)

                        self.tell(player, MSG['ADMIN TELL RESET'].replace('{player}', '<lime>%s<end>' % players[0].displayName), COLORS['SYSTEM'])

        else:

            self.tell(player, '<orange><size=18>Rank-ME</size> <grey>v%s<end><end>' % self.Version, f=False)
            self.tell(player, self.Description, f=False)
            self.tell(player, 'Plugin developed by <#9810FF>SkinN<end>, powered by <orange>Oxide 2<end>.', profile='76561197999302614', f=False)

    # -------------------------------------------------------------------------
    # - PLUGIN FUNCTIONS / HOOKS
    def top_advert(self):
        ''' Function to send an advert of the Top X of a list '''

        keys = self.keys.keys()

        # Choose a random key
        key = keys[random.Range(0, len(keys))]

        while key in self.keys and key in PLUGIN['RESTRICTED TO PLAYERS']:

            key = keys[random.Range(0, len(keys))]

        # Sort players list
        lis = self._sort(key)[:3]

        # Join players to the line:
        msg = '<white>,<end> '.join(["%s (<white>%s<end>)" % (self.db[i]['name'], self.db[i][key] if key in self.db[i] else self.keys[key]) for i in lis])

        # Send message
        self.say(MSG['TOP3 ADVERT'].replace('{top}', msg).replace('{list}', LISTS[key.upper()]))

    # -------------------------------------------------------------------------
    def store_player(self, player, reset=False):
        '''Initiates the player data '''

        uid = self.playerid(player)

        # Is Player not a NPC?
        if len(uid) == 17:

            # Reset Player Data
            if reset and uid in self.db:

                del self.db[uid]

                if not player.IsSleeping():

                    self.tell(player, MSG['PLAYER DATA RESET'], COLORS['SYSTEM'])

            # Player Has Data?
            if uid not in self.db:

                self.db[uid] = {}

                for i in self.keys:

                    self.db[uid][i] = self.keys[i]

            else:

                # Update New Keys
                for k in self.keys:

                    if k not in self.db[uid]:

                        self.db[uid][k] = self.keys[k]

                if 'distance' in self.db[uid]:

                    self.db[uid]['pvpdistance'] = float(self.db[uid]['distance'])

                    del self.db[uid]['distance']

            # Update player info
            self.db[uid]['name'] = player.displayName
            self.db[uid]['admin'] = bool(player.net.connection.authLevel)

    # -------------------------------------------------------------------------
    def playerid(self, player):
        '''Function to return the player UID '''

        return rust.UserIDFromPlayer(player)

    # -------------------------------------------------------------------------
    def get_valid_list(self, key):
        '''Function to get a valid list of players of a key list'''

        val = {}

        for uid in self.db:

            ply = self.db[uid]
            adm = ply['admin'] if 'admin' in ply else False

            if not adm or adm and key not in PLUGIN['RESTRICTED TO ADMINS']:

                val[uid] = ply[key] if key in ply else self.keys[key]

        return val

    # -------------------------------------------------------------------------
    def _sort(self, lis):

        m = PLUGIN['LISTS MAX PLAYERS']
        v = self.get_valid_list(lis)

        # Return sorted list
        return sorted(v, key=lambda ply: v[ply], reverse=True)[:m if m and m < 21 else 10]

    # -------------------------------------------------------------------------
    def _float(self, f):
        '''Function to return a two decimal float '''

        return float('%.2f' % f)

    # -------------------------------------------------------------------------
    def _IsPlayerInZone(self, player):
        ''' Function to check whether the player is in Whitelisted zone '''

        ZoneManager = plugins.Find('ZoneManager')

        # Is Zone Manager running?
        if ZoneManager and PLUGIN['ENABLE ZONE MANAGER SUPPORT']:

            return any(player in self.zones[i] or ZoneManager.Call('IsPlayerInZone', i, player) for i in self.zones)

        return True

    # -------------------------------------------------------------------------
    def _RemovePlayerFromZones(self, player):
        '''Removes player from any of the zones cache if in any'''

        for i in self.zones:

            if player in self.zones[i]:

                self.zones[i].remove(player)

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

        self.tell(player, 'For all <orange>Rank-ME<end>\'s commands type <orange>/rankme help<end>', f=False)

    # -------------------------------------------------------------------------
    # Zone Manager API Hooks
    def OnEnterZone(self, zoneid, player):
        '''Hook called when the player enters a Zone Manager zone'''

        # Add player to zones cache
        if zoneid in self.zones and player not in self.zones[zoneid]:

            self.zones[zoneid].append(player)

        if PLUGIN['ENABLE ZONE MANAGER SUPPORT']:

            if zoneid in self.zones and PLUGIN['INFORM PLAYER ENTER/EXIT ZONE']:

                self.tell(player, MSG['ON ZONE ENTER'])

    # -------------------------------------------------------------------------
    def OnExitZone(self, zoneid, player):
        '''Hook called when the player leaves a Zone Manager zone'''

        # Remove player from zones cache
        if zoneid in self.zones and player in self.zones[zoneid]:

            self.zones[zoneid].remove(player)

        if PLUGIN['ENABLE ZONE MANAGER SUPPORT']:

            if zoneid in self.zones and PLUGIN['INFORM PLAYER ENTER/EXIT ZONE']:

                self.tell(player, MSG['ON ZONE EXIT'])

