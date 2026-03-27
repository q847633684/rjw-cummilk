using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using rjw;

namespace MilkCum.RJW;

/// <summary>
/// RJW 仍把 xxx.breastsDef 指向 Chest；本 mod 在补丁里增加 Breast 叶部位后，用 Harmony 衔接生成与列表逻辑（不修改 RJW 源码）。
/// </summary>
[HarmonyPatch]
internal static class RJW_BreastAnatomyHarmony
{
	private static BodyPartDef BreastDef => DefDatabase<BodyPartDef>.GetNamedSilentFail("Breast");

	private static BodyPartDef MechBreastDef => DefDatabase<BodyPartDef>.GetNamedSilentFail("MechBreast");

	[HarmonyPatch(typeof(Genital_Helper), nameof(Genital_Helper.get_breastsBPR))]
	[HarmonyPrefix]
	private static bool get_breastsBPR_Prefix(Pawn pawn, ref BodyPartRecord __result)
	{
		var breast = BreastDef;
		var mechBreast = MechBreastDef;
		if (pawn?.RaceProps?.body?.AllParts == null || (breast == null && mechBreast == null))
		{
			return true;
		}

		var hit = breast != null
			? pawn.RaceProps.body.AllParts.FirstOrDefault(b => b.def == breast)
			: null;
		if (hit == null && mechBreast != null)
		{
			hit = pawn.RaceProps.body.AllParts.FirstOrDefault(b => b.def == mechBreast);
		}

		if (hit != null)
		{
			__result = hit;
			return false;
		}

		return true;
	}

	[HarmonyPatch(typeof(SexPartAdder), nameof(SexPartAdder.add_breasts))]
	[HarmonyPrefix]
	private static bool add_breasts_Prefix(Pawn pawn, Pawn parent, Gender gender)
	{
		var breast = BreastDef;
		var mechBreast = MechBreastDef;
		if (pawn?.RaceProps?.body?.AllParts == null || (breast == null && mechBreast == null))
		{
			return true;
		}

		foreach (var bpr in pawn.RaceProps.body.AllParts)
		{
			if (bpr.def != breast && bpr.def != xxx.mechbreastsDef && bpr.def != mechBreast)
			{
				continue;
			}

			if (pawn.health.hediffSet.PartIsMissing(bpr))
			{
				continue;
			}

			if (pawn.TryAddRacePart(bpr, gender))
			{
				continue;
			}

			LegacySexPartAdder.AddBreasts(pawn, bpr, parent);
		}

		return false;
	}

	[HarmonyPatch(typeof(RaceGroupDef), nameof(RaceGroupDef.GetRacePartDefNames))]
	[HarmonyPrefix]
	private static bool GetRacePartDefNames_Prefix(
		RaceGroupDef __instance,
		Pawn pawn,
		BodyPartRecord bpr,
		Gender gender,
		ref List<string> __result)
	{
		var breast = BreastDef;
		var mechBreast = MechBreastDef;
		if (bpr == null || (breast == null && mechBreast == null))
		{
			return true;
		}

		bool isBreast = breast != null && bpr.def == breast;
		bool isMechBreast = mechBreast != null && bpr.def == mechBreast;
		if (!isBreast && !isMechBreast)
		{
			return true;
		}

		if (gender == Gender.Female)
		{
			__result = __instance.femaleBreasts;
			return false;
		}

		if (gender == Gender.Male)
		{
			__result = __instance.maleBreasts;
			return false;
		}

		__result = __instance.femaleBreasts;
		return false;
	}

	[HarmonyPatch(typeof(RaceGroupDef), nameof(RaceGroupDef.GetChances))]
	[HarmonyPrefix]
	private static bool GetChances_Prefix(
		RaceGroupDef __instance,
		Pawn pawn,
		BodyPartRecord bpr,
		Gender gender,
		ref List<float> __result)
	{
		var breast = BreastDef;
		var mechBreast = MechBreastDef;
		if (breast == null && mechBreast == null)
		{
			return true;
		}

		if (bpr == null || (bpr.def != breast && bpr.def != mechBreast))
		{
			return true;
		}

		if (gender == Gender.Female)
		{
			__result = __instance.chancefemaleBreasts;
			return false;
		}

		if (gender == Gender.Male)
		{
			__result = __instance.chancemaleBreasts;
			return false;
		}

		__result = __instance.chancefemaleBreasts;
		return false;
	}

	[HarmonyPatch(typeof(PawnExtensions), nameof(PawnExtensions.GetBreastList))]
	[HarmonyPrefix]
	private static bool GetBreastList_Prefix(Pawn pawn, ref List<Hediff> __result)
	{
		var breast = BreastDef;
		var mechBreast = MechBreastDef;
		if (pawn?.RaceProps?.body?.AllParts == null || (breast == null && mechBreast == null))
		{
			return true;
		}

		try
		{
			var list = new List<Hediff>();
			foreach (var bpr in pawn.RaceProps.body.AllParts)
			{
				if (bpr.def != breast && bpr.def != xxx.mechbreastsDef && bpr.def != mechBreast)
				{
					continue;
				}

				list.AddRange(Genital_Helper.get_PartsHediffList(pawn, bpr));
			}

			__result = list;
			pawn.GetRJWPawnData().breasts = list;
			return false;
		}
		catch
		{
			return true;
		}
	}

	/// <summary>
	/// RJW 的 Hediff_MissingBodyPart_RemoveParts 只认 xxx.breastsDef（Chest）；叶部位为 Breast 时单侧缺失不会清 RJW 乳房类 Hediff，此处对齐行为。
	/// 目标必须与 RJW 相同：<see cref="Hediff_MissingPart.PostAdd"/>。
	/// </summary>
	[HarmonyPatch(typeof(Hediff_MissingPart), nameof(Hediff_MissingPart.PostAdd))]
	[HarmonyAfter("rjw.Hediff_MissingBodyPart_RemoveParts")]
	private static class MissingBreast_RemoveSexPartHediffs
	{
		private static void Postfix(Hediff_MissingPart __instance)
		{
			var breast = BreastDef;
			var mechBreast = MechBreastDef;
			var bodyPart = __instance?.Part;
			if (bodyPart == null || (breast == null && mechBreast == null))
			{
				return;
			}

			bool isBreast = breast != null && bodyPart.def == breast;
			bool isMechBreast = mechBreast != null && bodyPart.def == mechBreast;
			if (!isBreast && !isMechBreast)
			{
				return;
			}

			if (!__instance.pawn.health.hediffSet.hediffs.Any(h => h.Part == bodyPart && h.def != __instance.def))
			{
				return;
			}

			var toRemove = __instance.pawn.health.hediffSet.hediffs
				.Where(h => h.Part == bodyPart && h.def != __instance.def)
				.ToList();
			foreach (var h in toRemove)
			{
				__instance.pawn.health.RemoveHediff(h);
			}
		}
	}

	/// <summary>
	/// 解剖补丁使用英文 customLabel；此处用 Keyed 在中文等语言下显示「左乳/右乳」等。
	/// </summary>
	[HarmonyPatch(typeof(BodyPartRecord), "get_Label")]
	private static class BodyPartRecord_Label_BreastSidesTranslate
	{
		private static void Postfix(BodyPartRecord __instance, ref string __result)
		{
			switch (__instance?.customLabel)
			{
				case "left breast":
					__result = "EM.BodyPartLeftBreast".Translate();
					break;
				case "right breast":
					__result = "EM.BodyPartRightBreast".Translate();
					break;
				case "left mech breast":
					__result = "EM.BodyPartLeftMechBreast".Translate();
					break;
				case "right mech breast":
					__result = "EM.BodyPartRightMechBreast".Translate();
					break;
			}
		}
	}
}
