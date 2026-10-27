using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Segunda tanda de recursos: marco de hueco, brillo del aro, fuego de antorcha optimizado, salpicadura de araña,
    /// aura de la runa, separación de haces láser y fuego para todas las antorchas de la escena abierta.
    /// </summary>
    public static class GameFeelAssetBuilder2
    {
        const string Root = GameFeelAssetBuilder.Root;
        const string MeshDir = Root + "/Meshes";

        [MenuItem("Tools/Game Feel/Regenerar recursos (tanda 2)")]
        public static void BuildAll()
        {
            var frame = BuildTexture("feel_frame", FrameAlpha);
            var ring = BuildTexture("feel_ring", RingAlpha);
            BuildMaterial("Feel_SlotFrame", frame, true);
            BuildMaterial("Feel_RingGlow", ring, true);
            BuildMaterial("Feel_PortalGlow", AssetDatabase.LoadAssetAtPath<Texture2D>(GameFeelAssetBuilder.TextureDir + "/feel_soft_dot.png"), true);
            BuildPrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log("[GameFeel] Recursos tanda 2 generados");
        }

        // ------------------------------------------------------------- Texturas
        static float FrameAlpha(float u, float v)
        {
            float d = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)); // distancia al borde
            float band = Mathf.Exp(-Mathf.Pow((d - 0.06f) / 0.035f, 2f));
            return Mathf.Clamp01(band + Mathf.Clamp01(1f - d / 0.5f) * 0.08f);
        }

        static float RingAlpha(float u, float v)
        {
            float dx = u * 2f - 1f, dy = v * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.09f, 2f)) + Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.22f, 2f)) * 0.35f);
        }

        static Texture2D BuildTexture(string name, System.Func<float, float, float> alpha)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha((x + 0.5f) / size, (y + 0.5f) / size)));
            tex.Apply();
            Directory.CreateDirectory(GameFeelAssetBuilder.TextureDir);
            string path = GameFeelAssetBuilder.TextureDir + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material BuildMaterial(string name, Texture2D tex, bool doubleSided)
        {
            // Se parte del material aditivo de partículas ya configurado (URP Particles/Unlit, aditivo).
            var source = AssetDatabase.LoadAssetAtPath<Material>(GameFeelAssetBuilder.MaterialDir + "/Feel_ParticleAdditive.mat");
            string path = GameFeelAssetBuilder.MaterialDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(source); AssetDatabase.CreateAsset(mat, path); }
            else mat.CopyPropertiesFromMaterial(source);
            mat.SetTexture("_BaseMap", tex);
            if (doubleSided && mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------- Partículas
        static void BuildPrefabs()
        {
            var additive = AssetDatabase.LoadAssetAtPath<Material>(GameFeelAssetBuilder.MaterialDir + "/Feel_ParticleAdditive.mat");
            var alpha = AssetDatabase.LoadAssetAtPath<Material>(GameFeelAssetBuilder.MaterialDir + "/Feel_ParticleAlpha.mat");
            Save(TorchFire(additive), "Feel_TorchFire");
            Save(Splat(alpha), "Feel_Splat");
            Save(MagicLoop(additive), "Feel_MagicLoop");
        }

        static ParticleSystem NewSystem(string name, Material mat)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return ps;
        }

        static Gradient Grad(Color a, Color b, Color c)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.45f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.7f, 0.55f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>
        /// Fuego de antorcha barato: espacio local, sin ruido ni colisiones (Unity puede saltarse su simulación
        /// cuando no se ve: "procedural culling"), máximo 14 partículas y material compartido.
        /// </summary>
        static ParticleSystem TorchFire(Material mat)
        {
            var ps = NewSystem("Feel_TorchFire", mat);
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.duration = 1f; main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 16;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.28f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            var em = ps.emission; em.rateOverTime = 26f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 8f; shape.radius = 0.04f; shape.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = Grad(new Color(1f, 0.75f, 0.25f), new Color(1f, 0.32f, 0.02f), new Color(0.5f, 0.03f, 0f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.25f));
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sortMode = ParticleSystemSortMode.None; r.maxParticleSize = 0.4f;
            return ps;
        }

        static ParticleSystem Splat(Material mat)
        {
            var ps = NewSystem("Feel_Splat", mat);
            var main = ps.main;
            main.loop = false; main.playOnAwake = true; main.duration = 0.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 30;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.8f, 0.12f), new Color(0.12f, 0.35f, 0.05f));
            main.gravityModifier = 1.3f;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = 0.05f; shape.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            return ps;
        }

        static ParticleSystem MagicLoop(Material mat)
        {
            var ps = NewSystem("Feel_MagicLoop", mat);
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // deja estela cuando la runa vuela
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 40;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new Color(0.55f, 0.9f, 1f);
            main.gravityModifier = -0.05f;
            var em = ps.emission; em.rateOverTime = 18f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.18f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = Grad(Color.white, new Color(0.6f, 0.95f, 1f), new Color(0.3f, 0.6f, 1f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            return ps;
        }

        static void Save(ParticleSystem ps, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(ps.gameObject, GameFeelAssetBuilder.PrefabDir + "/" + name + ".prefab");
            Object.DestroyImmediate(ps.gameObject);
        }

        // ------------------------------------------------------------- Láser en haces

        /// <summary>
        /// Separa la malla de una barrera láser en <paramref name="bands"/> haces (de arriba hacia abajo), cada uno
        /// como hijo con su propia malla guardada en Assets/_Custom/GameFeel/Meshes. Oculta el renderer original.
        /// </summary>
        public static List<GameObject> SplitIntoBands(GameObject src, int bands)
        {
            var mf = src.GetComponent<MeshFilter>();
            var mr = src.GetComponent<MeshRenderer>();
            var mesh = mf.sharedMesh;
            var verts = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;
            var tris = mesh.triangles;

            // Componentes conexas (uniendo vértices con la misma posición).
            var parent = Enumerable.Range(0, verts.Length).ToArray();
            System.Func<int, int> find = null;
            find = i => parent[i] == i ? i : (parent[i] = find(parent[i]));
            System.Action<int, int> unite = (a, b) => { a = find(a); b = find(b); if (a != b) parent[a] = b; };
            var byPos = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < verts.Length; i++)
            {
                var key = Vector3Int.RoundToInt(verts[i] * 10000f);
                if (byPos.TryGetValue(key, out var other)) unite(i, other); else byPos[key] = i;
            }
            for (int t = 0; t < tris.Length; t += 3) { unite(tris[t], tris[t + 1]); unite(tris[t], tris[t + 2]); }

            var comps = new Dictionary<int, List<int>>(); // raíz -> triángulos (índice inicial)
            for (int t = 0; t < tris.Length; t += 3)
            {
                int root = find(tris[t]);
                if (!comps.TryGetValue(root, out var list)) comps[root] = list = new List<int>();
                list.Add(t);
            }
            var compList = comps.Values.Select(list =>
            {
                float y = 0f; int n = 0;
                foreach (var t in list) for (int k = 0; k < 3; k++) { y += src.transform.TransformPoint(verts[tris[t + k]]).y; n++; }
                return new { tris = list, y = y / n };
            }).OrderByDescending(c => c.y).ToList();

            // Agrupa en bandas cortando por los saltos de altura más grandes.
            var groups = new List<List<int>>();
            if (compList.Count <= bands) foreach (var c in compList) groups.Add(new List<int>(c.tris));
            else
            {
                var gaps = Enumerable.Range(0, compList.Count - 1).Select(i => new { i, gap = compList[i].y - compList[i + 1].y })
                    .OrderByDescending(g => g.gap).Take(bands - 1).Select(g => g.i).OrderBy(i => i).ToList();
                int start = 0;
                foreach (var cut in gaps.Concat(new[] { compList.Count - 1 }))
                {
                    var g = new List<int>();
                    for (int i = start; i <= cut; i++) g.AddRange(compList[i].tris);
                    groups.Add(g); start = cut + 1;
                }
            }

            Directory.CreateDirectory(MeshDir);
            var result = new List<GameObject>();
            for (int gi = 0; gi < groups.Count; gi++)
            {
                var map = new Dictionary<int, int>();
                var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nu = new List<Vector2>(); var nt = new List<int>();
                foreach (var t in groups[gi])
                    for (int k = 0; k < 3; k++)
                    {
                        int old = tris[t + k];
                        if (!map.TryGetValue(old, out var idx))
                        {
                            idx = nv.Count; map[old] = idx; nv.Add(verts[old]);
                            if (normals.Length > 0) nn.Add(normals[old]);
                            if (uvs.Length > 0) nu.Add(uvs[old]);
                        }
                        nt.Add(idx);
                    }
                var m = new Mesh { name = src.name + "_haz" + gi };
                m.SetVertices(nv); if (nn.Count > 0) m.SetNormals(nn); if (nu.Count > 0) m.SetUVs(0, nu);
                m.SetTriangles(nt, 0); m.RecalculateBounds();
                string path = MeshDir + "/" + src.name.Replace(" ", "_") + "_haz" + gi + ".asset";
                AssetDatabase.CreateAsset(m, path);

                var child = new GameObject("Haz " + gi);
                child.transform.SetParent(src.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = m;
                var r = child.AddComponent<MeshRenderer>();
                r.sharedMaterials = mr.sharedMaterials;
                r.shadowCastingMode = mr.shadowCastingMode;
                r.receiveShadows = mr.receiveShadows;
                result.Add(child);
            }
            mr.enabled = false;
            return result;
        }

        // ------------------------------------------------------------- Fuego en antorchas

        [MenuItem("Tools/Game Feel/Añadir fuego a las antorchas de la escena")]
        public static void AddFiresMenu() => Debug.Log("[GameFeel] Fuegos añadidos: " + AddTorchFires());

        /// <summary>Añade Feel_TorchFire sobre cada antorcha de pared (malla Torch_A) que no tenga ya uno.</summary>
        public static int AddTorchFires()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameFeelAssetBuilder.PrefabDir + "/Feel_TorchFire.prefab");
            int added = 0;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var mf in Resources.FindObjectsOfTypeAll<MeshFilter>())
            {
                if (mf.gameObject.scene != scene || mf.sharedMesh == null || mf.sharedMesh.name != "Torch_A") continue;
                var path = AnimationUtility.CalculateTransformPath(mf.transform, null);
                if (path.Contains("Seccion 2")) continue; // el laberinto ya tiene sus propias llamas
                if (mf.transform.Find("Fuego") != null) continue;
                // Punto de la llama: promedio de los vértices más altos de la antorcha (la copa).
                var v = mf.sharedMesh.vertices;
                float maxY = v.Max(p => p.y);
                var top = v.Where(p => p.y > maxY - 0.05f).ToList();
                // Un poco por debajo de las puntas de la jaula, para que la llama salga desde dentro.
                Vector3 local = new Vector3(top.Average(p => p.x), maxY - 0.11f, top.Average(p => p.z));
                var fire = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mf.transform);
                fire.name = "Fuego";
                fire.transform.localPosition = local;
                fire.transform.localRotation = Quaternion.identity;
                Undo.RegisterCreatedObjectUndo(fire, "Fuego antorcha");
                added++;
            }
            return added;
        }
    }
}
