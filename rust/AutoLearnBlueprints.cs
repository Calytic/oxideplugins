using System.Collections.Generic;
using System.Linq;

using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("AutoLearnBlueprints", "ApocDev", "1.0.5", ResourceId = 1056)]
    public class AutoLearnBlueprints : RustPlugin
    {
        // Add/remove to this list (using the shortname)
        // To automatically have players learn specific blueprints.
        private static readonly List<string> DefaultIncludeBps = new List<string>
        {
            "lock.code", // Code Lock
            "pistol.revolver", // Revolver
            "ammo.pistol" // Pistol Ammo (for the revolver)
        };

        private List<string> LearnBlueprints
        {
            get
            {
                var o = Config.Get<List<string>>("DefaultBlueprints");
                if (o == null || o.Count == 0)
                {
                    return new List<string>();
                }

                return o;
            }
        }

        protected override void LoadDefaultConfig()
        {
            Config["DefaultBlueprints"] = DefaultIncludeBps;
        }

        void OnServerInitialized()
        {
            HandleUpdateBlueprintsCommand();
        }

        void LearnDefaultBlueprints(BasePlayer player)
        {
            var blueprints = ItemManager.bpList.Where(bp => LearnBlueprints.Contains(bp.targetItem.shortname)).ToList();
            foreach (var bp in blueprints)
            {
                // Make sure the player hasn't learned it already. (Doesn't hurt if they did already though)
                if (!player.blueprints.CanCraft(bp.targetItem.itemid, 0))
                {
                    player.blueprints.Learn(bp.targetItem);
                    player.ChatMessage("You have learned the " + bp.targetItem.displayName.translated + " blueprint automatically!");
                }
            }
        }

        [ConsoleCommand("bps.update")]
        void HandleUpdateBlueprintsCommand()
        {
            // Only update active players. Players that join later will be hit with OnPlayerInit when they wake up anyway.
            foreach (var player in BasePlayer.activePlayerList)
            {
                LearnDefaultBlueprints(player);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            LearnDefaultBlueprints(player);
        }
    }
}
