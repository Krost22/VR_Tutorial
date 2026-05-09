# 🔊 Unity Local TTS

**Local Text-to-Speech for Unity 6 using Windows SAPI — no external dependencies, no cloud APIs.**

> [!IMPORTANT]
> **100% Local and Free:** This system **does not** require an internet connection, **does not** use external AI APIs (like OpenAI or Azure), and **does not** consume tokens. It is a completely open and free tool that utilizes the native resources of your Windows operating system.

Generate `.wav` audio files directly from the Unity Editor using the native Windows speech synthesis engine. The generated clips are standard Unity assets, organized by language, and ready to be used on any build platform.

---

## 📋 Table of Contents

- [Requirements](#-requirements)
- [Installation](#-installation)
- [How to Access TTS Generator](#-how-to-access-tts-generator)
- [Core Features](#-core-features)
- [Usage Guide](#-usage-guide)
- [Installing Other Language Voices](#-installing-other-language-voices)
- [Script Architecture](#-script-architecture)
- [Folder Structure](#-folder-structure)
- [FAQ](#-faq)

---

## 🔧 Requirements

| Requirement | Detail |
|---|---|
| **Engine** | Unity 6 (any LTS version) |
| **Operating System** | Windows 10 / 11 |
| **PowerShell** | v5.1+ (included in Windows by default) |
| **SAPI Voices** | At least one voice installed on the system |

> ⚠️ This module **only works within the Windows Editor**. The generated `.wav` clips work on any build platform (Android, iOS, WebGL, etc.).

---

## 📦 Installation

### Option A — Via UnityPackage

1. Download the `.unitypackage` file.
2. In Unity, go to **Assets → Import Package → Custom Package...**.
3. Select the downloaded file.
4. Ensure all files are checked and click **Import**.

### Option B — Manual Copy

1. Copy the entire `Unity Local TTS` folder into your `Assets/` directory.
2. Wait for Unity to reimport and compile.

---

## 🖥️ How to Access TTS Generator

1. Open your project in **Unity 6**.
2. In the top menu bar, go to **Tools → Unity Local TTS → Generator**.

A window named **"TTS Generator"** will open. You can dock it in any Editor panel.

---

## 🚀 Core Features

- **Multi-language (UI & TTS)**: Interface and synthesis available in English, Spanish, and Portuguese.
- **Live Preview**: Listen to the text instantly without generating files on disk. Includes an immediate stop button.
- **Batch Generation**: Process `.txt` or `.csv` files to generate dozens of clips automatically with a progress bar.
- **Auto-Assignment**: Automatically assign the generated clip to a selected `AudioSource` or `TTSAudioPlayer` in the scene.
- **Naming Templates**: Define custom naming formats using tokens like `{lang}`, `{id}`, and `{name}`.
- **Automatic Organization**: Files are saved in subfolders by language (`GeneratedAudio/EN/`, etc.).
- **Generation History**: Quick access to the last 10 generated clips for preview or location.

---

## 🎯 Usage Guide

### 1. Set UI Language
In the top right corner, select the interface language (**EN, ES, PT**). The entire window will translate automatically.

### 2. Select Voice Language
In the **Language** section, choose the text language. This will filter your Windows system voices to show only compatible ones.

### 3. Text Input
- **Text to Speak**: Write your script here. The field adjusts automatically and does not deform the UI.
- **File Name**: Optional. If you use **Naming Templates**, the defined format will be applied.

### 4. Live Preview
Click **🔈 Live Preview** to hear the current voice. If the text is long, you can use the **⏹ Stop** button to stop synthesis at any time.

### 5. Batch Generation
1. Expand the **📦 Batch Generation** section.
2. Drag a `.txt` file (one phrase per line) or a `.csv` file (format `text,filename`).
3. Click **⚡ Generate All Lines**. The system will ignore empty lines or comments (`#`, `//`).

---

## 🌍 Installing Other Language Voices

If no voices appear for the desired language, install them in Windows:

1. Go to **Settings → Time & Language → Speech**.
2. Click **Add voices**.
3. Search for the language (e.g., `English (United Kingdom)` or `Spanish (Mexico)`).
4. **Install the voice pack** (make sure it includes the microphone/voice icon).
5. **Restart Unity** and click **🔄 Refresh Voices** in the generator.

---

## 📐 Script Architecture

### Runtime
- **`TTSVoiceSettings.cs`**: Stores voice settings (rate, volume, language).
- **`TTSAudioPlayer.cs`**: Component to play the generated clips in-game.

### Editor
- **`WindowsTTSEngine.cs`**: Core engine. Launches asynchronous PowerShell processes to interact with SAPI.
- **`TTSLocalization.cs`**: Manages window interface translations.
- **`TTSEditorWindow.cs`**: UI with history and batching logic.
- **`TTSEditorUtility.cs`**: Helpers for file cleanup, auto-assignment, and folder organization.

---

## 📁 Folder Structure

```
Assets/Unity Local TTS/
├── 📂 Editor/              # Tool scripts (not included in build)
├── 📂 Runtime/             # Game components
└── 📂 GeneratedAudio/      # Audio output organized by language
    ├── 📂 EN/
    ├── 📂 ES/
    └── 📂 PT/
```

---

<div align="center">

---

**Unity Local TTS V1.0**

Developed by **KrostGames** · 2026

*Free, local, and token-free software.*

---

</div>
