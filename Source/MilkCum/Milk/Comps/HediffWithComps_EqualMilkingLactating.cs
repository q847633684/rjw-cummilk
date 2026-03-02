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
        // 水池模型（L 驱动）：每 200 tick 更新。永久泌乳/动物：不衰减 L，仅 L≤0 时设为基础值，避免池满时流速=0 仍衰减导致 L→0→重置 的波动。
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
    /// <summary>当前产奶流速（池单位/天）= 左池流速 + 右池流速；与 CompEquallyMilkable 一致。左/右池流速按侧计算；总池满(Fullness≥maxF)时返回 0（仅用于饱食度扣除与界面显示，池子进水仍由 CompEquallyMilkable.UpdateMilkPools 每 30 tick 执行，不依赖本方法）。使用当前 Fullness 而非 Charge，避免 NeedInterval 先于 CompPostTick 执行时仍扣饱食度。</summary>
    internal float GetFlowPerDay()
    {
        if (CompEquallyMilkable == null) return 0f;
        float maxF = CompEquallyMilkable.maxFullness;
        if (CompEquallyMilkable.Fullness >= maxF) return 0f;
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

            // 2. 池子与可产
            if (CompEquallyMilkable != null)
            {
                lines.Add("EM.PoolTotalMilkCapacity".Translate(
                    totalMilk.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                    maxF.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
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
                    float flowPerDay = GetFlowPerDay();
                    lines.Add("EM.MilkFlowPerDay".Translate(flowPerDay.ToStringPercent()));
                    if (Pawn.needs?.food != null)
                        lines.Add("LactatingAddedNutritionPerDay".Translate(ExtraNutritionPerDay().ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                    else if (Pawn.needs?.energy != null)
                        lines.Add("CurrentMechEnergyFallPerDay".Translate() + ": " + ExtraEnergyPerDay().ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute));
                }
            }

            // 4. 时间
            lines.Add("EM.PoolRemainingDays".Translate() + ": " + (IsPermanentLactation ? Lang.Permanent : RemainingDays.ToString("F1")));

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
