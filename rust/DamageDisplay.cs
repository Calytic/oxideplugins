using Rust;
using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{

    [Info("DamageDisplayGUI", "cogu", "1.5.4")]
    [Description("Displays the given damage to a player in a GUI")]
    class DamageDisplay : RustPlugin
    {
        HashSet<ulong> users = new HashSet<ulong>();
		System.Collections.Generic.List<ulong> DisabledFor = new System.Collections.Generic.List<ulong>();
		public bool DisplayAttackerName => Config.Get<bool>("DisplayAttackerName");
		public bool DisplayVictimName => Config.Get<bool>("DisplayVictimName");
		public bool DisplayDistance => Config.Get<bool>("DisplayDistance");
		public bool DisplayBodyPart => Config.Get<bool>("DisplayBodyPart");
		public bool DamageForAttacker => Config.Get<bool>("DamageForAttacker");
		public bool DamageForVictim => Config.Get<bool>("DamageForVictim");
		public float AnchorMinVictim => Config.Get<float>("AnchorMinVictim");
		public float AnchorMaxVictim => Config.Get<float>("AnchorMaxVictim");
		public float AnchorMinAttacker => Config.Get<float>("AnchorMinAttacker");
		public float AnchorMaxAttacker => Config.Get<float>("AnchorMaxAttacker");
		void Unload() => SaveData();
        void OnServerSave() => SaveData();
		#region JSON
        string json = @"[  
		                { 
							""name"": ""DamageDisplay"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 0.0"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.455 0.91"",
                                    ""anchormax"": ""0.575 0.99""
                                }
                            ]
                        },
						{
                            ""parent"": ""DamageDisplay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{damage}"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""{anchormin} 0.6"",
                                    ""anchormax"": ""{anchormax} 1""
                                }
                            ]
                        },
						{
                            ""parent"": ""DamageDisplay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{victim}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""{anchormin} 0.0000000001"",
                                    ""anchormax"": ""{anchormax} 0.6""
                                }
                            ]
                        },
						{
                            ""parent"": ""DamageDisplay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{attacker}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""{anchormin} 0.0000000001"",
                                    ""anchormax"": ""{anchormax} 0.6""
                                }
                            ]
                        },
						{
                            ""parent"": ""DamageDisplay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{bodypart}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""{anchormin} 0.255"",
                                    ""anchormax"": ""{anchormax} 0.755""
                                }
                            ]
                        },
						{
                            ""parent"": ""DamageDisplay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{distance}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""{anchormin} 0.355"",
                                    ""anchormax"": ""{anchormax} 0.965""
                                }
                            ]
                        }
                    ]
                    ";
        #endregion
		
		protected override void LoadDefaultConfig()
        {	
			Config["DisplayAttackerName"] = false;
			Config["DisplayVictimName"] = false;
			Config["DisplayDistance"] = true;
			Config["DisplayBodyPart"] = false;
			Config["DamageForVictim"] = true;
			Config["DamageForAttacker"] = true;
			Config["AnchorMinAttacker"] = 0;
			Config["AnchorMaxAttacker"] = 0.5;
			Config["AnchorMinVictim"] = 0;
			Config["AnchorMaxVictim"] = 1.5;
			SaveConfig();
        }

        private void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo hitInfo)
        {
            if (victim == null || hitInfo == null) return;
            DamageType type = hitInfo.damageTypes.GetMajorityDamageType();
            if (type == null) return;

            if (hitInfo?.Initiator != null && hitInfo?.Initiator?.ToPlayer() != null && users.Contains(hitInfo.Initiator.ToPlayer().userID) && victim.ToPlayer() != null)
            {
				string vName = "";
				string aName = "";
				string bodypart = "";
				string distance = "";
				float anchorminvictim = AnchorMinVictim;
				float anchormaxvictim = AnchorMaxVictim;
				float anchorminattacker = AnchorMinAttacker;
				float anchormaxattacker = AnchorMaxAttacker;
				
                NextTick(() =>
                {
				if(DisplayBodyPart && hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer()){
					bodypart = FirstUpper(GetBoneName(victim, ((uint)hitInfo?.HitBone)));
				}
				if(DisplayAttackerName && hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer()){
					aName = hitInfo?.Initiator?.ToPlayer().displayName;
				}
				if(DisplayVictimName && hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer()){
					vName = victim.ToPlayer().displayName;
				}
				if(DisplayDistance && hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer()){
					distance = GetDistance(victim, hitInfo);
				}
				if(DamageForAttacker && hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer()){
					CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = hitInfo?.Initiator?.ToPlayer().net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{damage}", ""+"-"+System.Convert.ToDouble(Math.Round(hitInfo.damageTypes.Total(), 0, MidpointRounding.AwayFromZero))+" HP").Replace("{victim}", ""+vName).Replace("{attacker}", "").Replace("{bodypart}", ""+bodypart).Replace("{distance}", ""+distance).Replace("{anchormin}", ""+anchorminattacker).Replace("{anchormax}", ""+anchormaxattacker)));
					DisabledFor.Remove(Convert.ToUInt64(hitInfo?.Initiator?.ToPlayer().UserIDString));
				}
				if(DamageForVictim && hitInfo?.Initiator?.ToPlayer() != victim.ToPlayer()){
					CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = victim.ToPlayer().net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{damage}", ""+"<color=#cc0000>"+"-"+System.Convert.ToDouble(Math.Round(hitInfo.damageTypes.Total(), 0, MidpointRounding.AwayFromZero))+" HP"+"</color>").Replace("{victim}", "").Replace("{attacker}", ""+"<color=#cc0000>"+aName+"</color>").Replace("{bodypart}", ""+"<color=#cc0000>"+bodypart+"</color>").Replace("{distance}", ""+"<color=#cc0000>"+distance+"</color>").Replace("{anchormin}", ""+anchorminvictim).Replace("{anchormax}", ""+anchormaxvictim)));
					DisabledFor.Remove(Convert.ToUInt64(victim.ToPlayer().UserIDString));
				}
							timer.Once(3f, () =>
							{
								CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = hitInfo?.Initiator?.ToPlayer().net.connection }, null, "DestroyUI", new Facepunch.ObjectList("DamageDisplay"));
								CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = victim.ToPlayer().net.connection }, null, "DestroyUI", new Facepunch.ObjectList("DamageDisplay"));
								DisabledFor.Add(Convert.ToUInt64(hitInfo?.Initiator?.ToPlayer().UserIDString));
								DisabledFor.Add(Convert.ToUInt64(victim.ToPlayer().UserIDString));
							});
                });
            }
        }
		
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
	}
}
