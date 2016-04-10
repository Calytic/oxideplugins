ï»¿/*
TODO:
- Add configuration and localization support
- Add cooldown option for taunting
- Add option for picking which taunts are allowed?
- Add option to only taunt prop's effect(s)
- Add option to show gibs for props or not
- Figure out why Hurt() isn't working for damage passing
- Fix OnPlayerInput checks not allowing players to be props sometimes (dictionary issue)
- Move taunt GUI button to better position
- Unselect active item if selected (make sure to restore fully)
- Update configuration to have usable defaults
- Update configuration automatically
- Whitelist objects to block bad prefabs
*/

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("HideAndSeek", "Wulf/lukespragg", 0.1, ResourceId = 0)]
    [Description("The classic game(mode) of hide and seek, as props.")]

    class HideAndSeek : RustPlugin
    {
        #region Configuration

        string NoPermission => Config.Get<string>("NoPermission");
        string PlayerHiding => Config.Get<string>("PlayerHiding");
        string PlayerNotHiding => Config.Get<string>("PlayerNotHiding");

        protected override void LoadDefaultConfig()
        {
            Config["NoPermission"] = "Sorry, you can't use 'hide' right now";
            Config["PlayerHiding"] = "<size=20>You're hiding... shhh!</size>";
            Config["PlayerNotHiding"] = "<size=20>You're no longer hiding, run!</size>";
        }

        #endregion

        #region General Setup

        readonly Dictionary<BaseEntity, BasePlayer> props = new Dictionary<BaseEntity, BasePlayer>();

        void Loaded()
        {
            permission.RegisterPermission("hideandseek.allowed", this);

            //foreach (var player in props.Values) TauntButton(player, null);
        }

        #endregion

        #region Player Info

        class OnlinePlayer
        {
            public BasePlayer Player;
            public bool IsHidden;
        }

        [OnlinePlayers] Hash<BasePlayer, OnlinePlayer> onlinePlayers = new Hash<BasePlayer, OnlinePlayer>();

        #endregion

        #region Player Restoring

        void OnPlayerInit(BasePlayer player)
        {
            if (!props.ContainsValue(player)) return;

            player.EndSleeping();
            SetPropFlags(player);
        }

        #endregion

        #region Prop Flags

        void SetPropFlags(BasePlayer player)
        {
            // Remove admin/developer flags
            if (player.IsAdmin()) player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
            if (player.IsDeveloper()) player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, false);

            // Change to third-person view
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.EyesViewmode, false);

            onlinePlayers[player].IsHidden = true;
        }

        void UnsetPropFlags(BasePlayer player)
        {
            // Change to normal view
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.EyesViewmode, false);

            // Restore admin/developer flags
            if (player.net.connection.authLevel > 0) player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            if (DeveloperList.IsDeveloper(player)) player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, true);

            onlinePlayers[player].IsHidden = false;
        }

        #endregion

        #region Player Hiding

        void HidePlayer(BasePlayer player)
        {
            // Make the player invisible
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
            player.gameObject.SetLayerRecursive(10);
            player.CancelInvoke("MetabolismUpdate");
            player.CancelInvoke("InventoryUpdate");

            // Set the player flags
            SetPropFlags(player);

            // Show the taunt button
            TauntButton(player, null);

            PrintToChat(player, PlayerHiding);
        }

        void UnhidePlayer(BasePlayer player)
        {
            // Make the player visible
            player.metabolism.Reset();
            player.InvokeRepeating("InventoryUpdate", 1f, 0.1f*Random.Range(0.99f, 1.01f));
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
            player.gameObject.SetLayerRecursive(17);

            // Set the player flags
            UnsetPropFlags(player);

            // Remove the taunt button
            CuiHelper.DestroyUi(player, tauntPanel);

            PrintToChat(player, PlayerNotHiding);
        }

        #endregion

        #region Chat Commands

        [ChatCommand("hide")]
        void HideChat(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "hideandseek.allowed"))
            {
                SendReply(player, NoPermission);
                return;
            }

            // Check if player is already hidden
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.Spectating) || onlinePlayers[player].IsHidden)
            {
                SendReply(player, "You're already hidden!");
                return;
            }

            var ray = new Ray(player.eyes.position, player.eyes.HeadForward());
            var entity = FindObject(ray, 3); // TODO: Make distance (3) configurable
            if (entity == null || props.ContainsKey(entity)) return;

            // Hide active item
            if (player.GetActiveItem() != null)
            {
                var heldEntity = player.GetActiveItem().GetHeldEntity() as HeldEntity;
                //heldEntity?.SetHeld(false);
            }

            // Hide the player
            HidePlayer(player);

            // Create the prop entity
            var propEntity = GameManager.server.CreateEntity(entity.name, player.transform.position, player.transform.rotation);
            propEntity.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
            propEntity.SendMessage("InitializeItem", entity, SendMessageOptions.DontRequireReceiver);
            propEntity.Spawn();
            props.Add(propEntity, player);
        }

        [ChatCommand("unhide")]
        void UnhideChat(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "hideandseek.allowed"))
            {
                SendReply(player, NoPermission);
                return;
            }

            // Check if player is already unhidden
            if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.Spectating) || !onlinePlayers[player].IsHidden)
            {
                SendReply(player, "You're already unhidden!");
                return;
            }

            // Unhide the player
            UnhidePlayer(player);

            // Remove the prop entity
            if (!props.ContainsValue(player)) return;
            BaseEntity propEntity = null;
            foreach (var prop in props.Where(prop => prop.Value == player)) propEntity = prop.Key;
            if (propEntity == null || propEntity.isDestroyed) return;
            props.Remove(propEntity);
            propEntity.Kill(BaseNetworkable.DestroyMode.Gib);
        }

        #endregion

        #region Prop Taunting

        [ChatCommand("taunt")]
        void TauntPlayer(BasePlayer player, string command, string[] args)
        {
            // Check if player is already unhidden
            if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.Spectating) || !onlinePlayers[player].IsHidden)
            {
                PrintToChat(player, "You're not a prop!");
                return;
            }

            var r = new System.Random();
            var taunts = new[]
            {
                "animals/bear/attack1",
                "animals/bear/attack2",
                "animals/bear/bite",
                "animals/bear/breathe-1",
                "animals/bear/breathing",
                "animals/bear/death",
                "animals/bear/roar1",
                "animals/bear/roar2",
                "animals/bear/roar3",
                "animals/boar/attack1",
                "animals/boar/attack2",
                "animals/boar/flinch1",
                "animals/boar/flinch2",
                "animals/boar/scream",
                "animals/chicken/attack1",
                "animals/chicken/attack2",
                "animals/chicken/attack3",
                "animals/chicken/cluck1",
                "animals/chicken/cluck2",
                "animals/chicken/cluck3",
                "animals/horse/attack",
                "animals/horse/flinch1",
                "animals/horse/flinch2",
                "animals/horse/heavy_breath",
                "animals/horse/snort",
                "animals/horse/whinny",
                "animals/horse/whinny_large",
                "animals/rabbit/attack1",
                "animals/rabbit/attack2",
                "animals/rabbit/run",
                "animals/rabbit/walk",
                "animals/stag/attack1",
                "animals/stag/attack2",
                "animals/stag/death1",
                "animals/stag/death2",
                "animals/stag/flinch1",
                "animals/stag/scream",
                "animals/wolf/attack1",
                "animals/wolf/attack2",
                "animals/wolf/bark",
                "animals/wolf/breathe",
                "animals/wolf/howl1",
                "animals/wolf/howl2",
                "animals/wolf/run_attack",
                "barricades/damage",
                "beartrap/arm",
                "beartrap/fire",
                //"bucket_drop_debris",
                "build/frame_place",
                //"build/promote_metal",
                //"build/promote_stone",
                //"build/promote_toptier",
                //"build/promote_wood",
                "build/repair",
                "build/repair_failed",
                "build/repair_full",
                "building/fort_metal_gib",
                "building/metal_sheet_gib",
                "building/stone_gib",
                "building/thatch_gib",
                "building/wood_gib",
                "door/door-metal-impact",
                "door/door-metal-knock",
                "door/door-wood-impact",
                "door/door-wood-knock",
                "door/lock.code.denied",
                "door/lock.code.lock",
                "door/lock.code.unlock",
                "door/lock.code.updated",
                //"entities/helicopter/heli_explosion",
                //"entities/helicopter/rocket_airburst_explosion",
                //"entities/helicopter/rocket_explosion",
                "entities/helicopter/rocket_fire",
                "entities/loot_barrel/gib",
                "entities/loot_barrel/impact",
                "entities/tree/tree-impact",
                //"fire/fire_v2",
                //"fire/fire_v3",
                //"fire_explosion",
                //"gas_explosion_small",
                "gestures/cameratakescreenshot",
                "gestures/guitarpluck",
                "gestures/guitarstrum",
                "headshot",
                "headshot_2d",
                "hit_notify",
                /*"impacts/additive/explosion",
                "impacts/blunt/clothflesh/clothflesh1",
                "impacts/blunt/concrete/concrete1",
                "impacts/blunt/metal/metal1",
                "impacts/blunt/wood/wood1",
                "impacts/bullet/clothflesh/clothflesh1",
                "impacts/bullet/concrete/concrete1",
                "impacts/bullet/dirt/dirt1",
                "impacts/bullet/forest/forest1",
                "impacts/bullet/metal/metal1",
                "impacts/bullet/metalore/bullet_impact_metalore",
                "impacts/bullet/path/path1",
                "impacts/bullet/rock/bullet_impact_rock",
                "impacts/bullet/sand/sand1",
                "impacts/bullet/snow/snow1",
                "impacts/bullet/tundra/bullet_impact_tundra",
                "impacts/bullet/wood/wood1",
                "impacts/slash/concrete/slash_concrete_01",
                "impacts/slash/metal/metal1",
                "impacts/slash/metal/metal2",
                "impacts/slash/metalore/slash_metalore_01",
                "impacts/slash/rock/slash_rock_01",
                "impacts/slash/wood/wood1",*/
                "item_break",
                "player/beartrap_clothing_rustle",
                "player/beartrap_scream",
                "player/groundfall",
                "player/howl",
                //"player/onfire",
                "repairbench/itemrepair",
                "ricochet/ricochet1",
                "ricochet/ricochet2",
                "ricochet/ricochet3",
                "ricochet/ricochet4",
                //"survey_explosion",
                //"weapons/c4/c4_explosion",
                "weapons/rifle_jingle1",
                "weapons/survey_charge/survey_charge_stick",
                "weapons/vm_machete/attack-1",
                "weapons/vm_machete/attack-2",
                "weapons/vm_machete/attack-3",
                "weapons/vm_machete/deploy",
                "weapons/vm_machete/hit"
            };
            var taunt = taunts[r.Next(taunts.Length)];

            //PrintToChat($"<size=20>{taunt}</size>");
            Effect.server.Run($"assets/bundled/prefabs/fx/{taunt}.prefab", player.transform.position, Vector3.zero);
        }

        #endregion

        #region Damage Passing

        object OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            if (entity is BasePlayer) return null;
            if (!props.ContainsKey(entity))
            {
                var attacker = info.Initiator as BasePlayer;
                attacker?.Hurt(info.damageTypes.Total());
                return true;
            };

            var propPlayer = props[entity];
            if (propPlayer.health <= 1)
            {
                propPlayer.Die();
                return null;
            }
            props[entity].InitializeHealth(propPlayer.health - info.damageTypes.Total(), 100f);

            return true;
        }

        #endregion

        #region Death Handling

        void OnEntityDeath(BaseEntity entity)
        {
            // Check for prop entity/player
            if (!props.ContainsValue(entity.ToPlayer())) return;
            var player = entity.ToPlayer();

            // Get the prop entity
            BaseEntity propEntity = null;
            foreach (var prop in props.Where(prop => prop.Value == player)) propEntity = prop.Key;

            // Unhide and respawn the player
            UnhidePlayer(player);
            props.Remove(player);
            player.RespawnAt(player.transform.position, player.transform.rotation);

            // Remove the prop entity
            if (propEntity && !propEntity.isDestroyed) propEntity.Kill(BaseNetworkable.DestroyMode.Gib);
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            // Remove all corpses
            if (entity.LookupShortPrefabName().Equals("player_corpse.prefab")) entity.KillMessage();
        }

        #endregion

        #region Spectate Blocking

        object OnRunCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.connection != null && arg.cmd.name == "spectate") return true;
            return null;
        }

        object OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!props.ContainsValue(player) && !player.IsSpectating() && input.WasJustPressed(BUTTON.FIRE_PRIMARY)) HideChat(player, null, null);
            if (props.ContainsValue(player) && player.IsSpectating() && input.WasJustPressed(BUTTON.FIRE_SECONDARY)) UnhideChat(player, null, null);
            if (props.ContainsValue(player) && player.IsSpectating() && input.WasJustPressed(BUTTON.JUMP) || input.WasJustPressed(BUTTON.DUCK)) return true;

            return null;
        }

        #endregion

        #region Console Commands

        [ConsoleCommand("global.taunt")]
        void TauntConsole(ConsoleSystem.Arg arg)
        {
            var player = BasePlayer.Find(arg.GetString(0));
            if (player) TauntPlayer(player, null, null);
        }

        #endregion

        #region GUI Button

        string tauntPanel;

        void TauntButton(BasePlayer player, string text)
        {
            var elements = new CuiElementContainer();
            tauntPanel = elements.Add(new CuiPanel
            {
                Image = {Color = "0.0 0.0 0.0 0.0"},
                RectTransform = { AnchorMin = "0.026 0.037", AnchorMax = "0.075 0.10" }
            }, "HUD/Overlay", "taunt");
            elements.Add(new CuiElement
            {
                Parent = tauntPanel,
                Components =
                {
                    new CuiRawImageComponent {Url = "http://i.imgur.com/28fdPww.png"},
                    new CuiRectTransformComponent {AnchorMin = "0.0 0.0", AnchorMax = "1.0 1.0"}
                }
            });
            elements.Add(new CuiButton
            {
                Button = {Command = $"taunt {player.userID}", Color = "0.0 0.0 0.0 0.0"},
                RectTransform = {AnchorMin = "0.026 0.037", AnchorMax = "0.075 0.10"},
                Text = {Text = ""}
            });
            CuiHelper.DestroyUi(player, tauntPanel);
            CuiHelper.AddUi(player, elements);
        }

        #endregion

        #region Cleanup Props

        void Unload()
        {
            foreach (var prop in props)
            {
                var propEntity = prop.Key;
                if (!propEntity.isDestroyed) propEntity.Kill(BaseNetworkable.DestroyMode.Gib);
                UnhidePlayer(prop.Value);
            }

            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, tauntPanel);
        }

        #endregion

        #region Helper Methods

        static BaseEntity FindObject(Ray ray, float distance)
        {
            RaycastHit hit;
            return !Physics.Raycast(ray, out hit, distance) ? null : hit.GetEntity();
        }

        bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        #endregion
    }
}
