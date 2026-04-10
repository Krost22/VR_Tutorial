using UnityEngine;

public class CirclePulse : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        float scale = 1 + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = initialScale * scale;
    }
}
