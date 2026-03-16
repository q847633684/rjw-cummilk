using System.Collections.Generic;
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
    /// <summary>回缩吸收：仅当本步有回缩时返回 UpdateMilkPools 中缓存的「每日补充营养」；无回缩或未开启时返回 0。避免 UI/Needs 每次读取时遍历 entries。</summary>
    public float GetReabsorbedNutritionPerDay()
    {
        if (!MilkCumSettings.reabsorbNutritionEnabled || Pawn == null) return 0f;
        if (!hadShrinkLastStep) return 0f;
        return cachedReabsorbedNutritionPerDay;
    }

    /// <summary>水池模型：按对进水；每对仅当两侧都达基础容量后才允许撑大（见 Docs/泌乳系统逻辑图）。撑大后仍按压力曲线进水，超出撑大部分算溢出（持续微量溢出）。仅当本步发生溢出时才执行池水位回缩与回缩吸收；回缩后满度降低、下一轮压力减小流速恢复，效果上从满池微量进水转为明显泌乳，形成逻辑闭环。回缩时超出基础容量部分每 60 tick 按 ShrinkPerStep 向基础容量收敛，回缩吸收由 GetReabsorbedNutritionPerDay 折算饱食度。</summary>
    private void UpdateMilkPools()
    {
        var lactatingComp = Pawn?.LactatingHediffComp();
        if (lactatingComp == null || lactatingComp.RemainingDays <= 0f)
        {
            CachedFlowPerDayForDisplay = 0f;
            CachedFlowPerDayByKey = null;
            CachedPressureForDisplay = CachedLetdownForDisplay = CachedConditionsForDisplay = 1f;
            CachedFlowTick = Find.TickManager.TicksGame;
            return;
        }
        float currentLactation = lactatingComp.CurrentLactationAmount;
        float effectiveLForFlow = lactatingComp.EffectiveLactationAmountForFlow;
        float hungerFactor = PawnUtility.BodyResourceGrowthSpeed(Pawn);
        if (currentLactation <= 0f || hungerFactor <= 0f)
        {
            CachedFlowPerDayForDisplay = 0f;
            CachedFlowPerDayByKey = null;
            CachedPressureForDisplay = CachedLetdownForDisplay = CachedConditionsForDisplay = 1f;
            CachedFlowTick = Find.TickManager.TicksGame;
            return;
        }
        float drive = MilkCumSettings.GetEffectiveDrive(effectiveLForFlow);
        float condFactor = Pawn.GetMilkFlowMultiplierFromConditions();
        float geneFactor = Pawn.GetMilkFlowMultiplierFromGenes();
        float raceFlow = MilkCumSettings.defaultFlowMultiplierForHumanlike;
        float basePerDay = drive * hungerFactor * condFactor * geneFactor * raceFlow;
        var entries = GetCachedEntries();
        breastFullness ??= new Dictionary<string, float>();
        float overflowTotal = 0f;
        float flowPerTickScale = basePerDay / 60000f * 60f;
        if (MilkCumSettings.lactationPoolTickLog && Pawn != null)
        {
            MilkCumSettings.PoolTickLog(
                $"[MilkCum][INFO][MilkFlow] 小人={Pawn.LabelShort} 泌乳量L={currentLactation:F3} 驱动力drive={drive:F3} 饥饿系数hunger={hungerFactor:F3} 状态系数cond={condFactor:F3} 基因系数genes={geneFactor:F3} 种族流速倍率raceFlow={raceFlow:F3}; " +
                $"每日基础流速basePerDay=drive({drive:F3})×hunger({hungerFactor:F3})×cond({condFactor:F3})×genes({geneFactor:F3})×raceFlow({raceFlow:F3})={basePerDay:F3}; " +
                $"每60tick进池量flowPer60tick=basePerDay({basePerDay:F3})/60000×60={flowPerTickScale:F5}；" +
                $"单侧实际流速≈flowPer60tick×条目流速倍率×该侧状态修正×压力因子×喷乳反射");
        }
        SyncLeftRightFromBreastFullness();
        if (MilkCumSettings.enableLetdownReflex)
            lactatingComp.DecayLetdownReflex(60f / 60f); // Δt = 60 tick = 1 分钟
        MilkRelatedHealthHelper.UpdateInflammationAndTryTriggerMastitis(lactatingComp, Fullness, maxFullness);
        if (MilkCumSettings.enableToleranceDynamic)
            lactatingComp.UpdateToleranceDynamic(currentLactation, PoolModelConstants.Interval60PerDay);
        float extraFall60 = 0f;
        float baseMax = Mathf.Max(0.01f, maxFullness - capacityAdaptation);
        if (Fullness < baseMax && Pawn != null)
        {
            int basis = Mathf.Clamp(MilkCumSettings.lactationExtraNutritionBasis, 0, 300);
            float factor = basis / 150f;
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
        var fullnessBeforePerKey = new Dictionary<string, float>();
        if (MilkCumSettings.lactationPoolTickLog && Pawn != null)
        {
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.Key))
                    fullnessBeforePerKey[e.Key] = breastFullness.TryGetValue(e.Key, out float v) ? v : 0f;
        }
        var pairGroups = GetCachedPairGroups();
        float totalFlowThisStep = 0f;
        float pressureWeightedSum = 0f, letdownWeightedSum = 0f, conditionsWeightedSum = 0f;
        CachedFlowPerDayByKey = new Dictionary<string, float>();
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
                float curLeft = breastFullness.TryGetValue(leftE.Key, out float vl) ? vl : 0f;
                float curRight = breastFullness.TryGetValue(rightE.Key, out float vr) ? vr : 0f;
                float pressureLeft = MilkCumSettings.enablePressureFactor
                    ? MilkCumSettings.GetPressureFactor(curLeft / Mathf.Max(0.001f, stretchLeft))
                    : (curLeft >= stretchLeft ? 0f : 1f);
                float pressureRight = MilkCumSettings.enablePressureFactor
                    ? MilkCumSettings.GetPressureFactor(curRight / Mathf.Max(0.001f, stretchRight))
                    : (curRight >= stretchRight ? 0f : 1f);
                float conditionsLeft = Pawn.GetConditionsForSide(leftE.Key);
                float conditionsRight = Pawn.GetConditionsForSide(rightE.Key);
                float letdownLeft = MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(leftE.Key) : 1f;
                float letdownRight = MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(rightE.Key) : 1f;
                float flowLeft = leftE.FlowMultiplier * flowPerTickScale * conditionsLeft * pressureLeft * letdownLeft;
                float flowRight = rightE.FlowMultiplier * flowPerTickScale * conditionsRight * pressureRight * letdownRight;
                if (IsOverflowState(leftE.Key, curLeft, leftCap)) flowLeft = 0f;
                if (IsOverflowState(rightE.Key, curRight, rightCap)) flowRight = 0f;
                totalFlowThisStep += flowLeft + flowRight;
                pressureWeightedSum += flowLeft * pressureLeft + flowRight * pressureRight;
                letdownWeightedSum += flowLeft * letdownLeft + flowRight * letdownRight;
                conditionsWeightedSum += flowLeft * conditionsLeft + flowRight * conditionsRight;
                float toPerDay = 60000f / 60f;
                CachedFlowPerDayByKey[leftE.Key] = flowLeft * toPerDay;
                CachedFlowPerDayByKey[rightE.Key] = flowRight * toPerDay;
                var pairPool = new FluidPoolState();
                pairPool.SetFrom(curLeft, curRight, 0);
                float overflow = pairPool.TickGrowth(flowLeft, flowRight, leftCap, rightCap, stretchLeft, stretchRight);
                breastFullness[leftE.Key] = pairPool.LeftFullness;
                breastFullness[rightE.Key] = pairPool.RightFullness;
                if (overflow > 0f)
                {
                    if (pairPool.LeftFullness > leftCap) overflowTriggeredByKey[leftE.Key] = true;
                    if (pairPool.RightFullness > rightCap) overflowTriggeredByKey[rightE.Key] = true;
                }
                overflowTotal += overflow;
            }
            else
            {
                foreach (var e in list)
                {
                    float stretchCap = e.Capacity * PoolModelConstants.StretchCapFactor;
                    float current = breastFullness.TryGetValue(e.Key, out float v) ? v : 0f;
                    float pressure = MilkCumSettings.enablePressureFactor
                        ? MilkCumSettings.GetPressureFactor(current / Mathf.Max(0.001f, stretchCap))
                        : (current >= stretchCap ? 0f : 1f);
                    float conditionsE = Pawn.GetConditionsForSide(e.Key);
                    float letdownE = MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(e.Key) : 1f;
                    float flowPerTick = e.FlowMultiplier * flowPerTickScale * conditionsE * pressure * letdownE;
                    if (IsOverflowState(e.Key, current, e.Capacity)) flowPerTick = 0f;
                    totalFlowThisStep += flowPerTick;
                    pressureWeightedSum += flowPerTick * pressure;
                    letdownWeightedSum += flowPerTick * letdownE;
                    conditionsWeightedSum += flowPerTick * conditionsE;
                    CachedFlowPerDayByKey[e.Key] = flowPerTick * (60000f / 60f);
                    var (newFullness, overflow) = FluidPoolState.SingleBreastTickGrowth(current, flowPerTick, e.Capacity, stretchCap);
                    breastFullness[e.Key] = newFullness;
                    if (overflow > 0f) overflowTriggeredByKey[e.Key] = true;
                    overflowTotal += overflow;
                }
            }
        }
        // 供 UI 读取：池逻辑实际进水流速（池单位/天）与流速加权因子，与回缩/溢出等完全一致
        CachedFlowPerDayForDisplay = totalFlowThisStep * (60000f / 60f);
        CachedFlowTick = Find.TickManager.TicksGame;
        float denom = totalFlowThisStep >= 1E-5f ? totalFlowThisStep : 1f;
        CachedPressureForDisplay = totalFlowThisStep >= 1E-5f ? pressureWeightedSum / denom : 1f;
        CachedLetdownForDisplay = totalFlowThisStep >= 1E-5f ? letdownWeightedSum / denom : 1f;
        CachedConditionsForDisplay = totalFlowThisStep >= 1E-5f ? conditionsWeightedSum / denom : (Pawn != null ? Pawn.GetMilkFlowMultiplierFromConditions() : 1f);
        float healthPercent = 1f;
        if (Pawn?.health?.summaryHealth != null)
            healthPercent = Mathf.Clamp(Pawn.health.summaryHealth.SummaryHealthPercent, 0.2f, 1f);
        float shrinkFactor = (1f - PoolModelConstants.ShrinkPerStep) * healthPercent;
        float reabsorbedPoolThisStep = 0f;
        var reabsorbedPerKey = new Dictionary<string, float>();
        foreach (var e in entries)
        {
            if (!breastFullness.TryGetValue(e.Key, out float cur)) continue;
            if (cur <= e.Capacity)
            {
                overflowTriggeredByKey[e.Key] = false;
                continue;
            }
            if (!IsOverflowState(e.Key, cur, e.Capacity)) continue;
            float shrinkAmount = (cur - e.Capacity) * (1f - shrinkFactor);
            reabsorbedPoolThisStep += shrinkAmount;
            reabsorbedPerKey[e.Key] = shrinkAmount;
            breastFullness[e.Key] = e.Capacity + (cur - e.Capacity) * shrinkFactor;
            if (breastFullness[e.Key] <= e.Capacity) overflowTriggeredByKey[e.Key] = false;
        }
        hadShrinkLastStep = reabsorbedPoolThisStep > 0f;
        cachedReabsorbedNutritionPerDay = hadShrinkLastStep
            ? reabsorbedPoolThisStep / PoolModelConstants.Interval60PerDay * PoolModelConstants.NutritionPerPoolUnit
                * Mathf.Clamp01(MilkCumSettings.reabsorbNutritionEfficiency)
            : 0f;
        float addBack = 0f;
        if (reabsorbedPoolThisStep > 0f && MilkCumSettings.reabsorbNutritionEnabled && Pawn != null)
        {
            int basis = Mathf.Clamp(MilkCumSettings.lactationExtraNutritionBasis, 0, 300);
            float factor = basis / 150f;
            addBack = reabsorbedPoolThisStep * PoolModelConstants.NutritionPerPoolUnit
                * Mathf.Clamp01(MilkCumSettings.reabsorbNutritionEfficiency) * factor;
            if (Pawn.needs?.food != null)
                Pawn.needs.food.CurLevel = Mathf.Clamp(Pawn.needs.food.CurLevel + addBack, 0f, Pawn.needs.food.MaxLevel);
            else if (Pawn.needs?.energy != null)
                Pawn.needs.energy.CurLevel = Mathf.Clamp(Pawn.needs.energy.CurLevel + addBack / MilkCumSettings.nutritionToEnergyFactor, 0f, Pawn.needs.energy.MaxLevel);
        }
        SyncLeftRightFromBreastFullness();
        var pool = new FluidPoolState();
        pool.SetFrom(leftFullness, rightFullness, ticksFullPool);
        pool.UpdateFullPoolCounter(0.95f * maxFullness, 60);
        ticksFullPool = pool.TicksFullPool;
        SyncBaseFullness();
        HandleOverflow(overflowTotal);
        TrySendFullPoolLetter();
        if (MilkCumSettings.lactationPoolTickLog && Pawn != null)
        {
            float flowAdded = Fullness - fullnessBefore + reabsorbedPoolThisStep;
            float curLevel = Pawn.needs?.food != null ? Pawn.needs.food.CurLevel : (Pawn.needs?.energy != null ? Pawn.needs.energy.CurLevel : -1f);
            MilkCumSettings.PoolTickLog($"{Pawn.Name} 饱食={curLevel:F3} 营养-{extraFall60:F4} 乳池+{flowAdded:F4} 回缩-{reabsorbedPoolThisStep:F4} 营养+{addBack:F4} 池满={Fullness:F3}");
            float totalInflow = 0f;
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                float before = fullnessBeforePerKey.TryGetValue(e.Key, out float b) ? b : 0f;
                float after = breastFullness.TryGetValue(e.Key, out float a) ? a : 0f;
                float reabsorb = reabsorbedPerKey.TryGetValue(e.Key, out float r) ? r : 0f;
                float inflow = after - before + reabsorb;
                totalInflow += inflow;
            }
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                float before = fullnessBeforePerKey.TryGetValue(e.Key, out float b) ? b : 0f;
                float after = breastFullness.TryGetValue(e.Key, out float a) ? a : 0f;
                float reabsorb = reabsorbedPerKey.TryGetValue(e.Key, out float r) ? r : 0f;
                float inflow = after - before + reabsorb;
                float nutritionShare = totalInflow >= PoolModelConstants.Epsilon ? extraFall60 * (inflow / totalInflow) : 0f;
                MilkCumSettings.PoolTickLog($"  池 {e.Key}: 营养-{nutritionShare:F4} 进+{inflow:F4} 回缩-{reabsorb:F4} 满={after:F3}");
            }
        }
    }

    /// <summary>3.3 满池事件：满池超过约 1 天且开启设置时，每 2 天最多发一封「需要挤奶」提醒信。见 Docs/泌乳系统逻辑图</summary>
    private void TrySendFullPoolLetter()
    {
        if (!MilkCumSettings.enableFullPoolLetter || Pawn == null || !Pawn.Spawned || !Pawn.IsColonyPawn()
            || ticksFullPool < 60000) return;
        int now = Find.TickManager.TicksGame;
        if (lastFullPoolLetterTick >= 0 && now - lastFullPoolLetterTick < 120000) return;
        lastFullPoolLetterTick = now;
        string title = "EM.FullPoolLetterTitle".Translate();
        if (string.IsNullOrEmpty(title) || title == "EM.FullPoolLetterTitle") title = "EM.MilkPoolFull".Translate();
        string text = "EM.FullPoolLetterText".Translate(Pawn.LabelShort);
        if (string.IsNullOrEmpty(text)) text = Pawn.LabelShort + " " + "EM.MilkPoolFull".Translate();
        Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent, new TargetInfo(Pawn));
    }

    /// <summary>满池溢出：累计溢出量达到阈值时生成地面污物（不扣水位）；污物 Def 由设置指定</summary>
    private void HandleOverflow(float overflowThisTick)
    {
        if (overflowThisTick <= 0f || Pawn == null || !Pawn.Spawned || Pawn.Map == null) { return; }
        overflowAccumulator += overflowThisTick;
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
            if (filthSpawned > 0 && Pawn.RaceProps.Humanlike && Pawn.needs?.mood?.thoughts?.memories != null
                && MilkCumDefOf.EM_MilkOverflow != null)
                Pawn.needs.mood.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_MilkOverflow);
            if (filthSpawned > 0 && Pawn.Spawned && Pawn.Map != null)
            {
                string moteText = "EM.MilkOverflowMote".Translate();
                if (!string.IsNullOrEmpty(moteText) && moteText != "EM.MilkOverflowMote")
                    MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, moteText, 2.5f);
            }
        }
        overflowAccumulator = Mathf.Min(overflowAccumulator, PoolModelConstants.OverflowFilthThreshold * 2f);
    }
}
