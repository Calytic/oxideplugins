using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Facepunch;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Building Owners", "Reneb", "3.0.2")]
    class BuildingOwners : RustPlugin
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Static Fields
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Core.Configuration.DynamicConfigFile BuildingOwnersData;

        private static bool serverInitialized = false;
         
        int constructionLayer = LayerMask.GetMask("Construction", "Construction Trigger");

        string changeownerPermissions = "buildingowners.changeowner";


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void Loaded()
        {
            BuildingOwnersData = Interface.GetMod().DataFileSystem.GetDatafile("BuildingOwners");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"You don't have the permissions to use this command.","You don't have the permissions to use this command." },
                {"You are not allowed to use this command.","You are not allowed to use this command." },
                {"Syntax: /changeowner PLAYERNAME/STEAMID","Syntax: /changeowner PLAYERNAME/STEAMID" },
                {"Target player not found.","Target player not found." },
                {"New owner of this house is: {0}","New owner of this house is: {0}" },
                {"An admin gave you the ownership of this house","An admin gave you the ownership of this house" }
            }, this);
        }

        void OnServerInitialized() { serverInitialized = true; }

        void OnEntityBuilt(HeldEntity heldentity, GameObject gameobject)
        {
            if (!serverInitialized) return;

            var block = gameobject.GetComponent<BuildingBlock>();
            if (block == null) return;

            var player = heldentity.GetOwnerPlayer();
            if (player == null) return;

            var blockdata = FindBlockData(block);
            if (blockdata is string) return;

            SetBlockData(block, player.userID.ToString());
        }

        void OnServerSave()
        {
            SaveData();
        }
        void OnServerQuit()
        {
            SaveData();
        }

        void OnNewSave(string name)
        {
            Interface.Oxide.LogWarning("BuildingOwners: Wipe detected. Saving last buildingowners data in BuildingOwners_backup");

            var BuildingOwnersData_backup = Interface.GetMod().DataFileSystem.GetDatafile("BuildingOwners_backup");
            BuildingOwnersData_backup.Clear();
            var e = BuildingOwnersData.GetEnumerator();
            while(e.MoveNext())
            {
                BuildingOwnersData_backup[e.Current.Key] = e.Current.Value;
            }
            Interface.GetMod().DataFileSystem.SaveDatafile("BuildingOwners_backup");

            BuildingOwnersData = Interface.GetMod().DataFileSystem.GetDatafile("BuildingOwners");
            BuildingOwnersData.Clear();
            SaveData();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Data Management
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void SaveData()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("BuildingOwners");
        }

        void SetBlockData(BuildingBlock block, string steamid)
        {
            BuildingOwnersData[block.buildingID.ToString()] = steamid;
        }

        object FindBlockData(BuildingBlock block)
        {
            var buildingid = block.buildingID.ToString();
            if (BuildingOwnersData[buildingid] != null) return BuildingOwnersData[buildingid];
            return false;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // General Methods
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        string GetMsg(string key, object steamid = null) { return lang.GetMessage(key, this, steamid == null ? null : steamid.ToString()); }
        string GetMsg(string key, BasePlayer player = null) { return GetMsg(key, player == null ? null : player.userID.ToString()); }
        bool hasAccess(BasePlayer player, string permissionName) { if (player.net.connection.authLevel > 1) return true; return permission.UserHasPermission(player.userID.ToString(), permissionName); }

        object FindBuilding(BasePlayer player, Vector3 sourcePos, Vector3 sourceDirection)
        {
            RaycastHit rayhit;
            if (!UnityEngine.Physics.Raycast(sourcePos, sourceDirection, out rayhit, constructionLayer))
            {
                return GetMsg("Couldn't find any constructions", player);
            }
            var entity = rayhit.GetEntity();
            if (entity == null)
            {
                return GetMsg("Couldn't find any constructions", player);
            }
            var block = entity.GetComponent<BuildingBlock>();
            if (block == null)
            {
                return GetMsg("Couldn't find any constructions", player);
            }
            return block;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Chat Command
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("changeowner")]
        void cmdChatchangeowner(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player, changeownerPermissions))
            {
                SendReply(player, GetMsg("You are not allowed to use this command.",player));
                return; 
            }
            if (args == null || args.Length < 1)
            {
                SendReply(player, GetMsg("Syntax: /changeowner PLAYERNAME/STEAMID", player));
                return;
            }
            var target = BasePlayer.Find(args[0].ToString());
            if (target == null || target.net == null || target.net.connection == null)
            {
                SendReply(player, GetMsg("Target player not found.",player));
            }
            else
            {
                object block = FindBuilding(player, player.eyes.position, player.eyes.rotation * Vector3.forward);
                if ( block is string )
                {
                    SendReply(player, (string)block);
                }
                else
                {
                    var buildingblock = (BuildingBlock)block;
                    var userid = target.userID.ToString();
                    SetBlockData(buildingblock, userid);
                    SendReply(player, string.Format(GetMsg("New owner of this house is: {0}",player),target.displayName));
                    SendReply(target, GetMsg("An admin gave you the ownership of this house",target));
                }
            }
        }
    }
}