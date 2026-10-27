using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Barrera láser separada en haces (de arriba hacia abajo) que se pueden apagar uno por uno.
    /// Sirve para mostrar el progreso de un puzzle directamente en lo que bloquea el paso.
    /// </summary>
    public class LaserBeamSet : MonoBehaviour
    {
        [Tooltip("Haces ordenados de arriba hacia abajo.")]
        public List<GameObject> beams = new List<GameObject>();
        public AudioClip offSound;
        [Range(0f, 1f)] public float volume = 0.7f;
        public ParticleSystem sparksPrefab;
        public Color sparkTint = Color.white;
        public float flickerTime = 0.3f;

        public int ActiveCount
        {
            get { int n = 0; foreach (var b in beams) if (b != null && b.activeSelf) n++; return n; }
        }

        public void TurnOff(int index)
        {
            if (index < 0 || index >= beams.Count || beams[index] == null || !beams[index].activeSelf) return;
            if (!isActiveAndEnabled) { beams[index].SetActive(false); return; }
            StartCoroutine(TurnOffRoutine(beams[index]));
        }

        private IEnumerator TurnOffRoutine(GameObject beam)
        {
            var r = beam.GetComponent<Renderer>();
            Vector3 center = r != null ? r.bounds.center : beam.transform.position;
            if (offSound != null) FeelAudio.PlayVaried(offSound, center, volume, new Vector2(0.95f, 1.1f));
            float t = 0f;
            while (t < flickerTime)
            {
                t += Time.deltaTime;
                if (r != null) r.enabled = Mathf.Repeat(t * 22f, 1f) > 0.4f;
                yield return null;
            }
            if (sparksPrefab != null)
            {
                var ps = Instantiate(sparksPrefab, center, Quaternion.identity);
                foreach (var child in ps.GetComponentsInChildren<ParticleSystem>(true)) { var main = child.main; main.startColor = sparkTint; }
                ps.Play(true);
                Destroy(ps.gameObject, 3f);
            }
            if (r != null) r.enabled = true;
            beam.SetActive(false);
        }
    }
}
