var BarrelPoints = {
  Title: "BarrelPoints",
  Author: "Scriptzyy",
  Version: V(0, 1, 31),
  Description: "Gives players RewardPoints for destroying barrels.",
  HasConfig: true,

  OnServerInitialized: function(){
    for(let i = 0; i < Object.keys(this.Config.options.bonusPoints).length; i++){
      permission.RegisterPermission("barrelpoints." + Object.keys(this.Config.options.bonusPoints)[i], this.Plugin);
    }
  },

  LoadDefaultConfig: function(){
    this.Config.options = {
      pointsPerBarrel: 5,
      bonusPoints: {
        "add2": 2,
        "add3": 3,
        "vip": 5
      }
    };
  },

  OnEntityDeath: function(entity, info){
    if(entity.ShortPrefabName == "loot-barrel-1" || entity.ShortPrefabName == "loot-barrel-2" || entity.ShortPrefabName == "loot_barrel_1" || entity.ShortPrefabName == "loot_barrel_2" || entity.ShortPrefabName == "oil_barrel" || entity.ShortPrefabName == "oil-barrel"){
      let gainedPoints = this.Config.options.pointsPerBarrel;
      for(let i = 0; i < Object.keys(this.Config.options.bonusPoints).length; i++){
        if(permission.UserHasPermission(info.Initiator.UserIDString, "barrelpoints." + Object.keys(this.Config.options.bonusPoints)[i])){
          gainedPoints += this.Config.options.bonusPoints[Object.keys(this.Config.options.bonusPoints)[i]];
        }
      }
      info.Initiator.sendConsoleCommand("sr add " + info.Initiator.displayName + " " + gainedPoints);
      info.Initiator.sendConsoleCommand("chat.add", "", "<color=#a6d8b0>You have gained +" + gainedPoints + " RP!</color>");
    }
  }

}
