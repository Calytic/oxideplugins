using System;
using System.Collections.Generic;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.Networking.Events.Players;
using CodeHatch.Engine.Core.Cache;
using System.Linq;
using Oxide.Core;
 
namespace Oxide.Plugins
{
    [Info("TopKDR", "PaiN", 0.3, ResourceId = 0)] 
    [Description(".")]
    class TopKDR : ReignOfKingsPlugin 
    { 
		
		private bool Changed;
		private bool enablescoretags;
		private object tags;
		
		void LoadVariables()   
		{
			
			enablescoretags = Convert.ToBoolean(GetConfig("Settings", "EnableScoreTags", false));
			tags = GetConfig("ScoreTags", "Tags", new Dictionary<object, object>{
				{"[Tag1]", 5},
				{"(Tag2)", 10},
				{"[Tag3]", 15},
				{"{Tag4}", 20},
				{"$Tag5$", 25}
			}
			);
			
			if (Changed)
			{
				SaveConfig();
				Changed = false;
			
			}	
		}
		
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
		
		class StoredData
		{
			public Dictionary<ulong, int> Kills = new Dictionary<ulong, int>();
			public Dictionary<ulong, int> Deaths = new Dictionary<ulong, int>();	
		} 
	
		StoredData data; 
		void Loaded()
		{		
			data = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("TopKDR_data");  
			LoadVariables();  
			LoadDefaultMessages();
			
		}
		
		readonly Dictionary<string, string> messages = new Dictionary<string, string>();
		
		void LoadDefaultMessages()
        {
            messages.Add("TopList", "Name: {0}, Kills: {1}, Deaths: {2}, Score: {3}");
            lang.RegisterMessages(messages, this);
        }
		
		void SaveData() {Interface.GetMod().DataFileSystem.WriteObject("TopKDR_data", data);}
		
		private void OnEntityDeath(EntityDeathEvent e) 
		{
			if(e.Entity == null || e.KillingDamage.DamageSource == null || e == null) return;						
				ulong victimid = e.Entity.OwnerId;
				ulong attackerid = e.KillingDamage.DamageSource.OwnerId;
				
				
				if(data.Kills.ContainsKey(attackerid)) 
				data.Kills[attackerid] = data.Kills[attackerid] + 1;
				else
				data.Kills.Add(attackerid, 1);
		 	
				if(data.Deaths.ContainsKey(victimid)) 
				data.Deaths[victimid] = data.Deaths[victimid] + 1;
				else
				data.Deaths.Add(victimid, 1);
			
				SaveData();
			
			
		}
		
		void OnPlayerChat(PlayerEvent e)
		{
			if(!enablescoretags) return;
			Player player = e.Player;
			
            player.DisplayNameFormat = $"{GetPlayerTag(player)} %name%";
		}
		
		
		[ChatCommand("top")]
		void cmdTop(Player player, string cmd, string[] args)
		{
			List<KeyValuePair<ulong, int>> list = data.Kills.OrderByDescending(pair => pair.Value).ToList();
			if(args.Length == 0)
			{
				SendReply(player, "Syntax: /top [number] || ex. /top 5, /top 10");
				return;
			}
		
			for (int i = 0; i < Convert.ToInt32(args[0]); i++)
			{
				if (list.Count < i + 1)
				break;
				int kills = 0;
				kills = list[i].Value;
				int deaths = 0;
				if(!data.Deaths.ContainsKey(list[i].Key))
				deaths = 0;
				else
				deaths = data.Deaths[list[i].Key];
				
				int score = kills-deaths;
				if(score <= 0) score = 0;
						
				SendReply(player, $"{i+1}. " +sendlang("TopList", player.Id.ToString()),Server.GetPlayerById(list[i].Key).DisplayName.ToString(), kills.ToString(), deaths.ToString(), score.ToString() );
			}
		} 
		
		int GetPlayerScore(Player player)
		{
			int kills = 0;
			if(!data.Kills.ContainsKey(player.Id))
			kills = 0 ;
			else
			kills = data.Kills[player.Id];
			int deaths = 0;
			if(!data.Deaths.ContainsKey(player.Id))
			deaths = 0;
			else
			deaths = data.Deaths[player.Id];
				
			int score = kills-deaths;
			if(score <= 0) score = 0;
			return score;
		}
		
		string GetPlayerTag(Player player)
		{
			string playertag = "";
			foreach(var c in Config["ScoreTags", "Tags"] as Dictionary<string, object>)
			{
				if(GetPlayerScore(player) >= Convert.ToInt32(c.Value)) playertag = c.Key;
					return playertag;
			}
			return null;
		}
		
		string sendlang(string key, string userID = null)
        {
            return lang.GetMessage(key, this, userID);
        }
		
	}
}