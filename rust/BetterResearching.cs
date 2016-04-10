namespace Oxide.Plugins
{
    [Info("Better Researching", "Waizujin", 1.3)]
    [Description("Allows instant researching and adjustable research chance.")]
    public class BetterResearching : RustPlugin
    {
        public float ResearchChance { get { return Config.Get<float>("ResearchChance"); } }
        public float ResearchCostFraction { get { return Config.Get<float>("ResearchCostFraction"); } }
        public int PaperRequired { get { return Config.Get<int>("PaperRequired"); } }

        protected override void LoadConfig()
        {
            bool dirty = false;
            base.LoadConfig();

            if (Config["InstantResearch"] == null)
            {
                Config["InstantResearch"] = false;
                dirty = true;
            }

            if (Config["ResearchChance"] == null)
            {
                Config["ResearchChance"] = 0.3f;
                dirty = true;
            }

            if (Config["PaperRequired"] == null)
            {
                Config["PaperRequired"] = 10;
                dirty = true;
            }

            if (Config["ResearchCostFraction"] == null)
            {
                Config["ResearchCostFraction"] = 1f;
                dirty = true;
            }

            if (dirty)
            {
                PrintWarning("Updating configuration file with new values.");
                SaveConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();

            Config["InstantResearch"] = false;
            Config["ResearchChance"] = 0.3f;
            Config["PaperRequired"] = 10;
            Config["ResearchCostFraction"] = 1f;

            SaveConfig();
        }

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
            table.researchCostFraction = ResearchCostFraction;

            if((bool) Config["InstantResearch"]) {
                table.researchDuration = 0f;
            }
        }

        private float OnItemResearchEnd(ResearchTable table, float chance)
        {
            Item item = table.GetResearchItem();
            float num1 = ResearchChance;

            if (!item.hasCondition)
            {
                if (chance <= num1)
                {
                    chance = 0f;
                }
                else
                {
                    chance = 1f;
                }

                return chance;
            }

            float num2 = item.maxCondition / item.info.condition.max;
            float successChance = num1 * num2 * item.conditionNormalized;

            if (chance <= successChance)
            {
                chance = 0f;
            }
            else
            {
                chance = 1f;
            }

            return chance;
        }

        public void updateResearchTables()
        {
            var researchTables = UnityEngine.Object.FindObjectsOfType<ResearchTable>();

            foreach (ResearchTable researchTable in researchTables)
            {
                researchTable.requiredPaper = PaperRequired;
            }
        }
    }
}
