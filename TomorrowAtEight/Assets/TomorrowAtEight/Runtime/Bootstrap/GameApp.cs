// ARCH-002 | 2026-11-10 | Process-wide navigation state. Requests are applied by the
// session bootstrap system, never inside UI callbacks.
// Decision: .agents/ARCHITECTURE.md#arch-002---code-driven-scene-bootstrap
// Journal: .agents/days/2026-11-12.md
using TomorrowAtEight.Content;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TomorrowAtEight.Bootstrap
{
    /// <summary>Counts observed right after a session finished initializing or shutting down.</summary>
    public struct SessionDiagnostics
    {
        public int Ordinal;
        public string SceneId;
        public int Instances;
        public int Prefabs;
        public int OwnedEntities;
        public int Views;
        public int GeneratedAssets;
        public int Cameras;
        public int Canvases;
        public int EventSystems;
        public int SessionRoots;

        public override string ToString()
        {
            return $"session #{Ordinal} '{SceneId}': instances={Instances} prefabs={Prefabs} " +
                   $"ownedEntities={OwnedEntities} views={Views} generatedAssets={GeneratedAssets} " +
                   $"cameras={Cameras} canvases={Canvases} eventSystems={EventSystems} sessionRoots={SessionRoots}";
        }
    }

    /// <summary>
    /// Entry point of the player. Starts the launcher scene after load and holds the
    /// single active <see cref="GameSession"/>. Contains no gameplay state.
    /// </summary>
    public static class GameApp
    {
        /// <summary>The empty Unity scene that hosts every code-built session.</summary>
        public const string HostSceneName = "Main";
        public const string LogPrefix = "[TomorrowAtEight] ";

        static string s_RequestedSceneId;
        static bool s_WaitForHostScene;

        /// <summary>The running session, or null between sessions.</summary>
        public static GameSession Current { get; internal set; }

        /// <summary>Number of sessions started since the player launched.</summary>
        public static int SessionCount { get; internal set; }

        /// <summary>Last initialization failure, shown by the launcher.</summary>
        public static string LastError { get; internal set; }

        /// <summary>Entities still owned after the most recent teardown; expected to be zero.</summary>
        public static int LastTeardownLeftovers { get; internal set; }

        /// <summary>Diagnostics captured when the most recent session became ready.</summary>
        public static SessionDiagnostics LastReady { get; internal set; }

        public static bool HasRequest => s_RequestedSceneId != null;

        public static bool IsHostSceneReady =>
            !s_WaitForHostScene || SceneManager.GetActiveScene().name == HostSceneName;

        /// <summary>Asks the bootstrap to end the current session and start the given scene.</summary>
        public static void RequestScene(string sceneId)
        {
            s_RequestedSceneId = sceneId;
        }

        public static void RequestRestart()
        {
            if (Current != null)
                RequestScene(Current.SceneId);
        }

        public static void RequestMenu()
        {
            RequestScene(ContentCatalog.StartupScene.Id);
        }

        internal static string TakeRequest()
        {
            string sceneId = s_RequestedSceneId;
            s_RequestedSceneId = null;
            return sceneId;
        }

        // Statics survive when domain reload is disabled; reset them for each play session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_RequestedSceneId = null;
            s_WaitForHostScene = false;
            Current = null;
            SessionCount = 0;
            LastError = null;
            LastTeardownLeftovers = 0;
            LastReady = default;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            // In the Editor, Play may be pressed with another scene open. Sessions are
            // code-built, so switch to the empty host scene rather than inherit that
            // scene's cameras and lights. The test runner's own scene is left alone.
            Scene active = SceneManager.GetActiveScene();
            bool isTestRunnerScene = active.name.StartsWith("InitTestScene");
            if (active.name != HostSceneName && !isTestRunnerScene &&
                Application.CanStreamedLevelBeLoaded(HostSceneName))
            {
                s_WaitForHostScene = true;
                SceneManager.LoadScene(HostSceneName, LoadSceneMode.Single);
            }

            RequestMenu();
        }
    }
}
