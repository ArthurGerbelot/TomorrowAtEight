// ARCH-001.c | 2026-11-12 | Initial verification of rights and replacement tracking.
// Journal: .agents/days/2026-11-12.md
using System.Collections.Generic;
using NUnit.Framework;
using TomorrowAtEight.Content;
using TomorrowAtEight.EditorTools;
using TomorrowAtEight.Presentation;
using UnityEditor;
using UnityEngine;

namespace TomorrowAtEight.Tests
{
    public sealed class AssetRightsValidatorTests
    {
        static readonly ContentKey<Mesh> MeshKey = new ContentKey<Mesh>("mesh.unit.box");
        static readonly ContentKey<Material> MaterialKey = new ContentKey<Material>("material.unit.box");
        static readonly ContentKey<GameObject> ModelKey = new ContentKey<GameObject>("model.unit.box");

        static ContentRegistry NewRegistry(string meshRevision = "r1")
        {
            var registry = new ContentRegistry(null, UnityObjects.Destroy);
            registry.Register(MeshKey, ContentSource.Procedural, meshRevision, _ => new Mesh());
            registry.Register(MaterialKey, ContentSource.Procedural, "r1", _ => null);
            registry.Register(ModelKey, ContentSource.Procedural, "r1", _ => null, MeshKey, MaterialKey);
            registry.ValidateAndSeal();
            return registry;
        }

        static AssetMetadataEntry Record(string key, string status, params string[] scopes)
        {
            return new AssetMetadataEntry
            {
                contentKey = key,
                providerRevision = "r1",
                ipStatus = status,
                ipStatusReason = "test basis",
                allowedScopes = new List<string>(scopes),
            };
        }

        static AssetMetadataManifest Manifest(params AssetMetadataEntry[] entries)
        {
            return new AssetMetadataManifest { schemaVersion = 1, entries = new List<AssetMetadataEntry>(entries) };
        }

        static RightsReport Run(ContentRegistry registry, AssetMetadataManifest manifest, string scope)
        {
            return AssetRightsValidator.Validate(registry, manifest, new[] { ModelKey.Id }, scope);
        }

        [Test]
        public void EveryPartIsChecked_AParentStatusIsNotInherited()
        {
            ContentRegistry registry = NewRegistry();
            RightsReport report = Run(registry,
                Manifest(Record(ModelKey.Id, "cleared", AssetRightsValidator.PublicBuild)),
                AssetRightsValidator.PublicBuild);

            Assert.AreEqual(3, report.CheckedAssets);
            Assert.AreEqual(2, report.Count(AssetRightsValidator.CodeMissing));
            Assert.IsTrue(report.HasBlockingIssues);
        }

        [Test]
        public void StaleRevision_BlocksUntilReassessed_AndReplacementKeepsItsKey()
        {
            AssetMetadataManifest manifest = Manifest(
                Record(ModelKey.Id, "cleared", AssetRightsValidator.PublicBuild),
                Record(MeshKey.Id, "cleared", AssetRightsValidator.PublicBuild),
                Record(MaterialKey.Id, "cleared", AssetRightsValidator.PublicBuild));

            Assert.IsFalse(Run(NewRegistry(), manifest, AssetRightsValidator.PublicBuild).HasBlockingIssues);

            // Same key, new implementation: the old clearance must not carry over.
            RightsReport replaced = Run(NewRegistry("r2"), manifest, AssetRightsValidator.PublicBuild);
            Assert.AreEqual(1, replaced.Count(AssetRightsValidator.CodeStale));
            Assert.IsTrue(replaced.HasBlockingIssues);
        }

        [Test]
        public void ClearedPlaceholderAwaitingReplacement_IsReportedButNotBlocked()
        {
            AssetMetadataEntry mesh = Record(MeshKey.Id, "cleared", AssetRightsValidator.PublicBuild);
            mesh.replacementRequired = true;
            mesh.replacementReason = "placeholder";
            RightsReport report = Run(NewRegistry(),
                Manifest(Record(ModelKey.Id, "cleared", AssetRightsValidator.PublicBuild), mesh,
                    Record(MaterialKey.Id, "not_required", AssetRightsValidator.PublicBuild)),
                AssetRightsValidator.PublicBuild);

            Assert.IsFalse(report.HasBlockingIssues);
            Assert.AreEqual(1, report.Count(AssetRightsValidator.CodeReplacement));
        }

        [Test]
        public void TemporaryPermission_HoldsForItsScopeOnly_AndPendingAloneAllowsNothing()
        {
            AssetMetadataManifest manifest = Manifest(
                Record(ModelKey.Id, "cleared", AssetRightsValidator.PrivateDevelopment, AssetRightsValidator.PublicBuild),
                Record(MeshKey.Id, "pending", AssetRightsValidator.PrivateDevelopment),
                Record(MaterialKey.Id, "pending"));

            RightsReport development = Run(NewRegistry(), manifest, AssetRightsValidator.PrivateDevelopment);
            Assert.AreEqual(1, development.Count(AssetRightsValidator.CodePending), "Mesh is usable temporarily.");
            Assert.AreEqual(1, development.Count(AssetRightsValidator.CodeScope), "Material has no permitted scope.");

            RightsReport build = Run(NewRegistry(), manifest, AssetRightsValidator.PublicBuild);
            Assert.AreEqual(2, build.Count(AssetRightsValidator.CodeScope));
            Assert.IsTrue(build.HasBlockingIssues);
        }

        [Test]
        public void BlockedAndUnknownStatuses_AlwaysBlock()
        {
            RightsReport report = Run(NewRegistry(),
                Manifest(
                    Record(ModelKey.Id, "blocked", AssetRightsValidator.PublicBuild),
                    Record(MeshKey.Id, "maybe", AssetRightsValidator.PublicBuild),
                    Record(MaterialKey.Id, "cleared", AssetRightsValidator.PublicBuild)),
                AssetRightsValidator.PublicBuild);

            Assert.AreEqual(1, report.Count(AssetRightsValidator.CodeBlocked));
            Assert.AreEqual(1, report.Count(AssetRightsValidator.CodeUnknownStatus));
        }

        [Test]
        public void ProjectManifest_CoversRegisteredContentForEveryScope()
        {
            foreach (string scope in new[]
            {
                AssetRightsValidator.PrivateDevelopment, AssetRightsValidator.PublicSource,
                AssetRightsValidator.PublicBuild, AssetRightsValidator.PublicMedia,
            })
            {
                RightsReport report = ProjectCommands.ValidateRights(scope);
                Assert.IsFalse(report.HasBlockingIssues, report.ToText());
                Assert.AreEqual(0, report.Count(AssetRightsValidator.CodeOrphan), report.ToText());
                Assert.Greater(report.CheckedAssets, 0);
            }
        }

        [Test]
        public void DailyRecords_LoadThroughTheGeneratedCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ImportedAssetCatalog>(ImportedAssetCatalogBuilder.CatalogPath);
            Assert.IsNotNull(catalog, "Run TomorrowAtEight/Run/Rebuild Imported Asset Catalog.");
            DailyRecordSet days = DailyRecordSet.Parse(catalog.dailyRecords);
            CollectionAssert.IsEmpty(days.Problems);
            Assert.IsNotNull(days.Latest);
            Assert.IsNotEmpty(days.Latest.title);
            StringAssert.Contains("Play Game", days.Records[0].prompt);
        }
    }
}
