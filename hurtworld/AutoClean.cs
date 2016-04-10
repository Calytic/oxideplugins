using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("AutoClean", "Noviets", "1.0.0")]
    [Description("Provides automatic cleaning of objects outside of claimed areas")]

    class AutoClean : HurtworldPlugin
    {
		Dictionary<uLink.NetworkViewID, int> ObjectList = new Dictionary<uLink.NetworkViewID, int>();
		void SaveObjects() => Interface.GetMod().DataFileSystem.WriteObject("AutoClean/ObjectList", ObjectList);
		OwnershipStakeServer stake;
		int destroyed = 0;
		int hasstake = 0;
		int nostake = 0;
		int num = 0;

		protected override void LoadDefaultConfig()
        {
			if(Config["UpdateIntervalSeconds"] == null) Config.Set("UpdateIntervalSeconds", 7200);
			if(Config["IntervalsBeforeCleaning"] == null) Config.Set("IntervalsBeforeCleaning", 24);
			if(Config["ShowConsoleMessages"] == null) Config.Set("ShowConsoleMessages", true);
            SaveConfig();
		}
		
		void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"nopermission","AutoClean: You dont have Permission to do this!"}
            };
			
			lang.RegisterMessages(messages, this);
        }
		string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
		
		void Loaded()
        {
			permission.RegisterPermission("autoclean.admin", this);
			LoadDefaultMessages();
			LoadDefaultConfig();
		}
		
		void OnServerInitialized(){timer.Repeat(Convert.ToSingle(Config["UpdateIntervalSeconds"]), 0, () => { DoClean(); });}
		
		[ChatCommand("clean")]
        void manualClean(PlayerSession session)
        {
			if(permission.UserHasPermission(session.SteamId.ToString(),"autoclean.admin"))
			{
				DoClean();
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		
        void DoClean()
        {
			hasstake = 0;
			nostake = 0;
			destroyed = 0;
			num = 0;
			foreach(GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
			{
				if(obj.name.Contains("Constructed") || obj.name.Contains("StructureManager"))
				{
					var thecell = ConstructionManager.GetOwnershipCell(obj.transform.position);
					if(thecell != null)
					{
						var nView = uLink.NetworkView.Get(obj);
						if(nView != null && !nView.viewID.ToString().Contains("Unassigned"))
						{
							ConstructionManager.Instance.OwnershipCells.TryGetValue(ConstructionManager.GetOwnershipCell(obj.transform.position), out stake);
							if(stake == null)
							{
								nostake++;
								if(ObjectList.TryGetValue(nView.viewID, out num))
								{
									if(num == Convert.ToInt32(Config["IntervalsBeforeCleaning"]))
									{
										try
										{
											Singleton<NetworkManager>.Instance.NetDestroy(nView);
											ObjectList.Remove(nView.viewID);
											destroyed++;
										}
										catch{}
									}
									else
										ObjectList[nView.viewID] += 1;
								}
								else
									ObjectList.Add(nView.viewID, 1);
							}
							else
							{
								hasstake++;
								if(ObjectList.TryGetValue(nView.viewID, out num))
								{
									ObjectList.Remove(nView.viewID);
								}
							}
						}
					}
				}
			}
			SaveObjects();
			if((bool)Config["ShowConsoleMessages"]){
				Puts("Has Stake: "+hasstake);
				Puts("No Stake : "+nostake);
				Puts("Destroyed: "+destroyed);
			}
		}
	}
}