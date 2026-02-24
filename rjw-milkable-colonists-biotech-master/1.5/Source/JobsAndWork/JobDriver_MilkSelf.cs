using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;


namespace Milk
{

    [DefOf]
    public static class JobDefOfMilkSelf
    {
        public static JobDef MilkSelf;

        static JobDefOfMilkSelf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
        }
    }

    public class JobDriver_MilkSelf : JobDriver_GatherHumanBodyResourcesSelf
    {

        protected override HumanCompHasGatherableBodyResource GetComp(Pawn animal)
        {
            return ThingCompUtility.TryGetComp<CompMilkableHuman>(animal);
        }
    }

    public abstract class JobDriver_GatherHumanBodyResourcesSelf : JobDriver
    {

        public bool shouldreserve = true;

        private float gatherProgress;
        private float tickProgress;

        //protected const TargetIndex AnimalInd = TargetIndex.A;

        private float WorkTotal = 4800f; //max length this will run for, 10 milks
        //private float WorkTick = MilkSettings.milkUpdateInterval;
        private float WorkTick = MilkSettings.milkUpdateInterval * (1f / MilkSettings.workSpeedMult);

        protected abstract HumanCompHasGatherableBodyResource GetComp(Pawn animal);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.gatherProgress, "gatherProgress", 0f, false);
            Scribe_Values.Look<float>(ref this.tickProgress, "tickProgress", 0f, false);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true; // No reservations needed.
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {

            this.FailOn(() => pawn.health.Downed); //cant milk yourself if you're downed.  not sure what conscious and lost both legs and lying in bed counts as. one day I guess I'll have to find out
            this.FailOn(() => pawn.IsBurning()); //something for later? If on fire put self out with own milk?
            this.FailOn(() => pawn.IsFighting());
            this.FailOn(() => pawn.Drafted);

            Toil wait = new Toil();
            wait.initAction = delegate ()
            {
                Pawn actor = wait.actor;
                actor.pather.StopDead();

            };


            wait.tickAction = delegate ()
            {
                Pawn actor = wait.actor;
                actor.skills.Learn(SkillDefOf.Animals, 0.13f, false);
                this.gatherProgress += 1; // StatExtension.GetStatValue(actor, StatDefOf.AnimalGatherSpeed, true);
                this.tickProgress += 1;

                if (this.tickProgress > this.WorkTick)
                {
                    var myComp = ThingCompUtility.TryGetComp<CompMilkableHuman>(pawn);
                    myComp.TryMakeBottle(pawn, pawn, 0.02f); //milking self. less fulfilling. pawn is their own partner. or something.
                    this.tickProgress -= this.WorkTick;

                    SexUtility.DrawNude(pawn);
                    myComp.ShouldRedress = true;
                    myComp.shouldAdd = false;

                }

                if (this.gatherProgress > this.WorkTotal)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
                }


            };
            wait.AddFinishAction(delegate ()
            {
                /*
                if (xxx.is_human(pawn))
                {
                    var comp = CompRJW.Comp(pawn);
                    if (comp != null)
                    {
                        comp.drawNude = false;
                        pawn.Drawer.renderer.SetAllGraphicsDirty();
                    }
                }
                GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
                */
            });

            wait.AddEndCondition(delegate ()
            {
                var myComp = ThingCompUtility.TryGetComp<CompMilkableHuman>(pawn);
                if (myComp.BottleCount < 1f)
                {
                    return JobCondition.Succeeded;
                }
                return JobCondition.Ongoing;
            });
            wait.defaultCompleteMode = ToilCompleteMode.Never;

            //wait.handlingFacing = true;
            //ToilEffects.WithProgressBar(wait, TargetIndex.A, () => this.gatherProgress / this.WorkTotal, false, -0.5f);
            //wait.activeSkill = (() => SkillDefOf.Animals);
            yield return wait;
            yield break;
        }
    }
}