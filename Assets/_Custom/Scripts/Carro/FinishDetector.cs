using UnityEngine;

public class FinishDetector : MonoBehaviour
{
    public GameObject popupMeta; // Lo que quieres activar cuando llegue a la meta

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            Debug.Log("¡Llegaste a la meta!");
            if (popupMeta != null)
                popupMeta.SetActive(true);
        }
    }
}
