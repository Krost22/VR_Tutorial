using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autohand;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Mejoras de la sala de runas que se aplican sobre la escena combinada al construirla
    /// (CombinedSceneBuilder las llama después de unir las escenas; 1_GAME no se toca):
    ///
    /// 1. Entrada abierta: Wall_B_0 tenía una caja que tapaba todo el arco (en 1_GAME esa puerta no llevaba
    ///    a ningún sitio). Se cambia por MeshColliders de la pared y del arco, que siguen el borde del hueco.
    /// 2. Audio con las buenas prácticas del tutorial: fuentes 3D con atenuación de sala, sin la "explosión"
    ///    estridente, pilares con sonido de activación, tono de sala y crepitar en las antorchas.
    /// 3. Altar de las runas nuevo: pedestal escalonado de piedra con molduras de bronce, huecos con anillo
    ///    del color de su elemento y círculo central que se enciende con el progreso (RuneAltarGlow).
    /// 4. Final: al abrirse la puerta principal aparece un portal; al cruzarlo, "¡Gracias por jugar!" y
    ///    el juego se reinicia (EndPortal).
    /// </summary>
    public static class DungeonFinaleBuilder
    {
        const string MatDir = GameFeelAssetBuilder.MaterialDir;
        const string MeshDir = GameFeelAssetBuilder.Root + "/Meshes";
        const string FontAssetPath = "Assets/_Custom/Fonts/Cinzel/CinzelDecorative-Bold SDF.asset";
        static readonly Vector3 EndRoomPosition = new Vector3(0f, -60f, 0f);

        public static string Apply(Scene scene)
        {
            var log = new List<string>();
            var dungeon = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Mazmorra");
            if (dungeon == null) return "sin Mazmorra";
            log.Add(OpenEntrance(dungeon.transform));
            log.Add(DungeonAudio(dungeon.transform));
            log.Add(BuildAltar(dungeon.transform));
            log.Add(BuildEndPortal(scene, dungeon.transform));
            return string.Join("\n", log);
        }

        static Transform Find(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(GameFeelAssetBuilder.AudioDir + "/" + name + ".wav");

        // ================================================================ 1. Entrada

        static string OpenEntrance(Transform dungeon)
        {
            var wall = Find(dungeon, "Wall_B_0");
            if (wall == null) return "entrada: no hay Wall_B_0";
            int removed = 0, added = 0;
            foreach (var box in wall.GetComponents<BoxCollider>()) { Object.DestroyImmediate(box); removed++; }
            foreach (var mf in wall.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                added++;
            }
            return "entrada: cajas quitadas " + removed + ", MeshColliders del arco " + added;
        }

        // ================================================================ 2. Audio

        static string DungeonAudio(Transform dungeon)
        {
            var activate = Clip("feel_activate");
            int fixedSources = 0;

            // Pilares: sonido de activación propio (antes uno tenía una explosión y los otros nada).
            foreach (var p in dungeon.GetComponentsInChildren<PillarProjector>(true))
            {
                if (p.ActivationAudio == null) p.ActivationAudio = p.gameObject.AddComponent<AudioSource>();
                p.ActivationAudio.clip = activate;
                p.ActivationAudio.volume = 0.8f;
                p.ActivationAudio.playOnAwake = false;
                EditorUtility.SetDirty(p);
            }
            // Puerta principal: ya suenan el derrumbe de piedra y la fanfarria; la explosión solo tapaba.
            var doorFx = Find(dungeon, "Efectos_MainDoor-Seccion1");
            if (doorFx != null && doorFx.TryGetComponent<AudioSource>(out var doorAudio)) doorAudio.clip = null;

            // Todas las fuentes en 3D, con atenuación de sala (antes 2D y alcance de 500 m).
            foreach (var a in dungeon.GetComponentsInChildren<AudioSource>(true))
            {
                SetSpatial(a, 1f, 25f, 64);
                fixedSources++;
            }

            // Tono de sala: se oye al entrar y se apaga al salir hacia el tutorial.
            var floor = dungeon.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Tile_A")).ToArray();
            var roomCenter = floor.Length > 0 ? floor.Aggregate(new Bounds(floor[0].bounds.center, Vector3.zero), (b, r) => { b.Encapsulate(r.bounds); return b; }).center : dungeon.position;
            var ambience = Find(dungeon, "Ambiente_Mazmorra") ?? new GameObject("Ambiente_Mazmorra").transform;
            ambience.SetParent(dungeon, true);
            ambience.position = new Vector3(roomCenter.x, 2.5f, roomCenter.z);
            var amb = GetOrAdd<AudioSource>(ambience.gameObject);
            amb.clip = Clip("feel_room_tone");
            amb.loop = true;
            amb.playOnAwake = true;
            amb.volume = 0.8f;
            SetSpatial(amb, 1f, 18f, 200);
            amb.rolloffMode = AudioRolloffMode.Linear;
            amb.minDistance = 8f;

            // Crepitar en cada antorcha encendida de la mazmorra (solo se oye de cerca).
            var crackle = Clip("feel_torch_crackle");
            int torches = 0;
            foreach (var ps in dungeon.GetComponentsInChildren<ParticleSystem>(true).Where(p => p.name == "Fuego"))
            {
                var holder = ps.transform.Find("Crepitar") ?? new GameObject("Crepitar").transform;
                holder.SetParent(ps.transform, false);
                var a = GetOrAdd<AudioSource>(holder.gameObject);
                a.clip = crackle;
                a.loop = true;
                a.playOnAwake = false;
                a.volume = 0.55f;
                SetSpatial(a, 1f, 7f, 180);
                a.minDistance = 0.6f;
                if (holder.GetComponent<FeelLoopRandomStart>() == null) holder.gameObject.AddComponent<FeelLoopRandomStart>();
                torches++;
            }
            return "audio: " + fixedSources + " fuentes en 3D, pilares con sonido de activación, tono de sala, crepitar en " + torches + " antorchas";
        }

        static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();

        static void SetSpatial(AudioSource a, float blend, float maxDistance, int priority)
        {
            a.spatialBlend = blend;
            a.rolloffMode = AudioRolloffMode.Logarithmic;
            a.minDistance = 1f;
            a.maxDistance = maxDistance;
            a.dopplerLevel = 0f;
            a.spread = 0f;
            a.priority = priority;
        }

        // ================================================================ 3. Altar

        static readonly Dictionary<RunicSymbol, Color> ElementColor = new Dictionary<RunicSymbol, Color>
        {
            { RunicSymbol.Fuego, new Color(1f, 0.28f, 0.1f) },
            { RunicSymbol.Agua, new Color(0.2f, 0.88f, 1f) },
            { RunicSymbol.Tierra, new Color(1f, 0.82f, 0.22f) },
            { RunicSymbol.Viento, new Color(1f, 0.55f, 0.15f) },
        };

        static string BuildAltar(Transform dungeon)
        {
            var altar = Find(dungeon, "Altar");
            if (altar == null) return "altar: no encontrado";

            // El altar viejo (cubos) se apaga pero se conservan sus huecos (PlacePoint) y su collider de cuerpo.
            if (altar.TryGetComponent<MeshRenderer>(out var oldBody)) oldBody.enabled = false;
            var slots = new List<Transform>();
            foreach (Transform child in altar)
            {
                if (child.TryGetComponent<MeshRenderer>(out var r)) r.enabled = false;
                if (child.TryGetComponent<BoxCollider>(out var c)) c.enabled = false; // sobresalían 13 cm de la losa
                if (child.GetComponentInChildren<PlacePoint>(true) != null) slots.Add(child);
            }

            var stone = StoneMaterial(dungeon);
            var darkStone = Mat("Altar_PiedraOscura", stone, new Color(0.38f, 0.34f, 0.32f));
            var bronze = LitMaterial("Altar_Bronce", new Color(0.55f, 0.38f, 0.16f), 0.85f, 0.55f);
            var glow = UnlitMaterial("Altar_Brillo", Color.white);
            var gem = UnlitMaterial("Altar_Gema", new Color(1f, 0.72f, 0.35f));
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var cyl = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            var ring = RingMesh();

            var parent = altar.parent;
            var old = parent.Find("Altar_Runas");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Altar_Runas").transform;
            root.SetParent(parent, false);
            float floorY = altar.position.y - altar.lossyScale.y * 0.5f;
            root.SetPositionAndRotation(new Vector3(altar.position.x, floorY, altar.position.z), Quaternion.Euler(0f, altar.eulerAngles.y, 0f));

            // Pedestal escalonado (de abajo arriba). Losa superior a 1,02 m: los huecos quedan donde estaban.
            Part("Zocalo", root, cube, stone, new Vector3(0f, 0.07f, 0f), new Vector3(2.3f, 0.14f, 2.3f), true);
            Part("Escalon", root, cube, stone, new Vector3(0f, 0.2f, 0f), new Vector3(2.06f, 0.12f, 2.06f), true);
            Part("Cuerpo", root, cube, darkStone, new Vector3(0f, 0.59f, 0f), new Vector3(1.8f, 0.66f, 1.8f), true);
            Part("Moldura", root, cube, bronze, new Vector3(0f, 0.85f, 0f), new Vector3(1.84f, 0.05f, 1.84f), true);
            Part("Losa", root, cube, stone, new Vector3(0f, 0.97f, 0f), new Vector3(2.08f, 0.1f, 2.08f), true);

            // Columnitas en las esquinas con remate de bronce y gema.
            foreach (var sx in new[] { -1f, 1f })
            foreach (var sz in new[] { -1f, 1f })
            {
                var c = new Vector3(sx * 0.98f, 0f, sz * 0.98f);
                Part("Columna", root, cube, stone, c + new Vector3(0f, 0.69f, 0f), new Vector3(0.2f, 0.98f, 0.2f), true);
                Part("Remate", root, cube, bronze, c + new Vector3(0f, 1.2f, 0f), new Vector3(0.26f, 0.05f, 0.26f), true);
                Part("Gema", root, Resources.GetBuiltinResource<Mesh>("Sphere.fbx"), gem, c + new Vector3(0f, 1.27f, 0f), Vector3.one * 0.075f, false);
            }
            // Franja de runas en cada cara del cuerpo.
            for (int i = 0; i < 4; i++)
            {
                var q = Quaternion.Euler(0f, i * 90f, 0f);
                var strip = Part("Franja", root, cube, gem, q * new Vector3(0f, 0f, 0.905f) + new Vector3(0f, 0.62f, 0f), new Vector3(1.2f, 0.035f, 0.012f), false);
                strip.localRotation = q;
            }

            // Huecos: plato de piedra oscura y anillo del color de su elemento.
            var glowComp = root.gameObject.AddComponent<RuneAltarGlow>();
            var sockets = new List<RuneAltarGlow.Socket>();
            var pillars = dungeon.GetComponentsInChildren<PillarProjector>(true);
            var center = new List<Renderer>();
            foreach (var slot in slots)
            {
                var pp = slot.GetComponentInChildren<PlacePoint>(true);
                var local = root.InverseTransformPoint(pp.transform.position);
                local.y = 0f;
                var pillar = pillars.FirstOrDefault(p => p.AltarSocket == pp);
                var symbol = pillar != null ? pillar.RequiredSymbolInSocket : RunicSymbol.Ninguno;
                var color = ElementColor.TryGetValue(symbol, out var col) ? col : Color.white;
                Part("Hueco_" + symbol, root, cyl, darkStone, local + new Vector3(0f, 1.03f, 0f), new Vector3(0.42f, 0.012f, 0.42f), true);
                var rr = Part("Anillo_" + symbol, root, ring, glow, local + new Vector3(0f, 1.047f, 0f), new Vector3(0.2f, 1f, 0.2f), false);
                sockets.Add(new RuneAltarGlow.Socket { placePoint = pp, ring = rr.GetComponent<Renderer>(), color = color, required = symbol });
                // Línea del centro al hueco.
                var dir = new Vector3(local.x, 0f, local.z);
                float len = dir.magnitude - 0.24f - 0.22f;
                var line = Part("Linea_" + symbol, root, cube, glow, dir.normalized * (0.24f + len * 0.5f) + new Vector3(0f, 1.024f, 0f), new Vector3(0.02f, 0.004f, len), false);
                line.localRotation = Quaternion.LookRotation(dir.normalized);
                center.Add(line.GetComponent<Renderer>());
            }
            center.Add(Part("Circulo", root, ring, glow, new Vector3(0f, 1.025f, 0f), new Vector3(0.24f, 1f, 0.24f), false).GetComponent<Renderer>());
            glowComp.sockets = sockets.ToArray();
            glowComp.centerParts = center.ToArray();
            return "altar: nuevo pedestal con " + sockets.Count + " huecos (" + string.Join(", ", sockets.Select(s => s.required)) + ")";
        }

        static Transform Part(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3 scale, bool isStatic)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (isStatic)
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
            return go.transform;
        }

        static Material StoneMaterial(Transform dungeon)
        {
            var src = dungeon.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name.StartsWith("Wall_A"))?.sharedMaterial;
            return Mat("Altar_Piedra", src, Color.white, new Vector2(1.5f, 1.5f));
        }

        static Material Mat(string name, Material src, Color tint, Vector2? tiling = null)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            else if (src != null) m.CopyPropertiesFromMaterial(src);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", (src != null && src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : Color.white) * tint);
            if (tiling.HasValue) m.mainTextureScale = tiling.Value;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material LitMaterial(string name, Color color, float metallic, float smoothness)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material UnlitMaterial(string name, Color color)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Anillo plano de radio exterior 1 e interior 0,8 en el plano XZ (cara hacia arriba).</summary>
        static Mesh RingMesh()
        {
            string path = MeshDir + "/Anillo_Altar.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            Directory.CreateDirectory(MeshDir);
            const int seg = 48;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(d * 0.8f); v.Add(d);
                n.Add(Vector3.up); n.Add(Vector3.up);
                uv.Add(new Vector2(i / (float)seg, 0f)); uv.Add(new Vector2(i / (float)seg, 1f));
                if (i < seg)
                {
                    int b = i * 2;
                    tri.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
                }
            }
            mesh = new Mesh { name = "Anillo_Altar" };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ================================================================ 4. Portal final y sala de "gracias por jugar"

        static string BuildEndPortal(Scene scene, Transform dungeon)
        {
            var door = Find(dungeon, "Main Door To Section 2");
            var manager = dungeon.GetComponentInChildren<PuzzleRoomManager>(true);
            if (door == null || manager == null) return "portal: falta la puerta principal o el PuzzleRoomManager";
            var doorBounds = door.GetComponent<Renderer>().bounds;

            var feelMats = MatDir;
            var portalGlow = AssetDatabase.LoadAssetAtPath<Material>(feelMats + "/Feel_PortalGlow.mat");
            var magicLoop = AssetDatabase.LoadAssetAtPath<GameObject>(GameFeelAssetBuilder.PrefabDir + "/Feel_MagicLoop.prefab");
            var magic = AssetDatabase.LoadAssetAtPath<ParticleSystem>(GameFeelAssetBuilder.PrefabDir + "/Feel_Magic.prefab");
            var confetti = AssetDatabase.LoadAssetAtPath<ParticleSystem>(GameFeelAssetBuilder.PrefabDir + "/Feel_Confetti.prefab");
            var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<AutoHandPlayer>(true)).FirstOrDefault();
            var fader = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SceneFader>(true)).FirstOrDefault();

            // ----- Portal en el marco de la puerta, mirando hacia la sala.
            var oldPortal = Find(dungeon, "Portal_Final");
            if (oldPortal != null) Object.DestroyImmediate(oldPortal.gameObject);
            var portal = new GameObject("Portal_Final").transform;
            portal.SetParent(dungeon, true);
            // La sala queda del lado +X de la puerta (x = -25): el portal mira hacia allá.
            portal.SetPositionAndRotation(new Vector3(doorBounds.center.x, doorBounds.center.y, doorBounds.center.z), Quaternion.Euler(0f, 270f, 0f));
            float w = doorBounds.size.z, h = doorBounds.size.y;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(portal, false);
            var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            // Tonos fríos (azul-violeta): se distinguen del fuego cálido de la sala y no queman a blanco.
            var outerMat = Mat("Portal_Final_Brillo", portalGlow, new Color(0.25f, 0.4f, 0.95f, 0.85f));
            var innerMat = Mat("Portal_Final_Nucleo", portalGlow, new Color(0.55f, 0.45f, 1f, 0.7f));
            Part("Brillo", visual, quad, outerMat, new Vector3(0f, -0.04f, 0.02f), new Vector3(w * 0.95f, h * 0.97f, 1f), false);
            Part("Nucleo", visual, quad, innerMat, new Vector3(0f, -0.15f, 0.05f), new Vector3(w * 0.6f, h * 0.7f, 1f), false);
            if (magicLoop != null)
            {
                var fx = (GameObject)PrefabUtility.InstantiatePrefab(magicLoop, visual);
                fx.transform.localPosition = new Vector3(0f, -h * 0.3f, -0.1f);
                fx.transform.localScale = Vector3.one * 1.6f;
            }

            var trigger = portal.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(w, h, 1.2f);
            // Tope detrás del portal: detrás de la puerta ya no hay sala (la Sección 2 no está en la combinada).
            var stop = new GameObject("Tope").transform;
            stop.SetParent(portal, false);
            stop.localPosition = new Vector3(0f, 0f, 0.9f);
            stop.gameObject.AddComponent<BoxCollider>().size = new Vector3(w + 0.4f, h + 0.4f, 0.2f);

            var hum = portal.gameObject.AddComponent<AudioSource>();
            hum.clip = Clip("feel_portal_hum");
            hum.loop = true;
            hum.playOnAwake = false;
            hum.volume = 0.75f;
            SetSpatial(hum, 1f, 14f, 100);

            var end = portal.gameObject.AddComponent<EndPortal>();
            end.portalVisual = visual.gameObject;
            end.hum = hum;
            end.player = player;
            end.fader = fader;
            end.appearFeedback = Burst(portal, "Feel_PortalAparece", Clip("feel_activate"), 0.9f, magic, 0.35f);
            end.enterFeedback = Burst(portal, "Feel_PortalCruce", Clip("feel_whoosh"), 0.9f, null, 0.6f);

            // ----- Sala final: lejos y oscura, solo el mensaje y un círculo de luz bajo los pies.
            var oldRoom = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Sala_Final");
            if (oldRoom != null) Object.DestroyImmediate(oldRoom);
            var room = new GameObject("Sala_Final").transform;
            SceneManager.MoveGameObjectToScene(room.gameObject, scene);
            room.position = EndRoomPosition;
            var floorGo = new GameObject("Suelo_Invisible");
            floorGo.transform.SetParent(room, false);
            var floorBox = floorGo.AddComponent<BoxCollider>();
            floorBox.center = new Vector3(0f, -0.5f, 0f);
            floorBox.size = new Vector3(14f, 1f, 14f);
            var endPoint = new GameObject("Llegada").transform;
            endPoint.SetParent(room, false);

            var content = new GameObject("Contenido").transform;
            content.SetParent(room, false);
            var ringMat = UnlitMaterial("Fin_Anillo", new Color(1f, 0.75f, 0.4f));
            Part("Circulo_Suelo", content, RingMesh(), ringMat, new Vector3(0f, 0.01f, 0f), new Vector3(1.1f, 1f, 1.1f), false);
            Part("Circulo_Exterior", content, RingMesh(), ringMat, new Vector3(0f, 0.01f, 0f), new Vector3(2.4f, 1f, 2.4f), false);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            var titleMat = TextMaterial(font, "Fin_TextoTitulo", new Color(1f, 0.9f, 0.62f), new Color(0.75f, 0.42f, 0.1f), 0.2f);
            var subMat = TextMaterial(font, "Fin_TextoSub", new Color(0.4f, 0.85f, 1f), new Color(0.05f, 0.2f, 0.35f), 0.15f);
            var smallMat = TextMaterial(font, "Fin_TextoPequeno", new Color(0.85f, 0.85f, 0.85f), Color.black, 0f);
            var title = MakeText(content, "Titulo", "¡Gracias por jugar!", font, titleMat, new Vector3(0f, 1.78f, 3.2f), new Vector2(4.4f, 0.8f), 7f);
            MakeText(content, "Subtitulo", "LSV-INMERSIVE 360", font, subMat, new Vector3(0f, 1.2f, 3.2f), new Vector2(3.2f, 0.4f), 3.4f);
            var countdown = MakeText(content, "Cuenta", "El juego se reiniciará en 8", font, smallMat, new Vector3(0f, 0.78f, 3.2f), new Vector2(3.6f, 0.25f), 1.8f);
            if (magicLoop != null)
            {
                var fx = (GameObject)PrefabUtility.InstantiatePrefab(magicLoop, content);
                fx.transform.localPosition = new Vector3(0f, 1.4f, 3.4f);
                fx.transform.localScale = Vector3.one * 2f;
            }

            end.endPoint = endPoint;
            end.thanksRoot = content.gameObject;
            end.thanksTitle = title.transform;
            end.countdownText = countdown;
            end.arriveFeedback = Burst(room, "Feel_GraciasPorJugar", Clip("feel_success"), 0.9f, confetti, 0.5f);
            end.arriveFeedback.successPattern = true;

            // Se conecta al final del puzzle (se quita antes por si la escena se reconstruye).
            for (int i = manager.OnMainDoorOpened.GetPersistentEventCount() - 1; i >= 0; i--)
                if (manager.OnMainDoorOpened.GetPersistentMethodName(i) == nameof(EndPortal.Open))
                    UnityEventTools.RemovePersistentListener(manager.OnMainDoorOpened, i);
            UnityEventTools.AddVoidPersistentListener(manager.OnMainDoorOpened, end.Open);
            EditorUtility.SetDirty(manager);

            return "portal final en la puerta principal (" + w.ToString("0.0") + " × " + h.ToString("0.0") + " m) y sala de agradecimiento en " + EndRoomPosition
                   + (player == null ? " (sin AutoHandPlayer)" : "") + (fader == null ? " (sin SceneFader)" : "");
        }

        static FeelBurst Burst(Transform parent, string name, AudioClip clip, float volume, ParticleSystem particles, float haptic)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<FeelBurst>();
            b.clip = clip;
            b.volume = volume;
            b.spatialBlend = 1f;
            b.particlesPrefab = particles;
            b.hapticAmplitude = haptic;
            return b;
        }

        static Material TextMaterial(TMP_FontAsset font, string name, Color face, Color outline, float outlineWidth)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(font.material); AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(font.material);
            m.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            m.SetTexture("_MainTex", font.atlasTexture);
            m.SetColor("_FaceColor", face);
            m.SetColor("_OutlineColor", outline);
            m.SetFloat("_OutlineWidth", outlineWidth);
            if (outlineWidth > 0f) m.EnableKeyword("OUTLINE_ON"); else m.DisableKeyword("OUTLINE_ON");
            EditorUtility.SetDirty(m);
            return m;
        }

        static TextMeshPro MakeText(Transform parent, string name, string text, TMP_FontAsset font, Material mat, Vector3 localPos, Vector2 size, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var t = go.AddComponent<TextMeshPro>();
            t.font = font;
            t.fontSharedMaterial = mat;
            t.text = text;
            t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true;
            t.fontSizeMin = 0.5f;
            t.fontSizeMax = fontSize;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = size;
            t.color = Color.white;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }
    }
}
