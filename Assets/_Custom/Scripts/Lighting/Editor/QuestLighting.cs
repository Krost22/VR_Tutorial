using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace VRTutorial.Lighting.EditorTools
{
    /// <summary>
    /// Iluminación y postproceso pensados para Meta Quest 2. Menú: Tools/Iluminación Quest.
    ///
    /// - Postproceso: un solo perfil con corrección de color (va en la LUT, sin costo extra) y viñeta suave.
    ///   El bloom queda en el perfil pero apagado: en el Quest 2 cuesta alrededor de 1 ms por frame.
    /// - Horneado: prepara la escena para el lightmapper de Unity (Progressive GPU): luces de antorcha
    ///   horneadas, sondas de luz para los objetos que se mueven, sin sombras en tiempo real, y lanza el
    ///   horneado. Pensado para 3_TutorialMazmorra, que se regenera desde las escenas originales:
    ///   después de regenerarla hay que volver a hornearla.
    /// </summary>
    public static class QuestLighting
    {
        public const string ProfilePath = "Assets/_Custom/Lighting/PostProceso_Quest.asset";
        public const string LightingSettingsPath = "Assets/_Custom/Lighting/Horneado_Quest.lighting";
        public const string CombinedScenePath = "Assets/_Custom/Scenes/3_TutorialMazmorra.unity";
        public const string VolumeName = "PostProcesado";
        public const string ProbesName = "SondasDeLuz";

        // Horneado: equilibrio entre nitidez y memoria/tiempo de horneado en el Quest 2.
        const float TexelsPerUnit = 40f;
        const int MaxLightmapSize = 2048;
        const int Bounces = 2;
        // Radio de las sombras horneadas de las antorchas: bordes suaves, como la llama.
        const float TorchShadowRadius = 0.08f;
        // Antorchas de la mazmorra (en 1_GAME eran luces de Bakery: intensidad 4, alcance 6,5;
        // en Unity se ven parecidas con la mitad de intensidad).
        static readonly Color DungeonTorchColor = new Color(1f, 0.596f, 0f);
        const float DungeonTorchIntensity = 2f;
        const float DungeonTorchRange = 6.5f;
        // Cámara del jugador.
        const float CameraFarClip = 100f;
        static readonly Color CameraBackground = new Color(0.02f, 0.02f, 0.03f);
        // Rejilla de sondas de luz.
        const float ProbeSpacing = 2f;
        static readonly float[] ProbeHeights = { 0.4f, 1.4f, 2.6f };

        // ---------------------------------------------------------------- Postproceso

        [MenuItem("Tools/Iluminación Quest/Crear o actualizar perfil de postproceso")]
        public static void CreateProfileMenu() => Debug.Log("[Iluminación] perfil: " + AssetDatabase.GetAssetPath(GetOrCreateProfile()));

        public static VolumeProfile GetOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ProfilePath));
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            // Contraste y saturación un poco más altos: la piedra se veía lavada.
            var color = Get<ColorAdjustments>(profile);
            color.active = true;
            color.contrast.Override(14f);
            color.saturation.Override(10f);
            color.postExposure.Override(0.1f);

            // Sombras un poco frías y luces cálidas: separa el fuego de la piedra y da profundidad.
            var split = Get<SplitToning>(profile);
            split.active = true;
            split.shadows.Override(new Color(0.42f, 0.48f, 0.62f));
            split.highlights.Override(new Color(0.62f, 0.52f, 0.42f));
            split.balance.Override(-15f);

            // Viñeta suave: centra la mirada; en VR se aplica por ojo y no se nota como un marco.
            var vignette = Get<Vignette>(profile);
            vignette.active = true;
            vignette.color.Override(Color.black);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);
            vignette.rounded.Override(false);

            // Bloom apagado por rendimiento. Para probarlo: activar este override en el perfil.
            var bloom = Get<Bloom>(profile);
            bloom.active = false;
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.5f);
            bloom.scatter.Override(0.6f);
            bloom.highQualityFiltering.Override(false);
            bloom.downscale.Override(BloomDownscaleMode.Quarter);
            bloom.maxIterations.Override(3);

            // Tonemapping no: con HDR apagado (lo correcto en Quest) solo apagaría los colores.
            if (profile.TryGet<Tonemapping>(out var tone)) tone.active = false;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var c))
            {
                c = profile.Add<T>(false);
                c.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(c, profile);
            }
            return c;
        }

        [MenuItem("Tools/Iluminación Quest/Añadir postproceso a la escena activa")]
        public static void AddPostMenu() => Debug.Log("[Iluminación] " + AddPostToScene(SceneManager.GetActiveScene()));

        /// <summary>Crea (o actualiza) el volumen global y activa el postproceso en las cámaras del jugador.</summary>
        public static string AddPostToScene(Scene scene)
        {
            var profile = GetOrCreateProfile();
            var go = scene.GetRootGameObjects().FirstOrDefault(r => r.name == VolumeName);
            if (go == null)
            {
                go = new GameObject(VolumeName);
                SceneManager.MoveGameObjectToScene(go, scene);
            }
            var volume = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.sharedProfile = profile;

            int cams = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            {
                if (cam.targetTexture != null) continue; // cámaras de render a textura (espejos, UI)
                // Cámara del jugador: la escena mide unos 80 m, así que 100 m de plano lejano bastan y dan
                // más precisión de profundidad. Fondo casi negro: por las rendijas de la mazmorra no debe verse cielo.
                Undo.RecordObject(cam, "Cámara");
                if (cam.farClipPlane > CameraFarClip) cam.farClipPlane = CameraFarClip;
                cam.backgroundColor = CameraBackground;
                EditorUtility.SetDirty(cam);

                var data = cam.GetUniversalAdditionalCameraData();
                if (data.renderPostProcessing) continue;
                Undo.RecordObject(data, "Postproceso");
                data.renderPostProcessing = true;
                // El antialiasing de postproceso es caro en Quest: se deja apagado (se usa MSAA).
                data.antialiasing = AntialiasingMode.None;
                EditorUtility.SetDirty(data);
                cams++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            return scene.name + ": volumen " + VolumeName + " listo, postproceso activado en " + cams + " cámara(s)";
        }

        // ---------------------------------------------------------------- Ajustes del pipeline Android

        [MenuItem("Tools/Iluminación Quest/Ajustar pipeline de Quest (sin sombras, MSAA 4x)")]
        public static void QuestPipelineMenu() => Debug.Log("[Iluminación] " + ApplyQuestPipelineSettings());

        /// <summary>
        /// Con todo horneado ninguna luz proyecta sombras en tiempo real: se quitan del pipeline de Android
        /// para no compilar ni evaluar las variantes de sombras.
        /// </summary>
        public static string ApplyQuestPipelineSettings()
        {
            var names = QualitySettings.names;
            var log = new List<string>();
            // Calidad por defecto de Android (la que usa el Quest).
            int androidLevel = GetAndroidQualityLevel();
            var asset = QualitySettings.GetRenderPipelineAssetAt(androidLevel) as UniversalRenderPipelineAsset;
            if (asset == null) return "No se encontró el pipeline de Android.";
            var so = new SerializedObject(asset);
            void Set(string prop, bool value)
            {
                var p = so.FindProperty(prop);
                if (p != null && p.boolValue != value) { p.boolValue = value; log.Add(prop + "=" + value); }
            }
            Set("m_MainLightShadowsSupported", false);
            Set("m_AdditionalLightShadowsSupported", false);
            Set("m_SoftShadowsSupported", false);
            // MSAA 4x: en VR los bordes dentados se notan mucho y en la GPU por tiles del Quest es casi gratis.
            var msaa = so.FindProperty("m_MSAA");
            if (msaa != null && msaa.intValue != 4) { msaa.intValue = 4; log.Add("MSAA=4x"); }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return asset.name + " (" + names[androidLevel] + "): " + (log.Count == 0 ? "sin cambios" : string.Join(", ", log));
        }

        static int GetAndroidQualityLevel()
        {
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            if (qs != null)
            {
                var so = new SerializedObject(qs);
                var list = so.FindProperty("m_PerPlatformDefaultQuality");
                if (list != null)
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        var e = list.GetArrayElementAtIndex(i);
                        if (e.FindPropertyRelative("first").stringValue == "Android")
                            return e.FindPropertyRelative("second").intValue;
                    }
            }
            return QualitySettings.GetQualityLevel();
        }

        // ---------------------------------------------------------------- Horneado (lightmapper de Unity)

        [MenuItem("Tools/Iluminación Quest/Preparar escena activa para hornear")]
        public static void PrepareMenu() => Debug.Log("[Iluminación] " + PrepareSceneForBake(SceneManager.GetActiveScene()));

        /// <summary>
        /// Deja la escena lista para hornear sin hornear todavía:
        /// quita los restos de Bakery, enciende las luces de antorcha en modo Baked (no cuestan en tiempo real),
        /// luz direccional sin sombras, transparentes fuera del horneado, rejilla de sondas de luz y
        /// ajustes de horneado propios (para no pisar los de la plantilla ni los de la escena original).
        /// </summary>
        public static string PrepareSceneForBake(Scene scene)
        {
            var log = new List<string>();

            // 0) Restos de Bakery: el almacén de la escena y los componentes cuyo script ya no existe.
            var stores = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name == "!ftraceLightmaps").Select(t => t.gameObject).ToArray();
            foreach (var old in stores) Undo.DestroyObjectImmediate(old);
            if (stores.Length > 0) log.Add("quitado almacén de Bakery: " + stores.Length);
            int missing = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                    missing += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            if (missing > 0) log.Add("componentes sin script quitados (Bakery): " + missing);

            var roots = scene.GetRootGameObjects();

            // Las antorchas de la mazmorra solo tenían luz de Bakery: se les pone una luz de Unity equivalente.
            int added = 0;
            foreach (var root in roots)
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "Point Light" || t.parent == null || !t.parent.name.StartsWith("Torch")) continue;
                if (t.GetComponent<Light>() != null) continue;
                var l = Undo.AddComponent<Light>(t.gameObject);
                l.type = LightType.Point;
                l.color = DungeonTorchColor;
                l.intensity = DungeonTorchIntensity;
                l.range = DungeonTorchRange;
                l.lightmapBakeType = LightmapBakeType.Baked;
                added++;
            }
            if (added > 0) log.Add("luces nuevas en antorchas de la mazmorra: " + added);

            // 1) Luces.
            int baked = 0;
            foreach (var root in roots)
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.type == LightType.Directional)
                {
                    if (light.shadows != LightShadows.None)
                    {
                        Undo.RecordObject(light, "Sin sombras");
                        light.shadows = LightShadows.None;
                        log.Add("luz direccional " + light.name + " sin sombras en tiempo real");
                    }
                    continue;
                }
                if (light.lightmapBakeType == LightmapBakeType.Realtime) continue; // brillos y destellos de gameplay
                if (light.type != LightType.Point && light.type != LightType.Spot) continue;

                // Las luces Baked solo existen en el lightmap: encendidas no cuestan nada en tiempo real.
                Undo.RecordObject(light, "Luz horneada");
                light.lightmapBakeType = LightmapBakeType.Baked;
                light.enabled = true;
                light.shadows = LightShadows.Soft;
                light.shapeRadius = TorchShadowRadius;
                EditorUtility.SetDirty(light);
                baked++;
            }
            log.Add("luces horneadas: " + baked);

            // 2) Transparentes y efectos fuera del horneado: no deben dejar sombras horneadas.
            int cleared = 0;
            foreach (var root in roots)
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI)) continue;
                bool transparent = r.sharedMaterials.Any(m => m != null && m.renderQueue >= (int)RenderQueue.AlphaTest + 50);
                if (!transparent) continue;
                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject) & ~StaticEditorFlags.ContributeGI;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
                cleared++;
            }
            if (cleared > 0) log.Add("transparentes fuera del horneado: " + cleared);

            // 3) Sondas de luz para manos, cubos, balón, golem y runas.
            log.Add("sondas de luz: " + BuildProbeGrid(scene));

            // 4) Ajustes de horneado y datos de iluminación propios de esta escena.
            if (SceneManager.GetActiveScene() == scene)
            {
                Lightmapping.lightingSettings = GetOrCreateLightingSettings();
                log.Add("ajustes de horneado: " + LightingSettingsPath);
                if (Lightmapping.lightingDataAsset != null)
                {
                    var dataPath = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                    var sceneFolder = scene.path.Substring(0, scene.path.Length - ".unity".Length) + "/";
                    if (!dataPath.StartsWith(sceneFolder))
                    {
                        Lightmapping.lightingDataAsset = null;
                        log.Add("la escena ya no usa " + dataPath);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            return string.Join("\n", log);
        }

        /// <summary>Ajustes de horneado para Quest: solo GI horneada, sin mapas direccionales, AO suave.</summary>
        public static LightingSettings GetOrCreateLightingSettings()
        {
            var s = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (s == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LightingSettingsPath));
                s = new LightingSettings { name = "Horneado_Quest" };
                AssetDatabase.CreateAsset(s, LightingSettingsPath);
            }
            s.bakedGI = true;
            s.realtimeGI = false;
            s.mixedBakeMode = MixedLightingMode.IndirectOnly;     // no hay luces Mixed: nada de shadowmask
            s.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            s.lightmapResolution = TexelsPerUnit;
            s.lightmapMaxSize = MaxLightmapSize;
            s.lightmapPadding = 4;                                // menos sangrado entre islas al comprimir
            s.lightmapCompression = LightmapCompression.HighQuality;
            s.directionalityMode = LightmapsMode.NonDirectional;  // la mitad de memoria que Directional
            s.maxBounces = Bounces;
            s.directSampleCount = 32;
            s.indirectSampleCount = 512;
            s.environmentSampleCount = 256;
            s.filteringMode = LightingSettings.FilterMode.Auto;   // denoiser automático
            // Oclusión ambiental horneada: oscurece rincones y juntas de la piedra sin costo en el Quest.
            s.ao = true;
            s.aoMaxDistance = 0.6f;
            s.aoExponentIndirect = 1f;
            s.aoExponentDirect = 0f;
            s.prioritizeView = false;
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            return s;
        }

        static int BuildProbeGrid(Scene scene)
        {
            foreach (var old in scene.GetRootGameObjects().Where(r => r.name == ProbesName).ToArray())
                Object.DestroyImmediate(old);

            // Volumen de la escena: renderers estáticos (suelo, paredes, techo).
            Bounds? env = null;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (!GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI)) continue;
                if (env == null) env = r.bounds; else { var b = env.Value; b.Encapsulate(r.bounds); env = b; }
            }
            if (env == null) return 0;
            var area = env.Value;

            Physics.SyncTransforms();
            const int mask = ~0;
            var q = QueryTriggerInteraction.Ignore;
            bool IsEnvironment(Collider c) => c.attachedRigidbody == null && c.GetComponentInParent<CharacterController>() == null;

            var positions = new List<Vector3>();
            for (float x = area.min.x + ProbeSpacing * 0.5f; x <= area.max.x; x += ProbeSpacing)
            for (float z = area.min.z + ProbeSpacing * 0.5f; z <= area.max.z; z += ProbeSpacing)
            {
                var origin = new Vector3(x, area.max.y + 1f, z);
                var hits = Physics.RaycastAll(origin, Vector3.down, area.size.y + 3f, mask, q)
                    .Where(h => h.normal.y > 0.7f && IsEnvironment(h.collider))
                    .OrderByDescending(h => h.point.y).ToArray();
                for (int i = 0; i < hits.Length; i++)
                {
                    var floor = hits[i].point;
                    bool hasCeiling = Physics.Raycast(floor + Vector3.up * 0.05f, Vector3.up, out var up, 12f, mask, q);
                    // El impacto más alto sin nada encima es el tejado, no un suelo.
                    if (i == 0 && !hasCeiling && hits.Length > 1) continue;
                    float ceiling = hasCeiling ? up.point.y : floor.y + 4f;
                    foreach (var h in ProbeHeights)
                    {
                        var p = new Vector3(x, floor.y + h, z);
                        if (p.y > ceiling - 0.15f) continue;
                        if (Physics.CheckSphere(p, 0.12f, mask, q)) continue; // dentro de una pared o un mueble
                        if (positions.Any(o => (o - p).sqrMagnitude < 0.25f)) continue;
                        positions.Add(p);
                    }
                }
            }

            var go = new GameObject(ProbesName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();
            return positions.Count;
        }

        [MenuItem("Tools/Iluminación Quest/Hornear escena combinada")]
        public static void BakeCombinedMenu() => Debug.Log("[Iluminación] " + StartBakeCombined());

        /// <summary>Abre 3_TutorialMazmorra, la prepara, la guarda y empieza el horneado (asíncrono).</summary>
        public static string StartBakeCombined()
        {
            if (EditorApplication.isPlaying) return "Sal de Play mode primero.";
            if (Lightmapping.isRunning) return "Ya hay un horneado en curso.";
            var scene = SceneManager.GetActiveScene();
            if (scene.path != CombinedScenePath)
            {
                if (scene.isDirty) return "La escena activa tiene cambios sin guardar.";
                scene = EditorSceneManager.OpenScene(CombinedScenePath, OpenSceneMode.Single);
            }
            var log = PrepareSceneForBake(scene);
            log += "\n" + AddPostToScene(scene);
            log += "\n" + ApplyQuestPipelineSettings();
            EditorSceneManager.SaveScene(scene);
            Lightmapping.bakeCompleted -= OnBakeCompleted;
            Lightmapping.bakeCompleted += OnBakeCompleted;
            bool started = Lightmapping.BakeAsync();
            return log + "\n" + (started ? "Horneado en curso." : "El horneado no empezó (revisa la consola).");
        }

        static void OnBakeCompleted()
        {
            Lightmapping.bakeCompleted -= OnBakeCompleted;
            var scene = SceneManager.GetActiveScene();
            if (scene.path != CombinedScenePath) return;
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Iluminación] Horneado terminado: " + LightmapSettings.lightmaps.Length + " lightmap(s). Escena guardada.");
        }
    }
}
