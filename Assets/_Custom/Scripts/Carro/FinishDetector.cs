using UnityEngine;
using UnityEngine.Events;

public class FinishDetector : MonoBehaviour
{
    public GameObject popupMeta; // Lo que quieres activar cuando llegue a la meta

    [Tooltip("Se llama una sola vez cuando el golem llega a la meta (ej. TutorialFinale.Play).")]
    public UnityEvent onFinish;

    private bool _finished;

    private void OnTriggerEnter(Collider other)
    {
        if (_finished) return;
        var body = other.attachedRigidbody;
        if (other.CompareTag("Car") || (body != null && body.CompareTag("Car")))
        {
            _finished = true;
            Debug.Log("¡Llegaste a la meta!");
            if (popupMeta != null)
                popupMeta.SetActive(true);
            onFinish?.Invoke();
        }
    }
}
