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

        EventHelper.OnPostLoadLong += MilkCumMod.Settings.UpdateMilkCumSettings;
        EventHelper.OnPostNewGame += MilkCumMod.Settings.UpdateMilkCumSettings;
        EventHelper.OnPostLoadGame += MilkCumMod.Settings.UpdateMilkCumSettings;
        EventHelper.OnSettingsChanged += GeneHelper.ReloadImpliedGenes;
        EventHelper.OnPostLoadLong += Init;

        HarmonyLib.Harmony Harmony = MilkCumMod.Harmony;
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
        WorkGiver_Ingest_MilkProductFilter.ApplyOptionalPatches(MilkCumMod.Harmony);
        JobDriver_Ingest_MilkProductCheck.ApplyOptionalPatches(MilkCumMod.Harmony);
        ProlactinAddictionPatch.ApplyIfPossible(MilkCumMod.Harmony);
        CumpilationIntegration.ApplyPatches(MilkCumMod.Harmony);
        MilkCum.Harmony.Compatibility.RjwMilkHumanWorkGiverPatch.ApplyPatches(MilkCumMod.Harmony);
    }

    public static void Init()
    {
        JobDefOf.Milk.driverClass = typeof(JobDriver_MilkCumMilk);
        DefDatabase<WorkGiverDef>.GetNamed("Milk").giverClass = typeof(WorkGiver_MilkCumMilk);
        if (MilkCumDefOf.EM_MilkEntity != null)
            MilkCumDefOf.EM_MilkEntity.giverClass = typeof(WorkGiver_MilkCumMilkEntity);
        HediffDefOf.Lactating.hediffClass = typeof(HediffWithComps_MilkCumLactating);
        if (MilkCumDefOf.EM_Prolactin_Tolerance != null)
            MilkCumDefOf.EM_Prolactin_Tolerance.hediffClass = typeof(Hediff_ProlactinTolerance);
        if (MilkCumDefOf.EM_LactatingGain != null)
            MilkCumDefOf.EM_LactatingGain.hediffClass = typeof(Hediff_LactatingGain);

        if (DefDatabase<ThingDef>.GetNamedSilentFail("VCE_Cheese") is ThingDef cheese)
        {
            ThingDef humanMilkCheese = DefDatabase<ThingDef>.GetNamed("VCE_HumanMilkCheese");
            humanMilkCheese.label = Lang.Join(Lang.Human, Lang.Milk, cheese.label);
            humanMilkCheese.description = cheese.description;
        }
    }
}
