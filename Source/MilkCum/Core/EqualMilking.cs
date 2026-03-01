using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using MilkCum.Milk.Helpers;
using MilkCum.Milk.HarmonyPatches;
using MilkCum.Milk.Jobs;
using MilkCum.Milk.Givers;
using MilkCum.Milk.Comps;

namespace MilkCum.Core;

[StaticConstructorOnStartup]
public static class EqualMilking
{
    static EqualMilking()
    {
        LongEventHandler.QueueLongEvent(() => { EventHelper.TriggerPostLoadLong(); }, "EqualMilking_LongEvent", false, null);

        EventHelper.OnPostLoadLong += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnPostNewGame += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnPostLoadGame += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnSettingsChanged += GeneHelper.ReloadImpliedGenes;
        EventHelper.OnPostLoadLong += Init;
        // Patch vanilla：逐类 Patch，排除 Hediff_TipString_BreastPool_Patch（目标 get_TipString 在部分 RimWorld 版本不存在，改由 ApplyIfPossible 按需打）
        Harmony harmony = EqualMilkingMod.Harmony;
        Assembly asm = typeof(EqualMilking).Assembly;
        const string skipTypeFullName = "MilkCum.Milk.HarmonyPatches.Hediff_TipString_BreastPool_Patch";
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
            if (type.FullName == skipTypeFullName) continue;
            if (type == typeof(Hediff_TipString_BreastPool_Patch)) continue;
            if (type.GetCustomAttribute<HarmonyPatch>(false) == null) continue;
            try
            {
                new PatchClassProcessor(harmony, type).Patch();
            }
            catch (Exception ex)
            {
                Log.Error($"[MilkCum] Patch failed for {type.FullName}: {ex.Message}");
            }
        }
        Hediff_TipString_BreastPool_Patch.ApplyIfPossible(harmony);
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
        // 确保耐受/增益等 Def 的 hediffClass 在运行时被正确设置，避免 Def 加载时类型未解析导致 MakeHediff 时 type 为 null（ArgumentNullException: type）
        if (EMDefOf.EM_Prolactin_Tolerance != null)
            EMDefOf.EM_Prolactin_Tolerance.hediffClass = typeof(Hediff_ProlactinTolerance);
        if (EMDefOf.EM_LactatingGain != null)
            EMDefOf.EM_LactatingGain.hediffClass = typeof(Hediff_LactatingGain);
        StatCategoryDefOf.AnimalProductivity.displayAllByDefault = true;

        // label auto translations
        if (DefDatabase<ThingDef>.GetNamedSilentFail("VCE_Cheese") is ThingDef cheese)
        {
            ThingDef humanMilkCheese = DefDatabase<ThingDef>.GetNamed("VCE_HumanMilkCheese");
            humanMilkCheese.label = Lang.Join(Lang.Human, Lang.Milk, cheese.label);
            humanMilkCheese.description = cheese.description;
        }
    }
}
