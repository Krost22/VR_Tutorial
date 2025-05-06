using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using Autohand.Demo; 

public class MovementTutorial : MonoBehaviour
{
    [Header("Tutorial Settings")]
    public GameObject nextCircle;
    public AudioClip touchSound;
    public bool isFirstCircle = false;
    public bool isLastCircle = false;
    public GameObject wallToDeactivate;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = FindObjectOfType<AudioSource>();

        if (isFirstCircle)
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
        if (other.CompareTag("Player"))
        {
            PlaySound();

            if (isLastCircle && wallToDeactivate != null)
            {
                wallToDeactivate.SetActive(false);
            }

            if (nextCircle != null)
            {
                nextCircle.SetActive(true);

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
        yield return new WaitForSeconds(0.1f); // Esperamos 0.1 segundos
        gameObject.SetActive(false);
    }

    private void PlaySound()
    {
        if (audioSource != null && touchSound != null)
        {
            audioSource.PlayOneShot(touchSound);
        }
    }
}
