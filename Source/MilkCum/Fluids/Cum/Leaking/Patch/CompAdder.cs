using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Verse.Noise;
using RimWorld.QuestGen;
using rjw;
using MilkCum.Fluids.Cum.Cumflation;
using MilkCum.Fluids.Cum;
using MilkCum.Fluids.Cum.Gathering;

namespace MilkCum.Fluids.Cum.Leaking
{
    [StaticConstructorOnStartup]
    public static class AddComp
    {
        static AddComp()
        {
            AddLeakCumComp();
        }
        public static void AddLeakCumComp()
        {
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs.Where(x => x.race != null && x.race.Humanlike && x.thingClass != typeof(Corpse)))
            {
                thingDef.comps.Add(new CompProperties_SealCum());
            }
        }
    }
}
