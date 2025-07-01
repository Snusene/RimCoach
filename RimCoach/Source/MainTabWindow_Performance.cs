using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimCoach
{
    public class MainTabWindow_Performance : MainTabWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private string filterCategory = "All";
        private string filterMod = "All";
        private bool showAdvanced = true;
        private bool showCoachTips = true;
        private bool showSeverityColors = true;
        private bool showOnlyAboveThreshold = false;
        private bool highlightNew = true;
        private bool minimalistMode = false;
        private bool focusOnlyMode = false;
        private bool autoSortBySeverity = true;
        private bool darkTheme = false;
        private bool soundAlerts = true;
        private double userSpikeThreshold = 5.0;
        private double userAlertThreshold = 2.0;
        private int advisorCooldownTicks = 600;
        private int lastAdvisorMessageTick = 0;

        // Color palette
        private readonly Color lowSeverityColor = new Color(0.3f, 0.8f, 0.3f);
        private readonly Color moderateSeverityColor = new Color(0.8f, 0.8f, 0.2f);
        private readonly Color highSeverityColor = new Color(1.0f, 0.5f, 0.0f);
        private readonly Color criticalSeverityColor = new Color(1.0f, 0.2f, 0.2f);
        private readonly Color darkBackground = new Color(0.1f, 0.1f, 0.1f);
        private readonly Color lightBackground = new Color(0.9f, 0.9f, 0.9f);

        public override Vector2 InitialSize => new Vector2(900f, 650f);

        public MainTabWindow_Performance()
        {
            this.absorbInputAroundWindow = true;
            this.preventCameraMotion = false;
            this.soundAppear = SoundDefOf.TabOpen;
            this.soundClose = SoundDefOf.TabClose;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            // Apply theme
            GUI.color = darkTheme ? darkBackground : lightBackground;
            GUI.DrawTexture(inRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // Advisor Summary
            string coachSummary = PerformanceMonitor.GenerateCoachSummary();
            Text.Anchor = TextAnchor.MiddleCenter;
            listing.Label(coachSummary, 24f);
            Text.Anchor = TextAnchor.UpperLeft;
            listing.Gap(6f);

            // User Controls / Toggles
            listing.Label("RimCoach Settings:");
            listing.CheckboxLabeled(" Show Advanced Data", ref showAdvanced);
            listing.CheckboxLabeled(" Show Coach Tips", ref showCoachTips);
            listing.CheckboxLabeled(" Severity Colors", ref showSeverityColors);
            listing.CheckboxLabeled(" Minimalist Mode", ref minimalistMode);
            listing.CheckboxLabeled(" Focus Only Mode", ref focusOnlyMode);
            listing.CheckboxLabeled(" Highlight New", ref highlightNew);
            listing.CheckboxLabeled(" Auto-Sort by Severity", ref autoSortBySeverity);
            listing.CheckboxLabeled(" Sound Alerts", ref soundAlerts);
            listing.CheckboxLabeled(" Dark Theme", ref darkTheme);
            listing.GapLine();

            // Baseline Comparison
            double baselineDiff = PerformanceMonitor.CompareToBaseline();
            string baselineLabel = baselineDiff > 0
                ? $"Performance vs Baseline: +{baselineDiff:F1}% slower"
                : $"Performance vs Baseline: {Math.Abs(baselineDiff):F1}% faster";
            listing.Label(baselineLabel);

            // RimCoach Overhead
            listing.Label($"RimCoach overhead: {PerformanceMonitor.GetMonitorOverhead():F2} ms");

            listing.End();
            listing.GapLine();
        }
        private void DrawMethodList(Rect inRect)
        {
            var methods = PerformanceMonitor.GetDisplayData();

            if (autoSortBySeverity)
                methods = methods.OrderByDescending(m => m.AverageTimeMs).ToList();

            // Filtering
            if (showOnlyAboveThreshold)
                methods = methods.Where(m => m.AverageTimeMs >= userAlertThreshold).ToList();

            if (filterCategory != "All")
                methods = methods.Where(m => m.Def != null && m.Def.category == filterCategory).ToList();

            if (filterMod != "All")
                methods = methods.Where(m => m.Def != null && m.Def.modHint == filterMod).ToList();

            Rect scrollRect = new Rect(0, 0, inRect.width - 16, methods.Count * 40);
            Widgets.BeginScrollView(inRect, ref scrollPosition, scrollRect);

            float y = 0;
            foreach (var method in methods)
            {
                Rect rowRect = new Rect(0, y, scrollRect.width, 36);

                if (showSeverityColors)
                    GUI.color = GetSeverityColor(method.AverageTimeMs);

                Widgets.DrawBoxSolid(rowRect, GUI.color);
                GUI.color = Color.white;

                float x = 4f;
                Widgets.Label(new Rect(x, y, 180f, 36f), method.Def?.label ?? method.Key);
                x += 180f;

                Widgets.Label(new Rect(x, y, 60f, 36f), $"{method.AverageTimeMs:F2}ms");
                x += 60f;

                if (!minimalistMode)
                {
                    Widgets.Label(new Rect(x, y, 60f, 36f), $"Calls: {method.CallCount}");
                    x += 60f;

                    Widgets.Label(new Rect(x, y, 100f, 36f), $"Category: {method.Def?.category}");
                    x += 100f;

                    Widgets.Label(new Rect(x, y, 120f, 36f), $"Mod: {method.Def?.modHint}");
                    x += 120f;
                }

                if (showCoachTips && !string.IsNullOrEmpty(PerformanceAdvisor.GetTipFor(method)))
                {
                    x += 4f;
                    Widgets.Label(new Rect(x, y, rowRect.width - x - 4, 36f), PerformanceAdvisor.GetTipFor(method));
                }

                y += 40f;
            }

            Widgets.EndScrollView();
        }

        private Color GetSeverityColor(double ms)
        {
            if (ms < 0.5) return lowSeverityColor;
            if (ms < 2.0) return moderateSeverityColor;
            if (ms < 5.0) return highSeverityColor;
            return criticalSeverityColor;
        }
        private void DrawControls(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("Filters:");
            if (Widgets.ButtonText(listing.GetRect(30f), $"Category Filter: {filterCategory}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("All", () => filterCategory = "All")
                };
                options.AddRange(PerformanceMonitor.GetCategoryBreakdown().Keys
                    .Select(cat => new FloatMenuOption(cat, () => filterCategory = cat)));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (Widgets.ButtonText(listing.GetRect(30f), $"Mod Filter: {filterMod}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("All", () => filterMod = "All")
                };
                options.AddRange(PerformanceMonitor.GetModBreakdown().Keys
                    .Select(mod => new FloatMenuOption(mod, () => filterMod = mod)));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.CheckboxLabeled(" Show Only Above Alert Threshold", ref showOnlyAboveThreshold);
            listing.Label($"Alert Threshold (ms): {(float)userAlertThreshold:F1}");
            userAlertThreshold = listing.Slider((float)userAlertThreshold, 0.1f, 10.0f);

            listing.Label($"Spike Threshold (ms): {(float)userSpikeThreshold:F1}");
            userSpikeThreshold = listing.Slider((float)userSpikeThreshold, 1.0f, 20.0f);
            PerformanceMonitor.SpikeThresholdMs = userSpikeThreshold;
            PerformanceMonitor.AlertThresholdMs = userAlertThreshold;

            listing.GapLine();

            // Control Buttons
            if (listing.ButtonText("Pause / Resume"))
            {
                if (PerformanceMonitor.IsPaused)
                    PerformanceMonitor.Resume();
                else
                    PerformanceMonitor.Pause();
            }

            if (listing.ButtonText("Reset All Data"))
                PerformanceMonitor.ResetData();

            if (listing.ButtonText("Save Current as Baseline"))
                PerformanceMonitor.SaveBaseline();

            listing.CheckboxLabeled(" Sound Alerts", ref soundAlerts);
            listing.CheckboxLabeled(" Dark Theme", ref darkTheme);

            listing.End();
        }
        private void DrawAdvisorFooter(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // Advisor Recommendations
            listing.Label("RimCoach Top 5 Recommendations:");
            var recommendations = PerformanceAdvisor.GetTopRecommendations();
            if (recommendations != null && recommendations.Any())
            {
                foreach (var tip in recommendations.Take(5))
                {
                    listing.Label($"• {tip}");
                }
            }
            else
            {
                listing.Label("No major issues detected. Your colony is running well!");
            }

            listing.GapLine();

            // Inline Tooltips for explanation
            listing.Label("🛈 Tip: Hover over any column header to learn what it means.");
            listing.Label("🛈 Alert Threshold: Advisor warns when methods exceed this time.");
            listing.Label("🛈 Spike Threshold: Advisor logs sudden high-time events.");
            listing.Label("🛈 Baseline: Lets you compare before/after mod changes.");

            listing.GapLine();

            // Footer credits
            listing.Label($"RimCoach overhead: {PerformanceMonitor.GetMonitorOverhead():F2} ms (keeping things smooth!)");
            listing.Label("RimCoach by snues. Leave feedback on the Steam Workshop page!");

            listing.End();
        }
    }
}
