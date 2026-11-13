# ARCH-001.a - Keys and content registry

Part of [ARCH-001 - Unified content recipes](../ARCHITECTURE.md#arch-001---unified-content-recipes).
The parent record owns the shared status, dates, origin, and path conventions.

## Keys and identity

- A public content key is a case-sensitive, lowercase ASCII string following
  `<kind>.<namespace>.<name>[.<part>...]`. Segments contain letters, digits,
  underscores, or hyphens, and start with a letter or digit. Initial kinds are
  `entity`, `model`, `mesh`, `material`, `texture`, `shader`, `vfx`, `sfx`, `scene`,
  and `data`. Kind `font` (result `Font`) was added on 2026-11-12 for UI text.
  The namespace identifies a content family, not a date or contributor account.
- Examples: `entity.core.crate`, `model.core.crate`, `mesh.core.crate.body`,
  `material.core.crate.body`, `texture.core.crate.body.albedo`, and `sfx.core.hit`.
  These are naming examples, not adopted game content.
- `ContentKey<T>` wraps the canonical string and its expected result type.
  Definitions expose named key constants; gameplay code uses those constants.
  External text, including prompt references and daily JSON, uses the canonical
  string and is validated at the boundary. A path, Unity asset GUID, display name,
  or C# class name is not a public content key.
- Initial result contracts are `EntityRecipe` for `entity`, `GameObject` view
  templates for `model` and `vfx`, `Mesh` for `mesh`, `Material` for `material`,
  `Texture` for `texture`, `Shader` for `shader`, `AudioClip` for `sfx`, and
  `SceneRecipe` for `scene`. `data` keys declare their specific immutable data
  type. Each registration must match its key's kind and result contract.
- Keys identify content definitions, not spawned instances. A Unity `Entity` is a
  world-local runtime handle. Neither its index nor an internal catalog index is
  a persistent ID. Resolve text keys into prefab handles or compact catalog indices
  before passing data to ECS systems; keep managed strings out of component data.
- Preserve a key when changing its provider or appearance. Renaming/removing a
  published key requires a documented migration or explicit alias. Reject duplicate
  canonical keys; registration order never silently selects an override.

## Registry and asset lifetime

- A planned `ContentCatalog.RegisterAll(registry)` invokes explicit content modules
  in a documented order. Each module registers its own definitions and keys.
  New content normally extends a module; a new module adds one catalog call.
  Registration happens automatically during bootstrap, without runtime assembly
  scanning, reflection-based discovery, or scene searches.
- Each entry declares its key, result type, dependency keys, and provider. Planned
  operations are `Register<T>`, `ValidateAndSeal`, and `Resolve<T>`. Sealing checks
  duplicate keys, unknown dependencies, type mismatches, and dependency cycles
  before any game content spawns. Report the key and dependency chain on failure;
  stop initialization rather than silently substituting missing content.
- `Resolve<T>` lazily creates or loads one shared result per key per content session.
  Meshes, textures, materials, clips, and immutable recipe definitions are shared.
  Resolving an entity recipe returns its definition; it does not spawn an entity.
  Creating multiple instances is the responsibility of the entity factory or
  presentation bridge. Registry access and Unity object creation stay on the main
  thread; jobs receive resolved unmanaged data and handles.
- Procedural providers are the initial source. Imported assets use the same keys
  and declared types. An editor-only import map binds key constants to source
  assets; an ARCH-003 command writes their serialized references into a generated
  `ImportedAssetCatalog` at
  `Assets/TomorrowAtEight/Resources/TomorrowAtEight/ImportedAssets.asset`.
  The runtime loads this single catalog through a fixed Resources entry point.
  Missing catalogs/references fail validation when imported entries are declared.
  Runtime callers never use AssetDatabase or arbitrary asset-path lookups.
- Serialized references retain imported assets in builds. Asset paths and GUIDs
  remain editor binding details; replacing a procedural provider with an import
  preserves public keys and callers. Use ordinary Unity references first; a later
  Addressables provider may extend the same contract if needed.
- Model and VFX results are templates; presentation creates their live instances.
  Procedural templates stay inactive and contain no independent gameplay logic.
  Procedural materials declare their shader dependency; include shader references
  in the generated asset catalog rather than relying on an unreferenced shader
  name being available after build stripping.
- A content session owns the Unity objects it generates and disposes them at
  shutdown, after entity views and world-local prefabs are released. Imported
  source assets are borrowed. Despawning an entity destroys its instance/view,
  not shared meshes, materials, textures, or clips. Avoid mutating shared materials
  for per-instance effects; use per-instance rendering properties instead.
