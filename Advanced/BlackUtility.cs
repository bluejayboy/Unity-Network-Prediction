using UnityEngine;

namespace Black.Utility
{
    public static class BlackUtility
    {
        public static void ChangePhysicsFrameRate(float framesPerSecond)
        {
            Time.fixedDeltaTime = 1.0f / framesPerSecond;
        }
    }
}