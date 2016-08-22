using System.Collections.Generic;
using System;
using Rust;

namespace Oxide.Plugins
{
	[Info("MagicMeat", "ignignokt84", "0.1.1", ResourceId = 2011)]
	class MagicMeat : RustPlugin
	{
		void LoadDefaultMessages()
		{
			var messages = new Dictionary<string, string>
			{
				{"ConsoleCommand", "magicmeat"},
				{"VersionString", "MagicMeat v. {0}"},
				
				{"UsageHeader", "---- MagicMeat usage ----"},
				{"CmdUsageSet", "Set value of specified option"},
				{"CmdUsageGet", "Get value of specified option"},
				{"CmdUsageDef", "Loads default configuration"},
				{"CmdUsageVersion", "Prints version information"},
				{"CmdUsageOptionString", "[option]"},
				{"CmdUsageValueString", "[value]"},
				{"InvalidParameter", "Invalid parameter: {0}"},
				{"InvalidParamForCmd", "Invalid parameters for command \"{0}\""},
				{"NoPermission", "Cannot execute command: No permission"},
				{"SetSuccess", "Successfully set \"{0}\" to \"{1}\""},
				{"DefConfigLoad", "Loaded default configuration"}
			};
			lang.RegisterMessages(messages, this);
		}
		private Dictionary<Option,object> data = new Dictionary<Option,object>();
		// has config changed?
		private bool hasConfigChanged;
		// usage information string with formatting
		public string usageString;
		// command enum
		private enum Command { usage, set, get, version, def };
		// option enum
		private enum Option { pickup, store };
		// default values array
		private object[] def = { true, false };
		
		private Dictionary<int,int> recipes = new Dictionary<int,int>() {
			{1325935999, -2043730634},	// bear meat
			{-253819519, 991728250},	// pork
			{-1658459025, 1734319168},	// chicken
			{179448791, -1691991080},	// wolf meat
			{-533484654, -2078972355},	// fish
			{-642008142, -991829475}	// human meat
		};
		
		// load
		void Loaded()
		{
			LoadDefaultMessages();
			// build commands based on enum values
			string baseCommand = GetMessage("ConsoleCommand");
			foreach(Command command in Enum.GetValues(typeof(Command)))
				cmd.AddConsoleCommand((baseCommand + "." + command.ToString()), this, "ccmdDelegator");
			
			LoadConfig();
			// build usage string
			usageString = wrapSize(14, wrapColor("orange", GetMessage("UsageHeader"))) + "\n" +
						  wrapSize(12, wrapColor("cyan", baseCommand + "." + Command.set.ToString() + " " + GetMessage("CmdUsageOptionString") + " " + GetMessage("CmdUsageValueString")) + " - " + GetMessage("CmdUsageSet") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.get.ToString() + " " + GetMessage("CmdUsageOptionString")) + " - " + GetMessage("CmdUsageGet") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.def.ToString()) + " - " + GetMessage("CmdUsageDef") + "\n" +
									   wrapColor("cyan", baseCommand + "." + Command.version.ToString()) + " - " + GetMessage("CmdUsageVersion"));
		}
		
        // get message from Lang
        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
		
		// delegation method for console commands
		//[ConsoleCommand("tpve")]
		void ccmdDelegator(ConsoleSystem.Arg arg)
		{
			// user doesn't have access to run console command
			if(!hasAccess(arg)) return;
			
			string cmd = arg.cmd.namefull.Split('.')[1];
			if(!Enum.IsDefined(typeof(Command), cmd))
			{
				// shouldn't hit
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), cmd))));
			}
			else
			{
				switch((Command) Enum.Parse(typeof(Command), cmd))
				{
					case Command.version:
						SendReply(arg, wrapSize(14, wrapColor("orange", String.Format(GetMessage("VersionString"), this.Version.ToString()))));
						return;
					case Command.def:
						LoadDefaultConfig();
						SendReply(arg, wrapSize(12, wrapColor("green", GetMessage("DefConfigLoad"))));
						return;
					case Command.set:
						if(handleSet(arg)) return;
						break;
					case Command.get:
						if(handleGet(arg)) return;
						break;
					case Command.usage:
						showUsage(arg);
						return;
				}
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParamForCmd"), arg.cmd.namefull))));
			}
			showUsage(arg);
		}
		
		// handle set command
		private bool handleSet(ConsoleSystem.Arg arg)
		{
			if(!Enum.IsDefined(typeof(Option), arg.Args[0]))
			{
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), arg.Args[0]))));
				return false;
			}
			
			Option opt = (Option) Enum.Parse(typeof(Option), arg.Args[0]);
			object value;
			try {
				value = Convert.ToBoolean(arg.Args[1]);
			} catch(FormatException e) {
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), arg.Args[1]))));
				return false;
			}
			
			SaveEntry(opt,value);
			SendReply(arg, wrapSize(12, wrapColor("cyan", String.Format(GetMessage("SetSuccess"), new object[] {opt, value}))));
			return true;
		}
		
		// handle get command
		private bool handleGet(ConsoleSystem.Arg arg)
		{
			if(arg.Args[0] == "all")
			{
				foreach(Option option in Enum.GetValues(typeof(Option)))
				{
					printValue(arg, option);
				}
				return true;
			}
			if(!Enum.IsDefined(typeof(Option), arg.Args[0]))
			{
				SendReply(arg, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), arg.Args[0]))));
				return false;
			}
			Option opt = (Option) Enum.Parse(typeof(Option), arg.Args[0]);
			printValue(arg, opt);
			return true;
		}
		
		// show usage information
		void showUsage(ConsoleSystem.Arg arg)
		{
			SendReply(arg, usageString);
		}
		
		// prints the value of an Option
		private void printValue(ConsoleSystem.Arg arg, Option opt)
		{
			SendReply(arg, wrapSize(12, wrapColor("cyan", opt + ": ") + data[opt]));
		}
		
		// loads default configuration
		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadConfig();
		}
		
		// loads config from file
		private void LoadConfig()
		{
			foreach(Option opt in Enum.GetValues(typeof(Option)))
				data[opt] = Convert.ToBoolean(GetConfig(opt, def[(int)opt]));
			
			if (!hasConfigChanged) return;
			SaveConfig();
			hasConfigChanged = false;
		}
		
		// get config options, or set to default value if not found
		private object GetConfig(object opt, object defaultValue)
		{
			string optstr = opt.ToString();
			object value = Config[optstr];
			if (value == null)
			{
				value = defaultValue;
				Config[optstr] = value;
				hasConfigChanged = true;
			}
			return value;
		}
		
		// save updated entry to config
		private void SaveEntry(Option opt, object value)
		{
			string optstr = opt.ToString();
			data[opt] = value;
			Config[optstr] = value;
			SaveConfig();
		}
		
		// handle conversion of items
		void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			// check if item is in recipe dictionary
			if(!recipes.ContainsKey((int) item.info.itemid))
				return; // item not found
			
			// get details of item
			int cookedId = recipes[(int) item.info.itemid];
			int amount = item.amount;
			int position = item.position;
			
			// handle pickup (player inventory)
			if(getBool(Option.pickup) && container.playerOwner != null)
			{
				// item added to player inventory
				item.RemoveFromContainer();
				Item cookedMeat = ItemManager.CreateByItemID(cookedId, amount);
				cookedMeat.AddOwners(item.owners);
				if (!cookedMeat.MoveToContainer(container, position, true))
					cookedMeat.Remove(0f);
			}
			
			// handle storage (other inventory)
			if(getBool(Option.store) && container.entityOwner != null)
			{
				// item added to storage box
				item.RemoveFromContainer();
				Item cookedMeat = ItemManager.CreateByItemID(cookedId, amount);
				cookedMeat.AddOwners(item.owners);
				if (!cookedMeat.MoveToContainer(container, position, true))
					cookedMeat.Remove(0f);
			}
			
		}
		
		// wrap a string in a <size> tag with the passed size
		static string wrapSize(int size, string input)
		{
			if(input == null || input == "")
				return input;
			return "<size=" + size + ">" + input + "</size>";
		}
		
		// wrap a string in a <color> tag with the passed color
		static string wrapColor(string color, string input)
		{
			if(input == null || input == "" || color == null || color == "")
				return input;
			return "<color=" + color + ">" + input + "</color>";
		}
		
		// convert Option value to bool
		private bool getBool(Option opt)
		{
			return Convert.ToBoolean(data[opt]);
		}
		
		// convert Option value to float
		private float getFloat(Option opt)
		{
			return Convert.ToSingle(data[opt]);
		}
		
		// send message to player
		void sendMessage(BasePlayer player, string message)
		{
			//if(checkPopup())
			//	PopupNotifications.Call("CreatePopupNotification", message, player);
			//else
				SendReply(player, message);
		}
		
		// is admin
        private bool isAdmin(BasePlayer player)
        {
        	if (player == null) return false;
            if (player?.net?.connection == null) return true;
            return player.net.connection.authLevel > 0;
        }
		
		// check user access
		bool hasAccess(ConsoleSystem.Arg arg)
		{
			if (arg.connection != null)
			{
				if (arg.connection.authLevel < 1)
				{
					SendReply(arg, GetMessage("NoPermission"));
					return false;
				}
			}
			return true;
		}
	}
}