using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Oxide.Core;

using UnityEngine;

namespace Oxide.Plugins
{

    [Info("Sign Artist", "Bombardir", "0.3.2", ResourceId = 992)]
    class SignArtist : RustPlugin
    {
        GameObject WebObject;
        UnityWeb UWeb;
        Dictionary<BasePlayer, float> CoolDowns;

        #region Unity WWW

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
            private Queue<QueueItem> QueueList = new Queue<QueueItem>();
            private byte ActiveLoads;
            private SignArtist SignArtist;
            private MemoryStream stream = new MemoryStream();

            private void Awake()
            {
                SignArtist = (SignArtist)Interface.Oxide.RootPluginManager.GetPlugin(nameof(SignArtist));
            }

            private void OnDestroy()
            {
                QueueList.Clear();
                SignArtist = null;
            }

            public void Add(string url, BasePlayer player, Signage s, bool raw)
            {
                QueueList.Enqueue(new QueueItem(url, player, s, raw));
                if (ActiveLoads < SignArtist.MaxActiveLoads)
                    Next();
            }

            void Next()
            {
                if (QueueList.Count <= 0) return;
                ActiveLoads++;
                StartCoroutine(WaitForRequest(QueueList.Dequeue()));
            }

            byte[] GetImageBytes(WWW www)
            {
                var tex = www.texture;
                byte[] img;
                if (tex.format == TextureFormat.ARGB32 && !SignArtist.ForceJPG)
                    img = tex.EncodeToPNG();
                else
                    img = tex.EncodeToJPG(SignArtist.JPGCompression);
                //player.ChatMessage(tex.format + " - " + tex + " - " + tex.EncodeToPNG().Length + " - " + tex.GetRawTextureData().Length + " - " + tex.EncodeToJPG(SignArtist.JPGCompression).Length);
                DestroyImmediate(tex);
                return img;
            }

            private void ClearStream()
            {
                stream.Position = 0;
                stream.SetLength(0);
            }

            IEnumerator WaitForRequest(QueueItem info)
            {
                using (var www = new WWW(info.url))
                {
                    yield return www;
                    if (SignArtist == null) yield break;
                    var player = info.sender;
                    if (www.error != null)
                    {
                        player.ChatMessage(string.Format(SignArtist.Error, www.error));
                        //SignArtist.CoolDowns.Remove(player);
                    }
                    else
                    {
                        if (www.size > SignArtist.MaxSize)
                        {
                            player.ChatMessage(SignArtist.SizeError);
                            //SignArtist.CoolDowns.Remove(player);
                            ActiveLoads--;
                            Next();
                            yield break;
                        }

                        var img = info.raw ? www.bytes : GetImageBytes(www);
                        if (img.Length <= SignArtist.MaxSize)
                        {
                            var sign = info.sign;
                            if (sign.textureID > 0U)
                                FileStorage.server.Remove(sign.textureID, FileStorage.Type.png, sign.net.ID);
                            ClearStream();
                            stream.Write(img, 0, img.Length);
                            sign.textureID = FileStorage.server.Store(stream, FileStorage.Type.png, sign.net.ID);
                            ClearStream();
                            sign.SendNetworkUpdate();
                            Interface.Oxide.CallHook("OnSignUpdated", sign, player);
                            player.ChatMessage(SignArtist.Loaded);

                            if (SignArtist.ConsoleLog)
                                ServerConsole.PrintColoured(System.ConsoleColor.DarkYellow, string.Format(SignArtist.ConsoleLogMsg, player.userID, player.displayName, sign.textureID, info.url));
                            //Resources.UnloadUnusedAssets();
                        }
                        else
                        {
                            player.ChatMessage(SignArtist.SizeError);
                            //SignArtist.CoolDowns.Remove(player);
                        }
                    }
                    ActiveLoads--;
                    Next();
                }
            }
        }

        #endregion

        [ConsoleCommand("sil")]
        void ccmdSil(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            sil(arg.Player(), string.Empty, arg.Args ?? new string[0]);
        }

        #region Chat Commands

        [ChatCommand("sil")]
        void sil(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.ChatMessage(Syntax);
                return;
            }

            if (!HasPerm(player, "signartist.url"))
            {
                player.ChatMessage(NoPerm);
                return;
            }

            float cd;
            if (CoolDowns.TryGetValue(player, out cd) && cd > Time.realtimeSinceStartup && !HasPerm(player, "signartist.cd"))
            {
                player.ChatMessage(string.Format(CooldownMsg, ToReadableString(cd - Time.realtimeSinceStartup)));
                return;
            }

            RaycastHit hit;
            Signage sign = null;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, MaxDist))
                sign = hit.transform.GetComponentInParent<Signage>();

            if (sign == null)
            {
                player.ChatMessage(NoSignFound);
                return;
            }

            if (!sign.CanUpdateSign(player) && !HasPerm(player, "signartist.owner"))
            {
                player.ChatMessage(NotYourSign);
                return;
            }

            var raw = args.Length > 1 && args[1].Equals("raw", StringComparison.OrdinalIgnoreCase);
            if (raw && !HasPerm(player, "signartist.raw"))
            {
                player.ChatMessage(NoPerm);
                return;
            }
            UWeb.Add(args[0], player, sign, raw);
            player.ChatMessage(AddedToQueue);
            if (UrlCooldown > 0)
                CoolDowns[player] = Time.realtimeSinceStartup + UrlCooldown;
        }

        [ConsoleCommand("silt")]
        void ccmdSilt(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            silt(arg.Player(), string.Empty, arg.Args ?? new string[0]);
        }

        [ChatCommand("silt")]
        void silt(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.ChatMessage(Syntax);
                return;
            }

            if (!HasPerm(player, "signartist.url"))
            {
                player.ChatMessage(NoPerm);
                return;
            }

            float cd;
            if (CoolDowns.TryGetValue(player, out cd) && cd > Time.realtimeSinceStartup && !HasPerm(player, "signartist.cd"))
            {
                player.ChatMessage(string.Format(CooldownMsg, ToReadableString(cd - Time.realtimeSinceStartup)));
                return;
            }

            RaycastHit hit;
            Signage sign = null;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, MaxDist))
                sign = hit.transform.GetComponentInParent<Signage>();

            if (sign == null)
            {
                player.ChatMessage(NoSignFound);
                return;
            }

            if (!sign.CanUpdateSign(player) && !HasPerm(player, "signartist.owner"))
            {
                player.ChatMessage(NotYourSign);
                return;
            }

            var raw = args.Length > 1 && args[1].Equals("raw", StringComparison.OrdinalIgnoreCase);
            if (raw && !HasPerm(player, "signartist.raw"))
            {
                player.ChatMessage(NoPerm);
                return;
            }
            string txt = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(args[0])).TrimEnd('=');
            int textSize = 80;
            string txtClr = "000";
            string bg = "0FFF";
            if (args.Length > 2) int.TryParse(args[2], out textSize);
            if (args.Length > 3) txtClr = args[3];
            if (args.Length > 4) bg = args[4];
            var width = (int)Math.Round(100 * sign.bounds.size.x * .9);
            var height = (int)Math.Round(100 * sign.bounds.size.y * .9);
            var url = $"http://placeholdit.imgix.net/~text?fm=png32&txtsize={textSize}&txt64={txt}&w={width}&h={height}&txtclr={txtClr}&bg={bg}";
            UWeb.Add(url, player, sign, raw);
            SendReply(player, AddedToQueue);
            if (UrlCooldown > 0)
                CoolDowns[player] = Time.realtimeSinceStartup + UrlCooldown;
        }

        #endregion

        #region Config | Init | Unload

        float MaxDist = 2f;
        float UrlCooldown = 180f;
        uint MaxSize = 2048U;
        byte JPGCompression = 85;
        bool ForceJPG = false;
        string NoPerm = "You don't have permission to use this command!";
        string Syntax = "Syntax: /sil <URL> | /sil s <number>";
        string NoSignFound = "You need to look/get closer to a sign!";
        string NotYourSign = "You can't change this sign! (protected by tool cupboard)";
        string CooldownMsg = "You have recently used this command! You need to wait: {time}";
        string AddedToQueue = "Your picture was added to load queue!";
        string Loaded = "Image was loaded to Sign!";
        string Error = "Image loading fail! Error: {error}";
        string NotExists = "File with this name not exists in storage folder!";
        string SizeError = "This file is too large. Max size: {size}KB";
        bool ConsoleLog = true;
        string ConsoleLogMsg = "Player[{steam} {name}] loaded {id} image from {url}!";
        int MaxActiveLoads = 3;


        void LoadDefaultConfig()
        {
        }

        void OnServerInitialized()
        {
            permission.RegisterPermission("signartist.url", this);
            permission.RegisterPermission("signartist.raw", this);
            permission.RegisterPermission("signartist.owner", this);
            permission.RegisterPermission("signartist.cd", this);

            CheckCfg("Log url console", ref ConsoleLog);
            CheckCfg("Log format", ref ConsoleLogMsg);
            CheckCfg("Max active uploads", ref MaxActiveLoads);
            CheckCfg("Max sign detection distance", ref MaxDist);
            CheckCfg("Max file size(KB)", ref MaxSize);
            CheckCfg("Command cooldown after url", ref UrlCooldown);
            CheckCfg("Command cooldown msg", ref CooldownMsg);
            CheckCfg("NoPermission", ref NoPerm);
            CheckCfg("Syntax", ref Syntax);
            CheckCfg("No sign", ref NoSignFound);
            CheckCfg("Not your sign", ref NotYourSign);
            CheckCfg("Added to queue", ref AddedToQueue);
            CheckCfg("Loaded", ref Loaded);
            CheckCfg("Not Exists", ref NotExists);
            CheckCfg("Error", ref Error);
            CheckCfg("JPGCompression", ref JPGCompression);
            CheckCfg("ForceJPG", ref ForceJPG);
            SaveConfig();

            // Small performance improvements
            ConsoleLogMsg = "[Sign Artist]" + ConsoleLogMsg
                .Replace("{steam}", "{0}")
                .Replace("{name}", "{1}")
                .Replace("{id}", "{2}")
                .Replace("{url}", "{3}");
            Error = Error.Replace("{error}", "{0}");

            CooldownMsg = CooldownMsg.Replace("{time}", "{0}");

            SizeError = SizeError.Replace("{size}", MaxSize.ToString());
            // ----------------------------- //

            MaxSize *= 1024;

            WebObject = new GameObject("WebObject");
            UWeb = WebObject.AddComponent<UnityWeb>();
            CoolDowns = new Dictionary<BasePlayer, float>();
        }

        void Unload()
        {
            UnityEngine.Object.Destroy(WebObject);
            UWeb = null;
            CoolDowns = null;
        }

        #endregion

        #region Util methods

        void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] == null)
                Config[Key] = var;
            else
                try
                {
                    var = (T) Convert.ChangeType(Config[Key], typeof (T));
                }
                catch
                {
                    Config[Key] = var;
                }
        }

        bool HasPerm(BasePlayer p, string pe) => permission.UserHasPermission(p.UserIDString, pe);

        static string ToReadableString(float seconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(seconds).Duration();
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Days > 0 ? $"{span.Days:0} day{(span.Days == 1 ? string.Empty : "s")}, " : string.Empty,
                span.Hours > 0 ? $"{span.Hours:0} hour{(span.Hours == 1 ? string.Empty : "s")}, " : string.Empty,
                span.Minutes > 0 ? $"{span.Minutes:0} minute{(span.Minutes == 1 ? string.Empty : "s")}, " : string.Empty,
                span.Seconds > 0 ? $"{span.Seconds:0} second{(span.Seconds == 1 ? string.Empty : "s")}" : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }

        #endregion
    }
}
