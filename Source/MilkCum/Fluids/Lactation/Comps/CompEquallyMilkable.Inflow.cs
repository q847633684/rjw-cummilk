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

    /// <summary>单池流速统一计算：倍率×基础流速×状态×压力×喷乳反射；返回流速与因子，供逐池计算复用。</summary>
    private (float flowPerTick, float pressure, float conditions, float letdown) ComputeSideFlow(
        FluidPoolEntry entry,
        float currentFullness,
        float stretchCap,
        float flowPerTickScale,
        HediffComp_EqualMilkingLactating lactatingComp,
        float residualL,
        FluidPoolNetwork network)
    {
        float pressure = MilkCumSettings.enablePressureFactor
            ? MilkCumSettings.GetPressureFactor(currentFullness / Mathf.Max(0.001f, stretchCap))
            : (currentFullness >= stretchCap ? 0f : 1f);
        MilkCumSettings.ApplyOverflowResidualFlow(ref pressure, currentFullness, stretchCap, residualL, lactatingComp.GetInflammationForKey(entry.Key));
        float conditions = Pawn.GetConditionsForPoolKey(entry.Key);
        float letdown = MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(entry.Key) : 1f;
        float duct = ComputeDuctConductance(entry, network);
        float flowPerTick = Mathf.Max(0f, entry.FlowMultiplier * flowPerTickScale * conditions * pressure * letdown * duct);
        return (flowPerTick, pressure, conditions, letdown);
    }

    /// <summary>压力-导管模型：导管阻力受网络拓扑、炎症与机器能力影响，返回 0~1+ 的有效导通系数。</summary>
    private float ComputeDuctConductance(FluidPoolEntry entry, FluidPoolNetwork network)
    {
        if (string.IsNullOrEmpty(entry.Key)) return 1f;
        float outlet = network?.GetOutletHopFactor(entry.Key, MilkCumSettings.ductHopPenaltyPerEdge) ?? 1f;
        float inflammation = 0f;
        var lactatingComp = Pawn?.LactatingHediffComp();
        if (lactatingComp != null)
            inflammation = Mathf.Clamp01(lactatingComp.GetInflammationForKey(entry.Key) / Mathf.Max(0.01f, MilkCumSettings.inflammationCrit));
        float baseResistance = 1f + inflammation * MilkCumSettings.ductInflowInflammationResistance;
        float conductance = outlet / Mathf.Max(MilkCumSettings.ductConductanceMin, baseResistance);
        return Mathf.Clamp(conductance, MilkCumSettings.ductConductanceMin, MilkCumSettings.ductConductanceMax);
    }

    /// <summary>水池模型：按池 key 逐个进水。流速 = 基础日流速×条目倍率×状态×压力×喷乳反射；压力来自 Logistic 曲线或关压时的阶跃（顶满为 0），近满撑大时可再经 overflowResidualFlowFactor 抬升下限以模拟持续分泌/渗漏。超过撑大上限的部分计为溢出污物；超出基础容量的乳量每 60 tick 按 ShrinkPerStep×健康度向基础收敛（与是否本步溢出无关），回缩量由 GetReabsorbedNutritionPerDay 折算营养。</summary>
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
        if (currentLactation <= 0f)
        {
            ResetMilkFlowDisplayCache();
            return;
        }
        // 软停流：极端饥饿时保留极低基线流速，避免 60 tick 级别“断崖式 0 流速”。
        hungerFactor = Mathf.Max(0.05f, hungerFactor);
        float drive = MilkCumSettings.GetEffectiveDrive(effectiveLForFlow);
        float raceFlow = MilkCumSettings.defaultFlowMultiplierForHumanlike;
        // 每条池乘 GetConditionsForPoolKey（按该 Hediff Part），不再乘全局 cond，避免双重惩罚。
        float basePerDay = drive * hungerFactor * raceFlow;
        var entries = GetCachedEntries();
        breastFullness ??= new Dictionary<string, float>();
        float overflowTotal = 0f;
        float flowPerTickScale = Mathf.Max(0f, basePerDay / PoolModelConstants.TicksPerGameDay * PoolModelConstants.Interval60Ticks);
        if (MilkCumSettings.lactationPoolTickLog && Pawn != null)
        {
            var sb = new StringBuilder();
            sb.Append("[MilkCum][INFO][MilkFlow] 小人=").Append(Pawn.LabelShort).Append(" 泌乳量L=").Append(currentLactation.ToString("F3"))
                .Append(" 驱动力drive=").Append(drive.ToString("F3")).Append(" 饥饿系数hunger=").Append(hungerFactor.ToString("F3"))
                .Append(" 种族流速倍率raceFlow=").Append(raceFlow.ToString("F3")).Append("; ");
            sb.Append("每日基础流速basePerDay=drive(").Append(drive.ToString("F3")).Append(")×hunger(").Append(hungerFactor.ToString("F3"))
                .Append(")×raceFlow(").Append(raceFlow.ToString("F3")).Append(")=").Append(basePerDay.ToString("F3")).Append("; ");
            sb.Append("每60tick进池量flowPer60tick=basePerDay(").Append(basePerDay.ToString("F3")).Append(")/60000×60=").Append(flowPerTickScale.ToString("F5")).Append("；");
            sb.Append("单侧实际流速≈flowPer60tick×条目流速倍率×该侧状态修正×压力因子×喷乳反射×导管导通；");
            sb.Append("substeps=").Append(IsInflowEventBurstActive() ? Mathf.Max(1, MilkCumSettings.inflowEventSubsteps) : 1);
            MilkCumSettings.PoolTickLog(sb.ToString());
        }
        if (MilkCumSettings.enableLetdownReflex)
            lactatingComp.DecayLetdownReflex(60f / 60f); // Δt = 60 tick = 1 分钟
        MilkRelatedHealthHelper.UpdateInflammationAndTryTriggerMastitis(lactatingComp, Fullness, maxFullness);
        float residualL = lactatingComp.CurrentLactationAmount;
        if (MilkCumSettings.enableToleranceDynamic)
            lactatingComp.UpdateToleranceDynamic(currentLactation, PoolModelConstants.Interval60PerDay);
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
        float stretchTotal = 0f;
        for (int i = 0; i < entries.Count; i++)
            stretchTotal += entries[i].Capacity * PoolModelConstants.StretchCapFactor;
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
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            float stretchCap = e.Capacity * PoolModelConstants.StretchCapFactor;
            float current = GetFullnessForKey(e.Key);
            float perStepFlowAcc = 0f;
            float sideOverflowAcc = 0f;
            float pressureAcc = 0f, letdownAcc = 0f, conditionsAcc = 0f;
            for (int s = 0; s < substeps; s++)
            {
                var sideFlow = ComputeSideFlow(e, current, stretchCap, flowPerTickScale / substeps, lactatingComp, residualL, network);
                float flowPerTick = sideFlow.flowPerTick;
                perStepFlowAcc += flowPerTick;
                pressureAcc += flowPerTick * sideFlow.pressure;
                letdownAcc += flowPerTick * sideFlow.letdown;
                conditionsAcc += flowPerTick * sideFlow.conditions;
                var growth = FluidPoolState.SingleBreastTickGrowth(current, flowPerTick, e.Capacity, stretchCap);
                current = growth.newFullness;
                sideOverflowAcc += growth.overflow;
                if (current > e.Capacity)
                {
                    float excess = current - e.Capacity;
                    float reabs = excess * (1f - shrinkFactor);
                    reabsorbedPoolThisStep += reabs;
                    reabsorbedPerKeyCache[e.Key] = (reabsorbedPerKeyCache.TryGetValue(e.Key, out var old) ? old : 0f) + reabs;
                    current = e.Capacity + excess * shrinkFactor;
                }
            }
            totalFlowThisStep += perStepFlowAcc;
            pressureWeightedSum += pressureAcc;
            letdownWeightedSum += letdownAcc;
            conditionsWeightedSum += conditionsAcc;
            CachedFlowPerDayByKey[e.Key] = perStepFlowAcc * (PoolModelConstants.TicksPerGameDay / PoolModelConstants.Interval60Ticks);
            breastFullness[e.Key] = current;
            overflowTotal += sideOverflowAcc;
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

    /// <summary>3.3 满池事件：满池超过约 1 天且开启设置时，每 Pawn 每日最多一封，且受 fullPoolLetterCooldownDays 冷却。见 Docs/泌乳系统逻辑图</summary>
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
