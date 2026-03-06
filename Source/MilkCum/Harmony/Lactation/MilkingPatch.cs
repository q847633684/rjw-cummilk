using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MilkCum.Core;
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
            return;
        }
        if (mother.RaceProps?.Humanlike == true && mother.health?.hediffSet != null
            && !mother.health.hediffSet.HasHediff(HediffDefOf.Lactating))
        {
            mother.health.GetOrAddHediff(HediffDefOf.Lactating, mother.GetBreastOrChestPart());
        }
        // 人类分娩：若已有 Lactating（如药物）也叠加剩余天数+10、当前泌乳量+基础值
        if (mother.RaceProps?.Humanlike == true)
            PoolModelBirthHelper.ApplyBirthPoolValues(mother);
        MilkPermissionExtensions.TryGiveFirstLactationBirthMemory(mother);
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

/// <summary>泌乳灌满期间按 ExtraNutritionPerDay 施加额外饥饿；basis 控制倍率(150=1:1)。统一使用 comp.ExtraNutritionPerDay() 避免与 HediffComp 逻辑分叉。池满时 ExtraNutritionPerDay 已为 0，此处再按当前 Fullness 做一次满池判断，避免执行顺序导致多扣。回缩吸收：满池回缩时 GetReabsorbedNutritionPerDay &gt; 0，本补丁按同样 basis 将吸收量加回饱食度。设计原则 6：仅 Postfix 追加，不替换原版 NeedInterval 主逻辑。</summary>
[HarmonyPatch(typeof(Need_Food))]
[HarmonyPatch(nameof(Need_Food.NeedInterval))]
public static class Need_Food_LactatingNutrition_Patch
{
    /// <summary>Need_Food.NeedInterval 的调用间隔（tick），用于将每日流速折算为每间隔的饱食度变化。</summary>
    private const int NeedTicksInterval = 150;

    [HarmonyPostfix]
    public static void Postfix(Need_Food __instance)
    {
        Pawn pawn = __instance.pawn;
        if (pawn?.health?.hediffSet == null) return;
        if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating)?.TryGetComp<HediffComp_EqualMilkingLactating>() is not HediffComp_EqualMilkingLactating comp)
            return;
        var milkable = pawn.CompEquallyMilkable();
        if (milkable == null) return;
        int basis = Mathf.Clamp(EqualMilkingSettings.lactationExtraNutritionBasis, 0, 300);
        float factor = basis / 150f;
        // 产奶额外扣饱食度（池未满时）
        if (milkable.Fullness < milkable.maxFullness)
        {
            float flowPerDay = comp.ExtraNutritionPerDay();
            if (flowPerDay > 0f)
            {
                float extraFall = flowPerDay * factor * (NeedTicksInterval / 60000f);
                __instance.CurLevel = Mathf.Max(0f, __instance.CurLevel - extraFall);
            }
        }
        // 回缩吸收：满池回缩时未溢出部分折算为营养，加回饱食度
        float reabsorbedPerDay = milkable.GetReabsorbedNutritionPerDay();
        if (reabsorbedPerDay > 0f)
        {
            float reabsorbedPerInterval = reabsorbedPerDay * factor * (NeedTicksInterval / 60000f);
            __instance.CurLevel = Mathf.Min(__instance.MaxLevel, __instance.CurLevel + reabsorbedPerInterval);
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
        var comp = pawn.CompEquallyMilkable();
        if (comp != null)
            comp.ClearPools();
        PawnMilkStateExtensions.ClearLactatingStateCache(pawn);
    }
}

// AnimalProductionUtility_Patch 已移除：原用于隐藏原版产奶统计行（Transpiler 替换 GetCompProperties 为返回 null），
// 但该替换在信息卡构建 AnimalProductionStats 时易导致 NRE/越界，故改为不 patch。产奶类型/价值见主表格列。见 ADR-004。
// RaceProperties.SpecialDisplayStats 补丁已移除：与信息卡统计构建流程冲突，导致物品/小人信息卡均无法打开。产奶类型与价值改由主表格列等展示。
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
        float tBefore = Mathf.Max(0f, EqualMilkingSettings.GetProlactinTolerance(pawn) - EqualMilkingSettings.ProlactinToleranceGainPerDose);
        float rawSeverity = giveHediff.severity;

        // 已处于泌乳期时再次注射：直接生效，不走吸收延迟（不移除、不排队）。TryMergeWith 已对 L 加了 rawSeverity，此处只补耐受后的增量 deltaS，故传 deltaS - rawSeverity 避免 L 重复累加。
        if (hediff.Severity > rawSeverity)
        {
            float deltaS = rawSeverity * EqualMilkingSettings.GetProlactinToleranceFactor(tBefore) * EqualMilkingSettings.GetRaceDrugDeltaSMultiplier(pawn);
            float deltaLOnly = deltaS - rawSeverity;
            if (hediff.comps != null)
                foreach (var c in hediff.comps)
                    if (c is HediffComp_EqualMilkingLactating comp) { comp.AddFromDrug(deltaLOnly); break; }
            WorldComponent_EqualMilkingAbsorptionDelay.ApplyProlactinMoodEffects(pawn, rawSeverity);
            return;
        }

        // 水池模型吸收延迟：先移除刚加的 Lactating，到点时再挂并 AddFromDrug(Δs, t_before)
        pawn.health.RemoveHediff(hediff);
        var world = Find.World;
        var delayComp = world?.GetComponent<WorldComponent_EqualMilkingAbsorptionDelay>();
        if (delayComp != null)
        {
            int endTick = Find.TickManager.TicksGame + WorldComponent_EqualMilkingAbsorptionDelay.GetAbsorptionDelayTicks(pawn);
            delayComp.ScheduleLactating(pawn, rawSeverity, endTick, tBefore);
        }
        else
        {
            // 无 World 时（如测试）立即生效：Δs = raw × E_tol(t_before)，进水 ΔL = Δs × C_dose。3.3 动物差异化：乘种族药物倍率。
            float deltaS = rawSeverity * EqualMilkingSettings.GetProlactinToleranceFactor(tBefore) * EqualMilkingSettings.GetRaceDrugDeltaSMultiplier(pawn);
            var reapply = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart()) as HediffWithComps;
            if (reapply?.comps != null)
                foreach (var c in reapply.comps)
                    if (c is HediffComp_EqualMilkingLactating comp) { comp.AddFromDrug(deltaS); break; }
            WorldComponent_EqualMilkingAbsorptionDelay.ApplyProlactinMoodEffects(pawn, rawSeverity);
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

/// <summary>在 RJW 乳房行悬停（HediffComp_SexPart.CompTipStringExtra）后追加奶池块，保证「人类乳房」等 RJW 乳房 hediff 悬停时一定显示储量/流速。</summary>
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
            string poolKey = pawn.GetPoolKeyForBreastHediff(parent);
            if (string.IsNullOrEmpty(poolKey)) return;
            var entries = pawn.GetPoolEntriesForBreastHediff(parent);
            if (entries.Count == 0) return;
            string sectionLabel = parent.LabelCap;
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
