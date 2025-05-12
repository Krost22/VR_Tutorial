using UnityEngine;

public class CirclePulse : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
    private Vector3 initialScale;
    private Transform _t;

    private void Start()
    {
        _t = transform;
        initialScale = _t.localScale;
        
    }

    private void Update()
    {
        float scale = 1 + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        _t.localScale = initialScale * scale;
    }
}
