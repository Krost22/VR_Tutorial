# Editor Pets — Asset Store submission kit

Everything needed for the Publisher Portal is in this folder:

| File | Portal field | Size / rule |
|---|---|---|
| `KeyImages/Icon_160x160.png` | Icon image | 160×160, no text |
| `KeyImages/Card_420x280.png` | Card image | 420×280, title + publisher only |
| `KeyImages/Cover_1950x1300.png` | Cover image | 1950×1300, title / publisher / tagline |
| `KeyImages/Social_1200x630.png` | Social media image | 1200×630, no text |
| `KeyImages/Logo_512x512.png` | Logo / avatar (optional) | 512×512 |
| `Screenshots/01…10_*.png` | Screenshots | 10 × 1920×1080 (max 10) |
| `Trailer_1920x1080.mp4` | Video (upload to YouTube or Vimeo and paste the link, or upload the file) | 59 s, 1080p30 |
| `../../Build/EditorPets_1.0.0.unitypackage` | Not uploaded — test-import it in a clean project first | |

All images are 24-bit PNG without alpha, contain no Unity logos and do not use the default skybox.

---

## Listing copy (English — paste into the Publisher Portal)

**Package name:** Editor Pets

**Category:** Tools / Utilities (alternative: Tools / GUI)

**Short description (tagline):**
Pixel pets that live in your Scene View. Pet them, feed them, play fetch, and make your own from a sprite sheet in two clicks.

**Description:**

Give your Scene View some company. **Editor Pets** adds cute pixel-art pets that wander along the bottom of the Scene View while you work: they walk, nap, eat, chase a ball and jump for joy when you pet them. Everything is editor-only, so nothing ever ends up in your builds.

**17 pets included** — Corgi, Mishy the chubby black tabby, Tabby Cat, Capybara (with its yuzu), Red Dragon, Ice Dragon, Turtle, Deer, Crab, Snake, five pigeons and more. Each one has hand-made Idle, Walk, Sleep, Eat and Happy animations and its own favourite food.

**Make your own pet in two clicks.** Draw a sprite sheet (one row of square frames), select it and click **+ New Pet**. States are assigned from the file names (idle, walk, sleep, eat, happy), frame counts are detected automatically, and missing states fall back to Idle, so a single sheet is enough to start.

**Built for many pets.** A dedicated window with an animated thumbnail grid, search, All/Visible/Hidden filters, one-click show/hide, Solo, duplicate, and a live animated preview of every animation. A starter pick on first launch lets you choose your first companion.

**Features**
- Pets live in the Scene View as a lightweight overlay: no GameObjects, no scene changes, repaint capped at 30 fps.
- Click to pet (hearts!), drag to move, throw a ball and watch them chase it, Feed All.
- 16 pixel-art foods; pick each pet's food with one click or drop in your own.
- Animated preview with tabs for Idle, Walk, Sleep, Eat and Petted.
- Undo support, light and dark editor themes, several Scene Views at once.
- English and Spanish UI.
- Clean, documented C# (namespace `EditorPets`), editor-only assembly.
- 13-page illustrated manual (English + Spanish PDF).

**Technical details**
- Unity 2022.3 LTS or newer (developed and tested on Unity 6).
- Editor-only: `EditorPets.Editor.asmdef` with `includePlatforms: Editor`. No runtime code, no impact on builds.
- Works with Built-in, URP and HDRP (it draws in the Scene View only).
- No external dependencies.
- Sprites: 16–64 px pixel art, Point filtering.
- Documentation: `Documentation/EditorPets_Manual_EN.pdf`.

**Keywords:** editor, pets, pixel art, scene view, virtual pet, companion, desktop pet, mascot, cute, animals, sprite, editor extension, productivity, fun, tamagotchi

**Release notes (1.0.0):** First release.

---

## Guía de subida (paso a paso)

> La subida se hace con **tu cuenta de publisher**, así que estos pasos los haces tú: requieren iniciar sesión y publican contenido.

1. **Limpieza final** (ya hecha): las parodias, la escena del patio y el `GlobalPetSettings` duplicado están en `Assets/EditorPets_LocalExtras/`, **fuera** del paquete. `_Dev~` y los archivos de marketing no se publican (Unity ignora las carpetas que terminan en `~`).
2. **Prueba el paquete**: importa `_Dev~/Build/EditorPets_1.0.0.unitypackage` en un proyecto vacío (idealmente Unity 2022.3 LTS y Unity 6). Comprueba que compila sin errores, que aparece la bienvenida y que las mascotas caminan.
3. **Crea el borrador** en <https://publisher.unity.com> → *Products* → *Create package*. Nombre: *Editor Pets*, categoría *Tools / Utilities*.
4. **Instala Asset Store Publishing Tools** (gratis) desde la Asset Store → *Add to My Assets* → Package Manager → *My Assets* → *Import*.
5. **Valida** (Tools → Asset Store → Validator): tipo *UnityPackage* y como ruta **solo `Assets/KrostGames/EditorPets`** (no una subcarpeta ni `Assets/EditorPets_LocalExtras`). Con esa ruta pasan las 34 comprobaciones, incluidas *Check Demo Scenes* (escena `Demo/EditorPets_Demo.unity`) y *Check Documentation* (`Documentation/*.pdf`).
6. En Unity: **Tools → Asset Store → Uploader**, inicia sesión con tu cuenta de publisher, elige el borrador *Editor Pets*, selecciona la carpeta **`Assets/KrostGames/EditorPets`** y pulsa **Validate** (corrige lo que marque) y luego **Upload**.
7. En el Publisher Portal rellena **Metadata** con los textos de arriba, sube las **Key images** y las **Screenshots** de esta carpeta, pega el enlace del **video** y fija el **precio**.
8. Antes de enviar: pon la **fecha real** en `CHANGELOG.md` (ahora `2026-XX-XX`) y revisa `LICENSE.md` (ver nota). Después, **Submit for review**.

**Nota sobre la licencia:** `LICENSE.md` dice que se prohíbe la modificación sin permiso, pero el EULA de la Asset Store permite a los compradores modificar el asset para usarlo en sus proyectos. Conviene quitar la palabra *modification* de ese párrafo o dejar solo la referencia al EULA para evitar un rechazo o confusión.
