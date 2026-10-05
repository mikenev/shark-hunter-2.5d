using UnityEngine;

namespace SharkHunter
{
    /// <summary>Placeholder HUD drawn with IMGUI (no font/UI assets needed): score, hunger bar, hints, game over.</summary>
    public class HudOverlay : MonoBehaviour
    {
        public GameSession session;
        public SharkBite bite;

        GUIStyle label, big;
        bool hasBitten;

        void OnEnable()
        {
            if (session == null) session = FindFirstObjectByType<GameSession>();
            if (bite == null) bite = FindFirstObjectByType<SharkBite>();
            if (bite != null) bite.Snapped += OnSnapped;
        }

        void OnDisable() { if (bite != null) bite.Snapped -= OnSnapped; }
        void OnSnapped() => hasBitten = true;

        void OnGUI()
        {
            if (session == null) return;
            float s = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s, h = Screen.height / s;

            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                label.normal.textColor = Color.white;
                big = new GUIStyle(label) { fontSize = 54, alignment = TextAnchor.MiddleCenter };
            }

            GUI.Label(new Rect(20, 14, 300, 30), "Score  " + session.Score, label);

            const float barW = 260f, barH = 18f;
            GUI.Label(new Rect(20, 44, 120, 26), "Hunger", label);
            DrawRect(new Rect(110, 50, barW, barH), new Color(0f, 0f, 0f, 0.5f));
            Color fill = session.Hunger < 0.25f ? Color.Lerp(Color.red, new Color(1f, 0.6f, 0.1f), Mathf.PingPong(Time.time * 3f, 1f)) : new Color(0.95f, 0.55f, 0.2f);
            DrawRect(new Rect(112, 52, (barW - 4f) * session.Hunger, barH - 4f), fill);

            if (!hasBitten && session.State == GameState.Playing)
                GUI.Label(new Rect(0, h - 60, w, 30), "WASD / arrows to swim    Space / click / A to bite", new GUIStyle(label) { alignment = TextAnchor.MiddleCenter });

            if (session.State == GameState.Starved)
            {
                DrawRect(new Rect(0, 0, w, h), new Color(0f, 0.05f, 0.1f, 0.55f));
                GUI.Label(new Rect(0, h * 0.35f, w, 70), "STARVED", big);
                GUI.Label(new Rect(0, h * 0.35f + 70, w, 40), "Score " + session.Score + "   -   press R to try again", new GUIStyle(label) { alignment = TextAnchor.MiddleCenter });
            }
        }

        static void DrawRect(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
