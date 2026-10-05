using UnityEngine;

namespace SharkHunter
{
    /// <summary>
    /// Steer-and-thrust swimming on the 2D plane. The Rigidbody is locked to Z and never rotates;
    /// all rotation (facing flip, pitch) is the visual's job, driven through <see cref="ISharkVisual"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SharkController : MonoBehaviour
    {
        [SerializeField] SharkConfig config;
        [SerializeField] PlayArea area;

        Rigidbody rb;
        ISwimInput input;
        ISharkVisual visual;
        int facing = 1;
        float turnTimer;

        public int Facing => facing;
        public Vector2 Velocity => rb != null ? (Vector2)rb.linearVelocity : Vector2.zero;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            input = GetComponent<ISwimInput>();
            visual = GetComponentInChildren<ISharkVisual>();
            if (area == null) area = FindFirstObjectByType<PlayArea>();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 move = Vector2.ClampMagnitude(input != null ? input.Move : Vector2.zero, 1f);

            if (Mathf.Abs(move.x) > 0.2f && Mathf.Sign(move.x) != facing)
            {
                facing = (int)Mathf.Sign(move.x);
                turnTimer = config.turnTime;
            }
            turnTimer = Mathf.Max(0f, turnTimer - dt);

            bool thrusting = move.sqrMagnitude > 0.01f;
            float rate = thrusting ? config.acceleration : config.deceleration;
            if (turnTimer > 0f) rate *= config.turnAccelScale;

            Vector2 v = Vector2.MoveTowards((Vector2)rb.linearVelocity, move * config.maxSpeed, rate * dt);

            if (area != null)
            {
                Vector2 next = (Vector2)rb.position + v * dt;
                Vector2 clamped = area.Clamp(next);
                if (clamped.x != next.x) v.x = 0f;
                if (clamped.y != next.y) v.y = 0f;
            }

            rb.linearVelocity = new Vector3(v.x, v.y, 0f);
        }

        void Update()
        {
            if (visual == null) return;
            Vector2 v = Velocity;
            visual.UpdateState(new SwimState
            {
                Velocity = v,
                Speed01 = config.maxSpeed > 0f ? Mathf.Clamp01(v.magnitude / config.maxSpeed) : 0f,
                Facing = facing,
                TurnTime = config.turnTime,
            }, Time.deltaTime);
        }
    }
}
