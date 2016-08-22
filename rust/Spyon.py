import ConVar.Server as sv
from System import DateTime
import BasePlayer

default_cfg = {
	"authlevel": 1,
	"public": False,
	"exposure": 0,
	"logdaily": False,
	"cmds": (
		"global.setinfo",
		"global.kick",
		"global.kickall",
		"global.unban",
		"global.mutevoice",
		"global.unmutevoice",
		"global.mutechat",
		"global.unmutechat",
		"global.spectate",
		"global.teleport",
		"global.teleport2me",
		"global.teleportany",
		"inventory.give",
		"inventory.giveall",
		"inventory.givebpall",
		"inventory.giveto",
		"inventory.givearm",
		"inventory.giveid",
		"inventory.givebp",
		"heli.drop",
		"heli.calltome",
		"heli.call",
		"heli.strafe"
	),
	"chatcmds": (
		"/remove admin",
		"/remove all"
	)
}

only_check_type = [(list, tuple), (int, float), str, bool]
def mask_default_cfg(dcfg, cfg):
	# causes changes in cfg to affect dcfg,
	# which is fine since we don't care about the default cfg 
	# after masking
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

class Spyon:
	def __init__(self):
		self.Title = "Spyon"
		self.Description = "Logs command usage."
		self.Author = "sqroot"
		self.Version = V(1, 2, 1)
		self.ResourceId = 1685

	def send_msg(self, player, msg):
		rust.SendChatMessage(player, "Spyon", msg)

	def has_auth(self, player, authLevel):
		return player.GetComponent("BaseNetworkable").net.connection.authLevel >= authLevel

	def log(self, msg):
		if self.Config["public"]:
			permitted = (p for p in BasePlayer.activePlayerList if self.has_auth(p, self.Config["exposure"]))
			for p in permitted:
				self.send_msg(p, msg)
		log_name = "cmds_%s" % DateTime.Now.ToString("dd-MM-yyyy") if self.Config["logdaily"] else "cmds"
		sv.Log("oxide/logs/%s.txt" % log_name, msg)

	def log_cmd(self, player, cmd):
		self.log("%s: %s" % (player, cmd))

	def log_reason(self, player, reason):
		self.log("[REASON] %s: %s" % (player, reason))

	def is_rcon(self, arg):
		return not arg.connection and arg.isAdmin

	def is_allowed(self, player):
		return self.has_auth(player, self.Config["authlevel"])

	def is_rcon_or_allowed(self, arg):
		return self.is_rcon(arg) or arg.connection and arg.connection.player and self.is_allowed(arg.connection.player)

	def name(self, arg):
		# we assume that arg is either called by a player or rcon, ie is_rcon_or_allowed was called
		# check if user is ingame or connected via RCON
		if self.is_rcon(arg):
			return "RCON"
		return arg.connection.player.displayName

	def save_cfg(self, cfg):
		self.Config = cfg
		self.SaveConfig()

	def LoadDefaultConfig(self):
		self.save_cfg(default_cfg)

	def OnServerCommand(self, arg):
		if not (arg.cmd and arg.cmd.namefull and self.is_rcon_or_allowed(arg)):
			return
		p = self.name(arg)
		cmd = arg.cmd.namefull
		args = arg.ArgsStr
		if not self.is_rcon(arg) and cmd == "chat.say":
			# command is a chat command
			args = args.strip("\"").rstrip("\"")
			if args == "":
				return
			if args[0] != "/":
				return
			# makes sure no partial commands are matched
			# ex: "/tp" -> "/tp ", "/tpr" -> "/tpr ", "/tpr " doesn't start with "/tp "
			args = args + " "
			if not any(args.startswith(c + " ") for c in self.Config["chatcmds"]):
				return
			self.log_cmd(p, args)
		elif cmd in self.Config["cmds"]:
			# command is a regular command
			self.log_cmd(p, "%s %s" % (cmd, args))
		
	def reason_chat_hook(self, player, cmd, args):
		if not self.is_allowed(player):
			self.send_msg(player, "You do not have the required privilige to use this command.")
			return
		self.log_reason(player.displayName, " ".join(args))
		self.send_msg(player, "Reason logged.")

	def reason_con_hook(self, arg):
		if not self.is_rcon_or_allowed(arg):
			return
		self.log_reason(self.name(arg), arg.ArgsStr)
		print("Reason logged.")

	def Init(self):
		command.AddChatCommand("reason", self.Plugin, "reason_chat_hook")
		command.AddConsoleCommand("spyon.reason", self.Plugin, "reason_con_hook")
		masked = mask_default_cfg(default_cfg, self.Config)
		self.save_cfg(masked)