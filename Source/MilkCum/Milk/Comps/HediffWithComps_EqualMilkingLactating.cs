using MilkCum.Core;
using MilkCum.Milk.Comps;
using MilkCum.Milk.Helpers;
using RimWorld;
using Verse;
using UnityEngine;
using System.Text;
using System.Collections.Generic;
public class HediffWithComps_EqualMilkingLactating : HediffWithComps
{
    /// <summary>Granularity: 4 steps per severity (0.25). Stage table covers 0..MaxSeverityForStages; higher severity uses last stage.</summary>
    private const int SeverityStepsPerUnit = 4;
    private const int MaxSeverityForStages = 20;
    private HediffStage[] hediffStages;
    private HediffStage vanillaStage;
    private bool isDirty = true;
    public override HediffStage CurStage => GetCurStage();
    public override int CurStageIndex => GetStageIndex(this.Severity, (pawn.CompEquallyMilkable()?.Fullness ?? 0f) >= Mathf.Max(0.01f, pawn.CompEquallyMilkable()?.maxFullness ?? 1f));
    public override void PostTick()
    {
        if (isDirty)
        {
            isDirty = false;
            this.GenStages();
            EMDefOf.EM_Milk_Amount_Factor.Worker.ClearCacheForThing(this.pawn);
            EMDefOf.EM_Lactating_Efficiency_Factor.Worker.ClearCacheForThing(this.pawn);
            StatDefOf.MechEnergyUsageFactor.Worker.ClearCacheForThing(this.pawn);
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
        // 水池模型：吃药累加在 ingestion postfix 中统一处理，此处不再调用以免重复
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
    }
    public void OnGathered(float fullness)
    {
        if (this.Severity < 1f && this.Severity + fullness >= 1f)
            this.Severity = 1f;
        else
            this.Severity += fullness;
    }
    private void GenStages()
    {
        int maxSteps = MaxSeverityForStages * SeverityStepsPerUnit + 1;
        int count = maxSteps * 2;
        if (hediffStages == null || hediffStages.Length != count)
            hediffStages = new HediffStage[count];
        for (int i = 0; i < maxSteps; i++)
        {
            float severity = (float)i / SeverityStepsPerUnit;
            GenStage(ref hediffStages[i * 2], severity, false);
            GenStage(ref hediffStages[i * 2 + 1], severity, true);
        }
        this.vanillaStage = new HediffStage { fertilityFactor = 0.05f };
    }
    /// <summary>Stage formulas use float severity; 增益已改为独立 hediff EM_LactatingGain。</summary>
    private void GenStage(ref HediffStage stage, float severity, bool isFull)
    {
        if (stage == null) { stage = new HediffStage(); }
        int severityInt = Mathf.FloorToInt(severity);
        if (severityInt >= 1) { stage.label = Lang.Permanent + " x" + severityInt.ToString(); }
        else { stage.label = ""; }
        float mult = EqualMilkingSettings.lactatingEfficiencyMultiplierPerStack;
        // 额外饥饿/能量改为由 ExtraNutritionPerDay/GetFlowPerDay 与 Need_Food/Need 补丁 1:1 施加，此处不再用 offset 增加饥饿
        stage.hungerRateFactorOffset = 0f;
        StatUtility.SetStatValueInList(ref stage.statOffsets, StatDefOf.MechEnergyUsageFactor, 0f);
        StatUtility.SetStatValueInList(ref stage.statFactors, EMDefOf.EM_Milk_Amount_Factor, Mathf.Pow(mult, severity));
        StatUtility.SetStatValueInList(ref stage.statFactors, EMDefOf.EM_Lactating_Efficiency_Factor, isFull ? 0f : Mathf.Pow(mult, severity));
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

    public CompEquallyMilkable CompEquallyMilkable => this.Pawn.CompEquallyMilkable();
    public HediffWithComps_EqualMilkingLactating Parent => (HediffWithComps_EqualMilkingLactating)this.parent;

    /// <summary>剩余天数（游戏日），由 L 计算：L / (1/(B_T×E)+k×L)，仅作显示。</summary>
    public float RemainingDays
    {
        get
        {
            float L = currentLactationAmount;
            if (L <= 0f) return 0f;
            float D = GetDailyLactationDecay(L);
            if (D <= 0f) return 0f;
            return L / D;
        }
    }
    /// <summary>当前泌乳量 L（规格：基础值 = 总容量，归一化 1）。</summary>
    public float CurrentLactationAmount => currentLactationAmount;

    public override void CompExposeData()
    {
        base.CompExposeData();
        float legacyRemainingDays = 0f;
        Scribe_Values.Look(ref legacyRemainingDays, "PoolRemainingDays", 0f);
        Scribe_Values.Look(ref currentLactationAmount, "PoolCurrentLactationAmount", 0f);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (currentLactationAmount <= 0f && legacyRemainingDays > 0f)
                currentLactationAmount = legacyRemainingDays / (PoolModelConstants.BaseValueT * Mathf.Max(GetEffectiveDrugFactor(), PoolModelConstants.EffectiveDrugFactorMin));
            else if (currentLactationAmount <= 0f && Parent.Severity > 0f)
            {
                float eff = GetEffectiveDrugFactor();
                currentLactationAmount = Parent.Severity * GetBaseValueNormalized(Pawn) * eff;
            }
        }
    }

    /// <summary>归一化基础值 = 总容量（规格：基础值 = 总容量），当前实现为 1。</summary>
    public static float GetBaseValueNormalized(Pawn pawn = null) => 1f;

    /// <summary>有效药效系数：统一使用 EqualMilkingSettings.GetProlactinToleranceFactor。</summary>
    public float GetEffectiveDrugFactor()
    {
        return EqualMilkingSettings.GetProlactinToleranceFactor(Pawn);
    }

    /// <summary>每日衰减 D(L,E) = 1/(B_T×E) + k×L（游戏日⁻¹）。B_T 由设置 baselineMilkDurationDays 反推，使单次剂量时剩余约 N 日。</summary>
    public float GetDailyLactationDecay(float lactationAmount)
    {
        float eff = GetEffectiveDrugFactor();
        if (eff <= 0f) return 0f;
        float bT = EqualMilkingSettings.GetEffectiveBaseValueTForDecay();
        return 1f / (bT * eff) + PoolModelConstants.NegativeFeedbackK * lactationAmount;
    }

    /// <summary>当前 L 下的每日衰减（用于显示/兼容）。</summary>
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
        // 水池模型（L 驱动）：每游戏日 L −= D(L,E)；D = 1/(B_T×E)+k×L；L≤0 或 L&lt;ε 时泌乳结束。
        if (Pawn.IsHashIntervalTick(200))
        {
            bool permanentOrAnimal = Pawn.genes?.HasActiveGene(EMDefOf.EM_Permanent_Lactation) == true
                || (EqualMilkingSettings.femaleAnimalAdultAlwaysLactating && Pawn.IsAdultFemaleAnimalOfColony());
            if (currentLactationAmount <= 0f && permanentOrAnimal)
                currentLactationAmount = PoolModelConstants.BaseValueTBirth;
            else
            {
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
    /// <summary>灌满期间额外营养/天：与产奶流速 1:1 平衡，灌满多少池就消耗多少营养；满池后不额外消耗。</summary>
    public float ExtraNutritionPerDay()
    {
        if (Pawn.needs?.food == null) return 0f;
        float flow = GetFlowPerDay();
        return flow;
    }
    /// <summary>机械体灌满期间额外能量/天：与产奶流速 1:1 平衡，flowPerDay（池单位/天）× nutritionToEnergyFactor；满池后不额外消耗。</summary>
    public float ExtraEnergyPerDay()
    {
        if (Pawn.needs?.energy == null) return 0f;
        float flow = GetFlowPerDay();
        return flow * EqualMilkingSettings.nutritionToEnergyFactor;
    }
    /// <summary>当前产奶流速（池单位/天）= 左池流速 + 右池流速；与 CompEquallyMilkable 一致。左池流速 = 左乳 RJW × L×饥饿×Conditions×Genes×流速倍率，右池流速 = 右乳 RJW × 同上；满池或不可产奶时返回 0。</summary>
    internal float GetFlowPerDay()
    {
        if (CompEquallyMilkable == null) return 0f;
        float maxF = CompEquallyMilkable.maxFullness;
        if (Charge >= maxF) return 0f;
        float hungerFactor = PawnUtility.BodyResourceGrowthSpeed(Pawn);
        if (currentLactationAmount <= 0f || hungerFactor <= 0f) return 0f;
        float basePerDay = currentLactationAmount * hungerFactor
            * Pawn.GetMilkFlowMultiplierFromConditions()
            * Pawn.GetMilkFlowMultiplierFromGenes()
            * EqualMilkingSettings.defaultFlowMultiplierForHumanlike;
        float flowLeft = Pawn.GetMilkFlowMultiplierFromRJW_Left() * basePerDay;
        float flowRight = Pawn.GetMilkFlowMultiplierFromRJW_Right() * basePerDay;
        return flowLeft + flowRight;
    }
    public override string CompTipStringExtra
    {
        get
        {
            if (!Pawn.IsMilkable())
            {
                return base.CompTipStringExtra;
            }
            var lines = new List<string>();
            float maxF = Mathf.Max(0.01f, CompEquallyMilkable?.maxFullness ?? 1f);
            if (this.Charge >= maxF)
            {
                lines.Add("LactatingStoppedBecauseFull".Translate());
            }
            else
            {
                float growthSpeed = PawnUtility.BodyResourceGrowthSpeed(base.Pawn);
                if (growthSpeed == 0f)
                {
                    lines.Add("LactatingStoppedBecauseHungry".Translate().Colorize(ColorLibrary.RedReadable));
                }
                else if (Pawn.needs.food != null)
                {
                    lines.Add("LactatingAddedNutritionPerDay".Translate(this.ExtraNutritionPerDay().ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute), Pawn.MilkGrowthMultiplier()));
                }
                else if (Pawn.needs.energy != null)
                {
                    lines.Add("CurrentMechEnergyFallPerDay".Translate() + ": " + this.ExtraEnergyPerDay().ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute));
                }
            }
            // 产奶详情全部放在悬停 tooltip 中
            if (CompEquallyMilkable != null)
            {
                float leftCap = Mathf.Max(0.01f, Pawn.GetLeftBreastCapacityFactor());
                float rightCap = Mathf.Max(0.01f, Pawn.GetRightBreastCapacityFactor());
                lines.Add(Lang.MilkFullness + ": " + (Charge / maxF).ToStringPercent());
                lines.Add("EM.PoolLeftBreast".Translate() + " " + (CompEquallyMilkable.LeftFullness / leftCap).ToStringPercent() + ", " + "EM.PoolRightBreast".Translate() + " " + (CompEquallyMilkable.RightFullness / rightCap).ToStringPercent());
                lines.Add("EM.PoolRemainingDays".Translate() + ": " + RemainingDays.ToString("F1"));
                float flowPerDay = GetFlowPerDay();
                lines.Add("EM.MilkFlowPerDay".Translate(flowPerDay.ToStringPercent()));
                if (Pawn.MilkDef() != null)
                    lines.Add(Pawn.MilkDef().label + " x" + (Pawn.MilkAmount() * CompEquallyMilkable.Fullness).ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute));
            }
            else if (Pawn.MilkDef() != null)
            {
                lines.Add(Lang.MilkFullness + ": " + (Charge / maxF).ToStringPercent());
                lines.Add(Pawn.MilkDef().label + " x" + (Pawn.MilkAmount() * (CompEquallyMilkable?.Fullness ?? 0f)).ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute));
            }
            return lines.Count > 0 ? string.Join("\n", lines) : base.CompTipStringExtra;
        }
    }
    public override string CompLabelInBracketsExtra
    {
        get
        {
            float maxF = Mathf.Max(0.01f, CompEquallyMilkable?.maxFullness ?? 1f);
            var s = base.CompLabelInBracketsExtra + Lang.MilkFullness + ": " + (Charge / maxF).ToStringPercent();
            return s;
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
