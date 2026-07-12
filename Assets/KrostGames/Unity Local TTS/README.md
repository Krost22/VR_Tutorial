# 🔊 Unity Local TTS

**Text-to-Speech local para Unity 6 usando Windows SAPI — sin dependencias externas, sin APIs en la nube.**

> [!IMPORTANT]
> **100% Local y Gratuito:** Este sistema **no** requiere conexión a internet, **no** utiliza APIs de IA externas (como OpenAI o Azure) y **no** consume tokens. Es una herramienta completamente abierta y gratuita que utiliza los recursos nativos de tu sistema operativo Windows.

Genera archivos de audio `.wav` directamente desde el Editor de Unity utilizando el motor de síntesis de voz nativo de Windows. Los clips generados son assets estándar de Unity, organizados por idioma y listos para usar en cualquier plataforma de build.

---

## 📋 Tabla de Contenidos

- [Requisitos](#-requisitos)
- [Instalación](#-instalación)
- [Cómo Acceder al TTS Generator](#-cómo-acceder-al-tts-generator)
- [Características Principales](#-características-principales)
- [Guía de Uso](#-guía-de-uso)
- [Instalar Voces de Otros Idiomas](#-instalar-voces-de-otros-idiomas)
- [Arquitectura de Scripts](#-arquitectura-de-scripts)
- [Estructura de Carpetas](#-estructura-de-carpetas)
- [Preguntas Frecuentes](#-preguntas-frecuentes)

---

## 🔧 Requisitos

| Requisito | Detalle |
|---|---|
| **Motor** | Unity 6 (cualquier versión LTS) |
| **Sistema Operativo** | Windows 10 / 11 |
| **PowerShell** | v5.1+ (incluido en Windows por defecto) |
| **Voces SAPI** | Al menos una voz instalada en el sistema |

> ⚠️ Este módulo **solo funciona en el Editor de Windows**. Los clips `.wav` generados sí funcionan en cualquier plataforma de build (Android, iOS, WebGL, etc.).

---

## 📦 Instalación

### Opción A — Desde UnityPackage

1. Descarga el archivo `.unitypackage`.
2. En Unity, ve a **Assets → Import Package → Custom Package...**.
3. Selecciona el archivo descargado.
4. Asegúrate de que todos los archivos estén marcados y haz click en **Import**.

### Opción B — Copia manual

1. Copia la carpeta completa `Unity Local TTS` dentro de tu directorio `Assets/`.
2. Espera a que Unity reimporte y compile.

---

## 🖥️ Cómo Acceder al TTS Generator

1. Abre tu proyecto en **Unity 6**.
2. En la barra superior, ve a **Tools → Unity Local TTS → Generator**.

Se abrirá una ventana llamada **"TTS Generator"**. Puedes anclarla en cualquier panel del Editor.

---

## 🚀 Características Principales

- **Multi-idioma (UI & TTS)**: Interfaz y síntesis disponible en Inglés, Español y Portugués.
- **Live Preview**: Escucha el texto al instante sin generar archivos en disco. Incluye botón de parada inmediata.
- **Batch Generation**: Procesa archivos `.txt` o `.csv` para generar decenas de clips automáticamente con una barra de progreso.
- **Auto-Assignment**: Asigna automáticamente el clip generado a un `AudioSource` o `TTSAudioPlayer` seleccionado en la escena.
- **Naming Templates**: Define formatos de nombre personalizados usando tokens como `{lang}`, `{id}` y `{name}`.
- **Organización Automática**: Los archivos se guardan en subcarpetas por idioma (`GeneratedAudio/ES/`, etc.).
- **Historial de Generación**: Acceso rápido a los últimos 10 clips generados para previsualización o localización.

---

## 🎯 Guía de Uso

### 1. Configurar Idioma de la UI
En la esquina superior derecha, selecciona el idioma de la interfaz (**EN, ES, PT**). Toda la ventana se traducirá automáticamente.

### 2. Seleccionar Idioma de Voz
En la sección **Idioma**, elige el idioma del texto. Esto filtrará las voces de tu sistema Windows para mostrar solo las compatibles.

### 3. Entrada de Texto
- **Texto a Hablar**: Escribe tu guion aquí. El campo se ajusta automáticamente y no deforma la UI.
- **Nombre de Archivo**: Opcional. Si usas **Plantillas de Nombre**, se aplicará el formato definido abajo.

### 4. Vista Previa en Vivo (Live Preview)
Haz click en **🔈 Live Preview** para escuchar la voz actual. Si el texto es largo, puedes usar el botón **⏹ Stop** para detener la síntesis en cualquier momento.

### 5. Generación por Lotes (Batch)
1. Despliega la sección **📦 Batch Generation**.
2. Arrastra un archivo `.txt` (una frase por línea) o `.csv` (formato `texto,nombre`).
3. Haz click en **⚡ Generate All Lines**. El sistema ignorará líneas vacías o comentarios (`#`, `//`).

---

## 🌍 Instalar Voces de Otros Idiomas

Si no aparecen voces para el idioma deseado, instálalas en Windows:

1. Ve a **Configuración → Hora e idioma → Voz**.
2. Click en **Agregar voces**.
3. Busca el idioma (ej: `Español (México)` o `Português (Brasil)`).
4. **Instala el paquete de voz** (asegúrate de que incluya el ícono de micrófono/voz).
5. **Reinicia Unity** y haz click en **🔄 Actualizar Voces** en el generador.

---

## 📐 Arquitectura de Scripts

### Runtime
- **`TTSVoiceSettings.cs`**: Almacena configuraciones de voz (rate, volume, language).
- **`TTSAudioPlayer.cs`**: Componente para reproducir los clips generados en el juego.

### Editor
- **`WindowsTTSEngine.cs`**: Motor core. Lanza procesos PowerShell asíncronos para interactuar con SAPI.
- **`TTSLocalization.cs`**: Gestiona las traducciones de la interfaz de la ventana.
- **`TTSEditorWindow.cs`**: Interfaz de usuario con lógica de historial y batching.
- **`TTSEditorUtility.cs`**: Helpers para limpieza de archivos, auto-asignación y organización de carpetas.

---

## 📁 Estructura de Carpetas

```
Assets/Unity Local TTS/
├── 📂 Editor/              # Scripts de herramientas (no incluidos en build)
├── 📂 Runtime/             # Componentes de juego
└── 📂 GeneratedAudio/      # Salida de audios organizada por idioma
    ├── 📂 EN/
    ├── 📂 ES/
    └── 📂 PT/
```

---

<div align="center">

---

**Unity Local TTS V1.0**

Desarrollado por **KrostGames** · 2026

*Software libre, local y sin tokens.*

---

</div>
