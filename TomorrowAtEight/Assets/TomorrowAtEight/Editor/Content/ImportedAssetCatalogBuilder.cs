// ARCH-001.a, ARCH-004 | 2026-11-10 | Editor-only import map: binds key constants to source
// assets and writes their serialized references into the generated runtime catalog.
// Decision: .agents/architecture/ARCH-001.a-content-registry.md
// Journal: .agents/days/2026-11-12.md
using System;
using System.Collections.Generic;
using System.IO;
using TomorrowAtEight.Content;
using TomorrowAtEight.Definitions;
using UnityEditor;
using UnityEngine;

namespace TomorrowAtEight.EditorTools
{
    public static class ImportedAssetCatalogBuilder
    {
        public const string CatalogPath = "Assets/TomorrowAtEight/Resources/TomorrowAtEight/ImportedAssets.asset";
        public const string DailyRecordsFolder = "Assets/TomorrowAtEight/Data/Days";

        /// <summary>
        /// Source asset for every imported content key. Paths and GUIDs are editor binding
        /// details; runtime code only ever sees the key.
        /// </summary>
        static readonly KeyValuePair<IContentKey, string>[] s_Bindings =
        {
            new KeyValuePair<IContentKey, string>(
                CoreContent.LitShader,
                "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader"),
        };

        /// <summary>
        /// Regenerates the catalog in place. Repeatable: the asset and its GUID are kept,
        /// its entries and daily record list are overwritten.
        /// </summary>
        public static ImportedAssetCatalog Rebuild()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ImportedAssetCatalog>(CatalogPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
                AssetDatabase.Refresh();
                catalog = ScriptableObject.CreateInstance<ImportedAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var entries = new List<ImportedAssetCatalog.Entry>();
            for (int i = 0; i < s_Bindings.Length; i++)
            {
                IContentKey key = s_Bindings[i].Key;
                string path = s_Bindings[i].Value;
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, key.ResultType);
                if (asset == null)
                    throw new InvalidOperationException($"Import map: no {key.ResultType.Name} at '{path}' for '{key.Id}'.");
                entries.Add(new ImportedAssetCatalog.Entry { key = key.Id, asset = asset });
            }

            var dailyRecords = new List<TextAsset>();
            if (AssetDatabase.IsValidFolder(DailyRecordsFolder))
            {
                string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { DailyRecordsFolder });
                var paths = new List<string>();
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        paths.Add(path);
                }
                paths.Sort(StringComparer.Ordinal);
                for (int i = 0; i < paths.Count; i++)
                    dailyRecords.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(paths[i]));
            }

            catalog.entries = entries;
            catalog.dailyRecords = dailyRecords;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TomorrowAtEight] Imported asset catalog rebuilt at {CatalogPath}: " +
                      $"{entries.Count} asset binding(s), {dailyRecords.Count} daily record(s).");
            return catalog;
        }

        /// <summary>Returns null when the saved catalog matches the import map, else the problem.</summary>
        public static string Verify()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ImportedAssetCatalog>(CatalogPath);
            if (catalog == null)
                return $"Missing catalog asset at {CatalogPath}.";
            for (int i = 0; i < s_Bindings.Length; i++)
            {
                string id = s_Bindings[i].Key.Id;
                if (!catalog.TryGetAsset(id, out UnityEngine.Object asset) || asset == null)
                    return $"Catalog has no reference for '{id}'.";
            }
            if (catalog.dailyRecords.Count == 0)
                return "Catalog lists no daily record.";
            for (int i = 0; i < catalog.dailyRecords.Count; i++)
                if (catalog.dailyRecords[i] == null)
                    return $"Catalog daily record {i} is a missing reference.";
            return null;
        }
    }
}
