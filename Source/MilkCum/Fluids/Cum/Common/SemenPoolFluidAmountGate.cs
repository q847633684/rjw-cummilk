using System;
using rjw;

namespace MilkCum.Fluids.Cum.Common;

/// <summary>口交 <see cref="rjw.SexUtility.IngestFluids"/> 期间将 <see cref="HediffComp_SexPart.GetFluidAmount"/> 替换为池扣减后的实际量。</summary>
public static class SemenPoolFluidAmountGate
{
    [ThreadStatic] private static HediffComp_SexPart gatedComp;
    [ThreadStatic] private static float gatedValue;

    public static void Enter(HediffComp_SexPart comp, float amount)
    {
        gatedComp = comp;
        gatedValue = amount;
    }

    public static void Exit()
    {
        gatedComp = null;
        gatedValue = 0f;
    }

    public static bool TryGetOverride(HediffComp_SexPart comp, out float value)
    {
        if (gatedComp != comp)
        {
            value = default;
            return false;
        }

        value = gatedValue;
        return true;
    }
}
