using System;
using System.Linq;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace Milk
{
	public abstract class HumanCompHasGatherableBodyResource : ThingComp
	{
		protected abstract float GatherResourcesIntervalDays { get; }

		protected abstract float ResourceAmount { get; }

		protected abstract ThingDef ResourceDef { get; }

		protected abstract string SaveKey { get; }

		public float BreastSize = 1f;

		public float BreastSizeDays = 1f;

		protected float fullness;
		public float Fullness
		{
			get
			{
				return this.fullness;
			}
		}

		protected virtual bool Active
		{
			get
			{
				if (this.parent is Pawn)
				{
					var p = this.parent as Pawn;
					if (!p.InContainerEnclosed) //TODO: someday milking in bioreactor/vat/milkmachine?
						if (!p.Dead)
							return p.IsColonist || p.IsPrisoner || (p.Map != null && p.Map.IsPlayerHome);
				}

				return false;
			}
		}

		public bool ActiveAndFull
		{
			get
			{
				return this.Active && this.fullness >= 1f;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<float>(ref this.fullness, this.SaveKey, 0f, false);
		}

		const int updateInterval = 100;

		public override void CompTick()
		{
			if (!parent.IsHashIntervalTick(updateInterval))
			{
				return;
			}

			if (this.Active)
			{
				//Log.Message("CompTick " + xxx.get_pawnname(this.parent as Pawn));

				var pawn = this.parent as Pawn;
				var t2 = pawn.GetBreastList();
				var basemilkgen = 0.5f;
				var outmilkgen = 0f;
				if (!t2.NullOrEmpty())
					foreach ( var breasts in t2.Where(x => !x.TryGetComp<CompHediffBodyPart>().FluidType.NullOrEmpty()))
					{
						if (MilkBase.flatChestGivesMilk)
						{
							outmilkgen += Math.Max(0.1f, breasts.Severity);
						}
						else
						{
							outmilkgen += breasts.Severity;
						}
					}

				if (outmilkgen == 0)
				{
					this.fullness = 0;
					return;
				}
				outmilkgen += t2.Count * basemilkgen;
				this.BreastSizeDays = outmilkgen;

				float num = updateInterval / (this.GatherResourcesIntervalDays * 60000f);
				num *= PawnUtility.BodyResourceGrowthSpeed(pawn);

				//Log.Message("this.fullness + num " + this.fullness + num);
				this.fullness = Math.Min(this.fullness + num, 1.5f);
				//Log.Message("this.fullness " + this.fullness);

				if (!MilkBase.breastGrowthDisabled)
				{
					if (this.fullness >= 1.0f)
					{
						//unlikely reach here unless milking work is disabled
						//expand breasts if over max value
						if (pawn.IsHashIntervalTick(60000))
							if (!t2.NullOrEmpty())
								foreach (var breasts in t2)
								{
									if (breasts.Severity < 0.9f)
										breasts.Severity += (breasts.Severity * 0.01f);
								}
						//TODO: add milk leaking filth?
						//TODO: add breast pain?
					}
				}
			}
		}

		public void Gathered(Pawn doer)
		{
			if (!this.Active)
			{
				Log.Error(doer + " gathered body resources while not Active: " + this.parent, false);
			}

			if (!Rand.Chance(StatExtension.GetStatValue(doer, StatDefOf.AnimalGatherYield, true)))
			{
				MoteMaker.ThrowText((doer.DrawPos + this.parent.DrawPos) / 2f, this.parent.Map, Translator.Translate("TextMote_ProductWasted"), 3.65f);
			}
			else
			{
				var pawn = this.parent as Pawn;
				var t2 = pawn.GetBreastList();
				//var basemilkgen = 0.5f;
				var outmilkgen = 0f;
				if (!t2.NullOrEmpty())
					foreach (var breasts in t2.Where(x => !x.TryGetComp<CompHediffBodyPart>().FluidType.NullOrEmpty()))
					{
						if (MilkBase.flatChestGivesMilk)
						{
							outmilkgen += Math.Max(0.1f, breasts.Severity);
						}
						else
						{
							outmilkgen += breasts.Severity;
						}
					}
				//outmilkgen += t2.Count * basemilkgen;
				this.BreastSize = outmilkgen;

				int ii = GenMath.RoundRandom(this.ResourceAmount * this.BreastSize * this.fullness);
				while (ii > 0)
				{
					int num = Mathf.Clamp(ii, 1, this.ResourceDef.stackLimit);
					ii -= num;
					Thing thing = ThingMaker.MakeThing(this.ResourceDef, null);
					thing.stackCount = num;
					GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near, null, null, default(Rot4));
				}

				if (!MilkBase.breastGrowthDisabled)
				{
					var health = pawn.health.hediffSet;
					bool isLactatingDrug = health.HasHediff(HediffDef.Named("Lactating_Drug"));
					bool isLactatingNatural = health.HasHediff(HediffDef.Named("Lactating_Natural"));
					bool isLactatingPermanent = health.HasHediff(HediffDef.Named("Lactating_Permanent"));
					bool isOrassanLactatingPermanent = MilkBase.Orassan_Lactating_Permanent != null && health.HasHediff(MilkBase.Orassan_Lactating_Permanent);
					bool isHeavyLactatingPermanent = health.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent"));
					//expand breasts due to milking
					if (!t2.NullOrEmpty())
						foreach (var breasts in t2)
						{
							if (breasts is Hediff_PartBaseArtifical)
								continue;

							if (isHeavyLactatingPermanent)
								if (breasts.Severity < 1.2f)
									breasts.Severity += (breasts.Severity * 0.03f);
								else
								{
									if (isLactatingDrug)
										if (breasts.Severity < 0.9f)
											breasts.Severity += (breasts.Severity * 0.02f);

									if (isLactatingNatural || isLactatingPermanent || isOrassanLactatingPermanent)
										if (breasts.Severity < 0.6f)
											breasts.Severity += (breasts.Severity * 0.01f);

								}
						}
				}
			}
			this.fullness = 0f;
		}
	}
}
