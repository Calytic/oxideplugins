using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Oxide.Core.Plugins;
using Rust;
using System.Reflection;
namespace Oxide.Plugins
{

    /* ------------------- [ HOOKS ] -------------------

    [1] IsPlayerInArea(player, MinX, MinY, MaxX, MaxY); (Bool)
    [2] SetPlayerHealth(player, amount); (Void)
    [3] Slap(BasePlayer player, amount = 12); (Bool)
    [4] GivePlayerHealth(player, amount); (Void)
    [5] RemovePlayerHealth(player, amount); (Void)
    [6] Explode(BasePlayer player, damage = 60, times = 1); (Void)
    [7] IsPlayerInWater(player); (Bool)
    [8] HealAll(); (Void)
    [9] MoveEveryPlayerToPlayer(player); (Void)
    [10] GetGroundPosition(sourcepos); (Vecto3)
    [11] FindPlayer(stringtofind); (BasePlayer); [Reneb]
    [12] FindPlayerByID(id); (BasePlayer); [Reneb]

        Commands:
        Slap, Explode, Heal, HealAll, TpAll
    ----------------------------------------------------*/

    [Info("Hookie", "Norn", 0.1, ResourceId = 1518)]
    [Description("Useful hooks.")]
    public class Hookie : RustPlugin
    {
        void OnServerInitialized()
        {
            if(Config["Messages", "Teleported"] == null) { Puts("Configuration file out of date, resetting..."); LoadDefaultConfig(); }
        }
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating..."); Config.Clear();

            Config["General", "Commands"] = true;

            Config["Messages", "NoAuth"] = "<color=yellow>ERROR:</color> You don't have access to this command.";
            Config["Messages", "NoPlayersFound"] = "<color=yellow>ERROR:</color> No players found.";
            Config["Messages", "MultiplePlayers"] = "<color=yellow>ERROR:</color> Multiple players found.";
            Config["Messages", "Slap"] = "<color=yellow>INFO:</color> You have slapped <color=red>{name}</color>!";
            Config["Messages", "Slapped"] = "<color=yellow>INFO:</color> You have been slapped by <color=red>{name}</color>!";
            Config["Messages", "Explode"] = "<color=yellow>INFO:</color> You have exploded <color=red>{name}</color>!";
            Config["Messages", "Exploded"] = "<color=yellow>INFO:</color> You have been exploded by <color=red>{name}</color>!";
            Config["Messages", "Heal"] = "<color=yellow>INFO:</color> You have healed <color=green>{name}</color>!";
            Config["Messages", "Healed"] = "<color=yellow>INFO:</color> You have been healed by <color=green>{name}</color>!";
            Config["Messages", "TP"] = "<color=yellow>INFO:</color> You have teleported <color=green>{name}</color>!";
            Config["Messages", "Teleported"] = "<color=yellow>INFO:</color> You have been teleported by <color=green>{name}</color>!";

            Config["Admin", "MinLevel"] = 1;
            Config["Admin", "MaxLevel"] = 2;
        }
        private bool IsPlayerInArea(BasePlayer player, float MinX, float MinY, float MaxX, float MaxY)
        {
            if (player != null && player.isConnected)
            { float X = player.transform.position.x; float Y = player.transform.position.y; float Z = player.transform.position.z; if (X >= MinX && X <= MaxX && Y >= MinY && Y <= MaxY) { return true; } }
            return false;
        }
        private void Explode(BasePlayer player, float damage = 60, int times = 1)
        {
            if (player != null & player.isConnected) {
                for (int i = 1; i <= times; i++)
                { Effect.server.Run("assets/bundled/prefabs/fx/firebomb.prefab", player.transform.position); Effect.server.Run("assets/bundled/prefabs/fx/gas_explosion_small.prefab", player.transform.position); Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/rocket_explosion.prefab", player.transform.position); Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/rocket_explosion.prefab", player.transform.position); Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/rocket_explosion.prefab", player.transform.position); Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", player.transform.position); }
                player.Hurt(damage, global::Rust.DamageType.Explosion, null, true);
            }
        }
        private static LayerMask GROUND_MASKS = LayerMask.GetMask("Terrain", "World", "Construction");
        static Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, GROUND_MASKS))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }
        private object FindPlayerByID(ulong steamid)
        {
            BasePlayer targetplayer = BasePlayer.FindByID(steamid);
            if (targetplayer != null)
            {
                return targetplayer;
            }
            targetplayer = BasePlayer.FindSleeping(steamid);
            if (targetplayer != null)
            {
                return targetplayer;
            }
            return null;
        }
        private object FindPlayer(string tofind)
        {
            if (tofind.Length == 17)
            {
                ulong steamid; if (ulong.TryParse(tofind.ToString(), out steamid))
                { return FindPlayerByID(steamid); }
            }
            List<BasePlayer> onlineplayers = BasePlayer.activePlayerList as List<BasePlayer>; object targetplayer = null; foreach (BasePlayer player in onlineplayers.ToArray())
            {
                if (player.displayName.ToString() == tofind)
                    return player;
                else if (player.displayName.ToString().Contains(tofind))
                {
                    if (targetplayer == null)
                        targetplayer = player;
                    else
                        return Config["Messages", "MultiplePlayers"].ToString();
                }
            }
            if (targetplayer != null)
                return targetplayer; List<BasePlayer> offlineplayers = BasePlayer.sleepingPlayerList as List<BasePlayer>; foreach (BasePlayer player in offlineplayers.ToArray())
            {
                if (player.displayName.ToString() == tofind)
                    return player;
                else if (player.displayName.ToString().Contains(tofind))
                {
                    if (targetplayer == null)
                        targetplayer = player;
                    else
                        return Config["Messages", "MultiplePlayers"].ToString();
                }
            }
            if (targetplayer == null)
                return Config["Messages", "NoPlayersFound"].ToString(); return targetplayer;
        }
        private int MoveEveryPlayerToPlayer(BasePlayer user)
        {
            int count = 0; if (user == null) { return count; }
            float addon = 1; foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player != user)
                { Vector3 pos = player.transform.position; pos.z = pos.z + addon; var newSpawn = GetGroundPosition(pos); player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true); player.ClientRPCPlayer(null, player, "StartLoading"); if (BasePlayer.sleepingPlayerList.Contains(player) == false) BasePlayer.sleepingPlayerList.Add(player); player.transform.position = newSpawn; var LastPositionValue = typeof(BasePlayer).GetField("lastPositionValue", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)); LastPositionValue.SetValue(player, player.transform.position); player.ClientRPCPlayer(null, player, "ForcePositionTo", newSpawn); player.TransformChanged(); player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true); player.UpdateNetworkGroup(); player.SendNetworkUpdateImmediate(false); player.SendFullSnapshot(); player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, false); player.ClientRPCPlayer(null, player, "FinishLoading"); count++; addon++; }
            }
            if (count != 0) { Puts("Moved " + count.ToString() + " players to " + user.displayName + " [" + user.userID + "]."); }
            return count;
        }
        [ChatCommand("explode")]
        private void cmdExplode(BasePlayer player, string command, string[] args)
        {
            if (!Convert.ToBoolean(Config["General", "Commands"])) { return; }
            if (player.net.connection.authLevel >= Convert.ToInt32(Config["Admin", "MinLevel"]) && player.net.connection.authLevel <= Convert.ToInt32(Config["Admin", "MaxLevel"]))
            {
                if (args.Length == 1)
                {
                    object foundp = FindPlayer(args[0]);
                    BasePlayer target = foundp as BasePlayer;
                    if (target == null || !target.IsConnected())
                    {
                        PrintToChat(player, Config["Messages", "NoPlayersFound"].ToString()); return;
                    }
                    else
                    {
                        string parsed_config = Config["Messages", "Explode"].ToString();
                        parsed_config = parsed_config.Replace("{name}", target.displayName);
                        PrintToChat(player, parsed_config);
                        parsed_config = Config["Messages", "Exploded"].ToString();
                        parsed_config = parsed_config.Replace("{name}", player.displayName);
                        PrintToChat(target, parsed_config);
                        Puts(player.displayName + " has exploded " + target.displayName + " [ " + target.userID.ToString() + " ].");
                        Explode(target);
                    }
                }
            }
            else { PrintToChat(player, Config["Messages", "NoAuth"].ToString()); }
        }
        [ChatCommand("slap")]
        private void cmdSlap(BasePlayer player, string command, string[] args)
        {
            if (!Convert.ToBoolean(Config["General", "Commands"])) { return; }
            if (player.net.connection.authLevel >= Convert.ToInt32(Config["Admin", "MinLevel"]) && player.net.connection.authLevel <= Convert.ToInt32(Config["Admin", "MaxLevel"]))
            {
                if (args.Length == 1)
                {
                    object foundp = FindPlayer(args[0]);
                    BasePlayer target = foundp as BasePlayer;
                    if (target == null || !target.IsConnected())
                    {
                        PrintToChat(player, Config["Messages", "NoPlayersFound"].ToString()); return;
                    }
                    else
                    {
                        string parsed_config = Config["Messages", "Slap"].ToString();
                        parsed_config = parsed_config.Replace("{name}", target.displayName);
                        PrintToChat(player, parsed_config);
                        parsed_config = Config["Messages", "Slapped"].ToString();
                        parsed_config = parsed_config.Replace("{name}", player.displayName);
                        PrintToChat(target, parsed_config);
                        Puts(player.displayName + " has slapped " + target.displayName + " [ " + target.userID.ToString() + " ].");
                        Slap(target);
                    }
                }
            }
            else { PrintToChat(player, Config["Messages", "NoAuth"].ToString()); }
        }
        [ChatCommand("tpall")]
        private void cmdTPAll(BasePlayer player, string command, string[] args)
        {
            if (!Convert.ToBoolean(Config["General", "Commands"])) { return; }
            if (player.net.connection.authLevel >= Convert.ToInt32(Config["Admin", "MinLevel"]) && player.net.connection.authLevel <= Convert.ToInt32(Config["Admin", "MaxLevel"]))
            {
                string parsed_config = Config["Messages", "TP"].ToString();
                parsed_config = parsed_config.Replace("{name}", player.displayName);
                PrintToChat(parsed_config);
                MoveEveryPlayerToPlayer(player);
                PrintToChat("<color=yellow>"+player.displayName + "</color> has moved teleported every user.");
            }
            else { PrintToChat(player, Config["Messages", "NoAuth"].ToString()); }
        }
        [ChatCommand("healall")]
        private void cmdHealAll(BasePlayer player, string command, string[] args)
        {
            if (!Convert.ToBoolean(Config["General", "Commands"])) { return; }
            if (player.net.connection.authLevel >= Convert.ToInt32(Config["Admin", "MinLevel"]) && player.net.connection.authLevel <= Convert.ToInt32(Config["Admin", "MaxLevel"]))
            {
                string parsed_config = Config["Messages", "Healed"].ToString();
                parsed_config = parsed_config.Replace("{name}", player.displayName);
                PrintToChat(parsed_config);
                HealAll();
            }
            else { PrintToChat(player, Config["Messages", "NoAuth"].ToString()); }
        }
        [ChatCommand("heal")]
        private void cmdHeal(BasePlayer player, string command, string[] args)
        {
            if (!Convert.ToBoolean(Config["General", "Commands"])) { return; }
            if (player.net.connection.authLevel >= Convert.ToInt32(Config["Admin", "MinLevel"]) && player.net.connection.authLevel <= Convert.ToInt32(Config["Admin", "MaxLevel"]))
            {
                if (args.Length == 1)
                {
                    object foundp = FindPlayer(args[0]);
                    BasePlayer target = foundp as BasePlayer;
                    if (target == null || !target.IsConnected())
                    {
                        PrintToChat(player, Config["Messages", "NoPlayersFound"].ToString()); return;
                    }
                    else
                    {
                        string parsed_config = Config["Messages", "Heal"].ToString();
                        parsed_config = parsed_config.Replace("{name}", target.displayName);
                        PrintToChat(player, parsed_config);
                        parsed_config = Config["Messages", "Healed"].ToString();
                        parsed_config = parsed_config.Replace("{name}", player.displayName);
                        PrintToChat(target, parsed_config);
                        Puts(player.displayName + " has healed " + target.displayName + " [ " + target.userID.ToString() + " ].");
                        SetPlayerHealth(target, 100);
                    }
                }
            }
            else { PrintToChat(player, Config["Messages", "NoAuth"].ToString()); }
        }
        private void HealAll() { foreach (BasePlayer player in BasePlayer.activePlayerList) { if (player != null && player.isConnected) { player.health = 100; } } }
        private bool IsPlayerInWater(BasePlayer player) { if (player != null && player.isConnected) { return player.IsSwimming(); } return false; }
        private void RemovePlayerHealth(BasePlayer player, float hp) { if (player != null && player.isConnected) { player.health = player.health - hp; } }
        private void GivePlayerHealth(BasePlayer player, float hp) { if (player != null && player.isConnected) { player.health = player.health + hp; } }
        private void SetPlayerHealth(BasePlayer player, float hp) { if (player != null && player.isConnected) { player.health = hp; } }
        private bool Slap(BasePlayer player, float amount = 12)
        {
            if (player != null && player.isConnected)
            { float X = player.transform.position.x; float Y = player.transform.position.y; float Z = player.transform.position.z; Vector3 destination = new Vector3(X, Y + amount, Z); player.transform.position = destination; var LastPositionValue = typeof(BasePlayer).GetField("lastPositionValue", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)); LastPositionValue.SetValue(player, player.transform.position); player.ClientRPCPlayer(null, player, "ForcePositionTo", destination); player.TransformChanged(); return true; }
            return false;
        }
    }
}