using HarmonyLib;
using Verse;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using System.Linq;
using rjw;
using MilkCum.Core;
using MilkCum.Milk.Helpers;
using MilkCum.Milk.Comps;
using MilkCum.Milk.HarmonyPatches;

namespace MilkCum.RJW;

// 重要：此处禁止使用 Harmony.PatchAll()。PatchAll 会扫描整个程序集并处理 Hediff_TipString_BreastPool_Patch，
// 而该 patch 的目标 Hediff.get_TipString 在部分 RimWorld 版本中不存在，会导致 TypeInitializationException。
// 若你修改了本文件，请务必重新编译并将新 MilkCum.dll 复制到 Mod 的 Assemblies 与 Versions/1.6/Assemblies。
[StaticConstructorOnStartup]
internal static class ApplyPatches
{
    private static readonly Harmony Harmony;

    static ApplyPatches()
    {
        Harmony = new Harmony("com.akaster.rimworld.mod.equalmilking.rjw");
        Log.Message("[Equal Milking]: RJW Loaded, Patching...");
        if (AccessTools.TypeByName("SexFluidDef") == null)
        {
            Log.Warning("[Equal Milking]: RJW version too old, aborting.");
            return;
        }
        // 只处理本命名空间内的 patch 类，避免 PatchAll 扫描整个程序集时误处理 Hediff_TipString_BreastPool_Patch（目标 get_TipString 在部分版本不存在）
        new PatchClassProcessor(Harmony, typeof(CompAssignableToPawn_Box_Patch)).Patch();
        new PatchClassProcessor(Harmony, typeof(Hediff_BasePregnancy_Patch)).Patch();
    }
}
[HarmonyPatch(typeof(ExtensionHelper))]
public static class CompAssignableToPawn_Box_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ExtensionHelper.MilkAmount))]
    public static void GetMilkAmount_Postfix(Pawn pawn, ref float __result)
    {
        IEnumerable<ISexPartHediff> breasts = pawn.GetBreasts();
        if (!breasts.EnumerableNullOrEmpty())
        {
            // Multiplier
            float multiplier = breasts.Select(b => b.GetPartComp().FluidMultiplier).Sum();
            if (multiplier == 0)
            {
                Alert_FluidMultiplier.Culprits.Add(pawn);
            }
            else
            {
                Alert_FluidMultiplier.Culprits.Remove(pawn);
            }
            __result *= multiplier;
        }
    }
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ExtensionHelper.MilkDef))]
    public static bool GetMilkProductDef_Prefix(Pawn pawn, ref ThingDef __result)
    {
        __result = null;
        IEnumerable<ISexPartHediff> breasts = pawn.GetBreasts();
        if (!breasts.EnumerableNullOrEmpty())
        {
            foreach (ISexPartHediff breast in breasts)
            {
                if (breast.GetPartComp()?.Fluid?.consumable is ThingDef consumable && consumable.defName != "Milk")
                {
                    __result = consumable;
                    return false;
                }
            }
        }
        return true;
    }
}
[HarmonyPatch(typeof(Hediff_BasePregnancy))]
public static class Hediff_BasePregnancy_Patch
{
    /// <summary>RJW 分娩：与原版 DoBirthSpawn 一致，为母亲加 Lactating 并统一调用 ApplyBirthPoolValues（设计原则 2：分娩入口统一）。</summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Hediff_BasePregnancy.PostBirth))]
    public static void PostBirth_Prefix(Pawn mother, Pawn baby)
    {
        if (mother.IsMilkable())
        {
            Hediff lactating = mother.health.GetOrAddHediff(HediffDefOf.Lactating, mother.GetBreastOrChestPart());
            if (lactating != null)
            {
                lactating.Severity = Mathf.Max(lactating.Severity, 0.9999f);
            }
            PoolModelBirthHelper.ApplyBirthPoolValues(mother);
            ExtensionHelper.TryGiveFirstLactationBirthMemory(mother);
        }
        if (mother.IsInLactatingState() && mother.CompEquallyMilkable() is CompEquallyMilkable comp && mother.CanBreastfeedEver(baby))
        {
            // 谁可以使用我的奶：名单默认预填子女+伴侣
        }
    }
}
