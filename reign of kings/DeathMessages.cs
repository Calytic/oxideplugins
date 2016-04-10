using System;
using System.Collections.Generic;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.Engine.Core.Cache;
using System.Linq;
using Oxide.Core;
using CodeHatch.Damaging;
 
namespace Oxide.Plugins
{
    [Info("Death Messages", "PaiN", 0.4, ResourceId = 1042)] 
    [Description("Displays a server wide message showing who killed who")]
    class DeathMessages : ReignOfKingsPlugin 
    { 
		private List<string>DamageT = new List<string>{"Slash","Bash","Pierce","Projectile"};
		
		void OnEntityDeath(EntityDeathEvent e)
		{
			if(e.Entity == null || e.KillingDamage.DamageSource == null || e == null || e.Entity.IsPlayer == false || e.KillingDamage.DamageSource.Owner.IsServer || e.Entity.Owner.IsServer) return;
			foreach(string dmgt in DamageT)
			if(e.KillingDamage.DamageTypes.ToString() == dmgt)
			PrintToChat($"{e.KillingDamage.DamageSource.Owner.DisplayName} killed {e.Entity.Owner.DisplayName}");
		}
		
	}
}