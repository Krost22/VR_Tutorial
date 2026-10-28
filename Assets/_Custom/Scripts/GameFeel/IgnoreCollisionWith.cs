using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Bloqueo para el jugador dentro de un objeto agarrable grande (los pilares giratorios).
    /// AutoHand pasa todo lo agarrable a la capa "Grabbable", que el cuerpo del jugador ignora: sin esto el
    /// jugador atravesaría el pilar. Este collider fijo (fuera de la jerarquía agarrable) sí lo frena, y se le
    /// dice a la física que no choque con el propio pilar para no empujarlo ni hacerlo girar.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class IgnoreCollisionWith : MonoBehaviour
    {
        public Transform target;

        void Start()
        {
            if (target == null) return;
            var mine = GetComponent<Collider>();
            foreach (var c in target.GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(mine, c, true);
        }
    }
}
