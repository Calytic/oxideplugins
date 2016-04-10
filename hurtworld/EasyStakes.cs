//Reference: UnityEngine.UI
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("EasyStakes", "Noviets", "1.0.3", ResourceId = 1697)]
    [Description("Fill up every authorized stake with amber.")]

    class EasyStakes : HurtworldPlugin
    {		

		void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"nopermission","EasyStake: You don't have Permission to do this!"},
				{"stakes","EasyStakes: You're Authorized on {Count} stakes. You need {Amount} Amber to fill them all."},
				{"stakesfilled","EasyStakes: Successfully filled {count} Ownership Stakes with {amount} Amber"},
				{"noamber","EasyStakes: You've ran out of Amber!"}
            };
			
			lang.RegisterMessages(messages, this);
        }
		void Loaded()
        {
            permission.RegisterPermission("easystakes.use", this);
			LoadDefaultMessages();
		}

		string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

		[ChatCommand("stakes")]
        void stakeCommand(PlayerSession session, string command, string[] args)
        {
			int auth=0;
			int AmberNeeded = 0;
			if(permission.UserHasPermission(session.SteamId.ToString(), "easystakes.use"))
			{
				if(args.Length == 0)
				{
					foreach (OwnershipStakeServer stake in Resources.FindObjectsOfTypeAll<OwnershipStakeServer>())
					{
						if(stake.AuthorizedPlayers.Contains(session.Identity))
						{
							Inventory inv = stake.GetComponent<Inventory>() as Inventory;
							ItemInstance slot = inv.GetSlot(0);
							int Needs = 5 - slot.StackSize;
							AmberNeeded += Needs;
							auth++;
						}
					}
					hurt.SendChatMessage(session, Msg("stakes",session.SteamId.ToString()).Replace("{Count}",auth.ToString()).Replace("{Amount}",AmberNeeded.ToString()));
				}
				if(args.Length == 1)
				{
					if(args[0].ToLower() == "fill")
					{
						foreach (OwnershipStakeServer stake in Resources.FindObjectsOfTypeAll<OwnershipStakeServer>())
						{
							if(stake.AuthorizedPlayers.Contains(session.Identity))
							{
								Inventory pinv = session.WorldPlayerEntity.GetComponent<Inventory>();
								Inventory inv = stake.GetComponent<Inventory>() as Inventory;
								ItemInstance slot = inv.GetSlot(0);
								int Needs = 5 - slot.StackSize;
								AmberNeeded += Needs;
								auth++;
								if((bool)TakeAmber(session, Needs))
								{
									slot.StackSize = 5;
								}
								else
								{
									hurt.SendChatMessage(session, Msg("noamber",session.SteamId.ToString()));
									return;
								}
							}
						}
						hurt.SendChatMessage(session, Msg("stakesfilled",session.SteamId.ToString()).Replace("{count}",auth.ToString()).Replace("{amount}",AmberNeeded.ToString()));
					}
				}
			}
			else 
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		object TakeAmber(PlayerSession session, int amount)
		{
			PlayerInventory pinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
			var slot = pinv.Items;
			var ItemMgr = Singleton<GlobalItemManager>.Instance;
			for (var i = 0; i < pinv.Items.Length; i++)
			{
				if (slot[i] != null)
				{
					if (slot[i].Item.ItemId == 87) 
					{
						if(slot[i].StackSize > amount)
						{
							slot[i].StackSize = slot[i].StackSize - amount;
							ItemMgr.GiveItem(session.Player, ItemMgr.GetItem(87), 0);
							return true;
						}
						else
							return false;
					}
					if(i == pinv.Items.Length-1)
					{
						if (slot[i].Item.ItemId != 87)
							return false;
					}
				}
			}
			return false;
		}
	}
}