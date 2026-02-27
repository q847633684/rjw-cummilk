using Verse;
using RimWorld;
using MilkCum.Milk.Data;

namespace MilkCum.Milk.Helpers;
public static class GeneHelper
{
    public static void ReloadImpliedGenes()
    {
        if (EqualMilkingSettings.genes == null) { return; }
        EqualMilkingSettings.genes.RemoveWhere(x => x?.ThingDef == null);
        foreach (Gene_MilkTypeData geneData in EqualMilkingSettings.genes)
        {
            if (geneData?.ThingDef != null)
            {
                DefGenerator.AddImpliedDef(geneData.GenGeneDef(), true);
            }
        }
    }
}
