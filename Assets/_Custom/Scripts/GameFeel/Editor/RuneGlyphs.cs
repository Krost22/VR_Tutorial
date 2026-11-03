using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Símbolos de los elementos, compartidos por las runas y las piedras del arco (mismo lenguaje en todo el
    /// puzzle). Base alquímica (△ fuego, ▽ agua, ▽ con barra tierra, △ con barra aire) más una marca interior que
    /// los hace distintos aunque se vean girados 180° (una runa en la mano puede estar boca abajo):
    /// fuego = llama, agua = ola, tierra = punto, aire = anillo.
    /// Se dibujan en dos capas de flotantes: <c>glow</c> (lo que brilla, con halo) y <c>groove</c> (lo tallado).
    /// </summary>
    public static class RuneGlyphs
    {
        /// <param name="size">Lado de la textura en píxeles (las capas son size×size, fila 0 abajo).</param>
        /// <param name="center">Centro del símbolo en píxeles.</param>
        /// <param name="h">Alto del triángulo en píxeles.</param>
        /// <param name="stroke">Radio del trazo en píxeles.</param>
        /// <param name="height">Alto de la textura si no es cuadrada (0 = igual que <paramref name="size"/>).</param>
        /// <param name="innerMark">false = sin la marca interior (llama, ola, punto, anillo); la barra se mantiene.</param>
        public static void Draw(float[] glow, float[] groove, int size, Vector2 center, float h, float stroke, RunicSymbol s, float intensity = 1.35f, int height = 0, bool innerMark = true)
        {
            int W = size, H = height > 0 ? height : size;
            bool up = s == RunicSymbol.Fuego || s == RunicSymbol.Viento;
            float k = up ? 1f : -1f, w = h * 1.1f;
            var tip = center + new Vector2(0f, k * h * 0.55f);
            var b1 = center + new Vector2(-w * 0.5f, -k * h * 0.45f);
            var b2 = center + new Vector2(w * 0.5f, -k * h * 0.45f);
            Line(glow, groove, W, H, tip, b1, stroke, intensity);
            Line(glow, groove, W, H, b1, b2, stroke, intensity);
            Line(glow, groove, W, H, b2, tip, stroke, intensity);

            // Marca interior (centro del triángulo un poco hacia la base).
            var inner = center - new Vector2(0f, k * h * 0.08f);
            float u = h * 0.16f; // unidad de las marcas
            switch (s)
            {
                case RunicSymbol.Fuego when innerMark: // llama: gota que apunta al vértice
                {
                    var prev = Vector2.zero;
                    for (int i = 0; i <= 20; i++)
                    {
                        float a = i / 20f * Mathf.PI * 2f;
                        // Lágrima: redonda abajo (cos a > 0) y afilada arriba (cos a < 0), apuntando al vértice.
                        var p = inner + new Vector2(Mathf.Sin(a) * u * 0.75f * (1f - 0.8f * Mathf.Max(0f, -Mathf.Cos(a))), k * (-Mathf.Cos(a) * u * 1.1f + u * 0.15f));
                        if (i > 0) Line(glow, groove, W, H, prev, p, stroke * 0.8f, intensity);
                        prev = p;
                    }
                    break;
                }
                case RunicSymbol.Agua when innerMark: // ola: una onda horizontal
                {
                    var prev = Vector2.zero;
                    for (int i = 0; i <= 24; i++)
                    {
                        float t = i / 24f;
                        var p = inner + new Vector2((t - 0.5f) * u * 2.3f, Mathf.Sin(t * Mathf.PI * 3f) * u * 0.28f);
                        if (i > 0) Line(glow, groove, W, H, prev, p, stroke * 0.8f, intensity);
                        prev = p;
                    }
                    break;
                }
                case RunicSymbol.Tierra: // barra + punto macizo hacia la base
                case RunicSymbol.Viento: // barra + anillo hacia el vértice
                {
                    float barY = center.y + k * h * 0.12f;
                    float half = w * 0.42f;
                    Line(glow, groove, W, H, new Vector2(center.x - half, barY), new Vector2(center.x + half, barY), stroke, intensity);
                    var mark = new Vector2(center.x, center.y - k * h * 0.2f);
                    if (!innerMark) break;
                    if (s == RunicSymbol.Tierra)
                        Line(glow, groove, W, H, mark, mark + new Vector2(0.01f, 0f), u * 0.45f, intensity);
                    else
                    {
                        mark = new Vector2(center.x, center.y + k * h * 0.32f);
                        var prev = Vector2.zero;
                        for (int i = 0; i <= 18; i++)
                        {
                            float a = i / 18f * Mathf.PI * 2f;
                            var p = mark + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * u * 0.5f;
                            if (i > 0) Line(glow, groove, W, H, prev, p, stroke * 0.7f, intensity);
                            prev = p;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>Trazo con núcleo brillante (radio <paramref name="r"/>) y halo de 3r que se desvanece.</summary>
        public static void Line(float[] glow, float[] groove, int size, Vector2 a, Vector2 b, float r, float intensity)
            => Line(glow, groove, size, size, a, b, r, intensity);

        /// <summary>Igual, en una capa de <paramref name="width"/>×<paramref name="height"/> píxeles.</summary>
        public static void Line(float[] glow, float[] groove, int width, int height, Vector2 a, Vector2 b, float r, float intensity)
        {
            int size = width;
            float halo = r * 3f;
            int x0 = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - halo), x1 = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + halo);
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - halo), y1 = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + halo);
            var ab = b - a; float len2 = Mathf.Max(ab.sqrMagnitude, 1e-4f);
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(height - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(size - 1, x1); x++)
                {
                    var p = new Vector2(x, y);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float d = Vector2.Distance(p, a + ab * t);
                    if (d > halo) continue;
                    int i = y * size + x;
                    float core = Mathf.Clamp01(1f - Mathf.Max(0f, d - r * 0.5f) / (r * 0.6f));
                    float soft = Mathf.Pow(1f - d / halo, 2f) * 0.35f;
                    glow[i] = Mathf.Max(glow[i], (core + soft) * intensity);
                    if (groove != null) groove[i] = Mathf.Max(groove[i], core);
                }
        }
    }
}
