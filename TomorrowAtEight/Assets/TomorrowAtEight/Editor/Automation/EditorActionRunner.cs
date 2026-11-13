// ARCH-003 | 2026-11-11 | Shared runner for dated, one-time editor actions with a versioned
// completion record. Daily action implementations are temporary; see the retention rule.
// Decision: .agents/ARCHITECTURE.md#arch-003---owner-triggered-editor-automation
// Journal: .agents/days/2026-11-12.md
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TomorrowAtEight.EditorTools
{
    /// <summary>A one-time editor action. Applied actions are immutable; corrections get new IDs.</summary>
    public sealed class DailyEditorAction
    {
        /// <summary>Stable ID: YYYY-MM-DD/NNN/slug. Ordinal order is execution order.</summary>
        public string Id;
        public string Journal;
        public string Description;
        public string[] ExpectedOutputs;
        /// <summary>Must tolerate a retry after partial execution without duplicating outputs.</summary>
        public Action Apply;
        /// <summary>Returns null when the expected outputs exist and are correct.</summary>
        public Func<string> Verify;
    }

    public static class EditorActionRunner
    {
        public const string StatePath = "ProjectSettings/TomorrowAtEight.EditorActions.json";

        [Serializable]
        sealed class CompletedAction
        {
            public string id;
            public string completedUtc;
        }

        [Serializable]
        sealed class State
        {
            public int schemaVersion = 1;
            public List<CompletedAction> completed = new List<CompletedAction>();
        }

        [MenuItem("TomorrowAtEight/Run Daily Scripts", priority = 0)]
        static void RunDailyScriptsMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[TomorrowAtEight] Daily scripts cannot run in Play mode.");
                return;
            }
            string summary = RunPending(DailyActions.All);
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Tomorrow at Eight - Daily Scripts", summary, "OK");
        }

        /// <summary>Batch entry point for the same command.</summary>
        public static void RunDailyScriptsFromCommandLine()
        {
            string summary = RunPending(DailyActions.All);
            EditorApplication.Exit(summary.StartsWith("FAILED") ? 1 : 0);
        }

        /// <summary>
        /// Applies pending actions in ID order, stops at the first failure, and persists
        /// each completion only after its outputs are saved and verified.
        /// </summary>
        public static string RunPending(IReadOnlyList<DailyEditorAction> actions)
        {
            State state = LoadState();
            var sorted = new List<DailyEditorAction>(actions);
            sorted.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            int applied = 0;
            int skipped = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                DailyEditorAction action = sorted[i];
                if (IsCompleted(state, action.Id))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    Debug.Log($"[TomorrowAtEight] Applying daily action {action.Id}: {action.Description}");
                    action.Apply();
                    AssetDatabase.SaveAssets();
                    string problem = action.Verify == null ? null : action.Verify();
                    if (problem != null)
                        throw new InvalidOperationException("Output verification failed: " + problem);
                }
                catch (Exception exception)
                {
                    string failure =
                        $"FAILED at daily action {action.Id}: {exception.Message}\n" +
                        "The action stays pending and later actions were not run. Fix the cause, then run " +
                        "TomorrowAtEight/Run Daily Scripts again; completed actions are skipped. " +
                        $"Journal: {action.Journal}";
                    Debug.LogError("[TomorrowAtEight] " + failure);
                    Debug.LogException(exception);
                    return failure;
                }

                state.completed.Add(new CompletedAction
                {
                    id = action.Id,
                    completedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                });
                SaveState(state);
                applied++;
                Debug.Log($"[TomorrowAtEight] Daily action {action.Id} completed and recorded in {StatePath}.");
            }

            string summary = applied == 0
                ? $"Nothing pending. {skipped} daily action(s) already completed."
                : $"Applied {applied} daily action(s); {skipped} already completed.";
            Debug.Log("[TomorrowAtEight] " + summary);
            return summary;
        }

        static bool IsCompleted(State state, string id)
        {
            for (int i = 0; i < state.completed.Count; i++)
                if (state.completed[i].id == id)
                    return true;
            return false;
        }

        static State LoadState()
        {
            if (!File.Exists(StatePath))
                return new State();
            State state = JsonUtility.FromJson<State>(File.ReadAllText(StatePath));
            return state ?? new State();
        }

        static void SaveState(State state)
        {
            File.WriteAllText(StatePath, JsonUtility.ToJson(state, true) + "\n");
        }
    }
}
