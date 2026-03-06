using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using MilkCum.Core.Utils;
using MilkCum.Harmony;

namespace MilkCum.Core;

[StaticConstructorOnStartup]
public static class ModInit
{
    static ModInit()
    {
        LongEventHandler.QueueLongEvent(() => { EventHelper.TriggerPostLoadLong(); }, "MilkCum_LongEvent", false, null);

        EventHelper.OnPostLoadLong += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnPostNewGame += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnPostLoadGame += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnSettingsChanged += GeneHelper.ReloadImpliedGenes;
        EventHelper.OnPostLoadLong += Init;

        Harmony Harmony = EqualMilkingMod.Harmony;
        Assembly asm = typeof(ModInit).Assembly;
        Type[] types;
        try
        {
            types = asm.GetTypes();
        }
        catch (ReflectionTypeLoadException rtle)
        {
            types = rtle.Types ?? Array.Empty<Type>();
        }
        foreach (Type type in types)
        {
            if (type == null) continue;
            bool hasHarmonyPatch;
            try
            {
                hasHarmonyPatch = type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0;
            }
            catch (AmbiguousMatchException)
            {
                hasHarmonyPatch = true;
            }
            if (!hasHarmonyPatch) continue;
            try
            {
                new PatchClassProcessor(Harmony, type).Patch();
            }
            catch (AmbiguousMatchException)
            {
                Log.Warning($"[MilkCum] Type {type.FullName} has multiple [HarmonyPatch] attributes; PatchClassProcessor does not support that. Skipping auto patch for this type.");
            }
            catch (Exception ex)
            {
                Log.Error($"[MilkCum] Patch failed for {type.FullName}: {ex.Message}");
            }
        }
        WorkGiver_Ingest_MilkProductFilter.ApplyOptionalPatches(EqualMilkingMod.Harmony);
        JobDriver_Ingest_MilkProductCheck.ApplyOptionalPatches(EqualMilkingMod.Harmony);
        ProlactinAddictionPatch.ApplyIfPossible(EqualMilkingMod.Harmony);
        CumpilationIntegration.ApplyPatches(EqualMilkingMod.Harmony);
    }

    public static void Init()
    {
        JobDefOf.Milk.driverClass = typeof(JobDriver_EquallyMilk);
        DefDatabase<WorkGiverDef>.GetNamed("Milk").giverClass = typeof(WorkGiver_EquallyMilk);
        if (EMDefOf.EM_MilkEntity != null)
            EMDefOf.EM_MilkEntity.giverClass = typeof(WorkGiver_EquallyMilkEntity);
        HediffDefOf.Lactating.hediffClass = typeof(HediffWithComps_EqualMilkingLactating);
        if (EMDefOf.EM_Prolactin_Tolerance != null)
            EMDefOf.EM_Prolactin_Tolerance.hediffClass = typeof(Hediff_ProlactinTolerance);
        if (EMDefOf.EM_LactatingGain != null)
            EMDefOf.EM_LactatingGain.hediffClass = typeof(Hediff_LactatingGain);

        if (DefDatabase<ThingDef>.GetNamedSilentFail("VCE_Cheese") is ThingDef cheese)
        {
            ThingDef humanMilkCheese = DefDatabase<ThingDef>.GetNamed("VCE_HumanMilkCheese");
            humanMilkCheese.label = Lang.Join(Lang.Human, Lang.Milk, cheese.label);
            humanMilkCheese.description = cheese.description;
        }
    }
}
