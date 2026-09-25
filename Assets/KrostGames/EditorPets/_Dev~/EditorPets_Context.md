# EditorPets — Contexto técnico

Herramienta de Unity Editor que dibuja mascotas 2D interactivas como overlay de la **Scene View** (no son GameObjects: no existen en la escena 3D ni en builds). Guía de usuario en `README.md`.

---

> Notas internas de desarrollo. Esta carpeta `_Dev~` la ignora Unity, así que no entra en el paquete de la Asset Store (junto con `Marketing/` y `Tools/petgen.mjs`, el generador de sprites).

## Estructura del paquete

```
EditorPets/
├── README.md (EN), LEEME.md (ES), CHANGELOG.md, LICENSE.md, package.json
├── Editor/  EditorPets.Editor.asmdef · Scripts/*.cs · UI/*.uxml,*.uss,icon.png
├── Items/   Ball.png · Heart.png · GlobalPetSettings.asset · Food/*.png (librería de comidas)
├── Pets/<Nombre>/  asset PetData + sus sprite sheets (19 mascotas)
└── _Dev~/   (no se publica) notas, Marketing/, Tools/petgen.mjs
```
Rutas en código: `ScenePetOverlay.Root` (vía GUID del asmdef) + `UIFolder`, `ItemsFolder`, `FoodFolder`.

## Archivos (`Editor/Scripts`, asmdef `EditorPets.Editor`, solo Editor)

| Archivo | Rol |
|---------|-----|
| `PetData.cs` | ScriptableObject de una mascota. `GetAnimation(state)` resuelve sprite sheet + frames. |
| `GlobalPetSettings.cs` | ScriptableObject único: texturas de corazón/comida/pelota, radio y gravedad de la pelota. |
| `PetController.cs` | Estado, movimiento, animación y dibujo IMGUI de una mascota (clase plana). |
| `ScenePetOverlay.cs` | `[InitializeOnLoad]`. Orquesta todo: sync de mascotas, pelota, input, repaint, creación de mascotas. |
| `PetDataEditor.cs` | Inspector UI Toolkit de `PetData`: preview animado (IMGUIContainer) + campos por defecto. |
| `EditorPetsWindow.cs/.uxml/.uss` | Ventana `Tools → Editor Pets → Settings`: pestaña Mascotas (cuadrícula + detalle en `TwoPaneSplitView`) y Ajustes. |
| `PetCardElement.cs/.uxml/.uss` | Tile de la cuadrícula: miniatura animada (IMGUIContainer), nombre, ojo, menú contextual. |
| `WelcomeWindow.cs` | Bienvenida en la primera instalación (`Tools → Editor Pets → Welcome`). |

---

## Decisiones clave

**Una sola fuente de verdad para la visibilidad: `PetData.isActive`.**
`ScenePetOverlay.SyncPets()` corre en cada `EditorApplication.update` y crea/elimina `PetController`s según `isActive`. Así cualquier cambio (ventana, Inspector, Undo, asset nuevo o borrado) se refleja sin llamadas manuales. La lista de assets se refresca en `EditorApplication.projectChanged`.
(Antes existían `isActive` + `location Scene/House` y `UpdatePetInstance()` en cada editor; el Undo no actualizaba la escena.)

**Animaciones con fallback y frames automáticos.**
`PetData.GetAnimation(state)`: si el estado no tiene sheet, usa Idle (con los frames de Idle). Frames `0` = automático = `round(width / height)` (frames cuadrados en una fila). Walk y Play comparten sheet; Interact usa Petted.

**Rutas independientes de la ubicación.**
`ScenePetOverlay.Root` se obtiene del GUID de `EditorPets.Editor.asmdef`, así el paquete funciona en cualquier carpeta de `Assets/` o en `Packages/`. UXML/USS, icono y settings por defecto se cargan desde `Root`.

**GlobalPetSettings.**
`LoadGlobalSettingsAsset()` usa `Items/GlobalPetSettings.asset`; si no existe, cualquier otro del proyecto; si no hay ninguno, lo crea con `Items/Heart.png`, `Items/Ball.png` y `Items/Food/Bowl.png`.

**Comida por mascota.** `PetData.food` (vacío = comida por defecto de GlobalPetSettings). `FeedAll` le pasa a cada `PetController` su comida; se dibuja delante de la boca (75% del ancho según hacia dónde mire). `PetDataEditor.FoodPicker()` inserta bajo el campo `food` una fila de botones con todas las texturas de `Items/Food` (resalta la actual con `TrackPropertyValue`).

**Carga perezosa de la lista de mascotas.**
`allPets` es `null` hasta usarse (`Pets`) y se vuelve a `null` en `projectChanged`. `SyncPets` no la carga mientras `EditorApplication.isUpdating/isCompiling`: justo tras una recarga de dominio `FindAssets` puede devolver 0 y las mascotas desaparecían.

**Ventana para muchas mascotas.**
Un `schedule.Execute(Tick).Every(120)` sondea los tiles (nombre, visible, miniatura animada), el contador y el filtro, así cualquier cambio (Undo, Inspector, Scene View) se refleja sin bindings por tile. La edición se hace en el panel de detalle con `new InspectorElement(pet)` (mismo inspector que en el Inspector de Unity). Selección, búsqueda, filtro y pestaña se serializan en la ventana; el divisor y los scrolls usan `view-data-key`. Los colores usan variables del tema (`--unity-colors-*`) + acento naranja. Textos EN/ES vía `EditorPetsWindow.T(en, es)`.
`ScenePetOverlay.ShowOnly` / `SetAllVisible` agrupan el Undo en un paso; `Duplicate` copia el asset y le añade " Copy" al nombre.

**Sprites de ejemplo nuevos.** Tortuga, serpiente, gato, Mishy (variante gordita del gato) y capibara (32 px), venado y dragones (40 px), cangrejo y palomas (24 px), Clawdito y Kimoon (16 px, a 4×): generados con un script de formas + contorno automático + paletas (las palomas son la misma forma con 5 paletas y variantes de cola/cresta). `size` = 2× el frame para pixel art nítido.

**Crear mascota: `ScenePetOverlay.CreatePetFromSelection()`**
Menú `Assets/Create/EditorPets/New Pet` y botón **+ New Pet**. Toma las `Texture2D` seleccionadas, las asigna por palabra en el nombre (`walk|run`, `sleep`, `eat`, `happy|petted`, resto → Idle), nombra la mascota con la primera palabra del archivo y guarda el asset junto a las texturas. Texturas con import `Default` se pasan a Sprite / Point / sin compresión (evita el escalado a potencia de 2 que rompe el conteo de frames); las ya configuradas no se tocan.

---

## Simulación

- Delta time manual desde `EditorApplication.timeSinceStartup`, limitado a 0.05 s; deltas ≤ 0 se descartan.
- Física en X con los bounds de `SceneView.sceneViews[0]`; la Y se ajusta al piso de cada Scene View en `OnSceneGUI` (`SnapToFloor`), salvo en `Drag`.
- Pelota: gravedad, rebote 0.6, fricción, paredes, empuje suave de mascotas. Al agarrarla todas pasan a `Play`.
- Repaint limitado a 30 FPS; forzado durante drag/interacción.
- Arrastrar una mascota al borde de la Scene View inicia `DragAndDrop` del asset; soltarlo en la ventana la oculta. Soltar un `PetData` en la Scene View la muestra.
- Preferencias por usuario en `EditorPrefs`: `EditorPets_Interactable`, `EditorPets_ShowNames`, `EditorPets_Opacity`, `EditorPets_IsEnglish`, `EditorPets_Welcomed_v1`.

| Estado | Comportamiento |
|--------|---------------|
| `Idle` | Quieto 3–8 s |
| `Walk` | Camina, rebota en bordes |
| `Sleep` | 10–20 s |
| `Interact` | 2 s tras clic, salta con corazón |
| `Drag` | Mientras se arrastra |
| `Eat` | 4 s tras Feed All |
| `Play` | Persigue la pelota |
