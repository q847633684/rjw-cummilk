using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cumpilation;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cumpilation.Leaking
{
	[HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
	public class Patch_GetDrawParms
	{
		public static void Postfix(PawnRenderer __instance, ref PawnDrawParms __result)
		{
			var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
			if (pawn != null && pawn.health?.hediffSet != null && pawn.health.hediffSet.HasHediff(DefOfs.Deflating))
			{
				__result.flags &= ~PawnRenderFlags.Clothes;
			}
		}
	}
}
