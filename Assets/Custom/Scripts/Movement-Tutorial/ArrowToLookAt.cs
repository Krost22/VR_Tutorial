using System;
using System.Collections;
using UnityEngine;

public class ArrowToLookAt : MonoBehaviour
{
    [Range(0, 1)] [SerializeField] private float lookOffset = 0.9f;
    [SerializeField] private Material lookedMaterial;
    private Transform _mainCameraTransform;
    private Transform _t;
    private Renderer _renderer;
    private bool alreadyLookedAt = false;
    private bool startCalculating = false;
    public static Action<ArrowToLookAt> ArrowLooked;

    private IEnumerator Start()
    {
        _renderer = GetComponent<Renderer>();
        _t = transform;
        if (Camera.main) 
            _mainCameraTransform = Camera.main.transform;
        yield return new WaitForSeconds(1f);
        startCalculating = true;
    }

    private void Update()
    {
        if (!startCalculating) return;
        if (alreadyLookedAt) return;
        Vector3 cameraFrontVector = _mainCameraTransform.forward;
        Vector3 cameraToArrowVector = (_t.position - _mainCameraTransform.position ).normalized;
        float dotProduct = Vector3.Dot(cameraFrontVector, cameraToArrowVector);
        if (dotProduct >= lookOffset)
        {
            Debug.Log($"{name}: looked with the vectors {cameraFrontVector}, {cameraToArrowVector} with a Dot Product of {dotProduct}");
            alreadyLookedAt = true;
            _renderer.material = lookedMaterial;
            ArrowLooked?.Invoke(this);
        }
    }
}
