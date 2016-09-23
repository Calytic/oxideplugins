using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Popup Notifications", "emu / k1lly0u", "0.1.0", ResourceId = 1252)]
    public class PopupNotifications : RustPlugin
    {
        private static Vector2 position;
        private static Vector2 dimensions;
        private static ConfigData config;
        
        #region Oxide Hooks
        void OnServerInitialized()
        {
            lang.RegisterMessages(Messages, this);
            LoadVariables();
            config = configData;
            position = new Vector2(configData.PositionX, configData.PositionY);
            dimensions = new Vector2(configData.Width, configData.Height);
        }
        #endregion       

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public float ShowDuration { get; set; }
            public int MaxShownMessages { get; set; }
            public bool ScrollDown { get; set; }
            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
            public float Spacing { get; set; }
            public float Transparency { get; set; }
            public float FadeTime { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                ShowDuration = 8f,
                FadeTime = 1f,
                Height = 0.1f,
                MaxShownMessages = 8,
                PositionX = 0.8f,
                PositionY = 0.78f,
                ScrollDown = true,
                Spacing = 0.01f,
                Transparency = 0.7f,
                Width = 0.19f
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Commands
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
				var target = GetPlayerByName(args[0]);
                if (target is string)
                {
                    SendReply(player, (string)target);
                    return;
                }
				if(target as BasePlayer != null)
					CreatePopupOnPlayer(args[1], target as BasePlayer);				
			}
			else
				SendReply(player,msg("Usage: /popupmsg \"Your message here.\" OR /popupmsg \"player name\" \"You message here.\"", player.UserIDString));
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
					Puts(msg("Invalid duration"));
					
			}
			else
				Puts(msg("Usage: popupmsg.global \"Your message here.\" duration"));
        }

        [ConsoleCommand("popupmsg.toplayer")]
        private void ConPopupMessageToPlayer(ConsoleSystem.Arg arg)
        {
            if (!arg.isAdmin)
                return;

            if (arg.Args.Length >= 1)
            {
                var player = GetPlayerByName(arg.Args[1]);
                if (player is string)
                {
                    SendReply(arg, (string)player);
                    return;
                }
                if (arg.Args.Length == 2)
                {
                    if (player as BasePlayer != null && (player as BasePlayer).isConnected)
                        CreatePopupOnPlayer(arg.Args[0], player as BasePlayer);
                    else
                        Puts(msg("Couldn't send popup notification to player"));
                }
                else if (arg.Args.Length == 3)
                {

                    if (player as BasePlayer != null && (player as BasePlayer).isConnected)
                    {
                        float duration;
                        if (float.TryParse(arg.Args[2], out duration))
                            CreatePopupOnPlayer(arg.Args[0], player as BasePlayer, duration);
                        else
                            Puts(msg("Invalid duration"));
                    }
                    else
                        Puts(msg("Couldn't send popup notification to player"));

                }
                else
                    Puts(msg("Usage: popupmsg.toplayer \"Your message here.\" \"Player name\" <duration>"));
            }
        }
		
		private object GetPlayerByName(string name)
		{
            var players = covalence.Players.FindPlayers(name);
            if (players != null)
            {
                if (players.ToList().Count == 0)
                    return msg("No players found with that name");
                else if (players.ToList().Count > 1)
                    return msg("Multiple players found with that name");
                else if (players.ToArray()[0].Object is BasePlayer)
                {
                    if (!(players.ToArray()[0].Object as BasePlayer).isConnected)
                        return string.Format(msg("{0} is not online"), players.ToArray()[0].Name);
                    return players.ToArray()[0].Object as BasePlayer;
                }            
            }
            return msg("Unable to find a valid player");
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
				
				if(valid && slot >= 0 && slot <= configData.MaxShownMessages)
				{
					Notifier.GetPlayerNotifier(arg.Player()).DestroyNotification(slot);
				}
			}
        }
        #endregion

        #region API
        [HookMethod("CreatePopupNotification")]
		void CreatePopupNotification(string message, BasePlayer player = null, float duration = 0f)
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
        #endregion

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
			private Dictionary<int, string> usedSlots = new Dictionary<int, string>();
			
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
				for(int i = 0; i < config.MaxShownMessages; i++)
				{
					if(!usedSlots.ContainsKey(i))
						return i;
				}
				
				return -1;
			}
		
			public void CreateNotification(string message, float showDuration = 0f)
			{				
				Vector2 anchorMin;
				Vector2 anchorMax;
				Vector2 offset;
				int slot = GetEmptySlot();
				
				if(slot == -1)
				{
					msgQueue.Add(message);
					return;
				}
				
				offset = (new Vector2(0, dimensions.y) + new Vector2(0, config.Spacing)) * slot;
				
				if(config.ScrollDown)
					offset *= -1;
				
				anchorMin = position + offset;
				anchorMax = anchorMin + dimensions;

                string name = PUMSG + slot;
                var element = CreatePopupMessage(name, $"0.5 0.5 0.5 {config.Transparency}", message, slot, $"{anchorMin.x} {anchorMin.y}", $"{anchorMax.x} {anchorMax.y}");

                CuiHelper.AddUi(player, element);
				if(showDuration < 1f)
					showDuration = config.ShowDuration;
				
				usedSlots.Add(slot, name);
				StartCoroutine(DestroyAfterDuration(slot, showDuration));
			}
			
			public void DestroyNotification(int slot)
			{
                CuiHelper.DestroyUi(player, usedSlots[slot]);				
				StartCoroutine(DelayedRemoveFromSlot(slot));
			}
			
			private IEnumerator DelayedRemoveFromSlot(int slot)
			{
				yield return new WaitForSeconds(config.FadeTime + 0.5f);
				
				if(usedSlots.ContainsKey(slot))
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
        }
        #region UI
        static string PUMSG = "PopupNotification";
        static CuiElementContainer CreatePopupMessage(string panelName, string color, string text, int slot, string aMin, string aMax)
        {
            var NewElement = new CuiElementContainer();
            NewElement.Add(new CuiElement
            {
                Name = panelName,
                Components =
                        {
                            new CuiImageComponent { Color = color, FadeIn = 0.3f },
                            new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax }
                        },
                FadeOut = config.FadeTime
            });
            NewElement.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panelName,
                Components =
                    {
                        new CuiTextComponent {Text = text, Align = TextAnchor.MiddleCenter, FontSize = 14 },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    },
                FadeOut = config.FadeTime
            });
            var buttonName = CuiHelper.GetGuid();
            NewElement.Add(new CuiElement
            {
                Name = buttonName,
                Parent = panelName,
                Components =
                    {
                        new CuiButtonComponent {Command = $"popupmsg.close {slot}", Color = "1.0 0.3 0.0 0.5", FadeIn = 0.3f, ImageType = UnityEngine.UI.Image.Type.Tiled },
                        new CuiRectTransformComponent {AnchorMin = "0.89 0.79", AnchorMax = "0.99 0.99" }
                    },
                FadeOut = config.FadeTime
            });
            NewElement.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = buttonName,
                Components =
                    {
                        new CuiTextComponent {Text = "X", FadeIn = 0.3f, Align = TextAnchor.MiddleCenter, FontSize = 12  },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1" }
                    },
                FadeOut = config.FadeTime
            });
            return NewElement;
        }
        #endregion

        #region Localization
        string msg(string key, string id = null) => lang.GetMessage(key, this, id);
        Dictionary<string, string> Messages = new Dictionary<string, string>
            {
            {"Usage: /popupmsg \"Your message here.\" OR /popupmsg \"player name\" \"You message here.\"","Usage: /popupmsg \"Your message here.\" OR /popupmsg \"player name\" \"You message here.\"" },
            {"Invalid duration","Invalid duration" },
            {"Usage: popupmsg.global \"Your message here.\" duration","Usage: popupmsg.global \"Your message here.\" duration" },
            {"Couldn't send popup notification to player","Couldn't send popup notification to player" },
            {"Usage: popupmsg.toplayer \"Your message here.\" \"Player name\" <duration>","Usage: popupmsg.toplayer \"Your message here.\" \"Player name\" <duration>" },
            {"No players found with that name","No players found with that name" },
            {"Multiple players found with that name","Multiple players found with that name" },
            {"{0} is not online","{0} is not online" },
            {"Unable to find a valid player","Unable to find a valid player" }
            };
        #endregion
    }
}
