using UnityEngine;

namespace SharkHunter
{
    /// <summary>
    /// Presentation for the shark: facing flip, pitch toward travel direction, and tail wag.
    /// Lives on a child of the gameplay root. To swap models, replace this child with any prefab that
    /// implements <see cref="ISharkVisual"/>.
    /// </summary>
    public class SharkVisual : MonoBehaviour, ISharkVisual
    {
        [SerializeField] Transform tail;
        [SerializeField] float maxPitchDegrees = 35f;
        [SerializeField] float pitchSmoothing = 8f;
        [SerializeField] float wagAmplitude = 22f;
        [SerializeField] Vector2 wagFrequency = new Vector2(2.5f, 9f);

        float yaw, pitch, wagPhase;

        public void UpdateState(in SwimState s, float dt)
        {
            float targetYaw = s.Facing >= 0 ? 0f : 180f;
            float turnRate = 180f / Mathf.Max(s.TurnTime, 0.01f);
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, turnRate * dt);

            float targetPitch = Mathf.Clamp(s.Velocity.y / 6f, -1f, 1f) * maxPitchDegrees * Mathf.Clamp01(s.Speed01 * 2f);
            pitch = Mathf.Lerp(pitch, targetPitch, 1f - Mathf.Exp(-pitchSmoothing * dt));
            transform.localRotation = Quaternion.Euler(0f, yaw, pitch);

            if (tail != null)
            {
                wagPhase += dt * Mathf.Lerp(wagFrequency.x, wagFrequency.y, s.Speed01);
                float amp = wagAmplitude * Mathf.Lerp(0.6f, 1f, s.Speed01);
                tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(wagPhase) * amp, 0f);
            }
        }
    }
}
