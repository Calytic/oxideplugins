using UnityEngine;

namespace Oxide.Plugins
{
    [Info("No Signs", "bawNg", 0.4)]
    class NoSigns : RustPlugin
    {
        string notAllowedMessage = "<color=red>You are not allowed to use signs on this server</color>";

        void Loaded()
        {
            var signs = UnityEngine.Object.FindObjectsOfType<Signage>();
            Puts($"[No Signs] Removing {signs.Length} signs from the map...");
            foreach (var sign in signs) sign.Kill();
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            var player = container.playerOwner;
            if (!player || !item.info) return;
            
            if (!player.IsAdmin() && item.info.shortname.StartsWith("sign."))
            {
                PrintToChat(player, notAllowedMessage);
                item.Remove(0f);
            }
        }

        object OnCanCraft(ItemCrafter item_crafter, ItemBlueprint blueprint, int amount)
        {
            if (item_crafter.containers.Count < 1) return false;

            var item = blueprint.targetItem;
            if (!item) return null;

            var player = item_crafter.containers[0].playerOwner;
            if (!player) return null;

            if (!player.IsAdmin() && item.shortname.StartsWith("sign."))
            {
                PrintToChat(player, notAllowedMessage);
                return false;
            }

            return null;
        }

        void OnEntityBuilt(Planner planner, GameObject game_object)
        {
            if (game_object == null) return; // sphere check failed

            var player = planner.ownerPlayer;
            var entity = game_object.GetComponent<Signage>();
            if (!player || !entity) return;

            if (!player.IsAdmin() && entity.LookupPrefabName().StartsWith("signs/"))
            {
                PrintToChat(player, notAllowedMessage);
                entity.Kill();
            }
        }
    }
}
