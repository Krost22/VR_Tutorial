using Autohand;
using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Game feel de un pilar del puzzle de runas (se añade junto a PillarProjector, sin cambiar su lógica):
    /// - Runa correcta: destello del color del rayo, campanita y vibración. Runa equivocada: zumbido de error.
    /// - Anticipación/carga: mientras el rayo apunta al mural correcto, sube un zumbido, el mural se ilumina
    ///   y el pilar vibra cada vez más rápido hasta fijarse.
    /// - Pilar fijado: golpe metálico, chispas en el mural y el mural queda encendido (indicador permanente).
    /// </summary>
    public class PillarChargeFeel : MonoBehaviour
    {
        public PillarProjector pillar;
        public FeelGlow muralGlow;

        [Header("Carga (mientras apunta)")]
        public AudioClip chargeLoop;
        [Range(0f, 1f)] public float chargeVolume = 0.45f;
        public float minPitch = 0.75f;
        public float maxPitch = 1.8f;
        public float hapticTickInterval = 0.14f;
        [Range(0f, 1f)] public float hapticMax = 0.35f;

        [Header("Runa colocada")]
        public FeelBurst correctRuneBurst;
        public FeelBurst wrongRuneBurst;

        [Header("Pilar fijado")]
        public FeelBurst lockBurst;

        private AudioSource _loop;
        private float _nextTick;

        private void Awake()
        {
            if (pillar == null) pillar = GetComponent<PillarProjector>();
            var go = new GameObject("ChargeAudio");
            go.transform.SetParent(pillar != null && pillar.RaycastOrigin != null ? pillar.RaycastOrigin : transform, false);
            _loop = go.AddComponent<AudioSource>();
            _loop.clip = chargeLoop;
            _loop.loop = true;
            _loop.playOnAwake = false;
            _loop.spatialBlend = 0.9f;
            _loop.minDistance = 1.5f;
            _loop.maxDistance = 25f;
            _loop.dopplerLevel = 0f;
        }

        private void OnEnable()
        {
            if (pillar == null) return;
            pillar.OnProjectorLocked.AddListener(HandleLocked);
            if (pillar.AltarSocket != null) pillar.AltarSocket.OnPlace.AddListener(HandlePlaced);
        }

        private void OnDisable()
        {
            if (pillar == null) return;
            pillar.OnProjectorLocked.RemoveListener(HandleLocked);
            if (pillar.AltarSocket != null) pillar.AltarSocket.OnPlace.RemoveListener(HandlePlaced);
            if (_loop != null) _loop.Stop();
        }

        private void Update()
        {
            if (pillar == null) return;
            bool locked = pillar.IsLocked();
            float progress = pillar.IsActivatedByAltar && !locked ? pillar.LockProgress01 : 0f;

            if (progress > 0.001f)
            {
                if (_loop.clip != null && !_loop.isPlaying) _loop.Play();
                _loop.pitch = Mathf.Lerp(minPitch, maxPitch, progress);
                _loop.volume = Mathf.Lerp(0.2f, 1f, progress) * chargeVolume;
                if (muralGlow != null) muralGlow.SetLevel(progress * 0.8f);

                if (Time.time >= _nextTick && pillar.PillarGrabbable != null)
                {
                    pillar.PillarGrabbable.PlayHapticVibration(0.03f, Mathf.Lerp(0.05f, hapticMax, progress));
                    _nextTick = Time.time + Mathf.Lerp(hapticTickInterval, hapticTickInterval * 0.35f, progress);
                }
            }
            else
            {
                if (_loop.isPlaying) _loop.Stop();
                if (!locked && muralGlow != null) muralGlow.SetLevel(0f);
            }
        }

        private void HandleLocked()
        {
            if (_loop != null) _loop.Stop();
            if (muralGlow != null) muralGlow.MarkSolved();
            Vector3 at = muralGlow != null ? muralGlow.transform.position : transform.position;
            if (lockBurst != null) lockBurst.PlayAt(at);
            if (pillar.PillarGrabbable != null) pillar.PillarGrabbable.PlayHapticVibration(0.15f, 0.7f);
        }

        private void HandlePlaced(PlacePoint point, Grabbable grab)
        {
            var rune = grab != null ? grab.GetComponent<RuneData>() : null;
            bool correct = pillar.RequiredSymbolInSocket == RunicSymbol.Ninguno
                           || (rune != null && rune.CurrentSymbol == pillar.RequiredSymbolInSocket);
            Vector3 at = point != null ? point.transform.position : transform.position;
            if (correct)
            {
                if (correctRuneBurst != null) correctRuneBurst.PlayAt(at);
            }
            else
            {
                if (wrongRuneBurst != null) wrongRuneBurst.PlayAt(at);
                FeelHaptics.ErrorPattern(this);
            }
        }
    }
}
