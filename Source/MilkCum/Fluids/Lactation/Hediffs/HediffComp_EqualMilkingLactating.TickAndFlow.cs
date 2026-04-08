using MilkCum.Core;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;
using MilkCum.Integration.DubsBadHygiene;
using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using rjw;

namespace MilkCum.Fluids.Lactation.Hediffs;

public partial class HediffComp_EqualMilkingLactating
{

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (!Pawn.IsMilkable())
        {
            base.CompPostTick(ref severityAdjustment);
            return;
        }
        // 水池模型（L 驱动）：每 200 tick 更新。原版逻辑下 L 与 Severity 同步，衰减由 HediffComp_SeverityPerDay 驱动，此处只同步 L 并判定结束；永久泌乳/动物不衰减。
        if (Pawn.IsHashIntervalTick(200))
        {
            bool permanentOrAnimal = Pawn.genes?.HasActiveGene(MilkCumDefOf.EM_Permanent_Lactation) == true
                || (MilkCumSettings.femaleAnimalAdultAlwaysLactating && Pawn.IsAdultFemaleAnimalOfColony());
            if (permanentOrAnimal)
            {
                if (currentLactationAmount < PoolModelConstants.LactationEndEpsilon)
                {
                    lactationAmountFromDrug = PoolModelConstants.BaseValueTBirth;
                    lactationAmountFromBirth = 0f;
                }
                if (!cachedWasPermanentLactation)
                {
                    cachedWasPermanentLactation = true;
                    Parent.SetDirty();
                }
                // 不衰�?L，池满时流�?0 �?L 保持，池空后流速自然恢�?
            }
            else
            {
                cachedWasPermanentLactation = false;
                float cap = MilkCumSettings.lactationLevelCap;
                float target = cap > 0f ? Mathf.Min(Parent.Severity, cap) : Parent.Severity;
                if (MilkCumSettings.realismWeaningCurve)
                {
                    float halfLifeTicks = Mathf.Max(600f, MilkCumSettings.realismWeaningHalfLifeDays * GenDate.TicksPerDay);
                    float alpha = 1f - Mathf.Exp(-200f / halfLifeTicks);
                    if (target > lactationAmountFromDrug + 1e-6f)
                        lactationAmountFromDrug = target;
                    else
                        lactationAmountFromDrug = Mathf.Lerp(lactationAmountFromDrug, target, alpha);
                }
                else
                    lactationAmountFromDrug = target;
                lactationAmountFromBirth = 0f;
                if (currentLactationAmount < PoolModelConstants.LactationEndEpsilon)
                {
                    ResetAndRemoveLactating();
                    return;
                }
            }
        }
        this.Charge = CompEquallyMilkable != null ? CompEquallyMilkable.Fullness : 0f;
        if (Pawn != null && ModIntegrationGates.RjwModActive && MilkCumSettings.rjwPermanentBreastGainFromLactationEnabled)
            lactationTicksAccumulated++;
    }

    /// <summary>是否达到下一档「因泌乳永久撑大」里程碑；若达到则递增 permanentBreastGainMilestonesDone 并返回 true，由 RJW GameComponent 调用 ApplyPermanentBreastGain。仅当 RJW 已加载且 rjwPermanentBreastGainFromLactationEnabled 时有效。</summary>
    public bool TryConsumeNextPermanentGainMilestone()
    {
        if (!ModIntegrationGates.RjwModActive || !MilkCumSettings.rjwPermanentBreastGainFromLactationEnabled) return false;
        float days = MilkCumSettings.rjwPermanentBreastGainDaysPerMilestone;
        if (days <= 0f) return false;
        int ticksPerMilestone = (int)(days * 60000f);
        if (ticksPerMilestone <= 0) return false;
        int next = (permanentBreastGainMilestonesDone + 1) * ticksPerMilestone;
        if (lactationTicksAccumulated < next) return false;
        permanentBreastGainMilestonesDone++;
        return true;
    }

    /// <summary>泌乳结束：清空乳池、移除 Lactating hediff</summary>
    private void ResetAndRemoveLactating()
    {
        lactationAmountFromDrug = 0f;
        lactationAmountFromBirth = 0f;
        CompEquallyMilkable?.ClearPools();
        Pawn.health.RemoveHediff(parent);
    }
    /// <summary>灌满期间额外营养/天：与产奶流速平衡，flow（池单位/天）× NutritionPerPoolUnit；满池后不额外消耗</summary>
    public float ExtraNutritionPerDay()
    {
        if (Pawn.needs?.food == null) return 0f;
        float flow = GetFlowPerDay();
        return flow * PoolModelConstants.NutritionPerPoolUnit;
    }
    /// <summary>机械体灌满期间额外能�?天：与产奶流�?1:1 平衡，flowPerDay（池单位/天）× nutritionToEnergyFactor；满池后不额外消耗</summary>
    public float ExtraEnergyPerDay()
    {
        if (Pawn.needs?.energy == null) return 0f;
        float flow = GetFlowPerDay();
        return flow * MilkCumSettings.nutritionToEnergyFactor;
    }
    /// <summary>当前产奶流速（池单位/天）。仅读池逻辑缓存，缓存未刷新时返回 0（逻辑由 UpdateMilkPools 统一维护）。</summary>
    internal float GetFlowPerDay()
    {
        var milkComp = CompEquallyMilkable;
        return milkComp?.GetTotalFlowPerDayCached() ?? 0f;
    }

    /// <summary>产奶流速拆解：总流速与各乘数因子，用于悬停显示。总流速仅读池逻辑缓存，缓存未刷新时 TotalFlow=0；RJW 乳房体积倍率在乳房池 Tooltip 中由 GetFlowPerDayForBreastSides 的 mult 提供。</summary>
    internal FlowBreakdown GetFlowPerDayBreakdown()
    {
        var r = new FlowBreakdown();
        var milkComp = CompEquallyMilkable;
        if (milkComp == null) return r;
        float hungerFactor = PawnUtility.BodyResourceGrowthSpeed(Pawn);
        if (currentLactationAmount <= 0f || hungerFactor <= 0f) return r;
        r.Drive = MilkCumSettings.GetEffectiveDrive(EffectiveLactationAmountForFlow);
        r.Hunger = hungerFactor;
        r.Conditions = Pawn.GetMilkFlowMultiplierFromConditions();
        r.Setting = MilkCumSettings.defaultFlowMultiplierForHumanlike;
        if (!milkComp.IsCachedFlowValid())
        {
            r.TotalFlow = 0f;
            return r;
        }
        r.TotalFlow = milkComp.CachedFlowPerDayForDisplay;
        r.Pressure = milkComp.CachedPressureForDisplay;
        r.Letdown = milkComp.CachedLetdownForDisplay;
        r.Conditions = milkComp.CachedConditionsForDisplay;
        return r;
    }

    /// <summary>产奶流速拆解：总流速与各乘数因子（�?GetFlowPerDay 一致），用�?UI 显示</summary>
    public struct FlowBreakdown
    {
        public float TotalFlow;
        public float Drive;
        public float Hunger;
        public float Conditions;
        public float Setting;
        public float Pressure;
        public float Letdown;
    }
}
