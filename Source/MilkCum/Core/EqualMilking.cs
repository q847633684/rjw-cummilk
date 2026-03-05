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
        // Patch vanilla：逐类 Patch，由 PatchClassProcessor 处理所有带 [HarmonyPatch] 的类
        Harmony harmony = EqualMilkingMod.Harmony;
        Assembly asm = typeof(EqualMilking).Assembly;
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
                new PatchClassProcessor(harmony, type).Patch();
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
        // 确保耐受/增益等 Def 的 hediffClass 在运行时被正确设置，避免 Def 加载时类型未解析导致 MakeHediff 时 type 为 null（ArgumentNullException: type）
        if (EMDefOf.EM_Prolactin_Tolerance != null)
            EMDefOf.EM_Prolactin_Tolerance.hediffClass = typeof(Hediff_ProlactinTolerance);
        if (EMDefOf.EM_LactatingGain != null)
            EMDefOf.EM_LactatingGain.hediffClass = typeof(Hediff_LactatingGain);
        // 不再设置 displayAllByDefault：否则打开物品/Def 信息卡（泌乳素、人奶、精液等）时会因该分类下部分 StatWorker 仅支持 Pawn 导致 ShouldShowFor(req) NRE
        // StatCategoryDefOf.AnimalProductivity.displayAllByDefault = true;

        // label auto translations
        if (DefDatabase<ThingDef>.GetNamedSilentFail("VCE_Cheese") is ThingDef cheese)
        {
            ThingDef humanMilkCheese = DefDatabase<ThingDef>.GetNamed("VCE_HumanMilkCheese");
            humanMilkCheese.label = Lang.Join(Lang.Human, Lang.Milk, cheese.label);
            humanMilkCheese.description = cheese.description;
        }
    }
}
