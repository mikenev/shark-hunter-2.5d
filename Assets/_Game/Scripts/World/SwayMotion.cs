using UnityEngine;

namespace SharkHunter
{
    /// <summary>Cheap current-driven sway for kelp and similar props.</summary>
    public class SwayMotion : MonoBehaviour
    {
        public float degrees = 4f;
        public float frequency = 0.6f;
        float phase;
        Quaternion baseRot;

        void Start() { phase = Random.value * 6.28f; baseRot = transform.localRotation; }

        void Update()
        {
            transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * frequency + phase) * degrees);
        }
    }
}
