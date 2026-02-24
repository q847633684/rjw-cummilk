using System;
using System.Collections.Generic;
using RimWorld;
using rjw;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace Milk
{
    /*
    public abstract class JobDriver_GatherHumanBodyResourcesMouth : JobDriver
    {
     
        private float gatherProgress;

        protected const TargetIndex AnimalInd = TargetIndex.A;

        protected abstract float WorkTotal { get; } //this is going to be a long value. the other pawn is the one that is actually doing the work

        protected abstract HumanCompHasGatherableBodyResource GetComp(Pawn animal);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.gatherProgress, "gatherProgress", 0f, false);
        }

        public Thing Target         // for reservation
        {
            get
            {
                if (job == null)
                {
                    return null;
                }

                if (job.GetTarget(TargetIndex.A).Pawn != null)
                    return job.GetTarget(TargetIndex.A).Pawn;

                return job.GetTarget(TargetIndex.A).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo target = this.job.GetTarget(TargetIndex.A);
            Job job = this.job;
            return ReservationUtility.Reserve(pawn, target, job, 1, -1, null, errorOnFailed);
        }

        //to do: move this somewhere sensible
        //public static readonly JobDef breastfeederAdult = DefDatabase<JobDef>.GetNamed("BreastfeederAdult");

        //mouth -> starts breastfed upon.  breastfed upon derives from ___breasts


        protected override IEnumerable<Toil> MakeNewToils()
        {

            Pawn partner = (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !pawn.CanReserveAndReach(partner, PathEndMode.Touch, Danger.Deadly));
            this.FailOn(() => pawn.Drafted);
            this.FailOn(() => partner.IsFighting());
            this.FailOn(() => !partner.CanReach(pawn, PathEndMode.Touch, Danger.Deadly));
            //ToilFailConditions.FailOnDespawnedNullOrForbidden<JobDriver_GatherHumanBodyResourcesMouth>(this, TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);

            var partnerComp = ThingCompUtility.TryGetComp<CompMilkableHuman>(partner);
            var PartnerJob = DefDatabase<JobDef>.GetNamed("BreastfeederAdult");


            Toil StartPartnerJob = new Toil();
            StartPartnerJob.defaultCompleteMode = ToilCompleteMode.Instant;
            StartPartnerJob.socialMode = RandomSocialMode.Off;
            StartPartnerJob.initAction = delegate
            {
                var dri = partner.jobs.curDriver as JobDriver_GatherHumanBodyResourcesBreasts;
                if (dri == null)
                {
                    Job getting_milked = JobMaker.MakeJob(PartnerJob, pawn);

                    partner.jobs.StartJob(getting_milked, JobCondition.InterruptForced, null, false, true, null);

                }
            };
            yield return StartPartnerJob;

            Toil wait = new Toil();
            wait.initAction = delegate ()
            {
                
                Pawn actor = wait.actor;
                //Pawn pawn = (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
                actor.pather.StopDead();
                //to do: make this something real so it can potentially be animated better
                //PawnUtility.ForceWait(partner, 15000, null, true);
                
            };
            wait.tickAction = delegate ()
            {
                Pawn actor = wait.actor;
                actor.skills.Learn(SkillDefOf.Animals, 0.13f, false);
                this.gatherProgress += 1; // StatExtension.GetStatValue(actor, StatDefOf.AnimalGatherSpeed, true);

                bool flag = this.gatherProgress >= this.WorkTotal;
                if (flag)
                {
                    //this.GetComp((Pawn)((Thing)this.job.GetTarget(TargetIndex.A))).Gathered(this.pawn);
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
                }
            };
            wait.AddFinishAction(delegate ()
            {
                Pawn pawn = (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
                bool flag = pawn != null && pawn.CurJobDef == JobDefOf.Wait_MaintainPosture;
                if (flag)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                }

            });
            ToilFailConditions.FailOnDespawnedOrNull<Toil>(wait, TargetIndex.A);
            ToilFailConditions.FailOnCannotTouch<Toil>(wait, TargetIndex.A, PathEndMode.Touch);
            wait.AddEndCondition(delegate ()
            {

                //if (!this.GetComp((Pawn)((Thing)this.job.GetTarget(TargetIndex.A))).ActiveAndFull)
                if (partnerComp.Fullness < 0.1f)
                {
                    return JobCondition.Incompletable;
                }
                //if pawn hunger is full then also quit
                var hunger = pawn.needs.food;
                if (hunger.CurLevelPercentage > 0.99f)
                {
                    return JobCondition.Incompletable;
                    //to do? Add happy thought?
                }
                return JobCondition.Ongoing;
            });
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            ToilEffects.WithProgressBar(wait, TargetIndex.A, () => this.gatherProgress / this.WorkTotal, false, -0.5f);
            wait.activeSkill = (() => SkillDefOf.Animals);
            yield return wait;
            yield break;
        }
    }
    */
}
