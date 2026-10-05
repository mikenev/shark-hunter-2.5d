using UnityEngine;

namespace SharkHunter
{
    /// <summary>
    /// Perspective side-view camera with a slight downward tilt. Follows a target on X/Y with damping,
    /// looks ahead in the direction of travel, and stays inside the play area. Z distance is fixed.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SideViewCamera : MonoBehaviour
    {
        public Transform target;
        public PlayArea area;
        public float distance = 15f;
        public float pitchDegrees = 8f;
        public float fieldOfView = 35f;
        public float smoothTime = 0.3f;
        public float lookAhead = 2.5f;

        Camera cam;
        Vector3 focus, focusVel;
        Vector3 lastTargetPos;
        float lookAheadX, lookAheadVel;
        float shakeAmp, shakeTime, shakeDuration;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.fieldOfView = fieldOfView;
            if (area == null) area = FindFirstObjectByType<PlayArea>();
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (cam == null) cam = GetComponent<Camera>();
            cam.fieldOfView = fieldOfView;
            if (target == null) return;
            lastTargetPos = target.position;
            lookAheadX = 0f;
            focus = ClampFocus(target.position);
            Apply();
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float vx = (target.position.x - lastTargetPos.x) / dt;
            lastTargetPos = target.position;
            lookAheadX = Mathf.SmoothDamp(lookAheadX, Mathf.Clamp(vx, -1f, 1f) * lookAhead, ref lookAheadVel, 0.6f);

            Vector3 goal = ClampFocus(target.position + Vector3.right * lookAheadX);
            focus = Vector3.SmoothDamp(focus, goal, ref focusVel, smoothTime);
            Apply();
        }

        /// <summary>Screen-plane camera shake that decays over <paramref name="duration"/> seconds.</summary>
        public void Shake(float amplitude, float duration)
        {
            shakeAmp = Mathf.Max(shakeAmp * Mathf.Clamp01(shakeTime / Mathf.Max(shakeDuration, 0.001f)), amplitude);
            shakeDuration = duration;
            shakeTime = duration;
        }

        void Apply()
        {
            Quaternion rot = Quaternion.Euler(pitchDegrees, 0f, 0f);
            Vector3 pos = focus - rot * Vector3.forward * distance;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                Vector2 r = Random.insideUnitCircle * shakeAmp * Mathf.Clamp01(shakeTime / shakeDuration);
                pos += rot * new Vector3(r.x, r.y, 0f);
            }
            transform.SetPositionAndRotation(pos, rot);
        }

        Vector3 ClampFocus(Vector3 p)
        {
            float z = area != null ? area.PlaneZ : 0f;
            if (area == null) return new Vector3(p.x, p.y, z);

            float halfH = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
            float halfW = halfH * cam.aspect;
            return new Vector3(ClampAxis(p.x, area.min.x + halfW, area.max.x - halfW),
                               ClampAxis(p.y, area.min.y + halfH * 0.5f, area.max.y - halfH * 0.5f), z);
        }

        static float ClampAxis(float v, float lo, float hi) => lo > hi ? (lo + hi) * 0.5f : Mathf.Clamp(v, lo, hi);
    }
}
