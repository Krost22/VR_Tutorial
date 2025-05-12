using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class GardenController : MonoBehaviour
{
    private Plant[] _plants;
    private int _grownPlantCount = 0;
    public UnityEvent allPlantsGrown;
    private void Start()
    {
        _plants = FindObjectsByType<Plant>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Plant.HasGrown += plant =>
        {
            _grownPlantCount++;
            if(_plants.Length == _grownPlantCount)
                allPlantsGrown?.Invoke();
        };
    }
}
