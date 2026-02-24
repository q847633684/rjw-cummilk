using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;
using rjw;
using System.Reflection;

namespace Milk
{

    public class JobGiver_MilkSelf : ThinkNode_JobGiver
    {
        //public static readonly JobDef MilkSelf = DefDatabase<JobDef>.GetNamed("MilkSelf");

        protected override Job TryGiveJob(Pawn pawn)
        {

            if (pawn.Drafted) return null;
            if (pawn.IsFighting()) return null;
            if (pawn.IsBurning()) return null;

            var myComp = ThingCompUtility.TryGetComp<CompMilkableHuman>(pawn);
            if (myComp == null) return null;
            //if we're busy with something else that has multiple toils, wait.

            if (myComp.Fullness >= MilkSettings.fullnessMilkSelfAmount)
            {
                if (myComp.BottleCount > 0)
                {

                    //see if a milker exists
                    //Building_HumanMilker milker = null;
                    //var any_ins = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    //milker = (Building_HumanMilker)(__instance.GetType().GetProperty("Bed", any_ins).GetValue(__instance, null));


                    //Log.Message(pawn.Name.ToString() + " Trying job milkself");
                    var myJob = DefDatabase<JobDef>.GetNamed("MilkSelf");
                    //Job getting_milked = JobMaker.MakeJob(myJob, pawn);
                    Job tempJob = JobMaker.MakeJob(myJob, pawn, null, null);
                    //if (tempJob == null) { Log.Message("Null job"); }
                    return tempJob;
                }
            }

            return null;
        }
    }
}