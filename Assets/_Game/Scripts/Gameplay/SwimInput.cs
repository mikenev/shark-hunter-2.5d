using UnityEngine;
using UnityEngine.InputSystem;

namespace SharkHunter
{
    /// <summary>Keyboard (WASD / arrows) and gamepad stick swim input via the Input System.</summary>
    public class SwimInput : MonoBehaviour, ISwimInput
    {
        InputAction move;

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
        }

        void OnEnable() => move.Enable();
        void OnDisable() => move.Disable();
        void OnDestroy() => move?.Dispose();
    }
}
