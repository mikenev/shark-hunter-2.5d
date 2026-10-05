using UnityEngine;

namespace SharkHunter
{
    public struct SwimState
    {
        public Vector2 Velocity;
        /// <summary>Speed as a fraction of max speed, 0..1.</summary>
        public float Speed01;
        /// <summary>+1 facing right, -1 facing left.</summary>
        public int Facing;
        public float TurnTime;
    }

    /// <summary>
    /// The only thing gameplay knows about the shark's art. Any model/prefab that implements this
    /// can be dropped under the shark root.
    /// </summary>
    public interface ISharkVisual
    {
        void UpdateState(in SwimState state, float deltaTime);
    }
}
