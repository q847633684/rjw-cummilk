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

    /// <summary>归一化基准量 = 总容量（规格与水池模型一致），当前实现恒为 1。</summary>
    public static float GetBaseValueNormalized(Pawn pawn = null) => 1f;

    /// <summary>泌乳药效对水池的额外倍率；耐受已由原版/XML 体现在合并 severity，此处恒为 1。</summary>
    public float GetEffectiveDrugFactor() => 1f;

    /// <summary>流速因子显示用：整数显示为 "1"，否则保留 1～2 位小数。</summary>
    private static string FormatFlowFactor(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return "0";
        if (Mathf.Approximately(v, Mathf.Floor(v))) return ((int)v).ToString();
        return v <= 10f ? v.ToString("F2").TrimEnd('0').TrimEnd('.') : v.ToString("F1");
    }

    /// <summary>吃药时进水：ΔL = Δs × C_dose；若 syncSeverity 为 true 则同时增加 parent.Severity，供原版 SeverityPerDay 驱动衰减与面板显示。合并时调用 syncSeverity: false（Severity 已由 TryMergeWith 更新）。
    /// 封顶+倍数逻辑与 AddRemainingDays 共用 ApplyDeltaLToLAndSeverity。</summary>
    public void AddFromDrug(float deltaSeverity, bool syncSeverity = true)
    {
        float remainingBefore = RemainingDays;
        float deltaL = deltaSeverity * PoolModelConstants.DoseToLFactor;
        if (syncSeverity)
            ApplyDeltaLToLAndSeverity(deltaL);
        else
            lactationAmountFromDrug += deltaL;
        if (MilkCumSettings.lactationDrugIntakeLog && !SuppressDrugIntakeLog)
        {
            float remainingAfter = RemainingDays;
            float cDose = PoolModelConstants.DoseToLFactor;
            float deltaRemaining = remainingAfter - remainingBefore;
            Verse.Log.Message($"[MilkCum][INFO][LactationDrug] pawn={Pawn?.LabelShort} tick={Find.TickManager.TicksGame} mode=AddFromDrug_Base");
            Verse.Log.Message($"[MilkCum][INFO][LactationDrug] input Δs={deltaSeverity:F3} doseToL={cDose:F2}");
            Verse.Log.Message($"[MilkCum][INFO][LactationDrug] result ΔL={deltaL:F3} remainBefore={remainingBefore:F1}d remainAfter={remainingAfter:F1}d Δremain={deltaRemaining:+0.0;-0.0;0.0}d");
            Verse.Log.Message($"[MilkCum][INFO][LactationDrug] 公式 ΔL=Δs({deltaSeverity:F3})×C_dose({cDose:F2})={deltaL:F3}；剩余={remainingBefore:F1}d+Δ天数({deltaRemaining:+0.0;-0.0;0.0})d={remainingAfter:F1}d");
        }
    }

    /// <summary>分娩时累加：L 与 Severity 均加上基础值（不乘有效药效系数），原版 SeverityPerDay 对总 severity 生效。</summary>
    public void AddFromBirth()
    {
        float baseVal = GetBaseValueNormalized(Pawn);
        lactationAmountFromBirth += baseVal;
        if (Parent != null)
            Parent.Severity = Mathf.Min(Parent.def.maxSeverity, Parent.Severity + baseVal);
    }

    /// <summary>一次吸奶或挤奶增加若干天泌乳时间：仅当 Def 含负的 severityPerDay 时，按 days×|severityPerDay| 增加 L 与 Severity（与原版 RemainingDays 一致）。无该 comp 时不做任何事。</summary>
    public void AddRemainingDays(float days)
    {
        if (days <= 0f || IsPermanentLactation) return;
        if (Parent?.def?.comps == null) return;
        foreach (var c in Parent.def.comps)
        {
            if (c is HediffCompProperties_SeverityPerDay sp && sp.severityPerDay < 0f)
            {
                float deltaL = days * (-sp.severityPerDay);
                ApplyDeltaLToLAndSeverity(deltaL);
                return;
            }
        }
    }

    /// <summary>将 ΔL 应用到 L 与 Severity；启用 lactationLevelCap 时 L 封顶，超出部分按 lactationLevelCapDurationMultiplier 转为 Severity（与 AddFromDrug 一致）。</summary>
    private void ApplyDeltaLToLAndSeverity(float deltaL)
    {
        float cap = MilkCumSettings.lactationLevelCap;
        if (cap <= 0f)
        {
            lactationAmountFromDrug += deltaL;
            if (Parent != null)
                Parent.Severity = Mathf.Min(Parent.def.maxSeverity, Parent.Severity + deltaL);
            return;
        }
        float deltaLToL = Mathf.Max(0f, cap - currentLactationAmount);
        deltaLToL = Mathf.Min(deltaLToL, deltaL);
        lactationAmountFromDrug += deltaLToL;
        float overflow = deltaL - deltaLToL;
        float overflowSeverity = overflow * Mathf.Max(0.1f, MilkCumSettings.lactationLevelCapDurationMultiplier);
        if (Parent != null)
            Parent.Severity = Mathf.Min(Parent.def.maxSeverity, Parent.Severity + deltaLToL + overflowSeverity);
    }
}
