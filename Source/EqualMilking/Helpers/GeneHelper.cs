using Verse;
using RimWorld;
using EqualMilking.Data;

namespace EqualMilking.Helpers;
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
