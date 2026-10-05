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

        class ScriptedInput : ISwimInput { public Vector2 Move { get; set; } }

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
                if (Arg("-moveX", null) != null || Arg("-moveY", null) != null)
                {
                    var input = new ScriptedInput { Move = new Vector2(float.Parse(Arg("-moveX", "0")), float.Parse(Arg("-moveY", "0"))) };
                    typeof(SharkController).GetField("input", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shark, input);
                }
                startTime = Time.time;
            }
            if (frames < 30 || Time.time - startTime < float.Parse(Arg("-simSeconds", "0.2"))) return;
            EditorApplication.update -= Tick;

            var cam = Camera.main;
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
