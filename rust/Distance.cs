using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
	[Info("Distance", "ignignokt84", "0.1.1", ResourceId = 1780)]
	class Distance : RustPlugin
	{
		/*
		
		Distance is a measurement plugin designed for zone layout.  Includes point-to-point measurement,
		rangefinder, and a history, and overlays measurement vectors and distances onto the screen for
		easy reference.
		
		TODO:
			intelligently order procedures
			improve rangefinder target identification
			live rangefinding mode
		
		*/
		
		// load default messages to Lang
		void LoadDefaultMessages()
		{
			var messages = new Dictionary<string, string>
			{
				{"ChatCommand", "distance"},
				{"VersionString", "Distance v. {0}"},
				{"DistanceHeader", "---- Distance ----"},
				{"HistoryHeader", "---- History ----"},
				{"UsageHeader", "---- Distance usage ----"},
				{"CmdUsageOrigin", "Set origin to current position"},
				{"CmdUsageGet", "Calculate distance to origin"},
				{"CmdUsageRf", "Calculate distance to target (rangefinder)"},
				{"CmdUsageHistory", "Show last " + histSize + " distances measured"},
				{"CmdUsageClear", "Clear origin, history, or all (both)"},
				{"InvalidParameter", "Invalid Parameter: {0}"},
				{"NoEntries", "No entries"},
				{"OriginSet", "Origin set to {0}"},
				{"OriginNotSet", "Cannot measure distance - origin not set"},
				{"NoTarget", "Unable to find a target"},
				{"ClearOrigin", "Origin cleared"},
				{"ClearHistory", "History cleared"}
			};
			lang.RegisterMessages(messages, this);
        }
		
		private FieldInfo serverinput;
		// TODO - create custom class for player-specific origin/history
		// origin (reference point for distance)
		private Dictionary<BasePlayer,Vector3> origin = new Dictionary<BasePlayer,Vector3>();
		// TODO - expand history data, allow deletion of individual elements
		// history - an array of Vector3[] (both points) paired with the distance result
		private Dictionary<BasePlayer,KeyValuePair<Vector3[],float>[]> history = new Dictionary<BasePlayer,KeyValuePair<Vector3[],float>[]>();
		// enum of valid arg[0] arguments
		public enum FirstArgs { origin, get, rf, history, clear, version };
		// enum of valid arg[1] arguments (clear parameters)
		public enum ClearArgs { origin, history, all };
		// adjustment for eye height for raytrace
		private Vector3 eyesAdjust = new Vector3(0f, 1.6f, 0f);
		// history size
		public const int histSize = 5;
		// TODO add more colors to color table to accommodate larger history sizes
		// history color table - string value paired with Color value
		public KeyValuePair<string,Color>[] histColors = new KeyValuePair<string,Color>[histSize]
			{new KeyValuePair<string,Color>("cyan", Color.cyan),
			 new KeyValuePair<string,Color>("magenta", Color.magenta),
			 new KeyValuePair<string,Color>("green", Color.green),
			 new KeyValuePair<string,Color>("red", Color.red),
			 new KeyValuePair<string,Color>("yellow", Color.yellow)};
		
		// usage information string with formatting
		public string usageString;

		void Loaded()
		{
			LoadDefaultMessages();
			string chatCommand = GetMessage("ChatCommand");
            cmd.AddChatCommand(chatCommand, this, "cmdChatDelegator");
			serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
			
			// build usage string
			usageString = wrapSize(14, wrapColor("orange", GetMessage("UsageHeader"))) + "\n" +
						  wrapSize(12, wrapColor("cyan", "/" + chatCommand + " origin") + " - " + GetMessage("CmdUsageOrigin") + "\n" +
									   wrapColor("cyan", "/" + chatCommand + " get") + " - " + GetMessage("CmdUsageGet") + "\n" +
									   wrapColor("cyan", "/" + chatCommand + " rf") + " - " + GetMessage("CmdUsageRf") + "\n" +
									   wrapColor("cyan", "/" + chatCommand + " history") + " - " + GetMessage("CmdUsageHistory") + "\n" +
									   wrapColor("cyan", "/" + chatCommand + " clear [origin|history|all]") + " - " + GetMessage("CmdUsageClear"));
		}
        
        // get message from Lang
        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
		
		// main delegator process - handles all commands
		void cmdChatDelegator(BasePlayer player, string command, string[] args)
		{
			if(args.Length == 1)
			{
				if(!Enum.IsDefined(typeof(FirstArgs), args[0]))
				{
					notifyInvalidParameter(player, args[0]);
				}
				else
				{
					switch((FirstArgs) Enum.Parse(typeof(FirstArgs), args[0]))
					{
						case FirstArgs.origin:
							setOrigin(player);
							return;
						case FirstArgs.rf:
							rangefinder(player);
							return;
						case FirstArgs.get:
							measure(player);
							return;
						case FirstArgs.history:
							showHistory(player);
							return;
						case FirstArgs.version:
							sendMessage(player, wrapSize(14, wrapColor("orange", String.Format(GetMessage("VersionString"), this.Version.ToString()))));
							return;
					}
				}
			}
			else if(args.Length == 2)
			{
				if(args[0] != "clear")
				{
					notifyInvalidParameter(player, args[0]);
				}
				else
				{
					if(!Enum.IsDefined(typeof(ClearArgs), args[1]))
					{
						notifyInvalidParameter(player, args[1]);
					}
					else
					{
						switch((ClearArgs) Enum.Parse(typeof(ClearArgs), args[1]))
						{
							case ClearArgs.origin:
								sendMessage(player, clearOrigin(player));
								return;
							case ClearArgs.history:
								sendMessage(player, clearHistory(player));
								return;
							case ClearArgs.all:
								sendMessage(player, clearOrigin(player) + "\n" + clearHistory(player));
								return;
						}
					}
				}
			}
			
			showUsage(player);
		}
		
		// send message indicating invalid parameter or command
		void notifyInvalidParameter(BasePlayer player, string message)
		{
			sendMessage(player, wrapSize(14, wrapColor("red", String.Format(GetMessage("InvalidParameter"), message))));
		}
		
		// send message containing distance measured
		void notifyDistance(BasePlayer player, string message)
		{
			sendMessage(player, wrapSize(14, wrapColor("orange", GetMessage("DistanceHeader"))) + "\n" + wrapSize(12, message));
		}
		
		// send message containing history
		void notifyHistory(BasePlayer player, string message)
		{
			sendMessage(player, wrapSize(14, wrapColor("orange", GetMessage("HistoryHeader"))) + wrapSize(12, message));
		}
		
		// show usage information
		void showUsage(BasePlayer player)
		{
			sendMessage(player, usageString);
		}
		
		// set origin to the player's position + 1.0y
		void setOrigin(BasePlayer player)
		{
			var input = serverinput.GetValue(player) as InputState;
			Vector3 pos = player.transform.position + new Vector3(0,1f,0);
			origin[player] = pos;
			sendMessage(player, wrapSize(12, wrapColor("cyan", String.Format(GetMessage("OriginSet"), pos.ToString()))));
		}
		
		// measure distance from player's current position to origin
		void measure(BasePlayer player)
		{
			Vector3 o = Vector3.zero;
			if(!origin.TryGetValue(player, out o))
			{
				// origin not set
				sendMessage(player, wrapSize(12, wrapColor("red", GetMessage("OriginNotSet"))));
				showUsage(player);
				return;
			}
			else
			{
				// get player position + 1.0y
				var input = serverinput.GetValue(player) as InputState;
				Vector3 playerPosition = player.transform.position + new Vector3(0,1f,0);
				// calculate distance
				float distance = Vector3.Distance(o, playerPosition);
				// save position distance information
				addToHistory(player, o, playerPosition, distance);
				// draw line/text and send distance information to player
				string value = wrapColor("cyan", distance.ToString("0.000"));
				draw(player, o, playerPosition, Color.white, 10f, wrapSize(16, value));
				notifyDistance(player, value);
			}
		}
		
		// rangefinder - raycast from eyes to object player is looking at
		void rangefinder(BasePlayer player)
		{
			// get player position + 1.6y as eye-level
			var input = serverinput.GetValue(player) as InputState;
			Vector3 playerEyes = player.transform.position + eyesAdjust;
			var direction = Quaternion.Euler(input.current.aimAngles) * Vector3.forward;
			// raycast in the direction the player is looking
			var hits = UnityEngine.Physics.RaycastAll(playerEyes, direction);
			float closest = 100000f;
			Vector3 target = Vector3.zero;
			Collider collider = null;
			// find the closest hit
			foreach (var hit in hits)
			{
				string name = hit.collider.gameObject.name;
				if(hit.collider.gameObject.layer == 18) // skip Triggers layer
					continue;
				// ignore zones, meshes, and landmark nobuild hits
				if (name.StartsWith("Zone Manager") ||
					name == "prevent_building" ||
					name == "preventBuilding" ||
					name == "Mesh")
					continue;
				
				if (hit.distance < closest)
				{
					closest = hit.distance;
					target = hit.point;
					collider = hit.collider;
				}
			}
			// no target found
			if(target == Vector3.zero)
			{
				sendMessage(player, wrapSize(12, wrapColor("red", GetMessage("NoTarget"))));
				return;
			}
			else
			{
				// target found, calculate distance
				float distance = Vector3.Distance(playerEyes, target);
				// save position distance information
				addToHistory(player, playerEyes, target, distance);
				// draw line/text and send distance information to player
				string targetname = getHitObjectName(collider);
				string value = wrapColor("cyan", targetname + ": " + distance.ToString("0.000"));
				draw(player, playerEyes, target, Color.white, 10f, wrapSize(16, value));
				notifyDistance(player, value);
			}
		}
		
		// determines what name to display to the player based on the object type/layer
		string getHitObjectName(Collider collider)
		{
			GameObject gameObject = collider.gameObject;
			string output = prettify(gameObject.name);
			
			// debugging printouts
			//Puts("name: " + output);
			//Puts("layer: " + gameObject.layer);
			
			//for(int i = 0; i < 32; i++) { // get layer names
			//	Puts("layer[" + i + "] = " + LayerMask.LayerToName(i));
			//}
			
			//foreach(Component component in gameObject.GetComponents(typeof(Component)))
			//{
			//	Puts("component: " + component.name);
			//}
			
			switch(gameObject.layer)
			{
				case 21:
					// deployable
					if(output != "Mesh Collider Batch")
						return output;
					return LayerMask.LayerToName(gameObject.layer);
				case 4:
					// terrain
					return LayerMask.LayerToName(gameObject.layer);
				case 15:
					// water
					return LayerMask.LayerToName(gameObject.layer);
				case 23:
					// construction
					return LayerMask.LayerToName(gameObject.layer);
				case 8:
					// debris
					return fixName(output);
				case 26:
					// prefabs?
					return fixName(gameObject.name);
			}
			
			return output;
		}
		
		// fixes worldmodel object naming
		string fixName(string str)
		{
			str = Regex.Replace(str.Replace("worldmodel(Clone)",""),"[\\._-]"," ");
			return properCase(str);
		}
		
		// fixes messy names (like prefab paths)
		string prettify(string str)
		{
			str = Regex.Replace(str.Split('/').Last().ToLower().Replace(".prefab",""),"[0-9\\(\\)]","").Replace('_',' ').Replace('-',' ');
			
			return properCase(str).Trim(' ');
		}
		
		// convert string to proper case
		string properCase(string str)
		{
			char[] newChars = new char[str.Length];
			char last = ' ';
			for(int i=0; i<str.Length; i++)
			{
				if(last == ' ')
					if(i == str.Length-1)
						newChars[i] = ' ';
					else
						newChars[i] = Char.ToUpper(str[i]);
				else
					newChars[i] = str[i];
				last = newChars[i];
			}
			return new String(newChars);
		}
		
		// clear origin setting
		string clearOrigin(BasePlayer player)
		{
			origin.Remove(player);
			return wrapSize(12, wrapColor("green", GetMessage("ClearOrigin")));
		}
		
		// clear history
		string clearHistory(BasePlayer player)
		{
			history.Remove(player);
			return wrapSize(12, wrapColor("green", GetMessage("ClearHistory")));
		}
		
		// add distance information to history array
		private void addToHistory(BasePlayer player, Vector3 p1, Vector3 p2, float distance)
		{
			KeyValuePair<Vector3[],float>[] hist;
			// initialize history if null
			if(!history.TryGetValue(player, out hist))
				hist = new KeyValuePair<Vector3[], float>[histSize];
			// shift history entries
			int i;
			for(i=hist.Length-1; i>0; i--)
			{
				hist[i] = hist[i-1];
			}
			// insert new distance information entry at index 0 of history
			hist[0] = new KeyValuePair<Vector3[],float>(new Vector3[] {p1, p2}, distance);
			history[player] = hist;
		}
		
		// display history information
		void showHistory(BasePlayer player)
		{
			bool histExists = false;
			string output = "";
			
			KeyValuePair<Vector3[],float>[] hist;
			if(!history.TryGetValue(player, out hist))
			{
				// history array is empty
				output = "\n" + wrapColor("red", GetMessage("NoEntries"));
				notifyHistory(player, output);
				return;
			}
			// loop through history array, draw lines and distances, and add values to output string
			for(int i=0; i<hist.Length; i++)
			{
				if(hist[i].Value > 0)
				{
					string value = hist[i].Value.ToString("0.000");
					output += "\n" + wrapColor(histColors[i].Key, (i+1) + ": " + value);
					draw(player, hist[i].Key[0], hist[i].Key[1], histColors[i].Value, 10f, wrapSize(16, wrapColor(histColors[i].Key,value)));
					histExists = true;
				}
			}
			// no entries
			if(!histExists)
				output = "\n" + wrapColor("red", GetMessage("NoEntries"));
			// send output string to player
			notifyHistory(player, output);
		}
		
		//  wrapper to send message to user (placeholder in case additional handling is needed)
		void sendMessage(BasePlayer player, string message)
		{
			SendReply(player, message);
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
		
		// draw a line on the screen between two points, delegate to drawText if text is passed
		static void draw(BasePlayer player, Vector3 from, Vector3 to, Color color, float duration, string text)
		{
			player.SendConsoleCommand("ddraw.line", duration, color, from, to);
			// if text string is not null or empty, calculate text position as midpoint of line + 0.1y (to place text above line)
			if(text != null && text != "")
			{
				Vector3 textPosition = ((from + to)/2f); // midpoint of line
				textPosition = textPosition + new Vector3(0,0.1f,0); // shift text 0.1y
				drawText(player, textPosition, color, duration, text);
			}
		}
		
		// draw text on the screen at the specified position
		static void drawText(BasePlayer player, Vector3 position, Color color, float duration, string text)
		{
			player.SendConsoleCommand("ddraw.text", duration, color, position, text);
		}
		
		object OnReject(Network.Connection conn, string reason)
		{
			return false;
		}
	}
}