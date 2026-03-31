namespace MilkCum.Fluids.Shared.Data;

/// <summary>
/// 虚拟流体槽位（不绑定 <see cref="Verse.BodyPartRecord"/>）。泌乳用左/右乳槽；精液每器官池可为 <see cref="None"/> + 自定义键；TesticleLeft/Right 仅兼容旧版聚合槽命名。
/// </summary>
public enum FluidSiteKind
{
    None = 0,
    BreastLeft = 1,
    BreastRight = 2,
    TesticleLeft = 3,
    TesticleRight = 4,
}
