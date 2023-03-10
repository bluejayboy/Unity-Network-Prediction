using UnityEngine;

namespace BlackPrediction
{
    public struct ClientInput
    {
        public ulong Frame;

        // Add your inputs here.
        public float Horizontal;
        public float Vertical;
        public float Yaw;
        public bool Jump;
    }

    public struct ServerResult
    {
        public ulong Frame;
        public byte Buffer;

        // Add your results here.
        public Vector3 Position;
        public Vector3 Velocity;
        public bool IsGrounded;
    }
}