using System.Linq;
using UnityEngine;
using System;
using Rust;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("NameGrabber", "Wolfs Darker", "1.0.1")]
    class NameGrabber : RustPlugin
    {

        [ConsoleCommand("grabname")]
        void cmdGrabName(ConsoleSystem.Arg arg)
        {

            if (arg.Player() && !arg.Player().IsAdmin())
            {
                SendReply(arg, "You don't have access to this command.");
                return;
            }


            if (arg.Args.Length == 0)
            {
                Puts("Wrong command usage! Try: grabname type name.");
                return;
            }

            string type = arg.Args[0];
            string name = arg.Args[1];

            var items_definition = ItemManager.GetItemDefinitions();
            var items_found = new List<string>();
            var temp_variable = "";

            switch (type)
            {
                case "item":
                    foreach (ItemDefinition def in items_definition)
                    {
                        if (def != null)
                        {
                            if (name.Equals("all") || def.shortname.Contains(name))
                            {
                                temp_variable += "'" + def.shortname + "' ";

                                if (temp_variable.Length >= 120)
                                {
                                    items_found.Add(temp_variable);
                                    temp_variable = "";
                                }
                            }
                        }
                    }
                    break;
                case"deployable":
                    foreach (ItemDefinition itemDef in items_definition)
                    {
                        if (itemDef.GetComponent<ItemModDeployable>() != null && (name.Equals("all") || itemDef.GetComponent<ItemModDeployable>().name.ToLower().Contains(name)))
                        {
                            temp_variable += "'" + itemDef.GetComponent<ItemModDeployable>().entityPrefab.resourcePath + "' ";

                            if (temp_variable.Length >= 120)
                            {
                                items_found.Add(temp_variable);
                                temp_variable = "";
                            }
                        }
                    }
                    break;
                case "animal":
                    var animals = UnityEngine.Object.FindObjectsOfType<BaseNPC>().Where(entity => name.Equals("all") || entity.name.Contains(name)).ToArray();
                    foreach (var o in animals)
                    {
                        if (!items_found.Contains(o.name))
                            items_found.Add(o.name);
                    }
                    break;
                case "ingame":
                    var objects = UnityEngine.Object.FindObjectsOfType<BaseEntity>().Where(entity => name.Equals("all") || entity.name.Contains(name)).ToArray();
                    foreach (var o in objects)
                    {
                        if (!items_found.Contains(o.name))
                            items_found.Add(o.name);
                    }
                    type = "In game object";
                    break;
                default:
                    Puts("No entity found!");
                    break;
            }

            if (temp_variable.Length > 0)
                items_found.Add(temp_variable);

            if (items_found.Count != 0)
                foreach (string s in items_found)
                {
                    Puts(type + "s Found: " + s);
                }
            else
                Puts("There is no " + type + " named " + name + ".");
        }
    }
}