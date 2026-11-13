// ARCH-002 | 2026-11-10 | One code entry point owns initialization and shutdown of a
// content session. Order: imported catalog, content modules, validate and seal, default
// world, factory and presentation bridge, scene recipe, spawn, then enable gameplay.
// Decision: .agents/ARCHITECTURE.md#arch-002---code-driven-scene-bootstrap
// Journal: .agents/days/2026-11-12.md
using System;
using TomorrowAtEight.Content;
using TomorrowAtEight.Presentation;
using TomorrowAtEight.Simulation;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TomorrowAtEight.Bootstrap
{
    /// <summary>Services a scene recipe may use while it builds its content.</summary>
    public sealed class SceneBuildContext
    {
        public ContentRegistry Registry { get; internal set; }
        public EntityFactory Factory { get; internal set; }
        public EntityManager EntityManager { get; internal set; }
        public UiFactory Ui { get; internal set; }
        /// <summary>The session's single screen-space canvas.</summary>
        public RectTransform Canvas { get; internal set; }
        public Camera Camera { get; internal set; }

        /// <summary>Creates a session-owned singleton for system state or input commands.</summary>
        public Entity CreateSingleton<T>(T value) where T : unmanaged, IComponentData
        {
            Entity entity = Factory.CreateSessionEntity();
            EntityManager.AddComponentData(entity, value);
            return entity;
        }
    }

    /// <summary>
    /// Everything that exists for one launched scene: registry, generated assets, entity
    /// factory, views, camera, light, and UI. Nothing here outlives <see cref="Teardown"/>.
    /// </summary>
    public sealed class GameSession
    {
        public const string RootNamePrefix = "Session ";
        const float HudRefreshInterval = 0.25f;

        readonly World m_World;
        string m_Step = "not started";
        ContentRegistry m_Registry;
        EntityFactory m_Factory;
        PresentationBridge m_Bridge;
        GameObject m_Root;
        SceneHud m_Hud;
        Entity m_ReadyEntity;
        float m_NextHudRefresh;
        bool m_TornDown;

        public GameSession(World world, string sceneId, int ordinal)
        {
            m_World = world;
            SceneId = sceneId;
            Ordinal = ordinal;
        }

        public string SceneId { get; }
        public int Ordinal { get; }
        public bool IsReady { get; private set; }
        public string FailureMessage { get; private set; }
        public Camera Camera { get; private set; }
        public ContentRegistry Registry => m_Registry;
        public EntityFactory Factory => m_Factory;

        /// <summary>Runs every initialization step. Returns false and reports the failing step on error.</summary>
        public bool Initialize()
        {
            try
            {
                m_Step = "load imported asset catalog";
                ImportedAssetCatalog imported = ImportedAssetCatalog.Load();

                m_Step = "register content modules";
                m_Registry = new ContentRegistry(imported, UnityObjects.Destroy);
                ContentCatalog.RegisterAll(m_Registry, imported);

                m_Step = "validate and seal registry";
                m_Registry.ValidateAndSeal();

                m_Step = "obtain default world and create entity factory";
                if (m_World == null || !m_World.IsCreated)
                    throw new InvalidOperationException("The ECS world is not available.");
                EntityManager entityManager = m_World.EntityManager;
                m_Factory = new EntityFactory(entityManager, m_Registry);

                m_Step = "resolve scene recipe";
                string keyError = ContentKeyRules.Check(SceneId, typeof(SceneRecipe));
                if (keyError != null)
                    throw new ContentException(keyError);
                SceneRecipe recipe = m_Registry.Resolve(new ContentKey<SceneRecipe>(SceneId));

                m_Step = "create stage and presentation";
                m_Root = new GameObject(RootNamePrefix + Ordinal + " (" + SceneId + ")");
                Camera = CreateStage(recipe.Stage, m_Root.transform);
                var viewRoot = new GameObject("Views");
                viewRoot.transform.SetParent(m_Root.transform, false);
                m_Bridge = new PresentationBridge(entityManager, m_Registry, m_Factory.ModelIds, viewRoot.transform);

                var ui = new UiFactory(m_Registry.Resolve(ContentCatalog.UiFont));
                RectTransform canvas = UiFactory.CreateCanvas(m_Root.transform, "Canvas");
                UiFactory.CreateEventSystem(m_Root.transform);

                m_Step = "build scene content";
                var context = new SceneBuildContext
                {
                    Registry = m_Registry,
                    Factory = m_Factory,
                    EntityManager = entityManager,
                    Ui = ui,
                    Canvas = canvas,
                    Camera = Camera,
                };
                recipe.Build?.Invoke(context);
                if (recipe.ShowSceneHud)
                {
                    m_Hud = new SceneHud(ui, canvas, recipe.Title, recipe.Description,
                        GameApp.RequestRestart, GameApp.RequestMenu);
                }

                m_Step = "enable gameplay";
                m_ReadyEntity = m_Factory.CreateSessionEntity();
                entityManager.AddComponent<GameSessionReady>(m_ReadyEntity);
                IsReady = true;

                SessionDiagnostics diagnostics = CaptureDiagnostics();
                GameApp.LastReady = diagnostics;
                Debug.Log(GameApp.LogPrefix + "Ready " + diagnostics);
                ReportDuplicates(diagnostics);
                return true;
            }
            catch (Exception exception)
            {
                FailureMessage = $"Session initialization failed at step '{m_Step}' for '{SceneId}': {exception.Message}";
                Debug.LogError(GameApp.LogPrefix + FailureMessage);
                Debug.LogException(exception);
                return false;
            }
        }

        /// <summary>Copies ECS state to views and refreshes the HUD. Called after simulation.</summary>
        public void UpdatePresentation()
        {
            if (!IsReady)
                return;

            m_Bridge.Sync();

            if (m_Hud != null && Time.unscaledTime >= m_NextHudRefresh)
            {
                m_NextHudRefresh = Time.unscaledTime + HudRefreshInterval;
                SessionDiagnostics d = CaptureDiagnostics();
                m_Hud.SetStats(
                    $"Session #{d.Ordinal}  |  entities {d.Instances}  |  views {d.Views}  |  prefabs {d.Prefabs}\n" +
                    $"Shared generated assets {d.GeneratedAssets}  |  resolved keys {m_Registry.ResolvedCount}\n" +
                    $"Cameras {d.Cameras}  |  canvases {d.Canvases}  |  event systems {d.EventSystems}  |  roots {d.SessionRoots}\n" +
                    $"Leftover entities after last teardown: {GameApp.LastTeardownLeftovers}");
            }
        }

        /// <summary>
        /// Disables gameplay, releases entities and views while the world exists, then
        /// world-local prefabs, generated shared assets, and session references.
        /// Safe to call after a failed initialization and more than once.
        /// </summary>
        public void Teardown()
        {
            if (m_TornDown)
                return;
            m_TornDown = true;
            IsReady = false;

            bool worldAlive = m_World != null && m_World.IsCreated;
            int leftovers = 0;

            if (worldAlive && m_Factory != null)
            {
                EntityManager entityManager = m_World.EntityManager;
                if (m_ReadyEntity != Entity.Null && entityManager.Exists(m_ReadyEntity))
                    entityManager.DestroyEntity(m_ReadyEntity);
            }
            m_ReadyEntity = Entity.Null;

            m_Bridge?.Clear();
            m_Bridge = null;

            if (m_Factory != null)
            {
                if (worldAlive)
                {
                    // Removes instances, session singletons, and world-local prefabs.
                    m_Factory.Dispose();
                    leftovers = CountOwnedEntities(m_World.EntityManager);
                }
                m_Factory = null;
            }

            if (m_Root != null)
            {
                // Deactivate first: Destroy is deferred to the end of the frame, and the
                // camera, canvas, and event system must stop counting immediately.
                m_Root.SetActive(false);
                UnityObjects.Destroy(m_Root);
                m_Root = null;
            }
            Camera = null;
            m_Hud = null;

            m_Registry?.Dispose();
            m_Registry = null;

            GameApp.LastTeardownLeftovers = leftovers;
            if (leftovers != 0)
                Debug.LogError(GameApp.LogPrefix + $"Teardown of session #{Ordinal} '{SceneId}' left {leftovers} owned entities.");
            else
                Debug.Log(GameApp.LogPrefix + $"Teardown of session #{Ordinal} '{SceneId}' complete: 0 owned entities left.");
        }

        public SessionDiagnostics CaptureDiagnostics()
        {
            return new SessionDiagnostics
            {
                Ordinal = Ordinal,
                SceneId = SceneId,
                Instances = m_Factory == null ? 0 : m_Factory.CountInstances(),
                Prefabs = m_Factory == null ? 0 : m_Factory.PrefabCount,
                OwnedEntities = m_Factory == null ? 0 : m_Factory.CountAllOwned(),
                Views = m_Bridge == null ? 0 : m_Bridge.ViewCount,
                GeneratedAssets = m_Registry == null ? 0 : m_Registry.OwnedObjectCount,
                Cameras = CountInActiveScene<UnityEngine.Camera>(),
                Canvases = CountInActiveScene<Canvas>(),
                EventSystems = CountInActiveScene<EventSystem>(),
                SessionRoots = CountSessionRoots(),
            };
        }

        static void ReportDuplicates(SessionDiagnostics d)
        {
            if (d.Cameras != 1 || d.Canvases != 1 || d.EventSystems != 1 || d.SessionRoots != 1)
            {
                Debug.LogError(GameApp.LogPrefix +
                    $"Duplicate or missing session objects detected (expected exactly one camera, canvas, " +
                    $"event system, and session root): {d}");
            }
        }

        /// <summary>
        /// Active components of a type in the active scene. Objects kept in the
        /// DontDestroyOnLoad scene, such as URP's Rendering Debugger UI, are not session
        /// objects and are not counted.
        /// </summary>
        public static int CountInActiveScene<T>() where T : Component
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            T[] found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i].gameObject.scene == scene)
                    count++;
            }
            return count;
        }

        static int CountSessionRoots()
        {
            int count = 0;
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].activeSelf && roots[i].name.StartsWith(RootNamePrefix))
                    count++;
            }
            return count;
        }

        static int CountOwnedEntities(EntityManager entityManager)
        {
            EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<SessionOwned>() },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities,
            });
            int count = query.CalculateEntityCount();
            query.Dispose();
            return count;
        }

        static Camera CreateStage(StageSettings stage, Transform root)
        {
            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(root, false);
            cameraObject.transform.SetPositionAndRotation(stage.CameraPosition, Quaternion.Euler(stage.CameraEuler));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = stage.Background;
            camera.fieldOfView = stage.FieldOfView;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.rotation = Quaternion.Euler(stage.LightEuler);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = stage.LightColor;
            light.intensity = stage.LightIntensity;
            light.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = stage.Ambient;
            return camera;
        }
    }
}
