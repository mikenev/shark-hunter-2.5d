using System;
using System.Collections.Generic;
using UnityEngine;

namespace SharkHunter
{
    /// <summary>
    /// Bite attack: on press, the shark lunges forward and its mouth can connect with <see cref="IBitable"/>s
    /// for a short window. Each target is hit at most once per bite.
    /// </summary>
    [RequireComponent(typeof(SharkController))]
    public class SharkBite : MonoBehaviour
    {
        [SerializeField] SharkConfig config;

        static readonly Collider[] Buffer = new Collider[24];

        SharkController controller;
        ISwimInput input;
        ISharkVisual visual;
        readonly HashSet<IBitable> hitThisBite = new HashSet<IBitable>();
        float cooldown, windowLeft;

        /// <summary>Raised when a bite connects. The bool is true if it killed the target.</summary>
        public event Action<IBitable, bool> Bitten;
        /// <summary>Raised when a bite starts.</summary>
        public event Action Snapped;

        public Vector3 MouthPosition => transform.position + new Vector3(controller.Facing * config.mouthOffset, 0f, 0f);

        void Awake()
        {
            controller = GetComponent<SharkController>();
            input = GetComponent<ISwimInput>();
            visual = GetComponentInChildren<ISharkVisual>();
        }

        void Update()
        {
            cooldown -= Time.deltaTime;
            if (controller.Frozen || cooldown > 0f || input == null || !input.ConsumeBite()) return;
            StartBite();
        }

        void StartBite()
        {
            cooldown = config.biteCooldown;
            windowLeft = config.biteWindow;
            hitThisBite.Clear();

            float pitch = Mathf.Clamp(input.Move.y, -1f, 1f) * 0.6f;
            controller.Lunge(new Vector2(controller.Facing, pitch), config.lungeSpeed, config.lungeTime);
            visual?.PlayBite(config.biteWindow);
            Snapped?.Invoke();
        }

        void FixedUpdate()
        {
            if (windowLeft <= 0f) return;
            windowLeft -= Time.fixedDeltaTime;

            Vector3 mouth = MouthPosition;
            int n = Physics.OverlapSphereNonAlloc(mouth, config.biteRadius, Buffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var target = Buffer[i].GetComponentInParent<IBitable>();
                if (target == null || !hitThisBite.Add(target)) continue;
                bool killed = target.TakeBite(config.biteDamage, mouth);
                Bitten?.Invoke(target, killed);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = Color.red;
            int f = controller != null ? controller.Facing : 1;
            Gizmos.DrawWireSphere(transform.position + new Vector3(f * config.mouthOffset, 0f, 0f), config.biteRadius);
        }
    }
}
