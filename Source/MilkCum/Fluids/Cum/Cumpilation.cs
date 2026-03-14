using MilkCum.Fluids.Cum.Cumflation;
using MilkCum.Fluids.Cum.Gathering;
using MilkCum.Fluids.Cum.Reactions;
using System;
using Verse;

namespace MilkCum.Fluids.Cum
{
    [StaticConstructorOnStartup]
    public static class Cumpilation
    {
        static Cumpilation()
        {
            ModLog.Message("Cumpilation Loaded - Let's go you cumsluts");

            GatheringUtility.PrintFluidGatheringDefInfo();
            CumflationUtility.PrintCumflatableInfo();
            ReactionUtility.PrintFluidRecordInfo();
        }
    }
}