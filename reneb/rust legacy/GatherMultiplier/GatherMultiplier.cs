// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;

namespace Oxide.Plugins
{
    [Info("GatherMultiplier", "Reneb", "1.0.5", ResourceId = 974)]
    class GatherMultiplier : RustLegacyPlugin
    {
        int cachedGather;
        int cachedAmount;
        int cachedMinAmount;

        static int GathermetalOreMultiplier = 3;
        static int GathersulfurOreMultiplier = 3;
        static int GatherstonesMultiplier = 3;
        static int GatherwoodpileMultiplier = 10;
        static int GatheranimalMultiplier = 2;

        static Dictionary<string, object> resourceMultiplier = GetResourceList();


        FieldInfo startingtotal;


        static Dictionary<string,object> GetResourceList()
        {
            var resourcelist = new Dictionary<string, object>();
            resourcelist.Add("Blood", 1);
            resourcelist.Add("Animal Fat", 1);
            resourcelist.Add("Sulfur Ore", 1);
            resourcelist.Add("Metal Ore", 1);
            resourcelist.Add("Stones", 1);
            resourcelist.Add("Wood", 1); 
            resourcelist.Add("Cloth", 1);
            resourcelist.Add("Raw Chicken Breast", 1);
            resourcelist.Add("Leather", 1);
            return resourcelist;
        }

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key]; 
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<int>("Gather Multiplier: Metal Rock", ref GathermetalOreMultiplier);
            CheckCfg<int>("Gather Multiplier: Sulfur Rock", ref GathersulfurOreMultiplier);
            CheckCfg<int>("Gather Multiplier: Stone Rock", ref GatherstonesMultiplier);
            CheckCfg<int>("Gather Multiplier: WoodPile", ref GatherwoodpileMultiplier);
            CheckCfg<int>("Gather Multiplier: Animals", ref GatheranimalMultiplier);
            CheckCfg<Dictionary<string, object>>("Resource Multiplier", ref resourceMultiplier);
            SaveConfig();
        }

        void Unload()
        {
            /*foreach (ResourceTarget resource in UnityEngine.Resources.FindObjectsOfTypeAll<ResourceTarget>())
            {
                switch (resource.type)
                {
                    case ResourceTarget.ResourceTargetType.Animal:
                        cachedGather = 2;
                        break;
                    case ResourceTarget.ResourceTargetType.Rock2:
                        cachedGather = 3;
                        break;
                    case ResourceTarget.ResourceTargetType.Rock3:
                        cachedGather = 3;
                        break;

                    case ResourceTarget.ResourceTargetType.Rock1:
                        cachedGather = 3;
                        break;
                    case ResourceTarget.ResourceTargetType.WoodPile:
                        cachedGather = 10;
                        break;
                    default:
                        break;
                }
                foreach (ResourceGivePair resourceavaible in resource.resourcesAvailable)
                {
                    if (resourceMultiplier.ContainsKey(resourceavaible.ResourceItemName))
                    {
                        resourceavaible.amountMin /= (int)resourceMultiplier[resourceavaible.ResourceItemName];
                        resourceavaible.amountMax /= (int)resourceMultiplier[resourceavaible.ResourceItemName];
                        resourceavaible.CalcAmount();
                    }  
                }
                resource.gatherEfficiencyMultiplier = cachedGather;
                startingtotal.SetValue(resource, resource.GetTotalResLeft());
            }*/
        }
        void Loaded()
        {
            startingtotal = typeof(ResourceTarget).GetField("startingTotal", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public );
        }
        void OnResourceNodeLoaded(ResourceTarget resource)
        {
            cachedGather = 1;
            cachedAmount = 1;
            cachedMinAmount = 1;
            switch (resource.type)
            {
                case ResourceTarget.ResourceTargetType.Animal:
                    cachedGather = GatheranimalMultiplier;
                    break;
                case ResourceTarget.ResourceTargetType.Rock2:
                    cachedGather = GathermetalOreMultiplier;
                    break;
                case ResourceTarget.ResourceTargetType.Rock3:
                    cachedGather = GatherstonesMultiplier;
                    break;

                case ResourceTarget.ResourceTargetType.Rock1:
                    cachedGather = GathersulfurOreMultiplier;
                    break;
                case ResourceTarget.ResourceTargetType.WoodPile:
                    cachedGather = GatherwoodpileMultiplier;
                    break;
                default:
                    break;
            }
            foreach (ResourceGivePair resourceavaible in resource.resourcesAvailable)
            {
                if (resourceMultiplier.ContainsKey(resourceavaible.ResourceItemName))
                {
                    resourceavaible.amountMin *= (int)resourceMultiplier[resourceavaible.ResourceItemName];
                    resourceavaible.amountMax *= (int)resourceMultiplier[resourceavaible.ResourceItemName];
                    resourceavaible.CalcAmount();
                }
            }
            resource.gatherEfficiencyMultiplier = cachedGather;
            startingtotal.SetValue(resource, resource.GetTotalResLeft());
        }
       void InitializeGatherMult()
        { /*
            foreach (ResourceTarget resource in UnityEngine.Resources.FindObjectsOfTypeAll<ResourceTarget>())
            {
                cachedGather = 1;
                cachedAmount = 1;
                cachedMinAmount = 1;
                switch (resource.type)
                {
                    case ResourceTarget.ResourceTargetType.Animal:
                        cachedGather = GatheranimalMultiplier;
                        break;
                    case ResourceTarget.ResourceTargetType.Rock2:
                        cachedGather = GathermetalOreMultiplier;
                        break;
                    case ResourceTarget.ResourceTargetType.Rock3:
                        cachedGather = GatherstonesMultiplier;
                        break;

                    case ResourceTarget.ResourceTargetType.Rock1:
                        cachedGather = GathersulfurOreMultiplier;
                        break;
                    case ResourceTarget.ResourceTargetType.WoodPile:
                        cachedGather = GatherwoodpileMultiplier;
                        break;
                    default:
                        break;
                }
                foreach (ResourceGivePair resourceavaible in resource.resourcesAvailable)
                {
                    if (resourceMultiplier.ContainsKey(resourceavaible.ResourceItemName))
                    {
                        resourceavaible.amountMin *= (int)resourceMultiplier[resourceavaible.ResourceItemName];
                        resourceavaible.amountMax *= (int)resourceMultiplier[resourceavaible.ResourceItemName];
                        resourceavaible.CalcAmount();
                    }
                }
                resource.gatherEfficiencyMultiplier = cachedGather;
                startingtotal.SetValue(resource, resource.GetTotalResLeft());
            }*/
        }
            void OnServerInitialized()
        {
            timer.Once(0.01f, () => InitializeGatherMult());
        }
    }
}
