using System;
using System.Collections.Generic;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static UnityEngine.GraphicsBuffer;

namespace Milk
{
    /*
    public abstract class JobDriver_GatherHumanBodyResourcesBreasts : JobDriver
    {

        private float gatherProgress;

        protected const TargetIndex AnimalInd = TargetIndex.A;

        protected abstract float WorkTotal { get; } //this is going to be a long value. the other pawn is the one that is actually doing the work

        protected abstract HumanCompHasGatherableBodyResource GetComp(Pawn animal);

        public Pawn PartnerPawn = null;

        //public virtual Vector3 ForcedBodyOffset => Vector3.zero;

        public override abstract Vector3 ForcedBodyOffset { get; }
        //        public override Vector3 ForcedBodyOffset()

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


        protected override IEnumerable<Toil> MakeNewToils()
        {
            Pawn partner = (Pawn)this.job.GetTarget(TargetIndex.A).Thing;

            //ToilFailConditions.FailOnDespawnedNullOrForbidden<JobDriver_GatherHumanBodyResourcesBreasts>(this, TargetIndex.A);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !pawn.CanReserveAndReach(partner, PathEndMode.Touch, Danger.Deadly));
            this.FailOn(() => pawn.Drafted);
            this.FailOn(() => partner.IsFighting());
            this.FailOn(() => !partner.CanReach(pawn, PathEndMode.Touch, Danger.Deadly));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);

            Toil wait = new Toil();

            var myComp = ThingCompUtility.TryGetComp<CompMilkableHuman>(pawn);

            //var partnerComp = ThingCompUtility.TryGetComp<CompMilkableHuman>(partner);
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
                actor.skills.Learn(SkillDefOf.Animals, 0.01f, false);
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
                if (myComp.Fullness < 0.1f)
                {

                    return JobCondition.Incompletable;
                }
                //if pawn hunger is full then also quit
                var hunger = partner.needs.food;
                if (hunger.CurLevelPercentage > 0.99f)
                {
                    pawn.Drawer.renderer.graphics.ResolveApparelGraphics();
                    GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);

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
