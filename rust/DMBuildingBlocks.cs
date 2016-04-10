using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("DMBuildingBlocks", "ColonBlow", "1.0.4")]
    class DMBuildingBlocks : RustPlugin
    {


		void Loaded()
        	{        
			lang.RegisterMessages(messages, this);   
			permission.RegisterPermission("dmbuildingblocks.admin", this);
		}

		bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public bool ProtectFoundation => Config.Get<bool>("ProtectFoundation");
		public bool ProtectFoundationSteps => Config.Get<bool>("ProtectFoundationSteps");
		public bool ProtectFoundationTriangle => Config.Get<bool>("ProtectFoundationTriangle");
		public bool ProtectWindowWall => Config.Get<bool>("ProtectWindowWall");
		public bool ProtectDoorway => Config.Get<bool>("ProtectDoorway");
        	public bool ProtectFloor => Config.Get<bool>("ProtectFloor");
        	public bool ProtectFloorTriangle => Config.Get<bool>("ProtectFloorTriangle");
		public bool ProtectPillar => Config.Get<bool>("ProtectPillar");
		public bool ProtectStairsLShaped => Config.Get<bool>("ProtectStairsLShaped");
		public bool ProtectStairsUShaped => Config.Get<bool>("ProtectStairsUShaped");
		public bool ProtectRoof => Config.Get<bool>("ProtectRoof");
        	public bool ProtectLowWall => Config.Get<bool>("ProtectLowWall");
        	public bool ProtectWall => Config.Get<bool>("ProtectWall");
		public bool ProtectWallFrame => Config.Get<bool>("ProtectWallFrame");
		public bool ProtectFloorFrame => Config.Get<bool>("ProtectFloorFrame");

        	protected override void LoadDefaultConfig()
        	{
            	Config["ProtectFoundation"] = false;
		Config["ProtectFoundationSteps"] = false;
		Config["ProtectFoundationTriangle"] = false;
	    	Config["ProtectWindowWall"] = false;
	    	Config["ProtectDoorway"] = false;
	   	Config["ProtectFloor"] = false;
	   	Config["ProtectFloorTriangle"] = false;
           	Config["ProtectPillar"] = false;
	   	Config["ProtectStairsLShaped"] = false;
	   	Config["ProtectStairsUShaped"] = false;
	    	Config["ProtectRoof"] = false;
	    	Config["ProtectLowWall"] = false;
	    	Config["ProtectWall"] = false;
		Config["ProtectWallFrame"] = false;
		Config["ProtectFloorFrame"] = false;
            	SaveConfig();
        	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

        	Dictionary<string, string> messages = new Dictionary<string, string>()
        	{
			{"nopermission", "You do not have permission to use that command" },
			{"wrongsyntax", "Incorrect Syntax used. Please check to make sure you typed the commmand correctly" },
			{"ProtectFoundation", "You have set ProtectFoundations to " },
			{"ProtectFoundationSteps", "You have set ProtectFoundationSteps to " },
			{"ProtectFoundationTriangle", "You have set ProtectFoundationTriangle to " },
            		{"ProtectWindowWall", "You have set ProtectWindowWall to " },
            		{"ProtectDoorway", "You have set ProtectDoorway to " },
            		{"ProtectFloor", "You have set ProtectFloor to " },
            		{"ProtectFloorTriangle", "You have set ProtectFloorTriangles to " },
            		{"ProtectPillar", "You have set ProtectPillar to " },
            		{"ProtectStairsLShaped", "You have set ProtectStairsLShaped to " },
            		{"ProtectStairsUShaped", "You have set ProtectStairsUShaped to " },
            		{"ProtectRoof", "You have set ProtectRoof to " },
			{"ProtectLowWall", "You have set ProtectLowWall to " },
			{"ProtectWall", "You have set ProtectWall to " },
			{"ProtectWallFrame", "You have set ProtectWallFrame to " },
			{"ProtectFloorFrame", "You have set ProtectFloorFrame to " }
        	};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
	
		   if ((entity.name.Contains("foundation")) & (!entity.name.Contains("triangle")) & (!entity.name.Contains("steps")))
				{
				if ((ProtectFoundation == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if (entity.name.Contains("foundation.triangle"))
				{
				if ((ProtectFoundationTriangle == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if (entity.name.Contains("foundation.steps"))
				{
				if ((ProtectFoundationSteps == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}			
		   if (entity.name.Contains("wall.window"))
				{
				if ((ProtectWindowWall == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if (entity.name.Contains("wall.doorway"))
				{
				if ((ProtectDoorway == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if ((entity.name.Contains("floor")) & (!entity.name.Contains("triangle")))
				{
				if ((ProtectFloor == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if (entity.name.Contains("floor.triangle"))
				{
				if ((ProtectFloorTriangle == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if (entity.name.Contains("pillar"))
				{
				if ((ProtectPillar == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}			
		   if (entity.name.Contains("stairs.l"))
						{
				if ((ProtectStairsLShaped == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if (entity.name.Contains("stairs.u"))
						{
				if ((ProtectStairsUShaped == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if (entity.name.Contains("roof"))
				{
				if ((ProtectRoof == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if (entity.name.Contains("wall.low"))
				{
				if ((ProtectLowWall == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if ((entity.name.Contains("wall")) & (!entity.name.Contains("wall.low")) & (!entity.name.Contains("wall.doorway")) & (!entity.name.Contains("wall.window")))
				{
				if ((ProtectWall == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if (entity.name.Contains("wall.frame"))
				{
				if ((ProtectWallFrame == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		   if (entity.name.Contains("floor.frame"))
				{
				if ((ProtectFloorFrame == true) & (entity is BuildingBlock))
					{
                   			return false;
					}
				}
		return null;
        }


/////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Chat command to toggle ProtectFoundation true or false
        	[ChatCommand("ProtectFoundation")]
        	void chatCommand_ProtectFoundation(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectFoundation"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFoundation", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectFoundation"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFoundation", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			    }
			return;
		   	}
        	}


		// Chat command to toggle ProtectFoundationSteps true or false
        	[ChatCommand("ProtectFoundationSteps")]
        	void chatCommand_ProtectFoundationSteps(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectFoundationSteps"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFoundationSteps", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectFoundationSteps"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFoundationSteps", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			     }
			return;
		   	}
        	}

		// Chat command to toggle ProtectFoundationTriangle true or false
        	[ChatCommand("ProtectFoundationTriangle")]
        	void chatCommand_ProtectFoundationTriangle(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectFoundationTriangle"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFoundationTriangle", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectFoundationTriangle"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFoundationTriangle", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			     }
			return;
		   	}
        	}

		// Chat command to toggle ProtectWindowWall true or false
        	[ChatCommand("ProtectWindowWall")]
        	void chatCommand_ProtectWindowWall(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectWindowWall"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectWindowWall", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectWindowWall"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectWindowWall", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			     }
			return;
		   	}
        	}

		// Chat command to toggle ProtectDoorway true or false
        	[ChatCommand("ProtectDoorway")]
        	void chatCommand_ProtectDoorway(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			     if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectDoorway"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectDoorway", this) + paramatro);
					return;
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectDoorway"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectDoorway", this) + paramatro);
					return;
                		}
				else
				{
				 	SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			     }
			return;
		   	}
        	}

		// Chat command to toggle ProtectFloor true or false
        	[ChatCommand("ProtectFloor")]
        	void chatCommand_ProtectFloor(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectFloor"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFloor", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectFloor"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFloor", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			    }
			return;
		   	}
        	}

		// Chat command to toggle ProtectFloorTriangle true or false
        	[ChatCommand("ProtectFloorTriangle")]
        	void chatCommand_ProtectFloorTriangle(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectFloorTriangle"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFloorTriangle", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectFloorTriangle"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFloorTriangle", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			    }
			return;
		   	}
        	}

		// Chat command to toggle ProtectPillar true or false
        	[ChatCommand("ProtectPillar")]
        	void chatCommand_ProtectPillar(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectPillar"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectPillar", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectPillar"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectPillar", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			     }
			return;
		   	}
        	}

		// Chat command to toggle ProtectStairsLShaped true or false
        	[ChatCommand("ProtectStairsLShaped")]
        	void chatCommand_ProtectStairsLShaped(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectStairsLShaped"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectStairsLShaped", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectStairsLShaped"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectStairsLShaped", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			    }
			return;
		   	}
        	}

		// Chat command to toggle ProtectStairsUShaped true or false
        	[ChatCommand("ProtectStairsUShaped")]
        	void chatCommand_ProtectStairsUShaped(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectStairsUShaped"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectStairsUShaped", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectStairsUShaped"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectStairsUShaped", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			     }
			return;
		   	}
        	}

		// Chat command to toggle ProtectRoof true or false
        	[ChatCommand("ProtectRoof")]
        	void chatCommand_ProtectRoof(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectRoof"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectRoof", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectRoof"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectRoof", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			    }
			return;
		   	}
        	}

		// Chat command to toggle ProtectLowWall true or false
        	[ChatCommand("ProtectLowWall")]
        	void chatCommand_ProtectLowWall(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectLowWall"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectLowWall", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectLowWall"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectLowWall", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			     }
			return;
		   	}
        	}

		// Chat command to toggle ProtectWall true or false
        	[ChatCommand("ProtectWall")]
        	void chatCommand_ProtectWall(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectWall"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectWall", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectWall"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectWall", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			     }
			return;
		   	}
        	}

		// Chat command to toggle ProtectWallFrame true or false
        	[ChatCommand("ProtectWallFrame")]
        	void chatCommand_ProtectWallFrame(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectWallFrame"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectWallFrame", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectWallFrame"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectWallFrame", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			     }
			return;
		   	}
        	}

		// Chat command to toggle ProtectFloorFrame true or false
        	[ChatCommand("ProtectFloorFrame")]
        	void chatCommand_ProtectFloorFrame(BasePlayer player, string command, string[] args)
        	{
		if (!HasPermission(player, "dmbuildingblocks.admin"))
			{
			SendReply(player, lang.GetMessage("nopermission", this));
			}
		if (HasPermission(player, "dmbuildingblocks.admin"))
		   	{
			if (args != null && args.Length > 0)
			     {
                		string paramatro = args[0].ToLower();
                		if (paramatro == "true")
                		{
                    			Config["ProtectFloorFrame"] = true;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFloorFrame", this) + paramatro);
                		}
                		else if (paramatro == "false")
                		{
                    			Config["ProtectFloorFrame"] = false;
            				SaveConfig();
                    			SendReply(player, lang.GetMessage("ProtectFloorFrame", this) + paramatro);
                		} 
				else
				{
					SendReply(player, lang.GetMessage("wrongsyntax", this));
				}
			     }
			return;	
		   	}
        	}
    }
}
