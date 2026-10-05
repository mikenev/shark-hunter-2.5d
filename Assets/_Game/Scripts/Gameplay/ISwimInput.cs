using UnityEngine;

namespace SharkHunter
{
    /// <summary>Source of swim intent. Swap for AI, replays, touch controls, etc.</summary>
    public interface ISwimInput
    {
        /// <summary>Desired swim direction, magnitude 0..1.</summary>
        Vector2 Move { get; }
    }
}
