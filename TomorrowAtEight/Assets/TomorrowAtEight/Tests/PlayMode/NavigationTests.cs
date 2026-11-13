// ARCH-002 | 2026-11-10 | Play-mode check of startup, every launcher scene, restart, and
// repeated navigation without leftover entities, cameras, canvases, or event systems.
// Journal: .agents/days/2026-11-12.md
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TomorrowAtEight.Bootstrap;
using TomorrowAtEight.Content;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TomorrowAtEight.Tests
{
    public sealed class NavigationTests
    {
        const int TimeoutFrames = 600;

        [UnityTest]
        public IEnumerator Launcher_ListsPlayGameDisabled_AndThreeTestScenes()
        {
            yield return WaitForScene(ContentCatalog.StartupScene.Id, 0);

            LauncherCatalog catalog = GameApp.Current.Registry.Resolve(ContentCatalog.Launcher);
            Assert.AreEqual(4, catalog.Entries.Count);
            Assert.AreEqual("Play Game", catalog.Entries[0].Label);
            Assert.IsFalse(catalog.Entries[0].Enabled);
            Assert.IsNull(catalog.Entries[0].SceneId);
            for (int i = 1; i <= 3; i++)
            {
                Assert.AreEqual("Run Test Scene " + i, catalog.Entries[i].Label);
                Assert.IsTrue(catalog.Entries[i].Enabled);
            }

            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            Assert.AreEqual(4, buttons.Length, "One launcher button per catalog entry.");
            int disabled = 0;
            foreach (Button button in buttons)
                if (!button.interactable)
                    disabled++;
            Assert.AreEqual(1, disabled, "Only Play Game is disabled.");
            AssertSingleSessionObjects();
        }

        [UnityTest]
        public IEnumerator EveryScene_StartsRestartsAndReturnsToMenu_WithoutLeaks()
        {
            yield return WaitForScene(ContentCatalog.StartupScene.Id, 0);
            LauncherCatalog catalog = GameApp.Current.Registry.Resolve(ContentCatalog.Launcher);
            var sceneIds = new List<string>();
            foreach (LauncherEntry entry in catalog.Entries)
                if (entry.Enabled && entry.SceneId != null)
                    sceneIds.Add(entry.SceneId);
            Assert.AreEqual(3, sceneIds.Count);

            for (int round = 0; round < 3; round++)
            {
                foreach (string sceneId in sceneIds)
                {
                    int ordinal = GameApp.Current.Ordinal;
                    GameApp.RequestScene(sceneId);
                    yield return WaitForScene(sceneId, ordinal);
                    AssertSingleSessionObjects();
                    Assert.AreEqual(2, Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length,
                        "A test scene shows exactly Restart and Back to Menu.");
                    int initialInstances = GameApp.LastReady.Instances;
                    Assert.Greater(initialInstances, 0);

                    // Let simulation, deferred spawning, and presentation run.
                    for (int frame = 0; frame < 45; frame++)
                        yield return null;
                    Assert.IsTrue(GameApp.Current.IsReady);

                    ordinal = GameApp.Current.Ordinal;
                    GameApp.RequestRestart();
                    yield return WaitForScene(sceneId, ordinal);
                    AssertSingleSessionObjects();
                    Assert.AreEqual(initialInstances, GameApp.LastReady.Instances,
                        "A restarted scene starts from the same entity count.");

                    // Switch directly to the next scene on odd rounds, through the menu otherwise.
                    if (round % 2 == 0)
                    {
                        ordinal = GameApp.Current.Ordinal;
                        GameApp.RequestMenu();
                        yield return WaitForScene(ContentCatalog.StartupScene.Id, ordinal);
                        AssertSingleSessionObjects();
                        Assert.AreEqual(0, GameApp.LastReady.Views, "The launcher has no entity views.");
                    }
                }
            }

            Assert.IsNull(GameApp.LastError);
        }

        static IEnumerator WaitForScene(string sceneId, int afterOrdinal)
        {
            for (int frame = 0; frame < TimeoutFrames; frame++)
            {
                GameSession session = GameApp.Current;
                if (session != null && session.IsReady && session.SceneId == sceneId && session.Ordinal > afterOrdinal)
                {
                    // One more frame so objects destroyed by the previous session are gone.
                    yield return null;
                    yield break;
                }
                yield return null;
            }
            Assert.Fail($"Scene '{sceneId}' did not become ready. Last error: {GameApp.LastError}");
        }

        static void AssertSingleSessionObjects()
        {
            Assert.AreEqual(0, GameApp.LastTeardownLeftovers, "Entities left by the previous session.");
            SessionDiagnostics diagnostics = GameApp.Current.CaptureDiagnostics();
            Assert.AreEqual(1, diagnostics.Cameras, diagnostics.ToString());
            Assert.AreEqual(1, diagnostics.Canvases, diagnostics.ToString());
            Assert.AreEqual(1, diagnostics.EventSystems, diagnostics.ToString());
            Assert.AreEqual(1, diagnostics.SessionRoots, diagnostics.ToString());
            Assert.AreEqual(1, GameSession.CountInActiveScene<AudioListener>());
        }
    }
}
