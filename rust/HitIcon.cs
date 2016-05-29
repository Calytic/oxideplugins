using UnityEngine;
using System;
using Oxide.Game.Rust.Cui;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.IO;
using System.Linq;
using Rust;
namespace Oxide.Plugins
{
	[Info("HitIcon", "serezhadelaet", "0.3")]
    [Description("Configurable precached icon when you hit player|friend|clanmate")]
    class HitIcon : RustPlugin
    {
		int dmgtextsize;
		float timetodestroy;
		bool usefriends;
		bool useclans;
		bool usesound;
		bool Changed;
		bool friendapi = false;
		bool showdmg;
		bool showclandmg;
		bool showfrienddmg;
		string colorfriend;
		string colorhead;
		string colorbody;
		string colorclan;
		string dmgcolor;
		string endcolor;
		string matesound;
		Oxide.Plugins.Timer activateTimer;
		Dictionary<ulong, byte> active = new Dictionary<ulong, byte>();
		[PluginReference]
        private Plugin Friends;
		private void InitializeFriendsAPI()
        {
            if (Friends != null)
            { friendapi = true; Puts("Friends here");}
			else
			{ friendapi = false; Puts("Friends not here");}
		}
		private bool AreFriendsAPIFriend(string playerId, string friendId)
        {
			try
			{
				bool result = (bool)Friends?.CallHook("AreFriends", playerId, friendId);
				return result;
			}
            catch
			{
				return false;
			}
        }
		
		void language()
		{
			lang.RegisterMessages(new Dictionary<string, string>
            {
				{"Enabled", "Hit icon was <color=green>enabled</color>"},
				{"Disabled", "Hit icon was <color=red>disabled</color>"}
			}, this);
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
		
		void LoadVariables()
        {
			colorclan = Convert.ToString(GetConfig("Color", "Hit clanmate color", "0 256 0 1"));
            colorfriend = Convert.ToString(GetConfig("Color", "Hit friend color", "0 256 0 1"));
			colorhead = Convert.ToString(GetConfig("Color", "Hit head color", "256 0 0 1"));
			colorbody = Convert.ToString(GetConfig("Color", "Hit body color", "256 256 256 1"));
			dmgcolor = Convert.ToString(GetConfig("Color", "Text damage color", "256 256 256 1"));
			dmgtextsize = Convert.ToInt32(GetConfig("Configuration", "Damage text size", 15));
			usefriends = Convert.ToBoolean(GetConfig("Configuration", "Use Friends", true));
			useclans = Convert.ToBoolean(GetConfig("Configuration", "Use Clans", true));
			usesound = Convert.ToBoolean(GetConfig("Configuration", "Use sound when mate get attacked", true));
			showdmg = Convert.ToBoolean(GetConfig("Configuration", "Show damage", true));
			showclandmg = Convert.ToBoolean(GetConfig("Configuration", "Show clanmate damage", false));
			showfrienddmg = Convert.ToBoolean(GetConfig("Configuration", "Show friend damage", true));
			matesound = Convert.ToString(GetConfig("Configuration", "When mate get attacked sound fx", "assets/prefabs/instruments/guitar/effects/guitarpluck.prefab"));
			timetodestroy = Convert.ToSingle(GetConfig("Configuration", "Time to destroy", 0.4f));
			
            if (Changed)
            {
                SaveConfig();
                Changed = false;
            }
        }
		
		protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file!");
            Config.Clear();
            LoadVariables();
        }
		
		public class disabledplayers
		{
			string playerid;
			
			public disabledplayers()
			{
			}
			public disabledplayers(BasePlayer player)
			{
				playerid = player.userID.ToString();
			}
			public ulong GetPlayer()
            {
                ulong userid;
                if (!ulong.TryParse(playerid, out userid)) return 0;
                return userid;
            }
		}
		
		public class StoredData
		{
			public List<ulong> DisabledUsers = new List<ulong>();

			public StoredData()
			{
			}
		}
		static StoredData storedData;
		static List<ulong> DisabledUsers = new List<ulong>();
		static void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("HitIcon", storedData);
		static void LoadData()
		{
			try
			{
				storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("HitIcon");
			}
			catch
			{
				storedData = new StoredData();
			}
			foreach (var disabledplayer in storedData.DisabledUsers)
				DisabledUsers.Add(disabledplayer);
		}
		
		ImageCache ImageAssets;
        GameObject HitObject;
		private void cacheImage()
        {
			HitObject = new GameObject();
            ImageAssets = HitObject.AddComponent<ImageCache>();
            ImageAssets.imageFiles.Clear();
			string dataDirectory = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar;
			ImageAssets.getImage("hitimage", dataDirectory + "hit.png");
			download();
		}
		
        public class ImageCache : MonoBehaviour
        {
            public Dictionary<string, string> imageFiles = new Dictionary<string, string>();

            public List<Queue> queued = new List<Queue>();

            public class Queue
            {
                public string url { get; set; }
                public string name { get; set; }
            }

            private void OnDestroy()
            {
                foreach (var value in imageFiles.Values)
                {
                    FileStorage.server.RemoveEntityNum(uint.MaxValue, Convert.ToUInt32(value));
                }
            }

            public void getImage(string name, string url)
            {
                queued.Add(new Queue
                {
                    url = url,
                    name = name
                });
            }

            IEnumerator WaitForRequest(Queue queue)
            {
                using (var www = new WWW(queue.url))
                {
                    yield return www;
                    
                    if (string.IsNullOrEmpty(www.error))
                    {
                        imageFiles.Add(queue.name, FileStorage.server.Store(www.bytes, FileStorage.Type.png, uint.MaxValue).ToString());
                    }
                    else
                    {
						Debug.Log("Error downloading hit.png . It must be in your oxide/data/");
                        ConsoleSystem.Run.Server.Normal("oxide.unload HitIcon");
					}
                }
            }

            public void process()
            {
				StartCoroutine(WaitForRequest(queued[0]));
			}
        }

        public string fetchImage(string name)
        {
            string result;
            if (ImageAssets.imageFiles.TryGetValue(name, out result))
                return result;
            return string.Empty;
        }
		
		void download()
        {
            ImageAssets.process();
        }
		
		private class GUIv4
        {
            string guiname { get; set; }
            CuiElementContainer container = new CuiElementContainer();

            public void add(string uiname, string image, string start, string end, string colour)
            {
                guiname = uiname;
                CuiElement element = new CuiElement
                {
                    Name = guiname,
                    Components =
					{
						new CuiRawImageComponent
						{
							Png = image,
							Color = colour
						},
						new CuiRectTransformComponent
						{
							AnchorMin = start,
							AnchorMax = end
						}
					}
                };
                container.Add(element);
            }
			
			public void dmg(string uiname, string uitext, string start, string end, string uicolor, int uisize)
            {
				
				CuiElement element = new CuiElement
                {
					Name = uiname,
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = uitext,
                                FontSize = uisize,
								Color = uicolor
								
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = start,
                                AnchorMax = end
                            }
                        }
                };
                container.Add(element);
            }
			
			public void send(BasePlayer player)
            {
				CuiHelper.DestroyUi(player, guiname);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(container.ToJson()));
            }
		}
		
		private void OnPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
			var victim = hitinfo.HitEntity as BasePlayer;
			if (victim && !DisabledUsers.Contains(attacker.userID))
			{
				endcolor = colorbody;
				if(useclans)
				{
					String[] ClanTagAttacker = attacker.displayName.Split(new Char [] {' '});
					String[] ClanTagVictim = victim.displayName.Split(new Char [] {' '});
					if (ClanTagAttacker[0] == ClanTagVictim[0] && ClanTagVictim[0].StartsWith("[") && ClanTagAttacker[0].StartsWith("[") && ClanTagVictim[0].EndsWith("]") && ClanTagAttacker[0].EndsWith("]")) 
					{
						endcolor = colorclan;
						if(usesound) Effect.server.Run(matesound, attacker.transform.position, Vector3.zero, null, false);
					}
				}
				
				if (endcolor != colorclan && friendapi && usefriends && AreFriendsAPIFriend(victim.userID.ToString(), attacker.userID.ToString()))
                {
                    endcolor = colorfriend;
					if(usesound) Effect.server.Run(matesound, attacker.transform.position, Vector3.zero, null, false);
				}
				
				if(hitinfo.isHeadshot && endcolor != colorclan && endcolor != colorfriend)
				{
					endcolor = colorhead;
				}
				
				if(active[attacker.userID] == 0) 
				{
					try //Check if not destroyed before show new///
					{ 
						activateTimer.Destroy();
					} 
					catch 
					{   
						CuiHelper.DestroyUi(attacker,"hitdmg");
						CuiHelper.DestroyUi(attacker,"hitpng"); 
					}
				}
				active[attacker.userID] = 0;
				GUIv4 gui = new GUIv4();
				gui.add("hitpng", fetchImage("hitimage"), "0.492 0.4905", "0.506 0.5095", endcolor);
				gui.send(attacker);
				NextTick(() => 
				{
					if(endcolor == colorfriend && !showfrienddmg) showdmg = false;
					if(endcolor == colorclan && !showclandmg) showdmg = false;
					if(showdmg)
					{
						float damage = (int)hitinfo.damageTypes.Total();
						gui.dmg("hitdmg", damage.ToString(), "0.495 0.425", "0.55 0.48", dmgcolor, dmgtextsize);
						gui.send(attacker);
					}
					timer.Repeat(timetodestroy, 1, () =>
					{	
						active[attacker.userID] = 1;
						CuiHelper.DestroyUi(attacker,"hitdmg");
						CuiHelper.DestroyUi(attacker,"hitpng");
					});
				});
			}
		}
		
		[ChatCommand("hit")]
		void toggle(BasePlayer player)
		{	
			if(!DisabledUsers.Contains(player.userID))
			{
				storedData.DisabledUsers.Add(player.userID);
				DisabledUsers.Add(player.userID);
				PrintToChat(player, lang.GetMessage("Disabled", this, player.UserIDString));
			} 
			else
			{
				storedData.DisabledUsers.Remove(player.userID);
				DisabledUsers.Remove(player.userID);
				PrintToChat(player, lang.GetMessage("Enabled", this, player.UserIDString));
			}
		}
		
		void OnServerInitialized()
		{
			cacheImage();
			InitializeFriendsAPI();
		}
		
		void OnPlayerInit(BasePlayer player)
		{
			active[player.userID] = 0;
		}
		
		void Loaded()
        {
			LoadData();
			language();
			LoadVariables();
			foreach (BasePlayer player in BasePlayer.activePlayerList)
				{
					active[player.userID] = 0;
				}
		}
		
		void Unloaded()
        {
			if (BasePlayer.activePlayerList.Count > 0)
            {
				foreach (BasePlayer player in BasePlayer.activePlayerList)
				{
					CuiHelper.DestroyUi(player,"hitpng");
					CuiHelper.DestroyUi(player,"hitdmg");
				}
			}
			SaveData();
			UnityEngine.Object.Destroy(HitObject);
		}
	}
}	