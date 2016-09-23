using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Network;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("FlippableTurrets", "DylanSMR", "1.0.4", ResourceId = 2055)]
    class FlippableTurrets : RustPlugin
    {
        #region Fields
            static int constructionColl = UnityEngine.LayerMask.GetMask(new string[] { "Construction", "Deployable", "Prevent Building", "Deployed" });
            static FieldInfo serverinput;
            public InputState inputState;

            public string pf = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab";
            public Dictionary<AutoTurret, int> turretinv = new Dictionary<AutoTurret, int>();
            public Dictionary<AutoTurret, AutoPlayer> turretplayer = new Dictionary<AutoTurret, AutoPlayer>();

            public class AutoPlayer
            {
                public List<ProtoBuf.PlayerNameID> players = new List<ProtoBuf.PlayerNameID>();
                public AutoPlayer(){}
            }
        #endregion

        #region Oxide Hooks
            void Loaded()
            {
                permission.RegisterPermission("flippableturrets.canflip", this);
                lang.RegisterMessages(messages, this);
                serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            }
        #endregion

        #region Langauge
            Dictionary<string, string> messages = new Dictionary<string, string>()
            {
                {"Flipped", "You have flipped that turret successfully."},
                {"NotFlipped", "A error occured upon flipping the turret. This might be because its already flipped!"},
                {"Unflipped", "You have unflipped that turret successfully"},
                {"NotUnFlipped", "A error occured upon unflipping the turret. This might be because its already unflipped!"},
                {"NoPermission", "You do not have permission to preform that command."},
                {"NoTurret", "There is no turret in front of you to flip/unflip :("}
            };
        #endregion

        #region ChatCommands
            [ChatCommand("flipturret")]
            void cmdFlipTurret(BasePlayer player)
            {
                if(!permission.UserHasPermission(player.UserIDString, "flippableturrets.canflip")){
                    SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                    return;
                }
                inputState = serverinput.GetValue(player) as InputState;
                Ray ray = new Ray(player.eyes.position, Quaternion.Euler(inputState.current.aimAngles) * Vector3.forward);
                BaseEntity flipObject = FindTurret(ray, 5f);
                if(flipObject.ShortPrefabName.Contains("turret")){
                    if(flipTurreT(flipObject, player)) SendReply(player, lang.GetMessage("Flipped", this, player.UserIDString));
                    else SendReply(player, lang.GetMessage("NotFlipped", this, player.UserIDString));
                    return;
                }
                else{
                    SendReply(player, lang.GetMessage("NoTurret", this, player.UserIDString));
                    return;
                }
            }

            [ChatCommand("unflipturret")]
            void cmdUnFlipTurret(BasePlayer player)
            {
                if(!permission.UserHasPermission(player.UserIDString, "flippableturrets.canflip")){
                    SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                    return;
                }
                inputState = serverinput.GetValue(player) as InputState;
                Ray ray = new Ray(player.eyes.position, Quaternion.Euler(inputState.current.aimAngles) * Vector3.forward);
                BaseEntity flipObject = FindTurret(ray, 5f);
                if(flipObject.ShortPrefabName.Contains("turret")){
                    if(unflipTurreT(flipObject, player)) SendReply(player, lang.GetMessage("Unflipped", this, player.UserIDString));
                    else SendReply(player, lang.GetMessage("NotUnFlipped", this, player.UserIDString));
                    return;
                }
                else{
                    SendReply(player, lang.GetMessage("NoTurret", this, player.UserIDString));
                    return;
                }
            } 

        #endregion

        #region BaseHooks
            private Item BuildItems(string shortname, int amount)
            {
                var definition = ItemManager.FindItemDefinition(shortname); // Find the item definition from its shortname
                if (definition != null && amount != 0)
                {
                    Item item = ItemManager.CreateByItemID(definition.itemid, amount); // Create the item itself
                    if (item != null)
                        return item;
                }         
                return null;
            }  

            static BaseEntity FindTurret(Ray ray, float distance)
            {
                RaycastHit hit;
                if (!UnityEngine.Physics.Raycast(ray, out hit, distance, constructionColl))
                    return null;
                return hit.GetEntity();
            }

            private bool flipTurreT(BaseEntity turret, BasePlayer player)
            {
                try{
                    if(turret.transform.rotation.w == -0.00f) return false;

                    AutoTurret autoTurret = GameManager.server.CreateEntity(pf, new Vector3(turret.transform.position.x, turret.transform.position.y + 3f, turret.transform.position.z), new Quaternion(-2.4f, 0.0f, 0.0f, -0.00f), true) as AutoTurret;
                    autoTurret.Spawn();
                    autoTurret.health = turret.Health();

                    turretinv.Add(autoTurret, 0);
                    foreach(var item in turret.GetComponent<AutoTurret>().inventory.itemList.ToList()) if(item.info.displayName.english == ("5.56 Rifle Ammo")) turretinv[autoTurret] = turretinv[autoTurret] + item.amount;
                    turretplayer.Add(autoTurret, new AutoPlayer{});
                    foreach(var target in turret.GetComponent<AutoTurret>().authorizedPlayers) turretplayer[autoTurret].players.Add(target);

                    Item itemn = BuildItems("ammo.rifle", turretinv[autoTurret]);
                    if (itemn != null) itemn.MoveToContainer(autoTurret.inventory);
                    turretinv.Remove(autoTurret);

                    foreach(var entry in turretplayer[autoTurret].players) autoTurret.authorizedPlayers.Add(entry);
                    turretplayer.Remove(autoTurret);

                    autoTurret.SendNetworkUpdateImmediate();
                    turret.KillMessage();
                    return true;
                }  
                catch(System.Exception)
                {
                    return false;
                }
            }

            private bool unflipTurreT(BaseEntity turret, BasePlayer player)
            {
                try{
                    if(turret.transform.rotation.w != -0.00f) return false;
                    
                    AutoTurret autoTurret = GameManager.server.CreateEntity(pf, new Vector3(turret.transform.position.x, turret.transform.position.y - 3f, turret.transform.position.z), new Quaternion(0.0f, 0.1f, 0.0f, 1.0f), true) as AutoTurret;
                    autoTurret.Spawn();
                    autoTurret.health = turret.Health();   

                    turretinv.Add(autoTurret, 0);
                    foreach(var item in turret.GetComponent<AutoTurret>().inventory.itemList.ToList()) if(item.info.displayName.english == ("5.56 Rifle Ammo")) turretinv[autoTurret] = turretinv[autoTurret] + item.amount;
                    turretplayer.Add(autoTurret, new AutoPlayer{});
                    foreach(var target in turret.GetComponent<AutoTurret>().authorizedPlayers) turretplayer[autoTurret].players.Add(target);

                    Item itemn = BuildItems("ammo.rifle", turretinv[autoTurret]);
                    if (itemn != null) itemn.MoveToContainer(autoTurret.inventory);
                    turretinv.Remove(autoTurret);

                    foreach(var entry in turretplayer[autoTurret].players) autoTurret.authorizedPlayers.Add(entry);
                    turretplayer.Remove(autoTurret);

                    autoTurret.SendNetworkUpdateImmediate();
                    turret.KillMessage();
                    return true;
                }  
                catch(System.Exception)
                {
                    return false;
                }
            } 
        #endregion
    }
}