using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Fly", "Mughisi", "1.1.1")]
    public class Fly : HurtworldPlugin
    {
        private readonly Hash<PlayerSession, PlayerInfo> players = new Hash<PlayerSession, PlayerInfo>();
        private readonly MethodInfo accelerate = typeof(CharacterMotorSimple).GetMethod("Accelerate", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo motorState = typeof(CharacterMotorSimple).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);

        private class PlayerInfo
        {
            public readonly PlayerSession Session;
            public bool IsFlying;
            public float BaseSpeed;

            public PlayerInfo(PlayerSession session)
            {
                Session = session;
                IsFlying = false;
                BaseSpeed = 75f;
            }
        }

        void Loaded()
        {
            LoadDefaultMessages();
            permission.RegisterPermission("fly.allowed", this);

            foreach (var session in GameManager.Instance.GetSessions().Values)
                players.Add(session, new PlayerInfo(session));
        }

        private void Unload()
        {
            foreach (var player in players.Values)
            {
                if (!player.IsFlying) continue;
                SetFlymode(player.Session);
            }
        }

        private void OnPlayerConnected(PlayerSession session)
        {
            if (players.ContainsKey(session)) return;
            players.Add(session, new PlayerInfo(session));
        }

        private void OnPlayerDisconnect(PlayerSession session)
        {
            if (players.ContainsKey(session))
                players.Remove(session);
        }

        void OnPlayerInput(PlayerSession session, InputControls input)
        {
            if (!players[session].IsFlying) return;

            var motor = session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            if (!motor) return;
            var state = (CharMotorRewindDependantState)motorState.GetValue(motor);

            var direction = new Vector3(IntFromBool(input.StrafeLeft) * -1 + IntFromBool(input.StrafeRight), 0f, IntFromBool(input.Backward) * -1 + IntFromBool(input.Forward));
            var speed = players[session].BaseSpeed;

            motor.IsGrounded = true;
            direction = motor.RotationToLookQuaternionXCache * direction.normalized;

            if (input.Forward) direction.y = input.DirectionVector.y;
            if (input.Backward) direction.y = -input.DirectionVector.y;
            if (input.Sprint) speed *= 2;
            if (state.IsCrouching) speed /= 2;

            motor.Set_currentVelocity((Vector3)accelerate.Invoke(motor, new object[] { direction, speed, motor.GetVelocity(), 5, 5 }));

            if (!stats) return;
            stats.GetFluidEffect(EEntityFluidEffectType.ColdBar).Reset(true);
            stats.GetFluidEffect(EEntityFluidEffectType.Radiation).Reset(true);
            stats.GetFluidEffect(EEntityFluidEffectType.HeatBar).Reset(true);
            stats.GetFluidEffect(EEntityFluidEffectType.Dampness).Reset(true);
            stats.GetFluidEffect(EEntityFluidEffectType.Hungerbar).Reset(true);
            stats.GetFluidEffect(EEntityFluidEffectType.BodyTemperature).Reset(true);
            stats.GetFluidEffect(EEntityFluidEffectType.Toxin).Reset(true);
        }

        [ChatCommand("fly")]
        private void FlyCommand(PlayerSession session, string command, string[] args)
        {
            if (!session.IsAdmin && !permission.UserHasPermission(session.SteamId.ToString(), "fly.allowed"))
            {
                SendMessage(session, "No Permission");
                return;
            }

            if (args.Length > 0)
            {
                float speed;
                if (float.TryParse(args[0], out speed))
                {
                    SetFlymode(session, speed);
                    return;
                }
            }

            SetFlymode(session);
        }

        private Vector3 Ground(PlayerSession session)
        {
            var position = session.WorldPlayerEntity.transform.position;
            if (players[session].IsFlying) return position;
            var hits = Physics.RaycastAll(position, Vector3.down);
            if (hits.Length == 0) return position;
            if (hits[0].distance < 5) return position;
            return new Vector3(position.x, hits[0].point.y + 2, position.z);
        }

        private void SetFlymode(PlayerSession session, float speed = 75f)
        {
            var motor = session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();

            if (!motor) return;

            players[session].IsFlying = !players[session].IsFlying;

            if (players[session].IsFlying)
            {
                motor.GravityVector = Vector3.zero;
                motor.AirSpeedModifier = 1f;
                motor.FallDamageMultiplier = 0;
                players[session].BaseSpeed = speed;
            }
            else
            {
                session.WorldPlayerEntity.transform.position = Ground(session);
                motor.GravityVector = new Vector3(0f, -25f, 0f);
                motor.AirSpeedModifier = 0.2f;
                motor.FallDamageMultiplier = 1.5f;
            }

            AlertManager.Instance.GenericTextNotificationServer(
                players[session].IsFlying
                    ? lang.GetMessage("Enabled", this, session.SteamId.ToString())
                    : lang.GetMessage("Disabled", this, session.SteamId.ToString()), session.Player);
        }

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"Enabled", "Fly mode enabled"},
                {"Disabled", "Fly mode disabled"},
                {"No Permission", "You don't have permission to use this command."}
            };

            lang.RegisterMessages(messages, this);
        }

        private int IntFromBool(bool val)
            => (!val ? 0 : 1);

        private void SendMessage(PlayerSession session, string message)
            => hurt.SendChatMessage(session, lang.GetMessage(message, this, session.SteamId.ToString()));
    }
}
