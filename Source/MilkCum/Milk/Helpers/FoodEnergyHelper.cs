using MilkCum.Core;
using RimWorld;
using Verse;

namespace MilkCum.Milk.Helpers;

public static class FoodEnergyHelper
{
    public static bool IsHungryOrLowEnergy(this Pawn pawn)
    {
        if (pawn == null || pawn.needs == null || pawn.needs.food == null)
        {
            return false;
        }
        return FeedPatientUtility.IsHungry(pawn) || (pawn.needs?.energy?.CurLevelPercentage ?? float.MaxValue) < 0.2f;
    }
    public static bool IsSlightlyHungryOrLowEnergy(this Pawn pawn)
    {
        if (pawn == null || pawn.needs == null || pawn.needs.food == null)
        {
            return false;
        }
        return (pawn.needs?.food?.CurLevelPercentage ?? float.MaxValue) < 0.6f || (pawn.needs?.energy?.CurLevelPercentage ?? float.MaxValue) < 0.6f;
    }
    public static bool IsConsideredHungryForMom(this Pawn pawn, Pawn mom)
    {
        if (mom.RaceProps.Humanlike)
        {
            return pawn.IsHungryOrLowEnergy();
        }
        else
        {
            return pawn.IsSlightlyHungryOrLowEnergy();
        }
    }
    public static float TryConsumeBreastMilk(this Pawn baby, HediffComp_Chargeable hediffComp, float amount)
    {
        float consumed;
        if (baby.needs.food != null)
        {
            consumed = hediffComp.GreedyConsume(amount);
            baby.needs.food.CurLevel += consumed;
        }
        else if (baby.needs.energy != null)
        {
            consumed = hediffComp.GreedyConsume(amount / EqualMilkingSettings.nutritionToEnergyFactor) * EqualMilkingSettings.nutritionToEnergyFactor;
            baby.needs.energy.CurLevel += consumed;
        }
        else
        {
            consumed = 0f;
        }
        return consumed;
    }
    public static bool TryGetFoodOrEnergyNeed(this Pawn pawn, out float wanted, out float maxLevel)
    {
        if (pawn.needs.food != null)
        {
            wanted = pawn.needs.food.NutritionWanted;
            maxLevel = pawn.needs.food.MaxLevel;
        }
        else if (pawn.needs.energy != null)
        {
            wanted = pawn.needs.energy.MaxLevel - pawn.needs.energy.CurLevel;
            maxLevel = pawn.needs.energy.MaxLevel;
        }
        else
        {
            wanted = 0f;
            maxLevel = 0f;
            return false;
        }
        return true;
    }
}