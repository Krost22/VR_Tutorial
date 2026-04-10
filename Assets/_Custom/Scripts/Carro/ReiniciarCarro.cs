using UnityEngine;

public class ReiniciarCarro : MonoBehaviour
{
    public GameObject carro;          // El objeto que representa el carro
    public Transform puntoReinicio;   // El empty que representa la posici�n inicial

    public void Reiniciar()
    {
        if (carro != null && puntoReinicio != null)
        {
            carro.transform.position = puntoReinicio.position;
            carro.transform.rotation = puntoReinicio.rotation;

            Rigidbody rb = carro.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
