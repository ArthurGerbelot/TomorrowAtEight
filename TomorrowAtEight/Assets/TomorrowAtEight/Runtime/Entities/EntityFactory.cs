// ARCH-001.b | 2026-11-10 | One factory per world and session creates all gameplay entities.
// Decision: .agents/architecture/ARCH-001.b-entity-factory.md
// Journal: .agents/days/2026-11-12.md
using System;
using System.Collections.Generic;
using TomorrowAtEight.Content;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TomorrowAtEight.Simulation
{
    /// <summary>
    /// Resolves entity recipes into world-local ECS prefabs once, then instantiates them.
    /// Main thread only. Systems use the static deferred helpers with handles resolved here.
    /// </summary>
    public sealed class EntityFactory : IDisposable
    {
        readonly EntityManager m_EntityManager;
        readonly ContentRegistry m_Registry;
        readonly Dictionary<string, Entity> m_Prefabs = new Dictionary<string, Entity>(StringComparer.Ordinal);
        readonly List<string> m_ModelIds = new List<string>();
        readonly EntityQuery m_InstanceQuery;
        readonly EntityQuery m_AllOwnedQuery;
        bool m_Disposed;

        public EntityFactory(EntityManager entityManager, ContentRegistry registry)
        {
            m_EntityManager = entityManager;
            m_Registry = registry;

            // Stable for the session: registration order of every view template key.
            IReadOnlyList<ContentEntry> entries = registry.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Kind == ContentKeyRules.Model || entries[i].Kind == ContentKeyRules.Vfx)
                    m_ModelIds.Add(entries[i].Id);
            }

            m_InstanceQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SessionOwned>()
                .Build(entityManager);
            m_AllOwnedQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SessionOwned>()
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .Build(entityManager);
        }

        /// <summary>View template keys by <see cref="ViewModel.ModelIndex"/>.</summary>
        public IReadOnlyList<string> ModelIds => m_ModelIds;

        public int PrefabCount => m_Prefabs.Count;

        /// <summary>Live session entities, excluding prefabs.</summary>
        public int CountInstances() => m_Disposed ? 0 : m_InstanceQuery.CalculateEntityCount();

        /// <summary>Every session entity, including prefabs and disabled entities.</summary>
        public int CountAllOwned() => m_Disposed ? 0 : m_AllOwnedQuery.CalculateEntityCount();

        /// <summary>Returns the world-local prefab for a recipe, building it on first use.</summary>
        public Entity ResolvePrefab(ContentKey<EntityRecipe> key)
        {
            ThrowIfDisposed();
            if (m_Prefabs.TryGetValue(key.Id, out Entity prefab))
                return prefab;

            var group = new List<Entity>();
            Entity root = BuildPrefabEntity(key, group, 0);
            if (group.Count > 1)
            {
                // The root must be the first element so instantiating or destroying it
                // carries the whole composed hierarchy.
                DynamicBuffer<LinkedEntityGroup> linked = m_EntityManager.AddBuffer<LinkedEntityGroup>(root);
                for (int i = 0; i < group.Count; i++)
                    linked.Add(new LinkedEntityGroup { Value = group[i] });
            }

            m_Prefabs.Add(key.Id, root);
            return root;
        }

        /// <summary>Instantiates the recipe now and returns a fresh entity.</summary>
        public Entity Spawn(ContentKey<EntityRecipe> key, SpawnArgs args)
        {
            Entity prefab = ResolvePrefab(key);
            Entity instance = m_EntityManager.Instantiate(prefab);
            LocalTransform transform = ToTransform(args);
            m_EntityManager.SetComponentData(instance, transform);
            m_EntityManager.SetComponentData(instance, new LocalToWorld { Value = transform.ToMatrix() });
            return instance;
        }

        /// <summary>Releases an instance and the children it owns.</summary>
        public void Despawn(Entity entity)
        {
            ThrowIfDisposed();
            if (m_EntityManager.Exists(entity))
                m_EntityManager.DestroyEntity(entity);
        }

        /// <summary>
        /// Deferred spawn for systems. The returned handle is only usable with the same
        /// command buffer until playback.
        /// </summary>
        public static Entity SpawnDeferred(EntityCommandBuffer commands, Entity prefab, SpawnArgs args)
        {
            Entity instance = commands.Instantiate(prefab);
            LocalTransform transform = ToTransform(args);
            commands.SetComponent(instance, transform);
            // Playback happens after this frame's transform update; give the root its
            // world matrix now so its view never appears at the origin for a frame.
            commands.SetComponent(instance, new LocalToWorld { Value = transform.ToMatrix() });
            return instance;
        }

        /// <summary>Deferred despawn for systems; children in the linked group go with it.</summary>
        public static void DespawnDeferred(EntityCommandBuffer commands, Entity entity)
        {
            commands.DestroyEntity(entity);
        }

        /// <summary>
        /// Infrastructure entity for system state or request singletons. It is tagged so
        /// session teardown releases it with everything else.
        /// </summary>
        public Entity CreateSessionEntity()
        {
            ThrowIfDisposed();
            Entity entity = m_EntityManager.CreateEntity();
            m_EntityManager.AddComponent<SessionOwned>(entity);
            return entity;
        }

        /// <summary>Destroys every session entity and prefab; the world itself is left alone.</summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_EntityManager.DestroyEntity(m_AllOwnedQuery);
            m_Prefabs.Clear();
        }

        Entity BuildPrefabEntity(ContentKey<EntityRecipe> key, List<Entity> group, int depth)
        {
            if (depth > 16)
                throw new ContentException($"Entity recipe '{key}' nests children deeper than 16 levels.");

            EntityRecipe recipe = m_Registry.Resolve(key);
            m_Registry.TryGetEntry(key.Id, out ContentEntry entry);

            Entity entity = m_EntityManager.CreateEntity();
            group.Add(entity);
            m_EntityManager.AddComponent<Prefab>(entity);
            m_EntityManager.AddComponent<SessionOwned>(entity);
            m_EntityManager.AddComponentData(entity, LocalTransform.Identity);
            // A zero matrix keeps a view hidden until the transform system (or Spawn)
            // writes the real world matrix, which matters for children spawned late in a frame.
            m_EntityManager.AddComponentData(entity, new LocalToWorld { Value = float4x4.zero });

            if (recipe.Model.HasValue)
            {
                string modelId = recipe.Model.Value.Id;
                RequireDeclared(entry, modelId);
                int index = m_ModelIds.IndexOf(modelId);
                if (index < 0)
                    throw new ContentException($"Entity recipe '{key}' uses unregistered model '{modelId}'.");
                m_EntityManager.AddComponentData(entity, new ViewModel { ModelIndex = index });
            }

            recipe.AddComponents?.Invoke(m_EntityManager, entity);

            for (int i = 0; i < recipe.Children.Count; i++)
            {
                EntityRecipeChild child = recipe.Children[i];
                RequireDeclared(entry, child.Recipe.Id);
                Entity childEntity = BuildPrefabEntity(child.Recipe, group, depth + 1);
                m_EntityManager.AddComponentData(childEntity, new Parent { Value = entity });
                m_EntityManager.SetComponentData(childEntity, ToTransform(
                    SpawnArgs.At(child.Position, child.Rotation, child.Scale)));
            }

            return entity;
        }

        static void RequireDeclared(ContentEntry entry, string dependencyId)
        {
            for (int i = 0; i < entry.Dependencies.Count; i++)
            {
                if (string.Equals(entry.Dependencies[i], dependencyId, StringComparison.Ordinal))
                    return;
            }
            // Undeclared parts would be invisible to rights validation (ARCH-001.c).
            throw new ContentException($"'{entry.Id}' uses '{dependencyId}' without declaring it as a dependency.");
        }

        static LocalTransform ToTransform(SpawnArgs args)
        {
            // default(SpawnArgs) has a zero quaternion and zero scale; treat both as identity.
            quaternion rotation = math.lengthsq(args.Rotation.value) < 1e-8f ? quaternion.identity : args.Rotation;
            float scale = args.Scale <= 0f ? 1f : args.Scale;
            return LocalTransform.FromPositionRotationScale(args.Position, rotation, scale);
        }

        void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(EntityFactory));
        }
    }
}
