# Architecture memory

This file is the index and summary of technical decisions: register ARCH-NNN
records here. Detailed parts live in `architecture/`. Agents read the relevant
linked parts and referenced code before creating or extending a system.
These decisions describe the reusable design; they may evolve with a recorded reason.

## Recording and finding decisions

- Create a record headed `## ARCH-NNN - Title` for a significant architectural choice.
  Use the next number after the highest registered ID; never recycle retired IDs.
- Record date, status, decision and rationale, actual code paths/symbols, how to
  reuse or extend it, known limits, and the source day or owner decision.
- Split a large decision into stable thematic parts such as `ARCH-001.a` and
  `ARCH-001.b`. Keep its parent summary, shared status/origin, and links in this
  index. Store each part in `architecture/ARCH-NNN.x-topic.md`; its heading uses
  `# ARCH-NNN.x - Title` and links back to the parent. Read the relevant parts
  and their dependencies, rather than loading every detailed file by default.
- Child suffixes identify parts of the same decision, not new top-level ARCH
  numbers. Never reassign a published suffix or renumber parts when reordering.
  Existing parent references stay valid; code may cite a precise child ID/file.
- A move or compatible extension retains its ID and updates its code references.
  A replacement decision gets a new ID; link both records and mark the old one superseded.
- Add the ID near the code embodying the decision, rather than commenting every line.
  Use the actual decision date and implementation journal. For example:

```csharp
// ARCH-001 | YYYY-MM-DD | Stable IDs keep visual replacements compatible.
// Decision: .agents/ARCHITECTURE.md#arch-001---unified-content-recipes
// Journal: .agents/days/YYYY-MM-DD.md
```

From the project root, find all references or list registered decisions:

```bash
rg -n --hidden -g '!.git/**' 'ARCH-001' .
rg -n '^## ARCH-[0-9]+' .agents/ARCHITECTURE.md
rg -n '^# ARCH-[0-9]+[.][a-z]+' .agents/architecture
```

ARCH IDs identify decisions, not game entities. Content IDs belong to the implemented
catalog. The records below capture the owner's agreed design; code paths and
implementation journals are added when the systems are actually built.

The Unity project root is `TomorrowAtEight/` relative to the repository root.
Paths beginning with `Assets/`, `Packages/`, or `ProjectSettings/` below are relative
to that Unity project; `.agents/` paths are relative to the repository root.
API names and layouts marked as planned are contracts for implementation, not
evidence that the corresponding code already exists.

## ARCH-001 - Unified content recipes

- **Date/status:** 2026-11-10; clarified by the owner on 2026-11-11 and extended
  with asset rights metadata on 2026-11-12; first implementation written on
  2026-11-12 (Day 0). Compiled for WebGL on 2026-11-13; browser behavior remains
  unverified (see the Day 0 journal's follow-up).
- **Decision/reason:** one typed `ContentRegistry` holds the definitions of all
  content. Shared assets are resolved through it; an `EntityFactory` creates ECS
  instances from registered entity recipes. Features extend these entry points
  so that content selection, creation, and replacement follow one contract.

Read the relevant detailed contracts before implementing or extending this system:

| Part | Contract |
| --- | --- |
| [ARCH-001.a](architecture/ARCH-001.a-content-registry.md) | Stable typed keys, registration, shared assets, and resource lifetime. |
| [ARCH-001.b](architecture/ARCH-001.b-entity-factory.md) | World-local entity prefabs, spawning, and despawning. |
| [ARCH-001.c](architecture/ARCH-001.c-asset-rights.md) | Asset provenance, PI status, permitted scopes, and replacements. |
| [ARCH-001.d](architecture/ARCH-001.d-layout-validation.md) | Planned code layout and first implementation checks. |

- **Origin/evolution:** owner setup discussion, clarified by direct administrative
  request on 2026-11-11. These are concrete initial contracts; record reasons for
  future compatible extensions here. A replacement architecture follows the
  decision supersession procedure above. Add actual code paths and checks when
  implemented; this clarification does not create a development day.
- **Implementation (2026-11-12, [2026-11-12 journal](days/2026-11-12.md)):** all paths are under
  `Assets/TomorrowAtEight/`. Keys and kind contracts: `Runtime/Content/ContentKey.cs`
  (`ContentKey<T>`, `ContentKeyRules`). Registry: `Runtime/Content/ContentRegistry.cs`
  (`Register`, `RegisterImported`, `ValidateAndSeal`, `Resolve`,
  `GetDependencyClosure`, `Dispose`). Recipes: `Runtime/Content/Recipes.cs`
  (`EntityRecipe`, `SceneRecipe`, `StageSettings`). Module context and launcher
  rows: `Runtime/Content/ContentRegistration.cs`. Generated catalog type:
  `Runtime/Content/ImportedAssetCatalog.cs`. Factory: `Runtime/Entities/EntityFactory.cs`.
  Module list: `Content/ContentCatalog.cs` (`RegisterAll`), modules
  `Content/CoreContent.cs` and `Content/TestScenes/TestContent.cs`. Rights
  manifest: `Data/Content/AssetMetadata.json`, validated by
  `Editor/Content/AssetRightsValidator.cs`. Import map and catalog generation:
  `Editor/Content/ImportedAssetCatalogBuilder.cs`. Checks:
  `Tests/Editor/` (registry, factory, rights) and `Tests/PlayMode/NavigationTests.cs`.
  Initially written without Unity on 2026-11-12. A real WebGL build succeeded
  on 2026-11-13; browser behavior and Test Runner results remain unverified.
- **Launchable scenes:** a module calls `ContentRegistration.RegisterLauncherScene`
  to register a `scene.*` recipe and add its launcher row in one step;
  `AddLauncherPlaceholder` adds a disabled row. `CoreContent.RegisterLauncherCatalog`
  then registers `data.core.launcher`, which depends on every listed scene. The
  launcher itself is the recipe `scene.core.launcher` (`ContentCatalog.StartupScene`).
- **Extensions adopted while implementing (2026-11-12):** kind `font` with result
  type `Font` (UI text needs a tracked asset); every registration declares a
  `ContentSource` (`Definition`, `Procedural`, `Imported`, `BuiltIn`) and a
  `providerRevision` that ARCH-001.c records must match; a provider or recipe that
  uses a key it did not declare as a dependency fails, so the dependency closure
  used for rights validation is complete; registration problems are collected
  and reported together by `ValidateAndSeal`.
- **Documentation organization:** split into these thematic parts on 2026-11-12
  at the owner's request. The parent ID and adopted decisions remain unchanged;
  detailed contracts live in the linked files rather than duplicate copies here.

## ARCH-002 - Code-driven scene bootstrap

- **Date/status:** 2026-11-10; clarified on 2026-11-11; first implementation
  written on 2026-11-12 (Day 0), compiled for WebGL on 2026-11-13. Earlier Editor
  logs show successful session navigation; browser behavior remains unverified.
- **Decision:** one code entry point owns initialization and shutdown of a content
  session in the active gameplay scene. It constructs scene content, camera,
  lighting, and UI after definitions are registered. Import/build settings are
  scriptable; a saved scene or Inspector wiring is not a second content source.
- **Initialization order:** load the optional imported asset catalog; register
  content modules; validate and seal the registry; obtain the single default ECS
  gameplay world; create the world-local entity factory and presentation bridge;
  resolve the selected `scene.*` recipe; spawn its content through the factory;
  then enable gameplay updates. In normal play, Unity Entities owns creation,
  PlayerLoop updates, and disposal of its default world. The bootstrap does not
  create or tick a duplicate world. Test fixtures may own isolated worlds.
- **Readiness:** a session bootstrap in `InitializationSystemGroup` performs the
  initialization once and publishes a `GameSessionReady` singleton only on
  success. Gameplay systems require that singleton to update. Remove it before
  teardown or scene reconstruction; initialization failure leaves it absent.
- **Lifecycle:** initialization is idempotent for a session, including when domain
  reload is disabled. Prevent simulation of a partly initialized scene. On scene
  shutdown, disable gameplay updates, release spawned entities and presentation
  instances while the world exists, release world-local prefab caches, then release
  generated shared assets and clear session references. Do not dispose a world
  owned by Unity Entities. Report initialization failure with the failing step.
- **Reason:** changes can be implemented entirely through prompts without requiring
  the owner to arrange the hierarchy or configure Inspector fields manually.
- **Reuse/extension:** extend scene-building code using the content recipe mechanism.
  A demonstration scene establishes technical examples, not a permanent game genre.
  Owner-triggered editor preparation may generate saved content through ARCH-003.
- **Implementation (2026-11-12, [2026-11-12 journal](days/2026-11-12.md)):** `Runtime/Bootstrap/GameApp.cs`
  holds navigation requests and starts `scene.core.launcher` from
  `RuntimeInitializeOnLoadMethod(AfterSceneLoad)`. `SessionBootstrapSystem`
  (`Runtime/Bootstrap/SessionSystems.cs`, `InitializationSystemGroup`) applies
  requests; `GameSession` (`Runtime/Bootstrap/GameSession.cs`) runs the steps in
  the order above, reports the failing step, and publishes `GameSessionReady`
  (`Runtime/Entities/Components.cs`). A session owns one root GameObject with its
  camera, light, views, canvas, and event system; nothing persists between
  sessions except the Entities default world and `GameApp` statics.
- **Navigation and cleanup:** UI only calls `GameApp.RequestScene`,
  `RequestRestart`, or `RequestMenu`. The bootstrap tears the current session down
  in one frame and initializes the next in the following frame, because
  `Object.Destroy` is deferred. Teardown removes the ready singleton, views, every
  `SessionOwned` entity and prefab, the root object, then generated assets. Each
  start logs counts and reports an error unless exactly one camera, canvas, event
  system, and session root exist; each teardown logs leftover owned entities.
  A failed scene returns to the launcher, which shows the failure.
- **Host scene:** `Assets/TomorrowAtEight/Scenes/Main.unity` is empty and is the
  only build scene. If Play starts in another scene in the Editor, `GameApp`
  loads `Main` first; the Test Runner's scene is left alone.
- **Scene HUD:** a `SceneRecipe` with `ShowSceneHud` gets Restart and Back to Menu
  from `Runtime/Presentation/SceneHud.cs`; scenes do not build their own.
- **Origin/limits:** owner discussion during setup. Initially written without
  Unity on 2026-11-12; a real WebGL build succeeded on 2026-11-13. Browser behavior
  and Test Runner results remain unverified.

## ARCH-003 - Owner-triggered editor automation

- **Date/status:** 2026-11-11; first implementation written on 2026-11-12
  (Day 0). Build tooling was subsequently separated into private owner tooling.
  Real Unity compilation and Web player production were verified on 2026-11-13
  (Day 0 journal).
- **Decision:** allow the owner to run prepared Unity Editor commands to create or
  update assets, managers, and scenes. Use one shared runner and the existing
  ARCH-001 recipes and ARCH-002 bootstrap wherever applicable. Keep automation
  editor-only, outside player builds; execution is explicit, not automatic on
  project opening or script reload.
- **Menus:** `TomorrowAtEight/Run Daily Scripts` applies all pending daily actions in
  UTC date and explicit sequence order. It skips completed actions and reports
  when nothing is pending. Reusable commands live under `TomorrowAtEight/Run/`, for
  example `Rebuild Asset <content-id>` or `Rebuild Scene <content-id>`. Do not add
  a permanent menu entry for every date.
- **Daily actions:** each action has a stable ID such as `YYYY-MM-DD/NNN/slug`, its
  source journal, expected outputs, and any prerequisites. Multiple actions or
  owner corrections on the same day receive distinct IDs. Applied actions are
  immutable; later corrections use new IDs. Actions must tolerate retries after
  partial execution without duplicating assets or managers.
- **Completion state:** keep a project-local, versioned record of completed action
  IDs and completion times together with generated assets/scenes and their `.meta`
  files. The planned location is `ProjectSettings/TomorrowAtEight.EditorActions.json`;
  this file now records the foundation action complete at `2026-11-13T00:32:31Z`.
  A last-run date may be displayed, but it is not the
  criterion for skipping actions. Do not rely solely on machine preferences or
  disposable caches.
- **Success and failure:** save and verify each action's expected outputs before
  persisting its completion. Stop at the first failure, leave that action pending,
  and report its ID and recovery instructions. Previously completed actions stay
  completed; later actions remain pending. A successful repeat click is a no-op
  for daily actions already completed in this project state.
- **Reusable commands:** keep generators that are still useful. Repeated runs
  update the intended generated content without accumulating duplicates, retaining
  stable content IDs, existing asset GUIDs, and references. Document the scope of
  any overwritten generated content.
- **Retention:** daily action implementations are temporary. After execution is
  verified and their outputs and completion state are versioned together, remove
  obsolete daily code during maintenance when the current recipes or a documented,
  checked-in baseline can reproduce the required project state. Keep reusable
  generators and historical evidence in Git and the journal. A fresh checkout
  must work from that baseline and the remaining pending actions. Do not depend on
  self-deleting scripts or retain hundreds of obsolete daily scripts in active code.
  Check for eligible cleanup when adding new daily actions.
- **Owner handoff:** give the exact menu path, prerequisites, expected changes,
  one-time or repeatable behavior, and checks still needed. Continue all work that
  does not depend on the owner's execution. Distinguish code prepared from an action
  actually executed; attribute owner-reported results and synchronize memory and
  the relevant journal when execution is confirmed.
- **Implementation (2026-11-12, [2026-11-12 journal](days/2026-11-12.md)):** runner and state file handling in
  `Assets/TomorrowAtEight/Editor/Automation/EditorActionRunner.cs`; the explicit
  action list in `Editor/Automation/DailyActions.cs` (currently
  `2026-11-12/001/project-foundation`); reusable commands in
  `Editor/Automation/ProjectCommands.cs` under `TomorrowAtEight/Run/`:
  `Rebuild Scene Main`, `Rebuild Imported Asset Catalog`,
  `Configure Web Player Settings`, and `Validate Content and Asset Rights`.
  The daily content preparation batch entry point is
  `TomorrowAtEight.EditorTools.EditorActionRunner.RunDailyScriptsFromCommandLine`.
  The state file is created on the first successful run. The host scene, build
  scene list, and imported asset catalog were also written directly as source
  files on 2026-11-12 so the project is usable before the action runs; the action
  confirms them, applies web player settings, and validates rights.
- **Verification:** real Unity compilation and WebGL player production were
  verified on 2026-11-13 with zero build errors and warnings. Public-build
  rights validation passed for 13 assets. Browser behavior and Test Runner
  results remain unverified; historical project evidence is in the Day 0 journal.
- **Owner-local automation boundary (2026-11-13):** private orchestration is
  excluded from project source, including its build command, configuration,
  runtime state, logs and operating instructions. The owner's private tooling
  owns that implementation. The project retains content preparation and rights
  validation used by its recipes and tests; it has no dependency on private
  release tooling. This boundary does not change recorded project verification
  or the daily-action completion state.
- **Origin:** direct owner administrative request on 2026-11-11; first real
  agent-triggered web build observed on 2026-11-13 (Day 0 journal follow-up).
- **Technical references:** Unity's [MenuItem](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/menuitem)
  supports editor menu commands; [AssetDatabase.SaveAssets](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/assetdatabase/saveassets)
  and [EditorSceneManager.SaveScene](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/scenemanagement/editorscenemanager/savescene)
  provide asset and scene persistence. Confirm APIs against the project's Unity
  version when implementing.

## ARCH-004 - Public daily data and agent journals

- **Date/status:** 2026-11-11; first implementation written on 2026-11-12 (Day 0).
  JSON authoring is verified outside Unity; import, runtime loading, and UI are not.
- **Shared date key (owner decision, 2026-11-13):** the applied record's `date`
  identifies its build, Git commit subject and Git source tag as `YYYY-MM-DD`.
  The recap links to that tag, while opening the next voting day with its own
  explicit deadline. A Git tag already published must not be moved silently.
- **Decision/reason:** use one UTF-8 JSON file per development day for public content
  the game can load, paired with a Markdown journal for agent working context.
  JSON is directly readable and writable without Unity and supports structured
  runtime data. A separate ScriptableObject copy of each day's content is unnecessary.
- **Identity/location:** share the UTC date between
  `Assets/TomorrowAtEight/Data/Days/YYYY-MM-DD.json` and `.agents/days/YYYY-MM-DD.md`.
  The agent journal links to the public record and its relevant source revision.
- **Public content:** content rather than metadata: what was asked (the exact
  selected prompt and its author when known), what was done, optional details
  worth sharing, and optional suggestions for next prompts. Build, status, and
  content-ID data are not stored; the date identifies the day. Never invent a
  community winner. Use a root object with typed fields and arrays compatible
  with Unity's JSON serializer.
- **Ownership/history:** the JSON is authoritative for public fields, including
  the exact selected prompt. The Markdown owns starting context, technical owner
  inputs, reasoning, ARCH IDs, actual checks, unfinished work, and handoff. Link
  between them instead of maintaining duplicate prompt or public-summary copies.
  Preserve original selected input; append dated public corrections in JSON and
  technical corrections in the journal after a day is closed.
- **Authoring:** preserve the exact input in JSON before implementation. Serialize
  strings with JSON tooling and verify decoded text, including line breaks. The
  agent reads the JSON directly when a request needs an earlier prompt or summary.
  If Unity is unavailable, the source can still be written and validated; report
  import, runtime loading, and UI verification as pending.
- **Runtime loading:** Unity imports JSON as TextAsset data. Include the intended
  daily TextAssets through a lightweight catalog referenced by the bootstrap or
  existing content registry. Parse their text into plain serializable data objects;
  runtime code does not read `.agents/` or use AssetDatabase. Editor preparation
  may refresh catalog references via ARCH-003, preserving existing `.meta` GUIDs.
- **Build consistency:** the game's journal reflects the daily records actually
  included in that build. Later source edits require a new build to appear there.
  Public reports use the JSON content and the journal's actual verification/build
  facts; preparing records or a report does not publish them.
- **Implementation/origin:** direct owner administrative request on 2026-11-11,
  permitting SO, Markdown, or JSON with a preference for simple serialization.
- **Implementation (2026-11-12, [2026-11-12 journal](days/2026-11-12.md)):** the record is defined by
  `Assets/TomorrowAtEight/Runtime/Content/DailyRecord.cs` (`DailyRecord`,
  `DailyRecordSet.Parse`). Every file has exactly the same keys in the same
  order, so the game and a future website can process any day the same way:
  `date`, `day`, `title`, `author`, `prompt`, `done`, `details`, `suggestions`.
  Start each day from `.agents/days/TEMPLATE.json`; `.agents/days/day.schema.json`
  is the JSON Schema of the same structure for tools outside Unity. Change the
  data class, template, and schema together. The generated
  `ImportedAssetCatalog.dailyRecords` list references every JSON file in
  `Data/Days/`; `data.core.daily-records` parses them, and the launcher shows the
  latest record. Invalid records are listed as problems instead of being dropped
  silently. Refresh the list with `TomorrowAtEight/Run/Rebuild Imported Asset Catalog`
  (also run by `Build Web Player`) after adding a day. A full in-game journal
  view does not exist yet.
- **Technical references:** Unity [Text assets](https://docs.unity.com/en-us/engine/6000.3/manual/assets-and-media/asset-types/class-text-asset)
  and [JSON serialization](https://docs.unity.com/en-us/engine/6000.0/manual/programming-environment/code-reload-serialization/script-serialization/json-serialization).

## ARCH-005 - Unity Entities simulation and web presentation

- **Date/status:** 2026-11-11; first implementation written on 2026-11-12 (Day 0),
  compiled for WebGL on 2026-11-13 with Entities `1.5.0`. Browser behavior remains
  unverified.
- **Decision/reason:** use Unity's `com.unity.entities` ECS as the default for
  gameplay state and simulation. Prefer unmanaged `IComponentData` and
  `IBufferElementData` with systems operating on queries. Use `ISystem` where
  practical. Shared immutable configuration may use blob assets when useful.
  Managed services remain at boundaries; do not introduce another ECS framework.
- **System responsibilities:** input adapters submit commands; initialization
  prepares content; simulation systems update authoritative state; presentation
  reads the resulting state. Use Unity's system groups and explicit ordering for
  dependencies. Use fixed-step simulation for behavior that needs a fixed time
  step; do not impose it on every system. Structural changes during simulation
  use command buffers at a documented playback point (ARCH-001).
- **Web presentation:** use a main-thread `PresentationBridge` between ECS and
  ordinary URP GameObjects for renderable views. It instantiates each model view
  from registered content, tracks it by its world's full `Entity` handle, copies
  ECS transforms/presentation data after simulation, and removes views for
  despawned entities. World/session shutdown clears all remaining views. Views
  contain no independent gameplay simulation. UI, camera, audio, and other Unity
  features may use small managed adapters when their APIs require them.
- **Compatibility:** code-driven bootstrap does not make every Unity subsystem
  ECS-compatible. Entities Graphics 1.5 lists web platforms as unsupported; it is
  not part of the initial web baseline. Keep the bridge replaceable behind the
  same entity and content contracts if platform support changes. Burst and jobs
  are optimizations where supported and verified, not prerequisites that assume
  browser threading support. Establish a working web baseline before adding them.
- **Dependency baseline:** the existing project records Unity `6000.3.27f1` in
  `ProjectSettings/ProjectVersion.txt` and URP `17.3.0` in `Packages/manifest.json`.
  The Entities package is absent. Unity's 6000.3 package documentation lists
  `com.unity.entities` `1.5.0` as released; this is the initial installation
  candidate, subject to Package Manager resolution and Editor/web verification.
  Record the version actually resolved in the manifest/lockfile and this decision.
  Do not claim it is installed or tested yet. Add Physics, Entities Graphics,
  Netcode, or other packages only when a concrete task needs a compatible feature.
- **Extension rule:** implement gameplay through ECS by default. When a feature
  needs a managed Unity API or a different approach, keep its adapter narrow and
  record the practical reason. Use ARCH-001 keys/factories for its content rather
  than adding a separate loading or object-creation framework.
- **Implementation (2026-11-12, [2026-11-12 journal](days/2026-11-12.md)):** `com.unity.entities` `1.5.0` was
  added to `Packages/manifest.json` (the Unity registry listed it as latest, with
  minimum Unity `6000.0`). The lockfile is unchanged until Unity resolves it;
  record the resolved version here once observed. The bridge is
  `Assets/TomorrowAtEight/Runtime/Presentation/PresentationBridge.cs`, driven by
  `SessionPresentationSystem` in `PresentationSystemGroup`; it reads
  `LocalToWorld` and `ViewModel` and instantiates inactive `model.*` templates.
  Simulation systems live with their content (`Content/TestScenes/TestSystems.cs`),
  use `ISystem` and `SystemAPI.Query`, require `GameSessionReady`, and run before
  `TransformSystemGroup`. Deferred structural changes play back at
  `EndSimulationEntityCommandBufferSystem`; playing them back in the next frame
  could run after a teardown has destroyed the prefabs. Prefabs start with a zero
  `LocalToWorld` so a view stays hidden until its world matrix exists.
- **Managed adapters and their reasons:** session bootstrap and presentation
  systems (`SystemBase`; they create GameObjects), `PawnInputSystem` (Input
  System devices, camera, and event system), and uGUI built in code by
  `Runtime/Presentation/UiFactory.cs` with the legacy `Text` component and Unity's
  built-in font, because UI Toolkit and TextMesh Pro need imported assets.
- **Web build notes:** `Assets/TomorrowAtEight/link.xml` preserves the
  `TomorrowAtEight` assembly because ECS types are discovered by type. The URP Lit
  shader is referenced by the imported asset catalog so it is included in builds;
  runtime materials use only its default variant. Burst and jobs are not used.
- **Origin/limits:** direct owner administrative request on 2026-11-11.
  Source was initially written without Unity on 2026-11-12. The Web build
  module was missing then, but a real WebGL build succeeded on 2026-11-13.
  Browser behavior and Test Runner results remain unverified.
- **Technical references (checked 2026-11-11):** Unity's
  [Entities package for Unity 6000.3](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-entities),
  [Entities manual](https://docs.unity3d.com/Packages/com.unity.entities@1.5/manual/index.html),
  and [Entities Graphics requirements](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.5/manual/requirements-and-compatibility.html).

## ARCH-006 - Single repository and shared public data

- **Date/status:** 2026-11-11; single local repository configured. Website and
  public data export remain unimplemented.
- **Decision/reason:** keep one Git repository at the workspace root for the Unity
  project, `.agents/`, `AGENTS.md`, public README, development configuration, and
  the future public `web/` directory. Owner-specific release orchestration is
  excluded from this project history at the owner's explicit request.
  A single project history keeps game changes,
  prompts, agent context, and website changes together. Do not initialize a second
  repository inside the Unity project or future website by default.
- **History:** retain Unity's original `e7e4bd9` initial check-in as the parent of
  the repository migration, with its project files moved under `TomorrowAtEight/`
  in the migration tree. Preserve the owner's existing working changes separately
  from that migration. The old embedded Git metadata was backed up outside the
  working tree before removal; the root repository retains the original commit.
- **Git configuration:** retain the existing GitHub `origin`. Keep Unity's
  `.gitignore` in `TomorrowAtEight/` so its project-relative exclusions still work.
  Move `.gitattributes` to the repository root, where its LFS and Unity attribute
  macros are valid, and prefix Unity-specific paths. Git LFS is initialized
  locally; the devcontainer declares the official Git LFS feature for rebuilds.
  Retain Unity's local `core.filemode=false` and `core.symlinks=false` settings for
  the Windows-mounted working tree. Existing asset contents and `.meta` GUIDs
  remain unchanged; filesystem executable bits do not create spurious changes.
- **Versioned boundaries:** track project sources, Unity settings, package
  manifest/lockfile, `.meta` files, agent instructions/memory, and development
  configuration. Exclude Unity caches, generated builds, website dependencies and
  output, owner-local release tooling, machine-local `.aws/`, `.codex/`, `.claude/`, and
  home-directory backups.
  A local backup is not a project source file.
- **Public data:** ARCH-004's daily JSON in
  `Assets/TomorrowAtEight/Data/Days/` remains the editable public source. A future
  website build may read those files directly or a script may generate copies
  under `web/public/days/`, together with explicitly publishable media. Generated
  copies are not maintained independently. Technical `.agents/` journals are not
  automatically included in site output.
- **Visibility and publication:** direct owner clarification on 2026-11-11 selects
  a public source repository with publication to the official repository reserved
  to the owner. Public reading/forking does not grant write access. Do not grant
  collaborators or integrations independent write or publication permissions.
  Runtime visitors read exported JSON, media, and builds from the website. The
  local migration performed no push, site deployment, hosted visibility change,
  or permission change. Actual GitHub visibility and write permissions have not
  been audited from this environment.
- **Implementation/origin:** direct owner administrative request on 2026-11-11
  accepting the single-repository organization. Root `.gitignore`, `.gitattributes`,
  `.devcontainer/devcontainer.json`, and `AGENTS.md` describe the actual setup.
  This is repository administration and creates no game development day.
- **Verification:** a fresh local clone recovered the original history, agent
  framework, Unity source files and `.meta` contents, and the LFS-managed image.
  LFS integrity checks passed. Local caches and tool backups were absent from
  the clone; the owner's settings edit remained unchanged and unstaged in the
  original working tree.
- **Technical references:** Git's
  [attribute macro rules](https://git-scm.com/docs/gitattributes#_defining_macro_attributes)
  and the official
  [Git LFS devcontainer feature](https://github.com/devcontainers/features/tree/main/src/git-lfs).
