using UnityEngine;

namespace SharkHunter
{
    /// <summary>Tuning for one kind of prey. Data only.</summary>
    [CreateAssetMenu(menuName = "Shark Hunter/Prey Definition", fileName = "Prey")]
    public class PreyDefinition : ScriptableObject
    {
        public string displayName = "Fish";
        [Min(1)] public int health = 1;
        public float wanderSpeed = 1.6f;
        public float fleeSpeed = 5.5f;
        public float acceleration = 10f;
        [Tooltip("Starts fleeing when the shark is closer than this.")]
        public float detectRadius = 6f;
        public int points = 10;
        [Tooltip("Hunger restored when eaten (0..1).")]
        [Range(0f, 1f)] public float nutrition = 0.2f;
    }
}
