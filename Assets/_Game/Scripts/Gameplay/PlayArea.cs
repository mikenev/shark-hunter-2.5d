using UnityEngine;

namespace SharkHunter
{
    /// <summary>
    /// Defines the 2D gameplay plane: all gameplay lives at Z = <see cref="PlaneZ"/> and inside the X/Y rectangle.
    /// </summary>
    public class PlayArea : MonoBehaviour
    {
        public Vector2 min = new Vector2(-26f, -4.5f);
        public Vector2 max = new Vector2(26f, 7.5f);
        public float PlaneZ => transform.position.z;

        public Vector2 Clamp(Vector2 p) => new Vector2(Mathf.Clamp(p.x, min.x, max.x), Mathf.Clamp(p.y, min.y, max.y));

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            var c = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, PlaneZ);
            Gizmos.DrawWireCube(c, new Vector3(max.x - min.x, max.y - min.y, 0.1f));
        }
    }
}
