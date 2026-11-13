// ARCH-002, ARCH-005 | 2026-11-11 | The session lifecycle runs inside the default ECS world:
// navigation requests are applied in InitializationSystemGroup, views are synchronized in
// PresentationSystemGroup. The world is owned by Unity Entities and never disposed here.
// Decision: .agents/ARCHITECTURE.md#arch-002---code-driven-scene-bootstrap
// Journal: .agents/days/2026-11-12.md
using TomorrowAtEight.Content;
using Unity.Entities;

namespace TomorrowAtEight.Bootstrap
{
    /// <summary>
    /// Applies pending navigation requests. Teardown and the next initialization happen
    /// on separate frames, so destroyed objects of the old session are gone before the
    /// new session creates its camera and UI.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SessionBootstrapSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!GameApp.HasRequest || !GameApp.IsHostSceneReady)
                return;

            if (GameApp.Current != null)
            {
                GameApp.Current.Teardown();
                GameApp.Current = null;
                return;
            }

            string sceneId = GameApp.TakeRequest();
            GameApp.SessionCount++;
            var session = new GameSession(World, sceneId, GameApp.SessionCount);
            if (session.Initialize())
            {
                GameApp.Current = session;
                return;
            }

            // A partly initialized scene is never simulated: GameSessionReady stays absent.
            session.Teardown();
            GameApp.LastError = session.FailureMessage;
            if (sceneId != ContentCatalog.StartupScene.Id)
                GameApp.RequestMenu();
        }

        protected override void OnDestroy()
        {
            // World shutdown (leaving play mode or quitting): release the session while
            // the world still exists.
            if (GameApp.Current != null)
            {
                GameApp.Current.Teardown();
                GameApp.Current = null;
            }
        }
    }

    /// <summary>Runs after simulation and transforms; presentation only reads ECS state.</summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SessionPresentationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            GameSession session = GameApp.Current;
            if (session != null && session.IsReady)
                session.UpdatePresentation();
        }
    }
}
