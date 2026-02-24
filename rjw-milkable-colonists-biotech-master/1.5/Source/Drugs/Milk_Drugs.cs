using RimWorld;
using Verse;
using rjw;
using System.Linq;

namespace Milk
{
    public class ThingComp_BreastExpand : ThingComp
    {
        public override void PostIngested(Pawn pawn)
        {
            var breastList = pawn.GetBreastList();

            if (!breastList.NullOrEmpty())
                foreach (var breasts in breastList.Where(x => !x.TryGetComp<HediffComp_SexPart>().Fluid.defName.NullOrEmpty()))
                {
                    breasts.Severity += 0.2f;

                     if (MilkSettings.forceCapMaxBreastSize)
                    {
                        //I have no idea what the max is. Can you even go above 1?
                        float maxBreastSize = 2f;
                        if (MilkSettings.breastGrowthMaxSize != 0)
                            maxBreastSize = MilkSettings.breastGrowthMaxSize;

                        if (breasts.Severity > maxBreastSize)
                            breasts.Severity = maxBreastSize;
                    }
                }
        }
    }
    public class CompProperties_BreastExpand : CompProperties
    {
        public CompProperties_BreastExpand()
        {
            compClass = typeof(ThingComp_BreastExpand);
        }
    }
}