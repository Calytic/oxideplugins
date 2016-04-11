using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("BuildBlocker", "Bombardir", "1.3.4", ResourceId = 834)]
    class BuildBlocker : RustPlugin
    {
        #region Config

        static bool OnRoad = false;
        static bool OnRiver = false;
        static bool OnRock = false;
        static bool InTunnel = true;
        static bool InRock = true;
        static bool InCave = false;
        static bool InWarehouse = true;
        static bool InMetalBuilding = true;
        static bool InHangar = true;
        static bool InTank = true;
        static bool InBase = true;
        static bool InStormDrain = false;
        static bool UnTerrain = true;
        static bool UnBridge = false;
        static bool UnRadio = false;
        static bool BlockStructuresHeight = false;
        static bool BlockDeployablesHeight = false;
        static int MaxHeight = 100;
        static bool BlockStructuresWater = false;
        static bool BlockDeployablesWater = false;
        static int MaxWater = -2;
        static int AuthLVL = 2;
        static string Msg = "Hey! You can't build here!";
        static string MsgHeight = "You can't build here! (Height limit 100m)";
        static string MsgWater = "You can't build here! (Water limit -2m)";

        void LoadDefaultConfig() { }

        void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
				Config[Key] = var;
        }

        void Init() 
        {
            CheckCfg<bool>("Block On Roads", ref OnRoad);
            CheckCfg<bool>("Block On Rivers", ref OnRiver);
            CheckCfg<bool>("Block On Rock", ref OnRock);
            CheckCfg<bool>("Block In Rock", ref InRock);
            CheckCfg<bool>("Block In Rock Cave", ref InCave);
            CheckCfg<bool>("Block In Storm Drain", ref InStormDrain);
            CheckCfg<bool>("Block In Tunnel", ref InTunnel);
            CheckCfg<bool>("Block In Base", ref InBase);
            CheckCfg<bool>("Block In Warehouse", ref InWarehouse);
            CheckCfg<bool>("Block In Metal Building", ref InMetalBuilding);
            CheckCfg<bool>("Block In Hangar", ref InHangar);
            CheckCfg<bool>("Block Under Terrain", ref UnTerrain);
            CheckCfg<bool>("Block Under|On Metal Sphere", ref InTank);
            CheckCfg<bool>("Block Under|On Bridge", ref UnBridge);
            CheckCfg<bool>("Block Under|On Radar", ref UnRadio);
            CheckCfg<int>("Max Height Limit", ref MaxHeight);
            CheckCfg<bool>("Block Structures above the max height", ref BlockStructuresHeight);
            CheckCfg<bool>("Block Deployables above the max height", ref BlockDeployablesHeight);
            CheckCfg<int>("Max Under Water Height Limit", ref MaxWater);
            CheckCfg<bool>("Block Structures under water", ref BlockStructuresWater);
            CheckCfg<bool>("Block Deployables under water", ref BlockDeployablesWater);
            CheckCfg<string>("Block Water Message", ref MsgWater);
            CheckCfg<string>("Block Height Message", ref MsgHeight);
            CheckCfg<string>("Block Message", ref Msg); 
            CheckCfg<int>("Ignore Auth Lvl", ref AuthLVL);
            SaveConfig(); 
        }
        #endregion

        #region Logic

        void CheckBlock(BaseNetworkable StartBlock, BasePlayer sender, bool CheckHeight, bool CheckWater)
        {
            if (StartBlock && sender.net.connection.authLevel < AuthLVL && !StartBlock.isDestroyed)
            {
                Vector3 Pos = StartBlock.transform.position;
                if (StartBlock.name == "foundation.steps(Clone)")
                    Pos.y += 1.3f;

                if (CheckHeight || CheckWater)
                {
                    float height = TerrainMeta.HeightMap.GetHeight(Pos);
                    if (CheckHeight && Pos.y - height > MaxHeight)
                    {
                        sender.ChatMessage(MsgHeight);
                        StartBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                        return;
                    }
                    else if (CheckWater && height < 0 && height < MaxWater && Pos.y < 2.8f )
                    {
                        sender.ChatMessage(MsgWater);
                        StartBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                        return;
                    }
                }

                Pos.y += 200;
                RaycastHit[] hits = Physics.RaycastAll(Pos, Vector3.down, 202.8f);
                Pos.y -= 200;

                bool isMining = StartBlock is MiningQuarry;
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.collider)
                    {
                        string ColName = hit.collider.name;
                        if (UnTerrain && !isMining && ColName == "Terrain" && hit.point.y > Pos.y ||
                            InBase && ColName.StartsWith("base", StringComparison.CurrentCultureIgnoreCase) ||
                            InMetalBuilding && ColName == "Metal_building_COL" ||
                            UnBridge && ColName == "Bridge_top" ||
                            UnRadio && ColName.StartsWith("dish") ||
                            InWarehouse && ColName.StartsWith("Warehouse") ||
                            InHangar && ColName.StartsWith("Hangar") ||
                            OnRiver && ColName == "rivers" ||
                            InTunnel && ColName.Contains("unnel") ||
                            OnRoad && ColName.EndsWith("road", StringComparison.CurrentCultureIgnoreCase) || 
                            InStormDrain && ColName.StartsWith("Storm_drain", StringComparison.CurrentCultureIgnoreCase) ||
                            InTank && ColName == "howie_spheretank_blockin" ||
                            (ColName.StartsWith("rock", StringComparison.CurrentCultureIgnoreCase) ||
                            ColName.StartsWith("cliff", StringComparison.CurrentCultureIgnoreCase)) && 
                            (hit.point.y < Pos.y ? OnRock : hit.collider.bounds.Contains(Pos) ? InRock : InCave))
                        {
                            sender.ChatMessage(Msg);
                            StartBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        #region Hooks

        void OnEntityBuilt(Planner plan, GameObject obj) => CheckBlock(obj.GetComponent<BaseNetworkable>(), plan.ownerPlayer, BlockStructuresHeight, BlockStructuresWater);

        void OnItemDeployed(Deployer deployer, BaseEntity deployedentity)
        {
            if (!(deployedentity is BaseLock))
                CheckBlock((BaseNetworkable) deployedentity, deployer.ownerPlayer, BlockDeployablesHeight, BlockDeployablesWater);
        }

        #endregion
    }
}