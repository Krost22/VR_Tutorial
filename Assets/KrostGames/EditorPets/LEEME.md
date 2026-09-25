# Editor Pets

Mascotas en pixel art que viven en tu **Scene View**. Pasean, duermen, comen, juegan con una pelota y reaccionan cuando las acaricias. Crea tu propia mascota a partir de un sprite sheet en dos clics.

> **Versión 1.0.0** · Solo editor (no se incluye en tus builds) · English: [`README.md`](./README.md)

---

## Inicio rápido

1. Importa el paquete. Se abre la ventana de bienvenida y las mascotas empiezan a pasear por la parte baja de la Scene View.
2. Abre **Tools → Editor Pets → Settings** para mostrar/ocultar mascotas, soltar una pelota o darles de comer.
3. En la Scene View: **clic** en una mascota para acariciarla, **arrástrala** para moverla, **arrastra la pelota** para lanzarla.

La carpeta `EditorPets` puede estar en cualquier lugar del proyecto (`Assets/...` o `Packages/...`).

---

## Crea tu propia mascota

1. **Dibuja un sprite sheet**: todos los frames uno al lado del otro en **una sola fila**, cada frame **cuadrado**.
   Ejemplo: 4 frames de 32×32 → un PNG de 128×32.
2. **Solo Idle es obligatorio.** Para más animaciones haz un PNG por estado y pon el estado en el nombre del archivo:

   | Estado | Palabra en el nombre | Si falta... |
   |--------|---------------------|-------------|
   | Idle | `idle` (o cualquier otro nombre) | — |
   | Walk | `walk` o `run` | usa Idle |
   | Sleep | `sleep` | usa Idle |
   | Eat | `eat` | usa Idle |
   | Petted | `happy` o `petted` | usa Idle |

   Ej.: `Gato_Idle.png`, `Gato_Walk.png`, `Gato_Sleep.png`.
3. **Selecciona los PNG** en la ventana Project y pulsa **+ Nueva Mascota** en la ventana Editor Pets
   (o clic derecho → **Create → EditorPets → New Pet**). El asset se crea junto a los sprites.
4. **Elige su comida** con un clic en la fila de comidas de su inspector.

Listo: la mascota aparece en la Scene View. Los frames se detectan solos (ancho ÷ alto); escribe un número en `Frames` solo si los frames no son cuadrados (0 = automático). Velocidad y tamaño están en el mismo inspector, con preview animado de cada estado.

> Consejo: pon cada mascota en su propia carpeta dentro de `Pets/`, como las incluidas, para que asset y sprites vayan juntos.

---

## Comida

Cada mascota come su propia comida al pulsar **Alimentar**. La librería está en `Items/Food/`:

Apple · Bone · Bowl (por defecto) · Carrot · Cheese · Cookie · Egg · Fish · Leaf · Meat · Mooncake · Popsicle · Seeds · Shrimp · Strawberry · Watermelon

Suelta cualquier PNG en `Items/Food/` y aparece en el selector de comida de todas las mascotas. Las que no tengan comida usan el tazón por defecto (**Ajustes → Pelota, Comida y Corazón**).

---

## Ventana Editor Pets

**Cabecera:** contador (`19 mascotas · 4 visibles`), idioma EN/ES y **?** (cómo crear una mascota).
**Barra:** **+ Nueva Mascota**, **Crear Pelota**, **Alimentar** e **Interactuable** (desactívalo si las mascotas estorban al hacer clic).

**Pestaña Mascotas**, pensada para muchas mascotas:
- Cuadrícula de miniaturas animadas (caminan al pasar el mouse); las ocultas se ven atenuadas.
- **Buscar** por nombre y filtrar **Todas / Visibles / Ocultas**.
- El **ojo** de cada tarjeta la muestra/oculta. **Mostrar todas / Ocultar todas** bajo la cuadrícula (Ctrl+Z deshace).
- Clic = seleccionar, doble clic = seleccionar el asset, clic derecho = mostrar/ocultar, **mostrar solo esta**, mover, duplicar.
- Panel de detalle (arrastra el divisor): **Visible**, **Solo**, **Mover**, **Duplicar**, **Asset** y el inspector de la mascota.

**Pestaña Ajustes:** **Mostrar Nombres**, **Opacidad**, y pelota, comida por defecto y corazón.

La ventana sigue el tema claro/oscuro del editor.

---

## En la Scene View

| Acción | Efecto |
|--------|--------|
| Clic en una mascota | La acaricia (corazón + animación Petted) |
| Arrastrar una mascota | La mueve |
| Arrastrarla fuera de la Scene View y soltarla en la ventana Editor Pets | La oculta |
| Arrastrar un asset de mascota desde Project a la Scene View | La muestra |
| Arrastrar la pelota | La lanza |

Estados: Idle, Walk, Sleep (10–20 s), Petted (2 s tras un clic), Drag, Eat (4 s), Play (persigue la pelota).

---

## Mascotas incluidas

| Mascota | Comida | Mascota | Comida |
|---------|--------|---------|--------|
| Corgi | Hueso | Red Dragon | Carne |
| Noah | Hueso | Ice Dragon | Paleta helada |
| Pixel Dog | Hueso | Rock Pigeon | Semillas |
| Tabby Cat | Pescado | White Dove | Semillas |
| Mishy (gato negro atigrado y gordito) | Pescado | Mourning Dove | Semillas |
| Turtle | Hoja | Crowned Pigeon | Semillas |
| Deer | Manzana | Nicobar Pigeon | Semillas |
| Crab | Camarón | Clawdito | Galleta |
| Snake | Huevo | Kimoon | Pastel de luna |
| Capybara | Sandía | | |

---

## Estructura

```
EditorPets/
├── README.md, LEEME.md, CHANGELOG.md, LICENSE.md
├── Editor/                       Código solo de editor (EditorPets.Editor.asmdef)
│   ├── Scripts/                  C# (namespace EditorPets)
│   └── UI/                       UXML/USS de la ventana + icono
├── Items/
│   ├── Ball.png, Heart.png
│   ├── GlobalPetSettings.asset   física de la pelota + comida/corazón/pelota por defecto (se recrea si falta)
│   └── Food/                     librería de comidas
└── Pets/<Nombre>/                una carpeta por mascota: asset PetData + sus sprite sheets
```

---

## Compatibilidad

- Unity 2022.3 LTS o superior (probado en Unity 6).
- Ensamblado solo de editor (`includePlatforms: ["Editor"]`); funciona con cualquier render pipeline.
- Temas claro y oscuro del editor.

## Licencia

Ver [`LICENSE.md`](./LICENSE.md). Historial: [`CHANGELOG.md`](./CHANGELOG.md).
