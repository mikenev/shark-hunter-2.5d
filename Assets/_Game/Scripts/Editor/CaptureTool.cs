using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SharkHunter.EditorTools
{
    /// <summary>
    /// Headless play-mode screenshot of the Main scene, for visual/behaviour verification without the editor UI:
    /// Unity -batchmode -projectPath . -executeMethod SharkHunter.EditorTools.CaptureTool.Capture
    ///   -captureOut a.png [-sharkX n -sharkY n] [-moveX n -moveY n -simSeconds n]
    /// With -moveX/-moveY the shark is driven by scripted input for -simSeconds of game time before the shot.
    /// </summary>
    [InitializeOnLoad]
    public static class CaptureTool
    {
        const string PendingKey = "SharkHunter.CapturePending";
        static int frames;
        static float startTime;
        static PreyFish fleeProbe;

        class ScriptedInput : ISwimInput
        {
            public Vector2 Move { get; set; }
            public float BiteEvery = -1f;
            float next;
            public bool ConsumeBite()
            {
                if (BiteEvery < 0f || Time.time < next) return false;
                next = Time.time + BiteEvery;
                return true;
            }
        }

        static CaptureTool() => EditorApplication.playModeStateChanged += OnPlayMode;

        static string Arg(string name, string def)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : def;
        }

        public static void Capture()
        {
            SessionState.SetBool(PendingKey, true);
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            frames = 0;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            var shark = UnityEngine.Object.FindFirstObjectByType<SharkController>();
            if (++frames == 1)
            {
                shark.transform.position = new Vector3(float.Parse(Arg("-sharkX", "0")), float.Parse(Arg("-sharkY", "0")), 0f);
                shark.GetComponent<Rigidbody>().position = shark.transform.position;
                Physics.SyncTransforms();
                var sv = UnityEngine.Object.FindFirstObjectByType<SideViewCamera>();
                sv.GetComponent<Camera>().aspect = 16f / 9f;
                sv.SnapToTarget();
                if (Arg("-testPrey", "0") == "1")
                {
                    // Park the nearest prey just in front of the mouth and stop its AI so a bite must connect.
                    PreyFish best = null; float bd = float.MaxValue;
                    foreach (var pr in UnityEngine.Object.FindObjectsByType<PreyFish>(FindObjectsSortMode.None))
                    { float d = Vector2.Distance(pr.transform.position, shark.transform.position); if (d < bd) { bd = d; best = pr; } }
                    best.transform.position = shark.transform.position + new Vector3(float.Parse(Arg("-preyDx", "2")), 0f, 0f);
                    best.enabled = Arg("-preyFree", "0") == "1";
                    fleeProbe = best;
                    Debug.Log("TESTPREY parked " + best.name);
                }
                if (Arg("-moveX", null) != null || Arg("-moveY", null) != null || Arg("-biteEvery", null) != null)
                {
                    var input = new ScriptedInput { Move = new Vector2(float.Parse(Arg("-moveX", "0")), float.Parse(Arg("-moveY", "0"))), BiteEvery = float.Parse(Arg("-biteEvery", "-1")) };
                    typeof(SharkController).GetField("input", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shark, input);
                    typeof(SharkBite).GetField("input", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shark.GetComponent<SharkBite>(), input);
                }
                startTime = Time.time;
            }
            if (frames < 30 || Time.time - startTime < float.Parse(Arg("-simSeconds", "0.2"))) return;
            EditorApplication.update -= Tick;

            var cam = Camera.main;
            if (fleeProbe != null) Debug.Log($"FLEE dist={Vector2.Distance(fleeProbe.transform.position, shark.transform.position):F2}");
            var gs = UnityEngine.Object.FindFirstObjectByType<GameSession>();
            Debug.Log($"SESSION score={gs.Score} eaten={gs.PreyEaten} hunger={gs.Hunger:F2} state={gs.State} prey={UnityEngine.Object.FindObjectsByType<PreyFish>(FindObjectsSortMode.None).Length}");
            Debug.Log($"STATE shark={shark.transform.position} vel={shark.Velocity} facing={shark.Facing} cam={cam.transform.position} simTime={Time.time - startTime}");
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Arg("-captureOut", "capture.png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            Debug.Log("Captured " + Arg("-captureOut", "capture.png"));
            EditorApplication.Exit(0);
        }
    }
}
