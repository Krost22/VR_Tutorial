using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Plant : MonoBehaviour
{
    public bool IsGrowing { get; private set; }
    public static Action<Plant> HasGrown;
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private Transform plantModelTransform;
    [SerializeField] private float growTime;
    private float _currentGrowTime = 0;
    public bool hasCompletelyGrown = false;
    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterLayer) == 0) return;
        //if(!other.TryGetComponent<WaterCan>(out WaterCan can))return;
        WaterCan can = other.GetComponentInParent<WaterCan>();
        if (can != null)
        {
            if (can.IsWatering)
            {
                Grow();
            }
        }
    }

    private void Grow()
    {
        _currentGrowTime += Time.deltaTime;
        plantModelTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, _currentGrowTime / growTime);
        if (_currentGrowTime >= growTime && !hasCompletelyGrown)
        {
            hasCompletelyGrown = true;
            HasGrown?.Invoke(this);
        }
    }

}
