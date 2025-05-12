using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class LookDirectionTutorialNonPhysical : MonoBehaviour
{
    private ArrowToLookAt[] _arrows;
    private int _arrowsLookedCount = 0;
    public UnityEvent allArrowsWatched;
    private void Start()
    {
        _arrows = FindObjectsByType<ArrowToLookAt>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        ArrowToLookAt.ArrowLooked += arrow =>
        {
            _arrowsLookedCount++;
            if(_arrows.Length == _arrowsLookedCount)
                OnAllArrowsWatched();
        };
    }

    private void OnAllArrowsWatched()
    {
        foreach (var arrow in _arrows)
        {
            arrow.gameObject.SetActive(false);
        }
        allArrowsWatched?.Invoke();
    }
}

