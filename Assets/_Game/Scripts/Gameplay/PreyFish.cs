using System;
using UnityEngine;

namespace SharkHunter
{
    /// <summary>
    /// Prey on the gameplay plane: wanders, flees from the shark when it gets close, can be cornered at the
    /// edges of the play area, and dies after taking enough bites.
    /// </summary>
    public class PreyFish : MonoBehaviour, IBitable
    {
        public PreyDefinition definition;
        public GameObject deathEffect;

        /// <summary>Raised when any prey is killed.</summary>
        public static event Action<PreyFish> Eaten;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Eaten = null;

        Transform shark;
        PlayArea area;
        int health;
        Vector2 velocity, knockback, wanderDir;
        float wanderTimer, yaw, punch;
        bool fleeing;
        Vector3 baseScale;

        public Vector3 Position => transform.position;

        public void Initialize(Transform sharkTransform, PlayArea playArea)
        {
            shark = sharkTransform;
            area = playArea;
        }

        void Start()
        {
            if (area == null) area = FindFirstObjectByType<PlayArea>();
            if (shark == null) { var s = FindFirstObjectByType<SharkController>(); if (s != null) shark = s.transform; }
            health = definition.health;
            baseScale = transform.localScale;
            PickWanderDirection();
            yaw = wanderDir.x >= 0 ? 0f : 180f;
        }

        public bool TakeBite(int damage, Vector3 from)
        {
            health -= damage;
            Vector2 away = (Vector2)(transform.position - from);
            knockback += (away.sqrMagnitude > 0.001f ? away.normalized : Vector2.right) * 7f;
            punch = 1f;

            if (health > 0) return false;

            if (deathEffect != null)
            {
                var fx = Instantiate(deathEffect, transform.position, Quaternion.identity);
                Destroy(fx, 3f);
            }
            Eaten?.Invoke(this);
            Destroy(gameObject);
            return true;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector2 pos = transform.position;

            Vector2 desired = Steer(pos, dt);
            velocity = Vector2.MoveTowards(velocity, desired, definition.acceleration * dt);
            knockback = Vector2.Lerp(knockback, Vector2.zero, 1f - Mathf.Exp(-6f * dt));
            pos += (velocity + knockback) * dt;

            if (area != null)
            {
                Vector2 clamped = area.Clamp(pos);
                if (clamped.x != pos.x) velocity.x = 0f;
                if (clamped.y != pos.y) velocity.y = 0f;
                pos = clamped;
            }
            transform.position = new Vector3(pos.x, pos.y, area != null ? area.PlaneZ : 0f);

            Present(dt);
        }

        Vector2 Steer(Vector2 pos, float dt)
        {
            float dist = shark != null ? Vector2.Distance(pos, shark.position) : float.MaxValue;
            if (!fleeing && dist < definition.detectRadius) fleeing = true;
            else if (fleeing && dist > definition.detectRadius * 1.5f) { fleeing = false; PickWanderDirection(); }

            if (fleeing)
            {
                Vector2 away = (pos - (Vector2)shark.position).normalized;
                // Slide along walls instead of pressing into them, so prey can be cornered but not trapped on a line.
                if (area != null)
                {
                    const float margin = 1.5f;
                    if (pos.x < area.min.x + margin) away.x = Mathf.Max(away.x, 0.2f);
                    if (pos.x > area.max.x - margin) away.x = Mathf.Min(away.x, -0.2f);
                    if (pos.y < area.min.y + margin) away.y = Mathf.Max(away.y, 0.2f);
                    if (pos.y > area.max.y - margin) away.y = Mathf.Min(away.y, -0.2f);
                }
                // A little juking.
                float jitter = Mathf.Sin(Time.time * 7f + GetInstanceID()) * 0.35f;
                away = (away + new Vector2(-away.y, away.x) * jitter).normalized;
                return away * definition.fleeSpeed;
            }

            wanderTimer -= dt;
            if (wanderTimer <= 0f) PickWanderDirection();
            if (area != null)
            {
                if (pos.x < area.min.x + 1f) wanderDir.x = Mathf.Abs(wanderDir.x);
                if (pos.x > area.max.x - 1f) wanderDir.x = -Mathf.Abs(wanderDir.x);
                if (pos.y < area.min.y + 0.8f) wanderDir.y = Mathf.Abs(wanderDir.y);
                if (pos.y > area.max.y - 0.8f) wanderDir.y = -Mathf.Abs(wanderDir.y);
            }
            return wanderDir.normalized * definition.wanderSpeed;
        }

        void PickWanderDirection()
        {
            wanderTimer = UnityEngine.Random.Range(2f, 4.5f);
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            wanderDir = new Vector2(side, UnityEngine.Random.Range(-0.35f, 0.35f));
        }

        void Present(float dt)
        {
            if (Mathf.Abs(velocity.x) > 0.3f)
                yaw = Mathf.MoveTowardsAngle(yaw, velocity.x >= 0f ? 0f : 180f, 540f * dt);
            float pitch = Mathf.Clamp(Mathf.Atan2(velocity.y, Mathf.Max(Mathf.Abs(velocity.x), 0.5f)) * Mathf.Rad2Deg, -40f, 40f);
            transform.rotation = Quaternion.Euler(0f, yaw, pitch);

            punch = Mathf.MoveTowards(punch, 0f, 4f * dt);
            transform.localScale = baseScale * (1f + 0.3f * punch);
        }
    }
}
