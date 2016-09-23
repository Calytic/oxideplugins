using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Facepunch;
using Rust;
using Oxide.Core;
using Oxide.Core.Libraries;


namespace Oxide.Plugins
{
	[Info( "Wanted For Murder", "Raptor007", "1.0.7" )]
	[Description( "Players can punish unwanted PvP." )]
	
	class WantedForMurder : RustPlugin
	{
		bool DataChanged = false;
		Dictionary< ulong, List<ulong> > Murderers = new Dictionary< ulong, List<ulong> >();
		Dictionary< ulong, ulong > LastKillerOf = new Dictionary< ulong, ulong >();
		Dictionary< ulong, ulong > LastAttackerOf = new Dictionary< ulong, ulong >();
		Dictionary< string, string > EN = new Dictionary< string, string >();
		
		
		// Poor man's locking because we can't use System.Threading.
		
		volatile bool DataLocked = false;
		
		void LockData()
		{
			while( DataLocked );
			DataLocked = true;
		}
		
		void UnlockData()
		{
			DataLocked = false;
		}
		
		
		void ChatToPlayer( BasePlayer player, string key, params object[] args )
		{
			PrintToChat( player, string.Format( lang.GetMessage( key, this, player.UserIDString ), args ) );
		}
		
		
		void ChatToAll( string key, params object[] args )
		{
			foreach( BasePlayer player in BasePlayer.activePlayerList )
				ChatToPlayer( player, key, args );
		}
		
		
		bool SetUnconfiguredDefaults()
		{
			bool changed = false;
			
			if( Config[ "AnnounceWhenLoaded" ] == null )
			{
				Config[ "AnnounceWhenLoaded" ] = true;
				changed = true;
			}
			if( Config[ "AwakeAutoPunish" ] == null )
			{
				Config[ "AwakeAutoPunish" ] = false;
				changed = true;
			}
			if( Config[ "CastleDoctrine" ] == null )
			{
				Config[ "CastleDoctrine" ] = true;
				changed = true;
			}
			if( Config[ "RemoveMurdererBags" ] == null )
			{
				Config[ "RemoveMurdererBags" ] = true;
				changed = true;
			}
			if( Config[ "Sheriffs" ] == null )
			{
				Config[ "Sheriffs" ] = new List<ulong>();
				changed = true;
			}
			if( Config[ "ShowOnWake" ] == null )
			{
				Config[ "ShowOnWake" ] = true;
				changed = true;
			}
			if( Config[ "SleepersAutoPunish" ] == null )
			{
				Config[ "SleepersAutoPunish" ] = true;
				changed = true;
			}
			
			return changed;
		}
		
		
		protected override void LoadDefaultConfig()
		{
			LockData();
			
			Config.Clear();
			SetUnconfiguredDefaults();
			SaveConfig();
			
			UnlockData();
		}
		
		
		void LoadData()
		{
			Murderers = Interface.GetMod().DataFileSystem.ReadObject< Dictionary< ulong, List<ulong> > >( "WantedForMurder-Murderers" );
		}
		
		
		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject( "WantedForMurder-Version", Version.ToString() );
			Interface.GetMod().DataFileSystem.WriteObject( "WantedForMurder-Murderers", Murderers );
		}
		
		
		void OnServerSave()
		{
			if( DataChanged )
			{
				LockData();
				
				if( DataChanged )
				{
					DataChanged = false;
					SaveData();
				}
				
				UnlockData();
			}
		}
		
		
		string GetPlayerNameFromID( ulong player_id )
		{
			foreach( BasePlayer player in BasePlayer.activePlayerList )
			{
				if( player.userID == player_id )
					return player.displayName;
			}
			foreach( BasePlayer player in BasePlayer.sleepingPlayerList )
			{
				if( player.userID == player_id )
					return player.displayName;
			}
			
			return "Somebody";
		}
		
		
		ulong GetPlayerIDFromName( string name )
		{
			foreach( BasePlayer player in BasePlayer.activePlayerList )
			{
				if( player.displayName.ToLower().Contains(name.ToLower()) )
					return player.userID;
			}
			foreach( BasePlayer player in BasePlayer.sleepingPlayerList )
			{
				if( player.displayName.ToLower().Contains(name.ToLower()) )
					return player.userID;
			}
			
			return 0;
		}
		
		
		bool IsMurderer( ulong player_id )
		{
			if( Murderers.ContainsKey(player_id) )
				return true;
			return false;
		}
		
		
		bool IsKiller( ulong player_id )
		{
			foreach( KeyValuePair<ulong,ulong> pair in LastKillerOf )
			{
				if( pair.Value == player_id )
					return true;
			}
			return false;
		}
		
		
		bool PunishLastKillerOf( ulong player_id )
		{
			if( Murderers.ContainsKey(player_id) )
				return false;
			if( ! LastKillerOf.ContainsKey(player_id) )
				return false;
			
			ulong murderer_id = LastKillerOf[ player_id ];
			
			if( ! Murderers.ContainsKey(murderer_id) )
				Murderers[ murderer_id ] = new List<ulong>();
			
			// We don't care who kills murderers.
			if( LastKillerOf.ContainsKey(murderer_id) )
				LastKillerOf.Remove( murderer_id );
			
			if( ! Murderers[ murderer_id ].Contains(player_id) )
			{
				Murderers[ murderer_id ].Add( player_id );
				DataChanged = true;
				RemoveSleepingBagsFor( murderer_id );
				
				ChatToAll( "Punish", GetPlayerNameFromID(murderer_id), GetPlayerNameFromID(player_id) );
				
				return true;
			}
			
			return false;
		}
		
		
		void RemoveSleepingBagsFor( ulong player_id )
		{
			if( Config.Get<bool>("RemoveMurdererBags") )
			{
				foreach( SleepingBag bag in SleepingBag.FindForPlayer( player_id, true ) )
					bag.Kill();
			}
		}
		
		
		void RemoveMurdererSleepingBags()
		{
			foreach( BasePlayer player in BasePlayer.activePlayerList )
			{
				if( IsMurderer(player.userID) )
					RemoveSleepingBagsFor( player.userID );
			}
		}
		
		
		void OnItemDeployed( Deployer deployer, BaseEntity entity )
		{
			LockData();
			RemoveMurdererSleepingBags();
			UnlockData();
		}
		
		
		void OnEntityTakeDamage( BaseCombatEntity entity, HitInfo info )
		{
			if( entity == null )
				return;
			
			if( (info != null) && (info.Initiator != null) && info.Initiator.name.Contains("player") && entity.name.Contains("player") )
			{
				var victim = entity as BasePlayer;
				var killer = info.Initiator as BasePlayer;
				
				if( (victim == null) || (killer == null) )
					return;
				
				// Don't track self-harm.
				if( victim.userID != killer.userID )
				{
					LockData();
					
					// Don't track kills of murderers, and if Castle Doctrine is enabled, don't allow punishment for wounding home invaders.
					if( IsMurderer(victim.userID) || (Config.Get<bool>("CastleDoctrine") && killer.CanBuild() && ! victim.CanBuild()) )
					{
						// Prevent accidentally punishing the wrong person.
						if( LastAttackerOf.ContainsKey(victim.userID) )
							LastAttackerOf.Remove( victim.userID );
					}
					else
						LastAttackerOf[ victim.userID ] = killer.userID;
					
					UnlockData();
				}
			}
		}
		
		
		void OnEntityDeath( BaseCombatEntity entity, HitInfo info )
		{
			if( entity == null )
				return;
			
			if( entity.name.Contains("player") )
			{
				var victim = entity as BasePlayer;
				if( victim == null )
					return;
				
				bool sleep_murder = false;
				bool locked_data = false;
				
				if( (info != null) && (info.Initiator != null) && info.Initiator.name.Contains("player") )
				{
					var killer = info.Initiator as BasePlayer;
					if( killer == null )
						return;
					
					// Don't track suicides.
					if( victim.userID != killer.userID )
					{
						LockData();
						locked_data = true;
						
						// Don't track kills of murderers, and if Castle Doctrine is enabled, don't allow punishment for killing home invaders.
						if( IsMurderer(victim.userID) || (Config.Get<bool>("CastleDoctrine") && killer.CanBuild() && ! victim.CanBuild()) )
						{
							// Prevent accidentally punishing the wrong person.
							if( LastKillerOf.ContainsKey(victim.userID) )
								LastKillerOf.Remove( victim.userID );
						}
						else
						{
							LastKillerOf[ victim.userID ] = killer.userID;
							sleep_murder = victim.IsSleeping();
						}
					}
				}
				else
				{
					LockData();
					locked_data = true;
					
					if( LastAttackerOf.ContainsKey(victim.userID) )
					{
						LastKillerOf[ victim.userID ] = LastAttackerOf[ victim.userID ];
						sleep_murder = victim.IsSleeping();
					}
				}
				
				if( ! locked_data )
					LockData();
				
				// Always remove stale attacked-by data when a player dies.
				LastAttackerOf.Remove( victim.userID );
				
				if( Config.Get<bool>("SleepersAutoPunish") && sleep_murder )
					PunishLastKillerOf( victim.userID );
				else if( Config.Get<bool>("AwakeAutoPunish") && ! sleep_murder )
					PunishLastKillerOf( victim.userID );
				
				UnlockData();
			}
		}
		
		
		void ShowWantedListTo( BasePlayer player )
		{
			if( Murderers.Count > 0 )
			{
				foreach( KeyValuePair< ulong, List<ulong> > pair in Murderers )
				{
					string murderer = GetPlayerNameFromID( pair.Key );
					string victims = GetPlayerNameFromID( pair.Value[ 0 ] );
					if( pair.Value.Count == 2 )
						victims += " and " + GetPlayerNameFromID( pair.Value[ 1 ] );
					else if( pair.Value.Count > 2 )
					{
						for( int i = 1; i < pair.Value.Count - 1; i ++ )
							victims += ", " + GetPlayerNameFromID( pair.Value[ i ] );
						victims += ", and " + GetPlayerNameFromID( pair.Value[ pair.Value.Count - 1 ] );
					}
					
					ChatToPlayer( player, "Wanted", murderer, victims );
				}
			}
			else
				ChatToPlayer( player, "NobodyWanted" );
		}
		
		
		void OnPlayerSleepEnded( BasePlayer player )
		{
			LockData();
			
			if( Config.Get<bool>("ShowOnWake") )
			{
				if( Config.Get< List<ulong> >("Sheriffs").Contains(player.userID) )
					ChatToPlayer( player, "CommandsSheriff" );
				else
					ChatToPlayer( player, "Commands" );
				
				ShowWantedListTo( player );
			}
			
			UnlockData();
		}
		
		
		[ChatCommand("wanted")]
		void WantedCommand( BasePlayer player, string command, string[] args )
		{
			LockData();
			ShowWantedListTo( player );
			UnlockData();
		}
		
		
		[ChatCommand("punish")]
		void PunishCommand( BasePlayer player, string command, string[] args )
		{
			LockData();
			
			if( IsMurderer(player.userID) )
			{
				string victims = GetPlayerNameFromID( Murderers[ player.userID ][ 0 ] );
				if( Murderers[ player.userID ].Count == 2 )
					victims += " and " + GetPlayerNameFromID( Murderers[ player.userID ][ 1 ] );
				else if( Murderers[ player.userID ].Count > 2 )
				{
					for( int i = 1; i < Murderers[ player.userID ].Count - 1; i ++ )
						victims += ", " + GetPlayerNameFromID( Murderers[ player.userID ][ i ] );
					victims += ", and " + GetPlayerNameFromID( Murderers[ player.userID ][ Murderers[ player.userID ].Count - 1 ] );
				}
				
				ChatToPlayer( player, "PunishForbidden", victims );
			}
			else if( IsKiller(player.userID) )
			{
				List<string> victim_list = new List<string>();
				foreach( KeyValuePair<ulong,ulong> pair in LastKillerOf )
				{
					if( pair.Value == player.userID )
						victim_list.Add( GetPlayerNameFromID(pair.Key) );
				}
				string victims = victim_list[ 0 ];
				if( victim_list.Count == 2 )
					victims += " and " + victim_list[ 1 ];
				else if( victim_list.Count > 2 )
				{
					for( int i = 1; i < victim_list.Count - 1; i ++ )
						victims += ", " + victim_list[ i ];
					victims += ", and " + victim_list[ victim_list.Count - 1 ];
				}
				
				ChatToPlayer( player, "PunishForbidden", victims );
			}
			else if( LastKillerOf.ContainsKey(player.userID) )
			{
				ulong murderer_id = LastKillerOf[ player.userID ];
				if( Murderers.ContainsKey(murderer_id) && Murderers[ murderer_id ].Contains(player.userID) )
					ChatToPlayer( player, "PunishAlready", GetPlayerNameFromID(murderer_id) );
				else
					PunishLastKillerOf( player.userID );
			}
			else
				ChatToPlayer( player, "PunishNotFound" );
			
			UnlockData();
		}
		
		
		[ChatCommand("forgive")]
		void ForgiveCommand( BasePlayer player, string command, string[] args )
		{
			LockData();
			
			ulong murderer_id = 0;
			
			if( args.Length > 0 && args[ 0 ].Length > 0 )
				murderer_id = GetPlayerIDFromName( args[ 0 ] );
			else if( LastKillerOf.ContainsKey(player.userID) )
				murderer_id = LastKillerOf[ player.userID ];
			else
			{
				foreach( KeyValuePair< ulong, List<ulong> > pair in Murderers )
				{
					if( pair.Value.Contains(player.userID) )
					{
						murderer_id = pair.Key;
						break;
					}
				}
				
				if( (murderer_id == 0) && LastAttackerOf.ContainsKey(player.userID) )
					murderer_id = LastAttackerOf[ player.userID ];
			}
			
			if( murderer_id != 0 )
			{
				string murderer = GetPlayerNameFromID( murderer_id );
				
				if( LastKillerOf.ContainsKey(player.userID) && (LastKillerOf[ player.userID ] == murderer_id) )
					LastKillerOf.Remove( player.userID );
				
				if( LastAttackerOf.ContainsKey(player.userID) && (LastAttackerOf[ player.userID ] == murderer_id) )
					LastAttackerOf.Remove( player.userID );
				
				if( Murderers.ContainsKey(murderer_id) && Murderers[ murderer_id ].Contains(player.userID) )
				{
					Murderers[ murderer_id ].Remove( player.userID );
					if( Murderers[ murderer_id ].Count == 0 )
					{
						Murderers.Remove( murderer_id );
						ChatToAll( "ForgiveByAll", murderer, player.displayName );
					}
					else
						ChatToAll( "ForgiveBySome", murderer, player.displayName );
					
					DataChanged = true;
				}
				else
					ChatToPlayer( player, "ForgiveKill", murderer );
			}
			else
				ChatToPlayer( player, "ForgiveNotFound" );
			
			UnlockData();
		}
		
		
		[ChatCommand("pardon")]
		void PardonCommand( BasePlayer player, string command, string[] args )
		{
			LockData();
			
			if( Config.Get< List<ulong> >("Sheriffs").Contains(player.userID) )
			{
				ulong murderer_id = 0;
				
				if( args.Length > 0 && args[ 0 ].Length > 0 )
					murderer_id = GetPlayerIDFromName( args[ 0 ] );
				
				if( murderer_id != 0 )
				{
					string murderer = GetPlayerNameFromID( murderer_id );
					
					foreach( KeyValuePair<ulong,ulong> pair in LastKillerOf )
					{
						if( pair.Value == murderer_id )
							LastKillerOf.Remove( pair.Key );
					}
					
					foreach( KeyValuePair<ulong,ulong> pair in LastAttackerOf )
					{
						if( pair.Value == murderer_id )
							LastAttackerOf.Remove( pair.Key );
					}
					
					if( Murderers.ContainsKey(murderer_id) )
					{
						Murderers.Remove( murderer_id );
						DataChanged = true;
						ChatToAll( "Pardon", murderer, player.displayName );
					}
					else
						ChatToPlayer( player, "PardonNotWanted", murderer );
				}
				else
					ChatToPlayer( player, "PardonNotFound" );
			}
			else
				ChatToPlayer( player, "PardonForbidden" );
			
			UnlockData();
		}
		
		
		void Loaded()
		{
			LockData();
			
			EN.Clear();
			EN[ "Loaded"          ] = "WantedForMurder version {0} loaded.";
			EN[ "Commands"        ] = "WantedForMurder commands: /wanted /punish /forgive";
			EN[ "CommandsSheriff" ] = "WantedForMurder commands: /wanted /punish /forgive /pardon  (You are a sheriff.)";
			EN[ "Wanted"          ] = "<color=#ff3333ff>{0}</color> is wanted for murdering {1}.";
			EN[ "NobodyWanted"    ] = "Nobody is currently wanted for murder.";
			EN[ "Punish"          ] = "<color=#ff3333ff>{0}</color> is now wanted for murdering {1}!";
			EN[ "PunishAlready"   ] = "You have already punished {0}.";
			EN[ "PunishNotFound"  ] = "Couldn't find anyone to punish.";
			EN[ "PunishForbidden" ] = "You cannot punish anyone until you are forgiven by {0}.";
			EN[ "ForgiveBySome"   ] = "<color=#ff3333ff>{0}</color> has been forgiven by {1} but is still wanted.";
			EN[ "ForgiveByAll"    ] = "{0} has been forgiven by {1} and is no longer wanted.";
			EN[ "ForgiveKill"     ] = "You forgave {0}.";
			EN[ "ForgiveNotFound" ] = "Couldn't find anyone to forgive.";
			EN[ "Pardon"          ] = "{0} has been pardoned of all crimes by Sheriff {1}.";
			EN[ "PardonNotWanted" ] = "{0} was not wanted.";
			EN[ "PardonNotFound"  ] = "Couldn't find anyone to pardon.";
			EN[ "PardonForbidden" ] = "Only a sheriff may pardon crimes.  Try /forgive instead.";
			lang.RegisterMessages( EN, this );
			
			LoadConfig();
			if( SetUnconfiguredDefaults() )
				SaveConfig();
			LoadData();
			
			timer.Repeat( 1f, 0, () => { RemoveMurdererSleepingBags(); } );
			
			if( Config.Get<bool>("AnnounceWhenLoaded") )
				timer.Once( 1f, () => { ChatToAll( "Loaded", Version.ToString() ); } );
			
			UnlockData();
		}
	}
}
