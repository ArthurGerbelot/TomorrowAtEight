# ARCH-001.d - Layout and initial validation

Part of [ARCH-001 - Unified content recipes](../ARCHITECTURE.md#arch-001---unified-content-recipes).
The parent record owns the shared status, dates, origin, and path conventions.

| Unity project path | Responsibility |
| --- | --- |
| `Assets/TomorrowAtEight/Runtime/Content/` | Keys, registry, shared asset resolution, recipe contracts. |
| `Assets/TomorrowAtEight/Runtime/Entities/` | Entity factory, prefab cache, common ECS components. |
| `Assets/TomorrowAtEight/Runtime/Bootstrap/` | Session initialization and shutdown (ARCH-002). |
| `Assets/TomorrowAtEight/Runtime/Presentation/` | ECS to Unity rendering, audio, and UI adapters (ARCH-005). |
| `Assets/TomorrowAtEight/Content/` | Content modules, named keys, and concrete definitions. |
| `Assets/TomorrowAtEight/Editor/Content/` | Import bindings, catalog generation, content validation. |
| `Assets/TomorrowAtEight/Editor/Automation/` | Daily action runner and reusable commands (ARCH-003). |
| `Assets/TomorrowAtEight/Data/` | Daily JSON (`Days/`) and the rights manifest (`Content/`). |
| `Assets/TomorrowAtEight/Tests/` | Edit-mode and play-mode checks. |

The runtime and content folders compile into one assembly, `TomorrowAtEight`
(`Assets/TomorrowAtEight/TomorrowAtEight.asmdef`), because the bootstrap calls
the content module list. Editor and test code have their own assemblies.

- The first implementation must exercise registration and validation, two
  independent instances of one recipe sharing their assets, deferred spawning,
  despawn/child cleanup, provider replacement retaining the same keys, and clean
  teardown/reinitialization. Verify missing keys, duplicates, wrong types, and
  dependency cycles produce useful failures.
- Status on 2026-11-12: these checks are written as tests in
  `Assets/TomorrowAtEight/Tests/` but have not been run; the agent's environment
  has no Unity Editor. Run them from Window > General > Test Runner.
- Verify the same minimal flow in the Unity Editor and a browser build before
  reporting the infrastructure as operational for the web target. Any test
  content remains provisional and establishes no genre or visual identity.
