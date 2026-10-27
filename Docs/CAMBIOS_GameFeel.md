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
- Está al final de Build Settings (activa). El juego sigue empezando en `2_Adaptacion`; si quieres que empiece en la combinada, muévela al primer lugar.
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
