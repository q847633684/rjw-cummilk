using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Lactation.Helpers;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MilkCum.Harmony;

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
    /// <summary>Replace CompMilkable with CompEquallyMilkable；仅改 compClass，不阻止原构造执行，兼容未来原版/第三方构造逻辑。</summary>
    [HarmonyPostfix]
    public static void Postfix(CompProperties_Milkable __instance)
    {
        __instance.compClass = typeof(CompEquallyMilkable);
    }
}

[HarmonyPatch(typeof(ThingWithComps))]
public static class ThingWithComps_Patch
{
    /// <summary>Ensure EquallyMilkable Comp on Pawn；ExtensionHelper.CompEquallyMilkable() 内部已 if (TryGetComp==null) AddComp，不会重复添加。</summary>
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
    /// 设计原则 2/7：仅 Prefix 挂接，不替换原版 DoBirthSpawn。分娩结束时为母亲添加 Lactating 并调用 ApplyBirthPoolValues；动物与人类（Biotech）统一处理。
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Hediff_Pregnant.DoBirthSpawn))]
    public static void DoBirthSpawn_Prefix(Pawn mother)
    {
        if (mother.IsNormalAnimal())
        {
            mother.health.GetOrAddHediff(HediffDefOf.Lactating, mother.GetBreastOrChestPart());
            PoolModelBirthHelper.ApplyBirthPoolValues(mother);
            MilkCumSettings.LactationLog($"Birth lactation (animal): {mother.Name}");
            return;
        }
        if (mother.RaceProps?.Humanlike == true && mother.health?.hediffSet != null
            && !mother.health.hediffSet.HasHediff(HediffDefOf.Lactating))
        {
            mother.health.GetOrAddHediff(HediffDefOf.Lactating, mother.GetBreastOrChestPart());
        }
        // 人类分娩：若已有 Lactating（如药物）也叠加剩余天数+10、当前泌乳量+基础值
        if (mother.RaceProps?.Humanlike == true)
        {
            PoolModelBirthHelper.ApplyBirthPoolValues(mother);
            MilkCumSettings.LactationLog($"Birth lactation (humanlike): {mother.Name}");
        }
        MilkPermissionExtensions.TryGiveFirstLactationBirthMemory(mother);
    }
}
[HarmonyPatch(typeof(Need_Food))]
public static class Need_Food_Patch
{
    /// <summary>为饱食度悬停提示追加「饥饿率」拆解：当前饥饿档位、各 hediff 的 hungerRateFactor/hungerRateFactorOffset、最终乘数。本 mod 的 Lactating 已不用饥饿率，但可显示其他 hediff（如戒断）的影响。</summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Need_Food.GetTipString))]
    public static void GetTipString_PostFix(Need_Food __instance, ref string __result)
    {
        try
        {
            if (__instance?.pawn is not Pawn pawn || !pawn.IsMilkable()) return;
            if (pawn.health?.hediffSet == null || pawn.needs?.food == null) return;
            string hungerLabel = Lang.HungerRate;
            if (!string.IsNullOrEmpty(__result) && !string.IsNullOrEmpty(hungerLabel) && __result.Contains(hungerLabel))
                return;
            __result += "\n" + hungerLabel + ": ";
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
        catch (Exception ex)
        {
            Log.Warning($"[MilkCum] Need_Food.GetTipString postfix: {ex.Message}");
        }
    }
}

/// <summary>Lactating 被任意方式移除时（含其他 mod 直接 RemoveHediff）同步清空双池，保证池与 hediff 一致。本 mod 内通过 ResetAndRemoveLactating 会先 ClearPools 再 RemoveHediff，此处兜底。</summary>
[HarmonyPatch(typeof(Pawn_HealthTracker))]
[HarmonyPatch(nameof(Pawn_HealthTracker.RemoveHediff))]
public static class HediffSet_Remove_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn_HealthTracker __instance, Hediff hediff)
    {
        if (hediff?.def != HediffDefOf.Lactating) return;
        Pawn pawn = hediff.pawn;
        if (pawn == null) return;
        MilkCumSettings.LactationLog($"Lactating removed, ClearPools: {pawn.Name}");
        var comp = pawn.CompEquallyMilkable();
        if (comp != null)
            comp.ClearPools();
        PawnMilkStateExtensions.ClearLactatingStateCache(pawn);
    }
}

// AnimalProductionUtility_Patch / RaceProperties.SpecialDisplayStats 补丁已移除，见 ADR-004。右键喂奶与注射由 FloatMenuOptionProvider_* 提供。
/// <summary>设计原则 1：耐受/成瘾交给原版。本补丁仅挂接服用催乳剂后的「水池模型」逻辑（移除刚加的 Lactating、入队延迟、AddFromDrug）；成瘾判定与概率、耐受增减由原版 ChemicalDef + CompProperties_Drug + outcomeDoers/SeverityPerDay 驱动，此处不手写。</summary>
public static class ProlactinAddictionPatch
{
    public static void ApplyIfPossible(HarmonyLib.Harmony harmony)
    {
        try
        {
            var postfix = typeof(ProlactinAddictionPatch).GetMethod(nameof(DoIngestionOutcome_Postfix), BindingFlags.Public | BindingFlags.Static);
            if (postfix == null) return;

            // 1.6: Harmony 要求对“声明的方法”打补丁，不能对子类 override 打补丁；故只 patch 基类 IngestionOutcomeDoer.DoIngestionOutcome。
            var method = AccessTools.Method(typeof(IngestionOutcomeDoer), nameof(IngestionOutcomeDoer.DoIngestionOutcome),
                new[] { typeof(Pawn), typeof(Thing), typeof(int) });
            if (method == null)
                return;
            harmony.Patch(method, postfix: new HarmonyLib.HarmonyMethod(postfix));
        }
        catch (System.Exception ex)
        {
            Verse.Log.Warning($"[EqualMilking] ProlactinAddictionPatch.ApplyIfPossible failed (ingestion postfix skipped): {ex.Message}");
        }
    }

    public static void DoIngestionOutcome_Postfix(IngestionOutcomeDoer __instance, Pawn pawn, Thing ingested, int ingestedCount)
    {
        if (__instance is not IngestionOutcomeDoer_GiveHediff giveHediff || giveHediff.hediffDef != HediffDefOf.Lactating || pawn?.health?.hediffSet == null)
            return;
        var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) as HediffWithComps;
        if (hediff == null)
            return;
        // 耐受：本剂在 XML 中在 Lactating 之后添加，故当前已含本剂 +ProlactinToleranceGainPerDose。吃药前 t_before = 当前 - 该值。
        float tBefore = Mathf.Max(0f, MilkCumSettings.GetProlactinTolerance(pawn) - MilkCumSettings.ProlactinToleranceGainPerDose);
        float rawSeverity = giveHediff.severity;
        float effectiveSeverity = rawSeverity * MilkCumSettings.GetProlactinToleranceFactor(tBefore);

        // 已处于泌乳期时再次注射：直接生效，不走吸收延迟。若本 tick TryMergeWith 已对本 hediff 加过 raw（L+=raw×C, Severity+=raw），则补正为 Severity += (effective−raw)、L += (deltaS−raw)×C，即先 AddFromDrug(effective−raw) 再 AddFromDrug(deltaS−effective, false)。
        if (hediff.Severity > rawSeverity)
        {
            float raceMult = MilkCumSettings.GetRaceDrugDeltaSMultiplier(pawn);
            float deltaS = effectiveSeverity * raceMult;
            if (hediff.comps != null)
            {
                foreach (var c in hediff.comps)
                {
                    if (c is HediffComp_EqualMilkingLactating comp)
                    {
                        if (comp.MergedFromIngestionThisTick)
                        {
                            comp.AddFromDrug(effectiveSeverity - rawSeverity);
                            comp.AddFromDrug(deltaS - effectiveSeverity, syncSeverity: false);
                        }
                        else
                            comp.AddFromDrug(deltaS);
                        comp.MergedFromIngestionThisTick = false;
                        break;
                    }
                }
            }
            WorldComponent_MilkCumAbsorptionDelay.ApplyProlactinMoodEffects(pawn, rawSeverity);
            return;
        }

        // 水池模型吸收延迟：先移除刚加的 Lactating，到点时再挂并 AddFromDrug(effectiveSeverity×种族倍率)
        pawn.health.RemoveHediff(hediff);
        var world = Find.World;
        var delayComp = world?.GetComponent<WorldComponent_MilkCumAbsorptionDelay>();
        if (delayComp != null)
        {
            int endTick = Find.TickManager.TicksGame + WorldComponent_MilkCumAbsorptionDelay.GetAbsorptionDelayTicks(pawn);
            delayComp.ScheduleLactating(pawn, effectiveSeverity, endTick);
        }
        else
        {
            // 无 World 时（如测试）立即生效：Δs = effectiveSeverity × 种族倍率
            float deltaS = effectiveSeverity * MilkCumSettings.GetRaceDrugDeltaSMultiplier(pawn);
            MilkCumSettings.LactationLog($"Prolactin immediate apply: {pawn?.Name}, deltaS={deltaS:F3}");
            var reapply = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart()) as HediffWithComps;
            if (reapply?.comps != null)
                foreach (var c in reapply.comps)
                    if (c is HediffComp_EqualMilkingLactating comp) { comp.AddFromDrug(deltaS); break; }
            WorldComponent_MilkCumAbsorptionDelay.ApplyProlactinMoodEffects(pawn, rawSeverity);
            WorldComponent_MilkCumAbsorptionDelay.TryRecordProlactinIngestion(pawn);
        }
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

/// <summary>健康页乳房悬停：构建单对乳房的完整块（【标题】总览 + 左/右乳分行 + 因子）。</summary>
internal static class BreastPoolTooltipHelper
{
    private const string Tab = "  ";

    /// <summary>构建一对乳房的 tooltip 块：【sectionLabel】、总奶量/容量/效率、左乳/右乳分行与因子。</summary>
    public static string BuildBreastPairBlock(string sectionLabel, List<(string key, float fullness, float capacity, bool isLeft)> entries, string poolKey, Pawn pawn)
    {
        if ((entries?.Count ?? 0) == 0 || string.IsNullOrEmpty(poolKey) || pawn == null) return "";
        var comp = pawn.LactatingHediffComp();
        if (comp == null) return "";
        var left = entries.FirstOrDefault(e => e.isLeft);
        var right = entries.FirstOrDefault(e => !e.isLeft);
        float leftFull = left.capacity >= 0.001f ? left.fullness : 0f;
        float leftCap = Mathf.Max(0.001f, left.capacity);
        float rightFull = right.capacity >= 0.001f ? right.fullness : 0f;
        float rightCap = Mathf.Max(0.001f, right.capacity);
        float totalMilk = leftFull + rightFull;
        float totalBaseCap = leftCap + rightCap;
        float totalStretchCap = leftCap * PoolModelConstants.StretchCapFactor + rightCap * PoolModelConstants.StretchCapFactor;
        var (flowL, flowR, multL, multR) = pawn.GetFlowPerDayForBreastPair(poolKey);
        float flowPair = flowL + flowR;
        float letdownL = comp.GetLetdownReflexFlowMultiplier(poolKey + "_L");
        float letdownR = comp.GetLetdownReflexFlowMultiplier(poolKey + "_R");
        float pressureL = pawn.GetPressureFactorForSide(poolKey + "_L");
        float pressureR = pawn.GetPressureFactorForSide(poolKey + "_R");
        float conditionsL = pawn.GetConditionsForSide(poolKey + "_L");
        float conditionsR = pawn.GetConditionsForSide(poolKey + "_R");
        var b = comp.GetFlowPerDayBreakdown();
        string leftPercent = leftCap >= 0.001f ? (leftFull / leftCap).ToStringPercent() : "0%";
        string rightPercent = rightCap >= 0.001f ? (rightFull / rightCap).ToStringPercent() : "0%";
        float leftStretch = leftCap * PoolModelConstants.StretchCapFactor;
        float rightStretch = rightCap * PoolModelConstants.StretchCapFactor;
        string totalPercent = totalBaseCap >= 0.001f ? (totalMilk / totalBaseCap).ToStringPercent() : "0%";
        var factorLinesL = HediffComp_EqualMilkingLactating.BuildBreastEfficiencyFactorLinesForDevMode(b, multL, true, letdownL, pressureL, conditionsL);
        var factorLinesR = HediffComp_EqualMilkingLactating.BuildBreastEfficiencyFactorLinesForDevMode(b, multR, false, letdownR, pressureR, conditionsR);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("EM.PoolBreastSectionHeader".Translate(sectionLabel));
        sb.AppendLine();
        sb.AppendLine("EM.PoolSectionStorage".Translate());
        sb.Append(Tab).AppendLine("EM.PoolBreastTotalMilkLine".Translate(
            totalMilk.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
            totalBaseCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
            totalPercent));
        sb.Append(Tab).AppendLine("EM.PoolBreastStretchCapLine".Translate(totalStretchCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
        sb.AppendLine();
        sb.AppendLine("EM.PoolSectionFlow".Translate());
        sb.Append(Tab).AppendLine("EM.PoolBreastTotalFlowLine".Translate(flowPair.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
        sb.AppendLine();
        sb.AppendLine("EM.PoolLeftBreast".Translate());
        sb.Append(Tab).AppendLine("EM.PoolBreastSideMilkLine".Translate(
            leftFull.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
            leftCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
            leftPercent));
        sb.Append(Tab).AppendLine("EM.PoolBreastSideFlowLine".Translate(flowL.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
        sb.Append(Tab).AppendLine("EM.PoolBreastSideCapMax".Translate(leftStretch.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
        if (Verse.Prefs.DevMode)
        {
            sb.Append(Tab).AppendLine("EM.PoolBreastDevModeOnly".Translate());
            sb.Append(Tab).AppendLine("EM.PoolBreastMechanismLabel".Translate());
            foreach (string line in factorLinesL)
                sb.Append(Tab).Append(Tab).AppendLine(line);
        }
        sb.AppendLine();
        sb.AppendLine("EM.PoolRightBreast".Translate());
        sb.Append(Tab).AppendLine("EM.PoolBreastSideMilkLine".Translate(
            rightFull.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
            rightCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
            rightPercent));
        sb.Append(Tab).AppendLine("EM.PoolBreastSideFlowLine".Translate(flowR.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
        sb.Append(Tab).AppendLine("EM.PoolBreastSideCapMax".Translate(rightStretch.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
        if (Verse.Prefs.DevMode)
        {
            sb.Append(Tab).AppendLine("EM.PoolBreastDevModeOnly".Translate());
            sb.Append(Tab).AppendLine("EM.PoolBreastMechanismLabel".Translate());
            foreach (string line in factorLinesR)
                sb.Append(Tab).Append(Tab).AppendLine(line);
        }
        return sb.ToString();
    }
}

/// <summary>在 RJW 乳房行悬停（HediffComp_SexPart.CompTipStringExtra）后追加奶池块。仅当「有泌乳且当前为 RJW 乳房行」时追加；没有泌乳不追加，非乳房行不追加（genitalFamily != Breasts 已 return）。若 __result 中已包含本块（同一 getter 被调用两次并拼接时会重复），则不再追加以免同一悬浮内出现两份相同内容。</summary>
[HarmonyPatch(typeof(HediffComp_SexPart), "get_CompTipStringExtra")]
public static class HediffComp_SexPart_CompTipStringExtra_Patch
{
    [HarmonyPostfix]
    public static void Postfix(HediffComp_SexPart __instance, ref string __result)
    {
        if (__instance?.parent == null) return;
        if (__instance.Def.genitalFamily != GenitalFamily.Breasts) return;
        Hediff parent = __instance.parent;
        if (parent?.pawn == null) return;
        try
        {
            Pawn pawn = parent.pawn;
            if (pawn.CompEquallyMilkable() == null || !pawn.IsLactating()) return;
            string sectionLabel = parent.LabelCap;
            string blockHeader = "EM.PoolBreastSectionHeader".Translate(sectionLabel);
            if (!string.IsNullOrEmpty(__result) && __result.Contains(blockHeader)) return;
            string poolKey = pawn.GetPoolKeyForBreastHediff(parent);
            if (string.IsNullOrEmpty(poolKey)) return;
            var entries = pawn.GetPoolEntriesForBreastHediff(parent);
            if (entries.Count == 0) return;
            string block = BreastPoolTooltipHelper.BuildBreastPairBlock(sectionLabel, entries, poolKey, pawn);
            if (!string.IsNullOrEmpty(block))
                __result = (__result ?? "") + "\n" + block;
        }
        catch (Exception ex)
        {
            Log.Warning($"[MilkCum] HediffComp_SexPart CompTipStringExtra breast pool append: {ex.Message}");
        }
    }
}

/// <summary>未满一瓶的人奶：食用时营养按 CompPartialMilk.fillAmount 计算（池单位），不按 Def 的 Nutrition。目标方法可能为 Thing.GetStatValue(StatDef) 或 (StatDef,int)，或为扩展方法不在 Thing 上；由 ModInit 在找到目标时手动 Patch。</summary>
public static class Thing_GetStatValue_PartialMilkNutrition_Patch
{
    public static void ApplyIfPossible(HarmonyLib.Harmony harmony)
    {
        MethodBase target = AccessTools.Method(typeof(Thing), "GetStatValue", new[] { typeof(StatDef), typeof(int) })
            ?? AccessTools.Method(typeof(Thing), "GetStatValue", new[] { typeof(StatDef) });
        if (target == null) return;
        var prefix = new HarmonyMethod(typeof(Thing_GetStatValue_PartialMilkNutrition_Patch), nameof(Prefix));
        harmony.Patch(target, prefix: prefix);
    }

    public static bool Prefix(Thing __instance, StatDef stat, ref float __result)
    {
        if (stat != StatDefOf.Nutrition) return true;
        var comp = __instance.TryGetComp<Fluids.Lactation.Comps.CompPartialMilk>();
        if (comp == null || comp.fillAmount <= 0f) return true;
        __result = comp.fillAmount;
        return false;
    }
}
