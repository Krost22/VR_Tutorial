using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Reemplaza el póster "foto lsv" de la pared de inicio de 2_Adaptacion por un pequeño escenario 3D:
    /// arco de piedra entre dos columnas con antorchas, placa de bronce con el título "LSV-INMERSIVE 360"
    /// en relieve (capas de texto apiladas), esmeralda, 5 runas, cinta "Una aventura mágica" y utilería
    /// (barril, cajas, libros, poción). El póster queda desactivado, no borrado.
    /// Menú: Tools/Game Feel/Construir portal del título. Se puede volver a ejecutar: reemplaza "Portal_Titulo".
    /// </summary>
    public static class TitlePortalBuilder
    {
        const string RootName = "Portal_Titulo";
        const string Dir = "Assets/_Custom/Models/PortalTitulo";
        const string FontTtf = "Assets/_Custom/Fonts/Cinzel/CinzelDecorative-Bold.ttf";
        const string FontAssetPath = "Assets/_Custom/Fonts/Cinzel/CinzelDecorative-Bold SDF.asset";
        const string Kit = "Assets/DungeonModularPack/Prefabs/";

        // Cara de la pared del fondo (z = -1,26) y eje del jugador (x = 0,3).
        static readonly Vector3 Anchor = new Vector3(0.3f, 0f, -1.26f);

        [MenuItem("Tools/Game Feel/Construir portal del título")]
        public static void BuildMenu() => Debug.Log("[GameFeel] " + Build());

        public static string Build()
        {
            var scene = SceneManager.GetActiveScene();
            Directory.CreateDirectory(Dir);
            var log = new List<string>();

            // Materiales
            // Bronce oscuro y poco saturado: las antorchas cercanas ya le dan el tono cálido.
            var bronze = Lit("Portal_Bronce", new Color(0.3f, 0.22f, 0.12f), Color.black, 0.55f);
            var panel = Lit("Portal_Placa", new Color(0.15f, 0.24f, 0.17f), new Color(0.015f, 0.04f, 0.02f), 0.35f);
            // Pergamino con algo de brillo propio para que no se vuelva naranja bajo la luz de las antorchas.
            var parchment = Lit("Portal_Pergamino", new Color(0.42f, 0.38f, 0.3f), new Color(0.3f, 0.25f, 0.17f), 0.1f);
            var emerald = Lit("Portal_Esmeralda", new Color(0.05f, 0.45f, 0.18f), new Color(0.15f, 1f, 0.4f) * 1.1f, 0.9f);
            var runeRed = Lit("Portal_RunaRoja", new Color(0.4f, 0.04f, 0.03f), new Color(1f, 0.25f, 0.12f) * 0.9f, 0.8f);
            var runeGreen = Lit("Portal_RunaVerde", new Color(0.04f, 0.35f, 0.12f), new Color(0.2f, 1f, 0.35f) * 0.8f, 0.8f);
            var runeGold = Lit("Portal_RunaDorada", new Color(0.5f, 0.32f, 0.05f), new Color(1f, 0.7f, 0.2f) * 0.8f, 0.8f);
            var leatherA = Lit("Portal_LibroRojo", new Color(0.35f, 0.07f, 0.05f), Color.black, 0.25f);
            var leatherB = Lit("Portal_LibroVerde", new Color(0.08f, 0.22f, 0.1f), Color.black, 0.25f);
            var leatherC = Lit("Portal_LibroCafe", new Color(0.3f, 0.18f, 0.08f), Color.black, 0.25f);
            var pages = Lit("Portal_Paginas", new Color(0.85f, 0.78f, 0.6f), Color.black, 0.05f);
            var potion = Lit("Portal_Pocion", new Color(0.05f, 0.4f, 0.15f), new Color(0.2f, 0.9f, 0.4f) * 0.7f, 0.95f);
            var glass = Lit("Portal_Vidrio", new Color(0.55f, 0.7f, 0.65f), Color.black, 0.95f);
            var cork = Lit("Portal_Corcho", new Color(0.45f, 0.3f, 0.15f), Color.black, 0.1f);

            // Meshes procedurales (guardados como assets)
            var hexPanel = SaveMesh(Prism("Portal_HexPlaca", HexOutline(1.3f, 0.62f, 0.2f), 0.04f));
            var hexRim = SaveMesh(Prism("Portal_HexBorde", HexOutline(1.39f, 0.7f, 0.22f), 0.04f));
            var gem = SaveMesh(Octahedron("Portal_Gema", 0.13f, 0.22f, 0.07f));
            var runeMesh = SaveMesh(Octahedron("Portal_Rombo", 0.085f, 0.085f, 0.03f));

            // Raíz: girada 180° para que lo construido "mirando a -Z" quede mirando a la sala (+Z).
            foreach (var r in scene.GetRootGameObjects().Where(r => r.name == RootName).ToArray()) Object.DestroyImmediate(r);
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(Anchor, Quaternion.Euler(0f, 180f, 0f));
            var arch = new GameObject("Arquitectura").transform; arch.SetParent(root.transform, false);
            var title = new GameObject("Titulo").transform; title.SetParent(root.transform, false);
            var props = new GameObject("Utileria").transform; props.SetParent(root.transform, false);

            // Arquitectura del kit de mazmorra (estática y horneada)
            PlaceKit(Kit + "Arch_A.prefab", arch, new Vector3(0f, 0f, -0.2f), Vector3.zero, Vector3.one, true);
            foreach (float x in new[] { -2.0f, 2.0f })
            {
                PlaceKit(Kit + "Pillar_A.prefab", arch, new Vector3(x, 0f, -0.21f), Vector3.zero, Vector3.one, true);
                var torch = PlaceKit(Kit + "Torch_A.prefab", arch, new Vector3(x, 1.95f, -0.62f), new Vector3(0f, 180f, 0f), Vector3.one, true);
                AddTorchLightAndFire(torch);
            }

            // Placa: borde de bronce y panel oscuro con forma de escudo alargado.
            Part("Placa_Borde", title, hexRim, bronze, new Vector3(0f, 2.05f, -0.47f));
            Part("Placa", title, hexPanel, panel, new Vector3(0f, 2.05f, -0.5f));
            Part("Esmeralda", title, gem, emerald, new Vector3(0f, 2.88f, -0.56f));
            // Brillo verde de la esmeralda sobre el arco (horneado: no cuesta en el juego).
            var glowGo = new GameObject("Luz_Esmeralda");
            glowGo.transform.SetParent(title, false);
            glowGo.transform.localPosition = new Vector3(0f, 2.9f, -0.75f);
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point; glow.color = new Color(0.3f, 1f, 0.45f); glow.intensity = 1.2f; glow.range = 2.2f;
            glow.shadows = LightShadows.None; glow.lightmapBakeType = LightmapBakeType.Baked;
            glowGo.isStatic = true;
            Part("Esmeralda_Engaste", title, SaveMesh(Prism("Portal_Engaste", HexOutline(0.2f, 0.2f, 0.08f), 0.03f)), bronze, new Vector3(0f, 2.86f, -0.5f));
            var runeMats = new[] { runeRed, runeGreen, runeGold, runeGreen, runeRed };
            for (int i = 0; i < 5; i++)
                Part("Runa_" + (i + 1), title, runeMesh, runeMats[i], new Vector3(-0.5f + i * 0.25f, 1.6f, -0.55f));

            // Título en relieve: capas de texto apiladas (frente verde con borde dorado, cuerpo dorado oscuro).
            var font = GetOrCreateFont();
            var frontMat = TextMaterial(font, "Portal_TextoFrente", new Color(0.32f, 0.8f, 0.4f), new Color(1f, 0.78f, 0.32f), 0.22f);
            var bodyMat = TextMaterial(font, "Portal_TextoCuerpo", new Color(0.75f, 0.52f, 0.18f), new Color(0.75f, 0.52f, 0.18f), 0.22f);
            BuildRelief(title, "LSV-INMERSIVE", font, frontMat, bodyMat, new Vector3(0f, 2.28f, 0f), new Vector2(2.25f, 0.42f), 4.2f);
            BuildRelief(title, "360", font, frontMat, bodyMat, new Vector3(0f, 1.9f, 0f), new Vector2(1.2f, 0.45f), 4.6f);

            // Cinta de pergamino con el subtítulo (tres tramos, los extremos doblados hacia atrás).
            var ribbon = new GameObject("Cinta").transform; ribbon.SetParent(title, false);
            ribbon.localPosition = new Vector3(0f, 1.27f, -0.62f);
            Box("Cinta_Centro", ribbon, parchment, Vector3.zero, Vector3.zero, new Vector3(1.6f, 0.2f, 0.02f));
            Box("Cinta_Izq", ribbon, parchment, new Vector3(-0.945f, -0.02f, 0.068f), new Vector3(0f, 25f, 0f), new Vector3(0.32f, 0.17f, 0.02f));
            Box("Cinta_Der", ribbon, parchment, new Vector3(0.945f, -0.02f, 0.068f), new Vector3(0f, -25f, 0f), new Vector3(0.32f, 0.17f, 0.02f));
            var sub = MakeText(ribbon, "Subtitulo", "UNA AVENTURA MÁGICA", font, TextMaterial(font, "Portal_TextoCinta", new Color(0.3f, 0.15f, 0.05f), new Color(0.3f, 0.15f, 0.05f), 0f), new Vector3(0f, 0f, -0.012f), new Vector2(1.5f, 0.17f), 1.4f);

            // Utilería
            var crateAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Custom/Models/Wood Cratesboxes/barrelcrate.fbx");
            Mesh FindMesh(string n) => crateAsset.GetComponentsInChildren<MeshFilter>().First(m => m.name == n).sharedMesh;
            var crateMat = crateAsset.GetComponentInChildren<Renderer>().sharedMaterial;
            Part("Barril", props, FindMesh("barrel"), crateMat, new Vector3(-1.45f, 0.34f, -0.85f), new Vector3(0f, 30f, 0f), Vector3.one * 0.35f);
            Part("Caja_Izq", props, FindMesh("crate"), crateMat, new Vector3(-1.55f, 0.2f, -1.35f), new Vector3(0f, 12f, 0f), Vector3.one * 0.27f);
            Part("Caja_Der", props, FindMesh("crate"), crateMat, new Vector3(1.5f, 0.3f, -0.85f), new Vector3(0f, -8f, 0f), Vector3.one * 0.4f);
            // Libros sobre la caja derecha y una poción.
            var top = new Vector3(1.5f, 0.6f, -0.85f);
            Book(props, top + new Vector3(-0.05f, 0.03f, 0f), new Vector3(0.24f, 0.05f, 0.17f), 8f, leatherA, pages);
            Book(props, top + new Vector3(-0.04f, 0.08f, 0.01f), new Vector3(0.21f, 0.045f, 0.15f), -14f, leatherB, pages);
            Book(props, top + new Vector3(-0.06f, 0.125f, 0f), new Vector3(0.18f, 0.04f, 0.13f), 21f, leatherC, pages);
            Potion(props, top + new Vector3(0.17f, 0f, -0.04f), glass, potion, cork);
            Potion(props, new Vector3(-1.15f, 0f, -0.75f), glass, runeRed, cork, 0.8f);

            // El póster viejo se apaga (no se borra).
            var poster = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "foto lsv");
            if (poster != null) { poster.SetActive(false); log.Add("póster 'foto lsv' desactivado"); }

            EditorSceneManager.MarkSceneDirty(scene);
            log.Add("portal construido: arco, 2 columnas con antorchas, placa con título en relieve, esmeralda, 5 runas, cinta y utilería");
            return string.Join("\n", log);
        }

        // ------------------------------------------------------------------ Título en relieve

        static TMP_FontAsset GetOrCreateFont()
        {
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fa != null) return fa;
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(FontTtf);
            fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            fa.name = "CinzelDecorative-Bold SDF";
            fa.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚÑabcdefghijklmnopqrstuvwxyzáéíóúñ0123456789-.,!¡¿? ");
            AssetDatabase.CreateAsset(fa, FontAssetPath);
            fa.atlasTextures[0].name = fa.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.atlasPopulationMode = AtlasPopulationMode.Static; // atlas fijo: no cambia en el juego
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        static Material TextMaterial(TMP_FontAsset font, string name, Color face, Color outline, float outlineWidth)
        {
            string path = Dir + "/" + name + ".mat";
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
            t.enableWordWrapping = false;
            t.rectTransform.sizeDelta = size;
            t.color = Color.white;
            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }

        static void BuildRelief(Transform parent, string text, TMP_FontAsset font, Material front, Material body, Vector3 center, Vector2 size, float fontSize)
        {
            // 7 capas del cuerpo, de atrás hacia adelante, cada una un poco más clara: da volumen al mirarlo de lado.
            var group = new GameObject("Relieve_" + text).transform;
            group.SetParent(parent, false);
            group.localPosition = center;
            const int layers = 7;
            for (int i = 0; i < layers; i++)
            {
                float z = -0.535f - i * 0.006f;
                var t = MakeText(group, "Capa_" + i, text, font, body, new Vector3(0f, 0f, z), size, fontSize);
                float shade = Mathf.Lerp(0.45f, 0.95f, i / (float)(layers - 1));
                t.color = new Color(shade, shade, shade, 1f);
            }
            var face = MakeText(group, "Frente", text, font, front, new Vector3(0f, 0f, -0.535f - layers * 0.006f), size, fontSize);
            face.enableVertexGradient = true;
            face.colorGradient = new VertexGradient(new Color(0.85f, 1f, 0.8f), new Color(0.85f, 1f, 0.8f), new Color(0.55f, 0.75f, 0.55f), new Color(0.55f, 0.75f, 0.55f));
        }

        // ------------------------------------------------------------------ Piezas

        static GameObject PlaceKit(string prefabPath, Transform parent, Vector3 localPos, Vector3 localEuler, Vector3 scale, bool isStatic)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = scale;
            if (isStatic) foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
            return go;
        }

        public static void AddTorchLightAndFire(GameObject torch)
        {
            var fire = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Custom/GameFeel/Prefabs/Feel_TorchFire.prefab");
            var rs = torch.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var cup = new Vector3(b.center.x, b.max.y - 0.11f, b.center.z);
            if (fire != null)
            {
                var f = (GameObject)PrefabUtility.InstantiatePrefab(fire, torch.transform);
                f.name = "Fuego";
                f.transform.position = cup;
                f.isStatic = false;
            }
            var lightGo = new GameObject("Point Light");
            lightGo.transform.SetParent(torch.transform, false);
            lightGo.transform.position = cup + Vector3.up * 0.1f;
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.7f, 0f);
            l.intensity = 2f;
            l.range = 7f;
            l.shadows = LightShadows.Soft;
            l.lightmapBakeType = LightmapBakeType.Baked; // se convierte a luz de Bakery al hornear la combinada
            lightGo.isStatic = true;
        }

        static GameObject Part(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3? euler = null, Vector3? scale = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = scale ?? Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static GameObject Box(string name, Transform parent, Material mat, Vector3 localPos, Vector3 euler, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static void Book(Transform parent, Vector3 pos, Vector3 size, float yaw, Material cover, Material pages)
        {
            var b = new GameObject("Libro").transform;
            b.SetParent(parent, false);
            b.localPosition = pos;
            b.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box("Tapa", b, cover, Vector3.zero, Vector3.zero, size);
            Box("Paginas", b, pages, new Vector3(0.008f, 0f, 0f), Vector3.zero, new Vector3(size.x - 0.012f, size.y * 0.8f, size.z + 0.002f) - new Vector3(0f, 0f, 0.012f));
        }

        static void Potion(Transform parent, Vector3 basePos, Material glass, Material liquid, Material cork, float scale = 1f)
        {
            var p = new GameObject("Pocion").transform;
            p.SetParent(parent, false);
            p.localPosition = basePos;
            p.localScale = Vector3.one * scale;
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Frasco"; Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(p, false); body.transform.localPosition = new Vector3(0f, 0.07f, 0f); body.transform.localScale = Vector3.one * 0.14f;
            body.GetComponent<MeshRenderer>().sharedMaterial = liquid;
            var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Cuello"; Object.DestroyImmediate(neck.GetComponent<Collider>());
            neck.transform.SetParent(p, false); neck.transform.localPosition = new Vector3(0f, 0.16f, 0f); neck.transform.localScale = new Vector3(0.045f, 0.035f, 0.045f);
            neck.GetComponent<MeshRenderer>().sharedMaterial = glass;
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            c.name = "Corcho"; Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.SetParent(p, false); c.transform.localPosition = new Vector3(0f, 0.205f, 0f); c.transform.localScale = new Vector3(0.05f, 0.015f, 0.05f);
            c.GetComponent<MeshRenderer>().sharedMaterial = cork;
        }

        // ------------------------------------------------------------------ Meshes procedurales

        static Vector2[] HexOutline(float halfWidth, float halfHeight, float point)
        {
            // Escudo alargado con puntas a los lados (sentido horario visto desde -Z).
            return new[]
            {
                new Vector2(-halfWidth, 0f), new Vector2(-halfWidth + point, halfHeight), new Vector2(halfWidth - point, halfHeight),
                new Vector2(halfWidth, 0f), new Vector2(halfWidth - point, -halfHeight), new Vector2(-halfWidth + point, -halfHeight),
            };
        }

        static Mesh Prism(string name, Vector2[] outline, float depth)
        {
            // Polígono convexo extruido: cara frontal en z=-depth/2 (mira a -Z), trasera en +depth/2 y laterales.
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float zf = -depth * 0.5f, zb = depth * 0.5f;
            int count = outline.Length;
            Vector2 min = outline[0], max = outline[0];
            foreach (var p in outline) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            Vector2 UV(Vector2 p) => new Vector2((p.x - min.x) / (max.x - min.x), (p.y - min.y) / (max.y - min.y));
            // frente
            int f0 = v.Count;
            foreach (var p in outline) { v.Add(new Vector3(p.x, p.y, zf)); n.Add(Vector3.back); uv.Add(UV(p)); }
            for (int i = 1; i < count - 1; i++) { tri.Add(f0); tri.Add(f0 + i); tri.Add(f0 + i + 1); }
            // fondo
            int b0 = v.Count;
            foreach (var p in outline) { v.Add(new Vector3(p.x, p.y, zb)); n.Add(Vector3.forward); uv.Add(UV(p)); }
            for (int i = 1; i < count - 1; i++) { tri.Add(b0); tri.Add(b0 + i + 1); tri.Add(b0 + i); }
            // laterales
            for (int i = 0; i < count; i++)
            {
                var a = outline[i]; var c = outline[(i + 1) % count];
                var edge = (c - a).normalized; var normal = new Vector3(-edge.y, edge.x, 0f);
                int s = v.Count;
                v.Add(new Vector3(a.x, a.y, zf)); v.Add(new Vector3(c.x, c.y, zf)); v.Add(new Vector3(c.x, c.y, zb)); v.Add(new Vector3(a.x, a.y, zb));
                for (int k = 0; k < 4; k++) n.Add(normal);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                tri.Add(s); tri.Add(s + 2); tri.Add(s + 1); tri.Add(s); tri.Add(s + 3); tri.Add(s + 2);
            }
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            FixWinding(m);
            return m;
        }

        static Mesh Octahedron(string name, float w, float h, float d)
        {
            // Gema facetada (8 caras planas).
            var top = new Vector3(0, h, 0); var bot = new Vector3(0, -h, 0);
            var ring = new[] { new Vector3(w, 0, 0), new Vector3(0, 0, d), new Vector3(-w, 0, 0), new Vector3(0, 0, -d) };
            var v = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % 4];
                int s = v.Count; v.Add(top); v.Add(b); v.Add(a); tri.Add(s); tri.Add(s + 1); tri.Add(s + 2);
                s = v.Count; v.Add(bot); v.Add(a); v.Add(b); tri.Add(s); tri.Add(s + 1); tri.Add(s + 2);
            }
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetTriangles(tri, 0);
            m.SetUVs(0, v.Select(p => new Vector2(p.x / (2 * w) + 0.5f, p.y / (2 * h) + 0.5f)).ToList());
            m.RecalculateNormals(); m.RecalculateBounds();
            FixWinding(m);
            return m;
        }

        /// <summary>Gira los triángulos cuya normal geométrica apunta hacia dentro (las caras quedan visibles desde fuera).</summary>
        static void FixWinding(Mesh m)
        {
            var v = m.vertices; var t = m.triangles; var c = m.bounds.center;
            for (int i = 0; i < t.Length; i += 3)
            {
                var a = v[t[i]]; var b = v[t[i + 1]]; var d = v[t[i + 2]];
                var normal = Vector3.Cross(b - a, d - a);
                var mid = (a + b + d) / 3f;
                if (Vector3.Dot(normal, mid - c) < 0f) { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; }
            }
            m.triangles = t;
            m.RecalculateNormals();
        }

        static Mesh SaveMesh(Mesh m)
        {
            string path = Dir + "/" + m.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { EditorUtility.CopySerialized(m, existing); return existing; }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material Lit(string name, Color baseColor, Color emission, float smoothness)
        {
            string path = Dir + "/" + name + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                m.SetColor("_EmissionColor", emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; // mantiene _EMISSION al reimportar
                m.EnableKeyword("_EMISSION");
            }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
