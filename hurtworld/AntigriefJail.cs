using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    /*
    [B]Changelog 1.1.0[/B]
    [LIST]
    [*] Fixed broken references
    [*] Added timer to transport user to jail if found outside
    [*] Started LangApi conversion
    [*] Code simplification & Optimization
    [/LIST]
    */
    [Info("AntigriefJail", "Pho3niX90", "1.1.0")]
    class AntigriefJail : HurtworldPlugin
    {
        #region Configuration
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"helpMsg", "When a player crosses that fine line from playing the game to just griefing players you can type /giveticket PlayerName. If the player gets more than {0} tickets in the allotted timeframe then that player is sent to jail for {1} minutes."}
            }, this);
        }
        private string chatServerColor = "#ff0000";
        private string chatServerTitle = "Sheriff->";

        private string jailCoordinates_x = "-3132.176";
        private string jailCoordinates_y = "206.25";
        private string jailCoordinates_z = "-2520.553";

        private int ticketsBeforeJail = 5;
        private int jailLength = 60;

        private List<IssuedTicket> tickets = new List<IssuedTicket>();

        
        void Loaded()
        {
            LoadMessages();
        }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        protected override void LoadDefaultConfig()
        {
            this.SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config["chatServerTitle"] = chatServerTitle;
            Config["chatServerColor"] = chatServerColor;
            Config["ticketsBeforeJail"] = ticketsBeforeJail;
            Config["jailLength"] = jailLength; 

            Config["tickets"] = tickets;

            Config["jailCoordinates_x"] = jailCoordinates_x;
            Config["jailCoordinates_y"] = jailCoordinates_y;
            Config["jailCoordinates_z"] = jailCoordinates_z;
            base.SaveConfig();
        }
        #endregion

        void Init()
        {
            CheckCfg<string>("chatServerColor", ref chatServerColor);
            CheckCfg<string>("chatServerTitle", ref chatServerTitle);

            CheckCfg<string>("jailCoordinates_x", ref jailCoordinates_x);
            CheckCfg<string>("jailCoordinates_y", ref jailCoordinates_y);
            CheckCfg<string>("jailCoordinates_z", ref jailCoordinates_z);

            CheckCfg<int>("ticketsBeforeJail", ref ticketsBeforeJail);
            CheckCfg<int>("jailLength", ref jailLength);

            CheckCfg<List<IssuedTicket>>("tickets", ref tickets);
        }


        #region Chat Commands

        [ChatCommand("sheriff")]
        void HelpMessage(PlayerSession player, string command, string[] args)
        {
            hurt.SendChatMessage(player, GetMsg("helpMsg", player).Replace("{0}", ticketsBeforeJail.ToString()).Replace("{1}", jailLength.ToString()));
        }


        [ChatCommand("getjailcoords")]
        void GetJailCoords(PlayerSession player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;
            hurt.SendChatMessage(player, string.Format("The Jail is currently located at {0}, {1}, {2}", jailCoordinates_x, jailCoordinates_y, jailCoordinates_z));
        }

        [ChatCommand("gotojail")]
        void TelePortToJail(PlayerSession player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            player.WorldPlayerEntity.transform.position = GetJailCoords();
        }

        [ChatCommand("giveticket")]
        void GiveTicket(PlayerSession player, string command, string[] args)
        {
            //get the ID of the player for whom to give the ticket
            PlayerSession ticketTo = null;
            ticketTo = getPlayerFromName(string.Join(" ", args));
            if (ticketTo == null)
            {
                hurt.SendChatMessage(player, "Ticket was not issued. Unable to locate the player.");
                return;
            }
            else
            {
                int currentTicketCount = 1;
                bool alreadyIssued = false;

                foreach (var t in tickets)
                {
                    if (t.To == ticketTo.SteamId.m_SteamID)
                    {
                        currentTicketCount++;
                    }
                    if (t.From == player.SteamId.m_SteamID)
                    {
                        //this player has already issued a ticket to this
                        alreadyIssued = true;
                        break;
                    }
                }

                if (alreadyIssued)
                {
                    hurt.SendChatMessage(player, "You can only issue a single ticket to a player.");
                    return;
                }

                if (currentTicketCount >= ticketsBeforeJail)
                {
                    //Jail the player here and clear out all the tickets for jailed player

                    //Send the player to jail
                    int repeats = (int)Math.Floor(Math.Round(jailLength / 5d));
                    timer.Repeat(5, repeats, () => player.WorldPlayerEntity.transform.position = GetJailCoords());

                    //Create the time for unjailing the player
                    timer.Once(jailLength, () => UnJailPlayer(ticketTo));

                    //Send a message to the issuer
                    hurt.BroadcastChat(ticketTo.Name + " has been put in jail.");

                    //send a message to the defendant
                    hurt.SendChatMessage(ticketTo, player.Name + " has issued you a ticket for griefing. You have exceeded the maximum number of tickets and have now been transported to jail.");
                }
                else
                {
                    //add a ticket
                    var newTicket = new IssuedTicket();
                    newTicket.From = player.SteamId.m_SteamID;
                    newTicket.To = ticketTo.SteamId.m_SteamID;
                    tickets.Add(newTicket);
                    //Send a message to the issuer
                    hurt.SendChatMessage(player, "A ticket has been issued to " + ticketTo.Name);

                    //send a message to the defendant
                    hurt.SendChatMessage(ticketTo, player.Name + " has issued you a ticket for griefing. You have a total of " + currentTicketCount.ToString() + ". If you get " + ticketsBeforeJail.ToString() + ", you will be sent to jail for " + getTimeStringFromSeconds(jailLength) + ".");
                }
            }
        }

        [ChatCommand("setjailcoords")]
        void SetJailCoords(PlayerSession player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            jailCoordinates_x = player.WorldPlayerEntity.transform.position.x.ToString();
            jailCoordinates_y = player.WorldPlayerEntity.transform.position.y.ToString();
            jailCoordinates_z = player.WorldPlayerEntity.transform.position.z.ToString();
            SaveConfig();
            hurt.SendChatMessage(player, string.Format("Jail Coordinates have been set to {0}, {1}, {2}", jailCoordinates_x, jailCoordinates_y, jailCoordinates_z));
        }

        [ChatCommand("setjailtime")]
        void SetJailLength(PlayerSession player, string command, string[] args)
        {
            var isValid = Int32.TryParse(args[0], out jailLength);
            if (!player.IsAdmin || args.Length != 1 || !isValid)
                return;

            SaveConfig();
            hurt.SendChatMessage(player, "Jail time has been set to " + getTimeStringFromSeconds(jailLength) + ".");
        }

        [ChatCommand("setticketcountforjail")]
        void SetTicketCountBeforeJail(PlayerSession player, string command, string[] args)
        {
            var isValid = Int32.TryParse(args[0], out ticketsBeforeJail);
            if (!player.IsAdmin || args.Length != 1 || !isValid)
                return;

            SaveConfig();
            hurt.SendChatMessage(player, "# of tickets you get before going to jail has been set to " + ticketsBeforeJail + ".");
        }
        #endregion

        void UnJailPlayer(PlayerSession player)
        {
            if (player != null)
            {
                player.WorldPlayerEntity.transform.position += player.WorldPlayerEntity.transform.forward * 20;
                hurt.SendChatMessage(player, "You have served your Jail time.");
                hurt.BroadcastChat(player.Name + " has completed his jail time and has been set free.");

                //Remove all his tickets
                tickets.RemoveAll(x => x.To == player.SteamId.m_SteamID);
            }

        }

        #region common util functions
        bool isCoord(string arg)
        {
            double testDbl = 0;
            return Double.TryParse(arg, out testDbl);
        }

        Vector3 GetJailCoords()
        {
            Vector3 coord = new Vector3(float.Parse(jailCoordinates_x), float.Parse(jailCoordinates_y), float.Parse(jailCoordinates_z));
            return coord;
        }

        string getTimeStringFromSeconds(int seconds)
        {
            int s = seconds % 60;
            int minutes = seconds / 60;
            if (s > 0)
                return minutes.ToString() + " minutes and " + s.ToString() + " seconds";
            else
                return minutes.ToString() + " minutes";
        }

        PlayerSession getPlayerFromName(string identifier)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;
            foreach (var i in sessions)
            {
                if (i.Value.Name.ToLower().Contains(identifier.ToLower()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }

            return session;
        }


        #endregion

        #region Additional Classes
        class IssuedTicket
        {
            public ulong To { get; set; }
            public ulong From { get; set; }
        }
        string GetMsg(string key, object userID = null)
        {
            return (userID == null) ? lang.GetMessage(key, this) : lang.GetMessage(key, this, userID.ToString());
        }
        #endregion
    }



}
