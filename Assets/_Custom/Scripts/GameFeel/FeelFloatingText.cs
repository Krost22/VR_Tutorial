using System.Collections;
using TMPro;
using UnityEngine;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Texto flotante en el mundo ("+1", "3/13") que sube, mira al jugador y se desvanece.
    /// </summary>
    public static class FeelFloatingText
    {
        public static void Show(string text, Vector3 position, Color color, float size = 0.35f, float duration = 1.4f)
        {
            var go = new GameObject("[FloatingText]");
            go.transform.position = position;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);
            tmp.rectTransform.sizeDelta = new Vector2(2f, 0.5f);
            FeelTween.Instance.StartCoroutine(Animate(go.transform, tmp, duration));
        }

        private static IEnumerator Animate(Transform t, TextMeshPro tmp, float duration)
        {
            Vector3 start = t.position;
            Color baseColor = tmp.color;
            float time = 0f;
            while (time < duration && t != null)
            {
                time += Time.deltaTime;
                float k = time / duration;
                t.position = start + Vector3.up * (0.35f * FeelTween.Evaluate(FeelEase.OutCubic, k));
                float scale = FeelTween.Evaluate(FeelEase.OutBack, Mathf.Min(1f, k * 4f));
                t.localScale = Vector3.one * Mathf.Max(0.01f, scale);
                var cam = Camera.main;
                if (cam != null) t.rotation = Quaternion.LookRotation(t.position - cam.transform.position);
                var c = baseColor; c.a = 1f - Mathf.Clamp01((k - 0.6f) / 0.4f);
                tmp.color = c;
                yield return null;
            }
            if (t != null) Object.Destroy(t.gameObject);
        }
    }
}
