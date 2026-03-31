using MilkCum.Fluids.Cum.Comps;
using RimWorld;
using rjw;
using Verse;

namespace MilkCum.Fluids.Cum.Common;

public static class PawnSemenPoolExtensions
{
    public static CompVirtualSemenPool CompVirtualSemenPool(this Pawn pawn)
    {
        if (pawn == null) return null;
        CompVirtualSemenPool comp = pawn.TryGetComp<CompVirtualSemenPool>();
        if (comp == null)
        {
            comp = new CompVirtualSemenPool
            {
                parent = pawn,
                props = new CompProperties_VirtualSemenPool()
            };
            pawn.AllComps.Add(comp);
        }

        return comp;
    }

    /// <summary>按虚拟睾丸池扣减并返回实际射精量（不登记口交记录）。</summary>
    public static float ConsumeSemenForEjection(this Pawn donor, ISexPartHediff part, float nominal)
    {
        if (donor == null || part == null) return nominal;
        return donor.CompVirtualSemenPool().ConsumeForEjaculation(part, nominal, registerForFluidRecords: false);
    }
}
