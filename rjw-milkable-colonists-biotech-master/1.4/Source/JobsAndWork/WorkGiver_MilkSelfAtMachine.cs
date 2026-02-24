/**
using RimWorld;
using RimWorld.Planet;
using System;
using Verse;
using Verse.AI;

namespace Milk
{
    public class WorkGiver_MilkSelfAtMachine : WorkGiver_GatherHumanBodyResources
    {
        protected override JobDef JobDef
        {
            get
            {
                return JobDefOfMilkSelfAtMachine.MilkSelfAtMachine;
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.InteractionCell;
            }
        }

        protected override HumanCompHasGatherableBodyResource GetComp(Pawn animal)
        {
            return ThingCompUtility.TryGetComp<CompMilkableHuman>(animal);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn pawn2 = t as Pawn;
            // !pawn2.CanCasuallyInteractNow(false, false, false)
            //bool flag = pawn2 == null || !pawn2.RaceProps.Humanlike || pawn2.Drafted || pawn2.InAggroMentalState || CaravanFormingUtility.IsFormingCaravan(pawn2) || !pawn2.IsDesignatedMilkAllowManual() || !pawn.IsDesignatedMilkWillManual();
            bool flag = pawn2 == null || !pawn2.RaceProps.Humanlike || pawn2.InAggroMentalState || CaravanFormingUtility.IsFormingCaravan(pawn2) || !pawn2.IsDesignatedMilkAllowMachine();
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                HumanCompHasGatherableBodyResource comp = this.GetComp(pawn2);
                bool flag2 = comp != null && comp.Active && pawn2 != pawn && comp.BottleCount >= 1 && comp.Fullness > MilkSettings.fullnessMilkBottleAmount;
                if (flag2)
                {
                    LocalTargetInfo localTargetInfo = pawn2;
                    bool flag3 = ReservationUtility.CanReserve(pawn, localTargetInfo, 1, -1, null, forced);
                    if (flag3)
                    {
                        return true;
                    }
                }
                result = false;
            }
            return result;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return new Job(JobDefOfMilkSelfAtMachine.MilkSelfAtMachine, t);
        }
    }
}

**/

