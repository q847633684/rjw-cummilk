using HarmonyLib;
using MilkCum.Core;
using MilkCum.Fluids.Lactation.Comps;
using RimWorld;
using Verse;

namespace MilkCum.Harmony;

/// <summary>
/// 原版 <see cref="ResourceCounter.TotalHumanBabyEdibleNutrition"/> 只对满足
/// <c>IsNutritionGivingIngestibleForHumanlikeBabies</c> 的 <see cref="ThingDef"/> 按抽象营养统计；
/// <see cref="MilkCumDefOf.EM_HumanMilkPartial"/> 的 Nutrition 为 0，实际可食营养在 <see cref="CompPartialMilk.fillAmount"/>。
/// 导致 <see cref="Alert_LowBabyFood"/> 等资源读数低估 → 误报「殖民地婴儿食品不足」。
/// 在 getter 末尾按与 <c>ResourceCounter.UpdateResourceCounts</c> 相同的格子范围与 <c>ShouldCount</c> 规则累加未满瓶人奶营养。
/// </summary>
[HarmonyPatch(typeof(ResourceCounter), nameof(ResourceCounter.TotalHumanBabyEdibleNutrition), MethodType.Getter)]
public static class ResourceCounter_TotalHumanBabyEdibleNutrition_Postfix
{
    public static void Postfix(ResourceCounter __instance, ref float __result)
    {
        if (MilkCumDefOf.EM_HumanMilkPartial == null)
        {
            return;
        }

        Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
        if (map == null)
        {
            return;
        }

        __result += SumCountedPartialHumanMilkNutrition(map);
    }

    /// <summary>与 <c>ResourceCounter.ShouldCount</c> 一致，避免计入腐烂/黑雾内物品。</summary>
    private static bool ResourceCounterShouldCount(Thing t)
    {
        if (t.IsNotFresh())
        {
            return false;
        }

        if (t.SpawnedOrAnyParentSpawned && t.PositionHeld.Fogged(t.MapHeld))
        {
            return false;
        }

        return true;
    }

    private static float SumCountedPartialHumanMilkNutrition(Map map)
    {
        float sum = 0f;
        ThingDef partialDef = MilkCumDefOf.EM_HumanMilkPartial;
        var groups = map.haulDestinationManager.AllGroupsListForReading;
        for (int i = 0; i < groups.Count; i++)
        {
            foreach (Thing held in groups[i].HeldThings)
            {
                Thing inner = held.GetInnerIfMinified();
                if (inner.def != partialDef || !inner.def.CountAsResource || !ResourceCounterShouldCount(inner))
                {
                    continue;
                }

                if (inner.def.ingestible == null || !inner.def.ingestible.HumanEdible || !inner.def.ingestible.babiesCanIngest)
                {
                    continue;
                }

                if (inner.TryGetComp<CompPartialMilk>() is CompPartialMilk pm && pm.fillAmount > 0.0001f)
                {
                    sum += pm.fillAmount * inner.stackCount;
                }
            }
        }

        return sum;
    }
}
