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

public partial class HediffComp_EqualMilkingLactating : HediffComp_Lactating
{
    /// <summary>水池模型：药物诱发的 L 分量；与 parent.Severity 同步维护，日变化由原版 SeverityPerDay 驱动。</summary>
    private float lactationAmountFromDrug;
    /// <summary>水池模型：分娩诱发的 L 分量；同上，与 Severity 同步。</summary>
    private float lactationAmountFromBirth;
    /// <summary>当前泌乳量 L = 药物分量 + 分娩分量</summary>
    private float currentLactationAmount => lactationAmountFromDrug + lactationAmountFromBirth;
    /// <summary>喷乳反射 R∈[0,1]，键与当前 <see cref="FluidPoolEntry.Key"/> 一致（虚拟左/右枚举名、每叶稳定键等）。<see cref="SyncPoolKeyedStateToEntries"/> 在池键迁移时重建本字典。挤奶/吸奶对该键提升 R。</summary>
    private Dictionary<string, float> letdownReflexByKey;
    /// <summary>四层模型：炎症强度 I。每 60 tick 离散更新；I&gt;I_crit 触发乳腺炎；键同 <see cref="FluidPoolEntry.Key"/>。</summary>
    private Dictionary<string, float> inflammationByKey;
    /// <summary>挤奶 L 刺激：当日已累计量，每游戏日重置</summary>
    private float milkingLStimulusAccumulatedThisDay;
    private int lastMilkingLStimulusDayTick = -1;
    /// <summary>上次判定时是否为永久泌乳，用于从非永久变为永久时 SetDirty 刷新阶段名</summary>
    private bool cachedWasPermanentLactation;
    /// <summary>累计泌乳 tick，用于「因泌乳永久撑大」里程碑判定；仅当 RJW 已加载且 rjwPermanentBreastGainFromLactationEnabled 时累加。</summary>
    private int lactationTicksAccumulated;
    /// <summary>已触发的永久体型增益里程碑次数；每达 rjwPermanentBreastGainDaysPerMilestone 天递增一次并对 RJW 乳房 SetSeverity(base + delta)。</summary>
    private int permanentBreastGainMilestonesDone;
    /// <summary>本 tick 内 TryMergeWith 是否已把 other.Severity 同步到 L（仅本次 ingestion 有效，不存档）。已合并时原版已乘耐受，postfix 不再加量；未合并时 postfix 加 deltaS。</summary>
    internal bool MergedFromIngestionThisTick;
    /// <summary>本 tick 合并时传入的 other.Severity，供验证「原版是否已按耐受修正给药 severity」时打日志；仅本次 ingestion 有效，不存档。</summary>
    internal float LastMergedOtherSeverity;
    /// <summary>抑制本 tick 内 AddFromDrug 的「吃药进水」调试日志，用于在上层聚合为「每次吃药一条」。不存档。</summary>
    internal bool SuppressDrugIntakeLog;

    public CompEquallyMilkable CompEquallyMilkable => this.Pawn.CompEquallyMilkable();
    public HediffWithComps_MilkCumLactating Parent => (HediffWithComps_MilkCumLactating)this.parent;

    /// <summary>剩余天数（游戏日）：仅按原版 <see cref="HediffCompProperties_SeverityPerDay"/>（&lt;0）→ Severity÷|severityPerDay|；与原版日衰减一致。无该 comp 时返回 0。永久泌乳→正无穷（表格列单独显示「永久」）。</summary>
    public float RemainingDays
    {
        get
        {
            if (IsPermanentLactation)
                return float.PositiveInfinity;
            if (Parent?.def?.comps != null)
            {
                foreach (var c in Parent.def.comps)
                    if (c is HediffCompProperties_SeverityPerDay sp && sp.severityPerDay < 0f)
                        return Parent.Severity / (-sp.severityPerDay);
            }
            return 0f;
        }
    }
    /// <summary>当前泌乳水平 L（规格：基准量 = 总容量，归一化基准为 1）</summary>
    public float CurrentLactationAmount => currentLactationAmount;

    /// <summary>用于流速/驱动力计算的 L：当 lactationLevelCap&gt;0 时取 min(L, cap)，避免超上限时流速过高。</summary>
    public float EffectiveLactationAmountForFlow
    {
        get
        {
            float cap = MilkCumSettings.lactationLevelCap;
            return cap > 0f ? Mathf.Min(currentLactationAmount, cap) : currentLactationAmount;
        }
    }

    /// <summary>是否为永久泌乳（基因或动物设置）：L 不衰减；括号阶段名与悬停「剩余时间」均据此显示「永久」</summary>
    public bool IsPermanentLactation =>
        Pawn.genes?.HasActiveGene(MilkCumDefOf.EM_Permanent_Lactation) == true
        || (MilkCumSettings.femaleAnimalAdultAlwaysLactating && Pawn.IsAdultFemaleAnimalOfColony());

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref lactationAmountFromDrug, "EM.PoolLactationFromDrug", 0f);
        Scribe_Values.Look(ref lactationAmountFromBirth, "EM.PoolLactationFromBirth", 0f);
        List<string> letdownKeys = null;
        List<float> letdownVals = null;
        if (Scribe.mode == LoadSaveMode.Saving && letdownReflexByKey != null && letdownReflexByKey.Count > 0)
        {
            letdownKeys = letdownReflexByKey.Keys.ToList();
            letdownVals = letdownReflexByKey.Values.ToList();
        }
        Scribe_Collections.Look(ref letdownKeys, "EM.MilkSiteLetdownK", LookMode.Value);
        Scribe_Collections.Look(ref letdownVals, "EM.MilkSiteLetdownV", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && letdownKeys != null && letdownVals != null && letdownKeys.Count == letdownVals.Count)
        {
            letdownReflexByKey ??= new Dictionary<string, float>();
            letdownReflexByKey.Clear();
            for (int i = 0; i < letdownKeys.Count; i++)
                if (!string.IsNullOrEmpty(letdownKeys[i]))
                    letdownReflexByKey[letdownKeys[i]] = Mathf.Clamp01(letdownVals[i]);
        }
        List<string> inflammationKeys = null;
        List<float> inflammationVals = null;
        if (Scribe.mode == LoadSaveMode.Saving && inflammationByKey != null && inflammationByKey.Count > 0)
        {
            inflammationKeys = inflammationByKey.Keys.ToList();
            inflammationVals = inflammationByKey.Values.ToList();
        }
        Scribe_Collections.Look(ref inflammationKeys, "EM.MilkSiteInflamK", LookMode.Value);
        Scribe_Collections.Look(ref inflammationVals, "EM.MilkSiteInflamV", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.LoadingVars && inflammationKeys != null && inflammationVals != null && inflammationKeys.Count == inflammationVals.Count)
        {
            inflammationByKey ??= new Dictionary<string, float>();
            inflammationByKey.Clear();
            for (int i = 0; i < inflammationKeys.Count; i++)
                if (!string.IsNullOrEmpty(inflammationKeys[i]))
                    inflammationByKey[inflammationKeys[i]] = Mathf.Max(0f, inflammationVals[i]);
        }
        Scribe_Values.Look(ref milkingLStimulusAccumulatedThisDay, "EM.MilkingLStimulusAccumulatedThisDay", 0f);
        Scribe_Values.Look(ref lastMilkingLStimulusDayTick, "EM.LastMilkingLStimulusDayTick", -1);
        Scribe_Values.Look(ref lactationTicksAccumulated, "EM.LactationTicksAccumulated", 0);
        Scribe_Values.Look(ref permanentBreastGainMilestonesDone, "EM.PermanentBreastGainMilestonesDone", 0);
    }
}
