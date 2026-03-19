using MilkCum.Fluids.Shared.Data;
using NUnit.Framework;

namespace MilkCum.Tests.Fluids.Shared.Data;

/// <summary>FluidPoolState 单元测试：双池进水/溢出/回缩/满池计数与单乳进水公式。</summary>
[TestFixture]
public class FluidPoolStateTests
{
    private const float Eps = 1e-5f;

    #region TickGrowth（双池进水）

    [Test]
    public void TickGrowth_BothSidesHaveRoom_DistributesByFlow()
    {
        var s = new FluidPoolState { LeftFullness = 0f, RightFullness = 0f };
        float leftCap = 2f, rightCap = 2f, stretchL = 3f, stretchR = 3f;
        float flowL = 1f, flowR = 1f;

        var cap = new BreastPairCapacities(leftCap, rightCap, stretchL, stretchR);
        var (newLeft, newRight, overflow) = s.TickGrowth(flowL, flowR, cap);

        Assert.That(overflow, Is.EqualTo(0f).Within(Eps));
        Assert.That(newLeft, Is.EqualTo(1f).Within(Eps));
        Assert.That(newRight, Is.EqualTo(1f).Within(Eps));
    }

    [Test]
    public void TickGrowth_OnlyRightHasRoom_SpillsToRight()
    {
        var s = new FluidPoolState { LeftFullness = 2f, RightFullness = 0f };
        float leftCap = 2f, rightCap = 2f, stretchL = 3f, stretchR = 3f;
        float flowL = 0.5f, flowR = 0.5f;

        var cap = new BreastPairCapacities(leftCap, rightCap, stretchL, stretchR);
        var (newLeft, newRight, overflow) = s.TickGrowth(flowL, flowR, cap);

        Assert.That(overflow, Is.EqualTo(0f).Within(Eps));
        Assert.That(newLeft, Is.EqualTo(2f).Within(Eps));
        Assert.That(newRight, Is.EqualTo(1f).Within(Eps));
    }

    [Test]
    public void TickGrowth_OnlyLeftHasRoom_SpillsToLeft()
    {
        var s = new FluidPoolState { LeftFullness = 0f, RightFullness = 2f };
        float leftCap = 2f, rightCap = 2f, stretchL = 3f, stretchR = 3f;
        float flowL = 0.5f, flowR = 0.5f;

        var cap = new BreastPairCapacities(leftCap, rightCap, stretchL, stretchR);
        var (newLeft, newRight, overflow) = s.TickGrowth(flowL, flowR, cap);

        Assert.That(overflow, Is.EqualTo(0f).Within(Eps));
        Assert.That(newLeft, Is.EqualTo(1f).Within(Eps));
        Assert.That(newRight, Is.EqualTo(2f).Within(Eps));
    }

    [Test]
    public void TickGrowth_BothAtBaseCap_ThenStretch()
    {
        var s = new FluidPoolState { LeftFullness = 2f, RightFullness = 2f };
        float leftCap = 2f, rightCap = 2f, stretchL = 3f, stretchR = 3f;
        float flowL = 0.5f, flowR = 0.5f;

        var cap = new BreastPairCapacities(leftCap, rightCap, stretchL, stretchR);
        var (newLeft, newRight, overflow) = s.TickGrowth(flowL, flowR, cap);

        Assert.That(overflow, Is.EqualTo(0f).Within(Eps));
        Assert.That(newLeft, Is.EqualTo(2.5f).Within(Eps));
        Assert.That(newRight, Is.EqualTo(2.5f).Within(Eps));
    }

    [Test]
    public void TickGrowth_Overflow_WhenBothFullAndStretchFull()
    {
        var s = new FluidPoolState { LeftFullness = 3f, RightFullness = 3f };
        float leftCap = 2f, rightCap = 2f, stretchL = 3f, stretchR = 3f;
        float flowL = 1f, flowR = 1f;

        var cap = new BreastPairCapacities(leftCap, rightCap, stretchL, stretchR);
        var (newLeft, newRight, overflow) = s.TickGrowth(flowL, flowR, cap);

        Assert.That(overflow, Is.EqualTo(2f).Within(Eps));
        Assert.That(newLeft, Is.EqualTo(3f).Within(Eps));
        Assert.That(newRight, Is.EqualTo(3f).Within(Eps));
    }

    [Test]
    public void TickGrowth_BothSidesPartialFill_NoOverflow()
    {
        var s = new FluidPoolState { LeftFullness = 0.5f, RightFullness = 0.5f };
        float leftCap = 1f, rightCap = 1f, stretchL = 2f, stretchR = 2f;
        float flowL = 0.3f, flowR = 0.3f; // total 0.6, room 0.5 each → 0.3+0.3 无余量

        var cap = new BreastPairCapacities(leftCap, rightCap, stretchL, stretchR);
        var (newLeft, newRight, overflow) = s.TickGrowth(flowL, flowR, cap);

        Assert.That(overflow, Is.EqualTo(0f).Within(Eps));
        Assert.That(newLeft, Is.EqualTo(0.8f).Within(Eps));
        Assert.That(newRight, Is.EqualTo(0.8f).Within(Eps));
    }

    #endregion

    #region SingleBreastTickGrowth（单乳进水）

    [Test]
    public void SingleBreastTickGrowth_FillToBase_NoOverflow()
    {
        var (newFullness, overflow) = FluidPoolState.SingleBreastTickGrowth(0f, 1f, 2f, 3f);
        Assert.That(newFullness, Is.EqualTo(1f).Within(Eps));
        Assert.That(overflow, Is.EqualTo(0f).Within(Eps));
    }

    [Test]
    public void SingleBreastTickGrowth_AboveBase_StretchThenOverflow()
    {
        var (newFullness, overflow) = FluidPoolState.SingleBreastTickGrowth(2f, 2f, 2f, 3f);
        Assert.That(newFullness, Is.EqualTo(3f).Within(Eps));
        Assert.That(overflow, Is.EqualTo(1f).Within(Eps));
    }

    [Test]
    public void SingleBreastTickGrowth_AlreadyAtStretchCap_AllOverflow()
    {
        var (newFullness, overflow) = FluidPoolState.SingleBreastTickGrowth(3f, 1f, 2f, 3f);
        Assert.That(newFullness, Is.EqualTo(3f).Within(Eps));
        Assert.That(overflow, Is.EqualTo(1f).Within(Eps));
    }

    [Test]
    public void SingleBreastTickGrowth_ZeroFlow_NoChange()
    {
        var (newFullness, overflow) = FluidPoolState.SingleBreastTickGrowth(1f, 0f, 2f, 3f);
        Assert.That(newFullness, Is.EqualTo(1f).Within(Eps));
        Assert.That(overflow, Is.EqualTo(0f).Within(Eps));
    }

    #endregion

    #region TickShrink（回缩）

    [Test]
    public void TickShrink_AboveBase_ShrinksByFactor()
    {
        var s = new FluidPoolState { LeftFullness = 3f, RightFullness = 3f };
        float leftCap = 2f, rightCap = 2f;
        s.TickShrink(leftCap, rightCap, 0.5f);
        Assert.That(s.LeftFullness, Is.EqualTo(2.5f).Within(Eps)); // 2 + (3-2)*0.5
        Assert.That(s.RightFullness, Is.EqualTo(2.5f).Within(Eps));
    }

    [Test]
    public void TickShrink_AtOrBelowBase_NoChange()
    {
        var s = new FluidPoolState { LeftFullness = 2f, RightFullness = 1f };
        s.TickShrink(2f, 2f, 0.5f);
        Assert.That(s.LeftFullness, Is.EqualTo(2f).Within(Eps));
        Assert.That(s.RightFullness, Is.EqualTo(1f).Within(Eps));
    }

    [Test]
    public void TickShrink_FactorZero_ClampsToBase()
    {
        var s = new FluidPoolState { LeftFullness = 3f, RightFullness = 3f };
        s.TickShrink(2f, 2f, 0f);
        Assert.That(s.LeftFullness, Is.EqualTo(2f).Within(Eps));
        Assert.That(s.RightFullness, Is.EqualTo(2f).Within(Eps));
    }

    #endregion

    #region UpdateFullPoolCounter（满池计数）

    [Test]
    public void UpdateFullPoolCounter_AboveThreshold_Increments()
    {
        var s = new FluidPoolState { LeftFullness = 1f, RightFullness = 1f, TicksFullPool = 5 };
        s.UpdateFullPoolCounter(1.5f, 10);
        Assert.That(s.TicksFullPool, Is.EqualTo(15));
    }

    [Test]
    public void UpdateFullPoolCounter_BelowThreshold_ResetsToZero()
    {
        var s = new FluidPoolState { LeftFullness = 0.5f, RightFullness = 0.5f, TicksFullPool = 5 };
        s.UpdateFullPoolCounter(1.5f, 10);
        Assert.That(s.TicksFullPool, Is.EqualTo(0));
    }

    [Test]
    public void UpdateFullPoolCounter_AtThreshold_Increments()
    {
        var s = new FluidPoolState { LeftFullness = 1f, RightFullness = 0.5f, TicksFullPool = 0 };
        s.UpdateFullPoolCounter(1.5f, 1);
        Assert.That(s.TicksFullPool, Is.EqualTo(1));
    }

    #endregion
}
