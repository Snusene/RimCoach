using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimCoach
{
    public static class PerformanceAdvisor
    {
        private static int lastMessageTick = 0;
        private static int advisorCooldownTicks = 600;
        private static List<string> recentAdviceHistory = new List<string>();
        private static int maxAdviceHistory = 20;

        private static readonly System.Random rng = new System.Random();

        // Confidence rating
        private static double advisorConfidence = 0.85;

        // Spike cache
        private static List<SpikeEvent> recentSpikes = new List<SpikeEvent>();

        public static double AdvisorConfidence => advisorConfidence;

        public static void UpdateConfidence(double currentPerformanceScore)
        {
            // Confidence increases with better performance
            advisorConfidence = Math.Min(1.0, Math.Max(0.5, 1.0 - (currentPerformanceScore / 100.0)));
        }

        public static bool IsCooldownReady()
        {
            return Find.TickManager.TicksGame - lastMessageTick > advisorCooldownTicks;
        }

        public static void RecordAdviceShown(string advice)
        {
            if (!recentAdviceHistory.Contains(advice))
            {
                recentAdviceHistory.Add(advice);
                if (recentAdviceHistory.Count > maxAdviceHistory)
                    recentAdviceHistory.RemoveAt(0);
            }
            lastMessageTick = Find.TickManager.TicksGame;
        }

        public static bool WasAdviceRecentlyGiven(string advice)
        {
            return recentAdviceHistory.Contains(advice);
        }
        public static string GetTipFor(ProfiledMethod method)
        {
            if (method == null) return "No data available.";

            if (WasAdviceRecentlyGiven(method.Key))
                return ""; // Avoid repeating advice too often

            var patchDef = method.Def;
            if (patchDef == null)
                return $"Tracking method: {method.Key} – ~{method.AverageTimeMs:F2} ms per call.";

            string tip = $"{patchDef.label ?? method.Key} averages ~{method.AverageTimeMs:F2} ms. ";

            if (!string.IsNullOrEmpty(patchDef.severityHint))
                tip += $"Severity: {patchDef.severityHint}. ";

            if (!string.IsNullOrEmpty(patchDef.modHint))
            {
                tip += $"Detected Mod: {patchDef.modHint}. ";
                tip += GetModSpecificAdvice(patchDef.modHint);
            }

            if (!string.IsNullOrEmpty(patchDef.category))
                tip += GetCategoryAdvice(patchDef.category);

            RecordAdviceShown(method.Key);

            return tip.Trim();
        }

        private static string GetCategoryAdvice(string category)
        {
            switch (category)
            {
                case "AI":
                    return "Colonists and animals will think more. Consider fewer pawns or simpler mods.";
                case "Jobs":
                    return "Colonists may take longer choosing work. Try fewer active tasks or simpler work types.";
                case "Pathfinding":
                    return "Complex bases increase path planning. Build simpler hallways or pen animals.";
                case "Health":
                    return "Many injuries or health effects can slow things down. Heal colonists and reduce health-heavy mods.";
                case "World Systems":
                    return "Extra factions and events increase load. Reduce world complexity if needed.";
                case "Harmony":
                    return "Many mods patch the same thing. Disable conflicting or redundant mods.";
                case "Performance":
                    return "Optimization mods can help or conflict. Use them carefully.";
                case "Framework":
                    return "Framework mods add systems. Keep them updated and remove unused ones.";
                case "Utility":
                    return "Extra tools can add small overhead. Disable unused utilities.";
                case "UI":
                    return "UI overlays may slightly slow the game. Turn off unnecessary overlays.";
                case "Overhaul":
                    return "Major gameplay changes need more resources. Consider simpler setups.";
                case "Content":
                    return "More items, animals, factions increase thinking time. Reduce spawns where possible.";
                default:
                    return "";
            }
        }
        private static string GetModSpecificAdvice(string modHint)
        {
            if (string.IsNullOrEmpty(modHint))
                return "";

            modHint = modHint.ToLowerInvariant();

            // Vanilla Expanded family
            if (modHint.Contains("vanilla animals expanded"))
                return "Adds many new animals. Keep herd sizes manageable to reduce thinking time.";
            if (modHint.Contains("vanilla furniture expanded"))
                return "Adds lots of new furniture. Can make pathfinding a bit slower in cluttered bases.";
            if (modHint.Contains("vanilla power expanded"))
                return "Adds complex power systems. Try simpler grids to reduce calculations.";
            if (modHint.Contains("vanilla fishing expanded"))
                return "Fishing zones add extra jobs. Limit active fishing areas if performance dips.";
            if (modHint.Contains("vanilla events expanded"))
                return "Extra events increase story processing. Disable unused events in mod settings if needed.";
            if (modHint.Contains("vanilla genetics expanded"))
                return "Genetics adds breeding complexity. Reduce breeding programs to lower load.";
            if (modHint.Contains("vanilla factions expanded"))
                return "More factions increase world events. Consider fewer factions for smoother play.";
            if (modHint.Contains("vanilla apparel expanded"))
                return "Extra clothing items. Usually minor, but can add crafting choices.";
            if (modHint.Contains("vanilla weapons expanded"))
                return "Extra weapons can add a bit of load if heavily used.";

            // Framework / Utility
            if (modHint.Contains("jecstools"))
                return "Framework used by other mods. Keep it updated, disable unused dependent mods.";
            if (modHint.Contains("rocketman"))
                return "Optimization mod. Usually helps but check for compatibility with other optimizers.";
            if (modHint.Contains("rimthreaded"))
                return "Experimental multithreading. Can reduce stutters but may conflict with some mods.";
            if (modHint.Contains("runtimegc"))
                return "Garbage collector cleaner. Usually safe, adjust settings as needed.";
            if (modHint.Contains("xml extensions"))
                return "Framework for mod settings. Usually no performance impact unless misconfigured.";
            // Big mods / Overhauls
            if (modHint.Contains("combat extended"))
                return "Advanced combat simulation. Can increase CPU usage in large battles.";
            if (modHint.Contains("save our ship"))
                return "Adds space simulation layers. Can be heavy on world tick load—simplify ships if needed.";
            if (modHint.Contains("rimefeller") || modHint.Contains("rimatomics"))
                return "Complex power systems. Watch power grid complexity to reduce calculations.";
            if (modHint.Contains("children"))
                return "Adds child AI. Limit class sizes and schooling areas to reduce thinking time.";
            if (modHint.Contains("giddy up"))
                return "Pawn riding adds pathfinding complexity. Pen animals or limit mount use.";
            if (modHint.Contains("colony manager"))
                return "Automatically manages jobs. Can add job scan overhead; limit managed categories if needed.";
            if (modHint.Contains("rimworld of magic"))
                return "Complex spells and AI. Monitor settings to keep performance reasonable.";
            if (modHint.Contains("pick up and haul"))
                return "Extra hauling job scans. Use smart zones to reduce scanning.";
            if (modHint.Contains("cleaning area"))
                return "Controls cleaning zones. Adds minor job scan cost.";

            // QoL / Utility
            if (modHint.Contains("numbers"))
                return "Displays large data tables. May slow large colonies slightly.";
            if (modHint.Contains("rimhud"))
                return "Overlay for pawns. Minor UI overhead in big colonies.";
            if (modHint.Contains("rpg style inventory"))
                return "Adds inventory UI. Minor load for many pawns.";
            if (modHint.Contains("interaction bubbles"))
                return "Adds speech bubbles. Slight UI overhead.";
            if (modHint.Contains("what the hack"))
                return "Hacked mechs have complex AI. Limit hacked mech count.";
            if (modHint.Contains("snap out"))
                return "Mental break intervention. Minimal overhead.";
            if (modHint.Contains("defensive positions"))
                return "Lets colonists remember positions. Very low impact.";
            if (modHint.Contains("stack xxl"))
                return "Increases stack size. Usually safe.";
            if (modHint.Contains("allow tool"))
                return "Adds designators. Can increase job scan times with many orders.";
            if (modHint.Contains("common sense"))
                return "Smarter jobs. May add small overhead. Limit complexity in settings.";
            if (modHint.Contains("smart medicine"))
                return "Automates medicine use. Watch for scanning in large maps.";
            // Vanilla Expanded Submodules
            if (modHint.Contains("vanilla animals expanded - livestock"))
                return "Adds livestock animals. Manage herd sizes to reduce pathfinding load.";
            if (modHint.Contains("vanilla animals expanded - insectoids"))
                return "Adds insectoid spawns. Can increase AI load. Limit spawn zones if laggy.";
            if (modHint.Contains("vanilla animals expanded - cats and dogs"))
                return "Adds pet animals. Usually low impact, but manage breeding.";
            if (modHint.Contains("vanilla animals expanded - arctic"))
                return "Adds arctic animals. Watch for large map spawns.";
            if (modHint.Contains("vanilla animals expanded - desert"))
                return "Adds desert animals. Keep herds manageable.";

            if (modHint.Contains("vanilla story"))
                return "Adds new storytellers. Usually safe, but extra events can increase processing.";
            if (modHint.Contains("hospitality addons"))
                return "Adds extra guest AI. Limit visitor tasks to reduce load.";
            if (modHint.Contains("more faction interaction"))
                return "Increases faction events. Can raise world event processing.";
            if (modHint.Contains("rimwar"))
                return "Adds dynamic world conflicts. Can increase world tick load. Consider smaller map setups.";
            if (modHint.Contains("medieval overhaul"))
                return "Adds many defs and jobs. Watch for crafting and job scanning overhead.";

            if (modHint.Contains("ve_medical module"))
                return "Adds medical systems. Watch for extra health calculations.";
            if (modHint.Contains("ve_farming"))
                return "Adds farming jobs and crops. Manage fields to reduce scanning.";
            if (modHint.Contains("ve_props"))
                return "Adds decorative props. Usually low impact.";
            if (modHint.Contains("ve_art"))
                return "Adds art content. Generally safe.";
            if (modHint.Contains("ve_scribes"))
                return "Adds writing jobs. Slight AI processing added.";
            // Vanilla Factions Expanded
            if (modHint.Contains("vanilla factions expanded - settlers"))
                return "Adds settler faction. Extra events and interactions may increase load.";
            if (modHint.Contains("vanilla factions expanded - insectoids"))
                return "Adds insectoid faction. Can increase spawns and event processing.";
            if (modHint.Contains("vanilla factions expanded - medieval"))
                return "Adds medieval factions. More world events and raids.";
            if (modHint.Contains("vanilla factions expanded - pirates"))
                return "Adds pirate factions. Can increase raid complexity.";
            if (modHint.Contains("vanilla factions expanded - vikings"))
                return "Adds viking factions. Extra raids and events may increase CPU.";

            // Vanilla Weapons Expanded
            if (modHint.Contains("vanilla weapons expanded - spacer"))
                return "Adds spacer weapons. Usually minor impact.";
            if (modHint.Contains("vanilla weapons expanded - laser"))
                return "Adds laser weapons. Usually minor CPU unless heavy use.";
            if (modHint.Contains("vanilla weapons expanded - heavy"))
                return "Adds heavy weapons. May increase projectile calculations.";
            if (modHint.Contains("vanilla weapons expanded - quickdraw"))
                return "Adds quickdraw weapons. Minimal impact.";
            if (modHint.Contains("vanilla weapons expanded - grenades"))
                return "Grenades may increase explosion calculations.";

            // Utility mods
            if (modHint.Contains("trading control"))
                return "Improves trader placement. Minimal impact but adds map component ticking.";
            if (modHint.Contains("tech advancing"))
                return "Scales research cost dynamically. May add minor world tick cost.";
            if (modHint.Contains("map designer"))
                return "Customizes map gen. Only affects map generation time.";
            if (modHint.Contains("alpha biomes"))
                return "Adds new biomes with plants and animals. Extra spawns can increase load.";
            if (modHint.Contains("rainbeau realistic planets"))
                return "Improves world map gen. Minor world tick increase.";

            return "";
        }
        public static List<string> GetTopRecommendations()
        {
            var recommendations = new List<string>();

            var topMethods = PerformanceMonitor.GetDisplayData()
                .Where(m => m.AverageTimeMs >= PerformanceMonitor.AlertThresholdMs)
                .OrderByDescending(m => m.AverageTimeMs)
                .Take(5);

            foreach (var method in topMethods)
            {
                string tip = GetTipFor(method);
                if (!string.IsNullOrWhiteSpace(tip))
                    recommendations.Add(tip);
            }

            if (!recommendations.Any())
                recommendations.Add("No critical issues detected. Your colony is running smoothly!");

            return recommendations;
        }

        public static string GenerateCoachSummary()
        {
            double totalBudget = PerformanceMonitor.GetTotalTickBudget();
            if (totalBudget <= 0)
                return "RimCoach: No significant performance issues detected.";

            var topCategory = PerformanceMonitor.GetCategoryBreakdown()
                .OrderByDescending(kv => kv.Value)
                .FirstOrDefault();

            var topMod = PerformanceMonitor.GetModBreakdown()
                .OrderByDescending(kv => kv.Value)
                .FirstOrDefault();

            string summary = $"Total CPU Budget: {totalBudget:F2}ms.";

            if (!string.IsNullOrEmpty(topCategory.Key))
                summary += $" Top category: {topCategory.Key} at {topCategory.Value:F2}ms.";

            if (!string.IsNullOrEmpty(topMod.Key))
                summary += $" Top mod impact: {topMod.Key} at {topMod.Value:F2}ms.";

            summary += $" Advisor Confidence: {(advisorConfidence * 100):F0}%.";

            return summary;
        }
    }
}
