using UnityEngine;
namespace SilentHeist
{
    [CreateAssetMenu(menuName="Silent Heist/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Range(1,10)] public float playerSpeed = 5f;
        [Range(1,8)] public float guardPatrolSpeed = 2f;
        [Range(1,8)] public float guardChaseSpeed = 2.8f;
        public float holeLifetime = 4f;
        public float guardTrappedTime = 2.5f;
        public float cameraExposureTime = 1.7f;
        public float detectionDrain = 0.7f;
        public float guardVision = 6f;
        public float searchTime = 2.5f;
    }
}
