using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using VRTutorial.Lighting.EditorTools;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Construye la claraboya del techo con haces de luz celestiales translúcidos (god rays)
    /// y estandartes medievales rúnicos inspirados en la Imagen 3.
    ///
    /// Optimizado al 100% para Meta Quest (VR):
    /// - Haces de luz con textura de gradiente suave (feel_godray_beam.png) y sombreado aditivo:
    ///   0 siluetas poligonales, 0 ms de raymarching, 16 triángulos por haz.
    /// - Iluminación del tragaluz horneada en lightmaps (Progressive GPU).
    /// - Sin sombras en tiempo real ni luces costosas en runtime.
    /// </summary>
    public static class DungeonAtmosphereBuilder
    {
        const string MatDir = "Assets/_Custom/GameFeel/Materials";
        const string MeshDir = "Assets/_Custom/GameFeel/Meshes";
        const string TexDir = "Assets/_Custom/GameFeel/Textures";
        const string AtmosphereRootName = "Atmosfera_Mazmorra";

        // Centro exacto de Tile_A_22 (4x4m sobre el altar y pie de escaleras)
        public static readonly Vector3 SkylightCenter = new Vector3(-15.0f, 6.0f, 16.64f);

        [MenuItem("Tools/Game Feel/Aplicar claraboya y props de mazmorra (Imagen 3)")]
        public static void ApplyMenu()
        {
            var scene = SceneManager.GetActiveScene();
            var dungeon = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Mazmorra");
            if (dungeon == null)
            {
                Debug.LogWarning("[Atmosfera] No se encontró el objeto 'Mazmorra' en la escena activa.");
                return;
            }
            string log = Apply(dungeon.transform);
            Debug.Log("[Atmosfera] " + log);
        }

        [MenuItem("Tools/Game Feel/Quitar claraboya y props")]
        public static void RemoveMenu()
        {
            var scene = SceneManager.GetActiveScene();
            var dungeon = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Mazmorra");
            if (dungeon == null) return;
            Remove(dungeon.transform);
            Debug.Log("[Atmosfera] Claraboya y props eliminados.");
        }

        public static string Apply(Transform dungeon)
        {
            var log = new List<string>();
            var root = dungeon.Find(AtmosphereRootName);
            if (root != null) Object.DestroyImmediate(root.gameObject);

            var atmoGo = new GameObject(AtmosphereRootName);
            atmoGo.transform.SetParent(dungeon, true);
            atmoGo.transform.position = Vector3.zero;
            atmoGo.transform.rotation = Quaternion.identity;

            // 1. Claraboya en el techo con pozo de piedra, vigas y haces de luz celestiales
            log.Add(BuildSkylight(dungeon, atmoGo.transform));

            // 2. Estandartes medievales en las paredes
            log.Add(BuildBanners(dungeon, atmoGo.transform));

            // 3. Velas y detalles en repisas
            log.Add(BuildCandlesAndProps(dungeon, atmoGo.transform));

            // 4. Postprocesado URP adaptado a la paleta de la Imagen 3
            log.Add(UpdatePostProcessing());

            EditorUtility.SetDirty(atmoGo);
            return string.Join("\n", log);
        }

        public static void Remove(Transform dungeon)
        {
            var root = dungeon.Find(AtmosphereRootName);
            if (root != null) Object.DestroyImmediate(root.gameObject);

            // Reactivar Tile_A_22
            var tile22 = dungeon.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Tile_A_22");
            if (tile22 != null) tile22.gameObject.SetActive(true);
        }

        // ================================================================ 1. Claraboya
        static string BuildSkylight(Transform dungeon, Transform atmoRoot)
        {
            // Desactivar Tile_A_22 para abrir el hueco exacto de 4x4m
            var tile22 = dungeon.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Tile_A_22");
            if (tile22 != null)
            {
                Undo.RecordObject(tile22.gameObject, "Abrir claraboya");
                tile22.gameObject.SetActive(false);
            }

            var skylightGo = new GameObject("Claraboya_Techo");
            skylightGo.transform.SetParent(atmoRoot, true);
            skylightGo.transform.position = SkylightCenter;

            var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/DungeonModularPack/Materials/M_Wall.mat");

            // Pozo de luz: 4 muros verticales de piedra (4x4m, altura 1.6m)
            float h = 1.6f;
            float midY = h * 0.5f;

            // Muros exteriores del pozo
            CreateWall(skylightGo.transform, new Vector3(0, midY, -2.0f), new Vector3(4.0f, h, 0.35f), wallMat, "Muro_Norte");
            CreateWall(skylightGo.transform, new Vector3(0, midY, 2.0f), new Vector3(4.0f, h, 0.35f), wallMat, "Muro_Sur");
            CreateWall(skylightGo.transform, new Vector3(-2.0f, midY, 0), new Vector3(0.35f, h, 4.0f), wallMat, "Muro_Oeste");
            CreateWall(skylightGo.transform, new Vector3(2.0f, midY, 0), new Vector3(0.35f, h, 4.0f), wallMat, "Muro_Este");

            // 2 Vigas transversales de piedra (estilo arquitectónico de la Imagen 3)
            CreateWall(skylightGo.transform, new Vector3(0, 0.55f, -0.65f), new Vector3(4.0f, 0.40f, 0.35f), wallMat, "Viga_Piedra_1");
            CreateWall(skylightGo.transform, new Vector3(0, 0.55f, 0.65f), new Vector3(4.0f, 0.40f, 0.35f), wallMat, "Viga_Piedra_2");

            // Cielo exterior visto por la claraboya (placa unlit emisiva celestial hacia abajo)
            var skyGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skyGo.name = "Cielo_Exterior";
            skyGo.transform.SetParent(skylightGo.transform, false);
            skyGo.transform.localPosition = new Vector3(0, 1.55f, 0);
            skyGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            skyGo.transform.localScale = new Vector3(3.9f, 3.9f, 1f);
            var skyCol = skyGo.GetComponent<Collider>();
            if (skyCol != null) Object.DestroyImmediate(skyCol);

            var skyMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            skyMat.name = "M_Cielo_Claraboya";
            skyMat.SetFloat("_Cull", 0); // Doble faz para visibilidad total desde abajo
            skyMat.SetColor("_BaseColor", new Color(0.70f, 0.85f, 1.0f));
            skyGo.GetComponent<MeshRenderer>().sharedMaterial = skyMat;

            // Haces translúcidos de luz celestial (Haz_Claraboya.asset + Haz_Claraboya.mat)
            var beamMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshDir}/Haz_Claraboya.asset");
            var beamMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Haz_Claraboya.mat");
            if (beamMesh != null && beamMat != null)
            {
                var beamGo = new GameObject("Haz_Luz_Volumetrico");
                beamGo.transform.SetParent(skylightGo.transform, false);
                beamGo.transform.localPosition = Vector3.zero;
                beamGo.transform.localRotation = Quaternion.identity;
                var mf = beamGo.AddComponent<MeshFilter>();
                mf.sharedMesh = beamMesh;
                var mr = beamGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = beamMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
            }

            // Luz de la claraboya: Spot light celestial horneada apuntando diagonal hacia el altar
            var lightGo = new GameObject("Luz_Claraboya");
            lightGo.transform.SetParent(skylightGo.transform, false);
            lightGo.transform.localPosition = new Vector3(-0.2f, 1.5f, 0.0f);
            lightGo.transform.forward = new Vector3(0.2f, -1.0f, 0.05f).normalized;
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = new Color(0.68f, 0.84f, 1.0f); // azul cielo celestial
            l.intensity = 18f;
            l.range = 18f;
            l.spotAngle = 65f;
            l.innerSpotAngle = 28f;
            l.lightmapBakeType = LightmapBakeType.Baked;
            l.shadows = LightShadows.Soft;
            l.shapeRadius = 0.50f;

            // Marcar objetos estáticos para el horneado
            foreach (var r in skylightGo.GetComponentsInChildren<MeshRenderer>())
            {
                if (r.gameObject.name == "Haz_Luz_Volumetrico") continue;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            }

            return "claraboya: pozo de luz de 4x4m con vigas de piedra, cielo celestial y haces volumétricos suaves creados";
        }

        static void CreateWall(Transform parent, Vector3 localPos, Vector3 scale, Material mat, string name)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
        }

        // ================================================================ 2. Estandartes
        static string BuildBanners(Transform dungeon, Transform atmoRoot)
        {
            var bannerRoot = new GameObject("Estandartes");
            bannerRoot.transform.SetParent(atmoRoot, true);
            bannerRoot.transform.position = Vector3.zero;

            var bannerMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshDir}/Estandarte.asset");
            var bronzeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Custom/GameFeel/Materials/Altar_Bronce.mat");

            var bannerSpawns = new (string name, Vector3 pos, float rotY, string element)[]
            {
                // Flanqueando el arco de la pared trasera (Oeste)
                ("Estandarte_Arco_Norte", new Vector3(-24.70f, 3.8f, 13.1f), 90f,  "Fuego"),
                ("Estandarte_Arco_Sur",   new Vector3(-24.70f, 3.8f, 20.2f), 90f,  "Agua"),
                // Muro Norte entre columnas
                ("Estandarte_Muro_Norte1", new Vector3(-19.0f, 3.6f, 10.95f), 0f,  "Tierra"),
                ("Estandarte_Muro_Norte2", new Vector3(-15.0f, 3.6f, 10.95f), 0f,  "Viento"),
                // Muro Sur entre columnas
                ("Estandarte_Muro_Sur1",   new Vector3(-19.0f, 3.6f, 22.35f), 180f, "Fuego"),
                ("Estandarte_Muro_Sur2",   new Vector3(-15.0f, 3.6f, 22.35f), 180f, "Agua")
            };

            int count = 0;
            foreach (var b in bannerSpawns)
            {
                var bGo = new GameObject(b.name);
                bGo.transform.SetParent(bannerRoot.transform, false);
                bGo.transform.position = b.pos;
                bGo.transform.rotation = Quaternion.Euler(0f, b.rotY, 0f);

                // Mástil de bronce
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Mastil";
                pole.transform.SetParent(bGo.transform, false);
                pole.transform.localPosition = new Vector3(0, 0, 0);
                pole.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                pole.transform.localScale = new Vector3(0.045f, 0.70f, 0.045f);
                if (pole.GetComponent<Collider>() != null) Object.DestroyImmediate(pole.GetComponent<Collider>());
                if (bronzeMat != null) pole.GetComponent<MeshRenderer>().sharedMaterial = bronzeMat;

                // Tela del estandarte
                if (bannerMesh != null)
                {
                    var cloth = new GameObject("Tela");
                    cloth.transform.SetParent(bGo.transform, false);
                    cloth.transform.localPosition = new Vector3(0, 0, 0.02f);
                    cloth.transform.localRotation = Quaternion.identity;
                    var mf = cloth.AddComponent<MeshFilter>();
                    mf.sharedMesh = bannerMesh;
                    var mr = cloth.AddComponent<MeshRenderer>();
                    var bMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/M_Estandarte_{b.element}.mat");
                    if (bMat != null) mr.sharedMaterial = bMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }

                count++;
            }

            return $"estandartes: {count} estandartes medievales con glifos elementales colocados";
        }

        // ================================================================ 3. Velas y Utilería
        static string BuildCandlesAndProps(Transform dungeon, Transform atmoRoot)
        {
            var propsRoot = new GameObject("Utileria_Repisas");
            propsRoot.transform.SetParent(atmoRoot, true);
            propsRoot.transform.position = Vector3.zero;

            var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/DungeonModularPack/Materials/M_Wall.mat");
            var bronzeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Custom/GameFeel/Materials/Altar_Bronce.mat");

            // Repisa sobre el mural de fuego
            var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.name = "Repisa_Velas_Fuego";
            shelf.transform.SetParent(propsRoot.transform, false);
            shelf.transform.position = new Vector3(-19.2f, 3.25f, 11.15f);
            shelf.transform.localScale = new Vector3(2.4f, 0.12f, 0.40f);
            shelf.GetComponent<MeshRenderer>().sharedMaterial = wallMat;

            // Velas sobre la repisa
            float[] candleX = { -20.2f, -20.0f, -19.8f, -19.5f, -19.3f, -19.0f, -18.7f, -18.4f, -18.2f };
            float[] candleHeights = { 0.28f, 0.18f, 0.35f, 0.22f, 0.30f, 0.16f, 0.32f, 0.24f, 0.15f };

            for (int i = 0; i < candleX.Length; i++)
            {
                var candle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                candle.name = $"Vela_{i + 1}";
                candle.transform.SetParent(propsRoot.transform, false);
                float ch = candleHeights[i];
                candle.transform.position = new Vector3(candleX[i], 3.31f + ch * 0.5f, 11.15f);
                candle.transform.localScale = new Vector3(0.06f, ch * 0.5f, 0.06f);
                if (candle.GetComponent<Collider>() != null) Object.DestroyImmediate(candle.GetComponent<Collider>());

                var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "Llama";
                flame.transform.SetParent(candle.transform, false);
                flame.transform.localPosition = new Vector3(0, 1.15f, 0);
                flame.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f);
                if (flame.GetComponent<Collider>() != null) Object.DestroyImmediate(flame.GetComponent<Collider>());
                var flameMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                flameMat.SetColor("_BaseColor", new Color(1.0f, 0.80f, 0.25f));
                flame.GetComponent<MeshRenderer>().sharedMaterial = flameMat;
            }

            // Escudos apoyados contra el muro
            var shield1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shield1.name = "Escudo_Apoyado_1";
            shield1.transform.SetParent(propsRoot.transform, false);
            shield1.transform.position = new Vector3(-21.8f, 0.65f, 22.25f);
            shield1.transform.rotation = Quaternion.Euler(75f, 15f, 0f);
            shield1.transform.localScale = new Vector3(0.70f, 0.04f, 0.70f);
            if (shield1.GetComponent<Collider>() != null) Object.DestroyImmediate(shield1.GetComponent<Collider>());
            if (bronzeMat != null) shield1.GetComponent<MeshRenderer>().sharedMaterial = bronzeMat;

            return "utileria: repisa con 9 velas encendidas sobre mural y escudos apoyados creados";
        }

        // ================================================================ 4. Postprocesado
        public static string UpdatePostProcessing()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(QuestLighting.ProfilePath);
            if (profile == null) return "postproceso: perfil no encontrado";

            // ColorAdjustments: contraste equilibrado sin empastar sombras, saturación rica
            if (profile.TryGet<ColorAdjustments>(out var color))
            {
                color.contrast.Override(11.0f);
                color.saturation.Override(13.0f);
                color.postExposure.Override(0.35f);
            }

            // SplitToning: armonía de dos tonos de la Imagen 3 (altas luces de fuego ámbar + sombras azul pizarra celestial)
            if (profile.TryGet<SplitToning>(out var split))
            {
                split.active = true;
                split.shadows.Override(new Color(0.40f, 0.45f, 0.58f));    // sombras azul pizarra
                split.highlights.Override(new Color(0.70f, 0.58f, 0.40f)); // fuego ámbar dorado
                split.balance.Override(-8f);
            }

            // LiftGammaGain: levanta sutilmente las sombras con matiz azul frío y empuja el fuego en altas luces
            if (profile.TryGet<LiftGammaGain>(out var lgg))
            {
                lgg.active = true;
                lgg.lift.Override(new Vector4(0.98f, 0.99f, 1.03f, 0.012f));
                lgg.gamma.Override(new Vector4(1.02f, 1.01f, 1.04f, 0.02f));
                lgg.gain.Override(new Vector4(1.04f, 1.01f, 0.97f, 0.0f));
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return "postproceso: ajustado al contraste y dual-tone (fuego ámbar vs luz celestial) de Imagen 3";
        }
    }
}
