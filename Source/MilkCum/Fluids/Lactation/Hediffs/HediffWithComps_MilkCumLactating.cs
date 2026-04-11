using MilkCum.Core;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using rjw;
namespace MilkCum.Fluids.Lactation.Hediffs;

public class HediffWithComps_MilkCumLactating : HediffWithComps
{
    /// <summary>乳池是否达到「物理满」（撑大总容量×满池阈值）；与进水/TickGrowth 判定一致。</summary>
    private static bool IsPoolPhysicallyFull(CompEquallyMilkable comp)
    {
        if (comp == null) return false;
        float stretch = comp.GetPoolStretchCapacityTotal();
        if (stretch < 0.01f)
            return comp.Fullness >= Mathf.Max(0.01f, comp.maxFullness);
        return comp.Fullness >= stretch * PoolModelConstants.FullnessThresholdFactor;
    }

    /// <summary>Granularity: 4 steps per severity (0.25). Stage table covers 0..MaxSeverityForStages; higher severity uses last stage. maxSteps=81, count=162; if future allows severity>20 (e.g. large dose), display stays at last stage—not a bug, just a note.</summary>
    private const int SeverityStepsPerUnit = 4;
    private const int MaxSeverityForStages = 20;
    private HediffStage[] hediffStages;
    private HediffStage vanillaStage;
    private bool isDirty = true;
    public override HediffStage CurStage => GetCurStage();
    public override int CurStageIndex
    {
        get
        {
            try
            {
                var comp = pawn.CompEquallyMilkable();
                int raw = GetStageIndex(this.Severity, IsPoolPhysicallyFull(comp));
                // 信息�?原版会用 CurStageIndex 访问 def.stages[index]，必须落�?[0, def.stages.Count-1]，否则闪退
                if (def.stages == null || def.stages.Count == 0)
                    return 0;
                return Mathf.Min(raw, def.stages.Count - 1);
            }
            catch
            {
                return 0;
            }
        }
    }
    public override void PostTick()
    {
        if (isDirty)
        {
            isDirty = false;
            this.GenStages();
        }
        base.PostTick();
    }
    public override bool TryMergeWith(Hediff other)
    {
        if (other == null || other.def != this.def)
            return false;
        this.Severity += other.Severity;
        if (other.Severity >= 1f)
            this.Severity = Mathf.Floor(this.Severity);
        if (other.Severity >= 1f || this.Severity >= 1f)
            this.ageTicks = 0;
        // 水池模型：合并进来的 Severity 也需同步到 L。合并来自本 tick 吃药时，抑制本次 AddFromDrug 的日志，由 DoIngestionOutcome_Postfix 统一打「一针一条」。
        if (other.Severity > 0f && comps != null)
        {
            foreach (var c in comps)
                if (c is HediffComp_EqualMilkingLactating comp)
                {
                    comp.LastMergedOtherSeverity = other.Severity;
                    comp.SuppressDrugIntakeLog = true; // 已泌乳再次吃药时由 postfix 统一打一条，避免一针两套日志
                    comp.AddFromDrug(other.Severity, syncSeverity: false);
                    comp.MergedFromIngestionThisTick = true;
                    break;
                }
        }
        return true;
    }
    public void SetDirty()
    {
        isDirty = true;
    }
    private HediffComp_EqualMilkingLactating LactatingComp => this.TryGetComp<HediffComp_EqualMilkingLactating>();

    public void OnGathered()
    {
        if (this.Severity < 1f)
            this.Severity = Mathf.Min(1f, this.Severity + 0.5f); // 挤奶时最多推到 1，不直接变永久；永久=成瘾且满足需求不衰减
        if (MilkCumSettings.enableInflammationModel)
            LactatingComp?.AddMilkingLStimulus();
    }
    public void OnGathered(float fullness)
    {
        if (this.Severity < 1f && this.Severity + fullness >= 1f)
            this.Severity = 1f;
        else
            this.Severity += fullness;
        if (MilkCumSettings.enableInflammationModel)
            LactatingComp?.AddMilkingLStimulus();
    }
    /// <summary>挤奶/吸奶后按被扣量的虚拟槽键添加喷乳反射。</summary>
    public void OnGatheredLetdownByKeys(IEnumerable<string> drainedKeys) =>
        OnGatheredLetdownByKeys(drainedKeys, MilkingStimulusSource.Generic);

    /// <summary>同上，可选刺激源（SYS-06 婴/机倍率）。</summary>
    public void OnGatheredLetdownByKeys(IEnumerable<string> drainedKeys, MilkingStimulusSource stimulusSource)
    {
        if (drainedKeys == null) return;
        var comp = LactatingComp;
        foreach (string key in drainedKeys)
            comp?.AddLetdownReflexStimulus(key);
        if (MilkCumSettings.enableInflammationModel)
            comp?.AddMilkingLStimulus(stimulusSource);
    }
    /// <summary>设计原则 5：stages �?XML 移除，此�?C# 动态生成；capMods �?GenStage 内设置（当前 Clear �?EM_LactatingGain 等独�?Hediff 提供能力），避免�?Def 静态阶段冲突</summary>
    private void GenStages()
    {
        int maxSteps = MaxSeverityForStages * SeverityStepsPerUnit + 1;
        int count = maxSteps * 2;
        if (hediffStages == null || hediffStages.Length != count)
            hediffStages = new HediffStage[count];
        bool isPermanent = this.TryGetComp<HediffComp_EqualMilkingLactating>()?.IsPermanentLactation ?? false;
        for (int i = 0; i < maxSteps; i++)
        {
            float severity = (float)i / SeverityStepsPerUnit;
            GenStage(ref hediffStages[i * 2], severity, false, isPermanent);
            GenStage(ref hediffStages[i * 2 + 1], severity, true, isPermanent);
        }
        this.vanillaStage = new HediffStage { fertilityFactor = 0.05f };
    }
    /// <summary>Stage 名（括号内）：仅当真正不衰减时显示「永久」，否则留空；具体「天数：X」在 CompLabelInBracketsExtra 动态显示</summary>
    private void GenStage(ref HediffStage stage, float severity, bool isFull, bool isPermanentLactation)
    {
        if (stage == null) { stage = new HediffStage(); }
        int severityInt = Mathf.FloorToInt(severity);
        if (severityInt >= 1) { stage.label = isPermanentLactation ? Lang.Permanent : ""; }
        else { stage.label = ""; }
        // 不再使用 hungerRateFactorOffset，由 Need_Food.NeedInterval 补丁直接扣/加饱食度（营养↔乳池 1:1）
        if (stage.capMods != null) stage.capMods.Clear();
    }
    private int GetStageIndex(float severity, bool isFull)
    {
        int maxStep = MaxSeverityForStages * SeverityStepsPerUnit;
        int step = Mathf.Min((int)(severity * SeverityStepsPerUnit), maxStep);
        return step * 2 + (isFull ? 1 : 0);
    }
    private HediffStage GetCurStage()
    {
        try
        {
            if (pawn == null || pawn.RaceProps == null)
                return vanillaStage ?? (vanillaStage = new HediffStage { fertilityFactor = 0.05f });
            if (this.hediffStages == null || this.hediffStages.Length == 0 || this.hediffStages[0] == null)
                GenStages();
            if (hediffStages == null || hediffStages.Length == 0)
                return vanillaStage ?? (vanillaStage = new HediffStage { fertilityFactor = 0.05f });
            if (!pawn.IsMilkable() && pawn.RaceProps.Humanlike)
            {
                if (this.Severity > 1f) { this.Severity = 0.9999f; }
                return vanillaStage ?? (vanillaStage = new HediffStage { fertilityFactor = 0.05f });
            }
            var comp = pawn.CompEquallyMilkable();
            int raw = GetStageIndex(this.Severity, IsPoolPhysicallyFull(comp));
            int idx = Mathf.Clamp(raw, 0, hediffStages.Length - 1);
            HediffStage stage = hediffStages[idx];
            // 营养→乳池：不再用饥饿率乘数（易出 bug），改由 Need_Food.NeedInterval 补丁直接扣/加饱食度；此处不设 offset。
            stage.hungerRateFactorOffset = 0f;
            return stage;
        }
        catch
        {
            return vanillaStage ?? (vanillaStage = new HediffStage { fertilityFactor = 0.05f });
        }
    }
    public override bool Visible => (pawn?.RaceProps != null) && (pawn.IsMilkable() || pawn.RaceProps.Humanlike); //Milkable or breastfeedable in vanilla.
}

public class HediffCompProperties_EqualMilkingLactating : HediffCompProperties_Chargeable
{
    public HediffCompProperties_EqualMilkingLactating()
    {
        compClass = typeof(HediffComp_EqualMilkingLactating);
    }
}
