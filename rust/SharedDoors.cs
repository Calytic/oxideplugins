using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SharedDoors", "dbteku", 0.6)]
    [Description("Making sharing doors easier.")]
    class SharedDoors : CovalencePlugin
    {
        private const string RUST_IO = "clans";
        private const string CLANS_NAME = "Clans";
        private const string RUST_CLANS_LOADED_AFTER = "Rust Clans has been loaded. SharedDoors now hooking.";
        private const string MASTER_PERM = "SharedDoors.Master";
        private static Library rustIO;
        [PluginReference("Clans")]
        private Plugin Clans;
        private static Plugin ClansInstance;
        private MasterKeyHolders holders;

        void OnServerInitialized()
        {
                        permission.RegisterPermission(MASTER_PERM,this);
            ClansInstance = Clans;
            holders = new MasterKeyHolders();
        }

        void OnPluginLoaded(Plugin name)
        {
            if (name.Name == CLANS_NAME)
            {
                Puts(RUST_CLANS_LOADED_AFTER);
                Clans = name;
                ClansInstance = name;
            }
        }

        void OnPluginUnloaded(Plugin name)
        {
            if (name.Name == CLANS_NAME)
            {
                Puts(RUST_CLANS_LOADED_AFTER);
                Clans = null;
                ClansInstance = null;
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            IPlayer iPlayer = covalence.Players.FindPlayerById(player.userID.ToString());
            if (player.IsAdmin() || iPlayer.HasPermission(MASTER_PERM))
            {
                holders.AddMaster(player.userID.ToString());
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            IPlayer iPlayer = covalence.Players.FindPlayerById(player.userID.ToString());
            if (player.IsAdmin() || iPlayer.HasPermission(MASTER_PERM))
            {
                holders.RemoveMaster(player.userID.ToString());
            }
        }

        bool CanUseLock(BasePlayer player, BaseLock door)
        {
            IPlayer iPlayer = covalence.Players.FindPlayerById(player.userID.ToString());
            bool canUse = false;
            if((player.IsAdmin() && holders.IsAKeyMaster(player.userID.ToString())) || (iPlayer.HasPermission(MASTER_PERM) && holders.IsAKeyMaster(player.userID.ToString())))
            {
                canUse = true;
            }else
            {
                canUse = new DoorAuthorizer(door, player).canOpen();
            }

            return canUse;
        }

        protected static Plugin GetClans()
        {
            return ClansInstance;
        }

        [Command("sd")]
        void SharedDoorsCommand(IPlayer player, string command, string[] args)
        {
            
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "help")
                {
                    PlayerResponder.NotifyUser(player, "Master Mode Toggle: /sd masterMode");
                }
                else if (args[0].ToLower() == "mastermode" || args[0].ToLower() == "mm")
                {
                    if (player.IsAdmin || player.HasPermission(MASTER_PERM))
                    {
                        if (holders.HasMaster(player.Id))
                        {
                            holders.ToggleMasterMode(player.Id);
                            if (holders.IsAKeyMaster(player.Id))
                            {
                                PlayerResponder.NotifyUser(player, "Master Mode Enabled. You can now open all doors and chests.");
                            }
                            else
                            {
                                PlayerResponder.NotifyUser(player, "Master Mode Disabled. You can no longer open all doors and chests.");
                            }
                        }else
                        {
                            holders.AddMaster(player.Id);
                            holders.GiveMasterKey(player.Id);
                            PlayerResponder.NotifyUser(player, "Master Mode Enabled. You can now open all doors and chests.");
                        }

                    }else
                    {
                        PlayerResponder.NotifyUser(player, "Master Mode Not Available. You don't have permission to use this command.");
                    }
                }
            }
            else
            {
                PlayerResponder.NotifyUser(player, "Master Mode Toggle: /sd masterMode");
            }
        }

        private class PlayerResponder : RustPlugin
        {
            private const String PREFIX = "<color=#00ffffff>[</color><color=#ff0000ff>SharedDoors</color><color=#00ffffff>]</color>";


            public static void NotifyUser(IPlayer player, String message)
            {
               player.Message(PREFIX + " " + message);
            }

        }

        /*
         * 
         * Door Handler Class
         * 
         * */
        private class DoorAuthorizer
        {

            readonly FieldInfo whiteListField = typeof(CodeLock).GetField("whitelistPlayers", (BindingFlags.Instance | BindingFlags.NonPublic));
            readonly FieldInfo _codeField = typeof(CodeLock).GetField("code", (BindingFlags.Instance | BindingFlags.NonPublic));
            public BaseLock BaseDoor { get; protected set; }
            public BasePlayer Player { get; protected set; }
            private ToolCupboardChecker checker;
            private RustIOHandler handler;

            public DoorAuthorizer(BaseLock door, BasePlayer player)
            {
                this.BaseDoor = door;
                this.Player = player;
                checker = new ToolCupboardChecker(Player);
                handler = new RustIOHandler(this);
            }

            public bool canOpen()
            {
                bool canUse = false;
                if (BaseDoor.IsLocked())
                {
                    if (BaseDoor is CodeLock)
                    {
                        CodeLock codeLock = (CodeLock)BaseDoor;
                        canUse = canOpenCodeLock(codeLock, Player);
                    }
                    else if (BaseDoor is KeyLock)
                    {
                        KeyLock keyLock = (KeyLock)BaseDoor;
                        canUse = canOpenKeyLock(keyLock, Player);
                    }
                }
                else
                {
                    canUse = true;
                }

                return canUse;
            }

            public List<ulong> GetWhiteList()
            {
                return (List<ulong>)whiteListField.GetValue(BaseDoor);
            }

            private bool canOpenCodeLock(CodeLock door, BasePlayer player)
            {
                bool canUse = false;
                //Have to do this due to Facepunch not overriding their own method called HasLockPermission()
                var whitelist = (List<ulong>)whiteListField.GetValue(door);
                if (whitelist.Contains(player.userID))
                {
                    canUse = true;
                }
                else
                {
                    canUse = (player.CanBuild() && checker.isPlayerAuthorized());
                    if (canUse && handler.clansAvailable())
                    {
                        canUse = handler.isInClan(player);
                    }
                }

                playSound(canUse, door, player);
                return canUse;
            }

            private bool canOpenKeyLock(KeyLock door, BasePlayer player)
            {
                bool canUse = false;

                canUse = door.HasLockPermission(player) || (player.CanBuild() && checker.isPlayerAuthorized());

                return canUse;
            }

            private void playSound(bool canUse, CodeLock door, BasePlayer player)
            {
                if (canUse)
                {
                    Effect.server.Run(door.effectUnlocked.resourcePath, player.transform.position, Vector3.zero, null, false);
                }
                else
                {
                    Effect.server.Run(door.effectDenied.resourcePath, player.transform.position, Vector3.zero, null, false);
                }
            }
        }


        /*
         * 
         * Tool Cupboard Tool
         * 
         * */

        private class ToolCupboardChecker
        {

            public BasePlayer Player { get; protected set; }

            public ToolCupboardChecker(BasePlayer player)
            {
                this.Player = player;
            }

            public bool isPlayerAuthorized()
            {
                bool isIn = false;
                BuildPrivilegeTrigger trigger = Player.FindTrigger<BuildPrivilegeTrigger>();
                if (trigger != null)
                {
                    isIn = trigger.privlidgeEntity.IsAuthed(Player);
                }
                return isIn;
            }
        }

        /*
         * 
         * RustIO Handler
         * 
         * */

        private class RustIOHandler
        {

            private const string GET_CLAN_OF_PLAYER = "GetClanOf";
            private const string GET_CLAN = "GetClan";
            private const string MEMBERS = "members";
            public Plugin Clans { get; protected set; }
            public ulong OriginalPlayerID { get; protected set; }
            public DoorAuthorizer Door { get; protected set; }

            public RustIOHandler(DoorAuthorizer door)
            {
                this.Clans = SharedDoors.GetClans();
                if (door.BaseDoor is CodeLock)
                {
                    if (door.GetWhiteList().Count > 0)
                    {
                        this.OriginalPlayerID = door.GetWhiteList()[0];
                    }
                    else
                    {
                        this.OriginalPlayerID = 0;
                    }
                }
                this.Door = door;
            }

            public bool isInClan(BasePlayer player)
            {
                bool isInClan = false;
                if (clansAvailable())
                {
                    object obj = Clans.CallHook(GET_CLAN_OF_PLAYER, new object[] { OriginalPlayerID });
                    if (obj != null)
                    {
                        String clanName = obj.ToString();
                        object clan = Clans.CallHook(GET_CLAN, new object[] { clanName });
                        if (clan != null)
                        {
                            JObject jObject = JObject.FromObject(clan);
                            JArray members = (JArray)jObject.GetValue(MEMBERS);
                            string[] memberIds = members.ToObject<string[]>();
                            isInClan = (memberIds.Contains(player.userID.ToString()));
                        }
                    }
                }

                return isInClan;
            }

            public bool clansAvailable()
            {
                return this.Clans != null;
            }

        }

        /*
       * 
       * Admin Mode Handler
       * 
       * */

        private class MasterKeyHolders 
        {

            private Dictionary<string, PlayerSettings> keyMasters;

            public MasterKeyHolders()
            {
                keyMasters = new Dictionary<string, PlayerSettings>();
            }

            public void AddMaster(String id)
            {
                this.keyMasters.Add(id, new PlayerSettings(false));
            }

            public void RemoveMaster(String id)
            {
                this.keyMasters.Remove(id);
            }

            public void GiveMasterKey(String id)
            {
                PlayerSettings settings = null;
                bool exists = keyMasters.TryGetValue(id, out settings);
                if (exists)
                {
                    settings.IsMasterKeyHolder = true;
                }
            }

            public void RemoveMasterKey(String id)
            {
                PlayerSettings settings = null;
                bool exists = keyMasters.TryGetValue(id, out settings);
                if (exists)
                {
                    settings.IsMasterKeyHolder = false;
                }
            }

            public bool IsAKeyMaster(String id)
            {
                bool isKeyMaster = false;
                PlayerSettings settings = null;
                bool exists = keyMasters.TryGetValue(id, out settings);
                if (exists)
                {
                    isKeyMaster = settings.IsMasterKeyHolder;
                }
                return isKeyMaster;
            }

            public void ToggleMasterMode(String id)
            {
                PlayerSettings settings = null;
                bool exists = keyMasters.TryGetValue(id, out settings);
                if (exists)
                {
                    settings.ToggleMasterMode();    
                }
            }

            public bool HasMaster(string id)
            {
                return keyMasters.ContainsKey(id);
            }
        }


        /*
       * 
       * Player Settings
       * 
       * */

        private class PlayerSettings
        {
            public bool IsMasterKeyHolder { get; set; }

            public PlayerSettings(bool isMasterKeyHolder)
            {
                IsMasterKeyHolder = isMasterKeyHolder;
            }

            public void ToggleMasterMode()
            {
                IsMasterKeyHolder = !IsMasterKeyHolder;
            }

        }
    }
}
