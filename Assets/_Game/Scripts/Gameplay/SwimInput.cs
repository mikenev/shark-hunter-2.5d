using UnityEngine;
using UnityEngine.InputSystem;

namespace SharkHunter
{
    /// <summary>Keyboard (WASD / arrows) and gamepad stick swim input via the Input System.</summary>
    public class SwimInput : MonoBehaviour, ISwimInput
    {
        const float BiteBufferSeconds = 0.15f;

        InputAction move, bite;
        float biteBufferedUntil = -1f;

        public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick", processors: "stickDeadzone");

            bite = new InputAction("Bite", InputActionType.Button);
            bite.AddBinding("<Keyboard>/space");
            bite.AddBinding("<Mouse>/leftButton");
            bite.AddBinding("<Gamepad>/buttonSouth");
        }

        void OnEnable() { move.Enable(); bite.Enable(); }
        void OnDisable() { move.Disable(); bite.Disable(); }
        void OnDestroy() { move?.Dispose(); bite?.Dispose(); }

        void Update()
        {
            if (bite.WasPressedThisFrame()) biteBufferedUntil = Time.time + BiteBufferSeconds;
        }

        public bool ConsumeBite()
        {
            if (Time.time > biteBufferedUntil) return false;
            biteBufferedUntil = -1f;
            return true;
        }
    }
}
