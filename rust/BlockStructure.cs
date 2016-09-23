using System.Collections.Generic;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("BlockStructure", "Marat", "1.0.1, ResourceId = 2092")]
	[Description("Sets a limit build in height and depth in water")]
	
    class BlockStructure : RustPlugin
    {
		void Loaded()
        {
			LoadConfiguration();
            LoadDefaultMessages();
			permission.RegisterPermission(permBS, this);
        }
		
		int HeightBlock = 15;
		int WaterBlock = -1;
		bool ConfigChanged;
		bool usePermissions = true;
        bool BlockInHeight = false;
		bool BlockInWater = false;
		bool BlockInRock = false;
        string permBS = "blockstructure.allowed";
		
		protected override void LoadDefaultConfig() => PrintWarning("New configuration file created.");

        void LoadConfiguration()
        {
			HeightBlock = GetConfigValue("Options", "Height for block", HeightBlock);
			WaterBlock = GetConfigValue("Options", "Depth for block", WaterBlock);
			BlockInHeight = GetConfigValue("Options", "Block in Height", BlockInHeight);
			BlockInWater = GetConfigValue("Options", "Block in Water", BlockInWater);
			BlockInRock = GetConfigValue("Options", "Block In Rock", BlockInRock);
			usePermissions = GetConfigValue("Options", "UsePermissions", usePermissions);
			if (!ConfigChanged) return;
            PrintWarning("Configuration file updated.");
            SaveConfig();
		}
		T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                ConfigChanged = true;
            }
            if (data.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            data[setting] = value;
            ConfigChanged = true;
            return (T)Convert.ChangeType(value, typeof(T));
        }
		void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
				["blockWater"] = "<size=16><color=yellow>You can not build in water</color></size>",
                ["blockHeight"] = "<size=16><color=yellow>You can not build higher {0} meters</color></size>",
				["block"] = "<size=16><color=red>You can not build here</color></size>"
            }, this, "en");
        }
        void Block(BaseNetworkable block, BasePlayer player, bool Height, bool Water)
        {
            if (usePermissions && !IsAllowed(player.UserIDString, permBS) && block && !block.isDestroyed)
            {
                Vector3 Pos = block.transform.position;
                if (Height || Water)
                {
                    float height = TerrainMeta.HeightMap.GetHeight(Pos);
                    if (Height && Pos.y - height > HeightBlock)
                    {
                        Reply(player, Lang("blockHeight", player.UserIDString, HeightBlock));
                        block.Kill(BaseNetworkable.DestroyMode.Gib);
                        return;
                    }
                    else if (Water && height < 0 && height < WaterBlock && Pos.y < 2.8f )
                    {
                        Reply(player, Lang("blockWater", player.UserIDString, WaterBlock));
                        block.Kill(BaseNetworkable.DestroyMode.Gib);
                        return;
                    }
                }
				if (BlockInRock)
				{
				    Pos.y += 200;
                    RaycastHit[] hits = Physics.RaycastAll(Pos, Vector3.down, 200.0f);
                    Pos.y -= 200;
                    for (int i = 0; i < hits.Length; i++)
                    {
                        RaycastHit hit = hits[i];
                        if (hit.collider)
                        {
                            if (hit.collider.name == "Mesh")
                            {
							    Reply(player, Lang("block", player.UserIDString));
                                block.Kill(BaseNetworkable.DestroyMode.Gib);
                            }
                        }
                    }
				}
            }
        }
		string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        void Reply(BasePlayer player, string message, string args = null) => PrintToChat(player, $"{message}", args);
        void OnEntityBuilt(Planner plan, GameObject obj) => Block(obj.GetComponent<BaseNetworkable>(), plan.GetOwnerPlayer(), BlockInHeight, BlockInWater);
		bool IsAllowed(string id, string perm) => permission.UserHasPermission(id, perm);
    }
}