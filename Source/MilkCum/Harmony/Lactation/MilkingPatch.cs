using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Cum.Common;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;
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

/// <summary>Lactating 被任意方式移除时（含其他 mod 直接 RemoveHediff）同步清空乳池，保证池与 hediff 一致。本 mod 内通过 ResetAndRemoveLactating 会先 ClearPools 再 RemoveHediff，此处兜底。</summary>
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

        // 已处于泌乳期时再次注射：直接生效，不走吸收延迟。原版已乘耐受时合并值≈effective，TryMergeWith 已把 other.Severity 同步到 L，此处不再补差。
        if (hediff.Severity > rawSeverity)
        {
            float raceMult = MilkCumSettings.GetRaceDrugDeltaSMultiplier(pawn);
            float deltaS = effectiveSeverity * raceMult;
            float remainingBefore = 0f;
            float lactationBefore = 0f;
            bool logIntake = MilkCumSettings.lactationDrugIntakeLog;

            if (hediff.comps != null)
            {
                foreach (var c in hediff.comps)
                {
                    if (c is HediffComp_EqualMilkingLactating comp)
                    {
                        if (logIntake)
                        {
                            remainingBefore = comp.RemainingDays;
                            lactationBefore = comp.CurrentLactationAmount;
                            comp.SuppressDrugIntakeLog = true;
                        }

                        bool wasMerged = comp.MergedFromIngestionThisTick;
                        float mergedSeverity = comp.LastMergedOtherSeverity;

                        if (comp.MergedFromIngestionThisTick)
                        {
                            MilkCumSettings.LactationLog($"[MilkCum.验证] 已泌乳再次服药 数据: Pawn={pawn?.LabelShort} Def_raw={rawSeverity:F3} 合并other.Severity={mergedSeverity:F3} 自算effective={effectiveSeverity:F3}");
                            MilkCumSettings.LactationLog("[MilkCum.验证] 解读: 原版已乘耐受，合并值≈effective，无需补差");
                            comp.LastMergedOtherSeverity = 0f;
                        }
                        else
                        {
                            comp.AddFromDrug(deltaS);
                        }

                        comp.MergedFromIngestionThisTick = false;
                        if (logIntake)
                        {
                            comp.SuppressDrugIntakeLog = false;
                            float remainingAfter = comp.RemainingDays;
                            float lactationAfter = comp.CurrentLactationAmount;
                            float eTol = MilkCumSettings.GetProlactinToleranceFactor(pawn);
                            float rawInferred = (eTol * raceMult > 1E-6f) ? (deltaS / (eTol * raceMult)) : 0f;
                            float totalDeltaL = wasMerged ? (mergedSeverity * PoolModelConstants.DoseToLFactor) : (lactationAfter - lactationBefore);
                            float deltaRemaining = remainingAfter - remainingBefore;
                            int tick = Find.TickManager.TicksGame;
                            MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] pawn={pawn?.LabelShort} tick={tick} state=AlreadyLactating merged={(wasMerged ? "ByVanillaMerge" : "Direct")}");
                            MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] input rawDef~={rawInferred:F3} Δs={deltaS:F3} E_tol={eTol:F3} raceMult={raceMult:F3} doseToL={PoolModelConstants.DoseToLFactor:F2}");
                            MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] result 本针合计ΔL={totalDeltaL:F3} remainBefore={remainingBefore:F1}d remainAfter={remainingAfter:F1}d Δremain={deltaRemaining:+0.0;-0.0;0.0}d");
                            if (wasMerged)
                                MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] 公式 合并加L={mergedSeverity:F3}×C_dose={mergedSeverity * PoolModelConstants.DoseToLFactor:F3}=合计ΔL；effective=raw({rawSeverity:F3})×E_tol({eTol:F3})={effectiveSeverity:F3} Δs=effective×种族({raceMult:F3})={deltaS:F3}；剩余={remainingBefore:F1}d+Δ天数({deltaRemaining:+0.0;-0.0;0.0})d={remainingAfter:F1}d");
                            else
                                MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] 公式 有效剂量effective=raw({rawSeverity:F3})×E_tol({eTol:F3})={effectiveSeverity:F3}；Δs=effective×种族({raceMult:F3})={deltaS:F3}；ΔL=Δs×C_dose={totalDeltaL:F3}；剩余={remainingBefore:F1}d+Δ天数({deltaRemaining:+0.0;-0.0;0.0})d={remainingAfter:F1}d");
                        }
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

/// <summary>健康页乳房悬停：构建乳房池详情块（【标题】总览 + 可用侧分行 + 因子）。</summary>
internal static class BreastPoolTooltipHelper
{
    private const string Tab = "  ";

    /// <summary>构建乳房池 tooltip：优先显示该乳房子部位对应的池；若有两侧条目则同时显示。</summary>
    public static string BuildBreastPoolBlock(string sectionLabel, List<(string key, float fullness, float capacity, bool isLeft)> entries, Pawn pawn)
    {
        if ((entries?.Count ?? 0) == 0 || pawn == null) return "";
        var comp = pawn.LactatingHediffComp();
        if (comp == null) return "";
        var left = entries.FirstOrDefault(e => e.isLeft && !string.IsNullOrEmpty(e.key));
        var right = entries.FirstOrDefault(e => !e.isLeft && !string.IsNullOrEmpty(e.key));
        bool hasLeft = !string.IsNullOrEmpty(left.key);
        bool hasRight = !string.IsNullOrEmpty(right.key);
        if (!hasLeft && !hasRight)
        {
            var any = entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.key));
            if (string.IsNullOrEmpty(any.key)) return "";
            left = any;
            hasLeft = true;
        }
        float leftFull = hasLeft && left.capacity >= 0.001f ? left.fullness : 0f;
        float leftCap = hasLeft ? Mathf.Max(0.001f, left.capacity) : 0f;
        float rightFull = hasRight && right.capacity >= 0.001f ? right.fullness : 0f;
        float rightCap = hasRight ? Mathf.Max(0.001f, right.capacity) : 0f;
        float totalMilk = leftFull + rightFull;
        float totalBaseCap = Mathf.Max(0.001f, leftCap + rightCap);
        float totalStretchCap = (leftCap + rightCap) * PoolModelConstants.StretchCapFactor;
        var (flowL, flowR, multL, multR) = pawn.GetFlowPerDayForBreastSides(left.key, right.key);
        float flowTotal = flowL + flowR;
        float letdownL = string.IsNullOrEmpty(left.key) ? 1f : comp.GetLetdownReflexFlowMultiplier(left.key);
        float letdownR = string.IsNullOrEmpty(right.key) ? 1f : comp.GetLetdownReflexFlowMultiplier(right.key);
        float pressureL = string.IsNullOrEmpty(left.key) ? 1f : pawn.GetPressureFactorForPool(left.key);
        float pressureR = string.IsNullOrEmpty(right.key) ? 1f : pawn.GetPressureFactorForPool(right.key);
        float conditionsL = string.IsNullOrEmpty(left.key) ? 1f : pawn.GetConditionsForPoolKey(left.key);
        float conditionsR = string.IsNullOrEmpty(right.key) ? 1f : pawn.GetConditionsForPoolKey(right.key);
        var b = comp.GetFlowPerDayBreakdown();
        string leftPercent = leftCap >= 0.001f ? (leftFull / leftCap).ToStringPercent() : "0%";
        string rightPercent = rightCap >= 0.001f ? (rightFull / rightCap).ToStringPercent() : "0%";
        float leftStretch = leftCap * PoolModelConstants.StretchCapFactor;
        float rightStretch = rightCap * PoolModelConstants.StretchCapFactor;
        string totalPercent = totalBaseCap >= 0.001f ? (totalMilk / totalBaseCap).ToStringPercent() : "0%";
        var factorLinesL = HediffComp_EqualMilkingLactating.BuildBreastEfficiencyFactorLinesForDevMode(b, multL, letdownL, pressureL, conditionsL);
        var factorLinesR = HediffComp_EqualMilkingLactating.BuildBreastEfficiencyFactorLinesForDevMode(b, multR, letdownR, pressureR, conditionsR);
        string kL = FluidSiteKind.BreastLeft.ToString();
        string kR = FluidSiteKind.BreastRight.ToString();
        bool singleCustomLeaf = entries.Count == 1 && hasRight && !hasLeft
            && !string.IsNullOrEmpty(right.key) && right.key != kL && right.key != kR;
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
        sb.Append(Tab).AppendLine("EM.PoolBreastTotalFlowLine".Translate(flowTotal.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
        sb.AppendLine();
        if (hasLeft)
        {
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
        }

        if (hasRight)
        {
            sb.AppendLine();
            sb.AppendLine(singleCustomLeaf
                ? "EM.PoolBreastPerLeafDetail".Translate(sectionLabel)
                : "EM.PoolRightBreast".Translate());
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
        }

        return sb.ToString();
    }
}

/// <summary>健康表部位悬停（HediffComp_SexPart.CompTipStringExtra）：人类乳房(RJW) 行追加与该乳房 Hediff 对应的乳池左/右（或每叶）块；阴茎/雄性产卵器行追加全身虚拟精液左/右槽。泌乳 Hediff 自身仅显示总体，不在此 Patch。各块用已翻译标题去重，避免同一次 tooltip 拼接重复。</summary>
[HarmonyPatch(typeof(HediffComp_SexPart), "get_CompTipStringExtra")]
public static class HediffComp_SexPart_CompTipStringExtra_Patch
{
    [HarmonyPostfix]
    public static void Postfix(HediffComp_SexPart __instance, ref string __result)
    {
        if (__instance?.parent == null) return;
        Hediff parent = __instance.parent;
        if (parent?.pawn == null) return;
        Pawn pawn = parent.pawn;
        try
        {
            GenitalFamily gf = __instance.Def.genitalFamily;
            if (gf == GenitalFamily.Breasts)
            {
                if (pawn.CompEquallyMilkable() == null || !pawn.IsLactating()) return;
                string sectionLabel = parent.LabelCap;
                string blockHeader = "EM.PoolBreastSectionHeader".Translate(sectionLabel);
                if (!string.IsNullOrEmpty(__result) && __result.Contains(blockHeader)) return;
                var entries = pawn.GetPoolEntriesForBreastHediff(parent);
                if (entries.Count == 0) return;
                string block = BreastPoolTooltipHelper.BuildBreastPoolBlock(sectionLabel, entries, pawn);
                if (!string.IsNullOrEmpty(block))
                    __result = (__result ?? "") + "\n" + block;
                return;
            }

            if (!MilkCumSettings.Cum_EnableVirtualSemenPool) return;
            if (gf != GenitalFamily.Penis && gf != GenitalFamily.MaleOvipositor) return;
            if (__instance.Def.genitalTags == null || !__instance.Def.genitalTags.Contains(GenitalTag.CanPenetrate)) return;
            if (parent is not ISexPartHediff sp || !RjwSemenPoolEconomy.IsPenisLikeSemenPart(sp)) return;

            string semenHeader = "EM.HealthTabVirtualSemenPoolHeader".Translate();
            if (!string.IsNullOrEmpty(__result) && __result.Contains(semenHeader)) return;

            var rows = pawn.CompVirtualSemenPool().GetSemenPoolDisplayRows(pawn);
            if (rows.Count == 0) return;

            var sb = new System.Text.StringBuilder();
            sb.Append("\n");
            sb.AppendLine(semenHeader);
            foreach ((FluidSiteKind site, float current, float capacity) in rows)
            {
                string side = site == FluidSiteKind.TesticleLeft
                    ? "EM.VirtualSemenPoolLeft".Translate()
                    : "EM.VirtualSemenPoolRight".Translate();
                sb.AppendLine($"{side}: {current.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)} / {capacity.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)}");
            }

            __result = (__result ?? "") + sb.ToString();
        }
        catch (Exception ex)
        {
            Log.Warning($"[MilkCum] HediffComp_SexPart CompTipStringExtra pool append: {ex.Message}");
        }
    }
}

/// <summary>兼容详细解剖等「非 HediffComp_SexPart 的乳房行」：在 HediffWithComps.TipStringExtra 后追加乳池块。匹配策略为“乳房族 SexPart”或“身体部位 defName 含 Breast”。</summary>
[HarmonyPatch(typeof(HediffWithComps), "get_TipStringExtra")]
public static class HediffWithComps_TipStringExtra_BreastPoolFallback_Patch
{
    [HarmonyPostfix]
    public static void Postfix(HediffWithComps __instance, ref string __result)
    {
        if (__instance == null || __instance is HediffWithComps_MilkCumLactating) return;
        Pawn pawn = __instance.pawn;
        if (pawn == null) return;
        try
        {
            if (pawn.CompEquallyMilkable() == null || !pawn.IsLactating()) return;
            bool isBreastLikeRow = false;
            if (__instance.def is HediffDef_SexPart sp && sp.genitalFamily == GenitalFamily.Breasts)
                isBreastLikeRow = true;
            else
                isBreastLikeRow = __instance.Part?.def?.defName?.IndexOf("Breast", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isBreastLikeRow) return;

            string sectionLabel = __instance.LabelCap;
            string blockHeader = "EM.PoolBreastSectionHeader".Translate(sectionLabel);
            if (!string.IsNullOrEmpty(__result) && __result.Contains(blockHeader)) return;

            var entries = pawn.GetPoolEntriesForBreastHediff(__instance);
            if (entries.Count == 0 && __instance.Part != null)
                entries = pawn.GetPoolEntriesForBreastPart(__instance.Part);
            if (entries.Count == 0) return;

            string block = BreastPoolTooltipHelper.BuildBreastPoolBlock(sectionLabel, entries, pawn);
            if (!string.IsNullOrEmpty(block))
                __result = (__result ?? "") + "\n" + block;
        }
        catch (Exception ex)
        {
            Log.Warning($"[MilkCum] HediffWithComps TipStringExtra breast pool fallback: {ex.Message}");
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
