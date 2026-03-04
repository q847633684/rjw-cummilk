using MilkCum.Core;
using MilkCum.Milk.Comps;
using MilkCum.Milk.Helpers;
using RimWorld;
using Verse;
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using rjw;
public class HediffWithComps_EqualMilkingLactating : HediffWithComps
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
            var comp = pawn.CompEquallyMilkable();
            return GetStageIndex(this.Severity, (comp?.Fullness ?? 0f) >= Mathf.Max(0.01f, comp?.maxFullness ?? 1f));
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
        // 水池模型：合并进来的 Severity 也需同步进 L，否则仅吃药走 ingestion postfix 会令 L 增加，调试/基因/其他来源合并会导致 Severity 高但 L 低、产奶流速异常
        if (other.Severity > 0f && comps != null)
        {
            foreach (var c in comps)
                if (c is HediffComp_EqualMilkingLactating comp) { comp.AddFromDrug(other.Severity); break; }
        }
        return true;
    }
    public void SetDirty()
    {
        isDirty = true;
    }
    public void OnGathered()
    {
        if (this.Severity < 1f)
            this.Severity = Mathf.Min(1f, this.Severity + 0.5f); // 挤奶时最多推到 1，不直接变永久；永久=成瘾且满足需求不衰减
        var comp = comps?.OfType<HediffComp_EqualMilkingLactating>().FirstOrDefault();
        comp?.AddMilkingLStimulus();
    }
    public void OnGathered(float fullness)
    {
        if (this.Severity < 1f && this.Severity + fullness >= 1f)
            this.Severity = 1f;
        else
            this.Severity += fullness;
        var comp = comps?.OfType<HediffComp_EqualMilkingLactating>().FirstOrDefault();
        comp?.AddMilkingLStimulus();
    }
    /// <summary>挤奶/吸奶后按被扣量的池侧添加喷乳反射刺激（仅被操作的该侧 R 升高）。</summary>
    public void OnGatheredLetdownByKeys(IEnumerable<string> drainedKeys)
    {
        if (drainedKeys == null) return;
        var comp = comps?.OfType<HediffComp_EqualMilkingLactating>().FirstOrDefault();
        foreach (string key in drainedKeys)
            comp?.AddLetdownReflexStimulus(key);
    }
    /// <summary>设计原则 5：stages 由 XML 移除，此处 C# 动态生成；capMods 在 GenStage 内设置（当前 Clear 由 EM_LactatingGain 等独立 Hediff 提供能力），避免与 Def 静态阶段冲突。</summary>
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
    /// <summary>Stage 名（括号内）：仅当真正不衰减时显示「永久」，否则留空；具体「天数：X」在 CompLabelInBracketsExtra 动态显示。</summary>
    private void GenStage(ref HediffStage stage, float severity, bool isFull, bool isPermanentLactation)
    {
        if (stage == null) { stage = new HediffStage(); }
        int severityInt = Mathf.FloorToInt(severity);
        if (severityInt >= 1) { stage.label = isPermanentLactation ? Lang.Permanent : ""; }
        else { stage.label = ""; }
        // 额外饥饿/能量改为由 ExtraNutritionPerDay/GetFlowPerDay 与 Need_Food/Need 补丁 1:1 施加，此处不再用 offset 增加饥饿
        stage.hungerRateFactorOffset = 0f;
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
        if (this.hediffStages == null || this.hediffStages.Length == 0 || this.hediffStages[0] == null)
            GenStages();
        if (!pawn.IsMilkable() && pawn.RaceProps.Humanlike)
        {
            if (this.Severity > 1f) { this.Severity = 0.9999f; }
            return vanillaStage;
        }
        return hediffStages[CurStageIndex];
    }
    public override bool Visible => pawn.IsMilkable() || pawn.RaceProps.Humanlike; //Milkable or breastfeedable in vanilla.
}
public class HediffComp_EqualMilkingLactating : HediffComp_Lactating
{
    /// <summary>水池模型（L 驱动）：当前泌乳量 L。唯一状态量；每日衰减 = 1/(B_T×E)+k×L；泌乳结束 L≤0。</summary>
    private float currentLactationAmount;
    /// <summary>四层模型：喷乳反射 R∈[0,1]，按「哪对乳房的哪一侧」分别存储（key 与 breastFullness 一致，如 HumanBreast_L）。挤奶/吸奶某侧仅提升该侧 R。</summary>
    private Dictionary<string, float> letdownReflexByKey;
    /// <summary>四层模型：炎症负荷 I≥0。每 30 tick 离散更新；I>I_crit 触发乳腺炎。</summary>
    private float currentInflammation;
    /// <summary>挤奶 L 刺激：当日已累计量，每游戏日重置。</summary>
    private float milkingLStimulusAccumulatedThisDay;
    private int lastMilkingLStimulusDayTick = -1;
    /// <summary>耐受动态：有效耐受 E∈[0,1]，dE/dt = μ·L − ν·E；用于 E_tol 计算。</summary>
    private float effectiveToleranceE;
    /// <summary>上次判定时是否为永久泌乳，用于从非永久变为永久时 SetDirty 刷新阶段名。</summary>
    private bool cachedWasPermanentLactation;

    public CompEquallyMilkable CompEquallyMilkable => this.Pawn.CompEquallyMilkable();
    public HediffWithComps_EqualMilkingLactating Parent => (HediffWithComps_EqualMilkingLactating)this.parent;

    /// <summary>剩余天数（游戏日）。动力学 dL/dt = -(a+k×L)，精确解 T = (1/k)×ln(1 + k×L/a)；a=1/(B_T×E)。k→0 或 a≤0 时退化为 L/D(L)。</summary>
    public float RemainingDays
    {
        get
        {
            float L = currentLactationAmount;
            if (L <= 0f) return 0f;
            float eff = GetEffectiveDrugFactor();
            if (eff <= 0f) return 0f;
            float bT = EqualMilkingSettings.GetEffectiveBaseValueTForDecay();
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
    /// <summary>当前泌乳量 L（规格：基础值 = 总容量，归一化 1）。</summary>
    public float CurrentLactationAmount => currentLactationAmount;

    /// <summary>是否为永久泌乳（基因或动物设置）：L 不衰减；括号阶段名与悬停「剩余时间」均据此显示「永久」。</summary>
    public bool IsPermanentLactation =>
        Pawn.genes?.HasActiveGene(EMDefOf.EM_Permanent_Lactation) == true
        || (EqualMilkingSettings.femaleAnimalAdultAlwaysLactating && Pawn.IsAdultFemaleAnimalOfColony());

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref currentLactationAmount, "PoolCurrentLactationAmount", 0f);
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
                currentLactationAmount = Parent.Severity * GetBaseValueNormalized(Pawn) * eff;
            }
            currentInflammation = Mathf.Max(0f, currentInflammation);
            effectiveToleranceE = Mathf.Clamp01(effectiveToleranceE);
            if (effectiveToleranceE <= 0f && Pawn != null && EqualMilkingSettings.GetProlactinTolerance(Pawn) > 0f)
                effectiveToleranceE = Mathf.Clamp01(EqualMilkingSettings.GetProlactinTolerance(Pawn));
        }
    }

    /// <summary>耐受动态：dE/dt = μ·L − ν·E。Δt 单位：游戏日。</summary>
    public void UpdateToleranceDynamic(float L, float deltaTDays)
    {
        if (!EqualMilkingSettings.enableToleranceDynamic || deltaTDays <= 0f) return;
        float mu = Mathf.Max(0f, EqualMilkingSettings.toleranceDynamicMu);
        float nu = Mathf.Max(0.001f, EqualMilkingSettings.toleranceDynamicNu);
        float dE = (mu * L - nu * effectiveToleranceE) * deltaTDays;
        effectiveToleranceE = Mathf.Clamp01(effectiveToleranceE + dE);
    }

    /// <summary>耐受动态：当前 E，供 GetProlactinToleranceFactor 使用。</summary>
    public float GetEffectiveToleranceE() => Mathf.Clamp01(effectiveToleranceE);

    /// <summary>四层模型：炎症 I 离散更新。dI/dt = α·P² + β·Injury + γ·BadHygiene − ρ·I；Δt 单位：小时。</summary>
    public void UpdateInflammation(float P, float deltaTHours)
    {
        if (!EqualMilkingSettings.enableInflammationModel || deltaTHours <= 0f) return;
        float alpha = Mathf.Max(0f, EqualMilkingSettings.inflammationAlpha);
        float beta = Mathf.Max(0f, EqualMilkingSettings.inflammationBeta);
        float gamma = Mathf.Max(0f, EqualMilkingSettings.inflammationGamma);
        float rho = Mathf.Max(0.001f, EqualMilkingSettings.inflammationRho);
        float injury = CompEquallyMilkable != null && CompEquallyMilkable.HasTorsoOrBreastInjury(Pawn) ? 1f : 0f;
        float badHygiene = Pawn != null ? DubsBadHygieneIntegration.GetHygieneRiskFactorForMastitis(Pawn) : 0f;
        float dI = (alpha * P * P + beta * injury + gamma * badHygiene - rho * currentInflammation) * deltaTHours;
        currentInflammation = Mathf.Max(0f, currentInflammation + dI);
    }

    /// <summary>四层模型：若 I>I_crit 且允许乳腺炎，则挂 EM_Mastitis（与现有 MTB 判定并存）。启用质量时有效 I_crit 随 MilkQuality 提高。</summary>
    public void TryTriggerMastitisFromInflammation()
    {
        if (!EqualMilkingSettings.enableInflammationModel || !EqualMilkingSettings.allowMastitis) return;
        if (EMDefOf.EM_Mastitis == null || Pawn == null || !Pawn.RaceProps.Humanlike || !Pawn.IsLactating()) return;
        float crit = Mathf.Max(0.01f, EqualMilkingSettings.inflammationCrit);
        if (EqualMilkingSettings.enableMilkQuality)
            crit *= 1f + EqualMilkingSettings.milkQualityProtectionFactor * GetMilkQuality();
        if (currentInflammation < crit) return;
        var existing = Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_Mastitis);
        if (existing != null)
        {
            if (existing.Severity < 0.99f)
                existing.Severity = Mathf.Min(1f, existing.Severity + 0.1f);
        }
        else
            Pawn.health.AddHediff(EMDefOf.EM_Mastitis, Pawn.GetBreastOrChestPart());
        currentInflammation = Mathf.Max(0f, currentInflammation - crit * 0.5f);
    }

    /// <summary>四层模型（阶段3.3）：MilkQuality = f(Hunger, I) ∈ [0,1]。饱食度高、炎症低则质量高；未启用质量时返回 1。</summary>
    public float GetMilkQuality()
    {
        if (!EqualMilkingSettings.enableMilkQuality || Pawn == null) return 1f;
        float hunger = Pawn.needs?.food != null ? Mathf.Clamp01(Pawn.needs.food.CurLevel) : 1f;
        float w = Mathf.Clamp(EqualMilkingSettings.milkQualityInflammationWeight, 0f, 2f);
        float fromInflammation = Mathf.Clamp01(1f - w * currentInflammation);
        return Mathf.Clamp01(hunger * fromInflammation);
    }

    /// <summary>四层模型：挤奶/吸奶时 L 微幅刺激，单次与每日带上限。</summary>
    public void AddMilkingLStimulus()
    {
        if (!EqualMilkingSettings.enableInflammationModel) return;
        float perEvent = Mathf.Clamp(EqualMilkingSettings.milkingLStimulusPerEvent, 0f, 1f);
        float capEvent = Mathf.Clamp(EqualMilkingSettings.milkingLStimulusCapPerEvent, 0f, 1f);
        float capDay = Mathf.Clamp(EqualMilkingSettings.milkingLStimulusCapPerDay, 0f, 1f);
        int now = Find.TickManager.TicksGame;
        if (lastMilkingLStimulusDayTick < 0 || now - lastMilkingLStimulusDayTick >= 60000)
        {
            milkingLStimulusAccumulatedThisDay = 0f;
            lastMilkingLStimulusDayTick = now;
        }
        float room = Mathf.Max(0f, capDay - milkingLStimulusAccumulatedThisDay);
        float toAdd = Mathf.Min(perEvent, capEvent, room);
        if (toAdd <= 0f) return;
        currentLactationAmount += toAdd;
        milkingLStimulusAccumulatedThisDay += toAdd;
    }

    /// <summary>四层模型：当前喷乳反射 R∈[0,1]（用于总览显示）。未启用时返回 1；启用时为各侧 R 的加权平均，无数据时不低于 letdownReflexMin。</summary>
    public float GetLetdownReflex()
    {
        if (!EqualMilkingSettings.enableLetdownReflex) return 1f;
        var entries = Pawn?.GetBreastPoolEntries();
        if (entries == null || entries.Count == 0) return Mathf.Clamp01(EqualMilkingSettings.letdownReflexMin);
        float sumR = 0f;
        int n = 0;
        foreach (var e in entries)
        {
            float r = GetLetdownReflexForSide(e.Key);
            float minR = Mathf.Clamp01(EqualMilkingSettings.letdownReflexMin);
            sumR += Mathf.Max(minR, r);
            n++;
        }
        return n > 0 ? Mathf.Clamp01(sumR / n) : Mathf.Clamp01(EqualMilkingSettings.letdownReflexMin);
    }

    /// <summary>指定侧（poolKey_L / poolKey_R）的 R 值；无记录时返回 0（流速倍率里会与 min 合并）。</summary>
    private float GetLetdownReflexRaw(string sideKey)
    {
        if (string.IsNullOrEmpty(sideKey) || letdownReflexByKey == null || !letdownReflexByKey.TryGetValue(sideKey, out float r))
            return 0f;
        return Mathf.Clamp01(r);
    }

    /// <summary>指定侧的 R，无记录时为 0（用于显示/平均）。</summary>
    public float GetLetdownReflexForSide(string sideKey)
    {
        if (!EqualMilkingSettings.enableLetdownReflex) return 1f;
        return GetLetdownReflexRaw(sideKey);
    }

    /// <summary>进水流速的 R 倍率（按侧）：加成模式时 = 1+R×(boost-1)；否则 = R（无记录时用 min）。用于该侧进水与 UI 因子。</summary>
    public float GetLetdownReflexFlowMultiplier(string sideKey)
    {
        if (!EqualMilkingSettings.enableLetdownReflex) return 1f;
        float minR = Mathf.Clamp01(EqualMilkingSettings.letdownReflexMin);
        float r = GetLetdownReflexRaw(sideKey);
        r = Mathf.Max(minR, Mathf.Clamp01(r));
        float boost = Mathf.Clamp(EqualMilkingSettings.letdownReflexBoostMultiplier, 1f, 3f);
        if (boost > 1f)
            return Mathf.Max(1f, 1f + r * (boost - 1f));
        return r;
    }

    /// <summary>进水流速的 R 倍率（全侧平均，用于总产奶效率一行显示）。</summary>
    public float GetLetdownReflexFlowMultiplier()
    {
        if (!EqualMilkingSettings.enableLetdownReflex) return 1f;
        var entries = Pawn?.GetBreastPoolEntries();
        if (entries == null || entries.Count == 0) return 1f;
        float sum = 0f;
        foreach (var e in entries)
            sum += GetLetdownReflexFlowMultiplier(e.Key);
        return entries.Count > 0 ? sum / entries.Count : 1f;
    }

    /// <summary>四层模型：各侧 R 指数衰减；仅保留当前池中存在的 key，避免字典无限增长。</summary>
    public void DecayLetdownReflex(float deltaTMinutes)
    {
        if (!EqualMilkingSettings.enableLetdownReflex || deltaTMinutes <= 0f) return;
        if (letdownReflexByKey == null || letdownReflexByKey.Count == 0) return;
        float lambda = Mathf.Max(0.001f, EqualMilkingSettings.letdownReflexDecayLambda);
        bool boostMode = EqualMilkingSettings.letdownReflexBoostMultiplier > 1f;
        float minR = Mathf.Clamp01(EqualMilkingSettings.letdownReflexMin);
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
            if (!boostMode && minR >= 1E-5f && r < minR) r = minR;
            if (r < 1E-5f) r = 0f;
            letdownReflexByKey[key] = r;
        }
        foreach (string key in toRemove)
            letdownReflexByKey.Remove(key);
    }

    /// <summary>四层模型：挤奶/吸奶刺激该侧，R += ΔR，Clamp 至 1。仅被刺激的那一侧（如第一对的左乳）提升喷乳反射。</summary>
    public void AddLetdownReflexStimulus(string sideKey)
    {
        if (!EqualMilkingSettings.enableLetdownReflex || string.IsNullOrEmpty(sideKey)) return;
        letdownReflexByKey ??= new Dictionary<string, float>();
        float deltaR = Mathf.Clamp(EqualMilkingSettings.letdownReflexStimulusDeltaR, 0f, 1f);
        float r = GetLetdownReflexRaw(sideKey);
        letdownReflexByKey[sideKey] = Mathf.Min(1f, r + deltaR);
    }

    /// <summary>归一化基础值 = 总容量（规格：基础值 = 总容量），当前实现为 1。</summary>
    public static float GetBaseValueNormalized(Pawn pawn = null) => 1f;

    /// <summary>有效药效系数：统一使用 EqualMilkingSettings.GetProlactinToleranceFactor。</summary>
    public float GetEffectiveDrugFactor()
    {
        return EqualMilkingSettings.GetProlactinToleranceFactor(Pawn);
    }

    /// <summary>流速因子显示用：整数显示为 "1"，否则保留 1～2 位小数。</summary>
    private static string FormatFlowFactor(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return "0";
        if (Mathf.Approximately(v, Mathf.Floor(v))) return ((int)v).ToString();
        return v <= 10f ? v.ToString("F2").TrimEnd('0').TrimEnd('.') : v.ToString("F1");
    }

    /// <summary>每日衰减 D(L,E) = 1/(B_T×E) + k×L + η·I（启用炎症时）。B_T 由设置反推。</summary>
    public float GetDailyLactationDecay(float lactationAmount)
    {
        float eff = GetEffectiveDrugFactor();
        if (eff <= 0f) return 0f;
        float bT = EqualMilkingSettings.GetEffectiveBaseValueTForDecay();
        float D = 1f / (bT * eff) + PoolModelConstants.NegativeFeedbackK * lactationAmount;
        if (EqualMilkingSettings.enableInflammationModel)
            D += EqualMilkingSettings.lactationDecayInflammationEta * Mathf.Max(0f, currentInflammation);
        return D;
    }

    /// <summary>当前 L 下的每日衰减（用于显示）。</summary>
    public float GetDailyLactationDecay() => GetDailyLactationDecay(currentLactationAmount);

    /// <summary>吃药时进水：ΔL = Δs × C_dose。Δs 为本次实际生效的 Lactating 严重度增量（已包含一次耐受削弱）。</summary>
    public void AddFromDrug(float deltaSeverity)
    {
        currentLactationAmount += deltaSeverity * PoolModelConstants.DoseToLFactor;
    }

    /// <summary>分娩时累加：L += 基础值（不乘有效药效系数）。</summary>
    public void AddFromBirth()
    {
        currentLactationAmount += GetBaseValueNormalized(Pawn);
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (!Pawn.IsMilkable())
        {
            base.CompPostTick(ref severityAdjustment);
            return;
        }
        // 水池模型（L 驱动）：每 200 tick 更新。永久泌乳/动物：不衰减 L，仅 L≤0 时设为基础值，避免池满时流速=0 仍衰减导致 L→0→重置 的波动。见 记忆库/decisions/ADR-001-进水与衰减周期。
        if (Pawn.IsHashIntervalTick(200))
        {
            bool permanentOrAnimal = Pawn.genes?.HasActiveGene(EMDefOf.EM_Permanent_Lactation) == true
                || (EqualMilkingSettings.femaleAnimalAdultAlwaysLactating && Pawn.IsAdultFemaleAnimalOfColony());
            if (permanentOrAnimal)
            {
                if (currentLactationAmount < PoolModelConstants.LactationEndEpsilon)
                    currentLactationAmount = PoolModelConstants.BaseValueTBirth;
                if (!cachedWasPermanentLactation)
                {
                    cachedWasPermanentLactation = true;
                    Parent.SetDirty();
                }
                // 不衰减 L，池满时流速=0 但 L 保持，池空后流速自然恢复
            }
            else
            {
                cachedWasPermanentLactation = false;
                float step = 200f / 60000f;
                float dailyDecay = GetDailyLactationDecay(currentLactationAmount) * step;
                currentLactationAmount = Mathf.Max(0f, currentLactationAmount - dailyDecay);
                if (currentLactationAmount < PoolModelConstants.LactationEndEpsilon)
                    currentLactationAmount = 0f;
                if (currentLactationAmount <= 0f)
                {
                    ResetAndRemoveLactating();
                    return;
                }
            }
        }
        this.Charge = CompEquallyMilkable != null ? CompEquallyMilkable.Fullness : 0f;
    }

    /// <summary>泌乳结束：清空双池、移除 Lactating hediff。</summary>
    private void ResetAndRemoveLactating()
    {
        currentLactationAmount = 0f;
        CompEquallyMilkable?.ClearPools();
        Pawn.health.RemoveHediff(parent);
    }
    /// <summary>灌满期间额外营养/天：与产奶流速平衡，flow（池单位/天）× NutritionPerPoolUnit；满池后不额外消耗。</summary>
    public float ExtraNutritionPerDay()
    {
        if (Pawn.needs?.food == null) return 0f;
        float flow = GetFlowPerDay();
        return flow * PoolModelConstants.NutritionPerPoolUnit;
    }
    /// <summary>机械体灌满期间额外能量/天：与产奶流速 1:1 平衡，flowPerDay（池单位/天）× nutritionToEnergyFactor；满池后不额外消耗。</summary>
    public float ExtraEnergyPerDay()
    {
        if (Pawn.needs?.energy == null) return 0f;
        float flow = GetFlowPerDay();
        return flow * EqualMilkingSettings.nutritionToEnergyFactor;
    }
    /// <summary>当前产奶流速（池单位/天）= 左池流速 + 右池流速；与 CompEquallyMilkable 一致。启用四层时：驱动可用 D_eff=L·H(L)，再乘 PressureFactor(P)×R（见 Docs 第十二节）。</summary>
    internal float GetFlowPerDay()
    {
        var b = GetFlowPerDayBreakdown();
        return b.TotalFlow;
    }

    /// <summary>产奶流速拆解：总流速与各乘数因子，用于悬停显示「产奶效率：1(300%)/天」及「驱动×1 饥饿×1 状态×1 …」。</summary>
    internal FlowBreakdown GetFlowPerDayBreakdown()
    {
        var r = new FlowBreakdown();
        if (CompEquallyMilkable == null) return r;
        float maxF = Mathf.Max(0.001f, CompEquallyMilkable.maxFullness);
        float fullness = CompEquallyMilkable.Fullness;
        float hungerFactor = PawnUtility.BodyResourceGrowthSpeed(Pawn);
        if (currentLactationAmount <= 0f || hungerFactor <= 0f) return r;
        r.Drive = EqualMilkingSettings.GetEffectiveDrive(currentLactationAmount);
        r.Hunger = hungerFactor;
        r.Conditions = Pawn.GetMilkFlowMultiplierFromConditions();
        r.Genes = Pawn.GetMilkFlowMultiplierFromGenes();
        r.Setting = EqualMilkingSettings.defaultFlowMultiplierForHumanlike;
        float flowLeftMult = Pawn.GetMilkFlowMultiplierFromRJW_Left();
        float flowRightMult = Pawn.GetMilkFlowMultiplierFromRJW_Right();
        r.RjwSum = flowLeftMult + flowRightMult;
        float flow = r.Drive * r.Hunger * r.Conditions * r.Genes * r.Setting * r.RjwSum;
        var entries = Pawn.GetBreastPoolEntries();
        var milkComp = CompEquallyMilkable;
        // 压力、喷乳反射、状态（如乳腺炎）均按「哪对乳房的哪一侧」分别计算
        if (entries != null && entries.Count > 0 && milkComp != null)
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
                float pressureE = EqualMilkingSettings.enablePressureFactor
                    ? EqualMilkingSettings.GetPressureFactor(fullE / Mathf.Max(0.001f, stretch))
                    : (fullE >= stretch ? 0f : 1f);
                float letdownE = EqualMilkingSettings.enableLetdownReflex ? GetLetdownReflexFlowMultiplier(e.Key) : 1f;
                float contrib = conditionsE * e.FlowMultiplier * pressureE * letdownE;
                flowWithAllFactors += contrib;
                sumWeightedLetdown += e.FlowMultiplier * letdownE;
                sumWeightedPressure += e.FlowMultiplier * pressureE * letdownE;
                sumWeightedConditions += e.FlowMultiplier * conditionsE * pressureE * letdownE;
            }
            r.Pressure = sumWeightedLetdown >= 1E-5f ? sumWeightedPressure / sumWeightedLetdown : (EqualMilkingSettings.enablePressureFactor ? EqualMilkingSettings.GetPressureFactor(fullness / maxF) : (fullness >= maxF ? 0f : 1f));
            r.Letdown = r.RjwSum >= 1E-5f ? sumWeightedLetdown / r.RjwSum : (EqualMilkingSettings.enableLetdownReflex ? GetLetdownReflexFlowMultiplier() : 1f);
            r.Conditions = flowWithAllFactors >= 1E-5f ? sumWeightedConditions / flowWithAllFactors : r.Conditions;
            if (flowWithAllFactors >= 1E-5f)
                flow = baseWithoutConditions * flowWithAllFactors;
            else
            {
                float basePerDayFallback = r.Drive * r.Hunger * r.Conditions * r.Genes * r.Setting;
                flow = basePerDayFallback * r.RjwSum;
                if (!EqualMilkingSettings.enablePressureFactor && fullness >= maxF) flow = 0f;
                else if (EqualMilkingSettings.enablePressureFactor) flow *= r.Pressure;
                if (EqualMilkingSettings.enableLetdownReflex) flow *= r.Letdown;
            }
        }
        else
        {
            float basePerDay = r.Drive * r.Hunger * r.Conditions * r.Genes * r.Setting;
            if (EqualMilkingSettings.enablePressureFactor)
            {
                r.Pressure = EqualMilkingSettings.GetPressureFactor(fullness / maxF);
                flow = basePerDay * r.RjwSum * r.Pressure;
            }
            else
            {
                r.Pressure = fullness >= maxF ? 0f : 1f;
                flow = fullness >= maxF ? 0f : basePerDay * r.RjwSum;
            }
            r.Letdown = EqualMilkingSettings.enableLetdownReflex ? GetLetdownReflexFlowMultiplier() : 1f;
            if (EqualMilkingSettings.enableLetdownReflex) flow *= r.Letdown;
        }
        r.TotalFlow = flow;
        return r;
    }

    /// <summary>产奶流速拆解：总流速与各乘数因子（与 GetFlowPerDay 一致），用于 UI 显示。</summary>
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

    /// <summary>健康页乳房行悬停：构建单侧（左或右）产奶效率的因子拆解行；状态、压力、喷乳反射按该侧显示（null 时用总览 b 的值）。</summary>
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

    public override string CompTipStringExtra
    {
        get
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

            // 1. 状态总括
            if (growthSpeed == 0f)
                lines.Add("LactatingStoppedBecauseHungry".Translate().Colorize(ColorLibrary.RedReadable));
            else if (isFull)
                lines.Add(isShrinking ? "EM.LactatingStateFullShrinking".Translate() : "EM.LactatingStateFull".Translate());
            else
                lines.Add("EM.LactatingStateProducing".Translate((totalMilk / maxF).ToStringPercent()));

            // 2. 池子与可产（哺乳期行不显示左/右乳明细，乳房行悬停已有每对容量与奶量；总行格式与单侧一致：总奶量(满基%)/总容量 基础(撑大)）
            if (CompEquallyMilkable != null)
            {
                float baseTotal = Mathf.Max(0.01f, Pawn.GetLeftBreastCapacityFactor() + Pawn.GetRightBreastCapacityFactor());
                string totalPercentStr = baseTotal >= 0.001f ? (totalMilk / baseTotal).ToStringPercent() : "0%";
                lines.Add("EM.PoolTotalMilkCapacityFull".Translate(
                    totalMilk.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    totalPercentStr,
                    baseTotal.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    maxF.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                if (EqualMilkingSettings.enableMilkQuality)
                {
                    float q = GetMilkQuality();
                    lines.Add("EM.MilkQuality".Translate(q.ToStringPercent()));
                }
                if (Pawn.MilkDef() != null)
                    lines.Add(Pawn.MilkDef().label + " x" + (Pawn.MilkAmount() * totalMilk).ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute));
            }
            else if (Pawn.MilkDef() != null)
            {
                lines.Add(Lang.MilkFullness + ": " + (Charge / maxF).ToStringPercent());
                lines.Add(Pawn.MilkDef().label + " x" + (Pawn.MilkAmount() * (CompEquallyMilkable?.Fullness ?? 0f)).ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute));
            }

            // 3. 流速与营养（按状态分支）
            if (growthSpeed > 0f)
            {
                if (isFull)
                {
                    lines.Add("EM.MilkFlowStoppedFull".Translate());
                    if (isShrinking)
                        lines.Add("EM.ReabsorbedNutritionPerDay".Translate(reabsorbed.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                }
                else
                {
                    var b = GetFlowPerDayBreakdown();
                    float flowPerDay = b.TotalFlow;
                    string flowPercent = (flowPerDay * 100f).ToString("F0") + "%";
                    lines.Add("EM.MilkFlowValuePercent".Translate(
                        flowPerDay.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                        flowPercent));
                    try
                    {
                        var breastList = Pawn.GetBreastList();
                        if (breastList != null && breastList.Count > 0)
                        {
                            foreach (var h in breastList)
                            {
                                if (h?.def == null) continue;
                                string key = Pawn.GetPoolKeyForBreastHediff(h);
                                if (string.IsNullOrEmpty(key)) continue;
                                var (flowL, flowR, _, _) = Pawn.GetFlowPerDayForBreastPair(key);
                                float pairTotal = flowL + flowR;
                                string label = h.LabelCap;
                                if (string.IsNullOrEmpty(label)) label = h.def.label;
                                lines.Add("EM.PoolPairFlowLine".Translate(
                                    label,
                                    pairTotal.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                                    flowL.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                                    flowR.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                            }
                        }
                    }
                    catch { }
                    if (Pawn.needs?.food != null)
                        lines.Add("LactatingAddedNutritionPerDay".Translate(ExtraNutritionPerDay().ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                    else if (Pawn.needs?.energy != null)
                        lines.Add("CurrentMechEnergyFallPerDay".Translate() + ": " + ExtraEnergyPerDay().ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute));
                }
            }

            // 4. 时间
            lines.Add("EM.PoolRemainingDays".Translate() + ": " + (IsPermanentLactation ? Lang.Permanent : RemainingDays.ToString("F1")));

            // 5. 等效剂量（只读：由 L 与公式推导，1 标准剂量 ≈ 0.5 L）
            float oneDoseL = 0.5f * PoolModelConstants.DoseToLFactor;
            if (oneDoseL > 0f && currentLactationAmount > 0f)
                lines.Add("EM.EquivalentDose".Translate((currentLactationAmount / oneDoseL).ToString("F1")));

            return lines.Count > 0 ? string.Join("\n", lines) : base.CompTipStringExtra;
        }
    }
    /// <summary>括号内保留「天数 + 满度%」，便于一眼看到剩余时间与池满度；悬停展开为 1～4 块详情。</summary>
    public override string CompLabelInBracketsExtra
    {
        get
        {
            float maxF = Mathf.Max(0.01f, CompEquallyMilkable?.maxFullness ?? 1f);
            float fullness = CompEquallyMilkable != null ? CompEquallyMilkable.Fullness : Charge;
            string head = IsPermanentLactation ? "，" : ("EM.PoolDaysPrefix".Translate() + RemainingDays.ToString("F1") + "，");
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
    /// <summary>吸奶/消费后：仅将 Charge 同步为当前池总满度，并触发 OnGathered（不按比例缩放池，因已由 DrainForConsume 按对按侧扣过）。</summary>
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
