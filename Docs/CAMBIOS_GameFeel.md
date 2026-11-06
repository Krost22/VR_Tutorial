# Cambios: pendientes y game feel (2026-10-26)

Registro de todo lo que se cambió en el tutorial (`2_Adaptacion`) y en la sala de runas (`1_GAME`), con cómo deshacer o ajustar cada cosa.
Todo está en la rama `main`, en commits separados.

## Commits

| Commit | Qué hace | Cómo deshacerlo |
| --- | --- | --- |
| `43ba9f9` | Kit base: scripts `GameFeel`, sonidos, materiales y partículas. Por sí solo no cambia nada en las escenas. | `git revert 43ba9f9` (solo después de revertir los commits de abajo que lo usan) |
| `53aae94` | Ganchos opcionales en scripts existentes (tutorial y pilar). | `git revert 53aae94` |
| `24600f5` | Los 5 pendientes: meta, antorcha final, conexión de escenas, murales, textos. | `git revert 24600f5` |
| `a1ba880` | Game feel aplicado en las dos escenas. | `git revert a1ba880` |
| `ca100d2` | Correcciones encontradas al probar (círculos, video del aviso final, brillo de murales, llama). | `git revert ca100d2` |
| `f27365d` | Fundido de entrada que no se salta tras cargar. | `git revert f27365d` |

Para quitar **todo**: `git revert f27365d ca100d2 a1ba880 24600f5 53aae94 43ba9f9` (en ese orden, del más nuevo al más viejo).
Las escenas son archivos grandes: si se revierte un commit intermedio y git marca conflicto en una `.unity`, es más seguro revertir también los commits posteriores.

## Ajustar sin revertir

Casi todo se puede apagar desde el Inspector, sin tocar código:

- **Quitar un efecto concreto:** borrar el componente (`FeelDisappear`, `FeelPopup`, `FeelIdleMotion`, `FeelMotionBob`, `FeelImpact`, `FeelGlow`, `PillarChargeFeel`, `FeelSmashHaptics`) o vaciar el campo que lo usa (por ejemplo `HoopDetector > Score Feedback`). Los scripts vuelven a su comportamiento original: si un muro no tiene `FeelDisappear`, se apaga al instante como antes.
- **Ajustar intensidad:** cada `FeelBurst` (objetos `Feel_*` dentro del objeto `GameFeel` de cada escena) tiene volumen, partículas, destello, vibración y rebote. Poner en 0 o dejar vacío lo que no se quiera.
- **Toda la vibración añadida:** `FeelHaptics.Enabled = false` desde código.
- **Regenerar los sonidos:** menú `Tools > Game Feel > Regenerar recursos` (se sintetizan; los parámetros están en `GameFeelAssetBuilder.cs`).

## Pendientes resueltos

| Pendiente | Qué se hizo | Dónde |
| --- | --- | --- |
| Objeto final del golem | Antorcha flotante después de la meta (`Recompensa_Antorcha`), apagada con brillo azul que pulsa. Al llegar el golem se enciende, hay celebración y vuela hasta el golem. Es la antorcha que el jugador lleva al cinturón en la mazmorra. Para cambiar el objeto: reemplazar `Recompensa_Antorcha` y asignarlo en `GameFeel/TutorialFinale > Reward`. | `2_Adaptacion` |
| Activar la meta | `StartandFinishLine` activado. Su zona de detección empezaba a 1 m del suelo y el golem no la tocaba; ahora cubre del suelo a 2,5 m. `FinishDetector` dispara una sola vez y llama a `TutorialFinale`. | `2_Adaptacion` |
| Conectar tutorial y mazmorra | 7 s después de la meta: fundido a negro y carga de `1_GAME`, que empieza con fundido de entrada. Build: `2_Adaptacion` (índice 0) y `1_GAME` (índice 1) activas; `0_TUTORIAL` sigue desactivada. | `TutorialFinale`, `SceneFader`, Build Settings |
| Color de los murales | Materiales nuevos `Mural_Agua` (turquesa), `Mural_Tierra` (amarillo) y `Mural_Viento` (naranja), con brillo suave propio. El de Fuego no se tocó. | `Assets/_Custom/GameFeel/Materials` |
| Texto del aviso de la bandera | "¡Bandera completa! El láser azul se apagó". También: canasta "El láser rojo se apagó" y aviso final "¡Has completado el tutorial!..." (antes decía "Haz"). | `2_Adaptacion` |

## Las 12 técnicas de game feel aplicadas

Adaptadas a VR: Meta desaconseja mover o sacudir la cámara del jugador porque marea, así que la sacudida de pantalla y la congelación de imagen se reemplazan por vibración y por sacudir los objetos.

| # | Técnica | Dónde se aplicó |
| --- | --- | --- |
| 1 | Sonido en cada acción, con variación de tono | Pilares, círculos, piezas, canasta, martillo, runas, muros, puerta |
| 2 | Vibración sincronizada con sonido e imagen | Todas las acciones; patrón "ta-tán" en logros y doble pulso en errores |
| 3 | Partículas puntuales | Magia (pilares, runas), chispas (láseres, pilar fijado), confeti (logros), polvo (muros, golem) |
| 4 | Animación con suavizado (nada aparece o desaparece de golpe) | Pilares de mirada, círculos, muros láser, muro secreto, puerta |
| 5 | Rebote / squash & stretch | Pilar mirado, aro de la canasta, antorcha al llegar al golem, avisos |
| 6 | Destellos de luz y color | Pilar mirado, canasta, bandera, runa colocada, pilar fijado, final |
| 7 | Anticipación y carga | Al apuntar un pilar: zumbido que sube, mural que se ilumina y vibración cada vez más rápida durante los 2 s; los muros tiemblan antes de irse |
| 8 | Progresión musical | Cada pilar de mirada, círculo y pieza de la bandera suena una nota más alta |
| 9 | Indicadores de estado permanentes | Los murales quedan encendidos al fijar su pilar |
| 10 | Guiar la atención sin mover la cámara | Antorcha que flota y pulsa en la meta; círculo siguiente aparece con rebote; polvo y luz donde se abre algo |
| 11 | Movimiento secundario | Golem da pasos con balanceo y sonido; antorcha flota y gira |
| 12 | Sacudir objetos en vez de cámara + transiciones con fundido | Muros y golem tiemblan; cambio de escena con fundido a negro |

Fuentes: charla "Juice it or lose it" (Jonasson y Purho, GDC 2012); *Game Feel* (Steve Swink, 2008); "Designing Game Feel. A Survey" (Pichlmair y Johansen, 2020, [arXiv](https://arxiv.org/abs/2011.09201)); guías de háptica y confort de Meta ([hápticos](https://developers.meta.com/vr/design/haptics-overview/), [buenas prácticas](https://developers.meta.com/horizon/design/bp-overview/)).

## Correcciones encontradas al probar

- **Círculos de teletransporte:** la cadena se cortaba en el segundo círculo porque su `Start()` lo volvía a apagar al activarse (solo el primero estaba marcado como inicial). Por eso el último círculo nunca apagaba la barrera láser. Ahora el círculo anterior marca al siguiente antes de activarlo.
- **Aviso final del golem:** su control de video (VR Template) no tenía slider ni botones asignados y lanzaba `NullReferenceException` cada frame en cuanto aparecía (antes nunca aparecía porque la meta estaba apagada). Se desactivó ese control; el video se reproduce solo en bucle.
- **`LookDirectionTutorial`:** al completar, repetía todo el bloque de cierre en cada frame; ahora corre una sola vez.
- Scripts `MovementTutorial`, `HoopDetector`, `FinishDetector` y `ColombiaFlagBuilder` pasaron de Windows-1252 a UTF-8 (los acentos se veían como `�`).

## Cómo se probó

En el Editor, en Play mode, simulando las acciones por script (mirar cada pilar, pisar los 4 círculos, completar la bandera, aplastar, encestar, reiniciar y llevar el golem a la meta; colocar runas correctas e incorrecta y apuntar los 4 pilares). Se verificó: cada fase desbloquea la siguiente, la carga de `1_GAME`, el muro secreto, la puerta principal y la consola sin excepciones.

**No se probó con visor:** la intensidad real de la vibración, el volumen de los sonidos y la sensación de los efectos en VR. Conviene una pasada con el visor y ajustar los `FeelBurst` a gusto.

## Observaciones sin cambiar

- Los cubos amarillos de la bandera (`Amarillo(Clone)`) tienen la etiqueta `BasketBall`, así que encestar un cubo amarillo también cuenta como canasta.
- `MovementTutorial` reproduce su sonido en el primer `AudioSource` que encuentra en la escena (`FindObjectOfType`), que puede ser cualquiera.
- Al cambiar de escena, AutoHand deja un aviso "Some objects were not cleaned up" por su objeto `Camera Canvas Pointer`; no viene de estos cambios.

## Archivos

- Nuevos: `Assets/_Custom/Scripts/GameFeel/*` (14 scripts + generador en `Editor/`), `Assets/_Custom/GameFeel/{Audio,Materials,Prefabs,Textures}`.
- Modificados: `LookDirectionTutorial.cs`, `MovementTutorial.cs`, `ColombiaFlagBuilder.cs`, `HoopDetector.cs`, `FinishDetector.cs`, `PillarProjector.cs` (solo dos propiedades de lectura), `2_Adaptacion.unity`, `1_GAME.unity`, `ProjectSettings/EditorBuildSettings.asset`.

---

# Segunda tanda (2026-10-27)

## Commits

| Commit | Qué hace | Cómo deshacerlo |
| --- | --- | --- |
| `951a3ff` | Scripts y recursos nuevos (puzzle de bandera, haces láser, brillo del aro, arañas, portal, fuego de antorcha, constructor de escena combinada). | `git revert 951a3ff` (después de los de abajo) |
| `b93002a` | Tutorial (`2_Adaptacion`): runa final, pared que se abre, pasillo, bandera nueva, aro, arañas, fuego. | `git revert b93002a` |
| `d9fa8b1` | `1_GAME`: fuego en las antorchas de la sala de runas. | `git revert d9fa8b1` |
| `e121110` | Escena nueva `3_TutorialMazmorra` y su entrada en Build Settings. | `git revert e121110` o borrar la escena |

Para quitar solo esta tanda: `git revert e121110 d9fa8b1 b93002a 951a3ff`.

## Qué cambió

| Pedido | Resultado |
| --- | --- |
| Quitar el modelo de la meta | El arco "Finish" se eliminó; su zona de detección vive en `Meta_Golem`, alrededor de la runa. |
| Objeto final: runa u orbe | `Recompensa_Runa`: modelo `Rune01` con la gema azul `FragmentoRuna_Mat 2`, luz y aura de partículas, flotando y girando. El golem la recoge al llegar. |
| Abrir la parte marcada | La pared marcada es `Enviaroment/Wall_A (11)`. La runa vuela en arco desde el golem hasta esa pared, que tiembla y se hunde con polvo y estruendo. La runa queda flotando en la entrada como guía. |
| Paso a la mazmorra | Detrás hay un pasillo de 4 m (`Pasillo_Mazmorra`). En `2_Adaptacion` termina en un portal con arco que hace fundido y carga `1_GAME` al entrar (`Pasillo_FondoTemporal`). |
| Escena con las dos pegadas | `3_TutorialMazmorra.unity`, generada con **Tools > Game Feel > Construir escena combinada**. Las escenas originales no se tocan al generarla: se vuelve a generar cuando cambien. Ver detalles abajo. |
| Fuego en las antorchas | `Feel_TorchFire` en las 18 antorchas del tutorial (incluye las 2 del pasillo) y las 13 de la sala de runas. Herramienta: **Tools > Game Feel > Añadir fuego a las antorchas de la escena**. |
| Aro iluminado al sostener el balón | `GameFeel/BrilloAro`: halo dorado sobre el borde superior del aro y una luz, solo mientras una mano sostiene el balón (`HoldTargetHighlight`). |
| Feedback al aplastar arañas | Salpicadura verde, "squish" con tono que sube, destello, vibración y contador flotante "Arañas 3/13" (`FeelSmashHaptics`, objeto `GameFeel/Feel_Arana`). Los tomates mantienen su efecto original. |
| Puzzle de cubos más desafiante e intuitivo | Rediseño completo, abajo. |

## Puzzle de la bandera: diseño

Antes: bandera de 3×3 sobre la mesa, 3 huecos, cada uno aceptaba solo su color y nada indicaba qué abría.

Ahora (`Puzzle_Bandera`, junto a la barrera azul):

- **Más desafío:** tablero de 4 filas × 3 columnas con la proporción real de Colombia (2 filas amarillas, 1 azul, 1 roja). Hay 6 huecos repartidos en las 4 filas; los huecos aceptan cualquier color, así que hay que deducir cuál va en cada uno.
- **Mapeo directo (Norman):** el tablero está pegado a la barrera azul y un cable luminoso lo une al poste del láser. Lo que armas es lo que abre el paso.
- **Estado visible (Nielsen):** la barrera azul se separó en sus 4 haces. Cada fila completa apaga el haz de su misma altura, de arriba hacia abajo.
- **Mostrar, no explicar:** las 6 celdas ya puestas enseñan el patrón; los huecos vacíos laten en blanco. Un solo letrero corto: "Completa la bandera para apagar el láser azul".
- **Error sin castigo:** un cubo del color equivocado suena a error, destella en rojo, vibra dos veces y sale despedido suavemente hacia el jugador para volver a intentarlo.
- **Pista progresiva:** tras 2 errores en un mismo hueco, su marco se tiñe del color que pide. Quien ya entiende no la ve; quien nunca usó VR no se queda atascado.
- Los dispensadores de la mesa ahora guardan hasta 8 copias (antes 3: un cubo ya colocado podía reciclarse).
- El montaje anterior (huecos y cubos guía) quedó **desactivado, no borrado**; `ColombiaFlagBuilder` también está desactivado.

Fuentes consultadas: guías de onboarding de Meta ([hands onboarding](https://developers.meta.com/horizon/design/hands-onboarding-best-practices/)), charla sobre pruebas con usuarios novatos de VR ([sessionize](https://sessionize.com/s/joo-hyun-lee/your-vr-tutorial-will-be-skipped-onboarding-lesson/185361)), "show don't tell" en diseño de niveles ([Game Developer](https://www.gamedeveloper.com/design/level-design-tricks-of-the-trade)) y heurísticas de usabilidad aplicadas a juegos ([UX Collective](https://uxdesign.cc/usability-heuristics-in-game-design-29a324177d4e)).

## Escena combinada `3_TutorialMazmorra`

- Contiene todo el tutorial y la sala de runas de `1_GAME`, **sin la Sección 2 (laberinto)** ni el jugador propio de `1_GAME`.
- La mazmorra se agrupa en `Mazmorra`, se gira 90° y se sube 5,34 m para que su puerta (`Wall_B_0`) quede al final del pasillo del tutorial.
- Como los pilares se fijan a un ángulo absoluto del mundo, a cada `PerfectRotationY` se le suma 90° (probado: los 4 pilares apuntan a su mural y se fijan).
- La iluminación horneada de Bakery se conserva tras el giro (se ve igual que en `1_GAME`).
- La puerta principal de la sala (hacia la Sección 2) se sigue abriendo al final, pero detrás ya no hay laberinto.
- Es la primera escena de Build Settings (índice 0): el juego empieza en ella. `2_Adaptacion` y `1_GAME` siguen activas detrás (el portal del tutorial suelto carga `1_GAME`). Regenerarla no cambia este orden. Para volver a empezar en el tutorial suelto: `git revert` del commit "Build: la escena combinada es la primera" o arrastrar `2_Adaptacion` arriba en Build Settings.
- **Importante:** si cambias `2_Adaptacion` o `1_GAME`, vuelve a ejecutar el menú para regenerar la combinada (sobrescribe `3_TutorialMazmorra.unity`).

## Rendimiento del fuego

- 27 fuegos activos en la escena combinada, con ~370 partículas vivas en total (máximo 16 por antorcha).
- Un solo material aditivo compartido, sin luces, sombras, ruido ni colisiones.
- Simulación en espacio local para que Unity no los simule cuando no se ven ("procedural culling" activo, verificado).
- En un render de prueba a 1440×1600 mirando el pasillo largo, la diferencia con y sin fuego quedó dentro del ruido de medición (≈0,1–0,4 ms). Conviene confirmarlo en el visor con el Profiler.

## Cómo se probó

En Play mode, simulando por script: cubo equivocado (rechazo y pista tras 2 errores), los 6 cubos correctos (los 4 haces se apagan y la barrera se retira), balón sostenido (brillo del aro), arañas (salpicadura y contador), llegada del golem (la runa vuela, la pared se abre), portal (carga `1_GAME`) y, en la escena combinada, el final del golem y las dos fases del puzzle de runas. Consola sin errores.

**No probado con visor:** cómo se siente colocar cubos en el tablero vertical, el tamaño del letrero y los volúmenes.

## Observaciones

- El modelo del cubo de la bandera tiene el pivote en la base; cada hueco tiene un `PuntoColocado` 8 cm abajo para centrarlo.
- Unity cambió solo `LiberationSans SDF.asset` (atlas dinámico de TMP) y `Template Lighting Settings.lighting` (desactiva GI en tiempo real al abrir escenas en Unity 6). No se incluyeron en los commits.

---

# Iluminación horneada y postproceso para Quest 2 (2026-10-27)

## Commits

| Commit | Qué hace | Cómo deshacerlo |
| --- | --- | --- |
| `f8a10b0` | Herramienta `QuestLighting` (menú **Tools > Iluminación Quest**), perfil `PostProceso_Quest` y pipeline de Android sin sombras en tiempo real. El generador de la escena combinada quita el postproceso duplicado de `1_GAME`. | `git revert f8a10b0` (después de los de abajo) |
| `c23170d` | Volumen global `PostProcesado` y postproceso activado en la cámara del jugador, en `2_Adaptacion` y `1_GAME`. | `git revert c23170d`, o borrar el objeto `PostProcesado` de la escena |
| `dcab671` | `3_TutorialMazmorra` regenerada y horneada con Bakery: 3 lightmaps de 2048 y 624 sondas de luz. | `git revert dcab671` |

Para quitar todo: `git revert dcab671 c23170d f8a10b0`.

## Horneado (solo la escena combinada)

Es la escena con la que empieza el juego, así que es la que se horneó. Las originales conservan su horneado anterior.

- **Antes:** el tutorial usaba un horneado del lightmapper de Unity de abril, anterior al pasillo, la bandera y la runa. La mazmorra usaba el de Bakery de `1_GAME`.
- **Ahora:** las dos partes salen de un solo horneado con Bakery, en modo *Full lighting*: todas las antorchas están horneadas y ninguna cuesta en tiempo real.
- **Luces:**
  - Las 18 antorchas del tutorial tienen ahora su `BakeryPointLight`, con el doble de intensidad que la luz de Unity para verse parecidas.
  - Las luces de Unity de esas antorchas quedan apagadas para que no se dibujen en tiempo real.
  - La luz direccional sigue en tiempo real, pero sin sombras.
  - El brillo de la runa y el del aro siguen en tiempo real: no tienen sombras y son pocos.
- **Sondas de luz (`SondasDeLuz`):** 624 puntos cada 2 m, a 0,4, 1,4 y 2,6 m del suelo. Iluminan lo que se mueve (manos, cubos, balón, golem, runas) con el color de las antorchas; antes solo recibía la luz ambiente.
- **Transparentes fuera del horneado:** 3 objetos, entre ellos la pared semitransparente que desaparece, para que no dejen una sombra horneada.
- **Ajustes de Bakery:**
  - 30 texels por metro (la mazmorra tenía 50).
  - Lightmaps de 2048 como máximo y sin mapas direccionales.
  - 2 rebotes de luz y sondas L1.
  - Los lightmaps se comprimen solos en Android (ASTC).
- **Archivos:** `Assets/BakeryLightmaps/3_TutorialMazmorra_*`.
  - Bakery también guardó su margen de UV en los `.meta` de 12 modelos de `DungeonModularPack`; el modelo en sí no cambia.

**Importante:** al regenerar la escena combinada se pierde este horneado. Después de **Tools > Game Feel > Construir escena combinada** hay que ejecutar **Tools > Iluminación Quest > Hornear escena combinada (Bakery)**, que tarda alrededor de un minuto. Ese menú prepara la escena (luces, sondas, transparentes) y hornea.

## Postproceso

Un solo perfil, `Assets/_Custom/Lighting/PostProceso_Quest.asset`, elegido por su bajo costo en el Quest 2:

| Efecto | Ajuste | Costo |
| --- | --- | --- |
| Color Adjustments | contraste +14, saturación +10, exposición +0,1 | Va en la LUT de color: sin costo extra |
| Split Toning | sombras algo frías, luces cálidas | Va en la LUT de color |
| Vignette | intensidad 0,22, suave | Dentro de la misma pasada |
| Bloom | **apagado** (ya configurado a ¼ de resolución y 3 iteraciones) | Alrededor de 1 ms en Quest 2 si se activa |
| Tonemapping | no se usa | Con HDR apagado solo apagaría los colores |

- El antialiasing de postproceso de la cámara queda apagado.
- **Ajustar:** editar el perfil desde el Inspector; los cambios se ven en las tres escenas.
- **Quitar en una escena:** borrar el objeto `PostProcesado`, o desactivar *Post Processing* en la cámara del jugador.

## Rendimiento

- Las sombras en tiempo real se quitaron del pipeline de Android (`High_PipelineAsset`, el que usa el Quest): ya no hay pase de sombras por frame. Para recuperarlas, activar *Main Light > Cast Shadows* en ese asset y en la luz direccional.
- En PC el postproceso sumó unos 0,1 ms por frame. En el Quest 2 será más (una pasada de pantalla completa), y conviene medirlo con el Profiler o con OVR Metrics.
- La memoria de los lightmaps es de unos 12 MB con compresión ASTC.

## Cómo se probó

- Capturas antes y después de 6 zonas.
- Play mode en la escena combinada: carga 3 lightmaps y 624 sondas, y la consola queda sin errores.

**No probado con visor:** el brillo general, el costo real del postproceso y si la viñeta se nota.

## Tablero de la bandera: materiales nuevos

- **Problemas:**
  - La placa, el soporte y la base usaban `Railway_Barrier_Dielectric`, una textura de barrera estirada que se veía sucia y con reflejos borrosos.
  - Con la iluminación horneada, la luz naranja de las antorchas hacía ver el amarillo como naranja y el azul como verde.
- **Ahora** (materiales en `Assets/_Custom/Materials/Bandera/`):
  - `Bandera_Tablero`: placa de piedra oscura y mate.
  - `Bandera_Marco`: marco de bronce de 3,5 cm alrededor de la placa (objeto `Puzzle_Bandera/Borde`, sin colisiones).
  - `Bandera_Madera`: soporte y base.
  - `Bandera_Amarillo`, `Bandera_Azul` y `Bandera_Rojo`: para las celdas fijas del tablero y para los cubos de los tres dispensadores. Tienen poco albedo y bastante emisión: la luz da volumen al cubo pero no cambia su color, así se distinguen igual en cualquier zona.
  - Los colores de la pista de `FlagPuzzle` se ajustaron a los mismos tonos.
- **Dónde:** el cambio se hizo en `2_Adaptacion`, y después se regeneró y se volvió a hornear `3_TutorialMazmorra`.
- **Ajustar el color:** editar el material. `Base Color` es la parte que reacciona a la luz y `Emission` la que mantiene el color.
- **Deshacer:** `git revert` del commit "Bandera: tablero y cubos con materiales propios".

---

# Fase 1 nueva: mirada guiada al estilo Halo (2026-10-27)

## Commits

| Commit | Qué hace | Cómo deshacerlo |
| --- | --- | --- |
| `817e93b` | Script `GazeSequenceTutorial`, constructor `GazePhaseBuilder` (**Tools > Game Feel > Construir fase 1 (mirada guiada)**), materiales y zumbido. El generador de la escena combinada descarta el almacén de Bakery que a veces aparece en el tutorial. | `git revert 817e93b` (después de los de abajo) |
| `8e1c426` | `EditorVideoCapture` y `GazeTutorialAutopilot`: graban un video de prueba en Play mode. Solo existen en el Editor; no van al build. | `git revert 8e1c426` |
| `1bc6d37` | `2_Adaptacion` con la fase nueva; `3_TutorialMazmorra` regenerada y horneada. | `git revert 1bc6d37`: vuelven las runas y la pared marrón |
| `85fef44` | Unity sincronizó `_Color` en los materiales de la bandera (sin efecto visible). | No hace falta |

## Antes y ahora

| Antes | Ahora |
| --- | --- |
| 4 runas encendidas a la vez y sin instrucción (el aviso inicial nunca se activaba). | Una luz cada vez: frente (acierto fácil), arriba, abajo, izquierda y derecha. La primera dice "Mira fijamente la luz". |
| Contaba en cuanto el centro exacto de la vista tocaba la runa. | Hay que mirarla fijamente 0,7 s, con un margen de 6° más el tamaño de la luz. Si se aparta la vista, se descarga el doble de rápido. |
| Sin indicación de dónde mirar. | Punto de mira solo en esta fase, anillo que se cierra sobre la luz y flecha en el borde de la vista cuando la luz queda fuera. Además, la luz suena en 3D. |
| Pared marrón semitransparente que desaparecía al final. | Barrera de 5 haces cian con postes como los de las otras barreras. Cada luz cargada vuela a su gema en la columna, la enciende y apaga un haz. Al final, la barrera se hunde en el suelo. |

- **Durante la carga:**
  - La luz crece.
  - El zumbido sube de tono.
  - La vibración se acelera.
- **Al cargar:**
  - Nota musical que sube con cada luz, como antes.
  - Pulso de vibración.
- **Al llegar a la gema:**
  - Sonido de cierre.
  - Partículas cian.
  - Rebote de la gema.
  - Chispas al apagarse el haz.

**Ergonomía:** las luces están a unos 3 m, como máximo a 60° a cada lado, 32° hacia arriba y 30° hacia abajo. Ninguna queda detrás, así que se puede jugar sentado.

## Ajustar

- **En el componente `GazeSequenceTutorial`** (objeto `GameManager`):
  - `Dwell Time`: segundos de mirada para cargar.
  - `Gaze Angle`: margen de la mirada.
  - `Arrow Show Angle`: ángulo a partir del cual aparece la flecha.
  - `Hum Volume`: volumen del zumbido.
- **Mover una luz:** mover su objeto en `Fase1_Mirada/Luces`. El orden de la lista `Targets` es el orden del juego.
- **Reconstruir desde cero:** ejecutar **Tools > Game Feel > Construir fase 1 (mirada guiada)** en `2_Adaptacion`. Las posiciones de las luces están en `GazePhaseBuilder.cs`.
- **Volver a la fase vieja sin revertir:**
  - Desactivar `GazeSequenceTutorial` y `Fase1_Mirada`.
  - Activar `LookDirectionTutorial`, las 4 runas (`up`, `down`, `Left`, `right`) y `Prared vizualization`.

## Video de prueba desde el Editor

Con la escena en Play mode, añadir a un objeto `GazeTutorialAutopilot` (con la referencia `gaze`) y `EditorVideoCapture`, y llamar a `Begin()`. El autopilot mueve la cabeza como una persona; la captura guarda cuadros JPG a 30 fps y el audio en `audio.wav`. Se unen con:

```
ffmpeg -framerate 30 -i f_%05d.jpg -i audio.wav -c:v libx264 -crf 20 -pix_fmt yuv420p -c:a aac -shortest video.mp4
```

## Cómo se probó

- Play mode en `2_Adaptacion` y en `3_TutorialMazmorra`, con el autopilot: carga las 5 luces, apaga los 5 haces, retira la barrera y habilita el movimiento. La consola queda sin errores.
- Video de 23 s grabado en la escena combinada.

**No probado con visor:** el tiempo de carga, el volumen del zumbido y la posición de las luces de los lados.

## Observación

Con la ventana de Bakery abierta, Bakery crea su objeto `!ftraceLightmaps` en la escena activa. Si se guarda `2_Adaptacion` con ese objeto, la escena combinada queda con dos almacenes y Bakery avisa. Para evitarlo, el generador de la combinada lo quita del tutorial.

## Prueba completa del tutorial (2026-10-27)

`TutorialAutopilot` (`Assets/_Custom/Scripts/Tools/TutorialAutopilot.cs`, solo para el Editor) juega el tutorial entero en `3_TutorialMazmorra` y, junto con `EditorVideoCapture`, lo graba. Recorrido, en tiempo de juego:

| Momento | Fase | Resultado |
| --- | --- | --- |
| 3–18 s | Mirada guiada | Las 5 luces se cargan y la barrera se retira. |
| 20–34 s | Círculos de movimiento | Se pisan los 4 en orden y el láser de movimiento se apaga. |
| 41–54 s | Bandera | Un cubo rojo puesto a propósito en un hueco amarillo es rechazado. Con los 6 cubos correctos se apagan los 4 haces y la barrera azul. |
| 55–60 s | Arañas | 4 aplastadas con el martillo; aparece el contador. |
| 61–66 s | Canasta | El aro se ilumina al sostener el balón; el enceste apaga el láser rojo. |
| 67–89 s | Golem | Recorre la pista esquivando las 5 filas de pinchos y llega a la meta. |
| 89–98 s | Final | La runa vuela a la pared, la pared se hunde y se ve el pasillo hacia la mazmorra. |

- Sin errores en la consola.
- **Lo que la prueba no cubre:**
  - Agarrar con las manos de AutoHand: los cubos se colocan con `PlacePoint.Place` y el balón y el martillo se mueven por script.
  - Conducir el golem con la palanca y el volante: el golem se mueve por script.
- **Observación:**
  - `HoopDetector` suma un punto cada vez que el balón entra en el aro, así que un balón que rebota puede contar varias veces y repetir el confeti.
  - En la prueba sumó 3 puntos con un tiro, en parte porque el evento también se disparó a mano.
  - No afecta al avance: un enceste basta para apagar el láser.

---

# Portal del título en 3D (2026-10-27)

El póster plano `foto lsv` de la pared de inicio, la que queda detrás del jugador al empezar, se reemplazó por un pequeño escenario 3D, `Portal_Titulo`, inspirado en esa imagen:

- Arco de piedra (`Arch_A`) entre dos columnas (`Pillar_A`), cada una con su antorcha, fuego y luz horneada.
- Placa en forma de escudo, verde oscuro con borde de bronce, y el título **LSV-INMERSIVE 360** en relieve:
  - El relieve son 7 capas de texto doradas apiladas detrás de un frente verde con borde dorado.
  - La fuente es Cinzel Decorative Bold.
- Esmeralda facetada encima de la placa, con un brillo verde horneado sobre el arco, y 5 runas de colores debajo del título.
- Cinta de pergamino con "UNA AVENTURA MÁGICA".
- Utilería: barril, dos cajas, tres libros y dos pociones.

El póster quedó desactivado, no borrado. Para volver a él: desactivar `Portal_Titulo` y activar `foto lsv` en `2_Adaptacion`, y regenerar y hornear la combinada.

## Detalles

- **Herramienta:** **Tools > Game Feel > Construir portal del título** (`TitlePortalBuilder.cs`), que se ejecuta sobre `2_Adaptacion`. Las posiciones, los colores y el tamaño del texto están en ese archivo.
- **Fuente:** `Assets/_Custom/Fonts/Cinzel/CinzelDecorative-Bold.ttf`, descargada de Google Fonts con licencia SIL OFL 1.1 (`OFL.txt` al lado), libre para uso comercial.
  - El atlas SDF es estático y solo trae las letras necesarias (A–Z, a–z, acentos, ñ, números y signos).
  - Para usar otras letras, regenerar el atlas desde la ventana Font Asset Creator de TextMeshPro.
- **Modelos y materiales** del portal: `Assets/_Custom/Models/PortalTitulo/`.
- **Iluminación:**
  - El arco, las columnas y las antorchas son estáticos y se hornean.
  - La placa, la cinta y la utilería reciben luz de las sondas.
  - La cinta lleva algo de brillo propio y el bronce es oscuro para que la luz naranja de las antorchas no los vuelva naranja intenso.
- **Rendimiento:** unos 40 objetos pequeños sin sombras en tiempo real; el texto son 16 mallas de pocas letras con un material de TextMeshPro para móviles.

## Commits

| Commit | Qué hace | Cómo deshacerlo |
| --- | --- | --- |
| `5e0fbad` | Fuente, herramienta, modelos y materiales del portal. | `git revert 5e0fbad` (después del siguiente) |
| `d366004` | `2_Adaptacion` con el portal; combinada regenerada y horneada. | `git revert d366004` |

## Logo de Meshy en el portal (2026-10-27)

El escenario del portal se mantiene: arco, columnas, antorchas, barril, cajas, libros y pociones. Solo se cambió el logo: la placa hecha a mano (`Portal_Titulo/Titulo`, con su luz verde) quedó desactivada, y en su lugar está `Logo_Meshy`, el relieve hecho con Meshy a partir de la imagen del póster.

- **Modelo:** `Assets/_Custom/Models/PortalMeshy/Portal_Meshy.fbx`.
  - Tiene 3.994 triángulos y mide 2,9 × 1,96 m con 10 cm de relieve. Va dentro del arco, entre las columnas.
  - Se importa sin animación y con UV de lightmap para Bakery; es estático y se hornea.
- **Textura:** `Portal_Meshy_Color.png`, de 2048 px, comprimida en ASTC 6×6 en Android.
- **Material:** `Portal_Meshy.mat`. Lleva la misma textura como emisión (nivel 0,45): sin ella, la luz cálida de las antorchas volvía verdes las letras azules. Para ajustarlo, cambiar `Emission` en el material.
- **Herramienta:** **Tools > Game Feel > Colocar logo de Meshy en el portal** (`MeshyPortalBuilder.cs`), sobre `2_Adaptacion`. Después hay que regenerar y hornear la escena combinada.
- **Para volver a la placa hecha a mano:** activar `Portal_Titulo/Titulo` y desactivar `Logo_Meshy`.

| Commit | Qué hace | Cómo deshacerlo |
| --- | --- | --- |
| `a9f08d4` | Modelo, textura, material y herramienta. | `git revert a9f08d4` (después del siguiente) |
| `9fd5e36` | `2_Adaptacion` con el logo; escena combinada regenerada y horneada. | `git revert 9fd5e36` |

---

# Iluminación sin Bakery (2026-10-28)

Bakery desapareció del proyecto (`Assets/Bakery` no existe y está en `.gitignore`, así que git no puede recuperarlo). `QuestLighting.cs` dejó de compilar y, con él, todos los menús de **Tools**. La escena combinada ahora se hornea con el lightmapper de Unity.

- **Menú:** **Tools > Iluminación Quest > Hornear escena combinada**. Tarda menos de 1 minuto con la GPU.
- **Ajustes de horneado:** `Assets/_Custom/Lighting/Horneado_Quest.lighting`. La escena ya no usa el `Template Lighting Settings.lighting` compartido.
  - Progressive GPU, 40 texels por unidad, lightmaps de hasta 2048.
  - Sin mapas direccionales (la mitad de memoria), 2 rebotes, AO horneada suave (0,6 m) y denoiser automático.
  - Resultado: 2 lightmaps de 2048 más uno de 256, y 639 sondas de luz.
- **Luces:**
  - Las antorchas quedan encendidas en modo **Baked**: solo existen en el lightmap y no cuestan en tiempo real. Sus sombras horneadas son suaves (radio 0,08).
  - Las 9 antorchas encendidas de la sala de runas solo tenían luz de Bakery y la sala quedaba casi a oscuras. Ahora la herramienta les pone una luz de Unity equivalente: color (1, 0,6, 0), intensidad 2 y alcance 6,5 m. Para ajustarla, cambiar `DungeonTorch*` en `QuestLighting.cs` y volver a hornear.
  - Se quitan los componentes rotos de Bakery (30) y su almacén `!ftraceLightmaps`.
- **`Hedgehog.fbx`** (erizos de la barrera) ahora genera UV de lightmap; antes se horneaban mal.
- **Mejoras de imagen y rendimiento:**
  - El pipeline de Android (`High_PipelineAsset`) pasa de MSAA 1x a **4x**. En VR los bordes dentados se notan mucho, y en la GPU por tiles del Quest el 4x es casi gratis.
  - La cámara del jugador dibuja hasta 100 m en lugar de 1000 (la escena mide unos 80 m). Así hay más precisión de profundidad.
  - El fondo de la cámara es casi negro: por las rendijas de la mazmorra se veía azul cielo.
  - El perfil de postproceso no cambió.
- **Pendiente:**
  - `1_GAME` todavía tiene 10 luces rotas de Bakery y sus lightmaps viejos ya no se aplican. Hoy no afecta al juego, porque el build empieza en la combinada.
  - Comprobar con el visor el costo del MSAA 4x junto al postproceso.

| Commit | Qué hace | Cómo deshacerlo |
| --- | --- | --- |
| `053f264` | Herramienta sin Bakery, ajustes de horneado, UV de `Hedgehog`, comentarios. | `git revert 053f264` (después de los dos siguientes) |
| `d3662bb` | MSAA 4x en el pipeline de Quest. | `git revert d3662bb` |
| `d09910f` | Escena combinada horneada con Unity; se borran sus lightmaps viejos de Bakery. | `git revert d09910f` |

---

# Pasillo de la mazmorra, atajo de prueba y sonidos rehechos (2026-10-28)

## Colliders en el paso a la mazmorra

El suelo, las dos paredes y el techo de `Pasillo_Mazmorra` no tenían collider: el jugador podía atravesar las paredes o caerse en medio del pasillo. Las dos columnas de la puerta de la mazmorra (`Pillar_A_0` y `Pillar_A_1`, de `1_GAME`) tampoco tenían.

- **Herramienta:** **Tools > Game Feel > Pasillo de la mazmorra: colliders y atajo de salto** (`DungeonAccessBuilder.cs`), sobre `2_Adaptacion`.
  - Las piezas planas reciben una caja de al menos 30 cm de grosor, que crece hacia fuera del pasillo para no invadir el espacio por donde se camina. Las piezas con volumen, como los arcos, reciben un MeshCollider que respeta el hueco.
  - No toca antorchas, efectos transparentes ni el fondo temporal del pasillo (la pared y el portal a `1_GAME`).
  - Las piezas del pasillo quedan estáticas, así que ahora se hornean igual que el resto del tutorial. La combinada tiene 4 lightmaps.
- Las columnas de la entrada reciben su collider al construir la escena combinada (`CombinedSceneBuilder`).
- **Para deshacerlo:** quitar los `BoxCollider` de `Pasillo_Mazmorra` en `2_Adaptacion` y regenerar la combinada.

## Atajo: mantener A para saltar el tutorial

`GameFeel/SaltarTutorial` (`TutorialSkip`):

- Al mantener el botón **A** del control derecho, aparece un anillo de progreso delante de la vista y la vibración se va acelerando. Si se suelta antes de tiempo, se cancela.
- Pasados 2 segundos:
  1. Fundido a negro.
  2. La fase de mirada se da por terminada, con el movimiento libre.
  3. La pared `Wall_A (11)` queda abierta.
  4. El jugador aparece frente al pasillo, mirando hacia la mazmorra (`SaltarTutorial/Destino`).
  5. Fundido de vuelta.
- Solo funciona una vez por partida. En el Editor sin visor se activa con la tecla **K** (con la ventana Game enfocada).
- Los láseres y barreras del tutorial siguen encendidos detrás del jugador: el atajo es para probar la mazmorra, no para volver atrás.
- **Para quitarlo:** borrar el objeto `GameFeel/SaltarTutorial` en `2_Adaptacion` y regenerar la combinada.
- **Pruebas automáticas:** `TutorialSkip.simulateHold = true` hace como si A estuviera pulsado.

## Sonidos rehechos

**Investigación.** De ahí salen las buenas prácticas aplicadas:
- [Audiokinetic: procesado de sonoridad para juegos](https://www.audiokinetic.com/zh/loudness-processing-best-practice-chapter-3-scalable-loudness-processing-for-games): cada sonido suma a la salida total, así que hay que dejar margen.
- [Guía de diseño de audio de Meta](https://developers.meta.com/horizon/design/audio/): espacializar, atenuar con la distancia como pista, evitar sonidos fuertes o bruscos y separar sonidos por rango de frecuencias.
- [Audio en Unity para Meta Quest](https://developers.meta.com/horizon/documentation/unity/unity-audio/): no descomprimir al cargar lo que no sea corto.
- [Manual de Unity: compresión de audio](https://docs.unity3d.com/Manual/AudioFiles-compression.html): Decompress On Load para sonidos cortos y comprimidos en memoria para los largos.

**Cómo se aplicó.** Generador: **Tools > Game Feel > Regenerar sonidos** (`GameFeelAudioBuilder.cs`). Los nombres de archivo no cambiaron, así que las escenas siguen apuntando a los mismos clips.

| Práctica | Qué se hizo |
| --- | --- |
| Capas | Cada sonido tiene tres partes: un transitorio (el clic que marca el instante), un cuerpo (el tono o golpe que dice qué es) y una cola (el aire o la resonancia). |
| Sonoridad pareja | Se normaliza por sonoridad tipo LUFS (BS.1770 simplificada, ventanas de 400 ms), no por pico. Objetivos: interfaz -17, aire -19, golpes -15, premios -15, gran premio -13, error -19 y bucles -22. Comprobado con el medidor EBU R128 de ffmpeg: diferencia de 1 a 2 dB. |
| Margen | Pico máximo de -1 dBFS con limitador suave. |
| Altavoces del Quest | Los golpes graves llevan armónicos (saturación suave) para que se oigan en altavoces pequeños. Se filtra todo lo que está por debajo de 30 Hz. |
| Comodidad | El error es un "bonc-bonc" de madera, suave. Los premios son más brillantes y algo más fuertes. |
| Sin clics | Fundido de entrada de 1 ms y de salida de 20 ms, y dither al pasar a 16 bits. Los bucles (`charge` y `gaze_hum`) se repiten sin costura. |
| Variación | Pop, golpe, paso, whoosh y swish tienen 3 tomas, y el error 2. Están en `Resources/FeelSoundBank.asset`. `FeelAudio` alterna tomas sin repetir la anterior y varía el volumen alrededor de ±1 dB. |
| Mezcla | `FeelAudio` no deja que el mismo sonido se apile: como mucho 3 a la vez y nunca dos en menos de 40 ms. Prioridad 64, atenuación de 1,2 a 25 m y sin dispersión. |
| Importación | Todo en mono. Los cortos usan Decompress On Load y los de más de 1,6 s van comprimidos en memoria con carga en segundo plano. Vorbis a calidad 0,6. |

- **Para volver a una toma única por sonido:** borrar `Assets/_Custom/GameFeel/Resources/FeelSoundBank.asset`.
- **Para ajustar un sonido:** cambiar su función en `GameFeelAudioBuilder.cs` (por ejemplo `Thud`) y volver a ejecutar el menú.
- **Pendiente con visor:** volumen general y equilibrio entre sonidos en los altavoces del Quest.

| Commit | Qué hace | Cómo deshacerlo |
| --- | --- | --- |
| `103fd93` | Colliders del pasillo, atajo de salto y herramienta. | `git revert 103fd93` (después de `4d50ef5`) |
| `aecb4fb` | Sintetizador nuevo, banco de variantes y sonidos regenerados. | `git revert aecb4fb` |
| `4d50ef5` | Escena combinada regenerada y horneada. | `git revert 4d50ef5` |

---

# Final de la mazmorra: entrada abierta, audio, altar nuevo y portal (2026-10-28)

Todo lo aplica `DungeonFinaleBuilder.cs` sobre la escena combinada al construirla (**Tools > Game Feel > Construir escena combinada**). `1_GAME` no se toca. Después hay que hornear como siempre.

## Entrada abierta

- `Wall_B_0`, la pared con el arco por el que se entra desde el pasillo, tenía una caja de 0,2 × 4 × 4 m que tapaba el hueco entero. En `1_GAME` esa puerta no llevaba a ningún sitio.
- Ahora la pared y su arco (`Arch_A_1`) llevan MeshColliders que siguen el borde: se pasa por el centro del arco y los lados siguen siendo sólidos.

## Audio de la mazmorra (mismas buenas prácticas que el tutorial)

- Las 9 fuentes de audio de la sala estaban en 2D con 500 m de alcance. Ahora están en 3D con atenuación logarítmica de 1 a 25 m.
- Pilares: al activarse suena `feel_activate`, energía que sube y una campanita. Antes uno sonaba como una explosión y los otros no sonaban.
- Puerta principal: se quitó la explosión. Ya sonaban el derrumbe de piedra y la fanfarria, y la explosión solo los tapaba (y es incómoda en VR).
- Tono de sala (`Ambiente_Mazmorra`, `feel_room_tone`): aire grave, corriente lejana y alguna gota. Se oye al entrar y se apaga al volver al tutorial.
- Crepitar en las antorchas de la mazmorra (`feel_torch_crackle`): solo se oye de cerca. Cada antorcha empieza en un punto distinto y con un tono un poco diferente (`FeelLoopRandomStart`), para que no suenen sincronizadas.
- Los efectos visuales que ya tenía la sala (carga de los pilares, brillos y ráfagas al fijar, runa equivocada, fases) siguen igual. Ahora también usan las variantes de sonido y el límite de apilamiento de `FeelAudio`.

## Altar de las runas nuevo (`Altar_Runas`)

- Pedestal escalonado de piedra (zócalo, escalón, cuerpo oscuro, moldura de bronce y losa) con columnitas en las esquinas rematadas en bronce y una gema. En cada cara lleva una franja de runas que brilla.
- Huecos: plato de piedra oscura con un anillo del color de su elemento (Fuego rojo, Agua turquesa, Tierra amarillo, Viento naranja). La altura no cambió, así que las runas encajan donde antes.
- `RuneAltarGlow`, solo visual:
  - Hueco vacío: el anillo queda tenue y "respirando".
  - Runa correcta: se enciende con un rebote.
  - Runa equivocada: destello rojo.
  - El círculo central y sus líneas hacia los huecos se encienden según cuántas runas correctas hay.
- El altar viejo (cubos) queda apagado, pero conserva sus `PlacePoint` y el collider del cuerpo. Los colliders de los cubitos se apagaron porque sobresalían 13 cm de la losa.
- **Para volver al altar viejo:** borrar `Altar_Runas` y reactivar los MeshRenderer de `Altar` y sus `Placepoint-*`.

## Portal final y reinicio

- Al resolver la fase 2, la puerta principal (`Main Door To Section 2`) se hunde como antes. 1,6 s después aparece `Portal_Final` en el marco: brillo azul-violeta, partículas mágicas y un zumbido.
- Al cruzarlo (`EndPortal`):
  1. Fundido a negro.
  2. El jugador aparece en `Sala_Final`, una sala oscura 60 m bajo la mazmorra con un círculo de luz bajo los pies. Ahí ve "¡Gracias por jugar!", "LSV-INMERSIVE 360" y una cuenta atrás de 8 s.
  3. Fundido.
  4. Se vuelve a cargar la primera escena del build: el juego empieza de nuevo.
- Detrás del portal hay un tope invisible, porque la Sección 2 no está en la combinada.
- **Probado en Play mode:** al resolver aparece el portal; al cruzarlo, la sala final, la cuenta atrás y el reinicio. En consola solo sale el aviso conocido de AutoHand al cambiar de escena.
- **Para quitarlo:** borrar el componente `EndPortal` (la puerta se abre como antes y detrás no pasa nada).

## Pendiente con visor

- Brillo del portal y del altar en el Quest.
- Volumen del tono de sala y del crepitar.
- Tamaño del texto final, que está a 3,2 m.

| Commit | Qué hace | Cómo deshacerlo |
| --- | --- | --- |
| `b57df51` | Sonidos nuevos de la mazmorra. | `git revert b57df51` (después de los dos siguientes) |
| `93cfc79` | Herramienta del final de la mazmorra, `EndPortal`, `RuneAltarGlow`, `FeelLoopRandomStart` y materiales. | `git revert 93cfc79` (después del siguiente) |
| `20efe99` | Escena combinada regenerada y horneada. | `git revert 20efe99` |

# Correcciones del informe de QA (2026-11-03)

Revisión del informe de QA sobre `3_TutorialMazmorra`. Se corrigieron 4 de los 5 hallazgos. El quinto (BUG-05) no era un fallo.

## Runa de Fuego: error al colocarla (BUG-01)

- **Qué pasaba:** al colocar la runa de Fuego en su hueco salía en consola `Can't remove Rigidbody because ReturnHomeWhenDropped depends on it`. El hueco `PlacePoint Fuego` es el único de los cuatro con `disableRigidbodyOnPlace` activado: AutoHand destruye el Rigidbody de la runa al colocarla y lo vuelve a crear al quitarla. El `[RequireComponent(typeof(Rigidbody))]` de `ReturnHomeWhenDropped` lo impedía, y la runa se quedaba con física dentro del hueco.
- **Cambio:** `ReturnHomeWhenDropped` ya no exige el Rigidbody. Lo vuelve a buscar cada vez que lo necesita y no hace nada mientras no lo tenga.
- **Probado en Play mode:** colocar y quitar las 4 runas no deja errores. La de Fuego pierde el Rigidbody al colocarla y lo recupera al quitarla.
- **Para deshacer:** revertir `ReturnHomeWhenDropped.cs`.

## Canasta: un tiro contaba varias veces (BUG-02)

- **Cambio:** `HoopDetector` ignora el mismo balón durante `sameBallCooldown` (1,2 s) después de un enceste. Un balón con varios colliders o que rebota dentro del aro ya no suma varios puntos ni repite el confeti y el sonido.
- **Probado:** tres entradas seguidas del mismo balón suman 1 punto.
- **Para deshacer:** `sameBallCooldown = 0` en el inspector (vuelve a contar cada entrada).

## Círculos de movimiento: sonaban por la voz del narrador (BUG-03)

- **Qué pasaba:** `MovementTutorial` usaba `FindObjectOfType<AudioSource>()`, que devolvía el primer AudioSource de la escena: `Voice 11`, la narración.
- **Cambio:** el `touchSound` suena con `FeelAudio.Play` en la posición del círculo (3D, fuente temporal), igual que el resto de efectos.
- **Para deshacer:** revertir `MovementTutorial.cs`.

## Rescate por caída (BUG-04)

- **Nuevo `PlayerFallRescue`** (objeto `GameFeel/RescateCaida`):
  - Recuerda el último sitio donde el jugador estuvo apoyado en suelo fijo (no cuentan el carro del golem ni los objetos sueltos).
  - Si cae más de 6 m por debajo de ese sitio, funde a negro y lo devuelve allí. No gira la vista.
  - Los teletransportes del juego (círculos, atajo de salto, portal final a la sala de -60 m) no cuentan como caída: el destino pasa a ser el nuevo sitio seguro.
  - `AutoHandPlayer.IsGrounded()` casi nunca es verdadero en este proyecto (el cuerpo se apoya por física), así que el script también comprueba el suelo con un rayo corto.
- **Instalación:** **Tools > Game Feel > Rescate por caída del jugador** sobre `2_Adaptacion`. También lo instala el menú del pasillo de la mazmorra. Después se regeneró y se volvió a hornear la combinada.
- **Probado en Play mode:** con el jugador bajo el suelo, cayó y fue rescatado una vez al sitio de partida.
- **Para quitarlo:** borrar `GameFeel/RescateCaida` en `2_Adaptacion` y regenerar la combinada.

## BUG-05: no se cambió

`FeelDisappear` restaura la posición de la pared en el mismo frame en que la apaga. Es a propósito (comentario en el código): si alguien vuelve a activar el objeto, aparece entero. Nada vuelve a activar `Wall_A (11)`, así que no tiene efecto en el juego. El "bajó 0,0 m" del informe es la pared ya apagada y restaurada.

## Notas

- La regeneración también reescribió algunos materiales del generador (sobre todo finales de línea; en las runas, la textura heredada `_MainTex`). Es la salida normal de **Construir escena combinada**.
- Observaciones de UX del informe sin tocar: sonido de rebote del balón, ajuste de altura para jugar sentado y el golem que sigue "encendido" tras llegar a la meta.

# Parte dos: piedras de colores en el arco y revisión del puzzle de pilares (2026-11-03)

## Piedras de colores en el arco de la salida

- Cuatro piedras incrustadas en la banda del arco de la puerta principal (`Wall_B_1 Puerta seccion 1/Arch_A_1/Piedras_Arco`), una por pilar. Vistas desde la sala: abajo a la izquierda Fuego (rojo), arriba a la izquierda Tierra (amarillo), arriba a la derecha Viento (verde) y abajo a la derecha Agua (azul).
- `ArchElementStones`:
  - Al principio: apagadas (piedra oscura con un leve tono de su color).
  - Mientras su pilar apunta al mural correcto: parpadeo tenue que crece con la carga.
  - Pilar fijado: se encienden con destello, rebote, partículas de su color y una campanita que sube de tono con cada piedra.
  - Si se quita la runa del altar y el pilar se suelta: se apagan (sonido de apagado).
  - Con las cuatro: una ola de luz recorre el arco mientras se abre la puerta.
- Las crea **Construir escena combinada** (paso `BuildArchStones` de `DungeonFinaleBuilder`). Mide la banda del arco con rayos, así que cada piedra sigue la curva, con bisel y 3,5 cm por delante de la cara. Mallas en `GameFeel/Meshes/Arco_Piedra_*.asset`, material `Arco_Piedras` (URP Lit con la textura de la pared; color y emisión por `MaterialPropertyBlock`). Sin luces nuevas ni colliders. No van en el lightmap: les llega luz de las sondas.
- **Para quitarlo:** borrar `Piedras_Arco` (y la línea `BuildArchStones` del generador).

## Revisión de la parte dos (sala de runas)

Partida completa simulada en Play mode: colocar runas → girar pilares → fase 1 (pared secreta) → runas de Tierra y Viento → fase 2 (puerta, portal). Todo encadena bien y sin errores en consola. Quitar una runa con todo resuelto apaga su piedra y suelta su pilar; al volver a ponerla se fija otra vez.

Problema encontrado y corregido en `PillarProjector`:

- **Alinear los pilares era casi imposible en VR.** El rayo solo toca su mural en una ventana de ±4° (Tierra y Viento, mural a 14 m) a ±7° (Fuego y Agua). Además había que mantenerlo 2 s seguidos: cualquier temblor de la mano reiniciaba el contador.
- Dos ayudas nuevas (campos en el inspector; 0 = como antes):
  - `MissGraceTime` (0,3 s): si el rayo se sale del mural un momento, el contador se pausa en vez de reiniciarse.
  - `AssistAngle` (10°) y `AssistSpeed` (40°/s): si el jugador suelta el pilar cerca del ángulo correcto, el pilar gira solo hasta alinearse. Mientras se sostiene no hace nada.
- El imán gira con `MoveRotation`. La base de los pilares inferiores se mete 1,5 cm en el suelo y el rozamiento anulaba cualquier giro por velocidad (y hacía que el pilar se desviara unos grados solo).
- **Probado:** pilares soltados a 6-9° del ángulo correcto se alinean y se fijan en unos 3 s.

Comprobado sin cambios: los `PerfectRotationY` de la combinada coinciden con la dirección de cada mural (Fuego, Agua y Tierra 0°, Viento -90°). Los pilares solo giran sobre su eje vertical. Las runas de Tierra y Viento quedan accesibles al abrirse la pared secreta, y la escalera central sube al balcón de los pilares superiores y de la puerta.

## Pendiente con visor

- Que el imán no se sienta como si el pilar "se moviera solo" de forma molesta (si molesta: bajar `AssistAngle` a 5-6°).
- Brillo de las piedras en el Quest y si la azul apagada se distingue de la verde con la luz de las antorchas.
- Si cuesta girar los pilares inferiores con la mano por el rozamiento con el suelo.

# Runas distintas, arco de piedra tallada y segundo informe de QA (2026-11-03)

## Runas que se distinguen (conceptos A + D)

Antes las cuatro runas eran el mismo cubo con la misma estrella en las caras; solo cambiaba el color.

- **Símbolo propio en las 6 caras** (`RuneGlyphs`, el mismo que llevan las piedras del arco): △ fuego, ▽ agua, ▽ con barra tierra, △ con barra aire. Cada uno lleva una marca interior para que no se confundan aunque la runa esté boca abajo (△ girado es ▽): llama (fuego), ola (agua), punto (tierra) y anillo (aire).
- La estrella vieja se borra del color y del relieve del modelo (`Runa_Piedra_Albedo/Normal.png`), y el símbolo nuevo va en el mapa de emisión de cada elemento (`Runa_Glifo_<Elemento>.png`).
- **Aura de partículas** (hijo `Aura` de cada runa, material `Runa_Aura` con la chispa `feel_spark_dot.png`):
  - Fuego: brasas que suben y se quedan atrás al mover la runa.
  - Agua: gotas que caen.
  - Tierra: motas doradas en órbita lenta.
  - Viento: remolino.

  Son 24 partículas como máximo por runa, sin luces, con tamaños proporcionales al cubo.
- Herramienta: `RuneVisuals` (menú **Tools > Game Feel > Regenerar texturas de las runas**). La aplica **Construir escena combinada**.
- **Para quitarlo:** borrar la llamada `RuneVisuals.Apply` en `DungeonFinaleBuilder` y regenerar. Las runas vuelven a la estrella común.

## Piedras del arco menos "caricaturescas"

- Antes eran losas de color plano que brillaban enteras.
- Ahora son roca con relieve, apenas teñida de su color, con tonos de gema: rubí, ámbar, esmeralda y zafiro.
- Al encenderse brillan solo el símbolo del elemento (el mismo de las runas) y unas vetas finas.
- Texturas: `ArchStoneTextures` (`Arco_Piedra_Albedo/Normal/Vetas.png`, atlas 2×2, menú **Regenerar texturas de las piedras del arco**).
- UV por piedra con proyección plana para que el símbolo quede derecho.

## Segundo informe de QA (Gemini)

| Hallazgo | Veredicto | Qué se hizo |
| --- | --- | --- |
| BUG-06 (Medio): un teletransporte al vacío dejaba el sitio seguro en el aire y el rescate se repetía en bucle | Correcto | `PlayerFallRescue` separa la altura desde la que se mide la caída (sí se mueve con cada teletransporte) del sitio de rescate (solo cambia si hay suelo debajo del destino). Probado: teletransporte a y = -25 → 1 rescate al último suelo y ninguno más. |
| BUG-07 (Bajo): `PlacePoint Tierra ` con espacio al final | Correcto (viene de `1_GAME`) | El generador de la combinada recorta los nombres de los huecos. |
| BUG-08 (Bajo): cubos de la bandera con etiqueta `BasketBall` | Falso | Hoy solo el balón y su dispensador la llevan; el aviso de `AGENTS.md` estaba desactualizado y se quitó. |
| UX: al volver a poner una runa en un pilar ya fijado había que esperar otros 2 s | Válido | `PillarProjector`: si ya se había fijado y sigue apuntando a su mural, se vuelve a fijar en unos 0,3 s. |
| UX: el pergamino no vuelve si se lanza lejos | Falso | Ya lleva `ReturnHomeWhenDropped` desde que se creó. |
| UX: sacudidas de cámara en la escalera | Válido, otra solución | El jugador tenía `useSmoothStep` apagado: AutoHand subía el cuerpo de golpe en cada peldaño. Se activa en `2_Adaptacion` (`DungeonAccessBuilder.PlayerComfort`), en vez de la rampa invisible que proponía el informe. |

**Para deshacer:** desmarcar `useSmoothStep` en el jugador de `2_Adaptacion`; revertir `PlayerFallRescue.cs` y `PillarProjector.cs`.

## Pendiente con visor

- Si el aura de las runas se ve bien de cerca (en capturas, las gotas del agua apenas se aprecian sobre el altar).
- Que la subida suave de escalones no maree.

## Tercer informe de QA (solo la parte dos)

Informe de Gemini enfocado en la sala de runas (`Docs/QA_Prompt_Gemini_Parte2.md`): 0 bugs críticos, altos o medios. Confirma con mediciones:

- Re-fijado de un pilar en 26 frames (0,303 s), frente a 2 s la primera vez.
- Escalera suave.
- Rescate sin bucle tras un teletransporte al vacío.
- Símbolos de las runas limpios y distinguibles aunque estén girados.
- Aura de 10 a 18 partículas por runa.
- Arco legible desde 4 m.

- El único hallazgo "bajo" (`IsGrounded()` transitorio en `PlayerFallRescue`) ya lo cubre la comprobación con rayo de `IsOnSolidGround`: sin cambios.
- UX: las gotas del aura de Agua se veían poco. Ahora son más grandes, más claras, más frecuentes y caen más despacio (`RuneVisuals`). En capturas fijas siguen siendo discretas: falta revisarlas con visor.
- UX propuesta y no hecha: vibración o chispas al cruzar la mano por el rayo de un pilar.

## Pergamino: la leyenda de colores se veía en espejo

- **Qué pasaba:** la leyenda ("Fuego rojo · Agua azul · Tierra amarillo · Viento verde") era una sola línea más ancha que el papel. `MakeText` pone `fontSizeMin = 0,5`, mayor que su tamaño máximo (0,2), así que el autoajuste no podía encogerla. Las puntas asomaban fuera de la hoja y allí se veía la leyenda de la otra cara invertida, mezclada con la delantera.
- **Cambio:** la leyenda va en dos líneas, en una caja de 0,28 m dentro de la zona opaca del papel y con `fontSizeMin = 0,08` (`BuildHintScroll` en `DungeonFinaleBuilder`). Se corrigió también en la escena combinada actual, sin regenerar: no afecta al horneado.
- **Probado:** capturas del pergamino abierto por las dos caras. Se lee limpio, sin texto en espejo.

# Murales ilustrados, gemas del rayo y torres nuevas (2026-11-03)

Todo lo crea `PillarTowerBuilder` (paso de **Construir escena combinada**). La lógica del puzzle no cambia.

## Murales ilustrados

- Cada mural de color plano pasa a ser una ilustración de su elemento (`MuralArt`, 512×368, menú **Tools > Game Feel > Regenerar ilustraciones de los murales**). Lleva:
  - pizarra oscura con marco de bronce;
  - el símbolo del elemento en grande (el mismo de runas y piedras);
  - un engaste pintado en el centro;
  - el motivo de su elemento: llamas (Fuego), olas (Agua), montañas (Tierra) o remolinos (Viento).
- El mapa de emisión es el propio dibujo: `FeelGlow` lo hace brillar al cargar el pilar, sin iluminar la pizarra.
- **Malla propia** (`Mural_Panel`): el cubo de Unity ponía la imagen cabeza abajo en una de sus caras. El panel nuevo la pone al derecho por las dos y trae UV de lightmap.
- **Cada mural se centra donde pega su rayo** con el pilar alineado (antes el impacto quedaba hasta 44 cm del centro). Solo se mueve dentro de su plano: los cuatro rayos pegan ahora en el centro exacto y la zona de acierto queda simétrica (Viento pasó de 268–274° a 266–274°).
- Collider, etiqueta, capa y `FeelGlow` del mural no cambian.

## Gema donde converge el rayo

- Engaste de bronce y gema (`Gema_<Elemento>`) delante del mural, justo en el punto de impacto. No tiene collider, así que no tapa el rayo.
- `PillarTowerFeel`:
  - Sin runa, la gema gira despacio, torcida y apagada.
  - Mientras el rayo carga, se endereza hasta quedar alineada con el rayo.
  - Al fijarse el pilar, se enciende con un rebote. Quitar la runa la vuelve a soltar.

## Torres de proyección

- La columna genérica del pack (`Pillar_A`) y la bola negra del punto de disparo se ocultan; solo se desactiva su `MeshRenderer`.
- En su lugar, la torre nueva (hijo `Torre` de cada pilar): zócalo y basa octogonales, fuste con franjas de runa del color del elemento, tambor con el cristal emisor en una boca de bronce, aros de bronce y remate en punta. Mallas en `GameFeel/Meshes/Torre_<Elemento>_*`.
- Las franjas y el cristal (`PillarTowerFeel`) están casi apagados sin runa, se encienden al ponerla, suben con la carga y quedan al máximo al fijarse.
- **No cambia:**
  - El collider, el Rigidbody, el `Grabbable`, el `LineRenderer` ni los bloqueos de los pilares.
  - El rayo es la misma recta: el punto de disparo solo se adelanta por su propio eje hasta el cristal (3–9 cm).
- **Probado en Play mode:**
  - Fases 1 y 2, puerta, portal y las 4 piedras del arco.
  - Imán con pilares soltados a 6–9° del ángulo correcto.
  - Re-fijado tras quitar y volver a poner una runa.
  - Ningún error en consola.
- **Para quitarlo:** borrar la llamada `PillarTowerBuilder.Apply` en `DungeonFinaleBuilder` y regenerar (vuelven los murales planos, la columna vieja y la bola negra).

## Pendiente con visor

- Si la torre más estrecha que su collider (13 cm de diferencia arriba) se nota al agarrarla.
- Brillo de las gemas y de las franjas en el Quest.

## Rayo de energía

- **Antes:** una barra opaca de 20–29 cm de ancho y color plano (`Sprites/Default`).
- **Ahora:** material aditivo `Rayo_Energia` con la textura `feel_rayo_energia.png`, que lleva un núcleo brillante, un halo suave, tres hebras de energía que se entrelazan y motas. Además, un segundo trazo fino casi blanco (`Nucleo`) en el centro.
- `LaserBeamFeel` (en el objeto del `LineRenderer`):
  - La energía fluye del cristal hacia la gema.
  - Pulso suave; mientras carga parpadea más rápido; fijado queda estable y más intenso.
  - El `Nucleo` copia la posición del rayo y se enciende y apaga con él.
- Solo cambia el dibujo: el raycast, la longitud y los colores por elemento son los mismos. Lo configura `PillarTowerBuilder.ConfigureBeam`. El rayo no afecta al horneado, así que se aplicó sobre la escena actual sin volver a hornear.
- **Probado en Play mode:** los 4 pilares se fijan, las 4 piedras se encienden, la puerta se abre y no hay errores.
- **Para quitarlo:** borrar `LaserBeamFeel` y el hijo `Nucleo`, y devolver el material `Sprites-Default` al `LineRenderer` (o quitar `ConfigureBeam` del generador y regenerar).
- **Pendiente con visor:** el grosor desde lejos (unos 15 cm con halo). Si se lee demasiado fino, subir `widthCurve` en `ConfigureBeam`.


---

# Mejora de iluminación, horneado y postproceso (2026-11-04)

Mejora integral de la luminosidad del proyecto tras detectarse que los escenarios (tutorial y sala de runas) se percibían excesivamente oscuros y con sombras empastadas.

## Diagnóstico del problema

1. **Iluminación ambiental inerte:** `RenderSettings.ambientMode` estaba en `Skybox`, pero `RenderSettings.skybox` era `null`. En consecuencia, la GI progresiva no tenía rebote ambiental de entorno y las zonas sin visión directa de antorchas quedaban en negro absoluto.
2. **Antorchas insuficientes:** en la mazmorra la intensidad era 2 y el alcance 6,5 m; con la caída cuadrática de Unity URP, la luz se disipaba casi por completo a los 3 m en una sala de más de 16 m de ancho. Además, `PrepareSceneForBake` omitía actualizar luces existentes si ya tenían componente `Light`.
3. **Rebotes y AO del horneado:** configurado con solo 2 rebotes indirectos y un exponente de AO indirecto de 1,0 que oscurecía fuertemente las esquinas y juntas de los bloques de piedra.
4. **Postproceso aplastante:** contraste en 14 (aplastaba sombras a negro profundo), viñeta en 0,22 (cerraba la visión periférica en VR) y compensación de exposición casi nula (0,1 EV).

## Qué se hizo

- **Antorchas y luces de entorno (`QuestLighting.cs`):**
  - Mazmorra (sala de runas): intensidad subida a 3,2, alcance a 8,5 m y color de llama cálido `(1.0, 0.62, 0.12)`.
  - Tutorial: intensidad calibrada a 2,8 y alcance a 8,5 m.
  - Corrección en la herramienta para asegurar que todas las antorchas existentes actualicen sus valores de intensidad, alcance, color y sombras suaves horneadas.
- **Ambiente base Trilight (`RenderSettings`):**
  - Modo `AmbientMode.Trilight` con tonos cálidos de piedra: cielo `(0.24, 0.22, 0.20)`, ecuador `(0.18, 0.16, 0.14)` y suelo `(0.11, 0.10, 0.09)`. Evita negros puros y baña las zonas indirectas y las sondas de luz con una base visible y acogedora.
- **Ajustes de horneado (`Horneado_Quest.lighting`):**
  - Rebotes indirectos subidos de 2 a 4 (`PVRBounces: 4`).
  - Oclusión ambiental suavizada: `aoExponentIndirect: 0.45` y distancia a 0,5 m. Conserva el volumen y contacto de la piedra sin ennegrecer las juntas.
  - Muestras de entorno a 512 y ligero albedo boost de 1,1 para mayor rebote secundario.
- **Postproceso URP (`PostProceso_Quest.asset`):**
  - `ColorAdjustments`: `postExposure: 0.48f` (ganancia de exposición limpia y equilibrada para visores VR), `contrast: 7f` (suave para preservar detalle y relieve en la piedra), `saturation: 12f`.
  - `SplitToning`: sombras neutras suaves `(0.45, 0.47, 0.52)` y altas luces doradas `(0.65, 0.55, 0.45)`, balance en -5.
  - `LiftGammaGain` (componente nuevo): `lift: (1, 1, 1, 0.025)` para levantar el piso de sombras y `gamma: (1, 1, 1, 0.04)` para medios. Costo de rendimiento 0 en Quest (se fusiona en la misma LUT 32x32x32 del Uber pass).
  - `Vignette`: reducida a 0,14 con suavidad 0,50 para no oscurecer la visión periférica en VR.
- **Rehorneado:**
  - `3_TutorialMazmorra` rehorneada con Progressive GPU (4 lightmaps, 649 sondas de luz).

## Cómo deshacerlo o ajustar

- Para volver a los ajustes anteriores: `git checkout Assets/_Custom/Lighting/ Assets/_Custom/Scripts/Lighting/Editor/QuestLighting.cs Assets/_Custom/Scenes/3_TutorialMazmorra*` y rehornear.
- Para ajustar brillo en caliente: cambiar `Post Exposure` en `Assets/_Custom/Lighting/PostProceso_Quest.asset` desde el Inspector de Unity.


---

# Ambientación medieval y claraboya celestial — Inspirado en Imagen de Referencia 3 (2026-11-04)

Adaptación visual de la mazmorra (`1_GAME` y `3_TutorialMazmorra`, sala de runas) inspirada en la atmósfera cinematográfica, props y entrada de luz de la imagen de referencia facilitada por el usuario.

## Qué se implementó

1. **Claraboya en el techo (`Claraboya_Techo`):**
   - Se retiró el bloque central del techo `Tile_A_22` sobre el altar rúnico (`x = -15, y = 6, z = 16.64`), abriendo un pozo de 4×4 m.
   - Construcción de un tragaluz con 4 muros verticales de piedra escalonada, cornisa de moldura interior y una placa celestial emisiva unlit como cielo exterior visible.
   - **Haz de luz volumétrico (`Haz_Luz_Volumetrico`):** cilindro frustum cónico de 20 caras con 4 planos interiores en cruz (`Haz_Claraboya.asset`, 140 vértices, 384 triángulos). Material transparente aditivo URP Unlit (`Haz_Claraboya.mat`, color celeste pizarra). Gradiente de opacidad suave que se difumina por completo al tocar el suelo. **0 ms de costo de raymarching en Quest** (1 draw call, sin búferes de profundidad pesados).
   - **Luz celestial horneada (`Luz_Claraboya`):** luz tipo Spot horneada (LightmapBakeType.Baked) de color celeste cielo `(0.62, 0.80, 1.0)`, intensidad 22, ángulo 65° con penumbra suave de radio 0,40 m, apuntando diagonalmente hacia el altar central y el inicio de la escalinata. Baña las baldosas de piedra con un reflejo frío que contrasta con el fuego.

2. **Braseros de bronce con fuego (`Braseros`):**
   - 6 braseros ubicados estratégicamente según la referencia visual:
     - 2 en primer plano flanqueando la entrada este.
     - 2 flanqueando la escalinata principal hacia el portal de salida oeste.
     - 2 a los costados del altar central de runas.
   - Modelado procedural del cuenco acampanado de bronce (`Brasero_Cuenco.asset`) con lecho de brasas de carbón incandescente.
   - Fuego de partículas adaptado de `Feel_TorchFire` escalado a 1.8x para llamas majestuosas y vivas.
   - Luz horneada cálida (Point Light Baked, color ámbar fuego, intensidad calibrada, sombras suaves).

3. **Estandartes medievales rúnicos (`Estandartes`):**
   - 6 estandartes colgantes de tela carmesí con orla dorada y corte de cola de golondrina (`Estandarte.asset`), adornados con los glifos geométricos rúnicos de los cuatro elementos (`feel_estandarte_{fuego,agua,tierra,viento}.png`):
     - 2 flanqueando el arco de piedra de la salida oeste.
     - 2 en los muros norte entre las columnas de piedra.
     - 2 en los muros sur entre las columnas de piedra.
   - Soporte de barra cilíndrica de bronce envejecido con remates cónicos.

4. **Utilería y velas (`Utileria_Repisas`):**
   - Repisa de piedra sobre el mural de fuego con 9 velas encendidas de cera blanca con llama cálida.
   - Escudos metálicos con relieve de bronce apoyados contra los muros de la sala.

5. **Ajuste de postprocesado dual-tone (`PostProceso_Quest.asset`):**
   - Calibrado a la paleta cinematográfica de la imagen 3: contraste en 13.5, saturación viva en 15, SplitToning con altas luces ámbar dorado `(0.70, 0.58, 0.40)` y sombras azul pizarra frío `(0.40, 0.45, 0.58)` con balance en -8, y LiftGammaGain con elevación azulada sutil. Al ir en la LUT del postproceso URP, no añade costo extra en VR.

6. **Integración y persistencia:**
   - La herramienta `DungeonAtmosphereBuilder.cs` está vinculada a `DungeonFinaleBuilder.Apply()`. Cada vez que se regenere la escena combinada `3_TutorialMazmorra` desde **Tools > Game Feel > Construir escena combinada**, la claraboya, el haz volumétrico, los braseros, estandartes y utilería se vuelven a construir automáticamente.
   - Escena combinada rehorneada en Unity Progressive GPU (4 lightmaps de 2048, 648 sondas de luz).

## Rendimiento en Meta Quest (VR)

- **0 luces en tiempo real adicionales:** tanto los 6 braseros como la claraboya son 100% horneadas en los lightmaps.
- **Sin sombras en tiempo real ni raymarching volumétrico:** el haz de luz es una malla unlit transparente aditiva ultraligera.
- **Materiales compartidos:** se reutilizaron materiales existentes del pack (`M_Wall`, `M_Tile`, `Altar_Bronce`).

## Cómo probar o deshacer

- **Menú en el Editor:** **Tools > Game Feel > Aplicar claraboya y props de mazmorra (Imagen 3)** para aplicar/regenerar en la escena activa, o **Quitar claraboya y props** para eliminar y restaurar el techo original.
- **Rehornear tras cambios:** **Tools > Iluminación Quest > Hornear escena combinada** (~15–20 segundos en GPU).


---

## Refinamiento: Eliminación de tazones y rediseño de luz del techo (2026-11-04)

Siguiendo el feedback del usuario:
1. **Eliminación total de los tazones/braseros de fuego:**
   - Se suprimieron los 6 tazones/braseros de bronce, pedestales y sus luces horneadas. El espacio de juego vuelve a estar despejado, limpio y amplio para la interacción VR.
2. **Rediseño completo de la luz del techo (God Rays y Claraboya):**
   - **Fin del cono poligonal:** Se eliminó la malla cónica rígida y facetada que producía un efecto de "carpa plástica" con aristas duras.
   - **Textura de haz suave (`feel_godray_beam.png`):** Generada proceduralmente con curva gaussiana horizontal y degradado suave vertical en los cuatro extremos.
   - **Malla de haces cruzados translúcidos:** Malla ultraligera de 32 vértices (`Haz_Claraboya.asset`) que dibuja 4 haces de luz celestial individuales y angulados, sin aristas ni cortes contra la geometría.
   - **Pozo de luz arquitectónico preciso:** Alineado al bloque 4×4 m exacto de `Tile_A_22`, con vigas de piedra transversales de soporte y cielo exterior visible de doble faz.
   - **Rehorneado:** Escena rehorneada en Progressive GPU sin las sombras de los tazones y con reflejo suave de luz de día sobre las losas de piedra.

## Arreglo del build de Android (2026-11-06)

- **Problema:** el build fallaba con `NullReferenceException` en `MetaQuestFeatureBuildHooks.ApplySettingsOverride`. El paquete OpenXR busca un único `MetaQuestFeature` y había dos archivos de ajustes: `Assets/XR/Settings/OpenXR Package Settings.asset` (viejo, sin referencias) y `OpenXRPackageSettings.asset` (el activo).
- **Cambio:** se borró `OpenXR Package Settings.asset` y su `.meta`.
- **Deshacer:** `git checkout -- "Assets/XR/Settings/OpenXR Package Settings.asset" "Assets/XR/Settings/OpenXR Package Settings.asset.meta"`.
