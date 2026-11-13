# Current context

This file is the short operational handoff for the next task. Update it after a
development prompt or meaningful test: current behavior, relevant entry points,
known limitations, latest journal, and next useful action.
Describe what actually exists; keep rationale in ARCHITECTURE.md and work items
in PLAN.md. Before updating, preserve the relevant starting state in the daily journal.

## Current state

- **Latest day:** [2026-11-12](days/2026-11-12.md) (Day 0), status
  partial. The journal's later log observations report successful launcher/test
  session navigation and a Windows standalone build; visual behavior and Unity
  Test Runner results remain unverified. On 2026-11-13 the agent successfully
  compiled and produced a real WebGL build through the Windows filesystem
  bridge. Unity reported zero build errors/warnings; browser behavior remains
  unverified. This container has no Unity Editor. Check the mounted
  `/home/node/editorlogs/Editor.log` for relevant errors and `[TomorrowAtEight]`
  messages without dumping licensing or command-line credentials from the log.
- **What exists in source** (Unity paths under `TomorrowAtEight/`): ARCH-001 keys,
  registry, recipes, entity factory, rights manifest and validator; ARCH-002
  bootstrap (`GameApp`, `GameSession`, `SessionBootstrapSystem`); ARCH-003 runner
  and `TomorrowAtEight/Run/` commands; ARCH-004 daily record schema 1 and runtime
  loading; ARCH-005 presentation bridge and code-built uGUI. Entry points and
  file paths are listed in each ARCH record in [ARCHITECTURE.md](ARCHITECTURE.md).
- **Designed behavior:** the player starts `scene.core.launcher` in the empty host
  scene `Assets/TomorrowAtEight/Scenes/Main.unity` (the only build scene). The
  launcher lists Play Game (disabled placeholder) and Run Test Scene 1 to 3
  (`scene.test.shared-assets`, `scene.test.spawn-lifecycle`,
  `scene.test.input-commands`). Each scene is one `GameSession`; Restart and Back
  to Menu tear it down fully before the next starts. All test content is
  provisional; GAME.md is unchanged.
- **Packages:** `com.unity.entities` `1.5.0` is in `Packages/manifest.json`;
  it is now also resolved in `packages-lock.json`. Unity `6000.3.27f1`, URP `17.3.0`.
- **Hand-written generated files:** `Main.unity`, the build scene entry in
  `ProjectSettings/EditorBuildSettings.asset`,
  `Assets/TomorrowAtEight/Resources/TomorrowAtEight/ImportedAssets.asset`, and
  every `.meta` under `Assets/TomorrowAtEight/` were written by the agent, not
  by Unity. Unity may reserialize them on import; keep their GUIDs.
- **Current player:** a Web player exists for Day 0, labelled `2026-11-12`.
  The earlier build labelled `2026-11-13` was deleted by the owner. Compilation,
  Web build production and payload integrity were verified; browser behavior
  remains unverified. No player was published and no new community prompt was applied.
- **Owner tooling boundary:** release automation and its Unity build helper are
  private and excluded from versioned source. Implementation, machine paths,
  logs and operating instructions belong in owner-local tooling. Project content
  preparation and asset rights checks remain part of the shared game source.
- **Daily preparation:** during the bridge task, the shared state file appeared
  with `2026-11-12/001/project-foundation` completed at `2026-11-13T00:32:31Z`.
  This is file evidence, not an independently observed command run. Preserve
  this file and the concurrent Editor-generated assets/settings. Pending checks
  are the Test Runner and browser behavior. Builds apply any still-pending
  actions automatically; their failure stops the build.
- **Web build verified (2026-11-13):** Windows found the correct Unity and Web
  module. The build applied no pending daily actions, rebuilt the imported
  catalog, validated all 13 reachable assets for public-build rights without
  blocking issues. The latest rebuild, labeled `2026-11-12` at the owner's
  request, most recently succeeded in 7.5840 seconds with zero build errors/warnings and a
  correctly propagated success status. No C# error line appears in
  this run. Agent checks confirm the 17 output files, HTML payload references,
  gzip integrity, WebAssembly magic/version, and Unity data header. A browser
  run and Unity Test Runner results are still pending; nothing was published.
- **Adding a day's record:** copy `.agents/days/TEMPLATE.json` to `Data/Days/YYYY-MM-DD.json`, then
  refresh the catalog with `TomorrowAtEight/Run/Rebuild Imported Asset Catalog`
  or add the TextAsset reference to `ImportedAssets.asset`.
- **Repository:** single root repository (ARCH-006), public source, publication
  reserved to the owner. The Day 0 source snapshot uses `2026-11-12` as its
  commit subject, source tag and build key. The snapshot includes the current
  Unity rendering/player settings, Web build profile, Entities client settings
  and recorded foundation preparation state. Game verification facts are in
  the journal; machine-specific release tooling remains excluded. The recreated
  GitHub repository was confirmed public on 2026-11-13.
