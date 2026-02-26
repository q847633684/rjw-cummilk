namespace EqualMilking.Helpers;

public static class Constants
{
    public const string MILK_TYPE_PREFIX = "milkType_";
    public const float UNIT_SIZE = 32f;
    public const float MILK_CHARGE_FACTOR = 8f / 3f; // Max 0.125 => 1, 3 Milk full charge
}

/// <summary>水池模型常量：规格第七节、第一节已定。</summary>
public static class PoolModelConstants
{
    /// <summary>吃药时剩余天数增量（游戏日），每次 += BaseValueT × 有效药效系数。</summary>
    public const float BaseValueT = 3f;
    /// <summary>分娩时剩余天数增量（游戏日），剩余天数 += 10，不乘有效药效系数。</summary>
    public const float BaseValueTBirth = 10f;
    /// <summary>每日消耗系数基数：未成瘾/戒断时每游戏日 剩余天数 −= 1 + 0.1×(1+耐受)。</summary>
    public const float DailyConsumptionBase = 1f;
    /// <summary>每日消耗系数中耐受系数：0.1×(1+耐受)。</summary>
    public const float DailyConsumptionToleranceFactor = 0.1f;
    /// <summary>有效药效系数下限：max(1−耐受, 0.05)。</summary>
    public const float EffectiveDrugFactorMin = 0.05f;
}
