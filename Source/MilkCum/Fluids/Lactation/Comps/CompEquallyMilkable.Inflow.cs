using System.Collections.Generic;
using System.Text;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Core.Constants;
using MilkCum.Fluids.Shared.Data;
using MilkCum.Integration.DubsBadHygiene;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Comps;

/// <summary>池进水、溢出、回缩、满池信件。见 记忆库/docs/泌乳系统逻辑图。</summary>
public partial class CompEquallyMilkable
{
    private const float OverflowAccumulatorCap = 100f;

    /// <summary>单侧撑大上限：基容×StretchCapFactor，再套 SYS-02a 形变缓冲。</summary>
    private float GetPerSideStretchCapForEntry(FluidPoolEntry e) =>
        MilkRealismHelper.GetPerSideStretchCap(e, PoolModelConstants.CapacityStretchCap(e.Capacity));

    /// <summary>单侧通道 + 顺应性；<paramref name="sharedChannelWeight"/> = max(0, core×顺应性)，供单一泌乳轴按权重分配。</summary>
    private void PrepareSideInflowRow(
        FluidPoolEntry e,
        float currentFullness,
        float stretchCap,
        HediffComp_EqualMilkingLactating lactatingComp,
        float residualL,
        FluidPoolNetwork network,
        out MilkPoolInflowSimulator.SideChannelFactors chan,
        out float sharedChannelWeight)
    {
        chan = MilkPoolInflowSimulator.ComputeSideChannelFactors(Pawn, e, currentFullness, stretchCap, lactatingComp, residualL, network);
        float compliance = MilkRealismHelper.GetComplianceMultiplier(currentFullness, stretchCap);
        sharedChannelWeight = Mathf.Max(0f, MilkPoolInflowSimulator.BreastSideCoreRate(e, chan) * compliance);
    }

    /// <summary>营养折算系数（泌乳消耗/回缩加回）。lactationExtraNutritionBasis 以 NutritionBasisDenominator 为 100% 基准。</summary>
    private float GetNutritionFactorForExtra()
    {
        int basis = Mathf.Clamp(MilkCumSettings.lactationExtraNutritionBasis, 0, 300);
        return basis / PoolModelConstants.NutritionBasisDenominator;
    }

    /// <summary>回缩吸收：仅当本步有回缩时返回 UpdateMilkPools 中缓存的「每日补充营养」；无回缩或未开启时返回 0。避免 UI/Needs 每次读取时遍历 entries。</summary>
    public float GetReabsorbedNutritionPerDay()
    {
        if (!MilkCumSettings.reabsorbNutritionEnabled || Pawn == null) return 0f;
        if (!hadShrinkLastStep) return 0f;
        return cachedReabsorbedNutritionPerDay;
    }

    /// <summary>泌乳未进水时重置流速/UI 缓存，避免重复分支。</summary>
    private void ResetMilkFlowDisplayCache()
    {
        CachedFlowPerDayForDisplay = 0f;
        CachedFlowPerDayByKey = null;
        CachedPressureForDisplay = CachedLetdownForDisplay = CachedConditionsForDisplay = 1f;
        CachedFlowTick = Find.TickManager.TicksGame;
    }

    /// <summary>水池模型：按池 key 进水。「并行独立侧」每侧各自 flowPerTickScale×通道因子；「单一泌乳轴」(<c>inflowSharedMammaryBudget</c>) 将同一份 flowPerTickScale 按通道权重比例拆分。压力来自 Logistic 或关压阶跃，近满可经 overflowResidualFlowFactor 抬下限；涨缩、漏奶、营养回灌等同 记忆库/docs/泌乳系统逻辑图。</summary>
    private void UpdateMilkPools()
    {
        var lactatingComp = Pawn?.LactatingHediffComp();
        if (!MilkPoolInflowSimulator.TryComputeInflowScale(
                Pawn,
                lactatingComp,
                MilkRealismHelper.GetDrainReliefInflowBoost(this),
                out float basePerDay,
                out float flowPerTickScale))
        {
            ResetMilkFlowDisplayCache();
            return;
        }

        float currentLactation = lactatingComp.CurrentLactationAmount;
        var entries = GetCachedEntries();
        breastFullness ??= new Dictionary<string, float>();
        float overflowTotal = 0f;
        TryApplyPoolFedSelf();
        if (MilkCumSettings.lactationPoolTickLog && Pawn != null)
        {
            float hungerFactor = Mathf.Max(0.05f, PawnUtility.BodyResourceGrowthSpeed(Pawn));
            float drive = MilkCumSettings.GetEffectiveDrive(lactatingComp.EffectiveLactationAmountForFlow);
            float raceFlow = MilkCumSettings.defaultFlowMultiplierForHumanlike;
            float baseFromFactors = drive * hungerFactor * raceFlow;
            var sb = new StringBuilder();
            sb.Append("[MilkCum][INFO][MilkFlow] 小人=").Append(Pawn.LabelShort).Append(" 泌乳量L=").Append(currentLactation.ToString("F3"))
                .Append(" 驱动力drive=").Append(drive.ToString("F3")).Append(" 饥饿系数hunger=").Append(hungerFactor.ToString("F3"))
                .Append(" 种族流速倍率raceFlow=").Append(raceFlow.ToString("F3")).Append("; ");
            sb.Append("每日基础流速basePerDay=drive×hunger×race=").Append(baseFromFactors.ToString("F3"))
                .Append("（应与 TryComputeInflowScale 的 basePerDay ").Append(basePerDay.ToString("F3")).Append(" 一致）; ");
            sb.Append("每60tick标度flowPer60tick=").Append(flowPerTickScale.ToString("F5"))
                .Append("（=basePerDay/日tick×60×昼夜×代谢×排空 relief）; ");
            sb.Append("单侧实际流速≈flowPer60tick×core(流速倍率×状态×压力×喷乳×导管)×顺应性；单一轴再按 core×顺应性 比例分总预算；");
            sb.Append("substeps=").Append(IsInflowEventBurstActive() ? Mathf.Max(1, MilkCumSettings.inflowEventSubsteps) : 1);
            MilkCumSettings.PoolTickLog(sb.ToString());
        }
        if (MilkCumSettings.enableLetdownReflex)
            lactatingComp.DecayLetdownReflex(60f / 60f); // Δt = 60 tick = 1 分钟
        MilkRelatedHealthHelper.UpdateInflammationAndTryTriggerMastitis(lactatingComp);
        float residualL = lactatingComp.CurrentLactationAmount;
        float extraFall60 = 0f;
        float pendingNeedDelta = 0f;
        float baseMax = Mathf.Max(0.01f, maxFullness - CapacityAdaptation);
        if (Fullness < baseMax && Pawn != null)
        {
            float factor = GetNutritionFactorForExtra();
            float interval60PerDay = PoolModelConstants.Interval60PerDay;
            extraFall60 = lactatingComp.ExtraNutritionPerDay() * factor * interval60PerDay;
            if (Pawn.needs?.food != null)
                pendingNeedDelta -= extraFall60;
            else if (Pawn.needs?.energy != null)
            {
                float extraFallEnergy60 = lactatingComp.ExtraEnergyPerDay() * factor * interval60PerDay;
                pendingNeedDelta -= extraFallEnergy60;
            }
        }
        float fullnessBefore = Fullness;
        float stretchTotal = SumStretchCapsForEntries(entries);
        var network = FluidPoolNetwork.Build(entries, breastFullness);
        fullnessBeforePerKeyCache.Clear();
        if (MilkCumSettings.lactationPoolTickLog && Pawn != null)
        {
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.Key))
                    fullnessBeforePerKeyCache[e.Key] = breastFullness.TryGetValue(e.Key, out float v) ? v : 0f;
        }
        float healthPercent = 1f;
        if (Pawn?.health?.summaryHealth != null)
            healthPercent = Mathf.Clamp(Pawn.health.summaryHealth.SummaryHealthPercent, 0.2f, 1f);
        float shrinkFactor = Mathf.Clamp01((1f - PoolModelConstants.ShrinkPerStep) * healthPercent);
        float reabsorbedPoolThisStep = 0f;
        reabsorbedPerKeyCache.Clear();

        float totalFlowThisStep = 0f;
        float pressureWeightedSum = 0f, letdownWeightedSum = 0f, conditionsWeightedSum = 0f;
        CachedFlowPerDayByKey ??= new Dictionary<string, float>();
        int substeps = IsInflowEventBurstActive()
            ? Mathf.Max(1, MilkCumSettings.inflowEventSubsteps)
            : 1;
        if (MilkCumSettings.inflowSharedMammaryBudget)
        {
            PrepareInflowSharedAccDicts(entries);
            var flowAccByKey = inflowSharedFlowAccByKey;
            var pAccByKey = inflowSharedPAccByKey;
            var lAccByKey = inflowSharedLAccByKey;
            var cAccByKey = inflowSharedCAccByKey;
            for (int s = 0; s < substeps; s++)
            {
                float budgetPiece = flowPerTickScale / substeps;
                int count = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (!string.IsNullOrEmpty(entries[i].Key)) count++;
                }
                if (count == 0) continue;
                EnsureInflowSharedRowScratch(count);
                var eArr = inflowSharedRowEArr;
                var curArr = inflowSharedRowCurArr;
                var scArr = inflowSharedRowScArr;
                var keyArr = inflowSharedRowKeyArr;
                var chanArr = inflowSharedRowChanArr;
                var wArr = inflowSharedRowWArr;
                int ix = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    if (string.IsNullOrEmpty(e.Key)) continue;
                    float stretchCap = GetPerSideStretchCapForEntry(e);
                    float cur = GetFullnessForKey(e.Key);
                    PrepareSideInflowRow(e, cur, stretchCap, lactatingComp, residualL, network, out var chan, out float w);
                    eArr[ix] = e;
                    curArr[ix] = cur;
                    scArr[ix] = stretchCap;
                    keyArr[ix] = e.Key;
                    chanArr[ix] = chan;
                    wArr[ix] = w;
                    ix++;
                }
                float sumW = 0f;
                for (int k = 0; k < count; k++) sumW += wArr[k];
                for (int k = 0; k < count; k++)
                {
                    var e = eArr[k];
                    string key = keyArr[k];
                    float current = curArr[k];
                    float stretchCap = scArr[k];
                    var chan = chanArr[k];
                    float flowPerTick = sumW > PoolModelConstants.Epsilon ? budgetPiece * wArr[k] / sumW : 0f;
                    float sideOv = MilkPoolInflowSimulator.ApplyGrowthLeakShrinkSubstep(e, key, ref current, flowPerTick, stretchCap, chan.Pressure, lactatingComp, shrinkFactor, ref reabsorbedPoolThisStep, reabsorbedPerKeyCache);
                    overflowTotal += sideOv;
                    breastFullness[key] = current;
                    flowAccByKey[key] += flowPerTick;
                    pAccByKey[key] += flowPerTick * chan.Pressure;
                    lAccByKey[key] += flowPerTick * chan.Letdown;
                    cAccByKey[key] += flowPerTick * chan.Conditions;
                }
            }
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (string.IsNullOrEmpty(e.Key)) continue;
                string key = e.Key;
                float pf = flowAccByKey[key];
                totalFlowThisStep += pf;
                pressureWeightedSum += pAccByKey[key];
                letdownWeightedSum += lAccByKey[key];
                conditionsWeightedSum += cAccByKey[key];
                CachedFlowPerDayByKey[key] = pf * (PoolModelConstants.TicksPerGameDay / PoolModelConstants.Interval60Ticks);
            }
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (string.IsNullOrEmpty(e.Key)) continue;
                float stretchCap = GetPerSideStretchCapForEntry(e);
                float current = GetFullnessForKey(e.Key);
                float perStepFlowAcc = 0f;
                float sideOverflowAcc = 0f;
                float pressureAcc = 0f, letdownAcc = 0f, conditionsAcc = 0f;
                for (int s = 0; s < substeps; s++)
                {
                    var sideFlow = MilkPoolInflowSimulator.ComputeSideFlow(Pawn, e, current, stretchCap, flowPerTickScale / substeps, lactatingComp, residualL, network);
                    float flowPerTick = sideFlow.flowPerTick;
                    perStepFlowAcc += flowPerTick;
                    pressureAcc += flowPerTick * sideFlow.pressure;
                    letdownAcc += flowPerTick * sideFlow.letdown;
                    conditionsAcc += flowPerTick * sideFlow.conditions;
                    sideOverflowAcc += MilkPoolInflowSimulator.ApplyGrowthLeakShrinkSubstep(e, e.Key, ref current, flowPerTick, stretchCap, sideFlow.pressure, lactatingComp, shrinkFactor, ref reabsorbedPoolThisStep, reabsorbedPerKeyCache);
                }
                totalFlowThisStep += perStepFlowAcc;
                pressureWeightedSum += pressureAcc;
                letdownWeightedSum += letdownAcc;
                conditionsWeightedSum += conditionsAcc;
                CachedFlowPerDayByKey[e.Key] = perStepFlowAcc * (PoolModelConstants.TicksPerGameDay / PoolModelConstants.Interval60Ticks);
                breastFullness[e.Key] = current;
                overflowTotal += sideOverflowAcc;
            }
        }
        // 供 UI 读取：池逻辑实际进水流速（池单位/天）与流速加权因子，与回缩/溢出等完全一致
        CachedFlowPerDayForDisplay = totalFlowThisStep * (PoolModelConstants.TicksPerGameDay / PoolModelConstants.Interval60Ticks);
        CachedFlowTick = Find.TickManager.TicksGame;
        float denom = totalFlowThisStep >= PoolModelConstants.DisplayEpsilon ? totalFlowThisStep : 1f;
        CachedPressureForDisplay = totalFlowThisStep >= PoolModelConstants.DisplayEpsilon ? pressureWeightedSum / denom : 1f;
        CachedLetdownForDisplay = totalFlowThisStep >= PoolModelConstants.DisplayEpsilon ? letdownWeightedSum / denom : 1f;
        CachedConditionsForDisplay = totalFlowThisStep >= PoolModelConstants.DisplayEpsilon ? conditionsWeightedSum / denom : (Pawn != null ? Pawn.GetMilkFlowMultiplierFromConditions() : 1f);

        hadShrinkLastStep = reabsorbedPoolThisStep > 0f;
        cachedReabsorbedNutritionPerDay = hadShrinkLastStep
            ? reabsorbedPoolThisStep / PoolModelConstants.Interval60PerDay * PoolModelConstants.NutritionPerPoolUnit
                * Mathf.Clamp01(MilkCumSettings.reabsorbNutritionEfficiency)
            : 0f;
        float addBack = 0f;
        if (reabsorbedPoolThisStep > 0f && MilkCumSettings.reabsorbNutritionEnabled && Pawn != null)
        {
            float factor = GetNutritionFactorForExtra();
            addBack = reabsorbedPoolThisStep * PoolModelConstants.NutritionPerPoolUnit
                * Mathf.Clamp01(MilkCumSettings.reabsorbNutritionEfficiency) * factor;
            if (Pawn.needs?.food != null)
                pendingNeedDelta += addBack;
            else if (Pawn.needs?.energy != null)
                pendingNeedDelta += addBack / MilkCumSettings.nutritionToEnergyFactor;
        }
        // 统一净额结算，避免同一步“先扣后补”导致短周期抖动。
        if (Pawn?.needs?.food != null && Mathf.Abs(pendingNeedDelta) > PoolModelConstants.Epsilon)
        {
            Pawn.needs.food.CurLevel = Mathf.Clamp(Pawn.needs.food.CurLevel + pendingNeedDelta, 0f, Pawn.needs.food.MaxLevel);
        }
        else if (Pawn?.needs?.energy != null && Mathf.Abs(pendingNeedDelta) > PoolModelConstants.Epsilon)
        {
            Pawn.needs.energy.CurLevel = Mathf.Clamp(Pawn.needs.energy.CurLevel + pendingNeedDelta, 0f, Pawn.needs.energy.MaxLevel);
        }
        UpdateTicksFullPoolForEntries(entries);
        SyncBaseFullness();
        HandleOverflow(overflowTotal);
        TrySendFullPoolLetter();
        if (MilkCumSettings.lactationPoolTickLog && Pawn != null)
        {
            float flowAdded = Fullness - fullnessBefore + reabsorbedPoolThisStep;
            float curLevel = Pawn.needs?.food != null ? Pawn.needs.food.CurLevel : (Pawn.needs?.energy != null ? Pawn.needs.energy.CurLevel : -1f);
            float totalInflow = 0f;
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                float before = fullnessBeforePerKeyCache.TryGetValue(e.Key, out float b) ? b : 0f;
                float after = breastFullness.TryGetValue(e.Key, out float a) ? a : 0f;
                float reabsorb = reabsorbedPerKeyCache.TryGetValue(e.Key, out float r) ? r : 0f;
                float inflow = after - before + reabsorb;
                totalInflow += inflow;
            }
            var sb = new StringBuilder();
            sb.Append(Pawn.Name).Append(" 饱食=").Append(curLevel.ToString("F3")).Append(" 营养-").Append(extraFall60.ToString("F4"))
                .Append(" 乳池+").Append(flowAdded.ToString("F4")).Append(" 回缩-").Append(reabsorbedPoolThisStep.ToString("F4"))
                .Append(" 营养+").Append(addBack.ToString("F4")).Append(" 池满=").Append(Fullness.ToString("F3"));
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                float before = fullnessBeforePerKeyCache.TryGetValue(e.Key, out float b) ? b : 0f;
                float after = breastFullness.TryGetValue(e.Key, out float a) ? a : 0f;
                float reabsorb = reabsorbedPerKeyCache.TryGetValue(e.Key, out float r) ? r : 0f;
                float inflow = after - before + reabsorb;
                float nutritionShare = totalInflow >= PoolModelConstants.Epsilon ? extraFall60 * (inflow / totalInflow) : 0f;
                sb.AppendLine();
                sb.Append("  池 ").Append(e.Key).Append(": 营养-").Append(nutritionShare.ToString("F4")).Append(" 进+").Append(inflow.ToString("F4"))
                    .Append(" 回缩-").Append(reabsorb.ToString("F4")).Append(" 满=").Append(after.ToString("F3"));
            }
            MilkCumSettings.PoolTickLog(sb.ToString());
        }
    }

    /// <summary>SYS-03：极低饱食时从乳池自给营养与（DBH）口渴。</summary>
    private void TryApplyPoolFedSelf()
    {
        if (!MilkCumSettings.realismPoolFedSelf || Pawn == null) return;
        float maxFrac = Mathf.Clamp01(MilkCumSettings.realismPoolFedSelfMaxPoolFractionPer60);
        float capTake = Fullness * maxFrac;
        if (capTake <= PoolModelConstants.Epsilon) return;
        if (Pawn.needs?.food == null) return;
        float deficit = Mathf.Max(0f, MilkCumSettings.realismPoolFedSelfFoodTarget - Pawn.needs.food.CurLevel);
        float take = Mathf.Min(deficit, capTake);
        if (take <= PoolModelConstants.Epsilon) return;
        float drained = DrainForConsume(take, null);
        if (drained <= PoolModelConstants.Epsilon) return;
        Pawn.needs.food.CurLevel = Mathf.Clamp(
            Pawn.needs.food.CurLevel + drained * PoolModelConstants.NutritionPerPoolUnit,
            0f, Pawn.needs.food.MaxLevel);
        DubsBadHygieneIntegration.SatisfyThirst(Pawn, drained);
    }

    /// <summary>3.3 满池事件：满池超过约 1 天且开启设置时，每 Pawn 每日最多一封，且受 fullPoolLetterCooldownDays 冷却。见 记忆库/docs/泌乳系统逻辑图</summary>
    private void TrySendFullPoolLetter()
    {
        if (!MilkCumSettings.enableFullPoolLetter || Pawn == null || !Pawn.Spawned || !Pawn.IsColonyPawn()
            || GetMaxTicksFullPoolAcrossSides() < (int)PoolModelConstants.TicksPerGameDay) return;
        int currentDay = GenDate.DaysPassed;
        if (lastFullPoolLetterDay >= 0 && lastFullPoolLetterDay == currentDay) return;
        int now = Find.TickManager.TicksGame;
        int cooldownTicks = (int)(Mathf.Max(0.5f, MilkCumSettings.fullPoolLetterCooldownDays) * GenDate.TicksPerDay);
        if (lastFullPoolLetterTick >= 0 && now - lastFullPoolLetterTick < cooldownTicks) return;
        lastFullPoolLetterTick = now;
        lastFullPoolLetterDay = currentDay;
        string title = "EM.FullPoolLetterTitle".Translate();
        if (string.IsNullOrEmpty(title) || title == "EM.FullPoolLetterTitle") title = "EM.MilkPoolFull".Translate();
        string text = "EM.FullPoolLetterText".Translate(Pawn.LabelShort);
        if (string.IsNullOrEmpty(text)) text = Pawn.LabelShort + " " + "EM.MilkPoolFull".Translate();
        Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent, new TargetInfo(Pawn));
    }

    private void PrepareInflowSharedAccDicts(List<FluidPoolEntry> entries)
    {
        inflowSharedFlowAccByKey ??= new Dictionary<string, float>();
        inflowSharedPAccByKey ??= new Dictionary<string, float>();
        inflowSharedLAccByKey ??= new Dictionary<string, float>();
        inflowSharedCAccByKey ??= new Dictionary<string, float>();
        ClearAndPrimeInflowAccDict(inflowSharedFlowAccByKey, entries);
        ClearAndPrimeInflowAccDict(inflowSharedPAccByKey, entries);
        ClearAndPrimeInflowAccDict(inflowSharedLAccByKey, entries);
        ClearAndPrimeInflowAccDict(inflowSharedCAccByKey, entries);
    }

    private static void ClearAndPrimeInflowAccDict(Dictionary<string, float> d, List<FluidPoolEntry> entries)
    {
        d.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            string ek = entries[i].Key;
            if (string.IsNullOrEmpty(ek)) continue;
            d[ek] = 0f;
        }
    }

    private void EnsureInflowSharedRowScratch(int count)
    {
        if (count <= 0) return;
        if (inflowSharedRowEArr == null || inflowSharedRowEArr.Length < count)
        {
            inflowSharedRowEArr = new FluidPoolEntry[count];
            inflowSharedRowCurArr = new float[count];
            inflowSharedRowScArr = new float[count];
            inflowSharedRowKeyArr = new string[count];
            inflowSharedRowChanArr = new MilkPoolInflowSimulator.SideChannelFactors[count];
            inflowSharedRowWArr = new float[count];
        }
    }

    /// <summary>满池溢出：累计溢出量达到阈值时生成地面污物（不扣水位）；污物 Def 由设置指定</summary>
    private void HandleOverflow(float overflowThisTick)
    {
        if (overflowThisTick <= 0f || Pawn == null) return;
        overflowAccumulator += overflowThisTick;
        overflowAccumulator = Mathf.Min(overflowAccumulator, OverflowAccumulatorCap);
        if (!Pawn.Spawned || Pawn.Map == null) return;
        var filthDef = !string.IsNullOrEmpty(MilkCumSettings.overflowFilthDefName)
            ? DefDatabase<ThingDef>.GetNamedSilentFail(MilkCumSettings.overflowFilthDefName)
            : null;
        filthDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("Filth_Vomit");
        if (filthDef != null)
        {
            int filthSpawned = 0;
            while (overflowAccumulator >= PoolModelConstants.OverflowFilthThreshold)
            {
                FilthMaker.TryMakeFilth(Pawn.Position, Pawn.Map, filthDef, 1);
                overflowAccumulator -= PoolModelConstants.OverflowFilthThreshold;
                filthSpawned++;
            }
            if (filthSpawned > 0)
                overflowEventCount += filthSpawned;
            if (filthSpawned > 0 && Pawn.RaceProps.Humanlike && Pawn.needs?.mood?.thoughts?.memories != null
                && MilkCumDefOf.EM_MilkOverflow != null)
                Pawn.needs.mood.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_MilkOverflow);
            if (filthSpawned > 0)
            {
                string moteText = "EM.MilkOverflowMote".Translate();
                if (!string.IsNullOrEmpty(moteText) && moteText != "EM.MilkOverflowMote")
                    MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, moteText, 2.5f);
            }
        }
    }
}
