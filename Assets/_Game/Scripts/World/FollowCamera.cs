using UnityEngine;

namespace SharkHunter
{
    /// <summary>Keeps an effect (e.g. bubbles) centred on the camera horizontally, at a fixed vertical offset.</summary>
    [DefaultExecutionOrder(100)]
    public class FollowCamera : MonoBehaviour
    {
        public float yOffset = -8f;
        public float z = 0f;
        Transform cam;

        void LateUpdate()
        {
            if (cam == null) { var c = Camera.main; if (c == null) return; cam = c.transform; }
            transform.position = new Vector3(cam.position.x, cam.position.y + yOffset, z);
        }
    }
}
