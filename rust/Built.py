import UnityEngine.Physics
import RaycastHitEx
import BasePlayer

use_perm = "built.use"

class Built:
	def __init__(self):
		self.Title = "Built"
		self.Description = "Provides entity owner information."
		self.Author = "sqroot"
		self.Version = V(1, 1, 1)
		self.ResourceId = 1702
		self.DeadPlayersList = plugins.Find("DeadPlayersList")

	def send_msg(self, player, msg):
		rust.SendChatMessage(player, "Built", msg)

	def join_authed_players(self, authed):
		if len(authed) == 0:
			return "None"
		return "\n".join("%s (%s)" % (player.username, player.userid) for player in authed)

	def send_auth_msg(self, player, authed):
		self.send_msg(player, "Authorized:\n%s" % self.join_authed_players(authed))

	def is_allowed(self, player):
		return permission.UserHasPermission(str(player.userID), use_perm)

	def looking_at(self, player):
		hits = UnityEngine.Physics.RaycastAll(player.eyes.HeadRay(), maxDistance=100)
		ents = ((h, RaycastHitEx.GetEntity(h)) for h in hits)
		base_ents = ((h, e) for h, e in ents if e != None)
		_, closest = min(base_ents, key=lambda he: he[0].distance)
		return closest

	def player_name(self, uid):
		player = BasePlayer.FindByID(uid)
		if player:
			return player.displayName
		player = BasePlayer.FindSleeping(uid)
		if player:
			return player.displayName
		if self.DeadPlayersList and self.DeadPlayersList.IsLoaded: 
			name = self.DeadPlayersList.Call("GetPlayerName", uid)
			if name:
				return name
		return "Unknown"

	def built_hook(self, player, cmd, args):
		if not self.is_allowed(player):
			self.send_msg(player, "You do not have the permission to use this command.")
			return
		try:
			ent = self.looking_at(player)
		except ValueError:
			self.send_msg(player, "No entity found.")
			return
		owner_id = ent.OwnerID
		if owner_id != 0:
			self.send_msg(player, "%s (%s)" % (self.player_name(owner_id), owner_id))
		else:
			self.send_msg(player, "No owner found.")
		try:
			authed = ent.authorizedPlayers
		except AttributeError:
			# is not authable
			return
		self.send_auth_msg(player, authed)

	def Init(self):
		command.AddChatCommand("built", self.Plugin, "built_hook")
		if not permission.PermissionExists(use_perm):
			permission.RegisterPermission(use_perm, self.Plugin)
			permission.GrantGroupPermission("admin", use_perm, self.Plugin)