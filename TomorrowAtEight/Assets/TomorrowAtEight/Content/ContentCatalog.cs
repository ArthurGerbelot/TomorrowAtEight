// ARCH-001.a | 2026-11-10 | Explicit module list; no assembly scanning or scene searches.
// Decision: .agents/architecture/ARCH-001.a-content-registry.md
// Journal: .agents/days/2026-11-12.md
using TomorrowAtEight.Definitions;
using UnityEngine;

namespace TomorrowAtEight.Content
{
    /// <summary>
    /// Registers every content module in a documented order. New content normally extends
    /// a module; a new module adds one call here.
    /// </summary>
    public static class ContentCatalog
    {
        /// <summary>Scene started when the player launches and by Back to Menu.</summary>
        public static ContentKey<SceneRecipe> StartupScene => CoreContent.LauncherScene;

        /// <summary>Font used by the provisional development UI.</summary>
        public static ContentKey<Font> UiFont => CoreContent.UiFont;

        /// <summary>Launcher rows, in registration order.</summary>
        public static ContentKey<LauncherCatalog> Launcher => CoreContent.Launcher;

        public static void RegisterAll(ContentRegistry registry, ImportedAssetCatalog imported)
        {
            var registration = new ContentRegistration(registry, imported);

            // Order: 1. core (shared assets, launcher scene, Play Game row, daily records),
            //        2. provisional test scenes,
            //        3. launcher catalog, which depends on every scene registered above.
            CoreContent.Register(registration);
            TestContent.Register(registration);
            CoreContent.RegisterLauncherCatalog(registration);
        }
    }
}
