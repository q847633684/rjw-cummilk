using System.Collections.Generic;
using System.Text;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Core.Constants;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Comps;

/// <summary>池进水、溢出、回缩、满池信件。见 Docs/泌乳系统逻辑图。</summary>
public partial class CompEquallyMilkable
{
    private const float OverflowAccumulatorCap = 100f;

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

    /// <summary>水池模型：按对进水；每对仅当两侧都达基础容量后才允许撑大（见 Docs/泌乳系统逻辑图）。流速 = 基础日流速×条目倍率×状态×压力×喷乳反射；压力来自 Logistic 曲线或关压时的阶跃（顶满为 0），近满撑大时可再经 overflowResidualFlowFactor 抬升下限以模拟持续分泌/渗漏。TickGrowth 将超过撑大上限部分计为溢出污物；超出基础容量的乳量每 60 tick 按 ShrinkPerStep×健康度向基础收敛（与是否本步溢出无关），回缩量由 GetReabsorbedNutritionPerDay 折算营养。</summary>
    private void UpdateMilkPools()
    {
        var lactatingComp = Pawn?.LactatingHediffComp();
        if (lactatingComp == null || lactatingComp.RemainingDays <= 0f)
        {
            ResetMilkFlowDisplayCache();
            return;
        }
        float currentLactation = lactatingComp.CurrentLactationAmount;
        float effectiveLForFlow = lactatingComp.EffectiveLactationAmountForFlow;
        float hungerFactor = PawnUtility.BodyResourceGrowthSpeed(Pawn);
        if (currentLactation <= 0f || hungerFactor <= 0f)
        {
            ResetMilkFlowDisplayCache();
            return;
        }
        float drive = MilkCumSettings.GetEffectiveDrive(effectiveLForFlow);
        float condFactor = Pawn.GetMilkFlowMultiplierFromConditions();
        float raceFlow = MilkCumSettings.defaultFlowMultiplierForHumanlike;
        float basePerDay = drive * hungerFactor * condFactor * raceFlow;
        var entries = GetCachedEntries();
        breastFullness ??= new Dictionary<string, float>();
        float overflowTotal = 0f;
        float flowPerTickScale = Mathf.Max(0f, basePerDay / PoolModelConstants.TicksPerGameDay * PoolModelConstants.Interval60Ticks);
        if (MilkCumSettings.lactationPoolTickLog && Pawn != null)
        {
            var sb = new StringBuilder();
            sb.Append("[MilkCum][INFO][MilkFlow] 小人=").Append(Pawn.LabelShort).Append(" 泌乳量L=").Append(currentLactation.ToString("F3"))
                .Append(" 驱动力drive=").Append(drive.ToString("F3")).Append(" 饥饿系数hunger=").Append(hungerFactor.ToString("F3"))
                .Append(" 状态系数cond=").Append(condFactor.ToString("F3")).Append(" 种族流速倍率raceFlow=").Append(raceFlow.ToString("F3")).Append("; ");
            sb.Append("每日基础流速basePerDay=drive(").Append(drive.ToString("F3")).Append(")×hunger(").Append(hungerFactor.ToString("F3"))
                .Append(")×cond(").Append(condFactor.ToString("F3")).Append(")×raceFlow(").Append(raceFlow.ToString("F3")).Append(")=").Append(basePerDay.ToString("F3")).Append("; ");
            sb.Append("每60tick进池量flowPer60tick=basePerDay(").Append(basePerDay.ToString("F3")).Append(")/60000×60=").Append(flowPerTickScale.ToString("F5")).Append("；");
            sb.Append("单侧实际流速≈flowPer60tick×条目流速倍率×该侧状态修正×压力因子×喷乳反射");
            MilkCumSettings.PoolTickLog(sb.ToString());
        }
        if (MilkCumSettings.enableLetdownReflex)
            lactatingComp.DecayLetdownReflex(60f / 60f); // Δt = 60 tick = 1 分钟
        MilkRelatedHealthHelper.UpdateInflammationAndTryTriggerMastitis(lactatingComp, Fullness, maxFullness);
        float residualL = lactatingComp.CurrentLactationAmount;
        if (MilkCumSettings.enableToleranceDynamic)
            lactatingComp.UpdateToleranceDynamic(currentLactation, PoolModelConstants.Interval60PerDay);
        float extraFall60 = 0f;
        float baseMax = Mathf.Max(0.01f, maxFullness - capacityAdaptation);
        if (Fullness < baseMax && Pawn != null)
        {
            float factor = GetNutritionFactorForExtra();
            float interval60PerDay = PoolModelConstants.Interval60PerDay;
            extraFall60 = lactatingComp.ExtraNutritionPerDay() * factor * interval60PerDay;
            if (Pawn.needs?.food != null)
                Pawn.needs.food.CurLevel = Mathf.Clamp(Pawn.needs.food.CurLevel - extraFall60, 0f, Pawn.needs.food.MaxLevel);
            else if (Pawn.needs?.energy != null)
            {
                float extraFallEnergy60 = lactatingComp.ExtraEnergyPerDay() * factor * interval60PerDay;
                Pawn.needs.energy.CurLevel = Mathf.Clamp(Pawn.needs.energy.CurLevel - extraFallEnergy60, 0f, Pawn.needs.energy.MaxLevel);
            }
        }
        float fullnessBefore = Fullness;
        float stretchTotal = 0f;
        for (int i = 0; i < entries.Count; i++)
            stretchTotal += entries[i].Capacity * PoolModelConstants.StretchCapFactor;
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

        var pairGroups = GetCachedPairGroups();
        float totalFlowThisStep = 0f;
        float pressureWeightedSum = 0f, letdownWeightedSum = 0f, conditionsWeightedSum = 0f;
        bool anyFullThisStep = false;
        CachedFlowPerDayByKey ??= new Dictionary<string, float>();
        for (int g = 0; g < pairGroups.Count; g++)
        {
            var list = pairGroups[g];
            if (list.Count == 2)
            {
                FluidPoolEntry leftE = list[0].IsLeft ? list[0] : list[1];
                FluidPoolEntry rightE = list[0].IsLeft ? list[1] : list[0];
                if (string.IsNullOrEmpty(leftE.Key) || string.IsNullOrEmpty(rightE.Key)) continue;
                float leftCap = leftE.Capacity;
                float rightCap = rightE.Capacity;
                float stretchLeft = leftCap * PoolModelConstants.StretchCapFactor;
                float stretchRight = rightCap * PoolModelConstants.StretchCapFactor;
                float curLeft = GetFullnessForKey(leftE.Key);
                float curRight = GetFullnessForKey(rightE.Key);
                float pressureLeft = MilkCumSettings.enablePressureFactor
                    ? MilkCumSettings.GetPressureFactor(curLeft / Mathf.Max(0.001f, stretchLeft))
                    : (curLeft >= stretchLeft ? 0f : 1f);
                float pressureRight = MilkCumSettings.enablePressureFactor
                    ? MilkCumSettings.GetPressureFactor(curRight / Mathf.Max(0.001f, stretchRight))
                    : (curRight >= stretchRight ? 0f : 1f);
                MilkCumSettings.ApplyOverflowResidualFlow(ref pressureLeft, curLeft, stretchLeft, residualL, lactatingComp.GetInflammationForKey(leftE.Key));
                MilkCumSettings.ApplyOverflowResidualFlow(ref pressureRight, curRight, stretchRight, residualL, lactatingComp.GetInflammationForKey(rightE.Key));
                float conditionsLeft = Pawn.GetConditionsForSide(leftE.Key);
                float conditionsRight = Pawn.GetConditionsForSide(rightE.Key);
                float letdownLeft = MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(leftE.Key) : 1f;
                float letdownRight = MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(rightE.Key) : 1f;
                float flowLeft = Mathf.Max(0f, leftE.FlowMultiplier * flowPerTickScale * conditionsLeft * pressureLeft * letdownLeft * leftE.Density);
                float flowRight = Mathf.Max(0f, rightE.FlowMultiplier * flowPerTickScale * conditionsRight * pressureRight * letdownRight * rightE.Density);
                totalFlowThisStep += flowLeft + flowRight;
                pressureWeightedSum += flowLeft * pressureLeft + flowRight * pressureRight;
                letdownWeightedSum += flowLeft * letdownLeft + flowRight * letdownRight;
                conditionsWeightedSum += flowLeft * conditionsLeft + flowRight * conditionsRight;
                float toPerDay = PoolModelConstants.TicksPerGameDay / PoolModelConstants.Interval60Ticks;
                CachedFlowPerDayByKey[leftE.Key] = flowLeft * toPerDay;
                CachedFlowPerDayByKey[rightE.Key] = flowRight * toPerDay;
                var pairPool = new FluidPoolState();
                pairPool.SetFrom(curLeft, curRight, 0);
                var cap = new BreastPairCapacities(leftCap, rightCap, stretchLeft, stretchRight);
                var (leftNew, rightNew, overflow) = pairPool.TickGrowth(flowLeft, flowRight, cap);
                if (leftNew > leftCap)
                {
                    float excessL = leftNew - leftCap;
                    reabsorbedPoolThisStep += excessL * (1f - shrinkFactor);
                    reabsorbedPerKeyCache[leftE.Key] = excessL * (1f - shrinkFactor);
                    leftNew = leftCap + excessL * shrinkFactor;
                }
                if (rightNew > rightCap)
                {
                    float excessR = rightNew - rightCap;
                    reabsorbedPoolThisStep += excessR * (1f - shrinkFactor);
                    reabsorbedPerKeyCache[rightE.Key] = excessR * (1f - shrinkFactor);
                    rightNew = rightCap + excessR * shrinkFactor;
                }
                breastFullness[leftE.Key] = leftNew;
                breastFullness[rightE.Key] = rightNew;
                overflowTotal += overflow;
                if (leftNew >= leftCap * PoolModelConstants.FullnessThresholdFactor && rightNew >= rightCap * PoolModelConstants.FullnessThresholdFactor)
                    anyFullThisStep = true;
            }
            else
            {
                foreach (var e in list)
                {
                    float stretchCap = e.Capacity * PoolModelConstants.StretchCapFactor;
                    float current = GetFullnessForKey(e.Key);
                    float pressure = MilkCumSettings.enablePressureFactor
                        ? MilkCumSettings.GetPressureFactor(current / Mathf.Max(0.001f, stretchCap))
                        : (current >= stretchCap ? 0f : 1f);
                    MilkCumSettings.ApplyOverflowResidualFlow(ref pressure, current, stretchCap, residualL, lactatingComp.GetInflammationForKey(e.Key));
                    float conditionsE = Pawn.GetConditionsForSide(e.Key);
                    float letdownE = MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(e.Key) : 1f;
                    float flowPerTick = Mathf.Max(0f, e.FlowMultiplier * flowPerTickScale * conditionsE * pressure * letdownE * e.Density);
                    totalFlowThisStep += flowPerTick;
                    pressureWeightedSum += flowPerTick * pressure;
                    letdownWeightedSum += flowPerTick * letdownE;
                    conditionsWeightedSum += flowPerTick * conditionsE;
                    CachedFlowPerDayByKey[e.Key] = flowPerTick * (PoolModelConstants.TicksPerGameDay / PoolModelConstants.Interval60Ticks);
                    var (newFullness, overflow) = FluidPoolState.SingleBreastTickGrowth(current, flowPerTick, e.Capacity, stretchCap);
                    if (newFullness >= e.Capacity * PoolModelConstants.FullnessThresholdFactor)
                        anyFullThisStep = true;
                    if (newFullness > e.Capacity)
                    {
                        float excess = newFullness - e.Capacity;
                        reabsorbedPoolThisStep += excess * (1f - shrinkFactor);
                        reabsorbedPerKeyCache[e.Key] = excess * (1f - shrinkFactor);
                        newFullness = e.Capacity + excess * shrinkFactor;
                    }
                    breastFullness[e.Key] = newFullness;
                    overflowTotal += overflow;
                }
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
                Pawn.needs.food.CurLevel = Mathf.Clamp(Pawn.needs.food.CurLevel + addBack, 0f, Pawn.needs.food.MaxLevel);
            else if (Pawn.needs?.energy != null)
                Pawn.needs.energy.CurLevel = Mathf.Clamp(Pawn.needs.energy.CurLevel + addBack / MilkCumSettings.nutritionToEnergyFactor, 0f, Pawn.needs.energy.MaxLevel);
        }
        if (anyFullThisStep)
            ticksFullPool += 60;
        else
            ticksFullPool = Mathf.Max(0, ticksFullPool - 120);
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

    /// <summary>3.3 满池事件：满池超过约 1 天且开启设置时，每 Pawn 每日最多一封，且受 fullPoolLetterCooldownDays 冷却。见 Docs/泌乳系统逻辑图</summary>
    private void TrySendFullPoolLetter()
    {
        if (!MilkCumSettings.enableFullPoolLetter || Pawn == null || !Pawn.Spawned || !Pawn.IsColonyPawn()
            || ticksFullPool < (int)PoolModelConstants.TicksPerGameDay) return;
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
