using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milk
{
	[HarmonyPatch(typeof(HaulAIUtility), "PawnCanAutomaticallyHaulFast")]
	public static class HaulAIUtility_PawnCanAutomaticallyHaulFast_Patch
	{
		[HarmonyPostfix]
		public static void isMilk(ref Pawn p, ref Thing t, ref bool forced, ref bool __result)
		{
			LocalTargetInfo localTargetInfo = t;
			bool canReserve = ReservationUtility.CanReserve(p, localTargetInfo, 1, -1, null, forced) &&
				ReachabilityUtility.CanReach(p, t, PathEndMode.ClosestTouch, DangerUtility.NormalMaxDanger(p), false, 0) &&
				!FireUtility.IsBurning(t) &&
				(t.def == ThingDef.Named("Milk") || t.def == ThingDef.Named("HumanoidMilk") || t.def == ThingDef.Named("EggChickenUnfertilized"));
			if (canReserve)
			{
				__result = canReserve;
			}
		}
	}
}
