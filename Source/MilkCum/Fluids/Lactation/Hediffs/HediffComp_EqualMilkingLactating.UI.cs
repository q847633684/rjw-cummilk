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

    /// <summary>健康页乳房悬停 DevMode：返回「生产机制」顺序的因子行（RJW GetFluidMultiplier 侧倍率、营规、状态、泌乳素、饥饿、压力、喷乳反射）</summary>
    public static System.Collections.Generic.List<string> BuildBreastEfficiencyFactorLinesForDevMode(FlowBreakdown b, float sideMult, float? letdownForSide = null, float? pressureForSide = null, float? conditionsForSide = null)
    {
        float letdown = letdownForSide ?? b.Letdown;
        float pressure = pressureForSide ?? b.Pressure;
        float conditions = conditionsForSide ?? b.Conditions;
        var list = new System.Collections.Generic.List<string>();
        list.Add("EM.MilkFlowFactorLine".Translate("EM.MilkFlowBreastVolume".Translate(), FormatFlowFactor(sideMult)));
        list.Add("EM.MilkFlowFactorLine".Translate("EM.MilkFlowSetting".Translate(), FormatFlowFactor(b.Setting)));
        list.Add("EM.MilkFlowFactorLine".Translate("EM.MilkFlowConditions".Translate(), FormatFlowFactor(conditions)));
        list.Add("EM.MilkFlowFactorLine".Translate("EM.MilkFlowDrive".Translate(), FormatFlowFactor(b.Drive)));
        list.Add("EM.MilkFlowFactorLine".Translate("EM.MilkFlowHunger".Translate(), FormatFlowFactor(b.Hunger)));
        list.Add("EM.MilkFlowFactorLine".Translate("EM.MilkFlowPressure".Translate(), FormatFlowFactor(pressure)));
        list.Add("EM.MilkFlowFactorLine".Translate("EM.MilkFlowLetdown".Translate(), FormatFlowFactor(letdown)));
        return list;
    }

    public override string CompTipStringExtra
    {
        get
        {
            try
            {
            if (!Pawn.IsMilkable())
                return base.CompTipStringExtra;
            var lines = new List<string>();
            float stretchCap = Mathf.Max(0.01f, CompEquallyMilkable?.GetPoolStretchCapacityTotal() ?? 1f);
            float totalMilk = CompEquallyMilkable != null ? CompEquallyMilkable.Fullness : Charge;
            bool isFull = totalMilk >= stretchCap * PoolModelConstants.FullnessThresholdFactor;
            float reabsorbed = CompEquallyMilkable != null ? CompEquallyMilkable.GetReabsorbedNutritionPerDay() : 0f;
            bool isShrinking = reabsorbed > 0f;
            float growthSpeed = PawnUtility.BodyResourceGrowthSpeed(Pawn);

            // 1. 状态总括（仅一行：产奶中 / 池满 / 池满回缩中 / 饥饿红字）
            if (growthSpeed == 0f)
                lines.Add("LactatingStoppedBecauseHungry".Translate().Colorize(ColorLibrary.RedReadable));
            else if (isFull)
                lines.Add(isShrinking ? "EM.LactatingStateFullShrinking".Translate() : "EM.LactatingStateFull".Translate());
            else
                lines.Add("EM.LactatingStateProducing".Translate());

            // 1.1 停止原因列表（上游行为）：可能同时命中多项时逐条显示，便于排查“为什么停奶”。
            var stopReasons = new List<string>();
            if (growthSpeed == 0f)
                stopReasons.Add("LactatingStoppedBecauseHungry".Translate().Colorize(ColorLibrary.RedReadable));
            if (isFull)
                stopReasons.Add("LactatingStoppedBecauseFull".Translate());
            if (stopReasons.Count > 0)
            {
                foreach (var reason in stopReasons)
                    lines.Add("  - " + reason);
            }

            lines.Add("");
            // 2. 储量（仅总体；虚拟左/右或每叶明细见健康表里各 RJW 乳房行的悬停）
            lines.Add("EM.PoolSectionStorage".Translate());
            if (CompEquallyMilkable != null)
            {
                float poolBase = Mathf.Max(0.01f, CompEquallyMilkable.GetPoolBaseCapacityTotal());
                string totalPercentStr = poolBase >= 0.001f ? (totalMilk / poolBase).ToStringPercent() : "0%";
                lines.Add("  " + "EM.PoolBreastTotalMilkLine".Translate(
                    totalMilk.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    poolBase.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    totalPercentStr));
                lines.Add("  " + "EM.PoolBreastStretchCapLine".Translate(stretchCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
            };

            // 3. 产奶流速（池/天）：总流速仅读池逻辑在 UpdateMilkPools 中写入的缓存，保证 UI 与池逻辑单一数据源一致
            lines.Add("EM.PoolSectionFlow".Translate());
            if (growthSpeed > 0f)
            {
                var b = GetFlowPerDayBreakdown();
                lines.Add("  " + "EM.PoolBreastTotalFlowLine".Translate(b.TotalFlow.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                if (isFull)
                {
                    if (isShrinking)
                        lines.Add("  " + "EM.ReabsorbedNutritionPerDay".Translate(reabsorbed.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                    if (b.TotalFlow >= 0.001f && (MilkCumSettings.enablePressureFactor || MilkCumSettings.overflowResidualFlowFactor > 0.0001f))
                        lines.Add("  " + "EM.MilkFlowPressureWhenFull".Translate());
                }
            }
            lines.Add("  " + "EM.PoolFlowEstrogenNote".Translate());

            // 4. 消耗：仅当有额外营养/能量消耗且未满池时才显示整块，避免空标题
            if (growthSpeed > 0f && !isFull)
            {
                float extraNut = Pawn.needs?.food != null ? ExtraNutritionPerDay() : 0f;
                float extraEnergy = Pawn.needs?.energy != null ? ExtraEnergyPerDay() : 0f;
                if (extraNut >= 0.0001f || extraEnergy >= 0.0001f)
                {
                    lines.Add("EM.PoolSectionConsumption".Translate());
                    if (Pawn.needs?.food != null && extraNut >= 0.0001f)
                        lines.Add("  " + "EM.LactatingExtraNutritionShort".Translate(extraNut.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                    if (Pawn.needs?.energy != null && extraEnergy >= 0.0001f)
                        lines.Add("  " + ("CurrentMechEnergyFallPerDay".Translate() + ": " + extraEnergy.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                }
            }

            // 5. 周期
            lines.Add("EM.PoolSectionCycle".Translate());
            lines.Add("  " + ("EM.PoolRemainingDays".Translate() + ": " + (IsPermanentLactation ? Lang.Permanent : RemainingDays.ToString("F1"))));
            float oneDoseL = 0.5f * PoolModelConstants.DoseToLFactor;
            if (oneDoseL > 0f && currentLactationAmount > 0f)
                lines.Add("  " + "EM.EquivalentDose".Translate((currentLactationAmount / oneDoseL).ToString("F1")));

            return lines.Count > 0 ? string.Join("\n", lines) : base.CompTipStringExtra;
            }
            catch (Exception ex)
            {
                Log.Warning($"[MilkCum] HediffWithComps_MilkCumLactating.CompTipStringExtra: {ex.Message}");
                return base.CompTipStringExtra;
            }
        }
    }
    /// <summary>括号内保留「天数 + 总体满度%」；分侧满度见各 RJW 乳房行悬停。</summary>
    public override string CompLabelInBracketsExtra
    {
        get
        {
            float denom = Mathf.Max(0.01f, CompEquallyMilkable?.GetPoolStretchCapacityTotal() ?? 1f);
            float fullness = CompEquallyMilkable != null ? CompEquallyMilkable.Fullness : Charge;
            string head = IsPermanentLactation ? "" : ("EM.PoolDaysPrefix".Translate() + RemainingDays.ToString("F1") + "EM.PoolDaysSuffix".Translate() + " ");
            return base.CompLabelInBracketsExtra + head + Lang.MilkFullness + ": " + (fullness / denom).ToStringPercent();
        }
    }
    public override string CompDebugString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append(base.CompDebugString());
        if (!base.Pawn.Dead)
        {
            stringBuilder.AppendLine("remainingDays(computed): " + RemainingDays.ToString("F2"));
            stringBuilder.AppendLine("currentLactationAmount(L): " + currentLactationAmount.ToString("F3"));
            stringBuilder.AppendLine("effectiveDrugFactor(E): " + GetEffectiveDrugFactor().ToString("F3"));
            if (CompEquallyMilkable != null)
            {
                stringBuilder.AppendLine("pool L: " + CompEquallyMilkable.LeftFullness.ToString("F3") + " R: " + CompEquallyMilkable.RightFullness.ToString("F3"));
                stringBuilder.AppendLine("overflowAccumulator: " + CompEquallyMilkable.OverflowAccumulator.ToString("F3"));
            }
            if (MilkCumSettings.enableInflammationModel)
            {
                stringBuilder.AppendLine("inflammation I max: " + CurrentInflammation.ToString("F3"));
                var ent = Pawn?.GetResolvedBreastPoolEntries();
                if (ent != null)
                {
                    for (int i = 0; i < ent.Count; i++)
                    {
                        string k = ent[i].Key;
                        if (string.IsNullOrEmpty(k)) continue;
                        stringBuilder.AppendLine("  I[" + k + "]: " + GetInflammationForKey(k).ToString("F3"));
                    }
                }
            }
            if (Pawn?.RaceProps?.Humanlike == true)
            {
                float hygieneRisk = DubsBadHygieneIntegration.GetHygieneRiskFactorForMastitis(Pawn);
                stringBuilder.AppendLine("hygieneRisk(mastitis): " + hygieneRisk.ToString("F2"));
            }
        }
        return stringBuilder.ToString().TrimEndNewlines();
    }
    public void SetMilkFullness(float fullness)
    {
        CompEquallyMilkable.SetFullness(fullness);
        if (fullness < Charge) { this.Parent.OnGathered(Charge - fullness); }
        this.Charge = fullness;
    }
    /// <summary>吸奶/消费后：仅将 Charge 同步为当前池总满度，并触发 OnGathered（不按比例缩放池，因已由 DrainForConsume 按对按侧扣过）</summary>
    public void SyncChargeFromPool()
    {
        float oldCharge = Charge;
        Charge = CompEquallyMilkable != null ? CompEquallyMilkable.Fullness : 0f;
        if (CompEquallyMilkable != null && oldCharge > Charge)
            Parent.OnGathered(oldCharge - Charge);
    }
    new public HediffCompProperties_EqualMilkingLactating Props
    {
        get
        {
            return (HediffCompProperties_EqualMilkingLactating)this.props;
        }
    }
}
