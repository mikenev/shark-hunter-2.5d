using UnityEngine;

namespace SharkHunter
{
    /// <summary>
    /// Perspective already gives natural parallax for layers at different Z depths. This adds an optional
    /// extra drift: followFactor 0 = fixed in the world, 1 = locked to the camera (infinitely far).
    /// Use it on far backdrops so they never run out of width.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float followFactorX = 0f;
        [Range(0f, 1f)] public float followFactorY = 0f;

        Transform cam;
        Vector3 origin, camOrigin;

        void Start()
        {
            var c = Camera.main;
            if (c == null) { enabled = false; return; }
            cam = c.transform;
            origin = transform.position;
            camOrigin = cam.position;
        }

        void LateUpdate()
        {
            Vector3 d = cam.position - camOrigin;
            transform.position = origin + new Vector3(d.x * followFactorX, d.y * followFactorY, 0f);
        }
    }
}
