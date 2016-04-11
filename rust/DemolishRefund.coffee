Title: "DemolishRefund"
Author: "ÃthyrSurfer"
Version: V(0, 2, 2)
Description: "Refund the cost of demolished structures."
isBuildable: {
  "foundation": true,
  "roof": true,
  "pillar": true,
  "wall.low": true,
  "floor": true,
  "wall.doorway": true,
  "wall.window": true,
  "foundation.steps": true,
  "floor.triangle": true,
  "foundation.triangle": true,
  "wall": true,
  "block.stair.ushape": true,
  "block.stair.lshape": true,
  "wall.frame": true,
  "floor.frame": true
}

Init: () =>
  print "DemolishRefund PreInit build something to fully initialize..."
  this.init = (planner) =>
    print "DemolishRefund Initializing..."
    this.isBuildable = {}
    (this.isBuildable[b.name] = true) for b in planner.buildableList
    print "DemolishRefund Initialized"
    this.init = () =>

OnEntityBuilt: (planner, obj) =>
  this.init(planner)

OnStructureDemolish: (block, player, isImmediate) =>
  global = importNamespace("")
  blockName = block.blockDefinition.hierachyName
  if this.isBuildable[blockName]
    costs = block.blockDefinition.grades[block.grade].costToBuild
    items = for i in [0...costs.Count]
      ia = costs[i]
      global.ItemManager.CreateByItemID(ia.itemid, ia.amount)
  else
    if blockName is "wall.external.high.wood"
      blockName = "wall.external.high"
    itemdef = global.ItemManager.FindItemDefinition(blockName)
    items = [global.ItemManager.CreateByItemID(itemdef.itemid)]
  player.GiveItem(item) for item in items
  return
