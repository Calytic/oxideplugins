using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Facepunch;

namespace Oxide.Plugins
{
	[Info("PrefabSniffer", "Ayrin", "1.1.1", ResourceId = 1938)]
	class PrefabSniffer : RustPlugin
	{
		private static List<string> resourcesList;

        private string argmsg = "Usage: prefabs build/fx";

		[ConsoleCommand("prefabs")]
		void cmdSniffPrefabs(ConsoleSystem.Arg arg)
		{
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, argmsg);
                return;
            }


            resourcesList = new List<string>();
            var argname = "default";
            var filesraw = GameManifest.Get().pooledStrings;
            var filesField = typeof(FileSystem_AssetBundles).GetField("files", BindingFlags.Instance | BindingFlags.NonPublic);
            var files = (Dictionary<string, AssetBundle>)filesField.GetValue(FileSystem.iface);
            
            switch (arg.Args[0].ToLower())
            {
                case "build":
                    foreach (var str in files.Keys)
                    {
                        if ((str.StartsWith("assets/content/")
                        	|| str.StartsWith("assets/bundled/")
                        	|| str.StartsWith("assets/prefabs/")) && str.EndsWith(".prefab"))
                        {
                            if (str.Contains(".worldmodel.")
                            	|| str.Contains("/fx/")
                            	|| str.Contains("/effects/")
                            	|| str.Contains("/build/skins/")
                            	|| str.Contains("/_unimplemented/")
                            	|| str.Contains("/ui/")
                            	|| str.Contains("/sound/")
                            	|| str.Contains("/world/")
                            	|| str.Contains("/env/")
                            	|| str.Contains("/clothing/")
                            	|| str.Contains("/skins/")
                            	|| str.Contains("/decor/")
                            	|| str.Contains("/monument/")
                            	|| str.Contains("/crystals/")
                            	|| str.Contains("/projectiles/")
                            	|| str.Contains("/meat_")
                            	|| str.EndsWith(".skin.prefab")
                            	|| str.EndsWith(".viewmodel.prefab")
                            	|| str.EndsWith("_test.prefab")
                            	|| str.EndsWith("_collision.prefab")
                            	|| str.EndsWith("_ragdoll.prefab")
                            	|| str.EndsWith("_skin.prefab")
                            	|| str.Contains("/clutter/")) continue;
                            
                            var gmobj = GameManager.server.FindPrefab(str);
                            if (gmobj?.GetComponent<BaseEntity>() != null)
                                resourcesList.Add(str);
                        }
                    }
                    argname = "Build";
                    Puts("Check your ~/oxide/logs folder");
                    break;
                case "fx":
                    foreach (var str in filesraw)
                    {
                        if ((str.str.StartsWith("assets/content/")
                        	|| str.str.StartsWith("assets/bundled/")
                        	|| str.str.StartsWith("assets/prefabs/")) && str.str.EndsWith(".prefab"))
                        {
                            if (!str.str.Contains("/fx/")) continue;

                            resourcesList.Add(str.str.ToString());
                        }
                    }
                    argname = "FX";
                    Puts("Check your ~/oxide/logs folder");
                    break;
                case "all":
                	foreach (var str in filesraw)
                	{
                		resourcesList.Add(str.str.ToString());
                	}
                	argname = "ALL";
                	Puts("Check your ~/oxide/logs folder");
                	break;
                default:
                    SendReply(arg, argmsg);
                    break;
            }
            
            var now = DateTime.Now.ToString("dd-MM-yyyy");
            for (int i = 0; i < resourcesList.Count - 1; i++)
            {
                ConVar.Server.Log("oxide/logs/Prefabs" + argname + "_" + now + ".txt", string.Format("{0} - {1}", i, resourcesList[i]));
            }
		}
	}
}