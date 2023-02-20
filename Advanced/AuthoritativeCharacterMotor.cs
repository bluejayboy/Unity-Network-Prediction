using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Black.Utility;

namespace Black.ClientSidePrediction
{
    [DisallowMultipleComponent]
    public abstract class AuthoritativeCharacterMotor : NetworkBehaviour
    {
        private ulong inputFrame;
        private bool speedUp;
        private bool slowDown;

        private ClientInput currentInput;
        private ServerResult currentResult;

        private List<ClientInput> inputs = new List<ClientInput>();

        protected abstract ClientInput GetInput();
        public abstract void SetInput(ClientInput input);
        public abstract ServerResult GetResult();
        protected abstract void SetResult(ServerResult result);
        public abstract void ApplyPhysics();

        protected virtual void Start()
        {
            if (NetworkServer.active && AuthoritativeCharacterSystem.Instance != null)
            {
                AuthoritativeCharacterSystem.Instance.AddMotor(connectionToClient, this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (NetworkServer.active && AuthoritativeCharacterSystem.Instance != null)
            {
                AuthoritativeCharacterSystem.Instance.RemoveMotor(this);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!hasAuthority || AuthoritativeCharacterSystem.Instance == null)
            {
                return;
            }

            inputFrame++;

            CreateInput();
            Reconciliate();
            Predict();

            AuthoritativeCharacterSystem.Instance.SendInputToServer(currentInput);
        }

        [TargetRpc]
        public void SendResultToClient(ServerResult result)
        {
            currentResult = result;

            float updateRate = AuthoritativeCharacterSystem.Instance.UpdateRate;

            if (speedUp)
            {
                slowDown = result.InputBuffers > 20;
                speedUp = result.InputBuffers < 8;
            }
            else if (slowDown)
            {
                slowDown = result.InputBuffers > 6;
                speedUp = result.InputBuffers < 2;
            }
            else
            {
                slowDown = result.InputBuffers > 8;
                speedUp = result.InputBuffers < 3;
            }

            if (slowDown)
            {
                BlackUtility.ChangePhysicsFrameRate(updateRate - 8);
            }
            else if (speedUp)
            {
                BlackUtility.ChangePhysicsFrameRate(updateRate + 8);
            }
            else
            {
                BlackUtility.ChangePhysicsFrameRate(updateRate);
            }
        }

        private void CreateInput()
        {
            currentInput = GetInput();
            currentInput.TimeFrame = inputFrame;
        }

        private void Reconciliate()
        {
            if (isServer)
            {
                return;
            }

            SetResult(currentResult);
        }

        private void Predict()
        {
            if (isServer)
            {
                return;
            }

            inputs.RemoveAll(IsObsoleteInput);
            inputs.Add(currentInput);

            for (int i = 0; i < inputs.Count; i++)
            {
                SetInput(inputs[i]);
                ApplyPhysics();
            }
        }

        private bool IsObsoleteInput(ClientInput input)
        {
            return input.TimeFrame <= currentResult.TimeFrame;
        }
    }
}