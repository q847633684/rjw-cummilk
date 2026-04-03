using System.Collections.Generic;
using HarmonyLib;
using MilkCum.Core;
using MilkCum.Fluids.Lactation.Comps;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace MilkCum.Harmony;

/// <summary>
/// 原版 <see cref="Alert_NoBabyFoodCaravan"/> 仅以 <c>IsNutritionGivingIngestibleForHumanlikeBabies</c> 判断车队库存；
/// EM_HumanMilkPartial 的 Stat Nutrition 为 0（食用营养由 <see cref="CompPartialMilk"/> 承担），故不会被认作婴儿食品 → 误报「车队无婴儿食品」。
/// 另对 <see cref="RaceProperties.IsMechanoid"/> 做跳过，避免极端 Mod/种族配置下与婴儿逻辑叠加的误报。
/// 逻辑与 <c>Alert_NoBabyFoodCaravan.LowBabyFoodNutrition</c> 对齐，仅替换库存判定与上述跳过（RimWorld 1.6）。
/// </summary>
[HarmonyPatch(typeof(Alert_NoBabyFoodCaravan), nameof(Alert_NoBabyFoodCaravan.GetReport))]
public static class Alert_NoBabyFoodCaravan_GetReport_Postfix
{
    public static void Postfix(ref AlertReport __result)
    {
        if (!__result.active || __result.culpritsCaravans.NullOrEmpty())
        {
            return;
        }

        List<Caravan> original = __result.culpritsCaravans;
        List<Caravan> stillBad = new List<Caravan>(original.Count);
        foreach (Caravan caravan in original)
        {
            if (CaravanStillNeedsNoBabyFoodAlert(caravan))
            {
                stillBad.Add(caravan);
            }
        }

        if (stillBad.Count == original.Count)
        {
            return;
        }

        if (stillBad.Count == 0)
        {
            __result = new AlertReport { active = false };
            return;
        }

        __result = new AlertReport
        {
            active = true,
            culpritsCaravans = stillBad,
        };
    }

    /// <summary>与原版 <c>LowBabyFoodNutrition</c> 等价，仅 inventory 条件与 mechanoid 跳过不同。</summary>
    private static bool CaravanStillNeedsNoBabyFoodAlert(Caravan caravan)
    {
        bool babyNeedsExternalFood = false;
        for (int i = 0; i < caravan.PawnsListForReading.Count; i++)
        {
            Pawn pawn = caravan.PawnsListForReading[i];
            if (pawn.RaceProps.IsMechanoid)
            {
                continue;
            }

            if (!ChildcareUtility.CanSuckle(pawn, out _))
            {
                continue;
            }

            ChildcareUtility.BreastfeedFailReason? reason2;
            Predicate<Pawn, Pawn> feederPredicate = (Pawn _baby, Pawn _mom) =>
                ChildcareUtility.CanMomBreastfeedBaby(_mom, _baby, out reason2);
            if (!pawn.mindState.AnyAutofeeder(AutofeedMode.Urgent, feederPredicate, caravan.PawnsListForReading)
                && !pawn.mindState.AnyAutofeeder(AutofeedMode.Childcare, feederPredicate, caravan.PawnsListForReading))
            {
                babyNeedsExternalFood = true;
            }
        }

        if (!babyNeedsExternalFood)
        {
            return false;
        }

        return !CaravanInventoryHasEdibleBabyFood(caravan);
    }

    private static bool CaravanInventoryHasEdibleBabyFood(Caravan caravan)
    {
        foreach (Thing thing in CaravanInventoryUtility.AllInventoryItems(caravan))
        {
            if (thing.def.IsNutritionGivingIngestibleForHumanlikeBabies)
            {
                return true;
            }

            if (MilkCumDefOf.EM_HumanMilkPartial != null
                && thing.def == MilkCumDefOf.EM_HumanMilkPartial
                && thing.TryGetComp<CompPartialMilk>() is CompPartialMilk pm
                && pm.fillAmount > 0.0001f
                && thing.def.ingestible != null
                && thing.def.ingestible.HumanEdible
                && thing.def.ingestible.babiesCanIngest)
            {
                return true;
            }
        }

        return false;
    }
}
