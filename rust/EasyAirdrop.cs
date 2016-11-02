using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Easy Airdrop", "LaserHydra", "3.2.2", ResourceId = 860)]
    [Description("Easy Airdrop")]
    class EasyAirdrop : RustPlugin
    {
        ////////////////////////////////////////
        ///     On Plugin Loaded
        ////////////////////////////////////////

        void Loaded()
        {
            permission.RegisterPermission("easyairdrop.call", this);
            permission.RegisterPermission("easyairdrop.call.player", this);
            permission.RegisterPermission("easyairdrop.call.position", this);
            permission.RegisterPermission("easyairdrop.call.mass", this);

            LoadMessages();
            LoadConfig();
        }

        ////////////////////////////////////////
        ///     Config Handling
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Settings", "Broadcast to Chat", true);
            SetConfig("Settings", "Send to Console", true);
            SaveConfig();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>{
                { "Chat Message", "<color=red>{player}</color> has called an airdrop."}, 
                { "Console Message", "{player} has called an airdrop. {location}"},
                { "Massdrop Chat Message", "<color=red>{player}</color> has called <color=red>{amount}</color> airdrops."},
                { "Massdrop Console Message", "{player} has called {amount} airdrops."}
            }, this);
        }

        string msg(string key, string id = null)
        {
            return lang.GetMessage(key, this, id);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Generating new config file...");
        }

        ////////////////////////////////////////
        ///     Commands
        ////////////////////////////////////////

        [ConsoleCommand("airdrop")]
        void ccmdAirdrop(ConsoleSystem.Arg arg)
        {
            RunAsChatCommand(arg, cmdAirdrop);
        }

        [ConsoleCommand("massdrop")]
        void ccmdMassdrop(ConsoleSystem.Arg arg)
        {
            RunAsChatCommand(arg, cmdMassdrop);
        }

        [ChatCommand("airdrop")]
        void cmdAirdrop(BasePlayer player, string cmd, string[] args)
        {
            if(args.Length == 0)
            {
                SendChatMessage(player, "/airdrop <player|pos|random>");
                return;
            }

            if(args.Length > 0)
            {
                switch(args[0].ToLower())
                {
                    case "player":

                        if (!HasPermission(player, "player"))
                        {
                            SendChatMessage(player, "You don't have permission to use this command.");
                            return;
                        }

                        if (args.Length != 2)
                        {
                            SendChatMessage(player, "Syntax: /airdrop player <player>");
                            return;
                        }

                        BasePlayer target = GetPlayer(args[1], player, null);
                        if (target == null) return;

                        SpawnPlayerAirdrop(player, target);

                        break;

                    case "pos":

                        if (!HasPermission(player, "position"))
                        {
                            SendChatMessage(player, "You don't have permission to use this command.");
                            return;
                        }

                        if (args.Length != 3)
                        {
                            SendChatMessage(player, "Syntax: /airdrop pos <x> <z>");
                            return;
                        }

                        float x;
                        float y = UnityEngine.Random.Range(200, 300);
                        float z;

                        try
                        {
                            x = Convert.ToSingle(args[1]);
                            z = Convert.ToSingle(args[2]);
                        }
                        catch (FormatException ex)
                        {
                            SendChatMessage(player, "Arguments must be numbers!");
                            return;
                        }

                        SpawnAirdrop(new Vector3(x, y, z));
                        AnnounceAirdrop(player, new Vector3(x, y, z));

                        break;

                    case "random":

                        if (!HasPermission(player))
                        {
                            SendChatMessage(player, "You don't have permission to use this command.");
                            return;
                        }
                        
                        SpawnRandomAirdrop(player);

                        break;

                    default:

                        break;
                }
            }
        }

        [ChatCommand("massdrop")]
        void cmdMassdrop(BasePlayer player, string cmd, string[] args)
        {
            if(!HasPermission(player, "mass"))
            {
                SendChatMessage(player, "You don't have permission to use this command.");
                return;
            }

            if(args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /massdrop <count>");
                return;
            }

            int amount;

            try
            {
                amount = Convert.ToInt32(args[0]);
            }
            catch(FormatException ex)
            {
                SendChatMessage(player, "Argument must be a number!");
                return;
            }

            SpawnMassdrop(player, amount);
        }

        ////////////////////////////////////////
        ///     Airdrop Related
        ////////////////////////////////////////
        
        void SpawnPlayerAirdrop(BasePlayer player, BasePlayer target)
        {
            SpawnAirdrop(target.transform.position);

            AnnounceAirdrop(player, target.transform.position);
        }

        void SpawnMassdrop(BasePlayer player, int amount)
        {
            List<Vector3> locations = new List<Vector3>();

            for (int i = 1; i <= amount; i++)
            {
                Vector3 location = GetRandomVector();
                locations.Add(location);

                SpawnAirdrop(location);
            }

            AnnounceAirdrop(player, locations.ToArray());
        }

        void SpawnRandomAirdrop(BasePlayer player)
        {
            Vector3 position = GetRandomVector();
            SpawnAirdrop(position);

            AnnounceAirdrop(player, position);
        }

        void SpawnAirdrop(Vector3 position)
        {
            BaseEntity planeEntity = GameManager.server.CreateEntity("assets/prefabs/npc/cargo plane/cargo_plane.prefab", new Vector3(), new Quaternion(1f, 0f, 0f, 0f));

            if (planeEntity != null)
            {
                CargoPlane plane = planeEntity.GetComponent<CargoPlane>();

                plane.InitDropPosition(position);
                planeEntity.Spawn();
            }
        }

        ////////////////////////////////////////
        ///     Permission Related
        ////////////////////////////////////////

        bool HasPermission(BasePlayer player, string perm = "")
        {
            if (player == null)
                return true;

            if (string.IsNullOrEmpty(perm) && permission.UserHasPermission(player.UserIDString, "easyairdrop.call"))
                return true;
            else
            {
                if (permission.UserHasPermission(player.UserIDString, "easyairdrop.call." + perm))
                    return true;
            }

            return false;
        }

        ////////////////////////////////////////
        ///     Vector Related
        ////////////////////////////////////////

        Vector3 GetRandomVector()
        {
            float max = ConVar.Server.worldsize / 2;

            float x = UnityEngine.Random.Range(max * (-1), max);
            float y = UnityEngine.Random.Range(200, 300);
            float z = UnityEngine.Random.Range(max * (-1), max);

            return new Vector3(x, y, z);
        }

        ////////////////////////////////////////
        ///     Console Command Handling
        ////////////////////////////////////////

        void RunAsChatCommand(ConsoleSystem.Arg arg, Action<BasePlayer, string, string[]> command)
        {
            if (arg == null) return;

            BasePlayer player = arg?.connection?.player == null ? arg?.connection?.player as BasePlayer : null;
            string cmd = arg.cmd?.name ?? "unknown";
            string[] args = arg.HasArgs() ? arg.Args : new string[0];

            command(player, cmd, args);
        }

        ////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////

        BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix = null)
        {
            List<string> foundPlayers =
                (from player in BasePlayer.activePlayerList
                 where player.displayName.ToLower().Contains(searchedPlayer.ToLower())
                 select player.displayName).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    if(prefix == null)
                        SendChatMessage(executer, "The player can not be found.");
                    else
                        SendChatMessage(executer, prefix, "The player can not be found.");

                    break;

                case 1:
                    return BasePlayer.Find(foundPlayers[0]);

                default:
                    string players = ListToString(foundPlayers, 0, ", ");

                    if (prefix == null)
                        SendChatMessage(executer, "Multiple matching players found: \n" + players);
                    else
                    SendChatMessage(executer, prefix, "Multiple matching players found: \n" + players);

                    break;
            }

            return null;
        }

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        ////////////////////////////////////////
        ///     Config Setup
        ////////////////////////////////////////
        
        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        ////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => PrintToChat(msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null)
        {
            if (player != null)
                SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);
            else
                Puts(msg == null ? prefix : msg);
        }

        void AnnounceAirdrop(BasePlayer player, params Vector3[] locations)
        {
            string name = player == null ?  "Server" : player.displayName;
            string loc = string.Empty;
            string amount = locations.Length.ToString();

            if (locations.Length > 1)
                loc = "multiple locations";
            else
                loc = locations[0].ToString();

            string chatMessage = msg("Chat Message").Replace("{player}", name).Replace("{location}", loc);
            string consoleMessage = msg("Console Message").Replace("{player}", name).Replace("{location}", loc);

            if(locations.Length > 1)
            {
                chatMessage = msg("Massdrop Chat Message").Replace("{player}", name).Replace("{location}", loc).Replace("{amount}", amount);
                consoleMessage = msg("Massdrop Console Message").Replace("{player}", name).Replace("{location}", loc).Replace("{amount}", amount);
            }

            if ((bool) Config["Settings", "Broadcast to Chat"])
                BroadcastChat(chatMessage);

            if ((bool) Config["Settings", "Send to Console"])
                Puts(consoleMessage);
        }
    }
}
