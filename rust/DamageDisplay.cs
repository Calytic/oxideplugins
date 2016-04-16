using Rust;
using System;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using Oxide.Core;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{

    [Info("DamageDisplayGUI", "cogu", "1.6.1")]
    [Description("Displays the given damage to a player in a GUI")]
    class DamageDisplay : RustPlugin
    {
		/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
		///////////////////////////////////////				Configs			\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
		/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
				
        HashSet<ulong> users = new HashSet<ulong>();
		System.Collections.Generic.List<ulong> DisabledFor = new System.Collections.Generic.List<ulong>();
		public float DisplayAttackerNameRange => Config.Get<float>("DisplayAttackerNameRange");
		public float DisplayVictimNameRange => Config.Get<float>("DisplayVictimNameRange");
		public bool DisplayDistance => Config.Get<bool>("DisplayDistance");
		public bool DisplayBodyPart => Config.Get<bool>("DisplayBodyPart");
		public bool DamageForAttacker => Config.Get<bool>("DamageForAttacker");
		public bool DamageForVictim => Config.Get<bool>("DamageForVictim");
		public float X_MinVictim => Config.Get<float>("X_MinVictim");
		public float X_MaxVictim => Config.Get<float>("X_MaxVictim");
		public float Y_MinVictim => Config.Get<float>("Y_MinVictim");
		public float Y_MaxVictim => Config.Get<float>("Y_MaxVictim");
		public float X_MinAttacker => Config.Get<float>("X_MinAttacker");
		public float X_MaxAttacker => Config.Get<float>("X_MaxAttacker");
		public float Y_MinAttacker => Config.Get<float>("Y_MinAttacker");
		public float Y_MaxAttacker => Config.Get<float>("Y_MaxAttacker");
		public float DisplayTime => Config.Get<float>("DisplayTime");
		void Unload() => SaveData();
        void OnServerSave() => SaveData();
		
		protected override void LoadDefaultConfig()
        {	
			Config["DisplayAttackerNameRange"] = 50;
			Config["DisplayVictimNameRange"] = 50;
			Config["DisplayDistance"] = true;
			Config["DisplayBodyPart"] = false;
			Config["DamageForVictim"] = true;
			Config["DamageForAttacker"] = true;
			Config["X_MinVictim"] = 0.355;
			Config["X_MaxVictim"] = 0.475;
			Config["Y_MinVictim"] = 0.91;
			Config["Y_MaxVictim"] = 0.99;
			Config["X_MinAttacker"] = 0.555;
			Config["X_MaxAttacker"] = 0.675;
			Config["Y_MinAttacker"] = 0.91;
			Config["Y_MaxAttacker"] = 0.99;
			Config["DisplayTime"] = 0.3f;
			SaveConfig();
        }
		
		/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\

        private void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo hitInfo)
        {
			
            if (victim == null || hitInfo == null) return;
            DamageType type = hitInfo.damageTypes.GetMajorityDamageType();
            if (type == null) return;

            if (hitInfo?.Initiator != null && hitInfo?.Initiator?.ToPlayer() != null && users.Contains(hitInfo.Initiator.ToPlayer().userID) && victim.ToPlayer() != null)
            {
				/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
				///////////////////////////////////////				Configs			\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
				/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
				
				string vName = "";
				string aName = "";
				string bodypart = "";
				string distance = "";
				float displaytime = DisplayTime;
				float xminvictim = X_MinVictim;
				float xmaxvictim = X_MaxVictim;
				float yminvictim = Y_MinVictim;
				float ymaxvictim = Y_MaxVictim;
				float xminattacker = X_MinAttacker;
				float xmaxattacker = X_MaxAttacker;
				float yminattacker = Y_MinAttacker;
				float ymaxattacker = Y_MaxAttacker;
				float distanceBetween = Vector3.Distance(victim.transform.position,hitInfo.Initiator.ToPlayer().transform.position);
				float displayattackerrange = DisplayAttackerNameRange;
				float displayvictimrange = DisplayVictimNameRange;
				/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
				
				/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
				///////////////////////////////////////				Handling		\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
				/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
				
                NextTick(() =>
                {
				double damage = System.Convert.ToDouble(Math.Round(hitInfo.damageTypes.Total(), 0, MidpointRounding.AwayFromZero));
				if(DisplayAttackerNameRange == -1){
					displayattackerrange = 65535;
				}
				if(DisplayVictimNameRange == -1){
					displayvictimrange = 65535;
				}
				if(DisplayBodyPart && hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer()){
					bodypart = FirstUpper(GetBoneName(victim, ((uint)hitInfo?.HitBone)));
				}
				if(hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer() && distanceBetween <= displayattackerrange){
					aName = hitInfo?.Initiator?.ToPlayer().displayName;
				}
				if(hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer() && distanceBetween <= displayvictimrange){
					vName = victim.ToPlayer().displayName;
				}
				if(DisplayDistance && hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer()){
					distance = GetDistance(victim, hitInfo);
				}
				NextTick(() =>
                {
					if(DamageForAttacker && hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer()){
						UseUI(hitInfo?.Initiator?.ToPlayer(), "-"+damage.ToString()+" HP", distance, vName, bodypart, xminattacker, xmaxattacker, yminattacker, ymaxattacker);
					}
					if(DamageForVictim && hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer()){
						UseUI(victim.ToPlayer(), "<color=#cc0000>-"+damage.ToString()+" HP"+"</color>", "<color=#cc0000>"+distance+"</color>", "<color=#cc0000>"+aName+"</color>", "<color=#cc0000>"+bodypart+"</color>", xminvictim, xmaxvictim, yminvictim, ymaxvictim);
					}
				});
							timer.Once(displaytime, () =>
							{
								DestroyNotification(hitInfo?.Initiator?.ToPlayer());
								DestroyNotification(victim.ToPlayer());
							});
                });
				/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
				
            }
        }
		
		/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
		/////////////////////////////////					  Extra					 \\\\\\\\\\\\\\\\\\\\\\\\\\
		/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
		
		void Loaded()
        {
            LoadSavedData();
			Puts("DamageDisplay by cogu is now LIVE!");
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				users.Add(player.userID);
			}
        }
		
		void OnPlayerInit()
		{
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				users.Add(player.userID);
			}
		}

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("DamageDisplay", users);
        void LoadSavedData()
        {
            HashSet<ulong> users = Interface.Oxide.DataFileSystem.ReadObject<HashSet<ulong>>("DamageDisplay");
            this.users = users;
        }
		
		private void UseUI(BasePlayer player, string dmg, string dst, string name, string bpart, float xmin, float xmax, float ymin, float ymax)
        {
			float dtime = DisplayTime;
			
            var elements = new CuiElementContainer();
            CuiElement textElement = new CuiElement
                {
                    Name = "DamageDisplay",
                    Parent = "HUD/Overlay",
                    FadeOut = dtime,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = dmg+"\n"+dst+"\n"+bpart+"\n"+name,
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            FadeIn = dtime
                        },
                        new CuiOutlineComponent
                        {
                            Distance = "1.0 1.0",
                            Color = "0.0 0.0 0.0 1.0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = xmin + " " + ymin,
                            AnchorMax = xmax + " " + ymax
                        }
                    }
                };
				elements.Add(textElement);
            CuiHelper.AddUi(player, elements);
        }
		
		private void DestroyNotification(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, "DamageDisplay");
		}
		
		string GetBoneName(BaseCombatEntity entity, uint boneId) => entity?.skeletonProperties?.FindBone(boneId)?.name?.english ?? "Body";
		string FirstUpper(string original)
        {
            if (original == string.Empty)
                return string.Empty;
            List<string> output = new List<string>();
            foreach (string word in original.Split(' '))
                output.Add(word.Substring(0, 1).ToUpper() + word.Substring(1, word.Length - 1));
            return ListToString(output, 0, " ");
        }
		string ListToString(List<string> list, int first, string seperator) => string.Join(seperator, list.Skip(first).ToArray());
		string GetDistance(BaseCombatEntity entity, HitInfo info)
            {
                float distance = 0.0f;
                if (entity != null && info.Initiator != null)
                {
                    distance = Vector3.Distance(info.Initiator.transform.position, entity.transform.position);
                }
                return distance.ToString("0.0").Equals("0.0") ? "" : distance.ToString("0.0") + "m";
            }
			
		/////////////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
	}
}
