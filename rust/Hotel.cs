// Reference: NLua

using System;
using System.Collections.Generic;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using NLua;

namespace Oxide.Plugins
{
    [Info("Hotel", "Reneb", "1.1.4", ResourceId = 1298)]
    class Hotel : RustPlugin
    {

        ////////////////////////////////////////////////////////////
        // Plugin References
        ////////////////////////////////////////////////////////////

        [PluginReference]
        Plugin ZoneManager;

        [PluginReference("Economics")]
        Plugin Economics;

        //////////////////////////////////////////////////////////////////////////////////////
        // Workaround the Blocks of Economics. Hope This wont be needed in the future
        // THX MUGHISI ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void OnServerInitialized()
        {
            LoadPermissions();
        }

        ////////////////////////////////////////////////////////////
        // Fields
        ////////////////////////////////////////////////////////////

        static int deployableColl = UnityEngine.LayerMask.GetMask(new string[] { "Deployed" });
        static int constructionColl = UnityEngine.LayerMask.GetMask(new string[] { "Construction", "Construction Trigger" });
        Oxide.Plugins.Timer hotelTimer;
        Hash<BasePlayer, Oxide.Plugins.Timer> playerguiTimers = new Hash<BasePlayer, Oxide.Plugins.Timer>();

        ////////////////////////////////////////////////////////////
        // cached Fields
        ////////////////////////////////////////////////////////////

        public static Dictionary<string, HotelData> EditHotel = new Dictionary<string, HotelData>();
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        public static Vector3 Vector3UP = new Vector3(0f, 0.1f, 0f);
        public static Vector3 Vector3UP2 = new Vector3(0f, 1.5f, 0f);
        public FieldInfo fieldWhiteList;
        public FieldInfo serverinput;
        public static Quaternion defaultQuaternion = new Quaternion(0f, 0f, 0f, 0f);

        ////////////////////////////////////////////////////////////
        // Config Management
        ////////////////////////////////////////////////////////////

        static int authlevel = 2;
        static string MessageAlreadyEditing = "You are already editing a hotel. You must close or save it first.";
        static string MessageHotelNewHelp = "You must select a name for the new hotel: /hotel_new HOTELNAME";
        static string MessageHotelEditHelp = "You must select the name of the hotel you want to edit: /hotel_edit HOTELNAME";
        static string MessageHotelEditEditing = "You are editing the hotel named: {0}. Now say /hotel to continue configuring your hotel. Note that no one can register/leave the hotel while you are editing it.";
        static string MessageErrorAlreadyExist = "{0} is already the name of a hotel";
        static string MessageErrorNotAllowed = "You are not allowed to use this command";
        static string MessageErrorEditDoesntExist = "The hotel \"{0}\" doesn't exist";
        static string MessageMaintenance = "This Hotel is under maintenance by the admin, you may not open this door at the moment";
        static string MessageErrorUnavaibleRoom = "This room is unavaible, seems like it wasn't set correctly";
        static string MessageHotelNewCreated = "You've created a new Hotel named: {0}. Now say /hotel to continue configuring your hotel.";
        static string MessageErrorNotAllowedToEnter = "You are not allowed to enter this room, it's already been used my someone else";

        static string MessageErrorAlreadyGotRoom = "You already have a room in this hotel!";
        static string MessageErrorPermissionsNeeded = "You must have the {0} permission to rent a room here";
        static string MessageRentUnlimited = "You now have access to this room for an unlimited time";
        static string MessageRentTimeLeft = "You now have access to this room. You are allowed to keep this room for {0}";
        static string MessagePaydRent = "You payed for this room {0} coins";
        static string MessageErrorNotEnoughCoins = "This room costs {0} coins. You only have {1} coins";

        static string GUIBoardAdmin = "                             <color=green>HOTEL MANAGER</color> \n\nHotel Name:      {name} \n\nHotel Location: {loc} \nHotel Radius:     {hrad} \n\nRooms Radius:   {rrad} \nRooms:                {rnum} \n<color=red>Occupied:            {onum}</color>\nRent Price:                  {rp}";
        static string GUIBoardPlayer = "                             <color=green>{name}</color> \n\nRooms:        <color=green>{fnum}</color>/{rnum} ";
        static string GUIBoardPlayerRoom = "\n\n                        Your Room\nJoined:         {jdate}\nTimeleft:      {timeleft}.";
        static string GUIBoardPlayerMaintenance = "                             <color=green>{name}</color> \n\nHotel is under maintenance. Please wait couple seconds/minutes until the admin is finished.";
        static string xmin = "0.65";
        static string xmax = "1.0";
        static string ymin = "0.6";
        static string ymax = "0.9";
        static string pxmin = "0.3";
        static string pxmax = "0.6";
        static string pymin = "0.7";
        static string pymax = "0.95";
        static int pTimeOut = 10;

        static bool EnterZoneShowRoom = false;
        static bool EnterZoneShowPlayerGUI = false;
        static bool UseNPCShowRoom = true;
        static bool UseNPCShowPlayerGUI = true;
        static bool OpenDoorShowRoom = false;
        static bool OpenDoorPlayerGUI = true;

        public static string adminguijson = @"[  
			{ 
				""name"": ""HotelAdmin"",
				""parent"": ""Overlay"",
				""components"":
				[
					{
						 ""type"":""UnityEngine.UI.Image"",
						 ""color"":""0.1 0.1 0.1 0.7"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""{xmin} {ymin}"",
						""anchormax"": ""{xmax} {ymax}""
						
					}
				]
			},
			{
				""parent"": ""HotelAdmin"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Text"",
						""text"":""{msg}"",
						""fontSize"":15,
						""align"": ""MiddleLeft"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.1 0.1"",
						""anchormax"": ""1 1""
					}
				]
			}
		]
		";

        public static string playerguijson = @"[  
			{ 
				""name"": ""HotelPlayer"",
				""parent"": ""Overlay"",
				""components"":
				[
					{
						 ""type"":""UnityEngine.UI.Image"",
						 ""color"":""0.1 0.1 0.1 0.7"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""{pxmin} {pymin}"",
						""anchormax"": ""{pxmax} {pymax}""
						
					}
				]
			},
			{
				""parent"": ""HotelPlayer"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Text"",
						""text"":""{msg}"",
						""fontSize"":15,
						""align"": ""MiddleLeft"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.1 0.1"",
						""anchormax"": ""1 1""
					}
				]
			}
		]
		";

        protected override void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<int>("Configure - Level Required", ref authlevel);

            CheckCfg<string>("AdminMessage - Hotel - New - Help", ref MessageHotelNewHelp);
            CheckCfg<string>("AdminMessage - Hotel - New - Confirm", ref MessageHotelNewCreated);
            CheckCfg<string>("AdminMessage - Hotel - Edit - Help", ref MessageHotelEditHelp);
            CheckCfg<string>("AdminMessage - Hotel - Edit - Confirm", ref MessageHotelEditEditing);
            CheckCfg<string>("AdminMessage - Hotel - Error - Doesnt Exist", ref MessageErrorEditDoesntExist);
            CheckCfg<string>("AdminMessage - Hotel - Error - Already Exist", ref MessageErrorAlreadyExist);
            CheckCfg<string>("AdminMessage - Hotel - Error - Not Allowed", ref MessageErrorNotAllowed);
            CheckCfg<string>("AdminMessage - Hotel - Error - Already Editing Hotel", ref MessageAlreadyEditing);

            CheckCfg<string>("PlayerMessage - Hotel Maintenance", ref MessageMaintenance);
            CheckCfg<string>("PlayerMessage - Error - Unavaible Room", ref MessageErrorUnavaibleRoom);
            CheckCfg<string>("PlayerMessage - Error - Restricted", ref MessageErrorNotAllowedToEnter);
            CheckCfg<string>("PlayerMessage - Error - Already have a Room", ref MessageErrorAlreadyGotRoom);
            CheckCfg<string>("PlayerMessage - Error - Need Permissions", ref MessageErrorPermissionsNeeded);
            CheckCfg<string>("PlayerMessage - Unlimited Access", ref MessageRentUnlimited);
            CheckCfg<string>("PlayerMessage - Limited Access", ref MessageRentTimeLeft);
            CheckCfg<string>("PlayerMessage - Payd Rent", ref MessagePaydRent);
            CheckCfg<string>("PlayerMessage - Error - Not Enough Coins", ref MessageErrorNotEnoughCoins);
            
            CheckCfg<string>("GUI - Admin - Board Message", ref GUIBoardAdmin);
            CheckCfg<string>("GUI - Player - Board Message", ref GUIBoardPlayer);
            CheckCfg<string>("GUI - Player - Room Board Message ", ref GUIBoardPlayerRoom);
            CheckCfg<string>("GUI - Player - Maintenance Board Message", ref GUIBoardPlayerMaintenance);
            CheckCfg<string>("GUI - Player - minX", ref pxmin);
            CheckCfg<string>("GUI - Player - maxX", ref pxmax);
            CheckCfg<string>("GUI - Player - minY", ref pymin);
            CheckCfg<string>("GUI - Player - maxY", ref pymax);
            CheckCfg<int>("GUI - Player - Board Remove Timer", ref pTimeOut);
            CheckCfg<string>("GUI - Admin - minX", ref xmin);
            CheckCfg<string>("GUI - Admin - maxX", ref xmax);
            CheckCfg<string>("GUI - Admin - minY", ref ymin);
            CheckCfg<string>("GUI - Admin - maxY", ref ymax);
            CheckCfg<bool>("GUI - Player - Show Board When Entering Hotel Zone", ref EnterZoneShowPlayerGUI);
            CheckCfg<bool>("GUI - Player - Show Room When Entering Hotel Zone", ref EnterZoneShowRoom);
            CheckCfg<bool>("GUI - Player - Show Board When Talking To NPC", ref UseNPCShowPlayerGUI);
            CheckCfg<bool>("GUI - Player - Show Room When Talking To NPC", ref UseNPCShowRoom);
            CheckCfg<bool>("GUI - Player - Show Board When Opening Room Door", ref OpenDoorPlayerGUI);
            CheckCfg<bool>("GUI - Player - Show Room When Opening Room Door", ref OpenDoorShowRoom);
            SaveConfig();
        }


        ////////////////////////////////////////////////////////////
        // Data Management
        ////////////////////////////////////////////////////////////

        static StoredData storedData;

        class StoredData
        {
            public HashSet<HotelData> Hotels = new HashSet<HotelData>();

            public StoredData() { }
        }

        void OnServerSave() { SaveData(); }

        void SaveData() { Interface.GetMod().DataFileSystem.WriteObject("Hotel", storedData); }

        void LoadData()
        {
            try { storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("Hotel"); }
            catch { storedData = new StoredData(); }
        }

        public class DeployableItem
        {
            public string x;
            public string y;
            public string z;
            public string rx;
            public string ry;
            public string rz;
            public string rw;
            public string prefabname;

            Vector3 pos;
            Quaternion rot;

            public DeployableItem()
            {
            }

            public DeployableItem(Deployable deployable)
            {
                prefabname = StringPool.Get(deployable.prefabID).ToString();

                this.x = deployable.transform.position.x.ToString();
                this.y = deployable.transform.position.y.ToString();
                this.z = deployable.transform.position.z.ToString();

                this.rx = deployable.transform.rotation.x.ToString();
                this.ry = deployable.transform.rotation.y.ToString();
                this.rz = deployable.transform.rotation.z.ToString();
                this.rw = deployable.transform.rotation.w.ToString();
            }
            public Vector3 Pos()
            {
                if (pos == default(Vector3))
                    pos = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return pos;
            }
            public Quaternion Rot()
            {
                if (rot.w == 0f)
                    rot = new Quaternion(float.Parse(rx), float.Parse(ry), float.Parse(rz), float.Parse(rw));
                return rot;
            }
        }
        public class Room
        {
            public string roomid;
            public string x;
            public string y;
            public string z;

            public List<DeployableItem> defaultDeployables;

            public string renter;
            public string checkingTime;
            public string checkoutTime;

            double intcheckoutTime;
            Vector3 pos;

            public Room()
            {
            }

            public Room(Vector3 position)
            {
                this.x = Math.Ceiling(position.x).ToString();
                this.y = Math.Ceiling(position.y).ToString();
                this.z = Math.Ceiling(position.z).ToString();
                this.roomid = string.Format("{0}:{1}:{2}", this.x, this.y, this.z);
            }

            public Vector3 Pos()
            {
                if (pos == default(Vector3))
                    pos = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return pos;
            }

            public double CheckOutTime()
            {
                if (intcheckoutTime == default(double))
                    intcheckoutTime = Convert.ToDouble(checkoutTime);
                return intcheckoutTime;
            }

            public void Reset()
            {
                intcheckoutTime = default(double);
            }
        }

        public class HotelData
        {
            public string hotelname;
            public string x;
            public string y;
            public string z;
            public string r;
            public string rr;
            public string rd;
            public string npc;
            public string p;
            public string e;

            public Dictionary<string, Room> rooms;

            Vector3 pos;
            public bool enabled;
            public int price;

            public HotelData()
            {
                enabled = false;
                if (rooms == null) rooms = new Dictionary<string, Room>();
            }

            public HotelData(string hotelname)
            {
                this.hotelname = hotelname;
                this.x = "0";
                this.y = "0";
                this.z = "0";
                this.r = "60";
                this.rr = "10";
                this.rd = "86400";
                this.p = null;
                this.e = null;

                this.rooms = new Dictionary<string, Room>();
                enabled = false;
            }

            public Vector3 Pos()
            {
                if (this.x == "0" && this.y == "0" && this.z == "0")
                    return default(Vector3);
                if (pos == default(Vector3))
                    pos = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return pos;
            }

            public void RefreshRooms()
            {
                if (Pos() == default(Vector3))
                    return;
                Dictionary<string, Room> detectedRooms = FindAllRooms(Pos(), Convert.ToSingle(this.r), Convert.ToSingle(this.rr));

                List<string> toAdd = new List<string>();
                List<string> toDelete = new List<string>();
                if (rooms == null) rooms = new Dictionary<string, Room>();
                if (rooms.Count > 0)
                {
                    foreach (KeyValuePair<string, Room> pair in rooms)
                    {
                        if (pair.Value.renter != null)
                        {
                            detectedRooms.Remove(pair.Key);
                            Debug.Log(string.Format("{0} is occupied and can't be edited", pair.Key));
                            continue;
                        }
                        if (!detectedRooms.ContainsKey(pair.Key))
                        {
                            toDelete.Add(pair.Key);
                        }
                    }
                }
                foreach (KeyValuePair<string, Room> pair in detectedRooms)
                {
                    if (!rooms.ContainsKey(pair.Key))
                    {
                        toAdd.Add(pair.Key);
                    }
                    else
                    {
                        rooms[pair.Key] = pair.Value;
                    }

                }
                foreach (string roomid in toDelete)
                {
                    rooms.Remove(roomid);
                    Debug.Log(string.Format("{0} doesnt exist anymore, removing this room", roomid));
                }
                foreach (string roomid in toAdd)
                {
                    Debug.Log(string.Format("{0} is a new room, adding it", roomid));
                    rooms.Add(roomid, detectedRooms[roomid]);
                }
            }
            public int Price()
            {
                if (this.e == null) return 0;
                return Convert.ToInt32(this.e);
            }
            public void Deactivate()
            {
                enabled = false;
            }
            public void Activate()
            {
                enabled = true;
            }

            public void AddRoom(Room newroom)
            {
                if (rooms.ContainsKey(newroom.roomid))
                    rooms.Remove(newroom.roomid);

                rooms.Add(newroom.roomid, newroom);
            }
        }

        ////////////////////////////////////////////////////////////
        // Random Methods
        ////////////////////////////////////////////////////////////

        static double LogTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        static void CloseDoor(Door door)
        {
            door.SetFlag(BaseEntity.Flags.Open, false);
            door.SendNetworkUpdateImmediate(true);
        }
        static void OpenDoor(Door door)
        {
            door.SetFlag(BaseEntity.Flags.Open, true);
            door.SendNetworkUpdateImmediate(true);
        }
        static void LockLock(CodeLock codelock)
        {
            codelock.SetFlag(BaseEntity.Flags.Locked, true);
            codelock.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
        }
        static void UnlockLock(CodeLock codelock)
        {
            codelock.SetFlag(BaseEntity.Flags.Locked, false);
            codelock.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
        }

        void LoadPermissions()
        {
            if (!permission.PermissionExists("canhotel")) permission.RegisterPermission("canhotel", this);
            foreach (HotelData hotel in storedData.Hotels)
            {
                if (hotel.p == null) continue;
                if (!permission.PermissionExists(hotel.p)) permission.RegisterPermission(hotel.p, this);
            }
        }

        ////////////////////////////////////////////////////////////
        // Oxide Hooks
        ////////////////////////////////////////////////////////////

        void Unload()
        {
            SaveData();
            hotelTimer.Destroy();
        }

        void Loaded()
        {
            adminguijson = adminguijson.Replace("{xmin}", xmin).Replace("{xmax}", xmax).Replace("{ymin}", ymin).Replace("{ymax}", ymax);
            playerguijson = playerguijson.Replace("{pxmin}", pxmin).Replace("{pxmax}", pxmax).Replace("{pymin}", pymin).Replace("{pymax}", pymax);
            fieldWhiteList = typeof(CodeLock).GetField("whitelistPlayers", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            hotelTimer = timer.Repeat(60f, 0, () => CheckTimeOutRooms());
            LoadData();
        }

        object CanUseDoor(BasePlayer player, BaseLock baselock)
        {
            if (baselock == null) return null;
            CodeLock codelock = baselock as CodeLock;
            if (codelock == null) return null;
            BaseEntity parententity = codelock.GetParentEntity();
            if (parententity == null) return null;
            if (parententity.HasFlag(BaseEntity.Flags.Open)) return null;

            string zonename = string.Empty;
            HotelData targethotel = null;
            foreach (HotelData hotel in storedData.Hotels)
            {
                //Check if the player is inside a hotel
                // Is this the best way to do it?
                // Might need to actually make a list of all codelocks that are used inside a hotel instead of this ...
                object isplayerinzone = ZoneManager.Call("isPlayerInZone", hotel.hotelname, player);
                if (isplayerinzone is bool && (bool)isplayerinzone) targethotel = hotel;
            }
            if (targethotel == null) return null;

            if (OpenDoorPlayerGUI)
                RefreshPlayerHotelGUI(player, targethotel);
            if (OpenDoorShowRoom)
                ShowPlayerRoom(player, targethotel);

            if (!targethotel.enabled)
            {
                SendReply(player, MessageMaintenance);
                return false;
            }

            Room room = FindRoomByDoorAndHotel(targethotel, parententity);
            if (room == null)
            {
                SendReply(player, MessageErrorUnavaibleRoom);
                return false;
            }

            if (room.renter == null)
            {
                if (!CanRentRoom(player, targethotel)) return false;
                ResetRoom(codelock, targethotel, room);
                NewRoomOwner(codelock, player, targethotel, room);
                if(targethotel.e != null && Economics)
                {
                    EconomicsWithdraw(player, targethotel.Price());
                }
            }

            LockLock(codelock);

            if (room.renter != player.userID.ToString())
            {
                SendReply(player, MessageErrorNotAllowedToEnter);
                return false;
            }

            return true;
        }



        ////////////////////////////////////////////////////////////
        // Room Management Functions
        ////////////////////////////////////////////////////////////
        void CheckTimeOutRooms()
        {
            double currenttime = LogTime();
            foreach (HotelData hotel in storedData.Hotels)
            {
                if (!hotel.enabled) continue;
                foreach (KeyValuePair<string, Room> pair in hotel.rooms)
                {
                    if (pair.Value.CheckOutTime() == 0.0) continue;
                    if (pair.Value.CheckOutTime() > currenttime) continue;

                    ResetRoom(hotel, pair.Value);
                }
            }
        }
        static List<Door> FindDoorsFromPosition(Vector3 position, float radius)
        {
            List<Door> listLocks = new List<Door>();
            foreach (Collider col in UnityEngine.Physics.OverlapSphere(position, radius, constructionColl))
            {
                Door door = col.GetComponentInParent<Door>();
                if (door == null) continue;
                if (!door.HasSlot(BaseEntity.Slot.Lock)) continue;
                if (door.GetSlot(BaseEntity.Slot.Lock) == null) continue;
                if (!(door.GetSlot(BaseEntity.Slot.Lock) is CodeLock)) continue;
                CloseDoor(door);
                listLocks.Add(door);
            }
            return listLocks;
        }

        static Dictionary<string, Room> FindAllRooms(Vector3 position, float radius, float roomradius)
        {
            List<Door> listLocks = FindDoorsFromPosition(position, radius);

            Hash<Deployable, string> deployables = new Hash<Deployable, string>();
            Dictionary<string, Room> tempRooms = new Dictionary<string, Room>();

            foreach (Door door in listLocks)
            {
                Vector3 pos = door.transform.position;
                Room newRoom = new Room(pos);
                newRoom.defaultDeployables = new List<DeployableItem>();
                List<Deployable> founditems = new List<Deployable>();

                foreach (Collider col in UnityEngine.Physics.OverlapSphere(pos, roomradius, deployableColl))
                {
                    Deployable deploy = col.GetComponentInParent<Deployable>();
                    if (deploy == null) continue;
                    if (founditems.Contains(deploy)) continue;
                    founditems.Add(deploy);

                    bool canReach = true;
                    foreach (RaycastHit rayhit in UnityEngine.Physics.RaycastAll(deploy.transform.position + Vector3UP, (pos + Vector3UP - deploy.transform.position).normalized, Vector3.Distance(deploy.transform.position, pos) - 0.2f, constructionColl))
                    {
                        if (rayhit.collider.GetComponentInParent<Door>() != null)
                        {
                            if (rayhit.collider.GetComponentInParent<Door>() == door)
                                continue;
                        }
                        canReach = false;
                        break;
                    }
                    if (!canReach) continue;

                    if (deployables[deploy] != null) deployables[deploy] = "0";
                    else deployables[deploy] = newRoom.roomid;
                }
                tempRooms.Add(newRoom.roomid, newRoom);
            }
            foreach (KeyValuePair<Deployable, string> pair in deployables)
            {
                if (pair.Value != "0")
                {
                    DeployableItem newDeployItem = new DeployableItem(pair.Key);
                    tempRooms[pair.Value].defaultDeployables.Add(newDeployItem);
                }
            }
            return tempRooms;
        }

        static Room FindRoomByDoorAndHotel(HotelData hotel, BaseEntity door)
        {
            string roomid = string.Format("{0}:{1}:{2}", Math.Ceiling(door.transform.position.x).ToString(), Math.Ceiling(door.transform.position.y).ToString(), Math.Ceiling(door.transform.position.z).ToString());
            if (!hotel.rooms.ContainsKey(roomid)) return null;

            return hotel.rooms[roomid];
        }
        void EconomicsWithdraw(BasePlayer player, int amount)
        {
            Economics?.Call("Withdraw", player.userID, amount);
            SendReply(player, string.Format("You payed for this room {0} coins", amount.ToString()));
        }

        bool CanRentRoom(BasePlayer player, HotelData hotel)
        {
            foreach (KeyValuePair<string, Room> pair in hotel.rooms)
            {
                if (pair.Value.renter == player.userID.ToString())
                {
                    SendReply(player, MessageErrorAlreadyGotRoom);
                    return false;
                }
            }
            if (hotel.p != null)
            {
                if (!permission.UserHasPermission(player.userID.ToString(), hotel.p))
                {
                    SendReply(player, string.Format(MessageErrorPermissionsNeeded, hotel.p));
                    return false;
                }
            }
            if(hotel.e != null && Economics != null)
            {
                int money = Convert.ToInt32((double)Economics.Call("GetPlayerMoney", player.userID));
                if(money < hotel.Price())
                {
                    SendReply(player, string.Format(MessageErrorNotEnoughCoins, hotel.e, money.ToString()));
                    return false;
                }
            }
            return true;
        }
        bool FindHotelAndRoomByPos(Vector3 position, out HotelData hoteldata, out Room roomdata)
        {
            hoteldata = null;
            roomdata = null;
            position.x = Mathf.Ceil(position.x);
            position.y = Mathf.Ceil(position.y);
            position.z = Mathf.Ceil(position.z);
            foreach (HotelData hotel in storedData.Hotels)
            {
                foreach (KeyValuePair<string, Room> pair in hotel.rooms)
                {
                    if (pair.Value.Pos() == position)
                    {
                        hoteldata = hotel;
                        roomdata = pair.Value;
                        return true;
                    }
                }
            }
            return false;

        }
        CodeLock FindCodeLockByRoomID(string roomid)
        {
            string[] rpos = roomid.Split(':');
            if (rpos.Length != 3) return null;

            return FindCodeLockByPos(new Vector3(Convert.ToSingle(rpos[0]), Convert.ToSingle(rpos[1]), Convert.ToSingle(rpos[2])));
        }
        CodeLock FindCodeLockByPos(Vector3 pos)
        {
            CodeLock findcode = null;
            foreach (Collider col in UnityEngine.Physics.OverlapSphere(pos, 2f, constructionColl))
            {
                if (col.GetComponentInParent<Door>() == null) continue;
                if (!col.GetComponentInParent<Door>().HasSlot(BaseEntity.Slot.Lock)) continue;

                BaseEntity slotentity = col.GetComponentInParent<Door>().GetSlot(BaseEntity.Slot.Lock);
                if (slotentity == null) continue;
                if (slotentity.GetComponent<CodeLock>() == null) continue;

                if (findcode != null)
                    if (Vector3.Distance(pos, findcode.GetParentEntity().transform.position) < Vector3.Distance(pos, col.transform.position))
                        continue;
                findcode = slotentity.GetComponent<CodeLock>();
            }
            return findcode;
        }
        void SpawnDeployable(string prefabname, Vector3 pos, Quaternion rot, BasePlayer player = null)
        {
            UnityEngine.GameObject newPrefab = GameManager.server.FindPrefab(prefabname);
            if (newPrefab == null) return;

            BaseEntity entity = GameManager.server.CreateEntity(newPrefab.name, pos, rot);
            if (entity == null) return;

            if (player != null)
                entity.SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver);

            entity.Spawn(true);
        }

        void NewRoomOwner(CodeLock codelock, BasePlayer player, HotelData hotel, Room room)
        {
            BaseEntity door = codelock.GetParentEntity();
            Vector3 block = door.transform.position;

            EmptyDeployablesRoom(door, Convert.ToSingle(hotel.rr));

            foreach (DeployableItem deploy in room.defaultDeployables) { SpawnDeployable(deploy.prefabname, deploy.Pos(), deploy.Rot(), player); }

            List<ulong> whitelist = new List<ulong>();
            whitelist.Add(player.userID);
            fieldWhiteList.SetValue(codelock, whitelist);

            room.renter = player.userID.ToString();
            room.checkingTime = LogTime().ToString();

            room.checkoutTime = hotel.rd == "0" ? "0" : (LogTime() + double.Parse(hotel.rd)).ToString();
            room.Reset();

            LockLock(codelock);
            OpenDoor(door as Door);

            SendReply(player, hotel.rd == "0" ? MessageRentUnlimited : string.Format(MessageRentTimeLeft, ConvertSecondsToBetter(hotel.rd)));
        }
        void EmptyDeployablesRoom(BaseEntity door, float radius)
        {
            var founditems = new List<Deployable>();
            Vector3 doorpos = door.transform.position;
            foreach (Collider col in UnityEngine.Physics.OverlapSphere(doorpos, radius, deployableColl))
            {
                Deployable deploy = col.GetComponentInParent<Deployable>();
                if (deploy == null) continue;
                if (founditems.Contains(deploy)) continue;

                bool canReach = true;
                foreach (RaycastHit rayhit in UnityEngine.Physics.RaycastAll(deploy.transform.position + Vector3UP, (doorpos + Vector3UP - deploy.transform.position).normalized, Vector3.Distance(deploy.transform.position, doorpos) - 0.2f, constructionColl))
                {
                    if (rayhit.collider.GetComponentInParent<BaseEntity>() == door)
                        continue;
                    canReach = false;
                    break;
                }
                if (!canReach) continue;

                foreach (Collider col2 in UnityEngine.Physics.OverlapSphere(doorpos, radius, constructionColl))
                {
                    if (col2.GetComponentInParent<Door>() == null) continue;
                    if (col2.transform.position == doorpos) continue;

                    bool canreach2 = true;
                    foreach (RaycastHit rayhit in UnityEngine.Physics.RaycastAll(deploy.transform.position + Vector3UP, (col2.transform.position + Vector3UP - deploy.transform.position).normalized, Vector3.Distance(deploy.transform.position, col2.transform.position) - 0.2f, constructionColl)) { canreach2 = false; }
                    if (canreach2) { canReach = false; break; }
                }
                if (!canReach) continue;

                founditems.Add(deploy);
            }
            foreach (Deployable deploy in founditems)
            {
                if (!(deploy.GetComponentInParent<BaseEntity>().isDestroyed))
                    deploy.GetComponent<BaseEntity>().KillMessage();
            }
        }
        void ResetRoom(HotelData hotel, Room room)
        {
            CodeLock codelock = FindCodeLockByPos(room.Pos());
            if (codelock == null) return;
            ResetRoom(codelock, hotel, room);
        }
        void ResetRoom(CodeLock codelock, HotelData hotel, Room room)
        {
            BaseEntity door = codelock.GetParentEntity();
            Vector3 block = door.transform.position;

            EmptyDeployablesRoom(door, Convert.ToSingle(hotel.rr));
            foreach (DeployableItem deploy in room.defaultDeployables) { SpawnDeployable(deploy.prefabname, deploy.Pos(), deploy.Rot(), null); }

            fieldWhiteList.SetValue(codelock, new List<ulong>());

            UnlockLock(codelock);
            CloseDoor(door as Door);

            room.renter = null;
            room.checkingTime = null;
            room.checkoutTime = null;
            room.Reset();
        }

        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            string npcid = npc.userID.ToString();
            foreach (HotelData hotel in storedData.Hotels)
            {
                if (hotel.npc == null) continue;
                if (hotel.npc != npcid) continue;

                if (UseNPCShowPlayerGUI)
                    RefreshPlayerHotelGUI(player, hotel);
                if (UseNPCShowRoom)
                    ShowPlayerRoom(player, hotel);
            }
        }
        void OnEnterZone(string zoneid, BasePlayer player)
        {
            foreach (HotelData hotel in storedData.Hotels)
            {
                if (hotel.hotelname == null) continue;
                if (hotel.hotelname != zoneid) continue;

                if (EnterZoneShowPlayerGUI)
                    RefreshPlayerHotelGUI(player, hotel);
                if (EnterZoneShowRoom)
                    ShowPlayerRoom(player, hotel);
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// GUI
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void RefreshAdminHotelGUI(BasePlayer player)
        {
            RemoveAdminHotelGUI(player);

            if (!EditHotel.ContainsKey(player.userID.ToString())) return;
            string Msg = CreateAdminGUIMsg(player);
            if (Msg == string.Empty) return;
            string send = adminguijson.Replace("{msg}", Msg);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(send));
        }
        void RefreshPlayerHotelGUI(BasePlayer player, HotelData hotel)
        {
            RemovePlayerHotelGUI(player);
            string Msg = string.Empty;
            string send = string.Empty;

            if (!hotel.enabled)
            {

                Msg = CreatePlayerGUIMsg(player, hotel, GUIBoardPlayerMaintenance);
                send = playerguijson.Replace("{msg}", Msg);

            }
            else
            {
                Msg = CreatePlayerGUIMsg(player, hotel, GUIBoardPlayer);
                if (Msg == string.Empty) return;
                send = playerguijson.Replace("{msg}", Msg);
            }
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(send));
            playerguiTimers[player] = timer.Once(pTimeOut, () => RemovePlayerHotelGUI(player));
        }
        string ConvertSecondsToBetter(string seconds)
        {
            return ConvertSecondsToBetter(double.Parse(seconds));
        }
        string ConvertSecondsToBetter(double seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}d:{1:D2}h:{2:D2}m:{3:D2}s",
                t.Days,
                t.Hours,
                t.Minutes,
                t.Seconds);
        }
        string ConvertSecondsToDate(string seconds)
        {
            return ConvertSecondsToDate(double.Parse(seconds));
        }
        string ConvertSecondsToDate(double seconds)
        {
            return epoch.AddSeconds(seconds).ToLocalTime().ToString();
        }
        string CreatePlayerGUIMsg(BasePlayer player, HotelData hotel, string GUIMsg)
        {
            string newguimsg = string.Empty;

            string loc = hotel.x == null ? "None" : string.Format("{0} {1} {2}", hotel.x, hotel.y, hotel.z);
            string hrad = hotel.r == null ? "None" : hotel.r;
            string rrad = hotel.rr == null ? "None" : hotel.rr;
            string rnum = hotel.rooms == null ? "0" : hotel.rooms.Count.ToString();

            int onumint = 0;
            int fnumint = 0;
            string roomgui = string.Empty;
            if (hotel.rooms != null)
            {
                foreach (KeyValuePair<string, Room> pair in hotel.rooms)
                {
                    if (pair.Value.renter != null)
                    {
                        onumint++;
                        if (pair.Value.renter == player.userID.ToString())
                        {
                            roomgui = GUIBoardPlayerRoom.Replace("{jdate}", ConvertSecondsToDate(pair.Value.checkingTime)).Replace("{timeleft}", pair.Value.CheckOutTime() == 0.0 ? "Unlimited" : ConvertSecondsToBetter(pair.Value.CheckOutTime() - LogTime()));
                        }
                    }
                    else fnumint++;

                }
            }
            string onum = onumint.ToString();
            string fnum = fnumint.ToString();

            newguimsg = GUIMsg.Replace("{name}", hotel.hotelname).Replace("{loc}", loc).Replace("{hrad}", hrad).Replace("{rrad}", rrad).Replace("{rnum}", rnum).Replace("{onum}", onum).Replace("{fnum}", fnum) + roomgui;

            return newguimsg;
        }

        string CreateAdminGUIMsg(BasePlayer player)
        {
            string newguimsg = string.Empty;
            HotelData hoteldata = EditHotel[player.userID.ToString()];

            string loc = hoteldata.x == null ? "None" : string.Format("{0} {1} {2}", hoteldata.x, hoteldata.y, hoteldata.z);
            string hrad = hoteldata.r == null ? "None" : hoteldata.r;
            string rrad = hoteldata.rr == null ? "None" : hoteldata.rr;
            string rrp = hoteldata.e == null ? "None" : hoteldata.e;
            string rnum = hoteldata.rooms == null ? "0" : hoteldata.rooms.Count.ToString();

            int onumint = 0;
            int fnumint = 0;
            if (hoteldata.rooms != null)
            {
                foreach (KeyValuePair<string, Room> pair in hoteldata.rooms)
                {
                    if (pair.Value.renter != null) onumint++;
                    else fnumint++;
                }
            }
            string onum = onumint.ToString();
            string fnum = fnumint.ToString();

            newguimsg = GUIBoardAdmin.Replace("{name}", hoteldata.hotelname).Replace("{loc}", loc).Replace("{hrad}", hrad).Replace("{rrad}", rrad).Replace("{rnum}", rnum).Replace("{onum}", onum).Replace("{fnum}", fnum).Replace("{rp}", rrp);

            return newguimsg;
        }

        void RemoveAdminHotelGUI(BasePlayer player) { CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("HotelAdmin")); }
        void RemovePlayerHotelGUI(BasePlayer player)
        {
            if (player == null || player.net == null) return;
            if (playerguiTimers[player] != null)
                playerguiTimers[player].Destroy();
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("HotelPlayer"));
        }

        void ShowHotelGrid(BasePlayer player)
        {
            HotelData hoteldata = EditHotel[player.userID.ToString()];
            if (hoteldata.x != null && hoteldata.r != null)
            {
                Vector3 hpos = hoteldata.Pos();
                float hrad = Convert.ToSingle(hoteldata.r);
                player.SendConsoleCommand("ddraw.sphere", 5f, UnityEngine.Color.blue, hpos, hrad);
            }
            if (hoteldata.rooms == null) return;
            foreach (KeyValuePair<string, Room> pair in hoteldata.rooms)
            {
                List<DeployableItem> deployables = pair.Value.defaultDeployables;
                foreach (DeployableItem deployable in deployables)
                {
                    player.SendConsoleCommand("ddraw.arrow", 10f, UnityEngine.Color.green, pair.Value.Pos(), deployable.Pos(), 0.5f);
                }
            }
        }
        void ShowPlayerRoom(BasePlayer player, HotelData hotel)
        {
            Room foundroom = null;
            foreach (KeyValuePair<string, Room> pair in hotel.rooms)
            {
                if (pair.Value.renter == player.userID.ToString())
                {
                    foundroom = pair.Value;
                    break;
                }
            }
            if (foundroom == null) return;
            player.SendConsoleCommand("ddraw.arrow", 10f, UnityEngine.Color.green, player.transform.position, foundroom.Pos() + Vector3UP2, 0.5f);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// CHAT Related 
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        bool hasAccess(BasePlayer player)
        {
            if (player == null) return false;
            if (player.net.connection.authLevel >= authlevel) return true;
            return permission.UserHasPermission(player.userID.ToString(), "canhotel");
        }

        [ChatCommand("hotel_save")]
        void cmdChatHotelSave(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, "You dont have access to this command"); return; }
            if (!EditHotel.ContainsKey(player.userID.ToString()))
            {
                SendReply(player, "You are not editing a hotel.");
                return;
            }
            HotelData editedhotel = EditHotel[player.userID.ToString()];

            HotelData removeHotel = null;
            foreach (HotelData hoteldata in storedData.Hotels)
            {
                if (hoteldata.hotelname.ToLower() == editedhotel.hotelname.ToLower())
                {
                    removeHotel = hoteldata;
                    break;
                }
            }
            if (removeHotel != null)
            {
                storedData.Hotels.Remove(removeHotel);
                removeHotel.Activate();
            }
            editedhotel.Activate();

            storedData.Hotels.Add(editedhotel);

            SaveData();
            LoadPermissions();

            EditHotel.Remove(player.userID.ToString());

            SendReply(player, "Hotel Saved and Closed.");

            RemoveAdminHotelGUI(player);
        }

        [ChatCommand("hotel_close")]
        void cmdChatHotelClose(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, "You dont have access to this command"); return; }
            if (!EditHotel.ContainsKey(player.userID.ToString()))
            {
                SendReply(player, "You are not editing a hotel.");
                return;
            }
            HotelData editedhotel = EditHotel[player.userID.ToString()];
            foreach (HotelData hoteldata in storedData.Hotels)
            {
                if (hoteldata.hotelname.ToLower() == editedhotel.hotelname.ToLower())
                {
                    hoteldata.Activate();
                    break;
                }
            }

            EditHotel.Remove(player.userID.ToString());

            SendReply(player, "Hotel Closed without saving.");

            RemoveAdminHotelGUI(player);
        }

        [ChatCommand("hotel")]
        void cmdChatHotel(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, "You dont have access to this command"); return; }
            if (!EditHotel.ContainsKey(player.userID.ToString()))
            {
                SendReply(player, "You are not editing a hotel. Create a new one with /hotel_new, or edit an existing one with /hotel_edit");
                return;
            }

            HotelData editedhotel = EditHotel[player.userID.ToString()];

            if (args.Length == 0)
            {
                SendReply(player, "==== Available options ====");
                SendReply(player, "/hotel location => sets the center hotel location where you stand");
                SendReply(player, "/hotel npc NPCID => sets the NPC that is hooked to this hotel (for UseNPC items)");
                SendReply(player, "/hotel permission PERMISSIONNAME => sets the oxide permissions that the player needs to rent a room here");
                SendReply(player, "/hotel radius XX => sets the radius of the hotel (the entire structure of the hotel needs to be covered by the zone");
                SendReply(player, "/hotel rentduration XX => Sets the duration of a default rent in this hotel. 0 is infinite.");
                SendReply(player, "/hotel rentprice XX => Sets the rentprice of a room. This requires Economics");
                SendReply(player, "/hotel reset => resets the hotel data (all players and rooms but keeps the hotel)");
                SendReply(player, "/hotel roomradius XX => sets the radius of the rooms");
                SendReply(player, "/hotel rooms => refreshs the rooms (detects new rooms, deletes rooms if they don't exist anymore, if rooms are in use they won't get taken in count)");
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "location":
                        string rad = editedhotel.r == null ? "20" : editedhotel.r;
                        string[] zoneargs = new string[] { "name", editedhotel.hotelname, "radius", rad };
                        ZoneManager.Call("CreateOrUpdateZone", editedhotel.hotelname, zoneargs, player.transform.position);

                        (EditHotel[player.userID.ToString()]).x = player.transform.position.x.ToString();
                        (EditHotel[player.userID.ToString()]).y = player.transform.position.y.ToString();
                        (EditHotel[player.userID.ToString()]).z = player.transform.position.z.ToString();

                        SendReply(player, string.Format("Location set to {0}", player.transform.position.ToString()));
                        break;
                    case "rentduration":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel rentduration XX");
                            return;
                        }
                        int rd = 86400;
                        int.TryParse(args[1], out rd);

                        (EditHotel[player.userID.ToString()]).rd = rd.ToString();
                        SendReply(player, string.Format("Rent Duration set to {0}", rd == 0 ? "Infinite" : rd.ToString()));
                        break;
                    case "rentprice":
                        if(Economics == null)
                        {
                            SendReply(player, "You don't have economics, so this is useless for you.");
                            return;
                        }
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel rentprice XX");
                            return;
                        }
                        int rp = 0;
                        if(!int.TryParse(args[1], out rp))
                        {
                            SendReply(player, "/hotel rentprice XX");
                            return;
                        }
                        (EditHotel[player.userID.ToString()]).e = rp == 0 ? null : rp.ToString();
                        SendReply(player, string.Format("Rent Price set to {0}", rp == 0 ? "null" : rp.ToString()));
                        break;
                    case "roomradius":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel roomradius XX");
                            return;
                        }
                        int rad3 = 5;
                        int.TryParse(args[1], out rad3);
                        if (rad3 < 1) rad3 = 5;

                        (EditHotel[player.userID.ToString()]).rr = rad3.ToString();

                        SendReply(player, string.Format("RoomRadius set to {0}", args[1]));
                        break;
                    case "permission":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel permission PERMISSIONNAME => Sets a permission that the player must have to rent in this hotel. put null or false to cancel the permission");
                            return;
                        }
                        string setnewperm = (args[1].ToLower() == "null" || args[1].ToLower() == "false" || args[1].ToLower() == "0") ? null : args[1];
                        (EditHotel[player.userID.ToString()]).p = setnewperm;

                        SendReply(player, string.Format("Permissions set to {0}", setnewperm == null ? "null" : setnewperm));
                        break;
                    case "npc":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel npc NPCID");
                            return;
                        }
                        int npcid = 0;
                        int.TryParse(args[1], out npcid);
                        if (npcid < 1) return;

                        (EditHotel[player.userID.ToString()]).npc = npcid.ToString();
                        SendReply(player, string.Format("NPC ID hooked to this hotel: {0}", npcid.ToString()));
                        break;
                    case "rooms":
                        SendReply(player, "Rooms Refreshing ...");
                        (EditHotel[player.userID.ToString()]).RefreshRooms();

                        SendReply(player, "Rooms Refreshed");
                        break;
                    case "reset":
                        foreach (KeyValuePair<string, Room> pair in (EditHotel[player.userID.ToString()]).rooms)
                        {
                            CodeLock codelock = FindCodeLockByRoomID(pair.Key);
                            if (codelock == null) continue;
                            ResetRoom(codelock, (EditHotel[player.userID.ToString()]), pair.Value);
                        }
                        break;
                    case "radius":
                        if (args.Length == 1)
                        {
                            SendReply(player, "/hotel radius XX");
                            return;
                        }
                        int rad2 = 20;
                        int.TryParse(args[1], out rad2);
                        if (rad2 < 1) rad2 = 20;

                        string[] zoneargs2 = new string[] { "name", editedhotel.hotelname, "radius", rad2.ToString() };
                        ZoneManager.Call("CreateOrUpdateZone", editedhotel.hotelname, zoneargs2);

                        (EditHotel[player.userID.ToString()]).r = rad2.ToString();

                        SendReply(player, string.Format("Radius set to {0}", args[1]));
                        break;

                    default:
                        SendReply(player, string.Format("Wrong argument {0}", args[0]));
                        break;
                }
            }

            ShowHotelGrid(player);
            RefreshAdminHotelGUI(player);
        }
        [ChatCommand("hotel_list")]
        void cmdChatHotelList(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, MessageErrorNotAllowed); return; }
            SendReply(player, "======= Hotel List ======");
            foreach (HotelData hotel in storedData.Hotels)
            {
                SendReply(player, string.Format("{0} - {1}", hotel.hotelname, hotel.rooms.Count.ToString()));
            }
        }

        [ChatCommand("hotel_edit")]
        void cmdChatHotelEdit(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, MessageErrorNotAllowed); return; }
            if (EditHotel.ContainsKey(player.userID.ToString())) { SendReply(player, MessageAlreadyEditing); return; }
            if (args.Length == 0) { SendReply(player, MessageHotelEditHelp); return; }

            string hname = args[0];
            foreach (HotelData hotel in storedData.Hotels)
            {
                if (hotel.hotelname.ToLower() == hname.ToLower())
                {
                    hotel.Deactivate();
                    if (hotel.x != null && hotel.r != null)
                    {
                        foreach (Collider col in UnityEngine.Physics.OverlapSphere(hotel.Pos(), Convert.ToSingle(hotel.r), constructionColl))
                        {
                            Door door = col.GetComponentInParent<Door>();
                            if (door != null)
                            {
                                if (door.HasSlot(BaseEntity.Slot.Lock))
                                {
                                    door.SetFlag(BaseEntity.Flags.Open, false);
                                    door.SendNetworkUpdateImmediate(true);
                                }
                            }
                        }
                    }
                    EditHotel.Add(player.userID.ToString(), hotel);
                    break;
                }
            }

            if (!EditHotel.ContainsKey(player.userID.ToString())) { SendReply(player, string.Format(MessageErrorEditDoesntExist, args[0])); return; }

            SendReply(player, string.Format(MessageHotelEditEditing, EditHotel[player.userID.ToString()].hotelname));

            RefreshAdminHotelGUI(player);
        }

        [ChatCommand("hotel_remove")]
        void cmdChatHotelRemove(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, MessageErrorNotAllowed); return; }
            if (EditHotel.ContainsKey(player.userID.ToString())) { SendReply(player, MessageAlreadyEditing); return; }
            if (args.Length == 0) { SendReply(player, MessageHotelEditHelp); return; }

            string hname = args[0];
            HotelData targethotel = null;
            foreach (HotelData hotel in storedData.Hotels)
            {
                if (hotel.hotelname.ToLower() == hname.ToLower())
                {
                    hotel.Deactivate();
                    targethotel = hotel;
                    break;
                }
            }
            if (targethotel == null) { SendReply(player, string.Format(MessageErrorEditDoesntExist, args[0])); return; }

            storedData.Hotels.Remove(targethotel);
            SaveData();
            SendReply(player, string.Format("Hotel Named: {0] was successfully removed", hname));

        }

        [ChatCommand("hotel_reset")]
        void cmdChatHotelReset(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, MessageErrorNotAllowed); return; }
            if (EditHotel.ContainsKey(player.userID.ToString())) { SendReply(player, MessageAlreadyEditing); return; }

            storedData.Hotels = new HashSet<HotelData>();
            SaveData();
            SendReply(player, "Hotels were all deleted");

        }


        BuildingBlock FindBlockFromRay(Vector3 Pos, Vector3 Aim)
        {
            var hits = UnityEngine.Physics.RaycastAll(Pos, Aim);
            float distance = 100000f;
            BuildingBlock target = null;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<BuildingBlock>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<BuildingBlock>();
                    }
                }
            }
            return target;
        }
        Vector3 RayForDoor(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles);
            BuildingBlock target = FindBlockFromRay(player.eyes.position, currentRot * Vector3.forward);
            if (target == null) return default(Vector3);
            if (target.GetComponent<Door>() == null) return default(Vector3);
            return target.transform.position;
        }
        bool FindRoomByID(string roomid, out HotelData targethotel, out Room targetroom)
        {
            targethotel = null;
            targetroom = null;
            foreach (HotelData hotel in storedData.Hotels)
            {
                if(hotel.rooms.ContainsKey(roomid))
                {
                    targethotel = hotel;
                    targetroom = (hotel.rooms)[roomid];
                    return true;
                }
            }
            return false;
        }
        [ChatCommand("room")]
        void cmdChatRoom(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, MessageErrorNotAllowed); return; }
            string roomid = string.Empty;
            int argsnum = 0;
            if (args.Length > 0)
            {
                string[] roomloc;
                roomloc = (args[0]).Split(':');
                if (roomloc.Length == 3)
                    roomid = args[0];
            }
            if (roomid == string.Empty)
            {
                Vector3 doorpos = RayForDoor(player);
                if (doorpos == default(Vector3))
                {
                    SendReply(player, "You must look at the door of the room or put the roomid");
                    return;
                }
                roomid = string.Format("{0}:{1}:{2}", Mathf.Ceil(doorpos.x).ToString(), Mathf.Ceil(doorpos.y).ToString(), Mathf.Ceil(doorpos.z).ToString());
            }
            else
                argsnum++;
            if (roomid == string.Empty)
            {
                SendReply(player, "Invalid room.");
                return;
            }
            HotelData targethotel = null;
            Room targetroom = null;
            if (!FindRoomByID(roomid, out targethotel, out targetroom))
            {
                SendReply(player, "No room was detected.");
                return;
            }
            if (args.Length - argsnum == 0)
            {
                SendReply(player, string.Format("Room ID is: {0} in hotel: {1}", targetroom.roomid, targethotel.hotelname));
                SendReply(player, "Options are:");
                SendReply(player, "/room \"optional:roomid\" reset => to reset this room");
                //SendReply(player, "/room \"optional:roomid\" give NAME/STEAMID => to give a player this room");
                SendReply(player, "/room \"optional:roomid\" duration XXXX => to set a new duration time for a player (from the time you set the duration)");
                return;
            }
            if (!targethotel.enabled)
            {
                SendReply(player, "This hotel is currently being edited by an admin, you can't manage a room from it");
                return;
            }
            switch (args[argsnum])
            {
                case "reset":
                    ResetRoom(targethotel, targetroom);
                    SendReply(player, string.Format("The room {0} was resetted", targetroom.roomid));
                break;

                case "duration":
                    if (targetroom.renter == null)
                    {
                        SendReply(player, string.Format("The room {0} has currently no renter, you can't set a duration for it", targetroom.roomid));
                        return;
                    }
                    if (args.Length == argsnum + 1)
                    {
                        double timeleft = targetroom.CheckOutTime() - LogTime();
                        SendReply(player, string.Format("The room {0} renter will expire in {1}", targetroom.roomid, targetroom.CheckOutTime() == 0.0 ? "Unlimited" : ConvertSecondsToBetter(timeleft)));
                        return;
                    }
                    double newtimeleft;
                    if (!double.TryParse(args[argsnum + 1], out newtimeleft))
                    {
                        SendReply(player, "/room \"optional:roomid\" duration NEWTIMELEFT");
                        return;
                    }
                    targetroom.checkoutTime = (newtimeleft + LogTime()).ToString();
                    SendReply(player, string.Format("New timeleft for room ID {0} is {1}s", targetroom.roomid, newtimeleft.ToString()));
                break;

                case "give":
                    if (targetroom.renter != null)
                    {
                        SendReply(player, string.Format("The room {0} is already rented by {1}, reset the room first to set a new renter", targetroom.roomid, targetroom.renter));
                        return;
                    }
                    if (args.Length == argsnum + 1)
                    {
                        SendReply(player, "/room \"optional:roomid\" give PLAYER/STEAMID");
                        return;
                    }

                break;

                default:
                    SendReply(player, "This is not a valid option, say /room \"optional:roomid\" to see the options");
                    break;
            }

        }

        [ChatCommand("hotel_new")]
        void cmdChatHotelNew(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, MessageErrorNotAllowed); return; }
            if (EditHotel.ContainsKey(player.userID.ToString())) { SendReply(player, MessageAlreadyEditing); return; }
            if (args.Length == 0) { SendReply(player, MessageHotelNewHelp); return; }

            string hname = args[0];
            if (storedData.Hotels.Count > 0)
            { 
                foreach (HotelData hotel in storedData.Hotels)
                {
                    if (hotel.hotelname.ToLower() == hname.ToLower())
                    {
                        SendReply(player, string.Format(MessageErrorAlreadyExist, hname));
                        return;
                    }
                }
            }
            HotelData newhotel = new HotelData(hname);
            newhotel.Deactivate();
            EditHotel.Add(player.userID.ToString(), newhotel);

            SendReply(player, string.Format(MessageHotelNewCreated, hname));
            RefreshAdminHotelGUI(player);
        }
    }
}