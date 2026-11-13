# ARCH-001.b - Entity factory

Part of [ARCH-001 - Unified content recipes](../ARCHITECTURE.md#arch-001---unified-content-recipes).
The parent record owns the shared status, dates, origin, and path conventions.

- An `EntityRecipe` declares default ECS components, child entity composition,
  dependencies, and an optional model/presentation key. Visual recipes compose
  mesh, material, texture, VFX, and SFX keys instead of embedding unrelated copies.
  Replacing `texture.core.crate.body.albedo`, for example, retains the keys of its
  material, model, and entity definition.
- One `EntityFactory` belongs to one ECS world and content session. It resolves
  each entity recipe into an ECS prefab once per world, using `Prefab` and
  `LinkedEntityGroup` for composed hierarchies where needed. The cache is not shared
  across worlds or retained between Play Mode sessions.
- Planned main-thread API: `ResolvePrefab(key)` returns the world-local prefab;
  `Spawn(key, SpawnArgs)` instantiates it and returns a fresh `Entity`;
  `Despawn(entity)` releases the instance and its owned children. `SpawnArgs`
  initially contains position, rotation, and uniform scale. Feature-specific
  initialization uses explicit typed component data, not an untyped property bag.
- During simulation, systems use the factory's deferred spawn/despawn helpers
  with an `EntityCommandBuffer` and previously resolved prefab handles. They do
  not perform managed registry lookups or structural changes inside jobs. A
  deferred entity handle is usable only in its command buffer until playback.
- Gameplay entity creation goes through this factory contract. Low-level ECS
  setup for system state, request buffers, and prefabs remains an infrastructure
  responsibility. Models and GameObjects are presentation, not a parallel store
  of authoritative gameplay state (ARCH-005).
