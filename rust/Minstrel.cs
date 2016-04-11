using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Text;

namespace Oxide.Plugins
{
    [Info("Minstrel", "4seti [Lunatiq] for Rust Planet", "0.0.7", ResourceId = 981)]
    public class Minstrel : RustPlugin
    {

        #region Utility Methods

        private void Log(string message)
        {
            Puts("{0}: {1}", Title, message);
        }

        private void Warn(string message)
        {
            PrintWarning("{0}: {1}", Title, message);
        }

        private void Error(string message)
        {
            PrintError("{0}: {1}", Title, message);
        }

        // Gets a config value of a specific type
        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
                return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        #endregion

        void Loaded()
        {
            Log("Loaded");
        }

        #region VARS
        private static FieldInfo serverinput;
        Dictionary<string, List<TuneNote>> tuneDict;
        Dictionary<ulong, List<TuneNote>> tuneRecTemp;
        List<string> cfgTunes;
        #endregion

        // Loads the default configuration
        protected override void LoadDefaultConfig()
        {
            Log("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }

        void LoadVariables()
        {
            Config["tunes"] = new List<string>();
            Config["version"] = Version;
        }

        void Init()
        {
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Instance | BindingFlags.NonPublic));
            try
            {
                LoadConfig();
                tuneRecTemp = new Dictionary<ulong, List<TuneNote>>();
                var version = GetConfig<Dictionary<string, object>>("version", null);
                VersionNumber verNum = new VersionNumber(Convert.ToUInt16(version["Major"]), Convert.ToUInt16(version["Minor"]), Convert.ToUInt16(version["Patch"]));
                var savedTunes = GetConfig<List<object>>("tunes", new List<object>());
                tuneDict = new Dictionary<string,List<TuneNote>>();
                cfgTunes = new List<string>();
                foreach (var savedTune in savedTunes)
                {
                    cfgTunes.Add((string)savedTune);
                    List<TuneNote> loadTune;
                    if (LoadTune((string)savedTune, out loadTune))
                        tuneDict.Add((string)savedTune, loadTune);
                }

            }
            catch (Exception ex)
            {
                Error("Init failed: " + ex.Message);
            }
        }
        void Unload()
        {
            DestroyAll<KeyboardGuitar>();
        }
        private static void DestroyAll<T>()
        {
            UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (UnityEngine.Object gameObj in objects)
                    GameObject.Destroy(gameObj);
        }
        
        
        [ChatCommand("ms")]
        void cmdToggleMS(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            if (player.GetComponent<KeyboardGuitar>() == null)
            {
                player.gameObject.AddComponent<KeyboardGuitar>();
                player.ChatMessage("ON - Added");
                player.ChatMessage(BuildNoteList(2.9f, 0));
            }
            else
            {
                if (player.GetComponent<KeyboardGuitar>().enabled)
                {
                    player.ChatMessage("OFF");
                    player.GetComponent<KeyboardGuitar>().enabled = false;
                }
                else
                {
                    player.ChatMessage("ON");
                    player.GetComponent<KeyboardGuitar>().enabled = true;
                    player.ChatMessage(BuildNoteList(player.GetComponent<KeyboardGuitar>().adjust, player.GetComponent<KeyboardGuitar>().start));
                }
            }
        }

        private bool LoadTune(string tuneName, out List<TuneNote> list)
        {
            try
            {
				list = Interface.GetMod().DataFileSystem.ReadObject<List<TuneNote>>("ms-" + tuneName);				
                return true;                
            }
            catch (Exception ex)
            {
				list = new List<TuneNote>();
                return false;
            }
        }
        void SaveTune(string playerName, string tuneName, ulong userID)
        {
            Interface.GetMod().DataFileSystem.WriteObject<List<TuneNote>>("ms-" + tuneName, tuneRecTemp[userID]);
            Log("Tune " + tuneName + " saved by " + playerName);
        }

        private string BuildNoteList(float adjust, float start)
        {
            string noteList = string.Empty;
            for (int i = 1; i < 9; i++)
            {
                noteList += string.Format("{0}: {1:0.00} ", i, (i-1) / adjust + start);
            }
            return noteList;
        }

        [ChatCommand("ms_tune")]
        void cmdTune(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            if (!checkComponent(player)) return;
            if (args.Length == 0) return;
            if (tuneDict.ContainsKey(args[0]))
            {
				if (!player.GetComponent<KeyboardGuitar>().playingTune)
				{
					player.ChatMessage("Playing: " + args[0]);
					playTune(player, args[0]);
				}
				else
				{
					player.ChatMessage("Already playing");
				}
            }
            else
                player.ChatMessage("Tune doesn't exists: " + args[0]);
        }
        
        [ChatCommand("ms_rec")]
        void cmdRec(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            if (!checkComponent(player)) return;
            if (args.Length == 0)
            {
                if (!player.GetComponent<KeyboardGuitar>().Recording)
                {
                    player.GetComponent<KeyboardGuitar>().Recording = true;
                    player.ChatMessage("Recording started...");
                }
                else
                {
                    player.GetComponent<KeyboardGuitar>().Recording = false;
                    if (tuneRecTemp.ContainsKey(player.userID))
                        tuneRecTemp[player.userID] = player.GetComponent<KeyboardGuitar>().recTune;
                    else
                        tuneRecTemp.Add(player.userID, player.GetComponent<KeyboardGuitar>().recTune);
                    player.GetComponent<KeyboardGuitar>().recTune = null;
                    //foreach (var item in playbackTune)
                    //{
                    //    player.ChatMessage(string.Format("NoteScale: {0:N2} - Delay: {1:N2}", item.NoteScale, item.Delay));
                    //}                    
                    player.ChatMessage("Recording stoped!");
                    player.ChatMessage(string.Format("Notes: {0} - Length: {1:N2}", tuneRecTemp[player.userID].Count, tuneRecTemp[player.userID].Sum(x => x.Delay)));
                }
            }
            else if (args.Length > 1 && !player.GetComponent<KeyboardGuitar>().Recording)
            {
                if (args[0] == "save")
                {
                    string tuneName = args[1];
                    if (!tuneRecTemp.ContainsKey(player.userID))
                    {
                        player.ChatMessage("Nothing to save!");
                    }
                    else
                    {
                        SaveTune(player.displayName, tuneName, player.userID);
                        if (tuneDict.ContainsKey(tuneName)) tuneDict.Remove(tuneName);
                        tuneDict.Add(tuneName, tuneRecTemp[player.userID]);
                        player.ChatMessage("Tune " + tuneName + " saved!");
                        cfgTunes.Add(tuneName);
                        Config["tunes"] = cfgTunes;
                        SaveConfig();
                        tuneRecTemp.Remove(player.userID);
                    }
                }
            }

        }
        void playTune(BasePlayer player, string tuneName)
        {
            player.GetComponent<KeyboardGuitar>().curTune = tuneDict[tuneName];
            player.GetComponent<KeyboardGuitar>().PlayTune();            
        }

        private bool checkComponent(BasePlayer player)
        {
            if (player.GetComponent<KeyboardGuitar>() == null)
            {
                player.ChatMessage("You have " + Title + " disabled!");
                return false;
            }
            return true;
        }

        List<object> getTune(string tuneName)
        {
            if (!tuneDict.ContainsKey(tuneName)) return null;
            var tunes = new List<object>();
            foreach (TuneNote note in tuneDict[tuneName])
            {
                var tunenote = new Dictionary<string, object>();
                tunenote.Add("NoteScale", note.NoteScale);
                tunenote.Add("Delay", note.Delay);
                tunenote.Add("Pluck", note.Pluck);
                tunes.Add(tunenote);
            }
            return tunes;
        }

        [ChatCommand("ms_adj")]
        void cmdMSAdjust(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            if (args.Length == 0) return;

            if (!checkComponent(player))
            {
                player.ChatMessage("Turn it ON, before using this");
            }
            else
            {
                if (player.GetComponent<KeyboardGuitar>().enabled)
                {
                    float adjust;
                    if (float.TryParse(args[0], out adjust))
                    {
                        player.GetComponent<KeyboardGuitar>().adjust = adjust;
                        player.ChatMessage(string.Format("Adjusted to: {0:N2}", adjust));
                    }
                    if (args.Length > 1)
                    {
                        float start;
                        if (float.TryParse(args[1], out start))
                        {
                            player.GetComponent<KeyboardGuitar>().start = start;
                            player.ChatMessage(string.Format("Start set to: {0:N2}", start));
                        }
                    }
                    player.ChatMessage(BuildNoteList(player.GetComponent<KeyboardGuitar>().adjust, player.GetComponent<KeyboardGuitar>().start));
                }
                else
                {
                    player.ChatMessage("Turn it ON, before using this");               
                }
            }
        }

        [ChatCommand("ms_list")]
        void cmdMSList(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            if (tuneDict.Count > 0)
            {
                player.ChatMessage("List of tunes avaliable");
                foreach (var tune in tuneDict)
                {
                    player.ChatMessage(string.Format("Name: {0}, Duration: {1:N2} seconds", tune.Key, tune.Value.Sum(x => x.Delay)));
                }
            }
            else
                player.ChatMessage("List of tunes is EMPTY");
        }

        [ChatCommand("ms_rel")]
        void cmdMSReload(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            if (args.Length == 0) return;
            List<TuneNote> tune;
            if (LoadTune(args[0], out tune))
            {
                if (tuneDict.ContainsKey(args[0]))
                {
                    tuneDict[args[0]] = tune;
                    player.ChatMessage("Reloaded: " + args[0]);
                }
                else
                {
                    tuneDict.Add(args[0], tune);
                    cfgTunes.Add(args[0]);
                    Config["tunes"] = cfgTunes;
                    SaveConfig();
                    player.ChatMessage("Tune: " + args[0] + " was added to playlist");
                }
            }
            else
            {
                player.ChatMessage("File not found!");
            }
        }

        [ChatCommand("ms_del")]
        void cmdMSDelete(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            if (args.Length == 0) return;
            List<TuneNote> tune;
            if (LoadTune(args[0], out tune))
            {
                if (tuneDict.ContainsKey(args[0]))
                {
                    cfgTunes.Remove(args[0]);
                    Config["tunes"] = cfgTunes;
                    SaveConfig();
                    tuneDict.Remove(args[0]);
                    player.ChatMessage("Removed from loadup: " + args[0] + " remove file in /data/ folder manually");
                }
                else
                {
                    player.ChatMessage("Tune wasn't added to loadup, but file exists!");
                }
            }
            else
            {
                player.ChatMessage("File not found!");
            }
        }

		[ChatCommand("msp")]
		void cmdMSPluck(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;
			if (args.Length == 0) return;
			if (!checkComponent(player))
			{
				player.ChatMessage("Turn it ON, before using this");
			}
			else
			{
				if (player.GetComponent<KeyboardGuitar>().enabled)
				{
					float scale;
					float repeat = 1;
					if (args.Length > 1)
						float.TryParse(args[1], out repeat);

                    if (float.TryParse(args[0], out scale))
					{
						List<TuneNote> repeatNote = new List<TuneNote>();
						for (int i = 0; i < repeat; i++)
						{
							repeatNote.Add(new TuneNote(scale, 2f));
                        }
						player.GetComponent<KeyboardGuitar>().curTune = repeatNote;
						if (!player.GetComponent<KeyboardGuitar>().playingTune)
						{
							player.GetComponent<KeyboardGuitar>().PlayTune();
							player.ChatMessage(string.Format("Note playing: {0:N2} - {1} times", scale, repeat));
						}
						else
							player.ChatMessage("Already playing");
					}					
				}
				else
				{
					player.ChatMessage("Turn it ON, before using this");
				}
			}
		}

		public class KeyboardGuitar : MonoBehaviour
        {
            private static float noteTime = 0.1f;

            public float adjust, start, NextTimeToPress, noteToPlay;
            private InputState input;
            public bool Recording = false;
            public BasePlayer owner;
            public List<TuneNote> recTune;
            public float nextNoteTime;
            //private TuneNote prevNote;
            Effect effectP = new Effect("fx/gestures/guitarpluck", new Vector3(0, 0, 0), Vector3.forward);
            Effect effectS = new Effect("fx/gestures/guitarstrum", new Vector3(0, 0, 0), Vector3.forward);
            void Awake()
            {
                owner = GetComponent<BasePlayer>();
                input = serverinput.GetValue(owner) as InputState;
                //enabled = false;
                adjust = 2.9f;
                NextTimeToPress = 0f;
                start = 0f;
            }
            void FixedUpdate()
            {
                float time = Time.realtimeSinceStartup;
                if (input.current.buttons != input.previous.buttons && input.current.buttons != 0 && NextTimeToPress < time)
                {
                    float num_shift = input.WasDown(BUTTON.SPRINT) ? 0.2f : 0;
                    bool Strum = input.WasDown(BUTTON.DUCK);
                        //fx/gestures/guitarstrum
                    NextTimeToPress = time + noteTime;
                    float num = (float)Math.Log((input.current.buttons / 262144), 2) / adjust + num_shift + start;
                    //owner.ChatMessage(num.ToString("N2"));
                    if (float.IsInfinity(num)) return;
                    if (num > 7) num = 7;
                    else if (num < -2) num = -2;
                    if (!Strum)
                    {
                        effectP.worldPos = transform.position;
                        effectP.origin = transform.position;
                        effectP.scale = num;
                        EffectNetwork.Send(effectP);
                    }
                    else
                    {
                        effectS.worldPos = transform.position;
                        effectS.origin = transform.position;
                        effectS.scale = num;
                        EffectNetwork.Send(effectS);
                    }
                    if (Recording)
                    {
                        if (recTune == null) recTune = new List<TuneNote>();
                        if (recTune.Count == 0)
                        {
                            recTune = new List<TuneNote>();
                            TuneNote curNote = new TuneNote(num, 0f, !Strum);
                            recTune.Add(curNote);
                            nextNoteTime = time;
                        }
                        else
                        {
                            recTune[recTune.Count - 1].Delay = time - nextNoteTime;
                            TuneNote curNote = new TuneNote(num, 0f, !Strum);
                            recTune.Add(curNote);
                            nextNoteTime = time;
                        }
                    }
                }   
            }

			public void PlayNote(float scale)
			{				
					effectP.worldPos = transform.position;
					effectP.origin = transform.position;
					effectP.scale = scale;
					EffectNetwork.Send(effectP);
			}

			public List<TuneNote> curTune;
			private Stack<TuneNote> tuneToPlay;
            public bool playingTune = false;
			private TuneNote nextNote;
			public void PlayTune()
			{
				tuneToPlay = new Stack<TuneNote>();
				foreach (var note in curTune.Reverse<TuneNote>())				
					tuneToPlay.Push(note);


				curTune = new List<TuneNote>();
				if (tuneToPlay.Count > 0)
				{
					nextNote = tuneToPlay.Pop();
					if (!playingTune)
						PlayTuneStack();
				}
            }
            private void PlayTuneStack()
            {
                if (!playingTune) playingTune = true;
                if (nextNote.Pluck)
                {
                    effectP.worldPos = transform.position;
                    effectP.origin = transform.position;
                    effectP.scale = nextNote.NoteScale;
                    EffectNetwork.Send(effectP);
                }
                else
                {
                    effectS.worldPos = transform.position;
                    effectS.origin = transform.position;
                    effectS.scale = nextNote.NoteScale;
                    EffectNetwork.Send(effectS);
                }
                if (tuneToPlay.Count > 0 && enabled)
                {					
					Invoke("PlayTuneStack", nextNote.Delay);
					nextNote = tuneToPlay.Pop();
				}
                else
                {
					if (!enabled)
						tuneToPlay = new Stack<TuneNote>();
					playingTune = false;
                }
            }
        }
        public class TuneNote
        {
            public float NoteScale, Delay;
            public bool Pluck;
            public TuneNote(float note, float delay, bool pluck = true)
            {
                NoteScale = note;
                Delay = delay;
                Pluck = pluck;
            }
        }
    }
}
