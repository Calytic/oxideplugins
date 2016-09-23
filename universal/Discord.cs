using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;

using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Discord", "seanbyrne88", "0.2.0")]
    [Description("Discord Client for Rust OxideMods")]
    class Discord : CovalencePlugin
    {
        private static PluginConfig Settings;
        private static string BaseURLTemplate = "https://discordapp.com/api/channels/{{ChannelID}}/messages";

        void SendMessage(string MessageText)
        {
            string payloadJson = JsonConvert.SerializeObject(new DiscordPayload()
            {
                MessageText = MessageText
            });

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if(Settings.BotToken.StartsWith("Bot "))
            {
                headers.Add("Authorization", Settings.BotToken);
            }
            else
            {
                headers.Add("Authorization", String.Format("Bot {0}", Settings.BotToken));
            }
            headers.Add("Content-Type", "application/json");

            string url = BaseURLTemplate.Replace("{{ChannelID}}", Settings.ChannelID.ToString());
            webrequest.EnqueuePost(url, payloadJson, (code, response) => PostCallBack(code, response), this, headers);
            //webrequest.EnqueuePost(UrlWithAccessToken, payloadJson, (code, response) => PostCallBack(code, response), this);
        }

        

        void PostCallBack(int code, string response)
        {
            if(code != 200)
            {
                PrintWarning(String.Format("Discord Api responded with {0}: {1}", code, response));
            }
        }

        void Init()
        {
            LoadConfigValues();
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config.WriteObject(DefaultConfig(), true);

            PrintWarning("Default Configuration File Created");
        }

        private void LoadConfigValues()
        {
            Settings = Config.ReadObject<PluginConfig>();
        }

        private PluginConfig DefaultConfig()
        {
            return new PluginConfig
            {
                BotToken = String.Empty,
                ChannelID = 0
            };
        }

        private class PluginConfig
        {
            public string BotToken { get; set; }
            public ulong ChannelID { get; set; }
        }

        class DiscordPayload
        {
            [JsonProperty("content")]
            public string MessageText { get; set; }
        }
    }
}

