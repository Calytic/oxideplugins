using System;
using Oxide.Core.Libraries;
using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("MinimumPlaytime", "KeyboardCavemen", "1.1.2")]
    class MinimumPlaytime : RustPlugin
    {
        private string steamAPIKey;
        private int minAmountOfHoursPlayed;
        private string errorMessageDisallowConnectionPart1;
        private string errorMessageDisallowConnectionPart2;
        private string errorMessagePrivateProfile;
        private string errorMessageSteamAPIUnavailable;

        private readonly WebRequests webRequests = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
        private float timerDelay = 0.1f;
        private List<string> verifiedPlayers;

        //Oxide Hook
        void Init()
        {
            this.steamAPIKey = Config.Get<string>("steamAPIKey");
            this.minAmountOfHoursPlayed = Config.Get<int>("minAmountOfHoursPlayed");
            this.errorMessageDisallowConnectionPart1 = Config.Get<string>("errorMessageDisallowConnectionPart1");
            this.errorMessageDisallowConnectionPart2 = Config.Get<string>("errorMessageDisallowConnectionPart2");
            this.errorMessagePrivateProfile = Config.Get<string>("errorMessagePrivateProfile");
            this.verifiedPlayers = Config.Get<List<string>>("verifiedPlayers");
            this.errorMessageSteamAPIUnavailable = Config.Get<string>("errorMessageSteamAPIUnavailable");
        }

        //Oxide Hook
        void LoadDefaultConfig()
        {
            Config["steamAPIKey"] = "insertAPIKeyHere";
            Config["minAmountOfHoursPlayed"] = 2;
            Config["errorMessageDisallowConnectionPart1"] = "Sorry, to join this server you need to have at least";
            Config["errorMessageDisallowConnectionPart2"] = "hours of playtime. We suggest you spend some time on a Official/Community server and hope you come back later!";
            Config["errorMessagePrivateProfile"] = "Sorry, to join this server you need to have a public Steam profile. Please change your profile settings to public and come back later!";
            Config["errorMessageSteamAPIUnavailable"] = "MinimumPlaytime was unable to check your Rust playtime because the Steam API is unavailable right now. Please try again later!";
            Config["verifiedPlayers"] = new List<string>();
        }

        //Oxide Hook
        void CanClientLogin(Network.Connection connection)
        {
            //Checks if the player has been verified before, if not -> verify player.
            if (!verifiedPlayers.Contains(connection.userid.ToString()))
            {
                if (steamAPIKey != "insertAPIKeyHere")
                {
                    string url = "http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key=" + steamAPIKey + "&format=json&input_json={\"appids_filter\":[252490],\"steamid\":" + connection.userid + "}";

                    ConnectingPlayer connectingPlayer = new ConnectingPlayer(connection);
                    webRequests.EnqueueGet(url, (code, response) => connectingPlayer.connectionResponse = WebRequestCallback(code, response), this, null, 1.5f);


                    RespondOnWebRequest(connectingPlayer);
                }
                else
                {
                    ConnectionAuth.Reject(connection, "MinimumPlaytime has no steamAPI key entered by the server owner yet.");
                }
            }
        }

        private void RespondOnWebRequest(ConnectingPlayer connectingPlayer)
        {
            timer.Once(timerDelay, () =>
            {
                if (connectingPlayer.connectionResponse != null)
                {
                    if (connectingPlayer.connectionResponse.Equals("LoginAllowed"))
                    {
                        verifiedPlayers.Add(connectingPlayer.connection.userid.ToString());
                        Config["verifiedPlayers"] = verifiedPlayers;
                        SaveConfig();
                    }
                    else
                    {
                        ConnectionAuth.Reject(connectingPlayer.connection, connectingPlayer.connectionResponse);
                    }
                }
                else
                {
                    RespondOnWebRequest(connectingPlayer);
                }
            });
        }


        private string WebRequestCallback(int code, string response)
        {
            if (code == 200 && response != null)
            {
                int gameCount = Convert.ToInt32(response.Substring(response.IndexOf("\"game_count\": ") + 14, 1));

                //If there is an instance of "Rust" in the players steam library (should always have one if player has a public profile).
                if (gameCount == 1)
                {
                    int playTimeForeverStartIndex = response.IndexOf("playtime_forever\"", 0) + 19;
                    int playTimeForeverLength = response.IndexOf("}", playTimeForeverStartIndex) - playTimeForeverStartIndex;

                    int playTimeForever = Convert.ToInt32(response.Substring(playTimeForeverStartIndex, playTimeForeverLength));
                    playTimeForever /= 60;

                    //Allow connection
                    if (playTimeForever >= minAmountOfHoursPlayed)
                    {
                        return "LoginAllowed";
                    }

                    //Disallow connection
                    else
                    {
                        return errorMessageDisallowConnectionPart1 + " " + minAmountOfHoursPlayed + errorMessageDisallowConnectionPart2;
                    }
                }
                //No instance of "Rust" found in the players steam library because of a private profile.
                else
                {
                    return errorMessagePrivateProfile;
                }
            }
            //Code is not 200 && response == null -> something went wrong with the webrequest.
            else
            {
                return errorMessageSteamAPIUnavailable;
            }
        }

        private class ConnectingPlayer
        {
            public Network.Connection connection;
            public string connectionResponse;

            public ConnectingPlayer(Network.Connection connection)
            {
                this.connection = connection;
            }
        }
    }
}