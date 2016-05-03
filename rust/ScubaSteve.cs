using Oxide.Core;
using System.Collections.Generic;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System;

namespace Oxide.Plugins
{
    [Info("Scuba Steve", "DaBludger", 1.6, ResourceId = 0)]
    [Description("Be the SEAL, this will protect you from drowning and cold damage while swimming.")]
    public class ScubaSteve : RustPlugin
    {
        private bool Changed;
        private bool damageArmour = false;
        private bool configloaded = false;
        private float armourDamageAmount = 0.0f;
        private float head = 0.3f;
        private float chest = 0.2f;
        private float pants = 0.2f;
        private float gloves = 0.05f;
        private float boots = 0.05f;

        private float chead = 0.3f;
        private float cchest = 0.2f;
        private float cpants = 0.2f;
        private float cgloves = 0.05f;
        private float cboots = 0.05f;

        void OnPluginLoaded(Plugin name)
        {
            if ("ScubaSteve".Equals(name.Name) && !configloaded)
            {
                LoadVariables();
            }
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
			configloaded = true;
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (hitinfo.hasDamage)
            {
                float dd = 0.0f;
                bool armourDamaged = false;
                if (hitinfo.damageTypes?.Get(Rust.DamageType.Drowned) > 0.0f)
                {
                    dd = getDamageDeduction(entity.ToPlayer(), Rust.DamageType.Drowned);
                    float newdamage = getScaledDamage(hitinfo.damageTypes.Get(Rust.DamageType.Drowned), dd);
                    hitinfo.damageTypes.Set(Rust.DamageType.Drowned, newdamage);
                    armourDamaged = true;
                }
                if (hitinfo.damageTypes?.Get(Rust.DamageType.Cold) > 0.0f && entity.ToPlayer().IsSwimming())
                {
                    dd = getDamageDeduction(entity.ToPlayer(), Rust.DamageType.Cold);
                    float newdamage = getScaledDamage(hitinfo.damageTypes.Get(Rust.DamageType.Cold), dd);
                    hitinfo.damageTypes.Set(Rust.DamageType.Cold, newdamage);
                    armourDamaged = true;
                }
                if (armourDamaged && damageArmour)
                {
                    foreach (Item i in entity.ToPlayer().inventory.containerWear.itemList)
                    {
                        if (i.info.name.ToLower().Contains("hazmat"))
                        {
                            i.condition = i.condition - armourDamageAmount;
                        }
                    }
                }
            }
        }

        private float getScaledDamage(float current, float deduction)
        {
            float newd = current - (current * deduction);
            if (newd < 0.0f)
            {
                newd = 0.0f;
            }
            return newd;
        }


        private float getDamageDeduction(BasePlayer player, Rust.DamageType damageType)
        {
            float dd = 0.0f;
            foreach (Item i in player.inventory.containerWear.itemList)
            {
                if (!i.isBroken)
                {
                    if (i.info.name.ToLower().Contains("hazmat_helmet.item"))
                    {
                        //PrintToChat(player, "damageArmour "+damageArmour, new object[0]);
                        if (damageType == Rust.DamageType.Drowned)
                        {
                            dd += head;
                        }
                        if (damageType == Rust.DamageType.Cold)
                        {
                            dd += chead;
                        }
                    }
                    if (i.info.name.ToLower().Contains("hazmat_jacket.item"))
                    {
                        if (damageType == Rust.DamageType.Drowned)
                        {
                            dd += chest;
                        }
                        if (damageType == Rust.DamageType.Cold)
                        {
                            dd += cchest;
                        }
                    }
                    if (i.info.name.ToLower().Contains("hazmat_pants.item"))
                    {
                        if (damageType == Rust.DamageType.Drowned)
                        {
                            dd += pants;
                        }
                        if (damageType == Rust.DamageType.Cold)
                        {
                            dd += cpants;
                        }
                    }
                    if (i.info.name.ToLower().Contains("hazmat_gloves.item"))
                    {
                        if (damageType == Rust.DamageType.Drowned)
                        {
                            dd += gloves;
                        }
                        if (damageType == Rust.DamageType.Cold)
                        {
                            dd += cgloves;
                        }
                    }
                    if (i.info.name.ToLower().Contains("hazmat_boots.item"))
                    {
                        if (damageType == Rust.DamageType.Drowned)
                        {
                            dd += boots;
                        }
                        if (damageType == Rust.DamageType.Cold)
                        {
                            dd += cboots;
                        }
                    }
                }
            }
            return dd;
        }

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        private void LoadVariables()
        {
            Puts("Loading Config File:");
            chead = Convert.ToSingle(GetConfig("Cold", "head", "0.3"));
            Puts("Cold damage midigation head: " + chead);
            cchest = Convert.ToSingle(GetConfig("Cold", "chest", "0.2"));
            Puts("Cold damage midigation chest: " + cchest);
            cpants = Convert.ToSingle(GetConfig("Cold", "pants", "0.2"));
            Puts("Cold damage midigation pants: " + cpants);
            cgloves = Convert.ToSingle(GetConfig("Cold", "gloves", "0.05"));
            Puts("Cold damage midigation gloves: " + cgloves);
            cboots = Convert.ToSingle(GetConfig("Cold", "boots", "0.05"));
            Puts("Cold damage midigation boots: " + cboots);

            head = Convert.ToSingle(GetConfig("Drown", "head", "0.3"));
            Puts("Drown damage midigation head: " + head);
            chest = Convert.ToSingle(GetConfig("Drown", "chest", "0.2"));
            Puts("Drown damage midigation chest: " + chest);
            pants = Convert.ToSingle(GetConfig("Drown", "pants", "0.2"));
            Puts("Drown damage midigation pants: " + pants);
            gloves = Convert.ToSingle(GetConfig("Drown", "gloves", "0.05"));
            Puts("Drown damage midigation gloves: " + gloves);
            boots = Convert.ToSingle(GetConfig("Drown", "boots", "0.05"));
            Puts("Drown damage midigation boots: " + boots);

            //The only peace of armour that has condition is the helmet so this is removed until the other have it added
            //damageArmour = Convert.ToBoolean(GetConfig("Attire", "TakesDamage", "false"));
            //Puts("Amour takes damage: "+ damageArmour);
            //armourDamageAmount = Convert.ToSingle(GetConfig("Attire", "DamageAmount", "0.0"));
            //Puts("How much damage does the armour take: "+ armourDamageAmount);

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }
    }
}
