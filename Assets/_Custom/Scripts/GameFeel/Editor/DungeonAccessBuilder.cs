using System.Collections.Generic;
using System.Linq;
using Autohand;
using VRTutorial.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Paso del tutorial a la mazmorra. Menú: Tools/Game Feel/Pasillo de la mazmorra: colliders y atajo de salto
    /// (se ejecuta sobre 2_Adaptacion; después hay que regenerar y hornear la escena combinada).
    ///
    /// - Colliders: el suelo, las paredes y el techo de Pasillo_Mazmorra no tenían; el jugador podía
    ///   atravesarlos o caerse. Se ponen cajas con al menos 30 cm de grosor hacia fuera del pasillo
    ///   (las piezas son planas y una caja de grosor 0 deja pasar objetos rápidos). También quedan estáticas
    ///   para que se horneen.
    /// - Atajo: TutorialSkip (mantener A) deja al jugador frente al pasillo con la pared abierta.
    ///
    /// Las columnas de la entrada de la mazmorra (de 1_GAME) reciben su collider al construir la combinada
    /// (<see cref="AddEntranceColliders"/>, llamado desde CombinedSceneBuilder).
    /// </summary>
    public static class DungeonAccessBuilder
    {
        public const string PassageName = "Pasillo_Mazmorra";
        public const string SkipName = "SaltarTutorial";
        const float MinThickness = 0.3f;
        // Delante de la pared Wall_A (11), mirando hacia la mazmorra (-X).
        static readonly Vector3 SkipDestination = new Vector3(-3.3f, 0f, 16.64f);
        const float SkipYaw = 270f;

        [MenuItem("Tools/Game Feel/Pasillo de la mazmorra: colliders y atajo de salto")]
        public static void Menu() => Debug.Log("[GameFeel] " + Apply(SceneManager.GetActiveScene()));

        public static string Apply(Scene scene)
        {
            var log = new List<string>();
            log.Add(AddPassageColliders(scene));
            log.Add(InstallSkip(scene));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return string.Join("\n", log);
        }

        // ---------------------------------------------------------------- Colliders

        public static string AddPassageColliders(Scene scene)
        {
            var passage = scene.GetRootGameObjects().FirstOrDefault(r => r.name == PassageName);
            if (passage == null) return "no hay " + PassageName + " en " + scene.name;

            // Solo piezas sólidas: fuera antorchas, efectos transparentes y el fondo temporal (pared + portal
            // a 1_GAME), que tiene su propio trigger y se quita al construir la escena combinada.
            var renderers = passage.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => !r.name.StartsWith("Antorcha"))
                .Where(r => r.GetComponentsInParent<Transform>(true).All(p => p.name != "Pasillo_FondoTemporal"))
                .Where(r => r.sharedMaterials.All(m => m == null || m.renderQueue < (int)UnityEngine.Rendering.RenderQueue.AlphaTest + 50))
                .ToArray();
            if (renderers.Length == 0) return PassageName + " sin piezas";
            var room = renderers[0].bounds;
            foreach (var r in renderers) room.Encapsulate(r.bounds);

            // Mismas banderas estáticas que el suelo del tutorial, para que el pasillo se hornee igual.
            var reference = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true))
                .FirstOrDefault(r => r.name.StartsWith("Tile_B") && GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI));
            var flags = reference != null ? GameObjectUtility.GetStaticEditorFlags(reference.gameObject) : StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;

            int added = 0;
            foreach (var r in renderers)
            {
                if (r.GetComponent<Collider>() == null) { AddOutwardBox(r, room.center); added++; }
                if (GameObjectUtility.GetStaticEditorFlags(r.gameObject) != flags)
                {
                    Undo.RecordObject(r.gameObject, "Estático");
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
                }
            }
            return PassageName + ": colliders nuevos " + added + " de " + renderers.Length + " piezas";
        }

        /// <summary>
        /// Colliders en las columnas de la entrada de la mazmorra (no tenían). Solo piezas a la altura del jugador.
        /// </summary>
        public static string AddEntranceColliders(Scene scene, Vector3 doorCenter, float radius = 3f)
        {
            var dungeon = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Mazmorra");
            if (dungeon == null) return "sin Mazmorra";
            int added = 0;
            var names = new List<string>();
            foreach (var r in dungeon.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponentInParent<Collider>(true) != null || r.GetComponent<Collider>() != null) continue;
                if (r.name.Contains("Torch") || r.name.Contains("Antorcha")) continue;
                var b = r.bounds;
                if (b.min.y > doorCenter.y + 2.2f || b.size.y < 0.3f) continue;
                if (Vector2.Distance(new Vector2(b.center.x, b.center.z), new Vector2(doorCenter.x, doorCenter.z)) > radius) continue;
                r.gameObject.AddComponent<BoxCollider>();
                names.Add(r.name);
                added++;
            }
            return "entrada de la mazmorra: colliders nuevos " + added + (added > 0 ? " (" + string.Join(", ", names) + ")" : "");
        }

        static void AddOutwardBox(Renderer r, Vector3 roomCenter)
        {
            // Piezas con volumen (arcos, columnas): MeshCollider, que respeta huecos como el de un arco.
            var mf = r.GetComponent<MeshFilter>();
            var s = r.bounds.size;
            if (mf != null && mf.sharedMesh != null && Mathf.Min(s.x, Mathf.Min(s.y, s.z)) >= MinThickness)
            {
                Undo.AddComponent<MeshCollider>(r.gameObject).sharedMesh = mf.sharedMesh;
                return;
            }
            var box = Undo.AddComponent<BoxCollider>(r.gameObject); // se ajusta solo a la malla
            var t = r.transform;
            Vector3 size = box.size, center = box.center;
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 local = Vector3.zero; local[axis] = 1f;
                Vector3 world = t.TransformVector(local);
                float worldThickness = size[axis] * world.magnitude;
                if (worldThickness >= MinThickness) continue;
                // Se engrosa hacia fuera del pasillo para no invadir el espacio por donde se camina.
                float outward = Mathf.Sign(Vector3.Dot(world.normalized, r.bounds.center - roomCenter));
                if (outward == 0f) outward = 1f;
                float newSize = MinThickness / world.magnitude;
                center[axis] += outward * (newSize - size[axis]) * 0.5f;
                size[axis] = newSize;
            }
            box.size = size;
            box.center = center;
        }

        // ---------------------------------------------------------------- Atajo de salto

        public static string InstallSkip(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            T Find<T>() where T : Component => roots.SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();

            var gameFeel = roots.FirstOrDefault(r => r.name == "GameFeel");
            if (gameFeel == null)
            {
                gameFeel = new GameObject("GameFeel");
                SceneManager.MoveGameObjectToScene(gameFeel, scene);
            }
            var holder = gameFeel.transform.Find(SkipName);
            if (holder == null)
            {
                holder = new GameObject(SkipName).transform;
                Undo.RegisterCreatedObjectUndo(holder.gameObject, "Atajo");
                holder.SetParent(gameFeel.transform, false);
            }
            var destination = holder.Find("Destino");
            if (destination == null)
            {
                destination = new GameObject("Destino").transform;
                destination.SetParent(holder, false);
            }
            destination.SetPositionAndRotation(SkipDestination, Quaternion.Euler(0f, SkipYaw, 0f));

            var skip = holder.TryGetComponent<TutorialSkip>(out var existing) ? existing : holder.gameObject.AddComponent<TutorialSkip>();
            skip.gaze = Find<GazeSequenceTutorial>();
            var finale = Find<TutorialFinale>();
            skip.wallToOpen = finale != null ? finale.doorToOpen : null;
            skip.player = Find<AutoHandPlayer>();
            skip.destination = destination;
            skip.fader = Find<SceneFader>();
            skip.progressMaterial = AssetDatabase.LoadAssetAtPath<Material>(GameFeelAssetBuilder.MaterialDir + "/Feel_ParticleAdditive.mat");
            EditorUtility.SetDirty(skip);

            // Comprobación: el destino no debe caer dentro de nada sólido y debe tener suelo debajo.
            Physics.SyncTransforms();
            bool blocked = Physics.CheckCapsule(SkipDestination + Vector3.up * 0.4f, SkipDestination + Vector3.up * 1.6f, 0.25f, ~0, QueryTriggerInteraction.Ignore);
            bool floor = Physics.Raycast(SkipDestination + Vector3.up, Vector3.down, 2f, ~0, QueryTriggerInteraction.Ignore);

            var missing = new List<string>();
            if (skip.gaze == null) missing.Add("GazeSequenceTutorial");
            if (skip.wallToOpen == null) missing.Add("pared (TutorialFinale.doorToOpen)");
            if (skip.player == null) missing.Add("AutoHandPlayer");
            if (skip.fader == null) missing.Add("SceneFader");
            return "atajo " + SkipName + " listo" + (missing.Count > 0 ? "; falta: " + string.Join(", ", missing) : "")
                   + "; destino " + (blocked ? "BLOQUEADO" : "libre") + ", " + (floor ? "con suelo" : "SIN SUELO");
        }
    }
}
