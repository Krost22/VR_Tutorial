using UnityEngine;

public class HoopDetector : MonoBehaviour
{
    public int score = 0;
    public GameObject Pared_Nivel;
    public GameObject Popup;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("BasketBall"))
        {
            Pared_Nivel.SetActive(false);
            Popup.SetActive(true);
            score++;
            Debug.Log("¡Enceste! Puntos: " + score);
        }
    }
}
