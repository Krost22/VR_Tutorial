using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Plant : MonoBehaviour
{
    public bool IsGrowing { get; private set; }
    [SerializeField] private float growTime;
    [SerializeField] private Transform plantModelTransform;
    private float currentGrowTime = 0;

    private void OnTriggerStay(Collider other)
    {
        if(!other.TryGetComponent<WaterCan>(out WaterCan can))return;
        if (can.IsWatering)
        {
            Grow();
        }
    }

    private void Grow()
    {
        currentGrowTime += Time.deltaTime;
        plantModelTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, currentGrowTime / growTime);
    }

}
