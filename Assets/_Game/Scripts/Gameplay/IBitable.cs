using UnityEngine;

namespace SharkHunter
{
    /// <summary>Anything the shark's mouth can hit.</summary>
    public interface IBitable
    {
        Vector3 Position { get; }

        /// <summary>Apply a bite. Returns true if this bite killed it.</summary>
        bool TakeBite(int damage, Vector3 from);
    }
}
