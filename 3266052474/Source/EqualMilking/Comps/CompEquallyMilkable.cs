using Verse;
using RimWorld;
using UnityEngine;
using EqualMilking.Helpers;
using System.Collections.Generic;
using EqualMilking.Data;

namespace EqualMilking;
public class CompEquallyMilkable : CompMilkable
{
    protected Pawn Pawn => parent as Pawn;
    protected override int GatherResourcesIntervalDays => Mathf.Max((int)Props.milkIntervalDays, 1);
    protected float fGatherResourcesIntervalDays => Pawn.MilkIntervalDays();
    protected override int ResourceAmount => (int)Pawn.MilkAmount();
    protected override ThingDef ResourceDef => Pawn.MilkDef();
    protected virtual float fResourceAmount => Pawn.MilkAmount();
    protected virtual float GrowthMultiplier => Pawn.MilkGrowthMultiplier();
    public float breastfedAmount = 0f;
    public float maxFullness = 1f;
    private MilkSettings milkSettings = null;
    public MilkSettings MilkSettings
    {
        get
        {
            if (milkSettings == null && parent is Pawn pawn)
            {
                milkSettings = pawn.GetDefaultMilkSetting();
            }
            return milkSettings;
        }
    }
    internal List<Pawn> assignedFeeders = new();
    private int updateTick = 0;
    private bool cachedActive = false;
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref breastfedAmount, "BreastfedAmount", 0f);
        Scribe_Deep.Look(ref milkSettings, "MilkSettings");
        Scribe_Collections.Look(ref assignedFeeders, "CanBeFedBy", LookMode.Reference);
    }
    protected override bool Active
    {
        get
        {
            if (updateTick > Find.TickManager.TicksGame) { return cachedActive; }
            if (parent.Faction == null || parent is not Pawn pawn || !parent.SpawnedOrAnyParentSpawned || !pawn.IsColonyPawn())
            {
                updateTick = Find.TickManager.TicksGame + 500;
                cachedActive = false;
                return false;
            }
            // Add/Remove lactating hediff
            // Apply gene effect
            if (pawn.genes?.HasActiveGene(EMDefOf.EM_Permanent_Lactation) == true)
            {
                Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating);
                lactating.Severity = Mathf.Max(lactating.Severity, 0.9999f);
            }
            // Remove lactating hediff for non-milkable animals/mechs
            else if (!pawn.RaceProps.Humanlike && !pawn.IsMilkable())
            {
                if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) is Hediff lactating)
                {
                    pawn.health.RemoveHediff(lactating);
                }
                cachedActive = false;
                updateTick = Find.TickManager.TicksGame + 500;
                return false;
            }
            // Add lactating hediff for lactating animals
            else if (EqualMilkingSettings.femaleAnimalAdultAlwaysLactating && pawn.IsAdultFemaleAnimalOfColony())
            {
                Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating);
                lactating.Severity = Mathf.Max(lactating.Severity, 1f);
            }

            if (pawn.IsLactating() && pawn.IsMilkable())
            {
                cachedActive = true;
                return true;
            }

            cachedActive = false;
            updateTick = Find.TickManager.TicksGame + 500;
            return false;
        }
    }
    public override void CompTick()
    {
        if (!parent.IsHashIntervalTick(30)) { return; }
        if (!Active || fullness == maxFullness) { return; }
        float num = maxFullness / (this.fGatherResourcesIntervalDays * 2000f); // 60000Ticks/day over 30 ticks/process
        fullness += num * GrowthMultiplier;
        if (fullness > maxFullness)
        {
            fullness = maxFullness;
        }
    }
    /// <summary>
    /// Set the fullness of the milkable pawn, will clamp the value between 0 and maxFullness
    /// </summary>
    /// <param name="value">new fullness value</param>
    public void SetFullness(float value)
    {
        fullness = Mathf.Clamp(value, 0f, maxFullness);
    }
    /// <summary>
    /// Gather the milk from the milkable pawn, place the milk in the milking spot or the doer's position. Overrides vanilla method.
    /// </summary>
    /// <param name="doer">the pawn that is gathering the milk</param>
    new public void Gathered(Pawn doer)
    {
        if (!Active)
        {
            Log.Error(string.Concat(doer, " gathered body resources while not Active: ", parent));
        }
        float yieldFactor = doer.GetStatValue(StatDefOf.AnimalGatherYield);
        Building_Milking milkingSpot = (doer.jobs?.curDriver as JobDriver_EquallyMilk)?.MilkBuilding;
        if (milkingSpot != null)
        {
            yieldFactor += milkingSpot.YieldOffset();
        }
        if (!Rand.Chance(yieldFactor) && parent.Map != null)
        {
            MoteMaker.ThrowText((doer.DrawPos + parent.DrawPos) / 2f, parent.Map, Lang.ProductWasted, 3.65f);
        }
        else
        {
            int num = GenMath.RoundRandom(fResourceAmount * fullness);
            Pawn pawn = parent as Pawn;
            pawn.LactatingHediffWithComps()?.OnGathered();

            while (num > 0)
            {
                int stack = Mathf.Clamp(num, 1, ResourceDef.stackLimit);
                num -= stack;
                Thing thing = ThingMaker.MakeThing(ResourceDef);
                if (thing.TryGetComp<CompShowProducer>() is CompShowProducer compShowProducer)
                {
                    if (EqualMilkingSettings.HasRaceTag(thing))
                    {
                        compShowProducer.producerKind = pawn.kindDef;

                    }
                    if (EqualMilkingSettings.HasPawnTag(thing))
                    {
                        compShowProducer.producer = pawn;
                    }
                }

                thing.stackCount = stack;
                if (milkingSpot != null && milkingSpot.def != EMDefOf.EM_MilkingSpot)
                {
                    milkingSpot.PlaceMilkThing(thing);
                }
                else
                {
                    GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near);
                }
            }
        }
        fullness = 0f;
    }
    /// <summary>
    /// Check if the pawn is allowed to be auto fed by the milkable pawn.
    /// Internally, it checks if the assignedFeeders list is empty or contains the pawn.
    /// </summary>
    /// <param name="pawn">the pawn that is being checked</param>
    /// <returns>true if the pawn is allowed to be auto fed by the milkable pawn</returns>
    public bool AllowedToBeAutoFedBy(Pawn pawn)
    {
        return this.MilkSettings?.canBeFed == true && (assignedFeeders.NullOrEmpty() || assignedFeeders.Contains(pawn));
    }
}
