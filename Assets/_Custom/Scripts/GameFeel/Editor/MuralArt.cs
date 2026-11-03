using System.IO;
using UnityEditor;
using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Ilustraciones de los murales de la sala de runas (una por elemento, 512×368, la proporción del mural 2,16×1,54 m):
    /// pizarra oscura con marco de bronce, el símbolo del elemento grande (sin marca interior: ahí va la gema) con su
    /// engaste en el centro, y el motivo del elemento en su color: llamas (Fuego), olas (Agua), montañas (Tierra) y
    /// remolinos (Viento). Todo simétrico (se lee igual por las dos caras del panel).
    /// El mapa de emisión es el dibujo: FeelGlow lo hace brillar al cargar el pilar, sin encender la pizarra.
    /// Menú: Tools/Game Feel/Regenerar ilustraciones de los murales.
    /// </summary>
    public static class MuralArt
    {
        public const int W = 512, H = 368;
        const float PxPerMeter = H / 1.54f;

        static string AlbedoPath(RunicSymbol s) => GameFeelAssetBuilder.TextureDir + "/Mural_" + ElementPalette.Name(s) + "_Dibujo.png";
        static string EmissionPath(RunicSymbol s) => GameFeelAssetBuilder.TextureDir + "/Mural_" + ElementPalette.Name(s) + "_Brillo.png";

        [MenuItem("Tools/Game Feel/Regenerar ilustraciones de los murales")]
        public static void Menu()
        {
            foreach (var s in ElementPalette.All) Generate(s, ElementPalette.Glow(s));
            Debug.Log("[GameFeel] ilustraciones de los murales regeneradas");
        }

        public static (Texture2D albedo, Texture2D emission) Ensure(RunicSymbol s, Color c, bool force = false)
        {
            if (force || !File.Exists(AlbedoPath(s)) || !File.Exists(EmissionPath(s))) Generate(s, c);
            return (AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath(s)), AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionPath(s)));
        }

        static void Generate(RunicSymbol s, Color c)
        {
            Directory.CreateDirectory(GameFeelAssetBuilder.TextureDir);
            var motif = new float[W * H];   // motivo del elemento
            var glyph = new float[W * H];   // símbolo y engaste
            var frame = new float[W * H];   // marco de bronce
            var center = new Vector2(W * 0.5f, H * 0.5f);

            // Marco doble con rombos en las esquinas.
            Rect(frame, 9, 9, W - 10, H - 10, 2.4f);
            Rect(frame, 19, 19, W - 20, H - 20, 1.4f);
            foreach (var corner in new[] { new Vector2(19, 19), new Vector2(W - 20, 19), new Vector2(19, H - 20), new Vector2(W - 20, H - 20) })
                Diamond(frame, corner, 9f, 1.6f);

            // Símbolo grande: su centro interior coincide con la gema (centro del panel).
            bool up = s == RunicSymbol.Fuego || s == RunicSymbol.Viento;
            float gh = H * 0.62f;
            var gCenter = center + new Vector2(0f, (up ? 1f : -1f) * gh * 0.08f);
            RuneGlyphs.Draw(glyph, null, W, gCenter, gh, 4.2f, s, 1.3f, H, innerMark: false);
            // Engaste pintado alrededor de la gema (0,22 m de radio).
            Circle(glyph, center, 0.22f * PxPerMeter, 2.2f, 1f);
            Circle(glyph, center, 0.26f * PxPerMeter, 1.2f, 0.6f);

            var rng = new System.Random(77 + (int)s * 13);
            switch (s)
            {
                case RunicSymbol.Fuego: // lenguas de fuego desde abajo + brasas
                    // Lenguas de fuego con contorno (dos bordes que se juntan en la punta), en espejo; ninguna en el
                    // centro, que es donde están la base del símbolo y la gema.
                    foreach (var (dx, hgt, wid) in new[] { (60f, 105f, 34f), (122f, 125f, 38f), (184f, 95f, 32f), (232f, 62f, 24f) })
                        foreach (var side in new[] { -1f, 1f })
                        {
                            float bx = center.x + side * dx;
                            for (int edge = -1; edge <= 1; edge += 2)
                            {
                                var prev = new Vector2(bx + edge * wid * 0.5f, 26f);
                                for (int k = 1; k <= 28; k++)
                                {
                                    float t = k / 28f;
                                    float half = wid * 0.5f * Mathf.Sin((1f - t) * Mathf.PI * 0.5f + 0.15f) * (1f - t * 0.35f);
                                    float sway = Mathf.Sin(t * Mathf.PI * 1.4f) * 12f * t * side;
                                    var p = new Vector2(bx + edge * half + sway, 26f + t * hgt);
                                    RuneGlyphs.Line(motif, null, W, H, prev, p, 2.6f, 0.8f);
                                    prev = p;
                                }
                            }
                            // Lengua interior más corta.
                            var prevIn = new Vector2(bx, 30f);
                            for (int k = 1; k <= 16; k++)
                            {
                                float t = k / 16f;
                                var p = new Vector2(bx + Mathf.Sin(t * Mathf.PI) * 6f * side, 30f + t * hgt * 0.55f);
                                RuneGlyphs.Line(motif, null, W, H, prevIn, p, 2f, 0.6f);
                                prevIn = p;
                            }
                        }
                    for (int k = 0; k < 14; k++)
                    {
                        float x = (float)rng.NextDouble() * 200f + 20f, y = 170f + (float)rng.NextDouble() * 150f;
                        foreach (var sx in new[] { center.x - x, center.x + x }) Dot(motif, new Vector2(sx, y), 2.2f, 0.6f);
                    }
                    break;
                case RunicSymbol.Agua: // olas arriba y abajo
                    foreach (var (row, amp, per) in new[] { (34f, 7f, 58f), (56f, 6f, 52f), (78f, 5f, 46f), (H - 34f, 7f, 58f), (H - 56f, 5f, 50f) })
                        Wave(motif, row, amp, per, 2.4f, 0.7f, center.x);
                    for (int k = 0; k < 10; k++)
                    {
                        float x = (float)rng.NextDouble() * 180f + 40f, y = 110f + (float)rng.NextDouble() * 150f;
                        foreach (var sx in new[] { center.x - x, center.x + x }) Circle(motif, new Vector2(sx, y), 3f + (float)rng.NextDouble() * 3f, 1f, 0.5f);
                    }
                    break;
                case RunicSymbol.Tierra: // cordillera abajo, estratos y piedrecitas
                {
                    float[] peaks = { 0f, 70f, 40f, 105f, 55f, 125f };
                    for (int side = -1; side <= 1; side += 2)
                        for (int k = 0; k < peaks.Length - 1; k++)
                        {
                            float x0 = center.x + side * k * 46f, x1 = center.x + side * (k + 1) * 46f;
                            RuneGlyphs.Line(motif, null, W, H, new Vector2(x0, 30f + peaks[k] * 0.9f), new Vector2(x1, 30f + peaks[k + 1] * 0.9f), 2.6f, 0.7f);
                        }
                    foreach (var y in new[] { 30f, 44f }) RuneGlyphs.Line(motif, null, W, H, new Vector2(28f, y), new Vector2(W - 28f, y), 1.6f, 0.5f);
                    for (int k = 0; k < 12; k++)
                    {
                        float x = (float)rng.NextDouble() * 200f + 20f, y = 230f + (float)rng.NextDouble() * 100f;
                        foreach (var sx in new[] { center.x - x, center.x + x }) Dot(motif, new Vector2(sx, y), 2.6f, 0.55f);
                    }
                    break;
                }
                default: // Viento: espirales a los lados y corrientes que cruzan
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        foreach (var (cy, r) in new[] { (H * 0.7f, 40f), (H * 0.3f, 32f) })
                        {
                            var sc = new Vector2(center.x + sx * 175f, cy);
                            var prev = sc;
                            for (int k = 1; k <= 60; k++)
                            {
                                float t = k / 60f, a = t * Mathf.PI * 4f;
                                var p = sc + new Vector2(sx * Mathf.Cos(a), Mathf.Sin(a)) * r * t;
                                RuneGlyphs.Line(motif, null, W, H, prev, p, 2.2f, 0.7f);
                                prev = p;
                            }
                        }
                    }
                    foreach (var (row, amp) in new[] { (H * 0.5f + 95f, 10f), (H * 0.5f - 105f, 8f) })
                        Wave(motif, row, amp, 120f, 1.8f, 0.55f, center.x);
                    break;
            }

            // Componer: pizarra con grano, dibujo del color del elemento, marco de bronce.
            var albedo = new Color[W * H];
            var emission = new Color[W * H];
            var slate = new Color(0.13f, 0.12f, 0.115f);
            var bronze = new Color(0.55f, 0.4f, 0.2f);
            var light = Color.Lerp(c, Color.white, 0.35f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    float n = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.6f + Mathf.PerlinNoise(x * 0.12f + 9f, y * 0.12f + 3f) * 0.4f;
                    float vignette = 1f - 0.35f * Mathf.Pow(Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), center) / (W * 0.6f)), 2f);
                    var col = slate * Mathf.Lerp(0.75f, 1.25f, n) * vignette;
                    float m = Mathf.Clamp01(motif[i]), gl = Mathf.Clamp01(glyph[i]), fr = Mathf.Clamp01(frame[i]);
                    col = Color.Lerp(col, c * 0.75f, m * 0.85f);
                    col = Color.Lerp(col, light, gl);
                    col = Color.Lerp(col, bronze, fr);
                    col.a = 1f;
                    albedo[i] = col;
                    float e = Mathf.Clamp01(Mathf.Max(m * 0.6f, gl));
                    emission[i] = new Color(e, e, e, 1f);
                }
            Save(AlbedoPath(s), albedo);
            Save(EmissionPath(s), emission);
        }

        static void Rect(float[] buf, float x0, float y0, float x1, float y1, float r)
        {
            RuneGlyphs.Line(buf, null, W, H, new Vector2(x0, y0), new Vector2(x1, y0), r, 1f);
            RuneGlyphs.Line(buf, null, W, H, new Vector2(x1, y0), new Vector2(x1, y1), r, 1f);
            RuneGlyphs.Line(buf, null, W, H, new Vector2(x1, y1), new Vector2(x0, y1), r, 1f);
            RuneGlyphs.Line(buf, null, W, H, new Vector2(x0, y1), new Vector2(x0, y0), r, 1f);
        }

        static void Diamond(float[] buf, Vector2 c, float s, float r)
        {
            var pts = new[] { c + new Vector2(0, s), c + new Vector2(s, 0), c + new Vector2(0, -s), c + new Vector2(-s, 0) };
            for (int i = 0; i < 4; i++) RuneGlyphs.Line(buf, null, W, H, pts[i], pts[(i + 1) % 4], r, 1f);
        }

        static void Circle(float[] buf, Vector2 c, float radius, float r, float intensity)
        {
            var prev = c + new Vector2(radius, 0f);
            for (int k = 1; k <= 48; k++)
            {
                float a = k / 48f * Mathf.PI * 2f;
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                RuneGlyphs.Line(buf, null, W, H, prev, p, r, intensity);
                prev = p;
            }
        }

        static void Dot(float[] buf, Vector2 c, float r, float intensity) => RuneGlyphs.Line(buf, null, W, H, c, c + new Vector2(0.01f, 0f), r, intensity);

        /// <summary>Onda horizontal simétrica respecto al centro (cos), de borde a borde dentro del marco.</summary>
        static void Wave(float[] buf, float row, float amp, float period, float r, float intensity, float cx)
        {
            var prev = new Vector2(28f, row + amp * Mathf.Cos((28f - cx) / period * Mathf.PI * 2f));
            for (float x = 32f; x <= W - 28f; x += 4f)
            {
                var p = new Vector2(x, row + amp * Mathf.Cos((x - cx) / period * Mathf.PI * 2f));
                RuneGlyphs.Line(buf, null, W, H, prev, p, r, intensity);
                prev = p;
            }
        }

        static void Save(string path, Color[] pixels)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.sRGBTexture = true;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
        }
    }
}
