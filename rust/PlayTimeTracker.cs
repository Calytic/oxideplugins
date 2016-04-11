/*
* Version 1.4
*/

using System;
using Oxide.Core;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins {
  [Info("Playtime and AFK Tracker", "ArcaneCraeda", 1.4)]
  [Description("Logs every players' play and afk time, separately.")]
  public class PlayTimeTracker : RustPlugin {

    protected override void LoadDefaultConfig() {
      PrintWarning("Creating a configuration file for PlayTimeTracker.");
      Config.Clear();
      Config["Afk Check Interval"] = 30;
      Config["Cycles Until Afk"] = 4;
      Config["Track AFK Time?"] = true;
      SaveConfig();
    }

    class PlayTimeData {
      public Dictionary<string, PlayTimeInfo> Players = new Dictionary<string, PlayTimeInfo>();

      public PlayTimeData() {  }
    };

    class PlayTimeInfo {
      public string SteamID;
      public string Name;
      public long PlayTime;
      public long AfkTime;
      public string HumanPlayTime;
      public string HumanAfkTime;
      public string LastSeen;

      public PlayTimeInfo() {  }

      public PlayTimeInfo(BasePlayer player) {
        SteamID = player.userID.ToString();
        Name = player.displayName;
        PlayTime = 0;
        AfkTime = 0;
        HumanPlayTime = "00:00:00";
        HumanAfkTime = "00:00:00";
        LastSeen = "Never";
      }
    };

    class PlayerStateData {
      public Dictionary<string, PlayerStateInfo> Players = new Dictionary<string, PlayerStateInfo>();

      public PlayerStateData() {  }
    };

    class PlayerStateInfo {
      public string SteamID;
      public long InitTimeStamp;
      public int AfkCount;
      public long AfkTime;
      public double[] Position;
      public string LiveName;

      public PlayerStateInfo() {  }

      public PlayerStateInfo(BasePlayer player) {
        InitTimeStamp = 0;
        SteamID = player.userID.ToString();
        AfkCount = 0;
        AfkTime = 0;
        Position = new double[3];
        LiveName = player.displayName;
      }
    };

    PlayTimeData playTimeData;
    PlayerStateData playerStateData = new PlayerStateData();

    public string prefix = "PlayTimeTracker: ";

    int afkCheckInterval { get { return Config.Get<int>("Afk Check Interval"); } }
    int cyclesUntilAfk { get { return Config.Get<int>("Cycles Until Afk"); } }
    bool afkCounts { get { return Config.Get<bool>("Track AFK Time?"); } }

    void Init() {
      Puts("PlayTimeTracker Initializing...");
      LoadPermissions();
    }

    void LoadPermissions() {
      string [] permissions = {"PlayTimeTracker.CanCheckPlayTime", "PlayTimeTracker.CanCheckAfkTime", "PlayTimeTracker.CanCheckLastSeen", "PlayTimeTracker.CanCheckSelfPlayTime", "PlayTimeTracker.CanCheckSelfAfkTime", "PlayTimeTracker.CanCheckSelfLastSeen"};
      for (int i = 0; i < permissions.Length; i++){
        if (!permission.PermissionExists(permissions[i])) { permission.RegisterPermission(permissions[i], this); }
      }
    }

    void OnPluginLoaded() {
      playTimeData = Interface.GetMod().DataFileSystem.ReadObject<PlayTimeData>("PlayTimeTracker");
      if (afkCounts) { timer.Repeat(afkCheckInterval, 0, () => afkCheck()); }
      foreach (BasePlayer player in BasePlayer.activePlayerList) { initPlayerState(player); }
    }

    void OnPluginUnloaded() {
      foreach (BasePlayer player in BasePlayer.activePlayerList) {
        savePlayerState(player);
      }
    }

    void OnPlayerInit(BasePlayer player) {
      var info = new PlayTimeInfo(player);
     
      if (!playTimeData.Players.ContainsKey(info.SteamID)) {
        playTimeData.Players.Add(info.SteamID, info);
      }
      playTimeData.Players[info.SteamID].Name = player.displayName;
      playTimeData.Players[info.SteamID].LastSeen = "Now";

      Interface.GetMod().DataFileSystem.WriteObject("PlayTimeTracker", playTimeData);

      initPlayerState(player);
    }

    void OnPlayerDisconnected(BasePlayer player) {
      savePlayerState(player);
    }


    [ChatCommand("playtime")]
    void cmdPlayTime(BasePlayer player, string cmd, string[] args) {
      string target = player.userID.ToString();
      if (args.Length!=0) {
        if (!hasPermission(player, "PlayTimeTracker.CanCheckPlayTime")) { return; }
        var queriedPlayer = args[0];
        string playerSteamID = FindPlayer(queriedPlayer);
        if (String.IsNullOrEmpty(playerSteamID)) {
          SendReply(player, prefix + "The player '" + queriedPlayer + "' does not exist in the system.");
          return; 
        }
        target = playerSteamID.ToString();
      } else {
        if (!hasPermission(player, "PlayTimeTracker.CanCheckSelfPlayTime")) { return; }
      }

      if (playerStateData.Players.ContainsKey(target)) {
        long currentTimestamp = GrabCurrentTimestamp();
        long initTimeStamp = playerStateData.Players[target].InitTimeStamp;
        long totalPlayed = currentTimestamp - initTimeStamp;
        if (playTimeData.Players.ContainsKey(target)){totalPlayed += playTimeData.Players[target].PlayTime;}
        TimeSpan humanPlayTime = TimeSpan.FromSeconds(totalPlayed);
        player.ChatMessage(playerStateData.Players[target].LiveName + "'s total playtime: " + string.Format("{0:c}", humanPlayTime));
      }else{
         player.ChatMessage("The player has never been seen on the server.");
      }
    }

    [ChatCommand("afktime")]
    void cmdAfkTime(BasePlayer player, string cmd, string[] args) {
      string target = player.userID.ToString();
      if (args.Length!=0) {
        if (!hasPermission(player, "PlayTimeTracker.CanCheckAfkTime")) { return; }
        var queriedPlayer = args[0];
        string playerSteamID = FindPlayer(queriedPlayer);
        if (String.IsNullOrEmpty(playerSteamID)) {
          SendReply(player, prefix + "The player '" + queriedPlayer + "' does not exist in the system.");
          return;
        }
        target = playerSteamID.ToString();
      } else {
        if (!hasPermission(player, "PlayTimeTracker.CanCheckSelfAfkTime")) { return; }
      }

      if (playerStateData.Players.ContainsKey(target)) {
        long afkTime = playerStateData.Players[target].AfkTime;
        if (playTimeData.Players.ContainsKey(target)){afkTime += playTimeData.Players[target].AfkTime;}
        TimeSpan humanAfkTime = TimeSpan.FromSeconds(afkTime);
        player.ChatMessage(playerStateData.Players[target].LiveName + "'s time spent AFK: " + string.Format("{0:c}", humanAfkTime));
      }else{
         player.ChatMessage("The player has never been seen on the server.");
      }
    }

    [ChatCommand("lastseen")]
    void cmdLastSeen(BasePlayer player, string command, string[] args) {
      string target = player.userID.ToString();
      if (args.Length!=0) {
        if (!hasPermission(player, "PlayTimeTracker.CanCheckLastSeen")) { return; }
        var queriedPlayer = args[0];
        string playerSteamID = FindPlayer(queriedPlayer);
        if (String.IsNullOrEmpty(playerSteamID)) {
          SendReply(player, prefix + "The player '" + queriedPlayer + "' does not exist in the system.");
          return;
        }
        target = playerSteamID.ToString();
      } else {
        if (!hasPermission(player, "PlayTimeTracker.CanCheckSelfLastSeen")) { return; }
      }

      if (playTimeData.Players.ContainsKey(target)) {
        player.ChatMessage(playTimeData.Players[target].Name + " was last seen " + playTimeData.Players[target].LastSeen);
      }else{
         player.ChatMessage("The player has never been seen on the server.");
      }
    }

    // Master AFK checking function, iterates through all connected players.
    private void afkCheck() {
      foreach (BasePlayer player in BasePlayer.activePlayerList) {
        var state = new PlayerStateInfo(player);

        if (playerStateData.Players.ContainsKey(state.SteamID)) {
          double currentX = Math.Round(player.transform.position.x, 2);
          double currentY = Math.Round(player.transform.position.y, 2);
          double currentZ = Math.Round(player.transform.position.z, 2);

          double[] storedPos = playerStateData.Players[state.SteamID].Position;

          if (currentX == storedPos[0] && currentY == storedPos[1] && currentZ == storedPos[2]) {
            playerStateData.Players[state.SteamID].AfkCount += 1;
          } else {
            playerStateData.Players[state.SteamID].AfkCount = 0;
            playerStateData.Players[state.SteamID].Position[0] = currentX;
            playerStateData.Players[state.SteamID].Position[1] = currentY;
            playerStateData.Players[state.SteamID].Position[2] = currentZ;
          }

          if (playerStateData.Players[state.SteamID].AfkCount > cyclesUntilAfk) {
            playerStateData.Players[state.SteamID].AfkTime += afkCheckInterval;
          }
        }
      }
    }

    private void initPlayerState(BasePlayer player) {
      long currentTimestamp = GrabCurrentTimestamp();
      var state = new PlayerStateInfo(player);

      if (!playerStateData.Players.ContainsKey(state.SteamID))
      {
        playerStateData.Players.Add(state.SteamID, state);
      }
      playerStateData.Players[state.SteamID].InitTimeStamp = currentTimestamp;
      playerStateData.Players[state.SteamID].AfkTime = 0;
      playerStateData.Players[state.SteamID].AfkCount = 0;
      playerStateData.Players[state.SteamID].LiveName = player.displayName;

      playerStateData.Players[state.SteamID].Position[0] = Math.Round(player.transform.position.x, 2);
      playerStateData.Players[state.SteamID].Position[1] = Math.Round(player.transform.position.y, 2);
      playerStateData.Players[state.SteamID].Position[2] = Math.Round(player.transform.position.z, 2);
    }

    private void savePlayerState(BasePlayer player) {
      long currentTimestamp = GrabCurrentTimestamp();
      var info = new PlayTimeInfo(player);
      var state = new PlayerStateInfo(player);

      if (!playTimeData.Players.ContainsKey(info.SteamID)){
        playTimeData.Players.Add(info.SteamID, info);
      }
      long initTimeStamp = playerStateData.Players[state.SteamID].InitTimeStamp;
      long afkTime = playerStateData.Players[state.SteamID].AfkTime;
      long totalPlayed = currentTimestamp - initTimeStamp;

      playTimeData.Players[info.SteamID].AfkTime += afkTime;
      TimeSpan humanAfkTime = TimeSpan.FromSeconds(playTimeData.Players[info.SteamID].AfkTime);
      playTimeData.Players[info.SteamID].HumanAfkTime = string.Format("{0:c}", humanAfkTime);

      playTimeData.Players[info.SteamID].PlayTime += totalPlayed;
      TimeSpan humanPlayTime = TimeSpan.FromSeconds(playTimeData.Players[info.SteamID].PlayTime);
      playTimeData.Players[info.SteamID].HumanPlayTime = string.Format("{0:c}", humanPlayTime);

      playTimeData.Players[info.SteamID].LastSeen = (DateTime.Now).ToString("G");

      Interface.GetMod().DataFileSystem.WriteObject("PlayTimeTracker", playTimeData);
    }

    private static long GrabCurrentTimestamp() {
      long timestamp = 0;
      long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
      ticks /= 10000000;
      timestamp = ticks;

      return timestamp;
    }

    private bool hasPermission(BasePlayer player, string _permission) {
      if (permission.UserHasPermission(player.userID.ToString(), _permission)) { return true; }
      player.ChatMessage("You do not have access to this command.");
      return false;
    }

    private string FindPlayer(string name) {
      foreach (var player in playTimeData.Players) {
        if (player.Value.Name.ToLower().Contains(name.ToLower())) { return player.Value.SteamID; }
      }
      return "";
    }
  };
};
