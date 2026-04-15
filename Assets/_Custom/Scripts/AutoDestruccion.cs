using UnityEngine;

public class AutoDestruccion : MonoBehaviour
{
    [Tooltip("Tiempo en segundos antes de que se destruya el objeto")]
    public float tiempoDeVida = 3f;

    void Start()
    {
        // Destruye el objeto al que está adherido este script después del tiempo especificado
        Destroy(gameObject, tiempoDeVida);
    }
}
