// R-eference: Facepunch.ID
// R-eference: Google.ProtocolBuffers
// Reference: Facepunch.HitBox

using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using static Oxide.Core.Configuration.DynamicConfigFile;
namespace Oxide.Plugins
{
    [Info("Death Messages", "Hatemail", "0.1.1")]
    public class DeathMessages : RustLegacyPlugin
    {

        [PluginReference]
        private Plugin Death;
        [PluginReference]
        private Plugin PlayerDatabase;

        private static System.Random random = new System.Random();
        private string chatName = "Death";
        private Dictionary<string, Dictionary<string, List<string>>> messages;
        private List<string> disabledMessages;
        private Dictionary<string, string> bodyParts;
        Dictionary<string, Dictionary<string, List<string>>> defaultDeathMessages = new Dictionary<string, Dictionary<string, List<string>>>
        {
            { "Suicide", new Dictionary <string, List <string>>
                {
                    { "Default", new List<string> { "{killed} has left this cruel world." } },
                    { "Radiation", new List <string> { "{killed} has succumbed to radiation poisoning." } },
                    { "Falling", new List<string> { "{killed} has fallen to a brutal death."} },
                    { "Bleeding", new List<string> { "{killed} has bled out."} },
                    { "Water", new List<string> { "{killed} thought they could swim."} }
                }
            },
            { "PvP", new Dictionary <string, List <string>>
                {
                    { "Default", new List<string> { "{killer} killed {killed} using {weapon} with a hit to their {bodypart}" } },
                    { "Weapon.Explosive Charge", new List<string> { "{killer} blew the ever living shit out of {killed} with an explosive charge!!" } },
                    { "Weapon.F1 Grenade", new List<string> { "{killer} killed {killed} using {weapon}" } },
                    { "Weapon.M4", new List<string> { "{killer} killed {killed} using an {weapon}" } },
                }
            },
            { "EvP", new Dictionary <string, List <string>>
                {
                    { "Default", new List<string> { "{killer} killed {killed}." } },
                    { "Mutant Wolf", new List<string> { "{killer} killed {killed}." } },
                    { "Wolf", new List<string> { "{killer} killed {killed}." } },
                    { "Mutant Bear", new List<string> { "{killer} killed {killed}." } },
                    { "Bear", new List<string> { "{killer} killed {killed}." } }
                }
            },
            { "PvE", new Dictionary <string, List <string>>
                {
                    {"Default", new List<string> { "{killer} killed {killed} using {weapon} at {distance}m." } },
                    {"Mutant Bear", new List<string> { "{killer} just took down a mutant bear using {weapon} at {distance}m" } }
                }
            }
        };

        void Loaded()
        {
            try
            {
                messages = Config.Get<Dictionary<string, Dictionary<string, List<string>>>> ("Messages");
            }
            catch (InvalidCastException ex)
            {
                var temp = Config.Get<Dictionary<string, Dictionary<string, object>>>("Messages");
                messages = new Dictionary<string, Dictionary<string, List<string>>>();
                foreach (var msgD in temp)
                {
                    Dictionary<string, List<string>> tempD = new Dictionary<string, List<string>>();
                    foreach (var val in msgD.Value)
                    {
                        tempD.Add(val.Key, ((List<object>)val.Value).Select(x => x.ToString()).ToList<string>());
                    }
                    messages.Add(msgD.Key, tempD);
                }
            }
            bodyParts = Config.Get<Dictionary<string, string>>("BodyParts");
            chatName = Config.Get<string>("Settings", "ChatName");
            disabledMessages = Config.Get<List<string>>("Settings", "DisabledMessages");

        }

        protected override void LoadDefaultConfig()
        {
            string[] niceNames = (string[])typeof(BodyParts).GetField("niceNames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);

            Config["Messages"] = defaultDeathMessages;
            Config["Settings"] = new Dictionary<string, object> { { "ChatName", "Death" } };
            Config["Settings", "DisabledMessages"] = new List<string> { "Suicide.Default" };
            var bodyParts = new Dictionary<string, string>();
            foreach (var part in niceNames)
            {
                if (!bodyParts.ContainsKey(part))
                {
                    bodyParts.Add(part, part);
                }
            }
            Config["BodyParts"] = bodyParts;
        }
        [HookMethod("BuildServerTags")]
        private void BuildServerTags(IList<string> taglist)
        {
            taglist.Add("death_messages");
        }
        private void OnPlayerSuicide(TakeDamage takedamage, DamageEvent damage, object tags)
        {
            var message = getDeathMessage("Suicide", tags.GetProperty("weapon").ToString(), tags);
            if (message != null)
            {
                rust.BroadcastChat(chatName, message);
            }
            //Puts((string)Death?.CallHook("GetDeathString", "Killer: {killer} {killerId} Killed: {killed} {killedId} Weapon: {weapon} BodyPart: {bodypart} Distance: {distance} DeathType: {deathType} Location: {location}", tags) ?? "Unknown");
        }
        private void OnPlayerDeath(TakeDamage takedamage, DamageEvent damage, object tags)
        {
            string weapon = tags.GetProperty("weapon").ToString();
            string deathtype = tags.GetProperty("deathType").ToString();
            string killer = tags.GetProperty("killer").ToString();
            string bodyPart = tags.GetProperty("bodypart").ToString();
            string message = null;
            if (bodyParts.ContainsKey(bodyPart))
            {
                tags.SetProperty("bodypart", bodyParts[bodyPart]);
            }
            switch (deathtype)
            {
                case "human":
                    {
                        if (weapon == "Spike Wall" && killer == "Unknown")
                        {
                            tags.SetProperty("killerId", PlayerDatabase?.Call("GetPlayerData", tags.GetProperty("killerId"), "name") ?? tags.GetProperty("killerId"));
                        }
                        message = getDeathMessage("PvP", weapon, tags);
                        break;
                    }
                default:
                    {
                        message = getDeathMessage("EvP", killer, tags);
                        break;
                    }
            }
            if (message != null)
            {
                rust.BroadcastChat(chatName, message);
            }
            //Puts((string)Death?.CallHook("GetDeathString", "Killer: {killer} {killerId} Killed: {killed} {killedId} Weapon: {weapon} BodyPart: {bodypart} Distance: {distance} DeathType: {deathType} Location: {location}", tags) ?? "Unknown");
        }
        private void OnStructureDestroyed(TakeDamage takedamage, DamageEvent damage, object tags)
        {
            //Puts((string)Death?.CallHook("GetDeathString", "Killer: {killer} {killerId} Killed: {killed} {killedId} Weapon: {weapon} BodyPart: {bodypart} Distance: {distance} DeathType: {deathType} Location: {location}", tags) ?? "Unknown");
        }
        private void OnDeployableDestroyed(TakeDamage takedamage, DamageEvent damage, object tags)
        {
            //Puts((string)Death?.CallHook("GetDeathString", "Killer: {killer} {killerId} Killed: {killed} {killedId} Weapon: {weapon} BodyPart: {bodypart} Distance: {distance} DeathType: {deathType} Location: {location}", tags) ?? "Unknown");
        }
        private void OnAnimalDeath(TakeDamage takedamage, DamageEvent damage, object tags)
        {
            string killed = tags.GetProperty("killed").ToString();
            var message = getDeathMessage("PvE", killed, tags);
            if (message != null)
            {
                rust.BroadcastChat(chatName, message);
            }
            //Puts((string)Death?.CallHook("GetDeathString", "Killer: {killer} {killerId} Killed: {killed} {killedId} Weapon: {weapon} BodyPart: {bodypart} Distance: {distance} DeathType: {deathType} Location: {location}", tags) ?? "Unknown");
        }

        private string getDeathMessage(string deathType, string listKey, object tags)
        {
            List<string> messageList;
            string message = null;
            if (messages.ContainsKey(deathType))
            {
                if (messages[deathType].ContainsKey(listKey))
                {
                    if (disabledMessages.Contains($"{deathType}.{listKey}"))
                        return null;
                    messageList = messages[deathType][listKey];
                }
                else
                {
                    if (disabledMessages.Contains($"{deathType}.Default"))
                        return null;
                    messageList = messages[deathType]["Default"];
                }
                message = messageList[random.Next(messageList.Count)].ToString();
            }
            if (message == null) return message;
            return Death.CallHook("GetDeathString", message, tags).ToString(); ;
        }
    }
}
