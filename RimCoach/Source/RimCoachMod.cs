using UnityEngine;
using Verse;

namespace RimCoach
{
    public class RimCoachMod : Mod
    {
        public static RimCoachSettings Settings;

        public RimCoachMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimCoachSettings>();
            Log.Message("[RimCoach] Mod initialized successfully.");
        }

        public override string SettingsCategory() => "RimCoach";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("RimCoach - Advisor Settings");
            listing.CheckboxLabeled(" Enable Coach Tips", ref RimCoachSettings.ShowCoachTips);
            listing.CheckboxLabeled(" Enable Sound Alerts", ref RimCoachSettings.SoundAlerts);
            listing.CheckboxLabeled(" Use Dark Theme", ref RimCoachSettings.DarkTheme);

            listing.GapLine();
            listing.Label("These settings affect all RimCoach tabs and Advisor recommendations.");

            listing.End();
            Settings.Write();
        }
    }

    public class RimCoachSettings : ModSettings
    {
        public static bool ShowCoachTips = true;
        public static bool SoundAlerts = true;
        public static bool DarkTheme = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ShowCoachTips, "ShowCoachTips", true);
            Scribe_Values.Look(ref SoundAlerts, "SoundAlerts", true);
            Scribe_Values.Look(ref DarkTheme, "DarkTheme", false);
        }
    }
}
