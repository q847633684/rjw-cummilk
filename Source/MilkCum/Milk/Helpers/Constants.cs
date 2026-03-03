namespace MilkCum.Milk.Helpers;

public static class Constants
{
    public const string MILK_TYPE_PREFIX = "milkType_";
    public const float UNIT_SIZE = 32f;
    public const float MILK_CHARGE_FACTOR = 8f / 3f; // Max 0.125 => 1, 3 Milk full charge
}

/// <summary>水池模型常量：规格第七节、第一节、十四节已定。详见 记忆库/domain/代码常量与公式对应。</summary>
public static class PoolModelConstants
{
    /// <summary>基础值_T（B_T）：每日基础衰减 = 1/(B_T×有效药效系数)。</summary>
    public const float BaseValueT = 3f;
    /// <summary>分娩时 L 增量（不乘有效药效系数），单位与 L 一致（归一化容量）。</summary>
    public const float BaseValueTBirth = 10f;
    /// <summary>有效药效系数下限：max(1−耐受, 0.05)。</summary>
    public const float EffectiveDrugFactorMin = 0.05f;
    /// <summary>负反馈系数 k：每日衰减 = 1/(B_T×E) + k×L。τ≈1/k 游戏日。建议 [0.002, 0.05]。</summary>
    public const float NegativeFeedbackK = 0.01f;
    /// <summary>L 下限：L 小于此值时视为 0，泌乳结束。避免浮点长期接近 0 不结束。</summary>
    public const float LactationEndEpsilon = 1E-5f;

    /// <summary>吸收延迟：基准 tick 数（0.25 游戏日）。实际延迟 = BaseAbsorptionDelayTicks × factor，factor = Lerp(1.5, 0.5, InverseLerp(0.25, 2, Clamp(代谢率, 0.25, 2)))，不做除法避免爆炸。见 记忆库/decisions/ADR-002-吸收延迟基准。</summary>
    public const int BaseAbsorptionDelayTicks = 15000;

    /// <summary>满池撑大：单侧最大水位 = HalfPool × StretchCapFactor（规格：暂时允许超过基础容量）。</summary>
    public const float StretchCapFactor = 1.2f;
    /// <summary>溢出污物：累计溢出量达到此阈值时生成 1 格地面污物（不扣水位）。</summary>
    public const float OverflowFilthThreshold = 0.02f;
    /// <summary>排水后回缩：每 30 tick 超出基础容量部分乘以 (1 - ShrinkPerStep)，约 0.5 游戏日回缩到基础容量。</summary>
    public const float ShrinkPerStep = 0.009f;
    /// <summary>剂量转 L 系数 C_dose：ΔL = Δs × C_dose × E_tol(t_before)。默认 1。</summary>
    public const float DoseToLFactor = 1f;
    /// <summary>1 池单位对应的营养：ExtraNutritionPerDay = flow × 本值；改池容量或 L 单位时只改此处即可保持平衡。</summary>
    public const float NutritionPerPoolUnit = 1f;
}
