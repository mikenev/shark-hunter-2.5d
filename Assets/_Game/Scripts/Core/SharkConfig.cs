using UnityEngine;

namespace SharkHunter
{
    /// <summary>Movement tuning for the player shark. Pure data; no art references.</summary>
    [CreateAssetMenu(menuName = "Shark Hunter/Shark Config", fileName = "SharkConfig")]
    public class SharkConfig : ScriptableObject
    {
        [Header("Swimming")]
        public float maxSpeed = 7f;
        public float acceleration = 14f;
        public float deceleration = 6f;

        [Header("Turning")]
        [Tooltip("Seconds to flip facing when reversing horizontally.")]
        public float turnTime = 0.35f;
        [Tooltip("Acceleration multiplier while the flip is in progress (the shark can't thrust at full power mid-turn).")]
        [Range(0.05f, 1f)] public float turnAccelScale = 0.35f;

        [Header("Bite")]
        public float biteCooldown = 0.55f;
        public int biteDamage = 1;
        [Tooltip("Speed of the forward lunge during a bite.")]
        public float lungeSpeed = 13f;
        public float lungeTime = 0.18f;
        [Tooltip("How long after starting a bite the mouth can connect.")]
        public float biteWindow = 0.26f;
        public float biteRadius = 0.8f;
        [Tooltip("Mouth position in front of the shark's centre.")]
        public float mouthOffset = 1.5f;
    }
}
