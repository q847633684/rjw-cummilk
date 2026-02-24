using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using EqualMilking.Data;
using static EqualMilking.Helpers.Categories;

namespace EqualMilking.Helpers;

public static class ExtensionHelper
{
    private const int LactatingStateCacheTicks = 60;
    private static readonly Dictionary<Pawn, (bool value, int tick)> LactatingStateCache = new();

    public static MilkSettings GetDefaultMilkSetting(this Pawn pawn)
    {
        if (pawn == null) { return null; }
        return GetPawnCategory(pawn) switch
        {
            PawnCategory.Colonist => EqualMilkingSettings.colonistSetting.Copy(),
            PawnCategory.Prisoner => EqualMilkingSettings.prisonerSetting.Copy(),
            PawnCategory.Slave => EqualMilkingSettings.slaveSetting.Copy(),
            PawnCategory.Animal => EqualMilkingSettings.animalSetting.Copy(),
            PawnCategory.Mechanoid => EqualMilkingSettings.mechSetting.Copy(),
            PawnCategory.Entity => EqualMilkingSettings.entitySetting.Copy(),
            _ => null,
        };
    }
    public static float BaseNutritionPerDay(this Pawn p)
    {
        return p.ageTracker.CurLifeStage.hungerRateFactor * p.RaceProps.baseHungerRate * 2.6666667E-05f * 60000f;
    }
    #region Breastfeeding
    public static bool IsMilkable(this Pawn pawn) => EqualMilkingSettings.IsMilkable(pawn);
    public static bool HasNutritiousMilk(this Pawn pawn) => pawn.IsMilkable() && (pawn.MilkDef().ingestible?.CachedNutrition ?? 0) > 0;
    public static bool HasDrugMilk(this Pawn pawn) => pawn.IsMilkable() && ((pawn.MilkDef().ingestible?.drugCategory ?? DrugCategory.None) != DrugCategory.None);
    public static bool HasEdibleMilk(this Pawn pawn) => pawn.HasNutritiousMilk() || pawn.HasDrugMilk();
    public static bool IsLactating(this Pawn pawn) => pawn?.health?.hediffSet?.HasHediff(HediffDefOf.Lactating) ?? false;
    /// <summary>统一泌乳判断：本体 Lactating 或 RJW 系 Lactating_Drug / Lactating_Permanent / Heavy_Lactating_Permanent。带 60 tick 缓存。</summary>
    public static bool IsInLactatingState(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return false;
        int now = Find.TickManager.TicksGame;
        if (LactatingStateCache.TryGetValue(pawn, out var cached) && now - cached.tick < LactatingStateCacheTicks)
            return cached.value;
        bool value = pawn.health.hediffSet.HasHediff(HediffDefOf.Lactating)
            || (DefDatabase<HediffDef>.GetNamedSilentFail("Lactating_Drug") is HediffDef d1 && pawn.health.hediffSet.HasHediff(d1))
            || (DefDatabase<HediffDef>.GetNamedSilentFail("Lactating_Permanent") is HediffDef d2 && pawn.health.hediffSet.HasHediff(d2))
            || (DefDatabase<HediffDef>.GetNamedSilentFail("Heavy_Lactating_Permanent") is HediffDef d3 && pawn.health.hediffSet.HasHediff(d3));
        LactatingStateCache[pawn] = (value, now);
        return value;
    }
    /// <summary>清除泌乳缓存（如 hediff 变更时可调，非必须）。</summary>
    public static void ClearLactatingStateCache(Pawn pawn = null)
    {
        if (pawn == null) LactatingStateCache.Clear();
        else LactatingStateCache.Remove(pawn);
    }
    public static bool AllowMilking(this Pawn pawn) => pawn.CompEquallyMilkable()?.MilkSettings?.allowMilking ?? false;
    public static bool SetAllowMilking(this Pawn pawn, bool allow)
    {
        MilkSettings milkSettings = pawn.CompEquallyMilkable()?.MilkSettings;
        if (milkSettings == null) return false;
        milkSettings.allowMilking = allow;
        return true;
    }
    public static bool AllowToBeFed(this Pawn pawn) => pawn.CompEquallyMilkable()?.MilkSettings?.canBeFed ?? false;
    public static bool SetAllowToBeFed(this Pawn pawn, bool allow)
    {
        MilkSettings milkSettings = pawn.CompEquallyMilkable()?.MilkSettings;
        if (milkSettings == null) return false;
        milkSettings.canBeFed = allow;
        return true;
    }
    public static bool AllowMilkingSelf(this Pawn pawn) => pawn.CompEquallyMilkable()?.MilkSettings?.allowMilkingSelf ?? false;
    public static bool SetAllowMilkingSelf(this Pawn pawn, bool allow)
    {
        MilkSettings milkSettings = pawn.CompEquallyMilkable()?.MilkSettings;
        if (milkSettings == null) return false;
        milkSettings.allowMilkingSelf = allow;
        return true;
    }
    public static bool AllowBreastFeeding(this Pawn pawn) => pawn.CompEquallyMilkable()?.MilkSettings?.allowBreastFeeding ?? false;
    public static bool SetAllowBreastFeeding(this Pawn pawn, bool allow)
    {
        MilkSettings milkSettings = pawn.CompEquallyMilkable()?.MilkSettings;
        if (milkSettings == null) return false;
        milkSettings.allowBreastFeeding = allow;
        return true;
    }
    public static bool AllowBreastFeedingAdult(this Pawn pawn) => pawn.CompEquallyMilkable()?.MilkSettings?.allowBreastFeedingAdult ?? false;
    public static bool SetAllowBreastFeedingAdult(this Pawn pawn, bool allow)
    {
        MilkSettings milkSettings = pawn.CompEquallyMilkable()?.MilkSettings;
        if (milkSettings == null) return false;
        milkSettings.allowBreastFeedingAdult = allow;
        return true;
    }
    public static bool AllowBreastFeedByAge(this Pawn pawn, Pawn baby) => pawn != baby && (baby.IsAdult() ? pawn.AllowBreastFeedingAdult() : pawn.AllowBreastFeeding());
    public static bool AllowedToBreastFeed(this Pawn pawn, Pawn baby)
    {
        if (baby?.MapHeld == null || pawn?.MapHeld == null || baby.MapHeld != pawn.MapHeld) { return false; } // Not on same map
        try
        {
            // Prevents weird null references
            if (baby.IsForbiddenHeld(pawn)) { return false; }
        }
        catch
        {
            return false;
        }
        if (pawn == baby) { return false; }
        if (!pawn.CanBreastfeedEver(baby)) { return false; }
        if (!SuckleRestrictionAllowed(pawn, baby)) { return false; }
        if (!baby.CompEquallyMilkable().AllowedToBeAutoFedBy(pawn)) { return false; }
        return AllowBreastFeedByAge(pawn, baby);
    }
    /// <summary>指定“谁可以吸我的奶”：产奶者列表 allowedSucklers 非空则仅列表中可吸；空则默认子女+伴侣。</summary>
    private static bool SuckleRestrictionAllowed(Pawn mother, Pawn baby)
    {
        var comp = mother?.CompEquallyMilkable();
        if (comp == null) return true;
        if (comp.allowedSucklers != null && comp.allowedSucklers.Count > 0)
            return comp.allowedSucklers.Contains(baby);
        return IsDefaultSuckler(mother, baby);
    }

    /// <summary>默认“谁可以吸奶”：子女或伴侣。</summary>
    private static bool IsDefaultSuckler(Pawn mother, Pawn baby)
    {
        if (mother?.relations == null || baby?.relations == null) return false;
        return mother.relations.DirectRelationExists(PawnRelationDefOf.Lover, baby)
            || mother.relations.DirectRelationExists(PawnRelationDefOf.Parent, baby)
            || mother.relations.DirectRelationExists(PawnRelationDefOf.Child, baby)
            || (mother.relations.FamilyByBlood != null && mother.relations.FamilyByBlood.Contains(baby))
            || (baby.relations?.FamilyByBlood != null && baby.relations.FamilyByBlood.Contains(mother));
    }

    /// <summary>指定谁可以使用产出的奶/奶制品：无 producer 允许；自己始终允许；否则看产奶者 allowedConsumers，空=仅自己。</summary>
    public static bool CanConsumeMilkProduct(this Pawn consumer, Thing food)
    {
        if (consumer == null || food == null) return true;
        var comp = food.TryGetComp<CompShowProducer>();
        if (comp?.producer == null) return true;
        if (comp.producer == consumer) return true;
        var producerComp = comp.producer.CompEquallyMilkable();
        if (producerComp?.allowedConsumers == null || producerComp.allowedConsumers.Count == 0)
            return false; // 仅产奶者本人
        return producerComp.allowedConsumers.Contains(consumer);
    }
    public static bool AllowedToAutoBreastFeed(this Pawn pawn, Pawn baby)
    {
        if (!pawn.AllowedToBreastFeed(baby)) { return false; }
        if (!baby.CompEquallyMilkable().AllowedToBeAutoFedBy(pawn)) { return false; }
        return true;
    }
    public static bool IsAdult(this Pawn pawn)
    {
        if (pawn.ageTracker == null) { return true; }
        if (pawn.RaceProps.Humanlike)
        {
            return (pawn.ageTracker.CurLifeStage?.developmentalStage ?? DevelopmentalStage.Adult) > DevelopmentalStage.Baby;
        }
        if (pawn.IsNormalAnimal())
        {
            return (pawn.ageTracker.CurLifeStage?.developmentalStage ?? DevelopmentalStage.Adult) > DevelopmentalStage.Baby
                && pawn.ageTracker.AgeBiologicalYearsFloat > EqualMilkingSettings.animalBreastfeed.BabyAge;
        }
        else if (pawn.RaceProps.IsMechanoid)
        {
            return (pawn.ageTracker.CurLifeStage?.developmentalStage ?? DevelopmentalStage.Adult) > DevelopmentalStage.Baby
                && pawn.ageTracker.AgeBiologicalYearsFloat > EqualMilkingSettings.mechanoidBreastfeed.BabyAge;
        }
        return true;
    }
    public static bool CanBreastfeedButNotChildcare(this Pawn pawn, Pawn baby)
    {
        if (pawn.AllowedToAutoBreastFeed(baby) && pawn.IsLactating() // Can breastfeed
            && (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Childcare) || pawn.IsWorkTypeDisabledByAge(WorkTypeDefOf.Childcare, out _))) // But not childcare
        {
            return true;
        }
        return false;
    }
    public static string ChildcareText(this Pawn pawn, Pawn baby)
    {
        if (pawn.CanBreastfeedButNotChildcare(baby))
        {
            return Lang.Breastfeed;
        }
        if (pawn.AllowedToAutoBreastFeed(baby) && pawn.IsLactating())
        {
            return AutofeedMode.Childcare.Translate().CapitalizeFirst() + " & " + Lang.Breastfeed;
        }
        return AutofeedMode.Childcare.Translate().CapitalizeFirst();
    }
    #endregion
    #region Milk
    public static ThingDef MilkDef(this Pawn pawn) => EqualMilkingSettings.GetMilkProductDef(pawn);
    public static float MilkAmount(this Pawn pawn) => EqualMilkingSettings.GetMilkAmount(pawn);
    public static float MilkIntervalDays(this Pawn pawn) => EqualMilkingSettings.GetMilkIntervalDays(pawn);
    public static float MilkGrowthMultiplier(this Pawn pawn) => EqualMilkingSettings.GetMilkGrowthMultiplier(pawn);
    public static float MilkGrowthTime(this Pawn pawn) => pawn.MilkIntervalDays() / EqualMilkingSettings.GetLactatingEfficiencyFactorWithTolerance(pawn);
    public static float MilkPerYear(this Pawn pawn) => 60f / pawn.MilkIntervalDays() * pawn.MilkAmount();
    public static float MilkMarketValue(this Pawn pawn) => pawn.MilkDef()?.BaseMarketValue ?? 0;
    public static float MilkMarketValuePerYear(this Pawn pawn) => pawn.MilkPerYear() * pawn.MilkMarketValue();
    public static bool MilkTypeCanBreastfeed(this Pawn mom) => EqualMilkingSettings.MilkTypeCanBreastfeed(mom);
    public static bool CanBreastfeedEver(this Pawn mom, Pawn baby) => EqualMilkingSettings.CanBreastfeedEver(mom, baby);
    public static HediffComp_EqualMilkingLactating LactatingHediffComp(this Pawn pawn) => pawn.health.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating)?.TryGetComp<HediffComp_EqualMilkingLactating>();
    public static Hediff LactatingHediff(this Pawn pawn) => pawn.health.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating);
    public static HediffWithComps_EqualMilkingLactating LactatingHediffWithComps(this Pawn pawn) => pawn.LactatingHediff() as HediffWithComps_EqualMilkingLactating;
    public static float DistanceTo(this Pawn pawn, Pawn other)
    {
        if (pawn.Map != other.Map || pawn.Map == null || other.Map == null) return float.MaxValue;
        return pawn.Position.DistanceTo(other.Position);
    }
    public static void Set(this CompProperties_Milkable comp, RaceMilkType value)
    {
        comp.milkDef = DefDatabase<ThingDef>.GetNamed(value.milkTypeDefName, true);
        comp.milkAmount = value.milkAmount;
        comp.milkIntervalDays = Mathf.Max(1, (int)value.milkIntervalDays);
        comp.milkFemaleOnly = false;
    }
    public static CompEquallyMilkable CompEquallyMilkable(this ThingWithComps thing)
    {
        if (thing is not Pawn pawn) { return null; }
        CompEquallyMilkable comp = pawn.TryGetComp<CompEquallyMilkable>();
        if (comp == null)
        {
            comp = new CompEquallyMilkable
            {
                parent = pawn,
                props = pawn.def.GetCompProperties<CompProperties_Milkable>() ??
                    new CompProperties_Milkable()
                    {
                        milkDef = pawn.MilkDef() ?? DefDatabase<ThingDef>.GetNamed("Milk", true),
                        milkAmount = (int)pawn.MilkAmount(),
                        milkIntervalDays = Mathf.Max(1, (int)pawn.MilkIntervalDays()),
                        milkFemaleOnly = false
                    }
            };
            pawn.AllComps.Add(comp);
        }
        return comp;
    }
    public static bool IsAdultFemaleAnimalOfColony(this Pawn pawn)
    {
        return pawn.Faction == Faction.OfPlayer
            && pawn.IsNormalAnimal()
            && !(pawn.LactatingHediff()?.Severity >= 1)
            && pawn.gender != Gender.Male
            && pawn.ageTracker.CurLifeStage?.reproductive == true;
    }
    #endregion
}
