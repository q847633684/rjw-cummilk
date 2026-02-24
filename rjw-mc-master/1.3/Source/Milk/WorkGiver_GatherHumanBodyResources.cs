using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Milk
{
	public abstract class WorkGiver_GatherHumanBodyResources : WorkGiver_Scanner
	{
		protected abstract JobDef JobDef { get; }

		protected abstract HumanCompHasGatherableBodyResource GetComp(Pawn animal);

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			foreach (Pawn pawn2 in pawn.Map.mapPawns.FreeColonistsAndPrisonersSpawned)
			{
				yield return pawn2;
			}
			yield break;
		}

		public override PathEndMode PathEndMode
		{
			get
			{
				return PathEndMode.Touch;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn pawn2 = t as Pawn;
			bool flag = pawn2 == null || !pawn2.RaceProps.Humanlike || pawn2.Drafted || pawn2.InAggroMentalState || CaravanFormingUtility.IsFormingCaravan(pawn2);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				HumanCompHasGatherableBodyResource comp = this.GetComp(pawn2);
				bool flag2 = comp != null && comp.ActiveAndFull && pawn2 != pawn;
				if (flag2)
				{
					LocalTargetInfo localTargetInfo = pawn2;
					bool flag3 = ReservationUtility.CanReserve(pawn, localTargetInfo, 1, -1, null, forced);
					if (flag3)
					{
						return true;
					}
				}
				result = false;
			}
			return result;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return new Job(this.JobDef, t);
		}
	}
}
