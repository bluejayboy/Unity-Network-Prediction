using System.Collections.Generic;
using UnityEngine;
using Mirror;
using KinematicCharacterController;
using Black.Utility;

namespace Black.ClientSidePrediction
{
    [DisallowMultipleComponent]
    public sealed class AuthoritativeCharacterSystem : NetworkBehaviour
    {
        public static AuthoritativeCharacterSystem Instance { get; private set; }

        [SerializeField] private byte tickRate = 30;
        [SerializeField] private byte updateRate = 60;
        public float UpdateRate => updateRate;

        private int tickRateTimer;
        private int tickRateQuotient;

        private List<AuthoritativeCharacterMotor> motors = new List<AuthoritativeCharacterMotor>();
        private Dictionary<NetworkConnection, List<ClientInput>> clientInputs = new Dictionary<NetworkConnection, List<ClientInput>>();

        private void Awake()
        {
            Instance = this;
            tickRateQuotient = updateRate / tickRate;

            BlackUtility.ChangePhysicsFrameRate(updateRate);
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            tickRateTimer++;

            if (tickRateTimer < tickRateQuotient)
            {
                return;
            }

            tickRateTimer = 0;

            for (int i = 0; i < tickRateQuotient; i++)
            {
                SimulateAllMotors();
            }
        }

        [Command(requiresAuthority = false)]
        public void SendInputToServer(ClientInput input, NetworkConnectionToClient conn = null)
        {
            if (clientInputs.ContainsKey(conn))
            {
                clientInputs[conn].Add(input);
            }
        }

        public void AddMotor(NetworkConnection conn, AuthoritativeCharacterMotor motor)
        {
            if (!motors.Contains(motor))
            {
                motors.Add(motor);
            }

            if (!clientInputs.ContainsKey(conn))
            {
                clientInputs.Add(conn, new List<ClientInput>());
            }
        }

        public void RemoveMotor(AuthoritativeCharacterMotor motor)
        {
            if (motors.Contains(motor))
            {
                motors.Remove(motor);
            }
        }

        private void SimulateAllMotors()
        {
            for (int i = 0; i < motors.Count; i++)
            {
                if (clientInputs.ContainsKey(motors[i].connectionToClient))
                {
                    ApplyInput(motors[i]);
                }
            }

            KinematicCharacterSystem.ForceSimulate();

            for (int i = 0; i < motors.Count; i++)
            {
                if (clientInputs.ContainsKey(motors[i].connectionToClient))
                {
                    ApplyResult(motors[i]);
                }
            }
        }

        private void ApplyInput(AuthoritativeCharacterMotor motor)
        {
            List<ClientInput> inputs = clientInputs[motor.connectionToClient];

            if (inputs.Count <= 0)
            {
                return;
            }

            motor.SetInput(inputs[0]);
        }

        private void ApplyResult(AuthoritativeCharacterMotor motor)
        {
            List<ClientInput> inputs = clientInputs[motor.connectionToClient];

            if (inputs.Count <= 0)
            {
                return;
            }

            ServerResult result = motor.GetResult();
            result.TimeFrame = inputs[0].TimeFrame;
            result.InputBuffers = (byte)inputs.Count;

            inputs.RemoveAt(0);
            motor.SendResultToClient(result);
        }
    }
}