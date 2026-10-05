using UnityEngine;

namespace SharkHunter
{
    /// <summary>Game feel for bites: camera shake on a hit (bigger on a kill).</summary>
    public class BiteFeedback : MonoBehaviour
    {
        public SharkBite bite;
        public SideViewCamera cam;

        void OnEnable()
        {
            if (bite == null) bite = FindFirstObjectByType<SharkBite>();
            if (cam == null) cam = GetComponent<SideViewCamera>();
            if (bite != null) bite.Bitten += OnBitten;
        }

        void OnDisable() { if (bite != null) bite.Bitten -= OnBitten; }

        void OnBitten(IBitable target, bool killed) => cam.Shake(killed ? 0.22f : 0.12f, killed ? 0.25f : 0.15f);
    }
}
