using UnityEngine;

namespace Black.ClientSidePrediction
{
    public struct ClientInput
    {
        public ulong TimeFrame;

        public float Horizontal;
        public float Vertical;
        public Quaternion Rotation;
        public bool Jump;
    }

    public struct ServerResult
    {
        public ulong TimeFrame;
        public byte InputBuffers;

        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 GroundNormal;
        public Vector3 InnerGroundNormal;
        public Vector3 OuterGroundNormal;
        public Vector3 LastSavedMove;
        public bool LastMovementIterationFoundAnyGround;
        public bool MustUnground;
        public float MustUngroundTime;
        public bool FoundAnyGround;
        public bool IsStableOnGround;
        public bool SnapPrevented;
        public bool JumpRequested;
        public bool JumpConsumed;
        public bool JumpedThisFrame;
        public bool IsCrouching;
        public bool ShouldBeCrouching;
        public float TimeSinceJumpRequested;
        public float TimeSinceLastAbleToJump;
    }
}