# EditorPets — Contexto de la Herramienta

## Descripción General

**EditorPets** es una herramienta para Unity Editor que añade mascotas virtuales interactivas dentro de la **Scene View**. Las mascotas deambulan, duermen, reaccionan al clic, comen, juegan con una pelota y pueden ser completamente personalizadas mediante ScriptableObjects.

---

## Estructura del Proyecto

```
Assets/KrostGames/EditorPets/
├── Editor/                           → Scripts del editor (carpeta Editor obligatoria)
│   ├── PetData.cs                    → Definición del ScriptableObject de datos de mascota
│   ├── GlobalPetSettings.cs          → ScriptableObject de configuración global
│   ├── PetController.cs              → Lógica runtime de cada mascota (estados, animación, dibujo)
│   ├── ScenePetOverlay.cs           → Orquestador principal que renderiza en SceneView
│   └── EditorPetsWindow.cs          → Ventana de configuración (Tools/Editor Pets Settings)
│
├── Data/                             → Assets de ScriptableObject creados
│   ├── GlobalPetSettings.asset       → Configuración global (texturas, física de pelota)
│   ├── DefaultDog.asset              → Mascota por defecto (perro pixel art)
│   └── Corgi.asset                   → Mascota Corgi
│
├── Sample/
│   └── Editor Pets Sample.unity      → Escena de ejemplo
│
├── Textures/                         → Texturas e iconos
│   ├── Ball.png                      → Pelota de juego
│   ├── Food.png                      → Comida
│   ├── Heart.png                     → Corazón (partículas)
│   ├── Corgi/                        → Spritesheets del Corgi
│   │   ├── CorgiIdle_pet-Sheet.png          → Idle (3 frames)
│   │   ├── Corgi Walking_pet 1.png          → Walk (17 frames)
│   │   ├── Corgi sleep_pet-Sheet.png        → Sleep (4 frames)
│   │   ├── Corgi sleep_pet_0010.png         → Frame individual de sleep
│   │   └── Corgi Happy_pet.png              → Petted/Feliz (1 frame)
│   └── Legacy Dog/                   → Sprites del perro pixel art
│       ├── Dog_Idle.png              → Idle (1 frame, 64x64)
│       ├── Dog_Walk.png              → Walk (2 frames, 128x64)
│       └── Dog_Sleep.png             → Sleep (1 frame, 64x64)
│
└── EditorPets_Context.md             → Este archivo
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

**Dibujo:** Sombra, nombre (opcional), partículas de corazón, comida durante Eat, y la mascota misma con opacidad global.

**Física:** Gravedad simple que mantiene a la mascota en el suelo (`floorY = bounds.height - size.y - 25`). Solo aplica si no está en estado `Drag`.

### 4. ScenePetOverlay.cs (`Editor/ScenePetOverlay.cs`)

Clase **estática** con `[InitializeOnLoad]` — el núcleo de la herramienta.

**Suscripciones:**
- `SceneView.duringSceneGui` → Renderizado e input
- `EditorApplication.update` → Física y actualización de estados
- `EditorApplication.delayCall` → Inicialización de mascotas

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

**Input:**
- Click en mascota → `Interact` + corazón
- Drag de mascota → si se arrastra fuera de la SceneView, inicia drag & drop del `PetData`
- Drag & drop de `PetData` desde Project Window a SceneView → lo activa en escena
- Click y drag en pelota → la mueve con física de lanzamiento

**Persistencia:** Usa `EditorPrefs` para `interactable`, `showNames`, `globalOpacity`.

### 5. EditorPetsWindow.cs (`Editor/EditorPetsWindow.cs`)

Ventana del editor accesible desde `Tools → Editor Pets Settings`.

**Toolbar:**
- Toggle "Interactable" (habilita/deshabilita input en SceneView)
- Botón "Spawn Ball" (crea la pelota)
- Botón "Feed All" (todas las mascotas comen)
- Botón "Reload All" (refresca)

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
- Botones "Ping Asset" y "Randomize Pos"

**Drag & Drop:** Arrastrar un PetData desde la ventana hacia ella misma lo envía a House.

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
- Delta time está limitado a 0.05s para evitar saltos.
- Las texturas del DefaultDog se generan proceduralmente con `Texture2D.SetPixel()` y se guardan como PNG en `Assets/KrostGames/EditorPets/Textures/`.
- El soporte para arrastrar mascotas fuera de la SceneView usa `DragAndDrop.StartDrag()`.
- `Undo.RecordObject` se usa en todas las modificaciones de `EditorPetsWindow` para soportar Ctrl+Z.
