using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimCoach
{
    [StaticConstructorOnStartup]
    public static class DynamicPatches
    {
        private static readonly Harmony harmony = new Harmony("snues.RimCoach.DynamicPatches");
        private static readonly List<PatchDef> patchDefs = new List<PatchDef>();

        static DynamicPatches()
        {
            LoadPatchDefs();
            ApplyAllPatches();
        }

        private static void LoadPatchDefs()
        {
            try
            {
                patchDefs.Clear();
                patchDefs.AddRange(DefDatabase<PatchDef>.AllDefs);
                Log.Message($"[RimCoach] Loaded {patchDefs.Count} PatchDefs.");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimCoach] Error loading PatchDefs: {ex}");
            }
        }
        private static void ApplyAllPatches()
        {
            foreach (var patch in patchDefs)
            {
                try
                {
                    if (string.IsNullOrEmpty(patch.targetClass) || string.IsNullOrEmpty(patch.targetMethod))
                    {
                        Log.Warning($"[RimCoach] Skipped invalid PatchDef: {patch.defName} (missing targetClass or targetMethod).");
                        continue;
                    }

                    Type targetType = GenTypes.GetTypeInAnyAssembly(patch.targetClass);
                    if (targetType == null)
                    {
                        Log.Warning($"[RimCoach] Could not find target class: {patch.targetClass} for PatchDef: {patch.defName}.");
                        continue;
                    }

                    MethodInfo targetMethod = AccessTools.Method(targetType, patch.targetMethod);
                    if (targetMethod == null)
                    {
                        Log.Warning($"[RimCoach] Could not find target method: {patch.targetMethod} in {patch.targetClass}.");
                        continue;
                    }

                    MethodInfo prefix = typeof(DynamicPatches).GetMethod(nameof(RecordPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefix));

                    Log.Message($"[RimCoach] Patched {patch.targetClass}.{patch.targetMethod} successfully.");
                }
                catch (Exception ex)
                {
                    Log.Error($"[RimCoach] Exception while patching {patch?.defName}: {ex}");
                }
            }
        }
        private static void RecordPrefix(MethodBase __originalMethod)
        {
            try
            {
                if (__originalMethod == null) return;

                string key = __originalMethod.DeclaringType?.FullName + "." + __originalMethod.Name;
                var def = patchDefs.FirstOrDefault(p =>
                    p.targetClass == __originalMethod.DeclaringType?.FullName &&
                    p.targetMethod == __originalMethod.Name);

                if (def == null)
                {
                    Log.Warning($"[RimCoach] No matching PatchDef found for {key}.");
                    return;
                }

                double startTime = Time.realtimeSinceStartup * 1000;
                PerformanceMonitor.RecordSample(key, startTime, def);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimCoach] Error in RecordPrefix: {ex}");
            }
        }
    }
}
