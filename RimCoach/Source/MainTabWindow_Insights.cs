using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimCoach
{
    public class MainTabWindow_Insights : MainTabWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private bool darkTheme = false;
        private bool showCoachTips = true;
        private bool soundAlerts = true;

        private readonly Color darkBackground = new Color(0.1f, 0.1f, 0.1f);
        private readonly Color lightBackground = new Color(0.9f, 0.9f, 0.9f);

        public override Vector2 InitialSize => new Vector2(800f, 600f);

        public MainTabWindow_Insights()
        {
            this.absorbInputAroundWindow = true;
            this.preventCameraMotion = false;
            this.soundAppear = SoundDefOf.TabOpen;
            this.soundClose = SoundDefOf.TabClose;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            // Theme background
            GUI.color = darkTheme ? darkBackground : lightBackground;
            GUI.DrawTexture(inRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // Advisor Greeting / Header
            Text.Anchor = TextAnchor.MiddleCenter;
            listing.Label("💡 RimCoach Insights & Recommendations", 24f);
            Text.Anchor = TextAnchor.UpperLeft;
            listing.GapLine();

            // Friendly Coach Context
            listing.Label("Welcome, Overseer! RimCoach has analyzed your colony's performance and compiled targeted advice to keep things smooth and your colonists happy.");
            listing.Gap();

            // Baseline Comparison
            double baselineDiff = PerformanceMonitor.CompareToBaseline();
            string baselineLabel = baselineDiff > 0
                ? $"🛈 Compared to your baseline, performance is currently {baselineDiff:F1}% slower."
                : $"🛈 Great work! Performance is {Math.Abs(baselineDiff):F1}% faster than baseline.";
            listing.Label(baselineLabel);
            listing.GapLine();

            listing.End();
        }
        private void DrawTopRecommendations(Listing_Standard listing)
        {
            listing.Label("✅ Top 5 Recommendations from RimCoach:");

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
                listing.Label("RimCoach says: No critical issues detected. Your colony is running smoothly!");
            }

            listing.GapLine();
        }
        private void DrawProblemModsAndCategories(Listing_Standard listing)
        {
            listing.Label("⚠️ Detected Mods with Notable Impact:");

            var modBreakdown = PerformanceMonitor.GetModBreakdown();
            if (modBreakdown != null && modBreakdown.Any())
            {
                foreach (var kv in modBreakdown.OrderByDescending(kv => kv.Value).Take(10))
                {
                    listing.Label($"• {kv.Key}: {kv.Value:F2} ms total impact");
                }
            }
            else
            {
                listing.Label("No high-impact mods detected at this time.");
            }

            listing.GapLine();

            listing.Label("📊 Category Impact Breakdown:");
            var categoryBreakdown = PerformanceMonitor.GetCategoryBreakdown();
            if (categoryBreakdown != null && categoryBreakdown.Any())
            {
                foreach (var kv in categoryBreakdown.OrderByDescending(kv => kv.Value))
                {
                    listing.Label($"• {kv.Key}: {kv.Value:F2} ms total");
                }
            }
            else
            {
                listing.Label("No significant category impacts found.");
            }

            listing.GapLine();

            listing.Label("🛈 Coach Tip: Mods with high total ms can slow colony AI. Consider disabling or configuring them if performance drops.");
            listing.GapLine();
        }
        private void DrawFooterControls(Listing_Standard listing)
        {
            listing.GapLine();
            listing.Label("Settings:");

            listing.CheckboxLabeled(" Enable Coach Tips", ref showCoachTips);
            listing.CheckboxLabeled(" Sound Alerts", ref soundAlerts);
            listing.CheckboxLabeled(" Dark Theme", ref darkTheme);

            listing.GapLine();
            if (listing.ButtonText("Visit Steam Workshop Page for Feedback"))
            {
                Application.OpenURL("https://steamcommunity.com/app/294100/workshop/");
            }

            listing.Label("🛈 RimCoach is designed to help you optimize your colony without hassle. We value your feedback!");

            listing.GapLine();
            listing.Label("Made by snues. Leave a review or suggestion on our Steam Workshop page!");
        }
    }
}
