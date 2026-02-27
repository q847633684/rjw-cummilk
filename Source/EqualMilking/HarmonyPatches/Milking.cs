using HarmonyLib;
using Verse;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using RimWorld;
using System.Linq;
using EqualMilking.Helpers;
using UnityEngine;
using Verse.AI;
namespace EqualMilking.HarmonyPatches;

[HarmonyPatch(typeof(CompMilkable))]
public static class CompMilkable_Patch
{
    //防止使用 Vanilla Milkable Comp，因为它已被 CompEquallyMilkable 取代
    //也禁用文本显示
    [HarmonyPrefix]
    [HarmonyPatch("Active", MethodType.Getter)]
    [HarmonyPriority(Priority.First)]
    public static bool Get_Active_Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}
[HarmonyPatch(typeof(CompProperties_Milkable))]
[HarmonyPatch(MethodType.Constructor)]
public static class CompProperties_Milkable_Patch
{
    /// <summary>
    /// Replace CompMilkable with CompEquallyMilkable
    /// </summary>
    public static bool Prefix(CompProperties_Milkable __instance)
    {
        __instance.compClass = typeof(CompEquallyMilkable);
        return false;
    }
}

[HarmonyPatch(typeof(ThingWithComps))]
public static class ThingWithComps_Patch
{
    /// <summary>
    /// Ensure initialization of EquallyMilkable Comp on ThingWithComps
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ThingWithComps.InitializeComps))]
    public static void InitializeComps_Post(ThingWithComps __instance)
    {
        __instance.CompEquallyMilkable();
    }

}
[HarmonyPatch(typeof(Hediff_Pregnant))]
public static class Hediff_Pregnant_Patch
{
    /// <summary>
    /// 7.8: 分娩结束时为母亲添加 Lactating。动物原有逻辑；人类（Biotech 原版分娩）也自动进入泌乳期。
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Hediff_Pregnant.DoBirthSpawn))]
    public static void DoBirthSpawn_Prefix(Pawn mother)
    {
        if (mother.IsNormalAnimal())
        {
            mother.health.AddHediff(HediffDefOf.Lactating);
            PoolModelBirthHelper.ApplyBirthPoolValues(mother);
            return;
        }
        if (mother.RaceProps?.Humanlike == true && mother.health?.hediffSet != null
            && !mother.health.hediffSet.HasHediff(HediffDefOf.Lactating))
        {
            mother.health.GetOrAddHediff(HediffDefOf.Lactating);
        }
        // 人类分娩：若已有 Lactating（如药物）也叠加剩余天数+10、当前泌乳量+基础值
        if (mother.RaceProps?.Humanlike == true)
            PoolModelBirthHelper.ApplyBirthPoolValues(mother);
    }
}
[HarmonyPatch(typeof(Hediff_LaborPushing))]
public static class Hediff_LaborPushing_Patch
{
    /// <summary>
    /// Allow mother to breastfeed babies after giving birth
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Hediff_LaborPushing.PreRemoved))]
    public static void PreRemoved_Postfix(Hediff_LaborPushing __instance)
    {
        if (__instance.pawn.IsLactating() && __instance.pawn.CompEquallyMilkable() is CompEquallyMilkable comp)
        {
            IEnumerable<Pawn> babies = __instance.pawn.relations.Children.Where(child => child.ageTracker.AgeBiologicalTicks < 100);
            if (babies.Any(baby => __instance.pawn.CanBreastfeedEver(baby)))
            {
                // 名单空时默认子女+伴侣可吸奶，无需再设 allowBreastFeeding
            }
        }
    }
}
[HarmonyPatch(typeof(Need_Food))]
public static class Need_Food_Patch
{
    /// <summary>
    /// Add hunger rate from lactating to food need tip string
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Need_Food.GetTipString))]
    public static void GetTipString_PostFix(Need_Food __instance, ref string __result)
    {
        if (__instance.pawn is not Pawn pawn || !pawn.IsMilkable()) { return; }
        if (!pawn.IsMilkable()) { return; }
        __result += "\n" + Lang.HungerRate + ": ";
        HungerCategory curCategory = pawn.needs.food.CurCategory;
        if (curCategory != HungerCategory.Fed)
        {
            __result += "\n   -" + curCategory.Label() + ": x" + HungerLevelUtility.HungerMultiplier(curCategory).ToStringPercent();
        }
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            HediffStage curStage = hediff?.CurStage;
            if (curStage == null) { continue; }
            if (curStage.hungerRateFactor != 1f)
            {
                __result += "\n   -" + hediff.LabelCap + ": x" + hediff.CurStage.hungerRateFactor.ToStringPercent();
            }
            if (curStage.hungerRateFactorOffset != 0f)
            {
                if (curStage.hungerRateFactorOffset > 0f)
                {
                    __result += "\n   -" + hediff.LabelCap + ": +" + hediff.CurStage.hungerRateFactorOffset.ToStringByStyle(ToStringStyle.PercentZero);
                }
                else
                {
                    __result += "\n   -" + hediff.LabelCap + ": " + hediff.CurStage.hungerRateFactorOffset.ToStringByStyle(ToStringStyle.PercentZero);
                }
            }
        }
        __result += "\n" + "StatsReport_FinalValue".Translate() + " " + (pawn.health.hediffSet.GetHungerRateFactor() * HungerLevelUtility.HungerMultiplier(curCategory)).ToStringPercent();

    }
}
/// <summary>
/// Bypass vanilla milkable comp stat entries
/// </summary>
[HarmonyPatch]
public static class AnimalProductionUtility_Patch
{
    public static MethodInfo getPropertiesMethod = AccessTools.Method(typeof(ThingDef), nameof(ThingDef.GetCompProperties)).MakeGenericMethod(typeof(CompProperties_Milkable));
    public static MethodInfo getMilkablePropertiesMethod = AccessTools.Method(typeof(AnimalProductionUtility_Patch), nameof(GetMilkableProperties));
    public static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName(typeof(AnimalProductionUtility).FullName + "+<AnimalProductionStats>d__0"), "MoveNext");
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(getPropertiesMethod))
            {
                codes[i].opcode = OpCodes.Call;
                codes[i].operand = getMilkablePropertiesMethod;
                return codes.AsEnumerable();
            }
        }
        Log.Error("[Equal Milking] Failed to patch AnimalProductionUtility.AnimalProductionStats");
        return instructions;
    }
    public static CompProperties_Milkable GetMilkableProperties(ThingDef def) => null;
}
/// <summary>
/// Add accurate stat defs to all milkable pawns
/// </summary>
[HarmonyPatch(typeof(RaceProperties))]
public static class RaceProperties_Patch
{
    /// <summary>
    /// replace vanilla stat entries EM ones
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(RaceProperties.SpecialDisplayStats))]
    public static void SpecialDisplayStats_PostFix(ref IEnumerable<StatDrawEntry> __result, StatRequest req)
    {
        __result = GetSpecialDisplayStats(__result, req);
    }
    /// <summary>
    /// Generate the stat entries for milkable pawns
    /// </summary>
    private static IEnumerable<StatDrawEntry> GetSpecialDisplayStats(IEnumerable<StatDrawEntry> result, StatRequest req)
    {
        foreach (StatDrawEntry item in result) { yield return item; }
        Pawn pawn = req.Pawn ?? (req.Thing as Pawn);
        if (pawn == null) { yield break; }
        try
        {
            if (!pawn.IsMilkable() || pawn.MilkDef() == null) { yield break; }
            yield return new StatDrawEntry(StatCategoryDefOf.AnimalProductivity, "Stat_Animal_MilkType".Translate(), pawn.MilkDef().LabelCap, "Stat_Animal_MilkTypeDesc".Translate(), 9880, null, Gen.YieldSingle<Dialog_InfoCard.Hyperlink>(new Dialog_InfoCard.Hyperlink(pawn.MilkDef(), -1)), false, false);
            yield return new StatDrawEntry(StatCategoryDefOf.AnimalProductivity, "Stat_Animal_MilkAmount".Translate(), pawn.MilkAmount().ToStringByStyle(ToStringStyle.FloatOne), "Stat_Animal_MilkAmountDesc".Translate(), 9870, null, null, false, false);
            yield return new StatDrawEntry(StatCategoryDefOf.AnimalProductivity, "Stat_Animal_MilkGrowthTime".Translate(), "PeriodDays".Translate(pawn.MilkGrowthTime().ToStringByStyle(ToStringStyle.FloatTwo)), "Stat_Animal_MilkGrowthTimeDesc".Translate(), 9860, null, null, false, false);
            yield return new StatDrawEntry(StatCategoryDefOf.AnimalProductivity, "Stat_Animal_MilkPerYear".Translate(), pawn.MilkPerYear().ToStringByStyle(ToStringStyle.FloatOne), "Stat_Animal_MilkPerYearDesc".Translate(), 9850, null, null, false, false);
            yield return new StatDrawEntry(StatCategoryDefOf.AnimalProductivity, "Stat_Animal_MilkValue".Translate(), pawn.MilkMarketValue().ToStringMoney(), "Stat_Animal_MilkValueDesc".Translate(), 9840, null, null, false, false);
            yield return new StatDrawEntry(StatCategoryDefOf.AnimalProductivity, "Stat_Animal_MilkValuePerYear".Translate(), pawn.MilkMarketValuePerYear().ToStringMoney(), "Stat_Animal_MilkValuePerYearDesc".Translate(), 9830, null, null, false, false);
        }
        finally {}
    }
}
#if v1_5
[HarmonyPatch(typeof(FloatMenuMakerMap))]
public static class FloatMenuMakerMap_Patch
{
    /// <summary>
    /// Add breastfeed and inject options to humanlike RMB menu
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(FloatMenuMakerMap.AddHumanlikeOrders))]
    public static void AddHumanlikeOrders_PostFix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
    {
        using IEnumerator<Thing> enumerator = pawn.Map.thingGrid.ThingsAt(clickPos.ToIntVec3()).GetEnumerator();
        while (enumerator.MoveNext())
        {
            Thing thing = enumerator.Current;
            // Load entity on platform
            if (thing is Building_HoldingPlatform building_HoldingPlatform)
            {
                thing = building_HoldingPlatform.HeldPawn;
            }
            if (thing is Pawn target)
            {
                if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn)) { continue; }
                if (pawn != target)
                {
                    // Remove original breastfeed option
                    foreach (FloatMenuOption opt in opts.ToList())
                    {
                        if (opt.Label.StartsWith("BabyCareBreastfeedUnable".Translate(target.Named("BABY")))
                            || opt.Label.StartsWith("BabyCareBreastfeed".Translate(target.Named("BABY"))))
                        {
                            opts.Remove(opt);
                        }
                    }
                    opts.AddRange(target.BreastfeedMenuOptions(pawn));
                }
                if (target.ShouldShowInjectMenu())
                {
                    opts.AddRange(target.InjectMenuOptions(pawn));
                }

            }
        }
    }
}
#endif

// 成瘾机制增强补丁：仅在目标方法存在时手动应用，避免 DoIngestionOutcome 不存在时崩溃
public static class ProlactinAddictionPatch
{
    public static void ApplyIfPossible(HarmonyLib.Harmony harmony)
    {
        var type = typeof(IngestionOutcomeDoer_GiveHediff);
        var method = type.GetMethod("DoIngestionOutcome", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
            method = type.BaseType?.GetMethod("DoIngestionOutcome", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
            return;
        var postfix = typeof(ProlactinAddictionPatch).GetMethod(nameof(DoIngestionOutcome_Postfix), BindingFlags.Public | BindingFlags.Static);
        if (postfix != null)
            harmony.Patch(method, postfix: new HarmonyLib.HarmonyMethod(postfix));
    }

    public static void DoIngestionOutcome_Postfix(IngestionOutcomeDoer_GiveHediff __instance, Pawn pawn, Thing ingested)
    {
        if (__instance.hediffDef != HediffDefOf.Lactating || pawn?.health?.hediffSet == null)
            goto AddictionCheck;
        var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) as HediffWithComps;
        if (hediff == null)
            goto AddictionCheck;
        // 水池模型吸收延迟：先移除刚加的 Lactating，到点时再挂并 AddFromDrug
        float severity = __instance.severity;
        pawn.health.RemoveHediff(hediff);
        var world = Find.World;
        var delayComp = world?.GetComponent<WorldComponent_EqualMilkingAbsorptionDelay>();
        if (delayComp != null)
        {
            int endTick = Find.TickManager.TicksGame + WorldComponent_EqualMilkingAbsorptionDelay.GetAbsorptionDelayTicks(pawn);
            delayComp.ScheduleLactating(pawn, severity, endTick);
        }
        else
        {
            // 无 World 时（如测试）立即生效
            var reapply = pawn.health.GetOrAddHediff(HediffDefOf.Lactating) as HediffWithComps;
            if (reapply?.comps != null)
                foreach (var c in reapply.comps)
                    if (c is HediffComp_EqualMilkingLactating comp) { comp.AddFromDrug(severity); break; }
            WorldComponent_EqualMilkingAbsorptionDelay.ApplyProlactinMoodEffects(pawn, severity);
        }
AddictionCheck:
        if (ingested?.def == null || ingested.def.defName != "EM_Prolactin" || __instance.hediffDef != HediffDefOf.Lactating)
            return;
        HandleAddictionMechanics(pawn);
    }

    private static void HandleAddictionMechanics(Pawn pawn)
    {
        float addictionChance = 0.04f;
        var tolerance = pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Tolerance);
        if (tolerance != null)
            addictionChance *= (1 + tolerance.Severity);
        if (!Rand.Chance(addictionChance))
            return;
        if (pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Addiction) != null)
            return;
        var addiction = HediffMaker.MakeHediff(EMDefOf.EM_Prolactin_Addiction, pawn);
        pawn.health.AddHediff(addiction);
        var lactating = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
        if (lactating != null && lactating.Severity < 1f)
            lactating.Severity = 1f;
    }
}

/// <summary>水池模型：分娩时对 Lactating 应用剩余天数+=10、当前泌乳量+=基础值。</summary>
internal static class PoolModelBirthHelper
{
    public static void ApplyBirthPoolValues(Pawn mother)
    {
        var hediff = mother?.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating) as HediffWithComps;
        if (hediff?.comps == null) return;
        foreach (var c in hediff.comps)
        {
            if (c is HediffComp_EqualMilkingLactating comp)
            {
                comp.AddFromBirth();
                break;
            }
        }
    }
}