using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AntiFoundationStack", "Jake_Rich", 0.1)]
    [Description("Prevents foundation stacking")]

    public class AntiFoundationStack : RustPlugin
    {

        #region Localization

        void Init()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BlockMessage"] = "<color=Orange>Anti-Foundation Stacking:</color> Overlap Detected.",
            }, this);
            
        }

        #endregion

        #region AntiStack

        #region Config Values
        Dictionary<string, double> foundationWidth = new Dictionary<string, double>
        {
            {"foundation", 1.5d},
            //{"foundation.triangle", 1d},
        };

        #endregion

        int copyLayer = LayerMask.GetMask("Construction");

        void OnEntityBuilt(Planner plan, GameObject go)
        {

            BaseEntity entity = go.ToBaseEntity();

            if (entity == null)
            {
                return;
            }

            if (!foundationWidth.ContainsKey(entity.ShortPrefabName))
            {
                return;
            }

            double localEntityWidth = foundationWidth[entity.ShortPrefabName];

            List<BaseEntity> list = new List<BaseEntity>();

            Vis.Entities(entity.transform.position, 3f, list, copyLayer, QueryTriggerInteraction.Ignore);
            foreach (BaseEntity targetEntity in list)
            {
                if (targetEntity == entity || !foundationWidth.ContainsKey(targetEntity.ShortPrefabName))
                {
                    continue;
                }

                float localDistance = Vector3.Distance(targetEntity.CenterPoint(),entity.CenterPoint());
                double targetEntityWidth = foundationWidth[targetEntity.ShortPrefabName];

                if (localDistance + 0.05d < localEntityWidth + targetEntityWidth) //Accounts for floating point errors
                { 
                    entity.Kill();
                    if (plan.GetOwnerPlayer() != null)
                    {
                        PrintToChat(plan.GetOwnerPlayer(), lang.GetMessage("BlockMessage",this));
                    }
                    return;
                }
            }
        }

        #endregion

    }
}


