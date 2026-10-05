using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SharkHunter.EditorTools
{
    /// <summary>
    /// Generates the placeholder vertical slice (meshes, materials, prefabs, scene, URP/WebGL settings) from code.
    /// Run via Tools/Shark Hunter/Build Placeholder Slice, or headless with -executeMethod.
    /// It regenerates everything under Assets/_Game/Art/Generated, Prefabs/Placeholder and Scenes/Main.unity.
    /// Real art should go under Assets/_Game/Art (and be tracked in ASSETS.md), not in the Generated folder.
    /// </summary>
    public static class SliceBuilder
    {
        const string Root = "Assets/_Game";
        const string GenDir = Root + "/Art/Generated";
        const string PrefabDir = Root + "/Prefabs/Placeholder";
        const string SettingsDir = Root + "/Settings";
        const string ScenePath = Root + "/Scenes/Main.unity";

        const float FloorY = -6f;

        [MenuItem("Tools/Shark Hunter/Build Placeholder Slice")]
        public static void BuildAll()
        {
            foreach (var d in new[] { GenDir + "/Meshes", GenDir + "/Materials", GenDir + "/Textures", PrefabDir, SettingsDir, Root + "/Scenes" })
                Directory.CreateDirectory(d);
            AssetDatabase.DeleteAsset(PrefabDir);
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();

            var mats = MakeMaterials();
            var meshes = MakeMeshes();
            var config = LoadOrCreate<SharkConfig>(SettingsDir + "/SharkConfig.asset");
            var defs = MakePreyDefinitions();
            var burst = MakeBurstEffect(MakeBubbleMaterial());
            var prefabs = MakePrefabs(meshes, mats, config, defs, burst);
            var profile = MakeVolumeProfile();
            var bubbleMat = AssetDatabase.LoadAssetAtPath<Material>(GenDir + "/Materials/Bubble.mat");

            BuildScene(prefabs, meshes, mats, config, profile, bubbleMat);
            ConfigureProject();
            AssetDatabase.SaveAssets();
            Debug.Log("Shark Hunter: placeholder slice built.");
        }

        // ---------- materials ----------

        class Mats { public Material shark, sharkBelly, dark, fishA, fishAB, fishC, fishCB, fishD, fishDB, rock, rockDark, kelp, kelpDark, sand, ridge; }

        static Mats MakeMaterials() => new Mats
        {
            shark = Mat("Shark_Back", "#5F7F98"), sharkBelly = Mat("Shark_Belly", "#E4EDF1"), dark = Mat("Dark", "#1E2630"),
            fishA = Mat("FishA_Back", "#F2872B"), fishAB = Mat("FishA_Belly", "#FFD9A0"),
            fishC = Mat("FishC_Back", "#D9487F"), fishCB = Mat("FishC_Belly", "#FFC2D6"),
            fishD = Mat("FishD_Back", "#3F7F96"), fishDB = Mat("FishD_Belly", "#8DBBC0"),
            rock = Mat("Rock", "#4F6068"), rockDark = Mat("Rock_Dark", "#1B2D36"),
            kelp = Mat("Kelp", "#3E9A5C"), kelpDark = Mat("Kelp_Dark", "#14392E"),
            sand = Mat("Sand", "#A89A70"), ridge = Mat("Ridge", "#2E5870"),
        };

        static Material Mat(string name, string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            string path = GenDir + "/Materials/" + name + ".mat";
            AssetDatabase.DeleteAsset(path);
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetColor("_SpecColor", new Color(0.05f, 0.05f, 0.05f));
            m.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material MakeBubbleMaterial()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float rim = Mathf.SmoothStep(0.65f, 0.92f, r) * (1f - Mathf.SmoothStep(0.92f, 1f, r));
                float fill = (1f - Mathf.SmoothStep(0.85f, 1f, r)) * 0.12f;
                float glint = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(dx, dy), new Vector2(-0.35f, 0.4f)) * 5f);
                float a = Mathf.Clamp01(rim * 0.8f + fill + glint * 0.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            string texPath = GenDir + "/Textures/Bubble.png";
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            string path = GenDir + "/Materials/Bubble.mat";
            AssetDatabase.DeleteAsset(path);
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetTexture("_BaseMap", t);
            m.SetColor("_BaseColor", new Color(0.85f, 0.97f, 1f, 1f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ---------- meshes ----------

        class Meshes { public Mesh sharkBody, sharkTail, sharkJaw, fish, rockA, rockB, kelpA, kelpB, seabed; public Mesh[] ridges; }

        static Mesh Save(Mesh m)
        {
            string path = GenDir + "/Meshes/" + m.name + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Meshes MakeMeshes() => new Meshes
        {
            sharkBody = Save(ShapeFactory.SharkBody()), sharkTail = Save(ShapeFactory.SharkTail()), sharkJaw = Save(ShapeFactory.SharkJaw()), fish = Save(ShapeFactory.Fish()),
            rockA = Save(ShapeFactory.Rock(1)), rockB = Save(ShapeFactory.Rock(2)),
            kelpA = Save(ShapeFactory.Kelp(1)), kelpB = Save(ShapeFactory.Kelp(2)),
            seabed = Save(ShapeFactory.Seabed()),
            ridges = new[]
            {
                Save(ShapeFactory.Ridge(1, 150f, -4f, 7f)),
                Save(ShapeFactory.Ridge(2, 190f, -2f, 11f)),
                Save(ShapeFactory.Ridge(3, 240f, 0f, 16f)),
            },
        };

        // ---------- prefabs ----------

        class Prefabs { public GameObject shark, fishAmbient, rockA, rockB, kelpA, kelpB; public PreyFish preySmall, preyBig; }

        static GameObject MeshObject(string name, Mesh mesh, params Material[] mats)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            var p = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return p;
        }

        static Prefabs MakePrefabs(Meshes m, Mats mat, SharkConfig config, PreyDefs defs, GameObject burst)
        {
            var p = new Prefabs();

            // Shark: gameplay root (physics + input + controller) with a swappable Visual child.
            var shark = new GameObject("Shark");
            var rb = shark.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var col = shark.AddComponent<CapsuleCollider>();
            col.direction = 0; col.radius = 0.35f; col.height = 2.8f; col.center = new Vector3(0.2f, 0f, 0f);
            shark.AddComponent<SwimInput>();
            var controller = shark.AddComponent<SharkController>();
            var biteComp = shark.AddComponent<SharkBite>();
            new SerializedObject(biteComp).Also(bo => { bo.FindProperty("config").objectReferenceValue = config; bo.ApplyModifiedProperties(); });
            new SerializedObject(controller).Also(co => { co.FindProperty("config").objectReferenceValue = config; co.ApplyModifiedProperties(); });
            var visual = new GameObject("Visual");
            visual.transform.SetParent(shark.transform, false);
            var body = MeshObject("Body", m.sharkBody, mat.shark, mat.sharkBelly, mat.dark);
            body.transform.SetParent(visual.transform, false);
            var tail = MeshObject("Tail", m.sharkTail, mat.shark);
            tail.transform.SetParent(visual.transform, false);
            tail.transform.localPosition = new Vector3(ShapeFactory.SharkTailJointX, 0f, 0f);
            var jaw = MeshObject("Jaw", m.sharkJaw, mat.sharkBelly);
            jaw.transform.SetParent(visual.transform, false);
            jaw.transform.localPosition = new Vector3(ShapeFactory.JawHingeX, ShapeFactory.JawHingeY, 0f);
            var sv = visual.AddComponent<SharkVisual>();
            new SerializedObject(sv).Also(so =>
            {
                so.FindProperty("tail").objectReferenceValue = tail.transform;
                so.FindProperty("jaw").objectReferenceValue = jaw.transform;
                so.ApplyModifiedProperties();
            });
            p.shark = SavePrefab(shark, "Shark");

            p.fishAmbient = SavePrefab(MeshObject("FishAmbient", m.fish, mat.fishD, mat.fishDB, mat.dark), "FishAmbient");
            p.preySmall = SavePrey("PreyFishSmall", m.fish, defs.small, burst, 1f, mat.fishA, mat.fishAB, mat.dark);
            p.preyBig = SavePrey("PreyFishBig", m.fish, defs.big, burst, 2.3f, mat.fishC, mat.fishCB, mat.dark);
            p.rockA = SavePrefab(MeshObject("RockA", m.rockA, mat.rock), "RockA");
            p.rockB = SavePrefab(MeshObject("RockB", m.rockB, mat.rock), "RockB");
            p.kelpA = SavePrefab(Kelp("KelpA", m.kelpA, mat.kelp), "KelpA");
            p.kelpB = SavePrefab(Kelp("KelpB", m.kelpB, mat.kelp), "KelpB");
            return p;
        }

        static PreyFish SavePrey(string name, Mesh mesh, PreyDefinition def, GameObject burst, float scale, params Material[] mats)
        {
            var go = MeshObject(name, mesh, mats);
            go.transform.localScale = Vector3.one * scale;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 0.4f; col.center = new Vector3(0.05f, 0f, 0f);
            var prey = go.AddComponent<PreyFish>();
            prey.definition = def;
            prey.deathEffect = burst;
            return SavePrefab(go, name).GetComponent<PreyFish>();
        }

        class PreyDefs { public PreyDefinition small, big; }

        static PreyDefs MakePreyDefinitions() => new PreyDefs
        {
            small = CreateIfMissing<PreyDefinition>(SettingsDir + "/Prey_Small.asset", d =>
            { d.displayName = "Small fish"; d.health = 1; d.wanderSpeed = 1.6f; d.fleeSpeed = 5.5f; d.detectRadius = 6f; d.points = 10; d.nutrition = 0.2f; }),
            big = CreateIfMissing<PreyDefinition>(SettingsDir + "/Prey_Big.asset", d =>
            { d.displayName = "Big fish"; d.health = 2; d.wanderSpeed = 1.2f; d.fleeSpeed = 4.2f; d.detectRadius = 5f; d.points = 35; d.nutrition = 0.45f; }),
        };

        static T CreateIfMissing<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            init(a);
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static GameObject MakeBurstEffect(Material bubbleMat)
        {
            var go = new GameObject("BiteBurst");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.35f, 0.3f, 0.9f), new Color(1f, 0.7f, 0.5f, 0.9f));
            main.gravityModifier = -0.15f;
            main.maxParticles = 40;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = bubbleMat;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/BiteBurst.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject Kelp(string name, Mesh mesh, Material mat)
        {
            var go = MeshObject(name, mesh, mat);
            go.AddComponent<SwayMotion>();
            return go;
        }

        // ---------- volume ----------

        static VolumeProfile MakeVolumeProfile()
        {
            string path = SettingsDir + "/WaterVolumeProfile.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0f);
            ca.contrast.Override(8f);
            ca.saturation.Override(12f);
            ca.colorFilter.Override(new Color(0.9f, 0.97f, 1f));
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.28f);
            vig.smoothness.Override(0.5f);
            AssetDatabase.AddObjectToAsset(ca, profile);
            AssetDatabase.AddObjectToAsset(vig, profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        // ---------- scene ----------

        static void BuildScene(Prefabs pf, Meshes m, Mats mat, SharkConfig config, VolumeProfile profile, Material bubbleMat)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rnd = new System.Random(7);

            var area = new GameObject("PlayArea").AddComponent<PlayArea>();

            // Shark
            var shark = (GameObject)PrefabUtility.InstantiatePrefab(pf.shark);
            shark.transform.position = new Vector3(0f, 0f, 0f);
            var controller = shark.GetComponent<SharkController>();
            var so = new SerializedObject(controller);
            so.FindProperty("area").objectReferenceValue = area;
            so.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);

            // Camera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 200f; cam.allowHDR = false;
            camGo.AddComponent<AudioListener>();
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var sv = camGo.AddComponent<SideViewCamera>();
            sv.target = shark.transform; sv.area = area;
            sv.SnapToTarget();

            // Water look
            var env = new GameObject("WaterEnvironment").AddComponent<WaterEnvironment>();
            env.targetCamera = cam;
            env.Apply();
            var volGo = new GameObject("Post Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true; vol.sharedProfile = profile;

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional; light.shadows = LightShadows.None;
            light.color = new Color(0.85f, 0.96f, 1f); light.intensity = 0.95f;
            lightGo.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

            // Bubbles
            MakeBubbles(bubbleMat);

            // Static world
            var world = new GameObject("World").transform;
            var seabed = MeshObject("Seabed", m.seabed, mat.sand);
            seabed.transform.SetParent(world, false);

            var back = Group("Layer_Background", world);
            for (int i = 0; i < m.ridges.Length; i++)
            {
                var ridge = MeshObject("Ridge_" + i, m.ridges[i], mat.ridge);
                ridge.transform.SetParent(back, false);
                ridge.transform.position = new Vector3(0f, 0f, new[] { 28f, 46f, 68f }[i]);
                var pl = ridge.AddComponent<ParallaxLayer>();
                pl.followFactorX = new[] { 0.1f, 0.2f, 0.3f }[i];
            }

            var mid = Group("Layer_Midground", world);
            for (float x = -50f; x < 50f; x += 3.5f + (float)rnd.NextDouble() * 4f)
            {
                float z = 4f + (float)rnd.NextDouble() * 10f;
                bool rock = rnd.NextDouble() < 0.4;
                var prefab = rock ? (rnd.Next(2) == 0 ? pf.rockA : pf.rockB) : (rnd.Next(2) == 0 ? pf.kelpA : pf.kelpB);
                var go = Place(prefab, mid, x, z);
                float s = rock ? 1.4f + (float)rnd.NextDouble() * 2.2f : 0.8f + (float)rnd.NextDouble() * 0.8f;
                go.transform.localScale = new Vector3(s, s, s);
                go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                if (rock) go.transform.position += Vector3.up * 0.15f * s;
            }

            var near = Group("Layer_Foreground", world);
            for (float x = -48f; x < 48f; x += 9f + (float)rnd.NextDouble() * 8f)
            {
                float z = -5f - (float)rnd.NextDouble() * 2f;
                bool rock = rnd.NextDouble() < 0.35;
                var go = Place(rock ? pf.rockA : pf.kelpA, near, x, z);
                float s = rock ? 1.6f + (float)rnd.NextDouble() : 1.3f + (float)rnd.NextDouble() * 0.4f;
                go.transform.localScale = new Vector3(s, s, s);
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = rock ? mat.rockDark : mat.kelpDark;
                if (rock) go.transform.position += Vector3.up * 0.2f * s;
            }

            // Ambient fish
            var life = Group("Ambient Life", world);
            for (int i = 0; i < 12; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf.fishAmbient);
                go.transform.SetParent(life, false);
                float cx = -24f + (float)rnd.NextDouble() * 48f;
                go.transform.position = new Vector3(cx, -3.5f + (float)rnd.NextDouble() * 9f, 3f + (float)rnd.NextDouble() * 7f);
                float sc = 0.9f + (float)rnd.NextDouble() * 0.9f;
                go.transform.localScale = Vector3.one * sc;
                var sw = go.AddComponent<SimpleSwimmer>();
                sw.speed = 0.8f + (float)rnd.NextDouble() * 1.4f;
                sw.minX = cx - 6f - (float)rnd.NextDouble() * 6f;
                sw.maxX = cx + 6f + (float)rnd.NextDouble() * 6f;
                sw.bobAmplitude = 0.15f + (float)rnd.NextDouble() * 0.3f;
            }

            // Gameplay: session/HUD, prey spawner, bite feedback.
            var session = new GameObject("GameSession").AddComponent<GameSession>();
            session.shark = shark.GetComponent<SharkController>();
            var hud = session.gameObject.AddComponent<HudOverlay>();
            hud.session = session; hud.bite = shark.GetComponent<SharkBite>();
            var fb = camGo.AddComponent<BiteFeedback>();
            fb.bite = shark.GetComponent<SharkBite>(); fb.cam = sv;
            var spawner = new GameObject("PreySpawner").AddComponent<PreySpawner>();
            spawner.shark = shark.transform; spawner.area = area;
            spawner.entries = new[]
            {
                new PreySpawner.Entry { prefab = pf.preySmall, targetCount = 8 },
                new PreySpawner.Entry { prefab = pf.preyBig, targetCount = 3 },
            };

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static Transform Group(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static GameObject Place(GameObject prefab, Transform parent, float x, float z)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, ShapeFactory.SeabedHeight(x, z) - 0.05f, z);
            return go;
        }

        static void MakeBubbles(Material bubbleMat)
        {
            var go = new GameObject("Bubbles");
            go.AddComponent<FollowCamera>().yOffset = -9f;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.gravityModifier = 0f;

            var emission = ps.emission; emission.rateOverTime = 14f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 1f, 16f);
            shape.rotation = new Vector3(-90f, 0f, 0f);
            shape.position = new Vector3(0f, 0f, 2f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.y = new ParticleSystem.MinMaxCurve(0.9f);
            vel.x = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0f);

            var noise = ps.noise;
            noise.enabled = true; noise.strength = 0.35f; noise.frequency = 0.4f; noise.quality = ParticleSystemNoiseQuality.Low;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.1f), new GradientAlphaKey(0.9f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = bubbleMat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
        }

        // ---------- project settings ----------

        static void ConfigureProject()
        {
            PlayerSettings.productName = "Shark Hunter 2.5D";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // WebGL: small build, and safe on hosts (GitHub Pages) that can't send Content-Encoding for .br/.gz.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;

            // Forward rendering everywhere: required for WebGL, cheaper, and keeps fog/transparency simple. SSAO off.
            foreach (var path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
            {
                var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (rd == null) continue;
                rd.renderingMode = RenderingMode.Forward;
                foreach (var f in rd.rendererFeatures) f.SetActive(false);
                EditorUtility.SetDirty(rd);
            }

            // SRP Batcher off: in headless (-batchmode) Metal captures it made every Lit material draw with one shared colour.
            // This scene is a few hundred cheap draws, so the cost is negligible. Re-test windowed before re-enabling.
            foreach (var path in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (asset == null) continue;
                asset.useSRPBatcher = false;
                EditorUtility.SetDirty(asset);
            }

            // The template maps WebGL to the "Mobile" quality level. Make that URP asset WebGL-lean.
            var mobile = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (mobile != null)
            {
                mobile.supportsHDR = false;
                mobile.msaaSampleCount = 2;
                mobile.renderScale = 1f;
                var rp = new SerializedObject(mobile);
                rp.FindProperty("m_MainLightShadowsSupported").boolValue = false;
                rp.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
                rp.ApplyModifiedProperties();
                EditorUtility.SetDirty(mobile);
            }
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static void Also<T>(this T value, System.Action<T> action) => action(value);
    }
}
