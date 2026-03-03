using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MilkCum.Core;
using MilkCum.Milk.Comps;
using MilkCum.Milk.Helpers;
using MilkCum.Milk.World;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MilkCum.Milk.HarmonyPatches;

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
            mother.health.AddHediff(HediffDefOf.Lactating, mother.GetBreastOrChestPart());
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
        ExtensionHelper.TryGiveFirstLactationBirthMemory(mother);
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
        if (__instance.pawn is not Pawn pawn || !pawn.IsMilkable()) return;
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
}

/// <summary>泌乳灌满期间按 ExtraNutritionPerDay 施加额外饥饿；basis 控制倍率(150=1:1)。统一使用 comp.ExtraNutritionPerDay() 避免与 HediffComp 逻辑分叉。池满时 ExtraNutritionPerDay 已为 0，此处再按当前 Fullness 做一次满池判断，避免执行顺序导致多扣。回缩吸收：满池回缩时 GetReabsorbedNutritionPerDay &gt; 0，本补丁按同样 basis 将吸收量加回饱食度。设计原则 6：仅 Postfix 追加，不替换原版 NeedInterval 主逻辑。</summary>
[HarmonyPatch(typeof(Need_Food))]
[HarmonyPatch(nameof(Need_Food.NeedInterval))]
public static class Need_NeedInterval_Patch
{
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
    }
}

/// <summary>
/// 绕过香草可挤奶比较统计条目：将 GetCompProperties&lt;CompProperties_Milkable&gt; 替换为返回 null，原版产奶行不显示；本 mod 用 RaceProperties.SpecialDisplayStats 显示产奶。
/// </summary>
[HarmonyPatch]
public static class AnimalProductionUtility_Patch
{
    private static MethodBase _cachedTargetMethod;

    public static MethodInfo getPropertiesMethod = AccessTools.Method(typeof(ThingDef), nameof(ThingDef.GetCompProperties)).MakeGenericMethod(typeof(CompProperties_Milkable));
    public static MethodInfo getMilkablePropertiesMethod = AccessTools.Method(typeof(AnimalProductionUtility_Patch), nameof(GetMilkableProperties));

    public static MethodBase TargetMethod()
    {
        if (_cachedTargetMethod != null) return _cachedTargetMethod;
        var parent = typeof(AnimalProductionUtility);
        // 迭代器可能是 <AnimalProductionStats>d__0 / d__1 等，按嵌套类型名查找
        foreach (var nested in parent.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (nested.Name.Contains("AnimalProductionStats") && nested.Name.Contains("d__"))
            {
                var moveNext = AccessTools.Method(nested, "MoveNext");
                if (moveNext != null)
                {
                    _cachedTargetMethod = moveNext;
                    return _cachedTargetMethod;
                }
            }
        }
        // 回退：固定名称 d__0（旧版）
        var fallbackType = AccessTools.TypeByName(parent.FullName + "+<AnimalProductionStats>d__0");
        _cachedTargetMethod = fallbackType != null ? AccessTools.Method(fallbackType, "MoveNext") : null;
        return _cachedTargetMethod;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        bool replacedAny = false;
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(getPropertiesMethod))
            {
                codes[i].opcode = OpCodes.Call;
                codes[i].operand = getMilkablePropertiesMethod;
                replacedAny = true;
            }
        }
        if (!replacedAny)
            Log.Warning("[Equal Milking] Could not find GetCompProperties<CompProperties_Milkable> in AnimalProductionUtility.AnimalProductionStats; vanilla milk stat display may apply. This is non-fatal.");
        return codes.AsEnumerable();
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
            yield return new StatDrawEntry(StatCategoryDefOf.AnimalProductivity, "Stat_Animal_MilkValue".Translate(), pawn.MilkMarketValue().ToStringMoney(), "Stat_Animal_MilkValueDesc".Translate(), 9840, null, null, false, false);
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

/// <summary>健康页：乳房 hediff 悬停时追加该乳/该对奶量与容量（左乳：x/x, 右乳：x/x）。见 Docs/泌乳系统逻辑图。目标方法 get_TipString 在部分 RimWorld 版本中可能不存在，故不参与 PatchAll，改为 Init 时按需手动 Patch；若为 GetTipString() 则一并 Patch 以兼容。健康页实际使用 HealthCardUtility.GetTooltip 与 TipStringExtra，故同时 Patch GetTooltip 确保悬停能显示。</summary>
public static class Hediff_TipString_BreastPool_Patch
{
    private static bool _applied;

    public static void ApplyIfPossible(HarmonyLib.Harmony harmony)
    {
        if (harmony == null || _applied) return;
        MethodBase tipStringGetter = HarmonyLib.AccessTools.PropertyGetter(typeof(Hediff), "TipString");
        if (tipStringGetter != null)
        {
            try
            {
                harmony.Patch(tipStringGetter, postfix: new HarmonyLib.HarmonyMethod(typeof(Hediff_TipString_BreastPool_Patch).GetMethod(nameof(TipString_Postfix), BindingFlags.Public | BindingFlags.Static)));
                _applied = true;
            }
            catch (System.Exception ex)
            {
                Verse.Log.Warning($"[MilkCum] Hediff TipString postfix not applied: {ex.Message}");
            }
        }
        if (!_applied)
        {
            MethodBase getTipString = HarmonyLib.AccessTools.Method(typeof(Hediff), "GetTipString");
            if (getTipString != null)
            {
                try
                {
                    harmony.Patch(getTipString, postfix: new HarmonyLib.HarmonyMethod(typeof(Hediff_TipString_BreastPool_Patch).GetMethod(nameof(GetTipString_Postfix), BindingFlags.Public | BindingFlags.Static)));
                    _applied = true;
                }
                catch (System.Exception ex)
                {
                    Verse.Log.Warning($"[MilkCum] Hediff GetTipString postfix not applied: {ex.Message}");
                }
            }
        }
    }

    public static void TipString_Postfix(Hediff __instance, ref string __result)
    {
        AppendPoolTooltip(__instance, ref __result);
    }

    public static void GetTipString_Postfix(Hediff __instance, ref string __result)
    {
        AppendPoolTooltip(__instance, ref __result);
    }

    private static void AppendPoolTooltip(Hediff __instance, ref string __result)
    {
        if (__instance?.pawn == null) return;
        Pawn pawn = __instance.pawn;
        if (pawn.CompEquallyMilkable() == null || !pawn.IsLactating()) return;
        try
        {
            var list = pawn.GetBreastList();
            if (list == null || !list.Contains(__instance)) return;
        }
        catch { return; }
        var entries = pawn.GetPoolEntriesForBreastHediff(__instance);
        if (entries == null || entries.Count == 0) return;
        var parts = new List<string>();
        foreach (var (_, fullness, capacity, isLeft) in entries)
        {
            string label = isLeft ? "EM.PoolLeftBreast".Translate() : "EM.PoolRightBreast".Translate();
            float baseCap = Mathf.Max(0.001f, capacity);
            float stretchCap = baseCap * PoolModelConstants.StretchCapFactor;
            string percentStr = baseCap >= 0.001f ? (fullness / baseCap).ToStringPercent() : "0%";
            string text = "EM.PoolBreastMilkCap".Translate(
                fullness.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                percentStr,
                baseCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                stretchCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute));
            parts.Add(label + ": " + text);
        }
        if (parts.Count > 0)
            __result = (__result ?? "") + "\n" + string.Join(", ", parts);
        string poolKey = pawn.GetPoolKeyForBreastHediff(__instance);
        if (!string.IsNullOrEmpty(poolKey))
        {
            var comp = pawn.LactatingHediffComp();
            if (comp != null)
            {
                var b = comp.GetFlowPerDayBreakdown();
                var (flowL, flowR, multL, multR) = pawn.GetFlowPerDayForBreastPair(poolKey);
                float flowPair = flowL + flowR;
                __result = (__result ?? "") + "\n" + "EM.PoolPairEfficiencyHeader".Translate(
                    flowPair.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    (flowPair * 100f).ToString("F0"));
                __result += "\n" + "EM.PoolLeftEfficiencyHeader".Translate(
                    flowL.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    (flowL * 100f).ToString("F0"));
                __result += "\n" + "EM.PoolEfficiencyFactorsBracket".Translate(HediffComp_EqualMilkingLactating.BuildBreastEfficiencyFactorLine(b, multL, true));
                __result += "\n" + "EM.PoolRightEfficiencyHeader".Translate(
                    flowR.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    (flowR * 100f).ToString("F0"));
                __result += "\n" + "EM.PoolEfficiencyFactorsBracket".Translate(HediffComp_EqualMilkingLactating.BuildBreastEfficiencyFactorLine(b, multR, false));
            }
        }
    }
}

/// <summary>健康页悬停由 HealthCardUtility.GetTooltip(Pawn, BodyPartRecord) 构建。本 Patch 在该方法返回的 tooltip 后追加一行「左乳：x/x，右乳：x/x」。目标方法用字符串 "GetTooltip" 避免编译时 CS0117。</summary>
[HarmonyPatch(typeof(HealthCardUtility), "GetTooltip", new[] { typeof(Pawn), typeof(BodyPartRecord) })]
public static class HealthCardUtility_GetTooltip_BreastPool_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref string __result, Pawn pawn, BodyPartRecord part)
    {
        if (pawn == null || part == null) return;
        if (pawn.CompEquallyMilkable() == null || !pawn.IsLactating()) return;
        var hediffSet = pawn.health?.hediffSet;
        if (hediffSet?.hediffs == null) return;
        var diffs = hediffSet.hediffs.Where(h => h != null && h.Part == part);
        var appendedKeys = new HashSet<string>();
        foreach (var h in diffs)
        {
            string key = pawn.GetPoolKeyForBreastHediff(h);
            if (string.IsNullOrEmpty(key) || !appendedKeys.Add(key)) continue;
            var entries = pawn.GetPoolEntriesForBreastHediff(h);
            if (entries == null || entries.Count == 0) continue;
            var parts = new List<string>();
            foreach (var (_, fullness, capacity, isLeft) in entries)
            {
                string label = isLeft ? "EM.PoolLeftBreast".Translate() : "EM.PoolRightBreast".Translate();
                float baseCap = Mathf.Max(0.001f, capacity);
                float stretchCap = baseCap * PoolModelConstants.StretchCapFactor;
                string percentStr = baseCap >= 0.001f ? (fullness / baseCap).ToStringPercent() : "0%";
                string text = "EM.PoolBreastMilkCap".Translate(
                    fullness.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    percentStr,
                    baseCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    stretchCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute));
                parts.Add(label + ": " + text);
            }
            if (parts.Count > 0)
            {
                __result = (__result ?? "") + "\n    " + string.Join(", ", parts);
                if (!string.IsNullOrEmpty(key))
                {
                    var comp = pawn.LactatingHediffComp();
                    if (comp != null)
                    {
                        var b = comp.GetFlowPerDayBreakdown();
                        var (flowL, flowR, multL, multR) = pawn.GetFlowPerDayForBreastPair(key);
                        float flowPair = flowL + flowR;
                        __result += "\n    " + "EM.PoolPairEfficiencyHeader".Translate(
                            flowPair.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                            (flowPair * 100f).ToString("F0"));
                        __result += "\n    " + "EM.PoolLeftEfficiencyHeader".Translate(
                            flowL.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                            (flowL * 100f).ToString("F0"));
                        __result += "\n    " + "EM.PoolEfficiencyFactorsBracket".Translate(HediffComp_EqualMilkingLactating.BuildBreastEfficiencyFactorLine(b, multL, true));
                        __result += "\n    " + "EM.PoolRightEfficiencyHeader".Translate(
                            flowR.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                            (flowR * 100f).ToString("F0"));
                        __result += "\n    " + "EM.PoolEfficiencyFactorsBracket".Translate(HediffComp_EqualMilkingLactating.BuildBreastEfficiencyFactorLine(b, multR, false));
                    }
                }
                break;
            }
        }
    }
}
