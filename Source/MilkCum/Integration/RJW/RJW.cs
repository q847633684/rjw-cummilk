using HarmonyLib;
using Verse;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using System.Linq;
using rjw;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Harmony;

namespace MilkCum.RJW;

// 只处理本命名空间内的 patch 类，避免 PatchAll 扫描整个程序集
// 若你修改了本文件，请务必重新编译并将新 MilkCum.dll 复制到 Mod 的 Assemblies 与 Versions/1.6/Assemblies。
[StaticConstructorOnStartup]
internal static class ApplyPatches
{
    private static readonly HarmonyLib.Harmony Harmony;

    static ApplyPatches()
    {
        Harmony = new HarmonyLib.Harmony("com.akaster.rimworld.mod.equalmilking.rjw");
        Log.Message("[MilkCum]: RJW Loaded, Patching...");
        if (AccessTools.TypeByName("SexFluidDef") == null)
        {
            Log.Warning("[MilkCum]: RJW version too old, aborting.");
            return;
        }
        // 只处理本命名空间内的 patch 类
        new PatchClassProcessor(Harmony, typeof(CompAssignableToPawn_Box_Patch)).Patch();
        new PatchClassProcessor(Harmony, typeof(Hediff_BasePregnancy_Patch)).Patch();
        new PatchClassProcessor(Harmony, typeof(JobDriver_Sex_OrgasmMilk_Patch)).Patch();
    }
}
[HarmonyPatch(typeof(PawnMilkPoolExtensions))]
public static class CompAssignableToPawn_Box_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PawnMilkPoolExtensions.MilkAmount))]
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
    [HarmonyPatch(nameof(PawnMilkPoolExtensions.MilkDef))]
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
            MilkCumSettings.LactationLog($"RJW PostBirth lactation: {mother.Name}");
            MilkPermissionExtensions.TryGiveFirstLactationBirthMemory(mother);
        }
        if (mother.IsInLactatingState() && mother.CompEquallyMilkable() is CompEquallyMilkable comp && mother.CanBreastfeedEver(baby))
        {
            // 谁可以使用我的奶：名单默认预填子女+伴侣
        }
    }
}

/// <summary>RJW produceFluidOnOrgasm：高潮时若该角色泌乳且乳房 Def 标记 produceFluidOnOrgasm，则向对应乳池追加少量奶量。</summary>
[HarmonyPatch(typeof(JobDriver_Sex), nameof(JobDriver_Sex.Orgasm))]
public static class JobDriver_Sex_OrgasmMilk_Patch
{
    private const float PoolUnitsPerOrgasmPerBreastSide = 0.05f;

    [HarmonyPostfix]
    public static void Postfix(JobDriver_Sex __instance)
    {
        Pawn pawn = __instance?.pawn;
        if (pawn == null || pawn.Dead) return;
        if (!pawn.IsLactating()) return;
        var comp = pawn.CompEquallyMilkable();
        if (comp == null) return;
        if (!MilkCumSettings.rjwBreastSizeEnabled) return;
        var list = pawn.GetBreastList();
        if (list == null || list.Count == 0) return;
        var entries = pawn.GetBreastPoolEntries();
        if (entries == null || entries.Count < 2 * list.Count) return;
        var toAdd = new List<(string key, float addAmount, float cap)>();
        for (int i = 0; i < list.Count; i++)
        {
            var h = list[i];
            if (h?.def is not HediffDef_SexPart def || !def.produceFluidOnOrgasm) continue;
            int idxL = 2 * i;
            int idxR = 2 * i + 1;
            if (idxR >= entries.Count) break;
            float density = PawnMilkPoolExtensions.GetBreastDensity(h.def);
            float amount = PoolUnitsPerOrgasmPerBreastSide * density;
            toAdd.Add((entries[idxL].Key, amount, entries[idxL].Capacity));
            toAdd.Add((entries[idxR].Key, amount, entries[idxR].Capacity));
        }
        if (toAdd.Count > 0)
            comp.AddMilkToKeys(toAdd);
    }
}
