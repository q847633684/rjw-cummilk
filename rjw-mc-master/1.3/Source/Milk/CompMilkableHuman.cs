using System;
using Verse;
using rjw;

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

		protected override ThingDef ResourceDef
		{
			get
			{
				return this.Props.milkDef;
			}
		}

		protected override string SaveKey
		{
			get
			{
				return "milkFullness";
			}
		}

		public CompProperties_MilkableHuman Props
		{
			get
			{
				return (CompProperties_MilkableHuman)this.props;
			}
		}
		
		protected override bool Active
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
				var health = pawn.health.hediffSet;
				BodyPartRecord bpr = pawn.RaceProps.body.AllParts.Find(item => item.def == xxx.breastsDef);
				if (health.GetPartHealth(bpr) == 0)
				{
					if (pawn.IsHashIntervalTick(120))
					{
						if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Lactating_Natural")))
						{
							pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Lactating_Natural")));
						}
						if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Lactating_Drug")))
						{
							pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Lactating_Drug")));
						}
						if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Lactating_Permanent")))
						{
							pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Lactating_Permanent")));
						}
						if (MilkBase.Orassan_Lactating_Permanent != null && pawn.health.hediffSet.HasHediff(MilkBase.Orassan_Lactating_Permanent))
						{
							pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(MilkBase.Orassan_Lactating_Permanent));
						}
						if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent")))
						{
							pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Heavy_Lactating_Permanent")));
						}
					}
					//Log.Warning("[Milk] comp chest health is 0");
					return false;
				}

				bool isHeavyLactatingPermanent = health.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent"));
				if (isHeavyLactatingPermanent)
					return false;

				bool isLactatingDrug = health.HasHediff(HediffDef.Named("Lactating_Drug"));
				bool isLactatingNatural = health.HasHediff(HediffDef.Named("Lactating_Natural"));
				bool isLactatingPermanent = health.HasHediff(HediffDef.Named("Lactating_Permanent"));
				bool isOrassanLactatingPermanent = MilkBase.Orassan_Lactating_Permanent != null && health.HasHediff(MilkBase.Orassan_Lactating_Permanent);

				Func<Hediff> humanPregnancy = () => health.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("HumanPregnancy"));// cnp?

				Func<bool> isMilkableMuffalo = () => health.HasHediff(DefDatabase<HediffDef>.GetNamedSilentFail("GR_MuffaloMammaries"));

				bool shouldLactateNaturally = 
					!isLactatingPermanent && !isLactatingNatural && !isOrassanLactatingPermanent &&
					((humanPregnancy()?.Visible ?? false) || rjw.PawnExtensions.IsVisiblyPregnant(pawn) || isMilkableMuffalo());

				if (shouldLactateNaturally)
				{
					pawn.health.AddHediff(HediffDef.Named("Lactating_Natural"), null, null, null);
				}

				if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Lactating_Permanent")))
				{
					if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Lactating_Natural")))
					{
						pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Lactating_Natural")));
					}
				}

				if (MilkBase.Orassan_Lactating_Permanent != null && pawn.health.hediffSet.HasHediff(MilkBase.Orassan_Lactating_Permanent))
				{
					if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Lactating_Natural")))
					{
						pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Lactating_Natural")));
					}
				}

				return ((!this.Props.milkFemaleOnly || pawn.gender == Gender.Female) &&
						(MilkBase.ignoreReproductiveStage || pawn.ageTracker.CurLifeStage.reproductive) &&
						pawn.RaceProps.Humanlike &&
						(isLactatingDrug || isLactatingPermanent || isLactatingNatural || isOrassanLactatingPermanent));
			}
		}

		public override string CompInspectStringExtra()
		{
			if (!this.Active)
				return null;
			return Translator.Translate("MilkFullness") + ": " + GenText.ToStringPercent(base.Fullness);
		}
	}
}
