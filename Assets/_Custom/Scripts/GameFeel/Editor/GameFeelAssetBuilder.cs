using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Genera los recursos del kit de game feel: sonidos sintetizados (WAV), textura de partícula,
    /// materiales URP y prefabs de partículas. Menú: Tools/Game Feel/Regenerar recursos.
    /// Todo se crea en Assets/_Custom/GameFeel; borrar esa carpeta quita los recursos.
    /// </summary>
    public static class GameFeelAssetBuilder
    {
        public const string Root = "Assets/_Custom/GameFeel";
        public const string AudioDir = Root + "/Audio";
        public const string MaterialDir = Root + "/Materials";
        public const string PrefabDir = Root + "/Prefabs";
        public const string TextureDir = Root + "/Textures";

        private const int SampleRate = 44100;

        [MenuItem("Tools/Game Feel/Regenerar recursos")]
        public static void BuildAll()
        {
            BuildAudio();
            var dot = BuildTexture();
            BuildMaterials(dot);
            BuildPrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log("[GameFeel] Recursos generados en " + Root);
        }

        // ---------------------------------------------------------------- Audio

        public static void BuildAudio()
        {
            Directory.CreateDirectory(AudioDir);
            Write("feel_pop", Pop());
            Write("feel_chime", Chime());
            Write("feel_success", Success());
            Write("feel_fanfare", Fanfare());
            Write("feel_powerdown", PowerDown());
            Write("feel_error", Error());
            Write("feel_whoosh", Whoosh());
            Write("feel_thud", Thud(0.35f, 1f));
            Write("feel_step", Thud(0.25f, 0.55f));
            Write("feel_lock", Lock());
            Write("feel_charge", Charge());
            Write("feel_rumble", Rumble());
            Write("feel_swish", Swish());
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono = true;
                importer.loadInBackground = false;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * SampleRate)];

        private static void Tone(float[] b, float start, float freq, float amp, float decay, float attack = 0.004f)
        {
            int s0 = (int)(start * SampleRate);
            for (int i = s0; i < b.Length; i++)
            {
                float t = (i - s0) / (float)SampleRate;
                float env = t < attack ? t / attack : Mathf.Exp(-(t - attack) * decay);
                if (env < 0.0005f && t > attack) break;
                b[i] += amp * env * Mathf.Sin(2f * Mathf.PI * freq * t);
            }
        }

        private static void Bell(float[] b, float start, float f, float amp, float decay)
        {
            Tone(b, start, f, amp, decay);
            Tone(b, start, f * 2.0f, amp * 0.45f, decay * 1.6f);
            Tone(b, start, f * 3.01f, amp * 0.22f, decay * 2.2f);
            Tone(b, start, f * 4.17f, amp * 0.12f, decay * 3f);
            Tone(b, start, f * 5.43f, amp * 0.06f, decay * 4f);
        }

        private static void Sweep(float[] b, float start, float dur, float f0, float f1, float amp, Func<float, float> env, bool buzzy)
        {
            int s0 = (int)(start * SampleRate);
            int n = (int)(dur * SampleRate);
            double phase = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float k = i / (float)n;
                float f = Mathf.Lerp(f0, f1, k * k);
                phase += 2 * Math.PI * f / SampleRate;
                float s = (float)Math.Sin(phase);
                if (buzzy) s = s + 0.35f * (float)Math.Sin(phase * 3) + 0.2f * (float)Math.Sin(phase * 5);
                b[s0 + i] += amp * env(k) * s;
            }
        }

        private static void Noise(float[] b, float start, float dur, float amp, Func<float, float> env, Func<float, float> lowpass, int seed, bool highpass = false)
        {
            var rng = new System.Random(seed);
            int s0 = (int)(start * SampleRate);
            int n = (int)(dur * SampleRate);
            float lp = 0f;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float k = i / (float)n;
                float white = (float)(rng.NextDouble() * 2 - 1);
                float a = Mathf.Clamp01(lowpass(k));
                lp += a * (white - lp);
                float v = highpass ? white - lp : lp;
                b[s0 + i] += amp * env(k) * v;
            }
        }

        private static float[] Pop()
        {
            var b = Buf(0.22f);
            Sweep(b, 0f, 0.08f, 380f, 950f, 0.8f, k => Mathf.Pow(1f - k, 2f), false);
            Bell(b, 0.02f, 1250f, 0.18f, 30f);
            return b;
        }

        private static float[] Chime()
        {
            var b = Buf(1.3f);
            Bell(b, 0f, 880f, 0.55f, 3.6f);
            return b;
        }

        private static float[] Success()
        {
            var b = Buf(1.7f);
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < notes.Length; i++) Bell(b, i * 0.09f, notes[i], 0.4f, 3.2f);
            return b;
        }

        private static float[] Fanfare()
        {
            var b = Buf(2.8f);
            float[] chord = { 261.63f, 329.63f, 392f, 523.25f };
            foreach (var f in chord)
            {
                Tone(b, 0f, f, 0.18f, 1.1f, 0.03f);
                Tone(b, 0f, f * 2f, 0.07f, 1.6f, 0.03f);
                Tone(b, 0f, f * 3f, 0.04f, 2.2f, 0.03f);
            }
            float[] arp = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f, 1568f };
            for (int i = 0; i < arp.Length; i++) Bell(b, 0.12f + i * 0.08f, arp[i], 0.28f, 2.6f);
            Noise(b, 0.1f, 1.8f, 0.05f, k => Mathf.Sin(Mathf.PI * k) * (1f - k), k => 0.9f, 11, true);
            return b;
        }

        private static float[] PowerDown()
        {
            var b = Buf(0.75f);
            Sweep(b, 0f, 0.65f, 950f, 55f, 0.5f, k => Mathf.Pow(1f - k, 1.4f), true);
            Noise(b, 0f, 0.4f, 0.25f, k => (Mathf.Repeat(k * 23f, 1f) < 0.3f ? 1f : 0.2f) * (1f - k), k => 0.6f, 3);
            return b;
        }

        private static float[] Error()
        {
            var b = Buf(0.35f);
            Sweep(b, 0f, 0.09f, 150f, 140f, 0.45f, k => k < 0.1f ? k * 10f : 1f - k * 0.3f, true);
            Sweep(b, 0.14f, 0.09f, 140f, 120f, 0.45f, k => k < 0.1f ? k * 10f : 1f - k * 0.3f, true);
            return b;
        }

        private static float[] Whoosh()
        {
            var b = Buf(0.6f);
            Noise(b, 0f, 0.58f, 0.9f, k => Mathf.Pow(Mathf.Sin(Mathf.PI * k), 2f), k => Mathf.Lerp(0.03f, 0.35f, Mathf.Sin(Mathf.PI * k)), 5);
            return b;
        }

        private static float[] Thud(float length, float strength)
        {
            var b = Buf(length);
            Sweep(b, 0f, length * 0.8f, 120f, 42f, 0.9f * strength, k => Mathf.Exp(-k * 6f), false);
            Noise(b, 0f, 0.03f, 0.5f * strength, k => 1f - k, k => 0.25f, 7);
            return b;
        }

        private static float[] Lock()
        {
            var b = Buf(1f);
            Sweep(b, 0f, 0.2f, 110f, 50f, 0.7f, k => Mathf.Exp(-k * 5f), false);
            Tone(b, 0f, 520f, 0.35f, 18f, 0.001f);
            Tone(b, 0f, 1310f, 0.25f, 24f, 0.001f);
            Tone(b, 0f, 2470f, 0.15f, 30f, 0.001f);
            Noise(b, 0f, 0.025f, 0.4f, k => 1f - k, k => 0.7f, 9);
            Bell(b, 0.02f, 1046.5f, 0.22f, 4f);
            return b;
        }

        private static float[] Charge()
        {
            // Bucle exacto de 1 s: todas las frecuencias completan ciclos enteros.
            var b = Buf(1f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SampleRate;
                float trem = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                float s = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.5f
                          + Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.3f
                          + Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.15f
                          + Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.08f;
                b[i] = s * trem * 0.5f;
            }
            return b;
        }

        private static float[] Rumble()
        {
            var b = Buf(1.7f);
            var rng = new System.Random(21);
            float brown = 0f, lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float k = i / (float)b.Length;
                brown = Mathf.Clamp(brown * 0.995f + (float)(rng.NextDouble() * 2 - 1) * 0.05f, -1f, 1f);
                lp += 0.08f * (brown - lp);
                float grind = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 7f * k * 1.7f);
                float env = Mathf.Min(1f, k / 0.08f) * Mathf.Min(1f, (1f - k) / 0.25f);
                b[i] = lp * 4f * grind * env;
            }
            Sweep(b, 0f, 1.6f, 55f, 48f, 0.3f, k => Mathf.Min(1f, k / 0.1f) * (1f - k), false);
            return b;
        }

        private static float[] Swish()
        {
            var b = Buf(0.4f);
            Noise(b, 0f, 0.38f, 0.7f, k => k < 0.08f ? k / 0.08f : Mathf.Pow(1f - k, 2f), k => 0.5f, 13, true);
            Bell(b, 0.05f, 1567.98f, 0.12f, 6f);
            return b;
        }

        private static void Write(string name, float[] samples)
        {
            // Normaliza, suaviza el final y escribe WAV PCM de 16 bits mono.
            float peak = 0.0001f;
            foreach (var s in samples) peak = Mathf.Max(peak, Mathf.Abs(s));
            float gain = 0.89f / peak;
            int fade = Mathf.Min(samples.Length, SampleRate / 100);
            for (int i = 0; i < fade; i++) samples[samples.Length - 1 - i] *= i / (float)fade;

            string path = Path.Combine(AudioDir, name + ".wav");
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int dataBytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + dataBytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(SampleRate);
                w.Write(SampleRate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(dataBytes);
                foreach (var s in samples) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * gain * 32767f), -32768, 32767));
            }
        }

        // ---------------------------------------------------------------- Textura

        public static Texture2D BuildTexture()
        {
            Directory.CreateDirectory(TextureDir);
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            string path = TextureDir + "/feel_soft_dot.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------------------------------------------------------------- Materiales

        public static void BuildMaterials(Texture2D dot)
        {
            Directory.CreateDirectory(MaterialDir);
            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");

            var additive = LoadOrCreate(MaterialDir + "/Feel_ParticleAdditive.mat", particleShader);
            SetupTransparent(additive, additiveBlend: true);
            additive.SetTexture("_BaseMap", dot);
            additive.SetColor("_BaseColor", Color.white);

            var alpha = LoadOrCreate(MaterialDir + "/Feel_ParticleAlpha.mat", particleShader);
            SetupTransparent(alpha, additiveBlend: false);
            alpha.SetTexture("_BaseMap", dot);
            alpha.SetColor("_BaseColor", Color.white);

            var fade = LoadOrCreate(MaterialDir + "/Feel_Fade.mat", unlit);
            SetupTransparent(fade, additiveBlend: false);
            fade.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f));
            if (fade.HasProperty("_Cull")) fade.SetFloat("_Cull", 0f);
            if (fade.HasProperty("_ZTest")) fade.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            fade.renderQueue = 4000;

            // Murales del puzzle de runas: mismo material base que tenían (AH_Blue 4) pero con el color de su rayo.
            var muralBase = FindMaterial("AH_Blue 4");
            BuildMural("Mural_Agua", muralBase, new Color(0.05f, 0.78f, 0.82f), new Color(0f, 1f, 0.97f));
            BuildMural("Mural_Tierra", muralBase, new Color(0.86f, 0.74f, 0.33f), new Color(0.93f, 0.87f, 0.45f));
            BuildMural("Mural_Viento", muralBase, new Color(0.62f, 0.3f, 0.06f), new Color(0.95f, 0.45f, 0.08f));

            foreach (var m in new[] { additive, alpha, fade }) EditorUtility.SetDirty(m);
        }

        private static void BuildMural(string name, Material source, Color baseColor, Color glow)
        {
            string path = MaterialDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
        }

        private static Material FindMaterial(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Material"))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m != null && m.name == name) return m;
            }
            return null;
        }

        private static Material LoadOrCreate(string path, Shader shader)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader) mat.shader = shader;
            return mat;
        }

        private static void SetupTransparent(Material m, bool additiveBlend)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additiveBlend ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(additiveBlend ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)(additiveBlend ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        // ---------------------------------------------------------------- Partículas

        public static void BuildPrefabs()
        {
            Directory.CreateDirectory(PrefabDir);
            var additive = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Feel_ParticleAdditive.mat");
            var alpha = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Feel_ParticleAlpha.mat");

            SavePrefab(Sparks(additive), "Feel_Sparks");
            SavePrefab(Magic(additive), "Feel_Magic");
            SavePrefab(Dust(alpha), "Feel_Dust");
            SavePrefab(Flame(additive), "Feel_Flame");
            SavePrefab(Confetti(alpha), "Feel_Confetti");
        }

        private static ParticleSystem NewSystem(string name, Material mat, int burst, float duration = 0.6f)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            if (burst > 0) emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        private static Gradient Fade(Color a, Color b, float alphaMid = 1f)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(alphaMid, 0.6f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static ParticleSystem Sparks(Material mat)
        {
            var ps = NewSystem("Feel_Sparks", mat, 28, 0.4f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = new Color(1f, 0.85f, 0.55f);
            main.gravityModifier = 0.6f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.05f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, new Color(1f, 0.45f, 0.1f));
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.06f; r.lengthScale = 1.5f;
            return ps;
        }

        private static ParticleSystem Magic(Material mat)
        {
            var ps = NewSystem("Feel_Magic", mat, 36, 0.6f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startColor = new Color(0.55f, 0.9f, 1f);
            main.gravityModifier = -0.12f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.12f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, Color.white);
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 1.2f;
            return ps;
        }

        private static ParticleSystem Dust(Material mat)
        {
            var ps = NewSystem("Feel_Dust", mat, 22, 0.8f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.62f, 0.55f, 0.46f, 0.55f);
            main.gravityModifier = -0.03f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, Color.white, 0.6f);
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 1.9f));
            var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 0.3f; limit.dampen = 0.15f;
            return ps;
        }

        private static ParticleSystem Flame(Material mat)
        {
            var ps = NewSystem("Feel_Flame", mat, 0, 1f);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new Color(1f, 0.85f, 0.5f);
            main.gravityModifier = -0.2f;
            var emission = ps.emission; emission.rateOverTime = 35f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 8f; shape.radius = 0.035f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.55f), 0f), new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0.5f), new GradientColorKey(new Color(0.6f, 0.1f, 0f), 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            return ps;
        }

        private static ParticleSystem Confetti(Material mat)
        {
            var ps = NewSystem("Feel_Confetti", mat, 70, 1f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var colors = new Gradient();
            colors.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.1f), 0f), new GradientColorKey(new Color(0.15f, 0.45f, 1f), 0.33f), new GradientColorKey(new Color(1f, 0.2f, 0.25f), 0.66f), new GradientColorKey(new Color(0.3f, 1f, 0.5f), 1f) },
                           new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };
            main.gravityModifier = 0.35f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 35f; shape.radius = 0.1f; shape.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, Color.white);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 1.2f; limit.dampen = 0.08f;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.8f;
            return ps;
        }

        private static void SavePrefab(ParticleSystem ps, string name)
        {
            string path = PrefabDir + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(ps.gameObject, path);
            UnityEngine.Object.DestroyImmediate(ps.gameObject);
        }
    }
}
