// ARCH-003 | 2026-11-11 | Reusable, repeatable owner-triggered editor commands. Nothing here
// runs on project open or script reload.
// Decision: .agents/ARCHITECTURE.md#arch-003---owner-triggered-editor-automation
// Journal: .agents/days/2026-11-12.md
using System;
using System.IO;
using TomorrowAtEight.Bootstrap;
using TomorrowAtEight.Content;
using TomorrowAtEight.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TomorrowAtEight.EditorTools
{
    public static class ProjectCommands
    {
        public const string MenuRoot = "TomorrowAtEight/Run/";
        public const string HostScenePath = "Assets/TomorrowAtEight/Scenes/" + GameApp.HostSceneName + ".unity";

        [MenuItem(MenuRoot + "Rebuild Scene Main", priority = 20)]
        static void RebuildHostSceneMenu() => Report("Rebuild Scene Main", () =>
        {
            EnsureHostScene();
            ConfigureBuildScenes();
        });

        [MenuItem(MenuRoot + "Rebuild Imported Asset Catalog", priority = 21)]
        static void RebuildCatalogMenu() => Report("Rebuild Imported Asset Catalog",
            () => ImportedAssetCatalogBuilder.Rebuild());

        [MenuItem(MenuRoot + "Configure Web Player Settings", priority = 22)]
        static void ConfigureWebMenu() => Report("Configure Web Player Settings", ConfigureWebPlayerSettings);

        [MenuItem(MenuRoot + "Validate Content and Asset Rights", priority = 40)]
        static void ValidateMenu() => Report("Validate Content and Asset Rights", () =>
        {
            foreach (string scope in new[]
            {
                AssetRightsValidator.PrivateDevelopment, AssetRightsValidator.PublicSource,
                AssetRightsValidator.PublicBuild, AssetRightsValidator.PublicMedia,
            })
            {
                RightsReport report = ValidateRights(scope);
                if (report.HasBlockingIssues)
                    Debug.LogError("[TomorrowAtEight] " + report.ToText());
                else
                    Debug.Log("[TomorrowAtEight] " + report.ToText());
            }
        });

        /// <summary>Creates the empty host scene if it is missing. Never edits an existing one.</summary>
        public static void EnsureHostScene()
        {
            if (File.Exists(HostScenePath))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(HostScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            bool saved = EditorSceneManager.SaveScene(scene, HostScenePath);
            EditorSceneManager.CloseScene(scene, true);
            if (!saved)
                throw new InvalidOperationException($"Could not save the host scene to {HostScenePath}.");
            AssetDatabase.ImportAsset(HostScenePath);
        }

        /// <summary>Makes the host scene the only scene in the build list.</summary>
        public static void ConfigureBuildScenes()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            bool matches = scenes.Length == 1 && scenes[0].enabled && scenes[0].path == HostScenePath;
            if (!matches)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(HostScenePath, true) };
        }

        public static string VerifyBuildScenes()
        {
            if (!File.Exists(HostScenePath))
                return $"Host scene {HostScenePath} does not exist.";
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length != 1 || !scenes[0].enabled || scenes[0].path != HostScenePath)
                return $"Build scene list is not exactly [{HostScenePath}].";
            return null;
        }

        /// <summary>
        /// Web player settings for simple static hosting: gzip with the decompression
        /// fallback, so no server-side Content-Encoding configuration is required.
        /// </summary>
        public static void ConfigureWebPlayerSettings()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
        }

        public static string VerifyWebPlayerSettings()
        {
            if (PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Gzip)
                return "Web compression format is not Gzip.";
            if (!PlayerSettings.WebGL.decompressionFallback)
                return "Web decompression fallback is disabled.";
            return null;
        }

        /// <summary>
        /// Registers and validates all content exactly like the runtime bootstrap, then
        /// checks the rights records of every asset part reachable from the startup scene.
        /// </summary>
        public static RightsReport ValidateRights(string scope)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ImportedAssetCatalog>(ImportedAssetCatalogBuilder.CatalogPath);
            var registry = new ContentRegistry(catalog, UnityObjects.Destroy);
            try
            {
                ContentCatalog.RegisterAll(registry, catalog);
                registry.ValidateAndSeal();
                AssetMetadataManifest manifest = AssetMetadataManifest.LoadFromProject();
                return AssetRightsValidator.Validate(
                    registry, manifest, new[] { ContentCatalog.StartupScene.Id }, scope);
            }
            finally
            {
                registry.Dispose();
            }
        }

        static void Report(string name, Action command)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError($"[TomorrowAtEight] '{name}' cannot run in Play mode.");
                return;
            }
            try
            {
                command();
                Debug.Log($"[TomorrowAtEight] '{name}' finished.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[TomorrowAtEight] '{name}' failed: {exception.Message}");
                Debug.LogException(exception);
            }
        }
    }
}
