using UnityEngine;

namespace MilkCum.Milk.Data;

/// <summary>
/// 水池模型：双池状态与推进逻辑。不持有 Pawn，容量/流速由调用方传入；便于测试与 Comp 职责分离。
/// </summary>
public class LactationPoolState
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
    /// 双池进水：先填满两侧基础容量；仅当两侧都满（≥基础容量）后才允许撑大（至 stretchCap），超出部分溢出。返回本 tick 溢出量。
    /// </summary>
    public float TickGrowth(float flowLeftPerTick, float flowRightPerTick,
        float leftBaseCap, float rightBaseCap, float stretchCapLeft, float stretchCapRight)
    {
        LeftFullness = Mathf.Clamp(LeftFullness, 0f, stretchCapLeft);
        RightFullness = Mathf.Clamp(RightFullness, 0f, stretchCapRight);

        float roomLeftBase = Mathf.Max(0f, leftBaseCap - LeftFullness);
        float roomRightBase = Mathf.Max(0f, rightBaseCap - RightFullness);

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
            if (remainder > 1E-6f)
            {
                float extraLeft = Mathf.Min(remainder, roomLeftBase - addLeft);
                if (extraLeft > 0f) { addLeft += extraLeft; remainder -= extraLeft; }
                if (remainder > 1E-6f)
                    addRight += Mathf.Min(remainder, roomRightBase - addRight);
            }
        }

        float remainderAfterBase = flowLeftPerTick + flowRightPerTick - addLeft - addRight;
        float newLeft = LeftFullness + addLeft;
        float newRight = RightFullness + addRight;

        // 阶段二：仅当两侧都达到基础容量时，才允许撑大（超过基础容量至 stretchCap）
        const float eps = 1E-5f;
        bool bothAtBase = newLeft >= leftBaseCap - eps && newRight >= rightBaseCap - eps;
        if (remainderAfterBase > 1E-6f && bothAtBase)
        {
            float stretchRoomLeft = Mathf.Max(0f, stretchCapLeft - newLeft);
            float stretchRoomRight = Mathf.Max(0f, stretchCapRight - newRight);
            float stretchRoomTotal = stretchRoomLeft + stretchRoomRight;
            if (stretchRoomTotal > 1E-6f)
            {
                float toAddStretch = Mathf.Min(remainderAfterBase, stretchRoomTotal);
                float addLeftStretch = Mathf.Min(toAddStretch * (stretchRoomLeft / stretchRoomTotal), stretchRoomLeft);
                float addRightStretch = Mathf.Min(toAddStretch - addLeftStretch, stretchRoomRight);
                addLeft += addLeftStretch;
                addRight += addRightStretch;
            }
        }

        float overflowTotal = flowLeftPerTick + flowRightPerTick - addLeft - addRight;
        LeftFullness += addLeft;
        RightFullness += addRight;
        return overflowTotal;
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
}
