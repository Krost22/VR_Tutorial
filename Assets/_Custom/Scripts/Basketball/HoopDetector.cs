using UnityEngine;
using VRTutorial.GameFeel;

public class HoopDetector : MonoBehaviour
{
    public int score = 0;
    public GameObject Pared_Nivel;
    public GameObject Popup;

    [Header("Game feel (opcional)")]
    [Tooltip("Confeti, sonido de red, destello y vibración al encestar.")]
    public FeelBurst scoreFeedback;
    [Tooltip("El aro rebota al encestar.")]
    public Transform hoopVisual;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("BasketBall"))
        {
            FeelDisappear.HideOrDeactivate(Pared_Nivel);
            Popup.SetActive(true);
            score++;
            Debug.Log("¡Enceste! Puntos: " + score);

            if (scoreFeedback != null) scoreFeedback.PlayAt(transform.position);
            if (hoopVisual != null) FeelTween.Punch(hoopVisual, 0.2f, 0.4f);
        }
    }
}
