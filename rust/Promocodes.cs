using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Promocodes", "LaserHydra", "2.1.2", ResourceId = 1471)]
    [Description("Set up promocodes which run a command on the player who redeemed a code")]
    class Promocodes : RustPlugin
    {
        static List<Promocode> promocodes = new List<Promocode>();

        #region Classes
        
        class Promocode
        {
            public List<object> availableCodes = new List<object>();
            public List<object> commands = new List<object>();

            public Promocode()
            {
            }
            
            internal Promocode(string command, int generateCodes = 10)
            {
                this.commands.Add(command);

                for (int i = 1; i <= generateCodes; i++)
                    availableCodes.Add(GenerateCode());
            }
            
            internal Promocode(object obj)
            {
                if (obj is Dictionary<string, object>)
                {
                    Dictionary<string, object> dic = obj as Dictionary<string, object>;

                    Console.WriteLine(string.Join(Environment.NewLine, (from kvp in dic select $"{kvp.Key}: {kvp.Value}").ToArray()));

                    availableCodes = dic["availableCodes"] as List<object>;
                    commands = dic["commands"] as List<object>;
                }
                else if (obj is Promocode)
                {
                    Promocode code = (Promocode) obj;

                    this.commands = code.commands;
                    this.availableCodes = code.availableCodes;
                }
            }

            internal static Promocode Get(string code) => promocodes.Find((c) => c.availableCodes.Contains(code));
            
            internal static void Redeem(string code, BasePlayer player)
            {
                Promocode promocode = Get(code);

                foreach(string command in promocode.commands)
                    ConsoleSystem.Run.Server.Normal(command.Replace("{steamid}", player.UserIDString).Replace("{name}", player.displayName));
                
                promocode.availableCodes.Remove(code);
            }

            internal static string GenerateCode()
            {
                var promocode = new List<string>();

                for(int blocks = 1; blocks <= 5; blocks++)
                {
                    string block = string.Empty;

                    for(int chars = 1; chars <= 5; chars++)
                    {
                        char[] charArray = "ABCDEFGHIJKLMNOPQRSTUVXYZabcdefghijklmnopqrstuvwxyz1234567890".ToCharArray();

                        block += charArray[UnityEngine.Random.Range(0, charArray.Count() - 1)];
                    }

                    promocode.Add(block);
                }

                return string.Join("-", promocode.ToArray());
            }
        }

        #endregion

        #region Plugin General

        ////////////////////////////////////////
        ///     Plugin Related Hooks
        ////////////////////////////////////////

        void Loaded()
        {
#if !RUST
            throw new NotSupportedException("This plugin or the version of this plugin does not support this game!");
#endif

            LoadConfig();
            LoadMessages();

            promocodes = (from promocode in GetConfig(new List<object>(), "Promocodes") select new Promocode(promocode)).ToList();
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Invalid Code", "The entered code is invalid!"},
                {"Code Redeemed", "You successfully redeemed the code."},
            }, this);
        }
        
        void LoadConfig()
        {
            SetConfig("Promocodes", new List<object> { new Promocode("oxide.usergroup add {steamid} vip") });

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");
        #endregion

        #region Commands

        [ChatCommand("redeem")]
        void cmdRedeem(BasePlayer player, string cmd, string[] args)
        {
            if(args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /redeem <code>");
                return;
            }

            if(Promocode.Get(args[0]) == null)
            {
                SendChatMessage(player, GetMsg("Invalid Code", player.userID));
                return;
            }

            Promocode.Redeem(args[0], player);
            SendChatMessage(player, GetMsg("Code Redeemed", player.userID));

            Puts($"{player.displayName} has redeemed the code {args[0]}");

            SaveConfig();
        }

        #endregion

        #region General Methods

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString<T>(List<T> list, int first, string seperator) => string.Join(seperator, (from item in list select item.ToString()).Skip(first).ToArray());

        static T TryConvert<S, T>(S source, T converted)
        {
            try
            {
                return (T) Convert.ChangeType(source, typeof(T));
            }
            catch(Exception)
            {
                return default(T);
            }
        }

        ////////////////////////////////////////
        ///     Config Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        ////////////////////////////////////////
        ///     Message Related
        ////////////////////////////////////////

        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());

        ////////////////////////////////////////
        ///     Chat Related
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        #endregion
    }
}
