using HarmonyLib;
using Verse;

namespace Cumpilation
{
    /// <summary>
    /// 已合并进 MilkCum：不再在此创建 Harmony，由 MilkCum 主入口统一 PatchAll() 本程序集（含 Cumpilation 的 [HarmonyPatch]）。
    /// </summary>
    [StaticConstructorOnStartup]
    static internal class HarmonyInit
    {
        static HarmonyInit()
        {
            // 原 vegapnk.cumpilation 独立 Mod 时在此 harmony.PatchAll()。
            // 合并后由 MilkCum.Core.ModInit 静态构造中 MilkCumMod.Harmony 统一打补丁，此处不再执行。
        }
    }
}
