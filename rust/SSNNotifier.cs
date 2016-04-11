using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using System.Security.Cryptography;
using System.Text;

namespace Oxide.Plugins
{
    [Info("SSNNotifier", "Umlaut", "0.0.4")]
    class SSNNotifier : RustPlugin
    {
        // Types defenition

        class ConfigData
        {
            public bool print_errors = true;
            public string server_name = "insert here name of your server";
            public string server_password = "insert here password of your server";
        }

        // Object vars

        ConfigData m_configData;
        WebRequests m_webRequests = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");

        public string m_host = "survival-servers-network.com";
        public string m_port = "1024";

        //

        void Loaded()
        {
            try
            {
                m_configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NotifyServerOn();

            timer.Repeat(60*5, 0, () => NotifyServerOn());
        }

        private void Unload()
        {
            NotifyServerOff();
        }

        void LoadDefaultConfig()
        {
            m_configData = new ConfigData();
            Config.WriteObject(m_configData, true);
        }

        //

        void OnPlayerInit(BasePlayer player)
        {
            NotifyPlayerConnected(player.userID, player.displayName, player.net.connection.ipaddress.Split(':')[0]);
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            NotifyPlayerDisconnected(player.userID, player.displayName);
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null || hitInfo.Initiator == null)
            {
                return;
            }

            BasePlayer playerVictim = entity as BasePlayer;
            BasePlayer playerKiller = hitInfo.Initiator as BasePlayer;

            if (playerVictim == null || playerKiller == null || playerVictim == playerKiller)
            {
                return;
            }

            double distance = Math.Sqrt(
                Math.Pow(playerVictim.transform.position.x - playerKiller.transform.position.x, 2) +
                Math.Pow(playerVictim.transform.position.y - playerKiller.transform.position.y, 2) +
                Math.Pow(playerVictim.transform.position.z - playerKiller.transform.position.z, 2));

            NotifyMurder(playerVictim.userID, playerVictim.displayName, playerKiller.userID, playerKiller.displayName, hitInfo.Weapon.GetItem().info.itemid, distance, hitInfo.isHeadshot, playerVictim.IsSleeping());
        }

        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            string message = "";
            foreach (string line in arg.Args)
            {
                message += line + " ";
            }

            message = message.Trim();
            if (message != "" && message[0] != '/')
            {
                NotifyPlayerChatMessage(player.userID, player.displayName, message);
            }

            return null;
        }

        //

        void SendRequest(string subUrl, List<string> values)
        {
            string requestUrl = "http://%host:%port/%suburl".Replace("%host", m_host).Replace("%port", m_port).Replace("%suburl", subUrl);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("server_name", m_configData.server_name);

            string body = "";
            foreach (string line in values)
            {
                body += line;
                body += "\n";
            }

            byte[] data = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(body + m_configData.server_password));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            headers.Add("salt", sBuilder.ToString());
            m_webRequests.EnqueuePost(requestUrl, body, (code, response) => webRequestCallback(code, response), this, headers);
        }

        private void webRequestCallback(int code, string response)
        {
            if (response == null)
            {
                if (m_configData.print_errors)
                {
                    Puts("Couldn't get an answer from SSN service.");
                }
            }
            else if (code != 200)
            {
                if (m_configData.print_errors)
                {
                    Puts("SSN error (%code): %text".Replace("%code", code.ToString()).Replace("%text", response));
                }
            }
        }

        //

        void NotifyMurder(ulong victimSteamId, string victimDisplayName, ulong killerSteamId, string killerDisplayName, int weaponRustItemId, double distance, bool isHeadshot, bool isSleeping)
        {
            List<string> values = new List<string>();
            values.Add(victimSteamId.ToString());
            values.Add(victimDisplayName);
            values.Add(killerSteamId.ToString());
            values.Add(killerDisplayName);
            values.Add(weaponRustItemId.ToString());
            values.Add(ItemManager.CreateByItemID(weaponRustItemId).info.displayName.english);
            values.Add(distance.ToString());
            values.Add(isHeadshot ? "true" : "false");
            values.Add(isSleeping ? "true" : "false");

            SendRequest("murder/create", values);
        }

        void NotifyPlayerConnected(ulong steamid, string displayName, string ipAddress)
        {
            List<string> values = new List<string>();
            values.Add(steamid.ToString());
            values.Add(displayName);
            values.Add(ipAddress);

            SendRequest("player/connect", values);
        }

        void NotifyPlayerDisconnected(ulong steamid, string displayName)
        {
            List<string> values = new List<string>();
            values.Add(steamid.ToString());
            values.Add(displayName);
            SendRequest("player/disconnect", values);
        }

        void NotifyPlayerChatMessage(ulong steamid, string displayName, string messageText)
        {
            List<string> values = new List<string>();
            values.Add(steamid.ToString());
            values.Add(displayName);
            values.Add(messageText);

            SendRequest("player/chat_message", values);
        }

        void NotifyPlayerBan(ulong steamid, string displayName, string reason)
        {
            List<string> values = new List<string>();
            values.Add(steamid.ToString());
            values.Add(displayName);
            values.Add(reason);
            SendRequest("player/ban", values);
        }

        void NotifyPlayerMute(ulong steamid, string displayName, string reason)
        {
            List<string> values = new List<string>();
            values.Add(steamid.ToString());
            values.Add(displayName);
            values.Add(reason);
            SendRequest("player/mute", values);
        }

        void NotifyServerOn()
        {
            List<string> values = new List<string>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                values.Add(player.userID.ToString());
            }
            SendRequest("server/on", values);
        }

        void NotifyServerOff()
        {
            SendRequest("server/off", new List<string>());
        }
    }
}
