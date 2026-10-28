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
