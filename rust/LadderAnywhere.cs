using System.Reflection;
using UnityEngine;
using System.Linq;
using Oxide.Core.Plugins;
using System.Text;

namespace Oxide.Plugins
{
    [Info("LadderAnywhere", "LadderAnywhere by Deicide666ra", "1.0.7", ResourceId = 1327)]
    public class LadderAnywhere : RustPlugin
    {
        class LadderConfig
        {
            public LadderConfig() { }

            public float maxDist = 5; // meters
            public int authLevel = 0; // 0= anyone, 1= moderator/owner, 2= owner only
            public bool radiationCheck = true; // true means players cannot place ladders in radiation areas

            // Item blacklist, if the object pointed contains these words, abort placement
            public string[] blacklist = new[] { "wall.external", "player", "ladder", "cupboard", "furnace", "barricade", "storage" };
        }

        private LadderConfig g_config;

        void Loaded() => LoadConfigValues();
        protected override void LoadDefaultConfig()
        {
            g_config = new LadderConfig();
            Config.WriteObject(g_config, true);
            Puts("New configuration file created.");
        }

        void LoadConfigValues()
        {
            try
            {
                g_config = Config.ReadObject<LadderConfig>();
            }
            catch
            {
                Puts("Could not read config, creating new default config.");
                LoadDefaultConfig();
            }
        }


        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder();
            sb.Append("<color=yellow>LadderAnywehre 1.0.2</color> Â· Bypass building privs with ladders\n");
            sb.Append("Usage: <color=lime>/ldr</color> with a ladder in hand places the ladder where you are pointing.\n");
            sb.Append($"Excluded items: {g_config.blacklist.ToSentence()}");
            player.ChatMessage(sb.ToString());
        }


        [ChatCommand("ldr")]
        void cmdLadder(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel < g_config.authLevel)
            {
                player.ChatMessage("You are not authorized to use this command.");
                return;
            }

            if (player.GetActiveItem() == null ||
                player.GetActiveItem().info.shortname != "ladder.wooden.wall")
            {
                player.ChatMessage($"You need to have a ladder in hand to do this. {player.GetActiveItem()?.info.shortname}");
                return;
            }

            RaycastHit hit;
            var hitSomething = Physics.Raycast(player.eyes.HeadRay(), out hit, g_config.maxDist);

            if (!hitSomething)
            {
                player.ChatMessage($"You need to be closer to the surface you wish to place the ladder on (max {g_config.maxDist}m).");
                return;
            }

            var prefab = hit.collider.transform.parent?.gameObject?.name;
            var entity = hit.GetEntity();

            if (g_config.radiationCheck && player.radiationLevel != 0)
            {
                player.ChatMessage("You cannot place ladders within radiation zones.");
                return;
            }
            
            if ((prefab == null && entity == null) ||
                (!string.IsNullOrEmpty(prefab) && g_config.blacklist.Any(e => prefab.Contains(e))) ||
                (entity != null && g_config.blacklist.Any(e => entity.LookupPrefabName().Contains(e))))
            {
                player.ChatMessage($"You cannot put a ladder on this item.");
                return;
            }


            Quaternion currentRot;
            if (!TryGetPlayerView(player, out currentRot))
                return;

            player.GetActiveItem().RemoveFromContainer();
            DoDeploy(player, hit.collider, currentRot, hit.point);
        }


       // Just a debug function to test if a ladder would fit there or not
       [ChatCommand("lt")]
        void cmdLt(BasePlayer player, string cmd, string[] args)
        {
            if (!player.IsAdmin())
            {
                player.ChatMessage("You are not authorized to use this command.");
                return;
            }

            RaycastHit hit;
            var hitSomething = Physics.Raycast(player.eyes.HeadRay(), out hit, g_config.maxDist);

            if (!hitSomething)
            {
                player.ChatMessage($"You need to be closer to the surface you wish to place the ladder on (max {g_config.maxDist}m).");
                return;
            }

            var entity = hit.GetEntity();
            if (entity != null)
                player.ChatMessage($"Entity: {entity.LookupPrefabName()}");

            var prefab = hit.GetCollider().transform.parent?.gameObject?.name;
            if (prefab != null)
                player.ChatMessage($"Prefab: {prefab}");
           
            if (g_config.radiationCheck && player.radiationLevel != 0)
            {
                player.ChatMessage("You cannot place ladders within radiation zones.");
                return;
            }

            if (!string.IsNullOrEmpty(prefab) && g_config.blacklist.Any(e => prefab.Contains(e)) ||
                (prefab == null && entity == null))
            {
                player.ChatMessage($"You cannot put a ladder on this item.");
                return;
            }

        }


        /////////////////////////////////////////////////////
        ///  DoDeploy(BuildPlayer buildplayer, BasePlayer player, Collider baseentity)
        ///  Deploy Deployables
        ///  FULL CREDIT TO RENEB, I TOTALLY STOLE THIS
        /////////////////////////////////////////////////////
        private void DoDeploy(BasePlayer player, Collider baseentity, Quaternion currentRot, Vector3 closestHitpoint)
        {
            var VectorUP = new Vector3(0f, 1f, 0f);
            var newPos = closestHitpoint + (VectorUP * 0f);
            var newRot = currentRot;
            newRot.x = 0f;
            newRot.z = 0f;
            SpawnDeployable("assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", newPos, newRot, player);
        }


        /////////////////////////////////////////////////////
        ///  SpawnDeployable()
        ///  Function to spawn a deployable
        ///  FULL CREDIT TO RENEB, I TOTALLY STOLE THIS
        /////////////////////////////////////////////////////
        private void SpawnDeployable(string prefab, Vector3 pos, Quaternion angles, BasePlayer player)
        {
            var newBaseEntity = GameManager.server.CreateEntity(prefab, pos, angles);
            if (newBaseEntity == null)
            {
                return;
            }
            newBaseEntity.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
            newBaseEntity.SendMessage("InitializeItem", newBaseEntity, SendMessageOptions.DontRequireReceiver);
            newBaseEntity.Spawn(true);
            newBaseEntity.transform.RotateAround(newBaseEntity.transform.position, newBaseEntity.transform.up, 180f);
            newBaseEntity.SendNetworkUpdate();
        }


        /////////////////////////////////////////////////////
        ///  TryGetPlayerView( BasePlayer player, out Quaternion viewAngle )
        ///  Get the angle on which the player is looking at
        ///  Notice that this is very usefull for spectating modes as the default player.transform.rotation doesn't work in this case.
        ///  FULL CREDIT TO RENEB, I TOTALLY STOLE THIS
        /////////////////////////////////////////////////////
        static bool TryGetPlayerView(BasePlayer player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);

            var serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            var input = serverinput.GetValue(player) as InputState;
            if (input == null)
                return false;
            if (input.current == null)
                return false;

            viewAngle = Quaternion.Euler(input.current.aimAngles);
            return true;
        }
    }
}