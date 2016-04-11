// Reference: Newtonsoft.Json
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Text;
using Oxide.Core.Libraries;
using Oxide.Plugins;
using System.Collections;
using System.Text.RegularExpressions;
using Random = System.Random;
using Quests;

namespace Oxide.Plugins
{
    [Info("Quests", "ShadowEvil", "1.0.6", ResourceId = 1084)]
    public class QuestPlugin : RustPlugin
    {
        private VersionNumber DataVersion;
        private DynamicConfigFile PlayerDataFile = new DynamicConfigFile();

        public static Dictionary<string, QuestInfo> PlayerQuestInfo = new Dictionary<string, QuestInfo>();
        public static List<KeyValuePair<string, Quest>> quests = new List<KeyValuePair<string, Quest>>();

        public double rateOfChange = 0.10;
        public double LastPercentChange = 0.00;

        public QuestPlugin()
        {
            DataVersion = new VersionNumber(1, 0, 6);
        }

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            LoadQuests();
        }

        [HookMethod("Unload")]
        void Unload()
        {
            SaveQuests();
            Log("Saved");
        }

        [HookMethod("Loaded")]
        void Loaded()
        {
            PrintToChat(String.Format("<color=red>Quests</color> by <color=lightblue>ShadowEvil</color> has been loaded. Version: <color=aqua>{0}</color>\n\t\t\t " +
                                    "Type: <color=lightblue>/quests</color> for more information on quests!", DataVersion));
            LoadPlayerQuestData();
        }

        [HookMethod("OnPlayerDisconnected")]
        void OnPlayerDisconnected(BasePlayer player)
        {
            SaveQuests();
        }

        [HookMethod("PlayerInit")]
        void OnPlayerInit(BasePlayer player)
        {
            QuestINFO(player);
            SaveQuests();
        }

        [HookMethod("OnEntityDeath")]
        void OnAnimalDeath(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (hitinfo == null) return;
            if (hitinfo.Initiator == null) return;
            if (hitinfo.Initiator.ToPlayer() == null) return;
            if (entity.GetComponent<BaseNPC>() == null) return;
            BasePlayer player = hitinfo.Initiator.ToPlayer();
            string animalName = entity.GetComponent<BaseNPC>().corpseEntity.Substring(12).Split('_')[0].ToLower();
            hasPlayerFinishedQuest_KillAnimal(player, animalName);
        }

        [HookMethod("OnGather")]
        void OnGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            hasPlayerFinishedQuest_Gather(player, entity, item);
        }

        [ChatCommand("quests")]
        void cmdChatQuest(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            cmdChatQuest(player, args);
        }

        [HookMethod("OnServerSave")]
        void OnServerSave()
        {
            SaveQuests();
            Log("Quest progress saved!");
        }

        public void Log(string message)
        {
            Puts(String.Format("QuestPlugin: {0}", message));
        }

        public void LoadQuests()
        {
            LoadConfig();
            ClearQuests();
            if (Config["quests"] != null)
            {
                var qDataTmp = (Dictionary<string, object>)Convert.ChangeType(Config["quests"], typeof(Dictionary<string, object>));
                foreach (var iQuest in qDataTmp)
                {
                    string name = iQuest.Key;
                    var qData = iQuest.Value as Dictionary<string, object>;
                    string description = Convert.ToString(qData["description"]);
                    
                    string objective = Convert.ToString(qData["Objective"]);
                    int amount = Convert.ToInt32(qData["Amount"]);
                    string reward = Convert.ToString(qData["Reward"]);
                    int rewardamount = Convert.ToInt32(qData["RewardAmount"]);
                    string type = Convert.ToString(qData["Type"]);
                    Quest q;
                    quests.Add(new KeyValuePair<string, Quest>(name, q = new Quest()
                    {
                        qName = name,
                        qDescription = description,
                        qReward = reward,
                        qRewardAmount = rewardamount,
                        qType = type,
                        qObjective = objective,
                        qAmount = amount
                    }));
                }
            }
        }

        public void SaveQuests()
        {
            SavePlayerQuestData();
        }

        public void SavePlayerQuestData()
        {
            PlayerDataFile["Profiles"] = PlayerQuestInfo;
            Interface.GetMod().DataFileSystem.SaveDatafile("Quests_PlayerData");
            //Interface.Oxide.DataFileSystem.WriteObject<Dictionary<string, QuestInfo>>("Quests_PlayerData", PlayerQuestInfo);
        }

        public void LoadPlayerQuestData()
        {
            PlayerDataFile = Interface.GetMod().DataFileSystem.GetDatafile("Quests_PlayerData");
            var PlayerQuests = ReadFromData<Dictionary<string, QuestInfo>>("Profiles") ?? new Dictionary<string, QuestInfo>();
            PlayerQuestInfo = PlayerQuests;
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                QuestINFO(player);
            }
        }

        public T ReadFromData<T>(string dataKey)
        {
            string serializeObject = JsonConvert.SerializeObject(PlayerDataFile[dataKey]);
            return JsonConvert.DeserializeObject<T>(serializeObject);
        }

        public void cmdChatQuest(BasePlayer player, string[] args)
        {
            string t = "";
            if (args.Length == 0)
            {
                // Default information
                t += "Quest commands: \n" +
                       "\t\t <color=lightblue>/quests list</color>\n\t\t\t Displays the quests available to you.\n" +
                       "\t\t <color=lightblue>/quests accept \"questname\"</color>\n\t\t\t Accepts the quest based on the quest name.\n" +
                       "\t\t <color=lightblue>/quests active</color>\n\t\t\t Displays the active quest.\n" +
                       "\t\t <color=lightblue>/quests abandon \"questname\"</color>\n\t\t\t Abandons the quest based on the quest name.\n" +
                       "\t\t <color=lightblue>/quests progress</color>\n\t\t\t Shows the progress of your current quests!\n" +
                       "\t\t <color=lightblue>/quests showprogress <number></color>\n\t\t\t Changes the progress shown percentage.";
            }
            else
            {
                switch (args[0])
                {
                    case "list":
                        ShowQuestList(player, args, ref t);
                        break;
                    case "accept":
                        AcceptQuest(player, args, ref t);
                        break;
                    case "active":
                        UserActiveQuests(player, args, ref t);
                        break;
                    case "abandon":
                        AbandonQuest(player, args, ref t);
                        break;
                    case "progress":
                        ShowQuestProgress(player, args, ref t);
                        break;
                    case "showprogress":
                        ChangeQuestProgress(player, args, ref t);
                        break;
                }
            }
            if (t.Length < 1) return;
            Notification(player, t);
        }
        #region ChatFunctions
        public void ChangeQuestProgress(BasePlayer player, string[] args, ref string t)
        {
            if (args.Length != 2)
            {
                t += "<color=lightblue>Usage: </color> /quests showprogress <number>\n\t\t This will change how often you see quest progress updates.\n\t\t <color=yellow>i.e. \"/quests showprogress 25\"</color>\n\t\t Will show amount remaining until finishing of quest.";
            }
            else
            {
                int a = -1;
                try
                {
                    a = Convert.ToInt32(args[1]);
                }
                catch(Exception)
                {
                    t += "Error, you must use numbers, not letters!";
                    return;
                }
                QuestInfo qI = QuestINFO(player);
                qI.progressPercent = a;
                rateOfChange = (double)a / 100;
                LastPercentChange = 0.00;
                t += string.Format("Rate of change set to <color=red>{0}</color>", rateOfChange.ToString("0.##%"));
                SaveQuests();
            }
        }

        public void ShowQuestProgress(BasePlayer player, string[] args, ref string t)
        {
            int pageIndex = 1;
            if (args.Length > 1)
            {
                try
                {
                    pageIndex = Convert.ToInt32(args[1]);
                }
                catch (Exception)
                {
                    t += "Error, you must use numbers, no letters... i.e. /quests progress 1";
                    return;
                }
            }
            t += "<color=lightblue>Quest Progress | Page " + pageIndex + "</color>\n";
            QuestInfo qI = QuestINFO(player);
            List<Quest> ActiveQuests = qI.ActiveQuests();
            if (ActiveQuests.Count < 1) { t += String.Format("You are currently not on a quest!"); return; }
            var pages = ConvertQuests(ActiveQuests).Skip(pageIndex * 5 - 5).Take(5);
            if (pages.Count() < 1) { t += "There are no active quests beyond this point."; return; }
            foreach (var q in pages)
            {
                int amount = 0;
                if (q.Value.qType == "gather") amount = GetGatheredAmount(q.Value, player);
                if (q.Value.qType == "kill") amount = GetKilledAmount(q.Value, player);
                t += String.Format("<color=red>{0}</color>\n\t\tCompleted Amount: {1} / {2}\t\t\tReward: {3} [x{4}]\n", ConvertToUpper(q.Value.qName), amount, q.Value.qAmount, q.Value.qReward, q.Value.qRewardAmount);
            }
        }

        public void UserActiveQuests(BasePlayer player, string[] args, ref string t)
        {
            int pageIndex = 1;
            if (args.Length > 1)
            {
                try
                {
                    pageIndex = Convert.ToInt32(args[1]);
                } catch(Exception)
                {
                    t += "Error, you must use numbers, no letters... i.e. /quests active 1";
                    return;
                }
            }
            t += "<color=lightblue>Active quest | Page " + pageIndex + "</color>\n";
            QuestInfo qI = QuestINFO(player);
            List<Quest> ActiveQuests = qI.ActiveQuests();
            if (ActiveQuests.Count < 1) { t += String.Format("You are currently not on a quest!"); return; }
            var pages = ConvertQuests(ActiveQuests).Skip(pageIndex * 5 - 5).Take(5);
            if (pages.Count() < 1) { t += "There are no active quests beyond this point."; return; }
            foreach (var q in pages)
                t += String.Format("<color=red>{0}</color>\n\t\t {1}\n\t\t <color=lightblue>Reward: </color><color=aqua>{2}</color>\n", ConvertToUpper(q.Value.qName), q.Value.qDescription, q.Value.qReward);
        }

        public void AcceptQuest(BasePlayer player, string[] args, ref string t)
        {
            Quest RequestQuest, myQuest;
            if (args.Length != 2)
            {
                t += "Usage: <color=lightblue>/quests accept \"questname\"</color>. Please try again.";
                return;
            }

            RequestQuest = FindQuest(args[1].ToLower());
            if (RequestQuest != null)
            {
                //t += String.Format("Requested Quest: {0}\n", ConvertToUpper(RequestQuest.qName));
                QuestInfo qI = QuestINFO(player);
                List<Quest> ActiveQuests = qI.ActiveQuests();
 /*               List<KeyValuePair<string, Quest>> aQuests = ActiveQuests(player.userID.ToString());
                if (aQuests.Count >= 1) { t += "You are not able to be on more than one quest at a time..."; return; }*/
                if (!ActiveQuests.Contains(RequestQuest))
                {
                    AddPlayerToQuest(RequestQuest, player);
                }
                else
                {
                    t += "You are already on this quest!";
                }
            }
            else
            {
                t += "There is no quest by the name of \"<color=lightblue>" + args[1].ToLower() + "</color>\". Please try again!";
                return;
            }
        }

        public void AbandonQuest(BasePlayer player, string[] args, ref string t)
        {
            Quest RequestQuest, myQuest;
            if (args.Length != 2)
            {
                t += "Usage: <color=lightblue>/quests abandon \"questname\"</color>. Please try again.";
                return;
            }

            RequestQuest = FindQuest(args[1].ToLower());
            if (RequestQuest != null)
            {
                QuestInfo qI = QuestINFO(player);
                List<Quest> ActiveQuests = qI.ActiveQuests();
                //t += String.Format("Requested Quest: {0}\n", ConvertToUpper(RequestQuest.qName));
                //List<KeyValuePair<string, Quest>> aQuests = ActiveQuests(player.userID.ToString());
                if (!ActiveQuests.Contains(RequestQuest))
                {
                    t += "You are not on this quest!";
                    return;
                }
                else
                {
                    RemovePlayerFromQuest(RequestQuest, player);
                    return;
                }
            }
            else
            {
                t += "There is no quest by the name of \"<color=lightblue>" + args[1].ToLower() + "</color>\". Please try again!";
                return;
            }
        }

        public void ShowQuestList(BasePlayer player, string[] args, ref string t)
        {
            int pageIndex = 1;
            if (args.Length > 1)
            {
                try
                {
                    pageIndex = Convert.ToInt32(args[1]);
                }
                catch (Exception)
                {
                    t += "Error, you must use numbers, no letters... i.e. /quests list 1";
                    return;
                }
            }
            var myQuests = ConvertQuests(quests).Skip(pageIndex * 5 - 5).Take(5);
            if (myQuests.Count() < 1) { t += "There are no quests beyond this point."; return; }
            t += String.Format("Available quests - Page: {0}\n", pageIndex);
            foreach (var q in myQuests)
                t += String.Format("<color=red>{0}</color>\n\t\t<color=lightblue>Description: </color>{1}\n\t\t<color=lightblue>Reward:</color> <color=aqua>{2}</color>\n", ConvertToUpper(q.Value.qName), q.Value.qDescription, q.Value.qReward);
            return;
        }
        #endregion

        #region QuestFunctions

        public int GetGatheredAmount(Quest q, BasePlayer player = null)
        {
            if (player == null) return -1;
            QuestInfo qI = QuestINFO(player);
            int Value;
            if (qI.QuestProgress.TryGetValue(q.qName, out Value))
            {
                return Value;
            }
            return -2;
/*            for (int i = 0; i < Gathered.Count; i++)
            {
                if (Gathered[i].Key == player.userID.ToString())
                {
                    if (Gathered[i].Value == q.qName)
                    {
                        return Gathered[i].Value2;
                    }
                }
            }
            return -2;*/
        }

        public void UpdateGathered(Quest q, int newAmount, BasePlayer player = null)
        {
            if (player == null) return;
            QuestInfo qI = QuestINFO(player);
            int Value;
            if (qI.QuestProgress.TryGetValue(q.qName, out Value))
            {
                qI.QuestProgress[q.qName] = newAmount;
            }
        }

        public int GetKilledAmount(Quest q, BasePlayer player = null)
        {
            if(player == null) return -1;
            QuestInfo qI = QuestINFO(player);
            int Value;
            if (qI.QuestProgress.TryGetValue(q.qName, out Value))
            {
                return Value;
            }
            return -2;
        }

        public void UpdateKilled(Quest q, int newAmount, BasePlayer player = null)
        {
            if (player == null) return;
            
            QuestInfo qI = QuestINFO(player);
            int Value;
            if (qI.QuestProgress.TryGetValue(q.qName, out Value))
            {
                qI.QuestProgress[q.qName] = newAmount;
            }
        }

        public void hasPlayerFinishedQuest_Gather(BasePlayer player, BaseEntity entity, Item item)
        {
            QuestInfo qI = QuestINFO(player);
            List<Quest> ActiveQuests = qI.ActiveQuests();
            if (ActiveQuests.Count < 1) return;
            foreach (var q in ActiveQuests)
            {
                //Notification(player, q.Value.qObjective + " | " + item.info.shortname);
                if (q.qObjective == item.info.shortname)
                {
                    UpdateGathered(q, GetGatheredAmount(q, player) + item.amount, player);
                    item.amount = 0;

                    int aGathered = GetGatheredAmount(q, player);

                    var cP = (double)aGathered / q.qAmount;
                    if (LastPercentChange == 0.00)
                        LastPercentChange = cP;
                    var lP = LastPercentChange;
                    var rPC = rateOfChange;
                    double pC = (double)cP - lP;
                    if (pC >= rPC)
                    {
                        Notification(player, String.Format("Amount Gathered for <color=lightblue>{0}</color>: <color=aqua>{1} / {2}</color>", ConvertToUpper(q.qName), aGathered, q.qAmount));
                        LastPercentChange = cP;
                        SaveQuests();
                    }
                    hasCompletedQuest(player, item, q);
                    return;
                }
            }
            return;
        }

        public void hasCompletedQuest(BasePlayer player, Item item, Quest q)
        {
            string FullItemName = String.Empty;
            int aGathered = GetGatheredAmount(q, player);
            if (aGathered >= q.qAmount)
            {
                switch (q.qObjective)
                {
                    case "wood": FullItemName = "Wood"; break;
                    case "stones": FullItemName = "Stones"; break;
                    case "metal_ore": FullItemName = "Metal Ore"; break;
                    case "sulfur_ore": FullItemName = "Sulfur Ore"; break;
                    case "fat_animal": FullItemName = "Animal Fat"; break;
                    case "cloth": FullItemName = "Cloth"; break;
                    case "wolfmeat_raw": FullItemName = "Raw Wolf Meat"; break;
                    case "bone_fragments": FullItemName = "Bone Fragments"; break;
                    default: FullItemName = "\"Item not found\""; break;
                }

                if (aGathered < q.qAmount)
                {
                    Notification(player, String.Format("Insufficent amount of <color=lightblue>{0}</color> to finish the quest.", FullItemName));
                    return;
                }

                Notification(player, String.Format("AmountGathered: <color=lightblue>{0}</color>", q.qAmount));
                LastPercentChange = 0.00;
                GiveBackItems(player.inventory, item, item.info.itemid, q.qAmount, q);
                CompletedQuest(q, player);
                PlayerRewardItem(player, q.qReward, q.qRewardAmount);
                return;
            }
            return;
        }

        public bool BlueprintChance()
        {
            int i = 0, chance = 0, amount = 0;
            Random r = new Random();
            do
            {
                i++;
                chance = r.Next(1, 100);
                if (chance % 4 == 0)
                {
                    if (chance % 6 == 0)
                    {
                        amount++;
                    }
                }
                else
                {
                    //t += "false " + chance + " | ";
                }
            } while (i <= 50);
            if (amount >= 6)
                return true;
            else
                return false;
        }

        public void PlayerRewardItem(BasePlayer player, string RewardName, int RewardAmount)
        {
            PlayerInventory inv = player.inventory;
            ItemDefinition RewardItemDefinition = ItemManager.FindItemDefinition(RewardName);
            inv.GiveItem(RewardItemDefinition.itemid, RewardAmount, false);
            Notification(player, String.Format("You have been rewarded the item <color=yellow>{0}</color> [{1}] for completing the quest!", RewardItemDefinition.displayName.english, RewardAmount));
        }

        public void hasPlayerFinishedQuest_KillAnimal(BasePlayer player, string entityName)
        {
            QuestInfo qI = QuestINFO(player);
            List<Quest> ActiveQuests = qI.ActiveQuests();
            if (ActiveQuests.Count < 1) return;
            foreach (var q in ActiveQuests)
            {
                if (entityName == q.qObjective)
                {
                    UpdateKilled(q, GetKilledAmount(q, player) + 1, player);
                    int amountTmp = GetKilledAmount(q, player);
                    Notification(player, String.Format("You have killed {0}/{1} <color=aqua>{2}</color>", amountTmp, q.qAmount, char.ToUpper(q.qObjective[0]) + q.qObjective.Substring(1)));
                    if (amountTmp >= q.qAmount)
                    {
                        CompletedQuest(q, player);
                        PlayerRewardItem(player, q.qReward, q.qRewardAmount);
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        public void GiveBackItems(PlayerInventory inventory, Item item, int itemId, int amount, Quest quest, BasePlayer player = null)
        {
            List<Item> i = new List<Item>();
            i.Add(item);
            int aGathered = GetGatheredAmount(quest, player);
            int GiveBack = Convert.ToInt32(aGathered - amount);
            if (player != null)
            {
                //Notification(player, "Amount to give back: " + GiveBack.ToString());
            }
            if (GiveBack <= 1) return;
            //if (player != null) Notification(player, "Giving items...");
            inventory.GiveItem(itemId, GiveBack, true);
        }

        public void CompletedQuest(Quest q, BasePlayer player)
        {
            //Notification(player, "Quest completed!");
            RemovePlayerFromQuest(q, player, true);
        }

        #endregion

        #region MiscFunctions

        public void RemovePlayerFromQuest(Quest q, BasePlayer player, bool QuestCompleted = false)
        {
            QuestInfo qI = QuestINFO(player);
            qI.RemoveQuest(q);
            Notification(player, "You have been removed from the quest!");
            SaveQuests();
        }

        public void AddPlayerToQuest(Quest q, BasePlayer player, bool showNotification = true)
        {
            QuestInfo qI = QuestINFO(player);
            qI.AddQuest(q);
            Notification(player, "You have been added to the quest!");
            SaveQuests();
        }

        public static Quest FindQuest(string q)
        {
            Quest quest;
            for (int i = 0; i < quests.Count; i++)
            {
                quest = quests[i].Value;
                if (quest.qName == q)
                {
                    return quest;
                }
            }
            return null;
        }

        public string ConvertToUpper(string s)
        {
            string[] x = s.Split(' ');
            s = String.Empty;
            for (int i = 0; i < x.Length; i++)
            {
                x[i] = char.ToUpper(x[i][0]) + x[i].Substring(1) + " ";
                s += x[i];
            }
            return s;
        }

        public SortedList<int, Quest> ConvertQuests(List<KeyValuePair<string, Quest>> q)
        {
            SortedList<int, Quest> a = new SortedList<int, Quest>();
            int i = 0;
            foreach (var s in q)
            {
                a.Add(i, s.Value);
                i++;
            }
            return a;
        }

        public SortedList<int, Quest> ConvertQuests(List<Quest> q)
        {
            SortedList<int, Quest> a = new SortedList<int, Quest>();
            int i = 0;
            foreach (var s in q)
            {
                a.Add(i, s);
                i++;
            }
            return a;
        }

        public void ClearQuests()
        {
            for (int i = 0; i < quests.Count; i++)
            {
                quests.RemoveAt(i);
            }
        }

        private void Notification(BasePlayer player, string m)
        {
            player.ChatMessage(String.Format("<color=lightblue>Quests: </color> {0}", m));
        }
        #endregion

        private QuestInfo QuestINFO(BasePlayer player)
        {
            var steamId = player.userID.ToString();
            if (PlayerQuestInfo.ContainsKey(steamId)) return PlayerQuestInfo[steamId];
            PlayerQuestInfo[steamId] = new QuestInfo(player.userID.ToString());
            SaveQuests();
            return PlayerQuestInfo[steamId];
        }
    }
}
 
namespace Quests
{
    public class QuestInfo
    {
        public QuestInfo(string playerID)
        {
            playerid = playerID;
            QuestProgress = new Dictionary<string, int>();
            progressPercent = 25;
        }

        public void AddQuest(Quest q)
        {
            QuestProgress.Add(q.qName, 0);
        }

        public void RemoveQuest(Quest q)
        {
            QuestProgress.Remove(q.qName);
        }

        public List<Quest> ActiveQuests()
        {
            List<Quest> q = new List<Quest>();
            foreach(var qP in QuestProgress)
            {
                q.Add(QuestPlugin.FindQuest(qP.Key));
            }
            return q;
        }

        public int progressPercent { get; set; }
        private string playerid { get; set; }
        public Dictionary<string, int> QuestProgress { get; set; }
    }

    public class Quest
    {
        public string qName, qDescription, qType, qObjective;
        public string qReward;
        public int qAmount, qRewardAmount;

        public Quest Create(string questName, string description, string reward, string type, string objective, int amount)
        {
            var quest = new Quest()
            {
                qName = questName,
                qDescription = description,
                qReward = reward,
                qType = type,
                qObjective = objective,
                qAmount = amount,
            };
            return quest;
        }
    }
}