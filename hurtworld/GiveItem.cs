using System.Collections.Generic;
using System.Linq;
using System;
using uLink;

namespace Oxide.Plugins
{
    [Info("GiveItem", "Noviets", "1.0.5")]
    [Description("Gives an item to anyone or everyone")]

    class GiveItem : HurtworldPlugin
    {		

		void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"nopermission","GiveItem: You do not have Permission to use this command."},
                {"playernotfound","GiveItem: Unable to find player: {Player}"},
				{"invalid","GiveItem: Incorrect Usage. (/giveitem Player ItemID Amount)"},
				{"invaliditem","GiveItem: That item does not exist. Please check the ItemID"},
				{"success","GiveItem: {Amount} x {ItemName} given to {Player}"}
            };
			
			lang.RegisterMessages(messages, this);
        }
		void Loaded()
        {
            permission.RegisterPermission("giveitem.use", this);
			LoadDefaultMessages();
		}

		string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
		
		private PlayerSession GetSession(string source) 
		{
			try
			{
				foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> p in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
				{
					if(p.Value != null)
					{
						if(p.Value.IsLoaded) {
							if (source.ToLower() == p.Value.Name.ToLower())
								return p.Value;
						}
					}
				}
				foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> pair in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
				{
					if(pair.Value != null)
					{
						if(pair.Value.IsLoaded)
						{
							if (pair.Value.Name.ToLower().Contains(source.ToLower()))
								return pair.Value;
						}
					}
				}
			}
			catch {
				Puts("An error occurred fetching session");
				return null; 
			}
			return null;
		}

		[ChatCommand("giveitem")]
        void GiveItemCommand(PlayerSession session, string command, string[] args)
        {
			if(permission.UserHasPermission(session.SteamId.ToString(), "giveitem.use"))
			{
				if(args.Length == 3)
				{
					PlayerSession target = GetSession(args[0]);
					if(target != null)
					{
						int ItemID = Convert.ToInt32(args[1]);
						int Amount = Convert.ToInt32(args[2]);
						var ItemMgr = Singleton<GlobalItemManager>.Instance;
						var ItemName = GlobalItemManager.Instance.GetItem(ItemID);
						if(ItemName != null){
							var CleanName = ItemName.GetNameKey().Replace("Items/","").Replace("AmmoType/","").Replace("Machines/","");
							ItemMgr.GiveItem(target.Player, ItemMgr.GetItem(ItemID), Amount);
							hurt.SendChatMessage(session, (Msg("success").Replace("{Amount}",Amount.ToString()).Replace("{ItemName}",CleanName).Replace("{Player}", target.Name)));
						}
						else
							hurt.SendChatMessage(session, Msg("invaliditem"));
					}
					else
						hurt.SendChatMessage(session, Msg("playernotfound").Replace("{Player}",args[0]));
				}
				else
					hurt.SendChatMessage(session, Msg("invalid"));
			}
			else 
				hurt.SendChatMessage(session, Msg("nopermission"));
		}
		[ChatCommand("giveall")]
        void GiveAllCommand(PlayerSession session, string command, string[] args)
        {
			if(permission.UserHasPermission(session.SteamId.ToString(), "giveitem.use"))
			{
				if(args.Length == 2)
				{
					int ItemID = Convert.ToInt32(args[0]);
					int Amount = Convert.ToInt32(args[1]);
					foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> pair in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
					{
						var ItemMgr = Singleton<GlobalItemManager>.Instance;
						var ItemName = GlobalItemManager.Instance.GetItem(ItemID);
						if(ItemName != null){
							var CleanName = ItemName.GetNameKey().Replace("Items/","").Replace("AmmoType/","").Replace("Machines/","");
							ItemMgr.GiveItem(pair.Value.Player, ItemMgr.GetItem(ItemID), Amount);
							hurt.SendChatMessage(session, (Msg("success").Replace("{Amount}",Amount.ToString()).Replace("{ItemName}",CleanName).Replace("{Player}", pair.Value.Name)));
						}
						else
							hurt.SendChatMessage(session, Msg("invaliditem"));
					}
				}
				else
					hurt.SendChatMessage(session, Msg("invalid"));
			}
			else 
				hurt.SendChatMessage(session, Msg("nopermission"));
		}
		[ChatCommand("itemid")]
        void itemIDCommand(PlayerSession session, string command, string[] args)
        {
			if(permission.UserHasPermission(session.SteamId.ToString(), "giveitem.use"))
			{
				var ItemMgr = Singleton<GlobalItemManager>.Instance;
				var i=0;
				while(i < 310)
				{
					var ItemName = GlobalItemManager.Instance.GetItem(i);
					if (ItemName != null)
					{
						var CleanName = ItemName.GetNameKey().Replace("Items/","").Replace("AmmoType/","").Replace("Machines/","");
						if (CleanName.ToLower().Contains(args[0].ToLower()))
							hurt.SendChatMessage(session, "<color=orange>"+CleanName+"</color>  ID:<color=yellow> "+i+"</color>");
					}
					i++;
				}
			}
		}
	}
}