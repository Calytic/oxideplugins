Title: "UpgradeRefund"
Author: "Ãthyr"
Version: V(0, 0, 1)
Description: "Refund the cost of the previous tier when upgrading."

OnStructureUpgrade: (block, player, grade) =>
  global = importNamespace("")
  costs = block.blockDefinition.grades[block.grade].costToBuild
  for i in [0...costs.Count]
    ia = costs[i]
    item = global.ItemManager.CreateByItemID(ia.itemid, ia.amount)
    player.GiveItem(item)
  return
