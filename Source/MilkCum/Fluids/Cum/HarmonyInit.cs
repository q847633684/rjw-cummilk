using HarmonyLib;
using Verse;

namespace MilkCum.Fluids.Cum
{
    /// <summary>
    /// 宸插悎骞惰繘 MilkCum锛氫笉鍐嶅湪姝ゅ垱寤?Harmony锛岀敱 MilkCum 涓诲叆鍙ｇ粺涓€ PatchAll() 鏈▼搴忛泦锛堝惈 Cumpilation 鐨?[HarmonyPatch]锛夈€?
    /// </summary>
    [StaticConstructorOnStartup]
    static internal class HarmonyInit
    {
        static HarmonyInit()
        {
            // 鍘?vegapnk.cumpilation 鐙珛 Mod 鏃跺湪姝?harmony.PatchAll()銆?
            // 鍚堝苟鍚庣敱 MilkCum.Core.ModInit 闈欐€佹瀯閫犱腑 MilkCumMod.Harmony 缁熶竴鎵撹ˉ涓侊紝姝ゅ涓嶅啀鎵ц銆?
        }
    }
}
