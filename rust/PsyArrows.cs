using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Text;
using Rust;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("PsyArrows", "Psyk", "0.1", ResourceId = 714)]
    [Description("More Arrow Types For Rust! - By Psyk.")]
    class PsyArrows : RustPlugin
    {
        string arrow_type;
        bool enabled;
        float detonationTime = 5f;
        float projectileSpeed = 90f;
        float gravityModifier = 0f;
        float expRad = 100f;
        int arrow_curr = 374890416;
        int arrow_price = 30;
        bool sticky = true;
        bool highjump = false;
        static System.Random rnd = new System.Random();

        Dictionary<ulong, string> bowmen = new Dictionary<ulong, string>(); // steamid, arrow_type

        int GetAmountItems(BasePlayer player)
        {
            Item item = player.inventory.FindItemID("metal.refined");
            int item_id = item.info.itemid;

            return player.inventory.containerMain.GetAmount(item_id, true);
        }

        void ArrowBleed(BasePlayer player)
        {
            player.health = player.health - rnd.Next(3, 8);
        }


        void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            if (hitInfo != null && hitInfo.WeaponPrefab.ToString().Contains("hunting") || hitInfo.WeaponPrefab.ToString().Contains("bow") && attacker.IsAdmin())
            {
                /*if (hitInfo.HitEntity.gameObject.name.Contains("player"))
                {*/



                var hitPlayer = hitInfo.HitEntity.ToPlayer();

                // hitPlayer.health = hitPlayer.health - rnd.Next(15, 25);
                // hitPlayer.Hurt(hitPlayer.metabolism.bleeding.@value * 2f, DamageType.Bleeding, null, true);
                //timer.Repeat(3, rnd.Next(1, 5), () => ArrowBleed(hitPlayer));
                //hitPlayer.ChatMessage("<color='red'> You've begun to bleed. :( </color>");
                if (/*attacker.IsAdmin() || attacker.userID == 76561198031895400*/ true)
                    {
                        if (bowmen.TryGetValue(attacker.userID, out arrow_type))
                        {
                            switch (arrow_type)
                            {
                                case "wind":
                                    // Hit a player
                                    var plyX = hitPlayer.transform.position.x;
                                    var plyY = hitPlayer.transform.position.y;
                                    var plyZ = hitPlayer.transform.position.z;
                                    int constant = rnd.Next(4, 5);
                                    var new_vector = new Vector3(plyX + constant, plyY + rnd.Next(5, 8), plyZ + constant);
                                    hitPlayer.MovePosition(new_vector);
                                    attacker.ChatMessage("[<color='lime'>PsyArrow</color>] You hit " + hitPlayer.displayName);
                                    bowmen.Remove(attacker.userID);
                                break;

                                case "fire":
                                    CreateRocket(hitPlayer.transform.position, hitPlayer.transform.position, true);
                                    attacker.ChatMessage("[<color='lime'>PsyArrow</color>] You hit " + hitPlayer.displayName + " Health : " + hitPlayer.health);
                                    bowmen.Remove(attacker.userID);
                                    break;

                                case "explosive":
                                     CreateRocket(hitPlayer.transform.position, hitPlayer.transform.position, false);
                                     bowmen.Remove(attacker.userID);
                                    break;
                                case "knockdown":
                                    attacker.ChatMessage("[<color='lime'>PsyArrow</color>] You knocked " + hitPlayer.displayName + " down! Save him or kill him.");
                                    hitPlayer.StartWounded();
                                    bowmen.Remove(attacker.userID);
                                    break;
                                case "narco":
                                    hitPlayer.StartSleeping();
                                    attacker.ChatMessage("[<color='lime'>PsyArrow</color>] You hit " + hitPlayer.displayName);
                                    attacker.inventory.Take(attacker.inventory.FindItemIDs(arrow_curr), arrow_curr, arrow_price);
                                    bowmen.Remove(attacker.userID);
                                    break;
                                case "poision":
                                    var poisionTimer = timer.Repeat(rnd.Next(1, 3), rnd.Next(5, 10), () => poisionPlayer(hitPlayer));
                                    bowmen.Remove(attacker.userID);
                                    break;
                            }
                        }
                        else
                        {
                            attacker.ChatMessage("<color='red'>PsyArrows</color> Your bow is not drawn! You must select an arrow.");
                        }

                        if (hitInfo.isHeadshot)
                        {
                            // Hit a player
                            var plyX = hitPlayer.transform.position.x;
                            var plyY = hitPlayer.transform.position.y;
                            var plyZ = hitPlayer.transform.position.z;
                            var new_vector = new Vector3(plyX + 1, plyY + rnd.Next(100, 200), plyZ);
                            hitPlayer.MovePosition(new_vector);
                            attacker.ChatMessage("[<color='lime'>PsyArrow</color>] You hit " + hitPlayer.displayName);
                            hitPlayer.health = 1;
                        }
                    }
                    else
                    {
                        if (GetAmountItems(attacker) >= arrow_price)
                        {
                            if (bowmen.TryGetValue(attacker.userID, out arrow_type))
                            {
                                switch (arrow_type)
                                {
                                    case "narco":
                                        hitPlayer.StartSleeping();
                                        attacker.ChatMessage("[<color='lime'>PsyArrow</color>] You hit " + hitPlayer.displayName);
                                        attacker.inventory.Take(attacker.inventory.FindItemIDs(arrow_curr), arrow_curr, arrow_price);
                                        //arrow_type = "normal";
                                        bowmen.Remove(attacker.userID);
                                        break;
                                    case "poision":
                                        var poisionTimer = timer.Repeat(rnd.Next(1, 3), rnd.Next(5, 10), () => poisionPlayer(hitPlayer));
                                        attacker.inventory.Take(attacker.inventory.FindItemIDs(arrow_curr), arrow_curr, arrow_price);
                                        //arrow_type = "normal";
                                        bowmen.Remove(attacker.userID);
                                        break;
                                }
                            }
                            else
                            {
                                attacker.ChatMessage("[<color='red'>PsyArrows]</color> Your bow is not drawn! You must select an arrow.");

                            }
                        }
                        else
                        {
                            attacker.ChatMessage("<color='red'>PsyArrows</color> Sorry, " + attacker.displayName + ", but you require [" + arrow_price + "]" + "["+arrow_curr+"] to fire these arrows.");
                        }

                    }
                /*}
                else
                {
                    if(arrow_type == "normal")
                    {

                    }
                    else
                    {
                        attacker.ChatMessage("[<color='red'>PsyArrow</color>] You have to hit a valid player with this type of arrow!");
                    }
                }*/
            }
        }

        private static readonly FieldInfo ServerInput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));




        private BaseEntity CreateRocket(Vector3 startPoint, Vector3 direction, bool isFireRocket)
        {
            ItemDefinition projectileItem;

            if (isFireRocket)
                projectileItem = GetFireRocket();
            else
                projectileItem = GetRocket();

            ItemModProjectile component = projectileItem.GetComponent<ItemModProjectile>();
            BaseEntity entity = GameManager.server.CreateEntity(component.projectileObject.resourcePath, startPoint, new Quaternion(), true);

            TimedExplosive timedExplosive = entity.GetComponent<TimedExplosive>();
            ServerProjectile serverProjectile = entity.GetComponent<ServerProjectile>();

            serverProjectile.gravityModifier = gravityModifier;
            serverProjectile.speed = projectileSpeed;
            timedExplosive.timerAmountMin = detonationTime;
            timedExplosive.timerAmountMax = detonationTime;
            timedExplosive.explosionRadius = 1000f;
            timedExplosive.canStick = true;

            entity.SendMessage("InitializeVelocity", (object)(direction * 2f));
            entity.Spawn(true);

            return entity;
        }

        private ItemDefinition GetRocket()
        {
            return ItemManager.FindItemDefinition("ammo.rocket.basic");
        }

        private ItemDefinition GetFireRocket()
        {
            return ItemManager.FindItemDefinition("ammo.rocket.fire");
        }

        void poisionPlayer(BasePlayer player)
        {
            player.health = player.health - rnd.Next(2, 4);
            player.ChatMessage("[<color='lime'>PsyArrow : POISION</color>] You've been poisioned! It should wear off soon..");
        }



        string GetVictim(BaseCombatEntity vic)
        {
            string victim = "Unknown Victim";

            if (vic != null)
            {
                if (vic.ToPlayer() != null)
                {
                    victim = vic.ToPlayer().displayName; // Get Name of the Victim.
                }
            }
            return victim;
        }

        [ChatCommand("arrow")]
        void arrow(BasePlayer player, string command, string[] args)
        {
            Puts(player.playerFlags.ToString());
            if (args.Length >= 1)
            {
                switch (args[0])
                {
                    case "narco":
                        arrow_type = "narco";
                        break;
                    case "wind":
                        arrow_type = "wind";
                        break;
                    case "poision":
                        arrow_type = "poision";
                        break;
                    case "fire":
                        arrow_type = "fire";
                        break;
                    case "explosive":
                        arrow_type = "explosive";
                        break;
                    case "knockdown":
                        arrow_type = "knockdown";
                        break;
                    case "slow":
                        arrow_type = "slow";
                        break;
                    default:
                        player.ChatMessage("[<color='orange'>PsyArrow</color>] Arrow Types \n 1) narco \n 2) wind \n 3) poision \n 4) fire \n 5) explosive \n 6) knockdown \n 7) slow ");
                        break;
                }


                player.ChatMessage("[<color='cyan'>PsyArrow</color>] Arrow type switched to [<color='lime'> " + arrow_type + " </color>]");
                bowmen.Add(player.userID, arrow_type);
            }
            else
            {
                player.ChatMessage("[<color='orange'>PsyArrow</color>] Arrow Types \n [USAGE: /arrow arrow_type : Ex. /arrow narco ]\n 1) narco \n 2) wind \n 3) poision \n 4) fire \n 5) explosive \n 6) knockdown \n 7) slow ");

            }
        }
    }
}
