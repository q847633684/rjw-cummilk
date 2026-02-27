using System.Collections.Generic;
using MilkCum.Milk.Helpers;
using RimWorld;
using Verse;
using Verse.AI;

namespace MilkCum.Milk.Jobs;
public class JobDriver_ActiveSuckle : JobDriver
{
    private Pawn Target => (Pawn)TargetA.Thing;
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return this.pawn.Reserve(Target, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(pawn, this.job, 1, -1, null, errorOnFailed, false);
    }
    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        yield return ChildcareHelper.ActiveSuckle(Target, this.pawn, ReadyForNextToil);
    }
}
