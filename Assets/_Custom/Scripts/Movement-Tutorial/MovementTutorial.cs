using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using Autohand.Demo;
using VRTutorial.GameFeel;

public class MovementTutorial : MonoBehaviour
{
    [Header("Tutorial Settings")]
    public GameObject nextCircle;
    public AudioClip touchSound;
    public bool isFirstCircle = false;
    public bool isLastCircle = false;
    public GameObject wallToDeactivate;

    [Header("Game feel (opcional)")]
    [Tooltip("Se reproduce al pisar el círculo; el tono sube según el orden del círculo.")]
    public FeelBurst stepFeedback;
    [Tooltip("Orden del círculo (0, 1, 2...) para la nota que suena.")]
    public int stepIndex = 0;

    private AudioSource audioSource;
    private bool _touched;
    // Lo marca el círculo anterior antes de activarlo. Sin esto, Start() apagaba el círculo
    // recién activado (no es el primero) y la cadena se cortaba en el segundo círculo.
    private bool _activatedByPrevious;

    private void Start()
    {
        audioSource = FindObjectOfType<AudioSource>();

        if (isFirstCircle || _activatedByPrevious)
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_touched) return;
        if (other.CompareTag("Player"))
        {
            _touched = true;
            PlaySound();
            if (stepFeedback != null) stepFeedback.PlayStepAt(transform.position, stepIndex * 2);

            if (isLastCircle && wallToDeactivate != null)
            {
                FeelDisappear.HideOrDeactivate(wallToDeactivate);
            }

            if (nextCircle != null && nextCircle != gameObject)
            {
                var nextTutorial = nextCircle.GetComponent<MovementTutorial>();
                if (nextTutorial != null) nextTutorial._activatedByPrevious = true;
                nextCircle.SetActive(true);
                // El siguiente círculo aparece con un rebote para llamar la atención.
                FeelTween.PopIn(nextCircle.transform, 0.4f);

                TeleportPoint teleportPoint = nextCircle.GetComponent<TeleportPoint>();
                if (teleportPoint != null)
                {
                    teleportPoint.alwaysShow = true;
                }
            }

            StartCoroutine(DestroyAfterDelay());
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        // Se encoge antes de destruirse en vez de desaparecer de golpe.
        FeelTween.ScaleTo(transform, Vector3.zero, 0.25f, FeelEase.InBack);
        yield return new WaitForSeconds(0.28f);
        Destroy(gameObject);
    }

    private void PlaySound()
    {
        if (audioSource != null && touchSound != null)
        {
            audioSource.PlayOneShot(touchSound);
        }
    }
}
