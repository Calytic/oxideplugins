
namespace Oxide.Plugins
{
    [Info("EmptyLowFPS", "Dezito", "0.0.2", ResourceId = 1889)]
    [Description("Set low fps.limit when no players connected")]

    class EmptyLowFPS : RustPlugin
    {
		private static int MaxFPSLimit = 256;
		private static int EmptyFPSLimit = 30;
		
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
            CheckCfg<int>("MaxFPSLimit", ref MaxFPSLimit);
            CheckCfg<int>("EmptyFPSLimit", ref EmptyFPSLimit);
            SaveConfig();
			
			if (BasePlayer.activePlayerList.Count == 0)
				ServerEmpty();
			else
				ServerNotEmpty();
				
        }
		
		void OnPlayerConnected(Network.Message packet)
		{
			if (BasePlayer.activePlayerList.Count == 0)
				ServerNotEmpty();
		}
		
		void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			if (BasePlayer.activePlayerList.Count == 1)
				ServerEmpty();
		}
		
        void Unload()
        {
			if (MaxFPSLimit != null)
				ConsoleSystem.Run.Server.Normal("fps.limit "+MaxFPSLimit);
			else 
				ConsoleSystem.Run.Server.Normal("fps.limit 256");
        }
		
		void ServerNotEmpty()
		{
			Puts("Server is NOT empty setting fps.limit to " + MaxFPSLimit);
			ConsoleSystem.Run.Server.Normal("fps.limit "+MaxFPSLimit);
		}
		
		void ServerEmpty()
		{
			Puts("Server is empty setting fps.limit to " + EmptyFPSLimit);
			ConsoleSystem.Run.Server.Normal("fps.limit " + EmptyFPSLimit);
		}
		
    }
}
