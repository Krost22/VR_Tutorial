using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRTutorial.Tutorial;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Monta la fase 1 nueva (mirada guiada, estilo Halo) en la escena activa (2_Adaptacion):
    /// 5 luces en orden (frente, arriba, abajo, izquierda, derecha), barrera de 5 haces con una gema por luz,
    /// texto de ayuda para la primera luz y el componente GazeSequenceTutorial.
    /// Lo viejo no se borra: LookDirectionTutorial queda desactivado y las runas y la pared marrón, apagadas.
    /// Menú: Tools/Game Feel/Construir fase 1 (mirada guiada). Se puede volver a ejecutar: reemplaza "Fase1_Mirada".
    /// </summary>
    public static class GazePhaseBuilder
    {
        const string RootName = "Fase1_Mirada";
        const string MatDir = GameFeelAssetBuilder.MaterialDir;
        static readonly Color Cyan = new Color(0.35f, 0.9f, 1f);

        // Cabeza del jugador al empezar (el rig está en (0.29, 0, -0.13); ojos a ~1,6 m).
        static readonly Vector3 Head = new Vector3(0.3f, 1.6f, -0.1f);

        // Frente (acierto fácil), arriba, abajo, izquierda y derecha: dentro de ±60° a los lados, +32° y -30°.
        static readonly (string name, Vector3 pos)[] Targets =
        {
            ("Luz_1_Frente",    new Vector3(0.3f, 1.55f, 2.9f)),
            ("Luz_2_Arriba",    new Vector3(0.3f, 3.15f, 2.45f)),
            ("Luz_3_Abajo",     new Vector3(0.3f, 0.35f, 2.15f)),
            ("Luz_4_Izquierda", new Vector3(-2.3f, 1.6f, 1.4f)),
            ("Luz_5_Derecha",   new Vector3(2.9f, 1.6f, 1.4f)),
        };

        const float BarrierZ = 3.3f;
        // Columna de gemas a un lado del centro, para no quedar detrás de la primera luz.
        const float GemX = 1.4f;
        static readonly float[] BeamHeights = { 2.05f, 1.65f, 1.25f, 0.85f, 0.45f };

        [MenuItem("Tools/Game Feel/Construir fase 1 (mirada guiada)")]
        public static void BuildMenu() => Debug.Log("[GameFeel] " + Build());

        public static string Build()
        {
            var log = new List<string>();
            var scene = SceneManager.GetActiveScene();
            var old = Object.FindObjectsByType<LookDirectionTutorial>(FindObjectsInactive.Include).FirstOrDefault();
            if (old == null) return "No se encontró LookDirectionTutorial en la escena activa.";

            // Recursos
            var hum = BuildHum();
            var orb = Lit("Feel_Orbe", new Color(0.05f, 0.22f, 0.28f), Cyan * 1.6f, 0.6f);
            var gemOff = Lit("Feel_GemaApagada", new Color(0.08f, 0.1f, 0.12f), new Color(0.02f, 0.06f, 0.08f), 0.8f);
            var gemOn = Lit("Feel_GemaEncendida", new Color(0.2f, 0.6f, 0.7f), Cyan * 2f, 0.8f);
            var beamMat = Lit("Feel_HazMirada", new Color(0.15f, 0.6f, 0.72f), Cyan * 0.8f, 0.3f);
            var guide = Additive("Feel_Guia", Color.white, 50);
            var ring = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Feel_RingGlow.mat");
            var glowMat = Additive("Feel_BrilloOrbe", new Color(0.35f, 0.9f, 1f, 0.55f), 0);
            var sparks = AssetDatabase.LoadAssetAtPath<ParticleSystem>(GameFeelAssetBuilder.PrefabDir + "/Feel_Sparks.prefab");
            var magic = AssetDatabase.LoadAssetAtPath<ParticleSystem>(GameFeelAssetBuilder.PrefabDir + "/Feel_Magic.prefab");
            var dust = AssetDatabase.LoadAssetAtPath<ParticleSystem>(GameFeelAssetBuilder.PrefabDir + "/Feel_Dust.prefab");
            AudioClip Clip(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(GameFeelAssetBuilder.AudioDir + "/" + n + ".wav");

            // Raíz nueva
            foreach (var r in scene.GetRootGameObjects().Where(r => r.name == RootName).ToArray()) Object.DestroyImmediate(r);
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            // Luces
            var lights = new GameObject("Luces").transform; lights.SetParent(root.transform, false);
            var targets = new List<Transform>();
            foreach (var (name, pos) in Targets)
            {
                var t = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                t.name = name;
                Object.DestroyImmediate(t.GetComponent<Collider>());
                t.transform.SetParent(lights, false);
                t.transform.position = pos;
                t.transform.localScale = Vector3.one * 0.2f;
                var mr = t.GetComponent<MeshRenderer>();
                mr.sharedMaterial = orb;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // Halo suave alrededor (mira siempre a la cámara desde el script).
                var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
                g.name = "Brillo";
                Object.DestroyImmediate(g.GetComponent<Collider>());
                g.transform.SetParent(t.transform, false);
                g.transform.localScale = Vector3.one * 1.9f;
                g.transform.rotation = Quaternion.LookRotation(pos - Head);
                var gr = g.GetComponent<MeshRenderer>();
                gr.sharedMaterial = glowMat;
                gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                targets.Add(t.transform);
            }

            // Texto solo para la primera luz
            var hint = new GameObject("Ayuda_PrimeraLuz");
            hint.transform.SetParent(root.transform, false);
            hint.transform.position = Targets[0].pos + new Vector3(0f, -0.38f, 0f);
            hint.transform.rotation = Quaternion.LookRotation(Targets[0].pos - Head);
            var tmp = hint.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "Mira fijamente la luz";
            tmp.fontSize = 1.5f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.97f, 1f);
            tmp.rectTransform.sizeDelta = new Vector2(2.6f, 0.5f);

            // Barrera: postes copiados de la barrera láser del movimiento, 5 haces, columna central con gemas.
            var barrier = new GameObject("Barrera_Mirada");
            barrier.transform.SetParent(root.transform, false);
            foreach (var src in new[] { "Enviaroment/Tutorial Movement/pared laser/Scifi_SmallLaser_Barrier", "Enviaroment/Tutorial Movement/pared laser/Scifi_SmallLaser_Barrier.002" })
            {
                var s = FindPath(scene, src);
                if (s == null) { log.Add("no se encontró " + src); continue; }
                var copy = Object.Instantiate(s.gameObject, barrier.transform);
                copy.name = "Poste" + (src.EndsWith("002") ? "_Derecho" : "_Izquierdo");
                copy.transform.SetPositionAndRotation(new Vector3(s.position.x, s.position.y, BarrierZ), s.rotation);
                copy.transform.localScale = s.lossyScale;
            }
            var postMat = FindPath(scene, "Enviaroment/Tutorial Movement/pared laser/Scifi_SmallLaser_Barrier")?.GetComponent<Renderer>()?.sharedMaterial;

            var beamObjs = new List<GameObject>();
            for (int i = 0; i < BeamHeights.Length; i++)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                b.name = "Haz " + i;
                Object.DestroyImmediate(b.GetComponent<Collider>());
                b.transform.SetParent(barrier.transform, false);
                b.transform.position = new Vector3(0f, BeamHeights[i], BarrierZ);
                b.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                b.transform.localScale = new Vector3(0.045f, 4.35f, 0.045f);
                var mr = b.GetComponent<MeshRenderer>();
                mr.sharedMaterial = beamMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var pulse = b.AddComponent<LaserPulse>();
                pulse.laserRenderer = mr;
                pulse.emissionColor = Cyan;
                pulse.pulseSpeed = 2f;
                pulse.pulseIntensity = 1.3f;
                beamObjs.Add(b);
            }

            var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "Columna";
            Object.DestroyImmediate(column.GetComponent<Collider>());
            column.transform.SetParent(barrier.transform, false);
            column.transform.position = new Vector3(GemX, 1.15f, BarrierZ + 0.03f);
            column.transform.localScale = new Vector3(0.07f, 1.15f, 0.07f);
            if (postMat != null) column.GetComponent<MeshRenderer>().sharedMaterial = postMat;

            var gems = new List<Renderer>();
            for (int i = 0; i < BeamHeights.Length; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                g.name = "Gema " + i;
                Object.DestroyImmediate(g.GetComponent<Collider>());
                g.transform.SetParent(barrier.transform, false);
                g.transform.position = new Vector3(GemX, BeamHeights[i], BarrierZ - 0.05f);
                g.transform.localScale = Vector3.one * 0.12f;
                var mr = g.GetComponent<MeshRenderer>();
                mr.sharedMaterial = gemOff;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                gems.Add(mr);
            }

            var block = barrier.AddComponent<BoxCollider>();
            block.center = new Vector3(0f, 1.25f, BarrierZ);
            block.size = new Vector3(9f, 2.5f, 0.15f);

            var set = barrier.AddComponent<LaserBeamSet>();
            set.beams = beamObjs;
            set.offSound = Clip("feel_powerdown");
            set.volume = 0.6f;
            set.sparksPrefab = sparks;
            set.sparkTint = Cyan;

            var disappear = barrier.AddComponent<FeelDisappear>();
            disappear.mode = FeelDisappear.Mode.SinkDown;
            disappear.sinkDistance = 2.6f;
            disappear.duration = 0.9f;
            disappear.anticipation = 0.35f;
            disappear.flicker = false;
            disappear.sound = Clip("feel_rumble");
            disappear.volume = 0.7f;
            disappear.particlesPrefab = dust;

            // Respuesta al llegar a la gema
            var feelRoot = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "GameFeel");
            var old2 = feelRoot != null ? feelRoot.transform.Find("Feel_GemaEncendida") : null;
            if (old2 != null) Object.DestroyImmediate(old2.gameObject);
            var arriveGo = new GameObject("Feel_GemaEncendida");
            arriveGo.transform.SetParent(feelRoot != null ? feelRoot.transform : root.transform, false);
            var arrive = arriveGo.AddComponent<FeelBurst>();
            arrive.clip = Clip("feel_lock");
            arrive.volume = 0.7f;
            arrive.pitchRisesWithStep = true;
            arrive.particlesPrefab = magic;
            arrive.tintParticles = true;
            arrive.particleTint = Cyan;
            arrive.particleScale = 0.6f;
            arrive.hapticAmplitude = 0.4f;
            arrive.hapticDuration = 0.06f;

            // Componente nuevo, con las mismas referencias que el viejo
            var gaze = old.GetComponent<GazeSequenceTutorial>();
            if (gaze == null) gaze = old.gameObject.AddComponent<GazeSequenceTutorial>();
            gaze.playerCamera = old.playerCamera;
            gaze.movementScriptToDisable = old.movementScriptToDisable;
            gaze.targets = targets;
            gaze.firstHint = hint;
            gaze.beams = set;
            gaze.gems = gems;
            gaze.gemOnMaterial = gemOn;
            gaze.barrier = barrier;
            gaze.reticleMaterial = guide;
            gaze.ringMaterial = ring;
            gaze.arrowMaterial = guide;
            gaze.guideColor = Cyan;
            gaze.humClip = hum;
            gaze.lookFeedback = old.lookFeedback;
            gaze.arriveFeedback = arrive;
            gaze.completeFeedback = old.completeFeedback;
            gaze.canvasHUD = old.canvasHUD;
            gaze.popUp = old.popUp;
            gaze.popUpInicial = old.popUpInicial;
            gaze.nextStep = old.nextStep;
            gaze.enabled = true;

            // Lo viejo, apagado pero sin borrar
            old.enabled = false;
            foreach (var lp in old.lookPoints) if (lp.pointObject != null) lp.pointObject.SetActive(false);
            if (old.pared != null) old.pared.SetActive(false);
            log.Add("fase 1 nueva: " + targets.Count + " luces, " + beamObjs.Count + " haces; LookDirectionTutorial, runas y pared marrón desactivados");

            EditorSceneManager.MarkSceneDirty(scene);
            return string.Join("\n", log);
        }

        static Transform FindPath(Scene scene, string path)
        {
            var parts = path.Split('/');
            var root = scene.GetRootGameObjects().FirstOrDefault(r => r.name == parts[0]);
            return root == null ? null : (parts.Length == 1 ? root.transform : root.transform.Find(string.Join("/", parts.Skip(1))));
        }

        static Material Lit(string name, Color baseColor, Color emission, float smoothness)
        {
            string path = MatDir + "/" + name + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", smoothness);
            m.SetColor("_EmissionColor", emission);
            // RealtimeEmissive mantiene la palabra clave _EMISSION al reimportar (con None, URP la quita).
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.EnableKeyword("_EMISSION");
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Additive(string name, Color color, int queueOffset)
        {
            string path = MatDir + "/" + name + ".mat";
            var src = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Feel_ParticleAdditive.mat");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(src);
            m.SetColor("_BaseColor", color);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + queueOffset; // la guía va encima de los demás transparentes
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Zumbido suave en bucle (2 s): quinta 220/330 Hz con trémolo lento. Bucle sin cortes.</summary>
        static AudioClip BuildHum()
        {
            const int rate = 44100;
            string path = GameFeelAssetBuilder.AudioDir + "/feel_gaze_hum.wav";
            int n = rate * 2;
            var data = new short[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float trem = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 2f * t);
                float s = 0.55f * Mathf.Sin(2f * Mathf.PI * 220f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 330f * t) + 0.12f * Mathf.Sin(2f * Mathf.PI * 660f * t);
                data[i] = (short)Mathf.RoundToInt(Mathf.Clamp(s * trem * 0.55f, -1f, 1f) * 32767f);
            }
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + n * 2);
                w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(n * 2);
                foreach (var s in data) w.Write(s);
            }
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
