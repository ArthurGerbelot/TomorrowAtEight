// ARCH-001.b, ARCH-002 | 2026-11-10 | Recipes are immutable definitions; factories and the
// bootstrap turn them into live entities and scenes.
// Decision: .agents/architecture/ARCH-001.b-entity-factory.md
// Journal: .agents/days/2026-11-12.md
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TomorrowAtEight.Content
{
    /// <summary>A child entity composed under its parent recipe (LinkedEntityGroup).</summary>
    public readonly struct EntityRecipeChild
    {
        public readonly ContentKey<EntityRecipe> Recipe;
        public readonly float3 Position;
        public readonly quaternion Rotation;
        public readonly float Scale;

        public EntityRecipeChild(ContentKey<EntityRecipe> recipe, float3 position, quaternion rotation, float scale)
        {
            Recipe = recipe;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public EntityRecipeChild(ContentKey<EntityRecipe> recipe, float3 position, float scale = 1f)
            : this(recipe, position, quaternion.identity, scale) { }
    }

    /// <summary>
    /// Definition of one kind of gameplay entity: default ECS components, optional
    /// presentation model, and composed children. Every key used here must also be
    /// declared as a dependency of the recipe's registration.
    /// </summary>
    public sealed class EntityRecipe
    {
        /// <summary>Optional <c>model.*</c> or <c>vfx.*</c> view template.</summary>
        public ContentKey<GameObject>? Model { get; }

        /// <summary>Adds the recipe's typed default components to a prefab entity.</summary>
        public Action<EntityManager, Entity> AddComponents { get; }

        public IReadOnlyList<EntityRecipeChild> Children { get; }

        public EntityRecipe(
            ContentKey<GameObject>? model = null,
            Action<EntityManager, Entity> addComponents = null,
            params EntityRecipeChild[] children)
        {
            Model = model;
            AddComponents = addComponents;
            Children = children ?? Array.Empty<EntityRecipeChild>();
        }
    }

    /// <summary>Camera, light, and ambient setup created in code for a scene.</summary>
    public struct StageSettings
    {
        public Vector3 CameraPosition;
        public Vector3 CameraEuler;
        public float FieldOfView;
        public Color Background;
        public Vector3 LightEuler;
        public Color LightColor;
        public float LightIntensity;
        public Color Ambient;

        public static StageSettings Default => new StageSettings
        {
            CameraPosition = new Vector3(0f, 7f, -11f),
            CameraEuler = new Vector3(30f, 0f, 0f),
            FieldOfView = 50f,
            Background = new Color(0.09f, 0.10f, 0.13f, 1f),
            LightEuler = new Vector3(50f, -30f, 0f),
            LightColor = Color.white,
            LightIntensity = 1.1f,
            Ambient = new Color(0.42f, 0.44f, 0.50f, 1f),
        };
    }

    /// <summary>
    /// Definition of a code-built scene. <see cref="Build"/> spawns content through the
    /// session's entity factory; nothing is read from a saved scene or the Inspector.
    /// </summary>
    public sealed class SceneRecipe
    {
        public string Title { get; }
        public string Description { get; }
        public StageSettings Stage { get; }
        /// <summary>When true the bootstrap adds the common Restart / Back to Menu HUD.</summary>
        public bool ShowSceneHud { get; }
        public Action<Bootstrap.SceneBuildContext> Build { get; }

        public SceneRecipe(
            string title,
            string description,
            StageSettings stage,
            bool showSceneHud,
            Action<Bootstrap.SceneBuildContext> build)
        {
            Title = title;
            Description = description;
            Stage = stage;
            ShowSceneHud = showSceneHud;
            Build = build;
        }
    }
}
