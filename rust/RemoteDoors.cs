using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("RemoteDoors", "Reneb", "1.0.8", ResourceId = 1379)]
    class RemoteDoors : RustPlugin
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Configs
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        protected override void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        static string permissionRemoteDoors = "remotedoors.use";
        static bool allowusers = true;
        static Dictionary<string, object> cost = defaultCost();
        static int maxDistance = 60;
        static int antiTrapDistance = 80;
        static int maxDoors = 20;

        void Init()
        {
            CheckCfg("Remote Activator - Cost", ref cost);
            CheckCfg("Remote Activator - Max Door Distance", ref maxDistance);
            CheckCfg("Remote Activator - Max Doors", ref maxDoors);
            CheckCfg("Remote Activator - Anti Trap Distance", ref antiTrapDistance);
            SaveConfig();
        }

        static Dictionary<string, object> defaultCost()
        {
            var defaultcost = new Dictionary<string, object>();
            defaultcost.Add("High Quality Metal", "200");
            defaultcost.Add("Battery - Small", "1");
            return defaultcost;
        }

        bool hasAccess(BasePlayer player)
        {
            if (player.net.connection.authLevel > 1) return true;
            return permission.UserHasPermission(player.userID.ToString(), permissionRemoteDoors);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Fields
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static int constructionColl = LayerMask.GetMask("Construction");
        static int doorColl = LayerMask.GetMask("Construction Trigger", "Construction");
        static int signColl = LayerMask.GetMask("Deployed");
        static int playerColl = LayerMask.GetMask("Player (Server)");

        Vector3 VectorForward = new Vector3(0f, 0f, 0.10f);

        RaycastHit cachedHit;

        static FieldInfo serverinput;
        static FieldInfo buildingPriviledge;
        static FieldInfo fieldWhiteList;
        static MethodInfo updatelayer;

        static Hash<Vector3, RemoteActivator> remoteActivators = new Hash<Vector3, RemoteActivator>();


        void Loaded()
        {
            LoadData();
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            buildingPriviledge = typeof(BasePlayer).GetField("buildingPrivilege", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            fieldWhiteList = typeof(CodeLock).GetField("whitelistPlayers", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            updatelayer = typeof(BuildingBlock).GetMethod("UpdateLayer", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            if (!permission.PermissionExists(permissionRemoteDoors)) permission.RegisterPermission(permissionRemoteDoors, this);
        }
        void OnServerInitialized()
        {
            InitializeTable();
            List<Vector3> toDelete = new List<Vector3>();

            foreach (Vector3 pos in remoteActivators.Keys)
            {
                bool todelete = true;
                foreach (Collider col in Physics.OverlapSphere(pos, 2f, signColl))
                {
                    Signage sign = col.GetComponentInParent<Signage>();
                    if (sign == null) continue;
                    if (pos == new Vector3(Mathf.Ceil(sign.transform.position.x), Mathf.Ceil(sign.transform.position.y), Mathf.Ceil(sign.transform.position.z)))
                        todelete = false;
                }
                if (todelete)
                    toDelete.Add(pos);
            }
            foreach (Vector3 del in toDelete)
            {
                storedData.RemoteActivators.Remove(remoteActivators[del]);
                remoteActivators.Remove(del);
                Debug.Log("removed a sign");
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Item Management ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        readonly Dictionary<string, string> displaynameToShortname = new Dictionary<string, string>();
        private void InitializeTable()
        {
            displaynameToShortname.Clear();
            var ItemsDefinition = ItemManager.itemList;
            foreach (var itemdef in ItemsDefinition)
            {
                displaynameToShortname.Add(itemdef.displayName.english.ToLower(), itemdef.shortname);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Data
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static StoredData storedData;

        class StoredData
        {
            public HashSet<RemoteActivator> RemoteActivators = new HashSet<RemoteActivator>();

            public StoredData()
            {
            }
        }

        void LoadData()
        {
            remoteActivators.Clear();
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("RemoteDoors");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var remote in storedData.RemoteActivators)
                remoteActivators[remote.Pos()] = remote;
        }
        void Unloaded()
        {
            SaveData();
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("RemoteDoors", storedData);
        }


        public class RemoteDoor
        {
            public string x;
            public string y;
            public string z;

            Door door;
            Vector3 pos = default(Vector3);

            public RemoteDoor()
            {
            }
            public RemoteDoor(Vector3 pos)
            {
                x = pos.x.ToString();
                y = pos.y.ToString();
                z = pos.z.ToString();
                this.pos = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
            }
            public bool OpenDoor(bool openclose)
            {
                if (door == null)
                    door = FindDoor();
                if (door == null) return false;
                door.SetFlag(BaseEntity.Flags.Open, openclose);
                door.SendNetworkUpdateImmediate();
                return true;
            }
            public Vector3 Pos()
            {
                if (pos == default(Vector3))
                    pos = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return pos;
            }
            Door FindDoor()
            {
                foreach (Collider col in Physics.OverlapSphere(Pos(), 2f, doorColl))
                {
                    if (col.GetComponentInParent<Door>() == null) continue;
                    if (Mathf.Ceil(col.transform.position.x) == pos.x && Mathf.Ceil(col.transform.position.y) == pos.y && Mathf.Ceil(col.transform.position.z) == pos.z)
                    {
                        door = col.GetComponentInParent<Door>();
                    }
                }
                return door;
            }
        }

        Dictionary<Vector3, float> allowAuth = new Dictionary<Vector3, float>();
        public class RemoteActivator
        {
            public string name;
            public string x;
            public string y;
            public string z;
            public string owner;
            public List<string> autorizedUsers;
            public List<RemoteDoor> listedDoors;

            Vector3 pos = default(Vector3);

            public RemoteActivator() { }

            public RemoteActivator(Vector3 pos, string name, string owner)
            {
                x = pos.x.ToString();
                y = pos.y.ToString();
                z = pos.z.ToString();

                this.name = name;
                this.owner = owner;

                autorizedUsers = new List<string>();
                autorizedUsers.Add(owner);

                listedDoors = new List<RemoteDoor>();
            }

            public Vector3 Pos()
            {
                if (pos == default(Vector3))
                    pos = new Vector3(Mathf.Ceil(float.Parse(x)), Mathf.Ceil(float.Parse(y)), Mathf.Ceil(float.Parse(z)));
                return pos;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Remote Activator Spawning
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        string SignTexture()
        {
            return "iVBORw0KGgoAAAANSUhEUgAAAlgAAAEsCAYAAAAfPc2WAAAABGdBTUEAALGPC/xhBQAAACBjSFJNAAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAABmJLR0QAAAAAAAD5Q7t/AAAACXBIWXMAAAsSAAALEgHS3X78AABKxElEQVR42u2dz28bSXr+H3Y3f0mUTdsaj5yxFtEG2okxMTZz8GWAILdFbnMOcshhc86/sN9T/oBcN8AiCILcAiSnHPawQYAcMggGyeyuM6vNehEpY9qWLUqixCbZTX4PzttbLFVVV5MtqSk9H8CwSDa7q6ubbz/11lvvWwMwAyGEEEIIKY3guhtACCGEEHLToMAihBBCCCkZCixCCCGEkJKhwCKEEEIIKRkKLEIIIYSQkqHAIoQQQggpGQosQgghhJCSocAihBBCCCkZCixCCCGEkJKhwCKEEEIIKRkKLEIIIYSQkqHAIoQQQggpGQosQgghhJCSocAihBBCCCkZCixCCCGEkJKhwCKEEEIIKRkKLEIIIYSQkqHAIoQQQggpGQosQgghhJCSocAihBBCCCkZCixCCCGEkJKhwCKEEEIIKRkKLEIIIYSQkqHAIoQQQggpGQosQgghhJCSocAihBBCCCkZCixCCCGEkJKhwCKEEEIIKRkKLEIIIYSQkqHAIoQQQggpGQosQgghhJCSocAihBBCCCkZCixCCCGEkJKhwCKEEEIIKZlo2R3U63V89tln2NjYwGQyQZqm6Pf7mE6n2Taz2Qyz2QxJkqBWq2XvAZh7LX8DwHQ6RRAECMMQ0+k024e6fRAESNMUABCGIcIwnNuHevwkSbLvq/+r29RqtQvv6/sBgCAIUKvV5s5R/Z7aBtt56uerH386naJWqyEIgqw/9P2o+w6CILfdeeeWd/5qv6vnL++p37P1gb5vW7vV81f7TL8H5Ngu1O3y+sL0vn48aZft+uddb9d2epvDMMz+l+vjc67ym9Gvr+l+1I/vc59EUYQkSeauv+089N+cqx1yjtJ+OX99G/166P1iO67+uXqvAcjOS9/edo/o/aa3U9+P3DtFrmWapqjVatn/tvNY9P5TX8t9rfer6TeR9/uR6+hzrvp2uj1S7wnT7123x6a2qH2mf08+Fxuhvue6B3zvE9c26m/V1OfqM0C1v6bju663j01Wzz+v3abjm35zURRZn8tpms49w9V+V+8BedaLLtDP13Ses9ksO7Z6/CAI0O12Ua/X0Ww2kSQJ/v3f/x1v375FkiQX7qNlqAGYLfLFKIrw7W9/G2tra5hMJpkB6HQ6uHv3Lo6Ojqw/ZJvIyXu4q9sFQYAkSbKLstDJWwy+rQ02A1r0mEJe/yxKkX51Pex8+qDo58uck22/ef1V5Jou2i7Zh29f+7ZPjIQYoUXO0fS57TdYdL9Ft1P7rEif54myIvfwZZzfojbN5/hhGM4NTn334XMv2uxREXxswiJ9cB22xPe4RZ4Fiz7nfPrLNrBbpN+K/I58nudFEAEVRdGFwaFPe23tMG1z7949HB8fYzAYIAxDRFGEjY0NAMCvfvUrjEYjnJ2dYTgcLnyNsuOhoMASYdVsNjEejzMVWK/XkSQJkiRBEARot9sXGpg3IgCK3RSi5k0Cy+YZ8OqUBQzWVbDo8X2+d93ndpltrcq5LUoRj6J+r/vey5fVR5f1kKxKG3z70fS+6lX18ZisKpfxe63Kb/oy23rd51jmc9DmQRXkOV7kPvf5XeufiS6ZTqeIoghRFGEymWA8HmM6neLhw4eo1+vo9XoYj8f45S9/mXnqF+pDFBBYjx49wu7uLg4ODtBqtbC2tjbXwFarhVqthslkAgAXvEt5UzlFEMVruwHUqUV9mqmKVLlt5HoxDSBUYSWvgfIexFdxP17GMcruh8tss8lbYHvIlGk7yc2hys8NfXpXnerLe26Xha4/ZCagXq9jNpshjmM0Go3MQTQcDlGr1fDkyRMcHR3hv//7v9FsNvH1118vJpLhKbA++eQTfPjhh/jmm29Qr9ezqcE4jrG+vj4nZkQdytSh6WTL6DQ95kqdE07T9NYapcsStZdJlQ3FdZ+Deg1lAKN6btUYrTKm+66DIrbBdk9X7fx8r4fEVsm5TafT7AGwSP+U1b5VYJXOYRXt8mX0gcRVmuIJRQCV1T96vJYIKdVjFgQBzs7O0Gq1UK/XcX5+jiRJsLu7i2azif/4j//A2toa/vM//7Pw79BLYD1+/BgfffQRRqNRZgwajUZmCGSqcDqdYjgcZuKm2WxmQW5ra2uI4xhpmmbf9Z2+U4WT/Jim0ykajQYajQbG4zFGo9Gc4ArDcM5w+WJqk36xJdAu7wcjn+uB6vpreU8P4DPhusDS9mazCQCI4/g3F9oQc2H6vmyrvlb3YTtvnxtP3dY2tetzPYpiOpbeFlsf+7ZT31b2qY7WfI2GOtJSH8Ky/0ajgVqtlrmuqzp9pN5PamDuZDKZC16XzwVToK8+DZemKVqtFu7du4d+v4/BYIB6vQ7gN9dUjqcHEVcRaV8URZjNZhiPx9n11sVzkVG+GqwchuHce4ve2/JaP47NFhQ5jqt/fK6dbkeL2iV9P64FL+prV5vVfbRaLQDAaDTKPSefZ0GePba91vdj6z/TNkWuh/4deS7L6zRN0Ww2s+e43PfSt7ZFLK7+DoIA4/EYYRii1Wplgmk2m2E0GmW/qXa7jSAIMJlM0Gg0soHreDzOYl8fPHiA9fV1/OxnPwPwPkaryL2cK7A2Njawu7uLfr+P+/fvZycu6k/1Wp2dneFb3/oW/vAP/xC7u7vY2trKOu3DDz9Eu93Opg/1DsxbAWO6oLPZ+9WB/X4fd+7cyW5eW+Cb+r25TjAcT242n4uq7kO/2KbXs9kMr1+/Zr8Y+sW22kR//9WrV+y/JfrP9r6+Asi2AslHsMv3ZIVvGIbo9/vo9/toNptoNpuIoiiL41QHSKZ+nU6nWZxnkiRI0xRhGGIymeD8/Dwb3IVhiM3NTXS73WyVkm1awnXupmvhOvdl+t91XfPuCROqsHLFv+Qt0pDP4zjGyckJut2ucVWY+iC0Hc/2u6zX6xgOh3j16hUajQY+/PBD9t8S/ffw4UP2i6FfGo0Ger0e9vb28M///M/4n//5H6yvr895s9SQIgA4OTnBb//2b2M8HuPdu3fo9/vY39/3X0QDh8AKggCffPIJjo+P8ejRI0wmk6zREn0/Go0wGo3wu7/7u/jTP/1TPHv2DBsbG9koUxouqhHAhalDZwNrtbmLqv4TxSmrCV2pCoriq5qXPQb7hf13nVzG1JN6rpJqJQxD1Ot1vH37Fm/fvkW73Uar1coElrjngyBAFF3MHpMkSRbrKQJrNBplwno2m2E4HGI0GiEIAmxtbeHBgwdZ6hixRZc5nVSlaR8ZPJSB/FbUVV4yhan+kz7wPa5M2QDIPLRVme5j/928fhFbNJvNcHp6ii+++AJ//dd/jf/6r//KBnwycJNBe71ex5s3b/DJJ5/g1atXeP36NYIgwK9+9Su/c4RDYK2traHT6eCjjz7CbDbL3Gjioh4MBrh79y7++I//GH/yJ3+CVquFs7OzrGEiriQeS1yPeq4N6Wy1g0yxW2ouIJlqiKIIYRhmhtWlzk2jT5MqNo2ifS9s3nfUY7Bf7O1RMU0xsP+W7z+1D8pGdfNHUZQZt6OjI/T7fbRaLbRarSzAtNFoZEKs0Whk11PaKN4pEUwysAN+M90i6QySJEG328W9e/cyYSVTBFdxzr797+s5WHQ6Rv3fdl/meStlu+l0imaziTRNkSQJ6vX6nBfYFDvj8vCqK1/l9yvxMey/cvqP/XKxX2RKsF6vY319HXEc42//9m/xd3/3dzg+Pkan08lCNCT8KQgCDIdDbG1tZR74yWSCg4OD3P5yCqx2u43t7W3cuXMna1wcx+h0Onj9+jV+7/d+D3/+53+OZ8+e4fz8HKPRCI1GA6PRKFtRKLEShBACAD//+c+xtraGtbW1zHUvYks8hvK/GMvxeJx5sORv8WDJe2oQ68cff3zdp0kIqQhxHGfeqziOszRTzWYTa2tr+OKLL/CXf/mX+OlPf4qHDx9iMBig1WplIk2NgRWv209/+tNcr1oI4P+ZPgiCAOvr67h//342wk+SJJsW/Oijj/AXf/EXePLkCc7Pz7OgMABoNpvZCFQdiRJCyC9/+cu5VXJqZnM1IF4NyhZRlSQJxuMx4jjO/o1GIwyHQ5ycnGA4HCIIggtxPISQ24tqV8T2iOAaj8f41re+hd///d/Hv/3bv6Hf76Ner2fxnOJcGo1GcwtpNjY28O7dO+dxrconDEM8ePAgE00ioGSV2g9+8APs7Ozg5ORkLnpfpgOqFGdCCKkOqpgyfab+r39Hfa2vPBQjeH5+ft2nSAipICadMpvNcHJygp2dHfzgBz+Yy36gTomK6AKQedvzMAqsMAxx586dLMJebdzp6Sm+//3v47vf/S76/X52EDWgVZ1DJYQQFZddsH1msiem2DY1xQUhhAjqqnE1BAFAtsL5u9/9Lr7//e/j9PTUOMiT+NzZbIa1tbXcYxoFVhAE2NzczNxhsvOzszM8e/YMn3/+OUajUZZqXvdamZZjEkKIDVe6C9s2Ji+YreA7IeT2Yku7It4s+TcajfD555/j2bNnODs7u2BLZLWjvk8bRoElq3j0/BOz2Qzf+9730G63MR6Ps/dUo6YvfSeEkDzU0aVr6tD2Wn2PdocQoqPaFj0Br9ie8XiMdruN733vexdWQ6qpp3wxCqxms4lWqzVXwy9JEmxvb+Ozzz7LIullWbWg563Q4yQIIcQlgIqEFai5chiWQAixoesR3VaIlhFt89lnn2F7extJkswlIJWM897HNb0piULVBF2j0Qgff/wxHj58iDiOs6B3VYSp/0yZVQkhxGQTFvV4m7I8X2byVELI6mHyRqm2Q6YKJY3Dw4cP8fHHH2d5ENUM70U0jVFgra+vZ41So+i/853vZAeRpY42Y0Z3PSGkKEUHZa4SHYQQAuTrEanyILomCAJ85zvfmdM/eqkzH1sTmd5sNptz4knyQezs7GRlROSgrkzZRWocEUJuD66FMLa6c+pnJhG2SGZqQsjNx1VTVd5XvVuTyQQ7OztzxalFhEnqKnURoA1nHiyZf5ScEFKfRw7kKiHiClglhBBBFUu2v/UYK7Er6mIcqR/JsARCiIo6HQiYPd9qqbXJZIJ2u53l/pQ4dIk5V3NiuTAKLBFWegPUgomCrgL19wghRMc0MCs63aeKLrVI98bGxnWfHiGkgphqI+phCZIjy6RjRGjJvzyMU4Q2F72peK1NEZq+RwghtilBm92xTSeK/VFXNdfrda5cJoTMYdIhJqGlv29bkDOdThfPg2XLmGw7mD4d6IqTIITcblyDMR1XXix91Nlut1Gr1XB8fHzdp0gIqRgmQaWvQDZtb9JDURQtXipHdqILpjzhpMdPMAaLEJKHvnRa/0zdRrUx4sYPwxCNRgMAMBwOr/t0CCEVQ4/BAvJ1jEn/+HxfxSiw1ALPeUkBbdtQXBFCTLhWDbrisNTpQPXzZrMJAIjjGAA4RUgIuUCeN9wlmFRxpRaBzsM6RaiPGG3BYTavlsv1Rgi5vdhSLPgYLX214J07d5CmKc7Pz537J4TcXmx6RNU0rsV6i1aLMAos08FMjbK91htHCCE2XOEEtuXU9XodURRhPB5jPB6zTA4hxIktaN1Hz5icSj7kCizbQdSG+BRnJYQQwG+1oG1byXslZbok5qqI254QcjtxaRVb7JW+bRGR5Zwi1Evl+ASF6Y2l0SOEmPC1DXoBeVkmPR6Ps5w16rJpDu4IISq6HnGFN6nf0fWProvyiGwf6ErNR7mZGk8IISZ8wxAkubGIqNlshjRNs4B3FncmhPjgG0dl0j/q/7440zSofxfJtExxRQhZBJMhc40cbRUnCCHEhK9uMdmcorYlN9GoqSCiaTuby41iixBiwmQ/8tz1ptAF2hhCiAtTKhh9pm0R3ZOHNchdP4hpx7Ylj+rfHE0SQlRMq3aK2Ak9/94yI0xCyM3HpEeK6BlTmiofnB4sWxBYnpDyCYwnhNxO8lbpmNC9V/RaEUJ80b3e6vu29/TXi5QANAqsNE0vVKo3BbmbahD6ROcTQm4vtpFj3oDMFQ+Rl7uPEHK70acGTTUJBV3zqFooTVPvYxpXEboytZtqgsm2ptgrGjtCiIotlUuercgLTuWAjhBiQtUn6kAtr5C8absimiZyfWibgzRts2gDCCG3i0ViGXwC4PX9E0KIji1o3RSDZRJbRXBWRS1iAPOSkBJCiIpP5nbXZ8y3RwjxoQx9soitsQa5T6fTLEuyunN5T5L/mSrXM00DIcSGHscZBEFW+qaIJ2oRTxgh5PbhyoYAYE7PyN/q99Rkx0sHucsOR6MR4jjO3gvD0FqPUIdpGgghJmQAN5vNMpuivifYXPb6ezoUXIQQlTw9YqpDKCILAOI4xmg0MjqUXORmcm82mwCAyWQCAFlZCn2FoQpd94QQG2I3VA+4iKsyXPkc1BFCTLj0imp/ROeI7hEdVEomd9mRWqH+wYMHiKIIaZrOjTZdeWkotAghOuq0oLxWRZfNiLkGcrQzhBAbNi2iCitV16RpiiiK8ODBAwAX9ZAvRoGlGzlpxLt377JK9noj1f9VOJokhKiIkJKYBrEnEv9gw+ThMrn+KbYIISquUALdXkhR+Xfv3hmr2ahTh3kYrZlu5IIgwOnpaTbi1Bttqg9GYUUIMdFqtRCGIcbjcSauZOAmSY51fAZxzIdFCHFh0im2+KwoinB6emrUQ77kblmv15EkCWq1GhqNBgA4pwM5LUgIcVGr1TAej5EkSfaeKpJspSzyhBeFFSEkjzytIu81Gg3UajUkSYJ6vb7QsawCq1arodlsYjQazUXU5yUU1Q0lxRYhRGU0GmEymVxwtZviOV2rlnVbQ4FFCDGh65G81cdqWqparYbRaIRms1lYz1inCNfW1nB2doYwDBFFEeI4zmKzfIslUlwRQnQkx56tlqANk/hi7BUhxAfXIE1/T2Kt4jhGFEUIwxBnZ2dYW1tbfopwbW0Nw+Ewc4ulaZoFpOoNtS175IiSEGJC0jOoRVN97YWtUDRDEwghLmwZD/Riz/K5DATFTtXrdQyHQ6ytrc1t58IosN6+fXthtaDNpaYHibEmISHEhawc1MMOihZ7tq0oJIQQHZv325VqSrUpsrrw7du3Fz6zYRRYEtTuK5b0ZYx5c52EkNuLblvKsBG0M4QQG654TV9tI3ZLXZyTh1FgtdvtuUYte1KEECKYDFwRb7fNk0V7QwixUZaeEX3kQ2R601TQ0JVdWfdYMbsyIcSGyYOVF4PlWj2oL7mm3SGEqJimB02aRcekg/RE6y6MHiwRWKZgMP1gvkaREEKAix6nPA+WywC6VgYRQoiQp1VsC2jU16UILFsV+zzDRcNGCPFF92LliSj9tY/nixBCTPjoGZPuKeIhtwos0wpB0zb6CJKrCAkheegepzwPlssWAawkQQhxY9ImuhfdFd9pS0vlwpnJ3ZZjxjZq1CtTqw0khBDBx1vls9JHHwzSo0UI0THVH7SlZbAtwFlk8GYVWD7Vp21Z3fXEXYQQoqIbMdOATLc3eXlqCCHEhu6BMmVvB8q1N0aBJVnbdaPnyoJqOxlCCNHRRZWvraBNIYQsgs3p46pGA8zrHzVBsg/OojqmSvW2kSeXSBNCiuATdpD3fZPdoQ0ihLgwzcaZwhNcaacWLpWjf1kXUabcEGrEPXNhEUJ8sQklm50xvcc0DYQQG7qzSNcrKi69Y3I6uYhyt8BiIolV7gkhPugGT8izGwxDIIT4sMygyySmfG1P4LtT1xJHfXtWuCeE5OFaSOOzret9QggRTF5y27SgLdxgkQFdbgxWXiFEW2MprAghLvSBWF4clqkkjsnm0PYQQkyYSubI375aR/AZ3BkFVpqmznTwvjmumJOGEJJHETthirfS08LQ5hBCVHxsjI/9mE6nSNPU+7hGgRVFkTMHhCnQy2TgOEVICNGxhRjkjSBteWs4kCOEuDDlv7IVfLYt5pPPosgrdB1ATpD7Isum9WBVCixCiIrJNixiawghxBfdGeRrQ/JSVLkwerAmk8mFKULbMmiX2nO9Twi53dgW0fjEYal/L5KfhhBye8jTJzb7o39vOp1iMpl46xprsecwDI1BpHm1wlR1SENHCDHhGkEWed+Vx4YQQgQ1VjMv755N+4gu8sW5itBUW9C0tFEvtmpbWUgIIT61Sl1JRovEbhFCiCle3JV/T/0sb5bORW4md32ntpo9Ju8WCz4TQmzkBbXnfWbK4k4IITo2IWWLx7KlZCglD5bJDVZUMFFcEUJM5MVY5X2XC2gIIUUpGthu0jAyTeiL1YOl5nqYzWbWuUfTUmkKK0KIDVcslc12mALa87zrhBCi46tZREypn4ku8rU1xjQNtVoNaZpmSm02m2E6nTqTj+rfZ/wVIcREkRI5ru/qoouLawghJnQb4eMEEs0TBEH2/TRNC83OWfNgicgCgCAIjKPFvNEkRRYhxBeXh1z/W16bMrsTQoiOa9pP/rfFXhX1XAm5tQjlIPq8Y5HSFoQQomOqAqGzyAoeDuoIISqL6hV1inARuxLkbvB/3qu8kaQNGjtCiIqtqr1voWcmFiWEFMFlI1x6RrRPEORKJSPGb+kHCILAO3vpInOdhJDbgy2cwLV93jTgMmV3CCE3m0VK+M1mM0wmkwshUkXsi1VgqQ0IggDr6+vZNGFeNnf1b44uCSE6usGyeacAv9QNhBBiw6RH8nRMGIZYX1+f814VdRpZS+WoKwaDIMDDhw/RbDYvLG3UV/Ew4JQQ4sJUhgIwl71RtzF9bvouB3WEEB3T1B9wsbKEfNZsNvHw4cM5gTWdTpcvlSPTgrJMcTgc4u3bt4iiyOqxcr1HCCE2TGUpTNvkvb9MSQtCyO0hT7vMZjNEUYS3b99iOBzOpWxY2oOleq9qtRoajQZOT09xcnKCKIoQhuGFoC/duNHIEUJ88Kn35QpuZ9A7IcQHXZ/otiIIAoRhiCiKcHJygtPTUzQajbntfPOBAo48WKpSkzQNks09DMPsIK5sysyFRQjRyZsKNG2vfm6LozBtTwghpiB33Q7VajUEQZAlFhXNo3rXi64mdG6tz1lOp1OkaYo0TbNpRP2AujKkuCKE6JgCTG3CSzeGedsTQoiKKTWMimiZ2WyWaZzpdOqMKffRNkYPli6S9B3rU4i2gzHolBCiU2RKz5WjxvY92htCiIrL8aPOsom2UR1HNj3kM7iz1iI0vRaVJ8rOlj6eo0pCiA1byZu8OCvb5z6iixBCbCuRRVhJrUF1dm4ZG2MUWGr8lWrgVENoarj6/aINIYTcHlzpFmzbm9LAmFLFEEKIip4A3TQLJ3+btI76XpE4LKsHy2SoXAc1wQB3QoiOnncGyBdGNu+VLqxcqR5uIoPBAP1+H6enp0iSBP1+f+7zVquFVquFTqeDdruNbreLVqtV2vH7/T7iOEaSJJhMJhgMBnOf5dHtdgEA9+7dy9q5TPv+5V/+pdT+tbX56dOnpbRhd3cXW1tbl3Kuee28beTpFPWfTdcU1TSR60PbvKXJMOorBnXFSAghKkXsgsngmWIhboO9ieMYL1++xOHhIeI4zt02juM5sdPtdvHBBx8s/GBX+eqrr5b6vrRLbV+n08GjR49KaV/V2d/fvxXned2odkFdUWjyVplix12zdy6sHiyJtVIPaFriaFr6mLeMmhBye1mksr3umdKN5G2oRZgkCV68eIFer7fUfvr9Pvr9Pvb397G9vV25B/xgMMDe3h729/exs7ODzc3N627SpRHHMQ4ODvD48ePrbsqNxhQnbkoppW5rGsAVTTRq9WDphisvuZZpdHmT8mD1+30MBoPMFS8jQxVxx0dRhI2NDbRaLXS7XUSR3VHo407e2dm59h/gdU9F5CGj9NPT0+za6Nen0+kgiiJ0Oh3U63V0u110Op1Cx6nCFMRNpGhViKJTjKtOr9fDixcvkCRJafuM4xh7e3t48+YNnjx54rRT10Ecx3j+/Dm2trawu7t73c25NMSLVbX+v0noziB5z+d7phWFvljTNLjiHdTX+ihTXovSW2Vx1ev1cHR0hMPDQ6/t1Ye6+p1Op5O55FfpR1SlqQgbvV4PL1++nIv9sCHbqG0UEfzo0aPCYouUT5GUDbb3V9nmmNjb21vaa+Wi3+/jiy++wNOnTyv5G5Bzv6kiK0mSzFtHLgc1E4I4i0z6xaRx9P2U5sFy5cOyTRuaTmqVRpdJkqDX62F/f7+00eJgMMBgMLh2L1SRPqj6VMTh4SFevHiRK/zyiOMYvV4PvV4Pn376aSUfMLeFonZCNYA3TVQJly2uhCRJ8NVXX1VaZLXb7ZWxoUU5ODjAo0ePrtTrf5uwZT4whTvZ9M4iiY2txZ7VKcK8uUmTuFolUSUcHh7iiy++KN0VD/xmtUzV6fV6+OKLL0o16jIV8dVXX5XSr3t7e3j+/PnS4kpFpjfJ1eO7KtmWb++m5uG7KnEliMjy8QZfB2UMqKrMixcvrrsJNx5bKJP6mUnv5K0utGENctdzReiNke2K1BSrKkmS4Pnz517Lihfl3r17132auazCVMRltXFVBPBNIC8WwmY7XNutejiCzsHBQeH7XKa72+02AGA4HGZpFHxJkgR7e3t4+vRpaeEMMgWv7y+OY5yenhY6zxcvXuDJkydLtUfiRRflsgZih4eH6Pf7tEWXiK9mMdkoicey6SMTzl+QOrJUVxS6Vg7aXGxVRVasXPaoreo/mlWYiihj2tLGxsbGpZ87eY9LDLnEVV45rlWwNz4MBoNC3oxut4vt7W2rjZFpet8B5GAwKDUm6N69e9aVgFtbW9je3sbe3p5X+yQedBmB9OjRo8pONe7v71f+WbGKmPLk2VJJqbZERJW+jV6n0IZxijBNU2c29rxYK/XgVTZ6g8HgSlzisnKtqqzCVES/38fBwcGltekmLwWvOq4UCy5Pl82lv+rs7e15b7uzs4OnT586H8qyKrWIYDo4OLiy6bhWq4UnT55420jfRUerSL/fv1JbfFtQbYWefsonxEANkk/T1FvXePuATQrPJKJWxcjJQ36ZmCBZ9q/u0yQaqjwiWZWpiCIPHeCiqLVdG+DidVyWqk5BVA1bwmITvnZl1TO593o974FH0Szg4rXx9Y7t7+9f2cq9KIqwu7uLL7/8Mnfb09PTK2nTdcHko5ePrmFMseb634tgTdNgOrhpyaK65FFvfJUN3SLiStIt5OVPklWDkuKhqtNPqzIVUUS8bW1tOVMuqPmyDg8PkSQJPvjgg1L7tcpTEFXAVhnCVojV9j1TcHyVPeY+7O/ve223ubm50EP48ePH2b2fR6/Xw/b29pWtbOt0Ouh0OrkCc9UD3Tc3N539z+Sjl4dttaCpxqA+VbiIjbEGucv/pmXQ6kH1RujfqyIHBweFpqc6nQ52dna8PVFiKLa2tkpfjVgmRaci8n7w3W4X3W4XBwcH3sLNZ3ny0dGR1758RvStVgtbW1vY2trCzs4ODg8Pb43HqKqoNcB8sOXeK7qfquGTbw74jbdnUXZ2dtDv971s0+Hh4ZU+6D/44INc21zVVY6+bGxsYDAYOK81k49eDibPlfo+YJ6RW9Sr5UzToL9nOoAutvTvVM3YxXHsPUoE3ntEPv3004Wn+aIoquSPpOhURBEj+/jx40LxHnnXw6ed3W638Ig+iiJsbW1RYF0xrliqIolGTTXEVtmL5TuQWPbBK4MMH16+fHnd3XIjefTokfNzycdIysGWpzMvzMlmc3xxCizbSFF9zzVirJq4AlAogejOzs6NzR58FVMRvoHjvV7POZrzEVirkAaDmMmrI+gyaDdp9aBv8Hbew7nMfZhKTpHl8bGpNz3v11Xj0ikmHaMP+BYZwAXWD/4v34O647z8NMuqvctGsnb7sLm5eWPnwK9yKsJ3pO16uPgI4slkcgk9RS4TPb4hb0CWl6bBtV3V8Z2y63Q6pcREFUmse5n5AXWGw6FXH6w64j3Po8hsC7HjmpVz5cXSpxP1OK08vLY21R00baM3vGr4jhCXFRZV5yZORUjAOlkdfEWRK25C/Vyoqv1x4TtdX+aKZN99+YiesvCx0VUMuViE7e3t3G16vd6VCtzbgG/WAzWD+6I4g9xNSUVt29tyR1TJ2Pk+xIt4XlaRq56K8MlfJVMRptF5t9vNNTJxHGcJTG/ytbvtuOImVtFzJfimHihzRbLvvq4qqLzX63kNkpYNB3j58qX3IFNHFjyVQavVyl1RCDD5aFm46hG6tvfVQSaMTyJ5QKnLGU15r6o2BejCN5YgiqIbnXTyuqYifIx0v983erw6nY7XKG4wGODLL78svbA0uXx8bIkpTYxqBH1KV1QVX+9rmYMH331dhWe4SMqYZacIqxRX9ujRo1yB1e/3WULnEvFJYKwKsSK/QesUYV4dQkENELPlt6kCvm7Wzc3NG+0BWcWpiCJ5qqSwtBSs5rRhdSkyIrSVtTAtxllFT1aVf5eX7cHq9/veeQklyfFNQVLb5GGKxbpJ/XAVmEIMXAv1TDNwRQdxViUhSUT1OAc9Hkt3n+knUhWR5fuglezkN5VVnIrodDpe04QqIrRevHiBzc1NZ/LRsqnKFETV0QdiiyyiMQ3qigaiVoGbPBAwDZwk4e+bN28K/a7LCFu4bvT+2N7ezu0DerGWxzYTp3uoVI2jD+aK2harwDLtPC+XhCvr8nXj+8C7CStUXKzqVMTOzo5XGQ3TPnu9Hnq9HjqdDh49enTp04dVmoJYdWzF5W3byd+kOsjvb1l8V95VHd02iBcrT2QxFmtxTI4iwL6C2aR3FnEYOdM0+OTBUv+Xhq0yN/0GXtWpiE6ns/TKzsFgMDd9SK4Hk+cqL9DUtJL5suqHkWry5MmTGxu+4bOiULxYZHnysrXLNst6sIxbp2nqnVHZ5XKrktha9fIKZbHKUxFbW1ulpM+Q6cNli32TcljEVojgmk6nWSiD5O6r1+vXfUo3iiqImt3d3Rs9+PWNxWJm/cUxVX5Q31e3s1Gr1ZCmafZ3HkaBNZ1OjTkg8hSeHuhepZEkH6Q3g62trdJGsv1+H1988QXF9xVjcsvbvOD636qwStMUURTNCapWq7WSAss3NKHMe9XXG3KdYROSk/AmTA3m4ePFUpNEV0H4rhIufeKaoVO3L7pa2XiFTG4wm6ozxUaYXPmElMXm5ia63S5evHix9FRfkiRZ/qybHn9XZUyJRE1/q+IsCAKkaYo0TREEAVqtFqbT6UrGv11HyoTriMcsQrfbxe7ubinpYlR8Ctdf1/lubW3l2rT9/X3s7u5iY2PDO6fhbceWikG3KXnfA35T5WZhD5YqsEzue5+EXVXzYJHqU8SQy8j26dOnS08dJEmC58+f08t5hbhGjLYgdlM6Bnk/DEMA71dorWLZpOvwYPkK0TJXFPuwubmJp0+f4unTp6WLq6rj68WirSqOj9dK3lfRbY9aRjAP4xMtSRJEUXRhmtCnVI5p5WEVKJLs8ibP9fv2w2AwKM2jc5lTERK7EMcxXr58uXDuqziO8eLFixtdIqlqFMmgrOesmU6nSJIE9XodYRhiNBrh/Pw8s12rhm96mDdv3pTmfXnz5o3Xdpft2e10Ouh0OtjY2LjxeQjzkPJiLi+WrIwm/phSMajvmzB5uWazWWZ3fOJGrVOEpoaYGq02ZJFU8leF7492FacXLqMfVm0qotVqYWdnBzs7Ozg8PMTLly8Lr7jp9XrY3t5eetRc1SmIquBrH2zxEmIowzBEEASYTCaYTCaYTqcXVj+vCkVW2pYhIuM49vaGLSuwdnZ2jPu4yQPZZRAb5rKbL1++5GBwCWxJR3VM6R2K2BjjFKEplsqk/nwaXxVj5/vQ9E3EuarchqkImWJ49uxZYSPOmIbLxzQ16BpR6ukcRFhFUYQ0TefE1arSarW8bVQZ3gvf+7wsj5J4mtV/xEwURblThUUEMimWJ89kixbNs2e0SHk7yovJqmKaBt+H901/wBaZiiiL65qKaLVaePr0aaGR3k0X2FVET/XisifymXjZZarwJuCbpXx/f3+pc06SxFh6xcSyhZXJYmxtbeUKW6ZsKIYtmN0Uc2X7vml7F84hn63+l63hVcb34X3T57eLTkUsy1VORdjY2tryLj9z06eIVwWXPRFxNZlMsgD3KoYlFMW3yHwRgWRib2/P67d90wvfVxlfLxZZjjztYktm7It1FaEpY7LJdZ8nvqpi+DqdjrcLftkRYpW56VMRNh4/fuy1f7rdr4e8UaW+2CZJksKFV6uOBDj7cHBwsNDvs9fref8mt7e3b3XA+XXz+PHjW7eK8rLI0yi++qZoNnfvKcI8D5YtSKxKni3f0ZisKLup3NapCOa5qgZFBl0mG5SmaWbobPmzVhWfZfrC3t5eIZHV6/Wwt7fnte1Nqfu36hS5H4gdWzqGvDhxUwLkUqcITS4yUw0xW3bmKhm9IpXYyypQWkU4FWGHI8bLR1917HpftS9SYUJe+9QyXDVarVahFahS8sm1YjaOYzx//txbXAHvV7LRe3X9bG1tcUFACdhyYNm0jLwnr/Oq2thw/oL00hSuTO2uaPuqGD+fHCMqIhBu2pL7Iv1wcHCAdrtdeDRb9lREHMdLiZ84jr3SNlBgXR22aUCXJ9wVM1Elb/kybG9vo9/ve09XSxHgVquFbrebLWQZDocYDAaFp703NzdvtPfq5cuXODo6WmofnU7HO65zWeR+IItjG8wB9vAEsTUyJeiTcF3H+VSzBbnbRqCrwPb2diHP1IsXL3B0dITt7e0bNZIo0g8y8vU1upcxFfH8+XMA772QReO1JFO7D1w1dXXYMrbrr1XvlU1gVW0wtwxSpaBoMfI4jpf2unc6nRufXymO45UKEJe0FhRZl4vNg27yYPlqH+NTSpJpqUWfbR4rG1XKgaUiCSmLxFjJCLHT6eCDDz5At9t1xvPIyrnT01McHh5iZ2enMlNgaj88fvwYBwcHXtvv7e3hzZs3TqEpsWtFUl34TEUkSZKNwvf29rC3t4fNzU1sbGw4r0WSJDg8PMT+/r63Qa3adbqJ2Ko+uOIk9OTHNm9XlapHLEOn08HTp08Li6wyjsmpweqxs7ODL7/88rqbsfL4ahfbd0QX+WL8JU2n02wnPg3SRZgr6L0KPH78GEdHR4VHBLq7vdPpzBkjVQioLDtaevHixdJB93/wB39w4b1VmYowXafDw8M5IaevjlxklNrtdkuZIly1KYjrQo9/cNkM27RhXoHoVeYqRRbFVbXpdDqFwlvIPDbvk15Cx4Sqb0Qb+a4ktHqw1B1LDJZpO1fAapUN3ZMnT/DVV18ttSzf97tHR0eVjONalakIn+SfZbj9yxI0qzYFcdXYFs3oXiyXDdG301/fFDqdDp49e4bnz59f2hTR48ePb7yYvwkUDW8hFykSiyWf6zFYS68idKk9V2NMJS+qauyiKMLTp0+vZOl+lXMrXcfItegxryL2YHd3l2kcrhGTQHItkda9XjfRgyWIrdrd3S11EUa328Wnn35KcbUiSHgLKY6e5SCv9qD+t2/KKh2jwEqSZM575ROMampk1Q2dGK7LjrtJkqTSHo2rFFlFj2Wbdi2Tra2tG71qqmqYUrsEQZDFWfkUlle/r1PVQd2ybG1t4dmzZ0sNBmRRyaeffnplA0xSHj4ldIgZH11isyeqFioy22O9Uq78V6bG+GxfRaIowpMnT3BwcHCpGdwHg0GlUwBUdSrisr1Xu7u7FFfXgGoroihCo9EovDBmkVU9NwEZEEjqETUGUrdf3W4XURRhY2MDnU7nRq2Evo1ICZ2bnAj7srClkMrLy7eMd9wosMIwXCimQS/KWtWVhCYeP36Mra2tbBVc2UKryh4sQTx6vV6v0Mq7PLrdLnZ2dpYadZd9PbrdbulTLsSfWq2GMAwRRRFmsxnG4zHq9TrCMJzbLi/5qMtA3nSKlNa5LEyLZ257ey67DY8fP65kTG/VWTQ/py7MdBvlwhrk7jJspu30bfWGrQIS9L2zs4Ner7fQSkPTPjc3N1fKFS8j5F6vh5cvXy40RSfn/ejRo6XOfXNzE5ubm3PXY1GxJW2SVBvkepDpwCAIEIYhptMpkiS5MPWhx0zI/6bRp5pahhBCVEyrBU2VZ/TvqNuavpPHQpO5poymJiG2SuJqrlOiKBslSAyQ5LWS1/pDPoqiTEh0Oh3U6/WVd8lXaSpCjZOS1BKTySRriy4C5XpweqSaSLzVZDJBEATecSW2bMpVXlBDCLl+TIM0k2Aq047kWjV1qs/VmFWKuyrUQVGUZdK9DKrg0s6jClMRKpd5PfJYhetVdaIoQq1WQ5IkxlWBwm2NsSKEXD6uKUPV46UXlS+CV7as6XQ6J7R8gt7lNQ0jIURF8uqlaXrB220LOfBJNJqXrJQQcjsxaRFXUHteDlBfcgWWxErYhJUpmJ3CihBiYzweYzabzU0LmmyGKRGpycOlTxPS9hBCTNiSGdty7kmiUd/M7TrOb0nOh1qthjRNrdH3JsFFCCEm0jSdGxXm2Yxl0jcQQohKXuFmdcAmXnbJDVoUZwyWHGg6nRqXJroM302NySKELIeMBnXXe57IAuzG0LUNIYTk2QeT/ZFVzosO2rz8XqLk0jQ1fubTeEIIEUwhBUWr3Kvf4dQgIcQXH90immcZu+IUWKpBC8MQw+Ewd4lj3mtCyO3GZjeKhhfkufoJIQTI1yUmXTMcDudm7hbRMoUit4bDoTX5n95IaSiNHiFEx2YXinqxfD8jhNxebMHtgF3HDIfDpY/rLbDCMMymCPMKrC5aeZoQcvMxearyBmO2Zdb6+7Q3hBAdm6fbpWXSNC1UFseEt8BSa7bZlk+bljwSQojKIrUDbbVNVXuz6hUkCCGXj57TU7dHqv1YtlatdwxWFEVot9sYj8feBmyVij0TQq4WfQVgEWFkqyrBsARCiE6RGoK1Wg3j8RjtdnsuV98iAzfvWoS1Wg2NRgOz2Wwus7sstdbVICGE+OBayeNaUs38e4QQX/QVx7r3SuqjTqdTzGYzNBqNpW2K9xRhrVZDGIZzFev1UhV62QpCCNGxeZ5MdsNVzFmtkSrbmPZPCCHAvDbRq9OI/ZhOpwiCAGEYXq7AUg2a6rWS3BB64BhzYRFCFmGRJIDyvmn1MiGEmDA5hdTXkr1dr0N4acWeAcwVZxWxZRJZ6t8cSRJCdHxX/rmytqv7yduOEEJcAzFVz4i+0Ut6mfaXh7cHazweYzgcol6vzx1Uyl6YAlY5kiSE5OESSj55sdTVgxRXhBATpjAE+Vst5jydTlGv1zEcDjEej+e+r+8vD+8g9zRNMRqN5lxnNlcbIYS4KLKiB7DnyWJiY0LIoqjB7voCvtFoZCwPWARvDxbwXmSpkfa2uAk1aJUQQkwUyebu8k4x0SghJA99UYygOopE1wRBcEFclR6DZYp5iKLowkpC0/amGC1CCAHMpSoEPa6ziICiB4sQomPSI6b4cVlBGEVRKbGdhWoR6tH1LiXIvFiEEBtFijSbsi2rnzH2ihDiQq82I+/p2+jZEpalkMDSG2ULGDNtSwghNlxpGFyoAfK0N4QQGy6PlK5lyrIl3gJLMp3K0kUpgqhXoLY1mBBCAHvOK5fNcHnDaWcIIXmYRJQephCGYZaSKgiCpYVWoUzuEnslYkttmGu6kBBCBNMimCIB74KeDoa2hhBiwleniKiSWKwrK5UjSHB7GIa5LjfTCRFCSF6wqfqZy4ZwIQ0hJA/TIMwU0iQzc64Eo0XwzoMlDRLXGYC51YSu7xBCiGAybCYhpRtFky1hklFCSB55tkE8VgCyUKgy7EkhD5Zep0capnurbCt+CCFEyFsGbRNNpjQOeuF5QghRsekTteagXm95WQoLLLUOoWvptH5ShBAC2GOw8myFyd6YxBRtDiFExWUzVLui1iMsw47kThGqrvvJZJIFuKvqjyNGQkgR9OB033xWNo8WbRAhxBdXVQjRN5PJZG77RQSXU2DpjYjjeM6YMZkoIaQotpiqvHxW+vcYikAIKYJPYXmxL3EcW7fxpVAerDiO0Ww2Lxg6k8HkiJIQ4oPqmvf1RqniylZjjBBCVEwaxfR/s9lEHMe5aWLyyBVYao0eU2oG07YUV4QQG66cV67YTpW84HdCCDHhk0JKUjaYai6r2+RRKMg9ii7OKPq49AkhJA+XR1yFAzlCSFFc9sSkYUx6J29fF/ZhelPN2K5iynmVlyGVEEJUFhFGJq8W08IQQnwp6gjS9Y58v0gJHaMHq9Vq5TbEFOxu2p6jTEKIL7bUDS47UiR2ixBy+3Dl1LNlRLDZE5s+MmEUWOPx2Lix6jLLCyqlsSOEmHCtFjQVjLdtq3vP86YWCSG3G5dt0O2MbYrQpo9MFBJY6+vrxsbaCq7SZU8I0dE9TnneJ1VouUahnCokhNgwJRtVbYpuW3S9IywtsGz1Bbvd7oVG6Q2ncSOE5OGbtT1vG3qrCCFFsWkW1VkkekenSCFoo8BqNBpZVWk56HQ6RavVMga1mxIAUmgRQkzYyuT4lrPQ92GqLUYIITqmuE6Ts6hWq6HVas3VWgaAMAzRaDS8j2cUWEmSZJWl1YOK6HKJLPnbJ9cEIeT24VoQYxNHpkLQek1D2hxCiAmfQHZdeIneUe1JEARIksT7uEaBlabpherSahyEqeG29POEEKKSl2jUtr2vPaHdIYTo2Epzyd/6Z6YaqdPpFGmaeh/TO5O72si8Qs8cQRJCbJi8UfK/K0O7avRs+5LtCCHEhG1FsivtlGsVswujwFJdY6ZYCZk+VGMndKPHeAhCiA++i2T01YK24qu0OYQQFZt4Um2KnkjUFvxumjq0YY3BUmvw6AezLXdUP2cBVkKIC91mLFLn1BRDQZtDCFFRHUEmLePKiqDapul0unwMlslV74rD4oiREFIUU+1Bm2veFNSetz9CCMnDZHNsi26KVozwFljqwdXtTGLLlRSQEHK78cnKrr9v+r4+urQNAAkhRNUlNlGlvrbtY2mBpRssW0kK3eVmi8MihBDBJ8eVjs+AjSEJhBAbpvgr9X2TvpHXi6aeyvVguZL/2UaXNHKEEBe2dC++3nNCCFmEvNqmptc+9smENU3DdDp15qtR/zc1lMaQEGLCFGwKXLQb+iplV1yovCaEEBsuT3ierpnNZnNlcpZK02AaYZpSzJuWTDP+ihBSBNfiGVtuLDVdjFp5ghBCTNh0iq0ihEkHLZ2mQW2MfnD9YHlCikKLEKJiE06uJKM6s9kMaZrOLZ9Wq08QQojgo1Ns04bL5PS0Cix96XQRMWXycBFCiA2fPFimz4MgmPNkqf8TQghg9lD5iq5lwhC8S+XoYklfEm0LWiWEEBOmVTq+xZ6B9+IqiiJEUTQntAghxISrlrJuf/QpxEUwWiQxVnnLpgF7AlIGnhJCXOixDnlGzLTEejqdol6vIwxDTKdTiixCyAVMesSUUNSldSTWs4iNMW6Zpimm02luAUR53xYQTy8WIURHH7i5qkS4SNMUcRxjMpmgXq+j0WjMrfIhhBDBpktcaRt0GzWdTpGmqfcxrVIsL9Je3cblvaLIIoSo2EaTrtGj/n0RUmEYIo5jpGmKtbU1RFFEm0MImcMUg6W/b6tS4wqFysM6RWhb4WOLwzJtT0NHCNFx2QXfJKNic5rNJsIwxHA4xGQyyUQWIYSouHSKKf5KPte3X3qKUE0ymieUfEedhBBiwmZrfEadUtk+CAKcn58jjmN0Op3rPiVCSEUpqll0L1eRMARrDJaprmCeF0t9jxBCTPjmuzIJLn17yYUlryeTSSa6CCFExxborr4n25nqF0qM+sKJRlXRVMQbRWFFCMljUZtiE2Fiq2Tl82AwuO5TJIRUnCJ6xVWyy0XgszPTqh/1tfq9ZTOfEkJuHz6ii6EHhJBF0RfruQrOq691LVQk4N1ai1AN5DLV6FFfmz5TG0YIIS587ETewhuxN1IrjBBCAHu4gUvDmOI/gyAoZF+cQe56NXuXB4sxWIQQX3y9UUUW2IitKpKnhhByuzDFW6l/mzxd6r+lg9wBzCUa1QWT3iiTuGJdMEKIDZdNUd8rUqqC1SMIISZsYU7yns1ZJNuqiUb177uw5sEyNU49mOk1c18RQhYhb2WhT2FWMX7Mg0UIcaGngLFpGtlWRS0jmIdXsee8TKbqNrY4CUIIAXAhWNTXRthiOtVVhMD7VA2EECK4Eozm1R9cxnFkFVi6YNILs6rvLZNKnhBy+7Atd86Lj8jzpgdBgHq9ft2nRwipMKZSgD4ap4hXHXDkwdIDvCR6Xtzv4o6XgHhTRWp6rwghOq5iq7bFMrZC82qsaJIkCMMQa2tr132KhJCKYdInonFUPQO8DzOQbAr6Qr8icaFGgTWZTIxGMI5jAL/JnqxmM2XJHELIIpiqQQiuKvdqclEAaLVaqNfrGI1G131KhJCKkTcVKHpGViHHcWzUQUmSeCcctXqwdKbTKX79619naeKDIMhGjKbvFFF5hJDbgyknjS1LsishoJqTpl6vo9VqIU3TbCBICCGAu3gz8D73Z5IkCIIA0+kUaZri17/+tTElg03zmLDmwdKp1Wr4+uuvs1o84/E4c8/bToJThIQQH1wDMlvCP5kWbDabqNfriOMYo9GIdocQcgFbwnR5bzqdYjweZ7WYRe/Y9rPwFKHNyO3t7eH09BTA+yr20+k0c5e54ioIIUQoGixqCkOQUWaj0QAAjMfjbNBHu0MI0TFpErFFqp4BgNPTU+zt7TlXLQ+Hw9xjenuwAKDX6+EXv/gFGo1G5lKT6cLZbJYFhqmJuTiaJIS4ULMkmz6zecTr9XoWqiCGUTxbhBAi6JpEFuxJHKeIqzAM0Wg08Itf/AK9Xs+6rzAM8e7du9zjWoPcTcRxjB//+McAgPPzc0wmEwyHQ6Rpinq9jiRJsgB4iitCiIkidsGUcVlSMaiDPHV72h1CiI66mCZNUyRJgnq9jjRNMRwOMZlMcH5+DgD48Y9/bI3lFO/VwgLLtgqn0WjgJz/5CX72s5/h/v37ODs7w3g8xunpKdI0RRRFF0aQNHaEEBVTKQrT3zoipGT5tAzmXGUuCCHElC8viiKkaYrT01OMx2OcnZ3h/v37+NnPfoaf/OQnWfiBaV9xHGdecxdGgSUqzrTjs7Mz/PCHP8S7d++wvr6O6XSK09NTHB8fzy2bpqEjhPigerxddiMMw2wFj8Rg6d4r2R8hhOjo6V2Oj49xenqK6XSK9fV1vHv3Dj/84Q9xdnbmHOzFcexV9NkosE5OTqxerHa7ja+++go/+tGP5uKtzs7O8Pr1awwGAwBcSUgIMaPXAFO93rb4zVqthnq9jkajMReMqu+XU4SEEB1djwwGA7x+/ToTUhJX9aMf/QhfffUV2u22dV9pmnpNDwJACOD/6W9Op1Ocn5+j2+1eKPwMAM1mEz//+c8RxzE++eQTNJvNbEQ5mUyypY6qWiSEEAD45ptvEEURWq1W5pWSaT+9NEWappktGY/HiOMYcRxnyZAnkwmSJMF4PMZkMslGld/61reu+zQJIRVhMplkMeODwQDD4TDLe9VsNpEkCf7mb/4G//iP/4j19XWrFzwIgsyZ5OMpt5adHw6HODw8xNbWlvEgzWYTf//3f49er4c/+7M/w/b2Nl69eoW7d+9iNpvh5OQka8BHH3103f1LCKkIx8fHGI1GGA6HWFtbQ6vVQhRFiKIIzWYTzWYzE12Sd0+ElaRjUIWW5K+Rz6WcFyGEAMCrV68AvB+4NZtNNBoNHB8f48MPP8T+/j7+6q/+Cv/6r/+KZrOZxXfqSDqHb775xmt6EABqAKwyLIoi/M7v/A7W19fNX/6/YK8PPvgAn3/+Of7oj/4InU4nU4vj8RhJkmA2m2XGUseWYVXPlaNvbz2h/5tykOONRqO5zM+SUOy6kXw97Bc3phpQ7L/l+w94H9N0WW2WY6Vpin6/n61MPj8/x/n5OdbW1rIEoa1WKzN6zWYzS/ciaRhmsxnOz88xHA4zYTYejzGbzTAajTIPlqwECsMQ9+7dy2oS1ut1dLvdLH7rsmK0TIbZ1f9VQPpX7gN5AIn30CevmO33ZPrtubaXBI/qjAf7b7n+Y7/8pl+iKEKj0UC9Xke9XsdgMMA//dM/4R/+4R/w5s0btFota5tkJu7ly5fo9XrlCCzgvcja3NzEw4cPjQ+zWq2G09NTfPDBB/j444/x8ccf49NPP8W9e/dw586drHyFq2OKdKr8LRdQ3V5dti2dKVMLkq9Lcl7o+7YVc9SPoWd/Vb9na7/pfMIwZL9Yzkf9js3AqqvI2H/F+0/93NUfPg842zUQ43Z0dITJZIJGo4GTkxMMBgOsra2h3W6j1WrNCSwRVSK0JN5qNBplHivdkyWDudFohDRNEYYh7t+/jzt37mA8HqNer+PevXtzMaN57fe9jnqcmC2Roeva+FxX232h3xt6ipy86yj3rdzHjUYDtVotGxyrq630Y8hD0tYfpnvb1Gb1u7roZ/8V7z8RFOyX+X6J4xgnJyc4OjrCl19+ia+//hpff/013rx5g42NDeNvV84jiiIcHBzg9evXRu+WjVyBJY1uNBr49re/nbnQVNQR/Pn5OZrNJjY3N7G9vY3NzU1EUTSnrPMupn7x9c5S47pcRWJrtRo6nQ5GoxGOjo4yI246vn6+1+WNkPNVvYBqW+/cuYMwDDEcDrNkaT79YDvnIg9kG2EYzq0cDcMQd+/eRZIkOD4+zu4PNSO3ei8sQtkrVW0rYE9PT/HRRx+h3W7j7du3mEwmxgDsZY6r7yNP6Mj2+uhwOBzi+Pg4G6GJJ0i2sT2cTGLPJVr11XrqvnXDWqvVcH5+ng0qRBxJG+Ue1h8O9+/fRxiGmcdK8tYAyPYPIMu9J8lG5bgi3GR/a2tr2YhaF8QmIZtnm3yul75vaaf8tu/evYt2u33BG5r3e7SJ8zLuQ/l7NpuhXq/jwYMHGA6H+N///V9sbGzM9Yncg2V6lpZZpCDfU9skfTedTnH37l1EUYTj4+PsISnH831o5gk+vT159lT9ntzL7XYbaZri5ORkbhvV+6Jfs6tEvUdt5yNiajQa4d69e2g2mxgMBl79INfP9z63DU5lX7PZ+wLNh4eH2N/fx+HhIUajEdbW1i542tR9qs+Dt2/f4ptvvvFKzTB3XvAQWEK9XsdHH32E+/fvGz8XpSc3qwSeSiLSsn6IrptbN5CtVgv1ej0Lwhdj5ut+1N8vOpIvel5yUWU6ShUnkmdM+lRuDJvYuCq3unocMbrtdht3797NHvqqZ0gecup1uoz+9EU16urUI/B+1Wy73cZwOMyEwjL9JMcrAzFEIiCiKJoL9JYyVqoguQ5cnjPpb0n4V6/X0W63EUUR3rx5kwkh9f4STFMhJoFken2ViKCT37C8J8mZ5b5SBx7LUsa9JveV3P9SGkSfiip7sOM6J9f5qL9h3d6ImD0+PsZwOJwTCddhJ/V2S3/KQEOeWWopOjU1SRni1vdZtujzUmIp5ZkliTtdA7Syp4Nns1kWNiCrkIH54s6m66He09988w0ODw8Lea6yfaGAwJKGPXjwAL/1W791oXEyEpCl1HLjyPs2l95l/jhdSvsqjl/0GPqNXeQYVcDmnVTvB9O5VqXN4kWxcVntXfQ+tAVkXlW7y0DK3ZhGpjLQWMS4VQWf67poPNxl2i/XflVvY9V+x+prddCq91kV2izt9MVnoG/6zlU+4/KeuZd9fP19dYZF7ofxeJy9rzMejwG8n0Xa39/3znllbBcKCizgvUHc3NzE48ePM/EkHqrJZFKJIDvAfeNW5cdlavOiP4jrPqdFf8TX2e6ibS57OmTZPvONtyi77WWgehpM1Ot1pyFcBYoMrJYNwi/7QXqdv43LbmsV2rxou6smEIucU1XaPJ1OUa/X5zxcMgNwfHyMV69e4fz8fGnP/0ICSzoqiiJ0Oh08ePAgS8wly611N1uZUxRXociv+rhqf5V9Ey7iEbvu/qjC8S6j/UKeEco7T5luklW66qBm2et9Ffe5z3HE86kLDz3+7Ko942UIY/X74t2V65n3+/fpN2HVfy+rbl8uy/aqQedVvM+rdFw1Xlv2LVOt4jmWBTNlCavsXLCgwFKRlVWtVgsbGxtZ8Jh4tfQVCK7Atbx5YUE6xrS9Lu5cAfX6ts7OMpyL7ha1zbHr26jxAeo2ywbY26bg9P7VS4zoP9i8qUrfVR2+bXZdB9cDx9RuV3+Ip0BiGpaNUbEFtur3CXAx+NbHkNgWKMhqOTWmTR3M6DFuYkzSNHV6mF33r35fuqZ7Tfeefs+5PFgSkyKxEr6xevp2ej/kBfTnfV8/rg1Tf+sxQqZ+kOuad/1t1079DejCW78vbffuosjxZFbDFBKgH893YGm7R4rYb1uf+R5LPjO1W/2OvhLZde8tih5Dph/XtWhDP77pGVFkJsV0P5mmZG3bqn2jhzz4xojp26p9o24rvy85xmw2w+npKV69eoXRaFR6rGopAkuQYM56vY719XW0Wq1MeEkHqFH7ErStZn1XC7kCvwlGUy+O2nHqj1rtWDVJoXrxJFBQjQ8Tb4Ba20y9CdRVR6rBlP2qYk9dPaULQPV46sNWF4ONRsM5dWJDD4JU+1HtO934qf0uAdF6DJ2co5y/KtL0YFcZjavbmq6f2mb5W7bR/1YfSuo+1Xwq6r71a5Dd8Er/i1tYCpXniS1xJ9seDOr1lqBtW7CofFeCWdXvqvvOC+RWR2T6CK3dbmNtbQ1hGOLo6Ci7Hqo4U/ta+kU9jvpPvXfUFTbqvar+bk0P9Hq9ngXir6+vo1arza3MUUeX6vnr96N+/mohaHH/y7XS26G2Uc5Tv9/0KTv1n2rvZP9qf0j/yj127949pGma5fKSvldtnj7tY/rtq21Uz0nsk77C1XTvhWGYZcGX76qfq/v2WZg0m71foSV2Wv3N67871fabHo5qW+R6qqs+9ftNvT/Efum/D/VBK9dDBLvaV6otU7fVn09yTP2ZoB9Tv3fUe1P2obbZF7k/JQ+c+juV46n9r4s79fxkO1PKG/X81UUy+nNR+lM9nv48Ue89+X3o18n2+5ZjqYvn1GeR/nySwHX9+aTae1lxfHJygtevX2e/h8ugVIFFCCGEEEIsxZ4JIYQQQsjiUGARQgghhJQMBRYhhBBCSMlQYBFCCCGElAwFFiGEEEJIyVBgEUIIIYSUDAUWIYQQQkjJUGARQgghhJQMBRYhhBBCSMlQYBFCCCGElAwFFiGEEEJIyVBgEUIIIYSUDAUWIYQQQkjJUGARQgghhJQMBRYhhBBCSMlQYBFCCCGElAwFFiGEEEJIyVBgEUIIIYSUDAUWIYQQQkjJUGARQgghhJQMBRYhhBBCSMlQYBFCCCGElAwFFiGEEEJIyVBgEUIIIYSUDAUWIYQQQkjJUGARQgghhJQMBRYhhBBCSMlQYBFCCCGElAwFFiGEEEJIyVBgEUIIIYSUDAUWIYQQQkjJUGARQgghhJQMBRYhhBBCSMlQYBFCCCGElAwFFiGEEEJIyVBgEUIIIYSUDAUWIYQQQkjJUGARQgghhJQMBRYhhBBCSMlQYBFCCCGElAwFFiGEEEJIyVBgEUIIIYSUDAUWIYQQQkjJUGARQgghhJQMBRYhhBBCSMn8f8kzn+4/fE6rAAAAAElFTkSuQmCC";
        }

        private void SpawnDeployableSign(string prefab, Vector3 pos, Quaternion angles)
        {
            var newItem = ItemManager.CreateByName(prefab);
            if (newItem?.info.GetComponent<ItemModDeployable>() == null)
            {
                return;
            }
            var deployable = newItem.info.GetComponent<ItemModDeployable>().entityPrefab.resourcePath;
            if (deployable == null)
            {
                return;
            }
            var newBaseEntity = GameManager.server.CreateEntity(deployable, pos, angles);
            if (newBaseEntity == null)
            {
                return;
            }
            newBaseEntity.SendMessage("InitializeItem", newItem, SendMessageOptions.DontRequireReceiver);
            newBaseEntity.Spawn();

            var stream = new MemoryStream();
            var stringSign = Convert.FromBase64String(SignTexture());
            stream.Write(stringSign, 0, stringSign.Length);

            var sign = newBaseEntity.GetComponent<Signage>();
            sign.textureID = FileStorage.server.Store(stream, FileStorage.Type.png, sign.net.ID);

            stream.Position = 0;
            stream.SetLength(0);

            newBaseEntity.SetFlag(BaseEntity.Flags.Locked, true);
            newBaseEntity.SendNetworkUpdate();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// HUD
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public string doorsoverlay = @"[
		                {
							""name"": ""RemoteOverlay"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 0.8"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                },
                                {
                                    ""type"":""NeedsCursor"",
                                }
                            ]
                        },
                        {
                            ""parent"": ""RemoteOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""REMOTE ACTIVATOR"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleLeft"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.99"",
                                    ""anchormax"": ""1 0.89""
                                }
                            ]
                        },
                        {
                            ""parent"": ""RemoteOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Close"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 0.10""
                                }
                            ]
                        },
                        {
				            ""parent"": ""RemoteOverlay"",
				            ""components"":
				            [
					            {
						            ""type"":""UnityEngine.UI.Button"",
						            ""command"":""remote.close"",
						            ""color"": ""0.5 0.5 0.5 0.2"",
						            ""imagetype"": ""Tiled""
					            },
					            {
						            ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 0.10""
					            }
				            ]
			            },
                        {
                            ""parent"": ""RemoteOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Close"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.4 0.60"",
                                    ""anchormax"": ""0.5 0.70""
                                }
                            ]
                        },
                        {
				            ""parent"": ""RemoteOverlay"",
				            ""components"":
				            [
					            {
						            ""type"":""UnityEngine.UI.Button"",
						            ""command"":""remote.cmd close {remoteid}"",
						            ""color"": ""0.1 0.1 0.1 0.9"",
						            ""imagetype"": ""Tiled""
					            },
					            {
						            ""type"":""RectTransform"",
						             ""anchormin"": ""0.4 0.60"",
                                     ""anchormax"": ""0.5 0.70""
					            }
				            ]
			            },
                        {
                            ""parent"": ""RemoteOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Open"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
						             ""anchormin"": ""0.50 0.60"",
                                     ""anchormax"": ""0.60 0.70""
                                }
                            ]
                        },
                        {
				            ""parent"": ""RemoteOverlay"",
				            ""components"":
				            [
					            {
						            ""type"":""UnityEngine.UI.Button"",
						            ""command"":""remote.cmd open {remoteid}"",
						            ""color"": ""0.1 0.1 0.1 0.9"",
						            ""imagetype"": ""Tiled""
					            },
					            {
						            ""type"":""RectTransform"",
						             ""anchormin"": ""0.50 0.60"",
                                     ""anchormax"": ""0.60 0.70""
					            }
				            ]
			            }
                    ]
                    ";
        public string getaccessoverlay = @"[
                        {
                            ""parent"": ""RemoteOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Access"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
						             ""anchormin"": ""0.40 0.50"",
                                     ""anchormax"": ""0.60 0.60""
                                }
                            ]
                        },
                        {
				            ""parent"": ""RemoteOverlay"",
				            ""components"":
				            [
					            {
						            ""type"":""UnityEngine.UI.Button"",
						            ""command"":""remote.cmd access {remoteid}"",
						            ""color"": ""0.1 0.1 0.1 0.9"",
						            ""imagetype"": ""Tiled""
					            },
					            {
						            ""type"":""RectTransform"",
						             ""anchormin"": ""0.40 0.50"",
                                     ""anchormax"": ""0.60 0.60""
					            }
				            ]
			            }
                    ]
                    ";
        public string adminoverlay = @"[
                        {
                            ""parent"": ""RemoteOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Reset Doors"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
						             ""anchormin"": ""0.40 0.30"",
                                     ""anchormax"": ""0.50 0.40""
                                }
                            ]
                        },
                        {
				            ""parent"": ""RemoteOverlay"",
				            ""components"":
				            [
					            {
						            ""type"":""UnityEngine.UI.Button"",
						            ""command"":""remote.cmd reset {remoteid}"",
						            ""color"": ""0.1 0.1 0.1 0.9"",
						            ""imagetype"": ""Tiled""
					            },
					            {
						            ""type"":""RectTransform"",
						             ""anchormin"": ""0.40 0.30"",
                                     ""anchormax"": ""0.50 0.40""
					            }
				            ]
			            },
                        {
                            ""parent"": ""RemoteOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Add Doors"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
						             ""anchormin"": ""0.50 0.30"",
                                     ""anchormax"": ""0.60 0.40""
                                }
                            ]
                        },
                        {
				            ""parent"": ""RemoteOverlay"",
				            ""components"":
				            [
					            {
						            ""type"":""UnityEngine.UI.Button"",
						            ""command"":""remote.cmd add {remoteid}"",
						            ""color"": ""0.1 0.1 0.1 0.9"",
						            ""imagetype"": ""Tiled""
					            },
					            {
						            ""type"":""RectTransform"",
						             ""anchormin"": ""0.50 0.30"",
                                     ""anchormax"": ""0.60 0.40""
					            }
				            ]
			            },
                         {
                            ""parent"": ""RemoteOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Reset Access"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
						             ""anchormin"": ""0.40 0.20"",
                                     ""anchormax"": ""0.50 0.30""
                                }
                            ]
                        },
                        {
				            ""parent"": ""RemoteOverlay"",
				            ""components"":
				            [
					            {
						            ""type"":""UnityEngine.UI.Button"",
						            ""command"":""remote.cmd resetaccess {remoteid}"",
						            ""color"": ""0.1 0.1 0.1 0.9"",
						            ""imagetype"": ""Tiled""
					            },
					            {
						            ""type"":""RectTransform"",
						             ""anchormin"": ""0.40 0.20"",
                                     ""anchormax"": ""0.50 0.30""
					            }
				            ]
			            },
                        {
                            ""parent"": ""RemoteOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Give Access"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
						             ""anchormin"": ""0.50 0.20"",
                                     ""anchormax"": ""0.60 0.30""
                                }
                            ]
                        },
                        {
				            ""parent"": ""RemoteOverlay"",
				            ""components"":
				            [
					            {
						            ""type"":""UnityEngine.UI.Button"",
						            ""command"":""remote.cmd giveaccess {remoteid}"",
						            ""color"": ""0.1 0.1 0.1 0.9"",
						            ""imagetype"": ""Tiled""
					            },
					            {
						            ""type"":""RectTransform"",
						             ""anchormin"": ""0.50 0.20"",
                                     ""anchormax"": ""0.60 0.30""
					            }
				            ]
			            }
                    ]
                    ";

        void ShowUI(BasePlayer player, Vector3 remotePos, string ttype)
        {
            RemoteActivator ract = remoteActivators[remotePos];
            if (ract == null)
            {
                SendReply(player, "Invalid Remove Activator");
                return;
            }
            var doverlay = doorsoverlay.Replace("{remoteid}", string.Format("'{0}' '{1}' '{2}'", remotePos.x, remotePos.y, remotePos.z));
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(doverlay));
            if (ttype == "admin")
            {
                var aoverlay = adminoverlay.Replace("{remoteid}", string.Format("'{0}' '{1}' '{2}'", remotePos.x, remotePos.y, remotePos.z));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(aoverlay));
            }
            if (allowAuth.ContainsKey(remotePos) && (Time.realtimeSinceStartup - allowAuth[remotePos] <= 15))
            {
                var goverlay = getaccessoverlay.Replace("{remoteid}", string.Format("'{0}' '{1}' '{2}'", remotePos.x, remotePos.y, remotePos.z));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(goverlay));
            }
        }

        void OnUseSignage(BasePlayer player, Signage sign)
        {
            Vector3 targetPos = new Vector3(Mathf.Ceil(sign.transform.position.x), Mathf.Ceil(sign.transform.position.y), Mathf.Ceil(sign.transform.position.z));
            if (remoteActivators[targetPos] == null) return;
            if (remoteActivators[targetPos].owner == player.userID.ToString())
                ShowUI(player, targetPos, "admin");
            else if (remoteActivators[targetPos].autorizedUsers.Contains(player.userID.ToString()) || (allowAuth.ContainsKey(targetPos) && (Time.realtimeSinceStartup - allowAuth[targetPos] <= 15)))
                ShowUI(player, targetPos, "normal");
            else
            {
                SendReply(player, "You are not allowed to use the remote doors here");
            }
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.USE))
            {
                if (Physics.Raycast(player.eyes.HeadRay(), out cachedHit, 1f, signColl))
                    if (cachedHit.collider.GetComponentInParent<Signage>() != null)
                        OnUseSignage(player, cachedHit.collider.GetComponentInParent<Signage>());
            }
        }

        class RemoteDoorAdder : MonoBehaviour
        {
            BasePlayer player;
            float lastUpdate;
            public Vector3 remoteActivate;
            InputState inputState;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                lastUpdate = Time.realtimeSinceStartup;
                Invoke("DestroyThis", 60f);
            }
            void FixedUpdate()
            {
                if (!player.IsConnected() || player.IsDead()) { Destroy(this); return; }
                inputState = serverinput.GetValue(player) as InputState;
                if (inputState.WasJustPressed(BUTTON.FIRE_PRIMARY))
                {
                    float currentTime = Time.realtimeSinceStartup;
                    if (lastUpdate + 0.5f < currentTime)
                    {
                        lastUpdate = currentTime;
                        TryAddDoor(player, remoteActivate);
                    }
                }
            }
            public void Refresh()
            {
                CancelInvoke("DestroyThis");
                Invoke("DestroyThis", 60f);
            }
            void DestroyThis()
            {
                PrintToChat(player, "You are done added new doors");
                Destroy(this);
            }
        }
        static void PrintToChat(BasePlayer player, string message)
        {
            player.SendConsoleCommand("chat.add", 0, message, 1f);
        }
        static void TryAddDoor(BasePlayer player, Vector3 remoteActivate)
        {
            if (remoteActivators[remoteActivate] == null)
            {
                Debug.Log("This remote activator doesnt exist");
                return;
            }
            BaseEntity foundEntity = FindRayStructure(player.eyes.HeadRay(), doorColl);
            if (foundEntity == null)
            {
                PrintToChat(player, "Couldn't find a door");
                return;
            }
            if (foundEntity.GetComponent<Door>() == null)
            {
                PrintToChat(player, "You are not looking at a door");
                return;
            }
            if (remoteActivators[remoteActivate].listedDoors.Count >= maxDoors)
            {
                PrintToChat(player, "You've reached the max doors allowed per remote activator");
                return;
            }
            Door door = foundEntity.GetComponent<Door>();
            if (!door.HasSlot(BaseEntity.Slot.Lock))
            {
                PrintToChat(player, "This door doesn't have a lock");
                return;
            }
            if (Vector3.Distance(remoteActivate, door.transform.position) > float.Parse(maxDistance.ToString()))
            {
                PrintToChat(player, "This door is too far from the remote activator");
                return;
            }
            BaseEntity baselock = door.GetSlot(BaseEntity.Slot.Lock);
            if (baselock == null)
            {
                PrintToChat(player, "This door doesn't have a lock");
                return;
            }
            CodeLock codelock = baselock.GetComponent<CodeLock>();
            if (codelock == null)
            {
                PrintToChat(player, "This door needs a Code Lock");
                return;
            }
            List<ulong> whitelistcodelock = fieldWhiteList.GetValue(codelock) as List<ulong>;
            if (!whitelistcodelock.Contains(player.userID))
            {
                PrintToChat(player, "You must have access to this code lock to add this door.");
                return;
            }
            Vector3 goodPos = new Vector3(Mathf.Ceil(foundEntity.transform.position.x), Mathf.Ceil(foundEntity.transform.position.y), Mathf.Ceil(foundEntity.transform.position.z));
            foreach (RemoteDoor rdoorr in remoteActivators[remoteActivate].listedDoors)
            {
                if (rdoorr.Pos() == goodPos)
                {
                    PrintToChat(player, "This door is already listed");
                    return;
                }
            }
            storedData.RemoteActivators.Remove(remoteActivators[remoteActivate]);
            remoteActivators[remoteActivate].listedDoors.Add(new RemoteDoor(goodPos));
            storedData.RemoteActivators.Add(remoteActivators[remoteActivate]);
            PrintToChat(player, "Added a new door");
        }

        static BaseEntity FindRayStructure(Ray ray, int currentCol)
        {
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 3f, currentCol))
                return null;
            return hit.GetEntity();
        }
        [ConsoleCommand("remote.close")]
        void ccmdRemoteClose(ConsoleSystem.Arg arg)
        {
            var player = arg.connection?.player as BasePlayer;
            if (player == null) return;
            CuiHelper.DestroyUi(player, "RemoteOverlay");
        }
        [ConsoleCommand("remote.cmd")]
        void ccmdRemoteCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(4)) return;
            var player = arg.connection?.player as BasePlayer;
            if (player == null) return;
            var arg1 = arg.GetString(0).Replace("'", "");
            var x = arg.GetString(1).Replace("'", "");
            var y = arg.GetString(2).Replace("'", "");
            var z = arg.GetString(3).Replace("'", "");

            Vector3 targetPos = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
            if (remoteActivators[targetPos] == null) return;

            if (Vector3.Distance(remoteActivators[targetPos].Pos(), player.transform.position) > 3f)
            {
                SendReply(player, "You are too far from the remote activator.");
                return;
            }
            switch (arg1)
            {
                case "add":
                    if (remoteActivators[targetPos].owner != player.userID.ToString())
                    {
                        SendReply(player, "Only the owner of this remote activator can add new doors");
                        return;
                    }
                    RemoteDoorAdder remoteadded = player.GetComponent<RemoteDoorAdder>();
                    if (remoteadded == null)
                        remoteadded = player.gameObject.AddComponent<RemoteDoorAdder>();
                    remoteadded.remoteActivate = targetPos;
                    remoteadded.Refresh();
                    SendReply(player, "You are now adding doors for 1 minute");
                    break;
                case "reset":
                    if (remoteActivators[targetPos].owner != player.userID.ToString())
                    {
                        SendReply(player, "Only the owner of this remote activator can reset the doors");
                        return;
                    }
                    storedData.RemoteActivators.Remove(remoteActivators[targetPos]);
                    remoteActivators[targetPos].listedDoors.Clear();
                    storedData.RemoteActivators.Add(remoteActivators[targetPos]);
                    SendReply(player, "You've cleared all the doors");
                    break;
                case "open":
                case "close":
                    if (!remoteActivators[targetPos].autorizedUsers.Contains(player.userID.ToString()))
                    {
                        SendReply(player, "You dont have access to this remote activator.");
                        return;
                    }
                    foreach (Collider col in Physics.OverlapSphere(targetPos, float.Parse(antiTrapDistance.ToString()), playerColl))
                    {
                        BasePlayer tplayer = col.GetComponentInParent<BasePlayer>();
                        if (tplayer == null) continue;
                        if (!tplayer.IsConnected()) continue;
                        if (!remoteActivators[targetPos].autorizedUsers.Contains(tplayer.userID.ToString()))
                        {
                            SendReply(player, "Someone that doesn't have the authorisation to use this switch is near, you must wait for him to leave or give him access.");
                            return;
                        }
                    }
                    bool shouldopen = arg1 == "open" ? true : false;
                    List<RemoteDoor> toDelete = new List<RemoteDoor>();
                    foreach (RemoteDoor rdoor in remoteActivators[targetPos].listedDoors)
                    {
                        if (!rdoor.OpenDoor(shouldopen))
                        {
                            toDelete.Add(rdoor);
                        }
                    }
                    if (toDelete.Count > 0)
                    {
                        storedData.RemoteActivators.Remove(remoteActivators[targetPos]);
                        foreach (RemoteDoor rdoor in toDelete)
                        {
                            remoteActivators[targetPos].listedDoors.Remove(rdoor);
                        }
                        storedData.RemoteActivators.Add(remoteActivators[targetPos]);
                    }
                    break;
                case "access":
                    if (!allowAuth.ContainsKey(targetPos))
                    {
                        SendReply(player, "You are not allowed to get access to this remote activator.");
                        return;
                    }
                    if (Time.realtimeSinceStartup - allowAuth[targetPos] > 15)
                    {
                        SendReply(player, "You are not allowed to get access to this remote activator.");
                        return;
                    }
                    if (remoteActivators[targetPos].autorizedUsers.Contains(player.userID.ToString()))
                    {
                        SendReply(player, "You already have access to this remote activator.");
                        return;
                    }
                    if (Vector3.Distance(remoteActivators[targetPos].Pos(), player.transform.position) > 3f)
                    {
                        SendReply(player, "You are too far from the remote activator." + Vector3.Distance(remoteActivators[targetPos].Pos(), player.transform.position));
                        return;
                    }
                    storedData.RemoteActivators.Remove(remoteActivators[targetPos]);
                    remoteActivators[targetPos].autorizedUsers.Add(player.userID.ToString());
                    storedData.RemoteActivators.Add(remoteActivators[targetPos]);
                    SendReply(player, "You now have access to this remote activator.");
                    break;
                case "giveaccess":
                    if (remoteActivators[targetPos].owner != player.userID.ToString())
                    {
                        SendReply(player, "Only the owner of this remote activator give access to other players");
                        return;
                    }
                    if (allowAuth.ContainsKey(targetPos)) allowAuth.Remove(targetPos);
                    allowAuth.Add(targetPos, Time.realtimeSinceStartup);
                    SendReply(player, "People that use the remote activator will be allowed to auth in the next 15 seconds");
                    break;
                case "resetaccess":
                    if (remoteActivators[targetPos].owner != player.userID.ToString())
                    {
                        SendReply(player, "Only the owner of this remote activator reset the access");
                        return;
                    }
                    storedData.RemoteActivators.Remove(remoteActivators[targetPos]);
                    remoteActivators[targetPos].autorizedUsers.Clear();
                    remoteActivators[targetPos].autorizedUsers.Add(player.userID.ToString());
                    storedData.RemoteActivators.Add(remoteActivators[targetPos]);
                    SendReply(player, "You have reseted the remote activator access list.");
                    break;

            }

        }
        void Pay(BasePlayer player)
        {
            List<Item> collect = new List<Item>();
            foreach (KeyValuePair<string, object> pair in cost)
            {
                string itemname = pair.Key.ToLower();
                if (displaynameToShortname.ContainsKey(itemname))
                    itemname = displaynameToShortname[itemname];
                ItemDefinition itemdef = ItemManager.FindItemDefinition(itemname);
                if (itemdef == null) continue;
                player.inventory.Take(collect, itemdef.itemid, Convert.ToInt32(pair.Value));
                player.Command(string.Format("note.inv {0} -{1}", itemdef.itemid, pair.Value));
            }
            foreach (Item item in collect)
            {
                item.Remove(0f);
            }
        }
        bool CanPay(BasePlayer player)
        {
            foreach (KeyValuePair<string, object> pair in cost)
            {
                string itemname = pair.Key.ToLower();
                if (displaynameToShortname.ContainsKey(itemname))
                    itemname = displaynameToShortname[itemname];
                ItemDefinition itemdef = ItemManager.FindItemDefinition(itemname);
                if (itemdef == null) continue;
                int amount = player.inventory.GetAmount(itemdef.itemid);
                if (amount < Convert.ToInt32(pair.Value))
                    return false;
            }
            return true;
        }
        [ChatCommand("remote")]
        void cmdChatNPCPathTest(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player))
            {
                SendReply(player, "You are not allowed to use this.");
                return;
            }
            if (args.Length == 0)
            {
                SendHelp(player);
                return;
            }
            switch (args[0].ToLower())
            {
                default:
                    List<BuildingPrivlidge> playerpriv = buildingPriviledge.GetValue(player) as List<BuildingPrivlidge>;
                    if (playerpriv.Count == 0)
                    {
                        SendReply(player, "You must have a Tool Cupboard to use this");
                        return;
                    }
                    foreach (BuildingPrivlidge priv in playerpriv.ToArray())
                    {
                        List<ProtoBuf.PlayerNameID> authorized = priv.authorizedPlayers;
                        bool foundplayer = false;
                        foreach (ProtoBuf.PlayerNameID pni in authorized.ToArray())
                        {
                            if (pni.userid == player.userID)
                                foundplayer = true;
                        }
                        if (!foundplayer)
                        {
                            SendReply(player, "You must have access to all surrounding tool cupboards");
                            return;
                        }
                    }
                    if (!CanPay(player))
                    {
                        SendReply(player, "You don't have enough resources to create a new remote activator");
                        return;
                    }
                    BaseEntity targetEnt = FindRayStructure(player.eyes.HeadRay(), constructionColl);
                    if (targetEnt == null)
                    {
                        SendReply(player, "You must be looking at a wall from maximum 3m away");
                        return;
                    }
                    BuildingBlock targetBlock = targetEnt.GetComponent<BuildingBlock>();
                    if (targetBlock == null)
                    {
                        SendReply(player, "You must be looking at a wall from maximum 3m away");
                        return;
                    }
                    if (targetBlock.blockDefinition.info.name.english.ToLower() != "wall")
                    {
                        SendReply(player, "You must be looking at a wall from maximum 3m away");
                        return;
                    }
                    Quaternion newRotation = targetEnt.transform.rotation * Quaternion.Euler(0, -90, 0);
                    Vector3 newPosition = targetEnt.transform.position + new Vector3(0f, 1.5f, 0f) + (newRotation * VectorForward);

                    RemoteActivator newRemote = new RemoteActivator(newPosition, args[0], player.userID.ToString());
                    if (remoteActivators[newRemote.Pos()] != null)
                    {
                        SendReply(player, "There is already a remote activator on this wall");
                        return;
                    }
                    SpawnDeployableSign("sign.wooden.small", newPosition, newRotation);
                    storedData.RemoteActivators.Add(newRemote);
                    remoteActivators[newRemote.Pos()] = newRemote;
                    Pay(player);
                    break;
            }
        }
        string GetCostInString()
        {
            var coststring = string.Empty;
            foreach (KeyValuePair<string, object> pair in cost)
            {
                coststring += string.Format("{0}x {1} - ", pair.Value, pair.Key);
            }
            return coststring;
        }
        void SendHelp(BasePlayer player, bool full = true)
        {
            var coststring = GetCostInString();
            var text = "<size=18>Remote Door Switch</size>\n<color=\"#ffd479\">/remote</color> - Get help on how to create a remote door switch that will remotely open and close your doors.";

            if (full)
            {
                text += "\n<color=\"#ffd479\">/remote</color> \"NAME\" - Create a new remote switch for doors where you are looking at. Needs to be on a wall!!";
                text += string.Format("\nIt will cost you: {0}", coststring == string.Empty ? "Nothing" : coststring);
                text += string.Format("\nYou may have {0} doors per remote, max {1}m away from the remote.", maxDoors, maxDistance);
            }
            SendReply(player, text);
        }
        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            SendHelp(player, false);
        }
    }
}
