# Editor Pets

Pixel-art pets that live in your **Scene View**. They wander, nap, eat, play with a ball and react when you pet them. Make your own pet from a sprite sheet in two clicks.

> **Version 1.0.0** · Editor-only (nothing is added to your builds) · Spanish version: [`LEEME.md`](./LEEME.md)

---

## Quick start

1. Import the package. The welcome window opens and the pets start walking along the bottom of the Scene View.
2. Open **Tools → Editor Pets → Settings** to show/hide pets, spawn a ball or feed them.
3. In the Scene View: **click** a pet to pet it, **drag** it to move it, **drag the ball** to throw it.

The `EditorPets` folder can live anywhere in your project (`Assets/...` or `Packages/...`).

---

## Make your own pet

1. **Draw a sprite sheet**: all frames side by side in **one row**, every frame **square**.
   Example: 4 frames of 32×32 → a 128×32 PNG.
2. **Only Idle is required.** For more animations make one PNG per state and put the state in the file name:

   | State | Word in the file name | If missing |
   |-------|----------------------|------------|
   | Idle | `idle` (or anything else) | — |
   | Walk | `walk` or `run` | uses Idle |
   | Sleep | `sleep` | uses Idle |
   | Eat | `eat` | uses Idle |
   | Petted | `happy` or `petted` | uses Idle |

   e.g. `Cat_Idle.png`, `Cat_Walk.png`, `Cat_Sleep.png`.
3. **Select the PNG(s)** in the Project window and click **+ New Pet** in the Editor Pets window
   (or right-click → **Create → EditorPets → New Pet**). The pet asset is created next to the sprites.
4. **Pick its food** with one click in the food row of its inspector.

Done: the pet appears in the Scene View. Frame counts are detected automatically (width ÷ height); type a number in `Frames` only for non-square frames (0 = auto). Speed and size are in the same inspector, with a live animated preview of every state.

> Tip: put each pet in its own folder under `Pets/` like the bundled ones, so its asset and sprites stay together.

---

## Food

Each pet eats its own food when you click **Feed All**. The food library lives in `Items/Food/`:

Apple · Bone · Bowl (default) · Carrot · Cheese · Cookie · Egg · Fish · Leaf · Meat · Mooncake · Popsicle · Seeds · Shrimp · Strawberry · Watermelon

Drop any PNG in `Items/Food/` and it shows up in the food picker of every pet. Pets without food eat from the default bowl (**Settings → Ball, Food & Heart**).

---

## The Editor Pets window

**Header:** pet counter (`19 pets · 4 visible`), EN/ES language switch and **?** (how to make a pet).
**Toolbar:** **+ New Pet**, **Spawn Ball**, **Feed All** and **Interactable** (turn it off if pets get in the way of your clicks).

**Pets tab**, made for many pets:
- Grid of animated thumbnails (they walk on hover); hidden pets are dimmed.
- **Search** by name and filter **All / Visible / Hidden**.
- The **eye** on each tile shows/hides that pet. **Show all / Hide all** below the grid (Ctrl+Z undoes).
- Click = select, double-click = select the asset, right-click = show/hide, **show only this one**, move, duplicate.
- Details pane (drag the divider to resize): **Visible**, **Solo**, **Move**, **Duplicate**, **Asset** and the pet's inspector.

**Settings tab:** **Show Names**, **Opacity**, and the ball, default food and heart settings.

The window follows the editor's light/dark theme.

---

## In the Scene View

| Action | Result |
|--------|--------|
| Click a pet | Pets it (heart + Petted animation) |
| Drag a pet | Moves it |
| Drag a pet out of the Scene View and drop it on the Editor Pets window | Hides it |
| Drag a pet asset from the Project window into the Scene View | Shows it |
| Drag the ball | Throws it |

States: Idle, Walk, Sleep (10–20 s), Petted (2 s after a click), Drag, Eat (4 s), Play (chases the ball).

---

## Included pets

| Pet | Food | Pet | Food |
|-----|------|-----|------|
| Corgi | Bone | Red Dragon | Meat |
| Noah | Bone | Ice Dragon | Popsicle |
| Pixel Dog | Bone | Rock Pigeon | Seeds |
| Tabby Cat | Fish | White Dove | Seeds |
| Mishy (chubby black tabby) | Fish | Mourning Dove | Seeds |
| Turtle | Leaf | Crowned Pigeon | Seeds |
| Deer | Apple | Nicobar Pigeon | Seeds |
| Crab | Shrimp | Clawdito | Cookie |
| Snake | Egg | Kimoon | Mooncake |
| Capybara | Watermelon | | |

---

## Folder structure

```
EditorPets/
├── README.md, LEEME.md, CHANGELOG.md, LICENSE.md
├── Editor/                       Editor-only code (EditorPets.Editor.asmdef)
│   ├── Scripts/                  C# (namespace EditorPets)
│   └── UI/                       UXML/USS of the window + icon
├── Items/
│   ├── Ball.png, Heart.png
│   ├── GlobalPetSettings.asset   ball physics + default food/heart/ball (recreated if missing)
│   └── Food/                     food library
└── Pets/<Pet Name>/              one folder per pet: PetData asset + its sprite sheets
```

---

## Compatibility

- Unity 2022.3 LTS or newer (tested on Unity 6).
- Editor-only assembly (`includePlatforms: ["Editor"]`); works with any render pipeline.
- Light and dark editor themes.

## License

See [`LICENSE.md`](./LICENSE.md). Version history: [`CHANGELOG.md`](./CHANGELOG.md).
