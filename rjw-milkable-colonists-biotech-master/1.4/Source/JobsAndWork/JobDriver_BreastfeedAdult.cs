using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;


namespace Milk
{

    [DefOf]
    public static class JobDefOfBreastfeedAdult
    {
        public static JobDef BreastfeedAdult;

        static JobDefOfBreastfeedAdult()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
        }
    }

    public class JobDriver_BreastfeedAdult : JobDriver_GatherHumanBodyResourcesMouth
    {
        protected override HumanCompHasGatherableBodyResource GetComp(Pawn animal)
        {
            return ThingCompUtility.TryGetComp<CompMilkableHuman>(animal);
        }
    }

    public abstract class JobDriver_GatherHumanBodyResourcesMouth : JobDriver
    {

        public bool shouldreserve = true;

        private float gatherProgress;
        private float tickProgress;

        protected const TargetIndex AnimalInd = TargetIndex.A;

        private float WorkTotal = 4800f; //max length this will run for, 24 milks
        private float WorkTick = MilkSettings.milkUpdateInterval * (1f/ MilkSettings.workSpeedMult);
        
        protected abstract HumanCompHasGatherableBodyResource GetComp(Pawn animal);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.gatherProgress, "gatherProgress", 0f, false);
            Scribe_Values.Look<float>(ref this.tickProgress, "tickProgress", 0f, false);
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
            //this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !pawn.CanReserveAndReach(partner, PathEndMode.Touch, Danger.Deadly));
            this.FailOn(() => pawn.Drafted);
            this.FailOn(() => pawn.health.Downed);
            this.FailOn(() => pawn.IsBurning());
            //this.FailOn(() => partner.health.Downed); //you should be able to gather from downed pawns
            //this.FailOn(() => partner.Drafted); //it seems being drafted stops this anyway?  not quite sure why. I wanted to use drafted as a way to help slow pawns catch fast pawns
            this.FailOn(() => partner.IsFighting());
            this.FailOn(() => partner.IsBurning());

            var partnerComp = ThingCompUtility.TryGetComp<CompMilkableHuman>(partner);
            var PartnerJob = DefDatabase<JobDef>.GetNamed("BreastfeederAdult");

            ToilFailConditions.FailOnDespawnedNullOrForbidden<JobDriver_GatherHumanBodyResourcesMouth>(this, TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);

            Toil wait = new Toil();
            wait.initAction = delegate ()
            {

                Pawn actor = wait.actor;
                //Pawn pawn = (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
                actor.pather.StopDead();
                //to do: make this something real so it can potentially be animated better

                if (partner.Drafted) partner.drafter.Drafted = false;
                JobDef def = PartnerJob;
                if (!partner.Awake()) def = (partner.IsSelfShutdown() ? JobDefOf.SelfShutdown : JobDefOf.Wait_Asleep);
                Job job = JobMaker.MakeJob(def, pawn);
                if (!partner.Awake())
                {
                    job.forceSleep = true;
                    job.targetA = partner.Position;
                }
                job.expiryInterval = (int)WorkTotal * 2;

                if (MilkSettings.enableLessInteruptions)
                {
                    //new style, less interrupts
                    if (partner.CanCasuallyInteractNow(true, false, true) || partner.pather.MovingNow || (!partner.Awake()))
                    {
                        partner.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
                    }
                }
                else
                {
                    //old style, just interrupts
                    partner.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
                }

                //I split the ForceWait function out above and swapped the job for my own instead of Wait_MaintainPosture
                //PawnUtility.ForceWait(partner, (int)WorkTotal * 2, pawn, true, true);

                SexUtility.DrawNude(partner);
                partnerComp.ShouldRedress = true;
                partnerComp.shouldAdd = false;

            };
            //ToilFailConditions.FailOnDespawnedNullOrForbidden<JobDriver_GatherHumanBodyResourcesMouth>(this, TargetIndex.A);
            //yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            wait.tickAction = delegate ()
            {
                Pawn actor = wait.actor;
                actor.skills.Learn(SkillDefOf.Animals, 0.13f, false);
                //float jobProgress = StatExtension.GetStatValue(actor, StatDefOf.AnimalGatherSpeed, true);
                //removed the relying on animal stat
                this.gatherProgress += 1; // jobProgress;
                this.tickProgress += 1; // jobProgress;

                pawn.rotationTracker.Face(partner.DrawPos);

                if (this.tickProgress > this.WorkTick)
                {

                    //work loop is done in TryFeedPartner
                    partnerComp.TryFeedPartner(partner, pawn);

                    this.tickProgress -= this.WorkTick;
                }

                if (this.gatherProgress > this.WorkTotal)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
                }


            };

            wait.AddFinishAction(delegate ()
            {
                /*
                bool flag = partner != null && partner.CurJobDef == PartnerJob;
                if (flag)
                {
                    partner.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                }*/

                bool flag = partner != null && partner.CurJobDef == PartnerJob;
                if (flag)
                {
                    partner.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                }
            });
            ToilFailConditions.FailOnDespawnedOrNull<Toil>(wait, TargetIndex.A);
            ToilFailConditions.FailOnCannotTouch<Toil>(wait, TargetIndex.A, PathEndMode.Touch);
            wait.AddEndCondition(delegate ()
            {

                //if (!this.GetComp((Pawn)((Thing)this.job.GetTarget(TargetIndex.A))).ActiveAndFull)
                if (partnerComp.Fullness < 0.1f)
                {
                    return JobCondition.Succeeded;
                }
                //if pawn hunger is full then also quit
                var hunger = pawn.needs.food;
                if (hunger.CurLevelPercentage > 0.95f)
                {
                    return JobCondition.Succeeded;
                    //to do? Add happy thought?
                }
                return JobCondition.Ongoing;
            });
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            //wait.handlingFacing = true;
            ToilEffects.WithProgressBar(wait, TargetIndex.A, () => this.gatherProgress / this.WorkTotal, false, -0.5f);
            wait.activeSkill = (() => SkillDefOf.Animals);
            yield return wait;
            yield break;
        }
    }
}