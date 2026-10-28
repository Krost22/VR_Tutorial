using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Circuito del golem más corto y sencillo. Menú: Tools/Game Feel/Circuito del golem: corto y sencillo
    /// (sobre 2_Adaptacion; después hay que regenerar y hornear la escena combinada).
    ///
    /// Antes: slalom de unos 23 m entre 8 barreras de erizos (z 22 → 37) con la meta en z 41,4.
    /// Ahora: una sola puerta de erizos con un hueco ancho en el centro (z 22) y la meta en z 28,5, marcada
    /// con una línea de luz y la palabra META en el suelo. La runa de recompensa flota justo detrás de la meta.
    /// Las barreras quitadas quedan desactivadas (no borradas): para volver atrás basta con reactivarlas y
    /// mover "Meta_Golem/Meta" y "Recompensa_Runa" a su sitio anterior (z 41,4 y 42,6).
    /// </summary>
    public static class GolemTrackBuilder
    {
        public const float FinishZ = 28.5f;
        const float KeepBarriersUpToZ = 23f;
        const float CorridorWidth = 9.4f;

        [MenuItem("Tools/Game Feel/Circuito del golem: corto y sencillo")]
        public static void Menu() => Debug.Log("[GameFeel] " + Apply(SceneManager.GetActiveScene()));

        public static string Apply(Scene scene)
        {
            var log = new List<string>();
            var roots = scene.GetRootGameObjects();
            Transform FindIn(string path)
            {
                var parts = path.Split('/');
                var root = roots.FirstOrDefault(r => r.name == parts[0]);
                return root == null ? null : parts.Length == 1 ? root.transform : root.transform.Find(string.Join("/", parts.Skip(1)));
            }

            // 1) Meta más cerca.
            var finish = FindIn("Meta_Golem/Meta");
            if (finish == null) return "no hay Meta_Golem/Meta en " + scene.name;
            Undo.RecordObject(finish, "Meta");
            finish.position = new Vector3(finish.position.x, finish.position.y, FinishZ);
            if (finish.TryGetComponent<BoxCollider>(out var box))
            {
                // Que cubra todo el ancho del pasillo: el golem no puede pasar sin tocarla.
                Undo.RecordObject(box, "Meta");
                var s = finish.lossyScale;
                box.size = new Vector3(CorridorWidth / Mathf.Max(0.001f, Mathf.Abs(s.x)), box.size.y, box.size.z);
            }
            log.Add("meta movida a z " + FinishZ);

            // 2) La recompensa flota justo detrás de la meta.
            var reward = FindIn("Recompensa_Runa");
            if (reward != null)
            {
                Undo.RecordObject(reward, "Recompensa");
                reward.position = new Vector3(reward.position.x, reward.position.y, FinishZ + 1.2f);
                log.Add("runa de recompensa en z " + (FinishZ + 1.2f));
            }

            // 3) Una sola puerta de erizos: las barreras más allá se apagan.
            int off = 0, kept = 0;
            foreach (var r in roots.Where(r => r.name.StartsWith("Barrera Golem")))
            {
                bool keep = r.transform.position.z <= KeepBarriersUpToZ;
                if (r.activeSelf != keep) { Undo.RecordObject(r, "Barrera"); r.SetActive(keep); }
                if (keep) kept++; else off++;
            }
            log.Add("barreras: " + kept + " se quedan, " + off + " apagadas");

            // 4) Línea de meta visible en el suelo.
            var oldLine = roots.FirstOrDefault(r => r.name == "Linea_Meta");
            if (oldLine != null) Object.DestroyImmediate(oldLine);
            var line = new GameObject("Linea_Meta");
            SceneManager.MoveGameObjectToScene(line, scene);
            line.transform.position = new Vector3(0f, 0.012f, FinishZ);
            var glowMat = AssetDatabase.LoadAssetAtPath<Material>(GameFeelAssetBuilder.MaterialDir + "/Altar_Gema.mat");
            var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var strip = new GameObject("Franja");
            strip.transform.SetParent(line.transform, false);
            strip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            strip.transform.localScale = new Vector3(CorridorWidth, 0.25f, 1f);
            strip.AddComponent<MeshFilter>().sharedMesh = quad;
            var mr = strip.AddComponent<MeshRenderer>();
            mr.sharedMaterial = glowMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Custom/Fonts/Cinzel/CinzelDecorative-Bold SDF.asset");
            var textMat = AssetDatabase.LoadAssetAtPath<Material>(GameFeelAssetBuilder.MaterialDir + "/Altar_Texto.mat");
            if (font != null)
            {
                var go = new GameObject("Texto_META");
                go.transform.SetParent(line.transform, false);
                go.transform.localPosition = new Vector3(0f, 0.002f, -0.75f);
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // tumbado, se lee desde el lado del jugador
                var t = go.AddComponent<TextMeshPro>();
                t.font = font;
                if (textMat != null) t.fontSharedMaterial = textMat;
                t.text = "META";
                t.alignment = TextAlignmentOptions.Center;
                t.fontSize = 6f;
                t.rectTransform.sizeDelta = new Vector2(3f, 0.8f);
                t.color = new Color(1f, 0.78f, 0.35f);
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            log.Add("línea de meta en el suelo");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return string.Join("\n", log);
        }
    }
}
