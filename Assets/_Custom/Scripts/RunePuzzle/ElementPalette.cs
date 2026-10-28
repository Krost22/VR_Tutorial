using UnityEngine;

namespace VRTutorial.RunePuzzle
{
    /// <summary>
    /// Un color por elemento, el mismo en toda la cadena del puzzle: runa → hueco del altar → rayo del pilar → mural.
    /// Rojo, azul, amarillo y verde: cuatro tonos bien separados (antes rojo y naranja se confundían con la
    /// luz de las antorchas). El nombre del elemento acompaña siempre al color (altar y pergamino) para quien
    /// distinga mal los colores.
    /// </summary>
    public static class ElementPalette
    {
        /// <summary>Color brillante (runas, anillos, rayos, murales).</summary>
        public static Color Glow(RunicSymbol s) => s switch
        {
            RunicSymbol.Fuego => new Color(1f, 0.2f, 0.08f),
            RunicSymbol.Agua => new Color(0.1f, 0.5f, 1f),
            RunicSymbol.Tierra => new Color(1f, 0.8f, 0.1f),
            RunicSymbol.Viento => new Color(0.25f, 1f, 0.35f),
            _ => Color.white
        };

        /// <summary>Tinta: el mismo tono más oscuro, legible sobre el pergamino claro.</summary>
        public static Color Ink(RunicSymbol s) => s switch
        {
            RunicSymbol.Fuego => new Color(0.72f, 0.08f, 0.03f),
            RunicSymbol.Agua => new Color(0.05f, 0.28f, 0.72f),
            RunicSymbol.Tierra => new Color(0.6f, 0.4f, 0f),
            RunicSymbol.Viento => new Color(0.08f, 0.5f, 0.14f),
            _ => new Color(0.25f, 0.15f, 0.08f)
        };

        public static string Name(RunicSymbol s) => s switch
        {
            RunicSymbol.Fuego => "Fuego",
            RunicSymbol.Agua => "Agua",
            RunicSymbol.Tierra => "Tierra",
            RunicSymbol.Viento => "Viento",
            _ => "?"
        };

        public static string ColorName(RunicSymbol s) => s switch
        {
            RunicSymbol.Fuego => "rojo",
            RunicSymbol.Agua => "azul",
            RunicSymbol.Tierra => "amarillo",
            RunicSymbol.Viento => "verde",
            _ => ""
        };

        /// <summary>Texto con color de tinta para TextMeshPro, ej. "&lt;color=#B8140A&gt;Fuego&lt;/color&gt;".</summary>
        public static string InkTag(RunicSymbol s, string text) => "<color=#" + ColorUtility.ToHtmlStringRGB(Ink(s)) + ">" + text + "</color>";

        public static readonly RunicSymbol[] All = { RunicSymbol.Fuego, RunicSymbol.Agua, RunicSymbol.Tierra, RunicSymbol.Viento };
    }
}
