using UnityEngine;

namespace SharkHunter
{
    /// <summary>Ambient fish: patrols back and forth along X with a gentle bob. Placeholder for future prey AI.</summary>
    public class SimpleSwimmer : MonoBehaviour
    {
        public float speed = 1.5f;
        public float minX = -10f, maxX = 10f;
        public float bobAmplitude = 0.3f, bobFrequency = 1.2f;
        public float turnSpeed = 360f;

        float dir = 1f, baseY, phase, yaw;

        void Start()
        {
            baseY = transform.position.y;
            phase = Random.value * 6.28f;
            dir = Random.value < 0.5f ? -1f : 1f;
            yaw = dir > 0 ? 0f : 180f;
        }

        void Update()
        {
            var p = transform.position;
            p.x += dir * speed * Time.deltaTime;
            if (p.x > maxX) dir = -1f; else if (p.x < minX) dir = 1f;
            p.y = baseY + Mathf.Sin(Time.time * bobFrequency + phase) * bobAmplitude;
            transform.position = p;
            yaw = Mathf.MoveTowardsAngle(yaw, dir > 0 ? 0f : 180f, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, yaw, Mathf.Cos(Time.time * bobFrequency + phase) * 8f);
        }
    }
}
