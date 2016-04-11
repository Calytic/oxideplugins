using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using Facepunch;

namespace Oxide.Plugins
{
	[Info("BuildingWrapper", "ignignokt84", "0.1.2", ResourceId = 1798)]
	class BuildingWrapper : RustPlugin
	{
		/*
		BuildingWrapper helper plugin for ZoneManager
		
		Allows players to neatly and automatically wrap buildings in zones
		*/
		
		// load default messages to Lang
		void LoadDefaultMessages()
		{
			var messages = new Dictionary<string, string>
			{
				{"ChatCommand", "bw"},
				{"VersionString", "BuildingWrapper v. {0}"},
				
				{"UsageHeader", "---- BuildingWrapper usage ----"},
				{"CmdUsageWrap", "Wrap new or existing zone around the building being looked at"},
				{"CmdUsageParamZoneId", "Note: [zone_id] is required, but can be entered as \"auto\" for automatic generation"},
				
				{"NoZoneManager", "ZoneManager not detected - BuildingWrapper disabled"},
				{"NoPermission", "You do not have permission to use this command"},
				
				{"NotSupported", "The command \"{0}\" is not currently supported"},
				
				{"InvalidParameter", "Invalid Parameter: {0}"},
				{"MissingZoneId", "Missing value for required parameter zone_id (use \"auto\" for automatic assignment)"},
				{"InvalidBuffer", "Invalid buffer value: {0}"},
				{"NoBuilding", "No building detected"},
				
				{"ZoneWrapSuccess", "Successfully created/updated zone {0}"},
				{"ZoneWrapFailure", "Failed to create/update zone {0}"}
			};
			lang.RegisterMessages(messages, this);
		}
		
		// ZoneManager base permission
		private const string ZoneManagerPermZone = "zonemanager.zone";

		// link to ZoneManager
		[PluginReference]
		Plugin ZoneManager;
		
		static FieldInfo serverinput;
		private readonly FieldInfo instancesField = typeof(MeshColliderBatch).GetField("instances", BindingFlags.Instance | BindingFlags.NonPublic);
        private int layerMasks = LayerMask.GetMask("Construction", "Construction Trigger", "Trigger");
		// usage information string with formatting
		public string usageString;
		// command enum
		private enum Command { usage, wrap, extend};
		// option enum
		private enum Option { box, sphere };
		// vertical height adjustment
		private const float yAdjust = 2f;
		
		// load
		void Loaded()
		{
			LoadDefaultMessages();
			string chatCommand = GetMessage("ChatCommand");
			cmd.AddChatCommand(chatCommand, this, "cmdChatDelegator");
			
			// build usage string
			usageString = wrapSize(14, wrapColor("orange", GetMessage("UsageHeader"))) + "\n" +
						  wrapSize(12, wrapColor("cyan", "/" + chatCommand + " " + Command.wrap + " [zone_id] <box|sphere> <buffer>") + " - " + GetMessage("CmdUsageWrap") + "\n" +
						  wrapColor("yellow", GetMessage("CmdUsageParamZoneId")));// + "\n" +
									   //wrapColor("cyan", "/" + chatCommand + " " + Command.update + " [zone_id] <box|sphere> <buffer>") + " - " + GetMessage("CmdUsageUpdate"));
			
			serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
		}
		
		// server initialized
		private void OnServerInitialized()
		{
			if(ZoneManager == null)
				PrintError(GetMessage("NoZoneManager"));
			// don't use popups yet
			//usePopups = (PopupNotifications != null);
		}

		// get message from Lang
		string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
		
		// print usage string
		void showUsage(BasePlayer player)
		{
			SendReply(player, usageString);
		}
		
		// main delegator process - handles all commands
		void cmdChatDelegator(BasePlayer player, string command, string[] args)
		{
			if(ZoneManager == null)
			{
				SendReply(player, wrapSize(12, wrapColor("red", GetMessage("NoZoneManager"))));
				return;
			}
			if(!hasPermission(player, ZoneManagerPermZone))
			{
				SendReply(player, wrapSize(12, wrapColor("red", GetMessage("NoPermission"))));
				return;
			}
			if(args == null || args.Length == 0)
				showUsage(player);
			else if(!Enum.IsDefined(typeof(Command), args[0]))
				SendReply(player, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidParameter"), args[0]))));
			else
			{
				switch((Command) Enum.Parse(typeof(Command), args[0]))
				{
					case Command.wrap:
						break;
					case Command.extend:
						SendReply(player, wrapSize(12, wrapColor("red", String.Format(GetMessage("NotSupported"),"extend"))));
						return;
					case Command.usage:
						showUsage(player);
						return;
				}
				
				bool update = false;
				float buffer = 1.0f;
				Option opt = Option.sphere;
				if(args.Length < 2 || args[1] == "")
				{
					SendReply(player, wrapSize(12, wrapColor("red", GetMessage("MissingZoneId"))));
					return;
				}
				
				string zoneId = args[1];
				if(zoneId == null || zoneId == "auto")
					zoneId = UnityEngine.Random.Range(1, 99999999).ToString();
				int i = 2;
				if(i < args.Length)
					if(args[i] == "box" || args[i] == "sphere")
						opt = (Option) Enum.Parse(typeof(Option), args[i++]);
				if(i < args.Length)
					try {
						buffer = Convert.ToSingle(args[i++]);
					} catch(FormatException e) {
						SendReply(player, wrapSize(12, wrapColor("red", String.Format(GetMessage("InvalidBuffer"), args[i-1]))));
					}
				//float timestart = UnityEngine.Time.realtimeSinceStartup;
				WrapBuilding(player, zoneId, opt, buffer);
				//float timeend = UnityEngine.Time.realtimeSinceStartup;
				//Puts("wrap time: " + (timeend-timestart) + "s");
			}
		}
		
		// wrap building delegator
		void WrapBuilding(BasePlayer player, string zoneId, Option shape, float buffer)
		{
			object closestEntity;
			if(!GetRaycastTarget(player, out closestEntity))
			{
				SendReply(player, wrapSize(12, wrapColor("red", GetMessage("NoBuilding"))));
				return;
			}
			BuildingBlock initialBlock = closestEntity as BuildingBlock;
			if(initialBlock == null)
			{
				SendReply(player, wrapSize(12, wrapColor("red", GetMessage("NoBuilding"))));
				return;
			}
			HashSet<BuildingBlock> all_blocks;
			if(!GetStructure(initialBlock, out all_blocks))
			{
				SendReply(player, wrapSize(12, wrapColor("red", GetMessage("NoBuilding"))));
				return;
			}
			
			bool success = false;
			if(shape == Option.box)
				success = WrapBox(player, zoneId, all_blocks, buffer);
			else if(shape == Option.sphere)
				success = WrapSphere(zoneId, all_blocks, buffer);
			
			string str = success ? "Success" : "Failure";
			
			SendReply(player, wrapSize(12, wrapColor(success ? "cyan" : "red", String.Format(GetMessage("ZoneWrap"+str), zoneId))));
		}
		
		// raycast and return the closest entity - returns false if a valid entity is not found
		// amalgamation of processes from CopyPaste, and some adjustments
		bool GetRaycastTarget(BasePlayer player, out object closestEntity)
		{
			closestEntity = false;
			var input = serverinput.GetValue(player) as InputState;
			if (input == null || input.current == null || input.current.aimAngles == Vector3.zero)
				return false;
			
			Vector3 sourceEye = player.transform.position + new Vector3(0f, 1.6f, 0f);
			Ray ray = new Ray(sourceEye, Quaternion.Euler(input.current.aimAngles) * Vector3.forward);
			
			var hits = Physics.RaycastAll(ray);
			float closestdist = 100f;
			foreach (var hit in hits)
			{
				if (hit.collider.isTrigger)
					continue;
				if (hit.distance < closestdist)
				{
					closestdist = hit.distance;
					closestEntity = hit.GetEntity();
				}
			}
			if (closestEntity is bool)
				return false;
			return true;
		}
		
		// get all BuildingBlock entities in structure
		// basic process replicated from CopyPaste, reduced to only handle BuildingBlocks
		bool GetStructure(BuildingBlock initialBlock, out HashSet<BuildingBlock> structure)
		{
			structure = new HashSet<BuildingBlock>();
			List<Vector3> checkFrom = new List<Vector3>();
			BuildingBlock fbuildingblock;
			
			checkFrom.Add(initialBlock.transform.position);
			structure.Add(initialBlock);

			int current = 0;
			while (true)
			{
				current++;
				if (current > checkFrom.Count)
					break;
				List<BaseEntity> list = Pool.GetList<BaseEntity>();
				Vis.Entities<BaseEntity>(checkFrom[current - 1], 3f, list, layerMasks);
				for (int i = 0; i < list.Count; i++)
				{
					BaseEntity hit = list[i];
					if (hit.GetComponentInParent<BuildingBlock>() != null)
					{
						fbuildingblock = hit.GetComponentInParent<BuildingBlock>();
						if (!(structure.Contains(fbuildingblock)))
						{
							checkFrom.Add(fbuildingblock.transform.position);
							structure.Add(fbuildingblock);
						}
					}
				}
			}
			return true;
		}
		
		// wrap building in a box zone
		bool WrapBox(BasePlayer player, string zoneId, HashSet<BuildingBlock> blocks, float buffer)
		{
			float minY =  Mathf.Infinity;
			float maxY = -Mathf.Infinity;
			
			Vector2 origin = new Vector2(0f, 0f); // origin, for rotations
			
			float extents = 0f;
			HashSet<Vector2> points = new HashSet<Vector2>();
			for(int i=0; i<blocks.Count(); i++)
			{
				Bounds b = blocks.ElementAt(i).WorldSpaceBounds().ToBounds();
				//float d = b.max;
				Vector3 v = blocks.ElementAt(i).CenterPoint();
				
				if(v == Vector3.zero)
					continue; // cannot get position?
				
				float d = Mathf.Abs(Vector3.Distance(v, b.max))*2f;
				if(d > extents) extents = d; // set max extents for buffering zone
				
				// flatten point to horizontal plane
				points.Add(new Vector2(v.x, v.z));
				
				if (v.y < minY) minY = v.y;
				if (v.y > maxY) maxY = v.y;
			}
			// get height (vertical axis)
			float sizeY = maxY - minY;

			// calculate hull
			Vector2[] hull = constructHull(points.ToArray());
			// extract center point from hull
			Vector2 center2 = getCenterFromHull(hull);
			Vector3 center = new Vector3(center2.x, minY + sizeY/2f, center2.y);
			//drawHull(player, hull, center); // draw hull for debugging
			
			// finds the smallest rectangle by traversing each edge in the hull,
			// rotating the hull to align the edge with the x axis, then finding
			// the minimum and maximum x and y values, and computing the area
			float minArea = Mathf.Infinity;
			float bestX = 0f;
			float bestY = 0f;
			float bestAngle = 0f;
			Vector2 bestCenter = center2; // initial "best" center point
			for(int i=0; i<hull.Length; i++)
			{
				int j = i+1;
				if(j == hull.Length)
					j = 0; // wrap j to first hull point
				
				// determine angle of current line segment
				float angle = getAngle(hull[i], hull[j]);
				// translate all points by rotating them by the specified angle
				Vector2[] rPoints = rotateAll(hull, angle, origin);
				float min_X =  Mathf.Infinity;
				float max_X = -Mathf.Infinity;
				float min_Y =  Mathf.Infinity;
				float max_Y = -Mathf.Infinity;
				// find min and max x/y values from points
				for(int k = 0; k<rPoints.Length; k++)
				{
					if(rPoints[k].x < min_X) min_X = rPoints[k].x;
					if(rPoints[k].x > max_X) max_X = rPoints[k].x;
					if(rPoints[k].y < min_Y) min_Y = rPoints[k].y;
					if(rPoints[k].y > max_Y) max_Y = rPoints[k].y;
				}
				float x = max_X - min_X;
				float y = max_Y - min_Y;
				float area = x*y;
				if(area < minArea)
				{
					// smallest area so far - save key values
					minArea = area;
					bestX = x;
					bestY = y;
					bestAngle = angle;
					bestCenter = rotate(new Vector2(min_X + x/2f, min_Y + y/2f), -angle, origin);
					// draw box for debugging
					//drawHull(player, rotateAll(new Vector2[] {new Vector2(max_X, max_Y),
					//							   new Vector2(max_X, min_Y),
					//							   new Vector2(min_X, min_Y),
					//							   new Vector2(min_X, max_Y)}, -angle, origin), new Vector3(bestCenter.x, center.y, bestCenter.y));
				}
			}
			
			// add buffer value to extents
			extents += buffer;
			// convert angle to degrees
			bestAngle *= Mathf.Rad2Deg;
			// draw center point for debugging
			//drawCenter(player, new Vector3(bestCenter.x, center.y, bestCenter.y));
			
			// create zone with parameters: zoneId, args, position
			return (bool) ZoneManager?.Call("CreateOrUpdateZone", new object[] {zoneId,
																				new string[] { "size", (bestX+extents) + " " + (sizeY+extents) + " " + (bestY+extents),
																							   "rotation", bestAngle.ToString()},
																				new Vector3(bestCenter.x, center.y+yAdjust, bestCenter.y)
																				});
		}
		
		// wrap building in a sphere zone
		bool WrapSphere(string zoneId, HashSet<BuildingBlock> blocks, float buffer)
		{
			float minX =  Mathf.Infinity;
			float maxX = -Mathf.Infinity;
			float minY =  Mathf.Infinity;
			float maxY = -Mathf.Infinity;
			float minZ =  Mathf.Infinity;
			float maxZ = -Mathf.Infinity;
			for(int i=0; i<blocks.Count(); i++)
			{
				Vector3 v = blocks.ElementAt(i).CenterPoint();
				if(v == Vector3.zero)
					continue; // cannot get position?
				
				if (v.x < minX) minX = v.x;
				if (v.x > maxX) maxX = v.x;
				if (v.y < minY) minY = v.y;
				if (v.y > maxY) maxY = v.y;
				if (v.z < minZ) minZ = v.z;
				if (v.z > maxZ) maxZ = v.z;
			}
			
			float sizeX = maxX - minX;
			float sizeY = maxY - minY;
			float sizeZ = maxZ - minZ;

			// get center + yAdjust (y shift)
			Vector3 center = new Vector3(minX + sizeX / 2.0f, (minY + sizeY / 2.0f) + yAdjust, minZ + sizeZ / 2.0f);
			
			// find radius
			float radius = 0f;
			float extents = 0f;
			for(int i=0; i<blocks.Count(); i++)
			{
				Bounds b = blocks.ElementAt(i).WorldSpaceBounds().ToBounds();
				Vector3 v = blocks.ElementAt(i).CenterPoint();
				if(v == Vector3.zero)
					continue; // cannot get position?
				// get distance from center
				float d = Vector3.Distance(center, v);
				if(d > radius)
				{
					radius = d; // set radius = distance
					extents = Vector3.Distance(v, b.max);
				}
			}
			
			radius += extents + buffer; // add extents and buffer
			// create zone with parameters: zoneId, args, position
			return (bool) ZoneManager?.Call("CreateOrUpdateZone", new object[] {zoneId,
																				new string[] { "radius", radius.ToString() },
																				center
																				});
		}
		
		// rotate all passed Vector2 (point) in the array around the center point to achieve the given angle
		private Vector2[] rotateAll(Vector2[] v, float angle, Vector2 center)
		{
			Vector2[] rotated = new Vector2[v.Length];
			for(int i=0; i<v.Length; i++)
				rotated[i] = rotate(v[i], angle, center);
			return rotated;
		}
		
		// rotate the passed Vector2 (point) around the center point to achieve the given angle
		private Vector2 rotate(Vector2 v, float angle, Vector2 center)
		{
			if(v == center || angle == 0f)
				return v;
			float x = center.x + (v.x-center.x)*Mathf.Cos(angle) - (v.y-center.y)*Mathf.Sin(angle);
			float y = center.y + (v.x-center.x)*Mathf.Sin(angle) + (v.y-center.y)*Mathf.Cos(angle);
			return new Vector2(x,y);

			//return new Vector2( (float)rx, (float)(v.x * sa + v.y * ca));
		}

		// calculate angle in radians of the line segment connnecting two points
		float getAngle(Vector2 p0, Vector2 p1)
		{
			return Mathf.Atan2(p1.x - p0.x,p1.y - p0.y);//*Mathf.Rad2Deg;
		}
		
		// construct 2d hull using Andrew's monotone chain 2d convex hull algorithm
		// converted from c++ algorithm implementation (c) softSurver/Dan Sunday
		// see http://geomalgorithms.com/a10-_hull-1.html#chainHull_2D() for c++
		Vector2[] constructHull(Vector2[] points)
		{
			if(points == null || points.Length == 0)
				return null;
			
			// sort points by x then y
			points = points.OrderBy(x => x.x).ThenBy(x => x.y).ToArray();
			
			int i; // array scan index
			
			Stack<Vector2> hullStack = new Stack<Vector2>();
			int minmin = 0;
			float xmin = points[0].x;
			
			for(i=1; i<points.Length; i++)
				if(points[i].x != xmin) break;
			int minmax = i-1;
			
			if(minmax == points.Length-1) // all x-coords = xmin
			{
				hullStack.Push(points[minmin]);
				if(points[minmax].y != points[minmin].y) // non-trivial segment
					hullStack.Push(points[minmax]);
				hullStack.Push(points[minmin]);
				return hullStack.ToArray();
			}
			
			int maxmax = points.Length-1;
			float xmax = points[points.Length-1].x;
			for(i=points.Length-2; i>=0; i--)
				if(points[i].x != xmax) break;
			int maxmin = i+1;
			
			// compute lower hull on stack
			hullStack.Push(points[minmin]);
			i = minmax;
			while(++i <= maxmin)
			{
				if(isLeft(points[minmin], points[maxmin], points[i]) >= 0 && i < maxmin)
					continue; // ignore point on or above lower line
				
				while(hullStack.Count > 1) // at least 2 points on stack
				{
					Vector2 topPoint = hullStack.Pop();
					if(isLeft(hullStack.Peek(), topPoint, points[i]) > 0)
					{
						hullStack.Push(topPoint); // new hull point
						break;
					}
				}
				hullStack.Push(points[i]); // push point to stack
			}
			
			// computer upper hull on stack
			if(maxmax != maxmin)
				hullStack.Push(points[maxmax]);
			int bot = hullStack.Count; // index for bottom of the stack
			i = maxmin;
			while(--i >= minmax)
			{
				if(isLeft(points[maxmax], points[minmax], points[i]) >= 0 && i > minmax)
					continue; // ignore point below or on upper line
				
				while(hullStack.Count > bot)
				{
					Vector2 topPoint = hullStack.Pop();
					if(isLeft(hullStack.Peek(), topPoint, points[i]) > 0)
					{
						hullStack.Push(topPoint); // new hull point
						break;
					}
				}
				hullStack.Push(points[i]);
			}
			
			if(minmax != minmin)
				hullStack.Push(points[minmin]);
			
			return hullStack.ToArray();
		}
		
		// tests if a point is left/on/right of a line
		// >0 p2 is left of line p0p1
		// =0 p2 on line p0p1
		// <0 p2 is right of line p0p1
		float isLeft(Vector2 p0, Vector2 p1, Vector2 p2)
		{
			return (p1.x - p0.x)*(p2.y - p0.y) - (p2.x - p0.x)*(p1.y - p0.y);
		}
		
		// gets the center point from a hull by finding the min/max x/y values and averaging them
		Vector2 getCenterFromHull(Vector2[] hull)
		{
			float min_X = Mathf.Infinity;
			float max_X = -Mathf.Infinity;
			float min_Y = Mathf.Infinity;
			float max_Y = -Mathf.Infinity;
			for(int i=0; i<hull.Length-1; i++)
			{
				if(hull[i].x < min_X) min_X = hull[i].x;
				if(hull[i].x > max_X) max_X = hull[i].x;
				if(hull[i].y < min_Y) min_Y = hull[i].y;
				if(hull[i].y > max_Y) max_Y = hull[i].y;
			}
			return new Vector2((min_X + max_X)/2f, (min_Y + max_Y)/2f);
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
		
		// draw hull, for debugging - assumes array is an ordered set of perimiter vertices
		void drawHull(BasePlayer player, Vector2[] hull, Vector3 center)
		{
			for(int i=0; i<hull.Length; i++)
			{
				Vector3 from = new Vector3(hull[i].x, center.y+1f, hull[i].y);
				int j = i+1;
				if(j==hull.Length) j = 0;
				Vector3 to = new Vector3(hull[j].x, center.y+1f, hull[j].y);
				player.SendConsoleCommand("ddraw.line", 10f, Color.cyan, from, to);
			}
		}
		
		// draw center point as xyz axis, for debugging
		void drawCenter(BasePlayer player, Vector3 center)
		{
			float length = 0.5f;
			Vector3 xP = new Vector3(length, 0, 0);
			Vector3 xN= new Vector3(-length, 0, 0);
			Vector3 yP = new Vector3(0, length, 0);
			Vector3 yN = new Vector3(0, -length, 0);
			Vector3 zP = new Vector3(0, 0, length);
			Vector3 zN = new Vector3(0, 0, -length);
			player.SendConsoleCommand("ddraw.line", 10f, Color.red, center+xN, center+xP);
			player.SendConsoleCommand("ddraw.line", 10f, Color.blue, center+yN, center+yP);
			player.SendConsoleCommand("ddraw.line", 10f, Color.green, center+zN, center+zP);
		}
		
		// check if player is admin (copied from ZoneManager)
		private static bool isAdmin(BasePlayer player)
		{
			if (player?.net?.connection == null) return true;
			return player.net.connection.authLevel > 0;
        }
        
		// check permissions (copied from ZoneManager)
		private bool hasPermission(BasePlayer player, string permname)
		{
			return isAdmin(player) || permission.UserHasPermission(player.UserIDString, permname);
        }
	}
}