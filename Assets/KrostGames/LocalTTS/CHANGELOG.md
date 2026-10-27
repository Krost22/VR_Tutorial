# Changelog

All notable changes to Local TTS are documented in this file.

## [1.1.0] - 2026-10-07

### Added
- **Real-time speech at runtime, no bake needed:** `LocalTTS.Speak(text)` in one line, plus `SynthesizeAsync`, `SynthesizeDataAsync`, `Prepare`, `Prewarm` and `GetVoicesAsync`. Works in the Windows Editor and in Windows builds.
- **TTS Speaker** component: lines with per-line preview and bake, voice profile or inline voice, Auto / Realtime Only / Baked Only modes, Interrupt / Queue / Ignore, Speak on Start, Prepare on Start, `OnSpeechStarted` / `OnSpeechFinished` events and a `Loudness` value for lip-sync or meters. Every action can be wired from UnityEvents.
- **Line Library:** lines spoken from code in Play Mode are recorded and can be baked in one click, so the same code also speaks on platforms without real-time speech. Loaded from Resources, no setup in builds. A build warning reports unbaked lines.
- **OneCore voices** (Windows 10/11, Settings > Speech) next to the SAPI 5 Desktop voices, so far more voices and languages are available.
- **Pitch** control (rate, pitch and volume for every voice) and **SSML** input.
- **Silence trimming:** removes the ~0.1 s before and ~0.7 s after speech that Windows voices add.
- **Generator window rebuilt with UI Toolkit:** engine status, language filter for voices, voice profiles (load / save), naming template preview, assign to selection, last-clip card, non-blocking batch with progress and cancel, history of the last 20 clips, drag and drop of clips onto any AudioClip field, `Ctrl+Enter` preview, follows the light and dark Editor themes.
- Inspectors for TTS Speaker, Line Library and Voice Profile with an installed-voice picker and a ▶ test button. The speaker Inspector warns about lines that would be silent on the active build target.
- Menus: GameObject > Audio > TTS Speaker, AudioSource context menu > Add TTS Speaker, Tools > Local TTS > Line Library / Bake Speakers in Open Scenes / Open Demo Scene / Documentation.
- Demo scene (`Demo/LocalTTS_Demo.unity`) with real-time speech, three speakers with baked lines (EN, ES, PT) and an animated face driven by `Loudness`.
- English and Spanish PDF manuals, `LEEME.md`, `LICENSE.md`, `package.json`.

### Changed
- The product is now called **Local TTS**. Menus are under **Tools > Local TTS** and **Assets > Create > Local TTS**, the package installs into `Assets/KrostGames/LocalTTS`, and the manuals are `LocalTTS_Manual_EN.pdf` / `LocalTTS_Manual_ES.pdf`. Assembly names, namespaces and asset GUIDs did not change.
- One persistent background speech process replaces a new PowerShell process per line: a line now takes tens of milliseconds instead of about a second, and the Editor never freezes while generating.
- Generation is asynchronous. The synchronous 1.0 methods of `WindowsTTSEngine` are kept for existing scripts.
- Output folder is resolved from the package location (and can be changed), so the package can live anywhere in `Assets/`.
- Settings persist: project settings in `ProjectSettings/KrostGames.LocalTTS.asset`, the Generator's state in `UserSettings/`, the UI language per user.
- Namespaces are now `KrostGames.TTS` and `KrostGames.TTS.EditorTools`; assemblies are `KrostGames.LocalTTS` and `KrostGames.LocalTTS.Editor`.
- `TTSLanguage` adds French, German, Italian, Japanese, Chinese, Korean and Russian; any installed voice can also be chosen by name.
- `TTSVoiceSettings` is now the Voice Profile asset (adds pitch) and is used by speakers and the Generator.
- The example clips moved from `GeneratedAudio/Examples` to `Demo/Audio/Examples`.

### Fixed
- Clips were written to a hard-coded `GeneratedAudio` path instead of the package's folder once the package was moved, e.g. into `Assets/KrostGames`.
- Text containing double quotes, line breaks or very long text broke the PowerShell command line.
- A voice that could not be selected was silently replaced by the default voice; engine errors are now reported.
- Reading the PowerShell error stream before waiting could block, and the 30 s timeout never applied.
- In `.txt` batch files any line with a comma was split into text and file name ("Hola, bienvenido" became the text "Hola"). CSV now supports quotes, `""`, line breaks inside quotes, `;` separators and a header row.
- File names typed by the user could contain invalid characters or folder separators, or end in `.wav.wav`; automatic names kept commas and accents.
- *Clear All* permanently deleted every `.wav` under the output folder, including the shipped examples. It now moves only generated clips to the OS trash after confirmation.
- The Live Preview process kept playing after closing the window or recompiling.
- The window repainted continuously during preview and scanned the disk on every repaint.
- The UI language and window state reset after every recompile; several labels were not translated.
- "Generating…" never showed because generation blocked the Editor.
- `TTSAudioPlayer.Play()` / `Stop()` threw if called before `Awake`.
- The engine no longer hangs a domain reload: the speech process is stopped before assemblies reload and when the Editor or game quits.

### Documentation
- Removed the link to a FAQ section that did not exist, and added a real FAQ.
- The usage guide now covers generating clips, naming templates, assignment, history, batch, speakers, the Line Library and scripting.
- Corrected the folder structure (`Assets/KrostGames/LocalTTS`), the PowerShell requirement (Windows PowerShell 5.1, not "5.1+"), and claims that generation was asynchronous, that the text field resized itself and that `TTSVoiceSettings` stored settings.
- Restarting Unity after installing a voice is not needed (press ↻). The 1.0 PDF advice to run `Set-ExecutionPolicy Bypass` was removed: it lowers system security and the asset never needed it.
- Licensing wording now matches LICENSE.md.

## [1.0.0] - 2026-05-08

### Added
- First release: Editor window that generates `.wav` files with Windows SAPI voices (English, Spanish, Portuguese), live preview, batch generation from `.txt` / `.csv`, naming templates, auto-assignment to the selected object, history of the last 10 clips, UI in English, Spanish and Portuguese, and the `TTSAudioPlayer` component.
