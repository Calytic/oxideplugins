import UnityEngine.Physics
import RaycastHitEx

use_perm = "built.use"

class Built:
	def __init__(self):
		self.Title = "Built"
		self.Description = "Provides entity owner information."
		self.Author = "sqroot"
		self.Version = V(1, 3, 2)
		self.ResourceId = 1702

	def send_msg(self, player, msg):
		rust.SendChatMessage(player, "Built", msg)

	def built_hook(self, player, cmd, args):
		if not permission.UserHasPermission(str(player.userID), use_perm):
			self.send_msg(player, "You do not have the permission to use this command.")
			return
		hits = UnityEngine.Physics.RaycastAll(player.eyes.HeadRay(), maxDistance=100)
		ents = ((h, RaycastHitEx.GetEntity(h)) for h in hits)
		base_ents = ((h, e) for h, e in ents if e != None)
		try:
			_, closest = min(base_ents, key=lambda he: he[0].distance)
		except ValueError:
			self.send_msg(player, "No entity found.")
			return
		owner_id = closest.OwnerID
		if owner_id != 0:
			p = covalence.Players.FindPlayerById(str(owner_id))
			self.send_msg(player, "%s (%s)" % (p.Name if p else "Unknown", owner_id))
		else:
			self.send_msg(player, "No owner found.")
		try:
			authed = closest.authorizedPlayers
		except AttributeError:
			# is not authable
			return
		authed_info = "\n".join("%s (%s)" % (player.username, player.userid) for player in authed) if authed else "None"
		self.send_msg(player, "Authorized:\n%s" % authed_info)

	def Init(self):
		command.AddChatCommand("built", self.Plugin, "built_hook")
		if not permission.PermissionExists(use_perm):
			permission.RegisterPermission(use_perm, self.Plugin)
			permission.GrantGroupPermission("admin", use_perm, self.Plugin)