// ARCH-001.a, ARCH-001.d | 2026-11-10 | First-implementation checks for keys and the registry.
// Journal: .agents/days/2026-11-12.md
using System;
using NUnit.Framework;
using TomorrowAtEight.Content;
using TomorrowAtEight.Presentation;
using UnityEngine;

namespace TomorrowAtEight.Tests
{
    public sealed class ContentRegistryTests
    {
        static readonly ContentKey<Mesh> MeshKey = new ContentKey<Mesh>("mesh.unit.box");
        static readonly ContentKey<Material> MaterialKey = new ContentKey<Material>("material.unit.box");
        static readonly ContentKey<string> DataA = new ContentKey<string>("data.unit.a");
        static readonly ContentKey<string> DataB = new ContentKey<string>("data.unit.b");

        static ContentRegistry NewRegistry() => new ContentRegistry(null, UnityObjects.Destroy);

        [Test]
        public void Keys_RejectBadSyntaxUnknownKindAndWrongType()
        {
            Assert.Throws<ArgumentException>(() => new ContentKey<Mesh>("mesh.core"));
            Assert.Throws<ArgumentException>(() => new ContentKey<Mesh>("Mesh.core.cube"));
            Assert.Throws<ArgumentException>(() => new ContentKey<Mesh>("mesh.core.-cube"));
            Assert.Throws<ArgumentException>(() => new ContentKey<Mesh>("widget.core.cube"));
            Assert.Throws<ArgumentException>(() => new ContentKey<Material>("mesh.core.cube"));
            Assert.DoesNotThrow(() => new ContentKey<Mesh>("mesh.core.crate.body_2"));
            Assert.DoesNotThrow(() => new ContentKey<int>("data.days.2026-11-12"));
        }

        [Test]
        public void Resolve_SharesOneResultPerKey_AndDisposeReleasesGeneratedObjects()
        {
            ContentRegistry registry = NewRegistry();
            int created = 0;
            registry.Register(MeshKey, ContentSource.Procedural, "r1", _ =>
            {
                created++;
                return new Mesh { name = "unit" };
            });
            registry.ValidateAndSeal();

            Mesh first = registry.Resolve(MeshKey);
            Mesh second = registry.Resolve(MeshKey);
            Assert.AreSame(first, second);
            Assert.AreEqual(1, created);
            Assert.AreEqual(1, registry.OwnedObjectCount);

            registry.Dispose();
            Assert.IsTrue(first == null, "Generated mesh should be destroyed with the session.");
        }

        [Test]
        public void Seal_ReportsDuplicateKeys()
        {
            ContentRegistry registry = NewRegistry();
            registry.Register(DataA, ContentSource.Definition, "r1", _ => "one");
            registry.Register(DataA, ContentSource.Definition, "r1", _ => "two");
            var exception = Assert.Throws<ContentException>(registry.ValidateAndSeal);
            StringAssert.Contains("Duplicate content key 'data.unit.a'", exception.Message);
        }

        [Test]
        public void Seal_ReportsUnknownDependencyWithChain()
        {
            ContentRegistry registry = NewRegistry();
            registry.Register(DataA, ContentSource.Definition, "r1", _ => "one", DataB);
            var exception = Assert.Throws<ContentException>(registry.ValidateAndSeal);
            StringAssert.Contains("data.unit.a -> data.unit.b", exception.Message);
        }

        [Test]
        public void Seal_ReportsDependencyCycleWithChain()
        {
            ContentRegistry registry = NewRegistry();
            registry.Register(DataA, ContentSource.Definition, "r1", _ => "one", DataB);
            registry.Register(DataB, ContentSource.Definition, "r1", _ => "two", DataA);
            var exception = Assert.Throws<ContentException>(registry.ValidateAndSeal);
            StringAssert.Contains("Dependency cycle: data.unit.a -> data.unit.b -> data.unit.a", exception.Message);
        }

        [Test]
        public void Seal_FailsWhenImportedEntryHasNoCatalog()
        {
            ContentRegistry registry = NewRegistry();
            registry.RegisterImported(new ContentKey<Shader>("shader.unit.missing"), "r1");
            var exception = Assert.Throws<ContentException>(registry.ValidateAndSeal);
            StringAssert.Contains("no ImportedAssetCatalog", exception.Message);
        }

        [Test]
        public void Resolve_RejectsMissingKeyWrongTypeAndUndeclaredDependency()
        {
            ContentRegistry registry = NewRegistry();
            registry.Register(DataA, ContentSource.Definition, "r1", _ => "one");
            // Uses DataA without declaring it.
            registry.Register(DataB, ContentSource.Definition, "r1", resolver => resolver.Resolve(DataA) + "!");
            Assert.Throws<InvalidOperationException>(() => registry.Resolve(DataA), "Resolve before sealing must fail.");
            registry.ValidateAndSeal();

            Assert.Throws<ContentException>(() => registry.Resolve(new ContentKey<string>("data.unit.none")));
            Assert.Throws<ContentException>(() => registry.Resolve(new ContentKey<int>("data.unit.a")));
            var exception = Assert.Throws<ContentException>(() => registry.Resolve(DataB));
            StringAssert.Contains("without declaring it as a dependency", exception.Message);
        }

        [Test]
        public void ProviderReplacement_KeepsKeysAndCallers()
        {
            Func<Func<ContentRegistry, Mesh>, string> build = meshProvider =>
            {
                ContentRegistry registry = NewRegistry();
                registry.Register(MeshKey, ContentSource.Procedural, "r", meshProvider);
                registry.Register(MaterialKey, ContentSource.Definition, "r",
                    resolver => new Material(Shader.Find("Hidden/InternalErrorShader")) { name = resolver.Resolve(MeshKey).name },
                    MeshKey);
                registry.ValidateAndSeal();
                Material material = registry.Resolve(MaterialKey);
                string name = material.name;
                UnityEngine.Object.DestroyImmediate(material);
                registry.Dispose();
                return name;
            };

            Assert.AreEqual("first", build(_ => new Mesh { name = "first" }));
            Assert.AreEqual("second", build(_ => new Mesh { name = "second" }));
        }

        [Test]
        public void DependencyClosure_ListsEveryReferencedPart()
        {
            ContentRegistry registry = NewRegistry();
            registry.Register(MeshKey, ContentSource.Procedural, "r1", _ => new Mesh());
            registry.Register(DataA, ContentSource.Definition, "r1", _ => "one", MeshKey);
            registry.Register(DataB, ContentSource.Definition, "r1", _ => "two", DataA);
            registry.ValidateAndSeal();

            CollectionAssert.AreEquivalent(
                new[] { "data.unit.b", "data.unit.a", "mesh.unit.box" },
                registry.GetDependencyClosure(new[] { "data.unit.b" }));
            registry.Dispose();
        }
    }
}
