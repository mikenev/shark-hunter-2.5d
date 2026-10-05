using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SharkHunter
{
    public enum GameState { Playing, Starved }

    /// <summary>Score, hunger and the start/restart loop. Eating prey restores hunger; at zero the shark starves.</summary>
    public class GameSession : MonoBehaviour
    {
        public SharkController shark;
        [Range(0f, 1f)] public float startHunger = 0.8f;
        public float hungerDrainPerSecond = 0.02f;

        public int Score { get; private set; }
        public int PreyEaten { get; private set; }
        public float Hunger { get; private set; }
        public GameState State { get; private set; } = GameState.Playing;

        InputAction restart;

        void Awake()
        {
            Hunger = startHunger;
            if (shark == null) shark = FindFirstObjectByType<SharkController>();
            restart = new InputAction("Restart", InputActionType.Button);
            restart.AddBinding("<Keyboard>/r");
            restart.AddBinding("<Keyboard>/enter");
            restart.AddBinding("<Gamepad>/start");
        }

        void OnEnable() { PreyFish.Eaten += OnEaten; restart.Enable(); }
        void OnDisable() { PreyFish.Eaten -= OnEaten; restart.Disable(); }
        void OnDestroy() => restart?.Dispose();

        void OnEaten(PreyFish prey)
        {
            if (State != GameState.Playing) return;
            Score += prey.definition.points;
            PreyEaten++;
            Hunger = Mathf.Clamp01(Hunger + prey.definition.nutrition);
        }

        void Update()
        {
            if (State == GameState.Playing)
            {
                Hunger = Mathf.Max(0f, Hunger - hungerDrainPerSecond * Time.deltaTime);
                if (Hunger <= 0f)
                {
                    State = GameState.Starved;
                    if (shark != null) shark.Frozen = true;
                }
            }
            else if (restart.WasPressedThisFrame())
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
