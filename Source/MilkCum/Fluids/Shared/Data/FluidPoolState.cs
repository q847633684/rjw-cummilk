using MilkCum.Core.Constants;
using UnityEngine;

namespace MilkCum.Fluids.Shared.Data;

/// <summary>
/// 体液池状态（乳汁/精液共享）：当前仅保留单池进水纯计算工具。
/// </summary>
public class FluidPoolState
{
    /// <summary>单乳池进水：当前水位 + 本 tick 流速，先填至 baseCap 再允许撑大至 stretchCap，超出部分溢出。返回 (新水位, 本 tick 溢出量)。</summary>
    public static (float newFullness, float overflow) SingleBreastTickGrowth(float currentFullness, float flowPerTick, float baseCap, float stretchCap)
    {
        currentFullness = Mathf.Clamp(currentFullness, 0f, stretchCap);
        float roomBase = Mathf.Max(0f, baseCap - currentFullness);
        float add = Mathf.Min(flowPerTick, roomBase);
        float remainder = flowPerTick - add;
        float newVal = currentFullness + add;
        float stretchRoom = Mathf.Max(0f, stretchCap - newVal);
        float addStretch = Mathf.Min(remainder, stretchRoom);
        float overflow = flowPerTick - add - addStretch;
        return (newVal + addStretch, overflow);
    }
}
