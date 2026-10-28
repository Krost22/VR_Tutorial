# AGENTS.md — VR_Tutorial

Contexto para agentes de IA (Claude Code, Codex, etc.) que trabajen en este repositorio.
Última actualización: 2026-10-28. El historial detallado de cambios está en [Docs/CAMBIOS_GameFeel.md](Docs/CAMBIOS_GameFeel.md).

## Qué es

Juego VR para Meta Quest (Unity) que enseña a un jugador novato a moverse e interactuar con las manos y luego lo lleva a una mazmorra con puzzles. El tutorial termina conectado a la primera sala de la mazmorra.

- **Unity 6000.4.2f1**, URP 17.4, OpenXR, XR Interaction Toolkit 3.4 + XR Hands.
- **AutoHand** (`Assets/AutoHand`) para manos físicas y agarre: `Grabbable`, `Hand`, `PlacePoint`, `DispenserPoint`, `PhysicsGadgetButton`, `Smash`.
- Iluminación horneada con el lightmapper de Unity (Progressive GPU). Bakery se usó antes y ya no está en el proyecto.
- Arte de la mazmorra: `Assets/DungeonModularPack`.
- Idioma del proyecto: español (textos del juego, comentarios, commits y documentación).

## Escenas (`Assets/_Custom/Scenes`)

| Escena | Build | Qué es |
| --- | --- | --- |
| `3_TutorialMazmorra` | **Índice 0 (inicio)** | Escena **generada**: tutorial + sala de runas pegados, sin laberinto. No editar a mano. |
| `2_Adaptacion` | Índice 1 | Tutorial original. Termina en un portal que carga `1_GAME`. |
| `1_GAME` | Índice 2 | Mazmorra: sala de runas + Sección 2 (laberinto, en construcción, se ve apagada). |
| `0_TUTORIAL` | Desactivada | Versión antigua del tutorial. |
| `DemoAutoHand` | No | Escena de pruebas de AutoHand. |

### Flujo del tutorial (`2_Adaptacion`)

1. **Mirada guiada** (`GazeSequenceTutorial`, objeto `Fase1_Mirada`, al estilo Halo):
   - Se enciende una luz cada vez: frente, arriba, abajo, izquierda y derecha.
   - Hay que mirarla fijamente 0,7 s para cargarla.
   - Cada luz vuela a su gema y apaga un haz de la barrera; con las 5, la barrera se hunde y se desbloquea el movimiento.
   - Después vienen los círculos de teletransporte en cadena (`MovementTutorial`).
   - El viejo `LookDirectionTutorial` queda desactivado en la escena.
2. **Interacción:**
   - **Bandera de Colombia** (`Puzzle_Bandera`, `FlagPuzzle` + `LaserBeamSet`): tablero 4×3 con 6 huecos. Cada fila completa apaga un haz de la barrera azul; un cubo equivocado sale despedido, y tras 2 errores aparece una pista.
   - **Martillo y arañas:** aplastar arañas con el martillo (`FeelSmashHaptics`, contador flotante).
   - **Canasta** (`HoopDetector`): el aro brilla mientras se sostiene el balón (`HoldTargetHighlight`), y encestar apaga el láser rojo.
3. **Golem:** se conduce por una pista hasta `Meta_Golem` (`FinishDetector` → `TutorialFinale`). Recoge la runa `Recompensa_Runa`, que vuela hasta la pared `Enviaroment/Wall_A (11)`. La pared se hunde y abre `Pasillo_Mazmorra`.

### Sala de runas (`1_GAME`, `Assets/_Custom/Scripts/RunePuzzle`)

En la combinada, al resolver la fase 2 aparece un portal en la puerta principal. Al cruzarlo se ve "¡Gracias por jugar!" y el juego se reinicia.

- Se colocan runas en sus altares y se activan los pilares (`PillarProjector`).
- Cada pilar proyecta un rayo de color que debe apuntar a su mural del mismo color; se fija al llegar a `PerfectRotationY`.
- `PuzzleRoomManager` lleva dos fases:
  - Fase 1: 2 pilares abren un compartimento secreto.
  - Fase 2: los 4 pilares abren la puerta principal hacia la Sección 2.

## Código

- `Assets/_Custom/Scripts/` — scripts del juego por zona:
  - `Movement-Tutorial`
  - `Bandera de colombia`
  - `Basketball`
  - `Carro`: golem y meta
  - `RunePuzzle`
  - `Labyrinth`: Sección 2
  - `Exploration`: antorchas y cinturón
  - `GameFeel`
- `Assets/_Custom/Scripts/GameFeel/` (namespace `VRTutorial.GameFeel`) — kit de game feel reutilizable:
  - **Utilidades:** `FeelAudio`, `FeelHaptics`, `FeelTween`.
  - **Feedback y efectos:** `FeelBurst`, `FeelDisappear`, `FeelPopup`, `FeelGlow`, `FeelIdleMotion`, `FeelMotionBob`, `FeelImpact`, `FeelFloatingText`.
  - **Mecánicas del tutorial:** `FlagPuzzle`, `LaserBeamSet`, `HoldTargetHighlight`, `TutorialFinale`, `ScenePortal`, `SceneFader`, `PillarChargeFeel`, `FeelSmashHaptics`.
  - **Atajo de prueba:** `TutorialSkip` (objeto `GameFeel/SaltarTutorial`). Mantener **A** 2 s (o **K** en el Editor) salta el tutorial y deja al jugador frente al pasillo de la mazmorra, con la pared abierta.
  - **Final de la mazmorra:** `EndPortal` (portal en la puerta principal → sala "¡Gracias por jugar!" → reinicio), `RuneAltarGlow` (brillos del altar) y `FeelLoopRandomStart` (bucles de audio desincronizados).
  - **Variantes de sonido:** `FeelSoundBank` (`Resources/FeelSoundBank.asset`); `FeelAudio` alterna tomas y evita que un mismo sonido se apile.
- `Assets/_Custom/Scripts/GameFeel/Editor/` — herramientas del menú **Tools > Game Feel**:
  - `GameFeelAssetBuilder` y `GameFeelAssetBuilder2`: crean texturas, materiales, prefabs de partículas, el fuego de antorchas y las mallas de los haces.
  - `GameFeelAudioBuilder`: **Regenerar sonidos**. Sonidos por capas, sonoridad igualada por categoría (tipo LUFS), techo de -1 dBFS y variantes. Ver "Sonidos rehechos" en `Docs/CAMBIOS_GameFeel.md`.
  - `DungeonAccessBuilder`: **Pasillo de la mazmorra: colliders y atajo de salto**, sobre `2_Adaptacion`. La combinada añade los colliders de las columnas de la entrada.
  - `CombinedSceneBuilder`: **Construir escena combinada**. Al final llama a `DungeonFinaleBuilder`, que abre la entrada de la mazmorra, ajusta su audio, crea el altar nuevo (`Altar_Runas`), el portal final (`Portal_Final`) y la sala `Sala_Final`.
  - `GazePhaseBuilder`: **Construir fase 1 (mirada guiada)**.
  - `TitlePortalBuilder`: **Construir portal del título**, el escenario 3D con el título "LSV-INMERSIVE 360" en la pared de inicio. Usa la fuente Cinzel Decorative (OFL) de `Assets/_Custom/Fonts/Cinzel`.
  - `MeshyPortalBuilder`: **Colocar logo de Meshy en el portal**. Pone el relieve de Meshy (`Assets/_Custom/Models/PortalMeshy`) dentro del arco y apaga la placa hecha a mano (`Portal_Titulo/Titulo`).
- `Assets/_Custom/Scripts/Tools/EditorVideoCapture.cs` — solo para el Editor:
  - `EditorVideoCapture` graba un video de prueba en Play mode (JPG a 30 fps más WAV, que luego se unen con ffmpeg).
  - `GazeTutorialAutopilot` mueve la cabeza sin visor en la fase 1.
  - `TutorialAutopilot` (`Tools/TutorialAutopilot.cs`) juega el tutorial completo, hasta que se abre la pared hacia la mazmorra. Para grabarlo, pasar `poseProvider = () => ap.HeadPose` a `EditorVideoCapture`.
- `Assets/_Custom/GameFeel/{Audio,Materials,Meshes,Prefabs,Textures}` — recursos generados por esas herramientas.

## Reglas de trabajo

- **Escenas originales intocables al combinar:** `3_TutorialMazmorra` se regenera desde el menú cada vez que cambien `2_Adaptacion` o `1_GAME`.
  - El generador agrupa la mazmorra en `Mazmorra`, la gira 90°, la sube 5,34 m y suma 90° a cada `PerfectRotationY`.
  - Además quita el jugador de `1_GAME`, `LabyrinthManager`, la Sección 2 y el `PostProcesado` duplicado.
  - **Después de regenerarla hay que volver a hornearla:** **Tools > Iluminación Quest > Hornear escena combinada**, que tarda menos de 1 minuto.
- **Iluminación:**
  - **Ya no se usa Bakery** (se quitó del proyecto). Todo se hornea con el lightmapper de Unity (Progressive GPU) usando los ajustes propios `Assets/_Custom/Lighting/Horneado_Quest.lighting`: 40 texels/unidad, máximo 2048, sin mapas direccionales, 2 rebotes, AO suave.
  - Las luces de antorcha están encendidas en modo **Baked**: solo existen en el lightmap y no cuestan en tiempo real.
  - Las antorchas de la mazmorra solo tenían luz de Bakery; al hornear, la herramienta les pone una luz de Unity equivalente.
  - La preparación también quita los componentes rotos de Bakery y su almacén `!ftraceLightmaps`.
  - Los objetos que se mueven reciben luz de las sondas (`SondasDeLuz`).
  - No hay sombras en tiempo real: el pipeline de Android las tiene desactivadas. Ese pipeline (`High_PipelineAsset`) usa MSAA 4x.
  - El postproceso es un solo perfil (`Assets/_Custom/Lighting/PostProceso_Quest.asset`): color y viñeta, con el bloom apagado. La cámara del jugador dibuja hasta 100 m y tiene fondo casi negro.
  - La herramienta está en `Assets/_Custom/Scripts/Lighting/Editor/QuestLighting.cs`.
  - El horneado es asíncrono: consultar `Lightmapping.isRunning` cada cierto tiempo.
  - `1_GAME` todavía tiene 10 luces rotas de Bakery y su almacén, y sus lightmaps viejos (`Assets/BakeryLightmaps/1_GAME_*`) ya no se aplican. Solo la combinada (la escena de inicio) está horneada con Unity.
- **Commits pequeños en `main`**, mensajes en español, uno por tema, para poder revertir por partes. No hacer push sin que el usuario lo pida.
- **Documentar cada tanda** en `Docs/CAMBIOS_GameFeel.md`: qué cambió, commit y cómo deshacerlo.
- **Efectos opcionales:** cada efecto se apaga quitando su componente o vaciando su campo; el script debe volver a su comportamiento original. Mantener ese patrón.
- **VR y confort:** nunca mover ni sacudir la cámara del jugador. Usar vibración, sacudir objetos y fundidos.
- **Rendimiento (Quest):**
  - Partículas en espacio local, con procedural culling.
  - Materiales compartidos.
  - Sin luces por efecto si se puede evitar.
- Archivos `.cs` en **UTF-8** (algunos scripts viejos estaban en Windows-1252 y los acentos se rompían).
- No incluir en commits los cambios automáticos de Unity en `LiberationSans SDF.asset` ni en `Template Lighting Settings.lighting`: revertirlos con `git checkout`.
- Hay cambios del usuario sin commitear en `Assets/KrostGames/` y `Assets/EditorPets*`: no tocarlos ni incluirlos.

## Controlar el Editor desde un agente

- El Editor abierto se maneja con la **Unity CLI** y `com.unity.pipeline`: `unity status` y `unity command eval_file <archivo.cs> --timeout N`. El puente unityMCP no ve instancias en este proyecto.
- Escribir el C# en un archivo, porque el `eval` en línea se rompe con las comillas.
- Cada eval tiene unos 5 s de hilo principal: dividir el trabajo largo en varios evals. `EditorApplication.delayCall` no se dispara con el Editor en segundo plano.
- Si todos los evals fallan con "Main thread operation timed out", probablemente Unity está minimizado: hay que traer la ventana al frente.
- **Pruebas en Play mode:**
  - Simular las acciones por script.
  - Proteger cada script con `if (!EditorApplication.isPlaying) return`.
  - Sin visor la cámara XR queda en y=0: moverla y desactivar su `TrackedPoseDriver`.
- **Capturas:** cámara temporal `HideAndDontSave` con `UniversalAdditionalCameraData`, `Render()` a una RenderTexture, y luego `ReadPixels` y `EncodeToPNG`.
- `GameObject.Find` no encuentra objetos inactivos: usar `Resources.FindObjectsOfTypeAll<Transform>()`.
- Al guardar, a veces la escena queda marcada como modificada sin estarlo: volver a guardar o recargar antes de entrar en Play mode.

## Estado actual

- **Hecho:**
  - Kit de game feel aplicado en el tutorial y la sala de runas.
  - Final del golem con runa y pared que se abre.
  - Puzzle de bandera rediseñado.
  - Fuego en las antorchas (27 en la combinada).
  - Escena combinada como inicio del build.
  - Escena combinada horneada con el lightmapper de Unity (2 lightmaps de 2048 más uno pequeño, 639 sondas de luz) y con postproceso ligero.
- **Pendiente de verificar con visor:**
  - Intensidad de la vibración y volúmenes.
  - Ergonomía del tablero vertical de la bandera y tamaño de letreros.
  - Rendimiento real del fuego en el Profiler del Quest.
  - Costo real del postproceso en el Quest y brillo general tras el horneado.
- **En construcción:** Sección 2 de `1_GAME` (laberinto con gravedad, pasillo de gravedad cero, pedestal de cristal). Hoy no está en la escena combinada: en la puerta principal de la sala de runas está el portal final.
- **Problemas conocidos sin corregir:**
  - Los cubos amarillos de la bandera llevan la etiqueta `BasketBall`.
  - `MovementTutorial` usa el primer `AudioSource` de la escena.
  - AutoHand avisa "Some objects were not cleaned up" al cambiar de escena.
- **Arreglo de AutoHand en Unity 6:** las ventanas fantasma ya están corregidas; ver [BUGFIX_AUTOHAND_GHOST_WINDOWS.md](BUGFIX_AUTOHAND_GHOST_WINDOWS.md).

## Otros contenidos del repo (no son el juego)

`Assets/KrostGames/` (EditorPets, LocalTTS, ClipLoop) y `Assets/EditorPets*` son paquetes de Asset Store del usuario (publisher KrostGames) que se desarrollan en este mismo proyecto. Son independientes del juego VR.
