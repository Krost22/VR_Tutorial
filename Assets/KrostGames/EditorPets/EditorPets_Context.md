# EditorPets — Contexto de la Herramienta

## Descripción General

**EditorPets** es una herramienta para Unity Editor que añade mascotas virtuales interactivas dentro de la **Scene View**. Las mascotas deambulan, duermen, reaccionan al clic, comen, juegan con una pelota y pueden ser completamente personalizadas mediante ScriptableObjects.

**Versión actual:** 1.0.0 (primera release pública para Unity Asset Store).

---

## Estructura del Proyecto

```
Assets/KrostGames/EditorPets/
├── icon.png                           → Icono del package (512x512)
├── LICENSE.md                         → All Rights Reserved
├── CHANGELOG.md                       → Historial de versiones (formato Keep a Changelog)
├── README.md                          → Documentación de usuario
├── EditorPets_Context.md              → Este archivo (documento técnico interno)
│
├── Editor/                            → Scripts del editor (carpeta Editor obligatoria)
│   ├── EditorPets.Editor.asmdef       → Assembly definition (solo Editor)
│   ├── PetData.cs                     → Definición del ScriptableObject de datos de mascota
│   ├── PetDataEditor.cs               → Inspector personalizado con preview animado
│   ├── GlobalPetSettings.cs           → ScriptableObject de configuración global
│   ├── PetController.cs               → Lógica runtime de cada mascota (estados, animación, dibujo)
│   ├── ScenePetOverlay.cs             → Orquestador principal que renderiza en SceneView
│   ├── EditorPetsWindow.cs            → Ventana de configuración (Tools/Editor Pets Settings)
│   └── WelcomeWindow.cs               → Ventana de bienvenida en primera instalación
│
├── Data/                              → Assets de ScriptableObject creados
│   ├── GlobalPetSettings.asset        → Configuración global (texturas, física de pelota)
│   └── Example DataPets/              → Pets de ejemplo pre-creados
│       ├── Corgi.asset                → Mascota Corgi
│       ├── DefaultDog.asset           → Mascota por defecto (perro pixel art)
│       └── Noah Dog.asset             → Mascota Noah Dog
│
├── Example scene/                     → Escena de ejemplo
│   ├── Editor Pets Sample.unity       → Jardín con casa, cerca, árboles y 3 pets
│   └── Materials/                     → Materiales de la escena
│
├── Textures/                          → Texturas e iconos
│   ├── Ball.png                       → Pelota de juego
│   ├── Food.png                       → Comida
│   ├── Heart.png                      → Corazón (partículas)
│   ├── Corgi/                         → Spritesheets del Corgi
│   │   ├── CorgiIdle_pet-Sheet.png
│   │   ├── Corgi Walking_pet 1.png
│   │   ├── Corgi sleep_pet-Sheet.png
│   │   ├── Corgi sleep_pet_0010.png
│   │   └── Corgi Happy_pet.png
│   ├── Legacy Dog/                    → Sprites del perro pixel art procedural
│   │   ├── Dog_Idle.png
│   │   ├── Dog_Walk.png
│   │   └── Dog_Sleep.png
│   └── Noah Dog/                      → Sprites del Noah Dog
│       └── Noah.png
│
└── Marketing/                         → Screenshots y material de marketing
    ├── cover.png                      → 1600x900 banner principal
    ├── feature_01_pets.png            → 1280x720: mascotas en escena
    ├── feature_02_ball.png            → 1280x720: física de la pelota
    ├── feature_03_states.png          → 1280x720: 5 estados de animación
    └── feature_04_settings.png        → 1280x720: settings window
```

---

## Componentes y su Función

### 1. PetData.cs (`Editor/PetData.cs`)

ScriptableObject que define los datos de una mascota.

| Campo | Tipo | Propósito |
|-------|------|-----------|
| `petName` | `string` | Nombre visible de la mascota |
| `isActive` | `bool` | Si está activa en escena |
| `location` | `PetLocation` | `Scene` (visible) o `House` (oculta) |
| `idleTexture`, `walkTexture`, `sleepTexture`, `eatTexture`, `pettedTexture` | `Texture2D` | Spritesheets para cada estado de animación |
| `framesIdle`, `framesWalk`, `framesSleep`, `framesEat`, `framesPetted` | `int` | N° de frames horizontales en cada spritesheet |
| `animationSpeed` | `float` | Velocidad de animación en FPS |
| `moveSpeed` | `float` | Velocidad de movimiento en píxeles/segundo |
| `size` | `Vector2` | Tamaño de dibujo en píxeles |

Se crea desde el menú: `Assets → Create → EditorPets → Pet Data`

### 2. GlobalPetSettings.cs (`Editor/GlobalPetSettings.cs`)

ScriptableObject singleton con configuración global.

| Campo | Tipo | Propósito |
|-------|------|-----------|
| `heartTexture` | `Texture2D` | Textura del corazón (partículas) |
| `foodTexture` | `Texture2D` | Textura de la comida |
| `ballTexture` | `Texture2D` | Textura de la pelota |
| `ballRadius` | `float` | Radio de la pelota (física) |
| `gravity` | `float` | Gravedad de la pelota |

### 3. PetController.cs (`Editor/PetController.cs`)

Controlador runtime de cada mascota. No es un MonoBehaviour — es una clase plana instanciada por `ScenePetOverlay`.

**Máquina de Estados (7 estados):**

| Estado | Comportamiento |
|--------|---------------|
| `Idle` | Quieto, timer aleatorio 3-8s antes de cambiar |
| `Walk` | Camina horizontalmente, rebota en bordes |
| `Sleep` | Duerme 10-20s |
| `Interact` | Se activa al hacer clic, muestra corazón, dura 2s |
| `Drag` | Mientras se arrastra con el mouse |
| `Eat` | Come durante 4s tras recibir comida |
| `Play` | Persigue la pelota |

**Animación:** Spritesheets horizontales. Usa `GUI.DrawTextureWithTexCoords` con coordenadas UV calculadas según `currentFrame % totalFrames`. Soporta volteo horizontal (`facingLeft`).

**Dibujo:** Sombra, nombre (opcional), partículas de corazón, comida durante Eat, y la mascota misma con opacidad global. El `GUIStyle` del nombre se cachea estáticamente para evitar garbage collection, y su color se adapta al skin del editor (light/dark).

**Posición Y:** La posición Y de la mascota se asigna mediante `SnapToFloor(floorY)` desde `OnSceneGUI` por cada Scene View, usando los bounds locales de cada ventana. Esto permite que la mascota se renderice en el "piso" correcto de cada Scene View abierta, independientemente de cuál sea la activa. Solo no se aplica durante el estado `Drag`.

### 4. ScenePetOverlay.cs (`Editor/ScenePetOverlay.cs`)

Clase **estática** con `[InitializeOnLoad]` — el núcleo de la herramienta.

**Suscripciones:**
- `SceneView.duringSceneGui` → Renderizado e input
- `EditorApplication.update` → Física y actualización de estados
- `EditorApplication.delayCall` → Inicialización de mascotas

**Inicialización segura:** `lastUpdateTime` se inicializa en el constructor estático para evitar un deltaTime enorme en el primer frame tras una recompilación.

**Bounds para física:** `EditorUpdate` usa la **primera** `SceneView` abierta (`SceneView.sceneViews[0]`) en lugar de `lastActiveSceneView`, lo que da un comportamiento consistente sin importar en cuál ventana se hizo clic por última vez.

**Flujo de inicio:**
1. `InitializePets()` busca todos los `PetData` assets.
2. Si no hay ninguno, crea el `DefaultDog` (perro pixel art generado proceduralmente).
3. Para cada `PetData` activo en `Scene`, crea un `PetController`.
4. Carga o crea `GlobalPetSettings.asset`.

**Pelota (Ball):** Simulación física 2D completa:
- Gravedad, rebote contra el suelo (coef 0.6), fricción horizontal
- Colisiones con bordes y empuje de mascotas
- Arrastre con el mouse para lanzar (velocidad basada en delta del drag)
- Al agarrar la pelota, todas las mascotas cambian a `Play`
- Posición Y se clampa al piso local de cada SceneView antes del render

**Input:**
- Click en mascota → `Interact` + corazón
- Drag de mascota → si se arrastra fuera de la SceneView, inicia drag & drop del `PetData`
- Drag & drop de `PetData` desde Project Window a SceneView → lo activa en escena
- Click y drag en pelota → la mueve con física de lanzamiento
- Al soltar el drag de una mascota, se hace snap al piso local de la SceneView

**Repaint throttled:** `RequestRepaint(force)` limita el repintado a 30 FPS para reducir overhead. Durante drag de mascota o pelota, se llama con `force: true` para respuesta inmediata.

**Persistencia:** Usa `EditorPrefs` para `interactable`, `showNames`, `globalOpacity`.

**API pública adicional:**
- `RandomizePetPosition(PetData)` — randomiza la X sin destruir el `PetController` (preserva estado).
- `HideAllPets()` — pone `isActive = false` en todos los `PetData` y los remueve de la escena.
- `ShowAllPets()` — pone `isActive = true` y `location = Scene` en todos los `PetData`.

### 5. EditorPetsWindow.cs (`Editor/EditorPetsWindow.cs`)

Ventana del editor accesible desde `Tools → Editor Pets Settings`.

**Toolbar:**
- Toggle "Interactable" (habilita/deshabilita input en SceneView)
- Botón "Spawn Ball" (crea la pelota)
- Botón "Feed All" (todas las mascotas comen)
- Botón "Reload All" (refresca)
- Botón "Hide All" (oculta todas las mascotas)
- Botón "Show All" (muestra todas las mascotas)

**Panel Global:**
- Show Names toggle
- Slider de opacidad global
- Foldout "Global Item Settings" (texturas de food/heart/ball, radio, gravedad)

**Lista de mascotas:** Scroll view con cada `PetData`:
- Preview de thumbnail
- Campo de nombre (renombrado inline)
- Toggle "Show in Scene"
- Botón "Send to House / Bring to Scene"
- Foldout "Advanced Customization": Move Speed, Anim Speed, Draw Size
- Secciones de animación: IDLE, WALK, SLEEP, EAT, PETTED (cada una con textura y frame count)
- Botones "Ping Asset", "Randomize Pos" y "Duplicate"

**Drag & Drop:** Arrastrar un PetData desde la ventana hacia ella misma lo envía a House.

---

### 6. PetDataEditor.cs (`Editor/PetDataEditor.cs`)

Inspector personalizado para `PetData` con `[CustomEditor(typeof(PetData))]`.

**Características:**
- **Preview animado en vivo**: renderiza la textura del estado seleccionado con animación real a la `animationSpeed` del pet.
- **Toolbar de estados**: tabs IDLE / WALK / SLEEP / EAT / PETTED para previsualizar cada spritesheet sin salir del inspector.
- **Slider de frame count**: el rango máximo se ajusta al ancho de la textura (`width / 8`) para evitar valores absurdos.
- **Play/Pause**: botón para congelar la animación.
- **Reset**: vuelve al frame 0.
- **Field groups**: Identity / Movement & Size / Animation States con `EditorStyles.boldLabel`.

Esto reemplaza el inspector por defecto de `PetData` con una experiencia mucho más visual y rápida de configurar.

---

## Dependencias y Namespace

Todas las clases están en el namespace `EditorPets`. Los scripts del editor están envueltos en `#if UNITY_EDITOR` (excepto `PetData.cs` y `GlobalPetSettings.cs` que también pueden usarse desde fuera del editor).

---

## Integración con Unity Editor

- `ScenePetOverlay` se activa automáticamente al cargar el proyecto (gracias a `[InitializeOnLoad]`).
- No requiere configuración manual. Si no hay datos, se autogenera el perro pixel art.
- La ventana `EditorPetsWindow` se abre manualmente desde `Tools → Editor Pets Settings`.
- Las texturas se importan como Sprite, filtro Point, sin compresión.
- Todos los cambios en PetData actualizan la SceneView inmediatamente via `ScenePetOverlay.UpdatePetInstance()`.

---

## Notas Técnicas

- El dibujo se hace con `Handles.BeginGUI()` / `GUI.DrawTexture()` dentro de `SceneView.duringSceneGui`, no con `OnGUI`.
- `PetController` almacena el delta time manualmente desde `EditorApplication.update` (no usa `Time.deltaTime`).
- Delta time está limitado a 0.05s para evitar saltos; además, deltas ≤ 0 se descartan para evitar frames inválidos tras una recompilación.
- Las texturas del DefaultDog se generan proceduralmente con `Texture2D.SetPixel()` y se guardan como PNG en `Assets/KrostGames/EditorPets/Textures/`.
- El soporte para arrastrar mascotas fuera de la SceneView usa `DragAndDrop.StartDrag()`.
- `Undo.RecordObject` se usa en todas las modificaciones de `EditorPetsWindow` para soportar Ctrl+Z.
- El assembly definition `EditorPets.Editor.asmdef` aísla los scripts de editor y restringe la compilación a la plataforma `Editor`, reduciendo tiempos de compilación del proyecto principal.
- `PetController._nameLabelStyle` se cachea estáticamente para evitar crear un `GUIStyle` por cada llamada a `Draw()` (reducción de GC).
- El color del texto del nombre se obtiene de `GUI.skin.label.normal.textColor` cada frame para adaptarse al skin activo (light/dark).
- Las operaciones que modifican la `AssetDatabase` durante `OnGUI` (como `DuplicatePet`) se difieren con `EditorApplication.delayCall` para evitar `InvalidOperationException: Collection was modified` (al mutar `allPets` mientras se itera) y `Invalid GUILayout state` (al disparar reimport durante el render).
