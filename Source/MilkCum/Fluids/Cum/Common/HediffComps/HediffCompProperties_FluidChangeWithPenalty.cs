using MilkCum.Fluids.Cum.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MilkCum.Fluids.Cum.Common
{
    public class HediffCompProperties_FluidChangeWithPenalty : HediffCompProperties_FluidChange
    {
        public HediffDef penaltyHediff;
        public float penaltySeverity;

        public HediffCompProperties_FluidChangeWithPenalty() => this.compClass = typeof(HediffComp_FluidChangeWithPenalty);

    }
}
