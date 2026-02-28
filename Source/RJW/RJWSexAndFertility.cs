using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using MilkCum.Core;
using MilkCum.Milk.Comps;

namespace MilkCum.RJW;

/// <summary>哺乳后性行为额外满足 Need_Sex；泌乳期怀孕概率乘数；性互动中泌乳状态（Thought）。3.2：性行为后可选增加少量泌乳池进水。</summary>
public static class RJWSexAndFertility
{
    private const float SexSatisfactionBonusAfterNursing = 0.06f;

    /// <summary>3.2：性行为后为泌乳者增加少量池进水（若设置开启）。</summary>
    public static void ApplyPostSexLactationBoost(Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null || !EqualMilkingSettings.rjwSexAddsLactationBoost
            || EqualMilkingSettings.rjwSexLactationBoostDeltaS <= 0f) return;
        var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) as HediffWithComps;
        if (hediff?.comps == null) return;
        foreach (var c in hediff.comps)
        {
            if (c is HediffComp_EqualMilkingLactating comp)
            {
                float deltaS = EqualMilkingSettings.rjwSexLactationBoostDeltaS * EqualMilkingSettings.GetProlactinToleranceFactor(pawn) * EqualMilkingSettings.GetRaceDrugDeltaSMultiplier(pawn);
                comp.AddFromDrug(deltaS);
                break;
            }
        }
    }

    public static void GiveSexSatisfactionAfterNursing(Pawn pawn)
    {
        if (pawn?.needs == null || !EqualMilkingSettings.rjwSexSatisfactionAfterNursingEnabled) return;
        if (!RJWLustIntegration.WasRecentlyNursed(pawn)) return;
        RJWLustIntegration.AddSexNeed(pawn, SexSatisfactionBonusAfterNursing);
    }
}

[HarmonyPatch]
public static class JobDriver_Sex_End_Patch
{
    static System.Reflection.MethodBase TargetMethod()
    {
        var t = AccessTools.TypeByName("rjw.JobDriver_SexBaseInitiator");
        if (t == null) return null;
        return AccessTools.Method(t, "End");
    }

    [HarmonyPostfix]
    static void Postfix(Verse.AI.JobDriver __instance)
    {
        Pawn initiator = __instance.pawn;
        Pawn partner = (Pawn)AccessTools.Property(__instance.GetType(), "Partner")?.GetValue(__instance);
        RJWSexAndFertility.GiveSexSatisfactionAfterNursing(initiator);
        if (partner != null) RJWSexAndFertility.GiveSexSatisfactionAfterNursing(partner);
        RJWSexAndFertility.ApplyPostSexLactationBoost(initiator);
        if (partner != null) RJWSexAndFertility.ApplyPostSexLactationBoost(partner);
        if (EqualMilkingSettings.rjwLactatingInSexDescriptionEnabled && EMDefOf.EM_HadSexWhileLactating != null)
        {
            if (initiator?.needs?.mood?.thoughts?.memories != null && initiator.IsInLactatingState())
                initiator.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_HadSexWhileLactating);
            if (partner?.needs?.mood?.thoughts?.memories != null && partner.IsInLactatingState())
                partner.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_HadSexWhileLactating);
        }
    }
}

[HarmonyPatch]
public static class PawnCapacityWorker_Fertility_Lactating_Patch
{
    static System.Reflection.MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("rjw.PawnCapacityWorker_Fertility"), "CalculateCapacityLevel");
    }

    [HarmonyPostfix]
    static void Postfix(Pawn pawn, ref float __result)
    {
        if (pawn == null || __result <= 0f) return;
        if (!pawn.IsInLactatingState()) return;
        float factor = Mathf.Clamp01(EqualMilkingSettings.rjwLactationFertilityFactor);
        __result *= factor;
    }
}
