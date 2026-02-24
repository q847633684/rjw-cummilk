using EqualMilking;
using RimWorld;
using Verse;
using UnityEngine;
using System.Text;
using EqualMilking.Helpers;
public class HediffWithComps_EqualMilkingLactating : HediffWithComps
{
    private HediffStage[] hediffStages = new HediffStage[EqualMilkingSettings.maxLactationStacks * 2 + 2];
    private HediffStage vanillaStage;
    private bool isDirty = true;
    public override HediffStage CurStage => GetCurStage();
    public override int CurStageIndex => GetStageIndex(Mathf.FloorToInt(this.Severity), pawn.CompEquallyMilkable().Fullness >= 1f);
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
        {
            return false;
        }
        // Enhance the severity of the hediff if it is less than 1
        if (other.Severity < 1f && this.Severity < 1f)
        {
            this.Severity += other.Severity;
            if (this.Severity > 1f)
            {
                this.Severity = 0.9999f; // Prevent the severity from reaching 1
            }
            return true;
        }
        if (this.Severity < EqualMilkingSettings.maxLactationStacks)
        {
            this.Severity += other.Severity;
            this.Severity = Mathf.Floor(this.Severity);
            this.ageTicks = 0;
            return true;
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
        {
            this.Severity = 0.9999f;
        }
    }
    public void OnGathered(float fullness)
    {
        if (this.Severity < 1f)
        {
            if (this.Severity + fullness >= 1f)
            {
                this.Severity = 0.9999f;
            }
            else
            {
                this.Severity += fullness;
            }
        }
    }
    private void GenStages()
    {
        if (hediffStages == null || hediffStages.Length == 0)
        {
            hediffStages = new HediffStage[EqualMilkingSettings.maxLactationStacks * 2 + 2];
        }
        for (int i = 0; i <= EqualMilkingSettings.maxLactationStacks; i++)
        {
            GenStage(ref hediffStages[i * 2], i, false);
            GenStage(ref hediffStages[i * 2 + 1], i, true);
        }
        this.vanillaStage = new HediffStage
        {
            fertilityFactor = 0.05f
        };

    }
    private void GenStage(ref HediffStage stage, int severity, bool isFull)
    {
        if (stage == null) { stage = new HediffStage(); }
        if (severity >= 1) { stage.label = Lang.Permanent + " x" + severity.ToString(); }
        else { stage.label = ""; }
        stage.hungerRateFactorOffset = isFull ? 0f : Mathf.Pow(EqualMilkingSettings.hungerRateMultiplierPerStack, Mathf.Max(severity, 1f));
        StatUtility.SetStatValueInList(ref stage.statOffsets, StatDefOf.MechEnergyUsageFactor, stage.hungerRateFactorOffset);
        StatUtility.SetStatValueInList(ref stage.statFactors, EMDefOf.EM_Milk_Amount_Factor, Mathf.Pow(EqualMilkingSettings.milkAmountMultiplierPerStack, Mathf.Max(severity - 1, 0f)));
        StatUtility.SetStatValueInList(ref stage.statFactors, EMDefOf.EM_Lactating_Efficiency_Factor, isFull ? 0f : Mathf.Pow(EqualMilkingSettings.lactatingEfficiencyMultiplierPerStack, Mathf.Max(severity - 1, 0f)));
    }
    private int GetStageIndex(int severity, bool isFull)
    {
        return severity * 2 + (isFull ? 1 : 0);
    }
    private HediffStage GetCurStage()
    {
        if (this.hediffStages == null || this.hediffStages.Length == 0 || this.hediffStages[0] == null)
        {
            GenStages();
        }
        if (!pawn.IsMilkable() && pawn.RaceProps.Humanlike)
        {
            if (this.Severity > 1f) { this.Severity = 0.9999f; }//No permanent lactation for non-milkable humanlike
            return vanillaStage;
        }
        return hediffStages[CurStageIndex];
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
        return Parent.CurStage.hungerRateFactorOffset * PawnUtility.BodyResourceGrowthSpeed(base.Pawn) * Parent.pawn.BaseNutritionPerDay();
    }
    public float ExtraEnergyPerDay()
    {
        return Parent.CurStage.statOffsets.GetStatOffsetFromList(StatDefOf.MechEnergyUsageFactor) * Pawn.needs.energy.BaseFallPerDay;
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
        {
            return -0.1f;
        }
        return this.Parent.Severity >= 1f ? 0f : -0.1f;
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
