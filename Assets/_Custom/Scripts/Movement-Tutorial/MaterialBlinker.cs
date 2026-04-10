using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class MaterialBlinker : MonoBehaviour
{
    public Color emissionColor = Color.yellow;
    public float blinkSpeed = 2f;

    private Material mat;
    private Color baseColor;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        baseColor = emissionColor;
        mat.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        float emission = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f; // Oscila entre 0 y 1
        Color finalColor = baseColor * Mathf.LinearToGammaSpace(emission);
        mat.SetColor("_EmissionColor", finalColor);
    }
}
