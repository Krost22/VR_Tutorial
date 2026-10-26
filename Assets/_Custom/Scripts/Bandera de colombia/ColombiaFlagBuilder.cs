using UnityEngine;
using VRTutorial.GameFeel;

public class ColombiaFlagBuilder : MonoBehaviour
{
    public int points = 0;
    public GameObject popupOnComplete;
    public GameObject muro;

    [Header("Game feel (opcional)")]
    [Tooltip("Al colocar cada cubo; el tono sube con cada pieza.")]
    public FeelBurst pieceFeedback;
    [Tooltip("Al completar la bandera.")]
    public FeelBurst completeFeedback;

    private int totalPoints = 3;
    private bool _completed;

    // Llama a esta función cuando un cubo se coloque correctamente
    public void AddPoint()
    {
        points++;
        if (pieceFeedback != null) pieceFeedback.PlayStep((points - 1) * 2);

        if (points >= totalPoints && !_completed)
        {
            CompleteFlag();
        }
    }

    // Esta función activa el popup
    private void CompleteFlag()
    {
        _completed = true;
        if (completeFeedback != null) completeFeedback.Play();
        if (popupOnComplete != null)
        {
            popupOnComplete.SetActive(true);
            FeelDisappear.HideOrDeactivate(muro);
        }
    }
}
