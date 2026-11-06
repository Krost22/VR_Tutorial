using System.Collections;
using System.Collections.Generic;
using Autohand;
using UnityEngine;
using UnityEngine.XR;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Al iniciar la escena gira el rig del jugador (solo en el eje vertical) para que la cabeza
    /// mire hacia <see cref="target"/>, sin importar hacia dónde esté parado el jugador en su cuarto.
    /// Así no tiene que darse la vuelta físicamente para empezar el tutorial.
    /// Si el jugador recentra la vista (botón Meta) antes de terminar la fase indicada, se vuelve a alinear.
    /// Para desactivarlo basta con quitar este componente.
    /// </summary>
    public class PlayerStartFacing : MonoBehaviour
    {
        public AutoHandPlayer player;
        [Tooltip("Punto hacia el que debe mirar el jugador al empezar. Si está vacío, usa el frente (eje Z azul) de este objeto.")]
        public Transform target;
        [Tooltip("Tiempo máximo esperando a que el visor reporte su posición (en el Editor sin visor no llega nunca).")]
        public float maxWaitForTracking = 3f;
        [Tooltip("Durante este tiempo tras la primera alineación se corrige otra vez si la cabeza quedó desviada (la pose del visor a veces llega tarde).")]
        public float settleTime = 1f;
        [Tooltip("Desviación (grados) a partir de la cual se corrige durante el asentamiento.")]
        public float settleThreshold = 25f;
        [Tooltip("Volver a alinear si el jugador recentra la vista mientras esta fase no haya terminado. Vacío = solo al iniciar.")]
        public VRTutorial.Tutorial.GazeSequenceTutorial realignUntilCompleted;

        readonly List<XRInputSubsystem> _subsystems = new List<XRInputSubsystem>();
        Coroutine _routine;

        void Start()
        {
            if (player == null) player = FindAnyObjectByType<AutoHandPlayer>();
            if (player == null || player.headCamera == null) return;

            SubsystemManager.GetSubsystems(_subsystems);
            foreach (var s in _subsystems) s.trackingOriginUpdated += OnRecenter;

            _routine = StartCoroutine(AlignRoutine(true));
        }

        void OnDestroy()
        {
            foreach (var s in _subsystems) if (s != null) s.trackingOriginUpdated -= OnRecenter;
        }

        void OnRecenter(XRInputSubsystem _)
        {
            if (realignUntilCompleted == null || realignUntilCompleted.Completed || !isActiveAndEnabled) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(AlignRoutine(false));
        }

        IEnumerator AlignRoutine(bool waitForTracking)
        {
            if (waitForTracking)
            {
                float t = 0f;
                while (t < maxWaitForTracking && !HeadTracked()) { t += Time.unscaledDeltaTime; yield return null; }
            }
            // Un par de frames para que el TrackedPoseDriver aplique la pose real.
            yield return null;
            yield return null;
            Align();

            float settle = 0f;
            while (settle < settleTime)
            {
                settle += Time.unscaledDeltaTime;
                if (Mathf.Abs(YawError()) > settleThreshold) Align();
                yield return null;
            }
            _routine = null;
        }

        static bool HeadTracked()
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return head.isValid && head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked;
        }

        float YawError()
        {
            Vector3 desired = DesiredForward();
            Vector3 current = player.headCamera.transform.forward;
            current.y = 0f;
            if (current.sqrMagnitude < 1e-4f) current = player.headCamera.transform.up; // mirando recto arriba/abajo
            current.y = 0f;
            if (desired.sqrMagnitude < 1e-4f || current.sqrMagnitude < 1e-4f) return 0f;
            return Vector3.SignedAngle(current, desired, Vector3.up);
        }

        Vector3 DesiredForward()
        {
            Vector3 dir = target != null ? target.position - player.headCamera.transform.position : transform.forward;
            dir.y = 0f;
            return dir;
        }

        void Align()
        {
            float delta = YawError();
            if (Mathf.Abs(delta) < 1f) return;
            player.AddRotation(Quaternion.Euler(0f, delta, 0f));
        }
    }
}
