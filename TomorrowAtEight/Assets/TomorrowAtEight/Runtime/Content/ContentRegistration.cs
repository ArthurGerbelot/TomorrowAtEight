// ARCH-001.a | 2026-11-10 | Content modules register through one context; scenes reach
// the launcher through the same call that registers their recipe.
// Decision: .agents/architecture/ARCH-001.a-content-registry.md
// Journal: .agents/days/2026-11-12.md
using System;
using System.Collections.Generic;

namespace TomorrowAtEight.Content
{
    /// <summary>One row of the development launcher.</summary>
    public sealed class LauncherEntry
    {
        public string Label { get; }
        /// <summary>Canonical <c>scene.*</c> key, or null for a placeholder row.</summary>
        public string SceneId { get; }
        public bool Enabled { get; }
        public string Note { get; }

        public LauncherEntry(string label, string sceneId, bool enabled, string note)
        {
            Label = label;
            SceneId = sceneId;
            Enabled = enabled;
            Note = note;
        }
    }

    /// <summary>Immutable launcher contents, resolved through <c>data.core.launcher</c>.</summary>
    public sealed class LauncherCatalog
    {
        public IReadOnlyList<LauncherEntry> Entries { get; }

        public LauncherCatalog(IReadOnlyList<LauncherEntry> entries)
        {
            Entries = entries;
        }
    }

    /// <summary>
    /// Passed to every content module by <c>ContentCatalog.RegisterAll</c>. Modules register
    /// definitions on <see cref="Registry"/>; scenes that should appear in the launcher use
    /// <see cref="RegisterLauncherScene"/>.
    /// </summary>
    public sealed class ContentRegistration
    {
        readonly List<LauncherEntry> m_LauncherEntries = new List<LauncherEntry>();
        readonly List<IContentKey> m_LauncherScenes = new List<IContentKey>();

        public ContentRegistry Registry { get; }
        /// <summary>Generated catalog of imported assets; null when it has not been generated.</summary>
        public ImportedAssetCatalog Imported { get; }

        public ContentRegistration(ContentRegistry registry, ImportedAssetCatalog imported)
        {
            Registry = registry;
            Imported = imported;
        }

        public IReadOnlyList<LauncherEntry> LauncherEntries => m_LauncherEntries;
        public IContentKey[] LauncherSceneKeys => m_LauncherScenes.ToArray();

        /// <summary>
        /// Registers a scene recipe and lists it in the launcher. This is the single
        /// registration path for launchable scenes; rows appear in registration order.
        /// </summary>
        public void RegisterLauncherScene(
            string label,
            ContentKey<SceneRecipe> key,
            string providerRevision,
            Func<ContentRegistry, SceneRecipe> provider,
            params IContentKey[] dependencies)
        {
            Registry.Register(key, ContentSource.Definition, providerRevision, provider, dependencies);
            m_LauncherEntries.Add(new LauncherEntry(label, key.Id, true, null));
            m_LauncherScenes.Add(key);
        }

        /// <summary>Adds a visible row that cannot be launched yet, such as Play Game.</summary>
        public void AddLauncherPlaceholder(string label, string note)
        {
            m_LauncherEntries.Add(new LauncherEntry(label, null, false, note));
        }
    }
}
