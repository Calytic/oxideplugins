using System;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{

    [ Info(        "Blueprint Checker", "Dablin", "1.0.6"                                     )]
    [ Description( "Check how many blueprints an online player has learnt and what they are." )]
		
    class BlueprintChecker : RustPlugin {
			
		// [ Required CONSTANTS ] ---------------------------------------------
		
		private const string	CREDIT_TITLE					= "<color=green>BluePrint Checker</color>";
		private const string	CREDIT_AUTHOR					= "<color=lime>Dablin</color>";
		private const string	CREDIT_VERSION					= "1.0.6</color>";
		private const string	CREDIT_EMAIL					= "mail@paulmoffat.net";
		private const string	CREDIT_WEBSITE					= "www.paulmoffat.net";
		private const string	CREDIT_BY						= "by";
		private const string	CREDIT_V						= "(<color=yellow>v";
		private const string	CREDIT_OPEN						= "<color=lime>-=[ </color>";
		private const string	CREDIT_CLOSE					= ") <color=lime>]=-</color>";
								
		// [ Required Variables ] ---------------------------------------------
		
		private string 			ARG_ListInScon 					= "";
		private string 			ARG_ListInChat 					= "";
		private string 			ARG_Help						= "";
		private string 			ARG_Knows						= "";
		
		private	string			CDV								= "OL25S12";

		private int 			CFG_AuthorityLevelRequired		= 0;
		private bool			CFG_UpdatePlayerOnConnect		= true;
		private	bool			CFG_UpdatePlayerOnStudy			= true;
		private bool			CFG_UpdatePlayerVerbose			= true;

		private string			MSG_AccessDenied				= "";
		private string			MSG_CommandLine					= "";
		private string			MSG_Help						= "";
		private string			MSG_Knows						= "";
		private string			MSG_KnowsThis					= "";
		private string			MSG_KnowsCount					= "";
		private string			MSG_KnowsNot					= "";
		private string			MSG_LogWithArg					= "";
		private string			MSG_LogNoArg					= "";
		private string			MSG_MultiplePlayersFound		= "";
		private string			MSG_PlayerNotFound				= "";
		private string			MSG_What						= "";
				
		// Database Variables
		
		private List<int>		Player_BPKeys					= null;
		private string			Player_ID						= "";
		private int				Player_KnownBP					= 0;
		private string			Player_Name						= "";
		
		// Misc - Don't Store
		private int		 		BPMax 							= 0;
		private bool			ConfigMismatch					= false;
		private bool			ConfigMissing					= false;
		private string			CRNL							= "";
		private bool			PlayerOfflineData				= false;
		private BasePlayer 		PlayerChecker 					= null;
		private BasePlayer 		PlayerToCheck 					= null;
		private Data			PlayerBPData					= null;
		private string			SPC								= "";
		private int				SteamIDLength					= 0;
		
		// DEBUGGING VARIABLES
		private const string	DBG_PLAYERBPKEYS				= "dbg_playerbpkeys";
		private const string	DBG_MAXBP						= "dbg_maxbp";
		private bool			DEBUG							= false;

		
// [ SUB-CLASSES ] -------------------------------------------------------------
		
		class Data {
					
			public string Version = "";
			public List< PlayerData > PlayerBPData;
			
			public Data(){
			
				PlayerBPData = new List< PlayerData >();
				
			}
		
		}
		
		class PlayerData {
		
			public string 		UserID;
			public string 		UserName;
			public int  		KnownBP;
			public List<int> 	BlueprintKeys;
			
            public PlayerData(){
			
				UserID 			= "";
				UserName 		= "";
				KnownBP 		= 0;
				BlueprintKeys 	= new List<int>();
			
			}
											
        }

// [ PRIVATE FUNCTIONS ] -------------------------------------------------------

		// Check calling Player has required security level to use this command
		private bool AuthenticatedPlayer( BasePlayer PLAYER, int AUTHORITYLEVEL ){
		
            // Player Does
			if( PLAYER.net.connection.authLevel >= AUTHORITYLEVEL )
                return true;
				
            // Player Does NOT!
			SendReply( PLAYER, MSG_AccessDenied );
            return false;
			
        }

		// BPCheck Chat Command Hooking Function
		[ ChatCommand( "bpcheck" )]
        private void BPCheck( BasePlayer PLAYER, string COMMAND, string[] ARGS ){
		
            // Log command call in server console and check for required security level
            if( ARGS != null )
				Puts( MSG_LogWithArg, PLAYER.displayName, string.Join( SPC, ARGS ));
			else
				Puts( MSG_LogNoArg, PLAYER.displayName );
				
			PlayerChecker = PLAYER;
			if( !AuthenticatedPlayer( PLAYER, CFG_AuthorityLevelRequired )) return;
			
			if( ARGS != null && ARGS.Length > 0 ){
			
				// Display Help Information
				if( ARGS[ 0 ].ToString().ToLower() == ARG_Help ){
				
					SendReply( PlayerChecker, Credit() + CRNL + MSG_Help );
					return;
					
				}
				
				if( ARGS[ 0 ].ToString().ToLower() == DBG_MAXBP ){
				
						Debug( "BPMax = " + BPMax);
						return;
						
				}
												
				// Check for player online/within database
				if( !(PlayerToCheck = GetPlayer( ARGS[ 0 ], PlayerChecker )))
					if( !PlayerOfflineData )
						return;
								
			} else {
			
				// No Player name given, so display the Credits instead
				SendReply( PlayerChecker, string.Format( Credit() + MSG_CommandLine ));
				return;
				
			}
							
			var ItemDefinitions = ItemManager.GetItemDefinitions();	// List of all available in game items
			bool BPCheck		= false;
			int	BPState			= 0;
			string BPLongname	= "";

			if( !PlayerOfflineData ){
			
				if( ARGS.Length == 1 	)
						SendReply( PlayerChecker, string.Format( 	MSG_KnowsCount,
																	PlayerToCheck.displayName,
																	PlayerBPCount( PlayerToCheck.displayName, PlayerChecker ),
																	BPMax 														));			
																															
				if( ARGS.Length > 1 && ARGS.Length < 3 ){
				
					if( ARGS[ 1 ].ToString().ToLower() == ARG_ListInScon )
						Puts( PlayerBPList( PlayerToCheck.displayName, PlayerChecker ));
				
					if( ARGS[ 1 ].ToString().ToLower() == ARG_ListInChat )
						PrintToChat( PlayerChecker, PlayerBPList( PlayerToCheck.displayName, PlayerChecker ));
						
					if( ARGS[ 1 ].ToString().ToLower() == DBG_PLAYERBPKEYS )
						Debug( PlayerToCheck.displayName + " BP Key States: " + GetBPKeyState( PlayerToCheck, ref Player_BPKeys ));
							
				}
				
				if( ARGS.Length > 2 && ARGS.Length < 4 )					
					if( ARGS[ 1 ].ToString() == ARG_Knows ){	// Perform a check on a specific blueprint
					
						BPCheck = true;
						BPState = PlayerBPCheck( PlayerToCheck.displayName, PlayerChecker, ARGS[ 2 ].ToString(), ref BPLongname );
						
					}
				
				if( BPCheck ){
									
					if( BPState == -1 )
						SendReply( PlayerChecker, string.Format( MSG_What, 		ARGS[ 2 ].ToString() 					));
					if( BPState == 0  )
						SendReply( PlayerChecker, string.Format( MSG_KnowsNot, 	PlayerToCheck.displayName, BPLongname	));
					if( BPState == 1  )
						SendReply( PlayerChecker, string.Format( MSG_KnowsThis, PlayerToCheck.displayName, BPLongname 	));
												
				}
				
			} else {
			
				if( ARGS.Length == 1 	)
						SendReply( PlayerChecker, string.Format( 	MSG_KnowsCount,
																	Player_Name + " (offline)",
																	PlayerBPCount( Player_Name, PlayerChecker ),
																	BPMax 												));			
																																																			
				if( ARGS.Length > 1 && ARGS.Length < 3 ){
				
					if( ARGS[ 1 ].ToString().ToLower() == ARG_ListInScon )
						Puts( PlayerBPList( Player_Name, PlayerChecker ));
				
					if( ARGS[ 1 ].ToString().ToLower() == ARG_ListInChat )
						PrintToChat( PlayerChecker, PlayerBPList( Player_Name, PlayerChecker ));
																												
				}
				
				if( ARGS.Length > 2 && ARGS.Length < 4 )					
					if( ARGS[ 1 ].ToString() == ARG_Knows ){	// Perform a check on a specific blueprint{
					
						BPCheck = true;
						BPState = PlayerBPCheck( Player_Name, PlayerChecker, ARGS[ 2 ].ToString(), ref BPLongname );
						
					}

				if( BPCheck ){
								
					if( BPState == -1 )
						SendReply( PlayerChecker, string.Format( MSG_What, 		ARGS[ 2 ].ToString() 	));
					if( BPState == 0  )
						SendReply( PlayerChecker, string.Format( MSG_KnowsNot, 	Player_Name + " (offline)", BPLongname	));
					if( BPState == 1  )
						SendReply( PlayerChecker, string.Format( MSG_KnowsThis, Player_Name + " (offline)", BPLongname 	));
					
				}
																						
			}
				
			PlayerToCheck = null;
				
        }
		
        private void Loaded(){
		
			try {
			
				PlayerBPData = Interface.Oxide.DataFileSystem.ReadObject< Data >( "BlueprintChecker" );
				
			} catch {
						
				PrintWarning( "[Loaded] Database load error. Possible invalid or corrupted data entries. Please check/delete database file/entries then reload Blueprint Checker" );
				return;
				
			}
			
			if( PlayerBPData.Version != CDV ){
			
				PrintWarning( "[Loaded] Database found from previous version '" + PlayerBPData.Version + "', updating for current version '" + CDV + "'" );
				PlayerBPData.Version = CDV;
				Interface.Oxide.DataFileSystem.WriteObject( "BlueprintChecker", PlayerBPData );
				
			}
				
        }
		
		private int CountBP(){
		
			var ItemDefinitions = ItemManager.GetItemDefinitions();
			var CountBP 		= 0;
			
			foreach( var ItemCheck in ItemDefinitions )
				CountBP++;
							
			return CountBP;
		
		}
		
		private string Credit(){
		
			return CREDIT_OPEN + CREDIT_TITLE + SPC + CREDIT_BY + SPC + CREDIT_AUTHOR + SPC + CREDIT_V + CREDIT_VERSION + CREDIT_CLOSE;
		
		}
		
		private string CrNl( int LINENUMBER ){
				
			string crnl = CRNL;
			
			if( LINENUMBER > 1 )
				crnl += CrNl( LINENUMBER - 1 );
			
			return crnl;			
		
		}
				
		private void Debug( string MESSAGE ){
		
			if( DEBUG )
				PrintWarning( "<DEBUG> " + MESSAGE );
		
		}

		private string GetBPKeyState( BasePlayer PLAYER, ref List<int> STATEKEYRING ){
				
			//Debug( "[GetBPKeyState] Started");
			
			var 	ItemDefinitions 	= ItemManager.GetItemDefinitions();
			var 	BPID				= 0;
			string	BPKeys				= "";
			
			foreach( var ItemCheck in ItemDefinitions ){
			
				BPID++;
						
				if(  PLAYER.blueprints.CanCraft( ItemCheck.itemid, 0 ))
					BPKeys += "1";
				else
					BPKeys += "0";
				
				if( STATEKEYRING.Count < BPID ) {
				
					if( PLAYER.blueprints.CanCraft( ItemCheck.itemid, 0 ))
						STATEKEYRING.Add( 1 );
					else
						STATEKEYRING.Add( 0 );
					
					//Debug (" STATEKEYRING.Count < BPID ");
					
				} else {
				
					if( PLAYER.blueprints.CanCraft( ItemCheck.itemid, 0 ))
						STATEKEYRING[ BPID - 1 ] = 1;
					else
						STATEKEYRING[ BPID - 1 ] = 0;
					
					//Debug (" !STATEKEYRING.Count < BPID ");
					
				}
									
			}
			
			//Debug( "[GetBPKeyState] Keyring Size: " + STATEKEYRING.Count );
			
			return BPKeys;
		
		}
				
		private object GetConfig( string KEY, object DEFAULTVALUE ){
		
			if( Config[ KEY ] != null )			
				return Config[ KEY ];
			
			ConfigMismatch = true;
			
			Config[ KEY ] = DEFAULTVALUE;
			return Config[ KEY ];
		
		}
		
		private BasePlayer GetPlayer( string PLAYERTOCHECK, BasePlayer PLAYERCHECKER ){
					
			PlayerOfflineData = false;
			
			// Search for absolute player name or steamid
			BasePlayer PlayerToCheck = BasePlayer.Find( PLAYERTOCHECK );
			
			if( PlayerToCheck )
				return PlayerToCheck;
		
			// Perform a partial playername search
			
			List<string> PlayersFound = new List<string>();
			
			string PlayerCheck = PLAYERTOCHECK.ToLower();
			
			foreach( BasePlayer PLAYER in BasePlayer.activePlayerList )
				if( PLAYER.displayName.ToLower().Contains( PlayerCheck ))
					PlayersFound.Add( PLAYER.displayName );
					
			if( PlayersFound.Count == 1 )
				return BasePlayer.Find( PlayersFound[ 0 ] );
				
			if( PlayersFound.Count > 1 )
				SendReply( PLAYERCHECKER, MSG_MultiplePlayersFound );	
			else {
							
				// Perform a secondary database search for the player				
				
				//Debug( "[GetPlayer] Searching Database... " );
				
				List<int> PlayersFoundOffline = new List<int>();
				
				//Debug( "[GetPlayer] PlayerBPData.PlayerBPData.Count = " + Convert.ToString( PlayerBPData.PlayerBPData.Count ));
				
				for( int _key = 0; _key < PlayerBPData.PlayerBPData.Count; _key++ ){
				
					//Debug( "[GetPlayer] _key = " + Convert.ToString( _key ));
				
					// This should occur within normal situations but while I was developing the database functions some usernames were reset to null
					if( PlayerBPData.PlayerBPData[ _key ].UserName == null )
						continue;
						
					//Debug( "[GetPlayer] Playername found = " + PlayerBPData.PlayerBPData[ _key ].UserName );
					
					if( PlayerBPData.PlayerBPData[ _key ].UserName.ToLower().Contains( PLAYERTOCHECK.ToLower() 	)||
						PlayerBPData.PlayerBPData[ _key ].UserID.ToLower().Contains( PLAYERTOCHECK.ToLower() 	)){
						
							PlayersFoundOffline.Add( _key );
							
					}
						
				}
							
				//Debug( "[GetPlayer] PlayersFoundOffline.Count = " + Convert.ToString( PlayersFoundOffline.Count ));
							
				if( PlayersFoundOffline.Count > 1 )				
					SendReply( PLAYERCHECKER, MSG_MultiplePlayersFound );
				else
					if( PlayersFoundOffline.Count == 1 ){				
														
						//Debug( "[GetPlayer] " + PLAYERTOCHECK + " found in database" );
						
						PlayerOfflineData = true;
						
						Player_ID			= PlayerBPData.PlayerBPData[ PlayersFoundOffline[0] ].UserID;
						Player_Name			= PlayerBPData.PlayerBPData[ PlayersFoundOffline[0] ].UserName;
						Player_KnownBP 		= PlayerBPData.PlayerBPData[ PlayersFoundOffline[0] ].KnownBP;
						Player_BPKeys		= PlayerBPData.PlayerBPData[ PlayersFoundOffline[0] ].BlueprintKeys;
					
					} else
						SendReply( PLAYERCHECKER, string.Format( MSG_PlayerNotFound, PLAYERTOCHECK )); // Player not found online or in database

			}
												
			return null;
		
		}

		private void OnConsumableUse( Item ITEM ){
										
			if( CFG_UpdatePlayerOnStudy && ITEM.GetOwnerPlayer() && ITEM.IsBlueprint() ){
				
				//Debug( "[OnConsumableUse] '" + ITEM.info.shortname + "' blueprint studied by " + ITEM.GetOwnerPlayer().displayName );
					
				if( CFG_UpdatePlayerVerbose )
					Puts( "[OnConsumableUse] Updating Database for <" + ITEM.GetOwnerPlayer().UserIDString + "> : " + ITEM.GetOwnerPlayer().displayName );
					
				UpdatePlayer( ITEM.GetOwnerPlayer() );
				
			}
		
		}

		private void OnPlayerInit( BasePlayer PLAYER ){
		
			//Debug( "[OnPlayerInit] New Player Connected: " + PLAYER.displayName );
				
			if( CFG_UpdatePlayerOnConnect ){
			
				if( CFG_UpdatePlayerVerbose )
					Puts( "[OnPlayerInit] Updating Database for <" + PLAYER.UserIDString + "> : " + PLAYER.displayName );
			
				UpdatePlayer( PLAYER );
			
			}
				
		}
		
		private int PlayerBPCheck( string PLAYERID, BasePlayer PLAYERCHECKER, string BLUEPRINTID, ref string BPLONGNAME ){
		
			//Debug( BLUEPRINTID );
			
			var ItemDefinitions 	= ItemManager.GetItemDefinitions();
			bool BPKnows 			= false;							// Does the player know that specific blueprint
			bool BPNotFound 		= false;							// Does that specific blueprint even exist
			int	 BPID				= 0;
				
			foreach( var ItemCheck in ItemDefinitions                       ){
				
				if( BLUEPRINTID.ToLower() == ItemCheck.shortname.ToLower() || BLUEPRINTID.ToLower() == ItemCheck.displayName.translated.ToLower() ){
				
					BPNotFound = false;
					BPLONGNAME = ItemCheck.displayName.translated;
					
					if( !PlayerOfflineData) {
					
						if( PlayerToCheck.blueprints.CanCraft( ItemCheck.itemid, 0 ))
							BPKnows = true;
						else
							BPKnows = false;
						
					} else {
											
						if( Player_BPKeys[ BPID ] == 1 )
							BPKnows = true;
						else
							BPKnows = false;
					
					}
						
					break; // Item found, break out of item check loop
					
				} else
					BPNotFound = true;
				
				++BPID;
					
			}
			
			if( BPNotFound )
				return -1;
			else			
				if( !BPKnows )
					return 0;
				else
					return 1;
		
		}
		
		private int PlayerBPCount( string PLAYERID, BasePlayer PLAYERCHECKER ){
		
			BasePlayer Player = GetPlayer( PLAYERID, PLAYERCHECKER );

			if( !PlayerOfflineData ){
			
				//Debug( "[PlayerBPCount] Return Online Data" );
				
				var ItemDefinitions = ItemManager.GetItemDefinitions();
				var BPCount 		= 0;
				
				foreach( var ItemCheck in ItemDefinitions )
					if( Player.blueprints.CanCraft( ItemCheck.itemid, 0 ))
						BPCount++;
						
					Player_KnownBP = BPCount;
					return Player_KnownBP;
				
			} else {
			
				//Debug( "[PlayerBPCount] Return Offline Data" );
			
				return Player_KnownBP;
				
			}
						
		}
		
		private string PlayerBPList( string PLAYERID, BasePlayer PLAYERCHECKER ){
		
			BasePlayer Player 	= GetPlayer( PLAYERID, PLAYERCHECKER );
			var ItemDefinitions = ItemManager.GetItemDefinitions();
			string BPChatList	= "";
			int BPID			= 0;
			
			foreach( var ItemCheck in ItemDefinitions                       ){
																				
				if( !PlayerOfflineData ){

					// If the blueprints are to be listed, forward the list into the requested display output
					if( Player.blueprints.CanCraft( ItemCheck.itemid, 0 )){
																
						if( BPChatList != "" )
							BPChatList += " | ";
								
						if( BPChatList == "" )
							BPChatList += Player.displayName + SPC + MSG_Knows + "..." + CrNl( 2 );

						BPChatList += ItemCheck.displayName.translated;
							
					}
				
				} else {

					// If the blueprints are to be listed, forward the list into the requested display output
					if( Player_BPKeys[ BPID ] == 1 ){
																
						if( BPChatList != "" )
							BPChatList += " | ";
								
						if( BPChatList == "" )
							BPChatList += Player_Name + SPC + "(offline)" + SPC + MSG_Knows + "..." + CrNl( 2 );

						BPChatList += ItemCheck.displayName.translated;
						
					}
							
				}

				++BPID;
								
			}
			
			return BPChatList;
		
		}
		
		private void PrintToChat( BasePlayer PLAYERCHECKER, string MESSAGE ){
		
			//Debug( "[PrintToChat] Message Length = " + Convert.ToString( MESSAGE.Length ));
			
			if( MESSAGE.Length < 1000 ){
			
				SendReply( PLAYERCHECKER, MESSAGE );
			
			} else {
			
				//Debug( "[PrintToChat] Displaying Multiple Messages..." );
			
				int MESSAGEPARTS = ( MESSAGE.Length / 1000 );
				
				if( Convert.ToDecimal( MESSAGEPARTS ) < Convert.ToDecimal( MESSAGE.Length ) / 1000 )
					++MESSAGEPARTS;
					
				string MessagePart = "";
				
				//Debug( "[PrintToChat] MessageParts = " + Convert.ToString( MESSAGEPARTS ));
				
				for( int _printMessage = 0; _printMessage < MESSAGEPARTS; ++_printMessage ){
				
					if( _printMessage != ( MESSAGEPARTS - 1 ))
						MessagePart = MESSAGE.Substring( ( _printMessage * 1000 ), 1000 );
					else
						MessagePart = MESSAGE.Substring( _printMessage * 1000 );
						
					//Debug( "[PrintToChat] Next Message Length = " + Convert.ToString( MessagePart.Length ));
					SendReply( PLAYERCHECKER, MessagePart );						
				
				}
					
			}
		
		}
		
		private void OnServerInitialized(){
		
			PrintWarning( "[OnServerInitialized] Loading Configuration Data..." );
			
			LoadConfig();
			
			// Transfer loaded variables to their internal counterparts
			
			string CDVCheck;
			
			CDVCheck						= Convert.ToString(	 Config[ "CDV"							]);
			ARG_ListInScon 					= Convert.ToString(  Config[ "ARG_ListInScon" 				]);
			ARG_ListInChat 					= Convert.ToString(  Config[ "ARG_ListInChat" 				]);
			ARG_Help 						= Convert.ToString(  Config[ "ARG_Help" 					]);
			ARG_Knows 						= Convert.ToString(  Config[ "ARG_Knows" 					]);
			CFG_AuthorityLevelRequired 		= Convert.ToInt32(   Config[ "CFG_AuthorityLevelRequired" 	]);
			CFG_UpdatePlayerOnConnect 		= Convert.ToBoolean( Config[ "CFG_UpdatePlayerOnConnect" 	]);
			CFG_UpdatePlayerOnStudy			= Convert.ToBoolean( Config[ "CFG_UpdatePlayerOnStudy" 		]);
			CFG_UpdatePlayerVerbose 		= Convert.ToBoolean( Config[ "CFG_UpdatePlayerVerbose" 		]);
			MSG_AccessDenied 				= Convert.ToString(  Config[ "MSG_AccessDenied" 			]);
			MSG_CommandLine 				= Convert.ToString(  Config[ "MSG_CommandLine" 				]);
			MSG_Help 						= Convert.ToString(  Config[ "MSG_Help" 					]);
			MSG_Knows 						= Convert.ToString(  Config[ "MSG_Knows" 					]);
			MSG_KnowsThis 					= Convert.ToString(  Config[ "MSG_KnowsThis" 				]);
			MSG_KnowsCount 					= Convert.ToString(  Config[ "MSG_KnowsCount" 				]);
			MSG_KnowsNot 					= Convert.ToString(  Config[ "MSG_KnowsNot" 				]);
			MSG_LogWithArg 					= Convert.ToString(  Config[ "MSG_LogWithArg" 				]);
			MSG_LogNoArg 					= Convert.ToString(  Config[ "MSG_LogNoArg" 				]);
			MSG_MultiplePlayersFound 		= Convert.ToString(  Config[ "MSG_MultiplePlayersFound" 	]);
			MSG_PlayerNotFound 				= Convert.ToString(  Config[ "MSG_PlayerNotFound" 			]);
			MSG_What 						= Convert.ToString(  Config[ "MSG_What" 					]);
					
			if( CDVCheck != CDV )
				ConfigMismatch = true;
			
			if( ConfigMismatch && !ConfigMissing ){
			
				PrintWarning( "[OnServerInitialized] Existing configuration version mismatch found - updating for current version '" + CDV + "'" );
				LoadDefaultConfig();
								
				ConfigMismatch = false;
				ConfigMissing = false;
				
			}
			
			// Other variables initialization - Not Stored
			
			BPMax 			= CountBP();
			CRNL			= "\n";
			Player_BPKeys	= new List<int>();
			
			if( PlayerBPData == null ){
			
				PrintWarning( "[OnServerInitialized] BP Database not found, creating a new one" );
				new Data();
				
			} else
				PrintWarning( "[OnServerInitialized] Loading existing BP Database with " + PlayerBPData.PlayerBPData.Count + " player records" );
				
			SPC				= " ";
			SteamIDLength 	= 17;
				
		}
		
		private void UpdateDatabase( string PLAYERID, string PLAYERNAME ){
		
			//Debug( "[UpdateDatabase] Players currently stored in database: " + Convert.ToString( PlayerBPData.PlayerBPData.Count ));
			
			PlayerData UpdatedPlayerData 	= new PlayerData();
			UpdatedPlayerData.UserID 		= PLAYERID;
			UpdatedPlayerData.UserName 		= PLAYERNAME;			
			UpdatedPlayerData.BlueprintKeys = Player_BPKeys;			
			
			if( !PlayerOfflineData )
				UpdatedPlayerData.KnownBP 		= PlayerBPCount( PLAYERID, null ); // God I hope that null value doesn't come back to bite me in the arse
			else
				UpdatedPlayerData.KnownBP 		= Player_KnownBP;

			
			if( PlayerBPData.PlayerBPData.Count == 0 ){
			
				if( CFG_UpdatePlayerVerbose )
					PrintWarning( "[UpdateDatabase] Database empty. Adding first player entry: " + PLAYERID );
				
				PlayerBPData.PlayerBPData.Add( UpdatedPlayerData );
				
			} else {
			
				bool PlayerFound = false;
			
				for( int key = 0; key < PlayerBPData.PlayerBPData.Count; key++ ){
				
					if( PlayerBPData.PlayerBPData[ key ].UserID == PLAYERID ){

						//Debug( "[UpdateDatabase] Updating database for " + PLAYERID );
						PlayerBPData.PlayerBPData[ key ] = UpdatedPlayerData;
						PlayerFound = true;
						
					}

				}
				
				if( !PlayerFound ){

					if( CFG_UpdatePlayerVerbose )
						PrintWarning( "[UpdateDatabase] " + PLAYERID + " not found, adding to database" );				
						
					PlayerBPData.PlayerBPData.Add( UpdatedPlayerData );

				}
			
			}
			
			PlayerBPData.Version = CDV;
			Interface.Oxide.DataFileSystem.WriteObject( "BlueprintChecker", PlayerBPData );
				
		}
				
		private void UpdateKeyring( BasePlayer PLAYER, ref List<int> STATEKEYRING ){
		
			//Debug( "[UpdateKeyring] Started" );
			
			GetBPKeyState( PLAYER, ref Player_BPKeys );
					
		}
		
		private void UpdatePlayer( BasePlayer PLAYER ){
		
			Player_ID = PLAYER.UserIDString;
			Player_Name = PLAYER.displayName;
			
			//Debug( "[UpdatePlayer] Player_ID = " + Player_ID );
			
			UpdateKeyring( PLAYER, ref Player_BPKeys );
			UpdateDatabase( Player_ID, Player_Name );

		}
		
// [ PROTECTED FUNCTIONS ] -----------------------------------------------------

		protected override void LoadDefaultConfig() {
		
			if( !ConfigMismatch )
				ConfigMissing = true;
		
            if( ConfigMissing ){
			
				PrintWarning( "[LoadDefaultConfig] Configuration file not found - Creating a new one with default values." );
				Config.Clear();
				GetConfig( "CDV", CDV );
			
			} else
				Config[ "CDV" ] = CDV;
			
			// Create new configuration keys with default values
			
			GetConfig( "ARG_ListInScon", 				"listincon" 																	); 
			GetConfig( "ARG_ListInScon", 				"listincon" 																	); 
            GetConfig( "ARG_ListInChat", 				"listinchat" 																	);
            GetConfig( "ARG_Help",						"help" 																			);
            GetConfig( "ARG_Knows",						"knows" 																		);
            GetConfig( "CFG_AuthorityLevelRequired",	2 																				);
            GetConfig( "CFG_UpdatePlayerOnConnect", 	true 																			);
            GetConfig( "CFG_UpdatePlayerOnStudy",		true 																			);
            GetConfig( "CFG_UpdatePlayerVerbose",		true 																			);
            GetConfig( "MSG_AccessDenied",				"You are not allowed to use this command!" 										);
            GetConfig( "MSG_CommandLine", 				"\nType /bpcheck 'username or steamid' or\n\t     /bpcheck help for more info" 	);
			
            GetConfig( "MSG_Help", 						"/bpcheck 'username' : Count Known BPs\n/bpcheck 'username' knows 'itemname' : Confirm player has specific BP\n/bpcheck 'username' listinchat : Display known BPs in Chat\n/bpcheck 'username' listincon : Display known BPs in Server Console" );
			
			GetConfig( "MSG_Knows",						"Knows" 																		);
            GetConfig( "MSG_KnowsThis", 				"YES, '{0}' does know the {1} BP" 												);
            GetConfig( "MSG_KnowsCount", 				"'{0}' knows {1} of {2} BPs" 													);
            GetConfig( "MSG_KnowsNot", 					"NO, '{0}' does NOT know the {1} BP" 											);
            GetConfig( "MSG_LogWithArg", 				"{0} used /bpcheck {1}" 														);
            GetConfig( "MSG_LogNoArg", 					"{0} used /bpcheck" 															);
            GetConfig( "MSG_MultiplePlayersFound",		"Multiple Players Found - Please be more specific"								);
            GetConfig( "MSG_PlayerNotFound",			"Player '{0}' not online or found in database " 								);
            GetConfig( "MSG_What", 						"Um, what the hell is a '{0}'?" 												);
            
			SaveConfig();
				
        }
																				
    }
	
}