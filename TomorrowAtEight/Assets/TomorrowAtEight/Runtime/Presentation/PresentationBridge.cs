// ARCH-005 | 2026-11-11 | Main-thread bridge from ECS state to ordinary URP GameObjects.
// Entities Graphics does not support web platforms, so views are plain renderers.
// Decision: .agents/ARCHITECTURE.md#arch-005---unity-entities-simulation-and-web-presentation
// Journal: .agents/days/2026-11-12.md
using System;
using System.Collections.Generic;
using TomorrowAtEight.Content;
using TomorrowAtEight.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TomorrowAtEight.Presentation
{
    /// <summary>
    /// Keeps one view GameObject per entity that has a <see cref="ViewModel"/>, copies
    /// transforms after simulation, and removes views of despawned entities.
    /// Views hold no gameplay state.
    /// </summary>
    public sealed class PresentationBridge
    {
        sealed class View
        {
            public GameObject Instance;
            public Transform Transform;
            public int SeenAtSync;
        }

        readonly ContentRegistry m_Registry;
        readonly IReadOnlyList<string> m_ModelIds;
        readonly GameObject[] m_Templates;
        readonly Transform m_Root;
        readonly EntityQuery m_Query;
        readonly Dictionary<Entity, View> m_Views = new Dictionary<Entity, View>();
        readonly List<Entity> m_Removed = new List<Entity>();
        int m_SyncCount;

        public PresentationBridge(
            EntityManager entityManager,
            ContentRegistry registry,
            IReadOnlyList<string> modelIds,
            Transform root)
        {
            m_Registry = registry;
            m_ModelIds = modelIds;
            m_Templates = new GameObject[modelIds.Count];
            m_Root = root;
            m_Query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SessionOwned, ViewModel, LocalToWorld>()
                .Build(entityManager);
        }

        public int ViewCount => m_Views.Count;

        /// <summary>Creates, updates, and removes views to match the current entities.</summary>
        public void Sync()
        {
            m_SyncCount++;

            NativeArray<Entity> entities = m_Query.ToEntityArray(Allocator.Temp);
            NativeArray<ViewModel> models = m_Query.ToComponentDataArray<ViewModel>(Allocator.Temp);
            NativeArray<LocalToWorld> transforms = m_Query.ToComponentDataArray<LocalToWorld>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                if (!m_Views.TryGetValue(entities[i], out View view))
                {
                    view = CreateView(models[i].ModelIndex);
                    m_Views.Add(entities[i], view);
                }

                view.SeenAtSync = m_SyncCount;
                float4x4 matrix = transforms[i].Value;
                Vector3 scale = new Vector3(
                    math.length(matrix.c0.xyz),
                    math.length(matrix.c1.xyz),
                    math.length(matrix.c2.xyz));
                if (scale.x < 1e-6f || scale.y < 1e-6f || scale.z < 1e-6f)
                {
                    // World matrix not computed yet (spawned after this frame's transform update).
                    view.Transform.localScale = Vector3.zero;
                    continue;
                }
                view.Transform.SetPositionAndRotation(transforms[i].Position, transforms[i].Rotation);
                view.Transform.localScale = scale;
            }

            entities.Dispose();
            models.Dispose();
            transforms.Dispose();

            if (m_Views.Count == 0)
                return;

            m_Removed.Clear();
            foreach (KeyValuePair<Entity, View> pair in m_Views)
            {
                if (pair.Value.SeenAtSync != m_SyncCount)
                    m_Removed.Add(pair.Key);
            }
            for (int i = 0; i < m_Removed.Count; i++)
            {
                DestroyView(m_Views[m_Removed[i]]);
                m_Views.Remove(m_Removed[i]);
            }
        }

        /// <summary>Removes every remaining view; called at session shutdown.</summary>
        public void Clear()
        {
            foreach (KeyValuePair<Entity, View> pair in m_Views)
                DestroyView(pair.Value);
            m_Views.Clear();
        }

        View CreateView(int modelIndex)
        {
            if (modelIndex < 0 || modelIndex >= m_Templates.Length)
                throw new ContentException($"View model index {modelIndex} is outside the session's model table.");

            GameObject template = m_Templates[modelIndex];
            if (template == null)
            {
                // Shared template, resolved once per session; instances are created here.
                template = m_Registry.Resolve(new ContentKey<GameObject>(m_ModelIds[modelIndex]));
                m_Templates[modelIndex] = template;
            }

            GameObject instance = UnityEngine.Object.Instantiate(template, m_Root);
            instance.name = m_ModelIds[modelIndex];
            instance.SetActive(true);
            return new View { Instance = instance, Transform = instance.transform };
        }

        static void DestroyView(View view)
        {
            if (view.Instance != null)
                UnityObjects.Destroy(view.Instance);
        }
    }

    /// <summary>Destroys Unity objects correctly in play mode and in edit-mode tests.</summary>
    public static class UnityObjects
    {
        public static void Destroy(UnityEngine.Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
