using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("NoFallDmg", "Noviets", "1.0.0")]
    [Description("Disables fall damage")]

    class NoFallDmg : HurtworldPlugin
    {
		void OnLoaded() 
		{
			foreach(PlayerSession session in GameManager.Instance.GetSessions().Values)
			{
				if(session.IsLoaded)
				{
					CharacterMotorSimple motor = session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();
					motor.FallDamageMultiplier = 0f;
				}
			}
		}
		void OnPlayerInit(PlayerSession session)
		{
			CharacterMotorSimple motor = session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();
			motor.FallDamageMultiplier = 0f;
		}
	}
}