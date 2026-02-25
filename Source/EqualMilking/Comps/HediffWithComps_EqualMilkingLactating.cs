using EqualMilking;
using RimWorld;
using Verse;
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using EqualMilking.Helpers;
public class HediffWithComps_EqualMilkingLactating : HediffWithComps
{
    /// <summary>Granularity: 4 steps per severity (0.25). Stage table covers 0..MaxSeverityForStages; higher severity uses last stage.</summary>
    private const int SeverityStepsPerUnit = 4;
    private const int MaxSeverityForStages = 20;
    private HediffStage[] hediffStages;
    private HediffStage[] hediffStagesWithGain;
    private HediffStage vanillaStage;
    private HediffStage vanillaStageWithGain;
    private bool isDirty = true;
    public override HediffStage CurStage => GetCurStage();
    public override int CurStageIndex => GetStageIndex(this.Severity, pawn.CompEquallyMilkable().Fullness >= 1f);
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
        {
            hediffStages = new HediffStage[count];
            hediffStagesWithGain = new HediffStage[count];
        }
        for (int i = 0; i < maxSteps; i++)
        {
            float severity = (float)i / SeverityStepsPerUnit;
            GenStage(ref hediffStages[i * 2], severity, false, false);
            GenStage(ref hediffStages[i * 2 + 1], severity, true, false);
            GenStage(ref hediffStagesWithGain[i * 2], severity, false, true);
            GenStage(ref hediffStagesWithGain[i * 2 + 1], severity, true, true);
        }
        this.vanillaStage = new HediffStage { fertilityFactor = 0.05f };
        this.vanillaStageWithGain = new HediffStage { fertilityFactor = 0.05f };
        // vanillaStageWithGain 的 capMods 在 GetCurStage 里按当前 Severity 再填
    }
    /// <summary>仅吃过 Prolactin/Lucilactin（有耐受或成瘾）的泌乳期才给意识/操纵/移动增益。</summary>
    private static bool HasDrugInducedLactation(Pawn p)
    {
        return p?.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Tolerance) != null
            || p?.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Addiction) != null;
    }
    /// <summary>按吃药多少（severity）叠加；数值来自设置 lactatingGainCapModPercent，受 lactatingGainEnabled 控制。</summary>
    private static void AddGainCapMods(HediffStage stage, float severity)
    {
        if (!EqualMilkingSettings.lactatingGainEnabled || EqualMilkingSettings.lactatingGainCapModPercent <= 0f) return;
        float pct = Mathf.Clamp(EqualMilkingSettings.lactatingGainCapModPercent, 0f, 0.20f);
        float offset = pct * severity;
        if (stage.capMods == null) stage.capMods = new List<PawnCapacityModifier>();
        else stage.capMods.Clear();
        stage.capMods.Add(new PawnCapacityModifier { capacity = PawnCapacityDefOf.Consciousness, offset = offset });
        stage.capMods.Add(new PawnCapacityModifier { capacity = PawnCapacityDefOf.Manipulation, offset = offset });
        stage.capMods.Add(new PawnCapacityModifier { capacity = PawnCapacityDefOf.Moving, offset = offset });
    }
    /// <summary>Stage formulas use float severity so 0.5 vs 0.9999 differ: more severity = more milk, faster growth, more hunger. addGain = 吃药引起的泌乳期才加的 capMods。</summary>
    private void GenStage(ref HediffStage stage, float severity, bool isFull, bool addGain)
    {
        if (stage == null) { stage = new HediffStage(); }
        int severityInt = Mathf.FloorToInt(severity);
        if (severityInt >= 1) { stage.label = Lang.Permanent + " x" + severityInt.ToString(); }
        else { stage.label = ""; }
        // 食物消耗、产奶量、产奶效率：三项统一用同一倍率（lactatingEfficiencyMultiplierPerStack），加成%一致
        float mult = EqualMilkingSettings.lactatingEfficiencyMultiplierPerStack;
        stage.hungerRateFactorOffset = isFull ? 0f : Mathf.Pow(mult, Mathf.Max(severity, 0.5f));
        StatUtility.SetStatValueInList(ref stage.statOffsets, StatDefOf.MechEnergyUsageFactor, stage.hungerRateFactorOffset);
        StatUtility.SetStatValueInList(ref stage.statFactors, EMDefOf.EM_Milk_Amount_Factor, Mathf.Pow(mult, severity));
        StatUtility.SetStatValueInList(ref stage.statFactors, EMDefOf.EM_Lactating_Efficiency_Factor, isFull ? 0f : Mathf.Pow(mult, severity));
        if (addGain)
            AddGainCapMods(stage, severity);
        else if (stage.capMods != null)
            stage.capMods.Clear();
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
        {
            GenStages();
        }
        bool useGain = HasDrugInducedLactation(pawn);
        if (!pawn.IsMilkable() && pawn.RaceProps.Humanlike)
        {
            if (this.Severity > 1f) { this.Severity = 0.9999f; }
            if (useGain)
            {
                AddGainCapMods(vanillaStageWithGain, this.Severity);
                return vanillaStageWithGain;
            }
            return vanillaStage;
        }
        return useGain ? hediffStagesWithGain[CurStageIndex] : hediffStages[CurStageIndex];
    }
    public override bool Visible => pawn.IsMilkable() || pawn.RaceProps.Humanlike; //Milkable or breastfeedable in vanilla.
}
public class HediffComp_EqualMilkingLactating : HediffComp_Lactating
{
    public CompEquallyMilkable CompEquallyMilkable => this.Pawn.CompEquallyMilkable();
    public HediffWithComps_EqualMilkingLactating Parent => (HediffWithComps_EqualMilkingLactating)this.parent;
    public override void CompExposeData()
    {
        base.CompExposeData();
    }
    public float ExtraNutritionPerDay()
    {
        float tol = EqualMilkingSettings.GetProlactinTolerance(Pawn);
        return Parent.CurStage.hungerRateFactorOffset * (1f - tol) * PawnUtility.BodyResourceGrowthSpeed(base.Pawn) * Parent.pawn.BaseNutritionPerDay();
    }
    public float ExtraEnergyPerDay()
    {
        float tol = EqualMilkingSettings.GetProlactinTolerance(Pawn);
        float offset = Parent.CurStage.statOffsets.GetStatOffsetFromList(StatDefOf.MechEnergyUsageFactor);
        return offset * (1f - tol) * Pawn.needs.energy.BaseFallPerDay;
    }
    public override void CompPostTick(ref float severityAdjustment)
    {
        if (base.Pawn.IsHashIntervalTick(200))
        {
            severityAdjustment += 0.0033333334f * SeverityChangePerDay();
        }
        if (!Pawn.IsMilkable())
        {
            base.CompPostTick(ref severityAdjustment);
            return;
        }
        this.Charge = CompEquallyMilkable.Fullness;
    }
    public override string CompTipStringExtra
    {
        get
        {
            if (!Pawn.IsMilkable())
            {
                return base.CompTipStringExtra;
            }
            if (this.Charge >= 1f)
            {
                return "LactatingStoppedBecauseFull".Translate();
            }
            float growthSpeed = PawnUtility.BodyResourceGrowthSpeed(base.Pawn);
            if (growthSpeed == 0f)
            {
                return "LactatingStoppedBecauseHungry".Translate().Colorize(ColorLibrary.RedReadable);
            }
            if (Pawn.needs.food != null)
            {
                return "LactatingAddedNutritionPerDay".Translate(this.ExtraNutritionPerDay().ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute), Pawn.MilkGrowthMultiplier());
            }
            else if (Pawn.needs.energy != null)
            {
                return "CurrentMechEnergyFallPerDay".Translate() + ": " + this.ExtraEnergyPerDay().ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute);
            }
            return base.CompTipStringExtra;
        }
    }
    public override string CompLabelInBracketsExtra
    {
        get
        {
            return base.CompLabelInBracketsExtra + Lang.MilkFullness + ": " + this.Charge.ToStringPercent() + ", " + Pawn.MilkDef().label + " x" + (Pawn.MilkAmount() * CompEquallyMilkable.Fullness).ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute);
        }
    }
    public override string CompDebugString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append(base.CompDebugString());
        if (!base.Pawn.Dead)
        {
            stringBuilder.AppendLine("severity/day: " + this.SeverityChangePerDay().ToString("F3"));
        }
        return stringBuilder.ToString().TrimEndNewlines();
    }
    private float SeverityChangePerDay()
    {
        if (!Pawn.IsMilkable())
            return -0.1f;
        var addiction = Pawn.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Addiction);
        if (addiction != null)
        {
            if (addiction.CurStageIndex == 1)
            {
                float tol = Pawn.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Tolerance)?.Severity ?? 0f;
                return -0.1f * (1f + tol);
            }
            return 0f; // 成瘾且满足需求 → 不衰减（永久维持）
        }
        // 未成瘾：不论 severity 多高都按耐受衰减
        float tolerance = Pawn.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Tolerance)?.Severity ?? 0f;
        return -0.1f * (1f + tolerance);
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
