// Reference: UnityEngine.UI
using Oxide.Core;
using System;
using System.Reflection;

namespace Oxide.Plugins
{
	[Info("Stacksize", "Noviets", "1.0.5")]
	[Description("Stacksize")]

	class Stacksize : HurtworldPlugin
	{
		void OnServerInitialized() => Loaded();
		void Loaded()
		{
			LoadDefaultConfig();

			GlobalItemManager GIM = Singleton<GlobalItemManager>.Instance;
            (GIM.GetItem(4) as ConsumableItem)?.StackSize((int)Config["Steak"]);
            (GIM.GetItem(5) as ConsumableItem)?.StackSize((int)Config["Steak"]);
			(GIM.GetItem(6) as ConsumableItem)?.StackSize((int)Config["Steak"]);
			(GIM.GetItem(25) as ConsumableItem)?.StackSize((int)Config["FreshOwrong"]);
			(GIM.GetItem(53) as ConstructionItem)?.StackSize((int)Config["BlastFurnace"]);
			(GIM.GetItem(88) as GearItem)?.StackSize((int)Config["Backpacks"]);
			(GIM.GetItem(90) as GearItem)?.StackSize((int)Config["Backpacks"]);
			(GIM.GetItem(91) as ConstructionItem)?.StackSize((int)Config["ConstructionHammer"]);
			(GIM.GetItem(93) as ConstructionItem)?.StackSize((int)Config["OwnershipStake"]);
			(GIM.GetItem(127) as ConstructionItem)?.StackSize((int)Config["Drills"]);
			(GIM.GetItem(144) as ConstructionItem)?.StackSize((int)Config["C4"]);
			(GIM.GetItem(155) as ExplosiveItem)?.StackSize((int)Config["Dynamite"]);
			(GIM.GetItem(166) as VehicleAttachmentWheelItem)?.StackSize((int)Config["Wheels"]);
			(GIM.GetItem(167) as VehicleAttachmentWheelItem)?.StackSize((int)Config["Wheels"]);
			(GIM.GetItem(171) as VehicleAttachmentEngineItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(172) as VehicleAttachmentEngineItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(173) as VehicleAttachmentEngineItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(174) as VehicleAttachmentGearBoxItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(175) as VehicleAttachmentGearBoxItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(178) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(179) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(180) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(181) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(182) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(183) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(184) as VehicleAttachmentWheelItem)?.StackSize((int)Config["Wheels"]);
			(GIM.GetItem(192) as VehicleAttachmentWheelItem)?.StackSize((int)Config["Wheels"]);
			(GIM.GetItem(193) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(194) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(195) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(196) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(197) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(198) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(199) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(200) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(201) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(202) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(203) as VehicleAttachmentSimpleItem)?.StackSize((int)Config["CarPanels"]);
			(GIM.GetItem(204) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(205) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(206) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(207) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(222) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(223) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(224) as PaintMaskItem)?.StackSize((int)Config["Designs"]);
			(GIM.GetItem(232) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(233) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(234) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(235) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(236) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(237) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(238) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(239) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(240) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(241) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(242) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(243) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(244) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(245) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(246) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(247) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(248) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(249) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(250) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(251) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(252) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(253) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(254) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(255) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(256) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(257) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(258) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(259) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(260) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(261) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(262) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(263) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(264) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(265) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(266) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(267) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(268) as PaintItem)?.StackSize((int)Config["Paints"]);
			(GIM.GetItem(273) as ConstructionItem)?.StackSize((int)Config["LandcrabMine"]);
			(GIM.GetItem(274) as ConstructionItem)?.StackSize((int)Config["PoisonTrap"]);
			(GIM.GetItem(276) as GearItem)?.StackSize((int)Config["Backpacks"]);
			(GIM.GetItem(277) as GearItem)?.StackSize((int)Config["Backpacks"]);
			(GIM.GetItem(296) as VehicleAttachmentEngineItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(297) as VehicleAttachmentEngineItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(298) as VehicleAttachmentEngineItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(299) as VehicleAttachmentEngineItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(300) as VehicleAttachmentGearBoxItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(301) as VehicleAttachmentGearBoxItem)?.StackSize((int)Config["CarParts"]);
			(GIM.GetItem(304) as MeleeWeaponItem)?.StackSize((int)Config["Wrench"]);
			(GIM.GetItem(305) as VehicleAttachmentWheelItem)?.StackSize((int)Config["Wheels"]);
			(GIM.GetItem(306) as VehicleAttachmentWheelItem)?.StackSize((int)Config["Wheels"]);
			(GIM.GetItem(307) as ConstructionItem)?.StackSize((int)Config["Drills"]);
			(GIM.GetItem(308) as ConstructionItem)?.StackSize((int)Config["Drills"]);
			(GIM.GetItem(310) as ConstructionItem)?.StackSize((int)Config["Sign"]);
		}
		protected override void LoadDefaultConfig()
        {
			if(Config["Steak"] == null) Config.Set("Steak", 1);
			if(Config["FreshOwrong"] == null) Config.Set("FreshOwrong", 1);
			if(Config["Dynamite"] == null) Config.Set("Dynamite", 5);
			if(Config["C4"] == null) Config.Set("C4", 1);
			if(Config["Paints"] == null) Config.Set("Paints", 1);
			if(Config["PoisonTrap"] == null) Config.Set("PoisonTrap", 1);
			if(Config["CarParts"] == null) Config.Set("CarParts", 1);
			if(Config["CarPanels"] == null) Config.Set("CarPanels", 1);
			if(Config["Wheels"] == null) Config.Set("Wheels", 1);
			if(Config["Designs"] == null) Config.Set("Designs", 1);
			if(Config["Drills"] == null) Config.Set("Drills", 1);
			if(Config["Wrench"] == null) Config.Set("Wrench", 1);
			if(Config["OwnershipStake"] == null) Config.Set("OwnershipStake", 1);
			if(Config["ConstructionHammer"] == null) Config.Set("ConstructionHammer", 1);
			if(Config["BlastFurnace"] == null) Config.Set("BlastFurnace", 1);
			if(Config["Backpacks"] == null) Config.Set("Backpacks", 1);
			if(Config["LandcrabMine"] == null) Config.Set("LandcrabMine", 1);
			if(Config["Sign"] == null) Config.Set("Sign", 1);
            SaveConfig();
        }
	}
}