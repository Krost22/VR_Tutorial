using UnityEngine;

public class ColombiaFlagBuilder : MonoBehaviour
{
    public int points = 0;
    public GameObject popupOnComplete;
    public GameObject muro;

    private int totalPoints = 3;

    // Llama a esta función cuando un cubo se coloque correctamente
    public void AddPoint()
    {
        points++;

        if (points >= totalPoints)
        {
            CompleteFlag();
        }
    }

    // Esta función activa el popup
    private void CompleteFlag()
    {
        if (popupOnComplete != null)
        {
            popupOnComplete.SetActive(true);
            muro.SetActive(false);
        }
    }
}
