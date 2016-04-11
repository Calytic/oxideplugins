using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;
namespace Oxide.Plugins
{
	[Info("AdvTest", "BDM", "3.1.0")]
    [Description("Public release of AdvTest.")]
    public class AdvTest : RustLegacyPlugin
	{
        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"Prefix", "AdvTest"},
                {"NoPerm", "You dont have permission to use /test"},
                {"TestWarning", "[color red]During test only press buttons as directed."},
                {"TestSyntax", "[color red]Wrong Syntax:[color white] Use [color orange]/test 'username' 'test name'"},
                {"NoPlayer1", "No player name [color yellow]"},
                {"NoPlayer2", "[color white] was found."},
                {"TestTargetDirections", "[color orange]Wait for instructions from an Admin!"},
                {"TargetColor", "[color yellow]"},
                {"TestRecoil1", "[COLOR WHITE] is now being tested for [COLOR GREEN]no recoil."},
                {"TestRecoil2", "[COLOR WHITE] now has 1 p250 and 8 9mm rounds"},
                {"TestRecoil3", "If the player fires and his gun moves up he is [COLOR GREEN]clear [COLOR WHITE]of hacks."},
                {"TestMenu1", "[COLOR WHITE] is now being tested for [color green]menus."},
                {"TestMenu2", "If any of these DONT move the user, he is running a menu:" },
                {"TestMenu3", "Tell the user to press F2 which will make him go forward."},
                {"TestMenu4", "Tell the user to press F5 which will make him go to the left"},
                {"TestMenu5", "Tell the user to press Insert which will make him go backwards."},
                {"TestBind1", "[COLOR WHITE] is now being tested for [color green]key binds."},
                {"TestBind2", "Tell the user to press numkey1 which will make him jump and go forward." },
                {"TestBind3", "If the user runs against the wall and disconnects within 20 seconds they fail."},
                {"TestScreenShot1", "[COLOR WHITE] is now being tested for [color green]screenshot."},
                {"TestScreenShot2", "Once user has uploaded, check it on their profile."},
                {"TestScreenShot3", "Correct dates should be May 1st ending in --:'11', or --:'25', or May 2nd ending in --:'11'."},
                {"TestScreenShotTarget1", "Please hit F12, shift+tab, then click view screenshots and"},
                {"TestScreenShotTarget2", "upload (bottom left) then tell [COLOR CYAN]"},
                {"TestClear", "You have [COLOR GREEN]concluded [COLOR WHITE]the testing of[COLOR YELLOW] "},
                {"TestClearTarget", "You have [COLOR GREEN] passed [COLOR WHITE] the test, buttons rebound, enjoy the server!"},
                {"TestHelpIntro", "AdvTest - v3.1.0 - BDM"},
                {"TestHelp1", "[color orange]The following commands are currently included (use provided example syntax)"},
                {"TestHelp2", "[color cyan]Recoil [color white] - [color green]/test 'BDM' recoil"},
                {"TestHelp3", "[color cyan]Menu [color white] - [color green]/test 'BDM' menu"},
                {"TestHelp4", "[color cyan]Bind [color white] - [color green]/test 'BDM' bind"},
                {"TestHelp5", "[color cyan]Screenshot [color white] - [color green]/test 'BDM' screenshot"},
                {"TestHelp6", "[color cyan]Fail [color white] - [color green]/test 'BDM' fail"},
                {"TestHelp7", "[color cyan]Clear [color white] - [color green]/test 'BDM' clear"},
                {"TestFailed", " [color red]has been banned for [color orange]Test Failed."}
            };
            lang.RegisterMessages(messages, this);
        }
        void Loaded()
        {
            permission.RegisterPermission("advtest.allowed", this);
            LoadDefaultMessages();
        }
        [ChatCommand("test")]
        void cmdTest(NetUser netuser, string command, string[] args)
        {
            NetUser targetuser = rust.FindPlayer(args[0]);
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advtest.allowed"))
            {
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("NoPerm", netuser.userID.ToString()));
            }
            else if (args.Length > 2 || args.Length < 1)
            {
                rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestSyntax", netuser.userID.ToString()));
            }
            else if (args.Length == 2)
            {
                    if (args[1].ToLower() == "recoil")
                {
                    if (targetuser != null)
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TargetColor") + netuser.displayName + GetMessage("TestRecoil1", netuser.userID.ToString()));
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TargetColor") + netuser.displayName + GetMessage("TestRecoil2", netuser.userID.ToString()));
                        timer.Once(0.5f, () =>
                        {
                            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestRecoil3", netuser.userID.ToString()));
                        });
                        RecoilTest(targetuser);
                    }
                    else
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("NoPlayer1", netuser.userID.ToString()) + args[0].ToString() + GetMessage("NoPlayer2", netuser.userID.ToString()));
                    }
                }
                else if (args[1].ToLower() == "menu")
                {
                    if (targetuser != null)
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TargetColor", netuser.userID.ToString()) + targetuser.displayName + GetMessage("TestMenu1", netuser.userID.ToString()));
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestMenu2", netuser.userID.ToString()));
                        timer.Once(1f, () =>
                        {
                            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestMenu3", netuser.userID.ToString()));
                        });
                        timer.Once(3f, () =>
                        {
                            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestMenu4", netuser.userID.ToString()));
                        });
                        timer.Once(6f, () =>
                        {
                            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestMenu5", netuser.userID.ToString()));
                        });
                        MenuTest(targetuser);
                    }
                    else
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("NoPlayer1", netuser.userID.ToString()) + args[0].ToString() + GetMessage("NoPlayer2", netuser.userID.ToString()));
                    }
                }
                else if (args[1].ToLower() == "bind")
                {
                    if (targetuser != null)
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TargetColor", netuser.userID.ToString()) + targetuser.displayName + GetMessage("TestBind1", netuser.userID.ToString()));
                        timer.Once(1f, () =>
                        {
                            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestBind2", netuser.userID.ToString()));
                        });
                        timer.Once(3f, () =>
                        {
                            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestBind3", netuser.userID.ToString()));
                        });
                        BindTest(targetuser);
                    }
                    else
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("NoPlayer1", netuser.userID.ToString()) + args[0].ToString() + GetMessage("NoPlayer2", netuser.userID.ToString()));
                    }
                }
                else if (args[1].ToLower() == "screenshot")
                {
                    if (targetuser != null)
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TargetColor", netuser.userID.ToString()) + targetuser.displayName + GetMessage("TestScreenShot1", netuser.userID.ToString()));
                        timer.Once(1f, () =>
                        {
                            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestScreenShot2", netuser.userID.ToString()));
                        });
                        timer.Once(3f, () =>
                        {
                            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestScreenShot3", netuser.userID.ToString()));
                        });
                        ScreenShotTest(targetuser);
                    }
                    else
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("NoPlayer1", netuser.userID.ToString()) + args[0].ToString() + GetMessage("NoPlayer2", netuser.userID.ToString()));
                    }
                }
                else if (args[1].ToLower() == "clear")
                {
                    if (targetuser != null)
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestClear", netuser.userID.ToString()) + targetuser.displayName);
                        ClearTest(targetuser);
                    }
                    else
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("NoPlayer1", netuser.userID.ToString()) + args[0].ToString() + GetMessage("NoPlayer2", netuser.userID.ToString()));
                    }
                }
                else if (args[1].ToLower() == "fail")
                {
                    if (targetuser != null)
                    {
                        FailTest(targetuser);
                    }
                    else
                    {
                        rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("NoPlayer1", netuser.userID.ToString()) + args[0].ToString() + GetMessage("NoPlayer2", netuser.userID.ToString()));
                    }
                }
            }
            else if (args.Length == 1)
            {
                if (args[0].ToLower() == "help")
                {
                    HelpMenu(netuser);
                }
                else
                {
                    rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestSyntax", netuser.userID.ToString()));
                }
            }
        }
       void HelpMenu(NetUser netuser)
        {
            rust.Notice(netuser, GetMessage("TestHelpIntro", netuser.userID.ToString()));
            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestHelp1", netuser.userID.ToString()));
            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestHelp2", netuser.userID.ToString()));
            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestHelp3", netuser.userID.ToString()));
            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestHelp4", netuser.userID.ToString()));
            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestHelp5", netuser.userID.ToString()));
            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestHelp6", netuser.userID.ToString()));
            rust.SendChatMessage(netuser, GetMessage("Prefix", netuser.userID.ToString()), GetMessage("TestHelp7", netuser.userID.ToString()));
        }
        void RecoilTest(NetUser targetuser)
        {
            rust.RunServerCommand(string.Format("{0} \"{1}\" \"{2}\" \"1\" \"8\"", "inv.giveplayer", targetuser.userID, "P250".ToString()));
            rust.RunClientCommand(targetuser, "input.bind Duck None None");
            rust.RunClientCommand(targetuser, "input.bind Jump None None");
            rust.RunClientCommand(targetuser, "input.bind Fire Mouse0 W");
            rust.RunClientCommand(targetuser, "input.bind AltFire None None");
            rust.RunClientCommand(targetuser, "input.bind Up None None");
            rust.RunClientCommand(targetuser, "input.bind Down None None");
            rust.RunClientCommand(targetuser, "input.bind Left None None");
            rust.RunClientCommand(targetuser, "input.bind Right None None");
            rust.RunClientCommand(targetuser, "input.bind Flashlight None None");
            rust.RunClientCommand(targetuser, "input.mousespeed 0.0");
            rust.InventoryNotice(targetuser, "+P250");
            rust.SendChatMessage(targetuser, GetMessage("Prefix", targetuser.userID.ToString()), GetMessage("TestTargetDirections", targetuser.userID.ToString()));
        }
        void MenuTest(NetUser targetuser)
        {
            rust.RunClientCommand(targetuser, "input.bind Duck None None");
            rust.RunClientCommand(targetuser, "input.bind Jump F2 Insert");
            rust.RunClientCommand(targetuser, "input.bind Fire None None");
            rust.RunClientCommand(targetuser, "input.bind AltFire None None");
            rust.RunClientCommand(targetuser, "input.bind Up F2 None");
            rust.RunClientCommand(targetuser, "input.bind Down Insert None");
            rust.RunClientCommand(targetuser, "input.bind Left F5 None");
            rust.RunClientCommand(targetuser, "input.bind Right None None");
            rust.RunClientCommand(targetuser, "input.bind Flashlight None None");
            rust.RunClientCommand(targetuser, "input.mousespeed 0.0");
            rust.SendChatMessage(targetuser, GetMessage("Prefix", targetuser.userID.ToString()), GetMessage("TestTargetDirections", targetuser.userID.ToString()));
        }
        void BindTest(NetUser targetuser)
        {
            rust.RunClientCommand(targetuser, "input.bind Duck None None");
            rust.RunClientCommand(targetuser, "input.bind Jump Keypad1 None");
            rust.RunClientCommand(targetuser, "input.bind Fire None None");
            rust.RunClientCommand(targetuser, "input.bind AltFire None None");
            rust.RunClientCommand(targetuser, "input.bind Up Keypad1 None");
            rust.RunClientCommand(targetuser, "input.bind Down None None");
            rust.RunClientCommand(targetuser, "input.bind Left None None");
            rust.RunClientCommand(targetuser, "input.bind Right None None");
            rust.RunClientCommand(targetuser, "input.bind Flashlight None None");
            rust.RunClientCommand(targetuser, "input.mousespeed 0.0");
            rust.SendChatMessage(targetuser, GetMessage("Prefix", targetuser.userID.ToString()), GetMessage("TestTargetDirections", targetuser.userID.ToString()));
        }
        void ScreenShotTest(NetUser targetuser)
        {
            rust.RunClientCommand(targetuser, "input.bind Duck None None");
            rust.RunClientCommand(targetuser, "input.bind Jump None None");
            rust.RunClientCommand(targetuser, "input.bind Fire None None");
            rust.RunClientCommand(targetuser, "input.bind AltFire None None");
            rust.RunClientCommand(targetuser, "input.bind Up None None");
            rust.RunClientCommand(targetuser, "input.bind Down None None");
            rust.RunClientCommand(targetuser, "input.mousespeed 0.0");
            rust.RunClientCommand(targetuser, "input.bind Left None None");
            rust.RunClientCommand(targetuser, "input.bind Right None None");
            rust.RunClientCommand(targetuser, "input.bind Flashlight None None");
            rust.RunClientCommand(targetuser, "gui.show_branding");
            rust.SendChatMessage(targetuser, GetMessage("Prefix", targetuser.userID.ToString()), GetMessage("TestScreenShotTarget1", targetuser.userID.ToString()));
            rust.SendChatMessage(targetuser, GetMessage("Prefix", targetuser.userID.ToString()), GetMessage("TestScreenShotTarget2", targetuser.userID.ToString()));
        }
        void ClearTest(NetUser targetuser)
        {
            rust.RunClientCommand(targetuser, "render.fov 60");
            rust.RunClientCommand(targetuser, "render.frames -1");
            rust.RunClientCommand(targetuser, "input.bind Up W None");
            rust.RunClientCommand(targetuser, "input.bind Down S None");
            rust.RunClientCommand(targetuser, "input.bind Left A None");
            rust.RunClientCommand(targetuser, "input.bind Right D None");
            rust.RunClientCommand(targetuser, "input.mousespeed 3.0");
            rust.RunClientCommand(targetuser, "input.bind Fire Mouse0 None");
            rust.RunClientCommand(targetuser, "input.bind AltFire Mouse1 none");
            rust.RunClientCommand(targetuser, "input.bind Sprint LeftShift none");
            rust.RunClientCommand(targetuser, "input.bind Duck LeftControl None");
            rust.RunClientCommand(targetuser, "input.bind Jump Space None");
            rust.RunClientCommand(targetuser, "input.bind Inventory Tab None");
            rust.RunClientCommand(targetuser, "gui.show");
            rust.RunClientCommand(targetuser, "gui.show_branding");
            rust.RunClientCommand(targetuser, "config.load");
            rust.SendChatMessage(targetuser, GetMessage("Prefix", targetuser.userID.ToString()), GetMessage("TestClearTarget", targetuser.userID.ToString()));
        }
        void FailTest(NetUser targetuser)
        {
            rust.RunClientCommand(targetuser, "render.fov 60");
            rust.RunClientCommand(targetuser, "render.frames -1");
            rust.RunClientCommand(targetuser, "input.bind Up W None");
            rust.RunClientCommand(targetuser, "input.bind Down S None");
            rust.RunClientCommand(targetuser, "input.bind Left A None");
            rust.RunClientCommand(targetuser, "input.bind Right D None");
            rust.RunClientCommand(targetuser, "input.mousespeed 2.0");
            rust.RunClientCommand(targetuser, "input.bind Fire Mouse0 None");
            rust.RunClientCommand(targetuser, "input.bind AltFire Mouse1 none");
            rust.RunClientCommand(targetuser, "input.bind Sprint LeftShift none");
            rust.RunClientCommand(targetuser, "input.bind Duck LeftControl None");
            rust.RunClientCommand(targetuser, "input.bind Jump Space None");
            rust.RunClientCommand(targetuser, "input.bind Inventory Tab None");
            rust.RunClientCommand(targetuser, "gui.show");
            rust.RunClientCommand(targetuser, "gui.show_branding");
            rust.RunClientCommand(targetuser, "config.load");
            rust.SendChatMessage(targetuser, GetMessage("Prefix", targetuser.userID.ToString()), GetMessage("TargetColor", targetuser.userID.ToString()) + targetuser.displayName + GetMessage("TestFailed", targetuser.userID.ToString()));
            rust.RunServerCommand("banid " + targetuser.userID + " testfailed");
            rust.RunServerCommand("kick " + targetuser.userID);
        }
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
    }
}

