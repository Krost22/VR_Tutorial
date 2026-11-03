Actúa como Senior QA Game Tester de VR. Proyecto Unity `VR_Tutorial` (Unity 6000.4.2f1, URP 17.4, AutoHand, OpenXR; objetivo Meta Quest). Escena: `Assets/_Custom/Scenes/3_TutorialMazmorra.unity` (índice 0 del build).

ALCANCE: SOLO la parte dos, la sala de runas de la mazmorra (objeto raíz `Mazmorra`): runas, altar, pilares, murales, piedras del arco, pared secreta, puerta principal y portal final. NO pruebes el tutorial (mirada, círculos, bandera, arañas, canasta, golem). Para llegar rápido usa el atajo `TutorialSkip` (mantener K 2 s en Play mode) o teletransporta al jugador con `AutoHandPlayer.SetPosition` frente al pasillo de la mazmorra.

Contexto: `AGENTS.md` (sección "Sala de runas") y `Docs/CAMBIOS_GameFeel.md` (secciones del 2026-11-03).

MODO ESTRICTO SOLO LECTURA: no modifiques, guardes ni crees archivos ni escenas del proyecto. Puedes entrar en Play mode y simular acciones por script (sin guardar). Sin visor la cámara XR queda en y=0: para capturas usa una cámara temporal o mueve la cámara y desactiva su TrackedPoseDriver solo en Play mode.

## Flujo esperado

1. Las runas de Fuego y Agua están en la sala. Las de Tierra y Viento están encerradas tras `SecretWall`.
2. Cada runa va en su hueco del altar `Altar_Runas` (`PlacePoint Fuego/Agua/Tierra/Viento`). Con la runa correcta, su pilar (`PillarProjector`) se enciende y se puede girar.
3. El rayo del pilar debe apuntar 2 s a su mural del mismo color; entonces el pilar se fija.
4. Fase 1 (Fuego + Agua fijados): se abre `SecretWall`.
5. Fase 2 (los 4 fijados): se abre `Main Door To Section 2` y aparece `Portal_Final`.
6. Al cruzar el portal: sala "¡Gracias por jugar!", cuenta atrás de 8 s y reinicio.

## 1. Runas: cada una distinta (`RuneVisuals`, `RuneGlyphs`)

- Cada runa lleva su símbolo en las 6 caras: △ con llama (Fuego), ▽ con ola (Agua), ▽ con barra y punto (Tierra), △ con barra y anillo (Viento). Comprueba con capturas cercanas (≈0,6 m) que:
  - no quedan restos de la estrella vieja en el color ni en el relieve;
  - el símbolo no sale cortado ni deformado en ninguna cara;
  - las cuatro se distinguen aunque se giren 180°.
- Aura de partículas (hijo `Aura`): brasas que suben (Fuego), gotas que caen (Agua), motas en órbita (Tierra) y remolino (Viento). Comprueba que:
  - se ven y no atraviesan ni se esconden dentro del cubo;
  - siguen a la runa al moverla (Fuego y Agua quedan atrás, en espacio de mundo);
  - se encogen cuando la runa desaparece al volver a su sitio (`ReturnHomeWhenDropped`);
  - el número de partículas vivas por runa no pasa de 24.
- Runa soltada en el suelo o lanzada fuera de la sala: vuelve sola a su sitio, sin errores. Tras tres retornos seguidos sin tocarla queda quieta flotando: es intencional.

## 2. Altar y pilares (`PillarProjector`)

- Runa correcta en su hueco: el pilar se activa (`IsActivatedByAltar`), se puede agarrar y aparece el rayo.
- Runa equivocada: el pilar no se activa, el anillo del altar da un destello rojo (`RuneAltarGlow`) y la piedra del arco sigue apagada.
- Imán `AssistAngle` 10°: un pilar soltado a 5–9° de `PerfectRotationY` se alinea solo y se fija. A más de 12° no se mueve. Mientras está agarrado (`Grabbable.IsHeld`) el imán no actúa.
- `MissGraceTime` 0,3 s: una salida breve del rayo no reinicia `LockProgress01`; una salida larga sí.
- Re-fijado rápido: con un pilar fijado, quita su runa (el pilar se suelta y su piedra se apaga) y vuelve a ponerla. Si sigue apuntando a su mural debe fijarse en unos 0,3 s, no en 2 s. Mídelo frame a frame.
- Pilares fijados: empujarlos o golpearlos no debe desviar el rayo de su mural.
- Comprueba que el `PerfectRotationY` de cada pilar apunta a su mural del mismo color (rayo y color del mural).

## 3. Piedras del arco (`Wall_B_1 Puerta seccion 1/Arch_A_1/Piedras_Arco`, `ArchElementStones`)

- Al empezar, las 4 apagadas: roca con el símbolo en bajorrelieve, sin brillo.
- Desde la sala: abajo a la izquierda Fuego (rubí), arriba a la izquierda Tierra (ámbar), arriba a la derecha Viento (esmeralda), abajo a la derecha Agua (zafiro).
- Cada piedra se enciende solo cuando se fija su pilar (brillan el símbolo y las vetas, no la losa entera). `LitCount` debe ir 0 → 2 → 4.
- Mientras su pilar carga, la piedra parpadea tenue. Al quitar una runa, su piedra se apaga.
- Con las 4: la ola de luz se dispara una sola vez.
- Toma capturas del arco apagado, a medias y completo desde unos 4 m. ¿Se leen los símbolos? ¿Algún color se confunde con la luz de las antorchas?

## 4. Fases, puerta y portal

- Fuerza órdenes distintos: los 4 pilares en cualquier orden, Tierra o Viento antes de la fase 1 (por script) y runas quitadas después de abrir la puerta. Ninguna combinación debe bloquear la partida ni abrir algo antes de tiempo.
- Las runas de Tierra y Viento solo deben poder sacarse a mano después de abrir `SecretWall`.
- Puerta principal y portal: animación, sonido y portal tras unos 1,6 s. Al cruzarlo: fundido, sala final, cuenta atrás y reinicio limpio (sin errores al recargar, salvo el aviso conocido de AutoHand).

## 5. Jugador en la sala

- Accesibilidad a pie con `AutoHandPlayer` (sin atravesar paredes ni barandillas): altar, pilares inferiores, escalera central → balcón con los pilares superiores y la puerta, y compartimento secreto.
- Escalera: el jugador tiene `useSmoothStep` activado. Al subir y bajar, la altura del cuerpo debe cambiar de forma suave y no a saltos por peldaño (registra la altura frame a frame).
- Rescate por caída (`GameFeel/RescateCaida`, `PlayerFallRescue`): saca al jugador del balcón o fuera de la sala y comprueba que el rescate lo devuelve a suelo firme de la mazmorra. Un teletransporte al vacío no debe causar rescates en bucle. El portal a la sala de y = -60 no debe disparar ningún rescate.

## Falsos positivos ya revisados (no reportar salvo que encuentres algo nuevo)

- El pergamino de pistas sí vuelve solo a su atril (`ReturnHomeWhenDropped`).
- Los cubos de la bandera ya no tienen la etiqueta `BasketBall` (y la bandera no es parte del alcance).
- Solo el hueco de Fuego quita el Rigidbody de la runa al colocarla. Es configuración de AutoHand y ya no produce errores.
- Avisos CS0618 (API obsoleta) del Editor y "Some objects were not cleaned up" de AutoHand al cambiar de escena.

## Formato del informe

- **Resumen ejecutivo:** tabla de severidades (Crítico, Alto, Medio, Bajo).
- **Bugs ordenados por severidad.** Cada uno con: ubicación (ruta en la jerarquía), pasos para reproducir, esperado, obtenido, frecuencia, evidencia (log, valores o captura) y causa sospechada (archivo:línea).
- **Flujos que funcionaron.**
- **Pruebas no realizadas,** con la justificación.
- **Observaciones de UX:** separadas de los bugs.

Distingue lo verificado en Play mode de lo deducido leyendo código. Antes de reportar algo como bug, confirma que no es intencional según el código o `Docs/CAMBIOS_GameFeel.md`.
