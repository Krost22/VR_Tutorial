Actúa como Senior QA Game Tester de VR. Proyecto Unity `VR_Tutorial` (Unity 6000.4.2f1, URP 17.4, AutoHand, OpenXR; objetivo Meta Quest). Escena a probar: `Assets/_Custom/Scenes/3_TutorialMazmorra.unity` (índice 0 del build). Contexto del proyecto en `AGENTS.md` y cambios recientes en `Docs/CAMBIOS_GameFeel.md` (secciones del 2026-11-03).

MODO ESTRICTO SOLO LECTURA: no modifiques, guardes ni crees archivos del proyecto ni escenas. Puedes entrar en Play mode y simular acciones por script (sin guardar). Sin visor la cámara XR queda en y=0: si necesitas mirar, mueve la cámara y desactiva su TrackedPoseDriver solo en Play mode.

## 1. Regresión de los arreglos del informe anterior

1. BUG-01 (runas): coloca y quita las 4 runas (`CuboArcano-Fuego/Agua/Tierra/Viento`) en sus huecos del altar (`PlacePoint Fuego/Agua/Tierra/Viento`). Esperado: ningún error "Can't remove Rigidbody". La de Fuego pierde el Rigidbody al colocarla y lo recupera al quitarla. Una runa soltada en el suelo vuelve sola a su sitio (`ReturnHomeWhenDropped`).
2. BUG-02 (canasta): llama varias veces seguidas a `HoopDetector.OnTriggerEnter` con el mismo balón. Esperado: +1 punto por tiro (`sameBallCooldown` = 1,2 s). Un balón distinto sí suma.
3. BUG-03 (círculos): al pisar un círculo de `MovementTutorial`, el sonido no sale por el AudioSource `Voice 11` (narración).
4. BUG-04 (caída): con el jugador bajo el suelo o fuera del mapa, `GameFeel/RescateCaida` (`PlayerFallRescue`) hace fundido y lo devuelve al último suelo firme (`RescueCount` sube a 1). Comprueba también que NO salte con los teletransportes legítimos: círculos, atajo `TutorialSkip` (mantener K en el Editor) y portal final a la sala de y = -60.

## 2. Parte dos: sala de runas (prioridad alta)

Flujo: runas en el altar → cada pilar se enciende y se puede girar → su rayo debe apuntar 2 s a su mural del mismo color → el pilar se fija. Fase 1 (Fuego + Agua) abre `SecretWall` con las runas de Tierra y Viento. Fase 2 (los 4) abre `Main Door To Section 2` y activa `Portal_Final`.

1. Piedras del arco (`Wall_B_1 Puerta seccion 1/Arch_A_1/Piedras_Arco`, `ArchElementStones`):
   - Al empezar, las 4 apagadas.
   - Cada una se enciende solo cuando su pilar se fija: rojo = Fuego, amarillo = Tierra, verde = Viento, azul = Agua. Comprueba que el color y la posición coinciden con el pilar fijado.
   - `LitCount` debe dar 0 → 2 → 4.
   - Mientras un pilar carga (apuntando antes de fijarse), su piedra parpadea tenue.
   - Quitar una runa con el pilar fijado apaga su piedra; volver a ponerla la enciende.
   - Con las 4 encendidas, la ola de luz se dispara una sola vez.
   - Toma capturas del arco en esos estados y mira si los colores apagados y encendidos se distinguen con la luz de las antorchas.
2. Ayudas de los pilares (`PillarProjector`):
   - `AssistAngle` 10°: suelta un pilar activo a 5-9° de `PerfectRotationY`. Debe alinearse solo y fijarse en unos 3 s.
   - Suéltalo a más de 12°: no debe moverse solo.
   - Con el pilar agarrado (`Grabbable.IsHeld`), el imán no debe actuar.
   - `MissGraceTime` 0,3 s: una salida breve del rayo no reinicia `LockProgress01`; una salida larga sí.
   - Un pilar sin su runa no debe poder girarse ni proyectar.
3. Runa equivocada en un hueco: el pilar no se activa, el anillo del altar da destello rojo y la piedra del arco sigue apagada.
4. Orden alternativo: intenta fijar Tierra o Viento antes de la fase 1. Sus runas están tras la pared secreta: ¿se puede, y qué pasa? Prueba también a fijar los 4 en cualquier orden.
5. Robustez:
   - Empuja los pilares fijados (deben seguir quietos y con el rayo en su mural).
   - Retira runas después de abrir la puerta.
   - Lanza runas fuera de la sala.
   - Pisa el pergamino de pistas.
   - Cruza el portal: fundido, "¡Gracias por jugar!", cuenta atrás y reinicio limpio de la escena.
6. Accesibilidad física: con el jugador (`AutoHandPlayer`), comprueba que se pueda llegar a pie (sin atravesar paredes) al altar, a los 4 pilares (dos están en el balcón, subiendo la escalera central), al compartimento secreto y al portal.

## 3. Pase rápido del tutorial completo (humo)

Mirada guiada (5 luces) → círculos → bandera de Colombia → arañas con martillo → canasta → golem hasta la meta → pared que se abre → pasillo → sala de runas. Anota cualquier error o aviso nuevo de consola (ignora los avisos conocidos: AutoHand "Some objects were not cleaned up" al cambiar de escena y los CS0618 de API obsoleta del Editor).

## 4. Formato del informe

- **Resumen ejecutivo:** tabla de severidades (Crítico, Alto, Medio, Bajo).
- **Metodología.**
- **Bugs ordenados por severidad.** Cada uno con: escena y ubicación (ruta en la jerarquía), pasos para reproducir, esperado, obtenido, frecuencia, evidencia (log, valores o captura) y causa sospechada (archivo:línea).
- **Flujos que funcionaron.**
- **Pruebas no realizadas,** con la justificación.
- **Observaciones de UX:** separadas de los bugs.

Distingue lo verificado en Play mode de lo deducido leyendo código. No reportes como bug un comportamiento que el código documenta como intencional sin explicar por qué igualmente es un problema.
