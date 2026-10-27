# Walkthrough recording

How `Marketing/AssetStore/Walkthrough_1920x1080.mp4` was made. Everything happens in a **clean Unity 6.4 project** that
contains only Local TTS (URP Blank template + `com.unity.pipeline` so the Unity CLI can drive the Editor).

1. Open the clean project maximized on the primary monitor, load the Default layout and dock the TTS Generator next to
   the Inspector. Run `preroll.cs` and `calib.cs` with `node unity_eval.mjs --eval <file>` (set `UPROJ` to the project path).
2. `node record.mjs directorA.cs eventsA.json takeA.nut` records the Editor region (screen 0,43 1920x989) with
   wall-clock timestamps while `directorA.cs` performs the Generator and TTS Speaker steps through the real UI and logs
   every click, drag and audio start.
3. `playsetup.cs`, enter Play Mode, then `node record.mjs directorB.cs eventsB.json takeB.nut` for the demo scene.
4. Edit-mode previews cannot be read back with `AudioClip.GetData`, so `resynth.cs` synthesizes the same lines with
   the same voices (identical length to the millisecond) for the soundtrack.
5. Extract frames (`ffmpeg -i takeA.nut -fps_mode passthrough -vf crop=1920:967:0:0 -q:v 2 framesA/%05d.jpg`) and
   their timestamps (`ffprobe ... frame=pts_time` -> `ptsA.txt`), copy the events and pts files next to
   `walkthrough.html`, then in `..`: `node render.mjs cues cues.json`, `node audio.mjs cues.json audio.wav`,
   `FRAMES=<dir with framesA/framesB> node render.mjs master video.mp4 --sub 1`, `node render.mjs mux video.mp4 audio.wav out.mp4`.

The scripts contain absolute paths of the machine they were recorded on; adjust `OUT` / paths before reuse.
