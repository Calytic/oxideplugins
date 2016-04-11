using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Friends", "Schwarz", "1.0.3")]
    class Friends : RustLegacyPlugin
    {
		object cachedValue;
		string hurted = "";
		string hurtedId = "";
		string killerId = "";
		string UNKNOWN = "UNKNOWN";
		string killer = "";

        [PluginReference]
        Plugin FriendsDatabase;
		
		object ModifyDamage(TakeDamage takedamage, DamageEvent damage)
		{
			killerId = damage.attacker.client?.netUser.userID.ToString() ?? UNKNOWN;
			killer = damage.attacker.client?.netUser.displayName ?? UNKNOWN;
			NetUser killeruser =  damage.attacker.client?.netUser ?? null;
			NetUser hurteduser = damage.victim.client?.netUser ?? null;
			hurted = damage.victim.client?.netUser.displayName ?? UNKNOWN;
			hurtedId = damage.victim.client?.netUser.userID.ToString() ?? UNKNOWN;
			cachedValue = Interface.CallHook("isFriend", killerId, hurtedId);
			if (cachedValue is bool && (bool)cachedValue)
			{
				rust.Notice(killeruser, string.Format("Stop shooting ! It's your friend: {0}", hurted));
				return CancelDamage(damage);
			}
			return null;
		}
		
		object CancelDamage(DamageEvent damage)
        {
            damage.amount = 0f;
            damage.status = LifeStatus.IsAlive;
            return damage;
        }

		void SendHelpText(NetUser netuser)
        {
            SendReply(netuser, "/addfriend to add friends to avoid killing them.");
			SendReply(netuser, "/unfriend to remove friends.");
        }	 
    }
}