using UnityEngine;

namespace JunctionLab
{
    [CreateAssetMenu(menuName = "Junction Lab/Traffic Settings")]
    public class TrafficSettings : ScriptableObject
    {
        [Header("Traffic")]
        [Range(0.05f, 0.65f)] public float arrivalsPerSecondPerApproach = 0.2f;
        [Range(3, 12)] public float speed = 7;
        [Range(1, 5)] public float acceleration = 2.5f;
        [Range(12, 100)] public int capacity = 64;
        public int seed = 42;
        [Header("Signals")]
        [Range(4, 20)] public float greenSeconds = 10;
        [Range(1, 4)] public float yellowSeconds = 2;
        [Range(1, 4)] public float clearanceSeconds = 1.5f;
        public bool adaptive = true;
    }
}
