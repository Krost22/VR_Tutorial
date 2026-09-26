// Builds the Editor Pets user manual (EN + ES) as HTML with embedded images, ready to print to PDF.
// usage: node build.mjs   -> manual_en.html, manual_es.html next to this file
import fs from 'node:fs';
import path from 'node:path';

const HERE = path.dirname(new URL(import.meta.url).pathname).replace(/^\/([A-Za-z]:)/, '$1');
const img = (file) => `data:image/${file.endsWith('.png') ? 'png' : 'jpeg'};base64,${fs.readFileSync(path.join(HERE, 'img', file)).toString('base64')}`;
const fig = (file, caption, cls = '') => `<figure class="${cls}"><img src="${img(file)}"><figcaption>${caption}</figcaption></figure>`;

const T = {
  en: {
    lang: 'en', title: 'Editor Pets — User Manual', version: 'Version 1.0.0 · Unity 2022.3 or newer · Editor-only',
    tagline: 'Pixel pets that live in your Scene View',
    toc: 'Contents',
    sections: [
      ['overview', 'What is Editor Pets?', `
        <p>Editor Pets adds small pixel-art companions to the Unity <b>Scene View</b>. They wander along the bottom of the view, take naps, eat, chase a ball and react when you click them, while you keep working on your game.</p>
        ${fig('scene.png', 'Pets walking along the bottom of the Scene View while a level is being edited.')}
        <ul>
          <li><b>Editor-only:</b> everything lives in an editor assembly. Nothing is added to your builds and nothing runs in Play Mode builds.</li>
          <li><b>Lightweight:</b> pets are drawn as an overlay (no GameObjects, no scene changes) and repaints are limited to 30 fps.</li>
          <li><b>17 pets included</b>, each with Idle, Walk, Sleep, Eat and Happy animations, plus a library of 16 foods.</li>
          <li><b>Make your own</b> from a sprite sheet in two clicks.</li>
          <li>Works with any render pipeline, light and dark editor themes, and several Scene Views at once.</li>
        </ul>`],
      ['install', 'Installation and first launch', `
        <ol>
          <li>Import the package. It installs into <code>Assets/KrostGames/EditorPets</code>. You can move the folder anywhere later (even into <code>Packages/</code>); the tool finds its files on its own.</li>
          <li>The <b>Welcome</b> window opens and asks you to <b>choose your first pet</b>: a Fire, Water or Grass starter, or any of the 17 pets from the <i>All pets</i> tab. Nothing is locked; the choice just decides who keeps you company first.</li>
          <li>Click <b>I choose you!</b> The pet appears in your Scene View and the other pets are hidden (turn them back on any time).</li>
        </ol>
        <div class="row">${fig('welcome.jpg', 'Starter pick on first launch.')}${fig('joined.jpg', 'Your first pet joins the Scene View.')}</div>
        <p class="note">You can reopen this window from <b>Tools → Editor Pets → Welcome</b>.</p>
        <h3>Demo scene</h3>
        <p><b>Tools → Editor Pets → Open Demo Scene</b> (or <i>Open demo scene</i> in the Welcome window) opens <code>Demo/EditorPets_Demo.unity</code>, a small pixel-art platformer level, and frames it in a 2D Scene View so you can watch your pets walk on the grass.</p>
        ${fig('scene.png', 'The demo scene with pets.')}`],
      ['window', 'The Editor Pets window', `
        <p>Open it from <b>Tools → Editor Pets → Settings</b>. It has a header, a toolbar and two tabs.</p>
        ${fig('window.jpg', 'Pets tab: grid of pets on top, the selected pet below.')}
        <h3>Header and toolbar</h3>
        <table><tr><th>Control</th><th>What it does</th></tr>
          <tr><td>Counter</td><td>How many pets exist and how many are visible.</td></tr>
          <tr><td>EN / ES</td><td>Switches the window language (English / Spanish).</td></tr>
          <tr><td>?</td><td>Quick help on making your own pet.</td></tr>
          <tr><td>+ New Pet</td><td>Creates a pet from the sprite sheets selected in the Project window (see chapter 5).</td></tr>
          <tr><td>Spawn Ball</td><td>Drops a ball in the Scene View. Drag it to throw it; pets chase it.</td></tr>
          <tr><td>Feed All</td><td>Every visible pet eats its own food for a few seconds.</td></tr>
          <tr><td>Interactable</td><td>Turn off if pets get in the way of your clicks in the Scene View.</td></tr>
        </table>
        <h3>Pets tab</h3>
        <ul>
          <li><b>Grid:</b> animated thumbnails (they walk when you hover them). Hidden pets are dimmed.</li>
          <li><b>Search</b> by name and filter <b>All / Visible / Hidden</b>.</li>
          <li><b>Eye</b> on each tile: show or hide that pet. <b>Show all / Hide all</b> under the grid. Everything can be undone with Ctrl+Z.</li>
          <li><b>Click</b> a tile to select it, <b>double-click</b> to select the asset in the Project window, <b>right-click</b> for more: show/hide, <i>show only this one</i>, move to a random spot, duplicate, select asset.</li>
          <li><b>Details pane</b> (drag the divider to resize): Visible, <b>Solo</b> (hide every other pet), Move, Duplicate, Asset, and the full pet inspector.</li>
        </ul>
        <div class="row">${fig('solo.jpg', 'Searching “pige” and using Solo on the Crowned Pigeon.')}${fig('settings.jpg', 'Settings tab.')}</div>
        <h3>Settings tab</h3>
        <ul>
          <li><b>Show Names</b> and <b>Opacity</b> of the pets in the Scene View (saved per user).</li>
          <li><b>Ball, Food &amp; Heart:</b> the shared textures (heart, default food, ball), ball radius and gravity. Stored in <code>Items/GlobalPetSettings.asset</code>, which is recreated automatically if you delete it.</li>
        </ul>`],
      ['scene', 'Playing with the pets in the Scene View', `
        <table><tr><th>Action</th><th>Result</th></tr>
          <tr><td>Click a pet</td><td>Pets it: a heart pops up and the Happy animation plays.</td></tr>
          <tr><td>Drag a pet</td><td>Moves it. Release it and it lands on the floor of the view.</td></tr>
          <tr><td>Drag a pet out of the Scene View and drop it on the Editor Pets window</td><td>Hides it.</td></tr>
          <tr><td>Drag a pet asset from the Project window into the Scene View</td><td>Shows it.</td></tr>
          <tr><td>Spawn Ball, then drag and release the ball</td><td>Throws it; pets chase it.</td></tr>
          <tr><td>Feed All</td><td>Each pet eats its own food.</td></tr>
        </table>
        ${fig('closeup.png', 'Every pet has Idle, Walk, Sleep, Eat and Happy animations.')}
        <p>On their own, pets idle, walk, and sometimes fall asleep for 10–20 seconds. Eating lasts 4 seconds, being petted 2 seconds.</p>`],
      ['create', 'Creating your own pet (step by step)', `
        <p>A pet is one <b>PetData</b> asset plus one to five sprite sheets. This chapter walks you through the whole process.</p>
        <h3>5.1 Draw the sprite sheets</h3>
        ${fig('sheet.png', 'A sprite sheet is a single row of square frames.', 'wide')}
        <ul>
          <li><b>One row</b> of frames, side by side, no gaps, transparent background, PNG.</li>
          <li><b>Square frames</b> (width = height). The number of frames is then detected automatically as <i>width ÷ height</i>. Example: 4 frames of 32×32 → a 128×32 PNG.</li>
          <li>Use the <b>same frame size</b> for all the sheets of a pet, draw it <b>facing right</b> (it is flipped automatically when walking left) and put its <b>feet on the bottom row</b> of the frame.</li>
          <li>Good sizes are 16, 24, 32, 40 or 64 px per frame, with 2–8 frames per animation.</li>
          <li>Tools: Aseprite (<i>File → Export Sprite Sheet → By Rows, 1 row</i>), Piskel, LibreSprite or Photoshop all work.</li>
        </ul>
        <h3>5.2 Which animations</h3>
        <table><tr><th>State</th><th>When it plays</th><th>Word in the file name</th><th>If missing</th></tr>
          <tr><td><b>Idle</b> (required)</td><td>Standing still, and while being dragged</td><td><code>idle</code> or anything else</td><td>—</td></tr>
          <tr><td>Walk</td><td>Walking and chasing the ball</td><td><code>walk</code> or <code>run</code></td><td>uses Idle</td></tr>
          <tr><td>Sleep</td><td>Napping</td><td><code>sleep</code></td><td>uses Idle</td></tr>
          <tr><td>Eat</td><td>After Feed All</td><td><code>eat</code></td><td>uses Idle</td></tr>
          <tr><td>Petted</td><td>After a click</td><td><code>happy</code> or <code>petted</code></td><td>uses Idle</td></tr>
        </table>
        ${fig('states.png', 'The five sheets of Mishy, named so they are assigned automatically.', 'half')}
        <h3>5.3 Put them in a folder</h3>
        <p>Create a folder such as <code>Assets/KrostGames/EditorPets/Pets/My Cat/</code> (any folder in your project works) and copy the PNGs there. Keeping each pet in its own folder, like the bundled ones, keeps its asset and sprites together.</p>
        <h3>5.4 Create the pet</h3>
        <ol>
          <li>Select all the PNGs of the pet in the Project window.</li>
          <li>Click <b>+ New Pet</b> in the Editor Pets window, or right-click → <b>Create → EditorPets → New Pet</b>.</li>
          <li>The pet asset is created <b>next to the sprites</b>, named after the first word of the file name (<i>Mishy_Walk.png</i> → <i>Mishy</i>), is selected, and starts walking in the Scene View.</li>
        </ol>
        <p class="note">If the PNGs are still on Unity's default texture import settings, New Pet switches them to <b>Sprite, Point filter, no compression</b> so the pixels stay crisp and the frame count is correct. Textures you already configured are left untouched.</p>
        ${fig('inspector.jpg', 'The pet inspector (also shown in the details pane of the window).')}
        <h3>5.5 Fine-tune it in the inspector</h3>
        <ul>
          <li><b>Preview</b> with tabs IDLE / WALK / SLEEP / EAT / PETTED, play/pause and the resolved number of frames. Use it to check each sheet.</li>
          <li><b>Pet Name</b>: shown above the pet when <i>Show Names</i> is on. <b>Is Active</b>: visible in the Scene View.</li>
          <li><b>Sprite Sheets</b> and <b>Frames</b>: drop another texture into any state; leave Frames at <b>0</b> for automatic, or type the number of frames only if your frames are not square.</li>
          <li><b>Food</b>: click one of the foods of the library (the chosen one is highlighted in orange), or drop any texture in the field.</li>
          <li><b>Animation Speed</b> (frames per second, shared by all states), <b>Move Speed</b> (pixels per second) and <b>Size</b> (pixels in the Scene View). For crisp pixel art use a whole multiple of the frame size: 32 px frames → 64 or 96.</li>
        </ul>
        <h3>5.6 Variants and manual creation</h3>
        <ul>
          <li><b>Duplicate</b> (details pane or right-click) copies a pet so you can swap its sheets or change its name, speed, size or food.</li>
          <li>You can also create an empty pet with <b>Create → EditorPets → New Pet</b> without selecting textures, then fill in the fields by hand.</li>
        </ul>
        <h3>5.7 Checklist</h3>
        <table><tr><th>Problem</th><th>Fix</th></tr>
          <tr><td>The animation jumps or shows half frames</td><td>The frames are not square: type the real number of frames in <i>Frames</i>.</td></tr>
          <tr><td>The pet looks blurry</td><td>Set the texture's Filter Mode to <b>Point</b> and Compression to <b>None</b>.</td></tr>
          <tr><td>A white square appears</td><td>The pet has no Idle texture.</td></tr>
          <tr><td>The pet is tiny or huge</td><td>Change <b>Size</b> (e.g. 2× the frame size).</td></tr>
          <tr><td>It walks backwards</td><td>Draw the sprites facing right.</td></tr>
        </table>`],
      ['food', 'Food', `
        ${fig('foods.png', 'The food library in Items/Food.', 'wide')}
        <ul>
          <li>Each pet eats its own <b>Food</b> (set in its inspector). Pets without food eat from the default <b>Bowl</b> (Settings tab).</li>
          <li>To add a food, drop a small PNG (16×16 works great; Point filter) into <code>Items/Food/</code>. It appears in the food picker of every pet.</li>
          <li>Food is drawn in front of the pet's mouth, on the floor, while it plays its Eat animation.</li>
        </ul>`],
      ['api', 'For programmers', `
        <p>All code is in the <code>EditorPets</code> namespace, in the editor-only assembly <code>EditorPets.Editor</code>.</p>
        <table><tr><th>Member</th><th>Description</th></tr>
          <tr><td><code>PetData</code></td><td>ScriptableObject: <code>petName</code>, <code>isActive</code>, five textures with their frame counts (0 = auto), <code>food</code>, <code>animationSpeed</code>, <code>moveSpeed</code>, <code>size</code>. <code>GetAnimation(PetState)</code> returns the texture and frame count used for a state (with the Idle fallback).</td></tr>
          <tr><td><code>ScenePetOverlay.SetVisible(pet, bool)</code></td><td>Shows or hides a pet (undoable).</td></tr>
          <tr><td><code>ScenePetOverlay.SetAllVisible(bool)</code> / <code>ShowOnly(pet)</code></td><td>Show or hide every pet / keep only one visible.</td></tr>
          <tr><td><code>ScenePetOverlay.FeedAll()</code> / <code>SpawnBall()</code></td><td>Same as the toolbar buttons.</td></tr>
          <tr><td><code>ScenePetOverlay.CreatePetFromSelection()</code></td><td>The + New Pet command.</td></tr>
          <tr><td><code>ScenePetOverlay.Duplicate(pet)</code> / <code>RandomizePetPosition(pet)</code></td><td>Copy a pet asset / move it to a random spot.</td></tr>
        </table>
        <p>The Scene View always reflects each pet's <code>isActive</code> (changes from scripts, the inspector or Undo are picked up automatically). User preferences are stored in EditorPrefs: <code>EditorPets_Interactable</code>, <code>EditorPets_ShowNames</code>, <code>EditorPets_Opacity</code>, <code>EditorPets_IsEnglish</code>, <code>EditorPets_Welcomed_v1</code>.</p>`],
      ['faq', 'Troubleshooting and FAQ', `
        <table><tr><th>Question</th><th>Answer</th></tr>
          <tr><td>I don't see any pet.</td><td>Open the Editor Pets window: check the counter and click <b>Show all</b>. Pets live along the bottom of the Scene View.</td></tr>
          <tr><td>Pets block my clicks on scene objects.</td><td>Turn off <b>Interactable</b> in the toolbar.</td></tr>
          <tr><td>Are pets included in my build?</td><td>No. Everything is editor-only.</td></tr>
          <tr><td>Does it cost performance?</td><td>Very little: pets are an overlay and repaints are capped at 30 fps. Hide pets you don't need.</td></tr>
          <tr><td>Several Scene Views?</td><td>Supported: pets appear in all of them, standing on each view's floor.</td></tr>
          <tr><td>How do I uninstall it?</td><td>Delete the <code>EditorPets</code> folder. Optionally remove the EditorPrefs keys listed in chapter 7.</td></tr>
        </table>
        <p class="note">Support: use the contact link on the KrostGames publisher page of the Unity Asset Store.</p>`],
    ],
  },
  es: {
    lang: 'es', title: 'Editor Pets — Manual de usuario', version: 'Versión 1.0.0 · Unity 2022.3 o superior · Solo editor',
    tagline: 'Mascotas en pixel art que viven en tu Scene View',
    toc: 'Contenido',
    sections: [
      ['overview', '¿Qué es Editor Pets?', `
        <p>Editor Pets añade pequeñas mascotas en pixel art a la <b>Scene View</b> de Unity. Pasean por la parte baja de la vista, duermen, comen, persiguen una pelota y reaccionan cuando haces clic en ellas, mientras sigues trabajando en tu juego.</p>
        ${fig('scene.png', 'Mascotas paseando por la parte baja de la Scene View mientras se edita un nivel.')}
        <ul>
          <li><b>Solo editor:</b> todo vive en un ensamblado de editor. No se añade nada a tus builds.</li>
          <li><b>Ligero:</b> las mascotas se dibujan como overlay (sin GameObjects ni cambios en la escena) y el repintado se limita a 30 fps.</li>
          <li><b>17 mascotas incluidas</b>, cada una con animaciones Idle, Walk, Sleep, Eat y Happy, y una librería de 16 comidas.</li>
          <li><b>Crea las tuyas</b> a partir de un sprite sheet en dos clics.</li>
          <li>Funciona con cualquier render pipeline, con los temas claro y oscuro, y con varias Scene Views a la vez.</li>
        </ul>`],
      ['install', 'Instalación y primer inicio', `
        <ol>
          <li>Importa el paquete. Se instala en <code>Assets/KrostGames/EditorPets</code>. Puedes mover la carpeta a cualquier lugar después (incluso a <code>Packages/</code>); la herramienta encuentra sus archivos sola.</li>
          <li>Se abre la ventana de <b>Bienvenida</b> y te pide <b>elegir tu primera mascota</b>: un inicial de Fuego, Agua o Planta, o cualquiera de las 17 desde la pestaña <i>Todas</i>. Nada está bloqueado; la elección solo decide quién te acompaña primero.</li>
          <li>Pulsa <b>¡Te elijo a ti!</b> La mascota aparece en tu Scene View y las demás quedan ocultas (puedes mostrarlas cuando quieras).</li>
        </ol>
        <div class="row">${fig('welcome.jpg', 'Elección del inicial en el primer inicio.')}${fig('joined.jpg', 'Tu primera mascota se une a la Scene View.')}</div>
        <p class="note">Puedes volver a abrir esta ventana desde <b>Tools → Editor Pets → Welcome</b>.</p>
        <h3>Escena demo</h3>
        <p><b>Tools → Editor Pets → Open Demo Scene</b> (o <i>Abrir escena demo</i> en la bienvenida) abre <code>Demo/EditorPets_Demo.unity</code>, un pequeño nivel de plataformas en pixel art, y lo encuadra en una Scene View 2D para que veas a tus mascotas pasear sobre el pasto.</p>
        ${fig('scene.png', 'La escena demo con mascotas.')}`],
      ['window', 'La ventana Editor Pets', `
        <p>Ábrela desde <b>Tools → Editor Pets → Settings</b>. Tiene una cabecera, una barra de herramientas y dos pestañas.</p>
        ${fig('window.jpg', 'Pestaña Pets: cuadrícula de mascotas arriba y la mascota seleccionada abajo.')}
        <h3>Cabecera y barra</h3>
        <table><tr><th>Control</th><th>Qué hace</th></tr>
          <tr><td>Contador</td><td>Cuántas mascotas hay y cuántas están visibles.</td></tr>
          <tr><td>EN / ES</td><td>Cambia el idioma de la ventana.</td></tr>
          <tr><td>?</td><td>Ayuda rápida para crear tu mascota.</td></tr>
          <tr><td>+ New Pet</td><td>Crea una mascota con los sprite sheets seleccionados en la ventana Project (capítulo 5).</td></tr>
          <tr><td>Spawn Ball</td><td>Suelta una pelota en la Scene View. Arrástrala para lanzarla; las mascotas la persiguen.</td></tr>
          <tr><td>Feed All</td><td>Todas las mascotas visibles comen su propia comida unos segundos.</td></tr>
          <tr><td>Interactable</td><td>Desactívalo si las mascotas estorban tus clics en la Scene View.</td></tr>
        </table>
        <h3>Pestaña Pets</h3>
        <ul>
          <li><b>Cuadrícula:</b> miniaturas animadas (caminan al pasar el mouse). Las ocultas se ven atenuadas.</li>
          <li><b>Busca</b> por nombre y filtra <b>Todas / Visibles / Ocultas</b>.</li>
          <li>El <b>ojo</b> de cada tarjeta muestra u oculta esa mascota. <b>Mostrar todas / Ocultar todas</b> bajo la cuadrícula. Todo se deshace con Ctrl+Z.</li>
          <li><b>Clic</b> selecciona, <b>doble clic</b> selecciona el asset en Project, <b>clic derecho</b> ofrece más: mostrar/ocultar, <i>mostrar solo esta</i>, mover a un sitio aleatorio, duplicar, seleccionar asset.</li>
          <li><b>Panel de detalle</b> (arrastra el divisor para redimensionar): Visible, <b>Solo</b> (oculta las demás), Move, Duplicate, Asset y el inspector completo de la mascota.</li>
        </ul>
        <div class="row">${fig('solo.jpg', 'Buscando “pige” y usando Solo en la Crowned Pigeon.')}${fig('settings.jpg', 'Pestaña Settings.')}</div>
        <h3>Pestaña Settings</h3>
        <ul>
          <li><b>Show Names</b> y <b>Opacity</b> de las mascotas en la Scene View (se guardan por usuario).</li>
          <li><b>Ball, Food &amp; Heart:</b> texturas compartidas (corazón, comida por defecto, pelota), radio y gravedad de la pelota. Se guardan en <code>Items/GlobalPetSettings.asset</code>, que se recrea solo si lo borras.</li>
        </ul>`],
      ['scene', 'Jugar con las mascotas en la Scene View', `
        <table><tr><th>Acción</th><th>Resultado</th></tr>
          <tr><td>Clic en una mascota</td><td>La acaricias: aparece un corazón y se reproduce la animación Happy.</td></tr>
          <tr><td>Arrastrar una mascota</td><td>La mueve. Al soltarla vuelve al suelo de la vista.</td></tr>
          <tr><td>Arrastrarla fuera de la Scene View y soltarla en la ventana Editor Pets</td><td>La oculta.</td></tr>
          <tr><td>Arrastrar un asset de mascota desde Project a la Scene View</td><td>La muestra.</td></tr>
          <tr><td>Spawn Ball y luego arrastrar y soltar la pelota</td><td>La lanza; las mascotas la persiguen.</td></tr>
          <tr><td>Feed All</td><td>Cada mascota come su propia comida.</td></tr>
        </table>
        ${fig('closeup.png', 'Cada mascota tiene animaciones Idle, Walk, Sleep, Eat y Happy.')}
        <p>Por su cuenta, las mascotas esperan, caminan y a veces duermen entre 10 y 20 segundos. Comer dura 4 segundos y ser acariciadas 2 segundos.</p>`],
      ['create', 'Crear tu propia mascota (paso a paso)', `
        <p>Una mascota es un asset <b>PetData</b> más de uno a cinco sprite sheets. Este capítulo recorre todo el proceso.</p>
        <h3>5.1 Dibuja los sprite sheets</h3>
        ${fig('sheet.png', 'Un sprite sheet es una sola fila de frames cuadrados.', 'wide')}
        <ul>
          <li><b>Una sola fila</b> de frames, uno al lado del otro, sin huecos, con fondo transparente, en PNG.</li>
          <li><b>Frames cuadrados</b> (ancho = alto). Así el número de frames se detecta solo como <i>ancho ÷ alto</i>. Ejemplo: 4 frames de 32×32 → un PNG de 128×32.</li>
          <li>Usa el <b>mismo tamaño de frame</b> en todos los sheets de una mascota, dibújala <b>mirando a la derecha</b> (se voltea sola al caminar a la izquierda) y pon sus <b>patas en la última fila</b> del frame.</li>
          <li>Buenos tamaños: 16, 24, 32, 40 o 64 px por frame, con 2 a 8 frames por animación.</li>
          <li>Herramientas: Aseprite (<i>File → Export Sprite Sheet → By Rows, 1 fila</i>), Piskel, LibreSprite o Photoshop sirven.</li>
        </ul>
        <h3>5.2 Qué animaciones</h3>
        <table><tr><th>Estado</th><th>Cuándo se ve</th><th>Palabra en el nombre</th><th>Si falta</th></tr>
          <tr><td><b>Idle</b> (obligatorio)</td><td>Quieta y mientras la arrastras</td><td><code>idle</code> o cualquier otro</td><td>—</td></tr>
          <tr><td>Walk</td><td>Caminando y persiguiendo la pelota</td><td><code>walk</code> o <code>run</code></td><td>usa Idle</td></tr>
          <tr><td>Sleep</td><td>Durmiendo</td><td><code>sleep</code></td><td>usa Idle</td></tr>
          <tr><td>Eat</td><td>Después de Feed All</td><td><code>eat</code></td><td>usa Idle</td></tr>
          <tr><td>Petted</td><td>Después de un clic</td><td><code>happy</code> o <code>petted</code></td><td>usa Idle</td></tr>
        </table>
        ${fig('states.png', 'Los cinco sheets de Mishy, nombrados para asignarse solos.', 'half')}
        <h3>5.3 Ponlos en una carpeta</h3>
        <p>Crea una carpeta como <code>Assets/KrostGames/EditorPets/Pets/Mi Gato/</code> (sirve cualquier carpeta del proyecto) y copia ahí los PNG. Tener cada mascota en su propia carpeta, como las incluidas, mantiene juntos su asset y sus sprites.</p>
        <h3>5.4 Crea la mascota</h3>
        <ol>
          <li>Selecciona todos los PNG de la mascota en la ventana Project.</li>
          <li>Pulsa <b>+ New Pet</b> en la ventana Editor Pets, o clic derecho → <b>Create → EditorPets → New Pet</b>.</li>
          <li>El asset se crea <b>junto a los sprites</b>, con el nombre de la primera palabra del archivo (<i>Mishy_Walk.png</i> → <i>Mishy</i>), queda seleccionado y empieza a pasear por la Scene View.</li>
        </ol>
        <p class="note">Si los PNG siguen con la importación por defecto de Unity, New Pet los cambia a <b>Sprite, filtro Point, sin compresión</b> para que los píxeles se vean nítidos y el número de frames sea correcto. Las texturas que ya configuraste no se tocan.</p>
        ${fig('inspector.jpg', 'El inspector de la mascota (también aparece en el panel de detalle de la ventana).')}
        <h3>5.5 Ajústala en el inspector</h3>
        <ul>
          <li><b>Preview</b> con pestañas IDLE / WALK / SLEEP / EAT / PETTED, reproducir/pausar y el número de frames detectado. Úsalo para revisar cada sheet.</li>
          <li><b>Pet Name</b>: se muestra sobre la mascota si <i>Show Names</i> está activo. <b>Is Active</b>: visible en la Scene View.</li>
          <li><b>Sprite Sheets</b> y <b>Frames</b>: suelta otra textura en cualquier estado; deja Frames en <b>0</b> para automático, o escribe el número de frames solo si tus frames no son cuadrados.</li>
          <li><b>Food</b>: haz clic en una comida de la librería (la elegida se resalta en naranja) o suelta cualquier textura en el campo.</li>
          <li><b>Animation Speed</b> (frames por segundo, compartido por todos los estados), <b>Move Speed</b> (píxeles por segundo) y <b>Size</b> (píxeles en la Scene View). Para pixel art nítido usa un múltiplo entero del tamaño de frame: frames de 32 px → 64 o 96.</li>
        </ul>
        <h3>5.6 Variantes y creación manual</h3>
        <ul>
          <li><b>Duplicate</b> (panel de detalle o clic derecho) copia una mascota para cambiarle los sheets, nombre, velocidad, tamaño o comida.</li>
          <li>También puedes crear una mascota vacía con <b>Create → EditorPets → New Pet</b> sin seleccionar texturas y rellenar los campos a mano.</li>
        </ul>
        <h3>5.7 Lista de comprobación</h3>
        <table><tr><th>Problema</th><th>Solución</th></tr>
          <tr><td>La animación salta o muestra medios frames</td><td>Los frames no son cuadrados: escribe el número real de frames en <i>Frames</i>.</td></tr>
          <tr><td>La mascota se ve borrosa</td><td>Pon el Filter Mode de la textura en <b>Point</b> y Compression en <b>None</b>.</td></tr>
          <tr><td>Aparece un cuadrado blanco</td><td>La mascota no tiene textura Idle.</td></tr>
          <tr><td>Se ve diminuta o enorme</td><td>Cambia <b>Size</b> (p. ej. 2× el tamaño de frame).</td></tr>
          <tr><td>Camina de espaldas</td><td>Dibuja los sprites mirando a la derecha.</td></tr>
        </table>`],
      ['food', 'Comida', `
        ${fig('foods.png', 'La librería de comidas en Items/Food.', 'wide')}
        <ul>
          <li>Cada mascota come su propia <b>Food</b> (se elige en su inspector). Las que no tienen comida usan el <b>Bowl</b> por defecto (pestaña Settings).</li>
          <li>Para añadir una comida, suelta un PNG pequeño (16×16 va muy bien; filtro Point) en <code>Items/Food/</code>. Aparecerá en el selector de todas las mascotas.</li>
          <li>La comida se dibuja delante de la boca, en el suelo, mientras la mascota reproduce su animación Eat.</li>
        </ul>`],
      ['api', 'Para programadores', `
        <p>Todo el código está en el namespace <code>EditorPets</code>, en el ensamblado solo de editor <code>EditorPets.Editor</code>.</p>
        <table><tr><th>Miembro</th><th>Descripción</th></tr>
          <tr><td><code>PetData</code></td><td>ScriptableObject: <code>petName</code>, <code>isActive</code>, cinco texturas con sus frames (0 = automático), <code>food</code>, <code>animationSpeed</code>, <code>moveSpeed</code>, <code>size</code>. <code>GetAnimation(PetState)</code> devuelve la textura y los frames de un estado (con respaldo en Idle).</td></tr>
          <tr><td><code>ScenePetOverlay.SetVisible(pet, bool)</code></td><td>Muestra u oculta una mascota (con Undo).</td></tr>
          <tr><td><code>ScenePetOverlay.SetAllVisible(bool)</code> / <code>ShowOnly(pet)</code></td><td>Mostrar u ocultar todas / dejar solo una visible.</td></tr>
          <tr><td><code>ScenePetOverlay.FeedAll()</code> / <code>SpawnBall()</code></td><td>Igual que los botones de la barra.</td></tr>
          <tr><td><code>ScenePetOverlay.CreatePetFromSelection()</code></td><td>El comando + New Pet.</td></tr>
          <tr><td><code>ScenePetOverlay.Duplicate(pet)</code> / <code>RandomizePetPosition(pet)</code></td><td>Copiar el asset de una mascota / moverla a un sitio aleatorio.</td></tr>
        </table>
        <p>La Scene View siempre refleja el <code>isActive</code> de cada mascota (los cambios desde scripts, el inspector o Undo se aplican solos). Las preferencias se guardan en EditorPrefs: <code>EditorPets_Interactable</code>, <code>EditorPets_ShowNames</code>, <code>EditorPets_Opacity</code>, <code>EditorPets_IsEnglish</code>, <code>EditorPets_Welcomed_v1</code>.</p>`],
      ['faq', 'Solución de problemas y preguntas frecuentes', `
        <table><tr><th>Pregunta</th><th>Respuesta</th></tr>
          <tr><td>No veo ninguna mascota.</td><td>Abre la ventana Editor Pets: mira el contador y pulsa <b>Show all</b>. Las mascotas viven en la parte baja de la Scene View.</td></tr>
          <tr><td>Las mascotas bloquean mis clics en objetos.</td><td>Desactiva <b>Interactable</b> en la barra.</td></tr>
          <tr><td>¿Se incluyen en mi build?</td><td>No. Todo es solo de editor.</td></tr>
          <tr><td>¿Afecta al rendimiento?</td><td>Muy poco: son un overlay y el repintado está limitado a 30 fps. Oculta las que no necesites.</td></tr>
          <tr><td>¿Varias Scene Views?</td><td>Sí: las mascotas aparecen en todas, sobre el suelo de cada vista.</td></tr>
          <tr><td>¿Cómo la desinstalo?</td><td>Borra la carpeta <code>EditorPets</code>. Si quieres, elimina las claves de EditorPrefs del capítulo 7.</td></tr>
        </table>
        <p class="note">Soporte: usa el enlace de contacto de la página de publisher de KrostGames en la Unity Asset Store.</p>`],
    ],
  },
};

const css = `
@page { size: A4; margin: 16mm 15mm 18mm 15mm; }
* { box-sizing: border-box; }
body { font-family: 'Segoe UI', Arial, sans-serif; color: #1f2733; font-size: 10.5pt; line-height: 1.5; margin: 0; }
h1 { font-family: 'Segoe UI Black', 'Segoe UI', sans-serif; font-size: 34pt; margin: 0; color: #1d3354; }
h2 { font-family: 'Segoe UI Black', 'Segoe UI', sans-serif; font-size: 18pt; color: #1d3354; border-bottom: 3px solid #ff8246; padding-bottom: 4px; margin: 0 0 12px; page-break-before: always; }
h3 { font-size: 12.5pt; color: #c85a24; margin: 18px 0 6px; }
.cover { height: 250mm; display: flex; flex-direction: column; justify-content: space-between; }
.cover img.art { width: 100%; border-radius: 10px; }
.cover .sub { font-size: 15pt; color: #3a5a80; margin: 6px 0 2px; }
.cover .ver { color: #667; }
.toc { columns: 2; font-size: 11pt; } .toc div { margin: 3px 0; }
figure { margin: 10px 0 14px; text-align: center; page-break-inside: avoid; }
figure img { max-width: 100%; border-radius: 6px; border: 1px solid #d6dde6; }
figure.half img { max-width: 60%; } figure.wide img { max-width: 100%; }
figcaption { font-size: 9pt; color: #667; margin-top: 4px; }
.row { display: flex; gap: 10px; } .row figure { flex: 1; } .row img { max-height: 250px; }
table { border-collapse: collapse; width: 100%; margin: 8px 0 12px; font-size: 9.8pt; page-break-inside: avoid; }
th { background: #1d3354; color: white; text-align: left; padding: 5px 8px; }
td { border-bottom: 1px solid #e3e8ef; padding: 5px 8px; vertical-align: top; }
tr:nth-child(odd) td { background: #f6f9fc; }
code { background: #eef3f8; padding: 1px 4px; border-radius: 3px; font-size: 9.2pt; }
.note { background: #fff4ec; border-left: 4px solid #ff8246; padding: 8px 12px; border-radius: 4px; }
`;

for (const t of Object.values(T)) {
  const toc = t.sections.map(([id, h], i) => `<div>${i + 1}. ${h}</div>`).join('');
  const body = t.sections.map(([id, h, html], i) => `<h2 id="${id}">${i + 1}. ${h}</h2>${html}`).join('\n');
  const html = `<!doctype html><html lang="${t.lang}"><head><meta charset="utf-8"><title>${t.title}</title><style>${css}</style></head><body>
<section class="cover">
  <div><h1>Editor Pets</h1><div class="sub">${t.tagline}</div><div class="ver">${t.version} · KrostGames</div></div>
  <img class="art" src="${img('cover.png')}">
  <div><b>${t.toc}</b><div class="toc">${toc}</div></div>
</section>
${body}
</body></html>`;
  fs.writeFileSync(path.join(HERE, `manual_${t.lang}.html`), html);
}
console.log('manual html written');
