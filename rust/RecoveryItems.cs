using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("RecoveryItems", "RedMat", "1.0.1", ResourceId = 2091)]
    [Description("If you wear an item it is recovered.")] 
    class RecoveryItems : RustPlugin
    {
        #region Global variables
        // Global variables declared area
        string gstrItemListFileNm = "RecoveryItemsData";
        string gstrItemListInfoFileNm = "RecoveryItemsDataInfo";

        #region Timer
        Timer gobjTimer = null;
        #endregion

        #endregion

        #region Init
        void Init()
        {
            LoadDefaultMessages();

            // permission set
            permission.RegisterPermission("recoveryitems.use", this);
        }
        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "No permission",
                ["TimeChange"] = "The HP recovery time has changed.",
                ["ItemAdd"] = "Item Add.",
                ["ItemDel"] = "Item Delete",
                ["StartHeal"] = "Start Heal",
                ["Help"] = "Help : /hpitem command[add, del] itemID Amount"
            }, this);
        }

        #endregion

        #region Runtime plug-ins loaded
        /// <summary>
        /// Runtime plug-ins loaded
        /// </summary>
        void Loaded()
        {
            recoveryItemsData = Interface.Oxide.DataFileSystem.ReadObject<RecoveryItemsData>(gstrItemListFileNm);
            recoveryItemsDataInfo = Interface.Oxide.DataFileSystem.ReadObject<RecoveryItemsDataInfo>(gstrItemListInfoFileNm);

            // Default Value
            if (!recoveryItemsData.glstRecoveryItemsData.Contains(2007564590)) recoveryItemsData.glstRecoveryItemsData.Add(2007564590);
            if (!recoveryItemsDataInfo.glstRecoveryItemsDataInfo.ContainsKey(2007564590)) recoveryItemsDataInfo.glstRecoveryItemsDataInfo.Add(2007564590, 1);

            Interface.Oxide.DataFileSystem.WriteObject(gstrItemListFileNm, recoveryItemsData);
            Interface.Oxide.DataFileSystem.WriteObject(gstrItemListInfoFileNm, recoveryItemsDataInfo);

            fStartTimer();
        }
        #endregion

        #region user function

        #region Timer
        private void fStartTimer()
        {
            Single sglTimer = Single.Parse(Config["HpRecoveryTime"].ToString());

            List<int> lstItemData = recoveryItemsData.glstRecoveryItemsData;
            Dictionary<int, int> dctRecoveryItemsDataInfo = recoveryItemsDataInfo.glstRecoveryItemsDataInfo;

            #region í íì´ë¨¸ ì²ë¦¬
            if (gobjTimer != null) gobjTimer.Destroy();
            gobjTimer = timer.Repeat(sglTimer, 0, () =>
            {
                // Search in all your (login)
                for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    BasePlayer objPlayer = new BasePlayer();

                    objPlayer = BasePlayer.activePlayerList[i];

                    // Apply the amount of recovered items set
                    for (int y = 0; y < lstItemData.Count; y++)
                    {
                        // Check whether the item is set
                        if (dctRecoveryItemsDataInfo.ContainsKey(lstItemData[y]))
                        {
                            if ((bool)fGetWear(objPlayer, lstItemData[y]))
                            {
                                objPlayer.Heal(dctRecoveryItemsDataInfo[lstItemData[y]]);
                            }
                        }
                    }
                }
            });
            #endregion
        }
        #endregion
        
        #region Whether to wear a particular item
        private bool fGetWear(BasePlayer pobjBasePlayer, int pintItemID)
        {
            Item objWearItem = pobjBasePlayer.inventory.containerWear.FindItemByItemID(pintItemID);

            if (objWearItem != null)
            {
                return true;
            }

            return false;
        }
        #endregion

        #region ChatCommand

        [ChatCommand("rt")]
        void fChatCmdHpTime(BasePlayer pbasPlayer, string pstrCmd, string[] args)
        {
            string strParam = "";
            string strConfig = Config["HpRecoveryTime"].ToString();
            Single sglHpTime = 5f;

            // Check permissions
            if (!permission.UserHasPermission(pbasPlayer.UserIDString, "recoveryitems.use"))
            {
                SendReply(pbasPlayer, Lang("NoPermission", pbasPlayer.UserIDString));
                return;
            }

            if (args.Length > 0)
            {
                strParam = args[0];
                Single.TryParse(strParam, out sglHpTime);
            }

            Single.TryParse(strConfig, out sglHpTime);

            if (strConfig == null || sglHpTime < 1)
            {
                Config["HpRecoveryTime"] = 5f;
            }
            else
            {
                Config["HpRecoveryTime"] = sglHpTime;
            }

            SaveConfig();

            SendReply(pbasPlayer, Lang("TimeChange", pbasPlayer.UserIDString));

            fStartTimer();
        }

        [ChatCommand("ri")]
        void fChatCmdHpItem(BasePlayer pbasPlayer, string pstrCmd, string[] args)
        {
            // Check permissions
            if (!permission.UserHasPermission(pbasPlayer.UserIDString, "recoveryitems.use"))
            {
                SendReply(pbasPlayer, Lang("NoPermission", pbasPlayer.UserIDString));
                return;
            }

            if (args.Length > 3)
            {
                SendReply(pbasPlayer, Lang("Help", pbasPlayer.UserIDString));
            }
            else
            {
                switch(args[0])
                {
                    case "add":
                        if (!recoveryItemsData.glstRecoveryItemsData.Contains(int.Parse(args[1])))
                        {
                            recoveryItemsData.glstRecoveryItemsData.Add(int.Parse(args[1]));
                        }

                        if (!recoveryItemsDataInfo.glstRecoveryItemsDataInfo.ContainsKey(int.Parse(args[1])))
                        {
                            recoveryItemsDataInfo.glstRecoveryItemsDataInfo.Add(int.Parse(args[1]), int.Parse(args[2]));
                        }

                        SendReply(pbasPlayer, Lang("ItemAdd", pbasPlayer.UserIDString));
                        break;
                    case "del":
                        if (recoveryItemsData.glstRecoveryItemsData.Contains(int.Parse(args[1])))
                        {
                            recoveryItemsData.glstRecoveryItemsData.Remove(int.Parse(args[1]));
                        }

                        if (recoveryItemsDataInfo.glstRecoveryItemsDataInfo.ContainsKey(int.Parse(args[1])))
                        {
                           recoveryItemsDataInfo.glstRecoveryItemsDataInfo.Remove(int.Parse(args[1]));
                        }

                        SendReply(pbasPlayer, Lang("ItemDel", pbasPlayer.UserIDString));
                        break;
                    default:
                        SendReply(pbasPlayer, Lang("Help", pbasPlayer.UserIDString));
                        break;
                }

                Interface.Oxide.DataFileSystem.WriteObject(gstrItemListFileNm, recoveryItemsData);
                Interface.Oxide.DataFileSystem.WriteObject(gstrItemListInfoFileNm, recoveryItemsDataInfo);

                fStartTimer();
                SendReply(pbasPlayer, Lang("StartHeal", pbasPlayer.UserIDString));
            }
        }

        #endregion

        #region Config File Create
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            Config.Clear();
            Config["HpRecoveryTime"] = 5;
            SaveConfig();
        }
        #endregion

        #region DataFile Create
        RecoveryItemsData recoveryItemsData;
        RecoveryItemsDataInfo recoveryItemsDataInfo;

        class RecoveryItemsData
        {
            public List<int> glstRecoveryItemsData = new List<int>();

            public RecoveryItemsData()
            {
            }
        }

        class RecoveryItemsDataInfo
        {
            public Dictionary<int, int> glstRecoveryItemsDataInfo = new Dictionary<int, int>();

            public RecoveryItemsDataInfo()
            {
            }
        }
        #endregion

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
