// ARCH-003 | 2026-11-11 | Explicit list of dated editor actions. Remove an entry during
// maintenance once its outputs and completion state are versioned and reproducible.
// Decision: .agents/ARCHITECTURE.md#arch-003---owner-triggered-editor-automation
// Journal: .agents/days/2026-11-12.md
using System.Collections.Generic;

namespace TomorrowAtEight.EditorTools
{
    public static class DailyActions
    {
        public static readonly IReadOnlyList<DailyEditorAction> All = new[]
        {
            new DailyEditorAction
            {
                Id = "2026-11-12/001/project-foundation",
                Journal = ".agents/days/2026-11-12.md",
                Description =
                    "Confirm the host scene and build scene list, regenerate the imported asset catalog, " +
                    "apply web player settings, and validate asset rights for the public source and build.",
                ExpectedOutputs = new[]
                {
                    ProjectCommands.HostScenePath,
                    "ProjectSettings/EditorBuildSettings.asset",
                    ImportedAssetCatalogBuilder.CatalogPath,
                    "ProjectSettings/ProjectSettings.asset (web compression settings)",
                },
                Apply = () =>
                {
                    ProjectCommands.EnsureHostScene();
                    ProjectCommands.ConfigureBuildScenes();
                    ImportedAssetCatalogBuilder.Rebuild();
                    ProjectCommands.ConfigureWebPlayerSettings();
                },
                Verify = () =>
                {
                    string problem = ProjectCommands.VerifyBuildScenes()
                        ?? ImportedAssetCatalogBuilder.Verify()
                        ?? ProjectCommands.VerifyWebPlayerSettings();
                    if (problem != null)
                        return problem;

                    foreach (string scope in new[] { AssetRightsValidator.PublicSource, AssetRightsValidator.PublicBuild })
                    {
                        RightsReport report = ProjectCommands.ValidateRights(scope);
                        if (report.HasBlockingIssues)
                            return report.ToText();
                    }
                    return null;
                },
            },
        };
    }
}
