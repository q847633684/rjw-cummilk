using Verse;

namespace MilkCum.Core.Settings;

/// <summary>与 RJW Balls &amp; Ovaries（性腺模组）联动开关；默认全开，仅在检测到该模组时生效。</summary>
internal partial class MilkCumSettings
{
    public static bool Integration_Ballz_Enable = true;
    public static bool Integration_Ballz_GonadSemenCapacity = true;
    public static bool Integration_Ballz_TestosteroneRefill = true;
    public static bool Integration_Ballz_NeuteredNoSemen = true;
    public static bool Integration_Ballz_ElastrationPenalty = true;
    /// <summary>默认关：泌乳几乎不应由雌激素条驱动；开启后仍仅为 ±1% 量级背景项。</summary>
    public static bool Integration_Ballz_EstrogenLactation = false;
    /// <summary>睾酮仅在有「有机睾丸」Hediff 时调节精池回充（去势/假体无活体睾丸组织则不套用）；关闭则任意小人只要有 TestosteroneEffect 即参与回充。</summary>
    public static bool Integration_Ballz_TestosteroneRequiresPenisPool = true;
    /// <summary>睾丸装备：仅压迫/缺血/机械梗阻，不奖励情趣增产量。</summary>
    public static bool Integration_Ballz_TesticleGear = true;

    private static void ExposeBallzIntegrationData()
    {
        Scribe_Values.Look(ref Integration_Ballz_Enable, "EM.Integration.Ballz.Enable", true);
        Scribe_Values.Look(ref Integration_Ballz_GonadSemenCapacity, "EM.Integration.Ballz.GonadSemenCapacity", true);
        Scribe_Values.Look(ref Integration_Ballz_TestosteroneRefill, "EM.Integration.Ballz.TestosteroneRefill", true);
        Scribe_Values.Look(ref Integration_Ballz_NeuteredNoSemen, "EM.Integration.Ballz.NeuteredNoSemen", true);
        Scribe_Values.Look(ref Integration_Ballz_ElastrationPenalty, "EM.Integration.Ballz.ElastrationPenalty", true);
        Scribe_Values.Look(ref Integration_Ballz_EstrogenLactation, "EM.Integration.Ballz.EstrogenLactation", false);
        Scribe_Values.Look(ref Integration_Ballz_TestosteroneRequiresPenisPool, "EM.Integration.Ballz.TestosteroneRequiresPenisPool", true);
        Scribe_Values.Look(ref Integration_Ballz_TesticleGear, "EM.Integration.Ballz.TesticleGear", true);
    }
}
