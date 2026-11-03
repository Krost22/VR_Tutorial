using System.Collections.Generic;
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

    [Header("Un enceste por tiro")]
    [Tooltip("Segundos en que el mismo balón no vuelve a contar (un balón con varios colliders, o que rebota " +
             "dentro del aro, disparaba el trigger varias veces). 0 = cuenta cada entrada, como antes.")]
    public float sameBallCooldown = 1.2f;

    // Último enceste de cada balón (por su Rigidbody, o su objeto si no tiene).
    readonly Dictionary<Object, float> _lastScore = new Dictionary<Object, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("BasketBall"))
        {
            if (sameBallCooldown > 0f)
            {
                Object ball = other.attachedRigidbody != null ? (Object)other.attachedRigidbody : other.transform.root;
                if (_lastScore.TryGetValue(ball, out float last) && Time.time - last < sameBallCooldown) return;
                _lastScore[ball] = Time.time;
            }

            FeelDisappear.HideOrDeactivate(Pared_Nivel);
            Popup.SetActive(true);
            score++;
            Debug.Log("¡Enceste! Puntos: " + score);

            if (scoreFeedback != null) scoreFeedback.PlayAt(transform.position);
            if (hoopVisual != null) FeelTween.Punch(hoopVisual, 0.2f, 0.4f);
        }
    }
}
