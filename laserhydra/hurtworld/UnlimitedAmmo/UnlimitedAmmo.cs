//Reference: UnityEngine.UI
using System.Collections.Generic;
using Assets.Scripts.Core;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("UnlimitedAmmo", "LaserHydra", "1.1.1", ResourceId = 1619)]
    [Description("Allows you to have unlimited ammo.")]
    class UnlimitedAmmo : HurtworldPlugin
    {
        List<string> ids = new List<string>();
        List<PlayerSession> unlimitedAmmo = new List<PlayerSession>();

        ////////////////////////////////////////
        ///     Plugin Related Hooks
        ////////////////////////////////////////

        void Loaded()
        {
#if !HURTWORLD
            throw new NotSupportedException("This plugin or the version of this plugin does not support this game!");
#endif

            RegisterPerm("use");
            LoadData();
            LoadMessages();

            unlimitedAmmo = (from current in GameManager.Instance.GetSessions().Values
                             where current != null && current.Name != null && current.IsLoaded && ids.Contains(current.SteamId.ToString())
                             select current).ToList();

            foreach (PlayerSession current in unlimitedAmmo)
                if (current != null && current.Name != null && current.IsLoaded)
                    LoadAmmo(current);
            
            timer.Repeat(2.5F, 0, () => {
                foreach (PlayerSession current in unlimitedAmmo)
                    if (current != null && current.Name != null && current.IsLoaded)
                        LoadAmmo(current);
            });
        }

        void OnPlayerConnected(PlayerSession player)
        {
            if (ids.Contains(player.SteamId.ToString()))
                unlimitedAmmo.Add(player);
        }

        void OnPlayerDisconnected(PlayerSession player)
        {
            if (unlimitedAmmo.Contains(player))
                unlimitedAmmo.Remove(player);
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        void LoadData()
        {
            ids = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("UnlimitedAmmo_Users");
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("UnlimitedAmmo_Users", ids);
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Enabled", "You now have unlimited ammo!"},
                {"Disabled", "You no longer have unlimited ammo!"}
            }, this);
        }

        ////////////////////////////////////////
        ///     Ammo Related
        ////////////////////////////////////////

        /*void OnWeaponFired(IItem item, EquippedHandlerBase handler)
        {
        }*/

        void LoadAmmo(PlayerSession player)
        {
			if(!player.IsLoaded)
				return;
			
            NetworkEntityComponentBase netEntity = player.WorldPlayerEntity.GetComponent<NetworkEntityComponentBase>();

            if (netEntity == null)
                return;

            EquippedHandlerBase equippedHandler = netEntity.GetComponent<EquippedHandlerBase>();

            if (equippedHandler == null)
                return;

            EquippedHandlerServer equippedHandlerServer = equippedHandler as EquippedHandlerServer;

            if (equippedHandlerServer == null)
                return;

            ItemInstance equippedItem = equippedHandler.GetEquippedItem();

            if (equippedItem == null)
                return;

            GunItem gunItem = equippedItem.Item as GunItem;
            BowItem bowItem = equippedItem.Item as BowItem;

            //PrintWarning($"Item: {equippedItem.Item.GetNameKey().Split('/').Last()}");
            //PrintWarning($"Is Gun: {!(gunItem == null)}");

            if ((bowItem != null || gunItem != null) && equippedHandlerServer != null)
            {
                if (gunItem != null)
                {
                    AutomaticGunItem aGunItem = gunItem as AutomaticGunItem;
                    GunItemEquippedState gunEquipState = gunItem.EquippedState(equippedHandler);

                    //PrintWarning($"Is Automatic: {!(aGunItem == null)}");
                    //PrintWarning($"Clip Size: {gunItem.GetClipSize().ToString()}");
                    //PrintWarning($"Ammo Count: {equippedItem.AuxData.ToString()}");

                    //if (equippedItem.AuxData <= 1)
                    //{
                        equippedItem.AuxData = Convert.ToByte(gunItem.GetClipSize());
                        equippedHandlerServer.AuxSync();
                    //}
                }
                else
                {
                    PlayerInventory inventory = player.WorldPlayerEntity.GetComponent<PlayerInventory>();

                    //PrintWarning($"Has Ammo: {inventory.HasItem(bowItem.GetAmmoType().ItemId, 1)}");

                    if (!inventory.HasItem(bowItem.GetAmmoType().ItemId, 1))
                        GiveItem(player, bowItem.GetAmmoType(), 1);
                }
            }
        }

        void GiveItem(PlayerSession player, IItem item, int amount)
        {
            PlayerInventory inventory = player.WorldPlayerEntity.GetComponent<PlayerInventory>();
            ItemInstance itemInstance = new ItemInstance(item, amount);
            
            inventory.GiveItemServer(itemInstance);
        }

        ////////////////////////////////////////
        ///     Commands
        ////////////////////////////////////////

        [ChatCommand("toggleammo")]
        void ToggleAmmo(PlayerSession player, string cmd, string[] args)
        {
            if(!HasPerm(player.SteamId, "use"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if(ids.Contains(player.SteamId.ToString()))
            {
                ids.Remove(player.SteamId.ToString());

                if (unlimitedAmmo.Contains(player))
                    unlimitedAmmo.Remove(player);

                SendChatMessage(player, GetMsg("Disabled", player.SteamId));
            }
            else
            {
                ids.Add(player.SteamId.ToString());

                if (!unlimitedAmmo.Contains(player))
                    unlimitedAmmo.Add(player);

                SendChatMessage(player, GetMsg("Enabled", player.SteamId));
            }

            SaveData();
        }

        ////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////

        PlayerSession GetPlayer(string searchedPlayer, PlayerSession player)
        {
            foreach (PlayerSession current in GameManager.Instance.GetSessions().Values)
                if (current != null && current.Name != null && current.IsLoaded && current.Name.ToLower() == searchedPlayer.ToLower())
                    return current;

            List<PlayerSession> foundPlayers =
                (from current in GameManager.Instance.GetSessions().Values
                 where current != null && current.Name != null && current.IsLoaded && current.Name.ToLower().Contains(searchedPlayer.ToLower())
                 select current).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    SendChatMessage(player, "The player can not be found.");
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    List<string> playerNames = (from current in foundPlayers select current.Name).ToList();
                    string players = ListToString(playerNames, 0, ", ");
                    SendChatMessage(player, "Multiple matching players found: \n" + players);
                    break;
            }

            return null;
        }

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        ////////////////////////////////////////
        ///     Config & Message Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID.ToString());
        }

        ////////////////////////////////////////
        ///     Permission Related
        ////////////////////////////////////////

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        bool HasPerm(object uid, params string[] permArray)
        {
            uid = uid.ToString();
            string perm = ListToString(permArray.ToList(), 0, ".");

            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        string PermissionPrefix
        {
            get
            {
                return this.Title.Replace(" ", "").ToLower();
            }
        }

        ////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => hurt.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(PlayerSession player, string prefix, string msg = null) => hurt.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);
    }
}
