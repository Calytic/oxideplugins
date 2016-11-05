using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ResolutionAPI", "azalea`", "0.1")]
    public class ResolutionAPI : RustPlugin
    {
        Dictionary<ulong, string> resolutionData = new Dictionary<ulong, string>();

		DynamicConfigFile resolutionDataFile = Interface.Oxide.DataFileSystem.GetFile("ResolutionAPI");

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"RES_TITLE", "Choose the flat square or the ratio of your monitor"},
                {"RES_SELECTED", "You choosed {0} resolution.\nTo change, use command <color=#DCFF66>/ratio</color>"},
                {"RES_CHOOSED", "Choosed"}
            }, this);
        }

        string GetLangMessage(string key, string steamID = null) => lang.GetMessage(key, this, steamID);

        void Loaded() => LoadDefaultMessages();

        protected override void LoadDefaultConfig()
        {
            Config["ShowOnPlayerInit"] = true;
        }

        void OnServerInitialized() => resolutionData = resolutionDataFile.ReadObject<Dictionary<ulong, string>>();

        void OnServerSave() => resolutionDataFile.WriteObject(resolutionData);

        void Unload() => resolutionDataFile.WriteObject(resolutionData);

        void OnPlayerInit(BasePlayer player)
        {
            if (!Config.Get<bool>("ShowOnPlayerInit")) return;

            if (player == null)
                return;

            if (resolutionData.ContainsKey(player.userID))
                return;

            if (player.IsReceivingSnapshot())
            {
                timer.Once(2, () => OnPlayerInit(player));
                return;
            }

            ShowResolutionMenu(player);
        }

		void ShowResolutionMenu(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, "ResolutionMain");

			CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "ResolutionMain",
				Parent = "Hud",
                Components =
                {
                    new CuiRawImageComponent { Color = "0 0 0 0" },
					new CuiNeedsCursorComponent(),
                    new CuiRectTransformComponent()
                }
            });

            string TitlePanelName = CuiHelper.GetGuid();

            container.Add(new CuiElement
            {
                Name = TitlePanelName,
                Parent = "ResolutionMain",
                Components =
                {
                    new CuiRawImageComponent { Color = "1 1 1 0.4" },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2 0.85",
                        AnchorMax = "0.8 0.95"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = TitlePanelName,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetLangMessage("RES_TITLE"),
                        FontSize = 25,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent(),
                    new CuiOutlineComponent() { Color = "0 0 0 1" }
                }
            });

            //16/9: 

            //9 / 16 = X;
            //(xMax - xMin) / X = B
            // Ymax - B = Ymin

            string UserResolution = (string)(GetUserResolution(player.userID) ?? string.Empty);

            CreateBox(container, "0.2 0.4444", "0.4 0.8", "16x9", UserResolution == "16x9");
            CreateBox(container, "0.6 0.48", "0.8 0.8", "16x10", UserResolution == "16x10");
            CreateBox(container, "0.2 0.1333", "0.4 0.4", "4x3", UserResolution == "4x3");
            CreateBox(container, "0.6 0.15", "0.8 0.4", "5x4", UserResolution == "5x4");
            											
			CuiHelper.AddUi(player, container); 
		}
				
        void CreateBox(CuiElementContainer container, string AnchorMin, string AnchorMax, string Resolution, bool Active = false)
        {
            string BoxName = CuiHelper.GetGuid();

            container.Add(new CuiElement
            {
                Name = BoxName,
                Parent = "ResolutionMain",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "resolution.select " + Resolution, // NOTE! Text will put as CMD
                        Close = "ResolutionMain",
                        Color = "1 1 1 0.4"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = AnchorMin,
                        AnchorMax = AnchorMax
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = BoxName,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = Resolution + (Active ? "\n\n" + GetLangMessage("RES_CHOOSED") : ""),
                        FontSize = 30,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent(),
                    new CuiOutlineComponent() { Color = "0 0 0 1" }
                }
            });
        }
		
		[ConsoleCommand("resolution.select")]
		void ConsoleCmd_Select(ConsoleSystem.Arg arg)
		{
			if (arg.connection != null)
            {
				string SelectedResolution = arg.Args[0];

                switch(SelectedResolution)
                {
                    case "16x9":
                    case "16x10":
                    case "5x4":
                    case "4x3": break;

                    default: return;
                }

                BasePlayer player = arg.Player();

				resolutionData[player.userID] = SelectedResolution;

                Interface.Oxide.CallHook("OnUserResolution", player, SelectedResolution);

				SendReply(player, $"<size=16>{string.Format(GetLangMessage("RES_SELECTED"), SelectedResolution)}</size>");
			}
		}
		
		[ChatCommand("ratio")]
        void ChatCmd_Ratio(BasePlayer player, string command, string[] args) => ShowResolutionMenu(player);

        object GetUserResolution(ulong userId)
        {
            string ResolutionState;

            if (resolutionData.TryGetValue(userId, out ResolutionState))
                return ResolutionState;

            return null;
        }
    }
}