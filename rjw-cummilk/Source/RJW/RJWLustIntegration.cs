using System;
using HarmonyLib;
using RimWorld;
using Verse;
using EqualMilking.Helpers;

namespace EqualMilking.RJW;

/// <summary>哺乳/吸奶时增加 RJW 性需求 (Need_Sex.CurLevel)；泌乳期对性需求有持续小幅正向影响。</summary>
public static class RJWLustIntegration
{
    private const float SexNeedPerSuckleTick = 0.006f;
    private const float SexNeedPerBreastfeedComplete = 0.08f;
    private const float SexNeedLactatingBonusPerInterval = 0.004f;

    /// <summary>他人吸奶/被哺乳：每次 SuckleFromLactatingPawn 成功时给母亲与婴儿增加一点性需求。</summary>
    public static void OnSuckleFromLactatingPawn(Pawn feeder, Pawn baby)
    {
        if (feeder == null || !EqualMilkingSettings.rjwLustFromNursingEnabled) return;
        AddSexNeed(feeder, SexNeedPerSuckleTick);
        if (baby != null)
            AddSexNeed(baby, SexNeedPerSuckleTick);
    }

    /// <summary>一次完整哺乳结束时增加性需求，并记录“刚哺乳”用于性行为额外满足。</summary>
    public static void OnBreastfeedComplete(Pawn mother, Pawn baby)
    {
        if (mother == null) return;
        if (EqualMilkingSettings.rjwLustFromNursingEnabled)
        {
            AddSexNeed(mother, SexNeedPerBreastfeedComplete);
            if (baby != null) AddSexNeed(baby, SexNeedPerBreastfeedComplete);
        }
        if (EqualMilkingSettings.rjwSexSatisfactionAfterNursingEnabled)
        {
            int now = Find.TickManager.TicksGame;
            LastNursedTick[mother] = now;
            if (baby != null) LastNursedTick[baby] = now;
        }
    }

    /// <summary>刚哺乳/吸奶的 tick 记录，用于性行为结束时额外满足 Need_Sex。</summary>
    public static readonly System.Collections.Generic.Dictionary<Pawn, int> LastNursedTick = new();
    private const int RecentlyNursedTicks = 2500; // ~1 小时
    public static bool WasRecentlyNursed(Pawn p) => p != null && LastNursedTick.TryGetValue(p, out int t) && Find.TickManager.TicksGame - t < RecentlyNursedTicks;

    public static void AddSexNeed(Pawn pawn, float amount)
    {
        if (pawn?.needs == null || amount <= 0f) return;
        Type needSexType = AccessTools.TypeByName("rjw.Need_Sex");
        if (needSexType == null) return;
        Need need = pawn.needs.TryGetNeed(needSexType);
        if (need == null) return;
        float cur = need.CurLevel;
        need.CurLevel = Mathf.Clamp01(cur + amount);
    }

    /// <summary>泌乳期时每 NeedInterval 给性需求加一点（减缓衰减/略提升）。</summary>
    public static void ApplyLactatingSexNeedBonus(Pawn pawn, Need need)
    {
        if (pawn == null || need == null || !EqualMilkingSettings.rjwSexNeedLactatingBonusEnabled || !pawn.IsInLactatingState()) return;
        float cur = need.CurLevel;
        need.CurLevel = Mathf.Clamp01(cur + SexNeedLactatingBonusPerInterval);
    }
}

[HarmonyPatch]
public static class ChildcareHelper_SuckleFromLactatingPawn_Patch
{
    static System.Reflection.MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("EqualMilking.Helpers.ChildcareHelper"), "SuckleFromLactatingPawn");
    }

    [HarmonyPostfix]
    static void Postfix(bool __result, Pawn baby, Pawn feeder)
    {
        if (!__result || feeder == null) return;
        RJWLustIntegration.OnSuckleFromLactatingPawn(feeder, baby);
    }
}

[HarmonyPatch]
public static class ChildcareHelper_Breastfeed_Patch
{
    static System.Reflection.MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("EqualMilking.Helpers.ChildcareHelper"), "Breastfeed");
    }

    [HarmonyPostfix]
    static void Postfix(ref Verse.AI.Toil __result, Pawn pawn, Pawn baby)
    {
        if (__result == null || pawn == null) return;
        __result.AddFinishAction(() => RJWLustIntegration.OnBreastfeedComplete(pawn, baby));
    }
}

[HarmonyPatch]
public static class Need_Sex_NeedInterval_Patch
{
    static System.Reflection.MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("rjw.Need_Sex"), "NeedInterval");
    }

    [HarmonyPostfix]
    static void Postfix(Need __instance)
    {
        if (__instance?.pawn == null) return;
        RJWLustIntegration.ApplyLactatingSexNeedBonus(__instance.pawn, __instance);
    }
}
