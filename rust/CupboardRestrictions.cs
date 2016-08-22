using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CupboardRestrictions", "DylanSMR", "1.0.4", ResourceId = 2020)]
    [Description("Confirms cupboards are only placed on foundations or floors.")]
    public class CupboardRestrictions : RustPlugin
    {
        void Loaded(){
            lang.RegisterMessages(messages, this);
        }

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"MayNotPlace", "You may not place a tool cupboard on anything but a floor/foundation!"},
        };

        void OnEntitySpawned(BaseEntity entity, UnityEngine.GameObject gameObject){
            try {
                if(entity.ToString().Contains("cupboard.tool")) {
                    if(entity.OwnerID == null) return;
                    var player = BasePlayer.FindByID(entity.OwnerID);
                    if(player.IsSleeping() || !player.IsConnected()) return;
                    var onbuildingblock = false;
                        List<BaseEntity> nearby = new List<BaseEntity>();
                        Vis.Entities(entity.transform.position, 1, nearby);
                        foreach (var ent in nearby){
                            if(ent.ShortPrefabName.Contains("cupboard") && nearby.Count == 1){ onbuildingblock = false; break; }
                            if(ent.ToString().Contains("foundation")){
                                List<BaseEntity> nerb = new List<BaseEntity>();
                                Vis.Entities(new Vector3(ent.transform.position.x, ent.transform.position.y + 0.5f, ent.transform.position.z), 1, nerb);
                                foreach(var ent2 in nerb) if(ent2.ToString().Contains("cupboard")) return;
                            }
                        }
                        if(onbuildingblock == false) {
                            SendReply(player, lang.GetMessage("MayNotPlace", this));  
                            entity.KillMessage();
                            player.inventory.GiveItem(ItemManager.CreateByItemID(1257201758, 1));
                            return;
                        }else return;
                }else return;
            }catch(System.Exception) {return;}
        }
    }
}