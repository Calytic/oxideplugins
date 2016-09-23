using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SharedDoors", "dbteku", 0.3  )]
    [Description("Making sharing doors easier.")]
    class SharedDoors : RustPlugin
    {
        private const string RUST_IO = "clans";
        private const string CLANS_NAME = "Clans";
        private const string RUST_CLANS_LOADED_AFTER = "Rust Clans has been loaded. SharedDoors now hooking.";
        private static Library rustIO;
        [PluginReference("Clans")]
        private Plugin Clans;
        private static Plugin ClansInstance;

        void OnServerInitialized()
        {
            ClansInstance = Clans;
        }

        void OnPluginLoaded(Plugin name)
        {
            if(name.Name == CLANS_NAME)
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

        bool CanUseDoor(BasePlayer player, BaseLock door)
        {
            bool canUse = false;
            canUse = new DoorAuthorizer(door, player).canOpen();
            return canUse;
        }

        protected static Plugin GetClans()
        {
            return ClansInstance;
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
                if(door.BaseDoor is CodeLock)
                {
                    if (door.GetWhiteList().Count > 0)
                    {
                        this.OriginalPlayerID = door.GetWhiteList()[0];
                    }else
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

    }
}
