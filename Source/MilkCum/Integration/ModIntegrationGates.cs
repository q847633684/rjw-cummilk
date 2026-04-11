using RimWorld;
using Verse;

namespace MilkCum.Integration;

/// <summary>
/// 联动是否生效以「对应 mod 已在当前 mod 列表中激活」为准，不再叠加单独的启用勾选项。
/// 检测时机与调试工具页的「可选模组」列表一致。
/// 乳房虚拟乳池与 RJW 胸形经济：<see cref="RjwModActive"/> 为 false 时相关系统明确不运行（与 About 中 RJW 依赖一致）。
/// </summary>
[StaticConstructorOnStartup]
internal static class ModIntegrationGates
{
    public static readonly bool RjwModActive;

    static ModIntegrationGates()
    {
        RjwModActive = ModLister.GetModWithIdentifier("rim.job.world") != null;
    }
}
