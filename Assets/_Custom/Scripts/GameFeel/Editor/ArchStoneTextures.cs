using System.IO;
using UnityEditor;
using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Texturas de las piedras del arco de la salida (atlas 2×2, una celda por piedra, 512 px en total):
    /// - Albedo: roca gris con grano y manchas (el color del elemento lo pone el material por bloque de propiedades).
    /// - Relieve (normal map): vetas y símbolo grabados en la piedra.
    /// - Emisión: solo brillan las vetas y el símbolo del elemento (RuneGlyphs, el mismo de las runas), no la losa
    ///   entera: así parece piedra mágica y no un bloque de color plano.
    /// Orden de las celdas: 0 Fuego, 1 Tierra, 2 Viento, 3 Agua (el mismo que usa DungeonFinaleBuilder).
    /// Menú: Tools/Game Feel/Regenerar texturas de las piedras del arco.
    /// </summary>
    public static class ArchStoneTextures
    {
        public const int Size = 512, Cell = Size / 2;
        public const string AlbedoPath = GameFeelAssetBuilder.TextureDir + "/Arco_Piedra_Albedo.png";
        public const string NormalPath = GameFeelAssetBuilder.TextureDir + "/Arco_Piedra_Normal.png";
        public const string EmissionPath = GameFeelAssetBuilder.TextureDir + "/Arco_Piedra_Vetas.png";

        /// <summary>Elemento de cada celda del atlas (mismo orden que el layout de DungeonFinaleBuilder).</summary>
        static readonly RunicSymbol[] CellElements = { RunicSymbol.Fuego, RunicSymbol.Tierra, RunicSymbol.Viento, RunicSymbol.Agua };

        [MenuItem("Tools/Game Feel/Regenerar texturas de las piedras del arco")]
        public static void Menu() { Ensure(true); Debug.Log("[GameFeel] texturas de las piedras del arco regeneradas"); }

        public static (Texture2D albedo, Texture2D normal, Texture2D emission) Ensure(bool force = false)
        {
            if (force || !File.Exists(AlbedoPath) || !File.Exists(NormalPath) || !File.Exists(EmissionPath)) Generate();
            return (AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionPath));
        }

        static void Generate()
        {
            Directory.CreateDirectory(GameFeelAssetBuilder.TextureDir);
            var glow = new float[Size * Size];   // emisión
            var groove = new float[Size * Size]; // profundidad de lo tallado (0..1)

            for (int q = 0; q < 4; q++)
            {
                int ox = (q % 2) * Cell, oy = (q / 2) * Cell;
                var rng = new System.Random(4021 + q * 97);
                // Vetas: grietas que serpentean desde el borde hacia el centro y se ramifican.
                // Pocas y finas: acompañan al símbolo, no lo tapan (antes parecían rayos).
                for (int v = 0; v < 3; v++)
                {
                    float ang = (v + (float)rng.NextDouble() * 0.6f) / 3f * Mathf.PI * 2f;
                    var p = new Vector2(Cell * 0.5f, Cell * 0.5f) + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Cell * 0.45f;
                    float dir = ang + Mathf.PI + ((float)rng.NextDouble() - 0.5f) * 0.8f;
                    Crack(glow, groove, rng, ox, oy, p, dir, 26 + rng.Next(18), 0.8f, 1);
                }
                // Símbolo del elemento, centrado y derecho (el mismo que llevan las runas).
                var c = new Vector2(ox + Cell * 0.5f, oy + Cell * 0.5f);
                RuneGlyphs.Draw(glow, groove, Size, c, Cell * 0.3f, 3.2f, CellElements[q]);
                // Halo suave alrededor del símbolo (la piedra "se llena" de luz cerca del grabado).
                for (int y = 0; y < Cell; y++)
                    for (int x = 0; x < Cell; x++)
                    {
                        float d = Vector2.Distance(new Vector2(ox + x, oy + y), c) / (Cell * 0.32f);
                        glow[(oy + y) * Size + ox + x] += Mathf.Clamp01(1f - d) * 0.22f;
                    }
            }

            var albedo = new Color[Size * Size];
            var height = new float[Size * Size];
            var emission = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    float n = Fbm(x * 0.018f, y * 0.018f);
                    float fine = Mathf.PerlinNoise(x * 0.11f + 31f, y * 0.11f + 17f);
                    float g = Mathf.Clamp01(groove[i]);
                    // Roca: grano grueso + fino, más oscura dentro de lo tallado.
                    float a = Mathf.Lerp(0.34f, 0.62f, n) * Mathf.Lerp(0.9f, 1.05f, fine) * (1f - 0.35f * g);
                    albedo[i] = new Color(a, a * 0.98f, a * 0.95f, 1f);
                    height[i] = n * 0.35f + fine * 0.08f - g * 0.6f;
                    float e = Mathf.Clamp01(0.04f + glow[i]);
                    emission[i] = new Color(e, e, e, 1f);
                }

            var normals = new Color[Size * Size];
            const float strength = 6f;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float hx = height[y * Size + Mathf.Min(x + 1, Size - 1)] - height[y * Size + Mathf.Max(x - 1, 0)];
                    float hy = height[Mathf.Min(y + 1, Size - 1) * Size + x] - height[Mathf.Max(y - 1, 0) * Size + x];
                    var nrm = new Vector3(-hx * strength, -hy * strength, 1f).normalized;
                    normals[y * Size + x] = new Color(nrm.x * 0.5f + 0.5f, nrm.y * 0.5f + 0.5f, nrm.z * 0.5f + 0.5f, 1f);
                }

            Save(AlbedoPath, albedo, TextureImporterType.Default, true);
            Save(NormalPath, normals, TextureImporterType.NormalMap, false);
            Save(EmissionPath, emission, TextureImporterType.Default, true);
        }

        static float Fbm(float x, float y)
        {
            float sum = 0f, amp = 0.5f, f = 1f;
            for (int o = 0; o < 4; o++) { sum += amp * Mathf.PerlinNoise(x * f + 11.3f * o, y * f + 7.1f * o); f *= 2.1f; amp *= 0.5f; }
            return Mathf.Clamp01(sum / 0.9375f);
        }

        static void Crack(float[] glow, float[] groove, System.Random rng, int ox, int oy, Vector2 p, float dir, int steps, float width, int depth)
        {
            for (int s = 0; s < steps; s++)
            {
                var next = p + new Vector2(Mathf.Cos(dir), Mathf.Sin(dir)) * 2.5f;
                if (next.x < 6 || next.y < 6 || next.x > Cell - 6 || next.y > Cell - 6) return;
                float k = 1f - s / (float)steps; // se afina hacia la punta
                Line(glow, groove, new Vector2(ox, oy) + p, new Vector2(ox, oy) + next, width * (0.6f + 0.9f * k), 0.35f + 0.25f * k);
                p = next;
                dir += ((float)rng.NextDouble() - 0.5f) * 0.45f;
                if (depth > 0 && rng.NextDouble() < 0.06)
                    Crack(glow, groove, rng, ox, oy, p, dir + (rng.NextDouble() < 0.5 ? 0.9f : -0.9f), steps / 3, width * 0.7f, depth - 1);
            }
        }

        /// <summary>Trazo con núcleo brillante (radio <paramref name="r"/>) y halo de 3r que se desvanece.</summary>
        static void Line(float[] glow, float[] groove, Vector2 a, Vector2 b, float r, float intensity)
        {
            float halo = r * 3f;
            int x0 = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - halo), x1 = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + halo);
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - halo), y1 = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + halo);
            var ab = b - a; float len2 = Mathf.Max(ab.sqrMagnitude, 1e-4f);
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(Size - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(Size - 1, x1); x++)
                {
                    var p = new Vector2(x, y);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float d = Vector2.Distance(p, a + ab * t);
                    if (d > halo) continue;
                    int i = y * Size + x;
                    float core = Mathf.Clamp01(1f - Mathf.Max(0f, d - r * 0.5f) / (r * 0.6f));
                    float soft = Mathf.Pow(1f - d / halo, 2f) * 0.35f;
                    glow[i] = Mathf.Max(glow[i], (core + soft) * intensity);
                    groove[i] = Mathf.Max(groove[i], core);
                }
        }

        static void Save(string path, Color[] pixels, TextureImporterType type, bool srgb)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, !srgb);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = type;
            imp.sRGBTexture = srgb;
            imp.maxTextureSize = Size;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
        }
    }
}
