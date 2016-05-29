import ServerUsers
import BasePlayer

default_cfg = {
	"host": "localhost",
	"port": 3306,
	"database": "bansync",
	"user": "root",
	"password": "CHANGEME"
}

banlist = lambda: {ban.steamid:ban for ban in ServerUsers.GetAll(ServerUsers.UserGroup.Banned)}

def bandiff(old, new):
	old_ids = set(old)
	new_ids = set(new)
	banned = {new[ban] for ban in new_ids - old_ids}
	unbanned = {old[unban] for unban in old_ids - new_ids}
	return banned, unbanned

def echo(msg):
	print("[BanSync] %s" % msg)

class BannedUser:
	def __init__(self, steamid, username, notes):
		self.steamid = steamid
		self.username = username
		self.notes = notes

class BanSync:
	def __init__(self):
		self.Title = "BanSync"
		self.Description = "Syncs bans across servers."
		self.Author = "sqroot"
		self.Version = V(1, 0, 2)
		self.ResourceId = 1897

	def LoadDefaultConfig(self):
		self.Config = default_cfg
		self.SaveConfig()

	def save_current_bans(self):
		ServerUsers.Save()
		table = data.GetData("BanSync")
		# cast to tuple/str because at this point Oxide can't handle lists/longs
		table["old_bans"] = tuple((str(ban.steamid), ban.username, ban.notes) for ban in ServerUsers.GetAll(ServerUsers.UserGroup.Banned))
		data.SaveData("BanSync")

	def pull_bans(self, rows_affected):
		if rows_affected > 0:
			self.save_current_bans()
		def update_bans(rows):
			if rows is not None:
				banned, unbanned = bandiff(banlist(), {r["steamid"]:r for r in rows})
				for b in banned:
					steamid = b["steamid"]
					name = b["name"]
					reason = b["reason"]
					ServerUsers.Set(steamid, ServerUsers.UserGroup.Banned, name, reason)
					self.old_bans[steamid] = BannedUser(steamid, name, reason)
					player = BasePlayer.FindByID(steamid)
					if player:
						player.Kick("Banned: %s" % reason)
					echo("Banned user '%s' with ID %d and reason '%s'." % (name, steamid, reason))
				for ub in unbanned:
					steamid = ub.steamid
					ServerUsers.Remove(steamid)
					self.old_bans.pop(steamid, None)
					echo("Unbanned user '%s' with ID %d." % (ub.username, steamid))
				if banned or unbanned:
					self.save_current_bans()
			timer.Once(20, self.push_bans, self.Plugin)
		mysql.Query(mysql.NewSql().Append("SELECT * FROM bans;"), self.db, update_bans)

	def push_bans(self):
		bans = banlist()
		banned, unbanned = bandiff(self.old_bans, bans)
		self.old_bans = bans
		q = mysql.NewSql()
		if banned:
			query = "REPLACE INTO bans VALUES %s;" % ",".join("(@%d, @%d, @%d)" % (i, i+1, i+2) for i in range(0, 3*len(banned), 3))
			q.Append(query, *[attrib for b in banned for attrib in (b.steamid, b.username, b.notes)])
		if unbanned:
			query = "DELETE FROM bans WHERE steamid IN (%s);" % ",".join(str(b.steamid) for b in unbanned)
			q.Append(query)
		if banned or unbanned:
			mysql.ExecuteNonQuery(q, self.db, self.pull_bans)
		else:
			self.pull_bans(0)

	def OnServerInitialized(self):
		table = data.GetData("BanSync")
		self.old_bans = {long(steamid):BannedUser(long(steamid), name, reason) for steamid, name, reason in table.get("old_bans", {})}
		# the update cycle works as follows:
		# 1) all differences not caused by pull_bans calls since the last push_bans call are applied to the database via push_bans (bans are added, unbans are removed)
		#    (if this is the first time we're calling push_bans, all bans added since the last time this plugin was used are applied to the database)
		# 2) remote bans are loaded from the database via pull_bans and differences applied 
		#    (local users that aren't in the db are unbanned, remote users that aren't local are banned)
		# 3) wait for 20s
		# 4) restart at #1
		self.db = mysql.OpenDb(self.Config["host"], self.Config["port"], self.Config["database"], self.Config["user"], self.Config["password"], self.Plugin)
		query = "CREATE TABLE IF NOT EXISTS bans (steamid BIGINT UNSIGNED NOT NULL PRIMARY KEY, name TEXT NOT NULL, reason TEXT NOT NULL);"
		mysql.ExecuteNonQuery(mysql.NewSql().Append(query), self.db, lambda _: self.push_bans())

	def Unload(self):
		mysql.CloseDb(self.db)