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

        /// <summary>Los sonidos se sintetizan en GameFeelAudioBuilder (capas, sonoridad igualada, variantes).</summary>
        public static void BuildAudio() => Debug.Log("[GameFeel] Sonidos: " + GameFeelAudioBuilder.BuildAll());

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
            BuildMural("Mural_Agua", muralBase, new Color(0.05f, 0.75f, 0.8f), new Color(0f, 0.85f, 0.85f));
            BuildMural("Mural_Tierra", muralBase, new Color(0.88f, 0.68f, 0.12f), new Color(0.9f, 0.7f, 0.1f));
            BuildMural("Mural_Viento", muralBase, new Color(0.8f, 0.3f, 0.03f), new Color(0.9f, 0.35f, 0.02f));

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
            // Brillo propio suave para que el color se lea igual que el rayo aunque la sala esté oscura.
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(glow.r, glow.g, glow.b) * 0.18f);
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
