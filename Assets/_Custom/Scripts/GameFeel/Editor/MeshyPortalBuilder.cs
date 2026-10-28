using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Pone el logo "LSV-Inmersive 360" hecho con Meshy en el portal de la pared de inicio de 2_Adaptacion,
    /// en lugar de la placa con el título hecha a mano. El resto del escenario (arco, columnas, antorchas,
    /// barril, cajas, libros y pociones) se mantiene; solo se apaga el grupo "Portal_Titulo/Titulo".
    /// Deja la importación lista para Quest: sin animación, con UV de lightmap y textura ASTC.
    /// Menú: Tools/Game Feel/Colocar logo de Meshy en el portal. Se puede volver a ejecutar.
    /// </summary>
    public static class MeshyPortalBuilder
    {
        const string Dir = "Assets/_Custom/Models/PortalMeshy";
        const string ModelPath = Dir + "/Portal_Meshy.fbx";
        const string TexturePath = Dir + "/Portal_Meshy_Color.png";
        const string MaterialPath = Dir + "/Portal_Meshy.mat";
        const string InstanceName = "Logo_Meshy";

        // Cabe entre las dos columnas del portal; el fondo del logo apoya en el frente del arco (z = -0,81).
        const float TargetWidth = 2.9f;
        const float EmissionLevel = 0.45f;
        static readonly Vector3 Center = new Vector3(0.3f, 2.0f, -0.8f);

        [MenuItem("Tools/Game Feel/Colocar logo de Meshy en el portal")]
        public static void BuildMenu() => Debug.Log("[GameFeel] " + Build());

        public static string Build()
        {
            ConfigureImport();
            var mat = GetOrCreateMaterial();
            var scene = SceneManager.GetActiveScene();

            foreach (var r in scene.GetRootGameObjects().Where(r => r.name == InstanceName).ToArray()) Object.DestroyImmediate(r);
            var root = new GameObject(InstanceName);
            SceneManager.MoveGameObjectToScene(root, scene);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            go.name = "Relieve";

            // El FBX trae la rotación (270, 0, 0): así el frente ya mira a +Z (la sala) y la parte de arriba queda arriba.
            var renderer = go.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterial = mat;
            go.transform.position = Vector3.zero;
            var b = renderer.bounds;
            float k = TargetWidth / b.size.x;
            go.transform.localScale *= k;
            b = renderer.bounds;
            go.transform.position += new Vector3(Center.x - b.center.x, Center.y - b.center.y, Center.z - b.min.z);

            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            string log = "logo de Meshy colocado: " + renderer.bounds.size.ToString("F2") + " m";

            // El escenario del portal sigue; solo se apaga la placa con el título hecha a mano (y su luz verde).
            var portal = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Portal_Titulo");
            if (portal != null)
            {
                portal.SetActive(true);
                var title = portal.transform.Find("Titulo");
                if (title != null && title.gameObject.activeSelf) { title.gameObject.SetActive(false); log += "; placa hecha a mano desactivada"; }
            }
            // Versión anterior de esta herramienta: relieve grande contra la pared, ya no se usa.
            foreach (var r in scene.GetRootGameObjects().Where(r => r.name == "Portal_Meshy").ToArray()) { Object.DestroyImmediate(r); log += "; quitado el relieve grande anterior"; }
            EditorSceneManager.MarkSceneDirty(scene);
            return log;
        }

        static void ConfigureImport()
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            bool changed = false;
            if (mi.importAnimation) { mi.importAnimation = false; changed = true; }
            if (mi.animationType != ModelImporterAnimationType.None) { mi.animationType = ModelImporterAnimationType.None; changed = true; }
            if (!mi.generateSecondaryUV) { mi.generateSecondaryUV = true; changed = true; } // UV de lightmap para el horneado
            if (mi.materialImportMode != ModelImporterMaterialImportMode.None) { mi.materialImportMode = ModelImporterMaterialImportMode.None; changed = true; }
            if (mi.importCameras || mi.importLights) { mi.importCameras = false; mi.importLights = false; changed = true; }
            if (changed) mi.SaveAndReimport();

            var ti = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            var android = ti.GetPlatformTextureSettings("Android");
            if (!android.overridden || android.format != TextureImporterFormat.ASTC_6x6 || android.maxTextureSize != 2048)
            {
                android.overridden = true;
                android.maxTextureSize = 2048;
                android.format = TextureImporterFormat.ASTC_6x6;
                ti.SetPlatformTextureSettings(android);
                ti.mipmapEnabled = true;
                ti.anisoLevel = 4; // se ve en ángulo al girar la cabeza
                ti.SaveAndReimport();
            }
        }

        static Material GetOrCreateMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, MaterialPath); }
            m.shader = shader;
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.2f);
            // Algo de brillo con su propia textura: la luz cálida de las antorchas volvía verdes las letras azules.
            m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
            m.SetColor("_EmissionColor", new Color(EmissionLevel, EmissionLevel, EmissionLevel));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; // mantiene _EMISSION y no se hornea
            m.EnableKeyword("_EMISSION");
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
