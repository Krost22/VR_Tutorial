using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Genera 3_TutorialMazmorra.unity uniendo el tutorial (2_Adaptacion) y la sala de runas de 1_GAME
    /// (sin el laberinto de la Sección 2). Las escenas originales no se modifican: se abren, se combinan
    /// en memoria y el resultado se guarda como copia. Menú: Tools/Game Feel/Construir escena combinada.
    ///
    /// La mazmorra se gira 90° y se sube al nivel del tutorial para que su puerta de entrada (Wall_B_0)
    /// quede al final del pasillo que se abre detrás de la pared Wall_A (11) del tutorial.
    ///
    /// Después de regenerarla hay que volver a hornearla: Tools/Iluminación Quest/Hornear escena combinada.
    /// </summary>
    public static class CombinedSceneBuilder
    {
        public const string TutorialPath = "Assets/_Custom/Scenes/2_Adaptacion.unity";
        public const string GamePath = "Assets/_Custom/Scenes/1_GAME.unity";
        public const string OutputPath = "Assets/_Custom/Scenes/3_TutorialMazmorra.unity";

        // Centro de la puerta de la mazmorra en la escena combinada (final del pasillo del tutorial).
        static readonly Vector3 DoorTarget = new Vector3(-9.0f, 0f, 16.64f);
        const float YawDegrees = 90f;

        [MenuItem("Tools/Game Feel/Construir escena combinada")]
        public static void BuildMenu() => Debug.Log("[GameFeel] " + Build());

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "Sal de Play mode primero.";
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) return "Hay escenas con cambios sin guardar; guárdalas primero.";

            var log = new List<string>();
            var tutorial = EditorSceneManager.OpenScene(TutorialPath, OpenSceneMode.Single);
            var game = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Additive);

            // 1) Quitar de la mazmorra lo que sobra: jugador propio, laberinto, fundido y postproceso duplicados.
            foreach (var root in game.GetRootGameObjects())
            {
                if (root.name == "Dommy-TestPlayerEditor" || root.name == "Auto Hand Player Container" || root.name == "LabyrinthManager" || root.name == "PostProcesado")
                { log.Add("quitado " + root.name); Object.DestroyImmediate(root); }
            }
            var gameRoots = game.GetRootGameObjects();
            Transform Find(string path) => gameRoots.Select(r => r.name == path.Split('/')[0] ? (path.Contains("/") ? r.transform.Find(path.Substring(path.IndexOf('/') + 1)) : r.transform) : null).FirstOrDefault(t => t != null);
            var seccion2 = Find("Escenario dungeon/Seccion 2");
            if (seccion2 != null) { Object.DestroyImmediate(seccion2.gameObject); log.Add("quitada Seccion 2 (laberinto)"); }
            var gameFader = Find("GameFeel/SceneFader");
            if (gameFader != null) Object.DestroyImmediate(gameFader.gameObject);

            // 2) Punto de giro: centro inferior de la puerta Wall_B_0.
            var door = Find("Escenario dungeon/Seccion 1/Wall_B_0");
            if (door == null) return "No se encontró Wall_B_0 en 1_GAME.";
            var rs = door.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var pivot = new Vector3(b.center.x, door.position.y, door.position.z);

            var parent = new GameObject("Mazmorra");
            SceneManager.MoveGameObjectToScene(parent, game);
            parent.transform.position = pivot;
            foreach (var root in game.GetRootGameObjects())
                if (root != parent) root.transform.SetParent(parent.transform, true);
            parent.transform.SetPositionAndRotation(DoorTarget, Quaternion.Euler(0f, YawDegrees, 0f));
            log.Add("mazmorra girada " + YawDegrees + "° y movida de " + pivot + " a " + DoorTarget);

            // 3) Los pilares se fijan a un ángulo absoluto del mundo: hay que sumarles el giro.
            foreach (var pillar in parent.GetComponentsInChildren<RunePuzzle.PillarProjector>(true))
                pillar.PerfectRotationY += YawDegrees;

            // 4) En el tutorial, el fondo temporal del pasillo (pared + portal a 1_GAME) ya no hace falta.
            foreach (var root in tutorial.GetRootGameObjects())
            {
                // Restos de Bakery (ya no se usa): su almacén no debe pasar a la escena combinada.
                if (root.name == "!ftraceLightmaps") { Object.DestroyImmediate(root); log.Add("quitado almacén de Bakery del tutorial"); continue; }
                if (root.name != "Pasillo_Mazmorra") continue;
                var end = root.transform.Find("Pasillo_FondoTemporal");
                if (end != null) { Object.DestroyImmediate(end.gameObject); log.Add("quitado fondo temporal del pasillo"); }
            }

            // 5) Unir y guardar como escena nueva (las originales quedan intactas).
            EditorSceneManager.MergeScenes(game, tutorial);
            // Las columnas de la puerta de la mazmorra no tenían collider.
            log.Add(DungeonAccessBuilder.AddEntranceColliders(tutorial, DoorTarget));
            bool ok = EditorSceneManager.SaveScene(tutorial, OutputPath, true);
            log.Add("guardada " + OutputPath + ": " + ok);

            // Volver a abrir el tutorial desde disco descarta la versión combinada en memoria.
            EditorSceneManager.OpenScene(TutorialPath, OpenSceneMode.Single);

            var list = EditorBuildSettings.scenes.ToList();
            if (!list.Any(s => s.path == OutputPath))
            {
                list.Add(new EditorBuildSettingsScene(OutputPath, true));
                EditorBuildSettings.scenes = list.ToArray();
                log.Add("añadida a Build Settings");
            }
            return string.Join("\n", log);
        }
    }
}
