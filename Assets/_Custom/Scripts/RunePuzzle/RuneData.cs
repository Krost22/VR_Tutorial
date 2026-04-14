using UnityEngine;

namespace VRTutorial.RunePuzzle
{
    public enum RunicSymbol
    {
        Ninguno,
        Fuego,
        Agua,
        Tierra,
        Viento
    }

    /// <summary>
    /// Va adjunto al Cubo interactivo de AutoHand (que tiene Rigidbody y Grabbable).
    /// </summary>
    public class RuneData : MonoBehaviour
    {
        [Header("Configuración de la Runa")]
        [Tooltip("El símbolo mágico que representa este cubo.")]
        public RunicSymbol CurrentSymbol;

        // Puedes agregar más datos aquí después, como efectos de partículas locales.
    }
}
