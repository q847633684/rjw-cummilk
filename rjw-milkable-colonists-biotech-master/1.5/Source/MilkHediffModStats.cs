using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Milk
{

    [DefOf]
    public static class MilkHediffModStats
    {
        public static StatDef MilkProductionSpeed = DefDatabase<StatDef>.GetNamed("MilkProductionSpeed");
        public static StatDef MilkProductionYield = DefDatabase<StatDef>.GetNamed("MilkProductionYield");
    }

    public static class IdeoDefOf
    {
        [MayRequire("c0ffee.rjw.ideologyaddons")] public static readonly MemeDef MemeHucow = DefDatabase<MemeDef>.GetNamedSilentFail("Hucow");
        //[MayRequireIdeology] public static readonly PreceptDef Bestiality_OnlyVenerated;
    }

    public static class HediffDefOf
    {
        [MayRequire("c0ffee.rjw.ideologyaddons")] public static readonly HediffDef HediffHucow = DefDatabase<HediffDef>.GetNamedSilentFail("Hucow");
    }
 
}

