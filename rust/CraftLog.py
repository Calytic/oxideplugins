import ConVar.Server as sv

default_cfg = {
	"whitelist": ()
}

class CraftLog:
	def __init__(self):
		self.Title = "CraftLog"
		self.Description = "Logs crafting."
		self.Author = "sqroot"
		self.Version = V(1, 2, 0)
		self.ResourceId = 1694

	def LoadDefaultConfig(self):
		self.Config.Clear()
		self.Config = default_cfg
		self.SaveConfig()

	def log(self, task, msg):
		uid = task.taskUID
		player = task.owner
		playerID = player.userID
		playerName = player.displayName
		n = task.amount
		item_name = task.blueprint.targetItem.displayName.english
		wl = self.Config["whitelist"]
		if wl and item_name not in wl:
			return
		sv.Log("oxide/logs/crafted.txt", "[%s] %s (%s) %s: %dx %s" % (uid, playerName, playerID, msg, n, item_name))

	def OnItemCraft(self, task, item):
		self.log(task, "started")

	def OnItemCraftCancelled(self, task):
		self.log(task, "cancelled at")