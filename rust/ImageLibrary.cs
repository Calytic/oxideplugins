using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Configuration;
using Oxide.Core;
using System.IO;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("ImageLibrary", "Absolut", "1.0.0", ResourceId = 000000)]

    class ImageLibrary : RustPlugin
    {
        #region Fields

        ImageData imageData;
        private DynamicConfigFile ImageLibraryData;

        public class ImageData
        {
            public Dictionary<string, Dictionary<int, uint>> Images = new Dictionary<string, Dictionary<int, uint>>();
        }

        static GameObject webObject;
        static Images images;

        #endregion

        #region Hooks   

        void Loaded()
        {
            ImageLibraryData = Interface.Oxide.DataFileSystem.GetFile("ImageLibrary_Data");
        }

        void Unload()
        {
            SaveData();
        }

        void OnServerInitialized()
        {
            webObject = new GameObject("WebObject");
            images = webObject.AddComponent<Images>();
            images.SetDataDir(this);
            LoadData();
        }

        #endregion

        #region External Calls
        public string GetImage(string shortname, int skin = 0)
        {
            if (!imageData.Images.ContainsKey(shortname)) return imageData.Images["NONE"][0].ToString();
            if (!imageData.Images[shortname].ContainsKey(skin))
                return imageData.Images["NONE"][0].ToString();
            return imageData.Images[shortname][skin].ToString();
        }

        public bool HasImage(string shortname, int skin = 0)
        {
            if (!imageData.Images.ContainsKey(shortname)) return false;
            if (!imageData.Images[shortname].ContainsKey(skin))
                return false;
            return true;
        }

        public bool AddImage(string url, string name, int skin = 0)
        {
            if (!HasImage(name, skin))
            {
                images.Add(url,name, skin);
                return true;
            }
            return false;
        }
        #endregion

        #region Images
        class QueueImages
        {
            public string url;
            public string name;
            public int skin;
            public QueueImages(string ur, string nm, int sk)
            {
                url = ur;
                name = nm;
                skin = sk;
            }
        }

        class Images : MonoBehaviour
        {
            ImageLibrary filehandler;
            const int MaxActiveLoads = 3;
            static readonly List<QueueImages> QueueList = new List<QueueImages>();
            static byte activeLoads;
            private MemoryStream stream = new MemoryStream();

            public void SetDataDir(ImageLibrary fc) => filehandler = fc;
            public void Add(string url, string name, int skin)
            {
                QueueList.Add(new QueueImages(url, name, skin));
                if (activeLoads < MaxActiveLoads) Next();
            }

            void Next()
            {
                activeLoads++;
                var qi = QueueList[0];
                QueueList.RemoveAt(0);
                var www = new WWW(qi.url);
                StartCoroutine(WaitForRequest(www, qi));
            }

            private void ClearStream()
            {
                stream.Position = 0;
                stream.SetLength(0);
            }

            IEnumerator WaitForRequest(WWW www, QueueImages info)
            {
                yield return www;

                if (www.error == null)
                {
                    if (!filehandler.imageData.Images.ContainsKey(info.name))
                        filehandler.imageData.Images.Add(info.name, new Dictionary<int, uint>());
                    if (!filehandler.imageData.Images[info.name].ContainsKey(info.skin))
                    {
                        ClearStream();
                        stream.Write(www.bytes, 0, www.bytes.Length);
                        uint textureID = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue);
                        ClearStream();
                        filehandler.imageData.Images[info.name].Add(info.skin, textureID);
                    }
                }
                activeLoads--;
                if (QueueList.Count > 0) Next();
                else filehandler.SaveData();
            }
        }

        [ConsoleCommand("RefreshAllImages")]
        private void cmdRefreshAllImages(ConsoleSystem.Arg arg)
        {
                RefreshAllImages();
        }

        private void RefreshAllImages()
        {
            imageData.Images.Clear();
            images.Add("http://www.hngu.net/Images/College_Logo/28/b894b451_c203_4c08_922c_ebc95077c157.png", "NONE", 0);
            foreach (var entry in ItemImages)
                foreach (var item in entry.Value)
                    images.Add(item.Value, entry.Key, item.Key);
            timer.Once(10, () =>
            {
                SaveData();
            });
        }

        private void CheckNewImages()
        {
            foreach (var entry in ItemImages)
                foreach (var item in entry.Value)
                    if (!imageData.Images[entry.Key].ContainsKey(item.Key))
                        images.Add(item.Value, entry.Key, item.Key);
            timer.Once(10, () =>
            {
                SaveData();
            });
        }

        private Dictionary<string, Dictionary<int, string>> ItemImages = new Dictionary<string, Dictionary<int, string>>
        {
                { "tshirt", new Dictionary<int, string>
                {
                {0, "http://imgur.com/SAD8dWX.png" },
                {10130, "http://imgur.com/tqwRCKw.png"},
                {10033, "http://imgur.com/UjGqhac.png" },
                {10003, "http://imgur.com/Q2w1w74.png"},
                {14177, "http://imgur.com/wuj2TnQ.png" },
                {10056, "http://imgur.com/2lfKuYz.png"},
                {14181, "http://imgur.com/MgRHg0D.png" },
                {10024, "http://imgur.com/C0IH5q0.png"},
                {10035, "http://imgur.com/Vh9yCpv.png" },
                {10046, "http://imgur.com/r4EZ4X5.png"},
                {10038, "http://imgur.com/tSWGLIo.png" },
                {101, "http://imgur.com/iY3zqU3.png" },
                {10025, "http://imgur.com/6s4nmz6.png" },
                {10002, "http://imgur.com/2CwEo5f.png"},
                {10134, "http://imgur.com/bgAgtiN.png" },
                {10131, "http://imgur.com/QBDtZZt.png"},
                {10041, "http://imgur.com/ZWIFX0J.png" },
                {10053, "http://imgur.com/JzPIjvu.png"},
                {10039, "http://imgur.com/2e6RlNV.png" },
                {584379, "http://imgur.com/QGo7psZ.png"},
                {10043, "http://imgur.com/4oz5N6s.png" },
                }
            },
            {"pants", new Dictionary<int, string>
            {
                {0, "http://imgur.com/iiFJAso.png" },
                {10001, "http://imgur.com/ntwPM8B.png"},
                {10049, "http://imgur.com/UroE7FB.png" },
                {10019, "http://imgur.com/e4lMi7b.png"},
                {10078, "http://imgur.com/GtYg84o.png" },
                {10048, "http://imgur.com/NFpjEVG.png"},
                {10021, "http://imgur.com/zVQSCOM.png" },
                {10020, "http://imgur.com/jrILSlp.png" },
            }
            },
            {"shoes.boots", new Dictionary<int, string>
            {
                {0, "http://imgur.com/b8HJ3TJ.png" },
                {10080, "http://imgur.com/7LSy7LN.png"},
                {10023, "http://imgur.com/JWk9YKb.png" },
                {10088, "http://imgur.com/RRFrv7d.png"},
                {10034, "http://imgur.com/wkYqkDd.png" },
                {10044, "http://imgur.com/2b01wU2.png"},
                {10022, "http://imgur.com/CCqzvRr.png" },
            }
            },
             {"tshirt.long", new Dictionary<int, string>
            {
                {0, "http://imgur.com/KPxtIQI.png" },
                {10047, "http://imgur.com/S8H3tcI.png"},
                {10004, "http://imgur.com/e28NFqe.png" },
                {10089, "http://imgur.com/fg1o3bI.png"},
                {10106, "http://imgur.com/QCTgWL8.png" },
                {10050, "http://imgur.com/d7VWfRi.png"},
                {10032, "http://imgur.com/mWO2yFG.png" },
                {10005, "http://imgur.com/9x9r5nv.png"},
                {10125, "http://imgur.com/JF2G3Bo.png" },
                {10118, "http://imgur.com/MxfnH0L.png"},
                {10051, "http://imgur.com/PNQbN6q.png" },
                {10006, "http://imgur.com/mq1o74X.png"},
                {10036, "http://imgur.com/kHA82wu.png" },
                {10042, "http://imgur.com/gVSKubo.png" },
                {10007, "http://imgur.com/Nddd4yq.png"},
            }
            },
             {"mask.bandana", new Dictionary<int, string>
            {
                {0, "http://imgur.com/PImuCst.png" },
                {10061, "http://imgur.com/6z7XFqf.png"},
                {10060, "http://imgur.com/RvaahST.png" },
                {10067, "http://imgur.com/AXu92sd.png"},
                {10104, "http://imgur.com/eb2WQMJ.png" },
                {10066, "http://imgur.com/e2UwJ5L.png"},
                {10063, "http://imgur.com/u7cupKO.png" },
                {10059, "http://imgur.com/wJF8J0l.png"},
                {10065, "http://imgur.com/FVDQu9Y.png" },
                {10064, "http://imgur.com/k71r2Zq.png"},
                {10062, "http://imgur.com/TtSFbRI.png" },
                {10079, "http://imgur.com/hBW2DeR.png"},
            }
            },
             {"mask.balaclava", new Dictionary<int, string>
            {
                {0, "http://imgur.com/BYFgE5c.png" },
                {10105, "http://imgur.com/aZ24Prz.png"},
                {10069, "http://imgur.com/lZRsgUO.png"},
                {10071, "http://imgur.com/yqrwGPA.png" },
                {10068, "http://imgur.com/NSXHTJ8.png"},
                {10057, "http://imgur.com/qq0Kkf8.png" },
                {10075, "http://imgur.com/c52VsFb.png"},
                {10070, "http://imgur.com/qnu8n2a.png" },
                {10054, "http://imgur.com/QZCZVSP.png"},
                {10090, "http://imgur.com/1ngWJs4.png" },
                {10110, "http://imgur.com/4e4Jups.png"},
                {10084, "http://imgur.com/TXqTQBd.png" },
                {10139, "http://imgur.com/3hJfzEV.png"},
                {10111, "http://imgur.com/0Kl5Dcu.png" },
            }
            },
             {"jacket.snow", new Dictionary<int, string>
            {
                {0, "http://imgur.com/32ZO3jO.png" },
                {10082, "http://imgur.com/8jqmVOg.png"},
                {10113, "http://imgur.com/t2WQFcw.png" },
                {10083, "http://imgur.com/1lEjT1g.png"},
                {10112, "http://imgur.com/fdTvghu.png" },
            }
            },
             {"jacket", new Dictionary<int, string>
            {
                {0, "http://imgur.com/zU7TQPR.png" },
                {10011, "http://imgur.com/1qLvjuy.png"},
                {10012, "http://imgur.com/GA1QAnS.png" },
                {10009, "http://imgur.com/spufx0f.png"},
                {10015, "http://imgur.com/ua9esyK.png" },
                {10013, "http://imgur.com/7rkcCZ4.png"},
                {10072, "http://imgur.com/8jX3QSR.png" },
                {10010, "http://imgur.com/8snfg2N.png" },
                {10008, "http://imgur.com/Tk0KIFU.png"},
                {10014, "http://imgur.com/o0ZdjsQ.png" },
            }
            },
            {"hoodie", new Dictionary<int, string>
            {
                {0, "http://imgur.com/EvGigZB.png" },
                {10142, "http://imgur.com/WwwArof.png"},
                {14179, "http://imgur.com/wEyu9Ew.png" },
                {10052, "http://imgur.com/ghnihF2.png"},
                {14178, "http://imgur.com/EOh10jX.png" },
                {10133, "http://imgur.com/hmZGoIY.png"},
                {14072, "http://imgur.com/A0o5Tm5.png" },
                {10132, "http://imgur.com/i0tdeK7.png"},
                {10129, "http://imgur.com/UqqydUz.png" },
                {10086, "http://imgur.com/A7gjMm0.png"},
            }
            },
            {"hat.cap", new Dictionary<int, string>
            {
                {0, "http://imgur.com/TfycJC9.png" },
                {10029, "http://imgur.com/QFNHOZz.png"},
                {10027, "http://imgur.com/Zf14dTy.png" },
                {10055, "http://imgur.com/1zfiClI.png"},
                {10030, "http://imgur.com/acgOSe6.png" },
                {10026, "http://imgur.com/Augez3h.png"},
                {10028, "http://imgur.com/VZqY3iA.png" },
                {10045, "http://imgur.com/F34fPio.png" },
            }
            },
            {"hat.beenie", new Dictionary<int, string>
            {
                {0, "http://imgur.com/yDkGk47.png" },
                {14180, "http://imgur.com/ProarPm.png"},
                {10018, "http://imgur.com/gEPcMj7.png" },
                {10017, "http://imgur.com/QKmuZg9.png"},
                {10040, "http://imgur.com/2EEZQdG.png" },
                {10016, "http://imgur.com/PMU76bY.png"},
                {10085, "http://imgur.com/FDKeEhw.png" },
            }
            },
            {"burlap.gloves", new Dictionary<int, string>
            {
                {0, "http://imgur.com/8aFVMgl.png" },
                {10128, "http://imgur.com/HqZut8a.png"},
            }
            },
            {"burlap.shirt", new Dictionary<int, string>
            {
                {0, "http://imgur.com/MUs4xL6.png" },
                {10136, "http://imgur.com/E4wXccC.png"},
            }
            },
            {"hat.boonie", new Dictionary<int, string>
            {
                {0, "http://imgur.com/2b4OjxB.png" },
                {10058, "http://imgur.com/lkfKdyj.png"},
            }
            },
            {"santahat", new Dictionary<int, string>
            {
                {0, "http://imgur.com/bmOV0aX.png" },
            }
            },
            {"hazmat.pants", new Dictionary<int, string>
            {
                {0, "http://imgur.com/ZsaLNUK.png" },
            }
            },
            {"hazmat.jacket", new Dictionary<int, string>
            {
                {0, "http://imgur.com/uKk9ghN.png" },
            }
            },
            {"hazmat.helmet", new Dictionary<int, string>
            {
                {0, "http://imgur.com/BHSrFsh.png" },
            }
            },
            {"hazmat.gloves", new Dictionary<int, string>
            {
                {0, "http://imgur.com/JYTXvnx.png" },
            }
            },
            {"hazmat.boots", new Dictionary<int, string>
            {
                {0, "http://imgur.com/sfU4PdX.png" },
            }
            },
            {"hat.miner", new Dictionary<int, string>
            {
                {0, "http://imgur.com/RtRy2ne.png" },
            }
            },
            {"hat.candle", new Dictionary<int, string>
            {
                {0, "http://imgur.com/F7nP0PC.png" },
            }
            },
            {"hat.wolf", new Dictionary<int, string>
            {
                {0, "http://imgur.com/D2Z8QjL.png" },
            }
            },
            {"burlap.trousers", new Dictionary<int, string>
            {
                {0, "http://imgur.com/tDqEh7T.png" },
            }
            },
            {"burlap.shoes", new Dictionary<int, string>
            {
                {0, "http://imgur.com/wXrkSxd.png" },
            }
            },
            {"burlap.headwrap", new Dictionary<int, string>
            {
                {0, "http://imgur.com/u6YLWda.png" },
            }
            },
            {"bucket.helmet", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Sb5cnpz.png" },
                {10127, "http://imgur.com/ZD3jtRS.png"},
                {10126, "http://imgur.com/qULrqXO.png" },
            }
            },
            {"wood.armor.pants", new Dictionary<int, string>
            {
                {0, "http://imgur.com/k2O9xEX.png" },
            }
            },
            {"wood.armor.jacket", new Dictionary<int, string>
            {
                {0, "http://imgur.com/9PUyVIv.png" },
            }
            },
            {"roadsign.kilt", new Dictionary<int, string>
            {
                {0, "http://imgur.com/WLh1Nv4.png" },
            }
            },
            {"roadsign.jacket", new Dictionary<int, string>
            {
                {0, "http://imgur.com/tqpDp2V.png" },
            }
            },
            {"riot.helmet", new Dictionary<int, string>
            {
                {0, "http://imgur.com/NlxGOum.png" },
            }
            },
            {"metal.plate.torso", new Dictionary<int, string>
            {
                {0, "http://imgur.com/lMw6ez2.png" },
            }
            },
            {"metal.facemask", new Dictionary<int, string>
            {
                {0, "http://imgur.com/BPd5q6h.png" },
            }
            },

            {"coffeecan.helmet", new Dictionary<int, string>
            {
                {0, "http://imgur.com/RrY8aMM.png" },
            }
            },
            {"bone.armor.suit", new Dictionary<int, string>
            {
                {0, "http://imgur.com/FkFR1kX.png" },
            }
            },
            {"attire.hide.vest", new Dictionary<int, string>
            {
                {0, "http://imgur.com/RQ8LJ5q.png" },
            }
            },
            {"attire.hide.skirt", new Dictionary<int, string>
            {
                {0, "http://imgur.com/nRlYLJW.png" },
            }
            },
            {"attire.hide.poncho", new Dictionary<int, string>
            {
                {0, "http://imgur.com/cqHND3g.png" },
            }
            },
            {"attire.hide.pants", new Dictionary<int, string>
            {
                {0, "http://imgur.com/rJy27KQ.png" },
            }
            },
            {"attire.hide.helterneck", new Dictionary<int, string>
            {
                {0, "http://imgur.com/2RXe7cg.png" },
            }
            },
            {"attire.hide.boots", new Dictionary<int, string>
            {
                {0, "http://imgur.com/6S98FbC.png" },
            }
            },
            {"deer.skull.mask", new Dictionary<int, string>
            {
                {0, "http://imgur.com/sqLjUSE.png" },
            }
            },
            {"pistol.revolver", new Dictionary<int, string>
            {
                {0, "http://imgur.com/C6BHyBB.png" },
                {10114, "http://imgur.com/DAj7lQo.png"},
            }
            },
            {"pistol.semiauto", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Zwqg3ic.png" },
                {10087, "http://imgur.com/hQwcNSG.png"},
                {10108, "http://imgur.com/21uutmr.png" },
                {10081, "http://imgur.com/vllF4FS.png"},
                {10073, "http://imgur.com/MSBvxA7.png" },
            }
            },
            {"rifle.ak", new Dictionary<int, string>
            {
                {0, "http://imgur.com/qlgloXW.png" },
                {10135, "http://imgur.com/0xgio10.png"},
                {10137, "http://imgur.com/UPDtgyK.png" },
                {10138, "http://imgur.com/XXKKLC4.png"},
            }
            },
            {"rifle.bolt", new Dictionary<int, string>
            {
                {0, "http://imgur.com/8oVVXJS.png" },
                {10117, "http://imgur.com/lFOPXfE.png"},
                {10115, "http://imgur.com/qbTQ06y.png" },
                {10116, "http://imgur.com/VhRwq7N.png"},
            }
            },
            {"shotgun.pump", new Dictionary<int, string>
            {
                {0, "http://imgur.com/OHRph6g.png" },
                {10074, "http://imgur.com/h91b64t.png"},
                {10140, "http://imgur.com/ktINZdj.png" },
            }
            },
            {"shotgun.waterpipe", new Dictionary<int, string>
            {
                {0, "http://imgur.com/3BliJtR.png" },
                {10143, "http://imgur.com/rmftGXr.png"},
            }
            },
            {"rifle.lr300", new Dictionary<int, string>
            {
                {0, "http://imgur.com/NYffUwv.png"},
            }
            },
            {"crossbow", new Dictionary<int, string>
            {
                {0, "http://imgur.com/nDBFhTA.png" },
            }
            },
            {"smg.thompson", new Dictionary<int, string>
            {
                {0, "http://imgur.com/rSQ5nHj.png" },
                {10120, "http://imgur.com/H3nPvJh.png"},
            }
            },
            {"weapon.mod.small.scope", new Dictionary<int, string>
            {
                {0, "http://imgur.com/jMvDHLz.png" },
            }
            },
            {"weapon.mod.silencer", new Dictionary<int, string>
            {
                {0, "http://imgur.com/oighpzk.png" },
            }
            },
            {"weapon.mod.muzzlebrake", new Dictionary<int, string>
            {
                {0, "http://imgur.com/sjxJIjT.png" },
            }
            },
            {"weapon.mod.muzzleboost", new Dictionary<int, string>
            {
                {0, "http://imgur.com/U9aMaPN.png" },
            }
            },
            {"weapon.mod.lasersight", new Dictionary<int, string>
            {
                {0, "http://imgur.com/rxIzDwY.png" },
            }
            },
            {"weapon.mod.holosight", new Dictionary<int, string>
            {
                {0, "http://imgur.com/R76B83t.png" },
            }
            },
            {"weapon.mod.flashlight", new Dictionary<int, string>
            {
                {0, "http://imgur.com/4gFapPt.png" },
            }
            },
            {"spear.wooden", new Dictionary<int, string>
            {
                {0, "http://imgur.com/7QpIs8B.png" },
            }
            },
            {"spear.stone", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Y3HstyV.png" },
            }
            },
            {"smg.2", new Dictionary<int, string>
            {
                {0, "http://imgur.com/ElXI2uv.png" },
            }
            },
            {"smg.mp5", new Dictionary<int, string>
            {
                {0, "http://imgur.com/ohazNYk.png" },
            }
            },
            {"shotgun.double", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Pm2Q4Dj.png" },
            }
            },
            {"salvaged.sword", new Dictionary<int, string>
            {
                {0, "http://imgur.com/M6gWbNv.png" },
            }
            },
            {"salvaged.cleaver", new Dictionary<int, string>
            {
                {0, "http://imgur.com/DrelWEg.png" },
            }
            },
            {"rocket.launcher", new Dictionary<int, string>
            {
                {0, "http://imgur.com/2yDyb9p.png" },
            }
            },
            {"rifle.semiauto", new Dictionary<int, string>
            {
                {0, "http://imgur.com/UfGP5kq.png" },
            }
            },
            {"pistol.eoka", new Dictionary<int, string>
            {
                {0, "http://imgur.com/SSb9czm.png" },
            }
            },
            {"machete", new Dictionary<int, string>
            {
                {0, "http://imgur.com/KfwkwV8.png" },
            }
            },
            {"mace", new Dictionary<int, string>
            {
                {0, "http://imgur.com/OtsvCkC.png" },
            }
            },
            {"longsword", new Dictionary<int, string>
            {
                {0, "http://imgur.com/1StsKVe.png" },
            }
            },
            {"lmg.m249", new Dictionary<int, string>
            {
                {0, "http://imgur.com/f7Rzrn2.png" },
            }
            },
            {"knife.bone", new Dictionary<int, string>
            {
                {0, "http://imgur.com/9TaVbYX.png" },
            }
            },
            {"flamethrower", new Dictionary<int, string>
            {
                {0, "http://imgur.com/CwhZ8i7.png" },
            }
            },
            {"bow.hunting", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Myv79jT.png" },
            }
            },
            {"bone.club", new Dictionary<int, string>
            {
                {0, "http://imgur.com/ib11D8V.png" },
            }
            },
            {"grenade.f1", new Dictionary<int, string>
            {
                {0, "http://imgur.com/ZwrVuXh.png" },
            }
            },
            {"grenade.beancan", new Dictionary<int, string>
            {
                {0, "http://imgur.com/FQZOd7m.png" },
            }
            },
            {"ammo.handmade.shell", new Dictionary<int, string>
            {
                {0, "http://imgur.com/V0CyZ7j.png" },
            }
            },
            {"ammo.pistol", new Dictionary<int, string>
            {
                {0, "http://imgur.com/gDNR7oj.png" },
            }
            },
             {"ammo.pistol.fire", new Dictionary<int, string>
            {
                {0, "http://imgur.com/VyX0pAu.png" },
            }
            },
            {"ammo.pistol.hv", new Dictionary<int, string>
            {
                {0, "http://imgur.com/E1dB4Nb.png" },
            }
            },
            {"ammo.rifle", new Dictionary<int, string>
            {
                {0, "http://imgur.com/rqVkjX3.png" },
            }
            },
            {"ammo.rifle.explosive", new Dictionary<int, string>
            {
                {0, "http://imgur.com/hpAxKQc.png" },
            }
            },
            {"ammo.rifle.hv", new Dictionary<int, string>
            {
                {0, "http://imgur.com/BkG4hLM.png" },
            }
            },
            {"ammo.rifle.incendiary", new Dictionary<int, string>
            {
                {0, "http://imgur.com/SN4XV2S.png" },
            }
            },
            {"ammo.rocket.basic", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Weg1M6y.png" },
            }
            },
            {"ammo.rocket.fire", new Dictionary<int, string>
            {
                {0, "http://imgur.com/j4XMSmO.png" },
            }
            },
            {"ammo.rocket.hv", new Dictionary<int, string>
            {
                {0, "http://imgur.com/5mdVIIV.png" },
            }
            },
            {"ammo.rocket.smoke", new Dictionary<int, string>
            {
                {0, "http://imgur.com/kMTgSEI.png" },
            }
            },
            {"ammo.shotgun", new Dictionary<int, string>
            {
                {0, "http://imgur.com/caFY5Bp.png" },
            }
            },
            {"ammo.shotgun.slug", new Dictionary<int, string>
            {
                {0, "http://imgur.com/ti5fCBp.png" },
            }
            },
            {"arrow.hv", new Dictionary<int, string>
            {
                {0, "http://imgur.com/r6VLTt2.png" },
            }
            },
            {"arrow.wooden", new Dictionary<int, string>
            {
                {0, "http://imgur.com/yMCfjKh.png" },
            }
            },
            {"bandage", new Dictionary<int, string>
            {
                {0, "http://imgur.com/TuMpnnu.png" },
            }
            },
            {"syringe.medical", new Dictionary<int, string>
            {
                {0, "http://imgur.com/DPDicE6.png" },
            }
            },
            { "largemedkit", new Dictionary<int, string>
            {
                {0, "http://imgur.com/iPsWViD.png" },
            }
            },
            { "antiradpills", new Dictionary<int, string>
            {
                {0, "http://imgur.com/SIhXEtB.png" },
            }
            },
            { "blood", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Mdtvg2m.png" },
            }
            },
            {"bed", new Dictionary<int, string>
            {
                {0, "http://imgur.com/K0zQtwh.png" },
            }
            },
            {"box.wooden", new Dictionary<int, string>
            {
                {0, "http://imgur.com/dFqTUTQ.png" },
            }
            },
            {"box.wooden.large", new Dictionary<int, string>
            {
                {0, "http://imgur.com/qImBEtL.png" },
                {10124, "http://imgur.com/oXO4riD.png" },
                {10122, "http://imgur.com/Ue06zjq.png" },
                {10123, "http://imgur.com/QAizFb6.png" },
                {10141, "http://imgur.com/gSzIfNj.png" },
            }
            },
            {"campfire", new Dictionary<int, string>
            {
                {0, "http://i.imgur.com/TiAlJpv.png" },
            }
            },
            {"ceilinglight", new Dictionary<int, string>
            {
                {0, "http://imgur.com/3sikyL6.png" },
            }
            },
            {"door.double.hinged.metal", new Dictionary<int, string>
            {
                {0, "http://imgur.com/awNuhRv.png" },
            }
            },
            {"door.double.hinged.toptier", new Dictionary<int, string>
            {
                {0, "http://imgur.com/oJCqHd6.png" },
            }
            },
            {"door.double.hinged.wood", new Dictionary<int, string>
            {
                {0, "http://imgur.com/tcHmZXZ.png" },
            }
            },
            {"door.hinged.metal", new Dictionary<int, string>
            {
                {0, "http://imgur.com/UGZftiQ.png" },
            }
            },
            {"door.hinged.toptier", new Dictionary<int, string>
            {
                {0, "http://imgur.com/bc2TrfQ.png" },
            }
            },
            {"door.hinged.wood", new Dictionary<int, string>
            {
                {0, "http://imgur.com/PrrWSN2.png" },
            }
            },
            {"floor.grill", new Dictionary<int, string>
            {
                {0, "http://imgur.com/bp7ZOkE.png" },
            }
            },
            {"floor.ladder.hatch", new Dictionary<int, string>
            {
                {0, "http://imgur.com/suML6jj.png" },
            }
            },
            {"gates.external.high.stone", new Dictionary<int, string>
            {
                {0, "http://imgur.com/o4NWWXp.png" },
            }
            },
            {"gates.external.high.wood", new Dictionary<int, string>
            {
                {0, "http://imgur.com/DRa9a8G.png" },
            }
            },
            {"cupboard.tool", new Dictionary<int, string>
            {
                {0, "http://imgur.com/OzUewI1.png" },
            }
            },
            {"shelves", new Dictionary<int, string>
            {
                {0, "http://imgur.com/vjtdyk5.png" },
            }
            },
            {"shutter.metal.embrasure.a", new Dictionary<int, string>
            {
                {0, "http://imgur.com/1ke0LVO.png" },
            }
            },
            {"shutter.metal.embrasure.b", new Dictionary<int, string>
            {
                {0, "http://imgur.com/uRtgNRH.png" },
            }
            },
            {"shutter.wood.a", new Dictionary<int, string>
            {
                {0, "http://imgur.com/VngPUi2.png" },
            }
            },
            {"sign.hanging", new Dictionary<int, string>
            {
                {0, "http://imgur.com/VIeRGh9.png" },
            }
            },
            {"sign.hanging.banner.large", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Owr3668.png" },
            }
            },
            {"sign.hanging.ornate", new Dictionary<int, string>
            {
                {0, "http://imgur.com/nQ1xHYb.png" },
            }
            },
            {"sign.pictureframe.landscape", new Dictionary<int, string>
            {
                {0, "http://imgur.com/nNh1uro.png" },
            }
            },
            {"sign.pictureframe.portrait", new Dictionary<int, string>
            {
                {0, "http://imgur.com/CQr8UYq.png" },
            }
            },
            {"sign.pictureframe.tall", new Dictionary<int, string>
            {
                {0, "http://imgur.com/3b51GfA.png" },
            }
            },
            {"sign.pictureframe.xl", new Dictionary<int, string>
            {
                {0, "http://imgur.com/3zdBDqa.png" },
            }
            },
            {"sign.pictureframe.xxl", new Dictionary<int, string>
            {
                {0, "http://imgur.com/9xSgewe.png" },
            }
            },
            {"sign.pole.banner.large", new Dictionary<int, string>
            {
                {0, "http://imgur.com/nGRDZrO.png" },
            }
            },
            {"sign.post.double", new Dictionary<int, string>
            {
                {0, "http://imgur.com/CXUsPSn.png" },
            }
            },
            {"sign.post.single", new Dictionary<int, string>
            {
                {0, "http://imgur.com/0qXuSMs.png" },
            }
            },
            {"sign.post.town", new Dictionary<int, string>
            {
                {0, "http://imgur.com/KgN4T1C.png" },
            }
            },
            {"sign.post.town.roof", new Dictionary<int, string>
            {
                {0, "http://imgur.com/hCLJXg4.png" },
            }
            },
            {"sign.wooden.huge", new Dictionary<int, string>
            {
                {0, "http://imgur.com/DehcZTb.png" },
            }
            },
            {"sign.wooden.large", new Dictionary<int, string>
            {
                {0, "http://imgur.com/BItcvBB.png" },
            }
            },
            {"sign.wooden.medium", new Dictionary<int, string>
            {
                {0, "http://imgur.com/zXJcB26.png" },
            }
            },
            {"sign.wooden.small", new Dictionary<int, string>
            {
                {0, "http://imgur.com/wfDYYYW.png" },
            }
            },
            {"jackolantern.angry", new Dictionary<int, string>
            {
                {0, "http://imgur.com/NRdMCfb.png" },
            }
            },
            {"jackolantern.happy", new Dictionary<int, string>
            {
                {0, "http://imgur.com/2gIfuAO.png" },
            }
            },
            {"ladder.wooden.wall", new Dictionary<int, string>
            {
                {0, "http://imgur.com/E3haHSe.png" },
            }
            },
            {"lantern", new Dictionary<int, string>
            {
                {0, "http://imgur.com/UHQdu3Q.png" },
            }
            },
            {"lock.code", new Dictionary<int, string>
            {
                {0, "http://imgur.com/pAXI8ZY.png" },
            }
            },
            {"mining.quarry", new Dictionary<int, string>
            {
                {0, "http://imgur.com/4Mgh1nK.png" },
            }
            },
            {"mining.pumpjack", new Dictionary<int, string>
            {
                {0, "http://imgur.com/FWbMASw.png" },
            }
            },
            {"wall.external.high", new Dictionary<int, string>
            {
                {0, "http://imgur.com/mB8oila.png" },
            }
            },
            {"wall.external.high.stone", new Dictionary<int, string>
            {
                {0, "http://imgur.com/7t3BdwH.png" },
            }
            },
            {"wall.frame.cell", new Dictionary<int, string>
            {
                {0, "http://imgur.com/oLj65GS.png" },
            }
            },
            {"wall.frame.cell.gate", new Dictionary<int, string>
            {
                {0, "http://imgur.com/iAcwJmG.png" },
            }
            },
            {"wall.frame.fence", new Dictionary<int, string>
            {
                {0, "http://imgur.com/4HVSY9Y.png" },
            }
            },
            {"wall.frame.fence.gate", new Dictionary<int, string>
            {
                {0, "http://imgur.com/mpmO78C.png" },
            }
            },
            {"wall.frame.shopfront", new Dictionary<int, string>
            {
                {0, "http://imgur.com/G7fB7kk.png" },
            }
            },
            {"wall.window.bars.metal", new Dictionary<int, string>
            {
                {0, "http://imgur.com/QmkIpkZ.png" },
            }
            },
            {"wall.window.bars.toptier", new Dictionary<int, string>
            {
                {0, "http://imgur.com/AsMdaCc.png" },
            }
            },
            {"wall.window.bars.wood", new Dictionary<int, string>
            {
                {0, "http://imgur.com/VS3SVVB.png" },
            }
            },
            {"lock.key", new Dictionary<int, string>
            {
                {0, "http://imgur.com/HuelWn0.png" },
            }
            },
            { "barricade.concrete", new Dictionary<int, string>
            {
                {0, "http://imgur.com/91Ob9XP.png" },
            }
            },
            {"barricade.metal", new Dictionary<int, string>
            {
                {0, "http://imgur.com/7rseBMC.png" },
            }
            },
            { "barricade.sandbags", new Dictionary<int, string>
            {
                {0, "http://imgur.com/gBQLSgQ.png" },
            }
            },
            { "barricade.wood", new Dictionary<int, string>
            {
                {0, "http://imgur.com/ycYTO3W.png" },
            }
            },
            { "barricade.woodwire", new Dictionary<int, string>
            {
                {0, "http://imgur.com/PMEFBla.png" },
            }
            },
            { "barricade.stone", new Dictionary<int, string>
            {
                {0, "http://imgur.com/W8qTCEX.png" },
            }
            },
            {"bone.fragments", new Dictionary<int, string>
            {
                {0, "http://imgur.com/iOJbBGT.png" },
            }
            },
            {"charcoal", new Dictionary<int, string>
            {
                {0, "http://imgur.com/G2hyxqi.png" },
            }
            },
            {"cloth", new Dictionary<int, string>
            {
                {0, "http://imgur.com/0olknLW.png" },
            }
            },
            {"coal", new Dictionary<int, string>
            {
                {0, "http://imgur.com/SIWOdbj.png" },
            }
            },
            {"crude.oil", new Dictionary<int, string>
            {
                {0, "http://imgur.com/VmQvwPS.png" },
            }
            },
            {"fat.animal", new Dictionary<int, string>
            {
                {0, "http://imgur.com/7NdUBKm.png" },
            }
            },
            {"hq.metal.ore", new Dictionary<int, string>
            {
                {0, "http://imgur.com/kdBrQ2P.png" },
            }
            },
            {"lowgradefuel", new Dictionary<int, string>
            {
                {0, "http://imgur.com/CSNPLYX.png" },
            }
            },
            {"metal.fragments", new Dictionary<int, string>
            {
                {0, "http://imgur.com/1bzDvUs.png" },
            }
            },
            {"metal.ore", new Dictionary<int, string>
            {
                {0, "http://imgur.com/yrTGHvv.png" },
            }
            },
            {"leather", new Dictionary<int, string>
            {
                {0, "http://imgur.com/9rqWrIy.png" },
            }
            },
            {"metal.refined", new Dictionary<int, string>
            {
                {0, "http://imgur.com/j2947YU.png" },
            }
            },
            {"wood", new Dictionary<int, string>
            {
                {0, "http://imgur.com/AChzDls.png" },
            }
            },
            {"seed.corn", new Dictionary<int, string>
            {
                {0, "http://imgur.com/u9ZPaeG.png" },
            }
            },
            {"seed.hemp", new Dictionary<int, string>
            {
                {0, "http://imgur.com/wO6aojb.png" },
            }
            },
            {"seed.pumpkin", new Dictionary<int, string>
            {
                {0, "http://imgur.com/mHaV8ei.png" },
            }
            },
            {"skull.human", new Dictionary<int, string>
            {
                {0, "http://imgur.com/ZFnWubS.png" },
            }
            },
            {"skull.wolf", new Dictionary<int, string>
            {
                {0, "http://imgur.com/f4MRE72.png" },
            }
            },
            {"stones", new Dictionary<int, string>
            {
                {0, "http://imgur.com/cluFzuZ.png" },
            }
            },
            {"sulfur", new Dictionary<int, string>
            {
                {0, "http://imgur.com/1RTTB7k.png" },
            }
            },
            {"sulfur.ore", new Dictionary<int, string>
            {
                {0, "http://imgur.com/AdxkKGb.png" },
            }
            },
            {"gunpowder", new Dictionary<int, string>
            {
                {0, "http://imgur.com/qV7b4WD.png" },
            }
            },
            {"researchpaper", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Pv8jxrl.png" },
            }
            },
            {"explosives", new Dictionary<int, string>
            {
                {0, "http://imgur.com/S43G64k.png" },
            }
            },
            {"botabag", new Dictionary<int, string>
            {
                {0, "http://imgur.com/MkIOiUs.png" },
            }
            },
            {"box.repair.bench", new Dictionary<int, string>
            {
                {0, "http://imgur.com/HpwYNjI.png" },
            }
            },
            {"bucket.water", new Dictionary<int, string>
            {
                {0, "http://imgur.com/svlCdlv.png" },
            }
            },
            {"explosive.satchel", new Dictionary<int, string>
            {
                {0, "http://imgur.com/dlUW54q.png" },
            }
            },
            {"explosive.timed", new Dictionary<int, string>
            {
                {0, "http://imgur.com/CtxUCgC.png" },
            }
            },
            {"flare", new Dictionary<int, string>
            {
                {0, "http://imgur.com/MS0JcRT.png" },
            }
            },
            {"fun.guitar", new Dictionary<int, string>
            {
                {0, "http://imgur.com/l96owHe.png" },
            }
            },
            {"furnace", new Dictionary<int, string>
            {
                {0, "http://imgur.com/77i4nqb.png" },
            }
            },
            {"furnace.large", new Dictionary<int, string>
            {
                {0, "http://imgur.com/NmsmUzo.png" },
            }
            },
            {"hatchet", new Dictionary<int, string>
            {
                {0, "http://imgur.com/5juFLRZ.png" },
            }
            },
            {"icepick.salvaged", new Dictionary<int, string>
            {
                {0, "http://imgur.com/ZTJLWdI.png" },
            }
            },
            {"axe.salvaged", new Dictionary<int, string>
            {
                {0, "http://imgur.com/muTaCg2.png" },
            }
            },
            {"pickaxe", new Dictionary<int, string>
            {
                {0, "http://imgur.com/QNirWhG.png" },
            }
            },
            {"research.table", new Dictionary<int, string>
            {
                {0, "http://imgur.com/C9wL7Kk.png" },
            }
            },
            {"small.oil.refinery", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Qqz6RgS.png" },
            }
            },
            {"stone.pickaxe", new Dictionary<int, string>
            {
                {0, "http://imgur.com/54azzFs.png" },
            }
            },
            {"stonehatchet", new Dictionary<int, string>
            {
                {0, "http://imgur.com/toLaFZd.png" },
            }
            },
            {"supply.signal", new Dictionary<int, string>
            {
                {0, "http://imgur.com/wj6yzow.png" },
            }
            },
            {"surveycharge", new Dictionary<int, string>
            {
                {0, "http://imgur.com/UPNvuY0.png" },
            }
            },
            {"target.reactive", new Dictionary<int, string>
            {
                {0, "http://imgur.com/BNcKZnU.png" },
            }
            },
            {"tool.camera", new Dictionary<int, string>
            {
                {0, "http://imgur.com/4AaLCfW.png" },
            }
            },
            {"water.barrel", new Dictionary<int, string>
            {
                {0, "http://imgur.com/JsmzCeU.png" },
            }
            },
            {"water.catcher.large", new Dictionary<int, string>
            {
                {0, "http://imgur.com/YWrJQoa.png" },
            }
            },
            {"water.catcher.small", new Dictionary<int, string>
            {
                {0, "http://imgur.com/PTXcYXs.png" },
            }
            },
            {"water.purifier", new Dictionary<int, string>
            {
                {0, "http://imgur.com/L7R4Ral.png" },
            }
            },
            {"rock", new Dictionary<int, string>
            {
                {0, "http://imgur.com/2GMBs5M.png" },
            }
            },
            {"torch", new Dictionary<int, string>
            {
                {0, "http://imgur.com/qKYxg5E.png" },
            }
            },
            {"stash.small", new Dictionary<int, string>
            {
                {0, "http://imgur.com/fH4RWZe.png" },
            }
            },
            {"sleepingbag", new Dictionary<int, string>
            {
                {0, "http://imgur.com/oJes3Lo.png" },
                {10121, "http://imgur.com/GvwtwGH.png" },
                {10037, "http://imgur.com/gDYUE6H.png" },
                {10119, "http://imgur.com/3lxtYiD.png" },
                {10109, "http://imgur.com/wQeDRzA.png" },
                {10107, "http://imgur.com/AHUGw7a.png" },
                {10077, "http://imgur.com/j7YFRrI.png" },
                {10076, "http://imgur.com/UCtDwNT.png" },
            }
            },
            {"hammer.salvaged", new Dictionary<int, string>
            {
                {0, "http://imgur.com/5oh3Wke.png" },
            }
            },
            {"hammer", new Dictionary<int, string>
            {
                {0, "http://imgur.com/KNG2Gvs.png" },
            }
            },
            {"blueprintbase", new Dictionary<int, string>
            {
                {0, "http://imgur.com/gMdRr6G.png" },
            }
            },
            {"fishtrap.small", new Dictionary<int, string>
            {
                {0, "http://imgur.com/spuGlOj.png" },
            }
            },
            {"building.planner", new Dictionary<int, string>
            {
                {0, "http://imgur.com/oXu5F27.png" },
            }
            },
            {"battery.small", new Dictionary<int, string>
            {
                {0, "http://imgur.com/214z05n.png" },
            }
            },
            {"can.tuna.empty", new Dictionary<int, string>
            {
                {0, "http://imgur.com/GB02zHx.png" },
            }
            },
            {"can.beans.empty", new Dictionary<int, string>
            {
                {0, "http://imgur.com/9K5In35.png" },
            }
            },
            { "cctv.camera", new Dictionary<int, string>
            {
                {0, "http://imgur.com/4j4LD01.png" },
            }
            },
            {"pookie.bear", new Dictionary<int, string>
            {
                {0, "http://imgur.com/KJSccj0.png" },
            }
            },
            {"targeting.computer", new Dictionary<int, string>
            {
                {0, "http://imgur.com/oPMPl3B.png" },
            }
            },
            {"trap.bear", new Dictionary<int, string>
            {
                {0, "http://imgur.com/GZD4bVy.png" },
            }
            },
            {"trap.landmine", new Dictionary<int, string>
            {
                {0, "http://imgur.com/YR0lVCs.png" },
            }
            },
            {"autoturret", new Dictionary<int, string>
            {
                {0, "http://imgur.com/4R0ByHj.png" },
            }
            },
            {"spikes.floor", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Nj0yJs0.png" },
            }
            },
            {"note", new Dictionary<int, string>
            {
                {0, "http://imgur.com/AM3Uech.png" },
            }
            },
            {"paper", new Dictionary<int, string>
            {
                {0, "http://imgur.com/pK49c6M.png" },
            }
            },
            {"map", new Dictionary<int, string>
            {
                {0, "http://imgur.com/u8HBelr.png" },
            }
            },
            {"stocking.large", new Dictionary<int, string>
            {
                {0, "http://imgur.com/di39MBT.png" },
            }
            },
            {"stocking.small", new Dictionary<int, string>
            {
                {0, "http://imgur.com/6eAg1zi.png" },
            }
            },
            {"generator.wind.scrap", new Dictionary<int, string>
            {
                {0, "http://imgur.com/fuQaE1H.png" },
            }
            },
            {"xmas.present.large", new Dictionary<int, string>
            {
                {0, "http://imgur.com/dU3nhYo.png" },
            }
            },
            {"xmas.present.medium", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Ov5YUty.png" },
            }
            },
            {"xmas.present.small", new Dictionary<int, string>
            {
                {0, "http://imgur.com/hWCd67B.png" },
            }
            },
            {"door.key", new Dictionary<int, string>
            {
                {0, "http://imgur.com/kw8UAN2.png" },
            }
            },
            { "wolfmeat.burned", new Dictionary<int, string>
            {
                {0, "http://imgur.com/zAJhDNd.png" },
            }
            },
            { "wolfmeat.cooked", new Dictionary<int, string>
            {
                {0, "http://imgur.com/LKlgpMe.png" },
            }
            },
            { "wolfmeat.raw", new Dictionary<int, string>
            {
                {0, "http://imgur.com/qvMvis8.png" },
            }
            },
            { "wolfmeat.spoiled", new Dictionary<int, string>
            {
                {0, "http://imgur.com/8kXOVyJ.png" },
            }
            },
            {"waterjug", new Dictionary<int, string>
            {
                {0, "http://imgur.com/BJzeMkc.png" },
            }
            },
            {"water.salt", new Dictionary<int, string>
            {
                {0, "http://imgur.com/d4ihUtv.png" },
            }
            },
            {"water", new Dictionary<int, string>
            {
                {0, "http://imgur.com/xdz5L7M.png" },
            }
            },
            {"smallwaterbottle", new Dictionary<int, string>
            {
                {0, "http://imgur.com/YTLCucH.png" },
            }
            },
            {"pumpkin", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Gb9NvdQ.png" },
            }
            },
            {"mushroom", new Dictionary<int, string>
            {
                {0, "http://imgur.com/FeWuvuh.png" },
            }
            },
            {"meat.boar", new Dictionary<int, string>
            {
                {0, "http://imgur.com/4ijrHrn.png" },
            }
            },
            {"meat.pork.burned", new Dictionary<int, string>
            {
                {0, "http://imgur.com/5Dam9qQ.png" },
            }
            },
            {"meat.pork.cooked", new Dictionary<int, string>
            {
                {0, "http://imgur.com/yhgxCPG.png" },
            }
            },
            {"humanmeat.burned", new Dictionary<int, string>
            {
                {0, "http://imgur.com/DloSZvl.png" },
            }
            },
            {"humanmeat.cooked", new Dictionary<int, string>
            {
                {0, "http://imgur.com/ba2j2rG.png" },
            }
            },
            {"humanmeat.raw", new Dictionary<int, string>
            {
                {0, "http://imgur.com/28SpF8Y.png" },
            }
            },
            {"humanmeat.spoiled", new Dictionary<int, string>
            {
                {0, "http://imgur.com/mSWVRUi.png" },
            }
            },
            {"granolabar", new Dictionary<int, string>
            {
                {0, "http://imgur.com/3rvzSwj.png" },
            }
            },
            {"fish.cooked", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Idtzv1t.png" },
            }
            },
            {"fish.minnows", new Dictionary<int, string>
            {
                {0, "http://imgur.com/7LXZH2S.png" },
            }
            },
            {"fish.troutsmall", new Dictionary<int, string>
            {
                {0, "http://imgur.com/aJ2PquF.png" },
            }
            },
            {"fish.raw", new Dictionary<int, string>
            {
                {0, "http://imgur.com/GdErxqf.png" },
            }
            },
            {"corn", new Dictionary<int, string>
            {
                {0, "http://imgur.com/6V5SJZ0.png" },
            }
            },
            {"chocholate", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Ymq7PsV.png" },
            }
            },
            {"chicken.burned", new Dictionary<int, string>
            {
                {0, "http://imgur.com/34sYfir.png" },
            }
            },
            {"chicken.cooked", new Dictionary<int, string>
            {
                {0, "http://imgur.com/UvHbBhH.png" },
            }
            },
            {"chicken.raw", new Dictionary<int, string>
            {
                {0, "http://imgur.com/gMldKSz.png" },
            }
            },
            {"chicken.spoiled", new Dictionary<int, string>
            {
                {0, "http://imgur.com/hiOEwGn.png" },
            }
            },
            {"cactusflesh", new Dictionary<int, string>
            {
                {0, "http://imgur.com/8R16YDP.png" },
            }
            },
            {"candycane", new Dictionary<int, string>
            {
                {0, "http://imgur.com/DSxrXOI.png" },
            }
            },
            {"can.tuna", new Dictionary<int, string>
            {
                {0, "http://imgur.com/c8rDUP3.png" },
            }
            },
            {"can.beans", new Dictionary<int, string>
            {
                {0, "http://imgur.com/Ysn6ThW.png" },
            }
            },
            {"blueberries", new Dictionary<int, string>
            {
                {0, "http://imgur.com/tFZ66fB.png" },
            }
            },
            {"black.raspberries", new Dictionary<int, string>
            {
                {0, "http://imgur.com/HZjKpX9.png" },
            }
            },
            {"bearmeat", new Dictionary<int, string>
            {
                {0, "http://imgur.com/hpL2I64.png" },
            }
            },
            {"bearmeat.burned", new Dictionary<int, string>
            {
                {0, "http://imgur.com/f1eVA0W.png" },
            }
            },
            {"bearmeat.cooked", new Dictionary<int, string>
            {
                {0, "http://imgur.com/e5Z6w1y.png" },
            }
            },
            {"apple", new Dictionary<int, string>
            {
                {0, "http://imgur.com/goMCM2w.png" },
            }
            },
            {"apple.spoiled", new Dictionary<int, string>
            {
                {0, "http://imgur.com/2pi2sUH.png" },
            }
            },
        };

        #endregion

        #region Data Management

        void SaveData()
        {
            ImageLibraryData.WriteObject(imageData);
        }

        void LoadData()
        {
            try
            {
                imageData = ImageLibraryData.ReadObject<ImageData>();
                CheckNewImages();
            }
            catch
            {

                Puts("Couldn't load Image Data, creating new datafile and refreshing Images");
                imageData = new ImageData();
                RefreshAllImages();
            }
        }
        #endregion
    }
}