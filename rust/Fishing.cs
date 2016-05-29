using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
     [Info("Fishing", "Colon Blow", "1.1.9", ResourceId = 1537)]
     class Fishing : RustPlugin
     {
	public int fishchance;
	public int fishchancemodweapon;
	public int fishchancemodattire;
	public int fishchancemoditem;
	public int fishchancemodtime;
	public string FishIcon;
	public string chancetext1;
	public string chancetext2;
	public float currenttime;
	public float ghitDistance;
	public float whitDistance;
	private static int waterlayer;
	private static int groundlayer;
	private bool Changed;
	//string CaughtFish = "assets/content/unimplemented/fishing_rod/vm_fishing_rod/pluck_fish.prefab";
	string randomlootprefab = "assets/bundled/prefabs/radtown/dmloot/dm tier3 lootbox.prefab";

	Dictionary<ulong, string> GuiInfo = new Dictionary<ulong, string>();

        void Loaded()
        {                       
        	lang.RegisterMessages(messages, this);
        	LoadVariables();
		permission.RegisterPermission("fishing.allowed", this);
	}
        void LoadDefaultConfig()
        {
	    	Puts("No configuration file found, generating...");
	    	Config.Clear();
            	LoadVariables();
        }

        void OnServerInitialized()
        {
        	waterlayer = UnityEngine.LayerMask.GetMask("Water");
	        groundlayer = UnityEngine.LayerMask.GetMask("Terrain", "World", "Construction");
        }

        bool IsAllowed(BasePlayer player, string perm)
        {
        	if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
        	return false;
        }
//Configuration Variables

	public bool ShowFishCatchIcon = true;
	public bool allowrandomitemchance = true;
	public bool useweaponmod = true;
	public bool useattiremod = true;
	public bool useitemmod = true;
	public bool usetimemod = true;

	public int fishchancedefault = 10;
	public int randomitemchance = 1;
	public int fishchancemodweaponbonus = 10;
	public int fishchancemodattirebonus = 10;
	public int fishchancemoditembonus = 10;
	public int fishchancemodtimebonus = 10;

	public string iconcommonfish2 = "http://i.imgur.com/HftxU00.png";
	public string iconuncommonfish1 = "http://i.imgur.com/xReDQM1.png";
	public string iconcommonfish1 = "http://i.imgur.com/rBEmhpg.png";
	public string iconrandomitem = "http://i.imgur.com/y2scGmZ.png";
	public string iconrarefish1 = "http://i.imgur.com/jMZxGf1.png";

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        private void LoadConfigVariables()
        {
            CheckCfg("Show Fish Catch Indicator", ref ShowFishCatchIcon);
            CheckCfg("Allow Random Item Chance", ref allowrandomitemchance);
            CheckCfg("Allow Bonus from Weapon", ref useweaponmod);
            CheckCfg("Allow Bonus from Attire", ref useattiremod);
            CheckCfg("Allow Bonus from Item", ref useitemmod);
            CheckCfg("Allow Bonus from Time of Day", ref usetimemod);

            CheckCfg("Chance - Default to Catch Fish (Percentage)", ref fishchancedefault);
            CheckCfg("Chance - Get Random World Item (Percentage)", ref randomitemchance);
            CheckCfg("Bonus - From Weapon (Percentage)", ref fishchancemodweaponbonus);
            CheckCfg("Bonus - From Attire (Percentage)", ref fishchancemodattirebonus);
            CheckCfg("Bonus - From Items (Percentage)", ref fishchancemoditembonus);
            CheckCfg("Bonus - From Time of Day (Percentage)", ref fishchancemodtimebonus);

            CheckCfg("Icon - Url for Common Fish 2", ref iconcommonfish2);
            CheckCfg("Icon - Url for Common Fish 1", ref iconcommonfish1);
            CheckCfg("Icon - Url for UnCommon Fish 1", ref iconuncommonfish1);
            CheckCfg("Icon - Url for Random Item", ref iconrandomitem);
            CheckCfg("Icon - Url for Rare Fish 1", ref iconrarefish1);
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }

        object GetConfig(string menu, string datavalue, object defaultValue)
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

//Plugin Messages that use language
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"missedfish", "You Missed the fish...." },
            {"commonfish1", "You Got a Savis Island Swordfish" },
            {"commonfish2", "You Got a Hapis Island RazorJaw" },
            {"uncommonfish1", "You Got a Colon BlowFish" },
            {"rarefish1", "You Got a Craggy Island Dorkfish" },
            {"randomitem", "You found something in the water !!!" },
            {"chancetext1", "Your chance to catch a fish is : " },
            {"chancetext2", "% at Current time of : " }
        };

// Modifiers that chance the chance a player will get a fish or not
	void catchChanceMod(BasePlayer player)
	{

	fishchancemodweapon = 0;
	fishchancemodattire = 0;
	fishchancemoditem = 0;
	fishchancemodtime = 0;
	currenttime = TOD_Sky.Instance.Cycle.Hour;

	Item activeItem = player.GetActiveItem();

            	if (activeItem != null && activeItem.info.shortname == "spear.stone" && useweaponmod)
		{ 
		fishchancemodweapon = fishchancemodweaponbonus;
		}
            	if (activeItem != null && activeItem.info.shortname == "crossbow" && useweaponmod)
		{ 
		fishchancemodweapon = fishchancemodweaponbonus;
		}

	int hasBoonieOn = player.inventory.containerWear.GetAmount(-1397343301, true);
	if (hasBoonieOn >= 1 && useattiremod)
		{ 
		fishchancemodattire = fishchancemodattirebonus;
		}
	int hasPookie = player.inventory.containerMain.GetAmount(640562379, true);
	if (hasPookie >= 1 && useitemmod)
		{ 
		fishchancemoditem = fishchancemoditembonus;
		}
	if (currenttime < 8 && currenttime > 6 && usetimemod)
		{
		fishchancemodtime = fishchancemodtimebonus;
		}
	if (currenttime < 19 && currenttime > 16 && usetimemod)
		{
		fishchancemodtime = fishchancemodtimebonus;
		}
	return;
	}
	
// Checks to see if player is looking at water
	bool isLookingAtWater(BasePlayer player)
        {
		whitDistance = 0;
		ghitDistance = 0;
		UnityEngine.Ray ray = new UnityEngine.Ray(player.eyes.position, player.eyes.HeadForward());

		var hitsw = UnityEngine.Physics.RaycastAll(ray, 5f, waterlayer);
		var hitsg = UnityEngine.Physics.RaycastAll(ray, 5f, groundlayer);

	foreach (var hit in hitsw) 
		{
		if (hit.distance == null) return false;
		whitDistance = hit.distance;
		}
	foreach (var hit in hitsg)
		{
		if (hit.distance == null) return false;
		ghitDistance = hit.distance;
		}
		if (whitDistance < ghitDistance && whitDistance > 0) return true;
		return false;
        }

// Chance roll to see if player gets a fish or not in Open water areas
	void rollforfish(BasePlayer player, HitInfo hitInfo)
	{
		catchChanceMod(player);
		int roll = UnityEngine.Random.Range(0, 100);
		fishchance = fishchancedefault+fishchancemodweapon+fishchancemodattire+fishchancemoditem+fishchancemodtime;
		if (roll < fishchance)
                {
			catchFishFX(player, hitInfo);
			return;
                }
		else
		SendReply(player, lang.GetMessage("missedfish", this));
		return;
	}

// Effect for catching fish
	void catchFishFX(BasePlayer player, HitInfo hitInfo)
	{

	int fishtyperoll = UnityEngine.Random.Range(1, 100);
	if (fishtyperoll > 99)
		{
		FishIcon = iconrarefish1;
		SendReply(player, lang.GetMessage("rarefish", this));
		player.inventory.GiveItem(ItemManager.CreateByItemID(865679437, 5));
		player.Command("note.inv", 865679437, 5);
		}
	if (fishtyperoll >= 90 && fishtyperoll < 100)
		{
		FishIcon = iconuncommonfish1;
		SendReply(player, lang.GetMessage("uncommonfish1", this));
		player.inventory.GiveItem(ItemManager.CreateByItemID(865679437, 2));
		player.Command("note.inv", 865679437, 2);
		}
	if (fishtyperoll > 45 && fishtyperoll < 90)
		{
		FishIcon = iconcommonfish2;
		SendReply(player, lang.GetMessage("commonfish2", this));
		player.inventory.GiveItem(ItemManager.CreateByItemID(88869913, 1));
		player.Command("note.inv", 88869913, 1);
		}
	if (fishtyperoll >= 1 && fishtyperoll <= 45)
		{
		FishIcon = iconcommonfish1;
		SendReply(player, lang.GetMessage("commonfish1", this));
		player.inventory.GiveItem(ItemManager.CreateByItemID(865679437, 1));
		player.Command("note.inv", 865679437, 1);
		}
	if (fishtyperoll < randomitemchance && allowrandomitemchance)
		{
		FishIcon = iconrandomitem;
		SendReply(player, lang.GetMessage("randomitem", this));
		SpawnLootBox(player, hitInfo);
		}

	catchFishCui(player);
	}

// Runs Fishing Action on player attack when all criteria are met
	void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
	{
	var player = attacker as BasePlayer;

	if (!IsAllowed(player, "fishing.allowed")) return;
	if (IsAllowed(player, "fishing.allowed"))
		{
		if (hitInfo?.HitEntity as BaseCombatEntity) return;
		if (hitInfo == null) return;

		if (hitInfo.WeaponPrefab.ToString().Contains("spear") || hitInfo.WeaponPrefab.ToString().Contains("bow"))
			{
			if (isLookingAtWater(player))
				{
				rollforfish(player, hitInfo);
				hitInfo.CanGather = true;
				return;
				}
			}
			if (player.IsHeadUnderwater())
			{
				{
				rollforfish(player, hitInfo);
				hitInfo.CanGather = true;
				return;
				}
			}
		}
	}

// Show fish icon and player animation when catching fish
	void catchFishCui(BasePlayer player)
	{
	if (ShowFishCatchIcon) FishingGui(player);
	}

// Displays Fish catch icon
        void FishingGui(BasePlayer player)
        {
	DestroyCui(player);

        var elements = new CuiElementContainer();
        GuiInfo[player.userID] = CuiHelper.GetGuid();

        if (ShowFishCatchIcon)
        {
        	elements.Add(new CuiElement
                {
                    Name = GuiInfo[player.userID],
                    Components =
                    {
                        new CuiRawImageComponent { Color = "1 1 1 1", Url = FishIcon, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        new CuiRectTransformComponent { AnchorMin = "0.025 0.04",  AnchorMax = "0.075 0.12" }
                    }
                });
            }

            CuiHelper.AddUi(player, elements);
	    timer.Once(1f, () => DestroyCui(player));
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                string guiInfo;
                if (GuiInfo.TryGetValue(player.userID, out guiInfo)) CuiHelper.DestroyUi(player, guiInfo);
            }
        }
	void DestroyCui(BasePlayer player)
	{
                string guiInfo;
                if (GuiInfo.TryGetValue(player.userID, out guiInfo)) CuiHelper.DestroyUi(player, guiInfo);
	}

	
	[ChatCommand("fishchance")]
        void cmdChatfishchance(BasePlayer player, string command, string[] args)
        {
	currenttime = TOD_Sky.Instance.Cycle.Hour;
	catchChanceMod(player);
	var fishchancepercent = fishchancedefault+fishchancemodweapon+fishchancemodattire+fishchancemoditem+fishchancemodtime;
	SendReply(player, lang.GetMessage("chancetext1", this) + fishchancepercent + lang.GetMessage("chancetext2", this) + currenttime);
        }

        void SpawnLootBox(BasePlayer player, HitInfo hitInfo)
        {
            var createdPrefab = GameManager.server.CreateEntity(randomlootprefab, hitInfo.HitPositionWorld);
            BaseEntity entity = createdPrefab?.GetComponent<BaseEntity>();
            entity?.Spawn(true);
        }

     }
}