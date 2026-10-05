using UnityEngine;
using UnityEngine.Rendering;

namespace SharkHunter
{
    /// <summary>
    /// The underwater look: linear fog, ambient tint and background colour, blended from a shallow to a deep
    /// colour by camera height.
    /// </summary>
    [ExecuteAlways]
    public class WaterEnvironment : MonoBehaviour
    {
        public Color shallowColor = new Color(0.07f, 0.42f, 0.62f);
        public Color deepColor = new Color(0.015f, 0.12f, 0.26f);
        public Color ambientColor = new Color(0.22f, 0.36f, 0.50f);
        public float fogStart = 8f;
        public float fogEnd = 140f;
        public float surfaceY = 9f;
        public float floorY = -6f;
        public Camera targetCamera;

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void LateUpdate() => Apply();

        public void Apply()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            float y = cam != null ? cam.transform.position.y : 0f;
            float t = Mathf.InverseLerp(surfaceY, floorY, y);
            Color water = Color.Lerp(shallowColor, deepColor, t);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
            RenderSettings.fogColor = water;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(ambientColor, water, 0.35f);
            RenderSettings.skybox = null;

            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = water;
            }
        }
    }
}
