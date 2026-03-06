using MilkCum.Core;
using RimWorld;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;
public static class GeneHelper
{
    public static void ReloadImpliedGenes()
    {
        if (MilkCumSettings.genes == null) { return; }
        MilkCumSettings.genes.RemoveWhere(x => x?.ThingDef == null);
        foreach (Gene_MilkTypeData geneData in MilkCumSettings.genes)
        {
            if (geneData?.ThingDef != null)
            {
                DefGenerator.AddImpliedDef(geneData.GenGeneDef(), true);
            }
        }
    }
}
