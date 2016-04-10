using System.Linq;
using Oxide.Game.TheForest;

namespace Oxide.Plugins
{
    [Info("FilterExt", "Wulf/lukespragg", 0.1, ResourceId = 1466)]
    [Description("Extension to Oxide's filter for removing unwanted console messages.")]

    class FilterExt : TheForestPlugin
    {
        void Loaded()
        {
            // Get existing filter list
            var filter = TheForestExtension.Filter.ToList();

            // Add messages to filter
            filter.Add("Placeholder");

            // Update filter list
            TheForestExtension.Filter = filter.ToArray();
        }
    }
}
