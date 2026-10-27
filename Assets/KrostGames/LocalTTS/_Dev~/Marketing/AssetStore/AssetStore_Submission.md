# Local TTS — Asset Store submission kit (1.1.0, resubmission)

## What the review asked for, and what changed

| Review note | Fix |
|---|---|
| **Contains Unity / Asset Store mention** in the title | The product is now **Local TTS**. Renamed everywhere: package title, `package.json` displayName, folder `Assets/KrostGames/LocalTTS`, menus (**Tools > Local TTS**, **Assets > Create > Local TTS**), window headers, demo scene, README / LEEME / CHANGELOG / LICENSE, PDF manuals (`LocalTTS_Manual_EN.pdf`, `LocalTTS_Manual_ES.pdf`) and the trailer. The asmdef files were renamed to `KrostGames.LocalTTS(.Editor).asmdef`; assembly names, namespaces and GUIDs did not change. The three demo example clips were regenerated with text that does not name the product. |
| **Image quality required** | New key images at the exact sizes: minimal design on a dark, flat background with one accent colour (mint), focused on the title and publisher. Every image is drawn from code (HTML/SVG, `../../Tools/keyimages/keyimages.mjs`) or is a real Editor capture. No AI image generation, no Unity logos, no default skybox. |
| **Excess text in marketing images** | The key images (Icon, Card, Cover, Social) contain no text at all. Feature text lives in the screenshots and the description. |
| **AI disclosure required** | Text for the **AI Description** field below. |

## Files for the Publisher Portal

| File | Portal field | Size / rule |
|---|---|---|
| `KeyImages/Icon_160x160.png` | Icon image | 160×160, no text |
| `KeyImages/Card_420x280.png` | Card image | 420×280, no text |
| `KeyImages/Cover_1950x1300.png` | Cover image | 1950×1300, no text |
| `KeyImages/Social_1200x630.png` | Social media image | 1200×630, no text |
| `KeyImages/Logo_512x512.png` | Logo (optional) | 512×512 |
| `KeyImages/Logo_120x120.png` | Small logo / avatar (optional) | 120×120, no text |
| `Screenshots/01…08_*.png` | Screenshots | 8 × 1920×1080 |
| `Trailer_1920x1080.mp4` | Video (upload to YouTube or Vimeo and paste the link) | 44.5 s, 1920×1080 at 60 fps, 268 MB (H.264 + AAC). **Replace the old video link**: the old trailer shows the old name. A 540p preview is in `../LocalTTS_Trailer_preview_540p.mp4`. |

| `Walkthrough_1920x1080.mp4` | Second video (optional): the tool used in the Editor | ~78 s, 1080p30. A real screen recording of a clean Unity 6.4 project with only Local TTS imported: Generator (type, preview, save .wav, Spanish voice, pitch), TTS Speaker (preview, add a line, bake) and the demo scene in Play Mode. The cursor, zoom and captions were added in editing; the voices are the exact clips that played. |

| `../../Build/LocalTTS_1.1.0.unitypackage` | Not uploaded (the Uploader exports the folder itself); for testing or direct delivery | 4.3 MB, 57 files under `Assets/KrostGames/LocalTTS`, no `_Dev~`. Batch-imported into a clean Unity 6.4 URP project: 0 compile errors. |

Key images have no text at all (the mark as a speaking character; bubble waveforms are real voice envelopes); the earlier versions with title / publisher are in `KeyImages/WithText/`. All images are 24-bit PNG without alpha. To rebuild: `node _Dev~/Tools/keyimages/keyart.mjs` (key images) and `keyimages.mjs` (screenshots) (needs Chrome and ffmpeg). The screenshots in `Tools/keyimages/shots/` were captured from the Editor and the demo scene in Play Mode.

## Validator result

Asset Store Publishing Tools 12.0.5, type *UnityPackage*, path `Assets/KrostGames/LocalTTS`, category Tools: **all checks pass**, including *Check Package Naming*, *Check Demo Scenes* and *Check Documentation*. One informational warning, *Check Static Variables*: it lists every script with static fields or events so you can check them when "Enter Play Mode Options" disables domain reload. It does not block the submission.

---

## Listing copy (English, paste into the Publisher Portal)

**Package name:** Local TTS – Offline Text-to-Speech
(Keep "Unity" and "Asset Store" out of the name. "Local TTS" alone also works.)

**Category:** Tools / Audio

**Short description:**
Offline text-to-speech with the voices already installed in Windows. Speak in real time, or bake lines into .wav clips that play on every platform. No cloud, no API keys, no tokens.

**Description:**

Give your characters, tutorials and menus a voice without leaving the Editor and without any online service. **Local TTS** uses the text-to-speech voices that ship with Windows, so it is free per line, works offline and never sends your text anywhere.

**Real-time speech, no bake.** `LocalTTS.Speak("Hello!")` synthesizes and plays a line on the spot in the Windows Editor and in Windows builds. Repeated lines come from a memory cache and play instantly.

**Bake once, play everywhere.** Turn any line into a standard .wav AudioClip that plays on Android, iOS, WebGL, consoles, macOS and Linux. A TTS Speaker in Auto mode picks the right option on each platform and its Inspector warns about lines that would be silent on the active build target.

**TTS Speaker component.** Write lines in the Inspector, listen with ▶, bake with one click and call Speak() from UI buttons, XR interactions, Timeline or code. Speech goes through the GameObject's AudioSource, so it can be 3D and routed to a mixer. Events for speech started / finished and a Loudness value for lip-sync or meters.

**Line Library.** Lines your code speaks in Play Mode are recorded automatically. Bake them once and the same code speaks on platforms without real-time synthesis.

**Generator window.** Type, preview from memory, save .wav files, batch-generate from .txt or .csv, use naming templates and drag clips from the history onto any AudioClip field.

**Features**
- Every Windows voice: SAPI 5 "Desktop" voices and the Windows 10/11 OneCore voices, in any language you install.
- Rate, pitch and volume per line, shareable Voice Profiles and SSML input.
- Silence trimming, so clips start and end on the voice.
- Editor UI in English, Spanish and Portuguese; follows the light and dark themes.
- Demo scene with real-time speech, three speakers with baked lines and an animated face driven by Loudness.
- Clean, documented C# (namespace `KrostGames.TTS`), runtime and editor assemblies.
- English and Spanish PDF manuals.

**Technical details**
- Unity 2022.3 LTS or newer (tested on Unity 6.3, 6.4 and 6.5).
- Generating speech: Windows 10 or 11, in the Editor or in Windows builds. Uses Windows PowerShell 5.1, which ships with Windows. No internet connection needed.
- Playing baked clips: every platform.
- Real-time speech is not available on non-Windows platforms; baked clips are used there.
- Works with Built-in, URP and HDRP. No external dependencies; the demo UI uses uGUI.
- The voices belong to Microsoft and are not included; check that the Windows license terms allow your use of the generated audio.
- Documentation: `Documentation/LocalTTS_Manual_EN.pdf` (Spanish: `LocalTTS_Manual_ES.pdf`).

**Keywords:** tts, text to speech, voice, speech, offline, narrator, dialogue, voice over, accessibility, windows voices, sapi, audio clip, lip sync, subtitles, npc

**Release notes (1.1.0):** Real-time speech at runtime, TTS Speaker component, Line Library, OneCore voices, pitch and SSML, silence trimming, rebuilt Generator window, inspectors, demo scene and manuals. The product is now named Local TTS (menus under Tools > Local TTS). See CHANGELOG.md.

## AI Description (paste into the "AI Description" field)

> Review this text before submitting and adjust it if you used other AI tools than the ones listed.

This package was created with the assistance of AI tools. An AI coding assistant (Claude, by Anthropic) was used to help write, refactor and review parts of the C# source code and the Editor UI files (UXML/USS), to draft the documentation (README, manuals, changelog), and to write the scripts that compose the marketing images and the trailer. All AI-assisted content was reviewed, tested and edited by the publisher.

The marketing images and the trailer are built from real Editor screenshots and hand-specified vector shapes; no AI image or video generators were used. All speech in the demo scene, the example clips and the trailer was synthesized offline with the text-to-speech voices built into Microsoft Windows, using this package; no generative AI voice models are used, and the package itself does not include or call any AI service.

---

## Guía de reenvío (paso a paso)

> Estos pasos usan tu cuenta de publisher, así que los haces tú.

1. **Prueba el paquete** en un proyecto vacío (Unity 2022.3 LTS y Unity 6): compila sin errores, aparece **Tools > Local TTS > Generator** y la escena `Demo/LocalTTS_Demo.unity` habla.
2. **Valida** (Tools > Asset Store > Validator): tipo *UnityPackage*, ruta **`Assets/KrostGames/LocalTTS`**. `_Dev~` no se publica (Unity ignora las carpetas que terminan en `~`).
3. En el **Publisher Portal**, abre el borrador rechazado y cambia el **nombre** a *Local TTS – Offline Text-to-Speech* (o *Local TTS*). Revisa que ni el título ni la descripción empiecen por "Unity".
4. **Metadata**: pega la descripción, los detalles técnicos y las keywords de arriba. Pega el texto de **AI Description**.
5. **Media**: borra las imágenes anteriores y sube las de `KeyImages/` y `Screenshots/`. Sube `Trailer_1920x1080.mp4` a YouTube o Vimeo y **reemplaza el enlace del vídeo** anterior.
6. En Unity: **Tools > Asset Store > Uploader**, elige el borrador, selecciona la carpeta **`Assets/KrostGames/LocalTTS`**, pulsa **Validate** y luego **Upload**.
7. En la nota para el revisor puedes poner: *"Renamed to Local TTS (no Unity / Asset Store in the title, docs or UI), new minimal key images without excess text, and AI disclosure added."*
8. **Submit for review**.
