 using System;
 using CodeHatch.Engine.Networking;
 using CodeHatch.Common;
 using CodeHatch.Networking.Events.Entities;


namespace Oxide.Plugins
{
    [Info("BattleBoss", "D-Kay", "1.2.0")]
    public class BigBoss : ReignOfKingsPlugin
    {
        private const double damageReduction = 60; // Amount reduce damage against the boss. Use 1 - 100 (100 is normal damage taken)
        private const double damageIncrease = 250; // Percentage to increase the boss's damage. 200% is double the normal damage, etc.


        private Player Boss1;
        private Player Boss2;
        private Player Boss3;
        private Player Boss4;
        private Player Boss5;

        [ChatCommand("setboss")]
        private void SetTheCurrentRaidBoss(Player player, string cmd, string[] input)
        {
            if (!player.HasPermission("admin") && !player.HasPermission("support"))
            {
                PrintToChat(player, "You must be an admin to use this command.");
                return;
            }

            // Get target's name
            var playerName = ConvertArrayToString(input);

            var targetPlayer = Server.GetPlayerByName(playerName);
            if (targetPlayer == null)
            {
                PrintToChat(player, "That player does not appear to be online.");
                return;
            }

            //Set the player to be the boss
            SetTheBoss(targetPlayer);
            PrintToChat("[FF0000]Battle[FFFFFF] : " + targetPlayer.DisplayName + " has been turned into a devastating evil knight by the Gods! Kill him quick!");
        }

        [ChatCommand("listboss")]
        private void ListBosses(Player player, string cmd)
        {
            if (!player.HasPermission("admin") && !player.HasPermission("support"))
            {
                PrintToChat(player, "You must be an admin to use this command.");
                return;
            }
            if (Boss1 != null)
            {
                PrintToChat("[FF0000]Battle[FFFFFF] : " + Boss1.DisplayName + " is boss 1.");
            }
            if (Boss2 != null)
            {
                PrintToChat("[FF0000]Battle[FFFFFF] : " + Boss2.DisplayName + " is boss 2.");
            }
            if (Boss3 != null)
            {
                PrintToChat("[FF0000]Battle[FFFFFF] : " + Boss3.DisplayName + " is boss 3.");
            }
            if (Boss4 != null)
            {
                PrintToChat("[FF0000]Battle[FFFFFF] : " + Boss4.DisplayName + " is boss 4.");
            }
            if (Boss5 != null)
            {
                PrintToChat("[FF0000]Battle[FFFFFF] : " + Boss5.DisplayName + " is boss 5.");
            }
            PrintToChat("[FF0000]Battle[FFFFFF] : There currently are no bosses!");
        }

        [ChatCommand("removeboss")]
        private void RemoveTheCurrentRaidBoss(Player player, string cmd, string[] input)
        {
            if (!player.HasPermission("admin") && !player.HasPermission("support"))
            {
                PrintToChat(player, "You must be an admin to use this command.");
                return;
            }

            //Reset the boss variable to null
            var BossNumber = input[0];
            if (BossNumber == "1")
            {
                Boss1 = null;
                PrintToChat("[FF0000]Battle[FFFFFF] : An evil knight has been reduced to a mere mortal.");
                return;
            }
            if (BossNumber == "2")
            {
                Boss2 = null;
                PrintToChat("[FF0000]Battle[FFFFFF] : An evil knight has been reduced to a mere mortal.");
                return;
            }
            if (BossNumber == "3")
            {
                Boss3 = null;
                PrintToChat("[FF0000]Battle[FFFFFF] : An evil knight has been reduced to a mere mortal.");
                return;
            }
            if (BossNumber == "4")
            {
                Boss4 = null;
                PrintToChat("[FF0000]Battle[FFFFFF] : An evil knight has been reduced to a mere mortal.");
                return;
            }
            if (BossNumber == "5")
            {
                Boss5 = null;
                PrintToChat("[FF0000]Battle[FFFFFF] : An evil knight has been reduced to a mere mortal.");
                return;
            }
            if (BossNumber == "all")
            {
                Boss1 = null;
                Boss2 = null;
                Boss3 = null;
                Boss4 = null;
                Boss5 = null;
                PrintToChat("[FF0000]Battle[FFFFFF] : All evil knights have been reduced to mere mortals once more.");
                return;
            }
        }

        private void SetTheBoss(Player player)
        {
            if (Boss1 == null)
            {
                Boss1 = player;
                PrintToChat(player, "[FF2222]" + player.DisplayName + "[FFFFFF] was set to boss 1.");
                return;
            }

            if (Boss2 == null)
            {
                Boss2 = player;
                PrintToChat(player, "[FF2222]" + player.DisplayName + "[FFFFFF] was set to boss 2.");
                return;
            }

            if (Boss3 == null)
            {
                Boss3 = player;
                PrintToChat(player, "[FF2222]" + player.DisplayName + "[FFFFFF] was set to boss 3.");
                return;
            }

            if (Boss4 == null)
            {
                Boss4 = player;
                PrintToChat(player, "[FF2222]" + player.DisplayName + "[FFFFFF] was set to boss 4.");
                return;
            }

            if (Boss5 == null)
            {
                Boss5 = player;
                PrintToChat(player, "[FF2222]" + player.DisplayName + "[FFFFFF] was set to boss 5.");
                return;
            }
            PrintToChat("[FF0000]Battle[FFFFFF] : There currently can't be any more bosses!");
        }


        private void OnEntityHealthChange(EntityDamageEvent damageEvent)
        {
            //var attacker = damageEvent.Damage.DamageSource.Owner;
            var target = damageEvent.Entity.Owner;
            if (Boss1 == null && Boss2 == null && Boss3 == null && Boss4 == null && Boss5 == null) return;

            //Other creatures on the server
            //if(attacker.DisplayName == "Server") return;					

            //Was the boss hurt by a player?
            if (target == Boss1)
            {
                if (damageEvent.Damage.Amount > 0 // taking damage
                        && damageEvent.Entity.IsPlayer // entity taking damage is player
                        && damageEvent.Damage.DamageSource.IsPlayer // entity delivering damage is a player
                        && damageEvent.Entity != damageEvent.Damage.DamageSource // entity taking damage is not taking damage from self
                ) {
                    PrintToChat(damageEvent.Damage.DamageSource.Owner, "[FF0000]Battle[FFFFFF] : Your attacks are doing less damage to this person!");
                    double damageTaken = damageEvent.Damage.Amount * (damageReduction / 100);
                    damageEvent.Damage.Amount = (int)damageTaken;
                }
            }
            if (target == Boss2)
            {
                if (damageEvent.Damage.Amount > 0 // taking damage
                        && damageEvent.Entity.IsPlayer // entity taking damage is player
                        && damageEvent.Damage.DamageSource.IsPlayer // entity delivering damage is a player
                        && damageEvent.Entity != damageEvent.Damage.DamageSource // entity taking damage is not taking damage from self
                )
                {
                    PrintToChat(damageEvent.Damage.DamageSource.Owner, "[FF0000]Battle[FFFFFF] : Your attacks are doing less damage to this person!");
                    double damageTaken = damageEvent.Damage.Amount * (damageReduction / 100);
                    damageEvent.Damage.Amount = (int)damageTaken;
                }
            }
            if (target == Boss3)
            {
                if (damageEvent.Damage.Amount > 0 // taking damage
                        && damageEvent.Entity.IsPlayer // entity taking damage is player
                        && damageEvent.Damage.DamageSource.IsPlayer // entity delivering damage is a player
                        && damageEvent.Entity != damageEvent.Damage.DamageSource // entity taking damage is not taking damage from self
                )
                {
                    PrintToChat(damageEvent.Damage.DamageSource.Owner, "[FF0000]Battle[FFFFFF] : Your attacks are doing less damage to this person!");
                    double damageTaken = damageEvent.Damage.Amount * (damageReduction / 100);
                    damageEvent.Damage.Amount = (int)damageTaken;
                }
            }
            if (target == Boss4)
            {
                if (damageEvent.Damage.Amount > 0 // taking damage
                        && damageEvent.Entity.IsPlayer // entity taking damage is player
                        && damageEvent.Damage.DamageSource.IsPlayer // entity delivering damage is a player
                        && damageEvent.Entity != damageEvent.Damage.DamageSource // entity taking damage is not taking damage from self
                )
                {
                    PrintToChat(damageEvent.Damage.DamageSource.Owner, "[FF0000]Battle[FFFFFF] : Your attacks are doing less damage to this person!");
                    double damageTaken = damageEvent.Damage.Amount * (damageReduction / 100);
                    damageEvent.Damage.Amount = (int)damageTaken;
                }
            }
            if (target == Boss5)
            {
                if (damageEvent.Damage.Amount > 0 // taking damage
                        && damageEvent.Entity.IsPlayer // entity taking damage is player
                        && damageEvent.Damage.DamageSource.IsPlayer // entity delivering damage is a player
                        && damageEvent.Entity != damageEvent.Damage.DamageSource // entity taking damage is not taking damage from self
                )
                {
                    PrintToChat(damageEvent.Damage.DamageSource.Owner, "[FF0000]Battle[FFFFFF] : Your attacks are doing less damage to this person!");
                    double damageTaken = damageEvent.Damage.Amount * (damageReduction / 100);
                    damageEvent.Damage.Amount = (int)damageTaken;
                }
            }

            if (target != Boss1 && target != Boss2 && target != Boss3 && target != Boss4 && target != Boss5)
            {
                //Did the boss hurt another player's face?
                if (damageEvent.Damage.Amount > 0 // taking damage
                        && damageEvent.Entity.IsPlayer // entity taking damage is player
                        && damageEvent.Damage.DamageSource.IsPlayer // entity delivering damage is a player
                        && damageEvent.Entity != damageEvent.Damage.DamageSource // entity taking damage is not taking damage from self
                ) {
                    PrintToChat(damageEvent.Entity.Owner, "[FF0000]Battle[FFFFFF] : Your foe deals you a devastating blow!");
                    double damageGiven = damageEvent.Damage.Amount * (damageIncrease / 100);
                    damageEvent.Damage.Amount = (int)damageGiven;
                }
            }
        }

        private void OnEntityDeath(EntityDeathEvent deathEvent)
        {
            if (deathEvent.Entity.Owner == Boss1)
            {
                Boss1 = null;
                BossDeath();
                return;
            }
            if (deathEvent.Entity.Owner == Boss2)
            {
                Boss2 = null;
                BossDeath();
                return;
            }
            if (deathEvent.Entity.Owner == Boss3)
            {
                Boss3 = null;
                BossDeath();
                return;
            }
            if (deathEvent.Entity.Owner == Boss4)
            {
                Boss4 = null;
                BossDeath();
                return;
            }
            if (deathEvent.Entity.Owner == Boss5)
            {
                Boss5 = null;
                BossDeath();
                return;
            }
        }

        private void BossDeath() 
        { 
            PrintToChat("[FF0000]Battle[FFFFFF] : An evil knight has been reduced to a mere mortal.");
            if (Boss1 == null && Boss2 == null && Boss3 == null && Boss4 == null && Boss5 == null)
            {
                PrintToChat("[FF0000]Battle[FFFFFF] : All evil knights have been reduced to mere mortals once more.");
            }
        }

        private string ConvertArrayToString(string[] textArray)
        {
            var newText = textArray[0];
            if (textArray.Length > 1)
            {
                for (var i = 1; i < textArray.Length; i++)
                {
                    newText = newText + " " + textArray[i];
                }
            }
            return newText;
        }
	}
}
