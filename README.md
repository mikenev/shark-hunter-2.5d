# Shark Hunter 2.5D

2.5D underwater shark hunting game. Unity 6000.3.25f1 (Unity 6.3 LTS), URP, low-poly stylised look.
Gameplay is constrained to a 2D plane (X/Y, Z locked); the camera is a perspective side view with a slight
downward tilt, and layered foreground/background props sit at different Z depths for parallax.
Targets: desktop and WebGL (GitHub Pages).

## Run it
Open the project in Unity 6000.3.25f1 and open `Assets/_Game/Scenes/Main.unity`. WASD / arrows / gamepad stick to swim; Space / left click / gamepad A to bite; R / Enter / Start to restart after starving.

## Regenerate the placeholder slice
`Tools > Shark Hunter > Build Placeholder Slice` rebuilds all generated meshes, materials, prefabs and the Main scene
from code (`Assets/_Game/Scripts/Editor/SliceBuilder.cs`), and applies the URP / WebGL project settings.
Headless: `Unity -batchmode -quit -projectPath . -executeMethod SharkHunter.EditorTools.SliceBuilder.BuildAll`.

## Layout
```
Assets/_Game/
  Scripts/
    Core/       SharkConfig (movement + bite tuning), PreyDefinition (per-prey tuning)
    Gameplay/   PlayArea, ISwimInput/SwimInput, SharkController, SharkBite, IBitable, PreyFish, PreySpawner,
                GameSession (score/hunger), ISharkVisual, SimpleSwimmer (background fish)
    CameraRig/  SideViewCamera
    World/      ParallaxLayer, WaterEnvironment (fog/tint), FollowCamera (bubbles), SwayMotion, BiteFeedback, HudOverlay (IMGUI placeholder HUD)
    Visuals/    MeshBuilder, ShapeFactory (procedural placeholder meshes), SharkVisual
    Editor/     SliceBuilder, BuildTools, CaptureTool
  Prefabs/Placeholder/   generated prefabs
  Art/Generated/         generated meshes/materials/textures (procedural, no third-party assets)
  Art/                   put real third-party art here and list it in ASSETS.md
  Settings/              SharkConfig, volume profile
```

## Swapping art
Gameplay code only talks to `ISharkVisual`. Replace the `Visual` child of `Prefabs/Placeholder/Shark.prefab`
with any model that has a component implementing `ISharkVisual` (see `SharkVisual` for the reference implementation).

## Gameplay loop
Bite = a short lunge plus a mouth hitbox (`SharkBite`, tuned in `Settings/SharkConfig`). Prey (`PreyFish`, tuned by
`Settings/Prey_*.asset`) wanders, flees when the shark is near, and can be cornered at the play-area edge. Big fish take
two bites. Eating restores hunger and scores points; if hunger hits zero the shark starves (`GameSession`).

## Publish to GitHub Pages
1. In Unity: `Tools > Shark Hunter > Build WebGL` (output in `Build/WebGL`, gitignored).
2. Run `tools/publish-webgl.sh` — force-pushes the build as a single commit to the `gh-pages` branch.
3. One-time: repo Settings > Pages > Deploy from branch `gh-pages`, `/ (root)`.
Live at https://mikenev.github.io/shark-hunter-2.5d/
