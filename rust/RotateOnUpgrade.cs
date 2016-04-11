using System;
using System.Collections.Generic;
using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("RotateOnUpgrade", "KeyboardCavemen", "1.3.0")]
    class RotateOnUpgrade : RustPlugin
    {
        private bool allowAdminRotate;
        private bool allowDemolish;
        private bool allowDemolishDoors;
        private int amountOfMinutesAfterUpgrade = 0;
        private List<KeyValuePair<BuildingBlock, DateTime>> upgradedBuildingBlocks = new List<KeyValuePair<BuildingBlock, DateTime>>();

        private int timerInterval = 60;
        private DateTime lastTimerTick;

        //Oxide Hook
        void OnServerInitialized()
        {
            checkConfig();

            this.allowAdminRotate = Config.Get<bool>("allowAdminRotate");
            this.allowDemolish = Config.Get<bool>("allowDemolish");
            this.allowDemolishDoors = Config.Get<bool>("allowDemolishDoors");
            this.amountOfMinutesAfterUpgrade = Config.Get<int>("amountOfMinutesAfterUpgrade");
            loadBuildingBlocksFromConfig();
 
            timer.Every(timerInterval, () => timerTickHandler());
        }

        //Oxide Hook
        protected override void LoadDefaultConfig()
        {
            Config["allowAdminRotate"] = true;
            Config["allowDemolish"] = true;
            Config["allowDemolishDoors"] = false;
            Config["amountOfMinutesAfterUpgrade"] = 10;
            Config["configVersion"] = this.Version.ToString();
            Config["positionsOfBuildingBlocks"] = new List<string>();
            Config["timesOfUpgrade"] = new List<string>();
            Config.Save(Manager.ConfigPath + "\\" + this.Name + ".json");

            Puts("Created new default config.");
        }

        private void loadBuildingBlocksFromConfig()
        {
            List<string> positionsOfBuildingBlocks = new List<string>();
            positionsOfBuildingBlocks = Config.Get<List<string>>("positionsOfBuildingBlocks");

            List<string> timesOfUpgrade = new List<string>();
            timesOfUpgrade = Config.Get<List<string>>("timesOfUpgrade");

            List<BuildingBlock> allBuildingBlocks = new List<BuildingBlock>();
            allBuildingBlocks.AddRange(UnityEngine.GameObject.FindObjectsOfType<BuildingBlock>());

            for (int i = 0; i < positionsOfBuildingBlocks.Count; i++)
            {
                List<BuildingBlock> results = new List<BuildingBlock>();
                results = allBuildingBlocks.FindAll(x => x.transform.position.ToString().Equals(positionsOfBuildingBlocks[i]) && x.HasFlag(BaseEntity.Flags.Reserved2));

                if (results.Count > 0)
                {
                    DateTime timeOfUpgrade = DateTime.Parse(timesOfUpgrade[i]);
                    foreach (BuildingBlock buildingBlock in results)
                    {
                        this.upgradedBuildingBlocks.Add(new KeyValuePair<BuildingBlock, DateTime>(buildingBlock, timeOfUpgrade));
                    }
                }
            }

            Puts("Loaded " + this.upgradedBuildingBlocks.Count.ToString() + " blocks from config");

            updateConfig();
        }

        //Oxide Hook
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (this.allowAdminRotate && player.IsAdmin() && player.GetActiveItem() != null && player.GetActiveItem().info.shortname.Equals("hammer"))
            {
                if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                {
                    RaycastHit hit;
                    if (Physics.Raycast(player.eyes.position, (player.eyes.rotation * Vector3.forward), out hit, 2f, Layers.Server.Buildings))
                    {
                        BaseEntity baseEntity = hit.collider.gameObject.ToBaseEntity();
                        if (baseEntity != null)
                        {
                            BuildingBlock block = baseEntity.GetComponent<BuildingBlock>();

                            if (block != null && block.blockDefinition.canRotate && !block.HasFlag(BaseEntity.Flags.Reserved1))
                            {
                                block.SetFlag(BaseEntity.Flags.Reserved1, true);
                                addBlockToList(block, DateTime.Now.AddMinutes(-this.amountOfMinutesAfterUpgrade));

                                int remainingSeconds = timerInterval - DateTime.Now.Subtract(lastTimerTick).Seconds;
                                SendReply(player, "<color=green>You can now rotate this " + block.blockDefinition.info.name.english + " for " + remainingSeconds + " seconds.</color>");
                            }
                        }
                    }
                }
            }
        }

        //Oxide Hook
        void OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (block.grade == BuildingGrade.Enum.Twigs)
            {
                if (allowDemolish)
                {
                    block.SetFlag(BaseEntity.Flags.Reserved2, true);

                    if (this.amountOfMinutesAfterUpgrade > 0)
                    {
                        addBlockToList(block, DateTime.Now);
                    }
                }
            }

            else if (block.name.Contains("build/door.hinged") && block.grade == BuildingGrade.Enum.Wood)
            {
                if (allowDemolishDoors)
                {
                    block.SetFlag(BaseEntity.Flags.Reserved2, true);

                    if (this.amountOfMinutesAfterUpgrade > 0)
                    {
                        addBlockToList(block, DateTime.Now);
                    }
                }
            }
        }

        private void checkConfig()
        {
            string configVersion = Config.Get<string>("configVersion");
            if (configVersion == null || configVersion != this.Version.ToString())
            {
                //Back it up to a .old file
                Config.Save(Manager.ConfigPath + "\\" + this.Name + ".old.json");
                Puts("Config out of date, backuped it to " + this.Name + ".old.json.");

                //Read the building parts from the old config.
                List<string> positionsOfBuildingBlocks = new List<string>();
                positionsOfBuildingBlocks = Config.Get<List<string>>("positionsOfBuildingBlocks");

                List<string> timesOfUpgrade = new List<string>();
                timesOfUpgrade = Config.Get<List<string>>("timesOfUpgrade");

                //Create the new config.
                LoadDefaultConfig();

                //If any old building parts got loaded, import them into the new config.
                if (positionsOfBuildingBlocks.Count > 0)
                {
                    Config["positionsOfBuildingBlocks"] = positionsOfBuildingBlocks;
                    Config["timesOfUpgrade"] = timesOfUpgrade;

                    SaveConfig();
                    Puts("Imported the old building parts into the new config.");
                } 
            }
        }

        private void timerTickHandler()
        {
            this.lastTimerTick = DateTime.Now;
            int oldCount = this.upgradedBuildingBlocks.Count;

            for (int i = 0; i < this.upgradedBuildingBlocks.Count; i++)
            {
                if (DateTime.Now >= this.upgradedBuildingBlocks[i].Value.AddMinutes(amountOfMinutesAfterUpgrade))
                {
                    this.upgradedBuildingBlocks[i].Key.SetFlag(BaseEntity.Flags.Reserved1, false);
                    this.upgradedBuildingBlocks[i].Key.SetFlag(BaseEntity.Flags.Reserved2, false);

                    this.upgradedBuildingBlocks.RemoveAt(i);
                    i--;
                }
            }

            if (!this.upgradedBuildingBlocks.Count.Equals(oldCount))
            {
                updateConfig();
            }
        }

        private void addBlockToList(BuildingBlock buildingBlock, DateTime dateTime)
        {
            KeyValuePair<BuildingBlock, DateTime> newKVP = new KeyValuePair<BuildingBlock, DateTime>(buildingBlock, dateTime);
            if (!this.upgradedBuildingBlocks.Exists(x => x.Key.Equals(newKVP.Key)))
            {
                this.upgradedBuildingBlocks.Add(newKVP);
                updateConfig();
            }

        }

        private void updateConfig()
        {
            List<string> positionsOfBuildingBlocks = new List<string>();
            List<string> timesOfUpgrade = new List<string>();

            foreach (KeyValuePair<BuildingBlock, DateTime> KVP in this.upgradedBuildingBlocks)
            {
                positionsOfBuildingBlocks.Add(KVP.Key.transform.position.ToString());
                timesOfUpgrade.Add(KVP.Value.ToString());
            }

            Config["positionsOfBuildingBlocks"] = positionsOfBuildingBlocks;
            Config["timesOfUpgrade"] = timesOfUpgrade;
            SaveConfig();
        }
    }
}