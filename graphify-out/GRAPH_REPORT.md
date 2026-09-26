# Graph Report - EditorPets  (2026-09-25)

## Corpus Check
- Corpus is ~26,655 words - fits in a single context window. You may not need a graph.

## Summary
- 292 nodes · 588 edges · 11 communities
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 26 edges (avg confidence: 0.82)
- Token cost: 79,689 input · 0 output

## Community Hubs (Navigation)
- Sprite Generator (petgen)
- Design Decisions & Packaging
- Editor Pets Window
- Scene Overlay & Ball
- Pet Behaviour State Machine
- Animation & Pet Creation
- Scripts & Namespaces
- Welcome Window
- Window Features (docs)
- Pet Inspector & Food Picker
- Package Manifest

## God Nodes (most connected - your core abstractions)
1. `ScenePetOverlay` - 43 edges
2. `EditorPetsWindow` - 28 edges
3. `PetData` - 27 edges
4. `Canvas` - 22 edges
5. `PetController` - 21 edges
6. `README.md (Editor Pets manual, EN)` - 19 edges
7. `WelcomeWindow` - 13 edges
8. `PetState` - 12 edges
9. `zzz()` - 12 edges
10. `Per-Pet Food` - 12 edges

## Surprising Connections (you probably didn't know these)
- `Manual Delta Time from timeSinceStartup (clamped 0.05 s)` --references--> `PetController`  [INFERRED]
  _Dev~/EditorPets_Context.md → Editor/Scripts/PetController.cs
- `Multi-Scene View Support` --references--> `ScenePetOverlay`  [EXTRACTED]
  CHANGELOG.md → Editor/Scripts/ScenePetOverlay.cs
- `Duplicate Pet` --references--> `ScenePetOverlay`  [EXTRACTED]
  README.md → Editor/Scripts/ScenePetOverlay.cs
- `Window Tick Polling (schedule.Execute every 120 ms)` --references--> `EditorPetsWindow`  [EXTRACTED]
  _Dev~/EditorPets_Context.md → Editor/Scripts/EditorPetsWindow.cs
- `PetData UI Toolkit Inspector with Live Animated Preview` --references--> `EditorPetsWindow`  [EXTRACTED]
  CHANGELOG.md → Editor/Scripts/EditorPetsWindow.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **New Pet Creation Pipeline** — readme_new_pet_flow, readme_state_by_file_name, readme_auto_frame_detection, _dev__editorpets_context_texture_import_fix, editor_scripts_scenepetoverlay_editorpets_scenepetoverlay_createpetfromselection, editor_scripts_petdata_editorpets_petdata_getanimation [EXTRACTED 1.00]
- **Per-Pet Feeding Flow** — readme_feed_all, readme_per_pet_food, readme_food_library, readme_default_food_bowl, editor_scripts_petdataeditor_editorpets_petdataeditor_foodpicker, editor_scripts_scenepetoverlay_editorpets_scenepetoverlay_feedall [EXTRACTED 1.00]
- **Visibility Sync Mechanism** — changelog_isactive_single_source, _dev__editorpets_context_lazy_pet_list, editor_scripts_scenepetoverlay_editorpets_scenepetoverlay_syncpets, readme_solo_show_only, readme_show_hide_all, _dev__editorpets_context_window_tick_polling [INFERRED 0.85]

## Communities (11 total, 0 thin omitted)

### Community 0 - "Sprite Generator (petgen)"
Cohesion: 0.09
Nodes (46): Canvas, CAPY_PAL, capybara(), capySheets(), cat(), CAT_PAL, catSheets(), CLAWD_PAL (+38 more)

### Community 1 - "Design Decisions & Packaging"
Cohesion: 0.07
Nodes (43): EditorPets_Context.md (internal technical notes), Asset Store Packaging, _Dev~ Folder (ignored by Unity, not published), GlobalPetSettings Load Fallback Chain, Lazy Pet List Loading (allPets null until used), petgen.mjs Procedural Sprite Generator, Window Tick Polling (schedule.Execute every 120 ms), Location-Independent Paths via asmdef GUID (+35 more)

### Community 2 - "Editor Pets Window"
Cohesion: 0.09
Nodes (20): DragPerformEvent, DragUpdatedEvent, Button, Label, List, Texture2D, VisualElement, EditorPetsWindow (+12 more)

### Community 3 - "Scene Overlay & Ball"
Cohesion: 0.08
Nodes (18): Manual Delta Time from timeSinceStartup (clamped 0.05 s), Ball, Repaint Throttled to 30 FPS (forced during drag), Dictionary, List, MenuItem, Texture2D, Vector2 (+10 more)

### Community 4 - "Pet Behaviour State Machine"
Cohesion: 0.12
Nodes (18): List, Rect, Texture2D, Vector2, HeartParticle, PetController, HeartTexture, PetState (+10 more)

### Community 5 - "Animation & Pet Creation"
Cohesion: 0.12
Nodes (16): Auto Texture Import Fix (Sprite / Point / no compression), Button, Label, PetCardElement, Texture2D, frames, IMGUIContainer, Automatic Frame Count Detection (width / height, 0 = auto) (+8 more)

### Community 6 - "Scripts & Namespaces"
Cohesion: 0.22
Nodes (11): EditorPets, editorpets_editorpetswindow, system, system_collections_generic, system_io, system_linq, system_text_regularexpressions, unityeditor (+3 more)

### Community 7 - "Welcome Window"
Cohesion: 0.17
Nodes (6): MenuItem, MenuItem, Texture2D, WelcomeWindow, EditorWindow, InitializeOnLoadMethod

### Community 8 - "Window Features (docs)"
Cohesion: 0.22
Nodes (10): Per-User EditorPrefs Preferences, Welcome Window (first install), Duplicate Pet, Editor Pets Window (Tools > Editor Pets > Settings), EN/ES Language Switch, Interactable Toggle, Pet Search and All/Visible/Hidden Filter, Settings Tab (Show Names, Opacity, ball/food/heart) (+2 more)

### Community 9 - "Pet Inspector & Food Picker"
Cohesion: 0.27
Nodes (6): PetData UI Toolkit Inspector with Live Animated Preview, Editor, Texture2D, VisualElement, PetDataEditor, IEnumerable

### Community 10 - "Package Manifest"
Cohesion: 0.22
Nodes (8): author, name, description, displayName, keywords, name, unity, version

## Knowledge Gaps
- **43 isolated node(s):** `All`, `Visible`, `Hidden`, `Idle`, `Walk` (+38 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 91 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ScenePetOverlay` connect `Scene Overlay & Ball` to `Design Decisions & Packaging`, `Editor Pets Window`, `Pet Behaviour State Machine`, `Scripts & Namespaces`, `Window Features (docs)`?**
  _High betweenness centrality (0.178) - this node is a cross-community bridge._
- **Why does `EditorPetsWindow` connect `Editor Pets Window` to `Design Decisions & Packaging`, `Scene Overlay & Ball`, `Animation & Pet Creation`, `Scripts & Namespaces`, `Welcome Window`, `Window Features (docs)`, `Pet Inspector & Food Picker`?**
  _High betweenness centrality (0.169) - this node is a cross-community bridge._
- **Why does `PetData` connect `Design Decisions & Packaging` to `Editor Pets Window`, `Scene Overlay & Ball`, `Pet Behaviour State Machine`, `Animation & Pet Creation`, `Scripts & Namespaces`, `Window Features (docs)`?**
  _High betweenness centrality (0.159) - this node is a cross-community bridge._
- **Are the 6 inferred relationships involving `PetData` (e.g. with `Bundled Cat Pets (Tabby Cat, Mishy)` and `Bundled Critters (Turtle, Deer, Crab, Snake, Capybara)`) actually correct?**
  _`PetData` has 6 INFERRED edges - model-reasoned connections that need verification._
- **What connects `All`, `Visible`, `Hidden` to the rest of the system?**
  _43 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Sprite Generator (petgen)` be split into smaller, more focused modules?**
  _Cohesion score 0.08942139099941554 - nodes in this community are weakly interconnected._
- **Should `Design Decisions & Packaging` be split into smaller, more focused modules?**
  _Cohesion score 0.07493061979648474 - nodes in this community are weakly interconnected._