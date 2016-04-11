using DevilsIsland;
using JetStream;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Devil's Island", "Nick Holmes", 0.7, ResourceId = 1372)]
    [Description("Devil's Island Game Mode")]
    public class DevilsIsland : GameModePlugin<DevilsIslandConfig, DevilsIslandState>
    {
        private ILocator liveLocator = null;
        private ILocator locator = null;

        protected override void Initialize()
        {
            // Ugly work around for wierd servers that have worldsize == 0
            int worldSize = ConVar.Server.worldsize;
            if (worldSize == 0) worldSize = GameConfig.FallbackWorldSize;

            liveLocator = new RustIOLocator(worldSize);
            locator = new LocatorWithDelay(liveLocator, 60);
            
            if(GameConfig.IsBossPositionNotifierEnabled)
                Timers.Add("AdviseBossPosition", timer.Repeat(GameConfig.BossPositionNotifierInterval, 0, () => AdviseBossPosition()));
            
            if(GameConfig.IsHelpNotiferEnabled)
                Timers.Add("HelpNotifier", timer.Repeat(GameConfig.HelpNotifierInverval, 0, () => AdviseRules()));

            Timers.Add("BossPromote", timer.Repeat(30, 0, () => TryForceBoss()));
        }

        #region Timers and Events

        // AdviceBossPosition is called every n seconds, and updates all played on the current
        // location of the Boss.This is intended to be a negative aspect of being the Boss.
        // If there is no Boss, players are reminded how to become the Boss.
        // TODO: If there is no Boss, after x minutes, just promote someone.
        void AdviseBossPosition()
        {
            if (State.BossExists())
            {
                bool moved;
                string bossCoords = locator.GridReference(State.Boss, out moved);

                if (moved)
                    PrintToChat(Text.Broadcast_BossLocation_Moved, State.BossName, bossCoords);
                else
                    PrintToChat(Text.Broadcast_BossLocation_Static, State.BossName, bossCoords);
            }
            else
                PrintToChat(Text.Broadcast_ClaimAvailable);
        }

        // AdviseRules is called every m seconds, and reminds players where they can find the
        // Game Mode rules. Useful at the moment, but probably a bit annoying in the long term
        public void AdviseRules()
        {
            PrintToChat(Text.Broadcast_HelpAdvice);
        }

        public void TryForceBoss()
        {
            if (State.NoBoss() && State.TryForceNewBoss())
                PrintToChat("{0} has been made the new Boss. Kill him!", State.BossName);
        }

        #endregion

        #region Player Commands
        [ChatCommand("rules")]
        void RulesCommand(BasePlayer player, string command, string[] args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Text.Synopsis);
            sb.AppendLine();
            sb.AppendLine(Text.PlayerCommandSection);
            sb.AppendLine(Text.StatusCommandHint);
            sb.AppendLine(Text.ClaimCommandHint);
            sb.AppendLine(Text.RebelCommandHint);
            sb.AppendLine(Text.WhereCommandHint);
            sb.AppendLine();
            sb.AppendLine(Text.BossCommandSection);
            sb.AppendLine(Text.TaxCommandHint);
            sb.AppendLine(Text.LootCommandHint);
            sb.AppendLine(Text.RecruitCommandHint);
            sb.AppendFormat(Text.HeloStrikeCommandHint, GameConfig.HeloStrikePrice_Quantity, GameConfig.HeloStrikePrice_ItemDef.displayName.english);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("Outlaw Commands:");
            sb.AppendFormat("<color=lime>/decoy <i>player</i></color> For a few minutes, the Boss sees <i>player</i>'s location, instead of yours. Costs {0} {1}", GameConfig.DecoyPrice_Quantity, GameConfig.HeloStrikePrice_ItemDef.displayName.english);
            PrintToChat(player, sb.ToString());
        }

        // StatusCommand implements the "/status" player command. The purpose of this
        // command is simple to show users their current status, and some global status that
        // is relevent to them. Also, it should remind players of any other commands they can
        // currently use.
        [ChatCommand("status")]
        void StatusCommand(BasePlayer player, string command, string[] args)
        {
            StringBuilder sb = new StringBuilder();
            bool moved;

            string bossName = State.NoBoss() ? Text.NoBoss : State.BossName;
            string bossCoords = State.NoBoss() ? "err, nowhere!" : locator.GridReference(State.Boss, out moved);

            if (State.IsBoss(player))
            {
                sb.AppendLine(Text.YouAreTheBoss);
                sb.AppendFormat(Text.TaxRate, State.TaxRate * 100f);

                sb.AppendLine();
                if (State.Outlaws.Any())
                {
                    sb.AppendLine("Outlaws:");
                    foreach (Outlaw outlaw in State.Outlaws.All())
                    {
                        string location = locator.GridReference(outlaw.GetEffectiveTarget(), out moved);
                        sb.AppendFormat("{0}", outlaw.Player.displayName);
                        if(moved)
                            sb.AppendFormat(" on the move at {0}\n", location);
                        else
                            sb.AppendFormat(" loitering at {0}\n", location);
                    }
                }
                else
                    sb.AppendLine("There are currenly no Outlaws");

                sb.AppendLine();
                if (State.Henchmen.Any())
                {
                    sb.AppendLine("Henchmen:");
                    foreach (Henchman henchman in State.Henchmen.All())
                    {
                        string location = locator.GridReference(henchman.Player, out moved);
                        sb.AppendFormat("{0}", henchman.Player.displayName);
                        if (moved)
                            sb.AppendFormat(" on the move at {0}\n", location);
                        else
                            sb.AppendFormat(" loitering at {0}\n", location);
                    }
                }
                else
                    sb.AppendLine("You have no henchmen, you can /recruit some");
            }
            else if (State.Henchmen.Contains(player))
            {
                sb.AppendLine("<color=red><size=17>You are a Henchman</size></color>");

                sb.AppendLine();
                if (State.Outlaws.Any())
                {
                    sb.AppendLine("Outlaws:");
                    foreach (Outlaw outlaw in State.Outlaws.All())
                    {
                        string location = locator.GridReference(outlaw.GetEffectiveTarget(), out moved);
                        sb.AppendFormat("{0}", outlaw.Player.displayName);
                        if (moved)
                            sb.AppendFormat(" on the move at {0}\n", location);
                        else
                            sb.AppendFormat(" loitering at {0}\n", location);
                    }
                }
                else
                    sb.AppendLine("There are currenly no Outlaws");

                sb.AppendLine();
                if (State.Henchmen.All().Where(h => h.Player != player).Any())
                {
                    sb.AppendLine("Follow Henchmen:");
                    foreach (Henchman henchman in State.Henchmen.All().Where(h => h.Player != player))
                    {
                        string location = locator.GridReference(henchman.Player, out moved);
                        sb.AppendFormat("{0}", henchman.Player.displayName);
                        if (moved)
                            sb.AppendFormat(" on the move at {0}\n", location);
                        else
                            sb.AppendFormat(" loitering at {0}\n", location);
                    }
                }
                else
                    sb.AppendLine("You are the only henchman");
            }
            else
            {
                if (State.Outlaws.Contains(player))
                    sb.AppendLine(Text.YouAreAnOutlaw);

                sb.AppendFormat(Text.TaxRate, State.TaxRate * 100f);
                sb.AppendLine();

                if (State.BossExists())
                {
                    
                    sb.AppendFormat(Text.CurrentBoss, bossName, bossCoords);
                    if (State.LootContainer != null)
                    {
                        sb.AppendFormat(Text.CurrentTaxBox, locator.GridReference(State.LootContainer, out moved));
                        sb.AppendLine();
                    }

                    if (State.Henchmen.Any())
                    {
                        sb.AppendLine("Henchmen:");
                        foreach (Henchman henchman in State.Henchmen.All())
                        {
                            string location = locator.GridReference(henchman.Player, out moved);
                            sb.AppendFormat("{0}", henchman.Player.displayName);
                            if (moved)
                                sb.AppendFormat(" on the move at {0}\n", location);
                            else
                                sb.AppendFormat(" up to no good at {0}\n", location);
                        }
                    }
                    else
                        sb.AppendLine("There are currenly no Henchmen");

                }
                else
                    sb.AppendFormat(Text.NoBoss);               
            }

            PrintToChat(player, sb.ToString());
        }

        // ClaimCommon implements the "/claim" player command. This allows any player to become
        // the Boss, if there currently is none. Command has no effect if is already a Boss.
        [ChatCommand("claim")]
        void ClaimCommand(BasePlayer player, string command, string[] args)
        {
            if (GuardAgainst(() => State.IsBoss(player), player, Text.Error_YourAreBoss)
            || GuardAgainst(() => State.BossExists(), player, Text.Error_OtherIsBoss, State.BossName))
                return;

            State.SetBoss(player);  
            PrintToChat(player, Text.Success_WelcomeNewBoss);
            PrintToChat(Text.Broadcast_FirstBoss, State.BossName);
        }

        // LootCommand implements the "/loot" player command. This command allows the Boss
        // to designate which storage box he wants his taxes paying into. 
        [ChatCommand("loot")]
        void LootCommand(BasePlayer player, string command, string[] args)
        {
            if(GuardAgainst(() => State.NoBoss(), player, Text.Error_Loot_NoBoss)
            || GuardAgainst(() => State.IsNotBoss(player), player, Text.Error_Loot_NotBoss, State.BossName))
                return;

            RaycastHit hit;
            StorageContainer targetBox = null;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 2.5f))
                targetBox = hit.transform.GetComponentInParent<StorageContainer>();

            if (targetBox != null && targetBox.GetType() == typeof(StorageContainer))
            {
                State.LootContainer = targetBox;
                PrintToChat(player, Text.Success_Looting);
            }
            else
                PrintToChat(player, "Stand close to, and look directly at, the storage box you want your taxes paying into.");
        }

        [ChatCommand("helo")]
        void HeloCommmand(BasePlayer player, string command, string[] args)
        {
            if (GuardAgainst(() => State.IsNotBoss(player), player, "You aren't the boss")
             || GuardAgainst(() => args.Length != 1, player, "Usage '/helo player' where player can also be partial name")
             || GuardAgainst(() => !State.Outlaws.HasMatchByPartialName(args[0]), player, "player \"{0}\" not found, or ambiguous", args[0])
             || GuardAgainst(() => State.CanNotAffordHeloStrike(player), player, "Ordering a helo strike costs {0} {1}", GameConfig.HeloStrikePrice_Quantity, GameConfig.HeloStrikePrice_ItemDef.displayName.english))
                return;

            int heloCount = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>().Count();
            if(heloCount >= GameConfig.MaxHelos)
            {
                PrintToChat(player, "Insufficient airspace for more than {0} helicopters, please wait for extant patrols to complete", GameConfig.MaxHelos);
                return;
            }

            State.OrderHeloStrike(args[0]);
            PrintToChat(player, "The helo is inbound");
        }

        [ChatCommand("decoy")]
        void DecoyCommmand(BasePlayer player, string command, string[] args)
        {
            if (GuardAgainst(() => !State.Outlaws.Contains(player), player, "You aren't an outlaw")
             || GuardAgainst(() => args.Length != 1, player, "Usage '/decoy player' where player can also be partial name")
             || GuardAgainst(() => State.PlayerGoodMatch(args[0]), player, "player \"{0}\" not found, or ambiguous", args[0])
             || GuardAgainst(() => !State.CanAffordDecoy(player), player, "Decoying costs {0} {1}", GameConfig.DecoyPrice_Quantity, GameConfig.DecoyPrice_ItemId.displayName.english))
                return;

            State.Decoy(player, args[0]);
            PrintToChat(player, "You now have a decoy. Only you know this");
        }

        [ChatCommand("tax")]
        void SetTaxCommmand(BasePlayer player, string command, string[] args)
        {
            if (GuardAgainst(() => State.NoBoss(), player, Text.Error_TaxChange_NoBoss)
             || GuardAgainst(() => State.IsNotBoss(player), player, Text.Error_TaxChange_NotBoss, State.BossName)
             || GuardAgainst(() => args.Length != 1, player, Text.Error_TaxChange_BadArgs))
                return;

            float newTaxRate = 0;

            if (float.TryParse(args[0], out newTaxRate))
            {
                newTaxRate /= 100;
                newTaxRate = Math.Max(0.03f, Math.Min(0.45f, newTaxRate));

                bool increased = (newTaxRate > State.TaxRate);

                State.TaxRate = newTaxRate;

                if (increased)
                    PrintToChat(Text.Broadcast_TaxIncrease, State.BossName, State.TaxRate * 100);
                else
                    PrintToChat(Text.Broadcast_TaxDecrease, State.BossName, State.TaxRate * 100);
            }
            else
                PrintToChat(player, Text.Error_TaxChange_BadArgs);
        }

        [ChatCommand("where")]
        void WhereCommand(BasePlayer player, string command, string[] args)
        {
            bool moved;
            PrintToChat(player, Text.YourLocation, liveLocator.GridReference(player, out moved));
        }

        [ChatCommand("rebel")]
        void RebelCommand(BasePlayer player, string command, string[] args)
        {
            if(GuardAgainst(() => State.IsBoss(player), player, Text.Error_Rebel_IsTheBoss)
            || GuardAgainst(() => State.Outlaws.Contains(player), player, Text.Error_Rebel_IsAlreadyOutlaw))
                return;

            State.Outlaws.Add(player);
            PrintToChat(Text.Broadcast_NewOutlaw, player.displayName);
        }

        [ChatCommand("recruit")]
        void RecruitCommand(BasePlayer player, string command, string[] args)
        {
            if(GuardAgainst(() => !State.IsBoss(player), player, "You need to be the Boss to recruit henchmen")
             || GuardAgainst(() => args.Length != 1, player, "Usage '/recruit player' where player can also be partial name")
             || GuardAgainst(() => State.PlayerGoodMatch(args[0]), player, "player \"{0}\" not found, or ambiguous", args[0]))
                return;

            BasePlayer recruit = State.TryRecruit(args[0]);
            if(recruit != null)
            {
                PrintToChat(State.Boss, "You have invited {0} to undergo the Bukake Ritual", recruit.displayName);
                PrintToChat(recruit, "Boss {0} offers you employment, it's up to you to <color=lime>/accept</color> it", State.BossName);
            }
        }

        [ChatCommand("accept")]
        void AcceptCommand(BasePlayer player, string command, string[] args)
        {
            if (GuardAgainst(() => !State.PendingRequest.Contains(player), player, "You have no pending recruitment requests"))
                return;

            if(State.TryPromote(player))
                PrintToChat(
                    "{0} passed the Bukake Ritual, and is now a henchman for {1}. Someone pass him a tissue", player.displayName, State.Boss.displayName);
        }

        #endregion

        #region Console Commands
        [ConsoleCommand("devilsisland.diagnostic")]
        private void DiagnosticCommand(ConsoleSystem.Arg arg)
        {
            PrintToConsole(arg.Player(), "Devil's Island version 0.7 ({0}.{1}.{2})", Version.Major, Version.Minor, Version.Patch);

            PrintToConsole(arg.Player(), "\nHenchmen:");
            foreach (Henchman h in State.Henchmen.All())
                PrintToConsole(arg.Player(), "\t{0}", h.Player.displayName);

            PrintToConsole(arg.Player(), "\n\nOutlaws:");
            foreach (Outlaw h in State.Outlaws.All())
                PrintToConsole(arg.Player(), "\t{0}", h.Player.displayName);
        }

        [ConsoleCommand("devilsisland.reset")]
        private void ResetCommand(ConsoleSystem.Arg arg)
        {
            if (arg.isAdmin)
            {
                PrintToConsole(arg.Player(), "Resetting Devil's Island Game State");
                State.SetBoss(null);
            }
        }
        #endregion

        #region Tax Resource Gathering
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null || State.IsBoss(player) || State.Outlaws.Contains(player) || State.Henchmen.Contains(player))
                return;

            State.CollectTaxFrom(item);
        }

        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            State.CollectTaxFrom(item);
        }

        #endregion

        #region Player Death

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            // Check to see if the LootContainer got destroyed
            StorageContainer container = entity as StorageContainer;
            if (container != null && State.LootContainer == container)
            {
                State.LootContainer = null;
                return;
            }
            
            BasePlayer victim = entity.ToPlayer();
            if (victim == null)
                return;

            BasePlayer attacker = null;
            if (info != null && info.Initiator != null)
                attacker = info.Initiator.ToPlayer();

            // Handle Boss Death
            if (State.IsBoss(victim))
            {
                // Killed by Player?
                if (attacker != null)
                {
                    if (State.IsBoss(attacker))
                    {
                        State.SetBoss(null);
                        PrintToChat(Text.Broadcast_BossSuicide, attacker.displayName);
                    }
                    else
                    {
                        State.SetBoss(attacker);
                        PrintToChat(Text.Broadcast_NewBoss, State.BossName, victim.displayName);
                    }
                }
                else
                {
                    PrintToChat("Boss {0} died foolishly. You can /claim the title.", State.BossName);
                    State.SetBoss(null);
                }
                return;
            }

            // Handle Outlaw Death
            if(State.Outlaws.Contains(victim) && State.IsBoss(attacker))
            {
                State.Outlaws.Remove(victim);
                PrintToChat("Boss {0} executed outlaw {1}. He died like the rebel dog he was.", State.BossName, victim.displayName);
                return;
            }

            // Handle Outlaw Death
            if (State.Outlaws.Contains(victim) && State.Henchmen.Contains(attacker))
            {
                State.Outlaws.Remove(victim);
                PrintToChat("Henchman {0} made the world a better place: outlaw {1} is worm food.", attacker.displayName, victim.displayName);
                return;
            }

            // Spice up murder, a little bit
            if(!State.Outlaws.Contains(victim) && !State.IsBoss(victim) && !State.Henchmen.Contains(victim)
                && attacker != null && !State.IsBoss(attacker) && !State.Outlaws.Contains(attacker) && !State.Henchmen.Contains(attacker)
                && attacker != victim)
            {
                State.Outlaws.Add(attacker);
                PrintToChat("{0} has embarked on a life of crime, and is now an Outlaw.", attacker.displayName);
                return;
            }
        }

        #endregion

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if(player == State.Boss && !GameConfig.AllowBossDisconnect)
            {
                PrintToChat("The Boss has been sacked for sleeping on the job, feel free to /claim it");
                State.SetBoss(null);
            }
        }

        #region Integration Point
        public BasePlayer Boss
        {
            get { return State.Boss; }
        }
        #endregion
    }
}

namespace DevilsIsland
{    
    public class DevilsIslandState : IGameState
    {
        public DevilsIslandState()
        {
            Outlaws = new Outlaws();
            Henchmen = new Henchmen();
            PendingRequest = new List<BasePlayer>();
        }
        
        public void AttachConfig(IGameConfig configFile)
        {
            this.config = (DevilsIslandConfig)configFile;
        }

        private DevilsIslandConfig config = null;
        
        private BasePlayer currentBoss = null;
        internal DateTime noBossSince = DateTime.Now;

        private float taxRate = 0.1f;
        private Dictionary<ItemDefinition, float> coffers = new Dictionary<ItemDefinition, float>();

        // Some of the method here might seem redundant (i.e. IsBoss, IsNotBoss), but their
        // purpose is to make the code more readable.

        #region Boss

        public bool NoBoss()
        {
            return currentBoss == null;
        }

        public bool BossExists()
        {
            return currentBoss != null;
        }

        public bool IsBoss(BasePlayer player)
        {
            return player == currentBoss;
        }

        public bool IsNotBoss(BasePlayer player)
        {
            return player != currentBoss;
        }

        public BasePlayer Boss { get { return currentBoss; } }

        public string BossName { get { return currentBoss == null ? "no boss" : currentBoss.displayName;  } }

        public void SetBoss(BasePlayer newBoss)
        {
            if(currentBoss != newBoss)
            {
                currentBoss = newBoss;
                if (currentBoss == null)
                    noBossSince = DateTime.Now;
                Outlaws.Clear();

                PendingRequest.Clear();

                foreach (Henchman hench in Henchmen.All())
                    hench.Player.inventory.Strip();

                Henchmen.Clear();
                LootContainer = null;
            }
        }

        public bool TryForceNewBoss()
        {
            if (BossExists() || BasePlayer.activePlayerList.Count() < config.AutoBossPromoteMinPlayers)
                return false;

            if(DateTime.Now > noBossSince.AddSeconds(config.AutoBossPromoteDelay))
            {                
                int newBossIndex = Oxide.Core.Random.Range(BasePlayer.activePlayerList.Count());
                SetBoss(BasePlayer.activePlayerList.Skip(newBossIndex).First());
                return true;
            }

            return false;
        }

        #endregion

        #region Tax

        public float TaxRate { get { return taxRate; } set { taxRate = value; } }

        public StorageContainer LootContainer { get; set; }

        public IEnumerable<ItemDefinition> CofferItems { get { return coffers.Keys.Where(k => coffers[k] >= 1); } }

        public float CofferAmount(ItemDefinition key)
        {
            return coffers[key];
        }

        public void CollectTaxFrom(Item item)
        {
            // Calculate the tax as floats, so we still get something when item amount is low.
            float taxAmount = ((float)item.amount * taxRate);

            item.amount -= (int)taxAmount;

            if (!coffers.ContainsKey(item.info))
                coffers.Add(item.info, 0);

            coffers[item.info] += taxAmount;

            int intAmount = (int)coffers[item.info];
            if(intAmount >= 1 && LootContainer != null)
            {
                Item taxItem = ItemManager.CreateByItemID(item.info.itemid, intAmount, false);
                taxItem.MoveToContainer(LootContainer.inventory);
                coffers[item.info] -= (float)intAmount;
            }

        }
        #endregion

        #region Helo Strikes

        public bool CanNotAffordHeloStrike(BasePlayer player)
        {
            return player.inventory.GetAmount(config.HeloStrikePrice_ItemDef.itemid) < config.HeloStrikePrice_Quantity;
        }

        public void OrderHeloStrike(string targetPartialName)
        {
            // Deduct the cost
            List<Item> collector = new List<Item>();
            currentBoss.inventory.Take(collector, config.HeloStrikePrice_ItemDef.itemid, config.HeloStrikePrice_Quantity);

            // Call in the whirlibird
            Outlaw target;
            BaseEntity entity = GameManager.server.CreateEntity("assets/bundled/prefabs/npc/patrol_helicopter/PatrolHelicopter.prefab", new Vector3(), new Quaternion(), true);
            if (entity != null && Outlaws.TryResolveByPartialName(targetPartialName, out target))
            {
                entity.GetComponent<PatrolHelicopterAI>().SetInitialDestination(target.GetEffectiveTarget().transform.position + new Vector3(0.0f, 10f, 0.0f), 0.25f);
                entity.Spawn(true);
            }
        }

        #endregion

        #region Outlaws

        public Outlaws Outlaws { get; private set; }

        public bool CanAffordDecoy(BasePlayer player)
        {
            return player.inventory.GetAmount(config.DecoyPrice_ItemId.itemid) >= config.DecoyPrice_Quantity;
        }

        public void Decoy(BasePlayer player, string targetPartialName)
        {
            // Deduct the cost
            List<Item> collector = new List<Item>();
            player.inventory.Take(collector, config.DecoyPrice_ItemId.itemid, config.DecoyPrice_Quantity);

            // Set up the decoy
            BasePlayer target = BasePlayer.activePlayerList.Single(p => p.displayName.IndexOf(targetPartialName, StringComparison.InvariantCultureIgnoreCase) != -1);

            Outlaw source;
            if(Outlaws.TryResovleByPlayer(player, out source))
                source.SetDecoy(target, DateTime.Now.AddSeconds(300));
        }
        #endregion

        #region Henchmen

        public Henchmen Henchmen { get; private set; }
        public List<BasePlayer> PendingRequest { get; private set; }

        public BasePlayer TryRecruit(string playerPartialName)
        {
            if (BasePlayer.activePlayerList.Count(p => p.displayName.IndexOf(playerPartialName, StringComparison.InvariantCultureIgnoreCase) != -1) != 1)
                return null;

            BasePlayer recruit = BasePlayer.activePlayerList.Single(p => p.displayName.IndexOf(playerPartialName, StringComparison.InvariantCultureIgnoreCase) != -1);

            if (recruit == Boss || PendingRequest.Contains(recruit) || Henchmen.Contains(recruit))
                return null;

            PendingRequest.Add(recruit);
            return recruit;
        }

        public bool TryPromote(BasePlayer player)
        {
            if (PendingRequest.Contains(player))
                PendingRequest.Remove(player);

            if (Outlaws.Contains(player))
                Outlaws.Remove(player);

            if (!Henchmen.Contains(player))
            {
                Henchmen.Add(player);
                return true;
            }
            return false;
        }

        #endregion

        public bool PlayerGoodMatch(string playerPartialName)
        {
            return BasePlayer.activePlayerList.Count(p => p.displayName.IndexOf(playerPartialName, StringComparison.InvariantCultureIgnoreCase) != -1) != 1;
        }
    }

    #region Outlaws
    public class Outlaws
    {
        List<Outlaw> outlaws = new List<Outlaw>();

        public void Clear()
        {
            outlaws.Clear();
        }

        public void Add(BasePlayer newOutlaw)
        {
            outlaws.Add(new Outlaw(newOutlaw));
        }
        
        public void Remove(BasePlayer oldOutlaw)
        {
            Outlaw itemToRemove = outlaws.SingleOrDefault(o => o.Player == oldOutlaw);
            if (itemToRemove != null)
                outlaws.Remove(itemToRemove);
        }

        public bool Contains(BasePlayer player)
        {
            return outlaws.Any(o => o.Player == player);
        }

        public bool Any()
        {
            return outlaws.Any();
        }

        public IEnumerable<Outlaw> All()
        {
            return outlaws.AsReadOnly();
        }

        public bool TryResovleByPlayer(BasePlayer player, out Outlaw matchingOutlaw)
        {
            matchingOutlaw = outlaws.SingleOrDefault(o => o.Player == player);
            return (matchingOutlaw != null);
        }

        public bool HasMatchByPartialName(string partialName)
        {
            Outlaw match;
            return TryResolveByPartialName(partialName, out match);
        }

        public bool TryResolveByPartialName(string partialName, out Outlaw matchingOutlaw)
        {
            matchingOutlaw = outlaws.SingleOrDefault(o => o.Player.displayName.IndexOf(partialName, StringComparison.InvariantCultureIgnoreCase) != -1);
            return (matchingOutlaw != null);
        }
    }

    public class Outlaw
    {
        public Outlaw(BasePlayer player)
        {
            Player = player;
            decoyTarget = null;
            decoyUntil = DateTime.MinValue;
        }

        public BasePlayer Player { get; private set; }

        private BasePlayer decoyTarget;
        private DateTime decoyUntil;
        
        public void SetDecoy(BasePlayer player, DateTime until)
        {
            decoyTarget = player;
            decoyUntil = until;
        }

        public BasePlayer GetEffectiveTarget()
        {
            if (decoyTarget != null && decoyUntil >= DateTime.Now)
            {
                return decoyTarget;
            }
            else
                return Player;
        }
    }
    #endregion

    #region Henchmen
    public class Henchmen
    {
        List<Henchman> henchmen = new List<Henchman>();

        public void Clear()
        {
            henchmen.Clear();
        }

        public void Add(BasePlayer newHenchman)
        {
            henchmen.Add(new Henchman(newHenchman));
        }

        public void Remove(BasePlayer oldHenchman)
        {
            Henchman itemToRemove = henchmen.SingleOrDefault(o => o.Player == oldHenchman);
            if (itemToRemove != null)
                henchmen.Remove(itemToRemove);
        }

        public bool Contains(BasePlayer player)
        {
            return henchmen.Any(o => o.Player == player);
        }

        public bool Any()
        {
            return henchmen.Any();
        }

        public IEnumerable<Henchman> All()
        {
            return henchmen.AsReadOnly();
        }

        public bool TryResovleByPlayer(BasePlayer player, out Henchman matchingHenchman)
        {
            matchingHenchman = henchmen.SingleOrDefault(o => o.Player == player);
            return (matchingHenchman != null);
        }

        public bool HasMatchByPartialName(string partialName)
        {
            Henchman match;
            return TryResolveByPartialName(partialName, out match);
        }

        public bool TryResolveByPartialName(string partialName, out Henchman matchingHenchman)
        {
            matchingHenchman = henchmen.SingleOrDefault(o => o.Player.displayName.IndexOf(partialName, StringComparison.InvariantCultureIgnoreCase) != -1);
            return (matchingHenchman != null);
        }
    }

    public class Henchman
    {
        public Henchman(BasePlayer player)
        {
            Player = player;
        }

        public BasePlayer Player { get; private set; }
    }

    #endregion

    public class DevilsIslandConfig : IGameConfig
    {
        public void AttachConfigFile(DynamicConfigFile configFile)
        {
            this.configFile = configFile;
        }
        
        private DynamicConfigFile configFile;

        public void UpdateConfigFile()
        {
            bool changed = false;
            changed |= DefaultValue("HelpNotifierEnabled", true);
            changed |= DefaultValue("HelpNotifierInverval", 300);
            changed |= DefaultValue("BossPositionNotifierEnabled", true);
            changed |= DefaultValue("BossPositionNotifierInterval", 90);
            changed |= DefaultValue("AutoBossPromoteDelay", 300);
            changed |= DefaultValue("AutoBossPromoteMinPlayers", 5);
            changed |= DefaultValue("HeloStrikePrice_Quantity", 25);
            changed |= DefaultValue("HeloStrikePrice_Item", "metal.refined");
            changed |= DefaultValue("MaxHelos", 2);
            changed |= DefaultValue("DecoyPrice_Quantity", 5);
            changed |= DefaultValue("DecoyPrice_Item", "metal.refined");
            changed |= DefaultValue("EvadePrice_Quantity", 100);
            changed |= DefaultValue("EvadePrice_Item", "leather");
            changed |= DefaultValue("FallbackWorldSize", 4000);
            changed |= DefaultValue("AllowBossDisconnect", false);

            if (changed)
                configFile.Save();
        }

        private bool DefaultValue(string key, object defaultValue)
        {
            if (configFile[key] == null)
            {
                configFile[key] = defaultValue;
                return true;
            }
            else
                return false;
        }

        public bool IsHelpNotiferEnabled { get { return (bool)configFile["HelpNotifierEnabled"]; } }
        public int HelpNotifierInverval { get { return (int)configFile["HelpNotifierInverval"]; } }
        
        public bool IsBossPositionNotifierEnabled { get { return (bool)configFile["BossPositionNotifierEnabled"]; } }
        public int BossPositionNotifierInterval { get { return (int)configFile["BossPositionNotifierInterval"]; } }
        
        public int AutoBossPromoteDelay { get { return (int)configFile["AutoBossPromoteDelay"]; } }
        public int AutoBossPromoteMinPlayers { get { return (int)configFile["AutoBossPromoteMinPlayers"]; } }

        private ItemDefinition heloStrikePriceItemDef;
        public ItemDefinition HeloStrikePrice_ItemDef
        {
            get
            {
                if (heloStrikePriceItemDef == null) heloStrikePriceItemDef = FindOrDefault((string)configFile["HeloStrikePrice_Item"], "metal.refined");
                return heloStrikePriceItemDef;
            }
        }
        public int HeloStrikePrice_Quantity { get { return (int)configFile["HeloStrikePrice_Quantity"]; } }

        public int MaxHelos { get { return (int)configFile["MaxHelos"]; } }

        private ItemDefinition decoyPriceItemDef;
        public ItemDefinition DecoyPrice_ItemId
        {
            get
            {
                if (decoyPriceItemDef == null) decoyPriceItemDef = FindOrDefault((string)configFile["DecoyPrice_Item"], "metal.refined");
                return decoyPriceItemDef;
            }
        }
        public int DecoyPrice_Quantity { get { return (int)configFile["DecoyPrice_Quantity"]; } }

        private ItemDefinition evadePriceItemDef;
        public ItemDefinition EvadePrice_ItemId
        {
            get
            {
                if (evadePriceItemDef == null) evadePriceItemDef = FindOrDefault((string)configFile["EvadePrice_Item"], "leather");
                return evadePriceItemDef;
            }
        }
        public int EvadePrice_Quantity { get { return (int)configFile["EvadePrice_Quantity"]; } }

        public int FallbackWorldSize { get { return (int)configFile["FallbackWorldSize"]; } }

        private static ItemDefinition FindOrDefault(string itemName, string fallbackName)
        {
            ItemDefinition def = ItemManager.FindItemDefinition(itemName);
            if (def == null) def = ItemManager.FindItemDefinition(fallbackName);
            return def;
        }

        public bool AllowBossDisconnect { get { return (bool)configFile["AllowBossDisconnect"];  } }
    }

    #region Player Grid Coordinates and Locators
    public interface ILocator
    {
        string GridReference(Component component, out bool moved);
    }

    public class RustIOLocator : ILocator
    {
        public RustIOLocator(int worldSize)
        {
            translate = worldSize / 2f;
            scale = worldSize / 26f;
        }

        private readonly float translate;
        private readonly float scale;

        public string GridReference(Component component, out bool moved)
        {
            var pos = component.transform.position;
            float x = pos.x + translate;
            float z = pos.z + translate;

            int lat = (int)Math.Floor(x / scale);
            char latChar = (char)('A' + lat);
            int lon = 26 - (int)Math.Floor(z / scale);

            moved = false; // We dont know, so just return false
            return string.Format("{0}{1}", latChar, lon);
        }
    }

    public class LocatorWithDelay : ILocator
    {
        public LocatorWithDelay(ILocator liveLocator, int updateInterval)
        {
            this.liveLocator = liveLocator;
            this.updateInterval = updateInterval;
        }

        private readonly ILocator liveLocator;
        private readonly int updateInterval;
        private readonly Dictionary<Component, ExpiringCoordinates> locations = new Dictionary<Component, ExpiringCoordinates>();

        public string GridReference(Component component, out bool moved)
        {
            ExpiringCoordinates item = null;
            bool m;

            if(locations.ContainsKey(component))
            {
                item = locations[component];
                if (item.Expires < DateTime.Now)
                {
                    string location = liveLocator.GridReference(component, out m);
                    item.GridChanged = item.Location != location;
                    item.Location = location;
                    item.Expires = DateTime.Now.AddSeconds(updateInterval);
                }
            }
            else
            {
                item = new ExpiringCoordinates();
                item.Location = liveLocator.GridReference(component, out m);
                item.GridChanged = true;
                item.Expires = DateTime.Now.AddSeconds(updateInterval);
                locations.Add(component, item);
            }

            moved = item.GridChanged;
            return item.Location;
        }
        
        class ExpiringCoordinates
        {
            public string Location { get; set; }
            public bool GridChanged { get; set; }
            public DateTime Expires { get; set; }
        }
    }
    #endregion

    public static class Text
    {
        public const string NoBoss = "There is no Boss - you can <color=lime>/claim</color> it\n";
        public const string YouAreTheBoss = "<color=red><size=17>You are the Boss</size></color>";
        public const string YouAreAnOutlaw = "<color=red><size=17>You are an Outlaw</size></color>";
        public const string CurrentBoss = "Current Boss: {0} at {1}\n";
        public const string CurrentTaxBox = "You can raid his tax box at {0}";
        public const string TaxRate = "Tax Rate: {0}%\n";
        public const string CofferItem = "\t{0}: {1}\n";
        public const string YourLocation = "You are at {0}";

        public const string Synopsis = "Devil's Island is controlled by one Boss. The Boss taxes gathering of resources, and everyone either pays up or kills him, or becomes an Outlaw";
        public const string PlayerCommandSection = "Player Commands:";
        public const string StatusCommandHint = "<color=lime>/status</color> displays your current status";
        public const string ClaimCommandHint = "<color=lime>/claim</color> if there is no Boss, you can take the job";
        public const string RebelCommandHint = "<color=lime>/rebel</color> makes you an outlaw - you don't pay tax, but the Boss can find you";
        public const string WhereCommandHint = "<color=lime>/where</color> displays your coordinates using RustIO coordinates";
        public const string BossCommandSection = "Boss Commands:";
        public const string TaxCommandHint = "<color=lime>/tax n</color> sets the tax rate";
        public const string LootCommandHint = "<color=lime>/loot</color> while looking at a box. Collected taxes will then be paid into this box.";
        public const string HeloStrikeCommandHint = "<color=lime>/helo <i>player</i></color> send a helo to <i>player</i>'s location. Costs {0} {1}";
        public const string RecruitCommandHint = "<color=lime>/recruit <i>player</i></color> invite <i>player</i> to be one of your henchmen";

        public const string Broadcast_ClaimAvailable = "No one is the Boss, use <color=lime>/claim</color> to become the Boss.";
        public const string Broadcast_HelpAdvice = "Confused? Type <color=lime>/rules</color> for help.";

        public const string Error_YourAreBoss = "You're already the Boss, dumbass";
        public const string Error_OtherIsBoss = "Kill the Boss ({0}) to become the Boss...";
        public const string Success_WelcomeNewBoss = "You are the Boss, for now. Use <color=lime>/loot</color> set set your tax storage box, and /tax to set the tax rate.";
        public const string Broadcast_FirstBoss = "{0} is now the new Boss. Kill him to become the Boss";

        public const string Error_Loot_NoBoss = "If you want the loot, use <color=lime>/claim</color> to become the Boss.";
        public const string Error_Loot_NotBoss = "You aren't the Boss. Kill {0} if you want the loot.";
        public const string Success_Looting = "Taxes will now be paid into this storage box.";

        public const string Error_TaxChange_NoBoss = "If you want to change the tax rate, use <color=lime>/claim</color> to become the Boss";
        public const string Error_TaxChange_NotBoss = "You aren't the Boss. Kill {0} if you want to change the tax rate";
        public const string Error_TaxChange_BadArgs = "usage: <color=lime>/tax n</color>, where n is between 3 and 45 percent.";
        public const string Broadcast_TaxIncrease = "The evil bastard {0} has increased your taxes to {1}%";
        public const string Broadcast_TaxDecrease = "{0} has lowered your taxes to {1}%";

        public const string Error_Rebel_IsTheBoss = "You're the Boss - you can't rebel against yourself!";
        public const string Error_Rebel_IsAlreadyOutlaw = "You're already on the Boss's shit list.";
        public const string Broadcast_NewOutlaw = "{0} no longer tolerates the Boss's greed, and refuses to pay tax. You can <color=lime>/rebel</color> too.";

        public const string Broadcast_BossSuicide = "Boss {0} could not take it any more. Use <color=lime>/claim</color> to become the new Boss";
        public const string Broadcast_NewBoss = "{0} killed {1} and is now the Boss. Kill him to become the Boss";

        public const string Broadcast_BossLocation_Moved = "Boss {0} is on the move, now at {1}.";
        public const string Broadcast_BossLocation_Static = "Boss {0} is camping out at {1}";
    }
}

namespace JetStream
{
    public class GameModePlugin<TConfig, TState> : RustPlugin
        where TConfig : IGameConfig, new()
        where TState : IGameState, new()
    {
        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        protected Dictionary<string, Timer> Timers { get { return timers;  } }

        protected TConfig GameConfig { get; private set; }
        protected TState State { get; private set; }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating default configuration file");
            GameConfig = new TConfig();
            GameConfig.AttachConfigFile(this.Config);
            GameConfig.UpdateConfigFile();
        }

        [HookMethod("OnServerInitialized")]
        void base_OnServerInitialized()
        {
            if (GameConfig == null)
            {
                GameConfig = new TConfig();
                GameConfig.AttachConfigFile(this.Config);
                GameConfig.UpdateConfigFile();
            }

            State = new TState();  
            State.AttachConfig(GameConfig);

            Initialize();
        }

        [HookMethod("Unload")]
        void base_Unload()
        {
            Puts("Unload called");
            
            foreach (Timer t in timers.Values)
                t.Destroy();

            timers.Clear();
        }

        protected virtual void Initialize()
        { }

        protected bool GuardAgainst(Func<bool> condition, BasePlayer player, string errorMsgFormat, params object[] args)
        {
            if (condition())
            {
                PrintToChat(player, errorMsgFormat, args);
                return true;
            }
            else
                return false;
        }
    }

    public interface IGameConfig
    {
        void AttachConfigFile(DynamicConfigFile configFile);
        void UpdateConfigFile();
    }

    public interface IGameState
    {
        void AttachConfig(IGameConfig configFile);
    }

}