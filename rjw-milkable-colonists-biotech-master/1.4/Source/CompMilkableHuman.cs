using System;
using Verse;
using rjw;
using RimWorld;
using static rjw.Dialog_Sexcard;

namespace Milk
{
	public class CompMilkableHuman : HumanCompHasGatherableBodyResource
	{
		protected override float GatherResourcesIntervalDays
		{
			get
			{
				return this.Props.milkIntervalDays;
			}
		}

		protected override float ResourceAmount
		{
			get
			{
				return this.Props.milkAmount;
			}
		}
        protected override float ResourceAmountBase
        {
            get
            {
                return this.Props.milkAmountBase;
            }
        }

        protected override ThingDef ResourceDef
		{
			get
			{
				return this.Props.milkDef;
			}
		}

		protected override string SaveKeyFullness
		{
			get
			{
				return "milkFullness";
			}
		}

        protected override string SaveKeyBottleCount
        {
            get
            {
                return "bottleCount";
            }
        }


        public CompProperties_MilkableHuman Props
		{
			get
			{
				return (CompProperties_MilkableHuman)this.props;
			}
		}
		
		public override bool Active
		{
			get
			{
				if (!base.Active)
				{
					return false;
				}

				Pawn pawn = this.parent as Pawn;
				if (pawn == null)
				{
					Log.Warning("[Milk] comp.parent is null");
					return false;
				}
                var phs = pawn.health.hediffSet;

                BodyPartRecord bpr = pawn.RaceProps.body.AllParts.Find(item => item.def == xxx.breastsDef);
				if (phs.GetPartHealth(bpr) == 0)
				{
					if (pawn.IsHashIntervalTick(120))
					{
                        if (ModsConfig.BiotechActive)
						{
                            if (phs.HasHediff(HediffDef.Named("Lactating")))
                            {
                                pawn.health.RemoveHediff(phs.GetFirstHediffOfDef(HediffDef.Named("Lactating")));
                            }
                        }
                        if (phs.HasHediff(HediffDef.Named("Lactating_Drug")))
                        {
                            pawn.health.RemoveHediff(phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Drug")));
                        }
                        if (phs.HasHediff(HediffDef.Named("Lactating_Natural")))
                        {
                            pawn.health.RemoveHediff(phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Natural")));
                        }
                        if (phs.HasHediff(HediffDef.Named("Lactating_Permanent")))
						{
							pawn.health.RemoveHediff(phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Permanent")));
						}
						if (phs.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent")))
						{
							pawn.health.RemoveHediff(phs.GetFirstHediffOfDef(HediffDef.Named("Heavy_Lactating_Permanent")));
						}
					}
					//Log.Warning("[Milk] comp chest health is 0");
					return false;
				}
				
				/*
				bool isHeavyLactatingPermanent = health.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent"));
				if (isHeavyLactatingPermanent)
					return false;
				*/


				//conditions to add and remove other hediffs based on what hediffs the pawn currently has
				bool isLactatingBT = false;
                bool isLactatingDrug = phs.HasHediff(HediffDef.Named("Lactating_Drug"));
                bool isLactatingNatural = phs.HasHediff(HediffDef.Named("Lactating_Natural"));
				bool isLactatingPermanent = phs.HasHediff(HediffDef.Named("Lactating_Permanent"));
                bool isHeavyLactatingPermanent = phs.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent"));

				bool isPregnant = false;
				bool isPregnantLateStage = false;

                if (ModsConfig.BiotechActive)
                {
                    isLactatingBT = phs.HasHediff(HediffDef.Named("Lactating"));
                    isPregnant = phs.HasHediff(HediffDef.Named("PregnantHuman"));
                }

				Hediff pregnantHediff = null;
                if (isPregnant)
				{
                    pregnantHediff = phs.GetFirstHediffOfDef(HediffDef.Named("PregnantHuman"));
					isPregnantLateStage = pregnantHediff.Severity > 0.8;
                }
				//moved a bunch of add/remove hediff logic from here and into the HumanCompHasGatherableBodyResources tick instead. Probably slightly better for performance.

				bool returnResult = ((!this.Props.milkFemaleOnly || pawn.gender == Gender.Female) &&
                        (MilkSettings.ignoreReproductiveStage || pawn.ageTracker.CurLifeStage.reproductive) &&
                        pawn.RaceProps.Humanlike &&
                        (isLactatingBT || isLactatingPermanent || isHeavyLactatingPermanent || isLactatingDrug || isLactatingNatural || isPregnantLateStage));

				 //pawn.MilkAvailable = returnResult;

				return returnResult;

            }
		}

		public override string CompInspectStringExtra()
		{
			if (!this.Active)
				return null;
            return Translator.Translate("MilkFullnessMilk") + ": " + GenText.ToStringPercent(base.Fullness) + " (" + Translator.Translate("MilkFullnessMilkBottles") + ": " + ((Math.Floor(base.bottleCount * 10f)) / 10f).ToString() + ")";
		}
	}
}
