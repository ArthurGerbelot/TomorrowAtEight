// ARCH-001.c | 2026-11-12 | Rights and replacement metadata is joined to registered content
// by authoring tools; it is not runtime state and is not bundled into player builds.
// Decision: .agents/architecture/ARCH-001.c-asset-rights.md
// Journal: .agents/days/2026-11-12.md
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TomorrowAtEight.Content;
using UnityEngine;

namespace TomorrowAtEight.EditorTools
{
    /// <summary>One reviewed asset implementation, identified by key and provider revision.</summary>
    [Serializable]
    public sealed class AssetMetadataEntry
    {
        public string contentKey;
        public string providerRevision;
        /// <summary>procedural, third_party_dependency, ai_generated, community, purchased, ...</summary>
        public string origin;
        public string source;
        public string author;
        /// <summary>not_required, pending, cleared, or blocked.</summary>
        public string ipStatus;
        public string ipStatusReason;
        public string rightsholder;
        public string license;
        public List<string> evidence = new List<string>();
        public List<string> allowedScopes = new List<string>();
        public string reviewDate;
        public bool replacementRequired;
        public string replacementReason;
        public string replacementTarget;
    }

    [Serializable]
    public sealed class AssetMetadataManifest
    {
        public const string ProjectPath = "Assets/TomorrowAtEight/Data/Content/AssetMetadata.json";
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion;
        public List<AssetMetadataEntry> entries = new List<AssetMetadataEntry>();

        public static AssetMetadataManifest LoadFromProject()
        {
            if (!File.Exists(ProjectPath))
                throw new FileNotFoundException("Asset metadata manifest not found.", ProjectPath);
            AssetMetadataManifest manifest = JsonUtility.FromJson<AssetMetadataManifest>(File.ReadAllText(ProjectPath));
            if (manifest == null || manifest.schemaVersion != CurrentSchemaVersion)
                throw new InvalidDataException($"{ProjectPath}: unsupported or missing schemaVersion.");
            return manifest;
        }
    }

    public enum RightsSeverity
    {
        Info,
        Warning,
        Blocking,
    }

    public sealed class RightsIssue
    {
        public RightsSeverity Severity;
        public string Code;
        public string ContentKey;
        public string Message;

        public override string ToString() => $"[{Severity}] {Code} {ContentKey}: {Message}";
    }

    public sealed class RightsReport
    {
        public string Scope;
        public int CheckedAssets;
        public readonly List<RightsIssue> Issues = new List<RightsIssue>();

        public bool HasBlockingIssues
        {
            get
            {
                for (int i = 0; i < Issues.Count; i++)
                    if (Issues[i].Severity == RightsSeverity.Blocking)
                        return true;
                return false;
            }
        }

        public int Count(string code)
        {
            int count = 0;
            for (int i = 0; i < Issues.Count; i++)
                if (Issues[i].Code == code)
                    count++;
            return count;
        }

        public string ToText()
        {
            var text = new StringBuilder();
            text.Append("Asset rights report for scope '").Append(Scope).Append("': ")
                .Append(CheckedAssets).Append(" asset(s) checked, ")
                .Append(HasBlockingIssues ? "BLOCKED" : "no blocking issues").Append('.');
            for (int i = 0; i < Issues.Count; i++)
                text.Append("\n  ").Append(Issues[i]);
            return text.ToString();
        }
    }

    /// <summary>
    /// Checks every asset part in the dependency closure of the selected content against
    /// the metadata manifest for one use scope. A status is never inherited from a parent.
    /// </summary>
    public static class AssetRightsValidator
    {
        public const string PrivateDevelopment = "private_development";
        public const string PublicSource = "public_source";
        public const string PublicBuild = "public_build";
        public const string PublicMedia = "public_media";

        public const string CodeMissing = "missing_record";
        public const string CodeDuplicate = "duplicate_record";
        public const string CodeStale = "stale_revision";
        public const string CodeBlocked = "blocked";
        public const string CodeUnknownStatus = "unknown_status";
        public const string CodeScope = "scope_not_permitted";
        public const string CodePending = "rights_pending";
        public const string CodeReplacement = "replacement_required";
        public const string CodeOrphan = "orphan_record";

        public static bool IsKnownScope(string scope)
        {
            return scope == PrivateDevelopment || scope == PublicSource ||
                   scope == PublicBuild || scope == PublicMedia;
        }

        public static RightsReport Validate(
            ContentRegistry registry,
            AssetMetadataManifest manifest,
            IEnumerable<string> rootIds,
            string scope)
        {
            if (!IsKnownScope(scope))
                throw new ArgumentException($"Unknown use scope '{scope}'.", nameof(scope));

            var report = new RightsReport { Scope = scope };
            var records = new Dictionary<string, AssetMetadataEntry>(StringComparer.Ordinal);
            var duplicates = new HashSet<string>(StringComparer.Ordinal);
            if (manifest != null && manifest.entries != null)
            {
                for (int i = 0; i < manifest.entries.Count; i++)
                {
                    AssetMetadataEntry record = manifest.entries[i];
                    if (record == null || string.IsNullOrEmpty(record.contentKey))
                        continue;
                    if (records.ContainsKey(record.contentKey))
                        duplicates.Add(record.contentKey);
                    else
                        records.Add(record.contentKey, record);
                }
            }

            var used = new HashSet<string>(StringComparer.Ordinal);
            List<string> closure = registry.GetDependencyClosure(rootIds);
            for (int i = 0; i < closure.Count; i++)
            {
                string id = closure[i];
                if (!registry.TryGetEntry(id, out ContentEntry entry) || !ContentKeyRules.IsAssetKind(entry.Kind))
                    continue;

                report.CheckedAssets++;
                used.Add(id);
                CheckAsset(report, entry, records, duplicates, scope);
            }

            foreach (KeyValuePair<string, AssetMetadataEntry> pair in records)
            {
                if (!used.Contains(pair.Key))
                {
                    Add(report, RightsSeverity.Warning, CodeOrphan, pair.Key,
                        "The manifest has a record for content that is not part of the selected content.");
                }
            }

            return report;
        }

        static void CheckAsset(
            RightsReport report,
            ContentEntry entry,
            Dictionary<string, AssetMetadataEntry> records,
            HashSet<string> duplicates,
            string scope)
        {
            string id = entry.Id;
            if (duplicates.Contains(id))
            {
                Add(report, RightsSeverity.Blocking, CodeDuplicate, id,
                    "More than one metadata record exists for this key; keep exactly one active record.");
                return;
            }

            // Unknown or stale records default to pending with no allowed scopes.
            if (!records.TryGetValue(id, out AssetMetadataEntry record))
            {
                Add(report, RightsSeverity.Blocking, CodeMissing, id,
                    "No metadata record; treated as pending with no permitted scope.");
                return;
            }
            if (!string.Equals(record.providerRevision, entry.ProviderRevision, StringComparison.Ordinal))
            {
                Add(report, RightsSeverity.Blocking, CodeStale, id,
                    $"Record reviews provider revision '{record.providerRevision}' but the registered " +
                    $"implementation is '{entry.ProviderRevision}'. A replaced implementation needs a new assessment.");
                return;
            }

            bool pending = false;
            switch (record.ipStatus)
            {
                case "not_required":
                case "cleared":
                    break;
                case "pending":
                    pending = true;
                    break;
                case "blocked":
                    Add(report, RightsSeverity.Blocking, CodeBlocked, id,
                        "Use is prohibited: " + Describe(record.ipStatusReason));
                    return;
                default:
                    Add(report, RightsSeverity.Blocking, CodeUnknownStatus, id,
                        $"Unknown ipStatus '{record.ipStatus}'; treated as pending with no permitted scope.");
                    return;
            }

            if (string.IsNullOrEmpty(record.ipStatusReason))
            {
                Add(report, RightsSeverity.Blocking, CodeUnknownStatus, id,
                    "The record does not state the basis for its ipStatus.");
                return;
            }

            if (record.allowedScopes == null || !record.allowedScopes.Contains(scope))
            {
                Add(report, RightsSeverity.Blocking, CodeScope, id,
                    $"Scope '{scope}' is not among the documented permitted scopes" +
                    (pending ? " and rights are pending; a pending status alone permits nothing." : "."));
                return;
            }

            if (pending)
            {
                Add(report, RightsSeverity.Warning, CodePending, id,
                    "Used under a documented temporary permission; permanent rights or evidence are outstanding: " +
                    Describe(record.ipStatusReason));
            }

            if (record.replacementRequired)
            {
                Add(report, RightsSeverity.Info, CodeReplacement, id,
                    $"Replacement required ({Describe(record.replacementReason)}); target: {Describe(record.replacementTarget)}.");
            }
        }

        static string Describe(string text) => string.IsNullOrEmpty(text) ? "not stated" : text;

        static void Add(RightsReport report, RightsSeverity severity, string code, string key, string message)
        {
            report.Issues.Add(new RightsIssue { Severity = severity, Code = code, ContentKey = key, Message = message });
        }
    }
}
