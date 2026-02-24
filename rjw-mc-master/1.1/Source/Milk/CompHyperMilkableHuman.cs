using System;
using Verse;

namespace Milk
{
	public class CompHyperMilkableHuman : HumanCompHasGatherableBodyResource
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

		public CompProperties_HyperMilkableHuman Props
		{
			get
			{
				return (CompProperties_HyperMilkableHuman)this.props;
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
				if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent")))
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
				}

				return (!this.Props.milkFemaleOnly || pawn.gender == Gender.Female) &&
					pawn.ageTracker.CurLifeStage.reproductive &&
					pawn.RaceProps.Humanlike &&
					pawn.health.hediffSet.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent"));
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
