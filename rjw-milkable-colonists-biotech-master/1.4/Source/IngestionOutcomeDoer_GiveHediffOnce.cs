using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Milk
{
	class IngestionOutcomeDoer_GiveHediffOnce : IngestionOutcomeDoer
	{
#pragma warning disable CS0649

		public HediffDef hediff;

		public List<BodyPartDef> partsToAffect;

#pragma warning restore CS0649

		protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested)
		{
			HediffGiverUtility.TryApply(pawn, hediff, partsToAffect);
			SendLetter(pawn, ingested);
		}

		protected void SendLetter(Pawn pawn, Thing thing) // copied from HediffGiver
		{
			if (PawnUtility.ShouldSendNotificationAbout(pawn))
			{
				if (thing == null)
				{
					Find.LetterStack.ReceiveLetter(
						"LetterHediffFromRandomHediffGiverLabel".Translate(pawn.LabelShort, hediff.LabelCap,
						pawn.Named("PAWN")).CapitalizeFirst(),
						"LetterHediffFromRandomHediffGiver".Translate(pawn.LabelShort, hediff.LabelCap, pawn.Named("PAWN")).CapitalizeFirst(),
						LetterDefOf.NegativeEvent, pawn);
				}
				else
				{
					Find.LetterStack.ReceiveLetter(
						"LetterHealthComplicationsLabel".Translate(pawn.LabelShort, hediff.LabelCap, pawn.Named("PAWN")).CapitalizeFirst(),
						"LetterHealthComplications".Translate(pawn.LabelShort, hediff.LabelCap, thing.LabelCap, pawn.Named("PAWN")).CapitalizeFirst(),
						LetterDefOf.NegativeEvent, pawn);
				}
			}
		}
	}
}
