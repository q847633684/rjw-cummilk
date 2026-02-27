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
    /// 双池进水：按左右流速填充，满则溢出。返回本 tick 溢出量（未填入池的部分）。
    /// </summary>
    public float TickGrowth(float flowLeftPerTick, float flowRightPerTick,
        float stretchCapLeft, float stretchCapRight)
    {
        LeftFullness = Mathf.Clamp(LeftFullness, 0f, stretchCapLeft);
        RightFullness = Mathf.Clamp(RightFullness, 0f, stretchCapRight);
        float roomLeft = stretchCapLeft - LeftFullness;
        float roomRight = stretchCapRight - RightFullness;
        float roomTotal = roomLeft + roomRight;
        float addLeft;
        float addRight;
        if (roomTotal <= 0f)
        {
            addLeft = 0f;
            addRight = 0f;
        }
        else
        {
            bool leftFull = roomLeft <= 0f;
            bool rightFull = roomRight <= 0f;
            if (leftFull && !rightFull)
            {
                addLeft = 0f;
                addRight = Mathf.Min(flowLeftPerTick + flowRightPerTick, roomRight);
            }
            else if (!leftFull && rightFull)
            {
                addLeft = Mathf.Min(flowLeftPerTick + flowRightPerTick, roomLeft);
                addRight = 0f;
            }
            else if (leftFull && rightFull)
            {
                addLeft = 0f;
                addRight = 0f;
            }
            else
            {
                float addPerTickTotal = flowLeftPerTick + flowRightPerTick;
                addLeft = Mathf.Min(flowLeftPerTick, roomLeft);
                addRight = Mathf.Min(flowRightPerTick, roomRight);
                float remainder = addPerTickTotal - addLeft - addRight;
                if (remainder > 1E-6f)
                {
                    float extraLeft = Mathf.Min(remainder, roomLeft - addLeft);
                    if (extraLeft > 0f) { addLeft += extraLeft; remainder -= extraLeft; }
                    if (remainder > 1E-6f)
                        addRight += Mathf.Min(remainder, roomRight - addRight);
                }
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
