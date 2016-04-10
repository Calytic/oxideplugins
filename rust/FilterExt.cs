using System.Linq;

using Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("FilterExt", "Wulf/lukespragg", 0.1)]
    [Description("Extension to Oxide's filter for removing unwanted messages.")]

    class FilterExt : RustPlugin
    {
        void Loaded()
        {
            // Get existing filter list
            var filter = RustExtension.Filter.ToList();

            // Add messages to filter
            filter.Add("- deleting");
            filter.Add("ERROR building certificate chain");
            filter.Add("Enforcing SpawnPopulation Limits");
            filter.Add("Reporting Performance Data");
            filter.Add("Saved ");
            filter.Add("Saving complete");
            filter.Add("TimeWarning:");
            filter.Add("but max allowed is");
            filter.Add("[event] assets/bundled/prefabs/");

            // Update filter list
            RustExtension.Filter = filter.ToArray();
        }
    }
}
