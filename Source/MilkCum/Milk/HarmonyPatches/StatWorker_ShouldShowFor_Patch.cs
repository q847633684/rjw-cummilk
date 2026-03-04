using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MilkCum.Milk.HarmonyPatches;

/// <summary>
/// 信息卡统计 NRE 全局防护。StatsReportUtility.StatsToDraw 会枚举分类下所有 StatDef 并调用 Worker.ShouldShowFor(req)。
/// 任一 StatWorker（原版或 mod）在 req 无 Pawn、或 Pawn 下某属性为 null 时都可能 NRE，导致列表错位与 ArgumentOutOfRangeException。
/// Prefix：req.Pawn == null 时直接返回 false 并跳过原方法。
/// Finalizer：原方法或其它 patch 抛出的任何异常都吞掉并设 __result = false，避免崩溃；该 stat 仅不显示。
/// 见 ADR-004、报错修复记录。
/// </summary>
[HarmonyPatch(typeof(StatWorker))]
[HarmonyPatch(nameof(StatWorker.ShouldShowFor))]
public static class StatWorker_ShouldShowFor_Patch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(StatRequest req, ref bool __result)
    {
        if (req.Pawn == null)
        {
            __result = false;
            return false;
        }
        return true;
    }

    /// <summary>兜底：ShouldShowFor 内任意 NRE/异常都视为「不显示」并吞掉，避免信息卡崩溃。</summary>
    [HarmonyFinalizer]
    public static Exception Finalizer(Exception __exception, ref bool __result)
    {
        if (__exception != null)
        {
            __result = false;
            return null; // 吞掉异常，不向上抛
        }
        return null;
    }
}
