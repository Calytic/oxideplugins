import ConVar.Server as sv

default_cfg = {
	"blacklist": (),
	"whitelist": ()
}

only_check_type = [(list, tuple), (int, float), str, bool]
def mask_default_cfg(dcfg, cfg):
	if isinstance(dcfg, dict):
		if not isinstance(cfg, dict):
			return dcfg
		for k, v in dcfg.items():
			if k not in cfg:
				cfg[k] = v
				continue
			cfg[k] = mask_default_cfg(v, cfg[k])
		return cfg
	for t in only_check_type:
		if isinstance(dcfg, t):
			return cfg if isinstance(cfg, t) else dcfg
	raise TypeError("did not expect type %s in default cfg" % type(dcfg))

class CraftLog:
	def __init__(self):
		self.Title = "CraftLog"
		self.Description = "Logs crafting."
		self.Author = "sqroot"
		self.Version = V(1, 4, 1)
		self.ResourceId = 1694

	def save_cfg(self, cfg):
		self.Config = cfg
		self.SaveConfig()

	def LoadDefaultConfig(self):
		self.save_cfg(default_cfg)

	def log(self, task, msg):
		item = task.blueprint.targetItem
		full_name = item.displayName.english
		item_names = [item.itemid, item.shortname, full_name]
		wl = self.Config["whitelist"]
		if wl and not any(n in wl for n in item_names):
			return
		bl = self.Config["blacklist"]
		if any(n in bl for n in item_names):
			return
		o = task.owner
		sv.Log("oxide/logs/crafted.txt", "[%s] %s (%s) %s: %dx %s" % (task.taskUID, o.displayName, o.userID, msg, task.amount, full_name))

	def OnItemCraft(self, task, player, item):
		self.log(task, "started")

	def OnItemCraftCancelled(self, task):
		self.log(task, "cancelled at")

	def Init(self):
		masked = mask_default_cfg(default_cfg, self.Config)
		self.save_cfg(masked)