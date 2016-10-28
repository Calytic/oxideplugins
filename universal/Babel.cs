/*
TODO:
- Implement the silly access token method for Bing/Microsoft
- Add API method to get all shared supported languages
- Add warning if an invalid language code is used
*/

using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("Babel", "Wulf/lukespragg", "0.3.3", ResourceId = 1963)]
    [Description("Plugin API for translating messages using free or paid translation services")]

    class Babel : CovalencePlugin
    {
        #region Initialization

        string apiKey;
        string service;

        protected override void LoadDefaultConfig()
        {
            Config["ApiKey"] = apiKey = GetConfig("ApiKey", "");
            Config["Service"] = service = GetConfig("Service", "google");
            SaveConfig();
        }

        void Init() => LoadDefaultConfig();

        #endregion

        #region Translation API

        /// <summary>
        /// Translates text from one language to another language
        /// </summary>
        /// <param name="text"></param>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <param name="callback"></param>
        void Translate(string text, string to, string from = "auto", Action<string> callback = null)
        {
            if (string.IsNullOrEmpty(apiKey) && service.ToLower() != "google")
            {
                PrintWarning("Invalid ApiKey, please check that it is valid and try again");
                return;
            }

            // Reference: https://www.microsoft.com/en-us/translator/getstarted.aspx
            if (service.ToLower() == "bing" || service.ToLower() == "microsoft")
            {
                webrequest.EnqueueGet($"http://api.microsofttranslator.com/V2/Ajax.svc/Detect?appId={apiKey}&text={Uri.EscapeUriString(text)}", (c, r) =>
                {
                    if (c != 200 || r == null || r.Contains("<html>")) return;

                    if (r.Contains("ArgumentException: Invalid appId"))
                    {
                        PrintWarning("Invalid ApiKey, please check that it is valid and try again");
                        return;
                    }

                    if (r.Contains("ArgumentOutOfRangeException: 'to' must be a valid language"))
                    {
                        PrintWarning("Invalid language code, please check that it is valid and try again");
                        return;
                    }

                    var url = $"http://api.microsofttranslator.com/V2/Ajax.svc/Translate?appId={apiKey}&to={to}&from={r}&text={Uri.EscapeUriString(text)}";
                    webrequest.EnqueuePost(url, null, (code, response) => Callback(code, response, text, callback), this);
                }, this);
                return;
            }

            // Reference: https://cloud.google.com/translate/v2/quickstart
            if (service.ToLower() == "google")
            {
                var url = string.IsNullOrEmpty(apiKey)
                    ? $"https://translate.googleapis.com/translate_a/single?client=gtx&tl={to}&sl={from}&dt=t&q={Uri.EscapeUriString(text)}"
                    : $"https://www.googleapis.com/language/translate/v2?key={apiKey}&target={to}&source={from}&q={Uri.EscapeUriString(text)}";
                webrequest.EnqueuePost(url, null, (code, response) => Callback(code, response, text, callback), this);
                return;
            }

            // Reference: https://tech.yandex.com/keys/get/?service=trnsl
            if (service.ToLower() == "yandex")
            {
                webrequest.EnqueueGet($"https://translate.yandex.net/api/v1.5/tr.json/detect?key={apiKey}&hint={from}&text={Uri.EscapeUriString(text)}", (c, r) =>
                {
                    if (c != 200 || r == null) return;

                    from = (string)JObject.Parse(r).GetValue("lang");
                    var url = $"https://translate.yandex.net/api/v1.5/tr.json/translate?key={apiKey}&lang={from}-{to}&text={Uri.EscapeUriString(text)}";
                    webrequest.EnqueuePost(url, null, (code, response) => Callback(code, response, text, callback), this);
                }, this);
            }
        }

        void Callback(int code, string response, string text, Action<string> callback = null)
        {
            if (code != 200 || response == null || response.Contains("<html>"))
            {
                PrintWarning($"Translation failed! {UppercaseFirst(service)} responded with: {response} ({code})");
                return;
            }

            if (response.Contains("ArgumentOutOfRangeException: 'from'"))
            {
                PrintWarning("Translation failed! Invalid 'from' language, make sure a valid language code is used");
                return;
            }

            string translated = null;
            if (service.ToLower() == "google" && string.IsNullOrEmpty(apiKey))
                translated = new Regex(@"\[\[\[""((?:\s|.)+?)"",""(?:\s|.)+?""").Match(response).Groups[1].ToString();
            else if (service.ToLower() == "google" && !string.IsNullOrEmpty(apiKey))
                translated = (string)JObject.Parse(response)["data"]["translations"]["translatedText"];
            else if (service.ToLower() == "microsoft" || service.ToLower() == "bing")
                translated = new Regex("\"(.*)\"").Match(response).Groups[1].ToString();
            else if (service.ToLower() == "yandex")
                translated = (string)JObject.Parse(response).GetValue("text").First;
#if DEBUG
            PrintWarning($"Original: {text}");
            PrintWarning($"Translated: {translated}");
            if (translated == text) PrintWarning("Translated text is the same as original text");
#endif

            callback?.Invoke(string.IsNullOrEmpty(translated) ? text : Regex.Unescape(translated));
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            s.ToCharArray()[0] = char.ToUpper(s.ToCharArray()[0]);
            return new string(s.ToCharArray());
        }

        #endregion
    }
}
