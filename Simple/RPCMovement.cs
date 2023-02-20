using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;

public class RPCMovement : NetworkBehaviour
{
    public struct State
    {
        public int bufferedFrames;
        public int timestamp;
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    public struct Command
    {
        public sbyte move;
        public sbyte rotate;
    }

    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float rotateSpeed = 50.0f;
    [SerializeField] private float fixedUpdateInterval = 0.02f;
    [SerializeField] private float lerpSpeed = 0.1f;
    [SerializeField] private float deltaIncrease = 0.001f;

     private State state;

    private State predictedState;
    private List<Command> clientCommands;
    private List<Command> serverCommands = new List<Command>(64);

    private void Awake()
    {
        state = new State
        {
            timestamp = 0,
            position = Vector3.zero,
            rotation = Quaternion.identity
        };
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            clientCommands = new List<Command>(64);
        }
    }

    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            if (state.bufferedFrames <= 1)
            {
                Time.fixedDeltaTime = fixedUpdateInterval - deltaIncrease;
            }
            else if (state.bufferedFrames > 5)
            {
                Time.fixedDeltaTime = fixedUpdateInterval + deltaIncrease * (state.bufferedFrames - 4);
            }
            else
            {
                Time.fixedDeltaTime = fixedUpdateInterval;
            }

            Command command = CreateCommand();

            if (command.move != 0 || command.rotate != 0 || Vector3.Distance(predictedState.position, state.position) > 1)
            {
                clientCommands.Add(command);
                UpdatePredictedState();
                CmdMove(command);
            }

            if (clientCommands.Count > 60)
            {
                clientCommands.Clear();
            }
        }

        if (isServer)
        {
            if (serverCommands.Count > 60)
            {
                serverCommands.Clear();
            }

            if (serverCommands.Count > 0)
            {
                state = ExecuteCommand(state, serverCommands[0]);

                RpcUpdateState(state);

                serverCommands.RemoveAt(0);
            }
        }

        SyncState();
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(state.position, state.rotation, Vector3.one);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }

    [Command]
    private void CmdMove(Command command)
    {
        serverCommands.Add(command);
    }

    [ClientRpc]
    private void RpcUpdateState(State newState)
    {
        state = newState;

        if (clientCommands == null)
        {
            return;
        }

        int difference = predictedState.timestamp - state.timestamp;

        while (clientCommands.Count > difference)
        {
            clientCommands.RemoveAt(0);
        }

        UpdatePredictedState();
    }

    private Command CreateCommand()
    {
        var command = new Command();

        command.move += (sbyte)(Input.GetKey(KeyCode.W) ? 1 : 0);
        command.move += (sbyte)(Input.GetKey(KeyCode.S) ? -1 : 0);
        command.rotate += (sbyte)(Input.GetKey(KeyCode.D) ? 1 : 0);
        command.rotate += (sbyte)(Input.GetKey(KeyCode.A) ? -1 : 0);

        return command;
    }

    private State ExecuteCommand(State previousState, Command command)
    {
        Vector3 newPosition = previousState.position;
        Quaternion newRotation = previousState.rotation;

        if (command.move != 0)
        {
            newPosition = previousState.position + newRotation * Vector3.forward * command.move * fixedUpdateInterval * moveSpeed;
        }

        if (command.rotate != 0)
        {
            newRotation = previousState.rotation * Quaternion.Euler(Vector3.up * fixedUpdateInterval * rotateSpeed * command.rotate);
        }

        return new State
        {
            bufferedFrames = serverCommands.Count,
            timestamp = previousState.timestamp + 1,
            position = newPosition,
            rotation = newRotation
        };
    }

    private void SyncState()
    {
        if (isServer)
        {
            transform.position = state.position;
            transform.rotation = state.rotation;

            return;
        }

        State selectedState = isLocalPlayer ? predictedState : state;

        transform.position = Vector3.Lerp(transform.position, selectedState.position, lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, selectedState.rotation, lerpSpeed);
    }

    private void UpdatePredictedState()
    {
        predictedState = state;

        foreach (Command command in clientCommands)
        {
            predictedState = ExecuteCommand(predictedState, command);
        }
    }
}