using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Runas que se distinguen entre sí (antes las cuatro eran el mismo cubo con la misma estrella; solo cambiaba el color):
    /// A) Símbolo propio en las 6 caras (RuneGlyphs: el mismo de las piedras del arco). La estrella vieja se borra
    ///    del color y del relieve del modelo; el símbolo nuevo va en el mapa de emisión de cada elemento.
    /// B) Aura de partículas por elemento: brasas que suben (Fuego), gotas que caen (Agua), piedrecitas en órbita
    ///    (Tierra) y un remolino (Viento). Pocas partículas, material aditivo compartido, sin luces.
    /// Texturas: Tools/Game Feel/Regenerar texturas de las runas. Lo aplica "Construir escena combinada".
    /// Para quitarlo: borrar los hijos "Aura" de las runas y la llamada RuneVisuals.Apply en DungeonFinaleBuilder.
    /// </summary>
    public static class RuneVisuals
    {
        const string SrcDir = "Assets/_Custom/Models/Cubos arcanos";
        const int N = 1024;
        public const string StoneAlbedoPath = GameFeelAssetBuilder.TextureDir + "/Runa_Piedra_Albedo.png";
        public const string StoneNormalPath = GameFeelAssetBuilder.TextureDir + "/Runa_Piedra_Normal.png";
        public static string GlyphPath(RunicSymbol s) => GameFeelAssetBuilder.TextureDir + "/Runa_Glifo_" + ElementPalette.Name(s) + ".png";

        [MenuItem("Tools/Game Feel/Regenerar texturas de las runas")]
        public static void Menu() { GenerateStone(); GenerateGlyphs(); Debug.Log("[GameFeel] texturas de las runas regeneradas"); }

        // ---------------------------------------------------------------- Texturas

        static Color32[] LoadRaw(string file, out int size)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            t.LoadImage(File.ReadAllBytes(SrcDir + "/" + file)); // PNG tal cual, sin la compresión de importación
            size = t.width;
            var px = t.GetPixels32();
            Object.DestroyImmediate(t);
            return px;
        }

        /// <summary>Reduce a N×N promediando bloques.</summary>
        static Color[] Downsample(Color32[] src, int srcSize)
        {
            int f = srcSize / N; var dst = new Color[N * N]; float inv = 1f / (f * f * 255f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float r = 0, g = 0, b = 0, a = 0;
                    for (int j = 0; j < f; j++)
                        for (int i = 0; i < f; i++)
                        {
                            var c = src[(y * f + j) * srcSize + x * f + i];
                            r += c.r; g += c.g; b += c.b; a += c.a;
                        }
                    dst[y * N + x] = new Color(r * inv, g * inv, b * inv, a * inv);
                }
            return dst;
        }

        /// <summary>Máscara de la estrella vieja (a partir de su emisión), engrosada unos píxeles.</summary>
        static bool[] OldGlyphMask(out Color[] emission)
        {
            emission = Downsample(LoadRaw("box_low_Material_Emission.png", out int s), s);
            var m = new bool[N * N];
            for (int i = 0; i < m.Length; i++) m[i] = emission[i].maxColorComponent > 0.08f;
            return Dilate(m, 4);
        }

        static bool[] Dilate(bool[] m, int r)
        {
            var tmp = new bool[m.Length]; var o = new bool[m.Length];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                { bool v = false; for (int d = -r; d <= r && !v; d++) { int xx = x + d; v = xx >= 0 && xx < N && m[y * N + xx]; } tmp[y * N + x] = v; }
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                { bool v = false; for (int d = -r; d <= r && !v; d++) { int yy = y + d; v = yy >= 0 && yy < N && tmp[yy * N + x]; } o[y * N + x] = v; }
            return o;
        }

        /// <summary>Rellena lo enmascarado interpolando la piedra de alrededor por el eje con el hueco más corto.</summary>
        static void Inpaint(Color[] px, bool[] mask)
        {
            var src = (Color[])px.Clone();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    int i = y * N + x;
                    if (!mask[i]) continue;
                    int l = x, r = x, d = y, u = y;
                    while (l > 0 && mask[y * N + l] && x - l < 80) l--;
                    while (r < N - 1 && mask[y * N + r] && r - x < 80) r++;
                    while (d > 0 && mask[d * N + x] && y - d < 80) d--;
                    while (u < N - 1 && mask[u * N + x] && u - y < 80) u++;
                    bool hOk = !mask[y * N + l] && !mask[y * N + r], vOk = !mask[d * N + x] && !mask[u * N + x];
                    if (hOk && (!vOk || r - l <= u - d))
                        px[i] = Color.Lerp(src[y * N + l], src[y * N + r], (x - l) / (float)Mathf.Max(1, r - l));
                    else if (vOk)
                        px[i] = Color.Lerp(src[d * N + x], src[u * N + x], (y - d) / (float)Mathf.Max(1, u - d));
                }
        }

        /// <summary>Piedra limpia (sin la estrella) para las cuatro runas: color y relieve.</summary>
        public static void GenerateStone()
        {
            Directory.CreateDirectory(GameFeelAssetBuilder.TextureDir);
            var mask = OldGlyphMask(out _);
            var albedo = Downsample(LoadRaw("box_low_Material_AlbedoTransparency.png", out int sa), sa);
            Inpaint(albedo, mask);
            for (int i = 0; i < albedo.Length; i++) // gris (el color lo pone el material de cada elemento)
            {
                float v = albedo[i].r * 0.3f + albedo[i].g * 0.59f + albedo[i].b * 0.11f;
                albedo[i] = new Color(v, v, v, albedo[i].a);
            }
            Save(StoneAlbedoPath, albedo, TextureImporterType.Default, true);
            var normal = Downsample(LoadRaw("box_low_Material_Normal.png", out int sn), sn);
            Inpaint(normal, mask);
            Save(StoneNormalPath, normal, TextureImporterType.NormalMap, false);
        }

        /// <summary>Un mapa de emisión por elemento con su símbolo en las 6 caras del cubo.</summary>
        public static void GenerateGlyphs()
        {
            Directory.CreateDirectory(GameFeelAssetBuilder.TextureDir);
            var mask = OldGlyphMask(out var oldEmission);
            var raw = new bool[N * N];
            for (int i = 0; i < raw.Length; i++) raw[i] = oldEmission[i].maxColorComponent > 0.08f;
            var faces = FaceSlots(raw);
            foreach (var s in ElementPalette.All)
            {
                var glow = new float[N * N];
                foreach (var (c, h) in faces) RuneGlyphs.Draw(glow, null, N, c, h, N * 0.0042f, s, 1.2f);
                var px = new Color[N * N];
                for (int i = 0; i < px.Length; i++) { float v = Mathf.Clamp01(glow[i]); px[i] = new Color(v, v, v, 1f); }
                Save(GlyphPath(s), px, TextureImporterType.Default, true);
            }
        }

        /// <summary>
        /// Centros de las caras: las 4 estrellas viejas (columna de la cruz del UV) y las 2 caras laterales, que están a la
        /// altura de la segunda empezando por arriba, a un paso de cara a cada lado.
        /// </summary>
        static List<(Vector2 c, float h)> FaceSlots(bool[] m)
        {
            var groups = new List<(int y0, int y1)>();
            int start = -1, last = -100;
            for (int y = 0; y < N; y++)
            {
                bool any = false;
                for (int x = 0; x < N && !any; x++) any = m[y * N + x];
                if (!any) continue;
                if (y - last > 15) { if (start >= 0) groups.Add((start, last)); start = y; }
                last = y;
            }
            if (start >= 0) groups.Add((start, last));
            var slots = new List<(Vector2 c, float h)>();
            foreach (var (y0, y1) in groups)
            {
                int x0 = N, x1 = 0;
                for (int y = y0; y <= y1; y++) for (int x = 0; x < N; x++) if (m[y * N + x]) { x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); }
                slots.Add((new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), (y1 - y0) * 0.62f));
            }
            if (slots.Count >= 2)
            {
                var byTop = slots.OrderByDescending(s => s.c.y).ToList(); // fila 0 = abajo: la de arriba tiene la y mayor
                float step = 0f;
                for (int i = 1; i < byTop.Count; i++) step += byTop[i - 1].c.y - byTop[i].c.y;
                step /= byTop.Count - 1;
                var row = byTop[1];
                slots.Add((row.c + new Vector2(-step, 0f), row.h));
                slots.Add((row.c + new Vector2(step, 0f), row.h));
            }
            return slots;
        }

        static void Save(string path, Color[] pixels, TextureImporterType type, bool srgb)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false, !srgb);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = type;
            imp.sRGBTexture = srgb;
            imp.maxTextureSize = N;
            imp.mipmapEnabled = true;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
        }

        // ---------------------------------------------------------------- Escena

        /// <summary>Pone la piedra limpia y el símbolo propio en el material de cada runa, y su aura de partículas.</summary>
        public static string Apply(Transform dungeon)
        {
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(StoneAlbedoPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(StoneNormalPath);
            if (albedo == null || normal == null) return "runas: faltan las texturas (Tools/Game Feel/Regenerar texturas de las runas)";
            var particleMat = AuraMaterial();
            int runes = 0, auras = 0;
            foreach (var rune in dungeon.GetComponentsInChildren<RuneData>(true))
            {
                var glyph = AssetDatabase.LoadAssetAtPath<Texture2D>(GlyphPath(rune.CurrentSymbol));
                if (rune.TryGetComponent<Renderer>(out var r) && r.sharedMaterial != null && glyph != null)
                {
                    var m = r.sharedMaterial; // Runa_<Elemento>.mat (de ApplyElementColors)
                    m.SetTexture("_BaseMap", albedo);
                    m.SetTexture("_BumpMap", normal);
                    m.EnableKeyword("_NORMALMAP");
                    m.SetTexture("_EmissionMap", glyph);
                    m.SetColor("_EmissionColor", ElementPalette.Glow(rune.CurrentSymbol) * 1.6f);
                    EditorUtility.SetDirty(m);
                    runes++;
                }
                if (AddAura(rune, r, particleMat)) auras++;
            }
            return "runas: símbolo propio en " + runes + ", aura de partículas en " + auras;
        }

        /// <summary>
        /// Material aditivo (copia de Feel_ParticleAdditive) con una chispa de núcleo nítido: con el punto blando
        /// general las brasas y gotas parecían bolitas difusas o niebla.
        /// </summary>
        static Material AuraMaterial()
        {
            string texPath = GameFeelAssetBuilder.TextureDir + "/feel_spark_dot.png";
            if (!File.Exists(texPath))
            {
                const int S = 64;
                var px = new Color[S * S];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(S * 0.5f, S * 0.5f)) / (S * 0.5f);
                        float v = Mathf.Clamp01(Mathf.Exp(-d * d * 9f) + Mathf.Exp(-d * d * 3f) * 0.45f) * Mathf.Clamp01((1f - d) * 3f);
                        px[y * S + x] = new Color(1f, 1f, 1f, v);
                    }
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                tex.SetPixels(px); tex.Apply();
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            var src = AssetDatabase.LoadAssetAtPath<Material>(GameFeelAssetBuilder.MaterialDir + "/Feel_ParticleAdditive.mat");
            if (src == null) return null;
            string path = GameFeelAssetBuilder.MaterialDir + "/Runa_Aura.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(src);
            var spark = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            m.mainTexture = spark;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", spark);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static bool AddAura(RuneData rune, Renderer body, Material mat)
        {
            var t = rune.transform;
            var old = t.Find("Aura");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            if (mat == null) return false;
            var go = new GameObject("Aura");
            go.transform.SetParent(t, false);
            // Escala 1 en el mundo (la runa está escalada ×20): las medidas de abajo son metros. Sigue a la runa si
            // ReturnHomeWhenDropped la encoge al desaparecer.
            var ls = t.lossyScale;
            go.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
            float half = body != null ? Mathf.Min(body.bounds.extents.x, body.bounds.extents.y, body.bounds.extents.z) : 0.07f;
            float edge = half * 2f; // lado del cubo (~0,4 m): tamaños y velocidades proporcionales a la runa

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = mat;
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pr.receiveShadows = false;
            pr.maxParticleSize = 0.08f;

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 24;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            var em = ps.emission;
            var sh = ps.shape;
            sh.enabled = true;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.2f)));
            var vel = ps.velocityOverLifetime;

            var c = ElementPalette.Glow(rune.CurrentSymbol);
            switch (rune.CurrentSymbol)
            {
                case RunicSymbol.Fuego: // brasas que suben y se quedan atrás si se mueve la runa
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(edge * 0.05f, edge * 0.15f);
                    main.startSize = new ParticleSystem.MinMaxCurve(edge * 0.07f, edge * 0.12f);
                    main.gravityModifier = -0.025f;
                    em.rateOverTime = 16f;
                    sh.shapeType = ParticleSystemShapeType.Box;
                    sh.boxThickness = Vector3.zero; // desde la superficie, un poco por fuera del cubo (si no, nacen dentro y no se ven)
                    sh.scale = Vector3.one * edge * 1.08f;
                    col.color = Fade(new Color(1f, 0.8f, 0.35f), new Color(1f, 0.3f, 0.05f), new Color(0.8f, 0.1f, 0f));
                    break;
                case RunicSymbol.Agua: // gotas que se desprenden y caen despacio
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
                    main.startSize = new ParticleSystem.MinMaxCurve(edge * 0.08f, edge * 0.12f);
                    main.gravityModifier = 0.08f;
                    em.rateOverTime = 12f;
                    sh.shapeType = ParticleSystemShapeType.Box;
                    sh.boxThickness = Vector3.zero;
                    sh.scale = Vector3.one * edge * 1.08f;
                    col.color = Fade(new Color(0.85f, 0.97f, 1f), new Color(0.45f, 0.8f, 1f), c); // más claras: sobre el cubo oscuro se perdían
                    break;
                case RunicSymbol.Tierra: // piedrecitas en órbita lenta
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
                    main.startSize = new ParticleSystem.MinMaxCurve(edge * 0.1f, edge * 0.15f);
                    em.rateOverTime = 4f;
                    sh.shapeType = ParticleSystemShapeType.Sphere;
                    sh.radius = half * 1.7f;
                    sh.radiusThickness = 0f;
                    Orbit(vel, 1.2f, 0f, 0f);
                    col.color = Fade(new Color(1f, 0.8f, 0.4f), c * 0.9f, new Color(0.6f, 0.4f, 0.15f));
                    break;
                default: // Viento: remolino rápido que sube un poco
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(edge * 0.06f, edge * 0.1f);
                    em.rateOverTime = 14f;
                    sh.shapeType = ParticleSystemShapeType.Sphere;
                    sh.radius = half * 1.5f;
                    sh.radiusThickness = 0f;
                    Orbit(vel, 3.5f, -0.02f, 0.04f);
                    col.color = Fade(new Color(0.85f, 1f, 0.9f), c, c * 0.5f);
                    break;
            }
            ps.Play();
            return true;
        }

        static void Orbit(ParticleSystem.VelocityOverLifetimeModule vel, float orbitalY, float radial, float up)
        {
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(up);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            vel.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(orbitalY);
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
            vel.radial = new ParticleSystem.MinMaxCurve(radial);
        }

        /// <summary>Aparece, cambia de color y se desvanece.</summary>
        static ParticleSystem.MinMaxGradient Fade(Color a, Color b, Color end)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.5f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(g);
        }
    }
}
