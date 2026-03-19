using MilkCum.Core.Constants;
using UnityEngine;

namespace MilkCum.Fluids.Shared.Data;

/// <summary>双池基础容量与撑大容量，用于 TickGrowth 参数封装。</summary>
public readonly struct BreastPairCapacities
{
    public float LeftBase { get; }
    public float RightBase { get; }
    public float LeftStretch { get; }
    public float RightStretch { get; }

    public BreastPairCapacities(float leftBase, float rightBase, float leftStretch, float rightStretch)
    {
        LeftBase = leftBase;
        RightBase = rightBase;
        LeftStretch = leftStretch;
        RightStretch = rightStretch;
    }
}

/// <summary>
/// 体液池状态（乳汁/精液共享）：双池状态与推进逻辑。不持有 Pawn，容量/流速由调用方传入。
/// </summary>
public class FluidPoolState
{
    public float LeftFullness;
    public float RightFullness;
    public int TicksFullPool;

    public float TotalFullness => LeftFullness + RightFullness;

    public void SetFrom(float left, float right, int ticksFull)
    {
        LeftFullness = left;
        RightFullness = right;
        TicksFullPool = ticksFull;
    }

    /// <summary>
    /// 双池进水（纯计算）：先填满两侧基础容量，仅当两侧都满后才允许撑大，超出部分溢出。返回 (newLeft, newRight, overflow)，不修改 LeftFullness/RightFullness，由调用方写回并做回缩。
    /// </summary>
    public (float newLeft, float newRight, float overflow) TickGrowth(float flowLeftPerTick, float flowRightPerTick, BreastPairCapacities cap)
    {
        float left = Mathf.Clamp(LeftFullness, 0f, cap.LeftStretch);
        float right = Mathf.Clamp(RightFullness, 0f, cap.RightStretch);
        float roomLeftBase = Mathf.Max(0f, cap.LeftBase - left);
        float roomRightBase = Mathf.Max(0f, cap.RightBase - right);

        float addLeft;
        float addRight;

        // 阶段一：只填到基础容量（可把一侧满后富余的流速加到另一侧）
        if (roomLeftBase <= 0f && roomRightBase <= 0f)
        {
            addLeft = 0f;
            addRight = 0f;
        }
        else if (roomLeftBase <= 0f)
        {
            addLeft = 0f;
            addRight = Mathf.Min(flowLeftPerTick + flowRightPerTick, roomRightBase);
        }
        else if (roomRightBase <= 0f)
        {
            addLeft = Mathf.Min(flowLeftPerTick + flowRightPerTick, roomLeftBase);
            addRight = 0f;
        }
        else
        {
            float totalFlow = flowLeftPerTick + flowRightPerTick;
            addLeft = Mathf.Min(flowLeftPerTick, roomLeftBase);
            addRight = Mathf.Min(flowRightPerTick, roomRightBase);
            float remainder = totalFlow - addLeft - addRight;
            if (remainder > PoolModelConstants.Epsilon)
            {
                float roomLeftRemain = roomLeftBase - addLeft;
                float roomRightRemain = roomRightBase - addRight;
                float roomTotal = roomLeftRemain + roomRightRemain;
                if (roomTotal > PoolModelConstants.Epsilon)
                {
                    float fracLeft = roomLeftRemain / roomTotal;
                    float addLeftRemainder = Mathf.Min(remainder * fracLeft, roomLeftRemain);
                    float addRightRemainder = Mathf.Min(remainder - addLeftRemainder, roomRightRemain);
                    addLeft += addLeftRemainder;
                    addRight += addRightRemainder;
                }
                else if (roomLeftRemain > PoolModelConstants.Epsilon)
                    addLeft += Mathf.Min(remainder, roomLeftRemain);
                else if (roomRightRemain > PoolModelConstants.Epsilon)
                    addRight += Mathf.Min(remainder, roomRightRemain);
            }
        }

        float remainderAfterBase = flowLeftPerTick + flowRightPerTick - addLeft - addRight;
        float newLeft = left + addLeft;
        float newRight = right + addRight;

        // 阶段二：仅当两侧都达到基础容量时，才允许撑大。用阶段一后的 newLeft/newRight 判断，避免「本 tick 刚好补满」仍不触发 stretch、导致撑大难而溢出多。
        float eps = PoolModelConstants.DisplayEpsilon;
        bool bothAtBase = newLeft >= cap.LeftBase - eps && newRight >= cap.RightBase - eps;
        if (remainderAfterBase > PoolModelConstants.Epsilon && bothAtBase)
        {
            float stretchRoomLeft = Mathf.Max(0f, cap.LeftStretch - newLeft);
            float stretchRoomRight = Mathf.Max(0f, cap.RightStretch - newRight);
            float stretchRoomTotal = stretchRoomLeft + stretchRoomRight;
            if (stretchRoomTotal > PoolModelConstants.Epsilon)
            {
                float toAddStretch = Mathf.Min(remainderAfterBase, stretchRoomTotal);
                float addLeftStretch = Mathf.Min(toAddStretch * (stretchRoomLeft / stretchRoomTotal), stretchRoomLeft);
                float addRightStretch = Mathf.Min(toAddStretch - addLeftStretch, stretchRoomRight);
                addLeft += addLeftStretch;
                addRight += addRightStretch;
            }
        }

        float overflowTotal = flowLeftPerTick + flowRightPerTick - addLeft - addRight;
        float resultLeft = left + addLeft;
        float resultRight = right + addRight;
        return (resultLeft, resultRight, Mathf.Max(0f, overflowTotal));
    }

    /// <summary>
    /// 排水后回缩：超出基础容量部分乘以 shrinkFactor（0~1），shrinkFactor 越小回缩越快；可含健康度系数。
    /// </summary>
    public void TickShrink(float leftBaseCap, float rightBaseCap, float shrinkFactor)
    {
        if (LeftFullness > leftBaseCap)
            LeftFullness = leftBaseCap + (LeftFullness - leftBaseCap) * shrinkFactor;
        if (RightFullness > rightBaseCap)
            RightFullness = rightBaseCap + (RightFullness - rightBaseCap) * shrinkFactor;
    }

    /// <summary>
    /// 更新连续满池 tick 计数：总水位 >= 阈值则累加 deltaTicks，否则置 0。
    /// </summary>
    public void UpdateFullPoolCounter(float fullThreshold, int deltaTicks)
    {
        if (TotalFullness >= fullThreshold)
            TicksFullPool += deltaTicks;
        else
            TicksFullPool = 0;
    }

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
