namespace Oxide.Plugins
{
    [Info("Instant Research", "Artasan", 1.0)]
    [Description("Allows instant research.")]
    public class InstantResearch : RustPlugin
    {
        void OnServerInitialized()
        {
            updateResearchTables();
        }

        void OnItemDeployed(Deployer deployer, BaseEntity deployedEntity)
        {
            Item item = deployer.GetItem();

            if (item.info.shortname == "research_table")
            {
                updateResearchTables();
            }
        }

        void OnItemResearchStart(ResearchTable table)
            {
            table.researchDuration = 0f;
            }

        public void updateResearchTables()
		{}
    }
}
