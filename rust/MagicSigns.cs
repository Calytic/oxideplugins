using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections;
using System.Linq;
namespace Oxide.Plugins
{
    [Info("MagicSigns", "Norn", 0.3, ResourceId = 1446)]
    [Description("Random signs.")]
    public class MagicSigns : RustPlugin
    {
        bool INIT = false;
        private readonly WebRequests scrapeQueue = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
        static GameObject WebObject;
        static UnityWeb UWeb;
        class QueueItem
        {
            public string url;
            public Signage sign;
            public BasePlayer sender;
            public QueueItem(string ur, BasePlayer se, Signage si)
            {
                url = ur;
                sender = se;
                sign = si;
            }
        }
        class UnityWeb : MonoBehaviour
        {
            internal static bool ConsoleLog = true;
            internal static string ConsoleLogMsg = "Player[{steam} {name}] loaded {id} image from {url}!";
            internal static int MaxActiveLoads = 3;
            static List<QueueItem> QueueList = new List<QueueItem>();
            static byte ActiveLoads = 0;

            public void Add(string url, BasePlayer player, Signage s)
            {
                QueueList.Add(new QueueItem(url, player, s));
                if (ActiveLoads < MaxActiveLoads)
                    Next();
            }

            void Next()
            {
                ActiveLoads++;
                QueueItem qi = QueueList[0];
                QueueList.RemoveAt(0);
                WWW www = new WWW(qi.url);
                StartCoroutine(WaitForRequest(www, qi));
            }

            IEnumerator WaitForRequest(WWW www, QueueItem info)
            {
                yield return www;
                BasePlayer player = info.sender;
                if (www.error == null)
                {
                      Signage sign = info.sign;
                      if (sign.textureID > 0U)
                      FileStorage.server.Remove(sign.textureID, FileStorage.Type.png, sign.net.ID);
                     sign.textureID = FileStorage.server.Store(www.bytes, FileStorage.Type.png, sign.net.ID, 0U);
                     sign.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                }
                ActiveLoads--;
                if (QueueList.Count > 0)
                    Next();
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
                if(src.Length >= 1 && src != "images/loading.gif" && src.EndsWith(".jpg"))
                {
                    src.Replace("http://4walled.cc/thumb/", "http://4walled.cc/src/");
                    ScrapedImages.Add(src);
                    count++;
                }
            }
            string tags = Config["Image", "Tags"].ToString(); if (tags.Length == 0) { tags = "Random Pool"; }
            string aspect = Config["Image", "Aspect"].ToString(); if (aspect.Length == 0) { aspect = "All"; }
            string safeforwork = Config["Image", "SafeForWork"].ToString(); if (safeforwork.Length == 0) { safeforwork = "All (18+)"; }
            if (count != 0) { Puts("Scraped " + count.ToString() + " images [ Tags: "+ tags + " ] [ Aspect: " + aspect + " ] [ Safe For Work: " + safeforwork + " ]"); if (!INIT) { INIT = true; } }
        }
        private void PopulateImageList()
        {
            INIT = false;
            ScrapedImages.Clear();
            scrapeQueue.EnqueueGet("http://4walled.cc/search.php?tags="+ System.Uri.EscapeDataString(Config["Image", "Tags"].ToString()) +"&board=&width_aspect="+ Config["Image", "Aspect"].ToString() +"&searchstyle=larger&sfw="+ Config["Image", "SafeForWork"].ToString() +"&search=random", (code, response) => ParseScrapeResponse(code, response), this);
        }
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ GENERAL SETTINGS ] ---

            Config["General", "AuthLevel"] = 2;

            Config["Image", "Aspect"] = "";
            Config["Image", "SafeForWork"] = "";
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
                    PrintToChat(player, "<color=yellow>ADMIN:</color> /ms <tags | aspect | sfw | <color=red>wipe</color>>");
                    if (Config["Image", "Tags"] != null)
                    {
                        string tags = null;
                        if(Config["Image", "Tags"].ToString().Length == 0){tags = "None. [<color=green>Random</color>]";}else{tags = Config["Image", "Tags"].ToString();}
                        if(tags != null) PrintToChat(player, "<color=yellow>Current Tags:</color> " + tags + ".");
                    }
                }
                else if (args[0] == "sfw")
                {
                    if (args.Length == 1)
                    {
                        if (Config["Image", "SafeForWork"] != null)
                        {
                            string aspect = null;
                            if (Config["Image", "SafeForWork"].ToString().Length == 0) { aspect = "All"; } else { aspect = Config["Image", "SafeForWork"].ToString(); }
                            if (aspect != null) PrintToChat(player, "<color=yellow>Safe For Work:</color> " + aspect + ".");
                        }
                        PrintToChat(player, "<color=yellow>USAGE:</color> /ms sfw <new_sfw>\n(e.g. /ms sfw <new_sfw> (\"<color=green>All</color>\" to return to all images).");
                    }
                    else
                    {
                        if (args[1].Length >= 1)
                        {
                            if (args[1].ToLower() == "all") { Config["Image", "SafeForWork"] = ""; PrintToChat(player, "You have updated Magic Signs sfw: <color=yellow>All Images</color>."); } else { Config["Image", "SafeForWork"] = args[1]; PrintToChat(player, "You have updated Magic Signs sfw: <color=yellow>" + Config["Image", "SafeForWork"].ToString() + "</color>."); }
                            SaveConfig();
                        }
                    }
                }
                else if (args[0] == "aspect")
                {
                    if (args.Length == 1)
                    {
                        if (Config["Image", "Aspect"] != null)
                        {
                            string aspect = null;
                            if (Config["Image", "Aspect"].ToString().Length == 0) { aspect = "All"; } else { aspect = Config["Image", "Aspect"].ToString(); }
                            if (aspect != null) PrintToChat(player, "<color=yellow>Current Aspect:</color> " + aspect + ".");
                        }
                        PrintToChat(player, "<color=yellow>USAGE:</color> /ms aspect <aspect>\n(e.g. /ms aspect <aspect> (etc \"<color=yellow>1920x177</color>\" or \"<color=green>All</color>\" to return to all ratios).");
                    }
                    else
                    {
                        if (args[1].Length >= 1)
                        {
                            if (args[1].ToLower() == "all") { Config["Image", "Aspect"] = ""; PrintToChat(player, "You have updated Magic Signs aspect: <color=yellow>All Ratios</color>."); } else { Config["Image", "Aspect"] = args[1]; PrintToChat(player, "You have updated Magic Signs aspect: <color=yellow>" + Config["Image", "Aspect"].ToString() + "</color>."); }
                            SaveConfig();
                        }
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
                BasePlayer player = planner.ownerPlayer;
                if (permission.UserHasPermission(player.userID.ToString(), "can.ms"))
                {
                    BaseEntity e = gameObject.ToBaseEntity();
                    if (!(e is BaseEntity) || player == null) { return; }
                    if (e.GetComponent<Signage>() != null)
                    {
                        if (!Cooldown.ContainsKey(player.userID))
                        {
                            int id = GetRandomInt(0, ScrapedImages.Count);
                            UWeb.Add(ScrapedImages[id], player, e.GetComponent<Signage>());
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
            if (!permission.PermissionExists("can.ms")) permission.RegisterPermission("can.ms", this);
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