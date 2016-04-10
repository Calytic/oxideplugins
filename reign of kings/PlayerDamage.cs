using System;
using System.Collections.Generic;
using System.Linq;

using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.Common;
 
namespace Oxide.Plugins
{
    [Info("Player Damage", "PaiN", 0.1, ResourceId = 0)] 
    [Description("Turn ON/OFF the player damage.")]
    class PlayerDamage : ReignOfKingsPlugin 
    {
		private bool Changed;
		private bool pdmg;
		
		object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null) 
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;  
        } 
		protected override void LoadDefaultConfig()
		{
			Puts("Creating a new configuration file!");
			Config.Clear();
			LoadVariables();
		}
		void Loaded()
		{
			LoadVariables();
			if(!permission.PermissionExists("canpdmg")) permission.RegisterPermission("canpdmg", this);
		}
		
		void LoadVariables()
		{
			pdmg = Convert.ToBoolean(GetConfig("Settings", "PDMG-OFF", false));
			if (Changed)
			{
				SaveConfig();
				Changed = false;
			
			}
		}
		
		
		void OnEntityHealthChange(EntityDamageEvent damageEvent)
        {
            if(damageEvent.Damage.Amount > 0 && damageEvent.Entity.IsPlayer && damageEvent.Damage.DamageSource.IsPlayer && damageEvent.Entity != damageEvent.Damage.DamageSource && pdmg == false) 
            {
                damageEvent.Cancel();
                damageEvent.Damage.Amount = 0f; 
                SendReply(damageEvent.Damage.DamageSource.Owner, "[FF0000]Player-Damage is currently DISABLED.[FFFFFF]");
            }
        }
		
		[ChatCommand("pdmg")]
		void cmdKos(Player player, string cmd, string[] args)
		{
			if(!permission.UserHasPermission(player.Id.ToString(), "canpdmg"))
			{
				SendReply(player, "You do not have permission to use this command!");
				return;
			}
			if(pdmg == false)
			{
				pdmg = true;
				SendReply(player, "You have enabled Player-Damage");
			}
			else if(pdmg == true)
			{
				pdmg = false;
				SendReply(player, "You have disabled Player-Damage");
			}
		}
	} 
}