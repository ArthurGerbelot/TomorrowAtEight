// ARCH-001.a, ARCH-004 | 2026-11-10 | Core module: shared assets, launcher, daily records.
// Decision: .agents/architecture/ARCH-001.a-content-registry.md
// Journal: .agents/days/2026-11-12.md
using TomorrowAtEight.Bootstrap;
using TomorrowAtEight.Content;
using TomorrowAtEight.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace TomorrowAtEight.Definitions
{
    /// <summary>
    /// Shared infrastructure content. The launcher and its primitives are development
    /// tooling and provisional placeholders; they adopt no genre or visual direction.
    /// </summary>
    public static class CoreContent
    {
        public static readonly ContentKey<Font> UiFont = new ContentKey<Font>("font.core.ui");
        public static readonly ContentKey<Shader> LitShader = new ContentKey<Shader>("shader.core.lit");
        public static readonly ContentKey<Mesh> CubeMesh = new ContentKey<Mesh>("mesh.core.cube");
        public static readonly ContentKey<Mesh> OctahedronMesh = new ContentKey<Mesh>("mesh.core.octahedron");
        public static readonly ContentKey<Mesh> PlaneMesh = new ContentKey<Mesh>("mesh.core.plane");
        public static readonly ContentKey<DailyRecordSet> DailyRecords = new ContentKey<DailyRecordSet>("data.core.daily-records");
        public static readonly ContentKey<LauncherCatalog> Launcher = new ContentKey<LauncherCatalog>("data.core.launcher");
        public static readonly ContentKey<SceneRecipe> LauncherScene = new ContentKey<SceneRecipe>("scene.core.launcher");

        public static void Register(ContentRegistration registration)
        {
            ContentRegistry registry = registration.Registry;
            ImportedAssetCatalog imported = registration.Imported;

            // Unity's built-in runtime font: a borrowed third-party dependency.
            registry.Register(UiFont, ContentSource.BuiltIn, "unity-builtin-legacyruntime",
                _ => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

            // Referenced through the generated catalog so build stripping keeps it.
            registry.RegisterImported(LitShader, "urp-17.3.0-lit");

            registry.Register(CubeMesh, ContentSource.Procedural, "procedural-1",
                _ => ProceduralMeshes.CreateCube(CubeMesh.Id));
            registry.Register(OctahedronMesh, ContentSource.Procedural, "procedural-1",
                _ => ProceduralMeshes.CreateOctahedron(OctahedronMesh.Id));
            registry.Register(PlaneMesh, ContentSource.Procedural, "procedural-1",
                _ => ProceduralMeshes.CreatePlane(PlaneMesh.Id));

            registry.Register(DailyRecords, ContentSource.Definition, "schema-1",
                _ => DailyRecordSet.Parse(imported == null ? null : imported.dailyRecords));

            registry.Register(LauncherScene, ContentSource.Definition, "code-1",
                _ => new SceneRecipe(
                    "Tomorrow at Eight",
                    "Development launcher",
                    StageSettings.Default,
                    false,
                    BuildLauncher),
                Launcher, DailyRecords, UiFont);

            // First launcher row. It becomes a real scene through RegisterLauncherScene
            // once a selected prompt creates the game.
            registration.AddLauncherPlaceholder("Play Game", "not available yet");
        }

        /// <summary>Called after every module so the catalog depends on all listed scenes.</summary>
        public static void RegisterLauncherCatalog(ContentRegistration registration)
        {
            var entries = new LauncherEntry[registration.LauncherEntries.Count];
            for (int i = 0; i < entries.Length; i++)
                entries[i] = registration.LauncherEntries[i];

            registration.Registry.Register(Launcher, ContentSource.Definition, "code-1",
                _ => new LauncherCatalog(entries),
                registration.LauncherSceneKeys);
        }

        static void BuildLauncher(SceneBuildContext context)
        {
            LauncherCatalog catalog = context.Registry.Resolve(Launcher);
            DailyRecordSet days = context.Registry.Resolve(DailyRecords);
            UiFactory ui = context.Ui;

            const float width = 460f;
            const float rowHeight = 52f;
            const float rowGap = 12f;
            const float headerHeight = 96f;
            float height = headerHeight + catalog.Entries.Count * (rowHeight + rowGap) + 18f;

            RectTransform panel = ui.AddPanel(context.Canvas, "Launcher", UiFactory.PanelColor);
            UiFactory.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(width, height));

            Vector2 top = new Vector2(0.5f, 1f);
            Text title = ui.AddLabel(panel, "Title", "Tomorrow at Eight", 30, TextAnchor.MiddleCenter, UiFactory.TextColor);
            UiFactory.Place(title.rectTransform, top, top, new Vector2(0f, -16f), new Vector2(width - 32f, 40f));
            Text subtitle = ui.AddLabel(panel, "Subtitle", "Development launcher", 16, TextAnchor.MiddleCenter, UiFactory.MutedTextColor);
            UiFactory.Place(subtitle.rectTransform, top, top, new Vector2(0f, -56f), new Vector2(width - 32f, 24f));

            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                LauncherEntry entry = catalog.Entries[i];
                string sceneId = entry.SceneId;
                bool launchable = entry.Enabled && sceneId != null;
                string label = string.IsNullOrEmpty(entry.Note) ? entry.Label : entry.Label + "  (" + entry.Note + ")";

                Button button = ui.AddButton(panel, "Entry " + i, label, launchable,
                    launchable ? () => GameApp.RequestScene(sceneId) : (System.Action)null);
                UiFactory.Place((RectTransform)button.transform, top, top,
                    new Vector2(0f, -(headerHeight + i * (rowHeight + rowGap))), new Vector2(width - 48f, rowHeight));
            }

            DailyRecord latest = days.Latest;
            string footer = latest == null
                ? "No daily record is included in this build."
                : $"Latest daily record: Day {latest.day} ({latest.date})  |  {latest.title}";
            for (int i = 0; i < days.Problems.Count; i++)
                footer += "\nDaily record problem: " + days.Problems[i];
            footer += $"\nVersion {Application.version}  |  Unity {Application.unityVersion}  |  sessions started: {GameApp.SessionCount}";

            Vector2 bottom = new Vector2(0.5f, 0f);
            Text footerText = ui.AddLabel(context.Canvas, "Footer", footer, 14, TextAnchor.LowerCenter, UiFactory.MutedTextColor);
            UiFactory.Place(footerText.rectTransform, bottom, bottom, new Vector2(0f, 14f), new Vector2(1100f, 80f));

            if (!string.IsNullOrEmpty(GameApp.LastError))
            {
                Vector2 upper = new Vector2(0.5f, 1f);
                Text error = ui.AddLabel(context.Canvas, "Error", GameApp.LastError, 14, TextAnchor.UpperCenter, UiFactory.ErrorTextColor);
                UiFactory.Place(error.rectTransform, upper, upper, new Vector2(0f, -12f), new Vector2(1100f, 70f));
            }
        }
    }
}
