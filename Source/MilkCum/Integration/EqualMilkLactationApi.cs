using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Integration;

/// <summary>
/// 第三方模组接入 TianTian.MilkCum 乳池的推荐入口：比直接写 <c>CompMilkable.fullness</c> 或 <c>HediffComp_Lactating.Charge</c> 更完整
/// （设置里可开启对二者的自动桥接；仍建议优先调用本 API，并优先用 <see cref="TryDrainPoolForConsume"/> 走导管逻辑）。
/// </summary>
public static class EqualMilkLactationApi
{
    /// <summary>取得小人身上的 <see cref="CompEquallyMilkable"/>（无则 null）。</summary>
    public static CompEquallyMilkable TryGetMilkPoolComp(Pawn pawn) => pawn?.CompEquallyMilkable();

    /// <summary>将当前泌乳 Hediff 的 Charge 与乳池总满度对齐，并视降幅调用 <c>OnGathered</c>。</summary>
    public static void SyncLactatingChargeFromPool(Pawn pawn) => pawn?.LactatingHediffComp()?.SyncChargeFromPool();

    /// <summary>
    /// 将乳池总目标设为 <paramref name="targetPoolUnits"/>（池单位，与 UI/Charge 同单位），按比例分摊到各乳池 key；
    /// 联动排空炎症缓解、短时进水突发、<see cref="SyncLactatingChargeFromPool"/>。
    /// </summary>
    public static bool TryApplyTotalPoolTarget(Pawn pawn, float targetPoolUnits)
    {
        var comp = TryGetMilkPoolComp(pawn);
        if (comp == null) return false;
        float stretch = Mathf.Max(0.01f, comp.GetPoolStretchCapacityTotal());
        comp.ApplyExternalTotalTarget(targetPoolUnits, stretch, "EqualMilkLactationApi.TryApplyTotalPoolTarget");
        return true;
    }

    /// <summary>
    /// 按游戏内「手挤」规则从乳池扣量（导管压力/炎症阻力等）：与玩家挤奶一致；扣后同步 Charge。
    /// 若需无视导管、纯比例改变总量，请用 <see cref="TryApplyTotalPoolTarget"/>。
    /// </summary>
    /// <returns>实际扣掉的池单位量。</returns>
    public static float TryDrainPoolForConsume(Pawn pawn, float amountPoolUnits)
    {
        if (amountPoolUnits <= 0f || pawn == null) return 0f;
        var comp = TryGetMilkPoolComp(pawn);
        if (comp == null) return 0f;
        float drained = comp.DrainForConsume(amountPoolUnits, null, false);
        if (drained > 0f)
            SyncLactatingChargeFromPool(pawn);
        return drained;
    }
}
