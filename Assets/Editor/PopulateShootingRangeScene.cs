using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IVRAR.Group02.Environment;
using IVRAR.Group02.XR;

namespace IVRAR.Group02.Editor
{
    /// <summary>
    /// Editor utility that permanently populates the advanced shooting range environment,
    /// 22 tactical mannequin enemies, concrete & VTAC hurdles, overhead floodlights,
    /// and DesktopFallbackController into Group02_MainScene.unity hierarchy.
    /// </summary>
    [InitializeOnLoad]
    public static class PopulateShootingRangeScene
    {
        private const string ROOT_NAME = "ShootingRangeEnvironment";

        static PopulateShootingRangeScene()
        {
            EditorApplication.delayCall += AutoCheckAndPopulate;
        }

        [MenuItem("IVRAR/Populate Shooting Range in Hierarchy", false, 1)]
        public static void ForcePopulate()
        {
            PopulateScene(true);
        }

        [MenuItem("IVRAR/Import TMP Essential Resources", false, 2)]
        public static void ImportTMPEssentials()
        {
            string pkgPath = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (!File.Exists(pkgPath))
            {
                var dirs = Directory.GetDirectories("Library/PackageCache", "com.unity.ugui*");
                if (dirs.Length > 0)
                {
                    pkgPath = Path.Combine(dirs[0], "Package Resources/TMP Essential Resources.unitypackage");
                }
            }

            if (File.Exists(pkgPath))
            {
                AssetDatabase.ImportPackage(pkgPath, false);
                Debug.Log("[PopulateShootingRangeScene] Imported TMP Essential Resources successfully.");
            }
        }

        private static void AutoCheckAndPopulate()
        {
            CheckTMPEssentials();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name == "Group02_MainScene")
            {
                GameObject root = GameObject.Find(ROOT_NAME);
                if (root == null || root.transform.Find("Hurdle_JerseyBarrier_01") == null)
                {
                    PopulateScene(false);
                }
            }
        }

        private static void CheckTMPEssentials()
        {
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                ImportTMPEssentials();
            }
        }

        public static void PopulateScene(bool forceRecreate)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            // Ensure Camera has DesktopFallbackController and AudioListener
            Camera mainCam = Camera.main ?? Object.FindObjectOfType<Camera>();
            if (mainCam != null)
            {
                if (mainCam.GetComponent<AudioListener>() == null)
                {
                    Undo.AddComponent<AudioListener>(mainCam.gameObject);
                }
                if (mainCam.GetComponent<DesktopFallbackController>() == null)
                {
                    Undo.AddComponent<DesktopFallbackController>(mainCam.gameObject);
                }
            }

            // Balanced Realistic Directional Light
            foreach (var light in Object.FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(0.98f, 0.95f, 0.90f, 1f);
                    light.intensity = 1.15f;
                    EditorUtility.SetDirty(light);
                }
            }

            // Balanced RenderSettings
            RenderSettings.fog = false;
            RenderSettings.ambientLight = new Color(0.68f, 0.72f, 0.78f, 1f);
            RenderSettings.ambientIntensity = 0.82f;

            GameObject existing = GameObject.Find(ROOT_NAME);
            if (existing != null)
            {
                if (!forceRecreate && existing.transform.Find("Hurdle_JerseyBarrier_01") != null) return;
                Undo.DestroyObjectImmediate(existing);
            }

            // Delegate to complete environment builder which creates architecture, hurdles, 22 enemies, bench, lights
            GameObject root = RangeEnvironmentRuntimeSpawner.SpawnEnvironment();
            if (root != null)
            {
                Undo.RegisterCreatedObjectUndo(root, "Populate Shooting Range Environment");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PopulateShootingRangeScene] Successfully populated 22 tactical enemies, hurdles, balanced lighting, and desktop controller into hierarchy!");
        }
    }
}
