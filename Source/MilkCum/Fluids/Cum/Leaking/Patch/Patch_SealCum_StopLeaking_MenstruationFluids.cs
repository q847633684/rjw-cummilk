using HarmonyLib;
using MilkCum.Fluids.Cum.Common;
using MilkCum.Fluids.Cum.Leaking;
using RJW_Menstruation;
using Verse;
using RwMenstruationCum = RJW_Menstruation.Cum;

namespace MilkCum.Fluids.Cum.Leaking.Patch;

/// <summary>
/// 月经周期模组中子宫内 <see cref="RwMenstruationCum"/> 的自然衰减：密封时不应按泄漏系数衰减（与 CumpilationLite 意图一致；适配当前 DismishNatural 签名）。
/// </summary>
[HarmonyPatch(typeof(RwMenstruationCum), nameof(RwMenstruationCum.DismishNatural))]
static class Patch_SealCum_StopLeaking_MenstruationFluids
{
	public static bool Prefix(RwMenstruationCum __instance, ref float leakfactor, HediffComp_Menstruation comp, float antisperm = 0f)
	{
		if (__instance?.pawn == null)
			return true;

		Comp_SealCum seal = __instance.pawn.TryGetComp<Comp_SealCum>();
		if (seal != null && seal.IsSealed())
		{
			ModLog.Debug("seal stopped leaking");
			leakfactor = 0f;
			return true;
		}

		ModLog.Debug("seal did not stop leaking");
		return true;
	}
}
