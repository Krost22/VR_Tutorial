# Changelog

All notable changes to EditorPets will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-XX-XX

### Added
- Initial public release.
- Interactive pets in the Scene View: walk, sleep, idle, eat, play, interact, drag.
- 7-state machine per pet with autonomous transitions and configurable timers.
- Horizontal spritesheet animation with UV-based frame selection and directional flip.
- Ball with 2D physics: gravity, bounce, friction, wall collisions, pet pushing, mouse drag-to-throw.
- Petting interaction: click a pet to spawn a heart particle and trigger a brief happy state.
- Feeding interaction: food bowl texture appears next to the pet for 4 seconds.
- Playing interaction: pets chase the ball when it is dragged.
- `PetData` ScriptableObject with 5 animation states (Idle, Walk, Sleep, Eat, Petted), movement/animation speed, and draw size.
- `GlobalPetSettings` ScriptableObject for shared textures and ball physics.
- `EditorPetsWindow` (Tools → Editor Pets Settings) with toolbar, per-pet panels, and global settings.
- Custom Inspector for `PetData` with live animated preview, state tabs (IDLE/WALK/SLEEP/EAT/PETTED), play/pause, reset, and frame count slider.
- Multi-Scene View support: pets stay on the floor of every open Scene View regardless of which is active.
- Repaint throttled to 30 FPS with immediate force during drag for low overhead.
- Hide All / Show All buttons for quick visibility control.
- Duplicate button to clone a `PetData` (deferred via `EditorApplication.delayCall`).
- Randomize Position that preserves controller state.
- Welcome Window shown on first install with quick start guide.
- Sample scene `Example scene/Editor Pets Sample.unity` with a backyard (grass, fence, house, trees, food bowl), camera, and 3 pre-placed pets (Corgi, DefaultDog, Noah Dog).
- 3 sample `PetData` assets: Corgi, DefaultDog, Noah Dog.
- Global textures: Ball, Food, Heart (all with Point filter, uncompressed).
- Corgi sprite sheets: Idle (3 frames), Walk (17 frames), Sleep (4 frames), Happy (1 frame).
- Legacy Dog sprites: Idle, Walk (2 frames), Sleep.
- `LICENSE.md` (All Rights Reserved).
- `icon.png` (512×512) for Asset Store.
- `EditorPets.Editor.asmdef` (Editor-only assembly) for faster compilation and namespace isolation.
- Dark mode friendly UI using `GUI.skin` colors.
- Cached `GUIStyle` for the pet name tag (reduced GC).
- `OnValidate` validation in `PetData` (clamped frame counts).
- Tooltips on all toolbar buttons.

### Fixed
- Texture paths hardcoded to `Assets/EditorPets/...` now correctly point to `Assets/KrostGames/EditorPets/...` (the actual install location), so the ball and food textures load properly out of the box.
- `lastUpdateTime` initialized in the static constructor to avoid huge delta time on the first frame after recompilation.
- X-axis physics no longer depends on which Scene View is active (uses the first available for consistency).
- Repaint throttled to 30 FPS to reduce editor overhead; immediate force during drag.
- `Randomize Position` no longer destroys and recreates the `PetController` (preserves state, animation, hearts).
- `Duplicate` deferred via `EditorApplication.delayCall` to avoid `InvalidOperationException: Collection was modified` and `Invalid GUILayout state` during OnGUI.

### Notes
- This is the first public release. Future updates will follow semantic versioning.
- Tested on Unity 2021.3 LTS, Unity 2022.3 LTS, and Unity 6.
- All pet rendering is done with `Handles.BeginGUI()` / `GUI.DrawTexture()` inside `SceneView.duringSceneGui`.
- All physics simulation uses `EditorApplication.update`, not `Time.deltaTime`.
