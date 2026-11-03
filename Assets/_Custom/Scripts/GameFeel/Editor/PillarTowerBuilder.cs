using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Torres de proyección, murales ilustrados y gemas del rayo (lo aplica "Construir escena combinada").
    /// - Torre: decorado nuevo hijo de cada pilar (zócalo octogonal, fuste con franjas de runa del color del elemento,
    ///   tambor con el cristal emisor, aros de bronce y remate). Solo se apaga la malla vieja (Pillar_A) y la bola
    ///   negra del punto de disparo: collider, Rigidbody, Grabbable y rayo siguen siendo los mismos.
    ///   El punto de disparo se adelanta por su propio eje hasta el cristal: el rayo es la misma recta.
    /// - Mural: panel con la ilustración de su elemento (símbolo grande + motivo: llamas, olas, montañas, remolinos).
    ///   Se centra donde pega el rayo con el pilar alineado (antes pegaba hasta 44 cm del centro); malla propia con la
    ///   imagen al derecho por las dos caras (el cubo de Unity la ponía cabeza abajo en una cara). Collider, etiqueta,
    ///   capa y FeelGlow no cambian.
    /// - Gema: en el punto donde converge el rayo, sobre un engaste de bronce; se alinea con el rayo al cargar
    ///   (PillarTowerFeel).
    /// Para quitarlo: borrar la llamada PillarTowerBuilder.Apply en DungeonFinaleBuilder y regenerar.
    /// </summary>
    public static class PillarTowerBuilder
    {
        const string MeshDir = GameFeelAssetBuilder.Root + "/Meshes";
        const string MatDir = GameFeelAssetBuilder.MaterialDir;
        const string TexDir = GameFeelAssetBuilder.TextureDir;
        const float DrumRadius = 0.36f;      // radio (vértice) del tambor del cristal
        const float CrystalOut = 0.045f;     // el cristal sobresale de la cara del tambor

        public static string Apply(Transform dungeon)
        {
            Directory.CreateDirectory(MeshDir);
            var stone = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Altar_Piedra.mat");
            var bronze = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Altar_Bronce.mat");
            int towers = 0, murals = 0;
            var log = new List<string>();
            foreach (var p in dungeon.GetComponentsInChildren<PillarProjector>(true))
            {
                var e = p.RequiredSymbolInSocket;
                var c = ElementPalette.Glow(e);
                var glowMat = GlowMaterial("Torre_Brillo_" + ElementPalette.Name(e), c);
                var feel = p.TryGetComponent<PillarTowerFeel>(out var f0) ? f0 : p.gameObject.AddComponent<PillarTowerFeel>();
                feel.pillar = p;
                feel.color = c;
                if (BuildTower(p, e, stone, bronze, glowMat, feel)) towers++;
                ConfigureBeam(p, c);
                var muralLog = BuildMural(p, e, c, bronze, glowMat, feel);
                if (muralLog != null) { murals++; log.Add(muralLog); }
                EditorUtility.SetDirty(feel);
            }
            return "torres nuevas: " + towers + ", murales ilustrados con gema: " + murals + (log.Count > 0 ? " (" + string.Join("; ", log) + ")" : "");
        }

        // ================================================================ Torre

        static bool BuildTower(PillarProjector p, RunicSymbol e, Material stone, Material bronze, Material glowMat, PillarTowerFeel feel)
        {
            var t = p.transform;
            var old = t.Find("Torre");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            if (p.RaycastOrigin == null) return false;

            // Adelanta el punto de disparo por su propio eje hasta el cristal (misma recta: mismos impactos).
            var origin = p.RaycastOrigin;
            var flatFwd = Vector3.ProjectOnPlane(origin.forward, Vector3.up).normalized;
            float apothem = DrumRadius * Mathf.Cos(Mathf.PI / 8f);
            var axisPoint = new Vector3(t.position.x, origin.position.y, t.position.z);
            float radial = Vector3.Dot(origin.position - axisPoint, flatFwd);
            float wanted = apothem + CrystalOut;
            if (radial < wanted) origin.position += origin.forward * ((wanted - radial) / Mathf.Max(0.2f, Vector3.Dot(origin.forward, flatFwd)));

            var torre = new GameObject("Torre").transform;
            torre.SetParent(t, false);
            var ls = t.lossyScale;
            torre.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z); // medidas en metros reales
            torre.rotation = Quaternion.LookRotation(Vector3.Cross(flatFwd, Vector3.up), Vector3.up); // +X local = hacia el rayo
            torre.position = t.position;
            var o = torre.InverseTransformPoint(origin.position);

            string n = "Torre_" + ElementPalette.Name(e);
            var parts = TowerMeshes(n, o.y, o.x);
            Part(torre, "Piedra", parts.stone, stone);
            Part(torre, "Bronce", parts.bronze, bronze);
            var glow = Part(torre, "Runas", parts.glow, glowMat);

            var crystal = new GameObject("Cristal").transform;
            crystal.SetParent(torre, false);
            crystal.localPosition = o;
            crystal.localRotation = Quaternion.Euler(0f, 0f, 90f); // punta hacia el rayo
            var cr = Part(crystal, "Gema", Bipyramid("Torre_Cristal", 6, 0.055f, 0.085f), glowMat, true);

            // Apagar la columna vieja y la bola negra del punto de disparo (sin tocar su LineRenderer).
            if (t.TryGetComponent<MeshRenderer>(out var oldMr)) oldMr.enabled = false;
            if (origin.TryGetComponent<MeshRenderer>(out var ball)) ball.enabled = false;

            feel.glowParts = new[] { glow };
            feel.crystal = cr;
            return true;
        }

        static Renderer Part(Transform parent, string name, Mesh mesh, Material mat, bool atParent = false)
        {
            var go = atParent ? parent.gameObject : new GameObject(name);
            if (!atParent) go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return mr;
        }

        /// <summary>Mallas de la torre (alto total 2,46 m como la columna vieja). Emisor hacia +X a la altura del cristal.</summary>
        static (Mesh stone, Mesh bronze, Mesh glow) TowerMeshes(string name, float emitY, float emitX)
        {
            var s = new MeshBuild(); var b = new MeshBuild(); var g = new MeshBuild();
            float drumBottom = emitY - 0.2f, drumTop = emitY + 0.22f;
            // Zócalo y basa
            s.Frustum(8, 0.43f, 0.41f, 0f, 0.1f);
            s.Frustum(8, 0.37f, 0.35f, 0.1f, 0.26f);
            b.Frustum(8, 0.355f, 0.355f, 0.26f, 0.3f);
            // Fuste inferior con franjas de runa
            s.Frustum(8, 0.3f, 0.27f, 0.3f, drumBottom - 0.06f);
            float mid = (0.3f + drumBottom) * 0.5f;
            g.Frustum(8, 0.296f, 0.292f, mid - 0.22f, mid - 0.19f);
            g.Frustum(8, 0.288f, 0.284f, mid + 0.04f, mid + 0.07f);
            for (int i = 0; i < 8; i += 2) // ranuras verticales
            {
                float a = (i + 0.5f) * Mathf.PI / 4f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                g.Box(dir * 0.268f + Vector3.up * (mid - 0.075f), new Vector3(0.025f, 0.2f, 0.025f), Quaternion.LookRotation(dir));
            }
            // Tambor del cristal
            b.Frustum(8, 0.33f, 0.33f, drumBottom - 0.06f, drumBottom);
            s.Frustum(8, DrumRadius, DrumRadius, drumBottom, drumTop);
            b.Frustum(8, 0.38f, 0.38f, drumTop, drumTop + 0.06f);
            g.Frustum(8, DrumRadius + 0.004f, DrumRadius + 0.004f, emitY - 0.012f, emitY + 0.012f); // anillo a la altura del rayo
            // Boca del emisor (bronce) alrededor del cristal
            float apo = DrumRadius * Mathf.Cos(Mathf.PI / 8f);
            b.Frustum(8, 0.09f, 0.075f, apo - 0.02f, Mathf.Max(apo + 0.01f, emitX - 0.02f), new Vector3(0f, emitY, 0f), Quaternion.Euler(0f, 0f, -90f));
            // Fuste superior y remate
            s.Frustum(8, 0.26f, 0.22f, drumTop + 0.06f, 2.06f);
            g.Frustum(8, 0.236f, 0.232f, 1.86f, 1.89f);
            b.Frustum(8, 0.27f, 0.27f, 2.06f, 2.11f);
            s.Frustum(8, 0.29f, 0.0f, 2.11f, 2.4f);
            g.Bipyramid(new Vector3(0f, 2.43f, 0f), 6, 0.035f, 0.05f);
            return (s.Save(MeshDir + "/" + name + "_Piedra.asset"), b.Save(MeshDir + "/" + name + "_Bronce.asset"), g.Save(MeshDir + "/" + name + "_Runas.asset"));
        }

        static Mesh Bipyramid(string name, int sides, float r, float half)
        {
            var m = new MeshBuild();
            m.Bipyramid(Vector3.zero, sides, r, half);
            return m.Save(MeshDir + "/" + name + ".asset");
        }

        static Material GlowMaterial(string name, Color c)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", Color.Lerp(new Color(0.12f, 0.11f, 0.1f), c, 0.35f));
            m.SetFloat("_Smoothness", 0.8f);
            m.SetFloat("_Metallic", 0f);
            m.SetColor("_EmissionColor", c * 0.08f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.EnableKeyword("_EMISSION");
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ================================================================ Rayo de energía

        /// <summary>
        /// Rayo místico en vez de la barra opaca de Sprites-Default: material aditivo con textura de energía que fluye
        /// (LaserBeamFeel), más estrecho, con un núcleo fino casi blanco. Solo cambia cómo se dibuja.
        /// </summary>
        static void ConfigureBeam(PillarProjector p, Color c)
        {
            var lr = p.LaserRenderer;
            if (lr == null) return;
            var mat = BeamMaterial();
            if (mat == null) return;
            lr.sharedMaterial = mat;
            lr.textureMode = LineTextureMode.Tile;
            lr.textureScale = new Vector2(0.7f, 1f); // una repetición cada ~1,4 m
            lr.widthMultiplier = 1f;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(0.08f, 0.17f), new Keyframe(1f, 0.15f));
            lr.numCapVertices = 4;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.colorGradient = Grad(c, 0.9f, 0.75f);

            var old = lr.transform.Find("Nucleo");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var coreGo = new GameObject("Nucleo");
            coreGo.transform.SetParent(lr.transform, false);
            var core = coreGo.AddComponent<LineRenderer>();
            core.useWorldSpace = true;
            core.positionCount = 2;
            core.sharedMaterial = mat;
            core.textureMode = LineTextureMode.Tile;
            core.textureScale = new Vector2(1.3f, 1f);
            core.alignment = lr.alignment;
            core.widthMultiplier = 1f;
            core.widthCurve = new AnimationCurve(new Keyframe(0f, 0.035f), new Keyframe(1f, 0.03f));
            core.numCapVertices = 4;
            core.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            core.receiveShadows = false;
            core.colorGradient = Grad(Color.Lerp(c, Color.white, 0.7f), 1f, 0.85f);
            core.enabled = false;

            var feel = lr.TryGetComponent<LaserBeamFeel>(out var f0) ? f0 : lr.gameObject.AddComponent<LaserBeamFeel>();
            feel.pillar = p;
            feel.core = core;
            feel.color = c;
            EditorUtility.SetDirty(feel);
            EditorUtility.SetDirty(lr);
        }

        static Gradient Grad(Color c, float a0, float a1)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(a1, 1f) });
            return g;
        }

        static Material BeamMaterial()
        {
            string texPath = TexDir + "/feel_rayo_energia.png";
            if (!File.Exists(texPath)) BeamTexture(texPath);
            var src = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Feel_ParticleAdditive.mat");
            if (src == null) return null;
            string path = MatDir + "/Rayo_Energia.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(src);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            m.mainTexture = tex;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            m.enableInstancing = false; // los LineRenderer usan bloque de propiedades para el desplazamiento
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// 256 (a lo largo, se repite) × 64 (a lo ancho). Núcleo brillante, halo suave, tres hebras de energía que se
        /// entrelazan y motas; todo periódico a lo largo para que al desplazarse no se note la costura.
        /// </summary>
        static void BeamTexture(string path)
        {
            const int L = 256, A = 64;
            var px = new Color[L * A];
            var rng = new System.Random(5);
            var motes = new List<Vector3>();
            for (int i = 0; i < 9; i++) motes.Add(new Vector3((float)rng.NextDouble(), (float)rng.NextDouble() * 1.4f - 0.7f, 0.5f + (float)rng.NextDouble() * 0.5f));
            for (int y = 0; y < A; y++)
                for (int x = 0; x < L; x++)
                {
                    float u = x / (float)L, v = (y + 0.5f) / A * 2f - 1f; // v: -1..1 a lo ancho
                    float core = Mathf.Exp(-Mathf.Pow(v * 7f, 2f));
                    float halo = Mathf.Exp(-Mathf.Pow(v * 2.4f, 2f)) * 0.38f;
                    float strands = 0f;
                    for (int k = 0; k < 3; k++)
                    {
                        float f = 1 + k, ph = k * 0.33f;
                        float cy = 0.42f * Mathf.Sin((u * f + ph) * Mathf.PI * 2f) * (0.7f + 0.3f * Mathf.Sin((u * 2f + ph) * Mathf.PI * 2f));
                        strands += Mathf.Exp(-Mathf.Pow((v - cy) * 13f, 2f)) * 0.55f;
                    }
                    float flow = 0.82f + 0.18f * Mathf.Sin(u * Mathf.PI * 2f * 3f);
                    float mote = 0f;
                    foreach (var m in motes)
                    {
                        float du = Mathf.Abs(u - m.x); du = Mathf.Min(du, 1f - du) * L / A * 2f; // distancia en las mismas unidades que v
                        mote += Mathf.Exp(-(du * du + (v - m.y) * (v - m.y)) * 260f) * m.z;
                    }
                    float edge = Mathf.Clamp01((1f - Mathf.Abs(v)) * 4f);
                    float val = Mathf.Clamp01((core + halo + strands * flow + mote) * edge);
                    px[y * L + x] = new Color(1f, 1f, 1f, val);
                }
            var tex = new Texture2D(L, A, TextureFormat.RGBA32, false);
            tex.SetPixels(px); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true;
            imp.wrapModeU = TextureWrapMode.Repeat;
            imp.wrapModeV = TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }

        // ================================================================ Mural y gema

        static string BuildMural(PillarProjector p, RunicSymbol e, Color c, Material bronze, Material glowMat, PillarTowerFeel feel)
        {
            var charge = p.GetComponent<PillarChargeFeel>();
            var mural = charge != null && charge.muralGlow != null ? charge.muralGlow.transform : null;
            if (mural == null || !mural.TryGetComponent<MeshFilter>(out var mf) || !mural.TryGetComponent<Renderer>(out var mr)) return null;

            // Dónde pega el rayo con el pilar en su ángulo correcto.
            var q0 = p.transform.rotation;
            var eul = q0.eulerAngles;
            p.transform.rotation = Quaternion.Euler(eul.x, p.PerfectRotationY, eul.z);
            Physics.SyncTransforms();
            bool hitOk = Physics.Raycast(p.RaycastOrigin.position, p.RaycastOrigin.forward, out var hit, p.ProjectionDistance, p.MuralLayer)
                         && hit.collider.transform == mural;
            var rayDir = p.RaycastOrigin.forward;
            p.transform.rotation = q0;
            Physics.SyncTransforms();
            if (!hitOk) return ElementPalette.Name(e) + ": el rayo no llega a su mural";

            // Centrar el mural en el impacto (solo dentro de su plano).
            var normal = hit.normal;
            var center = mural.GetComponent<Collider>().bounds.center;
            var shift = Vector3.ProjectOnPlane(hit.point - center, normal);
            mural.position += shift;

            mf.sharedMesh = PanelMesh();
            mr.sharedMaterial = MuralMaterial(e, c);

            // Gema con engaste, donde converge el rayo.
            var gName = "Gema_" + ElementPalette.Name(e);
            var oldGem = mural.parent.Find(gName);
            if (oldGem != null) Object.DestroyImmediate(oldGem.gameObject);
            var holder = new GameObject(gName).transform;
            holder.SetParent(mural.parent, false);
            holder.position = hit.point;
            holder.rotation = Quaternion.LookRotation(normal, Vector3.up); // +Z sale del mural hacia la torre
            var ring = new MeshBuild();
            ring.Frustum(12, 0.2f, 0.17f, 0f, 0.035f, Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            ring.Frustum(12, 0.13f, 0.11f, 0.035f, 0.06f, Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            Part(holder, "Engaste", ring.Save(MeshDir + "/Mural_Engaste.asset"), bronze);
            var gem = new GameObject("Gema").transform;
            gem.SetParent(holder, false);
            gem.localPosition = new Vector3(0f, 0f, 0.15f);
            var gr = Part(gem, "Gema", Bipyramid("Mural_Gema", 6, 0.085f, 0.14f), glowMat, true);
            gem.localRotation = Quaternion.Euler(0f, 0f, 0f);
            feel.gem = gem;
            feel.gemRenderer = gr;
            feel.alignedRotation = Quaternion.Euler(90f, 0f, 0f); // eje de la gema (Y de la malla) a lo largo del rayo

            return ElementPalette.Name(e) + " centrado " + shift.magnitude.ToString("0.00") + " m";
        }

        /// <summary>Caja 1×1×1 con la imagen al derecho por delante y por detrás (lados con un borde del dibujo).</summary>
        static Mesh PanelMesh()
        {
            string path = MeshDir + "/Mural_Panel.asset";
            var m = new MeshBuild();
            // +Z: vista desde +Z la derecha es -X. -Z: vista desde -Z la derecha es +X.
            m.QuadUV(new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f),
                     new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            m.QuadUV(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                     new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            var edge = new Vector2(0.01f, 0.01f);
            foreach (var (a, b, cc, d) in new[]
            {
                (new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, 0.5f)),
                (new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f)),
                (new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)),
                (new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f)),
            })
                m.QuadUV(a, b, cc, d, edge, edge, edge, edge);
            var mesh = m.Save(path);
            Unwrapping.GenerateSecondaryUVSet(mesh); // UV de lightmap: el mural es estático y se hornea
            return mesh;
        }

        static Material MuralMaterial(RunicSymbol e, Color c)
        {
            var (albedo, emission) = MuralArt.Ensure(e, c);
            string path = MatDir + "/Mural_Ilustrado_" + ElementPalette.Name(e) + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetTexture("_BaseMap", albedo);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.3f);
            m.SetTexture("_EmissionMap", emission);
            m.SetColor("_EmissionColor", c * 0.35f); // FeelGlow parte de aquí y lo sube al cargar el pilar
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(m);
            return m;
        }

        // ================================================================ Mallas (facetadas)

        class MeshBuild
        {
            readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> tri = new List<int>();

            /// <summary>Cara de una caja centrada en el origen: mira hacia fuera (el centro de la cara) con los UV dados.</summary>
            public void QuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                var outward = (a + b + c + d) * 0.25f;
                var normal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(normal, outward) < 0f) normal = -normal;
                int i = v.Count;
                v.AddRange(new[] { a, b, c, d }); n.AddRange(new[] { normal, normal, normal, normal });
                uv.AddRange(new[] { ua, ub, uc, ud });
                // Unity dibuja la cara cuyo Cross(b-a, c-a) apunta hacia quien mira: se elige el orden según la normal.
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0f) tri.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                else tri.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
            {
                var normal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(normal, outward) < 0f) { (b, d) = (d, b); normal = -normal; }
                int i = v.Count;
                v.AddRange(new[] { a, b, c, d }); n.AddRange(new[] { normal, normal, normal, normal });
                float len = (b - a).magnitude, hgt = (d - a).magnitude;
                uv.AddRange(new[] { Vector2.zero, new Vector2(len, 0), new Vector2(len, hgt), new Vector2(0, hgt) });
                tri.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                // Orden de Unity: horario visto desde la normal.
                int t = tri.Count - 6;
                var p0 = v[tri[t]]; var p1 = v[tri[t + 1]]; var p2 = v[tri[t + 2]];
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), normal) < 0f)
                    for (int k = t; k < t + 6; k += 3) (tri[k + 1], tri[k + 2]) = (tri[k + 2], tri[k + 1]);
            }

            void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                var normal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(normal, outward) < 0f) { (b, c) = (c, b); normal = -normal; }
                int i = v.Count;
                v.AddRange(new[] { a, b, c }); n.AddRange(new[] { normal, normal, normal });
                uv.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.up });
                tri.AddRange(new[] { i, i + 1, i + 2 });
            }

            /// <summary>Tronco de pirámide de <paramref name="sides"/> lados entre y0 e y1 (con tapas), caras planas.</summary>
            public void Frustum(int sides, float r0, float r1, float y0, float y1) => Frustum(sides, r0, r1, y0, y1, Vector3.zero, Quaternion.identity);

            public void Frustum(int sides, float r0, float r1, float y0, float y1, Vector3 offset, Quaternion rot)
            {
                Vector3 P(float r, float a, float y) => offset + rot * new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                var axisMid = offset + rot * new Vector3(0f, (y0 + y1) * 0.5f, 0f);
                for (int i = 0; i < sides; i++)
                {
                    // Vértices desfasados medio lado: una cara (no una arista) mira hacia +X.
                    float a0 = (i + 0.5f) * Mathf.PI * 2f / sides, a1 = (i + 1.5f) * Mathf.PI * 2f / sides;
                    var b0 = P(r0, a0, y0); var b1 = P(r0, a1, y0); var t1 = P(r1, a1, y1); var t0 = P(r1, a0, y1);
                    var outward = (b0 + b1 + t0 + t1) * 0.25f - axisMid;
                    if (r1 > 0.0001f) Quad(b0, b1, t1, t0, outward); else Tri(b0, b1, t0, outward);
                    var top = offset + rot * new Vector3(0f, y1, 0f); var bottom = offset + rot * new Vector3(0f, y0, 0f);
                    if (r1 > 0.0001f) Tri(top, t0, t1, rot * Vector3.up);
                    Tri(bottom, b1, b0, rot * Vector3.down);
                }
            }

            /// <summary>Gema: dos pirámides unidas por la base, eje en Y.</summary>
            public void Bipyramid(Vector3 center, int sides, float r, float half)
            {
                var top = center + Vector3.up * half; var bottom = center + Vector3.down * half;
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                    var p0 = center + new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r);
                    var p1 = center + new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r);
                    var mid = (p0 + p1) * 0.5f - center;
                    Tri(top, p0, p1, mid + Vector3.up * 0.5f);
                    Tri(bottom, p1, p0, mid + Vector3.down * 0.5f);
                }
            }

            public void Box(Vector3 center, Vector3 size, Quaternion rot)
            {
                var h = size * 0.5f;
                Vector3 C(float x, float y, float z) => center + rot * new Vector3(x * h.x, y * h.y, z * h.z);
                var f = new[]
                {
                    (C(-1,-1, 1), C( 1,-1, 1), C( 1, 1, 1), C(-1, 1, 1), rot * Vector3.forward),
                    (C( 1,-1,-1), C(-1,-1,-1), C(-1, 1,-1), C( 1, 1,-1), rot * Vector3.back),
                    (C( 1,-1, 1), C( 1,-1,-1), C( 1, 1,-1), C( 1, 1, 1), rot * Vector3.right),
                    (C(-1,-1,-1), C(-1,-1, 1), C(-1, 1, 1), C(-1, 1,-1), rot * Vector3.left),
                    (C(-1, 1, 1), C( 1, 1, 1), C( 1, 1,-1), C(-1, 1,-1), rot * Vector3.up),
                };
                foreach (var (a, b, c, d, o) in f) Quad(a, b, c, d, o);
            }

            public Mesh Save(string path)
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                bool isNew = mesh == null;
                if (isNew) mesh = new Mesh();
                mesh.Clear();
                mesh.name = Path.GetFileNameWithoutExtension(path);
                mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                if (isNew) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
                return mesh;
            }
        }
    }
}
