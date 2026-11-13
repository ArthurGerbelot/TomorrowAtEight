// ARCH-004 | 2026-11-11 | Public daily JSON is parsed into plain serializable data.
// Decision: .agents/ARCHITECTURE.md#arch-004---public-daily-data-and-agent-journals
// Journal: .agents/days/2026-11-12.md
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TomorrowAtEight.Content
{
    /// <summary>
    /// Content of <c>Assets/TomorrowAtEight/Data/Days/YYYY-MM-DD.json</c>: what was asked
    /// and what was done. Every file has exactly these keys, in this order; copy
    /// <c>.agents/days/TEMPLATE.json</c>. The same structure is described for other
    /// tools in <c>.agents/days/day.schema.json</c>.
    /// </summary>
    [Serializable]
    public sealed class DailyRecord
    {
        /// <summary>UTC date, equal to the file name.</summary>
        public string date;
        /// <summary>Day number; day 0 is the foundation build.</summary>
        public int day;
        public string title;
        /// <summary>Reddit name of the prompt's author, or empty.</summary>
        public string author;
        /// <summary>What was asked: the exact selected prompt, unchanged.</summary>
        public string prompt;
        /// <summary>What was done, in plain sentences.</summary>
        public List<string> done = new List<string>();
        /// <summary>Optional details worth sharing.</summary>
        public List<string> details = new List<string>();
        /// <summary>Optional ideas for the next prompts.</summary>
        public List<string> suggestions = new List<string>();
    }

    /// <summary>Daily records included in the running build, oldest first.</summary>
    public sealed class DailyRecordSet
    {
        public IReadOnlyList<DailyRecord> Records { get; }
        /// <summary>Records that could not be loaded; shown rather than silently dropped.</summary>
        public IReadOnlyList<string> Problems { get; }

        public DailyRecord Latest => Records.Count == 0 ? null : Records[Records.Count - 1];

        public DailyRecordSet(IReadOnlyList<DailyRecord> records, IReadOnlyList<string> problems)
        {
            Records = records;
            Problems = problems;
        }

        public static DailyRecordSet Parse(IReadOnlyList<TextAsset> sources)
        {
            var records = new List<DailyRecord>();
            var problems = new List<string>();
            if (sources != null)
            {
                for (int i = 0; i < sources.Count; i++)
                {
                    TextAsset source = sources[i];
                    if (source == null)
                    {
                        problems.Add($"Daily record reference {i} is missing.");
                        continue;
                    }

                    DailyRecord record;
                    try
                    {
                        record = JsonUtility.FromJson<DailyRecord>(source.text);
                    }
                    catch (Exception exception)
                    {
                        problems.Add($"{source.name}: invalid JSON ({exception.Message}).");
                        continue;
                    }

                    string problem = Validate(record, source.name);
                    if (problem != null)
                        problems.Add(problem);
                    else
                        records.Add(record);
                }
            }

            records.Sort((a, b) => string.CompareOrdinal(a.date, b.date));
            return new DailyRecordSet(records, problems);
        }

        static string Validate(DailyRecord record, string sourceName)
        {
            if (record == null)
                return $"{sourceName}: empty record.";
            if (record.date != sourceName)
                return $"{sourceName}: file name does not match record date '{record.date}'.";
            if (record.day < 0 || string.IsNullOrEmpty(record.title))
                return $"{sourceName}: day number or title is missing.";
            if (string.IsNullOrEmpty(record.prompt))
                return $"{sourceName}: the selected prompt text is missing.";
            return null;
        }
    }
}
