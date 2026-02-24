using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;
using rjw;
using System.Reflection;

namespace Milk
{

    public class JobGiver_MilkSelfAtMachineAutomatic : ThinkNode_JobGiver
    {

        //public static readonly ThingDef MilkingStation = DefDatabase<ThingDef>.GetNamed("MilkingStation");

        protected override Job TryGiveJob(Pawn pawn)
        {

            if (pawn.Drafted) return null;
            if (pawn.IsFighting()) return null;
            if (pawn.IsBurning()) return null;

            var myComp = ThingCompUtility.TryGetComp<CompMilkableHuman>(pawn);
            if (myComp == null) return null;
            //if we're busy with something else that has multiple toils, wait.

            if (myComp.Fullness >= MilkSettings.fullnessMilkMachineAmount)
            {
                if (myComp.BottleCount > 0)
                {

                    //see if a milker exists
                    //Building_HumanMilker milker = null;
                    //var any_ins = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    //milker = (Building_HumanMilker)(__instance.GetType().GetProperty("Bed", any_ins).GetValue(__instance, null));
                    Log.Message(pawn.Name.ToString() + " Trying job MilkSelfAtMachineAutomatic");
                    //Thing thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.MilkingStation), PathEndMode.InteractionCell, TraverseParms.For(pawn), 999f, (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x));
                    Thing thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, pawn.Map.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("MilkingStation")), PathEndMode.InteractionCell, TraverseParms.For(pawn), 999f, (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x));

                    if (thing != null)
                    {
                        var myJob = DefDatabase<JobDef>.GetNamed("MilkSelfAtMachineAutomatic");
                        //Job getting_milked = JobMaker.MakeJob(myJob, pawn);
                        Job tempJob = JobMaker.MakeJob(myJob, pawn, thing, null);
                        if (tempJob == null) { Log.Message("Null job"); }
                        return tempJob;
                    }
                }
            }

            return null;
        }
    }
}