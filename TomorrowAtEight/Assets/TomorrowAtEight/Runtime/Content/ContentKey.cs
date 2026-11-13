// ARCH-001.a | 2026-11-10 | Stable typed keys identify content definitions, not instances.
// Decision: .agents/architecture/ARCH-001.a-content-registry.md
// Journal: .agents/days/2026-11-12.md
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TomorrowAtEight.Content
{
    /// <summary>Untyped view of a content key, used for dependency declarations.</summary>
    public interface IContentKey
    {
        string Id { get; }
        Type ResultType { get; }
    }

    /// <summary>
    /// Canonical content key plus the type its registration must produce.
    /// Format: <c>kind.namespace.name[.part...]</c>, lowercase ASCII.
    /// </summary>
    public readonly struct ContentKey<T> : IContentKey, IEquatable<ContentKey<T>>
    {
        public string Id { get; }
        public Type ResultType => typeof(T);

        public ContentKey(string id)
        {
            string error = ContentKeyRules.Check(id, typeof(T));
            if (error != null)
                throw new ArgumentException(error, nameof(id));
            Id = id;
        }

        public bool Equals(ContentKey<T> other) => string.Equals(Id, other.Id, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ContentKey<T> other && Equals(other);
        public override int GetHashCode() => Id == null ? 0 : StringComparer.Ordinal.GetHashCode(Id);
        public override string ToString() => Id ?? "<unset>";
    }

    /// <summary>Syntax rules and kind-to-result-type contracts for content keys.</summary>
    public static class ContentKeyRules
    {
        public const string Entity = "entity";
        public const string Model = "model";
        public const string Mesh = "mesh";
        public const string Material = "material";
        public const string Texture = "texture";
        public const string Shader = "shader";
        public const string Vfx = "vfx";
        public const string Sfx = "sfx";
        public const string Scene = "scene";
        public const string Data = "data";
        // Compatible extension, 2026-11-12: UI text needs a tracked font asset.
        public const string Font = "font";

        // A null contract means the key declares its own immutable data type.
        static readonly Dictionary<string, Type> s_Contracts = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            { Entity, typeof(EntityRecipe) },
            { Model, typeof(GameObject) },
            { Vfx, typeof(GameObject) },
            { Mesh, typeof(UnityEngine.Mesh) },
            { Material, typeof(UnityEngine.Material) },
            { Texture, typeof(UnityEngine.Texture) },
            { Shader, typeof(UnityEngine.Shader) },
            { Sfx, typeof(AudioClip) },
            { Scene, typeof(SceneRecipe) },
            { Font, typeof(UnityEngine.Font) },
            { Data, null },
        };

        // Kinds whose results are assets that need ARCH-001.c rights records.
        static readonly HashSet<string> s_AssetKinds = new HashSet<string>(StringComparer.Ordinal)
        {
            Model, Vfx, Mesh, Material, Texture, Shader, Sfx, Font,
        };

        /// <summary>Returns null when valid, otherwise a message describing the problem.</summary>
        public static string Check(string id, Type resultType)
        {
            string error = CheckSyntax(id, out string kind);
            if (error != null)
                return error;

            Type contract = s_Contracts[kind];
            if (contract != null && contract != resultType)
            {
                return $"Content key '{id}' has kind '{kind}', which requires result type " +
                       $"{contract.Name}, but it was declared with {resultType?.Name ?? "<null>"}.";
            }
            return null;
        }

        /// <summary>Validates external text (prompts, daily JSON) at the boundary.</summary>
        public static string CheckSyntax(string id, out string kind)
        {
            kind = null;
            if (string.IsNullOrEmpty(id))
                return "Content key is empty.";

            string[] segments = id.Split('.');
            if (segments.Length < 3)
                return $"Content key '{id}' must follow kind.namespace.name[.part...].";

            for (int i = 0; i < segments.Length; i++)
            {
                if (!IsValidSegment(segments[i]))
                {
                    return $"Content key '{id}' has an invalid segment '{segments[i]}'. Segments use " +
                           "lowercase letters, digits, underscores, or hyphens and start with a letter or digit.";
                }
            }

            if (!s_Contracts.ContainsKey(segments[0]))
                return $"Content key '{id}' has unknown kind '{segments[0]}'.";

            kind = segments[0];
            return null;
        }

        public static string KindOf(string id)
        {
            int dot = id == null ? -1 : id.IndexOf('.');
            return dot <= 0 ? null : id.Substring(0, dot);
        }

        public static bool IsAssetKind(string kind) => kind != null && s_AssetKinds.Contains(kind);

        static bool IsValidSegment(string segment)
        {
            if (segment.Length == 0)
                return false;
            for (int i = 0; i < segment.Length; i++)
            {
                char c = segment[i];
                bool alphanumeric = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
                if (alphanumeric)
                    continue;
                if (i > 0 && (c == '_' || c == '-'))
                    continue;
                return false;
            }
            return true;
        }
    }
}
