# EditorPets

Mascotas virtuales interactivas dentro de la **Scene View** de Unity. Las mascotas deambulan, duermen, reaccionan al clic, comen y juegan con una pelota. Se configuran mediante `ScriptableObject` y se gestionan desde una ventana dedicada.

> **Version 1.0.0** — First public release for Unity Asset Store.
> Solo funciona en el editor (no se incluye en builds).

---

## Instalación

Copia la carpeta `Assets/KrostGames/EditorPets/` dentro de tu proyecto de Unity. No requiere dependencias externas.

Si la mueves a otra ubicación, asegúrate de que:
- Todos los `.cs` permanezcan dentro de una carpeta llamada `Editor/`.
- El archivo `EditorPets.Editor.asmdef` esté junto a los scripts.
- Las texturas y assets de `Data/` se regeneren las rutas (se autocorrigen en el primer load).

---

## Uso rápido

1. Abre `Tools → Editor Pets Settings`.
2. Pulsa **Spawn Ball** para crear la pelota y **Feed All** para dar comida a todas las mascotas.
3. Crea nuevas mascotas con `Assets → Create → EditorPets → Pet Data`.
4. Arrastra el `PetData` desde el Project a la Scene View para añadirlo a la escena.
5. Click en una mascota para acariciarla. Drag para moverla. Drag fuera de la Scene View para "enviarla a casa" (ocultarla).

---

## Configuración

### Crear una mascota (PetData)

Cada mascota es un `ScriptableObject` con:
- `petName` — nombre que se muestra sobre el sprite.
- `isActive` / `location` — visibilidad y ubicación (`Scene` / `House`).
- 5 spritesheets horizontales: `idleTexture`, `walkTexture`, `sleepTexture`, `eatTexture`, `pettedTexture`.
- `framesXxx` — número de frames en cada spritesheet.
- `animationSpeed` — frames por segundo.
- `moveSpeed` — píxeles por segundo al caminar.
- `size` — tamaño del sprite en píxeles.

> El `PetDataEditor` (inspector personalizado) incluye un preview animado en vivo con tabs por estado y slider de frame count. Si una textura cambia de tamaño, el slider se reajusta automáticamente.

### Configuración global (GlobalPetSettings)

Asset ubicado en `Assets/KrostGames/EditorPets/Data/GlobalPetSettings.asset`. Contiene:
- Texturas globales: corazón (partículas), comida, pelota.
- `ballRadius` — radio físico de la pelota.
- `gravity` — gravedad aplicada a la pelota.

Se crea automáticamente la primera vez que se carga el sistema.

---

## Interacción

| Acción | Efecto |
|--------|--------|
| Click izquierdo en mascota | La acaricia (corazón + animación `petted`) |
| Drag de mascota | La mueve por la Scene View |
| Drag fuera de la Scene View | Inicia un drag & drop del `PetData` (oculta la mascota) |
| Drag de `PetData` desde Project a Scene View | La activa y trae a escena |
| Click y drag en pelota | La lanza con física |
| `Interactable` (toolbar) | Habilita/deshabilita toda interacción |
| `Show Names` | Muestra etiquetas sobre los sprites |
| `Opacity` | Transparencia global de la Scene View |

---

## Estados de la mascota

| Estado | Comportamiento |
|--------|---------------|
| `Idle` | Quieto, transiciona a Walk o Sleep aleatoriamente |
| `Walk` | Camina horizontalmente, rebota en bordes |
| `Sleep` | Duerme 10-20s |
| `Interact` | Tras click, salta ligeramente con corazón (2s) |
| `Drag` | Mientras se arrastra con el mouse |
| `Eat` | Estado tras `Feed All` (4s) |
| `Play` | Persigue la pelota |

---

## Estructura del proyecto

```
Assets/KrostGames/EditorPets/
├── icon.png                           ← Icono del package (512x512)
├── LICENSE.md                         ← All Rights Reserved
├── CHANGELOG.md                       ← Historial de versiones
├── README.md                          ← Este archivo
├── EditorPets_Context.md              ← Documento técnico interno
├── Editor/
│   ├── EditorPets.Editor.asmdef       ← Assembly definition (solo Editor)
│   ├── PetData.cs                     ← ScriptableObject de mascota
│   ├── PetDataEditor.cs               ← Inspector personalizado
│   ├── GlobalPetSettings.cs           ← ScriptableObject de configuración
│   ├── PetController.cs               ← Lógica runtime (estados, animación, draw)
│   ├── ScenePetOverlay.cs             ← Orquestador principal (suscripción a eventos del editor)
│   ├── EditorPetsWindow.cs            ← Ventana de configuración
│   └── WelcomeWindow.cs               ← Ventana de bienvenida (primera instalación)
├── Data/                              ← Assets de ScriptableObject
│   ├── GlobalPetSettings.asset
│   └── Example DataPets/              ← Pets de ejemplo
│       ├── Corgi.asset
│       ├── DefaultDog.asset
│       └── Noah Dog.asset
├── Example scene/                     ← Escena de ejemplo
│   ├── Editor Pets Sample.unity
│   └── Materials/                     ← Materiales de la escena
├── Textures/                          ← Spritesheets y texturas globales
│   ├── Ball.png, Food.png, Heart.png
│   ├── Corgi/ (sprites del Corgi)
│   ├── Legacy Dog/ (sprites del pixel dog)
│   └── Noah Dog/ (sprites del Noah Dog)
└── Marketing/                         ← Screenshots y material de marketing
    ├── cover.png
    └── feature_*.png
```

---

## Sample Scene

La escena `Example scene/Editor Pets Sample.unity` muestra un jardín con casa, cerca, árboles y 3 pets (Corgi, DefaultDog, Noah Dog) pre-colocados.

---

## Changelog

El changelog detallado está en [`CHANGELOG.md`](./CHANGELOG.md).

Resumen:
- **1.0.0** — Primera release pública para Unity Asset Store.

---

## Compatibilidad

- Unity 2021.3 LTS o superior (probado en Unity 6).
- Solo se compila en el editor (carpeta `Editor/` + `.asmdef` con `includePlatforms: ["Editor"]`).
- Compatible con dark mode y light mode del editor.

---

## Licencia

All Rights Reserved. Ver [`LICENSE.md`](./LICENSE.md) para los términos completos.
