using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CodeHatch.Damaging;
using CodeHatch.Engine.Networking;
using CodeHatch.Common;
using CodeHatch.Inventory.Blueprints;
using Oxide.Core;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.ItemContainer;
using CodeHatch.UserInterface.Dialogues;
using CodeHatch.Engine.Events.Prefab;
using CodeHatch.Blocks.Networking.Events;

namespace Oxide.Plugins
{
    [Info("No Friendly Fire", "D-Kay", "1.0")]
    public class NoFriendlyFire : ReignOfKingsPlugin
    {
        private void SendHelpText(Player player)
        {
            PrintToChat(player, "[0000FF]No Friendly Fire[FFFFFF]");
            PrintToChat(player, "[00FF00]/nff on[FFFFFF] - Turn the friendly fire safety on.");
            PrintToChat(player, "[00FF00]/nff off[FFFFFF] - Turn the friendly fire safety off.");
        }

        private Collection<string> _NoFriendlyFire = new Collection<string>();
        private void LoadData()
        {
            _NoFriendlyFire = Interface.GetMod().DataFileSystem.ReadObject<Collection<string>>("SavedExceptionList");
        }

        private void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("SavedExceptionList", _NoFriendlyFire);
        }

        void Loaded()
        {
            LoadData();
        }

        [ChatCommand("nff")]
        private void NoFriendlyFireOnOrOff(Player player, string cmd, string[] input)
        {
            var onoff = input[0];
            if (onoff == "off")
            {
                var position = -1;
                for (var i = 0; i < _NoFriendlyFire.Count; i++)
                {
                    if (_NoFriendlyFire[i] == player.DisplayName.ToLower())
                    {
                        position = i;
                        break;
                    }
                }

                if (position < 0)
                {
                    PrintToChat(player, "[4444FF]NFF[FFFFFF] : You already have NFF turned off.");
                    return;
                }

                _NoFriendlyFire.RemoveAt(position);
                PrintToChat(player, "[4444FF]NFF[FFFFFF] : NFF was turned off.");
                SaveData();
            }
            if (onoff == "on")
            {
                //Check if player is already on the list
                foreach (var tradeMaster in _NoFriendlyFire)
                {
                    if (tradeMaster.ToLower() == player.DisplayName.ToLower())
                    {
                        PrintToChat(player, "[4444FF]NFF[FFFFFF] : You already have NFF turned on.");
                        return;
                    }
                }

                // Add the player to the list
                _NoFriendlyFire.Add(player.DisplayName.ToLower());
                PrintToChat(player, "[4444FF]NFF[FFFFFF] : NFF was turned on.");
                SaveData();
            }
        }

        private void OnEntityHealthChange(EntityDamageEvent damageEvent)
        {
            foreach (var tradeMaster in _NoFriendlyFire)
            {
                if (tradeMaster.ToLower() == damageEvent.Entity.Owner.DisplayName.ToLower())
                {
                    if (
                        damageEvent.Damage.Amount > 0 // taking damage
                        && damageEvent.Entity.IsPlayer // entity taking damage is player
                        && damageEvent.Damage.DamageSource.IsPlayer // entity delivering damage is a player
                        && damageEvent.Entity != damageEvent.Damage.DamageSource // entity taking damage is not taking damage from self
                        && damageEvent.Entity.Owner.GetGuild().DisplayName == damageEvent.Damage.DamageSource.Owner.GetGuild().DisplayName // both entities are in the same guild
                        )
                    {
                        damageEvent.Cancel("No Friendly Fire");
                        damageEvent.Damage.Amount = 0f;
                    }
                }
            }
        }
    }
}
