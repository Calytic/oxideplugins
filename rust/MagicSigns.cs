using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.IO;
namespace Oxide.Plugins
{
    [Info("MagicSigns", "Norn", 0.4, ResourceId = 1446)]
    [Description("Random signs.")]
    public class MagicSigns : RustPlugin
    {
        bool INIT = false;
        private readonly WebRequests scrapeQueue = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
        static GameObject WebObject;
        static UnityWeb UWeb;
        uint MaxSize = 2048U;
        class QueueItem
        {
            public string url;
            public Signage sign;
            public BasePlayer sender;
            public bool raw;

            public QueueItem(string ur, BasePlayer se, Signage si, bool raw)
            {
                url = ur;
                sender = se;
                sign = si;
                this.raw = raw;
            }
        }
        class UnityWeb : MonoBehaviour
        {
            internal static bool ConsoleLog = true;
            internal static string ConsoleLogMsg = "Player[{steam} {name}] loaded {id} image from {url}!";
            internal static int MaxActiveLoads = 3;
            private Queue<QueueItem> QueueList = new Queue<QueueItem>();
            static byte ActiveLoads = 0;
            private MemoryStream stream = new MemoryStream();
            byte JPGCompression = 85;

            public void Add(string url, BasePlayer player, Signage s, bool raw)
            {
                QueueList.Enqueue(new QueueItem(url, player, s, raw));
                if (ActiveLoads < MaxActiveLoads)
                    Next();
            }

            void Next()
            {
                if (QueueList.Count <= 0) return;
                ActiveLoads++;
                StartCoroutine(WaitForRequest(QueueList.Dequeue()));
            }

            private void ClearStream()
            {
                stream.Position = 0;
                stream.SetLength(0);
            }
            byte[] GetImageBytes(WWW www)
            {
                var tex = www.texture;
                byte[] img;
                img = tex.EncodeToJPG(JPGCompression);
                DestroyImmediate(tex);
                return img;
            }
            IEnumerator WaitForRequest(QueueItem info)
            {
                using (var www = new WWW(info.url))
                {
                    yield return www;
                    var player = info.sender;
                    if (www.error == null)
                    {

                        var img = info.raw ? www.bytes : GetImageBytes(www);
                            var sign = info.sign;
                            if (sign.textureID > 0U)
                                FileStorage.server.Remove(sign.textureID, FileStorage.Type.png, sign.net.ID);
                            ClearStream();
                            stream.Write(img, 0, img.Length);
                            sign.textureID = FileStorage.server.Store(stream, FileStorage.Type.png, sign.net.ID);
                            ClearStream();
                            sign.SendNetworkUpdate();
                            Interface.Oxide.CallHook("OnSignUpdated", sign, player);
                    }
                    ActiveLoads--;
                    Next();
                }
            }
    }
        List<string> ScrapedImages = new List<string>();
        private void ParseScrapeResponse(int code, string response)
        {
            if (response == null || code != 200)
            {
                Puts("Failed to scrape images...");
                return;
            }
            int count = 0;
            foreach (Match m in Regex.Matches(response, "<img.+?src=[\"'](.+?)[\"'].+?>", RegexOptions.IgnoreCase | RegexOptions.Multiline))
            {
                string src = m.Groups[1].Value;
                if(src.Length >= 1 && src.EndsWith(".jpg")) // Parsing scraped html
                {
                    string modified = src.Insert(0, "http:");
                    modified = modified.Remove(modified.Trim().Length - 5);
                    modified += ".jpg";
                    ScrapedImages.Add(modified);
                    count++;
                }
            }
            string tags = Config["Image", "Tags"].ToString(); if (tags.Length == 0) { tags = "Random Pool"; }
            if (count != 0) { Puts("Scraped " + count.ToString() + " images [ Tags: "+ tags + " ]"); if (!INIT) { INIT = true; } }
        }
        private void PopulateImageList()
        {
            INIT = false;
            ScrapedImages.Clear();
            string type = "q_type=png"; // Temporary png tag to prevent animated jpg
            string search_string = "http://imgur.com/search/time?"+type+"&q=" + System.Uri.EscapeDataString(Config["Image", "Tags"].ToString());
            scrapeQueue.EnqueueGet(search_string, (code, response) => ParseScrapeResponse(code, response), this);
        }
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ GENERAL SETTINGS ] ---

            Config["General", "AuthLevel"] = 2;

            Config["Image", "Tags"] = "";

            Config["Messages", "NoAuth"] = "You <color=red>don't</color> have the required authorization level to use this command.";
            Config["Messages", "NoSigns"] = "There are <color=red>no</color> signs to wipe.";

            SaveConfig();
        }
        [ChatCommand("ms")]
        private void ChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel >= Convert.ToInt32(Config["General", "AuthLevel"]))
            {
                if (args.Length == 0 || args.Length > 2)
                {
                    PrintToChat(player, "<color=yellow>ADMIN:</color> /ms <tags | <color=red>wipe</color>>");
                    if (Config["Image", "Tags"] != null)
                    {
                        string tags = null;
                        if(Config["Image", "Tags"].ToString().Length == 0){tags = "None. [<color=green>Random</color>]";}else{tags = Config["Image", "Tags"].ToString();}
                        if(tags != null) PrintToChat(player, "<color=yellow>Current Tags:</color> " + tags + ".");
                    }
                }
                else if (args[0] == "tags")
                {
                    if (args.Length == 1)
                    {
                        if (Config["Image", "Tags"] != null)
                        {
                            string tags = null;
                            if (Config["Image", "Tags"].ToString().Length == 0) { tags = "None. [<color=green>Random</color>]"; } else { tags = Config["Image", "Tags"].ToString(); }
                            if (tags != null) PrintToChat(player, "<color=yellow>Current Tags:</color> " + tags + ".");
                        }
                        PrintToChat(player, "<color=yellow>USAGE:</color> /ms tags <tags>\n(e.g. /ms tags \"<color=yellow>Emma Watson</color>\" or \"<color=yellow>Random</color>\" to go back to random pool).");
                    }
                    else
                    {
                        if(args[1].Length >= 1)
                        {
                            if(args[1].ToLower() == "random") { Config["Image", "Tags"] = ""; PrintToChat(player, "You have updated Magic Signs tags: <color=yellow>Random Pool</color>."); } else { Config["Image", "Tags"] = args[1]; PrintToChat(player, "You have updated Magic Signs tags: <color=yellow>" + Config["Image", "Tags"].ToString() + "</color>."); PopulateImageList(); }
                            SaveConfig();
                        }
                    }
                }
                else if (args[0] == "wipe")
                {
                    if (args.Length == 1)
                    {
                        var SIGNLIST = UnityEngine.Object.FindObjectsOfType<Signage>(); int count = 0;
                        if(SIGNLIST.Count() == 0) { PrintToChat(player, Config["Messages", "NoSigns"].ToString()); return; }
                        foreach (var sign in SIGNLIST)
                        {
                            if (sign != null)
                            {
                                sign.Kill();
                                count++;
                            }
                        }
                        if (count == 0) { PrintToChat(player, Config["Messages", "NoSigns"].ToString()); } else { PrintToChat(player, "Wiped <color=yellow>" + count.ToString() + "</color> signs."); Puts(player.displayName + " [ "+player.userID.ToString()+" ] has wiped the map of " + count.ToString() + " signs."); }
                    }
                }
            }
            else
            {
                if(Config["Messages", "NoAuth"] != null) { PrintToChat(player, Config["Messages", "NoAuth"].ToString()); }
            }
        }
        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (INIT != false)
            {
                BasePlayer player = planner.GetOwnerPlayer();
                if (permission.UserHasPermission(player.userID.ToString(), "magicsigns.able"))
                {
                    BaseEntity e = gameObject.ToBaseEntity();
                    if (!(e is BaseEntity) || player == null) { return; }
                    if (e.GetComponent<Signage>() != null)
                    {
                        if (!Cooldown.ContainsKey(player.userID))
                        {
                            int id = GetRandomInt(0, ScrapedImages.Count);
                            UWeb.Add(ScrapedImages[id], player, e.GetComponent<Signage>(), false);
                            ScrapedImages.Remove(ScrapedImages[id]);
                            if (player.net.connection.authLevel < 1) { InitCooldown(player); }
                            if (ScrapedImages.Count == 0) { Puts(player.displayName + " has used the last image in the list, scraping more..."); PopulateImageList(); }
                        }
                        else
                        {
                            PrintToChat(player, "You must wait " + Cooldown[player.userID].ToString() + " seconds before placing another sign.");
                            e.Kill();
                        }
                    }
                }
            }
            return;
        }

        void Unload()
        {
            GameObject.Destroy(WebObject);
            if(COOLDOWN_TIMER != null) { COOLDOWN_TIMER.Destroy(); }
        }
		System.Random rnd = new System.Random();
        protected int GetRandomInt(int min, int max)
        {
            return rnd.Next(min, max);
        }
        int DEFAULT_COOLDOWN = 10;
        Dictionary<ulong, int> Cooldown = new Dictionary<ulong, int>();
        private void CooldownTimer()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.isConnected && player != null)
                {
                    if (Cooldown.ContainsKey(player.userID))
                    {
                        int time_left = 0;
                        if (Cooldown.TryGetValue(player.userID, out time_left))
                        {
                            if (time_left <= 0) { Cooldown.Remove(player.userID); } else { Cooldown[player.userID]--; }
                        }
                    }
                }
            }
        }
        private void InitCooldown(BasePlayer player)
        {
            if(!Cooldown.ContainsKey(player.userID))
            {
                Cooldown.Add(player.userID, DEFAULT_COOLDOWN);
            }
        }
        void Loaded()
        {
            if (!permission.PermissionExists("magicsigns.able")) permission.RegisterPermission("magicsigns.able", this);
        }
        Timer COOLDOWN_TIMER = null;
        void OnServerInitialized()
        {
            WebObject = new GameObject("WebObject");
            UWeb = WebObject.AddComponent<UnityWeb>();
            timer.Once(5, () => PopulateImageList());
            COOLDOWN_TIMER = timer.Repeat(1, 0, () => CooldownTimer());
        }
    }
}