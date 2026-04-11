namespace MilkCum.Fluids.Shared.Data;

/// <summary>
/// 虚拟流体槽位（不绑定 <see cref="Verse.BodyPartRecord"/>）。泌乳用左/右乳槽；射精经济用左/右虚拟睾丸槽（多阴茎汇入两池）。
/// </summary>
public enum FluidSiteKind
{
    None = 0,
    BreastLeft = 1,
    BreastRight = 2,
    TesticleLeft = 3,
    TesticleRight = 4,
}
