using UnityEngine;
using VRTutorial.GameFeel;

public class LookDirectionTutorial : MonoBehaviour
{
    public Camera playerCamera;
    public float detectionDistance = 10f;

    // Al terminar el tuto
    public GameObject canvasHUD;
    public GameObject nextStep;
    public GameObject popUp;
    public GameObject pared;
    public GameObject popUpInicial;

    // Script de movimiento a desactivar
    public MonoBehaviour movementScriptToDisable;

    [Header("Game feel (opcional)")]
    [Tooltip("Se reproduce al mirar cada pilar; el tono sube con cada uno (1º, 2º, 3º, 4º).")]
    public FeelBurst lookFeedback;
    [Tooltip("Se reproduce una vez al mirar los cuatro pilares.")]
    public FeelBurst completeFeedback;

    [System.Serializable]
    public class LookPoint
    {
        public string name;
        public GameObject pointObject;
        public Material defaultMaterial;
        public Material blinkingMaterial;
        [HideInInspector] public bool looked = false;
        [HideInInspector] public Renderer rend;
    }

    public LookPoint[] lookPoints;

    private int _lookedCount;
    private bool _completed;

    void Start()
    {
        foreach (var lp in lookPoints)
        {
            if (lp.pointObject.TryGetComponent(out Renderer rend))
            {
                lp.rend = rend;
                lp.rend.material = lp.blinkingMaterial;
            }
        }

        // Bloquea el movimiento al inicio
        if (movementScriptToDisable != null)
            movementScriptToDisable.enabled = false;
    }

    void Update()
    {
        // Se ejecuta una sola vez: antes se repetía cada frame después de completar.
        if (_completed) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, detectionDistance))
        {
            foreach (var lp in lookPoints)
            {
                if (!lp.looked && hit.transform == lp.pointObject.transform)
                {
                    lp.looked = true;
                    lp.rend.material = lp.defaultMaterial;
                    Debug.Log("Looked at: " + lp.name);

                    // Game feel: nota que sube con cada pilar + rebote del pilar mirado.
                    if (lookFeedback != null) lookFeedback.PlayStepAt(lp.pointObject.transform.position, _lookedCount * 2);
                    FeelTween.Punch(lp.pointObject.transform, 0.25f, 0.35f);
                    _lookedCount++;
                }
            }
        }

        if (AllLooked())
        {
            _completed = true;
            FeelDisappear.HideOrDeactivate(pared);
            popUpInicial.SetActive(false);
            popUp.SetActive(true);
            canvasHUD.SetActive(false);
            nextStep.SetActive(true);
            if (completeFeedback != null) completeFeedback.Play();

            foreach (var lp in lookPoints)
            {
                // Los pilares se encogen con un pequeño rebote en vez de desaparecer de golpe.
                var point = lp.pointObject;
                FeelTween.ScaleTo(point.transform, Vector3.zero, 0.35f, FeelEase.InBack, () => { if (point) point.SetActive(false); });
            }

            // Vuelve a habilitar el movimiento
            if (movementScriptToDisable != null)
                movementScriptToDisable.enabled = true;
        }
    }

    bool AllLooked()
    {
        foreach (var lp in lookPoints)
            if (!lp.looked) return false;
        return true;
    }
}
