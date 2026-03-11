using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Helpers;
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
                int raw = GetStageIndex(this.Severity, (comp?.Fullness ?? 0f) >= Mathf.Max(0.01f, comp?.maxFullness ?? 1f));
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
        // 水池模型：合并进来的 Severity 也需同步�?L，否则仅吃药�?ingestion postfix 会令 L 增加，调�?基因/其他来源合并会导�?Severity 高但 L 低、产奶流速异�?
        if (other.Severity > 0f && comps != null)
        {
            foreach (var c in comps)
                if (c is HediffComp_EqualMilkingLactating comp)
                {
                    comp.AddFromDrug(other.Severity);
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
    /// <summary>挤奶/吸奶后按被扣量的池侧添加喷乳反射刺激（仅被操作的该侧 R 升高）</summary>
    public void OnGatheredLetdownByKeys(IEnumerable<string> drainedKeys)
    {
        if (drainedKeys == null) return;
        var comp = LactatingComp;
        foreach (string key in drainedKeys)
            comp?.AddLetdownReflexStimulus(key);
        if (MilkCumSettings.enableInflammationModel)
            comp?.AddMilkingLStimulus();
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
            int raw = GetStageIndex(this.Severity, (comp?.Fullness ?? 0f) >= Mathf.Max(0.01f, comp?.maxFullness ?? 1f));
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
public class HediffComp_EqualMilkingLactating : HediffComp_Lactating
{
    /// <summary>水池模型（L 驱动）：药物诱发的 L 分量；衰减用药物基准 B_T。</summary>
    private float lactationAmountFromDrug;
    /// <summary>水池模型：分娩诱发的 L 分量；衰减用分娩基准 B_T。</summary>
    private float lactationAmountFromBirth;
    /// <summary>当前泌乳量 L = 药物分量 + 分娩分量</summary>
    private float currentLactationAmount => lactationAmountFromDrug + lactationAmountFromBirth;
    /// <summary>四层模型：喷乳反�?R∈[0,1]，按「哪对乳房的哪一侧」分别存储（key �?breastFullness 一致，�?HumanBreast_L）。挤�?吸奶某侧仅提升该�?R</summary>
    private Dictionary<string, float> letdownReflexByKey;
    /// <summary>四层模型：炎症负�?I�?。每 60 tick 离散更新；I>I_crit 触发乳腺炎</summary>
    private float currentInflammation;
    /// <summary>挤奶 L 刺激：当日已累计量，每游戏日重置</summary>
    private float milkingLStimulusAccumulatedThisDay;
    private int lastMilkingLStimulusDayTick = -1;
    /// <summary>耐受动态：有效耐受 E∈[0,1]，dE/dt = μ·L − ν·E；用于 E_tol 计算</summary>
    private float effectiveToleranceE;
    /// <summary>上次判定时是否为永久泌乳，用于从非永久变为永久时 SetDirty 刷新阶段名</summary>
    private bool cachedWasPermanentLactation;
    /// <summary>本 tick 内 TryMergeWith 是否对本 comp 加过 raw（仅本次 ingestion 有效，不存档）。用于已泌乳再次吃药时区分「已合并→补差」与「未合并→加全量 deltaS」。</summary>
    internal bool MergedFromIngestionThisTick;

    public CompEquallyMilkable CompEquallyMilkable => this.Pawn.CompEquallyMilkable();
    public HediffWithComps_MilkCumLactating Parent => (HediffWithComps_MilkCumLactating)this.parent;

    /// <summary>剩余天数（游戏日）。按分量分别用药物/分娩 B_T；总 L 用加权 B_T_eff 估算。</summary>
    public float RemainingDays
    {
        get
        {
            float L = currentLactationAmount;
            if (L <= 0f) return 0f;
            float eff = GetEffectiveDrugFactor();
            if (eff <= 0f) return 0f;
            float bT = GetEffectiveBaseValueTForRemaining();
            float a = 1f / (bT * eff);
            float k = PoolModelConstants.NegativeFeedbackK;
            if (a <= 0f) return 0f;
            if (k <= 0f)
            {
                float D = a;
                return D <= 0f ? 0f : L / D;
            }
            float arg = 1f + k * L / a;
            if (arg <= 1f) return 0f;
            return (1f / k) * Mathf.Log(arg);
        }
    }
    /// <summary>剩余天数用加权 B_T：L_drug/B_T_drug + L_birth/B_T_birth 反推 B_T_eff = L_total / (L_drug/B_T_drug + L_birth/B_T_birth)。</summary>
    private float GetEffectiveBaseValueTForRemaining()
    {
        float Ld = lactationAmountFromDrug;
        float Lb = lactationAmountFromBirth;
        if (Ld <= 0f && Lb <= 0f) return MilkCumSettings.GetEffectiveBaseValueTForDecay();
        if (Lb <= 0f) return MilkCumSettings.GetEffectiveBaseValueTForDecay();
        if (Ld <= 0f) return MilkCumSettings.GetEffectiveBaseValueTForDecayBirth();
        float bTd = MilkCumSettings.GetEffectiveBaseValueTForDecay();
        float bTb = MilkCumSettings.GetEffectiveBaseValueTForDecayBirth();
        float denom = Ld / bTd + Lb / bTb;
        if (denom <= 0f) return bTd;
        return (Ld + Lb) / denom;
    }
    /// <summary>当前泌乳�?L（规格：基础�?= 总容量，归一�?1）</summary>
    public float CurrentLactationAmount => currentLactationAmount;

    /// <summary>是否为永久泌乳（基因或动物设置）：L 不衰减；括号阶段名与悬停「剩余时间」均据此显示「永久」</summary>
    public bool IsPermanentLactation =>
        Pawn.genes?.HasActiveGene(MilkCumDefOf.EM_Permanent_Lactation) == true
        || (MilkCumSettings.femaleAnimalAdultAlwaysLactating && Pawn.IsAdultFemaleAnimalOfColony());

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref lactationAmountFromDrug, "EM.PoolLactationFromDrug", 0f);
        Scribe_Values.Look(ref lactationAmountFromBirth, "EM.PoolLactationFromBirth", 0f);
        float legacyTotal = 0f;
        if (Scribe.mode == LoadSaveMode.Saving)
            legacyTotal = lactationAmountFromDrug + lactationAmountFromBirth;
        Scribe_Values.Look(ref legacyTotal, "PoolCurrentLactationAmount", 0f);
        if (Scribe.mode == LoadSaveMode.LoadingVars && lactationAmountFromDrug <= 0f && lactationAmountFromBirth <= 0f && legacyTotal > 0f)
        {
            lactationAmountFromDrug = legacyTotal;
            lactationAmountFromBirth = 0f;
        }
        List<string> letdownKeys = null;
        List<float> letdownVals = null;
        if (Scribe.mode == LoadSaveMode.Saving && letdownReflexByKey != null && letdownReflexByKey.Count > 0)
        {
            letdownKeys = letdownReflexByKey.Keys.ToList();
            letdownVals = letdownReflexByKey.Values.ToList();
        }
        Scribe_Collections.Look(ref letdownKeys, "EM.LetdownReflexKeys", LookMode.Value);
        Scribe_Collections.Look(ref letdownVals, "EM.LetdownReflexVals", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && letdownKeys != null && letdownVals != null && letdownKeys.Count == letdownVals.Count)
        {
            letdownReflexByKey ??= new Dictionary<string, float>();
            letdownReflexByKey.Clear();
            for (int i = 0; i < letdownKeys.Count; i++)
                if (!string.IsNullOrEmpty(letdownKeys[i]))
                    letdownReflexByKey[letdownKeys[i]] = Mathf.Clamp01(letdownVals[i]);
        }
        Scribe_Values.Look(ref currentInflammation, "EM.Inflammation", 0f);
        Scribe_Values.Look(ref milkingLStimulusAccumulatedThisDay, "EM.MilkingLStimulusAccumulatedThisDay", 0f);
        Scribe_Values.Look(ref lastMilkingLStimulusDayTick, "EM.LastMilkingLStimulusDayTick", -1);
        Scribe_Values.Look(ref effectiveToleranceE, "EM.EffectiveToleranceE", 0f);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (currentLactationAmount <= 0f && Parent.Severity > 0f)
            {
                float eff = GetEffectiveDrugFactor();
                lactationAmountFromDrug = Parent.Severity * GetBaseValueNormalized(Pawn) * eff;
                lactationAmountFromBirth = 0f;
            }
            currentInflammation = Mathf.Max(0f, currentInflammation);
            effectiveToleranceE = Mathf.Clamp01(effectiveToleranceE);
            if (effectiveToleranceE <= 0f && Pawn != null && MilkCumSettings.GetProlactinTolerance(Pawn) > 0f)
                effectiveToleranceE = Mathf.Clamp01(MilkCumSettings.GetProlactinTolerance(Pawn));
        }
    }

    /// <summary>耐受动态：dE/dt = μ·L �?ν·E。Δt 单位：游戏日</summary>
    public void UpdateToleranceDynamic(float L, float deltaTDays)
    {
        if (!MilkCumSettings.enableToleranceDynamic || deltaTDays <= 0f) return;
        float mu = Mathf.Max(0f, MilkCumSettings.toleranceDynamicMu);
        float nu = Mathf.Max(0.001f, MilkCumSettings.toleranceDynamicNu);
        float dE = (mu * L - nu * effectiveToleranceE) * deltaTDays;
        effectiveToleranceE = Mathf.Clamp01(effectiveToleranceE + dE);
    }

    /// <summary>耐受动态：当前 E，供 GetProlactinToleranceFactor 使用</summary>
    public float GetEffectiveToleranceE() => Mathf.Clamp01(effectiveToleranceE);

    /// <summary>四层模型：炎�?I 离散更新。dI/dt = α·P² + β·Injury + γ·BadHygiene �?ρ·I；Δt 单位：小时</summary>
    public void UpdateInflammation(float P, float deltaTHours)
    {
        if (!MilkCumSettings.enableInflammationModel || deltaTHours <= 0f) return;
        float alpha = Mathf.Max(0f, MilkCumSettings.inflammationAlpha);
        float beta = Mathf.Max(0f, MilkCumSettings.inflammationBeta);
        float gamma = Mathf.Max(0f, MilkCumSettings.inflammationGamma);
        float rho = Mathf.Max(0.001f, MilkCumSettings.inflammationRho);
        float injury = CompEquallyMilkable != null && CompEquallyMilkable.HasTorsoOrBreastInjury(Pawn) ? 1f : 0f;
        float badHygiene = Pawn != null ? DubsBadHygieneIntegration.GetHygieneRiskFactorForMastitis(Pawn) : 0f;
        float dI = (alpha * P * P + beta * injury + gamma * badHygiene - rho * currentInflammation) * deltaTHours;
        currentInflammation = Mathf.Max(0f, currentInflammation + dI);
    }

    /// <summary>四层模型：若 I>I_crit 且允许乳腺炎，则�?EM_Mastitis（与现有 MTB 判定并存）。启用质量时有效 I_crit �?MilkQuality 提高</summary>
    public void TryTriggerMastitisFromInflammation()
    {
        if (!MilkCumSettings.enableInflammationModel || !MilkCumSettings.allowMastitis) return;
        if (MilkCumDefOf.EM_Mastitis == null || Pawn == null || !Pawn.RaceProps.Humanlike || !Pawn.IsLactating()) return;
        float crit = Mathf.Max(0.01f, MilkCumSettings.inflammationCrit);
        if (MilkCumSettings.enableMilkQuality)
            crit *= 1f + MilkCumSettings.milkQualityProtectionFactor * GetMilkQuality();
        if (currentInflammation < crit) return;
        var existing = Pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_Mastitis);
        if (existing != null)
        {
            if (existing.Severity < 0.99f)
                existing.Severity = Mathf.Min(1f, existing.Severity + 0.1f);
        }
        else
            Pawn.health.AddHediff(MilkCumDefOf.EM_Mastitis, Pawn.GetBreastOrChestPart());
        currentInflammation = Mathf.Max(0f, currentInflammation - crit * 0.5f);
    }

    /// <summary>四层模型（阶�?.3）：MilkQuality = f(Hunger, I) �?[0,1]。饱食度高、炎症低则质量高；未启用质量时返�?1</summary>
    public float GetMilkQuality()
    {
        if (!MilkCumSettings.enableMilkQuality || Pawn == null) return 1f;
        float hunger = Pawn.needs?.food != null ? Mathf.Clamp01(Pawn.needs.food.CurLevel) : 1f;
        float w = Mathf.Clamp(MilkCumSettings.milkQualityInflammationWeight, 0f, 2f);
        float fromInflammation = Mathf.Clamp01(1f - w * currentInflammation);
        return Mathf.Clamp01(hunger * fromInflammation);
    }

    /// <summary>四层模型：挤奶/吸奶时 L 微幅刺激，单次与每日带上限；仅 enableInflammationModel 时生效。</summary>
    public void AddMilkingLStimulus()
    {
        float perEvent = Mathf.Clamp(MilkCumSettings.milkingLStimulusPerEvent, 0f, 1f);
        float capEvent = Mathf.Clamp(MilkCumSettings.milkingLStimulusCapPerEvent, 0f, 1f);
        float capDay = Mathf.Clamp(MilkCumSettings.milkingLStimulusCapPerDay, 0f, 1f);
        int now = Find.TickManager.TicksGame;
        if (lastMilkingLStimulusDayTick < 0 || now - lastMilkingLStimulusDayTick >= 60000)
        {
            milkingLStimulusAccumulatedThisDay = 0f;
            lastMilkingLStimulusDayTick = now;
        }
        float room = Mathf.Max(0f, capDay - milkingLStimulusAccumulatedThisDay);
        float toAdd = Mathf.Min(perEvent, capEvent, room);
        if (toAdd <= 0f) return;
        lactationAmountFromDrug += toAdd;
        milkingLStimulusAccumulatedThisDay += toAdd;
    }

    /// <summary>四层模型：当前喷乳反射 R∈[0,1]（用于总览显示）。未启用时返回 1；启用时为各侧 R 的算术平均，无数据时返回 0。</summary>
    public float GetLetdownReflex()
    {
        if (!MilkCumSettings.enableLetdownReflex) return 1f;
        var entries = Pawn?.GetBreastPoolEntries();
        if ((entries?.Count ?? 0) == 0) return 0f;
        float sumR = 0f;
        int n = 0;
        foreach (var e in entries)
        {
            sumR += GetLetdownReflexForSide(e.Key);
            n++;
        }
        return n > 0 ? Mathf.Clamp01(sumR / n) : 0f;
    }

    /// <summary>指定侧（poolKey_L / poolKey_R）的 R 值；无记录时返回 0（流速倍率里会�?min 合并）</summary>
    private float GetLetdownReflexRaw(string sideKey)
    {
        if (string.IsNullOrEmpty(sideKey) || letdownReflexByKey == null || !letdownReflexByKey.TryGetValue(sideKey, out float r))
            return 0f;
        return Mathf.Clamp01(r);
    }

    /// <summary>指定侧的 R，无记录时为 0（用于显�?平均）</summary>
    public float GetLetdownReflexForSide(string sideKey)
    {
        if (!MilkCumSettings.enableLetdownReflex) return 1f;
        return GetLetdownReflexRaw(sideKey);
    }

    /// <summary>进水流速的 R 倍率（按侧）：无刺激（该侧无记录）时=1；有刺激时倍率 = 1+R×(boost−1)，R 可衰减到 0 时倍率回 1。</summary>
    public float GetLetdownReflexFlowMultiplier(string sideKey)
    {
        if (!MilkCumSettings.enableLetdownReflex) return 1f;
        if (string.IsNullOrEmpty(sideKey) || letdownReflexByKey == null || !letdownReflexByKey.TryGetValue(sideKey, out float rVal))
            return 1f;
        float r = Mathf.Clamp01(rVal);
        float boost = Mathf.Clamp(MilkCumSettings.letdownReflexBoostMultiplier, 1f, 3f);
        return Mathf.Max(1f, 1f + r * (boost - 1f));
    }

    /// <summary>进水流速的 R 倍率（全侧平均，用于总产奶效率一行显示）</summary>
    public float GetLetdownReflexFlowMultiplier()
    {
        if (!MilkCumSettings.enableLetdownReflex) return 1f;
        var entries = Pawn?.GetBreastPoolEntries();
        if ((entries?.Count ?? 0) == 0) return 1f;
        float sum = 0f;
        foreach (var e in entries)
            sum += GetLetdownReflexFlowMultiplier(e.Key);
        return entries.Count > 0 ? sum / entries.Count : 1f;
    }

    /// <summary>四层模型：各侧 R 指数衰减，可衰减到 0；仅保留当前池中存在的 key，避免字典无限增长。</summary>
    public void DecayLetdownReflex(float deltaTMinutes)
    {
        if (!MilkCumSettings.enableLetdownReflex || deltaTMinutes <= 0f) return;
        if (letdownReflexByKey == null || letdownReflexByKey.Count == 0) return;
        float lambda = Mathf.Max(0.001f, MilkCumSettings.letdownReflexDecayLambda);
        var validKeys = Pawn?.GetBreastPoolEntries()?.Select(e => e.Key).ToHashSet();
        var keys = letdownReflexByKey.Keys.ToList();
        var toRemove = new List<string>();
        foreach (string key in keys)
        {
            if (validKeys != null && !validKeys.Contains(key))
            {
                toRemove.Add(key);
                continue;
            }
            float r = letdownReflexByKey[key] * Mathf.Exp(-lambda * deltaTMinutes);
            if (r < 1E-5f) r = 0f;
            letdownReflexByKey[key] = r;
        }
        foreach (string key in toRemove)
            letdownReflexByKey.Remove(key);
    }

    /// <summary>四层模型：挤�?吸奶刺激该侧，R += ΔR，Clamp �?1。仅被刺激的那一侧（如第一对的左乳）提升喷乳反射</summary>
    public void AddLetdownReflexStimulus(string sideKey)
    {
        if (!MilkCumSettings.enableLetdownReflex || string.IsNullOrEmpty(sideKey)) return;
        letdownReflexByKey ??= new Dictionary<string, float>();
        float deltaR = Mathf.Clamp(MilkCumSettings.letdownReflexStimulusDeltaR, 0f, 1f);
        float r = GetLetdownReflexRaw(sideKey);
        letdownReflexByKey[sideKey] = Mathf.Min(1f, r + deltaR);
    }

    /// <summary>归一化基础�?= 总容量（规格：基础�?= 总容量），当前实现为 1</summary>
    public static float GetBaseValueNormalized(Pawn pawn = null) => 1f;

    /// <summary>有效药效系数：统一使用 MilkCumSettings.GetProlactinToleranceFactor</summary>
    public float GetEffectiveDrugFactor()
    {
        return MilkCumSettings.GetProlactinToleranceFactor(Pawn);
    }

    /// <summary>流速因子显示用：整数显示为 "1"，否则保�?1�? 位小数</summary>
    private static string FormatFlowFactor(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return "0";
        if (Mathf.Approximately(v, Mathf.Floor(v))) return ((int)v).ToString();
        return v <= 10f ? v.ToString("F2").TrimEnd('0').TrimEnd('.') : v.ToString("F1");
    }

    /// <summary>每日衰减 D(L,E) = 1/(B_T×E) + k×L + η·I（启用炎症时）。B_T 由参数传入（药物/分娩分别用各自基准）。</summary>
    public float GetDailyLactationDecayWithBT(float lactationAmount, float bT)
    {
        float eff = GetEffectiveDrugFactor();
        if (eff <= 0f) return 0f;
        float D = 1f / (bT * eff) + PoolModelConstants.NegativeFeedbackK * lactationAmount;
        if (MilkCumSettings.enableInflammationModel)
            D += MilkCumSettings.lactationDecayInflammationEta * Mathf.Max(0f, currentInflammation);
        return D;
    }

    /// <summary>每日衰减（药物分量用药物 B_T）；用于显示总衰减量。</summary>
    public float GetDailyLactationDecay(float lactationAmount)
    {
        return GetDailyLactationDecayWithBT(lactationAmount, MilkCumSettings.GetEffectiveBaseValueTForDecay());
    }

    /// <summary>当前 L 下的每日衰减（两分量之和，用于显示）</summary>
    public float GetDailyLactationDecay() => GetDailyLactationDecayWithBT(lactationAmountFromDrug, MilkCumSettings.GetEffectiveBaseValueTForDecay()) + GetDailyLactationDecayWithBT(lactationAmountFromBirth, MilkCumSettings.GetEffectiveBaseValueTForDecayBirth());

    /// <summary>吃药时进水：ΔL_drug = Δs × C_dose。</summary>
    public void AddFromDrug(float deltaSeverity)
    {
        float remainingBefore = RemainingDays;
        float deltaL = deltaSeverity * PoolModelConstants.DoseToLFactor;
        lactationAmountFromDrug += deltaL;
        float remainingAfter = RemainingDays;
        if (MilkCumSettings.lactationDrugIntakeLog)
        {
            float eTol = MilkCumSettings.GetProlactinToleranceFactor(Pawn);
            float raceMult = MilkCumSettings.GetRaceDrugDeltaSMultiplier(Pawn);
            float cDose = PoolModelConstants.DoseToLFactor;
            float rawInferred = (eTol * raceMult > 1E-6f) ? (deltaSeverity / (eTol * raceMult)) : 0f;
            string note = deltaSeverity < 0f ? " [已泌乳补差: TryMergeWith 已加 raw，此处补 deltaS−raw]" : "";
            Verse.Log.Message($"[MilkCum] 吃药进水: Pawn={Pawn?.LabelShort} | raw(反推)={rawInferred:F3} Δs={deltaSeverity:F3} E_tol={eTol:F3} 种族倍率={raceMult:F3} C_dose={cDose:F2} | 进水ΔL={deltaL:F3} | 剩余时间 {remainingBefore:F1}→{remainingAfter:F1} 天 (+{remainingAfter - remainingBefore:F1}){note}");
        }
    }

    /// <summary>分娩时累加：L_birth += 基础值（不乘有效药效系数）</summary>
    public void AddFromBirth()
    {
        lactationAmountFromBirth += GetBaseValueNormalized(Pawn);
    }

    /// <summary>一次吸奶或挤奶增加一天泌乳时间：将 L 增加「一日衰减量×days」，等效延长剩余天数。永久泌乳不生效。</summary>
    public void AddRemainingDays(float days)
    {
        if (days <= 0f || IsPermanentLactation) return;
        float dailyDecay = GetDailyLactationDecay();
        if (dailyDecay <= 0f) return;
        float LToAdd = dailyDecay * days;
        lactationAmountFromDrug += LToAdd;
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (!Pawn.IsMilkable())
        {
            base.CompPostTick(ref severityAdjustment);
            return;
        }
        // 水池模型（L 驱动）：�?200 tick 更新。永久泌�?动物：不衰减 L，仅 L�? 时设为基础值，避免池满时流�?0 仍衰减导�?L�?→重�?的波动。见 记忆�?decisions/ADR-001-进水与衰减周期�?
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
                float step = 200f / 60000f;
                float bTdrug = MilkCumSettings.GetEffectiveBaseValueTForDecay();
                float bTbirth = MilkCumSettings.GetEffectiveBaseValueTForDecayBirth();
                float Ddrug = GetDailyLactationDecayWithBT(lactationAmountFromDrug, bTdrug);
                float Dbirth = GetDailyLactationDecayWithBT(lactationAmountFromBirth, bTbirth);
                float decayDrug = Ddrug * step;
                float decayBirth = Dbirth * step;
                lactationAmountFromDrug = Mathf.Max(0f, lactationAmountFromDrug - decayDrug);
                lactationAmountFromBirth = Mathf.Max(0f, lactationAmountFromBirth - decayBirth);
                if (currentLactationAmount < PoolModelConstants.LactationEndEpsilon)
                {
                    lactationAmountFromDrug = 0f;
                    lactationAmountFromBirth = 0f;
                }
                if (currentLactationAmount <= 0f)
                {
                    ResetAndRemoveLactating();
                    return;
                }
            }
        }
        this.Charge = CompEquallyMilkable != null ? CompEquallyMilkable.Fullness : 0f;
    }

    /// <summary>泌乳结束：清空双池、移除 Lactating hediff</summary>
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
    /// <summary>当前产奶流速（池单�?天）= 左池流�?+ 右池流速；�?CompEquallyMilkable 一致。启用四层时：驱动可�?D_eff=L·H(L)，再�?PressureFactor(P)×R（见 Docs 第十二节）</summary>
    internal float GetFlowPerDay()
    {
        var b = GetFlowPerDayBreakdown();
        return b.TotalFlow;
    }

    /// <summary>产奶流速拆解：总流速与各乘数因子，用于悬停显示「产奶效率：1(300%)/天」及「驱动�? 饥饿×1 状态�? …」</summary>
    internal FlowBreakdown GetFlowPerDayBreakdown()
    {
        var r = new FlowBreakdown();
        if (CompEquallyMilkable == null) return r;
        float maxF = Mathf.Max(0.001f, CompEquallyMilkable.maxFullness);
        float fullness = CompEquallyMilkable.Fullness;
        float hungerFactor = PawnUtility.BodyResourceGrowthSpeed(Pawn);
        if (currentLactationAmount <= 0f || hungerFactor <= 0f) return r;
        r.Drive = MilkCumSettings.GetEffectiveDrive(currentLactationAmount);
        r.Hunger = hungerFactor;
        r.Conditions = Pawn.GetMilkFlowMultiplierFromConditions();
        r.Genes = Pawn.GetMilkFlowMultiplierFromGenes();
        r.Setting = MilkCumSettings.defaultFlowMultiplierForHumanlike;
        float flowLeftMult = Pawn.GetMilkFlowMultiplierFromRJW_Left();
        float flowRightMult = Pawn.GetMilkFlowMultiplierFromRJW_Right();
        r.RjwSum = flowLeftMult + flowRightMult;
        float flow = r.Drive * r.Hunger * r.Conditions * r.Genes * r.Setting * r.RjwSum;
        var entries = Pawn.GetBreastPoolEntries();
        var milkComp = CompEquallyMilkable;
        // 压力、喷乳反射、状态（如乳腺炎）均按「哪对乳房的哪一侧」分别计�?
        if (entries.Count > 0 && milkComp != null)
        {
            float baseWithoutConditions = r.Drive * r.Hunger * r.Genes * r.Setting;
            float flowWithAllFactors = 0f;
            float sumWeightedLetdown = 0f;
            float sumWeightedPressure = 0f;
            float sumWeightedConditions = 0f;
            foreach (var e in entries)
            {
                float conditionsE = Pawn.GetConditionsForSide(e.Key);
                float stretch = e.Capacity * PoolModelConstants.StretchCapFactor;
                float fullE = milkComp.GetFullnessForKey(e.Key);
                float pressureE = MilkCumSettings.enablePressureFactor
                    ? MilkCumSettings.GetPressureFactor(fullE / Mathf.Max(0.001f, stretch))
                    : (fullE >= stretch ? 0f : 1f);
                float letdownE = MilkCumSettings.enableLetdownReflex ? GetLetdownReflexFlowMultiplier(e.Key) : 1f;
                float contrib = conditionsE * e.FlowMultiplier * pressureE * letdownE;
                flowWithAllFactors += contrib;
                sumWeightedLetdown += e.FlowMultiplier * letdownE;
                sumWeightedPressure += e.FlowMultiplier * pressureE * letdownE;
                sumWeightedConditions += e.FlowMultiplier * conditionsE * pressureE * letdownE;
            }
            r.Pressure = sumWeightedLetdown >= 1E-5f ? sumWeightedPressure / sumWeightedLetdown : (MilkCumSettings.enablePressureFactor ? MilkCumSettings.GetPressureFactor(fullness / maxF) : (fullness >= maxF ? 0f : 1f));
            r.Letdown = r.RjwSum >= 1E-5f ? sumWeightedLetdown / r.RjwSum : (MilkCumSettings.enableLetdownReflex ? GetLetdownReflexFlowMultiplier() : 1f);
            r.Conditions = flowWithAllFactors >= 1E-5f ? sumWeightedConditions / flowWithAllFactors : r.Conditions;
            if (flowWithAllFactors >= 1E-5f)
            {
                flow = baseWithoutConditions * flowWithAllFactors;
                // 与 UpdateMilkPools 的 basePerDay 一致：实际进水用了全局状态倍率，扣营养也应用同一倍率，保证 营养- ≈ 乳池+
                flow *= Pawn.GetMilkFlowMultiplierFromConditions();
            }
            else
            {
                float basePerDayFallback = r.Drive * r.Hunger * r.Conditions * r.Genes * r.Setting;
                flow = basePerDayFallback * r.RjwSum;
                if (!MilkCumSettings.enablePressureFactor && fullness >= maxF) flow = 0f;
                else if (MilkCumSettings.enablePressureFactor) flow *= r.Pressure;
                if (MilkCumSettings.enableLetdownReflex) flow *= r.Letdown;
            }
        }
        else
        {
            float basePerDay = r.Drive * r.Hunger * r.Conditions * r.Genes * r.Setting;
            if (MilkCumSettings.enablePressureFactor)
            {
                r.Pressure = MilkCumSettings.GetPressureFactor(fullness / maxF);
                flow = basePerDay * r.RjwSum * r.Pressure;
            }
            else
            {
                r.Pressure = fullness >= maxF ? 0f : 1f;
                flow = fullness >= maxF ? 0f : basePerDay * r.RjwSum;
            }
            r.Letdown = MilkCumSettings.enableLetdownReflex ? GetLetdownReflexFlowMultiplier() : 1f;
            if (MilkCumSettings.enableLetdownReflex) flow *= r.Letdown;
        }
        r.TotalFlow = flow;
        return r;
    }

    /// <summary>产奶流速拆解：总流速与各乘数因子（�?GetFlowPerDay 一致），用�?UI 显示</summary>
    public struct FlowBreakdown
    {
        public float TotalFlow;
        public float Drive;
        public float Hunger;
        public float Conditions;
        public float Genes;
        public float Setting;
        public float RjwSum;
        public float Pressure;
        public float Letdown;
    }

    /// <summary>健康页乳房行悬停：构建单侧（左或右）产奶效率的因子拆解行；状态、压力、喷乳反射按该侧显示（null 时用总览 b 的值）</summary>
    public static string BuildBreastEfficiencyFactorLine(FlowBreakdown b, float sideMult, bool leftSide, float? letdownForSide = null, float? pressureForSide = null, float? conditionsForSide = null)
    {
        float letdown = letdownForSide ?? b.Letdown;
        float pressure = pressureForSide ?? b.Pressure;
        float conditions = conditionsForSide ?? b.Conditions;
        var parts = new List<string>();
        parts.Add("EM.MilkFlowFactorItem".Translate("EM.MilkFlowDrive".Translate(), FormatFlowFactor(b.Drive)));
        parts.Add("EM.MilkFlowFactorItem".Translate("EM.MilkFlowHunger".Translate(), FormatFlowFactor(b.Hunger)));
        parts.Add("EM.MilkFlowFactorItem".Translate("EM.MilkFlowConditions".Translate(), FormatFlowFactor(conditions)));
        parts.Add("EM.MilkFlowFactorItem".Translate("EM.MilkFlowGenes".Translate(), FormatFlowFactor(b.Genes)));
        parts.Add("EM.MilkFlowFactorItem".Translate("EM.MilkFlowSetting".Translate(), FormatFlowFactor(b.Setting)));
        parts.Add("EM.MilkFlowFactorItem".Translate(leftSide ? "EM.PoolLeftBreast".Translate() : "EM.PoolRightBreast".Translate(), FormatFlowFactor(sideMult)));
        parts.Add("EM.MilkFlowFactorItem".Translate("EM.MilkFlowPressure".Translate(), FormatFlowFactor(pressure)));
        parts.Add("EM.MilkFlowFactorItem".Translate("EM.MilkFlowLetdown".Translate(), FormatFlowFactor(letdown)));
        return string.Join(" ", parts);
    }

    /// <summary>健康页乳房悬�?DevMode：返回「生产机制」顺序的因子行（乳房体积、基因、设置、状态、驱动、饥饿、压力、喷乳反射）</summary>
    public static System.Collections.Generic.List<string> BuildBreastEfficiencyFactorLinesForDevMode(FlowBreakdown b, float sideMult, bool leftSide, float? letdownForSide = null, float? pressureForSide = null, float? conditionsForSide = null)
    {
        float letdown = letdownForSide ?? b.Letdown;
        float pressure = pressureForSide ?? b.Pressure;
        float conditions = conditionsForSide ?? b.Conditions;
        var list = new System.Collections.Generic.List<string>();
        list.Add("EM.MilkFlowFactorLine".Translate("EM.MilkFlowBreastVolume".Translate(), FormatFlowFactor(sideMult)));
        list.Add("EM.MilkFlowFactorLine".Translate("EM.MilkFlowGenes".Translate(), FormatFlowFactor(b.Genes)));
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
            float maxF = Mathf.Max(0.01f, CompEquallyMilkable?.maxFullness ?? 1f);
            float totalMilk = CompEquallyMilkable != null ? CompEquallyMilkable.Fullness : Charge;
            bool isFull = totalMilk >= maxF;
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

            lines.Add("");
            // 2. 储量
            lines.Add("EM.PoolSectionStorage".Translate());
            if (CompEquallyMilkable != null)
            {
                float baseTotal = Mathf.Max(0.01f, Pawn.GetLeftBreastCapacityFactor() + Pawn.GetRightBreastCapacityFactor());
                string totalPercentStr = baseTotal >= 0.001f ? (totalMilk / baseTotal).ToStringPercent() : "0%";
                lines.Add("  " + "EM.PoolBreastTotalMilkLine".Translate(
                    totalMilk.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    baseTotal.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    totalPercentStr));
                lines.Add("  " + "EM.PoolBreastStretchCapLine".Translate(maxF.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
            };

            // 3. 产奶流速（池/天）：与 UpdateMilkPools 一致，满池时仍按压力曲线有微量进水，故始终显示 GetFlowPerDayBreakdown 的总流速
            lines.Add("EM.PoolSectionFlow".Translate());
            if (growthSpeed > 0f)
            {
                var b = GetFlowPerDayBreakdown();
                lines.Add("  " + "EM.PoolBreastTotalFlowLine".Translate(b.TotalFlow.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                if (isFull)
                {
                    if (isShrinking)
                        lines.Add("  " + "EM.ReabsorbedNutritionPerDay".Translate(reabsorbed.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                    if (MilkCumSettings.enablePressureFactor && b.TotalFlow >= 0.001f)
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
    /// <summary>括号内保留「天数 + 满度%」，便于一眼看到剩余时间与池满度；悬停展开见下方详情。</summary>
    public override string CompLabelInBracketsExtra
    {
        get
        {
            float maxF = Mathf.Max(0.01f, CompEquallyMilkable?.maxFullness ?? 1f);
            float fullness = CompEquallyMilkable != null ? CompEquallyMilkable.Fullness : Charge;
            string head = IsPermanentLactation ? "" : ("(" + "EM.PoolDaysPrefix".Translate() + RemainingDays.ToString("F1") + "EM.PoolDaysSuffix".Translate() + ") ");
            return base.CompLabelInBracketsExtra + head + Lang.MilkFullness + ": " + (fullness / maxF).ToStringPercent();
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
            stringBuilder.AppendLine("dailyLactationDecay(D): " + GetDailyLactationDecay().ToString("F3"));
            stringBuilder.AppendLine("effectiveDrugFactor(E): " + GetEffectiveDrugFactor().ToString("F3"));
            if (CompEquallyMilkable != null)
            {
                stringBuilder.AppendLine("pool L: " + CompEquallyMilkable.LeftFullness.ToString("F3") + " R: " + CompEquallyMilkable.RightFullness.ToString("F3"));
                stringBuilder.AppendLine("overflowAccumulator: " + CompEquallyMilkable.OverflowAccumulator.ToString("F3"));
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
    /// <summary>吸奶/消费后：仅将 Charge 同步为当前池总满度，并触�?OnGathered（不按比例缩放池，因已由 DrainForConsume 按对按侧扣过）</summary>
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
public class HediffCompProperties_EqualMilkingLactating : HediffCompProperties_Chargeable
{
    public HediffCompProperties_EqualMilkingLactating()
    {
        compClass = typeof(HediffComp_EqualMilkingLactating);
    }
}
