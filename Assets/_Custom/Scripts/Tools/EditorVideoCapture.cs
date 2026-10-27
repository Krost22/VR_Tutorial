#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using VRTutorial.Tutorial;

namespace VRTutorial.DevTools
{
    /// <summary>
    /// Solo para el Editor: graba en Play mode lo que ve la cabeza del jugador, a cuadros fijos
    /// (Time.captureFramerate), como imágenes JPG más un WAV del audio del juego (AudioRenderer).
    /// Después se unen con ffmpeg. No se incluye en el build.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class EditorVideoCapture : MonoBehaviour
    {
        public Camera source;
        // Si se asigna, la cámara del video usa esta pose (p. ej. la del autopilot) en vez de la de source.
        public System.Func<Pose> poseProvider;
        public string outputDir;
        public int width = 1280, height = 720, fps = 30;
        public float verticalFov = 62f;
        public float maxSeconds = 45f;
        public bool recordAudio = true;

        public bool Recording { get; private set; }
        public bool Done { get; private set; }
        public int Frames => _frame;

        Camera _cam;
        RenderTexture _rt;
        Texture2D _tex;
        int _frame;
        readonly List<float> _audio = new List<float>();
        int _channels = 2;

        public void Begin()
        {
            Directory.CreateDirectory(outputDir);
            foreach (var f in Directory.GetFiles(outputDir)) File.Delete(f);
            Time.captureFramerate = fps;

            var go = new GameObject("__videoCam") { hideFlags = HideFlags.HideAndDontSave };
            _cam = go.AddComponent<Camera>();
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            _cam.fieldOfView = verticalFov;
            _cam.nearClipPlane = 0.03f;
            _cam.farClipPlane = 200f;
            _cam.enabled = false;
            _rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            _cam.targetTexture = _rt;
            _tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            if (recordAudio)
            {
                _channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
                AudioRenderer.Start();
            }
            Recording = true;
        }

        void LateUpdate()
        {
            if (!Recording || source == null) return;
            if (poseProvider != null) { var p = poseProvider(); _cam.transform.SetPositionAndRotation(p.position, p.rotation); }
            else _cam.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            _cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            _tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            _tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(outputDir, "f_" + _frame.ToString("D5") + ".jpg"), _tex.EncodeToJPG(92));
            _frame++;

            if (recordAudio)
            {
                int n = AudioRenderer.GetSampleCountForCaptureFrame();
                if (n > 0)
                {
                    var buf = new NativeArray<float>(n * _channels, Allocator.Temp);
                    AudioRenderer.Render(buf);
                    _audio.AddRange(buf);
                    buf.Dispose();
                }
            }
            if (_frame >= maxSeconds * fps) Stop();
        }

        public void Stop()
        {
            if (!Recording) return;
            Recording = false;
            Time.captureFramerate = 0;
            if (recordAudio)
            {
                AudioRenderer.Stop();
                WriteWav(Path.Combine(outputDir, "audio.wav"), _audio, AudioSettings.outputSampleRate, _channels);
            }
            if (_cam != null) Destroy(_cam.gameObject);
            if (_rt != null) _rt.Release();
            Done = true;
        }

        void OnDestroy() => Stop();

        static void WriteWav(string path, List<float> samples, int rate, int channels)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int bytes = samples.Count * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16); w.Write((short)1); w.Write((short)channels); w.Write(rate);
                w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (var s in samples) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
            }
        }
    }

    /// <summary>
    /// Solo para el Editor: mueve la cabeza del jugador como lo haría una persona en la fase 1
    /// (nota la flecha, gira hacia la luz, la mira fijamente hasta cargarla) para probarla sin visor.
    /// </summary>
    public class GazeTutorialAutopilot : MonoBehaviour
    {
        public GazeSequenceTutorial gaze;
        public Vector3 headPosition = new Vector3(0.3f, 1.6f, -0.1f);
        public float degreesPerSecond = 85f;
        public float endHold = 4f;
        public bool Finished { get; private set; }

        Transform _cam;
        Quaternion _rot;

        void Start()
        {
            _cam = gaze.playerCamera.transform;
            foreach (var b in _cam.GetComponents<Behaviour>())
                if (b.GetType().Name.Contains("TrackedPoseDriver")) b.enabled = false;
            // Empieza mirando un poco abajo y a la izquierda: la primera luz aparece cerca, no justo en el centro.
            _rot = Quaternion.Euler(14f, -16f, 0f);
            StartCoroutine(Run());
        }

        void LateUpdate()
        {
            // Pequeño temblor natural de la cabeza.
            float t = Time.time;
            var jitter = Quaternion.Euler(Mathf.PerlinNoise(t * 0.7f, 0f) * 0.8f - 0.4f, Mathf.PerlinNoise(0f, t * 0.7f) * 0.8f - 0.4f, 0f);
            _cam.SetPositionAndRotation(headPosition, _rot * jitter);
        }

        IEnumerator Run()
        {
            while (gaze.CurrentTarget == null && !gaze.Completed) yield return null;
            yield return new WaitForSeconds(0.9f); // lee el texto

            while (!gaze.Completed)
            {
                var target = gaze.CurrentTarget;
                if (target == null) { yield return null; continue; }
                var look = Quaternion.LookRotation(target.position - headPosition);
                float angle = Quaternion.Angle(_rot, look);
                if (angle > 35f) yield return new WaitForSeconds(0.7f); // ve la flecha y decide
                yield return Turn(look, angle);
                while (gaze.CurrentTarget == target) yield return null; // mira fijo hasta cargar
                yield return new WaitForSeconds(0.25f);
                // Sigue a la luz con la mirada hacia la barrera mientras vuela.
                yield return Turn(Quaternion.LookRotation(new Vector3(headPosition.x, 1.25f, 3.3f) - headPosition), 0f, 0.9f);
                yield return new WaitForSeconds(0.4f);
            }
            yield return new WaitForSeconds(endHold);
            Finished = true;
        }

        IEnumerator Turn(Quaternion to, float angle, float fixedDuration = -1f)
        {
            var from = _rot;
            if (angle <= 0f) angle = Quaternion.Angle(from, to);
            float duration = fixedDuration > 0f ? fixedDuration : Mathf.Max(0.45f, angle / degreesPerSecond);
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float u = Mathf.Clamp01(time / duration);
                u = u * u * (3f - 2f * u);
                _rot = Quaternion.Slerp(from, to, u);
                yield return null;
            }
            _rot = to;
        }
    }
}
#endif
