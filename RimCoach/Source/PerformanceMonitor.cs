using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimCoach
{
    public static class PerformanceMonitor
    {
        // Pausing, thresholds
        private static bool paused = false;
        private static bool autoRefresh = true;

        // Settings
        public static double SpikeThresholdMs { get; set; } = 5.0;
        public static double AlertThresholdMs { get; set; } = 2.0;
        public static int MaxSpikeHistory = 50;

        // Sample storage
        private static readonly Dictionary<string, ProfiledMethod> trackedMethods = new Dictionary<string, ProfiledMethod>();
        private static readonly List<ProfiledMethod> displayList = new List<ProfiledMethod>();

        // Spike logs
        private static readonly List<SpikeEvent> spikeLog = new List<SpikeEvent>();

        // Trend tracking
        private static readonly List<PerformanceSnapshot> trendHistory = new List<PerformanceSnapshot>();

        // Baseline comparison
        private static PerformanceSnapshot baselineSnapshot = null;

        // Per-map support
        private static readonly Dictionary<int, MapProfile> mapProfiles = new Dictionary<int, MapProfile>();

        // Overhead monitoring
        private static double lastMonitorOverheadMs = 0.0;
        private static double rollingMonitorOverheadMs = 0.0;

        // Spike detection tracking
        private static int lastCheckTick = 0;
        private static int checkIntervalTicks = 600;

        public static bool IsPaused => paused;
        public static bool AutoRefresh => autoRefresh;

        public static double RimCoachOverhead => rollingMonitorOverheadMs;

        public static void Pause() => paused = true;
        public static void Resume() => paused = false;
        public static void ToggleAutoRefresh() => autoRefresh = !autoRefresh;
        public static void ResetData()
        {
            trackedMethods.Clear();
            displayList.Clear();
            spikeLog.Clear();
            trendHistory.Clear();
            mapProfiles.Clear();
            baselineSnapshot = null;
        }
        public static void RecordSample(string key, double ms, PatchDef def, int mapID = -1)
        {
            if (paused) return;

            var start = DateTime.UtcNow;

            if (!trackedMethods.TryGetValue(key, out var method))
            {
                method = new ProfiledMethod
                {
                    Key = key,
                    Def = def
                };
                trackedMethods[key] = method;
            }

            method.AddSample(ms);

            // Spike detection
            if (ms >= SpikeThresholdMs)
            {
                var spike = new SpikeEvent
                {
                    MethodKey = key,
                    PatchDef = def,
                    SpikeTimeMs = ms,
                    Timestamp = DateTime.Now
                };

                spikeLog.Add(spike);
                if (spikeLog.Count > MaxSpikeHistory)
                    spikeLog.RemoveAt(0);
            }

            // Per-map support
            if (mapID >= 0)
            {
                if (!mapProfiles.TryGetValue(mapID, out var profile))
                {
                    profile = new MapProfile { MapID = mapID };
                    mapProfiles[mapID] = profile;
                }
                profile.AddSample(key, ms, def);
            }

            var end = DateTime.UtcNow;
            lastMonitorOverheadMs = (end - start).TotalMilliseconds;

            // Smooth out overhead (rolling average)
            rollingMonitorOverheadMs = (rollingMonitorOverheadMs * 0.95) + (lastMonitorOverheadMs * 0.05);
        }

        public static double GetMonitorOverhead()
        {
            return rollingMonitorOverheadMs;
        }

        public static void UpdateDisplayData()
        {
            if (paused) return;

            displayList.Clear();

            foreach (var pair in trackedMethods)
            {
                if (pair.Value.TotalSamples > 0)
                {
                    pair.Value.UpdateRollingStats();
                    displayList.Add(pair.Value);
                }
            }

            displayList.Sort((a, b) => b.AverageTimeMs.CompareTo(a.AverageTimeMs));

            if (autoRefresh)
                StoreTrendSnapshot();
        }

        private static void StoreTrendSnapshot()
        {
            if (displayList.Count == 0) return;

            var snapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.Now,
                Methods = displayList.Select(m => m.Clone()).ToList()
            };

            trendHistory.Add(snapshot);

            if (trendHistory.Count > 100)
                trendHistory.RemoveAt(0);
        }

        public static void SaveBaseline()
        {
            baselineSnapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.Now,
                Methods = displayList.Select(m => m.Clone()).ToList()
            };
        }

        public static double CompareToBaseline()
        {
            if (baselineSnapshot == null || baselineSnapshot.Methods.Count == 0) return 0;

            double totalCurrent = displayList.Sum(m => m.AverageTimeMs);
            double totalBaseline = baselineSnapshot.Methods.Sum(m => m.AverageTimeMs);

            if (totalBaseline <= 0) return 0;

            return ((totalCurrent - totalBaseline) / totalBaseline) * 100.0;
        }
        public static List<ProfiledMethod> GetDisplayData()
        {
            if (autoRefresh)
                UpdateDisplayData();

            return displayList;
        }

        public static bool HasData()
        {
            return trackedMethods.Count > 0;
        }

        public static ProfiledMethod GetTopIssue()
        {
            return displayList.FirstOrDefault(m => m.AverageTimeMs >= AlertThresholdMs);
        }

        public static List<SpikeEvent> GetSpikeHistory()
        {
            return new List<SpikeEvent>(spikeLog);
        }

        public static Dictionary<string, double> GetCategoryBreakdown()
        {
            return displayList
                .Where(m => m.Def != null && !string.IsNullOrEmpty(m.Def.category))
                .GroupBy(m => m.Def.category)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.AverageTimeMs));
        }

        public static Dictionary<string, double> GetModBreakdown()
        {
            return displayList
                .Where(m => m.Def != null && !string.IsNullOrEmpty(m.Def.modHint))
                .GroupBy(m => m.Def.modHint)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.AverageTimeMs));
        }

        public static List<ProfiledMethod> GetMethodsBySeverity(string severity)
        {
            return displayList
                .Where(m => m.Def != null && m.Def.severityHint != null &&
                            m.Def.severityHint.ToLowerInvariant().Contains(severity.ToLowerInvariant()))
                .ToList();
        }

        public static List<ProfiledMethod> GetMethodsByMod(string modHint)
        {
            return displayList
                .Where(m => m.Def != null && m.Def.modHint != null &&
                            m.Def.modHint.ToLowerInvariant().Contains(modHint.ToLowerInvariant()))
                .ToList();
        }

        public static Dictionary<int, MapProfile> GetAllMapProfiles()
        {
            return new Dictionary<int, MapProfile>(mapProfiles);
        }

        public static double GetTotalTickBudget()
        {
            return displayList.Sum(m => m.AverageTimeMs);
        }

        public static double GetCategoryAverage(string category)
        {
            var methods = displayList.Where(m => m.Def != null && m.Def.category == category);
            if (!methods.Any()) return 0;

            return methods.Average(m => m.AverageTimeMs);
        }
        public static List<PerformanceSnapshot> GetTrendHistory()
        {
            return new List<PerformanceSnapshot>(trendHistory);
        }

        public static void LoadBaseline(PerformanceSnapshot snapshot)
        {
            baselineSnapshot = snapshot;
        }

        public static PerformanceSnapshot GetBaseline()
        {
            return baselineSnapshot;
        }

        public static List<ProfiledMethod> GetMethodsWithHighVariance(double threshold)
        {
            return displayList
                .Where(m => m.StdDevMs >= threshold)
                .ToList();
        }

        public static List<ProfiledMethod> GetMostFrequentMethods(int topN = 10)
        {
            return displayList
                .OrderByDescending(m => m.CallCount)
                .Take(topN)
                .ToList();
        }

        public static string GenerateCoachSummary()
        {
            double total = GetTotalTickBudget();
            if (total <= 0) return "RimCoach: No significant performance issues detected.";

            var topCategory = GetCategoryBreakdown()
                .OrderByDescending(kv => kv.Value)
                .FirstOrDefault();

            var topMod = GetModBreakdown()
                .OrderByDescending(kv => kv.Value)
                .FirstOrDefault();

            string summary = $"Total CPU Budget: {total:F2}ms.";

            if (!string.IsNullOrEmpty(topCategory.Key))
                summary += $" Top category: {topCategory.Key} at {topCategory.Value:F2}ms.";

            if (!string.IsNullOrEmpty(topMod.Key))
                summary += $" Top mod impact: {topMod.Key} at {topMod.Value:F2}ms.";

            double overhead = RimCoachOverhead;
            summary += $" RimCoach overhead: {overhead:F2}ms.";

            return summary;
        }
    }

    public class PerformanceSnapshot
    {
        public DateTime Timestamp;
        public List<ProfiledMethod> Methods;
    }

    public class MapProfile
    {
        public int MapID;
        private readonly Dictionary<string, ProfiledMethod> methods = new Dictionary<string, ProfiledMethod>();

        public void AddSample(string key, double ms, PatchDef def)
        {
            if (!methods.TryGetValue(key, out var method))
            {
                method = new ProfiledMethod { Key = key, Def = def };
                methods[key] = method;
            }
            method.AddSample(ms);
        }

        public IEnumerable<ProfiledMethod> GetMethods() => methods.Values;
    }
}
