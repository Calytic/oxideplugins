using System;                      //DateTime
using System.Reflection;           //Required for BindFlag
using System.Data;
using System.Linq;
using System.Collections.Generic;  //Required for Whilelist
using UnityEngine;
//using Rust;
using Oxide.Core;                  //Interface
//using Oxide.Core.Plugins;


namespace Oxide.Plugins
{
	[Info("Thief", "Alphawar", "0.5.0", ResourceId = 1503)]
	[Description("Mod for handling Thief class.")]
    class ThiefAPI : RustPlugin
	{
		///////////////////////////////////////
		///     Config, Values and Data     ///
		///////////////////////////////////////
		//Creates Blank Values to be loaded later
		private int Cooldown;
		private int Pass;
		private int LPchance;
		private int RandomNumber;
		private int DamageTicks;
		private int pickCostFM;
		private int pickCostSE;
		private float DamageAmount;
		private string NotAllowed;
		private string Allowed = "You start to pick the lock.";
		private string Failed;
		private string Success;
		private string MaxedAttempts = "My hands need a rest.";
		private string ChatPrefixColor = "008800";
        private string ChatPrefix = "Server";
		private string PickEnabledMessage = "Lock Pick Mode Enabled";
		private string PickDisabledMessage = "Lock Pick Mode Disabled";
		private string CooldownMessage;
		private bool PickEnabled;
		
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
		
		protected override void LoadDefaultConfig()
		{
			Puts("Creating a new configuration file!");
			Config.Clear();
			LoadVariables();
		}
		
		void LoadVariables() //Stores Default Values, calling GetConfig passing: menu, dataValue, defaultValue
		{
			//Booleans
			PickEnabled = Convert.ToBoolean(GetConfig("Settings", "PickEnabled", false));
			//Ints
			pickCostFM = Convert.ToInt32(GetConfig("Values", "MetalFragCost", 250));
			pickCostSE = Convert.ToInt32(GetConfig("Values", "StoneCost", 150));
			DamageTicks = Convert.ToInt32(GetConfig("Values", "DamageTicks", 50));
			LPchance = Convert.ToInt32(GetConfig("Values", "LPchance", 15));
			Cooldown = Convert.ToInt32(GetConfig("Values", "Cooldown", 25));
			//Floats
			DamageAmount = Convert.ToSingle(GetConfig("Values", "DamageAmount", 0.2f));
			//Strings
			NotAllowed = Convert.ToString(GetConfig("Messages", "NotAllowed", "You are not a thief."));
			Failed = Convert.ToString(GetConfig("Messages", "Failed", "You failed to pick the lock."));
			Success = Convert.ToString(GetConfig("Messages", "Success", "You have gained access to the door."));
			CooldownMessage = Convert.ToString(GetConfig("Messages", "ShortCooldown", "I cant do that right now"));
		}
		
		Hash<ulong, PlayerInfo> Thiefs = new Hash<ulong, PlayerInfo>();
		
        class PlayerInfo
        {
            public string UserId;
            public string Name;

            public PlayerInfo()
            {
            }

            public PlayerInfo(BasePlayer player)
            {
                UserId = player.userID.ToString();
                Name = player.displayName;
            }

            public ulong GetUserId()
            {
                ulong user_id;
                if (!ulong.TryParse(UserId, out user_id)) return 0;
                return user_id;
            }
        }
		
		class StoredData
		{
			public Dictionary<ulong, double> canpick = new Dictionary<ulong, double>();
			public HashSet<PlayerInfo> Thiefs = new HashSet<PlayerInfo>();
			
			public StoredData()
            {
            }
		}
		
		StoredData storedData;
		
		///////////////////////////////
		///   plugin initiation     ///
		///////////////////////////////
		System.Reflection.FieldInfo whitelistPlayers = typeof(CodeLock).GetField("whitelistPlayers", BindingFlags.NonPublic | BindingFlags.Instance);
		
        private void Loaded()
        {
			LoadVariables();
			if (!permission.PermissionExists("ThiefAPI.can"))
			{
				permission.RegisterPermission("ThiefAPI.can", this);
				Puts("ThiefAPI.can permission registered");
				Puts("only people with permission ThiefAPI.can can apply for the role");
			}
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("ThiefAPI"); 
        }
		
		[ChatCommand("lp")]
		void picklock(BasePlayer player, string cmd, string[] args)
		{
			if (!IsAllowed(player, "ThiefAPI.can", NotAllowed)) return;
			
			if (Thiefs[player.userID] != null)
			{
				DisableThiefMode(player);
				SendChatMessage(player, PickDisabledMessage);
			}
			else
			{
				EnableThiefMode(player);
				SendChatMessage(player, PickEnabledMessage);
			}
			
		}
		[ChatCommand("lpwipe")]
		void picklockwipe(BasePlayer player, string cmd, string[] args)
		{
			if (!IsAllowed(player, "ThiefAPI.can", NotAllowed)) return;
			storedData.canpick.Clear();
			Interface.GetMod().DataFileSystem.WriteObject("ThiefAPI", storedData);
		}
		
		object CanUseDoor(BasePlayer player, BaseLock codeLock)
		{
			ulong steamID = player.userID;
			double nextpicktime;
			//bool canBeThief;
			bool PassFail;
			if (!IsAllowed(player, "ThiefAPI.can","null")) return null;
            //SendChatMessage(player, NotAllowed);
			if (Thiefs[player.userID] != null){
				
				if (!storedData.canpick.TryGetValue(steamID, out nextpicktime))
				{
					SendChatMessage(player, "UserDataCreated");
					storedData.canpick.Add(steamID, GetTimeStamp() + Cooldown);
					Interface.GetMod().DataFileSystem.WriteObject("ThiefAPI", storedData);
				}
				
				if (codeLock is CodeLock)
				{
					List<ulong> whitelist = (List<ulong>)whitelistPlayers.GetValue(codeLock);
					if (whitelist.Contains(player.userID))
					{
						SendChatMessage(player, "You have the code");
						return null;
					}
				}
				
				if (GetTimeStamp() < nextpicktime)
				{
					int nexttele = Convert.ToInt32(GetTimeStamp() - nextpicktime);
					SendChatMessage(player, CooldownMessage);
					return null;
				}
				bool lockPickCostPass=lockPickCost(player);
				if (lockPickCostPass==true)
				{
					string debugmessage = Convert.ToString(lockPickCostPass);
					storedData.canpick[steamID] = GetTimeStamp() + Cooldown;
					Interface.GetMod().DataFileSystem.WriteObject("ThiefAPI", storedData);
					bool Pass=LPRoll();
					if (Pass==true)
					{
						SendChatMessage(player, Success);
						return true;
					}
					else
					{
						SendChatMessage(player, Failed);
						timer.Repeat(0.2f, DamageTicks, () =>
						{
							player.Hurt(DamageAmount);
						});
						return false;
					}
				}
			}
			return null;
		}

		
		///////////////////////////////
		///      plugin Logic       ///
		///////////////////////////////
		
		private bool lockPickCost(BasePlayer player)
        {
            int playerFM = player.inventory.GetAmount(688032252); // Frag metal
            int playerRk = player.inventory.GetAmount(3506021); // Spawn rock
            int playerSE = player.inventory.GetAmount(-892070738); // Stones
            {
                if ((playerFM >= pickCostFM) && (playerRk >= 1) && (playerSE >= pickCostSE)) // buyFlare/buyTarget is my config option for amount
                {
                    player.inventory.Take(null, 688032252, pickCostFM); // Take the specified amount of item
                    player.inventory.Take(null, -892070738, pickCostSE);
                    //callPaymentStrike(player); // Run function, not yet needed, may use to run code, as it only returns true or false atm.
                    return true;
                }
				else 
				{
					SendChatMessage(player, "You Dont have the inventory");
					return false;
				}
            }
		}

		bool LPRoll()
		{
			//create logic to roll LP pass/fail
			RandomNumber = UnityEngine.Random.Range(0, 101);
			if (RandomNumber < LPchance)
			{
				return true;
			}
			return false;
		}
		bool IsAllowed(BasePlayer player, string perm, string reason)
        {
            if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
			if (reason != "null")
				SendChatMessage(player, reason);
            return false;
        }

		double GetTimeStamp()
		{
			return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds ;
		}
		
        void DisableThiefMode(BasePlayer player)
        {
            storedData.Thiefs.RemoveWhere(info => info.GetUserId() == player.userID);
            Thiefs.Remove(player.userID);
        }
		
        void EnableThiefMode(BasePlayer player)
        {
            var info = new PlayerInfo(player);
            storedData.Thiefs.Add(info);
            Thiefs[player.userID] = info;
        }
		
		//Function to send messages to player
		void SendChatMessage(BasePlayer player, string message, string args = null) => PrintToChat(player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: {message}", args);
	}
}