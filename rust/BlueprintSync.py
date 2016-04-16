from System import Action, String, Object
from System.Collections.Generic import List, Dictionary
import ServerMgr
import ItemManager

default_cfg = {
	"host": "localhost",
	"port": 3306,
	"user": "root",
	"password": "CHANGEME"
}

class BlueprintSync:
	def __init__(self):
		self.Title = "BlueprintSync"
		self.Description = "Syncs player blueprints across multiple servers."
		self.Author = "sqroot"
		self.Version = V(1, 0, 2)
		self.ResourceId = 1807

	def LoadDefaultConfig(self):
		self.Config.Clear()
		self.Config = default_cfg
		self.SaveConfig()

	def create_bp_table(self, uid):
		query = mysql.NewSql()
		query.Append("CREATE TABLE IF NOT EXISTS player_%d (bpid INT PRIMARY KEY);" % uid)
		mysql.ExecuteNonQuery(query, self.db)

	def OnPlayerInit(self, player):
		if not self.db:
			print("Cannot read blueprints from database because database couldn't be opened.")
			return
		uid = player.userID
		self.create_bp_table(uid)
		def learn_bps(rows):
			if not rows:
				return
			for r in rows:
				player.blueprints.Learn(ItemManager.FindItemDefinition(r["bpid"]))
		query = mysql.NewSql()
		query.Append("SELECT * FROM player_%d;" % uid)
		mysql.Query(query, self.db, Action[List[Dictionary[String, Object]]](learn_bps))

	def OnPlayerDisconnected(self, player, reason):
		if not self.db:
			print("Cannot write blueprints to database because database couldn't be opened.")
			return
		uid = player.userID
		self.create_bp_table(uid)
		bps = ServerMgr.Instance.persistance.GetPlayerInfo(uid).blueprints.complete
		if len(bps) == 0:
			return
		query = mysql.NewSql()
		msg = "REPLACE INTO player_%d VALUES %s;" % (uid, ",".join("(%d)" % bp for bp in bps))
		query.Append(msg)
		mysql.ExecuteNonQuery(query, self.db)

	def Init(self):
		self.db = None
		self.db = mysql.OpenDb(self.Config["host"], self.Config["port"], "bpsync", self.Config["user"], self.Config["password"], self.Plugin)

	def Unload(self):
		if not self.db:
			print("Cannot close database because database couldn't be opened.")
			return
		mysql.CloseDb(self.db)