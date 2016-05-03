using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;                      //DateTime
using System.Collections.Generic;  //Required for Whilelist
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("PvXSelector", "Alphawar", "0.6.0", ResourceId = 1817)]
    [Description("Player vs x Selector")]
    class PvXselector : RustPlugin
    {
        [PluginReference]
        Plugin EntityOwner;

        StoredData storedData;
        class StoredData
        {
            public Dictionary<int, ulong> ticketCount = new Dictionary<int, ulong>();
            public Hash<ulong, PlayerInfo> PvXData = new Hash<ulong, PlayerInfo>();

            public StoredData()
            {
            }
        }
        class PlayerInfo
        {
            public ulong UserId;
            public string Name;
            public bool selected;
            public bool PvP;
            public bool changeRequested;
            public string reason;

            public PlayerInfo()
            {
            }

            public PlayerInfo(BasePlayer _player, bool _pvp, bool _sel, bool _req, string _rsn)
            {
                UserId = _player.userID;
                Name = _player.displayName;
                PvP = _pvp;
                selected = _sel;
                changeRequested = _req;
                reason = _rsn;
            }
        }
        
        void Loaded()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("PvXselector");
            lang.RegisterMessages(messages, this);
            permissionHandle();
            LoadVariables();
        }

        void Unloaded()
        {
            Interface.GetMod().DataFileSystem.WriteObject("PvXselector", storedData);
        }

        void OnPlayerInit(BasePlayer _player)
        {
            if (_player == null) return;

            if (_player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerInit(_player));
                return;
            }
            if (storedData.PvXData.ContainsKey(_player.userID)) return;

            storedData.PvXData.Add(_player.userID, new PlayerInfo { UserId = _player.userID, Name = _player.displayName, selected = false, PvP = false, changeRequested = false, reason = string.Empty });
            SelectorOverlay(_player);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Main Controlls ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        [ChatCommand("pvx")]
        void PvXCmd(BasePlayer _player, string cmd, string[] args)
        {
            if ((args == null || args.Length == 0)) return;
            if (args[0] == "change") SelectorOverlay(_player);
            if (args[0] == "ticket")
            {
                if (!IsAllowed(_player, "PvXSelector.admin", "you are not admin")) return;
                
                if (args.Length == 1)
                {
                    string[] _passcmd = { "ticket", "listcount" };
                    PvXFunction(_player, _passcmd);
                }
                else
                {
                    PvXFunction(_player, args);
                }
            }
            if (args[0] == "reason")
            {
                if (storedData.PvXData[_player.userID].changeRequested == true)
                {
                    if (args.Length >= 2)
                    {
                        storedData.PvXData[_player.userID].reason = args[1];
                    }
                }
            }
            if (args[0] == "debug")
            {

            }
        }

        [ConsoleCommand("PvXSelection")]
        void PvXSelection(ConsoleSystem.Arg arg)
        {
            if ((arg.Args.Length == 0) || (arg.Args.Length == 2)) return;
            BasePlayer _player = (BasePlayer)arg.connection.player;
            if (!(storedData.PvXData.ContainsKey(_player.userID)))
            {
                storedData.PvXData.Add(_player.userID, new PlayerInfo { UserId = _player.userID, Name = _player.displayName, selected = false, PvP = false, changeRequested = false, reason = string.Empty });
            }
            string cmdValue = arg.Args[0];
            Puts("Player Selected " + cmdValue);
            if (storedData.PvXData[_player.userID].selected == false)
            {
                selectPvX(_player, cmdValue);
            }
            else if (storedData.PvXData[_player.userID].changeRequested == true)
            {
                ChatMessageHandler(_player, lang.GetMessage("AlreadySubmitted", this, _player.UserIDString));
            }
            else if (storedData.PvXData[_player.userID].selected)
            {
                bool _result = changeRequested(_player, cmdValue);
                if (_result)
                {
                    int _id = GetNewID();
                    if (_id == 0)
                    {
                        Puts("Error: Not ID was returned, Check data file for odd requests");  //debug
                    }
                    else
                    {
                        storedData.ticketCount.Add(_id, _player.userID);
                        ChatMessageHandler(_player, lang.GetMessage("TicketSubmitted", this, _player.UserIDString));
                    }
                }
                if (_result == false)
                {
                    Puts("Error: changeRequested returned a false."); //debug
                }
            }
            Interface.Oxide.DataFileSystem.WriteObject("PvXselector", storedData);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Functions /////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            bool _result;
            if (entity is BasePlayer && hitinfo.Initiator is BasePlayer)
            {
                BasePlayer victim = (BasePlayer)entity;
                BasePlayer attacker = (BasePlayer)hitinfo.Initiator;
                if (attacker == victim) return;
                if (!(storedData.PvXData.ContainsKey(attacker.userID))){
                    string[] test = { "change" };
                    PvXCmd(attacker, "PvXCmd", test);
                    NullifyDamage(hitinfo);
                    return;}
                if (!(storedData.PvXData.ContainsKey(victim.userID))){
                    string[] test = { "change" };
                    PvXCmd(victim, "PvXCmd", test);
                    NullifyDamage(hitinfo);
                    return;}
                _result = Damageplayer(attacker, victim);
                if (_result == true) return;
                if(storedData.PvXData[victim.userID].PvP == false) ChatMessageHandler(attacker, lang.GetMessage("PvETarget", this, attacker.UserIDString));
                if (storedData.PvXData[attacker.userID].PvP == false) ChatMessageHandler(attacker, lang.GetMessage("PvEPlayer", this, attacker.UserIDString));
                NullifyDamage(hitinfo);
            }
            else if (((entity is BuildingBlock) || (entity is Door)) && (hitinfo.Initiator is BasePlayer))
            {
                BasePlayer attacker = (BasePlayer)hitinfo.Initiator;
                BaseEntity _target = entity;
                if (!(storedData.PvXData.ContainsKey(attacker.userID))){
                    string[] test = { "change" };
                    PvXCmd(attacker, "PvXCmd", test);
                    NullifyDamage(hitinfo);
                    return;}
                if (_target.OwnerID == attacker.userID) return;
                    _result = DamageEntity(attacker, _target);
                if (_result == true) return;
                if (storedData.PvXData[entity.OwnerID].PvP == false) ChatMessageHandler(attacker, lang.GetMessage("PvEStructure", this, attacker.UserIDString));
                if (storedData.PvXData[attacker.userID].PvP == false) ChatMessageHandler(attacker, lang.GetMessage("PvEPlayer", this, attacker.UserIDString));
                NullifyDamage(hitinfo);
            }
            else return;
        }
        

        void PvXFunction(BasePlayer _player, string[] args)
        {
            if (args[1] == "listcount")
            {
                ticketListCount(_player);
            }
            else if (args[1] == "list")
            {
                if (storedData.ticketCount.Count == 0)
                {
                    ChatMessageHandler(_player, lang.GetMessage("NoTicket", this, _player.UserIDString));
                    return;
                }
                else ticketListFunction(_player);
            }
            else if (args[1] == "detailed")
            {
                ticketDisplayFunction(_player);
            }
            else if ((args[1] == "accept") && (args.Length == 3))
            {
                int _selection = Convert.ToInt32(args[2]);
                if (storedData.ticketCount.ContainsKey(_selection))
                {
                    ulong _ticketUlong;
                    storedData.ticketCount.TryGetValue(_selection, out _ticketUlong);
                    if (storedData.PvXData[_ticketUlong].PvP == true)
                    {
                        storedData.PvXData[_ticketUlong].PvP = false;
                    }
                    else if (storedData.PvXData[_ticketUlong].PvP == false)
                    {
                        storedData.PvXData[_ticketUlong].PvP = true;
                    }
                    storedData.PvXData[_ticketUlong].changeRequested = false;
                    storedData.ticketCount.Remove(_selection);
                    Interface.GetMod().DataFileSystem.WriteObject("PvXselector", storedData);
                    ChatMessageHandler(_player, lang.GetMessage("TicketAccepted", this, _player.UserIDString));
                }
            }
            else if ((args[1] == "decline") && (args.Length == 3))
            {
                int _selection = Convert.ToInt32(args[2]);
                if (storedData.ticketCount.ContainsKey(_selection))
                {
                    storedData.ticketCount.Remove(_selection);
                    Interface.GetMod().DataFileSystem.WriteObject("PvXselector", storedData);
                    ChatMessageHandler(_player, lang.GetMessage("TicketRemoved", this, _player.UserIDString));
                }
            }
        }

        static void NullifyDamage(HitInfo hitinfo)
        {
            hitinfo.damageTypes = new DamageTypeList();
            hitinfo.DoHitEffects = false;
            hitinfo.HitMaterial = 0;
            hitinfo.PointStart = Vector3.zero;
        }

        void selectPvX(BasePlayer _player, string _selection)
        {
            bool _pvpSelection = _selection != "pve";
            storedData.PvXData[_player.userID].PvP = _pvpSelection;
            storedData.PvXData[_player.userID].selected = true;
        }

        bool changeRequested(BasePlayer _player, string _selection)
        {
            bool _pvpSelection = _selection != "pve";
            if (storedData.PvXData[_player.userID].PvP == _pvpSelection) return false;
            if (storedData.PvXData[_player.userID].changeRequested == true) return false;
            storedData.PvXData[_player.userID].changeRequested = true;
            storedData.PvXData[_player.userID].reason = lang.GetMessage("TicketDefaultReason", this, _player.UserIDString);
            Interface.GetMod().DataFileSystem.WriteObject("PvXselector", storedData);
            return true;
        }

        int GetNewID()
        {
            for (int _i = 1; _i <= 500; _i++)
            {
                if (storedData.ticketCount.ContainsKey(_i))
                {
                    Puts("Key {0} exists", _i); //debug
                }
                else
                {

                    Puts("Key {0} doesnt exist, Returning number", _i); //debug
                    return _i;
                }
            }
            return 0;
        }

        void ticketListCount(BasePlayer _player)
        {
            putSendMsg(_player, "Ticket Count: " + storedData.ticketCount.Count);
            putSendMsg(_player, " ");
        }

        void ticketListFunction(BasePlayer _player)
        {
            putSendMsg(_player, "Listing Available Tickets");
            putSendMsg(_player, "-----------------------------");
            foreach (var _ticket in storedData.ticketCount)
            {
                int _ticketID = _ticket.Key;
                ulong _ticketUser = _ticket.Value;
                string _ticketName = storedData.PvXData[_ticketUser].Name;
                putSendMsg(_player, "Ticket ID: [" + _ticketID + "], Player Name: " + _ticketName + ".");
                putSendMsg(_player, " ");
            }
        }
        void ticketDisplayFunction(BasePlayer _player)
        {
            int maxTicketCount = 3;
            int _i = 0;
            SendReply(_player, "Tickets");
            SendReply(_player, "------------------------");
            foreach (var _ticket in storedData.ticketCount)
            {
                int _ticketID = _ticket.Key;
                ulong _ticketUser = _ticket.Value;
                string _ticketName = storedData.PvXData[_ticketUser].Name;
                string _playerPvXSelection = "PvP";
                string _ticketReason = storedData.PvXData[_ticketUser].reason;
                if (storedData.PvXData[_ticketUser].PvP == false) _playerPvXSelection = "PvE";
                _i++;
                if (_i <= maxTicketCount)
                {
                    SendReply(_player, "Ticket ID: " + _ticketID);
                    SendReply(_player, "Player Name: " + _ticketName);
                    SendReply(_player, "Player UserID: " + _ticketUser);
                    SendReply(_player, "Player Currently is: " + _playerPvXSelection);
                    SendReply(_player, "Reason: " + _ticketReason);
                }
                else return;
            }
        }

        bool IsAllowed(BasePlayer _player, string perm, string reason)
        {
            if (permission.UserHasPermission(_player.UserIDString, perm)) return true;
            if (reason != "null")
                SendReply(_player, reason);
            return false;
        }

        void putSendMsg(BasePlayer _player, string msg)
        {
            Puts(msg);
            SendReply(_player, msg);
        }

        bool Damageplayer (BasePlayer attacker, BasePlayer victim)
        {
            bool testvar1 = storedData.PvXData[attacker.userID].PvP;
            bool testvar2 = storedData.PvXData[victim.userID].PvP;
            if ((testvar1 == true) && (testvar2 == true)) return true;
            else return false;
        }

        bool DamageEntity(BasePlayer attacker, BaseEntity _entity)
        {
            bool testvar1 = storedData.PvXData[attacker.userID].PvP;
            bool testvar2 = storedData.PvXData[_entity.OwnerID].PvP;
            if ((testvar1 == true) && (testvar2 == true))return true;
            else return false;
        }
            void ChatMessageHandler(BasePlayer player, string message)
        {
            PrintToChat(player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: {message}");
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Config ////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        private bool DebugMode;
        private bool NamesIncludeSleepers;
        private string ChatPrefixColor;
        private string ChatPrefix;
        private string ChatMessageColor;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file!");
            Config.Clear();
            LoadVariables();
        }
        void LoadVariables() //Stores Default Values, calling GetConfig passing: menu, dataValue, defaultValue
        {
            //Booleans
            DebugMode = Convert.ToBoolean(GetConfig("Settings", "DebugMode", false));
            //Ints
            //Floats
            //Strings
            ChatPrefix = Convert.ToString(GetConfig("ChatSettings", "ChatPrefix", "PvX"));
            ChatPrefixColor = Convert.ToString(GetConfig("ChatSettings", "ChatPrefixColor", "008800"));
            ChatMessageColor = Convert.ToString(GetConfig("ChatSettings", "ChatMessageColor", "yellow"));
        }

        object GetConfig(string menu, string dataValue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
            }
            object value;
            if (!data.TryGetValue(dataValue, out value))
            {
                value = defaultValue;
                data[dataValue] = value;
            }
            return value;
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Permision /////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void permissionHandle()
        {
            string[] Permissionarray = { "admin", "wipe"};
            foreach (string i in Permissionarray)
            {
                string regPerm = Title.ToLower() + "." + i;
                Puts("Checking if " + regPerm + " is registered.");
                if (!permission.PermissionExists(regPerm))
                {
                    permission.RegisterPermission(regPerm, this);
                    Puts(regPerm + " is registered.");
                }
                else
                {
                    Puts(regPerm + " is already registered.");
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Debug /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        int DebugLevel = 0;
        void DebugMessage(int _minDebuglvl, string _msg)
        {
            if (DebugLevel >= _minDebuglvl)
            {
                Puts(_msg);
                if (DebugLevel == 3 && _minDebuglvl == 1)
                {
                    PrintToChat(_msg);
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Lang //////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"TicketRemoved", "Ticket has been Declined and removed" },
            {"TicketAccepted", "Ticket has been Accepted, Players Selection Changed" },
            {"TicketDefaultReason", "Change Requested Via GUI" },
            {"NoTicket", "There are no tickets to display" },
            {"TicketSubmitted", "You have submitted a ticket to change" },
            {"AlreadySubmitted", "You have already submitted to change" },
            {"PvETarget", "You are attacking a PvE player" },
            {"PvEPlayer", "You are a PvE player" },
            {"PvEStructure", "That structure belongs to a PvE player" }
        };

        //////////////////////////////////////////////////////////////////////////////////////
        // GUI ///////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private void SelectorOverlay(BasePlayer player)
        {
            var elements = new CuiElementContainer();

            var mainName = elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.1 0.1 0.1 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.1 0.15",
                    AnchorMax = "0.4 0.25"
                },
                CursorEnabled = true
            }, "HUD/Overlay", "RulesGUI");
            var PVP = new CuiButton
            {
                Button =
                {
                    Command = "PvXSelection pvp",
                    Close = mainName,
                    Color = "0.8 0.2 0.2 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.2 0.16",
                    AnchorMax = "0.45 0.8"
                },
                Text =
                {
                    Text = "PVP",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
            var PVE = new CuiButton
            {
                Button =
                {
                    Command = "PvXSelection pve",
                    Close = mainName,
                    Color = "0.2 0.8 0.2 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.55 0.2",
                    AnchorMax = "0.8 0.8"
                },
                Text =
                {
                    Text = "PVE",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
            elements.Add(PVP, mainName);
            elements.Add(PVE, mainName);
            CuiHelper.AddUi(player, elements);
        }
    }
}
