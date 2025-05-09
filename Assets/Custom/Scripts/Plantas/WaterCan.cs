using System;
using UnityEngine;
using UnityEngine.Serialization;

public class WaterCan : MonoBehaviour
{
    
    [SerializeField] private ParticleSystem waterParticles;
    public bool IsWatering { get; private set; }
    private Transform _t;
    private void Awake()
    {
        _t = transform;
    }

    private void Update()
    {
        if (_t.localRotation.eulerAngles.x is > 35 and < 250 && !IsWatering)
        {
            StartWatering();
        }else if (_t.localRotation.eulerAngles.x is <= 35 or >= 250 && IsWatering)
        {
            StopWatering();
        }
    }

    private void StartWatering()
    {
        IsWatering = true;
        waterParticles.Play();
        
    }

    private void StopWatering()
    {
        IsWatering = false;
        waterParticles.Stop();
        
    }
}
