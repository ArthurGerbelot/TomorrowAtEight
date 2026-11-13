// ARCH-001.b, ARCH-001.d | 2026-11-10 | Entity factory checks in an isolated test world.
// Journal: .agents/days/2026-11-12.md
using NUnit.Framework;
using TomorrowAtEight.Content;
using TomorrowAtEight.Definitions;
using TomorrowAtEight.Presentation;
using TomorrowAtEight.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TomorrowAtEight.Tests
{
    public sealed class EntityFactoryTests
    {
        static readonly ContentKey<EntityRecipe> ChildKey = new ContentKey<EntityRecipe>("entity.unit.child");
        static readonly ContentKey<EntityRecipe> ParentKey = new ContentKey<EntityRecipe>("entity.unit.parent");

        World m_World;
        ContentRegistry m_Registry;
        EntityFactory m_Factory;

        [SetUp]
        public void SetUp()
        {
            m_World = new World("EntityFactoryTests");
            m_Registry = new ContentRegistry(null, UnityObjects.Destroy);
            m_Registry.Register(ChildKey, ContentSource.Definition, "r1", _ => new EntityRecipe());
            m_Registry.Register(ParentKey, ContentSource.Definition, "r1",
                _ => new EntityRecipe(null,
                    (manager, entity) => manager.AddComponentData(entity, new Lifetime { Remaining = 2f }),
                    new EntityRecipeChild(ChildKey, new float3(1f, 0f, 0f), 0.5f)),
                ChildKey);
            m_Registry.ValidateAndSeal();
            m_Factory = new EntityFactory(m_World.EntityManager, m_Registry);
        }

        [TearDown]
        public void TearDown()
        {
            m_Factory.Dispose();
            m_Registry.Dispose();
            m_World.Dispose();
        }

        [Test]
        public void TwoInstances_AreIndependent_AndShareOnePrefab()
        {
            EntityManager manager = m_World.EntityManager;
            Entity first = m_Factory.Spawn(ParentKey, SpawnArgs.At(new float3(1f, 2f, 3f)));
            Entity second = m_Factory.Spawn(ParentKey, SpawnArgs.At(new float3(-1f, 0f, 0f), quaternion.identity, 2f));

            Assert.AreNotEqual(first, second);
            Assert.AreEqual(1, m_Factory.PrefabCount);
            Assert.AreEqual(new float3(1f, 2f, 3f), manager.GetComponentData<LocalTransform>(first).Position);
            Assert.AreEqual(2f, manager.GetComponentData<LocalTransform>(second).Scale);

            manager.SetComponentData(first, new Lifetime { Remaining = 9f });
            Assert.AreEqual(2f, manager.GetComponentData<Lifetime>(second).Remaining);

            // Two roots plus one child each; prefabs are not counted as instances.
            Assert.AreEqual(4, m_Factory.CountInstances());
            Assert.AreEqual(6, m_Factory.CountAllOwned());
        }

        [Test]
        public void Despawn_RemovesOwnedChildren()
        {
            Entity instance = m_Factory.Spawn(ParentKey, SpawnArgs.At(float3.zero));
            Assert.AreEqual(2, m_Factory.CountInstances());
            m_Factory.Despawn(instance);
            Assert.AreEqual(0, m_Factory.CountInstances());
        }

        [Test]
        public void DeferredSpawnAndDespawn_ApplyAtPlayback()
        {
            EntityManager manager = m_World.EntityManager;
            Entity prefab = m_Factory.ResolvePrefab(ParentKey);

            var spawnCommands = new EntityCommandBuffer(Allocator.Temp);
            Entity deferred = EntityFactory.SpawnDeferred(spawnCommands, prefab, SpawnArgs.At(new float3(4f, 0f, 0f)));
            spawnCommands.SetComponent(deferred, new Lifetime { Remaining = 7f });
            Assert.AreEqual(0, m_Factory.CountInstances(), "Nothing exists before playback.");
            spawnCommands.Playback(manager);
            spawnCommands.Dispose();
            Assert.AreEqual(2, m_Factory.CountInstances());

            EntityQuery roots = manager.CreateEntityQuery(typeof(Lifetime));
            NativeArray<Entity> entities = roots.ToEntityArray(Allocator.Temp);
            Assert.AreEqual(1, entities.Length);
            Assert.AreEqual(7f, manager.GetComponentData<Lifetime>(entities[0]).Remaining);

            var despawnCommands = new EntityCommandBuffer(Allocator.Temp);
            EntityFactory.DespawnDeferred(despawnCommands, entities[0]);
            despawnCommands.Playback(manager);
            despawnCommands.Dispose();
            entities.Dispose();
            Assert.AreEqual(0, m_Factory.CountInstances());
        }

        [Test]
        public void Dispose_LeavesNoSessionEntities_AndANewFactoryStartsClean()
        {
            EntityManager manager = m_World.EntityManager;
            m_Factory.Spawn(ParentKey, SpawnArgs.At(float3.zero));
            m_Factory.CreateSessionEntity();
            m_Factory.Dispose();

            EntityQuery owned = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SessionOwned>()
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .Build(manager);
            Assert.AreEqual(0, owned.CalculateEntityCount());

            m_Factory = new EntityFactory(manager, m_Registry);
            m_Factory.Spawn(ParentKey, SpawnArgs.At(float3.zero));
            Assert.AreEqual(2, m_Factory.CountInstances());
            Assert.AreEqual(1, m_Factory.PrefabCount);
        }

        [Test]
        public void UndeclaredChild_FailsWithUsefulMessage()
        {
            var registry = new ContentRegistry(null, UnityObjects.Destroy);
            var orphanParent = new ContentKey<EntityRecipe>("entity.unit.undeclared");
            registry.Register(ChildKey, ContentSource.Definition, "r1", _ => new EntityRecipe());
            registry.Register(orphanParent, ContentSource.Definition, "r1",
                _ => new EntityRecipe(null, null, new EntityRecipeChild(ChildKey, float3.zero)));
            registry.ValidateAndSeal();

            var factory = new EntityFactory(m_World.EntityManager, registry);
            var exception = Assert.Throws<ContentException>(() => factory.ResolvePrefab(orphanParent));
            StringAssert.Contains("without declaring it as a dependency", exception.Message);
            factory.Dispose();
            registry.Dispose();
        }
    }
}
