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
- `PetData` ScriptableObject with 5 animation states (Idle, Walk, Sleep, Eat, Petted), movement/animation speed, and draw size. Only Idle is required: missing states fall back to Idle.
- Automatic frame count (`0` = sheet width ÷ height, square frames); set a number to override.
- **+ New Pet** (window toolbar and `Assets → Create → EditorPets → New Pet`): builds a pet from the selected sprite sheets, assigning them to states by file name (`idle`, `walk`/`run`, `sleep`, `eat`, `happy`/`petted`) and fixing their import settings when they are still on `Default`.
- `GlobalPetSettings` ScriptableObject for shared textures and ball physics (sliders with sane ranges).
- `EditorPetsWindow` (Tools → Editor Pets → Settings) built for many pets: animated thumbnail grid (walks on hover), name search, All/Visible/Hidden filter, pet/visible counter, per-tile eye toggle, right-click menu (show only this one, move, duplicate, select asset), resizable details pane with the selected pet's inspector, separate Settings tab, EN/ES UI, light/dark theme support and a "Make your own pet" help dialog.
- "Solo" (show only one pet) and Duplicate actions; new pets spawn spread across the Scene View width.
- UI Toolkit inspector for `PetData` with live animated preview, state tabs (IDLE/WALK/SLEEP/EAT/PETTED), play/pause and resolved frame count; the same inspector is embedded in the window.
- Multi-Scene View support: pets stay on the floor of every open Scene View regardless of which is active.
- Repaint throttled to 30 FPS with immediate force during drag for low overhead.
- Hide All / Show All buttons for quick visibility control (undoable).
- Duplicate button to clone a `PetData`.
- Randomize Position that preserves controller state.
- Welcome Window shown on first install with a quick start guide.
- 19 sample pets, each in its own `Pets/<Name>/` folder (asset + sprite sheets): Corgi, Noah, Pixel Dog, Tabby Cat, Mishy, Turtle, Deer, Crab, Snake, Capybara, Red Dragon, Ice Dragon, five pigeons (Rock Pigeon, White Dove, Mourning Dove, Crowned Pigeon, Nicobar Pigeon) and two parody mascots (Clawdito, Kimoon), the new ones with Idle/Walk/Sleep/Eat/Happy pixel-art sheets.
- Per-pet food (`PetData.food`) with a one-click food picker in the pet inspector, fed by a 16-item pixel-art library in `Items/Food/` (drop any PNG there to extend it). Food is drawn in front of the pet's mouth, and shown in the EAT preview.
- New pixel-art ball, heart and default food bowl.
- Corgi sprite sheets: Idle (3 frames), Walk (17 frames), Sleep (4 frames), Happy (1 frame).
- Pixel Dog sprites: Idle, Walk (2 frames), Sleep.
- English documentation (`README.md`) plus Spanish (`LEEME.md`).
- `LICENSE.md` (All Rights Reserved).
- `icon.png` (512×512) for Asset Store.
- `EditorPets.Editor.asmdef` (Editor-only assembly) for faster compilation and namespace isolation.
- Dark mode friendly UI using `GUI.skin` colors.
- Cached `GUIStyle` for the pet name tag (reduced GC).
- Tooltips on all toolbar buttons and `PetData` / `GlobalPetSettings` fields.

### Fixed
- Package paths resolved from the asmdef GUID, so the folder can be moved anywhere under `Assets/` or installed under `Packages/`.
- A single `isActive` flag controls visibility (removed the overlapping `location` Scene/House flag); the Scene View re-syncs every update, so Undo, inspector edits and new/deleted pet assets show up without a reload.
- Missing sprite sheets (e.g. no Sleep) no longer draw a white square.
- Pets no longer vanish from the Scene View after a script recompile (the pet list was read while the AssetDatabase was still busy and came back empty).
- `GlobalPetSettings` shipped with `ballRadius = 1` / `gravity = 1` (a 2 px ball that barely fell); restored to 16 / 1200 and the package's own asset now wins over stray copies.
- `lastUpdateTime` initialized in the static constructor to avoid huge delta time on the first frame after recompilation.
- X-axis physics no longer depends on which Scene View is active (uses the first available for consistency).
- Repaint throttled to 30 FPS to reduce editor overhead; immediate force during drag.
- `Randomize Position` no longer destroys and recreates the `PetController` (preserves state, animation, hearts).

### Removed
- Sample backyard scene: pets are a Scene View overlay and never interacted with the 3D yard.
- Automatic procedural "Pixel Dog" generation when no pets exist (the window now shows how to create one instead).
- "Reload All" button (no longer needed).
- Marketing images and internal dev notes moved out of the package (`_Dev~`, ignored by Unity).

### Notes
- This is the first public release. Future updates will follow semantic versioning.
- All pet rendering is done with `Handles.BeginGUI()` / `GUI.DrawTexture()` inside `SceneView.duringSceneGui`.
- All physics simulation uses `EditorApplication.update`, not `Time.deltaTime`.
- Requires Unity 2022.3+; tested on Unity 6.
