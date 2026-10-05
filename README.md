# Shark Hunter 2.5D

2.5D underwater shark hunting game. Unity 6000.3.25f1 (Unity 6.3 LTS), URP, low-poly stylised look.
Gameplay is constrained to a 2D plane (X/Y, Z locked); the camera is a perspective side view with a slight
downward tilt, and layered foreground/background props sit at different Z depths for parallax.
Targets: desktop and WebGL (GitHub Pages).

## Run it
Open the project in Unity 6000.3.25f1 and open `Assets/_Game/Scenes/Main.unity`. WASD / arrows / gamepad stick to swim.

## Regenerate the placeholder slice
`Tools > Shark Hunter > Build Placeholder Slice` rebuilds all generated meshes, materials, prefabs and the Main scene
from code (`Assets/_Game/Scripts/Editor/SliceBuilder.cs`), and applies the URP / WebGL project settings.
Headless: `Unity -batchmode -quit -projectPath . -executeMethod SharkHunter.EditorTools.SliceBuilder.BuildAll`.

## Layout
```
Assets/_Game/
  Scripts/
    Core/       SharkConfig (movement tuning ScriptableObject)
    Gameplay/   PlayArea, ISwimInput/SwimInput, SharkController, ISharkVisual, SimpleSwimmer
    CameraRig/  SideViewCamera
    World/      ParallaxLayer, WaterEnvironment (fog/tint), FollowCamera (bubbles), SwayMotion
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
