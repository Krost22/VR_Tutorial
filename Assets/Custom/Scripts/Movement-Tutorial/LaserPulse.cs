using UnityEngine;

public class LaserPulse : MonoBehaviour
{
    public Renderer laserRenderer;
    public Color emissionColor = Color.red;
    public float pulseSpeed = 2f;
    public float pulseIntensity = 1f;
    private readonly string EmissionString = "_EmissionColor";
    
    private Material laserMaterial;

    void Start()
    {
        if (laserRenderer == null)
            laserRenderer = GetComponent<Renderer>();

        if (laserRenderer != null)
            laserMaterial = laserRenderer.material;
    }

    void Update()
    {
        if (laserMaterial)
        {
            float emission = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f; // Oscila entre 0 y 1
            Color finalColor = emissionColor * (emission * pulseIntensity);
            laserMaterial.SetColor(EmissionString, finalColor);
        }
    }
}
