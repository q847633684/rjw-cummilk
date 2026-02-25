using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using EqualMilking;

namespace EqualMilking.RJW;

/// <summary>哺乳后性行为额外满足 Need_Sex；泌乳期怀孕概率乘数；性互动中泌乳状态（Thought）。</summary>
public static class RJWSexAndFertility
{
    private const float SexSatisfactionBonusAfterNursing = 0.06f;

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
