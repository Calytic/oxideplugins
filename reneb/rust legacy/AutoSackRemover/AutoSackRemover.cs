using UnityEngine;
using System.Reflection;
using uLink;

namespace Oxide.Plugins 
{
    [Info("AutoSackRemover", "Reneb", "1.0.1", ResourceId = 949)]
    class AutoSackRemover : RustLegacyPlugin
    {
        Vector3 cachedVector1;
        Vector3 cachedVector2;
        LootableObject cachedSack;
        public FieldInfo usingPlayer;

        public static int timeToRemove = 1;
        public static bool dontremoveiflooted = true;
        public static bool useAutoRemover = true;

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<int>("Settings: time before removing the loot sack", ref timeToRemove);
            CheckCfg<bool>("Settings: dont remove if being looted", ref dontremoveiflooted);
            CheckCfg<bool>("Settings: activate plugin", ref useAutoRemover);
            SaveConfig();
        }

        void Loaded()
        {
            usingPlayer = typeof(LootableObject).GetField("_currentlyUsingPlayer", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }
        void FindSack(Vector3 position)
        {
            cachedSack = null;
            foreach (Collider collider in Physics.OverlapSphere(position, 1f))
            {
                if (collider.gameObject.name == "LootSack(Clone)")
                {
                    cachedSack = collider.GetComponent<LootableObject>();
                }
            }
            if (cachedSack == null) return;
            if (timeToRemove == 0)
                DestroySack(cachedSack);
            else
            {
                timer.Once(timeToRemove, () => DestroySack(cachedSack));
            }
        }
        void DestroySack(LootableObject sack)
        {
            if (sack != null)
            {

                if (dontremoveiflooted && ((uLink.NetworkPlayer)usingPlayer.GetValue(sack) != uLink.NetworkPlayer.unassigned))
                {
                    timer.Once(1f, () => DestroySack(sack));
                }
                else
                {
                    NetCull.Destroy(sack.gameObject);
                }
            }
        }

        void OnKilled(TakeDamage takedamage, DamageEvent damage)
        {
            if (!useAutoRemover) return;
            if (takedamage.GetComponent<HumanController>() == null) return;

            takedamage.GetComponent<Character>().transform.GetGroundInfo(out cachedVector1, out cachedVector2);
            timer.Once(0.01f, () => FindSack(cachedVector1));
        }

        [ChatCommand("sackremover")]
        void cmdChatSackRemover(NetUser netuser, string command, string[] args)
        {
            if (!netuser.CanAdmin()) { SendReply(netuser, "You dont have access to this command."); return; }
            useAutoRemover = !useAutoRemover;
            SaveConfig();
            SendReply(netuser, "AutoSackRemover is now set to: "+useAutoRemover.ToString());
        }

        void SendHelpText(NetUser netuser)
        {
            if (!netuser.CanAdmin()) return;
            SendReply(netuser, "Sacks Remover: /sackremover NEWREMOVETIME");
        }
    }
}
 