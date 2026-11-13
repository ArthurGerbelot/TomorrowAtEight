// ARCH-001, ARCH-002, ARCH-005 | 2026-11-10 | Provisional test scenes that exercise the
// shared foundation. They establish no genre, world, or visual direction (GAME.md).
// Decision: .agents/ARCHITECTURE.md#arch-001---unified-content-recipes
// Journal: .agents/days/2026-11-12.md
using TomorrowAtEight.Bootstrap;
using TomorrowAtEight.Content;
using TomorrowAtEight.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TomorrowAtEight.Definitions
{
    public static class TestContent
    {
        public static readonly ContentKey<Material> AmberMaterial = new ContentKey<Material>("material.test.amber");
        public static readonly ContentKey<Material> TealMaterial = new ContentKey<Material>("material.test.teal");
        public static readonly ContentKey<Material> RoseMaterial = new ContentKey<Material>("material.test.rose");
        public static readonly ContentKey<Material> SlateMaterial = new ContentKey<Material>("material.test.slate");

        public static readonly ContentKey<GameObject> AmberBlockModel = new ContentKey<GameObject>("model.test.block-amber");
        public static readonly ContentKey<GameObject> TealBlockModel = new ContentKey<GameObject>("model.test.block-teal");
        public static readonly ContentKey<GameObject> GemModel = new ContentKey<GameObject>("model.test.gem");
        public static readonly ContentKey<GameObject> GroundModel = new ContentKey<GameObject>("model.test.ground");

        public static readonly ContentKey<EntityRecipe> Ground = new ContentKey<EntityRecipe>("entity.test.ground");
        public static readonly ContentKey<EntityRecipe> Spinner = new ContentKey<EntityRecipe>("entity.test.spinner");
        public static readonly ContentKey<EntityRecipe> SpinnerAlt = new ContentKey<EntityRecipe>("entity.test.spinner-alt");
        public static readonly ContentKey<EntityRecipe> Satellite = new ContentKey<EntityRecipe>("entity.test.satellite");
        public static readonly ContentKey<EntityRecipe> Drifter = new ContentKey<EntityRecipe>("entity.test.drifter");
        public static readonly ContentKey<EntityRecipe> EmitterEntity = new ContentKey<EntityRecipe>("entity.test.emitter");
        public static readonly ContentKey<EntityRecipe> PawnEntity = new ContentKey<EntityRecipe>("entity.test.pawn");
        public static readonly ContentKey<EntityRecipe> Marker = new ContentKey<EntityRecipe>("entity.test.marker");

        public static readonly ContentKey<SceneRecipe> SharedAssetsScene = new ContentKey<SceneRecipe>("scene.test.shared-assets");
        public static readonly ContentKey<SceneRecipe> SpawnLifecycleScene = new ContentKey<SceneRecipe>("scene.test.spawn-lifecycle");
        public static readonly ContentKey<SceneRecipe> InputCommandsScene = new ContentKey<SceneRecipe>("scene.test.input-commands");

        const float GroundSize = 14f;

        public static void Register(ContentRegistration registration)
        {
            ContentRegistry registry = registration.Registry;

            RegisterMaterial(registry, AmberMaterial, new Color(0.93f, 0.66f, 0.22f));
            RegisterMaterial(registry, TealMaterial, new Color(0.20f, 0.66f, 0.68f));
            RegisterMaterial(registry, RoseMaterial, new Color(0.86f, 0.36f, 0.50f));
            RegisterMaterial(registry, SlateMaterial, new Color(0.24f, 0.27f, 0.33f));

            // Two models share one mesh; each part keeps its own key and rights record.
            RegisterModel(registry, AmberBlockModel, CoreContent.CubeMesh, AmberMaterial);
            RegisterModel(registry, TealBlockModel, CoreContent.CubeMesh, TealMaterial);
            RegisterModel(registry, GemModel, CoreContent.OctahedronMesh, RoseMaterial);
            RegisterModel(registry, GroundModel, CoreContent.PlaneMesh, SlateMaterial);

            registry.Register(Ground, ContentSource.Definition, "code-1",
                _ => new EntityRecipe(GroundModel),
                GroundModel);

            registry.Register(Spinner, ContentSource.Definition, "code-1",
                _ => new EntityRecipe(AmberBlockModel, AddSpin),
                AmberBlockModel);

            registry.Register(SpinnerAlt, ContentSource.Definition, "code-1",
                _ => new EntityRecipe(TealBlockModel, AddSpin),
                TealBlockModel);

            registry.Register(Satellite, ContentSource.Definition, "code-1",
                _ => new EntityRecipe(TealBlockModel),
                TealBlockModel);

            // Composed hierarchy: the satellite is a child entity in the linked group.
            registry.Register(Drifter, ContentSource.Definition, "code-1",
                _ => new EntityRecipe(GemModel,
                    (manager, entity) =>
                    {
                        AddSpin(manager, entity);
                        manager.AddComponentData(entity, new Drift());
                        manager.AddComponentData(entity, new Lifetime { Remaining = 4f });
                    },
                    new EntityRecipeChild(Satellite, new float3(0.95f, 0f, 0f), 0.32f)),
                GemModel, Satellite);

            registry.Register(EmitterEntity, ContentSource.Definition, "code-1",
                _ => new EntityRecipe(null,
                    (manager, entity) => manager.AddComponentData(entity, new Emitter())));

            registry.Register(PawnEntity, ContentSource.Definition, "code-1",
                _ => new EntityRecipe(AmberBlockModel,
                    (manager, entity) => manager.AddComponentData(entity, new Pawn { Speed = 5f }),
                    new EntityRecipeChild(Satellite, new float3(0f, 0.1f, 0.62f), 0.36f)),
                AmberBlockModel, Satellite);

            registry.Register(Marker, ContentSource.Definition, "code-1",
                _ => new EntityRecipe(GemModel,
                    (manager, entity) =>
                    {
                        AddSpin(manager, entity);
                        manager.AddComponent<TargetMarker>(entity);
                    }),
                GemModel);

            // Launchable scenes: this call registers the recipe and adds the launcher row.
            registration.RegisterLauncherScene("Run Test Scene 1", SharedAssetsScene, "code-1",
                _ => new SceneRecipe(
                    "Test Scene 1 - Shared assets",
                    "Twelve independent entities are spawned from two recipes. Both recipes reuse one cube " +
                    "mesh, and every instance of a recipe shares its material. Each instance spins with its own ECS data.",
                    StageSettings.Default, true, BuildSharedAssets),
                Ground, Spinner, SpinnerAlt);

            registration.RegisterLauncherScene("Run Test Scene 2", SpawnLifecycleScene, "code-1",
                _ => new SceneRecipe(
                    "Test Scene 2 - Deferred spawn and despawn",
                    "An emitter system spawns composed entities through a command buffer. Each one drifts, " +
                    "then is despawned with its child when its lifetime ends, so counts stay bounded.",
                    StageSettings.Default, true, BuildSpawnLifecycle),
                Ground, EmitterEntity, Drifter);

            registration.RegisterLauncherScene("Run Test Scene 3", InputCommandsScene, "code-1",
                _ => new SceneRecipe(
                    "Test Scene 3 - Input commands",
                    "Click or tap the ground, or hold WASD / arrow keys. An input adapter writes a command; " +
                    "a fixed-step ECS system moves the block. The view only follows ECS state.",
                    StageSettings.Default, true, BuildInputCommands),
                Ground, PawnEntity, Marker);
        }

        static void BuildSharedAssets(SceneBuildContext context)
        {
            SpawnGround(context);

            const int count = 12;
            for (int i = 0; i < count; i++)
            {
                float angle = i * (math.PI * 2f / count);
                var position = new float3(math.cos(angle) * 4.2f, 0.75f, math.sin(angle) * 4.2f);
                float scale = 0.7f + 0.3f * (i % 3);
                ContentKey<EntityRecipe> recipe = i % 2 == 0 ? Spinner : SpinnerAlt;

                Entity entity = context.Factory.Spawn(recipe, SpawnArgs.At(position, quaternion.identity, scale));
                // Per-instance state is typed component data, not a property bag.
                context.EntityManager.SetComponentData(entity, new Spin
                {
                    Axis = math.normalize(new float3(0.2f * (i % 4), 1f, 0.15f * (i % 3))),
                    RadiansPerSecond = 0.6f + 0.25f * i,
                });
            }
        }

        static void BuildSpawnLifecycle(SceneBuildContext context)
        {
            SpawnGround(context);

            // Systems receive a resolved prefab handle; they never look up content keys.
            Entity prefab = context.Factory.ResolvePrefab(Drifter);
            Entity emitter = context.Factory.Spawn(EmitterEntity, SpawnArgs.At(new float3(0f, 0.9f, 0f)));
            context.EntityManager.SetComponentData(emitter, new Emitter
            {
                Prefab = prefab,
                Interval = 0.3f,
                Timer = 0.3f,
                SpawnLifetime = 4f,
                Rng = Unity.Mathematics.Random.CreateFromIndex(20261112u),
            });
        }

        static void BuildInputCommands(SceneBuildContext context)
        {
            SpawnGround(context);
            context.Factory.Spawn(PawnEntity, SpawnArgs.At(new float3(0f, 0.5f, 0f)));
            context.Factory.Spawn(Marker, SpawnArgs.At(new float3(0f, -100f, 0f), quaternion.identity, 0.5f));
            context.CreateSingleton(new MoveCommand());
        }

        static void SpawnGround(SceneBuildContext context)
        {
            context.Factory.Spawn(Ground, SpawnArgs.At(float3.zero, quaternion.identity, GroundSize));
        }

        static void AddSpin(EntityManager manager, Entity entity)
        {
            manager.AddComponentData(entity, new Spin { Axis = math.up(), RadiansPerSecond = 1.5f });
        }

        static void RegisterMaterial(ContentRegistry registry, ContentKey<Material> key, Color color)
        {
            registry.Register(key, ContentSource.Procedural, "procedural-1",
                resolver =>
                {
                    var material = new Material(resolver.Resolve(CoreContent.LitShader)) { name = key.Id };
                    material.SetColor("_BaseColor", color);
                    material.SetFloat("_Smoothness", 0.2f);
                    return material;
                },
                CoreContent.LitShader);
        }

        static void RegisterModel(
            ContentRegistry registry,
            ContentKey<GameObject> key,
            ContentKey<Mesh> mesh,
            ContentKey<Material> material)
        {
            registry.Register(key, ContentSource.Procedural, "procedural-1",
                resolver =>
                {
                    // Inactive template without gameplay logic; the bridge instantiates views.
                    var template = new GameObject(key.Id);
                    template.SetActive(false);
                    template.AddComponent<MeshFilter>().sharedMesh = resolver.Resolve(mesh);
                    MeshRenderer renderer = template.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = resolver.Resolve(material);
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    return template;
                },
                mesh, material);
        }
    }
}
