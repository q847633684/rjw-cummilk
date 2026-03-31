namespace MilkCum.Fluids.Shared.Data;

/// <summary>
/// 虚拟流体槽位（不绑定 <see cref="Verse.BodyPartRecord"/>）。泌乳用左/右乳槽；射精经济用左/右睾丸虚拟槽；与 Docs/流体与分侧架构-方案整理.md 一致。
/// </summary>
public enum FluidSiteKind
{
    None = 0,
    BreastLeft = 1,
    BreastRight = 2,
    TesticleLeft = 3,
    TesticleRight = 4,
}
