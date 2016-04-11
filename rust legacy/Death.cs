// Reference: Facepunch.ID
// Reference: Facepunch.HitBox

using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Death API", "Hatemail", "0.1.0")]
    public class Death : RustLegacyPlugin
    {
        private const string UNKNOWN = "Unknown";

        [HookMethod("BuildServerTags")]
        private void BuildServerTags(IList<string> taglist)
        {
            taglist.Add("death_api");
        }

        private void OnKilled(TakeDamage takedamage, DamageEvent damage)
        {
            WeaponImpact impact = damage.extraData as WeaponImpact;
            DeathTags tags = new DeathTags();
            tags.killer = damage.attacker.client?.netUser.displayName ?? UNKNOWN;
            tags.killerId = damage.attacker.client?.netUser.userID.ToString() ?? UNKNOWN;
            tags.weapon = impact?.dataBlock.name ?? UNKNOWN;
            tags.distance = Math.Floor(Vector3.Distance(damage.attacker.id.transform.position, damage.victim.id.transform.position));
            tags.location = damage.victim.id.transform.position;

            if (takedamage is HumanBodyTakeDamage)
            {
                SetPlayerDeathTags((HumanBodyTakeDamage)takedamage, damage, ref tags);
                CheckForHuntingBow(takedamage, damage, ref tags);
                switch (tags.deathType)
                {
                    case DeathTypes.entity:
                    case DeathTypes.human:
                        {
                            Core.Interface.CallHook("OnPlayerDeath", takedamage, damage, tags);
                            break;
                        }
                    case DeathTypes.suicide:
                        {
                            Core.Interface.CallHook("OnPlayerSuicide", takedamage, damage, tags);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            else if (takedamage is ProtectionTakeDamage && damage.sender.gameObject.GetComponentInChildren<BasicWildLifeAI>() && damage.attacker.client != null)
            {
                SetAnimalDeathTags(takedamage, damage, ref tags);
                CheckForHuntingBow(takedamage, damage, ref tags);
                Core.Interface.CallHook("OnAnimalDeath", takedamage, damage, tags);
            }
            else
            {
                SetBuildingDeathTags(takedamage, damage, ref tags);
                if (takedamage.GetComponent<DeployableObject>())
                {
                    Core.Interface.CallHook("OnDeployableDestroyed", takedamage, damage, tags);
                }
                else
                { 
                    Core.Interface.CallHook("OnStructureDestroyed", takedamage, damage, tags);
                }
            }
        }
        private void CheckForHuntingBow(TakeDamage takedamage, DamageEvent damage, ref DeathTags tags)
        {
            if (damage.attacker.client != null && ((tags.weapon.Equals(UNKNOWN) || tags.weapon.Equals("Bleeding"))))
            {
                PlayerInventory inv = damage.attacker.client?.netUser.playerClient.controllable.GetComponent<PlayerInventory>();
                if (inv != null && (inv.activeItem?.datablock?.name?.Contains("Bow") ?? false))
                {
                    tags.weapon = inv.activeItem.datablock.name;
                }
            }
        }
        private void SetBuildingDeathTags(TakeDamage takedamage, DamageEvent damage, ref DeathTags tags)
        {
            var deployable = takedamage.GetComponent<DeployableObject>();
            var structure = takedamage.GetComponent<StructureComponent>();
            if (deployable)
            {
                tags.killed = Regex.Replace(deployable.name.Replace("(Clone)", ""), "([a-z])([A-Z])", "$1 $2");
                tags.killedId = deployable.creatorID.ToString();
            }
            else if (structure)
            {
                tags.killed = Regex.Replace(structure.name.Replace("(Clone)", ""), "([a-z])([A-Z])", "$1 $2");
                tags.killedId = structure._master?.creatorID.ToString();
            }
            if (damage.attacker.id.GetComponent<TimedExplosive>())
            {
                tags.weapon = "Explosive Charge";
            }
            else if (damage.attacker.id.GetComponent<TimedGrenade>())
            {
                tags.weapon = "F1 Grenade";
            }
            else if (damage.attacker.id.GetComponent<EnvDecay>())
            {
                tags.weapon = "Decay";
            }


        }

        private void SetAnimalDeathTags(TakeDamage takedamage, DamageEvent damage, ref DeathTags tags)
        {
            tags.deathType = tags.killer.Equals(UNKNOWN) ? DeathTypes.unknown : DeathTypes.human;
            var mutant = takedamage.ToString().Contains("Mutant");
            if (takedamage.GetComponent<BearAI>())
            {
                tags.killed = (mutant) ? "Mutant Bear" : "Bear";
                return;
            }
            if (takedamage.GetComponent<WolfAI>())
            {
                tags.killed = (mutant) ? "Mutant Wolf" : "Wolf";
                return;
            }
            if (takedamage.GetComponent<StagAI>())
            {
                tags.killed = "Stag";
                return;
            }
            if (takedamage.GetComponent<ChickenAI>())
            {
                tags.killed = "Chicken";
                return;
            }
            if (takedamage.GetComponent<RabbitAI>())
            {
                tags.killed = "Rabbit";
                return;
            }
            if (takedamage.GetComponent<BoarAI>())
            {
                tags.killed = "Boar";
                return;
            }
        }

        private void SetPlayerDeathTags(HumanBodyTakeDamage humanBodyTakeDamage, DamageEvent damage, ref DeathTags tags)
        {
            Metabolism metabolism = damage.attacker.id.GetComponent<Metabolism>();
            FallDamage fallDamage = damage.attacker.id.GetComponent<FallDamage>();
            tags.killed = damage.victim.client?.netUser.displayName ?? UNKNOWN;
            tags.killedId = damage.victim.client?.netUser.userID.ToString() ?? UNKNOWN;
            tags.bodypart = damage.bodyPart.GetNiceName();
            if (damage.attacker.id.GetComponentInChildren<BasicWildLifeAI>())
            {
                tags.deathType = DeathTypes.entity;
                var mutant = damage.attacker.idMain?.ToString().Contains("Mutant") ?? false;
                if (damage.attacker.id.GetComponent<WolfAI>())
                {
                    tags.killer = (mutant) ? "Mutant Wolf" : "Wolf";
                    return;
                }
                if (damage.attacker.id.GetComponent<BearAI>())
                {
                    tags.killer = (mutant) ? "Mutant Bear" : "Bear";
                    return;
                }
            }

            if (damage.attacker.id.GetComponent<DeployableObject>())
            {
                tags.deathType = DeathTypes.human;
                tags.killerId = damage.attacker.id.GetComponent<DeployableObject>().creatorID.ToString();
                if (damage.attacker.id.GetComponent<SpikeWall>())
                {
                    tags.weapon = "Spike Wall";
                }
                else if (damage.attacker.id.GetComponent<TimedExplosive>())
                {
                    tags.weapon = "Explosive Charge";
                }
                return;
            }

            if (damage.attacker.id.GetComponent<TimedGrenade>())
            {
                tags.deathType = DeathTypes.human;
                tags.weapon = "F1 Grenade";
                return;
            }

            if (damage.attacker.client == damage.victim.client)
            {
                tags.deathType = DeathTypes.suicide;
                tags.killerId = tags.killedId;
                float fallDmg = (float)fallDamage?.GetType().GetField("injuredTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(fallDamage);

                if (damage.damageTypes == 0 && WaterLine.Height != 0f && humanBodyTakeDamage.transform.position.y <= WaterLine.Height)
                {
                    tags.weapon = tags.weapon.Equals(UNKNOWN) ? "Water" : tags.weapon;
                }
                else if (damage.attacker.id.GetComponent<Radiation>() && metabolism.GetRadLevel() >= 500f)
                {
                    tags.weapon = tags.weapon.Equals(UNKNOWN) ? "Radiation" : tags.weapon;
                }
                else if (fallDamage != null && fallDamage.GetLegInjury() >= 1f)
                {
                    tags.weapon = tags.weapon.Equals(UNKNOWN) ? "Falling" : tags.weapon;
                }
                else if (humanBodyTakeDamage.IsBleeding())
                {
                    tags.weapon = tags.weapon.Equals(UNKNOWN) ? "Bleeding" : tags.weapon;
                }
                if (tags.weapon.Equals(UNKNOWN)) tags.weapon = "Suicide";
            }
            else if (damage.victim.client && damage.attacker.client)
            {
                tags.deathType = DeathTypes.human;
                if (humanBodyTakeDamage.IsBleeding())
                {
                    tags.weapon = tags.weapon.Equals(UNKNOWN) ? "Bleeding" : tags.weapon;
                }
            }
        }

        string GetDeathString(string format, DeathTags tags)
        {
            return NamedFormatting(tags, format, null);
        }

        public class DeathTags
        {
            public DeathTypes deathType { get; set; } = DeathTypes.unknown;
            public DamageTypeFlags damageType { get; set; } = 0;
            public string killer { get; set; } = UNKNOWN;
            public string killerId { get; set; } = UNKNOWN;
            public string killed { get; set; } = UNKNOWN;
            public string killedId { get; set; } = UNKNOWN;
            public string weapon { get; set; } = UNKNOWN;
            public string bodypart { get; set; } = UNKNOWN;
            public double distance { get; set; } = 0;
            public Vector3 location { get; set; } = Vector3.zero;
            override public string ToString() => $"Killer: {killer} {killerId} Killed: {killed} {killedId} Weapon: {weapon} BodyPart: {bodypart} Distance: {distance} DeathType: {deathType.ToString()} Location: {location}";

        }

        public enum DeathTypes
        {
            unknown = 0,
            suicide = 1,
            human = 2,
            entity = 3
        }
        //http://www.hanselman.com/blog/CommentView.aspx?guid=fde45b51-9d12-46fd-b877-da6172fe1791
        //Remove or keep hmmm, could just pass the named format and a dictionary of replacements and loops through and use String.Replace..
        public static string NamedFormatting(object anObject, string aFormat, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            Type type = anObject.GetType();
            Regex reg = new Regex(@"({)([^}]+)(})", RegexOptions.IgnoreCase);
            MatchCollection mc = reg.Matches(aFormat);
            int startIndex = 0;
            foreach (Match m in mc)
            {
                Group g = m.Groups[2]; //it's second in the match between { and }
                int length = g.Index - startIndex - 1;
                sb.Append(aFormat.Substring(startIndex, length));

                string toGet = String.Empty;
                string toFormat = String.Empty;
                int formatIndex = g.Value.IndexOf(":"); //formatting would be to the right of a :
                if (formatIndex == -1) //no formatting, no worries
                {
                    toGet = g.Value;
                }
                else //pickup the formatting
                {
                    toGet = g.Value.Substring(0, formatIndex);
                    toFormat = g.Value.Substring(formatIndex + 1);
                }

                //first try properties
                PropertyInfo retrievedProperty = type.GetProperty(toGet);
                Type retrievedType = null;
                object retrievedObject = null;
                if (retrievedProperty != null)
                {
                    retrievedType = retrievedProperty.PropertyType;
                    retrievedObject = retrievedProperty.GetValue(anObject, null);
                }
                else //try fields
                {
                    FieldInfo retrievedField = type.GetField(toGet);
                    if (retrievedField != null)
                    {
                        retrievedType = retrievedField.FieldType;
                        retrievedObject = retrievedField.GetValue(anObject);
                    }
                }

                if (retrievedType != null) //Cool, we found something
                {
                    string result = String.Empty;
                    if (toFormat == String.Empty) //no format info
                    {
                        result = retrievedType.InvokeMember("ToString",
                          BindingFlags.Public | BindingFlags.NonPublic |
                          BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase
                          , null, retrievedObject, null) as string;
                    }
                    else //format info
                    {
                        result = retrievedType.InvokeMember("ToString",
                          BindingFlags.Public | BindingFlags.NonPublic |
                          BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase
                          , null, retrievedObject, new object[] { toFormat, formatProvider }) as string;
                    }
                    sb.Append(result);
                }
                else //didn't find a property with that name, so be gracious and put it back
                {
                    sb.Append("{");
                    sb.Append(g.Value);
                    sb.Append("}");
                }
                startIndex = g.Index + g.Length + 1;
            }
            if (startIndex < aFormat.Length) //include the rest (end) of the string
            {
                sb.Append(aFormat.Substring(startIndex));
            }
            return sb.ToString();
        }
    }
    
}