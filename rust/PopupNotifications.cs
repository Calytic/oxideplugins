using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Oxide.Core.Plugins;
using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("Popup Notifications", "emu", "0.0.8", ResourceId = 1252)]
    public class PopupNotifications : RustPlugin
    {
		#region Config
		private static float defaultShowDuration = 8f;
		private static int maxShownMessages = 5;
		private static bool scrollDown = true;
		private static Vector2 position = new Vector2(0.8f, 0.78f);
		private static Vector2 dimensions = new Vector2(0.19f, 0.1f);
		private static float spacing = 0.01f;
		private static float transparency = 0.7f;
		private static float fadeOutTime = 1f;
		
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["DefaultShowDuration"] = defaultShowDuration;
            Config["MaxShownMessages"] = maxShownMessages;
            Config["ScrollDown"] = scrollDown;
            Config["PositionX"] = position.x;
            Config["PositionY"] = position.y;
            Config["Width"] = dimensions.x;
            Config["Height"] = dimensions.y;
            Config["Spacing"] = spacing;
            Config["Transparency"] = transparency;
            Config["FadeTime"] = fadeOutTime;
            SaveConfig();
        }
		
		private void LoadConfig()
		{
			defaultShowDuration = GetConfig<float>("DefaultShowDuration", defaultShowDuration);
			maxShownMessages = GetConfig<int>("MaxShownMessages", maxShownMessages);
			scrollDown = GetConfig<bool>("ScrollDown", scrollDown);
			float x = GetConfig<float>("PositionX", position.x);
			float y = GetConfig<float>("PositionY", position.y);
			position = new Vector2(x, y);
			float w = GetConfig<float>("Width", dimensions.x);
			float h = GetConfig<float>("Height", dimensions.y);
			dimensions = new Vector2(w, h);
			spacing = GetConfig<float>("Spacing", spacing);
			transparency = GetConfig<float>("Transparency", transparency);
			fadeOutTime = GetConfig<float>("FadeTime", fadeOutTime);
		}
		
        T GetConfig<T>(string key, T defaultValue) {
            try {
                var val = Config[key];
                if (val == null)
                    return defaultValue;
                if (val is List<object>) {
                    var t = typeof(T).GetGenericArguments()[0];
                    if (t == typeof(String)) {
                        var cval = new List<string>();
                        foreach (var v in val as List<object>)
                            cval.Add((string)v);
                        val = cval;
                    } else if (t == typeof(int)) {
                        var cval = new List<int>();
                        foreach (var v in val as List<object>)
                            cval.Add(Convert.ToInt32(v));
                        val = cval;
                    }
                } else if (val is Dictionary<string, object>) {
                    var t = typeof(T).GetGenericArguments()[1];
                    if (t == typeof(int)) {
                        var cval = new Dictionary<string,int>();
                        foreach (var v in val as Dictionary<string, object>)
                            cval.Add(Convert.ToString(v.Key), Convert.ToInt32(v.Value));
                        val = cval;
                    }
                }
                return (T)Convert.ChangeType(val, typeof(T));
            } catch (Exception ex) {
                return defaultValue;
            }
        }
		#endregion
		
        [ChatCommand("popupmsg")]
        private void SendPopupMessage(BasePlayer player, string command, string[] args)
        {
			if(!player.IsAdmin())
				return;
		
			if(args.Length == 1)
			{
				CreateGlobalPopup(args[0]);
			}
			else if(args.Length == 2)
			{
				BasePlayer target = GetPlayerByName(args[0]);
				if(target != null)
					CreatePopupOnPlayer(args[1], target);
				else
					player.ChatMessage("No players found with that name.");
			}
			else
				player.ChatMessage("Usage: /popupmsg \"Your message here.\" OR /popupmsg \"player name\" \"You message here.\"");
		}
		
        [ConsoleCommand("popupmsg.global")]
        private void ConPopupMessageGlobal(ConsoleSystem.Arg arg)
        {
			if(!arg.isAdmin)
				return;
		
			if(arg.Args.Length == 1)
			{
				CreateGlobalPopup(arg.Args[0]);
			}
			else if(arg.Args.Length == 2)
			{
				float duration;
				if(float.TryParse(arg.Args[1], out duration))
					CreateGlobalPopup(arg.Args[0], duration);
				else
					Puts("Invalid duration");
					
			}
			else
				Puts("Usage: popupmsg.global \"Your message here.\" duration");
        }
		
        [ConsoleCommand("popupmsg.toplayer")]
        private void ConPopupMessageToPlayer(ConsoleSystem.Arg arg)
        {
			if(!arg.isAdmin)
				return;
		
			BasePlayer player;
		
			if(arg.Args.Length == 2)
			{
				player = GetPlayerByName(arg.Args[1]);
				
				if(player != null)
					CreatePopupOnPlayer(arg.Args[0], player);
				else
					Puts("Couldn't send popup notification to player");
			}
			else if(arg.Args.Length == 3)
			{
				player = GetPlayerByName(arg.Args[1]);
				
				if(player != null)
				{
					float duration;
					if(float.TryParse(arg.Args[2], out duration))
						CreatePopupOnPlayer(arg.Args[0], player, duration);
					else
						Puts("Invalid duration");
				}
				else
					Puts("Couldn't send popup notification to player");
					
			}
			else
				Puts("Usage: popupmsg.toplayer \"Your message here.\" \"Player name\" duration");
        }
		
		private BasePlayer GetPlayerByName(string name)
		{
			string currentName;
			string lastName;
			BasePlayer foundPlayer = null;
			name = name.ToLower();
		
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			{
				currentName = player.displayName.ToLower();
				
				if(currentName.Contains(name))
				{
					if(foundPlayer != null)
					{
						lastName = foundPlayer.displayName;
						if(currentName.Replace(name, "").Length < lastName.Replace(name, "").Length)
						{
							foundPlayer = player;
						}
					}
					
					foundPlayer = player;
				}
			}
		
			return foundPlayer;
		}
		
        [ConsoleCommand("popupmsg.close")]
        private void CloseCommand(ConsoleSystem.Arg arg)
        {
			if(arg.Player() == null)
				return;
		
			if(arg.Args.Length == 1)
			{
				int slot;
				bool valid = int.TryParse(arg.Args[0], out slot);
				
				if(valid && slot >= 0 && slot <= maxShownMessages)
				{
					Notifier.GetPlayerNotifier(arg.Player()).DestroyNotification(slot);
				}
			}
        }

		[HookMethod("CreatePopupNotification")]
		void CreatePopupNotification(string message, BasePlayer player = null, double duration = 0)
		{
			if(player != null)
			{
				CreatePopupOnPlayer(message, player, (float)duration);
			}
			else
			{
				CreateGlobalPopup(message, (float)duration);
			}
		}
		
		
		private void CreatePopupOnPlayer(string message, BasePlayer player, float duration = 0f)
		{
			Notifier.GetPlayerNotifier(player).CreateNotification(message, duration);
		}
		
		private void CreateGlobalPopup(string message, float duration = 0f)
		{
			foreach(BasePlayer activePlayer in BasePlayer.activePlayerList)
			{
				CreatePopupOnPlayer(message, activePlayer, duration);
			}
		}
		
		class Notifier : MonoBehaviour
		{
			private List<string> msgQueue = new List<string>();
			private List<int> usedSlots = new List<int>();
			
			private BasePlayer player;
			
			void Awake()
			{
				player = GetComponent<BasePlayer>();
			}
		
			public static Notifier GetPlayerNotifier(BasePlayer player)
			{
				Notifier component = player.GetComponent<Notifier>();
				
				if(component == null)
					component = player.gameObject.AddComponent<Notifier>();
					
				return component;
			}
			
			private int GetEmptySlot()
			{
				for(int i = 0; i < maxShownMessages; i++)
				{
					if(!usedSlots.Contains(i))
						return i;
				}
				
				return -1;
			}
		
			public void CreateNotification(string message, float showDuration = 0f)
			{
				string uiDef;
				Vector2 anchorMin;
				Vector2 anchorMax;
				Vector2 offset;
				int slot = GetEmptySlot();
				
				if(slot == -1)
				{
					msgQueue.Add(message);
					return;
				}
				
				offset = (new Vector2(0, dimensions.y) + new Vector2(0, spacing)) * slot;
				
				if(scrollDown)
					offset *= -1;
				
				anchorMin = position + offset;
				anchorMax = anchorMin + dimensions;
				
				uiDef = notificationDefinition;
				uiDef = uiDef.Replace("{slot}", slot.ToString());
				uiDef = uiDef.Replace("{alpha}", transparency.ToString());
				uiDef = uiDef.Replace("{fadetime}", fadeOutTime.ToString());
				uiDef = uiDef.Replace("{positionMin}", anchorMin.x + " " + anchorMin.y);
				uiDef = uiDef.Replace("{positionMax}", anchorMax.x + " " + anchorMax.y);
				uiDef = uiDef.Replace("{message}", message);
				
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(uiDef, null, null, null, null));
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Alert" + slot, null, null, null, null));
				
				if(showDuration < 1f)
					showDuration = defaultShowDuration;
				
				usedSlots.Add(slot);
				StartCoroutine(DestroyAfterDuration(slot, showDuration));
			}
			
			public void DestroyNotification(int slot)
			{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Notification" + slot, null, null, null, null));
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("NotificationText" + slot, null, null, null, null));
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Alert" + slot, null, null, null, null));
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("NotificationCloseButton" + slot, null, null, null, null));
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("NotificationCloseText" + slot, null, null, null, null));
				
				StartCoroutine(DelayedRemoveFromSlot(slot));
			}
			
			private IEnumerator DelayedRemoveFromSlot(int slot)
			{
				yield return new WaitForSeconds(fadeOutTime + 0.5f);
				
				if(usedSlots.Contains(slot))
					usedSlots.Remove(slot);
					
				if(msgQueue.Count > 0)
				{
					CreateNotification(msgQueue[0]);
					msgQueue.RemoveAt(0);
				}
			}
			
			private IEnumerator DestroyAfterDuration(int slot, float duration)
			{
				yield return new WaitForSeconds(duration);
				
				DestroyNotification(slot);
			}
		
			#region UIdef
			private const string notificationDefinition = @"[  
							{
								""parent"": ""HUD/Overlay"",
								""name"": ""Notification{slot}"",
								""fadeOut"": ""{fadetime}"",
								""components"":
								[
									{
										""type"":""UnityEngine.UI.Image"",
										""fadeIn"": ""0.3"",
										""color"": ""0.5 0.5 0.5 {alpha}"" 
									},
									{
										""type"":""RectTransform"",
										""anchormin"": ""{positionMin}"",
										""anchormax"": ""{positionMax}""
									}
								]
							},
							{
								""parent"": ""Notification{slot}"",
								""name"": ""NotificationText{slot}"",
								""fadeOut"": ""{fadetime}"",
								""components"":
								[
									{
										""type"":""UnityEngine.UI.Text"",
										""text"":""{message}"",
										""fontSize"":14,
										""align"": ""MiddleCenter"",
										""fadeIn"": ""0.3"",
										""color"": ""1.0 1.0 1.0 1.0""
									},
									{
										""type"":""RectTransform"",
										""anchormin"": ""0 0"",
										""anchormax"": ""1 1""
									}
								]
							},
							{
								""parent"": ""Notification{slot}"",
								""name"": ""NotificationCloseButton{slot}"",
								""fadeOut"": ""{fadetime}"",
								""components"":
								[
									{
										""type"":""UnityEngine.UI.Button"",
										""command"":""popupmsg.close {slot}"",
										""color"": ""1.0 0.3 0.0 0.5"",
										""fadeIn"": ""0.3"",
										""imagetype"": ""Tiled""
									},
									{
										""type"":""RectTransform"",
										""anchormin"": ""0.9 0.8"",
										""anchormax"": ""1 1""
									}
								]
							},
							{
								""parent"": ""NotificationCloseButton{slot}"",
								""name"": ""NotificationCloseText{slot}"",
								""fadeOut"": ""{fadetime}"",
								""components"":
								[
									{
										""type"":""UnityEngine.UI.Text"",
										""text"":""X"",
										""fontSize"":12,
										""align"": ""MiddleCenter"",
										""fadeIn"": ""0.3"",
										""color"": ""1.0 1.0 1.0 1.0""
									},
									{
										""type"":""RectTransform"",
										""anchormin"": ""0 0"",
										""anchormax"": ""1 1""
									}
								]
							},
							{
								""parent"": ""Notification{slot}"",
								""name"": ""Alert{slot}"",
								""fadeOut"": ""0.5"",
								""components"":
								[
									{
										""type"":""UnityEngine.UI.Image"",
										""color"": ""1.0 0.5 0.0 0.3""
									},
									{
										""type"":""RectTransform"",
										""anchormin"": ""0 0"",
										""anchormax"": ""1 1""
									}
								]
							}
						]
						";
			#endregion
		}
		
		private void OnServerInitialized()
		{
			LoadConfig();
		}
    }
}
