using UnityEngine;

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
                }
            }
        }

        if (AllLooked())
        {
            pared.SetActive(false);
            popUpInicial.SetActive(false);
            popUp.SetActive(true);
            canvasHUD.SetActive(false);
            nextStep.SetActive(true);

            foreach (var lp in lookPoints)
            {
                lp.pointObject.SetActive(false);  // Desactiva los objetos visuales
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
